using System;

namespace DecoLimitLifter
{
    /// Central knobs used by the other patches.
    internal static class DecoLimits
    {
        // --- Vanilla pools for SuperSaver reusable arrays ---
        // (Matching SuperSaverReusableByteArray)
        public const int VanillaDataSortedBytes = 2_000_000;
        public const int VanillaHeaderBytes = 70_000;


        // Upper bounds we allow our guards to grow to (safety clamps).
        public const int MaxDataSortedBytes = 64 * 1024 * 1024;  // 64MB hard ceiling
        public const int MaxHeaderBytes = 4 * 1024 * 1024;  // 4.1MB  header ceiling

        // --- Blueprint save "list" (the big one used by BlueprintConverter) ---
        // Vanilla for small/normal saves so the game feels snappy.
        public const int VanillaSaveBufferBytes = 20_000_000; // ~20MB (FtD default: ~10MB)
        public const int MinSaveBufferBytes = 20_000_000; // never below vanilla (FtD default: ~10MB)
        public const int MaxSaveBufferBytes = 256 * 1024 * 1024; // safety upper bound

        // The value the transpiler uses when BlueprintConverter allocates the array.
        // We change this per-save (see BlueprintConverterBufferPatch).
        public static int SaveBufferBytes = VanillaSaveBufferBytes;

        // Decoration soft cap (runtime tweaked by DecoLimitsPatch, also clarity)
        public const int MaxDecorations = 100_000;

        // Conservative per-deco serialization budget for the *outer* save buffer.
        // (Header+payload + some general overhead). Tuned to avoid index OOR while
        // keeping small saves tiny.
        private const int BytesPerDecoEstimate = 220;

        // Compute a per-save recommendation:
        //   vanilla base + Ndeco * estimate, then clamp to sane min/max.
        public static int RecommendSaveBufferForDecoCount(int decoCount)
        {
            try
            {
                long wanted = (long)VanillaSaveBufferBytes
                              + (long)Math.Max(0, decoCount) * BytesPerDecoEstimate;

                if (wanted < MinSaveBufferBytes) wanted = MinSaveBufferBytes;
                if (wanted > MaxSaveBufferBytes) wanted = MaxSaveBufferBytes;

                // round up to next power of two, fewer reallocations by the GC
                return NextPow2((int)wanted);
            }
            catch { return VanillaSaveBufferBytes; }
        }

        private static int NextPow2(int v)
        {
            if (v <= 0) return 1;
            v--;
            v |= v >> 1; v |= v >> 2; v |= v >> 4; v |= v >> 8; v |= v >> 16;
            return v + 1;
        }
    }
}
