# Loot tables

`loottables.json` decides which magic items a creature or chest drops, how many, how often and at what
rarity. The shipped copy is embedded in the mod and written to `<BepInEx>/config/EpicLoot/baseconfig/`;
change it there, through a patch in `<BepInEx>/config/EpicLoot/patches/`, or from another mod through
`API.AddLootTables` (see [API.md](API.md)).

## A table

```jsonc
{
  "Object": "Tier1Mob",
  "StarScaling": { "DropChance": 0.057, "BonusDrops": 0, "RarityShift": 0.08, "RarityFalloff": 0.5, "MaxRarity": "Legendary" },
  "LeveledLoot": [
    {
      "Level": 1,
      "Drops": [ [0, 95], [1, 5] ],
      "Loot": [
        { "Item": "Tier0Everything", "Weight": 10, "WeightPerStar": -1.063, "Rarity": [ 75, 25, 0, 0, 0 ] },
        { "Item": "Tier1Everything", "Weight": 1,  "WeightPerStar": 1.063,  "Rarity": [ 75, 25, 0, 0, 0 ] }
      ]
    },
    { "Level": 5, "Drops": [ [0, 65], [1, 34], [2, 1] ], "Loot": [ ... ] }
  ]
}
```

| Field | Meaning |
|---|---|
| `Object` | The creature or container prefab name, or a template name other tables refer to. Several tables may share an Object; each rolls on its own (the bosses have an item table and a shardstone table). |
| `RefObject` | Makes this entry use another table's loot (see [Creature entries](#creature-entries)). |
| `LeveledLoot` | The anchor levels (below). |
| `StarScaling` | How the loot grows per star past an anchor (below). |
| `Modifiers` | Star-independent adjustments (below). |
| `StarMultiplier` | How much each star counts for this table; 1.5 makes a 2★ creature roll like a 3★ one. |
| `Auto` | Set by the creature sorter on the entries it writes. Leave it alone, or remove it to keep an entry as it is. |

### Levels are anchors

A creature's level is its stars plus one: 0★ is level 1, 2★ is level 3. A roll uses the highest level at
or below the creature's that has data, and StarScaling extrapolates the rest. `Drops` and `Loot` are
anchored separately, so a level may set only one of them and take the other from below. A table needs a
level 1 entry (or whichever level is its lowest) for creatures at that level to drop anything.

- **`Drops`** is `[count, weight]` pairs: the chance of each number of drops. `[0, 95], [1, 5]` drops one
  item 5% of the time.
- **`Loot`** is the list each drop is picked from, by `Weight` (default 1), with replacement.

### Loot entries

| Field | Meaning |
|---|---|
| `Item` | A prefab name, an `ItemSets` name, or a reference to another table (below). |
| `Weight` | Relative chance of this entry being picked. |
| `WeightPerStar` | Added to `Weight` for every star past the anchor, never going below 0. This is how a template shifts toward the next tier's gear without another level. |
| `Rarity` | Relative weights for Magic, Rare, Epic, Legendary, Mythic, Ancient, in that order. A shorter array leaves the rest at 0. An entry without one drops a plain item, unless the set or table it names supplies one. |
| `RarityItems` | A per-rarity override of `Item`: the rolled rarity picks a prefab, a set or a table reference (used by shardstones and the sets below). |

**Table references.** `"Tier3Mob.2"` rolls from Tier3Mob's loot as a level 2 creature would. `"Tier3Mob.*"`
rolls it at the level the referring table is being rolled at, which is how the elite templates take their
gear from the normal ones at every level with a single entry. A referenced table uses its own StarScaling
for its item mix and rarity.

**Rarity-keyed sets.** A set whose entry carries a `RarityItems` map drops by rarity. `EnchantingMats`
maps each rarity to that tier's materials:

```json
{
  "Name": "EnchantingMats",
  "Loot": [
    { "Item": "Tier0Mats", "Rarity": [ 1, 1, 1, 1, 1, 1 ],
      "RarityItems": { "Magic": "Tier0Mats", "Rare": "Tier1Mats", "Epic": "Tier2Mats", "Legendary": "Tier3Mats", "Mythic": "Tier4Mats", "Ancient": "Tier5Mats" } }
  ]
}
```

