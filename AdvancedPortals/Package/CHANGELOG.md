**1.2.0**
* Portals now use the shared configurable-piece system. Each portal gains new server-synced settings that apply live, without a restart:
  * **Requires Workbench** / **Workbench** - whether a crafting station is needed at all, and which one.
  * **Piece Category** - which hammer build tab the portal appears in.
  * **Building Cost** - now supports a per-ingredient refund flag.
* Disabling a portal is now reversible in-game. Previously a disabled portal stayed gone until a restart.
* Fixed: editing your own config file while connected to a server no longer overwrites the values the server sent you.
* **Breaking:** the config file has been reorganised and your previous customisations will not carry over. Each portal now has its own section named after it (`[Ancient Portal]`), and the build cost format changed from `Item:Quantity,Item:Quantity` to `Item,Quantity,Refundable|Item,Quantity,Refundable`. Delete `randyknapp.mods.advancedportals.cfg` and re-apply any changes you had made.

**1.1.3**
* Fixed an issue with the AllowEverything config option no longer being used in the teleport check.

**1.1.2**
* Changed portals metal check patch to better respect other mods including World Advancement & Progression.

**1.1.1**
* Fixed a small typo issue that only manifested when TargetPortal was installed.

**1.1.0**
* Overhauled portal appearances to better match vanilla styles!
* Now requires Jotunn to run, please install this new dependency!

**1.0.11**
* Update for Valheim version 0.219.13 Bog Witch.

**1.0.10**
* ServerSync Update fixing a multiplayer issue

**1.0.9**
* Update for 0.217.24 - Hildr's Request

**1.0.8**
* Fixing the Portal Saving issue with the changes that were presented in a recent update.
* This now adds the Custom Portal Prefabs to the PortalPrefab list during Game.Awake reducing the need to patch any ZDOMan stuff.

**1.0.7**
* Hildir's Request Update 0.217.14

**1.0.6**
* Restored Portal Connections on Dedicated Servers.

**1.0.5**
* Updated Portal Connection logic which was preventing Advanced Portals from connecting

**1.0.4**
* Updates for 0.216.9 Valheim

**1.0.3**
* Vapok fixed a bug that makes Adventure Backpacks work with Advanced Portals

**1.0.2**
* Added bronze to Ancient portal transport list (how could I forget?)
* Updated to support other mods that extend the inventory (Thanks Vapok)

**1.0.1**
* Added compatibility with AnyPortal and TargetPortal
* Fixed a bug with Obsidian and Black Marble portal recipes
* Added 5 BlackMetal to the default recipe for Black Marble portals (delete your config to automatically use the new recipe)

**1.0.0**
* Initial Release