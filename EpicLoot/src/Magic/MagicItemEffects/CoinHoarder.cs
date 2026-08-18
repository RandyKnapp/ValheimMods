using EpicLoot.General;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

public class CoinHoarder
{
    // Method used to evaluate coins in players inventory.
    public static float GetCoinHoarderValue(Player player, float effectValue)
    {
        if (player == null)
        {
            return 0f;
        }

        float totalCoins = CoinPurse.GetTotalCoins(player);
        if (totalCoins <= 0)
        {
            return 0f;
        }

        if (totalCoins <= 1000)
        {
            // Linear fraction increase up till 1000 coins, then logarithmic decay increase (1.145x at 1000)
            return (totalCoins * 0.000145f);
        }

        // Slope intercept at effectValue 3 * 1000 coins = 0.145065498747
        // This will result in a bump at higher effects and higher coin counts when going just over 1000 coins
        // But the logarithmic curve quickly diminishes these returns, 20,000 coins and 10 coinhoarder results in 0.22115
        float coinHoarderBonus = (Mathf.Log10(effectValue * totalCoins) * 6.258f / 150f);
        return coinHoarderBonus;
    }
}