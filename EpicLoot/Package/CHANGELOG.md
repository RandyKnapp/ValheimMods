**0.15.2**
* Other mods can now read Epic Loot's tooltip information, change it, or draw their own item tooltip instead.
    * The log names any mod that takes over drawing the item tooltip.
* Makes the shortcut to open shard slots configurable
    * Fixes newer BowsBeforeHoes quivers never letting you open their shard slots
* Adds a Vietnamese translation, selectable from the game's language setting.
* Augment choices are a more even text size, and long effects wrap onto two lines instead of being cut off.

**0.15.1**
* Fix storage tab not showing up when the enchanting table is upgraded to include it

**0.15.0**

Changes:
* Sets overhaul!
    * Sets can be configured to drop at any rarity, and set pieces of different rarities count toward the same set
    * The rune feature can now extract and Etch sets from and to items
    * Set visual effects keep showing while a set weapon is stowed on your back, but the set bonuses still need it in hand.
    * Set bonuses that need a key press are marked Activated, show their key in their description, and have a gamepad button by default.
* Adds Quick Configure, editable in game configs for most Epicloot options, and a 1 time setup tutorial
    * Server admins can change every Quick Configure setting from their own game, and the server passes the changes on to every player.
* Improvements to the loot system (existing loot tables can be upgraded)
    * new loot table format, with more control over what drops and how much
    * Modded creatures are now automatically included in the loot tables
* All Boss drops are now configuable, and the loot table changes are non-destructive
* Eikthyr and Black Forest creatures and chests no longer drop wooden shields, and troll armor is now part of the regular Black Forest armor loot.
* Slotting runestones into shard slots can be turned off, which leaves runestones already slotted inactive but still removable.
* Runestones that repeat an effect already on an item can be set to count for less with each copy.
* The enchanting table's search boxes can now find items by their enchantments.
* The enchanting table can store enchanting materials: the new Storage feature holds materials. Existing configs need Storage added to Table Features Active to see it.
* Rare magic effects are marked with stars and a shimmer in tooltips and at the enchanting table.
* Tempering
    * Tempering now costs a single iron or gold bounty token per attempt instead of materials.
    * A failed temper no longer lowers an enchantment unless the server turns that on, and then only the enchantment being tempered can drop.
    * The temper panel shows the chance to downgrade
* Bounties now pay double the coins and bounty tokens.
* Other mods can now read a player's active bounties, so they can show them to friends.
* Works with Item Favorite Framework when it is installed: favorited items are kept out of enchanting table screens that use items up and are marked with a star.
* New Enchantments!
    * Prosperity gives a chance for additional loot drops from creatures and chests
    * Green Thumb on the Cultivator makes the plants you sow grow faster.
    * Close Quarter on melee weapons reduces how far your hits knock enemies back.
    * Life After Kill on weapons heals you a set amount each time you kill an enemy.
    * The grappling hook gets its own enchantments, Swift Reel and Long Line, and is treated as a tool rather than a weapon for loot and damage effects.

Bugfixes:
* Loot tables can now give items as many enchantments as there are to roll.
* Item tooltips now fit their content and stay beside the cursor, and the comparison tooltip follows the hovered item.
* The effect lists on the Augment and Rune tabs now use one text size and show long effects in full.
* The trader and tempering panels only move while a key is held or by their corner handle, so clicking them no longer nudges them.
* Storage mods can now let the enchanting table work on items directly in their chests.
* Lox armor is now sorted into Plains loot.
* Materials refunded from a destroyed enchanting table follow the game's latest reset of cheat flags.
* Ashlands elites no longer drop Deep North shardstones.
* Brokkr's Gift drops are no longer swapped for shardstones or sacrifice materials.
* Less stutter around chests, and when first opening the enchanting table or one of its tabs.
* Fixes the Dvergr rogues' crossbows showing up as a second Arbalest in loot, the merchant and item gating.
* Fixes Stagger Duration shortening staggers on most creatures and not working from projectiles in multiplayer
* Fixes bounty creatures losing their extra health after a reload or when another player's game took them over.
* Fixes bounty rewards claimed with a full inventory dropping a single coin or token instead of the full reward.
* Fixes several set bonus problems, including duplicate pieces counting twice
* Fixes disenchanting and re-enchanting an item with a durability bonus raising its durability without limit
* Fixes magic staff tooltips showing a wildly wrong health cost and leaving out the eitr cost.
* Fixes Double Magic Shot costing more eitr than described, fires a normal shot instead of wasting eitr on a failed cast
* Fixes panel text going missing when another mod ships a broken copy of Valheim's fonts
* Fixes the rested carry weight shardstone losing its comfort bonus
* Fixes the Dark Red shardstone's ranged skills bonus doing nothing
* The Light Green shardstone's resting health regeneration now only works while you are actively resting, not for as long as you are Rested.
* Fixes Bulk Up being able to push health regeneration below zero
* Fixes Apportation never moving you to your thrown weapon
* Fixes a flood of sound warnings when travelling through a portal with TargetPortal.
* Lucky Craft now only saves materials when crafting, not when building, planting or tempering
* Quick Draw no longer makes crossbows reload slower at high skill, and Triple Shot from a set only affects bows and crossbows
* Coin Weighted Weapon now shows the damage bonus it actually gives at the coins you carry, and weapon tooltips include it before the weapon is equipped
* Coin Weighted Weapon's bonus now rises smoothly with no sudden jump, higher rolls always give more
* Shard slots on backpacks and quivers now open with Ctrl held instead of Alt, so other mods' Alt hotkeys no longer get in the way, and players can choose to need it for every item.
* Fixes an error when an area attack hit a creature that had just died, which could stop the attack hitting the rest of the group, and enchantments no longer trigger on such hits.
* Fixes the Leather Belt and the craftable rings vanishing when dropped on a dedicated server.
* Fixes Modify Noise making you slightly louder instead of quieter.
* Riches now gives treasure more often the stronger it is, instead of at the same rate at any strength.
* Luck, Riches and Lucky Loot now count in full and update as soon as you socket, unsocket or augment a worn item.
* Lucky Loot no longer multiplies creatures dropped by others, such as elite blobs, and stays within its limit per kill.
* Rune extraction power now also applies to small enchantment values.
* Enchantments that rule each other out can no longer end up together through augmenting, rune etching or slotting runestones and shardstones.
* Replacing an enchantment such as Throwable now treats it as gone, so it can no longer bring in Recall without Throwable or block choices that only clashed with it.
* Item tooltips now show the weight of the stack you hold instead of a full stack.
* Parry bonuses no longer apply to early blocks with shields that cannot parry, such as tower shields.
* A refused Air Jump no longer uses up a jump, and the stamina check now uses your real jump cost.
* Fixes an error that could cut short area damage from enchantments when a hit spawned a creature, such as bats.
* Bountiful Harvest now works on every plant in multiplayer, not only those your own game controls.
* Meads Cooldown now works together with Instant Mead and Potion Efficacy.
* Indestructible armor no longer wears down unseen and shows as broken once the enchantment is gone.
* Triple Shot and Double Magic Shot now lower each projectile's whole damage, including weapon upgrades and arrows, as described.
* Enchanting table upgrades that get no answer, for example because the table's owner left, now refund their cost.
* The health bar grows more slowly at high health and no longer runs off the top of the screen.
* Item gating no longer loses items when the same boss is listed more than once for an item type.
* Fixes slotted shardstone tooltips showing a roll range, unknown enchantments showing blank lines, and blank names or icons in the crafting result window.
* Set bonuses now read as one short line each, and holding Shift (or the left trigger on a gamepad) shows what each one does in full.
* Stamina on Kill and Everflow now trigger once per kill, not once for every hit that lands as a creature dies.

