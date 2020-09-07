// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This file specifies various assembly level attributes.
//

using System.Runtime.CompilerServices;
using System.Windows.Markup;

[assembly:Dependency("mscorlib,", LoadHint.Always)]
[assembly:Dependency("System,", LoadHint.Always)]
[assembly:Dependency("System.Xml,", LoadHint.Sometimes)]

[assembly: TypeForwardedTo(typeof(System.Xaml.Permissions.XamlAccessLevel))]
#pragma warning disable SYSLIB0003 // Type or member is obsolete
[assembly: TypeForwardedTo(typeof(System.Xaml.Permissions.XamlLoadPermission))]
#pragma warning restore SYSLIB0003 // Type or member is obsolete
[assembly: TypeForwardedTo(typeof(System.Windows.Markup.ValueSerializerAttribute))]

[assembly:XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml", "System.Windows.Markup")]
