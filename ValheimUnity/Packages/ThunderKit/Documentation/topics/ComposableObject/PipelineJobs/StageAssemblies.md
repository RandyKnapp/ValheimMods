[StageAssemblies](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Pipelines/Jobs/StageAssemblies.cs) copies Assemblies built by unity out to StagingPaths

## Fields
* **Stage Debug Database**
  - Enable this to copy debugging information to StagingPaths 
  - Additional steps are required to get debug information displaying line numbers
* **Prefer Player Builds**
  - Player Builds are release builds of assemblies which include optimization for release and removes debugging information
  - These should generally be used for final release testing and performance testing
  - Updated Player Builds are only available after an AssetBundle build has occurred

## Required ManifestDatums

* [AssemblyDefinitions](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Manifests/Datum/AssemblyDefinitions.cs)

## Remarks

Stage Assemblies is how you deploy Assemblies defined in AssemblyDefinitions ManifestDatums.

StageAssemblies will execute for each Manifest in the Manifest hierarchy.
