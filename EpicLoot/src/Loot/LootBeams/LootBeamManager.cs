using System;
using System.Collections.Generic;
using EpicLoot.Config;
using UnityEngine;

namespace EpicLoot.LootBeams
{
    // Only the drops nearest the camera glow, up to Loot Beam Limit. A beam is a prefab of six to nine particle
    // systems, four line renderers, an animator and a looping hum; with one per drop, 1,000 magic drops (a Deep
    // North dungeon floor) took a frame from 11 ms to 37 ms and froze one frame for 0.8 s as they spawned.
    // Beams are pooled per rarity and follow their drop from here, upright, instead of being parented to it.
    public class LootBeamManager : MonoBehaviour
    {
        private const float RefreshInterval = 0.25f;
        private const float HeightOffset = 0.01f;

        private static LootBeamManager _instance;
        private static readonly List<LootBeam> Drops = new List<LootBeam>();
        private static readonly Comparison<LootBeam> ByDistance = (a, b) => a.DistanceSq.CompareTo(b.DistanceSq);

        private readonly Stack<GameObject>[] _pools = new Stack<GameObject>[Rarities.Count];
        private readonly Color?[] _poolColors = new Color?[Rarities.Count];
        private float _nextRefresh;

        internal static void Register(LootBeam drop)
        {
            if (_instance == null)
            {
                var host = new GameObject("EpicLoot LootBeams");
                _instance = host.AddComponent<LootBeamManager>();
            }

            Drops.Add(drop);
            if (drop.OwnEffect != null)
            {
                drop.OwnEffect.SetActive(false);
            }
            _instance._nextRefresh = 0f;
        }

        internal static void Unregister(LootBeam drop)
        {
            Drops.Remove(drop);
            if (_instance != null)
            {
                _instance.Hide(drop);
            }
            else
            {
                drop.Beam = null;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            if (Time.time < _nextRefresh)
            {
                return;
            }
            _nextRefresh = Time.time + RefreshInterval;

            Camera camera = Utils.GetMainCamera();
            if (camera == null)
            {
                return;
            }

            Vector3 eye = camera.transform.position;
            for (int i = 0; i < Drops.Count; i++)
            {
                Drops[i].DistanceSq = (Drops[i].transform.position - eye).sqrMagnitude;
            }
            Drops.Sort(ByDistance);

            int limit = ELConfig.LootBeamLimit?.Value ?? 100;
            // Hidden first, so a beam the nearer drops need is back in its pool before they ask for one.
            for (int i = limit; i < Drops.Count; i++)
            {
                Hide(Drops[i]);
            }
            for (int i = 0; i < limit && i < Drops.Count; i++)
            {
                Show(Drops[i]);
            }
        }

        private void LateUpdate()
        {
            for (int i = 0; i < Drops.Count; i++)
            {
                LootBeam drop = Drops[i];
                if (drop.Beam != null)
                {
                    drop.Beam.transform.SetPositionAndRotation(drop.transform.position + Vector3.up * HeightOffset, Quaternion.identity);
                }
                if (drop.OwnEffect != null && drop.OwnEffect.activeSelf)
                {
                    drop.OwnEffect.transform.rotation = Quaternion.identity;
                }
            }
        }

        private void Show(LootBeam drop)
        {
            if (drop.OwnEffect != null && !drop.OwnEffect.activeSelf)
            {
                drop.OwnEffect.SetActive(true);
            }

            if (drop.Beam != null || drop.BeamRarity == null)
            {
                return;
            }

            drop.BeamColor = EpicLoot.GetRarityColorARGB(drop.BeamRarity.Value);
            drop.Beam = Take(drop.BeamRarity.Value, drop.BeamColor);
            drop.Beam.transform.SetPositionAndRotation(drop.transform.position + Vector3.up * HeightOffset, Quaternion.identity);
            drop.Beam.SetActive(true);
            if (drop.Beam.TryGetComponent(out Animator animator))
            {
                animator.Play("Show", 0, 0f);
            }
        }

        private void Hide(LootBeam drop)
        {
            if (drop.OwnEffect != null && drop.OwnEffect.activeSelf)
            {
                drop.OwnEffect.SetActive(false);
            }

            if (drop.Beam == null)
            {
                return;
            }

            GameObject beam = drop.Beam;
            drop.Beam = null;
            beam.SetActive(false);
            int rarity = (int)drop.BeamRarity.GetValueOrDefault();
            Stack<GameObject> pool = _pools[rarity] ??= new Stack<GameObject>();
            // Beams of an old rarity colour are not reused, and a pool never holds more than the limit can show,
            // so a limit raised and lowered again does not leave hundreds of hidden beams behind.
            if (_poolColors[rarity] != drop.BeamColor || pool.Count >= (ELConfig.LootBeamLimit?.Value ?? 100))
            {
                Destroy(beam);
                return;
            }
            pool.Push(beam);
        }

        private GameObject Take(ItemRarity rarity, Color color)
        {
            int index = (int)rarity;
            Stack<GameObject> pool = _pools[index] ??= new Stack<GameObject>();

            // Beams pooled before a rarity colour change keep the old colour, so they are rebuilt.
            if (_poolColors[index] != color)
            {
                while (pool.Count > 0)
                {
                    Destroy(pool.Pop());
                }
                _poolColors[index] = color;
            }

            if (pool.Count > 0)
            {
                return pool.Pop();
            }

            GameObject beam = Instantiate(EpicAssets.MagicItemLootBeamPrefabs[index], transform);
            beam.SetActive(false);
            BeamColor.Apply(beam, color);
            return beam;
        }
    }
}
