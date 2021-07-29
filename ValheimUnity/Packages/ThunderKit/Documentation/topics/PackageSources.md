Implementing a PackageSource is designed to take care of all the management of the Unity systems. Therefore an implementation of a PackageSource requires that you write 2 pieces of code.

First you will need to acquire the list of available packages to download from the mod distribution system in question. Using this information you will populate the PackageSource data by calling and providing the requested data to the AddPackageGroup method in the PackageSource base class.

Next you will need to implement the OnInstallPackageFiles method.  In this method you will need to download the data from the mod distribution system and then write out the content in the layout required by your project conventions to the packageDirectory parametery provided in the method arguments.

If you would like to look at some examples, look at the [ThunderstoreSource](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Integrations/Thunderstore/ThunderstoreSource.cs) and the [LocalThunderstoreSource](assetlink://Packages/com.passivepicasso.thunderkit/Editor/Integrations/Thunderstore/LocalThunderstoreSource.cs) files.