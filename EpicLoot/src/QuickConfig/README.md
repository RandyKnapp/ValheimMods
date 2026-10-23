# Quick Configure: prefab ↔ code contract

The Quick Configure panel is **authored in Unity** (`ValheimUnity/Assets/EpicLoot/Prefabs/UI/QuickConfigure/`,
shipped in the `epicloot` bundle) and **bound by code** in this folder. Maintainers move, resize, restyle
and add rows in the editor; the code only finds rows by their GameObject name and wires the named
widget children below. Nothing here positions anything.

The editor-side generator that produced the initial prefabs is
`ValheimUnity/Assets/Scripts/Editor/QuickConfigPrefabBuilder.cs` (menu `Mod/Quick Configure/...`).
It only creates pages that do not exist yet, so hand edits survive; row templates and the shell can be
regenerated on purpose, and "Rebuild Selected Pages" resets just the page prefabs selected in the
Project window to the script's table.

## Rows and pages are identified by GameObject name (no baked components)

The prefabs carry **no EpicLoot components**, by design: they stay independent of the mod assembly,
so a rebuilt or renamed `EpicLoot.dll` can never turn a row into a missing script, and the prefabs
open cleanly in an editor that has no mod DLLs at all. (Historically the editor could not load
`EpicLoot.dll` either, until the MagicaCloth 2 asset satisfied `assembly_valheim`'s reference.) Instead:

- A row's **GameObject name is its key** (`GlobalDropRateModifier`, `json:adventuredata:Gamble.GamblesCount`,
  `action:preset:balanced`). Rename the object to rebind it. Decorative rows keep their template name
  (`Row_Header`, `Row_Text`) and are not bound.
- A **`Row_Group`** (a plain `HorizontalLayoutGroup` + `LayoutElement` in the column, not a template) puts
  several rows side by side; its children are bound exactly like the column's own. Give it
  `LayoutElement.flexibleHeight = 0`, or its force-expanded children make it take the column's spare
  height. The enchanting table's feature-level lines are groups (a `Row_Text` name, then the default and
  max `Row_TextField`s with their `Label` switched off), and so are the Loot Drops page's boss drop lines
  (a `Row_Cycle` mode, then its range `Row_Slider` showing only its `Value`).
- An optional tooltip override is an **inactive child named `Tooltip`** holding a `TMP_Text`; when
  present its text replaces the setting's own description.
- A page prefab's root name is the page id (`Page_Balance`); its title token is derived by code
  (`$mod_epicloot_cfg_page_balance`). An optional inactive child `Title` with a `TMP_Text` overrides it.

## Row key rule (the row GameObject's name)

| Key shape | Meaning | Examples |
|---|---|---|
| `<ELConfig static field>` | a BepInEx entry on `EpicLoot.Config.ELConfig` | `GlobalDropRateModifier`, `_adventureModeEnabled`, `ShowQuickConfigButton` |
| `<field>.<part>` | one part of a composite entry | `AbilityKeyCodes.0`, `AbilityBarPosition.x` |
| `Common.<Entry>` | a Common-layer entry on `Common.ModContext` | `Common.EnableDebugMode`, `Common.ConfigApplyDelay` |
| `json:<file>:<path>` | a value in a baseconfig JSON (host or server admin) | `json:adventuredata:Gamble.GamblesCount`, `json:loottables:RarityCounts`, `json:magiceffects:TripleBowShot.Chance`, `json:magiceffects:Riches` |
| `action:<verb>:<arg>` | a button | `action:preset:balanced`, `action:reset:TraderPanelPosition`, `action:url:discord`, `action:advanced:show` |
| `readout:<name>` | code-fed text | `readout:dropmix`, `readout:template` |

Unknown keys log one warning and the row is disabled. A registered key with no row is simply not shown.
`Row_Color` rows carry the colour key; the icon-index entry is derived (`_<rarity>MaterialIconColor`).

## Widget children (found by name inside the row)

