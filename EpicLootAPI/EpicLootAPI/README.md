# Epic Loot API

API designed to help developers extend [Epic Loot](https://valheim.thunderstore.io/package/RandyKnapp/EpicLoot/) with their own **magic effects**, **legendary items**, **sets**, and **abilities**.  
This wrapper provides convenience classes and reflection-based accessors to register new content into Epic Loot.

---

## 📦 Installation

You can use the API in one of two ways:

### 1. Bundle as DLL (Recommended)

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

Copy API.cs and EffectTypes.cs into your plugin project.
⚠️ Do not modify the provided methods unless you know what you are doing.

### Using API

After you finish defining all your content, make sure to `Register` them to EpicLoot

If you need to update your custom classes, use the API `Update` functions

### Example Magic Effect

```c#
public void Awake()
{
    var Definition = new MagicItemEffectDefinition("Blink", "Blink", "Teleport to impact point");
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
```c#
var legendary = new LegendaryInfo(LegendaryType.Mythic, "EndlessCrossbow", "Rusty Crossbow", "Gods have favored you");
legendary.Requirements.AllowedSkillTypes.Add(Skills.SkillType.Crossbows);
legendary.GuaranteedMagicEffects.Add("AddFrostDamage", 5, 10, 10);
legendary.GuaranteedMagicEffects.Add("Indestructible");
legendary.GuaranteedEffectCount = 3;
```

### Example Legendary Set
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
```c#
BountyTarget bounty = new BountyTarget(Heightmap.Biome.Meadows, "Boar");
bounty.Adds.AddMinion("Neck", 2);
bounty.RewardCoins = 100;
```