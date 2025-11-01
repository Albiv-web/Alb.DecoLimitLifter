using System;
using System.Reflection;                       // BindingFlags
using BrilliantSkies.Core.Serialisation.Bytes;
using BrilliantSkies.DataManagement.Serialisation;

namespace DecoLimitLifter.ExtendedSerialization
{
    public static class ExtendedSuperLoader
    {
        private const ushort SENTINEL = 0xFFFF;
        private const uint CHUNK = 65535U;
        private const int MAX_CHUNKS = 100;

        private static bool _oncePathToast;   // kept for parity, but only used if DclDebug.Enabled
        private static bool _onceClampToast;  // kept for parity, but only used if DclDebug.Enabled

        /// <summary>
        /// Hybrid loader:
        /// - Legacy:   [UInt16 headerLen][UInt16 pad][var-chunk dataLen]
        /// - Sentinel: [0xFFFF][UInt32 headerLen][UInt32 dataLen]
        /// </summary>
        public static ulong Deserialise(SuperLoader self, byte[] fullPacket, ref uint startFrom, byte bytesInTheObjectId)
        {
            if (fullPacket == null) throw new ArgumentNullException(nameof(fullPacket));

            // vanilla semantics: start-of-read is 0
            Priv.SetDatasWrittenRef_Loader(self, 0U);

            // 1) object id
            if (!Have(fullPacket, startFrom, bytesInTheObjectId))
            {
                DclDebug.Log("[DecoLimitLifter] Loader: not enough bytes for objectId.");
                return self.Id;
            }

            self.Id = ByteConversion.ConvertOutAnUnsignedInt(fullPacket, startFrom, bytesInTheObjectId);
            startFrom += (uint)bytesInTheObjectId;

            // 2) check sentinel
            bool isSentinel = false;
            if (Have(fullPacket, startFrom, 2))
            {
                ushort maybe = (ushort)ByteConversion.ConvertOut(fullPacket, startFrom, 2);
                isSentinel = (maybe == SENTINEL) && Have(fullPacket, startFrom + 2U, 8);
            }

            if (isSentinel)
            {
                if (!_oncePathToast && DclDebug.Enabled)
                {
                    _oncePathToast = true;
                    DclDebug.Log("[DecoLimitLifter] Loader path = SENTINEL");
                }

                startFrom += 2U;
                uint headerLen = ByteConversion.ConvertOut(fullPacket, startFrom, 4); startFrom += 4U;
                uint dataLen = ByteConversion.ConvertOut(fullPacket, startFrom, 4); startFrom += 4U;

                Priv.SetHeaderCount(self, headerLen / 7U);
                Priv.SetTotalDataLenRef(self, dataLen);

                EnsureHeader(self, headerLen);
                EnsureData(self, dataLen);

                uint hdrStart = startFrom;
                DebugBytes("SENTINEL preCopy HDR src", fullPacket, hdrStart, headerLen, 32);
                CopyBytes(fullPacket, ref startFrom, Priv.HeaderRef_Loader(self), headerLen);
                DebugHeader0(self, "SENTINEL postCopy HDR");

                uint datStart = startFrom;
                DebugBytes("SENTINEL preCopy DAT src", fullPacket, datStart, dataLen, 32);
                CopyBytes(fullPacket, ref startFrom, Priv.DataSortedRef_Loader(self), dataLen);
                DebugBytes("SENTINEL postCopy DAT dst", Priv.DataSortedRef_Loader(self), 0, Math.Min(32U, dataLen), 32);

                InitReaderSegment(self);               // vanilla post-state
                SetReaderLen_DirectFallback(self);      // belt & braces
                return self.Id;
            }

            // 3) legacy
            if (!_oncePathToast && DclDebug.Enabled)
            {
                _oncePathToast = true;
                DclDebug.Log("[DecoLimitLifter] Loader path = LEGACY");
            }

            if (!Have(fullPacket, startFrom, 2))
            {
                DclDebug.Log("[DecoLimitLifter] Loader: not enough bytes for legacy headerLen.");
                return self.Id;
            }

            uint headerLen2 = ByteConversion.ConvertOut(fullPacket, startFrom, 2); startFrom += 2U;
            Priv.SetHeaderCount(self, headerLen2 / 7U);

            // pad
            if (Have(fullPacket, startFrom, 2))
            {
                ByteConversion.ConvertOut(fullPacket, startFrom, 2);
                startFrom += 2U;
            }
            else
            {
                DclDebug.Log("[DecoLimitLifter] Loader: missing legacy pad (2).");
            }

            // dataLen in ≤100 chunks
            uint total = 0U;
            for (int i = 0; i < MAX_CHUNKS; i++)
            {
                if (!Have(fullPacket, startFrom, 2))
                {
                    DclDebug.Log("[DecoLimitLifter] Loader: truncated legacy dataLen.");
                    break;
                }
                uint piece = ByteConversion.ConvertOut(fullPacket, startFrom, 2); startFrom += 2U;
                total += piece;
                if (piece < CHUNK) break;
            }
            Priv.SetTotalDataLenRef(self, total);

            EnsureHeader(self, headerLen2);
            EnsureData(self, total);

            // copy header
            uint hdrStart2 = startFrom;
            DebugBytes("LEGACY preCopy HDR src", fullPacket, hdrStart2, headerLen2, 32);
            CopyBytes(fullPacket, ref startFrom, Priv.HeaderRef_Loader(self), headerLen2);
            DebugHeader0(self, "LEGACY postCopy HDR");

            // copy data
            uint datStart2 = startFrom;
            DebugBytes("LEGACY preCopy DAT src", fullPacket, datStart2, total, 32);
            CopyBytes(fullPacket, ref startFrom, Priv.DataSortedRef_Loader(self), total);
            DebugBytes("LEGACY postCopy DAT dst", Priv.DataSortedRef_Loader(self), 0, Math.Min(32U, total), 32);

            // vanilla post-state
            InitReaderSegment(self);
            SetReaderLen_DirectFallback(self);

            return self.Id;
        }

