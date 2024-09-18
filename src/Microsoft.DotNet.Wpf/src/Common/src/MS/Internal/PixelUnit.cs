// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace MS.Internal
{
    internal readonly struct PixelUnit
    {
        public readonly string Name;
        public readonly double Factor;

        private PixelUnit(string name, double factor)
        {
            Name = name;
            Factor = factor;
        }

        public static bool TryParsePixel(ReadOnlySpan<char> value, out PixelUnit pixelUnit)
        {
            if (value.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            {
                pixelUnit = new PixelUnit("px", 1.0);
                return true;
            }
            else
            {
                pixelUnit = default;
                return false;
            }
        }

        public static bool TryParsePixelPerInch(ReadOnlySpan<char> value, out PixelUnit pixelUnit)
        {
            if (value.EndsWith("in", StringComparison.OrdinalIgnoreCase))
            {
                pixelUnit = new PixelUnit("in", 96.0);
                return true;
            }
            else
            {
                pixelUnit = default;
                return false;
            }
        }

        public static bool TryParsePixelPerCentimeter(ReadOnlySpan<char> value, out PixelUnit pixelUnit)
        {
            if (value.EndsWith("cm", StringComparison.OrdinalIgnoreCase))
            {
                pixelUnit = new PixelUnit("cm", 96.0 / 2.54);
                return true;
            }
            else
            {
                pixelUnit = default;
                return false;
            }
        }

        public static bool TryParsePixelPerPoint(ReadOnlySpan<char> value, out PixelUnit pixelUnit)
        {
            if (value.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
            {
                pixelUnit = new PixelUnit("pt", 96.0 / 72.0);
                return true;
            }
            else
            {
                pixelUnit = default;
                return false;
            }
        }
    }
}
