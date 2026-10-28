using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace OpenAC.Net.Devices.Blazor;

/// <summary>Buffer local alimentado pelo navegador, sem polling pelo SignalR.</summary>
public sealed class WebDeviceReadStream : Stream
{
    private readonly ConcurrentQueue<byte> queue = new();
    private IOException? error;
    private bool disposed;
    internal Action? OnFailure { get; init; }
    public override bool CanRead => !disposed;
    public override bool CanSeek => false;
    public override bool CanWrite => false;

    public override long Length
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (queue.IsEmpty && error != null) throw error;
            return queue.Count;
        }
    }

    public override long Position
    {
        get => 0;
        set => throw new NotSupportedException();
    }

    [JSInvokable]
    public void Receive(byte[] data)
    {
        if (disposed) return;
        foreach (var value in data) queue.Enqueue(value);
    }

    [JSInvokable]
    public void Fail(string message)
    {
        error = new IOException(message);
        OnFailure?.Invoke();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        ObjectDisposedException.ThrowIf(disposed, this);
        if (count == 0) return 0;
        if (queue.IsEmpty && error != null) throw error;
        var read = 0;
        while (read < count && queue.TryDequeue(out var value)) buffer[offset + read++] = value;
        return read;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Read(buffer, offset, count));
    }

    public override void Flush() => queue.Clear();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            disposed = true;
            queue.Clear();
        }

        base.Dispose(disposing);
    }
}
