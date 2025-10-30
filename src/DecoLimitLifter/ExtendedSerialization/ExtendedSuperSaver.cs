using System;
using BrilliantSkies.Core.Serialisation.Bytes;
using BrilliantSkies.DataManagement.Serialisation;

namespace DecoLimitLifter.ExtendedSerialization
{
    public static class ExtendedSuperSaver
    {
        private const ushort SENTINEL = 0xFFFF;
        private const uint CHUNK = 65535U;
        private const int MAX_CHUNKS = 100;

        public static void Serialise(SuperSaver self, byte[] list, ref uint startByte, ulong objectId, byte bytesToWrite)
        {
            // object id (unchanged)
            ByteConversion.ConvertInAnUnsignedInt(list, startByte, bytesToWrite, objectId);
            startByte += (uint)bytesToWrite;

            uint headerLenBytes = self.HeaderCount * 7U;
            uint dataLenBytes = self._datasWrittenSorted;

            bool fitsLegacy = headerLenBytes <= CHUNK && NeededChunks(dataLenBytes) <= MAX_CHUNKS;

            if (fitsLegacy)
            {
                // Legacy: [headerLen:UInt16][pad:UInt16=0][dataLen:varChunks]
                ByteConversion.ConvertIn(list, startByte, 2, headerLenBytes); startByte += 2U;
                ByteConversion.ConvertIn(list, startByte, 2, 0U);           startByte += 2U;

                WriteVarChunks(list, ref startByte, dataLenBytes);

                CopyBytes(self.Header, headerLenBytes, list, ref startByte);
                CopyBytes(self.DataSorted, dataLenBytes, list, ref startByte);
            }
            else
            {
                // New: [0xFFFF][headerLen:UInt32][dataLen:UInt32]
                ByteConversion.ConvertIn(list, startByte, 2, SENTINEL);     startByte += 2U;
                ByteConversion.ConvertIn(list, startByte, 4, headerLenBytes);startByte += 4U;
                ByteConversion.ConvertIn(list, startByte, 4, dataLenBytes);  startByte += 4U;

                CopyBytes(self.Header, headerLenBytes, list, ref startByte);
                CopyBytes(self.DataSorted, dataLenBytes, list, ref startByte);
            }
        }

        private static void WriteVarChunks(byte[] list, ref uint start, uint toWrite)
        {
            for (int i = 0; i < MAX_CHUNKS; i++)
            {
                uint v = Math.Min(CHUNK, toWrite);
                ByteConversion.ConvertIn(list, start, 2, v);
                start += 2U;
                if (toWrite < CHUNK) break;
                toWrite -= CHUNK;
            }
        }

        private static int NeededChunks(uint n) => n == 0 ? 1 : (int)((n + (CHUNK - 1)) / CHUNK);

        private static void CopyBytes(byte[] src, uint len, byte[] dst, ref uint cursor)
        {
            for (uint i = 0; i < len; i++)
            {
                dst[cursor++] = src[i];
            }
        }
    }
}
