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

        if (Player.m_localPlayer == null)
        {
            return;
        }

        if (Item.m_equipped && Player.m_localPlayer.IsItemEquiped(Item))
        {
            Multiplayer_Player_Patch.UpdatePlayerZDOForEquipment(Player.m_localPlayer, Item, MagicItem != null);
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
        SetMagicItem(MagicItem);
    }

    public override void Load()
    {
        if (!string.IsNullOrEmpty(Value))
        {
            Deserialize();
        }

        FixupValuelessEffects();

        //Check Indestructible on Item
        Indestructible.MakeItemIndestructible(Item);

        SetMagicItem(MagicItem);
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