so a table entry

```json
{ "Item": "EnchantingMats", "Weight": 1, "Rarity": [ 50, 41, 7, 2, 0, 0 ] }
```

drops Magic materials half the time, Rare 41% and so on, in place of one weighted entry per tier.
`EnchantingRunestones` does the same for blank runestones.

- Stars promote the entry's `Rarity` and luck weighs it before it picks, so starred creatures drop
  higher tiers.
- A rarity the map lacks uses the nearest one it has, so map every rarity the entry can reach.
- An entry with no `Rarity` of its own uses the set's. Both shipped sets then pick every tier evenly.
- Shardstones, runestones and enchanting materials a table names always drop as themselves; the drop
  ratios never turn them into something else.
- Quick Configure's Loot Drops page leaves these entries' `Rarity` alone when you edit a table's rarity.

## Star scaling

A creature rolls at an **effective level**: `1 + stars × StarMultiplier(creature) × StarMultiplier(table) ×
Star Loot Scaling`. The last is the server option in the Balance section (default 1; 0 means stars never
affect loot). The distance past each anchor is then scaled:

| `StarScaling` field | Per star past the anchor |
|---|---|
| `DropChance` | Added to the chance of dropping anything (0.05 = five percentage points), keeping the shape of the counts. An anchor that never drops starts from one item. |
| `BonusDrops` | Extra items on a roll that drops something. The whole part is guaranteed; the fraction is the chance of one more. |
| `RarityShift` | The share of each rarity's weight promoted one tier, compounding per star. |
| `RarityFalloff` | Scales `RarityShift` for each tier above the lowest one the entry rolls (default 1). A low falloff drains the bottom tier quickly while the top grows slowly. |
| `MaxRarity` | Promotion never moves weight into a higher tier than this. |
| `MaxDrops` | A cap on the item count after `BonusDrops`. |

Each field is taken from the first place that sets it: the creature's own entry, then the table, then
`DefaultStarScaling` at the top of `loottables.json` (for other mods' tables and anything that sets
nothing). Write `0` to switch a field off rather than leaving it out; an omitted field falls through to the
default. The boss shardstone tables set all three to 0 so they drop exactly as written.

Promotion happens once per drop, before a `RarityItems` map picks its prefab, by whichever table supplied
the rarity. Luck is applied afterwards, as before.

### Modifiers

`Modifiers` apply at every level, on a table or on a creature's entry:

| Field | Effect |
|---|---|
| `DropRate` | Multiplies the odds of dropping anything, the same way `Global Drop Rate Modifier` does. Creature and table values multiply. |
| `BonusDrops` | Extra items on every roll that drops something. Creature and table values add. |
| `RarityShift` | One extra promotion step. Creature and table values add. |

## Creature entries

A creature usually points at a template:

```json
{ "Object": "Greydwarf", "RefObject": "Tier1Mob" }
```

It can tune its own loot without copying the template:

```json
{ "Object": "GoblinBrute", "RefObject": "Tier6EliteMob", "StarMultiplier": 1.5, "Modifiers": { "DropRate": 1.25 } }
```

An entry may also carry its own `StarScaling`, which wins field by field over the template's.

**Elite runestones.** The server option `Elite Runestone Drops` (Balance section, also on Quick Configure's
Loot Drops page) adds one more table to each elite template, `Tier3EliteMob` to `Tier9EliteMob`, while the
game runs: one blank runestone of the biome's rarity (Magic in the Black Forest up to Mythic or Ancient in
the Deep North) at `Elite Runestone Drop Chance`, the same at every star level. It is never written to
`loottables.json`; `lootpreview` shows it, and a creature's own `Modifiers` apply to it as to its other
tables.

## A full example

Everything above in one file: a pair of templates for the Mountain's wolves, a rarity-keyed bow set, and
three creatures tuned on top of the templates. In a real `loottables.json` the root field and the two
arrays merge into the ones already there, and the three creature entries replace the shipped `Wolf`,
`Fenring_Cultist` and `Fenring` lines.

