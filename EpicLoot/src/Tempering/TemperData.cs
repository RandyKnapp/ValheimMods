using System.Collections.Generic;
using System.Linq;
using System.Text;
using EpicLoot.Config;
using EpicLoot.LegendarySystem;
using UnityEngine;

namespace EpicLoot;

public class TemperData
{
    private const string SUCCESS_GREEN = "#22bb33";
    public const string FAIL_RED = "#CC0000";
    private const string CRIT_SUCCESS_GOLD = "#FFD700";

    private static float BASE_CHANCE => Mathf.Clamp01(ELConfig.TemperBaseChance.Value);
    private static float OVER_TEMPERED_DECREMENT => Mathf.Clamp01(ELConfig.TemperDecrement.Value);
    public static bool CAN_DESTROY_ITEM => ELConfig.TemperDestroysItem.Value;
    public static float DESTROY_CHANCE => Mathf.Clamp01(ELConfig.TemperChanceToDestroy.Value);

    private readonly ItemDrop.ItemData itemData;
    private readonly MagicItem magicItem;
    private readonly MagicItemEffect selectedEffect;
    private MagicItemEffectDefinition selectedDefinition;
    public readonly MagicItemEffectDefinition.ValueDef selectedValues;

    private readonly string rarityColor;

    private readonly MagicItem _tempItem;
    private float _tempValue;
    private float _tempIncrement;
    private float _tempUpdatedValue;
    private MagicItemEffect _tempEffect;

    public readonly float probability;
    // Rolled by RollOutcome when the temper actually executes (materials consumed), not at selection
    // time -- re-selecting an enchantment in the panel must not silently reroll a pending outcome.
    public bool success { get; private set; }
    private bool critical;
    private readonly int indexOfEffect;

    // The value range a temper of this effect works against: the legendary/mythic guaranteed-effect
    // range when the item is a unique, the rarity table otherwise -- the same resolution the detailed
    // tooltip uses (MagicItem.GetEffectDetailBlock). Null means the effect has no rolled value at this
    // rarity (a valueless effect like Warmth) and cannot be tempered.
    private static MagicItemEffectDefinition.ValueDef ResolveValues(MagicItem magicItem,
        MagicItemEffectDefinition def)
    {
        return string.IsNullOrEmpty(magicItem.LegendaryID)
            ? def.GetValuesForRarity(magicItem.Rarity)
            : UniqueLegendaryHelper.GetLegendaryEffectValues(magicItem.LegendaryID, def.Type);
    }

    // Whether the effect can be tempered at all: it needs a value range and a positive increment to
    // step by. The enchantment list uses this to leave untemperable effects out entirely.
    public static bool IsTemperable(MagicItem magicItem, MagicItemEffect effect)
    {
        MagicItemEffectDefinition def = MagicItemEffectDefinitions.Get(effect.EffectType);
        if (def == null)
        {
            return false;
        }

        MagicItemEffectDefinition.ValueDef values = ResolveValues(magicItem, def);
        return values != null && values.Increment > 0f && values.MaxValue > 0f;
    }

