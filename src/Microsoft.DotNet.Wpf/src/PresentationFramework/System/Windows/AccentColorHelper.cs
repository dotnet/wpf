using System;
using System.Windows.Media;
using System.Windows.Appearance;

using MS.Internal;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using MS.Internal.WindowsRuntime.Windows.UI.ViewManagement;


namespace System.Windows;

internal static class AccentColorHelper
{

    #region Internal Methods

    internal static Color GetAccentColor(UISettingsRCW.UIColorType uiColorType = UISettingsRCW.UIColorType.Accent)
    {
        Color color = _defaultAccentColor;

        if (_UISettings.TryGetColorValue(uiColorType, out color))
        {
            return color;
        }

        return color;
    }

    #endregion

    #region Internal Properties

    internal static Color SystemAccentColor
    {
        get
        {
            return GetAccentColor(UISettingsRCW.UIColorType.Accent);
        }
    }

    internal static UISettings _UISettings
    {
        get
        {
            if (_uiSettings == null)
            {
                _uiSettings = new UISettings();
            }

            return _uiSettings;
        }
    }

    #endregion

    #region Private Fields

    private static Color _defaultAccentColor = Color.FromArgb(0xff, 0x00, 0x78, 0xd4);

    private static UISettings _uiSettings = null;

    #endregion
}