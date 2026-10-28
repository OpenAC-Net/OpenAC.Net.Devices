using Microsoft.JSInterop;
using OpenAC.Net.Devices.Blazor;
using OpenAC.Net.Devices.Blazor.WebBluetooth;
using OpenAC.Net.Devices.Blazor.WebSerial;
using OpenAC.Net.Devices.Blazor.WebUsb;

namespace OpenAC.Net.Devices.Tests;

public class WebDeviceTests
{
    private sealed class FakeInterop : IJSRuntime, IJSObjectReference
    {
        public List<string> Calls { get; } = [];
        public List<byte[]> Writes { get; } = [];
        public string Kind { get; set; } = "serial";
        public bool CanRead { get; set; } = true;
        public bool DisconnectOnClose { get; set; }
        public CancellationTokenSource? CancelOnWrite { get; set; }
        public int WriteFailures { get; set; }
        public WebDeviceReadStream? Receiver { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls.Add(identifier);
            object? result = null;
            switch (identifier)
            {
                case "import":
                case "createManager":
                case "bind":
                    result = this;
                    break;
                case "request":
                    result = new WebDeviceInfo { Id = 1, Kind = Kind, Name = "Fake device" };
                    break;
                case "open":
                    Receiver = ((DotNetObjectReference<WebDeviceReadStream>)args![2]!).Value;
                    result = CanRead;
                    break;
                case "write":
                    CancelOnWrite?.Cancel();
                    cancellationToken.ThrowIfCancellationRequested();
                    if (WriteFailures-- > 0) throw new JSException("Transfer failed.");
                    Writes.Add((byte[])args![1]!);
                    break;
                case "close" when DisconnectOnClose:
                    throw new JSDisconnectedException("Circuit closed.");
                case "getDevices":
                    result = new[] { new WebDeviceInfo { Id = 1, Kind = Kind } };
                    break;
            }
            return ValueTask.FromResult(result is null ? default! : (TValue)result);
        }
        public ValueTask DisposeAsync() { Calls.Add("disposeReference"); return ValueTask.CompletedTask; }
    }

    [Test]
    public async Task Factory_RegistraTodosOsTransportesWeb()
    {
        using var serial = OpenDeviceFactory.Create(new WebSerialConfig());
        using var usb = OpenDeviceFactory.Create(new WebUsbConfig());
        using var bluetooth = OpenDeviceFactory.Create(new WebBluetoothConfig());
        await Assert.That(serial).IsTypeOf<OpenWebSerialStream>();
        await Assert.That(usb).IsTypeOf<OpenWebUsbStream>();
        await Assert.That(bluetooth).IsTypeOf<OpenWebBluetoothStream>();
        await Assert.That(serial.Config.ControlePorta).IsFalse();
        await Assert.That(usb.Config.ControlePorta).IsFalse();
        await Assert.That(bluetooth.Config.ControlePorta).IsFalse();
    }

    [Test]
    public async Task MetodosSincronos_PelaFactory_ExigemOperacoesAssincronas()
    {
        using var stream = OpenDeviceFactory.Create(new WebSerialConfig());
        await Assert.That(() => stream.Open()).ThrowsExactly<NotSupportedException>();
        await Assert.That(() => stream.Close()).ThrowsExactly<NotSupportedException>();
        await Assert.That(() => stream.Write([1])).ThrowsExactly<NotSupportedException>();
        await Assert.That(() => stream.Read()).ThrowsExactly<NotSupportedException>();
        await Assert.That(() => stream.WriteRead([1])).ThrowsExactly<NotSupportedException>();
    }

