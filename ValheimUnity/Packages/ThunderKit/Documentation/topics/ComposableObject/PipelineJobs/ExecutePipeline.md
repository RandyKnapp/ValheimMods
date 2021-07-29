[ExecutePipeline](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Pipelines/Jobs/ExecutePipeline.cs) will invoke an assigned Pipeline

## Fields
* **Execute Pipeline**
  - A Pipeline to invoke
* **Inherit Root Manifest**
  - When toggled on the assigned Pipeline will have its Manifest field replaced with the parent Pipeline's assigned Manifest

## Remarks

`Warning` There is no cycle detection in place, take care to avoid making infinite loops

Use this to invoke additional pipelines from the current pipeline.

Using Execute Pipeline you can organize complex Pipelines into smaller re-usable chunks