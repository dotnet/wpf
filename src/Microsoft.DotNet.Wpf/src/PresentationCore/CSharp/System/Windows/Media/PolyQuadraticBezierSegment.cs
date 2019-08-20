// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using MS.Internal;
using MS.Internal.PresentationCore;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Collections;
using System.Text;
using System.Globalization;
using System.Windows.Media;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Diagnostics;
using System.Security;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    /// <summary>
    /// PolyQuadraticBezierSegment
    /// </summary>
    public sealed partial class PolyQuadraticBezierSegment : PathSegment
    {
        /// <summary>
        /// Creates a string representation of this object based on the format string 
        /// and IFormatProvider passed in.  
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        internal override string ConvertToString(string format, IFormatProvider provider)
        {
            return (Points != null) ? "Q" + Points.ConvertToString(format, provider) : "";
        }
    }
}

