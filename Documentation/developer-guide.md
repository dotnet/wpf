# Developer Guide

The following document describes the setup and workflow that is recommended for working on the WPF project. It assumes that you have read the [contributing guide](contributing.md).

The [Issue Guide](issue-guide.md) describes our approach to using GitHub issues.

## Machine Setup

Follow the [Building CoreFX on Windows](https://github.com/dotnet/corefx/blob/master/Documentation/building/windows-instructions.md) instructions.

WPF requires the following workloads and  components be selected when installing Visual Studio:

* Required Workloads:
  * .NET Desktop Development
  * Desktop development with C++
* Required Individual Components:
  * C++/CLI support
  * Windows 10 SDK

## Workflow

We use the following workflow for building and testing features and fixes.

You first need to [Fork](https://github.com/dotnet/corefx/wiki/Checking-out-the-code-repository#fork-the-repository) and [Clone](https://github.com/dotnet/corefx/wiki/Checking-out-the-code-repository#clone-the-repository) this WPF repository. This is a one-time task.

### Testing Locally built WPF assemblies (excluding PresentationBuildTasks)
This section of guide is intended to discuss the different approaches for ad-hoc testing of WPF assemblies,
and not automated testing. There are a few different ways this can be done, and for the most part,
it can be a matter of personal preference on which workflow you choose.

#### Copying binaries to publish location of a self-contained application
The simplest approach is to publish your sample app using `dotnet publish -r <rid> --self-contained`.
You can add the `<SelfContained>true</SelfContained>` and `<RuntimeIdentifer>rid</RuntimeIdentifier>`
properties to your .csproj or .vbproj file and then you can simply execute `dotnet publish`.
We recommend always supplying a runtime identifier, as many of the WPF assemblies are architecture dependent.
The values you can choose here are `win-x86` or `win-x64`.

Then to copy the WPF assemblies to this published location, simply run the copy-wpf.ps1 script
located in the `eng` folder of the repo and point it to the location of your test application:
> eng\copy-wpf.ps1 -destination "c:\mysampleproj"

#### Copying binaries to local dotnet installation

If you want/need to test an existing application that targets the shared installation, 
it is safest to setup a test host, rather than trying to copy assemblies over the shared installation.
The arcade infrastructure creates a local dotnet installation in the `.dotnet` folder contained at the root
of the repository when you do a full build using the `build.cmd` or `build.sh` script.
You can run the copy-wpf.ps1 script again, except you can leave out the destination and be sure to pass in the 
the `-testhost` parameter:
> eng\copy-wpf.ps1 -testhost  
```cmd eng\copy-wpf.ps1 -testhost  ```

You need to set environment variables so that your testhost installation is used when launching the application.
Once these are set, you should be able to launch the executable from the command line and then your assemblies
will be used.

- DOTNET_ROOT=<path_to_wpf_repo>\\.dotnet
- DOTNET_MULTILEVEL_LOOKUP=0

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

*Note: you should build locally with the `-pack` param to ensure the binaries are put in the correct location.*
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
    <Reference Include="$(WpfRepoRoot)\artifacts\packaging\$(WpfConfig)\Microsoft.DotNet.Wpf.GitHub\ref\netcoreapp3.0\*.dll" Private="false" />
    <ReferenceCopyLocalPaths Include="$(WpfRepoRoot)\artifacts\packaging\$(WpfConfig)\Microsoft.DotNet.Wpf.GitHub\lib\netcoreapp3.0\*.dll" />
    <ReferenceCopyLocalPaths Include="$(WpfRepoRoot)\artifacts\packaging\$(WpfConfig)\Microsoft.DotNet.Wpf.GitHub\lib\$(RuntimeIdentifier)\*.dll" />
  </ItemGroup>
```

### Testing specific versions of the Microsoft.WindowsDesktop.App runtime
At times, it is necessary to install and test specific versions of the runtime. This can be helpful if you are trying to root cause when an issue started occuring.

For testing different versions of the runtime, you can install a specific version of the runtimes via the dotnet install script: https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script
**Note**: These install the versions to your %user% directory, so you can use the DOTNET_ROOT environment variables to ensure these get used as described above. Otherwise, you can point them to install in %programfiles% and specify which version of the runtime should be picked up.

Below is an example powershell script of how you can use the `dotnet-install.ps1` script:

```
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
If you can build directly from source, you can add this to your project file to pick up the version of the shared runtime you want to test:
```xml
 <PropertyGroup>
    <MicrosoftWindowsDesktopAppVersion>3.0.0-preview5-27619-18</MicrosoftWindowsDesktopAppVersion>
 <PropertyGroup>
 <FrameworkReference Update="Microsoft.WindowsDesktop.App">
    <TargetingPackVersion>$(MicrosoftWindowsDesktopAppVersion)</TargetingPackVersion>
 </FrameworkReference>
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
