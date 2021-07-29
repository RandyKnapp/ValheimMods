[Delete](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Pipelines/Jobs/Delete.cs) a file or directory

## Fields
* **Recursive**
  - When enabled will copy the entire contents of a specified directory including all subdirectories and files to be content of a Destination directory
  - When using Recursive the Source and Destination are expected to be directory and will error if a file is set as the value
* **Path**
  - Path to file or directory to be deleted
  - Supports PathReferences

* **Fatal Failure**
  - When enabled will cause the job to terminate pipeline execution if the attempt to delete the file or directory fails.

## Inherited Fields
* **Per Manifest**
  - When enabled this job will execute once for each Manifest associated with the Pipeline

* **Excluded Manifests**
  - When Per Manifest is toggled on and you need the pipeline to not execute this job for certain Manifests, add them to this field

## Remarks

PathReferences are resources which can define dynamic paths, you can use them in fields that support PathReferences by invoking them with arrow brackets.

For example if we wanted to delete a mod loader we previously placed in StagingRoot in a pipeline, we can specify the following in this element's Path field

`<StagingRoot>/ModLoader`