using UnityEngine;

namespace EpicLoot
{
    [RequireComponent(typeof(ItemDrop))]
    public class LootBeam : MonoBehaviour
    {
        private ItemDrop _itemDrop;
        private GameObject _beam;

        private void Awake()
        {
            _itemDrop = GetComponent<ItemDrop>();
        }

        private void Update()
        {
            if (ShouldShowBeam())
            {
                var magicItem = _itemDrop.m_itemData.GetMagicItem();
                _beam = Instantiate(EpicLoot.Assets.MagicItemLootBeamPrefabs[(int) magicItem.Rarity], transform);
            }

            if (ShouldHideBeam())
            {
                Destroy(_beam);
                _beam = null;
            }

            if (_beam != null)
            {
                _beam.transform.rotation = Quaternion.identity;

                float groundHeight = ZoneSystem.instance.GetGroundHeight(_beam.transform.position);
                _beam.transform.position = new Vector3(_beam.transform.position.x, groundHeight, _beam.transform.position.z);
            }
        }

        public bool ShouldShowBeam()
        {
            return _beam == null && _itemDrop != null && _itemDrop.m_itemData != null && _itemDrop.m_itemData.IsMagic();
        }

        public bool ShouldHideBeam()
        {
            return _beam != null && (_itemDrop == null || _itemDrop.m_itemData == null || !_itemDrop.m_itemData.IsMagic());
        }
    }
}
