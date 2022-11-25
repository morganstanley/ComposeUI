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
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MorganStanley.ComposeUI.Messaging.Core;

/// <summary>
///     Represents an UTF8-encoded string buffer that uses pooled memory.
/// </summary>
public sealed class Utf8Buffer : IDisposable
{
    /// <summary>
    ///     Gets the string value of the buffer.
    /// </summary>
    /// <returns></returns>
    public string GetString()
    {
        ThrowIfDisposed();

        return Encoding.GetString(_bytes, 0, _length);
    }

    /// <summary>
    ///     Gets the bytes of the underlying buffer as a <see cref="ReadOnlySpan{T}" />
    /// </summary>
    /// <returns></returns>
    public ReadOnlySpan<byte> GetSpan()
    {
        ThrowIfDisposed();

        return new ReadOnlySpan<byte>(_bytes, 0, _length);
    }

    /// <summary>
    ///     Tries to decode the Base64-encoded contents of the buffer into a <see cref="IBufferWriter{T}" />.
    /// </summary>
    /// <param name="bufferWriter"></param>
    /// <returns>True, if the decoding was successful, False otherwise.</returns>
    public bool TryGetBase64Bytes(IBufferWriter<byte> bufferWriter)
    {
        var utf8Span = new Span<byte>(_bytes, 0, _length);
        var span = bufferWriter.GetSpan(Base64.GetMaxDecodedFromUtf8Length(utf8Span.Length));
        var status = Base64.DecodeFromUtf8(utf8Span, span, out _, out var bytesWritten);

        if (status != OperationStatus.Done)
            return false;

        bufferWriter.Advance(bytesWritten);

        return true;
    }

    /// <summary>
    ///     Decodes the Base64-encoded contents of the buffer into a <see cref="IBufferWriter{T}" />.
    /// </summary>
    /// <param name="bufferWriter"></param>
    /// <exception cref="InvalidOperationException">The buffer is not Base64-encoded.</exception>
    public void GetBase64Bytes(IBufferWriter<byte> bufferWriter)
    {
        if (!TryGetBase64Bytes(bufferWriter))
            throw ThrowHelper.InvalidBase64();
    }

    /// <summary>
    ///     Tries to decode the Base64-encoded contents of the buffer into a new array.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns>True, if the decoding was successful, False otherwise.</returns>
    public bool TryGetBase64Bytes([NotNullWhen(true)] out byte[]? bytes)
    {
        ThrowIfDisposed();

        var bufferWriter = new ArrayBufferWriter<byte>();

        if (!TryGetBase64Bytes(bufferWriter))
        {
            bytes = null;

            return false;
        }

        bytes = bufferWriter.WrittenMemory.ToArray();

        return true;
    }

