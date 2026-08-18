# Epic Loot integration API

A supported contract for other plugins, so integrating with Epic Loot does not mean Harmony-patching its
internals and re-breaking on every release.

There are two layers, and you can use either:

| | What it is | When to use it |
|---|---|---|
| **`EpicLoot.API`** | A `static partial class` in `EpicLoot.dll`. The actual ABI. | You are comfortable with `Type.GetType("EpicLoot.API, EpicLoot")` and reflection, or you already have your own glue. |
| **`EpicLootAPI.dll`** | A small shim that wraps every endpoint in typed methods and does the reflection for you. Bundle it with ILRepack. | Almost always. See [the shim README](../../EpicLootAPI/EpicLootAPI/README.md) for the ILRepack target. |

Epic Loot's plugin GUID is `randyknapp.mods.epicloot`. Declare it as a soft dependency:

```csharp
[BepInDependency("randyknapp.mods.epicloot", BepInDependency.DependencyFlags.SoftDependency)]
```

---

## The signature rule

Nothing but primitives, `string`, vanilla/Unity types (`Player`, `ItemDrop.ItemData`, `Vector3`,
`UnityEngine.Object`) and `System.Func`/`Action` built from those ever crosses this boundary. **No Epic
Loot type appears in any signature.** Structured data travels as JSON strings; rarity travels as an `int`.

That is what lets you call the API with no assembly reference at all, and what keeps an internal refactor
from breaking your build.

Two consequences worth knowing before you write glue by hand:

- **`ref`, not `out`.** The shim's reflection transport reads mutated arguments back out of the `object[]`
  that `MethodInfo.Invoke` fills in, which does not work with `out`. Endpoints like `TryGetRarity` take
  `ref int rarity`. The typed shim presents them as normal `out` parameters.
- **Rarity is an `int`.** `0`=Magic, `1`=Rare, `2`=Epic, `3`=Legendary, `4`=Mythic. Call
  `GetRarityCount()` rather than hard-coding 5.

## Versioning

```csharp
API.ApiVersion            // const int, currently 1
API.GetApiVersion()       // same value, as a call
API.GetPluginVersion()    // "0.13.0"
API.GetPluginId()         // "randyknapp.mods.epicloot"
API.HasEndpoint(name)     // feature probe for a specific method
API.GetEndpointNames()    // every public endpoint, for diagnostics
```

`ApiVersion` is bumped on every additive change and is independent of the plugin version. If you rely on
something newer than the oldest Epic Loot you support, gate it on `HasEndpoint`. Through the shim, a
missing endpoint logs a warning and no-ops rather than throwing.

Epic Loot runs `NetworkCompatibility(EveryoneMustHaveMod, VersionStrictness.Patch)` — every client and the
server must run the same patch version. The shim is not subject to that; bundle whichever version you
built against.

---

## Migration table

Every row is something at least one released mod does today. The left column is unsupported and has
broken before.

| Instead of patching / reflecting | Call |
|---|---|
| `EpicLoot_UnityLib.InventoryManagement.GetAllItems` / `HasItem` / `CountItem` / `RemoveItem` / `RemoveExactItem` | `RegisterInventoryProvider` |
| `EpicLoot.PlayerExtensions.GetEquipment` (deprecated — internals call `GetMagicEquipment`, so this patch does nothing) | `RegisterEquipmentProvider` |
| `EpicLoot.Crafting.EnchantCostsHelper.GetSacrificeProducts` (prefix returning false) | `RegisterSacrificeFilter` |
| `EpicLoot.Crafting.EnchantTabController.GetEnchantCosts` (the type no longer exists) | `GetEnchantCostsJson` |
| `EpicLoot.EpicLoot.GetRarityColor` + enumerating the `ItemRarity` enum | `GetRarityColorByIndex`, `GetRarityCount` |
| `EpicLoot.ItemDataExtensions.IsMagic` / `GetRarity` | `IsMagicItem`, `TryGetRarity` |
| `m_shared.m_name.StartsWith("$mod_epicloot")` | `IsEpicLootItem` |
| `LootRoller.RollMagicItem` / `RollEffect` / `InitializeMagicItem` / `GetLuckFactor` via a publicized DLL | `TryMakeMagicItem`, `RollMagicItemJson`, `GetLuckFactor` |
| `UniqueLegendaryHelper.TryGetLegendaryInfo` / `GetSetForLegendaryItem` | `GetLegendaryInfoJson`, `GetLegendaryIDs` |
| `MagicItemComponent.SetMagicItem` | `ApplyMagicItemJson`, or `TryMakeMagicItem` |
| `MagicItemEffectDefinitions.GetAvailableEffects` / `AllDefinitions` | `GetAvailableEffectTypes`, `GetAllMagicEffectTypes` |
| Polling for "did this item change?" | `AddMagicItemChangedListener` |

