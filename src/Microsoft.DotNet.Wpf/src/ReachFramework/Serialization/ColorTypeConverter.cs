// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++

    Abstract:
        This file implements the ColorTypeConverter
        used by the Xps Serialization APIs for serializing
        colors to a Xps package.

--*/
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps.Packaging;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// This class implements a type converter for converting
    /// Color to Uris.  It handles the writing of the Color Context
    /// to a Xps package and returns a package URI to the
    /// caller.  It also handles reading an Color Context from a
    /// Xps package given a Uri.
    /// </summary>
    public class ColorTypeConverter : ExpandableObjectConverter
    {
        #region Public overrides for ExpandableObjectConverted

        /// <summary>
        /// Returns whether this converter can convert an object
        /// of the given type to the type of this converter.
        /// </summary>
        /// <param name="context">
        /// An ITypeDescriptorContext that provides a format context.
        /// </param>
        /// <param name="sourceType">
        /// A Type that represents the type you want to convert from.
        /// </param>
        /// <returns>
        /// true if this converter can perform the conversion;
        /// otherwise, false.
        /// </returns>
        public
        override
        bool
        CanConvertFrom(
            ITypeDescriptorContext      context,
            Type                        sourceType
            )
        {
            return IsSupportedType(sourceType);
        }

        /// <summary>
        /// Returns whether this converter can convert the object
        /// to the specified type.
        /// </summary>
        /// <param name="context">
        /// An ITypeDescriptorContext that provides a format context.
        /// </param>
        /// <param name="destinationType">
        /// A Type that represents the type you want to convert to.
        /// </param>
        /// <returns>
        /// true if this converter can perform the conversion;
        /// otherwise, false.
        /// </returns>
        public
        override
        bool
        CanConvertTo(
            ITypeDescriptorContext      context,
            Type                        destinationType
            )
        {
            return IsSupportedType(destinationType);
        }

        /// <summary>
        /// Converts the given value to the type of this converter.
        /// </summary>
        /// <param name="context">
        /// An ITypeDescriptorContext that provides a format context.
        /// </param>
        /// <param name="culture">
        /// The CultureInfo to use as the current culture.
        /// </param>
        /// <param name="value">
        /// The Object to convert.
        /// </param>
        /// <returns>
        /// An Object that represents the converted value.
        /// </returns>
        public
        override
        object
        ConvertFrom(
            ITypeDescriptorContext              context,
            System.Globalization.CultureInfo    culture,
            object                              value
            )
        {
            if( value == null )
            {
                throw new ArgumentNullException("value");
            }
            if (!IsSupportedType(value.GetType()))
            {
                throw new NotSupportedException(SR.Get(SRID.Converter_ConvertFromNotSupported));
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts the given value object to the specified type,
        /// using the arguments.
        /// </summary>
        /// <param name="context">
        /// An ITypeDescriptorContext that provides a format context.
        /// </param>
        /// <param name="culture">
        /// A CultureInfo object. If null is passed, the current
        /// culture is assumed.
        /// </param>
        /// <param name="value">
        /// The Object to convert.
        /// </param>
        /// <param name="destinationType">
        /// The Type to convert the value parameter to.
        /// </param>
        /// <returns>
        /// The Type to convert the value parameter to.
        /// </returns>
        public
        override
        object
        ConvertTo(
            ITypeDescriptorContext              context,
            System.Globalization.CultureInfo    culture,
            object                              value,
            Type                                destinationType
            )
        {
            if (!IsSupportedType(destinationType))
            {
                throw new NotSupportedException(SR.Get(SRID.Converter_ConvertToNotSupported));
            }

            Color color = (Color)value;

            string colorString;

            if (color.ColorContext == null)
            {
                colorString = color.ToString(culture);

                if (colorString.StartsWith("sc#", StringComparison.Ordinal) && colorString.Contains("E"))
                {
                    //
                    // Fix bug 1588888: Serialization produces non-compliant scRGB color string.
                    //
                    // Avalon Color.ToString() will use "R"oundtrip formatting specifier for scRGB colors,
                    // which may produce numbers in scientific notation, which is invalid XPS.
                    //
                    // Fix: We detect such colors and format with "F"ixed specifier. We don't always use F
                    // in order to properly handle RGB colors.
                    //
                    IFormattable formattableColor = (IFormattable)color;
                    colorString = formattableColor.ToString("F", culture);
                }
            }
            else
            {
                string uriString = SerializeColorContext(context, color.ColorContext);
        
                StringBuilder sb = new StringBuilder();
                IFormatProvider provider = culture;
                char separator = MS.Internal.TokenizerHelper.GetNumericListSeparator(provider);

                sb.AppendFormat(provider, "ContextColor {0} ", uriString);
                sb.AppendFormat(provider, "{1:R}{0}", separator, color.ScA);
                for (int i = 0; i < color.GetNativeColorValues().GetLength(0); ++i)
                {
                    sb.AppendFormat(provider, "{0:R}", color.GetNativeColorValues()[i]);
                    if (i < color.GetNativeColorValues().GetLength(0) - 1)
                    {
                        sb.AppendFormat(provider, "{0}", separator);
                    }
                }

                colorString = sb.ToString();
            }

            return colorString;
        }

        /// <summary>
        /// Gets a collection of properties for the type of object
        /// specified by the value parameter.
        /// </summary>
        /// <param name="context">
        /// An ITypeDescriptorContext that provides a format context.
        /// </param>
        /// <param name="value">
        /// An Object that specifies the type of object to get the
        /// properties for.
        /// </param>
        /// <param name="attributes">
        /// An array of type Attribute that will be used as a filter.
        /// </param>
        /// <returns>
        /// A PropertyDescriptorCollection with the properties that are
        /// exposed for the component, or null if there are no properties.
        /// </returns>
        public
        override
        PropertyDescriptorCollection
        GetProperties(
            ITypeDescriptorContext      context,
            object                      value,
            Attribute[]                 attributes
            )
        {
            throw new NotImplementedException();
        }

        #endregion Public overrides for ExpandableObjectConverted

        #region Private static helper methods

        /// <summary>
        /// Looks up the type in a table to determine
        /// whether this type is supported by this
        /// class.
        /// </summary>
        /// <param name="type">
        /// Type to lookup in table.
        /// </param>
        /// <returns>
        /// True is supported; otherwise false.
        /// </returns>
        private
        static
        bool
        IsSupportedType(
            Type            type
            )
        {
            bool isSupported = false;

            foreach (Type t in SupportedTargetTypes)
            {
                if (t.Equals(type))
                {
                    isSupported = true;
                    break;
                }
            }

            return isSupported;
        }

        #endregion Private static helper methods

        #region Public static helper methods

        /// <summary>
        /// Serializes a ColorContext to the package and returns it Uri
        /// </summary>
        /// <param name="context">
        /// Type descriptor context
        /// </param>
        /// <param name="colorContext">
        /// ColorContext
        /// </param>
        /// <returns>
        /// string containing the profileUri
        /// </returns>
        public
        static
        string
        SerializeColorContext(
            IServiceProvider                    context,
            ColorContext                        colorContext
            )
        {
            Uri profileUri = null;

            if( colorContext == null )
            {
                throw new ArgumentNullException("colorContext");
            }
            if ( context!= null )
            {
                PackageSerializationManager manager = (PackageSerializationManager)context.GetService(typeof(XpsSerializationManager));
                Dictionary<int, Uri> colorContextTable = manager.ResourcePolicy.ColorContextTable;
                Dictionary<int, Uri> currentPageColorContextTable = manager.ResourcePolicy.CurrentPageColorContextTable;
    
                if(currentPageColorContextTable==null)
                {
                    //
                    // throw the appropriae exception
                    //
                }
                
                if(colorContextTable==null)
                {
                    //
                    // throw the appropriae exception
                    //
                }

                if (colorContextTable.ContainsKey(colorContext.GetHashCode()))
                {
                    //
                    // The colorContext has already been cached (and therefore serialized).
                    // No need to serialize it again so we just use the Uri in the
                    // package where the original was serialized. For that Uri used
                    // a relationship is only created if this has not been included on
                    // the current page before.
                    //
                    profileUri = colorContextTable[colorContext.GetHashCode()];

                    if (!currentPageColorContextTable.ContainsKey(colorContext.GetHashCode()))
                    {
                       //
                       // Also, add a relationship for the current page to this Color Context
                       // resource.  This is needed to conform with Xps specification.
                       //
                       manager.AddRelationshipToCurrentPage(profileUri, XpsS0Markup.ResourceRelationshipName);
                       currentPageColorContextTable.Add(colorContext.GetHashCode(), profileUri);
                    }
                }
                else
                {
                    MS.Internal.ContentType colorContextMimeType = XpsS0Markup.ColorContextContentType;
                        
                    XpsResourceStream resourceStream = manager.AcquireResourceStream(typeof(ColorContext), colorContextMimeType.ToString());

                    byte [] buffer = new byte[512];

                    Stream profileStream = colorContext.OpenProfileStream();
                    int count;
                    
                    while ( (count = profileStream.Read( buffer, 0, buffer.GetLength(0)) ) > 0 )
                    {
                        resourceStream.Stream.Write(buffer,0,count);
                    }
                    
                    profileStream.Close();
    
                    //
                    // Make sure to commit the resource stream by releasing it.
                    //
                    profileUri = resourceStream.Uri;
                    manager.ReleaseResourceStream(typeof(ColorContext));

                    colorContextTable.Add(colorContext.GetHashCode(), profileUri);
                    currentPageColorContextTable.Add(colorContext.GetHashCode(), profileUri);
                }
            }
            else // Testing only
            {
                profileUri = colorContext.ProfileUri;
            }

            //First Step make sure that nothing that should not be escaped is escaped
            Uri safeUnescapedUri = new Uri(profileUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.SafeUnescaped),
                                                    profileUri.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative);
            //Second Step make sure that everything that should escaped is escaped
            String uriString = safeUnescapedUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);

            return uriString;
        }

        #endregion Public static helper methods

        #region Private static data

        /// <summary>
        /// A table of supported types for this type converter
        /// </summary>
        private static Type[] SupportedTargetTypes = {
            typeof(string)
        };

        #endregion Private static data
    }
}
