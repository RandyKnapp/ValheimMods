# !! READ ME FIRST BEFORE UPGRADING !!
The new, v2.0 of Equipment and Quick Slots uses a new system for saving and loading under the hood.

It is NOT compatible with 1.x versions and 1.x must be uninstalled correctly before upgrading to the 2.0 version!

### How to Upgrade Equipment and Quick Slots 1.x to 2.0.X
1. Start the game with the old mod installed
2. Find a safe place for your character
3. Unequip all equipment and move everything out of the quick slots
4. Quit the game using the menu
5. Remove the old Equipment and Quick Slots dll and install the new version
6. Run the game again
7. You should see your new empty slots in the inventory and be able to re-equip your items

# Equipment and Quick Slots v2.0.3
##### by RandyKnapp
Give equipped items their own dedicated inventory slots, plus three more hotkeyable quick slots.

Hotkeys must be assigned according to the conventions listed here: https://docs.unity3d.com/Manual/class-InputManager.html

Source: [Github](https://github.com/RandyKnapp/ValheimMods/EquipmentAndQuickSlots/)

Install with [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)

Copy EquipmentAndQuickSlots.dll into the BepInEx/plugins folder

### Changelist:
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