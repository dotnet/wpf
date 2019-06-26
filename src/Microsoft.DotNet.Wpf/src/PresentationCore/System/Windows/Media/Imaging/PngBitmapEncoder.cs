// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Security;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using MS.Win32.PresentationCore;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Windows.Media.Imaging
{
    #region PngInterlaceOption

    /// <summary>
    /// Possibile options for the interlaced setting.
    /// </summary>
    public enum PngInterlaceOption : int
    {
        /// <summary>
        /// Let the encoder decide what is best.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Save as an interlaced bitmap.
        /// </summary>
        On = 1,

        /// <summary>
        /// Do not save as an interlaced bitmap.
        /// </summary>
        Off = 2,
    }

    #endregion

    #region PngBitmapEncoder

    /// <summary>
    /// Built-in Encoder for Png files.
    /// </summary>
    public sealed class PngBitmapEncoder : BitmapEncoder
    {
        #region Constructors

        /// <summary>
        /// Constructor for PngBitmapEncoder
        /// </summary>
        public PngBitmapEncoder() :
            base(true)
        {
            _supportsPreview = false;
            _supportsGlobalThumbnail = false;
            _supportsGlobalMetadata = false;
            _supportsFrameThumbnails = false;
            _supportsMultipleFrames = false;
            _supportsFrameMetadata = true;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Encode this bitmap as interlaced.
        /// </summary>
        public PngInterlaceOption Interlace
        {
            get
            {
                return _interlaceOption;
            }
            set
            {
                _interlaceOption = value;
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
        /// Setups the encoder and other properties before encoding each frame
        /// </summary>
        internal override void SetupFrame(SafeMILHandle frameEncodeHandle, SafeMILHandle encoderOptions)
        {
            PROPBAG2 propBag = new PROPBAG2();
            PROPVARIANT propValue = new PROPVARIANT();

            // There is only one encoder option supported here:

            if (_interlaceOption != c_defaultInterlaceOption)
            {
                try
                {
                    propBag.Init("InterlaceOption");
                    propValue.Init(_interlaceOption == PngInterlaceOption.On);

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

        private Guid _containerFormat = MILGuidData.GUID_ContainerFormatPng;

        private const PngInterlaceOption c_defaultInterlaceOption = PngInterlaceOption.Default;
        private PngInterlaceOption _interlaceOption = c_defaultInterlaceOption;

        #endregion
    }

    #endregion // PngBitmapEncoder
}


