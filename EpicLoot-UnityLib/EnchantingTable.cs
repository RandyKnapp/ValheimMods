using UnityEngine;

namespace EpicLoot_UnityLib
{
    public class EnchantingTable : MonoBehaviour, Hoverable, Interactable
    {
        public const float UseDistance = 2.7f;
        public const string DisplayNameLocID = "mod_epicloot_assets_enchantingtable";

        public GameObject EnchantingUIPrefab;

        public bool Interact(Humanoid user, bool repeat, bool alt)
        {
            if (repeat || user != Player.m_localPlayer || !InUseDistance(user))
                return false;

            EnchantingTableUI.Show(EnchantingUIPrefab);

            return false;
        }

        public void Awake()
        {
            
        }

        public void Update()
        {
            if (Player.m_localPlayer != null && EnchantingTableUI.instance != null && EnchantingTableUI.instance.isActiveAndEnabled && !InUseDistance(Player.m_localPlayer))
                EnchantingTableUI.Hide();
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public bool InUseDistance(Humanoid human)
        {
            return Vector3.Distance(human.transform.position, transform.position) < UseDistance;
        }

        public string GetHoverText()
        {
            return !InUseDistance(Player.m_localPlayer)
                ? Localization.instance.Localize("<color=grey>$piece_toofar</color>")
                : Localization.instance.Localize($"${DisplayNameLocID}\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
        }

        public string GetHoverName()
        {
            return DisplayNameLocID;
        }
    }
}
