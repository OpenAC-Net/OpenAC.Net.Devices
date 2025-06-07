using OpenAC.Net.Devices.Blazor.Extensions;
using SpawnDev.BlazorJS.JSObjects;

namespace OpenAC.Net.Devices.Blazor.WebSerial;

public sealed class WebWriteSerialStream(WritableStream writable) : Stream
{
    #region Fields

    private readonly MemoryStream writeStream = new();

    #endregion Fields

    #region Properties
    
    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => writeStream.Length;

    public override long Position
    {
        get => writeStream.Position;
        set => writeStream.Position = value;
    }

    #endregion Properties
    
    #region Methods

    public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

    public override void SetLength(long value) => throw new NotImplementedException();

    public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

    public override void Write(byte[] buffer, int offset, int count) => writeStream.Write(buffer, offset, count);

    public override void Flush()
    {
        try
        {
            var buffer = writeStream.ToArrayBuffer();
            writable.GetWriter().Write(buffer).ConfigureAwait(false).GetAwaiter().GetResult();
            writeStream.Clear();
        }
        catch (Exception e)
        {
            throw new IOException("Erro ao enviar dados", e);
        }
    }
    
    protected override void Dispose(bool disposing)
    {
        if (!disposing) return;

        Flush();
        writeStream.Dispose();
    }

    #endregion Methods
}