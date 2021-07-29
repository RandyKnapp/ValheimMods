The [AssemblyDefinitions](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Manifests/Datum/AssemblyDefinitions.cs) ManifestDatum stores references to Unity AssemblyDefinition objects.

## Fields
* **Definitions**
  - An array of AssemblyDefinition assets

## Inherited Fields

* **Staging Paths**
  - A list of destinations to deploy files to
  - Supports PathReferences

## PipelineJobs

* [StageAssemblies](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Pipelines/Jobs/StageAssemblies.cs) 
  - Copies each assembly referenced in each AssemblyDefinition ManifestDatum to the output paths defined in its Staging Paths.

## Remarks

You can create AssemblyDefinition assets using the project window context menu under Create/AssemblyDefinitions

Use this ManifestDatum and the StageAssemblies PipelineJob to build and deploy code inside your unity project to the specified Staging Paths

for more information about Unity Script compilation and assembly definition files, refer to the Unity Manual.

[Unity Manual - Script compilation and assembly definition files](https://docs.unity3d.com/2018.4/Documentation/Manual/ScriptCompilationAssemblyDefinitionFiles.html)