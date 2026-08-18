using System.Collections.Generic;
using EpicLoot.General;
using Jotunn.Managers;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Coinplated: spend a portion of your coin purse to absorb incoming damage.
    //
    //   pool     = purse * PoolPercent%              coins committed this hit
    //   absorbed = min(incomingDamage, pool * efficiency)
    //   spent    = ceil(absorbed / efficiency)       <= pool, so a chip hit costs a few coins
    //
    // Higher efficiency therefore means fewer coins per point of damage blocked (Magic pays 2 coins per
    // damage, Mythic ~1.11)
    public static class Coinplated {
        // Config default, registered in ShardEffectDefinitions.EffectConfigs.
        public const float DefaultPoolPercent = 10f;
        private const string PoolPercentKey = "PoolPercent";
        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { PoolPercentKey, DefaultPoolPercent },
        };

        private static GameObject effect = null;

        // Tooltip: the rolled value is a whole-number percent, but the readable figure is damage blocked
        // per coin spent (50..90 -> 0.5..0.9). Providers must be PURE -- this only reads config.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.Coinplated, value => new object[]
            {
                value * 0.01f,      // {0} damage blocked per coin spent
                GetPoolPercent(),   // {1} % of the purse committed per hit
            });
        }

        // Prefix handler invoked by CharacterRpcDamageDispatch (victim-side incoming modifier; runs after
        // avoidance so a fully-avoided hit never spends coins).
        public static void ModifyIncoming(Character __instance, HitData hit) {
            if (hit == null || __instance != Player.m_localPlayer) {
                return;
            }

            var player = Player.m_localPlayer;

            // Socketed on chest armour, so this is a player-wide effect.
            float efficiency = player.GetTotalActiveMagicEffectValue(MagicEffectType.Coinplated, 0.01f);
            if (efficiency <= 0f) {
                return;
            }

            float total = hit.m_damage.EpicLootGetTotalDamageAgainstPlayer();
            if (total <= 0f) {
                return;
            }

            List<ItemDrop.ItemData> coins = CoinPurse.GetCoinStacks(player);
            int purse = CoinPurse.GetTotalCoins(coins);
            if (purse <= 0) {
                return;
            }

            float pool = purse * GetPoolPercent() * 0.01f;
            float absorb = Mathf.Min(total, pool * efficiency);
            if (absorb <= 0f) {
                return;
            }

            // absorb <= pool * efficiency, so this can never exceed the committed pool; clamped to the
            // purse anyway so rounding can't overdraw.
            int spend = Mathf.Min(purse, Mathf.CeilToInt(absorb / efficiency));
            if (spend <= 0) {
                return;
            }

            CoinPurse.Spend(player, coins, spend);
            hit.m_damage.Modify(1f - (absorb / total));

            if (DamageText.instance != null) {
                DamageText.instance.ShowText(DamageText.TextType.Blocked, __instance.transform.position,
                    Mathf.RoundToInt(absorb), true);
            }
            if (effect == null) {
                effect = PrefabManager.Instance.GetPrefab("fx_GoblinShieldHit");
            }
            if (effect != null) {
                GameObject.Instantiate(effect, __instance.transform.position, Quaternion.identity);
            }
        }

        private static float GetPoolPercent() {
            var config = MagicItemEffectDefinitions.GetEffectConfig(MagicEffectType.Coinplated);
            if (config != null && config.TryGetValue(PoolPercentKey, out var value)) {
                return Mathf.Max(0f, value);
            }
            return DefaultPoolPercent;
        }
    }
}
