/*
   LZ4 - Fast LZ compression algorithm
   Copyright (C) 2011-present, Yann Collet.

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

   You can contact the original author at :
    - LZ4 homepage : http://www.lz4.org
    - LZ4 source repository : https://github.com/lz4/lz4

   ***

	 This is a translation of the original lz4 sources to C#
   lz4.c / lz4hc.c / xxhash.c
	  - source repository : https://github.com/IonKiwi/lz4.managed
*/

namespace IonKiwi.lz4 {
	internal unsafe struct LZ4_stream {
		public fixed ulong table[lz4.LZ4_STREAM_MINSIZE];
		public LZ4_stream_t_internal internal_donotuse;
	}

	internal unsafe struct LZ4_stream_t_internal {
		public fixed uint hashTable[lz4.LZ4_HASH_SIZE_U32];
		public byte* dictionary;
		public LZ4_stream_t_internal* dictCtx;
		public uint currentOffset;
		public uint tableType;
		public uint dictSize;
	}

	internal unsafe struct LZ4_streamDecode {
		public fixed byte minStateSize[lz4.LZ4_STREAMDECODE_MINSIZE];
		public LZ4_streamDecode_t_internal internal_donotuse;
	}

	internal unsafe struct LZ4_streamDecode_t_internal {
		public byte* externalDict;
		public byte* prefixEnd;
		// translation: size_t -> uint
		public uint extDictSize;
		public uint prefixSize;
	}

	internal unsafe struct LZ4_streamHC {
		public fixed byte minStateSize[lz4.LZ4_STREAMHC_MINSIZE];
		public LZ4HC_CCtx_internal internal_donotuse;
	}

	internal unsafe struct LZ4HC_CCtx_internal {
		public fixed uint hashTable[lz4.LZ4HC_HASHTABLESIZE];
		public fixed ushort chainTable[lz4.LZ4HC_MAXD];
		public byte* end;       /* next block here to continue on current prefix */
		public byte* prefixStart;  /* Indexes relative to this position */
		public byte* dictStart; /* alternate reference for extDict */
		public uint dictLimit;       /* below that point, need extDict */
		public uint lowLimit;        /* below that point, no more dict */
		public uint nextToUpdate;    /* index from which to continue dictionary update */
		public short compressionLevel;
		public sbyte favorDecSpeed;   /* favor decompression speed if this flag set,
                                  otherwise, favor compression ratio */
		public sbyte dirty;           /* stream has to be fully reset if this flag is set */
		public LZ4HC_CCtx_internal* dictCtx;
	}

	internal struct LZ4HC_optimal_t {
		public int price;
		public int off;
		public int mlen;
		public int litlen;
	}

	internal struct LZ4HC_match_t {
		public int off;
		public int len;
	}

	internal enum tableType_t { clearedTable = 0, byPtr, byU32, byU16 }

	internal enum limitedOutput_directive {
		notLimited = 0,
		limitedOutput = 1,
		fillOutput = 2
	}

	internal enum dict_directive { noDict = 0, withPrefix64k, usingExtDict, usingDictCtx }

	internal enum dictIssue_directive { noDictIssue = 0, dictSmall }

	internal enum earlyEnd_directive { decode_full_block = 0, partial_decode = 1 }

	internal enum dictCtx_directive { noDictCtx, usingDictCtxHc }

	internal enum lz4hc_strat_e { lz4hc, lz4opt }

	internal enum repeat_state_e { rep_untested, rep_not, rep_confirmed }

	internal enum HCfavor_e { favorCompressionRatio = 0, favorDecompressionSpeed }

	internal unsafe struct cParams_t {
		public lz4hc_strat_e strat;
		public int nbSearches;
		public uint targetLength;
	}

	internal unsafe struct XXH32_state {
		public uint total_len_32;
		public uint large_len;
		public uint v1;
		public uint v2;
		public uint v3;
		public uint v4;
		public fixed uint mem32[4];
		public uint memsize;
		public uint reserved;
	}

	internal enum XXH_errorcode { XXH_OK = 0, XXH_ERROR }

	internal enum XXH_endianess { XXH_bigEndian = 0, XXH_littleEndian = 1 }

	internal enum XXH_alignment { XXH_aligned, XXH_unaligned }
}
