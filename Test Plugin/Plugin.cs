using BepInEx;
using BepInEx.Logging;
using BoplFixedMath;
using HarmonyLib;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Start documenting Source Code!!!!

// cycle abilities (change which one is in the middle to be switched) - ability storm for refrence
// Or maybe just switch out a random one every time, instead of just the middle

// Time-Stop timer
// rock bounce off the water
// Cancel rock on jump
// Use abilities as rock
// Ride the lightning: Tesla coil is a light bridge
// Only space maps
// Jewish space laser airstike
// reroll abilities once during play
// More than 3 abilities??
// Loadouts
// Rainbow character
// Change default size (challenging, need to mess with sessionhandler)
// Force field ability: basically modified gust: very strong but short range

// TODO: Streamline plugin project creation (create with all refrences and add DOMAIN to PluginInfo)
// TODO: Streamline plugin build (build directly to folder) DONE

/// 
/// Lessons I've learned so far:
/// - Keyboard input works in patch
/// - Position is called more than once per frame
/// - Player constructor only activates on first screen
/// - I can access all players every frame using the static instance of PlayerHandler
/// - The game only records/sends inputs, possibly only inputs for basegame action (abilities, movement, etc.)


namespace MyFirstBoplPlugin
{
    [BepInPlugin("me.antimality." + PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        internal static ManualLogSource Log;
        private static Harmony harmony;

        private void Awake()
        {
            Log = base.Logger;

            // Plugin startup logic
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");

            harmony = new("me.antimality." + PluginInfo.PLUGIN_NAME);

            harmony.PatchAll();
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }

        private void Update()
        {
        }
    }
}

