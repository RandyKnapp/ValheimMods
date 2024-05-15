using HarmonyLib;
using TMPro;
using UnityEngine;

namespace EpicLoot.Adventure
{
    [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.Awake))]
    public class MessageHud_Awake_Patch
    {
        public static void Postfix(MessageHud __instance)
        {
            var biomeMessageTextRect = __instance.m_biomeFoundPrefab.transform.Find("UnlockMessage/Title").GetComponent<TMP_Text>().rectTransform;
            biomeMessageTextRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 800);
        }
    }
}
