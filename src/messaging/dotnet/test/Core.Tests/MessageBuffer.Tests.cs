// Morgan Stanley makes this available to you under the Apache License,
// Version 2.0 (the "License"). You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0.
// 
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership. Unless required by applicable law or agreed
// to in writing, software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions
// and limitations under the License.

using System.Buffers;
using System.Text;
using MorganStanley.ComposeUI.Messaging.TestUtils;

namespace MorganStanley.ComposeUI.Messaging;

public class MessageBufferTests
{
    [Theory]
    [InlineData("")]
    [InlineData(StringConstants.LoremIpsum)]
    [InlineData(StringConstants.Emojis)]
    public void GetString_returns_the_original_string(string input)
    {
        var buffer = MessageBuffer.Create(input);

        buffer.GetString().Should().Be(input);
    }

    [Fact]
    public void GetString_throws_if_disposed()
    {
        var buffer = MessageBuffer.Create("x");
        buffer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => buffer.GetString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(StringConstants.LoremIpsum)]
    [InlineData(StringConstants.Emojis)]
    public void GetSpan_returns_the_original_string_encoded_as_UTF8(string input)
    {
        var buffer = MessageBuffer.Create(input);
        var span = buffer.GetSpan();

        span.ToArray().Should().Equal(Encoding.UTF8.GetBytes(input));
    }

    [Fact]
    public void GetSpan_throws_if_disposed()
    {
        var buffer = MessageBuffer.Create("x");
        buffer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => buffer.GetSpan());
    }

    [Theory]
    [InlineData("")]
    [InlineData(StringConstants.LoremIpsum)]
    [InlineData(StringConstants.Emojis)]
    public void GetString_returns_the_original_string_when_created_from_bytes(string input)
    {
        var buffer = MessageBuffer.Create(Encoding.UTF8.GetBytes(input));

        buffer.GetString().Should().Be(input);
    }

    [Theory]
    [InlineData("")]
    [InlineData(StringConstants.LoremIpsum)]
    [InlineData(StringConstants.Emojis)]
    public void GetSpan_returns_the_original_UTF8_bytes(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var buffer = MessageBuffer.Create(bytes);
        var span = buffer.GetSpan();

        span.ToArray().Should().Equal(bytes);
    }

    [Fact]
    public void TryGetBase64Bytes_gets_the_decoded_bytes_and_returns_true_when_the_data_is_valid_Base64()
    {
        var bytes = GetRandomBytes(100);
        var base64String = Convert.ToBase64String(bytes);
        var bufferWriter = new ArrayBufferWriter<byte>();
        var buffer = MessageBuffer.Create(base64String);

        buffer.TryGetBase64Bytes(out var decodedBytes).Should().BeTrue();
        buffer.TryGetBase64Bytes(bufferWriter).Should().BeTrue();
        decodedBytes.Should().Equal(bytes);
        bufferWriter.WrittenMemory.ToArray().Should().Equal(bytes);
    }

    [Fact]
    public void TryGetBase64Bytes_throws_if_disposed()
    {
        var buffer = MessageBuffer.Create("x");
        buffer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => buffer.TryGetBase64Bytes(out _));
        Assert.Throws<ObjectDisposedException>(() => buffer.TryGetBase64Bytes(new ArrayBufferWriter<byte>()));
    }

    [Fact]
    public void GetBase64Bytes_throws_if_disposed()
    {
        var buffer = MessageBuffer.Create("x");
        buffer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => buffer.GetBase64Bytes());
        Assert.Throws<ObjectDisposedException>(() => buffer.GetBase64Bytes(new ArrayBufferWriter<byte>()));
    }

    [Fact]
    public void TryGetBase64Bytes_returns_false_if_the_data_is_not_valid_Base64()
    {
        var buffer = MessageBuffer.Create("****");

        buffer.TryGetBase64Bytes(out _).Should().BeFalse();
        buffer.TryGetBase64Bytes(new ArrayBufferWriter<byte>()).Should().BeFalse();
    }

    [Fact]
    public void GetBase64Bytes_throws_if_the_data_is_not_valid_Base64()
    {
        var buffer = MessageBuffer.Create("****");
        
        Assert.Throws<FormatException>(() => buffer.GetBase64Bytes());
        Assert.Throws<FormatException>(() => buffer.GetBase64Bytes(new ArrayBufferWriter<byte>()));
    }

    [Fact]
    public void Create_throws_when_called_with_invalid_UTF8()
    {
        var invalidData = new byte[] { (byte)'L', (byte)'O', (byte)'L', 0xFF, 0xFF, 0xFF, 0xFF };

        new Action(
                () =>
                {
                    _ = MessageBuffer.Create(invalidData);
                    _ = MessageBuffer.Create(invalidData.AsSpan());
                    _ = MessageBuffer.Create(new ReadOnlySequence<byte>(invalidData));
                })
            .Should()
            .Throw<InvalidOperationException>();
    }

    [Fact]
    public void CreateBase64_encodes_the_bytes_in_Base64()
    {
        var bytes = GetRandomBytes(100);

        var buffer = MessageBuffer.CreateBase64(bytes);

        buffer.GetString().Should().Be(Convert.ToBase64String(bytes));
    }

    [Fact]
    public void CreateBase64_encodes_the_byte_span_in_Base64()
    {
        var bytes = GetRandomBytes(100);

        var buffer = MessageBuffer.CreateBase64(new ReadOnlySpan<byte>(bytes));

        buffer.GetString().Should().Be(Convert.ToBase64String(bytes));
    }

    [Fact]
    public void CreateBase64_encodes_the_byte_sequence_in_Base64()
    {
        var bytes = GetRandomBytes(100);

        var buffer = MessageBuffer.CreateBase64(MemoryHelper.CreateMultipartSequence(bytes.Chunk(10).ToArray()));

        buffer.GetString().Should().Be(Convert.ToBase64String(bytes));
    }

    private static byte[] GetRandomBytes(int count)
    {
        var result = new byte[count];
        new Random().NextBytes(result);

        return result;
    }
}

