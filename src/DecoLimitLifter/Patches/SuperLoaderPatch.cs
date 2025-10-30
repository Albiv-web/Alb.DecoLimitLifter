using BrilliantSkies.DataManagement.Serialisation;
using HarmonyLib;

namespace DecoLimitLifter.Patches
{
    [HarmonyPatch(typeof(SuperLoader), nameof(SuperLoader.Deserialise))]
    internal static class SuperLoader_Deserialise_Patch
    {
        static bool Prefix(SuperLoader __instance, byte[] fullPacket, ref uint startFrom, byte bytesInTheObjectId, ref ulong __result)
        {
            __result = ExtendedSerialization.ExtendedSuperLoader.Deserialise(__instance, fullPacket, ref startFrom, bytesInTheObjectId);
            return false; // skip original
        }
    }
}

