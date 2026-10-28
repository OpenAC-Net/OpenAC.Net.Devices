namespace OpenAC.Net.Devices.Blazor;

/// <summary>Metadados; Id é local à instância do serviço.</summary>
public sealed class WebDeviceInfo
{
    public int Id { get; init; }
    public string Kind { get; init; } = "";
    public string Name { get; init; } = "";
    public int? VendorId { get; init; }
    public int? ProductId { get; init; }
}
