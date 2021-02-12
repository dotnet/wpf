# Developer Guide

The following document describes the setup and workflow that is recommended for working on the WPF project. It assumes that you have read the [contributing guide](contributing.md).

The [Issue Guide](issue-guide.md) describes our approach to using GitHub issues.

## Machine Setup

Follow the [Building CoreFX on Windows](https://github.com/dotnet/corefx/blob/master/Documentation/building/windows-instructions.md) instructions.

WPF requires the following workloads and  components be selected when installing Visual Studio:

* Required Workloads: [wpf.vsconfig](wpf.vsconfig)
    *  Also see [Import or export installation configurations](https://docs.microsoft.com/en-us/visualstudio/install/import-export-installation-configurations?view=vs-2019)

## Workflow

We use the following workflow for building and testing features and fixes.

You first need to [Fork](https://github.com/dotnet/corefx/wiki/Checking-out-the-code-repository#fork-the-repository) and [Clone](https://github.com/dotnet/corefx/wiki/Checking-out-the-code-repository#clone-the-repository) this WPF repository. This is a one-time task.


### Running DRTs locally ###
In order to run the set of DRTs on your local machine, pass the `-test` parameter to the `build.cmd` script. At the end of the run, you should see something like this:

```
  A total of 1 test Infos were processed, with the following results.
   Passed: 1
   Failed (need to analyze): 0
   Failed (with BugIDs): 0
   Ignore: 0

```
If there were any failures, you can cd into $(RepoRoot)\artifacts\test\$(Configuration)\$(Platform)\Test and run the tests manually with the `/debugtests` flag using the `RunDrts.cmd` script. Note that you do not run the `RunDrtsDebug` script, as this will debug the test infrastructure, `QualityVault`. When you pass the `/debugtests` flag, a cmd window will open where you can open the test executable in Visual Studio and debug it. When the cmd pops up, you will see instructions for debugging using a few different commands, however these commands will enable you to debug the `Simple Test Invocation` executable, `sti.exe`, which simply launches the test executable you are most likely interested in debugging. Using `DrtXaml.exe` as an example, this is how you can debug the test executable. Any MSBuild style properties should be replaced with actual values:

1. `$(RepoRoot)\artifacts\test\$(Configuration)\$(Platform)\Test\RunDrts.cmd /name=DrtXaml /debugtests`
2. Enter following command into the cmd window that pops up:
`"%ProgramFiles%\Microsoft Visual Studio\2019\Preview\Common7\IDE\devenv.exe" DrtXaml.exe`
3. Once Visual Studio is open, go to `Debug-> DrtXaml Properties` and do the following:
    - Manually change the `Debugger Type` from `Auto` to `Mixed (CoreCLR)`.
    - Change the `Environment` from `Default` to a custom one that properly defines the `DOTNET_ROOT` variable so that the host is able to locate the install of `Microsoft.NETCore.App`.
      - x86 (Default): Name: `DOTNET_ROOT(x86)` Value: `$(RepoRoot).dotnet\x86`
      - x64 (/p:Platform=x64): Name: `DOTNET_ROOT` Value: `$(RepoRoot).dotnet` 
4. From there you can F5 and the test will execute.

*Note: To run a specific test, you can pass the name of the test like this: `/name=DrtXaml`. The names of these tests are contained in DrtList.xml.*

*NOTE: This requires being run from an admin window at the moment. Removing this restriction is tracked by https://github.com/dotnet/wpf/issues/816.*

### Testing Locally built WPF assemblies (excluding PresentationBuildTasks)
This section of guide is intended to discuss the different approaches for ad-hoc testing of WPF assemblies,
and not automated testing. For automated testing, see the [Running DRTs locally](#Running-DRTs-locally) section above. There are a few different ways this can be done, and for the most part, it depends on what you are trying to accomplish. This section tries to lay out which scenarios require which workflow.

*NOTE: You should build locally with the `-pack` param to ensure the binaries are put in the correct location for manual testing.*

#### Copying binaries to publish location of a self-contained application
The simplest approach is to publish your sample app using `dotnet publish -r <rid> --self-contained`.
You can add the `<SelfContained>true</SelfContained>` and `<RuntimeIdentifer>rid</RuntimeIdentifier>`
properties to your .csproj or .vbproj file and then you can simply execute `dotnet publish`.
We recommend always supplying a runtime identifier, as many of the WPF assemblies are architecture dependent.
The values you can choose here are `win-x86` or `win-x64`.

Then to copy the WPF assemblies to this published location, simply run the copy-wpf.ps1 script
located in the `eng` folder of the repo and point it to the location of your test application:
> eng\copy-wpf.ps1 -destination "c:\mysampleproj"

#### Copying binaries to test host installation of dotnet

If you want/need to test an existing application that targets the shared installation, it is safest to setup a test host, rather than trying to copy assemblies over the shared installation. A test host is a complete install of dotnet (host and runtimes) used for testing applications and can be setup by using the [dotnet install script](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script). This method is also effective for internal contributors who are working on porting our current test corpus from .NET Framework to .NET Core and wants to run the tests against locally built assemblies. Note that there is nothing fundamentally different between a testhost installation of dotnet and the one installed in `$env:ProgramFiles`. However the dotnet host dll won't be able to find the testhost install if the appropriate environment variables aren't set. Note that these environment variables are set for you by copy-wpf.ps1 

You can run the copy-wpf.ps1 script again and be sure to pass in the `-testhost` parameter:
> eng\copy-wpf.ps1 -testhost -destination "c:\testhost\x86"

If your testhost directory has multiple versions of the `Microsoft.WindowsDesktop.App` shared runtime in it, you can use the `-version` parameter to specify which one you want:

> eng\copy-wpf.ps1 -testhost -destination "c:\testhost\x86" -version "3.0.0-preview6-27728-04"  

If there are multiple versions, you will see a warning and the last installed runtime will be selected. You can backup the folder by creating a copy of it, and the script will ensure that this folder isn't touched as long as the word "Copy" is in the path. This was chosen because the default for Windows Explorer is to append "- Copy" to the folder. This allows you to easily backup folders containing the runtime assemblies knowing you can restore them to their original state if needed. 

If you are installing to a special test host location, you will see output from the script that confirms the appropriate environment variables are set:

```
** Setting env:DOTNET_ROOT(x86) to c:\testhost\x86 **
** Setting env:DOTNET_MULTILEVEL_LOOKUP to 0 **
```

**How to find location of the exe to test?**
If you are testing an application and don't know where the executable is located, the easiest thing to do
is use Process Explorer (from SysInternals) or attach to the process with a debugger like Visual Studio.

#### Testing API changes 
The above instructions imply that you are testing assemblies that don't have any changes to the
public API surface that the test application needs to use. If you need to test some API changes
you are making, the C# team has done some great work to make this relatively straightforward.
When the C# compiler detects a collision with assembly references, the assembly with the
higher version number is chosen. Assuming our locally built binaries are newer than what is
installed, we can then simply reference those local binaries directly from the project file, like this:

```xml
  <PropertyGroup>
     <!-- Change this value based on where your local repo is located -->
     <WpfRepoRoot>d:\dev\src\dotnet\wpf</WpfRepoRoot>
     <!-- Change based on which assemblies you build (Release/Debug) -->
     <WpfConfig>Debug</WpfConfig>
     <!-- Publishing a self-contained app ensures our binaries are used. -->
     <SelfContained>true</SelfContained>
    <!-- The runtime identifier needs to match the architecture you built WPF assemblies for. -->
    <RuntimeIdentifier>win-x86</RuntimeIdentifier>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="$(WpfRepoRoot)\artifacts\packaging\$(WpfConfig)\Microsoft.DotNet.Wpf.GitHub\lib\net6.0\*.dll" />
    <ReferenceCopyLocalPaths Include="$(WpfRepoRoot)\artifacts\packaging\$(WpfConfig)\Microsoft.DotNet.Wpf.GitHub\lib\$(RuntimeIdentifier)\*.dll" />
  </ItemGroup>
```

### Testing specific versions of the Microsoft.WindowsDesktop.App runtime
At times, it is necessary to install and test specific versions of the runtime. This can be helpful if you are trying to root cause when an issue started occuring, or need to compare functionality between two different versions.

For testing different versions of the runtime, you can install a specific version of the runtimes via the [dotnet install script](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script). Below is an example powershell script of how you can use the `dotnet-install.ps1` script that will install both 32-bit and 64-bit versions of the `Microsoft.WindowsDesktop.App` runtime into the specified folder:

```ps1
$dotnet_install = "$env:TEMP\dotnet-install.ps1"
$x64InstallDir  = "$env:ProgramFiles\dotnet"
$x86InstallDir  = "${env:ProgramFiles(x86)}\dotnet"

Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile $dotnet_install

.$dotnet_install -Channel master -Version 3.0.0-preview5-27619-18 -Runtime windowsdesktop -Architecture x64 -InstallDir $x64InstallDir
.$dotnet_install -Channel master -Version 3.0.0-preview5-27619-18 -Runtime windowsdesktop -Architecture x86 -InstallDir $x86InstallDir
```

This would install version `3.0.0-preview5-27619-18` of the `Microsoft.WindowsDesktop.App` shared runtime. You can pass `"Latest"` to get the latest version of the runtime.  You can also use this script to install the runtimes as well as the SDK. If you know a particular SDK version and are curious to know what `Microsoft.WindowsDesktop.App` version is associated with it, there is a file called `Microsoft.NETCoreSdk.BundledVersions.props` contained inside the SDK folder. Inside that file, you will find an entry that looks like this:

```xml
    <KnownFrameworkReference Include="Microsoft.WindowsDesktop.App"
                              TargetFramework="netcoreapp3.0"
                              RuntimeFrameworkName="Microsoft.WindowsDesktop.App"
                              DefaultRuntimeFrameworkVersion="3.0.0-preview4-27613-28"
                              LatestRuntimeFrameworkVersion="3.0.0-preview4-27613-28"
                              TargetingPackName="Microsoft.WindowsDesktop.App.Ref"
                              TargetingPackVersion="3.0.0-preview4-27615-11"
                              RuntimePackNamePatterns="runtime.**RID**.Microsoft.WindowsDesktop.App"
                              RuntimePackRuntimeIdentifiers="win-x64;win-x86"
                              />
```
In this example, the version of `Microsoft.WindowsDesktop.App` associated with this SDK is `3.0.0-preview4-27613-28`.

**Note**: The ability to install the WindowsDesktop runtime via the dotnet install script is being tracked by: https://github.com/dotnet/cli/issues/11115 

#### Specifying which version of the runtime to use
If you can build directly from source, and want to test your application against a certain version of the `Microsoft.WindowsDesktop.App` shared runtime, you can add this to your project file to pick up the version of the shared runtime you want to test:
```xml
 <PropertyGroup>
    <MicrosoftWindowsDesktopAppVersion>3.0.0-preview5-27619-18</MicrosoftWindowsDesktopAppVersion>
 </PropertyGroup>
 <ItemGroup>
   <FrameworkReference Update="Microsoft.WindowsDesktop.App">
      <TargetingPackVersion>$(MicrosoftWindowsDesktopAppVersion)</TargetingPackVersion>
   </FrameworkReference>
 </ItemGroup>
```

If you don't have the ability to build from source, you can update the *.runtimeconfig.json file located next to the executable to pick up your version:
```json
{
  "runtimeOptions": {
    "tfm": "netcoreapp3.0",
    "framework": {
      "name": "Microsoft.WindowsDesktop.App",
      "version": "3.0.0-preview5-27619-18"
    }
  }
}
```

#### Finding a specific version of Microsoft.WindowsDesktop.App that interests you
Follow the steps defined [here](https://github.com/dotnet/arcade/blob/master/Documentation/SeePackagesLatestVersion.md) to get setup for [swagger API](https://maestro-prod.westus2.cloudapp.azure.com/swagger/ui/index.html). Note that you need to authorize each time you login, so keep note of your token or you'll have to generate a new one. Assuming you have a commit (and therefore an Azure DevOps build id) that you are interested in, you can enter the build id into your query.

### Testing PresentationBuildTasks
-- add more content here --

## More Information

* [git commands and workflow](https://github.com/dotnet/corefx/wiki/git-reference)
* [Coding guidelines](https://github.com/dotnet/corefx/tree/master/Documentation#coding-guidelines)
* [up-for-grabs WPF issues](https://github.com/dotnet/wpf/issues?q=is%3Aopen+is%3Aissue+label%3Aup-for-grabs)
* [easy WPF issues](https://github.com/dotnet/wpf/issues?utf8=%E2%9C%93&q=is%3Aopen+is%3Aissue+label%3Aeasy)
* [Code generation in dotnet/wpf](codegen.md)
* [Testing in Helix](testing-in-helix.md)
