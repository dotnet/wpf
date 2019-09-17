// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;              // for ArrayList
using System.Diagnostics;

using System.Windows;                  // for Rect                        WindowsBase.dll
using System.Windows.Media;            // for Geometry, Brush, ImageData. PresentationCore.dll
using System.Windows.Media.Imaging;
using System.Printing;

using System.Security;

namespace Microsoft.Internal.AlphaFlattener
{
    /// <summary>
    /// Implementation of IProxyDrawingContext interface which stores primitives to a DisplayList
    /// </summary>
    internal class DisplayListDrawingContext : IProxyDrawingContext
    {
        #region Private Fields

        private Flattener  _flattener;
        private double     _opacity;
        private BrushProxy _opacityMask;
        private Matrix     _transform;
        private Geometry   _clip;

        // stores settings prior to pushing new settings
        private Stack _pushedStack = new Stack();

        #endregion

        #region Constructors

        public DisplayListDrawingContext(
                    Flattener  flattener, 
                    double     opacity, 
                    BrushProxy opacityMask, 
                    Matrix     trans, 
                    Geometry   clip)
        {
            _flattener   = flattener;
            _opacity     = opacity;
            _opacityMask = opacityMask;
            _transform   = trans;
            _clip        = clip;
        }

        #endregion

        #region IProxyDrawingContext Members

        void IProxyDrawingContext.Comment(string message)
        {
        }

        void IProxyDrawingContext.Push(double opacity, BrushProxy opacityMask)
        {
            _pushedStack.Push(_opacity);
            _pushedStack.Push(_opacityMask);

            _opacity *= opacity;
            _opacityMask = BrushProxy.BlendBrush(_opacityMask, opacityMask);
        }

        void IProxyDrawingContext.Pop()
        {
            _opacityMask = (BrushProxy)_pushedStack.Pop();
            _opacity = (double)_pushedStack.Pop();
        }

        void IProxyDrawingContext.DrawGeometry(BrushProxy brush, PenProxy pen, Geometry geometry, Geometry clip, Matrix brushTrans, ProxyDrawingFlags flags)
        {
            if ((brush != null) && (pen != null)) // Split fill & stroke into two
            {
                ((IProxyDrawingContext)(this)).DrawGeometry(brush, null, geometry, clip, brushTrans, flags);

                brush = null;
            }

            bool empty;

            clip = Utility.Intersect(_clip, Utility.TransformGeometry(clip, _transform), Matrix.Identity, out empty);

            if (empty)
            {
                return;
            }

            GeometryPrimitive geo = new GeometryPrimitive();

            // apply drawing flags to primitive
            if ((flags & ProxyDrawingFlags.PixelSnapBounds) != 0)
            {
                geo.PixelSnapBounds = true;
            }

            if (pen != null)
            {
                if (! brushTrans.IsIdentity)
                {
                    double scale;

                    bool uniform = Utility.HasUniformScale(brushTrans, out scale);

                    if (uniform)
                    {
                        geo.Pen = pen.Clone();
                        geo.Pen.Scale(scale);
                        geo.Pen.StrokeBrush.ApplyTransform(brushTrans);
                    }
                    else
                    {
                        // relative may not be good enough
                        geometry = Utility.InverseTransformGeometry(geometry, brushTrans);
                        geometry = geometry.GetWidenedPathGeometry(pen.GetPen(true), 0.0001, ToleranceType.Relative);
                        geometry = Utility.TransformGeometry(geometry, brushTrans);

                        brush = pen.StrokeBrush;
                        pen = null;
                    }
                }
                else
                {
                    geo.Pen = pen.Clone();
                }
            }

            if (brush != null)
            {
                geo.Brush = brush.ApplyTransformCopy(brushTrans);
            }

            geo.Geometry  = geometry;
            geo.Clip      = clip;
            geo.Transform = _transform;

            geo.PushOpacity(_opacity, _opacityMask);
            _flattener.AddPrimitive(geo);
        }

