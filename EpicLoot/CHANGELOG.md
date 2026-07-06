## Version 0.12.15

* Fixed an issue with bounties not spawning correctly if not using the Star Level System mod.
* Fixed an issue with bounty and treasure map pins not displaying until toggled.
* If you experienced loot not dropping from previous version upgrades (12.12, 12.13) please refresh your BepInEx\config\EpicLoot\baseconfig\loottables.json file to remove null entries.

## Version 0.12.14

* Fixed an issue with auto-adding loot tables causing mobs to not drop loot.
* All items from the game can now be added to loot tables from patches and work with the "Auto Add ..." configuration settings.

## Version 0.12.13

* Added back PlayerExtensions.GetEquipment for mods that patch that method.
  * If you are a mod author please remove your patches to this method! We refactored it to use vanilla methods in version 12.7.

## Version 0.12.12

* Bug fix for Mead Cooldown reducing all SE_Stats mead times, should now work as intended.
* Bug fix for Gambling loot tables not saving values correctly when auto-assigned causing large coin values. 
  * Needs a adventuredata.json configuration refresh to apply.
* Bug fix for Modify Armor effect showing up for non-armor applying item types.
* Bug fix for Modify Elemental Damage effect showing up on the wrong items.
* Bug fix for a tool tip display issue when items had a subtitle (trinkets).
* Bug fix for a tool tip display issue for set items not showing equipped correctly.
* Disabling adventure mode in the main configuration file will now also remove the minimap toggles.
* Star Level System API support.

## Version 0.12.11

* NEW FEATURE: map pins from bounties and treasure maps can now be toggled in the minimap UI in the bottom left corner! Thanks Rusty!
* Removed a debugging feature that accidentally made it to production. This would cause a new folder called "dumps" to be generated in your game directory. It is safe to delete that folder and its contents. Apologies for the mess!
* Reduced visibility of patch warnings for adding and removing existing entries. These warnings will no longer be forced and should keep the log file cleaner by default. They will appear if your log settings are set to show Warning and Error.
* Tweaked descriptions of some base configurations in the epicloot.cfg file to be more clear.
* UI tooltip compatibility fix when using the VENI mod. Tooltips when generated on the right side of the screen should display to the left of the cursor now.
* Bug fix for sacrificing unidentified items not using the correct configuration because the default value for "isMagic" and "IsUnidentified" was true if not explicitly set. Existing baseconfig "enchantcosts.json" file may still have the issue, you may need to regenerate it to see the update depending on your game patches and settings.
* Fixed the StaggerOnDamageTaken magic effect not working.
* Fixed identifying items would have twice the magic effect power of items enchanted by other means.
* Fixed a null reference exception issue when generating treasure maps if configurations were missing for any biomes.
* Fixed Multishot effects CostScale setting not applying to stamina and eitr costs correctly (this setting does not work for draw stamina).

## Version 0.12.10

* Fixed an issue with enchanting table identifying unknown magic items changing the stack size of the original item (coins and other costs).
* Fixed formatting error with minimal configuration that would not load the mod properly when used.
* Fixed formatting issue with the Turkish localization.
* Fixed enchanting table rune extraction not always destroying the correct inventory item.
* Minor spelling fixes for English localization.
* Added missing localizations for ModifyFireRate magic effect for all other languages (existed for English).
* Added missing localization for key mod_epicloot_me_modifydamage_desc.

## Version 0.12.9

* Fixed issue with empty IdentifyCosts throwing an error.
* Added compatibility with Guilds mod that broke buying things in the custom trader menu.

## Version 0.12.8

* Many configuration tweaks. Delete and regenerate the json files specified to get the changes for each or grab the fixes manually if you have made changes:
* adventuredata.json, enchantcosts.json: Added default configurations for "None" and "Misc" item types. This will fix support for some modded items like "Bows Before Hoes" quivers.
  * If you see other items with no costs in the enchanting table it is related to this issue. Please report issues only after refreshing your base configurations.
