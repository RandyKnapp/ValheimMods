using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Burns a chunk of health to refill an amount of stamina, cooldown based
    public static class RunningOnEmpty {
        private const float Cooldown = 30f;

        // Below this the charge isn't worth burning; firing anyway would spend the whole cooldown on nothing.
        private const float Epsilon = 0.01f;

        // Cooldown HUD indicator (Leech trophy icon with a radial recharge sweep). Built lazily on the first
        // proc -- see GetOrCreateCooldownIndicator -- so ObjectDB is loaded when the trophy is queried. Its
        // presence on the player is also the cooldown gate (checked via CooldownHash below).
        private const string CooldownName = "EL_RunningOnEmptyCooldown";
        private static readonly int CooldownHash = CooldownName.GetStableHashCode();
        private static StatusEffect _cooldownIndicator;
        private static bool _cooldownMissingLogged;

        // How much stamina the charge would bank if it fired right now; 0 while on cooldown. PURE READ --
        // never spends. Also the single source of truth for the amount, so TryRefill can't disagree with the
        // gate about what this shard is worth.
        public static float GetCoverable(Player player, float fraction) {
            if (fraction <= 0f) {
                return 0f;
            }

            // The visible cooldown status effect is the gate: while it's present the shard stays inert.
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

        // Burn the health, bank the stamina, start the cooldown. Unlike the adrenaline shard this reports
        // nothing back: it raises the stamina pool rather than discounting the cost, so vanilla's own
        // subtraction draws from the refilled pool.
        public static void TryRefill(Player player, float fraction) {
            var lump = GetCoverable(player, fraction);
            if (lump <= 0f) {
                return;
            }

            player.UseHealth(lump);
            player.AddStamina(lump);
            ShowCooldown(player);
        }

        // Adds the recharge indicator to the player; its lifetime (m_ttl = Cooldown) is the cooldown.
        // Activation is gated on the effect's absence, so it's never already present here.
        private static void ShowCooldown(Player player) {
            var indicator = GetOrCreateCooldownIndicator();
            if (indicator != null) {
                player.GetSEMan().AddStatusEffect(indicator, true);
            }
        }

        // Lazily builds the cooldown indicator prototype. Runs on a proc, so ObjectDB is loaded and the Leech
        // trophy icon is available. A null icon would render as an invisible HUD entry, so if the trophy
        // lookup fails we log once and leave _cooldownIndicator null -- GetCoverable then keeps the shard
        // inert rather than granting a cooldown-free refill.
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
            se.m_ttl = Cooldown;
            se.m_cooldownIcon = true;
            _cooldownIndicator = se;
            return _cooldownIndicator;
        }
    }
}
