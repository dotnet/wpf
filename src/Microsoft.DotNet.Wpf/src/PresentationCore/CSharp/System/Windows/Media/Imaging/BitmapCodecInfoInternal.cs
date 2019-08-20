// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using MS.Internal;
using MS.Win32;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Media.Imaging;
using System.Text;

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
