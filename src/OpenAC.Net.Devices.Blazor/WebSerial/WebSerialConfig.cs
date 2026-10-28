namespace OpenAC.Net.Devices.Blazor.WebSerial;

/// <summary>Configura uma porta Web Serial selecionada por WebDeviceService.</summary>
public class WebSerialConfig : WebDeviceConfig
{
    static WebSerialConfig() => OpenDeviceFactory.Register<WebSerialConfig, OpenWebSerialStream>();
    public WebSerialConfig() : base("WebSerial") { }
    public WebSerialConfig(WebDevice port) : this() => Port = port;
    public WebDevice? Port { get => Device; set => Device = value; }
    public int BaudRate { get; set; } = 9600;
    public int? BufferSize { get; set; }
    public int? DataBits { get; set; }
    public FlowControlType? FlowControl { get; set; }
    public ParityType? Parity { get; set; }
    public int? StopBits { get; set; }
    internal override string Kind => "serial";
    internal override object GetOpenOptions() => new
    {
        baudRate = BaudRate, bufferSize = BufferSize, dataBits = DataBits,
        flowControl = FlowControl?.ToString().ToLowerInvariant(),
        parity = Parity?.ToString().ToLowerInvariant(), stopBits = StopBits
    };
}
