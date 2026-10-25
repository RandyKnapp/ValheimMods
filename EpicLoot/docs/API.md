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
- **Rarity is an `int`.** `0`=Magic, `1`=Rare, `2`=Epic, `3`=Legendary, `4`=Mythic, `5`=Ancient. Call
  `GetRarityCount()` rather than hard-coding 5.

## Versioning

```csharp
API.ApiVersion            // const int, currently 3
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
| `EpicLoot.EpicLoot.GetSetItemColor` | `GetSetItemColor` |
| `EpicLoot.ItemDataExtensions.IsMagic` / `GetRarity` | `IsMagicItem`, `TryGetRarity` |
| `m_shared.m_name.StartsWith("$mod_epicloot")` | `IsEpicLootItem` |
| `LootRoller.RollMagicItem` / `RollEffect` / `InitializeMagicItem` / `GetLuckFactor` via a publicized DLL | `TryMakeMagicItem`, `RollMagicItemJson`, `GetLuckFactor` |
| `UniqueLegendaryHelper.TryGetLegendaryInfo` / `GetSetForLegendaryItem` | `GetLegendaryInfoJson`, `GetLegendaryIDs` |
| `MagicItemComponent.SetMagicItem` | `ApplyMagicItemJson`, or `TryMakeMagicItem` |
| `MagicItemEffectDefinitions.GetAvailableEffects` / `AllDefinitions` | `GetAvailableEffectTypes`, `GetAllMagicEffectTypes` |
| Polling for "did this item change?" | `AddMagicItemChangedListener` |
| Reflecting or patching `MagicTooltip`, `MagicItem.GetTooltip`, `GetSetTooltip`, or parsing `item.GetTooltip()` for Epic Loot's lines | `GetItemTooltipData` |
| Transpiling Epic Loot's tooltip builders to change a line | `AddTooltipEditor` |
| Patching `InventoryGrid.CreateItemTooltip` / `PatchOnHoverFix` around Epic Loot to draw your own tooltip | `SetTooltipOverride` |

Reading Epic Loot's BepInEx config entries directly is **not** promoted to the API and remains unsupported.

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
string GetSetItemColor();                          // the colour of set pieces and set lines (API 3)
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

#### Saving items changed in place

What `getItems` offers is also what the table may *change*: Enchant, Augment, Rune Etch, a Rune Extract in
a Reduce mode, Set Etch, a Set Extract that strips the set, and Disenchant all rewrite the target item
rather than consuming it. Epic Loot writes the change to your instance, but only you can save it where it
lives. A vanilla chest does not save on its own here, and it re-reads its ZDO after anyone opens it, so an
unsaved change is undone after the materials were spent (a disenchant also hands back the socketed stones
first, so the revert duplicates them).

```csharp
API.RegisterInventoryProvider("my.plugin.guid", ...);           // first
API.RegisterInventoryProviderSaveHandler("my.plugin.guid", item =>
{
    Container chest = FindContainerHolding(item);
    if (chest == null || !chest.m_nview.IsOwner()) return false;  // Epic Loot logs that it may not last
    chest.GetInventory().Changed();                                // Container.OnContainerChanged saves it
    return true;
});
```

- It runs on the frame of the change, right after Epic Loot re-checked (through `getItems`) that you still
  offer the item. Before paying for an in-place change Epic Loot asks `getItems` again, so a container that
  reloaded since the list was built fails that check instead of taking the materials.
- **Do not reload the container before saving.** A reload (`Container.Load`) replaces every item instance
  from the ZDO and discards the change you were handed.
- Without a save handler your items are still offered for in-place changes, and the first one logs a
  warning naming your id. Players can keep a provider's items to spending only by listing its id under the
  server setting `Spend-Only Storage Mods` (Enchanting Table section), which defaults to
  `Azumatt.AzuCraftyBoxes`. Listed providers still supply materials and runes and can have items
  sacrificed or identified.
- `GetRegisteredProviders()["InventorySave"]` lists the providers with a save handler.

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

## Item tooltip (API 3)

Epic Loot works out its part of an item's tooltip once, as data, and draws it from that data. You can read
the data, change it before Epic Loot draws it, or take over drawing the inventory tooltip yourself.

```csharp
string GetItemTooltipData(ItemDrop.ItemData item, int qualityLevel, bool includeDetails, bool localize);
bool   AddTooltipEditor(string id, Func<ItemDrop.ItemData, string, string> editor);
bool   RemoveTooltipEditor(string id);
bool   SetTooltipOverride(string ownerId, bool overriding);
bool   IsTooltipOverridden();
string GetItemTooltipWithoutEpicLoot(ItemDrop.ItemData item, int qualityLevel, bool crafting,
                                     float worldLevel, int stackOverride);