    public TemperData(ItemDrop.ItemData itemData, string effectType)
    {
        this.itemData = itemData;

        magicItem = itemData.GetMagicItem();
        selectedEffect = magicItem.GetEffects(effectType)[0];
        selectedDefinition = MagicItemEffectDefinitions.Get(effectType);
        selectedValues = ResolveValues(magicItem, selectedDefinition);
        rarityColor = EpicLoot.GetRarityColor(magicItem.Rarity);

        // The panel filters untemperable effects out via IsTemperable, so this only trips if a config
        // reload removed the effect's values mid-session. A zero probability keeps the button disabled
        // rather than throwing out of the click handler.
        if (selectedValues == null)
        {
            probability = 0f;
            return;
        }

        probability = CalculateProbability();

        _tempItem = new MagicItem
        {
            Version = magicItem.Version,
            Rarity = magicItem.Rarity,
            TypeNameOverride = magicItem.TypeNameOverride,
            AugmentedEffectIndex = magicItem.AugmentedEffectIndex,
            // Copied, not aliased: OnSuccess mutates these lists, and sharing them with the live
            // item mutated it before (and independently of) the save.
            AugmentedEffectIndices = new List<int>(magicItem.AugmentedEffectIndices),
            TemperedEffectIndices = new List<int>(magicItem.TemperedEffectIndices),
            DisplayName = magicItem.DisplayName,
            LegendaryID = magicItem.LegendaryID,
            SetID = magicItem.SetID,
            IsUnidentified = magicItem.IsUnidentified,
            // SaveMagicItem replaces the payload wholesale -- omitting the socket state destroyed
            // every socketed shard/runestone (and the socket capacity) on both temper outcomes.
            SocketCount = magicItem.SocketCount,
            Sockets = magicItem.Sockets.Select(socket => new SocketedEffect
            {
                Version = socket.Version,
                Effect = socket.Effect != null
                    ? new MagicItemEffect(socket.Effect.EffectType, socket.Effect.EffectValue)
                    : null,
                SourcePrefab = socket.SourcePrefab,
                SourceRarity = socket.SourceRarity,
                ShardType = socket.ShardType,
                StackMultiplier = socket.StackMultiplier
            }).ToList()
        };

        for (int i = 0; i < magicItem.Effects.Count; ++i)
        {
            MagicItemEffect effect = magicItem.Effects[i];
            MagicItemEffect newValue = new MagicItemEffect(
                effect.EffectType,
                effect.EffectValue);

            if (newValue.EffectType == effectType)
            {
                _tempValue = newValue.EffectValue;
                _tempIncrement = selectedValues.Increment;
                newValue.EffectValue += selectedValues.Increment;
                _tempUpdatedValue = newValue.EffectValue;
                _tempEffect = newValue;
                indexOfEffect = i;
            }
            _tempItem.Effects.Add(newValue);
        }
    }

    // Decides this temper's outcome. Called by TemperPanel.OnTemper at execution (after the
    // requirements are consumed), so selecting/deselecting in the panel can never reroll it.
    public void RollOutcome()
    {
        // Defensive-path instance (no value range resolved): never succeeds, never crits, and the
        // OnFail guard leaves the item untouched.
        if (selectedValues == null || _tempItem == null)
        {
            success = false;
            critical = false;
            return;
        }

        success = UnityEngine.Random.value <= probability;
        critical = UnityEngine.Random.value <= GetCriticalSuccessChance();
    }

