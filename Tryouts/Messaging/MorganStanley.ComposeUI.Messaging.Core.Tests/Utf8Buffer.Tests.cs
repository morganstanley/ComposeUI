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

public class Utf8BufferTests
{
    [Theory]
    [InlineData("")]
    [InlineData(StringConstants.LoremIpsum)]
    [InlineData(StringConstants.Emojis)]
    public void GetString_returns_the_original_string(string input)
    {
        var buffer = Utf8Buffer.Create(input);

        buffer.GetString().Should().Be(input);
    }

    [Theory]
    [InlineData("")]
    [InlineData(StringConstants.LoremIpsum)]
    [InlineData(StringConstants.Emojis)]
    public void GetSpan_returns_the_original_string_encoded_as_UTF8(string input)
    {
        var buffer = Utf8Buffer.Create(input);
        var span = buffer.GetSpan();

        span.ToArray().Should().Equal(Encoding.UTF8.GetBytes(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData(StringConstants.LoremIpsum)]
    [InlineData(StringConstants.Emojis)]
    public void GetString_returns_the_original_string_when_created_from_bytes(string input)
    {
        var buffer = Utf8Buffer.Create(Encoding.UTF8.GetBytes(input));

        buffer.GetString().Should().Be(input);
    }

    [Theory]
    [InlineData("")]
    [InlineData(StringConstants.LoremIpsum)]
    [InlineData(StringConstants.Emojis)]
    public void GetSpan_returns_the_original_UTF8_bytes(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var buffer = Utf8Buffer.Create(bytes);
        var span = buffer.GetSpan();

        span.ToArray().Should().Equal(bytes);
    }

    [Fact]
    public void Create_throws_when_called_with_invalid_UTF8()
    {
        var invalidData = new byte[] { (byte)'L', (byte)'O', (byte)'L', 0xFF, 0xFF, 0xFF, 0xFF };

        new Action(
                () =>
                {
                    _ = Utf8Buffer.Create(invalidData);
                    _ = Utf8Buffer.Create(invalidData.AsSpan());
                    _ = Utf8Buffer.Create(new ReadOnlySequence<byte>(invalidData));
                })
            .Should()
            .Throw<InvalidOperationException>();
    }

    [Fact]
    public void CreateBase64_encodes_the_bytes_in_Base64()
    {
        var bytes = GetRandomBytes(100);

        var buffer = Utf8Buffer.CreateBase64(bytes);

        buffer.GetString().Should().Be(Convert.ToBase64String(bytes));
    }

    [Fact]
    public void CreateBase64_encodes_the_byte_span_in_Base64()
    {
        var bytes = GetRandomBytes(100);

        var buffer = Utf8Buffer.CreateBase64(new ReadOnlySpan<byte>(bytes));

        buffer.GetString().Should().Be(Convert.ToBase64String(bytes));
    }

    [Fact]
    public void CreateBase64_encodes_the_byte_sequence_in_Base64()
    {
        var bytes = GetRandomBytes(100);

        var buffer = Utf8Buffer.CreateBase64(MemoryHelper.CreateMultipartSequence(bytes.Chunk(10).ToArray()));

        buffer.GetString().Should().Be(Convert.ToBase64String(bytes));
    }

    private static byte[] GetRandomBytes(int count)
    {
        var result = new byte[count];
        new Random().NextBytes(result);

        return result;
    }
}

