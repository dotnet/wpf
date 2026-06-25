// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

global using System;
global using System.Collections.Generic;
global using System.Diagnostics;

global using DataFormatsCore = System.Private.Windows.Ole.DataFormatsCore<
    System.Windows.DataFormat>;
#pragma warning disable IDE0005 // Using directive is unnecessary.
global using DragDropHelper = System.Private.Windows.Ole.DragDropHelper<
    System.Windows.Ole.WpfOleServices,
    System.Windows.DataFormat>;
#pragma warning restore IDE0005 // Using directive is unnecessary.
global using ClipboardCore = System.Private.Windows.Ole.ClipboardCore<
    System.Windows.Ole.WpfOleServices>;
global using Composition = System.Private.Windows.Ole.Composition<
    System.Windows.Ole.WpfOleServices,
    System.Windows.Nrbf.WpfNrbfSerializer,
    System.Windows.DataFormat>;

global using SR = MS.Internal.PresentationCore.SR;

global using DllImport = MS.Internal.PresentationCore.DllImport;
