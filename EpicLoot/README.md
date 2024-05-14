# Epic Loot

Author: [RandyKnapp](https://discord.gg/ZNhYeavv3C)
Source: [Github](https://github.com/RandyKnapp/ValheimMods/blob/main/EpicLoot/)
Patreon: [patreon.com/randyknapp](https://www.patreon.com/randyknapp)
Discord: [RandyKnapp's Mod Community](https://discord.gg/ZNhYeavv3C)
Patch notes: [Github Patchnotes](https://github.com/RandyKnapp/ValheimMods/blob/main/EpicLoot/CHANGELOG.md)

This mod aims to add a loot drop experience to Valheim similar to Diablo or other RPGs. Monsters and chests can now drop Magic, Rare, Epic, Legendary, or Mythic magic items. Each magic item has a number of magic effects on it, that give bonuses to the item or your character when that magic item is equipped.

The mod is currently in ***Early Access***! That means it's **not done**! Be patient as the author adds new features, fixes bugs, and finishes things up. If you want to help, please provide feedback on the [Nexus mod page](https://www.nexusmods.com/valheim/mods/387) or on the [github](https://github.com/RandyKnapp/ValheimMods/tree/main/EpicLoot) for the following:

  * **Bugs** *(check to make sure your bug is new and not already reported)*
  * **Balance Issues** *(drops too strong? Too weak? Ruin the crafting progression?)*
  * **Missing content** *(check the TODO list below to make sure the author isn't already planning to do it)*
  * **Suggestions** for new magic item effects
  * **Suggestions** for something else like UI or art improvements

***EpicLoot works in multiplayer and on dedicated servers!*** The server and all players should have the mod and its dependencies installed.

## Ashlands Update Notice!

The Ashlands update introduced the addition of the Mythic tier rarity, as well as a huge rebalance of the mod to be more vanilla friendly. If you are upgrading from a previous version you may wish to restore the old values. There is now an add-on with patches to restore the old Epic values: [thunderstore link](https://thunderstore.io/c/valheim/p/RandyKnapp/EpicPatches_EpicLoot/).

Mythic sets have not been created for the base mod, but the feature is available if you wish to create your own sets and add them with the patching system.

## Documentation

**Magic Effects:** [Magic Effects Wiki](https://github.com/RandyKnapp/ValheimMods/wiki/Magic-Effects)
**Loot Drops:** [info.md](https://github.com/RandyKnapp/ValheimMods/blob/main/EpicLoot/info.md)
**Patching System:** [Config Patching](https://github.com/RandyKnapp/ValheimMods/wiki/Config-Patching)

## Credits

Epic Loot Team Members:
  * [Vapok](https://github.com/Vapok) - Joined in Dec 2022, made hundreds of changes and bugfixes since.
  * [OrianaVenture](https://github.com/OrianaVenture) - Joined in Dec 2023, helping with maintenance and improvements.

Contibutions from the following modders were invaluable and appreciated: 
  * [blaxxun (CLLC)](https://www.nexusmods.com/valheim/mods/495) - bugfixes, config sync, various magic item effects
  * [M3TO](https://github.com/M3TO) - bugfixes
  * [jsza](https://github.com/jsza) - bugfixes
  * [maxrd2](https://github.com/maxrd2) - bugfixes
  * [nanonull](https://github.com/nanonull) - bugfixes, lifesteal
  * [xPucTu4](https://github.com/xPucTu4) - bugfixes
  * LitanyOfFire - legendary definitions
  * [Digitalroot](https://github.com/Digitalroot) - Help with testing

## Installation

Copy the contents of "plugins" to a new folder called "EpicLoot" in your BepInEx/plugins directory (on both clients and dedicated servers). Currently all main mod json files must be contained in the same folder as the EpicLoot.dll file. When using a thunderstore mod manager these files should be placed in the correct directory for you.

## Cheats

Enter these into the console (F5) after using `devcommands`:

  * `magicitem <rarity> <itemtype> <amount>`: Roll a random magic item using the specified values. (alias: `mi`)
    * `<rarity>`: (String) One of: magic, rare, epic, legendary, random. If left empty, uses random.
  * `<itemtype>`: (String) The internal ID of an item. May be "random". If left empty, uses random.
  * `<amount>`: (Int) The number of magic items to roll. If the other values are set to random, rerolls that random item each time. If left empty, uses 1.
  * `<effectcount>`: (Int) The number of magic effects to roll on each item. If left empty, it rolls effect count as normal.
  * `magicmats`: Spawns a bunch of all the magic crafting materials
  * `magicitemwitheffect <effectID> <itemtype>`: Spawn a magic item with the specified magic effect. (alias: `mieffect`)
    * `<effectID>`: (String) The type of magic effect you want to guarantee
  * `<itemtype>`: (String) The internal ID of an item.
  * `magicitemlegendary <legendaryID> <itemtype>`: Spawn a specific legendary item. (alias: `milegend`)
  * `<legendaryID>`: (String) The ID of the legendary you want to spawn
  * `<itemtype>`: (String) The internal ID of an item (optional, will use an item that meets the legendary definition's requirements)
  * `magicitemset <setID>`: Spawn all the items in a legendary item set. (alias: `miset`)
  * `<setID>`: (String) The ID of the legendary set you want to spawn

## Current Known Mod Conflicts

  * **BetterUI**: You won't be able to see the magic item properties in the tooltip. Go to the BetterUI config and set `showCustomTooltips = false`.

## Known Bugs

  * Gamepad: Still some gamepad issues, especially when using other mods that change the inventory.