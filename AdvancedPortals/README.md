# Advanced Portals

Author: [RandyKnapp](https://discord.gg/ZNhYeavv3C)
Source: [Github](https://github.com/OrianaVenture/Randy_Vapok_ValheimMods/tree/main/AdvancedPortals)
Patreon: [patreon.com/randyknapp](https://www.patreon.com/randyknapp)
Discord: [RandyKnapp's Mod Community](https://discord.gg/ZNhYeavv3C)

Adds three new portals to provide a lore-friendly and balanced way to reduce the item-transport slog!

  * **Ancient Portal:** Allows teleporting Copper and Tin
    * *Requires:* 20 Ancient Bark, 5 Iron, 2 Surtling Cores
  * **Obsidian Portal:** Allows teleporting Iron
    * *Requires:* 20 Obsidian, 5 Silver, 2 Surtling Cores
  * **Black Marble Portal:** Allows teleporting anything
    * *Requires:* 20 Black Marble, BlackMetal 5, 2 Refined Eitr

## Version 1.1.0!

As of the 1.1.0 update Jotunn is required to run this mod. A version check will be performed on server connection to ensure all players have the mod installed properly.

Configurations should sync on servers and live update on changing the randyknapp.mods.advancedportals.cfg file.

## Configuration:

Each portal can be configured:

  * Enabled: Enable building the portal. Existing portals of this type will not be removed.
  * Recipe: Items needed to build the portal in the format "ITEM1:QUANTITY,ITEM2:QUANTITY,..." where each ITEM is the item ID ([found here](https://valheim-modding.github.io/Jotunn/data/objects/item-list.html)), and QUANTITY is an integer.
  * Allowed Items: Items allowed to teleport through the portal in the format: "ITEM1,ITEM2,ITEM3,..." where ITEM is the item ID.
  * Allow Everything: Allow all items through the portal.
  * Use All Previous: For the Obsidian portal also include the Allowed Items from the Ancient portal. For the Black Marble portal also include the Allowed Items from both the Ancient and Obsidian portals.

## Installation:

  * Manual: Drop the AdvancedPortals.dll into your BepInEx/plugins folder. Download Jotunn and install similarly.
  * ThunderStore: When using a thunderstore mod manager the files should be placed in the correct directory for you. Dependencies should install automatically.
