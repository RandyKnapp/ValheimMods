namespace EpicLoot
{
    // Controls whether a shardstone can be taken back out of a socket once it has been placed.
    public enum ShardSocketMode
    {
        Free,           // Any shard can be freely inserted and removed.
        BreakValueless, // A shard granting an effect with no rarity-scaled value (e.g. Warmth) must be
                        // broken to be removed. Shards granting a scaled value -- and shards that grant
                        // nothing at all on this item -- stay freely removable.
        BreakAll,       // Every shard must be broken to be removed.
        Permanent       // Every shard is permanent: it can be neither removed nor broken.
    }
}
