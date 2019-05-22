// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows.Ink
{
    /// <summary>Flag values which can help the renderer decide how to
    /// draw the ink strokes</summary>
    [Flags]
    internal enum DrawingFlags
    {
        /// <summary>The stroke should be drawn as a polyline</summary>
        Polyline = 0x00000000,
        /// <summary>The stroke should be fit to a curve, such as a bezier.</summary>
        FitToCurve = 0x00000001,
        /// <summary>The stroke should be rendered by subtracting its rendering values
        /// from those on the screen</summary>
        SubtractiveTransparency = 0x00000002,
        /// <summary>Ignore any stylus pressure information when rendering</summary>
        IgnorePressure = 0x00000004,
        /// <summary>The stroke should be rendered with anti-aliased edges</summary>
        AntiAliased = 0x00000010,
        /// <summary>Ignore any stylus rotation information when rendering</summary>
        IgnoreRotation = 0x00000020,
        /// <summary>Ignore any stylus angle information when rendering</summary>
        IgnoreAngle = 0x00000040,
    };
}
