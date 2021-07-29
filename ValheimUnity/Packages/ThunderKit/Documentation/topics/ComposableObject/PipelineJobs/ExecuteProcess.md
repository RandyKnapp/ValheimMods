[ExecuteProcess](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Pipelines/Jobs/ExecuteProcess.cs) starts a new process

## Fields
* **Working Directory**
  - The Working Directory the process should run under
  - Supports PathReferences

* **Executable**
  - The executable to start
  - Supports PathReferences

* **Arguments**
  - An array of command line arguments to pass to the process being started
  - Supports PathReferences

## Remarks

Execute Process can be used to launch games, external build processes, or any other process necessary for a build pipeline

Use PathReferences to simplify the fields of ExecuteProcess and to provide a centralized set of variables to make it easier to manage multiple build pipelines.

The [BepInEx Launch Pipeline](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Tempaltes/BepInEx/Pipelines/Launch.asset) uses Execute Process to launch the configured game and pass parameters necessary to load BepInEx with winhttp.dll and doorstop
