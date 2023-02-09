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

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp;
using MorganStanley.ComposeUI.Messaging.TestUtils;

namespace MorganStanley.ComposeUI.Messaging.Protocol.Json;

public class Utf8JsonReaderExtensionsTests
{
    [Theory]
    [ClassData(typeof(CopyStringTheoryData))]
    public void CopyString_writes_the_unescaped_UTF8_string_to_the_buffer(CopyStringTestData testData)
    {
        var reader = testData.CreateReader();
        var output = new byte[reader.HasValueSequence ? reader.ValueSequence.Length : reader.ValueSpan.Length];

        var bytesWritten = reader.CopyString(output);

        output.AsSpan(0, bytesWritten).ToArray().Should().Equal(testData.ExpectedBytes);
    }

    public class CopyStringTestData
    {
        public CopyStringTestData(byte[][] utf8Buffers, byte[] expectedBytes)
        {
            ExpectedBytes = expectedBytes;
            _utf8Buffers = utf8Buffers;
        }

        public byte[] ExpectedBytes { get; }

        public Utf8JsonReader CreateReader()
        {
            Utf8JsonReader reader;

            if (_utf8Buffers.Length == 1)
            {
                reader = new Utf8JsonReader(_utf8Buffers[0]);
            }
            else
            {
                reader = new Utf8JsonReader(MemoryHelper.CreateMultipartSequence(_utf8Buffers));
            }

            Debug.Assert(reader.Read());
            Debug.Assert(reader.TokenType == JsonTokenType.StartArray);
            Debug.Assert(reader.Read());
            Debug.Assert(reader.TokenType == JsonTokenType.String);

            return reader;
        }

        public override string ToString()
        {
            return SymbolDisplay.FormatLiteral(
                Encoding.UTF8.GetString(ExpectedBytes),
                quote: false);
        }

        private readonly byte[][] _utf8Buffers;
    }

    public class CopyStringTheoryData : TheoryData<CopyStringTestData>
    {
        public CopyStringTheoryData()
        {
            Add("", "");
            Add("nothing special", "nothing special");
            Add("first line\\nsecond line", "first line\nsecond line");
            Add("head\\t\\r\\n\\b\\ftail", "head\t\r\n\b\ftail");
            Add("ComposeUI\\uD83D\\uDD25", "ComposeUI🔥");
        }

        private void Add(string inputString, string expected)
        {
            inputString = "[\"" + inputString + "\"]";
            var utf8Bytes = Encoding.UTF8.GetBytes(inputString);
            var expectedBytes = Encoding.UTF8.GetBytes(expected);
            Add(new CopyStringTestData(new[] { utf8Bytes }, expectedBytes));

            if (utf8Bytes.Length > 2)
            {
                var cutoff = utf8Bytes.Length / 2;
                Span<byte> utf8Span = utf8Bytes;

                Add(
                    new CopyStringTestData(
                        new[] { utf8Span[..cutoff].ToArray(), utf8Span[cutoff..].ToArray() },
                        expectedBytes));
            }
        }
    }
}