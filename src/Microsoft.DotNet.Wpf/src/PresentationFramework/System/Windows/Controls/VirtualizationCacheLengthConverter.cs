// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Virtualization cache length converter implementation
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
using System.Text;

namespace System.Windows.Controls
{
    /// <summary>
    /// VirtualizationCacheLengthConverter - Converter class for converting
    /// instances of other types to and from VirtualizationCacheLength instances.
    /// </summary>
    public class VirtualizationCacheLengthConverter : TypeConverter
    {
        #region Public Methods

        /// <summary>
        /// CanConvertFrom - Returns whether or not this class can convert from a given type.
        /// </summary>
        /// <returns>
        /// bool - True if thie converter can convert from the provided type, false if not.
        /// </returns>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="sourceType"> The Type being queried for support. </param>
        public override bool CanConvertFrom(ITypeDescriptorContext typeDescriptorContext, Type sourceType)
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
        /// CanConvertTo - Returns whether or not this class can convert to a given type.
        /// </summary>
        /// <returns>
        /// bool - True if this converter can convert to the provided type, false if not.
        /// </returns>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="destinationType"> The Type being queried for support. </param>
        public override bool CanConvertTo(ITypeDescriptorContext typeDescriptorContext, Type destinationType)
        {
            // We can convert to an InstanceDescriptor or to a string.
            if (    destinationType == typeof(InstanceDescriptor)
                ||  destinationType == typeof(string))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// ConvertFrom - Attempt to convert to a VirtualizationCacheLength from the given object
        /// </summary>
        /// <returns>
        /// The VirtualizationCacheLength which was constructed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException is thrown if the example object is not null and is not a valid type
        /// which can be converted to a VirtualizationCacheLength.
        /// </exception>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="cultureInfo"> The CultureInfo which is respected when converting. </param>
        /// <param name="source"> The object to convert to a VirtualizationCacheLength. </param>
        public override object ConvertFrom(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo, object source)
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
                    double value = Convert.ToDouble(source, cultureInfo);
                    return new VirtualizationCacheLength(value);
                }
            }
            throw GetConvertFromException(source);
        }

        /// <summary>
        /// ConvertTo - Attempt to convert a VirtualizationCacheLength to the given type
        /// </summary>
        /// <returns>
        /// The object which was constructoed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException is thrown if the object is not null and is not a VirtualizationCacheLength,
        /// or if the destinationType isn't one of the valid destination types.
        /// </exception>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="cultureInfo"> The CultureInfo which is respected when converting. </param>
        /// <param name="value"> The VirtualizationCacheLength to convert. </param>
        /// <param name="destinationType">The type to which to convert the VirtualizationCacheLength instance. </param>
        public override object ConvertTo(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }

            if (value != null
                && value is VirtualizationCacheLength)
            {
                VirtualizationCacheLength gl = (VirtualizationCacheLength)value;

                if (destinationType == typeof(string))
                {
                    return (ToString(gl, cultureInfo));
                }

                if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo ci = typeof(VirtualizationCacheLength).GetConstructor(new Type[] { typeof(double), typeof(VirtualizationCacheLengthUnit) });
                    return (new InstanceDescriptor(ci, new object[] { gl.CacheBeforeViewport, gl.CacheAfterViewport }));
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
        /// Creates a string from a VirtualizationCacheLength given the CultureInfo.
        /// </summary>
        /// <param name="cacheLength">VirtualizationCacheLength.</param>
        /// <param name="cultureInfo">Culture Info.</param>
        /// <returns>Newly created string instance.</returns>
        static internal string ToString(VirtualizationCacheLength cacheLength, CultureInfo cultureInfo)
        {
            char listSeparator = TokenizerHelper.GetNumericListSeparator(cultureInfo);

            // Initial capacity [64] is an estimate based on a sum of:
            // 24 = 2x double (twelve digits is generous for the range of values likely)
            //  2 = 2x separator characters
            // Is 26 really a good number??? 
            StringBuilder sb = new StringBuilder(26);

            sb.Append(cacheLength.CacheBeforeViewport.ToString(cultureInfo));
            sb.Append(listSeparator);
            sb.Append(cacheLength.CacheAfterViewport.ToString(cultureInfo));
            return sb.ToString();
        }
        /// <summary>
        /// Parses a VirtualizationCacheLength from a string given the CultureInfo.
        /// </summary>
        /// <param name="s">String to parse from.</param>
        /// <param name="cultureInfo">Culture Info.</param>
        /// <returns>Newly created VirtualizationCacheLength instance.</returns>
        static internal VirtualizationCacheLength FromString(string s, CultureInfo cultureInfo)
        {
            TokenizerHelper th = new TokenizerHelper(s, cultureInfo);
            double[] lengths = new double[2];
            int i = 0;

            // Peel off each double in the delimited list.
            while (th.NextToken())
            {
                if (i >= 2)
                {
                    i = 3;    // Set i to a bad value.
                    break;
                }

                lengths[i] = Double.Parse(th.GetCurrentToken(), cultureInfo);
                i++;
            }

            // We have a reasonable interpreation for one value (all four edges), two values (horizontal, vertical),
            // and four values (left, top, right, bottom).
            switch (i)
            {
                case 1:
                    return new VirtualizationCacheLength(lengths[0]);

                case 2:
                    // Should allowInfinity be false ??? (Rob)
                    return new VirtualizationCacheLength(lengths[0], lengths[1]);
            }

            throw new FormatException(SR.Get(SRID.InvalidStringVirtualizationCacheLength, s));
        }

    #endregion
    }
}

