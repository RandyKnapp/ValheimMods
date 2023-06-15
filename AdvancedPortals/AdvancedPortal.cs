using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedPortals
{
    public class AdvancedPortal : MonoBehaviour
    {
        public List<string> AllowedItems = new List<string>();
        public bool AllowEverything;
        private ZNetView _nview;
        public string DefaultName;
        private void Awake()
        {
            _nview = GetComponent<ZNetView>();

            if (_nview == null || _nview.GetZDO() == null)
                return;

            ZDOMan.instance.AddPortal(_nview.GetZDO());
        }

        private void Start()
        {
            ZDOMan.instance.ConvertPortals();
            ZDOMan.instance.ConnectPortals();
        }
    }
}
