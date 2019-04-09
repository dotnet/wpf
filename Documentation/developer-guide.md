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
> eng\copy-wpf.ps1 -destination "proj location"

#### Copying binaries to shared installation
*Note: We recommend backing up the original files when doing this.*

If you want/need to test an existing application that targets the shared installation, you can copy
the assemblies directly over the existing Microsoft.WindowsDesktop.App ones.
You can run the copy-wpf.ps1 script again, except this time the destination refers to the version 
of the shared framework to copy over, and the `-shared` parameter must be passed in as well:
> eng\copy-wpf.ps1 -destination "version" -shared  

#### Testing API changes 
The above instructions imply that you are testing assemblies that don't have any changes to the
public API surface that the test application needs to use. If you need to test some API changes
you are making, the C# team has done some great work to make this relatively straightforward.
When the C# compiler detects a collision with assembly references, the assembly with the
higher version number is chosen. Assuming our locally built binaries are newer than what is
installed, we can then simply reference those local binaries directly from the project file, like this:

*Note: you should build locally with the `-pack` param to ensure the binaries are put in the correct location.* 

```
  <PropertyGroup>
     <!-- Change this value based on where your local repo is located -->
     <WpfRepoRoot>d:\dev\src\dotnet\wpf</WpfRepoRoot>
     <!-- Publishing a self-contained app ensures our binaries are used. -->
     <SelfContained>true</SelfContained>
    <!-- The runtime identifier here should match below. -->
    <RuntimeIdentifier>win-x86</RuntimeIdentifier>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="$(WpfRepoRoot)\artifacts\Microsoft.DotNet.Wpf.GitHub\ref\netcoreapp3.0\*.dll" Private="false" />
    <ReferenceCopyLocalPaths Include="$(WpfRepoRoot)\artifacts\Microsoft.DotNet.Wpf.GitHub\lib\netcoreapp3.0\*.dll" />
    <ReferenceCopyLocalPaths Include="$(WpfRepoRoot)\artifacts\Microsoft.DotNet.Wpf.GitHub\lib\win-x86\*.dll" />
  </ItemGroup>
```

### Testing PresentationBuildTasks
-- add more content here --

## More Information

* [git commands and workflow](https://github.com/dotnet/corefx/wiki/git-reference)
* [Coding guidelines](https://github.com/dotnet/corefx/tree/master/Documentation#coding-guidelines)
* [up-for-grabs WPF issues](https://github.com/dotnet/wpf/issues?q=is%3Aopen+is%3Aissue+label%3Aup-for-grabs)
* [easy WPF issues](https://github.com/dotnet/wpf/issues?utf8=%E2%9C%93&q=is%3Aopen+is%3Aissue+label%3Aeasy)
