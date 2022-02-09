
## Valheim Modding - Getting Started
Randy Knapp - 2/7/2022

#### Reference:
* All my code can be used as example and instruction and can be found on my github here:  
    [https://github.com/RandyKnapp/ValheimMods](https://github.com/RandyKnapp/ValheimMods)

#### Before Starting:
* It can be useful to make a copy of your Valheim directory (called “Valheim-Dev” or something), that way you can control which mods are installed without having to mess up your main install. If you’re using a mod manager, you probably don’t need to do this. Or if you just want to make some simple stuff and don’t mind manually dealing with your mods, it’s not necessary. You can launch the game manually by running valheim.exe in that folder, you won’t be able to launch it via steam.

#### Setting up Valheim for Testing and Debugging Mods:
* Make sure we are launching Valheim with the console enabled, set your launch options for Valheim to include “-console”. (If you’re using a copy of the valheim dir, you’ll need to make a shortcut to valheim.exe and add the launch option to the target of the shortcut.)
* Install the latest version of BepInEx, you probably already have, but just make sure:
  * [https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
  * After installing BepInEx, I recommend opening its config file (Valheim\BepInEx\config\BepInEx.cfg) and changing `PreventClose = true` to `PreventClose = false`. This will let you shut down Valheim instantly by clicking the close button on the BepInEx output console.
* Install some important utility mods:
  * **RuntimeUnityEditor**:  [Link](https://github.com/ManlyMarco/RuntimeUnityEditor)
  This mod lets you inspect the Unity hierarchy at runtime and modify properties at will, super valuable. Push F12 to bring this up, you may want to rebind the Steam Overlay’s “Take Screenshot” command, which defaults to F12.
  * **Configuration Manager**: [Nexus](https://www.nexusmods.com/valheim/mods/740) [Thunderstore](https://valheim.thunderstore.io/package/cjayride/ConfigurationManager/)
  This mod lets you configure your installed mods with an in-game gui, also useful. Push F1 in game to open this.
  * **Assembly Publicizer**:  [Link](https://github.com/elliotttate/Bepinex-Tools/releases)
  This one is actually really important, it creates versions of the Valheim DLLs that have every class member and method made public, so you don’t have to do convoluted reflection stuff to get access to the private members, and your compiled mod will still link with the normal DLLs no problem.
  * **NOTE:** Run the game once after the publicizer is installed to generate the publicized DLLs! After you have the publicized DLLs, you can uninstall this mod and only re-install it when Valheim updates.

#### Setting up Visual Studio:

* Get Visual Studio 2017 or later (or JetBrains Rider, or anything that can build and compile C#)
    
* Create a new C# Class Library Project in a new solution
  * In that project’s properties, set the following:
    * Application > Target Framework: .NET Framework 4.6.1 (this is what Valheim uses)
    * Build > Allow Unsafe Code: true (this lets us use the publicized dlls)
    * Build Events > Post-build event command line (use your valheim install dir): 
    This will automatically install your new mod after a successful build. As you add assets, you’ll need to modify this command to include copying your assets over as well.
    ```
    xcopy "$(TargetDir)\$(TargetFileName)" "C:\Program Files (x86)\Steam\steamapps\common\Valheim\BepInEx\plugins\" /q /y /i
    ```

* Add the references that you will need for your project. (I will just use “Valheim” to mean your install path. Mine is at C:\Program Files (x86)\Steam\steamapps\common\Valheim)
  * BepInEx.dll: Valheim\BepInEx\core\BepInEx.dll
    * Need this to hook into BepInEx’s modloading stuff
  * 0Harmony: Valheim\BepInEx\core\0Harmony.dll
    * This is what lets us modify the valheim runtime code through patches, lots more on this later
    * BepInEx uses HarmonyX, a custom version of Harmony with streamlined features
  * assembly_valheim_publicized.dll: Valheim\valheim_Data\Managed\publicized_assemblies\assembly_valheim_publicized.dll
    * The main valheim code, this contains almost all the classes that we’ll be modifying
  * UnityEngine.dll: Valheim\unstripped_corlib\UnityEngine.dll
    * Use the ones in the unstripped_corlib director that BepInEx installs, because the ones shipped with Valheim have had all the code not referenced by Valheim stripped out to reduce file-size.
  * Other Unity DLLs: Unity has all its functionality split into many DLLs. You’ll need to reference the ones you actually use. The ones I use the most are, but try to only include what you need:
    * UnityEngine.CoreModule.dll
    * UnityEngine.UI.dll
    * UnityEngine.TextRenderingModule.dll
    * UnityEngine.AssetBundleModule.dll
    * UnityEngine.InputLegacyModule.dll

#### Create your first plugin:
* Create a public class that implements `BaseUnityPlugin` (from BepInEx)
    ```cs
    using BepInEx;  
    public class MyMod : BaseUnityPlugin
    ```
* Create a public const string representing the ID for your mod)
  * (Make up something you like, don't use my name!)
    ```cs
    public const string PluginId = "randyknapp.mods.mymod";
    ```
* Add the `BepInEx` plugin attribute to your class:
    ```cs
    [BepInPlugin(PluginId, "My Mod", "1.0.0")]
    public class MyMod : BaseUnityPlugin
    ```
* Add a Harmony instance and initialize it in this class's `Awake` method, and shut it down in the `OnDestroy` method.
  * `Awake` will get called when BepInEx first loads your mod, and `OnDestroy` when Valheim shuts down.
  * The JetBrains Annotations stuff here (`[UsedImplicitly]`) is optional, it's a free package that you can add to VisualStudio, but it's nice. It can be used to tell the compiler stuff like, hey this function is going to get called even if it doesn't seem like it.
    ```cs
    using System.Reflection;
    using BepInEx;
    using HarmonyLib;
    using JetBrains.Annotations;

    namespace MyMod
    {
        [BepInPlugin(PluginId, "MyMod", "1.0.0")]
        public class MyMod : BaseUnityPlugin
        {
            public const string PluginId = "randyknapp.mods.mymod";

            private Harmony _harmony;

            [UsedImplicitly]
            private void Awake()
            {
                _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);
            }

            [UsedImplicitly]
            private void OnDestroy()
            {
                _harmony?.UnpatchSelf();
            }
        }
    }
    ```
* At this point, you can compile your mod. It should automatically install (because of the post-build event from above). You should see some output like this (in Visual Studio):
    ```
    1>------ Build started: Project: MyMod, Configuration: Debug Any CPU ------
    1>  MyMod -> C:\path\to\src\MyMod\bin\Debug\MyMod.dll
    1>  1 File(s) copied
    ========== Build: 1 succeeded, 0 failed, 0 up-to-date, 0 skipped ==========
    ```
* Run Valheim now and confirm that your mod is being loaded and that there are no errors. You should see something like this in the BepInEx output window:
    ```
    [Info   :   BepInEx] Loading [MyMod 1.0.0]
    ```
* If you're here, great! You just made your first mod!

#### Singleton Reference:
* There's probably some stuff you'll want to reference via static methods that access this class, like logging (see below), so here's how to add a singleton instance to your class:
* Add a static reference to your plugin class
    ```cs
    private static MyMod _instance;
    ```
* Initialize it in `Awake` and null it out in `OnDestroy`
    ```cs
    private void Awake()
    {
        _instance = this;
        // ...
    }

    private void OnDestroy()
    {
        _instance = null;
        // ...
    }
    ```
#### BepInEx Configuration:
* BepInEx provides a built-in configuration system. Here's an example of how to use it.
  * Add the config entry to your class:
    ```cs
    private static ConfigEntry<bool> _loggingEnabled;
    ```
  * In the `Awake` method, load that config:
    ```cs
    _loggingEnabled = Config.Bind("Logging", "Logging Enabled", true, "Enable logging");
    ```

#### Debug Text:
* If you want to write something out to the console you can do it a couple of ways:
  * Use the BepInEx logger and wrap it in the config var we created above

    ```cs
    public static void Log(string message)
    {
        if (_loggingEnabled.Value)
            _instance.Logger.LogInfo(message);
    }

    public static void LogWarning(string message)
    {
        if (_loggingEnabled.Value)
            _instance.Logger.LogWarning(message);
    }

    public static void LogError(string message)
    {
        if (_loggingEnabled.Value)
            _instance.Logger.LogError(message);
    }
    ```
    * You can now write to the log by using `MyMod.Log("message");` anywhere in this project and turn it on or off via the config.
  * *Alternatively:* If you don't care about being able to toggle on and off logging, use `using UnityEngine;` and write `Debug.Log("my message");` (or `LogWarning` or `LogError`)

#### Actually making changes to the game:
  * Now we can start using Harmony to modify the actual code.
  * In Valheim, most of the UI is a singleton instance of a particular class, so it's easy to modify it during its lifetime.
  * In order to see what's going on under the hood, we need to decompile the Valheim source.
    * If you have JetBrains Resharper, it has a built in decompiler. Just Ctrl+Click on a Valheim class or method and it will decompile the source and show you in Visual Studio.
    * Otherwise, you can use a tool like ilSpy [Link](https://github.com/icsharpcode/ILSpy)
  * For our simple example, we're going to change the color of the name of the container in the UI.
  * Create a Harmony patch class with the `HarmonyPatch` attribute inside your plugin. (It doesn't have to be in your plugin, you could make a new file with a new class, it just has to be a `public static` class.)
    ```cs
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    public static class InventoryGui_Awake_Patch
    {
    }
    ```
  * The `HarmonyPatch` attribute takes the type of the class we want to patch, and the name of the method we want to patch. Best practice is to use the `nameof` operator to get the name, rather than a string.
  * In this class, we need to make our patch method. With Harmony, you can make Prefix, Postfix, or Transpiler patches. Prefix gets called before the base method, and you can optionally tell the program to not call the base method. Postfix gets called after the base method, and you can change the return value if you want. Transpiler is a special patch that changes the IL of the base method itself, but that's beyond the scope of this tutorial.
  * Let's make a Postfix patch that takes in the calling instance of the `InventoryGui`
    ```cs
    public static void Postfix(InventoryGui __instance)
    {
    }
    ```
  * The `InventoryGui __instance` parameter is a special parameter name that Harmony can use to pass along the instance of the class that this method is being called on.
  * Now, finally, lets do the real work. We're just going to change the color property of a plain-ol' Unity Text component to a new color, and then we'll test it out:
    ```cs
    public static void Postfix(InventoryGui __instance)
    {
        __instance.m_containerName.color = Color.green;
    }
    ```
  * Go ahead and build, then run Valheim. Load up a game and test it out. If you don't have a container at hand, try using the console commands. Push F5 to open the console, type `-devcommands` and hit enter to enable dev commands. Then enter `spawn Hammer` and `spawn Wood 20`, which should give you enough to build a workbench and a chest. When you open the chest, the color of the label should have changed!
* Congrats! Your first Valheim UI mod!
#### More Resources:
* HarmonyX Documentation: [https://github.com/BepInEx/HarmonyX/wiki](https://github.com/BepInEx/HarmonyX/wiki)
* Join the Valheim Modding Discord: [Invite](https://discord.gg/Dft7SkYHEs)
