using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EpicLoot.HealingQueue
{
    /// consolidate healing into single value to not spam with UI labels
    public class HealingQueueMono : MonoBehaviour
    {
        public Player player;
        public List<float> healRequests = new List<float>();

        private void Start()
        {
            InvokeRepeating(nameof(UpdateHealingQueue), 1f, 0.4f);
        }

        public void UpdateHealingQueue()
        {
            if (!player.IsDead())
            {
                var healingQueue = player.GetComponent<HealingQueueMono>();
                if (healingQueue && healingQueue.healRequests.Count > 0)
                {
                    player.Heal(healingQueue.healRequests.Sum());
                    healingQueue.healRequests.Clear();
                }
            }
        }
    }
}