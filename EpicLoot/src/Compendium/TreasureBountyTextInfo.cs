using EpicLoot.Adventure;
using EpicLoot.Biomes;
using System.Collections.Generic;
using System.Linq;

namespace EpicLoot.Compendium;

public class TreasureBountyTextInfo(string topic) : MagicTextInfo(topic)
{
    public override void Build(MagicPages instance)
    {
        if (!Player.m_localPlayer)
        {
            return;
        }

        List<string> content = new();
        AdventureSaveData saveData = Player.m_localPlayer.GetAdventureSaveData();

        List<TreasureMapChestInfo> sortedTreasureMaps = saveData.TreasureMaps
            .Where(x => x.State == TreasureMapState.Purchased)
            .OrderBy(x => BiomeDataManager.GetOrder(x.Biome))
            .ToList();
        // A bounty with no target is a broken save entry; it has no name or classification to show.
        List<BountyInfo> sortedBounties = saveData.Bounties
            .Where(x => x.Target != null && (x.State == BountyState.InProgress || x.State == BountyState.Complete))
            .OrderBy(x => x.State)
            .ToList();

        if (sortedTreasureMaps.Count > 0)
        {
            foreach (TreasureMapChestInfo treasureMap in sortedTreasureMaps)
            {
                content.Add($" - $mod_epicloot_merchant_treasuremaps: " +
                            $"<color={BiomeDataManager.GetColor(treasureMap.Biome)}>{BiomeDataManager.GetLocalizationToken(treasureMap.Biome)} " +
                            $"#{treasureMap.Interval + 1}</color>");
            }

            instance.MagicPagesTextArea.Add($"<color=#FFA626>" + 
                                            $"<size={MagicPages.LARGE_FONT_SIZE}>" + 
                                            "$mod_epicloot_merchant_treasuremaps" + 
                                            "</size></color>", content.ToArray());
            content.Clear();
        }

        if (sortedBounties.Count > 0)
        {
            foreach (BountyInfo bounty in sortedBounties)
            {
                string targetName = AdventureDataManager.GetBountyName(bounty);
                content.Add($" - <size={MagicPages.LARGE_FONT_SIZE}>{targetName}</size>  " +
                    $"<color=#c0c0c0ff>$mod_epicloot_activebounties_classification:</color> " +
                    $"<color=#d66660>{AdventureDataManager.GetMonsterName(bounty.Target.MonsterID)}</color>, ");

                string info = $" $mod_epicloot_activebounties_biome: <color={BiomeDataManager.GetColor(bounty.Biome)}>{BiomeDataManager.GetLocalizationToken(bounty.Biome)}</color>";

                string status = "";
                switch (bounty.State)
                {
                    case BountyState.InProgress:
                        status = "<color=#00f0ff>$mod_epicloot_bounties_tooltip_inprogress</color>";
                        break;
                    case BountyState.Complete:
                        status = "<color=#70f56c>$mod_epicloot_bounties_tooltip_vanquished</color>";
                        break;
                }

                info += $"  <color=#c0c0c0ff>$mod_epicloot_bounties_tooltip_status {status}</color>";


                int iron = bounty.RewardIron;
                int gold = bounty.RewardGold;
                info += $", $mod_epicloot_bounties_tooltip_rewards " +
                        $"{(iron > 0 ? $"<color=white>{MerchantPanel.GetIronBountyTokenName()} x{iron}</color>" : "")}" +
                        $"{(iron > 0 && gold > 0 ? ", " : "")}" +
                        $"{(gold > 0 ? $"<color=#f5da53>{MerchantPanel.GetGoldBountyTokenName()} x{gold}</color>" : "")}";
                
                content.Add(info);
                
            }

            instance.MagicPagesTextArea.Add($"<color=#FFA626><size={MagicPages.LARGE_FONT_SIZE}>" 
                                            + $"$mod_epicloot_activebounties</size></color>", content.ToArray());
        }

        if (sortedTreasureMaps.Count == 0 && sortedBounties.Count == 0)
        {
            instance.MagicPagesTextArea.Add("$mod_epicloot_no_active_adventures");
        }
    }
}