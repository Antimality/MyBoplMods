using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

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

        private void Awake()
        {
            Log = base.Logger;

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
        /// <summary>
        /// Replaces the original function. Removes the if block that removes the previous revive anchor
        /// </summary>
        [HarmonyPatch(typeof(Revive), nameof(Revive.SetReviveFlag))]
        [HarmonyPrefix]
        public static bool SetReviveFlagReplacement(GameObject indicator, ref Revive __instance, ref Player ___player, ref RevivePositionIndicator ___reviveIndicator)
        {
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
            
            // Disable the original function
            return false;
        }
    }
}

/*
    Don't Believe what you see, see what you believe.
    - Antimality.
*/