New Sets:
- Wrath of the Thunderer set, whose full set lets you hurl your melee weapon, even mid-swing, to slam down where you aim.
- Trickster's Guile set, whose full set takes you behind an enemy you keep your aim on for a heavy strike.
- Dverger Arsenal set, whose full set turns your crossbow bolts into lobbed fireballs that explode where you aim.
- Egil's Mark set, whose full set makes your arrows fly faster and hit harder the further they travel, at the cost of a much slower draw.
- Rime of Élivágar set for mages, whose full set surrounds you with drifting snow and freezes the water beneath you so you can walk across it for a steady cost of Eitr.
- Fylgja's Bond set for rangers, whose full set binds a tamed creature as a spirit animal that fights beside you.
- Freyja's Falcon set for spear fighters, whose full set gives you dark wings and lets you dive from any height to slam the ground, at full force from high enough up.
- Bölþorn's Bane set, whose full set sends your crossbow bolts through every creature in their path.
- Heiðr's Seiðr set for mages who use a bow, which trains Elemental Magic with your bow and, as a full set, fires arrows conjured from Eitr.
- Mímir's Well set for mages, which trades health for a deep pool of Eitr and, as a full set, wraps you in a barrier that takes part of every hit from your Eitr before your armor.
- Einherjar set for spellswords, whose full set pours your Eitr into your weapon for a spell of extra damage.
- Ullr's Hunt set for archers, whose full set lets your arrows mark creatures for a heavy axe or polearm hit.
- Hel's Covenant set for blood mages, whose full set raises creatures slain by you or your summons as thralls of the same star level that fight for you.
- Huldufólk set for rogues, which lets you vanish in smoke when you dodge near enemies and, as a full set, makes your sneak attacks far deadlier.
- Surtr's Brand set for two-handed axes and swords, whose full set makes your combo finishers and secondary attacks erupt in Brenna's fire.
- Hel's Court armor set for blood fighters, whose full set drains enemies so they cannot heal while every player striking them heals.
- Hrungnir's Stand set for tower shields, whose full set stores charge from your blocks and releases a rock shockwave on the next one.
- Svalinn's Arc set for bucklers and round shields, whose full set throws your shield through enemies and back, hitting harder the more damage you have blocked.
- Coils of the World Serpent set for spear fighters, which turns part of your physical damage into venom that ignores resistance and, as a full set, dooms heavily envenomed enemies to walk nine steps and fall dead.
- Gullveig's Rebirth set for elemental mages, whose full set makes you a Hexen for as long as you wear it: a Jotun Witch's body, magic blast and lightning bolt, and flight that burns Eitr and sinks you to the ground when it runs dry.
- Jotunn's Blood set for two-handed fighters, whose bonuses make you larger, stronger and able to carry more, at the cost of a growing weakness to fire.



**0.14.13**
* Adds a repair system for bounties/treasure which had not previous spawned, due to 1.0 save migration
* Fix for rune etching being free
    * The rune tab's effect list is now locked during the countdown
* Tempering Fixes 
    * Fix for tempering an item without paying for it
    * Fix for effects on unique legendaries and mythics not being temperable at all
    * A failed temper no longer raises an effect that was below its rarity's minimum up to the minimum
