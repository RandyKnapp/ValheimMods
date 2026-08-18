using System.Collections.Generic;
using EpicLoot.General;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Wagers a number of coins on the hit, and if it kills the target, refunds them.
    public static class Wager {
        // Config default, registered in ShardEffectDefinitions.EffectConfigs.
        public const float DefaultDamagePerCoin = 0.5f;

        private const string DamagePerCoinKey = "DamagePerCoin";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float>
        {
            { DamagePerCoinKey, DefaultDamagePerCoin },
        };

        // The stake riding on the hit currently being processed, so the postfix knows what to refund. The
        // HitData is kept alongside it so a nested Character.Damage (one hit triggering another) can only
        // ever cancel the refund, never pay it out against the wrong hit.
        private static HitData stakedHit;
        private static int stakedCoins;

        // Tooltip: {0} is the stake (the raw rolled value), {1} the damage it buys.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.Wager,
                value => new object[] { value, value * GetDamagePerCoin() });
        }

        // Prefix handler invoked by CharacterDamageDispatch (attacker-side outgoing modifier).
        public static void ModifyOutgoingHit(Character __instance, HitData hit, Character attacker) {
            stakedHit = null;
            stakedCoins = 0;

            if (hit == null || !(attacker is Player player) || player != Player.m_localPlayer) {
                return;
            }

            // Don't burn coins on friendlies.
            if (__instance == null || __instance == player || __instance.IsTamed()) {
                return;
            }

            int stake = Mathf.RoundToInt(player.GetTotalActiveMagicEffectValue(MagicEffectType.Wager));
            if (stake <= 0) {
                return;
            }

            float total = hit.m_damage.EpicLootGetTotalDamage();
            if (total <= 0f) {
                return;
            }

            List<ItemDrop.ItemData> coins = CoinPurse.GetCoinStacks(player);
            if (CoinPurse.GetTotalCoins(coins) < stake) {
                return; // can't cover the stake -- no bonus
            }

            float bonus = stake * GetDamagePerCoin();
            if (bonus <= 0f) {
                return;
            }

            CoinPurse.Spend(player, coins, stake);

            // Flat add, applied as a scale so it splits across the hit's damage types in their existing
            // proportions (and so resistances still apply the way they would to the base hit).
            hit.m_damage.Modify((total + bonus) / total);

            stakedHit = hit;
            stakedCoins = stake;
        }

        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction). Kill detection mirrors
        // StaminaOnKill -- the hit must have dropped the target to (or below) zero health.
        public static void OnDamageDealt(Character __instance, HitData hit) {
            var owedFor = stakedHit;
            var refund = stakedCoins;
            stakedHit = null;
            stakedCoins = 0;

            if (refund <= 0 || owedFor == null || !ReferenceEquals(owedFor, hit) || __instance == null) {
                return;
            }

            if (__instance.GetHealth() > 0f) {
                return; // the bet was lost -- the stake is gone
            }

            CoinPurse.Refund(Player.m_localPlayer, refund);
        }

        private static float GetDamagePerCoin() {
            var config = MagicItemEffectDefinitions.GetEffectConfig(MagicEffectType.Wager);
            if (config != null && config.TryGetValue(DamagePerCoinKey, out var value)) {
                return Mathf.Max(0f, value);
            }
            return DefaultDamagePerCoin;
        }
    }
}
