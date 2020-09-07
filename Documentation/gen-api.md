# GenApi Usage in WPF on .NET Core
In WPF on .NET Core, C# reference assemblies are created via the use of [GenAPI](https://github.com/dotnet/arcade/tree/master/src/Microsoft.DotNet.GenAPI) and a separate reference assembly project located in the `ref` directory under a particular assemblies source directory.

WPF assemblies make extensive use of the [InternalsVisibleToAttribute](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.internalsvisibletoattribute?view=netcore-3.0) which precludes the use of [ProduceReferenceAssembly](https://docs.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-properties?view=vs-2019) or [ProduceOnlyReferenceAssembly](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/refonly-compiler-option).  This is because these compiler options will include internal types and members in the reference assembly.  In WPF, this creates dangling references to assemblies that do not exist in the `WindowsDesktop` reference pack.

Using GenAPI allows us to strip out internals, removing the dangling references from our reference assemblies.

## [GenApi.props](/eng/WpfArcadeSdk/tools/GenApi.props)
Contains various properties related to GenAPI runs and configurations.
* `GenAPIEnabledProjects`
  * The set of projects to run GenAPI on
* `GlobalApiExclusionsFile`
  * A file that specifies API surface area to exclude from code generation (see [GlobalApiExclusions.txt](/eng/WpfArcadeSdk/tools/GenApi/GlobalApiExclusions.txt))
* `GlobalAttrExclusionsFile`
  * A file that specifies Attributes to exclude from code generation (see [GlobalAttrExclusions.txt](/eng/WpfArcadeSdk/tools/GenApi/GlobalAttrExclusions.txt))
* `GenAPIAdditionalParameters`
  * Parameters to GenAPI built up from local configuration
* _GenerateReferenceAssemblySource
  * A private parameter used to enable GenAPI targets
## [GenApi.targets](/eng/WpfArcadeSdk/tools/GenApi.targets)
Contains targets and properties related to GenAPI runs
* `GenAPITargetDir`
  * The directory where GenAPI will generate code
* `GenAPITargetPath`
  * The full path to the file GenAPI will generate
* `EnsureGenAPITargetDirectory`
  * Creates the directory specified by `GenAPITargetDir` if it does not exist
## Using GenAPI in WPF
GenAPI is run only on-demand.  In the event that a change to a runtime assembly creates new public surface area, a developer will see an [ApiCompat](api-compat.md) error between the reference assembly and the runtime assembly.  In order to address this, the developer must run GenAPI to generate new reference assembly code.
### Running GenAPI
GenAPI can be run by setting the following MSBuild property while building.
```
/p:GenerateReferenceAssemblySource=true
```
When a build is run with that property enabled, GenAPI will read the runtime assembly and generate a new `{AssemblyName}.cs` file under the ref directory in the assembly's source tree.

This new file will contain the newly created surface area and will need to be checked in along with the runtime assembly change.  The next build without `GenerateReferenceAssemblySource` enabled will no longer display an ApiCompat error as the surface area will now match the baseline.
### Issues with GenAPI
Often, GenAPI will generate code output that will contain code that is either private, internal, or creates build errors.  For this reason a developer usually cannot just use the output of GenAPI directly.  Instead, the developer should do the following:
* Build with GenAPI enabled
* Diff the output file against the previous version
* Extract just the new surface area (and related code) from the generated code
* Revert the generated file
* Add back the new surface area to the reference assembly code
* Ensure that nothing in the new surface area is private or internal unless requried by XAML compilation or other reference assemblies
* Rebuild without GenAPI enabled and verify there are no ApiCompat errors
