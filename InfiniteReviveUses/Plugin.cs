using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

///
/// TODOs:
///     Add online using config logic from DefaultSize
///     Add cooldown config
///     Make it disable the ability when max-uses is zero?
///         Add warning in ability select screen that it is disabled        
///         Try to remove it from ability drops?
///

namespace InfiniteReviveUses
{
    /// <summary>
    /// Use a single revive to clone!
    /// </summary>
    [BepInPlugin("me.antimality." + PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        internal static ManualLogSource Log;
        private static Harmony harmony;

        // Config file
        internal static ConfigFile config;
        internal static ConfigEntry<int> maxUseSetting;

        private void Awake()
        {
            Log = Logger;
            config = Config;

            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");

            harmony = new("me.antimality." + PluginInfo.PLUGIN_NAME);

            // Bind the config
            maxUseSetting = config.Bind("Settings", "Max revive uses", -1, "-1 for unlimited, 0 to disable revives.");
            // Lower cap
            if (maxUseSetting.Value < 0)
            {
                maxUseSetting.Value = int.MaxValue;
                config.Save();
            }

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
        /// <summary>
        /// Replaces the original function. Removes the if block that removes the previous revive anchor.
        /// </summary>
        [HarmonyPatch(typeof(Revive), nameof(Revive.SetReviveFlag))]
        [HarmonyPrefix]
        public static bool SetReviveFlagReplacement(GameObject indicator, ref Revive __instance, ref Player ___player, ref RevivePositionIndicator ___reviveIndicator)
        {
            ///
            /// This works, but still has the casting affect.
            /// Better to add a patch in the ability itself (CastSpell)
            ///
            if (Plugin.maxUseSetting.Value == 0)
            {
                // Delete the visual anchor
                indicator.GetComponent<RevivePositionIndicator>().End();
                // Stop
                return false;
            }

            /// Removed the following section:
            /*
            if (___reviveIndicator != null && !___reviveIndicator.IsDestroyed)
            {
                ___reviveIndicator.End();
                int num = ___player.RespawnPositions.Count - 1;
                while (num >= 0 && num < ___player.RespawnPositions.Count)
                {
                    if (___player.RespawnPositions[num] == null || (___player.RespawnPositions[num] != null && ___player.RespawnPositions[num].IsDestroyed))
                    {
                        ___player.RespawnPositions.RemoveAt(num);
                    }
                    else
                    {
                        num--;
                    }
                }
            }
            */
            // Add the new revive indicator:
            ___reviveIndicator = indicator.GetComponent<RevivePositionIndicator>();
            ___player = PlayerHandler.Get().GetPlayer(__instance.GetComponent<IPlayerIdHolder>().GetPlayerId());
            ___player.ExtraLife = ReviveStatus.willReviveOnDeath;
            ___player.ReviveInstance = __instance;
            ___player.RespawnPositions.Add(___reviveIndicator);

            ///
            /// Not good, this limits the player's total number of anchors.
            /// need to keep a list for each revive instance
            ///
            // Max uses
            int overflow = Plugin.maxUseSetting.Value - ___player.RespawnPositions.Count;
            if (overflow < 0)
            {
                // Take the oldest anchors
                foreach (RevivePositionIndicator i in ___player.RespawnPositions.GetRange(0, -overflow))
                {
                    // Delete them
                    i.End();
                }
                // Remove from list
                ___player.RespawnPositions.RemoveRange(0, -overflow);
            }

            // Disable the original function
            return false;
        }
    }
}

/*
    Don't Believe what you see, see what you believe.
    - Antimality.
*/
