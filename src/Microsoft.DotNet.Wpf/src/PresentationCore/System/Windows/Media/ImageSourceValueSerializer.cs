// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Value serializer for ImageSource instances
//
//
#pragma warning disable 1634, 1691 // Allow suppression of certain presharp messages

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace System.Windows.Media
{
    /// <summary>
    /// Value serializer for Transform instances
    /// </summary>
    public class ImageSourceValueSerializer : ValueSerializer
    {
        /// <summary>
        /// Returns true.
        /// </summary>
        public override bool CanConvertFromString(string value, IValueSerializerContext context)
        {
            return true;
        }

        /// <summary>
        /// Returns true if the given transform can be converted into a string
        /// </summary>
        public override bool CanConvertToString(object value, IValueSerializerContext context)
        {
            ImageSource imageSource = value as ImageSource;
            #pragma warning disable 6506
            return imageSource != null && imageSource.CanSerializeToString();
            #pragma warning restore 6506
        }

        /// <summary>
        /// Converts a string into a transform.
        /// </summary>
        public override object ConvertFromString(string value, IValueSerializerContext context)
        {
            if (!string.IsNullOrEmpty(value))
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
            return base.ConvertFromString(value, context);
        }

        /// <summary>
        /// Converts a transform into a string.
        /// </summary>
        public override string ConvertToString(object value, IValueSerializerContext context)
        {
            ImageSource imageSource = value as ImageSource;
            if (imageSource != null)
                return imageSource.ConvertToString(null,  System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS);
            else
                return base.ConvertToString(value, context);
        }
    }
}
