using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using System.Threading;
using TMPro;
using UnityEngine;

namespace SuddenDeathTimer
{
    [BepInPlugin("me.antimality." + PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static ManualLogSource logger;
        private static Harmony harmony;

        private static TextMeshProUGUI textComp;

        private void Awake()
        {
            logger = base.Logger;

            logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");

            harmony = new("me.antimality." + PluginInfo.PLUGIN_NAME);

            harmony.PatchAll(typeof(Patch));
        }

        internal static void Log(string message, bool err = false)
        {
            if (err) logger.LogError(message);
            else logger.LogInfo(message);
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
            Patch.Timer.Dispose();
        }
    }

    [HarmonyPatch]
    public class Patch
    {
        /* Patches */

        /// <summary>
        /// Create a new timer when a match starts
        /// </summary>
        [HarmonyPatch(typeof(GameSessionHandler), "SpawnPlayers")]
        [HarmonyPostfix]
        public static void SummonTimerTextbox()
        {
            new Timer();
        }

        // Update timer as long as a game is running
        [HarmonyPatch(typeof(GameSessionHandler), nameof(GameSessionHandler.UpdateSim))]
        [HarmonyPostfix]
        public static void UpdateTimer()
        {
            Timer.Update();
        }


        /// <summary>
        /// Dispose of timer once you leave the game
        /// </summary>
        [HarmonyPatch(typeof(GameSessionHandler), nameof(GameSessionHandler.LeaveGame))]
        [HarmonyPostfix]
        public static void DisposeTimer()
        {
            Timer.Dispose();
        }


        /* Timer Object */

        internal class Timer
        {
            // Singleton
            private static Timer instance = null;

            // Timer objects
            private GameObject textObj;
            private TextMeshProUGUI textComp;

            // Time to reach Sudden Death
            private static int maxTime = 120;

            /// <summary>
            /// Create a new timer at the top of the screen
            /// </summary>
            internal Timer()
            {
                // Override previous timer
                Dispose();
                instance = this;

                // Read the time to reach Sudden Death
                maxTime = (int)GetGameSessionHandler().TimeBeforeSuddenDeath;

                // I don't understand why this is the correct Canvas, but it is
                Canvas canvas = GameObject.Find("AbilitySelectCanvas").GetComponent<Canvas>();

                // If canvas doesn't exist yet
                if (canvas == null) throw new MissingReferenceException("Game canvas doesn't exist yet!");

                // Create text object
                textObj = new GameObject("TimeStopTimer", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(canvas.transform);

                // Create text component
                textComp = textObj.GetComponent<TextMeshProUGUI>();

                // Dunno what this does
                //textComp.raycastTarget = false;

                textComp.fontSize = 50f;
                textComp.alignment = TextAlignmentOptions.Center;
                textComp.font = LocalizedText.localizationTable.GetFont(Settings.Get().Language, false);

                // Sets the textbox to the top of the screen
                RectTransform location = textObj.GetComponent<RectTransform>();
                float canvasHeight = canvas.GetComponent<RectTransform>().rect.height;
                location.anchoredPosition = new Vector2(0, canvasHeight / 2 - 50);

                textObj.SetActive(true);
            }

            /// <summary>
            /// Update timer
            /// </summary>
            internal static void Update()
            {
                // Skip if no timer exists yet
                if (instance == null) return;

                // Get time ingame
                int time = (int)Updater.SimTimeSinceLevelLoaded;

                // Calculate time left (rounded up)
                int timeToSuddenDeath = (int)(maxTime - time) + 1;

                // TODO: Set size and/or color proportionate to time left (doesn't seem to work)

                // Update text
                instance.textComp.text = timeToSuddenDeath.ToString();

                // Dispose if game has ended or Sudden Death started
                if (GameSessionHandler.HasGameEnded() || timeToSuddenDeath <= 0) Dispose();
            }


            /// <summary>
            /// Dispose of the timer
            /// </summary>
            internal static void Dispose()
            {
                if (instance != null) GameObject.Destroy(instance.textObj);
            }
        }

        /* Stolen code from Splotch */
        /// <summary>
        /// Gets the GameSessionHandler
        /// </summary>
        /// <returns>the GameSessionHandler or null if it isn't instantized</returns>
        internal static GameSessionHandler GetGameSessionHandler()
        {
            FieldInfo selfRefField = typeof(GameSessionHandler).GetField("selfRef", BindingFlags.Static | BindingFlags.NonPublic);
            return selfRefField.GetValue(null) as GameSessionHandler;
        }
    }
}

/*
    Don't Believe what you see, see what you believe.
    - Antimality.
*/