    [Test]
    public async Task EscritaSemAbertura_NaoDescartaDadosSilenciosamente()
    {
        await using var stream = new OpenWebUsbStream(new WebUsbConfig());
        await Assert.That(() => stream.WriteAsync([1])).ThrowsExactly<InvalidOperationException>();
        await Assert.That(() => (Task)stream.ReadAsync()).ThrowsExactly<InvalidOperationException>();
        await Assert.That(() => stream.OpenAsync()).ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task Serial_LeituraEscritaEFechamento_UsamInteropAssincrono()
    {
        var interop = new FakeInterop();
        await using var service = new WebDeviceService(interop);
        await using var selected = await service.RequestSerialPortAsync();
        await using var stream = new OpenWebSerialStream(new WebSerialConfig(selected));
        await stream.OpenAsync();
        interop.Receiver!.Receive([1, 2, 3]);
        await Assert.That(await stream.ReadAsync()).IsEquivalentTo(new byte[] { 1, 2, 3 });
        await Assert.That((await stream.ReadAsync()).Length).IsEqualTo(0);
        await stream.WriteAsync([4, 5, 6]);
        await Assert.That(interop.Writes.SelectMany(x => x)).IsEquivalentTo(new byte[] { 4, 5, 6 });
        await stream.CloseAsync();
        await Assert.That(stream.Conectado).IsFalse();
        await Assert.That(interop.Calls.Count(x => x == "close")).IsEqualTo(1);
    }

    [Test]
    public async Task EscritaGrande_DivideMensagensParaSignalR()
    {
        var interop = new FakeInterop { Kind = "usb", CanRead = false };
        await using var service = new WebDeviceService(interop);
        await using var selected = await service.RequestUsbDeviceAsync();
        await using var stream = new OpenWebUsbStream(new WebUsbConfig(selected) { WriteBufferSize = 100000 });
        await stream.OpenAsync();
        var payload = Enumerable.Range(0, 25000).Select(x => (byte)x).ToArray();
        await stream.WriteAsync(payload);
        await Assert.That(interop.Writes.All(x => x.Length <= 8192)).IsTrue();
        await Assert.That(interop.Writes.SelectMany(x => x).SequenceEqual(payload)).IsTrue();
        await Assert.That(stream.CanRead).IsFalse();
    }

    [Test]
    public async Task DisposeAsync_FechaSemRevogarPermissao_EImpedeReabertura()
    {
        var interop = new FakeInterop();
        await using var service = new WebDeviceService(interop);
        await using var selected = await service.RequestSerialPortAsync();
        var stream = new OpenWebSerialStream(new WebSerialConfig(selected));
        await stream.OpenAsync();
        await stream.DisposeAsync();
        await stream.DisposeAsync();
        await Assert.That(interop.Calls.Count(x => x == "close")).IsEqualTo(1);
        await Assert.That(() => stream.OpenAsync()).ThrowsExactly<ObjectDisposedException>();
    }

    [Test]
    public async Task DisposeAsync_ToleraCircuitoDesconectado()
    {
        var interop = new FakeInterop { DisconnectOnClose = true };
        await using var service = new WebDeviceService(interop);
        await using var selected = await service.RequestSerialPortAsync();
        var stream = new OpenWebSerialStream(new WebSerialConfig(selected));
        await stream.OpenAsync();
        await stream.DisposeAsync();
        await Assert.That(stream.Conectado).IsFalse();
        await Assert.That(stream.IsDisposed).IsTrue();
    }

    [Test]
    public async Task MesmoHandle_ImpedeDoisStreamsSemFecharOPrimeiro()
    {
        var interop = new FakeInterop();
        await using var service = new WebDeviceService(interop);
        await using var selected = await service.RequestSerialPortAsync();
        await using var first = new OpenWebSerialStream(new WebSerialConfig(selected));
        await using var second = new OpenWebSerialStream(new WebSerialConfig(selected));
        await first.OpenAsync();
        await Assert.That(() => second.OpenAsync()).ThrowsExactly<InvalidOperationException>();
        await first.WriteAsync([9]);
        await Assert.That(interop.Calls.Count(x => x == "close")).IsEqualTo(0);
    }

    [Test]
    public async Task Bluetooth_ExigeUuidsETransporteCorretoAntesDoInterop()
    {
        var interop = new FakeInterop { Kind = "bluetooth" };
        await using var service = new WebDeviceService(interop);
        await using var selected = await service.RequestBluetoothDeviceAsync(new WebBluetoothRequestOptions());
        await using var bluetooth = new OpenWebBluetoothStream(new WebBluetoothConfig(selected));
        await Assert.That(() => bluetooth.OpenAsync()).ThrowsExactly<InvalidOperationException>();
        await using var usb = new OpenWebUsbStream(new WebUsbConfig(selected));
        await Assert.That(() => usb.OpenAsync()).ThrowsExactly<InvalidOperationException>();
        await Assert.That(interop.Calls.Contains("open")).IsFalse();
    }

    [Test]
    public async Task ErroDeLeitura_EhExpostoAposConsumirOsBytesRecebidos()
    {
        using var buffer = new WebDeviceReadStream();
        buffer.Receive([1, 2]);
        buffer.Fail("Disconnected");
        var bytes = new byte[8];
        await Assert.That(buffer.Read(bytes, 0, bytes.Length)).IsEqualTo(2);
        await Assert.That(() => buffer.Read(bytes, 0, bytes.Length)).ThrowsExactly<IOException>();
        buffer.Receive([3]);
        buffer.Flush();
        await Assert.That(() => buffer.Length).ThrowsExactly<IOException>();
    }

    [Test]
    public async Task CancelamentoAntesDaOperacao_NaoAbreDispositivo()
    {
        var interop = new FakeInterop();
        await using var service = new WebDeviceService(interop);
        await using var selected = await service.RequestSerialPortAsync();
        await using var stream = new OpenWebSerialStream(new WebSerialConfig(selected));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.That(() => stream.OpenAsync(cancellation.Token)).ThrowsExactly<OperationCanceledException>();
        await Assert.That(interop.Calls.Contains("open")).IsFalse();
    }

    [Test]
    public async Task CancelamentoDuranteEscrita_ComControlePorta_FechaDispositivo()
    {
        using var cancellation = new CancellationTokenSource();
        var interop = new FakeInterop { CancelOnWrite = cancellation };
        await using var service = new WebDeviceService(interop);
        await using var selected = await service.RequestSerialPortAsync();
        await using var stream = new OpenWebSerialStream(new WebSerialConfig(selected) { ControlePorta = true });
        await Assert.That(() => stream.WriteAsync([1], cancellation.Token)).ThrowsExactly<OperationCanceledException>();
        await Assert.That(interop.Calls.Count(x => x == "close")).IsEqualTo(1);
        await Assert.That(stream.Conectado).IsFalse();
    }

    [Test]
    public async Task FalhaDeTransferencia_PreservaRetentativasDaBiblioteca()
    {
        var interop = new FakeInterop { WriteFailures = 1 };
        await using var service = new WebDeviceService(interop);
        await using var selected = await service.RequestSerialPortAsync();
        await using var stream = new OpenWebSerialStream(new WebSerialConfig(selected)
        {
            Tentativas = 2, IntervaloTentativas = 1
        });
        await stream.OpenAsync();
        await stream.WriteAsync([1, 2]);
        await Assert.That(interop.Calls.Count(x => x == "open")).IsEqualTo(2);
        await Assert.That(interop.Calls.Count(x => x == "write")).IsEqualTo(2);
        await Assert.That(interop.Writes.SelectMany(x => x)).IsEquivalentTo(new byte[] { 1, 2 });
    }
}
