using OpenAC.Net.Devices.Blazor.Extensions;
using SpawnDev.BlazorJS.JSObjects;

namespace OpenAC.Net.Devices.Blazor.WebSerial;

public sealed class WebSerialReadStream : Stream
{
    #region Fields

    private readonly MemoryStream readStream = new();
    private readonly ReadableStream readableStream;
    private readonly CancellationTokenSource cts = new();

    #endregion Fields

    #region Constructors

    public WebSerialReadStream(ReadableStream readable)
    {
        readableStream = readable;
        ReadStream();
    }

    #endregion Constructors
    
    #region Properties
    
    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => readStream.Length;

    public override long Position
    {
        get => readStream.Position;
        set => readStream.Position = value;
    }

    #endregion Properties
    
    #region Methods

    public override long Seek(long offset, SeekOrigin origin) => readStream.Seek(offset, origin);

    public override void SetLength(long value) => readStream.SetLength(value);

    public override int Read(byte[] buffer, int offset, int count) => readStream.Read(buffer, offset, count);

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override void Flush() => readStream.Clear();
    
    private void ReadStream()
    {
        Task.Run(async () =>
        {
            do
            {
                var dados = await readableStream.GetReader().Read();
                if (dados.Value != null)
                {
                    await readStream.WriteAsync(dados.Value.ToArray());
                    while (!dados.Done && !cts.IsCancellationRequested && dados.Value != null)
                    {
                        dados = await readableStream.GetReader().Read();
                        if (dados.Value != null)
                            await readStream.WriteAsync(dados.Value.ToArray());
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(1));
            } while (!cts.IsCancellationRequested);
            
        }, cts.Token);
    }
    
    protected override void Dispose(bool disposing)
    {
        if (!disposing) return;

        cts.Cancel();
        cts.Dispose();
        readStream.Dispose();
    }

    #endregion Methods
}