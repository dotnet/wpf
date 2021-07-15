// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Implementation of GeometryCollection
//

using System;
using MS.Internal;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Composition;
using System.Text.RegularExpressions;
using System.Windows.Media.Animation;
using System.Windows.Markup;
using System.Runtime.InteropServices;

namespace System.Windows.Media
{
    /// <summary>
    /// The class definition for GeometryCollection
    /// </summary>    
    public sealed partial class GeometryCollection : Animatable, IList, IList<Geometry>
    {
    }
}
