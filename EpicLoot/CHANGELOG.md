## Version 0.9.33 - Valheim Update 0.217.27
* Updated for 0.217.27 References
* Added Brenna's Trophy to Sacrifice List

<details>
<summary><b>Changelog History</b> (<i>click to expand</i>)</summary>

## Version 0.9.32 - Auga Compatibility Part 2
* Now updating Skills in Auga when Magic Effects give Bonus to Skills.

## Version 0.9.31 - Auga Compatibility
* In preparation for Auga Update, this adds changes needed to support Auga interfaces for the Enchanting Table and Tooltips
* Various bug fixes

## Version 0.9.30 - Bug Fixes
* Updated a couple entries in loottables.json that were typos or incorrectly tiered.
* Updated ServerSync to current community standard.
  * Will need to update SERVERS to this version.
* Updated EpicLoot Unity
* Made a change to Augmenter/Enchanter Furniture that may cause errors from the previous version.
  * Dismantle the augmenter/enchanter created from 0.9.29 and recreate.

## Version 0.9.29 - Bug Fixes
* Tooltips when only one effect was present were getting cut off.
  * This is fixed by adding some text after the effects.
* Added Rarity and Effect Count to Tooltips.
* Fixed a display issue on Skills when a +Skills Effect is equipped.
* Fixed a long term display issue where additional skill bar wouldn't disappear after unequipping a weapon
* Added a null check to the Loot Roller in rare cases where an item is configured, but doesn't exist in game.
* Changed original augmenter and enchanter to be furniture and updated descriptions
* Updated Unity Project to 0.217.24 and TMP 3.2.0 -preview5
* Updated a few places where colors were not hex values.

## Version 0.9.28 - Augmenting Menu Issue
* Missed a spot where I needed to update to TMP_Text

## Version 0.9.27 - Fixing Server Sync
* Had to update server sync correctly.

## Version 0.9.26 - Valheim Update 0.217.24
* Updated for Valheim 0.217.24
* Adjust Swamp Bounties to have better chance to spawn. Looking at you Leeches!

## Version 0.9.24/25 - Fixing Bounties
* Fixed: Some bounties would spawn without name plate and would not register as a kill.
* Slightly changed the logic to hopefully prevent underwater bounty spawns that shouldn't be underwater.
* 0.9.25 - is a recompile to up the version after the zip got messed up.

## Version 0.9.23 - Crafting with Enchanted Components
* Recipes built with items that are Enchanted will now carry over their magical properties to the new item.
    * The highest magical rarity will carry over if more than one magical item is consumed.
* Server-Synced Configuration is available toggle the enablement of this functionality.
    * Default will leave this functionality Disabled.

## Version 0.9.22 - Bounty System Improvements Part 2
* Would help if I included the translations in the Module Zip
    * Also, when Auga is NOT installed, have to forcably localize the strings.
* Added the new trophies to the Enchantcosts

## Version 0.9.21 - Bounty System Improvements
* Completely Overhauled how the Bounty Ledger is stored.
  * No longer using GlobalKeys when playing.
  * Saves to a data file in config folder.
  * Saves a backup to World File on Shutdown.
  * Restores from World File is data file is missing.
  * Tested on Single Player, P2P, and Dedicated Server
    * Tested Bounty Kills without Issue
* Added a Bounty Limiter to the Config
  * When enabled, and set to a max, prevents players from purchasing more bounties if they are at max In-Progress bounties.
  * These are per-player maximums.
* Revamped how Attack Speed modifications are handled using the AnimationSpeedManager
  * Thanks to Wacky for the assistance in the Discord getting it setup.
  * Thanks to Smoothbrain for their community contributions.
  * This should allow Wacky's EpicMMO and Duel Wield to adjust attack speeds accordingly with Epic Loot
## Version 0.9.20 - Hildir's Request Bug Fixes
* Added null checking to the Gated Items to prevent patches from causing errors.
* Fixed Map Pins going away when logging in and out
* Extensively tested the Sacrifice process on the Enchanting Table.
  * The change here is that stuff will sacrifice if you have it in your inventory, instead of showing the full list of sacrifices.
    * If this still doesn't work for you, please ensure all your JSON's are updated correctly.

