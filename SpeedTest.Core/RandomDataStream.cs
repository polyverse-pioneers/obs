namespace SpeedTest.Core;

public sealed class RandomDataStream : Stream
{
    private readonly long _length;
    private readonly Random _random = new(12345);
    private long _position;

    public RandomDataStream(long length)
    {
        _length = length;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_position >= _length)
        {
            return 0;
        }

        var remaining = _length - _position;
        var toWrite = (int)Math.Min(count, remaining);

        _random.NextBytes(buffer.AsSpan(offset, toWrite));
        _position += toWrite;

        return toWrite;
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }
}
