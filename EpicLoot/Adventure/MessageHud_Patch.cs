using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Adventure
{
    [HarmonyPatch(typeof(MessageHud), "Awake")]
    public class MessageHud_Patch
    {
        public static void Postfix(MessageHud __instance)
        {
            var biomeMessageTextRect = __instance.m_biomeFoundPrefab.transform.Find("UnlockMessage/Title").GetComponent<Text>().rectTransform;
            biomeMessageTextRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 800);
        }
    }
}
