using Common;
using System;
using System.Collections.Generic;

namespace EpicLoot.LegendarySystem
{
    [Serializable]
    public class GuaranteedMagicEffect
    {
        public string Type;
        public MagicItemEffectDefinition.ValueDef Values;
        // Optional per-rarity override of Values, for uniques and sets enabled at several rarities. A
        // rarity left out falls back to Values (see UniqueLegendaryHelper.ResolveValues).
        public MagicItemEffectDefinition.ValuesPerRarityDef ValuesPerRarity;
    }

    [Serializable]
    public class TextureReplacement
    {
        public string ItemID;
        public string MainTexture;
        public string ChestTex;
        public string LegsTex;
    }

    [Serializable]
    public class LegendaryInfo
    {
        public string ID;
        public string Name;
        public string Description;
        // The rarities this unique can roll at. Left empty here because Newtonsoft appends to a
        // pre-filled list: UniqueLegendaryHelper backfills it from the block the entry came from
        // (LegendaryItems -> Legendary, MythicItems -> Mythic). A set piece always takes its set's list.
        public List<ItemRarity> Rarities = new List<ItemRarity>();
        // Default instance: an entry that omits "Requirements" must not be null (legendary rolls
        // call CheckRequirements on every entry).
        public MagicItemEffectRequirements Requirements = new MagicItemEffectRequirements();
        public List<GuaranteedMagicEffect> GuaranteedMagicEffects = new List<GuaranteedMagicEffect>();
        public int GuaranteedEffectCount = -1;
        public float SelectionWeight = 1;
        public string EquipFx;
        public FxAttachMode EquipFxMode = FxAttachMode.Player;
        public List<TextureReplacement> TextureReplacements = new List<TextureReplacement>();
        // Kept for config compatibility only: set membership comes from the sets' LegendaryIDs, and a
        // mismatch is reported at load.
        public bool IsSetItem;
        public bool Enchantable;
        public List<RecipeRequirementConfig> EnchantCost = new List<RecipeRequirementConfig>();
    }

    [Serializable]
    public class SetBonusInfo
    {
        public int Count;
        public GuaranteedMagicEffect Effect;
    }

    [Serializable]
    public class LegendarySetInfo
    {
        public string ID;
        public string Name;
        // The rarities the set's pieces can roll at. Empty here for the same reason as
        // LegendaryInfo.Rarities; backfilled from the blocks its pieces are in (LegendaryItems -> Legendary,
        // MythicItems -> Mythic), since that is where the pieces dropped before sets had a rarity list.
        public List<ItemRarity> Rarities = new List<ItemRarity>();
        public List<string> LegendaryIDs = new List<string>();
        public List<SetBonusInfo> SetBonuses = new List<SetBonusInfo>();
        public List<string> FullSetFx = new List<string>();
        public ArmorTintInfo ArmorTint;
        // Code-built visuals while the full set is worn (SetVisuals): a glow fitted to the weapon in hand, the same kind
        // of glow on both hands, an arrow on the string while a bow is drawn, and an aura around the wearer. All optional.
        public WeaponGlowInfo WeaponGlow;
        public WeaponGlowInfo HandGlow;
        public DrawArrowInfo DrawArrow;
        public BodyAuraInfo BodyAura;
    }

    [Serializable]
    public class ArmorTintInfo
    {
        public string Color;
        public float Strength = 1f;
        public float Hue;
        public float Saturation;
        public float Value;
    }

    // Every field is optional and kept in range where it is read (SetVisuals); these are the defaults.
    [Serializable]
    public class WeaponGlowInfo
    {
        public string Color = "#FFFFFF";
        public float Alpha = 0.3f;               // each glow particle's opacity
        public float Rate = 40f;                 // glow particles a second
        public float Size = 1f;                  // multiplies the glow particles' 0.1-0.28 m
        public float Speed;                      // metres a second the glow drifts out from the weapon
        public float Glints;                     // star glints a second (0 for none)
        public string GlintColor;                // defaults to a paler Color
        public string Accent;                    // "Prefab/child/path": vanilla particles laid along the weapon too
        public string AccentColor;               // recolours the accent; left as it is when unset
        public float AccentScale = 1f;
        public float LightIntensity = 0.8f;
        public float LightRange = 2.5f;
    }

    // Every field is optional and kept in range where it is read (SetDrawArrow); these are the defaults.
    [Serializable]
    public class DrawArrowInfo
    {
        public string Model = "ArrowFrost/model"; // "Prefab/child/path": the arrow mesh shown on the string, as it is
        public float Length = 0.9f;               // metres, the arrow scaled to it (0 keeps the model's own size)
        public string Color = "#FFFFFF";          // the glow particles along it
        public float Alpha = 0.3f;
        public float Rate = 60f;
        public float Size = 0.5f;                 // multiplies the glow particles' 0.1-0.28 m
        public string Accent;                     // "Prefab/child/path": vanilla particles laid along the arrow too
        public string AccentColor;                // recolours the accent; left as it is when unset
        public float AccentScale = 1f;
        public float LightIntensity = 0.8f;
        public float LightRange = 2f;
    }

    [Serializable]
    public class BodyAuraInfo
    {
        public string Preset;                    // the code-built aura: "FallingLeaves"
        public string Color = "#FFFFFF";         // each particle is tinted between Color and Color2
        public string Color2;
        public float Density = 1f;               // multiplies how many particles it makes
    }

    [Serializable]
    public class LegendaryItemConfig
    {
        // Every unique and set, whatever rarities it rolls at, belongs in these two blocks.
        public List<LegendaryInfo> LegendaryItems = new List<LegendaryInfo>();
        public List<LegendarySetInfo> LegendarySets = new List<LegendarySetInfo>();
        // Legacy per-rarity blocks, still loaded and merged into the ones above: a unique here defaults to
        // Mythic, and a set to the blocks its pieces are in.
        // Kept so existing patches that target $.MythicItems / $.MythicSets keep applying.
        public List<LegendaryInfo> MythicItems = new List<LegendaryInfo>();
        public List<LegendarySetInfo> MythicSets = new List<LegendarySetInfo>();
    }
}
