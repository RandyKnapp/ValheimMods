using System.Collections.Generic;
using System.Linq;
using EpicLoot.GatedItemType;

namespace EpicLoot.PlayerKnown
{
    public static class PlayerKnownManager
    {
        // server sends to new peers, client sends to everyone
        private const string Name_RPC_ServerKnownMats = "EpicLoot_ServerKnown_Mats";
        private const string Name_RPC_ClientKnownMats = "EpicLoot_ClientKnown_Mats";
        private const string Name_RPC_ServerKnownRecipes = "EpicLoot_ServerKnown_Recipes";
        private const string Name_RPC_ClientKnownRecipes = "EpicLoot_ClientKnown_Recipes";
        private const string Name_RPC_AddKnownRecipe = "EpicLoot_AddKnownRecipe";
        private const string Name_RPC_AddKnownMaterial = "EpicLoot_AddKnownMaterial";

        public static Dictionary<long, HashSet<string>> PlayerKnownRecipes = new Dictionary<long, HashSet<string>>();
        public static Dictionary<long, HashSet<string>> PlayerKnownMaterial = new Dictionary<long, HashSet<string>>();

        private static bool IsEnabled => EpicLoot.GetGatedItemTypeMode() != GatedItemTypeMode.Unlimited;

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
            if (!IsEnabled)
                return;

            if (Common.Utils.IsServer())
            {
                ServerSendKnownMats(peer);
                ServerSendKnownRecipes(peer);
            }
        }

        public static void OnPeerDisconnect(ZNetPeer peer)
        {
            if (!IsEnabled)
                return;

            EpicLoot.Log($"Removing known for peer {peer.m_uid}");
            if (PlayerKnownRecipes.ContainsKey(peer.m_uid))
            {
                PlayerKnownRecipes.Remove(peer.m_uid);
            }

            if (PlayerKnownMaterial.ContainsKey(peer.m_uid))
            {
                PlayerKnownMaterial.Remove(peer.m_uid);
            }
        }

        public static void OnPlayerSpawn()
        {
            if (!IsEnabled)
                return;

            ClientSendKnownRecipes();
            ClientSendKnownMats();
        }

