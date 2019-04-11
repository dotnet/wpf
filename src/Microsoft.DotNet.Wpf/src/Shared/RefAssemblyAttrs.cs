// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// The C++ version of these definitions is in inc\BuildInfo.hxx.

using System;
using System.Text;

#if WINDOWS_BASE
namespace MS.Internal.WindowsBase
#elif PRESENTATION_CORE
namespace MS.Internal.PresentationCore
#elif PRESENTATIONFRAMEWORK
namespace MS.Internal.PresentationFramework
#elif REACHFRAMEWORK
namespace MS.Internal.ReachFramework
#elif UIAUTOMATIONTYPES
namespace MS.Internal.UIAutomationTypes
#elif DIRECTWRITE_FORWARDER
namespace MS.Internal.DirectWriteForwarder
#else
namespace Microsoft.Internal
#endif
{
    internal static class BuildInfo
    {
        internal const string WCP_PUBLIC_KEY_TOKEN = "31bf3856ad364e35";
        internal const string WCP_VERSION = "4.0.0.0";
        internal const string WCP_PUBLIC_KEY_STRING = "0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9";
        internal const string WCP_VERSION_SUFFIX = "_cor3";
        internal const string MIL_VERSION_SUFFIX = "";

        // Constants to prevent hardcoding in InternalsVisibleTo attribute
        internal const string DirectWriteForwarder = "DirectWriteForwarder, PublicKey=" + WCP_PUBLIC_KEY_STRING;
        internal const string PresentationCore = "PresentationCore, PublicKey="+ WCP_PUBLIC_KEY_STRING;
        internal const string PresentationCFFRasterizer = "PresentationCFFRasterizer, PublicKey="+ WCP_PUBLIC_KEY_STRING;
        internal const string PresentationFramework = "PresentationFramework, PublicKey="+ WCP_PUBLIC_KEY_STRING;
        internal const string PresentationUI = "PresentationUI, PublicKey="+ WCP_PUBLIC_KEY_STRING;
        internal const string PresentationFrameworkLuna = "PresentationFramework.Luna, PublicKey="+ WCP_PUBLIC_KEY_STRING;
        internal const string PresentationFrameworkRoyale = "PresentationFramework.Royale, PublicKey="+ WCP_PUBLIC_KEY_STRING;
        internal const string PresentationFrameworkAero = "PresentationFramework.Aero, PublicKey="+ WCP_PUBLIC_KEY_STRING;
        internal const string PresentationFrameworkAero2 = "PresentationFramework.Aero2, PublicKey="+ WCP_PUBLIC_KEY_STRING;
        internal const string PresentationFrameworkAeroLite = "PresentationFramework.AeroLite, PublicKey="+ WCP_PUBLIC_KEY_STRING;
        internal const string PresentationFrameworkClassic = "PresentationFramework.Classic, PublicKey="+ WCP_PUBLIC_KEY_STRING;
        internal const string PresentationFrameworkSystemCore = "PresentationFramework-SystemCore, PublicKey="+ WCP_PUBLIC_KEY_STRING;
        internal const string PresentationFrameworkSystemData = "PresentationFramework-SystemData, PublicKey="+ WCP_PUBLIC_KEY_STRING;
        internal const string PresentationFrameworkSystemDrawing = "PresentationFramework-SystemDrawing, PublicKey="+ WCP_PUBLIC_KEY_STRING;
        internal const string PresentationFrameworkSystemXml = "PresentationFramework-SystemXml, PublicKey="+ WCP_PUBLIC_KEY_STRING;
        internal const string PresentationFrameworkSystemXmlLinq = "PresentationFramework-SystemXmlLinq, PublicKey="+ WCP_PUBLIC_KEY_STRING;
        internal const string ReachFramework = "ReachFramework, PublicKey="+ WCP_PUBLIC_KEY_STRING;
        internal const string SystemPrinting = "System.Printing, PublicKey="+ WCP_PUBLIC_KEY_STRING;
        internal const string SystemXaml = "System.Xaml, PublicKey=" + WCP_PUBLIC_KEY_STRING;
        internal const string WindowsFormsIntegration = "WindowsFormsIntegration, PublicKey="+ WCP_PUBLIC_KEY_STRING;
        internal const string SystemWindowsPresentation = "System.Windows.Presentation, PublicKey=" + WCP_PUBLIC_KEY_STRING;
        internal const string SystemWindowsControlsRibbon = "System.Windows.Controls.Ribbon, PublicKey=" + WCP_PUBLIC_KEY_STRING;
    }

    internal static class DllImport
    {
        internal const string PresentationNative = "PresentationNative" + BuildInfo.WCP_VERSION_SUFFIX + ".dll";
        internal const string PresentationCFFRasterizerNative = "PresentationCFFRasterizerNative" + BuildInfo.WCP_VERSION_SUFFIX + ".dll";
        internal const string MilCore = "wpfgfx" + BuildInfo.WCP_VERSION_SUFFIX + ".dll";

        // DLL's w/o version suffix
        internal const string UIAutomationCore = "UIAutomationCore.dll";
        internal const string Wininet = "Wininet.dll";
        internal const string WindowsCodecs = "WindowsCodecs.dll";
        internal const string WindowsCodecsExt = "WindowsCodecsExt.dll";
        internal const string Mscms = "mscms.dll";
        internal const string PrntvPt = "prntvpt.dll";
        internal const string Ole32 = "ole32.dll";
        internal const string User32 = "user32.dll";
        internal const string NInput = "ninput.dll";
    }
}


