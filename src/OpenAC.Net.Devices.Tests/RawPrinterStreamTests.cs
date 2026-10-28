namespace OpenAC.Net.Devices.Tests;

public class RawPrinterStreamTests
{
    [Test]
    public async Task Flush_ComBufferVazio_NaoDeveEnviarTrabalhoParaImpressora()
    {
        using var stream = new RawPrinterStream("IMPRESSORA_INEXISTENTE_12345");

        await Assert.That(() => stream.Flush()).ThrowsNothing();
        await Assert.That(stream.Length).IsEqualTo(0);
    }

    [Test]
    public async Task OpenEClose_ComControlePortaTrueESemDados_NaoDeveTentarImprimir()
    {
        var config = new RawConfig("IMPRESSORA_INEXISTENTE_12345")
        {
            ControlePorta = true
        };

        using var device = OpenDeviceFactory.Create(config);
        device.Open();
        await Assert.That(device.Conectado).IsTrue();

        device.Close();
        await Assert.That(device.Conectado).IsFalse();
    }
}
