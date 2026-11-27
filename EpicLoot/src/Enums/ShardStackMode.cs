namespace EpicLoot
{
    // Two shards of one color on one item always grant the same effect (it is derived from color and slot).
    public enum ShardStackMode
    {
        Blocked,
        Diminishing, // Each further shard of that color contributes a decayed fraction of its value.
        Full
    }
}
