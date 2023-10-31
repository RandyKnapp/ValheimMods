## Release 2.1.12
* Updated for Valheim 0.217.27
* Updated Auga and CLLC API's
* If Auga is loaded, set the default position of Quick Slot Bar accordingly

<details>
<summary><b>Changelog History</b> (<i>click to expand</i>)</summary>

## Release 2.1.11
* Adjusting InventoryGrid Initialization to prevent Awake from happening before variables are set.
  * This has fixed a compatibility issue that was found with Jewelcrafting allowing EAQS to now be used with Smoothbrain's Jewelcrafting
## Release 2.1.10
* Fixing Hotkey Bar Binding Texts
## Release 2.1.8 & 2.1.9
* Hildir's Request Updates 0.217.24
* Updated version from 2.1.7 to 2.1.9 because I forgot to change it.
## Release 2.1.7
* Hildir's Request Updates 0.217.14
## Release 2.1.6
* Updates needed for Valheim 0.216.7
## Release 2.1.5
* Fixing Keybinds to defaults if config is messed up and showing None.
  * This was caused by a change in how keybinds are stored.
* Tooltips when using Controllers are now visible and not hiding behind the Equipment Slots
## Release 2.1.4
* DLL packaged with 2.1.3 was incorrectly built as 2.1.2 and might not have had all the changes in it.
* Bumping version by 1 and reuploading correct version.
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

</details>