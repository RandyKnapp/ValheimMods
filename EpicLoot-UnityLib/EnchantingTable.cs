using UnityEngine;

namespace EpicLoot_UnityLib
{
    public class EnchantingTable : MonoBehaviour, Hoverable, Interactable
    {
        public const float UseDistance = 2.7f;
        public const string DisplayNameLocID = "mod_epicloot_assets_enchantingtable";

        public GameObject EnchantingUIPrefab;

        private Player _interactingPlayer;

        public bool Interact(Humanoid user, bool repeat, bool alt)
        {
            if (repeat || user != Player.m_localPlayer || !InUseDistance(user))
                return false;

            EnchantingTableUI.Show(EnchantingUIPrefab);
            _interactingPlayer = Player.m_localPlayer;
            return false;
        }

        public void Awake()
        {
            var wearTear = GetComponent<WearNTear>();
            if (wearTear != null)
            {
                wearTear.m_destroyedEffect.m_effectPrefabs = new EffectList.EffectData[]
                {
                    new EffectList.EffectData() { m_prefab = ZNetScene.instance.GetPrefab("vfx_SawDust") },
                    new EffectList.EffectData() { m_prefab = ZNetScene.instance.GetPrefab("sfx_wood_destroyed") }
                };
                wearTear.m_hitEffect.m_effectPrefabs = new EffectList.EffectData[1]
                {
                    new EffectList.EffectData() { m_prefab = ZNetScene.instance.GetPrefab("vfx_SawDust") }
                };
            }
        }

        public void Update()
        {
            if (_interactingPlayer != null && EnchantingTableUI.instance != null && EnchantingTableUI.instance.isActiveAndEnabled && !InUseDistance(_interactingPlayer))
            {
                EnchantingTableUI.Hide();
                _interactingPlayer = null;
            }
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
