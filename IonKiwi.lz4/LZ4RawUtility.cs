using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using static System.Net.WebRequestMethods;

namespace IonKiwi.lz4 {
	public static class LZ4RawUtility {
		public static int CompressBound(int size) {
			return lz4.LZ4_compressBound(size);
		}

		public static unsafe int Compress(byte[] input, int inputOffset, int length, byte[] output, int outputOffset) {
			if (input == null) {
				throw new ArgumentNullException(nameof(input));
			}
			else if (inputOffset < 0 || inputOffset >= input.Length) {
				throw new ArgumentOutOfRangeException(nameof(inputOffset));
			}
			else if (length < 0 || inputOffset + length > input.Length) {
				throw new ArgumentOutOfRangeException(nameof(length));
			}
			else if (output == null) {
				throw new ArgumentNullException(nameof(output));
			}
			else if (outputOffset < 0 || outputOffset >= output.Length) {
				throw new ArgumentOutOfRangeException(nameof(outputOffset));
			}
			int maxLength = lz4.LZ4_compressBound(length - inputOffset);
			if (outputOffset + maxLength > output.Length) {
				throw new InvalidOperationException($"Output should be at least '{maxLength}' bytes (not including '{nameof(outputOffset)}').");
			}
			int compressedSize;
			fixed (byte* inputPtr = &input[inputOffset], outputPtr = &output[outputOffset]) {
				compressedSize = lz4.LZ4_compress_default(inputPtr, outputPtr, length, maxLength);
			}
			if (compressedSize <= 0) {
				throw new Exception("Compression failed");
			}
			return compressedSize;
		}

		public static unsafe int CompressHC(byte[] input, int inputOffset, int length, byte[] output, int outputOffset, int compressionLevel) {
			if (input == null) {
				throw new ArgumentNullException(nameof(input));
			}
			else if (inputOffset < 0 || inputOffset >= input.Length) {
				throw new ArgumentOutOfRangeException(nameof(inputOffset));
			}
			else if (length < 0 || inputOffset + length > input.Length) {
				throw new ArgumentOutOfRangeException(nameof(length));
			}
			else if (output == null) {
				throw new ArgumentNullException(nameof(output));
			}
			else if (outputOffset < 0 || outputOffset >= output.Length) {
				throw new ArgumentOutOfRangeException(nameof(outputOffset));
			}
			int maxLength = lz4.LZ4_compressBound(length - inputOffset);
			if (outputOffset + maxLength > output.Length) {
				throw new InvalidOperationException($"Output should be at least '{maxLength}' bytes (not including '{nameof(outputOffset)}').");
			}
			int compressedSize;
			fixed (byte* inputPtr = &input[inputOffset], outputPtr = &output[outputOffset]) {
				compressedSize = lz4.LZ4_compress_HC(inputPtr, outputPtr, length, maxLength, compressionLevel);
			}
			if (compressedSize <= 0) {
				throw new Exception("Compression failed");
			}
			return compressedSize;
		}

		public static unsafe int Decompress(byte[] input, int inputOffset, int length, byte[] output, int outputOffset) {
			if (input == null) {
				throw new ArgumentNullException(nameof(input));
			}
			else if (inputOffset < 0 || inputOffset >= input.Length) {
				throw new ArgumentOutOfRangeException(nameof(inputOffset));
			}
			else if (length < 0 || inputOffset + length > input.Length) {
				throw new ArgumentOutOfRangeException(nameof(length));
			}
			else if (output == null) {
				throw new ArgumentNullException(nameof(output));
			}
			else if (outputOffset < 0 || outputOffset >= output.Length) {
				throw new ArgumentOutOfRangeException(nameof(outputOffset));
			}

			int decompressedSize;
			fixed (byte* inputPtr = &input[inputOffset], outputPtr = &output[outputOffset]) {
				decompressedSize = lz4.LZ4_decompress_safe(inputPtr, outputPtr, length, output.Length - outputOffset);
			}
			if (decompressedSize <= 0) {
				throw new Exception("Decompression failed");
			}
			return decompressedSize;
		}

