using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BoplFixedMath;
using HarmonyLib;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// TODOs:
///     Ability cancel:
///         Add warning in ability select screen that it is disabled        
///         Try to remove it from ability drops/ability select? (backup option - it just gives something random instead)

namespace InfiniteReviveUses
{
    /// <summary>
    /// Unlimited revives from a single ability!
    /// Or configure to limit/disable the ability.
    /// </summary>
    [BepInPlugin("me.antimality." + PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        internal static ManualLogSource Log;
        private static Harmony harmony;

        // Config file
        internal static ConfigFile config;
        internal static ConfigEntry<int> maxUsesSetting;
        internal static ConfigEntry<float> cooldownSetting;

        private void Awake()
        {
            Log = Logger;
            config = Config;

            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");

            harmony = new("me.antimality." + PluginInfo.PLUGIN_NAME);

            // Bind max uses
            maxUsesSetting = config.Bind("Settings", "Max revive uses", -1, "-1 for unlimited, 0 to disable revives.");
            // Negative value = unlimited
            if (maxUsesSetting.Value < 0)
            {
                maxUsesSetting.Value = int.MaxValue;
                config.Save();
            }

            // Bind max uses
            cooldownSetting = config.Bind("Settings", "Revive cooldown", 10f, "Default is 10.");
            // Negative value = zero
            if (cooldownSetting.Value < 0)
            {
                cooldownSetting.Value = 0;
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
        /* MAX USES */
        // Tracks the revive anchors made by each instance of the Revive ability
        private static readonly Dictionary<Revive, List<RevivePositionIndicator>> anchors = new Dictionary<Revive, List<RevivePositionIndicator>>();

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
            if (Plugin.maxUsesSetting.Value == 0)
            {
                // Delete the visual anchor
                indicator.GetComponent<RevivePositionIndicator>().End();
                // Stop
                return false;
            }

            // Add the new revive indicator:
            ___reviveIndicator = indicator.GetComponent<RevivePositionIndicator>();
            ___player = PlayerHandler.Get().GetPlayer(__instance.GetComponent<IPlayerIdHolder>().GetPlayerId());
            ___player.ExtraLife = ReviveStatus.willReviveOnDeath;
            ___player.ReviveInstance = __instance;
            ___player.RespawnPositions.Add(___reviveIndicator);

            // Add to count
            if (anchors.ContainsKey(__instance)) anchors[__instance].Add(___reviveIndicator);
            else anchors[__instance] = [___reviveIndicator];

            // If there's too many - remove the oldest anchor from this instance
            if (anchors[__instance].Count > maxUses)
            {
                RevivePositionIndicator anchor = anchors[__instance][0];
                // Remove from player
                ___player.RespawnPositions.Remove(anchor);
                // Remove object
                anchor.End();
                // Remove from list
                anchors[__instance].Remove(anchor);
            }

            // Disable the original function
            return false;
        }

        /* COOLDOWN */
        [HarmonyPatch(typeof(Ability), nameof(Ability.Awake))]
        [HarmonyPostfix]
        public static void CustomizeCooldown(ref Ability __instance)
        {
            if (__instance.name.StartsWith("Revival")) __instance.Cooldown = cooldown;
        }


        /* CONFIG SYNCING */
        // Stored values
        private static int maxUses;
        private static Fix cooldown;

        /// <summary>
        /// When you start a game, delete previously saved Default Size value
        /// </summary>
        [HarmonyPatch(typeof(GameSession), nameof(GameSession.Init))]
        [HarmonyPostfix]
        public static void OnGameStart()
        {
            // If online
            if (GameLobby.isOnlineGame)
            {
                try
                {
                    // Use host's default size setting
                    maxUses = int.Parse(SteamManager.instance.currentLobby.GetData("reviveMaxUses"), CultureInfo.InvariantCulture);
                    cooldown = (Fix)float.Parse(SteamManager.instance.currentLobby.GetData("reviveCooldown"), CultureInfo.InvariantCulture);
                }
                // If host doesn't have the mod
                catch (FormatException)
                {
                    // Set to normal values
                    maxUses = 1;
                    cooldown = (Fix)10f;
                    Plugin.Log.LogError($"Host doesn't have InfiniteReviveUses mod. Disabling functionality.");
                }
            }
            // If local
            else
            {
                // Use value from config
                maxUses = Plugin.maxUsesSetting.Value;
                cooldown = (Fix)Plugin.cooldownSetting.Value;
            }
        }

        /// <summary>
        /// When creating a lobby, inject the maxUses value from config to the lobby
        /// </summary>
        [HarmonyPatch(typeof(SteamManager), "OnLobbyEnteredCallback")]
        [HarmonyPostfix]
        public static void OnEnterLobby(Lobby lobby)
        {
            // If I am the owner of this lobby, load the value
            if (SteamManager.LocalPlayerIsLobbyOwner)
            {
                // Harmony lint thinks this won't work (because I'm editing a parameter's value), but it does
                #pragma warning disable Harmony003
                lobby.SetData("reviveMaxUses", Plugin.maxUsesSetting.Value.ToString());
                lobby.SetData("reviveCooldown", Plugin.cooldownSetting.Value.ToString());
            }
        }
    }
}

/*
    Don't Believe what you see, see what you believe.
    - Antimality.
*/
