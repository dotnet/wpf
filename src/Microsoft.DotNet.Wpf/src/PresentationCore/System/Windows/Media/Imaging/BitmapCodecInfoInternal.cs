// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//

namespace System.Windows.Media.Imaging
{
    #region BitmapCodecInfoInternal

    /// <summary>
    /// Codec info for a given Encoder/Decoder
    /// </summary>
    internal class BitmapCodecInfoInternal : BitmapCodecInfo
    {
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        private BitmapCodecInfoInternal()
        {
        }

        /// <summary>
        /// Internal Constructor
        /// </summary>
        internal BitmapCodecInfoInternal(SafeMILHandle codecInfoHandle) :
            base(codecInfoHandle)
        {
        }

        #endregion
    }

    #endregion
}
