using BepInEx;
using BepInEx.Logging;
using BoplFixedMath;
using HarmonyLib;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimeStopTimer
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
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

            harmony.PatchAll(typeof(Patch));
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
            TimerTextbox.DisposeAll();
        }

        public static string currentScene;

        private void Update()
        {
            // Delete old textboxes on scene change
            if (currentScene != SceneManager.GetActiveScene().name)
            {
                currentScene = SceneManager.GetActiveScene().name;
                TimerTextbox.DisposeAll();
            }
        }
    }

    [HarmonyPatch]
    public class Patch
    {
        [HarmonyPatch(typeof(TimeStop), nameof(TimeStop.Init))]
        [HarmonyPostfix]
        public static void StartTimeStop(ref int ___casterId)
        {
            new TimerTextbox(___casterId, casting: true);
        }

        [HarmonyPatch(typeof(TimeStop), nameof(TimeStop.End))]
        [HarmonyPrefix]
        public static void EndTimeStop(ref int ___casterId)
        {
            foreach (TimerTextbox box in TimerTextbox.Textboxes)
            {
                if (box.playerID == ___casterId && box.casting)
                {
                    box.Dispose();
                    break;
                }
            }
        }

        [HarmonyPatch(typeof(TimeStop), nameof(TimeStop.UpdateSim))]
        [HarmonyPrefix]
        public static void DuringTimeStop(ref TimeStop __instance, ref float ___duration, ref float ___secondsElapsed, ref int ___casterId)
        {
            foreach (TimerTextbox box in TimerTextbox.Textboxes)
            {
                if (box.playerID == ___casterId && box.casting)
                {
                    box.Update((float)(___duration - ___secondsElapsed) + 2f);
                    break;
                }
            }
        }

        [HarmonyPatch(typeof(CastSpell), nameof(CastSpell.OnEnterAbility))]
        [HarmonyPostfix]
        public static void OnCast(ref GameObject ___spell, ref PlayerInfo ___playerInfo)
        {
            if (___spell.name == "TimeStopSphere")
            {
                new TimerTextbox(___playerInfo.playerId);
            }
        }

        [HarmonyPatch(typeof(CastSpell), nameof(CastSpell.ExitAbility), typeof(AbilityExitInfo))]
        [HarmonyPrefix]
        public static void OnExit(ref PlayerInfo ___playerInfo)
        {
            foreach (TimerTextbox box in TimerTextbox.Textboxes)
            {
                if (box.playerID == ___playerInfo.playerId && !box.casting)
                {
                    box.Dispose();
                    break;
                }
            }
        }

        [HarmonyPatch(typeof(CastSpell), nameof(CastSpell.UpdateSim))]
        [HarmonyPrefix]
        public static void CastingTimeStop(ref GameObject ___spell, ref Fix ___castTime, ref Fix ___timeSinceActivation, ref PlayerInfo ___playerInfo)
        {
            foreach (TimerTextbox box in TimerTextbox.Textboxes)
            {
                if (box.playerID == ___playerInfo.playerId && !box.casting)
                {
                    box.Update((float)(___castTime - ___timeSinceActivation) + 1.25f);
                    break;
                }
            }

        }
    }

    class TimerTextbox
    {
        public static List<TimerTextbox> Textboxes = new List<TimerTextbox>();

        public readonly int playerID;
        public readonly bool casting;
        private Canvas canvas;
        private GameObject textObj;
        private TextMeshProUGUI textComp;
        private RectTransform location;

        public TimerTextbox(int playerID, bool casting = false)
        {
            Textboxes.Add(this);

            this.playerID = playerID;
            this.casting = casting;

            Summon();
        }

        public void Summon()
        {
            canvas = GameObject.Find("AbilitySelectCanvas").GetComponent<Canvas>();

            if (canvas == null || !Plugin.currentScene.Contains("Level"))
            {
                Plugin.Log.LogError($"No suitable canvas! Canvas: {canvas}, Scene: {Plugin.currentScene}");
                return;
            }

            textObj = new GameObject("TimeStopTimer", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(canvas.transform);

            textComp = textObj.GetComponent<TextMeshProUGUI>();

            // Dunno what this does
            textComp.raycastTarget = false;

            // Color of the casting player
            textComp.color = PlayerHandler.Get().GetPlayer(playerID).Color.GetColor("_ShadowColor");
            textComp.fontSize = 50f;
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.font = LocalizedText.localizationTable.GetFont(Settings.Get().Language, false);

            location = textObj.GetComponent<RectTransform>();
            // Moves the refrence point of the textbox to the upper left corner, I think
            location.pivot = new Vector2(0, 1);

            textObj.SetActive(true);
        }

        public void Update(float time)
        {
            if (time < 1)
            {
                Dispose();
                return;
            }

            textComp.text = ((int)time).ToString();

            // TODO: Player location to canvas location

            // Height and width of the screen, roughly
            float canvasHeight = canvas.GetComponent<RectTransform>().rect.height;
            float canvasWidth = canvas.GetComponent<RectTransform>().rect.width;

            // Relative to the middle of the screen
            // Top right (ish)
            int offset = (Textboxes.IndexOf(this)) * 100;
            location.anchoredPosition = new Vector2(canvasWidth / 2 - 200, canvasHeight / 2 - 100 - offset);
        }

        public void Dispose()
        {
            Textboxes.Remove(this);
            GameObject.Destroy(textObj);
        }

        public static void DisposeAll()
        {
            for (int i = Textboxes.Count - 1; i >= 0; i--)
            {
                Textboxes[i].Dispose();
            }
        }
    }
}

/*
    Don't Believe what you see, see what you believe.
    - Antimality.
*/