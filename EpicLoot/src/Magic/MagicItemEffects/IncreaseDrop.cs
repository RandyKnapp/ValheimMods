using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

public abstract class IncreaseDrop
{
    public const int PercentEffectVersion = 2;

    private const float BonusEffectCooldown = 0.25f;

    private static float _nextEffectTime;

    private readonly Dictionary<string, int> _pendingTotals = new Dictionary<string, int>();
    private Vector3 _pendingPosition;
    private bool _batching;

    public string MagicEffect { get; set; }
    public string ZDOVar { get; set; }
    public float LegacyValueScale { get; set; }
    public string FillerItem { get; set; }
    public HashSet<string> BetterItems { get; set; }

    public static int VersionFor(string effectType)
    {
        return effectType == MagicEffectType.IncreaseMiningDrop || effectType == MagicEffectType.IncreaseTreeDrop
            ? PercentEffectVersion
            : 1;
    }

    public static bool ConvertLegacyValue(MagicItemEffect effect)
    {
        if (effect == null || effect.Version >= PercentEffectVersion)
        {
            return false;
        }

        IncreaseDrop drop = null;
        if (effect.EffectType == MagicEffectType.IncreaseMiningDrop)
        {
            drop = IncreaseMiningDrop.Instance;
        }
        else if (effect.EffectType == MagicEffectType.IncreaseTreeDrop)
        {
            drop = IncreaseTreeDrop.Instance;
        }

        if (drop == null)
        {
            return false;
        }

        effect.EffectValue *= drop.LegacyValueScale;
        effect.Version = PercentEffectVersion;
        return true;
    }

    public void Publish(Player player, ZDO zdo)
    {
        float bonus = player.GetTotalActiveMagicEffectValue(MagicEffect);
        if (bonus != zdo.GetFloat(ZDOVar))
        {
            zdo.Set(ZDOVar, bonus);
        }
    }

    public float BonusOf(Character character)
    {
        return character is Player player && player.m_nview != null && player.m_nview.GetZDO() is ZDO zdo
            ? zdo.GetFloat(ZDOVar)
            : 0f;
    }

    public static bool IsCheated(Character character)
    {
        return character is Player player && player.GetInventory().CheatedDamagingItemEquipped();
    }

    public static Vector3 GroundedDropPosition(DropOnDestroyed dropper)
    {
        Vector3 position = dropper.transform.position;
        float groundHeight = ZoneSystem.instance.GetGroundHeight(position);
        if (position.y < groundHeight)
        {
            position.y = groundHeight + 0.1f;
        }

        return position + Vector3.up * dropper.m_spawnYOffset;
    }

    public void BeginBatch(Vector3 feedbackPosition)
    {
        _batching = true;
        _pendingPosition = feedbackPosition;
    }

    public void EndBatch()
    {
        _batching = false;
        ShowPendingBonus();
    }

    public void Grant(Character attacker, DropTable dropTable, Vector3 position, HitData conversionHit = null)
    {
        Grant(BonusOf(attacker), IsCheated(attacker), dropTable, position, conversionHit);
    }

    public void Grant(float percent, bool cheated, DropTable dropTable, Vector3 position, HitData conversionHit = null)
    {
        if (percent <= 0f || dropTable == null || dropTable.IsEmpty())
        {
            return;
        }

        float rolls = percent / 100f;
        int rollCount = Mathf.FloorToInt(rolls);
        if (Random.value < rolls - rollCount)
        {
            rollCount++;
        }

        if (rollCount == 0)
        {
            return;
        }

        bool skipFiller = SkipsFiller(dropTable);
        var totals = new Dictionary<GameObject, int>();
        for (int i = 0; i < rollCount; i++)
        {
            foreach (GameObject drop in dropTable.GetDropList())
            {
                ItemDrop itemDrop = drop.GetComponent<ItemDrop>();
                if (itemDrop == null || (skipFiller && drop.name == FillerItem))
                {
                    continue;
                }

                int amount = 1;
                GameObject prefab = conversionHit != null
                    ? Game.instance.CheckDropConversion(conversionHit, itemDrop, drop, ref amount)
                    : drop;
                totals.TryGetValue(prefab, out int total);
                totals[prefab] = total + amount;
            }
        }

        foreach (KeyValuePair<GameObject, int> total in totals)
        {
            Spawn(total.Key, total.Value, position, cheated);
            AddToPendingBonus(total.Key, total.Value, position);
        }

        if (!_batching)
        {
            ShowPendingBonus();
        }
    }

    private bool SkipsFiller(DropTable dropTable)
    {
        bool hasFiller = false;
        bool hasBetter = false;
        foreach (DropTable.DropData drop in dropTable.m_drops)
        {
            if (drop.m_item == null)
            {
                continue;
            }

            string name = drop.m_item.name;
            if (name == FillerItem)
            {
                hasFiller = true;
            }
            else if (BetterItems == null || BetterItems.Contains(name))
            {
                hasBetter = true;
            }
        }

        return hasFiller && hasBetter;
    }

    private static void Spawn(GameObject prefab, int amount, Vector3 position, bool cheated)
    {
        int maxStack = Mathf.Max(1, prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxStackSize);
        while (amount > 0)
        {
            int stack = Mathf.Min(amount, maxStack);
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0, 360), 0f);
            GameObject spawned = Object.Instantiate(prefab, position + Random.insideUnitSphere * 0.3f, rotation);
            ItemDrop itemDrop = spawned.GetComponent<ItemDrop>();
            ItemDrop.OnCreateNew(itemDrop, cheated);
            itemDrop.SetStack(stack);
            amount -= stack;
        }
    }

    private void AddToPendingBonus(GameObject prefab, int amount, Vector3 position)
    {
        if (!_batching)
        {
            _pendingPosition = position;
        }

        string name = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
        _pendingTotals.TryGetValue(name, out int total);
        _pendingTotals[name] = total + amount;
    }

    private void ShowPendingBonus()
    {
        if (_pendingTotals.Count == 0)
        {
            return;
        }

        if (DamageText.instance != null)
        {
            foreach (KeyValuePair<string, int> total in _pendingTotals)
            {
                DamageText.instance.ShowText(DamageText.TextType.Bonus, _pendingPosition, $"+{total.Value} {total.Key}");
            }
        }

        PlayBonusEffect(_pendingPosition);
        _pendingTotals.Clear();
    }

    private static void PlayBonusEffect(Vector3 position)
    {
        if (Time.time < _nextEffectTime || InventoryGui.instance == null)
        {
            return;
        }

        _nextEffectTime = Time.time + BonusEffectCooldown;
        InventoryGui.instance.m_craftBonusEffect.Create(position, Quaternion.identity);
    }
}
