# Epic Loot API

Typed wrappers for integrating with [Epic Loot](https://valheim.thunderstore.io/package/RandyKnapp/EpicLoot/),
so you never need a hard assembly reference or a Harmony patch on its internals.

Two things live here:

- **Extending Epic Loot with new content** — magic effects, legendary items and sets, abilities,
  material conversions, sacrifices, bounties. That is what most of this README covers.
  (Recipes are deprecated — see [Example Recipe](#example-recipe--deprecated).)
- **Integrating with Epic Loot's behaviour** — inventory and equipment providers, sacrifice filters,
  lifecycle events, queries, and loot generation. Summarized under
  [Integration hooks](#integration-hooks) below; full reference in
  [EpicLoot/docs/API.md](../../EpicLoot/docs/API.md).

Everything is reflection-bound against `EpicLoot.API`, so a missing Epic Loot means a logged warning and a
no-op, not a crash. Check before you start:

```csharp
if (!EpicLootAPI.EpicLoot.IsLoaded()) return;   // Epic Loot absent or too old
```

---

## Installation

You can use the API in one of two ways:

### 1. Bundle as DLL

Include `EpicLootAPI.dll` into your project and bundle it into your plugin using [ILRepack](https://github.com/ravibpatel/ILRepack.Lib.MSBuild.Task).

**Example `ILRepack.targets`:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
    <Target Name="ILRepacker" AfterTargets="Build">
        <ItemGroup>
            <InputAssemblies Include="$(TargetPath)" />
            <InputAssemblies Include="$(OutputPath)\EpicLootAPI.dll" />
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

### Using API

- New effects, sets etc must all be registered to Epicloot

---

## Integration hooks

These are for integrating functionality into Epicloot. Primary if you need to provide compatibility between Epicloot and your mod.

### Inventory provider — source enchanting materials from elsewhere

Epic Loot spends the player's own inventory first and only charges the shortfall to providers.

```c#
EpicLoot.RegisterInventoryProvider(
    id:              "my.plugin.guid",
    getItems:        () => NearbyContainerItems(),                       // live instances, not copies
    countItem:       name => CountInContainers(name),                    // by m_shared.m_name
    removeItem:      (name, amount) => TakeFromContainers(name, amount),  // returns amount removed
    removeExactItem: (item, amount) => TakeExact(item, amount));          // match by reference!
```

`removeExactItem` must match by reference, not by name — magic data lives on the item instance, so a
name match will consume the wrong enchanted item.

### Equipment provider

Equipment providers are a way to ensure that equipment that is registered in custom slots count towards Epicloots effect totals.

Feeds `GetMagicEquipment`, so contributed items count toward effect totals, set bonuses, tooltips and
shard socketing alike. Non-magic items are filtered out for you.

```c#
EpicLoot.RegisterEquipmentProvider("my.plugin.guid", player => MyExtraSlots(player));

// Effect totals are memoized and only invalidate on vanilla EquipItem/UnequipItem.
// If your slots change outside those, say so or stale values keep being served:
player.InvalidatePlayerEffectCache();
```

### Sacrifice filter
Stopping items from being sacrificed

```c#
EpicLoot.RegisterSacrificeFilter("my.plugin.guid", item => !IsInMyQuickSlot(item));
```

### Custom item slots — rarity backgrounds on slots you draw yourself

Epic Loot decorates the vanilla inventory grid and hotkey bar with Harmony *transpilers*. If your mod
reimplements `InventoryGrid.UpdateGui` or `HotkeyBar.UpdateIcons` (a prefix returning `false`, say), the
original body never runs and your slots end up with no rarity background. Call this per element instead,
wherever you fill the slot in:

```c#
EpicLoot.ApplyMagicItemBackground(element.m_go, element.m_equiped, item, inventoryGrid: false);
```

Pass `null` for `item` to clear a slot. `inventoryGrid: true` also draws the legendary set marker. Child
images are created only when a slot first needs one, so calling this every frame — and for empty slots —
is cheap.

### Events
Event listeners provide hooks to common interaction points. The most frequently fired of these will be `OnMagicItemChanged`
which provides a number of different reasons that equipment changes, you will likely need to filter to the specific reason you need to take action on.

```c#
EpicLoot.AddMagicItemChangedListener((item, reason) =>
{
    if (reason == EpicLoot.ChangeReason.Socket) { /* ... */ }
});

EpicLoot.AddLootGeneratedListener(item => { /* ... */ });
EpicLoot.AddBountyCompletedListener((player, monsterId) => { /* ... */ });
```

`OnMagicItemChanged` fires on every magic-data write. Reason tokens: `Enchant`, `Augment`, `Disenchant`,
`Rune`, `Temper`, `Socket`, `Unsocket`, `LootRoll`, `Transfer`, and `Unspecified` for writes with no
dedicated call site. Item loading does not raise anything.

### Queries
Trying to determine if an item is a magic-item? `IsMagicItem` exposes the internal magic check.

```c#
if (item.IsMagicItem() && item.TryGetRarity(out ItemRarity rarity))
{
    string colour = EpicLoot.GetRarityColor(rarity);
}

bool ours   = item.IsEpicLootItem();        // instead of sniffing "$mod_epicloot"
var costs   = item.GetEnchantCosts(ItemRarity.Epic);
var salvage = item.GetSacrificeProducts();  // empty = not sacrificeable
MagicItem data = item.GetMagicItem();
```

### Loot generation
You can now roll Epicloot in the same way that Epicloot does, if you need to provide appropriate Epicloot drops.

```c#
if (item.CanBeMagicItem())
{
    float luck = EpicLoot.GetLuckFactor(dropPoint);
    item.TryMakeMagicItem(ItemRarity.Epic, luck);

    // or forcing a named legendary -- fails rather than downgrading if it does not fit
    item.TryMakeMagicItem(ItemRarity.Legendary, luck, "HeimdallLegs");
}

// Inspect before applying
MagicItem rolled = EpicLoot.RollMagicItem(ItemRarity.Rare, item, luck);
item.ApplyMagicItem(rolled);
```

### Example Magic Effect
Add a custom magic effect, with its own specific requirements.
_Implementing_ the actual effect is on you. In this example, a patch on Projectile.OnHit.

```c#
public void Awake()
{
    EpicLoot.RegisterMagicEffectRequirement(
        "MyMod.RequiresBow",
        (itemData, magicItem, effectType, checkLootRoll, checkAugmentRoll, checkRuneRoll) =>
            itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow);

    var Definition = new MagicItemEffectDefinition("Blink", "Blink", "Teleport to impact point");
    Definition.Requirements.ExternalRequirements.Add("MyMod.RequiresBow");
    Definition.Requirements.AllowedSkillTypes.Add(Skills.SkillType.Bows, Skills.SkillType.Spears);
    Definition.Requirements.AllowedRarities.Add(ItemRarity.Epic, ItemRarity.Legendary, ItemRarity.Mythic);
    Definition.SelectionWeight = 1;
}

[HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
private static class Projectile_Setup_Patch
{
    private static void Postfix(Projectile __instance, Vector3 hitPoint)
    {
        if (__instance.m_owner is not Player player) return;
        if (!EpicLoot.HasActiveMagicEffectOnWeapon(null, __instance.m_weapon, "Blink", out float _)) return;

        player.TeleportInstant(hitPoint, player.transform.rotation);
    }
}

private static void TeleportInstant(this Player player, Vector3 position, Quaternion rotation)
{
    player.transform.position = position;
    player.transform.rotation = rotation;
}

```

### Example Legendary Item
Add a custom legendary item, with its own specific enchantments, on a specific item type. 

```c#
var legendary = new LegendaryInfo(LegendaryType.Mythic, "EndlessCrossbow", "Rusty Crossbow", "Gods have favored you");
legendary.Requirements.AllowedSkillTypes.Add(Skills.SkillType.Crossbows);
legendary.GuaranteedMagicEffects.Add("AddFrostDamage", 5, 10, 10);
legendary.GuaranteedMagicEffects.Add("Indestructible");
legendary.GuaranteedEffectCount = 3;
```

### Example Legendary Set
Add a custom legendary set. Each of the set entries must also be added.

```C#
LegendarySetInfo DragonSet = new LegendarySetInfo(LegendaryType.Mythic, "DragonForm", "Dragon Form");
DragonSet.SetBonuses.Add(2, EffectType.ModifyStaminaRegen, 40, 40, 1);
DragonSet.SetBonuses.Add(3, EffectType.AddCarryWeight, 100, 100, 1);
DragonSet.SetBonuses.Add(4, "DragonForm", 1, 1, 1);
DragonSet.LegendaryIDs.Add("DragonChest", "DragonLegs", "DragonCape", "DragonHelmet");

LegendaryInfo DragonChest = new LegendaryInfo(LegendaryType.Mythic,
    "DragonChest", "Dragon Chestpiece", "Cries from the queen ring throughout the fabric of this armor");
DragonChest.IsSetItem = true;
DragonChest.Requirements.AllowedItemTypes.Add("Chest");
DragonChest.GuaranteedEffectCount = 6;
DragonChest.GuaranteedMagicEffects.Add(EffectType.ModifyArmor);
DragonChest.GuaranteedMagicEffects.Add(EffectType.IncreaseStamina);

LegendaryInfo DragonLegs = new LegendaryInfo(LegendaryType.Mythic, "DragonLegs",
    "Dragon Legwarmers", "Padded with the scaly furs of dragons.");
DragonLegs.IsSetItem = true;
DragonLegs.Requirements.AllowedItemTypes.Add("Legs");
DragonLegs.GuaranteedEffectCount = 6;
DragonLegs.GuaranteedMagicEffects.Add(EffectType.AddMovementSkills);
DragonLegs.GuaranteedMagicEffects.Add(EffectType.ModifyMovementSpeedLowHealth);

LegendaryInfo DragonCape = new LegendaryInfo(LegendaryType.Mythic, "DragonCape", "Dragon Cape", "The mere smell of this fabric calls out to the dragons.");
DragonCape.IsSetItem = true;
DragonCape.Requirements.AllowedItemTypes.Add("Shoulder");
DragonCape.GuaranteedEffectCount = 6;

LegendaryInfo DragonHelmet = new LegendaryInfo(LegendaryType.Mythic, "DragonHelmet", "Dragon Helmet", "Marks from the last war of the dragons still flicker on this helmet.");
DragonHelmet.IsSetItem = true;
DragonHelmet.Requirements.AllowedItemTypes.Add("Helmet");
DragonHelmet.GuaranteedEffectCount = 6;
```

### Example Simple Ability 
Best to use simple ability if you are only looking to trigger status effect using hotkey
```c#
SE_Stats SE_DragonForm = ScriptableObject.CreateInstance<SE_Stats>();
SE_DragonForm.name = "SE_DragonForm"
// make sure to register your Status Effect into ObjectDB
AbilityDefinition DragonAbility = new AbilityDefinition("DragonForm", "gdkingheart", 100f, "SE_DragonForm");
DragonAbility.IconAsset = "MyIconName";
EpicLoot.RegisterAsset(MySprite.name, MySprite);
```

### Example Proxy Ability
Proxy abilities generate delegate functions based on defined Proxy class. Inherit from Proxy,
and define your solution.
```c#
AbilityProxyDefinition DragonProxy = new AbilityProxyDefinition("DragonForm", AbilityActivationMode.Activated, typeof(DragonForm));
DragonProxy.Ability.IconAsset = "gdkingheart";
DragonProxy.Ability.Cooldown = 1000f;
```
```c#
public class DragonForm : Proxy
{
    public float m_cooldown;
    public bool m_isTriggered;

    public override bool IsOnCooldown()
    {
        if (m_isTriggered) return false;
        return base.IsOnCooldown();
    }
    
    public override void Activate()
    {
        base.Activate();
        ActivateStatusEffectAction();
    }

    public override void ActivateStatusEffectAction()
    {
        if (Player == null) return;
        if (Player.GetSEMan().HaveStatusEffect("SE_DragonForm".GetStableHashCode()))
        {
            CreatureFormManager.Revert(Player);
            m_isTriggered = false;
        }
        else if (Player.GetSEMan().AddStatusEffect("SE_DragonForm".GetStableHashCode()) is { } statusEffect)
        {
            statusEffect.m_ttl = 1000f;
            m_isTriggered = true;
        }
    }
    
    public override void SetCooldownEndTime(float cooldownEndTime)
    {
        m_cooldown = cooldownEndTime;
    }

    public override float GetCooldownEndTime() => m_cooldown;
}
```

### Example Recipe
`CustomRecipe` is marked `[Obsolete]`. Epic Loot removed `recipes.json` in 0.13.0 and no longer ships a
recipe config, so there is nothing left for these to register into.

```c#
CustomRecipe recipe = new CustomRecipe("Recipe_Rusty", "Iron", CraftingTable.Workbench, 5);
recipe.resources.Add("IronOre", 5);
```

### Example Material Conversion
```c#
MaterialConversion HealthUpgrade_Bonemass = new MaterialConversion(MaterialConversionType.Junk, "Recipe_FaderRunestone_2", "RunestoneMythic");
HealthUpgrade_Bonemass.Resources.Add("HealthUpgrade_Bonemass", 1);
```

### Example Sacrifice
```c#
Sacrifice SacrificeHearts = new Sacrifice();
SacrificeHearts.ItemNames.Add("Bonemass heart", "Elder heart");
SacrificeHearts.AddRequiredItemType(ItemDrop.ItemData.ItemType.Consumable);
SacrificeHearts.Products.Add("ShardMythic", 2);
```
### Example Bounty
Adding a custom bounty
```c#
BountyTarget bounty = new BountyTarget(Heightmap.Biome.Meadows, "Boar");
bounty.Adds.AddMinion("Neck", 2);
bounty.RewardCoins = 100;
```
