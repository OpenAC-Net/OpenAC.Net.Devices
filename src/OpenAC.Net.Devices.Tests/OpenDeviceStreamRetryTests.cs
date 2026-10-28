namespace OpenAC.Net.Devices.Tests;

public class OpenDeviceStreamRetryTests
{
    private sealed class FlakyConfig() : BaseConfig("Flaky")
    {
        public int FailuresBeforeSuccess { get; set; }
    }

    private sealed class FlakyStream : Stream
    {
        private readonly FlakyDeviceStream owner;
        public MemoryStream WrittenData { get; } = new();
        public int FlushCount { get; private set; }

        public FlakyStream(FlakyDeviceStream owner)
        {
            this.owner = owner;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => WrittenData.Length;
        public override long Position { get => WrittenData.Position; set => WrittenData.Position = value; }

        public override void Flush() => FlushCount++;

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            FlushCount++;
            return Task.CompletedTask;
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            owner.WriteAttempts++;
            if (owner.WriteAttempts <= owner.Config.FailuresBeforeSuccess)
                throw new IOException($"Falha simulada na tentativa {owner.WriteAttempts}");

            WrittenData.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Write(buffer, offset, count);
            return Task.CompletedTask;
        }
    }

    private sealed class FlakyDeviceStream(FlakyConfig config) : OpenDeviceStream<FlakyConfig>(config)
    {
        public FlakyStream UnderlyingStream { get; private set; } = null!;
        public int WriteAttempts { get; set; }
        public int ReconnectCount { get; private set; }

        protected override int Available => 0;

        protected override bool OpenInternal()
        {
            UnderlyingStream ??= new FlakyStream(this);
            Writer = new BinaryWriter(UnderlyingStream);
            return true;
        }

        protected override bool CloseInternal()
        {
            ReconnectCount++;
            Writer = null;
            return true;
        }
    }

    [Test]
    public async Task Write_QuandoFalhaDuasVezesESucessoNaTerceira_DeveRespeitarTentativasEConcluir()
    {
        var config = new FlakyConfig
        {
            ControlePorta = false,
            Tentativas = 3,
            IntervaloTentativas = 1,
            FailuresBeforeSuccess = 2
        };

        using var device = new FlakyDeviceStream(config);
        device.Open();

        var payload = new byte[] { 10, 20, 30, 40 };
        device.Write(payload);

        await Assert.That(device.WriteAttempts).IsEqualTo(3);
        await Assert.That(device.UnderlyingStream.WrittenData.ToArray()).IsEquivalentTo(payload);
        await Assert.That(device.UnderlyingStream.FlushCount).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task Write_QuandoExcedeNumeroDeTentativas_DeveLancarIOException()
    {
        var config = new FlakyConfig
        {
            ControlePorta = false,
            Tentativas = 3,
            IntervaloTentativas = 1,
            FailuresBeforeSuccess = 3
        };

        using var device = new FlakyDeviceStream(config);
        device.Open();

        await Assert.That(() => device.Write([1, 2, 3]))
            .ThrowsExactly<IOException>();

        await Assert.That(device.WriteAttempts).IsEqualTo(3);
    }

    [Test]
    public async Task WriteAsync_QuandoFalhaDuasVezesESucessoNaTerceira_DeveRespeitarTentativasEConcluir()
    {
        var config = new FlakyConfig
        {
            ControlePorta = false,
            Tentativas = 3,
            IntervaloTentativas = 1,
            FailuresBeforeSuccess = 2
        };

        using var device = new FlakyDeviceStream(config);
        await device.OpenAsync();

        var payload = new byte[] { 99, 88, 77 };
        await device.WriteAsync(payload);

        await Assert.That(device.WriteAttempts).IsEqualTo(3);
        await Assert.That(device.UnderlyingStream.WrittenData.ToArray()).IsEquivalentTo(payload);
        await Assert.That(device.UnderlyingStream.FlushCount).IsGreaterThanOrEqualTo(1);
    }
}