* Fix for identifying items consuming them without giving anything back
* Fix for identifying several items at once handing back more items than were identified
* The identify preview only lists items progression gating will actually hand out
* In the "Player Must Have Crafted Item" drop mode, identifying no longer caps items at the biome of the last boss killed
* Fix for extracting the last enchantment from a socketed item destroying its sockets and the shardstones in them
* Extracting an enchantment no longer resets the item's augment history (which reset the rising re-augment cost), and augmented/tempered markers stay on the effects that earned them
* Sacrifice, identify, rune etching and rune extraction only pay out for items they actually removed (no cost when cancelled, no gain either)
* Unknown magic effects (a typo in legendaries.json, a removed mod) are skipped instead of being given made-up value ranges, and the missing-definition warning is logged once
* Hold-Shift ranges, temper ranges and the augment and rune lists now show the correct range for effects on unique items
* Blood Block's self-damage no longer triggers any effect
* Forest's Aid and Icy Retribution now need an attacker; falls, lava and drowning no longer trigger them
* Damage dealt by effects themselves is no longer treated as a weapon strike
    * Reflect, Eikthyr's charge, Frost AOE, Trailblazer, the meteor, Icy Retribution, Corpse Rot and chain lightning no longer crit, double, pay Wager or Mercenary per target, lifesteal, execute, Slow, Paralyze or build Eikthyr charges
* Fix for Reflect and other effects reading a creature's attack as your weapon whenever your game controlled the attacking creature
* Arrows and thrown weapons now use the effects of the weapon that fired them
* Executioner is only used up by a hit that carries Executioner
* Fix for Wager refunding its stake on hits that did not kill (single player and hosts); Stamina on Kill and Queen's Everflow used the same check
* Fix for Triple Shot and Double Magic Shot able to leave every weapon of that kind at reduced damage until restart, if an error happened mid-shot
* Fix for Explosive Arrows' explosion hurting players without PvP enabled
* The parry bonus in shield tooltips now shows the real multiplier
* Fix for Immovable blocks failing silently once the stagger bar filled
* Fix for blocked hits being converted to elemental damage twice
* Eitr Shield and Coinplated now only pay for damage that gets past the Staff of Protection bubble, blocking, resistances and armor
* Automatic Mead now judges the damage that will actually land, drinks at most one mead per hit, and works with Instant Mead
* Skill enchantments now count wherever the game reads the skill level
* Skill enchantments now add weapon damage past skill 100


**0.14.12**
* Fixes a bug with the new save system which would cause Bounties to get lost

**0.14.11**
* Gamepad support for the enchanting table (Thanks ksavila!)
* Gamepad support for the merchant panel (Thanks ksavila!)
* The Compendium's magic pages now open when navigated to with a gamepad, and the right stick scrolls them
* The bounty and treasure map pin filters moved into the vanilla map's pin filter panel, beside the boss and death filters
    * They toggle the same way the vanilla filters do and can be reached with the d-pad, so they no longer take the gamepad triggers
    * Selecting one no longer lets you place your own map pins with the bounty or treasure icon
* Fix for cancelling a sacrifice or identify not stopping it
    * The countdown kept running after the cancel, so the items were consumed anyway
* Fix for the Rune tab's action button reading "Apply Rune" after every finished action, even in Extract mode
* Deep North trophies can now be sacrificed
* Items auto-added from other mods now recognize the Deep North materials, so gear crafted from them is gated behind the Biome boss
* Dark Green shardstones on melee weapons now imbue 4/5/6/7/8/9% poison damage (Magic to Ancient), down from 10/20/30/40/50/60%
* Translations updated for all 34 non-English languages

**0.14.10**
* Fix for items losing their enchantment when taken off an armor stand
    * The enchantment data was still on the item, but nothing re-read it, so the item behaved as if it were mundane until it was dropped on the ground again
* Fix for the trader window not opening at all on some clients
    * Vanilla closes the store on the same Use press that opened it whenever the game happens to run Player's update before StoreGui's, so the window was created and destroyed in one frame, drawing nothing and logging nothing
    * The store now ignores that one press, the same way the inventory window already does
* Fixes for the bounty ledger which could reset when the server was restarted
    * This could result in bounties being marked incomplete after being completed (but not turned in before a server restart)   
    * This could also result in bounties not being properly marked as completed if the owner was offline during the kill, and before a server restart
* Fixes for not persisting bounty or treasuremap data to the character 
* Fix for magic effect values being permanently reset to 1 when an item was reloaded
    * An effect was treated as valueless (a yes/no grant like Indestructible) whenever the loaded magiceffects.json had no value block for that item's rarity, and the reset was written straight back to the item
    * This hit Ancient items on servers whose magiceffects.json predates the Ancient rarity, since a server replaces the client's enchantment definitions on connect
    * Only effects with no values at any rarity are normalized now; a missing rarity block logs a warning naming the effect and rarity instead of destroying the rolled value
* Boss shardstones now show what their upgrade does
    * Shocking Charge, Forest's Aid, Corpse Rot, Icy Retribution, Queen's Everflow and Necrotic Fire had no number in their tooltip at all, so every rarity read identically
    * All 35 languages carry the new numbersw

**0.14.9**
* Fix for Grappling hook
* Trader window diagnostics, please report your log if you have issues opening the trader window
* Another fix for Joystick controller issues

**0.14.8**
* Controller update for 9/17
* Increases placement allowance for bounties/treasures
* Removes bounty/treasure relocation due to the potential for schisms

**0.14.7**
* Recompiled against current game build (fixes building destruction issues)


