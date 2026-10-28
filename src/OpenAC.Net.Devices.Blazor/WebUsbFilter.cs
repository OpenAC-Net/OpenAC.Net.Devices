namespace OpenAC.Net.Devices.Blazor;

public sealed class WebUsbFilter
{
    public int? VendorId { get; set; }
    public int? ProductId { get; set; }
    public int? ClassCode { get; set; }
    public int? SubclassCode { get; set; }
    public int? ProtocolCode { get; set; }
    public string? SerialNumber { get; set; }
}
