using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace InstantGust
{
    /// <summary>
    /// Causes gust to cast immediately 
    /// </summary>
    [BepInPlugin("me.antimality." + PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        internal static ManualLogSource Log;
        private static Harmony harmony;

        private void Awake()
        {
            Log = Logger;

            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");

            harmony = new("me.antimality." + PluginInfo.PLUGIN_NAME);

            harmony.PatchAll(typeof(Patch));
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }

    [HarmonyPatch]
    public class Patch
    {
        /// <summary>
        /// Replaces the original function to Cast the ability immediately instead of after the animation.
        /// 
        /// QuickSpell includes, as far as I can tell, only Gust and Revive
        /// I could target spesifically Gust, but the change for Revive is meaningless
        /// </summary>
        [HarmonyPatch(typeof(QuickSpell), nameof(QuickSpell.OnEnterAbility))]
        [HarmonyPrefix]
        public static bool DontWaitForAnim(ref Player ___player, ref PlayerInfo ___playerInfo, ref SpriteAnimator ___animator, ref SpriteRenderer ___spriteRen,
                                            ref PlayerPhysics ___physics, ref DPhysicsCircle ___smallCollider, ref PlayerBody ___body, ref Vec2 ___EnterAbilityOffset,
                                            ref QuickSpell __instance, ref Ability ___ability, ref AnimationData ___animData, ref string ___AudioToPlayOnEnter)
        {
            // Normal code
            ___playerInfo = ___ability.GetPlayerInfo();
            ___player = PlayerHandler.Get().GetPlayer(___playerInfo.playerId);
            ___spriteRen.material = ___playerInfo.playerMaterial;
            ___physics.SyncPhysicsTo(___playerInfo);
            ___smallCollider.UpdatePhysicsPositions();
            ___body.position = ___playerInfo.position + ___EnterAbilityOffset;
            __instance.transform.position = (Vector3)___body.position;
            ___physics.UnGround();
            ___body.selfImposedVelocity = Vec2.zero;
            ___body.externalVelocity = Vec2.zero;
            if (!string.IsNullOrEmpty(___AudioToPlayOnEnter))
            {
                AudioManager.Get().Play(___AudioToPlayOnEnter);
            }

            // Changed from beginAnimThenDoAction to just beginAnimation
            ___animator.beginAnimation(___animData.GetAnimation("castAir"));

            // Added call to Cast immediately
            MethodInfo CastMethod = typeof(QuickSpell).GetMethod("Cast", BindingFlags.NonPublic | BindingFlags.Instance);
            CastMethod.Invoke(__instance, []);

            // Disable original function
            return false;
        }
    }

}

/*
    Don't Believe what you see, see what you believe.
    - Antimality.
*/
