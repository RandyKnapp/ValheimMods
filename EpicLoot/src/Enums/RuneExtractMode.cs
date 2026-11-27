namespace EpicLoot
{
    public enum RuneExtractMode
    {
        KeepItem,
        ReduceEnchants,
        ReduceEnchantsAndRarity, // Rarity also drops one tier, values clamped to it.
        DestroyItem
    }
}
