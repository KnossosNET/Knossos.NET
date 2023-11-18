using System;
using System.IO;


namespace Knossos.NET.Classes
{
    /// <summary>
    /// Small Class that allows to track the progress of how much % of file stream has been read
    /// </summary>
    public class ProgressStream : Stream
    {
        private Stream stream;
        private Action<int>? progressCallback;


        public ProgressStream(Stream stream, Action<int>? progressCallback)
        {
            this.stream = stream;
            this.progressCallback = progressCallback;
        }

        public override bool CanRead => stream.CanRead;

        public override bool CanSeek => stream.CanSeek;

        public override bool CanWrite => stream.CanWrite;

        public override long Length => stream.Length;

        public override long Position { get => stream.Position; set => stream.Position = value; }

        public override void Flush()
        {
            stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int n = stream.Read(buffer, offset, count);
            progressCallback?.Invoke((int)((100 * stream.Position) / stream.Length));
            return n;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            stream.Seek(offset, origin);
            return Position;
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
        }

        public override void Close()
        {
            stream.Close();
            base.Close();
        }
    }
}
