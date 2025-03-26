# Windows Presentation Foundation (WPF)
[![.NET Foundation](https://img.shields.io/badge/.NET%20Foundation-blueviolet.svg)](https://www.dotnetfoundation.org/)
[![Build Status](https://dnceng.visualstudio.com/public/_apis/build/status/dotnet/wpf/dotnet-wpf%20CI)](https://dnceng.visualstudio.com/public/_build/latest?definitionId=270)
[![codecov](https://codecov.io/gh/dotnet/wpf/branch/main/graph/badge.svg?flag=production)](https://codecov.io/gh/dotnet/wpf)
[![MIT License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/dotnet/wpf/blob/main/LICENSE.TXT)

Windows Presentation Foundation (WPF) is a UI framework for building Windows desktop applications. 

WPF supports a broad set of application development features, including an application model, resources, controls, graphics, layout, data binding and documents. WPF uses the Extensible Application Markup Language (XAML) to provide a declarative model for application programming.

WPF's rendering is vector-based, which enables applications to look great on high DPI monitors, as they can be infinitely scaled. WPF also includes a flexible hosting model, which makes it straightforward to host a video in a button, for example.

Visual Studio's designer, as well as Visual Studio Blend, make it easy to build WPF applications, with drag-and-drop and/or direct editing of XAML markup.

As of .NET 6.0, WPF supports ARM64. 

See the [WPF Roadmap](roadmap.md) to learn about project priorities, status and ship dates.

[WinForms](https://github.com/dotnet/winforms) is another UI framework for building Windows desktop applications that is supported on .NET (7.0.x/6.0.x). WPF and WinForms applications only run on Windows. They are part of the `Microsoft.NET.Sdk.WindowsDesktop` SDK. You are recommended to use the most recent version of [Visual Studio](https://visualstudio.microsoft.com/downloads/) to develop WPF and WinForms applications for .NET.  

To build the WPF repo and contribute features and fixes for .NET 8.0, [Visual Studio 2022 Preview](https://visualstudio.microsoft.com/vs/preview/) is required.

## Getting started

* [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0), [.NET 7.0 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)
* [.NET Preview SDKs (8.0 daily, 7.0 servicing)](https://github.com/dotnet/installer)
* [Getting started instructions](Documentation/getting-started.md)
* [Contributing guide](Documentation/contributing.md)
* [Migrating .NET Framework WPF Apps to .NET Core](https://docs.microsoft.com/en-us/dotnet/desktop-wpf/migration/convert-project-from-net-framework)

## Status

- We are currently developing WPF for .NET 8. 

See the [WPF roadmap](roadmap.md) to learn about the schedule for specific WPF components.

Test published at [separate repo](https://github.com/dotnet/wpf-test) Tests and have limited coverage at this time. We will add more tests, however, it will be a progressive process.

The Visual Studio WPF designer is now available as part of Visual Studio 2019. 

## How to Engage, Contribute and Provide Feedback

Some of the best ways to contribute are to try things out, file bugs, join in design conversations, and fix issues.

* This repo defines [contributing guidelines](Documentation/contributing.md) and also follows the more general [.NET Core contributing guide](https://github.com/dotnet/runtime/blob/main/CONTRIBUTING.md).
* If you have a question or have found a bug, [file an issue](https://github.com/dotnet/wpf/issues/new).
* Use [daily builds](Documentation/getting-started.md#installation) if you want to contribute and stay up to date with the team.

### .NET Framework issues

Issues with .NET Framework, including WPF, should be filed on [VS developer community](https://developercommunity.visualstudio.com/spaces/61/index.html), 
or [Product Support](https://support.microsoft.com/en-us/contactus?ws=support).
They should not be filed on this repo.

## Relationship to .NET Framework

This code base is a fork of the WPF code in the .NET Framework. .NET Core 3.0 was released with a goal of WPF having parity with the .NET Framework version. Over time, the two implementations may diverge.

The [Update on .NET Core 3.0 and .NET Framework 4.8](https://blogs.msdn.microsoft.com/dotnet/2018/10/04/update-on-net-core-3-0-and-net-framework-4-8/) provides a good description of the forward-looking differences between .NET Core and .NET Framework.

This [update](https://devblogs.microsoft.com/dotnet/net-core-is-the-future-of-net/) states how going forward .NET Core is the future of .NET. and .NET Framework 4.8 will be the last major version of .NET Framework.


## Code of Conduct

This project uses the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct) to define expected conduct in our community. Instances of abusive, harassing, or otherwise unacceptable behavior may be reported by contacting a project maintainer at conduct@dotnetfoundation.org.

## Reporting security issues and security bugs

Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC) <secure@microsoft.com>. You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the MSRC PGP key, can be found in the [Security TechCenter](https://www.microsoft.com/msrc/faqs-report-an-issue).

Also see info about related [Microsoft .NET Core and ASP.NET Core Bug Bounty Program](https://www.microsoft.com/msrc/bounty-dot-net-core).

## License

.NET Core (including the WPF repo) is licensed under the [MIT license](LICENSE.TXT).

## .NET Foundation

.NET Core WPF is a [.NET Foundation](https://www.dotnetfoundation.org/projects) project.

See the [.NET home repo](https://github.com/Microsoft/dotnet) to find other .NET-related projects.

