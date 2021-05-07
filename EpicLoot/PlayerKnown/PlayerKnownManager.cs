using EpicLoot.GatedItemType;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EpicLoot.PlayerKnown
{
    public static class PlayerKnownManager
    {
        // server sends to new peers
        private const string Name_RPC_ServerKnown = "EpicLoot_ServerKnown";
        // client sends to everyone
        private const string Name_RPC_ClientKnown = "EpicLoot_ClientKnown";
        // client sends to everyone
        private const string Name_RPC_AddKnownRecipe = "EpicLoot_AddKnownRecipe";
        // client sends to everyone
        private const string Name_RPC_AddKnownMaterial = "EpicLoot_AddKnownMaterial";

        public static Dictionary<long, HashSet<string>> playerKnownRecipes = new Dictionary<long, HashSet<string>>();
        public static Dictionary<long, HashSet<string>> playerKnownMaterial = new Dictionary<long, HashSet<string>>();

        public static void RegisterRPC(ZRoutedRpc routedRpc)
        {
            if (!Common.Utils.IsServer())
            {
                routedRpc.Register<ZPackage>(Name_RPC_ServerKnown, RPC_ServerKnown);
            }
            routedRpc.Register<ZPackage>(Name_RPC_ClientKnown, RPC_ClientKnown);
            routedRpc.Register<string>(Name_RPC_AddKnownRecipe, RPC_AddKnownRecipe);
            routedRpc.Register<string>(Name_RPC_AddKnownMaterial, RPC_AddKnownMaterial);
        }

        public static void OnPeerConnect(ZNetPeer peer)
        {
            if (Common.Utils.IsServer())
            {
                ServerSendKnown(peer);
            }
        }

        public static void OnPeerDisconnect(ZNetPeer peer)
        {
            EpicLoot.Log($"Removing known for peer {peer.m_uid}");
            if (playerKnownRecipes.ContainsKey(peer.m_uid))
            {
                playerKnownRecipes.Remove(peer.m_uid);
            }
            if (playerKnownMaterial.ContainsKey(peer.m_uid))
            {
                playerKnownMaterial.Remove(peer.m_uid);
            }
        }

        public static void OnPlayerSpawn()
        {
            ClientSendKnown();
        }

        public static void OnPlayerAddKnownItem(string itemName)
        {
            ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, Name_RPC_AddKnownMaterial, itemName);
        }

        public static void OnPlayerAddKnownRecipe(string recipeName)
        {
            ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, Name_RPC_AddKnownRecipe, recipeName);
        }

        public static void RPC_ServerKnown(long sender, ZPackage pkg)
        {
            LoadServerKnown(playerKnownMaterial, pkg);
            LoadServerKnown(playerKnownRecipes, pkg);
        }

        public static void RPC_ClientKnown(long sender, ZPackage pkg)
        {
            if (sender == ZNet.instance.GetUID())
            {
                return;
            }
            int materialCount = LoadKnown(sender, pkg, playerKnownMaterial);
            int recipeCount = LoadKnown(sender, pkg, playerKnownRecipes);

            EpicLoot.Log($"Received known from peer {sender}: {materialCount} materials / {recipeCount} recipes");
        }

        public static void RPC_AddKnownRecipe(long sender, string recipe)
        {
            HashSet<string> knownRecipes;
            if (!playerKnownRecipes.TryGetValue(sender, out knownRecipes))
            {
                EpicLoot.LogWarning($"PlayerKnownManager.RPC_AddKnownRecipe: hashset is null for peer {sender}");
                return;
            }
            knownRecipes.Add(recipe);
            EpicLoot.Log($"Received add known recipe from peer {sender}: {recipe}");
        }

        public static void RPC_AddKnownMaterial(long sender, string material)
        {
            HashSet<string> knownMaterial;
            if (!playerKnownMaterial.TryGetValue(sender, out knownMaterial))
            {
                EpicLoot.LogWarning($"PlayerKnownManager.RPC_AddKnownMaterial: hashset is null for peer {sender}");
                return;
            }
            knownMaterial.Add(material);
            EpicLoot.Log($"Received add known material from peer {sender}: {material}");
        }

        public static void ServerSendKnown(ZNetPeer peer)
        {
            ZPackage pkg = new ZPackage();

            WriteServerKnown(playerKnownMaterial, pkg);
            WriteServerKnown(playerKnownRecipes, pkg);

            ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, Name_RPC_ServerKnown, pkg);
        }

        public static void WriteServerKnown(Dictionary<long, HashSet<string>> dict, ZPackage pkg)
        {
            pkg.Write(dict.Count);
            foreach (KeyValuePair<long, HashSet<string>> kvp in dict)
            {
                ZPackage pkg2 = new ZPackage();
                pkg2.Write(kvp.Key);
                WriteKnown(kvp.Value, pkg2);
                pkg.Write(pkg2);
            }
        }

        public static void LoadServerKnown(Dictionary<long, HashSet<string>> dict, ZPackage pkg)
        {
            int numEntries = pkg.ReadInt();
            for (int i = 0; i < numEntries; i++)
            {
                ZPackage pkg2 = pkg.ReadPackage();
                int playerID = pkg2.ReadInt();
                LoadKnown(playerID, pkg2, dict);
            }
        }

        public static void ClientSendKnown()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                EpicLoot.LogWarning("PlayerKnownManager.ClientSendKnown: m_localPlayer == null");
                return;
            }

            ZPackage pkg = new ZPackage();
            WriteKnown(player.m_knownMaterial, pkg);
            WriteKnown(player.m_knownRecipes, pkg);
            long uid = player.GetPlayerID();
            SetKnown(uid, player.m_knownMaterial, playerKnownMaterial);
            SetKnown(uid, player.m_knownRecipes, playerKnownRecipes);


            EpicLoot.Log($"Sending known: {player.m_knownMaterial.Count} materials / {player.m_knownRecipes.Count} recipes {ZRoutedRpc.instance}");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Name_RPC_ClientKnown, pkg);
        }

        public static int LoadKnown(long sender, ZPackage pkg, Dictionary<long, HashSet<string>> outDict)
        {
            ZPackage pkgKnown = pkg.ReadPackage();
            var tempKnown = new HashSet<string>();
            int numKnown = pkgKnown.ReadInt();
            for (int i = 0; i < numKnown; i++)
            {
                tempKnown.Add(pkgKnown.ReadString());
            }
            SetKnown(sender, tempKnown, outDict);
            return tempKnown.Count;
        }

        public static void SetKnown(long sender, HashSet<string> hashset, Dictionary<long, HashSet<string>> outDict)
        {
            if (outDict.ContainsKey(sender))
            {
                outDict.Remove(sender);
            }
            outDict.Add(sender, hashset);
        }

        public static void WriteKnown(HashSet<string> known, ZPackage outPkg)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(known.Count);
            foreach (string s in known)
            {
                pkg.Write(s);
            }
            outPkg.Write(pkg);
        }

        public static bool IsItemKnown(string itemName)
        {
            return playerKnownMaterial.Values.Any(material => material.Contains(itemName));
        }

        public static bool IsRecipeKnown(string recipeName)
        {
            return playerKnownRecipes.Values.Any(recipes => recipes.Contains(recipeName));
        }
    }
}
