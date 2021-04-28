using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch]
    public static class DoubleJump
    {
        public const string DoubleJumpChargeKey = "DoubleJump";

        public static bool IsJumping;
        public static bool IsDoubleJumping;
        public static bool FirstCallToIsOnGround;
        public static readonly Dictionary<Character, bool> GroundContactTracker = new Dictionary<Character, bool>();

        [HarmonyPatch(typeof(Character), nameof(Character.Jump))]
        public static class Character_Jump_Patch
        {
            public static bool Prefix(Character __instance)
            {
                if (__instance.IsPlayer())
                {
                    IsJumping = true;
                    IsDoubleJumping = false;
                    FirstCallToIsOnGround = false;
                }

                return true;
            }

            public static void Postfix(Character __instance)
            {
                if (__instance.IsPlayer())
                {
                    IsJumping = false;
                    IsDoubleJumping = false;
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.IsOnGround))]
        public static class Character_IsOnGround_Patch
        {
            public static void Postfix(Character __instance, ref bool __result)
            {
                if (!FirstCallToIsOnGround && __instance.IsPlayer() && !__result && IsJumping && HasDoubleJumpCharge(__instance))
                {
                    FirstCallToIsOnGround = true;

                    __result = true;
                    IsDoubleJumping = true;
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnJump))]
        public static class Player_OnJump_Patch
        {
            public static void Postfix(Player __instance)
            {
                if (IsJumping && IsDoubleJumping)
                {
                    UseDoubleJumpCharge(__instance);

                    var audioSource = __instance.GetComponent<AudioSource>();
                    if (audioSource == null)
                    {
                        audioSource = __instance.gameObject.AddComponent<AudioSource>();
                        audioSource.outputAudioMixerGroup = AudioMan.instance.m_ambientMixer;
                    }

                    audioSource.PlayOneShot(EpicLoot.Assets.DoubleJumpSFX);
                }
            }
        }

        //UpdateGroundContact
        [HarmonyPatch(typeof(Character), nameof(Character.ResetGroundContact))]
        public static class Character_ResetGroundContact_Patch
        {
            public static bool Prefix(Character __instance)
            {
                if (!__instance.IsPlayer())
                {
                    return true;
                }

                GroundContactTracker.TryGetValue(__instance, out var previousGroundContact);

                if (__instance.m_groundContact && previousGroundContact != __instance.m_groundContact)
                {
                    ResetDoubleJumpCharges(__instance);
                }

                GroundContactTracker[__instance] = __instance.m_groundContact;

                return true;
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        public static class Humanoid_EquipItem_Patch
        {
            public static void Postfix(Humanoid __instance)
            {
                ResetDoubleJumpCharges(__instance);
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
        public static class Humanoid_UnequipItem_Patch
        {
            public static void Postfix(Humanoid __instance)
            {
                ResetDoubleJumpCharges(__instance);
            }
        }

        public static bool HasDoubleJumpCharge(Character character)
        {
            var doubleJumpCharges = character.m_nview.GetZDO().GetInt(DoubleJumpChargeKey);
            return doubleJumpCharges > 0;
        }

        public static void UseDoubleJumpCharge(Character character)
        {
            var doubleJumpCharges = character.m_nview.GetZDO().GetInt(DoubleJumpChargeKey);
            character.m_nview.GetZDO().Set(DoubleJumpChargeKey, doubleJumpCharges - 1);
        }

        public static void ResetDoubleJumpCharges(Character character)
        {
            var player = character.IsPlayer() ? character as Player : null;
            if (player != null && player.HasMagicEquipmentWithEffect(MagicEffectType.DoubleJump))
            {
                // TODO: multiple charges
                const int doubleJumpCharges = 1;
                character.m_nview.GetZDO().Set(DoubleJumpChargeKey, doubleJumpCharges);
            }
            else
            {
                character.m_nview.GetZDO().Set(DoubleJumpChargeKey, 0);
            }
        }
    }
}
