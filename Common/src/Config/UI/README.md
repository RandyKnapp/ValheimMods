# In-game config UI

A widget kit for building config panels out of Jotunn's `GUIManager` primitives, plus a **shared
launcher**: one button bottom-right of the main and pause menus that lists every loaded mod which has
registered a panel. Alongside it, a **shared startup popup queue**, so popups that open by themselves on
the main menu show one at a time, and a **per-user first-run record**, so a first-run tutorial greets a
user once rather than once per mod manager profile.

## Dependency rule

**Nothing in this folder may reference `YamlConfigFile`, `YamlConfigManager`, `ConfigNetwork` or
`ValidationReport`.** Its only in-repo dependency is `Logger`.

That is not tidiness — it is what lets a mod with a completely different config system take this folder
and join the shared launcher without also swallowing the YAML framework next door. Keep it true.

## Registering a panel

```csharp
internal static void Init() {
    ConfigUILauncher.Init();
    ApplyRegistration();
}

internal static void ApplyRegistration() {
    if (ValConfig.ShowQuickConfigButton.Value) { ConfigUILauncher.Register("MyMod", OpenPanel); }
    else { ConfigUILauncher.Unregister("MyMod"); }
}
```

Call `Init()` from `Awake`. Wire `ShowQuickConfigButton.SettingChanged` to `ApplyRegistration` so the
entry can be turned off without a restart. With exactly one mod registered the button opens that panel
directly instead of showing a one-item list.

The button is visible **only to a host or a server admin** — a remote non-admin's edits would be
overwritten by the next server broadcast, so offering the editor at all would be a lie. Visibility
re-evaluates on `OnAdminStatusChanged`, so it appears when admin status arrives without a relog.

## Where the button appears

**Main menu and pause menu, never the in-game HUD.** Two GameObjects, one per parent, and exactly one of
them is ever on screen:

| Parent | Shown when |
| --- | --- |
| `GUIManager.CustomGUIFront` | `FejdStartup.instance != null`, i.e. the `start` scene |
| `Menu.m_root` | Valheim shows the pause menu — `m_root` is toggled by the game, so this follows for free |

The `FejdStartup` gate is load-bearing. Jotunn **rebuilds `CustomGUIFront` on every scene change**
(`GUIManager.TryCreateGUI`, wired to `SceneManager.sceneLoaded`), so without it the `main` scene grows its
own copy of the button: one floating over the HUD, and a second one beside the pause menu's, at a
different position because `CustomGUIFront` carries its own `Canvas` and `CanvasScaler`.

Both parents route through the single `EnsureCornerButton(existing, parent)`. It bails when the button it
already holds is still parented to `parent`, then falls back to `parent.Find(ButtonObjectName)` and adopts
that if present, and only creates as a last resort — so `OnCustomGUIAvailable` and the `Menu.Start`
postfix firing repeatedly cannot stack buttons.

The mod list itself closes on its top-right `X` or on **Escape**. In-world that goes through a prefix on
`Menu.Update`, which closes the list and returns `false` for that one frame: Valheim reads Escape inline
there, so swallowing the frame is the only way to keep the press from also collapsing the pause menu the
list was opened from. One press dismisses the list, a second dismisses the menu. In the start scene there
is no `Menu`, so the broker's own `Update` handles it and no suppression is needed.

## The frozen cross-assembly contract

Every mod compiles its **own** `QuickConfigBroker`, so those types are unrelated as far as the CLR is
concerned and no cast between them can ever work. The first copy to run creates a `DontDestroyOnLoad`
GameObject named `ModQuickConfigLauncher`; every copy after that finds it and calls into whichever broker
is already there **by reflection**, binding `Register(string, Action)` by exact signature. Only BCL types
cross the boundary.

```csharp
internal const string BrokerObjectName = "ModQuickConfigLauncher";
internal const string BrokerTypeName   = "QuickConfigBroker";
internal const string ButtonObjectName = "ModQuickConfigButton";
internal const int    ContractVersion  = 2;

public int  BrokerVersion { get; }
public void Register(string modName, Action openPanel);
public void Unregister(string modName);
public bool IsRegistered(string modName);
```

**Amendment rules: additive only.** Never rename a member, reorder or retype a parameter, add a
same-arity overload, or narrow visibility. A newer caller probes with `GetMethod(...) != null` and
degrades silently. A genuine breaking change would need a *new* `BrokerObjectName`, i.e. two buttons on
screen during the transition — so do not make one.

**First broker to create the GameObject wins.** Version mismatches are advisory and logged once at Info,
naming the owning assembly; registration never refuses. The accepted cost is that an old copy inside an
unrelated mod pins the launcher UI at an old version. The alternative — handing the launcher over to a
newer copy mid-session — would leave every other assembly's cached `MethodInfo` pointing at a retired
component, which fails silently and much worse.

## Startup popups

A popup that opens by itself on the main menu — a first-run tutorial, an update notice — goes through the
shared queue instead of opening from its own `FejdStartup.Start` hook. With several mods each doing the
latter, they all wait for the same moment and land on the same frame.

