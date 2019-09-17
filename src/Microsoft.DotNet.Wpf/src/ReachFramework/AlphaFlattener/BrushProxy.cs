// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;              // for ArrayList
using System.Collections.Generic;
using System.Diagnostics;

#if DEBUG_RASTERIZATION
using System.IO;
#endif

using System.Windows;                  // for Rect                        WindowsBase.dll
using System.Windows.Media;            // for Geometry, Brush, ImageData. PresentationCore.dll
using System.Windows.Media.Imaging;
using System.Security;

using System.Windows.Xps.Serialization;
using MS.Utility;

namespace Microsoft.Internal.AlphaFlattener
{
    /// <summary>
    ///
    /// </summary>
    internal static class Configuration
    {
        /// <summary>
        /// Treat all alpha as opaque
        /// </summary>
        public static bool ForceAlphaOpaque;    // = false;

        /// <summary>
        /// Blend all alpha with white background
        /// </summary>
        public static bool BlendAlphaWithWhite; // = false;

        /// <summary>
        /// Controls how one cycle of gradient brush into N rings/slides of solid color
        /// </summary>
        public static double GradientDecompositionDensity = 1;

#if DEBUG
        /// <summary>
        /// Print out more trace information for checked build
        /// </summary>
        public static int Verbose; // = 0;

        /// <summary>
        /// Serializes flattened primitives to XAML as debugging information.
        /// </summary>
        public static bool SerializePrimitives = false;
#endif

        /// <summary>
        /// Displays debugging text at the top in GDI page output.
        /// </summary>
        public static bool DisplayPageDebugHeader = true;

        /// <summary>
        /// Maximum number of brushes to decompose before choosing rasterization
        /// </summary>
        public static int DecompositionDepth = 3;

        /// <summary>
        /// Maximum number of transparency layers to consider before ignore alpha flattening
        /// </summary>
        public static int MaximumTransparencyLayer = 12;

        /// <summary>
        /// Resolution for rasterization when brushes are too complicated
        /// </summary>
        public static int RasterizationDPI = 150;

        /// <summary>
        /// Output file to be passed to StartDoc
        /// </summary>
        public static string OutputFile; // = null;

     // public static bool ForceGrayScale    ; //= false;
     // public static bool AlwaysUnfoldDB    ; //= false;
     // public static bool PreserveText      ; //= false;
     // public static bool SupportAlphaBlend ; //= false;

        /// <summary>
        /// Maximum number of gradient steps allowed in gradient decomposition
        /// </summary>
        public const int MaxGradientSteps = 4096;