Two things are **not** promoted to the API and remain unsupported internals: `PatchOnHoverFix.AddScrollbar`
(UI plumbing) and reading Epic Loot's BepInEx config entries directly.

---

## Queries

All null-safe and non-throwing. Note that the underlying extension methods are not — `GetRarity` throws
for a non-magic item — which is why `TryGetRarity` exists.

```csharp
bool   IsMagicItem(ItemDrop.ItemData item);
bool   TryGetRarity(ItemDrop.ItemData item, ref int rarity);
int    GetRarityCount();
string GetRarityColorByIndex(int rarity);          // "#AA55FF"
string GetRarityDisplayNameByIndex(int rarity);    // "$mod_epicloot_Epic"
string GetItemRarityColor(ItemDrop.ItemData item); // the colour this item's name is drawn in
bool   IsEpicLootItem(ItemDrop.ItemData item);
bool   IsShardStone(ItemDrop.ItemData item);
bool   IsRunestone(ItemDrop.ItemData item);
bool   IsMagicCraftingMaterial(ItemDrop.ItemData item);
bool   IsUnidentified(ItemDrop.ItemData item);
bool   CanBeMagicItem(ItemDrop.ItemData item);
bool   ItemHasMagicEffect(ItemDrop.ItemData item, string effectType, bool includeSocketed);
List<string> GetAllMagicEffectTypes();
string GetEnchantCostsJson(ItemDrop.ItemData item, int rarity);   // [{ "Item": prefab, "Amount": n }]
string GetSacrificeProductsJson(ItemDrop.ItemData item);
string GetMagicItemJson(ItemDrop.ItemData item);
```

`IsUnidentified` matters: an unidentified item reports `true` from `IsMagicItem`, but its effects are not
revealed yet and should not be shown or acted on.

Through the shim these are extension methods:

```csharp
using EpicLootAPI;

if (!EpicLoot.IsLoaded()) return;

if (item.IsMagicItem() && item.TryGetRarity(out ItemRarity rarity))
{
    string colour = EpicLoot.GetRarityColor(rarity);
}
```

---

## Providers

The core of the API: contribute to what Epic Loot considers available, instead of patching the methods
that ask.

Every registered delegate runs inside a try/catch. One that throws is logged with your id — always, not
gated by Epic Loot's logging config, because it is your bug — and treated as contributing nothing. A
broken integration degrades to vanilla behaviour; it does not break the enchanting table.

### Inventory providers

Widen the enchanting table's view of the player's inventory: nearby containers, a backpack, a remote
stash. Epic Loot spends the player's own inventory first and only charges the shortfall to providers, in
registration order.

```csharp
API.RegisterInventoryProvider(
    id:              "my.plugin.guid",
    getItems:        () => NearbyContainerItems(),          // may be null
    countItem:       name => CountInContainers(name),       // by m_shared.m_name
    removeItem:      (name, amount) => TakeFromContainers(name, amount),  // returns amount removed
    removeExactItem: (item, amount) => TakeExact(item, amount));

API.UnregisterInventoryProvider("my.plugin.guid");
```

