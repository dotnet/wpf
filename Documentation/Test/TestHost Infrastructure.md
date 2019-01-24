# TestHost Infrastructure
In order for tests to run against recently built assemblies, a TestHost must be constructed and used during the test run process.
## Building a TestHost
TestHosts are local installs of .NET Core that have been modified to include locally-built assemblies.  
### [TestHost.csproj](https://github.com/dotnet/wpf/tree/master/src/Microsoft.DotNet.Wpf/src/TestHost/TestHost.csproj)
This project is responsible for triggering creation of the TestHost during the build.  This is done during the `Restore` target in order to ensure that 
by the time WPF binaries are being built, the TestHost exists and projects can place their outputs there.
### [install-testhost.ps1](https://github.com/dotnet/wpf/tree/master/eng/install-testhost.ps1)
This script is run from the `CopyDotNetToTestHost` target in [TestHost.csproj](https://github.com/dotnet/wpf/tree/master/test.cmd) 
in order to initiate the creation of the TestHost.  By default, the version of .NET Core installed is dictated by the version called out in [global.json](https://github.com/dotnet/wpf/tree/master/global.json) 
under `tools -> dotnet`. 

You can override this default by passing `/p:UseLatestSdkForTestHost=true` during the build.
Note that CI builds ignore this parameter and always use the .NET Core version from global.json.

TestHosts are not created if there already is a current TestHost that has the same version of .NET Core as the one that is about to be created.  This is a strict 
equality comparison and downgrades are allowed.  In the event that the current TestHost does not match the version specified in the build, it will be deleted and a 
new one created.
### [TestHostBinPlacing.targets](https://github.com/dotnet/wpf/tree/master/src/Microsoft.DotNet.Wpf/TestHostBinPlacing.targets)
This file contains the targets that place build output into the TestHost.  Projects that need to do so just need to set the property `BinPlaceToTestHost` to true
in their project file.  This ensures that the `TestHostBinPlacing` target picks up the file accordingly.  For an example, see [System.Xaml.csproj](https://github.com/dotnet/wpf/tree/master/src/Microsoft.DotNet.Wpf/src/System.Xaml/System.Xaml.csproj)
## Running Against the TestHost
In order for the TestHost to be useful, tests must be able to run against the hosted assemblies.  When running the tests 
(either via [test.cmd](https://github.com/dotnet/wpf/tree/master/test.cmd) or directly calling [build.ps1](https://github.com/dotnet/wpf/tree/master/eng/common/build.ps1) with -test) 
the engineering system orchestrates pointing at the TestHost for this purpose.
### [configure-toolset.ps1](https://github.com/dotnet/wpf/tree/master/eng/configure-toolset.ps1)
This file is an extensibility point provided by [Arcade](https://github.com/dotnet/arcade) that allows us to modify the build prior to it starting. The modifications 
needed to target the TestHost are the following:
* Turn off .NET Core multi-level lookup.  This stops .NET Core from searching all .NET Core installations for the best version match of an SDK.
* Ensure the build is using the .NET Core build engine
* Find the path to the current TestHost and prepend it to the path environment variable for the process.

When these are done, any future invocations of .NET Core in this particular instantiation of the build system will use the TestHost.
## Verification
In order to verify that the TestHost is doing what we expect, a unit test has been added ([TestHostVerifier.cs](https://github.com/dotnet/wpf/tree/master/src/Microsoft.DotNet.Wpf/test/UnitTest/TestHost/TestHostVerifier.cs)).
This test forces assembly loads of all WPF managed binaries and verifies that they are loading from the appropriate paths.
