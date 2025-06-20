using OpenAC.Net.Core.Logging;
using SpawnDev.BlazorJS.JSObjects;

namespace OpenAC.Net.Devices.Blazor.WebSerial;

/// <summary>
/// Implementa um stream de comunicação serial via WebSerial para uso em aplicações Blazor.
/// </summary>
public class OpenWebSerialStream(WebSerialConfig config) : OpenDeviceStream<WebSerialConfig>(config)
{
    /// <summary>
    /// Obtém a quantidade de bytes disponíveis para leitura no buffer.
    /// </summary>
    protected override int Available => (int)((Reader?.BaseStream.Length ?? 0) - (Reader?.BaseStream.Position ?? 0));

    /// <summary>
    /// Limpa o buffer de leitura da porta serial.
    /// </summary>
    /// <exception cref="InvalidOperationException">Lançada se a porta serial não estiver configurada.</exception>
    public override void Limpar()
    {
        if(Config.Port == null)
            throw new InvalidOperationException("Porta serial não configurada.");
        
        if (!Config.Port.Connected) return;
        
        Reader?.BaseStream.Flush();
    }

    /// <summary>
    /// Abre a porta serial com as configurações especificadas.
    /// </summary>
    /// <returns>Verdadeiro se a porta foi aberta com sucesso, falso caso contrário.</returns>
    /// <exception cref="InvalidOperationException">Lançada se a porta serial não estiver configurada.</exception>
    protected override bool OpenInternal()
    {
        if(Config.Port == null)
            throw new InvalidOperationException("Porta serial não configurada.");
        
        try
        {
            Config.Port.Open(new SerialOptions
            {
                BaudRate = Config.BaudRate,
                BufferSize = Config.BufferSize,
                DataBits = Config.DataBits,
                FlowControl = Config.FlowControl,
                Parity = Config.Parity,
                StopBits = Config.StopBits
            }).ConfigureAwait(false).GetAwaiter().GetResult();
        
            Writer = new BinaryWriter(new WebWriteSerialStream(Config.Port.Writable));
            // Reader provavelmente não ta funcional
            Reader = new BinaryReader(new WebSerialReadStream(Config.Port.Readable));
            
            return true;
        }
        catch (Exception e)
        {
            this.Log().Error("Erro ao abrir a porta serial", e);
            return false;
        }
    }

    /// <summary>
    /// Fecha a porta serial e libera os recursos associados.
    /// </summary>
    /// <returns>Verdadeiro se a porta foi fechada com sucesso.</returns>
    /// <exception cref="InvalidOperationException">Lançada se a porta serial não estiver configurada.</exception>
    protected override bool CloseInternal()
    {
        if(Config.Port == null)
            throw new InvalidOperationException("Porta serial não configurada.");
        
        Config.Port.Close().ConfigureAwait(false).GetAwaiter().GetResult();
        Writer?.Dispose();
        Reader?.Dispose();
        
        Reader = null;
        Writer = null;
        return true;
    }
}