```json
{
  "DefaultStarScaling": { "DropChance": 0.149, "BonusDrops": 0.016, "RarityShift": 0.28, "RarityFalloff": 1 },
  "ItemSets": [
    {
      "Name": "HunterBows",
      "Loot": [
        { "Item": "BowHuntsman", "Rarity": [ 60, 30, 10, 0, 0, 0 ],
          "RarityItems": { "Magic": "BowHuntsman", "Rare": "BowHuntsman", "Epic": "BowDraugrFang", "Legendary": "BowSpineSnap" } }
      ]
    }
  ],
  "LootTables": [
    {
      "Object": "MountainHunterMob",
      "StarScaling": { "DropChance": 0.15, "BonusDrops": 0.05, "RarityShift": 0.3, "RarityFalloff": 0.5, "MaxRarity": "Legendary", "MaxDrops": 2 },
      "LeveledLoot": [
        {
          "Level": 1,
          "Drops": [ [0, 80], [1, 20] ],
          "Loot": [
            { "Item": "Tier4Everything", "Weight": 6, "WeightPerStar": -1.5, "Rarity": [ 60, 35, 5, 0, 0, 0 ] },
            { "Item": "Tier5Everything", "Weight": 0, "WeightPerStar": 1.5,  "Rarity": [ 60, 35, 5, 0, 0, 0 ] },
            { "Item": "HunterBows",      "Weight": 1 },
            { "Item": "EnchantingMats",  "Weight": 2, "Rarity": [ 50, 41, 7, 2, 0, 0 ] }
          ]
        },
        {
          "Level": 3,
          "Drops": [ [0, 50], [1, 45], [2, 5] ]
        }
      ]
    },
    {
      "Object": "MountainHunterElite",
      "StarMultiplier": 1.5,
      "StarScaling": { "DropChance": 0.12, "BonusDrops": 0.05, "RarityShift": 0, "MaxRarity": "Epic" },
      "Modifiers": { "RarityShift": 0.1 },
      "LeveledLoot": [
        {
          "Level": 1,
          "Drops": [ [0, 70], [1, 28], [2, 2] ],
          "Loot": [
            { "Item": "MountainHunterMob.*", "Weight": 10 },
            { "Item": "Tier5Mob.3", "Weight": 1 },
            { "Item": "White_Magic_ShardStone", "Weight": 2, "Rarity": [ 70, 25, 5 ],
              "RarityItems": { "Magic": "White_Magic_ShardStone", "Rare": "White_Rare_ShardStone", "Epic": "White_Epic_ShardStone" } }
          ]
        },
        {
          "Level": 4,
          "Loot": [
            { "Item": "MountainHunterMob.*", "Weight": 8 },
            { "Item": "Tier5Mob.3", "Weight": 2 },
            { "Item": "White_Magic_ShardStone", "Weight": 3, "Rarity": [ 30, 50, 20 ],
              "RarityItems": { "Magic": "White_Magic_ShardStone", "Rare": "White_Rare_ShardStone", "Epic": "White_Epic_ShardStone" } }
          ]
        }
      ]
    },
    {
      "Object": "MountainHunterElite",
      "StarScaling": { "DropChance": 0, "BonusDrops": 0, "RarityShift": 0 },
      "LeveledLoot": [
        {
          "Level": 1,
          "Drops": [ [0, 90], [1, 10] ],
          "Loot": [
            { "Item": "EnchantingRunestones", "Rarity": [ 0, 70, 30, 0, 0, 0 ] }
          ]
        }
      ]
    },
    { "Object": "Wolf", "RefObject": "MountainHunterMob" },
    { "Object": "Fenring_Cultist", "RefObject": "MountainHunterMob", "StarMultiplier": 1.2,
      "StarScaling": { "RarityShift": 0.45, "MaxRarity": "Mythic" } },
    { "Object": "Fenring", "RefObject": "MountainHunterElite",
      "Modifiers": { "DropRate": 1.25, "BonusDrops": 0.1, "RarityShift": 0.05 } }
  ]
}
```

