# Contributing Guide

Primary focus of .NET Core 3.0 release is to achieve parity with WPF for .NET Framework.
Since we're currently still porting parts of WPF codebase (incl. tests) to GitHub (see progress at [../README.md#port-status]),
we're not ready to handle non-trivial or larger contributions beyond parity fixes yet.

We plan to accept these kind of contributions during 3.0 release:

* Low-risk changes, which are easy to review (e.g. typos, comment changes, documentation improvements, etc.).
* Test fixes, test improvements and new tests increasing code coverage.
* Infrastructure fixes and improvements, which are aligned with achieving our goal to ship high quality .NET Core 3.0 release.
* Bug fixes for differences between WinForms for .NET Core and .NET Framework.

If you have a **larger change** falling into any of these categories, we recommend to **check with our team members** prior to creating a PR.
We recommend to first create a [new issue](https://github.com/dotnet/wpf/issues), where you can describe your intent and help us understand the change you plan to contribute.

**WARNING:** Expect that we may reject or postpone PRs which do not align with our primary focus (parity with WPF for .NET Framework), 
or which could introduce unnecessary risk (e.g. in code which is historically sensitive, or is not well covered by tests).
Such PRs may be closed and reconsidered later after we ship .NET Core 3.0.



## Developer Guide

Before you start, please review [WPF contributing doc](TODO) and **[.NET Core contributing doc](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/contributing.md)** for coding style and PR gotchas.

* Per-machine setup: [Machine setup](#machine-setup) and [Fork and clone repo](https://github.com/dotnet/corefx/wiki/Checking-out-the-code-repository)
* [Build and run tests](#build-and-run-tests)
* [git commands and workflow](https://github.com/dotnet/corefx/wiki/git-reference) - for newbies on GitHub
* Pick issue: [up-for-grabs](https://github.com/dotnet/wpf/issues?q=is%3Aopen+is%3Aissue+label%3Aup-for-grabs) or [easy](https://github.com/dotnet/wpf/issues?utf8=%E2%9C%93&q=is%3Aopen+is%3Aissue+label%3Aeasy)
* [Coding guidelines](https://github.com/dotnet/corefx/tree/master/Documentation#coding-guidelines)

### Machine Setup

TODO

### Build and run tests

#### To build

In the root of your repo, run `build.cmd` (or `build.cmd -verbose` for logs).

#### To run tests

In the root of your repo, run `test.cmd` or open the solution file `src\Microsoft.DotNet.Wpf\test\DRT\DrtXaml.sln` ("DRT" is an internal name used for unit tests).
* Note that this will test the version of `System.Xaml.dll` currently installed in your shared framework; if you rebuild `System.Xaml.dll` and want to test your changes, you will need to replace the current DLL with your own (be sure to make a backup first).
