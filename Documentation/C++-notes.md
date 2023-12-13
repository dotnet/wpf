# C++ Notes

- `$(RepoRoot)eng\WpfArcadeSdk\tools\` contains `Wpf.Cpp.Props`,`Wpf.Cpp.targets`, `Wpf.Cpp.PrivateTools.props` and `Wpf.Cpp.PrivateTools.targets`, which contain all the important C++ related properties 
- We need to undefine the `TargetFramework` property when `ProjectReference`-ing a C++/CLI project from a C# project
  - When a C++/CLI project is built directly from the solution, `TargetFramework=net6.0-windows` etc. property is NOT passed to it. 
  - When the same project is built via a C# project, it receives a global property `TargetFramework=net6.0-windows`. 
  - This results in msbuild treating those two instances as _sufficiently different_ and builds them independently. 
    - In turn, the same project is built twice (often simultaneously), and results in simultaneous writes to the PDB etc. 
	- This leads to build failures. 
  - The solution is to delete `TargetFramework` property when specifying `ProjectReference` to a C++/CLI project from a C# project, like this:
    `<ProjectReference Include="$(WpfSourceDir)PresentationCore\CPP\PresentationCoreCpp.vcxproj">
      <UndefineProperties>TargetFramework;TargetFrameworks</UndefineProperties>
     </ProjectReference>`

### Deprecated Compiler Features

- /nowarn:D9035 is now being passed to build.ps1 in order to suppress the following C++ compiler warnings:
  - cl : Command line error D9035: option 'clr:pure' has been deprecated and will be removed in a future release
  - cl : Command line error D9035: option 'Zc:forScope-' has been deprecated and will be removed in a future release