* enchantcosts.json:
  * New field IsUnidentified for DisenchantProducts to better distinguish the configuration for these items with the previous change.
  * Changed identify CostByRarity blackforest cost to Bronze to match other items.
* loottables.json: Fixed a bug with the auto sorter misclassifying items if their first crafting material was lower tier than the rest.
* itemsorter.json:
  * Added Ooze to Swamp BiomeMaterials.
  * Fixed Plains having the bonemass boss key rather than defeated_goblinking.
* Removed throwable bombs showing up as a possible unidentifiable item roll.
* Added mod compatibility for Steady Regeneration (broke in 0.12.6).

## Version 0.12.7

* Reworked magic effects OffSet Attack and DodgeBuff to fix bugs including missing assets (sound and visuals).
* Normalized OffSet Attack values in magiceffects.json, this value represents the percentage of damage reduction (15-40%).
* Bug fix for enchanting table identify costs not applying for more than one cost item.
* Compatibility improvement for other inventory mods, should recognize magic items in additional slots when equipped.
* Bug fix for spam accepting bounties at the merchant and fixed errors when closing the store UI before completed.
* More edge case safeguards for misconfigured files and unexpected game items.

## Version 0.12.6

* Fixed AddHealthRegen not applying after the 12.5 update.
* Fixed typo for defeated_eikthyr key in new itemsorter.json file.
* More edge case detection for the automatic item sorting logic.
* Added some null reference exceptions safeguards in different places in code.

## Version 0.12.5

* NEW itemsorter.json configuration file to configure the auto-add behaviors!
* Tooltips now display on hover in the enchanting table menus.
* Some minor API updates.
* Refactored how the tooltip is built, tried to add any missing information.
* Refactored many magic effects to fix issues including Mead effects, damage effects, block and parry, base player stats and regen, explosive arrows. Some calculations will work slightly different than before.
* Removed the all powerful throwable butcher knife. If you have one of these already rolled it should be blocked from throwing now.
* Reworked bulk up enchant to remove X% of health regen and add X% of max health. If you have 50% enchant on an item, 6 regen ticks, and 80 health you should get 3 regen ticks and 120 max health.
* Band-aid fixed a rare error with offset attack audio clip not existing.
* Did a QOL pass to clean up the magic effects for each available configuration. There were so many changes that listing them here would be too long. If you need to see the specific changes check the release commit comparison on Github.
  * Normalized some Increments to make more sense. Tried to make the minimum 0.5 when possible.
  * Fixed some item type checks for all configs, majorly for the balanced config.
  * Added missing Trinket type in many places for the minimal config.
  * Added missing rarity values on some effects.
  * Made it so only one form of modify damage can roll on an item for balanced and minimal, and only one of physical and one of elemental on legendary config.
  * Fixes for excluded effects, they must be excluded on both effects to work correctly.
* Note: If you used version 0.12.0+ please delete your old BepInEx\config\EpicLoot\baseconfig and let them regenerate! Or, if you have made changes, grab the changes from github to manually update your files.
* Note: If you are NOT making edits to the baseconfig files we highly recommend setting "Debug - Always Refresh Core Configs" to true in your randyknapp.mods.epicloot.cfg file. Then you will not need to manually delete to refresh them.

## Version 0.12.4

* Fixed another an issue with MultiShot not resetting weapon attack values properly due to the percent chance to trigger config.

## Version 0.12.3

