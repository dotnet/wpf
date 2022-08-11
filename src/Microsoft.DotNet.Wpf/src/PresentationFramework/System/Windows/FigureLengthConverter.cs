// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Figure length converter implementation
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
    /// FigureLengthConverter - Converter class for converting 
    /// instances of other types to and from FigureLength instances.
    /// </summary> 
    public class FigureLengthConverter: TypeConverter
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
        /// Attempts to convert to a FigureLength from the given object.
        /// </summary>
        /// <param name="typeDescriptorContext">The ITypeDescriptorContext for this call.</param>
        /// <param name="cultureInfo">The CultureInfo which is respected when converting.</param>
        /// <param name="source">The object to convert to a FigureLength.</param>
        /// <returns>
        /// The FigureLength instance which was constructed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException is thrown if the example object is not null 
        /// and is not a valid type which can be converted to a FigureLength.
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
                    return new FigureLength(Convert.ToDouble(source, cultureInfo)); //conversion from numeric type
                }
            }
            throw GetConvertFromException(source);
        }

        /// <summary>
        /// Attempts to convert a FigureLength instance to the given type.
        /// </summary>
        /// <param name="typeDescriptorContext">The ITypeDescriptorContext for this call.</param>
        /// <param name="cultureInfo">The CultureInfo which is respected when converting.</param>
        /// <param name="value">The FigureLength to convert.</param>
        /// <param name="destinationType">The type to which to convert the FigureLength instance.</param>
        /// <returns>
        /// The object which was constructed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException is thrown if the object is not null and is not a FigureLength,
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
                &&  value is FigureLength )
            {
                FigureLength fl = (FigureLength)value;

                if (destinationType == typeof(string)) 
                { 
                    return (ToString(fl, cultureInfo)); 
                }

                if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo ci = typeof(FigureLength).GetConstructor(new Type[] { typeof(double), typeof(FigureUnitType) });
                    return (new InstanceDescriptor(ci, new object[] { fl.Value, fl.FigureUnitType }));
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
        /// Converts a FigureLength instance to a String given the CultureInfo.
        /// </summary>
        /// <param name="fl">FigureLength instance to convert.</param>
        /// <param name="cultureInfo">Culture Info.</param>
        /// <returns>String representation of the object.</returns>
        static internal string ToString(FigureLength fl, CultureInfo cultureInfo)
        {
            switch (fl.FigureUnitType)
            {
                //  for Auto print out "Auto". value is always "1.0"
                case FigureUnitType.Auto:
                    return ("Auto");

                case FigureUnitType.Pixel:
                    return Convert.ToString(fl.Value, cultureInfo);

                default:
                    return Convert.ToString(fl.Value, cultureInfo) + " " + fl.FigureUnitType.ToString();
            }
        }

        /// <summary>
        /// Parses a FigureLength from a string given the CultureInfo.
        /// </summary>
        /// <param name="s">String to parse from.</param>
        /// <param name="cultureInfo">Culture Info.</param>
        /// <returns>Newly created FigureLength instance.</returns>
        /// <remarks>
        /// Formats: 
        /// "[value][unit]"
        ///     [value] is a double
        ///     [unit] is a string in FigureLength._unitTypes connected to a FigureUnitType
        /// "[value]"
        ///     As above, but the FigureUnitType is assumed to be FigureUnitType.Pixel
        /// "[unit]"
        ///     As above, but the value is assumed to be 1.0
        ///     This is only acceptable for a subset of FigureUnitType: Auto
        /// </remarks>
        static internal FigureLength FromString(string s, CultureInfo cultureInfo)
        {
            double value;
            FigureUnitType unit;
            XamlFigureLengthSerializer.FromString(s, cultureInfo,
                out value, out unit);

            return (new FigureLength(value, unit));
        }

        #endregion Internal Methods
    }
}
