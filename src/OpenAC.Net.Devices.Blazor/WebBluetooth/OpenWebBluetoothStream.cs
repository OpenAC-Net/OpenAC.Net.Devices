namespace OpenAC.Net.Devices.Blazor.WebBluetooth;

/// <summary>Stream BLE/GATT com escrita e notificações opcionais para leitura.</summary>
public class OpenWebBluetoothStream(WebBluetoothConfig config) : OpenWebDeviceStream<WebBluetoothConfig>(config);
