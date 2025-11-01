using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace DecoLimitLifter.Patches
{
    // Patch specific overload: Convert(MainConstruct, bool)
    [HarmonyPatch]
    internal static class BlueprintConverterBufferPatch
    {
        static MethodBase TargetMethod()
        {
            // Type that hosts Convert(...)
            var tConv = AccessTools.TypeByName("Assets.Scripts.BlueprintConverter");
            if (tConv == null) return null;

            // MainConstruct type
            var tMC = AccessTools.TypeByName("MainConstruct");
            if (tMC == null) return null;

            // Convert(MainConstruct, bool)
            return AccessTools.GetDeclaredMethods(tConv).FirstOrDefault(m =>
            {
                if (m.Name != "Convert") return false;
                var p = m.GetParameters();
                return p.Length == 2 && p[0].ParameterType == tMC && p[1].ParameterType == typeof(bool);
            });
        }

        // Before Convert runs, decide buffer size. 
        [HarmonyPrefix]
        static void Prefix()
        {
            // Ensure correct.
            DecoLimitLifter.DecoLimits.SaveBufferBytes =
                Math.Max(DecoLimitLifter.DecoLimits.MinSaveBufferBytes,
                         DecoLimitLifter.DecoLimits.VanillaSaveBufferBytes);
        }

        // Replaces "new byte[10_000_000]" with "new byte[DecoLimits.SaveBufferBytes]"
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            var fSaveBuf = AccessTools.Field(typeof(DecoLimitLifter.DecoLimits),
                                             nameof(DecoLimitLifter.DecoLimits.SaveBufferBytes));

            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].opcode == OpCodes.Ldc_I4 &&
                    code[i].operand is int val &&
                    val == 10_000_000)
                {
                    // Replaces any occurrence of 10,000,000 with our configurable buffer size
                    code[i] = new CodeInstruction(OpCodes.Ldsfld, fSaveBuf);
                }
            }
            return code;
        }
    }
}
