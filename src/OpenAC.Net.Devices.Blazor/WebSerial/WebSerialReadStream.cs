using OpenAC.Net.Devices.Blazor.Extensions;
using SpawnDev.BlazorJS.JSObjects;

namespace OpenAC.Net.Devices.Blazor.WebSerial;

/// <summary>
/// Implementa um <see cref="Stream"/> de leitura baseado em <see cref="ReadableStream"/> para WebSerial.
/// </summary>
public sealed class WebSerialReadStream : Stream
{
    #region Fields

    /// <summary>
    /// Stream interno para armazenamento dos dados lidos.
    /// </summary>
    private readonly MemoryStream readStream = new();

    /// <summary>
    /// Stream legível de onde os dados são recebidos.
    /// </summary>
    private readonly ReadableStream readableStream;

    /// <summary>
    /// Token de cancelamento para controle da leitura assíncrona.
    /// </summary>
    private readonly CancellationTokenSource cts = new();

    #endregion Fields

    #region Constructors

    /// <summary>
    /// Inicializa uma nova instância de <see cref="WebSerialReadStream"/>.
    /// </summary>
    /// <param name="readable">O <see cref="ReadableStream"/> de origem.</param>
    public WebSerialReadStream(ReadableStream readable)
    {
        readableStream = readable;
        ReadStream();
    }

    #endregion Constructors
    
    #region Properties

    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanSeek => true;

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override long Length => readStream.Length;

    /// <inheritdoc/>
    public override long Position
    {
        get => readStream.Position;
        set => readStream.Position = value;
    }

    #endregion Properties
    
    #region Methods

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => readStream.Seek(offset, origin);

    /// <inheritdoc/>
    public override void SetLength(long value) => readStream.SetLength(value);

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) => readStream.Read(buffer, offset, count);

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void Flush() => readStream.Clear();

    /// <summary>
    /// Inicia a leitura assíncrona do <see cref="ReadableStream"/> e armazena os dados no <see cref="readStream"/>.
    /// </summary>
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

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (!disposing) return;

        cts.Cancel();
        cts.Dispose();
        readStream.Dispose();
    }

    #endregion Methods
}