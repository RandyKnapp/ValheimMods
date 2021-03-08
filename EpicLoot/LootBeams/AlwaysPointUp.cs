using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
