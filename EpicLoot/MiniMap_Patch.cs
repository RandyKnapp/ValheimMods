using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot
{
    [HarmonyPatch(typeof(Minimap), "Update")]
    public static class MiniMap_Patch
    {
        public static Text PositionDisplay;

        public static void Postfix(Minimap __instance)
        {
            if (PositionDisplay == null)
            {
                var go = new GameObject("PositionDisplay", typeof(RectTransform));
                go.transform.SetParent(__instance.transform, false);
                go.transform.SetAsLastSibling();

                PositionDisplay = go.AddComponent<Text>();
                
            }

            if (Player.m_localPlayer == null)
            {
                PositionDisplay.gameObject.SetActive(false);
            }
            else
            {
                PositionDisplay.gameObject.SetActive(true);
                PositionDisplay.text = Player.m_localPlayer.transform.position.ToString("F0");
            }
        }
    }
}
