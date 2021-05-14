using System.Collections.Generic;
using System.Linq;
using System.Text;
using EpicLoot.Adventure;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EpicLoot
{
    [HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.UpdateTextsList))]
    public static class TextsDialog_UpdateTextsList_Patch
    {
        public static void Postfix(TextsDialog __instance)
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            AddMagicEffectsPage(__instance, player);
            AddMagicEffectsExplainPage(__instance);
            AddTreasureAndBountiesPage(__instance, player);
        }

        public static void AddMagicEffectsPage(TextsDialog textsDialog, Player player)
        {
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
                var totalEffectText = MagicItem.GetEffectText(effectDef, sum);
                var highestRarity = (ItemRarity) entry.Value.Max(x => (int) x.Value.GetRarity());

                t.AppendLine($"<size=20><color={EpicLoot.GetRarityColor(highestRarity)}>{totalEffectText}</color></size>");
                foreach (var entry2 in entry.Value)
                {
                    var effect = entry2.Key;
                    var item = entry2.Value;
                    t.AppendLine($" <color=silver>- {MagicItem.GetEffectText(effect, item.GetRarity(), false)} ({item.GetDecoratedName()})</color>");
                }

                t.AppendLine();
            }

            textsDialog.m_texts.Insert(2, new TextsDialog.TextInfo("Active Magic Effects", Localization.instance.Localize(t.ToString())));
        }
        
        public static void AddTreasureAndBountiesPage(TextsDialog textsDialog, Player player)
        {
            var t = new StringBuilder();

            var saveData = player.GetAdventureSaveData();

            t.AppendLine("<color=orange><size=30>Treasure Maps</size></color>");
            t.AppendLine();

            var sortedTreasureMaps = saveData.TreasureMaps.Where(x => x.State == TreasureMapState.Purchased).OrderBy(x => GetBiomeOrder(x.Biome));
            foreach (var treasureMap in sortedTreasureMaps)
            {
                t.AppendLine(Localization.instance.Localize($"Treasure Map: <color={GetBiomeColor(treasureMap.Biome)}>$biome_{treasureMap.Biome.ToString().ToLower()} #{treasureMap.Interval + 1}</color>"));
            }

            t.AppendLine();
            t.AppendLine();
            t.AppendLine("<color=orange><size=30>Active Bounties</size></color>");
            t.AppendLine();

            var sortedBounties = saveData.Bounties.OrderBy(x => x.State);
            foreach (var bounty in sortedBounties)
            {
                if (bounty.State == BountyState.Claimed)
                {
                    continue;
                }

                var targetName = AdventureDataManager.GetBountyName(bounty);
                t.AppendLine($"<size=24>{targetName}</size>");
                t.Append($"  <color=silver>Classification: <color=#d66660>{AdventureDataManager.GetMonsterName(bounty.Target.MonsterID)}</color>, ");
                t.AppendLine($" Biome: <color={GetBiomeColor(bounty.Biome)}>$biome_{bounty.Biome.ToString().ToLower()}</color></color>");

                var status = "";
                switch (bounty.State)
                {
                    case BountyState.InProgress:
                        status = ("<color=#00f0ff>In Progress</color>");
                        break;
                    case BountyState.Complete:
                        status = ("<color=#70f56c>Vanquished!</color>");
                        break;
                }

                t.Append($"  <color=silver>Status: {status}");

                var iron = bounty.RewardIron;
                var gold = bounty.RewardGold;
                t.AppendLine($", Reward: {(iron > 0 ? $"<color=white>{MerchantPanel.GetIronBountyTokenName()} x{iron}</color>" : "")}{(iron > 0 && gold > 0 ? ", " : "")}{(gold > 0 ? $"<color=#f5da53>{MerchantPanel.GetGoldBountyTokenName()} x{gold}</color>" : "")}</color>");
                t.AppendLine();
            }

            textsDialog.m_texts.Insert(4, new TextsDialog.TextInfo("Treasure & Bounties", Localization.instance.Localize(t.ToString())));
        }
        
        public static string GetBiomeColor(Heightmap.Biome biome)
        {
            var biomeColor = "white";
            switch (biome)
            {
                case Heightmap.Biome.Meadows: biomeColor = "#75d966"; break;
                case Heightmap.Biome.BlackForest: biomeColor = "#72a178"; break;
                case Heightmap.Biome.Swamp: biomeColor = "#a88a6f"; break;
                case Heightmap.Biome.Mountain: biomeColor = "#a3bcd6"; break;
                case Heightmap.Biome.Plains: biomeColor = "#d6cea3"; break;
            }

            return biomeColor;
        }
        
        public static float GetBiomeOrder(Heightmap.Biome biome)
        {
            if (biome == Heightmap.Biome.BlackForest)
            {
                return 1.5f;
            }

            return (float) biome;
        }

        public static void AddMagicEffectsExplainPage(TextsDialog textsDialog)
        {
            var sortedMagicEffects = MagicItemEffectDefinitions.AllDefinitions
                .Where(x => !x.Value.Requirements.NoRoll)
                .Select(x => new KeyValuePair<string, string>(string.Format(Localization.instance.Localize(x.Value.DisplayText), "<b><color=yellow>X</color></b>"), Localization.instance.Localize(x.Value.Description)))
                .OrderBy(x => x.Key);

            var t = new StringBuilder();
            foreach (var effectEntry in sortedMagicEffects)
            {
                t.AppendLine($"<size=24>{effectEntry.Key}</size>");
                t.AppendLine($"<color=silver>{effectEntry.Value}</color>");
                t.AppendLine();
            }

            textsDialog.m_texts.Insert(3, new TextsDialog.TextInfo(
                Localization.instance.Localize("$mod_epicloot_me_explaintitle"),
                Localization.instance.Localize(t.ToString())));
        }
    }

    [HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.ShowText), typeof(TextsDialog.TextInfo))]
    public static class TextsDialog_ShowText_Patch
    {
        public static Transform TextContainer;
        public static Text TitleTextPrefab;
        public static Text DescriptionTextPrefab;

        public static bool Prefix(TextsDialog __instance, TextsDialog.TextInfo text)
        {
            if (TitleTextPrefab == null)
            {
                TextContainer = __instance.m_textAreaTopic.transform.parent;
                var textContainerBackground = TextContainer.gameObject.AddComponent<Image>();
                textContainerBackground.color = new Color();
                textContainerBackground.raycastTarget = true;

                var verticalLayoutGroup = TextContainer.GetComponent<VerticalLayoutGroup>();
                verticalLayoutGroup.spacing = 0;

                TitleTextPrefab = Object.Instantiate(__instance.m_textAreaTopic, __instance.transform);
                TitleTextPrefab.gameObject.SetActive(false);
            }

            if (DescriptionTextPrefab == null)
            {
                DescriptionTextPrefab = Object.Instantiate(__instance.m_textArea, __instance.transform);
                DescriptionTextPrefab.gameObject.SetActive(false);
            }

            for (var i = 0; i < TextContainer.childCount; i++)
            {
                Object.Destroy(TextContainer.GetChild(i).gameObject);
            }

            var description = Object.Instantiate(TitleTextPrefab, TextContainer);
            description.gameObject.SetActive(true);
            description.text = text.m_topic;

            var parts = text.m_text.Split('\n');
            foreach (var part in parts)
            {
                var paragraphText = Object.Instantiate(DescriptionTextPrefab, TextContainer);
                paragraphText.gameObject.SetActive(true);
                paragraphText.text = part;
            }

            return false;
        }
    }
}
