using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BrilliantSkies.DataManagement.Serialisation;
using HarmonyLib;

namespace DecoLimitLifter.Patches
{
    // Patch *every* Deserialise that starts with:
    // (byte[] fullPacket, ref uint startFrom, byte bytesInTheObjectId)
    [HarmonyPatch(typeof(SuperLoader))]
    internal static class SuperLoader_Deserialise_All_Patch
    {
        [HarmonyTargetMethods]
        static IEnumerable<MethodBase> TargetMethods()
        {
            return typeof(SuperLoader)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => m.Name == "Deserialise")
                .Where(m =>
                {
                    var p = m.GetParameters();
                    if (p.Length < 3) return false;
                    return p[0].ParameterType == typeof(byte[])
                        && p[1].ParameterType.IsByRef
                        && p[1].ParameterType.GetElementType() == typeof(uint)
                        && p[2].ParameterType == typeof(byte);
                });
        }

        // Prefix: call loader and skip the original. Logging is gated by DclDebug.
        static bool Prefix(
            SuperLoader __instance,
            [HarmonyArgument(0)] byte[] fullPacket,
            [HarmonyArgument(1)] ref uint startFrom,
            [HarmonyArgument(2)] byte bytesInTheObjectId,
            ref ulong __result)
        {
            // optional trace debug blablabla
            if (DecoLimitLifter.DclDebug.Enabled)
                DecoLimitLifter.DclDebug.Log($">>> SuperLoader.Deserialise PREFIX  startFrom={startFrom} bytesId={bytesInTheObjectId}");

            __result = DecoLimitLifter.ExtendedSerialization.ExtendedSuperLoader
                .Deserialise(__instance, fullPacket, ref startFrom, bytesInTheObjectId);

            if (DecoLimitLifter.DclDebug.Enabled)
            {
                // quick state snapshot after our loader returns
                uint hc = DecoLimitLifter.ExtendedSerialization.Priv.GetHeaderCount(__instance);
                uint tot = DecoLimitLifter.ExtendedSerialization.Priv.TotalDataLenRef(__instance);
                uint rl = DecoLimitLifter.ExtendedSerialization.Priv.ReaderLenRef(__instance);
                uint dw = DecoLimitLifter.ExtendedSerialization.Priv.DatasWrittenRef_Loader(__instance);
                DecoLimitLifter.DclDebug.Log($"<<< SuperLoader.Deserialise EXIT    id={__result} HC={hc} total={tot} readerLen={rl} written={dw}");
            }

            return false; // skip original
        }
    }
}
