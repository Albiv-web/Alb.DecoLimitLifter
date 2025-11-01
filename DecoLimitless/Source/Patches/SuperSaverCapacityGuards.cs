using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace DecoLimitLifter.Patches
{
    internal static class GuardUtils
    {
        internal static int NextPow2(int v)
        {
            if (v <= 0) return 1;
            v--;
            v |= v >> 1; v |= v >> 2; v |= v >> 4; v |= v >> 8; v |= v >> 16;
            return v + 1;
        }
    }

    // ---------------- Header guard ----------------
    [HarmonyPatch(typeof(BrilliantSkies.DataManagement.Serialisation.SuperSaver),
                  nameof(BrilliantSkies.DataManagement.Serialisation.SuperSaver.WriteHeader))]
    internal static class SuperSaver_WriteHeader_Guard
    {
        // FAST accessors (created once)
        private static readonly AccessTools.FieldRef<
            BrilliantSkies.DataManagement.Serialisation.SuperSaver, byte[]> Ref_Header =
            AccessTools.FieldRefAccess<BrilliantSkies.DataManagement.Serialisation.SuperSaver, byte[]>("Header");

        private static readonly Func<BrilliantSkies.DataManagement.Serialisation.SuperSaver, uint> Get_HeaderCount =
            AccessTools.MethodDelegate<Func<BrilliantSkies.DataManagement.Serialisation.SuperSaver, uint>>(
                AccessTools.PropertyGetter(typeof(BrilliantSkies.DataManagement.Serialisation.SuperSaver), "HeaderCount"));

        [HarmonyPrefix]
        private static void Prefix(object __instance)
            => EnsureHeaderCapacity((BrilliantSkies.DataManagement.Serialisation.SuperSaver)__instance, headersToAdd: 1);

        private static void EnsureHeaderCapacity(BrilliantSkies.DataManagement.Serialisation.SuperSaver ss, uint headersToAdd)
        {
            ref var header = ref Ref_Header(ss);
            uint currentCount = Get_HeaderCount(ss);
            uint needBytes = (currentCount + headersToAdd) * 7U;

            int have = header?.Length ?? 0;
            if (have >= needBytes) return;

            int growTo = Math.Min(GuardUtils.NextPow2((int)needBytes), DecoLimits.MaxHeaderBytes);
            if (growTo > have) header = new byte[growTo];
        }
    }

    // ---------------- DataSorted guard ----------------
    [HarmonyPatch]
    internal static class SuperSaver_ByIdHelpWrite_Guard
    {
        // FAST field refs
        private static readonly AccessTools.FieldRef<
            BrilliantSkies.DataManagement.Serialisation.SuperSaver, byte[]> Ref_Data =
            AccessTools.FieldRefAccess<BrilliantSkies.DataManagement.Serialisation.SuperSaver, byte[]>("DataSorted");

        private static readonly Func<BrilliantSkies.DataManagement.Serialisation.SuperSaver, uint> Get_Written =
            AccessTools.MethodDelegate<Func<BrilliantSkies.DataManagement.Serialisation.SuperSaver, uint>>(
                AccessTools.PropertyGetter(typeof(BrilliantSkies.DataManagement.Serialisation.SuperSaver), "_datasWrittenSorted"));

        // Explicit-interface target
        static MethodBase TargetMethod()
        {
            var t = typeof(BrilliantSkies.DataManagement.Serialisation.SuperSaver);
            var iface = typeof(BrilliantSkies.DataManagement.Serialisation.VariableTypes.IVariableWriteHelp);

            var map = t.GetInterfaceMap(iface);
            for (int i = 0; i < map.InterfaceMethods.Length; i++)
            {
                var im = map.InterfaceMethods[i];
                if (im.Name == nameof(BrilliantSkies.DataManagement.Serialisation.VariableTypes.IVariableWriteHelp.ByIdHelpWrite))
                {
                    var ps = im.GetParameters();
                    if (ps.Length == 2 && ps[0].ParameterType == typeof(uint) && ps[1].ParameterType == typeof(uint))
                        return map.TargetMethods[i];
                }
            }

            // Fallbacks for odd builds
            var byName = AccessTools.GetDeclaredMethods(t).FirstOrDefault(m =>
                m.Name.EndsWith(".IVariableWriteHelp." +
                    nameof(BrilliantSkies.DataManagement.Serialisation.VariableTypes.IVariableWriteHelp.ByIdHelpWrite)));
            if (byName != null) return byName;

            var bySig = AccessTools.GetDeclaredMethods(t).FirstOrDefault(m =>
            {
                if (!m.Name.Contains("ByIdHelpWrite")) return false;
                var p = m.GetParameters();
                return p.Length == 2 && p[0].ParameterType == typeof(uint) && p[1].ParameterType == typeof(uint);
            });
            if (bySig != null) return bySig;

            throw new MissingMethodException("Could not locate SuperSaver implementation of IVariableWriteHelp.ByIdHelpWrite.");
        }

        [HarmonyPrefix]
        private static void Prefix(object __instance, uint id, uint dataSize)
        {
            // Game clamps length to 255; size our capacity check to that
            uint payload = Math.Min(255u, dataSize);
            EnsureDataCapacity((BrilliantSkies.DataManagement.Serialisation.SuperSaver)__instance, bytesToAppend: 2u + 1u + payload);
        }

        private static void EnsureDataCapacity(BrilliantSkies.DataManagement.Serialisation.SuperSaver ss, uint bytesToAppend)
        {
            ref var data = ref Ref_Data(ss);
            uint written = Get_Written(ss);
            uint needBytes = written + bytesToAppend;

            int have = data?.Length ?? 0;
            if (have >= needBytes) return;

            int growTo = Math.Min(GuardUtils.NextPow2((int)needBytes), DecoLimits.MaxDataSortedBytes);
            if (growTo > have) data = new byte[growTo];
        }
    }
}
