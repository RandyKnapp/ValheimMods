# Equipment and Quick Slots API

Typed wrappers for integrating with [Equipment and Quick Slots](https://valheim.thunderstore.io/package/RandyKnapp/EquipmentAndQuickSlots/)
(EAQS), so you never need a hard assembly reference or a Harmony patch on its internals.

What you can do with it:

- **Add your own slots** — a backpack berth, a quiver, a second trinket, a progression-gated
  accessory slot. EAQS draws the cell on its panel, persists whatever is put there through vanilla
  save/load, protects it from Stack All and auto-pickup like its own slots, and hands it to Epic
  Loot as equipped gear.
- **Read slot state** — which slots exist, what sits in each, whether a grid position is a slot.
- **React to changes** — listeners for slot topology and for items entering or leaving a slot.

Everything is reflection-bound against `EquipmentAndQuickSlots.API`, so a missing EAQS means a logged
warning and a no-op, not a crash. Check before you start:

```csharp
if (!EquipmentAndQuickSlotsAPI.EAQS.IsLoaded()) return;   // EAQS absent or too old
```

Full endpoint reference with the signature rules:
[EquipmentAndQuickSlots/docs/API.md](../../EquipmentAndQuickSlots/docs/API.md).

---

## Installation

You can use the API in one of two ways:

### 1. Bundle as DLL

Include `EquipmentAndQuickSlotsAPI.dll` in your project and bundle it into your plugin using
[ILRepack](https://github.com/ravibpatel/ILRepack.Lib.MSBuild.Task). The shim is deliberately
`Version 0.0.0.0`, so you never need to rebuild it when EAQS's version moves.

**Example `ILRepack.targets`:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Target Name="ILRepacker" AfterTargets="Build">
        <ItemGroup>
            <InputAssemblies Include="$(TargetPath)" />
            <InputAssemblies Include="$(OutputPath)\EquipmentAndQuickSlotsAPI.dll" />
        </ItemGroup>
        <ILRepack Parallel="true" DebugInfo="true" Internalize="true"
                  InputAssemblies="@(InputAssemblies)"
                  OutputFile="$(TargetPath)"
                  TargetKind="SameAsPrimaryAssembly"
                  LibraryPath="$(OutputPath)" />
    </Target>
</Project>
```

### 2. Source Files

Copy the contents of `src/` into your plugin project.
Do not modify the provided methods.

### Using the API

- Declare EAQS as a **soft** dependency so it loads first when present, and so your mod still
  loads without it:

  ```csharp
  [BepInDependency("randyknapp.mods.equipmentandquickslots", BepInDependency.DependencyFlags.SoftDependency)]
  ```
- Slot **definitions are not persisted** — only their contents are. Register your slots from
  `Awake` on every launch. Items already sitting in the cell from the last session are there when
  you register; nothing is lost if you register late, the cell just isn't drawn until you do.
- Gate anything newer than the oldest EAQS you support on `EAQS.HasEndpoint("...")`. Through the
  shim a missing endpoint logs a warning and no-ops.

---

## Custom slots

EAQS reserves four cells for API slots; they appear on the panel under the quick slots, three to a
row. `AddSlot` returns `false` when the id is already taken or all four cells are in use, so keep
the result and don't assume a slot exists.

```csharp
public static bool AddSlot(string slotId, string ownerPluginGuid, string nameToken,
                           Func<ItemDrop.ItemData, bool> isValid, Func<bool> isActive);
public static bool RemoveSlot(string slotId);
```

| Parameter | What to pass |
|---|---|
| `slotId` | Unique, stable, namespaced with your plugin — `"MyMod_Quiver"`, not `"Quiver"`. This is the key everything else uses. |
| `ownerPluginGuid` | Your plugin GUID. Recorded on the slot for diagnostics (`eaqs_api` prints it). |
| `nameToken` | The cell label — a localization token (`$mymod_quiver`) or plain text. Long names are auto-shrunk and ellipsized to the cell. |
| `isValid` | Which items the player may put in the slot. `null` accepts anything. |
| `isActive` | Whether the slot is currently available — re-evaluated live, so config toggles, progression and unlock conditions just work. `null` means always. |

### Example: a slot for a specific item

A backpack mod that wants its pack to live in a dedicated cell rather than the grid:

```csharp
[BepInDependency("randyknapp.mods.equipmentandquickslots", BepInDependency.DependencyFlags.SoftDependency)]
public class MyBackpacks : BaseUnityPlugin
{
    public const string GUID = "my.mod.backpacks";

    private void Awake()
    {
        if (!EAQS.IsLoaded()) return;

        bool added = EAQS.AddSlot(
            slotId:          "MyBackpacks_Pack",
            ownerPluginGuid: GUID,
            nameToken:       "$mybackpacks_slot",
            isValid:         item => item.m_shared.m_name == "$item_mybackpacks_pack",
            isActive:        () => true);

        if (!added)
            Logger.LogWarning("EAQS had no free custom slot for the backpack; it will live in the grid instead.");
    }
}
```

### Example: an accessory slot gated by config and progression

`isActive` is a delegate, so the cell can appear and disappear while the game runs. An item sitting
in a slot that turns inactive is moved back into the regular inventory — never dropped, never lost.
(It does not jump back by itself when the slot reappears; the player puts it back.)

```csharp
private static readonly HashSet<string> Trinkets = new() { "$item_mymod_ring", "$item_mymod_amulet" };

EAQS.AddSlot(
    "MyMod_Accessory",
    GUID,
    "$mymod_accessory_slot",
    isValid:  item => Trinkets.Contains(item.m_shared.m_name),
    isActive: () => EnableAccessorySlot.Value                                  // a ConfigEntry<bool>
                    && ZoneSystem.instance != null
                    && ZoneSystem.instance.GetGlobalKey("defeated_eikthyr"));  // unlocks after the first boss
```

### Example: removing a slot

Removing rescues whatever is in the cell (regular inventory first, any other free slot second,
the ground only as a last resort) and returns `false` for an unknown id.

```csharp
private void OnDestroy()
{
    if (EAQS.IsLoaded())
        EAQS.RemoveSlot("MyMod_Accessory");
}
```

### What EAQS does for your slot

- **Placement rules** — the panel tints the cell red while an item that fails `isValid` is being
  dragged, and the drop is refused. Drops that fit land like any inventory move.
- **Protection** — Stack All never pulls the item into a container; auto-pickup never lands loose
  pickups in it (both per the player's EAQS config, same as the quick slots).
- **Death** — the item travels to the gravestone at its cell position and comes back to the same
  cell on pickup. It follows the `Dont drop equipment on death` setting together with the paperdoll
  cells.
- **Epic Loot** — when Epic Loot is installed, items in custom slots are reported to it as equipped,
  so their magic effects, set bonuses, tooltips and equip visuals count exactly like gear in a
  vanilla slot, and they are excluded from the Sacrifice tab. You do not need to register anything
  with Epic Loot yourself.

What it deliberately does **not** do: it never calls `EquipItem` on your behalf. A custom slot is a
typed cell, not a vanilla equipment slot — if your item should *behave* as worn (stat bonuses, a
status effect, a model on the character), apply that yourself when it enters the slot and remove
it when it leaves (see [Listeners](#listeners)).

---

## Queries

```csharp
List<string> ids = EAQS.GetSlotIds();            // "Quick1".."Quick6", "Helmet", ..., "Utility3", plus custom ids
string info     = EAQS.GetSlotInfoJson("Helmet"); // {"id":"Helmet","index":8,"nameToken":"Head","active":true,"gridX":0,"gridY":4,"isQuickSlot":false,"isEquipmentSlot":true,"isCustomSlot":false,"ownerPluginGuid":"","occupied":true}

if (EAQS.TryGetSlotItem("MyMod_Accessory", out ItemDrop.ItemData item))
    ApplyAccessoryBonus(item);

List<ItemDrop.ItemData> quick = EAQS.GetQuickSlotItems();       // items in the active quick slots
List<ItemDrop.ItemData> worn  = EAQS.GetEquipmentSlotItems();   // items in the paperdoll cells

int visible = EAQS.GetVisibleRows();   // rows the player can see (4 + EAQS's configured extra rows)
int full    = EAQS.GetFullHeight();    // visible rows + the hidden slot rows
```

### Example: is this inventory position one of EAQS's cells?

Useful for inventory-sorting, quick-stacking and "drop everything" features that must leave slot
items alone. The player inventory is `8 × GetFullHeight()`; every cell from row `GetVisibleRows()`
down is EAQS territory.

```csharp
bool IsProtected(ItemDrop.ItemData item)
{
    return EAQS.IsLoaded() && EAQS.IsSlotCell(item.m_gridPos.x, item.m_gridPos.y, out _);
}

void SortInventory(Inventory inventory)
{
    foreach (ItemDrop.ItemData item in inventory.GetAllItems())
    {
        if (IsProtected(item)) continue;   // never rearrange equipment, quick or custom slot contents
        // ...
    }
}
```

The ids returned by `IsSlotCell`'s `out` parameter are the same ids `GetSlotIds` lists, so you can
special-case your own slot by comparing against your `slotId`.

---

## Listeners

Listeners are plain method calls rather than C# events, so a reflection-only consumer can subscribe
too. A listener that throws is logged and does not stop the others.

```csharp
// Topology: a custom slot was added or removed (payload: its slotId)
EAQS.AddSlotChangedListener(slotId => { /* ... */ });

// Content: the item in a slot changed (payload: slotId, old item or null, new item or null)
EAQS.AddSlotItemChangedListener((slotId, oldItem, newItem) => { /* ... */ });

// matching Remove* for each
EAQS.RemoveSlotItemChangedListener(MyHandler);
```

`SlotItemChanged` is raised once per frame, after EAQS's own validation has settled, so you see
the final state of a move rather than every intermediate position. It fires for built-in slots too;
filter on the id you care about.

### Example: apply a bonus while your item sits in your slot

```csharp
private void Awake()
{
    if (!EAQS.IsLoaded()) return;

    EAQS.AddSlot("MyMod_Charm", GUID, "$mymod_charm_slot", item => item.m_shared.m_name == "$item_mymod_charm", null);
    EAQS.AddSlotItemChangedListener(OnSlotItemChanged);

    // The slot may already hold the charm from the last session
    if (EAQS.TryGetSlotItem("MyMod_Charm", out ItemDrop.ItemData charm))
        ApplyCharm(charm);
}

private void OnSlotItemChanged(string slotId, ItemDrop.ItemData oldItem, ItemDrop.ItemData newItem)
{
    if (slotId != "MyMod_Charm") return;

    if (oldItem != null) RemoveCharm(oldItem);
    if (newItem != null) ApplyCharm(newItem);
}

private static void ApplyCharm(ItemDrop.ItemData charm)  => Player.m_localPlayer?.GetSEMan().AddStatusEffect("SE_MyCharm".GetStableHashCode());
private static void RemoveCharm(ItemDrop.ItemData charm) => Player.m_localPlayer?.GetSEMan().RemoveStatusEffect("SE_MyCharm".GetStableHashCode());
```

---

## Versioning

```csharp
EAQS.GetApiVersion();          // int, bumped on every additive change — independent of the plugin version
EAQS.GetPluginVersion();       // "3.0.0"
EAQS.HasEndpoint("AddSlot");   // feature probe before relying on something newer
```

---

## Without the shim — raw reflection

Everything the shim does is one reflection call away; no EAQS type appears in any signature, only
primitives, `string`, vanilla types (`ItemDrop.ItemData`) and `System.Func`/`Action` built from
those. Endpoints that hand data back use `ref` rather than `out`, so a `MethodInfo.Invoke` can read
the value out of the argument array.

```csharp
Type api = Type.GetType("EquipmentAndQuickSlots.API, EquipmentAndQuickSlots");
if (api == null) return;   // EAQS not installed

// Add a slot
api.GetMethod("AddSlot").Invoke(null, new object[]
{
    "MyMod_Quiver", GUID, "$mymod_quiver",
    (Func<ItemDrop.ItemData, bool>)(item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo),
    (Func<bool>)(() => true)
});

// Read the slot item (ref parameter: read it back from the args array)
object[] args = { "MyMod_Quiver", (ItemDrop.ItemData)null };
bool has = (bool)api.GetMethod("TryGetSlotItem").Invoke(null, args);
ItemDrop.ItemData arrows = (ItemDrop.ItemData)args[1];
```

Keep the reflection in a method that is never inlined into your `Awake` (a separate method is
enough), and gate on `Chainloader.PluginInfos.ContainsKey("randyknapp.mods.equipmentandquickslots")`
first — the type lookup is what fails when the mod is absent.

---

## Testing your integration

EAQS ships a console command that exercises the API from in-game:

```
eaqs_api    — API version, endpoint list, and per-slot state JSON (ids, owners, occupancy, grid positions)
```

Register your slot, open the console, and `eaqs_api` should list it with your plugin GUID as owner.
A complete reference consumer — three accessory slots with config and progression gates, a listener
harness, and a command that adds/removes slots to prove capacity limits — lives in the
`ValheimTestMod` project next to this repository.
