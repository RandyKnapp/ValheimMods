using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string Sniper = nameof(Sniper);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // Sniper (set items only, NoRoll). The value is a damage bonus per metre: a bow shot that hits further than
    // MinDistance from where it was fired deals +value% damage for every metre between the two, with no cap
    // (0.8/1/1.2 at L/M/A: +12/15/18% just past 15 m, +80/100/120% at 100 m). The arrow also flies 1 + SpeedBonusPerValue
    // x value times as fast, and the bow takes 1 + DrawTimeBonusPerValue x value times as long to draw (2/2.25/2.5x for
    // both). The rarity raises all of it.
    //
    // The draw is slowed by lengthening the weapon's draw duration for the length of GetAttackDrawPercentage, not
    // by dividing its result: vanilla clamps the result to 1, so a divided full draw would never complete. Every
    // reader of the draw (crosshair ring, the "drawpercent" animation, stamina drain, and the arrow's velocity,
    // accuracy and damage at release) goes through that method, so a half-drawn shot stays a half-drawn shot.
    // QuickDraw's postfix on the same method still shortens the longer draw.
    //
    // Speed is applied per arrow in Projectile.Setup, on the firing client (the projectile's owner simulates it), which
    // also notes the arrow's value. The damage is added in Projectile.OnHit, which also runs there, for that one hit:
    // the distance is a straight line from vanilla's m_startPoint (where Setup found the arrow) to the hit point.
    // Crossbows, staffs and the grappling hook are left alone.
    public static class Sniper
    {
        // All tunable in this effect's Config block in magiceffects.json, under these key names.
        public const float DefaultSpeedBonusPerValue = 1.25f;      // extra arrow speed (x) per point of value
        public const float DefaultDrawTimeBonusPerValue = 1.25f;   // extra draw time (x) per point of value
        public const float DefaultMinDistance = 15f;               // metres; a hit at this range or closer gets nothing

        private const string SpeedBonusPerValueKey = "SpeedBonusPerValue";
        private const string DrawTimeBonusPerValueKey = "DrawTimeBonusPerValue";
        private const string MinDistanceKey = "MinDistance";

        private const string GrapplingHookName = "$item_graplinghook";

        // Player-editable, so each is kept non-negative.
        private static float SpeedBonusPerValue => EffectConfig.GetClamped(MagicEffectType.Sniper, SpeedBonusPerValueKey, DefaultSpeedBonusPerValue, 0f, 10f);
        private static float DrawTimeBonusPerValue => EffectConfig.GetClamped(MagicEffectType.Sniper, DrawTimeBonusPerValueKey, DefaultDrawTimeBonusPerValue, 0f, 10f);
        private static float MinDistance => EffectConfig.GetClamped(MagicEffectType.Sniper, MinDistanceKey, DefaultMinDistance, 0f, 1000f);

        // The value a shot was fired with, kept with the arrow until it hits.
        private class Shot
        {
            public float Value;
        }

        private static readonly ConditionalWeakTable<Projectile, Shot> Shots = new ConditionalWeakTable<Projectile, Shot>();

        // Tooltip: "... {1}x as fast, and those that hit beyond {3} m deal +{0}% damage per metre flown, but ... {2}x as
        // long to draw."
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.Sniper,
                value => new object[] { value, GetSpeedMultiplier(value), GetDrawTimeMultiplier(value), MinDistance });
        }

        // Never below 1, so a bad edit can not slow the arrow or speed up the draw.
        private static float GetSpeedMultiplier(float value)
        {
            return 1f + Mathf.Max(0f, value) * SpeedBonusPerValue;
        }

        private static float GetDrawTimeMultiplier(float value)
        {
            return 1f + Mathf.Max(0f, value) * DrawTimeBonusPerValue;
        }

        // Percent, for a hit this many metres from where the arrow was fired.
        private static float GetDamageBonus(float value, float distance)
        {
            return distance > MinDistance ? Mathf.Max(0f, value * distance) : 0f;
        }

        internal static bool IsSniperBow(ItemDrop.ItemData item)
        {
            return item != null &&
                   item.m_shared.m_skillType == Skills.SkillType.Bows &&
                   item.m_shared.m_attack.m_bowDraw &&
                   item.m_shared.m_name != GrapplingHookName;
        }

        // Humanoid, not Player: this method runs for every humanoid (Draugr/Fuling archers), and Harmony passes
        // __instance through without a type check. Prefix and finalizer, because m_shared is the descriptor
        // shared by every copy of the bow, so the restore must run even when the original throws.
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetAttackDrawPercentage))]
        private static class Humanoid_GetAttackDrawPercentage_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Humanoid __instance, ref float __state)
            {
                __state = -1f;
                if (__instance is not Player player || player != Player.m_localPlayer)
                {
                    return;
                }

                ItemDrop.ItemData weapon = player.GetCurrentWeapon();
                if (!IsSniperBow(weapon) || !player.HasActiveMagicEffect(MagicEffectType.Sniper, out float value))
                {
                    return;
                }

                __state = weapon.m_shared.m_attack.m_drawDurationMin;
                weapon.m_shared.m_attack.m_drawDurationMin *= GetDrawTimeMultiplier(value);
            }

            [HarmonyFinalizer]
            private static void Finalizer(Humanoid __instance, float __state)
            {
                if (__state < 0f)
                {
                    return;
                }

                ItemDrop.ItemData weapon = __instance.GetCurrentWeapon();
                if (weapon != null)
                {
                    weapon.m_shared.m_attack.m_drawDurationMin = __state;
                }
            }
        }

        // Once Setup has run, the arrow keeps its value for the hit and is marked for its look (SniperVisuals).
        [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
        private static class Projectile_Setup_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Character owner, ref Vector3 velocity, ItemDrop.ItemData item, out float __state)
            {
                __state = -1f;
                if (owner == null || owner != Player.m_localPlayer || !IsSniperBow(item) ||
                    !Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.Sniper, out float value))
                {
                    return;
                }

                velocity *= GetSpeedMultiplier(value);
                __state = value;
            }

            [HarmonyPostfix]
            private static void Postfix(Projectile __instance, float __state)
            {
                if (__state < 0f)
                {
                    return;
                }

                Shots.Remove(__instance);
                Shots.Add(__instance, new Shot { Value = __state });
                SniperVisuals.MarkArrow(__instance);
            }
        }

        // The arrow's damage is raised for this hit only and put back after, even if the hit throws: OnHit can run more
        // than once for one arrow (a bounce, a pierce), each time at its own distance.
        [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
        private static class Projectile_OnHit_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Projectile __instance, Vector3 hitPoint, out HitData.DamageTypes? __state)
            {
                __state = null;
                if (!Shots.TryGetValue(__instance, out Shot shot))
                {
                    return;
                }

                float distance = Vector3.Distance(__instance.m_startPoint, hitPoint);
                float bonus = GetDamageBonus(shot.Value, distance);
                if (bonus <= 0f)
                {
                    return;
                }

                __state = __instance.m_damage;
                __instance.m_damage.Modify(1f + bonus / 100f);
                EpicLoot.Log($"[Sniper] Hit at {distance:0.#} m: +{bonus:0.#}% damage.");
            }

            [HarmonyFinalizer]
            private static void Finalizer(Projectile __instance, HitData.DamageTypes? __state)
            {
                if (__state.HasValue)
                {
                    __instance.m_damage = __state.Value;
                }
            }
        }
    }
}