        void IProxyDrawingContext.DrawImage(ImageProxy image, Rect dest, Geometry clip, Matrix trans)
        {
            bool empty;

            clip = Utility.Intersect(_clip, Utility.TransformGeometry(clip, _transform), Matrix.Identity, out empty);

            if (empty)
            {
                return;
            }

            ImagePrimitive ip = new ImagePrimitive();

            // Fix bug 1460208: Give each ImagePrimitive its own ImageProxy, since rendering may alter
            // the images.
            ip.Image     = image.Clone();
            ip.DstRect   = dest;
            ip.Clip      = clip;
            ip.Transform = trans * _transform;
            
            ip.PushOpacity(_opacity, _opacityMask);
            _flattener.AddPrimitive(ip);
        }

        bool IProxyDrawingContext.DrawGlyphs(GlyphRun glyphrun, Geometry clip, Matrix trans, BrushProxy foreground)
        {
            bool empty;

            clip = Utility.Intersect(_clip, Utility.TransformGeometry(clip, _transform), Matrix.Identity, out empty);

            if (empty)
            {
                return true;
            }

            GlyphPrimitive gp = new GlyphPrimitive();

            gp.GlyphRun  = glyphrun;
            gp.Clip      = clip;
            gp.Transform = trans * _transform;
            gp.Brush     = foreground;
            
            gp.PushOpacity(_opacity, _opacityMask);
            _flattener.AddPrimitive(gp);

            return true;
        }

        #endregion
    }

    /// <summary>
    /// Implement of IProxyDrawingContext on white background. 
    ///   1) BrushProxy/PenProxy is broken down to Avalon Brush/Pen.
    ///   2) All transparency as blended with white.
    ///   3) Output is sent to ILegacyDevice interfiace.
    /// </summary>
    internal class BrushProxyDecomposer : IProxyDrawingContext
    {
#if DEBUG
        static int _seq; // = 0;
        private string         _comment;
#endif

        #region Private Fields

        private ILegacyDevice  _dc;
        private bool           _costing;
        private double         _cost;

        #endregion

        #region Constructors

        public BrushProxyDecomposer(ILegacyDevice dc)
        {
            _dc      = dc;
         // _costing = false;
         // _cost    = 0;
        }

        #endregion

        #region Private Methods

        // Breaking RadialGradientBrush apart to simplify drawing
        private bool LinearFillGeometry(BrushProxy linear, BrushProxy other, bool pre, ArrayList brushes, int from, Geometry shape)
        {
            bool                opacityOnly = false;
            LinearGradientBrush b           = null;
            double              opacity     = 0;
            bool                result      = true;

            b = linear.Brush as LinearGradientBrush;

            BrushProxy saveMask = linear.OpacityMask;
            double saveOpacity  = linear.Opacity;

            if (b != null)
            {
                opacity = linear.Opacity;
            }
            else
            {
                b           = saveMask.Brush as LinearGradientBrush;
                opacity     = saveMask.Opacity;
                opacityOnly = true;

                Debug.Assert(b != null, "LinearGradientBrush expected");
            }

            linear.OpacityMask = null;

            // Need to give flattener BrushProxy's opacity, which includes Brush's opacity and
            // opacity pushed from parent primitives.
            LinearGradientFlattener rf = new LinearGradientFlattener(b, shape, opacity);

            int steps = rf.Steps;

            if (_costing)
            {
                if (steps > Configuration.MaxGradientSteps) // Avoid decomposition if there are too many steps
                {
                    _cost = 1;
                    return true;
                }
            }
            else
            {
                _dc.PushClip(shape);
            }

            for (int i = 0; i < steps; i ++)
            {
                Color color;

                Geometry slice = rf.GetSlice(i, out color);

                BrushProxy blend;

                if (opacityOnly)
                {
                    linear.Opacity = saveOpacity * Utility.NormalizeOpacity(color.ScA);

                    if (pre)
                    {
                        blend = linear.BlendBrush(other);
                    }
                    else
                    {
                        blend = other.BlendBrush(linear);
                    }
                }
                else
                {
                    if (saveMask == null)
                    {
                        blend = BrushProxy.BlendColorWithBrush(false, color, other, !pre);
                    }
                    else
                    {
                        blend = BrushProxy.BlendColorWithBrush(false, color, saveMask, false);

                        if (blend != null)
                        {
                            if (pre)
                            {
                                blend = blend.BlendBrush(other);
                            }
                            else
                            {
                                blend = other.BlendBrush(blend);
                            }
                        }
                    }
                }

                if (blend == null)
                {
                    result = false;
                }
                else
                {
                    result = FillGeometry(blend, brushes, from, slice);
                }

                if (!result)
                {
                    break;
                }

                // Break when we already know decomposition of gradient is more costly
                if (_costing && (_cost > 0))
                {
                    break;
                }
            }

            linear.OpacityMask = saveMask;
            linear.Opacity     = saveOpacity;

            if (!_costing)
            {
                _dc.PopClip();
            }

            return result;
        }