- `getItems` must return **live instances**, not copies. Epic Loot needs instance identity to preserve
  magic data, and it de-duplicates against what the player already holds.
- `removeExactItem` must match **by reference**, not by name. Matching by name will consume the wrong
  enchanted item.
- `removeItem` / `removeExactItem` return how many they actually removed; returning more than requested is
  clamped.

### Equipment providers

Contribute equipped items for magic effect resolution — extra equipment slots, quick slots, an equipped
backpack. These feed `PlayerExtensions.GetMagicEquipment`, so contributed items count toward effect
totals, legendary set bonuses, tooltips and shard socketing alike. Non-magic items are filtered out for
you, so returning everything is fine.

```csharp
API.RegisterEquipmentProvider("my.plugin.guid", player => MyExtraSlots(player));
```

Contributed items also get their equip effect visuals — the auras and particle effects worn on the
player, such as `Glowing`. Those are reconciled against `GetMagicEquipment` after every equipment
change, so an item in your slot lights up exactly like one in a vanilla slot; you do not need to attach
or remove anything yourself.

Epic Loot memoizes effect totals per player and invalidates only on vanilla `Humanoid.EquipItem` /
`UnequipItem`. If your slots change outside those methods, tell it:

```csharp
API.InvalidatePlayerEffectCache(player);
```

Without that call, stale totals keep being served after a slot change. The same call also refreshes the
worn equip effect visuals, so it is the one signal to send whenever your slot contents move.

### Sacrifice filters

Veto sacrificing specific items — typically one equipped in a slot Epic Loot cannot see, which would
otherwise be destroyed by accident. Returning `false` yields no sacrifice products, which hides the item
from the Sacrifice tab.

```csharp
API.RegisterSacrificeFilter("my.plugin.guid", item => !IsInMyQuickSlot(item));
```

Called for every item the tab evaluates, so keep it cheap.

`API.GetRegisteredProviders()` returns the registered ids grouped by family, for diagnostics.

---

## Drawing your own item slots

Epic Loot decorates the vanilla inventory grid and hotkey bar with **transpilers**. If your plugin
reimplements `InventoryGrid.UpdateGui` or `HotkeyBar.UpdateIcons` — a prefix returning `false`, say —
the original body never runs, so neither does the decoration, and your slots show no rarity background.
A postfix on those methods cannot fix it for you: Epic Loot has no way to tell which of your elements
holds which item.

Call this once per element instead, from wherever you fill it in:

```csharp
API.ApplyMagicItemBackground(element.m_go, element.m_equiped, item, inventoryGrid: false);
```

- Pass `null` for `item` to clear a slot you are emptying.
- `inventoryGrid: true` also draws the legendary set marker; `false` is the hotbar treatment.
- Child images are created the first time a slot actually needs one, so it is safe to call every frame
  and calling it for empty slots costs nothing.

---

## Events

Add and remove listeners with plain method calls, so a reflection-only consumer can subscribe without
binding to an event field.

```csharp
API.AddMagicItemChangedListener((item, reason) => { /* ... */ });
API.AddLootGeneratedListener(item => { /* ... */ });
API.AddBountyCompletedListener((player, monsterId) => { /* ... */ });
// plus a matching Remove* for each
```

`OnMagicItemChanged` fires on **every** magic-data write, with a reason token from `API.ChangeReason`:

| Token | Raised by |
|---|---|
| `Enchant` | Enchanting an item at the table |
| `Augment` | Locking in or applying an augment |
| `Disenchant` | Sacrificing a magic item (the component is dropped) |
| `Rune` | Rune extract, rune enhance, and the item reduction that follows an extract |
| `Temper` | A tempering success or failure |
| `Socket` / `Unsocket` | Adding or removing a shard/runestone |
| `LootRoll` | A magic item rolled as loot, including via `TryMakeMagicItem` |
| `Transfer` | Magic effects carried onto a crafted item |
| `Unspecified` | A write with no dedicated call site |

