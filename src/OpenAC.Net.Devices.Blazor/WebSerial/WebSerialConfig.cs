using SpawnDev.BlazorJS.JSObjects;

namespace OpenAC.Net.Devices.Blazor.WebSerial;

public class WebSerialConfig() : BaseConfig("WebSerial")
{
    public SerialPort? Port { get; set; }
    
    public int BaudRate { get; set; } = 9600;
    
    public int? BufferSize { get; set; }
    
    public int? DataBits { get; set; }

    public FlowControlType? FlowControl { get; set; }
    
    public ParityType? Parity { get; set; }
    
    public int? StopBits { get; set; }
}