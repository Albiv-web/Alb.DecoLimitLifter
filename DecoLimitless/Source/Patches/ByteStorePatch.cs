using HarmonyLib;

namespace DecoLimitLifter.Patches
{
    // Ensures global blueprint buffer (ByteStore.MegaBytes) is big enough.
    [HarmonyPatch]
    internal static class ByteStorePatch
    {
        internal static void EnsureMegaBytes()
        {
            var cur = BrilliantSkies.DataManagement.Serialisation.ByteStore.MegaBytes;
            int want = System.Math.Max(cur?.Length ?? 0, DecoLimitLifter.DecoLimits.SaveBufferBytes);
            if (cur == null || cur.Length < want)
                BrilliantSkies.DataManagement.Serialisation.ByteStore.MegaBytes = new byte[want];
        }

        // Safety net: if something constructs a SuperSaver before OnLoad, still resize.
        [HarmonyPostfix, HarmonyPatch(typeof(BrilliantSkies.DataManagement.Serialisation.SuperSaver), MethodType.Constructor)]
        private static void AfterFirstSuperSaverCtor() => EnsureMegaBytes();
    }
}