**0.14.6**
* Destroying an enchanting table now refunds the full upgrade cost of every unlocked feature. Amounts above an item's stack size (Surtling Cores, berries, mushrooms, fish) were previously lost
* Optimizes bounty and treasure chests spawning, now spawns the target considerably earlier
* Fixes Epicloot trader UI not showing up when other mods break the trader (your other mods are still breaking your game, just not breaking Epicloot)

**0.14.5**
* Additional safety checks for world upgrades
* Fix for edge cases that could cause the trader UI to not show up
* Fix for existing loot pools that already contain invalid items


**0.14.4**
* Adds safety checks for data lookup issues
* Prevents the item auto-sorter from adding new hidden, unused items to the loot pool

**0.14.3**

* Improves config synchronization refresh from disk on dedicated Linux servers, now also for the baseconfig json files (magiceffects, loottables, ...): an edit is applied once the file stops changing, and is picked up even when the server never sees a file event (NFS, SFTP, container mounts)
* A config reloaded on a player-hosted game is now pushed to the connected players, not only from a dedicated server
* A client connected to a server no longer applies edits to its own baseconfig files until it disconnects, at which point its own files are restored
* Config reloads and server config pushes now log at the default log level
* Fixed magicmats spawning broken items (reminder magicmats is a cheat command)
* Delays main menu welcome message and config prompt until after cutscenes have played

**0.14.2**

* Fixes biome explored not being updated on Deep North
* Fixes audio issue with enchanting table not being volume adjusted
* Added Deep North bounty targets

**0.14.1**

* Deep North support
    * Deep North creatures drop magic items
    * Deep North gear is categorized
* Epic Loot console commands are no longer flagged as cheats, but still require admin


**0.14.0**

Changes:
* New **Ancient** rarity
    * Every value-based magic effect and shardstone effect gains an Ancient tier
    * Ashlands creatures, treasure chests and Fader roll a small Ancient chance
    * Every enchanting table feature gains a seventh level

Bugfixes:
* Haldor's stock no longer rerolls every time you interact with him (gambler's stock can be consumed when purchased, or infinitely purchasable)
* Swapping directly between two items that both carry the same exclusive shard category (boss or unique) is no longer refused.
* Augment UI now correctly re-highlights the selected enchant when augmenting
* Fixes LightningStrikes not hitting creatures that were too large

**0.13.5**

Changes:
* Custom biome support. Biomes added by other mods (Expand World Data and the like) can be declared in the new `biomedata.json`, which now owns biome progression order, and boss keys
    * Every `Biome` field in `adventuredata.json` and every biome key in `enchantcosts.json` takes a biome name (vanilla or from `biomedata.json`) or the numeric biome value
    * An unknown biome name is now reported and that entry skipped, instead of the whole config file being thrown away
    * Bounties, treasure maps, unidentified items and item gating all read biome order and boss keys from `biomedata.json`
    * `Bounties.Bosses` in `adventuredata.json` is deprecated
    * A biome with no boss keys is never gated
    * Unidentified items are created at startup for every custom biome in the local `biomedata.json` (ship the file with the modpack; a server push alone cannot add prefabs)
    * Under `BossKillUnlocksNextBiomeItems`, identifying an item no longer advances past the biome on the item's name
    * The identify panel says `(Progress-gated)` when gating will identify a selected item at a lower tier than its biome
    * New console commands: `biomes` lists the biome order with keys and status, `knowbiome <biome>` marks a biome discovered so custom-biome bounties can be tested
* `Tempering.CostsByRarity` in `adventuredata.json` now supports removing rarities from tempering entirely
    * A rarity left out of the block is no longer temperable at all, and its items are kept out of the temper panel
    * A rarity with an empty cost list is still temperable, for free
* Shardstone upgrades moved out of the Convert Materials tab's generic Upgrade list into their own Upgrade Shardstones mode
    * That mode only lists an upgrade whose source stone you actually have, instead of every color you hold
    * Sacrificing a shardstone now yields 2 dust, 2 reagents and 2 essence of its rarity
* Treasure chests and Bounties now resolve their spawn points much quicker
    * Both systems now inherently support custom biomes, world size changes and biome distribution changes

Bugfixes:
* Adds detection and support for custom item containers (such as backpacks, quivers)
    * Hotkeys to open sockets on inventory containing items like this automatically changes  
* Partially broken creatures used for bounties no longer crash the bounty system
* Treasure maps added through the API now search for positions like normal
* Compendium group headings are no longer unreadable the first time the compendium is opened in a session
* Fixes shards being disenchtantable


**0.13.4**

Changes:
* Shardstone effect tuning is now exposed to config
    * New `Global` block in `shardstones.json` for tunables shared by several shards
* Tempering costs are configurable per rarity through a `Tempering.CostsByRarity` block in `adventuredata.json`. A rarity left out keeps its built-in default
* The same effect declared on several shard slots with disagreeing `Config` blocks is reported rather than silently resolved

Bugfixes:
* Editing `shardstones.json` live, now properly updates effects
* Slow is clamped to a floor of 10%
* A tempering cost naming an item that cannot be resolved is warned about once and skipped
* Fixes Adrenaline Surge's "Seconds per 1%" tooltip not localizing
* A patch edit now reloads the rebuilt configs into the running game
* The patch file watcher now covers subdirectories, so patches shipped in `patches/<ModName>/` are watched
* Patch files that changed but rebuilt nothing now say so in the log instead of failing silently
* A baseconfig file that deserializes to null is rejected and the currently loaded config is kept

Performance:
* The movement-penalty measurement is cached per physics step
* Magic effect value lookups hold on to the last player's table and no longer allocate a delegate per call on the per-tick paths
* Every shard effect sitting on a hot vanilla method bails on a zero effect value before doing any other work
* The comparison tooltip only walks the player's equipment while Ctrl is actually held