        public static void OnPlayerAddKnownItem(string itemName)
        {
            if (!IsEnabled)
                return;

            ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, Name_RPC_AddKnownMaterial, itemName);
        }

        public static void OnPlayerAddKnownRecipe(string recipeName)
        {
            if (!IsEnabled)
                return;

            ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, Name_RPC_AddKnownRecipe, recipeName);
        }

        public static void RPC_ServerKnownMats(long sender, ZPackage pkg)
        {
            if (!IsEnabled)
                return;

            LoadServerKnownMats(PlayerKnownMaterial, pkg);
        }

        public static void RPC_ServerKnownRecipes(long sender, ZPackage pkg)
        {
            if (!IsEnabled)
                return;

            LoadServerKnownRecipes(PlayerKnownRecipes, pkg);
        }

        public static void RPC_ClientKnownMats(long sender, ZPackage pkg)
        {
            if (!IsEnabled)
                return;

            var materialCount = LoadKnownMats(sender, pkg, PlayerKnownMaterial);

            EpicLoot.Log($"Received known from peer {sender}: {materialCount} materials");
        }

        public static void RPC_ClientKnownRecipes(long sender, ZPackage pkg)
        {
            if (!IsEnabled)
                return;

            var recipeCount = LoadKnownRecipes(sender, pkg, PlayerKnownRecipes);

            EpicLoot.Log($"Received known from peer {sender}: {recipeCount} recipes");
        }

        public static void RPC_AddKnownRecipe(long sender, string recipe)
        {
            if (!IsEnabled)
                return;

            if (!PlayerKnownRecipes.TryGetValue(sender, out var knownRecipes))
            {
                EpicLoot.LogWarning($"PlayerKnownManager.RPC_AddKnownRecipe: hashset is null for peer {sender}");
                return;
            }
            knownRecipes.Add(recipe);
            EpicLoot.Log($"Received add known recipe from peer {sender}: {recipe}");
        }

        public static void RPC_AddKnownMaterial(long sender, string material)
        {
            if (!IsEnabled)
                return;

            if (!PlayerKnownMaterial.TryGetValue(sender, out var knownMaterial))
            {
                EpicLoot.LogWarning($"PlayerKnownManager.RPC_AddKnownMaterial: hashset is null for peer {sender}");
                return;
            }
            knownMaterial.Add(material);
            EpicLoot.Log($"Received add known material from peer {sender}: {material}");
        }

        public static void ServerSendKnownMats(ZNetPeer peer)
        {
            if (!IsEnabled)
                return;

            var pkg = new ZPackage();

            WriteServerKnownMats(PlayerKnownMaterial, pkg);

            ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, Name_RPC_ServerKnownMats, pkg);
        }

        public static void ServerSendKnownRecipes(ZNetPeer peer)
        {
            if (!IsEnabled)
                return;

            var pkg = new ZPackage();

            WriteServerKnownRecipes(PlayerKnownRecipes, pkg);

            ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, Name_RPC_ServerKnownRecipes, pkg);
        }

        public static void WriteServerKnownRecipes(Dictionary<long, HashSet<string>> dict, ZPackage pkg)
        {
            if (!IsEnabled)
                return;

            pkg.Write(dict.Count);
            foreach (var kvp in dict)
            {
                var pkg2 = new ZPackage();
                pkg2.Write(kvp.Key);
                WriteKnownRecipes(kvp.Value, pkg2);
                pkg.Write(pkg2);
            }
        }

        public static void WriteServerKnownMats(Dictionary<long, HashSet<string>> dict, ZPackage pkg)
        {
            if (!IsEnabled)
                return;

            pkg.Write(dict.Count);
            foreach (var kvp in dict)
            {
                var pkg2 = new ZPackage();
                pkg2.Write(kvp.Key);
                WriteKnownMats(kvp.Value, pkg2);
                pkg.Write(pkg2);
            }
        }

        public static void LoadServerKnownMats(Dictionary<long, HashSet<string>> dict, ZPackage pkg)
        {
            if (!IsEnabled)
                return;

            var numEntries = pkg.ReadInt();
            for (var i = 0; i < numEntries; i++)
            {
                var pkg2 = pkg.ReadPackage();
                var playerID = pkg2.ReadInt();
                LoadKnownMats(playerID, pkg2, dict);
            }
        }

        public static void LoadServerKnownRecipes(Dictionary<long, HashSet<string>> dict, ZPackage pkg)
        {
            if (!IsEnabled)
                return;

            var numEntries = pkg.ReadInt();
            for (var i = 0; i < numEntries; i++)
            {
                var pkg2 = pkg.ReadPackage();
                var playerID = pkg2.ReadInt();
                LoadKnownRecipes(playerID, pkg2, dict);
            }
        }

        public static void ClientSendKnownMats()
        {
            if (!IsEnabled)
                return;

            var player = Player.m_localPlayer;
            if (player == null)
            {
                EpicLoot.LogWarning("PlayerKnownManager.ClientSendKnown: m_localPlayer == null");
                return;
            }

            var pkg = new ZPackage();
            WriteKnownMats(player.m_knownMaterial, pkg);

            EpicLoot.Log($"Sending known: {player.m_knownMaterial.Count} materials");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Name_RPC_ClientKnownMats, pkg);
        } 

        public static void ClientSendKnownRecipes()
        {
            if (!IsEnabled)
                return;

            var player = Player.m_localPlayer;
            if (player == null)
            {
                EpicLoot.LogWarning("PlayerKnownManager.ClientSendKnown: m_localPlayer == null");
                return;
            }

            var pkg = new ZPackage();
            WriteKnownRecipes(player.m_knownRecipes, pkg);

            EpicLoot.Log($"Sending known: {player.m_knownRecipes.Count} recipes");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Name_RPC_ClientKnownRecipes, pkg);
        }

        public static int LoadKnownRecipes(long sender, ZPackage pkg, Dictionary<long, HashSet<string>> outDict)
        {
            if (!IsEnabled)
                return 0;

            var pkgKnown = pkg.ReadPackage();
            var tempKnown = new HashSet<string>();
            var numKnown = pkgKnown.ReadInt();
            for (var i = 0; i < numKnown; i++)
            {
                tempKnown.Add(pkgKnown.ReadString());
            }
            SetKnownRecipes(sender, tempKnown, outDict);
            return tempKnown.Count;
        }

        public static int LoadKnownMats(long sender, ZPackage pkg, Dictionary<long, HashSet<string>> outDict)
        {
            if (!IsEnabled)
                return 0;

            var pkgKnown = pkg.ReadPackage();
            var tempKnown = new HashSet<string>();
            var numKnown = pkgKnown.ReadInt();
            for (var i = 0; i < numKnown; i++)
            {
                tempKnown.Add(pkgKnown.ReadString());
            }
            SetKnownMats(sender, tempKnown, outDict);
            return tempKnown.Count;
        }

        public static void SetKnownRecipes(long sender, HashSet<string> hashset, Dictionary<long, HashSet<string>> outDict)
        {
            if (!IsEnabled)
                return;

            if (outDict.ContainsKey(sender))
            {
                outDict.Remove(sender);
            }
            outDict.Add(sender, hashset);
        }

        public static void SetKnownMats(long sender, HashSet<string> hashset, Dictionary<long, HashSet<string>> outDict)
        {
            if (!IsEnabled)
                return;

            if (outDict.ContainsKey(sender))
            {
                outDict.Remove(sender);
            }
            outDict.Add(sender, hashset);
        }

        public static void WriteKnownRecipes(HashSet<string> known, ZPackage outPkg)
        {
            if (!IsEnabled)
                return;

            var pkg = new ZPackage();
            pkg.Write(known.Count);
            foreach (var s in known)
            {
                pkg.Write(s);
            }
            outPkg.Write(pkg);
        }

        public static void WriteKnownMats(HashSet<string> known, ZPackage outPkg)
        {
            if (!IsEnabled)
                return;

            var pkg = new ZPackage();
            pkg.Write(known.Count);
            foreach (var s in known)
            {
                pkg.Write(s);
            }
            outPkg.Write(pkg);
        }

        public static bool IsItemKnown(string itemName)
        {
            if (!IsEnabled)
                return false;

            return PlayerKnownMaterial.Values.Any(material => material.Contains(itemName));
        }

        public static bool IsRecipeKnown(string recipeName)
        {
            if (!IsEnabled)
                return false;

            return PlayerKnownRecipes.Values.Any(recipes => recipes.Contains(recipeName));
        }
    }
}
