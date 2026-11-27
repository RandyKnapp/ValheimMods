namespace EpicLoot
{
    public enum ShardSocketMode
    {
        Free,
        BreakValueless, // A shard granting an effect with no rarity-scaled value (e.g. Warmth) must be
                        // broken to be removed. Shards granting a scaled value -- and shards that grant
                        // nothing at all on this item -- stay freely removable.
        BreakAll,
        Permanent
    }
}
