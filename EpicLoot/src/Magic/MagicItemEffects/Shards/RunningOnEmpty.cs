using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class RunningOnEmpty {
        public const float DefaultCooldown = 30f;

        private const string CooldownKey = "Cooldown";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { CooldownKey, DefaultCooldown },
        };

        // Floored just above zero: a cooldown of 0 would give the indicator a zero ttl, which vanilla
        // treats as "no timeout", leaving the shard permanently gated instead of permanently ready.
        private static float GetCooldown() {
            return Mathf.Max(0.1f,
                EffectConfig.Get(MagicEffectType.RunningOnEmpty, CooldownKey, DefaultCooldown));
        }

        // Below this the charge isn't worth burning; firing anyway would spend the whole cooldown on nothing.
        private const float Epsilon = 0.01f;

        // The cooldown indicator's presence on the player is also the cooldown gate.
        private const string CooldownName = "EL_RunningOnEmptyCooldown";
        private static readonly int CooldownHash = CooldownName.GetStableHashCode();
        private static StatusEffect _cooldownIndicator;
        private static bool _cooldownMissingLogged;

        // How much stamina the charge would bank if it fired right now; 0 while on cooldown. PURE READ --
        // never spends. TryRefill uses it too, so the gate and the payment agree.
        public static float GetCoverable(Player player, float fraction) {
            if (fraction <= 0f) {
                return 0f;
            }

            if (player.GetSEMan().HaveStatusEffect(CooldownHash)) {
                return 0f;
            }

            // Without an indicator there is nothing to hold the cooldown, so the charge would refill for free
            // every frame. Stay unavailable instead.
            if (GetOrCreateCooldownIndicator() == null) {
                return 0f;
            }

            var lump = Mathf.Min(player.GetMaxHealth() * fraction, player.GetHealth() - 1f);
            return lump > Epsilon ? lump : 0f;
        }

        // Unlike the adrenaline shard this reports nothing back: it raises the stamina pool rather than
        // discounting the cost, so vanilla's own subtraction draws from the refilled pool.
        public static void TryRefill(Player player, float fraction) {
            var lump = GetCoverable(player, fraction);
            if (lump <= 0f) {
                return;
            }

            player.UseHealth(lump);
            player.AddStamina(lump);
            ShowCooldown(player);
        }

        // The indicator's m_ttl is the cooldown, stamped here because the cached prototype would not follow a
        // retune.
        private static void ShowCooldown(Player player) {
            var indicator = GetOrCreateCooldownIndicator();
            if (indicator != null) {
                indicator.m_ttl = GetCooldown();
                player.GetSEMan().AddStatusEffect(indicator, true);
            }
        }

        // A null icon would render as an invisible HUD entry, so a failed trophy lookup leaves the indicator
        // null, and GetCoverable then keeps the shard inert rather than granting a cooldown-free refill.
        private static StatusEffect GetOrCreateCooldownIndicator() {
            if (_cooldownIndicator != null) {
                return _cooldownIndicator;
            }

            var icon = ObjectDB.instance?.GetItemPrefab("TrophyLeech")?
                .GetComponent<ItemDrop>()?.m_itemData.GetIcon();
            if (icon == null) {
                if (!_cooldownMissingLogged) {
                    EpicLoot.LogWarning("RunningOnEmpty: could not find 'TrophyLeech' icon; cooldown indicator will not display.");
                    _cooldownMissingLogged = true;
                }
                return null;
            }

            var se = ScriptableObject.CreateInstance<StatusEffect>();
            se.name = CooldownName;
            se.m_name = "$mod_epicloot_se_runningonempty";
            se.m_icon = icon;
            se.m_ttl = GetCooldown(); // restamped on every proc by ShowCooldown
            se.m_cooldownIcon = true;
            _cooldownIndicator = se;
            return _cooldownIndicator;
        }
    }
}
