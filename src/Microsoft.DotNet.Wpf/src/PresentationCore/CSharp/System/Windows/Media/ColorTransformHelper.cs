// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MS.Internal;
using MS.Win32;
using System.Security;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Win32.SafeHandles;
using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using UnsafeNativeMethods=MS.Win32.PresentationCore.UnsafeNativeMethods;

namespace System.Windows.Media
{
    #region ColorTransformHandle

    internal class ColorTransformHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Use this constructor if the handle exists at construction time.
        /// </summary>
        internal ColorTransformHandle()
            : base(true)
        {
        }

        /// <summary>
        /// Use this constructor if the handle exists at construction time.
        /// </summary>
        internal ColorTransformHandle(IntPtr profile)
            : base(true)
        {
            SetHandle(profile);
        }

        protected override bool ReleaseHandle()
        {
            return UnsafeNativeMethods.Mscms.DeleteColorTransform(handle);
        }
    }

    #endregion

    #region ColorTransformHelper

    /// <summary>
    /// Class to call into MSCMS color transform APIs
    /// </summary>
    internal class ColorTransformHelper
    {
        /// Constructor
        internal ColorTransformHelper()
        {
        }

        /// Creates an ICM Profile Transform
        /// Retrieves a standard color space profile
        internal void CreateTransform(SafeProfileHandle sourceProfile, SafeProfileHandle destinationProfile)
        {
            if (sourceProfile == null || sourceProfile.IsInvalid)
            {
                throw new ArgumentNullException("sourceProfile");
            }

            if (destinationProfile == null || destinationProfile.IsInvalid)
            {
                throw new ArgumentNullException("destinationProfile");
            }

            IntPtr[] handles = new IntPtr[2];
            bool success = true;

            sourceProfile.DangerousAddRef(ref success);
            Debug.Assert(success);
            destinationProfile.DangerousAddRef(ref success);
            Debug.Assert(success);

            try
            {
                handles[0] = sourceProfile.DangerousGetHandle();
                handles[1] = destinationProfile.DangerousGetHandle();

                UInt32[] dwIntents = new UInt32[2] {INTENT_PERCEPTUAL, INTENT_PERCEPTUAL};

                // No need to get rid of the old handle as it will get GC'ed
                _transformHandle = UnsafeNativeMethods.Mscms.CreateMultiProfileTransform(
                    handles,
                    (UInt32)handles.Length,
                    dwIntents,
                    (UInt32)dwIntents.Length,
                    BEST_MODE | USE_RELATIVE_COLORIMETRIC,
                    0
                    );
            }
            finally
            {
                sourceProfile.DangerousRelease();
                destinationProfile.DangerousRelease();
            }

            if (_transformHandle == null || _transformHandle.IsInvalid)
            {
                HRESULT.Check(Marshal.GetHRForLastWin32Error());
            }
        }

        /// Translates the colors
        /// Retrieves a standard color space profile
        internal void TranslateColors(IntPtr paInputColors, UInt32 numColors, UInt32 inputColorType, IntPtr paOutputColors, UInt32 outputColorType)
        {
            if (_transformHandle == null || _transformHandle.IsInvalid)
            {
                throw new InvalidOperationException(SR.Get(SRID.Image_ColorTransformInvalid));
            }

            HRESULT.Check(UnsafeNativeMethods.Mscms.TranslateColors(
                _transformHandle,
                paInputColors,
                numColors,
                inputColorType,
                paOutputColors,
                outputColorType));
        }


        #region Data members

        /// Handle for the ICM Color Transform
        private ColorTransformHandle _transformHandle;

        /// Intents
        private const UInt32 INTENT_PERCEPTUAL = 0;
        private const UInt32 INTENT_RELATIVE_COLORIMETRIC = 1;
        private const UInt32 INTENT_SATURATION = 2;
        private const UInt32 INTENT_ABSOLUTE_COLORIMETRIC = 3;

        /// Flags for create color transform
        private const UInt32 PROOF_MODE = 0x00000001;
        private const UInt32 NORMAL_MODE = 0x00000002;
        private const UInt32 BEST_MODE = 0x00000003;
        private const UInt32 ENABLE_GAMUT_CHECKING = 0x00010000;
        private const UInt32 USE_RELATIVE_COLORIMETRIC = 0x00020000;
        private const UInt32 FAST_TRANSLATE = 0x00040000;

        #endregion
    }

    #endregion
}


