namespace EpicLoot.MagicItemEffects.Shards {
    public static class HeartyEitr {
        public static void Apply(Player player, float maxHealth, ref float eitr) {
            if (eitr <= 0f) {
                return;
            }

            eitr += maxHealth * player.GetTotalActiveMagicEffectValue(MagicEffectType.HeartyEitr, 0.01f);
        }
    }
}
