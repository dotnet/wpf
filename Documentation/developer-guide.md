# Developer Guide

The following document describes the setup and workflow that is recommended for working on the WPF project. It assumes that you have read the [contributing guide](contributing.md).

The [Issue Guide](issue-guide.md) describes our approach to using GitHub issues.

## Machine Setup

Follow the [Building CoreFX on Windows](https://github.com/dotnet/corefx/blob/master/Documentation/building/windows-instructions.md) instructions.

WPF requires the following workloads and  components be selected when installing Visual Studio:

* Required Workloads:
  * .NET Desktop Development
  * Desktop development with C++
* Required Individual Components:
  * C++/CLI support
  * Windows 10 SDK

## Workflow

We use the following workflow for building and testing features and fixes.

You first need to [Fork](https://github.com/dotnet/corefx/wiki/Checking-out-the-code-repository#fork-the-repository) and [Clone](https://github.com/dotnet/corefx/wiki/Checking-out-the-code-repository#clone-the-repository) this WPF repository. This is a one-time task.

-- add more content here --

## More Information

* [git commands and workflow](https://github.com/dotnet/corefx/wiki/git-reference)
* [Coding guidelines](https://github.com/dotnet/corefx/tree/master/Documentation#coding-guidelines)
* [up-for-grabs WPF issues](https://github.com/dotnet/wpf/issues?q=is%3Aopen+is%3Aissue+label%3Aup-for-grabs)
* [easy WPF issues](https://github.com/dotnet/wpf/issues?utf8=%E2%9C%93&q=is%3Aopen+is%3Aissue+label%3Aeasy)
