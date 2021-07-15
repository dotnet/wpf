// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
                                                                                                                                         
                                                                              
    Abstract:
        This file implements the XpsImageSerializationService
        used by the Xps Serialization APIs for serializing
        images to a Xps package.
--*/
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Security;
using System.Windows.Xps.Packaging;

using MS.Internal;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// This class implements a support service for serialization
    /// of BitmapSource instances to a Xps package.
    /// </summary>
    internal class XpsImageSerializationService
    {
        #region Public methods

        /// <summary>
        /// This method retrieves the BitmapEncoder to be used for
        /// serialization based on the specified BitmapSource.
        /// </summary>
        /// <param name="bitmapSource">
        /// A reference to a BitmapSource that will be encoded.
        /// </param>
        /// <returns>
        /// Returns a reference to a new BitmapEncoder.
        /// </returns>
        public
        BitmapEncoder
        GetEncoder(
            BitmapSource bitmapSource
            )
        {
            BitmapEncoder encoder = null;

            if (bitmapSource is BitmapFrame)
            {
                //
                // This code gets the encoder based on the decoder that was
                // used for this specific BitmapSource.
                //
                BitmapFrame bitmapImage = bitmapSource as BitmapFrame;
                BitmapCodecInfo codecInfo = null;

                if (bitmapImage != null && bitmapImage.Decoder != null)
                    codecInfo = bitmapImage.Decoder.CodecInfo;

                if (codecInfo != null)
                {
                    encoder = BitmapEncoder.Create(codecInfo.ContainerFormat);

                    // Avoid GIF encoder which does not save transparency well
                    if ( !( encoder is JpegBitmapEncoder || 
                            encoder is PngBitmapEncoder ||
                            encoder is TiffBitmapEncoder ||
                            encoder is WmpBitmapEncoder)
                       )
                    {
                        encoder = null;
                    }
                }
            }

            //
            // The code above assumes that the BitmapSource is actually
            // a BitmapImage.  If it is not then we assume Png and use
            // that encoder.
            //
            if (encoder == null)
            {
                if (Microsoft.Internal.AlphaFlattener.Utility.NeedPremultiplyAlpha(bitmapSource))
                {
                    encoder = new WmpBitmapEncoder();
                }
                else
                {
                    encoder = new PngBitmapEncoder();
                }
            }

            return encoder;
        }


        /// <summary>
        /// This method determines if a bitmap is of a supported 
        /// Xps Mime type
        /// </summary>
        /// <param name="bitmapSource">
        /// A reference to a BitmapSource to be tested.
        /// </param>
        /// <returns>
        /// Returns true if the bitmapSource is of supported mimetype
        /// </returns>
        public
        bool
        IsSupportedMimeType(
            BitmapSource bitmapSource
            )
        {
            BitmapCodecInfo codecInfo = null;
            string imageMimeType = "";

            if (bitmapSource is BitmapFrame)
            {
                //
                // This code gets the encoder based on the decoder that was
                // used for this specific BitmapSource.
                //
                BitmapFrame bitmapFrame = bitmapSource as BitmapFrame;
                
                if (bitmapFrame != null && bitmapFrame.Decoder != null)
                {
                    codecInfo = bitmapFrame.Decoder.CodecInfo;
                }
            }
            
            if (codecInfo != null)
            {
                imageMimeType = codecInfo.MimeTypes;
            }
            int start = 0;
            int comma = imageMimeType.IndexOf(',', start);
            bool foundType = false;
            //
            // Test all strings before commas
            //
            if( comma != -1 )
            {
                while (comma != -1 && !foundType)
                {
                    string subString =  imageMimeType.Substring(start, comma);
                    foundType = XpsManager.SupportedImageType( new ContentType(subString) );
                    start = comma+1;
                    comma = imageMimeType.IndexOf(',', start);
                }
                
            }
            
            //
            // If we still have not found a supported type
            // Test the remainder of the string
            //
            if( !foundType )
            {
                foundType = XpsManager.SupportedImageType( new ContentType(imageMimeType.Substring(start)) );
            }
        
            return foundType;
        }
        
        /// <summary>
        /// This method verifies whether a given BitmapSource
        /// is serializable by this service.
        /// </summary>
        /// <param name="bitmapSource">
        /// A reference to a BitmapSource to be checked.
        /// </param>
        /// <returns>
        /// A boolean value specifing serializability.
        /// </returns>
        public
        bool
        VerifyImageSourceSerializability(
            BitmapSource bitmapSource
            )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method serializes a given BitmapSource to a
        /// stream and returns a reference to the stream.
        /// </summary>
        /// <param name="bitmapSource">
        /// A reference to a BitmapSource to be serialized.
        /// </param>
        /// <returns>
        /// A reference to a Stream where BitmapSource was serialized.
        /// </returns>
        public
        Stream
        SerializeToStream(
            BitmapSource bitmapSource
            )
        {
            throw new NotImplementedException();
        }

        #endregion Public methods
    }
}
