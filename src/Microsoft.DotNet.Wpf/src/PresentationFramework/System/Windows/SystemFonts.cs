// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Security;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;
using MS.Win32;
using MS.Internal;

namespace System.Windows
{
    /// <summary>
    ///     Contains properties that are queries into the system's various settings.
    /// </summary>
    public static class SystemFonts
    {

        #region Fonts

        /// <summary>
        ///     Maps to SPI_GETICONTITLELOGFONT
        /// </summary>
        public static double IconFontSize
        {
            get
            {
                return ConvertFontHeight(SystemParameters.IconMetrics.lfFont.lfHeight);
            }
        }

        /// <summary>
        ///     Maps to SPI_GETICONTITLELOGFONT
        /// </summary>
        public static FontFamily IconFontFamily
        {
            get
            {
                if (_iconFontFamily == null)
                {
                    _iconFontFamily = new FontFamily(SystemParameters.IconMetrics.lfFont.lfFaceName);
                }

                return _iconFontFamily;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETICONTITLELOGFONT
        /// </summary>
        public static FontStyle IconFontStyle
        {
            get
            {
                return (SystemParameters.IconMetrics.lfFont.lfItalic != 0) ? FontStyles.Italic : FontStyles.Normal;
            }
        }

        /// <summary>
        ///     Maps to SPI_GETICONTITLELOGFONT
        /// </summary>
        public static FontWeight IconFontWeight
        {
            get
            {
                return FontWeight.FromOpenTypeWeight(SystemParameters.IconMetrics.lfFont.lfWeight);
            }
        }

        /// <summary>
        ///     Maps to SPI_GETICONTITLELOGFONT
        /// </summary>
        public static TextDecorationCollection IconFontTextDecorations
        {
            get
            {
                if (_iconFontTextDecorations == null)
                {
                    _iconFontTextDecorations = new TextDecorationCollection();

                    if (SystemParameters.IconMetrics.lfFont.lfUnderline != 0)
                    {
                        CopyTextDecorationCollection(TextDecorations.Underline, _iconFontTextDecorations);
                    }

                    if (SystemParameters.IconMetrics.lfFont.lfStrikeOut != 0)
                    {
                        CopyTextDecorationCollection(TextDecorations.Strikethrough, _iconFontTextDecorations);
                    }

                    _iconFontTextDecorations.Freeze();
                }

                return _iconFontTextDecorations;
            }
        }

        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static double CaptionFontSize
        {
            get
            {
                return ConvertFontHeight(SystemParameters.NonClientMetrics.lfCaptionFont.lfHeight);
            }
        }


        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static FontFamily CaptionFontFamily
        {
            get
            {
                if (_captionFontFamily == null)
                {
                    _captionFontFamily = new FontFamily(SystemParameters.NonClientMetrics.lfCaptionFont.lfFaceName);
                }

                return _captionFontFamily;
            }
        }


        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static FontStyle CaptionFontStyle
        {
            get
            {
                return (SystemParameters.NonClientMetrics.lfCaptionFont.lfItalic != 0) ? FontStyles.Italic : FontStyles.Normal;
            }
        }


        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static FontWeight CaptionFontWeight
        {
            get
            {
                return FontWeight.FromOpenTypeWeight(SystemParameters.NonClientMetrics.lfCaptionFont.lfWeight);
            }
        }


        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static TextDecorationCollection CaptionFontTextDecorations
        {
            get
            {
                if (_captionFontTextDecorations == null)
                {
                    _captionFontTextDecorations = new TextDecorationCollection();

                    if (SystemParameters.NonClientMetrics.lfCaptionFont.lfUnderline != 0)
                    {
                        CopyTextDecorationCollection(TextDecorations.Underline, _captionFontTextDecorations);
                    }

                    if (SystemParameters.NonClientMetrics.lfCaptionFont.lfStrikeOut != 0)
                    {
                        CopyTextDecorationCollection(TextDecorations.Strikethrough, _captionFontTextDecorations);
                    }
                    _captionFontTextDecorations.Freeze();
                }

                return _captionFontTextDecorations;
            }
        }

        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static double SmallCaptionFontSize
        {
            get
            {
                return ConvertFontHeight(SystemParameters.NonClientMetrics.lfSmCaptionFont.lfHeight);
            }
        }


        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static FontFamily SmallCaptionFontFamily
        {
            get
            {
                if (_smallCaptionFontFamily == null)
                {
                    _smallCaptionFontFamily = new FontFamily(SystemParameters.NonClientMetrics.lfSmCaptionFont.lfFaceName);
                }

                return _smallCaptionFontFamily;
            }
        }


        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static FontStyle SmallCaptionFontStyle
        {
            get
            {
                return (SystemParameters.NonClientMetrics.lfSmCaptionFont.lfItalic != 0) ? FontStyles.Italic : FontStyles.Normal;
            }
        }


        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static FontWeight SmallCaptionFontWeight
        {
            get
            {
                return FontWeight.FromOpenTypeWeight(SystemParameters.NonClientMetrics.lfSmCaptionFont.lfWeight);
            }
        }


        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static TextDecorationCollection SmallCaptionFontTextDecorations
        {
            get
            {
                if (_smallCaptionFontTextDecorations == null)
                {
                   _smallCaptionFontTextDecorations = new TextDecorationCollection();

                    if (SystemParameters.NonClientMetrics.lfSmCaptionFont.lfUnderline != 0)
                    {
                        CopyTextDecorationCollection(TextDecorations.Underline, _smallCaptionFontTextDecorations);
                    }

                    if (SystemParameters.NonClientMetrics.lfSmCaptionFont.lfStrikeOut != 0)
                    {
                        CopyTextDecorationCollection(TextDecorations.Strikethrough, _smallCaptionFontTextDecorations);
                    }

                    _smallCaptionFontTextDecorations.Freeze();
                }

                return _smallCaptionFontTextDecorations;
            }
        }

        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static double MenuFontSize
        {
            get
            {
                return ConvertFontHeight(SystemParameters.NonClientMetrics.lfMenuFont.lfHeight);
            }
        }

        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static FontFamily MenuFontFamily
        {
            get
            {
                if (_menuFontFamily == null)
                {
                    _menuFontFamily = new FontFamily(SystemParameters.NonClientMetrics.lfMenuFont.lfFaceName);
                }

                return _menuFontFamily;
            }
        }

        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static FontStyle MenuFontStyle
        {
            get
            {
                return (SystemParameters.NonClientMetrics.lfMenuFont.lfItalic != 0) ? FontStyles.Italic : FontStyles.Normal;
            }
        }

        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static FontWeight MenuFontWeight
        {
            get
            {
                return FontWeight.FromOpenTypeWeight(SystemParameters.NonClientMetrics.lfMenuFont.lfWeight);
            }
        }

        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static TextDecorationCollection MenuFontTextDecorations
        {
            get
            {
                if (_menuFontTextDecorations == null)
                {
                   _menuFontTextDecorations = new TextDecorationCollection();

                    if (SystemParameters.NonClientMetrics.lfMenuFont.lfUnderline != 0)
                    {
                        CopyTextDecorationCollection(TextDecorations.Underline, _menuFontTextDecorations);
                    }

                    if (SystemParameters.NonClientMetrics.lfMenuFont.lfStrikeOut != 0)
                    {
                        CopyTextDecorationCollection(TextDecorations.Strikethrough, _menuFontTextDecorations);
                    }
                    _menuFontTextDecorations.Freeze();
                }

                return _menuFontTextDecorations;
            }
        }

        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static double StatusFontSize
        {
            get
            {
                return ConvertFontHeight(SystemParameters.NonClientMetrics.lfStatusFont.lfHeight);
            }
        }


        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static FontFamily StatusFontFamily
        {
            get
            {
                if (_statusFontFamily == null)
                {
                    _statusFontFamily = new FontFamily(SystemParameters.NonClientMetrics.lfStatusFont.lfFaceName);
                }

                return _statusFontFamily;
            }
        }


        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static FontStyle StatusFontStyle
        {
            get
            {
                return (SystemParameters.NonClientMetrics.lfStatusFont.lfItalic != 0) ? FontStyles.Italic : FontStyles.Normal;
            }
        }


        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static FontWeight StatusFontWeight
        {
            get
            {
                return FontWeight.FromOpenTypeWeight(SystemParameters.NonClientMetrics.lfStatusFont.lfWeight);
            }
        }


        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static TextDecorationCollection StatusFontTextDecorations
        {
            get
            {

                if (_statusFontTextDecorations == null)
                {
                   _statusFontTextDecorations = new TextDecorationCollection();

                    if (SystemParameters.NonClientMetrics.lfStatusFont.lfUnderline!= 0)
                    {
                        CopyTextDecorationCollection(TextDecorations.Underline, _statusFontTextDecorations);
                    }

                    if (SystemParameters.NonClientMetrics.lfStatusFont.lfStrikeOut != 0)
                    {
                        CopyTextDecorationCollection(TextDecorations.Strikethrough, _statusFontTextDecorations);
                    }
                    _statusFontTextDecorations.Freeze();
                }

                return _statusFontTextDecorations;
            }
        }

        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static double MessageFontSize
        {
            get
            {
                return ConvertFontHeight(SystemParameters.NonClientMetrics.lfMessageFont.lfHeight);
            }
        }


        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static FontFamily MessageFontFamily
        {
            get
            {
                if (_messageFontFamily == null)
                {
                    _messageFontFamily = new FontFamily(SystemParameters.NonClientMetrics.lfMessageFont.lfFaceName);
                }

                return _messageFontFamily;
            }
        }


        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static FontStyle MessageFontStyle
        {
            get
            {
                return (SystemParameters.NonClientMetrics.lfMessageFont.lfItalic != 0) ? FontStyles.Italic : FontStyles.Normal;
            }
        }

        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static FontWeight MessageFontWeight
        {
            get
            {
                return FontWeight.FromOpenTypeWeight(SystemParameters.NonClientMetrics.lfMessageFont.lfWeight);
            }
        }

        /// <summary>
        ///     Maps to SPI_NONCLIENTMETRICS
        /// </summary>
        public static TextDecorationCollection MessageFontTextDecorations
        {
            get
            {
                if (_messageFontTextDecorations == null)
                {
                    _messageFontTextDecorations = new TextDecorationCollection();

                    if (SystemParameters.NonClientMetrics.lfMessageFont.lfUnderline != 0)
                    {
                        CopyTextDecorationCollection(TextDecorations.Underline, _messageFontTextDecorations);
                    }

                    if (SystemParameters.NonClientMetrics.lfMessageFont.lfStrikeOut != 0)
                    {
                        CopyTextDecorationCollection(TextDecorations.Strikethrough, _messageFontTextDecorations);
                    }
                    _messageFontTextDecorations.Freeze();
                }

                return _messageFontTextDecorations;
            }
        }

        private static void CopyTextDecorationCollection(TextDecorationCollection from, TextDecorationCollection to)
        {

            int count = from.Count;
            for (int i = 0; i < count; ++i)
            {
                to.Add(from[i]);
            }
        }

        #endregion

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static SystemResourceKey CreateInstance(SystemResourceKeyID KeyId)
        {
            return new SystemResourceKey(KeyId);
        }

        #region Keys

        /// <summary>
        ///     IconFontSize System Resource Key
        /// </summary>
        public static ResourceKey IconFontSizeKey
        {
            get
            {
                if (_cacheIconFontSize == null)
                {
                    _cacheIconFontSize = CreateInstance(SystemResourceKeyID.IconFontSize);
                }

                return _cacheIconFontSize;
            }
        }

        /// <summary>
        ///     IconFontFamily System Resource Key
        /// </summary>
        public static ResourceKey IconFontFamilyKey
        {
            get
            {
                if (_cacheIconFontFamily == null)
                {
                    _cacheIconFontFamily = CreateInstance(SystemResourceKeyID.IconFontFamily);
                }

                return _cacheIconFontFamily;
            }
        }

        /// <summary>
        ///     IconFontStyle System Resource Key
        /// </summary>
        public static ResourceKey IconFontStyleKey
        {
            get
            {
                if (_cacheIconFontStyle == null)
                {
                    _cacheIconFontStyle = CreateInstance(SystemResourceKeyID.IconFontStyle);
                }

                return _cacheIconFontStyle;
            }
        }

        /// <summary>
        ///     IconFontWeight System Resource Key
        /// </summary>
        public static ResourceKey IconFontWeightKey
        {
            get
            {
                if (_cacheIconFontWeight == null)
                {
                    _cacheIconFontWeight = CreateInstance(SystemResourceKeyID.IconFontWeight);
                }

                return _cacheIconFontWeight;
            }
        }

        /// <summary>
        ///     IconFontTextDecorations System Resource Key
        /// </summary>
        public static ResourceKey IconFontTextDecorationsKey
        {
            get
            {
                if (_cacheIconFontTextDecorations == null)
                {
                    _cacheIconFontTextDecorations = CreateInstance(SystemResourceKeyID.IconFontTextDecorations);
                }

                return _cacheIconFontTextDecorations;
            }
        }




        /// <summary>
        ///     CaptionFontSize System Resource Key
        /// </summary>
        public static ResourceKey CaptionFontSizeKey
        {
            get
            {
                if (_cacheCaptionFontSize == null)
                {
                    _cacheCaptionFontSize = CreateInstance(SystemResourceKeyID.CaptionFontSize);
                }

                return _cacheCaptionFontSize;
            }
        }

        /// <summary>
        ///     CaptionFontFamily System Resource Key
        /// </summary>
        public static ResourceKey CaptionFontFamilyKey
        {
            get
            {
                if (_cacheCaptionFontFamily == null)
                {
                    _cacheCaptionFontFamily = CreateInstance(SystemResourceKeyID.CaptionFontFamily);
                }

                return _cacheCaptionFontFamily;
            }
        }

        /// <summary>
        ///     CaptionFontStyle System Resource Key
        /// </summary>
        public static ResourceKey CaptionFontStyleKey
        {
            get
            {
                if (_cacheCaptionFontStyle == null)
                {
                    _cacheCaptionFontStyle = CreateInstance(SystemResourceKeyID.CaptionFontStyle);
                }

                return _cacheCaptionFontStyle;
            }
        }

        /// <summary>
        ///     CaptionFontWeight System Resource Key
        /// </summary>
        public static ResourceKey CaptionFontWeightKey
        {
            get
            {
                if (_cacheCaptionFontWeight == null)
                {
                    _cacheCaptionFontWeight = CreateInstance(SystemResourceKeyID.CaptionFontWeight);
                }

                return _cacheCaptionFontWeight;
            }
        }

        /// <summary>
        ///     CaptionFontTextDecorations System Resource Key
        /// </summary>
        public static ResourceKey CaptionFontTextDecorationsKey
        {
            get
            {
                if (_cacheCaptionFontTextDecorations == null)
                {
                    _cacheCaptionFontTextDecorations = CreateInstance(SystemResourceKeyID.CaptionFontTextDecorations);
                }

                return _cacheCaptionFontTextDecorations;
            }
        }

        /// <summary>
        ///     SmallCaptionFontSize System Resource Key
        /// </summary>
        public static ResourceKey SmallCaptionFontSizeKey
        {
            get
            {
                if (_cacheSmallCaptionFontSize == null)
                {
                    _cacheSmallCaptionFontSize = CreateInstance(SystemResourceKeyID.SmallCaptionFontSize);
                }

                return _cacheSmallCaptionFontSize;
            }
        }

        /// <summary>
        ///     SmallCaptionFontFamily System Resource Key
        /// </summary>
        public static ResourceKey SmallCaptionFontFamilyKey
        {
            get
            {
                if (_cacheSmallCaptionFontFamily == null)
                {
                    _cacheSmallCaptionFontFamily = CreateInstance(SystemResourceKeyID.SmallCaptionFontFamily);
                }

                return _cacheSmallCaptionFontFamily;
            }
        }

        /// <summary>
        ///     SmallCaptionFontStyle System Resource Key
        /// </summary>
        public static ResourceKey SmallCaptionFontStyleKey
        {
            get
            {
                if (_cacheSmallCaptionFontStyle == null)
                {
                    _cacheSmallCaptionFontStyle = CreateInstance(SystemResourceKeyID.SmallCaptionFontStyle);
                }

                return _cacheSmallCaptionFontStyle;
            }
        }

        /// <summary>
        ///     SmallCaptionFontWeight System Resource Key
        /// </summary>
        public static ResourceKey SmallCaptionFontWeightKey
        {
            get
            {
                if (_cacheSmallCaptionFontWeight == null)
                {
                    _cacheSmallCaptionFontWeight = CreateInstance(SystemResourceKeyID.SmallCaptionFontWeight);
                }

                return _cacheSmallCaptionFontWeight;
            }
        }

        /// <summary>
        ///     SmallCaptionFontTextDecorations System Resource Key
        /// </summary>
        public static ResourceKey SmallCaptionFontTextDecorationsKey
        {
            get
            {
                if (_cacheSmallCaptionFontTextDecorations == null)
                {
                    _cacheSmallCaptionFontTextDecorations = CreateInstance(SystemResourceKeyID.SmallCaptionFontTextDecorations);
                }

                return _cacheSmallCaptionFontTextDecorations;
            }
        }

        /// <summary>
        ///     MenuFontSize System Resource Key
        /// </summary>
        public static ResourceKey MenuFontSizeKey
        {
            get
            {
                if (_cacheMenuFontSize == null)
                {
                    _cacheMenuFontSize = CreateInstance(SystemResourceKeyID.MenuFontSize);
                }

                return _cacheMenuFontSize;
            }
        }

        /// <summary>
        ///     MenuFontFamily System Resource Key
        /// </summary>
        public static ResourceKey MenuFontFamilyKey
        {
            get
            {
                if (_cacheMenuFontFamily == null)
                {
                    _cacheMenuFontFamily = CreateInstance(SystemResourceKeyID.MenuFontFamily);
                }

                return _cacheMenuFontFamily;
            }
        }

        /// <summary>
        ///     MenuFontStyle System Resource Key
        /// </summary>
        public static ResourceKey MenuFontStyleKey
        {
            get
            {
                if (_cacheMenuFontStyle == null)
                {
                    _cacheMenuFontStyle = CreateInstance(SystemResourceKeyID.MenuFontStyle);
                }

                return _cacheMenuFontStyle;
            }
        }

        /// <summary>
        ///     MenuFontWeight System Resource Key
        /// </summary>
        public static ResourceKey MenuFontWeightKey
        {
            get
            {
                if (_cacheMenuFontWeight == null)
                {
                    _cacheMenuFontWeight = CreateInstance(SystemResourceKeyID.MenuFontWeight);
                }

                return _cacheMenuFontWeight;
            }
        }

        /// <summary>
        ///     MenuFontTextDecorations System Resource Key
        /// </summary>
        public static ResourceKey MenuFontTextDecorationsKey
        {
            get
            {
                if (_cacheMenuFontTextDecorations == null)
                {
                    _cacheMenuFontTextDecorations = CreateInstance(SystemResourceKeyID.MenuFontTextDecorations);
                }

                return _cacheMenuFontTextDecorations;
            }
        }

        /// <summary>
        ///     StatusFontSize System Resource Key
        /// </summary>
        public static ResourceKey StatusFontSizeKey
        {
            get
            {
                if (_cacheStatusFontSize == null)
                {
                    _cacheStatusFontSize = CreateInstance(SystemResourceKeyID.StatusFontSize);
                }

                return _cacheStatusFontSize;
            }
        }

        /// <summary>
        ///     StatusFontFamily System Resource Key
        /// </summary>
        public static ResourceKey StatusFontFamilyKey
        {
            get
            {
                if (_cacheStatusFontFamily == null)
                {
                    _cacheStatusFontFamily = CreateInstance(SystemResourceKeyID.StatusFontFamily);
                }

                return _cacheStatusFontFamily;
            }
        }

        /// <summary>
        ///     StatusFontStyle System Resource Key
        /// </summary>
        public static ResourceKey StatusFontStyleKey
        {
            get
            {
                if (_cacheStatusFontStyle == null)
                {
                    _cacheStatusFontStyle = CreateInstance(SystemResourceKeyID.StatusFontStyle);
                }

                return _cacheStatusFontStyle;
            }
        }

        /// <summary>
        ///     StatusFontWeight System Resource Key
        /// </summary>
        public static ResourceKey StatusFontWeightKey
        {
            get
            {
                if (_cacheStatusFontWeight == null)
                {
                    _cacheStatusFontWeight = CreateInstance(SystemResourceKeyID.StatusFontWeight);
                }

                return _cacheStatusFontWeight;
            }
        }

        /// <summary>
        ///     StatusFontTextDecorations System Resource Key
        /// </summary>
        public static ResourceKey StatusFontTextDecorationsKey
        {
            get
            {
                if (_cacheStatusFontTextDecorations == null)
                {
                    _cacheStatusFontTextDecorations = CreateInstance(SystemResourceKeyID.StatusFontTextDecorations);
                }

                return _cacheStatusFontTextDecorations;
            }
        }

        /// <summary>
        ///     MessageFontSize System Resource Key
        /// </summary>
        public static ResourceKey MessageFontSizeKey
        {
            get
            {
                if (_cacheMessageFontSize == null)
                {
                    _cacheMessageFontSize = CreateInstance(SystemResourceKeyID.MessageFontSize);
                }

                return _cacheMessageFontSize;
            }
        }

        /// <summary>
        ///     MessageFontFamily System Resource Key
        /// </summary>
        public static ResourceKey MessageFontFamilyKey
        {
            get
            {
                if (_cacheMessageFontFamily == null)
                {
                    _cacheMessageFontFamily = CreateInstance(SystemResourceKeyID.MessageFontFamily);
                }

                return _cacheMessageFontFamily;
            }
        }

        /// <summary>
        ///     MessageFontStyle System Resource Key
        /// </summary>
        public static ResourceKey MessageFontStyleKey
        {
            get
            {
                if (_cacheMessageFontStyle == null)
                {
                    _cacheMessageFontStyle = CreateInstance(SystemResourceKeyID.MessageFontStyle);
                }

                return _cacheMessageFontStyle;
            }
        }

        /// <summary>
        ///     MessageFontWeight System Resource Key
        /// </summary>
        public static ResourceKey MessageFontWeightKey
        {
            get
            {
                if (_cacheMessageFontWeight == null)
                {
                    _cacheMessageFontWeight = CreateInstance(SystemResourceKeyID.MessageFontWeight);
                }

                return _cacheMessageFontWeight;
            }
        }

        /// <summary>
        ///     MessageFontTextDecorations System Resource Key
        /// </summary>
        public static ResourceKey MessageFontTextDecorationsKey
        {
            get
            {
                if (_cacheMessageFontTextDecorations == null)
                {
                    _cacheMessageFontTextDecorations = CreateInstance(SystemResourceKeyID.MessageFontTextDecorations);
                }

                return _cacheMessageFontTextDecorations;
            }
        }

        #endregion

        #region Implementation

        private static double ConvertFontHeight(int height)
        {
            int dpi = SystemParameters.Dpi;

            if (dpi != 0)
            {
                return (double)(Math.Abs(height) * 96 / dpi);
            }
            else
            {
                // Could not get the DPI to convert the size, using the hardcoded fallback value
                return FallbackFontSize;
            }
        }

        private const double FallbackFontSize = 11.0;   // To use if unable to get the system size

        internal static void InvalidateIconMetrics()
        {
            _iconFontTextDecorations = null;
            _iconFontFamily = null;
        }

        internal static void InvalidateNonClientMetrics()
        {
            _messageFontTextDecorations = null;
            _statusFontTextDecorations = null;
            _menuFontTextDecorations = null;
            _smallCaptionFontTextDecorations = null;
            _captionFontTextDecorations = null;

            _messageFontFamily = null;
            _statusFontFamily = null;
            _menuFontFamily = null;
            _smallCaptionFontFamily = null;
            _captionFontFamily = null;
        }

        private static TextDecorationCollection _iconFontTextDecorations;
        private static TextDecorationCollection _messageFontTextDecorations;
        private static TextDecorationCollection _statusFontTextDecorations;
        private static TextDecorationCollection _menuFontTextDecorations;
        private static TextDecorationCollection _smallCaptionFontTextDecorations;
        private static TextDecorationCollection _captionFontTextDecorations;

        private static FontFamily _iconFontFamily;
        private static FontFamily _messageFontFamily;
        private static FontFamily _statusFontFamily;
        private static FontFamily _menuFontFamily;
        private static FontFamily _smallCaptionFontFamily;
        private static FontFamily _captionFontFamily;

        private static SystemResourceKey _cacheIconFontSize;
        private static SystemResourceKey _cacheIconFontFamily;
        private static SystemResourceKey _cacheIconFontStyle;
        private static SystemResourceKey _cacheIconFontWeight;
        private static SystemResourceKey _cacheIconFontTextDecorations;
        private static SystemResourceKey _cacheCaptionFontSize;
        private static SystemResourceKey _cacheCaptionFontFamily;
        private static SystemResourceKey _cacheCaptionFontStyle;
        private static SystemResourceKey _cacheCaptionFontWeight;
        private static SystemResourceKey _cacheCaptionFontTextDecorations;
        private static SystemResourceKey _cacheSmallCaptionFontSize;
        private static SystemResourceKey _cacheSmallCaptionFontFamily;
        private static SystemResourceKey _cacheSmallCaptionFontStyle;
        private static SystemResourceKey _cacheSmallCaptionFontWeight;
        private static SystemResourceKey _cacheSmallCaptionFontTextDecorations;
        private static SystemResourceKey _cacheMenuFontSize;
        private static SystemResourceKey _cacheMenuFontFamily;
        private static SystemResourceKey _cacheMenuFontStyle;
        private static SystemResourceKey _cacheMenuFontWeight;
        private static SystemResourceKey _cacheMenuFontTextDecorations;
        private static SystemResourceKey _cacheStatusFontSize;
        private static SystemResourceKey _cacheStatusFontFamily;
        private static SystemResourceKey _cacheStatusFontStyle;
        private static SystemResourceKey _cacheStatusFontWeight;
        private static SystemResourceKey _cacheStatusFontTextDecorations;
        private static SystemResourceKey _cacheMessageFontSize;
        private static SystemResourceKey _cacheMessageFontFamily;
        private static SystemResourceKey _cacheMessageFontStyle;
        private static SystemResourceKey _cacheMessageFontWeight;
        private static SystemResourceKey _cacheMessageFontTextDecorations;

        #endregion
    }
}
