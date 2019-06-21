// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Grid length converter implementation
//
//              See spec at http://avalon/layout/Specs/Star%20LengthUnit.mht
//
//

using MS.Internal;
using MS.Utility;
using System.ComponentModel;
using System.Windows;
using System;
using System.Security;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows.Markup;

namespace System.Windows
{
    /// <summary>
    /// GridLengthConverter - Converter class for converting 
    /// instances of other types to and from GridLength instances.
    /// </summary> 
    public class GridLengthConverter: TypeConverter
    {
        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Checks whether or not this class can convert from a given type.
        /// </summary>
        /// <param name="typeDescriptorContext">The ITypeDescriptorContext 
        /// for this call.</param>
        /// <param name="sourceType">The Type being queried for support.</param>
        /// <returns>
        /// <c>true</c> if thie converter can convert from the provided type, 
        /// <c>false</c> otherwise.
        /// </returns>
        public override bool CanConvertFrom(
            ITypeDescriptorContext typeDescriptorContext, 
            Type sourceType)
        {
            // We can only handle strings, integral and floating types
            TypeCode tc = Type.GetTypeCode(sourceType);
            switch (tc)
            {
                case TypeCode.String:
                case TypeCode.Decimal:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default: 
                    return false;
            }
        }

        /// <summary>
        /// Checks whether or not this class can convert to a given type.
        /// </summary>
        /// <param name="typeDescriptorContext">The ITypeDescriptorContext 
        /// for this call.</param>
        /// <param name="destinationType">The Type being queried for support.</param>
        /// <returns>
        /// <c>true</c> if this converter can convert to the provided type, 
        /// <c>false</c> otherwise.
        /// </returns>
        public override bool CanConvertTo(
            ITypeDescriptorContext typeDescriptorContext, 
            Type destinationType) 
        {
            return (    destinationType == typeof(InstanceDescriptor) 
                    ||  destinationType == typeof(string)   );
        }

        /// <summary>
        /// Attempts to convert to a GridLength from the given object.
        /// </summary>
        /// <param name="typeDescriptorContext">The ITypeDescriptorContext for this call.</param>
        /// <param name="cultureInfo">The CultureInfo which is respected when converting.</param>
        /// <param name="source">The object to convert to a GridLength.</param>
        /// <returns>
        /// The GridLength instance which was constructed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException is thrown if the example object is not null 
        /// and is not a valid type which can be converted to a GridLength.
        /// </exception>
        public override object ConvertFrom(
            ITypeDescriptorContext typeDescriptorContext, 
            CultureInfo cultureInfo, 
            object source)
        {
            if (source != null)
            {
                if (source is string)
                {
                    return (FromString((string)source, cultureInfo));
                }
                else
                {
                    //  conversion from numeric type
                    double value;
                    GridUnitType type;

                    value = Convert.ToDouble(source, cultureInfo);

                    if (DoubleUtil.IsNaN(value))
                    {
                        //  this allows for conversion from Width / Height = "Auto" 
                        value = 1.0;
                        type = GridUnitType.Auto;
                    }
                    else
                    {
                        type = GridUnitType.Pixel;
                    }

                    return new GridLength(value, type);
                }
            }
            throw GetConvertFromException(source);
        }

        /// <summary>
        /// Attempts to convert a GridLength instance to the given type.
        /// </summary>
        /// <param name="typeDescriptorContext">The ITypeDescriptorContext for this call.</param>
        /// <param name="cultureInfo">The CultureInfo which is respected when converting.</param>
        /// <param name="value">The GridLength to convert.</param>
        /// <param name="destinationType">The type to which to convert the GridLength instance.</param>
        /// <returns>
        /// The object which was constructed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException is thrown if the object is not null and is not a GridLength,
        /// or if the destinationType isn't one of the valid destination types.
        /// </exception>
        public override object ConvertTo(
            ITypeDescriptorContext typeDescriptorContext, 
            CultureInfo cultureInfo,
            object value,
            Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }

            if (    value != null
                &&  value is GridLength )
            {
                GridLength gl = (GridLength)value;

                if (destinationType == typeof(string)) 
                { 
                    return (ToString(gl, cultureInfo)); 
                }

                if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo ci = typeof(GridLength).GetConstructor(new Type[] { typeof(double), typeof(GridUnitType) });
                    return (new InstanceDescriptor(ci, new object[] { gl.Value, gl.GridUnitType }));
                }
            }
            throw GetConvertToException(value, destinationType);
        }

        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Converts a GridLength instance to a String given the CultureInfo.
        /// </summary>
        /// <param name="gl">GridLength instance to convert.</param>
        /// <param name="cultureInfo">Culture Info.</param>
        /// <returns>String representation of the object.</returns>
        static internal string ToString(GridLength gl, CultureInfo cultureInfo)
        {
            switch (gl.GridUnitType)
            {
                //  for Auto print out "Auto". value is always "1.0"
                case (GridUnitType.Auto):
                    return ("Auto");

                //  Star has one special case when value is "1.0".
                //  in this case drop value part and print only "Star"
                case (GridUnitType.Star):
                    return (
                        DoubleUtil.IsOne(gl.Value)
                        ? "*"
                        : Convert.ToString(gl.Value, cultureInfo) + "*");

                //  for Pixel print out the numeric value. "px" can be omitted.
                default:
                    return (Convert.ToString(gl.Value, cultureInfo));

            }
        }

        /// <summary>
        /// Parses a GridLength from a string given the CultureInfo.
        /// </summary>
        /// <param name="s">String to parse from.</param>
        /// <param name="cultureInfo">Culture Info.</param>
        /// <returns>Newly created GridLength instance.</returns>
        /// <remarks>
        /// Formats: 
        /// "[value][unit]"
        ///     [value] is a double
        ///     [unit] is a string in GridLength._unitTypes connected to a GridUnitType
        /// "[value]"
        ///     As above, but the GridUnitType is assumed to be GridUnitType.Pixel
        /// "[unit]"
        ///     As above, but the value is assumed to be 1.0
        ///     This is only acceptable for a subset of GridUnitType: Auto
        /// </remarks>
        static internal GridLength FromString(string s, CultureInfo cultureInfo)
        {
            double value;
            GridUnitType unit;
            XamlGridLengthSerializer.FromString(s, cultureInfo,
                out value, out unit);

            return (new GridLength(value, unit));
        }

        #endregion Internal Methods

    }
}
