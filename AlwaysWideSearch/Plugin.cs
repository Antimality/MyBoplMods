using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace AlwaysWideSearch
{
    [BepInPlugin("me.antimality." + PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static ManualLogSource logger;
        private static Harmony harmony;

        private void Awake()
        {
            logger = base.Logger;

            logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");

            harmony = new("me.antimality." + PluginInfo.PLUGIN_NAME);

            harmony.PatchAll();
        }

        internal static void Log(object message, bool err = false)
        {
            if (err) logger.LogError(message);
            else logger.LogInfo(message);
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }

    [HarmonyPatch]
    public class Patch
    {
        /// <summary>
        /// Always do wide search
        /// </summary>
        [HarmonyPatch(typeof(SteamManager), "StartMatchmaking")]
        [HarmonyPrefix]
        public static void AlwaysWide(ref bool widenSearch)
        {
            widenSearch = true;
        }
    }

}

/*
    Don't Believe what you see, see what you believe.
    - Antimality.
*/