```csharp
ConfigUIStartupPopups.Enqueue("MyMod.UpdateNotice", ConfigUIStartupPopups.OrderNotice,
    tryOpen: () => { ShowNotice(); return notice != null; },
    isOpen:  () => notice != null);
```

Safe to call from `Awake`: the queue waits for the main menu itself, and anything still pending when the
player starts a game shows on their return to the menu. A no-op on a dedicated server.

**When it opens a popup.** The main menu must have been ready — no intro cinematic, `m_mainMenu` and
`m_menuList` active, no `UnifiedPopup`, no connection-failed notice, and no kit panel open — for 1 s
without a break before the first popup of a visit, and 0.5 s between popups. The gap is load-bearing: a
closing panel is destroyed at the end of its frame, and its `MainMenuGuard` shows the main menu again
then — under a popup opened on the same frame, which would never hide it again.

**`tryOpen` and `isOpen`.** `tryOpen` returns true when its popup is now up; false (or a throw) means it
declined, and the next popup gets its turn on the same frame. After that `isOpen` is polled every frame
until it returns false. It must compare Unity objects with `!= null` — never `?.`, `??`,
`ReferenceEquals` or `is null`, which do not see a destroyed object as gone. Queuing a pending key again
replaces its callbacks; queuing the key that is showing is refused.

**Order.** Lower first, equal orders in the order they were queued. Conventions:

| Constant | Value | For |
| --- | --- | --- |
| `OrderWelcome` | 100 | first-run tutorials and welcomes |
| `OrderNotice` | 200 | anything else: update notices, migration prompts |

Offset within a band when a mod has several (`OrderWelcome + 10`).

**"A kit panel is open"** means a direct child of `GUIManager.CustomGUIFront`, active, carrying a
component whose type name is `ConfigUIInputGuard`. Every panel this kit builds has one — `CreatePanel`,
the picker, the prompt, the launcher's list — whichever copy built it, and so should a mod's own overlay.
That type name is part of the contract below.

### The queue's frozen contract

Same arrangement as the launcher, on **its own** `DontDestroyOnLoad` GameObject. Not on the launcher's:
`ConfigUILauncher` only adds a broker to a launcher object it created itself, so had the queue created
that object first, every older copy would find no broker there and lose the Mod Config button. Nor as a
broker method: the broker belongs to whichever copy loaded first, and an older one would switch the queue
off.

```csharp
internal const string QueueObjectName    = "ModStartupPopupQueue";
internal const string QueueTypeName      = "StartupPopupQueue";
internal const string PanelGuardTypeName = "ConfigUIInputGuard";
internal const int    ContractVersion    = 1;

public int  QueueVersion { get; }
public bool Enqueue(string key, int order, Func<bool> tryOpen, Func<bool> isOpen);
public void Cancel(string key);
public bool IsQueued(string key);
```

The launcher's amendment rules apply unchanged: additive only.

## First-run record

Which first-run popups a user has seen, kept **once per user**. A flag in the mod's own cfg lives in one
mod manager profile, and greets the same user again in every other profile. The record is a BepInEx-format
file next to Valheim's own per-user files, which every profile shares:

```
%USERPROFILE%\AppData\LocalLow\IronGate\Valheim\ModQuickConfig\FirstRun.cfg
```

```ini
[FirstRun]
StarLevelSystem = 1
```

One line per mod, keyed by a plain identifier that never changes once shipped. The value is the newest
**revision** of that mod's popup the user has seen; raise the revision in the mod to show a reworked
tutorial again. A write never lowers a value. **Deleting the file brings every mod's popup back.** The
folder, file and section names and the meaning of the value are frozen: every copy of this folder reads
and writes the same file.

Each mod keeps a per-profile override as a client setting, `ConfigEntry<FirstRunMode>`:

| Value | Meaning |
| --- | --- |
| `Auto` | show once per user — the record decides |
| `ShowNextLaunch` | show on the next launch in this profile, then back to `Auto` |
| `Never` | never open by itself in this profile |

```csharp
ConfigUIFirstRun.QueueFirstRunPopup("MyMod", 1, ValConfig.FirstRunPopup, ConfigUIStartupPopups.OrderWelcome,
    open: () => { OpenWelcome(); return welcome != null; },
    isOpen: () => welcome != null);

// in the popup's close, by whatever route:
ConfigUIFirstRun.MarkSeen("MyMod", 1);
```

`ShowNextLaunch` is spent when the popup **opens**, not when it closes, so a popup that itself offers
"show this again next launch" keeps that choice. A mod with no popup does none of this.

Every read and write opens the file afresh rather than holding it for the session: the user may delete
it while the game runs, and a held instance would write every other mod's line back into it. The record
is never read or written on a dedicated server — it may run as the same Windows user, and must not mark
that user's own tutorials seen. A record that exists but cannot be read counts as seen, with a warning,
so an unreadable folder does not reopen the popup every launch.

Retiring an older per-profile flag: `TakeLegacyEntry(cfg, section, key, out raw)` reads its value and
drops the line, so the next save no longer writes it back. Migrate "already seen" with `MarkSeen`.

## Widgets

