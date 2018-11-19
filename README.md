# Windows Presentation Framework (WPF)
This repo contains the open-source components of Windows Presentation Foundation (WPF) that run on top of .NET Core 3. This is based on, but separate from, the version of WPF that is supported in the Windows Desktop .NET Framework.

Currently, only a subset of the full WPF Framework is available as open-source (`System.Xaml` and related tests); more code will be pushed to the repo over the coming months. 

# Using the code
To get started use WPF on .NET Core 3, follow the [Getting Started instructions](https://github.com/dotnet/samples/tree/master/wpf). The WPF APIs are documented on MSDN in the [.NET API Browser)[https://docs.microsoft.com/en-us/dotnet/api/?view=netstandard-2.0] (*note that this URL doesn't exist yet - I assume 3.0 will come online soon?*).

All WPF features are available on .NET Core 3, but some .NET Framework features (such as remoting or AppDomains) are not supported. See [**insert some link to .NET Core docs**](http://msdn.microsoft.com) for more info

# How to build the code
Currently, this repo only contains the `System.XAML` assembly and its related tests. It is not sufficient to build a complete version of WPF. 

**Can you do anything useful with it?**

# Contribution guidelines
We are not accepting pull requests to WPF for this Preview release; we will udpate this section with contribution guidelines as we move closer to an initial full release of the code.

If you have a feedback (bug, suggestion, etc.)  *specifically* about WPF on .NET Core 3, please [open a new issue here](https://github.com/dotnet/wpf/issues/). If you have more general feedback about .NET Core 3, please use the [dotnet/core](https://github.com/dotnet/core) repo.

# Code of conduct
The WPF repo follows the [.NET Foundation Code of Condut](http://www.dotnetfoundation.org/code-of-conduct).

[![Build Status](https://dnceng.visualstudio.com/internal/_apis/build/status/dotnet.wpf)](https://dnceng.visualstudio.com/internal/_build/latest?definitionId=234)
