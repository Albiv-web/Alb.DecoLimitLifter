using System;
using BrilliantSkies.Modding;   // GamePlugin interfaces
using HarmonyLib;

namespace DecoLimitLifter
{
    // Loaded by FtD’s plugin system. Manual: implement GamePlugin or GamePlugin_PostLoad
    public class FtDInterface : GamePlugin_PostLoad
    {
        public string name => "DecoLimitLifter";
        public Version version => new Version(1, 0);

        public void OnLoad()
        {
            var harmony = new Harmony("alb.ftd.decolimit");
            harmony.PatchAll(typeof(FtDInterface).Assembly);
        }

        public bool AfterAllPluginsLoaded() => true;

        public void OnSave() { }
    }
}
