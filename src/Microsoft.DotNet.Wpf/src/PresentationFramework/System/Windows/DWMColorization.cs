using System;
using System.Diagnostics;
using System.Windows.Appearance;
using System.Windows.Media;
using Microsoft.Win32;
using MS.Internal;

namespace System.Windows;
internal static class DWMColorization
{
    /// <summary>
    /// Maximum <see cref="Byte"/> size with the current <see cref="Single"/> precision.
    /// </summary>
    private static readonly float _byteMax = (float)Byte.MaxValue;

    /// <summary>
    /// The maximum value of the background HSV brightness after which the text on the accent will be turned dark.
    /// </summary>
    private const double BackgroundBrightnessThresholdValue = 80d;

    /// <summary>
    /// The registry path containing colorization information.
    /// </summary>
    private static readonly string _dwmKey = "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\DWM";

    /// <summary>
    /// The Accent Color that is currently applied to the application.
    /// </summary>
    private static Color _currentApplicationAccentColor = Color.FromArgb(255, 0, 120, 212);

    internal static Color CurrentApplicationAccentColor
    {
        get { return _currentApplicationAccentColor; }
    }

    /// <summary>
    /// Computes the accent color from value in the registry key.
    /// </summary>
    /// <returns>Updated <see cref="System.Windows.Media.Color"/> Accent Color.</returns>
    /// <exception cref="NotImplementedException"></exception>
    internal static Color GetSystemAccentColor()
    {
        var dwmValue = (Int32)Registry.GetValue(
            _dwmKey,
            "AccentColor",
            null);

        ByteColor systemAccentByteValue  = new ByteColor(0xff, 0x00, 0x78, 0xd4); // Initializing the accent to default blue value

        if (dwmValue is Int32 x)
        {
            systemAccentByteValue = ParseDWordColor(x);
        }
        else
        {
            throw new NotImplementedException();
        }

        Color newAccentColor = Color.FromArgb(systemAccentByteValue.A, systemAccentByteValue.R, systemAccentByteValue.G, systemAccentByteValue.B);

        return newAccentColor;
    }

    /// <summary>
    /// Computes the current Accent Colors and calls for updating of accent color values in resource dictionary
    /// </summary>
    internal static void ApplyAccentColors()
    {
        Color systemAccent = GetSystemAccentColor();

        Color primaryAccent;
        Color secondaryAccent;
        Color tertiaryAccent;

        bool isDarkTheme = Application.isThemeDark();

        if (isDarkTheme)
        {
            primaryAccent = UpdateColor(systemAccent, 15f, -12f);
            secondaryAccent = UpdateColor(systemAccent, 30f, -24f);
            tertiaryAccent = UpdateColor(systemAccent, 45f, -36f);
        }
        else
        {
            primaryAccent = UpdateColorBrightness(systemAccent, -5f);
            secondaryAccent = UpdateColorBrightness(systemAccent, -10f);
            tertiaryAccent = UpdateColorBrightness(systemAccent, -15f);
        }

        UpdateColorResources(systemAccent, primaryAccent, secondaryAccent, tertiaryAccent);

        _currentApplicationAccentColor = systemAccent;
    }

