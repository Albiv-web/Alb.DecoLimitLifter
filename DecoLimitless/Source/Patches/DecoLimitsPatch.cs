using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DecoLimitLifter.Patches
{
    [HarmonyPatch]
    internal static class DecoLimitsPatch
    {
        // Touch a few likely entry points so the limit is raised early and stays raised.
        [HarmonyPatch]
        private static IEnumerable<MethodBase> TargetMethods()
        {
        
            var t = AccessTools.TypeByName(
                "BrilliantSkies.Ftd.Constructs.Modules.All.Decorations.AllConstructDecorations");
            if (t == null) yield break;

            foreach (var c in AccessTools.GetDeclaredConstructors(t))
                yield return c;

            foreach (var m in AccessTools.GetDeclaredMethods(t))
                if (m.Name == "NewDecoration" || m.Name == "CanAddHere" || m.Name == "ByteDataLoaded")
                    yield return m;
        }

        [HarmonyPrefix]
        private static void Prefix()
        {
            var t = AccessTools.TypeByName(
                "BrilliantSkies.Ftd.Constructs.Modules.All.Decorations.AllConstructDecorations");
            var f = AccessTools.Field(t, "_limitPerPacketManager");
            if (f == null) return;

            int cur = (int)f.GetValue(null);
            if (cur < DecoLimits.MaxDecorations)
                f.SetValue(null, DecoLimits.MaxDecorations);

        }
    }
}
