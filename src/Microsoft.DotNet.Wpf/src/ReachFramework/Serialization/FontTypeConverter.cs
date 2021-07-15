// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++

    Abstract:
        This file implements the FontTypeConverter used by
        the Xps Serialization APIs for serializing fonts
        to a Xps package.

--*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Xps.Packaging;
using MS.Utility;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// This class implements a type converter for converting
    /// fonts to Uris.  It handles the writing of the font
    /// to a Xps package and returns a package URI to the
    /// caller.  It also handles reading a font from a
    /// Xps package given a Uri.
    /// </summary>
    public class FontTypeConverter : ExpandableObjectConverter
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
            if( context == null )
            {
                throw new ArgumentNullException("context");
            }
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXConvertFontBegin);

            if (!IsSupportedType(destinationType))
            {
                throw new NotSupportedException(SR.Get(SRID.Converter_ConvertToNotSupported));
            }

            PackageSerializationManager manager = (PackageSerializationManager)context.GetService(typeof(XpsSerializationManager));
            //
            // Ensure that we have a valid GlyphRun instance
            //
            GlyphRun fontGlyphRun = (GlyphRun)value;
            if (fontGlyphRun == null)
            {
                throw new ArgumentException(SR.Get(SRID.MustBeOfType, "value", "GlyphRun"));
            }

            //
            // Obtain the font serialization service from the serlialization manager.
            //
            IServiceProvider resourceServiceProvider = manager.ResourcePolicy;
            XpsFontSerializationService fontService = (XpsFontSerializationService)resourceServiceProvider.GetService(typeof(XpsFontSerializationService));
            if (fontService == null)
            {
                throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoFontService));
            }

            //
            // Retrieve the current font subsetter
            //
            XpsFontSubsetter fontSubsetter = fontService.FontSubsetter;

            //
            // Add the font subset to the font subsetter and retrieve a Uri
            // to the font within the Xps package.
            //
            Uri resourceUri = fontSubsetter.ComputeFontSubset(fontGlyphRun);

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXConvertFontEnd);

            return resourceUri;
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
            Type    type
            )
        {
            bool bSupported = false;

            foreach (Type t in SupportedTargetTypes)
            {
                if (t.Equals(type))
                {
                    bSupported = true;
                    break;
                }
            }

            return bSupported;
        }

        #endregion Private static helper methods

        #region Private static data

        /// <summary>
        /// A table of supported types for this type converter
        /// </summary>
        private static Type[] SupportedTargetTypes = {
            typeof(Uri)
        };

        #endregion Private static data
    }
}
