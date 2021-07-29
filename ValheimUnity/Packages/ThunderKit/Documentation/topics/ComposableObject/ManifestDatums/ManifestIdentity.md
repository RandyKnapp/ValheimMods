The [ManifestIdentity](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Manifests/Datum/ManifestIdentity.cs) stores unique identifying information used by ThunderKit to construct dependency information for package stores and mod loaders.

### Fields
* **Author**
  - The name of the developer or team responsible for developing and releasing this package.
* **Name**
  - The name of this package, this is the dependency name and can only contain valid path characters and except for spaces
* **Version**
  - The current in development version of this pacakge.
* **Description**
  - A short description of the package
* **Icon**
  - Image for the package, used by some Package Sources like Thunderstore
* **Dependencies**
  - A list of Manifests for packages this Manifest depends on

## Inherited Fields

* **Staging Paths**
  - A list of destinations to deploy files to
  - Supports PathReferences
  
## PipelineJobs

* [StageDependencies](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Pipelines/Jobs/StageDependencies.cs)
  - Uses the ManifestIdentity.Dependencies array to deploy dependencies loaded by the ThunderKit Package Manager

* [StageThunderstoreManifest](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Pipelines/Jobs/StageThunderstoreManifest.cs) 
  - Uses ManifestIdentity information to construct a manifest json file for Thunderstore

## Remarks

The ManifestIdentity contains the dependencies for each mod as well as some common identifying information for package distribution sites like Thunderstore and Mod.io

The ManifestIdentity is required on every Manifest in order to conduct most build operations.

This is because PipelineJobs will use the dependency hierarchy to discover all assets that need to be built or copied to Staging Paths.

To find external dependencies use the [ThunderKit Package Manager](menulink://Tools/ThunderKit/Packages)