Every row root: `RectTransform` + `LayoutElement`, named after its key, laid out by the column's
`VerticalLayoutGroup`. Inside the row a `HorizontalLayoutGroup` places the children; the code never
sets positions. Texts are TextMeshPro (`TMP_Text` / `TMP_InputField`); the placeholder font is replaced at
runtime by `QuickConfigStyle` (`MagicFontManager`). The same pass gives every `Image` whose sprite name
starts with `woodpanel` vanilla's `litpanel` material, so the panel backgrounds dim with the day/night
light like vanilla's own panels. That material exists only in the game, so the prefabs keep the default
one; buttons and fields stay unlit, as in vanilla.

Every scroll list (a row's `Items`, a WeightList's `Items`, the picker's `List`) carries a vertical
`Scrollbar` child wired to its `ScrollRect` with `AutoHide`, so it shows whenever there is more than fits.
The same style pass gives each `ScrollRect` the scroll fix the enchanting table and merchant panel use:
scroll sensitivity 800 (since Call to Arms a wheel notch moves a list far less) and a handle pinned at
0.4 of the track, so a long list never shrinks it to a sliver.

| Template | Children the code binds |
|---|---|
| `Row_Toggle` | `Label`, `Toggle` (uGUI `Toggle`) |
| `Row_Slider` | `Label`, `Slider` (uGUI `Slider`), `Value` (`TMP_InputField`, typed value, two-way) |
| `Row_Cycle` | `Label`, `Cycle` (`Button` whose child `Text` shows the current option) |
| `Row_Picker` | `Label`, `Field` (`TMP_InputField`, free text), `Pick` (`Button` opening the picker overlay) |
| `Row_TextField` | `Label`, `Field` (`TMP_InputField`), optional `Status` (`TMP_Text` under the field) |
| `Row_Flags` | `Label`, `Flags` (container) → `Flag` template (`Toggle` + `Label`); code clones `Flag` per enum member and hides the template |
| `Row_Button` | `Button` (`Button` + child `Text`), optional `Label` (text beside it) |
| `Row_Header` | `Label` |
| `Row_Text` | `Label` (wrapping; readouts and notes) |
| `Row_Color` | `Label`, `Swatch` (`Image`), `Field` (`TMP_InputField`, name or `#hex`), `Pick` (`Button`), `Cycle` (`Button` + `Text`, icon index) |
| `Row_EffectConfigs` | `Head` → `Label`, `Effect` (`Button` + child `Text`: the shown effect; opens the picker over every loaded effect with a Config, editable ones first sorted by name, read-only ones last marked "(read-only)"), `Add` (`Button`, shown only for open-key effects, i.e. Riches); `Items` (`ScrollRect`; `content` holds an inactive `Item` template: `Key` (`TMP_InputField`, read-only unless open-key), `Pick` (`Button`, ObjectDB item picker, open-key effects in a world only), `Value` (`TMP_InputField`, float), `Remove` (`Button`, open-key only)). Key `json:magiceffects:EffectConfigs`; writes `shardstones.json` and/or `magiceffects.json` depending on the effect. Each item row carries its key's localized label as tooltip; the row tooltip sits on `Head`. |
| `Row_Bounties` | `Head` → `Label`, `Biome` (`Button` + child `Text`: the shown biome; opens the picker over the file's biomes plus every registry biome), `Add` (`Button`); `Items` (`ScrollRect`; `content` holds an inactive `Item` template: `Target` (`TMP_InputField`, creature prefab name), `Pick` (`Button`, monster picker; hidden when nothing is known), `Iron`, `Gold`, `Coins` (`TMP_InputField` ints), `Remove` (`Button`)). Key `json:adventuredata:Bounties.Targets`; every biome is staged, the shown one is row UI state. The tooltip sits on `Head`. |
| `Row_BiomeCosts` | `Head` → `Label`; `Items` (`ScrollRect`; its `content` holds an inactive `Item` template, cloned per biome of `TreasureMap.BiomeInfo`: `Name` (`TMP_Text`, read-only biome name), `Cost` (`TMP_InputField` int coins, -1 = not offered), `Tokens` (`TMP_InputField` int forest tokens)). No Add/Remove: the biome set is the file's. Key `json:adventuredata:TreasureMap.BiomeInfo`. The tooltip sits on `Head`. |
| `Row_BiomeDrops` | `Head` → `Label`, `Biome` (`Button` + child `Text`: the shown biome; opens the picker over the biomes that have drop tables, plus "Other"), `Target` (`Button` + child `Text`: the shown loot table, e.g. "Tier1Mob · level 2 (Greydwarf, Skeleton)"; opens the picker over the biome's tables), optional `Prev` / `Next` (`Button`s stepping through them); `Amount` (a WeightList with open keys: how many items drop) and `Rarity` (a WeightList with fixed keys: one chance per rarity). Key `json:loottables:LootTables`; every table is staged, the shown biome and table are row UI state. The tooltip sits on `Head`. |
| `Row_RarityCounts` | `Head` → `Label`, `Rarity` (`Button` + child `Text`: the shown rarity; opens the picker); `Enchantments` and `Sockets` (WeightLists with open keys). Key `json:loottables:RarityCounts` (MagicEffectsCount and SocketCounts); every rarity is staged, the shown one is row UI state. The tooltip sits on `Head`. |
| `Row_UpgradeCosts` | `Head` → `Label`, `Feature` (`Button`: the shown enchanting table feature); `Items` (`ScrollRect` whose `content` lays out an inactive `Level` template, cloned per level of the shown feature: `Head` → `Label` ("Unlock" for level 0, then "Level N"), `Add` (`Button`); then an `Item` template cloned per cost item: `Field` (`TMP_InputField`, item prefab name), `Pick` (`Button`, ObjectDB item picker, hidden when no item list is known), `Amount` (`TMP_InputField` int), `Remove` (`Button`)). When `content` carries a `GridLayoutGroup`, the code sets its cell height to fit the level with the most items. Key `json:enchantingupgrades:UpgradeCosts`; the levels shown run from Unlock to the feature's staged `MaximumFeatureLevels` value, and a level added by raising it is written with an `UpgradeValues` entry copied from the top level. The tooltip sits on `Head`. |

A **WeightList** is a named child of a row (`Enchantments`, `Amount`, ...) holding a count:chance table
whose chances should add up to 100: `Head` → `Label`, `Total` (`TMP_Text`, the warning shown only while
they do not), `Add` (`Button`, open keys only); `Items` (`ScrollRect`; `content` holds an inactive `Item`
template: `Count` (`TMP_InputField` int, open keys) or `Name` (`TMP_Text`, fixed keys), `Slider` (0-100),
`Value` (`TMP_InputField` int), `Remove` (`Button`, open keys)). The sliders move freely; on Save each
changed table is rebalanced to 100, the chances the player changed keeping their values and the others
filling the rest (`WeightTable.Rebalance`). A table left untouched is not rewritten, even when it is off.
| `Row_ItemCategories` | `Head` → `Label`, `Category` (`Button` + child `Text`: the shown category; opens the picker over every `ItemInfo[].Type`), `Add` (`Button`); `Items` (`ScrollRect`; `content` holds an inactive `Item` template: `Boss` (`Button` + child `Text`: boss key; opens the picker over `none` + the known keys), `Field` (`TMP_InputField` item prefab name), `Pick` (`Button`, ObjectDB item picker, hidden on the main menu), `Remove` (`Button`)). Key `json:iteminfo:ItemInfo`; every category is staged, the shown one is row UI state. The tooltip sits on `Head`. |

Overlays, instantiated by code under `GUIManager.CustomGUIFront`:

- `QuickConfigPicker`: `Panel` → `Title`, `Search` (`TMP_InputField`), `List` (`ScrollRect`, content holds
  an `Entry` template `Button` + `Text`), `Close` (`Button`).
- `QuickConfigConfirm`: `Panel` → `Title`, `Body`, `Keep`, `Discard`, `SaveClose` (`Button`s).

Shell `QuickConfigPanel` (root stretched full screen, carries vanilla `Localize`):
`Overlay` (dim `Image`, blocks clicks) → `Panel` → `Title`, `Close`, `Pages` (empty container the page
prefabs are instantiated into), `Status`, `Back`, `Reset`, `Save`, `Next`, `Finish`.

Pages `Pages/Page_<Name>` (root stretched, named after the page id): one `Column` or `Left` + `Right`
columns, each with a `VerticalLayoutGroup`; children are nested instances of the row templates, each
renamed to its key. Page order lives in code (`QuickConfigureTool.PageOrder`); a page prefab missing
from the bundle is skipped with a warning.

## Basic and advanced pages

Only the basic pages (`QuickConfigureTool.BasicPages`: `Page_Welcome` in first-time setup, then
`Page_Balance`) are in the Back/Next sequence until the player presses the `action:advanced:show` button;
`action:advanced:hide` takes the others out again. Both are `Row_Button`s on the Balance page, in the same
place, and code shows one at a time. Pressing one acts at once instead of staging anything: it writes the
client `.cfg` entry `Show Advanced Quick Configure Pages`, so the choice is remembered. Edits on hidden pages
stay staged and Save still writes them. When no basic page carries an `action:advanced:show` row, every page
is shown, so a bundle without the button never strands the rest.

## Reset Page

The shell's `Reset` button stages the shipped default of every row on the shown page the player may
edit (after a confirm); Save writes it like any other edit, and other pages are untouched. A .cfg row
takes its BepInEx default. A JSON row takes the value from the file the mod ships (the embedded
baseconfig, `ShippedDefaults`; magiceffects.json is the overhaul of the staged Balance Template): a
simple value by the dotted path in its key, a table through its slot's `Default`, which merges by identity
so an entry the shipped file lacks (a loot table, a bounty biome, an item category another mod added)
keeps its current values. A slot without a default is left alone.

## Text and localization

Row `Label` text is plain English in the prefab (maintainers edit it in place). Any label, title or
button text that starts with `$` is run through `Localization.instance.Localize` by the code, so a
label can be switched to a token later. Page titles, nav buttons, the welcome text and status strings are
`$mod_epicloot_cfg_*` tokens in `localizations/English.json`.

## Rules for the save path

- `.cfg` writes happen in one batch (`SaveOnConfigSet` off, one `Save()`), only for entries whose staged
  value differs from the baseline taken when the panel opened.
- JSON writes edit the on-disk baseconfig as a `JObject` (only the changed tokens) and reload it through
  `ELConfig.ReloadBaseConfigsFromDisk`. They **never** call `ConfigVersionManager.RecordWrittenContent`:
  panel edits are the player's work, and stamping them as the mod's would let the next update overwrite
  them silently. The Balance preset rewrite from the embedded overhaul *does* record itself.
- JSON rows and the preset buttons are editable by the host and by a server admin
  (`QuickConfigBindings.CanEditFiles`: Jotunn's `PlayerIsAdmin` off-host), read-only for anyone else.
  The shared launcher only shows the panel to those two anyway.

## Saving as a server admin

A server admin's `.cfg` changes travel through Jotunn's config sync as before. Their JSON changes are
written on the server by `QuickConfigServerSave`, over one CustomRPC (`epicloot_quickconfig_RPC`):

1. The `.cfg` batch is applied and pushed first, so the server already has a new Balance Template.
2. **Fetch**: the client asks for the server's current text of every file with a staged change.
3. The client applies the staged changes to that text with the host's own code
   (`JsonConfigEdits.ApplyStaged`, the slots' writers) and sends each edited file back with a hash of
   the text it edited. With a preset pressed, it asks for the magiceffects.json rewrite instead of
   sending edits to that file, as the host drops them.
4. **Write**: the server checks the sender against its admin list (vanilla `ZNet.IsAdmin` on the peer's
   host name) and that each file still hashes the same, then rewrites magiceffects.json if asked (backing
   up a hand-edited one; there is no one to ask), writes the files in `StagedConfig.JsonFileOrder` and
   reloads them, which pushes them to every connected player. A file that will not load is put back.
5. The save completes when the server's reply **and** the push of every file it wrote have arrived
   (`ELConfig.ServerConfigApplied`), since only the push puts the saved values in the admin's game.
   Until then the pages are locked and Save waits; a closed panel gets the outcome on the HUD.

The server only checks, writes and reloads; it never builds the panel's bindings. File names from the
client are checked against `StagedConfig.JsonFileOrder` before they become paths. Each step times out
(20 s for a reply, 15 s for the push), so a lost answer never leaves the panel locked.
