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

        // The attack whose current burst fires the extra projectiles, and the share of damage each of them
        // deals. Set for the length of one FireProjectileBurst.
        private static Attack _scaledBurstAttack;
        private static float _scaledBurstDamage = 1f;

        // True while the local player's current attack fires a triple shot (rolled in OnAttackTrigger, before
        // its bursts), for effects that change how each of its projectiles flies.
        internal static bool IsTripleShotFiring => _pendingShot == PendingShotType.TripleBow;

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
            // Triple shot is checked against the weapon too: from a set bonus it is worn on armour, and would
            // otherwise triple a staff's shots or the grappling hook's.
            var skill = __instance.GetWeapon().m_shared.m_skillType;
            if ((skill == Skills.SkillType.Bows || skill == Skills.SkillType.Crossbows) &&
                player.HasActiveMagicEffect(MagicEffectType.TripleBowShot, out float _))
            {
                var cfg = GetEffectConfig(MagicEffectType.TripleBowShot);
                // Decided here, before vanilla takes the ammo: a proc the player cannot pay for fires an ordinary shot,
                // and must not have taken the extra arrows first.
                if (RollChance(cfg) && CanPayExtraCost(player, __instance, GetCostScale(cfg, 2f)))
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

        [HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
        [HarmonyPrefix]
        public static void Attack_FireProjectileBurst_Prefix(Attack __instance)
        {
            _scaledBurstAttack = null;
            if (_pendingShot == PendingShotType.None)
            {
                return;
            }

            if (__instance?.GetWeapon() == null || __instance.m_character is not Player player ||
                player != Player.m_localPlayer)
            {
                return;
            }

            if (_pendingShot == PendingShotType.TripleBow)
            {
                ModifyShot(ref player, ref __instance, GetEffectConfig(MagicEffectType.TripleBowShot), 0.4f, 2f, 1.25f, 3);
            }
            else
            {
                ModifyShot(ref player, ref __instance, GetEffectConfig(MagicEffectType.DoubleMagicShot), 0.66f, 2f, 1.2f, 2);
            }
        }

        private static float GetCostScale(Dictionary<string, float> configuration, float fallback)
        {
            return configuration != null && configuration.ContainsKey(COSTSCALE_KEY) ? configuration[COSTSCALE_KEY] : fallback;
        }

        private static bool ModifyShot(ref Player player, ref Attack attack, Dictionary<string, float> configuration,
            float damage, float costScale, float accuracy, int projectiles)
        {
            costScale = GetCostScale(configuration, costScale);

            // Paid first, so a proc the player cannot afford fires an ordinary shot and costs nothing extra.
            if (!TryPayExtraCost(player, attack, costScale))
            {
                // A cost vanilla charges once per attack is refused once per attack; a per-burst one is retried
                // each burst. Reset the projectiles in case an earlier burst of this attack was doubled.
                if (!attack.m_perBurstResourceUsage)
                {
                    _pendingShot = PendingShotType.None;
                }

                attack.m_projectileAccuracy = attack.m_weapon.m_shared.m_attack.m_projectileAccuracy;
                attack.m_projectiles = attack.m_weapon.m_shared.m_attack.m_projectiles;
                return false;
            }

            if (configuration != null)
            {
                if (configuration.ContainsKey(DAMAGE_KEY))
                {
                    damage = configuration[DAMAGE_KEY];
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

            // Applied to each projectile's own HitData as vanilla builds it (Attack_ModifyDamage_Postfix), so the
            // whole hit is cut. Scaling the weapon's base m_damages left its upgrades, world level and the ammo
            // at full strength.
            _scaledBurstAttack = attack;
            _scaledBurstDamage = damage;

            attack.m_projectileAccuracy = attack.m_weapon.m_shared.m_attack.m_projectileAccuracy * accuracy;

            attack.m_projectiles = attack.m_weapon.m_shared.m_attack.m_projectiles * projectiles;
            ShotProjectiles = projectiles;

            return true;
        }

        // Attack.ModifyDamage runs once per projectile inside FireProjectileBurst, on the finished HitData
        // (weapon with its upgrades and world level, plus the ammo), just before it is handed to the projectile.
        [HarmonyPatch(typeof(Attack), nameof(Attack.ModifyDamage))]
        [HarmonyPostfix]
        public static void Attack_ModifyDamage_Postfix(Attack __instance, HitData hitData)
        {
            if (_scaledBurstAttack != null && __instance == _scaledBurstAttack && hitData != null)
            {
                hitData.m_damage.Modify(_scaledBurstDamage);
            }
        }

        /// <summary>
        /// Ends the burst's damage scaling. A finalizer, not a postfix, so an exception mid-burst cannot leave
        /// it applying to the attack's later hits.
        /// </summary>
        [HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
        [HarmonyFinalizer]
        public static void Attack_FireProjectileBurst_Finalizer()
        {
            _scaledBurstAttack = null;
        }

        /// <summary>
        /// Charges what the extra projectiles cost, if the player can pay it. CostScale is the whole attack's cost
        /// multiplier (the description's "twice the eitr" is 2), and vanilla charges the first 1x itself, so only
        /// CostScale - 1 is ours. Charging the full CostScale on top made a double shot cost three times.
        ///
        /// Vanilla charges its share once, as the attack starts (before any burst), or with m_perBurstResourceUsage
        /// (Staff of Frost, ...) on every burst, checking and charging right after this prefix and Stop()ing the
        /// attack when the player cannot pay. In the per-burst case the check has to cover both shares: charging
        /// ours first left too little for vanilla's, so the eitr was taken and nothing was fired.
        /// </summary>
        private static bool TryPayExtraCost(Player player, Attack attack, float costScale)
        {
            bool perBurst = attack.m_perBurstResourceUsage;
            if (!perBurst && attack.m_projectileBurstsFired > 0)
            {
                // Paid on this attack's first burst, matching vanilla's one charge per attack.
                return true;
            }

            if (!CanPayExtraCost(player, attack, costScale))
            {
                return false;
            }

            float extra = Mathf.Max(0f, costScale - 1f);
            float extraStamina = attack.GetAttackStamina() * extra;
            float extraEitr = attack.GetAttackEitr() * extra;

            if (extraStamina > 0f) { player.UseStamina(extraStamina); }
            if (extraEitr > 0f) { player.UseEitr(extraEitr); }

            // Not gated, as vanilla does not gate its own. Clamp to leave 1 HP, as vanilla does at both of its own
            // attack-health spends (Attack.cs DoMeleeAttack / FireProjectileBurst): Character.UseHealth clamps to 0,
            // not 1, so an unclamped charge here can take the player to 0 and kill them.
            float extraHealth = attack.GetAttackHealth() * extra;
            if (extraHealth > 0f) { player.UseHealth(Mathf.Min(player.GetHealth() - 1f, extraHealth)); }

            return true;
        }

        // The check half of TryPayExtraCost: whether the player has what the extra projectiles cost.
        private static bool CanPayExtraCost(Player player, Attack attack, float costScale)
        {
            bool perBurst = attack.m_perBurstResourceUsage;
            float extra = Mathf.Max(0f, costScale - 1f);
            float stamina = attack.GetAttackStamina();
            float eitr = attack.GetAttackEitr();
            float extraStamina = stamina * extra;
            float extraEitr = eitr * extra;

            // UseStamina/UseEitr scale by the world's resource rates; vanilla's own HaveStamina/HaveEitr checks do not.
            if (extraStamina > 0f && !player.HaveStamina((perBurst ? stamina : 0f) + extraStamina * Game.m_staminaRate))
            {
                return false;
            }

            return !(extraEitr > 0f) || player.HaveEitr((perBurst ? eitr : 0f) + extraEitr * Game.m_eitrRate);
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