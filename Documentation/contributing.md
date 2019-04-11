# Contributing Guide

The primary focus of .NET Core 3.0 release for WPF is to achieve parity with .NET Framework. Priority will be given to changes that align with that goal. See the [roadmap](../roadmap.md) to understand project goals.

We need the most help with the following types of changes:

* Test fixes, test improvements and new tests increasing code coverage.
* Bug fixes that specifically target partity between .NET Core and .NET Framework.

Please [file an issue](https://github.com/dotnet/wpf/issues) for any larger change you would like to propose.

See [Developer Guide](developer-guide.md) to learn how to develop changes for this repo.

This project follows the general [.NET Core Contribution Guidelines](https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/contributing.md). The contribution bar from the general contribution guidelines is copied below.

## Do not change files in the `Shared`, `WindowsBase`, `PresentationCore`, and `PresentationFramework` directories

The directories [`src/Microsoft.DotNet.Wpf/src/Shared`](https://github.com/dotnet/wpf/tree/master/src/Microsoft.DotNet.Wpf/src/Shared), [`src/Microsoft.DotNet.Wpf/src/WindowsBase`](https://github.com/dotnet/wpf/tree/master/src/Microsoft.DotNet.Wpf/src/WindowsBase), [`src/Microsoft.DotNet.Wpf/src/PresentationCore`](https://github.com/dotnet/wpf/tree/master/src/Microsoft.DotNet.Wpf/src/PresentationCore), [`src/Microsoft.DotNet.Wpf/src/PresentationFramework`](https://github.com/dotnet/wpf/tree/master/src/Microsoft.DotNet.Wpf/src/PresentationFramework) contains files that are shared between the public GitHub repo and the internal WPF repos. Changes to files in this directory will *not* be accepted since they also affect internal code. Once all the appropriate sources have been published to the public repo, this restriction will no longer be needed.

## Contribution "Bar"

Project maintainers will consider changes that improve the product or fix known bugs (please file issues to make bugs "known").

Maintainers will not merge changes that have narrowly-defined benefits due to compatibility risk or complexity added to the product. We may revert changes if they are found to be breaking.

Most .NET Core components are cross-platform and we appreciate contributions that either improve their feature set in a given environment or that add support for a new environment. We will typically not accept contributions that implement support for an OS-specific technolology on another operating system. For example, we do not intend to create an implementation of the Windows registry for Linux or an implementation of the macOS keychain for Windows. We also do not intend to accept contributions that provide cross-platform implementations for Windows Forms or WPF.

Contributions must also satisfy the other published guidelines defined in this document.

## Code Formatting Improvements and Minor Enhancements

We will consider code-formatting improvements that are identified by running code analyzers.

Our CodeAnalysis rules are not enabled by default. These can be enabled by setting the MSBuild property `EnableAnalyzers=true` (in commandline, it is set as `/p:EnableAnalyzers=true`).  

The code analyzer would likely recommend changes that can result in changes to the generated IL. In general, we prefer code-formatting PR's to be limited to changes that do not have any impact on the IL - these are easier to review and approve and do not require additional testing. 

Please open issues for changes that affect the IL or might require additional validation, and work with the project maintainers to determine whether a PR would be appropriate.
