using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BoplFixedMath;
using HarmonyLib;
using UnityEngine;

namespace TimeStopEnhanced
{
    /// <summary>
    /// Adds configuration for Time Stop cooldown, casting duration, and freeze duration.
    /// </summary>
    [BepInPlugin("me.antimality." + PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static ManualLogSource logger;
        private static Harmony harmony;

        internal static ConfigEntry<float> cooldown;
        internal static ConfigEntry<float> castDuration;
        internal static ConfigEntry<float> freezeDuration;

        private void Awake()
        {
            logger = base.Logger;

            logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");

            // Bind the configs TODO: CHECK DEFAULT VALUES
            cooldown = Config.Bind("Durations", "Cooldown", 8f, "The cooldown time of Time Stop. Default is 8");
            castDuration = Config.Bind("Durations", "CastingDuration", 9f, "The casting duration of Time Stop. Default is 9");
            freezeDuration = Config.Bind("Durations", "FreezeDuration", 10f, "The freeze duration of Time Stop. Default is 10");

            harmony = new("me.antimality." + PluginInfo.PLUGIN_NAME);

            harmony.PatchAll(typeof(Patch));
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
    public static class Patch
    {
        /// <summary>
        /// Casting time & cooldown modification
        /// </summary>
        [HarmonyPatch(typeof(CastSpell), nameof(CastSpell.Init))]
        [HarmonyPostfix]
        public static void ModifyAbility(ref GameObject ___spell, ref Fix ___castTime, ref Ability ___ability)
        {
            // Only activate on the intended ability
            if (___spell.name == "TimeStopSphere")
            {
                // Modify casting time
                ___castTime = (Fix) Plugin.castDuration.Value;
                // Modify cooldown
                ___ability.Cooldown = (Fix) Plugin.cooldown.Value;
            }
        }

        /// <summary>
        /// Freeze duration modification
        /// </summary>
        [HarmonyPatch(typeof(TimeStop), nameof(TimeStop.Init))]
        [HarmonyPostfix]
        public static void FreezeDuration(ref float ___duration)
        {
            // Modify freeze duration
            ___duration = Plugin.freezeDuration.Value;
        }
    }

}

/*
    Don't Believe what you see, see what you believe.
    - Antimality.
*/
