using EpicLoot.LegendarySystem;
using EpicLoot.MagicItemEffects;
using EpicLoot.ShardStones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EpicLoot
{
    public enum ItemRarity
    {
        Magic,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    [Serializable]
    public class MagicItemEffect
    {
        public const float DefaultValue = 1;

        public int Version = 1;
        public string EffectType { get; set; }
        public float EffectValue;

        public MagicItemEffect(string type, float value = DefaultValue)
        {
            EffectType = type;
            EffectValue = value;
        }
    }

    // A single effect socketed into an item via a Runestone or Shard.
    // Stores enough to apply the effect and to reconstruct the original
    // socketed item (Runestone/Shard) when it is removed from the socket.
    [Serializable]
    public class SocketedEffect
    {
        public int Version = 2;
        public MagicItemEffect Effect;  // null for an inert shard (no effect for the host item type)
        public string SourcePrefab;     // e.g. "EtchedRunestoneEpic" / "Yellow_Epic_ShardStone"
        public ItemRarity SourceRarity; // for tooltip range + reconstruction
        public ShardType ShardType = ShardType.None; // set for shard sockets; None for runestones

        // The share of its normal value this socket ended up contributing, < 1 only when same-color
        // stacking decay was applied (see ShardSocketManager.RecomputeSocketValues). Effect.EffectValue
        // already has it baked in; this is kept purely so the tooltip can say why the number is low.
        // Defaults to 1 so items saved before stacking existed deserialize unchanged.
        public float StackMultiplier = 1f;

        public SocketedEffect()
        {
        }

        public SocketedEffect(MagicItemEffect effect, string sourcePrefab, ItemRarity sourceRarity)
        {
            Effect = effect;
            SourcePrefab = sourcePrefab;
            SourceRarity = sourceRarity;
        }
    }

    [Serializable]
    public class MagicItem
    {
        public int Version = 3;
        public ItemRarity Rarity;
        public List<MagicItemEffect> Effects = new List<MagicItemEffect>();
        public string TypeNameOverride;
        public int AugmentedEffectIndex = -1;
        public List<int> AugmentedEffectIndices = new List<int>();
        public List<int> TemperedEffectIndices = new List<int>();
        public string DisplayName;
        public string LegendaryID;
        public string SetID;
        public bool IsUnidentified = false;
        public int SocketCount = 0;
        public List<SocketedEffect> Sockets = new List<SocketedEffect>();

        public MagicItem()
        {
        }

        public MagicItem(int version, ItemRarity rarity, string typeNameOverride, int augmentedEffectIndex, List<int> augmentedEffectIndices, string displayName, string legendaryID, string setID, bool isUnidentified)
        {
            Version = version;
            Rarity = rarity;
            TypeNameOverride = typeNameOverride;
            AugmentedEffectIndex = augmentedEffectIndex;
            AugmentedEffectIndices = augmentedEffectIndices;
            DisplayName = displayName;
            LegendaryID = legendaryID;
            SetID = setID;
            IsUnidentified = isUnidentified;
        }

        public string GetItemTypeName(ItemDrop.ItemData baseItem)
        {
            return string.IsNullOrEmpty(TypeNameOverride) ? 
                Localization.instance.Localize(baseItem.m_shared.m_name).ToLowerInvariant() : TypeNameOverride;
        }

        // Glyphs are written as escapes, not literals: a CP1252 round-trip corrupted the equivalent
        // characters in EpicLoot.GetMagicEffectPip once already. \u25BE/\u25BC = augmented,
        // \u25B2 = tempered, \u2666/\u25C6 = plain (Auga uses the first of each pair).
        public string GetMagicEffectPip(int effectIndex)
        {
            if (EpicLoot.HasAuga)
            {
                if (HasBeenAugmented(effectIndex))
                {
                    return "\u25BE";
                }

                if (HasBeenTempered(effectIndex))
                {
                    return "\u25B2";
                }

                return "\u2666";
            }
            else
            {
                if (HasBeenAugmented(effectIndex))
                {
                    return "\u25BC";
                }

                if (HasBeenTempered(effectIndex))
                {
                    return "\u25B2";
                }

                return "\u25C6";
            }

        }
        public bool HasBeenAugmented(int index) => AugmentedEffectIndex == index || AugmentedEffectIndices.Contains(index);
        public bool HasBeenTempered(int index) => TemperedEffectIndices.Contains(index);
        public string GetRarityDisplay()
        {
            var color = GetColorString();
            return $"<color={color}>{EpicLoot.GetRarityDisplayName(Rarity)}</color>";
        }

        // True while the player holds Shift, used to reveal the detailed per-effect tooltip block
        // (description, roll range, config values). Shared by the magic-item and loose-shard tooltips.
        public static bool ShowEffectDetails => ZInput.GetKey(KeyCode.LeftShift) || ZInput.GetKey(KeyCode.RightShift);

        public string GetTooltip()
        {
            var showRange = ShowEffectDetails;

            var color = GetColorString();
            var tooltip = new StringBuilder();
            tooltip.Append($"\n<color={color}>");
            for (var index = 0; index < Effects.Count; index++)
            {
                var effect = Effects[index];
                var pip = GetMagicEffectPip(index);
                if (showRange)
                {
                    // Header without the inline range; the range and other details go in the block below.
                    // Close/reopen the rarity color around the dim detail block rather than nesting tags.
                    tooltip.AppendLine($"{pip} {GetEffectText(effect, Rarity, false, LegendaryID)}");
                    tooltip.Append($"</color><color=#c0c0c0ff>");
                    tooltip.Append(GetEffectDetailBlock(effect, Rarity, LegendaryID, null, "   "));
                    tooltip.Append($"</color><color={color}>");
                }
                else
                {
                    tooltip.AppendLine($"{pip} {GetEffectText(effect, Rarity, false)}");
                }
            }

            tooltip.Append($"</color>");

            if (SocketCount > 0)
            {
                tooltip.AppendLine($"$mod_epicloot_sockets ({GetUsedSocketCount()}/{SocketCount}):");
                foreach (SocketedEffect socket in Sockets)
                {
                    if (socket == null)
                    {
                        continue;
                    }
                    var socketColor = EpicLoot.GetRarityColor(socket.SourceRarity);
                    // Inline the socketed item's own icon, resolved from its source prefab
                    var iconTag = ShardTooltipSprites.GetSpriteTag(socket.SourcePrefab);
                    var removalTag = GetSocketRemovalTag(socket);
                    if (socket.Effect != null)
                    {
                        tooltip.AppendLine($"  <color={socketColor}>{iconTag} {GetEffectText(socket.Effect, socket.SourceRarity, false)}</color>{GetSocketStackTag(socket)}{removalTag}");
                        if (showRange)
                        {
                            tooltip.Append($"<color=#c0c0c0ff>");
                            tooltip.Append(GetEffectDetailBlock(socket.Effect, socket.SourceRarity, null, null, "     "));
                            tooltip.Append($"</color>");
                        }
                    }
                    else if (socket.ShardType != ShardType.None)
                    {
                        // An inert shard: socketed but has no defined effect for this item type.
                        tooltip.AppendLine($"  <color={socketColor}>{iconTag} $mod_epicloot_shard_noeffect</color>{removalTag}");
                    }
                }
                for (var i = 0; i < GetOpenSocketCount(); i++)
                {
                    tooltip.AppendLine("  \u25CA<color=#808080> $mod_epicloot_empty_socket</color>");
                }
            }

            tooltip.AppendLine($"$mod_epicloot_itemtooltip_rarity: {GetRarityDisplay()}<pos=75%>" +
                $"$mod_epicloot_itemtooltip_effects: <color={color}>{Effects.Count}</color>");

            return tooltip.ToString();
        }

        // Marks sockets the current config won't let the player simply empty. Driven by the removal
        // policy rather than by the kind of line being written: an effect with no rarity-scaled value
        // (Warmth, say) renders as a perfectly normal effect line yet is break-only under
        // ShardSocketMode.BreakValueless, so without the marker there would be no cue.
        private static string GetSocketRemovalTag(SocketedEffect socket)
        {
            switch (ShardSocketManager.GetRemovalPolicy(socket))
            {
                case SocketRemoval.BreakOnly:
                    return " <color=#808080>$mod_epicloot_socket_tip_breakonly</color>";
                case SocketRemoval.Locked:
                    return " <color=#808080>$mod_epicloot_socket_tip_permanent</color>";
                default:
                    return "";
            }
        }

        // Marks a socket whose value was cut by same-color stacking decay, so the reduced number on the
        // line above reads as the rule working rather than as a bug. Nothing is shown at full strength,
        // which is every socket unless ShardStackMode.Diminishing is on.
        private static string GetSocketStackTag(SocketedEffect socket)
        {
            if (socket.StackMultiplier >= 1f)
            {
                return "";
            }

            var percent = Mathf.RoundToInt(socket.StackMultiplier * 100f);
            var label = string.Format(Localization.instance.Localize("$mod_epicloot_socket_tip_stacked"), percent);
            return $" <color=#808080>{label}</color>";
        }

        public string GetCompactTooltip() {
            var color = GetColorString();
            var tooltip = new StringBuilder();
            tooltip.Append($"<color={color}>");
            for (var index = 0; index < Effects.Count; index++) {
                tooltip.AppendLine($"{GetEffectText(Effects[index], Rarity, true)}");
            }
            tooltip.Append($"</color>");

            return tooltip.ToString();
        }

        public Color GetColor()
        {
            if (ColorUtility.TryParseHtmlString(GetColorString(), out Color color))
            {
                return color;
            }
            return Color.white;
        }

        public string GetColorString()
        {
            return EpicLoot.GetRarityColor(Rarity);
        }

        // Socket helpers
        public int GetUsedSocketCount() => Sockets.Count;
        public int GetOpenSocketCount() => Mathf.Max(0, SocketCount - Sockets.Count);
        public bool HasSockets() => SocketCount > 0;
        public bool HasOpenSocket() => GetOpenSocketCount() > 0;
        public IEnumerable<MagicItemEffect> GetSocketedEffects() => Sockets.Where(x => x?.Effect != null).Select(x => x.Effect);

        // includeSocketed defaults to false so all crafting/bookkeeping reads (loot roll, augment,
        // rune-extract, disenchant, requirements/exclusivity, names) stay rolled-only. Effect
        // application and tooltip reads pass includeSocketed: true.
        public List<MagicItemEffect> GetEffects(string effectType = null, bool includeSocketed = false)
        {
            IEnumerable<MagicItemEffect> source = Effects;
            if (includeSocketed && Sockets.Count > 0)
            {
                source = source.Concat(GetSocketedEffects());
            }
            return effectType == null ? source.ToList() : source.Where(x => x.EffectType == effectType).ToList();
        }

        // includeSocketed defaults to true here: GetTotalEffectValue is only called from effect
        // application (per-item patches) and tooltip display, never from crafting/bookkeeping, so
        // socketed effects should always count toward the aggregate value.
        public float GetTotalEffectValue(string effectType, float scale = 1.0f, bool includeSocketed = true)
        {
            return SumEffectValue(effectType, scale, includeSocketed);
        }

        // Allocation-free equivalent of GetEffects(effectType, includeSocketed).Sum(...) * scale.
        // Used on the hot per-item effect-application patches (armor/durability) which are called from
        // vanilla UI/render loops, so it iterates the backing lists directly rather than building
        // intermediate LINQ lists/iterators each call.
        public float SumEffectValue(string effectType, float scale = 1.0f, bool includeSocketed = true)
        {
            float total = 0f;
            for (int i = 0; i < Effects.Count; i++)
            {
                if (Effects[i].EffectType == effectType)
                {
                    total += Effects[i].EffectValue;
                }
            }

            if (includeSocketed)
            {
                for (int i = 0; i < Sockets.Count; i++)
                {
                    MagicItemEffect effect = Sockets[i]?.Effect;
                    if (effect != null && effect.EffectType == effectType)
                    {
                        total += effect.EffectValue;
                    }
                }
            }

            return total * scale;
        }

        public bool HasEffect(string effectType, bool checkHealthCritical = false, bool includeSocketed = false)
        {
            if (Effects.Exists(x => x.EffectType == effectType))
            {
                return true;
            }
            if (includeSocketed && Sockets.Exists(x => x?.Effect != null && x.Effect.EffectType == effectType))
            {
                return true;
            }
            return checkHealthCritical && HasEffect(ModifyWithLowHealth.EffectNameWithLowHealth(effectType), false, includeSocketed);
        }

        public bool HasAnyEffect(IEnumerable<string> effectTypes)
        {
            return Effects.Any(x => effectTypes.Contains(x.EffectType));
        }

        public bool CanBeDisenchanted()
        {
            return !Effects.Any(x => !MagicItemEffectDefinitions.Get(x.EffectType)?.CanBeDisenchanted ?? false);
        }

        // Per-effect override that supplies the full ordered set of {0},{1},... format args for an
        // effect's DisplayText, derived from the item's rolled value. Effects that show more than one
        // number (e.g. BulkUp: +health/-regen, or a rolled value plus a hardcoded constant) register a
        // provider here; everything else defaults to a single {0} = value. Providers must be PURE (they
        // are probed with dummy values to count/classify placeholders) and return float values.
        private static readonly Dictionary<string, Func<float, object[]>> DisplayValueProviders = new();

        public static void RegisterDisplayValues(string effectType, Func<float, object[]> provider)
        {
            if (string.IsNullOrEmpty(effectType) || provider == null)
            {
                return;
            }
            DisplayValueProviders[effectType] = provider;
        }

        // Builds the {0},{1},... args for an effect's DisplayText from its rolled value, using the
        // registered provider if any. Always returns at least one element so string.Format has a {0}.
        private static object[] GetDisplayArgs(string effectType, float value)
        {
            if (!string.IsNullOrEmpty(effectType) && DisplayValueProviders.TryGetValue(effectType, out var provider))
            {
                var args = provider(value);
                if (args != null && args.Length > 0)
                {
                    return args;
                }
            }
            return new object[] { value };
        }

        // Formats a DisplayText with the given args, falling back to the raw localized text if the
        // string references a placeholder index the args don't cover (guards a mis-authored
        // localization/provider mismatch from throwing out of a whole tooltip/panel).
        private static string FormatDisplayText(string displayText, object[] args)
        {
            var localizedDisplayText = Localization.instance.Localize(displayText);
            try
            {
                return string.Format(localizedDisplayText, args);
            }
            catch (FormatException e)
            {
                EpicLoot.LogWarning($"DisplayText format error for '{localizedDisplayText}': {e.Message}");
                return localizedDisplayText;
            }
        }

        public static string GetEffectText(MagicItemEffectDefinition effectDef, float value)
        {
            return FormatDisplayText(effectDef.DisplayText, GetDisplayArgs(effectDef.Type, value));
        }

        // Range preview (compendium/enchant/augment): fills each placeholder with that derived value's
        // own min-max range. A constant slot (min == max, e.g. a fixed "200") collapses to a single
        // number rather than a range. A null value def (valueless effect) fills placeholders with "".
        public static string GetEffectTextRange(MagicItemEffectDefinition effectDef, MagicItemEffectDefinition.ValueDef values)
        {
            if (values == null)
            {
                var count = GetDisplayArgs(effectDef.Type, 0f).Length;
                var empties = new object[count];
                for (var i = 0; i < count; i++)
                {
                    empties[i] = string.Empty;
                }
                return FormatDisplayText(effectDef.DisplayText, empties);
            }

            var argsMin = GetDisplayArgs(effectDef.Type, values.MinValue);
            var argsMax = GetDisplayArgs(effectDef.Type, values.MaxValue);
            var display = new object[argsMin.Length];
            for (var i = 0; i < argsMin.Length; i++)
            {
                var min = argsMin[i];
                var max = i < argsMax.Length ? argsMax[i] : argsMin[i];
                display[i] = (min is float fMin && max is float fMax && !Mathf.Approximately(fMin, fMax))
                    ? $"({min}-{max})"
                    : $"{min}";
            }
            return FormatDisplayText(effectDef.DisplayText, display);
        }

        // Builds args where rolled-value slots become valueToken but constant slots (e.g. a fixed
        // "200"/"2x") keep their real value, detected by probing the provider with two different inputs:
        // a slot whose value changes is rolled-value; a slot that stays put is constant.
        private static object[] GetGenericArgs(string effectType, string valueToken)
        {
            var probeA = GetDisplayArgs(effectType, 1f);
            var probeB = GetDisplayArgs(effectType, 2f);
            var args = new object[probeA.Length];
            for (var i = 0; i < probeA.Length; i++)
            {
                var isConstant = i < probeB.Length && Equals(probeA[i], probeB[i]);
                args[i] = isConstant ? probeA[i] : valueToken;
            }
            return args;
        }

        // Generic preview (compendium "explain"/set X-marker, tempering old->new transition): the
        // rolled-value placeholders get valueToken; constant placeholders show their real value.
        public static string GetEffectTextGeneric(MagicItemEffectDefinition effectDef, string valueToken)
        {
            return FormatDisplayText(effectDef.DisplayText, GetGenericArgs(effectDef.Type, valueToken));
        }

        public static string GetEffectText(MagicItemEffect effect, ItemRarity rarity,
            bool showRange, string legendaryID, MagicItemEffectDefinition.ValueDef valuesOverride)
        {
            var effectDef = MagicItemEffectDefinitions.Get(effect.EffectType);
            var result = GetEffectText(effectDef, effect.EffectValue);
            var values = valuesOverride ?? (string.IsNullOrEmpty(legendaryID) ?
                effectDef.GetValuesForRarity(rarity) :
                UniqueLegendaryHelper.GetLegendaryEffectValues(legendaryID, effect.EffectType));
            if (showRange && values != null)
            {
                if (!Mathf.Approximately(values.MinValue, values.MaxValue))
                {
                    result += $" [{values.MinValue}-{values.MaxValue}]";
                }
            }
            return result;
        }

        public static string GetEffectText(MagicItemEffect effect, ItemRarity rarity,
            bool showRange, string legendaryID = null)
        {
            return GetEffectText(effect, rarity, showRange, legendaryID, null);
        }

        public static string GetEffectText(MagicItemEffect effect, MagicItemEffectDefinition.ValueDef valuesOverride)
        {
            return GetEffectText(effect, ItemRarity.Legendary, false, null, valuesOverride);
        }

        // Builds the indented, per-effect detail lines shown under an effect when Shift is held: the
        // localized Description (with the X marker filled in with this item's rolled value), the roll
        // range on its own line, and any Config values with human-readable labels. Every piece is
        // optional and omitted when the effect has no data for it, so an effect with none of them
        // contributes nothing. Returns raw text (no color tags); callers wrap it in a dim color.
        public static string GetEffectDetailBlock(MagicItemEffect effect, ItemRarity rarity,
            string legendaryID, MagicItemEffectDefinition.ValueDef valuesOverride, string indent)
        {
            var effectDef = MagicItemEffectDefinitions.Get(effect.EffectType);
            if (effectDef == null)
            {
                return string.Empty;
            }

            var values = valuesOverride ?? (string.IsNullOrEmpty(legendaryID) ?
                effectDef.GetValuesForRarity(rarity) :
                UniqueLegendaryHelper.GetLegendaryEffectValues(legendaryID, effect.EffectType));

            var block = new StringBuilder();

            if (!string.IsNullOrEmpty(effectDef.Description))
            {
                block.Append($"{indent}{GetEffectDescription(effectDef, effect.EffectValue)}\n");
            }

            if (values != null && !Mathf.Approximately(values.MinValue, values.MaxValue))
            {
                block.Append($"{indent}$mod_epicloot_detail_range: [{values.MinValue}-{values.MaxValue}]\n");
            }

            if (effectDef.Config != null && effectDef.Config.Count > 0)
            {
                foreach (var kvp in effectDef.Config)
                {
                    block.Append($"{indent}{effectDef.GetConfigLabel(kvp.Key)}: {kvp.Value:0.###}\n");
                }
            }

            return block.ToString();
        }

        // Concrete-value description (Shift detail block, shard preview): substitutes any {0},{1},...
        // placeholders with this effect's provider values, then fills the legacy
        // "<b><color=yellow>X</color></b>" marker with the rolled value. Descriptions using neither
        // convention pass through unchanged (string.Format is a no-op without placeholders).
        public static string GetEffectDescription(MagicItemEffectDefinition effectDef, float value)
        {
            var description = FormatDisplayText(effectDef.Description, GetDisplayArgs(effectDef.Type, value));
            return ApplyValueToDescription(description, value);
        }

        // Generic description (compendium "explain" page): substitutes {0},{1},... with the generic
        // marker (constant slots keep their real value); the legacy "X" marker is already generic and is
        // left in place.
        public static string GetEffectDescriptionGeneric(MagicItemEffectDefinition effectDef, string valueToken)
        {
            return FormatDisplayText(effectDef.Description, GetGenericArgs(effectDef.Type, valueToken));
        }

        // Replaces the standard "X" value marker embedded in a localized Description with this item's
        // rolled value, preserving the marker's styling. The handful of non-standard markers (X/2, X%)
        // don't match and stay generic.
        private static string ApplyValueToDescription(string localizedDescription, float value)
        {
            return localizedDescription.Replace("<b><color=yellow>X</color></b>",
                $"<b><color=yellow>{value:0.#}</color></b>");
        }

        public void ReplaceEffect(int index, MagicItemEffect newEffect)
        {
            if (index < 0 || index >= Effects.Count)
            {
                EpicLoot.LogError("Tried to replace effect on magic item outside of the range of the effects list!");
                return;
            }

            SetEffectAsAugmented(index);

            Effects[index] = newEffect;
        }

        public bool HasBeenAugmented()
        {
            return AugmentedEffectIndex >= 0 && AugmentedEffectIndex < Effects.Count || AugmentedEffectIndices.Count > 0;
        }

        public bool IsEffectAugmented(int index)
        {
            return AugmentedEffectIndex == index || AugmentedEffectIndices.Contains(index);
        }

        public void SetEffectAsAugmented(int index)
        {
            if (AugmentedEffectIndex == index)
            {
                return;
            }

            if (!IsEffectAugmented(index))
            {
                AugmentedEffectIndices.Add(index);
            }
        }

        public int GetAugmentCount()
        {
            var old = AugmentedEffectIndex >= 0 ? 1 : 0;
            return old + AugmentedEffectIndices.Count;
        }

        public bool IsUniqueLegendary()
        {
            return !string.IsNullOrEmpty(LegendaryID);
        }

        public LegendaryInfo GetLegendaryInfo()
        {
            if (IsUniqueLegendary())
            {
                UniqueLegendaryHelper.TryGetLegendaryInfo(LegendaryID, out var itemInfo);
                return itemInfo;
            }

            return null;
        }

        public string GetFirstEquipEffect(out FxAttachMode mode)
        {
            foreach (var effect in Effects)
            {
                var effectDef = MagicItemEffectDefinitions.Get(effect.EffectType);
                if (effectDef != null && !string.IsNullOrEmpty(effectDef.EquipFx))
                {
                    mode = effectDef.EquipFxMode;
                    return effectDef.EquipFx;
                }
            }

            mode = FxAttachMode.None;
            return null;
        }

        public bool IsLegendarySetItem()
        {
            return !string.IsNullOrEmpty(SetID);
        }
    }
}
