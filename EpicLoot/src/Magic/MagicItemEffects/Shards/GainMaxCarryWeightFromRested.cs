namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to max carry weight based on the player's comfort level when rested.
    public static class GainMaxCarryWeightFromRested {
        // ModifyMaxCarryWeight handler invoked by SharedSEManModifyMaxCarryWeightPatch.
        public static void ModifyMaxCarryWeight(Player player, SEMan seman, ref float limit) {
            var perComfort = player.GetTotalActiveMagicEffectValue(
                MagicEffectType.GainMaxCarryWeightFromRested);
            if (perComfort == 0f) {
                return;
            }

            // HaveStatusEffect walks the status effect list, so it stays behind the memoized lookup.
            if (!seman.HaveStatusEffect(SEMan.s_statusEffectRested)) {
                return;
            }

            var comfortLevel = player.GetComfortLevel();
            if (comfortLevel <= 0) {
                return;
            }

            limit += perComfort * comfortLevel;
        }
    }
}