## Version 0.9.19 - Hildir's Request Update
* Provides Compatibility with Hildir's Request
* Removes Legacy Workbench Functions
    * To perform Enchanting, please use Enchanting Table
* Fix for Map Pins

## Version 0.9.18 - New Feature: Enchanting Table Upgrades (Take 3)
* Fixed an issue where when placing a table down, can cause an error.

## Version 0.9.17 - New Feature: Enchanting Table Upgrades (Take 2)
* Fixed the config where it was defaulting only two features. Whoops.
  * All features will be enabled by default.
  * Use config to adjust appropriately.
* Completely re-engineered how the client/server relationship works.
  * Notes: Any table upgrades done in 0.9.16 **will be reset**. This was unavoidable.
  * Table Upgrades are now sticky on save.
* Updated UI Refresh to refresh information on interaction.
  * This should result in ingredients being updated properly in most cases.

## Version 0.9.16 - New Feature: Enchanting Table Upgrades
* The Enchanting Table now allows for Table Upgrades which can increase various modifiers. 
  * Upgrades are now available individually for each Enchanting Table Feature:
    * Sacrificing
    * Enchanting
    * Converting Materials
    * Disenchanting
    * Augmenting
  * In addition, Enchanting Table Features are now gated and need unlocking before using.
  * Each Enchanting Table maintains their own upgraded state.
  * Epic Loot configuration adds a new Config Section called "Enchating Table" and two new settings that apply to ALL ENCHANTING TABLES:
    * Upgrades Active
      * When enabled, table features are locked and must be unlocked and upgraded. 
      * When disabled, all table features are unlocked, and set to be Level 1, with no upgrades available.
    * Features Active
      * Allows Specifying which Enchanting Table Features are enabled.
        * Select the features desired and unselect the features not desired.
  * Enchanting Table Upgrade Costs are JSON configurable, and can be patched like other JSON's
    * The `enchantingupgrades.json` provides adjustment for upgrade costs and upgrade benefit adjustments.

## Version 0.9.15 - Valheim Update 0.217.5 - Part 3
* Updated `enchantcosts.json` with the corrected spelling of Trophy (from Trophie).
* Fixed issue with bounties not working correctly after update to 0.9.14.

## Version 0.9.14 - Valheim Update 0.217.5 - Part 2
* Left a piece out that needed to be updated with regards to `CopyCustomDataFromUpgradedItem`

## Version 0.9.13 - Valheim Update 0.217.5
* Required updates for Valheim version 0.217.5

## Version 0.9.12 - More Performance Improvements and Bugfixes
* Reduced the frequency that Multiplayer send Legendary Info
    * Improves frame rates when near others.
* Slightly improved performance of Loot Beams
* Fixed HotkeyBar Icons Updating
    * Now updates background when items removed.

## Version 0.9.11 - Performance Improvements
* Rebuilt the way Map Pins are maintained for Treasures and Bounties.
    * This should no longer cause a slow down in FPS just because you have a bunch of bounties.
* Rebuilt InventoryGui.UpdateGui Methods which were drawing Magic Item backgrounds.
    * Removed Postfix Patches and Loops
    * Used Transpilers instead
* Rebuilt HotKeyBar Updates
    * Removed Postfixes and implemented Transpiler instead.
* Changed the Location of Where Adventure Saved Data is stored.
    * Cleaned up KnownTexts and now saves in Player Custom Data

## Version 0.9.10
* Udpated for Valheim 0.214.305
* Added Merchant Fix for Auga 1.2.0
* Added Changelog.md for Thunderstore

## Version 0.9.9
* Removed butcher knife and stone axe from the allowed to enchant list
* New Feature: Disenchant - remove the enchant from an item at the cost of iron bounty tokens
* Merge Patching: Enabled "Merge" patch action, adds or overwrites all named properties in the target object (Thanks @nelson-saldanha)
* Updating for Valheim 0.214.2 Patch
## Version 0.9.8
  * Patch Config File location moved. Patch files have been moved outside of the Epic Loot Plugin Folder to prevent mod managers from deleting patch files.
    * Patch files are now located in BepInEx\config\EpicLoot\patches folder.
    * This folder will automatically be created upon first run of Epic Loot.
    * Debug Merged Output Files (if set to output) will be located in BepInEx\config\EpicLoot
  * Fix for "craftable legendary with no effects" bug
