using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace OpenAC.Net.Devices.Blazor;

/// <summary>Associação entre um botão e o seletor nativo; descarte ao remover o componente.</summary>
public sealed class WebDeviceButtonBinding : IAsyncDisposable
{
    private readonly Func<WebDeviceInfo, Task> onSelected;
    private readonly Func<string, Task>? onError;
    private readonly DotNetObjectReference<WebDeviceButtonBinding> reference;
    private IJSObjectReference? binding;
    private bool disposed;

    internal WebDeviceButtonBinding(Func<WebDeviceInfo, Task> onSelected, Func<string, Task>? onError)
    {
        this.onSelected = onSelected;
        this.onError = onError;
        reference = DotNetObjectReference.Create(this);
    }

    internal async Task InitializeAsync(IJSObjectReference manager, ElementReference button, string kind, object options) =>
        binding = await manager.InvokeAsync<IJSObjectReference>("bind", button, kind, options, reference);

    [JSInvokable]
    public Task Selected(WebDeviceInfo info) => disposed ? Task.CompletedTask : onSelected(info);
    [JSInvokable]
    public Task Failed(string message) => disposed ? Task.CompletedTask : onError?.Invoke(message) ?? Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            if (binding != null) await binding.InvokeVoidAsync("dispose");
        }
        catch (JSDisconnectedException) { }
        finally
        {
            reference.Dispose();
            if (binding != null)
            {
                try { await binding.DisposeAsync(); }
                catch (JSDisconnectedException) { }
            }
        }
    }
}
