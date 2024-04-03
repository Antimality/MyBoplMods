using BepInEx;
using BepInEx.Logging;
using BoplFixedMath;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// TODO: Fix online
///
/// Problem with online: players don't see each other scale (desync)
/// The game only records and sends inputs, proccessing these inputs is done locally and in real time
/// I need to handle all player's inputs on every machine
/// Might not be possible, it seems that Client.cs only recieves the inputs sent by the base game
/// If that's so, change the grow/shrink abilities to do what I want and lower their cooldown
/// 
/// Plausible workaround: steam chat, see https://discord.com/channels/1175164882388275310/1197174978618064896/1223581167903834132
///

namespace ScaleControl
{
    /// <summary>
    /// Allows players to shrink and grow at will.
    /// Controlles:
    ///     K&M: Scrollwheel (F to reset)
    ///     Controller: Right trigger to grow, left to shrink (left stick button to reset)
    ///     
    /// TODOs:
    ///     Refine controller setup
    ///     Add configurable controls
    ///     Add max/min buttons?
    ///     Add audio? (it activates a lot and becomes loud and unpleasant. Maybe only make sound when you reach max?
    ///     Scale cooldown by size? generally, you're less dangerous the smaller you are, might make sense to have quicker cooldowns
    /// </summary>
    [BepInPlugin("me.antimality." + PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = base.Logger;

            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
        }


        private void Update()
        {
            // Online
            if (GameLobby.isOnlineGame)
            {
                // NOT IMPLEMENTED YET
                //PlayerPatch.UpdateOnline();
            }
            // Offline
            else
            {
                PlayerPatch.UpdateLocal();
            }

            // Inform user that the mod does not work online
            if (SceneManager.GetActiveScene().name == "ChSelect_online" && warningBox == null)
            {
                DisplayWarning();
            }
            // Dispose of the warning textbox when exiting the online select screen
            else if (SceneManager.GetActiveScene().name != "ChSelect_online" && warningBox != null)
            {
                Dispose();
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        GameObject warningBox;


        /// <summary>
        /// Displays a warning at the bottom of the online ability selection screen
        /// </summary>
        private void DisplayWarning()
        {
            Canvas canvas = GameObject.Find("Canvas").GetComponent<Canvas>();

            if (canvas == null)
            {
                Plugin.Log.LogError($"No suitable canvas!");
                return;
            }

            warningBox = new GameObject("ScaleControl Warning", typeof(RectTransform), typeof(TextMeshProUGUI));
            warningBox.transform.SetParent(canvas.transform);

            TextMeshProUGUI textComp = warningBox.GetComponent<TextMeshProUGUI>();

            // Dunno what this does
            textComp.raycastTarget = false;

            textComp.text = "WARNING: Scale Control mod does NOT work online";
            textComp.color = Color.red;
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.font = LocalizedText.localizationTable.GetFont(Settings.Get().Language, false);

            RectTransform location = warningBox.GetComponent<RectTransform>();

            // Width and Height of the screen
            float canvasHeight = canvas.GetComponent<RectTransform>().rect.height;
            float canvasWidth = canvas.GetComponent<RectTransform>().rect.width;

            // Set text size acording to screen size
            textComp.fontSize = canvasWidth/100;

            // Puts textbox on the bottom of the screen
            location.anchoredPosition = new Vector2(0, -canvasHeight / 2 + 100);
            location.sizeDelta = new Vector2(canvasWidth, 100);

            warningBox.SetActive(true);
        }

        /// <summary>
        /// Destroy 
        /// </summary>
        private void Dispose()
        {
            GameObject.Destroy(warningBox);
        }
    }

    /// <summary>
    /// Detects inputs from local players and changes their scale accordingly
    /// (Not a harmony patch)
    /// </summary>
    public class PlayerPatch
    {
        // Multiplication factors
        private static Fix MouseFactor = (Fix)0.5;
        private static Fix GamepadFactor = (Fix)0.03;

        /// <summary>
        /// On online games
        /// </summary>
        public static void UpdateOnline()
        {
            foreach (Player player in PlayerHandler.Get().PlayerList())
            {
                /// Maybe send chat messages discribing each player and whether or not the should grow?
                /// Each client would need to read and apply for all players
                /// Send: currentLobby.SendChatString 
                /// Recieve: SteamMatchmaking.OnChatMessage += OnChatMessageCallback;
                /// and see: SteamManager.OnChatMessageCallback

                // Send
                if (player.IsLocalPlayer)
                {

                }
                // Receive
                else
                {

                }
            }
        }

            /// <summary>
            /// On local games
            /// </summary>
            public static void UpdateLocal()
        {
            foreach (Player player in PlayerHandler.Get().PlayerList())
            {
                // Keyboard and Mouse
                if (player.UsesKeyboardAndMouse)
                {
                    // Grow
                    if (Mouse.current.scroll.ReadValue().y > 0)
                    {
                        player.Scale += MouseFactor;
                    }

                    // Shrink
                    else if (Mouse.current.scroll.ReadValue().y < 0)
                    {
                        player.Scale -= MouseFactor;
                    }

                    // Reset
                    if (Keyboard.current[Key.F].wasPressedThisFrame)
                    {
                        player.Scale = Fix.One;
                    }
                }

                // Controller
                else
                {
                    foreach (Gamepad gp in Gamepad.all)
                    {
                        if (player.inputDevice.deviceId == gp.deviceId)
                        {
                            // Add deadzone?
                            // Or just make it buttons?
                            // Control size: Right=Grow, Left=Shrink
                            if (gp.rightTrigger.ReadValue() > 0 || gp.leftTrigger.ReadValue() > 0)
                            {
                                player.Scale += GamepadFactor * (Fix)(gp.rightTrigger.ReadValue() - gp.leftTrigger.ReadValue());
                            }

                            // Reset
                            if (gp.leftStickButton.isPressed)
                            {
                                player.Scale = Fix.One;
                            }
                        }
                    }
                }

                // Lower limit of size in case the mod is used with NoSizeCaps (smaller causes division by zero)
                player.Scale = Fix.Max(player.Scale, (Fix)0.01f);
            }
        }
    }
}
