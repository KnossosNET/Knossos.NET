using IonKiwi.lz4;
using System.Threading.Tasks;

namespace VP.NET
{
    public enum CompressionHeader
    {
        LZ41
    }

    public struct CompressionInfo
    {
        /// <summary>
        /// null for uncompressed
        /// </summary>
        public CompressionHeader? header;
        public int? uncompressedFileSize;
    }

    public static class VPCompression
    {
        public static CompressionHeader CompressionHeader { get; set; } = CompressionHeader.LZ41;
        public static LZ4FrameBlockSize BlockSize { get; set; } = LZ4FrameBlockSize.Max1MB;
        /// <summary>
        /// Min 3 and Max 12
        /// </summary>
        public static int CompressionLevel { get; set; } = 6;
        /// <summary>
        /// In general we want to compress files that arent already compressed, .dds being the exception here as it shows large gains.
        /// .ogg, .png and .jpeg are formats that should be already compressed and nothing will be gained
        /// Compressing .wav do show some gains and do work, but i would not recommend to compress audio as it may cause shuttering during playblack
        /// .json should be always protected, you do not want to compress the mod.json file do you?
        /// .eff are always way too small
        /// </summary>
        public static List<string> ExtensionIgnoreList { get; set; } = new List<string> { ".ogg", ".wav", ".png", ".jpeg", ".json", ".eff", ".ini", ".vp", ".vpc", ".exe", ".dll", ".doc", ".docx", ".pdf", ".xls", ".xlsx", ".txt", ".so", ".appimage", ".token", ".mp4", ".7z"/*, ".fc2", ".fs2", ".tbm", ".tbl"*/ };
        /// <summary>
        /// Minimum size in bytes that a file has to have in order to compress it
        /// Default is 10240 bytes (10KB)
        /// </summary>
        public static int MinimumSize { get; set; } = 10240;
        /// <summary>
        /// Minimum FSO version that supports all compression features
        /// </summary>
        public static string MinimumFSOVersion { get; } = "23.1.0-20230527";

        /// <summary>
        /// Compresses a source stream into a destination stream
        /// This method cant check if file extension is on the ignore list, that is need to be done before calling this method
        /// If the source stream is a vp stream the originalFileSize must be passed
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="originalFileSize"></param>
        /// <exception cref="Exception"></exception>
        public static async Task<int> CompressStream(Stream input, Stream output, int? originalFileSize = null)
        {
            int compressedSize = 0;
            if(CompressionLevel < 3 || CompressionLevel > 12) 
            {
                CompressionLevel = 6;
            }

            switch (CompressionHeader)
            {
                case CompressionHeader.LZ41:
                        compressedSize = LZ4RawUtility.LZ41_Stream_Compress_HC(input, output, BlockSize, CompressionLevel, originalFileSize);
                    break;
            }
            return compressedSize;
        }

        /// <summary>
        /// Decompresses a source stream into a destination stream
        /// If the source stream is a vp stream the compressedFileSize must be passed
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="header"></param>
        /// <param name="compressedFileSize"></param>
        /// <returns>Uncompressed file size</returns>
        public static async Task<int> DecompressStream(Stream input, Stream output, CompressionHeader? header = null, int? compressedFileSize = null)
        {
            int uncompressedSize = 0;

            if(header == null)
            {
                long org_pos = input.Position;
                BinaryReader br = new BinaryReader(input);
                if (new string(br.ReadChars(4)) == "LZ41")
                {
                    header = CompressionHeader.LZ41;
                }
                input.Position = org_pos;
            }

            switch (header)
            {
                case CompressionHeader.LZ41:
                    uncompressedSize = LZ4RawUtility.LZ41_Stream_Decompress(input, output, compressedFileSize);
                    break;
            }

            return uncompressedSize;
        }
    }
}
