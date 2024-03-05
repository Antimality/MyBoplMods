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

// Enahanced Grapple:
// Longer/stronger grapple
// more than one grapple at a time
// Detatch button

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

    [HarmonyPatch]
    public class patch
    {
        //[HarmonyPatch(typeof(Rope))]
        //[HarmonyPatch(MethodType.Constructor)]
        //[HarmonyPostfix]
        //public static void Test1(Rope __instance)
        //{
        //    __instance.maxTravelTime = (Fix) 3;
        //}

        [HarmonyPatch(typeof(Rope))]
        [HarmonyPatch(nameof(Rope.UpdateSim))]
        [HarmonyPrefix]
        public static void Test2(ref Fix ___maxTravelTime, ref Fix ___timePassed)
        {
            //Plugin.Log.LogInfo($"update: {___maxTravelTime}, {___timePassed}");
        }

        [HarmonyPatch(typeof(HookshotInstant), "FireHook")]
        [HarmonyPrefix]
        public static bool FireHookOverride(ref RopeBody ___ropeBody, ref bool ___hasHadCooldownSet, ref PlayerInfo ___playerInfo, ref uint ___firedRopeNumber,
                                            ref Rope ___ropePrefab, ref Fix ___hookSpawnOffset, ref Fix ___cooldown, ref Fix ___startingSegmentSeparation,
                                            ref RopeHook ___hookPrefab, ref Fix ___hookSpeed, ref InstantAbility ___instantAbility, ref FixTransform ___fixTrans)
        {
            ___hasHadCooldownSet = false;
            Player player = PlayerHandler.Get().GetPlayer(___playerInfo.playerId);
            AudioManager.Get().Play("hookshotFire");
            if (___playerInfo.useRopeOnOtherPlayer)
            {
                Vec2 vec = ___playerInfo.playerToUseRopeOn.fixTrans.position + player.AimVector() * ___hookSpawnOffset;
                Rope rope = FixTransform.InstantiateFixed(___ropePrefab, vec);
                rope.maxTravelTime = Fix.MaxValue;
                rope.maxRopesPerPlayer = 3;
                rope.nrOfSegments = 30;
                int playerId = ___playerInfo.playerToUseRopeOn.fixTrans.GetComponent<IPlayerIdHolder>().GetPlayerId();
                RopeBody ropeBody = rope.Initialize(vec, ___playerInfo.playerToUseRopeOn.fixTrans.position, ___playerInfo.isGrounded, ___playerInfo.playerId, ___startingSegmentSeparation, ___playerInfo.playerMaterial);
                ___firedRopeNumber = ropeBody.number;
                FixTransform.InstantiateFixed(___hookPrefab, vec).Initialize(ropeBody, topAttachment: true, ((player.AimVector() == Vec2.zero) ? Vec2.right : player.AimVector()) * ___hookSpeed, rope, playerId);
                ___playerInfo.slimeController.body.selfImposedVelocity = Vec2.zero;
                PlayerBody component = ___playerInfo.playerToUseRopeOn.fixTrans.GetComponent<PlayerBody>();
                if (component != null)
                {
                    component.selfImposedVelocity = Vec2.zero;
                    component.AttachRope(null, topAttachment: false);
                    component.AttachRope(ropeBody, topAttachment: false);
                }
                else
                {
                    BounceBall component2 = ___playerInfo.playerToUseRopeOn.fixTrans.GetComponent<BounceBall>();
                    component2?.AttachRope(null, topAttachment: false);
                    component2?.AttachRope(ropeBody, topAttachment: false);
                }
                ___hasHadCooldownSet = true;
                ___instantAbility?.SetCoolDown(___cooldown);
            }
            else
            {
                Vec2 vec2 = ___fixTrans.position + player.AimVector() * ___hookSpawnOffset;
                Rope rope2 = FixTransform.InstantiateFixed(___ropePrefab, vec2);
                rope2.maxTravelTime = Fix.MaxValue;
                rope2.maxRopesPerPlayer = 3;
                rope2.nrOfSegments = 30;
                RopeBody ropeBody = rope2.Initialize(vec2, ___playerInfo.slimeController.body.position, ___playerInfo.isGrounded, ___playerInfo.playerId, ___startingSegmentSeparation, ___playerInfo.playerMaterial);
                ___firedRopeNumber = ropeBody.number;
                FixTransform.InstantiateFixed(___hookPrefab, vec2).Initialize(ropeBody, topAttachment: true, ((player.AimVector() == Vec2.zero) ? Vec2.right : player.AimVector()) * ___hookSpeed, rope2, ___playerInfo.playerId);
                ___playerInfo.slimeController.body.selfImposedVelocity = Vec2.zero;
                ___playerInfo.slimeController.body.AttachRope(null, topAttachment: false);
                ___playerInfo.slimeController.body.AttachRope(ropeBody, topAttachment: false);
                ___ropeBody = ropeBody;
            }



            Plugin.Log.LogInfo("TEST");
            return false;
        }
    }
}

