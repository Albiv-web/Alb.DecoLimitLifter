using System;
using HarmonyLib;
using BrilliantSkies.DataManagement.Serialisation;

namespace DecoLimitLifter.Patches
{
    // Allocate minimum needed buffer for ConvertToReader (small saves stay small).
    [HarmonyPatch(typeof(SuperSaver), nameof(SuperSaver.ConvertToReader))]
    internal static class SuperSaver_ConvertToReader_BufferPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(SuperSaver __instance, ref SuperLoader __result)
        {
            // Compute exact legacy-container size:
            uint headerLen = __instance.HeaderCount * 7U;
            uint dataLen = __instance._datasWrittenSorted;
            uint chunks = dataLen == 0 ? 1U : (dataLen + 65534U) / 65535U; // ceil
            uint meta = 1U + 2U + 2U + (chunks * 2U); // id + headerLen + pad + chunked dataLen

            uint needed = headerLen + dataLen + meta;

            // Keep a tiny floor to avoid pathological tiny arrays; no big minimum.
            const int Floor = 64 * 1024; // 64 KB is enough for tiny blueprints
            int size = (int)Math.Min(
                Math.Max((int)needed, Floor),
                DecoLimits.MaxSaveBufferBytes
            );

            var buf = new byte[size];
            uint off = 0;

            __instance.Serialise(buf, ref off, 0UL, 1);
            off = 0;

            var loader = new SuperLoader();
            loader.Deserialise(buf, ref off, 1);
            if (loader.Id > 0UL) throw new Exception("Why is the ID not 0?");

            __result = loader;
            return false; // skip original
        }
    }
}
