// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth2
{
    public class GameObjectContainer : MonoBehaviour
    {
        [SerializeField]
        private List<GameObject> gameObjectList = new List<GameObject>();


        private Dictionary<string, GameObject> gameObjectDict = new Dictionary<string, GameObject>();

        protected void Awake()
        {
            // create dictionary.
            foreach (var obj in gameObjectList)
            {
                if (obj)
                {
                    gameObjectDict.Add(obj.name, obj);
                }
            }
        }

        public bool Contains(string objName)
        {
            return gameObjectDict.ContainsKey(objName);
        }

        public GameObject GetGameObject(string objName)
        {
            if (gameObjectDict.ContainsKey(objName))
            {
                return gameObjectDict[objName];
            }
            else
                return null;
        }
    }
}
