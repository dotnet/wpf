// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file specifies various assembly level attributes.


using System.Reflection;
using System.Windows;
using System.Windows.Markup;

[assembly:ThemeInfo(
    // Specifies the location of theme specific resources
    ResourceDictionaryLocation.SourceAssembly,
    // Specifies the location of non-theme specific resources:
    ResourceDictionaryLocation.SourceAssembly)]

[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "System.Windows.Controls.Ribbon")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "System.Windows.Controls")]
