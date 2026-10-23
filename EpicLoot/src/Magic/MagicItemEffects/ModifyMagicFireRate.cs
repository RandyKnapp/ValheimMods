using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects;

// Fire Rate (bows) and Magic Fire Rate (staves). Shortening the pause between an attack's projectile bursts only
// speeds up a weapon that fires several, like the Staff of Frost's stream; every bow, crossbow and most staves fire
// one burst, so on them the effect did nothing. A single-burst attack instead plays faster, the way Attack Speed
// works for melee: a bow's release and next nock, a staff's cast. Each attack gets one of the two, never both. A
// bow's draw happens before its attack starts and is untouched (Quick Draw's job).
public class ModifyMagicFireRate
{
    // The fire rate effect that suits the weapon, or null when neither does.
    private static string GetFireRateEffect(ItemDrop.ItemData weapon)
    {
        if (weapon?.m_shared == null)
        {
            return null;
        }

        if (weapon.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow)
        {
            return MagicEffectType.ModifyFireRate;
        }

        Skills.SkillType skill = weapon.m_shared.m_skillType;
        return skill == Skills.SkillType.ElementalMagic || skill == Skills.SkillType.BloodMagic
            ? MagicEffectType.ModifyMagicFireRate
            : null;
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.UpdateProjectile))]
    public static class ModifyFireRate_Attack_UpdateProjectile_Patch
    {
        public static void Prefix(Attack __instance, out float __state)
        {
            __state = -1f;
            if (__instance.m_projectileBursts <= 1 || __instance.m_character is not Player player)
            {
                return;
            }

            string effect = GetFireRateEffect(__instance.GetWeapon());
            if (effect != null && player.HasActiveMagicEffect(effect, out float value, 0.01f))
            {
                __state = __instance.m_burstInterval;
                __instance.m_burstInterval *= 1 - value;
            }
        }

        public static void Postfix(Attack __instance, float __state)
        {
            if (__state >= 0f)
            {
                __instance.m_burstInterval = __state;
            }
        }
    }

    // The single-burst half, through the same AnimationSpeedManager the attack speed effects use.
    public static double ModifyFireAnimationSpeed(Character character, double speed)
    {
        if (character is not Player player || !player.InAttack() || player.m_currentAttack == null)
        {
            return speed;
        }

        Attack attack = player.m_currentAttack;
        if (attack.m_attackType != Attack.AttackType.Projectile || attack.m_projectileBursts > 1)
        {
            return speed;
        }

        string effect = GetFireRateEffect(attack.GetWeapon());
        if (effect == null)
        {
            return speed;
        }

        float value = player.GetTotalActiveMagicEffectValue(effect, 0.01f);
        return value > 0f ? speed * (1.0d + value) : speed;
    }

    // Added once, like the attack speed effects' handler.
    [HarmonyPatch(typeof(Game), nameof(Game.Awake))]
    private static class Game_Awake_Patch
    {
        private static bool _registered;

        [UsedImplicitly]
        private static void Postfix()
        {
            if (_registered)
            {
                return;
            }

            _registered = true;
            AnimationSpeedManager.Add(ModifyFireAnimationSpeed);
        }
    }
}