## Version 0.9.7
  * Fixed an unfortunate amount of bugs:
    * Feather Falling, Indestructible, and Free Build (among others) not loading correctly.
    * Old Items Created in an extremely old version of EIDF were not converting to Custom Data.
## Version 0.9.6
  * Removed dependency on EIDF in the Thunderstore manifest
## Version 0.9.5
  * Fixed a bug where two enchanting tables next to each other could not be used
  * Removed accidentally included "_patched.json" files
## Version 0.9.4
  * **New Features:**
    * Enchanting Table
      * New build piece with custom crafting UI just for Epic Loot!
      * Currently allows mass sacrificing, mass material conversions/upgrades/junk->trophy recipes, enchanting and augmenting (but more to come!)
      * Item and recipe lists sortable and filterable
      * Select many items or recipes and do them all at once
      * The old way of enchanting at the forge will stay in the game for now, but will be removed in a future release
      * A new config file (`materialconversions.json`) was added to facilitate the material conversion recipes at the new table
    * Added new Magic Effects: DoubleMagicShot and TripleBowShot (Credit: ploppy for the PR on this!)
  * **Changes:**
    * Converted EpicLoot to use the new Custom Data field provided in-game and REMOVED dependency on Extended Item Data Framework.
      * This means that **EpicLoot no longer requires Extended Item Data Framework**, in order to run.
      * EpicLoot is also fully compatible with OTHER mods still using Extended Item Data Framework without issue.
      * Mods that show as incompatible with EpicLoot or Extended Item Data Framework will need to be updated by their respective mod authors in order to remove the incompatibility.
  * **Bug Fixes:**
    * Rare Reagent Localization was not correct. Fixed to allow localization.
    * Made AllowedItemTypes and ExcludedItemTypes to be flexible to be either a Game Item Type, or a configured Item Type grouping from the iteminfo.json.
      * This allows for things like the StaffSkeleton to be included as AllowedItemTypes of "Staffs" where as "Staff" is not an in-game item type and does not include StaffSkeleton.
    * Fixed Auga Tool Tip fails when an item name does not reference a localizable string.
    * Fixed issue with Magic Item backgrounds sticking to Hotkeybar and Quick Slots when items are moved around.
    * Adding vanilla sparkles to mundane items, such as RubyGold ring
    * Added the ability to patch translation.json using patch config without the need to restart the game or server.
    * Fixed errors with Stagger effect.
    * Items that have many Available Effects/Enchants were clipping when reviewing them in the Augment tab. Fixed to scale the font to a smaller size.
      * This is not an issue in Auga.
## Version 0.9.3
  * Introduction of the JSON Configuration Patching System. 
    * Please reference https://github.com/RandyKnapp/ValheimMods/wiki/Config-Patching for information.
    * Example Patch Configs and Additional Information to be made available Soon(tm)
  * Localized Asset Names
    * This has removed all elements of hardcoded names in the Unity Prefabs and have replaced with localizable strings.
    * This also means that the Config settings for customizing the rarity names have been removed from the Module Config.
    * To customize the Rarity Names, edit the **translations.json** file.
    * Additionally, to target EpicLoot items by name, use this notation `$mod_epicloot_legendary $mod_epicloot_assets_essence`
  * Fix bug where Runestones, Belts, Rings, and Andvaranaut where not able to be picked up off the ground when spawned.
    * They also now glow.  Oooooohh.. shinny!
  * Updated Item Gating to provide more balance and less Clubs.
    * In `iteminfo.json`, "Fallback" can now be a specific prefab name (for a single item), or the **Type** of a different group.
      * For example: Instead of Staff's falling back to a single Club, Staff's now fall back to Spears, and based on Gating preferences, can spawn different levels of Spears.
      * This is also changed for Fist weapons, and some other balance changes.
  * Fixed Cultist and Growth Bounties Prefab Names which were incorrect and preventing Bounty from spawning.  (This will require you to update your adventuredata.json)
  * Fixed Missing Mistland items from loottables. (This will require you to update your loottables.json)
  * Added in ability to use prefab names in AllowedItemNames/ExcludedItemNames
  * Now showing modified attack stamina in Epic Loot tooltip.
  * Added in MountainCave TreasureChest to Loot Tables. (This will require you to update your loottables.json)
  * Changing "Modify Damage" enchant to be a Scaling Percetage based Damage Addition.  This will allow scaling to be better between lower and higher tiered players/weapons.
  * Adding in Dvergers and DvergerMage's to Loot Table, whom were left out. 
  * Removing Timescale which was not working and preventing vanilla timescale from functioning.
  * Adding Blood Magic and Elemental Magic Skills to available Skill Effect options.
