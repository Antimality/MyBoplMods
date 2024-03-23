using BepInEx;
using BepInEx.Logging;
using BoplFixedMath;
using HarmonyLib;
using System.CodeDom;
using System.Collections.Generic;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
            DestroyTextboxes();
        }
       
        public static string currentScene;

        private void Update()
        {
            // Delete old textboxes on scene change
            if (currentScene != SceneManager.GetActiveScene().name)
            {
                currentScene = SceneManager.GetActiveScene().name;
                DestroyTextboxes();
            }
        }

        private void DestroyTextboxes()
        {
            foreach (TimerTextbox box in TimerTextbox.Textboxes)
            {
                box.Destroy();
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
            new TimerTextbox(___casterId);
        }

        [HarmonyPatch(typeof(TimeStop), nameof(TimeStop.End))]
        [HarmonyPrefix]
        public static void EndTimeStop(ref int ___casterId)
        {
            foreach (TimerTextbox box in TimerTextbox.Textboxes)
            {
                if (box.playerID == ___casterId)
                {
                    box.Destroy();
                    break;
                }
            }
        }

        [HarmonyPatch(typeof(TimeStop), nameof(TimeStop.UpdateSim))]
        [HarmonyPrefix]
        public static void DuringTimeStop(ref TimeStop __instance, ref float ___duration, ref float ___secondsElapsed, ref int ___casterId)
        {
            // This shows me how long the ability is going for. The default duration is 10s + 1.5s of animation exit
            //Plugin.Log.LogInfo($"Caster {__instance.GetPlayerId()}, Elapsed: {___secondsElapsed}, Duration: {___duration}");

            foreach (TimerTextbox box in TimerTextbox.Textboxes)
            {
                if (box.playerID == ___casterId)
                {
                    box.Update(((int)((float)(___duration - ___secondsElapsed) + 2)).ToString());
                    break;
                }
            }
        }

        [HarmonyPatch(typeof(CastSpell), nameof(CastSpell.OnEnterAbility))]
        [HarmonyPostfix]
        public static void OnCast(ref PlayerInfo ___playerInfo)
        {
            new TimerTextbox(___playerInfo.playerId);
        }

        [HarmonyPatch(typeof(CastSpell), nameof(CastSpell.ExitAbility), typeof(AbilityExitInfo))]
        [HarmonyPrefix]
        public static void OnExit(ref PlayerInfo ___playerInfo)
        {
            foreach (TimerTextbox box in TimerTextbox.Textboxes)
            {
                if (box.playerID == ___playerInfo.playerId)
                {
                    box.Destroy();
                    break;
                }
            }
        }

        [HarmonyPatch(typeof(CastSpell), nameof(CastSpell.UpdateSim))]
        [HarmonyPrefix]
        public static void CastingTimeStop(ref GameObject ___spell, ref Fix ___castTime, ref Fix ___timeSinceActivation, ref PlayerInfo ___playerInfo)
        {
            // spell - type of spell (TimeStopSphere)
            // Cast time - how long the spell takes to activate in seconds (10s)
            // Time since activation - how long its been since ability started casting (in seconds)
            
            //Plugin.Log.LogInfo($"Spell: {___spell}, casttime: {___castTime}, timesince: {___timeSinceActivation}");

            foreach (TimerTextbox box in TimerTextbox.Textboxes)
            {
                if (box.playerID == ___playerInfo.playerId) {
                    box.Update(((int)((float)(___castTime - ___timeSinceActivation) + 1.5)).ToString());
                    break;
                }
            }

        }
    }

    class TimerTextbox
    {
        public static List<TimerTextbox> Textboxes = new List<TimerTextbox>();

        public readonly int playerID;
        private GameObject textObj;
        private TextMeshProUGUI textComp;
        private RectTransform location;

        public TimerTextbox(int playerID)
        {
            Textboxes.Add(this);

            this.playerID = playerID;

            Summon();
        }

        public void Summon()
        {
            Canvas canvas = GameObject.Find("AbilitySelectCanvas").GetComponent<Canvas>();

            if (canvas == null || !Plugin.currentScene.Contains("Level"))
            {
                Plugin.Log.LogError($"No suitable canvas! Canvas: {canvas}, Scene: {Plugin.currentScene}");
                return;
            }

            // TODO: give more spesific name for the object
            textObj = new GameObject("TimeStopTimer", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(canvas.transform);

            textComp = textObj.GetComponent<TextMeshProUGUI>();

            // Dunno what this does
            textComp.raycastTarget = false;

            textComp.text = "";
            // Color of the casting player
            textComp.color = PlayerHandler.Get().GetPlayer(playerID).Color.GetColor("_ShadowColor");
            textComp.fontSize = 50f;
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.font = LocalizedText.localizationTable.GetFont(Settings.Get().Language, false);

            location = textObj.GetComponent<RectTransform>();
            // Moves the refrence point of the textbox to the upper left corner, I think
            location.pivot = new UnityEngine.Vector2(0, 1);

            // Height and width of the screen, seems to be a little less on the width
            float canvasHeight = canvas.GetComponent<RectTransform>().rect.height;
            float canvasWidth = canvas.GetComponent<RectTransform>().rect.width;

            // Relative to the middle of the screen
            // Top right (ish)
            // TODO: accomidate more players
            location.anchoredPosition = new UnityEngine.Vector2(canvasWidth / 2 - 200, canvasHeight / 2 - 100);

            textObj.SetActive(true);
        }

        public void Update(string text)
        {
            textComp.text = text;

            // TODO: Player location to canvas location
            //Vec2 playerPos = PlayerHandler.Get().GetPlayer(playerID).Position;
            //Plugin.Log.LogInfo(playerPos);
            //location.anchoredPosition = new UnityEngine.Vector2(((int)playerPos.x)*10, (int)playerPos.y);
        }

        public void Destroy()
        {
            Textboxes.Remove(this);
            GameObject.Destroy(textObj);
        }

        public override string ToString()
        {
            return textObj.ToString();
        }
    }
}

