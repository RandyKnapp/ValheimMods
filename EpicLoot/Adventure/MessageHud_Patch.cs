using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Adventure
{
    [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.Awake))]
    public class MessageHud_Awake_Patch
    {
        public static void Postfix(MessageHud __instance)
        {
            var biomeMessageTextRect = __instance.m_biomeFoundPrefab.transform.Find("UnlockMessage/Title").GetComponent<Text>().rectTransform;
            biomeMessageTextRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 800);
        }
    }
}