**0.13.3**

New Content:
* Brokkr's Gift
    * A rare artifact that can add shard slots to existing equipment
    * Drops rarely from Dvergr town, Charred fortress and Ashlands stone chests, and from Mistlands and Ashlands treasure map chests
    * Configurable success and number of slots added
* Shardstone Compendium page
    * Built live from your shard config

Changes:
* Localization for all languages is now available for shardstones and shardstone features
* Improves compatibility with Equipment and Quickslots. The slots it adds now carry Epic Loot's rarity background and equipped frame like the rest of the inventory
* The Golden shardstone's shield slot grants Lucky Block correctly
* `cheatsockets` now clamps to the maximum socket count instead of building an absurd socket row

Bugfixes:
* Terminal commands now auto-complete every entry 
* Opportunist and the melee stagger duration bonus now actually apply.
* Head Hunter now works across players (similar to riches)
* Summon damage was rewritten, it incorrectly impacted other non-summoned creatures
* Enchanting table feature levels are now written only by the table's owner
* Server-pushed configs that fail to parse are ignored with an error, keeping whatever is currently loaded.
* An enchant or rune cost naming an item that does not exist now logs a warning instead of throwing
* A `baseconfig` file containing `null` or nothing at all falls back to the embedded default
* Config file watchers now notice files being created, deleted or renamed, which also covers editors that save by writing a temp file and renaming it
* Deleting the last patch that targeted a config file now rebuilds that file from the default, instead of leaving the stale patched output in place forever
* One malformed patch no longer takes out the rest of the patch system. A bad patch, patch file or patch directory is reported and skipped individually
* `MultiAdd` patches validate their inputs, `InsertBefore`/`InsertAfter` work on object members, and a patch may name its target as `loottables.json` as well as `loottables`
* The enchantable item scan no longer re-runs on every world join.
* A stale or incomplete `shardstones.json` now skips the affected shard with an explanatory error
* The bounty ledger is now per world. A static path captured the first world loaded, so hosting a second world in the same session read and wrote the first world's ledger
* The ledger retries if the zone system is not ready yet. Bailing permanently left it empty for the whole session, silently dropping every offline player's bounty kill
* Accepting a bounty that the save data refuses no longer spawns creatures with no save record -- impossible to complete and never despawned
* The beacon on a bounty target clears when it takes damage again. The patch was only reaching `Character.OnDamaged`, which real bounty targets never call
* Darting Thoughts math corrected and tooltip now displays the actual values
* Ammo Conservation no longer grants a phantom instant reload to bows, and no longer cancels the reload tick outright
* The Slow attack speed hook is registered once instead of on every world load, which was compounding the slow multiplier within a session
* Executioner and the stagger damage multiplier no longer reuse a stale multiplier. Executioner applied to only the first target of an area projectile, and a melee stagger multiplier was silently reused for every later hit
* Multishot decides its proc before ammo is spent, so the extra ammo cost is charged for the shot it belongs to, and the effect no longer leaks into the next attack with a different weapon
* Quick Draw no longer applies to Draugr and Fuling archers, and a zero-value roll no longer overwrites the game's own crossbow reload timing
* Double Jump resets on ground contact, so walking off a ledge after an air jump no longer refuses the next one
* Eitr Weaving only triggers on an actual block, rather than on any attack aimed at you
* The low-health health regen bonus adds to the base bonus instead of overwriting it
* Corpse Rot now has a cooldown
* Potion duration bonuses no longer compound every time you re-drink the same potion
* Fixed a crash in Apportation when the hit is known only by its collider
* Legendary leg texture overrides were being written into the chest texture slot
* A fully upgraded table no longer *creates* bounty tokens on each disenchant
* The Convert tab re-checks that you can afford the cost when you press the button, matching the other tabs
* Tempering no longer destroys every socketed shard and runestone on the item, on both outcomes
* Tempering's gamepad navigation no longer walks off the end of the list on items that have untemperable effects, and an effect can no longer be recorded as tempered twice
* A temper requirement whose item no longer exists renders as an empty row and counts as unaffordable, rather than crashing the panel or making tempering free
* The enchanting table and rune panels close cleanly if you die with them open, instead of erroring every frame over the death screen
* The rarity background no longer covers up the equipped marker, so a worn magic item is distinguishable from one in your bag.
* `lootres` reports a clear error for an unknown table, an empty level, or an out-of-range index instead of crashing

API:
* Every API entry point that parses JSON now catches parse failures and reports them through `OnError` instead of throwing into the calling plugin
* `AddAbilityProxy` rejects duplicate ability IDs, and a registered proxy now survives a config reload or a server config push -- its definition used to vanish on the first reload
* Fixed the cross-mod `ItemInfo` bridge: its accessors discarded the result of every call and returned null or false regardless of what the other mod did
* New `API.ChangeReason.AddSocket`, raised when Brokkr's Gift adds shard slots to an item
* `API.ApplyMagicItemBackground` now also restyles the slot's equipped marker, so slots added by other mods (Equipment and Quickslots) match the rest of the inventory

**0.13.2**
* Fixes Loot generation recursively happening with certain mods
* Fixes terrain generation memory leak, noticable with excessive bounties

**0.13.1**
* Fix uploaded version

**0.13.0**

