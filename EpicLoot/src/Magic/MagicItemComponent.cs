using EpicLoot.Data;
using EpicLoot.MagicItemEffects;
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace EpicLoot;

public class MagicItemComponent : CustomItemData
{
    public const string TypeID = "rkel";
    public MagicItem MagicItem;

    protected override bool AllowStackingIdenticalValues { get; set; } = true;

    public void SetMagicItem(MagicItem magicItem)
    {
        if (magicItem == null)
        {
            return;
        }

        MagicItem = magicItem;
        Value = Serialize();
        Save();

        // Every magic-data write funnels through here (enchant, augment, socket, rune, temper,
        // transfer, loot roll), so this is the one place Indestructible needs to be re-derived and the
        // one place the API's change event can be raised with guaranteed coverage. Callers that know
        // *why* they are writing wrap themselves in API.WithChangeReason, which only labels the event
        // raised here -- it still fires exactly once per write.
        Indestructible.Sync(Item);
        API.RaiseMagicItemChanged(Item);

        if (Player.m_localPlayer == null)
        {
            return;
        }

        if (Item.m_equipped && Player.m_localPlayer.IsItemEquiped(Item))
        {
            Multiplayer_Player_Patch.UpdatePlayerZDOForEquipment(Player.m_localPlayer, Item, MagicItem != null);
            // The worn item's effects just changed; drop the memoized per-player totals. This is
            // the single funnel for magic-data writes, so paths that never re-equip (tempering,
            // rune etching) get correct totals immediately instead of on the next equip change.
            EquipmentEffectCache.Reset(Player.m_localPlayer);
        }
    }

    public string Serialize()
    {
        return JsonConvert.SerializeObject(MagicItem, Formatting.None);
    }

    public void Deserialize()
    {
        try
        {
            if (string.IsNullOrEmpty(Value))
            {
                return;
            }

            MagicItem = JsonConvert.DeserializeObject<MagicItem>(Value);
        }
        catch (Exception)
        {
            EpicLoot.LogError($"[{nameof(MagicItemComponent)}] Could not deserialize MagicItem json data! ({Item?.m_shared?.m_name})"); 
            throw;
        }
    }

    public CustomItemData Clone()
    {
        return MemberwiseClone() as CustomItemData;
    }

    public override void FirstLoad()
    {
        if (Item.m_shared.m_name == "$item_helmet_dverger")
        {
            MagicItem magicItem = new MagicItem();
            magicItem.Rarity = ItemRarity.Rare;
            magicItem.Effects.Add(new MagicItemEffect(MagicEffectType.DvergerCirclet));
            magicItem.TypeNameOverride = "$mod_epicloot_circlet";

            MagicItem = magicItem;
        }
        else if (Item.m_shared.m_name == "$item_beltstrength")
        {
            MagicItem magicItem = new MagicItem();
            magicItem.Rarity = ItemRarity.Rare;
            magicItem.Effects.Add(new MagicItemEffect(MagicEffectType.Megingjord));
            magicItem.TypeNameOverride = "$mod_epicloot_belt";

            MagicItem = magicItem;
        }
        else if (Item.m_shared.m_name == "$item_wishbone")
        {
            MagicItem magicItem = new MagicItem();
            magicItem.Rarity = ItemRarity.Epic;
            magicItem.Effects.Add(new MagicItemEffect(MagicEffectType.Wishbone));
            magicItem.TypeNameOverride = "$mod_epicloot_remains";

            MagicItem = magicItem;
        }

        FixupValuelessEffects();
        SetMagicItemQuietly();
    }

    public override void Load()
    {
        if (!string.IsNullOrEmpty(Value))
        {
            Deserialize();
        }

        FixupValuelessEffects();

        SetMagicItemQuietly();

        // SetMagicItem bails out on a null MagicItem, so sync here too -- a component with no magic
        // item still needs the flag reverted if this instance was previously made indestructible.
        Indestructible.Sync(Item);
    }

    /// <summary>
    /// Normalizing writes done while an item is loading are not changes -- every item entering the world
    /// passes through here, so raising the API change event would flood listeners at world load.
    /// </summary>
    private void SetMagicItemQuietly()
    {
        bool previous = API.SuppressChangeEvents;
        API.SuppressChangeEvents = true;
        try
        {
            SetMagicItem(MagicItem);
        }
        finally
        {
            API.SuppressChangeEvents = previous;
        }
    }

    // ItemInfo.Remove<T> calls this after clearing m_customData but before dropping the component,
    // so HasMagicEffect would still report the effect -- Sync would refuse to revert. Force it.
    public override void Unload()
    {
        Indestructible.Revert(Item);
    }

    private void FixupValuelessEffects()
    {
        if (MagicItem == null)
        {
            return;
        }

        foreach (MagicItemEffect effect in MagicItem.Effects)
        {
            if (MagicItemEffectDefinitions.IsValuelessEffect(effect.EffectType, MagicItem.Rarity) &&
                !Mathf.Approximately(effect.EffectValue, 1))
            {
                EpicLoot.Log($"Fixing up effect on {MagicItem.DisplayName}: effect={effect.EffectType}");
                effect.EffectValue = 1;
            }
        }
    }
}

