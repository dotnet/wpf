// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Collections;
using System.Security;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using MS.Win32.PresentationCore;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace System.Windows.Media.Imaging
{
    #region TiffCompressOption

    /// <summary>
    /// Compress options for saving TIFF bitmap
    /// </summary>
    public enum TiffCompressOption
    {
        /// <summary>
        /// Don't care the compression schema
        /// In this case, the encoder will try to save the bitmap using the best
        /// compression schema
        /// </summary>
        Default = 0,

        /// <summary>
        /// No compression
        /// </summary>
        None = 1,

        /// <summary>
        /// Use CCITT3 compression schema. This works only for Black/white bitmap
        /// </summary>
        Ccitt3 = 2,

        /// <summary>
        /// Use CCITT4 compression schema. This works only for Black/white bitmap
        /// </summary>
        Ccitt4 = 3,

        /// <summary>
        /// Use LZW compression schema.
        /// </summary>
        Lzw = 4,

        /// <summary>
        /// Use RLE compression schema. This works only for Black/white bitmap
        /// </summary>
        Rle = 5,

        /// <summary>
        /// Use ZIP-deflate compression.
        /// </summary>
        Zip = 6
    };

    #endregion

    #region TiffBitmapEncoder

    /// <summary>
    /// Built-in Encoder for Tiff files.
    /// </summary>
    public sealed class TiffBitmapEncoder : BitmapEncoder
    {
        #region Constructors

        /// <summary>
        /// Constructor for TiffBitmapEncoder
        /// </summary>
        public TiffBitmapEncoder() :
            base(true)
        {
            _supportsPreview = false;
            _supportsGlobalThumbnail = false;
            _supportsGlobalMetadata = false;
            _supportsFrameThumbnails = true;
            _supportsMultipleFrames = true;
            _supportsFrameMetadata = true;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Set the compression type.  There are 3 compression types that require the
        /// format to be BlackWhite (TIFFCompressCCITT3, TIFFCompressCCITT4, and TIFFCompressRLE).
        /// Setting any of those 3 compression types automatically sets the encode format as well.
        /// Setting the format to something else will clear any of those compression types.
        /// </summary>
        public TiffCompressOption Compression
        {
            get
            {
                return _compressionMethod;
            }
            set
            {
                _compressionMethod = value;
            }
        }

        #endregion

        #region Internal Properties / Methods

        /// <summary>
        /// Returns the container format for this encoder
        /// </summary>
        internal override Guid ContainerFormat
        {
            get
            {
                return _containerFormat;
            }
        }

        /// <summary>
        /// Returns whether metadata is fixed size or not.
        /// </summary>
        internal override bool IsMetadataFixedSize
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Setups the encoder and other properties before encoding each frame
        /// </summary>
        internal override void SetupFrame(SafeMILHandle frameEncodeHandle, SafeMILHandle encoderOptions)
        {
            PROPBAG2 propBag = new PROPBAG2();
            PROPVARIANT propValue = new PROPVARIANT();

            // There is only one encoder option supported here:

            if (_compressionMethod != c_defaultCompressionMethod)
            {
                try
                {
                    propBag.Init("TiffCompressionMethod");
                    propValue.Init((byte) _compressionMethod);

                    HRESULT.Check(UnsafeNativeMethods.IPropertyBag2.Write(
                        encoderOptions,
                        1,
                        ref propBag,
                        ref propValue));
                }
                finally
                {
                    propBag.Clear();
                    propValue.Clear();
                }
            }

            HRESULT.Check(UnsafeNativeMethods.WICBitmapFrameEncode.Initialize(
                frameEncodeHandle,
                encoderOptions
                ));
        }

        #endregion

        #region Internal Abstract

        /// Need to implement this to derive from the "sealed" object
        internal override void SealObject()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Data Members

        private Guid _containerFormat = MILGuidData.GUID_ContainerFormatTiff;

        private const TiffCompressOption c_defaultCompressionMethod = TiffCompressOption.Default;
        private TiffCompressOption _compressionMethod = c_defaultCompressionMethod;

        #endregion
    }

    #endregion // TiffBitmapEncoder
}


