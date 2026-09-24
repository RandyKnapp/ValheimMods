using HarmonyLib;
using UnityEngine;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers {
    // Where the hit Character.Damage is processing came from, for the attacker-side dispatcher
    // (SharedCharacterDamagePatch) to tell weapon strikes apart from everything else.
    //
    // Bonus hits: damage EpicLoot deals on its own -- Reflect, the Eikthyr charge, FrostAOE, Trailblazer's fire,
    // the Moder and Bonemass novas, the meteor and chain lightning -- carries the local player as attacker, so
    // without a tag every weapon-strike effect (crits, double damage, Wager and Mercenary stakes, lifesteal,
    // Executioner, Slow, Paralyze, Eikthyr charges) rolled on it as well, once per target it touched.
    //  * Direct damage goes through DealBonusDamage, which holds the tag for the call: Character.Damage runs the
    //    whole attacker-side dispatch synchronously.
    //  * Damage that lands later, from a spawned vanilla Aoe or Projectile, is tagged with MarkBonusSource. The
    //    patches below hold the tag while a marked object deals damage, and mark whatever such an object sets up
    //    while tagged (a projectile's spawn-on-hit). Chain lightning's jumps are Instantiate clones of the live
    //    strike, so they carry the marker from the start.
    //
    // Firing weapon: a projectile lands long after its attack finished, when the hands may hold something else --
    // or nothing, for a thrown weapon, which vanilla unequips as it leaves the hand. While a local player's
    // Projectile or Aoe deals damage, FiringWeapon is the weapon that fired it (Projectile.m_weapon /
    // Aoe.m_itemData), which MagicEffectsHelper.GetActiveWeapon prefers over the hands.
    public static class HitSource {
        private static int _bonusDepth;

        public static bool IsBonusHit => _bonusDepth > 0;

        public static ItemDrop.ItemData FiringWeapon { get; private set; }

        public static void DealBonusDamage(IDestructible target, HitData hit) {
            _bonusDepth++;
            try {
                target.Damage(hit);
            } finally {
                _bonusDepth--;
            }
        }

        public static void MarkBonusSource(GameObject source) {
            if (source != null && !source.TryGetComponent(out BonusSource _)) {
                source.AddComponent<BonusSource>();
            }
        }

        // Saved on entry to a hook and handed back on exit, so nested hooks (Projectile.OnHit calling DoAOE)
        // restore what they found.
        internal struct Scope {
            public bool Bonus;
            public ItemDrop.ItemData PreviousWeapon;
        }

        internal static Scope Enter(Component source, Character owner, ItemDrop.ItemData weapon) {
            var scope = new Scope { PreviousWeapon = FiringWeapon };
            if (source != null && source.TryGetComponent(out BonusSource _)) {
                scope.Bonus = true;
                _bonusDepth++;
            }
            if (weapon != null && owner != null && owner == Player.m_localPlayer) {
                FiringWeapon = weapon;
            }
            return scope;
        }

        internal static void Exit(Scope scope) {
            if (scope.Bonus) {
                _bonusDepth--;
            }
            FiringWeapon = scope.PreviousWeapon;
        }

        internal static void MarkIfBonus(Component instance) {
            if (IsBonusHit && instance != null) {
                MarkBonusSource(instance.gameObject);
            }
        }

        public sealed class BonusSource : MonoBehaviour {
        }
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
    internal static class HitSource_Projectile_OnHit_Patch {
        [HarmonyPrefix]
        private static void Prefix(Projectile __instance, out HitSource.Scope __state) {
            __state = HitSource.Enter(__instance, __instance.m_owner, __instance.m_weapon);
        }

        [HarmonyFinalizer]
        private static void Finalizer(HitSource.Scope __state) => HitSource.Exit(__state);
    }

    // The in-flight damage path (m_hitMidFlight) reaches DoAOE from FixedUpdate rather than OnHit.
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.DoAOE))]
    internal static class HitSource_Projectile_DoAOE_Patch {
        [HarmonyPrefix]
        private static void Prefix(Projectile __instance, out HitSource.Scope __state) {
            __state = HitSource.Enter(__instance, __instance.m_owner, __instance.m_weapon);
        }

        [HarmonyFinalizer]
        private static void Finalizer(HitSource.Scope __state) => HitSource.Exit(__state);
    }

    [HarmonyPatch(typeof(Aoe), nameof(Aoe.OnHit))]
    internal static class HitSource_Aoe_OnHit_Patch {
        [HarmonyPrefix]
        private static void Prefix(Aoe __instance, out HitSource.Scope __state) {
            __state = HitSource.Enter(__instance, __instance.m_owner, __instance.m_itemData);
        }

        [HarmonyFinalizer]
        private static void Finalizer(HitSource.Scope __state) => HitSource.Exit(__state);
    }

    // Chain jumps are spawned from CustomFixedUpdate, not from OnHit. They normally inherit the marker by being
    // clones of the live strike; holding the tag here as well marks them through the Setup postfix should the
    // prefab ever point m_chainObj somewhere else. Only chaining Aoes pay for the component lookup.
    [HarmonyPatch(typeof(Aoe), nameof(Aoe.CustomFixedUpdate))]
    internal static class HitSource_Aoe_CustomFixedUpdate_Patch {
        [HarmonyPrefix]
        private static void Prefix(Aoe __instance, out HitSource.Scope __state) {
            __state = __instance.m_chainStartChance > 0f
                ? HitSource.Enter(__instance, null, null)
                : new HitSource.Scope { PreviousWeapon = HitSource.FiringWeapon };
        }

        [HarmonyFinalizer]
        private static void Finalizer(HitSource.Scope __state) => HitSource.Exit(__state);
    }

    [HarmonyPatch(typeof(Aoe), nameof(Aoe.Setup))]
    internal static class HitSource_Aoe_Setup_Patch {
        [HarmonyPostfix]
        private static void Postfix(Aoe __instance) => HitSource.MarkIfBonus(__instance);
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
    internal static class HitSource_Projectile_Setup_Patch {
        [HarmonyPostfix]
        private static void Postfix(Projectile __instance) => HitSource.MarkIfBonus(__instance);
    }
}
