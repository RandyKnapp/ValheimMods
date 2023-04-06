## Release 2.1.3
  * Improved Controller Support Between Hotbars/Inventories
    * Known Bug: The weight calculation is still not working when using controller to transfer items.
  * Updated Keybindings to Support Controllers
  * Rebuilt QuickSlotHotkeyBar from the Ground Up
    * No longer a Prefix that blocks UpdateIcons
    * Allows other mods to affect item icons in the Hotkeybar (like EpicLoot)
    * Potential for Performance Improvement
## Release 2.1.2
  * Updated for Valheim 0.214.2 Patch
## Release 2.1.1
  * Fixed compatibility issues with JewelCrafting and MultiUserChest
## Release 2.1.0
  * Updated for Mistlands!
  * Now uses player.m_customData instead of knownTexts
  * On death, drops equipment in second gravestone
  * Fixed bug where you couldn't move items out of your quickslots
  * Added new config features: DontDropEquipmentOnDeath, DontDropQuickslotsOnDeath, InstantlyReequipArmorOnPickup, InstantlyReequipQuickslotsOnPickup

### Archieved Change Log:
* 1.0.3
    * Integrated fix for larger containers (this mod was not allowing the same row to be used in containers as it uses in the Inventory)
* 1.0.4
    * Fixed issue where gamepad could not use quick slots
* 1.0.5
    * Fixed issue where the previous fix broke the #8 hotkey...
* 2.0.0 Stability Update
    * Items are saved even if accidentally uninstalling or having an error
    * UIs work better with controller
    * Never drop or lose items on death
* 2.0.1
    * Hotfix for not being able to craft when fully equipped
* 2.0.2
    * Fixed an issue where some equipment was lost on death
    * Re-added the toggles to disable and enable features
* 2.0.3
    * Fixed a bug where players could teleport with items they normally can't
* 2.0.4
    * Fixed gamepad navigation for the crafting recipe list
    * Fixed bug preventing new characters from being created
* 2.0.5
    * Put in a potential fix for "losing items" on tombstone pickup (they're in your inventory, just outside the grid. This version should fix that.)
* 2.0.6
    * Fix for double upgrade when using EpicLoot with EAQS
    * Fix for items lost in tombstone by [jsza](https://github.com/jsza).
* 2.0.7
    * (This entire update provided by [jsza](https://github.com/jsza))
    * Fix a variety of equipment bugs
    * Fix a variety of pickup/stacking bugs
* 2.0.8
    * Had to update the number to re-upload to ThunderStore
* 2.0.9
    * Quick slots position is now configurable
* 2.0.10
    * Fixed an encumberance bug
* 2.0.11
    * Added support for Project Auga
* 2.0.12
    * Better Valheim+ and Auga positioning for the inventory
* 2.0.14
    * Updated for H&H
* 2.0.15
    * Yet Another Attempt at fixing the lost-equipment-on-death bug
