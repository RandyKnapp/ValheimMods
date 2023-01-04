if exist "G:\Steam\steamapps\common\Valheim dedicated server\BepInEx\plugins" ( 
    xcopy "C:\Users\rknapp\Documents\GitHub\ValheimMods\EpicLoot\bin\Debug\EpicLoot.dll" "G:\Steam\steamapps\common\Valheim dedicated server\BepInEx\plugins\EpicLoot\" /q /y /i
    xcopy "C:\Users\rknapp\Documents\GitHub\ValheimMods\EpicLoot\loottables.json" "G:\Steam\steamapps\common\Valheim dedicated server\BepInEx\plugins\EpicLoot\" /q /y /i
    xcopy "C:\Users\rknapp\Documents\GitHub\ValheimMods\Libs\fastJSON.dll" "G:\Steam\steamapps\common\Valheim dedicated server\BepInEx\plugins\EpicLoot\" /q /y /i
    xcopy "C:\Users\rknapp\Documents\GitHub\ValheimMods\ValheimUnity\AssetBundles\epicloot" "G:\Steam\steamapps\common\Valheim dedicated server\BepInEx\plugins\EpicLoot\" /q /y /i )