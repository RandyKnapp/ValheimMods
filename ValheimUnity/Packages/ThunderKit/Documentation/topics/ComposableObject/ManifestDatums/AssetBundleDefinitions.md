[AssetBundleDefinitions](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Manifests/Datum/AssetBundleDefinitions.cs) allow you to specify what assets you would like to include in your project output in the form of Unity AssetBundle(s)

## Fields

* **Asset Bundles**
  - A collection of definitions for AssetBundles
  - Each bundle can provide a name and a collection of assets

## Inherited Fields

* **Staging Paths**
  - A list of destinations to deploy files to
  - Supports PathReferences

## PipelineJobs

* [StageAssetBundles](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Pipelines/Jobs/StageAssetBundles.cs) 
  - Builds and deploys AssetBundles defined by AssetBundleDefinitions

## Remarks

The PipelineJob, StageAssetBundles, will use the AssetBundleDefinitions ManifestDatum and the Pipeline Manifest's dependency hierarchy to determine how to build out the AssetBundles.

This means that only AssetBundles defined within the Pipelines Manifest hiearchy will be included in StageAssetBundles processing.

Assets assigned to an AssetBundle are gaurenteed to be included in the asset bundle, even if this means duplicating information.

You may need to consider how duplication of your assets will affect how your mods will work.

When StageAssetBundles executes, it will process all AssetBundles simultanously, and will resolve dependencies both within and across Manifests.

## More Information

[Unity Manual - Asset Bundles](https://docs.unity3d.com/2018.4/Documentation/Manual/AssetBundlesIntro.html)