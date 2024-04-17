using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Steamworks.Data;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Color = UnityEngine.Color;

namespace RainbowPlayer
{
    [BepInPlugin("me.antimality." + PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        private static Harmony harmony;
        private static ManualLogSource logger;

        internal static ConfigEntry<bool> onlyWhite;
        internal static ConfigEntry<int> colorScheme;
        internal static ConfigEntry<uint> framesPerColor;

        private void Awake()
        {
            logger = base.Logger;

            Log($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");

            // Get config values
            onlyWhite = Config.Bind("Settings", "Only white player", false, "If set to false, all players are rainbow-ed");
            colorScheme = Config.Bind("Settings", "Color scheme", 2, "0: Base game colors\n1: Random\n2: Rainbow");
            framesPerColor = Config.Bind("Settings", "Frames per color", 6u, "How many frames to hold each color. 60 frames means one second");
            // Clamps:
            if (colorScheme.Value < 0 || colorScheme.Value > 2) colorScheme.Value = 2;
            if (framesPerColor.Value == 0u) framesPerColor.Value = 1u;
            // Update
            Config.Save();


            harmony = new("me.antimality." + PluginInfo.PLUGIN_NAME);

            harmony.PatchAll(typeof(Patch));
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
            Patch.MyPlayer.Clear();
        }

        internal static void Log(object message)
        {
            logger.LogInfo(message);
        }
    }

    [HarmonyPatch]
    public static class Patch
    {
        private enum ColorScheme
        {
            Base,
            Random,
            Rainbow
        };
        private static ColorScheme colorScheme;

        // White player
        static private int whitePlayerId;
        static private bool onlyWhitePlayer = true;

        // Color change frequency
        static private uint framesPerColor;

        /// <summary>
        /// Detect the white player when the game starts
        /// </summary>
        [HarmonyPatch(typeof(GameSession), nameof(GameSession.Init))]
        [HarmonyPostfix]
        public static void OnGameStart()
        {
            // Recieve config values
            if (GameLobby.isOnlineGame)
            {
                // Use host's setting
                onlyWhitePlayer = bool.Parse(SteamManager.instance.currentLobby.GetData("onlyWhite"));
                framesPerColor = uint.Parse(SteamManager.instance.currentLobby.GetData("framesPerColor"));
                colorScheme = (ColorScheme)int.Parse(SteamManager.instance.currentLobby.GetData("colorScheme"));
            }
            else
            {
                // Use value from local config
                onlyWhitePlayer = Plugin.onlyWhite.Value;
                framesPerColor = Plugin.framesPerColor.Value;
                colorScheme = (ColorScheme)Plugin.colorScheme.Value;
            }


            // Delete previously saved white player
            whitePlayerId = -1;

            MyPlayer.Clear();

            // Get the white player
            foreach (Player player in PlayerHandler.Get().PlayerList())
            {
                // Create player wrapper
                new MyPlayer(player.Id, player.Color.GetColor("_ShadowColor"));
                if (player.Color.GetColor("_ShadowColor") == Color.white)
                {
                    whitePlayerId = player.Id;
                }
            }
        }

        /// <summary>
        /// Changes the player's visual/sprite color. Run on PlayerBody.UpdateSim
        /// </summary>
        [HarmonyPatch(typeof(PlayerBody), nameof(PlayerBody.UpdateSim))]
        [HarmonyPostfix]
        public static void ChangeColor(ref IPlayerIdHolder ___idHolder)
        {
            // Search for player's SlimeController
            foreach (SlimeController sc in GetSlimeControllers())
            {
                // Find current player
                if (sc.GetPlayerId() == ___idHolder.GetPlayerId() && sc != null)
                {
                    // All players
                    if (!onlyWhitePlayer) MyPlayer.UpdateById(___idHolder.GetPlayerId(), sc);
                    // Only white
                    if (onlyWhitePlayer && sc.GetPlayerId() == whitePlayerId) MyPlayer.UpdateById(___idHolder.GetPlayerId(), sc);
                }
            }
        }

        // Set sprite color method
        private static void SetColor(this SlimeController sc, Color color) => sc.GetPlayerSprite().material.SetColor("_ShadowColor", color);

        /// <summary>
        /// Player Wrapper
        /// </summary>
        internal class MyPlayer
        {
            // Player wrapper list
            static internal List<MyPlayer> playerList = [];

            private int playerId;
            private int tick;
            private Color currentColor;

            internal MyPlayer(int playerId, Color origColor)
            {
                this.playerId = playerId;
                this.tick = -1;
                this.currentColor = origColor;

                playerList.Add(this);
            }

            internal static void UpdateById(int id, SlimeController sc)
            {
                foreach (MyPlayer player in playerList) if (player.playerId == id) player.UpdatePlayer(sc);
            }

            private void UpdatePlayer(SlimeController sc)
            {
                // Update tick
                tick++;

                bool newColor = tick % framesPerColor == 0;
                if (!newColor) return;

                // Random Colors
                if (colorScheme == ColorScheme.Random) currentColor = GetRandomColor();
                // Gradient Colors
                else if (colorScheme == ColorScheme.Rainbow) currentColor = GetRainbowColor();
                // Base Colors
                else currentColor = GetNextBaseColor();

                sc.SetColor(currentColor);
            }


            // Base game's player colors
            static private List<Color> baseColors = [
                new Color(0.989f, 1.000f, 0.316f), // Yellow
            new Color(0.641f, 0.532f, 0.981f, 1f), // Violet
            new Color(0.297f, 0.951f, 1.000f), // Torqousie
            new Color(1.000f, 1.000f, 1.000f), // White
            new Color(1.000f, 0.448f, 0.482f), // Red
            new Color(0.875f, 0.463f, 0.795f), // Purple
            new Color(1.000f, 0.675f, 0.761f), // Pink
            new Color(0.981f, 0.629f, 0.112f), // Orange
            new Color(0.726f, 0.726f, 0.726f), // Grey
            new Color(0.281f, 0.915f, 0.446f), // Green
            new Color(0.451f, 0.784f, 1.000f), // Blue
            new Color(0.415f, 0.415f, 0.415f), // Black
            ];
            private Color GetNextBaseColor()
            {
                return baseColors[(baseColors.IndexOf(currentColor) + 1) % baseColors.Count];
            }

            // Random color generator
            private static System.Random rand = new System.Random();
            private static Color GetRandomColor()
            {
                return new Color((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
            }

            private static readonly float GRADIENT_FACTOR = 25f;
            private readonly float randomizer = (float)rand.NextDouble();
            private Color GetRainbowColor()
            {
                var colorTick = randomizer + (tick / framesPerColor) / GRADIENT_FACTOR;
                return Color.HSVToRGB(Mathf.PingPong(colorTick, 1f), 1, 1);
            }

            internal static void Clear()
            {
                playerList.Clear();
            }
        }

        /// <summary>
        /// Load config
        /// </summary>
        [HarmonyPatch(typeof(SteamManager), "OnLobbyEnteredCallback")]
        [HarmonyPostfix]
        public static void OnEnterLobby(Lobby lobby)
        {
            if (SteamManager.LocalPlayerIsLobbyOwner)
            {
                lobby.SetData("onlyWhite", Plugin.onlyWhite.Value.ToString());
                lobby.SetData("framesPerColor", Plugin.framesPerColor.Value.ToString());
                lobby.SetData("colorScheme", Plugin.colorScheme.Value.ToString());
            }
        }

        /* Stolen code from Splotch */
        /// <summary>
        /// Gets the GameSessionHandler
        /// </summary>
        /// <returns>the GameSessionHandler or null if it isn't instantized</returns>
        public static GameSessionHandler GetGameSessionHandler()
        {
            FieldInfo selfRefField = typeof(GameSessionHandler).GetField("selfRef", BindingFlags.Static | BindingFlags.NonPublic);
            return selfRefField.GetValue(null) as GameSessionHandler;
        }

        /// <summary>
        /// Gets the slime controllers
        /// </summary>
        /// <returns>A list of slime controllers</returns>
        public static SlimeController[] GetSlimeControllers()
        {
            FieldInfo slimeControllersField = typeof(GameSessionHandler).GetField("slimeControllers", BindingFlags.Instance | BindingFlags.NonPublic);
            return slimeControllersField.GetValue(GetGameSessionHandler()) as SlimeController[];
        }
    }
}

/*
    Don't Believe what you see, see what you believe.
    - Antimality.
*/
