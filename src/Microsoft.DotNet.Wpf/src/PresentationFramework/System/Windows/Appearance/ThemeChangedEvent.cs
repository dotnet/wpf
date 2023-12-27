// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.
using System.Windows.Media;
namespace System.Windows.Appearance;

/// <summary>
/// Event triggered when application theme is updated.
/// </summary>
/// <param name="currentApplicationTheme">Current application <see cref="ApplicationTheme"/>.</param>
/// <param name="systemAccent">Current base system accent <see cref="Color"/>.</param>
public delegate void ThemeChangedEvent(ApplicationTheme currentApplicationTheme, Color systemAccent);
