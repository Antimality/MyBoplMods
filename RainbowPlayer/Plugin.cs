using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

///
/// TODOs:
///     Make it work after revive
///     Add config for which player(s) gets rainbow-ed and the colors used
///     Improve gradient affect
///

namespace RainbowPlayer
{
    [BepInPlugin("me.antimality." + PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        private static Harmony harmony;
        private static ManualLogSource logger;

        private void Awake()
        {
            logger = base.Logger;

            Log($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");

            harmony = new("me.antimality." + PluginInfo.PLUGIN_NAME);

            harmony.PatchAll(typeof(Patch));
        }

        private void Update()
        {
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }

        internal static void Log(String message)
        {
            logger.LogInfo(message);
        }
    }

    [HarmonyPatch]
    public class Patch
    {
        // Base game's player colors
        static private List<Color> baseColors = [
            new Color(0.989f, 1.000f, 0.316f), // Yellow
            new Color(0.641f, 0.532f, 0.981f), // Violet
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

        // Random color generator
        private static System.Random rand = new System.Random();
        private static Color GetRandomColor()
        {
            return new Color((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
        }
        // Use random colors?
        static private bool useRandomColor = false;
        
        private static Color GetGradientColor(Color prev)
        {
            var r = prev.r + (float)rand.NextDouble()/10f;
            var g = prev.g + (float)rand.NextDouble()/10f;
            var b = prev.b + (float)rand.NextDouble()/10f;

            return new Color(r % 1f, g % 1f, b % 1f);
        }
        // Use gradients
        static private bool useGradient = true;

        // Current random color
        static private Color currentColor = GetRandomColor();

        // White player and controller
        static private Player whitePlayer;
        static private SlimeController whiteController;

        // Color change frequency
        static private readonly int FRAMES_PER_COLOR = 15;
        static private int tick = 0;

        /// <summary>
        /// Detect the white player when the game starts
        /// </summary>
        [HarmonyPatch(typeof(GameSession), nameof(GameSession.Init))]
        [HarmonyPostfix]
        public static void OnGameStart()
        {
            // Delete previously saved white player
            whitePlayer = null;

            // Get the white player
            foreach (Player player in PlayerHandler.Get().PlayerList())
            {
                if (player.Color.GetColor("_ShadowColor") == Color.white)
                {
                    Plugin.Log("Found white player");
                    whitePlayer = player;
                }
            }
        }

        /// <summary>
        /// Changes the player's visual/sprite color
        /// </summary>
        [HarmonyPatch(typeof(PlayerBody), nameof(PlayerBody.UpdateSim))]
        [HarmonyPostfix]
        public static void ChangeColor(ref IPlayerIdHolder ___idHolder)
        {
            // Ignore if not white player
            if (PlayerHandler.Get().GetPlayer(___idHolder.GetPlayerId()) != whitePlayer) return;

            // Search for white player's SlimeController
            foreach (SlimeController sc in GetSlimeControllers())
            {
                //Plugin.Log($"Slime control for player #{sc.GetPlayerId()} which is null? {sc == null}");
                if (sc.GetPlayerId() == whitePlayer.Id && sc != null) whiteController = sc;
            }

            // After revive there is a slime controller but it is null??
            if (whiteController == null)
            {
                return;
            }

            if (useRandomColor)
            {
                // Generate new color
                if (tick >= FRAMES_PER_COLOR)
                {
                    currentColor = GetRandomColor();
                    // Loop
                    tick = 0;
                }

                // Set color
                whiteController.GetPlayerSprite().material.SetColor("_ShadowColor", currentColor);
            }

            if (useGradient)
            {
                // Generate new color
                if (tick >= FRAMES_PER_COLOR)
                {
                    currentColor = GetGradientColor(currentColor);
                    // Loop
                    tick = 0;
                }

                // Set color
                whiteController.GetPlayerSprite().material.SetColor("_ShadowColor", currentColor);
            }

            else
            {
                // Color index
                int index = tick / FRAMES_PER_COLOR;

                // Loop
                if (index >= baseColors.Count)
                {
                    index = 0;
                    tick = 0;
                }

                // Changes the look of the player, but they always spawn in their beggining color (good)
                whiteController.GetPlayerSprite().material.SetColor("_ShadowColor", baseColors[index]);
            }

            // Cycle colors
            tick++;
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
