using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BoplFixedMath;
using HarmonyLib;
using Steamworks.Data;
using System;
using System.Globalization;

namespace DefaultSize
{
    /// <summary>
    /// Changes the default size of all players. They get set to this size at the beginning of each level.
    ///     - If online, host's setting is used
    /// </summary>
    [BepInPlugin("me.antimality." + PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        private static Harmony harmony;

        // Config file
        internal static ConfigFile config;
        internal static ConfigEntry<float> defaultSizeSetting;

        private void Awake()
        {
            Log = Logger;
            config = Config;

            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
            harmony = new("me.antimality." + PluginInfo.PLUGIN_NAME);

            // Bind the config
            defaultSizeSetting = config.Bind("Settings", "Default player size", 3f, "Minimum is 0.01 (Lower would cap to minimum). The default game's max scale is 3.");
            // Lower cap
            if (defaultSizeSetting.Value < 0.01f)
            {
                defaultSizeSetting.Value = 0.01f;
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
        // Stored value
        private static Fix defaultSize;

        /// <summary>
        /// Set the scale of the players at the begining of each round
        /// </summary>
        [HarmonyPatch(typeof(GameSessionHandler), "SpawnPlayers")]
        [HarmonyPostfix]
        public static void ChangePlayerSize()
        {
            // Change the size of all players
            foreach (Player player in PlayerHandler.Get().PlayerList())
            {
                player.Scale = defaultSize;
            }
        }

        /// <summary>
        /// When you start a game, read the intended value
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
                    defaultSize = (Fix)float.Parse(SteamManager.instance.currentLobby.GetData("DefaultSize"), CultureInfo.InvariantCulture);
                }
                // If host doesn't have the mod
                catch (FormatException)
                {
                    // Set size to normal
                    defaultSize = Fix.One;
                    Plugin.Log.LogError($"Host doesn't have DefaultSize mod. Disabling functionality.");
                }
            }
            // If local
            else
            {
                // Use value from config
                defaultSize = (Fix)Plugin.defaultSizeSetting.Value;
            }
        }

        /// <summary>
        /// When creating a lobby, inject the DefaultSize value from config to the lobby
        /// Sadly, you can't set the value for the clients in this patch, because for SOME REASON
        /// If you join a local game while in an online lobby, BOPL doesn't create a new local lobby...
        /// </summary>
        [HarmonyPatch(typeof(SteamManager), "OnLobbyEnteredCallback")]
        [HarmonyPostfix]
        public static void OnEnterLobby(Lobby lobby)
        {
            // If I am the owner of this lobby, load the value
            if (SteamManager.LocalPlayerIsLobbyOwner)
            {
                // Harmony linting thinks this won't work (because I'm editing a parameter's value), but it does
                #pragma warning disable Harmony003
                lobby.SetData("DefaultSize", Plugin.defaultSizeSetting.Value.ToString());
            }
        }
    }
}

/*
    Don't Believe what you see, see what you believe.
    - Antimality.
*/
