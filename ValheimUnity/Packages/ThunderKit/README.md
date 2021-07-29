## _**ThunderKit Crash Course**_

Welcome and thank you for your interest in ThunderKit. ThunderKit is a Unity extension that provides an expansive and expandable environment for mod development.

ThunderKit can help you develop multiple mods in a single project and streamline your building and testing efforts through automation.
Aiming to help you waste less time copying files, managing collections of mods, and acquiring dependencies, so that you can focus on your mods and test them under many different configurations and combinations easily.

Below is a quick overview of how to get started using ThunderKit, showing you the basic parts and pieces you will need to get together to get started making cool stuff!

ThunderKit comes with a template for a simple BepInEx setup that will help you get straight to modding if the game you're modding can use this it.

If you need to build a custom setup, ThunderKit has with integrated documentation that will help you understand how the BepInEx template works with a Tear down guide, so that you can start building a setup that works for your environment.

If you like ThunderKit consider donating through [GitHub Sponsors](https://github.com/sponsors/PassivePicasso)

### Initial Setup

These initial steps are the universal basic setup for any ThunderKit project.

1. Install Unity
    - The version of Unity you install should match the version of the game, the game's executable has its file and product version set, you can check the version of Unity the game uses there. 
    - In Windows you can check the file and product version of the executable by navigating to it in Windows Explorer, right clicking on the executable and selecting properties. The file and product version information is on the Details tab.
    - Download specific versions of Unity from the [Unity Download Archive](https://unity3d.com/get-unity/download/archive)
    - Unity Hub cannot install all versions of Unity directly, however it can install them from links on the Unity Download Archive page.
2. Create a new Unity Project with the installed version of Unity
3. Install ThunderKit
      - Navigate to the Project folder with your file explorer and open the Packages folder 
      - Open the manifest.json file in this folder and add the following to the top of dependencies
      `"com.passivepicasso.thunderkit":"https://github.com/PassivePicasso/ThunderKit.git",` 
      - Save and close the manifest.json file and focus Unity to complete the process.
4. Open the [ThunderKit Settings](menulink://Tools/ThunderKit/Settings) window
5. Click on Locate Game under the ThunderKit settings to locate and select the games executable
      * It may take some time for ThunderKit and Unity to complete the configuration
6. Open the project window from the main menu via Windows/General/Project
7. Create a Manifest by right clicking in any folder under assets and selecting ThunderKit/Manifest

### Next Steps

From this point forward you will need to understand some facets of mod developing in Unity and ThunderKit, these sections will cover those aspects. What you will need to do will vary depending on your projects.

If you're using Thunderstore, check the Thunderstore community for the game for a ThunderKit template.  Users of ThunderKit are encouraged to build and share reusable templates that define standard Pipelines for building mods for games and even specialized toolkits to allow more Unity Editor functionality for modding games.

### ManifestDatum Staging Paths

Each ManifestDatum on a Manifest will have a StagingPaths array. This is where you will indicate to ThunderKit where to deploy your assets. Paths are relative to the project's root folder unless an absolute path is manually specified.  Each entry will be a destination, so if you need to output assemblies or AssetBundles to multiple places, you can add multiple paths. 

### PathReferences

You can create PathReferences by using the project window context menu ThunderKit/PathReference menu item.

Use PathReferences to identify resuable common paths. A PathReference can be referenced in StagingPaths by using arrow brackets.

For example

`<ManifestStagingRoot>`


![](https://i.imgur.com/MtmmrRL.png)

ThunderKit includes a number of PathReferences already.

![](https://i.imgur.com/afj5qZI.png)

## Scripts, Assemblies, and Assembly Definitions
It is recommended to manage code for mod projects inside your ThunderKit project.
However there are a few things you need to do in order to get your project setup.

In order to deploy any code you create, you will need an AssemblyDefinition. 
To create an AssemblyDefinition, right click a folder in your project and navigate to Create/AssemblyDefinition

Any scripts placed in or under a  folder containing an AssemblyDefinition will be included in an assembly with the same name as the AssemblyDefinition.

- For Example, in the image below SomeScript and AnotherScript will both be included in a library file named NewAssembly.dll

![](https://i.imgur.com/XD6Mm6X.png)

### Preparing AssemblyDefinitions for Deployment
To include these Assemblies in your mod project, you will need to add an AssemblyDefinitions component to your Manifest.
1. Create or select your Manifest and click on Add Manifest Datum.

![](https://i.imgur.com/0OL996l.png)

3. Double Click on AssemblyDefinitions to add it.
4. Under the Assembly Definitions component add an element to Definitions, then drag and drop the AssemblyDefinition into the open field.
5. Finally specify the output path(s) for the assemblies under Staging Paths.

### Deploying Assemblies
To deploy assemblies managed by a Manifest you will need a Pipeline with the Stage Assemblies PipelineJob attached.
1. Create a Pipeline from the project window context menu, ThunderKit/Pipeline
2. Drag and drop your Manifest into the open Manifest field on the Pipeline.

![](https://i.imgur.com/7ff9RXm.png)

4. Click on Add Pipeline Job

![](https://i.imgur.com/jtM0Hx2.png)

5. Find and Double Click on Stage Assemblies

Now the Assemblies referenced in your Manifest will be deployed by the Pipeline to the paths specified in the Manifest's AssemblyDefinition's Staging Paths array.

## Preparing Assets and AssetBundles for Deployment
To deploy assets with your mod you may need to package them into AssetBundles.
ThunderKit provides the AssetBundleDefs ManifestDatum for Manifests and the StageAssetBundles PipelineJob for Pipelines.

1. Find and add an AssetBundleDefs to your Manifest

![](https://i.imgur.com/Bh6rE2e.png)

3. Add an element to the AssetBundles array
4. Name your AssetBundle, make sure this will be a unique name, unity can only load 1 bundle with a given name at a time.
5. Add assets to the Assets array of the bundle.
	* You can drag and drop assets from your Project window directly onto the Asset field to add them to the array.
	* You also use ThunderKit UnityPackage definitions to collect assets for the AssetBundle's Asset array.
6. Finally specify the output path(s) for the AssetBundles under Staging Paths.

### Deploying AssetBundles
The PipelineJob StageAssetBundles can be used to deploy AssetBundles defined by AssetBundleDefs.
The StageAssetBundles PipelineJob has a Bundle Artifact Path field, this defaults to `<AssetBundleStaging>`

This is a pre-staging are for AssetBundles to prevent unnecessarily rebuilding asset bundles, minimizing your build times.  This can be left on the default value.

The Simulate field will execute an analysis of the AssetBundles and report in the Console what will be included in each bundle, this is a minimal solution and in the future a UI that reports this information will be made available.

1. If you don't have a Pipeline, create a Pipeline from the project window context menu, ThunderKit/Pipeline
2. If a Manifest isn't assigned to your Pipeline, Drag and drop your Manifest into the open Manifest field on the Pipeline.
3. Find and add the StageAssetBundles PipelineJob to your Pipeline

![](https://i.imgur.com/qgzr9g7.png)

4. Your Pipeline will now stage AssetBundles during its next run, it will build out the AssetBundles to `<AssetBundleStaging>` and then copy them to each Staging Path specified in the AssetBundleDefs StagingPaths array.

## Redistributable  Libraries

If you need to create asset libraries that other developers need to depend on, use [UnityPackages](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Data/UnityPackage.cs) to simplify that process.
UnityPackages is ThunderKit tooling for [Unity Manual - Asset Packages](https://docs.unity3d.com/2018.4/Documentation/Manual/AssetPackages.html)

### Using UnityPackages 
UnityPackages can be used to setup asset lists that can be built out to a Unity Asset Package.  A Unity AssetPackage allows assets to be easily imported into other Unity projects.

Unity AssetPackages have an issue where assets in them will not work in other projects if the Source code is not included with them.  ThunderKit provides the ability to repair the mapping on these assets to allow you to distribute them with compiled assemblies instead.

To use UnityPackages to create redistributable AssetPackages follow these steps.
1. Find and Add a UnityPackages ManifestDatum to your Manifest

![](https://i.imgur.com/nKWrZKa.png)

3. Create a UnityPackage using the project window context menu, ThunderKit/UnityPackage
4. Add assets to the Asset Files array by adding assets individually or by dragging and dropping assets.
    - Folders can be added to the Asset Files array, however they will only include assets if the Export Package Options includes the Recurse flag
5. Find and Add a StageUnityPackages PipelineJob to your Pipeline
 
![](https://i.imgur.com/CC0zhc4.png)
 
6. Finally, make sure you've assigned paths to the UnityPackages StagingPaths on your Manifest
 