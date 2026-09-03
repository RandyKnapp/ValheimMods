# Equipment and Quick Slots
##### by RandyKnapp, rewritten by MidnightsFX

Version 3.0 is a complete rewrite.

Give equipped items their own dedicated inventory slots — Head, Chest, Legs, Shoulder, Utility
and Trinket — plus up to six hotkeyable quick slots.

* Client-side installable; does not need to be on servers.
* When the server *does* run the mod (requires [Jotunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/)),
  the balance settings (slot toggles, quick slot count, keep-on-death) are server-synced and
  admin-controlled.
* Existing 2.x characters are migrated automatically on first login. **Do not downgrade** to 2.x
  after saving under 3.0 — the automatic slot backup protects your items, but the old format is
  not written anymore.
* Quick slot hotkeys no longer trigger vanilla actions on the same key.
* Optional **extra utility slots** (`Equipment Slots / Utility Slot Count`, 1–3, default 1): wear
  more than one belt, Wishbone or Megingjord. Off by default and server-synced, because it is a
  balance change. You can never wear two copies of the same item, and the extra items show on
  your character for everyone.
* Configurable keep-on-death and auto-equip-on-gravestone-pickup behavior.
* Other mods can add custom slots through the API — see
  [docs/API.md](https://github.com/RandyKnapp/ValheimMods/tree/main/EquipmentAndQuickSlots/docs/API.md).

Please report bugs on our discord: https://discord.gg/ZNhYeavv3C

Source: [Github](https://github.com/RandyKnapp/ValheimMods/EquipmentAndQuickSlots/)

Install with [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/):
copy `EquipmentAndQuickSlots.dll` into the `BepInEx/plugins` folder.

### Console commands

* `eaqs_validate` — revalidates the slots and relocates overlapping/out-of-grid items
* `eaqs_api` — prints API version, endpoints and slot states
* `invcheck` — prints the inventory grid contents
* `eaqs_restorebackup` (cheat) — restores the automatic slot backup into free slots
* `breakequipment`, `dropall` (cheat) — testing helpers

### Compatibility

* **BetterUI** works out of the box. While BetterUI's HUD editing is enabled (its default), BetterUI
  positions the quick slot bar: move it with BetterUI's HUD edit key (F7 by default) and BetterUI
  remembers where you put it. `Quick Slots Anchor` / `Quick Slots Position` in this mod's config only
  decide where the bar starts. If the bar sits somewhere odd after updating from 3.0.0, press F7 and
  drag it back, or reset BetterUI's `uiData` setting to `none`.
* Other slot mods — AzuExtendedPlayerInventory, ExtendedPlayerInventory, ExtraSlots, ComfyQuickSlots —
  are declared incompatible; BepInEx will not load this mod next to them.

### Notes

The following things WILL NOT BE ADDED OR CONSIDERED.
* Player wardrobe features (instead use [Advizes Armoire](https://thunderstore.io/c/valheim/p/Advize/Armoire/))
* Slots which provide: dedicated arrow, or food slots, stat changes per slot (instead use [Extra Slots](https://thunderstore.io/c/valheim/p/shudnal/ExtraSlots/))
* 3D or otherwise interactable character model (use [Azu Extended Inventory](https://valheim.hexium.gg/mods/Azumatt/AzuExtendedPlayerInventory) instead)
