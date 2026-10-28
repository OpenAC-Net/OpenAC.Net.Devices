import test from "node:test";
import assert from "node:assert/strict";
import { createManager } from "../OpenAC.Net.Devices.Blazor/wwwroot/openac-devices.js";

globalThis.DotNet = { createJSObjectReference: value => value };
globalThis.isSecureContext = true;
globalThis.addEventListener = () => {};
globalThis.removeEventListener = () => {};
globalThis.document = { documentElement: {} };
globalThis.MutationObserver = class { observe() {} disconnect() {} };
const receiver = () => ({
    data: [], errors: [],
    async invokeMethodAsync(method, value) {
        if (method === "Receive") this.data.push(...value);
        else if (method === "Fail") this.errors.push(value);
    }
});
function navigatorWith(value) {
    Object.defineProperty(globalThis, "navigator", { value, configurable: true });
}
const deferred = () => {
    let resolve, reject;
    const promise = new Promise((yes, no) => { resolve = yes; reject = no; });
    return { promise, resolve, reject };
};
class Port extends EventTarget {
    getInfo() { return { usbVendorId: 123, usbProductId: 456 }; }
    async open(options) {
        this.options = options;
        this.readable = new ReadableStream({ start: controller => { this.controller = controller; } });
        this.writable = new WritableStream({ write: data => { this.sent = [...(this.sent ?? []), ...data]; } });
    }
    async close() {
        assert.equal(this.readable.locked, false);
        assert.equal(this.writable.locked, false);
        this.readable = this.writable = null;
        this.closeCount = (this.closeCount ?? 0) + 1;
    }
}

test("seleção serial parte do clique JS e libera listeners no descarte", async () => {
    const port = new Port(), button = new EventTarget(), selected = deferred();
    let inClick = false, requests = 0;
    navigatorWith({ serial: { requestPort(options) {
        assert.equal(inClick, true);
        assert.deepEqual(options, { filters: [] });
        requests++;
        return Promise.resolve(port);
    } } });
    const manager = createManager();
    const binding = manager.bind(button, "serial", { filters: [], absent: null }, {
        invokeMethodAsync(method, info) {
            assert.equal(method, "Selected");
            selected.resolve(info);
            return Promise.resolve();
        }
    });
    inClick = true;
    button.dispatchEvent(new Event("click"));
    inClick = false;
    assert.equal((await selected.promise).kind, "serial");
    binding.dispose();
    button.dispatchEvent(new Event("click"));
    assert.equal(requests, 1);
    await manager.dispose();
});

test("serial: ordem dos bytes, leitura em blocos e fechamento libera locks", async () => {
    const port = new Port(), sink = receiver();
    navigatorWith({ serial: { requestPort: async () => port } });
    const manager = createManager(), info = await manager.request("serial", {});
    assert.equal(await manager.open(info.id, { baudRate: 9600, parity: null }, sink), true);
    assert.deepEqual(port.options, { baudRate: 9600 });
    const received = deferred(), invoke = sink.invokeMethodAsync.bind(sink);
    sink.invokeMethodAsync = async (method, data) => {
        assert.ok(data.length <= 8192);
        await invoke(method, data);
        if (sink.data.length === 20000) received.resolve();
    };
    port.controller.enqueue(new Uint8Array(20000).fill(7));
    await received.promise;
    await Promise.all([
        manager.write(info.id, Uint8Array.of(1, 2)),
        manager.write(info.id, Uint8Array.of(3, 4))
    ]);
    assert.deepEqual(port.sent, [1, 2, 3, 4]);
    await manager.close(info.id);
    assert.equal(port.closeCount, 1);
    await manager.release(info.id);
    assert.throws(() => manager.open(info.id, {}, sink), /não encontrado/);
    await manager.dispose();
});

function usbDevice() {
    const device = new EventTarget();
    Object.assign(device, {
        opened: false, productName: "USB test", configuration: null, sent: [],
        async open() { this.opened = true; },
        async selectConfiguration(configurationValue) {
            const output = { endpointNumber: 2, direction: "out", type: "bulk" };
            const alternate = { alternateSetting: 0, endpoints: [output] };
            this.configuration = { configurationValue, interfaces: [
                { interfaceNumber: 3, alternate, alternates: [alternate] }
            ] };
        },
        async claimInterface(value) { this.claimed = value; },
        async transferOut(endpoint, data) {
            assert.equal(endpoint, 2);
            // Simula transferência parcial para verificar que nenhum byte é perdido.
            const count = Math.min(2, data.length);
            this.sent.push(...data.subarray(0, count));
            return { status: "ok", bytesWritten: count };
        },
        async close() { this.opened = false; this.closed = true; }
    });
    return device;
}

test("USB: detecta bulk OUT, respeita configuração e completa transferências parciais", async () => {
    const usb = usbDevice(), manager = createManager();
    navigatorWith({ usb: { requestDevice: async () => usb } });
    const info = await manager.request("usb", { filters: [] });
    assert.equal(await manager.open(info.id, { configurationValue: 2, packetSize: 64 }, receiver()), false);
    assert.equal(usb.configuration.configurationValue, 2);
    assert.equal(usb.claimed, 3);
    await manager.write(info.id, Uint8Array.of(1, 2, 3, 4, 5));
    assert.deepEqual(usb.sent, [1, 2, 3, 4, 5]);
    await manager.dispose();
    assert.equal(usb.closed, true);
});

