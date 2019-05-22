// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Windows.Media;
using Microsoft.Win32;
using MS.Win32;

namespace System.Windows
{
    /// <summary>
    ///     Contains properties that are queries into the system's various colors.
    /// </summary>
    public static class SystemColors
    {
        #region Colors

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color ActiveBorderColor
        {
            get
            {
                return GetSystemColor(CacheSlot.ActiveBorder);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color ActiveCaptionColor
        {
            get
            {
                return GetSystemColor(CacheSlot.ActiveCaption);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color ActiveCaptionTextColor
        {
            get
            {
                return GetSystemColor(CacheSlot.ActiveCaptionText);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color AppWorkspaceColor
        {
            get
            {
                return GetSystemColor(CacheSlot.AppWorkspace);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color ControlColor
        {
            get
            {
                return GetSystemColor(CacheSlot.Control);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color ControlDarkColor
        {
            get
            {
                return GetSystemColor(CacheSlot.ControlDark);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color ControlDarkDarkColor
        {
            get
            {
                return GetSystemColor(CacheSlot.ControlDarkDark);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color ControlLightColor
        {
            get
            {
                return GetSystemColor(CacheSlot.ControlLight);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color ControlLightLightColor
        {
            get
            {
                return GetSystemColor(CacheSlot.ControlLightLight);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color ControlTextColor
        {
            get
            {
                return GetSystemColor(CacheSlot.ControlText);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color DesktopColor
        {
            get
            {
                return GetSystemColor(CacheSlot.Desktop);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color GradientActiveCaptionColor
        {
            get
            {
                return GetSystemColor(CacheSlot.GradientActiveCaption);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color GradientInactiveCaptionColor
        {
            get
            {
                return GetSystemColor(CacheSlot.GradientInactiveCaption);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color GrayTextColor
        {
            get
            {
                return GetSystemColor(CacheSlot.GrayText);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color HighlightColor
        {
            get
            {
                return GetSystemColor(CacheSlot.Highlight);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color HighlightTextColor
        {
            get
            {
                return GetSystemColor(CacheSlot.HighlightText);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color HotTrackColor
        {
            get
            {
                return GetSystemColor(CacheSlot.HotTrack);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color InactiveBorderColor
        {
            get
            {
                return GetSystemColor(CacheSlot.InactiveBorder);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color InactiveCaptionColor
        {
            get
            {
                return GetSystemColor(CacheSlot.InactiveCaption);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color InactiveCaptionTextColor
        {
            get
            {
                return GetSystemColor(CacheSlot.InactiveCaptionText);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color InfoColor
        {
            get
            {
                return GetSystemColor(CacheSlot.Info);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color InfoTextColor
        {
            get
            {
                return GetSystemColor(CacheSlot.InfoText);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color MenuColor
        {
            get
            {
                return GetSystemColor(CacheSlot.Menu);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color MenuBarColor
        {
            get
            {
                return GetSystemColor(CacheSlot.MenuBar);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color MenuHighlightColor
        {
            get
            {
                return GetSystemColor(CacheSlot.MenuHighlight);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color MenuTextColor
        {
            get
            {
                return GetSystemColor(CacheSlot.MenuText);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color ScrollBarColor
        {
            get
            {
                return GetSystemColor(CacheSlot.ScrollBar);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color WindowColor
        {
            get
            {
                return GetSystemColor(CacheSlot.Window);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color WindowFrameColor
        {
            get
            {
                return GetSystemColor(CacheSlot.WindowFrame);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static Color WindowTextColor
        {
            get
            {
                return GetSystemColor(CacheSlot.WindowText);
            }
        }

        #endregion

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static SystemResourceKey CreateInstance(SystemResourceKeyID KeyId)
        {
            return new SystemResourceKey(KeyId);
        }

        #region Color Keys

        /// <summary>
        ///     ActiveBorderColor System Resource Key
        /// </summary>
        public static ResourceKey ActiveBorderColorKey
        {
            get
            {
                if (_cacheActiveBorderColor == null)
                {
                    _cacheActiveBorderColor = CreateInstance(SystemResourceKeyID.ActiveBorderColor);
                }

                return _cacheActiveBorderColor;
            }
        }

        /// <summary>
        ///     ActiveCaptionColor System Resource Key
        /// </summary>
        public static ResourceKey ActiveCaptionColorKey
        {
            get
            {
                if (_cacheActiveCaptionColor == null)
                {
                    _cacheActiveCaptionColor = CreateInstance(SystemResourceKeyID.ActiveCaptionColor);
                }

                return _cacheActiveCaptionColor;
            }
        }

        /// <summary>
        ///     ActiveCaptionTextColor System Resource Key
        /// </summary>
        public static ResourceKey ActiveCaptionTextColorKey
        {
            get
            {
                if (_cacheActiveCaptionTextColor == null)
                {
                    _cacheActiveCaptionTextColor = CreateInstance(SystemResourceKeyID.ActiveCaptionTextColor);
                }

                return _cacheActiveCaptionTextColor;
            }
        }

        /// <summary>
        ///     AppWorkspaceColor System Resource Key
        /// </summary>
        public static ResourceKey AppWorkspaceColorKey
        {
            get
            {
                if (_cacheAppWorkspaceColor == null)
                {
                    _cacheAppWorkspaceColor = CreateInstance(SystemResourceKeyID.AppWorkspaceColor);
                }

                return _cacheAppWorkspaceColor;
            }
        }

        /// <summary>
        ///     ControlColor System Resource Key
        /// </summary>
        public static ResourceKey ControlColorKey
        {
            get
            {
                if (_cacheControlColor == null)
                {
                    _cacheControlColor = CreateInstance(SystemResourceKeyID.ControlColor);
                }

                return _cacheControlColor;
            }
        }

        /// <summary>
        ///     ControlDarkColor System Resource Key
        /// </summary>
        public static ResourceKey ControlDarkColorKey
        {
            get
            {
                if (_cacheControlDarkColor == null)
                {
                    _cacheControlDarkColor = CreateInstance(SystemResourceKeyID.ControlDarkColor);
                }

                return _cacheControlDarkColor;
            }
        }

        /// <summary>
        ///     ControlDarkDarkColor System Resource Key
        /// </summary>
        public static ResourceKey ControlDarkDarkColorKey
        {
            get
            {
                if (_cacheControlDarkDarkColor == null)
                {
                    _cacheControlDarkDarkColor = CreateInstance(SystemResourceKeyID.ControlDarkDarkColor);
                }

                return _cacheControlDarkDarkColor;
            }
        }

        /// <summary>
        ///     ControlLightColor System Resource Key
        /// </summary>
        public static ResourceKey ControlLightColorKey
        {
            get
            {
                if (_cacheControlLightColor == null)
                {
                    _cacheControlLightColor = CreateInstance(SystemResourceKeyID.ControlLightColor);
                }

                return _cacheControlLightColor;
            }
        }

        /// <summary>
        ///     ControlLightLightColor System Resource Key
        /// </summary>
        public static ResourceKey ControlLightLightColorKey
        {
            get
            {
                if (_cacheControlLightLightColor == null)
                {
                    _cacheControlLightLightColor = CreateInstance(SystemResourceKeyID.ControlLightLightColor);
                }

                return _cacheControlLightLightColor;
            }
        }

        /// <summary>
        ///     ControlTextColor System Resource Key
        /// </summary>
        public static ResourceKey ControlTextColorKey
        {
            get
            {
                if (_cacheControlTextColor == null)
                {
                    _cacheControlTextColor = CreateInstance(SystemResourceKeyID.ControlTextColor);
                }

                return _cacheControlTextColor;
            }
        }

        /// <summary>
        ///     DesktopColor System Resource Key
        /// </summary>
        public static ResourceKey DesktopColorKey
        {
            get
            {
                if (_cacheDesktopColor == null)
                {
                    _cacheDesktopColor = CreateInstance(SystemResourceKeyID.DesktopColor);
                }

                return _cacheDesktopColor;
            }
        }

        /// <summary>
        ///     GradientActiveCaptionColor System Resource Key
        /// </summary>
        public static ResourceKey GradientActiveCaptionColorKey
        {
            get
            {
                if (_cacheGradientActiveCaptionColor == null)
                {
                    _cacheGradientActiveCaptionColor = CreateInstance(SystemResourceKeyID.GradientActiveCaptionColor);
                }

                return _cacheGradientActiveCaptionColor;
            }
        }

        /// <summary>
        ///     GradientInactiveCaptionColor System Resource Key
        /// </summary>
        public static ResourceKey GradientInactiveCaptionColorKey
        {
            get
            {
                if (_cacheGradientInactiveCaptionColor == null)
                {
                    _cacheGradientInactiveCaptionColor = CreateInstance(SystemResourceKeyID.GradientInactiveCaptionColor);
                }

                return _cacheGradientInactiveCaptionColor;
            }
        }

        /// <summary>
        ///     GrayTextColor System Resource Key
        /// </summary>
        public static ResourceKey GrayTextColorKey
        {
            get
            {
                if (_cacheGrayTextColor == null)
                {
                    _cacheGrayTextColor = CreateInstance(SystemResourceKeyID.GrayTextColor);
                }

                return _cacheGrayTextColor;
            }
        }

        /// <summary>
        ///     HighlightColor System Resource Key
        /// </summary>
        public static ResourceKey HighlightColorKey
        {
            get
            {
                if (_cacheHighlightColor == null)
                {
                    _cacheHighlightColor = CreateInstance(SystemResourceKeyID.HighlightColor);
                }

                return _cacheHighlightColor;
            }
        }

        /// <summary>
        ///     HighlightTextColor System Resource Key
        /// </summary>
        public static ResourceKey HighlightTextColorKey
        {
            get
            {
                if (_cacheHighlightTextColor == null)
                {
                    _cacheHighlightTextColor = CreateInstance(SystemResourceKeyID.HighlightTextColor);
                }

                return _cacheHighlightTextColor;
            }
        }

        /// <summary>
        ///     HotTrackColor System Resource Key
        /// </summary>
        public static ResourceKey HotTrackColorKey
        {
            get
            {
                if (_cacheHotTrackColor == null)
                {
                    _cacheHotTrackColor = CreateInstance(SystemResourceKeyID.HotTrackColor);
                }

                return _cacheHotTrackColor;
            }
        }

        /// <summary>
        ///     InactiveBorderColor System Resource Key
        /// </summary>
        public static ResourceKey InactiveBorderColorKey
        {
            get
            {
                if (_cacheInactiveBorderColor == null)
                {
                    _cacheInactiveBorderColor = CreateInstance(SystemResourceKeyID.InactiveBorderColor);
                }

                return _cacheInactiveBorderColor;
            }
        }

        /// <summary>
        ///     InactiveCaptionColor System Resource Key
        /// </summary>
        public static ResourceKey InactiveCaptionColorKey
        {
            get
            {
                if (_cacheInactiveCaptionColor == null)
                {
                    _cacheInactiveCaptionColor = CreateInstance(SystemResourceKeyID.InactiveCaptionColor);
                }

                return _cacheInactiveCaptionColor;
            }
        }

        /// <summary>
        ///     InactiveCaptionTextColor System Resource Key
        /// </summary>
        public static ResourceKey InactiveCaptionTextColorKey
        {
            get
            {
                if (_cacheInactiveCaptionTextColor == null)
                {
                    _cacheInactiveCaptionTextColor = CreateInstance(SystemResourceKeyID.InactiveCaptionTextColor);
                }

                return _cacheInactiveCaptionTextColor;
            }
        }

        /// <summary>
        ///     InfoColor System Resource Key
        /// </summary>
        public static ResourceKey InfoColorKey
        {
            get
            {
                if (_cacheInfoColor == null)
                {
                    _cacheInfoColor = CreateInstance(SystemResourceKeyID.InfoColor);
                }

                return _cacheInfoColor;
            }
        }

        /// <summary>
        ///     InfoTextColor System Resource Key
        /// </summary>
        public static ResourceKey InfoTextColorKey
        {
            get
            {
                if (_cacheInfoTextColor == null)
                {
                    _cacheInfoTextColor = CreateInstance(SystemResourceKeyID.InfoTextColor);
                }

                return _cacheInfoTextColor;
            }
        }

        /// <summary>
        ///     MenuColor System Resource Key
        /// </summary>
        public static ResourceKey MenuColorKey
        {
            get
            {
                if (_cacheMenuColor == null)
                {
                    _cacheMenuColor = CreateInstance(SystemResourceKeyID.MenuColor);
                }

                return _cacheMenuColor;
            }
        }

        /// <summary>
        ///     MenuBarColor System Resource Key
        /// </summary>
        public static ResourceKey MenuBarColorKey
        {
            get
            {
                if (_cacheMenuBarColor == null)
                {
                    _cacheMenuBarColor = CreateInstance(SystemResourceKeyID.MenuBarColor);
                }

                return _cacheMenuBarColor;
            }
        }

        /// <summary>
        ///     MenuHighlightColor System Resource Key
        /// </summary>
        public static ResourceKey MenuHighlightColorKey
        {
            get
            {
                if (_cacheMenuHighlightColor == null)
                {
                    _cacheMenuHighlightColor = CreateInstance(SystemResourceKeyID.MenuHighlightColor);
                }

                return _cacheMenuHighlightColor;
            }
        }

        /// <summary>
        ///     MenuTextColor System Resource Key
        /// </summary>
        public static ResourceKey MenuTextColorKey
        {
            get
            {
                if (_cacheMenuTextColor == null)
                {
                    _cacheMenuTextColor = CreateInstance(SystemResourceKeyID.MenuTextColor);
                }

                return _cacheMenuTextColor;
            }
        }

        /// <summary>
        ///     ScrollBarColor System Resource Key
        /// </summary>
        public static ResourceKey ScrollBarColorKey
        {
            get
            {
                if (_cacheScrollBarColor == null)
                {
                    _cacheScrollBarColor = CreateInstance(SystemResourceKeyID.ScrollBarColor);
                }

                return _cacheScrollBarColor;
            }
        }

        /// <summary>
        ///     WindowColor System Resource Key
        /// </summary>
        public static ResourceKey WindowColorKey
        {
            get
            {
                if (_cacheWindowColor == null)
                {
                    _cacheWindowColor = CreateInstance(SystemResourceKeyID.WindowColor);
                }

                return _cacheWindowColor;
            }
        }

        /// <summary>
        ///     WindowFrameColor System Resource Key
        /// </summary>
        public static ResourceKey WindowFrameColorKey
        {
            get
            {
                if (_cacheWindowFrameColor == null)
                {
                    _cacheWindowFrameColor = CreateInstance(SystemResourceKeyID.WindowFrameColor);
                }

                return _cacheWindowFrameColor;
            }
        }

        /// <summary>
        ///     WindowTextColor System Resource Key
        /// </summary>
        public static ResourceKey WindowTextColorKey
        {
            get
            {
                if (_cacheWindowTextColor == null)
                {
                    _cacheWindowTextColor = CreateInstance(SystemResourceKeyID.WindowTextColor);
                }

                return _cacheWindowTextColor;
            }
        }

        #endregion

        #region Brushes

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush ActiveBorderBrush
        {
            get
            {
                return MakeBrush(CacheSlot.ActiveBorder);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush ActiveCaptionBrush
        {
            get
            {
                return MakeBrush(CacheSlot.ActiveCaption);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush ActiveCaptionTextBrush
        {
            get
            {
                return MakeBrush(CacheSlot.ActiveCaptionText);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush AppWorkspaceBrush
        {
            get
            {
                return MakeBrush(CacheSlot.AppWorkspace);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush ControlBrush
        {
            get
            {
                return MakeBrush(CacheSlot.Control);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush ControlDarkBrush
        {
            get
            {
                return MakeBrush(CacheSlot.ControlDark);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush ControlDarkDarkBrush
        {
            get
            {
                return MakeBrush(CacheSlot.ControlDarkDark);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush ControlLightBrush
        {
            get
            {
                return MakeBrush(CacheSlot.ControlLight);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush ControlLightLightBrush
        {
            get
            {
                return MakeBrush(CacheSlot.ControlLightLight);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush ControlTextBrush
        {
            get
            {
                return MakeBrush(CacheSlot.ControlText);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush DesktopBrush
        {
            get
            {
                return MakeBrush(CacheSlot.Desktop);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush GradientActiveCaptionBrush
        {
            get
            {
                return MakeBrush(CacheSlot.GradientActiveCaption);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush GradientInactiveCaptionBrush
        {
            get
            {
                return MakeBrush(CacheSlot.GradientInactiveCaption);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush GrayTextBrush
        {
            get
            {
                return MakeBrush(CacheSlot.GrayText);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush HighlightBrush
        {
            get
            {
                return MakeBrush(CacheSlot.Highlight);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush HighlightTextBrush
        {
            get
            {
                return MakeBrush(CacheSlot.HighlightText);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush HotTrackBrush
        {
            get
            {
                return MakeBrush(CacheSlot.HotTrack);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush InactiveBorderBrush
        {
            get
            {
                return MakeBrush(CacheSlot.InactiveBorder);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush InactiveCaptionBrush
        {
            get
            {
                return MakeBrush(CacheSlot.InactiveCaption);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush InactiveCaptionTextBrush
        {
            get
            {
                return MakeBrush(CacheSlot.InactiveCaptionText);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush InfoBrush
        {
            get
            {
                return MakeBrush(CacheSlot.Info);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush InfoTextBrush
        {
            get
            {
                return MakeBrush(CacheSlot.InfoText);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush MenuBrush
        {
            get
            {
                return MakeBrush(CacheSlot.Menu);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush MenuBarBrush
        {
            get
            {
                return MakeBrush(CacheSlot.MenuBar);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush MenuHighlightBrush
        {
            get
            {
                return MakeBrush(CacheSlot.MenuHighlight);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush MenuTextBrush
        {
            get
            {
                return MakeBrush(CacheSlot.MenuText);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush ScrollBarBrush
        {
            get
            {
                return MakeBrush(CacheSlot.ScrollBar);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush WindowBrush
        {
            get
            {
                return MakeBrush(CacheSlot.Window);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush WindowFrameBrush
        {
            get
            {
                return MakeBrush(CacheSlot.WindowFrame);
            }
        }

        /// <summary>
        ///     System color of the same name.
        /// </summary>
        public static SolidColorBrush WindowTextBrush
        {
            get
            {
                return MakeBrush(CacheSlot.WindowText);
            }
        }

        /// <summary>
        ///     Inactive selection highlight brush.
        /// </summary>
        /// <remarks>
        ///     Please note that this property does not have an equivalent system color.
        /// </remarks>
        public static SolidColorBrush InactiveSelectionHighlightBrush
        {
            get
            {
                if (SystemParameters.HighContrast)
                {
                    return SystemColors.HighlightBrush;
                }
                else
                {
                    return SystemColors.ControlBrush;
                }
            }
        }

        /// <summary>
        ///     Inactive selection highlight text brush.
        /// </summary>
        /// <remarks>
        ///     Please note that this property does not have an equivalent system color.
        /// </remarks>
        public static SolidColorBrush InactiveSelectionHighlightTextBrush
        {
            get
            {
                if (SystemParameters.HighContrast)
                {
                    return SystemColors.HighlightTextBrush;
                }
                else
                {
                    return SystemColors.ControlTextBrush;
                }
            }
        }

        #endregion

        #region Brush Keys

        /// <summary>
        ///     ActiveBorderBrush System Resource Key
        /// </summary>
        public static ResourceKey ActiveBorderBrushKey
        {
            get
            {
                if (_cacheActiveBorderBrush == null)
                {
                    _cacheActiveBorderBrush = CreateInstance(SystemResourceKeyID.ActiveBorderBrush);
                }

                return _cacheActiveBorderBrush;
            }
        }

        /// <summary>
        ///     ActiveCaptionBrush System Resource Key
        /// </summary>
        public static ResourceKey ActiveCaptionBrushKey
        {
            get
            {
                if (_cacheActiveCaptionBrush == null)
                {
                    _cacheActiveCaptionBrush = CreateInstance(SystemResourceKeyID.ActiveCaptionBrush);
                }

                return _cacheActiveCaptionBrush;
            }
        }

        /// <summary>
        ///     ActiveCaptionTextBrush System Resource Key
        /// </summary>
        public static ResourceKey ActiveCaptionTextBrushKey
        {
            get
            {
                if (_cacheActiveCaptionTextBrush == null)
                {
                    _cacheActiveCaptionTextBrush = CreateInstance(SystemResourceKeyID.ActiveCaptionTextBrush);
                }

                return _cacheActiveCaptionTextBrush;
            }
        }

        /// <summary>
        ///     AppWorkspaceBrush System Resource Key
        /// </summary>
        public static ResourceKey AppWorkspaceBrushKey
        {
            get
            {
                if (_cacheAppWorkspaceBrush == null)
                {
                    _cacheAppWorkspaceBrush = CreateInstance(SystemResourceKeyID.AppWorkspaceBrush);
                }

                return _cacheAppWorkspaceBrush;
            }
        }

        /// <summary>
        ///     ControlBrush System Resource Key
        /// </summary>
        public static ResourceKey ControlBrushKey
        {
            get
            {
                if (_cacheControlBrush == null)
                {
                    _cacheControlBrush = CreateInstance(SystemResourceKeyID.ControlBrush);
                }

                return _cacheControlBrush;
            }
        }

        /// <summary>
        ///     ControlDarkBrush System Resource Key
        /// </summary>
        public static ResourceKey ControlDarkBrushKey
        {
            get
            {
                if (_cacheControlDarkBrush == null)
                {
                    _cacheControlDarkBrush = CreateInstance(SystemResourceKeyID.ControlDarkBrush);
                }

                return _cacheControlDarkBrush;
            }
        }

        /// <summary>
        ///     ControlDarkDarkBrush System Resource Key
        /// </summary>
        public static ResourceKey ControlDarkDarkBrushKey
        {
            get
            {
                if (_cacheControlDarkDarkBrush == null)
                {
                    _cacheControlDarkDarkBrush = CreateInstance(SystemResourceKeyID.ControlDarkDarkBrush);
                }

                return _cacheControlDarkDarkBrush;
            }
        }

        /// <summary>
        ///     ControlLightBrush System Resource Key
        /// </summary>
        public static ResourceKey ControlLightBrushKey
        {
            get
            {
                if (_cacheControlLightBrush == null)
                {
                    _cacheControlLightBrush = CreateInstance(SystemResourceKeyID.ControlLightBrush);
                }

                return _cacheControlLightBrush;
            }
        }

        /// <summary>
        ///     ControlLightLightBrush System Resource Key
        /// </summary>
        public static ResourceKey ControlLightLightBrushKey
        {
            get
            {
                if (_cacheControlLightLightBrush == null)
                {
                    _cacheControlLightLightBrush = CreateInstance(SystemResourceKeyID.ControlLightLightBrush);
                }

                return _cacheControlLightLightBrush;
            }
        }

        /// <summary>
        ///     ControlTextBrush System Resource Key
        /// </summary>
        public static ResourceKey ControlTextBrushKey
        {
            get
            {
                if (_cacheControlTextBrush == null)
                {
                    _cacheControlTextBrush = CreateInstance(SystemResourceKeyID.ControlTextBrush);
                }

                return _cacheControlTextBrush;
            }
        }

        /// <summary>
        ///     DesktopBrush System Resource Key
        /// </summary>
        public static ResourceKey DesktopBrushKey
        {
            get
            {
                if (_cacheDesktopBrush == null)
                {
                    _cacheDesktopBrush = CreateInstance(SystemResourceKeyID.DesktopBrush);
                }

                return _cacheDesktopBrush;
            }
        }

        /// <summary>
        ///     GradientActiveCaptionBrush System Resource Key
        /// </summary>
        public static ResourceKey GradientActiveCaptionBrushKey
        {
            get
            {
                if (_cacheGradientActiveCaptionBrush == null)
                {
                    _cacheGradientActiveCaptionBrush = CreateInstance(SystemResourceKeyID.GradientActiveCaptionBrush);
                }

                return _cacheGradientActiveCaptionBrush;
            }
        }

        /// <summary>
        ///     GradientInactiveCaptionBrush System Resource Key
        /// </summary>
        public static ResourceKey GradientInactiveCaptionBrushKey
        {
            get
            {
                if (_cacheGradientInactiveCaptionBrush == null)
                {
                    _cacheGradientInactiveCaptionBrush = CreateInstance(SystemResourceKeyID.GradientInactiveCaptionBrush);
                }

                return _cacheGradientInactiveCaptionBrush;
            }
        }

        /// <summary>
        ///     GrayTextBrush System Resource Key
        /// </summary>
        public static ResourceKey GrayTextBrushKey
        {
            get
            {
                if (_cacheGrayTextBrush == null)
                {
                    _cacheGrayTextBrush = CreateInstance(SystemResourceKeyID.GrayTextBrush);
                }

                return _cacheGrayTextBrush;
            }
        }

        /// <summary>
        ///     HighlightBrush System Resource Key
        /// </summary>
        public static ResourceKey HighlightBrushKey
        {
            get
            {
                if (_cacheHighlightBrush == null)
                {
                    _cacheHighlightBrush = CreateInstance(SystemResourceKeyID.HighlightBrush);
                }

                return _cacheHighlightBrush;
            }
        }

        /// <summary>
        ///     HighlightTextBrush System Resource Key
        /// </summary>
        public static ResourceKey HighlightTextBrushKey
        {
            get
            {
                if (_cacheHighlightTextBrush == null)
                {
                    _cacheHighlightTextBrush = CreateInstance(SystemResourceKeyID.HighlightTextBrush);
                }

                return _cacheHighlightTextBrush;
            }
        }

        /// <summary>
        ///     HotTrackBrush System Resource Key
        /// </summary>
        public static ResourceKey HotTrackBrushKey
        {
            get
            {
                if (_cacheHotTrackBrush == null)
                {
                    _cacheHotTrackBrush = CreateInstance(SystemResourceKeyID.HotTrackBrush);
                }

                return _cacheHotTrackBrush;
            }
        }

        /// <summary>
        ///     InactiveBorderBrush System Resource Key
        /// </summary>
        public static ResourceKey InactiveBorderBrushKey
        {
            get
            {
                if (_cacheInactiveBorderBrush == null)
                {
                    _cacheInactiveBorderBrush = CreateInstance(SystemResourceKeyID.InactiveBorderBrush);
                }

                return _cacheInactiveBorderBrush;
            }
        }

        /// <summary>
        ///     InactiveCaptionBrush System Resource Key
        /// </summary>
        public static ResourceKey InactiveCaptionBrushKey
        {
            get
            {
                if (_cacheInactiveCaptionBrush == null)
                {
                    _cacheInactiveCaptionBrush = CreateInstance(SystemResourceKeyID.InactiveCaptionBrush);
                }

                return _cacheInactiveCaptionBrush;
            }
        }

        /// <summary>
        ///     InactiveCaptionTextBrush System Resource Key
        /// </summary>
        public static ResourceKey InactiveCaptionTextBrushKey
        {
            get
            {
                if (_cacheInactiveCaptionTextBrush == null)
                {
                    _cacheInactiveCaptionTextBrush = CreateInstance(SystemResourceKeyID.InactiveCaptionTextBrush);
                }

                return _cacheInactiveCaptionTextBrush;
            }
        }

        /// <summary>
        ///     InfoBrush System Resource Key
        /// </summary>
        public static ResourceKey InfoBrushKey
        {
            get
            {
                if (_cacheInfoBrush == null)
                {
                    _cacheInfoBrush = CreateInstance(SystemResourceKeyID.InfoBrush);
                }

                return _cacheInfoBrush;
            }
        }

        /// <summary>
        ///     InfoTextBrush System Resource Key
        /// </summary>
        public static ResourceKey InfoTextBrushKey
        {
            get
            {
                if (_cacheInfoTextBrush == null)
                {
                    _cacheInfoTextBrush = CreateInstance(SystemResourceKeyID.InfoTextBrush);
                }

                return _cacheInfoTextBrush;
            }
        }

        /// <summary>
        ///     MenuBrush System Resource Key
        /// </summary>
        public static ResourceKey MenuBrushKey
        {
            get
            {
                if (_cacheMenuBrush == null)
                {
                    _cacheMenuBrush = CreateInstance(SystemResourceKeyID.MenuBrush);
                }

                return _cacheMenuBrush;
            }
        }

        /// <summary>
        ///     MenuBarBrush System Resource Key
        /// </summary>
        public static ResourceKey MenuBarBrushKey
        {
            get
            {
                if (_cacheMenuBarBrush == null)
                {
                    _cacheMenuBarBrush = CreateInstance(SystemResourceKeyID.MenuBarBrush);
                }

                return _cacheMenuBarBrush;
            }
        }

        /// <summary>
        ///     MenuHighlightBrush System Resource Key
        /// </summary>
        public static ResourceKey MenuHighlightBrushKey
        {
            get
            {
                if (_cacheMenuHighlightBrush == null)
                {
                    _cacheMenuHighlightBrush = CreateInstance(SystemResourceKeyID.MenuHighlightBrush);
                }

                return _cacheMenuHighlightBrush;
            }
        }

        /// <summary>
        ///     MenuTextBrush System Resource Key
        /// </summary>
        public static ResourceKey MenuTextBrushKey
        {
            get
            {
                if (_cacheMenuTextBrush == null)
                {
                    _cacheMenuTextBrush = CreateInstance(SystemResourceKeyID.MenuTextBrush);
                }

                return _cacheMenuTextBrush;
            }
        }

        /// <summary>
        ///     ScrollBarBrush System Resource Key
        /// </summary>
        public static ResourceKey ScrollBarBrushKey
        {
            get
            {
                if (_cacheScrollBarBrush == null)
                {
                    _cacheScrollBarBrush = CreateInstance(SystemResourceKeyID.ScrollBarBrush);
                }

                return _cacheScrollBarBrush;
            }
        }

        /// <summary>
        ///     WindowBrush System Resource Key
        /// </summary>
        public static ResourceKey WindowBrushKey
        {
            get
            {
                if (_cacheWindowBrush == null)
                {
                    _cacheWindowBrush = CreateInstance(SystemResourceKeyID.WindowBrush);
                }

                return _cacheWindowBrush;
            }
        }

        /// <summary>
        ///     WindowFrameBrush System Resource Key
        /// </summary>
        public static ResourceKey WindowFrameBrushKey
        {
            get
            {
                if (_cacheWindowFrameBrush == null)
                {
                    _cacheWindowFrameBrush = CreateInstance(SystemResourceKeyID.WindowFrameBrush);
                }

                return _cacheWindowFrameBrush;
            }
        }

        /// <summary>
        ///     WindowTextBrush System Resource Key
        /// </summary>
        public static ResourceKey WindowTextBrushKey
        {
            get
            {
                if (_cacheWindowTextBrush == null)
                {
                    _cacheWindowTextBrush = CreateInstance(SystemResourceKeyID.WindowTextBrush);
                }

                return _cacheWindowTextBrush;
            }
        }

        /// <summary>
        ///     InactiveSelectionHighlightBrush System Resource Key
        /// </summary>
        public static ResourceKey InactiveSelectionHighlightBrushKey
        {
            get
            {
                if (FrameworkCompatibilityPreferences.GetAreInactiveSelectionHighlightBrushKeysSupported())
                {
                    if (_cacheInactiveSelectionHighlightBrush == null)
                    {
                        _cacheInactiveSelectionHighlightBrush = CreateInstance(SystemResourceKeyID.InactiveSelectionHighlightBrush);
                    }

                    return _cacheInactiveSelectionHighlightBrush;
                }
                else
                {
                    return ControlBrushKey;
                }
            }
        }

        /// <summary>
        ///     InactiveSelectionHighlightTextBrush System Resource Key
        /// </summary>
        public static ResourceKey InactiveSelectionHighlightTextBrushKey
        {
            get
            {
                if (FrameworkCompatibilityPreferences.GetAreInactiveSelectionHighlightBrushKeysSupported())
                {
                    if (_cacheInactiveSelectionHighlightTextBrush == null)
                    {
                        _cacheInactiveSelectionHighlightTextBrush = CreateInstance(SystemResourceKeyID.InactiveSelectionHighlightTextBrush);
                    }

                    return _cacheInactiveSelectionHighlightTextBrush;
                }
                else
                {
                    return ControlTextBrushKey;
                }
            }
        }

        #endregion

        #region Implementation

        internal static bool InvalidateCache()
        {
            bool color = SystemResources.ClearBitArray(_colorCacheValid);
            bool brush = SystemResources.ClearBitArray(_brushCacheValid);
            return color || brush;
        }

        // Shift count and bit mask for A, R, G, B components
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
        /// <param name="slot">The color slot.</param>
        /// <returns>The system color.</returns>
        private static Color GetSystemColor(CacheSlot slot)
        {
            Color color;

            lock (_colorCacheValid)
            {
                // the loop protects against a race condition - see SystemParameters
                while (!_colorCacheValid[(int)slot])
                {
                    _colorCacheValid[(int)slot] = true;

                    uint argb;
                    int sysColor = SafeNativeMethods.GetSysColor(SlotToFlag(slot));

                    argb = (uint)FromWin32Value(sysColor);
                    color = Color.FromArgb((byte)((argb & 0xff000000) >>24), (byte)((argb & 0x00ff0000) >>16), (byte)((argb & 0x0000ff00) >>8), (byte)(argb & 0x000000ff));

                    _colorCache[(int)slot] = color;
                }

                color = _colorCache[(int)slot];
            }

            return color;
        }

        private static SolidColorBrush MakeBrush(CacheSlot slot)
        {
            SolidColorBrush brush;

            lock (_brushCacheValid)
            {
                // the loop protects against a race condition - see SystemParameters
                while (!_brushCacheValid[(int)slot])
                {
                    _brushCacheValid[(int)slot] = true;

                    brush = new SolidColorBrush(GetSystemColor(slot));
                    brush.Freeze();

                    _brushCache[(int)slot] = brush;
                }

                brush = _brushCache[(int)slot];
            }

            return brush;
        }

        private static int SlotToFlag(CacheSlot slot)
        {
            // FxCop: Hashtable would be overkill, using switch instead

            switch (slot)
            {
                case CacheSlot.ActiveBorder:
                    return (int)NativeMethods.Win32SystemColors.ActiveBorder;
                case CacheSlot.ActiveCaption:
                    return (int)NativeMethods.Win32SystemColors.ActiveCaption;
                case CacheSlot.ActiveCaptionText:
                    return (int)NativeMethods.Win32SystemColors.ActiveCaptionText;
                case CacheSlot.AppWorkspace:
                    return (int)NativeMethods.Win32SystemColors.AppWorkspace;
                case CacheSlot.Control:
                    return (int)NativeMethods.Win32SystemColors.Control;
                case CacheSlot.ControlDark:
                    return (int)NativeMethods.Win32SystemColors.ControlDark;
                case CacheSlot.ControlDarkDark:
                    return (int)NativeMethods.Win32SystemColors.ControlDarkDark;
                case CacheSlot.ControlLight:
                    return (int)NativeMethods.Win32SystemColors.ControlLight;
                case CacheSlot.ControlLightLight:
                    return (int)NativeMethods.Win32SystemColors.ControlLightLight;
                case CacheSlot.ControlText:
                    return (int)NativeMethods.Win32SystemColors.ControlText;
                case CacheSlot.Desktop:
                    return (int)NativeMethods.Win32SystemColors.Desktop;
                case CacheSlot.GradientActiveCaption:
                    return (int)NativeMethods.Win32SystemColors.GradientActiveCaption;
                case CacheSlot.GradientInactiveCaption:
                    return (int)NativeMethods.Win32SystemColors.GradientInactiveCaption;
                case CacheSlot.GrayText:
                    return (int)NativeMethods.Win32SystemColors.GrayText;
                case CacheSlot.Highlight:
                    return (int)NativeMethods.Win32SystemColors.Highlight;
                case CacheSlot.HighlightText:
                    return (int)NativeMethods.Win32SystemColors.HighlightText;
                case CacheSlot.HotTrack:
                    return (int)NativeMethods.Win32SystemColors.HotTrack;
                case CacheSlot.InactiveBorder:
                    return (int)NativeMethods.Win32SystemColors.InactiveBorder;
                case CacheSlot.InactiveCaption:
                    return (int)NativeMethods.Win32SystemColors.InactiveCaption;
                case CacheSlot.InactiveCaptionText:
                    return (int)NativeMethods.Win32SystemColors.InactiveCaptionText;
                case CacheSlot.Info:
                    return (int)NativeMethods.Win32SystemColors.Info;
                case CacheSlot.InfoText:
                    return (int)NativeMethods.Win32SystemColors.InfoText;
                case CacheSlot.Menu:
                    return (int)NativeMethods.Win32SystemColors.Menu;
                case CacheSlot.MenuBar:
                    return (int)NativeMethods.Win32SystemColors.MenuBar;
                case CacheSlot.MenuHighlight:
                    return (int)NativeMethods.Win32SystemColors.MenuHighlight;
                case CacheSlot.MenuText:
                    return (int)NativeMethods.Win32SystemColors.MenuText;
                case CacheSlot.ScrollBar:
                    return (int)NativeMethods.Win32SystemColors.ScrollBar;
                case CacheSlot.Window:
                    return (int)NativeMethods.Win32SystemColors.Window;
                case CacheSlot.WindowFrame:
                    return (int)NativeMethods.Win32SystemColors.WindowFrame;
                case CacheSlot.WindowText:
                    return (int)NativeMethods.Win32SystemColors.WindowText;
            }

            return 0;
        }

        private enum CacheSlot : int
        {
            ActiveBorder,
            ActiveCaption,
            ActiveCaptionText,
            AppWorkspace,
            Control,
            ControlDark,
            ControlDarkDark,
            ControlLight,
            ControlLightLight,
            ControlText,
            Desktop,
            GradientActiveCaption,
            GradientInactiveCaption,
            GrayText,
            Highlight,
            HighlightText,
            HotTrack,
            InactiveBorder,
            InactiveCaption,
            InactiveCaptionText,
            Info,
            InfoText,
            Menu,
            MenuBar,
            MenuHighlight,
            MenuText,
            ScrollBar,
            Window,
            WindowFrame,
            WindowText,

            NumSlots
        }

        private static BitArray _colorCacheValid = new BitArray((int)CacheSlot.NumSlots);
        private static Color[] _colorCache = new Color[(int)CacheSlot.NumSlots];
        private static BitArray _brushCacheValid = new BitArray((int)CacheSlot.NumSlots);
        private static SolidColorBrush[] _brushCache = new SolidColorBrush[(int)CacheSlot.NumSlots];

        private static SystemResourceKey _cacheActiveBorderBrush;
        private static SystemResourceKey _cacheActiveCaptionBrush;
        private static SystemResourceKey _cacheActiveCaptionTextBrush;
        private static SystemResourceKey _cacheAppWorkspaceBrush;
        private static SystemResourceKey _cacheControlBrush;
        private static SystemResourceKey _cacheControlDarkBrush;
        private static SystemResourceKey _cacheControlDarkDarkBrush;
        private static SystemResourceKey _cacheControlLightBrush;
        private static SystemResourceKey _cacheControlLightLightBrush;
        private static SystemResourceKey _cacheControlTextBrush;
        private static SystemResourceKey _cacheDesktopBrush;
        private static SystemResourceKey _cacheGradientActiveCaptionBrush;
        private static SystemResourceKey _cacheGradientInactiveCaptionBrush;
        private static SystemResourceKey _cacheGrayTextBrush;
        private static SystemResourceKey _cacheHighlightBrush;
        private static SystemResourceKey _cacheHighlightTextBrush;
        private static SystemResourceKey _cacheHotTrackBrush;
        private static SystemResourceKey _cacheInactiveBorderBrush;
        private static SystemResourceKey _cacheInactiveCaptionBrush;
        private static SystemResourceKey _cacheInactiveCaptionTextBrush;
        private static SystemResourceKey _cacheInfoBrush;
        private static SystemResourceKey _cacheInfoTextBrush;
        private static SystemResourceKey _cacheMenuBrush;
        private static SystemResourceKey _cacheMenuBarBrush;
        private static SystemResourceKey _cacheMenuHighlightBrush;
        private static SystemResourceKey _cacheMenuTextBrush;
        private static SystemResourceKey _cacheScrollBarBrush;
        private static SystemResourceKey _cacheWindowBrush;
        private static SystemResourceKey _cacheWindowFrameBrush;
        private static SystemResourceKey _cacheWindowTextBrush;
        private static SystemResourceKey _cacheInactiveSelectionHighlightBrush;
        private static SystemResourceKey _cacheInactiveSelectionHighlightTextBrush;
        private static SystemResourceKey _cacheActiveBorderColor;
        private static SystemResourceKey _cacheActiveCaptionColor;
        private static SystemResourceKey _cacheActiveCaptionTextColor;
        private static SystemResourceKey _cacheAppWorkspaceColor;
        private static SystemResourceKey _cacheControlColor;
        private static SystemResourceKey _cacheControlDarkColor;
        private static SystemResourceKey _cacheControlDarkDarkColor;
        private static SystemResourceKey _cacheControlLightColor;
        private static SystemResourceKey _cacheControlLightLightColor;
        private static SystemResourceKey _cacheControlTextColor;
        private static SystemResourceKey _cacheDesktopColor;
        private static SystemResourceKey _cacheGradientActiveCaptionColor;
        private static SystemResourceKey _cacheGradientInactiveCaptionColor;
        private static SystemResourceKey _cacheGrayTextColor;
        private static SystemResourceKey _cacheHighlightColor;
        private static SystemResourceKey _cacheHighlightTextColor;
        private static SystemResourceKey _cacheHotTrackColor;
        private static SystemResourceKey _cacheInactiveBorderColor;
        private static SystemResourceKey _cacheInactiveCaptionColor;
        private static SystemResourceKey _cacheInactiveCaptionTextColor;
        private static SystemResourceKey _cacheInfoColor;
        private static SystemResourceKey _cacheInfoTextColor;
        private static SystemResourceKey _cacheMenuColor;
        private static SystemResourceKey _cacheMenuBarColor;
        private static SystemResourceKey _cacheMenuHighlightColor;
        private static SystemResourceKey _cacheMenuTextColor;
        private static SystemResourceKey _cacheScrollBarColor;
        private static SystemResourceKey _cacheWindowColor;
        private static SystemResourceKey _cacheWindowFrameColor;
        private static SystemResourceKey _cacheWindowTextColor;

        #endregion
    }
}
