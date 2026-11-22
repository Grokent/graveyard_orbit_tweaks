using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;

namespace GraveyardOrbit.Tweaks
{
    [BepInPlugin(PluginGuid, "Graveyard Orbit Tweaks", PluginVersion)]
    public sealed class TweaksPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "agentbox.graveyardorbit.tweaks";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            _harmony = new Harmony(PluginGuid);
            try
            {
                _harmony.PatchAll(typeof(TweaksPlugin).Assembly);
                Log.LogInfo("Tweaks loaded:");
                Log.LogInfo("  drones: buy/upgrade prices halved, repair = $100");
                Log.LogInfo("  igniter: levels 2 and 3 (4.5 / 6 rocks/s) for $200 / $350");
            }
            catch (Exception ex)
            {
                Log.LogError("Tweaks patch failure: " + ex);
            }
        }

        private void OnDestroy()
        {
            try { _harmony?.UnpatchSelf(); } catch { }
        }
    }
}
