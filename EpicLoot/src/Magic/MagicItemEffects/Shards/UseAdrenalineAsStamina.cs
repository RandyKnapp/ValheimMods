using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a way to spend adrenaline to cover stamina costs
    public static class UseAdrenalineAsStamina {
        // Below this we treat the pool as empty. AddAdrenaline(-(covered / efficiency)) is not bit-exact, so
        // a fully drained pool can leave ~1e-7 behind, which would keep HaveStamina(0f) answering true for a
        // frame on a value too small to actually pay anything -- a one-frame hitch at exhaustion.
        private const float Epsilon = 0.01f;

        // Capped at 1:1 -- 100% is a perfect conversion, so stacked sources cannot make a point of adrenaline
        // worth more than a point of stamina. The caller looks the raw value up (and gates on the player
        // actually having the effect); this only normalizes it.
        private static float ClampEfficiency(float efficiency) {
            return Mathf.Clamp(efficiency, 0f, 1f);
        }

        // How much stamina the adrenaline pool could stand in for right now. PURE READ -- never spends.
        public static float GetCoverable(Player player, float efficiency) {
            efficiency = ClampEfficiency(efficiency);
            if (efficiency <= 0f) {
                return 0f;
            }

            var coverable = player.GetAdrenaline() * efficiency;
            return coverable > Epsilon ? coverable : 0f;
        }

        // Spends adrenaline to cover up to `shortfall` stamina. Returns how much was actually covered, which
        // the caller discounts from the stamina cost.
        public static float Pay(Player player, float shortfall, float efficiency) {
            efficiency = ClampEfficiency(efficiency);
            if (efficiency <= 0f) {
                return 0f;
            }

            var covered = Mathf.Min(shortfall, player.GetAdrenaline() * efficiency);
            if (covered <= Epsilon) {
                return 0f;
            }

            player.AddAdrenaline(-(covered / efficiency));
            return covered;
        }
    }
}