```

### The data

`GetItemTooltipData` returns JSON, or `""` for a null item or before a local player exists.
`qualityLevel < 1` means the item's own quality. `includeDetails` fills what Epic Loot shows while Shift is
held. `localize` runs the text fields through the game's localization; ids, kinds and colours are never
localized.

The data is **Epic Loot's part of the tooltip only**: the title and rarity, the stat lines magic can change,
effects, sockets, sets, shard and set-rune previews, and the key hints. Plain vanilla lines (crafter,
teleport, food, ...) are not in it. A trimmed example for an enchanted sword:

```json
{ "Schema": 1, "Quality": 3, "IncludeDetails": false,
  "Kind": "Magic", "IsMagic": true, "Title": "<color=#2BFFFF>Sharp Iron Sword</color>",
  "Rarity": 1, "RarityName": "$mod_epicloot_rare", "RarityColor": "#2BFFFF", "ItemTypeName": "iron sword",
  "MaterialRarity": -1, "Description": "$item_sword_iron_description",
  "Stats": [
    { "Id": "$item_weight", "Value": 0.8, "Modified": false },
    { "Id": "$inventory_slash", "Value": 81.2, "Min": 64.9, "Max": 81.2, "Modified": true },
    { "Id": "$item_staminause", "Value": 12.0, "Modified": false } ],
  "Effects": [
    { "EffectType": "ModifyPhysicalDamage", "Value": 16, "Text": "+16% Physical Damage",
      "Marker": "Plain", "RollMin": 10, "RollMax": 20 } ],
  "Sockets": { "Count": 2, "Used": 1, "Open": 1, "Entries": [
    { "SourcePrefab": "Red_Rare_ShardStone", "Rarity": 1, "EffectType": "AddFireDamage", "Value": 5,
      "Text": "+5 Fire Damage", "State": "Active", "StackMultiplier": 1, "Removal": "Free" } ] },
  "Sets": [], "Hints": [ { "Id": "socket_open", "Text": "[<color=yellow><b>$KEY_Use</b></color>] $mod_epicloot_press_socket" } ],
  "Extra": [] }
