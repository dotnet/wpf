// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.IO;
using System.Windows.Ink;
using System.Runtime.Serialization.Formatters;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.Win32;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Security;
using MS.Utility;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows
{
    /// <summary>
    ///      StrokeCollectionConverter is a class that can be used to convert StrokeCollection objects
    ///      to strings representing base64 encoded ink, and strings to StrokeCollection objects.
    /// </summary>
    public class StrokeCollectionConverter : TypeConverter
    {
        /// <summary>
        ///    Public constructor
        /// </summary>
        public StrokeCollectionConverter()
        {
        }

        /// <summary>
        ///     Determines if this converter can convert an object to an StrokeCollection object
        /// </summary>
        /// <param name="context">An ITypeDescriptorContext that provides a format context.</param>
        /// <param name="sourceType">A Type that represents the type you want to convert from. </param>
        /// <returns>true if this converter can perform the conversion; otherwise, false.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Gets a value indicating whether this converter can
        ///       convert an object to the given destination type using the context.
        /// </summary>
        /// <param name="context">An ITypeDescriptorContext that provides a format context.</param>
        /// <param name="destinationType">A Type that represents the type you want to convert to.</param>
        /// <returns>true if this converter can perform the conversion; otherwise, false.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            // This method overrides CanConvertTo from TypeConverter. This is called when someone
            //  wants to convert an instance of StrokeCollection to another type.  Here,
            //  only conversion to an InstanceDescriptor is supported.
            if (destinationType == typeof(InstanceDescriptor))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Converts the given object to the converter's native type.  In this implementation, a string
        /// representing base64 encoded ink will be converted to an StrokeCollection object.
        /// </summary>
        /// <param name="context">An ITypeDescriptorContext that provides a format context.</param>
        /// <param name="culture">The CultureInfo to use as the current culture. </param>
        /// <param name="value">The Object to convert. </param>
        /// <returns>An Object that represents the converted value.</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            //we only support converting to / from a string
            string text = value as string;
            // brianew - presharp issue
            //   diabling the warning for not using IsNullOrEmpty on the string since
            //   are using the two operations for differnt results.
#pragma warning disable 1634, 1691
#pragma warning disable 6507
            if (text != null)
            {
                //always return an ink object
                //even if the string is empty
                text = text.Trim();

                if (text.Length != 0)
                {
                    using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(text)))
                    {
                        return new StrokeCollection(ms);
                    }
                }
                else
                {
                    return new StrokeCollection();
                }
            }
#pragma warning restore 6507
#pragma warning restore 1634, 1691
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        ///      Converts the given object to another type.  In this implementation, the only supported
        ///      convertion is from an StrokeCollection object to a string.  The default implementation will make a call
        ///      to ToString on the object if the object is valid and if the destination
        ///      type is string.  If this cannot convert to the desitnation type, this will
        ///      throw a NotSupportedException.
        /// </summary>
        /// <param name="context">An ITypeDescriptorContext that provides a format context.</param>
        /// <param name="culture">A CultureInfo object. If a null reference (Nothing in Visual Basic) is passed, the current culture is assumed. </param>
        /// <param name="value">The Object to convert.</param>
        /// <param name="destinationType">The Type to convert the value parameter to. </param>
        /// <returns>An Object that represents the converted value</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }


            //if someone wants to convert to a string...
            StrokeCollection strokes = value as StrokeCollection;
            if (strokes != null)
            {
                if (destinationType == typeof(string))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        strokes.Save(ms, true);
                        ms.Position = 0;
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
                    //if someone wants to convert to an InstanceDescriptor
                else if (destinationType == typeof(InstanceDescriptor))
                {
                    //get a ref to the StrokeCollection objects constructor that takes a byte[]
                    ConstructorInfo ci = typeof(StrokeCollection).GetConstructor(new Type[] { typeof(Stream) });

                    // samgeo - Presharp issue
                    // Presharp gives a warning when local IDisposable variables are not closed
                    // in this case, we can't call Dispose since it will also close the underlying stream
                    // which strokecollection needs open to read in the constructor
#pragma warning disable 1634, 1691
#pragma warning disable 6518
                    MemoryStream stream = new MemoryStream();
                    strokes.Save(stream, true/*compress*/);
                    stream.Position = 0;
                    return new InstanceDescriptor(ci, new object[] { stream });
#pragma warning restore 6518
#pragma warning restore 1634, 1691
                }
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <summary>
        /// Returns whether this object supports a standard set of values that can be
        /// picked from a list, using the specified context.
        /// </summary>
        /// <param name="context">An ITypeDescriptorContext that provides a format context.</param>
        /// <returns>
        ///     true if GetStandardValues should be called to find a common set
        ///     of values the object supports; otherwise, false.
        /// </returns>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return false;
        }
    }
}
