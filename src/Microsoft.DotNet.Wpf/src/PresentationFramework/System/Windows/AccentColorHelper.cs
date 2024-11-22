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
        if (UISettings.TryGetColorValue(uiColorType, out Color color))
        {
            return color;
        }

        return _defaultAccentColor;
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

    private static UISettings UISettings
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

    private static readonly Color _defaultAccentColor = Color.FromArgb(0xff, 0x00, 0x78, 0xd4);

    private static UISettings _uiSettings = null;

    #endregion
}