// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
//
//
// This file was generated, please do not edit it directly.
//
//---------------------------------------------------------------------------

using MS.Internal;
using MS.Internal.Collections;
using MS.Utility;
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
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Windows.Markup;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    /// <summary>
    /// ColorCollectionConverter - Converter class for converting instances of other types to and from ColorCollection instances
    /// </summary>
    public sealed class ColorCollectionConverter : TypeConverter
    {
        /// <summary>
        /// Returns true if this type converter can convert from a given type.
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext td, Type t)
        {
            if (t == typeof(string))
            {
                return true;
            }
            else
            {
                return base.CanConvertFrom(td, t);
            }
        }

        /// <summary>
        /// Returns true if this type converter can convert to the given type.
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>true if conversion is possible</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))     
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Attempts to convert to a ColorCollection from the given object.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// A NotSupportedException is thrown if the example object is null or is not a valid type
        /// which can be converted to a ColorCollection.
        /// </exception>
        public override object ConvertFrom(ITypeDescriptorContext td, CultureInfo cultureInfo, object value)
        {
            if (value == null)
            {
                throw GetConvertFromException(value);
            }

            String source = value as string;

            if (source != null)
            {
                return ColorCollection.Parse(source, cultureInfo);
            }
            else
            {
                return base.ConvertFrom(td, cultureInfo, value);
            }
        }

        /// <summary>
        /// TypeConverter method implementation that converts an object of type ColorCollection
        /// to an InstanceDescriptor or a string.  If the source is not a ColorCollection or
        /// the destination is not a string it forwards the call to
        /// TypeConverter.ConvertTo.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// A NotSupportedException is thrown if the example object is null
        /// or if the destinationType isn't one of the valid destination types.
        /// </exception>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="cultureInfo"> The CultureInfo which will govern this conversion. </param>
        /// <param name="value">value to convert from</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>converted value</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo cultureInfo, object value, Type destinationType)
        {
            if (destinationType != null && value is ColorCollection)
            {
                ColorCollection instance = (ColorCollection)value;

                if (destinationType == typeof(string))
                {
                    // Delegate to the IFormatProvider version of ToString.
                    return instance.ConvertToString(null, cultureInfo);
                }
            }

            // Pass unhandled cases to base class (which will throw exceptions for null value or destinationType.)
            return base.ConvertTo(context, cultureInfo, value, destinationType);
        }
    }
}
