# Epic Loot

Author: [RandyKnapp](https://discord.gg/ZNhYeavv3C)
Source: [Github](https://github.com/RandyKnapp/ValheimMods/tree/main/EpicLoot)
Patreon: [patreon.com/randyknapp](https://www.patreon.com/randyknapp)
Discord: [RandyKnapp's Mod Community](https://discord.gg/ZNhYeavv3C)
Patch notes: [Github Patchnotes](https://github.com/RandyKnapp/ValheimMods/blob/main/EpicLoot/CHANGELOG.md)

This mod aims to add a loot drop experience to Valheim similar to Diablo or other RPGs. Monsters and chests can now drop Magic, Rare, Epic, Legendary, Mythic, or Ancient magic items. Each magic item has a number of magic effects on it, that give bonuses to the item or your character when that magic item is equipped, and may also come with shard slots you fill yourself.

The mod is currently in ***Early Access***! That means it's **not done**! Be patient as the author adds new features, fixes bugs, and finishes things up. If you want to help, please provide feedback on the [Nexus mod page](https://www.nexusmods.com/valheim/mods/387) or on the [github](https://github.com/RandyKnapp/ValheimMods/tree/main/EpicLoot) for the following:

  * **Bugs** *(check to make sure your bug is new and not already reported)*
  * **Balance Issues** *(drops too strong? Too weak? Ruin the crafting progression?)*
  * **Missing content** *(check the [TODO list](https://github.com/RandyKnapp/ValheimMods/blob/main/EpicLoot/todo.md) to make sure the author isn't already planning to do it)*
  * **Suggestions** for new magic item effects
  * **Suggestions** for something else like UI or art improvements

***EpicLoot works in multiplayer and on dedicated servers!*** The server and all players should have the mod and its dependencies installed.

## Version 0.13!

Introducing a large new feature: **Shardstones!**

Shardstones grant a variety of small buffs to allow much greater control over your build.
  * Press Interact (E) on a socketed item in your inventory to open its shard slots, then drag in a shardstone or an Etched Runestone.
  * Enchanted items can now drop with shard slots, and the rarer the item the more slots it is likely to have.
  * Shardstones drop all over the world, each biome and boss has their own drops. Maybe you can find a unique shardstone also?
  * Shardstones can be upgraded to a higher rarity in the enchanting table's Convert Materials tab, under its own Upgrade Shardstones mode
  * A shardstone you don't want can be sacrificed for dust, reagents and essence of its own rarity

Shard drops come from the loot tables, so if you have customized your loottables.json you will need to accept the config update prompt (or refresh BepInEx/config/EpicLoot/baseconfig/loottables.json) before any shardstones will drop.

Also new in 0.13: **Tempering**, a service from Hildir that rerolls the value of an existing enchantment. Tired of augmenting and hoping? Well now you can focus entirely on upgrading specific enchants!

Other notable changes:
- Expanded tooltips that reveal the full details of an effect when you hold Shift
- Config upgrade system can automatically upgrade your configs or just let you know when an upgrade includes changes to your configs
- Quick Configure: an in-game settings panel for the options most worlds change

See the wiki on thunderstore for more information! Link below!

## Quick Configure

The **Mod Config** button at the bottom right of the main menu and the pause menu opens Epic Loot's
Quick Configure panel: a paged editor over the settings most worlds want to change (balance presets and
drop mix, features, rarity tables, loot drops, shardstones, the enchanting table, the merchant,
bounties, interface, item colours, effect tuning, advanced). Hover a row for the same description you
would read in the config file. Save writes the `.cfg` in one go; on the host, the JSON-backed rows
(rarity tables, each biome's drop amounts and rarity weights, merchant and bounty numbers, bounty
targets and rewards per biome, treasure map costs per biome, the item categories that gate drops behind
bosses, every effect's tunables) are written into `config/EpicLoot/baseconfig/` and reloaded live. A server admin who is not the host can
change the server settings from the pause menu; the JSON rows are read-only there. The first launch
opens the panel once as a setup wizard, once per user rather than once per mod manager profile: the
record is `ModQuickConfig/FirstRun.cfg` next to your Valheim saves. Set `Welcome Wizard` in the config to
`ShowNextLaunch` to run it again, or to `Never` to keep it closed in that profile.
`Show Quick Configure Button` hides the launcher entry. The config update prompt opens after the wizard,
and other mods' welcomes take turns with it rather than opening on top of it.

## Documentation

For more information please see the [wiki on thunderstore](https://thunderstore.io/c/valheim/p/RandyKnapp/EpicLoot/wiki/).

## For mod authors

Epic Loot has a supported integration API — no assembly reference and no Harmony patches on its internals
required. It covers registering new content (magic effects, legendaries, abilities, bounties, ...) and
hooking into behaviour: contributing items from your own containers or equipment slots, vetoing sacrifices,
subscribing to enchant/socket/loot events, and generating magic items.

See [docs/API.md](docs/API.md) for the reference and a migration table from the internals mods patch today.
The [`EpicLootAPI`](../EpicLootAPI/EpicLootAPI/README.md) shim gives you typed wrappers you can bundle into
your plugin with ILRepack.

## Credits

Epic Loot Team Members:
  * [Vapok](https://github.com/Vapok) - Joined in Dec 2022, made hundreds of changes and bugfixes since.
  * [OrianaVenture](https://github.com/OrianaVenture) - Joined in Dec 2023, helping with maintenance and improvements.
  * [Warp](https://github.com/jneb802) - Joined in Oct 2024, helped design new magic effects.
  * [MidnightFX](https://github.com/MidnightsFX) - Joined in Oct 2024, helping with maintenance and improvements.
  * [Rusty](https://github.com/RustyMods) - Joined in Oct 2025, helped create the API.
  * [Leslie](https://github.com/Lesliechan201) - Joined in Jan 2026, Helped create effects and improve balance.

Contibutions from the following modders were invaluable and appreciated: 
  * Blaxxun (CLLC) - bugfixes, various magic item effects
  * [M3TO](https://github.com/M3TO) - bugfixes
  * [jsza](https://github.com/jsza) - bugfixes
  * [maxrd2](https://github.com/maxrd2) - bugfixes
  * [nanonull](https://github.com/nanonull) - bugfixes, lifesteal
  * [xPucTu4](https://github.com/xPucTu4) - bugfixes
  * LitanyOfFire - legendary definitions
  * [Digitalroot](https://github.com/Digitalroot) - Help with testing

## Installation

Copy the contents of "plugins" to a new folder called "EpicLoot" in your BepInEx/plugins directory (on both clients and dedicated servers). When using a thunderstore mod manager these files should be placed in the correct directory for you.

## Cheats

Moved to a new page on the [Wiki](https://thunderstore.io/c/valheim/p/RandyKnapp/EpicLoot/wiki/2750-7cheatscommands/)!

### Spawn an etched runestone

With `devcommands` enabled, use:

```text
runestone <rarity> <effectID> <value> [amount]
runestone epic ModifyMovementSpeed 12.5
```

This drops an etched runestone near your player with exactly one effect and the specified
value. Rarity names are `magic`, `rare`, `epic`, `legendary`, `mythic`, and `ancient`.
Use Tab to complete rarity names and case-sensitive effect IDs. The optional amount defaults
to 1 and must be between 1 and 100.

Use a period for decimals. Values must be finite. Effects without a numeric strength, such
as `Indestructible`, require value `1`. Numeric values are not limited to normal roll ranges;
normal equipment restrictions still apply when socketing the runestone. Like other Epic Loot
cheats, this command requires cheat confirmation and marks the character as cheated. On a
dedicated server, admins need a mod that enables devcommands for them.

## Current Known Mod Conflicts

  * **BetterUI**: You won't be able to see the magic item properties in the tooltip. Go to the BetterUI config and set `showCustomTooltips = false`.

## Known Bugs

  * Gamepad: Still some gamepad issues, especially when using other mods that change the inventory.
