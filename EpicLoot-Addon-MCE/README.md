# Epic Loot Addon - MCE v1.0.0
Author: RandyKnapp
Source: [Github](https://github.com/RandyKnapp/ValheimMods/tree/main/EpicLoot-Addon-MCE/)

## What does it do?

This mod addon allows EpicLoot config files to be enforced by the server using ModConfigEnforcer. All loot tables, recipes, even item names will be controlled by the server for all connected clients. Both clients and server are required to have this addon installed to get server enforced configs.

#### Currentlt Enforced by MCE:

  * Config Value: Balance / Item Drop Limits
  * Config File: enchantcosts.json
  * Config File: iteminfo.json
  * Config Fite: itemnames.json
  * Config File: loottables.json
  * Config File: magiceffects.json
  * Config File: recipes.json

## How to Install

For both the server and the client:

1. Install [EpicLoot](https://www.nexusmods.com/valheim/mods/387) (requires version 0.6.4 or later)
2. Install [ModConfigEnforcer](https://www.nexusmods.com/valheim/mods/460) (requires version 1.4.1 or later)
3. Copy EpicLoot-Addon-MCE.dll to your BepInEx plugins folder

## Patch Notes
#### Version 1.0.0
  * Initial release, allows MCE to control config values
