// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This file specifies type-forwarding attributes, used by both the
// real assembly and the reference assembly.
//

using System;
using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(System.Windows.Automation.Provider.IRawElementProviderSimple))]
[assembly: TypeForwardedTo(typeof(System.Windows.Automation.Provider.ITextRangeProvider))]
[assembly: TypeForwardedTo(typeof(System.Windows.Automation.Provider.ProviderOptions))]
