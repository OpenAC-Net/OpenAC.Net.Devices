using OpenAC.Net.Core;
using OpenAC.Net.Devices.Blazor.WebSerial;

namespace OpenAC.Net.Devices.Tests;

public class OpenDeviceFactoryTests
{
    private sealed class DerivedSerialConfig : SerialConfig;

    private sealed class CustomTestConfig() : BaseConfig("CustomTest");

    private sealed class UnregisteredConfig() : BaseConfig("Unregistered");

    private sealed class CustomTestStream(CustomTestConfig config) : OpenDeviceStream<CustomTestConfig>(config)
    {
        protected override int Available => 0;
        protected override bool OpenInternal() => true;
        protected override bool CloseInternal() => true;
    }

    [Test]
    public async Task Create_ComConfigNula_DeveLancarArgumentNullException()
    {
        await Assert.That(() => OpenDeviceFactory.Create(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Create_ComConfigNaoRegistrada_DeveLancarOpenException()
    {
        var config = new UnregisteredConfig();

        await Assert.That(() => OpenDeviceFactory.Create(config))
            .ThrowsExactly<OpenException>();
    }

    [Test]
    public async Task Create_ComConfigsPadrao_DeveCriarStreamsCorrespondentes()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            using var serialStream = OpenDeviceFactory.Create(new SerialConfig());
            using var tcpStream = OpenDeviceFactory.Create(new TCPConfig("localhost", 9100));
            using var rawStream = OpenDeviceFactory.Create(new RawConfig("Printer"));
            using var fileStream = OpenDeviceFactory.Create(new FileConfig(tempFile));
            using var webSerialStream = OpenDeviceFactory.Create(new WebSerialConfig());

            await Assert.That(serialStream).IsNotNull();
            await Assert.That(tcpStream).IsNotNull();
            await Assert.That(rawStream).IsNotNull();
            await Assert.That(fileStream).IsNotNull();
            await Assert.That(webSerialStream).IsTypeOf<OpenWebSerialStream>();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Test]
    public async Task Create_ComSubclasseDeConfigRegistrada_DeveResolverPelaClasseBase()
    {
        var derivedConfig = new DerivedSerialConfig();
        using var stream = OpenDeviceFactory.Create(derivedConfig);

        await Assert.That(stream).IsNotNull();
        await Assert.That(stream.Config).IsSameReferenceAs(derivedConfig);
    }

    [Test]
    public async Task Register_ChamadoMultiplasVezes_DeveSerIdempotenteECriarInstancia()
    {
        OpenDeviceFactory.Register<CustomTestConfig, CustomTestStream>();
        OpenDeviceFactory.Register<CustomTestConfig, CustomTestStream>();

        var config = new CustomTestConfig();
        using var stream = OpenDeviceFactory.Create(config);

        await Assert.That(stream).IsTypeOf<CustomTestStream>();
    }
}
