// Interop assíncrono compartilhado por Blazor Server e WebAssembly.
// Nenhuma biblioteca JavaScript externa é necessária.
const chunkSize = 8192;

function clean(value) {
    if (Array.isArray(value)) return value.map(clean);
    if (value && typeof value === "object")
        return Object.fromEntries(Object.entries(value)
            .filter(([, item]) => item !== null && item !== undefined)
            .map(([key, item]) => [key, clean(item)]));
    return value;
}

function api(kind) {
    if (!globalThis.isSecureContext) throw new Error("Dispositivos web exigem HTTPS ou localhost.");
    if (!["serial", "usb", "bluetooth"].includes(kind)) throw new Error("Tipo de dispositivo inválido.");
    const result = navigator[kind];
    if (!result) throw new Error("A API Web " + kind + " não está disponível neste navegador/sistema.");
    return result;
}

function choose(kind, options) {
    const target = api(kind);
    const settings = clean(options);
    if (kind === "serial") return target.requestPort(settings);
    if (kind === "bluetooth" && settings.acceptAllDevices) delete settings.filters;
    return target.requestDevice(settings);
}

function bytes(value) {
    if (value instanceof DataView) return new Uint8Array(value.buffer, value.byteOffset, value.byteLength);
    return new Uint8Array(value);
}

class Session {
    constructor(kind, device) {
        this.kind = kind;
        this.device = device;
        this.closed = true;
        this.operations = Promise.resolve();
        this.notifications = Promise.resolve();
    }

    enqueue(operation) {
        const next = this.operations.catch(() => {}).then(operation);
        this.operations = next;
        return next;
    }

    async deliver(data) {
        for (let offset = 0; offset < data.length && !this.closed; offset += chunkSize)
            await this.receiver.invokeMethodAsync("Receive", data.slice(offset, offset + chunkSize));
    }

    async fail(error) {
        if (this.closed) return;
        try { await this.receiver.invokeMethodAsync("Fail", error?.message ?? String(error)); }
        catch { /* O circuito ou o componente deixou de existir. */ }
        // Não aguarda close aqui: close precisa aguardar o término da leitura.
        void this.enqueue(() => this.close()).catch(() => {});
    }

    async open(options, receiver) {
        if (!this.closed) throw new Error("O dispositivo já está aberto.");
        this.receiver = receiver;
        this.closed = false;
        this.options = clean(options);
        this.notifications = Promise.resolve();
        this.disconnected = () => { void this.fail(new Error("Dispositivo desconectado.")); };
        this.device.addEventListener(this.kind === "bluetooth" ? "gattserverdisconnected" : "disconnect", this.disconnected);
        try {
            if (this.kind === "serial") {
                await this.device.open(this.options);
                if (!this.device.writable) throw new Error("A porta serial não permite escrita.");
                this.writer = this.device.writable.getWriter();
                this.reader = this.device.readable?.getReader();
                if (this.reader) this.readTask = this.readSerial();
                return !!this.reader;
            }
            if (this.kind === "usb") {
                await this.openUsb();
                if (this.inEndpoint != null) this.readTask = this.readUsb();
                return this.inEndpoint != null;
            }
            return await this.openBluetooth();
        } catch (error) {
            await this.close().catch(() => {});
            throw error;
        }
    }

    async readSerial() {
        try {
            while (!this.closed) {
                const result = await this.reader.read();
                if (result.done) {
                    if (!this.closed) throw new Error("A leitura da porta serial foi encerrada.");
                    break;
                }
                if (result.value?.length) await this.deliver(bytes(result.value));
            }
        } catch (error) { await this.fail(error); }
        finally { this.reader.releaseLock(); }
    }

