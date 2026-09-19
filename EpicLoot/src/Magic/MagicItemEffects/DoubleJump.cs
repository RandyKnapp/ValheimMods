using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch]
    public static class DoubleJump
    {
        // This can use the magic effect system to track more charges in the future
        public static int MultiJumpCombo = 0;

        // Reset on ground contact, not only on a grounded Jump call: walking off a ledge after
        // using the double jump used to leave the counter latched, refusing the air jump until the
        // next grounded jump.
        [HarmonyPatch(typeof(Character), "UpdateGroundContact")]
        public static class Character_UpdateGroundContact_Patch
        {
            public static void Postfix(Character __instance)
            {
                if (MultiJumpCombo != 0 && __instance == Player.m_localPlayer && __instance.IsOnGround())
                {
                    MultiJumpCombo = 0;
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.Jump))]
        public static class Character_Jump_Patch
        {
            public static bool Prefix(Character __instance)
            {
                if (Player.m_localPlayer == null || __instance != Player.m_localPlayer)
                {
                    return true;
                }

                if (__instance.IsOnGround())
                {
                    MultiJumpCombo = 0;
                    return true;
                }

                // Character.Jump() has an airborne branch of its own: it re-pulls an attached
                // grappling hook. Swallowing the original method used to cost nothing (a jump in
                // mid-air did nothing at all), but now it eats every grapple re-pull, so hand the
                // input back to vanilla whenever that branch is the one that wants it. No combo is
                // spent - a grapple pull is not a jump.
                if (WantsGrapplePull(__instance))
                {
                    return true;
                }

                MultiJumpCombo++;

                var value = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.DoubleJump);
                if (MultiJumpCombo > value)
                {
                    return false;
                }

                MultiJump(__instance, MultiJumpCombo);
                return false;
            }
        }

        // Mirrors the grappling condition in Character.Jump(). m_localGrappler is never cleared on
        // break, so this leans on Unity's overloaded != to treat a destroyed hook as null.
        private static bool WantsGrapplePull(Character character)
        {
            var grappler = GrapplingPoint.m_localGrappler;
            return grappler != null
                   && Vector3.Distance(grappler.transform.position, character.transform.position)
                        > grappler.m_jumpPullMaxDist;
        }

        public static void MultiJump(Character player, float jumpsize)
        {
            if (player.IsEncumbered() || player.InDodge() || player.IsKnockedBack() || player.IsStaggering())
            {
                return;
            }

            if (!player.HaveStamina(player.m_jumpStaminaUsage))
            {
                Hud.instance.StaminaBarEmptyFlash();
                return;
            }

            float speed = player.m_speed;
            player.m_seman.ApplyStatusEffectSpeedMods(ref speed, player.m_currentVel);
            float skillFactor = 0f;
            Skills skills = player.GetSkills();
            if (skills != null)
            {
                skillFactor = skills.GetSkillFactor(Skills.SkillType.Jump);
                player.RaiseSkill(Skills.SkillType.Jump);
            }

            Vector3 jump = player.m_body.linearVelocity;
            Vector3 playerUp = (new Vector3() + Vector3.up).normalized;
            float skillOffset = 1f + skillFactor * 0.4f;
            float jumpForce = player.m_jumpForce * skillOffset;

            // Normalize as if on flat ground
            jump.y = 0;
            jump += playerUp * jumpForce;

            player.m_seman.ApplyStatusEffectJumpMods(ref jump);
            if (!(jump.x <= 0f) || !(jump.y <= 0f) || !(jump.z <= 0f))
            {
                player.ForceJump(jump);
            }
        }
    }
}