Layout is **top-left origin**: `anchorMin = anchorMax = pivot = (0,1)`, `anchoredPosition = (x, -y)`.
Build a `List<GameObject>` of rows in reading order, then call `LayoutColumn(rows, x, startY)` once. It
skips inactive rows, so `SetActive(false)` on a conditional row collapses its space — re-run it after any
visibility change to reflow.

| Widget | Use for |
| --- | --- |
| `AddToggleRow` | bool |
| `AddSliderRow` | int/float — slider plus a typed box, bound both ways and clamped |
| `AddEnumCycleRow` | an enum with **≤ 6** members |
| `AddPickerRow` | an enum with more, or any open-ended name (prefabs) |
| `AddEnumFlagsRow` | a small enum used as a set |
| `AddTextFieldRow` | free text |
| `AddStringListEditor` | `List<string>` with add/remove |

`AddCloseX(panel, panelWidth, onClick)` puts a dismiss button in a panel's top-right corner. Prefer it to
a full-width "Close" at the bottom: panel titles are centred so the corner is free, whereas a bottom
button has to be laid out around whatever the last row turns out to be.

Rows inside a `CreateScroll` must use `NewLayoutRow`, not `NewRow`: Jotunn's scroll content carries a
`VerticalLayoutGroup` that overwrites `anchoredPosition`, so those rows size themselves through a
`LayoutElement` and must not be passed to `LayoutColumn`.

**Do not put a Unity `Dropdown` in a Valheim scroll view.** `GUIManager.CreateDropDown` exists, but the
option list is instantiated as a child of the dropdown's own root, and the viewport's `Mask` clips it —
the popup is simply invisible. `ConfigUIPicker` parents to `CustomGUIFront` instead, and its filter box
makes it usable for lists a dropdown never could be.

`AddToggle` carries a workaround for a real Jotunn bug: `CreateToggle` parents with
`SetParent(parent)` and no `worldPositionStays: false`, which corrupts the toggle's scale. **Never call
`CreateToggle` directly** — go through `AddToggle`.

## Input blocking

Every `InputField` in a Valheim UI leaks keystrokes into the game. `CreatePanel` attaches a
`ConfigUIInputGuard` that takes a refcounted `GUIManager.BlockInput` and releases it from `OnDestroy`.
The release is tied to the component's lifecycle **on purpose**: a close-handler release does not run if
an exception is thrown mid-build or the scene changes, and the player is then stuck unable to move with
no way out but a relog.

## Localization

`AddText`, `AddButton` and the picker run their labels through `Localization.instance.Localize`, so pass
`$tokens` for anything your mod owns. Enum *member names* are deliberately left raw — they are the tokens
an admin types into a YAML file, and showing a translated form would teach them the wrong word.

The kit's own words go through `ConfigUI.L(token, english)`: the token's translation when anything
defines it, the English beside it when nothing does (Valheim would otherwise show `[token]`). So the folder
still reads correctly in a mod with no localization set up at all. Where Valheim has a word of its own the
kit uses it, and gets every language the game ships:

| Token | English |
| --- | --- |
| `$menu_close` | Close |
| `$menu_cancel` | Cancel |
| `$menu_ok` | OK |
| `$menu_filter` | Filter |
| `$piece_smelter_add` | Add |

Never put these in a mod's own localization file: Jotunn loads every mod's English over the game's
words, and would replace the translation with English for every player.

The rest are **shared tokens**. Ship them in your English file, so your translators can carry them into
your other languages:

```json
"modquickconfig_button": "Mod Config",
"modquickconfig_list_title": "Configure a mod",
"modquickconfig_no_matches": "No matches",
"modquickconfig_more_matches": "... $1 more, type to narrow",
```

**Their names are frozen, like the launcher contract.** The button and the mod list are drawn by
whichever copy owns the broker, and that copy's tokens are resolved from whatever mods are loaded — so
every copy must use the same names and the same `$1`. Additive only: a new word gets a new token, never a
renamed one. Two mods defining the same token is fine; Jotunn keeps the last, and they agree.

## Dropping this into another mod

Copy `Common/Config/UI/`, then:

1. Make sure your `Logger` exposes `LogDebug` / `LogInfo` / `LogWarning` / `LogError`.
2. Add a `ConfigEntry<bool> ShowQuickConfigButton` (Client config, default true, **not** `IsAdminOnly` —
   it is a per-machine UI preference).
3. Replace `Examples/ExampleConfigPanel.cs` with your own panel and registration.
4. Call your `Init()` from `Awake`.
5. Add the shared `modquickconfig_*` tokens from [Localization](#localization) to your English file.
6. Only if the mod has a first-run popup: add a `ConfigEntry<FirstRunMode> FirstRunPopup` (Client config,
   default `Auto`, not `IsAdminOnly`), pick a record key, and queue the popup as
   `Examples/ExampleWelcomePopup.cs` does. Any other popup that opens by itself on the main menu goes
   through `ConfigUIStartupPopups.Enqueue`.

The broker patches `Menu.Start` with a private Harmony instance keyed on the frozen object name, so a
second mod's copy cannot double-patch it and a plugin calling `Harmony.CreateAndPatchAll(assembly)`
cannot either.