```

- `Kind`: `Magic`, `Shard`, `SetRune`, `Chisel` (Brokkr's Gift), `Runestone`, `MagicMaterial` or `Mundane`.
  `IsMagic` is true when Epic Loot draws the whole tooltip.
- `Stats` are keyed by the vanilla label token they are drawn with (`$item_weight`, `$inventory_slash`,
  `$se_runstamina`, ...). A line is in the list exactly when Epic Loot draws it. `Modified` says magic
  changed it. `Secondary` is the figure in brackets (stack weight, block power at skill, the player's total on
  a modifier line); `Min`/`Max` are a damage line's roll range, or durability as current / maximum. `Variant`
  marks the special lines: `Bloodlust`, `Indestructible`, and `Percent` for the second `$item_healthuse` line.
- Effect `Marker` is `Plain`, `Augmented` or `Tempered`. Socket `State` is `Active`, `InactiveRune` (rune
  socketing is off) or `InertShard`. `Removal` is `Free`, `BreakOnly` or `Locked`.
- `Sets` holds the vanilla set first when an item is in both kinds. Each has its pieces and bonuses, with
  `Active`, the `Tier` an active bonus applies at, and the description and activation key with details.
  `SetColor` is the colour the set label, set sections and set rune lines are drawn in (the player's Set
  Item Color setting, also `GetSetItemColor()`).
- Rarities are indices, as everywhere in this API (`-1` for none).

The shim mirrors all of it as `ItemTooltipData` and friends, so `item.GetTooltipData()` hands you objects.

### Changing what Epic Loot draws

An editor gets the data as JSON and returns the edited JSON, or `null` to leave it. Epic Loot's own tooltip,
Auga's and `GetItemTooltipData` all see the edited data. Change a value, remove an effect or a hint, or add a
line to `Extra`:

```json
"Extra": [ { "Anchor": "AfterEffects", "Text": "<color=#7fff7f>Forged by my mod</color>" } ]
```

`Anchor` is `AfterHeader`, `AfterStats`, `AfterEffects`, `AfterSet` or `End` (an unknown anchor goes at the
end; on an item Epic Loot does not draw itself, every added line does).

- Editors run in registration order, each getting what the one before returned.
- They run when an item's tooltip is first shown and whenever its data changes while it stays shown, not
  every frame, so an editor may only depend on the item and the data it is given. Return `null` to leave
  the data as it is.
- The data an editor gets is unlocalized, as Epic Loot draws it.
- One that throws or returns something that is not tooltip data is removed from the chain and logged;
  register it again to put it back.
- Plain vanilla lines are not in the data, so an editor cannot change them; patch vanilla for those.

Through the shim: `EpicLoot.AddTooltipEditor(id, (item, data) => { ...; return data; })`.

### Drawing the tooltip yourself

`SetTooltipOverride(yourGuid, true)` tells Epic Loot your plugin draws the inventory item tooltip. While any
plugin holds the override:

- `InventoryGrid.CreateItemTooltip` runs as vanilla, and the `ItemDrop.ItemData.GetTooltip` calls made inside
  it return vanilla's text.
- Epic Loot leaves the inventory tooltip's box alone: no resizing, no scrollbar, no Ctrl comparison box.
- That is all it changes. `GetTooltip` anywhere else (vanilla's crafting panel, other plugins' windows, Epic
  Loot's own panels) keeps Epic Loot's text, set section and crafting material line.
- Draw Epic Loot's part from `GetItemTooltipData`, and include the `Hints`, or players never learn how to
  open sockets. For the rest of the item's text, call `GetItemTooltipWithoutEpicLoot`: it is `GetTooltip`
  with Epic Loot switched off for that one call (other plugins' additions stay). `qualityLevel < 1` and
  `worldLevel < 0` mean the item's own, `stackOverride` `-1` its own stack.

Every claim and release is written to the log at info level whatever the logging settings, naming the
plugin, so a player's log shows who took the tooltip over. Calling it again with the same state does
nothing. Release it with `false` when your plugin turns its tooltip off.

---

## Events

Add and remove listeners with plain method calls, so a reflection-only consumer can subscribe without
binding to an event field.

```csharp
API.AddMagicItemChangedListener((item, reason) => { /* ... */ });
API.AddLootGeneratedListener(item => { /* ... */ });
API.AddBountyCompletedListener((player, monsterId) => { /* ... */ });
API.AddAdventureDataChangedListener(() => { /* ... */ });          // API 2, see Bounties below
API.AddBountyTargetSlainListener(json => { /* ... */ });          // API 2, see Bounties below
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

## Bounties (API 2)

Accepted bounties live in each character's save, not on the server, so a mod that wants to show them to
other players (a party or guild mod, say) reads them on the owner's client and sends them on itself.

```csharp
string GetActiveBounties();   // JSON array, the local player's bounties in this world
int    GetBountyPinType();    // Minimap.PinType as int, -1 before the minimap starts
bool   AddAdventureDataChangedListener(Action listener);        // local client
bool   AddBountyTargetSlainListener(Action<string> listener);   // server / listen host
```

`GetActiveBounties` lists the bounties that are `InProgress` (being hunted) or `Complete` (finished, reward
not yet claimed):

