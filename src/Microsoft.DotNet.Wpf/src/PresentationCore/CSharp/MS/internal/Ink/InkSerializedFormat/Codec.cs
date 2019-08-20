// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Utility;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Ink;
using MS.Internal.Ink.InkSerializedFormat;


using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Ink.InkSerializedFormat
{
    /// <summary>
    /// A Math helper class.
    /// </summary>
    internal static class MathHelper
    {
        /// <summary>
        /// Returns the absolute value of a 32-bit signed integer.
        /// Unlike Math.Abs, this method doesn't throw OverflowException 
        /// when the signed integer equals int.MinValue (-2,147,483,648/0x80000000).
        /// It will return the same value (-2,147,483,648). 
        /// In this case, value can be casted to unsigned value which will be positive (2,147,483,648)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static int AbsNoThrow(int data)
        {
            // This behavior is desired for ISF decoder. Please refer to the below macro in old native code (codec.h).
            //  template{typename DataType}
            //  inline DataType Abs(DataType data) { return (data < 0) ? -data : data; };
            return (data < 0) ? -data : data;
        }

        /// <summary>
        /// Returns the absolute value of a 64-bit signed integer.
        /// Unlike Math.Abs, this method doesn't throw OverflowException 
        /// when the signed integer equals int.MinValue (-9,223,372,036,854,775,808/0x8000000000000000).
        /// It will return the same value -9,223,372,036,854,775,808 instead.
        /// In this case, value can be casted to unsigned value which will be positive (9,223,372,036,854,775,808)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal static long AbsNoThrow(long data)
        {
            // This behavior is desired for ISF decoder. Please refer to the below macro in old native code (codec.h).
            //  template{typename DataType}
            //  inline DataType Abs(DataType data) { return (data < 0) ? -data : data; };
            return (data < 0) ? -data : data;
        }
    }

    /// <summary>
    /// Abstact base class for DeltaDelta and some others
    /// </summary>
    internal abstract class DataXform
    {
        internal abstract void Transform(int data, ref int xfData, ref int extra);
        internal abstract void ResetState();
        internal abstract int InverseTransform(int xfData, int extra);
    }
    /// <summary>
    /// Oddly named because we have unmanged code we keep in sync with this that 
    /// has this name.
    /// </summary>
    internal class DeltaDelta : DataXform
    {
        private long _d_i_1 = 0;
        private long _d_i_2 = 0;

        internal DeltaDelta()
        {
        }

        /// <summary>
        /// Your guess is as good as mine
        /// </summary>
        /// <param name="data"></param>
        /// <param name="xfData"></param>
        /// <param name="extra"></param>
        internal override void Transform(int data, ref int xfData, ref int extra)
        {
            // Find out the delta delta of the number
            // Its absolute value could potentially be more than LONG_MAX
            long llxfData = (data + _d_i_2 - (_d_i_1 << 1));
            // Save the state info for next number
            _d_i_2 = _d_i_1;
            _d_i_1 = data;
            // Most of the cases, the delta delta will be less than LONG_MAX
            if ( Int32.MaxValue >= MathHelper.AbsNoThrow(llxfData) )
            {
                // In those cases, we set 0 to nExtra and 
                // assign the delta delta to xfData
                extra = 0;
                xfData = (int)llxfData;
            }
            else
            {
                long absLxfData = MathHelper.AbsNoThrow(llxfData);
                // Additional bits in most significant 32 bits
                extra = (int)(absLxfData >> (sizeof(int) << 3));
                // Left sift one bit and append sign bit the the LSB
                extra = (extra << 1) | ((llxfData < 0) ? 1 : 0);
                // Save least significant 32 bits in xfData
                xfData = (int)((unchecked((uint)~0 & absLxfData)));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal override void ResetState()
        {
            _d_i_1 = 0;
            _d_i_2 = 0;
        }

        /// <summary>
        /// Your guess is as good as mine
        /// </summary>
        /// <param name="xfData"></param>
        /// <param name="extra"></param>
        /// <returns></returns>
        internal override int InverseTransform(int xfData, int extra)
        {
            long llxfData;
            // Find out whether the original delta delta exceeded the limit
            if (0 != extra)
            {
                // Yes, we had |delta delta| more than LONG_MAX
                // Find out the original delta delta was negative
                bool negative = ((extra & 0x01) != 0);
                // Construct the |DelDel| from xfData and nExtra
                llxfData = (((long)extra >> 1) << (sizeof(int) << 3)) | (unchecked((uint)~0) & xfData);
                // Do the sign adjustment
                llxfData = (negative) ? -llxfData : llxfData;
            }
            else
            {
                llxfData = xfData;
            }
            // Reconstruct the number from delta delta
            long orgData = (llxfData - _d_i_2 + (_d_i_1 << 1));
            _d_i_2 = _d_i_1;
            _d_i_1 = orgData;
            // Typecast to LONG and return it
            return (int)orgData;
        }
    }
}
