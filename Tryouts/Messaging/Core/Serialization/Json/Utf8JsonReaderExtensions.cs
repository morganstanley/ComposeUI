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

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Text;
using System.Text.Json;

namespace MorganStanley.ComposeUI.Messaging.Core.Serialization.Json;

/// <summary>
/// Shims for APIs that are not available in .NET 6
/// </summary>
internal static class Utf8JsonReaderExtensions
{
    public static bool ValueIsEscaped(this ref Utf8JsonReader reader)
    {
        if (!reader.HasValueSequence)
        {
            return reader.ValueSpan.IndexOf(JsonConstants.Backslash) >= 0;
        }

        var valueSequence = reader.ValueSequence;

        foreach (var chunk in valueSequence)
        {
            if (chunk.Span.IndexOf(JsonConstants.Backslash) >= 0)
                return true;
        }

        return false;
    }

    public static int CopyString(this ref Utf8JsonReader reader, Span<byte> utf8Destination)
    {
        if (reader.TokenType is not (JsonTokenType.String or JsonTokenType.PropertyName))
        {
            throw ThrowHelper.StringExpected();
        }

        var valueLength = reader.HasValueSequence 
            ? checked((int)reader.ValueSequence.Length) 
            : reader.ValueSpan.Length;

        if (!reader.ValueIsEscaped())
        {
            if (utf8Destination.Length < valueLength)
                throw ThrowHelper.DestinationTooShort(nameof(utf8Destination));

            if (reader.HasValueSequence)
            {
                reader.ValueSequence.CopyTo(utf8Destination);

                return checked((int)reader.ValueSequence.Length);
            }

            reader.ValueSpan.CopyTo(utf8Destination);

            return reader.ValueSpan.Length;
        }

        byte[]? rentedBuffer = null;
        ReadOnlySpan<byte> utf8Bytes;

        if (!reader.HasValueSequence)
        {
            utf8Bytes = reader.ValueSpan;
        }
        else
        {
            rentedBuffer = ArrayPool<byte>.Shared.Rent(valueLength);
            reader.ValueSequence.CopyTo(rentedBuffer);
            utf8Bytes = rentedBuffer.AsSpan(0, valueLength);
        }

        try
        {
            return Unescape(utf8Bytes, utf8Destination);
        }
        finally
        {
            if (rentedBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }
    }

    private static int Unescape(ReadOnlySpan<byte> utf8Bytes, Span<byte> utf8Destination)
    {
        var bytesWritten = 0;

        while (utf8Bytes.Length > 0)
        {
            var idx = utf8Bytes.IndexOf(JsonConstants.Backslash);

            if (idx < 0)
            {
                utf8Bytes.CopyTo(utf8Destination[bytesWritten..]);
                bytesWritten += utf8Bytes.Length;

                return bytesWritten;
            }

            if (idx > 0)
            {
                utf8Bytes.Slice(start: 0, length: idx).CopyTo(utf8Destination[bytesWritten..]);
                bytesWritten += idx;
            }

            if (ParseUnicode(utf8Bytes, ref idx, out var codePoint))
            {
                if (IsLowSurrogate(codePoint))
                {
                    throw ThrowHelper.InvalidUnicodeSequence();
                }

                if (IsHighSurrogate(codePoint))
                {
                    if (!ParseUnicode(utf8Bytes, ref idx, out var lowSurrogate)
                        || !IsLowSurrogate(lowSurrogate))
                    {
                        throw ThrowHelper.InvalidUnicodeSequence();
                    }

                    codePoint = GetCodePointFromSurrogatePair(codePoint, lowSurrogate);
                }

                var rune = new Rune(codePoint);

                if (!rune.TryEncodeToUtf8(utf8Destination.Slice(bytesWritten), out var utf8BytesWritten))
                {
                    throw ThrowHelper.DestinationTooShort(nameof(utf8Destination));
                }

                bytesWritten += utf8BytesWritten;
            }
            else
            {
                switch (utf8Bytes[++idx])
                {
                    case JsonConstants.Backslash
                        or JsonConstants.Quote
                        or JsonConstants.Slash:
                    {
                        utf8Destination[bytesWritten++] = utf8Bytes[idx++];

                        break;
                    }

                    case (byte)'t':
                    {
                        utf8Destination[bytesWritten++] = JsonConstants.Tab;
                        idx++;

                        break;
                    }

                    case (byte)'n':
                    {
                        utf8Destination[bytesWritten++] = JsonConstants.LineFeed;
                        idx++;

                        break;
                    }

                    case (byte)'r':
                    {
                        utf8Destination[bytesWritten++] = JsonConstants.CarriageReturn;
                        idx++;

                        break;
                    }

                    case (byte)'f':
                    {
                        utf8Destination[bytesWritten++] = JsonConstants.FormFeed;
                        idx++;

                        break;
                    }

                    case (byte)'b':
                    {
                        utf8Destination[bytesWritten++] = JsonConstants.Backspace;
                        idx++;

                        break;
                    }
                }
            }

            utf8Bytes = utf8Bytes[idx..];
        }

        return bytesWritten;
    }

    private static bool ParseUnicode(ReadOnlySpan<byte> utf8Bytes, ref int idx, out int codePoint)
    {
        codePoint = 0;

        if (!utf8Bytes[idx..].StartsWith(JsonConstants.UnicodeEscape))
            return false;

        if (!Utf8Parser.TryParse(utf8Bytes.Slice(idx + 2, 4), out codePoint, out var bytesConsumed, 'x')
            || bytesConsumed != 4)
        {
            return false;
        }

        idx += 6;

        return true;
    }

    private static bool IsLowSurrogate(int codePoint) =>
        codePoint is >= JsonConstants.LowSurrogateStart and <= JsonConstants.LowSurrogateEnd;

    private static bool IsHighSurrogate(int codePoint) =>
        codePoint is >= JsonConstants.HighSurrogateStart and <= JsonConstants.HighSurrogateEnd;

    private static int GetCodePointFromSurrogatePair(int highSurrogate, int lowSurrogate)
    {
        // See https://learn.microsoft.com/en-us/dotnet/standard/base-types/character-encoding-introduction#surrogate-pairs
        return 0x10000 + ((highSurrogate - 0xD800) * 0x0400) + (lowSurrogate - 0xDC00);
    }

    private const int StackallocMaxBytes = 255;
}
