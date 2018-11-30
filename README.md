# Windows Presentation Framework (WPF)

[![Build Status](https://dnceng.visualstudio.com/internal/_apis/build/status/dotnet.wpf)](https://dnceng.visualstudio.com/internal/_build/latest?definitionId=234)

This repo contains the open-source components of Windows Presentation Foundation (WPF) that run on top of .NET Core.
This is based on, but separate from, the version of WPF that is part of .NET Framework.

We haven't finished porting WPF to .NET Core yet, which means not all source code is on GitHub (see [port status](#port-status) for details).
We plan to complete the port during 2019.
The reason it takes some time is that we need to support & build all the pieces in an open source way,
which requires decoupling the code base from our internal engineering system.
At the same time, we don't want to block open sourcing until the port is complete.
This is similar to how other .NET Core repos with existing code have been brought up, such as [CoreFx](https://github.com/dotnet/corefx) in 2014.

Even though .NET Core is a cross-platform technology, WPF only runs on Windows.



## Quick Links

* [.NET Core 3.0 SDK Preview 1](https://www.microsoft.com/net/download)
* [Overall .NET Core roadmap & shipdates](https://github.com/dotnet/core/blob/master/roadmap.md)



## Getting started with WPF on .NET Core

Follow [getting started instructions](Documentation/getting-started.md).



## Port Status

The port from WPF for .NET Framework is still in progress. Currently this repository contains these components:

* Binaries:
  * System.Xaml
* Tests:
  * DrtXaml

Binaries in this repository eventually roll up to the `Microsoft.NET.Sdk.WindowsDesktop` SDK.



## How to Engage, Contribute and Provide Feedback

Some of the best ways to contribute are to try things out, file bugs, join in design conversations, and fix issues.

* Use [daily builds](Documentation/getting-started.md#installation).
* If you have a question or found a bug, [file a new issue](https://github.com/dotnet/wpf/issues/new).
    * Issues with WPF on .NET Framework should be filed via Feedback Hub on Windows 10 (Category: *Developer Platfornm*, sub-category *UI frameworks and controls* and make it clear your feedback is for WPF, not WinUI XAML), or [Product Support](https://support.microsoft.com/en-us/contactus?ws=support) if you have a contract.

**IMPORTANT:** WPF for .NET Core 3.0 release focuses on parity with WPF for .NET Framework.
We do not plan to take contributions or address bugs that are not unique to WPF for .NET Core in 3.0 release.
Bugs which are present on both WPF platforms (for .NET Core and .NET Framework) will be prioritized for future releases of .NET Core (post-3.0).

### Issue Guide

Read our detailed [issue guide](Documentation/issue-guide.md) which covers:

* How to file high-quality bug reports
* How to use and understand Labels, Milestones, Assignees and Upvotes on issues
* How to escalate (accidentally) neglected issue or PR
* How we triage issues

For general .NET Core 3 issues (not specific to WPF), use the [.NET Core repo](https://github.com/dotnet/core/issues) or other repos if appropriate (e.g. [CoreFX](https://github.com/dotnet/corefx/issues), [WinForms](https://github.com/dotnet/winforms/issues)).

### Contributing Guide

Read our detailed [contributing guide](Documentation/contributing-guide.md) which covers:

* Which kind of PRs we accept/reject for .NET Core 3.0 release
* Coding style
* PR style preferences (squashing vs. merging, etc.)
* Developer guide for building and testing locally

### Community

This project has adopted the code of conduct defined by the [Contributor Covenant](https://contributor-covenant.org/) to clarify expected behavior in our community.
For more information, see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).

### Reporting security issues and security bugs

Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC) <secure@microsoft.com>.
You should receive a response within 24 hours.
If for some reason you do not, please follow up via email to ensure we received your original message.
Further information, including the MSRC PGP key, can be found in the [Security TechCenter](https://www.microsoft.com/msrc/faqs-report-an-issue).

Also see info about related [Microsoft .NET Core and ASP.NET Core Bug Bounty Program](https://www.microsoft.com/msrc/bounty-dot-net-core).



## License

.NET Core (including WPF repo) is licensed under the [MIT license](LICENSE.TXT).



## .NET Foundation

.NET Core WPF is a [.NET Foundation](https://www.dotnetfoundation.org/projects) project.

There are many .NET related projects on GitHub.

* [.NET home repo](https://github.com/Microsoft/dotnet) - links to 100s of .NET projects, from Microsoft and the community.
