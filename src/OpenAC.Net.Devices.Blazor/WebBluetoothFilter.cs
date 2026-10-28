namespace OpenAC.Net.Devices.Blazor;

public sealed class WebBluetoothFilter
{
    public string? Name { get; set; }
    public string? NamePrefix { get; set; }
    public string[]? Services { get; set; }
}
