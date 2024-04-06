using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace InfiniteBlackHoles
{
    [BepInPlugin("me.antimality." + PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        internal static ManualLogSource Log;
        private static Harmony harmony;

        private void Awake()
        {
            Log = Logger;

            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");

            harmony = new("me.antimality." + PluginInfo.PLUGIN_NAME);

            harmony.PatchAll(typeof(Patch));
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }

    [HarmonyPatch]
    public class Patch
    {
        [HarmonyPatch(typeof(BlackHoleClap), "FireCommon")]
        [HarmonyPostfix]
        public static void ReenableAbility(ref Ability ___ability)
        {
            ___ability.isCastable = true;
        }
    }

}

/*
    Don't Believe what you see, see what you believe.
    - Antimality.
*/
