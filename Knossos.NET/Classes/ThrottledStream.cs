using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Knossos.NET.Classes
{
    /// <summary>
    /// ThrottledStream class for Knet
    /// Applies a max download speed based on the maxBytesPerSecond defined in GlobalSettingsViewModel.cs
    /// </summary>
    public class ThrottledStream : Stream
    {
        private readonly Stream _stream;
        private long _maxBytesPerSecond;
        private long _bytes;
        private long _time;

        public ThrottledStream(Stream stream, long maxBytesPerSecond)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            if (maxBytesPerSecond < 0)
                throw new ArgumentOutOfRangeException(nameof(maxBytesPerSecond));

            _maxBytesPerSecond = maxBytesPerSecond;
            _time = Environment.TickCount;
        }

        private void Limit(int inBytes)
        {
            if (inBytes <= 0 || _maxBytesPerSecond <= 0)
                return;

            _bytes += inBytes;
            var timePassed = Environment.TickCount - _time;

            if (timePassed > 0)
            {
                if ((_bytes * 1000L / timePassed) > _maxBytesPerSecond)
                {
                    var wait = (int)((_bytes * 1000L / _maxBytesPerSecond) - timePassed);
                    if (wait > 1)
                    {
                        try { Thread.Sleep(wait); } catch { }
                        timePassed += wait;
                    }
                }

                if (timePassed >= 1000)
                {
                    _time = Environment.TickCount;
                    _bytes = 0;
                }
            }
        }

        public void SetMaxBytesPerSecond(long maxBytesPerSecond)
        {
            if (maxBytesPerSecond != 0 && _maxBytesPerSecond != maxBytesPerSecond)
            {
                _maxBytesPerSecond = maxBytesPerSecond;
                _time = Environment.TickCount;
                _bytes = 0;
            }
        }

        public override long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }

        public override long Length => _stream.Length;
        public override bool CanSeek => _stream.CanSeek;
        public override bool CanRead => _stream.CanRead;
        public override bool CanWrite => _stream.CanWrite;

        public override void Flush() => _stream.Flush();
        public override void SetLength(long value) => _stream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count)
            => _stream.Write(buffer, offset, count);

        public override int Read(byte[] buffer, int offset, int count)
        {
            Limit(count);
            return _stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
            => _stream.Seek(offset, origin);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Limit(buffer.Length);
            return _stream.ReadAsync(buffer, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}