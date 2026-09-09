# Advanced Portals

Author: [RandyKnapp](https://discord.gg/ZNhYeavv3C)
Source: [Github](https://github.com/RandyKnapp/ValheimMods/tree/main/AdvancedPortals)
Patreon: [patreon.com/randyknapp](https://www.patreon.com/randyknapp)
Discord: [RandyKnapp's Mod Community](https://discord.gg/ZNhYeavv3C)

Adds three new portals to provide a lore-friendly and balanced way to reduce the item-transport slog!

  * **Ancient Portal:** Allows teleporting Copper and Tin
    * *Requires:* 20 Ancient Bark, 5 Iron, 2 Surtling Cores
  * **Obsidian Portal:** Allows teleporting Iron
    * *Requires:* 20 Obsidian, 5 Silver, 2 Surtling Cores
  * **Black Marble Portal:** Allows teleporting anything
    * *Requires:* 20 Black Marble, BlackMetal 5, 2 Refined Eitr

## Version 1.2.0!

Portals are now fully configurable building pieces. Alongside the recipe and teleport settings, you can
change each portal's build category, whether it needs a crafting station at all, and which station that is.
Every setting applies live, without restarting the game.

**Upgrading from 1.1.x:** the config file has been reorganised and your previous customisations will not
carry over. Delete `randyknapp.mods.advancedportals.cfg` and re-apply any changes you had made.

Jotunn is required. A version check runs on server connection to make sure every player has the mod
installed properly. Settings are synced from the server and can be edited live, either in the in-game
Configuration Manager or by editing `randyknapp.mods.advancedportals.cfg`.

## Configuration:

Each portal has its own config section named after it: `[Ancient Portal]`, `[Obsidian Portal]`,
`[Black Marble Portal]`. Every setting is server-synced and only editable by an admin.

  * **Enabled:** Allow building this portal. Existing portals of this type are not removed.
  * **Building Cost:** What it costs to build, as `ITEM,QUANTITY,REFUNDABLE` entries separated by `|`.
    ITEM is the item ID ([found here](https://valheim-modding.github.io/Jotunn/data/objects/item-list.html)),
    QUANTITY is a whole number, and REFUNDABLE is `true` or `false` for whether you get it back on removal.
    For example: `ElderBark,20,true|Iron,5,true|SurtlingCore,2,true`
  * **Requires Workbench:** Whether a crafting station is needed to build this portal at all.
  * **Workbench:** Which crafting station is required, e.g. `piece_workbench`, `forge`, `blackforge`,
    `piece_artisanstation`. Ignored when Requires Workbench is off.
  * **Piece Category:** Which tab of the hammer build menu the portal appears in.
  * **Allowed Items:** Items allowed to teleport through the portal, as `ITEM1, ITEM2, ITEM3, ...`
  * **Allow Everything:** Allow all items through the portal. Overrides Allowed Items.
  * **Use All Previous:** Also allow everything the portals listed above this one allow. The Obsidian
    portal inherits from the Ancient portal; the Black Marble portal inherits from both.

In the in-game Configuration Manager each portal is a single collapsible row under **Building Pieces**,
with a table editor for the build cost. The teleport settings appear under the portal's own section.

## Installation:

  * Manual: Drop the AdvancedPortals.dll into your BepInEx/plugins folder. Download Jotunn and install similarly.
  * ThunderStore: When using a thunderstore mod manager the files should be placed in the correct directory for you. Dependencies should install automatically.
