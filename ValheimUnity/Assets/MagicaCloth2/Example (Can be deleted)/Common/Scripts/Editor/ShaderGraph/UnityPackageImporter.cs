// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace MagicaCloth2UPMImporterCollections
{
    /// <summary>
    /// 必要なUnityPackageの自動インストール
    /// </summary>
    [InitializeOnLoad]
    public static class UnityPackageImporter
    {
        static UnityPackageImporter()
        {
            Install("com.unity.shadergraph");
        }

        public static bool Install(string id)
        {
            Debug.Log($"Install...{id}");
            var request = Client.Add(id);
            while (!request.IsCompleted) { };
            if (request.Error != null) Debug.LogError(request.Error.message);
            return request.Error == null;
        }
    }
}
