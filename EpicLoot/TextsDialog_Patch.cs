using System.Collections.Generic;
using System.Linq;
using System.Text;
using EpicLoot.Adventure;
using HarmonyLib;

namespace EpicLoot
{
    [HarmonyPatch(typeof(TextsDialog), "UpdateTextsList")]
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
            AddTreasureAndBountiesPage(__instance, player);
        }

        private static void AddMagicEffectsPage(TextsDialog textsDialog, Player player)
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
                var totalEffectText = string.Format(effectDef.DisplayText, sum);
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

            textsDialog.m_texts.Insert(2, new TextsDialog.TextInfo("Magic Effects", t.ToString()));
        }

        private static void AddTreasureAndBountiesPage(TextsDialog textsDialog, Player player)
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
                t.AppendLine($" Biome: <color={GetBiomeColor(bounty.Biome)}>$biome_{bounty.Biome.ToString().ToLower()}</color>");

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

                t.Append($"  Status: {status}");

                var iron = bounty.RewardIron;
                var gold = bounty.RewardGold;
                t.AppendLine($", Reward: {(iron > 0 ? $"<color=white>{MerchantPanel.GetIronBountyTokenName()} x{iron}</color>" : "")}{(iron > 0 && gold > 0 ? ", " : "")}{(gold > 0 ? $"<color=#f5da53>{MerchantPanel.GetGoldBountyTokenName()} x{gold}</color>" : "")}</color>");
                t.AppendLine();
            }

            textsDialog.m_texts.Insert(3, new TextsDialog.TextInfo("Treasure & Bounties", t.ToString()));
        }

        private static string GetBiomeColor(Heightmap.Biome biome)
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

        private static float GetBiomeOrder(Heightmap.Biome biome)
        {
            if (biome == Heightmap.Biome.BlackForest)
            {
                return 1.5f;
            }

            return (float) biome;
        }
    }
}
