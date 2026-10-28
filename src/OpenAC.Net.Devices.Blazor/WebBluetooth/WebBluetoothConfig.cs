namespace OpenAC.Net.Devices.Blazor.WebBluetooth;

/// <summary>Comunicação BLE/GATT, diferente de Bluetooth clássico RFCOMM/SPP.</summary>
public class WebBluetoothConfig : WebDeviceConfig
{
    static WebBluetoothConfig() => OpenDeviceFactory.Register<WebBluetoothConfig, OpenWebBluetoothStream>();
    public WebBluetoothConfig() : base("WebBluetooth") { }
    public WebBluetoothConfig(WebDevice device) : this() => Device = device;
    public string ServiceUuid { get; set; } = "";
    public string WriteCharacteristicUuid { get; set; } = "";
    public string? NotificationCharacteristicUuid { get; set; }
    public bool WriteWithResponse { get; set; } = true;
    /// <summary>Limite por escrita GATT. Ajuste conforme o dispositivo.</summary>
    public int PacketSize { get; set; } = 20;
    internal override string Kind => "bluetooth";
    internal override object GetOpenOptions()
    {
        if (string.IsNullOrWhiteSpace(ServiceUuid) || string.IsNullOrWhiteSpace(WriteCharacteristicUuid))
            throw new InvalidOperationException("Informe o serviço e a característica GATT de escrita.");
        if (PacketSize <= 0 || PacketSize > 512)
            throw new ArgumentOutOfRangeException(nameof(PacketSize), "Use de 1 a 512 bytes por escrita GATT.");
        return new
        {
            serviceUuid = ServiceUuid, writeCharacteristicUuid = WriteCharacteristicUuid,
            notificationCharacteristicUuid = NotificationCharacteristicUuid,
            writeWithResponse = WriteWithResponse, packetSize = PacketSize
        };
    }
}
