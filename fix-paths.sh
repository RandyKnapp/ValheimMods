#!/bin/bash

cd "$(dirname "$0")"

sed -r \
	-e 's!\.\..+Valheim\\(valheim_Data\\Managed|unstripped_corlib)\\(UnityEngine\.(UI|UIModule)\.dll)!..\\valheim_Data\\Managed\\\2!g' \
	-e 's!\.\..+Valheim\\(valheim_Data\\Managed|unstripped_corlib)!..\\valheim_Data\\2019.4.20f1\\Editor\\Data\\Managed\\UnityEngine!g' \
	-e 's!\.\..+Valheim\\(BepInEx)!..\\valheim_Data\\\1!g' \
	-e 's|\.\..+\\(publicized_assemblies)|..\\valheim_Data\\Managed\\\1|g' \
	-e 's|C:\\Program Files \(x86\)\\Steam\\steamapps\\common\\Valheim\\BepInEx|$(ProjectDir)\\..\\valheim_Data\\BepInEx-out|g' \
	-e 's|\$\(TargetDir\)\\?\$\(TargetFileName\)|\$(TargetPath)|g' \
	-i `find . -name \*.csproj`
