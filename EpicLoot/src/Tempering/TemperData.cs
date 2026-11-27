using System.Collections.Generic;
using System.Linq;
using System.Text;
using EpicLoot.Config;
using Newtonsoft.Json;
using UnityEngine;

namespace EpicLoot;

public class TemperData
{
    private const string SUCCESS_GREEN = "#22bb33";
    public const string FAIL_RED = "#CC0000";
    private const string CRIT_SUCCESS_GOLD = "#FFD700";

    private static float BASE_CHANCE => Mathf.Clamp01(ELConfig.TemperBaseChance.Value);
    private static float OVER_TEMPERED_DECREMENT => Mathf.Clamp01(ELConfig.TemperDecrement.Value);
    public static bool CAN_DOWNGRADE => ELConfig.TemperFailDowngrades.Value;
    public static bool CAN_DESTROY_ITEM => ELConfig.TemperDestroysItem.Value;
    public static float DESTROY_CHANCE => Mathf.Clamp01(ELConfig.TemperChanceToDestroy.Value);

    private readonly ItemDrop.ItemData itemData;
    private readonly MagicItem magicItem;
    private readonly MagicItemEffect selectedEffect;
    private MagicItemEffectDefinition selectedDefinition;
    public readonly MagicItemEffectDefinition.ValueDef selectedValues;

    private readonly string rarityColor;

    // The success preview the panel shows. It is never saved: SaveToLiveItem writes the outcome.
    private readonly MagicItem _tempItem;
    private float _tempValue;
    private float _tempIncrement;
    private float _tempUpdatedValue;

    public readonly float probability;
    // Rolled by RollOutcome when the temper executes, so re-selecting in the panel never rerolls it.
    public bool success { get; private set; }
    private bool critical;
    private readonly int indexOfEffect;

    // The same range the detailed tooltip shows. Null for an effect with no rolled value at this rarity
    // (a valueless effect like Warmth).
    private static MagicItemEffectDefinition.ValueDef ResolveValues(MagicItem magicItem,
        MagicItemEffectDefinition def)
    {
        return MagicItemEffectDefinitions.GetRollRange(def, magicItem.Rarity, magicItem.LegendaryID);
    }

    // The range the temper panel shows and steps against for this effect on this item. Null for an
    // effect with no registered definition as well: a stand-in's made-up range is nothing to temper by.
    public static MagicItemEffectDefinition.ValueDef GetTemperRange(MagicItem magicItem, MagicItemEffect effect)
    {
        return MagicItemEffectDefinitions.GetRollRange(magicItem, effect.EffectType);
    }

    // A temper needs a value range and a positive increment to step by.
    public static bool IsTemperable(MagicItem magicItem, MagicItemEffect effect)
    {
        MagicItemEffectDefinition.ValueDef values = GetTemperRange(magicItem, effect);
        return values != null && values.Increment > 0f && values.MaxValue > 0f;
    }

    public TemperData(ItemDrop.ItemData itemData, string effectType)
    {
        this.itemData = itemData;

        magicItem = itemData.GetMagicItem();
        selectedEffect = magicItem.GetEffects(effectType)[0];
        // Get() for display (never null); the range itself comes only from a registered definition.
        selectedDefinition = MagicItemEffectDefinitions.Get(effectType);
        selectedValues = GetTemperRange(magicItem, selectedEffect);
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
            AugmentedEffectIndices = new List<int>(magicItem.AugmentedEffectIndices),
            TemperedEffectIndices = new List<int>(magicItem.TemperedEffectIndices),
            DisplayName = magicItem.DisplayName,
            LegendaryID = magicItem.LegendaryID,
            SetID = magicItem.SetID,
            IsUnidentified = magicItem.IsUnidentified,
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
                indexOfEffect = i;
            }
            _tempItem.Effects.Add(newValue);
        }
    }

    // Called by TemperPanel.OnTemper once the requirements are consumed.
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
        float steps = critical ? 2f : 1f;
        if (SaveToLiveItem(value => value + _tempIncrement * steps, markTempered: true))
        {
            UpdateLog(critical ? CRIT_SUCCESS_GOLD : SUCCESS_GREEN);
        }
    }

    public void OnFail()
    {
        if (_tempItem == null)
        {
            return;
        }

        // Downgrading is off by default: the failure costs only the payment and the item is not written.
        if (!CAN_DOWNGRADE)
        {
            _tempValue = selectedEffect.EffectValue;
            _tempUpdatedValue = _tempValue;
            UpdateLog(FAIL_RED);
            return;
        }

        // A failure never takes an effect below the worst legitimate roll, and never raises one already below it.
        if (SaveToLiveItem(value => Mathf.Min(value, Mathf.Max(selectedValues.MinValue, value - selectedValues.Increment)),
                markTempered: false))
        {
            UpdateLog(FAIL_RED);
        }
    }

    /// <summary>
    /// Writes the outcome onto a copy of the item as it is now, changing only the tempered effect. The item may
    /// have changed since the effect was selected (a shard socketed or removed), so the selection-time copy
    /// is never written back.
    /// </summary>
    private bool SaveToLiveItem(System.Func<float, float> outcome, bool markTempered)
    {
        MagicItem live = itemData.GetMagicItem();
        if (live == null || indexOfEffect >= live.Effects.Count ||
            live.Effects[indexOfEffect].EffectType != selectedEffect.EffectType)
        {
            EpicLoot.LogWarning("The tempered item changed its enchantments during the temper; it was left unchanged.");
            return false;
        }

        MagicItem result = JsonConvert.DeserializeObject<MagicItem>(JsonConvert.SerializeObject(live));
        MagicItemEffect effect = result.Effects[indexOfEffect];
        _tempValue = effect.EffectValue;
        effect.EffectValue = outcome(effect.EffectValue);
        _tempUpdatedValue = effect.EffectValue;
        if (markTempered && !result.TemperedEffectIndices.Contains(indexOfEffect))
        {
            result.TemperedEffectIndices.Add(indexOfEffect);
        }

        API.WithChangeReason(API.ChangeReason.Temper, () => itemData.SaveMagicItem(result));
        return true;
    }

    private void UpdateLog(string color)
    {
        string message = MagicItem.GetEffectTextGeneric(selectedDefinition, $"{_tempValue} → {_tempUpdatedValue}");
        TemperPanel.Instance.UpdateLog($"<color={color}>{message}</color>");
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

    // Chances of one temper attempt, not of a failure, so success, downgrade and destroy add up to at
    // most 100% in the info box. The destroy roll comes first on a failure (TemperPanel.OnTemper), so
    // a downgrade only happens when it misses, and an effect already at its floor cannot drop at all.
    public float GetDestroyChance() => CAN_DESTROY_ITEM ? (1f - probability) * DESTROY_CHANCE : 0f;

    public float GetDowngradeChance()
    {
        if (!CAN_DOWNGRADE || selectedValues == null || selectedEffect.EffectValue <= selectedValues.MinValue)
        {
            return 0f;
        }
        return (1f - probability) * (CAN_DESTROY_ITEM ? 1f - DESTROY_CHANCE : 1f);
    }
}
