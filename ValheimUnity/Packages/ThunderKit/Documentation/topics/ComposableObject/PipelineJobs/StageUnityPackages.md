[StageUnityPackages](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Pipelines/Jobs/StageUnityPackages.cs) builds and deploys Unity Asset Packages

## Required ManifestDatums

* [UnityPackages](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Manifests/Datum/UnityPackages.cs)

## Remarks

UnityPackages are used for creating and deploying Unity [Asset Packages](https://docs.unity3d.com/2018.4/Documentation/Manual/AssetPackages.html)

Asset Packages allow you to share assets for use in Unity Projects with other developers.

Unity's built in system for Asset Packages can be used directly to create Asset Packages, however they require that you redistribute assets against source code and assemblies in the same format.

If you have source code that assets depend on which you would like to deploy Asset Packages for against Assemblies instead.

Use the ThunderKit UnityPackages datum and StageUnityPackages job to build Asset Packages with remapped references to allow the assets to resolve when imported to a project with assemblies instead of source code.