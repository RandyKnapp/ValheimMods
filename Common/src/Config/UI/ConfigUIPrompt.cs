using Jotunn.Managers;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Common {

    // A modal "type a name" prompt, with room for a warning the caller wants read before the user commits.
    //
    // Sits on the same full-screen catcher pattern as the picker so clicks cannot fall through onto the
    // panel underneath, and takes its own refcounted input block: a prompt is exactly where someone types
    // a word that would otherwise walk their character across the map.
    internal static class ConfigUIPrompt {
        private const float PanelW = 460f;

        private static GameObject overlay;

        internal static void Show(string title, string label, string initial, string warning, Action<string> onAccept) {
            Close();
            if (GUIManager.Instance == null || GUIManager.CustomGUIFront == null) { return; }

            bool hasWarning = string.IsNullOrEmpty(warning) == false;
            float warningH = hasWarning ? 96f : 0f;
            float panelH = 190f + warningH;

            overlay = ConfigUI.NewUI("ConfigUIPromptOverlay", GUIManager.CustomGUIFront.transform, typeof(Image));
            RectTransform overlayRT = (RectTransform)overlay.transform;
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.offsetMin = Vector2.zero;
            overlayRT.offsetMax = Vector2.zero;
            overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
            // No click-out-to-dismiss here: a prompt is a decision, and dismissing one by clicking beside
            // it is how a half-typed rename gets thrown away.
            overlay.AddComponent<ConfigUI.ConfigUIInputGuard>().Hold();

            GameObject panel = GUIManager.Instance.CreateWoodpanel(
                parent: overlay.transform,
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 0f), width: PanelW, height: panelH, draggable: true);

            ConfigUI.AddText(panel.transform, 0f, 14f, PanelW, 30f, title, 18, TextAnchor.MiddleCenter,
                GUIManager.Instance.ValheimYellow);

            float y = 56f;
            if (hasWarning) {
                ConfigUI.AddText(panel.transform, 20f, y, PanelW - 40f, warningH - 8f, warning, 13,
                    TextAnchor.UpperLeft, new Color(0.98f, 0.75f, 0.14f));
                y += warningH;
            }

            ConfigUI.AddText(panel.transform, 20f, y, 120f, 28f, label, 15, TextAnchor.MiddleLeft);
            InputField field = ConfigUI.AddTextField(panel.transform, 140f, y, PanelW - 160f, initial, null,
                InputField.ContentType.Standard, null, 64);

            y += 46f;
            ConfigUI.AddButton(panel.transform, 20f, y, 190f, ConfigUI.L("$menu_cancel", "Cancel"), Close, 34f);
            ConfigUI.AddButton(panel.transform, PanelW - 210f, y, 190f, ConfigUI.L("$menu_ok", "OK"), () => {
                string value = field.text;
                Close();
                onAccept?.Invoke(value);
            }, 34f);

            field.Select();
            field.ActivateInputField();
        }

        internal static void Close() {
            if (overlay == null) { return; }
            UnityEngine.Object.Destroy(overlay);
            overlay = null;
        }
    }
}
