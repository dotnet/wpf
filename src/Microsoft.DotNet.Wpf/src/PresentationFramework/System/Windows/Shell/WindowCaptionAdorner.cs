// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Standard;

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Shell
#else
namespace Microsoft.Windows.Shell
#endif
{
    /// <summary>
    /// Draws the window icon and title over the caption band of a window whose client area has been extended
    /// into the frame (<see cref="WindowChromeFrameMode.ExtendedClientArea"/>).  DWM does not draw them in that
    /// mode because the caption is suppressed with WTNCA_NODRAWCAPTION | WTNCA_NODRAWICON.
    /// </summary>
    internal sealed class WindowCaptionAdorner : Adorner
    {
        private const double IconMargin = 8.0;
        private const double TextMargin = 8.0;
        private const int ICON_SMALL2 = 2;

        private readonly Window _window;
        private readonly Func<Rect> _getCaptionBand;
        private BitmapSource _hiconFallback;

        /// <param name="adornedElement">The element hosting the adorner layer (the child of the window's AdornerDecorator).</param>
        /// <param name="window">The window whose title and icon are drawn.</param>
        /// <param name="getCaptionBand">Returns the caption band in window coordinates (DIPs).</param>
        public WindowCaptionAdorner(UIElement adornedElement, Window window, Func<Rect> getCaptionBand)
            : base(adornedElement)
        {
            _window = window;
            _getCaptionBand = getCaptionBand;
            IsHitTestVisible = false;
            Focusable = false;
            SnapsToDevicePixels = true;
        }

        public bool ShowTitle { get; set; }
        public bool ShowIcon { get; set; }

        /// <summary>
        /// Where the icon was last drawn, in window coordinates (DIPs), or <see cref="Rect.Empty"/> when no
        /// icon is shown.  The chrome worker answers WM_NCHITTEST with HTSYSMENU over it, so the system gives
        /// the icon its standard behavior (system menu on click, close on double click).
        /// </summary>
        public Rect IconBounds { get; private set; } = Rect.Empty;

        /// <summary>Drops any cached icon so the next render re-queries the HWND.</summary>
        public void InvalidateIcon()
        {
            _hiconFallback = null;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            IconBounds = Rect.Empty;

            if (!ShowTitle && !ShowIcon)
            {
                return;
            }

            Rect windowBand = _getCaptionBand();
            if (windowBand.IsEmpty || windowBand.Width <= 0 || windowBand.Height <= 0)
            {
                return;
            }

            // The band is expressed in window coordinates; the adorner renders in the coordinate space of the
            // adorned element, which is usually offset by the window border. After a template change the adorned
            // element may no longer belong to the window: TransformToDescendant would throw, so draw nothing.
            if (!_window.IsAncestorOf(AdornedElement))
            {
                return;
            }

            Rect captionBand = _window.TransformToDescendant(AdornedElement).TransformBounds(windowBand);

            bool rtl = _window.FlowDirection == FlowDirection.RightToLeft;
            double x = rtl ? captionBand.Right - IconMargin : captionBand.Left + IconMargin;

            if (ShowIcon)
            {
                ImageSource icon = _GetIcon();
                if (icon != null)
                {
                    double iconWidth = SystemParameters.SmallIconWidth;
                    double iconHeight = SystemParameters.SmallIconHeight;
                    double iconX = rtl ? x - iconWidth : x;
                    double iconY = captionBand.Top + Math.Max(0, (captionBand.Height - iconHeight) / 2);
                    drawingContext.DrawImage(icon, new Rect(iconX, iconY, iconWidth, iconHeight));
                    x = rtl ? iconX - TextMargin : iconX + iconWidth + TextMargin;

                    // Same rect, back in window coordinates for the hit test.
                    double windowIconX = rtl ? windowBand.Right - IconMargin - iconWidth : windowBand.Left + IconMargin;
                    double windowIconY = windowBand.Top + Math.Max(0, (windowBand.Height - iconHeight) / 2);
                    IconBounds = new Rect(windowIconX, windowIconY, iconWidth, iconHeight);
                }
            }

            if (ShowTitle)
            {
                string title = _window.Title;
                if (string.IsNullOrEmpty(title))
                {
                    return;
                }

                double available = rtl ? x - captionBand.Left - TextMargin : captionBand.Right - x - TextMargin;
                if (available <= 0)
                {
                    return;
                }

                Brush foreground = WindowChrome.GetCaptionForeground(_window) ?? SystemColors.ActiveCaptionTextBrush;
                var typeface = new Typeface(
                    SystemFonts.CaptionFontFamily,
                    SystemFonts.CaptionFontStyle,
                    SystemFonts.CaptionFontWeight,
                    FontStretches.Normal);

                var text = new FormattedText(
                    title,
                    CultureInfo.CurrentUICulture,
                    _window.FlowDirection,
                    typeface,
                    SystemFonts.CaptionFontSize,
                    foreground,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip)
                {
                    MaxTextWidth = available,
                    MaxLineCount = 1,
                    Trimming = TextTrimming.CharacterEllipsis,
                };

                double textY = captionBand.Top + Math.Max(0, (captionBand.Height - text.Height) / 2);
                double textX = rtl ? x - text.Width : x;
                drawingContext.DrawText(text, new Point(textX, textY));
            }
        }

        private ImageSource _GetIcon()
        {
            ImageSource icon = _window.Icon;
            if (icon != null)
            {
                return icon;
            }

            if (_hiconFallback == null)
            {
                IntPtr hwnd = new WindowInteropHelper(_window).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    IntPtr hIcon = _FindWindowIcon(hwnd);
                    if (hIcon != IntPtr.Zero)
                    {
                        try
                        {
                            _hiconFallback = Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            _hiconFallback.Freeze();
                        }
                        catch (Exception e) when (e is ArgumentException || e is ComponentModel.Win32Exception)
                        {
                            _hiconFallback = null;
                        }
                    }
                }
            }

            return _hiconFallback;
        }

        /// <summary>
        /// The icon the system would draw in the caption: the one the application set on the window (Window sets
        /// the one embedded in the executable), then the window class icons, then the stock application icon.
        /// </summary>
        private static IntPtr _FindWindowIcon(IntPtr hwnd)
        {
            const int ICON_SMALL = 0;
            const int ICON_BIG = 1;

            foreach (int which in new[] { ICON_SMALL2, ICON_SMALL, ICON_BIG })
            {
                IntPtr hIcon = NativeMethods.SendMessage(hwnd, WM.GETICON, new IntPtr(which), IntPtr.Zero);
                if (hIcon != IntPtr.Zero)
                {
                    return hIcon;
                }
            }

            IntPtr classIcon = NativeMethods.GetClassLongPtr(hwnd, GCLP.HICONSM);
            if (classIcon == IntPtr.Zero)
            {
                classIcon = NativeMethods.GetClassLongPtr(hwnd, GCLP.HICON);
            }

            if (classIcon != IntPtr.Zero)
            {
                return classIcon;
            }

            return NativeMethods.LoadDefaultApplicationIcon();
        }
    }
}
