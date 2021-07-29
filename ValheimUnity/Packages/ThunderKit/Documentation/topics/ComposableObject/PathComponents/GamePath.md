[GamePath](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Core/Paths/Components/GamePath.cs) will return the value of GamePath in your [ThunderKitSettings](menulink://Tools/ThunderKit/Settings)

## Remarks

Use this to deploy files directly to the game folder.

This is used by the [BepInEx Launch Pipeline](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Templates/BepInEx/Pipelines/Launch.asset) to deploy the winhttp.dll file to the games directory so that doorstop can intercept the application startup.