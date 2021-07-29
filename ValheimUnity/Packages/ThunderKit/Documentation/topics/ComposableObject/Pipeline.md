
Pipelines are the Build portion of ThunderKit. In combination with PathReferences and Manifests, 
a Pipeline can build Assemblies, AssetBundles, manifest files, and anything else a project may need.

Pipelines are composed of a Manifest and a set of PipelineJobs. PipelineJobs will either execute once 
during a Pipeline run or once per Manifest. This can be indicated by the PipelineJob&apos;s name, 
functionality or explicit options on the PipelineJob.  See the documentation for a PipelineJob if
you&apos;re unsure how it executes.

When building a pipeline, you&apos;re goal is to setup your build process so that you don't have to
manually conduct any steps.  You can build a custom pipeline by looking at the steps you take to build
and deploy your mods for testing.  ThunderKit handles certain aspects of discovery for you.  

For Example, You don't need to determine where your assemblies are built to from Unity, this is
handled by the Stage Assemblies PipelineJob.

This means all you need to do is determine where the Assemblies should be placed and indicate this in
an Manifest AssemblyDefinitions component using its StagingPath array.

Each PipelineJob will use a ManifestDatum's StagingPaths to deploy its content, if you are 
constructing a new PipelineJob, you should attempt to do this, as well as utilize PathReference's resolver
on these output paths.

Some PipelineJobs include the ability to Exclude Manifests.  This can be used to exclude Manifests which
have special deployment requirements,  such as a Mod Loader.  In these situations you may need to have 
explicit control over where and how the content of that Manifest is deployed.  

The [Default-BepInEx](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Templates/BepInEx/Manifests/Default-BepInEx.asset)
template uses this to deploy BepInEx separately so that mods can properly be 
installed into its plugins, patchers and monomod folders.