using BepInEx;
using BepInEx.Logging;
using BoplFixedMath;
using HarmonyLib;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using ControlScheme = ScaleControlOnline.Patch.ScaleController.ControlScheme;

namespace ScaleControlOnline
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

            harmony.PatchAll(typeof(Patch));

            // Recieve input message
            SteamMatchmaking.OnChatMessage += Patch.ScaleController.ReadInputs;
        }
        internal static void Log(string message, bool err = false)
        {
            if (err) logger.LogError(message);
            else logger.LogInfo(message);
        }

        private void Update()
        {
            // Send input message
            Patch.ScaleController.SendInputs();
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }

    [HarmonyPatch]
    public class Patch
    {
        // Lobby to send messages through
        private static Lobby currentLobby;

        /// <summary>
        /// Initialize all player's ScaleControllers
        /// </summary>
        [HarmonyPatch(typeof(GameSession), nameof(GameSession.Init))]
        [HarmonyPostfix]
        public static void OnGameStart()
        {
            // Get players
            currentLobby = SteamManager.instance.currentLobby;

            // Clear ScaleControllers
            ScaleController.list.Clear();

            // Initialize ScaleControllers
            foreach (Player player in PlayerHandler.Get().PlayerList())
            {
                // Local players
                if (player.IsLocalPlayer)
                {
                    // K&M player
                    if (player.UsesKeyboardAndMouse) new ScaleController(player, ControlScheme.Keyboard);
                    // Controller 
                    else new ScaleController(player, ControlScheme.Controller);
                }
                // Online player
                else new ScaleController(player, ControlScheme.Online);
            }
        }

        /// <summary>
        /// Reset the scale of all players when match starts
        /// </summary>
        [HarmonyPatch(typeof(GameSessionHandler), "SpawnPlayers")]
        [HarmonyPrefix]
        public static void ResetPlayerSize()
        {
            ScaleController.ResetPlayerScales();
        }

        /// <summary>
        /// Update scale
        /// </summary>
        [HarmonyPatch(typeof(PlayerBody), nameof(PlayerBody.UpdateSim))]
        [HarmonyPostfix]
        public static void PlayerUpdate(ref IPlayerIdHolder ___idHolder)
        {
            // Handle each player
            Player player = PlayerHandler.Get().GetPlayer(___idHolder.GetPlayerId());
            ScaleController.checkPlayerInput(player);
        }

        /// <summary>
        /// Handles all the players controls
        /// </summary>
        internal class ScaleController
        {
            // List of instances
            internal static readonly List<ScaleController> list = new List<ScaleController>();

            // Control types
            internal enum ControlScheme
            {
                Keyboard,
                Controller,
                Online
            }

            // Player attributes
            private readonly Player player;
            private readonly ControlScheme controlScheme;
            private readonly Gamepad gamepad;
            private float scale = 1;

            // Multiplication factor
            private static readonly float SCALE_FACTOR = 0.03f;

            // Message prefix
            private static readonly string PREFIX = $"{PluginInfo.PLUGIN_NAME}>";

            internal ScaleController(Player player, ControlScheme controlType)
            {
                this.player = player;
                this.controlScheme = controlType;

                // Add controller
                if (controlType == ControlScheme.Controller) this.gamepad = (Gamepad)player.inputDevice;

                Plugin.Log("Created player of type " + controlType);

                // Add to list
                list.Add(this);
            }

            private void Update()
            {
                // Checks what inputs the player is giving in whatever way it is
                // And then sets `scale` accordingly
                switch (controlScheme)
                {
                    case ControlScheme.Keyboard:
                        CheckKeyboard(); break;
                    case ControlScheme.Controller:
                        CheckController(); break;
                }

                // Apply scale
                Scale();
            }

            /// <summary>
            /// Check keyboard inputs
            /// </summary>
            private void CheckKeyboard()
            {
                // Grow
                if (Keyboard.current[Key.E].isPressed) scale += SCALE_FACTOR;

                // Shrink
                if (Keyboard.current[Key.Q].isPressed) scale -= SCALE_FACTOR;

                // Reset
                if (Keyboard.current[Key.F].isPressed) ResetScale();
            }

            /// <summary>
            /// Check Controller inputs
            /// </summary>
            private void CheckController()
            {
                // Add deadzone?
                // Or just make it buttons?

                // Read triggers
                float rightTrig = gamepad.rightTrigger.ReadValue();
                float leftTrig = gamepad.leftTrigger.ReadValue();

                // Control size: Right=Grow, Left=Shrink
                scale += SCALE_FACTOR * (rightTrig - leftTrig);

                // Reset
                if (gamepad.leftStickButton.isPressed) ResetScale();
            }

            /// <summary>
            /// Send chat message containing the scale of local players
            /// </summary>
            internal static void SendInputs()
            {
                string msg = string.Empty;
                
                foreach (ScaleController sc in list)
                {
                    if (sc.controlScheme != ControlScheme.Online) msg += $"{sc.player.steamId.Value},{sc.scale};";
                }

                currentLobby.SendChatString($"{PREFIX}{msg}");
            }

            /// <summary>
            /// Chat listener
            /// </summary>
            internal static void ReadInputs(Lobby lobby, Friend sender, string msg)
            {
                // Ignore messages sent by me
                if (sender.IsMe) return;

                // Ignore messages not of this mod
                if (!msg.StartsWith(PREFIX)) return;

                // Parse message
                foreach (string pl in msg.Remove(0,PREFIX.Length).Split(';'))
                {
                    // End of message
                    if (pl.Length == 0) break;

                    string[] values = pl.Split(',');
                    ulong steamId = ulong.Parse(values[0]);
                    float scale = float.Parse(values[1]);

                    UpdateScaleBySteamId(steamId, scale);
                }
            }

            /// <summary>
            /// Update the scale of the player by their SteamId
            /// </summary>
            private static void UpdateScaleBySteamId(ulong steamId, float scale)
            {
                foreach (ScaleController scaleController in list)
                {
                    if (scaleController.player.steamId.Value == steamId) scaleController.scale = scale;
                }
                throw new Exception("Asked for a ScaleController for a player that doesn't have one");
            }

            private void Scale()
            {
                // Lower limit of size in case the mod is used with NoSizeCaps (smaller causes division by zero)
                scale = Math.Max(scale, 0.01f);
                
                // Apply
                player.Scale = (Fix)scale;

                // Hold true value
                scale = (float)player.Scale;
            }

            private void ResetScale()
            {
                scale = 1f;
            }

            /// <summary>
            /// Find <c>ScaleController</c> by <c>Player</c>
            /// </summary>
            /// <param name="player">Player to search for</param>
            /// <returns>Their <c>ScaleController</c></returns>
            private static ScaleController GetController(Player player)
            {
                foreach (ScaleController scaleController in list)
                {
                    if (scaleController.player == player) return scaleController;
                }
                throw new Exception("Asked for a ScaleController for a player that doesn't have one");
            }

            /// <summary>
            /// Call <c>CheckInput</c> for the requested <c>Player</c>
            /// </summary>
            internal static void checkPlayerInput(Player player)
            {
                GetController(player).Update();
            }

            /// <summary>
            /// Reset the scale of all players
            /// </summary>
            internal static void ResetPlayerScales()
            {
                foreach (ScaleController sc in list) sc.ResetScale();
            }
        }

        /*
         * TODO: Remove "chat message callback" message from SteamManager.OnChatMessageCallback
         * TODO: test clones
         * TODO: remove all online functionality from local game
         * TODO: No need for SendMessage to contain a list - in an online game there is only one local player
         * 
         * IT WORKS! But it is wayyyyy too slow and definitely will desync.
         * 
         * SCROLLWHEEL REQUIRES EXTRA WORK - mouse input is frame dependent, need to do some extra work to support mouse input.
         */
    }

}

/*
    Don't Believe what you see, see what you believe.
    - Antimality.
*/
