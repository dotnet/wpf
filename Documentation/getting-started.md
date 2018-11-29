# Getting started with WPF for .NET Core



## Installation

Choose one of these options:

1. Official public preview [.NET Core 3.0 SDK Preview 1](https://www.microsoft.com/net/download), or
2. [Daily build](https://aka.ms/netcore3sdk) (for more installer options see [dotnet/code-sdk repo](https://github.com/dotnet/core-sdk)).

**WARNING:** There is currently no XAML Designer support for WPF on .NET Core.
If you want to use the XAML Designer, you will need to do that in the context of a .NET Framework project, e.g. by "linking" the .NET Core source files into a .NET Framework project.



## Creating new applications

You can create a new WPF application with `dotnet new` command, using the new templates for WPF.

In your favorite console run:

```cmd
dotnet new wpf -o MyWPFApp
cd MyWPFApp
dotnet run
```


## Samples

Check out the .NET Core 3.0 WPF [samples](https://github.com/dotnet/samples/tree/master/wpf) for HelloWorld examples and more advanced scenarios.
