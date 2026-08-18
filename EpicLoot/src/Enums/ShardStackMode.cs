namespace EpicLoot
{
    // Controls whether more than one shardstone of the same color may sit on a single item. A shard's
    // effect is derived from (color, host item slot), so two shards of one color always grant the same
    // effect -- this is the rule that decides whether that is allowed, and at what strength.
    public enum ShardStackMode
    {
        Blocked,     // A color already socketed on an item cannot be socketed into it again.
        Diminishing, // Allowed; each further shard of that color contributes a decayed fraction of its value.
        Full         // Allowed at full value; no decay.
    }
}