test("USB: configuração incompatível falha e fecha o dispositivo aberto parcialmente", async () => {
    const usb = usbDevice(), manager = createManager();
    navigatorWith({ usb: { requestDevice: async () => usb } });
    const info = await manager.request("usb", {});
    await assert.rejects(() => manager.open(info.id,
        { configurationValue: 1, packetSize: 64, outEndpointNumber: 99 }, receiver()), /incompatíveis/);
    assert.equal(usb.opened, false);
    await manager.dispose();
});

test("USB: fechamento cancela leitura IN pendente sem esperar por timeout de interop", async () => {
    const usb = usbDevice(), read = deferred(), manager = createManager();
    const configure = usb.selectConfiguration.bind(usb);
    usb.selectConfiguration = async value => {
        await configure(value);
        usb.configuration.interfaces[0].alternate.endpoints.push(
            { endpointNumber: 1, direction: "in", type: "bulk" });
    };
    usb.transferIn = () => read.promise;
    usb.close = async () => { usb.opened = false; read.reject(new Error("closed")); };
    navigatorWith({ usb: { requestDevice: async () => usb } });
    const info = await manager.request("usb", {});
    await manager.open(info.id, { configurationValue: 1, packetSize: 64 }, receiver());
    await manager.close(info.id);
    assert.equal(usb.opened, false);
    await manager.dispose();
});

function bluetoothDevice() {
    const write = { properties: { write: true, writeWithoutResponse: true }, chunks: [],
        async writeValueWithResponse(data) { this.chunks.push([...data]); },
        async writeValueWithoutResponse(data) { this.chunks.push([...data]); }
    };
    const notify = new EventTarget();
    notify.startNotifications = async () => { notify.started = true; };
    notify.stopNotifications = async () => { notify.stopped = true; };
    const device = new EventTarget();
    device.name = "BLE test";
    device.gatt = {
        connected: false,
        async connect() { this.connected = true; return this; },
        async getPrimaryService(uuid) {
            assert.equal(uuid, "service");
            return { getCharacteristic: async uuid => uuid === "write" ? write : notify };
        },
        disconnect() { this.connected = false; }
    };
    return { device, write, notify };
}

test("BLE: valida filtros, fragmenta escrita GATT e recebe DataView com offset", async () => {
    const { device, write, notify } = bluetoothDevice(), manager = createManager(), sink = receiver();
    navigatorWith({ bluetooth: { requestDevice: async options => {
        assert.deepEqual(options, { acceptAllDevices: true, optionalServices: ["service"] });
        return device;
    } } });
    const info = await manager.request("bluetooth",
        { acceptAllDevices: true, filters: [], optionalServices: ["service"] });
    assert.equal(await manager.open(info.id, {
        serviceUuid: "service", writeCharacteristicUuid: "write",
        notificationCharacteristicUuid: "notify", writeWithResponse: false, packetSize: 20
    }, sink), true);
    await manager.write(info.id, new Uint8Array(45).fill(8));
    assert.deepEqual(write.chunks.map(x => x.length), [20, 20, 5]);
    const received = deferred(), invoke = sink.invokeMethodAsync.bind(sink);
    sink.invokeMethodAsync = async (...args) => { await invoke(...args); received.resolve(); };
    notify.value = new DataView(Uint8Array.of(99, 1, 2, 99).buffer, 1, 2);
    notify.dispatchEvent(new Event("characteristicvaluechanged"));
    await received.promise;
    assert.deepEqual(sink.data, [1, 2]);
    await manager.close(info.id);
    assert.equal(notify.stopped, true);
    assert.equal(device.gatt.connected, false);
    await manager.dispose();
});

test("BLE: erro ao habilitar notificações desconecta e erro de escrita é propagado", async () => {
    const { device, notify } = bluetoothDevice(), manager = createManager();
    notify.startNotifications = async () => { throw new Error("notify failed"); };
    navigatorWith({ bluetooth: { requestDevice: async () => device } });
    const info = await manager.request("bluetooth", {});
    await assert.rejects(() => manager.open(info.id, {
        serviceUuid: "service", writeCharacteristicUuid: "write",
        notificationCharacteristicUuid: "notify", writeWithResponse: true, packetSize: 20
    }, receiver()), /notify failed/);
    assert.equal(device.gatt.connected, false);
    await assert.rejects(() => manager.write(info.id, Uint8Array.of(1)), /fechado/);
    await manager.dispose();
});

test("APIs ausentes/contexto inseguro falham com mensagem e lista só dispositivos autorizados", async () => {
    navigatorWith({});
    const manager = createManager();
    assert.equal(manager.support().usb, false);
    assert.throws(() => manager.request("usb", {}), /não está disponível/);
    globalThis.isSecureContext = false;
    assert.throws(() => manager.request("usb", {}), /HTTPS/);
    globalThis.isSecureContext = true;
    const port = new Port();
    navigatorWith({ serial: { getPorts: async () => [port], requestPort: () => { throw new Error("unexpected"); } } });
    const authorized = await manager.getDevices("serial");
    assert.equal(authorized.length, 1);
    assert.equal(authorized[0].vendorId, 123);
    assert.deepEqual(await manager.getDevices("serial"), authorized);
    await manager.dispose();
});