    async openUsb() {
        const device = this.device, options = this.options;
        if (!Number.isInteger(options.packetSize) || options.packetSize < 1 || options.packetSize > 65536)
            throw new Error("PacketSize USB deve estar entre 1 e 65536.");
        if (!device.opened) await device.open();
        if (device.configuration?.configurationValue !== options.configurationValue)
            await device.selectConfiguration(options.configurationValue);

        let selected;
        for (const iface of device.configuration.interfaces) {
            if (options.interfaceNumber != null && iface.interfaceNumber !== options.interfaceNumber) continue;
            const alternates = options.alternateSetting == null
                ? [iface.alternate, ...iface.alternates.filter(a => a.alternateSetting !== iface.alternate.alternateSetting)]
                : iface.alternates.filter(a => a.alternateSetting === options.alternateSetting);
            for (const alternate of alternates) {
                const bulk = alternate.endpoints.filter(e => e.type === "bulk");
                const output = bulk.find(e => e.direction === "out" &&
                    (options.outEndpointNumber == null || e.endpointNumber === options.outEndpointNumber));
                const input = bulk.find(e => e.direction === "in" &&
                    (options.inEndpointNumber == null || e.endpointNumber === options.inEndpointNumber));
                if (!output || (options.inEndpointNumber != null && !input)) continue;
                selected = { iface, alternate, output, input };
                break;
            }
            if (selected) break;
        }
        if (!selected) throw new Error("Interface/endpoints bulk USB incompatíveis com a configuração.");
        await device.claimInterface(selected.iface.interfaceNumber);
        if (selected.iface.alternate.alternateSetting !== selected.alternate.alternateSetting)
            await device.selectAlternateInterface(selected.iface.interfaceNumber, selected.alternate.alternateSetting);
        this.outEndpoint = selected.output.endpointNumber;
        this.inEndpoint = selected.input?.endpointNumber;
    }

    async readUsb() {
        try {
            while (!this.closed) {
                const result = await this.device.transferIn(this.inEndpoint, this.options.packetSize);
                if (this.closed) break;
                if (result.status === "stall") {
                    await this.device.clearHalt("in", this.inEndpoint);
                    continue;
                }
                if (result.status !== "ok") throw new Error("Leitura USB falhou: " + result.status);
                if (result.data?.byteLength) await this.deliver(bytes(result.data));
            }
        } catch (error) { await this.fail(error); }
    }

    async openBluetooth() {
        const options = this.options;
        if (!options.serviceUuid || !options.writeCharacteristicUuid)
            throw new Error("Informe o serviço e a característica GATT de escrita.");
        if (!Number.isInteger(options.packetSize) || options.packetSize < 1 || options.packetSize > 512)
            throw new Error("PacketSize BLE deve estar entre 1 e 512.");
        const server = await this.device.gatt.connect();
        const service = await server.getPrimaryService(options.serviceUuid);
        this.writeCharacteristic = await service.getCharacteristic(options.writeCharacteristicUuid);
        const properties = this.writeCharacteristic.properties;
        if (options.writeWithResponse ? !properties.write : !properties.writeWithoutResponse)
            throw new Error("A característica GATT não suporta o modo de escrita solicitado.");
        if (!options.notificationCharacteristicUuid) return false;
        this.notificationCharacteristic = await service.getCharacteristic(options.notificationCharacteristicUuid);
        this.notificationHandler = event => {
            const data = bytes(event.target.value).slice();
            this.notifications = this.notifications.then(() => this.deliver(data)).catch(error => this.fail(error));
        };
        this.notificationCharacteristic.addEventListener("characteristicvaluechanged", this.notificationHandler);
        await this.notificationCharacteristic.startNotifications();
        return true;
    }

    async write(data) {
        if (this.closed) throw new Error("Dispositivo fechado. Use OpenAsync.");
        if (this.kind === "serial") {
            await this.writer.write(data);
        } else if (this.kind === "usb") {
            let offset = 0;
            while (offset < data.length) {
                const result = await this.device.transferOut(this.outEndpoint, data.subarray(offset));
                if (result.status !== "ok") {
                    if (result.status === "stall") await this.device.clearHalt("out", this.outEndpoint);
                    throw new Error("Escrita USB falhou: " + result.status);
                }
                if (!(result.bytesWritten > 0 && result.bytesWritten <= data.length - offset))
                    throw new Error("Escrita USB retornou uma quantidade inválida de bytes.");
                offset += result.bytesWritten;
            }
        } else {
            const method = this.options.writeWithResponse ? "writeValueWithResponse" : "writeValueWithoutResponse";
            for (let offset = 0; offset < data.length; offset += this.options.packetSize)
                await this.writeCharacteristic[method](data.subarray(offset, offset + this.options.packetSize));
        }
    }

