// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Windows.Appearance
{

    public static class ApplicationAccentColorManager
    {
    }


    public static class ApplicationThemeManager 
    { 

    }

    public enum ApplicationTheme
    {
        /// <summary>
        /// Unknown application theme.
        /// </summary>
        Unknown,

        /// <summary>
        /// Dark application theme.
        /// </summary>
        Dark,

        /// <summary>
        /// Light application theme.
        /// </summary>
        Light,

        /// <summary>
        /// High contract application theme.
        /// </summary>
        HighContrast
    }


    public enum SystemTheme
    {
        /// <summary>
        /// Unknown Windows theme.
        /// </summary>
        Unknown,

        /// <summary>
        /// Custom Windows theme.
        /// </summary>
        Custom,

        /// <summary>
        /// Default light theme.
        /// </summary>
        Light,

        /// <summary>
        /// Default dark theme.
        /// </summary>
        Dark,

        /// <summary>
        /// High-contrast theme: Desert
        /// </summary>
        HCWhite,

        /// <summary>
        /// High-contrast theme: Acquatic
        /// </summary>
        HCBlack,

        /// <summary>
        /// High-contrast theme: Dusk
        /// </summary>
        HC1,

        /// <summary>
        /// High-contrast theme: Nightsky
        /// </summary>
        HC2,

        /// <summary>
        /// Dark theme: Glow
        /// </summary>
        Glow,

        /// <summary>
        /// Dark theme: Captured Motion
        /// </summary>
        CapturedMotion,

        /// <summary>
        /// Light theme: Sunrise
        /// </summary>
        Sunrise,

        /// <summary>
        /// Light theme: Flow
        /// </summary>
        Flow

    }
}