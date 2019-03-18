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
  
  There is a sample provided at `src\Microsoft.DotNet.Wpf\test\sample\HelloWorld\HelloWorld.csproj` that shows how to use this for compilation. 
  
  Also look at `src\Microsoft.DotNet.Wpf\src\PresentationUI\PresentationUI.csproj`. 
  
  

In addition to these, care must be taken for the following: 

- Use `<EmbeddedResource>` instead of `<Resource>`
  - `PresentationBuildTask` will strip out `<Resource>` items during `_CompileTemporaryAssembly` phase. Using `EmbeddedResource` is equivalent (esp. in for `Xlf` based string resource generation with `Arcade.Sdk`) and will not be adversely affected by `PresentationBuildTasks` transformations. 
- Always use `<NetCoreReference>` instead of implicitly acquiring the full set of `Microsoft.NetCore.App` references. 
  - `Microsoft.NetCore.App` contains a version `WindowsBase` that clashes with WPF's `WindowsBase` during markup compilation.
  - To avoid this clash, we must always specify the references we need explicitly. 
  - Also, our code-base requires that all references be specified explicitly anyway to avoid inadvertent reference-creep to bug-fixes. 