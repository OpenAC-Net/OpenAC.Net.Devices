namespace OpenAC.Net.Devices.Blazor;

public sealed class WebSerialFilter
{
    public int? UsbVendorId { get; set; }
    public int? UsbProductId { get; set; }
    public string? BluetoothServiceClassId { get; set; }
}
