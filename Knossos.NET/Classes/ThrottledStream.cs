using System.Threading;
using System.Diagnostics;
using System.IO;
using System;


namespace Knossos.NET.Classes
{
    public class ThrottledStream : Stream
    {
        private Stream stream;
        private long maxBytesPerSecond = 0; //0 is disabled
        private long bytes = 0;
        private long time;

        public ThrottledStream(Stream stream, long maxBytesPerSecond)
        {
            if (stream == null || maxBytesPerSecond < 0)
            {
                throw new Exception("Base stream cant be null and maxBytesPerSecond needs to be 0 or higher.");
            }

            this.stream = stream;
            this.maxBytesPerSecond = maxBytesPerSecond;
            time = Environment.TickCount; //Register the starting time
        }

        private void Limit(int inBytes)
        {
            //If we havent read anything from the stream or  max bytes per second is 0 (disabled), just return
            if (inBytes <= 0 || maxBytesPerSecond <= 0 )
            {
                return;
            }

            bytes += inBytes;
            var timePassed = Environment.TickCount - time;

            if (timePassed > 0)
            {
                if ((bytes * 1000L / timePassed) > maxBytesPerSecond)
                {
                    var wait = (int)((bytes * 1000L / maxBytesPerSecond) - timePassed);
                    if (wait > 1)
                    {
                        try
                        {
                            Thread.Sleep(wait);
                        }
                        catch { }
                        timePassed += wait;
                    }
                }

                if (timePassed >= 1000)
                {
                    time = Environment.TickCount;
                    bytes = 0;
                }
            }
        }

        /* Possibility to change the Max Bytes per Second on the fly */
        public void SetMaxBytesPerSecond(long maxBytesPerSecond)
        {
            if (maxBytesPerSecond != 0 && this.maxBytesPerSecond != maxBytesPerSecond)
            {
                this.maxBytesPerSecond = maxBytesPerSecond;
                time = Environment.TickCount;
                bytes = 0;
            }
        }

        /* Needed Stream Override Implementations */

        public override long Position
        {
            get
            {
                return stream.Position;
            }
            set
            {
                stream.Position = value;
            }
        }

        public override long Length
        {
            get
            {
                return stream.Length;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return stream.CanSeek;
            }
        }

        public override bool CanRead
        {
            get
            {
                return stream.CanRead;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return stream.CanWrite;
            }
        }

        public override string ToString()
        {
            return stream.ToString()!;
        }

        public override void Flush()
        {
            stream.Flush();
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Limit(count);
            return stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }
    }
}
