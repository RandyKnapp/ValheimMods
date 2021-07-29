[Copy](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Pipelines/Jobs/Copy.cs) copy file(s) with configuration options

## Fields
* **Recursive**
  - When enabled will copy the entire contents of a specified directory including all subdirectories and files to be content of a Destination directory
  - When using Recursive the Source and Destination are expected to be directories and will error if a file is set as the value
* **Source Required**
  - Enable this field when the Source is required and an error should occur if not found
* **Source**
  - Name of File or directory to copy
  - Supports PathReferences
* **Destination**
  - Name of File or directory to copy to
  - Supports PathReferences

## Inherited Fields
* **Per Manifest**
  - When enabled this job will execute once for each Manifest associated with the Pipeline
* **Excluded Manifests**
  - When Per Manifest is toggled on and you need the pipeline to not execute this job for certain Manifests, add them to this field

## Remarks

PathReferences are resources which can define dynamic paths, you can use them in fields that support PathReferences by invoking them with arrow brackets.

For example if you use [ManifestPluginStaging](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Templates/PathReferences/ManifestPluginStaging.asset) in StagingPaths in your Manifest's ManifestDatums
You could then use [ManifestPluginStaging](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Templates/PathReferences/ManifestPluginStaging.asset) in Copy with Per Manifest toggled to copy those files to another location

This way you can deploy assets from multiple Manifests in your project simultaneously.

However, if the Per Manifest option is not toggled, an error will occur when using those PathReferences as they utilize information from Manifests to complete their task

The [BepInEx Launch Pipeline](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Templates/BepInEx/Pipelines/Launch.asset) uses Copy for its deployment steps.

First it will use a Copy with Per Manifest and Recursive enabled, and Source Required disabled to copy plugins, patchers and monomods to the bepinex output directory.
