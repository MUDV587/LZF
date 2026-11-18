using System;
using System.Linq;
using NUnit.Framework;

public class CLZF2Tests
{
    private static byte[] CreateRandomBytes(int size, int seed = 42)
    {
        var rng = new System.Random(seed);
        var bytes = new byte[size];
        rng.NextBytes(bytes);
        return bytes;
    }

    private static byte[] CreateRepetitiveBytes(int size)
    {
        var bytes = new byte[size];
        for (int i = 0; i < size; i++)
            bytes[i] = (byte)('A' + (i % 26));
        return bytes;
    }

    private static byte[] CreateZeroBytes(int size)
    {
        return new byte[size];
    }

    private static byte[] CreateTextBytes()
    {
        return System.Text.Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog. " +
            "The quick brown fox jumps over the lazy dog. The quick brown fox jumps over the lazy dog.");
    }

    private static void AssertRoundtrip(byte[] input)
    {
        byte[] compressed = CLZF2.Compress(input);
        byte[] decompressed = CLZF2.Decompress(compressed);
        Assert.AreEqual(input.Length, decompressed.Length);
        Assert.IsTrue(input.SequenceEqual(decompressed));
    }

    [Test]
    public void Roundtrip_Zeros_Small()
    {
        AssertRoundtrip(CreateZeroBytes(128));
    }

    [Test]
    public void Roundtrip_Zeros_Large()
    {
        AssertRoundtrip(CreateZeroBytes(65536));
    }

    [Test]
    public void Roundtrip_Repetitive_Small()
    {
        AssertRoundtrip(CreateRepetitiveBytes(512));
    }

    [Test]
    public void Roundtrip_Repetitive_Large()
    {
        AssertRoundtrip(CreateRepetitiveBytes(65536));
    }

    [Test]
    public void Roundtrip_Random_Small()
    {
        AssertRoundtrip(CreateRandomBytes(512));
    }

    [Test]
    public void Roundtrip_Random_Large()
    {
        AssertRoundtrip(CreateRandomBytes(65536));
    }

    [Test]
    public void Roundtrip_Random_ExtraLarge()
    {
        AssertRoundtrip(CreateRandomBytes(524288)); // 512 KB
    }

    [Test]
    public void Roundtrip_Text_Medium()
    {
        AssertRoundtrip(CreateTextBytes());
    }

    [Test]
    public void Roundtrip_SingleByte()
    {
        AssertRoundtrip(new byte[] { 0xAB });
    }

    [Test]
    public void Roundtrip_TwoBytes()
    {
        AssertRoundtrip(new byte[] { 0xAB, 0xCD });
    }

    [Test]
    public void Roundtrip_ThreeBytes()
    {
        AssertRoundtrip(new byte[] { 0xAB, 0xCD, 0xEF });
    }

    [Test]
    public void Roundtrip_FourBytes()
    {
        AssertRoundtrip(new byte[] { 0xAB, 0xCD, 0xEF, 0x01 });
    }

    [Test]
    public void Roundtrip_EmptyInput_ReturnsZeroLength()
    {
        byte[] compressed = CLZF2.Compress(new byte[0]);
        Assert.AreEqual(0, compressed.Length);

        byte[] decompressed = CLZF2.Decompress(compressed);
        Assert.AreEqual(0, decompressed.Length);
    }

    [Test]
    public void Roundtrip_CompressibleTailAtEnd_DoesNotCrash()
    {
        // match placed exactly at the end of the input — this used to read out of bounds
        byte[] input = new byte[4096];
        for (int i = 0; i < 4096; i++)
            input[i] = 0;

        AssertRoundtrip(input);
    }

    [Test]
    public void Compress_InputLength_OnlyCompressesPrefix()
    {
        byte[] input = CreateRandomBytes(4096);
        byte[] compressed = CLZF2.Compress(input, 100);
        byte[] decompressed = CLZF2.Decompress(compressed);

        Assert.AreEqual(100, decompressed.Length);
        Assert.IsTrue(input.Take(100).SequenceEqual(decompressed));
    }

    [Test]
    public void Compress_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CLZF2.Compress(null));
        Assert.Throws<ArgumentNullException>(() => CLZF2.Compress(null, 0));
        Assert.Throws<ArgumentNullException>(() => CLZF2.Decompress(null));
        Assert.Throws<ArgumentNullException>(() => CLZF2.Decompress(null, 0));

        byte[] temp = null;
        Assert.Throws<ArgumentNullException>(() => CLZF2.Compress(null, ref temp));
        Assert.Throws<ArgumentNullException>(() => CLZF2.Decompress(null, ref temp));
    }

    [Test]
    public void Compress_InputLengthBeyondArray_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CLZF2.Compress(new byte[10], 11));
        Assert.Throws<ArgumentOutOfRangeException>(() => CLZF2.Decompress(new byte[10], 11));
    }

    [Test]
    public void Compress_NegativeInputLength_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CLZF2.Compress(new byte[10], -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CLZF2.Decompress(new byte[10], -1));
    }

    [Test]
    public void Compress_ReusesProvidedBuffer()
    {
        byte[] input = CreateRepetitiveBytes(2048);
        byte[] buffer = new byte[input.Length * 2];
        byte[] originalBuffer = buffer;

        int size = CLZF2.Compress(input, ref buffer);

        Assert.AreSame(originalBuffer, buffer);
        Assert.Greater(size, 0);
    }

    [Test]
    public void Compress_SmallBuffer_GrowsBuffer()
    {
        byte[] input = CreateRepetitiveBytes(2048);
        byte[] buffer = new byte[16];

        int size = CLZF2.Compress(input, ref buffer);

        Assert.Greater(buffer.Length, 16);
        Assert.GreaterOrEqual(buffer.Length, size);
    }

    [Test]
    public void Compress_ConvenienceAndRefBufferApi_ProduceSameResult()
    {
        byte[] input = CreateRandomBytes(4096, 7);
        byte[] direct = CLZF2.Compress(input);

        byte[] buffer = null;
        int size = CLZF2.Compress(input, ref buffer);

        Assert.AreEqual(direct.Length, size);
        Assert.IsTrue(direct.SequenceEqual(buffer.Take(size)));
    }

    [Test]
    public void Decompress_CorruptData_ThrowsArgumentException()
    {
        // All-0x20 control bytes decode as back references that point before the output start.
        var garbage = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
        Assert.Throws<ArgumentException>(() => CLZF2.Decompress(garbage));
    }

    [Test]
    public void Decompress_TruncatedData_ThrowsArgumentException()
    {
        byte[] input = CreateRepetitiveBytes(2048);
        byte[] compressed = CLZF2.Compress(input);
        var truncated = compressed.Take(compressed.Length / 2).ToArray();

        Assert.Catch<ArgumentException>(() => CLZF2.Decompress(truncated));
    }

    [Test]
    public void Compress_4KBZeros_CompressesSignificantly()
    {
        byte[] zeros = CreateZeroBytes(4096);
        byte[] compressed = CLZF2.Compress(zeros);

        Assert.Less(compressed.Length, 128);
    }

    [Test]
    public void Compress_Multithreaded_ProducesConsistentResults()
    {
        byte[] input = CreateRandomBytes(16384, 99);
        byte[] expected = CLZF2.Compress(input);

        var results = new byte[4][];
        System.Threading.Tasks.Parallel.For(0, 4, i =>
        {
            results[i] = CLZF2.Compress(input);
        });

        foreach (var result in results)
            Assert.IsTrue(expected.SequenceEqual(result));
    }

    [Test]
    public void Compress_HighlyCompressible_EncodesAndDecodesCorrectly()
    {
        var input = new byte[65536];
        for (int i = 0; i < input.Length; i += 8)
        {
            System.Buffer.BlockCopy(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, 0, input, i, 8);
        }

        AssertRoundtrip(input);
    }
}
