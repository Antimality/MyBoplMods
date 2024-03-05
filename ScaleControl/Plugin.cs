using BepInEx;
using BepInEx.Logging;
using BoplFixedMath;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace ScaleControl
{
    [BepInPlugin("me.antimality." + PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = base.Logger;

            // Plugin startup logic
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
        }

        private void Update()
        {
            PlayerPatch.Update();
        }
    }

    public class PlayerPatch
    {
        private static Fix MouseFactor = (Fix)0.5;
        private static Fix GamepadFactor = (Fix)0.02;

        public static void Update()
        {
            List<Player> players = PlayerHandler.Get().PlayerList();

            for (int i = 0; i < players.Count; i++)
            {
                // TODO: Fix online
                ///
                /// Problem with online: players don't see each other scale
                /// Look into:
                /// ScaleChanger (class)
                /// ShootScaleChange (groth/shrink ability)
                ///
                /// The game only records and sends inputs, proccessing these inputs is done locally and in real time
                /// I need to handle all player's inputs on every machine
                /// Might not be possible, it seems that Client.cs only recieves the inputs sent by the base game
                /// If that's so, change the grow/shrink abilities to do what I want and lower their cooldown
                ///
                // Add max/min buttons?
                // Add audio? (it activates a lot and becomes loud and unpleasant. Maybe only make sound when you reach max?

                Player player = players[i];

                // Only target local players
                if (!player.IsLocalPlayer)
                {
                    continue;
                }

                // Keyboard and Mouse
                if (player.UsesKeyboardAndMouse)
                {
                    // Grow
                    if (Mouse.current.scroll.ReadValue().y > 0)
                    {
                        player.Scale = player.Scale + MouseFactor;
                    }

                    // Shrink
                    else if (Mouse.current.scroll.ReadValue().y < 0)
                    {
                        player.Scale = player.Scale - MouseFactor;
                    }

                    // Reset
                    if (Keyboard.current[Key.F].wasPressedThisFrame)
                    {
                        player.Scale = Fix.One;
                    }
                }

                // Controller
                // TODO: check multiple controllers
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
                                player.Scale = player.Scale + GamepadFactor * (Fix)(gp.rightTrigger.ReadValue() - gp.leftTrigger.ReadValue());
                            }

                            // Reset
                            if (gp.leftStickButton.isPressed)
                            {
                                player.Scale = Fix.One;
                            }
                        }
                    }

                }
            }
        }
    }
}
