// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Media;
using Microsoft.Win32;
using Standard;

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Shell
#else
namespace Microsoft.Windows.Shell
#endif
{
    /// <summary>
    /// Reproduces the heuristic DWM uses to pick the caption text color for a given caption background.
    /// All members are pure functions so they can be unit tested without a window.
    /// </summary>
    internal static class CaptionTextColorHelper
    {
        // Default caption backgrounds used by DWM when no accent color is shown on the title bar.
        private static readonly Color s_defaultDarkCaptionBackground = Color.FromRgb(0x2B, 0x2B, 0x2B);
        private static readonly Color s_defaultLightCaptionBackground = Colors.White;

        private const string DwmRegistryKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM";

        /// <summary>
        /// Whether a caption background is considered dark, i.e. needs a light text.
        /// This is the integer weighted luminance used by DWM (weights 2/5/1, threshold at mid range).
        /// </summary>
        internal static bool IsDark(Color color)
        {
            return color.R * 2 + color.G * 5 + color.B <= 1024;
        }

        /// <summary>Caption text color of an active window.</summary>
        /// <param name="background">The caption background, or null when the system default is used.</param>
        /// <param name="darkMode">Whether the caption is rendered with the dark theme.</param>
        internal static Color GetActiveCaptionText(Color? background, bool darkMode)
        {
            if (background.HasValue)
            {
                return IsDark(background.Value) ? Colors.White : Colors.Black;
            }

            return darkMode ? Colors.White : Colors.Black;
        }

        /// <summary>Caption text color of an inactive window: the active color blended toward the background.</summary>
        internal static Color GetInactiveCaptionText(Color? background, bool darkMode)
        {
            Color bg = background ?? (darkMode ? s_defaultDarkCaptionBackground : s_defaultLightCaptionBackground);
            Color active = GetActiveCaptionText(bg, darkMode);
            return Blend(active, bg, darkMode ? 0.4 : 0.6);
        }

        /// <summary>Linear blend of two colors: <paramref name="factor"/> = 0 returns a, 1 returns b.</summary>
        internal static Color Blend(Color a, Color b, double factor)
        {
            double aFactor = 1.0 - factor;
            return Color.FromArgb(
                (byte)Math.Round(a.A * aFactor + b.A * factor),
                (byte)Math.Round(a.R * aFactor + b.R * factor),
                (byte)Math.Round(a.G * aFactor + b.G * factor),
                (byte)Math.Round(a.B * aFactor + b.B * factor));
        }

        /// <summary>
        /// Returns the accent color when the user chose to show it on title bars ("ColorPrevalence").
        /// </summary>
        internal static bool TryGetPrevalentAccentColor(out Color accent)
        {
            accent = default(Color);

            if (!Utility.IsOSWindows10OrNewer)
            {
                return false;
            }

            int? colorPrevalence;
            try
            {
                colorPrevalence = Registry.GetValue(DwmRegistryKeyPath, "ColorPrevalence", null) as int?;
            }
            catch (Exception e) when (e is Security.SecurityException || e is IO.IOException || e is UnauthorizedAccessException)
            {
                return false;
            }

            if (colorPrevalence == null || colorPrevalence.Value == 0)
            {
                return false;
            }

            uint colorization;
            bool opaqueBlend;
            if (!NativeMethods.DwmGetColorizationColor(out colorization, out opaqueBlend))
            {
                return false;
            }

            accent = Utility.ColorFromArgbDword(colorization | 0xFF000000);
            return true;
        }
    }
}
