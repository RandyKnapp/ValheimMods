using System;

namespace EpicLoot.MagicItemEffects
{
	public static class ModifyWithLowHealth
	{
		public static void Apply(Player player, string name, Action<string> action)
		{
			action(name);
			if (player != null && player.GetHealth() / player.GetMaxHealth() < 0.3f)
			{
				action(name + "LowHealth");
			}
		}
	}
}