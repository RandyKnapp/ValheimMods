using UnityEngine;

namespace EpicLoot.LootBeams
{
    public class AlwaysPointUp : MonoBehaviour
    {
        public void Update()
        {
            transform.rotation = Quaternion.identity;
        }
    }
}