```json
[{ "Id": "Bounty.123.4.BlackForest.Troll", "PlayerId": 123, "MonsterId": "Troll", "Name": "Troll",
   "Biome": "BlackForest", "X": 512.0, "Y": 31.5, "Z": -880.0, "Radius": 47.25, "State": "InProgress",
   "Slain": false, "Adds": [{ "MonsterId": "Greydwarf", "Count": 2 }],
   "RewardIron": 2, "RewardGold": 0, "RewardCoins": 0, "Interval": 4 }]
```

- `X/Y/Z` and `Radius` describe the circle the owner's map shows. The creatures spawn somewhere inside it,
  picked later by whichever machine gets near, so the exact spot is not known in advance.
- `Name` is localized on the owner's client. An add's `Count` is how many are still alive.
- Anyone may kill a bounty's creatures, and the kill always credits the owner, even when the owner is offline
  (the server keeps it until they log in).

`AdventureDataChanged` fires after every write of the local player's adventure data: accepting, a kill,
completing, claiming or abandoning a bounty, buying a treasure map or finding its chest, replayed offline
kills, and the routine save with the character. It has no payload, and the routine save means it can fire
with nothing changed, so read `GetActiveBounties` and compare.

`BountyTargetSlain` fires on the server for every bounty creature that dies, owner online or not, with
`{ "BountyId", "PlayerId", "MonsterId", "IsAdd" }`. That is how a server-side mod keeps an offline owner's
bounty up to date. It also fires for the leftover creatures of an abandoned bounty, which no longer credit
anyone.

Pins drawn with `GetBountyPinType()` get Epic Loot's bounty icon, and the large map's bounty filter button
hides them along with Epic Loot's own.

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
count, unique and set-piece assignment, randomized wear, display name — and raises both `LootRoll` and
`OnLootGenerated`. Check `CanBeMagicItem` first. Passing an unknown `legendaryID`, or one whose
requirements do not fit the item, **fails the roll** rather than silently downgrading it.

To inspect before applying, use `RollMagicItemJson` then `ApplyMagicItemJson`.

Uniques and sets are not tied to one rarity: each carries a `Rarities` list in `legendaries.json`, and any
rarity can be enabled. `GetLegendaryIDs(rarity)` returns every unique and set piece enabled at that
rarity (empty where none are). `AddLegendaryItem(type, json)` and `AddLegendarySet(type, json)` accept any
rarity name; `type` is only the default for an entry whose json has no `Rarities`. `HasLegendarySet`
reports the number of distinct pieces worn, and returns true once enough are worn for every set bonus, at
any mix of rarities.

`AddLootTables` returns an opaque key; pass it back to `UpdateLootTables` to replace what you added.
Registrations are cached and re-applied whenever `loottables.json` reloads or a dedicated server pushes
its copy, so they survive both. (`LootRoller.Initialize` clears the table map, which is why the re-apply
matters — the same contract the content-registration endpoints have always had.)

The JSON takes the same table shape as `loottables.json` — `LeveledLoot` levels plus the optional
`StarScaling`, `Modifiers` and `StarMultiplier` — documented in [LootTables.md](LootTables.md). A table
in the deprecated flat form (`Drops`/`Loot` directly on the table) is still accepted and converted to a
level 1 entry, with a one-time warning in the log. A creature you give a table this way is never touched by
the creature sorter, which only fills in creatures that have none.

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
magicapi query       — every read-only endpoint against the held item
magicapi providers   — registered inventory/equipment/sacrifice provider ids
magicapi events      — toggle a logging listener for change and loot events
magicapi roll <n>    — TryMakeMagicItem on the held item
magicapi tooltip [details]        — the held item's tooltip data and its text without Epic Loot, to the BepInEx log
magicapi tooltipoverride on|off   — claim the item tooltip as a test plugin
magicapi tooltipedit on|off       — a test editor that adds a line and drops the first effect
```

`magicapi events` writes to the BepInEx log, so you can confirm your listener would have fired and with
which reason token.
