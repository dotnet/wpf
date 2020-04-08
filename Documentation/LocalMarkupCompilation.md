### Local Markup Compilation 

This repo builds PresentationBuildTasks.dll and the WindowsDesktop SDK itself, which are needed for markup-compilation. 

Some of the projects in this repo require Markup Compilation themselves - for e.g., theme assemblies. 

Here, we outline the scheme used by this repo to bootstrap markup compilation before a full-fledged WindowsDesktop SDK is available. All projects in this repo are base .NET Core SDK projects that utilize additional props/targets outlined below to enable markup compilation. 

Local Markup Compilation is implemented in the following files: 

	- eng\WpfArcadeSdk\tools\Pbt.props
	- eng\WpfArcadeSdk\tools\Pbt.targets
	- eng\WpfArcadeSdk\tools\NoInternalTypeHelper.targets
	
	
See comments in these targets for further details. Some additional work is needed to make this work completely. 
It can be enabled for a project by setting this:
	
  ```
  <PropertyGroup>
    <InternalMarkupCompilation>true</InternalMarkupCompilation>
  </PropertyGroup>
  ```
  
  Also, it's a good idea to use the following properties: 
  
  ```
  <PropertyGroup>
    <NoInternalTypeHelper>true</NoInternalTypeHelper>
    <GenerateDependencyFile>false<GenerateDependencyFile>
  </PropertyGroup>
  ```
  
  Please take a look at `src\Microsoft.DotNet.Wpf\src\PresentationUI\PresentationUI.csproj` to see how this is utilized.
  

In addition to these, care must be taken for the following: 

- Use `<EmbeddedResource>` instead of `<Resource>`
  - `PresentationBuildTask` will strip out `<Resource>` items during `_CompileTemporaryAssembly` phase. Using `EmbeddedResource` is equivalent (esp. in for `Xlf` based string resource generation with `Arcade.Sdk`) and will not be adversely affected by `PresentationBuildTasks` transformations. 
- Always use `<NetCoreReference>` instead of implicitly acquiring the full set of `Microsoft.NetCore.App` references. 
  - `Microsoft.NetCore.App` contains a version `WindowsBase` that clashes with WPF's `WindowsBase` during markup compilation.
  - To avoid this clash, we must always specify the references we need explicitly. 
  - Also, our code-base requires that all references be specified explicitly anyway to avoid inadvertent reference-creep to bug-fixes. 