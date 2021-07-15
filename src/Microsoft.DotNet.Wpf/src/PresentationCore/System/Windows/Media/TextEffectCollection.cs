// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  TextEffect class
//
//


using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Markup;

namespace System.Windows.Media
{
    /// <summary>
    /// Collection of TextEffect
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability=Readability.Unreadable)]
    public sealed partial class TextEffectCollection : Animatable, IList, IList<TextEffect>
    { 
    }
}
