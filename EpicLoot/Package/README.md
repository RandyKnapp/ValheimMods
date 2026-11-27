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

## Quick Configure

The **Mod Config** button at the bottom right of the main menu and the pause menu opens Epic Loot's
Quick Configure panel. Want to make changes to the mods config but don't want to learn or edit json?
The quick config is the way to do that. It lets you easily adjust balance, tune effects or change 
the mod to your liking.

## Documentation

For more information please see the [wiki on thunderstore](https://thunderstore.io/c/valheim/p/RandyKnapp/EpicLoot/wiki/).

## For mod authors

Epic Loot has a supported integration API — no assembly reference and no Harmony patches on its internals
required. It covers registering new content (magic effects, uniques, abilities, bounties, ...) and
hooking into behaviour: contributing items from your own containers or equipment slots, vetoing sacrifices,
subscribing to enchant/socket/loot events, and generating magic items.

See [docs/API.md](docs/API.md) for the reference and a migration table from the internals mods patch today.
The [`EpicLootAPI`](../EpicLootAPI/EpicLootAPI/README.md) shim gives you typed wrappers you can bundle into
your plugin with ILRepack.

### AI Policy
<!--
AI_Assisted_Creation: This assembly was partially or fully created with the assistance of generative AI (Claude Code, using Anthropic's Claude models).
AI_Model_Vendor: Anthropic
-->

We accept AI-assisted changes. All code, assets etc. Must be reviewed and tested before it will be merged or released.

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
  * LitanyOfFire - unique item definitions
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
