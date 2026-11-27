
namespace EpicLoot.MagicItemEffects.Shards {
    public static class DayArmor {
        // Called from ModifyArmor's GetArmor postfix, which resolves the wearer once for every armor effect.
        public static void ModifyArmor(Player player, ref float armor) {
            if (!EnvMan.IsDay()) {
                return;
            }

            var bonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.DayArmor, 0.01f);
            if (bonus != 0f) {
                armor *= 1f + bonus;
            }
        }
    }
}
