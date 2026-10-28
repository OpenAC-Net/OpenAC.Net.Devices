using Microsoft.JSInterop;

namespace OpenAC.Net.Devices.Blazor;

internal sealed class WebDeviceWriteStream(WebDevice device) : Stream
{
    private bool disposed;

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => !disposed;

    public override long Length => 0;

    public override long Position { get => 0; set => throw new NotSupportedException(); }

    public override void Flush() { }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("Use WriteAsync para comunicar com o navegador.");

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArguments(buffer, offset, count);
        ObjectDisposedException.ThrowIf(disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await device.WriteAsync(buffer.AsSpan(offset, count).ToArray(), cancellationToken);
        }
        catch (JSException error)
        {
            throw new IOException("Erro ao escrever no dispositivo do navegador.", error);
        }
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing) disposed = true;
        base.Dispose(disposing);
    }
}
