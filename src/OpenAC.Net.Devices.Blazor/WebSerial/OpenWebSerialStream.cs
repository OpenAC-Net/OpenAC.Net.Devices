namespace OpenAC.Net.Devices.Blazor.WebSerial;

/// <summary>Stream Web Serial. Use os métodos assíncronos de comunicação.</summary>
public class OpenWebSerialStream(WebSerialConfig config) : OpenWebDeviceStream<WebSerialConfig>(config);