        public static bool SetValue(string key, object val)
        {
            switch (key)
            {
#if DEBUG
                case "Verbose":
                    Verbose = (int) val;
                    return true;

                case "SerializePrimitives":
                    SerializePrimitives = (bool)val;
                    return true;
#endif

                case "DisplayPageDebugHeader":
                    DisplayPageDebugHeader = (bool)val;
                    return true;

                case "ForceAlphaOpaque":
                    ForceAlphaOpaque = (bool) val;
                    return true;

                case "BlendAlphaWithWhite":
                    BlendAlphaWithWhite = (bool) val;
                    return true;

                case "GradientDecompositionDensity":
                    GradientDecompositionDensity = (double) val;
                    return true;

                case "MaximumTransparencyLayer":
                    MaximumTransparencyLayer = (int) val;
                    return true;

                case "RasterizationDPI":
                    RasterizationDPI = (int)val;
                    return true;

                case "OutputFile":
                    OutputFile = (string) val;
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Estimate the cost of rasterizing an area
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        static internal double RasterizationCost(double width, double height)
        {
            return  1024 + width / 96 * RasterizationDPI *
                           height / 96 * RasterizationDPI *
                            3;
        }

        static internal double RasterizationCost(double size)
        {
            return 1024 + size / 96 * RasterizationDPI * 3;
        }
    };

    internal class PenProxy
    {
        #region Constructors

        private PenProxy()
        {
        }

        private PenProxy(Pen pen, BrushProxy brush)
        {
            Debug.Assert(pen != null, "pen expected");
            Debug.Assert(brush != null, "brush expected");

            _pen   = pen;
            _brush = brush;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets Avalon Pen represented by this PenProxy.
        /// </summary>
        /// <param name="ignoreBrushProxy">Ignores internal BrushProxy</param>
        /// <returns></returns>
        public Pen GetPen(bool ignoreBrushProxy)
        {
            if (ignoreBrushProxy)
            {
                return _pen;
            }
            else
            {
                Debug.Assert(_brush.BrushList == null, "Simple brush expected");

                Pen p = _pen.CloneCurrentValue();

                p.Brush = _brush.GetRealBrush();

                return p;
            }
        }

        public bool IsOpaque()
        {
            return _brush.IsOpaque();
        }

        public bool IsTransparent()
        {
            return _brush.IsTransparent();
        }

        #endregion

        #region Public Properties

        public BrushProxy StrokeBrush
        {
            get
            {
                return _brush;
            }
            set
            {
                _brush = value;
            }
        }

        #endregion

        #region Public Methods

        public void Scale(double ratio)
        {
            if (! Utility.AreClose(ratio, 1.0))
            {
                _pen = _pen.CloneCurrentValue();
                _pen.Thickness *= ratio;
            }
        }

        public void PushOpacity(double opacity, BrushProxy opacityMask)
        {
            if ((_brush.Brush != null) && (BrushProxy.IsOpaqueWhite(_brush.Brush) || BrushProxy.IsOpaqueBlack(_brush.Brush)))
            {
                _brush = _brush.Clone();
            }

            _brush = _brush.PushOpacity(opacity, opacityMask);
        }

        public PenProxy Clone()
        {
            PenProxy pen = new PenProxy();

            pen._pen   = this._pen;
            pen._brush = this._brush;

            return pen;
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Creates a PenProxy wrapper.
        /// </summary>
        /// <param name="pen"></param>
        /// <param name="bounds"></param>
        /// <returns>May return null if Brush is an empty brush</returns>
        public static PenProxy CreatePen(Pen pen, Rect bounds)
        {
            Debug.Assert(pen != null, "pen expected");
            Debug.Assert(pen.Brush != null, "pen expected to have a brush");

            if (IsNull(pen))
            {
                return null;
            }

            BrushProxy brush = BrushProxy.CreateBrush(pen.Brush, bounds);

            if (brush == null)
            {
                return null;
            }
            else
            {
                return new PenProxy(pen, brush);
            }
        }

        /// <summary>
        /// Creates a PenProxy wrapper around a user-provided Pen.
        /// </summary>
        /// <param name="pen"></param>
        /// <param name="bounds"></param>
        /// <param name="brushToWorldTransformHint">Transformation hint to help determine rasterization bitmap size if needed</param>
        /// <param name="treeWalkProgress">Used to detect visual tree cycles caused by VisualBrush</param>
        /// <returns>May return null if Brush is an empty brush</returns>
        /// <remarks>
        /// Attempts to simplify Pen.Brush via BrushProxy.ReduceBrush.
        /// </remarks>
        public static PenProxy CreateUserPen(Pen pen, Rect bounds, Matrix brushToWorldTransformHint, TreeWalkProgress treeWalkProgress)
        {
            Debug.Assert(pen != null, "pen expected");
            Debug.Assert(pen.Brush != null, "pen expected to have a brush");

            if (IsNull(pen))
            {
                return null;
            }

            BrushProxy brush = BrushProxy.CreateUserBrush(pen.Brush, bounds, brushToWorldTransformHint, treeWalkProgress);

            if (brush == null)
            {
                return null;
            }
            else
            {
                return new PenProxy(pen, brush);
            }
        }

        /// <summary>
        /// Determines if a pen is equivalent to a null pen.
        /// </summary>
        /// <param name="pen"></param>
        /// <remarks>
        /// A pen with a transparent brush is not considered empty unless Thickness is 0, since
        /// the non-zero thickness will shrink the geometry fill despite being invisible.
        /// </remarks>
        public static bool IsNull(Pen pen)
        {
            if (pen == null || pen.Thickness == 0)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Private Fields

        private BrushProxy _brush; // Brush within the pen, may change
        private Pen _pen;

        #endregion
    }

    internal class BrushProxy
    {
        #region Constructors

        public BrushProxy()
        {
            _brushList = new ArrayList();
            _opacity = 1.0;
        }

        /// <summary>
        /// Private constructor called by CreateBrush.
        /// </summary>
        /// <param name="brush"></param>
        private BrushProxy(Brush brush)
        {
            _brush   = brush;
            _opacity = Utility.NormalizeOpacity(brush.Opacity);
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            string str = null;

            if (_opacityOnly)
            {
                str = "^";
            }

            if (_brush != null)
            {
                str = str + _brush.GetType();
            }
            else if (_brushList != null)
            {
                str = str + "BrushList[" + _brushList.Count + "]";
            }

            if (_opacityMask != null)
            {
                str = str + "^" + _opacityMask.ToString();
            }

            return str;
        }

        /// <summary>
        /// Returns false if the brush has become empty.
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public bool MakeBrushAbsolute(Rect bounds)
        {
            bool copied = false;

            _bounds = bounds;

            if (! (_brush is SolidColorBrush) && ! Utility.IsIdentity(_brush.RelativeTransform))
            {
                _brush = _brush.CloneCurrentValue();
                copied = true;

                Matrix mat = Utility.MergeTransform(_brush.Transform, _brush.RelativeTransform, bounds);

                _brush.Transform         = new MatrixTransform(mat);
                _brush.RelativeTransform = Transform.Identity;
            }

            // If brush is relative to a bounding box, make it absolute so that it
            // can be used for drawing primitives with other bounding boxes.

            if (_brush is TileBrush)
            {
                TileBrush tb = _brush as TileBrush;

                if (tb.ViewportUnits == BrushMappingMode.RelativeToBoundingBox)
                {
                    if (!copied)
                    {
                        tb = tb.CloneCurrentValue();
                        copied = true;
                    }

                    Rect viewport = Utility.GetTileAbsoluteViewport(tb, bounds);

                    if (!Utility.IsRenderVisible(viewport))
                    {
                        // brush not visible anymore with new viewport
                        return false;
                    }

                    tb.ViewportUnits = BrushMappingMode.Absolute;
                    tb.Viewport = viewport;

                    _brush = tb;
                }

                if (tb.ViewboxUnits == BrushMappingMode.RelativeToBoundingBox)
                {
                    // Fix bug 1463955: Cloning DrawingBrush may cause its Drawing's bounds to become not visible.
                    // Therefore clone before getting absolute viewbox, which'll return Empty upon invisible viewbox.
                    if (!copied)
                    {
                        tb = tb.CloneCurrentValue();
                        copied = true;
                    }

                    Rect viewbox = Utility.GetTileAbsoluteViewbox(tb);

                    if (!Utility.IsValidViewbox(viewbox, tb.Stretch != Stretch.None))
                    {
                        // brush not visible anymore with new viewbox
                        return false;
                    }

                    tb.ViewboxUnits = BrushMappingMode.Absolute;
                    tb.Viewbox = viewbox;

                    _brush = tb;
                }
            }

            if (_brush is LinearGradientBrush)
            {
                LinearGradientBrush lb = _brush as LinearGradientBrush;

                if (lb.MappingMode == BrushMappingMode.RelativeToBoundingBox)
                {
                    if (!copied)
                    {
                        lb = lb.CloneCurrentValue();
                        copied = true;
                    }

                    lb.StartPoint = Utility.MapPoint(bounds, lb.StartPoint);
                    lb.EndPoint = Utility.MapPoint(bounds, lb.EndPoint);

                    lb.MappingMode = BrushMappingMode.Absolute;

                    _brush = lb;
                }
            }

            if (_brush is RadialGradientBrush)
            {
                RadialGradientBrush rb = _brush as RadialGradientBrush;

                if (rb.MappingMode == BrushMappingMode.RelativeToBoundingBox)
                {
                    if (!copied)
                    {
                        rb = rb.CloneCurrentValue();
                        copied = true;
                    }

                    rb.Center = Utility.MapPoint(bounds, rb.Center);
                    rb.GradientOrigin = Utility.MapPoint(bounds, rb.GradientOrigin);

                    rb.RadiusX = Math.Abs(rb.RadiusX * bounds.Width);
                    rb.RadiusY = Math.Abs(rb.RadiusY * bounds.Height);

                    rb.MappingMode = BrushMappingMode.Absolute;

                    _brush = rb;
                }
            }

            return true;
        }

        /// <summary>
        /// Add current brush to an ArrayList of BrushProxy
        /// </summary>
        /// <param name="bp"></param>
        public void AddTo(BrushProxy bp)
        {
            if (_brush != null)
            {
                ArrayList list = bp._brushList;

                if (list.Count == 0)
                {
                    bp._opacityOnly = _opacityOnly;
                }
                else
                {
                    Debug.Assert(bp._opacityOnly == _opacityOnly, "Brush and OpacityMask can't mix in a single list");
                }

                list.Add(this);
            }
            else
            {
                foreach (BrushProxy b in _brushList)
                {
                    b.AddTo(bp);
                }
            }
        }

        public BrushProxy Clone()
        {
            return MemberwiseClone() as BrushProxy;
        }

        public BrushProxy PushOpacity(double opacity, BrushProxy opacityMask)
        {
            _opacity *= Utility.NormalizeOpacity(opacity);

            if (opacityMask != null)
            {
                _opacityMask = BrushProxy.BlendBrush(_opacityMask, opacityMask);
            }

            if ((_opacityMask != null) && (_brush != null))
            {
                BrushProxy om = _opacityMask;

                _opacityMask = null;

                // Try to blend OpacityMask into brush
                BrushProxy result = this.BlendBrush(om);

                _opacityMask = om;

                if (result != null)
                {
                    return result;
                }
            }

            return this;
        }

        /// <summary>
        /// Check if a brush is opaque
        /// </summary>
        /// <returns>True if brush is totally opaque (opacity==1)</returns>
        public bool IsOpaque()
        {
            if ((_opacityMask != null) && !_opacityMask.IsOpaque())
            {
                return false;
            }

            if (!Utility.IsOpaque(_opacity))
            {
                return false;
            }

            if (_brush is SolidColorBrush)
            {
                SolidColorBrush y = _brush as SolidColorBrush;

                return Utility.IsOpaque(y.Color.ScA);
            }

            if (_brush is GradientBrush)
            {
                GradientBrush y = _brush as GradientBrush;

                foreach (GradientStop gs in y.GradientStops)
                {
                    if (!Utility.IsOpaque(gs.Color.ScA))
                    {
                        return false;
                    }
                }

                return true;
            }

            if (_brush is TileBrush)
            {
                TileBrush tb = _brush as TileBrush;

                //
                // A TileBrush that does not completely cover a region may be regarded as
                // effectively non-opaque, since underlying region may show through.
                //
                if (!IsTileCompleteCover(tb))
                {
                    return false;
                }

                //
                // TileBrush may still completely cover target region, and so it
                // may still be completely opaque. Check other TileBrush cases...
                //
            }

            if (_brush is ImageBrush)
            {
                ImageBrush ib = _brush as ImageBrush;

                if (_image == null)
                {
                    _image = new ImageProxy((BitmapSource)ib.ImageSource);
                }

                return _image.IsOpaque();
            }

            if (_brush is DrawingBrush)
            {
                DrawingBrush db = _brush as DrawingBrush;

                if (db.Drawing == null)
                {
                    return false;
                }

                Rect vb = db.Viewbox;

                Debug.Assert(Utility.IsRenderVisible(vb), "TileBrush.Viewbox area must be positive");

                return IsDrawingOpaque(GetDrawingPrimitive(), new RectangleGeometry(vb), Matrix.Identity);
            }

            if (_brush != null)
            {
                Debug.Assert(false, "IsOpaque(" + _brush.GetType() + ") not handled");
            }

            if ((_brushList != null) && (_brushList.Count != 0))
            {
                // Check the first brush
                return (_brushList[0] as BrushProxy).IsOpaque();
            }

            return false;
        }

        /// <summary>
        /// Check if a brush is totally transparent
        /// </summary>
        /// <returns></returns>
        public bool IsTransparent()
        {
            if (_brush is SolidColorBrush)
            {
                SolidColorBrush y = _brush as SolidColorBrush;

                double opacity = _opacity * Utility.NormalizeOpacity(y.Color.ScA);

                return Utility.IsTransparent(opacity);
            }

            if (_brush is GradientBrush)
            {
                GradientBrush y = _brush as GradientBrush;

                foreach (GradientStop gs in y.GradientStops)
                {
                    double opacity = _opacity * Utility.NormalizeOpacity(gs.Color.ScA);

                    if (!Utility.IsTransparent(opacity))
                    {
                        return false;
                    }
                }

                return true;
            }

            if (_brush is DrawingBrush)
            {
                if (Utility.IsTransparent(_opacity))
                {
                    return true;
                }

                DrawingBrush db = _brush as DrawingBrush;

                if (db.Drawing == null)
                {
                    return true;
                }

                Rect vb = db.Viewbox;

                Debug.Assert(Utility.IsRenderVisible(vb), "TileBrush.Viewbox must be visible");

                // Fix bug 1505766: Ensure primitive geometric comparisons are done in world space,
                // otherwise accuracy issues arise if geometries are too small.
                Matrix viewboxToViewportTransformHint = Utility.CreateViewboxToViewportTransform(db);

                // viewbox geometry must have drawingToWorldTransformHint applied
                Geometry viewboxGeometry = new RectangleGeometry(vb, 0, 0, new MatrixTransform(viewboxToViewportTransformHint));

                return IsDrawingTransparent(GetDrawingPrimitive(), viewboxGeometry, viewboxToViewportTransformHint);
            }

            if (_brush is ImageBrush)
            {
                if (Utility.IsTransparent(_opacity))
                {
                    return true;
                }

                ImageBrush ib = _brush as ImageBrush;

                if (ib.ImageSource == null)
                {
                    return true;
                }

                if (_image == null)
                {
                    _image = new ImageProxy((BitmapSource)ib.ImageSource);
                }

                return _image.IsTransparent();
            }

            if (_brush != null)
            {
                Debug.Assert(false, "IsTransparent not handled " + _brush.GetType());
            }

            return false;
        }

        public void ApplyTransform(Matrix trans)
        {
            if (!trans.IsIdentity)
            {
                if (!_bounds.IsEmpty)
                {
                    _bounds.Transform(trans);
                }

                if (_brushList == null)
                {
                    if (!(_brush is SolidColorBrush))
                    {
                        _brush = _brush.CloneCurrentValue();

                        Matrix mat = Matrix.Identity;

                        if (_brush.Transform != null)
                        {
                            mat = _brush.Transform.Value;
                        }

                        mat.Append(trans);

                        _brush.Transform = new MatrixTransform(mat);
                    }
                }
                else
                {
                    foreach (BrushProxy brush in _brushList)
                    {
                        brush.ApplyTransform(trans);
                    }
                }

                if (_opacityMask != null)
                {
                    _opacityMask.ApplyTransform(trans);
                }
            }
        }

        public BrushProxy ApplyTransformCopy(Matrix trans)
        {
            BrushProxy result = this;

            if (!trans.IsIdentity)
            {
                result = result.Clone();
                result.ApplyTransform(trans);
            }

            return result;
        }

        /// <summary>
        /// Calculate the blended brush of two brushes, the brush which can achieve the same
        /// result as drawing two brushes seperately
        /// </summary>
        /// <param name="brushB"></param>
        /// <returns></returns>
        public BrushProxy BlendBrush(BrushProxy brushB)
        {
            if (brushB.IsOpaque())
            {
                if (brushB._opacityOnly)
                {
                    // Ignore opaque OpacityMask
                    return this;
                }
                else if (!OpacityOnly)
                {
                    // If the second brush is opaque, ignore the first one
                    return brushB;
                }
            }

            // If there is no OpacitMask, blend two brushes when possible
            if ((this._opacityMask == null) && (brushB._opacityMask == null))
            {
                SolidColorBrush sA = _brush as SolidColorBrush;

                if (sA != null)
                {
                    return BlendColorWithBrush(_opacityOnly, Utility.Scale(sA.Color, _opacity), brushB, false);
                }

                SolidColorBrush sB = brushB.Brush as SolidColorBrush;

                if (sB != null)
                {
                    return BlendColorWithBrush(brushB._opacityOnly, Utility.Scale(sB.Color, brushB._opacity), this, true);
                }

                // Blend ImageBrush with compatible brush
                if (_brush is ImageBrush)
                {
                    BrushProxy bp = BlendImageBrush(brushB, true);

                    if (bp != null)
                    {
                        return bp;
                    }
                }

                // Blend ImageBrush with compatible brush
                if (brushB.Brush is ImageBrush)
                {
                    BrushProxy bp = brushB.BlendImageBrush(this, false);

                    if (bp != null)
                    {
                        return bp;
                    }
                }

                // Blend compatible LinearGradientBrushes
                BrushProxy p = BlendLinearGradientBrush(brushB);

                if (p != null)
                {
                    return p;
                }

                // Blend compatible RadialGradientBrushes
                p = BlendRadialGradientBrush(brushB);

                if (p != null)
                {
                    return p;
                }
            }

            // Abort if only one of them is an OpacityMask
            if (this._opacityOnly ^ brushB._opacityOnly)
            {
                return null;
            }

            // Construct a list of brushes
            BrushProxy rslt = new BrushProxy();

            this.AddTo(rslt);
            brushB.AddTo(rslt);

            return rslt;
        }

        public BitmapSource CreateBrushImage_ID(Matrix mat, int width, int height)
        {
            Toolbox.EmitEvent(EventTrace.Event.WClientDRXRasterStart);

            RenderTargetBitmap brushImage = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

            if (this.BrushList != null)
            {
                foreach (BrushProxy b in this.BrushList)
                {
                    brushImage.Render(new FillVisual(b, mat, width, height));
                }
            }
            else if (Brush != null)
            {
                brushImage.Render(new FillVisual(this, mat, width, height));
            }

            Toolbox.EmitEvent(EventTrace.Event.WClientDRXRasterEnd);

            return brushImage;
        }

#if DEBUG_RASTERIZATION
        static int s_seq = 0;
#endif

        public Byte[] CreateBrushImage(Matrix mat, int width, int height)
        {
            BitmapSource brushImage = CreateBrushImage_ID(mat, width, height);

#if DEBUG_RASTERIZATION
            s_seq ++;

            string filename = "file" + s_seq + ".png";

            BitmapEncoder encoder = new BitmapEncoderPng();

            encoder.Frames.Add(BitmapFrame.Create(brushImage));

            Stream imageStreamDest = new System.IO.FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

            encoder.Save(imageStreamDest);
#endif

            int stride = width * 4;

            Byte[] brushPixels = new Byte[stride * height];
            FormatConvertedBitmap converter = new FormatConvertedBitmap();
            converter.BeginInit();
            converter.Source = brushImage;
            converter.DestinationFormat = PixelFormats.Pbgra32;
            converter.EndInit();

            converter.CriticalCopyPixels(new Int32Rect(0, 0, width, height), brushPixels, stride, 0);

            return brushPixels;
        }

        /// <summary>
        /// Convert Color + TileBrush + Color into a self-contained DrawingBrush for drawing with Avalon
        /// </summary>
        /// <returns></returns>
        public Brush GetRealBrush()
        {
            // do self-contained brush update
            UpdateRealBrush(true);

            return _brush;
        }

        /// <summary>
        /// Updates the Avalon brush, possibly making it self-contained.
        /// </summary>
        /// <remarks>
        /// Self-contained updates generate a Brush that can be used by itself during rendering.
        /// Otherwise properties of BrushProxy are needed to properly render, and thus the update
        /// will only be useful for Primitive.OnRender.
        /// </remarks>
        public void UpdateRealBrush(bool selfContained)
        {
            double oldOpacity = _opacity;

            if (!selfContained)
            {
                // we can keep opacity outside in BrushProxy to avoid rebuilding Brush.
                // Primitive.OnRender will push opacity for us.
                _opacity = 1.0;
            }

            if (
                _beforeDrawing.A != 0 ||            // merge before/after brush color into brush
                _afterDrawing.A != 0 ||
                _drawingBrushChanged ||             // drawing Primitive has changed
                (_brushList != null && _brush == null))   // combine brushlist into one brush
            {
                _brush = BuildBrush();

                // reset properties that have been merged into brush
                _beforeDrawing = Colors.Transparent;
                _afterDrawing = Colors.Transparent;
                _opacity = 1.0;

                // _drawing needs to be rebuilt to reflect new _brush
                _drawing = null;
            }
            else if (!Utility.IsOpaque(_opacity))
            {
                // push opacity into brush without rebuilding the brush
                if (_opacity != Utility.GetOpacity(_brush))
                {
                    _brush = _brush.CloneCurrentValue();

                    _brush.Opacity = _opacity;
                }
            }

            if (!selfContained)
            {
                // keep opacity in BrushProxy
                _opacity = oldOpacity;
            }
        }

        public int GetBrushDepth()
        {
            int depth = 0;

            if (_brushList != null)
            {
                foreach (BrushProxy b in _brushList)
                {
                    depth += b.GetBrushDepth();
                }
            }
            else if (_brush is SolidColorBrush)
            {
                depth = 0;
            }
            else if (_brush is GradientBrush)
            {
                depth = 1;
            }
            else if (_brush is ImageBrush)
            {
                depth = 2;
            }
            else if (_brush is DrawingBrush)
            {
                depth = 2;
            }
            else
            {
                Debug.Assert(false, "Unexpected brush type");
                depth = 2;
            }

            if (_opacityMask != null)
            {
                depth += _opacityMask.GetBrushDepth();
            }

            return depth;
        }

        /// <summary>
        /// Gets cost of printing this brush, roughly the number of pixels rasterized.
        /// </summary>
        /// <param name="size">Size of fill region.</param>
        /// <returns>Returns 0 if no pixels are rasterized and no complicatd flattening expected.</returns>
        public double GetDrawingCost(Size size)
        {
            if (Utility.IsTransparent(_opacity))
            {
                return 0;
            }

            double cost = 0;

            if (_brushList != null)
            {
                // sum costs of individual brushes
                foreach (BrushProxy brush in _brushList)
                {
                    cost += brush.GetDrawingCost(size);
                }
            }
            else if (!(_brush is SolidColorBrush))
            {
                // Calculate base cost of drawing through GDIExporter.
                bool isOpaque = IsOpaque();

                if (isOpaque && (_brush.Transform == null || Utility.IsScaleTranslate(_brush.Transform.Value)))
                {
                    LinearGradientBrush linearBrush = _brush as LinearGradientBrush;

                    if (linearBrush != null)
                    {
                        // Check for axis-aligned linear gradients. As an optimization we collapse one of the
                        // dimensions to 1 pixel during rasterization.
                        cost = Configuration.RasterizationCost(
                            Utility.AreClose(linearBrush.StartPoint.X, linearBrush.EndPoint.X) ? 1 : size.Width,
                            Utility.AreClose(linearBrush.StartPoint.Y, linearBrush.EndPoint.Y) ? 1 : size.Height
                            );
                    }
                }

                if (cost == 0)
                {
                    // All other brushes are rasterized by GDIExporter.
                    cost = Configuration.RasterizationCost(size.Width, size.Height);
                }

                // When not opaque, adjust cost to account for possible blending with other brushes
                // during flattening.
                if (!isOpaque)
                {
                    cost *= Utility.TransparencyCostFactor;
                }
            }
            else
            {
                // SolidColorBrush or null brush, assume zero cost
            }

            return cost;
        }

        public bool IsWhite()
        {
            if (_brush != null)
            {
                SolidColorBrush scb = _brush as SolidColorBrush;

                if (scb != null)
                {
                    Color c = scb.Color;

                    if ((c.R == 255) && (c.G == 255) && (c.B == 255))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void CloneRealBrush()
        {
            if (_brush != null)
            {
                _brush = _brush.CloneCurrentValue();
            }
        }

        /// <summary>
        /// Determines if TileBrush viewport covers rectangle bounds.
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        /// <remarks>
        /// Coordinates are in world space.
        /// </remarks>
        public bool IsViewportCoverBounds(Rect bounds)
        {
            bool result = true;
            TileBrush tileBrush = (TileBrush)Brush;

            Debug.Assert(tileBrush.ViewportUnits == BrushMappingMode.Absolute);
            Rect viewport = tileBrush.Viewport;

            if (tileBrush.Transform != null && !tileBrush.Transform.IsIdentity)
            {
                viewport.Transform(tileBrush.Transform.Value);
            }

            // compare viewport with geometry bounds
            if (!Utility.AreClose(bounds, viewport))
            {
                // viewport dosen't cover entire geometry, multiple tiles are rendered
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Determines if TileBrush is tiled with respect to geometry bounds in world space.
        /// </summary>
        /// <remarks>
        /// TileBrush is tiled if TileMode is not None, and viewport doesn't cover entire bounds.
        /// </remarks>
        /// <returns></returns>
        public bool IsTiled(Rect bounds)
        {
            bool result = false;

            if (Brush != null)
            {
                TileBrush tileBrush = Brush as TileBrush;
                Debug.Assert(tileBrush.ViewportUnits == BrushMappingMode.Absolute);

                if (tileBrush != null &&
                    tileBrush.TileMode != TileMode.None &&
                    !IsViewportCoverBounds(bounds))
                {
                    // viewport doesn't cover geometry, then multiple tiles are rendered
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Unfolded DrawingBrush converted to Primitive.
        /// </summary>
        public Primitive GetDrawingPrimitive()
        {
            if (_drawing == null)
            {
                DrawingBrush drawingBrush = _brush as DrawingBrush;

                if (drawingBrush != null)
                {
                    Debug.Assert(drawingBrush.Drawing != null, "DrawingBrush where Drawing == null should've been culled");

                    // Calculate transformation from Drawing to world space. This is needed to estimate
                    // size of Drawing objects in world space for rasterization bitmap dimensions.
                    Matrix viewboxToViewportTransformHint = Utility.CreateViewboxToViewportTransform(drawingBrush);

                    _drawing = Primitive.DrawingToPrimitive(drawingBrush.Drawing, viewboxToViewportTransformHint);
                }
            }

            return _drawing;
        }

        /// <summary>
        /// Render a Geometry using a BrushProxy, handling OpacityMask properly
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="pen"></param>
        /// <param name="geo"></param>
        public void DrawGeometry(DrawingContext dc, Pen pen, Geometry geo)
        {
            if (_brushList != null)
            {
                foreach (BrushProxy b in _brushList)
                {
                    b.DrawGeometry(dc, null, geo);
                }
            }
            else
            {
                UpdateRealBrush(true);

                if (_opacityMask != null)
                {
                    dc.PushOpacityMask(_opacityMask.GetRealBrush());
                }

                dc.DrawGeometry(_brush, null, geo);

                if (_opacityMask != null)
                {
                    dc.Pop();
                }
            }

            if (pen != null)
            {
                dc.DrawGeometry(null, pen, geo);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Builds an Avalon Brush from BrushProxy.
        /// </summary>
        /// <returns></returns>
        private Brush BuildBrush()
        {
            Brush brush;

            // rebuild DrawingBrush _brush from Primitive _drawing if it has changed
            if (_drawingBrushChanged)
            {
                Debug.Assert(_drawing != null, "_drawing primitive changed, but it's null");

                // convert Primitive back to Drawing
                DrawingGroup drawing = new DrawingGroup();

                using (DrawingContext context = drawing.Open())
                {
                    _drawing.OnRender(context);
                }

                //
                // Create DrawingBrush from Drawing, preserving current brush's TileBrush properties.
                // Brush properties are pulled out into the BrushProxy.
                //
                // Cannot use CreateDrawingBrush since it creates untiled brushes. _brush
                // may be tiled.
                //
                DrawingBrush currentBrush = (DrawingBrush)_brush;

                DrawingBrush newBrush = Utility.CreateNonInheritingDrawingBrush(drawing);

                newBrush.AlignmentX = currentBrush.AlignmentX;
                newBrush.AlignmentY = currentBrush.AlignmentY;
                newBrush.Stretch = currentBrush.Stretch;
                newBrush.TileMode = currentBrush.TileMode;
                newBrush.Viewbox = currentBrush.Viewbox;
                newBrush.ViewboxUnits = currentBrush.ViewboxUnits;
                newBrush.Viewport = currentBrush.Viewport;
                newBrush.ViewportUnits = currentBrush.ViewportUnits;

                newBrush.Opacity = currentBrush.Opacity;
                newBrush.RelativeTransform = currentBrush.RelativeTransform;
                newBrush.Transform = currentBrush.Transform;

                _brush = newBrush;
                _drawingBrushChanged = false;
            }

            // build new brush
            if (_opacityOnly)
            {
                brush = BuildOpacityBrush();
            }
            else
            {
                brush = BuildRegularBrush();
            }

            return brush;
        }

        /// <summary>
        /// Gets brush's fill region bounds.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Empty resulting bounds indicates that bounds aren't needed, and that this brush
        /// should simply fill entire target region. This is the case with SolidColorBrushes.
        /// </remarks>
        private Rect GetBrushFillBounds()
        {
            Rect bounds = Rect.Empty;

            // Remember that a brush list may still have _brush != null, due to building
            // avalon brush from brush list and caching it.
            if (_brushList == null)
            {
                if (!(_brush is SolidColorBrush))
                {
                    Debug.Assert(!_bounds.IsEmpty);

                    bounds = _bounds;
                }
            }
            else
            {
                // brush list: get union of children brush bounds
                Debug.Assert(_brushList != null && _bounds.IsEmpty);

                foreach (BrushProxy child in _brushList)
                {
                    bounds.Union(child.GetBrushFillBounds());
                }
            }

            return bounds;
        }

        /// <summary>
        /// Creates drawing brush from drawing and fill region bounds.
        /// </summary>
        /// <param name="drawing"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        private static DrawingBrush CreateDrawingBrush(Drawing drawing, Rect bounds)
        {
            DrawingBrush brush = Utility.CreateNonInheritingDrawingBrush(drawing);

            brush.ViewboxUnits = BrushMappingMode.Absolute;
            brush.Viewbox = drawing.Bounds;

            if (bounds.IsEmpty)
            {
                // Empty bounds indiciates fill of entire region, so keep viewport as
                // relative unit rectangle. Do nothing.
            }
            else
            {
                // Drawing was performed in absolute coordinates, use those as viewport.
                brush.ViewportUnits = BrushMappingMode.Absolute;
                brush.Viewport = brush.Viewbox;
            }

            return brush;
        }

        /// <summary>
        /// Builds brush that is opacity mask.
        /// </summary>
        /// <returns></returns>
        private Brush BuildOpacityBrush()
        {
            DrawingGroup drawing = new DrawingGroup();

            drawing.Opacity = _opacity;

            Rect bounds = GetBrushFillBounds();

            using (DrawingContext context = drawing.Open())
            {
                // push before/after color and children brushes as opacity masks
                if (!Utility.IsTransparent(_beforeDrawing.ScA))
                {
                    context.PushOpacityMask(new SolidColorBrush(_beforeDrawing));
                }

                if (_brushList == null)
                {
                    context.PushOpacityMask(_brush);
                }
                else
                {
                    foreach (BrushProxy child in _brushList)
                    {
                        context.PushOpacityMask(child.GetRealBrush());
                    }
                }

                if (!Utility.IsTransparent(_afterDrawing.ScA))
                {
                    context.PushOpacityMask(new SolidColorBrush(_afterDrawing));
                }

                // fill opacity mask bounds with opaqueness
                Geometry geometry;
                if (bounds.IsEmpty)
                {
                    // unit rectangle representing entire brush fill region
                    geometry = new RectangleGeometry(new Rect(0, 0, 1, 1));
                }
                else
                {
                    // we have rect specifying fill bounds
                    geometry = new RectangleGeometry(bounds);
                }

                context.DrawGeometry(Brushes.Black, null, geometry);
            }

            return CreateDrawingBrush(drawing, bounds);
        }

        /// <summary>
        /// Renders drawing brush Primitive to DrawingBrush.
        /// </summary>
        private Brush BuildRegularBrush()
        {
            DrawingGroup drawing = new DrawingGroup();

            Rect bounds = GetBrushFillBounds();

            using (DrawingContext context = drawing.Open())
            {
                // construct geometry representing brush bounds if needed
                RectangleGeometry geometry = null;

                if (!Utility.IsTransparent(_beforeDrawing.ScA) || !Utility.IsTransparent(_afterDrawing.ScA) || _brushList == null)
                {
                    if (bounds.IsEmpty)
                    {
                        // unit rectangle representing entire brush fill region
                        geometry = new RectangleGeometry(new Rect(0, 0, 1, 1));
                    }
                    else
                    {
                        // we have rect specifying fill bounds
                        geometry = new RectangleGeometry(bounds);
                    }
                }

                // Compose brush from before/after colors and brush/brushlist.
                // Brush opacity does not apply to before/after colors.
                if (!Utility.IsTransparent(_beforeDrawing.ScA))
                {
                    context.DrawGeometry(new SolidColorBrush(_beforeDrawing), null, geometry);
                }

                bool opacityPushed = false;

                if (_brushList == null)
                {
                    double inheritedOpacity = _opacity;

                    if (_brushList == null && !Utility.IsTransparent(_brush.Opacity))
                    {
                        // push only inherited opacity, since brush opacity will be applied
                        // during DrawGeometry.
                        inheritedOpacity /= _brush.Opacity;
                    }

                    if (!Utility.IsOpaque(inheritedOpacity))
                    {
                        context.PushOpacity(inheritedOpacity);
                        opacityPushed = true;
                    }

                    context.DrawGeometry(_brush, null, geometry);
                }
                else
                {
                    if (!Utility.IsOpaque(_opacity))
                    {
                        context.PushOpacity(_opacity);
                        opacityPushed = true;
                    }

                    foreach (BrushProxy child in _brushList)
                    {
                        Brush childBrush = child.GetRealBrush();
                        Rect childBounds = child.GetBrushFillBounds();

                        Geometry childGeometry;

                        if (childBounds.IsEmpty)
                        {
                            // child brush fills entire region, use parent brush's geometry
                            childGeometry = geometry;
                        }
                        else
                        {
                            // child brush has its own fill region
                            childGeometry = new RectangleGeometry(childBounds);
                        }

                        context.DrawGeometry(
                            childBrush,
                            null,
                            childGeometry
                            );
                    }
                }

                if (opacityPushed)
                {
                    context.Pop();
                }

                if (!Utility.IsTransparent(_afterDrawing.ScA))
                {
                    context.DrawGeometry(new SolidColorBrush(_afterDrawing), null, geometry);
                }
            }

            return CreateDrawingBrush(drawing, bounds);
        }

        /// <summary>
        /// Check if a Drawing is opaque within a rectangular area.
        /// Being opaque means rendering using it will not depending on any background color.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="viewbox"></param>
        /// <param name="transform">Approximate transformation from Drawing to world space</param>
        /// <returns>True if the drawing is definitely opaque within viewbox</returns>
        private bool IsDrawingOpaque(Primitive p, Geometry viewbox, Matrix transform)
        {
            if (p == null)
            {
                return false;
            }

            if (!Utility.IsOpaque(p.Opacity))
            {
                return false;
            }

            CanvasPrimitive cp = p as CanvasPrimitive;

            if (cp != null)
            {
                // recursively check children opaqueness
                transform = p.Transform * transform;

                foreach (Primitive c in cp.Children)
                {
                    if (IsDrawingOpaque(c, viewbox, transform))
                    {
                        return true;
                    }
                }

                return false;
            }
            else if (p.IsOpaque)
            {
                // Get primitive geometry transformed to world space. GetShapeGeometry should
                // already transform by Primitive.Transform.
                Geometry shape = Utility.TransformGeometry(p.GetShapeGeometry(), transform);

                shape = Utility.Exclude(viewbox, shape, p.Transform);

                if (shape == null)
                {
                    return true;
                }

                Rect bounds = shape.Bounds;

                if (bounds.IsEmpty)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if a Drawing is transparent within a rectangular area.
        /// Being opaque means rendering using it will not depending on any background color.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="viewbox">Viewbox in world space, must have drawingToWorldTransformHint applied</param>
        /// <param name="drawingToWorldTransformHint">Approximate transformation from Drawing to world space</param>
        /// <returns>True if the drawing is definitely transparent within viewbox</returns>
        /// <remarks>
        /// Fix bug 1505766: drawingToWorldTransformHint is used to transform primitives to world space before doing
        /// geometric comparisons. Comparing geometry that's too small may result in false emptiness detection.
        /// </remarks>
        private bool IsDrawingTransparent(Primitive p, Geometry viewbox, Matrix drawingToWorldTransformHint)
        {
            if (p == null)
            {
                return true;
            }

            if (Utility.IsTransparent(p.Opacity))
            {
                return true;
            }

            CanvasPrimitive cp = p as CanvasPrimitive;

            if (cp != null)
            {
                // recursively check children transparency
                drawingToWorldTransformHint.Prepend(p.Transform);

                foreach (Primitive c in cp.Children)
                {
                    if (!IsDrawingTransparent(c, viewbox, drawingToWorldTransformHint))
                    {
                        return false;
                    }
                }

                return true;
            }
            else if (p.IsTransparent)
            {
                return true;
            }
            else
            {
                // Get primitive geometry transformed to world space. GetShapeGeometry should
                // already transform by Primitive.Transform.
                Geometry shape = Utility.TransformGeometry(p.GetShapeGeometry(), drawingToWorldTransformHint);

                bool empty;

                shape = Utility.Intersect(viewbox, shape, Matrix.Identity, out empty);

                if (shape == null)
                {
                    return true;
                }

                if (!Utility.IsRenderVisible(shape.Bounds))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if a TileBrush brush completely fills target region.
        /// A completely filling brush eliminates need to fill region with
        /// background color prior to rendering the brush.
        /// </summary>
        /// <param name="brush"></param>
        /// <returns></returns>
        internal static bool IsTileCompleteCover(TileBrush brush)
        {
            Debug.Assert(brush.ViewboxUnits == BrushMappingMode.Absolute);
            Debug.Assert(brush.ViewportUnits == BrushMappingMode.Absolute);

            bool result = true;

            Rect content = Utility.GetTileContentBounds(brush);

            // Transform content to viewport. Content must cover entire viewport for TileBrush
            // to be completely covered (whether tiled or not). Otherwise the viewport will
            // have transparent areas.
            Matrix viewboxToViewportTransform = Utility.CreateViewboxToViewportTransform(brush);

            Rect worldContent = content;
            worldContent.Transform(viewboxToViewportTransform);

            if (!worldContent.Contains(brush.Viewport))
            {
                // viewport has transparent areas
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Blend an ImageBrush with a solid color
        /// </summary>
        /// <param name="color"></param>
        /// <param name="pre"></param>
        /// <returns></returns>
        private BrushProxy BlendImage(Color color, bool pre)
        {
            ImageBrush ib = _brush.CloneCurrentValue() as ImageBrush;

            ImageProxy image = new ImageProxy((BitmapSource)ib.ImageSource);

            if (pre)
            {
                image.BlendUnderColor(color, _opacity, _opacityOnly);
            }
            else
            {
                image.BlendOverColor(color, _opacity, _opacityOnly);
            }

            ib.ImageSource = image.GetImage();
            ib.Opacity = 1;

            BrushProxy proxy = BrushProxy.CreateBrush(ib, _bounds);

            return proxy;
        }

        private BrushProxy BlendDrawingBrush(Color color, bool after)
        {
            if (_opacityOnly)
            {
                Primitive drawing = GetDrawingPrimitive();

                if (drawing == null)
                {
                    return EmptyBrush; // return EmptyBrush instead of null to avoid possible null reference
                }

                BrushProxy b = this.Clone();

                // Order is not important when blending with OpacityMask
                b._drawing = drawing.BlendOpacityMaskWithColor(BrushProxy.CreateColorBrush(color));
                b._drawingBrushChanged = true;
                b.OpacityOnly = false;

                return b;
            }
            else
            {
                // fill Drawing bounds with the color
                return BlendComplexColor(color, after);
            }
        }

        /// <summary>
        /// Blend a non-filling TileBrush (such as when Stretch == None) with a solid color
        /// </summary>
        /// <param name="color"></param>
        /// <param name="pre"></param>
        /// <returns></returns>
        private BrushProxy BlendTileBrush(Color color, bool pre)
        {
            // fill the region not covered by TileBrush content with the color
            return BlendComplexColor(color, pre);
        }

        /// <summary>
        /// Performs a blend of color with brush for generic case that results in complex BrushProxy
        /// that needs reduction to Avalon brush.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="pre"></param>
        /// <returns></returns>
        /// <remarks>
        /// For some brush types (DrawingBrush, TileBrush where content doesn't completely
        /// fill target geometry), we can't easily blend color directly into brush. Instead we
        /// save color in BeforeFill and AfterFill, and upon rendering we build a DrawingBrush
        /// composed of the fill colors and the original brush.
        /// </remarks>
        private BrushProxy BlendComplexColor(Color color, bool pre)
        {
            BrushProxy b = this.Clone();

            if (pre)
            {
                b._afterDrawing = Utility.BlendColor(b._afterDrawing, color);
            }
            else
            {
                b._beforeDrawing = Utility.BlendColor(color, b._beforeDrawing);
            }

            return b;
        }

        /// <summary>
        /// Blends a gradient brush stop color with solid color.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="stopColor"></param>
        /// <param name="pre"></param>
        /// <returns></returns>
        private Color BlendStopColor(Color color, Color stopColor, bool pre)
        {
            Color result;

            if (_opacityOnly)
            {
                result = Utility.Scale(color, Utility.NormalizeOpacity(stopColor.ScA) * _opacity);
            }
            else
            {
                if (pre)
                {
                    result = Utility.BlendColor(Utility.Scale(stopColor, _opacity), color);
                }
                else
                {
                    result = Utility.BlendColor(color, Utility.Scale(stopColor, _opacity));
                }
            }

            return result;
        }

        /// <summary>
        /// Calculates the number of stops blending two existing stops to generate.
        /// </summary>
        /// <param name="brush"></param>
        /// <param name="firstIndex">Index of first stop</param>
        /// <param name="secondIndex">Index of second stop</param>
        private static int CalculateBlendingStopCount(
            GradientBrush brush,
            int firstIndex,
            int secondIndex
            )
        {
            GradientStop first = brush.GradientStops[firstIndex];
            GradientStop second = brush.GradientStops[secondIndex];

            // Calculate distance between stops in world space.
            double stopDistance = 100.0;
            bool brushHandled = false;

            {
                LinearGradientBrush b = brush as LinearGradientBrush;

                if (b != null)
                {
                    brushHandled = true;

                    // calculate gradient length
                    double dx = b.EndPoint.X - b.StartPoint.X;
                    double dy = b.EndPoint.Y - b.StartPoint.Y;
                    double length = Math.Sqrt(dx * dx + dy * dy);

                    // map offsets to absolute coordinates
                    stopDistance = (second.Offset - first.Offset) * length;
                }
            }

            {
                RadialGradientBrush b = brush as RadialGradientBrush;

                if (b != null)
                {
                    brushHandled = true;

                    stopDistance = Math.Max(b.RadiusX, b.RadiusY) * (second.Offset - first.Offset);

                    // use diamater
                    stopDistance *= 2;
                }
            }

            if (!brushHandled)
            {
                Debug.Assert(false, "Unhandled GradientBrush type");
            }

            //
            // Calculate stop count. Factors were experimentally determined for best appearance.
            //
            // At small stop distances, the number of stops matters considerably, but it stabilizes
            // to about 24 stops at large distances (including page-sized gradients).
            //
            int stopCount = (int)Math.Ceiling(-6.297427 + 4.591693 * Math.Log(stopDistance));

            if (stopCount > 24)
            {
                return 24;
            }
            else if (stopCount < 3)
            {
                // anything less looks obviously wrong even for 5x5 gradients
                return 3;
            }
            else
            {
                return stopCount;
            }
        }

        /// <summary>
        /// Blend a gradient brush with a solid color
        /// </summary>
        /// <param name="color"></param>
        /// <param name="pre"></param>
        /// <param name="interpolationMode"></param>
        /// <returns></returns>
        private BrushProxy BlendGradient(Color color, bool pre, ColorInterpolationMode interpolationMode)
        {
            GradientBrush g = _brush as GradientBrush;

            bool ScRgb   = interpolationMode == ColorInterpolationMode.ScRgbLinearInterpolation;
            bool addStop = false;

            //
            // Fix bug 1511960/1693561: Avalon no longer premultiplies alpha when calculating gradient color.
            // When two neighboring stops exist where color differs in both alpha and color, or when
            // stop colors differ in alpha and the color we're blending with is not opaque, this results
            // in a gradient whose stops can't be blended with solid color. We detect these cases and fall back
            // to insert more Gradient stops.
            //
            // Example case: Gradient from 0xff0000ff to 0x00ff0000 on white background. With premultiplied
            // alpha when blending the stops, the resulting gradient is purely from blue to white. Without
            // premultiplied alpha the gradient is from blue to red to white.
            //
            if (!_opacityOnly && ! ScRgb)
            {
                Debug.Assert(g.GradientStops != null);

                bool colorOpaque = Utility.IsOpaque(color.ScA);

                for (int i = 1; i < g.GradientStops.Count; i++)
                {
                    GradientStop stop0 = g.GradientStops[i - 1];
                    GradientStop stop1 = g.GradientStops[i];

                    Color color0 = stop0.Color;
                    Color color1 = stop1.Color;

                    if (color0.A != color1.A)
                    {
                        // alpha differs
                        if (!colorOpaque ||
                            color0.R != color1.R ||
                            color0.G != color1.G ||
                            color0.B != color1.B)
                        {
                            // blend color isn't opaque, or color channels also differ.
                            // need to do stops.
                            addStop = true;
                            break;
                        }
                    }
                }
            }

            // Otherwise blend stops with color.
            g = g.CloneCurrentValue();
            GradientStopCollection gsc = new GradientStopCollection();

            g.Opacity = 1.0f;

            if (! ScRgb && ! addStop)
            {
                // Blend color into gradient stops.
                foreach (GradientStop gs in g.GradientStops)
                {
                    Color c = BlendStopColor(color, gs.Color, pre);
                    gsc.Add(new GradientStop(c, gs.Offset));
                }
            }
            else
            {
                //
                // Fix bug 1039871: NGCPP - radial gradient flattening, color interpolation doesn't match avalon
                //
                // This bug is due to incorrectness of blending color into gradient stops when using ScRgb
                // gradient color interpolation. Such interpolation is mathematically incorrect due to non-linearity
                // of ScRgb. We fix by manually approximating the interpolation through the addition of stops.
                //
                g.ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation;

                Debug.Assert(g.GradientStops.Count > 0);

                // Get the first stop.
                GradientStop prevStop = g.GradientStops[0];
                Color prevColor = Utility.NormalizeColor(prevStop.Color);

                GradientStop currentStop;
                Color currentColor;

                for (int stopIndex = 1; stopIndex < g.GradientStops.Count; stopIndex++)
                {
                    //
                    // Get current stop, and generate stops interpolating in ScRgb space at positions
                    // between prevStop inclusive and currentStop exclusive.
                    //
                    currentStop = g.GradientStops[stopIndex];
                    currentColor = Utility.NormalizeColor(currentStop.Color);

                    int blendCount = CalculateBlendingStopCount(g, stopIndex - 1, stopIndex);

                    if (addStop)    // reducing addStop count for srgb
                    {
                        blendCount = (blendCount + 1 ) / 2;
                    }

                    for (int blendIndex = 0; blendIndex < (blendCount - 1); blendIndex++)
                    {
                        float b = (float)blendIndex / (float)(blendCount - 1);
                        float a = 1.0f - b;

                        // Blend stop colors.
                        Color blend;

                        if (ScRgb)
                        {
                            blend = Color.FromScRgb(
                                        a * prevColor.ScA + b * currentColor.ScA,
                                        a * prevColor.ScR + b * currentColor.ScR,
                                        a * prevColor.ScG + b * currentColor.ScG,
                                        a * prevColor.ScB + b * currentColor.ScB
                                    );
                        }
                        else
                        {
                            blend = Color.FromArgb(
                                        (Byte) (a * prevColor.A + b * currentColor.A),
                                        (Byte) (a * prevColor.R + b * currentColor.R),
                                        (Byte) (a * prevColor.G + b * currentColor.G),
                                        (Byte) (a * prevColor.B + b * currentColor.B)
                                    );
                        }

                        // Blend with the solid color we're blending gradient with.
                        blend = BlendStopColor(
                            color,
                            blend,
                            pre
                            );

                        // Add the stop.
                        double offset = prevStop.Offset + b * (currentStop.Offset - prevStop.Offset);
                        gsc.Add(new GradientStop(blend, offset));
                    }

                    // Next stop.
                    prevStop = currentStop;
                    prevColor = currentColor;
                }

                // Add the last stop, which will be prevStop.
                prevColor = BlendStopColor(color, prevStop.Color, pre);
                gsc.Add(new GradientStop(prevColor, prevStop.Offset));
            }

            g.GradientStops = gsc;

            BrushProxy bp = BrushProxy.CreateBrush(g, _bounds);

            return bp;
        }

        /// <summary>
        /// Blend a solid color brush with the first or last brush in a brush list.
        /// </summary>
        /// <param name="b"></param>
        /// <param name="first"></param>
        /// <returns></returns>
        private BrushProxy BlendBrushList(BrushProxy b, bool first)
        {
            Debug.Assert(b._brush is SolidColorBrush, "SolidColorBrush expected");
            Debug.Assert(!b._opacityOnly, "OpacityMask not expected");

            int count = _brushList.Count;

            // SolidColorBrush ^ [b1 b2 ... bn] -> b1' ^ [b2 ... bn]
            if (_opacityOnly)
            {
                if (count == 0)
                {
                    return b;
                }

                Debug.Assert(first, "prefix only");

                b = b.BlendBrush(_brushList[0] as BrushProxy);

                if (count == 2)
                {
                    b._opacityMask = _brushList[1] as BrushProxy;
                }
                else if (count > 2)
                {
                    b._opacityMask = new BrushProxy();

                    for (int i = 1; i < count; i++)
                    {
                        (_brushList[i] as BrushProxy).AddTo(b._opacityMask);
                    }
                }

                return b;
            }
            else
            {
                BrushProxy list = new BrushProxy();

                foreach (BrushProxy bp in _brushList)
                {
                    if (first && (count == _brushList.Count))
                    {
                        b.BlendBrush(bp).AddTo(list); // Blend current with first in list
                    }
                    else if (!first && (count == 1))
                    {
                        bp.BlendBrush(b).AddTo(list); // Blend current with last in list
                    }
                    else
                    {
                        bp.AddTo(list);
                    }

                    count--;
                }

                return list;
            }
        }

        /// <summary>
        /// Check if brushA 'supercedes' brushB
        /// </summary>
        /// <param name="brushA"></param>
        /// <param name="brushB"></param>
        /// <returns></returns>
        private static bool Supercede(Brush brushA, Brush brushB)
        {
            TileBrush tA = brushA as TileBrush;

            if ((tA != null) && (tA.Stretch == Stretch.Fill) && (tA.TileMode == TileMode.Tile))
            {
                if (brushB is SolidColorBrush)
                {
                    return true;
                }

                Matrix matA = brushA.Transform.Value;
                Matrix matB = brushB.Transform.Value;

                matA.Invert();

                Matrix B2A = matB * matA;

                if (Utility.IsScaleTranslate(B2A))
                {
                    Rect viewportA = tA.Viewport;

                    TileBrush tB = brushB as TileBrush;

                    if ((tB != null) && (tB.Stretch == Stretch.Fill))
                    {
                        Rect viewportB = tB.Viewport;

                        viewportB.Transform(B2A);

                        double width = viewportB.Width;
                        double height = viewportB.Height;

                        switch (tB.TileMode)
                        {
                            case TileMode.Tile:
                                break;

                            case TileMode.FlipX:
                                width *= 2;
                                break;

                            case TileMode.FlipY:
                                height *= 2;
                                break;

                            case TileMode.FlipXY:
                                width *= 2;
                                height *= 2;
                                break;

                            default:
                                return false;
                        }

                        if (Utility.IsMultipleOf(viewportA.Width, width) &&
                            Utility.IsMultipleOf(viewportA.Height, height))
                        {
                            return true;
                        }
                    }

                    LinearGradientBrush lB = brushB as LinearGradientBrush;

                    if (lB != null)
                    {
                        double multiplier = 1;

                        switch (lB.SpreadMethod)
                        {
                            case GradientSpreadMethod.Reflect:
                                multiplier = 2;
                                break;

                            case GradientSpreadMethod.Repeat:
                                break;

                            default:
                                return false;
                        }

                        Point start = B2A.Transform(lB.StartPoint);
                        Point end = B2A.Transform(lB.EndPoint);

                        if (Utility.IsZero(start.X - end.X))
                        {
                            double height = Math.Abs(start.Y - end.Y) * multiplier;

                            if (Utility.IsMultipleOf(viewportA.Height, height))
                            {
                                return true;
                            }
                        }
                        else if (Utility.IsZero(start.Y - end.Y))
                        {
                            double width = Math.Abs(start.X - end.X) * multiplier;

                            if (Utility.IsMultipleOf(viewportA.Width, width))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Blend a 'compatible' brush with an ImageBrush to form a new ImageBrush
        /// </summary>
        /// <param name="brushB">Brush to blend with</param>
        /// <param name="pre"></param>
        /// <returns>New brush if successful</returns>
        private BrushProxy BlendImageBrush(BrushProxy brushB, bool pre)
        {
            ImageBrush ib = _brush as ImageBrush;

            if ((ib != null) && (brushB.Brush != null) && Supercede(ib, brushB.Brush)) // Check for compatibility
            {
                BitmapSource bs = (BitmapSource)(ib.ImageSource);

                if (bs != null)
                {
                    // Increase resolution for small image, to avoid losing information when blend with another brush
                    int imageWidth = bs.PixelWidth;
                    int imageHeight = bs.PixelHeight;

                    // Scale up the image if width or height is less than 128. Using 128 is just a heuristic.
                    // A better way would be finding the actual destination size and consider rasterization resolution
                    int scalex = (128 + imageWidth - 1) / imageWidth;
                    int scaley = (128 + imageHeight - 1) / imageHeight;

                    if ((scalex != 1) || (scaley != 1))
                    {
                        bs = new TransformedBitmap(bs, new ScaleTransform(scalex, scaley));

                        imageWidth *= scalex;
                        imageHeight *= scaley;
                    }

                    ImageProxy image = new ImageProxy(bs);

                    Rect viewport = ib.Viewport;

                    Matrix mat = (ib.Transform == null) ? Matrix.Identity : ib.Transform.Value;
                    mat.Invert();

                    mat.Translate(-viewport.Left, -viewport.Top);

                    double tileWidth = viewport.Width;
                    double tileHeight = viewport.Height;

                    mat.Scale(imageWidth / tileWidth, imageHeight / tileHeight);

                    image.PushOpacity(_opacity, _opacityMask, ib.Viewport, Matrix.Identity);

                    if (pre)
                    {
                        image.BlendUnderBrush(_opacityOnly, brushB, mat);
                    }
                    else
                    {
                        image.BlendOverBrush(_opacityOnly, brushB, mat);
                    }

                    ImageBrush ibnew = ib.CloneCurrentValue() as ImageBrush;

                    ibnew.Opacity      = 1.0;
                    ibnew.ImageSource  = image.GetImage();
                    ibnew.ViewboxUnits = BrushMappingMode.RelativeToBoundingBox;
                    ibnew.Viewbox      = new Rect(0, 0, 1, 1);

                    BrushProxy bp = BrushProxy.CreateBrush(ibnew, _bounds);

                    Debug.Assert(bp != null, "Blending visible ImageBrush with another brush should yield non-empty brush");

                    bp._opacityOnly = _opacityOnly & brushB._opacityOnly;

                    return bp;
                }
            }

            return null;
        }

        /// <summary>
        /// Blend GradientStopCollection if two GradientBrushes has the same GradientStop positions
        /// </summary>
        /// <param name="a"></param>
        /// <param name="opacityOnlyA"></param>
        /// <param name="b"></param>
        /// <param name="opacityOnlyB"></param>
        /// <returns></returns>
        private static GradientStopCollection BlendGradientStops(GradientBrush a, bool opacityOnlyA, GradientBrush b, bool opacityOnlyB)
        {
            if (a.ColorInterpolationMode != b.ColorInterpolationMode)
            {
                return null;
            }

            GradientStopCollection gcA = a.GradientStops;
            GradientStopCollection gcB = b.GradientStops;

            if ((gcA != null) && (gcB != null) && (gcA.Count == gcB.Count))
            {
                for (int i = 0; i < gcA.Count; i++)
                {
                    GradientStop gsA = gcA[i];
                    GradientStop gsB = gcB[i];

                    if ((gsA == null) || (gsB == null) || !Utility.IsZero(gsA.Offset - gsB.Offset))
                    {
                        return null;
                    }
                }

                GradientStopCollection g = new GradientStopCollection();

                for (int i = 0; i < gcA.Count; i++)
                {
                    GradientStop gsA = gcA[i];
                    GradientStop gsB = gcB[i];

                    GradientStop gs = new GradientStop();

                    gs.Offset = gsA.Offset;

                    if (opacityOnlyB)
                    {
                        gs.Color = Utility.Scale(gsA.Color, gsB.Color.ScA);
                    }
                    else if (opacityOnlyA)
                    {
                        gs.Color = Utility.Scale(gsB.Color, gsA.Color.ScA);
                    }
                    else
                    {
                        gs.Color = Utility.BlendColor(gsA.Color, gsB.Color);
                    }

                    g.Add(gs);
                }

                return g;
            }

            return null;
        }

        /// <summary>
        /// Blend two LinearGradientBrushes together, if they are compatible
        /// </summary>
        /// <param name="brushB"></param>
        /// <returns></returns>
        private BrushProxy BlendLinearGradientBrush(BrushProxy brushB)
        {
            LinearGradientBrush lbA = this._brush as LinearGradientBrush;
            LinearGradientBrush lbB = brushB._brush as LinearGradientBrush;

            if ((lbA == null) || (lbB == null))
            {
                return null;
            }

            // 1. Same SpreadMethod
            GradientSpreadMethod spread = lbA.SpreadMethod;

            if (spread != lbB.SpreadMethod)
            {
                return null;
            }

            // 2. Vectors from StartPoint to EndPoint are the same
            Point sA = lbA.Transform.Value.Transform(lbA.StartPoint);
            Point eA = lbA.Transform.Value.Transform(lbA.EndPoint);

            double dxA = sA.X - eA.X;
            double dyA = sA.Y - eA.Y;

            Point sB = lbB.Transform.Value.Transform(lbB.StartPoint);
            Point eB = lbB.Transform.Value.Transform(lbB.EndPoint);

            double dxB = sB.X - eB.X;
            double dyB = sB.Y - eB.Y;

            if (!Utility.IsZero(dxA - dxB) || !Utility.IsZero(dyA - dyB))
            {
                return null;
            }

            // 3. Check distance between two StartPoints

            double dX = sA.X - sB.X;
            double dY = sA.Y - sB.Y;

            int factor = 1;

            switch (spread)
            {
                case GradientSpreadMethod.Pad:
                    factor = 0;     // StartPoints must be same
                    break;

                case GradientSpreadMethod.Reflect:
                    factor = 2;     // Double the cycle
                    break;

                case GradientSpreadMethod.Repeat:
                    factor = 1;     // one cycle
                    break;

                default:
                    return null;
            }

            if ((Utility.IsZero(dX) || Utility.IsMultipleOf(dX, dxA * factor)) &&
                (Utility.IsZero(dY) || Utility.IsMultipleOf(dY, dyA * factor)))
            {
                // 4. GradientStops have the same stop positions
                GradientStopCollection g = BlendGradientStops(lbA, _opacityOnly, lbB, brushB._opacityOnly);

                if (g != null)
                {
                    BrushProxy bp = this.Clone();

                    LinearGradientBrush b = lbA.CloneCurrentValue();

                    b.GradientStops = g;

                    bp._brush = b;

                    return bp;
                }
            }

            return null;
        }

        /// <summary>
        /// Blend two RadialGradientBrushes together, if they are compatible
        /// </summary>
        /// <param name="brushB"></param>
        /// <returns></returns>
        private BrushProxy BlendRadialGradientBrush(BrushProxy brushB)
        {
            RadialGradientBrush rbA = this._brush as RadialGradientBrush;
            RadialGradientBrush rbB = brushB._brush as RadialGradientBrush;

            if ((rbA == null) || (rbB == null))
            {
                return null;
            }

            // 1. Same SpreadMethod
            GradientSpreadMethod spread = rbA.SpreadMethod;

            if (spread != rbB.SpreadMethod)
            {
                return null;
            }

            // 2. Same center
            if (!Utility.AreClose(rbA.Center * rbA.Transform.Value, rbB.Center * rbB.Transform.Value))
            {
                return null;
            }

            // 3. Same Focus
            if (!Utility.AreClose(rbA.GradientOrigin * rbA.Transform.Value, rbB.GradientOrigin * rbB.Transform.Value))
            {
                return null;
            }

            // 4. Same Radiuses
            if (Utility.AreClose(new Vector(Math.Abs(rbA.RadiusX), Math.Abs(rbA.RadiusY)) * rbA.Transform.Value,
                                 new Vector(Math.Abs(rbB.RadiusX), Math.Abs(rbB.RadiusY)) * rbB.Transform.Value))
            {
                // 5. GradientStops have the same stop positions
                GradientStopCollection g = BlendGradientStops(rbA, _opacityOnly, rbB, brushB._opacityOnly);

                if (g != null)
                {
                    BrushProxy bp = this.Clone();

                    RadialGradientBrush b = rbA.CloneCurrentValue();

                    b.GradientStops = g;

                    bp._brush = b;

                    return bp;
                }
            }

            return null;
        }

        #endregion

        #region Static Methods

        static private BrushProxy _blackBrush = new BrushProxy(Brushes.Black);
        static private BrushProxy _whiteBrush = new BrushProxy(Brushes.White);

        static public bool IsOpaqueWhite(Brush brush)
        {
            SolidColorBrush sb = brush as SolidColorBrush;

            if ((sb != null) && Utility.IsOpaque(sb.Opacity))
            {
                Color c = sb.Color;

                if ((c.A == 255) && (c.R == 255) && (c.G == 255) && (c.B == 255))
                {
                    return true;
                }
            }

            return false;
        }

        static public bool IsOpaqueBlack(Brush brush)
        {
            SolidColorBrush sb = brush as SolidColorBrush;

            if ((sb != null) && Utility.IsOpaque(sb.Opacity))
            {
                Color c = sb.Color;

                if ((c.A == 255) && (c.R == 0) && (c.G == 0) && (c.B == 0))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Performs core work in constructing BrushProxy wrapper.
        /// </summary>
        /// <param name="brush"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        /// <remarks>
        /// Handles opaque white/black brushes, but otherwise does not attempt to simplify
        /// or check for empty brush.
        /// </remarks>
        private static BrushProxy CreateBrushCore(Brush brush, Rect bounds)
        {
            Debug.Assert(brush != null, "null brush");

            // empty bound requires that brush be absolute. zero area means empty brush.
            if (bounds.Width == 0 || bounds.Height == 0)
            {
                return null;
            }

            //
            // Handle simple/degenerate brushes.
            //
            if (IsOpaqueWhite(brush))
            {
                return _whiteBrush;
            }

            if (IsOpaqueBlack(brush))
            {
                return _blackBrush;
            }

            //
            // Create brush proxy.
            //
            BrushProxy brushProxy = new BrushProxy(brush);

            if (!bounds.IsEmpty)
            {
                // make brush absolute relative to specified bounds
                if (!brushProxy.MakeBrushAbsolute(bounds))
                {
                    // Fix bug 1463955: Brush has become empty; return empty brush.
                    return null;
                }
            }

            //
            // Verify created brush. Ensure that we have absolute brush.
            //
            GradientBrush gb = brushProxy.Brush as GradientBrush;

            if (gb != null)
            {
                Debug.Assert(gb.MappingMode == BrushMappingMode.Absolute, "absolute brush");
            }

            TileBrush tb = brushProxy.Brush as TileBrush;

            if (tb != null)
            {
                // Viewport must be absolute, but Viewbox can be relative
                Debug.Assert(tb.ViewportUnits == BrushMappingMode.Absolute, "absolute brush required for BrushProxy");
            }

            return brushProxy;
        }

        /// <summary>
        /// Creates a BrushProxy wrapper around SolidColorBrush.
        /// </summary>
        /// <param name="color"></param>
        /// <remarks>SolidColorBrushes are the only types of brushes that can be specified without fill bounds due
        /// to the uniformity of the fill. Otherwise bounds are needed for proper rebuilding of brushes in BuildBrush.</remarks>
        /// <returns></returns>
        public static BrushProxy CreateColorBrush(Color color)
        {
            if (Utility.IsTransparent(color.ScA))
            {
                return null;
            }
            else
            {
                return CreateBrushCore(new SolidColorBrush(color), Rect.Empty);
            }
        }

        /// <summary>
        /// Creates a BrushProxy wrapper around Brush.
        /// </summary>
        /// <param name="brush"></param>
        /// <param name="bounds">Bounds of region Brush will be filling; used to convert Brush to use absolute coordinates.</param>
        /// <returns>May return null if empty brush.</returns>
        public static BrushProxy CreateBrush(Brush brush, Rect bounds)
        {
            if (IsEmpty(brush))
            {
                return null;
            }
            else
            {
                return CreateBrushCore(brush, bounds);
            }
        }

        /// <summary>
        /// Creates a BrushProxy opacity mask wrapper around Brush.
        /// </summary>
        /// <param name="brush"></param>
        /// <param name="bounds">Bounds of region Brush will be filling; used to convert Brush to use absolute coordinates.</param>
        /// <returns>May return null if empty brush.</returns>
        public static BrushProxy CreateOpacityMaskBrush(Brush brush, Rect bounds)
        {
            if (IsEmpty(brush))
            {
                return null;
            }
            else
            {
                BrushProxy result = CreateBrushCore(brush, bounds);

                if (result != null)
                {
                    result.OpacityOnly = true;
                }

                return result;
            }
        }

        /// <summary>
        /// Creates a BrushProxy wrapper around Brush provided by user.
        /// </summary>
        /// <param name="brush"></param>
        /// <param name="bounds"></param>
        /// <param name="brushToWorldTransformHint">Transformation hint to help determine rasterization bitmap size if needed</param>
        /// <param name="treeWalkProgress">Used to detect visual tree cycles caused by VisualBrush</param>
        /// <returns>May return null if empty brush.</returns>
        /// <remarks>
        /// Attempts to simplify brush via BrushProxy.ReduceBrush.
        /// </remarks>
        public static BrushProxy CreateUserBrush(Brush brush, Rect bounds, Matrix brushToWorldTransformHint, TreeWalkProgress treeWalkProgress)
        {
            // simplify brushes so we don't have to handle as many corner cases. this also
            // simplifies empty brushes to null.
            brush = ReduceBrush(brush, bounds, brushToWorldTransformHint, Size.Empty, treeWalkProgress);

            if (brush == null)
            {
                return null;
            }
            else
            {
                return CreateBrushCore(brush, bounds);
            }
        }

        /// <summary>
        /// Returns true if brush is equivalent to a transparent brush.
        /// should rename to IsTransparent someday
        /// </summary>
        /// <param name="brush"></param>
        /// <remarks>
        /// A transparent brush is not the same as a null brush, specifically when set as the opacity mask.
        /// A null opacity mask specifies that the object is opaque, whereas a transparent opacity mask
        /// specifies a transparent object.
        ///
        /// Can handle all brush types.
        /// </remarks>
        /// <returns></returns>
        public static bool IsEmpty(Brush brush)
        {
            if (brush == null)
            {
                // see remarks for why null brush is not empty
                return false;
            }

            if (Utility.IsTransparent(brush.Opacity))
            {
                return true;
            }

            if (brush.Transform != null && !Utility.IsValid(brush.Transform.Value))
            {
                // non-invertible transform, ignore object
                return true;
            }

            SolidColorBrush solidBrush = brush as SolidColorBrush;

            if (solidBrush != null)
            {
                if (Utility.IsTransparent(solidBrush.Color.ScA))
                {
                    // transparent solid color brush
                    return true;
                }

                return false;
            }

            GradientBrush gradientBrush = brush as GradientBrush;

            if (gradientBrush != null)
            {
                GradientStopCollection stops = gradientBrush.GradientStops;

                if (stops == null || stops.Count == 0)
                {
                    // gradient contains no stops, treat as empty brush
                    return true;
                }

                foreach (GradientStop stop in stops)
                {
                    if (!Utility.IsValid(stop.Offset))
                    {
                        // invalid stop offset, treat as invisible fill
                        return true;
                    }
                }

                LinearGradientBrush linearBrush = brush as LinearGradientBrush;

                if (linearBrush != null)
                {
                    if (!Utility.IsRenderVisible(linearBrush.StartPoint) || !Utility.IsRenderVisible(linearBrush.EndPoint))
                    {
                        // endpoints not visible
                        return true;
                    }

                    return false;
                }

                RadialGradientBrush radialBrush = brush as RadialGradientBrush;

                if (radialBrush != null)
                {
                    if (!Utility.IsRenderVisible(radialBrush.Center) ||
                        !Utility.IsRenderVisible(radialBrush.GradientOrigin) ||
                        !Utility.IsRenderVisible(radialBrush.RadiusX) ||
                        !Utility.IsRenderVisible(radialBrush.RadiusY))
                    {
                        // radial gradient not visible
                        return true;
                    }

                    return false;
                }

                Debug.Assert(false, "Unhandled GradientBrush type");
                return false;
            }

            TileBrush tileBrush = brush as TileBrush;

            if (tileBrush != null)
            {
                if (! Utility.IsRenderVisible(tileBrush.Viewport) ||
                    ! Utility.IsValidViewbox(tileBrush.Viewbox, tileBrush.Stretch != Stretch.None)
                   )
                {
                    return true;
                }

                Rect contentBounds = Utility.GetTileContentBounds(tileBrush);

                if (!Utility.IsRenderVisible(contentBounds))
                {
                    return true;
                }

                return false;
            }

            Debug.Assert(false, "Unandled Brush type");

            return false;
        }

        /// <summary>
        /// Simplifies the brush.
        /// </summary>
        /// <param name="brush"></param>
        /// <param name="bounds"></param>
        /// <param name="brushToWorldTransformHint"></param>
        /// <param name="pageSize">Fixed page dimension</param>
        /// <param name="treeWalkProgress">Used to detect visual tree cycles caused by VisualBrush</param>
        /// <returns></returns>
        public static Brush ReduceBrush(Brush brush, Rect bounds, Matrix brushToWorldTransformHint, Size pageSize, TreeWalkProgress treeWalkProgress)
        {
            if (brush == null || IsEmpty(brush))
            {
                return null;
            }

            double opacity = Utility.NormalizeOpacity(brush.Opacity);

            GradientBrush gb = brush as GradientBrush;

            if (gb != null)
            {
                // check for gradient brush where colors are similar enough to be a solid brush
                GradientStopCollection gsc = gb.GradientStops;

                Debug.Assert(gsc != null && gsc.Count > 0, "BrushProxy.IsEmpty should return true upon GradientBrush with zero stops");

                bool allTrans = true;
                bool allSame = true;

                Color c = gsc[0].Color;

                foreach (GradientStop gs in gsc)
                {
                    if (!Utility.IsTransparent(gs.Color.ScA))
                    {
                        allTrans = false;
                    }

                    if (!Color.AreClose(c, gs.Color))
                    {
                        allSame = false;
                    }
                }

                if (allTrans)
                {
                    return null;
                }

                if (allSame)
                {
                    Brush b = new SolidColorBrush(c);

                    b.Opacity = opacity;

                    return b;
                }

                return brush;
            }

            BitmapCacheBrush bcb = brush as BitmapCacheBrush;
            if (bcb != null)
            {
                Debug.Assert(!bounds.IsEmpty, "Bounds must not be empty for BitmapCacheBrush");

                if (bcb.Target == null)
                {
                    return null;
                }
        
                if(treeWalkProgress.IsTreeWalkInProgress(bcb))
                {
                    // A visual tree cycle has been detected while reducing vb, calling flattener.VisualWalk on bcb.Target will cause infinite recursion
                    return null;
                }
                                
                //
                // Convert from BitmapCacheBrush to DrawingBrush to reduce the number of brush types
                // we need to handle.  This will render the BitmapCacheBrush like a VisualBrush (i.e. as vector content)
                // which is much more straightforward to implement than trying to realize the brush as a bitmap at the correct size.
                //
                // We convert with help from VisualTreeFlattener, to handle transformations, opacity,
                // etc. The building of the resulting Drawing takes place in DrawingFlattenDrawingContext.
                //
                DrawingGroup drawing = new DrawingGroup();

                using (DrawingContext context = drawing.Open())
                {
                    DrawingFlattenDrawingContext metroContext = new DrawingFlattenDrawingContext(context);

                    // Mark the brush to avoid cycles in the visual tree
                    treeWalkProgress.EnterTreeWalk(bcb);
                    Visual visual = bcb.Target;
                    try 
                    {
                        VisualTreeFlattener flattener = new VisualTreeFlattener(metroContext, pageSize, treeWalkProgress);
                        flattener.VisualWalk(visual);
                    }
                    finally 
                    {
                        treeWalkProgress.ExitTreeWalk(bcb);
                    }                    

                    // Get Visual descendant bounds with clipping taken into consideration.
                    Rect visualBounds = VisualTreeHelper.GetDescendantBounds(visual);

                    Geometry visualClip = VisualTreeHelper.GetClip(visual);

                    if (visualClip != null)
                    {
                        visualBounds.Intersect(visualClip.Bounds);
                    }

                    // Get visual transform, and draw transformed rectangle covering descendant bounds
                    // to ensure Drawing bounds matches Visual descendant bounds.
                    Transform visualTransform = Utility.GetVisualTransform(visual);
                    context.PushTransform(visualTransform);

                    context.DrawGeometry(
                        Brushes.Transparent,
                        null,
                        new RectangleGeometry(visualBounds)
                        );

                    context.Pop();                        
                }

                DrawingBrush drawingBrush = Utility.CreateNonInheritingDrawingBrush(drawing);

                // copy Brush properties
                drawingBrush.Opacity = opacity;
                drawingBrush.RelativeTransform = bcb.RelativeTransform;
                drawingBrush.Transform = bcb.Transform;

                return drawingBrush;
            }
            
            VisualBrush vb = brush as VisualBrush;

            if (vb != null)
            {
                Debug.Assert(!bounds.IsEmpty, "Bounds must not be empty for VisualBrush");

                if (vb.Visual == null)
                {
                    return null;
                }
        
                if(treeWalkProgress.IsTreeWalkInProgress(vb))
                {
                    // A visual tree cycle has been detected while reducing vb, calling flattener.VisualWalk on its vb,Visual will cause infinite recursion
                    return null;
                }
                                
                //
                // Convert from VisualBrush to DrawingBrush to reduce the number of brush types
                // we need to handle.
                //
                // We convert with help from VisualTreeFlattener, to handle transformations, opacity,
                // etc. The building of the resulting Drawing takes place in DrawingFlattenDrawingContext.
                //
                DrawingGroup drawing = new DrawingGroup();

                using (DrawingContext context = drawing.Open())
                {
                    //
                    // Fix bug 1452451: Reduction of VisualBrush to DrawingBrush does preserve dimensions of
                    // non-visible elements such as Canvas, causing resulting DrawingBrush content to have
                    // possibly smaller bounds. But VisualBrush Viewbox may be in relative units, which can
                    // cause stretching if content bounds change.
                    //
                    // Fix is to draw transparent rectangle covering Visual descendant bounds to preserve
                    // bounds. Also apply Visual clip.
                    //
                    // Fix bug 1514270: VisualTreeFlattener may rasterize parts of Visual (3D content,
                    // bitmap effects), but the rasterization is done in bitmap-space, which may lead to
                    // poor fidelity as the low-resolution bitmaps are stretched to fill a large region.
                    // We need to provide VisualTreeFlattener a hint as to the transformation from
                    // VisualBrush.Visual to world-space.
                    //
                    DrawingFlattenDrawingContext metroContext = new DrawingFlattenDrawingContext(context);

                    Matrix visualToWorldTransformHint = Utility.CreateViewboxToViewportTransform(vb, bounds);
                    visualToWorldTransformHint.Append(brushToWorldTransformHint);

                    // Mark the visual brush to avoid cycles in the visual tree
                    treeWalkProgress.EnterTreeWalk(vb);
                    try 
                    {
                        VisualTreeFlattener flattener = new VisualTreeFlattener(metroContext, pageSize, treeWalkProgress);
                        flattener.InheritedTransformHint = visualToWorldTransformHint;
                        flattener.VisualWalk(vb.Visual);
                    }
                    finally 
                    {
                        treeWalkProgress.ExitTreeWalk(vb);
                    }                    

                    // Get Visual descendant bounds with clipping taken into consideration.
                    Rect visualBounds = VisualTreeHelper.GetDescendantBounds(vb.Visual);

                    Geometry visualClip = VisualTreeHelper.GetClip(vb.Visual);

                    if (visualClip != null)
                    {
                        visualBounds.Intersect(visualClip.Bounds);
                    }

                    // Get visual transform, and draw transformed rectangle covering descendant bounds
                    // to ensure Drawing bounds matches Visual descendant bounds.
                    Transform visualTransform = Utility.GetVisualTransform(vb.Visual);
                    context.PushTransform(visualTransform);

                    context.DrawGeometry(
                        Brushes.Transparent,
                        null,
                        new RectangleGeometry(visualBounds)
                        );

                    context.Pop();                        
                }

                DrawingBrush drawingBrush = Utility.CreateNonInheritingDrawingBrush(drawing);

                // copy TileBrush properties
                drawingBrush.AlignmentX = vb.AlignmentX;
                drawingBrush.AlignmentY = vb.AlignmentY;
                drawingBrush.Stretch = vb.Stretch;
                drawingBrush.TileMode = vb.TileMode;
                drawingBrush.Viewbox = vb.Viewbox;
                drawingBrush.ViewboxUnits = vb.ViewboxUnits;
                drawingBrush.Viewport = vb.Viewport;
                drawingBrush.ViewportUnits = vb.ViewportUnits;

                // copy Brush properties
                drawingBrush.Opacity = opacity;
                drawingBrush.RelativeTransform = vb.RelativeTransform;
                drawingBrush.Transform = vb.Transform;

                return drawingBrush;
            }

            ImageBrush ib = brush as ImageBrush;

            if (ib != null)
            {
                BitmapSource bitmapSource = ib.ImageSource as BitmapSource;

                if (bitmapSource != null)
                {
                    // we can handle bitmap images
                    return brush;
                }

                DrawingImage drawingImage = ib.ImageSource as DrawingImage;

                if (drawingImage != null)
                {
                    // convert to DrawingBrush to reduce number of ImageBrush.ImageSource types we need to handle
                    DrawingBrush db = Utility.CreateNonInheritingDrawingBrush(drawingImage.Drawing);

                    // copy TileBrush properties
                    db.AlignmentX = ib.AlignmentX;
                    db.AlignmentY = ib.AlignmentY;
                    db.Stretch = ib.Stretch;
                    db.TileMode = ib.TileMode;
                    db.Viewbox = ib.Viewbox;
                    db.ViewboxUnits = ib.ViewboxUnits;
                    db.Viewport = ib.Viewport;
                    db.ViewportUnits = ib.ViewportUnits;

                    // copy Brush properties
                    db.Opacity = opacity;
                    db.RelativeTransform = ib.RelativeTransform;
                    db.Transform = ib.Transform;

                    return db;
                }

                Debug.Assert(false, "Unhandled ImageBrush.ImageSource type");
            }

            return brush;
        }

        public static BrushProxy BlendBrush(BrushProxy one, BrushProxy two)
        {
            if (one == null)
            {
                return two;
            }

            if (two == null)
            {
                return one;
            }

            return one.BlendBrush(two);
        }

        public static BrushProxy BlendColorWithBrush(bool opacityOnly, Color colorA, BrushProxy brushB, bool reverse)
        {
            if (opacityOnly)
            {
                if (Utility.IsOpaque(colorA.ScA))
                {
                    return brushB;
                }

                BrushProxy b = brushB.Clone();

                b.PushOpacity(colorA.ScA, null);

                return b;
            }

            if (brushB._opacityMask != null)
            {
                if (reverse)
                {
                    return brushB.BlendBrush(BrushProxy.CreateColorBrush(colorA));
                }
                else
                {
                    return BrushProxy.CreateColorBrush(colorA).BlendBrush(brushB);
                }
            }

            // SolidColorBrush * BrushList
            if (brushB._brushList != null)
            {
                return brushB.BlendBrushList(BrushProxy.CreateColorBrush(colorA), !reverse);
            }

            Debug.Assert(brushB.Brush != null, "null brush not expected");

            if (reverse)
            {
                if (Utility.IsOpaque(colorA.ScA))
                {
                    return BrushProxy.CreateColorBrush(colorA);
                }
            }
            else
            {
                if (brushB.IsOpaque())
                {
                    if (brushB._opacityOnly)
                    {
                        return BrushProxy.CreateColorBrush(colorA);
                    }
                    else
                    {
                        return brushB;
                    }
                }
            }

            // SolidColorBrush * SolidColorBrush
            if (brushB.Brush is SolidColorBrush)
            {
                SolidColorBrush sB = brushB.Brush as SolidColorBrush;

                if (brushB._opacityOnly)
                {
                    return BrushProxy.CreateColorBrush(
                        Utility.Scale(
                            colorA,
                            Utility.NormalizeOpacity(sB.Color.ScA) * brushB._opacity
                            )
                        );
                }
                else
                {
                    return BrushProxy.CreateColorBrush(
                        Utility.BlendColor(
                            colorA,
                            Utility.Scale(
                                sB.Color,
                                brushB._opacity
                                )
                            )
                        );
                }
            }

            //
            // SolidColorBrush * TileBrush where TileBrush does not completely fill
            // (example: TileBrush.Stretch == Stretch.None)
            //
            // We need to fill region with color before/after filling with TileBrush,
            // since TileBrush does not completely fill. An alternative is to clip
            // TileBrush fill to its content, but that requires an internal Avalon fix.
            //
            if (brushB.Brush is TileBrush && !brushB._opacityOnly)
            {
                TileBrush tileBrush = (TileBrush)brushB.Brush;
                if (!IsTileCompleteCover(tileBrush))
                {
                    return brushB.BlendTileBrush(colorA, reverse);
                }
            }

            // SolidColorBrush * GradientBrush
            if (brushB.Brush is GradientBrush)
            {
                GradientBrush gradientBrush = (GradientBrush)brushB.Brush;
                return brushB.BlendGradient(colorA, reverse, gradientBrush.ColorInterpolationMode);
            }

            // SolidColorBrush * ImageBrush
            if (brushB.Brush is ImageBrush)
            {
                return brushB.BlendImage(colorA, reverse);
            }

            // SolidColorBrush * DrawingBrush
            if (brushB.Brush is DrawingBrush)
            {
                return brushB.BlendDrawingBrush(colorA, reverse);
            }

            Debug.Assert(false, "Brush type not expected");

            return brushB;
        }

        #endregion

        #region Public Properties

        public Brush Brush
        {
            get
            {
                return _brush;
            }
        }

        public double Opacity
        {
            get
            {
                return _opacity;
            }
            set
            {
                Debug.Assert(Utility.NormalizeOpacity(value) == value, "BrushProxy.Opacity must always be normalized");

                _opacity = value;
            }
        }

        public BrushProxy OpacityMask
        {
            get
            {
                return _opacityMask;
            }
            set
            {
                _opacityMask = value;
            }
        }

        /// <summary>
        /// Color fill prior to brush fill.
        /// </summary>
        /// <remarks>
        /// Not affected by brush opacity.
        /// </remarks>
        public Color BeforeFill
        {
            get
            {
                return _beforeDrawing;
            }
        }

        /// <summary>
        /// Color fill after brush fill.
        /// </summary>
        /// <remarks>
        /// Not affected by brush opacity.
        /// </remarks>
        public Color AfterFill
        {
            get
            {
                return _afterDrawing;
            }
        }

        public ArrayList BrushList
        {
            get
            {
                return _brushList;
            }
        }

        public bool OpacityOnly
        {
            get
            {
                return _opacityOnly;
            }
            set
            {
                _opacityOnly = value;
            }
        }

        [Flags]
        // Brush types are used both for classification and determine which brush to decompose first
        // Brush with higher number is decomposed first
        public enum BrushTypes
        {
            None = 0,

            SolidColorBrush = 1,
            ImageBrush = 2,
            DrawingBrush = 4,
            BrushList = 8,
            LinearGradientBrush = 16,  // Favour linear gradient brush in decomposition
            RadialGradientBrush = 32,  // Favour radial gradient brush more in decomposition

            HasOpacityMask = 64,  // Decompose brushes with opacity mask first
            OpacityMaskOnly = 128
        };

        public BrushTypes BrushType
        {
            get
            {
                BrushTypes result = BrushTypes.None;

                if (_opacityOnly)
                {
                    result |= BrushTypes.OpacityMaskOnly;
                }

                if (_opacityMask != null)
                {
                    result |= BrushTypes.HasOpacityMask;
                }

                if (_brushList != null)
                {
                    result |= BrushTypes.BrushList;
                }
                else if (_brush != null)
                {
                    if (_brush is SolidColorBrush)
                    {
                        result |= BrushTypes.SolidColorBrush;
                    }
                    else if (_brush is LinearGradientBrush)
                    {
                        result |= BrushTypes.LinearGradientBrush;
                    }
                    else if (_brush is RadialGradientBrush)
                    {
                        result |= BrushTypes.RadialGradientBrush;
                    }
                    else if (_brush is ImageBrush)
                    {
                        result |= BrushTypes.ImageBrush;
                    }
                    else if (_brush is DrawingBrush)
                    {
                        result |= BrushTypes.DrawingBrush;
                    }
                    else
                    {
                        Debug.Assert(false, "Unexpected brush type");
                    }
                }

                return result;
            }
        }

        static public BrushProxy EmptyBrush
        {
            get
            {
                if (s_EmptyBrush == null)
                {
                    s_EmptyBrush = new BrushProxy(new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)));
                }

                return s_EmptyBrush;
            }
        }

        #endregion

        #region Private Fields

        private Brush _brush;
        private ImageProxy _image;       // Image proxy for ImageBrush
        private double _opacity;         // Combined brush opacity and element opacity
        private BrushProxy _opacityMask;

        private Rect _bounds = Rect.Empty;// bounds of region this brush is filling, from CreateBrush

        //
        // _beforeDrawing and _afterDrawing are used to specify fill colors before and
        // after rendering TileBrush. It is needed since depending on brush content and
        // TileBrush.Stretch, the brush may not completely fill target geometry, thus making
        // it impossible to somehow blend colors into the underlying Brush itself.
        //
        // When rendering, push/pop these colors before and after pushing/popping _opacity.
        //
        private Color _beforeDrawing = Color.FromArgb(0, 0, 0, 0);
        private Color _afterDrawing = Color.FromArgb(0, 0, 0, 0);

        //
        // If _brush is DrawingBrush, its content is converted to Primitive _drawing so
        // that we can push opacity into it, etc. Modifying _drawing desynchronizes it with
        // _brush, the latter of which is used when rasterizing with Avalon. We take notice
        // of desynchronization to force recomposition of DrawingBrush for rasterization.
        //
        // It may be the case that (_brush is DrawingBrush && _drawing == null), which indicates
        // _drawing needs to be rebuilt from _brush.
        //
        private Primitive _drawing;    // Temp solution for Drawing within a DrawingBrush
        private bool _drawingBrushChanged;

        private ArrayList _brushList;
        private bool _opacityOnly;

        static private BrushProxy s_EmptyBrush;
        #endregion
    }

    /// <summary>
    /// DrawingVisual for rasterizing a BrushProxy into a bitmap
    /// </summary>
    internal class FillVisual : DrawingVisual
    {
        public FillVisual(BrushProxy brush, Matrix mat, int width, int height) : base()
        {
            using (DrawingContext ctx = RenderOpen())
            {
                if (brush.Brush != null)
                {
                    Brush b = brush.Brush.CloneCurrentValue();

                    Matrix bm = b.Transform.Value;

                    bm.Append(mat);

                    b.Transform = new MatrixTransform(bm);
                    b.Opacity = brush.Opacity;

                    Rect rect = new Rect(0, 0, width, height);

                    BrushProxy mask = brush.OpacityMask;

                    // Bug 1699894: OpacityMask is now supported by DrawingContext
                    if (mask != null)
                    {
                        Brush mb = mask.GetRealBrush().CloneCurrentValue();

                        Matrix mbm = mb.Transform.Value;

                        mbm.Append(mat);

                        mb.Transform = new MatrixTransform(mbm);
                        mb.Opacity = mask.Opacity;

                        ctx.PushOpacityMask(mb);
                    }

                    if (brush.BeforeFill.A != 0)
                    {
                        ctx.DrawRectangle(new SolidColorBrush(brush.BeforeFill), null, rect);
                    }

                    ctx.DrawRectangle(b, null, rect);

                    if (brush.AfterFill.A != 0)
                    {
                        ctx.DrawRectangle(new SolidColorBrush(brush.AfterFill), null, rect);
                    }

                    if (mask != null)
                    {
                        ctx.Pop();
                    }
                }
                else
                {
                    Debug.Assert(false, "Single brush expected");
                }
            }
        }
    }

    /// <summary>
    /// Represending color using 4 floating point numbers.
    /// We need to store temp out of [0..1] range color even in SRgb mode
    /// </summary>
    internal struct MyColor
    {
        #region Public Fields

        public float m_a;
        public float m_r;
        public float m_g;
        public float m_b;

        #endregion

        #region Constructors

        private MyColor(float a, float r, float g, float b)
        {
            Debug.Assert(
                Utility.IsValid(a) && Utility.IsValid(r) && Utility.IsValid(g) && Utility.IsValid(b),
                "MyColor float constructor has invalid color values"
                );

            m_a = a;
            m_r = r;
            m_g = g;
            m_b = b;
        }

        public MyColor(Color c, ColorInterpolationMode ciMode)
        {
            if (ciMode == ColorInterpolationMode.ScRgbLinearInterpolation)
            {
                c = Utility.NormalizeColor(c);

                m_a = c.ScA;
                m_r = c.ScR;
                m_g = c.ScG;
                m_b = c.ScB;
            }
            else
            {
                m_a = (float) (c.A / 255.0);
                m_r = (float) (c.R / 255.0);
                m_g = (float) (c.G / 255.0);
                m_b = (float) (c.B / 255.0);
            }
        }

        #endregion

        #region Public Methods

        public Color ToColor(ColorInterpolationMode ciMode)
        {
            if (ciMode == ColorInterpolationMode.ScRgbLinearInterpolation)
            {
                return Color.FromScRgb(m_a, m_r, m_g, m_b);
            }
            else
            {
                return Color.FromArgb(Utility.OpacityToByte(m_a), Utility.ColorToByte(m_r), Utility.ColorToByte(m_g), Utility.ColorToByte(m_b));
            }
        }
        #endregion

        #region Public Static Methods

        public static MyColor Interpolate(MyColor c0, float a, MyColor c1, float b)
        {
            return new MyColor(c0.m_a * a + c1.m_a * b,
                               c0.m_r * a + c1.m_r * b,
                               c0.m_g * a + c1.m_g * b,
                               c0.m_b * a + c1.m_b * b);
        }

        #endregion
    }

    internal class GradientColor
    {
        #region Constructors

        public GradientColor(GradientStopCollection stops, double opacity, GradientSpreadMethod spread, ColorInterpolationMode ciMode)
        {
            Debug.Assert(Utility.IsValid(opacity), "Opacity comes from BrushProxy, should be valid");

            double min = Double.MaxValue;
            double max = Double.MinValue;

         // _count  = 0;
            _color  = new MyColor[stops.Count + 2];
            _offset = new double [stops.Count + 2];
            _ciMode = ciMode;

            for (int i = 0; i < stops.Count; i++)
            {
                double offset = stops[i].Offset;

                // Only need the largest negative offset
                if ((offset < 0) && (min < 0) && (offset < min))
                {
                    continue;
                }

                // Only need the smalest positive offset larger than 1
                if ((offset > 1) && (max > 1) && (offset > max))
                {
                    continue;
                }

                // Gradient color interpolation is now not premultiplied.
                //
                MyColor color = new MyColor(stops[i].Color, _ciMode);
                color.m_a = (float)(color.m_a * opacity);

                if (AddStop(offset, color))
                {
                    min = Math.Min(min, offset);
                    max = Math.Max(max, offset);
                }
            }

            if (_count >= 2)
            {
                if (min > 0)
                {
                    AddStop(0, InterpolateColor(0, _offset[0], _color[0], _offset[1], _color[1]));
                }

                if (max < 1)
                {
                    AddStop(1, InterpolateColor(1, _offset[_count - 2], _color[_count - 2], _offset[_count - 1], _color[_count - 1]));
                }
            }

            _spread = spread;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a color when gradient brush has invalid endpoints.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// This currently retrieves the last color to match Avalon in some cases.
        /// </remarks>
        public Color GetInvalidGradientColor()
        {
            return _color[_count - 1].ToColor(_ciMode);
        }

        public Color GetColor(int i, int steps)
        {
            if (_count == 0)
            {
                Debug.Assert(false);    // Optimization is needed before reaching here

                return Color.FromArgb(0, 255, 255, 255); // transparent white
            }
            else if (_count == 1)
            {
                Debug.Assert(false);    // Optimization is needed before reaching here

                return _color[0].ToColor(_ciMode);
            }

            Debug.Assert(steps > 0);

            switch (_spread)
            {
                case GradientSpreadMethod.Pad:
                    if (i < 0)
                    {
                        i = 0;
                    }
                    else if (i >= steps)
                    {
                        i = steps - 1;
                    }
                    break;

                case GradientSpreadMethod.Reflect:
                    if (i < 0)
                    {
                        i = -i;
                    }

                    i = i % (steps * 2);

                    if (i >= steps)
                    {
                        i = steps * 2 - 1 - i;
                    }
                    break;

                case GradientSpreadMethod.Repeat:
                default:
                    while (i < 0)
                    {
                        i += steps;
                    }

                    i = i % steps;
                    break;
            }

            Debug.Assert((i >= 0) && (i < steps));

            float t = (float) (i) / (steps - 1);

            for (int c = 0; c < _count - 1; c ++)
            {
                if (t >= _offset[c] && (t <= _offset[c + 1]))
                {
                    MyColor mc = InterpolateColor(t, _offset[c], _color[c], _offset[c + 1], _color[c + 1]);

                    return mc.ToColor(_ciMode);
                }
            }

            Debug.Assert(false);

            return Color.FromArgb(0, 255, 255, 255);
        }

        /// <summary>
        /// Estimate color distance.
        /// Same colors have distance of 0.
        /// (1, 1, 1, 1) and (0, 0, 0, 0) have distance of 2.
        /// </summary>
        private static double Distance(MyColor c0, MyColor c1)
        {
            double sum = 0;

            double d;

            d = c0.m_a - c1.m_a; sum += d * d;
            d = c0.m_r - c1.m_r; sum += d * d;
            d = c0.m_g - c1.m_g; sum += d * d;
            d = c0.m_b - c1.m_b; sum += d * d;

            return Math.Sqrt(sum);
        }

        /// <summary>
        /// Estimate total color distance for gradient stops between [0..1]
        /// </summary>
        /// <returns></returns>
        public double ColorDistance()
        {
            double distance = 0;

            for (int i = 1; i < _count; i++)
            {
                if ((_offset[i - 1] >= 0) && (_offset[i] <= 1))
                {
                    distance += Distance(_color[i - 1], _color[i]);
                }
            }

            return distance;
        }

        public int BandSteps(double distance)
        {
            double step = distance / 96 * 20;                   // 20 steps per inch, for color distance 1 (#FF0000 & #000000)

            step *= ColorDistance();                            // Adjust by color distance
            step *= Configuration.GradientDecompositionDensity; // Adjust by external supplied density

            return (int)Math.Ceiling(Math.Max(5, step));        // At least five. Radials look bad with less steps.
        }

        #endregion

        #region Private Methods

        private bool AddStop(double offset, MyColor c)
        {
            // Avoid colors at the same offset after the 2nd one
            for (int k = 0; k < _count - 1; k++)
            {
                if ((Utility.AreClose(offset, _offset[k])) &&
                     Utility.AreClose(offset, _offset[k + 1]))
                {
                    return false;
                }
            }

            // Insert in increasing offset order
            int j = _count - 1;

            while (j >= 0)
            {
                if (offset >= _offset[j])
                {
                    break;
                }

                _offset[j + 1] = _offset[j];
                _color[j + 1] = _color[j];

                j--;
            }

            _offset[j + 1] = offset;
            _color[j + 1] = c;

            _count++;

            return true;
        }

        static private MyColor InterpolateColor(double offset, double i0, MyColor c0, double i1, MyColor c1)
        {
            double di = i1 - i0;

            Debug.Assert(di >= 0);

            if (Math.Abs(di) < Double.Epsilon)
            {
                if (offset < i0)
                {
                    return c0;
                }
                else
                {
                    return c1;
                }
            }

            float a = (float)((i1 - offset) / di);
            float b = (float)((offset - i0) / di);

            return MyColor.Interpolate(c0, a, c1, b);
        }

        #endregion

        #region Private Fields

        private GradientSpreadMethod _spread;

        private MyColor[] _color;  // colors with premultiplied alpha
        private double[] _offset;
        private int _count;
        private ColorInterpolationMode _ciMode;

        #endregion
    }

    /// <summary>
    /// Break linear gradient brush fill into slides with solid colors
    /// </summary>
    internal class LinearGradientFlattener
    {
        #region Constructors

        public LinearGradientFlattener(LinearGradientBrush brush, Geometry geometry, double opacity)
        {
            //
            // The general idea is to divide the gradient into bands that are perpendicular to the
            // gradient vector. We transform the gradient so it lies along the x-axis. Thus, getting
            // a band of the gradient involves creating a rectangular slice of the x-axis-aligned gradient,
            // and transforming it back to world-space.
            //

            _shape = geometry;
            _gradient = new GradientColor(brush.GradientStops, opacity, brush.SpreadMethod, brush.ColorInterpolationMode);

            Matrix brushToWorldTransform = (brush.Transform == null) ? Matrix.Identity : brush.Transform.Value;

            if (!Utility.IsRenderVisible(brush.StartPoint) ||
                !Utility.IsRenderVisible(brush.EndPoint) ||
                !Utility.IsValid(brushToWorldTransform))
            {
                // We have invalid/extreme brush points or transformation.
                return;
            }

            // In brush space, map its start point to origin and its end point to x-axis.
            // We call this new space x-space. Also store the length of the gradient vector.
            Matrix brushToXTransform;
            if (!TransformGradientToXAxis(brush, out brushToXTransform, out _bandWidth))
            {
                // invalid gradient brush
                return;
            }

            // Get bounding box of geometry in x-space. Slices of this will form
            // band rectangles that'll be transformed back into world space via _bandTransform.
            Matrix worldToXTransform = brushToWorldTransform;
            worldToXTransform.Invert();
            worldToXTransform.Append(brushToXTransform);

            Geometry xgeometry = Utility.TransformGeometry(geometry, worldToXTransform);
            _bounds = xgeometry.Bounds;

            // Compute x-space to world transform; we use this to transform each band to world space.
            _bandTransform = worldToXTransform;
            _bandTransform.Invert();

            //
            // Divide a single cycle of gradient into N slices.
            //
            {
                // need to scale band width to world space to increase fidelity for small brush fills
                // that get magnified
                double xToWorldScale = Utility.GetScale(brushToWorldTransform);

                _bandSteps = _gradient.BandSteps(_bandWidth * xToWorldScale);
                _bandDelta = _bandWidth / _bandSteps;     // Width of each slices

                double right = Math.Ceiling(_bounds.Right / _bandDelta);
                double left = Math.Floor(_bounds.Left / _bandDelta);

                _right = (int)(right);
                _left = (int)(left);

                Debug.Assert(_left <= left);
                Debug.Assert(_right >= right);
            }

            _valid = true;
        }

        #endregion

        #region Public Methods

        public Geometry GetSlice(int i, out Color color)
        {
            if (_valid)
            {
                i += _left;

                color = _gradient.GetColor(i, _bandSteps);

                // Create a slice of the bounding box, transform to original shape's coordinate space
                return CreateRotatedRectangle(i * _bandDelta, _bounds.Top, _bandDelta, _bounds.Height, _bandTransform);
            }
            else
            {
                // invalid gradient, get the default invalid color
                color = _gradient.GetInvalidGradientColor();

                return _shape;
            }
        }

        #endregion

        #region Public Properties

        public int Steps
        {
            get
            {
                if (_valid)
                {
                    return _right - _left;
                }
                else
                {
                    return 1;
                }
            }
        }

        #endregion

        #region Private Methods

        static private Geometry CreateRotatedRectangle(double x, double y, double w, double h, Matrix mat)
        {
            StreamGeometry geometry = new StreamGeometry();

            using (StreamGeometryContext context = geometry.Open())
            {
                context.BeginFigure(mat.Transform(new Point(x, y)), true, true);
                context.LineTo(mat.Transform(new Point(x + w, y)), true, true);
                context.LineTo(mat.Transform(new Point(x + w, y + h)), true, true);
                context.LineTo(mat.Transform(new Point(x, y + h)), true, true);
            }

            return geometry;
        }

        /// <summary>
        /// Creates a transformation that places gradient vector (StartPoint -> EndPoint) onto the x-axis,
        /// with StartPoint at the origin.
        /// </summary>
        /// <param name="brush"></param>
        /// <param name="transform">Output transformation of gradient vector onto x-axis.</param>
        /// <param name="gradientVectorLength">Length of the gradient vector.</param>
        /// <returns>Returns false if gradient doesn't have a direction, and therefore the brush should be treated as invalid.</returns>
        private static bool TransformGradientToXAxis(
            LinearGradientBrush brush,
            out Matrix transform,
            out double gradientVectorLength)
        {
            transform = Matrix.CreateTranslation(-brush.StartPoint.X, -brush.StartPoint.Y);

            Vector gradientVector = brush.EndPoint - brush.StartPoint;
            gradientVectorLength = gradientVector.Length;

            if (Utility.IsZero(gradientVector.X) && Utility.IsZero(gradientVector.Y))
            {
                // gradient doesn't have a direction
                return false;
            }
            else
            {
                double rotateAngle = Math.Atan2(-gradientVector.Y, gradientVector.X) * 180.0 / Math.PI;

                transform.Rotate(rotateAngle);

                return true;
            }
        }

        #endregion

        #region Private Fields

        private bool _valid;
        private Geometry _shape;
        private GradientColor _gradient;

        private Rect _bounds;           // bounds of fill geometry transformed to brush space
        private Matrix _bandTransform;   // band transformation from brush- to world-space

        private double _bandWidth;      // length of (StartPoint -> EndPoint) vector
        private int _bandSteps;         // number of steps to use when decomposing into bands
        private double _bandDelta;      // distance between bands
        private int _left;
        private int _right;

        #endregion
    }

    /// <summary>
    /// Break radial gradient fill into rings of solid colors
    /// </summary>
    internal class RadialGradientFlattener
    {
        #region Constructors

        public RadialGradientFlattener(RadialGradientBrush b, Geometry shape, double opacity)
        {
            Debug.Assert(Utility.IsValid(opacity), "Opacity comes from BrushProxy, should be valid");

            _trans = b.Transform.Value;

            _x0 = b.Center.X;
            _y0 = b.Center.Y;
            _u0 = b.GradientOrigin.X;
            _v0 = b.GradientOrigin.Y;
            _rx = Math.Abs(b.RadiusX);
            _ry = Math.Abs(b.RadiusY);

            _shape = shape;

            _gradient = new GradientColor(b.GradientStops, opacity, b.SpreadMethod, b.ColorInterpolationMode);

            if (!Utility.IsRenderVisible(_x0) ||
                !Utility.IsRenderVisible(_y0) ||
                !Utility.IsRenderVisible(_u0) ||
                !Utility.IsRenderVisible(_v0) ||
                !Utility.IsRenderVisible(_rx) ||
                !Utility.IsRenderVisible(_ry))
            {
                return;
            }

            // Calculate shape's bounds in brush space
            // Transform oldtrans = shape.Transform;

            Matrix mat = _trans;

            mat.Invert();

            shape = Utility.TransformGeometry(shape, mat);

            Rect bounds = shape.Bounds;

            // shape.Transform = oldtrans;

            double mint = Double.MaxValue;
            double maxt = Double.MinValue;

            // If focus is within bounds, then _mint = 0
            if ((_u0 >= bounds.Left) && (_u0 <= bounds.Right) &&
                (_v0 >= bounds.Top) && (_v0 <= bounds.Bottom))
            {
                mint = 0;
            }

            {
                Point p0 = new Point(0, 0); p0 = _trans.Transform(p0);
                Point p1 = new Point(_rx, _ry); p1 = _trans.Transform(p1);

                Vector v = p0 - p1;

                _bandSteps = _gradient.BandSteps(v.Length);  // Number of steps to decompose into
            }

            // Find minimum/maximum t with four corners
            bool missing = false;

            PointIntersectWithRing(bounds.TopLeft, ref mint, ref maxt, ref missing);
            PointIntersectWithRing(bounds.TopRight, ref mint, ref maxt, ref missing);
            PointIntersectWithRing(bounds.BottomRight, ref mint, ref maxt, ref missing);
            PointIntersectWithRing(bounds.BottomLeft, ref mint, ref maxt, ref missing);

            // When distance(center, origin) > radius, gradient forms a triangle which may not touch
            // one or more corners of the bounding box.
            // Do not decompose in such cases, as it could generate endless rings.
            // Force rasterization instead by making _right a huge value.
            if (missing)
            {
                _left  = 0;
                _right = Int32.MaxValue;
            }
            else
            {
                // Find minimum t with four line segments
                if (mint > 0)
                {
                    LineSegmentIntersectWithRing(bounds.TopLeft, bounds.TopRight, ref mint);
                    LineSegmentIntersectWithRing(bounds.TopRight, bounds.BottomRight, ref mint);
                    LineSegmentIntersectWithRing(bounds.BottomRight, bounds.BottomLeft, ref mint);
                    LineSegmentIntersectWithRing(bounds.BottomLeft, bounds.TopLeft, ref mint);
                }

                if (mint < 0)
                {
                    mint = 0;
                }

                double right = Math.Ceiling(maxt * _bandSteps);
                double left  = Math.Floor(mint * _bandSteps);

                _right = BoundedInt(right);
                _left  = BoundedInt(left);

                Debug.Assert(_left <= left);
                Debug.Assert(_right >= right);
            }

            _valid = true;
        }

        #endregion

        #region Public Methods

        public Geometry GetSlice(int i, out Color color)
        {
            if (_valid)
            {
                i += _left;

                float t = (float)i / _bandSteps;

                bool simple = Utility.IsScaleTranslate(_trans);

                color = _gradient.GetColor(i - 1, _bandSteps);

                // Center for each ring gradually moving from Focus (u0, v0) to Center (x0, y0)
                Point center = new Point(_u0 * (1 - t) + _x0 * t,
                                         _v0 * (1 - t) + _y0 * t);

                Geometry geometry;

                if (simple)
                {
                    center = _trans.Transform(center);

                    geometry = new EllipseGeometry(center, _rx * t * _trans.M11, _ry * t * _trans.M22);
                }
                else
                {
                    geometry = new EllipseGeometry(center, _rx * t, _ry * t);
                    geometry.Transform = new MatrixTransform(_trans);
                }

                return geometry;
            }
            else
            {
                // Gradient isn't valid, return the default color.
                color = _gradient.GetInvalidGradientColor();

                return _shape;
            }
        }

        #endregion

        #region Public Properties

        public int Steps
        {
            get
            {
                if (_valid)
                {
                    return _right - _left;
                }
                else
                {
                    return 1;
                }
            }
        }

        #endregion

        #region Private Methods

        // Radial gradient rings are parametric curves depending on parameter t
        // Find t for the ring which goes through p
        private void PointIntersectWithRing(Point p, ref double mint, ref double maxt, ref bool missing)
        {
            // x  = _u0 * (1 - t) + _x0 * t = _u0 + (_x0 - _u0) * t
            // y  = _v0 * (1 - t) + _y0 * t = _v0 + (_y0 - _v0) * t
            // rx = _rx * t
            // ry = _ry * t

            // Solve x + rx cos(theta) = p.X
            //       y + ry sin(theta) = p.Y

            //  Hypotenuse(p.X - x) / rx, (p.Y - y) / ry) = 1

            double a0 = (p.X - _u0) / _rx;
            double a1 = (_u0 - _x0) / _rx;

            double b0 = (p.Y - _v0) / _ry;
            double b1 = (_v0 - _y0) / _ry;

            // Hypotenuse(a0/t + a1, b0 / t + b1) = 1
            // Hypotenuse(a0 + a1 * t, b0 + b1 * t) = t * t

            // A * t * t + B t + C = 0

            double A = a1 * a1 + b1 * b1 - 1;
            double B = 2 * a0 * a1 + 2 * b0 * b1;
            double C = a0 * a0 + b0 * b0;

            double B4AC = B * B - 4 * A * C;

            bool touch = false;

            if (B4AC >= 0)
            {
                double root = Math.Sqrt(B4AC);

                double one = (-B + root) / A / 2;
                double two = (-B - root) / A / 2;

                // Ignore negative solutions

                if (one >= 0)
                {
                    maxt = Math.Max(maxt, one);
                    mint = Math.Min(mint, one);

                    touch = true;
                }

                if (two >= 0)
                {
                    maxt = Math.Max(maxt, two);
                    mint = Math.Min(mint, two);

                    touch = true;
                }
            }

            if (!touch)
            {
                missing = true;
            }
        }

        // Radial gradient rings are parametric curves depending on parameter t
        // Find the minimum t for the ring which goes intesects with the line segment (p0, p1)
        private void LineSegmentIntersectWithRing(Point p0, Point p1, ref double mint)
        {
            // Scale y coordinate to make radius X and Y the same.

            double ratio = _rx / _ry;

            p0.Y *= ratio;
            p1.Y *= ratio;

            double dx = p1.X - p0.X;
            double dy = p1.Y - p0.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);

            // Ignore if line P->Q is too short
            if (len < Double.Epsilon)
            {
                return;
            }

            // Point on ellipse:
            // x = a + bt + rt cos(theta)
            // y = c + dt + rt sin(theta)
            double a = _u0;
            double b = _x0 - _u0;

            double c = _v0 * ratio;
            double d = (_y0 - _v0) * ratio;

            // Formula for the line (p0, p1):
            // Ax + By + C = 0

            double A = dy;
            double B = -dx;
            double C = p0.Y * dx - p0.X * dy;

            // minimum t is reached with the distance from (x, y) to the line equals the radius
            // | (A(a+bt) + B(c+dt) + C | = sqrt(AA+BB) * radius t
            // | Et + F | = Gt

            double E = A * b + B * d;
            double F = A * a + B * c + C;
            double G = len * _rx;

            for (int i = 0; i < 2; i++)
            {
                double t;

                if (i == 0)
                {
                    t = F / (G - E); // Et + F = Gt   => (G-E)t = F
                }
                else
                {
                    t = -F / (G + E); // -Et - F = Gt => (G+E)t = -F
                }

                if ((t >= 0) && (t < mint))
                {
                    // Center for the ellipse
                    double cx = a + b * t;
                    double cy = c + d * t;

                    // Intersection point with line P->Q
                    double sx = cx + _rx * t * dy / len;
                    double sy = cy - _rx * t * dx / len;

                    // Relative location within [P..Q]
                    double loc = ((sx - p0.X) * dx + (sy - p0.Y) * dy) / (len * len);

                    // Ignore if the intersection is outside of [P..Q]
                    if ((loc > 0) && (loc < 1))
                    {
                        mint = t;
                    }
                }
            }
        }

        private static int BoundedInt(double v)
        {
            if (v < System.Int32.MinValue)
            {
                return System.Int32.MinValue;
            }
            else if (v > System.Int32.MaxValue)
            {
                return System.Int32.MaxValue;
            }
            else
            {
                return (int)v;
            }
        }

        #endregion

        #region Private Fields

        private bool _valid;

        private double _x0; // center
        private double _y0;
        private double _u0; // focus
        private double _v0;
        private double _rx; // radius
        private double _ry;

        private Geometry _shape; // fill region

        private GradientColor _gradient;
        private Matrix        _trans;

        private int _bandSteps;
        private int _left;
        private int _right;

        #endregion
    }
}
