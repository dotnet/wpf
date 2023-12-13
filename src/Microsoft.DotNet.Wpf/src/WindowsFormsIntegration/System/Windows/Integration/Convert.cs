// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using MS.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;

using SD = System.Drawing;
using SDI = System.Drawing.Imaging;
using SWF = System.Windows.Forms;

using SW = System.Windows;
using SWI = System.Windows.Input;
using SWM = System.Windows.Media;
using SWMI = System.Windows.Media.Imaging;

namespace System.Windows.Forms.Integration
{
    ///<summary>
    ///     Converters from System.Drawing types and WPF types
    ///</summary>
    internal static class Convert
    {
        internal const float systemDrawingPixelsPerInch = 72.0f;
        internal const float systemWindowsPixelsPerInch = 96.0f;

        private static Dictionary<SWF.Cursor, SWI.Cursor> _toSystemWindowsInputCursorDictionary;
        private static Dictionary<SWF.Cursor, SWI.Cursor> ToSystemWindowsInputCursorDictionary
        {
            get
            {
                if (_toSystemWindowsInputCursorDictionary == null)
                {
                    _toSystemWindowsInputCursorDictionary = new Dictionary<SWF.Cursor, System.Windows.Input.Cursor>();
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.AppStarting, SWI.Cursors.AppStarting);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.Arrow, SWI.Cursors.Arrow);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.Cross, SWI.Cursors.Cross);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.Hand, SWI.Cursors.Hand);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.Help, SWI.Cursors.Help);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.HSplit, SWI.Cursors.Arrow); //No equivalent
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.IBeam, SWI.Cursors.IBeam);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.No, SWI.Cursors.No);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.NoMove2D, SWI.Cursors.ScrollAll);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.NoMoveHoriz, SWI.Cursors.ScrollWE);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.NoMoveVert, SWI.Cursors.ScrollNS);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.PanEast, SWI.Cursors.ScrollE);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.PanNorth, SWI.Cursors.ScrollN);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.PanNE, SWI.Cursors.ScrollNE);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.PanNW, SWI.Cursors.ScrollNW);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.PanSouth, SWI.Cursors.ScrollS);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.PanSE, SWI.Cursors.ScrollSE);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.PanSW, SWI.Cursors.ScrollSW);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.PanWest, SWI.Cursors.ScrollW);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.SizeAll, SWI.Cursors.SizeAll);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.SizeNESW, SWI.Cursors.SizeNESW);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.SizeNS, SWI.Cursors.SizeNS);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.SizeNWSE, SWI.Cursors.SizeNWSE);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.SizeWE, SWI.Cursors.SizeWE);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.UpArrow, SWI.Cursors.UpArrow);
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.VSplit, SWI.Cursors.Arrow); //No equivalent
                    _toSystemWindowsInputCursorDictionary.Add(SWF.Cursors.WaitCursor, SWI.Cursors.Wait);
                }
                return _toSystemWindowsInputCursorDictionary;
            }
        }

        private static Dictionary<SWI.Cursor, SWF.Cursor> _toSystemWindowsFormsCursorDictionary;
        private static Dictionary<SWI.Cursor, SWF.Cursor> ToSystemWindowsFormsCursorDictionary
        {
            get
            {
                if (_toSystemWindowsFormsCursorDictionary == null)
                {
                    _toSystemWindowsFormsCursorDictionary = new Dictionary<SWI.Cursor, SWF.Cursor>();
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.AppStarting, SWF.Cursors.AppStarting);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.Arrow, SWF.Cursors.Arrow);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.Cross, SWF.Cursors.Cross);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.Hand, SWF.Cursors.Hand);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.Help, SWF.Cursors.Help);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.IBeam, SWF.Cursors.IBeam);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.No, SWF.Cursors.No);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.None, SWF.Cursors.Default);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.ScrollAll, SWF.Cursors.NoMove2D);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.ScrollWE, SWF.Cursors.NoMoveHoriz);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.ScrollNS, SWF.Cursors.NoMoveVert);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.ScrollE, SWF.Cursors.PanEast);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.ScrollN, SWF.Cursors.PanNorth);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.ScrollNE, SWF.Cursors.PanNE);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.ScrollNW, SWF.Cursors.PanNW);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.ScrollS, SWF.Cursors.PanSouth);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.ScrollSE, SWF.Cursors.PanSE);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.ScrollSW, SWF.Cursors.PanSW);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.ScrollW, SWF.Cursors.PanWest);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.SizeAll, SWF.Cursors.SizeAll);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.SizeNESW, SWF.Cursors.SizeNESW);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.SizeNS, SWF.Cursors.SizeNS);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.SizeNWSE, SWF.Cursors.SizeNWSE);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.SizeWE, SWF.Cursors.SizeWE);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.UpArrow, SWF.Cursors.UpArrow);
                    _toSystemWindowsFormsCursorDictionary.Add(SWI.Cursors.Wait, SWF.Cursors.WaitCursor);

                }
                return _toSystemWindowsFormsCursorDictionary;
            }
        }

        /// <summary>
        ///     Converts between a System.Windows.Forms.Cursor and a System.Windows.Input.Cursor
        /// </summary>
        internal static SWI.Cursor ToSystemWindowsInputCursor(SWF.Cursor swfCursor)
        {
            SWI.Cursor swiCursor;

            if (ToSystemWindowsInputCursorDictionary.TryGetValue(swfCursor, out swiCursor))
            {
                return swiCursor;
            }
            return SWI.Cursors.Arrow;
        }

        /// <summary>
        ///     Converts between a System.Windows.Input.Cursor and a System.Windows.Forms.Cursor
        /// </summary>
        internal static SWF.Cursor ToSystemWindowsFormsCursor(SWI.Cursor swiCursor)
        {
            SWF.Cursor swfCursor;
            if (swiCursor != null && ToSystemWindowsFormsCursorDictionary.TryGetValue(swiCursor, out swfCursor))
            {
                return swfCursor;
            }
            return SWF.Cursors.Default;
        }

        /// <summary>
        ///     Converts between a System.Drawing.Image and System.Windows.Media.Imaging.BitmapImage
        /// </summary>
        internal static SWMI.BitmapImage ToSystemWindowsMediaImagingBitmapImage(SD.Image fromImage)
        {
            if (fromImage == null)
            {
                return null;
            }

            SWMI.BitmapImage newImage = null;
            System.IO.MemoryStream stream = new System.IO.MemoryStream();

            //Use the same output format that the Image is in, unless it is a MemoryBmp,
            //in which case use a regular Bmp.
            SDI.ImageFormat IntermediateFormat = fromImage.RawFormat;
            if (IntermediateFormat.Guid.CompareTo(SDI.ImageFormat.MemoryBmp.Guid) == 0)
            {
                IntermediateFormat = SDI.ImageFormat.Bmp;
            }

            fromImage.Save(stream, IntermediateFormat);
            stream.Seek(0, System.IO.SeekOrigin.Begin);

            newImage = new SWMI.BitmapImage();
            newImage.BeginInit();
            newImage.StreamSource = stream;
            newImage.EndInit();

            return newImage;
        }

        /// <summary>
        ///     Converts from a System.Windows.Forms.Message to a System.Windows.Interop.MSG
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        internal static System.Windows.Interop.MSG ToSystemWindowsInteropMSG(SWF.Message msg)
        {
            SW.Interop.MSG msg2 = new SW.Interop.MSG();

            msg2.hwnd = msg.HWnd;
            msg2.lParam = msg.LParam;
            msg2.message = msg.Msg;
            msg2.pt_x = 0;
            msg2.pt_y = 0;
            msg2.time = MS.Win32.SafeNativeMethods.GetMessageTime();
            msg2.wParam = msg.WParam;
            return msg2;
        }

        /// <summary>
        ///     Converts from a System.Windows.Forms.Keys to a System.Windows.Input.ModifierKeys
        /// </summary>
        /// <param name="keyData"></param>
        /// <returns></returns>
        internal static SWI.ModifierKeys ToSystemWindowsInputModifierKeys(SWF.Keys keyData)
        {
            SWI.ModifierKeys modifiers = new System.Windows.Input.ModifierKeys();

            if ((keyData & SWF.Keys.Alt) == SWF.Keys.Alt)
                modifiers |= SWI.ModifierKeys.Alt;
            if ((keyData & SWF.Keys.Control) == SWF.Keys.Control)
                modifiers |= SWI.ModifierKeys.Control;
            if ((keyData & SWF.Keys.Shift) == SWF.Keys.Shift)
                modifiers |= SWI.ModifierKeys.Shift;
            if (((keyData & SWF.Keys.LWin) == SWF.Keys.LWin) ||
                ((keyData & SWF.Keys.RWin) == SWF.Keys.RWin))
            {
                modifiers |= SWI.ModifierKeys.Windows;
            }

            return modifiers;
        }

        internal static double SystemDrawingFontToSystemWindowsFontSize(SD.Font font)
        {
            return font.SizeInPoints * Convert.systemWindowsPixelsPerInch / Convert.systemDrawingPixelsPerInch;
        }

        internal static double FontSizeToSystemDrawing(double initialSize)
        {
            return initialSize * Convert.systemDrawingPixelsPerInch / Convert.systemWindowsPixelsPerInch;
        }

        internal static SWM.FontFamily ToSystemWindowsFontFamily(SD.FontFamily sdFamily)
        {
            //NOTE: if this throws an exception, it will get picked up by the PropertyMappingError event.
            return new SWM.FontFamily(sdFamily.Name);
        }

        internal static FontWeight ToSystemWindowsFontWeight(SD.Font sdFont)
        {
            return sdFont.Bold ? FontWeights.Bold : FontWeights.Normal;
        }

        internal static FontStyle ToSystemWindowsFontStyle(SD.Font sdFont)
        {
            return sdFont.Italic ? FontStyles.Italic : FontStyles.Normal;
        }

        internal static Size ToSystemWindowsSize(SD.Size size, Vector scale)
        {
            Size returnSize = new Size((double)size.Width, (double)size.Height);
            // Adjust for WFH scaling

            returnSize.Width  /= ScaleFactor(scale.X, Orientation.Horizontal);
            returnSize.Height /= ScaleFactor(scale.Y, Orientation.Vertical);

            return returnSize;
        }

        internal static Size ToSystemWindowsSize(SD.Size size, Vector scale, double dpiScaleX, double dpiScaleY)
        {
            Size returnSize = new Size((double)size.Width, (double)size.Height);
            // Adjust for WFH scaling

            returnSize.Width /= ScaleFactor(scale.X, Orientation.Horizontal, dpiScaleX);
            returnSize.Height /= ScaleFactor(scale.Y, Orientation.Vertical, dpiScaleY);

            return returnSize;
        }

        internal static SD.Size ToSystemDrawingSize(Size size, Vector scale)
        {
            size.Width *= ScaleFactor(scale.X, Orientation.Horizontal);
            size.Height *= ScaleFactor(scale.Y, Orientation.Vertical);
            return new SD.Size(Convert.ToBoundedInt(size.Width),
                Convert.ToBoundedInt(size.Height));
        }

        internal static SD.Size ToSystemDrawingSize(Size size, Vector scale, double dpiScaleX, double dpiScaleY)
        {
            size.Width *= ScaleFactor(scale.X, Orientation.Horizontal, dpiScaleX);
            size.Height *= ScaleFactor(scale.Y, Orientation.Vertical, dpiScaleY);
            return new SD.Size(Convert.ToBoundedInt(size.Width),
                Convert.ToBoundedInt(size.Height));
        }

        private static double ScaleFactor(double value, Orientation orientation)
        {
            //This is a guard against possible unset scale value (which shouldn't happen)
            if (HostUtils.IsZero(value)) { value = 1.0; }

            return value * HostUtils.PixelsPerInch(orientation) / systemWindowsPixelsPerInch;
        }

        private static double ScaleFactor(double value, Orientation orientation, double dpiScale)
        {
            //This is a guard against possible unset scale value (which shouldn't happen)
            if (HostUtils.IsZero(value)) { value = 1.0; }

            return value * dpiScale;
        }

        internal static SD.Size ConstraintToSystemDrawingSize(Size size, Vector scale, double dpiScaleX, double dpiScaleY)
        {
            // Convert WPF constraint to a System.Drawing.Size.  For SD.Size, 0 represents unconstrained,
            // while for SW.Size, this is represented by double.PositiveInfinity
            if (size.Width  == double.PositiveInfinity) { size.Width  = 0; }
            if (size.Height == double.PositiveInfinity) { size.Height = 0; }
            return ToSystemDrawingSize(size, scale, dpiScaleX, dpiScaleY);
        }

        internal static Padding ToSystemWindowsFormsPadding(Thickness thickness)
        {
            return new Padding(ToBoundedInt(thickness.Left),
                                    ToBoundedInt(thickness.Top),
                                    ToBoundedInt(thickness.Right),
                                    ToBoundedInt(thickness.Bottom));
        }

        internal static int ToBoundedInt(double value)
        {
            if (value > int.MaxValue)
            {
                return int.MaxValue;
            }
            if (value < int.MinValue)
            {
                return int.MinValue;
            }
            return (int)value;
        }

        ///<summary>
        ///     Converts between a System.Windows.Color and a System.Drawing.Color
        ///</summary>
        internal static SD.Color ToSystemDrawingColor(SWM.Color color)
        {
            return SD.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        /// <summary>
        ///     Converts from a System.Windows.Interop.MSG to a System.Windows.Forms.Message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        internal static SWF.Message ToSystemWindowsFormsMessage(System.Windows.Interop.MSG msg)
        {
            SWF.Message msg2 = new SWF.Message();

            msg2.HWnd = msg.hwnd;
            msg2.LParam = msg.lParam;
            msg2.Msg = msg.message;
            msg2.WParam = msg.wParam;

            return msg2;
        }
    }
}
