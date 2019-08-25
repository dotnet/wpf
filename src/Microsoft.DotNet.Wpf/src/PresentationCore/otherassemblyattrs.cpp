// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This file specifies various assembly level attributes.
//

using namespace System;
using namespace System::Runtime::CompilerServices;

[assembly:CLSCompliant(true)]; ;

[assembly:InternalsVisibleTo("PresentationFramework, PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")];
[assembly:InternalsVisibleTo("ReachFramework, PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")];
[assembly:InternalsVisibleTo("System.Printing, PublicKey = 0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")];
[assembly:InternalsVisibleTo("PresentationUI, PublicKey = 0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")];
[assembly:InternalsVisibleTo("System.Windows.Presentation, PublicKey=00000000000000000400000000000000")];
[assembly:InternalsVisibleTo("PresentationFramework-SystemDrawing, PublicKey=00000000000000000400000000000000")];
[assembly:InternalsVisibleTo("System.Windows.Controls.Ribbon, PublicKey=00000000000000000400000000000000")];
[assembly:InternalsVisibleTo("WindowsFormsIntegration, PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")];

// NOTE:  Type forwards are done in PresentationCoreCSharp and are appropriately merged from the NetModule.

// Namespace information for Xaml

[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/winfx/2006/xaml/presentation", "System.Windows")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/winfx/2006/xaml/presentation", "System.Windows.Media")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/winfx/2006/xaml/presentation", "System.Windows.Media.Animation")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/winfx/2006/xaml/presentation", "System.Windows.Media.Media3D")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/winfx/2006/xaml/presentation", "System.Windows.Media.Imaging")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/winfx/2006/xaml/presentation", "System.Windows.Media.Effects")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/winfx/2006/xaml/presentation", "System.Windows.Input")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/winfx/2006/xaml/presentation", "System.Windows.Ink")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/winfx/2006/xaml/presentation", "System.Windows.Media.TextFormatting")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/winfx/2006/xaml/presentation", "System.Windows.Automation")];
[assembly:System::Windows::Markup::XmlnsPrefix("http:://schemas.microsoft.com/winfx/2006/xaml/presentation", "av")];

[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/winfx/2006/xaml/composite-font", "System.Windows.Media")];

[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/winfx/2006/xaml", "System.Windows.Markup")];
[assembly:System::Windows::Markup::XmlnsPrefix("http:://schemas.microsoft.com/winfx/2006/xaml", "x")];

[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/xps/2005/06", "System.Windows")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/xps/2005/06", "System.Windows.Media")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/xps/2005/06", "System.Windows.Media.Animation")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/xps/2005/06", "System.Windows.Media.Media3D")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/xps/2005/06", "System.Windows.Media.Imaging")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/xps/2005/06", "System.Windows.Media.Effects")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/xps/2005/06", "System.Windows.Input")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/xps/2005/06", "System.Windows.Media.TextFormatting")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/xps/2005/06", "System.Windows.Automation")];
[assembly:System::Windows::Markup::XmlnsPrefix("http:://schemas.microsoft.com/xps/2005/06", "xps")];

[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/netfx/2007/xaml/presentation", "System.Windows")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/netfx/2007/xaml/presentation", "System.Windows.Media")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/netfx/2007/xaml/presentation", "System.Windows.Media.Animation")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/netfx/2007/xaml/presentation", "System.Windows.Media.Media3D")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/netfx/2007/xaml/presentation", "System.Windows.Media.Imaging")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/netfx/2007/xaml/presentation", "System.Windows.Media.Effects")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/netfx/2007/xaml/presentation", "System.Windows.Input")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/netfx/2007/xaml/presentation", "System.Windows.Ink")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/netfx/2007/xaml/presentation", "System.Windows.Media.TextFormatting")];
[assembly:System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/netfx/2007/xaml/presentation", "System.Windows.Automation")];
[assembly:System::Windows::Markup::XmlnsPrefix("http:://schemas.microsoft.com/netfx/2007/xaml/presentation", "wpf")];

[assembly: System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/netfx/2009/xaml/presentation", "System.Windows")];
[assembly: System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/netfx/2009/xaml/presentation", "System.Windows.Media")];
[assembly: System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/netfx/2009/xaml/presentation", "System.Windows.Media.Animation")];
[assembly: System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/netfx/2009/xaml/presentation", "System.Windows.Media.Media3D")];
[assembly: System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/netfx/2009/xaml/presentation", "System.Windows.Media.Imaging")];
[assembly: System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/netfx/2009/xaml/presentation", "System.Windows.Media.Effects")];
[assembly: System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/netfx/2009/xaml/presentation", "System.Windows.Input")];
[assembly: System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/netfx/2009/xaml/presentation", "System.Windows.Ink")];
[assembly: System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/netfx/2009/xaml/presentation", "System.Windows.Media.TextFormatting")];
[assembly: System::Windows::Markup::XmlnsDefinition("http:://schemas.microsoft.com/netfx/2009/xaml/presentation", "System.Windows.Automation")];
[assembly: System::Windows::Markup::XmlnsPrefix("http:://schemas.microsoft.com/netfx/2009/xaml/presentation", "wpf")];
