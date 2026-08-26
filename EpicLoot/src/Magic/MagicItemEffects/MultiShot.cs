using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch]
    public static class MultiShot
    {
        public static bool IsTripleShotActive = false;
        public static int ShotProjectiles = 0;
        public const string CHANCE_KEY = "Chance";
        public const string DAMAGE_KEY = "Damage";
        public const string COSTSCALE_KEY = "CostScale";
        public const string ACCURACY_KEY = "Accuracy";
        public const string PROJECTILES_KEY = "Projectiles";

        private enum PendingShotType { None, TripleBow, DoubleMagic }
        private static PendingShotType _pendingShot = PendingShotType.None;

        private static Dictionary<string, float> GetEffectConfig(string effectType)
        {
            if (MagicItemEffectDefinitions.AllDefinitions != null &&
                MagicItemEffectDefinitions.AllDefinitions.TryGetValue(effectType, out var def))
            {
                return def.Config;
            }
            return null;
        }

        private static bool RollChance(Dictionary<string, float> configuration)
        {
            if (configuration != null && configuration.ContainsKey(CHANCE_KEY) && configuration[CHANCE_KEY] < 1f)
            {
                return UnityEngine.Random.value <= configuration[CHANCE_KEY];
            }
            return true;
        }

        private static int GetProjectileCount(Dictionary<string, float> configuration, int fallback)
        {
            if (configuration != null && configuration.ContainsKey(PROJECTILES_KEY))
            {
                return Mathf.RoundToInt(configuration[PROJECTILES_KEY]);
            }
            return fallback;
        }

        // The proc must be decided BEFORE vanilla consumes ammo: OnAttackTrigger calls UseAmmo
        // first and FireProjectileBurst after. Rolling inside the burst prefix (the old shape)
        // meant the tripled ammo cost was charged one attack late, and the flag leaked into the
        // NEXT attack -- even with a different weapon -- once the last burst had fired.
        [HarmonyPatch(typeof(Attack), nameof(Attack.OnAttackTrigger))]
        [HarmonyPrefix]
        public static void Attack_OnAttackTrigger_Prefix(Attack __instance)
        {
            _pendingShot = PendingShotType.None;
            IsTripleShotActive = false;
            ShotProjectiles = 0;

            if (__instance?.GetWeapon() == null || __instance.m_character == null || !__instance.m_character.IsPlayer())
            {
                return;
            }

            Player player = (Player)__instance.m_character;
            if (player != Player.m_localPlayer)
            {
                return;
            }

            // If a weapon can have both magic effects applied to it this logic will need to be revised.
            if (player.HasActiveMagicEffect(MagicEffectType.TripleBowShot, out float _))
            {
                var cfg = GetEffectConfig(MagicEffectType.TripleBowShot);
                if (RollChance(cfg))
                {
                    _pendingShot = PendingShotType.TripleBow;
                    IsTripleShotActive = true;
                    ShotProjectiles = GetProjectileCount(cfg, 3);
                }
            }
            else if (player.HasActiveMagicEffect(MagicEffectType.DoubleMagicShot, out float _))
            {
                var cfg = GetEffectConfig(MagicEffectType.DoubleMagicShot);
                if (RollChance(cfg))
                {
                    _pendingShot = PendingShotType.DoubleMagic;
                    ShotProjectiles = GetProjectileCount(cfg, 2);
                }
            }
        }

        // Note: the ref HitData.DamageTypes? __state is set to null if no changes are made
        [HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
        [HarmonyPrefix]
        public static void Attack_FireProjectileBurst_Prefix(Attack __instance, ref HitData.DamageTypes? __state)
        {
            __state = null;
            if (_pendingShot == PendingShotType.None)
            {
                return;
            }

            if (__instance?.GetWeapon() == null || __instance.m_character is not Player player ||
                player != Player.m_localPlayer)
            {
                return;
            }

            // Record the damages value so it can be restored after changes
            __state = __instance.GetWeapon().m_shared.m_damages;

            if (_pendingShot == PendingShotType.TripleBow)
            {
                ModifyShot(ref player, ref __instance, GetEffectConfig(MagicEffectType.TripleBowShot), 0.4f, 2f, 1.25f, 3);
            }
            else
            {
                ModifyShot(ref player, ref __instance, GetEffectConfig(MagicEffectType.DoubleMagicShot), 0.66f, 2f, 1.2f, 2);
            }
        }

        private static bool ModifyShot(ref Player player, ref Attack attack, Dictionary<string, float> configuration,
            float damage, float costScale, float accuracy, int projectiles)
        {
            if (configuration != null)
            {
                if (configuration.ContainsKey(DAMAGE_KEY))
                {
                    damage = configuration[DAMAGE_KEY];
                }

                if (configuration.ContainsKey(COSTSCALE_KEY))
                {
                    costScale = configuration[COSTSCALE_KEY];
                }

                if (configuration.ContainsKey(ACCURACY_KEY))
                {
                    accuracy = configuration[ACCURACY_KEY];
                }

                if (configuration.ContainsKey(PROJECTILES_KEY))
                {
                    projectiles = Mathf.RoundToInt(configuration[PROJECTILES_KEY]);
                }
            }

            HitData.DamageTypes weaponDamage = attack.GetWeapon().m_shared.m_damages;
            weaponDamage.Modify(damage);
            attack.GetWeapon().m_shared.m_damages = weaponDamage;

            ModifyAttackCost(player, costScale, attack.GetAttackStamina(), attack.GetAttackEitr(), attack.GetAttackHealth());

            attack.m_projectileAccuracy = attack.m_weapon.m_shared.m_attack.m_projectileAccuracy * accuracy;

            attack.m_projectiles = attack.m_weapon.m_shared.m_attack.m_projectiles * projectiles;
            ShotProjectiles = projectiles;

            return true;
        }

        /// <summary>
        /// Restore the attack damages to previous state if changed by the prefix.
        /// </summary>
        [HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
        public static void Postfix(Attack __instance, ref HitData.DamageTypes? __state)
        {
            if (__state != null)
            {
                __instance.GetWeapon().m_shared.m_damages = __state.Value;
            }
        }

        public static void ModifyAttackCost(Player player, float scale, float stamcost, float eitrcost, float healthcost)
        {
            if (stamcost > 0) { player.UseStamina(stamcost * scale); }
            if (eitrcost > 0) { player.UseEitr(eitrcost * scale); }
            if (healthcost > 0) { player.UseHealth(healthcost * scale); }
        }
    }

    /// <summary>
    /// Patch to remove thrice ammo when using TripleShot
    /// </summary>
    [HarmonyPatch(typeof(Attack))]
    public static class UseAmmoTranspilerPatch
    {
        //[HarmonyDebug]
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Attack.UseAmmo))]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions);
            codeMatcher.MatchStartForward(
                    new CodeMatch(OpCodes.Callvirt),
                    new CodeMatch(OpCodes.Ldarg_1),
                    new CodeMatch(OpCodes.Ldind_Ref),
                    new CodeMatch(OpCodes.Ldc_I4_1),
                    new CodeMatch(OpCodes.Callvirt))
                .ThrowIfNotMatch("Unable to ammo removal for tripleshot.")
                .Advance(4)
                .RemoveInstructions(1)
                .InsertAndAdvance(Transpilers.EmitDelegate(CustomRemoveItem));
            return codeMatcher.Instructions();
        }

        public static bool CustomRemoveItem(Inventory inventory, ItemDrop.ItemData item, int amount)
        {
            if (MultiShot.IsTripleShotActive)
            {
                amount *= MultiShot.ShotProjectiles;
                MultiShot.IsTripleShotActive = false;
                MultiShot.ShotProjectiles = 0;
            }

            return inventory.RemoveItem(item, amount);
        }
    }
}