New Content:
* Shardstones Added! Shardstones are a new item that can be attached to weapons with available shardsockets
    * Thanks Leslie, Warp and Rusty for contributing effects 
    * Interacting with an enchanted item that has a shard socket will pull up a small storage UI, allowing you to socket or remove shards. 
    * Shardstones can be found in the world as loot, different shards in each biome
    * Every boss drops a shardstone of its own (Eikthyr, The Elder, Bonemass, Moder, Yagluth, The Queen and Fader), and two unique shardstones drop rarely from elite creatures and treasure chests
    * 90+ new magic effects have been added and are primarily available through shardstones
    * The enchanting table's probability panel now lists shard slot odds alongside effect count odds
    * `Shard Stack Mode` option controls whether several shards of the same color may be socketed into the same item
    * `Shard Removal Mode` option controls whether a socketed shard can be freely removed, must be broken out (destroying it), or is permanent
    * `Shard Stone Drop Ratio` balance option weighs shardstone drops against normal item, unidentified and material drops
* Tempering (Thanks Rusty!)
    * Tempering allows you to pay to increase the value of an existing enchantment (service provided by Hildir)
    * Tempering can be configured to allow upgrading past a tiers normal maximum.
    * Careful! Tempering can downgrade enchantments too if you critically fail (its configurable don't worry).
* Runestone Balance options
    * Runestone extraction can now be configured with new options: KeepItem / ReduceEnchants / ReduceEnchantsAndRarity / DestroyItem
    * Runestones can also be socketed into shardsockets
* Improved Tooltips!
    * User-side configuration allows you to expand vertical or horizontally (or shrink) tooltips (scrollbar automatically used, as needed)
    * SHIFT when moused over an item will now show extended tooltip information, this includes more detailed descriptions, configurable effects and precise values for multiple parts of the effects
* Terminal Commands upgraded (Thanks Rusty!)
    * Terminal commands now have better completion and more options
    * Color based formatting for rarity loot table rolls!
* Config option to show computed roll chance for all effects when enchanting an item (disabled by default)
* Client side config options to personalize how large item tooltips are
* Base configs now keep themselves up to date
    * Epic Loot now tracks config file versions and will prompt you to update configs which have been customized
    * Configs you *have* edited are never overwritten on their own. If an update changes one of them, a prompt at the main menu offers to replace it, copying your version to `BepInEx\config\EpicLoot\baseconfig-backup` first. Declining is remembered per file, and you are only asked again if that config changes in a future update
    * Configs targeted by a patch file are unaffected, as they are already rebuilt from the current defaults every launch
    * Dedicated servers log the affected files instead of prompting. 
    * Enabling `Always Refresh Core Configs` activates the patch system and prevents config file prompts (configs are rebuilt every restart, and only changes defined in patches will be applied)

Changes:
* Enchanting shards (Shard Magic, Shard Rare, and so on -- not the new Shardstones) are now crafted at the enchanting table instead of the forge
* The Leather Belt, Silver Ring and Gold Ruby Ring are now configurable
* Enchanting table building resources are now easily configurable
    * Resources to build the enchanting table has been changed to 10 wood, 2 greydwarf eyes
* The Epic Loot config file has been regrouped and now reads in a deliberate order instead of alphabetically, in both the file and the configuration manager
    * Sections are numbered in the order they are meant to be read (`1 - General`, `2 - Balance`, `3 - Shardstones & Runes`, `4 - Enchanting Table`, `5 - Adventure`, `6 - Interface`, `7 - Item Colors`, `8 - Abilities`, `9 - Debug`) and the settings inside each one keep the order they were written in
    * The bounty and adventure settings gathered into Adventure, the crafting UI / tooltip / panel position settings into Interface, `Rune Extract Mode` into Shardstones & Runes, and logging into Debug
    * Every setting you had customized is carried over to its new home automatically; nothing resets to defaults
    * Each setting's description now states whether it is synced from the server
* New cheat commands `magicshards` and `cheatsockets`

Bugfixes:
* Fixes tooltip display for enchanted items health use percentage
* Fixes a bug with Ragnar's ice wave which would cause a player to swim upwards in the air (upon exit) if the effect was used in a dungeon
* Attack speed compatibility: when another mod already provides AnimationSpeedManager (embedded as a binary), Epic Loot now routes its attack-speed changes into that shared instance instead of running its own copy, so the two mods' attack-speed modifiers stack correctly instead of overwriting each other
* Merchant panels are now draggable, and save their position client side
* Config patches now honor the `Priority` field reliably and equal-priority patches are now applied in a deterministic order instead of depending on the filesystem's file listing order
    * When any config patches are active, Epic Loot now always logs a one-line summary of how many patches were applied (regardless of your logging settings) to make patched setups obvious in bug reports
* Adventure data (Treasure hunts, bounties etc) is now compressed and cleaned resulting in massive reductions in stored custom data
* Transfer Enchantments through crafting now works properly
* Chest loot is now rolled when it is opened, not when it is spawned, so that the loot is always appropriate for the player opening it. This primarily fixes mountain & swamp loot which would be loaded very early, but actually opened later.
* Filtering an enchanting table list no longer hides items from you while still acting on them. Previously, typing in the Sacrifice tab's filter and pressing Select All would sacrifice every sacrificeable item in your inventory, not just the filtered ones
    * Select All, the products preview and the action itself now only ever cover the rows the filter is showing
    * Items you selected before filtering keep their selection, so you can filter and select several times over and then clear the filter to act on everything you picked
    * The filter is also reapplied after each action and when switching between Sacrifice and Identify, so the list no longer shows a stale set of rows
* Equip effect visuals (auras such as `Glowing`) are now reconciled against your equipped magic gear after every equipment change, instead of being attached from the model-building path and removed from the unequip path
    * Items worn in slots added by other mods now get their aura, which previously never appeared at all
    * The spurious `Unequipped item (...) from player that had fx, but could not find fx (...)!` error is gone — a missing effect is now simply nothing to remove
    * An aura shared by two worn items survives unequipping either one, and is removed when the last one comes off
* Fixed the reset for helmet attachment never running, which let the helmet's texture overrides and item effects be applied to a beard, hair or back-slung weapon model instead
* Fixed legendary texture overrides on armor being resolved by item type rather than by the item actually being attached, which picked the wrong item when two worn items shared a type
    * This also makes legendary armor texture overrides render for other players, which previously never happened

API:
* The API now covers integration, not just content registration. Full reference and a migration table in `EpicLoot/docs/API.md`
    * `API.ApiVersion` / `GetApiVersion()` / `HasEndpoint(name)` let a mod check what the installed Epic Loot supports and degrade gracefully instead of throwing
    * The `EpicLootAPI` shim provides typed wrappers for everything below and can be bundled into your plugin with ILRepack
* Inventory providers — `API.RegisterInventoryProvider` lets container, backpack and stash mods feed the enchanting table
    * Epic Loot spends the player's own inventory first and only charges the shortfall to providers
    * Removal by item instance matches on reference, so magic data on the consumed item is preserved
* Equipment providers — `API.RegisterEquipmentProvider` lets extra-slot mods contribute equipped items
    * Contributed items count toward effect totals, legendary set bonuses, tooltips and shard socketing, and receive their equip effect visuals
    * `API.InvalidatePlayerEffectCache(player)` discards memoized effect totals and refreshes worn equip effect visuals when your slot contents change outside vanilla equip/unequip
* Sacrifice filters — `API.RegisterSacrificeFilter` lets a mod veto sacrificing an item, so gear equipped in slots Epic Loot cannot see is not destroyed by accident
* Custom item slots — `API.ApplyMagicItemBackground` draws the rarity background, set marker and equipped overlay on a slot your own mod renders
    * Epic Loot decorates the vanilla grid and hotkey bar with transpilers, so a mod that reimplements `InventoryGrid.UpdateGui` or `HotkeyBar.UpdateIcons` previously lost the background entirely; this is the supported way to put it back
* Lifecycle events — `API.AddMagicItemChangedListener`, `AddLootGeneratedListener` and `AddBountyCompletedListener`
    * The change event fires on every magic-data write with a reason token: `Enchant`, `Augment`, `Disenchant`, `Rune`, `Temper`, `Socket`, `Unsocket`, `LootRoll`, `Transfer`, or `Unspecified`. Item loading does not raise it
* Queries — `IsMagicItem`, `TryGetRarity` (non-throwing), rarity colors and display names, `IsEpicLootItem`, `IsShardStone`, `IsRunestone`, `IsMagicCraftingMaterial`, `IsUnidentified`, `CanBeMagicItem`, `ItemHasMagicEffect`, `GetAllMagicEffectTypes`, `GetEnchantCostsJson`, `GetSacrificeProductsJson`
* Loot generation — `TryMakeMagicItem` rolls and applies a magic item exactly the way a drop does, plus `RollMagicItemJson` / `ApplyMagicItemJson` to inspect before applying, `GetLuckFactor`, `RollEffectCountForRarity`, `GetLegendaryIDs` and `GetLegendaryInfoJson`
    * `API.AddLootTables` / `UpdateLootTables` register loot tables at runtime, and they are re-applied on config reload and on a dedicated server's config push
* New `magicapi` console command (`version`, `query`, `providers`, `events`, `roll`) for testing an integration in game
* Mod authors patching `PlayerExtensions.GetEquipment` should switch to an equipment provider: that method is a deprecated alias and nothing inside Epic Loot calls it
* `EpicLootAPI`'s `MagicItem` was updated to account for tempering, shardstones and unidentified items
* New magic effect requirement flags: `ItemGivesAdrenaline` (item's attack grants above-default adrenaline) and `ItemHasAdrenaline` (item provides a max adrenaline pool)
  * Note: `ItemGivesAdrenaline` requires that the item gives more than 1 adrenaline. This is due to all items by default adding 1 adrenaline.
* New magic effect requirements allowed from the API
    * API magic effect requirements when defined for an effect will be evaluated for all items, allowing only items which fit the filter (Thanks Warp!)
* `recipes.json` has been removed (its contained recipes are now handled and directly exposed in the Bepinex config file [Randyknapp.epicloot.cfg])
    * Mod authors: `API.AddRecipe`, `API.AddRecipes`, `API.UpdateRecipes` and the shim's `CustomRecipe` are now deprecated.


**0.12.15**

* Fixed an issue with bounties not spawning correctly if not using the Star Level System mod.
* Fixed an issue with bounty and treasure map pins not displaying until toggled.
* If you experienced loot not dropping from previous version upgrades (12.12, 12.13) please refresh your BepInEx\config\EpicLoot\baseconfig\loottables.json file to remove null entries.

**0.12.14**

* Fixed an issue with auto-adding loot tables causing mobs to not drop loot.
* All items from the game can now be added to loot tables from patches and work with the "Auto Add ..." configuration settings.

**0.12.13**

* Added back PlayerExtensions.GetEquipment for mods that patch that method.
  * If you are a mod author please remove your patches to this method! We refactored it to use vanilla methods in version 12.7.

**0.12.12**

* Bug fix for Mead Cooldown reducing all SE_Stats mead times, should now work as intended.
* Bug fix for Gambling loot tables not saving values correctly when auto-assigned causing large coin values. 
  * Needs a adventuredata.json configuration refresh to apply.
* Bug fix for Modify Armor effect showing up for non-armor applying item types.
* Bug fix for Modify Elemental Damage effect showing up on the wrong items.
* Bug fix for a tool tip display issue when items had a subtitle (trinkets).
* Bug fix for a tool tip display issue for set items not showing equipped correctly.
* Disabling adventure mode in the main configuration file will now also remove the minimap toggles.
* Star Level System API support.

**0.12.11**

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

**0.12.10**

* Fixed an issue with enchanting table identifying unknown magic items changing the stack size of the original item (coins and other costs).
* Fixed formatting error with minimal configuration that would not load the mod properly when used.
* Fixed formatting issue with the Turkish localization.
* Fixed enchanting table rune extraction not always destroying the correct inventory item.
* Minor spelling fixes for English localization.
* Added missing localizations for ModifyFireRate magic effect for all other languages (existed for English).
* Added missing localization for key mod_epicloot_me_modifydamage_desc.

**0.12.9**

* Fixed issue with empty IdentifyCosts throwing an error.
* Added compatibility with Guilds mod that broke buying things in the custom trader menu.

**0.12.8**

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

**0.12.7**

* Reworked magic effects OffSet Attack and DodgeBuff to fix bugs including missing assets (sound and visuals).
* Normalized OffSet Attack values in magiceffects.json, this value represents the percentage of damage reduction (15-40%).
* Bug fix for enchanting table identify costs not applying for more than one cost item.
* Compatibility improvement for other inventory mods, should recognize magic items in additional slots when equipped.
* Bug fix for spam accepting bounties at the merchant and fixed errors when closing the store UI before completed.
* More edge case safeguards for misconfigured files and unexpected game items.

**0.12.6**

* Fixed AddHealthRegen not applying after the 12.5 update.
* Fixed typo for defeated_eikthyr key in new itemsorter.json file.
* More edge case detection for the automatic item sorting logic.
* Added some null reference exceptions safeguards in different places in code.

**0.12.5**

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

**0.12.4**

* Fixed another an issue with MultiShot not resetting weapon attack values properly due to the percent chance to trigger config.

**0.12.3**

* Changed how patches are applied - they will no longer override your baseconfig files on startup! You should be able to generate your baseconfig files once then make changes without them being reverted. See the [wiki for more information](https://thunderstore.io/c/valheim/p/RandyKnapp/EpicLoot/wiki/358-6-config-patching/). We apologize for the confusion around this new feature.
* Fixed an issue with MultiShot breaking enemy projectile damage (someday we will get this effect right on the first try).
* Fixed enchanting materials not stacking with "broken" items in containers. (if this is still happening after the update please report the issue)
* Changed the ..removespeedpenalty_display localization from "Weightless" to "Unhindered". This was a small issue with the 0.12.0 update. Previously this effect was called "No Movement Speed Penalty".
* Added missing configurations for ModifyMagicFireRate and ModifyFireRate in the magiceffects.json files.
* Reenabled Weightless rolls for the balanced config.

**0.12.2**

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

**0.12.1**

* Tweaked the Rune costs config values. 
  * If you used version 0.12.0 please delete your old BepInEx\config\EpicLoot\baseconfig and let them regenerate!
* Fixed Rune UI not enabling correctly on existing enchanting tables.
* Fixed an issue when adding multiple bosses to the same biome with the patch system.
* Fixed magic items not stacking when using the features from the Recycle & Reclaim mod.
* Fixed an issue with Rune configurations not syncing properly on a server.

**0.12.0**

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

**0.11.7**

* Updates for the Russian translation file.
* Removed extended item data framework support (this has been deprecated for over two years now).
* Bug fix for item stands not working correctly with magic items.
* Added missing trinket information in tooltips.

**0.11.6**

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

**0.11.4**

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

**0.11.3**

* Fixed issue with loot rolling lowest tier when using fallbacks from iteminfo config.
* Fixed commands like "magicitemset" not dropping the correct items.
* Multijump reworked to make successive jumps feel like a normal jump from the ground.
* Fixed Andvaranaut not appearing.

**0.11.2**

* Fixed issue with loot rolling higher tiers than should have been allowed for loottable config.
* Small bug fixes for various null reference exception errors.
* Fixed tooltip display bug with gamble menu.
* Fixed recipes not being added for singleplayer (and hosts).

**0.11.1**

* Hotpatch to re-add deprecated Items field in iteminfo.json file. This field does nothing, please remove from your patch files.

**0.11.0**

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

**0.10.7**

* Update for game version 0.220.3.
* Upgraded unity project to version 2022.3.50.
* Added additional null check for ModifyDamage_ItemData_GetDamage_Patch.
* Corrected panel lighting for the enchanting table to match other UI elements.
* Added checks to interval settings to prevent divide by zero errors.
* Updated ServerSync to version 1.18.
* New external dependency for JsonDotNET 13.0.3. You must install this for the mod to function!

**0.10.6**

* Fixed Coin Hoarder not working unless on a weapon.
* Fixed ranged damage from non-players not applying correctly.

**0.10.5**

* Fixed magiceffects ItemUsesStaminaOnAttack, ItemUsesEitrOnAttack, ItemUsesHealthOnAttack, and ItemUsesDrawStaminaOnAttack not resolving correctly when set to true.
* Changed Warmth effect to only roll on Epic and above.

**0.10.4**

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

**0.10.3**

* Hot fix for Sacrificing item selecting the wrong item in the inventory.
* Increased minimum required version to 10.0.

**0.10.2**

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

**0.10.1**

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