    /// <summary>
    ///     Decodes the Base64-encoded contents of the buffer into a new array.
    /// </summary>
    /// <returns>The decoded bytes.</returns>
    /// <exception cref="InvalidOperationException">The buffer is not Base64-encoded.</exception>
    public byte[] GetBase64Bytes()
    {
        if (!TryGetBase64Bytes(out var bytes))
            throw ThrowHelper.InvalidBase64();

        return bytes;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        DisposeCore();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Creates a new <see cref="Utf8Buffer" /> from a string.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Utf8Buffer Create(string value)
    {
        var buffer = GetBuffer(Encoding.GetByteCount(value));

        return new Utf8Buffer(buffer, Encoding.GetBytes(value, buffer));
    }

    /// <summary>
    ///     Creates a new <see cref="Utf8Buffer" /> from a byte array containing the raw UTF8 bytes.
    /// </summary>
    /// <param name="utf8Bytes"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">The content of the buffer is not a valid UTF8 byte sequence.</exception>
    public static Utf8Buffer Create(byte[] utf8Bytes)
    {
        return Create(utf8Bytes.AsSpan());
    }

    /// <summary>
    ///     Creates a new <see cref="Utf8Buffer" /> from a memory block containing the raw UTF8 bytes.
    /// </summary>
    /// <param name="utf8Bytes"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">The content of the buffer is not a valid UTF8 byte sequence.</exception>
    public static Utf8Buffer Create(ReadOnlySpan<byte> utf8Bytes)
    {
        ValidateUtf8Bytes(utf8Bytes);
        var buffer = GetBuffer(utf8Bytes.Length);
        utf8Bytes.CopyTo(buffer);

        return new Utf8Buffer(buffer, utf8Bytes.Length);
    }

    /// <summary>
    ///     Creates a new <see cref="Utf8Buffer" /> from a sequence containing the raw UTF8 bytes.
    /// </summary>
    /// <param name="utf8Bytes"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">The content of the buffer is not a valid UTF8 byte sequence.</exception>
    public static Utf8Buffer Create(ReadOnlySequence<byte> utf8Bytes)
    {
        var length = checked((int)utf8Bytes.Length);
        var buffer = GetBuffer(length);
        utf8Bytes.CopyTo(buffer);

        try
        {
            ValidateUtf8Bytes(buffer.AsSpan(0, length));
        }
        catch
        {
            ReleaseBuffer(buffer);

            throw;
        }

        return new Utf8Buffer(buffer, length);
    }

    /// <summary>
    ///     Creates a new <see cref="Utf8Buffer" /> from the provided byte array, encoding it as a Base64 string.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static Utf8Buffer CreateBase64(byte[] bytes)
    {
        return CreateBase64(bytes.AsSpan());
    }

    /// <summary>
    ///     Creates a new <see cref="Utf8Buffer" /> from the provided buffer, encoding it as a Base64 string.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static Utf8Buffer CreateBase64(ReadOnlySpan<byte> bytes)
    {
        var buffer = GetBuffer(Base64.GetMaxEncodedToUtf8Length(bytes.Length));

        try
        {
            var status = Base64.EncodeToUtf8(bytes, buffer, out _, out var bytesWritten);
            Debug.Assert(status == OperationStatus.Done);

            return new Utf8Buffer(buffer, bytesWritten);
        }
        catch
        {
            ReleaseBuffer(buffer);

            throw;
        }
    }

    /// <summary>
    ///     Creates a new <see cref="Utf8Buffer" /> from the provided buffer, encoding it as a Base64 string.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static Utf8Buffer CreateBase64(ReadOnlySequence<byte> bytes)
    {
        var length = checked((int)bytes.Length);
        var buffer = GetBuffer(Base64.GetMaxEncodedToUtf8Length(length));
        bytes.CopyTo(buffer);

        try
        {
            var status = Base64.EncodeToUtf8InPlace(buffer, length, out var bytesWritten);
            Debug.Assert(status == OperationStatus.Done);

            return new Utf8Buffer(buffer, bytesWritten);
        }
        catch
        {
            ReleaseBuffer(buffer);

            throw;
        }
    }

    /// <summary>
    ///     Creates a new <see cref="Utf8Buffer" /> using the provided buffer.
    ///     The buffer must have been allocated by calling <see cref="Utf8Buffer.GetBuffer" />
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="length"></param>
    /// <remarks>
    ///     There's no validation of the buffer. If it wasn't created using <see cref="GetBuffer" />, the behavior is
    ///     unspecified.
    /// </remarks>
    internal Utf8Buffer(byte[] bytes, int length)
    {
        Debug.Assert(bytes.Length >= length);

        _bytes = bytes;
        _length = length;
    }

    internal static byte[] GetBuffer(int capacity)
    {
        return Pool.Rent(capacity);
    }

    internal static void ReleaseBuffer(byte[] buffer)
    {
        Pool.Return(buffer);
    }

    private void DisposeCore()
    {
        ReleaseBuffer(_bytes);
    }

    private static readonly ArrayPool<byte> Pool = ArrayPool<byte>.Create();

    private readonly byte[] _bytes;
    private readonly int _length;
    private bool _disposed;

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Utf8Buffer));
    }

    private static void ValidateUtf8Bytes(ReadOnlySpan<byte> bytes)
    {
        try
        {
            Encoding.GetCharCount(bytes);
        }
        catch (DecoderFallbackException)
        {
            throw new InvalidOperationException("The provided buffer is not a valid UTF8 sequence");
        }
    }

    ~Utf8Buffer()
    {
        DisposeCore();
    }

    private static readonly UTF8Encoding Encoding = new(false, true);

    private static class ThrowHelper
    {
        public static InvalidOperationException InvalidBase64()
        {
            return new("The current buffer is not Base64-encoded");
        }

        public static InvalidOperationException InvalidUtf8()
        {
            return new("The provided buffer does not contain a valid UTF8 sequence");
        }
    }
}



