// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This file specifies various assembly level attributes.
//

using System;
using MS.Internal.WindowsBase;
using System.Resources;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Security;
using System.Windows.Markup;


[assembly:DependencyAttribute("System,", LoadHint.Always)]
[assembly:DependencyAttribute("System.Xaml,", LoadHint.Sometimes)]

[assembly:InternalsVisibleTo(BuildInfo.DirectWriteForwarder)]
[assembly:InternalsVisibleTo(BuildInfo.PresentationCore)]
[assembly:InternalsVisibleTo(BuildInfo.PresentationFramework)]
[assembly:InternalsVisibleTo(BuildInfo.PresentationUI)]
[assembly:InternalsVisibleTo(BuildInfo.PresentationFrameworkRoyale)]
[assembly:InternalsVisibleTo(BuildInfo.PresentationFrameworkLuna)]
[assembly:InternalsVisibleTo(BuildInfo.PresentationFrameworkAero)]
[assembly:InternalsVisibleTo(BuildInfo.PresentationFrameworkAero2)]
[assembly:InternalsVisibleTo(BuildInfo.PresentationFrameworkAeroLite)]
[assembly:InternalsVisibleTo(BuildInfo.PresentationFrameworkClassic)]
[assembly:InternalsVisibleTo(BuildInfo.ReachFramework)]
[assembly:InternalsVisibleTo(BuildInfo.SystemWindowsPresentation)]
[assembly:InternalsVisibleTo(BuildInfo.PresentationFrameworkSystemCore)]
[assembly:InternalsVisibleTo(BuildInfo.PresentationFrameworkSystemData)]
[assembly:InternalsVisibleTo(BuildInfo.PresentationFrameworkSystemDrawing)]
[assembly:InternalsVisibleTo(BuildInfo.PresentationFrameworkSystemXml)]
[assembly:InternalsVisibleTo(BuildInfo.PresentationFrameworkSystemXmlLinq)]

[assembly:TypeForwardedTo(typeof(System.Windows.Markup.ValueSerializer))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.ArrayExtension))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.DateTimeValueSerializer))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.IComponentConnector))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.INameScope))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.IProvideValueTarget))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.IUriContext))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.IValueSerializerContext))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.IXamlTypeResolver))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.MarkupExtension))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.NullExtension))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.StaticExtension))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.TypeExtension))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.AmbientAttribute))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.UsableDuringInitializationAttribute))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.ConstructorArgumentAttribute))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.ContentPropertyAttribute))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.ContentWrapperAttribute))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.DependsOnAttribute))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.DictionaryKeyPropertyAttribute))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.MarkupExtensionReturnTypeAttribute))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.NameScopePropertyAttribute))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.RootNamespaceAttribute))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.TrimSurroundingWhitespaceAttribute))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.UidPropertyAttribute))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.ValueSerializerAttribute))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.WhitespaceSignificantCollectionAttribute))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.XmlLangPropertyAttribute))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.XmlnsCompatibleWithAttribute))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.XmlnsDefinitionAttribute))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.XmlnsPrefixAttribute))]
[assembly:TypeForwardedTo(typeof(System.Windows.Markup.RuntimeNamePropertyAttribute))]

[assembly:TypeForwardedTo(typeof(System.IO.FileFormatException))]
[assembly:TypeForwardedTo(typeof(System.IO.Packaging.Package))]
[assembly:TypeForwardedTo(typeof(System.IO.Packaging.PackagePart))] 
[assembly:TypeForwardedTo(typeof(System.IO.Packaging.PackageProperties))] 
[assembly:TypeForwardedTo(typeof(System.IO.Packaging.PackagePartCollection))] 
[assembly:TypeForwardedTo(typeof(System.IO.Packaging.TargetMode))] 
[assembly:TypeForwardedTo(typeof(System.IO.Packaging.PackageRelationship))] 
[assembly:TypeForwardedTo(typeof(System.IO.Packaging.PackageRelationshipCollection))] 
[assembly:TypeForwardedTo(typeof(System.IO.Packaging.PackUriHelper))] 
[assembly:TypeForwardedTo(typeof(System.IO.Packaging.ZipPackage))] 
[assembly:TypeForwardedTo(typeof(System.IO.Packaging.ZipPackagePart))] 
[assembly:TypeForwardedTo(typeof(System.IO.Packaging.CompressionOption))] 
[assembly:TypeForwardedTo(typeof(System.IO.Packaging.EncryptionOption))] 
[assembly:TypeForwardedTo(typeof(System.IO.Packaging.PackageRelationshipSelector))] 
[assembly:TypeForwardedTo(typeof(System.IO.Packaging.PackageRelationshipSelectorType))]

