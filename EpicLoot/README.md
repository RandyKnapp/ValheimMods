# Epic Loot

This mod aims to add a loot drop experience to Valheim similar to Diablo or other RPGs. Monsters and chests can now drop Magic, Rare, Epic, or Legendary magic items. Each magic item has a number of magic effects on it, that give bonuses to the item or your character when that magic item is equipped.

The mod is currently in Early Access! That means it's not done! Be patient as the author adds new features, fixes bugs, and finishes things up. If you want to help, please provide feedback on the [Nexus mod page](TODO) or on the [github](https://github.com/RandyKnapp/ValheimMods/tree/main/EpicLoot) page for the following subjects:

  * Bugs
  * Balance Issues
  * Missing content (but first check the TODO list below to make sure the author isn't already planning to do it)
  * Suggestions for new magic item effects
  * Suggestions for other improvements

Information about EVERY magic effect and loot drop table can be found in info.md.

Current Known Mod Conflicts:

  * BetterUI: You won't be able to see the magic item properties in the tooltip. Go to the BetterUI config and set `showCustomTooltips = false`.

**Author's Note:** This mod uses an image of the Odal rune (ᛟ) to denote set items. It's reconstructed Proto-Germanic meaning is "Heritage" or "Possession" and the author felt like it was the best rune from the Elder Futhark to signify set items. However, the Odal rune with wings or feet was and is used as a Nazi symbol. The author ***UNEQUIVOCALLY CONDEMNS*** Nazis, Nazism, anti-semitism, and white supremacy. Furthermore, those who hold or practice those beliefs are not welcome to use this mod. Fuck Nazis.

## Bugs:

- [ ] Upgrading item removes magic item

## TODO:

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
- [ ] Enchant items: add new magic effects to non-magic item
- [ ] Streamline enchanting UI. Use selectable rarity per item.
- [ ] Augment items: change/reroll magic item effects (transmute? modify?)
- [ ] Gamble for magic items from Merchant
- [ ] Custom crafting station for enchanting
- [ ] Loot tables
  - [ ] Monsters
	- [X] Meadows
	- [X] Black Forest
	- [ ] Swamp
	- [ ] Mountains
	- [ ] Plains
  - [ ] World Chests
  	- [X] Meadows
	- [X] Black Forest
	- [ ] Swamp
	- [ ] Mountains
	- [ ] Plains
- [X] Set item UI treatment
- [ ] Balance, balance, balance
- [ ] Move tooltip code to postfix, parse and inject rather than redo from scratch

Future

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
- [ ] TreasureChest_fCrypt ???
- [X] TreasureChest_forestcrypt
- [ ] TreasureChest_heath ???
- [X] TreasureChest_meadows
- [ ] TreasureChest_meadows_buried
- [ ] TreasureChest_mountains
- [ ] TreasureChest_plains_stone
- [ ] TreasureChest_sunkencrypt
- [ ] TreasureChest_swamp
- [X] TreasureChest_trollcave
- [ ] shipwreck_karve_chest