        // -------------------- helpers --------------------

        private static void InitReaderSegment(SuperLoader self)
        {
            // Vanilla behavior: startofread is 0; segment length depends on headers.
            Priv.SetDatasWrittenRef_Loader(self, 0U);

            if (Priv.GetHeaderCount(self) == 0U)
            {
                // No headers: scan the whole buffer.
                Priv.SetReaderLenRef(self, Priv.TotalDataLenRef(self));
            }
            else
            {
                // First header’s SortedStart (3-byte value) is the *length* of the first segment.
                uint firstSortedStart = ByteConversion.ConvertOutLegacyElements(Priv.HeaderRef_Loader(self), 3U);
                Priv.SetReaderLenRef(self, firstSortedStart);
            }
        }

        private static bool Have(byte[] src, uint from, uint needed)
        {
            return from <= (uint)src.Length && ((uint)src.Length - from) >= needed;
        }

        private static void EnsureHeader(SuperLoader self, uint len)
        {
            var buf = Priv.HeaderRef_Loader(self);
            if (buf == null || (uint)buf.Length < len)
                Priv.SetHeaderRef_Loader(self, new byte[(int)len]);
        }

        private static void EnsureData(SuperLoader self, uint len)
        {
            var buf = Priv.DataSortedRef_Loader(self);
            if (buf == null || (uint)buf.Length < len)
                Priv.SetDataSortedRef_Loader(self, new byte[(int)len]);
        }

        // Safe copy that clamps to available bytes and logs once if clamped (logs only when DclDebug.Enabled).
        private static void CopyInSafe(byte[] src, ref uint cursor, byte[] dst, uint len)
        {
            uint available = (cursor <= (uint)src.Length) ? (uint)src.Length - cursor : 0U;
            if (len > available)
            {
                if (!_onceClampToast && DclDebug.Enabled)
                {
                    _onceClampToast = true;
                    DclDebug.Log($"[DecoLimitLifter] Loader: declared copy {len}B but only {available}B available. Clamping.");
                }
                len = available;
            }

            // clamp to dest capacity too
            uint dcap = (uint)dst.Length;
            if (len > dcap)
            {
                if (!_onceClampToast && DclDebug.Enabled)
                {
                    _onceClampToast = true;
                    DclDebug.Log($"[DecoLimitLifter] Loader: copy {len}B exceeds dst capacity {dcap}B. Clamping.");
                }
                len = dcap;
            }

            for (uint i = 0; i < len; i++)
                dst[i] = src[cursor++];

            // NOTE: behavior kept identical to working build: we do NOT force cursor to end.
        }

        private static void CopyBytes(byte[] src, ref uint cursor, byte[] dst, uint len)
        {
            // clamp to src & dst (same as your working build)
            uint available = (cursor <= (uint)src.Length) ? (uint)src.Length - cursor : 0U;
            if (len > available) len = available;
            if (dst == null) return;
            if (len > (uint)dst.Length) len = (uint)dst.Length;

            // fast copy
            Buffer.BlockCopy(src, (int)cursor, dst, 0, (int)len);
            cursor += len;
        }

        private static void DebugBytes(string tag, byte[] buf, uint start, uint len, int maxToShow)
        {
            if (!DclDebug.Enabled) return;

            uint show = Math.Min((uint)maxToShow, len);
            if (buf == null || start >= (uint)buf.Length)
            {
                DclDebug.Log($"{tag}: buf=null or OOR (start={start})");
                return;
            }
            uint avail = (uint)buf.Length - start;
            show = Math.Min(show, avail);
            System.Text.StringBuilder sb = new System.Text.StringBuilder(maxToShow * 3 + 64);
            for (uint i = 0; i < show; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(buf[start + i].ToString("X2"));
            }
            DclDebug.Log($"{tag}: start={start} len={len} bytes[{show}] = {sb}");
        }

        private static void DebugHeader0(SuperLoader self, string tag)
        {
            if (!DclDebug.Enabled) return;

            try
            {
                var hdr = Priv.HeaderRef_Loader(self);
                if (hdr == null || hdr.Length < 6)
                {
                    DclDebug.Log($"{tag}: header too short ({hdr?.Length ?? 0})");
                    return;
                }
                uint h0Id = ByteConversion.ConvertOut(hdr, 0, 3);
                uint h0Start = ByteConversion.ConvertOutLegacyElements(hdr, 3);
                DclDebug.Log($"{tag}: H0 id={h0Id} start={h0Start} HC={Priv.GetHeaderCount(self)} total={Priv.TotalDataLenRef(self)}");
            }
            catch { /* debug only */ }
        }

        private static void SetReaderLen_DirectFallback(SuperLoader self)
        {
            // If Priv.SetReaderLenRef isn't pointing at real field, mirror the value via the private field as well.
            uint rl;
            if (Priv.GetHeaderCount(self) == 0U) rl = Priv.TotalDataLenRef(self);
            else rl = ByteConversion.ConvertOutLegacyElements(Priv.HeaderRef_Loader(self), 3U);

            try
            {
                var f = typeof(SuperLoader).GetField("_readerLengthOfSortedSegment",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (f != null) f.SetValue(self, rl);
            }
            catch { /* best effort */ }
        }
    }
}