## Version 0.9.2
  * When using CLLC, creatures now correctly drop Epic Loot items
  * Gambles won't drop mats when the ItemsToMaterialsRatio is set to materials
  * Fixing a bunch of Object.Destroy calls that should be ZNetScene.instance.Destroy calls
  * Preventing the rings and leather belt from spawning at 0,0,0 when players join a game
  * Fixing the terminal to work for a ton of commands that were broken
  * Added an asset for each of the crafting materials
  * Added Biome/Boss/BossDefeatKey configuration to adventuredata.json
  * Fixing a bug that was causing Auga's compendium to display incorrectly
## Version 0.9.1
  * Drop rates can now be globally scaled by using the "Global Drop Rate Modifier" config value
  * Magic Materials can be dropped instead of enchanted items by using the "Items To Materials Drop Ratio" config value.
    * The value goes from 0-1 where 0 is all items and 1 is all materials and any values in between is a random percentage of drops that will be materials instead of items.
  * Items are now gated by boss kills instead of player known recipes/items (this removes the PlayerKnown RPCs completely)
    * Server admins be sure to update your server configs
    * The older way of gating by player known recipes or player has crafted is recommended for local games only
  * Bounties can now be gated by boss kills, see "Gated Bounty Mode" config option (credit: Vapok)
  * Gambles are now only gated by the player's known recipes, not the server's
  * Items in the RandomItems section of the SecretStash now only appear if the player knows the recipe (OtherItems still always appear)
  * Treasure map chests can be configured to contain Iron or Gold Bounty Tokens as well as coins. (Still defaults only to Forest Tokens, just adding more customizability for server admins)
  * Fixed visual issue with enchant success screen for Auga
  * Added support for storing enchanted items on Armor Stands
  * Fixed tooltips missing stamina/health/eitr use
  * Updated data to handle new item type TwoHandedWeaponLeft
  * Fixed various nullrefs
## Version 0.9.0
  * Updated for Valheim version 0.212.7 (Mistlands Update)
  * Augmenter no longer needed for augmenting (engage with augmenting far earlier in your playthrough)
  * Updated loot tables, gambles, treasure maps, and bounties for Mistlands and H&H
  * Added treasure chest loot for Mistlands dungeons
  * Fixed some multiplayer issues with Riches/Luck (only update on client, only update when equipment changes)
  * Updated FeatherFall to use vanilla effect
  * Deprecated several magic effects:
    * WaterWalking
    * AddSpiritResistance (Player already immune)
    * AddSpiritResistancePercentage
    * ReduceWeight
    * AddFireResistance (confusing overlap with the -X% Fire Damage effects)
    * AddFrostResistance
    * AddLightningResistance
    * AddChoppingResistancePercentage
    * NOTE: Deprecated effects can be augmented off of items for free, but cannot be kept. They may no longer function.
  * CanBeAugmented feature of magic effects is now functional
  * Reduced the possibility of recalling weapons getting too far away to recall (stop throwing them off mountains!)
  * Fixed a bug with Indestructible items
  * Bounty targets are sometimes named special names (join the Patreon to add your name!)
  * Fixed a bug with magic effect requirements for or excluding Tower Shields
## Version 0.8.10
  * Update for Valheim 0.211.11 and ServerSync 1.13
## Version 0.8.9
  * Compatibility fix for the Auga version of the merchant panel.
## Version 0.8.8
  * Fixed an issue with multiplayer that was causing too much data to be sent across the wire. Lag and desync issues should be minimal now.
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

</details>