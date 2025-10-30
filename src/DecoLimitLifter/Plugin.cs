using System;
using BrilliantSkies.Modding;         // GamePlugin / GamePlugin_PostLoad
using HarmonyLib;

namespace DecoLimitLifter
{
    public class FtDInterface : GamePlugin_PostLoad
    {
        public string name => "DecoLimitLifter";
        public Version version => new Version(1, 0);

        public void OnLoad()
        {
            // Apply all Harmony patches in this assembly
            var harmony = new Harmony("alb.ftd.decolimit");
            harmony.PatchAll(typeof(FtDInterface).Assembly);
        }

        public bool AfterAllPluginsLoaded() => true;
        public void OnSave() { }
    }
}
