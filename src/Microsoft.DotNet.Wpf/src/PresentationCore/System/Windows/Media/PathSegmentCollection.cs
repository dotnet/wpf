// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using MS.Internal;
using MS.Internal.PresentationCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ComponentModel.Design.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Markup;

namespace System.Windows.Media
{
    /// <summary>
    /// The class definition for PathSegmentCollection
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public sealed partial class PathSegmentCollection : Animatable, IList, IList<PathSegment>
    {
        /// <summary>
        /// Can serialze "this" to a string.
        /// This is true iff every segment is stroked.
        /// </summary>
        internal bool CanSerializeToString()
        {
            bool canSerialize = true;

            for (int i=0; i<_collection.Count; i++)
            { 
                if (!_collection[i].IsStroked)
                {
                    canSerialize = false;
                    break;
                }
            }

            return canSerialize;
        }

        /// <summary>
        /// Creates a string representation of this object based on the format string 
        /// and IFormatProvider passed in.  
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        internal string ConvertToString(string format, IFormatProvider provider)
        {
            if (_collection.Count == 0)
            {
                return String.Empty;
            }

            StringBuilder str = new StringBuilder();
            
            for (int i=0; i<_collection.Count; i++)
            { 
                str.Append(_collection[i].ConvertToString(format, provider));
            }

            return str.ToString();
        }
    }
}

