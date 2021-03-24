using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;

namespace EpicLoot
{
    [HarmonyPatch(typeof(TextsDialog), "UpdateTextsList")]
    public static class TextsDialog_UpdateTextsList_Patche
    {
        public static void Postfix(TextsDialog __instance)
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            var magicEffects = new Dictionary<string, List<KeyValuePair<MagicItemEffect, ItemDrop.ItemData>>>();

            var allEquipment = player.GetEquipment();
            foreach (var item in allEquipment)
            {
                if (item.IsMagic())
                {
                    foreach (var effect in item.GetMagicItem().Effects)
                    {
                        if (!magicEffects.TryGetValue(effect.EffectType, out var effectList))
                        {
                            effectList = new List<KeyValuePair<MagicItemEffect, ItemDrop.ItemData>>();
                            magicEffects.Add(effect.EffectType, effectList);
                        }
                        effectList.Add(new KeyValuePair<MagicItemEffect, ItemDrop.ItemData>(effect, item));
                    }
                }
            }

            var t = new StringBuilder();

            foreach (var entry in magicEffects)
            {
                var effectType = entry.Key;
                var effectDef = MagicItemEffectDefinitions.Get(effectType);
                var sum = entry.Value.Sum(x => x.Key.EffectValue);
                var totalEffectText = string.Format(effectDef.DisplayText, sum);
                var highestRarity = (ItemRarity)entry.Value.Max(x => (int) x.Value.GetRarity());

                t.AppendLine($"<size=20><color={EpicLoot.GetRarityColor(highestRarity)}>{totalEffectText}</color></size>");
                foreach (var entry2 in entry.Value)
                {
                    var effect = entry2.Key;
                    var item = entry2.Value;
                    t.AppendLine($" <color=silver>- {MagicItem.GetEffectText(effect, item.GetRarity(), false)} ({item.GetDecoratedName()})</color>");
                }

                t.AppendLine();
            }

            __instance.m_texts.Insert(2, new TextsDialog.TextInfo("Magic Effects", t.ToString()));
        }
    }
}
