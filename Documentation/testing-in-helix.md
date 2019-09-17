# Testing in Helix

I'd recommend seeing the official Helix [readme](https://github.com/dotnet/arcade/blob/master/src/Microsoft.DotNet.Helix/Sdk/Readme.md) if you are interested in some of the general Helix concepts. I'll briefly outline what we are doing that is a bit unique:

1. Helix has built-in support for running xUnit tests. Since we are not using xUnit, we have to manually setup our machines so that they work with QualityVault and STI. During the build, we create a payload directory that contains the infrastructure we need. A single project (in our case `DrtXaml`) is responsible for creating this directory (see instances of the MSBuild property `CreateTestPayload`).
2. After the build is done, we utilize Arcade's `AfterSolutionBuild.targets` extension point to finish creating the rest of the payload if the `-test` parameter is passed to the build. Here we add the just built DRTs and if `-ci` was **not** passed into to the build, run the tests. 
3. Helix allows you to specify a `HelixCorrelationPayload` directory, where this directory gets deployed to the Helix machine, and is made available in your various helix commands with the `HELIX_CORRELATION_PAYLOAD` environment variable. We use the payload directory created described above.
4. Helix and Azure Pipelines can report xUnit logs, so we will be updating QualityVault to produce an xUnit compatible log. We will then need to copy that log to a known location for it to be picked up. This location can be in any subfolder of the `HelixWorkItem` working directory. 

## How we are running tests
1. Currently, the `HelixQueues` that we selected are Windows.10.Amd64.Open;Windows.7.Amd64.Open;Windows.10.Amd64.Client19H1.Open. Essentially, this translates to: "Latest Windows 10", "Windows 7", and "Windows 10 Client 19H1 queue" addition. This enables tests to run on some of the most interesting and important SKUs, without overloading the Helix servers and/or making CI runs take an unnecessarily long time to run.
2. In a similar fashion, we only run test passes for Release builds, mainly to save time and resources running duplicate tests in a near-identical environment.
