// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Test.Graphics.Factories;

namespace Microsoft.Test.Graphics.ReferenceRender
{
    /// <summary/>
    public enum DepthTestFunction
    {
        /// <summary/>
        LessThan, // This will be the default
        /// <summary/>
        Never,
        /// <summary/>
        LessThanOrEqualTo,
        /// <summary/>
        EqualTo,
        /// <summary/>
        GreaterThan,
        /// <summary/>
        GreaterThanOrEqualTo,
        /// <summary/>
        Always
    }

    /// <summary>
    /// Abstracts framebuffer operations for test renderer.
    /// </summary>
    public class RenderBuffer
    {
        #region Constructors
        /// <summary/>
        public RenderBuffer(int width, int height)
        {
            this.height = height;
            this.width = width;
            frameBuffer = new Color[width, height];
            toleranceBuffer = new Color[width, height];
            zBuffer = new float[width, height];
            writeToZBuffer = true;
            depthTest = DepthTestFunction.LessThanOrEqualTo;

            ClearZBuffer();
            ClearFrameBuffer();  // this also clears tolerance buffer
        }

        /// <summary>
        /// Constructor that takes a background color.
        /// </summary>
        /// <param name="width">Buffer width, in pixels.</param>
        /// <param name="height">Buffer height, in pixels.</param>
        /// <param name="backgroundColor">Color for lowest blend layer. Must not be premultiplied.</param>
        public RenderBuffer(int width, int height, Color backgroundColor)
            : this(width, height)
        {
            Color background = ColorOperations.PreMultiplyColor(backgroundColor);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    frameBuffer[x, y] = background;
                }
            }
        }

        /// <summary>
        /// Constructor that blends and image with a background color.
        /// </summary>
        /// <param name="image">An image that will be premultiplied and blended with the background color.</param>
        /// <param name="backgroundColor">Color for lowest blend layer. Must not be premultiplied.</param>
        public RenderBuffer(Color[,] image, Color backgroundColor)
            : this(image.GetLength(0), image.GetLength(1))
        {
            Color background = ColorOperations.PreMultiplyColor(backgroundColor);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // copy image over background color using SRC over alpha blend
                    frameBuffer[x, y] = ColorOperations.PreMultipliedAlphaBlend(
                            ColorOperations.PreMultiplyColor(image[x, y]),
                            background);
                }
            }
        }

        /// <summary>
        /// This constructor is private because we don't want to cause confusion between
        /// which take premultiplied colors and which do not.  All public constructors
        /// expect non-premultiplied colors.
        /// </summary>
        /// <remarks>
        /// We may want to make all constructors private and have descriptive static methods
        /// perform the actual construction...
        /// </remarks>
        private RenderBuffer(Color[,] premultipliedImage)
            : this(premultipliedImage.GetLength(0), premultipliedImage.GetLength(1))
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    frameBuffer[x, y] = premultipliedImage[x, y];
                }
            }
        }

        /// <summary>
        /// Constructor that fills the framebuffer with an image.
        /// </summary>
        /// <param name="premultipliedImage">An image that will be premultiplied and pasted into the framebuffer.</param>
        public static RenderBuffer FromPremultipliedImage(Color[,] premultipliedImage)
        {
            return new RenderBuffer(premultipliedImage);
        }

        /// <summary>
        /// Copy constructor for Clone()
        /// </summary>
        private RenderBuffer(RenderBuffer copy)
        {
            width = copy.Width;
            height = copy.Height;

            frameBuffer = new Color[width, height];
            toleranceBuffer = new Color[width, height];
            zBuffer = new float[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    frameBuffer[x, y] = copy.frameBuffer[x, y];
                    toleranceBuffer[x, y] = copy.toleranceBuffer[x, y];
                    zBuffer[x, y] = copy.zBuffer[x, y];
                }
            }

            depthTest = copy.depthTest;
            writeToZBuffer = copy.writeToZBuffer;
        }

        /// <summary>
        /// Create an exact copy (not a reference) of the RenderBuffer.
        /// </summary>
        public RenderBuffer Clone()
        {
            return new RenderBuffer(this);
        }

        #endregion

        #region Buffer-wide operations

        /// <summary>
        /// Create a new RenderBuffer that has 4X blend per pixel
        /// </summary>
        /// <returns>A copy of the current RenderBuffer that is 1/4 of the original size</returns>
        public RenderBuffer DownSample4X()
        {
            RenderBuffer copy = new RenderBuffer(this.width / 2, this.height / 2);

            for (int y = 0; y < height; y += 2)
            {
                for (int x = 0; x < width; x += 2)
                {
                    int copyX = x / 2;
                    int copyY = y / 2;

                    // Blend the Color, Tolerance and Z values by doing a 4-way average
                    copy.frameBuffer[copyX, copyY] = MathEx.Average(
                            frameBuffer[x, y],
                            frameBuffer[x, y + 1],
                            frameBuffer[x + 1, y],
                            frameBuffer[x + 1, y + 1]
                            );
                    copy.toleranceBuffer[copyX, copyY] = MathEx.Average(
                            toleranceBuffer[x, y],
                            toleranceBuffer[x, y + 1],
                            toleranceBuffer[x + 1, y],
                            toleranceBuffer[x + 1, y + 1]
                            );
                    copy.zBuffer[copyX, copyY] = MathEx.Average(
                            zBuffer[x, y],
                            zBuffer[x, y + 1],
                            zBuffer[x + 1, y],
                            zBuffer[x + 1, y + 1]
                            );
                }
            }

            return copy;
        }

        /// <summary>
        /// Adjusts buffers for 16-bit rendering if needed
        /// </summary>
        public void EnsureCorrectBitDepth()
        {
            switch (ColorOperations.BitDepth)
            {
                case 16:
                    ConvertTo16BitColor();
                    break;

                case 32:
                    break;

                default:
                    throw new ApplicationException("Unsupported bit depth " + ColorOperations.BitDepth + " specified for rendering");
            }
        }

        private void ConvertTo16BitColor()
        {
            // TODO:
            //  The default tolerance has probably already been added during "AddDefaultTolerances"
            //  Consider fixing default tolerance in that method so that we don't have to touch it here.
            Color roundingTolerance = ColorOperations.ConvertToleranceFrom32BitTo16Bit(RenderTolerance.DefaultColorTolerance);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    frameBuffer[x, y] = ColorOperations.ConvertFrom32BitTo16Bit(frameBuffer[x, y]);
                    toleranceBuffer[x, y] = ColorOperations.Add(toleranceBuffer[x, y], roundingTolerance);
                }
            }
        }

        /// <summary>
        /// Turns all rendered pixels black
        /// </summary>
        public void MakeSilhouette()
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (zBuffer[x, y] != zBufferClearValue)
                    {
                        frameBuffer[x, y] = Colors.Black;
                    }
                }
            }
        }

        /// <summary>
        /// Clears the depth buffer.
        /// </summary>
        public void ClearZBuffer()
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    zBuffer[x, y] = zBufferClearValue;
                }
            }
        }

        /// <summary>
        /// Clears the frame buffer and the associated tolerance buffer.
        /// </summary>
        public void ClearFrameBuffer()
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    frameBuffer[x, y] = frameBufferClearValue;
                    toleranceBuffer[x, y] = toleranceBufferClearValue;
                }
            }
        }

        /// <summary>
        /// Clears the tolerance buffer to a given value.
        /// </summary>
        /// <param name="val">Tolerance clear color</param>
        public void ClearToleranceBuffer(Color val)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    toleranceBuffer[x, y] = val;
                }
            }
        }

        /// <summary>
        /// Blend the contents of the RenderBuffer with a background color
        /// </summary>
        public void AddBackground(Color opaqueBackgroundColor)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // We keep our colors and tolerances premultiplied.
                    // Do the tolerance blend first so that we have the correct alpha value from the framebuffer
                    toleranceBuffer[x, y] = ColorOperations.PreMultipliedToleranceBlend(
                            toleranceBuffer[x, y],
                            toleranceBufferClearValue,
                            frameBuffer[x, y].A);

                    frameBuffer[x, y] = ColorOperations.PreMultipliedAlphaBlend(
                            frameBuffer[x, y],
                            opaqueBackgroundColor);

                }
            }
        }

        /// <summary>
        /// Adds the default tolerance to the tolerance buffer for all rendered pixels (skip transparency)
        /// in the frame buffer.
        /// Also adds border tolerance if IgnoreViewportBorders is set.
        /// </summary>
        public void AddDefaultTolerances()
        {
            Color highDpiTolerance = Color.FromArgb(0, 0, 0, 0);
            if (!RenderTolerance.IsSquare96Dpi)
            {
                highDpiTolerance = Color.FromArgb(0, 3, 3, 3);
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (IsPixelRendered(x, y))
                    {
                        // Add default tolerance if we lit this pixel
                        AddToTolerance(x, y, RenderTolerance.DefaultColorTolerance + highDpiTolerance);
                    }
                }
            }

            if (RenderTolerance.IgnoreViewportBorders)
            {
                Color tolerance = Color.FromArgb(255, 255, 255, 255);
                int w = width - 1;
                int h = height - 1;
                for (int y = 0; y < height; y++)
                {
                    toleranceBuffer[0, y] = tolerance;
                    toleranceBuffer[w, y] = tolerance;
                }
                for (int x = 0; x < width; x++)
                {
                    toleranceBuffer[x, 0] = tolerance;
                    toleranceBuffer[x, h] = tolerance;
                }
            }

        }

        #endregion

        #region Effects

        /// <summary>
        /// Apply a bitmap effect to this RenderBuffer
        /// </summary>
        public void ApplyEffect(BitmapEffect effect)
        {
            ApplyEffect(effect, null);
        }

        /// <summary>
        /// Apply a bitmap effect to this RenderBuffer within the area defined by the effect input.
        /// </summary>
        public void ApplyEffect(BitmapEffect effect, BitmapEffectInput input)
        {
            if (effect == null)
            {
                return;
            }

            // TODO: We should probably draw a tolerance border around this Rect because it could be off by a bit
            Rect bounds = ComputeAreaToApplyEffect(input);
            if (bounds.IsEmpty)
            {
                // NOTE: Don't confuse 'bounds' with BitmapEffectInput.AreaToApplyEffect!
                // An empty area in the Input means to use the entire RenderTarget, and bounds
                //  is the size of the RenderTarget we want to apply the effect to.
                // At this stage, 'Empty' means that there is nothing to apply an effect to.
                return;
            }

            if (effect is DropShadowBitmapEffect)
            {
                ApplyDropShadow(effect as DropShadowBitmapEffect, bounds);
            }
            else
            {
                throw new NotImplementedException("Haven't implemented the " + effect.GetType().Name + " effect yet");
            }
        }

        /// <summary>
        /// Compute the DPI-correct rectangular area that the BitmapEffectInput describes
        /// </summary>
        private Rect ComputeAreaToApplyEffect(BitmapEffectInput input)
        {
            if (input == null || input.AreaToApplyEffect.IsEmpty)
            {
                // Empty area is the same as a null input (use entire RenderTarget).
                // width and height are already in absolute DPI (no conversion necessary)
                return new Rect(0, 0, width, height);
            }
            else
            {
                // 'renderedBounds' is already in absolute DPI (no conversion necessary)
                Rect renderedBounds = RenderedBounds;
                if (renderedBounds == Rect.Empty)
                {
                    // No effect can be applied because no pixels are rendered.
                    return Rect.Empty;
                }

                Rect area;
                if (input.AreaToApplyEffectUnits == BrushMappingMode.RelativeToBoundingBox)
                {
                    // The DPI conversion happens implicitly because renderedBounds is already adjusted.
                    area = MathEx.ScaleRect(input.AreaToApplyEffect, renderedBounds);
                }
                else
                {
                    area = MathEx.ConvertToAbsolutePixels(input.AreaToApplyEffect);

                    // Absolute is defined as relative to the boundingBox so we need to transform (offset)
                    //  its location by the bounding box's location (do not scale by Width/Height)

                    area.X += renderedBounds.X;
                    area.Y += renderedBounds.Y;
                }

                area = Rect.Intersect(area, renderedBounds);
                return MathEx.InflateToIntegerBounds(area);
            }
        }

        private void ApplyDropShadow(DropShadowBitmapEffect effect, Rect effectInput)
        {
            RenderBuffer clone = Clone();
            clone.boundsOverride = effectInput;
            double depth = MathEx.ConvertToAbsolutePixels(effect.ShadowDepth);
            double angle = MathEx.ToRadians(effect.Direction);
            Vector pixelOffset = new Vector(depth * Math.Cos(angle), depth * Math.Sin(-angle));
            Color effectColor = effect.Color;
            effectColor = ColorOperations.ScaleAlpha(effectColor, effect.Opacity);
            double blurRadius = 1.0 + (effect.Softness * 9.0);

            int xEnd = (int)effectInput.Right;
            int yEnd = (int)effectInput.Bottom;

            for (int y = (int)effectInput.Y; y < yEnd; y++)
            {
                for (int x = (int)effectInput.X; x < xEnd; x++)
                {
                    Point point = new Point(x + Const.pixelCenterX, y + Const.pixelCenterY) - pixelOffset;
                    Color? color = clone.GetPixelSample(point, blurRadius);

                    if (color.HasValue)
                    {
                        double opacity = ColorOperations.ByteToDouble(color.Value.A);
                        Color shadow = ColorOperations.ScaleAlpha(effectColor, opacity);
                        shadow = ColorOperations.PreMultiplyColor(shadow);
                        frameBuffer[x, y] = ColorOperations.PreMultipliedAlphaBlend(clone.frameBuffer[x, y], shadow);

                        if (frameBuffer[x, y] != clone.frameBuffer[x, y])
                        {
                            // Transfer the tolerance (silhouette, etc) over too
                            Color tolerance = clone.GetToleranceSample(point, blurRadius);
                            toleranceBuffer[x, y] = ColorOperations.Max(tolerance, clone.toleranceBuffer[x, y]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Apply an opacity mask based on the alpha values of a brush.
        /// </summary>
        public void ApplyOpacityMask(Brush brush)
        {
            if (brush == null)
            {
                return;
            }
            Color[,] colors = TextureGenerator.RenderBrushToColorArray(brush, width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double opacity = ColorOperations.ByteToDouble(colors[x, y].A);
                    frameBuffer[x, y] = ColorOperations.PreMultipliedOpacityScale(frameBuffer[x, y], opacity);
                }
            }
        }

        /// <summary>
        /// Clip the regions specified by the Geometry out of the frame buffer.
        /// </summary>
        public void ApplyClip(Geometry clip)
        {
            if (clip == null)
            {
                return;
            }
            Color[,] clipColors = TextureGenerator.RenderBrushToColorArray(GetClipBrush(clip), width, height);

            // Clipping is anti-aliased.  Need tolerance around clip edges.
            Color[,] tolColors = TextureGenerator.RenderBrushToColorArray(GetClipTolerance(clip), width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double opacity = ColorOperations.ByteToDouble(clipColors[x, y].A);
                    frameBuffer[x, y] = ColorOperations.PreMultipliedOpacityScale(frameBuffer[x, y], opacity);

                    Color tolerance = new Color();
                    tolerance.A = tolerance.R = tolerance.G = tolerance.B = tolColors[x, y].A;
                    toleranceBuffer[x, y] = ColorOperations.Add(toleranceBuffer[x, y], tolerance);
                }
            }
        }

        private Brush GetClipBrush(Geometry clip)
        {
            GeometryDrawing drawing = new GeometryDrawing(Brushes.Black, null, clip);
            DrawingBrush brush = new DrawingBrush(drawing);
            brush.Viewbox = new Rect(0, 0, width, height);
            brush.ViewboxUnits = BrushMappingMode.Absolute;
            return brush;
        }

        private Brush GetClipTolerance(Geometry clip)
        {
            clip = EnsureClosedGeometry(clip);

            // Since we want this pen to always be the same width (RootTwo) in Device Dependent Units,
            //  we convert this value to Device Independent Units before creating the Pen.
            double thickness = MathEx.ConvertToDeviceIndependentPixels(Const.RootTwo);

            GeometryDrawing drawing = new GeometryDrawing(null, new Pen(Brushes.Black, thickness), clip);
            DrawingBrush brush = new DrawingBrush(drawing);
            brush.Viewbox = new Rect(0, 0, width, height);
            brush.ViewboxUnits = BrushMappingMode.Absolute;
            return brush;
        }

        private Geometry EnsureClosedGeometry(Geometry g)
        {
            if (g is PathGeometry)
            {
                PathGeometry result = ((PathGeometry)g).CloneCurrentValue();
                foreach (PathFigure figure in result.Figures)
                {
                    if (!figure.IsClosed)
                    {
                        figure.Segments.Add(new LineSegment(figure.StartPoint, true));
                    }
                }
                g = result;
            }
            return g;
        }

        /// <summary>
        /// Apply a 2D transform to the RenderBuffer
        /// </summary>
        public void ApplyTransform(Transform transform)
        {
            if (transform == null || transform.Value == Matrix.Identity)
            {
                return;
            }
            RenderBuffer clone = Clone();
            Matrix inverse = transform.Value;
            inverse.Invert();
            inverse = MathEx.ConvertToAbsolutePixels(inverse);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Point point = inverse.Transform(new Point(x + Const.pixelCenterX, y + Const.pixelCenterY));
                    Color? color = clone.GetPixel(point);
                    if (color.HasValue)
                    {
                        frameBuffer[x, y] = color.Value;
                        toleranceBuffer[x, y] = clone.GetTolerance(point, frameBuffer[x, y]);
                    }
                    else
                    {
                        // Transformed outside of the RenderBuffer
                        frameBuffer[x, y] = ColorOperations.ColorFromArgb(0, 0, 0, 0);
                        toleranceBuffer[x, y] = RenderTolerance.DefaultColorTolerance;
                    }
                }
            }
        }

        /// <summary>
        /// Examines a render buffer whose background is transparent and adds tolerance
        /// around the edges of the rendered content.
        /// </summary>
        public void AddEdgeDetectionTolerance()
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    SetEdgeDetectionToleranceFor(x, y);
                }
            }
        }

        /// <summary>
        /// Get a Color representing the average color of a sample defined by the
        /// rectangle centered at "point" with a width and height of "radius" * 2.
        /// </summary>
        private Color? GetPixelSample(Point point, double radius)
        {
            // pixelCenter is factored into this point

            Rect sampleBounds = new Rect(point.X - radius, point.Y - radius, radius * 2, radius * 2);
            sampleBounds = MathEx.InflateToIntegerBounds(sampleBounds);
            List<Color?> colors = new List<Color?>();

            // Get pixel centers enclosed by the circle
            for (int y = (int)sampleBounds.Top; y < sampleBounds.Bottom; y++)
            {
                for (int x = (int)sampleBounds.Left; x < sampleBounds.Right; x++)
                {
                    Point center = new Point(x + Const.pixelCenterX, y + Const.pixelCenterY);
                    colors.Add(SafeGetPixel(x, y));
                }
            }
            if (colors.Count == 0)
            {
                return null;
            }
            return MathEx.Average(colors);
        }

        private Color GetToleranceSample(Point point, double radius)
        {
            // pixelCenter is factored into this point

            Rect sampleBounds = new Rect(point.X - radius, point.Y - radius, radius * 2, radius * 2);
            sampleBounds = MathEx.InflateToIntegerBounds(sampleBounds);
            List<Color?> colors = new List<Color?>();

            // Get pixel centers enclosed by the circle
            for (int y = (int)sampleBounds.Top; y < sampleBounds.Bottom; y++)
            {
                for (int x = (int)sampleBounds.Left; x < sampleBounds.Right; x++)
                {
                    Point center = new Point(x + Const.pixelCenterX, y + Const.pixelCenterY);
                    Color? color = SafeGetPixel(x, y);
                    if (color.HasValue)
                    {
                        colors.Add(toleranceBuffer[x, y]);
                    }
                    else
                    {
                        colors.Add(Color.FromArgb(255, 255, 255, 255));
                    }
                }
            }
            Color? result = MathEx.Average(colors);
            if (result.HasValue)
            {
                return result.Value;
            }
            return RenderTolerance.DefaultColorTolerance;
        }

        private void SetEdgeDetectionToleranceFor(int x, int y)
        {
            byte maxAlpha = frameBuffer[x, y].A;
            byte minAlpha = maxAlpha;
            Color?[] surroundingPixels = new Color?[]{
                        SafeGetPixel( x-1, y-1 ), SafeGetPixel( x, y-1 ), SafeGetPixel( x+1, y-1 ),
                        SafeGetPixel( x-1,   y ),                         SafeGetPixel( x+1,   y ),
                        SafeGetPixel( x-1, y+1 ), SafeGetPixel( x, y+1 ), SafeGetPixel( x+1, y+1 ) };

            foreach (Color? c in surroundingPixels)
            {
                if (c.HasValue)
                {
                    maxAlpha = Math.Max(maxAlpha, c.Value.A);
                    minAlpha = Math.Min(minAlpha, c.Value.A);
                }
                else
                {
                    // If the pixel lookup is out of bounds, this is an edge.
                    toleranceBuffer[x, y] = Color.FromArgb(255, 255, 255, 255);
                    return;
                }
            }
            byte diff = (byte)(maxAlpha - minAlpha);
            if (diff != 0)
            {
                toleranceBuffer[x, y] = Color.FromArgb(0, diff, diff, diff);
            }
        }

        #endregion

        #region Per-pixel operations

        /// <summary>
        /// Adds the value in tolerance to the tolerance buffer at [x,y]
        /// </summary>
        /// <param name="x">x index</param>
        /// <param name="y">y index</param>
        /// <param name="tolerance">New tolerance value that will be added</param>
        public void AddToTolerance(int x, int y, Color tolerance)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                toleranceBuffer[x, y] = ColorOperations.Add(tolerance, toleranceBuffer[x, y]);
            }
        }

        private bool IsPixelRendered(int x, int y)
        {
            // We clear z-buffer to 1.0 ( far plane ), this is zBufferClearValue.
            // For silouhette tolerance in image space, we need to tag pixels that need updating.  We do
            // that by using z-buffer values that would be outside the frustrum.
            return (zBuffer[x, y] < zBufferClearValue || frameBuffer[x, y] != frameBufferClearValue);
        }

        /// <summary>
        /// Writes a pixel to the buffer according to depth-test for single pass rendering.
        /// </summary>
        /// <param name="x">x index</param>
        /// <param name="y">y index</param>
        /// <param name="z">Depth buffer value</param>
        /// <param name="premultColor">Premultiplied Frame buffer value</param>
        /// <param name="premultTolerance">Premultiplied Tolerance buffer value</param>
        public void SetPixel(int x, int y, float z, Color premultColor, Color premultTolerance)
        {
            if (x >= 0 && x < width && y >= 0 && y < height && z >= 0 && z <= 1.0f)
            {
                bool isZFightingPossible = IsPixelRendered(x, y);
                bool drawPixel = DepthTest(z, zBuffer[x, y]);
                if (drawPixel)
                {
                    // Write to frame buffer according to alpha value of pixel
                    frameBuffer[x, y] = ColorOperations.PreMultipliedAlphaBlend(
                            premultColor,
                            frameBuffer[x, y]);

                    // We have a pre-multiplied tolerance value
                    toleranceBuffer[x, y] = ColorOperations.PreMultipliedToleranceBlend(
                            premultTolerance,
                            toleranceBuffer[x, y],
                            premultColor.A);
                }

                // See if we were within tolerance of another test result
                if (isZFightingPossible)
                {
                    if (DepthTestWithinTolerance(z, zBuffer[x, y], RenderTolerance.ZBufferTolerance))
                    {
                        // If so, ignore this pixel, since we can't be sure if it's right
                        toleranceBuffer[x, y] = Color.FromArgb(0, 255, 255, 255);
                    }
                }
                else if (z < RenderTolerance.NearPlaneTolerance || 1.0 - RenderTolerance.FarPlaneTolerance < z)
                {
                    // If we're right on the near/far clipping plane, we can't be sure if this pixel is right.

                    // Note that the tolerance at the near plane will be considerably more than that of the
                    //  far plane if we are using a perspective camera to view this scene.
                    toleranceBuffer[x, y] = Color.FromArgb(0, 255, 255, 255);
                }

                if (drawPixel && writeToZBuffer)
                {
                    zBuffer[x, y] = z;
                }
            }
        }

        private bool DepthTest(float val1, float val2)
        {
            switch (depthTest)
            {
                case DepthTestFunction.Never: return false;
                case DepthTestFunction.LessThan: return val1 < val2;
                case DepthTestFunction.LessThanOrEqualTo: return val1 <= val2;
                case DepthTestFunction.EqualTo: return val1 == val2;
                case DepthTestFunction.GreaterThanOrEqualTo: return val1 >= val2;
                case DepthTestFunction.GreaterThan: return val1 > val2;
                case DepthTestFunction.Always: return true;
            }

            throw new ApplicationException("Invalid Depth Test function.");
        }

        private bool DepthTestWithinTolerance(float val1, float val2, double tol)
        {
            switch (depthTest)
            {
                // Never and Always are precise, they can ignore tolerance
                case DepthTestFunction.Never: return false;
                case DepthTestFunction.Always: return false;

                // All other tests can flip if within numerical tolerance
                case DepthTestFunction.LessThan:
                case DepthTestFunction.LessThanOrEqualTo:
                case DepthTestFunction.EqualTo:
                case DepthTestFunction.GreaterThanOrEqualTo:
                case DepthTestFunction.GreaterThan:
                    return Math.Abs(val1 - val2) < tol;
            }

            throw new ApplicationException("Invalid Depth Test function.");
        }

        /// <summary>
        /// Get the pixel from the FrameBuffer at the specified Point.
        /// This method uses Bilinear interpolation to determine the Color value.
        /// If the Point is outside the bounds of the FrameBuffer, return null.
        /// </summary>
        private Color? GetPixel(Point point)
        {
            // point is somewhere in a pixel:
            // pixel center is at ( 0.5, 0.5 ) and is already factored into this point.
            // we need to find the weights of the surrounding pixel centers and interpolate to find the value
            //
            //  x   x'      x"
            //  +-------+-------+ y
            //  |       |       |
            //  |   1.......2   | y'
            //  |   : o |   :   |   // o is at ( point.X, point.Y )
            //  +---:---+---:---+
            //  |   :   |   :   |
            //  |   3.......4   | y"
            //  |       |       |
            //  +-------+-------+
            //
            // x  = floor( point.X - 0.5 )
            // x' = x + 0.5
            // x" = x + 1.5
            // y  = floor( point.Y - 0.5 )
            // y' = y + 0.5
            // y" = y + 1.5
            // leftWeight   = 1 - (point.X - x')
            // rightWeight  =     (point.X - x')
            // topWeight    = 1 - (point.Y - y')
            // bottomWeight =     (point.Y - y')

            //
            //  +-------+-------+
            //  |       |       |
            //  |   1.5.....2   | // 5 is the interpolated color at (point.X,y')
            //  |   : o |   :   |
            //  +---:-|-+---:---+
            //  |   : | |   :   |
            //  |   3.6.....4   | // 6 is the interpolated color at (point.X,y")
            //  |       |       |
            //  +-------+-------+
            // p5 = p1*leftWeight + p2*rightWeight
            // p6 = p3*leftWeight + p4*rightWeight
            // o = p5*topWeight + p6*bottomWeight

            int x = (int)Math.Floor(point.X - Const.pixelCenterX);
            int y = (int)Math.Floor(point.Y - Const.pixelCenterY);
            double xPrime = x + Const.pixelCenterX;
            double yPrime = y + Const.pixelCenterY;
            double rightWeight = (point.X - xPrime);
            double bottomWeight = (point.Y - yPrime);

            Color? p1 = SafeGetPixel(x, y);
            Color? p2 = SafeGetPixel(x + 1, y);
            Color? p3 = SafeGetPixel(x, y + 1);
            Color? p4 = SafeGetPixel(x + 1, y + 1);

            // Never blend with something outside the framebuffer

            Color? p5 = ColorOperations.Blend(p2, p1, rightWeight);
            Color? p6 = ColorOperations.Blend(p4, p3, rightWeight);

            return ColorOperations.Blend(p6, p5, bottomWeight);
        }

        /// <summary>
        /// Return a Color value from the FrameBuffer at (x,y).
        /// If (x,y) is outside the bounds of the FrameBuffer, return null.
        /// </summary>
        private Color? SafeGetPixel(int x, int y)
        {
            if (boundsOverride.HasValue)
            {
                Rect bounds = boundsOverride.Value;
                if (x < bounds.Left || bounds.Right <= x || y < bounds.Top || bounds.Bottom <= y)
                {
                    return null;
                }
                return frameBuffer[x, y];
            }
            if (x < 0 || width <= x || y < 0 || height <= y)
            {
                return null;
            }
            return frameBuffer[x, y];
        }

        private Color GetTolerance(Point point, Color expected)
        {
            // pixelCenter is factored into this point

            int x = (int)Math.Floor(point.X - Const.pixelCenterX);
            int y = (int)Math.Floor(point.Y - Const.pixelCenterY);

            Color missingPixelTolerance = ColorOperations.ColorFromArgb(0, 0, 0, 0);
            Color lookupTolerance = ColorOperations.ColorFromArgb(0, 0, 0, 0);
            for (int j = y; j < y + 2; j++)
            {
                for (int i = x; i < x + 2; i++)
                {
                    Color? current = SafeGetPixel(i, j);
                    if (current.HasValue)
                    {
                        // Keep the max of the diff tolerance and the existing tolerance
                        Color diff = ColorOperations.AbsoluteDifference(current.Value, expected);
                        lookupTolerance = ColorOperations.Max(lookupTolerance, diff);
                        lookupTolerance = ColorOperations.Max(lookupTolerance, toleranceBuffer[i, j]);
                    }
                    else
                    {
                        // increase tolerance by 25% since this pixel's value is unknown
                        missingPixelTolerance = ColorOperations.Add(missingPixelTolerance, ColorOperations.ColorFromArgb(.25, .25, .25, .25));
                    }
                }
            }
            return ColorOperations.Add(lookupTolerance, missingPixelTolerance);
        }

        #endregion

        #region Buffer-to-Buffer operations

        /// <summary>
        /// Paint another RenderBuffer over this one
        /// </summary>
        public void BitBlockTransfer(RenderBuffer r, int offsetX, int offsetY)
        {
            // Guard against x & y overflow/underflow
            int width = Math.Min(Width, offsetX + r.Width);
            int height = Math.Min(Height, offsetY + r.Height);
            int startX = Math.Max(offsetX, 0);
            int startY = Math.Max(offsetY, 0);

            // save depth test
            DepthTestFunction oldDepthTest = depthTest;
            depthTest = DepthTestFunction.Always;
            // Render
            for (int y = startY; y < height; y++)
            {
                for (int x = startX; x < width; x++)
                {
                    frameBuffer[x, y] = r.frameBuffer[x - offsetX, y - offsetY];
                    toleranceBuffer[x, y] = r.toleranceBuffer[x - offsetX, y - offsetY];
                    zBuffer[x, y] = r.zBuffer[x - offsetX, y - offsetY];
                }
            }
            // restore depth test
            depthTest = oldDepthTest;
        }

        #endregion

        #region Properties

        /// <summary/>
        public Color[,] FrameBuffer { get { return frameBuffer; } }

        /// <summary/>
        public Color[,] ToleranceBuffer { get { return toleranceBuffer; } }

        /// <summary/>
        public float[,] ZBuffer { get { return zBuffer; } }

        /// <summary/>
        public int Width { get { return width; } }

        /// <summary/>
        public int Height { get { return height; } }

        /// <summary/>
        public float ZBufferClearValue { get { return zBufferClearValue; } }

        /// <summary/>
        public DepthTestFunction DepthTestFunction { get { return depthTest; } set { depthTest = value; } }

        /// <summary/>
        public bool WriteToZBuffer { get { return writeToZBuffer; } set { writeToZBuffer = value; } }

        /// <summary>
        /// The bounds of this buffer's rendered content.
        /// This is an approximation of what we expect WPF's rendered bounds to be
        ///  and should be used with that in mind.
        /// </summary>
        internal Rect RenderedBounds
        {
            get
            {
                return CalculateRenderedBounds(true);
            }
        }

        /// <summary>
        /// The bounds of this buffer's rendered content.
        /// Actual bounds will never be smaller than this value.
        /// </summary>
        public Rect TightRenderedBounds
        {
            get
            {
                return CalculateRenderedBounds(false);
            }
        }

        private Rect CalculateRenderedBounds(bool addBorderForIgnoredTolerance)
        {
            int left = width;
            int top = height;
            int right = -1;
            int bottom = -1;
            bool empty = true;
            bool toleranceWasIgnored = false;
            Color white = Colors.White;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (IsPixelRendered(x, y))
                    {
                        if (toleranceBuffer[x, y] == white)
                        {
                            toleranceWasIgnored = true;
                            continue;
                        }

                        // Only add this pixel to render bounds if we have some inkling that it should be rendered.
                        // We want the bounds as tight as possible, and it's okay if our bounds are slightly smaller
                        //  than the rendered scene.
                        left = Math.Min(left, x);
                        top = Math.Min(top, y);
                        right = Math.Max(right, x + 1);
                        bottom = Math.Max(bottom, y + 1);
                        empty = false;
                    }
                }
            }

            if (empty)
            {
                return Rect.Empty;
            }
            if (addBorderForIgnoredTolerance && toleranceWasIgnored)
            {
                // Ignoring tolerance shrinks the actual bounds by about 1 pixel on each side.
                left = Math.Max(0, left - 1);
                top = Math.Max(0, top - 1);
                right = Math.Min(width, right + 1);
                bottom = Math.Min(height, bottom + 1);
            }

            return new Rect(left, top, right - left, bottom - top);
        }

        #endregion

        private Color[,] frameBuffer;
        private Color[,] toleranceBuffer;
        private float[,] zBuffer;
        /// <summary/>
        protected DepthTestFunction depthTest;

        /// <summary>
        /// This triggers the actual z-writing on a render pass
        /// </summary> 
        private bool writeToZBuffer;

        private int width;
        private int height;
        private Rect? boundsOverride;

        private static readonly float zBufferClearValue = 1.0f;
        private static readonly Color frameBufferClearValue = Color.FromArgb(0, 0, 0, 0);
        private static readonly Color toleranceBufferClearValue = Color.FromArgb(0, 0, 0, 0);

    }
}
