namespace OpenAC.Net.Devices.Blazor;

public sealed class WebBluetoothRequestOptions
{
    public WebBluetoothFilter[] Filters { get; set; } = [];
    public bool AcceptAllDevices { get; set; }
    public string[] OptionalServices { get; set; } = [];
}
