// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;                 // for Rect    WindowsBase.dll
using System.Windows.Media;           // for Brush, ImageData, Geometry. PresentationCore.dll
using System.Windows.Media.Imaging;

namespace System.Windows.Xps.Serialization
{
    /// <summary>
    /// Simplified version of DrawingContext to interface with NGC
    /// </summary>
    internal interface IMetroDrawingContext
    {
        /// <summary>
        /// Draw a Metro compatible Geometry
        /// </summary>
        /// <param name="brush"></param>
        /// <param name="pen"></param>
        /// <param name="geometry"></param>
        void DrawGeometry(Brush brush, Pen pen, Geometry geometry);

        /// <summary>
        /// Draw a BitmapSource
        /// </summary>
        /// <param name="image"></param>
        /// <param name="rectangle"></param>
        void DrawImage(ImageSource image, Rect rectangle);

        /// <summary>
        /// Draw a GlyphRun
        /// </summary>
        /// <param name="foreground"></param>
        /// <param name="glyphRun"></param>
        void DrawGlyphRun(Brush foreground, GlyphRun glyphRun);

        /// <summary>
        /// Push transform, clip, opacity and opacityMask
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="clip"></param>
        /// <param name="opacity"></param>
        /// <param name="opacityMask"></param>
        /// <param name="maskBounds"></param>
        /// <param name="onePrimitive"></param>
        /// <param name="nameAttr"></param>
        /// <param name="node">Current visual node, used for property query</param>
        /// <param name="navigateUri">Hyperlink associated with children drawings</param>
        /// <param name="edgeMode">Preserve RenderOptions.EdgeMode during serialization</param>
        void Push(
            Matrix transform,
            Geometry clip,
            double opacity,
            Brush opacityMask,
            Rect maskBounds,
            bool onePrimitive,
            String nameAttr,
            Visual node,
            Uri navigateUri,
            EdgeMode edgeMode
            );

        /// <summary>
        /// Undo the last Push
        /// </summary>
        void Pop();

        /// <summary>
        /// Add comment
        /// </summary>
        /// <param name="message"></param>
        void Comment(string message);
    }
}

namespace Microsoft.Internal.AlphaFlattener
{
    /// <summary>
    /// Flags for IProxyDrawingContext primitive drawing.
    /// </summary>
    [Flags]
    internal enum ProxyDrawingFlags
    {
        /// <summary>
        /// No drawing flags.
        /// </summary>
        None = 0,

        /// <summary>
        /// Snap primitive boundaries with clipping considered to pixels.
        /// </summary>
        /// <remarks>
        /// Needed to fix bug 1308518 where unfolding DrawingBrush tiles introduces "gap" between tiles
        /// as a result of anti-aliasing due to non-pixel-aligned boundaries.
        /// </remarks>
        PixelSnapBounds = 0x00000001,
    }

    /// <summary>
    /// Drawing interface which accepts BrushProxy, PenProxy, ImageProxy.
    /// </summary>
    internal interface IProxyDrawingContext
    {
        void Push(double opacity, BrushProxy opacityMask);

        void Pop();

        void DrawGeometry(BrushProxy brush, PenProxy pen, Geometry geometry, Geometry clip, Matrix brushTrans, ProxyDrawingFlags flags);

        void DrawImage(ImageProxy image, Rect dest, Geometry clip, Matrix trans);

        bool DrawGlyphs(GlyphRun glyphrun, Geometry clip, Matrix trans, BrushProxy foreground);

        void Comment(string message);
    }
}
