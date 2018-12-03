# WPF for .NET Core Roadmap

This roadmap communicates priorities for evolving and extending the scope of WPF for .NET Core.

At present, our primary focus is enabling the following for .NET Core 3.0:

* Achieve WPF functional and performance parity compared to .NET Framework
* Publish remaining WPF components to the repo
* Publish (and write) more WPF tests to the repo

As we complete those goals, we'll update our roadmap to include additional feature/capability areas we will focus on next.

## Timeline for Open Source
| Milestone | Release Date |
|---|---|
|Initial launch of WPF for .NET Core repository (beginning with System.Xaml)|Dec 4, 2018|
|Ability to merge PRs from community|Early 2019|
|Roadmap update for feature focus areas|Early 2019|
|Add remaining WPF for .NET Core components to repository|Continues thru 2019|

## Porting Status

The port from WPF for .NET Framework is still in progress. Currently this repository contains these components:

* Components:
  * System.Xaml
* Tests:
  * DrtXaml

Components in this repository eventually roll up to the `Microsoft.NET.Sdk.WindowsDesktop` SDK.
