using Microsoft.JSInterop;

namespace OpenAC.Net.Devices.Blazor;

/// <summary>Dispositivo autorizado no navegador. Use com apenas um stream por vez.</summary>
public sealed class WebDevice : IAsyncDisposable
{
    private readonly IJSObjectReference manager;
    private DotNetObjectReference<WebDeviceReadStream>? receiverReference;
    private bool disposed;

    internal WebDevice(IJSObjectReference manager, WebDeviceInfo info)
    {
        this.manager = manager;
        Info = info;
    }

    public WebDeviceInfo Info { get; }

    internal async Task<bool> OpenAsync(object options, WebDeviceReadStream receiver,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (receiverReference != null)
            throw new InvalidOperationException("O dispositivo já está aberto em outro stream.");
        cancellationToken.ThrowIfCancellationRequested();
        receiverReference = DotNetObjectReference.Create(receiver);
        try
        {
            return await manager.InvokeAsync<bool>("open", cancellationToken, Info.Id, options, receiverReference);
        }
        catch
        {
            await CloseAsync(CancellationToken.None);
            throw;
        }
    }

    internal async Task WriteAsync(byte[] data, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (receiverReference == null)
            throw new InvalidOperationException("Abra o dispositivo com OpenAsync antes de escrever.");
        // Mantém as mensagens abaixo do limite padrão de 32 KiB do SignalR.
        for (var offset = 0; offset < data.Length; offset += 8192)
            await manager.InvokeVoidAsync("write", cancellationToken, Info.Id,
                data.AsSpan(offset, Math.Min(8192, data.Length - offset)).ToArray());
    }

    internal async Task CloseAsync(CancellationToken cancellationToken)
    {
        if (receiverReference == null) return;
        try
        {
            await manager.InvokeVoidAsync("close", cancellationToken, Info.Id);
        }
        finally
        {
            receiverReference.Dispose();
            receiverReference = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        try
        {
            await CloseAsync(CancellationToken.None);
            await manager.InvokeVoidAsync("release", Info.Id);
        }
        catch (JSDisconnectedException)
        {
            // A página perdeu o circuito com o servidor.
        }
        finally
        {
            disposed = true;
        }
    }
}
