namespace EpicLoot.MagicItemEffects
{
    public static class AddCarryWeight
    {
        // Unlike the shard handlers this applies to any Player, not just the local one, so a tooltip or a weight
        // bar can read against a player the local client does not control.
        public static void ModifyMaxCarryWeight(Player player, ref float limit)
        {
            limit += player.GetTotalActiveMagicEffectValue(MagicEffectType.AddCarryWeight);
        }
    }
}
