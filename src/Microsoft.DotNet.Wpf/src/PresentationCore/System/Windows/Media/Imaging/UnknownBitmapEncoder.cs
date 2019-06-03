// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Collections;
using System.Security;
using System.Security.Permissions;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using MS.Win32.PresentationCore;
using System.Diagnostics;
using System.Windows.Media;
using System.Globalization;
using System.Windows.Media.Imaging;

namespace System.Windows.Media.Imaging
{
    #region UnknownBitmapEncoder

    /// <summary>
    /// Built-in Encoder for Unknown files.
    /// </summary>
    internal sealed class UnknownBitmapEncoder : BitmapEncoder
    {
        #region Constructors

        /// <summary>
        /// Constructor for UnknownBitmapEncoder
        /// </summary>
        /// <SecurityNote>
        /// Critical - will eventually create unmanaged resources based on guid
        /// </SecurityNote>
        [SecurityCritical]
        public UnknownBitmapEncoder(Guid containerFormat) :
            base(true)
        {
            _containerFormat = containerFormat;

            // Assume it supports everything
            _supportsPreview = true;
            _supportsGlobalThumbnail = true;
            _supportsGlobalMetadata = false;
            _supportsFrameThumbnails = true;
            _supportsMultipleFrames = true;
            _supportsFrameMetadata = true;
        }

        #endregion

        #region Internal Properties / Methods

        /// <summary>
        /// Returns the container format for this encoder
        /// </summary>
        /// <SecurityNote>
        /// Critical - uses guid to create unmanaged resources
        /// </SecurityNote>
        internal override Guid ContainerFormat
        {
            [SecurityCritical]
            get
            {
                return _containerFormat;
            }
        }

        /// <summary>
        /// Setups the encoder and other properties before encoding each frame
        /// </summary>
        /// <SecurityNote>
        /// Critical - Accesses unmanaged code
        /// TreatAsSafe - All parameters passed in are safe (null, 0 and safehandle)
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal override void SetupFrame(SafeMILHandle frameEncodeHandle, SafeMILHandle encoderOptions)
        {
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

        /// <SecurityNote>
        /// Critical - CLSID used for creation of critical resources
        /// </SecurityNote>
        [SecurityCritical]
        private Guid _containerFormat;

        #endregion
    }

    #endregion // UnknownBitmapEncoder
}


