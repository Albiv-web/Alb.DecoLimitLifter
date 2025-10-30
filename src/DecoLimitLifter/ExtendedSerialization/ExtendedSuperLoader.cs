using System;
using BrilliantSkies.Core.Serialisation.Bytes;
using BrilliantSkies.DataManagement.Serialisation;

namespace DecoLimitLifter.ExtendedSerialization
{
    public static class ExtendedSuperLoader
    {
        private const ushort SENTINEL = 0xFFFF;
        private const uint CHUNK = 65535U;
        private const int MAX_CHUNKS = 100;

        public static ulong Deserialise(SuperLoader self, byte[] fullPacket, ref uint startFrom, byte bytesInTheObjectId)
        {
            self._datasWrittenSorted = 0U;

            // object id
            self.Id = ByteConversion.ConvertOutAnUnsignedInt(fullPacket, startFrom, bytesInTheObjectId);
            startFrom += (uint)bytesInTheObjectId;

            // Not enough bytes to even peek? Treat as legacy (will error out upstream if truly invalid)
            if (startFrom + 2U > (uint)fullPacket.Length)
                return LegacyAfterId(self, fullPacket, ref startFrom);

            ushort maybe = (ushort)ByteConversion.ConvertOut(fullPacket, startFrom, 2);

            if (maybe == SENTINEL && startFrom + 10U <= (uint)fullPacket.Length)
            {
                // New: [0xFFFF][headerLen:UInt32][dataLen:UInt32]
                startFrom += 2U;
                uint headerLen = ByteConversion.ConvertOut(fullPacket, startFrom, 4); startFrom += 4U;
                self._totalDataLengthSorted = ByteConversion.ConvertOut(fullPacket, startFrom, 4); startFrom += 4U;

                self.HeaderCount = headerLen / 7U;

                EnsureBuffer(ref self.Header, headerLen);
                EnsureBuffer(ref self.DataSorted, self._totalDataLengthSorted);

                CopyIn(fullPacket, ref startFrom, self.Header, headerLen);
                CopyIn(fullPacket, ref startFrom, self.DataSorted, self._totalDataLengthSorted);

                InitReaderSegment(self, headerLen);
                return self.Id;
            }

            // Legacy
            return LegacyAfterId(self, fullPacket, ref startFrom);
        }

        private static ulong LegacyAfterId(SuperLoader self, byte[] fullPacket, ref uint startFrom)
        {
            uint headerLen = ByteConversion.ConvertOut(fullPacket, startFrom, 2); startFrom += 2U;
            self.HeaderCount = headerLen / 7U;

            // pad
            ByteConversion.ConvertOut(fullPacket, startFrom, 2); startFrom += 2U;

            // var-chunk length
            uint total = 0U;
            for (int i = 0; i < MAX_CHUNKS; i++)
            {
                uint piece = ByteConversion.ConvertOut(fullPacket, startFrom, 2);
                total += piece;
                startFrom += 2U;
                if (piece < CHUNK) break;
            }
            self._totalDataLengthSorted = total;

            EnsureBuffer(ref self.Header, headerLen);
            EnsureBuffer(ref self.DataSorted, self._totalDataLengthSorted);

            CopyIn(fullPacket, ref startFrom, self.Header, headerLen);
            CopyIn(fullPacket, ref startFrom, self.DataSorted, self._totalDataLengthSorted);

            InitReaderSegment(self, headerLen);
            return self.Id;
        }

        private static void InitReaderSegment(SuperLoader self, uint headerLen)
        {
            if (headerLen == 0U)
            {
                self._readerLengthOfSortedSegment = self._totalDataLengthSorted;
                self._datasWrittenSorted = 0U;
                return;
            }

            // Replicate SuperLoader.HeaderByte(..., SortedStart):
            uint firstSortedStart = ByteConversion.ConvertOutLegacyElements(self.Header, /*0*7 +*/ 3U);
            self._datasWrittenSorted = 0U;
            self._readerLengthOfSortedSegment = firstSortedStart;
        }

        private static void EnsureBuffer(ref byte[] buf, uint len)
        {
            if (buf == null || (uint)buf.Length < len)
                buf = new byte[len];
        }

        private static void CopyIn(byte[] src, ref uint cursor, byte[] dst, uint len)
        {
            for (uint i = 0; i < len; i++)
                dst[i] = src[cursor++];
        }
    }
}
