using ExtendedItemDataFramework;
using UnityEngine;

namespace EpicLoot {
  public static class LootRoller_Debug {
    public static bool _debugModeEnabled;
    public static string SelectedEffectForNextRolledItem = "";

    public static void AddDebugMagicEffects(ExtendedItemData data, MagicItem item) {
      if (SelectedEffectForNextRolledItem != "") {
        Debug.Log($"AddDebugMagicEffect {SelectedEffectForNextRolledItem}");
        item.Effects.Add(LootRoller.RollEffect(MagicItemEffectDefinitions.Get(SelectedEffectForNextRolledItem), item.Rarity));
        SelectedEffectForNextRolledItem = "";
      }
    }

  }
}