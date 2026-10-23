using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ItemFavoriteFrameworkAPI {
    /// <summary>
    /// Typed access to Item Favorite Framework's <c>ItemFavoriteFramework.API</c> without referencing it.
    /// Either bundle ItemFavoriteFrameworkAPI.dll into your plugin (ILRepack) or copy this one file into
    /// your project; it has no other dependencies.
    ///
    /// Declare a soft dependency so the framework loads first:
    /// <c>[BepInDependency("MidnightsFX.ItemFavoriteFramework", BepInDependency.DependencyFlags.SoftDependency)]</c>
    ///
    /// Every method is safe to call when the framework is not installed: queries answer false, 0 or an
    /// empty list, and changes answer false. Each endpoint is bound once to a typed delegate, so a call
    /// costs no reflection. Call from the main thread; everything answers for the local player.
    /// </summary>
    public static class ItemFavorites {
        /// <summary>The framework's BepInEx plugin GUID.</summary>
        public const string PluginGuid = "MidnightsFX.ItemFavoriteFramework";

        /// <summary>Bit of <see cref="GetMarks"/>: the item itself is favorited.</summary>
        public const int MarkItemFavorite = 1;
        /// <summary>Bit of <see cref="GetMarks"/>: the item sits in a favorited slot of the player's inventory.</summary>
        public const int MarkSlotFavorite = 2;
        /// <summary>Bit of <see cref="GetMarks"/>: the item is flagged as trash.</summary>
        public const int MarkTrash = 4;

        /// <summary>Mark id <see cref="AddMarkListener"/> reports for the favorite mark.</summary>
        public const string MarkIdFavorite = "favorite";
        /// <summary>Mark id <see cref="AddMarkListener"/> reports for the trash mark.</summary>
        public const string MarkIdTrash = "trash";
        /// <summary><see cref="AddItemClickHandler"/> button: left.</summary>
        public const int ButtonLeft = 0;
        /// <summary><see cref="AddItemClickHandler"/> button: right.</summary>
        public const int ButtonRight = 1;

        /// <summary>API 3. <see cref="SubmitRollVote"/> choice: pass.</summary>
        public const int RollPass = 1;
        /// <summary>API 3. <see cref="SubmitRollVote"/> choice: greed.</summary>
        public const int RollGreed = 2;
        /// <summary>API 3. <see cref="SubmitRollVote"/> choice: need.</summary>
        public const int RollNeed = 3;

        private const string ApiTypeName = "ItemFavoriteFramework.API, ItemFavoriteFramework";

        // The two endpoints with ref parameters need their own delegate types.
        private delegate bool StartRollFn(ItemDrop drop, string playerIdsJson, string optionsJson, ref string rollId, ref string error);
        private delegate bool StartRollForItemFn(ItemDrop.ItemData item, Vector3 position, string playerIdsJson, string optionsJson, ref string rollId, ref string error);

        private static bool bound;
        private static Type apiType;

        private static Func<int> getApiVersion;
        private static Func<string> getPluginVersion;
        private static Func<string, bool> hasEndpoint;
        private static Func<bool> isReady;
        private static Func<ItemDrop.ItemData, bool> isItemFavorited;
        private static Func<ItemDrop.ItemData, bool> isItemTrashFlagged;
        private static Func<int, int, bool> isSlotFavorited;
        private static Func<ItemDrop.ItemData, bool> isItemInFavoritedSlot;
        private static Func<ItemDrop.ItemData, bool> isProtected;
        private static Func<ItemDrop.ItemData, int> getMarks;
        private static Func<Inventory, List<ItemDrop.ItemData>> getFavoritedItems;
        private static Func<Inventory, List<ItemDrop.ItemData>> getTrashFlaggedItems;
        private static Func<Inventory, List<ItemDrop.ItemData>> getProtectedItems;
        private static Func<List<Vector2i>> getFavoritedSlots;
        private static Func<ItemDrop.ItemData, bool, bool> setItemFavorited;
        private static Func<ItemDrop.ItemData, bool, bool> setItemTrashFlagged;
        private static Func<int, int, bool, bool> setSlotFavorited;
        private static Action<Action> addChangedListener;
        private static Action<Action> removeChangedListener;
        private static Func<Sprite> getFavoriteIcon;
        private static Func<Sprite> getTrashIcon;
        private static Func<Color> getFavoriteColor;
        private static Func<Color> getFavoritedSlotColor;
        private static Func<Color> getTrashColor;
        private static Func<string, string, Color, Sprite, string, bool> registerMarkType;
        private static Func<string, bool> isMarkTypeRegistered;
        private static Func<ItemDrop.ItemData, string, bool> hasCustomMark;
        private static Func<ItemDrop.ItemData, List<string>> getCustomMarks;
        private static Func<ItemDrop.ItemData, string, bool, bool> setCustomMark;
        private static Action<Action<ItemDrop.ItemData, string, bool>> addMarkListener;
        private static Action<Action<ItemDrop.ItemData, string, bool>> removeMarkListener;
        private static Action<Func<Inventory, ItemDrop.ItemData, int, bool>> addItemClickHandler;
        private static Action<Func<Inventory, ItemDrop.ItemData, int, bool>> removeItemClickHandler;
        private static Func<ItemDrop.ItemData, bool> isItemWishlisted;
        private static Func<string> getWishlistJson;
        private static Func<string, string, bool> addWishlistEntry;
        private static Func<ItemDrop.ItemData, bool> addWishlistEntryFromItem;
        private static Func<string, int> removeWishlistEntries;
        private static Action<Action> addWishlistChangedListener;
        private static Action<Action> removeWishlistChangedListener;
        private static Action openWishlistEditor;
        private static Func<string, Func<string, string>, Func<ItemDrop.ItemData, string, bool>, Func<string, string>, Func<ItemDrop.ItemData, string>, bool> registerWishlistProvider;
        private static Func<string, bool> unregisterWishlistProvider;
        private static Func<Sprite> getWishlistIcon;
        private static Func<Color> getWishlistColor;
        private static Action<Func<ItemDrop.ItemData, string>> addItemNameProvider;
        private static Action<Func<ItemDrop.ItemData, string>> removeItemNameProvider;
        private static Func<string> getServerFeaturesJson;
        private static Func<string> getConnectedPlayersJson;
        private static Func<long, string> getPlayerWishlistJson;
        private static Func<ItemDrop.ItemData, string> getWantedByJson;
        private static Func<bool> isRollingAvailable;
        private static StartRollFn startRoll;
        private static StartRollForItemFn startRollForItem;
        private static Func<ItemDrop, string> getRollStateJson;
        private static Func<ItemDrop, bool> isDropLocked;
        private static Func<ItemDrop, long, bool> canPlayerPickUp;
        private static Func<string, int, bool> submitRollVote;
        private static Action<Action<string>> addRollListener;
        private static Action<Action<string>> removeRollListener;
        private static Func<Vector3, float, string> getPlayerIdsNearJson;
        private static Func<Character, string> getDamagerPlayerIdsJson;

        /// <summary>True when Item Favorite Framework is installed and its API was found.</summary>
        public static bool IsLoaded() {
            Bind();
            return apiType != null;
        }

        /// <summary>The framework's API version, or 0 when it is not installed.</summary>
        public static int GetApiVersion() {
            Bind();
            return getApiVersion != null ? getApiVersion() : 0;
        }

        /// <summary>The framework's plugin version, or an empty string when it is not installed.</summary>
        public static string GetPluginVersion() {
            Bind();
            return getPluginVersion != null ? getPluginVersion() : "";
        }

        /// <summary>Whether the installed framework has an endpoint of this name. Use it to gate anything newer than the oldest version you support.</summary>
        public static bool HasEndpoint(string name) {
            Bind();
            return hasEndpoint != null && hasEndpoint(name);
        }

        /// <summary>True once the local character's marks are loaded.</summary>
        public static bool IsReady() {
            Bind();
            return isReady != null && isReady();
        }

        /// <summary>Favorited, either this specific item (weapons, armor, tools) or its whole type (stackables).</summary>
        public static bool IsItemFavorited(ItemDrop.ItemData item) {
            Bind();
            return isItemFavorited != null && isItemFavorited(item);
        }

        /// <summary>Flagged as trash. Never true for a favorited item.</summary>
        public static bool IsItemTrashFlagged(ItemDrop.ItemData item) {
            Bind();
            return isItemTrashFlagged != null && isItemTrashFlagged(item);
        }

        /// <summary>A favorited cell of the local player's inventory grid.</summary>
        public static bool IsSlotFavorited(int x, int y) {
            Bind();
            return isSlotFavorited != null && isSlotFavorited(x, y);
        }

        /// <summary>The item is in the local player's inventory, in a favorited cell.</summary>
        public static bool IsItemInFavoritedSlot(ItemDrop.ItemData item) {
            Bind();
            return isItemInFavoritedSlot != null && isItemInFavoritedSlot(item);
        }

        /// <summary>
        /// Favorited, or sitting in a favorited slot. Skip these in anything that moves items out of the
        /// player's inventory automatically (store all, quick stack, sort) or destroys them.
        /// </summary>
        public static bool IsProtected(ItemDrop.ItemData item) {
            Bind();
            return isProtected != null && isProtected(item);
        }

        /// <summary><see cref="MarkItemFavorite"/> | <see cref="MarkSlotFavorite"/> | <see cref="MarkTrash"/>, or 0.</summary>
        public static int GetMarks(ItemDrop.ItemData item) {
            Bind();
            return getMarks != null ? getMarks(item) : 0;
        }

        /// <summary>Favorited items of an inventory; null means the local player's.</summary>
        public static List<ItemDrop.ItemData> GetFavoritedItems(Inventory inventory) {
            Bind();
            return getFavoritedItems != null ? getFavoritedItems(inventory) : new List<ItemDrop.ItemData>();
        }

        /// <summary>Trash-flagged items of an inventory; null means the local player's.</summary>
        public static List<ItemDrop.ItemData> GetTrashFlaggedItems(Inventory inventory) {
            Bind();
            return getTrashFlaggedItems != null ? getTrashFlaggedItems(inventory) : new List<ItemDrop.ItemData>();
        }

        /// <summary>Items <see cref="IsProtected"/> answers true for; null means the local player's inventory.</summary>
        public static List<ItemDrop.ItemData> GetProtectedItems(Inventory inventory) {
            Bind();
            return getProtectedItems != null ? getProtectedItems(inventory) : new List<ItemDrop.ItemData>();
        }

        /// <summary>Favorited cells of the local player's inventory grid, sorted by row then column. A sort should leave these cells alone.</summary>
        public static List<Vector2i> GetFavoritedSlots() {
            Bind();
            return getFavoritedSlots != null ? getFavoritedSlots() : new List<Vector2i>();
        }

        /// <summary>True when the item is in the requested state afterwards. False when refused (favorite and trash are exclusive) or the framework is absent.</summary>
        public static bool SetItemFavorited(ItemDrop.ItemData item, bool favorited) {
            Bind();
            return setItemFavorited != null && setItemFavorited(item, favorited);
        }

        /// <summary>True when the item is in the requested state afterwards. False when refused (favorite and trash are exclusive) or the framework is absent.</summary>
        public static bool SetItemTrashFlagged(ItemDrop.ItemData item, bool trashFlagged) {
            Bind();
            return setItemTrashFlagged != null && setItemTrashFlagged(item, trashFlagged);
        }

        /// <summary>True when the slot is in the requested state afterwards.</summary>
        public static bool SetSlotFavorited(int x, int y, bool favorited) {
            Bind();
            return setSlotFavorited != null && setSlotFavorited(x, y, favorited);
        }

        /// <summary>Called after any mark changes and after a character's marks load. Re-query what you display.</summary>
        public static void AddChangedListener(Action listener) {
            Bind();
            addChangedListener?.Invoke(listener);
        }

        /// <summary>Removes a listener added with <see cref="AddChangedListener"/>.</summary>
        public static void RemoveChangedListener(Action listener) {
            Bind();
            removeChangedListener?.Invoke(listener);
        }

        /// <summary>A white star with a dark outline, or null. Tint it with <see cref="GetFavoriteColor"/>.</summary>
        public static Sprite GetFavoriteIcon() {
            Bind();
            return getFavoriteIcon != null ? getFavoriteIcon() : null;
        }

        /// <summary>A white X with a dark outline, or null. Tint it with <see cref="GetTrashColor"/>.</summary>
        public static Sprite GetTrashIcon() {
            Bind();
            return getTrashIcon != null ? getTrashIcon() : null;
        }

        /// <summary>The player's favorited-item border colour.</summary>
        public static Color GetFavoriteColor() {
            Bind();
            return getFavoriteColor != null ? getFavoriteColor() : new Color(1f, 0.85f, 0f);
        }

        /// <summary>The player's favorited-slot border colour.</summary>
        public static Color GetFavoritedSlotColor() {
            Bind();
            return getFavoritedSlotColor != null ? getFavoritedSlotColor() : new Color(0f, 0.5f, 1f);
        }

        /// <summary>The player's trash border colour.</summary>
        public static Color GetTrashColor() {
            Bind();
            return getTrashColor != null ? getTrashColor() : new Color(0.5f, 0f, 0f);
        }

        /// <summary>
        /// API 2. Registers your own item mark: drawn as a border in <paramref name="color"/> (outranking
        /// favorite and trash), with <paramref name="icon"/> in the cell's top-left corner when not null, and
        /// <paramref name="tooltipToken"/> as a tooltip line when not empty. Stored on the item instance
        /// (stackables too), so it travels with the item; you decide what it means and clear it when done.
        /// Marks with the same non-empty <paramref name="group"/> are exclusive. Use a namespaced id without
        /// spaces, e.g. "mymod.loot.need". False when the framework is absent, too old, or the id is invalid.
        /// </summary>
        public static bool RegisterMarkType(string id, string group, Color color, Sprite icon, string tooltipToken) {
            Bind();
            return registerMarkType != null && registerMarkType(id, group, color, icon, tooltipToken);
        }

        /// <summary>API 2. Whether a mark type of this id is registered.</summary>
        public static bool IsMarkTypeRegistered(string id) {
            Bind();
            return isMarkTypeRegistered != null && isMarkTypeRegistered(id);
        }

        /// <summary>API 2. Whether the item carries this custom mark.</summary>
        public static bool HasCustomMark(ItemDrop.ItemData item, string id) {
            Bind();
            return hasCustomMark != null && hasCustomMark(item, id);
        }

        /// <summary>API 2. The custom mark ids the item carries, in registration order.</summary>
        public static List<string> GetCustomMarks(ItemDrop.ItemData item) {
            Bind();
            return getCustomMarks != null ? getCustomMarks(item) : new List<string>();
        }

        /// <summary>API 2. Sets or clears a custom mark; setting one clears the others of its group. True when the item is in the requested state afterwards.</summary>
        public static bool SetCustomMark(ItemDrop.ItemData item, string id, bool marked) {
            Bind();
            return setCustomMark != null && setCustomMark(item, id, marked);
        }

        /// <summary>API 2. Called with the item, the mark id (<see cref="MarkIdFavorite"/>, <see cref="MarkIdTrash"/> or a custom id) and whether it is now set.</summary>
        public static void AddMarkListener(Action<ItemDrop.ItemData, string, bool> listener) {
            Bind();
            addMarkListener?.Invoke(listener);
        }

        /// <summary>API 2. Removes a listener added with <see cref="AddMarkListener"/>.</summary>
        public static void RemoveMarkListener(Action<ItemDrop.ItemData, string, bool> listener) {
            Bind();
            removeMarkListener?.Invoke(listener);
        }

        /// <summary>
        /// API 2. Called on mouse down on any inventory cell with the grid's inventory, the item in the cell
        /// (null when empty) and the button (<see cref="ButtonLeft"/>, <see cref="ButtonRight"/>). Return true
        /// to take the click: vanilla and the favoriting clicks then ignore it. Take clicks on your own
        /// inventories only.
        /// </summary>
        public static void AddItemClickHandler(Func<Inventory, ItemDrop.ItemData, int, bool> handler) {
            Bind();
            addItemClickHandler?.Invoke(handler);
        }

        /// <summary>API 2. Removes a handler added with <see cref="AddItemClickHandler"/>.</summary>
        public static void RemoveItemClickHandler(Func<Inventory, ItemDrop.ItemData, int, bool> handler) {
            Bind();
            removeItemClickHandler?.Invoke(handler);
        }

        // -------------------------------------------------------------------------------------------------
        // Wishlist (API 3)
        // -------------------------------------------------------------------------------------------------

        /// <summary>API 3. The item matches an entry of the local player's wishlist.</summary>
        public static bool IsItemWishlisted(ItemDrop.ItemData item) {
            Bind();
            return isItemWishlisted != null && isItemWishlisted(item);
        }

        /// <summary>
        /// API 3. The local wishlist as JSON: [{"id":3,"prefab":"SwordIron","name":"Iron sword","autoNeed":false,
        /// "criteria":[{"provider":"...","json":"{...}","desc":"..."}]}]. "[]" when the framework is absent.
        /// </summary>
        public static string GetWishlistJson() {
            Bind();
            return getWishlistJson != null ? getWishlistJson() : "[]";
        }

        /// <summary>
        /// API 3. Adds an entry: an item prefab name, and "" or {"&lt;providerId&gt;": {criteria}} for conditions.
        /// True when it is on the list afterwards.
        /// </summary>
        public static bool AddWishlistEntry(string prefabName, string criteriaJson) {
            Bind();
            return addWishlistEntry != null && addWishlistEntry(prefabName, criteriaJson);
        }

        /// <summary>API 3. Adds the item as an entry, with the conditions the providers suggest for it.</summary>
        public static bool AddWishlistEntryFromItem(ItemDrop.ItemData item) {
            Bind();
            return addWishlistEntryFromItem != null && addWishlistEntryFromItem(item);
        }

        /// <summary>API 3. Removes every entry for that prefab; returns how many.</summary>
        public static int RemoveWishlistEntries(string prefabName) {
            Bind();
            return removeWishlistEntries != null ? removeWishlistEntries(prefabName) : 0;
        }

        /// <summary>API 3. Called after the local wishlist changes or loads.</summary>
        public static void AddWishlistChangedListener(Action listener) {
            Bind();
            addWishlistChangedListener?.Invoke(listener);
        }

        /// <summary>API 3. Removes a listener added with <see cref="AddWishlistChangedListener"/>.</summary>
        public static void RemoveWishlistChangedListener(Action listener) {
            Bind();
            removeWishlistChangedListener?.Invoke(listener);
        }

        /// <summary>API 3. Opens the wishlist editor.</summary>
        public static void OpenWishlistEditor() {
            Bind();
            openWishlistEditor?.Invoke();
        }

        /// <summary>
        /// API 3. Registers wishlist conditions of your own (see docs/API.md for the schema).
        /// <paramref name="matches"/>(item, criteriaJson) must depend on its arguments only: it is also run
        /// on other players' entries. <paramref name="suggest"/> may be null.
        /// </summary>
        public static bool RegisterWishlistProvider(string providerId, Func<string, string> getSchemaJson,
            Func<ItemDrop.ItemData, string, bool> matches, Func<string, string> describe, Func<ItemDrop.ItemData, string> suggest) {
            Bind();
            return registerWishlistProvider != null && registerWishlistProvider(providerId, getSchemaJson, matches, describe, suggest);
        }

        /// <summary>API 3. Removes a provider registered with <see cref="RegisterWishlistProvider"/>.</summary>
        public static bool UnregisterWishlistProvider(string providerId) {
            Bind();
            return unregisterWishlistProvider != null && unregisterWishlistProvider(providerId);
        }

        /// <summary>API 3. A white heart with a dark outline, or null. Tint it with <see cref="GetWishlistColor"/>.</summary>
        public static Sprite GetWishlistIcon() {
            Bind();
            return getWishlistIcon != null ? getWishlistIcon() : null;
        }

        /// <summary>API 3. The player's wishlist colour.</summary>
        public static Color GetWishlistColor() {
            Bind();
            return getWishlistColor != null ? getWishlistColor() : new Color(0.95f, 0.36f, 0.55f);
        }

        /// <summary>API 3. Names items in roll windows and loot messages; return null or "" to pass.</summary>
        public static void AddItemNameProvider(Func<ItemDrop.ItemData, string> provider) {
            Bind();
            addItemNameProvider?.Invoke(provider);
        }

        /// <summary>API 3. Removes a provider added with <see cref="AddItemNameProvider"/>.</summary>
        public static void RemoveItemNameProvider(Func<ItemDrop.ItemData, string> provider) {
            Bind();
            removeItemNameProvider?.Invoke(provider);
        }

        // -------------------------------------------------------------------------------------------------
        // Shared wishlists (API 3; the server must run the framework)
        // -------------------------------------------------------------------------------------------------

        /// <summary>API 3. {"iff":true,"rolls":true,"wishlists":true,...}; "iff" stays false until the server answered.</summary>
        public static string GetServerFeaturesJson() {
            Bind();
            return getServerFeaturesJson != null ? getServerFeaturesJson() : "{\"iff\":false}";
        }

        /// <summary>API 3. [{"player":"123","name":"Alice","iff":true,"sharesWishlist":true}].</summary>
        public static string GetConnectedPlayersJson() {
            Bind();
            return getConnectedPlayersJson != null ? getConnectedPlayersJson() : "[]";
        }

        /// <summary>API 3. Another player's shared wishlist, in <see cref="GetWishlistJson"/>'s shape.</summary>
        public static string GetPlayerWishlistJson(long playerId) {
            Bind();
            return getPlayerWishlistJson != null ? getPlayerWishlistJson(playerId) : "[]";
        }

        /// <summary>API 3. Who wants the item: [{"player":"123","name":"Alice","self":false}].</summary>
        public static string GetWantedByJson(ItemDrop.ItemData item) {
            Bind();
            return getWantedByJson != null ? getWantedByJson(item) : "[]";
        }

        // -------------------------------------------------------------------------------------------------
        // Loot rolls (API 3; the server must run the framework with EnableLootRolls on)
        // -------------------------------------------------------------------------------------------------

        /// <summary>API 3. Whether a roll asked for on this machine can start right now.</summary>
        public static bool IsRollingAvailable() {
            Bind();
            return isRollingAvailable != null && isRollingAvailable();
        }

        /// <summary>
        /// API 3. Starts a need/greed/pass roll on a drop this machine just spawned (and so owns), among the
        /// players in <paramref name="playerIdsJson"/> (["123","456"], see <see cref="PlayerIdsToJson"/>).
        /// <paramref name="optionsJson"/>: "" or {"label","source","groupId","rollSeconds","reserveSeconds"}.
        /// On false, <paramref name="error"/> says why ("notInstalled" when the framework is absent).
        /// </summary>
        public static bool StartRoll(ItemDrop drop, string playerIdsJson, string optionsJson, out string rollId, out string error) {
            Bind();
            rollId = "";
            error = "notInstalled";
            if (startRoll == null) { return false; }
            string id = "", why = "";
            bool started = startRoll(drop, playerIdsJson, optionsJson, ref id, ref why);
            rollId = id;
            error = why;
            return started;
        }

        /// <summary>
        /// API 3. Spawns the item as a drop at <paramref name="position"/> and starts a roll on it. On false the
        /// spawned drop is removed again, so the item can be handed out another way.
        /// </summary>
        public static bool StartRollForItem(ItemDrop.ItemData item, Vector3 position, string playerIdsJson, string optionsJson, out string rollId, out string error) {
            Bind();
            rollId = "";
            error = "notInstalled";
            if (startRollForItem == null) { return false; }
            string id = "", why = "";
            bool started = startRollForItem(item, position, playerIdsJson, optionsJson, ref id, ref why);
            rollId = id;
            error = why;
            return started;
        }

        /// <summary>API 3. {"rollId","phase":"free|rolling|reserved","winner","winnerName","secondsLeft","lockedForMe"}.</summary>
        public static string GetRollStateJson(ItemDrop drop) {
            Bind();
            return getRollStateJson != null ? getRollStateJson(drop) : "{\"phase\":\"free\"}";
        }

        /// <summary>API 3. Under a roll or reserved for anyone: vacuums and auto-storage should leave it alone.</summary>
        public static bool IsDropLocked(ItemDrop drop) {
            Bind();
            return isDropLocked != null && isDropLocked(drop);
        }

        /// <summary>API 3. Whether that player may take the drop now. True when the framework is absent.</summary>
        public static bool CanPlayerPickUp(ItemDrop drop, long playerId) {
            Bind();
            return canPlayerPickUp == null || canPlayerPickUp(drop, playerId);
        }

        /// <summary>API 3. Votes for the local player: <see cref="RollNeed"/>, <see cref="RollGreed"/> or <see cref="RollPass"/>.</summary>
        public static bool SubmitRollVote(string rollId, int choice) {
            Bind();
            return submitRollVote != null && submitRollVote(rollId, choice);
        }

        /// <summary>
        /// API 3. Roll events as JSON with "event" (accepted, refused, prompt, vote, result, lock) and "side"
        /// ("server" or "client"). See docs/API.md for each event's fields.
        /// </summary>
        public static void AddRollListener(Action<string> listener) {
            Bind();
            addRollListener?.Invoke(listener);
        }

        /// <summary>API 3. Removes a listener added with <see cref="AddRollListener"/>.</summary>
        public static void RemoveRollListener(Action<string> listener) {
            Bind();
            removeRollListener?.Invoke(listener);
        }

        /// <summary>API 3. Players within <paramref name="radius"/> of a point, as a JSON id list.</summary>
        public static string GetPlayerIdsNearJson(Vector3 point, float radius) {
            Bind();
            return getPlayerIdsNearJson != null ? getPlayerIdsNearJson(point, radius) : "[]";
        }

        /// <summary>API 3. Players who hit the character, as a JSON id list. Call on its owner before it is destroyed.</summary>
        public static string GetDamagerPlayerIdsJson(Character character) {
            Bind();
            return getDamagerPlayerIdsJson != null ? getDamagerPlayerIdsJson(character) : "[]";
        }

        /// <summary>Formats player ids the way the roll endpoints take them: ["123","456"].</summary>
        public static string PlayerIdsToJson(IEnumerable<long> playerIds) {
            return "[" + string.Join(",", playerIds.Select(id => "\"" + id.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\"").ToArray()) + "]";
        }

        // Once. Declare the soft dependency: without it your plugin may load before the framework, the
        // lookup finds nothing, and every call stays a no-op for the session.
        private static void Bind() {
            if (bound) { return; }
            bound = true;

            apiType = Type.GetType(ApiTypeName, throwOnError: false);
            if (apiType == null) { return; }

            getApiVersion = Get<Func<int>>("GetApiVersion");
            getPluginVersion = Get<Func<string>>("GetPluginVersion");
            hasEndpoint = Get<Func<string, bool>>("HasEndpoint");
            isReady = Get<Func<bool>>("IsReady");
            isItemFavorited = Get<Func<ItemDrop.ItemData, bool>>("IsItemFavorited");
            isItemTrashFlagged = Get<Func<ItemDrop.ItemData, bool>>("IsItemTrashFlagged");
            isSlotFavorited = Get<Func<int, int, bool>>("IsSlotFavorited");
            isItemInFavoritedSlot = Get<Func<ItemDrop.ItemData, bool>>("IsItemInFavoritedSlot");
            isProtected = Get<Func<ItemDrop.ItemData, bool>>("IsProtected");
            getMarks = Get<Func<ItemDrop.ItemData, int>>("GetMarks");
            getFavoritedItems = Get<Func<Inventory, List<ItemDrop.ItemData>>>("GetFavoritedItems");
            getTrashFlaggedItems = Get<Func<Inventory, List<ItemDrop.ItemData>>>("GetTrashFlaggedItems");
            getProtectedItems = Get<Func<Inventory, List<ItemDrop.ItemData>>>("GetProtectedItems");
            getFavoritedSlots = Get<Func<List<Vector2i>>>("GetFavoritedSlots");
            setItemFavorited = Get<Func<ItemDrop.ItemData, bool, bool>>("SetItemFavorited");
            setItemTrashFlagged = Get<Func<ItemDrop.ItemData, bool, bool>>("SetItemTrashFlagged");
            setSlotFavorited = Get<Func<int, int, bool, bool>>("SetSlotFavorited");
            addChangedListener = Get<Action<Action>>("AddChangedListener");
            removeChangedListener = Get<Action<Action>>("RemoveChangedListener");
            getFavoriteIcon = Get<Func<Sprite>>("GetFavoriteIcon");
            getTrashIcon = Get<Func<Sprite>>("GetTrashIcon");
            getFavoriteColor = Get<Func<Color>>("GetFavoriteColor");
            getFavoritedSlotColor = Get<Func<Color>>("GetFavoritedSlotColor");
            getTrashColor = Get<Func<Color>>("GetTrashColor");
            registerMarkType = Get<Func<string, string, Color, Sprite, string, bool>>("RegisterMarkType");
            isMarkTypeRegistered = Get<Func<string, bool>>("IsMarkTypeRegistered");
            hasCustomMark = Get<Func<ItemDrop.ItemData, string, bool>>("HasCustomMark");
            getCustomMarks = Get<Func<ItemDrop.ItemData, List<string>>>("GetCustomMarks");
            setCustomMark = Get<Func<ItemDrop.ItemData, string, bool, bool>>("SetCustomMark");
            addMarkListener = Get<Action<Action<ItemDrop.ItemData, string, bool>>>("AddMarkListener");
            removeMarkListener = Get<Action<Action<ItemDrop.ItemData, string, bool>>>("RemoveMarkListener");
            addItemClickHandler = Get<Action<Func<Inventory, ItemDrop.ItemData, int, bool>>>("AddItemClickHandler");
            removeItemClickHandler = Get<Action<Func<Inventory, ItemDrop.ItemData, int, bool>>>("RemoveItemClickHandler");
            isItemWishlisted = Get<Func<ItemDrop.ItemData, bool>>("IsItemWishlisted");
            getWishlistJson = Get<Func<string>>("GetWishlistJson");
            addWishlistEntry = Get<Func<string, string, bool>>("AddWishlistEntry");
            addWishlistEntryFromItem = Get<Func<ItemDrop.ItemData, bool>>("AddWishlistEntryFromItem");
            removeWishlistEntries = Get<Func<string, int>>("RemoveWishlistEntries");
            addWishlistChangedListener = Get<Action<Action>>("AddWishlistChangedListener");
            removeWishlistChangedListener = Get<Action<Action>>("RemoveWishlistChangedListener");
            openWishlistEditor = Get<Action>("OpenWishlistEditor");
            registerWishlistProvider = Get<Func<string, Func<string, string>, Func<ItemDrop.ItemData, string, bool>, Func<string, string>, Func<ItemDrop.ItemData, string>, bool>>("RegisterWishlistProvider");
            unregisterWishlistProvider = Get<Func<string, bool>>("UnregisterWishlistProvider");
            getWishlistIcon = Get<Func<Sprite>>("GetWishlistIcon");
            getWishlistColor = Get<Func<Color>>("GetWishlistColor");
            addItemNameProvider = Get<Action<Func<ItemDrop.ItemData, string>>>("AddItemNameProvider");
            removeItemNameProvider = Get<Action<Func<ItemDrop.ItemData, string>>>("RemoveItemNameProvider");
            getServerFeaturesJson = Get<Func<string>>("GetServerFeaturesJson");
            getConnectedPlayersJson = Get<Func<string>>("GetConnectedPlayersJson");
            getPlayerWishlistJson = Get<Func<long, string>>("GetPlayerWishlistJson");
            getWantedByJson = Get<Func<ItemDrop.ItemData, string>>("GetWantedByJson");
            isRollingAvailable = Get<Func<bool>>("IsRollingAvailable");
            startRoll = Get<StartRollFn>("StartRoll");
            startRollForItem = Get<StartRollForItemFn>("StartRollForItem");
            getRollStateJson = Get<Func<ItemDrop, string>>("GetRollStateJson");
            isDropLocked = Get<Func<ItemDrop, bool>>("IsDropLocked");
            canPlayerPickUp = Get<Func<ItemDrop, long, bool>>("CanPlayerPickUp");
            submitRollVote = Get<Func<string, int, bool>>("SubmitRollVote");
            addRollListener = Get<Action<Action<string>>>("AddRollListener");
            removeRollListener = Get<Action<Action<string>>>("RemoveRollListener");
            getPlayerIdsNearJson = Get<Func<Vector3, float, string>>("GetPlayerIdsNearJson");
            getDamagerPlayerIdsJson = Get<Func<Character, string>>("GetDamagerPlayerIdsJson");
        }

        // Matches the delegate's exact signature, so an endpoint an older framework lacks (or a changed
        // one) stays null and its wrapper keeps returning the default instead of throwing.
        private static TDelegate Get<TDelegate>(string name) where TDelegate : class {
            MethodInfo invoke = typeof(TDelegate).GetMethod("Invoke");
            Type[] parameters = invoke.GetParameters().Select(p => p.ParameterType).ToArray();
            MethodInfo method = apiType.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, parameters, null);
            if (method == null || method.ReturnType != invoke.ReturnType) { return null; }
            return Delegate.CreateDelegate(typeof(TDelegate), method, throwOnBindFailure: false) as TDelegate;
        }
    }
}
