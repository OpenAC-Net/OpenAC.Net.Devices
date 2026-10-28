namespace OpenAC.Net.Devices.Blazor.WebUsb;

/// <summary>Stream WebUSB. Use os métodos assíncronos de comunicação.</summary>
public class OpenWebUsbStream(WebUsbConfig config) : OpenWebDeviceStream<WebUsbConfig>(config);
