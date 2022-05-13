# Getting started with WPF for .NET

This document describes the experience of using WPF on .NET. The [Developer Guide](developer-guide.md) describes how to develop features and fixes for WPF.

## Installation

Choose one of these options:

1. [.NET 6.0 SDK (recommended)](https://www.microsoft.com/net/download)
2. [.NET 7.0 daily build (latest changes, but less stable)](https://github.com/dotnet/core/blob/main/daily-builds.md)

## Creating new applications

You can create a new WPF application with `dotnet new` command, using the new templates for WPF.

In your favorite console run:

```cmd
dotnet new wpf -o MyWPFApp
cd MyWPFApp
dotnet run
```

## Samples

Check out the [WPF for .NET samples](https://github.com/dotnet/samples/tree/main/wpf) for HelloWorld example. The existing [WPF for .NET samples](https://github.com/Microsoft/WPF-Samples) have also been updated to target .NET.


## Documentation

For WPF API documentation, see the [.NET API Browser](https://docs.microsoft.com/en-us/dotnet/api/?view=netcore-3.0).

For conceptual documentation (architecture, how-tos, etc.) most of the [documentation for WPF for .NET Framework](https://docs.microsoft.com/en-us/visualstudio/designers/getting-started-with-wpf) applies equally well to WPF for .NET. The main differences are around project structure and lack of Designer support.

## Missing features

* [XAML Browser applications (XBAPs)](https://docs.microsoft.com/en-us/dotnet/framework/wpf/app-development/wpf-xaml-browser-applications-overview) are not supported for .NET. 
* Not all .NET Framework features are supported for .NET. You can use the [.NET API Portability Analyzer](https://github.com/microsoft/dotnet-apiport) to see if your existing code can run on .NET.
