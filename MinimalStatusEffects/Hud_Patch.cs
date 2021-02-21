using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MinimalStatusEffects
{
    [HarmonyPatch(typeof(Hud), "UpdateStatusEffects", new Type[] {typeof(List<StatusEffect>) })]
    public static class Hud_UpdateStatusEffects_Patch
    {
        static void Postfix(List<StatusEffect> statusEffects, List<RectTransform> ___m_statusEffects, RectTransform ___m_statusEffectListRoot)
        {
            Vector2 screenPosition = MinimalStatusEffectConfig.ScreenPosition.Value;
            float width = MinimalStatusEffectConfig.ListSize.Value.x;
            float height = MinimalStatusEffectConfig.ListSize.Value.y;
            float iconSize = MinimalStatusEffectConfig.IconSize.Value;

            ___m_statusEffectListRoot.anchorMin = new Vector2(1, 1);
            ___m_statusEffectListRoot.anchorMax = new Vector2(1, 1);
            ___m_statusEffectListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            ___m_statusEffectListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            ___m_statusEffectListRoot.anchoredPosition = screenPosition;

            float yPos = 0;
            for (var index = 0; index < ___m_statusEffects.Count; index++)
            {
                StatusEffect statusEffect = statusEffects[index];
                RectTransform statusEffectObject = ___m_statusEffects[index];
                statusEffectObject.localPosition = new Vector3(0, yPos, 0);
                yPos -= MinimalStatusEffectConfig.EntrySpacing.Value;

                var name = statusEffectObject.Find("Name") as RectTransform;
                if (name != null)
                {
                    var nameText = name.GetComponent<Text>();
                    nameText.alignment = TextAnchor.MiddleLeft;
                    nameText.supportRichText = true;
                    name.anchorMin = new Vector2(0, 0.5f);
                    name.anchorMax = new Vector2(1, 0.5f);
                    name.anchoredPosition = new Vector2(120 + iconSize, 0);
                    name.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, iconSize);
                    name.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

                    string iconText = statusEffect.GetIconText();
                    var timeText = statusEffectObject.Find("TimeText");
                    if (!string.IsNullOrEmpty(iconText))
                    {
                        if (timeText != null)
                        {
                            timeText.gameObject.SetActive(false);
                        }

                        var displayName = Localization.instance.Localize(statusEffect.m_name);
                        nameText.text = $"{displayName} <color=#ffb75c>{iconText}</color>";
                    }
                }

                var icon = statusEffectObject.Find("Icon") as RectTransform;
                icon.anchorMin = new Vector2(0.5f, 0.5f);
                icon.anchorMax = new Vector2(0.5f, 0.5f);
                icon.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, iconSize);
                icon.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, iconSize);
                icon.anchoredPosition = new Vector2(iconSize, 0);
            }
        }
    }
}
