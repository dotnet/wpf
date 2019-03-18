# Projects and Assemblies

The following assemblies are being produced today: 
```
├───net472
│       PresentationBuildTasks.dll
│       PresentationBuildTasks.pdb
│       
├───netcoreapp2.1
│       PresentationBuildTasks.dll
│       PresentationBuildTasks.pdb
│       
├───netcoreapp3.0
│   │   DirectWriteForwarder.dll
│   │   DirectWriteForwarder.pdb
│   │   PresentationCore-CommonResources.dll
│   │   PresentationCore-CommonResources.pdb
│   │   PresentationCore.dll
│   │   PresentationCore.pdb
│   │   System.Windows.Input.Manipulations.dll
│   │   System.Windows.Input.Manipulations.pdb
│   │   System.Xaml.dll
│   │   System.Xaml.pdb
│   │   UIAutomationProvider.dll
│   │   UIAutomationProvider.pdb
│   │   UIAutomationTypes.dll
│   │   UIAutomationTypes.pdb
│   │   WindowsBase.dll
│   │   WindowsBase.pdb
│   │   
│   ├───cs
│   │       PresentationCore.resources.dll
│   │       System.Windows.Input.Manipulations.resources.dll
│   │       System.Xaml.resources.dll
│   │       UIAutomationProvider.resources.dll
│   │       UIAutomationTypes.resources.dll
│   │       WindowsBase.resources.dll
│   │       
│   ├───de
│   │       PresentationCore.resources.dll
│   │       System.Windows.Input.Manipulations.resources.dll
│   │       System.Xaml.resources.dll
│   │       UIAutomationProvider.resources.dll
│   │       UIAutomationTypes.resources.dll
│   │       WindowsBase.resources.dll
│   │       
│   ├───es
│   │       PresentationCore.resources.dll
│   │       System.Windows.Input.Manipulations.resources.dll
│   │       System.Xaml.resources.dll
│   │       UIAutomationProvider.resources.dll
│   │       UIAutomationTypes.resources.dll
│   │       WindowsBase.resources.dll
│   │       
│   ├───fr
│   │       PresentationCore.resources.dll
│   │       System.Windows.Input.Manipulations.resources.dll
│   │       System.Xaml.resources.dll
│   │       UIAutomationProvider.resources.dll
│   │       UIAutomationTypes.resources.dll
│   │       WindowsBase.resources.dll
│   │       
│   ├───it
│   │       PresentationCore.resources.dll
│   │       System.Windows.Input.Manipulations.resources.dll
│   │       System.Xaml.resources.dll
│   │       UIAutomationProvider.resources.dll
│   │       UIAutomationTypes.resources.dll
│   │       WindowsBase.resources.dll
│   │       
│   ├───ja
│   │       PresentationCore.resources.dll
│   │       System.Windows.Input.Manipulations.resources.dll
│   │       System.Xaml.resources.dll
│   │       UIAutomationProvider.resources.dll
│   │       UIAutomationTypes.resources.dll
│   │       WindowsBase.resources.dll
│   │       
│   ├───ko
│   │       PresentationCore.resources.dll
│   │       System.Windows.Input.Manipulations.resources.dll
│   │       System.Xaml.resources.dll
│   │       UIAutomationProvider.resources.dll
│   │       UIAutomationTypes.resources.dll
│   │       WindowsBase.resources.dll
│   │       
│   ├───pl
│   │       PresentationCore.resources.dll
│   │       System.Windows.Input.Manipulations.resources.dll
│   │       System.Xaml.resources.dll
│   │       UIAutomationProvider.resources.dll
│   │       UIAutomationTypes.resources.dll
│   │       WindowsBase.resources.dll
│   │       
│   ├───pt-BR
│   │       PresentationCore.resources.dll
│   │       System.Windows.Input.Manipulations.resources.dll
│   │       System.Xaml.resources.dll
│   │       UIAutomationProvider.resources.dll
│   │       UIAutomationTypes.resources.dll
│   │       WindowsBase.resources.dll
│   │       
│   ├───ru
│   │       PresentationCore.resources.dll
│   │       System.Windows.Input.Manipulations.resources.dll
│   │       System.Xaml.resources.dll
│   │       UIAutomationProvider.resources.dll
│   │       UIAutomationTypes.resources.dll
│   │       WindowsBase.resources.dll
│   │       
│   ├───tr
│   │       PresentationCore.resources.dll
│   │       System.Windows.Input.Manipulations.resources.dll
│   │       System.Xaml.resources.dll
│   │       UIAutomationProvider.resources.dll
│   │       UIAutomationTypes.resources.dll
│   │       WindowsBase.resources.dll
│   │       
│   ├───zh-Hans
│   │       PresentationCore.resources.dll
│   │       System.Windows.Input.Manipulations.resources.dll
│   │       System.Xaml.resources.dll
│   │       UIAutomationProvider.resources.dll
│   │       UIAutomationTypes.resources.dll
│   │       WindowsBase.resources.dll
│   │       
│   └───zh-Hant
│           PresentationCore.resources.dll
│           System.Windows.Input.Manipulations.resources.dll
│           System.Xaml.resources.dll
│           UIAutomationProvider.resources.dll
│           UIAutomationTypes.resources.dll
│           WindowsBase.resources.dll
│           
└───win-x86
        PenImc_cor3.dll
        PenImc_cor3.pdb
        PresentationNative_cor3.dll
        PresentationNative_cor3.pdb
        wpfgfx_cor3.dll
        wpfgfx_cor3.pdb
```        

