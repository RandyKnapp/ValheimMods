using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EpicLoot.Healing
{
    // consolidate healing into single value to not spam with UI labels
    public class HealingQueueMono : MonoBehaviour
    {
        public List<float> HealRequests = new List<float>();

        private Player _player;

        private void Start()
        {
            _player = GetComponent<Player>();
            InvokeRepeating(nameof(UpdateHealingQueue), 1f, 0.4f);
        }

        public void UpdateHealingQueue()
        {
            if (_player != null && !_player.IsDead() && HealRequests.Count > 0)
            {
                _player.Heal(HealRequests.Sum());
                HealRequests.Clear();
            }
        }
    }
}