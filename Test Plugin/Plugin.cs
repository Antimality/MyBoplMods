using BepInEx;
using BepInEx.Logging;
using BoplFixedMath;
using HarmonyLib;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Jewish space laser airstike
// Force field ability: basically modified gust: very strong but short range

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

