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

            // 1) Read object id
            self.Id = ByteConversion.ConvertOutAnUnsignedInt(fullPacket, startFrom, bytesInTheObjectId);
            startFrom += (uint)bytesInTheObjectId;

            // 2) Peek 2 bytes to detect sentinel
            if (startFrom + 2U <= (uint)fullPacket.Length)
            {
                ushort maybe = (ushort)ByteConversion.ConvertOut(fullPacket, startFrom, 2);
                if (maybe == SENTINEL && startFrom + 10U <= (uint)fullPacket.Length)
                {
                    // New: [0xFFFF][UInt32 headerLen][UInt32 dataLen]
                    startFrom += 2U;
                    uint headerLen = ByteConversion.ConvertOut(fullPacket, startFrom, 4); startFrom += 4U;
                    self._totalDataLengthSorted = ByteConversion.ConvertOut(fullPacket, startFrom, 4); startFrom += 4U;

                    self.HeaderCount = headerLen / 7U;
                    Ensure(ref self.Header, headerLen);
                    Ensure(ref self.DataSorted, self._totalDataLengthSorted);

                    CopyIn(fullPacket, ref startFrom, self.Header, headerLen);
                    CopyIn(fullPacket, ref startFrom, self.DataSorted, self._totalDataLengthSorted);

                    InitReaderSegment(self, headerLen);
                    return self.Id;
                }
            }

            // Legacy: [UInt16 headerLen][UInt16 pad][var-chunk dataLen]
            uint headerLen2 = ByteConversion.ConvertOut(fullPacket, startFrom, 2); startFrom += 2U;
            self.HeaderCount = headerLen2 / 7U;

            // pad
            ByteConversion.ConvertOut(fullPacket, startFrom, 2); startFrom += 2U;

            // var-chunk dataLen
            uint total = 0U;
            for (int i = 0; i < MAX_CHUNKS; i++)
            {
                uint piece = ByteConversion.ConvertOut(fullPacket, startFrom, 2);
                total += piece;
                startFrom += 2U;
                if (piece < CHUNK) break;
            }
            self._totalDataLengthSorted = total;

            Ensure(ref self.Header, headerLen2);
            Ensure(ref self.DataSorted, self._totalDataLengthSorted);

            CopyIn(fullPacket, ref startFrom, self.Header, headerLen2);
            CopyIn(fullPacket, ref startFrom, self.DataSorted, self._totalDataLengthSorted);

            InitReaderSegment(self, headerLen2);
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
            // Same as SuperLoader.HeaderByte(0, SortedStart)
            // SortedStart is the 2nd 3-byte field in 7-byte header entries → read legacy-elements at offset +3
            uint firstSortedStart = ByteConversion.ConvertOutLegacyElements(self.Header, 3U);
            self._datasWrittenSorted = 0U;
            self._readerLengthOfSortedSegment = firstSortedStart;
        }

        private static void Ensure(ref byte[] buf, uint len)
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
