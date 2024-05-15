using UnityEngine;

namespace EpicLoot.LootBeams
{
    [RequireComponent(typeof(ItemDrop))]
    public class LootBeam : MonoBehaviour
    {
        public static float HeightOffset = 0.01f;

        private ItemDrop _itemDrop;
        private GameObject _beam;
        private MagicItem _magicItem;
        private bool _isMagic;

        private void Awake()
        {
            _itemDrop = GetComponent<ItemDrop>();
        }

        private void Start()
        {
            _magicItem = _itemDrop.m_itemData.GetMagicItem();
            _isMagic = _itemDrop.m_itemData.IsMagic();
        }

        private void Update()
        {
            if (ShouldShowBeam())
            {

                if (_magicItem == null)
                    return;

                _beam = Instantiate(EpicLoot.Assets.MagicItemLootBeamPrefabs[(int) _magicItem.Rarity], transform);
                _beam.transform.localPosition = Vector3.up * HeightOffset;
                var beamColorSetter = _beam.AddComponent<BeamColorSetter>();
                beamColorSetter.SetColor(_magicItem.GetColor());
                _beam.GetComponent<Animator>().Play("Show");
                _beam.AddComponent<AlwaysPointUp>();

                var audio = _beam.GetComponentsInChildren<AudioSource>();
                foreach (var audioSource in audio)
                {
                    audioSource.outputAudioMixerGroup = AudioMan.instance.m_ambientMixer;
                }
            }

            if (ShouldHideBeam())
            {
                Destroy(_beam);
                _beam = null;
            }
        }

        public bool ShouldShowBeam()
        {
            return _beam == null && _itemDrop != null && _itemDrop.m_itemData != null && _isMagic;
        }

        public bool ShouldHideBeam()
        {
            return _beam != null && (_itemDrop == null || _itemDrop.m_itemData == null || !_isMagic);
        }
    }
}
