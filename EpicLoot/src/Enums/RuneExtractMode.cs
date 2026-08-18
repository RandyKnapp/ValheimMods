namespace EpicLoot
{
    // Controls what happens to the source item when a rune is extracted from it.
    public enum RuneExtractMode
    {
        KeepItem,                // The item is returned untouched.
        ReduceEnchants,          // The extracted enchantment is removed from the item (item kept).
        ReduceEnchantsAndRarity, // Extracted enchantment removed, rarity dropped one tier, values clamped.
        DestroyItem              // The item is consumed.
    }
}
