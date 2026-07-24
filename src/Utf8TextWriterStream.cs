using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace AppsettingsDiff;

/// <summary>
/// A write-only <see cref="Stream"/> adapter that decodes UTF-8 bytes as they arrive and
/// forwards the resulting characters straight to a wrapped <see cref="TextWriter"/>.
/// </summary>
/// <remarks>
/// A <see cref="Utf8JsonWriter"/> only knows how to write to a
/// <see cref="Stream"/> or an <see cref="System.Buffers.IBufferWriter{T}"/>, while every
/// report writer in this project needs to hand its output to an arbitrary
/// <see cref="TextWriter"/> (console, file, or an in-memory <see cref="StringWriter"/> for the
/// legacy string-returning APIs). Buffering the whole serialized payload in a
/// <see cref="MemoryStream"/> before copying it to the target writer defeats the purpose of
/// streaming for large diffs (hundreds of environments x thousands of keys): peak memory still
/// scales with the full output size. This adapter removes that intermediate buffer - each
/// <see cref="Utf8JsonWriter"/> flush is decoded and written straight through, so memory use is
/// bounded by the writer's internal buffer size rather than by the total report size.
/// </remarks>
internal sealed class Utf8TextWriterStream : Stream
{
    private readonly TextWriter _writer;
    private readonly Decoder _decoder;
    private char[] _charBuffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8TextWriterStream"/> class.
    /// </summary>
    /// <param name="writer">The destination writer that decoded characters are forwarded to.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="writer"/> is <see langword="null"/>.</exception>
    public Utf8TextWriterStream(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        _writer = writer;
        _decoder = Encoding.UTF8.GetDecoder();
        _charBuffer = new char[1024];
    }

    /// <inheritdoc />
    public override bool CanRead => false;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => true;

    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override void Flush() => _writer.Flush();

    /// <summary>
    /// Decodes the supplied UTF-8 byte span and writes the resulting characters to the
    /// wrapped <see cref="TextWriter"/>.
    /// </summary>
    /// <param name="buffer">The UTF-8 encoded bytes to decode and forward.</param>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (buffer.IsEmpty)
            return;

        var maxChars = _decoder.GetCharCount(buffer, flush: false);
        if (_charBuffer.Length < maxChars)
        {
            _charBuffer = new char[maxChars];
        }

        var charsWritten = _decoder.GetChars(buffer, _charBuffer, flush: false);
        if (charsWritten > 0)
        {
            _writer.Write(_charBuffer, 0, charsWritten);
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="buffer"/> is <see langword="null"/>.</exception>
    public override void Write(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        Write(buffer.AsSpan(offset, count));
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown; this stream is write-only.</exception>
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown; this stream does not support seeking.</exception>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown; this stream does not support setting a length.</exception>
    public override void SetLength(long value) => throw new NotSupportedException();
}
