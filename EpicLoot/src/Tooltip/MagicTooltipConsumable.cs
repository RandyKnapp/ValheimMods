namespace EpicLoot;

public partial class MagicTooltip
{
    private void AddFoodHealth()
    {
        if (item.m_shared.m_food > 0f)
        {
            text.AppendFormat("\n$item_food_health: <color=#ff8080ff>{0}</color>  ($item_current:<color=yellow>{1}</color>)",
                item.m_shared.m_food, Player.m_localPlayer.GetMaxHealth());
        }
    }

    private void AddFoodStamina()
    {
        if (item.m_shared.m_foodStamina > 0f)
        {
            text.AppendFormat("\n$item_food_stamina: <color=#ffff80ff>{0}</color>  ($item_current:<color=yellow>{1}</color>)",
                item.m_shared.m_foodStamina, Player.m_localPlayer.GetMaxStamina());
        }
    }

    private void AddFoodEitr()
    {
        if (item.m_shared.m_foodEitr > 0f)
        {
            text.AppendFormat("\n$item_food_eitr: <color=#9090ffff>{0}</color>  ($item_current:<color=yellow>{1}</color>)",
                item.m_shared.m_foodEitr, Player.m_localPlayer.GetMaxEitr());
        }
    }

    private void AddFoodBurn()
    {
        text.AppendFormat("\n$item_food_duration: <color=orange>{0}s</color>", item.m_shared.m_foodBurnTime);
    }

    private void AddFoodRegen()
    {
        if (item.m_shared.m_foodRegen > 0f)
        {
            text.AppendFormat("\n$item_food_regen: <color=orange>{0} hp/tick</color>", item.m_shared.m_foodRegen);
        }
    }
}