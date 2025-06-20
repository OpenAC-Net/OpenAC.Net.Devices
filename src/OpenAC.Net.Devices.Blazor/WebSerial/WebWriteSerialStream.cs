using OpenAC.Net.Devices.Blazor.Extensions;
using SpawnDev.BlazorJS.JSObjects;

namespace OpenAC.Net.Devices.Blazor.WebSerial;

/// <summary>
/// Stream de escrita para comunicação serial via WebSerial em Blazor.
/// </summary>
public sealed class WebWriteSerialStream(WritableStream writable) : Stream
{
    #region Fields

    /// <summary>
    /// Buffer interno para escrita dos dados.
    /// </summary>
    private readonly MemoryStream writeStream = new();

    #endregion Fields

    #region Properties

    /// <inheritdoc/>
    public override bool CanRead => false;

    /// <inheritdoc/>
    public override bool CanSeek => false;

    /// <inheritdoc/>
    public override bool CanWrite => true;

    /// <inheritdoc/>
    public override long Length => writeStream.Length;

    /// <inheritdoc/>
    public override long Position
    {
        get => writeStream.Position;
        set => writeStream.Position = value;
    }

    #endregion Properties

    #region Methods

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => writeStream.Write(buffer, offset, count);

    /// <summary>
    /// Envia os dados do buffer interno para o <see cref="WritableStream"/>.
    /// </summary>
    /// <exception cref="IOException">Erro ao enviar dados.</exception>
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

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (!disposing) return;

        Flush();
        writeStream.Dispose();
    }

    #endregion Methods
}