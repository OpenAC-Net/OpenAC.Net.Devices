namespace OpenAC.Net.Devices.Blazor.WebUsb;

/// <summary>Configura interfaces e endpoints bulk de um dispositivo WebUSB.</summary>
public class WebUsbConfig : WebDeviceConfig
{
    static WebUsbConfig() => OpenDeviceFactory.Register<WebUsbConfig, OpenWebUsbStream>();
    public WebUsbConfig() : base("WebUsb") { }
    public WebUsbConfig(WebDevice device) : this() => Device = device;
    public int ConfigurationValue { get; set; } = 1;
    public int? InterfaceNumber { get; set; }
    public int? AlternateSetting { get; set; }
    public int? OutEndpointNumber { get; set; }
    public int? InEndpointNumber { get; set; }
    public int PacketSize { get; set; } = 64;
    internal override string Kind => "usb";
    internal override object GetOpenOptions() => new
    {
        configurationValue = ConfigurationValue, interfaceNumber = InterfaceNumber,
        alternateSetting = AlternateSetting, outEndpointNumber = OutEndpointNumber,
        inEndpointNumber = InEndpointNumber, packetSize = PacketSize
    };
}