**The root.** `DefaultStarScaling` fills any field a table leaves out. Here it gives the two elite tables
their `RarityFalloff`.

**`HunterBows`** is a rarity-keyed set: the rarity a drop rolls picks the bow. Magic and Rare drop a
Huntsman bow, Epic a Draugr Fang and Legendary a Spinesnap, so the better bows only ever come at those
rarities. The table entry that names the set has no `Rarity`, so it uses the set's, promoted by stars
like any other.

**`MountainHunterMob`**, the normal template:

- Level 1 sets `Drops` and `Loot`. Level 3 sets only `Drops`, so from 2★ the counts come from level 3
  (where two items are possible) while the item list is still level 1's, two stars past its anchor.
- `WeightPerStar` moves weight from Mountain gear (`Tier4Everything`) to Plains gear
  (`Tier5Everything`): none of the gear is Plains at 0★, a quarter at 1★, half at 2★ and all of it
  from 4★.
- `StarScaling` sets all six fields. Each star past an anchor adds 15 points of drop chance and a 5%
  chance of one more item, and promotes 30% of the Magic weight to Rare, half that rate from Rare to
  Epic and half again from Epic to Legendary, never past Legendary. No roll gives more than two items.
- `EnchantingMats` is the shipped rarity-keyed set with the entry's own `Rarity`, so most materials
  are Magic or Rare and stars push some up a tier.

**`MountainHunterElite`**, the elite template, is two tables under one name, and both roll for every
creature on it.

- `StarMultiplier: 1.5` makes each star count one and a half times on this table, so a 2★ elite rolls
  at level 4 and gets the level 4 list.
- `"MountainHunterMob.*"` takes the normal template's gear at the elite's own effective level, under the
  normal template's scaling: a 2★ elite gets the item mix and rarity of a level 4 normal mob.
- `"Tier5Mob.3"` always rolls the shipped `Tier5Mob` list as a 2★ creature would, whatever the
  elite's stars.
- The shardstone entry has its own `RarityItems` map. `RarityShift: 0` keeps stars from promoting it,
  so the level 4 list raises its rarity in one step instead. `MaxRarity: "Epic"` keeps the `Modifiers`
  step from moving it past the stones the map names.
- `Modifiers.RarityShift: 0.1` is one more promotion at every level. It carries into the references, so
  an elite's wolf gear is a little better than a wolf's.
- Level 4 sets only `Loot`. A Loot anchor replaces the whole list, so it repeats every entry; `Drops`
  keeps extrapolating from level 1.
- The second table drops a Rare or Epic blank runestone 10% of the time at every star, because its
  `StarScaling` is all zeros. It comes second because a `MountainHunterElite.N` reference reads only the
  first table of a name.

**The creatures:**

- `Wolf` uses the normal template as it is.
- `Fenring_Cultist` uses it too, but its stars count 1.2 times. Its own `StarScaling` promotes rarity
  faster and lets it reach Mythic, and the template's other fields still apply.
- `Fenring` drops more often (`DropRate`), has a 10% chance of one more item on every roll that drops
  something (`BonusDrops`), and gets a small extra promotion on top of the template's (`RarityShift`).
  These apply to both elite tables, so its runestone table drops 15% of the time instead of 10%, now
  and then as a Legendary runestone.

A creature's `StarScaling` reaches only the entries of its own template. Gear that comes through a
reference uses that table's scaling: the creature's stars reach it only through the level `*` passes on,
and of its `Modifiers` only `RarityShift` carries over.

**What it rolls**, with Star Loot Scaling at 1 and no luck. Any drop and two or more items are per kill;
the gear columns are shares of the gear that drops.

| Creature, 0★ / 1★ / 2★ | Effective level | Any drop | Two or more items | Plains gear | Epic or better gear |
|---|---|---|---|---|---|
| Wolf | 1 / 2 / 3 | 20 / 35 / 50% | 0 / 2 / 5% | 0 / 25 / 50% | 5 / 10 / 17% |
| Fenring_Cultist | 1 / 2.2 / 3.4 | 20 / 38 / 56% | 0 / 2 / 7% | 0 / 30 / 60% | 5 / 15 / 30% |
| Fenring (gear table) | 1 / 2.5 / 4 | 40 / 59 / 75% | 6 / 14 / 23% | 3 / 36 / 61% | 19 / 27 / 46% |

