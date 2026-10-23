namespace EpicLoot
{
    // Controls what happens to the source item when its set is extracted into a set rune.
    public enum RuneSetExtractMode
    {
        StripSet,   // The item keeps its rarity, enchantments and sockets but is no longer a set piece.
        DestroyItem // The item is consumed.
    }
}
