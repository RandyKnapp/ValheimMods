using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_Addon_Helheim
{
    [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
    public static class Hud_Awake_Patch
    {
        public static void Postfix(Hud __instance)
        {
            var helheimTextObject = Object.Instantiate(Helheim.Assets.HelheimTextPrefab, __instance.m_rootObject.transform, false);
            helheimTextObject.AddComponent<HelheimText>();
        }
    }

    public class HelheimText : MonoBehaviour
    {
        public Text Label;

        public void Awake()
        {
            Label = GetComponentInChildren<Text>();
        }

        public void Update()
        {
            Label.text = Helheim.HelheimLevel == 0 ? "Valheim" : $"Helheim: Level {Helheim.HelheimLevel} - {GetHelheimName()}";
        }

        // Niflhel - the edge of Helheim
        // Baldyrborg - the funeral pyre of Baldyr
        // Myrkstaðr - the place of darkness
        // Frostsalr - the house of frost
        // Skaðihaugar - the mounds of death
        private static string GetHelheimName()
        {
            switch (Helheim.HelheimLevel)
            {
                case 1: return "Niflhel";
                case 2: return "Baldyrborg";
                case 3: return "Myrkstaðr";
                case 4: return "Frostsalr";
                case 5: return "Skaðihaugar";
                default: return "";
            }
        }
    }
}