        // Breaking RadialGradientBrush apart to simplify drawing
        private bool RadialFillGeometry(BrushProxy radial, BrushProxy other, bool pre, ArrayList brushes, int from, Geometry shape)
        {
            bool                opacityOnly = false;
            RadialGradientBrush b           = null;
            double              opacity     = 0;

            b = radial.Brush as RadialGradientBrush;

            BrushProxy saveMask = radial.OpacityMask;
            double saveOpacity  = radial.Opacity;

            if (b != null)
            {
                opacity = radial.Opacity;
            }
            else
            {
                b           = saveMask.Brush as RadialGradientBrush;
                opacity     = saveMask.Opacity;
                opacityOnly = true;

                Debug.Assert(b != null, "RadialGradientBrush expected");
            }

            radial.OpacityMask = null;

            // Need to give flattener BrushProxy's opacity, which includes Brush's opacity and
            // opacity pushed from parent primitives.
            RadialGradientFlattener rf = new RadialGradientFlattener(b, shape, opacity);

            int steps = rf.Steps;

            if (_costing)
            {
                if (steps > Configuration.MaxGradientSteps) // Avoid decomposition if there are too many steps
                {
                    _cost = 1;
                    return true;
                }
            }
            else
            {
                _dc.PushClip(shape);
            }

            bool result = true;

            for (int i = steps; i > 0; i--)
            {
                Color color;

                Geometry slice = rf.GetSlice(i, out color);

                BrushProxy blend = null;

                if (opacityOnly)
                {
                    radial.Opacity = saveOpacity * Utility.NormalizeOpacity(color.ScA);

                    if (pre)
                    {
                        blend = radial.BlendBrush(other);
                    }
                    else
                    {
                        blend = other.BlendBrush(radial);
                    }
                }
                else
                {
                    if (saveMask == null)
                    {
                        blend = BrushProxy.BlendColorWithBrush(false, color, other, !pre);
                    }
                    else
                    {
                        blend = BrushProxy.BlendColorWithBrush(false, color, saveMask, false);

                        if (pre)
                        {
                            blend = blend.BlendBrush(other);
                        }
                        else
                        {
                            blend = other.BlendBrush(blend);
                        }
                    }
                }

                result = FillGeometry(blend, brushes, from, slice);

                if (!result)
                {
                    break;
                }

                // Break when we already know decomposition of gradient is more costly
                if (_costing && (_cost > 0))
                {
                    break;
                }
            }
            
            radial.OpacityMask = saveMask;
            radial.Opacity     = saveOpacity;

            if (!_costing)
            {
                _dc.PopClip();
            }

            return result;
        }