The following projects exist in the repo. Those corresponding to the assemblies listed above are currently building. Others are in the process of being onboarded.

```
├───DirectWriteForwarder
│       DirectWriteForwarder.vcxproj
│       
├───Extensions
│   ├───PresentationFramework-SystemCore
│   │       PresentationFramework-SystemCore.csproj
│   │       
│   ├───PresentationFramework-SystemData
│   │       PresentationFramework-SystemData.csproj
│   │       
│   ├───PresentationFramework-SystemDrawing
│   │       PresentationFramework-SystemDrawing.csproj
│   │       
│   ├───PresentationFramework-SystemXml
│   │       PresentationFramework-SystemXml.csproj
│   │       
│   └───PresentationFramework-SystemXmlLinq
│           PresentationFramework-SystemXmlLinq.csproj
│           
├───PenImc
│   ├───dll
│   │       PenImc.vcxproj
│   │       
│   └───tablib
│           TabLib.vcxproj
│           
├───PresentationBuildTasks
│       PresentationBuildTasks.csproj
│       
├───PresentationCore
│   │   PresentationCore.csproj
│   │   
│   └───ref
│           PresentationCore.csproj
│           
├───PresentationCore-CommonResources
│   │   PresentationCore-CommonResources.csproj
│   │   
│   └───ref
│           PresentationCore-CommonResources.csproj
│           
├───PresentationFramework
│   │   PresentationFramework.csproj
│   │   
│   └───ref
│           PresentationFramework.csproj
│           
├───PresentationNative
│   ├───classification
│   │       classification.vcxproj
│   │       
│   ├───CLRHostWrapper
│   │       CLRHostWrapper.vcxproj
│   │       
│   ├───dll
│   │       PresentationNative.vcxproj
│   │       
│   ├───DWriteWrapper
│   │       DWriteWrapper.vcxproj
│   │       
│   ├───ums
│   │   ├───msls
│   │   │   ├───ls4
│   │   │   │       ls4.vcxproj
│   │   │   │       
│   │   │   └───lslo
│   │   │           lslo.vcxproj
│   │   │           
│   │   ├───nl
│   │   │       nl.vcxproj
│   │   │       
│   │   ├───pts3
│   │   │       pts3.vcxproj
│   │   │       
│   │   ├───ptswrapper
│   │   │       PTSWrapper.vcxproj
│   │   │       
│   │   └───shared
│   │           ptlsshared.vcxproj
│   │           
│   ├───Win32Wrapper
│   │       Win32Wrapper.vcxproj
│   │       
│   └───XpsPrintHelper
│           xpsprinthelper.vcxproj
│           
├───PresentationUI
│   │   PresentationUI.csproj
│   │   
│   └───ref
│           PresentationUI.csproj
│           
├───ReachFramework
│   │   ReachFramework.csproj
│   │   
│   └───ref
│       │   ReachFramework.csproj
│       │   
│       └───partial
│               ReachFramework-partial.csproj
│               
├───Shared
│   ├───OSVersionHelper
│   │       OSVersionHelper.vcxproj
│   │       
│   └───Tracing
│       │   wpf-etw.proj
│       │   
│       └───mcwpf
│               mcwpf.csproj
│               
├───System.Printing
│   │   System.Printing.vcxproj
│   │   
│   └───ref
│       │   System.Printing.csproj
│       │   
│       └───partial
│               System.Printing-partial.csproj
│               
├───System.Windows.Controls.Ribbon
│   │   System.Windows.Controls.Ribbon.csproj
│   │   
│   ├───ref
│   │       System.Windows.Controls.Ribbon.csproj
│   │       
│   └───Themes
│       └───Generator
│               ThemeGenerator.proj
│               
├───System.Windows.Input.Manipulations
│   │   System.Windows.Input.Manipulations.csproj
│   │   
│   └───ref
│           System.Windows.Input.Manipulations.csproj
│           
├───System.Windows.Presentation
│   │   System.Windows.Presentation.csproj
│   │   
│   └───ref
│           System.Windows.Presentation.csproj
│           
├───System.Xaml
│       System.Xaml.csproj
│       
├───Themes
│   ├───Generator
│   │       ThemeGenerator.nativeproj
│   │       
│   ├───PresentationFramework.Aero
│   │   │   PresentationFramework.Aero.csproj
│   │   │   
│   │   └───ref
│   │           PresentationFramework.Aero.csproj
│   │           
│   ├───PresentationFramework.Aero2
│   │   │   PresentationFramework.Aero2.csproj
│   │   │   
│   │   └───ref
│   │           PresentationFramework.Aero2.csproj
│   │           
│   ├───PresentationFramework.AeroLite
│   │   │   PresentationFramework.AeroLite.csproj
│   │   │   
│   │   └───ref
│   │           PresentationFramework.AeroLite.csproj
│   │           
│   ├───PresentationFramework.Classic
│   │   │   PresentationFramework.Classic.csproj
│   │   │   
│   │   └───ref
│   │           PresentationFramework.Classic.csproj
│   │           
│   ├───PresentationFramework.Luna
│   │   │   PresentationFramework.Luna.csproj
│   │   │   
│   │   └───ref
│   │           PresentationFramework.Luna.csproj
│   │           
│   └───PresentationFramework.Royale
│       │   PresentationFramework.Royale.csproj
│       │   
│       └───ref
│               PresentationFramework.Royale.csproj
│               
├───UIAutomation
│   ├───UIAutomationClient
│   │   │   UIAutomationClient.csproj
│   │   │   
│   │   └───ref
│   │           UIAutomationClient.csproj
│   │           
│   ├───UIAutomationClientSideProviders
│   │   │   UIAutomationClientSideProviders.csproj
│   │   │   
│   │   └───ref
│   │           UIAutomationClientSideProviders.csproj
│   │           
│   ├───UIAutomationProvider
│   │   │   UIAutomationProvider.csproj
│   │   │   
│   │   └───ref
│   │           UIAutomationProvider.csproj
│   │           
│   └───UIAutomationTypes
│       │   UIAutomationTypes.csproj
│       │   
│       └───ref
│               UIAutomationTypes.csproj
│               
├───WindowsBase
│       WindowsBase.csproj
│       
├───WindowsFormsIntegration
│   │   WindowsFormsIntegration.csproj
│   │   
│   └───ref
│           WindowsFormsIntegration.csproj
│           
└───WpfGfx
    ├───codegen
    │   └───mcg
    │           mcg.proj
    │           
    ├───common
    │   ├───DynamicCall
    │   │       DynamicCall.vcxproj
    │   │       
    │   ├───effects
    │   │       effects.vcxproj
    │   │       
    │   ├───scanop
    │   │       scanop.vcxproj
    │   │       
    │   └───shared
    │           shared.vcxproj
    │           
    ├───core
    │   ├───api
    │   │       api.vcxproj
    │   │       
    │   ├───av
    │   │       av.vcxproj
    │   │       
    │   ├───common
    │   │       common.vcxproj
    │   │       
    │   ├───control
    │   │   ├───dll
    │   │   │       milctrl.vcxproj
    │   │   │       
    │   │   └───util
    │   │           util.vcxproj
    │   │           
    │   ├───dll
    │   │       wpfgfx.vcxproj
    │   │       
    │   ├───fxjit
    │   │   ├───Collector
    │   │   │       Collector.vcxproj
    │   │   │       
    │   │   ├───Compiler
    │   │   │       Compiler.vcxproj
    │   │   │       
    │   │   ├───PixelShader
    │   │   │       PixelShader.vcxproj
    │   │   │       
    │   │   └───Platform
    │   │           Platform.vcxproj
    │   │           
    │   ├───geometry
    │   │       Geometry.vcxproj
    │   │       
    │   ├───glyph
    │   │       glyph.vcxproj
    │   │       
    │   ├───hw
    │   │   │   hw.vcxproj
    │   │   │   
    │   │   └───shaders
    │   │       └───ShaderGen
    │   │               shadergen.nativeproj
    │   │               
    │   ├───meta
    │   │       meta.vcxproj
    │   │       
    │   ├───resources
    │   │       resources.vcxproj
    │   │       
    │   ├───sw
    │   │       sw.vcxproj
    │   │       
    │   ├───targets
    │   │       targets.vcxproj
    │   │       
    │   └───uce
    │           uce.vcxproj
    │           
    ├───DbgXHelper
    │       DbgXHelper.vcxproj
    │       
    ├───exts
    │       exts.vcxproj
    │       
    ├───include
    │       GraphicsInclude.nativeproj
    │       
    ├───shared
    │   ├───debug
    │   │   ├───DebugDLL
    │   │   │       DebugDLL.nativeproj
    │   │   │       
    │   │   └───DebugLib
    │   │           DebugLib.vcxproj
    │   │           
    │   └───util
    │       ├───ConUtil
    │       │       ConUtil.nativeproj
    │       │       
    │       ├───DllUtil
    │       │       DllUtil.vcxproj
    │       │       
    │       ├───ExeUtil
    │       │       ExeUtil.nativeproj
    │       │       
    │       └───UtilLib
    │               UtilLib.vcxproj
    │               
    └───tools
        └───csp
                csp.csproj
                
```