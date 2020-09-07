// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Helpers to handle text dpi conversions and limitations.
//


using System;                   // Double, ...
using System.Windows;           // DependencyObject
using System.Windows.Documents; // TextElement, ...
using System.Windows.Media;     // FontFamily
using MS.Internal.PtsHost.UnsafeNativeMethods;  // PTS

namespace MS.Internal.Text
{
    // ----------------------------------------------------------------------
    // Helpers to handle text dpi conversions and limitations.
    // ----------------------------------------------------------------------
    internal static class TextDpi
    {
        // ------------------------------------------------------------------
        // Minimum width for text measurement.
        // ------------------------------------------------------------------
        internal static double MinWidth { get { return _minSize; } }

        // ------------------------------------------------------------------
        // Maximum width for text measurement.
        // ------------------------------------------------------------------
        internal static double MaxWidth { get { return _maxSize; } }

        // ------------------------------------------------------------------
        // Convert from logical measurement unit to text measurement unit.
        //
        //      d - value in logical measurement unit
        //
        // Returns: value in text measurement unit.
        // ------------------------------------------------------------------
        internal static int ToTextDpi(double d)
        {
            if (DoubleUtil.IsZero(d)) { return 0; }
            else if (d > 0)
            {
                if (d > _maxSize) { d = _maxSize; }
                else if (d < _minSize) { d = _minSize; }
            }
            else
            {
                if (d < -_maxSize) { d = -_maxSize; }
                else if (d > -_minSize) { d = -_minSize; }
            }
            return (int)Math.Round(d * _scale);
        }

        // ------------------------------------------------------------------
        // Convert from text measurement unit to logical measurement unit.
        //
        //      i - value in text measurement unit
        //
        // Returns: value in logical measurement unit.
        // ------------------------------------------------------------------
        internal static double FromTextDpi(int i)
        {
            return ((double)i) / _scale;
        }

        // ------------------------------------------------------------------
        // Convert point from logical measurement unit to text measurement unit.
        // ------------------------------------------------------------------
        internal static PTS.FSPOINT ToTextPoint(Point point)
        {
            PTS.FSPOINT fspoint = new PTS.FSPOINT();
            fspoint.u = ToTextDpi(point.X);
            fspoint.v = ToTextDpi(point.Y);
            return fspoint;
        }

        // ------------------------------------------------------------------
        // Convert size from logical measurement unit to text measurement unit.
        // ------------------------------------------------------------------
        internal static PTS.FSVECTOR ToTextSize(Size size)
        {
            PTS.FSVECTOR fsvector = new PTS.FSVECTOR();
            fsvector.du = ToTextDpi(size.Width);
            fsvector.dv = ToTextDpi(size.Height);
            return fsvector;
        }

        // ------------------------------------------------------------------
        // Convert rect from text measurement unit to logical measurement unit.
        // ------------------------------------------------------------------
        internal static Rect FromTextRect(PTS.FSRECT fsrect)
        {
            return new Rect(
                FromTextDpi(fsrect.u),
                FromTextDpi(fsrect.v),
                FromTextDpi(fsrect.du),
                FromTextDpi(fsrect.dv));
        }

        // ------------------------------------------------------------------
        // Make sure that LS/PTS limitations are not exceeded for offset
        // within a line.
        // ------------------------------------------------------------------
        internal static void EnsureValidLineOffset(ref double offset)
        {
            // Offset has to be > min allowed size && < max allowed size.
            if (offset > _maxSize) { offset = _maxSize; }
            else if (offset < -_maxSize) { offset = -_maxSize; }
        }

        // ------------------------------------------------------------------
        // Snaps the value to TextDPi, makeing sure that convertion to 
        // TextDpi and the to double produces the same value.
        // ------------------------------------------------------------------
        internal static void SnapToTextDpi(ref Size size)
        {
            size = new Size(FromTextDpi(ToTextDpi(size.Width)), FromTextDpi(ToTextDpi(size.Height)));
        }

        // ------------------------------------------------------------------
        // Make sure that LS/PTS limitations are not exceeded for line width.
        // ------------------------------------------------------------------
        internal static void EnsureValidLineWidth(ref double width)
        {
            // Line width has to be > 0 && < max allowed size.
            if (width > _maxSize) { width = _maxSize; }
            else if (width < _minSize) { width = _minSize; }
        }
        internal static void EnsureValidLineWidth(ref Size size)
        {
            // Line width has to be > 0 && < max allowed size.
            if (size.Width > _maxSize) { size.Width = _maxSize; }
            else if (size.Width < _minSize) { size.Width = _minSize; }
        }
        internal static void EnsureValidLineWidth(ref int width)
        {
            // Line width has to be > 0 && < max allowed size.
            if (width > _maxSizeInt) { width = _maxSizeInt; }
            else if (width < _minSizeInt) { width = _minSizeInt; }
        }

