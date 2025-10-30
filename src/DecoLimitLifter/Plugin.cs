using BepInEx;
using HarmonyLib;

namespace DecoLimitLifter
{
    [BepInPlugin("alb.ftd.decolimit", "Deco Limit Lifter", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            var harmony = new Harmony("alb.ftd.decolimit");
            harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo("Deco Limit Lifter loaded");
        }
    }
}

