# Getting started with WPF for .NET Core

This document describes the experience of using WPF on .NET Core. The [Developer Guide](developer-guide.md) describes how to develop features and fixes for WPF.

## Installation

Choose one of these options:

1. [.NET Core 3.1 SDK (recommended)](https://www.microsoft.com/net/download)
2. [.NET Core 3.1 daily build (latest changes, but less stable)](https://github.com/dotnet/core/blob/master/daily-builds.md)

## Creating new applications

You can create a new WPF application with `dotnet new` command, using the new templates for WPF.

In your favorite console run:

```cmd
dotnet new wpf -o MyWPFApp
cd MyWPFApp
dotnet run
```

## Samples

Check out the [WPF for .NET Core 3 samples](https://github.com/dotnet/samples/tree/master/wpf) for HelloWorld example. The existing [WPF for .NET Framework samples](https://github.com/Microsoft/WPF-Samples) have also been updated to dual-target .NET Framework and .NET Core 3.


## Documentation

For WPF API documentation, see the [.NET API Browser](https://docs.microsoft.com/en-us/dotnet/api/?view=netcore-3.0).

For conceptual documentation (architecture, how-tos, etc.) most of the [documentation for WPF for .NET Framework](https://docs.microsoft.com/en-us/visualstudio/designers/getting-started-with-wpf?view=vs-2017) applies equally well to WPF for .NET Core 3. The main differences are around project structure and lack of Designer support.

## Missing features

* To use the XAML Designer for WPF on .NET Core 3.1 you will need VS 2019 16.4.
* [XAML Browser applications (XBAPs)](https://docs.microsoft.com/en-us/dotnet/framework/wpf/app-development/wpf-xaml-browser-applications-overview) are not supported for .NET Core 3. 
* Not all .NET Framework features are supported for .NET Core 3. You can use the [.NET API Portability Analyzer](https://github.com/microsoft/dotnet-apiport) to see if your existing code can run on .NET Core 3.

A full list of supported / unsupported features will be available in a future update. 
