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

        internal unsafe void Translate(Span<float> srcValue, Span<float> dstValue)
        {
            // Transform colors using TranslateColors
            const UInt32 NumColors = 1;

            // There's no SkipLocalsInit, will be all zeroes
            long* paInputColors = stackalloc long[64 / sizeof(long)];
            long* paOutputColors = stackalloc long[64 / sizeof(long)];

            long inputColor = ICM2Color(srcValue);
            paInputColors[0] = inputColor;

            _colorTransformHelper.TranslateColors((nint)paInputColors, NumColors, _inputColorType, (nint)paOutputColors, _outputColorType);

            long outputColor = paOutputColors[0];
            for (int i = 0; i < dstValue.Length; i++)
            {
                UInt32 result = 0x0000ffff & (UInt32)(outputColor >> (16 * i));
                float a = (result & 0x7fffffff) / (float)0x10000;

                if (result < 0)
                    dstValue[i] = -a;
                else
                    dstValue[i] = a;
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

        private long ICM2Color(Span<float> srcValue)
        {
            long colorValue;

            if (srcValue.Length < 3 || srcValue.Length > 8)
            {
                throw new NotSupportedException(); // Only support color spaces with 3,4,5,6,7,8 channels
            }

            if (srcValue.Length <= 4)
            {
                Span<UInt16> channel = stackalloc UInt16[4];

                for (int i = 0; i < srcValue.Length; i++)
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
                Span<byte> channel = stackalloc byte[8];

                for (int i = 0; i < srcValue.Length; i++)
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

        private readonly UInt32 _inputColorType;

        private readonly UInt32 _outputColorType;

        #endregion
    }
}


