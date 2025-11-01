using HarmonyLib;

namespace DecoLimitLifter.Patches
{
    [HarmonyPatch]
    internal static class SuperSaverBuffersPatch
    {
        private const string PoolTypeName =
            "BrilliantSkies.DataManagement.Serialisation.SuperSaverReusableByteArray";

        private static bool _initialized;

        // --- Compatibility shim for Plugin.cs ---
        internal static void OnBootEnsurePools() => EnsurePoolsOnce();

        private static void EnsurePoolsOnce()
        {
            if (_initialized) return;
            _initialized = true;

            var t = AccessTools.TypeByName(PoolTypeName);
            if (t == null) return;

            var fData = AccessTools.Field(t, "DataSorted");
            var fHeader = AccessTools.Field(t, "Header");
            if (fData == null || fHeader == null) return;

            var curData = (byte[])fData.GetValue(null);
            var curHead = (byte[])fHeader.GetValue(null);

            // Create if null. Never shrink (avoid thrash).
            if (curData == null) fData.SetValue(null, new byte[DecoLimits.VanillaDataSortedBytes]);
            if (curHead == null) fHeader.SetValue(null, new byte[DecoLimits.VanillaHeaderBytes]);
        }

        // Run once on first SuperSaver construction as a safety net.
        [HarmonyPatch(typeof(BrilliantSkies.DataManagement.Serialisation.SuperSaver), MethodType.Constructor)]
        [HarmonyPrefix] private static void CtorPrefix() => EnsurePoolsOnce();
    }
}