`Unspecified` is the catch-all raised from `MagicItemComponent.SetMagicItem`, the single funnel every
magic-data write passes through — so coverage is complete even for paths that have no semantic token.
Writes that happen while an item is *loading* do not raise anything; otherwise every item entering the
world would notify you at world load.

The item's new magic data is already committed when your listener runs. A listener that throws is logged
and does not stop the other listeners or the game action.

---

## Loot generation

```csharp
bool   TryMakeMagicItem(ItemDrop.ItemData item, int rarity, float luck, string legendaryID);
string RollMagicItemJson(int rarity, ItemDrop.ItemData item, float luck);
bool   ApplyMagicItemJson(ItemDrop.ItemData item, string magicItemJson);
float  GetLuckFactor(Vector3 position);
int    RollEffectCountForRarity(int rarity);
List<string> GetLegendaryIDs(int rarity);
string GetLegendaryInfoJson(string legendaryID);
List<string> GetAvailableEffectTypes(ItemDrop.ItemData item, string magicItemJson, int rarity);
string AddLootTables(string json);
bool   UpdateLootTables(string key, string json);
```

`TryMakeMagicItem` is the one-call path: it reproduces the whole drop flow — effect selection, socket
count, legendary/mythic assignment, randomized wear, display name — and raises both `LootRoll` and
`OnLootGenerated`. Check `CanBeMagicItem` first. Passing an unknown `legendaryID`, or one whose
requirements do not fit the item, **fails the roll** rather than silently downgrading it.

To inspect before applying, use `RollMagicItemJson` then `ApplyMagicItemJson`.

`AddLootTables` returns an opaque key; pass it back to `UpdateLootTables` to replace what you added.
Registrations are cached and re-applied whenever `loottables.json` reloads or a dedicated server pushes
its copy, so they survive both. (`LootRoller.Initialize` clears the table map, which is why the re-apply
matters — the same contract the content-registration endpoints have always had.)

---

## Content registration

The older half of the API — adding magic effects, abilities, legendaries and sets, material conversions,
sacrifices, bounty targets, secret stash items and treasure maps — is unchanged and documented with worked
examples in [the shim README](../../EpicLootAPI/EpicLootAPI/README.md).

**Recipes are deprecated.** `AddRecipe`, `AddRecipes` and `UpdateRecipes` (and the shim's `CustomRecipe`)
are marked `[Obsolete]`: Epic Loot removed `recipes.json` in 0.13.0 and no longer ships a recipe config,
so there is nothing left for them to register into. Add recipes with Jotunn's `ItemManager` instead.

Two hooks there are worth calling out because they take callbacks rather than JSON:

- **`RegisterMagicEffectRequirement(name, predicate)`** — a custom gate for a magic effect, referenced from
  the effect's `Requirements.ExternalRequirements`. Enforced on **every** path, including shard socketing
  (unlike the built-in host-item gating, which socketing deliberately skips).
- **`RegisterProxyAbility(json, delegates)`** — an ability whose behaviour lives in your assembly.

Registrations survive config reloads and server config pushes: the API caches them and re-applies them on
each `OnSetup*` event.

There is also a non-code route that needs no plugin at all — the JSONPath patch engine reading
`<BepInEx>/config/EpicLoot/patches/*.json`. See `config-patches/` for examples. Use it when you only need
to change data.

---

## Testing your integration

Epic Loot ships a console command that exercises the API from in-game:

```
magicapi version     — API version, plugin version, endpoint and rarity list
magicapi query       — every read-only endpoint against the equipped item
magicapi providers   — registered inventory/equipment/sacrifice provider ids
magicapi events      — toggle a logging listener for change and loot events
magicapi roll <n>    — TryMakeMagicItem on the equipped item
```

`magicapi events` writes to the BepInEx log, so you can confirm your listener would have fired and with
which reason token.
