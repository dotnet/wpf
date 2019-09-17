// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: TextServicesDisplayAttribute
//

using System.Runtime.InteropServices;
using System.Windows.Threading;

using System.Collections;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Documents;
using MS.Win32;
using MS.Internal;

using System;

namespace System.Windows.Documents
{
    //------------------------------------------------------
    //
    //  TextServicesDisplayAttribute class
    //
    //------------------------------------------------------

    /// <summary>
    ///   The helper class to wrap TF_DISPLAYATTRIBUTE.
    /// </summary>
    internal class TextServicesDisplayAttribute
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        internal TextServicesDisplayAttribute(UnsafeNativeMethods.TF_DISPLAYATTRIBUTE attr)
        {
            _attr = attr;
        }

        //------------------------------------------------------
        //
        //  Internal Method
        //
        //------------------------------------------------------

        /// <summary>
        ///   Check if this is empty.
        /// </summary>
        internal bool IsEmptyAttribute()
        {
            if (_attr.crText.type != UnsafeNativeMethods.TF_DA_COLORTYPE.TF_CT_NONE ||
                _attr.crBk.type != UnsafeNativeMethods.TF_DA_COLORTYPE.TF_CT_NONE ||
                _attr.crLine.type != UnsafeNativeMethods.TF_DA_COLORTYPE.TF_CT_NONE ||
                _attr.lsStyle != UnsafeNativeMethods.TF_DA_LINESTYLE.TF_LS_NONE)
                return false;

            return true;
        }

        /// <summary>
        ///   Apply the display attribute to to the given range.
        /// </summary>
        internal void Apply(ITextPointer start, ITextPointer end)
        {
            //
            //   need to support line color and line style.
            //
             
#if NOT_YET
            if (_attr.crText.type != UnsafeNativeMethods.TF_DA_COLORTYPE.TF_CT_NONE)
            {
            }

            if (_attr.crBk.type != UnsafeNativeMethods.TF_DA_COLORTYPE.TF_CT_NONE)
            {
            }

            if (_attr.lsStyle != UnsafeNativeMethods.TF_DA_LINESTYLE.TF_LS_NONE)
            {
            }
#endif

        }

        /// <summary>
        ///   Convert TF_DA_COLOR to Color.
        /// </summary>
        internal static Color GetColor(UnsafeNativeMethods.TF_DA_COLOR dacolor, ITextPointer position)
        {
            if (dacolor.type == UnsafeNativeMethods.TF_DA_COLORTYPE.TF_CT_SYSCOLOR)
            {
                return GetSystemColor(dacolor.indexOrColorRef);
            }
            else if (dacolor.type == UnsafeNativeMethods.TF_DA_COLORTYPE.TF_CT_COLORREF)
            {
                int color = dacolor.indexOrColorRef;
                uint argb = (uint)FromWin32Value(color);
                return Color.FromArgb((byte)((argb & 0xff000000) >> 24), (byte)((argb & 0x00ff0000) >> 16), (byte)((argb & 0x0000ff00) >> 8), (byte)(argb & 0x000000ff));
            }

            Invariant.Assert(position != null, "position can't be null");
            return ((SolidColorBrush)position.GetValue(TextElement.ForegroundProperty)).Color;
        }

        /// <summary>
        /// Line color of the composition line draw
        /// </summary>
        internal Color GetLineColor(ITextPointer position)
        {
            return GetColor(_attr.crLine, position);
        }
        
        /// <summary>
        /// Line style of the composition line draw
        /// </summary>
        internal UnsafeNativeMethods.TF_DA_LINESTYLE LineStyle
        {
            get
            {
                return _attr.lsStyle;
            }
        }

        /// <summary>
        /// Line bold of the composition line draw
        /// </summary>
        internal bool IsBoldLine
        {
            get
            {
                return _attr.fBoldLine;
            }
        }

        internal UnsafeNativeMethods.TF_DA_ATTR_INFO AttrInfo
        {
            get 
            {
                return _attr.bAttr;
            }
        }

        /**
         * Shift count and bit mask for A, R, G, B components
         */
        private const int AlphaShift  = 24;
        private const int RedShift    = 16;
        private const int GreenShift  = 8;
        private const int BlueShift   = 0;

        private const int Win32RedShift    = 0;
        private const int Win32GreenShift  = 8;
        private const int Win32BlueShift   = 16;

        private static int Encode(int alpha, int red, int green, int blue) 
        {
            return red << RedShift | green << GreenShift | blue << BlueShift | alpha << AlphaShift;
        }

        private static int FromWin32Value(int value) 
        {
            return Encode(255,
                (value >> Win32RedShift) & 0xFF,
                (value >> Win32GreenShift) & 0xFF,
                (value >> Win32BlueShift) & 0xFF);
        }

        /// <summary>
        ///     Query for system colors.
        /// </summary>
        /// <param name="index">Same parameter as Win32's GetSysColor</param>
        /// <returns>The system color.</returns>
        private static Color GetSystemColor(int index)
        {
            uint argb;

            int color = SafeNativeMethods.GetSysColor(index);

            argb = (uint)FromWin32Value(color);
            return Color.FromArgb((byte)((argb&0xff000000)>>24), (byte)((argb&0x00ff0000)>>16),(byte)((argb&0x0000ff00)>>8), (byte)(argb&0x000000ff));
        }

        //------------------------------------------------------
        //
        //  Private Field
        //
        //------------------------------------------------------

        private UnsafeNativeMethods.TF_DISPLAYATTRIBUTE _attr;
    }
}
