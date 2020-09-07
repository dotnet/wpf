// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;

namespace System.Windows.Media.Imaging
{
    /// <summary>
    /// BitmapCreateOptions are used to specify various optimizations options.
    /// These options currently include whether or not to preserve the pixel format
    /// specified in the file, and whether or not to fully initialize the backing
    /// store object now, or later.
    /// </summary>
    [Flags]
    public enum BitmapCreateOptions
    {
        /// <summary>
        /// The default is none of the options turned on
        /// </summary>
        None = 0,

        /// <summary>
        /// PreservePixelFormat specifies whether or to guarantee the pixelformat
        /// the file is stored is the same one the image is loaded in. If the flag is
        /// off, the format the image is loaded will be picked by the system depending
        /// on what it determines will yeild the best performance. Turning the flag on
        /// will preserve the file format but may result in degraded performance.
        /// </summary>
        PreservePixelFormat = 1,

        /// <summary>
        /// DelayCreation tells the BitmapImage object whether or not to fully
        /// initialize itself now, or to wait until it actually has to. This is
        /// usefull when dealing with collections of images.
        /// </summary>
        DelayCreation = 2,

        /// <summary>
        /// IgnoreColorProfile tells the BitmapSource to ignore the embedded
        /// color profile. Any use of APIs such as CopyPixels will
        /// not return color corrected bits if this option is used.
        /// </summary>
        IgnoreColorProfile = 4,

        /// <summary>
        /// IgnoreImageCache loads any images without using the existing image cache.
        /// This should be used when images in the cache need to be refreshed. This option
        /// will replace any exisiting entries in the cache that are created using the same
        /// Uris.
        /// </summary>
        IgnoreImageCache = 8
    }
}
