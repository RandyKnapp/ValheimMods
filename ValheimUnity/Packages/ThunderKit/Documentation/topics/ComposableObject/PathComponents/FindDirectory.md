[Find Directory](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Paths/Components/FindDirectory.cs) will find and return the name of a Directory

## Fields

* **Search Option**
  - Option to search only top directory or all sub directories as well
* **Search Pattern**
  - pattern to search for in file and folder names
* **Path**
  - Root of path to search

## Remarks

The Path field will process PathReferences which will be resolved before being passed to Directory.EnumerateFiles.

This will return the name of the specified directory, not its full path.
 
Find Directory is a light wrapper over [Directory.EnumerateFiles](https://docs.microsoft.com/en-us/dotnet/api/system.io.directory.enumeratefiles?view=netframework-4.6)