    /// <summary>
    /// Updates application resources.
    /// </summary>        
    private static void UpdateColorResources(
        Color systemAccent,
        Color primaryAccent,
        Color secondaryAccent,
        Color tertiaryAccent)
    {
#if DEBUG
        System.Diagnostics.Debug.WriteLine("INFO | SystemAccentColor: " + systemAccent, "System.Windows.Accent");
        System
            .Diagnostics
            .Debug
            .WriteLine("INFO | SystemAccentColorPrimary: " + primaryAccent, "System.Windows.Accent");
        System
            .Diagnostics
            .Debug
            .WriteLine("INFO | SystemAccentColorSecondary: " + secondaryAccent, "System.Windows.Accent");
        System
            .Diagnostics
            .Debug
            .WriteLine("INFO | SystemAccentColorTertiary: " + tertiaryAccent, "System.Windows.Accent");
#endif

        if (GetBrightness(secondaryAccent) > BackgroundBrightnessThresholdValue)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("INFO | Text on accent is DARK", "System.Windows.Accent");
#endif
            Application.Current.Resources["TextOnAccentFillColorPrimary"] = Color.FromArgb(
                0xFF,
                0x00,
                0x00,
                0x00
            );
            Application.Current.Resources["TextOnAccentFillColorSecondary"] = Color.FromArgb(
                0x80,
                0x00,
                0x00,
                0x00
            );
            Application.Current.Resources["TextOnAccentFillColorDisabled"] = Color.FromArgb(
                0x77,
                0x00,
                0x00,
                0x00
            );
            Application.Current.Resources["TextOnAccentFillColorSelectedText"] = Color.FromArgb(
                0x00,
                0x00,
                0x00,
                0x00
            );
            Application.Current.Resources["AccentTextFillColorDisabled"] = Color.FromArgb(
                0x5D,
                0x00,
                0x00,
                0x00
            );
        }
        else
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("INFO | Text on accent is LIGHT", "System.Windows.Accent");
#endif
            Application.Current.Resources["TextOnAccentFillColorPrimary"] = Color.FromArgb(
                0xFF,
                0xFF,
                0xFF,
                0xFF
            );
            Application.Current.Resources["TextOnAccentFillColorSecondary"] = Color.FromArgb(
                0x80,
                0xFF,
                0xFF,
                0xFF
            );
            Application.Current.Resources["TextOnAccentFillColorDisabled"] = Color.FromArgb(
                0x87,
                0xFF,
                0xFF,
                0xFF
            );
            Application.Current.Resources["TextOnAccentFillColorSelectedText"] = Color.FromArgb(
                0xFF,
                0xFF,
                0xFF,
                0xFF
            );
            Application.Current.Resources["AccentTextFillColorDisabled"] = Color.FromArgb(
                0x5D,
                0xFF,
                0xFF,
                0xFF
            );
        }

        Application.Current.Resources["SystemAccentColor"] = systemAccent;
        Application.Current.Resources["SystemAccentColorPrimary"] = primaryAccent;
        Application.Current.Resources["SystemAccentColorSecondary"] = secondaryAccent;
        Application.Current.Resources["SystemAccentColorTertiary"] = tertiaryAccent;

        Application.Current.Resources["SystemAccentBrush"] = ToBrush(systemAccent);
        Application.Current.Resources["SystemFillColorAttentionBrush"] = ToBrush(secondaryAccent);
        Application.Current.Resources["AccentTextFillColorPrimaryBrush"] = ToBrush(tertiaryAccent);
        Application.Current.Resources["AccentTextFillColorSecondaryBrush"] = ToBrush(tertiaryAccent);
        Application.Current.Resources["AccentTextFillColorTertiaryBrush"] = ToBrush(secondaryAccent);
        Application.Current.Resources["AccentFillColorSelectedTextBackgroundBrush"] = ToBrush(systemAccent);
        Application.Current.Resources["AccentFillColorDefaultBrush"] = ToBrush(secondaryAccent);

        Application.Current.Resources["AccentFillColorSecondaryBrush"] = ToBrush(secondaryAccent, 0.9);
        Application.Current.Resources["AccentFillColorTertiaryBrush"] = ToBrush(secondaryAccent, 0.8);
    }

    /// <summary>
    /// Allows to change the brightness, saturation and luminance by a factors based on the HSL and HSV color space.
    /// </summary>
    /// <param name="color">Color to convert.</param>
    /// <param name="brightnessFactor">The value of the brightness change factor from <see langword="100"/> to <see langword="-100"/>.</param>
    /// <param name="saturationFactor">The value of the saturation change factor from <see langword="100"/> to <see langword="-100"/>.</param>
    /// <param name="luminanceFactor">The value of the luminance change factor from <see langword="100"/> to <see langword="-100"/>.</param>
    /// <returns>Updated <see cref="System.Windows.Media.Color"/>.</returns>
    private static Color UpdateColor(
        Color color,
        float brightnessFactor,
        float saturationFactor = 0,
        float luminanceFactor = 0)
    {
        if (brightnessFactor > 100f || brightnessFactor < -100f)
        {
            throw new ArgumentOutOfRangeException(nameof(brightnessFactor));
        }

        if (saturationFactor > 100f || saturationFactor < -100f)
        {
            throw new ArgumentOutOfRangeException(nameof(saturationFactor));
        }

        if (luminanceFactor > 100f || luminanceFactor < -100f)
        {
            throw new ArgumentOutOfRangeException(nameof(luminanceFactor));
        }

        (float hue, float rawSaturation, float rawBrightness) = ToHsv(color);

        (int red, int green, int blue) = FromHsvToRgb(
            hue,
            ToPercentage(rawSaturation + saturationFactor),
            ToPercentage(rawBrightness + brightnessFactor)
        );

        if (luminanceFactor == 0)
        {
            return Color.FromArgb(color.A, ToColorByte(red), ToColorByte(green), ToColorByte(blue));
        }

        (hue, float saturation, float rawLuminance) = ToHsl(Color.FromArgb(color.A, ToColorByte(red), ToColorByte(green), ToColorByte(blue)));

        (red, green, blue) = FromHslToRgb(hue, saturation, ToPercentage(rawLuminance + luminanceFactor));

        return Color.FromArgb(color.A, ToColorByte(red), ToColorByte(green), ToColorByte(blue));
    }

    /// <summary>
    /// Allows to change the brightness by a factor based on the HSV color space.
    /// </summary>
    /// <param name="color">Input color.</param>
    /// <param name="factor">The value of the brightness change factor from <see langword="100"/> to <see langword="-100"/>.</param>
    /// <returns>Updated <see cref="System.Windows.Media.Color"/>.</returns>
    private static Color UpdateColorBrightness(Color color, float factor)
    {
        if (factor > 100f || factor < -100f)
        {
            throw new ArgumentOutOfRangeException(nameof(factor));
        }

        (float hue, float saturation, float rawBrightness) = ToHsv(color);

        (int red, int green, int blue) = FromHsvToRgb(hue, saturation, ToPercentage(rawBrightness + factor));

        return Color.FromArgb(color.A, ToColorByte(red), ToColorByte(green), ToColorByte(blue));
    }

    /// <summary>
    /// Gets <see cref="System.Windows.Media.Color"/> brightness based on HSV space.
    /// </summary>
    /// <param name="color">Input color.</param>
    public static double GetBrightness(Color color)
    {
        (float _, float _, float brightness) = ToHsv(color);

        return (double)brightness;
    }

    /// <summary>
    /// Converts the color values stored as HSV (HSB) to RGB.
    /// </summary>
    private static (int R, int G, int B) FromHsvToRgb(float hue, float saturation, float brightness)
    {
        var red = 0;
        var green = 0;
        var blue = 0;

        if (AlmostEquals(saturation, 0, 0.01f))
        {
            red = green = blue = (int)(((brightness / 100f) * _byteMax) + 0.5f);

            return (red, green, blue);
        }

        hue /= 360f;
        brightness /= 100f;
        saturation /= 100f;

        var hueAngle = (hue - (float)Math.Floor(hue)) * 6.0f;
        var f = hueAngle - (float)Math.Floor(hueAngle);

        var p = brightness * (1.0f - saturation);
        var q = brightness * (1.0f - saturation * f);
        var t = brightness * (1.0f - (saturation * (1.0f - f)));

        switch ((int)hueAngle)
        {
            case 0:
                red = (int)(brightness * 255.0f + 0.5f);
                green = (int)(t * 255.0f + 0.5f);
                blue = (int)(p * 255.0f + 0.5f);

                break;
            case 1:
                red = (int)(q * 255.0f + 0.5f);
                green = (int)(brightness * 255.0f + 0.5f);
                blue = (int)(p * 255.0f + 0.5f);

                break;
            case 2:
                red = (int)(p * 255.0f + 0.5f);
                green = (int)(brightness * 255.0f + 0.5f);
                blue = (int)(t * 255.0f + 0.5f);

                break;
            case 3:
                red = (int)(p * 255.0f + 0.5f);
                green = (int)(q * 255.0f + 0.5f);
                blue = (int)(brightness * 255.0f + 0.5f);

                break;
            case 4:
                red = (int)(t * 255.0f + 0.5f);
                green = (int)(p * 255.0f + 0.5f);
                blue = (int)(brightness * 255.0f + 0.5f);

                break;
            case 5:
                red = (int)(brightness * 255.0f + 0.5f);
                green = (int)(p * 255.0f + 0.5f);
                blue = (int)(q * 255.0f + 0.5f);

                break;
        }

        return (red, green, blue);
    }

    /// <summary>
    /// Converts the color values stored as HSL to RGB.
    /// </summary>
    private static (int R, int G, int B) FromHslToRgb(float hue, float saturation, float lightness)
    {
        if (AlmostEquals(saturation, 0, 0.01f))
        {
            var color = (int)(lightness * _byteMax);

            return (color, color, color);
        }

        lightness /= 100f;
        saturation /= 100f;

        var hueAngle = hue / 360f;

        return (
            CalcHslChannel(hueAngle + 0.333333333f, saturation, lightness),
            CalcHslChannel(hueAngle, saturation, lightness),
            CalcHslChannel(hueAngle - 0.333333333f, saturation, lightness)
        );
    }

    /// <summary>
    /// HSV representation models how colors appear under light.
    /// </summary>
    /// <returns><see langword="float"/> hue, <see langword="float"/> saturation, <see langword="float"/> brightness</returns>
    private static (float Hue, float Saturation, float Value) ToHsv(Color color)
    {
        int red = color.R;
        int green = color.G;
        int blue = color.B;

        var max = Math.Max(red, Math.Max(green, blue));
        var min = Math.Min(red, Math.Min(green, blue));

        var fDelta = (max - min) / _byteMax;

        float hue;
        float saturation;
        float value;

        if (max <= 0)
        {
            return (0f, 0f, 0f);
        }

        saturation = fDelta / (max / _byteMax);
        value = max / _byteMax;

        if (fDelta <= 0.0)
        {
            return (0f, saturation * 100f, value * 100f);
        }

        if (max == red)
        {
            hue = ((green - blue) / _byteMax) / fDelta;
        }
        else if (max == green)
        {
            hue = 2f + (((blue - red) / _byteMax) / fDelta);
        }
        else
        {
            hue = 4f + (((red - green) / _byteMax) / fDelta);
        }

        if (hue < 0)
        {
            hue += 360;
        }

        return (hue * 60f, saturation * 100f, value * 100f);
    }

    /// <summary>
    /// HSL representation models the way different paints mix together to create colour in the real world,
    /// with the lightness dimension resembling the varying amounts of black or white paint in the mixture.
    /// </summary>
    /// <returns><see langword="float"/> hue, <see langword="float"/> saturation, <see langword="float"/> lightness</returns>
    private static (float Hue, float Saturation, float Lightness) ToHsl(Color color)
    {
        int red = color.R;
        int green = color.G;
        int blue = color.B;

        var max = Math.Max(red, Math.Max(green, blue));
        var min = Math.Min(red, Math.Min(green, blue));

        var fDelta = (max - min) / _byteMax;

        float hue;
        float saturation;
        float lightness;

        if (max <= 0)
        {
            return (0f, 0f, 0f);
        }

        saturation = 0.0f;
        lightness = ((max + min) / _byteMax) / 2.0f;

        if (fDelta <= 0.0)
        {
            return (0f, saturation * 100f, lightness * 100f);
        }

        saturation = fDelta / (max / _byteMax);

        if (max == red)
        {
            hue = ((green - blue) / _byteMax) / fDelta;
        }
        else if (max == green)
        {
            hue = 2f + (((blue - red) / _byteMax) / fDelta);
        }
        else
        {
            hue = 4f + (((red - green) / _byteMax) / fDelta);
        }

        if (hue < 0)
        {
            hue += 360;
        }

        return (hue * 60f, saturation * 100f, lightness * 100f);
    }

    /// <summary>
    /// Calculates the color component for HSL.
    /// </summary>
    private static int CalcHslChannel(float color, float saturation, float lightness)
    {
        float num1,
            num2;

        if (color > 1)
        {
            color -= 1f;
        }

        if (color < 0)
        {
            color += 1f;
        }

        if (lightness < 0.5f)
        {
            num1 = lightness * (1f + saturation);
        }
        else
        {
            num1 = lightness + saturation - lightness * saturation;
        }

        num2 = (2f * lightness) - num1;

        if (color * 6f < 1)
        {
            return (int)((num2 + (num1 - num2) * 6f * color) * _byteMax);
        }

        if (color * 2f < 1)
        {
            return (int)(num1 * _byteMax);
        }

        if (color * 3f < 2)
        {
            return (int)((num2 + (num1 - num2) * (0.666666666f - color) * 6f) * _byteMax);
        }

        return (int)(num2 * _byteMax);
    }

    /// <summary>
    /// Whether the floating point number is about the same.
    /// </summary>
    private static bool AlmostEquals(float numberOne, float numberTwo, float precision = 0)
    {
        if (precision <= 0)
        {
            precision = Single.Epsilon;
        }

        return numberOne >= (numberTwo - precision) && numberOne <= (numberTwo + precision);
    }

    /// <summary>
    /// Absolute percentage.
    /// </summary>
    private static float ToPercentage(float value)
    {
        return value switch
        {
            > 100f => 100f,
            < 0f => 0f,
            _ => value
        };
    }

    /// <summary>
    /// Absolute byte.
    /// </summary>
    private static byte ToColorByte(int value)
    {
        if (value > Byte.MaxValue)
        {
            value = Byte.MaxValue;
        }
        else if (value < Byte.MinValue)
        {
            value = Byte.MinValue;
        }

        return Convert.ToByte(value);
    }

    /// <summary>
    /// Converts the color of type Int32 to type ByteColor
    /// </summary>
    /// <param name="color">The Int32 color to be converted to corresponding ByteColor</param>
    /// <returns>Corresponding <see cref="System.Windows.ByteColor"/></returns>
    private static ByteColor ParseDWordColor(Int32 color)
    {
        Byte
            a = (byte)((color >> 24) & 0xFF),
            b = (byte)((color >> 16) & 0xFF),
            g = (byte)((color >> 8) & 0xFF),
            r = (byte)((color >> 0) & 0xFF);

        ByteColor current = new ByteColor(a, r, g, b);

        return current;
    }

    /// <summary>
    /// Creates a <see cref="SolidColorBrush"/> from a <see cref="System.Windows.Media.Color"/>.
    /// </summary>
    /// <param name="color">Input color.</param>
    /// <returns>Brush converted to color.</returns>
    private static SolidColorBrush ToBrush(Color color)
    {
        return new SolidColorBrush(color);
    }

    /// <summary>
    /// Creates a <see cref="SolidColorBrush"/> from a <see cref="System.Windows.Media.Color"/> with defined brush opacity.
    /// </summary>
    /// <param name="color">Input color.</param>
    /// <param name="opacity">Degree of opacity.</param>
    /// <returns>Brush converted to color with modified opacity.</returns>
    private static SolidColorBrush ToBrush(Color color, double opacity)
    {
        return new SolidColorBrush { Color = color, Opacity = opacity };
    }
}