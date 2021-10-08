## Version 0.8.7
  * Fixed Auga integration for crafting materials
  * Fixed Auga integration for showing the full tooltip in the sacrifice panel
## Version 0.8.6
  * Disabled water walking (won't fix), but made it free to augment off of existing gear.
## Version 0.8.5
  * Updated for H&H
## Version 0.8.4
  * Added support for Project Auga
  * Completed localization for the entire mod (thanks Anya77!)
## Version 0.8.3
  * ServerSync updated to latest
  * Fix for bounty completion bug
## Version 0.8.2
  * Better item name localization
  * Fix for explosive arrow friendly fire
  * Fix for Armor +% magic effect
  * Fix for serpentscale shield and parry effects
  * Fix for bounty target NRE and player known sync ([sbtoonz](https://github.com/sbtoonz))
  * Fix for getting encumbered incorrectly ([M3TO](https://github.com/M3TO))
  * New legendary weapons from LitanyOfFire: Lævateinn, Strength of the Valkyrie, Skofnung, Gram, Angurvadal, The Shattering, Atgier of Sagas, Skaði's Hunt, Ullr's Favor, The Endless Hunt, Message of Lindisfame, Höfuð, Njǫrd's Favor, Life-Drinker
## Version 0.8.1
  * Fixed an exploit in enchanting
## Version 0.8.0
  * Magic Effect lookup is now optimized
  * Removed debug function that was eating up frame-time
  * Legendary effects work in multiplayer
  * Fixed a nullref in Stagger Damage
  * Fixed issues with Indestructible
  * Water Walking no longer works in dungeons to prevent issues in Sunken Crypts
  * Feint now works as intended
  * Fixed a bug where level gaps in the loottables.json caused a nullref
  * New legendary: Mjolnir
  * Added legendary set system
  * Added item ability system
  * New Legendary Sets: Heimdall's Legacy, Ragnar's Fury
## Version 0.7.10
  * Fix for Server Side Character crash bug
  * Increased epic and legendary drop rates at low tiers
  * Wishbone is now Epic rarity
  * Dragon's Tears and Yagluth Things can now be converted to legendary runestones or sacrificed for legendary crafting mats
  * Moder and Yagluth trophies sacrifice for more runestones (2, 4 instead of 1, 3 respectively)
  * Bosses drop more items overall, especially Moder and Yagluth
  * Weights in config files are now floats instead of ints
  * Magic Effect: Luck, increase chance for higher rarity items
  * Magic Effect names shortened, detailed descriptions added to compendium
  * Fix not loading initial known items in singleplayer and for server host. ([jsza](https://github.com/jsza))
## Version 0.7.9
  * Reduced Opportunist chance to proc
  * Allowed AddSkill to exceed 100 points (and fixed vanilla bug around damage ranges not going over 100%)
  * Excluded pickaxes from a bunch of magic effects that are useless for them
  * Added increased chop and pickaxe damage for axes and pickaxes when they have bonus slash/pierce damage (by [M3TO](https://github.com/M3TO))
## Version 0.7.8
  * ConfigSync from blaxxun added. MCE and the MCE addon are NO LONGER NEEDED!
  * Config json files are now editable while the game is running and will automatically hot reload! (thanks blaxxun!)
  * New Magic Item Effects:
    * Water Walking
	* Double Jump
	* Quick Draw
	* Explosive Arrows
	* Skill Increase
	* Increase Stagger Duration
	* Quick Learner
	* Feather Fall (with effect!)
	* Thorns
	* Stagger on hit
	* Avoid damage on hit
	* Auto-recall thrown weapon
	* Bonus when health is low: Move Speed, Health Regen, Stamina Regen, Armor, Damage, Block Power, Parry, Attack Speed, Avoid Damage, Lifesteal
	* Free Build (hammer)
	* Comfortable
	* Glowing
	* Execution
	* Riches
	* Opportunist
	* Duelist
	* Increased Stagger Damage
	* Immovable
  * Overhaul of Resistances: removed Spirit, resistances now reduce damage by a set percent, and stack (fix existing resistances by using the console command: `fixresistances`)
  * Known items and recipes (for item gating) are now synced between all players on a server
  * Added "Sacrifice All" button to Sacrifice tab which sacrifices the whole stack of whatever you have selected
  * Fixed a bug where no boss trophies would drop in single player
## Version 0.7.7
  * Added config to drop trophies based only on the number of nearby players, not just total players on the server
  * Fixed unlocalized minimap icon text for treasure maps
  * Embedded fastJSON and epicloot asset bundle to DLL (this fixes several linux server issues including one that was causing problems with bounties)
  * Merged fix from [maxrd2](https://github.com/maxrd2) fixing a crash when hitting enemies on Linux
  * Added a toggle to disable adventure mode features: secret stash, gambling, treasure maps, and bounties
## Version 0.7.6
  * Updated loottables to cover up to 5-star enemies for all enemy types
  * Bounties are now completed correctly if another player kills your bounty target while you are offline (bounty completes on next login)
## Version 0.7.5
  * Correctly spawning and checking bounty targets slain (this breaks all current old-version bounties)
## Version 0.7.4
  * Removed freeze time, as it totally breaks multiplayer
## Version 0.7.3
  * Added config value to hide equipped and hotbar items in the sacrifice tab
  * Also added freeze time keybind (RCtrl+Backspace) and made free fly camera fixed update time (so you can fly around during freeze time)
  * Integrated LifeSteal magic effect (submitted by [nanonull](https://github.com/nanonull))
  * Added ModifyAttackSpeed, Waterproof, Paralyze
  * Localization part 1
  * Hotfix for bug where buying treasuremaps or bounties can disconnect you from the server
## Version 0.7.2
  * Bosses drop one trophy per player (configurable)
  * Fixed a bug where you could complete a bounty just by killing the minions if they were the same type as the bounty target
  * Changed bounty generation so you don't have identical bounties to other players on the server, to prevent confusion
  * Can now abandon bounties (please report bugs if the monsters actually don't spawn though)
## Version 0.7.1
  * Fixed bug with disappearing bounty/treasuremap pins on logout
  * Fixed but with items purchased from Haldor's Secret Stash disappearing on logout
  * Added some junk to trophy recipes
## Version 0.7.0
  * Added adventure panel to merchant
  * Added purchasing crafting mats from merchant
  * Added gambling for magic items from merchant
  * Added purchasable treasure maps
  * Added bounty hunting
  * Fixed some bugs with item gating
  * Fixed some bugs with augmenting legendary items
## Version 0.6.4
  * Item Names Update!
  * Adding logs to Augmenting
  * Show all magical effects in compendium
  * Show currently equipped item in tooltip by holding LeftControl
  * Adding support for Mod Config Enforcer (see addon mod)
## Version 0.6.3
  * Fixed a bug where augmenting an equipped item would apply the augment to the first item in the augment list
  * Fixed a bug where selecting a different item while one was already augmenting would not cancel the craft
  * Moved the restricted item names list to config (fan request, to allow Dyrnwyn as a drop on their server)
## Version 0.6.2
  * Fixed a bug where augmented items would not save when logging out
  * Fixed a bug where augmenting some items would result in UI errors
  * Changed the set item icon again, shieldknot
## Version 0.6.1
  * (reserved version number for Thunderstore rollback to 0.5.16)
## Version 0.6.0
  * Added Augmenting at forge with augmenter or at artisan table
  * Sacrifice, Enchant, and Augment recipes are now configurable in `enchantcosts.json`
  * Added options for alternate crafting tab layout (compatibility with SimpleRecycling)
  * Changed set item marker to a non-nazi associated symbol (triskelion instead of odal rune)
  * Added option to `magicitem` console command to specify the number of effects to roll
  * Added console command `cheatgating` which toggles item gating on or off
  * Enabled logging toggle for all of EpicLoot logging
## Version 0.5.16
  * Three new craftable, enchantable utility items
  * All conversion and upgrade recipes for crafting materials in configurable json file
  * Small coin amounts added to conversion and upgrade recipes for crafting materials
  * Magic effect changes:
    * Parry now only rolls on two-handed weapons and shields
	* Block now only rolls on shields
## Version 0.5.15
  * Loot tables now use a leveled format that can be extended beyond level 3
  * DLC Stuff allowed to enchant
  * Can limit magic effects by SkillType
  * Can limit magic effects by exclusions, see info.md
  * Drastically reduced Movement Speed bonus (for new drops or enchanted)
  * Added gating for dropped item types by known recipe or known item (or unlimited)
## Version 0.5.14
  * Updating the console command with a few more exclusions
  * Modified a UI display to better support multiple hotkeybars and multiple inventory grids
## Version 0.5.13
  * Made it slightly easier for other modders to access the enchanting and disenchanting information
  * Made the tooltip text lookup for set items more defensive to prevent some mod conflicts
## Version 0.5.12
  * Enchanting an item maintains its current durability percentage
  * Enchanting uses a new UI flow and shows the item after you enchant it
  * Magic Item Effects now load from a config file
  * Magic Item Effects now use a string ID instead of an enum
  * Changing TreasureChest_plains_stone loottable to use the TreasureChest_heath table
  * Fixed mod conflict with PlantingPlus
## Version 0.5.11
  * Fixed a bug where crafter name would be applied to upgraded objects
  * Upgraded objects automatically repaired to full durability
  * Changing the default rarity of Dverger Circlet, Megingjord, and Wishbone to Rare
  * Fixing a bug where Eikthyr (or some other mob, like Troll lvl 3) dies repeatedly
## Version 0.5.10
  * Removing all cheat and dlc items from the random loot generation cheat	
  * Fixed a bug that showed 0% chance for all magic effect counts while enchanting	
  * Fixed a bug that caused some chests to spawn non-magical items	
  * Updated loot tables with feedback from comments:
    * Chitin weapons moved to tier 2 weapons
    * Draugr Fang added to tier 4 weapons
    * Wolf Cape added to tier 4 armor
    * Added more options to Troll
    * Added more tier 3 drops to swamp mobs
    * Increased drop chance for Fuling Berserker and Fuling Shaman
    * Reduced loot tiers, increased drop chance counts for Serpent
    * Fixed treasure chests so they properly reflected their biome (`plains_stone` is actually Black Forest, `heath` is Plains)
    * Reduced loot tiers for `meadows_buried` and `shipwreck_karve` chests (they still have higher rarity chances)
## Version 0.5.9
  * Fixed bug where sacrificing with nearly full inventory resulted in lost items (items that do not fit in the inventory now fall to the ground)	
  * Loot2 and Loot3 in the loot table are now exclusive (e.g. if the mob is level 2, and Loot2 is present, then only the loot from Loot2 is used, otherwise it falls back to Loot)	
  * ItemSets have been added to the loot table schema (If the "Item" field of the Loot list is in the item set, roll on that loot table for that item instead)	
  * Can now reference other loot tables in the loot table item config using "Item": "`object`.`level`" where `object` is the name of a loot table entry and `level` is an integer between 1 and 3 that refers to Loot, Loot2, or Loot3.	
  * Number of magic effects per rarity is now configurable in loottables.json in the "MagicEffectsCount" object	
  * Completed loottables with updates from feedback and using the new system	
## Version 0.5.8
  * Hiding console commands behind the cheat flag	
  * Removing log spam	
## Version 0.5.7
  * Removing accidentally added debug object	
## Version 0.5.6
  * Fixing crafting tabs showing magic items	
  * Changing crafting tab item description to scrolling (can turn off in config)	
  * Can set display name of rarity types in config	
  * Can put non-magic items in the loot table by omitting the Rarity chance array	
  * Loot beam sounds now respect the in-game SFX volume setting	
## Version 0.5.5
  * Fixed yet another couple of crafting tab bugs	
  * Reduced mats upgrade recipe to 5:1	
  * Added shard to same rarity dust/essence/reagent recipe at 2:1	
  * Increased drop chance on The Elder and Bonemass	
  * Fixed crafting recipe list selection exploit/bug	
  * Added special recipe to sacrifice Swamp Key (who needs more than one?)	
## Version 0.5.4
  * Fixed bug with viewing effect ranges	
  * Added troll trophy to rare disenchant list	
  * Moved greydwarf brute and shaman trophy to rare disenchant list	
  * Fixed an icon bug where the new material message showed the red material icon	
  * Fixed some bugs with the crafting tabs	
## Version 0.5.3
  * Fixed stamina regen and health regen	
  * Added holding shift to see ranges in tooltips	
  * Fixed Elder/Bonemass runestone rarity mixup	
## Version 0.5.2
  * Fix for an enchant exploit	
  * Updated swamp loot tables	
  * Fixed resistances not working at all	
## Version 0.5.1
  * Fixed never respawning after dying	
  * Fixed a bug where a whole stack of trophies would be disenchanted for a single crafting material	
  * Updated correct ## Version number everywhere