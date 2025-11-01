using BrilliantSkies.Modding;
using BrilliantSkies.Core.Logger;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace DecoLimitLifter
{
    public sealed class Plugin : GamePlugin_PostLoad
    {
        public string name => "DecoLimitLifter";
        public Version version => new Version(1, 0, 0, 0);

        public void OnLoad()
        {
            try
            {
                const string hid = "alb.decolimitlifter";
                var h = new Harmony(hid);
                h.PatchAll(Assembly.GetExecutingAssembly());
               
                // Ensure global blueprint buffer (ByteStore.MegaBytes) is big enough.
                DecoLimitLifter.Patches.ByteStorePatch.EnsureMegaBytes();

                // Keep reusable pools at vanilla sizes; growth is ondemand only.
                Patches.SuperSaverBuffersPatch.OnBootEnsurePools();

                // Tiny selfcheck (silent if everything attached)
                var loaderTargets = typeof(BrilliantSkies.DataManagement.Serialisation.SuperLoader)
                    .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Where(m => m.Name == "Deserialise")
                    .Where(m =>
                    {
                        var p = m.GetParameters();
                        return p.Length >= 3 &&
                               p[0].ParameterType == typeof(byte[]) &&
                               p[1].ParameterType.IsByRef &&
                               p[1].ParameterType.GetElementType() == typeof(uint) &&
                               p[2].ParameterType == typeof(byte);
                    });

                bool okL = loaderTargets.Any(m => Harmony.GetPatchInfo(m)?.Owners?.Contains(hid) == true);
                bool okS = Harmony.GetPatchInfo(AccessTools.Method(
                             typeof(BrilliantSkies.DataManagement.Serialisation.SuperSaver), "Serialise"))
                           ?.Owners?.Contains(hid) == true;

                AdvLogger.LogError($"[DecoLimitLifter] Loaded. Patched: Loader:{(okL ? "✔" : "✖")} Saver:{(okS ? "✔" : "✖")}",
                    LogOptions._AlertDevAndCustomerInGame);
            }
            catch (Exception ex)
            {
                AdvLogger.LogException("[DecoLimitLifter] FAILED to patch", ex, LogOptions._AlertDevAndCustomerInGame);
            }
        }

        public bool AfterAllPluginsLoaded() => true;
        public void OnSave() { }
    }
}
