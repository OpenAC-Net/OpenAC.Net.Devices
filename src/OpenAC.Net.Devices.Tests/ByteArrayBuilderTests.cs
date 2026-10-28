using OpenAC.Net.Devices.Commom;

namespace OpenAC.Net.Devices.Tests;

public class ByteArrayBuilderTests
{
    [Test]
    public async Task Append_ComVariosOverloads_DeveConstruirArrayCorretamente()
    {
        using var builder = new ByteArrayBuilder();

        builder.Append(0x01)
               .Append(new byte[] { 0x02, 0x03 })
               .Append(new List<byte> { 0x04, 0x05 })
               .Append([0xFF, 0x06, 0x07, 0xFF], 1, 2);

        var result = builder.ToArray();

        await Assert.That(builder.Length).IsEqualTo(7);
        await Assert.That(result).IsEquivalentTo(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 });
    }

    [Test]
    public async Task Clear_DeveZerarConteudo()
    {
        using var builder = new ByteArrayBuilder();
        builder.Append([1, 2, 3]);

        builder.Clear();

        await Assert.That(builder.Length).IsEqualTo(0);
        await Assert.That(builder.ToArray()).IsEmpty();
    }
}
