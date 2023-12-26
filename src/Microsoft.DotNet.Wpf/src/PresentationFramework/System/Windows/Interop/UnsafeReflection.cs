// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

// This Source Code is partially based on reverse engineering of the Windows Operating System,
// and is intended for use on Windows systems only.
// This Source Code is partially based on the source code provided by the .NET Foundation.

using System.Windows.Controls;
// using Wpf.Ui.TaskBar;

namespace System.Windows.Interop;

/// <summary>
/// A set of dangerous methods to modify the appearance.
/// </summary>
internal static class UnsafeReflection
{
    /// <summary>
    /// Casts <see cref="BackgroundType"/> to <see cref="Dwmapi.DWMSBT"/>.
    /// </summary>
    public static Dwmapi.DWMSBT Cast(WindowBackdropType backgroundType)
    {
        return backgroundType switch
        {
            WindowBackdropType.Auto => Dwmapi.DWMSBT.DWMSBT_AUTO,
            WindowBackdropType.Mica => Dwmapi.DWMSBT.DWMSBT_MAINWINDOW,
            WindowBackdropType.Acrylic => Dwmapi.DWMSBT.DWMSBT_TRANSIENTWINDOW,
            WindowBackdropType.Tabbed => Dwmapi.DWMSBT.DWMSBT_TABBEDWINDOW,
            _ => Dwmapi.DWMSBT.DWMSBT_DISABLE
        };
    }
}
