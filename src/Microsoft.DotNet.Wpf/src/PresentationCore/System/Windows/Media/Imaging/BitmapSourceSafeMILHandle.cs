// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description:
//      A sub-class of SafeMILHandle that can estimate size for bitmap
//      source objects.

using System;
using System.Diagnostics;
using System.Security;
using MS.Internal;
using MS.Win32;

using UnsafeNativeMethods=MS.Win32.PresentationCore.UnsafeNativeMethods;

namespace System.Windows.Media.Imaging
{
    /// <summary>
    /// Constructor which computes size of the handle and delegates
    /// to baseclass safe handle.
    /// </summary>

    internal class BitmapSourceSafeMILHandle : SafeMILHandle
    {
        static BitmapSourceSafeMILHandle() { }

        /// <summary>
        /// Use this constructor if the handle isn't ready yet and later
        /// set the handle with SetHandle.
        /// </summary>
        internal BitmapSourceSafeMILHandle() : base()
        {
        }

        /// <summary>
        /// Use this constructor if the handle exists at construction time.
        /// SafeMILHandle owns the release of the parameter.
        /// </summary>
        internal BitmapSourceSafeMILHandle(IntPtr handle) : base()
        {
            SetHandle(handle);
        }

        /// <summary>
        /// Use this constructor if the handle exists at construction time and memory pressure
        /// should be shared with another SafeMILHandle.
        /// SafeMILHandle owns the release of the parameter.
        /// </summary>
        internal BitmapSourceSafeMILHandle(IntPtr handle, SafeMILHandle copyMemoryPressureFrom)
            : this(handle)
        {
            CopyMemoryPressure(copyMemoryPressureFrom);
        }

        /// <summary>
        /// Calculate the rough size for this handle
        /// </summary>
        internal void CalculateSize()
        {
            UpdateEstimatedSize(ComputeEstimatedSize(handle));
        }

        /// <summary>
        /// Compute a rough estimate of the size in bytes for the image
        /// </summary>
        private static long ComputeEstimatedSize(IntPtr bitmapObject)
        {
            long estimatedSize = 0;

            if (bitmapObject != IntPtr.Zero)
            {
                IntPtr wicBitmap;

                //
                // QueryInterface for the bitmap source to ensure we are
                // calling through the right vtable on the pinvoke.
                //

                int hr = UnsafeNativeMethods.MILUnknown.QueryInterface(
                    bitmapObject,
                    ref _uuidBitmap,
                    out wicBitmap
                    );

                if (hr == HRESULT.S_OK)
                {
                    Debug.Assert(wicBitmap != IntPtr.Zero);

                    //
                    // The safe handle will release the ref added by the above QI
                    //
                    // There's no need to copy memory pressure. Partly because this SafeMILHandle
                    // is temporary and will be collected after this method returns, partly
                    // because there might no memory pressure calculated yet.
                    //
                    SafeMILHandle bitmapSourceSafeHandle = new SafeMILHandle(wicBitmap);

                    uint pixelWidth = 0;
                    uint pixelHeight = 0;

                    hr = UnsafeNativeMethods.WICBitmapSource.GetSize(
                        bitmapSourceSafeHandle,
                        out pixelWidth,
                        out pixelHeight);

                    if (hr == HRESULT.S_OK)
                    {
                        Guid guidFormat;

                        hr = UnsafeNativeMethods.WICBitmapSource.GetPixelFormat(bitmapSourceSafeHandle, out guidFormat);
                        if (hr == HRESULT.S_OK)
                        {
                            //
                            // Go to long space to avoid overflow and check for overflow
                            //
                            PixelFormat pixelFormat = new PixelFormat(guidFormat);

                            long scanlineSize = (long)pixelWidth * pixelFormat.InternalBitsPerPixel / 8;

                            //
                            // Check that scanlineSize is small enough that we can multiply by pixelHeight
                            // without an overflow.  Since pixelHeight is a 32-bit value and we multiply by pixelHeight,
                            // then we can only have a 32-bit scanlineSize.  Since we need a sign bit as well,
                            // we need to check that scanlineSize can fit in 30 bits.
                            //

                            if (scanlineSize < 0x40000000)
                            {
                                estimatedSize = pixelHeight * scanlineSize;
                            }
                        }
                    }
                }
            }

            return estimatedSize;
        }

        /// <summary>
        /// This is overridden to prevent JIT'ing due to new behavior in the CLR. (#708970)
        /// </summary>
        protected override bool ReleaseHandle()
        {
            return base.ReleaseHandle();
        }

        /// <summary>
        /// Guid for IWICBitmapSource
        /// </summary>
        private static Guid _uuidBitmap = MILGuidData.IID_IWICBitmapSource;
}
}