        /// <summary>
        /// Compresses a file stream using LZ41 file format for FSO
        /// Supports random access
        /// Support passing VP file stream pointing to a file inside of it by using the optional inputfileSize argument,
        /// this function will only read inputfileSize bytes of the input stream.
        /// Also supports w
        /// The streams are not closed or disposed.
        /// Compression Level= 3(min), to 12(max). Default is 6.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="outputStream"></param>
        /// <param name="blockSize"></param>
        /// <param name="compressionLevel"></param>
        /// <param name="inputfileSize"></param>
        /// <returns>Compressed stream size</returns>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="IOException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="Exception"></exception>
        public static unsafe int LZ41_Stream_Compress_HC(Stream inputStream, Stream outputStream, LZ4FrameBlockSize blockSize, int compressionLevel = 6, int? inputfileSize = null)
        {
            //Validation
            if (compressionLevel < 3 || compressionLevel > 12)
                throw new ArgumentOutOfRangeException("Compression level is outside the minimum allowed, minimum is 3 and maximum is 12");
            if (!inputStream.CanRead)
                throw new IOException("Input stream can not be read.");
            if (inputStream.Length == 0)
                throw new IOException("Input stream can not empty.");
            if (inputfileSize.HasValue && inputfileSize > inputStream.Length)
                throw new ArgumentOutOfRangeException("The specified input file size is larger than the input stream length");
            if (!outputStream.CanWrite)
                throw new IOException("Output stream can not be written.");
            int _blockSize = 0;
            switch (blockSize)
            {
                case LZ4FrameBlockSize.Max64KB:
					_blockSize = 65536;
                    break;
                case LZ4FrameBlockSize.Max256KB:
                    _blockSize = 262144;
                    break;
                case LZ4FrameBlockSize.Max1MB:
                    _blockSize = 1048576;
                    break;
                case LZ4FrameBlockSize.Max4MB:
                    _blockSize = 4194304;
                    break;
                default:
                    throw new NotSupportedException("Unsupported blocksize "+blockSize.ToString());
            }

            //Init values
            LZ4_streamHC lz4StreamBody = new LZ4_streamHC();
            LZ4_streamHC* lz4Stream = &lz4StreamBody;
            int writtenBytes = 0;
            int fileSize = inputfileSize.HasValue ? inputfileSize.Value : (int)inputStream.Length;
            int maxBlocks = (lz4.LZ4_compressBound(fileSize) / _blockSize) + 50;
            Queue<int> offsets = new Queue<int>();
            lz4.LZ4_initStreamHC(lz4Stream, sizeof(LZ4_stream));

            //Write Header
            outputStream.Write(Encoding.ASCII.GetBytes("LZ41"), 0, 4);
            writtenBytes += 4;
            
            //Write the position of the first block in the chain
            offsets.Enqueue(4);

            int totalBytesRead = 0;
            //Proccess until end of input stream
            while (true)
            {
                fixed (byte* cmpPtr = new byte[lz4.LZ4_compressBound(_blockSize)])
                {
                    //Calulate how much data remains, each block contains "block size" of uncompressed data, except maybe the last
                    long remaining = fileSize - totalBytesRead;

                    //No more to read
                    if(remaining <= 0)
                        break;

                    long read = remaining < _blockSize ? remaining : _blockSize;
                    byte[] inBuf = new byte[read];
                    
                    //Read into buffer array
                    int inBytes = inputStream.Read(inBuf, 0, (int)read);
                    totalBytesRead += inBytes;

                    //Read Error
                    if (0 == inBytes)
                        throw new IOException("Error while reading from the input stream or unexpected end of stream.");

                    //Reset the stream to make all blocks independient blocks
                    lz4.LZ4_resetStreamHC_fast(lz4Stream, 6);

                    fixed (byte* inPtr = inBuf)
                    {
                        //Compress one block
                        int cmpBytes = lz4.LZ4_compress_HC_continue(lz4Stream, inPtr, cmpPtr, inBytes, lz4.LZ4_compressBound(_blockSize));

                        if (cmpBytes <= 0)
                            throw new Exception("An error has ocurred while compressing the data.");

                        //Write compressed block to stream
                        var byteArray = new byte[cmpBytes];
                        System.Runtime.InteropServices.Marshal.Copy(new IntPtr(cmpPtr), byteArray, 0, cmpBytes);
                        outputStream.Write(byteArray, 0, cmpBytes);
                        writtenBytes += cmpBytes;

                        //Add the ending position to the offset index what is also the starting position for next block
                        int last = offsets.LastOrDefault();
                        offsets.Enqueue(last + cmpBytes);

                        //This in case that for some reason we are trying to compress way more data than we should
                        if (offsets.Count > maxBlocks)
                            throw new Exception("Max blocks overflow.");
                    }
                }
            }

            //Save the number of offsets index for later
            int numberOffsets = offsets.Count();

            //Write offset index table to stream
            while (offsets.Any())
            {
                var elem = offsets.Dequeue();
                outputStream.Write(BitConverter.GetBytes(elem), 0, 4);
                writtenBytes += 4;
            }

            //Write the number of offsets
            outputStream.Write(BitConverter.GetBytes(numberOffsets), 0, 4);
            writtenBytes += 4;

            //Write the original uncompressed filesile
            outputStream.Write(BitConverter.GetBytes(fileSize), 0, 4);
            writtenBytes += 4;

            //Write the block size used
            outputStream.Write(BitConverter.GetBytes(_blockSize), 0, 4);
            writtenBytes += 4;
            return writtenBytes;
        }

