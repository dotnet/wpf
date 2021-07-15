// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.IO;
using System.Security;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Markup;
using MMCF = System.IO.Packaging;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using System.Windows.Media.Imaging;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Media
{
    #region ImageSourceConverter
    /// <summary>
    /// ImageSourceConverter
    /// </summary>
    public class ImageSourceConverter : TypeConverter
    {
        /// <summary>
        /// Returns true if this type converter can convert from a given type.
        /// </summary>
        /// <returns>
        /// bool - True if this converter can convert from the provided type, false if not.
        /// </returns>
        /// <param name="context"> The ITypeDescriptorContext for this call. </param>
        /// <param name="sourceType"> The Type being queried for support. </param>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string) || sourceType == typeof(Stream) || sourceType == typeof(Uri) || sourceType == typeof(byte[]))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Returns true if this type converter can convert to the given type.
        /// </summary>
        /// <returns>
        /// bool - True if this converter can convert to the provided type, false if not.
        /// </returns>
        /// <param name="context"> The ITypeDescriptorContext for this call. </param>
        /// <param name="destinationType"> The Type being queried for support. </param>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                // When invoked by the serialization engine we can convert to string only for some instances
                if (context != null && context.Instance != null)
                {
                    if (!(context.Instance is ImageSource))
                    {
                        throw new ArgumentException(SR.Get(SRID.General_Expected_Type, "ImageSource"), "context");
                    }

                    #pragma warning suppress 6506 // context is obviously not null
                    ImageSource value = (ImageSource)context.Instance;

                    #pragma warning suppress 6506 // value is obviously not null
                    return value.CanSerializeToString();
                }

                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Attempts to convert to a ImageSource from the given object.
        /// </summary>
        /// <returns>
        /// The ImageSource which was constructed.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// A NotSupportedException is thrown if the example object is null or is not a valid type
        /// which can be converted to a ImageSource.
        /// </exception>
        /// <param name="context"> The ITypeDescriptorContext for this call. </param>
        /// <param name="culture"> The CultureInfo which is respected when converting. </param>
        /// <param name="value"> The object to convert to an instance of ImageSource. </param>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            try
            {
                if (value == null)
                {
                    throw GetConvertFromException(value);
                }

                if (((value is string) && (!string.IsNullOrEmpty((string)value))) || (value is Uri))
                {
                    UriHolder uriHolder = TypeConverterHelper.GetUriFromUriContext(context, value);
                    return BitmapFrame.CreateFromUriOrStream(
                        uriHolder.BaseUri,
                        uriHolder.OriginalUri,
                        null,
                        BitmapCreateOptions.None,
                        BitmapCacheOption.Default,
                        null
                        );
                }
                else if (value is byte[])
                {
                    byte[] bytes = (byte[])value;

                    if (bytes != null)
                    {
                        Stream memStream = null;

                        //
                        // this might be a magical OLE thing, try that first.
                        //
                        memStream = GetBitmapStream(bytes);

                        if (memStream == null)
                        {
                            //
                            // guess not.  Try plain memory.
                            //
                            memStream = new MemoryStream(bytes);
                        }

                        return BitmapFrame.Create(
                            memStream,
                            BitmapCreateOptions.None,
                            BitmapCacheOption.Default
                            );
                    }
                }
                else if (value is Stream)
                {
                    Stream stream = (Stream)value;

                    return BitmapFrame.Create(
                        stream,
                        BitmapCreateOptions.None,
                        BitmapCacheOption.Default
                        );
                }

                return base.ConvertFrom(context, culture, value);
            }
            catch (Exception e)
            {
                if (!CriticalExceptions.IsCriticalException(e))
                {
                    if (context == null && CoreAppContextSwitches.OverrideExceptionWithNullReferenceException)
                    {
                        throw new NullReferenceException();
                    }

                    IProvideValueTarget ipvt = context?.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
                    if (ipvt != null)
                    {
                        IProvidePropertyFallback ippf = ipvt.TargetObject as IProvidePropertyFallback;
                        DependencyProperty dp = ipvt.TargetProperty as DependencyProperty;
                        // We only want to call IPPF.SetValue if the target can handle it.
                        // We need to check for non DP scenarios (This is currently an internal interface used
                        //             only by Image so it's okay for now)

                        if (ippf != null && dp != null && ippf.CanProvidePropertyFallback(dp.Name))
                        {
                            return ippf.ProvidePropertyFallback(dp.Name, e);
                        }
                    }
                }

                // We want to rethrow the exception in the case we can't handle it.
                throw;
            }
        }

        /// <summary>
        /// ConvertTo - Attempt to convert an instance of ImageSource to the given type
        /// </summary>
        /// <returns>
        /// The object which was constructoed.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// A NotSupportedException is thrown if "value" is null or not an instance of ImageSource,
        /// or if the destinationType isn't one of the valid destination types.
        /// </exception>
        /// <param name="context"> The ITypeDescriptorContext for this call. </param>
        /// <param name="culture"> The CultureInfo which is respected when converting. </param>
        /// <param name="value"> The object to convert to an instance of "destinationType". </param>
        /// <param name="destinationType"> The type to which this will convert the ImageSource instance. </param>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType != null && value is ImageSource)
            {
                ImageSource instance = (ImageSource)value;

                if (destinationType == typeof(string))
                {
                    // When invoked by the serialization engine we can convert to string only for some instances
                    if (context != null && context.Instance != null)
                    {
                        #pragma warning disable 6506
                        if (!instance.CanSerializeToString())
                        {
                            throw new NotSupportedException(SR.Get(SRID.Converter_ConvertToNotSupported));
                        }
                        #pragma warning restore 6506
                    }

                    // Delegate to the formatting/culture-aware ConvertToString method.
                    return instance.ConvertToString(null, culture);
                }
            }

            // Pass unhandled cases to base class (which will throw exceptions for null value or destinationType.)
            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// Try to get a bitmap out of a byte array.  This is an ole format that Access uses.
        /// this fails very quickly so we can try this first without a big perf hit.
        private unsafe Stream GetBitmapStream(byte[] rawData)
        {
            Debug.Assert(rawData != null, "rawData is null.");
            fixed (byte* pByte = rawData)
            {
                IntPtr addr = (IntPtr)pByte;

                if (addr == IntPtr.Zero)
                {
                    return null;
                }

                //
                // This will be pHeader.signature, but we try to
                // do this first so we avoid building the structure when we shouldn't
                //
                if (Marshal.ReadInt16(addr) != 0x1c15)
                {
                    return null;
                }

                //
                // The data is one of these OBJECTHEADER dudes.  It's an encoded format that Access uses to push images
                // into the DB.  It's not particularly documented, here is a KB:
                //
                // http://support.microsoft.com/default.aspx?scid=KB;EN-US;Q175261
                //
                OBJECTHEADER pHeader = (OBJECTHEADER)Marshal.PtrToStructure(addr, typeof(OBJECTHEADER));

                //
                // "PBrush" should be the 6 chars after position 12 as well.
                //
                string strPBrush = System.Text.Encoding.ASCII.GetString(rawData, pHeader.headersize + 12, 6);

                if (strPBrush != "PBrush")
                {
                    return null;
                }

                // OK, now we can safely trust that we've got a bitmap.
                byte[] searchArray = System.Text.Encoding.ASCII.GetBytes("BM");

                //
                // Search for "BMP" in the data which is the start of our bitmap data...
                //
                // 18 is from (12+6) above.
                //
                for (int i = pHeader.headersize + 18; i < pHeader.headersize + 510; i++)
                {
                    if (searchArray[0] == pByte[i] &&
                        searchArray[1] == pByte[i+1])
                    {
                        //
                        // found the bitmap data.
                        //
                        return new MemoryStream(rawData, i, rawData.Length - i);
                    }
}
            }

            return null;
        }

        //
        // For pulling encoded IPictures out of Access Databases
        //
        [StructLayout(LayoutKind.Sequential)]
        private struct OBJECTHEADER {
            public short signature; // this looks like it's always 0x1c15
            public short headersize; // how big all this goo ends up being.  after this is the actual object data.
            public short objectType; // we don't care about anything else...they don't seem to be meaningful anyway.
            public short nameLen;
            public short classLen;
            public short nameOffset;
            public short classOffset;
            public short width;
            public short height;
            public IntPtr pInfo;
        }
    }

    #endregion // ImageSourceConverter
}


