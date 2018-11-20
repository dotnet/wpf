# Windows Presentation Framework (WPF)
This repo contains the open-source components of Windows Presentation Foundation (WPF) that run on top of .NET Core 3. This is based on, but separate from, the version of WPF that is supported in the Windows Desktop .NET Framework.

Currently, only a subset of the full WPF Framework is available as open-source (`System.Xaml` and related tests); more code will be pushed to the repo over the coming months. 

# Using the code
To get started use WPF on .NET Core 3, follow the [Getting Started instructions](https://github.com/dotnet/samples/tree/master/wpf). The WPF APIs are documented on MSDN in the [.NET API Browser](https://docs.microsoft.com/en-us/dotnet/api/?view=netstandard-2.0) (*note that this URL doesn't exist yet - I assume 3.0 will come online soon?*).

All WPF features are available on .NET Core 3, but some .NET Framework features (such as remoting or AppDomains) are not supported. See [**insert some link to .NET Core docs**](http://msdn.microsoft.com) for more info

# Known issues
* WPF relies on the VCRuntime redistributable package. For this initial release, you will need to install the VCRuntime redistributable on any machines where you want WPF applications to run. You can [follow this issue in /dotnet/core-setup](https://github.com/dotnet/core-sdk/issues/160#issuecomment-440103176) for more information.
* There is currently no XAML Designer support for WPF on .NET Core. If you wantto use the XAML Designer, you will need to do that in the context of a .NET Framework (Desktop) project. 

# How to build the code
Currently, this repo only contains the `System.Xaml` assembly and its related tests. It is not sufficient to build a complete version of WPF, but you can rebuild `System.Xaml.dll` and run the associated tests.

## To build `System.Xaml`

* In the root of your repo, run `build` (or `build -verbose` for logs).

## To run the tests

* In the root of your repo, run `test` or open the solution file `src\Microsoft.DotNet.Wpf\test\DRT\DrtXaml.sln` ("DRT" is an internal name used for unit tests).

# Contribution guidelines
## Issues
* If you have feedback (bug report, feature suggestion, etc.)  *specifically* about WPF on .NET Core 3, please [open a new issue here](https://github.com/dotnet/wpf/issues/). 
* If you have more general feedback about .NET Core 3, please use the [dotnet/core](https://github.com/dotnet/core) repo.
* If you have feedback about WPF on the Desktop Framework, please report it using the Feedback Hub on Windows 10 (Category: *Developer Platfornm*, sub-category *UI frameworks and controls* and make it clear your feedback is for WPF, not UWP XAML).

## Code
We are not accepting pull requests to WPF at this time; we will udpate this section with contribution guidelines as we move closer to an initial full release of the code.

# Code of conduct
The WPF repo follows the [.NET Foundation Code of Condut](http://www.dotnetfoundation.org/code-of-conduct).

[![Build Status](https://dnceng.visualstudio.com/internal/_apis/build/status/dotnet.wpf)](https://dnceng.visualstudio.com/internal/_build/latest?definitionId=234)