        /// <summary>
        /// Decompresses a file stream using LZ41 file format for FSO
        /// Supports random access, to use that you need to pass a offset and a length
        /// Support VP file stream for in and out, for this it is needed to pass the compressedFilesize
        /// The streams are not closed or disposed.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="outputStream"></param>
        /// <param name="compressedFileSize"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns>Extracted bytes</returns>
        /// <exception cref="IOException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="Exception"></exception>
        public static unsafe int LZ41_Stream_Decompress(Stream inputStream, Stream outputStream, int? compressedFileSize = null, int? offset = null, int? length = null)
        {
            //Validation
            if (!inputStream.CanRead)
                throw new IOException("Input stream can not be read.");
            if (inputStream.Length == 0)
                throw new IOException("Input stream can not empty.");
            if (compressedFileSize.HasValue && compressedFileSize > inputStream.Length)
                throw new ArgumentOutOfRangeException("The specified compressed file size is larger than the input stream length");
            if (!outputStream.CanWrite)
                throw new IOException("Output stream can not be written.");
            if (offset.HasValue && !length.HasValue)
                throw new ArgumentOutOfRangeException("If you pass an offset you also need to pass the length.");

            LZ4_streamDecode lz4StreamBody = new LZ4_streamDecode();
            LZ4_streamDecode* lz4Stream = &lz4StreamBody;
            int writtenBytes = 0;
            long initialPosition = inputStream.Position;
            BinaryReader br = new BinaryReader(inputStream);
            List<int> offsets = new List<int>();

            if (!compressedFileSize.HasValue)
                compressedFileSize = (int)inputStream.Length;

            if (!offset.HasValue)
                offset = 0;

            if (length.HasValue && length.Value <= 0)
                throw new Exception("Length has be greater than 0");

            /*Read Header*/
            if (new string(br.ReadChars(4)) != "LZ41")
                throw new Exception("Header mismatch");

            /* Num Offsets */
            inputStream.Position = initialPosition + compressedFileSize.Value - 12;
            int numOffsets = br.ReadInt32();

            /* File Size */
            if(!length.HasValue) 
                length = br.ReadInt32();
            else
                inputStream.Position += 4;

            /* Block Size */
            int blockSize = br.ReadInt32();

            /* Read the offsets tail */
            inputStream.Position = initialPosition + (compressedFileSize.Value - 12 - (numOffsets * 4));

            for(var i=0; i<numOffsets;i++)
            {
                offsets.Add(br.ReadInt32());
            }

            /* The blocks [currentBlock to endBlock] contain the data we want */
            int currentBlock = offset.Value / blockSize;
            int endBlock = ((offset.Value + length.Value - 1) / blockSize) + 1;

            /* Seek to the first block to read */
            inputStream.Position = initialPosition + offsets[currentBlock];

            offset = offset % blockSize;

            /* Start decoding */

            for (; currentBlock < endBlock; ++currentBlock)
            {
                /* The difference in offsets is the size of the block */
                int cmpBytes = offsets[currentBlock + 1] - offsets[currentBlock];
                byte[] cmpBuf = new byte[lz4.LZ4_compressBound(blockSize)];
                inputStream.Read(cmpBuf,0, cmpBytes);
                fixed (byte* cmpPtr = cmpBuf)
                {
                    fixed (byte* decBuf = new byte[blockSize])
                    {
                        int decBytes = lz4.LZ4_decompress_safe_continue(lz4Stream, cmpPtr, decBuf, cmpBytes, blockSize);
                        if (decBytes <= 0)
                            throw new Exception("Error while compressing the block.");

                        /* Write out the part of the data we care about */
                        int blockLength = ((length.Value) < ((decBytes - offset.Value)) ? (length.Value) : ((decBytes - offset.Value)));

                        var byteArray = new byte[blockLength];
                        System.Runtime.InteropServices.Marshal.Copy(new IntPtr(decBuf + offset.Value), byteArray, 0, blockLength);
                        outputStream.Write(byteArray, 0, blockLength);
                        writtenBytes += blockLength;
                        offset = 0;
                        length -= blockLength;
                    }
                }
            }
            return writtenBytes;
        }
    }
}
