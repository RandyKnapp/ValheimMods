[StageDependencies](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Pipelines/Jobs/StageDependencies.cs) deploys Dependencies found in Manifests that are not in the project Assets folder

## Fields
* **Staging Path**
  - Enable this to copy debugging information to StagingPaths 
  - Supports PathReferences

## Required ManifestDatums

* [ManifestIdentity](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Manifests/Datum/ManifestIdentity.cs)

## Remarks

Stage Assemblies is how you deploy Assemblies defined in AssemblyDefinitions ManifestDatums.

StageAssemblies will execute for each Manifest in the Manifest hierarchy.
