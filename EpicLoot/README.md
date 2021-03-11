# Epic Loot v0.5.3
Author: RandyKnapp
Source: [Github](https://github.com/RandyKnapp/ValheimMods/blob/main/EpicLoot/)

This mod aims to add a loot drop experience to Valheim similar to Diablo or other RPGs. Monsters and chests can now drop Magic, Rare, Epic, or Legendary magic items. Each magic item has a number of magic effects on it, that give bonuses to the item or your character when that magic item is equipped.

The mod is currently in ***Early Access***! That means it's **not done**! Be patient as the author adds new features, fixes bugs, and finishes things up. If you want to help, please provide feedback on the [Nexus mod page](https://www.nexusmods.com/valheim/mods/387) or on the [github](https://github.com/RandyKnapp/ValheimMods/tree/main/EpicLoot) for the following:

  * **Bugs** *(check to make sure your bug is new and not already reported)*
  * **Balance Issues** *(drops too strong? Too weak? Ruin the crafting progression?)*
  * **Missing content** *(check the TODO list below to make sure the author isn't already planning to do it)*
  * **Suggestions** for new magic item effects
  * **Suggestions** for something else like UI or art improvements

***EpicLoot works in multiplayer and on dedicated servers!*** The server and all players should have the mod and its dependencies installed.

Information about every magic effect and loot drop table can be found in [info.md](https://github.com/RandyKnapp/ValheimMods/blob/main/EpicLoot/info.md).

## Cheats

Enter these into the console (F5):

  * `magicitem <rarity> <itemtype> <amount>`: Roll a random magic item using the specified values. (alias: `mi`)
    * `<rarity>`: (String) One of: magic, rare, epic, legendary, random. If left empty, uses random.
	* `<itemtype>`: (String) The internal ID of an item. May be "random". If left empty, uses random.
	* `<amount>`: (Int) The number of magic items to roll. If the other values are set to random, rerolls that random item each time. If left empty, uses 1.
  * `magicmats`: Spawns a bunch of all the magic crafting materials

## Dependencies

  * **Extended Item Data Framework**: Required. Download from [Nexus](https://www.nexusmods.com/valheim/mods/281]), [Thunderstore](https://valheim.thunderstore.io/package/RandyKnapp/ExtendedItemDataFramework/)

## Current Known Mod Conflicts

  * **BetterUI** ([Nexus](https://www.nexusmods.com/valheim/mods/189), [Thunderstore](https://valheim.thunderstore.io/package/Masa/BetterUI/)): You won't be able to see the magic item properties in the tooltip. Go to the BetterUI config and set `showCustomTooltips = false`.
  * **Multicraft** ([Nexus](https://www.nexusmods.com/valheim/mods/263), [Thunderstore](https://valheim.thunderstore.io/package/MaxiMods/MultiCraft/)): You won't be able to do any sacrificing or enchanting at all. You will need to disable Multicraft.

## Known Bugs

## TODO

- [X] Print Data
- [X] Finish Magic Effects
- [X] Prevent Repair (reverted for early access)
- [X] Effect exclusivity
- [X] Effect specific items
- [X] Effect requirements (i.e. must have movement penalty before removing it)
- [X] Dropped item particle effect
- [X] Create crafting materials
- [X] Destroy trophies for crafting materials (seidric reduction)
- [X] Destroy magic items for crafting materials (runic reduction)
- [X] Enchant items: add new magic effects to non-magic item
- [ ] Streamline enchanting UI. Use selectable rarity per item.
- [ ] Augment items: change/reroll magic item effects (transmute? modify?)
- [ ] Gamble for magic items from Merchant
- [ ] Custom crafting station for enchanting
- [ ] Loot tables
  - [ ] Monsters
	- [X] Meadows
	- [X] Black Forest
	- [X] Swamp
	- [ ] Mountains
	- [ ] Plains
  - [ ] World Chests
  	- [X] Meadows
	- [X] Black Forest
	- [X] Swamp
	- [ ] Mountains
	- [ ] Plains
- [X] Set item UI treatment
- [ ] Balance, balance, balance
- [ ] Move tooltip code to postfix, parse and inject rather than redo from scratch

#### Future TODO

- [ ] Custom item sets (replace troll too)
- [ ] Rename item if magic (prefix/postfix? Legendary names?)
- [ ] New Runes skill (enchanting)
- [ ] New Seidr skill (for what?)


## Magic Effects:

- [X] ModifyParry
- [X] ModifyArmor
- [X] ModifyBackstab
- [X] IncreaseHealth
- [X] IncreaseStamina
- [X] ModifyHealthRegen
- [X] ModifyStaminaRegen
- [X] AddFireDamage
- [X] AddFrostDamage
- [X] AddLightningDamage
- [X] AddPoisonDamage
- [X] AddSpiritDamage
- [X] ModifyElementalDamage
- [X] AddFireResistance       
- [X] AddFrostResistance      
- [X] AddLightningResistance
- [X] AddPoisonResistance
- [X] AddSpiritResistance
- [X] ModifySprintStaminaUse
- [X] ModifyJumpStaminaUse
- [X] ModifyAttackStaminaUse
- [X] ModifyBlockStaminaUse
- [X] ModifyMovementSpeed
- [X] Indestructible
- [X] Weightless
- [X] AddCarryWeight

## Chests

- [X] TreasureChest_blackforest
- [ ] TreasureChest_fCrypt (???)
- [X] TreasureChest_forestcrypt
- [ ] TreasureChest_heath (???)
- [X] TreasureChest_meadows
- [X] TreasureChest_meadows_buried
- [ ] TreasureChest_mountains
- [ ] TreasureChest_plains_stone
- [X] TreasureChest_sunkencrypt
- [X] TreasureChest_swamp
- [X] TreasureChest_trollcave
- [ ] shipwreck_karve_chest

**Author's Note:** This mod uses an image of the Odal rune (ᛟ) to denote set items. It's reconstructed Proto-Germanic meaning is "Heritage" or "Possession" and the author felt like it was the best rune from the Elder Futhark to signify set items. However, the Odal rune with wings or feet was and is used as a Nazi symbol. The author ***UNEQUIVOCALLY CONDEMNS*** Nazis, Nazism, anti-semitism, and white supremacy. Furthermore, those who hold or practice those beliefs are not welcome to use this mod. F\*\*k Nazis.