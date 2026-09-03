namespace EpicLoot.MagicItemEffects
{
    public static class AddCarryWeight
    {
        // ModifyMaxCarryWeight handler invoked by SharedSEManModifyMaxCarryWeightPatch. Unlike the shard
        // handlers this applies to any Player, not just the local one -- that was the guard the original
        // standalone patch used, and it is what lets a tooltip or a weight bar read against a player the
        // local client does not control.
        public static void ModifyMaxCarryWeight(Player player, ref float limit)
        {
            limit += player.GetTotalActiveMagicEffectValue(MagicEffectType.AddCarryWeight);
        }
    }
}
