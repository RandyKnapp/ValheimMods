using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot
{
    [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
    public static class Hud_Awake_Patch
    {
        public static void Postfix(Hud __instance)
        {
            var debugTextObject = Object.Instantiate(EpicLoot.Assets.DebugTextPrefab, __instance.m_rootObject.transform, false);
            debugTextObject.AddComponent<DebugText>();
        }
    }

    public class DebugText : MonoBehaviour
    {
        public Text Label;

        private static DebugText _instance;

        public void Awake()
        {
            _instance = this;
            Label = GetComponentInChildren<Text>();
            gameObject.SetActive(false);
        }

        public void OnDestroy()
        {
            _instance = null;
        }

        public static void SetText(string s)
        {
            _instance.Label.text = s;
        }
    }
}