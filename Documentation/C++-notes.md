# C++ Notes

- `$(RepoRoot)Wpf.Cpp.Props` contains all the important C++ related properties 
 - This file is an amalgam of `$(RepoRoot)tools-local\vcxproj.props` and `$(RepoRoot)src\wpf\vcxproj.props` found in Dotnet-Trusted repo
 - It's very important to include this file after `Microsoft.Cpp.Default.props` and before `Microsoft.Cpp.props` like this:
     `<Import Project="$(VCTargetsPath)\Microsoft.Cpp.Default.props" />
      <Import Project="$(WpfCppProps)" />
      <Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />`
 - Some properties related to managed C++ builds have not been moved over to `Wpf.cpp.props`. We don't have a working `/clr:netcore` system in place yet.
 - Remember:
   - s/`$(ArchGroup)`/`$(Architecture)` See how `$(Architecture)` is defined based on `$(Platform)` in `wpf.cpp.props`
   - s/`$(ConfigurationGroup)`/`$(Configuration)`
- We need to undefine the `TargetFramework` property when `ProjectReference`-ing a C++/CLI project from a C# project
  - When a C++/CLI project is built directly from the solution, `TargetFramework=netcoreapp3.0` property is NOT passed to it. 
  - When the same project is built via a C# project, it receives a global property `TargetFramework=netcoreapp3.0`. 
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
