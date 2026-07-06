using System.Collections.Generic;
using System.Linq;

namespace EpicLoot.Compendium;

public class MagicEffectTextInfo(string topic) : MagicTextInfo(topic)
{
    public override void Build(MagicPages instance)
    {
        if (!Player.m_localPlayer)
        {
            return;
        }

        Dictionary<string, List<KeyValuePair<MagicItemEffect, ItemDrop.ItemData>>> magicEffects =
            new Dictionary<string, List<KeyValuePair<MagicItemEffect, ItemDrop.ItemData>>>();

        List<ItemDrop.ItemData> allMagicEquipment = Player.m_localPlayer.GetMagicEquipment();
        foreach (ItemDrop.ItemData item in allMagicEquipment)
        {
            foreach (MagicItemEffect effect in item.GetMagicItem().Effects)
            {
                if (!magicEffects.TryGetValue(effect.EffectType,
                        out List<KeyValuePair<MagicItemEffect, ItemDrop.ItemData>> effectList))
                {
                    effectList = new List<KeyValuePair<MagicItemEffect, ItemDrop.ItemData>>();
                    magicEffects.Add(effect.EffectType, effectList);
                }

                effectList.Add(new KeyValuePair<MagicItemEffect, ItemDrop.ItemData>(effect, item));
            }
        }

        foreach (KeyValuePair<string, List<KeyValuePair<MagicItemEffect, ItemDrop.ItemData>>> entry in magicEffects)
        {
            string effectType = entry.Key;
            MagicItemEffectDefinition effectDef = MagicItemEffectDefinitions.Get(effectType);
            float sum = entry.Value.Sum(x => x.Key.EffectValue);
            string totalEffectText = MagicItem.GetEffectText(effectDef, sum);
            ItemRarity highestRarity = (ItemRarity)entry.Value.Max(x => (int)x.Value.GetRarity());

            List<string> content = new();
            foreach (KeyValuePair<MagicItemEffect, ItemDrop.ItemData> entry2 in entry.Value)
            {
                MagicItemEffect effect = entry2.Key;
                ItemDrop.ItemData item = entry2.Value;
                content.Add($" <color=#c0c0c0ff>- {MagicItem.GetEffectText(effect, item.GetRarity(), false)} " +
                    $"({item.GetDecoratedName()})</color>");
            }

            instance.MagicPagesTextArea.Add($"<size={MagicPages.MEDIUM_FONT_SIZE}>" +
                $"<color={EpicLoot.GetRarityColor(highestRarity)}>{totalEffectText}</color></size>",
                content.ToArray());
        }
    }
}
