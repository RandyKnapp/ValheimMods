using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MinimalStatusEffects
{
    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateStatusEffects), new Type[] { typeof(List<StatusEffect>) })]
    public static class Hud_UpdateStatusEffects_Patch
    {
        public static Sprite _sprite;

        static void Postfix(List<StatusEffect> statusEffects, List<RectTransform> ___m_statusEffects, RectTransform ___m_statusEffectListRoot)
        {
            if (Game.m_noMap && !MinimalStatusEffectConfig.EnabledInNomap.Value) return;

            Vector2 screenPosition = Game.m_noMap ? MinimalStatusEffectConfig.NomapScreenPosition.Value : MinimalStatusEffectConfig.ScreenPosition.Value;
            float width = Game.m_noMap ? MinimalStatusEffectConfig.NomapListSize.Value.x : MinimalStatusEffectConfig.ListSize.Value.x;
            float height = Game.m_noMap ? MinimalStatusEffectConfig.NomapListSize.Value.y : MinimalStatusEffectConfig.ListSize.Value.y;
            float iconSize = Game.m_noMap ? MinimalStatusEffectConfig.NomapIconSize.Value : MinimalStatusEffectConfig.IconSize.Value;

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
                yPos -= Game.m_noMap ? MinimalStatusEffectConfig.NomapEntrySpacing.Value : MinimalStatusEffectConfig.EntrySpacing.Value;

                var name = statusEffectObject.Find("Name") as RectTransform;
                if (name != null)
                {
                    var nameText = name.GetComponent<Text>();
                    nameText.alignment = TextAnchor.MiddleLeft;
                    nameText.supportRichText = true;
                    nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
                    nameText.resizeTextForBestFit = false;
                    nameText.fontSize = Game.m_noMap ? MinimalStatusEffectConfig.NomapFontSize.Value : MinimalStatusEffectConfig.FontSize.Value;
                    name.anchorMin = new Vector2(0, 0.5f);
                    name.anchorMax = new Vector2(1, 0.5f);
                    name.anchoredPosition = new Vector2(120 + iconSize, 2);
                    name.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, iconSize + 20);
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

                var cooldown = statusEffectObject.Find("Cooldown") as RectTransform;
                cooldown.anchorMin = new Vector2(0.5f, 0.5f);
                cooldown.anchorMax = new Vector2(0.5f, 0.5f);
                cooldown.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 16);
                cooldown.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 16);
                cooldown.anchoredPosition = new Vector2(20, -10);
                cooldown.Rotate(Vector3.forward * (-200 * Time.deltaTime));

                if (_sprite == null)
                {
                    _sprite = Common.Utils.LoadSpriteFromFile("MinimalStatusEffects", "refresh_icon.png");
                }

                if (_sprite != null)
                {
                    var cooldownImage = cooldown.GetComponent<Image>();
                    cooldownImage.overrideSprite = _sprite;
                }
            }
        }
    }

    //public void UpdateShipHud(Player player, float dt)
    [HarmonyPatch(typeof(Hud), "UpdateShipHud")]
    public static class Hud_UpdateShipHud_Patch
    {
        public static void Postfix(Hud __instance)
        {
            if (Game.m_noMap && !MinimalStatusEffectConfig.EnabledInNomap.Value) return;

            var scale = Game.m_noMap ? MinimalStatusEffectConfig.NomapSailingPowerIndicatorScale.Value : MinimalStatusEffectConfig.SailingPowerIndicatorScale.Value;
            var powerIcon = __instance.m_rudder.transform.parent as RectTransform;
            powerIcon.localScale = new Vector3(scale, scale, 1);
            powerIcon.anchoredPosition = Game.m_noMap ? MinimalStatusEffectConfig.NomapSailingPowerIndicatorPosition.Value : MinimalStatusEffectConfig.SailingPowerIndicatorPosition.Value;
            __instance.m_shipWindIndicatorRoot.anchoredPosition = Game.m_noMap ? MinimalStatusEffectConfig.NomapSailingWindIndicatorPosition.Value : MinimalStatusEffectConfig.SailingWindIndicatorPosition.Value;
        }
    }

}