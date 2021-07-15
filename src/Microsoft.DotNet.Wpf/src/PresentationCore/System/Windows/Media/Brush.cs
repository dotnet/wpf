// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: This file contains the implementation of Brush.
//              Brush is the abstract base class which describes how to fill 
//              a geometric area.
//
//

using MS.Internal;
using System;
using System.IO;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Markup;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media 
{
    /// <summary>
    /// Brush - 
    /// A brush is an object that represents a method to fill a plane.
    /// In addition to being able to fill a plane in an absolute way,
    /// Brushes are also able to adapt how they fill the plane to the
    /// size of the object that they are used to fill.
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability=Readability.Unreadable)]
    public abstract partial class Brush : Animatable, IFormattable
    {       
        #region Constructors
        
        /// <summary>
        /// Protected constructor for Brush.  
        /// Sets all values to their defaults.  
        /// To set property values, use the constructor which accepts paramters
        /// </summary>
        protected Brush()
        {
        }

        #endregion Constructors

        #region ToString

        /// <summary>
        /// Parse - this method is called by the type converter to parse a Brush's string 
        /// (provided in "value") with the given IFormatProvider.
        /// </summary>
        /// <returns>
        /// A Brush which was created by parsing the "value".
        /// </returns>
        /// <param name="value"> String representation of a Brush.  Cannot be null/empty. </param>
        /// <param name="context"> The ITypeDescriptorContext for this call. </param>
        internal static Brush Parse(string value, ITypeDescriptorContext context)
        {
            Brush brush;
            IFreezeFreezables freezer = null;
            if (context != null)
            {
                freezer = (IFreezeFreezables)context.GetService(typeof(IFreezeFreezables));
                if ((freezer != null) && freezer.FreezeFreezables)
                {
                    brush = (Brush)freezer.TryGetFreezable(value);
                    if (brush != null)
                    {
                        return brush;
                    }
                }
            }
            
            brush = Parsers.ParseBrush(value, System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS, context);
            
            if ((brush != null) && (freezer != null) && (freezer.FreezeFreezables))
            {
                freezer.TryFreeze(value, brush);
            }

            return brush;
        }

        /// <summary>
        /// Can serialze "this" to a string
        /// </summary>
        internal virtual bool CanSerializeToString()
        {
            return false;
        }

        #endregion
    }
}
