using ExtendedItemDataFramework;

namespace EpicLoot {
  public class LootRoller_Debug {
    public static string SelectedEffectForNextRolledItem = "";

    public static void AddDebugMagicEffects(ExtendedItemData data, MagicItem item) {
      if (SelectedEffectForNextRolledItem != "") {
        item.Effects.Add(LootRoller.RollEffect(MagicItemEffectDefinitions.Get(SelectedEffectForNextRolledItem), item.Rarity));
        SelectedEffectForNextRolledItem = "";
      }
    }
  }
}