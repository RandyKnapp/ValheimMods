Manifests are where you will store all the information about your projects for ThunderKit to utilize. 
This includes meta data information for mod distribution systems like Thunderstore. Manifests are composed
of ManifestDatums there are many ManifestDatums already available. Check the ManifestDatums section for a list
of ManifestDatums and their functionality.

All Manifests are prepopulated with a ManifestIdentity. The ManifestIdentity is where information about the 
identity of a mod and what dependencies it has are stored.  You can drag and drop any Manifest into the 
ManifestIdentity's Dependencies field, or access it from the Unity asset finder by clicking the small circle.

ManifestDatums all have an array named Staging Paths.  This array of strings informs Pipelines where the information
is expected to be written out to. As previously mentioned, the StagingPaths array can utilize PathReferences by
invoking one with the arrow bracket operators <>.

If you would like to check out an example, inspect the [Default-BepInEx](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Templates/BepInEx/Manifests/Default-BepInEx.asset) manifest in the ThunderKit Package.