    async close() {
        if (this.closed) return;
        this.closed = true;
        this.device.removeEventListener(this.kind === "bluetooth" ? "gattserverdisconnected" : "disconnect", this.disconnected);
        try {
            if (this.kind === "serial") {
                try {
                    if (this.reader) await this.reader.cancel().catch(() => {});
                    await this.readTask;
                } finally {
                    this.writer?.releaseLock();
                    if (this.device.readable || this.device.writable) await this.device.close();
                }
            } else if (this.kind === "usb") {
                // close cancela transferências IN pendentes e libera as interfaces.
                if (this.device.opened) await this.device.close();
                await this.readTask;
            } else {
                try {
                    if (this.notificationCharacteristic) {
                        this.notificationCharacteristic.removeEventListener("characteristicvaluechanged", this.notificationHandler);
                        if (this.device.gatt.connected) await this.notificationCharacteristic.stopNotifications();
                    }
                    await this.notifications;
                } finally {
                    if (this.device.gatt.connected) this.device.gatt.disconnect();
                }
            }
        } finally {
            this.reader = this.writer = this.readTask = this.receiver = undefined;
            this.writeCharacteristic = this.notificationCharacteristic = this.notificationHandler = undefined;
        }
    }
}

class DeviceManager {
    constructor() {
        this.sessions = new Map();
        this.bindings = new Set();
        this.nextId = 1;
        this.disposed = false;
        this.onPageHide = () => { void this.dispose(); };
        globalThis.addEventListener("pagehide", this.onPageHide);
    }

    support() {
        return {
            secureContext: globalThis.isSecureContext,
            serial: !!navigator.serial, usb: !!navigator.usb, bluetooth: !!navigator.bluetooth
        };
    }

    register(kind, device) {
        if (this.disposed) throw new Error("Serviço de dispositivos descartado.");
        for (const [id, session] of this.sessions)
            if (session.device === device && session.kind === kind) return this.info(id, session);
        const id = this.nextId++, session = new Session(kind, device);
        this.sessions.set(id, session);
        return this.info(id, session);
    }

    info(id, session) {
        const info = session.device.getInfo?.() ?? session.device;
        return {
            id, kind: session.kind, name: session.device.name ?? session.device.productName ?? session.kind,
            vendorId: info.usbVendorId ?? info.vendorId ?? null,
            productId: info.usbProductId ?? info.productId ?? null
        };
    }

    // choose é chamado antes de qualquer await, preservando a ativação do usuário.
    request(kind, options) {
        return choose(kind, options).then(device => this.register(kind, device));
    }

    async getDevices(kind) {
        const target = api(kind);
        const method = kind === "serial" ? "getPorts" : "getDevices";
        if (!target[method]) throw new Error("Este navegador não oferece listagem de dispositivos autorizados.");
        return (await target[method]()).map(device => this.register(kind, device));
    }

    bind(button, kind, options, callback) {
        if (this.disposed) throw new Error("Serviço de dispositivos descartado.");
        if (!button?.addEventListener) throw new Error("Informe um botão renderizado.");
        api(kind);
        let active = true, busy = false;
        const binding = {
            dispose: () => {
                if (!active) return;
                active = false;
                button.removeEventListener("click", handler);
                observer.disconnect();
                this.bindings.delete(binding);
            }
        };
        const handler = async () => {
            if (!active || busy) return;
            busy = true;
            const known = new Set(this.sessions.keys());
            try {
                const info = await this.request(kind, options);
                if (active) await callback.invokeMethodAsync("Selected", info);
                else if (!known.has(info.id)) await this.release(info.id);
            } catch (error) {
                if (active) {
                    try { await callback.invokeMethodAsync("Failed", error.message ?? String(error)); }
                    catch { binding.dispose(); }
                }
            } finally { busy = false; }
        };
        button.addEventListener("click", handler);
        const observer = new MutationObserver(() => {
            if (!button.isConnected) binding.dispose();
        });
        observer.observe(document.documentElement, { childList: true, subtree: true });
        this.bindings.add(binding);
        return DotNet.createJSObjectReference(binding);
    }

    session(id) {
        const session = this.sessions.get(id);
        if (!session) throw new Error("Dispositivo não encontrado ou já descartado.");
        return session;
    }

    open(id, options, receiver) {
        const session = this.session(id);
        return session.enqueue(() => session.open(options, receiver));
    }
    write(id, data) {
        const session = this.session(id);
        return session.enqueue(() => session.write(data));
    }
    close(id) {
        const session = this.sessions.get(id);
        return session ? session.enqueue(() => session.close()) : Promise.resolve();
    }
    async release(id) {
        await this.close(id);
        this.sessions.delete(id);
    }
    async dispose() {
        if (this.disposed) return;
        this.disposed = true;
        globalThis.removeEventListener("pagehide", this.onPageHide);
        for (const binding of [...this.bindings]) binding.dispose();
        await Promise.allSettled([...this.sessions.keys()].map(id => this.release(id)));
    }
}

export function createManager() {
    return DotNet.createJSObjectReference(new DeviceManager());
}
