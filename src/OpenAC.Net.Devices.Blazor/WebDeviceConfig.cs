namespace OpenAC.Net.Devices.Blazor;

/// <summary>Configuração compartilhada pelos transportes do navegador.</summary>
public abstract class WebDeviceConfig : BaseConfig
{
    protected WebDeviceConfig(string name) : base(name) => ControlePorta = false;
    public WebDevice? Device { get; set; }
    internal abstract string Kind { get; }
    internal abstract object GetOpenOptions();
}
