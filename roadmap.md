# WPF for .NET Core Roadmap

This roadmap communicates priorities for evolving and extending the scope of WPF for .NET Core.

At present, our primary focus is enabling the following for .NET Core 3.0:

* Achieve WPF functional and performance parity compared to .NET Framework
* Publish remaining WPF components to the repo
* Publish (and write) more WPF tests to the repo

> Note: There are some specific .NET Framework features will not be supported, such as [XBAPs](https://docs.microsoft.com/dotnet/framework/wpf/app-development/wpf-xaml-browser-applications-overview).

As we complete those goals, we'll update our roadmap to include additional feature/capability areas we will focus on next.

## Timeline for Open Source
| Milestone | Date |
|---|---|
|Initial launch of WPF for .NET Core repository (beginning with System.Xaml)|Dec 4, 2018|
|Roadmap update for feature focus areas|Early 2019|
|Add adequate tests that enable validating and merging community PRs|Continues thru 2019|
|Add remaining WPF for .NET Core components to repository|Continues thru 2019|

## Porting Status

The port from WPF for .NET Framework is still in progress.  All components applicable to WPF for .NET Core will eventually be published to this repository.

### Currently Available
* Components:
  * [System.Xaml](src/Microsoft.DotNet.Wpf/src/System.Xaml)
  * [WindowsBase](src/Microsoft.DotNet.Wpf/src/WindowsBase)
  * [PresentationCore](src/Microsoft.DotNet.Wpf/src/PresentationCore)
  * [PresentationFramework](src/Microsoft.DotNet.Wpf/src/PresentationFramework)
  * [PresentationBuildTasks](src/Microsoft.DotNet.Wpf/src/PresentationBuildTasks)
  * [DirectWriteForwarder](src/Microsoft.DotNet.Wpf/src/DirectWriteForwarder)
  * [ReachFramework](src/Microsoft.DotNet.Wpf/src/ReachFramework)
  * [System.Windows.Input.Manipulations](src/Microsoft.DotNet.Wpf/src/System.Windows.Input.Manipulations)
  * [UI Automation assemblies](src/Microsoft.DotNet.Wpf/src/UIAutomation)
    * [UIsAutomationClient](src/Microsoft.DotNet.Wpf/src/UIAutomation/UIAutomationClient)
    * [UIAutomationClientSideProviders](src/Microsoft.DotNet.Wpf/src/UIAutomation/UIAutomationClientSideProviders)
    * [UIAutomationProvider](src/Microsoft.DotNet.Wpf/src/UIAutomation/UIAutomationProvider)
    * [UIAutomationType](src/Microsoft.DotNet.Wpf/src/UIAutomation/UIAutomationTypes)
  
* Tests:
  * [DrtXaml](src/Microsoft.DotNet.Wpf/test/DRT/DrtXaml)

### In Progress
Note: This list is in rough priority order and may change.
* Components: 
  * `WindowsFormsIntegration`
  * `System.Windows.Controls.Ribbon`
  * `WPF Themes`
    * `PresentationFramework-Aero`
    * `PresentationFramework-Aero2`
    * `PresentationFramework-AeroLite`
    * `PresentationFramework-Classic`
    * `PresentationFramework-Luna`
    * `PresentationFramework-Royale`
  * `WPF Extensions`
    * `PresentationFramework-SystemCore`
    * `PresentationFramework-SystemData`
    * `PresentationFramework-SystemDrawing`
    * `PresentationFramework-SystemXml`
    * `PresentationFramework-SystemXmlLinq`
  * `System.Windows.Presentation`
  * `PresentationUI`
  * `System.Printing`
  * `PenIMC_cor3`
  * `WpfGfx_cor3`
