using System;
using System.IO;
using System.Text;

namespace Knossos.NET.Classes
{
    /// <summary>
    /// Helper class to read APNG files.
    /// </summary>
    public static class APNGHelper
    {
        // PNG signature
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        /// <summary>
        ///  Reads a stream to verify if it is a valid APNG file
        ///  Checks for acTL chuck presence.
        ///  Doesn't close or disposes the stream.
        ///  Throws a exception if the stream doesn't contains a png file data
        /// </summary>
        /// <param name="pngStream"></param>
        /// <returns>true if file is apng</returns>
        /// <exception cref="Exception"></exception>
        public static bool IsApng(Stream pngStream)
        {
            if (pngStream == null || !pngStream.CanRead)
                throw new ArgumentException("Stream inválido.");

            long originalPos = pngStream.Position;

            try
            {
                using (var br = new BinaryReader(pngStream, Encoding.ASCII, leaveOpen: true))
                {
                    var sig = br.ReadBytes(8);
                    if (!BytesEqual(sig, PngSignature))
                        throw new Exception("Incorrect PNG Signature");

                    // Look for acTL o IEND
                    while (pngStream.Position < pngStream.Length)
                    {
                        var lengthBytes = br.ReadBytes(4);
                        if (lengthBytes.Length < 4) break;
                        int length = ReadBigEndianInt(lengthBytes);

                        var chunkType = Encoding.ASCII.GetString(br.ReadBytes(4));

                        if (chunkType == "acTL")
                        {
                            return true;
                        }

                        pngStream.Seek(length + 4, SeekOrigin.Current);

                        if (chunkType == "IEND")
                            break; 
                    }
                }
                return false;
            }
            finally
            {
                pngStream.Seek(originalPos, SeekOrigin.Begin);
            }
        }


        // Helpers

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        private static int ReadBigEndianInt(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}