        // ------------------------------------------------------------------
        // Make sure that PTS limitations are not exceeded for page size.
        // ------------------------------------------------------------------
        internal static void EnsureValidPageSize(ref Size size)
        {
            // Page size has to be > 0 && < max allowed size.
            if (size.Width > _maxSize) { size.Width = _maxSize; }
            else if (size.Width < _minSize) { size.Width = _minSize; }
            if (size.Height > _maxSize) { size.Height = _maxSize; }
            else if (size.Height < _minSize) { size.Height = _minSize; }
        }
        internal static void EnsureValidPageWidth(ref double width)
        {
            // Page size has to be > 0 && < max allowed size.
            if (width > _maxSize) { width = _maxSize; }
            else if (width < _minSize) { width = _minSize; }
        }
        internal static void EnsureValidPageMargin(ref Thickness pageMargin, Size pageSize)
        {
            if (pageMargin.Left >= pageSize.Width) { pageMargin.Right = 0.0; }
            if (pageMargin.Left + pageMargin.Right >= pageSize.Width)
            {
                pageMargin.Right = Math.Max(0.0, pageSize.Width - pageMargin.Left - _minSize);
                if (pageMargin.Left + pageMargin.Right >= pageSize.Width)
                {
                    pageMargin.Left = pageSize.Width - _minSize;
                }
            }
            if (pageMargin.Top >= pageSize.Height) { pageMargin.Bottom = 0.0; }
            if (pageMargin.Top + pageMargin.Bottom >= pageSize.Height)
            {
                pageMargin.Bottom = Math.Max(0.0, pageSize.Height - pageMargin.Top - _minSize);;
                if (pageMargin.Top + pageMargin.Bottom >= pageSize.Height)
                {
                    pageMargin.Top = pageSize.Height - _minSize;
                }
            }
        }

        // ------------------------------------------------------------------
        // Make sure that LS/PTS limitations are not exceeded for object's size.
        // ------------------------------------------------------------------
        internal static void EnsureValidObjSize(ref Size size)
        {
            // Embedded object can have size == 0, but its width and height
            // have to be less than max allowed size.
            if (size.Width > _maxObjSize) { size.Width = _maxObjSize; }
            if (size.Height > _maxObjSize) { size.Height = _maxObjSize; }
        }

        // ------------------------------------------------------------------
        // Measuring unit for PTS presenters is int, but logical measuring
        // for the rest of the system is is double.
        // Logical measuring dpi        = 96
        // PTS presenters measuring dpi = 28800
        // ------------------------------------------------------------------
        private const double _scale = 28800.0 / 96; // 300

        // ------------------------------------------------------------------
        // PTS/LS limitation for max size.
        // ------------------------------------------------------------------
        private const int _maxSizeInt = 0x3FFFFFFE;
        private const double _maxSize = ((double)_maxSizeInt) / _scale; // = 3,579,139.40 pixels

        // ------------------------------------------------------------------
        // PTS/LS limitation for min size.
        // ------------------------------------------------------------------
        private const int _minSizeInt = 1;
        private const double _minSize = ((double)_minSizeInt) / _scale; // > 0

        // ------------------------------------------------------------------
        // Embedded object size is limited to 1/3 of max size accepted by LS/PTS.
        // ------------------------------------------------------------------
        private const double _maxObjSize = _maxSize / 3; // = 1,193,046.46 pixels
    }

#if TEXTLAYOUTDEBUG
    // ----------------------------------------------------------------------
    // Pefrormance debugging helpers.
    // ----------------------------------------------------------------------
    internal static class TextDebug
    {
        // ------------------------------------------------------------------
        // Enter the scope and log the message.
        // ------------------------------------------------------------------
        internal static void BeginScope(string name)
        {
            Log(name);
            ++_indent;
        }

        // ------------------------------------------------------------------
        // Exit the current scope.
        // ------------------------------------------------------------------
        internal static void EndScope()
        {
            --_indent;
        }

        // ------------------------------------------------------------------
        // Log message.
        // ------------------------------------------------------------------
        internal static void Log(string msg)
        {
            Console.WriteLine("> " + CurrentIndent + msg);
        }

        // ------------------------------------------------------------------
        // String representing current indent.
        // ------------------------------------------------------------------
        private static string CurrentIndent { get { return new string(' ', _indent * 2); } }

        // ------------------------------------------------------------------
        // Current indent.
        // ------------------------------------------------------------------
        private static int _indent;
    }
#endif // TEXTLAYOUTDEBUG
}
