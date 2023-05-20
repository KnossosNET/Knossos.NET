/*
   lz4.managed - C# translation of lz4
   Copyright (C) 2019-present, Ewout van der Linden.

   BSD 2-Clause License (http://www.opensource.org/licenses/bsd-license.php)

   Redistribution and use in source and binary forms, with or without
   modification, are permitted provided that the following conditions are
   met:

       * Redistributions of source code must retain the above copyright
   notice, this list of conditions and the following disclaimer.
       * Redistributions in binary form must reproduce the above
   copyright notice, this list of conditions and the following disclaimer
   in the documentation and/or other materials provided with the
   distribution.

   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
   "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
   LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
   A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
   OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
   SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
   LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
   DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
   THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
   OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

   lz4.managed
	  - source repository : https://github.com/IonKiwi/lz4.managed
*/

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace IonKiwi.lz4 {
	public static class LZ4Utility {

		public static byte[] Compress(byte[] input, LZ4FrameBlockMode blockMode = LZ4FrameBlockMode.Linked, LZ4FrameBlockSize blockSize = LZ4FrameBlockSize.Max1MB, LZ4FrameChecksumMode checksumMode = LZ4FrameChecksumMode.Content, long? maxFrameSize = null, bool highCompression = false) {
			if (input == null) {
				throw new ArgumentNullException("input");
			}
			return Compress(input, 0, input.Length, blockMode, blockSize, checksumMode, maxFrameSize, highCompression);
		}

		public static byte[] Compress(byte[] input, int inputOffset, int inputLength, LZ4FrameBlockMode blockMode = LZ4FrameBlockMode.Linked, LZ4FrameBlockSize blockSize = LZ4FrameBlockSize.Max1MB, LZ4FrameChecksumMode checksumMode = LZ4FrameChecksumMode.Content, long? maxFrameSize = null, bool highCompression = false) {
			if (input == null) {
				throw new ArgumentNullException("input");
			}
			else if (inputOffset < 0) {
				throw new ArgumentOutOfRangeException("inputOffset");
			}
			else if (inputLength <= 0) {
				throw new ArgumentOutOfRangeException("inputLength");
			}
			else if (inputOffset + inputLength > input.Length) {
				throw new ArgumentOutOfRangeException("inputOffset+inputLength");
			}

			using (var ms = new MemoryStream()) {
				using (var lz4 = LZ4Stream.CreateCompressor(ms, LZ4StreamMode.Write, blockMode, blockSize, checksumMode, maxFrameSize, highCompression, true)) {
					lz4.Write(input, inputOffset, inputLength);
				}
				return ms.ToArray();
			}
		}

		public static byte[] Decompress(byte[] input) {
			if (input == null) {
				throw new ArgumentNullException("input");
			}
			return Decompress(input, 0, input.Length);
		}

		public static byte[] Decompress(byte[] input, int inputOffset, int inputLength) {
			if (input == null) {
				throw new ArgumentNullException("input");
			}
			else if (inputOffset < 0) {
				throw new ArgumentOutOfRangeException("inputOffset");
			}
			else if (inputLength < 3) {
				throw new ArgumentOutOfRangeException("inputLength");
			}
			else if (inputOffset + inputLength > input.Length) {
				throw new ArgumentOutOfRangeException("inputOffset+inputLength");
			}

			using (var ms = new MemoryStream()) {
				using (var lz4 = LZ4Stream.CreateDecompressor(ms, LZ4StreamMode.Write, true)) {
					lz4.Write(input, inputOffset, inputLength);
				}
				return ms.ToArray();
            }
        }
	}
}
