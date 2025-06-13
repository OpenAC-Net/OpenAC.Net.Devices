using OpenAC.Net.Core.Logging;
using SpawnDev.BlazorJS.JSObjects;

namespace OpenAC.Net.Devices.Blazor.WebSerial;

public class OpenWebSerialStream(WebSerialConfig config) : OpenDeviceStream<WebSerialConfig>(config)
{
    protected override int Available => (int)((Reader?.BaseStream.Length ?? 0) - (Reader?.BaseStream.Position ?? 0));

    public override void Limpar()
    {
        if(Config.Port == null)
            throw new InvalidOperationException("Porta serial não configurada.");
        
        if (!Config.Port.Connected) return;
        
        Reader?.BaseStream.Flush();
    }

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