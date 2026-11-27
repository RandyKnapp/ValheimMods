using UnityEngine;

namespace EpicLoot.LootBeams
{
    // Marks a drop that glows: a magic item's beam, a magic crafting material's own particles, or both (an
    // unidentified item). It only registers the drop; LootBeamManager decides which drops show their glow.
    public class LootBeam : MonoBehaviour
    {
        // The rarity of the pooled beam this drop shows, or null for none.
        internal ItemRarity? BeamRarity;
        // A glow the item prefab carries itself, switched on and off rather than pooled.
        internal GameObject OwnEffect;

        internal GameObject Beam;
        internal Color BeamColor;
        internal float DistanceSq;

        private void OnEnable()
        {
            LootBeamManager.Register(this);
        }

        private void OnDisable()
        {
            LootBeamManager.Unregister(this);
        }
    }
}
