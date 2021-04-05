# Epic Loot v0.7.2 - Adventure Update!
Author: RandyKnapp
Source: [Github](https://github.com/RandyKnapp/ValheimMods/blob/main/EpicLoot/)

Patch notes: https://github.com/RandyKnapp/ValheimMods/blob/main/EpicLoot/patchnotes.md

This mod aims to add a loot drop experience to Valheim similar to Diablo or other RPGs. Monsters and chests can now drop Magic, Rare, Epic, or Legendary magic items. Each magic item has a number of magic effects on it, that give bonuses to the item or your character when that magic item is equipped.

The mod is currently in ***Early Access***! That means it's **not done**! Be patient as the author adds new features, fixes bugs, and finishes things up. If you want to help, please provide feedback on the [Nexus mod page](https://www.nexusmods.com/valheim/mods/387) or on the [github](https://github.com/RandyKnapp/ValheimMods/tree/main/EpicLoot) for the following:

  * **Bugs** *(check to make sure your bug is new and not already reported)*
  * **Balance Issues** *(drops too strong? Too weak? Ruin the crafting progression?)*
  * **Missing content** *(check the TODO list below to make sure the author isn't already planning to do it)*
  * **Suggestions** for new magic item effects
  * **Suggestions** for something else like UI or art improvements

***EpicLoot works in multiplayer and on dedicated servers!*** The server and all players should have the mod and its dependencies installed.

Information about every magic effect and loot drop table can be found in [info.md](https://github.com/RandyKnapp/ValheimMods/blob/main/EpicLoot/info.md).

## Installation

Copy the contents of "files" to a new folder called "EpicLoot" in your BepInEx/plugins directory.

## Cheats

Enter these into the console (F5) after using `imacheater`:

  * `magicitem <rarity> <itemtype> <amount>`: Roll a random magic item using the specified values. (alias: `mi`)
    * `<rarity>`: (String) One of: magic, rare, epic, legendary, random. If left empty, uses random.
	* `<itemtype>`: (String) The internal ID of an item. May be "random". If left empty, uses random.
	* `<amount>`: (Int) The number of magic items to roll. If the other values are set to random, rerolls that random item each time. If left empty, uses 1.
	* `<effectcount>`: (Int) The number of magic effects to roll on each item. If left empty, it rolls effect count as normal.
  * `magicmats`: Spawns a bunch of all the magic crafting materials

## Dependencies

  * **Extended Item Data Framework**: Required. Download from [Nexus](https://www.nexusmods.com/valheim/mods/281]), [Thunderstore](https://valheim.thunderstore.io/package/RandyKnapp/ExtendedItemDataFramework/)

## Current Known Mod Conflicts

  * **BetterUI** ([Nexus](https://www.nexusmods.com/valheim/mods/189), [Thunderstore](https://valheim.thunderstore.io/package/Masa/BetterUI/)): You won't be able to see the magic item properties in the tooltip. Go to the BetterUI config and set `showCustomTooltips = false`.
  * **Crafting With Containers** ([Thunderstore](https://valheim.thunderstore.io/package/abearcodes/CraftingWithContainers/)): Uses double resources when crafting, some other features break. Recommended to not use with EpicLoot.

## Known Bugs

  * Multiplayer Issue: Some players connecting to a dedicated server cannot access Enchanting sections of the crafting menu. This issue is being investigated.
  * Gamepad: Still some gamepad issues, especially when using other mods that change the inventory.

## TODO

[To-do List](https://github.com/RandyKnapp/ValheimMods/blob/main/EpicLoot/todo.md)

**Author's Note:** Older versions of this mod used an image of the Odal rune (ᛟ) to denote set items. It's reconstructed Proto-Germanic meaning is "Heritage" or "Possession" and the author felt like it was the best rune from the Elder Futhark to signify set items. However, the Odal rune with wings or feet was and is used as a Nazi symbol. The author ***UNEQUIVOCALLY CONDEMNS*** Nazis, Nazism, anti-semitism, and white supremacy. Furthermore, those who hold or practice those beliefs are not welcome to use this mod. F\*\*k Nazis.