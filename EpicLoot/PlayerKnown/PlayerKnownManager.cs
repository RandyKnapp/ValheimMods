using EpicLoot.GatedItemType;
using System.Collections.Generic;
using System.Linq;

namespace EpicLoot.PlayerKnown
{
    public static class PlayerKnownManager
    {
        private static GatedItemTypeMode mode;
        
        // server sends to new peers
        private const string Name_RPC_ServerKnownMats = "EpicLoot_ServerKnown_Mats";
        // client sends to everyone
        private const string Name_RPC_ClientKnownMats = "EpicLoot_ClientKnown_Mats";
        // server sends to new peers
        private const string Name_RPC_ServerKnownRecipes = "EpicLoot_ServerKnown_Recipes";
        // client sends to everyone
        private const string Name_RPC_ClientKnownRecipes = "EpicLoot_ClientKnown_Recipes";
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
                routedRpc.Register<ZPackage>(Name_RPC_ServerKnownMats, RPC_ServerKnownMats);
                routedRpc.Register<ZPackage>(Name_RPC_ServerKnownRecipes, RPC_ServerKnownRecipes);
            }
            routedRpc.Register<ZPackage>(Name_RPC_ClientKnownMats, RPC_ClientKnownMats);
            routedRpc.Register<ZPackage>(Name_RPC_ClientKnownRecipes, RPC_ClientKnownRecipes);
            routedRpc.Register<string>(Name_RPC_AddKnownRecipe, RPC_AddKnownRecipe);
            routedRpc.Register<string>(Name_RPC_AddKnownMaterial, RPC_AddKnownMaterial);
        }

        public static void OnPeerConnect(ZNetPeer peer)
        {
           
            if (Common.Utils.IsServer())
            {
                ServerSendKnownMats(peer);
                ServerSendKnownRecipes(peer);
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
            ClientSendKnownRecipes();
            ClientSendKnownMats();
        }

        public static void OnPlayerAddKnownItem(string itemName)
        {
            ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, Name_RPC_AddKnownMaterial, itemName);
        }

        public static void OnPlayerAddKnownRecipe(string recipeName)
        {
            ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, Name_RPC_AddKnownRecipe, recipeName);
        }

        public static void RPC_ServerKnownMats(long sender, ZPackage pkg)
        {
            LoadServerKnownMats(playerKnownMaterial, pkg);
        }
        public static void RPC_ServerKnownRecipes(long sender, ZPackage pkg)
        {
            LoadServerKnownRecipes(playerKnownRecipes, pkg);
        }

        public static void RPC_ClientKnownMats(long sender, ZPackage pkg)
        {
            int materialCount = LoadKnownMats(sender, pkg, playerKnownMaterial);

            EpicLoot.Log($"Received known from peer {sender}: {materialCount} materials");
        }
        public static void RPC_ClientKnownRecipes(long sender, ZPackage pkg)
        {
            int recipeCount = LoadKnownRecipes(sender, pkg, playerKnownRecipes);

            EpicLoot.Log($"Received known from peer {sender}: {recipeCount} recipes");
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

        public static void ServerSendKnownMats(ZNetPeer peer)
        {
            ZPackage pkg = new ZPackage();

            WriteServerKnownMats(playerKnownMaterial, pkg);

            ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, Name_RPC_ServerKnownMats, pkg);
        }
        public static void ServerSendKnownRecipes(ZNetPeer peer)
        {
            ZPackage pkg = new ZPackage();

            WriteServerKnownRecipes(playerKnownRecipes, pkg);

            ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, Name_RPC_ServerKnownRecipes, pkg);
        }

        public static void WriteServerKnownRecipes(Dictionary<long, HashSet<string>> dict, ZPackage pkg)
        {
            pkg.Write(dict.Count);
            foreach (KeyValuePair<long, HashSet<string>> kvp in dict)
            {
                ZPackage pkg2 = new ZPackage();
                pkg2.Write(kvp.Key);
                WriteKnownRecipes(kvp.Value, pkg2);
                pkg.Write(pkg2);
            }
        }
        public static void WriteServerKnownMats(Dictionary<long, HashSet<string>> dict, ZPackage pkg)
        {
            pkg.Write(dict.Count);
            foreach (KeyValuePair<long, HashSet<string>> kvp in dict)
            {
                ZPackage pkg2 = new ZPackage();
                pkg2.Write(kvp.Key);
                WriteKnownMats(kvp.Value, pkg2);
                pkg.Write(pkg2);
            }
        }

        public static void LoadServerKnownMats(Dictionary<long, HashSet<string>> dict, ZPackage pkg)
        {
            int numEntries = pkg.ReadInt();
            for (int i = 0; i < numEntries; i++)
            {
                ZPackage pkg2 = pkg.ReadPackage();
                int playerID = pkg2.ReadInt();
                LoadKnownMats(playerID, pkg2, dict);
            }
        }
        public static void LoadServerKnownRecipes(Dictionary<long, HashSet<string>> dict, ZPackage pkg)
        {
            int numEntries = pkg.ReadInt();
            for (int i = 0; i < numEntries; i++)
            {
                ZPackage pkg2 = pkg.ReadPackage();
                int playerID = pkg2.ReadInt();
                LoadKnownRecipes(playerID, pkg2, dict);
            }
        }

        public static void ClientSendKnownMats()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                EpicLoot.LogWarning("PlayerKnownManager.ClientSendKnown: m_localPlayer == null");
                return;
            }

            ZPackage pkg = new ZPackage();
            WriteKnownMats(player.m_knownMaterial, pkg);

            EpicLoot.Log($"Sending known: {player.m_knownMaterial.Count} materials");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Name_RPC_ClientKnownMats, pkg);
        } 
        public static void ClientSendKnownRecipes()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                EpicLoot.LogWarning("PlayerKnownManager.ClientSendKnown: m_localPlayer == null");
                return;
            }

            ZPackage pkg = new ZPackage();
            WriteKnownRecipes(player.m_knownRecipes, pkg);

            EpicLoot.Log($"Sending known: {player.m_knownRecipes.Count} recipes");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Name_RPC_ClientKnownRecipes, pkg);
        }

        public static int LoadKnownRecipes(long sender, ZPackage pkg, Dictionary<long, HashSet<string>> outDict)
        {
            ZPackage pkgKnown = pkg.ReadPackage();
            var tempKnown = new HashSet<string>();
            int numKnown = pkgKnown.ReadInt();
            for (int i = 0; i < numKnown; i++)
            {
                tempKnown.Add(pkgKnown.ReadString());
            }
            SetKnownRecipes(sender, tempKnown, outDict);
            return tempKnown.Count;
        }
        public static int LoadKnownMats(long sender, ZPackage pkg, Dictionary<long, HashSet<string>> outDict)
        {
            ZPackage pkgKnown = pkg.ReadPackage();
            var tempKnown = new HashSet<string>();
            int numKnown = pkgKnown.ReadInt();
            for (int i = 0; i < numKnown; i++)
            {
                tempKnown.Add(pkgKnown.ReadString());
            }
            SetKnownMats(sender, tempKnown, outDict);
            return tempKnown.Count;
        }

        public static void SetKnownRecipes(long sender, HashSet<string> hashset, Dictionary<long, HashSet<string>> outDict)
        {
            if (outDict.ContainsKey(sender))
            {
                outDict.Remove(sender);
            }
            outDict.Add(sender, hashset);
        }
        public static void SetKnownMats(long sender, HashSet<string> hashset, Dictionary<long, HashSet<string>> outDict)
        {
            if (outDict.ContainsKey(sender))
            {
                outDict.Remove(sender);
            }
            outDict.Add(sender, hashset);
        }

        public static void WriteKnownRecipes(HashSet<string> known, ZPackage outPkg)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(known.Count);
            foreach (string s in known)
            {
                pkg.Write(s);
            }
            outPkg.Write(pkg);
        }

        public static void WriteKnownMats(HashSet<string> known, ZPackage outPkg)
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
