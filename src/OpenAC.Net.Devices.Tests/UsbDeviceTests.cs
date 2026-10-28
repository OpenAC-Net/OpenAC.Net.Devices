using System.Text;

namespace OpenAC.Net.Devices.Tests;

public class UsbDeviceTests
{
    [Test]
    public async Task Create_SemDevicePathEVidPid_DeveLancarArgumentException()
    {
        var config = new UsbConfig();

        await Assert.That(() => OpenDeviceFactory.Create(config))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task Create_ComVendorIdEProductId_DeveInstanciarOpenUsbStream()
    {
        var config = new UsbConfig(0x04B8, 0x0202);
        using var device = OpenDeviceFactory.Create(config);

        await Assert.That(device).IsNotNull();
        await Assert.That(device.Config.Name).IsEqualTo("USB");
    }

    [Test]
    public async Task TryParseVidPidFromPath_ComCaminhoUsbWindows_DeveExtrairVidEPidCorretamente()
    {
        const string winUsbPath = @"\\?\usb#vid_04b8&pid_0202#5&1a2b3c4d&0&1#{28d78fad-5a12-11d1-ae5b-0000f803a8c2}";

        var ok = UsbConfig.TryParseVidPidFromPath(winUsbPath, out var vid, out var pid);

        await Assert.That(ok).IsTrue();
        await Assert.That(vid).IsEqualTo(0x04B8);
        await Assert.That(pid).IsEqualTo(0x0202);
    }

    [Test]
    public async Task GetDevices_DeveExecutarSemLancarExcecao()
    {
        var devices = UsbConfig.GetDevices();
        await Assert.That(devices).IsNotNull();
    }

    [Test]
    public async Task Write_ComDevicePathApontandoParaArquivoSimulado_DeveGravarDadosCorretamente()
    {
        var fakeUsbNode = Path.GetTempFileName();
        try
        {
            var info = new UsbDeviceInfo(fakeUsbNode, 0x04B8, 0x0202, "USB simulado");
            var config = new UsbConfig(info)
            {
                ControlePorta = true
            };

            using var device = OpenDeviceFactory.Create(config);
            device.Open();
            await Assert.That(device.Conectado).IsTrue();

            var bytes = Encoding.ASCII.GetBytes("ESC_POS_USB_TEST");
            device.Write(bytes);
            device.Close();

            var written = await File.ReadAllBytesAsync(fakeUsbNode);
            await Assert.That(written).IsEquivalentTo(bytes);
        }
        finally
        {
            if (File.Exists(fakeUsbNode))
                File.Delete(fakeUsbNode);
        }
    }

    [Test]
    public async Task ParseMacOsUsbDevices_DeveExtrairDispositivosDoIoRegELpStat()
    {
        const string sampleIoReg = """
            +-o Root  <class IORegistryEntry, id 0x100000100, retain 20>
              +-o TM-T20@14100000  <class IOUSBHostDevice, id 0x1000008a2>
                {
                  "idVendor" = 1208
                  "idProduct" = 514
                  "USB Product Name" = "TM-T20"
                }
              +-o USB_Serial@14200000  <class IOUSBHostDevice, id 0x1000008b0>
                {
                  "idVendor" = 0x1A86
                  "idProduct" = 0x7523
                  "USB Product Name" = "USB2.0-Serial"
                  "IOCalloutDevice" = "/dev/cu.usbserial-1420"
                }
            """;

        const string sampleLpStat = "device for EPSON_TM_T20: usb://EPSON/TM-T20?serial=123456";

        var devices = UsbConfig.ParseMacOsUsbDevices(sampleIoReg, sampleLpStat);

        await Assert.That(devices).Count().IsEqualTo(2);

        var epson = devices.First(d => d.VendorId == 0x04B8);
        await Assert.That(epson.ProductId).IsEqualTo(0x0202);
        await Assert.That(epson.DevicePath).IsEqualTo("cups:EPSON_TM_T20");

        var ch340 = devices.First(d => d.VendorId == 0x1A86);
        await Assert.That(ch340.ProductId).IsEqualTo(0x7523);
        await Assert.That(ch340.DevicePath).IsEqualTo("/dev/cu.usbserial-1420");
    }
}
