using System;
using System.IO;


namespace Knossos.NET.Classes
{
    /// <summary>
    /// Small Class that allows to track the progress of how much % of file stream has been read
    /// </summary>
    public class ProgressStream : Stream
    {
        private readonly Stream _stream;
        private readonly Action<int>? _progressCallback;

        public ProgressStream(Stream stream, Action<int>? progressCallback)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _progressCallback = progressCallback;
        }

        public override bool CanRead => _stream.CanRead;
        public override bool CanSeek => _stream.CanSeek;
        public override bool CanWrite => _stream.CanWrite;
        public override long Length => _stream.Length;
        public override long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }

        public override void Flush() => _stream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            int n = _stream.Read(buffer, offset, count);
            _progressCallback?.Invoke((int)((100 * _stream.Position) / _stream.Length));
            return n;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            _stream.Seek(offset, origin);
            return Position;
        }

        public override void SetLength(long value) => _stream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
            => _stream.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
            }
            base.Dispose(disposing);
        }

        public override void Close()
        {
            _stream.Close();
            base.Close();
        }
    }
}
