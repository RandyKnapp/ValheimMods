using System;
using UnityEngine;

namespace Common
{
    public class GotDestroyed : MonoBehaviour
    {
        public void OnDisable()
        {
            Debug.LogError($"I got destroyed! ({gameObject.name})");
            Debug.Log(Environment.StackTrace);
        }
    }
}
