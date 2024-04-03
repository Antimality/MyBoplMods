using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BoplFixedMath;
using HarmonyLib;
using Steamworks.Data;
using UnityEngine.SceneManagement;

namespace DefaultSize
{
    /// <summary>
    /// Changes the default size of all players. They get set to this size at the beginning of each level.
    /// </summary>
    [BepInPlugin("me.antimality." + PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        private static Harmony harmony;

        // Config file
        internal static ConfigFile config;
        internal static ConfigEntry<float> defaultSizeSetting;

        // Applied value
        internal static float defaultSize;

        private void Awake()
        {
            Log = Logger;
            config = Config;

            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
            harmony = new("me.antimality." + PluginInfo.PLUGIN_NAME);

            // Bind the config
            defaultSizeSetting = config.Bind("Settings", "Default player size", 1f, "Minimum is 0.01 (Lower would cap to minimum). The default game's max scale is 3.");
            // Lower cap
            if (defaultSizeSetting.Value < 0f) defaultSizeSetting.Value = 0.01f;

            // Use value from config
            //defaultSize = defaultSizeSetting.Value;

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
        [HarmonyPatch(typeof(GameSessionHandler), "SpawnPlayers")]
        [HarmonyPostfix]
        public static void ChangePlayerSize()
        {
            // Change the size of all players
            foreach (Player player in PlayerHandler.Get().PlayerList())
            {
                player.Scale = (Fix)Plugin.defaultSize;
            }
        }

        /// <summary>
        /// Grabs the intended value for DefaultSize
        ///     Online - from lobby
        ///     Local - from config file
        /// </summary>
        [HarmonyPatch(typeof(GameSession), nameof(GameSession.Init))]
        [HarmonyPostfix]
        public static void GameStarted()
        {
            // If online
            if (GameLobby.isOnlineGame)
            {
                Plugin.Log.LogInfo("Not host player, getting DefaultSize from lobby");
                // Use host's default size setting
                Plugin.defaultSize = float.Parse(SteamManager.instance.currentLobby.GetData("DefaultSize"));
            }
            // If local
            else
            {
                Plugin.Log.LogInfo($"Starting local game");
                // Use value from config
                Plugin.defaultSize = Plugin.defaultSizeSetting.Value;
            }
        }

        /// <summary>
        /// Once the host creates an online lobby, insert their config value to the lobby
        /// </summary>
        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.CreateFriendLobby))]
        [HarmonyPostfix]
        public static void OnCreateOnlineLobby()
        {
            Lobby lobby = SteamManager.instance.currentLobby;
            Plugin.Log.LogInfo($"Created lobby: {lobby}");
            if (SteamManager.LocalPlayerIsLobbyOwner)
            {
                Plugin.Log.LogInfo("Host player, adding DefaultSize from config");
                lobby.SetData("DefaultSize", Plugin.defaultSizeSetting.Value.ToString());
            }
            else
            {
                Plugin.Log.LogInfo("Not host player, how?");
            }
        }
    }
}

/*
    Don't Believe what you see, see what you believe.
    - Antimality.
*/
