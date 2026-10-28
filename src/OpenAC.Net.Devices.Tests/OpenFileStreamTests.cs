using System.Text;

namespace OpenAC.Net.Devices.Tests;

public class OpenFileStreamTests
{
    [Test]
    public async Task Write_MultiplasChamadasComControlePortaTrue_DeveConcatenarSemSobrescrever()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"openac_test_{Guid.NewGuid():N}.bin");
        try
        {
            var config = new FileConfig(tempFile)
            {
                CreateIfNotExists = true,
                ControlePorta = true
            };

            using var device = OpenDeviceFactory.Create(config);
            device.Open();
            await Assert.That(device.Conectado).IsTrue();

            device.Write(Encoding.UTF8.GetBytes("Linha1-"));
            device.Write(Encoding.UTF8.GetBytes("Linha2"));

            device.Close();
            await Assert.That(device.Conectado).IsFalse();

            var content = await File.ReadAllTextAsync(tempFile);
            await Assert.That(content).IsEqualTo("Linha1-Linha2");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Test]
    public async Task WriteAsync_ComControlePortaFalse_DeveGravarEDescarregarDados()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"openac_async_test_{Guid.NewGuid():N}.bin");
        try
        {
            var config = new FileConfig(tempFile)
            {
                CreateIfNotExists = true,
                ControlePorta = false
            };

            using var device = OpenDeviceFactory.Create(config);
            await device.OpenAsync();
            await Assert.That(device.Conectado).IsTrue();

            await device.WriteAsync(Encoding.UTF8.GetBytes("BlocoA"));
            await device.WriteAsync(Encoding.UTF8.GetBytes("BlocoB"));

            await device.CloseAsync();
            await Assert.That(device.Conectado).IsFalse();

            var content = await File.ReadAllTextAsync(tempFile);
            await Assert.That(content).IsEqualTo("BlocoABlocoB");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
