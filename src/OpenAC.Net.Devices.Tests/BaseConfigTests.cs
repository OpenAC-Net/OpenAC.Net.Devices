namespace OpenAC.Net.Devices.Tests;

public class BaseConfigTests
{
    private sealed class DummyConfig() : BaseConfig("Dummy");

    [Test]
    public async Task ValoresPadrao_DevemEstarCorretos()
    {
        var config = new DummyConfig();

        await Assert.That(config.Name).IsEqualTo("Dummy");
        await Assert.That(config.ControlePorta).IsTrue();
        await Assert.That(config.TimeOut).IsEqualTo(3);
        await Assert.That(config.Tentativas).IsEqualTo(3);
        await Assert.That(config.IntervaloTentativas).IsEqualTo(3000);
        await Assert.That(config.ReadBufferSize).IsEqualTo(200);
        await Assert.That(config.WriteBufferSize).IsEqualTo(3000);
    }

    [Test]
    public async Task PropriedadesNumericas_ComValorMenorQueUm_DevemLimitarEmUm()
    {
        var config = new DummyConfig
        {
            TimeOut = 0,
            Tentativas = -5,
            IntervaloTentativas = 0,
            ReadBufferSize = -10,
            WriteBufferSize = 0
        };

        await Assert.That(config.TimeOut).IsEqualTo(1);
        await Assert.That(config.Tentativas).IsEqualTo(1);
        await Assert.That(config.IntervaloTentativas).IsEqualTo(1);
        await Assert.That(config.ReadBufferSize).IsEqualTo(1);
        await Assert.That(config.WriteBufferSize).IsEqualTo(1);
    }

    [Test]
    public async Task AlteracaoDePropriedade_DeveDispararPropertyChangingEPropertyChanged()
    {
        var config = new DummyConfig();
        var changingProps = new List<string?>();
        var changedProps = new List<string?>();

        config.PropertyChanging += (_, e) => changingProps.Add(e.PropertyName);
        config.PropertyChanged += (_, e) => changedProps.Add(e.PropertyName);

        config.TimeOut = 10;
        config.TimeOut = 10; // Mesmo valor, não deve disparar novamente

        await Assert.That(changingProps).Count().IsEqualTo(1);
        await Assert.That(changingProps[0]).IsEqualTo(nameof(BaseConfig.TimeOut));
        await Assert.That(changedProps).Count().IsEqualTo(1);
        await Assert.That(changedProps[0]).IsEqualTo(nameof(BaseConfig.TimeOut));
    }

    [Test]
    public async Task SerialConfig_PortasValidasEInvalidas_DevemSerValidadasCorretamente()
    {
        var serial = new SerialConfig();

        serial.Porta = "COM3";
        await Assert.That(serial.Porta).IsEqualTo("COM3");

        serial.Porta = @"\\.\COM12";
        await Assert.That(serial.Porta).IsEqualTo(@"\\.\COM12");

        serial.Porta = "/dev/ttyUSB0";
        await Assert.That(serial.Porta).IsEqualTo("/dev/ttyUSB0");

        serial.Porta = "/dev/serial/by-id/usb-1234";
        await Assert.That(serial.Porta).IsEqualTo("/dev/serial/by-id/usb-1234");

        await Assert.That(() => serial.Porta = "")
            .ThrowsExactly<ArgumentException>();

        await Assert.That(() => serial.Porta = "PORTA_INEXISTENTE")
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task FileConfig_CreateIfNotExists_DeveManterCompatibilidadeComPropriedadeObsoleta()
    {
        var config = new FileConfig("teste.txt");

#pragma warning disable CS0618
        config.CreateIfNotExits = true;
        await Assert.That(config.CreateIfNotExists).IsTrue();

        config.CreateIfNotExists = false;
        await Assert.That(config.CreateIfNotExits).IsFalse();
#pragma warning restore CS0618
    }
}
