// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Reflection;
using MS.Internal.KnownBoxes;
using System.ComponentModel;
using System.Diagnostics;

namespace System.Windows
#endif
{
    /// <summary>
    ///     The unique IDs for the system resource keys
    /// </summary>
    internal enum SystemResourceKeyID
    {
        // ---- Colors and Brushes section ----
        InternalSystemColorsStart = 0,

        ActiveBorderBrush,
        ActiveCaptionBrush,
        ActiveCaptionTextBrush,
        AppWorkspaceBrush,
        ControlBrush,
        ControlDarkBrush,
        ControlDarkDarkBrush,
        ControlLightBrush,
        ControlLightLightBrush,
        ControlTextBrush,
        DesktopBrush,
        GradientActiveCaptionBrush,
        GradientInactiveCaptionBrush,
        GrayTextBrush,
        HighlightBrush,
        HighlightTextBrush,
        HotTrackBrush,
        InactiveBorderBrush,
        InactiveCaptionBrush,
        InactiveCaptionTextBrush,
        InfoBrush,
        InfoTextBrush,
        MenuBrush,
        MenuBarBrush,
        MenuHighlightBrush,
        MenuTextBrush,
        ScrollBarBrush,
        WindowBrush,
        WindowFrameBrush,
        WindowTextBrush,
        ActiveBorderColor,
        ActiveCaptionColor,
        ActiveCaptionTextColor,
        AppWorkspaceColor,
        ControlColor,
        ControlDarkColor,
        ControlDarkDarkColor,
        ControlLightColor,
        ControlLightLightColor,
        ControlTextColor,
        DesktopColor,
        GradientActiveCaptionColor,
        GradientInactiveCaptionColor,
        GrayTextColor,
        HighlightColor,
        HighlightTextColor,
        HotTrackColor,
        InactiveBorderColor,
        InactiveCaptionColor,
        InactiveCaptionTextColor,
        InfoColor,
        InfoTextColor,
        MenuColor,
        MenuBarColor,
        MenuHighlightColor,
        MenuTextColor,
        ScrollBarColor,
        WindowColor,
        WindowFrameColor,
        WindowTextColor,

        InternalSystemColorsEnd,

        // ---- Fonts section ----
        InternalSystemFontsStart,

        CaptionFontSize,
        CaptionFontFamily,
        CaptionFontStyle,
        CaptionFontWeight,
        CaptionFontTextDecorations,
        SmallCaptionFontSize,
        SmallCaptionFontFamily,
        SmallCaptionFontStyle,
        SmallCaptionFontWeight,
        SmallCaptionFontTextDecorations,
        MenuFontSize,
        MenuFontFamily,
        MenuFontStyle,
        MenuFontWeight,
        MenuFontTextDecorations,
        StatusFontSize,
        StatusFontFamily,
        StatusFontStyle,
        StatusFontWeight,
        StatusFontTextDecorations,
        MessageFontSize,
        MessageFontFamily,
        MessageFontStyle,
        MessageFontWeight,
        MessageFontTextDecorations,
        IconFontSize,
        IconFontFamily,
        IconFontStyle,
        IconFontWeight,
        IconFontTextDecorations,

        InternalSystemFontsEnd,

        // ---- SystemParameters section ---
        InternalSystemParametersStart,

        ThinHorizontalBorderHeight,
        ThinVerticalBorderWidth,
        CursorWidth,
        CursorHeight,
        ThickHorizontalBorderHeight,
        ThickVerticalBorderWidth,
        FixedFrameHorizontalBorderHeight,
        FixedFrameVerticalBorderWidth,
        FocusHorizontalBorderHeight,
        FocusVerticalBorderWidth,
        FullPrimaryScreenWidth,
        FullPrimaryScreenHeight,
        HorizontalScrollBarButtonWidth,
        HorizontalScrollBarHeight,
        HorizontalScrollBarThumbWidth,
        IconWidth,
        IconHeight,
        IconGridWidth,
        IconGridHeight,
        MaximizedPrimaryScreenWidth,
        MaximizedPrimaryScreenHeight,
        MaximumWindowTrackWidth,
        MaximumWindowTrackHeight,
        MenuCheckmarkWidth,
        MenuCheckmarkHeight,
        MenuButtonWidth,
        MenuButtonHeight,
        MinimumWindowWidth,
        MinimumWindowHeight,
        MinimizedWindowWidth,
        MinimizedWindowHeight,
        MinimizedGridWidth,
        MinimizedGridHeight,
        MinimumWindowTrackWidth,
        MinimumWindowTrackHeight,
        PrimaryScreenWidth,
        PrimaryScreenHeight,
        WindowCaptionButtonWidth,
        WindowCaptionButtonHeight,
        ResizeFrameHorizontalBorderHeight,
        ResizeFrameVerticalBorderWidth,
        SmallIconWidth,
        SmallIconHeight,
        SmallWindowCaptionButtonWidth,
        SmallWindowCaptionButtonHeight,
        VirtualScreenWidth,
        VirtualScreenHeight,
        VerticalScrollBarWidth,
        VerticalScrollBarButtonHeight,
        WindowCaptionHeight,
        KanjiWindowHeight,
        MenuBarHeight,
        SmallCaptionHeight,
        VerticalScrollBarThumbHeight,
        IsImmEnabled,
        IsMediaCenter,
        IsMenuDropRightAligned,
        IsMiddleEastEnabled,
        IsMousePresent,
        IsMouseWheelPresent,
        IsPenWindows,
        IsRemotelyControlled,
        IsRemoteSession,
        ShowSounds,
        IsSlowMachine,
        SwapButtons,
        IsTabletPC,
        VirtualScreenLeft,
        VirtualScreenTop,
        FocusBorderWidth,
        FocusBorderHeight,
        HighContrast,
        DropShadow,
        FlatMenu,
        WorkArea,
        IconHorizontalSpacing,
        IconVerticalSpacing,
        IconTitleWrap,
        KeyboardCues,
        KeyboardDelay,
        KeyboardPreference,
        KeyboardSpeed,
        SnapToDefaultButton,
        WheelScrollLines,
        MouseHoverTime,
        MouseHoverHeight,
        MouseHoverWidth,
        MenuDropAlignment,
        MenuFade,
        MenuShowDelay,
        ComboBoxAnimation,
        ClientAreaAnimation,
        CursorShadow,
        GradientCaptions,
        HotTracking,
        ListBoxSmoothScrolling,
        MenuAnimation,
        SelectionFade,
        StylusHotTracking,
        ToolTipAnimation,
        ToolTipFade,
        UIEffects,
        MinimizeAnimation,
        Border,
        CaretWidth,
        ForegroundFlashCount,
        DragFullWindows,
        BorderWidth,
        ScrollWidth,
        ScrollHeight,
        CaptionWidth,
        CaptionHeight,
        SmallCaptionWidth,
        MenuWidth,
        MenuHeight,
        ComboBoxPopupAnimation,
        MenuPopupAnimation,
        ToolTipPopupAnimation,
        PowerLineStatus,

        // ---- SystemThemeStyle section ---
        InternalSystemThemeStylesStart,

        FocusVisualStyle,
        NavigationChromeDownLevelStyle,
        NavigationChromeStyle,

        InternalSystemParametersEnd,

        MenuItemSeparatorStyle,
        
        GridViewScrollViewerStyle,
        GridViewStyle,
        GridViewItemContainerStyle,

        StatusBarSeparatorStyle,

        ToolBarButtonStyle,
        ToolBarToggleButtonStyle,
        ToolBarSeparatorStyle,
        ToolBarCheckBoxStyle,
        ToolBarRadioButtonStyle,
        ToolBarComboBoxStyle,
        ToolBarTextBoxStyle,
        ToolBarMenuStyle,

        InternalSystemThemeStylesEnd,

        InternalSystemColorsExtendedStart,

        InactiveSelectionHighlightBrush,
        InactiveSelectionHighlightTextBrush,

        InternalSystemColorsExtendedEnd
    }

#if !PBTCOMPILER
    /// <summary>
    ///     Implements ResourceKey to create unique keys for our system resources.
    ///     Keys will be exposed publicly only with the ResourceKey API.
    /// </summary>
    [TypeConverter(typeof(System.Windows.Markup.SystemKeyConverter))]
    internal class SystemResourceKey : ResourceKey
#else
    internal static class SystemResourceKey
#endif
    {
        //
        // This is the enum range representing the original SystemResource[Key]s.
        //
        private const short SystemResourceKeyIDStart = (short)SystemResourceKeyID.InternalSystemColorsStart;
        private const short SystemResourceKeyIDEnd = (short)SystemResourceKeyID.InternalSystemThemeStylesEnd;
        
        //
        // This is the enum range representing the extended SystemResource[Key]s.
        //
        private const short SystemResourceKeyIDExtendedStart = (short)SystemResourceKeyID.InternalSystemColorsExtendedStart;
        private const short SystemResourceKeyIDExtendedEnd = (short)SystemResourceKeyID.InternalSystemColorsExtendedEnd;
        
        //
        // This is the BAML id range representing the original SystemResourceKeys.
        //
        private const short SystemResourceKeyBAMLIDStart = SystemResourceKeyIDStart;
        private const short SystemResourceKeyBAMLIDEnd = SystemResourceKeyIDEnd;
        
        //
        // This is the BAML id range representing the original SystemResources.
        //
        private const short SystemResourceBAMLIDStart = SystemResourceKeyBAMLIDEnd;
        private const short SystemResourceBAMLIDEnd = (short)(SystemResourceBAMLIDStart + (SystemResourceKeyBAMLIDEnd - SystemResourceKeyBAMLIDStart));
        
        //
        // This is the BAML id range representing the extended SystemResourceKeys.
        //
        private const short SystemResourceKeyBAMLIDExtendedStart = SystemResourceBAMLIDEnd;
        private const short SystemResourceKeyBAMLIDExtendedEnd = (short)(SystemResourceKeyBAMLIDExtendedStart + (SystemResourceKeyIDExtendedEnd - SystemResourceKeyIDExtendedStart));
        
        //
        // This is the BAML id range representing the extended SystemResources.
        //
        private const short SystemResourceBAMLIDExtendedStart = SystemResourceKeyBAMLIDExtendedEnd;
        private const short SystemResourceBAMLIDExtendedEnd = (short)(SystemResourceBAMLIDExtendedStart + (SystemResourceKeyBAMLIDExtendedEnd - SystemResourceKeyBAMLIDExtendedStart));

#if !PBTCOMPILER
        internal static short GetSystemResourceKeyIdFromBamlId(short bamlId, out bool isKey)
        {
            isKey = true;
            
            if (bamlId > SystemResourceBAMLIDStart && bamlId < SystemResourceBAMLIDEnd)
            {
                // Not extended and not a key
                bamlId -= SystemResourceBAMLIDStart;
                isKey = false;
            }
            else if (bamlId > SystemResourceKeyBAMLIDExtendedStart && bamlId < SystemResourceKeyBAMLIDExtendedEnd)
            {
                // Extended Key
                bamlId -= (short)(SystemResourceKeyBAMLIDExtendedStart - SystemResourceKeyIDExtendedStart);
            }
            else if (bamlId > SystemResourceBAMLIDExtendedStart && bamlId < SystemResourceBAMLIDExtendedEnd)
            {
                // Extended but not a key
                bamlId -= (short)(SystemResourceBAMLIDExtendedStart - SystemResourceKeyIDExtendedStart);
                isKey = false;
            }

            return bamlId;
        }
#endif

        internal static short GetBamlIdBasedOnSystemResourceKeyId(Type targetType, string memberName)
        {
            short memberId = 0;
            string srkField = null;
            bool isKey = false;

            if (memberName.EndsWith("Key", false, TypeConverterHelper.InvariantEnglishUS))
            {
                srkField = memberName.Remove(memberName.Length - 3);

                if ((KnownTypes.Types[(int)KnownElements.MenuItem] == targetType) ||
                    (KnownTypes.Types[(int)KnownElements.ToolBar] == targetType) ||
                    (KnownTypes.Types[(int)KnownElements.StatusBar] == targetType))
                {
                    srkField = targetType.Name + srkField;
                }

                isKey = true;
            }
            else
            {
                srkField = memberName;
            }

            if (targetType.Assembly == XamlTypeMapper.AssemblyPF &&
                targetType.FullName == "System.Windows.SystemParameters" &&
                Enum.TryParse(srkField, out SystemResourceKeyID srkId))
            {
                bool isExtended = ((short)srkId > SystemResourceKeyIDExtendedStart &&
                                   (short)srkId < SystemResourceKeyIDExtendedEnd);

                if (isExtended)
                {
                    if (isKey)
                    {
                        // if keyId is more than the range it is the actual resource, else it is the key.
                        memberId = (short)(-((short)srkId - SystemResourceKeyIDExtendedStart + SystemResourceKeyBAMLIDExtendedStart));
                    }
                    else
                    {
                        memberId = (short)(-((short)srkId - SystemResourceKeyIDExtendedStart + SystemResourceBAMLIDExtendedStart));
                    }
                }
                else
                {
                    if (isKey)
                    {
                        // if keyId is more than the range it is the actual resource, else it is the key.
                        memberId = (short)(-((short)srkId));
                    }
                    else
                    {
                        memberId = (short)(-((short)srkId + SystemResourceBAMLIDStart));
                    }
                }
            }

            return memberId;
        }

#if !PBTCOMPILER
        internal object Resource
        {
            get
            {
                // *************************************************************************************
                // IMPORTANT NOTE: If an entry is added to this property, a corresponding one needs to
                // be added to the method GetResourceKey below as well
                // *************************************************************************************
                // FxCop: FxCop may complain that this method is too long.
                //        A hashtable would be overkill, which is the reason for using a switch.

                switch (_id)
                {
                    case SystemResourceKeyID.ActiveBorderBrush:
                        return SystemColors.ActiveBorderBrush;

                    case SystemResourceKeyID.ActiveCaptionBrush:
                        return SystemColors.ActiveCaptionBrush;

                    case SystemResourceKeyID.ActiveCaptionTextBrush:
                        return SystemColors.ActiveCaptionTextBrush;

                    case SystemResourceKeyID.AppWorkspaceBrush:
                        return SystemColors.AppWorkspaceBrush;

                    case SystemResourceKeyID.ControlBrush:
                        return SystemColors.ControlBrush;

                    case SystemResourceKeyID.ControlDarkBrush:
                        return SystemColors.ControlDarkBrush;

                    case SystemResourceKeyID.ControlDarkDarkBrush:
                        return SystemColors.ControlDarkDarkBrush;

                    case SystemResourceKeyID.ControlLightBrush:
                        return SystemColors.ControlLightBrush;

                    case SystemResourceKeyID.ControlLightLightBrush:
                        return SystemColors.ControlLightLightBrush;

                    case SystemResourceKeyID.ControlTextBrush:
                        return SystemColors.ControlTextBrush;

                    case SystemResourceKeyID.DesktopBrush:
                        return SystemColors.DesktopBrush;

                    case SystemResourceKeyID.GradientActiveCaptionBrush:
                        return SystemColors.GradientActiveCaptionBrush;

                    case SystemResourceKeyID.GradientInactiveCaptionBrush:
                        return SystemColors.GradientInactiveCaptionBrush;

                    case SystemResourceKeyID.GrayTextBrush:
                        return SystemColors.GrayTextBrush;

                    case SystemResourceKeyID.HighlightBrush:
                        return SystemColors.HighlightBrush;

                    case SystemResourceKeyID.HighlightTextBrush:
                        return SystemColors.HighlightTextBrush;

                    case SystemResourceKeyID.HotTrackBrush:
                        return SystemColors.HotTrackBrush;

                    case SystemResourceKeyID.InactiveBorderBrush:
                        return SystemColors.InactiveBorderBrush;

                    case SystemResourceKeyID.InactiveCaptionBrush:
                        return SystemColors.InactiveCaptionBrush;

                    case SystemResourceKeyID.InactiveCaptionTextBrush:
                        return SystemColors.InactiveCaptionTextBrush;

                    case SystemResourceKeyID.InfoBrush:
                        return SystemColors.InfoBrush;

                    case SystemResourceKeyID.InfoTextBrush:
                        return SystemColors.InfoTextBrush;

                    case SystemResourceKeyID.MenuBrush:
                        return SystemColors.MenuBrush;

                    case SystemResourceKeyID.MenuBarBrush:
                        return SystemColors.MenuBarBrush;

                    case SystemResourceKeyID.MenuHighlightBrush:
                        return SystemColors.MenuHighlightBrush;

                    case SystemResourceKeyID.MenuTextBrush:
                        return SystemColors.MenuTextBrush;

                    case SystemResourceKeyID.ScrollBarBrush:
                        return SystemColors.ScrollBarBrush;

                    case SystemResourceKeyID.WindowBrush:
                        return SystemColors.WindowBrush;

                    case SystemResourceKeyID.WindowFrameBrush:
                        return SystemColors.WindowFrameBrush;

                    case SystemResourceKeyID.WindowTextBrush:
                        return SystemColors.WindowTextBrush;

                    case SystemResourceKeyID.InactiveSelectionHighlightBrush:
                        return SystemColors.InactiveSelectionHighlightBrush;

                    case SystemResourceKeyID.InactiveSelectionHighlightTextBrush:
                        return SystemColors.InactiveSelectionHighlightTextBrush;

                    case SystemResourceKeyID.ActiveBorderColor:
                        return SystemColors.ActiveBorderColor;

                    case SystemResourceKeyID.ActiveCaptionColor:
                        return SystemColors.ActiveCaptionColor;

                    case SystemResourceKeyID.ActiveCaptionTextColor:
                        return SystemColors.ActiveCaptionTextColor;

                    case SystemResourceKeyID.AppWorkspaceColor:
                        return SystemColors.AppWorkspaceColor;

                    case SystemResourceKeyID.ControlColor:
                        return SystemColors.ControlColor;

                    case SystemResourceKeyID.ControlDarkColor:
                        return SystemColors.ControlDarkColor;

                    case SystemResourceKeyID.ControlDarkDarkColor:
                        return SystemColors.ControlDarkDarkColor;

                    case SystemResourceKeyID.ControlLightColor:
                        return SystemColors.ControlLightColor;

                    case SystemResourceKeyID.ControlLightLightColor:
                        return SystemColors.ControlLightLightColor;

                    case SystemResourceKeyID.ControlTextColor:
                        return SystemColors.ControlTextColor;

                    case SystemResourceKeyID.DesktopColor:
                        return SystemColors.DesktopColor;

                    case SystemResourceKeyID.GradientActiveCaptionColor:
                        return SystemColors.GradientActiveCaptionColor;

                    case SystemResourceKeyID.GradientInactiveCaptionColor:
                        return SystemColors.GradientInactiveCaptionColor;

                    case SystemResourceKeyID.GrayTextColor:
                        return SystemColors.GrayTextColor;

                    case SystemResourceKeyID.HighlightColor:
                        return SystemColors.HighlightColor;

                    case SystemResourceKeyID.HighlightTextColor:
                        return SystemColors.HighlightTextColor;

                    case SystemResourceKeyID.HotTrackColor:
                        return SystemColors.HotTrackColor;

                    case SystemResourceKeyID.InactiveBorderColor:
                        return SystemColors.InactiveBorderColor;

                    case SystemResourceKeyID.InactiveCaptionColor:
                        return SystemColors.InactiveCaptionColor;

                    case SystemResourceKeyID.InactiveCaptionTextColor:
                        return SystemColors.InactiveCaptionTextColor;

                    case SystemResourceKeyID.InfoColor:
                        return SystemColors.InfoColor;

                    case SystemResourceKeyID.InfoTextColor:
                        return SystemColors.InfoTextColor;

                    case SystemResourceKeyID.MenuColor:
                        return SystemColors.MenuColor;

                    case SystemResourceKeyID.MenuBarColor:
                        return SystemColors.MenuBarColor;

                    case SystemResourceKeyID.MenuHighlightColor:
                        return SystemColors.MenuHighlightColor;

                    case SystemResourceKeyID.MenuTextColor:
                        return SystemColors.MenuTextColor;

                    case SystemResourceKeyID.ScrollBarColor:
                        return SystemColors.ScrollBarColor;

                    case SystemResourceKeyID.WindowColor:
                        return SystemColors.WindowColor;

                    case SystemResourceKeyID.WindowFrameColor:
                        return SystemColors.WindowFrameColor;

                    case SystemResourceKeyID.WindowTextColor:
                        return SystemColors.WindowTextColor;

                    case SystemResourceKeyID.ThinHorizontalBorderHeight:
                        return SystemParameters.ThinHorizontalBorderHeight;

                    case SystemResourceKeyID.ThinVerticalBorderWidth:
                        return SystemParameters.ThinVerticalBorderWidth;

                    case SystemResourceKeyID.CursorWidth:
                        return SystemParameters.CursorWidth;

                    case SystemResourceKeyID.CursorHeight:
                        return SystemParameters.CursorHeight;

                    case SystemResourceKeyID.ThickHorizontalBorderHeight:
                        return SystemParameters.ThickHorizontalBorderHeight;

                    case SystemResourceKeyID.ThickVerticalBorderWidth:
                        return SystemParameters.ThickVerticalBorderWidth;

                    case SystemResourceKeyID.FixedFrameHorizontalBorderHeight:
                        return SystemParameters.FixedFrameHorizontalBorderHeight;

                    case SystemResourceKeyID.FixedFrameVerticalBorderWidth:
                        return SystemParameters.FixedFrameVerticalBorderWidth;

                    case SystemResourceKeyID.FocusHorizontalBorderHeight:
                        return SystemParameters.FocusHorizontalBorderHeight;

                    case SystemResourceKeyID.FocusVerticalBorderWidth:
                        return SystemParameters.FocusVerticalBorderWidth;

                    case SystemResourceKeyID.FullPrimaryScreenWidth:
                        return SystemParameters.FullPrimaryScreenWidth;

                    case SystemResourceKeyID.FullPrimaryScreenHeight:
                        return SystemParameters.FullPrimaryScreenHeight;

                    case SystemResourceKeyID.HorizontalScrollBarButtonWidth:
                        return SystemParameters.HorizontalScrollBarButtonWidth;

                    case SystemResourceKeyID.HorizontalScrollBarHeight:
                        return SystemParameters.HorizontalScrollBarHeight;

                    case SystemResourceKeyID.HorizontalScrollBarThumbWidth:
                        return SystemParameters.HorizontalScrollBarThumbWidth;

                    case SystemResourceKeyID.IconWidth:
                        return SystemParameters.IconWidth;

                    case SystemResourceKeyID.IconHeight:
                        return SystemParameters.IconHeight;

                    case SystemResourceKeyID.IconGridWidth:
                        return SystemParameters.IconGridWidth;

                    case SystemResourceKeyID.IconGridHeight:
                        return SystemParameters.IconGridHeight;

                    case SystemResourceKeyID.MaximizedPrimaryScreenWidth:
                        return SystemParameters.MaximizedPrimaryScreenWidth;

                    case SystemResourceKeyID.MaximizedPrimaryScreenHeight:
                        return SystemParameters.MaximizedPrimaryScreenHeight;

                    case SystemResourceKeyID.MaximumWindowTrackWidth:
                        return SystemParameters.MaximumWindowTrackWidth;

                    case SystemResourceKeyID.MaximumWindowTrackHeight:
                        return SystemParameters.MaximumWindowTrackHeight;

                    case SystemResourceKeyID.MenuCheckmarkWidth:
                        return SystemParameters.MenuCheckmarkWidth;

                    case SystemResourceKeyID.MenuCheckmarkHeight:
                        return SystemParameters.MenuCheckmarkHeight;

                    case SystemResourceKeyID.MenuButtonWidth:
                        return SystemParameters.MenuButtonWidth;

                    case SystemResourceKeyID.MenuButtonHeight:
                        return SystemParameters.MenuButtonHeight;

                    case SystemResourceKeyID.MinimumWindowWidth:
                        return SystemParameters.MinimumWindowWidth;

                    case SystemResourceKeyID.MinimumWindowHeight:
                        return SystemParameters.MinimumWindowHeight;

                    case SystemResourceKeyID.MinimizedWindowWidth:
                        return SystemParameters.MinimizedWindowWidth;

                    case SystemResourceKeyID.MinimizedWindowHeight:
                        return SystemParameters.MinimizedWindowHeight;

                    case SystemResourceKeyID.MinimizedGridWidth:
                        return SystemParameters.MinimizedGridWidth;

                    case SystemResourceKeyID.MinimizedGridHeight:
                        return SystemParameters.MinimizedGridHeight;

                    case SystemResourceKeyID.MinimumWindowTrackWidth:
                        return SystemParameters.MinimumWindowTrackWidth;

                    case SystemResourceKeyID.MinimumWindowTrackHeight:
                        return SystemParameters.MinimumWindowTrackHeight;

                    case SystemResourceKeyID.PrimaryScreenWidth:
                        return SystemParameters.PrimaryScreenWidth;

                    case SystemResourceKeyID.PrimaryScreenHeight:
                        return SystemParameters.PrimaryScreenHeight;

                    case SystemResourceKeyID.WindowCaptionButtonWidth:
                        return SystemParameters.WindowCaptionButtonWidth;

                    case SystemResourceKeyID.WindowCaptionButtonHeight:
                        return SystemParameters.WindowCaptionButtonHeight;

                    case SystemResourceKeyID.ResizeFrameHorizontalBorderHeight:
                        return SystemParameters.ResizeFrameHorizontalBorderHeight;

                    case SystemResourceKeyID.ResizeFrameVerticalBorderWidth:
                        return SystemParameters.ResizeFrameVerticalBorderWidth;

                    case SystemResourceKeyID.SmallIconWidth:
                        return SystemParameters.SmallIconWidth;

                    case SystemResourceKeyID.SmallIconHeight:
                        return SystemParameters.SmallIconHeight;

                    case SystemResourceKeyID.SmallWindowCaptionButtonWidth:
                        return SystemParameters.SmallWindowCaptionButtonWidth;

                    case SystemResourceKeyID.SmallWindowCaptionButtonHeight:
                        return SystemParameters.SmallWindowCaptionButtonHeight;

                    case SystemResourceKeyID.VirtualScreenWidth:
                        return SystemParameters.VirtualScreenWidth;

                    case SystemResourceKeyID.VirtualScreenHeight:
                        return SystemParameters.VirtualScreenHeight;

                    case SystemResourceKeyID.VerticalScrollBarWidth:
                        return SystemParameters.VerticalScrollBarWidth;

                    case SystemResourceKeyID.VerticalScrollBarButtonHeight:
                        return SystemParameters.VerticalScrollBarButtonHeight;

                    case SystemResourceKeyID.WindowCaptionHeight:
                        return SystemParameters.WindowCaptionHeight;

                    case SystemResourceKeyID.KanjiWindowHeight:
                        return SystemParameters.KanjiWindowHeight;

                    case SystemResourceKeyID.MenuBarHeight:
                        return SystemParameters.MenuBarHeight;

                    case SystemResourceKeyID.SmallCaptionHeight:
                        return SystemParameters.SmallCaptionHeight;

                    case SystemResourceKeyID.VerticalScrollBarThumbHeight:
                        return SystemParameters.VerticalScrollBarThumbHeight;

                    case SystemResourceKeyID.IsImmEnabled:
                        return BooleanBoxes.Box(SystemParameters.IsImmEnabled);

                    case SystemResourceKeyID.IsMediaCenter:
                        return BooleanBoxes.Box(SystemParameters.IsMediaCenter);

                    case SystemResourceKeyID.IsMenuDropRightAligned:
                        return BooleanBoxes.Box(SystemParameters.IsMenuDropRightAligned);

                    case SystemResourceKeyID.IsMiddleEastEnabled:
                        return BooleanBoxes.Box(SystemParameters.IsMiddleEastEnabled);

                    case SystemResourceKeyID.IsMousePresent:
                        return BooleanBoxes.Box(SystemParameters.IsMousePresent);

                    case SystemResourceKeyID.IsMouseWheelPresent:
                        return BooleanBoxes.Box(SystemParameters.IsMouseWheelPresent);

                    case SystemResourceKeyID.IsPenWindows:
                        return BooleanBoxes.Box(SystemParameters.IsPenWindows);

                    case SystemResourceKeyID.IsRemotelyControlled:
                        return BooleanBoxes.Box(SystemParameters.IsRemotelyControlled);

                    case SystemResourceKeyID.IsRemoteSession:
                        return BooleanBoxes.Box(SystemParameters.IsRemoteSession);

                    case SystemResourceKeyID.ShowSounds:
                        return BooleanBoxes.Box(SystemParameters.ShowSounds);

                    case SystemResourceKeyID.IsSlowMachine:
                        return BooleanBoxes.Box(SystemParameters.IsSlowMachine);

                    case SystemResourceKeyID.SwapButtons:
                        return BooleanBoxes.Box(SystemParameters.SwapButtons);

                    case SystemResourceKeyID.IsTabletPC:
                        return BooleanBoxes.Box(SystemParameters.IsTabletPC);

                    case SystemResourceKeyID.VirtualScreenLeft:
                        return SystemParameters.VirtualScreenLeft;

                    case SystemResourceKeyID.VirtualScreenTop:
                        return SystemParameters.VirtualScreenTop;

                    case SystemResourceKeyID.FocusBorderWidth:
                        return SystemParameters.FocusBorderWidth;

                    case SystemResourceKeyID.FocusBorderHeight:
                        return SystemParameters.FocusBorderHeight;

                    case SystemResourceKeyID.HighContrast:
                        return BooleanBoxes.Box(SystemParameters.HighContrast);

                    case SystemResourceKeyID.DropShadow:
                        return BooleanBoxes.Box(SystemParameters.DropShadow);

                    case SystemResourceKeyID.FlatMenu:
                        return BooleanBoxes.Box(SystemParameters.FlatMenu);

                    case SystemResourceKeyID.WorkArea:
                        return SystemParameters.WorkArea;

                    case SystemResourceKeyID.IconHorizontalSpacing:
                        return SystemParameters.IconHorizontalSpacing;

                    case SystemResourceKeyID.IconVerticalSpacing:
                        return SystemParameters.IconVerticalSpacing;

                    case SystemResourceKeyID.IconTitleWrap:
                        return SystemParameters.IconTitleWrap;

                    case SystemResourceKeyID.IconFontSize:
                        return SystemFonts.IconFontSize;

                    case SystemResourceKeyID.IconFontFamily:
                        return SystemFonts.IconFontFamily;

                    case SystemResourceKeyID.IconFontStyle:
                        return SystemFonts.IconFontStyle;

                    case SystemResourceKeyID.IconFontWeight:
                        return SystemFonts.IconFontWeight;

                    case SystemResourceKeyID.IconFontTextDecorations:
                        return SystemFonts.IconFontTextDecorations;

                    case SystemResourceKeyID.KeyboardCues:
                        return BooleanBoxes.Box(SystemParameters.KeyboardCues);

                    case SystemResourceKeyID.KeyboardDelay:
                        return SystemParameters.KeyboardDelay;

                    case SystemResourceKeyID.KeyboardPreference:
                        return BooleanBoxes.Box(SystemParameters.KeyboardPreference);

                    case SystemResourceKeyID.KeyboardSpeed:
                        return SystemParameters.KeyboardSpeed;

                    case SystemResourceKeyID.SnapToDefaultButton:
                        return BooleanBoxes.Box(SystemParameters.SnapToDefaultButton);

                    case SystemResourceKeyID.WheelScrollLines:
                        return SystemParameters.WheelScrollLines;

                    case SystemResourceKeyID.MouseHoverTime:
                        return SystemParameters.MouseHoverTime;

                    case SystemResourceKeyID.MouseHoverHeight:
                        return SystemParameters.MouseHoverHeight;

                    case SystemResourceKeyID.MouseHoverWidth:
                        return SystemParameters.MouseHoverWidth;

                    case SystemResourceKeyID.MenuDropAlignment:
                        return BooleanBoxes.Box(SystemParameters.MenuDropAlignment);

                    case SystemResourceKeyID.MenuFade:
                        return BooleanBoxes.Box(SystemParameters.MenuFade);

                    case SystemResourceKeyID.MenuShowDelay:
                        return SystemParameters.MenuShowDelay;

                    case SystemResourceKeyID.ComboBoxAnimation:
                        return BooleanBoxes.Box(SystemParameters.ComboBoxAnimation);

                    case SystemResourceKeyID.ClientAreaAnimation:
                        return BooleanBoxes.Box(SystemParameters.ClientAreaAnimation);

                    case SystemResourceKeyID.CursorShadow:
                        return BooleanBoxes.Box(SystemParameters.CursorShadow);

                    case SystemResourceKeyID.GradientCaptions:
                        return BooleanBoxes.Box(SystemParameters.GradientCaptions);

                    case SystemResourceKeyID.HotTracking:
                        return BooleanBoxes.Box(SystemParameters.HotTracking);

                    case SystemResourceKeyID.ListBoxSmoothScrolling:
                        return BooleanBoxes.Box(SystemParameters.ListBoxSmoothScrolling);

                    case SystemResourceKeyID.MenuAnimation:
                        return BooleanBoxes.Box(SystemParameters.MenuAnimation);

                    case SystemResourceKeyID.SelectionFade:
                        return BooleanBoxes.Box(SystemParameters.SelectionFade);

                    case SystemResourceKeyID.StylusHotTracking:
                        return BooleanBoxes.Box(SystemParameters.StylusHotTracking);

                    case SystemResourceKeyID.ToolTipAnimation:
                        return BooleanBoxes.Box(SystemParameters.ToolTipAnimation);

                    case SystemResourceKeyID.ToolTipFade:
                        return BooleanBoxes.Box(SystemParameters.ToolTipFade);

                    case SystemResourceKeyID.UIEffects:
                        return BooleanBoxes.Box(SystemParameters.UIEffects);

                    case SystemResourceKeyID.MinimizeAnimation:
                        return BooleanBoxes.Box(SystemParameters.MinimizeAnimation);

                    case SystemResourceKeyID.Border:
                        return SystemParameters.Border;

                    case SystemResourceKeyID.CaretWidth:
                        return SystemParameters.CaretWidth;

                    case SystemResourceKeyID.ForegroundFlashCount:
                        return SystemParameters.ForegroundFlashCount;

                    case SystemResourceKeyID.DragFullWindows:
                        return BooleanBoxes.Box(SystemParameters.DragFullWindows);

                    case SystemResourceKeyID.BorderWidth:
                        return SystemParameters.BorderWidth;

                    case SystemResourceKeyID.ScrollWidth:
                        return SystemParameters.ScrollWidth;

                    case SystemResourceKeyID.ScrollHeight:
                        return SystemParameters.ScrollHeight;

                    case SystemResourceKeyID.CaptionWidth:
                        return SystemParameters.CaptionWidth;

                    case SystemResourceKeyID.CaptionHeight:
                        return SystemParameters.CaptionHeight;

                    case SystemResourceKeyID.SmallCaptionWidth:
                        return SystemParameters.SmallCaptionWidth;

                    case SystemResourceKeyID.MenuWidth:
                        return SystemParameters.MenuWidth;

                    case SystemResourceKeyID.MenuHeight:
                        return SystemParameters.MenuHeight;

                    case SystemResourceKeyID.CaptionFontSize:
                        return SystemFonts.CaptionFontSize;

                    case SystemResourceKeyID.CaptionFontFamily:
                        return SystemFonts.CaptionFontFamily;

                    case SystemResourceKeyID.CaptionFontStyle:
                        return SystemFonts.CaptionFontStyle;

                    case SystemResourceKeyID.CaptionFontWeight:
                        return SystemFonts.CaptionFontWeight;

                    case SystemResourceKeyID.CaptionFontTextDecorations:
                        return SystemFonts.CaptionFontTextDecorations;

                    case SystemResourceKeyID.SmallCaptionFontSize:
                        return SystemFonts.SmallCaptionFontSize;

                    case SystemResourceKeyID.SmallCaptionFontFamily:
                        return SystemFonts.SmallCaptionFontFamily;

                    case SystemResourceKeyID.SmallCaptionFontStyle:
                        return SystemFonts.SmallCaptionFontStyle;

                    case SystemResourceKeyID.SmallCaptionFontWeight:
                        return SystemFonts.SmallCaptionFontWeight;

                    case SystemResourceKeyID.SmallCaptionFontTextDecorations:
                        return SystemFonts.SmallCaptionFontTextDecorations;

                    case SystemResourceKeyID.MenuFontSize:
                        return SystemFonts.MenuFontSize;

                    case SystemResourceKeyID.MenuFontFamily:
                        return SystemFonts.MenuFontFamily;

                    case SystemResourceKeyID.MenuFontStyle:
                        return SystemFonts.MenuFontStyle;

                    case SystemResourceKeyID.MenuFontWeight:
                        return SystemFonts.MenuFontWeight;

                    case SystemResourceKeyID.MenuFontTextDecorations:
                        return SystemFonts.MenuFontTextDecorations;

                    case SystemResourceKeyID.StatusFontSize:
                        return SystemFonts.StatusFontSize;

                    case SystemResourceKeyID.StatusFontFamily:
                        return SystemFonts.StatusFontFamily;

                    case SystemResourceKeyID.StatusFontStyle:
                        return SystemFonts.StatusFontStyle;

                    case SystemResourceKeyID.StatusFontWeight:
                        return SystemFonts.StatusFontWeight;

                    case SystemResourceKeyID.StatusFontTextDecorations:
                        return SystemFonts.StatusFontTextDecorations;

                    case SystemResourceKeyID.MessageFontSize:
                        return SystemFonts.MessageFontSize;

                    case SystemResourceKeyID.MessageFontFamily:
                        return SystemFonts.MessageFontFamily;

                    case SystemResourceKeyID.MessageFontStyle:
                        return SystemFonts.MessageFontStyle;

                    case SystemResourceKeyID.MessageFontWeight:
                        return SystemFonts.MessageFontWeight;

                    case SystemResourceKeyID.MessageFontTextDecorations:
                        return SystemFonts.MessageFontTextDecorations;

                    case SystemResourceKeyID.ComboBoxPopupAnimation:
                        return SystemParameters.ComboBoxPopupAnimation;

                    case SystemResourceKeyID.MenuPopupAnimation:
                        return SystemParameters.MenuPopupAnimation;

                    case SystemResourceKeyID.ToolTipPopupAnimation:
                        return SystemParameters.ToolTipPopupAnimation;

                    case SystemResourceKeyID.PowerLineStatus:
                        return SystemParameters.PowerLineStatus;
                }

                return null;
            }
        }

        internal static ResourceKey GetResourceKey(short id)
        {
            switch (id)
            {
                case (short)SystemResourceKeyID.ActiveBorderBrush:
                    return SystemColors.ActiveBorderBrushKey;

                case (short)SystemResourceKeyID.ActiveCaptionBrush:
                    return SystemColors.ActiveCaptionBrushKey;

                case (short)SystemResourceKeyID.ActiveCaptionTextBrush:
                    return SystemColors.ActiveCaptionTextBrushKey;

                case (short)SystemResourceKeyID.AppWorkspaceBrush:
                    return SystemColors.AppWorkspaceBrushKey;

                case (short)SystemResourceKeyID.ControlBrush:
                    return SystemColors.ControlBrushKey;

                case (short)SystemResourceKeyID.ControlDarkBrush:
                    return SystemColors.ControlDarkBrushKey;

                case (short)SystemResourceKeyID.ControlDarkDarkBrush:
                    return SystemColors.ControlDarkDarkBrushKey;

                case (short)SystemResourceKeyID.ControlLightBrush:
                    return SystemColors.ControlLightBrushKey;

                case (short)SystemResourceKeyID.ControlLightLightBrush:
                    return SystemColors.ControlLightLightBrushKey;

                case (short)SystemResourceKeyID.ControlTextBrush:
                    return SystemColors.ControlTextBrushKey;

                case (short)SystemResourceKeyID.DesktopBrush:
                    return SystemColors.DesktopBrushKey;

                case (short)SystemResourceKeyID.GradientActiveCaptionBrush:
                    return SystemColors.GradientActiveCaptionBrushKey;

                case (short)SystemResourceKeyID.GradientInactiveCaptionBrush:
                    return SystemColors.GradientInactiveCaptionBrushKey;

                case (short)SystemResourceKeyID.GrayTextBrush:
                    return SystemColors.GrayTextBrushKey;

                case (short)SystemResourceKeyID.HighlightBrush:
                    return SystemColors.HighlightBrushKey;

                case (short)SystemResourceKeyID.HighlightTextBrush:
                    return SystemColors.HighlightTextBrushKey;

                case (short)SystemResourceKeyID.HotTrackBrush:
                    return SystemColors.HotTrackBrushKey;

                case (short)SystemResourceKeyID.InactiveBorderBrush:
                    return SystemColors.InactiveBorderBrushKey;

                case (short)SystemResourceKeyID.InactiveCaptionBrush:
                    return SystemColors.InactiveCaptionBrushKey;

                case (short)SystemResourceKeyID.InactiveCaptionTextBrush:
                    return SystemColors.InactiveCaptionTextBrushKey;

                case (short)SystemResourceKeyID.InfoBrush:
                    return SystemColors.InfoBrushKey;

                case (short)SystemResourceKeyID.InfoTextBrush:
                    return SystemColors.InfoTextBrushKey;

                case (short)SystemResourceKeyID.MenuBrush:
                    return SystemColors.MenuBrushKey;

                case (short)SystemResourceKeyID.MenuBarBrush:
                    return SystemColors.MenuBarBrushKey;

                case (short)SystemResourceKeyID.MenuHighlightBrush:
                    return SystemColors.MenuHighlightBrushKey;

                case (short)SystemResourceKeyID.MenuTextBrush:
                    return SystemColors.MenuTextBrushKey;

                case (short)SystemResourceKeyID.ScrollBarBrush:
                    return SystemColors.ScrollBarBrushKey;

                case (short)SystemResourceKeyID.WindowBrush:
                    return SystemColors.WindowBrushKey;

                case (short)SystemResourceKeyID.WindowFrameBrush:
                    return SystemColors.WindowFrameBrushKey;

                case (short)SystemResourceKeyID.WindowTextBrush:
                    return SystemColors.WindowTextBrushKey;

                case (short)SystemResourceKeyID.InactiveSelectionHighlightBrush:
                    return SystemColors.InactiveSelectionHighlightBrushKey;

                case (short)SystemResourceKeyID.InactiveSelectionHighlightTextBrush:
                    return SystemColors.InactiveSelectionHighlightTextBrushKey;

                case (short)SystemResourceKeyID.ActiveBorderColor:
                    return SystemColors.ActiveBorderColorKey;

                case (short)SystemResourceKeyID.ActiveCaptionColor:
                    return SystemColors.ActiveCaptionColorKey;

                case (short)SystemResourceKeyID.ActiveCaptionTextColor:
                    return SystemColors.ActiveCaptionTextColorKey;

                case (short)SystemResourceKeyID.AppWorkspaceColor:
                    return SystemColors.AppWorkspaceColorKey;

                case (short)SystemResourceKeyID.ControlColor:
                    return SystemColors.ControlColorKey;

                case (short)SystemResourceKeyID.ControlDarkColor:
                    return SystemColors.ControlDarkColorKey;

                case (short)SystemResourceKeyID.ControlDarkDarkColor:
                    return SystemColors.ControlDarkDarkColorKey;

                case (short)SystemResourceKeyID.ControlLightColor:
                    return SystemColors.ControlLightColorKey;

                case (short)SystemResourceKeyID.ControlLightLightColor:
                    return SystemColors.ControlLightLightColorKey;

                case (short)SystemResourceKeyID.ControlTextColor:
                    return SystemColors.ControlTextColorKey;

                case (short)SystemResourceKeyID.DesktopColor:
                    return SystemColors.DesktopColorKey;

                case (short)SystemResourceKeyID.GradientActiveCaptionColor:
                    return SystemColors.GradientActiveCaptionColorKey;

                case (short)SystemResourceKeyID.GradientInactiveCaptionColor:
                    return SystemColors.GradientInactiveCaptionColorKey;

                case (short)SystemResourceKeyID.GrayTextColor:
                    return SystemColors.GrayTextColorKey;

                case (short)SystemResourceKeyID.HighlightColor:
                    return SystemColors.HighlightColorKey;

                case (short)SystemResourceKeyID.HighlightTextColor:
                    return SystemColors.HighlightTextColorKey;

                case (short)SystemResourceKeyID.HotTrackColor:
                    return SystemColors.HotTrackColorKey;

                case (short)SystemResourceKeyID.InactiveBorderColor:
                    return SystemColors.InactiveBorderColorKey;

                case (short)SystemResourceKeyID.InactiveCaptionColor:
                    return SystemColors.InactiveCaptionColorKey;

                case (short)SystemResourceKeyID.InactiveCaptionTextColor:
                    return SystemColors.InactiveCaptionTextColorKey;

                case (short)SystemResourceKeyID.InfoColor:
                    return SystemColors.InfoColorKey;

                case (short)SystemResourceKeyID.InfoTextColor:
                    return SystemColors.InfoTextColorKey;

                case (short)SystemResourceKeyID.MenuColor:
                    return SystemColors.MenuColorKey;

                case (short)SystemResourceKeyID.MenuBarColor:
                    return SystemColors.MenuBarColorKey;

                case (short)SystemResourceKeyID.MenuHighlightColor:
                    return SystemColors.MenuHighlightColorKey;

                case (short)SystemResourceKeyID.MenuTextColor:
                    return SystemColors.MenuTextColorKey;

                case (short)SystemResourceKeyID.ScrollBarColor:
                    return SystemColors.ScrollBarColorKey;

                case (short)SystemResourceKeyID.WindowColor:
                    return SystemColors.WindowColorKey;

                case (short)SystemResourceKeyID.WindowFrameColor:
                    return SystemColors.WindowFrameColorKey;

                case (short)SystemResourceKeyID.WindowTextColor:
                    return SystemColors.WindowTextColorKey;

                case (short)SystemResourceKeyID.ThinHorizontalBorderHeight:
                    return SystemParameters.ThinHorizontalBorderHeightKey;

                case (short)SystemResourceKeyID.ThinVerticalBorderWidth:
                    return SystemParameters.ThinVerticalBorderWidthKey;

                case (short)SystemResourceKeyID.CursorWidth:
                    return SystemParameters.CursorWidthKey;

                case (short)SystemResourceKeyID.CursorHeight:
                    return SystemParameters.CursorHeightKey;

                case (short)SystemResourceKeyID.ThickHorizontalBorderHeight:
                    return SystemParameters.ThickHorizontalBorderHeightKey;

                case (short)SystemResourceKeyID.ThickVerticalBorderWidth:
                    return SystemParameters.ThickVerticalBorderWidthKey;

                case (short)SystemResourceKeyID.FixedFrameHorizontalBorderHeight:
                    return SystemParameters.FixedFrameHorizontalBorderHeightKey;

                case (short)SystemResourceKeyID.FixedFrameVerticalBorderWidth:
                    return SystemParameters.FixedFrameVerticalBorderWidthKey;

                case (short)SystemResourceKeyID.FocusHorizontalBorderHeight:
                    return SystemParameters.FocusHorizontalBorderHeightKey;

                case (short)SystemResourceKeyID.FocusVerticalBorderWidth:
                    return SystemParameters.FocusVerticalBorderWidthKey;

                case (short)SystemResourceKeyID.FullPrimaryScreenWidth:
                    return SystemParameters.FullPrimaryScreenWidthKey;

                case (short)SystemResourceKeyID.FullPrimaryScreenHeight:
                    return SystemParameters.FullPrimaryScreenHeightKey;

                case (short)SystemResourceKeyID.HorizontalScrollBarButtonWidth:
                    return SystemParameters.HorizontalScrollBarButtonWidthKey;

                case (short)SystemResourceKeyID.HorizontalScrollBarHeight:
                    return SystemParameters.HorizontalScrollBarHeightKey;

                case (short)SystemResourceKeyID.HorizontalScrollBarThumbWidth:
                    return SystemParameters.HorizontalScrollBarThumbWidthKey;

                case (short)SystemResourceKeyID.IconWidth:
                    return SystemParameters.IconWidthKey;

                case (short)SystemResourceKeyID.IconHeight:
                    return SystemParameters.IconHeightKey;

                case (short)SystemResourceKeyID.IconGridWidth:
                    return SystemParameters.IconGridWidthKey;

                case (short)SystemResourceKeyID.IconGridHeight:
                    return SystemParameters.IconGridHeightKey;

                case (short)SystemResourceKeyID.MaximizedPrimaryScreenWidth:
                    return SystemParameters.MaximizedPrimaryScreenWidthKey;

                case (short)SystemResourceKeyID.MaximizedPrimaryScreenHeight:
                    return SystemParameters.MaximizedPrimaryScreenHeightKey;

                case (short)SystemResourceKeyID.MaximumWindowTrackWidth:
                    return SystemParameters.MaximumWindowTrackWidthKey;

                case (short)SystemResourceKeyID.MaximumWindowTrackHeight:
                    return SystemParameters.MaximumWindowTrackHeightKey;

                case (short)SystemResourceKeyID.MenuCheckmarkWidth:
                    return SystemParameters.MenuCheckmarkWidthKey;

                case (short)SystemResourceKeyID.MenuCheckmarkHeight:
                    return SystemParameters.MenuCheckmarkHeightKey;

                case (short)SystemResourceKeyID.MenuButtonWidth:
                    return SystemParameters.MenuButtonWidthKey;

                case (short)SystemResourceKeyID.MenuButtonHeight:
                    return SystemParameters.MenuButtonHeightKey;

                case (short)SystemResourceKeyID.MinimumWindowWidth:
                    return SystemParameters.MinimumWindowWidthKey;

                case (short)SystemResourceKeyID.MinimumWindowHeight:
                    return SystemParameters.MinimumWindowHeightKey;

                case (short)SystemResourceKeyID.MinimizedWindowWidth:
                    return SystemParameters.MinimizedWindowWidthKey;

                case (short)SystemResourceKeyID.MinimizedWindowHeight:
                    return SystemParameters.MinimizedWindowHeightKey;

                case (short)SystemResourceKeyID.MinimizedGridWidth:
                    return SystemParameters.MinimizedGridWidthKey;

                case (short)SystemResourceKeyID.MinimizedGridHeight:
                    return SystemParameters.MinimizedGridHeightKey;

                case (short)SystemResourceKeyID.MinimumWindowTrackWidth:
                    return SystemParameters.MinimumWindowTrackWidthKey;

                case (short)SystemResourceKeyID.MinimumWindowTrackHeight:
                    return SystemParameters.MinimumWindowTrackHeightKey;

                case (short)SystemResourceKeyID.PrimaryScreenWidth:
                    return SystemParameters.PrimaryScreenWidthKey;

                case (short)SystemResourceKeyID.PrimaryScreenHeight:
                    return SystemParameters.PrimaryScreenHeightKey;

                case (short)SystemResourceKeyID.WindowCaptionButtonWidth:
                    return SystemParameters.WindowCaptionButtonWidthKey;

                case (short)SystemResourceKeyID.WindowCaptionButtonHeight:
                    return SystemParameters.WindowCaptionButtonHeightKey;

                case (short)SystemResourceKeyID.ResizeFrameHorizontalBorderHeight:
                    return SystemParameters.ResizeFrameHorizontalBorderHeightKey;

                case (short)SystemResourceKeyID.ResizeFrameVerticalBorderWidth:
                    return SystemParameters.ResizeFrameVerticalBorderWidthKey;

                case (short)SystemResourceKeyID.SmallIconWidth:
                    return SystemParameters.SmallIconWidthKey;

                case (short)SystemResourceKeyID.SmallIconHeight:
                    return SystemParameters.SmallIconHeightKey;

                case (short)SystemResourceKeyID.SmallWindowCaptionButtonWidth:
                    return SystemParameters.SmallWindowCaptionButtonWidthKey;

                case (short)SystemResourceKeyID.SmallWindowCaptionButtonHeight:
                    return SystemParameters.SmallWindowCaptionButtonHeightKey;

                case (short)SystemResourceKeyID.VirtualScreenWidth:
                    return SystemParameters.VirtualScreenWidthKey;

                case (short)SystemResourceKeyID.VirtualScreenHeight:
                    return SystemParameters.VirtualScreenHeightKey;

                case (short)SystemResourceKeyID.VerticalScrollBarWidth:
                    return SystemParameters.VerticalScrollBarWidthKey;

                case (short)SystemResourceKeyID.VerticalScrollBarButtonHeight:
                    return SystemParameters.VerticalScrollBarButtonHeightKey;

                case (short)SystemResourceKeyID.WindowCaptionHeight:
                    return SystemParameters.WindowCaptionHeightKey;

                case (short)SystemResourceKeyID.KanjiWindowHeight:
                    return SystemParameters.KanjiWindowHeightKey;

                case (short)SystemResourceKeyID.MenuBarHeight:
                    return SystemParameters.MenuBarHeightKey;

                case (short)SystemResourceKeyID.SmallCaptionHeight:
                    return SystemParameters.SmallCaptionHeightKey;

                case (short)SystemResourceKeyID.VerticalScrollBarThumbHeight:
                    return SystemParameters.VerticalScrollBarThumbHeightKey;

                case (short)SystemResourceKeyID.IsImmEnabled:
                    return SystemParameters.IsImmEnabledKey;

                case (short)SystemResourceKeyID.IsMediaCenter:
                    return SystemParameters.IsMediaCenterKey;

                case (short)SystemResourceKeyID.IsMenuDropRightAligned:
                    return SystemParameters.IsMenuDropRightAlignedKey;

                case (short)SystemResourceKeyID.IsMiddleEastEnabled:
                    return SystemParameters.IsMiddleEastEnabledKey;

                case (short)SystemResourceKeyID.IsMousePresent:
                    return SystemParameters.IsMousePresentKey;

                case (short)SystemResourceKeyID.IsMouseWheelPresent:
                    return SystemParameters.IsMouseWheelPresentKey;

                case (short)SystemResourceKeyID.IsPenWindows:
                    return SystemParameters.IsPenWindowsKey;

                case (short)SystemResourceKeyID.IsRemotelyControlled:
                    return SystemParameters.IsRemotelyControlledKey;

                case (short)SystemResourceKeyID.IsRemoteSession:
                    return SystemParameters.IsRemoteSessionKey;

                case (short)SystemResourceKeyID.ShowSounds:
                    return SystemParameters.ShowSoundsKey;

                case (short)SystemResourceKeyID.IsSlowMachine:
                    return SystemParameters.IsSlowMachineKey;

                case (short)SystemResourceKeyID.SwapButtons:
                    return SystemParameters.SwapButtonsKey;

                case (short)SystemResourceKeyID.IsTabletPC:
                    return SystemParameters.IsTabletPCKey;

                case (short)SystemResourceKeyID.VirtualScreenLeft:
                    return SystemParameters.VirtualScreenLeftKey;

                case (short)SystemResourceKeyID.VirtualScreenTop:
                    return SystemParameters.VirtualScreenTopKey;

                case (short)SystemResourceKeyID.FocusBorderWidth:
                    return SystemParameters.FocusBorderWidthKey;

                case (short)SystemResourceKeyID.FocusBorderHeight:
                    return SystemParameters.FocusBorderHeightKey;

                case (short)SystemResourceKeyID.HighContrast:
                    return SystemParameters.HighContrastKey;

                case (short)SystemResourceKeyID.DropShadow:
                    return SystemParameters.DropShadowKey;

                case (short)SystemResourceKeyID.FlatMenu:
                    return SystemParameters.FlatMenuKey;

                case (short)SystemResourceKeyID.WorkArea:
                    return SystemParameters.WorkAreaKey;

                case (short)SystemResourceKeyID.IconHorizontalSpacing:
                    return SystemParameters.IconHorizontalSpacingKey;

                case (short)SystemResourceKeyID.IconVerticalSpacing:
                    return SystemParameters.IconVerticalSpacingKey;

                case (short)SystemResourceKeyID.IconTitleWrap:
                    return SystemParameters.IconTitleWrapKey;

                case (short)SystemResourceKeyID.IconFontSize:
                    return SystemFonts.IconFontSizeKey;

                case (short)SystemResourceKeyID.IconFontFamily:
                    return SystemFonts.IconFontFamilyKey;

                case (short)SystemResourceKeyID.IconFontStyle:
                    return SystemFonts.IconFontStyleKey;

                case (short)SystemResourceKeyID.IconFontWeight:
                    return SystemFonts.IconFontWeightKey;

                case (short)SystemResourceKeyID.IconFontTextDecorations:
                    return SystemFonts.IconFontTextDecorationsKey;

                case (short)SystemResourceKeyID.KeyboardCues:
                    return SystemParameters.KeyboardCuesKey;

                case (short)SystemResourceKeyID.KeyboardDelay:
                    return SystemParameters.KeyboardDelayKey;

                case (short)SystemResourceKeyID.KeyboardPreference:
                    return SystemParameters.KeyboardPreferenceKey;

                case (short)SystemResourceKeyID.KeyboardSpeed:
                    return SystemParameters.KeyboardSpeedKey;

                case (short)SystemResourceKeyID.SnapToDefaultButton:
                    return SystemParameters.SnapToDefaultButtonKey;

                case (short)SystemResourceKeyID.WheelScrollLines:
                    return SystemParameters.WheelScrollLinesKey;

                case (short)SystemResourceKeyID.MouseHoverTime:
                    return SystemParameters.MouseHoverTimeKey;

                case (short)SystemResourceKeyID.MouseHoverHeight:
                    return SystemParameters.MouseHoverHeightKey;

                case (short)SystemResourceKeyID.MouseHoverWidth:
                    return SystemParameters.MouseHoverWidthKey;

                case (short)SystemResourceKeyID.MenuDropAlignment:
                    return SystemParameters.MenuDropAlignmentKey;

                case (short)SystemResourceKeyID.MenuFade:
                    return SystemParameters.MenuFadeKey;

                case (short)SystemResourceKeyID.MenuShowDelay:
                    return SystemParameters.MenuShowDelayKey;

                case (short)SystemResourceKeyID.ComboBoxAnimation:
                    return SystemParameters.ComboBoxAnimationKey;

                case (short)SystemResourceKeyID.ClientAreaAnimation:
                    return SystemParameters.ClientAreaAnimationKey;

                case (short)SystemResourceKeyID.CursorShadow:
                    return SystemParameters.CursorShadowKey;

                case (short)SystemResourceKeyID.GradientCaptions:
                    return SystemParameters.GradientCaptionsKey;

                case (short)SystemResourceKeyID.HotTracking:
                    return SystemParameters.HotTrackingKey;

                case (short)SystemResourceKeyID.ListBoxSmoothScrolling:
                    return SystemParameters.ListBoxSmoothScrollingKey;

                case (short)SystemResourceKeyID.MenuAnimation:
                    return SystemParameters.MenuAnimationKey;

                case (short)SystemResourceKeyID.SelectionFade:
                    return SystemParameters.SelectionFadeKey;

                case (short)SystemResourceKeyID.StylusHotTracking:
                    return SystemParameters.StylusHotTrackingKey;

                case (short)SystemResourceKeyID.ToolTipAnimation:
                    return SystemParameters.ToolTipAnimationKey;

                case (short)SystemResourceKeyID.ToolTipFade:
                    return SystemParameters.ToolTipFadeKey;

                case (short)SystemResourceKeyID.UIEffects:
                    return SystemParameters.UIEffectsKey;

                case (short)SystemResourceKeyID.MinimizeAnimation:
                    return SystemParameters.MinimizeAnimationKey;

                case (short)SystemResourceKeyID.Border:
                    return SystemParameters.BorderKey;

                case (short)SystemResourceKeyID.CaretWidth:
                    return SystemParameters.CaretWidthKey;

                case (short)SystemResourceKeyID.ForegroundFlashCount:
                    return SystemParameters.ForegroundFlashCountKey;

                case (short)SystemResourceKeyID.DragFullWindows:
                    return SystemParameters.DragFullWindowsKey;

                case (short)SystemResourceKeyID.BorderWidth:
                    return SystemParameters.BorderWidthKey;

                case (short)SystemResourceKeyID.ScrollWidth:
                    return SystemParameters.ScrollWidthKey;

                case (short)SystemResourceKeyID.ScrollHeight:
                    return SystemParameters.ScrollHeightKey;

                case (short)SystemResourceKeyID.CaptionWidth:
                    return SystemParameters.CaptionWidthKey;

                case (short)SystemResourceKeyID.CaptionHeight:
                    return SystemParameters.CaptionHeightKey;

                case (short)SystemResourceKeyID.SmallCaptionWidth:
                    return SystemParameters.SmallCaptionWidthKey;

                case (short)SystemResourceKeyID.MenuWidth:
                    return SystemParameters.MenuWidthKey;

                case (short)SystemResourceKeyID.MenuHeight:
                    return SystemParameters.MenuHeightKey;

                case (short)SystemResourceKeyID.CaptionFontSize:
                    return SystemFonts.CaptionFontSizeKey;

                case (short)SystemResourceKeyID.CaptionFontFamily:
                    return SystemFonts.CaptionFontFamilyKey;

                case (short)SystemResourceKeyID.CaptionFontStyle:
                    return SystemFonts.CaptionFontStyleKey;

                case (short)SystemResourceKeyID.CaptionFontWeight:
                    return SystemFonts.CaptionFontWeightKey;

                case (short)SystemResourceKeyID.CaptionFontTextDecorations:
                    return SystemFonts.CaptionFontTextDecorationsKey;

                case (short)SystemResourceKeyID.SmallCaptionFontSize:
                    return SystemFonts.SmallCaptionFontSizeKey;

                case (short)SystemResourceKeyID.SmallCaptionFontFamily:
                    return SystemFonts.SmallCaptionFontFamilyKey;

                case (short)SystemResourceKeyID.SmallCaptionFontStyle:
                    return SystemFonts.SmallCaptionFontStyleKey;

                case (short)SystemResourceKeyID.SmallCaptionFontWeight:
                    return SystemFonts.SmallCaptionFontWeightKey;

                case (short)SystemResourceKeyID.SmallCaptionFontTextDecorations:
                    return SystemFonts.SmallCaptionFontTextDecorationsKey;

                case (short)SystemResourceKeyID.MenuFontSize:
                    return SystemFonts.MenuFontSizeKey;

                case (short)SystemResourceKeyID.MenuFontFamily:
                    return SystemFonts.MenuFontFamilyKey;

                case (short)SystemResourceKeyID.MenuFontStyle:
                    return SystemFonts.MenuFontStyleKey;

                case (short)SystemResourceKeyID.MenuFontWeight:
                    return SystemFonts.MenuFontWeightKey;

                case (short)SystemResourceKeyID.MenuFontTextDecorations:
                    return SystemFonts.MenuFontTextDecorationsKey;

                case (short)SystemResourceKeyID.StatusFontSize:
                    return SystemFonts.StatusFontSizeKey;

                case (short)SystemResourceKeyID.StatusFontFamily:
                    return SystemFonts.StatusFontFamilyKey;

                case (short)SystemResourceKeyID.StatusFontStyle:
                    return SystemFonts.StatusFontStyleKey;

                case (short)SystemResourceKeyID.StatusFontWeight:
                    return SystemFonts.StatusFontWeightKey;

                case (short)SystemResourceKeyID.StatusFontTextDecorations:
                    return SystemFonts.StatusFontTextDecorationsKey;

                case (short)SystemResourceKeyID.MessageFontSize:
                    return SystemFonts.MessageFontSizeKey;

                case (short)SystemResourceKeyID.MessageFontFamily:
                    return SystemFonts.MessageFontFamilyKey;

                case (short)SystemResourceKeyID.MessageFontStyle:
                    return SystemFonts.MessageFontStyleKey;

                case (short)SystemResourceKeyID.MessageFontWeight:
                    return SystemFonts.MessageFontWeightKey;

                case (short)SystemResourceKeyID.MessageFontTextDecorations:
                    return SystemFonts.MessageFontTextDecorationsKey;

                case (short)SystemResourceKeyID.ComboBoxPopupAnimation:
                    return SystemParameters.ComboBoxPopupAnimationKey;

                case (short)SystemResourceKeyID.MenuPopupAnimation:
                    return SystemParameters.MenuPopupAnimationKey;

                case (short)SystemResourceKeyID.ToolTipPopupAnimation:
                    return SystemParameters.ToolTipPopupAnimationKey;
                
                case (short)SystemResourceKeyID.FocusVisualStyle:
                    return SystemParameters.FocusVisualStyleKey;
                
                case (short)SystemResourceKeyID.NavigationChromeDownLevelStyle:
                    return SystemParameters.NavigationChromeDownLevelStyleKey;

                case (short)SystemResourceKeyID.NavigationChromeStyle:
                    return SystemParameters.NavigationChromeStyleKey;

                case (short)SystemResourceKeyID.MenuItemSeparatorStyle:
                    return MenuItem.SeparatorStyleKey;

                case (short)SystemResourceKeyID.GridViewScrollViewerStyle:
                    return GridView.GridViewScrollViewerStyleKey;

                case (short)SystemResourceKeyID.GridViewStyle:
                    return GridView.GridViewStyleKey;

                case (short)SystemResourceKeyID.GridViewItemContainerStyle:
                    return GridView.GridViewItemContainerStyleKey;

                case (short)SystemResourceKeyID.StatusBarSeparatorStyle:
                    return StatusBar.SeparatorStyleKey;

                case (short)SystemResourceKeyID.ToolBarButtonStyle:
                    return ToolBar.ButtonStyleKey;

                case (short)SystemResourceKeyID.ToolBarToggleButtonStyle:
                    return ToolBar.ToggleButtonStyleKey;

                case (short)SystemResourceKeyID.ToolBarSeparatorStyle:
                    return ToolBar.SeparatorStyleKey;

                case (short)SystemResourceKeyID.ToolBarCheckBoxStyle:
                    return ToolBar.CheckBoxStyleKey;
                
                case (short)SystemResourceKeyID.ToolBarRadioButtonStyle:
                    return ToolBar.RadioButtonStyleKey;
                
                case (short)SystemResourceKeyID.ToolBarComboBoxStyle:
                    return ToolBar.ComboBoxStyleKey;
                
                case (short)SystemResourceKeyID.ToolBarTextBoxStyle:
                    return ToolBar.TextBoxStyleKey;

                case (short)SystemResourceKeyID.ToolBarMenuStyle:
                    return ToolBar.MenuStyleKey;

                case (short)SystemResourceKeyID.PowerLineStatus:
                    return SystemParameters.PowerLineStatusKey;
            }

            return null;
        }

        internal static ResourceKey GetSystemResourceKey(string keyName)
        {
            switch (keyName)
            {
                case "SystemParameters.FocusVisualStyleKey" :
                    return SystemParameters.FocusVisualStyleKey;

                case "ToolBar.ButtonStyleKey" :
                    return ToolBarButtonStyleKey;

                case "ToolBar.ToggleButtonStyleKey" :
                    return ToolBarToggleButtonStyleKey;

                case "ToolBar.CheckBoxStyleKey" :
                    return ToolBarCheckBoxStyleKey;

                case "ToolBar.RadioButtonStyleKey" :
                    return ToolBarRadioButtonStyleKey;

                case "ToolBar.ComboBoxStyleKey" :
                    return ToolBarComboBoxStyleKey;

                case "ToolBar.TextBoxStyleKey" :
                    return ToolBarTextBoxStyleKey;

                case "ToolBar.MenuStyleKey" :
                    return ToolBarMenuStyleKey;

                case "ToolBar.SeparatorStyleKey" :
                    return ToolBarSeparatorStyleKey;

                case "MenuItem.SeparatorStyleKey" :
                    return MenuItemSeparatorStyleKey;

                case "StatusBar.SeparatorStyleKey" :
                    return StatusBarSeparatorStyleKey;

                case "SystemParameters.NavigationChromeStyleKey" :
                    return SystemParameters.NavigationChromeStyleKey;

                case "SystemParameters.NavigationChromeDownLevelStyleKey" :
                    return SystemParameters.NavigationChromeDownLevelStyleKey;

                case "GridView.GridViewStyleKey" :
                    return GridViewStyleKey;

                case "GridView.GridViewScrollViewerStyleKey" :
                    return GridViewScrollViewerStyleKey;

                case "GridView.GridViewItemContainerStyleKey" :
                    return GridViewItemContainerStyleKey;

                case "DataGridColumnHeader.ColumnFloatingHeaderStyleKey" :
                    return DataGridColumnHeaderColumnFloatingHeaderStyleKey;

                case "DataGridColumnHeader.ColumnHeaderDropSeparatorStyleKey" :
                    return DataGridColumnHeaderColumnHeaderDropSeparatorStyleKey;

                case "DataGrid.FocusBorderBrushKey" :
                    return DataGridFocusBorderBrushKey;

                case "DataGridComboBoxColumn.TextBlockComboBoxStyleKey" :
                    return DataGridComboBoxColumnTextBlockComboBoxStyleKey;
            }

            return null;
        }

        internal static object GetResource(short id)
        {
            SystemResourceKeyID keyId = (SystemResourceKeyID)id;
            if (_srk == null)
            {
                _srk = new SystemResourceKey(keyId);
            }
            else
            {
                _srk._id = keyId;
            }

            return _srk.Resource;
        }

        /// <summary>
        ///     Constructs a new instance of the key with the given ID.
        /// </summary>
        /// <param name="id">The internal, unique ID of the system resource.</param>
        internal SystemResourceKey(SystemResourceKeyID id)
        {
            Debug.Assert(((SystemResourceKeyID.InternalSystemColorsStart < id) && (id < SystemResourceKeyID.InternalSystemColorsEnd)) ||
                ((SystemResourceKeyID.InternalSystemFontsStart < id) && (id < SystemResourceKeyID.InternalSystemFontsEnd)) ||
                ((SystemResourceKeyID.InternalSystemParametersStart < id) && (id < SystemResourceKeyID.InternalSystemParametersEnd)) ||
                ((SystemResourceKeyID.InternalSystemColorsExtendedStart < id) && (id < SystemResourceKeyID.InternalSystemColorsExtendedEnd)),
                String.Format("Invalid SystemResourceKeyID (id={0})", (int)id));
            _id = id;
        }

        internal SystemResourceKeyID InternalKey
        {
            get
            {
                return _id;
            }
        }

        /// <summary>
        ///     Used to determine where to look for the resource dictionary that holds this resource.
        /// </summary>
        public override Assembly Assembly
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        ///     Determines if the passed in object is equal to this object.
        ///     Two keys will be equal if they both have the same ID.
        /// </summary>
        /// <param name="o">The object to compare with.</param>
        /// <returns>True if the objects are equal. False otherwise.</returns>
        public override bool Equals(object o)
        {
            SystemResourceKey key = o as SystemResourceKey;

            if (key != null)
            {
                return (key._id == this._id);
            }

            return false;
        }

        /// <summary>
        ///     Serves as a hash function for a particular type.
        /// </summary>
        public override int GetHashCode()
        {
            return (int)_id;
        }

        /// <summary>
        ///     get string representation of this key
        /// </summary>
        /// <returns>the string representation of the key</returns>
        public override string ToString()
        {
            return _id.ToString();
        }

        #region ResourceKeys

        internal static ComponentResourceKey DataGridFocusBorderBrushKey
        {
            get
            {
                if (_focusBorderBrushKey == null)
                {
                    _focusBorderBrushKey = new ComponentResourceKey(typeof(DataGrid), "FocusBorderBrushKey");
                }

                return _focusBorderBrushKey;
            }
        }

        internal static ComponentResourceKey DataGridComboBoxColumnTextBlockComboBoxStyleKey
        {
            get
            {
                if (_textBlockComboBoxStyleKey == null)
                {
                    _textBlockComboBoxStyleKey = new ComponentResourceKey(typeof(DataGrid), "TextBlockComboBoxStyleKey");
                }

                return _textBlockComboBoxStyleKey;
            }
        }

        internal static ResourceKey MenuItemSeparatorStyleKey
        {
            get
            {
                if (_menuItemSeparatorStyleKey == null)
                {
                    _menuItemSeparatorStyleKey = new SystemThemeKey(SystemResourceKeyID.MenuItemSeparatorStyle);
                }
                return _menuItemSeparatorStyleKey;
            }
        }

        internal static ComponentResourceKey DataGridColumnHeaderColumnFloatingHeaderStyleKey
        {
            get
            {
                if (_columnFloatingHeaderStyleKey == null)
                {
                    _columnFloatingHeaderStyleKey = new ComponentResourceKey(typeof(DataGrid), "ColumnFloatingHeaderStyleKey");
                }

                return _columnFloatingHeaderStyleKey;
            }
        }

        internal static ComponentResourceKey DataGridColumnHeaderColumnHeaderDropSeparatorStyleKey
        {
            get
            {
                if (_columnHeaderDropSeparatorStyleKey == null)
                {
                    _columnHeaderDropSeparatorStyleKey = new ComponentResourceKey(typeof(DataGrid), "ColumnHeaderDropSeparatorStyleKey");
                }

                return _columnHeaderDropSeparatorStyleKey;
            }
        }

        internal static ResourceKey GridViewItemContainerStyleKey
        {
            get
            {
                if (_gridViewItemContainerStyleKey == null)
                {
                    _gridViewItemContainerStyleKey = new SystemThemeKey(SystemResourceKeyID.GridViewItemContainerStyle);
                }

                return _gridViewItemContainerStyleKey;
            }
        }

        internal static ResourceKey GridViewScrollViewerStyleKey
        {
            get
            {
                if (_scrollViewerStyleKey == null)
                {
                    _scrollViewerStyleKey = new SystemThemeKey(SystemResourceKeyID.GridViewScrollViewerStyle);
                }

                return _scrollViewerStyleKey;
            }
        }

        internal static ResourceKey GridViewStyleKey
        {
            get
            {
                if (_gridViewStyleKey == null)
                {
                    _gridViewStyleKey = new SystemThemeKey(SystemResourceKeyID.GridViewStyle);
                }

                return _gridViewStyleKey;
            }
        }

        internal static ResourceKey StatusBarSeparatorStyleKey
        {
            get
            {
                if (_statusBarSeparatorStyleKey == null)
                {
                    _statusBarSeparatorStyleKey = new SystemThemeKey(SystemResourceKeyID.StatusBarSeparatorStyle);
                }
                return _statusBarSeparatorStyleKey;
            }
        }

        internal static ResourceKey ToolBarButtonStyleKey
        {
            get
            {
                if (_cacheButtonStyle == null)
                {
                    _cacheButtonStyle = new SystemThemeKey(SystemResourceKeyID.ToolBarButtonStyle);
                }
                return _cacheButtonStyle;
            }
        }


        /// <summary>
        ///     Resource Key for the ToggleButtonStyle
        /// </summary>
        internal static ResourceKey ToolBarToggleButtonStyleKey
        {
            get
            {
                if (_cacheToggleButtonStyle == null)
                {
                    _cacheToggleButtonStyle = new SystemThemeKey(SystemResourceKeyID.ToolBarToggleButtonStyle);
                }
                return _cacheToggleButtonStyle;
            }
        }

        /// <summary>
        ///     Resource Key for the SeparatorStyle
        /// </summary>
        internal static ResourceKey ToolBarSeparatorStyleKey
        {
            get
            {
                if (_cacheSeparatorStyle == null)
                {
                    _cacheSeparatorStyle = new SystemThemeKey(SystemResourceKeyID.ToolBarSeparatorStyle);
                }
                return _cacheSeparatorStyle;
            }
        }


        /// <summary>
        ///     Resource Key for the CheckBoxStyle
        /// </summary>
        internal static ResourceKey ToolBarCheckBoxStyleKey
        {
            get
            {
                if (_cacheCheckBoxStyle == null)
                {
                    _cacheCheckBoxStyle = new SystemThemeKey(SystemResourceKeyID.ToolBarCheckBoxStyle);
                }
                return _cacheCheckBoxStyle;
            }
        }

        /// <summary>
        ///     Resource Key for the RadioButtonStyle
        /// </summary>
        internal static ResourceKey ToolBarRadioButtonStyleKey
        {
            get
            {
                if (_cacheRadioButtonStyle == null)
                {
                    _cacheRadioButtonStyle = new SystemThemeKey(SystemResourceKeyID.ToolBarRadioButtonStyle);
                }
                return _cacheRadioButtonStyle;
            }
        }


        /// <summary>
        ///     Resource Key for the ComboBoxStyle
        /// </summary>
        internal static ResourceKey ToolBarComboBoxStyleKey
        {
            get
            {
                if (_cacheComboBoxStyle == null)
                {
                    _cacheComboBoxStyle = new SystemThemeKey(SystemResourceKeyID.ToolBarComboBoxStyle);
                }
                return _cacheComboBoxStyle;
            }
        }


        /// <summary>
        ///     Resource Key for the TextBoxStyle
        /// </summary>
        internal static ResourceKey ToolBarTextBoxStyleKey
        {
            get
            {
                if (_cacheTextBoxStyle == null)
                {
                    _cacheTextBoxStyle = new SystemThemeKey(SystemResourceKeyID.ToolBarTextBoxStyle);
                }
                return _cacheTextBoxStyle;
            }
        }


        /// <summary>
        ///     Resource Key for the MenuStyle
        /// </summary>
        internal static ResourceKey ToolBarMenuStyleKey
        {
            get
            {
                if (_cacheMenuStyle == null)
                {
                    _cacheMenuStyle = new SystemThemeKey(SystemResourceKeyID.ToolBarMenuStyle);
                }
                return _cacheMenuStyle;
            }
        }

        private static SystemThemeKey _cacheSeparatorStyle;
        private static SystemThemeKey _cacheCheckBoxStyle;
        private static SystemThemeKey _cacheToggleButtonStyle;
        private static SystemThemeKey _cacheButtonStyle;
        private static SystemThemeKey _cacheRadioButtonStyle;
        private static SystemThemeKey _cacheComboBoxStyle;
        private static SystemThemeKey _cacheTextBoxStyle;
        private static SystemThemeKey _cacheMenuStyle;

        private static ComponentResourceKey _focusBorderBrushKey;
        private static ComponentResourceKey _textBlockComboBoxStyleKey;
        private static SystemThemeKey _menuItemSeparatorStyleKey;
        private static ComponentResourceKey _columnHeaderDropSeparatorStyleKey;
        private static ComponentResourceKey _columnFloatingHeaderStyleKey;
        private static SystemThemeKey _gridViewItemContainerStyleKey;
        private static SystemThemeKey _scrollViewerStyleKey;
        private static SystemThemeKey _gridViewStyleKey;
        private static SystemThemeKey _statusBarSeparatorStyleKey;

        #endregion


        private SystemResourceKeyID _id;

        [ThreadStatic]
        private static SystemResourceKey _srk = null;
#endif
    }
}