        /// <summary>
        /// Fill a geometry using a list of brushes
        /// </summary>
        /// <param name="one">First brush to use</param>
        /// <param name="brushes">Brush list</param>
        /// <param name="from">Index to get the second brush</param>
        /// <param name="geometry">Geometry to fill</param>
        private bool FillGeometry(BrushProxy one, ArrayList brushes, int from, Geometry geometry)
        {
            Debug.Assert(one != null);

            BrushProxy two = null;

            if (one.BrushList == null)
            {
                if (from >= brushes.Count) // Only single brush left
                {
                    ((IProxyDrawingContext)this).DrawGeometry(one, null, geometry, null, Matrix.Identity, ProxyDrawingFlags.None);

                    return true;
                }

                two = brushes[from] as BrushProxy;

                from ++; // Move to next brush
            }
            else
            {
                Debug.Assert(one.BrushList.Count == 2, "Only two brushes allowed here");

                two = one.BrushList[1] as BrushProxy;
                one = one.BrushList[0] as BrushProxy;
            }

            BrushProxy.BrushTypes typeOne = one.BrushType;
            BrushProxy.BrushTypes typeTwo = two.BrushType;

            bool pre = true;

            // Try to break the brush with higher BrushTypes first, and then the other one
            for (int i = 0; i < 2; i ++)
            {
                // swap to higher one first iteration, then swap for the second loop
                if ((typeOne < typeTwo) || (i == 1))
                {
                    BrushProxy t = one; one = two; two = t;

                    BrushProxy.BrushTypes bt = typeOne; typeOne = typeTwo; typeTwo = bt;

                    pre = ! pre;
                }

                if ((typeOne & BrushProxy.BrushTypes.RadialGradientBrush) != 0)
                {
                    return RadialFillGeometry(one, two, pre, brushes, from, geometry);
                }

                if ((typeOne & BrushProxy.BrushTypes.LinearGradientBrush) != 0)
                {
                    return LinearFillGeometry(one, two, pre, brushes, from, geometry);
                }

                if ((typeOne & BrushProxy.BrushTypes.HasOpacityMask) != 0)
                {
                    BrushProxy.BrushTypes opacityType = one.OpacityMask.BrushType;

                    if ((opacityType & BrushProxy.BrushTypes.RadialGradientBrush) != 0)
                    {
                        return RadialFillGeometry(one, two, pre, brushes, from, geometry);
                    }

                    if ((opacityType & BrushProxy.BrushTypes.LinearGradientBrush) != 0)
                    {
                        return LinearFillGeometry(one, two, pre, brushes, from, geometry);
                    }
                }
            }

#if DEBUG
            if (Configuration.Verbose >= 2)
            {
                Debug.WriteLine("FillGeometry not implemented " + one + " " + two);
            }
#endif
   
            return false;
        }

