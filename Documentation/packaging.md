# Packaging

Packaging is implemented in the following files:

```
 $(WpfArcadeSdkToolsDir)\Packaging.props
 $(WpfArcadeSdkToolsDir)\Packaging.targets
 $(RepoRoot)\packaging\**\*
    │   
    ├───Microsoft.DotNet.Arcade.Wpf.Sdk
    │       Microsoft.DotNet.Arcade.Wpf.Sdk.ArchNeutral.csproj
    │       
    ├───Microsoft.DotNet.Wpf.DncEng
    │       Microsoft.DotNet.Wpf.DncEng.ArchNeutral.csproj
    │       Microsoft.DotNet.Wpf.DncEng.csproj
    │       
    └───Microsoft.DotNet.Wpf.GitHub
            Microsoft.DotNet.Wpf.GitHub.ArchNeutral.csproj
            Microsoft.DotNet.Wpf.GitHub.csproj
        
```
 
 - The `ArchNeutral` packages are built only during the `x86` (i.e., `AnyCPU`) build phase
    - Normally, the `ArchNeutral` packages will contain a `runtime.json` file that incorporates the *Bait & Switch* technique for referencing RID-specific packages automatically. 
    - `runtime.json` functionality is turned off when a packaging project requests so by setting `$(PlatformIndependentPackage)=true`.
      - See [Improve documentation - bait and switch pattern, other #1282 ](https://github.com/NuGet/docs.microsoft.com-nuget/issues/1282)
      
    
 - The packages that are not `ArchNeutral` are architecture-specific, and will produce packages containing the RID (`win-x86`, `win-x64`) as a prefix
   - The arch-specific packages are produced in each of the build phases. 
 
#### Package Names

There are two packages produced out of this repo, a *transport* package and an *MsBuild Sdk* package:

- `Microsoft.DotNet.Wpf.Github`
  - This contains assemblies and corresponding reference binaries that are currently built out of this repo ([https://www.github.com/dotnet/wpf](https://www.github.com/dotnet/wpf)). 
- `Microsoft.DotNet.Arcade.Wpf.Sdk`
  - This is an *MsBuild Sdk*, and is and extension to [Microsoft.DotNet.Arcade.Sdk](https://www.github.com/dotnet/arcade).
  - This Sdk contains all the build props, targets and scripts needed to build WPF. 
  - Since WPF's build is split across two repos, we build this Sdk out of one repo, and consume it as an *MsBuild Sdk* in the other repo. 

#### Opting into a package

- An assembly opts-into a package in `$(WpfArcadeSdkToolsDir)\Packaging.props` by simply setting the `PackageName` property, for e.g., like this:

    `<PackageName Condition="'$(MSBuildProjectName)'=='WpfGfx'">$(DncEngTransportPackageName)</PackageName>`

In practice, this is not needed. *Shipping* assemblies are already enumerated in detail within `$(WpfArcadeSdkToolsDir)ShippingProjects.props`, and each one of them is marked for packaging correctly within `Packaging.props` based on its `$(RepoLocation)` value (possible values are `{Internal, External}`)

- These package names that various assembly projects can opt to be packaged into are defined in `$(WpfArcadeSdkToolsDir)\Packaging.props`. The project names under `$(RepoRoot)\packaging\` must match these.

#### How Packaging Works

##### Preparing Package Assets (*`PreparePackageAssets`* target)

- *`PreparePackageAssets`* target is defined in `$(WpfArcadeSdkToolsDir)Packaging.targets`
- It runs after *`Build`*, and copies all project outputs, symbols, reference assemblies, satellite assemblies, and content files (defined in *`@(PackageContent)`* `ItemGroup`) to `$(ArtifactsPackagingDir)$(PackageName)\lib\$(TargetFrameworkOrRuntimeIdentifier)\` 
- If `@(PackagingAssemblyContent)` is populated, then only those files from `$(OutDir)` would be copied and packaged further - and none others would be included.
- At the end of this target, all files that need to be packed would have been laid out in the right folder hierarchy, and ready to be packed. 

##### Populate the `@(Content)` itemgroup with custom lsit of package-assets (*`CreateContentFolder`* target)

- *`CreateContentFolder`* is defined in `$(WpfArcadeSdkToolsDir)Packaging.targets`
- It runs just before *`GenerateNuspec`* target, and populates `@(Content)` with files that were copied during *`PreparePackageAssets`*
- This is consume by NuGet `PackTask` to create the package. 

##### Create a Nuget Package 

- The projects under `$(RepoRoot)\packaging` are the only ones with `$(IsPackable)=true`
- The layout of the generated package is identical to the layout under `$(ArtifactsPackagindir)$(PackageName)\`
