[OutputReference](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Paths/Components/OutputReference.cs) allows you to reference another PathReference using Unity's Asset system.

## Fields
* **Reference**
  - PathReference to call upon with the context of the parent PathReference

## Remarks

These can be used to build more complex PathReferences by combining smaller ones

Use this over a Resolver is you want to ensure that references are maintained through name changes
