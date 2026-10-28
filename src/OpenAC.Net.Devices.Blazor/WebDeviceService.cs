using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace OpenAC.Net.Devices.Blazor;

/// <summary>Seleção e descoberta de dispositivos no navegador. Registre com AddWebDevices.</summary>
public sealed class WebDeviceService(IJSRuntime js) : IAsyncDisposable
{
    private readonly SemaphoreSlim initialization = new(1, 1);
    private readonly Dictionary<int, WebDevice> devices = new();
    private IJSObjectReference? module;
    private IJSObjectReference? manager;
    private bool disposed;

    private async Task<IJSObjectReference> GetManagerAsync()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await initialization.WaitAsync();
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            module ??= await js.InvokeAsync<IJSObjectReference>("import",
                "./_content/OpenAC.Net.Devices.Blazor/openac-devices.js");
            manager ??= await module.InvokeAsync<IJSObjectReference>("createManager");
            return manager;
        }
        finally { initialization.Release(); }
    }

    private WebDevice Wrap(IJSObjectReference owner, WebDeviceInfo info)
    {
        lock (devices)
        {
            if (!devices.TryGetValue(info.Id, out var device))
                devices[info.Id] = device = new WebDevice(owner, info);
            return device;
        }
    }

    public async Task<WebDeviceSupport> GetSupportAsync() =>
        await (await GetManagerAsync()).InvokeAsync<WebDeviceSupport>("support");

    /// <summary>Solicita permissão. Em Server, prefira BindSerialButtonAsync para preservar o gesto.</summary>
    public Task<WebDevice> RequestSerialPortAsync(WebSerialRequestOptions? options = null) =>
        RequestAsync("serial", options ?? new WebSerialRequestOptions());

    public Task<WebDevice> RequestUsbDeviceAsync(WebUsbRequestOptions? options = null) =>
        RequestAsync("usb", options ?? new WebUsbRequestOptions());

    public Task<WebDevice> RequestBluetoothDeviceAsync(WebBluetoothRequestOptions options) =>
        RequestAsync("bluetooth", options);

    private async Task<WebDevice> RequestAsync(string kind, object options)
    {
        var owner = await GetManagerAsync();
        return Wrap(owner, await owner.InvokeAsync<WebDeviceInfo>("request", kind, options));
    }

    /// <summary>Lista somente dispositivos já autorizados; não abre o seletor.</summary>
    public async Task<WebDevice[]> GetAuthorizedDevicesAsync(string kind)
    {
        var owner = await GetManagerAsync();
        var infos = await owner.InvokeAsync<WebDeviceInfo[]>("getDevices", kind);
        return infos.Select(info => Wrap(owner, info)).ToArray();
    }

    public Task<WebDeviceButtonBinding> BindSerialButtonAsync(ElementReference button,
        Func<WebDevice, Task> onSelected, Func<string, Task>? onError = null, WebSerialRequestOptions? options = null) =>
        BindAsync(button, "serial", options ?? new WebSerialRequestOptions(), onSelected, onError);

    public Task<WebDeviceButtonBinding> BindUsbButtonAsync(ElementReference button,
        Func<WebDevice, Task> onSelected, Func<string, Task>? onError = null, WebUsbRequestOptions? options = null) =>
        BindAsync(button, "usb", options ?? new WebUsbRequestOptions(), onSelected, onError);

    public Task<WebDeviceButtonBinding> BindBluetoothButtonAsync(ElementReference button,
        WebBluetoothRequestOptions options, Func<WebDevice, Task> onSelected, Func<string, Task>? onError = null) =>
        BindAsync(button, "bluetooth", options, onSelected, onError);

    private async Task<WebDeviceButtonBinding> BindAsync(ElementReference button, string kind, object options,
        Func<WebDevice, Task> onSelected, Func<string, Task>? onError)
    {
        ArgumentNullException.ThrowIfNull(onSelected);
        var owner = await GetManagerAsync();
        var binding = new WebDeviceButtonBinding(info => onSelected(Wrap(owner, info)), onError);
        try
        {
            await binding.InitializeAsync(owner, button, kind, options);
            return binding;
        }
        catch { await binding.DisposeAsync(); throw; }
    }

    public async ValueTask DisposeAsync()
    {
        await initialization.WaitAsync();
        try
        {
            if (disposed) return;
            disposed = true;
            WebDevice[] selected;
            lock (devices) selected = devices.Values.ToArray();
            var errors = new List<Exception>();
            async Task Cleanup(Func<ValueTask> action)
            {
                try { await action(); }
                catch (JSDisconnectedException) { }
                catch (Exception error) { errors.Add(error); }
            }

            foreach (var device in selected) await Cleanup(device.DisposeAsync);
            if (manager != null) await Cleanup(() => manager.InvokeVoidAsync("dispose"));
            if (manager != null) await Cleanup(manager.DisposeAsync);
            if (module != null) await Cleanup(module.DisposeAsync);
            lock (devices) devices.Clear();
            if (errors.Count > 0) throw new AggregateException("Erro ao liberar dispositivos do navegador.", errors);
        }
        finally { initialization.Release(); }
    }
}
