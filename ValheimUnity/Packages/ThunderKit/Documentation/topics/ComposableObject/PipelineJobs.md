PipelineJobs are what conduct Unity Build actions such as staging assets, assetbundles and other files, copying and moving files for deployment, as well as creating and modifying zip files.

ThunderKit comes with a collection of PipelineJobs that cover each of these use cases and like ManifestDatums and PathComponents you can create new PipelineJobs using the help of ComposableObject Inspector.

PipelineJobs and ManifestDatums are paired together in ThunderKit.  When you create a new PipelineJob you may need to create a ManifestDatum to hold information for it.

Some pairs that already exist are;

1. ManifestDatum [AssemblyDefinitions](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Manifests/Datum/AssemblyDefinitions.cs) and PipelineJob [StageAssemblies](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Pipelines/Jobs/StageAssemblies.cs) 
2. ManifestDatum [AssetBundleDefinitions](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Manifests/Datum/AssetBundleDefinitions.cs) and PipelineJob [StageAssetBundles](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Pipelines/Jobs/StageAssetBundles.cs) 
3. ManifestDatum [UnityPackages](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Manifests/Datum/UnityPackages.cs) and PipelineJob [StageUnityPackages](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Pipelines/Jobs/StageUnityPackages.cs) 

Examine these pairs of types to gain a better understanding of how to build your own customized set of data.

See each ManifestDatums page for more information about them.