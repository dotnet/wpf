# Getting started with WPF for .NET Core


## Installation

Choose one of these options:

1. Official public preview [.NET Core 3.0 SDK Preview 1](https://www.microsoft.com/net/download), or
2. [Daily build](https://aka.ms/netcore3sdk) (for more installer options see [dotnet/code-sdk repo](https://github.com/dotnet/core-sdk)).


## Creating new applications

You can create a new WPF application with `dotnet new` command, using the new templates for WPF.

In your favorite console run:

```cmd
dotnet new wpf -o MyWPFApp
cd MyWPFApp
dotnet run
```


## WPF documentation

For WPF reference documentation (types, properties, methods, etc.) see the [.NET API Browser](https://docs.microsoft.com/en-us/dotnet/api/?view=netcore-3.0). <!-- note that this URL doesn't exist yet - I assume 3.0 will go live at the announce? -->

For conceptual documentation (architecture, how-tos, etc.) most of the [documentation for WPF for .NET Framework](https://docs.microsoft.com/en-us/visualstudio/designers/getting-started-with-wpf?view=vs-2017) applies equally well to WPF for .NET Core 3. The main differences are around project structure and lack of Designer support. 


## Samples

Check out the [WPF for .NET Core 3 samples](https://github.com/dotnet/samples/tree/master/wpf) for HelloWorld example. The existing [WPF for .NET Framework samples](https://github.com/Microsoft/WPF-Samples) have also been updated to dual-target .NET Framework and .NET Core 3.



## Known issues

* WPF Applications crash with `System.TypeLoadException` when the Visual C++ Redistributable for Visual Studio 2017 is not installed. The latest version of VC++ redistributable can be obtained from [the Visual C++ downloads page](https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads). This dependency will be removed prior to .NET Core 3.0 final release.
    * This is tracked by [#37](https://github.com/dotnet/wpf/issues/37).


## Missing features

* In this initial preview, WPF for .NET Core doesn't support the XAML Designer. If you want to use the XAML Designer, you will need to do that in the context of a .NET Framework project, e.g. by linking your .NET Core source files into a .NET Framework project.
    * You can see examples of how to do this in the [WPF Samples repo](https://github.com/Microsoft/WPF-Samples).
* [XAML Browser applications (XBAPs)](https://docs.microsoft.com/en-us/dotnet/framework/wpf/app-development/wpf-xaml-browser-applications-overview) are not supported for .NET Core 3. 
* Not all .NET Framework features are supported for .NET Core 3. You can use the [.NET API Portability Analyzer](https://github.com/microsoft/dotnet-apiport) to see if your existing code can run on .NET Core 3.

A full list of supported / unsupported features will be available in a future update. 
