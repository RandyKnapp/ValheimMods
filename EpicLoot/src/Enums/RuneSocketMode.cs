namespace EpicLoot
{
    // Controls whether a runestone can be taken back out of a socket once it has been placed.
    public enum RuneSocketMode
    {
        Free,      // Runes can be freely inserted and removed.
        Break,     // A socketed rune must be broken to be removed.
        Permanent  // A socketed rune is permanent: it can be neither removed nor broken.
    }
}
