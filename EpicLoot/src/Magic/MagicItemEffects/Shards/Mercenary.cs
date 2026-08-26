using System.Collections.Generic;
using EpicLoot.General;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Mercenary -- Spend coins on each hit to boost that hit's damage.
    // The cost is a flat fee plus a percentage of the whole purse, so it scales with wealth; the shard value is
    // the rate at which those spent coins convert into bonus damage. Read from the attacking weapon (the
    // shard is socketed there), so it only fires for that weapon. If the player can't cover the cost, no
    // coins are spent and no bonus is given.
    //
    //   coinsSpent = FlatCoinCost + round(totalCoins * CoinPercent%)
    //   rawBonus%  = BaseDamage + (coinsSpent * conversion * 0.01)
    //   final%     = softCap(rawBonus)   -- exponential decay above SoftCap, asymptotic to SoftCap+SoftCapScale
    //
    // The gap between the soft cap and the ceiling is deliberate headroom: at the current top conversion
    // rate (0.5) the decay only bites past ~100k coins, leaving room for future rarities with better
    // conversion rates to climb further without a formula change.
    public static class Mercenary
    {
        // Config defaults, registered in ShardEffectDefinitions.EffectConfigs and read back through
        // MagicItemEffectDefinitions.GetEffectConfig so they are retunable without a rebuild.
        public const float DefaultBaseDamage = 5f;
        public const float DefaultFlatCoinCost = 10f;
        public const float DefaultCoinPercent = 5f;
        public const float DefaultSoftCap = 30f;
        public const float DefaultSoftCapScale = 70f;

        private const string BaseDamageKey = "BaseDamage";
        private const string FlatCoinCostKey = "FlatCoinCost";
        private const string CoinPercentKey = "CoinPercent";
        private const string SoftCapKey = "SoftCap";
        private const string SoftCapScaleKey = "SoftCapScale";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float>
        {
            { BaseDamageKey, DefaultBaseDamage },
            { FlatCoinCostKey, DefaultFlatCoinCost },
            { CoinPercentKey, DefaultCoinPercent },
            { SoftCapKey, DefaultSoftCap },
            { SoftCapScaleKey, DefaultSoftCapScale },
        };

        // Tooltip: the rolled value is a conversion rate, not a damage number, so it is surfaced as the
        // more readable "damage % per 1000 coins spent" (value 10..50 -> 1%..5%). The remaining args are
        // the config constants. Providers must be PURE -- this only reads config.
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.Mercenary, value =>
            {
                var config = MagicItemEffectDefinitions.GetEffectConfig(MagicEffectType.Mercenary);
                float softCap = GetConfigValue(config, SoftCapKey, DefaultSoftCap);
                float softCapScale = GetConfigValue(config, SoftCapScaleKey, DefaultSoftCapScale);
                return new object[]
                {
                    value * 0.1f,                                                  // {0} bonus % per 1000 coins spent
                    GetConfigValue(config, FlatCoinCostKey, DefaultFlatCoinCost),  // {1} flat coin cost
                    GetConfigValue(config, CoinPercentKey, DefaultCoinPercent),    // {2} % of purse per hit
                    GetConfigValue(config, BaseDamageKey, DefaultBaseDamage),      // {3} flat damage bonus
                    softCap,                                                       // {4} soft cap
                    softCap + softCapScale,                                        // {5} ceiling
                };
            });
        }

        // Prefix handler invoked by CharacterDamageDispatch (attacker-side outgoing modifier).
        public static void ModifyOutgoingHit(HitData hit, Character attacker)
        {
            if (!(attacker is Player player) || player != Player.m_localPlayer)
            {
                return;
            }

            // Socketed on the weapon, so the rate is read from the attacking weapon rather than
            // player-wide. GetActiveWeapon resolves the weapon of the attack in flight;
            // GetCurrentWeapon returned the right hand first, so an off-hand weapon's shard never fired.
            var magicItem = global::EpicLoot.src.Magic.MagicItemEffects.Helpers.MagicEffectsHelper.GetActiveWeapon(player)?.GetMagicItem();
            if (magicItem == null ||
                !magicItem.HasEffect(MagicEffectType.Mercenary, includeSocketed: true))
            {
                return;
            }

            float conversion = magicItem.GetTotalEffectValue(MagicEffectType.Mercenary, 0.01f);
            if (conversion <= 0f)
            {
                return;
            }

            var config = MagicItemEffectDefinitions.GetEffectConfig(MagicEffectType.Mercenary);
            float flatCost = GetConfigValue(config, FlatCoinCostKey, DefaultFlatCoinCost);
            float coinPercent = GetConfigValue(config, CoinPercentKey, DefaultCoinPercent);

            List<ItemDrop.ItemData> coins = CoinPurse.GetCoinStacks(player);
            int purse = CoinPurse.GetTotalCoins(coins);
            int cost = Mathf.RoundToInt(flatCost + (purse * coinPercent * 0.01f));
            if (cost <= 0 || purse < cost)
            {
                return;
            }

            float raw = GetConfigValue(config, BaseDamageKey, DefaultBaseDamage) + (cost * conversion * 0.01f);
            float bonus = ApplySoftCap(raw,
                GetConfigValue(config, SoftCapKey, DefaultSoftCap),
                GetConfigValue(config, SoftCapScaleKey, DefaultSoftCapScale));
            if (bonus <= 0f)
            {
                return;
            }

            CoinPurse.Spend(player, coins, cost);
            hit.m_damage.Modify(1f + (bonus * 0.01f));
        }

        // Diminishing returns above softCap: the excess decays exponentially toward an asymptote of
        // softCap + scale, so the bonus never reaches the ceiling but approaches it smoothly. A
        // non-positive scale degrades to a hard cap.
        private static float ApplySoftCap(float raw, float softCap, float scale)
        {
            if (scale <= 0f)
            {
                return Mathf.Min(raw, softCap);
            }
            if (raw <= softCap)
            {
                return raw;
            }
            return softCap + (scale * (1f - Mathf.Exp(-(raw - softCap) / scale)));
        }

        private static float GetConfigValue(Dictionary<string, float> config, string key, float fallback)
        {
            return config != null && config.TryGetValue(key, out var value) ? value : fallback;
        }
    }
}
