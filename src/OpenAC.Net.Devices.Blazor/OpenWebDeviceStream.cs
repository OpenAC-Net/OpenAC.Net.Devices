namespace OpenAC.Net.Devices.Blazor;

/// <summary>Comunicação assíncrona com o navegador em Server e WebAssembly.</summary>
public abstract class OpenWebDeviceStream<TConfig>(TConfig config) : OpenDeviceStream<TConfig>(config), IAsyncDisposable
    where TConfig : WebDeviceConfig
{
    private WebDevice? activeDevice;
    private bool disposed;
    public override void Open() => throw AsyncRequired();
    public override void Close() => throw AsyncRequired();
    public override void Write(byte[] dados) => throw AsyncRequired();
    public override byte[] Read() => throw AsyncRequired();
    public override byte[] WriteRead(byte[] dados, int timeSleep = 10) => throw AsyncRequired();

    public override Task WriteAsync(byte[] dados, CancellationToken cancellationToken = default)
    {
        EnsureOpen(cancellationToken);
        return base.WriteAsync(dados, cancellationToken);
    }

    public override Task<byte[]> ReadAsync(CancellationToken cancellationToken = default)
    {
        EnsureOpen(cancellationToken);
        return base.ReadAsync(cancellationToken);
    }

    public override Task<byte[]> WriteReadAsync(byte[] dados, int timeSleep = 10, CancellationToken cancellationToken = default)
    {
        EnsureOpen(cancellationToken);
        return base.WriteReadAsync(dados, timeSleep, cancellationToken);
    }

    private static NotSupportedException AsyncRequired() =>
        new("Use OpenAsync, CloseAsync, WriteAsync, ReadAsync e WriteReadAsync no Blazor.");

    private void EnsureOpen(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed || IsDisposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        if (!Config.ControlePorta && activeDevice == null)
            throw new InvalidOperationException("Abra o dispositivo com OpenAsync antes de comunicar.");
    }
    protected override int Available => (int)(Reader?.BaseStream.Length ?? 0);
    public override void Limpar() => Reader?.BaseStream.Flush();

    protected override bool OpenInternal() =>
        throw new NotSupportedException("Use OpenAsync, WriteAsync, ReadAsync e WriteReadAsync no Blazor.");

    protected override bool CloseInternal() =>
        throw new NotSupportedException("Use CloseAsync ou DisposeAsync no Blazor.");

    protected override async Task<bool> OpenInternalAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed || IsDisposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        if (activeDevice != null) return true;
        var device = Config.Device ?? throw new InvalidOperationException("Selecione um dispositivo no navegador.");
        if (device.Info.Kind != Config.Kind)
            throw new InvalidOperationException($"O dispositivo selecionado não é do tipo {Config.Kind}.");
        var receiver = new WebDeviceReadStream { OnFailure = () => Conectado = false };
        try
        {
            var canRead = await device.OpenAsync(Config.GetOpenOptions(), receiver, cancellationToken);
            activeDevice = device;
            if (canRead) Reader = new BinaryReader(receiver);
            else receiver.Dispose();
            Writer = new BinaryWriter(new WebDeviceWriteStream(device));
            return true;
        }
        catch
        {
            receiver.Dispose();
            throw;
        }
    }

    protected override async Task<bool> CloseInternalAsync(CancellationToken cancellationToken = default)
    {
        if (activeDevice == null) return true;
        try
        {
            // A limpeza também ocorre no finally de uma operação cancelada.
            await activeDevice.CloseAsync(CancellationToken.None);
        }
        finally
        {
            activeDevice = null;
            Reader?.Dispose();
            Writer?.Dispose();
            Reader = null;
            Writer = null;
            Conectado = false;
        }

        return true;
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        try
        {
            await CloseInternalAsync();
        }
        catch (Microsoft.JSInterop.JSDisconnectedException)
        {
        }
        finally
        {
            disposed = true;
            Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
