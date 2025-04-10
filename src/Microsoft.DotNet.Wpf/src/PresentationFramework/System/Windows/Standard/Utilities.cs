// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// This file contains general utilities to aid in development.
// Classes here generally shouldn't be exposed publicly since
// they're not particular to any library functionality.
// Because the classes here are internal, it's likely this file
// might be included in multiple assemblies.

namespace Standard
{
    internal static partial class Utility
    {
        private static readonly Version _osVersion = Environment.OSVersion.Version;

        /// <summary>Convert a native integer that represent a color with an alpha channel into a Color struct.</summary>
        /// <param name="color">The integer that represents the color.  Its bits are of the format 0xAARRGGBB.</param>
        /// <returns>A Color representation of the parameter.</returns>
        public static Color ColorFromArgbDword(uint color)
        {
            return Color.FromArgb(
                (byte)((color & 0xFF000000) >> 24),
                (byte)((color & 0x00FF0000) >> 16),
                (byte)((color & 0x0000FF00) >> 8),
                (byte)((color & 0x000000FF) >> 0));
        }
        
        public static int GET_X_LPARAM(IntPtr lParam)
        {
            // Avoid overflow for negative coordinates https://github.com/dotnet/wpf/issues/6777
            return LOWORD((int) lParam.ToInt64());
        }

        
        public static int GET_Y_LPARAM(IntPtr lParam)
        {
            // Avoid overflow for negative coordinates https://github.com/dotnet/wpf/issues/6777
            return HIWORD((int) lParam.ToInt64());
        }
        
        public static int HIWORD(int i)
        {
            return (short)(i >> 16);
        }
        
        public static int LOWORD(int i)
        {
            return (short)(i & 0xFFFF);
        }
        
        public static bool IsFlagSet(int value, int mask)
        {
            return 0 != (value & mask);
        }     
        public static bool IsFlagSet(uint value, uint mask)
        {
            return 0 != (value & mask);
        }     
        public static bool IsFlagSet(long value, long mask)
        {
            return 0 != (value & mask);
        }     
        public static bool IsFlagSet(ulong value, ulong mask)
        {
            return 0 != (value & mask);
        }
        public static bool IsOSVistaOrNewer
        {
            get { return _osVersion >= new Version(6, 0); }
        }     
        public static bool IsOSWindows7OrNewer
        {
            get { return _osVersion >= new Version(6, 1); }
        }

        /// <summary>
        /// Whether the operating system version is greater than or equal to 6.2.
        /// </summary>
        public static bool IsOSWindows8OrNewer => _osVersion >= new Version(6, 2);

        /// <summary>
        /// Whether the operating system version is greater than or equal to 10.0* (build 10240).
        /// </summary>
        public static bool IsOSWindows10OrNewer => _osVersion.Build >= 10240;

        /// <summary>
        /// Whether the operating system version is greater than or equal to 11.0* (build 22000).
        /// </summary>
        public static bool IsOSWindows11OrNewer => _osVersion.Build >= 22000;

        /// <summary>
        /// Whether the operating system version is greater than or equal to 11.0* (build 22621).
        /// </summary>
        public static bool IsWindows11_22H2OrNewer => _osVersion.Build >= 22621;

        public static BitmapFrame GetBestMatch(IList<BitmapFrame> frames, int width, int height)
        {
            return _GetBestMatch(frames, _GetBitDepth(), width, height);
        }

        private static int _MatchImage(BitmapFrame frame, int bitDepth, int width, int height, int bpp)
        {
            int score = 2 * _WeightedAbs(bpp, bitDepth, false) +
                    _WeightedAbs(frame.PixelWidth, width, true) +
                    _WeightedAbs(frame.PixelHeight, height, true);

            return score;
        }

        private static int _WeightedAbs(int valueHave, int valueWant, bool fPunish)
        {
            int diff = (valueHave - valueWant);

            if (diff < 0)
            {
                diff = (fPunish ? -2 : -1) * diff;
            }

            return diff;
        }

