# Extended Item Data Framework v1.0.7

Author: Randy Knapp
Source: [Github](https://github.com/RandyKnapp/ValheimMods/tree/main/ExtendedItemDataFramework)

Provides a way to easily modify items, consistently and losslessly save their data, uniquely identify item instances, and extend item functionality.

Please note that this is a work in progress. This mod absolutely should not destroy or corrupt your items, but if it does, please report bugs to the Nexus or the Github so they can be addressed promptly.

## Players: How to Use
If you're just a Valheim player that loves mods, you'll probably only need to install this mod as a dependency for some other mod.

### Installing:

Use a mod manager or install manually by downloading the file, unzipping it, and copying the ExtendedItemDataFramework folder to you BepInEx plugins folder. That's it! You're good to go!

### Uninstalling:
When uninstalling this mod, first go to the config file in the BepInEx config directory (`randyknapp.mods.extendeditemdataframework.cfg`) and set `Enabled` to `false`. Run the game one more time, load a world, and then quit. This will save the game, reserializing out the correct data. Uninstall any mods dependent on this mod BEFORE uninstalling it.

### Config Options:
  * **Enabled (bool)**: Turn off to disable this mod. When uninstalling, load and quit a game once with this option set to false.
  * **Logging Enabled (bool)**: Enables log output from the mod (there's a lot).
  * **Display UniqueItemID in Tooltip (bool)**: If enabled, displays an item's unique id in magenta text at the bottom of the tooltip.

## Modders: How to Use
So you want to make some cool item stuff, but there's no convenient way to: A) uniquely identify items, or B) safely and consistently save custom item. That's where Extended Item Data Framework comes in.

### Under the Hood, How it Works
`ItemData` is currently saved by the game in two ways:
1. It's saved to an `Inventory` object to a `ZPackage`, which is saved by whatever owns the `Inventory` like the player or a container.
2. It's saved by an `ItemDrop` component into a `ZDO`.

**Extended Item Data Framework** works by capturing the save data capability of the `m_crafterName` string field of the `ItemData` object. This field is already capable of arbitrary length, the game already knows about it, and it already gets saved by the built-in systems. No need to mess about with modifying ZDOs, patching the Save/Load methods, or anything like that. It can also contain any characters, since it's not parsed by the game at all. Just written to binary using the string length.

So what **Extended Item Data Framework** does is create an `ExtendedItemData` object, which derives from `ItemData`, and replace the standard `ItemData` with the extended one in each place the game creates items. `ExtendedItemData` contains a list of objects that extend `BaseExtendedItemComponent`. Each one can save and load data. And each one can be accessed through the `ExtendedItemData` object.

Included in **Extended Item Data Framework** are two basic components: `CrafterNameData` and `UniqueItemData`.

> `CrafterNameData` exists to store the vanilla data of the crafting character, since we're commandeering the field that was originally used to store that data.

> `UniqueItemData` generates a GUID when items are created and saves it as a string. This lets other systems in the game get items by their unique ID, even if they are otherwise identical. The vanilla game does not distinguish identical items from each other. Currently, only items of the following types get a unique ID: OneHandedWeapon, Bow, Shield, Helmet, Chest, Legs, Hands, TwoHandedWeapon, Torch, Shoulder, Utility, Tool.

### How to Make Your Own Extended Item Components
1. Download and install ExtendedItemDataFramework to Valheim (see above).
2. Ensure your mod is dependent on Extended Item Data Framework by including a dependency in your own mod:
    ```
    [BepInPlugin("com.me.mods.mynewitemmod", "MyNewItemMod", "1.0.0)]
    [BepInDependency("randyknapp.mods.extendeditemdataframework")]
    public class MyNewItemMod : BaseUnityPlugin
    {
        ...
    }
    ```
3. Add a reference to `ExtendedItemDataFramework.dll` in your mod project.
4. Create a `public class` that extends `ExtendedItemDataFramework.BaseExtendedItemComponent` and implements its constructor and abstract members.
    * **`Constructor`**: The constructor must have one parameter: `ExtendedItemData parent` and it must call the base constructor by passing in it's fully qualified type name. Get that by calling `typeof(MyCoolItemComponent).AssemblyQualifiedName`. Example constructor:
	     ```
		 public UniqueItemData(ExtendedItemData parent) 
	         : base(typeof(UniqueItemData).AssemblyQualifiedName, parent)
	     { }
		 ```
    * **`string Serialize()`**: Return a string of whatever data your item component needs to save.
    * **`void Deserialize(string data)`**: Given the same string you saved in `Serialize`, initialize your component.
    * **`BaseExtendedItemComponent Clone()`**: The item to which your component is attached is being cloned, usually to move it around the world or from one inventory to another. Implement a function that returns a copy (a deep copy if necessary) of your component. The cloned object will be considered to be the same item as the original.  *Note: In Valheim, items which are cloned have their original destroyed immediately after.*
5. When the data for your component changes. Call the base `Save()` method. This will write your data to the `ItemData` so that it will get saved next time the `ItemData` is saved by the game.
6. Hook into the global item events to add your own components at runtime. `ExtendedItemData` exposes two static events for when item data is extended:
    * **`NewExtendedItemData(ExtendedItemData)`**: This occurs when an item is created at runtime in the game world. The passed in parameter to the event handler is the just-converted ExtendedItemData. You can do what you want with it in your handler, but usually you'll check if it matches some condition and then add your component.
    * **`LoadExtendedItemData(ExtendedItemData)`**: This occurs when an already extended item has been loaded from save data. Your component will already be deserialized and attached, but you could do whatever initialization you need to do here.
7. (Optional) Create `ItemData` extension methods to easily access your component or test for its existence. The extensions included in Extended Item Data Framework are:
    * **`bool IsExtended()`**: Returns true if the item has been extended.
    * **`ExtendedItemData Extended()`**: Returns the item cast to an `ExtendedItemData`.
    * **`string GetCrafterName()`**: Returns the real crafter name (not the new save data).
    * **`void SetCrafterName(string crafterName)`**: Sets the crafter name.
    * **`bool IsUnique()`**: Returns true if the item has a `UniqueItemData` component.
    * **`string GetUniqueId()`**: Returns the unique ID of the item.
8. Patch the required methods in the game to implement your desired functionality. In general, get access to your component by using the following example code where `itemData` is an `ItemDrop.ItemData` object:
    ```
    itemData.Extended()?.GetComponent<MyCoolItemComponent>()
    ```
9. As of version 1.0.7, the extended data of equipped items is no longer automatically replicated to other players by ZDO. You will be responsible for setting whatever data you need with your own custom ZDO/RPC code.
10. Some notes:
	* Extended Item Data Framework doesn't use json to serialize the components, but it does do some custom parsing. The custom delimiters are `"<|"` and `"|>"`. Technically you *can* use them in your serialized data, as the framework will escape them with a unique string, but the author recommends that you avoid it. For reference, the unique strings of the escaped delimiters are: `"randyknapp.mods.extendeditemdataframework.startdelimiter"` and `"randyknapp.mods.extendeditemdataframework.enddelimiter"`.
	* If you have any questions or feedback, please post comments on the github or reach out directly to the author at randy.bravo2@gmail.com.
