# API Compatibility
API compatibility is a build-time check that ensures that an _implementation_ assembly implements the API surface area of a _contract_ assembly.

For `WPF on .NET Core`, this means the following:
* All `WPF on .NET Core` reference assemblies (_implementation_) contain **at least** the API surface area contained by `WPF on .NET Framework 4.8` reference assemblies (_contract_), see [WpfValidateApiCompatForNetFramework](#wpfvalidateapicompatfornetframework).
* All hand-crafted reference assemblies for `WPF on .NET Core` (_implementation_) contain **exactly** the needed API surface area defined by their corresponding runtime assemblies (_contract_), see [WpfValidateApiCompatForRef](#wpfvalidateapicompatforref). (If you're adding new API surface area, you will need to update the hand-crafted reference assemblies, following [these instructions](https://github.com/dotnet/wpf/blob/main/Documentation/gen-api.md).)

This is accomplished by the use of the [Arcade API Compatibility tool](https://github.com/dotnet/arcade/blob/main/src/Microsoft.DotNet.ApiCompat) with some modifications to fit our specific needs.

## [ApiCompat.props](/eng/WpfArcadeSdk/tools/ApiCompat.props)
This props file implements necessary elements to trigger and control the usage of API Compatibility checks.
### Net48CompatNeededProjects
This property contains a list of projects that should have their reference assemblies compared against reference assemblies for `WPF on .NET Framework 4.8`.
### Net48RefAssembliesDir
This property points to the directory where reference assemblies for `WPF on .NET Framework 4.8` will be downloaded during native tool acquisition. In order
to avoid requiring the  [.NET Framework 4.8 Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net48) to be installed on all machines that build `WPF on .NET Core`, a private tools zip is used that
contains a copy of these assemblies.
### RefApiCompatNeededProjects
This property contains a list of projects that have hand-crafted reference assemblies that must be compared against their corresponding runtime assemblies during API Compatibility checks.

## [ApiCompat.targets](/eng/WpfArcadeSdk/tools/ApiCompat.targets)
This targets file implements necessary targets to run API compatibility checks.
### Properties
#### RunNetFrameworkApiCompat
Controls if a project's reference assembly is checked for API compatibility against the reference assemblies for `WPF on .NET Framework 4.8`.
#### RunRefApiCompat
Controls if a project's hand-crafted reference assembly is checked for API compatibility against its corresponding runtime assembly.
#### RunApiCompat
Controls if Arcade's default API compatibility targets will run.  WPF turns this off.
#### ApiCompatBaseline
Controls the location of the API compatibility baseline file for a specific project and API compatibility check.
### Items
These MSBuild Items are important to the setup of the API compatibility checks.
#### ResolvedMatchingContract
This points to the assembly that contains the contract API surface to validate against.
#### ResolvedImplementationAssembly
This points to the assembly whose API surface is being validated.
### Targets
The various targets both setup and execute API compatibility checks.
#### ResolveNetFrameworkApiCompatItems
Sets up [ApiCompatBaseline](#ApiCompatBaseline), [ResolvedMatchingContract](#ResolvedMatchingContract), and [ResolvedImplementationAssembly](#ResolvedImplementationAssembly) for projects that require
API compatibility checks against `WPF on .NET Framework 4.8`.  This is run before [WpfValidateApiCompatForNetFramework](#WpfValidateApiCompatForNetFramework) to
ensure that the necessary configuration is available for the check.
#### ResolveRefApiCompatItems
Sets up [ApiCompatBaseline](#ApiCompatBaseline), [ResolvedMatchingContract](#ResolvedMatchingContract), and [ResolvedImplementationAssembly](#ResolvedImplementationAssembly) for projects that require
API compatibility checks between runtime assemblies and hand-crafted reference assemblies.  This is run before [WpfValidateApiCompatForRef](#WpfValidateApiCompatForRef) to
ensure that the necessary configuration is available for the check.
#### WpfValidateApiCompatForNetFramework
Calls the API compatibility tool in order to validate a particular project's reference assembly against the corresponding reference assembly for `WPF on .NET Framework 4.8`.
The [ResolvedMatchingContract](#ResolvedMatchingContract) is the `.NET Framework 4.8` assembly and the [ResolvedImplementationAssembly](#ResolvedImplementationAssembly) is the
`.NET Core` assembly.  This will generate an MSBuild error for each compatibility issue found.  A developer can examine the current [baseline files](#Baseline-Files) to get
an idea of the kinds of errors that can be reported.

If the tool fails completely, an error of the form "ApiCompat failed for..." will be generated.  If this occurs, please [file an issue](https://github.com/dotnet/wpf/issues/new/choose) and include a link to your fork and branch that failed.
#### WpfValidateApiCompatForRef
Calls the API compatibility tool in order to validate a particular project's hand-crafted reference assembly against the corresponding runtime assembly.
The [ResolvedMatchingContract](#ResolvedMatchingContract) is the runtime assembly and the [ResolvedImplementationAssembly](#ResolvedImplementationAssembly) is the
hand-crafted reference assembly.  This will generate an MSBuild error for each compatibility issue found.  A developer can examine the current [baseline files](#Baseline-Files) to get
an idea of the kinds of errors that can be reported.

If the tool fails completely, an error of the form "ApiCompat failed for..." will be generated.  If this occurs, please [file an issue](https://github.com/dotnet/wpf/issues/new/choose) and include a link to your fork and branch that failed.
## [Baseline Files](/src/Microsoft.DotNet.Wpf/ApiCompat/Baselines)
This directory contains the aggregate baseline files for all initial API compatibility checks.  The filenames are of the general form
"{Project}-{APICompatType}.baseline.txt", where `APICompatType` is either `Net48` or `ref`.

Errors listed in a baseline file are ignored by the API compatibility tool on subsequent runs.

These baselined errors are, generally, one of the following:
* Intentional API changes that diverge from `WPF on .NET Framework 4.8`
* Errors resulting from changes to underlying assemblies in `.NET Core` that do not adversely affect product functionality
* Errors due to build-specific architecture at the time of baselining (e.g., the split nature of WPF's product build).

A developer can re-baseline the entirety of the product by setting the property `BaselineAllAPICompatError` to `true` during a build.