        /// From a list of BitmapFrames find the one that best matches the requested dimensions.
        /// The methods used here are copied from Win32 sources.  We want to be consistent with
        /// system behaviors.
        private static BitmapFrame _GetBestMatch(IList<BitmapFrame> frames, int bitDepth, int width, int height)
        {
            int bestScore = int.MaxValue;
            int bestBpp = 0;
            int bestIndex = 0;

            bool isBitmapIconDecoder = frames[0].Decoder is IconBitmapDecoder;

            for (int i = 0; i < frames.Count && bestScore != 0; ++i)
            {
                int currentIconBitDepth = isBitmapIconDecoder ? frames[i].Thumbnail.Format.BitsPerPixel : frames[i].Format.BitsPerPixel;

                if (currentIconBitDepth == 0)
                {
                    currentIconBitDepth = 8;
                }

                int score = _MatchImage(frames[i], bitDepth, width, height, currentIconBitDepth);
                if (score < bestScore)
                {
                    bestIndex = i;
                    bestBpp = currentIconBitDepth;
                    bestScore = score;
                }
                else if (score == bestScore)
                {
                    // Tie breaker: choose the higher color depth.  If that fails, choose first one.
                    if (bestBpp < currentIconBitDepth)
                    {
                        bestIndex = i;
                        bestBpp = currentIconBitDepth;
                    }
                }
            }

            return frames[bestIndex];
        }

        // This can be cached.  It's not going to change under reasonable circumstances.
        private static int s_bitDepth; // = 0;
        
        private static int _GetBitDepth()
        {
            if (s_bitDepth == 0)
            {
                using (SafeDC dc = SafeDC.GetDesktop())
                {
                    s_bitDepth = NativeMethods.GetDeviceCaps(dc, DeviceCap.BITSPIXEL) * NativeMethods.GetDeviceCaps(dc, DeviceCap.PLANES);
                }
            }
            return s_bitDepth;
        }

        /// <summary>GDI's DeleteObject</summary>   
        public static void SafeDeleteObject(ref IntPtr gdiObject)
        {
            IntPtr p = gdiObject;
            gdiObject = IntPtr.Zero;
            if (IntPtr.Zero != p)
            {
                NativeMethods.DeleteObject(p);
            }
        }
    
        public static void SafeDestroyWindow(ref IntPtr hwnd)
        {
            IntPtr p = hwnd;
            hwnd = IntPtr.Zero;
            if (NativeMethods.IsWindow(p))
            {
                NativeMethods.DestroyWindow(p);
            }
        }
        
        public static void SafeRelease<T>(ref T comObject) where T : class
        {
            T t = comObject;
            comObject = default(T);
            if (null != t)
            {
                Assert.IsTrue(Marshal.IsComObject(t));
                Marshal.ReleaseComObject(t);
            }
        }

        public static void AddDependencyPropertyChangeListener(object component, DependencyProperty property, EventHandler listener)
        {
            if (component == null)
            {
                return;
            }
            Assert.IsNotNull(property);
            Assert.IsNotNull(listener);

            DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(property, component.GetType());
            dpd.AddValueChanged(component, listener);
        }

        public static void RemoveDependencyPropertyChangeListener(object component, DependencyProperty property, EventHandler listener)
        {
            if (component == null)
            {
                return;
            }
            Assert.IsNotNull(property);
            Assert.IsNotNull(listener);

            DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(property, component.GetType());
            dpd.RemoveValueChanged(component, listener);
        }

        #region Extension Methods

        public static bool IsThicknessNonNegative(Thickness thickness)
        {
            if (!IsDoubleFiniteAndNonNegative(thickness.Top))
            {
                return false;
            }

            if (!IsDoubleFiniteAndNonNegative(thickness.Left))
            {
                return false;
            }

            if (!IsDoubleFiniteAndNonNegative(thickness.Bottom))
            {
                return false;
            }

            if (!IsDoubleFiniteAndNonNegative(thickness.Right))
            {
                return false;
            }

            return true;
        }

        public static bool IsCornerRadiusValid(CornerRadius cornerRadius)
        {
            if (!IsDoubleFiniteAndNonNegative(cornerRadius.TopLeft))
            {
                return false;
            }

            if (!IsDoubleFiniteAndNonNegative(cornerRadius.TopRight))
            {
                return false;
            }

            if (!IsDoubleFiniteAndNonNegative(cornerRadius.BottomLeft))
            {
                return false;
            }

            if (!IsDoubleFiniteAndNonNegative(cornerRadius.BottomRight))
            {
                return false;
            }

            return true;
        }

        public static bool IsDoubleFiniteAndNonNegative(double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d) || d < 0)
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}
