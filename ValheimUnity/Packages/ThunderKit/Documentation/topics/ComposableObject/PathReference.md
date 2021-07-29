Path References are one of the 3 main systems you will be using in ThunderKit.  It sits between 
Manifests and Pipelines, providing the ability for Pipelines to be able to deploy your custom content.
Due to this we should first understand how PathReferences work and how can be used.

PathReferences provide the ability for you to create a custom rule set for how paths are identified.  
You setup these rules by adding Path Components.  A PathReference will execute each of its components
in order and will have access to 2 sets of information depending on when they are executed.

In the first situation a PathReference has access only to information available on the Pipeline and 
static information available in the project.  This situation is when a PathReference is accessed from
a Pipeline during a PipelineJob that isn't setup to execute against a Manifest.

In the second a PathReference has access to the same information, plus it can access information about the 
Manifest currently being processed by the Pipeline. We will go over this in more detail in the Pipeline section.

In ThunderKit Pipelines and Manifests any place you can enter a path to a file or a folder, you can use 
a PathReference by invoking it in the field to ensure common paths are always used correctly.  You can 
invokea path reference by naming it in arrow brackets.

For example, you can use the [ManifestPluginStaging](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Templates/PathReferences/ManifestPluginStaging.asset)
PathReference in Staging Paths by calling it with arrow brackets.

If you wanted to put all your Manifest's AssetBundles into a subfolder named AssetBundles in your mod output
you add the following to your Manifest's AssetBundleDefinition's Staging Paths.

`<ManifestName>/AssetBundles`

When the StageAssetBundles PipelineJob executes against your Manifest, it will output the AssetBundles into the folder 
`ProjectRoot/ManifestName/AssetBundles`

Where ProjectRoot is the name of the folder your project is in, and ManifestName is the value you entered for your Manifest's ManifestIdentity's Name field.

ThunderKit comes with a number of already defined [PathReferences](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Templates/PathReferences). Please note that 
some adjustments may be needed for different games and mod loaders.

Finally it is important to note that fields which can use PathReferences do not automatically resolve.  
PipelineJobs must be built to resolve PathReferences using `PathReference.