if exist "\\RANDY\plugins" (
	xcopy "$(TargetDir)\$(TargetFileName)" "\\RANDY\plugins\$(ProjectName)\" /q /y /i
    xcopy "$(ProjectDir)loottables.json" "\\RANDY\plugins\$(ProjectName)\" /q /y /i
    xcopy "$(SolutionDir)Libs\fastJSON.dll" "\\RANDY\plugins\$(ProjectName)\" /q /y /i
    xcopy "$(SolutionDir)ValheimUnity\AssetBundles\epicloot" "\\RANDY\plugins\$(ProjectName)\" /q /y /i )