* Changed how patches are applied - they will no longer override your baseconfig files on startup! You should be able to generate your baseconfig files once then make changes without them being reverted. See the [wiki for more information](https://thunderstore.io/c/valheim/p/RandyKnapp/EpicLoot/wiki/358-6-config-patching/). We apologize for the confusion around this new feature.
* Fixed an issue with MultiShot breaking enemy projectile damage (someday we will get this effect right on the first try).
* Fixed enchanting materials not stacking with "broken" items in containers. (if this is still happening after the update please report the issue)
* Changed the ..removespeedpenalty_display localization from "Weightless" to "Unhindered". This was a small issue with the 0.12.0 update. Previously this effect was called "No Movement Speed Penalty".
* Added missing configurations for ModifyMagicFireRate and ModifyFireRate in the magiceffects.json files.
* Reenabled Weightless rolls for the balanced config.

## Version 0.12.2

* Fixed some display issues with the in game compendium.
* Added some localizations for the compendium menus.
* Updated Russian translations.
* Fixed an issue with Identify items not selecting a valid fallback, resulting in failure to identify items (mainly swamp items).
  * If you used version 0.12.0+ please delete your old BepInEx\config\EpicLoot\baseconfig and let them regenerate!
* Fixed some edge cases with magic materials not stacking with each other in the player inventory.
* Fixed Auto Mead not applying correctly.
* Fixed error handling for Multishot magic effects.
* Fixed modify low health threshold magic effect checking the wrong effect value.
* Reduced riches value for balanced and minimal configuration options.
* Fixed an issue with loot rolling creating too many items.
* Fixed items that cannot be runified showing up in the rune UI menus.
* Fixed an issue with magic items not initializing correctly after the 11.7 update (attempt to fix another bug made more, fun times).
* Fixed particle effects on dropped Andvaranaut.

## Version 0.12.1

* Tweaked the Rune costs config values. 
  * If you used version 0.12.0 please delete your old BepInEx\config\EpicLoot\baseconfig and let them regenerate!
* Fixed Rune UI not enabling correctly on existing enchanting tables.
* Fixed an issue when adding multiple bosses to the same biome with the patch system.
* Fixed magic items not stacking when using the features from the Recycle & Reclaim mod.
* Fixed an issue with Rune configurations not syncing properly on a server.

## Version 0.12.0

* NEW Unidentified items:
    * Unidentified items are a new item type that can be dropped as normal loot.
    * Unidentified items must be identified at the Enchanting Table before they can be used.
    * Unidentified items can be sacrificed for their tier materials.
    * New configuration "Balance - Items Unidentified Drop Ratio".
* NEW Runestone Extraction and Etching features to the Enchanting Table:
    * Extraction allows you to store a singular enchantment from an item into the Runestone.
    * Runestone Etching allows you to apply the enchantment from the Runestone onto an item, overwriting an existing enchantment.
    * New configuration "Balance - Rune Extract Destroys Item"
* Added ability to edit the core configuration files without patch files.
  * Files are generated under BepInEx\config\EpicLoot\baseconfig with the current default configurations from the release.
  * **Note: You will have to delete these configurations to get any new updated values in future mod updates!!!!**
      * **New configuration "Debug - Always Refresh Core Configs" when set to true will allow overriding these files. Server owners you will probably want this on if you only use patches!**
* Added the overhaul configuration system which provides access to directly editable configuration. Three configuration defaults are available as base templates:
    * `Balanced` - The new default for Epic Loot. Enchantments are powerful, but not overpowered.
    * `Legendary` - Similar the old default of epicloot, enchantments are very powerful.
    * `Vanilla` - A configuration that is closer to the vanilla experience. Enchantments give you an edge, but are not as powerful.
    * This is set under the Balance - Balance Template in the configuration file.
* NEW enchantments added:
    * `DartingThoughts` - Converts a portion of max eitr into eitr regen (sal)
    * `AddCrafterskills` - Increases crafting and cooking skills (midnight)
    * `Headhunter` - Increases chance to drop trophies from creatures (midnight)
    * `ModifyBlockForce` - Increases knockback on shields (warp)
    * `ModifyNoise` - Reduces character noise (makes you more stealthy) (warp)
    * `ModifyBuildDistance` - Increases the range you can build at (warp)
    * `ModifyPickupRange` - Increases the range you can autopickup items (warp)
    * `ModifyFireRate` - Increases fire rate of ranged weapons (warp)
    * `ModifyProjectileSpeed` - Increases the speed of fired projectiles (warp)
    * `AdrenalineRush` - Dodging an attack provides a short burst of increased damage (Leslie)
    * `Apportation` - Teleports you to your thrown weapons location (Leslie)
    * `Chain Lightning` - Chance to cause chain lighting on hit (Leslie)
    * `OffsetAttack` - Third attack in a combo provides stagger immunity and a damage reduction for you, when timed (Leslie)
    * `Automead` - Automatically consumes mead when health critical (warp)
    * `InstantMead` - Meads consume instantly when health critical (warp)
    * `MeadCooldown` - Reduces mead cooldown (warp)
* NEW automatic drop system assignment for weapons, armor and other items. This removes the requirement to add patches for other modded items for them to appear in drop tables.
  * New configurations under General:
    * "Auto Add Equipment"
    * "Auto Remove Equipment Not Found"
    * "Only Add Equipment With Recipes"
    * "Auto Add Remove Equipment From Vendor"
    * "Auto Add Remove Equipment From Lootlists"
* NEW search functionality for Epicloot Compendium pages (Thanks Rusty!)
* NEW EpicLoot API is now available! (Thanks Rusty!)
    * Easily retrieve enchantments that are active on the player
    * Magic Effects can be added by API
    * Abilities can be added by API
    * Sets can be added by API
    * Material conversions
    * Recipes
    * Sacrifices
    * Treasure Maps
    * Bounties
    * Secret stash items
* Enchanting Table fixes:
    * Augment UI now has a scroll bar and can display all item enchantments.
    * Augmenting can no longer roll duplicates in one reroll.
    * Sacrifice items now allows multiple items to be selected at once.
* Magic Effect tweaks:
    * Riches drops can now be configured and only drops one item from this new loot table at a time.
    * Double and Triple Shot can now be configured and have been rebalanced.
    * Elemental and Physical Resistances now have a configurable capped amount.
    * Eitr Weave buffed significantly.
* New configuration AudioVolumeAdjustment to control audio volume of this mod.
* Scrolling UI sensitivity tweaks.
* Improved randomization of item drops by type and tier.
* Added extended magic effect descriptions to compendium.
* Reduced Augment Upgrade costs of Thunderstone and YmirRemains from 10 to 5. We felt it was still too high.
* Other bugs fixes we forgot to write down (sorry! enjoy!)

## Version 0.11.7

* Updates for the Russian translation file.
* Removed extended item data framework support (this has been deprecated for over two years now).
* Bug fix for item stands not working correctly with magic items.
* Added missing trinket information in tooltips.

## Version 0.11.6

* Removed the NoRoll from Modify Summon Damage and Modify Summon Health. Can now roll these effects.

## Version 0.11.5 - Call To Arms

* Call To Arms content update!
  * (if you are using the mega overhaul patches you will NOT get these changes!)
  * (This is why we encourage using and making small patches, the mod is not finished)
  * Reclassified Ghost as a Tier 2 mob.
  * Added Bear (Bjorn) as a Tier 3 mob.
  * Added Vile (Unbjorn) as a Tier 6 mob.
  * Added new weapons, armor, and trinkets to appropriate drop tables.
  * Added Demister to loot tables.
* Built in Localization for 35 languages! (If you have suggestions for improvements in your language please reach out).
* New configuration AudioVolumeAdjustment to control audio volume of this mod. (NOT finished! More fixes coming soon).
* Removed "Crafting Tab Style" configuration since it did nothing.
* Scrolling UI sensitivity tweaks (NOT finished! More fixes coming soon).
* Comparison Tooltip jitter fix.
* Updated links in the welcome popup message.
* Magic effect fixes:
  * Fixed the checks for elemental damage on weapons to include all types.
  * Fixed the check for items that use health on attack (now includes bloodlust enchanted weapons).
  * Bulk Up algorithm reworked to remove HP bonus cap and smoothed effect value curve.
  * Quick Draw should now work on crossbows.
  * Explosive Arrows reworked to improve compatibility with other mods.
  * Modify Summon Damage and Modify Summon Health now supported.
  * Coin Hoarder algorithm reworked to require more coins for larger bonuses.
  * Modify Attack Health bug fix for restoring correct value state after performing an attack.
  * Stagger no longer applies to players with PVP disabled.

## Version 0.11.4

* Fixed some patches not applying properly on servers (could use more user verification).
* Fully fixed an issue with gamble costs applying to multiple of the same item incorrectly.
* Removed duplicate recipes from the recipes.json that are already included in materialconversions.json.
    * This includes removing the default ability to craft trophies at the workbench. You can find them in the Convert Materials, Salvage Junk tab at the enchanting table.
* Added junk conversion recipes for trophies: Serpent, Leech, Stone Golem, Goblin Shaman, and Morgen.
* Adjusted some Enchanting Upgrade costs, added new ones for Ashlands (thanks Majestic for the ideas).
* Fixes to item tooltips:
    * Legendary/Mythic set items that are part other sets now both display.
    * Shields now display any applicable DamageModifiers.
* Added in world text to indicate when Feint was applied.
* Fixed some display issues with Enchanting Table unlocks/star levels.

## Version 0.11.3

* Fixed issue with loot rolling lowest tier when using fallbacks from iteminfo config.
* Fixed commands like "magicitemset" not dropping the correct items.
* Multijump reworked to make successive jumps feel like a normal jump from the ground.
* Fixed Andvaranaut not appearing.

## Version 0.11.2

* Fixed issue with loot rolling higher tiers than should have been allowed for loottable config.
* Small bug fixes for various null reference exception errors.
* Fixed tooltip display bug with gamble menu.
* Fixed recipes not being added for singleplayer (and hosts).

## Version 0.11.1

* Hotpatch to re-add deprecated Items field in iteminfo.json file. This field does nothing, please remove from your patch files.

## Version 0.11.0

* New external dependency for Jotunn. You must install this for the mod to function!
    * Added version requirement check such that server AND clients must have the mod installed and running for multiplayer.
* Changes to how json files are handled:
    * Improves installation process, all default json files are now embedded in the mod. You can no longer change them directly!
    * Localization files are now loadable from Bepinex/config/EpicLoot/localizations.
    * iteminfo.json file: New field "UngatedFallback" added to allow specifying fallback categories and failsafe fallback items separately.
    * Patch files are now live-reloadable from the Bepinex/config/EpicLoot/patches folder without restarting the server.
    * New patch action "MultiAdd" which allows patching the same values into multiple locations.
* Added a terminal command "printconfig" which allows logging any of the current configurations available. Must be an admin.
    * This allows inspected the result of patch operations and what configs a client has received from the server
* Bug Fixes:
    * Magic effect DoubleJump fixed to prevent infinite jumping.
    * Gambling system reworked to allow better, more random, item selection.
    * Fixed UI tooltip spasm when too large for the screen, now resizes and included side-by side placement for comparison tooltip.
    * Enchanting table UI now has the correct text for initial level and unlock.
    * Fixed Augment UI having infinity symbols when used with AzuCraftyBoxes mod.
    * Block armor and parry armor no longer renamed to legacy values in the tooltip.
    * "lucktest" terminal command reworked, provides examples.
    * Enchanting Table should no longer drop extra materials when destroyed in some cases.
    * Movement speed bonuses no longer cause stamina cost increases.
    * Fixes misspelling of augmenting in the upgrade menu.
* Optimizations to some Art assets (download size reduced by 50%).
* Optimizations to how loot beams work (Up to a 20x improvement in performance).
* 3 New magic effects with new translations available (see Github for these new translation keys):
    * EitrWeave: parry blocks restore a little eitr
    * BulkUp: Converts part (up to 100%) of your health regen into bonus health
    * Spellsword: Adds an eitr cost to the weapon, increases damage
* Bounty and treasure chest spawning overhauled:
    * Spawn locations are now pre-selected and cached upon interacting with the merchant, decreasing wait times for accepting bounties and treasure maps.
    * Deprecated use of IncreaseRadiusCount and RadiusInterval algorithm parameters for selecting suitable locations. (To be reviewed, may add back)
    * Bounty spawning validation, bounties for creatures that don't exist in the world will not be generated.
    * Bounties and treasure chests should no longer spawn inside of rocks, underground or deep underwater.
    * Bounties and treasure for the Ashland and deep north should work more consistently.
* Added CheatSword as a requirement to place the decorative pieces (piece_enchanter, piece_augmenter) to hide them by default.
* Augment symbol changed to a filled in triangle rather than an empty diamond.
* Bounties now ping when using the Andvaranaut until they take first damage.

## Version 0.10.7

* Update for game version 0.220.3.
* Upgraded unity project to version 2022.3.50.
* Added additional null check for ModifyDamage_ItemData_GetDamage_Patch.
* Corrected panel lighting for the enchanting table to match other UI elements.
* Added checks to interval settings to prevent divide by zero errors.
* Updated ServerSync to version 1.18.
* New external dependency for JsonDotNET 13.0.3. You must install this for the mod to function!

## Version 0.10.6

* Fixed Coin Hoarder not working unless on a weapon.
* Fixed ranged damage from non-players not applying correctly.

## Version 0.10.5

* Fixed magiceffects ItemUsesStaminaOnAttack, ItemUsesEitrOnAttack, ItemUsesHealthOnAttack, and ItemUsesDrawStaminaOnAttack not resolving correctly when set to true.
* Changed Warmth effect to only roll on Epic and above.

## Version 0.10.4

* New configuration GatedFreebuildMode to control when items are unlocked when using the free build enchantment. Defaults to BossKillUnlocksCurrentBiomePieces.
* DoubleMagicShot now uses twice the eitr plus tweaks to fix projectile amount on some staffs.
* TripleShot now uses thrice the ammo and is less accurate at longer distances.
* 16 New Magic Effects with new translations available (see Github for these new translation keys):
  * Bloodlust
  * CoinHoarder
  * DecreaseForsakenCooldown
  * EitrLeech
  * IncreaseHeatResistance
  * IncreaseMiningDrop
  * IncreaseTreeDrop
  * ModifyAttackEitrUse
  * ModifyAttackHealthUse
  * ModifyDodgeStaminaUse
  * ModifyDrawStaminaUse
  * ModifyLowHealth: Raises the health critical threshold
  * ModifySummonDamage (Temporarily disabled for bug fixes)
  * ModifySummonHealth (Temporarily disabled for bug fixes)
  * ModifyWispRange
  * Warmth: Prevents the cold and freezing debuffs
* Additional new translations available:
  * mod_epicloot_itemtooltip_rarity
  * mod_epicloot_itemtooltip_effects
* Updated mod_epicloot_me_doublemagicshot_desc translation
* Added feature to allow some requirements in the MagicItemEffects json accept a value of false (such as ItemUsesEitrOnAttack).
* Removed Bows from ModifyAttackStaminaUse and ModifyAttackSpeed (It does not actually work).

## Version 0.10.3

* Hot fix for Sacrificing item selecting the wrong item in the inventory.
* Increased minimum required version to 10.0.

## Version 0.10.2

* Set fallback gambling Coins from 1 to 10000 when misconfigured.
* Tweaked default fallback items for the item info configuration.
* Set default maximum radius for Ashlands adventure spawns to 8000, should help with performance issues when spawning bounties and treasure chests.
* Bounties and treasure chest should no longer spawn in lava.
* Treasure chests now delete themselves (like tombstones) after fully looted.
* Tweaked code for buying treasure chests to prevent accidental multiple purchase.
* Added support for the nocost cheat for Epic Loot trader menu.
* Epic Loot trader menu now allows you to buy items (and do other interactions) with a full inventory. Items will drop at the player's feet.
* Reworked gating logic to fix bugs with some items dropping when not allowed.
* Simplified double and triple shot checks to improve other mod compatibilities.
* Reworked inventory management (checks, additions, removal) into a new class EpicLoot_UnityLib.InventoryManagement to improve other mod compatibilities.
* Re-enabled gotomerchant command to teleport the player again.
* Set the default value of the "Config Sync - Lock Config" configuration to true.

## Version 0.10.1

* Fixed internal mod version and updated logo.

## Version 0.10.0 - Ashlands & Mythic Rarity

* Bug fixes to catch various reported exceptions.
* Added check to ensure chest is empty before self-destruct on treasure chests.
* Additions for the new Ashlands content.
* Additions for missing Hildir content.
* Added Mythic rarity! New translations available:
  * $mod_epicloot_mythic
  * $mod_epicloot_mythicsetlabel (not used yet)
  * $mod_epicloot_basicmythicnameformat
* Rebalanced the mod to account for Mythic rarity and to be more vanilla friendly.
* Added missing crossbow skill: 4 new translation keys available:
  * $mod_epicloot_me_addcrossbowsskill_display
  * $mod_epicloot_me_addcrossbowsskill_desc
  * $mod_epicloot_me_addcrossbowsskill_prefix1
  * $mod_epicloot_me_addcrossbowsskill_suffix1
* Added missing fishing skill: 4 new translation keys available:
  * $mod_epicloot_me_addfishingskill_display
  * $mod_epicloot_me_addfishingskill_desc
  * $mod_epicloot_me_addfishingskill_prefix1
  * $mod_epicloot_me_addfishingskill_suffix1
* Added fishing rod support to enchanting system
* Added new magic effect requirement option: MustHaveEffectTypes, to only apply when any of the given effects are present on the item.
  * This is used to prevent recall from applying to weapons that are not throwable.

<details>
<summary><b>Changelog History</b> (<i>click to expand</i>)</summary>

## Version 0.9.38 - Various Bug Fixes
* Fix enchanting table not displaying correctly the first time it is accessed.
* Fix for Modify health critical effect not applying correctly.
* Changed attack speed to apply as a multiplier rather than additive since animation base speeds is not always 1.
* Reworked Bounty system, upgraded file format. NOT backwards compatible.
    * Reverting from this version your server bounty ledger will not load and all players with unrecorded bounty kills from other players will be lost.
    * Removes bounty data from global keys storage, fully upgrading to the external file save system.
* Added self-destruct to treasure chests that have been found upon reloading them.
* Minimap should now handle removing adventure map pins when abandoned or when resetting via console commands.
* Decreased maximum minimap offset by 20% so that treasure chests and bounties are better spawned within the red map circle.

## Version 0.9.37 - Auga Tooltip Bug Fix
* Auga Tooltip now populates correctly in the Crafting and Augmenting actions.

## Version 0.9.36 - Valheim Enchantment System Compatibility
* Added compatibility with Valheim Enchantment System
* Fixed bug that would occur if there was no rarity table in the loottable entry.
* Added defensive coding around loot rolling to ensure no errors would occur.
* Refactored Auga's EpicLoot tooltips for showing Magic Items.

## Version 0.9.35 - Various Clean Up
* Added additional logging and break prevention for loot rolls
    * This is to prevent loot roller from breaking because of bad patches.
* Added additional logging and break prevention for patch files directory.
    * This is to prevent bad BepInEx installs from crashing Epic Loot fully.
    * Still need to ensure the BepInEx for Valheim is used from Thunderstore.
* Added 4 new config settings that control additional items that bosses drop and to allow customization to drops in the same way that Trophies do.
    * Crypt Key Drop Mode
    * Crypt Key Drop Player Range
    * Wishbone Drop Mode
    * Wishbone Drop Player Range

## Version 0.9.34 - Valheim Update 0.217.27
* Updated for 0.217.27 References
* Updated Unity for 2022.3.9
* Added Mac/Linux Support
    * OpenGLCore and Metal Support are now bundled
* Added Brenna's Trophy to Sacrifice List
* Added in AdventureBackpacks API

## Version 0.9.33 - BepInEx 5.4.2201 Preparation
* The removal of the doorstop corlib search path presented a dependency issue for EpicLoot
  * Fixed the dependency issue by including missing DLL.

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