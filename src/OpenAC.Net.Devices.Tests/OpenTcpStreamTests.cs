using System.Net;
using System.Net.Sockets;
using System.Text;

namespace OpenAC.Net.Devices.Tests;

public class OpenTcpStreamTests
{
    [Test]
    public async Task WriteRead_ComHostnameLocalhost_DeveEnviarEReceberDados()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var serverTask = Task.Run(async () =>
        {
            using var serverClient = await listener.AcceptTcpClientAsync();
            await using var stream = serverClient.GetStream();
            var buffer = new byte[64];
            var read = await stream.ReadAsync(buffer);
            var msg = Encoding.UTF8.GetString(buffer, 0, read);
            var reply = Encoding.UTF8.GetBytes($"ACK:{msg}");
            await stream.WriteAsync(reply);
            await stream.FlushAsync();
            await Task.Delay(100);
        });

        var config = new TCPConfig("localhost", port)
        {
            ControlePorta = false,
            TimeOut = 2
        };

        using var device = OpenDeviceFactory.Create(config);
        device.Open();
        await Assert.That(device.Conectado).IsTrue();

        var response = device.WriteRead(Encoding.UTF8.GetBytes("PING"), timeSleep: 50);
        device.Close();

        await serverTask;

        await Assert.That(Encoding.UTF8.GetString(response)).IsEqualTo("ACK:PING");
    }

    [Test]
    public async Task WriteReadAsync_ComControlePortaTrue_DeveFuncionarAssincronamente()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var serverTask = Task.Run(async () =>
        {
            // Primeira conexão do OpenAsync() (que abre e fecha quando ControlePorta = true)
            using (await listener.AcceptTcpClientAsync())
            {
            }

            // Segunda conexão do WriteReadAsync()
            using var serverClient = await listener.AcceptTcpClientAsync();
            await using var stream = serverClient.GetStream();
            var buffer = new byte[64];
            var read = await stream.ReadAsync(buffer);
            var msg = Encoding.UTF8.GetString(buffer, 0, read);
            var reply = Encoding.UTF8.GetBytes($"PONG:{msg}");
            await stream.WriteAsync(reply);
            await stream.FlushAsync();
            await Task.Delay(100);
        });

        var config = new TCPConfig("127.0.0.1", port)
        {
            ControlePorta = true,
            TimeOut = 2
        };

        using var device = OpenDeviceFactory.Create(config);
        await device.OpenAsync();
        await Assert.That(device.Conectado).IsTrue();

        var response = await device.WriteReadAsync(Encoding.UTF8.GetBytes("ASYNC"), timeSleep: 50);
        await device.CloseAsync();

        await serverTask;

        await Assert.That(Encoding.UTF8.GetString(response)).IsEqualTo("PONG:ASYNC");
    }

    [Test]
    public async Task Limpar_DeveDescartarBytesPendentesNoBufferDeLeitura()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var serverTask = Task.Run(async () =>
        {
            using var serverClient = await listener.AcceptTcpClientAsync();
            await using var stream = serverClient.GetStream();
            await stream.WriteAsync("LIXO_NO_BUFFER"u8.ToArray());
            await stream.FlushAsync();
            await Task.Delay(200);
        });

        var config = new TCPConfig("127.0.0.1", port)
        {
            ControlePorta = false,
            TimeOut = 2
        };

        using var device = OpenDeviceFactory.Create(config);
        device.Open();

        await Task.Delay(50);
        device.Limpar();

        var lidoAposLimpar = device.Read();
        device.Close();
        await serverTask;

        await Assert.That(lidoAposLimpar).IsEmpty();
    }
}