    public void OnSuccess()
    {
        if (critical)
        {
            _tempEffect.EffectValue += _tempIncrement;
            _tempUpdatedValue = _tempEffect.EffectValue;
        }
        if (!_tempItem.TemperedEffectIndices.Contains(indexOfEffect))
        {
            _tempItem.TemperedEffectIndices.Add(indexOfEffect);
        }
        API.WithChangeReason(API.ChangeReason.Temper, () => itemData.SaveMagicItem(_tempItem));
        UpdateLog(critical ? CRIT_SUCCESS_GOLD : SUCCESS_GREEN);

    }
    public void OnFail()
    {
        if (_tempItem == null)
        {
            return;
        }

        _tempItem.Effects.Clear();

        string effectToReduce = SelectWeightedEffect(magicItem.Effects);
        for (int i = 0; i < magicItem.Effects.Count; ++i)
        {
            MagicItemEffect effect = magicItem.Effects[i];
            MagicItemEffect newValue = new MagicItemEffect(
                effect.EffectType,
                effect.EffectValue);

            if (effect.EffectType == effectToReduce)
            {
                MagicItemEffectDefinition def = MagicItemEffectDefinitions.Get(effect.EffectType);
                MagicItemEffectDefinition.ValueDef values = def != null ? ResolveValues(magicItem, def) : null;
                // SelectWeightedEffect only offers effects with a value range, but guard anyway -- a
                // failed lookup must degrade to "nothing reduced", never throw mid-temper.
                if (values != null)
                {
                    selectedDefinition = def;
                    _tempValue = newValue.EffectValue;
                    _tempIncrement = values.Increment;
                    // A failure can never push an effect below the worst legitimate roll for the
                    // rarity. An effect already at (or somehow below) MinValue just stays put.
                    newValue.EffectValue = Mathf.Max(values.MinValue,
                        newValue.EffectValue - values.Increment);
                    _tempUpdatedValue = newValue.EffectValue;
                    _tempEffect = newValue;
                }
            }

            _tempItem.Effects.Add(newValue);
        }

        API.WithChangeReason(API.ChangeReason.Temper, () => itemData.SaveMagicItem(_tempItem));
        UpdateLog(FAIL_RED);
    }
    private void UpdateLog(string color)
    {
        string message = MagicItem.GetEffectTextGeneric(selectedDefinition, $"{_tempValue} → {_tempUpdatedValue}");
        TemperPanel.Instance.UpdateLog($"<color={color}>{message}</color>");
    }
    // Picks the effect a failure reduces, weighted by how filled-out each effect's roll is. Only
    // effects with a value range participate: a valueless effect (Warmth) has nothing to reduce, and
    // handing one back would NRE the reduction above.
    private string SelectWeightedEffect(List<MagicItemEffect> effects)
    {
        List<MagicItemEffect> candidates = new List<MagicItemEffect>();
        List<float> weights = new List<float>();
        float maxInclusive = 0.0f;
        for (int i = 0; i < effects.Count; ++i)
        {
            MagicItemEffect effect = effects[i];
            MagicItemEffectDefinition def = MagicItemEffectDefinitions.Get(effect.EffectType);
            MagicItemEffectDefinition.ValueDef values = def != null ? ResolveValues(magicItem, def) : null;
            if (values == null || values.MaxValue <= 0f) continue;
            float num = effect.EffectValue / values.MaxValue;
            candidates.Add(effect);
            weights.Add(num);
            maxInclusive += num;
        }

        if (candidates.Count == 0)
        {
            // Nothing reducible on the item; the selected effect is temperable by construction, so
            // fall back to it -- OnFail's null guard then leaves the item untouched if even that fails.
            return selectedEffect.EffectType;
        }

        float random = UnityEngine.Random.Range(0.0f, maxInclusive);
        float value = 0.0f;
        for (int i = 0; i < candidates.Count; ++i)
        {
            value += weights[i];
            if (value >= random)
            {
                return candidates[i].EffectType;
            }
        }

        return candidates[candidates.Count - 1].EffectType;
    }
    public string GetTooltip()
    {
        bool showRange = ZInput.GetKey(KeyCode.LeftShift) ||
                         ZInput.GetKey(KeyCode.RightShift) ||
                         ZInput.GetButton("JoyLStick") ||
                         ZInput.GetButton("JoyRStick");

        StringBuilder sb = new StringBuilder();

        if (_tempItem == null)
        {
            return string.Empty;
        }

        sb.Append($"<color={rarityColor}>{itemData.GetDisplayName()}\n\n");
        for (int i = 0; i < _tempItem.Effects.Count; ++i)
        {
            MagicItemEffect effect = _tempItem.Effects[i];
            string pip = _tempItem.GetMagicEffectPip(i);
            MagicItemEffectDefinition def = MagicItemEffectDefinitions.Get(effect.EffectType);

            string result;
            if (effect.EffectType == selectedEffect.EffectType)
            {
                result = MagicItem.GetEffectTextGeneric(def,
                    $"{effect.EffectValue - selectedValues.Increment} <color={SUCCESS_GREEN}>→ {effect.EffectValue}</color>");
            }
            else
            {
                result = MagicItem.GetEffectText(def, effect.EffectValue);
            }
            if (showRange)
            {
                MagicItemEffectDefinition.ValueDef values = ResolveValues(magicItem, def);
                if (values != null && !Mathf.Approximately(values.MinValue, values.MaxValue))
                {
                    result += $"\n[{values.MinValue}-{values.MaxValue}]";
                }
            }

            sb.AppendLine($"{pip} {result}");
        }
        sb.Append("</color>");
        return sb.ToString();
    }
    private float CalculateProbability()
    {
        float overTemperedModifier = 0f;
        if (selectedEffect.EffectValue > selectedValues.MaxValue && selectedValues.Increment > 0f)
        {
            float difference = selectedEffect.EffectValue - selectedValues.MaxValue;
            float increments = difference / selectedValues.Increment;
            overTemperedModifier = increments * OVER_TEMPERED_DECREMENT;
        }
        return 1 - Mathf.Clamp01(selectedEffect.EffectValue / selectedValues.MaxValue - (BASE_CHANCE - overTemperedModifier));
    }
    public float GetCriticalSuccessChance() => Mathf.Clamp01(1 - selectedEffect.EffectValue / selectedValues.MaxValue);

    public MagicItem GetUpdatedMagicItem() => _tempItem;
}
