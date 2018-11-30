# Getting started with WPF for .NET Core



## Installation

Choose one of these options:

1. Official public preview [.NET Core 3.0 SDK Preview 1](https://www.microsoft.com/net/download), or
2. [Daily build](https://aka.ms/netcore3sdk) (for more installer options see [dotnet/code-sdk repo](https://github.com/dotnet/core-sdk)).


## Known issues

There is currently no XAML Designer support for WPF for .NET Core. If you want to use the XAML Designer, you will need to do that in the context of a .NET Framework project, e.g. by "linking" the .NET Core source files into a .NET Framework project.

WPF relies on the `VCRuntime` redistributable package. For this initial release, you will need to install the VCRuntime redistributable on any machines where you want WPF applications to run. See the Visual C++ section of [the Visual Studio 2017 redistributable files list](https://docs.microsoft.com/en-us/visualstudio/productinfo/2017-redistribution-vs#VisualStudio) for more information. You can also [follow this issue in /dotnet/core-setup](https://github.com/dotnet/core-sdk/issues/160#issuecomment-440103176) for future updates.


## Creating new applications

You can create a new WPF application with `dotnet new` command, using the new templates for WPF.

In your favorite console run:

```cmd
dotnet new wpf -o MyWPFApp
cd MyWPFApp
dotnet run
```


## WPF Documentation

For WPF reference documentation (types, properties, methods, etc.) see the [.NET API Browser](https://docs.microsoft.com/en-us/dotnet/api/?view=netcore-3.0). <!-- note that this URL doesn't exist yet - I assume 3.0 will go live at the announce? -->

For conceptual documentation (architecture, how-tos, etc.) most of the [documentation for WPF on Desktop](https://docs.microsoft.com/en-us/visualstudio/designers/getting-started-with-wpf?view=vs-2017) applies equally well to WPF on .NET Core 3. The main differences are around project structure and lack of Designer support. 

There are also some WPF features, such as [XAML Browser applications (XBAPs)](https://docs.microsoft.com/en-us/dotnet/framework/wpf/app-development/wpf-xaml-browser-applications-overview) that are not supported on .NET Core 3. A full list of supported / unsupported features will be available in a future update. Additionally, some .NET Framework features (such as partial-trust applications, speech, remoting, and AppDomains) are not supported at this time on .NET Core. You can use the [.NET API Portability Analyzer](https://github.com/microsoft/dotnet-apiport) to see if your existing code can run on .NET Core 3.


## Samples

Check out the .NET Core 3.0 WPF [samples](https://github.com/dotnet/samples/tree/master/wpf) for HelloWorld examples and more advanced scenarios.
