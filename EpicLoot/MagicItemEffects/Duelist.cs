using System;

namespace EpicLoot.MagicItemEffects
{
	public static class Duelist
	{
		public static void Apply(Player player, string name, Action<string> action)
		{
			ModifyWithLowHealth.Apply(player, name, action);
			if (!player.HasEquipmentOfType(ItemDrop.ItemData.ItemType.Shield) && player.HasMagicEquipmentWithEffect(MagicEffectType.Duelist))
			{
				action(MagicEffectType.Duelist);
			}
		}
	}
}