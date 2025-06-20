using SpawnDev.BlazorJS.JSObjects;

namespace OpenAC.Net.Devices.Blazor.WebSerial;

/// <summary>
/// Representa a configuração para comunicação via WebSerial.
/// </summary>
public class WebSerialConfig() : BaseConfig("WebSerial")
{
    /// <summary>
    /// Porta serial selecionada.
    /// </summary>
    public SerialPort? Port { get; set; }
    
    /// <summary>
    /// Taxa de transmissão em bauds. Padrão: 9600.
    /// </summary>
    public int BaudRate { get; set; } = 9600;
    
    /// <summary>
    /// Tamanho do buffer de leitura/escrita.
    /// </summary>
    public int? BufferSize { get; set; }
    
    /// <summary>
    /// Número de bits de dados.
    /// </summary>
    public int? DataBits { get; set; }

    /// <summary>
    /// Tipo de controle de fluxo.
    /// </summary>
    public FlowControlType? FlowControl { get; set; }
    
    /// <summary>
    /// Tipo de paridade.
    /// </summary>
    public ParityType? Parity { get; set; }
    
    /// <summary>
    /// Número de bits de parada.
    /// </summary>
    public int? StopBits { get; set; }
}