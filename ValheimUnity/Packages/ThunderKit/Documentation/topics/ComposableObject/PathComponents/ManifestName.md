[ManifestName](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Paths/Components/ManifestName.cs) can only be executed in the context of a Pipeline with an assigned Manifest.

## Context

* Can only be used in PipelineJobs that execute on Manifests or in ManifestDatum StagingPaths

## Remarks

When executed within the context of a Pipeline with an assigned Manifest, the ManifestVersion component will retrieve and return the value of the Manifest's ManifestIdentity.Name
