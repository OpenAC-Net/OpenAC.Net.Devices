namespace OpenAC.Net.Devices.Tests;

public class BluetoothDeviceTests
{
    [Test]
    public async Task TryParseAddress_EFormatAddress_DevemConverterFormatosMacCorretamente()
    {
        var ok1 = BluetoothConfig.TryParseAddress("00:11:22:AA:BB:CC", out var addr1);
        var ok2 = BluetoothConfig.TryParseAddress("00-11-22-AA-BB-CC", out var addr2);
        var ok3 = BluetoothConfig.TryParseAddress("001122AABBCC", out var addr3);
        var invalid = BluetoothConfig.TryParseAddress("ENDERECO_INVALIDO", out _);

        await Assert.That(ok1).IsTrue();
        await Assert.That(ok2).IsTrue();
        await Assert.That(ok3).IsTrue();
        await Assert.That(invalid).IsFalse();

        await Assert.That(addr1).IsEqualTo(0x001122AABBCCUL);
        await Assert.That(addr2).IsEqualTo(addr1);
        await Assert.That(addr3).IsEqualTo(addr1);
        await Assert.That(BluetoothConfig.FormatAddress(addr1)).IsEqualTo("00:11:22:AA:BB:CC");
    }

    [Test]
    public async Task Canal_ForaDoIntervaloRfcomm_DeveLancarArgumentOutOfRangeException()
    {
        var config = new BluetoothConfig("00:11:22:33:44:55");

        config.Canal = 0;
        await Assert.That(config.Canal).IsEqualTo(0);

        config.Canal = 30;
        await Assert.That(config.Canal).IsEqualTo(30);

        await Assert.That(() => config.Canal = -1)
            .ThrowsExactly<ArgumentOutOfRangeException>();

        await Assert.That(() => config.Canal = 31)
            .ThrowsExactly<ArgumentOutOfRangeException>();

        var info = new BluetoothDeviceInfo("00:11:22:33:44:55", 0x001122334455UL);
        await Assert.That(() => new BluetoothConfig(info, canal: 31))
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Create_ComEnderecoValidoEInvalido_DeveValidarCorretamente()
    {
        await Assert.That(() => OpenDeviceFactory.Create(new BluetoothConfig()))
            .ThrowsExactly<ArgumentException>();

        await Assert.That(() => OpenDeviceFactory.Create(new BluetoothConfig("MAC_INVALIDO")))
            .ThrowsExactly<ArgumentException>();

        var macInfo = new BluetoothDeviceInfo("00:11:22:33:44:55", 0x001122334455UL);
        var portInfo = new BluetoothDeviceInfo("/dev/rfcomm0", 0);
        using var streamMac = OpenDeviceFactory.Create(new BluetoothConfig(macInfo, canal: 1));
        using var streamDev = OpenDeviceFactory.Create(new BluetoothConfig(portInfo));

        await Assert.That(streamMac).IsNotNull();
        await Assert.That(streamDev).IsNotNull();
    }

    [Test]
    public async Task GetDevices_DeveExecutarSemLancarExcecao()
    {
        var devices = BluetoothConfig.GetDevices();
        await Assert.That(devices).IsNotNull();
    }

    [Test]
    public async Task ParseMacOsBluetoothDevices_DeveExtrairDispositivosDoSystemProfilerECuPorts()
    {
        const string sampleProfiler = """
            Bluetooth:
                Bluetooth Controller:
                    Address: AA:BB:CC:DD:EE:FF
                Connected:
                    MTP-II:
                        Address: 00:11:22:33:44:55
                        Major Type: Printer
                Not Connected:
                    Datecs-DPP250:
                        Address: 66-77-88-99-AA-BB
            """;

        var devCu = new[]
        {
            "/dev/cu.Bluetooth-Incoming-Port",
            "/dev/cu.MTP-II"
        };

        var devices = BluetoothConfig.ParseMacOsBluetoothDevices(sampleProfiler, devCu);

        await Assert.That(devices).Count().IsEqualTo(3);

        var mtp = devices.First(d => d.RawAddress == 0x001122334455UL);
        await Assert.That(mtp.Name).IsEqualTo("MTP-II");
        await Assert.That(mtp.Connected).IsTrue();

        var datecs = devices.First(d => d.RawAddress == 0x66778899AABBUL);
        await Assert.That(datecs.Name).IsEqualTo("Datecs-DPP250");
        await Assert.That(datecs.Connected).IsFalse();

        var cuPort = devices.First(d => d.Address == "/dev/cu.MTP-II");
        await Assert.That(cuPort.Name).IsEqualTo("MTP-II");
    }
}
