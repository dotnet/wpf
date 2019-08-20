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

using UnsafeNativeMethods=MS.Win32.PresentationCore.UnsafeNativeMethods;

namespace System.Windows.Media
{
    ///<summary>
    /// </summary>
    internal class ColorTransform
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        private ColorTransform()
        {
        }

        internal ColorTransform(ColorContext srcContext, ColorContext dstContext)
        {
            InitializeICM();

            if (srcContext == null)
            {
                srcContext = new ColorContext(PixelFormats.Bgra32);
            }

            if (dstContext == null)
            {
                dstContext = new ColorContext(PixelFormats.Bgra32);
            }

            _inputColorType = srcContext.ColorType;
            _outputColorType = dstContext.ColorType;

            _colorTransformHelper.CreateTransform(srcContext.ProfileHandle, dstContext.ProfileHandle);
        }

        internal ColorTransform(SafeMILHandle bitmapSource, ColorContext srcContext, ColorContext dstContext, System.Windows.Media.PixelFormat pixelFormat)
        {
            InitializeICM();

            if (srcContext == null)
            {
                srcContext = new ColorContext(pixelFormat);
            }
            if (dstContext == null)
            {
                dstContext = new ColorContext(pixelFormat);
            }

            _inputColorType = srcContext.ColorType;
            _outputColorType = dstContext.ColorType;

            //if this failed or the handle is invalid, we can't continue
            if (srcContext.ProfileHandle != null && !srcContext.ProfileHandle.IsInvalid)
            {
                //if this failed or the handle is invalid, we can't continue
                if (dstContext.ProfileHandle != null && !dstContext.ProfileHandle.IsInvalid)
                {
                    _colorTransformHelper.CreateTransform(srcContext.ProfileHandle, dstContext.ProfileHandle);
                }
            }
        }
        #endregion

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        #endregion

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal void Translate(float[] srcValue, float[] dstValue)
        {
            // 3. create Win32 unmanaged profile handle from memory source profile using OpenColorProfileW
            IntPtr[] pProfiles = new IntPtr[2];

            IntPtr paInputColors = IntPtr.Zero;
            IntPtr paOutputColors = IntPtr.Zero;

            try
            {
                // 6. transform colors using TranslateColors
                UInt32 numColors = 1;
                long inputColor = ICM2Color(srcValue);
                paInputColors = Marshal.AllocHGlobal(64);

                Marshal.WriteInt64(paInputColors, inputColor);

                paOutputColors = Marshal.AllocHGlobal(64);
                long outputColor = 0;

                Marshal.WriteInt64(paOutputColors, outputColor);

                _colorTransformHelper.TranslateColors(
                    (IntPtr)paInputColors,
                    numColors,
                    _inputColorType,
                    (IntPtr)paOutputColors,
                    _outputColorType
                    );

                outputColor = Marshal.ReadInt64(paOutputColors);
                for (int i = 0; i < dstValue.GetLength(0); i++)
                {
                    UInt32 result = 0x0000ffff & (UInt32)(outputColor >> (16 * i));
                    float a = (result & 0x7fffffff) / (float)(0x10000);

                    if (result < 0)
                        dstValue[i] = -a;
                    else
                        dstValue[i] = a;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(paInputColors);
                Marshal.FreeHGlobal(paOutputColors);
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private void InitializeICM()
        {
            _colorTransformHelper = new ColorTransformHelper();
        }

        private long ICM2Color(float[] srcValue)
        {
            long colorValue;

            if (srcValue.GetLength(0) < 3 || srcValue.GetLength(0) > 8)
            {
                throw new NotSupportedException(); // Only support color spaces with 3,4,5,6,7,8 channels
            }

            if (srcValue.GetLength(0) <= 4)
            {
                UInt16[] channel = new UInt16[4];

                channel[0] = channel[1] = channel[2] = channel[3] = 0;
                for (int i = 0; i < srcValue.GetLength(0); i++)
                {
                    if (srcValue[i] >= 1.0)// this fails for values above 1.0 and below 0.0
                    {
                        channel[i] = 0xffff;
                    }
                    else if (srcValue[i] <= 0.0)
                    {
                        channel[i] = 0x0;
                    }
                    else
                    {
                        channel[i] = (UInt16)(srcValue[i] * (float)0xFFFF);
                    }
                }

                colorValue = (long)(((UInt64)channel[3] << 48) + ((UInt64)channel[2] << 32) + ((UInt64)channel[1] << 16) + (UInt64)channel[0]);
            }
            else
            {
                byte[] channel = new byte[8];

                channel[0] = channel[1] = channel[2] = channel[3] =
                        channel[4] = channel[5] = channel[6] = channel[7] = 0;
                for (int i = 0; i < srcValue.GetLength(0); i++)
                {
                    if (srcValue[i] >= 1.0)// this fails for values above 1.0 and below 0.0
                    {
                        channel[i] = 0xff;
                    }
                    else if (srcValue[i] <= 0.0)
                    {
                        channel[i] = 0x0;
                    }
                    else
                    {
                        channel[i] = (byte)(srcValue[i] * (float)0xFF);
                    }
                }

                colorValue = (long)(((UInt64)channel[7] << 56) + ((UInt64)channel[6] << 48) +
                                    ((UInt64)channel[5] << 40) + ((UInt64)channel[4] << 32) +
                                    ((UInt64)channel[3] << 24) + ((UInt64)channel[2] << 16) +
                                    ((UInt64)channel[1] << 8) + ((UInt64)channel[0] << 0));
            }
            return colorValue;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private ColorTransformHelper _colorTransformHelper;

        private UInt32 _inputColorType;

        private UInt32 _outputColorType;

        #endregion
    }
}


