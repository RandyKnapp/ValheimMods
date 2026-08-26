using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects
{
    public class ModifyStaggerDamage_Character_Damage_Patch
    {
        public static float? HandlingProjectileDamage;

        // Prefix handler invoked by CharacterDamageDispatch (attacker-side outgoing modifier).
        public static void ApplyStaggerModifier(Character __instance, HitData hit, Character attacker)
        {
            if (attacker is Player player && __instance.IsStaggering())
            {
                // Projectile hits carry the value staged on the projectile's ZDO (set by the OnHit
                // prefix, cleared by its postfix); melee reads live. The old code cached the melee
                // value INTO the projectile static and never cleared it, so the first melee stagger
                // multiplier was silently reused for every later hit.
                float multiplier = HandlingProjectileDamage ?? ReadStaggerDamageValue(player);
                hit.ApplyModifier(multiplier);
            }
        }

        public static float ReadStaggerDamageValue(Player player)
        {
            if (Attack_Patch.ActiveAttack != null)
            {
                return 1 + MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                    player, Attack_Patch.ActiveAttack.m_weapon, MagicEffectType.ModifyStaggerDamage, 0.01f);
            }
            else
            {
                return 1 + player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyStaggerDamage, 0.01f);
            }
        }
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
    public class ModifyStaggerDamageProjectileHit_Projectile_OnHit_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Projectile __instance)
        {
            if (__instance == null || __instance.m_nview == null)
            {
                ModifyStaggerDamage_Character_Damage_Patch.HandlingProjectileDamage = null;
                return;
            }

            ModifyStaggerDamage_Character_Damage_Patch.HandlingProjectileDamage =
                __instance.m_nview.GetZDO()?.GetFloat("epic loot modify stagger damage", 1f);
        }

        [UsedImplicitly]
        private static void Postfix()
        {
            ModifyStaggerDamage_Character_Damage_Patch.HandlingProjectileDamage = null;
        }
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
    public class ModifyStaggerDamage_Projectile_Setup_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Character owner, Projectile __instance)
        {
            if (owner != null && owner.IsPlayer() && __instance != null && __instance.m_nview != null)
            {
                var zdo = __instance.m_nview.GetZDO();
                if (zdo == null)
                    return;

                zdo.Set("epic loot modify stagger damage",
                    ModifyStaggerDamage_Character_Damage_Patch.ReadStaggerDamageValue((Player)owner));
            }
        }
    }
}