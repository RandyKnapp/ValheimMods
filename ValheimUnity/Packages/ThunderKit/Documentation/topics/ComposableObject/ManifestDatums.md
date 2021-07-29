ManifestDatums are where collections of information can be stored.
ThunderKit comes with a few ManifestDatums to cover common use cases, however if you find that they do not cover your case you should create a ManifestDatum to 
collect the information you need.

ManifestDatums and PipelineJobs are paired together in ThunderKit.  When you create a new ManifestDatum you will need to create a PipelineJob to consume it.

Some pairs that already exist are;

1. ManifestDatum [AssemblyDefinitions](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Manifests/Datum/AssemblyDefinitions.cs) and PipelineJob [StageAssemblies](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Pipelines/Jobs/StageAssemblies.cs)
2. ManifestDatum [AssetBundleDefinitions](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Manifests/Datum/AssetBundleDefinitions.cs) and PipelineJob [StageAssetBundles](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Pipelines/Jobs/StageAssetBundles.cs)
3. ManifestDatum [UnityPackages](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Manifests/Datum/UnityPackages.cs) and PipelineJob [StageUnityPackages](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Pipelines/Jobs/StageUnityPackages.cs)

Examine these pairs of types to gain a better understanding of how to build your own customized set of data.

See each ManifestDatums page for more information about them.