// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++

    Abstract:
        This file implements the ImageSourceTypeConverter
        used by the Xps Serialization APIs for serializing
        images to a Xps package.

--*/
using System;
using System.Net;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Packaging;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps.Packaging;
using System.Security;
using MS.Internal.ReachFramework;
using System.Windows.Markup;
using MS.Internal.IO.Packaging;
using MS.Utility;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// This class implements a type converter for converting
    /// images to Uris.  It handles the writing of the image
    /// to a Xps package and returns a package URI to the
    /// caller.  It also handles reading an image from a
    /// Xps package given a Uri.
    /// </summary>
    public class ImageSourceTypeConverter : ExpandableObjectConverter
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
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXConvertImageBegin);

            if( context == null )
            {
                throw new ArgumentNullException("context");
            }
            if (!IsSupportedType(destinationType))
            {
                throw new NotSupportedException(SR.Get(SRID.Converter_ConvertToNotSupported));
            }

            //
            // Check that we have a valid BitmapSource instance.
            //
            BitmapSource bitmapSource = (BitmapSource)value;
            if (bitmapSource == null)
            {
                throw new ArgumentException(SR.Get(SRID.MustBeOfType, "value", "BitmapSource"));
            }

            //
            // Get the current serialization manager.
            //
            PackageSerializationManager manager = (PackageSerializationManager)context.GetService(typeof(XpsSerializationManager));

            //Get the image Uri if it has already been serialized
            Uri imageUri = GetBitmapSourceFromImageTable(manager, bitmapSource);

            //
            // Get the current page image cache
            //
            Dictionary<int, Uri> currentPageImageTable = manager.ResourcePolicy.CurrentPageImageTable;
            if (imageUri != null)
            {
                int uriHashCode = imageUri.GetHashCode();
                if(!currentPageImageTable.ContainsKey(uriHashCode))
                {
                   //
                   // Also, add a relationship for the current page to this image
                   // resource.  This is needed to conform with Xps specification.
                   //
                   manager.AddRelationshipToCurrentPage(imageUri, XpsS0Markup.ResourceRelationshipName);
                   currentPageImageTable.Add(uriHashCode, imageUri);
                }
            }
            else
            {
                //
                // This image as never serialized before so we will do it now.
                // Retrieve the image serialization service from the resource policy
                //
                IServiceProvider resourceServiceProvider = manager.ResourcePolicy;

                XpsImageSerializationService imageService = (XpsImageSerializationService)resourceServiceProvider.GetService(typeof(XpsImageSerializationService));
                if (imageService == null)
                {
                    throw new XpsSerializationException(SR.Get(SRID.ReachSerialization_NoImageService));
                }

                //
                // Obtain a valid encoder for the image.
                //
                BitmapEncoder encoder = imageService.GetEncoder(bitmapSource);
                string imageMimeType = GetImageMimeType(encoder);

                bool isSupportedMimeType = imageService.IsSupportedMimeType(bitmapSource);

                //
                // Acquire a writable stream from the serialization manager and encode
                // and serialize the image into the stream.
                //
                XpsResourceStream resourceStream = manager.AcquireResourceStream(typeof(BitmapSource), imageMimeType);
                bool bCopiedStream = false;

                BitmapFrame bitmapFrame = bitmapSource as BitmapFrame;

                if (isSupportedMimeType &&
                    bitmapFrame != null &&
                    bitmapFrame.Decoder != null
                    )
                {
                    BitmapDecoder decoder = bitmapFrame.Decoder;
                    try
                    {
                        Uri sourceUri = new Uri(decoder.ToString());
                        Stream srcStream = MS.Internal.WpfWebRequestHelper.CreateRequestAndGetResponseStream(sourceUri);
                        CopyImageStream(srcStream, resourceStream.Stream);
                        srcStream.Close();
                        bCopiedStream = true;
                    }
                    catch (UriFormatException)
                    {
                        //the uri was not valid we will re-encode the image below
                    }
                    catch (WebException)
                    {
                        //Web Request failed we will re-encode the image below
                    }
                }

                if (!bCopiedStream)
                {
                    Stream stream = new MemoryStream();
                    ReEncodeImage(bitmapSource, encoder, stream);
                    stream.Position = 0;
                    CopyImageStream(stream, resourceStream.Stream);
                }

                //
                // Make sure to commit the resource stream by releasing it.
                //
                imageUri = resourceStream.Uri;
                manager.ReleaseResourceStream(typeof(BitmapSource));

                AddBitmapSourceToImageTables(manager, imageUri);
            }

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXConvertImageEnd);

            return imageUri;
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

        private
        static
        void
        CopyImageStream( Stream sourceStream, Stream destinationStream )
        {
            byte[] buffer= new byte[_readBlockSize];
            int bytesRead = PackagingUtilities.ReliableRead( sourceStream, buffer, 0, _readBlockSize);
            while( bytesRead > 0 )
            {
                destinationStream.Write(buffer, 0, bytesRead);
                bytesRead = PackagingUtilities.ReliableRead( sourceStream, buffer, 0, _readBlockSize);
            }
        }

        private
        static
        void
        ReEncodeImage(BitmapSource bitmapSource, BitmapEncoder encoder, Stream stream )
        {
            BitmapFrame bitmapFrame = null;
            //
            // The uri the BitmapFrame.Create will use is null since it is accessing  metadata at
            // construction and its uri is still null
            //

            // If bitmapSource is indexed, has a color palette and transparency (e.g. transparent GIF)
            // PNG conversion may lose color or transparency or both information
            // To avoid this we convert all paletted bitmapSources to the 32 bit per pixel bgra format
            if (bitmapSource != null
                && bitmapSource.Palette != null
                && bitmapSource.Palette.Colors != null
                && bitmapSource.Palette.Colors.Count > 0)
            {
                bitmapSource = new FormatConvertedBitmap(bitmapSource, PixelFormats.Bgra32, null, 0.0);
            }

            bitmapFrame = BitmapFrame.Create(bitmapSource);

            encoder.Frames.Add(bitmapFrame);

            encoder.Save(stream);
}

        /// <summary>
        /// Calculates a Crc32 value for a given BitmapSource.
        /// </summary>
        /// <param name="bitmapSource">
        /// BitmapSource containing image data to calculate
        /// the Crc32 value for.
        /// </param>
        /// <returns>
        /// A 32-bit unsigned integer Crc32 value.
        /// </returns>
        private
        static
        UInt32
        CalculateImageCrc32(
            BitmapSource    bitmapSource
            )
        {
            Crc32 crc32 = new Crc32();

            int width = bitmapSource.PixelWidth;
            int height = bitmapSource.PixelHeight;
            int stride = (width * bitmapSource.Format.BitsPerPixel + 7) / 8;
            Int32Rect rect = new Int32Rect(0, 0, width, 1);
            byte[] pixels = new byte[stride];


            for (int i = 0; i < height; i += 1)
            {
                bitmapSource.CriticalCopyPixels(rect, pixels, stride, 0);
                rect.Y++;

                crc32.AddData(pixels);
            }

            return crc32.Crc32Value;
        }


        private
        static
        string
        GetImageMimeType(
            BitmapEncoder encoder
        )
        {
            string mimetypes;
            string imageMimeType = "image/unknown";
            //
            // To determine the mime-type of the image we just grab
            // the first one supported by this encoder and use that.
            //
            mimetypes = encoder.CodecInfo.MimeTypes;
            int comma = mimetypes.IndexOf(',');
            if (comma != -1)
            {
                imageMimeType = mimetypes.Substring(0, comma);
            }
            else
            {
                imageMimeType = mimetypes;
            }
            return imageMimeType;
        }


        private
        Uri
        GetBitmapSourceFromImageTable(PackageSerializationManager manager, BitmapSource bitmapSource)
        {
            Uri imageUri = null;

            //Initialize cache values
            _uriHashValue = 0;
            _crc32HashValue = 0;

            BitmapFrame bitmapFrame = bitmapSource as BitmapFrame;

            //Use the Uri hash table if we have a bitmap with a Uri
            if (bitmapFrame != null &&
                bitmapFrame.Decoder != null)
            {
                String sourceUri  = bitmapFrame.Decoder.ToString();
                if (sourceUri != null)
                {
                    _uriHashValue = sourceUri.GetHashCode();

                    Dictionary<int, Uri> imageUriHashTable = manager.ResourcePolicy.ImageUriHashTable;
                    if (imageUriHashTable.ContainsKey(_uriHashValue))
                    {
                        imageUri = imageUriHashTable[_uriHashValue];
                    }
                }
            }

            //Checking the UriHash for zero tells us if the uri of the bitmap was checked and if it returned a valid Uri
            if (_uriHashValue == 0)
            {
                //
                // Calculate the image Crc32 value.  This is used as a key
                // into the image cache.
                //
                _crc32HashValue = CalculateImageCrc32(bitmapSource);

                //
                // Get the current image cache
                //
                Dictionary<UInt32, Uri> imageCrcTable = manager.ResourcePolicy.ImageCrcTable;

                //
                // The image has already been cached (and therefore serialized).
                // No need to serialize it again so we just return the Uri in the
                // package where the original was serialized. For that Uri returned
                // a relationship is only created if this has not been included on
                // the current page before.
                //
                if (imageCrcTable.ContainsKey(_crc32HashValue))
                {
                    imageUri = imageCrcTable[_crc32HashValue];
                }
            }

            return imageUri;
        }

        private
        void
        AddBitmapSourceToImageTables(PackageSerializationManager manager, Uri imageUri)
        {
            if (_uriHashValue != 0)
            {
                manager.ResourcePolicy.ImageUriHashTable.Add(_uriHashValue,imageUri);
                _uriHashValue = 0;
            }
            else
            {
                manager.ResourcePolicy.ImageCrcTable.Add(_crc32HashValue, imageUri);
                _crc32HashValue = 0;
            }

            manager.ResourcePolicy.CurrentPageImageTable.Add(imageUri.GetHashCode(), imageUri);
        }

        #endregion Private static helper methods

        #region Private static data

        /// <summary>
        /// A table of supported types for this type converter
        /// </summary>
        private static Type[] SupportedTargetTypes = {
            typeof(Uri)
        };
        private static readonly int _readBlockSize = 1048576; //1MB

        /// <summary>
        /// Cached hash values for image tables
        /// </summary>
        private int _uriHashValue;
        private UInt32 _crc32HashValue;
        #endregion Private static data
    }
}