[assembly: TypeForwardedTo(typeof(System.Security.Permissions.MediaPermissionAudio))]
[assembly: TypeForwardedTo(typeof(System.Security.Permissions.MediaPermissionVideo))]
[assembly: TypeForwardedTo(typeof(System.Security.Permissions.MediaPermissionImage))]
[assembly: TypeForwardedTo(typeof(System.Security.Permissions.MediaPermission))]
[assembly: TypeForwardedTo(typeof(System.Security.Permissions.MediaPermissionAttribute))]
[assembly: TypeForwardedTo(typeof(System.Security.Permissions.WebBrowserPermissionLevel))]
[assembly: TypeForwardedTo(typeof(System.Security.Permissions.WebBrowserPermission))]
[assembly: TypeForwardedTo(typeof(System.Security.Permissions.WebBrowserPermissionAttribute))]

[assembly: TypeForwardedTo(typeof(System.Collections.ObjectModel.ReadOnlyObservableCollection<>))]
[assembly: TypeForwardedTo(typeof(System.Collections.ObjectModel.ObservableCollection<>))]
[assembly: TypeForwardedTo(typeof(System.Collections.Specialized.NotifyCollectionChangedAction))]
[assembly: TypeForwardedTo(typeof(System.Collections.Specialized.NotifyCollectionChangedEventArgs))]
[assembly: TypeForwardedTo(typeof(System.Collections.Specialized.NotifyCollectionChangedEventHandler))]
[assembly: TypeForwardedTo(typeof(System.Collections.Specialized.INotifyCollectionChanged))]

// XAML namespace definitions
[assembly:System.Windows.Markup.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "System.Windows")]
[assembly:System.Windows.Markup.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "System.Windows.Input")]
[assembly:System.Windows.Markup.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "System.Windows.Media")]
[assembly:System.Windows.Markup.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "System.Diagnostics")]
[assembly:System.Windows.Markup.XmlnsPrefix    ("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "av")]

[assembly:System.Windows.Markup.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml", "System.Windows.Markup")]

[assembly:System.Windows.Markup.XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/composite-font", "System.Windows.Media")]

[assembly:System.Windows.Markup.XmlnsDefinition("http://schemas.microsoft.com/netfx/2007/xaml/presentation", "System.Windows")]
[assembly:System.Windows.Markup.XmlnsDefinition("http://schemas.microsoft.com/netfx/2007/xaml/presentation", "System.Windows.Input")]
[assembly:System.Windows.Markup.XmlnsDefinition("http://schemas.microsoft.com/netfx/2007/xaml/presentation", "System.Windows.Media")]
[assembly:System.Windows.Markup.XmlnsDefinition("http://schemas.microsoft.com/netfx/2007/xaml/presentation", "System.Diagnostics")]
[assembly:System.Windows.Markup.XmlnsPrefix    ("http://schemas.microsoft.com/netfx/2007/xaml/presentation", "wpf")]

[assembly: System.Windows.Markup.XmlnsDefinition("http://schemas.microsoft.com/netfx/2009/xaml/presentation", "System.Windows")]
[assembly: System.Windows.Markup.XmlnsDefinition("http://schemas.microsoft.com/netfx/2009/xaml/presentation", "System.Windows.Input")]
[assembly: System.Windows.Markup.XmlnsDefinition("http://schemas.microsoft.com/netfx/2009/xaml/presentation", "System.Windows.Media")]
[assembly: System.Windows.Markup.XmlnsDefinition("http://schemas.microsoft.com/netfx/2009/xaml/presentation", "System.Diagnostics")]
[assembly: System.Windows.Markup.XmlnsPrefix("http://schemas.microsoft.com/netfx/2009/xaml/presentation", "wpf")]

[assembly:System.Windows.Markup.XmlnsDefinition("http://schemas.microsoft.com/xps/2005/06", "System.Windows")]
[assembly:System.Windows.Markup.XmlnsDefinition("http://schemas.microsoft.com/xps/2005/06", "System.Windows.Input")]
[assembly:System.Windows.Markup.XmlnsDefinition("http://schemas.microsoft.com/xps/2005/06", "System.Windows.Media")]
[assembly:System.Windows.Markup.XmlnsPrefix    ("http://schemas.microsoft.com/xps/2005/06", "metro")]

