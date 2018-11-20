# Windows Presentation Framework (WPF)
This repo contains the open-source components of Windows Presentation Foundation (WPF) that run on top of .NET Core 3. This is based on, but separate from, the version of WPF that is supported in the Windows Desktop .NET Framework.

Currently, only a subset of the full WPF Framework is available as open-source (`System.Xaml` and related tests); more code will be pushed to the repo over the coming months. 

# Using the code
To get started using WPF on .NET Core 3, follow the [Getting Started instructions](https://github.com/dotnet/samples/tree/master/wpf). The WPF APIs are documented on MSDN in the [.NET API Browser](https://docs.microsoft.com/en-us/dotnet/api/?view=netstandard-2.0).

> note that this URL doesn't exist yet - I assume 3.0 will come online soon?

Most of the [conceptual documentation for WPF on Desktop](https://docs.microsoft.com/en-us/visualstudio/designers/getting-started-with-wpf?view=vs-2017) applies equally well to WPF on .NET Core 3. The main differences are around project structure and lack of Designer support (see **Known issues**, below). There are also some WPF features, such as [XAML Browser applications (XBAPs)](https://docs.microsoft.com/en-us/dotnet/framework/wpf/app-development/wpf-xaml-browser-applications-overview) that are not supported on .NET Core 3. A full list of supported / unsupported features will be available in a future update. 

Additionally, some .NET Framework features (such as partial-trust applications, speech, remoting, and AppDomains) are not supported at this time on .NET Core. See [**insert some link to .NET Core docs**](http://msdn.microsoft.com) for more info.

# Known issues
* WPF relies on the VCRuntime redistributable package. For this initial release, you will need to install the VCRuntime redistributable on any machines where you want WPF applications to run. See the Visual C++ section of [the Visual Studio 2017 redistributable files list](https://docs.microsoft.com/en-us/visualstudio/productinfo/2017-redistribution-vs#VisualStudio) for more information. You can also [follow this issue in /dotnet/core-setup](https://github.com/dotnet/core-sdk/issues/160#issuecomment-440103176) for future updates.
* There is currently no XAML Designer support for WPF on .NET Core. If you want to use the XAML Designer, you will need to do that in the context of a .NET Framework (Desktop) project. 

# How to build the code
Currently, this repo only contains the `System.Xaml` assembly and its related tests. It is not sufficient to build a complete version of WPF, but you can rebuild `System.Xaml.dll` and run the associated tests.

## To build `System.Xaml`

* In the root of your repo, run `build` (or `build -verbose` for logs).

## To run the tests

* In the root of your repo, run `test` or open the solution file `src\Microsoft.DotNet.Wpf\test\DRT\DrtXaml.sln` ("DRT" is an internal name used for unit tests). Note that this will test the version of `System.Xaml.dll` currently installed in your shared framework; if you rebuild `System.Xaml.dll` and want to test your changes, you will need to replace the current DLL with your own (be sure to make a backup first).

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