        /// <summary>
        /// Check if rasterizing an area is more effective
        /// </summary>
        /// <param name="brush"></param>
        /// <param name="shape"></param>
        /// <returns>True if rasterizing is more cost effective</returns>
        private bool BetterRasterize(BrushProxy brush, Geometry shape)
        {
            if ((brush == null) || (brush.BrushList == null))
            {
                return false;
            }

            if (brush.GetBrushDepth() > Configuration.DecompositionDepth)
            {
                return true;
            }

            Rect bounds = shape.Bounds;

            _costing = true;
            _cost    = - Configuration.RasterizationCost(bounds.Width, bounds.Height);

            bool rslt = FillGeometry(brush.BrushList[0] as BrushProxy, brush.BrushList, 1, shape);

            _costing = false;

            if (rslt)
            {
                return _cost > 0;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Rasterize filling a geometry shape using a BrushProxy and render to _dc
        /// </summary>
        private void RasterizeGeometry(BrushProxy brush, Geometry shape)
        {
            Debug.Assert(brush != null, "brush expected");
            Debug.Assert(shape != null, "shape expected");
            Debug.Assert(!_costing, "in costing mode DrawImage");

            Rect bounds = shape.Bounds;

            int width  = (int) Math.Round(bounds.Width  * Configuration.RasterizationDPI / 96);
            int height = (int) Math.Round(bounds.Height * Configuration.RasterizationDPI / 96);

            if ((width >= 1) && (height >= 1)) // skip shape which is too small
            {
                Matrix mat = Utility.CreateMappingTransform(bounds, width, height);

                BitmapSource id = brush.CreateBrushImage_ID(mat, width, height);

                _dc.PushClip(shape);

#if DEBUG
                _seq++;
                _dc.Comment("-> DrawImage(raster) " + _seq);  		
#endif

                _dc.DrawImage(id, null, bounds);

#if DEBUG
                _dc.Comment("<- DrawImage(raster) " + _seq);  		
#endif

                _dc.PopClip();
            }
        }

        private static double DrawGeometryCost(Brush b, Pen p, Geometry g)
        {
            if (g == null)
            {
                return 0;
            }

            if ((b == null) && (p == null))
            {
                return 0;
            }

            if (b == null)
            {
                b = p.Brush;
            }

            if (b is SolidColorBrush)
            {
                return 512;
            }

            Rect bounds = g.Bounds;

            if (b is ImageBrush)
            {
                return 512 + Configuration.RasterizationCost(bounds.Width, bounds.Height);
            }

            if (b is LinearGradientBrush)
            {
                // need to check for vertical/horizontal
                return 512 + Configuration.RasterizationCost(Math.Max(bounds.Width, bounds.Height));
            }

            return 512 + Configuration.RasterizationCost(bounds.Width, bounds.Height);
        }

        #endregion

        #region IProxyDrawingContext Members

        void IProxyDrawingContext.Comment(string message)
        {
#if DEBUG
            _comment = message;
#endif
        }

        void IProxyDrawingContext.Push(double opacity, BrushProxy opacityMask)
        {
            // BrushProxyDecomposer sends output directly to GDI, so opacity
            // is invalid by this point.
            Debug.Assert(false, "Opacity invalid at BrushProxyDecomposer");
        }

        void IProxyDrawingContext.Pop()
        {
            Debug.Assert(false, "Opacity invalid at BrushProxyDecomposer");
        }

        void IProxyDrawingContext.DrawGeometry(BrushProxy brush, PenProxy pen, Geometry geometry, Geometry clip, Matrix brushTrans, ProxyDrawingFlags flags)
        {
            Debug.Assert(brushTrans.IsIdentity, "brushTrans not supported");

            if ((brush == null) && (pen == null))
            {
                return;
            }

            if (!_costing && (clip != null))
            {
                _dc.PushClip(clip);
            }

            if (brush != null)
            {
                brush = BrushProxy.BlendColorWithBrush(false, Colors.White, brush, false);
            }

            // Simplification, pushing transformation
            if (geometry is LineGeometry)
            {
                LineGeometry line = geometry.CloneCurrentValue() as LineGeometry;

                line.StartPoint = geometry.Transform.Value.Transform(line.StartPoint);
                line.EndPoint   = geometry.Transform.Value.Transform(line.EndPoint);
                line.Transform  = Transform.Identity;
            }

            if ((brush != null) && (brush.BrushList != null)) // List of brushes
            {
                Debug.Assert(pen == null, "no pen");

                if (_costing)
                {
                    FillGeometry(brush.BrushList[0] as BrushProxy, brush.BrushList, 1, geometry);
                }
                else
                {
                    bool rasterize = BetterRasterize(brush, geometry);

                    if (!rasterize)
                    {
                        rasterize = ! FillGeometry(brush.BrushList[0] as BrushProxy, brush.BrushList, 1, geometry);
                    }

                    if (rasterize)
                    {
                        bool empty = false;

                        if (clip != null)
                        {
                            // Fix bug 1506957: Clip geometry prior to rasterizing to prevent excessive
                            // rasterization bitmap size.
                            geometry = Utility.Intersect(geometry, clip, Matrix.Identity, out empty);
                        }

                        if (!empty)
                        {
                            RasterizeGeometry(brush, geometry);
                        }
                    }
                }
            }
            else // Single Avalon brush or pen
            {
                Pen p                  = null;
                BrushProxy strokeBrush = null;

                if (pen != null) // Blend pen with White
                {
                    p = pen.GetPen(true);
                    strokeBrush = pen.StrokeBrush;

                    if (! strokeBrush.IsOpaque())
                    {
                        strokeBrush = BrushProxy.BlendColorWithBrush(false, Colors.White, strokeBrush, false);
                    }
                }

                Brush b = null;

                if (_costing)
                {
                    if (brush != null)
                    {
                        // DrawingBrush is always rasterized onward from this stage. 
                        // Avoid the cost of creating new DrawingBrush in GetRealBrush during costing
                        if ((brush.Brush != null) && (brush.Brush is DrawingBrush))
                        {
                            b = brush.Brush;
                        }
                        else
                        {
                            b = brush.GetRealBrush();
                        }
                    }

                    _cost += DrawGeometryCost(b, p, geometry);
                }
                else
                {
                    if (brush != null)
                    {
                        b = brush.GetRealBrush();
                    }
                
#if DEBUG
                    _seq++;

                    _dc.Comment("-> DrawGeometry " + _seq + ' ' + _comment);
#endif
                    if (p == null)
                    {
                        _dc.DrawGeometry(b, null, null, geometry);
                    }
                    else
                    {
                        _dc.DrawGeometry(b, p, strokeBrush.GetRealBrush(), geometry);
                    }

#if DEBUG
                    _dc.Comment("<- DrawGeometry" + _seq + ' ' + _comment);

                    if (Configuration.Verbose >= 2)
                    {
                        Console.WriteLine("  DrawGeometry(" + _comment + ")");
                    }
#endif
                }
            }

            if (!_costing && (clip != null))
            {
                _dc.PopClip();
            }
        }

        void IProxyDrawingContext.DrawImage(ImageProxy image, Rect dest, Geometry clip, Matrix trans)
        {
            if (_costing)
            {
                _cost += image.PixelWidth * image.PixelHeight * 3;

                return;
            }

            // Sometimes clip selects only a small portion of the image. Clip image to reduce amount
            // of data sent to GDI.
            if (clip != null)
            {
                if (!Utility.IsRenderVisible(clip.Bounds))
                {
                    // completely clipped away
                    return;
                }

                // transform clip to image space, taking into account image DPI
                Matrix imageTransform = new Matrix();

                imageTransform.Scale(dest.Width / image.Image.Width, dest.Height / image.Image.Height);
                imageTransform.Translate(dest.X, dest.Y);
                imageTransform.Append(trans);

                imageTransform.Invert();

                Geometry imageClip = Utility.TransformGeometry(clip, imageTransform);

                // Clip the image to clip bounds. ImageProxy.GetClippedImage has no effect if clipping
                // bounds are almost image size.
                Rect clippedImageBounds;

                BitmapSource clippedImageSource = image.GetClippedImage(imageClip.Bounds, out clippedImageBounds);

                if (clippedImageSource == null)
                {
                    // image has been clipped away
                    return;
                }

                ImageProxy clippedImage = new ImageProxy(clippedImageSource);

                // adjust destination rectangle to new clipped image bounds
                double scaleX = dest.Width / image.Image.Width;
                double scaleY = dest.Height / image.Image.Height;

                dest = new Rect(
                    clippedImageBounds.X * scaleX + dest.X,
                    clippedImageBounds.Y * scaleY + dest.Y,
                    clippedImageBounds.Width * scaleX,
                    clippedImageBounds.Height * scaleY
                    );

                image = clippedImage;
            }

            image.BlendOverColor(Colors.White, 1.0, false);
            
            // BitmapSource img = image.GetImage();
            
            if (clip != null)
            {
                _dc.PushClip(clip);
            }
                        
            if (! trans.IsIdentity)
            {
                _dc.PushTransform(trans);
            }

#if DEBUG
            _seq ++;
            _dc.Comment("-> DrawImage " + _seq);
#endif
            
            _dc.DrawImage(image.Image, image.Buffer, dest);

#if DEBUG
            _dc.Comment("<- DrawImage " + _seq);

            if (Configuration.Verbose >= 2)
            {
                Console.WriteLine("  DrawImage(" + _comment + ")");
            }
#endif

            if (!trans.IsIdentity)
            {
                _dc.PopTransform();
            }

            if (clip != null)
            {
                _dc.PopClip();
            }
        }

        bool IProxyDrawingContext.DrawGlyphs(GlyphRun glyphrun, Geometry clip, Matrix trans, BrushProxy foreground)
        {
            Debug.Assert(!_costing, "in costing mode DrawyGlyphs");

            BrushProxy bp = BrushProxy.BlendColorWithBrush(false, Colors.White, foreground, false);

            Brush b = bp.Brush;

            if ((b == null) || (b is DrawingBrush))
            {
                return false;
            }
            
            if (clip != null)
            {
                _dc.PushClip(clip);
            }
            
            if (!trans.IsIdentity)
            {
                _dc.PushTransform(trans);
            }

#if DEBUG
            _seq ++;
            _dc.Comment("-> DrawGlyphRun " + _seq);  		
#endif

            _dc.DrawGlyphRun(b, glyphrun);

#if DEBUG
            _dc.Comment("<- DrawGlyphRun " + _seq);  		

            if (Configuration.Verbose >= 2)
            {
                Console.WriteLine("  DrawGlyphRun(" + _comment + ")");
            }
#endif

            if (!trans.IsIdentity)
            {
                _dc.PopTransform();
            }

            if (clip != null)
            {
                _dc.PopClip();
            }

            return true;
        }

        #endregion
    }

}