Fenring's shardstones are Rare or Epic 41% of the time at 0★ and 1★, and 75% from 2★, when the level 4
list takes over.

To have the creature sorter put another mod's creatures on these templates, name them under
`CreatureSorter.Overrides` in `itemsorter.json` (`"SomeModWolf": { "Template": "MountainHunterMob" }`).
`lootpreview Fenring` shows each table's anchors, drop chance and rarity split per star level in game.

## Checking a table

The console command `lootpreview <creature or table> [maxLevel]` prints, for each level, the anchors used,
the chance of any drop, items per drop, the rarity split and the item mix, with the creature's own tuning
and the Star Loot Scaling option applied.

## The flat form (deprecated)

Older tables put `Drops` and `Loot` directly on the table. That form only ever answered levels 1–3. It is
converted to a level 1 entry wherever it appears:

- **The on-disk file.** A `loottables.json` holding flat tables is converted once at startup, after a
  backup to `baseconfig-backup/`.
- **Patches.** A patch that writes a flat table is converted as soon as it runs, and the log names the
  patch. A patch whose path targets a table's `.Loot` or `.Drops` is applied to
  `.LeveledLoot[?(@.Level == 1)].Loot` (or `.Drops`) instead, again with a warning.
- **The API** and a file edited while the game runs are converted in memory.

A patch that picks one level (`LeveledLoot[?(@.Level == 4)]`) of a table that no longer has that level
logs which levels the table does have.

## The creature sorter

Every creature without a loot table is given one when a world loads, on the server or in single player.
The rules live in `itemsorter.json`:

- **`BiomeSorterData.<biome>.Creatures`** is the biome's ladder: a `ReferenceHealth` and a template for
  each class (`Weak`, `Normal`, `Strong`, `Elite`, `Boss`; a missing rung falls back to Normal).
- **`CreatureSorter`** holds the rest: `HealthRatios` (Weak below 0.55× the reference, Strong from 1.5×,
  Elite from 2.5×), the exclusions (`SkipPassive`, `ExcludeFactions`, `ExcludeNames` with `*` wildcards),
  `VariantSuffixes`, `FactionBiomes`, `ScanLocations`, `ClassExtras` (extra `Modifiers` or
  `StarMultiplier` per class; bosses get a large drop rate) and `Overrides`.

A creature's biome comes from the strongest evidence found:

1. its `Overrides` entry
2. world spawn lists
3. alternate-biome spawns
4. raids that end with a boss kill
5. being a variant of a placed creature (`Draugr_sleeping` goes where `Draugr` does)
6. key-gated spawns and raids
7. the locations that spawn it (loaded only if something is still unplaced, and only once: see below)
8. its drops, matched against `BiomeMaterials`
9. its faction

Its class comes from its base health against the biome's reference, or `Boss` for a boss.

The sorter writes `{ "Object": ..., "RefObject": ..., "Auto": true }` entries into `loottables.json` and
re-sorts or removes them on every world load. Entries without `"Auto"` are never touched, and neither are
tables other mods add through the API.

To place a creature yourself, add it under `CreatureSorter.Overrides`:

```json
"Overrides": {
  "SomeModWolf": { "Biome": "Mountain", "Class": "Elite" },
  "SomeModPet":  { "Skip": true },
  "SomeModBoss": { "Template": "Tier7EliteMob" }
}
```

Each run writes `creaturesorter-report.txt` to `<BepInEx>/config/EpicLoot/`, one line per creature with
its biome, the evidence, its health ratio, class, template and result. In game, `creaturesort dry` shows the
same without writing anything, and `creaturesort all` includes creatures that already have a table. The
server option `Auto Add Creatures To Loot Tables` turns the sorter off.

What each location spawns is remembered in `creaturesorter-locations.json` in the same folder, so the
location prefabs are loaded once rather than on every world load. The file starts over by itself when the
game or any mod updates. If a mod changes its locations without a version change, delete the file.
