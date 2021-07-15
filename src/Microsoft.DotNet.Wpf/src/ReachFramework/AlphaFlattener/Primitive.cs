// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;              // for ArrayList
using System.Diagnostics;

using System.Windows;                  // for Rect                        WindowsBase.dll
using System.Windows.Media;            // for Geometry, Brush, BitmapSource. PresentationCore.dll
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;           // for Glyphs

using System.Globalization;
using System.Text;
using System.Collections.Generic;
using System.Windows.Xps.Serialization;

namespace Microsoft.Internal.AlphaFlattener
{
    /// <summary>
    /// Base Primitive class
    /// </summary>
    internal abstract class Primitive
    {
        #region Private Fields

        private Geometry   _clip;      // changes coordinate spaces throughout printing

        /// <summary>
        /// Primitive opacity, possibly pushed from parent primitives.
        /// For example, parent Canvas opacity is pushed to children primitives when possible.
        /// </summary>
        private double     _opacity;

        private BrushProxy _opacityMask;
        private Matrix     _transform;

        //
        // Fix bug 1308518: OpacityMask with DrawingBrush results in gaps
        //
        // We convert each tile of DrawingBrush into primitive. If the tile edges don't fall
        // on pixel boundary, they are anti-aliased by Avalon, which results in "gaps". We
        // fix by setting pixel-snapping guidelines on geometry bounds when requested.
        //
        private bool       _pixelSnapBounds;

        #endregion

        #region Constructors

        public Primitive()
        {
            Opacity     = 1.0;
            Transform   = Matrix.Identity;
        }

        #endregion

        #region Public Abstract Methods

        /// <summary>
        /// Render to a DrawingContext
        /// </summary>
        /// <param name="ctx"></param>
        public abstract void OnRender(DrawingContext ctx);

        /// <summary>
        /// Get the exact outline of drawing with Transform applied
        /// </summary>
        /// <returns></returns>
        public abstract Geometry GetShapeGeometry();

        /// <summary>
        /// Exclude area covered by a Geometry or clip to it
        /// </summary>
        /// <param name="g"></param>
        public abstract void Exclude(Geometry g);

        public abstract BrushProxy BlendBrush(BrushProxy brush);

        public abstract void BlendOverImage(ImageProxy image, Matrix trans);

        /// <summary>
        /// Blend a Drawing which is used as OpacityMask with a solid color
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public abstract Primitive BlendOpacityMaskWithColor(BrushProxy color);

        #endregion

        #region Public Abstract Properties

        public abstract bool IsOpaque
        {
            get;
        }

        public abstract bool IsTransparent
        {
            get;
        }

        #endregion

        #region Public Virtual Methods

        public virtual void ApplyTransform()
        {
        }

        /// <returns>True if not empty</returns>
        public virtual bool Optimize()
        {
            return true;
        }

        /// <summary>
        /// Gets primitive-wide opacity, which is applied to entire primitive when rendering.
        /// </summary>
        public virtual double GetOpacity()
        {
            return Opacity;
        }

        public virtual void PushOpacity(double opacity, BrushProxy opacityMask)
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a shallow copy of the primitive.
        /// </summary>
        /// <returns></returns>
        public Primitive Clone()
        {
            return MemberwiseClone() as Primitive;
        }

        /// <summary>
        /// Returns a deep copy of the primitive.
        /// </summary>
        /// <remarks>
        /// Also clones the primitive tree.
        /// </remarks>
        /// <returns></returns>
        public Primitive DeepClone()
        {
            Primitive clone = Clone();

            clone.CloneMembers();

            return clone;
        }

        /// <summary>
        /// Get exact outline of drawing after clipping
        /// </summary>
        /// <returns></returns>
        public Geometry GetClippedShapeGeometry()
        {
            Geometry shape = GetShapeGeometry();

            if (shape != null && Clip != null)
            {
                bool empty;

                shape = Utility.Intersect(shape, Clip, Matrix.Identity, out empty);

                if (empty)
                {
                    shape = null;
                }
            }

            return shape;
        }

        /// <summary>
        /// Gets bounding box in world space.
        /// </summary>
        /// <returns></returns>
        public Rect GetRectBounds(bool needed)
        {
            Rect rect = Rect.Empty;

            if (needed)
            {
                rect = GetBoundsCore();

                if (!Utility.IsValid(rect))
                {
                    // transformations may've made rectangle invalid
                    rect = Rect.Empty;
                }
            }

            return rect;
        }

        /// <summary>
        /// Estimates the cost of flattening and GDIExporter rasterization.
        /// </summary>
        /// <param name="worldTransform">Transformation from primitive space to world.</param>
        /// <returns>Returns 0 if primitive incurs no cost whatsoever.</returns>
        public double GetDrawingCost(Matrix worldTransform)
        {
            if (Utility.IsTransparent(Opacity))
            {
                return 0;
            }
            else
            {
                // Calculate cost multiplier due to opacity: Translucency can cause brushes
                // to be rasterized and blended together.
                double factor = 1.0;

                if (!Utility.IsOpaque(Opacity))
                {
                    factor = Utility.TransparencyCostFactor;
                }

                return factor * GetBaseDrawingCost(worldTransform);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Primitive clip geometry.
        /// </summary>
        /// <remarks>
        /// Prior to tree flattening, Clip is in primitive space. The flattening process transforms
        /// Clip to world space.
        /// </remarks>
        public Geometry Clip
        {
            get
            {
                return _clip;
            }
            set
            {
                _clip = value;
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
                // Validate during tree walking so that calculating children opacity through multiplication
                // is done correctly.
                Debug.Assert(Utility.NormalizeOpacity(value) == value, "Opacity should be normalized during tree walking");

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

        public Matrix Transform
        {
            get
            {
                return _transform;
            }
            set
            {
                _transform = value;
            }
        }

        public bool PixelSnapBounds
        {
            get
            {
                return _pixelSnapBounds;
            }
            set
            {
                _pixelSnapBounds = value;
            }
        }

        #endregion

        #region Internal Static Methods

        /// <summary>
        /// Converts a Drawing object to a Primitive.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="drawingToWorldTransformHint">Drawing-to-world transformation;
        /// used to estimate world bounds of Drawing objects for use as rasterization bitmap dimensions.</param>
        /// <returns></returns>
        internal static Primitive DrawingToPrimitive(System.Windows.Media.Drawing d, Matrix drawingToWorldTransformHint)
        {
            if (d == null || !Utility.IsRenderVisible(d.Bounds))
            {
                return null;
            }

            {
                GeometryDrawing gd = d as GeometryDrawing;

                if (gd != null)
                {
                    GeometryPrimitive gp = null;

                    if (gd.Geometry != null)
                    {
                        gp = new GeometryPrimitive();

                        Rect bounds = gd.Geometry.Bounds;

                        if (gd.Brush != null)
                        {
                            // gd.Brush comes directly from user-provided objects, so we need to perform
                            // brush reduction
                            gp.Brush = BrushProxy.CreateUserBrush(gd.Brush, bounds, drawingToWorldTransformHint,  new TreeWalkProgress());
                        }

                        if ((gd.Pen != null) && (gd.Pen.Brush != null))
                        {
                            // pen needs to be made absolute relative to widened geometry bounds
                            Rect renderBounds = gd.Geometry.GetRenderBounds(gd.Pen);

                            gp.Pen = PenProxy.CreateUserPen(gd.Pen, renderBounds, drawingToWorldTransformHint, new TreeWalkProgress());
                        }

                        if ((gp.Brush == null) && (gp.Pen == null))
                        {
                            return null;
                        }

                        gp.Geometry = gd.Geometry;

                        if ((gp.Brush != null) && (gp.Pen != null)) // split
                        {
                            CanvasPrimitive cp = new CanvasPrimitive();

                            PenProxy pen = gp.Pen;
                            
                            gp.Pen = null;
                            
                            cp.Children.Add(gp);
                            
                            gp = new GeometryPrimitive();
                            
                            gp.Pen = pen;
                            gp.Geometry = gd.Geometry;
                            
                            cp.Children.Add(gp);
                            
                            return cp;
                        }
                    }

                    return gp;
                }
            }

            {
                GlyphRunDrawing gd = d as GlyphRunDrawing;

                if (gd != null)
                {
                    GlyphPrimitive gp = null;

                    if ((gd.GlyphRun != null) && (gd.ForegroundBrush != null))
                    {
                        gp = new GlyphPrimitive();

                        gp.GlyphRun = gd.GlyphRun;
                        gp.Brush = BrushProxy.CreateUserBrush(
                            gd.ForegroundBrush,
                            gd.GlyphRun.BuildGeometry().Bounds,
                            drawingToWorldTransformHint,
                            new TreeWalkProgress()
                            );
                    }

                    return gp;
                }
            }
            {
                ImageDrawing id = d as ImageDrawing;

                if (id != null)
                {
                    if (id.ImageSource != null)
                    {
                        DrawingImage di = id.ImageSource as DrawingImage;

                        // Convert DrawimgImage to DrawingBrush, 
                        // ImageDrawing with DrawingImage to geometry filled with DrawingBrush
                        if (di != null)
                        {
                            DrawingBrush db = Utility.CreateNonInheritingDrawingBrush(di.Drawing);

                            BrushProxy bp = BrushProxy.CreateBrush(db, id.Rect);

                            if (bp == null)
                            {
                                return null;
                            }
                            else
                            {
                                GeometryPrimitive gp = new GeometryPrimitive();

                                gp.Brush = BrushProxy.CreateBrush(db, id.Rect);
                                gp.Geometry = new RectangleGeometry(id.Rect);

                                return gp;
                            }
                        }

                        ImagePrimitive ip = new ImagePrimitive();
                        BitmapSource bs = (BitmapSource)id.ImageSource;
                                                
                        ip.Image = new ImageProxy(bs);
                        ip.DstRect = id.Rect;

                        return ip;
                    }

                    return null;
                }
            }

            DrawingGroup dg = d as DrawingGroup;

            if (dg != null)
            {
                Primitive primitive = null;

                if (Utility.IsRenderVisible(dg))
                {
                    if (dg.Transform != null)
                    {
                        drawingToWorldTransformHint.Prepend(dg.Transform.Value);
                    }

                    BitmapEffect effect = dg.BitmapEffect;

                    if (effect == null)
                    {
                        // convert drawinggroup subtree into primitive subtree
                        CanvasPrimitive cp = new CanvasPrimitive();

                        DrawingCollection children = dg.Children;

                        for (int i = 0; i < children.Count; i++)
                        {
                            Primitive p = DrawingToPrimitive(children[i], drawingToWorldTransformHint);

                            if (p != null)
                            {
                                cp.Children.Add(p);
                            }
                        }

                        if (cp.Children.Count > 0)
                        {
                            primitive = cp;
                        }
                    }
                    else
                    {
                        // DrawingGroup has bitmap effect. Rasterize entire subtree to bitmap; Avalon
                        // will handle the application of bitmap effect for us.
                        Matrix bitmapToDrawingTransform;

                        BitmapSource bitmap = Utility.RasterizeDrawing(
                            dg,
                            dg.Bounds,
                            drawingToWorldTransformHint,
                            out bitmapToDrawingTransform
                            );

                        if (bitmap != null)
                        {
                            // bitmap may be null if bounds too small/invalid
                            ImagePrimitive ip = new ImagePrimitive();

                            ip.Image = new ImageProxy(bitmap);
                            ip.DstRect = new Rect(0, 0, bitmap.Width, bitmap.Height);
                            ip.Transform = bitmapToDrawingTransform;

                            primitive = ip;
                        }
                    }

                    if (primitive != null)
                    {
                        if (dg.Transform != null)
                        {
                            primitive.Transform *= dg.Transform.Value;
                        }

                        primitive.Clip = dg.ClipGeometry;
                        primitive.Opacity = Utility.NormalizeOpacity(dg.Opacity);

                        if (dg.OpacityMask != null)
                        {
                            primitive.OpacityMask = BrushProxy.CreateOpacityMaskBrush(dg.OpacityMask, dg.Bounds);
                        }
                    }
                }

                return primitive;
            }

#if DEBUG
            Console.WriteLine("Drawing of type '" + d.GetType() + "' not handled.");
#endif

            return null;
        }

        #endregion

        #region Protected Methods

        protected int PushAll(DrawingContext dc)
        {
            int level = 0;

            // Clipping already in device coordinate space, so it needs to be pushed first
            if (Clip != null)
            {
                dc.PushClip(Clip);
                level++;
            }

            // Fix bug 1308518: Perform bounds pixel-snapping if requested to fix bug.
            if (PixelSnapBounds)
            {
                Rect bounds = GetRectBounds(true);

                double[] snapx = new double[] { bounds.Left, bounds.Right };
                double[] snapy = new double[] { bounds.Top, bounds.Bottom };

                dc.PushGuidelineSet(new GuidelineSet(snapx, snapy));
                level++;
            }

            if (!Transform.IsIdentity)
            {
                dc.PushTransform(new MatrixTransform(Transform));
                level++;
            }

            double opacity = GetOpacity();
            if (!Utility.IsOpaque(opacity))
            {
                dc.PushOpacity(opacity);
                level++;
            }

            return level;
        }

        protected static void PopAll(DrawingContext dc, int levels)
        {
            for (int i = 0; i < levels; i++)
            {
                dc.Pop();
            }
        }

        /// <summary>
        /// Extract opacity from OpacityMask to Opacity when possible
        /// </summary>
        protected void ExtractOpacity()
        {
            if ((OpacityMask != null) && (OpacityMask.Brush != null))
            {
                SolidColorBrush sb = OpacityMask.Brush as SolidColorBrush;

                if (sb != null)
                {
                    Opacity *= Utility.NormalizeOpacity(sb.Color.ScA) * OpacityMask.Opacity;

                    OpacityMask = null;
                }
            }
        }

        #endregion

        #region Protected Virtual/Abstract Methods

        /// <summary>
        /// Clones instance members of this Primitive.
        /// </summary>
        /// <remarks>
        /// Derived implementations must cal base implementation.
        /// </remarks>
        protected virtual void CloneMembers()
        {
            if (_opacityMask != null)
            {
                _opacityMask = _opacityMask.Clone();
            }
        }

        /// <summary>
        /// Gets primitive bounds in world space.
        /// </summary>
        protected abstract Rect GetBoundsCore();

        /// <summary>
        /// Gets base cost of drawing this primitive, which should include flattening and GDI
        /// rasterization cost.
        /// </summary>
        /// <param name="worldTransform">Transformation from primitive space to world.</param>
        /// <returns></returns>
        protected abstract double GetBaseDrawingCost(Matrix worldTransform);

        #endregion
    } // end of class Primitive


    /// <summary>
    /// Geometry Primitive
    /// </summary>
    internal class GeometryPrimitive : Primitive
    {
        #region Private Fields

        private BrushProxy _brush;
        private PenProxy _pen;
        private Geometry _geometry;
        private Geometry _widenGeometry;

        #endregion

        #region Constants

        private const double MyFlatteningTolerance = 0.08; // 1200 dpi

        #endregion

        #region Public Methods

        /// <summary>
        /// Change stroking to filling widened path
        /// </summary>
        public void Widen()
        {
            if (Pen != null)
            {
                Debug.Assert(Brush == null, "no brush expected");

                Brush = Pen.StrokeBrush;

                Geometry = WidenGeometry;
                _widenGeometry = null;
                Pen = null;
            }
        }

        /// <summary>
        /// Gets information for tiling the content of a TileBrush.
        /// </summary>
        /// <param name="brush">Brush whose content we're manually tiling</param>
        /// <param name="bounds">Bounds of brush fill region</param>
        /// <param name="startTile">Receives bounds of first tile</param>
        /// <param name="startScaleX">x-scaling factor for start tile; used to flip the tile</param>
        /// <param name="startScaleY">y-scaling factor for start tile; used to flip the tile</param>
        /// <param name="scaleFlipX">Factor to multiply scaling factor by whenever moving in x-direction</param>
        /// <param name="scaleFlipY">Factor to multiply scaling factor by whenever moving in y-direction</param>
        /// <param name="rowCount">Number of rows in tiling</param>
        /// <param name="columnCount">Number of columns in tiling</param>
        private static void GetTilingInformation(
            TileBrush brush,
            Rect bounds,
            out Rect startTile,
            out int startScaleX,
            out int startScaleY,
            out int scaleFlipX,
            out int scaleFlipY,
            out int rowCount,
            out int columnCount)
        {
            //
            // We calculate scaling and start tile bounds by moving in -x -y direction until
            // we obtain tile bounds that cover top-left corner of bounds.
            //

            // Calculate starting flipping scale multipliers.
            scaleFlipX = scaleFlipY = 1;

            switch (brush.TileMode)
            {
                case TileMode.None:
                    scaleFlipX = 0;
                    scaleFlipY = 0;
                    break;

                case TileMode.FlipX:
                    scaleFlipX = -1;
                    break;

                case TileMode.FlipY:
                    scaleFlipY = -1;
                    break;

                case TileMode.FlipXY:
                    scaleFlipX = -1;
                    scaleFlipY = -1;
                    break;

                case TileMode.Tile:
                default:
                    scaleFlipX = 1;
                    scaleFlipY = 1;
                    break;
            }

            // Starting at current tile, move to top-left tile, adjusting scaling factors
            // and bounds along the way.
            startScaleX = 1;
            startScaleY = 1;

            startTile = brush.Viewport;
            Debug.Assert(brush.ViewportUnits == BrushMappingMode.Absolute);

            if (brush.TileMode == TileMode.None)
            {
                // no tiling
                rowCount = columnCount = 1;
            }
            else
            {
                // move to left-most column
                while (startTile.Left > bounds.Left)
                {
                    startTile.Offset(-startTile.Width, 0);
                    startScaleX *= scaleFlipX;
                }

                columnCount = (int)Math.Ceiling((bounds.Right - startTile.Left) / startTile.Width);

                // move to top-most row
                while (startTile.Top > bounds.Top)
                {
                    startTile.Offset(0, -startTile.Height);
                    startScaleY *= scaleFlipY;
                }

                rowCount = (int)Math.Ceiling((bounds.Bottom - startTile.Top) / startTile.Height);
            }
        }

        /// <summary>
        /// Unfolds DrawingBrush tiles into one or more primitives.
        /// </summary>
        /// <returns>Returns a primitive that replaces the current GeometryPrimitive, or null if not visible.</returns>
        public Primitive UnfoldDrawingBrush()
        {
            if (_pen != null && _pen.StrokeBrush != null && _pen.StrokeBrush.Brush is DrawingBrush)
            {
                // Treat DrawingBrush stroke as fill so that we can unfold it.
                Widen();
            }
            
            if (_brush == null)
            {
                return this;
            }

            // Force rebuild of underlying DrawingBrush in case BrushProxy has attributes that necesitate
            // a DrawingBrush (i.e. before/after fill, drawing Primitive has changed).
            DrawingBrush drawingBrush = _brush.GetRealBrush() as DrawingBrush;

            // Bug: 1691872 We can't handle transformation well in unfolding yet
            
            if ((drawingBrush == null) || ! Utility.IsIdentity(drawingBrush.Transform))
            {
                // No DrawingBrush to unfold; keep current primitive.
                return this;
            }

            //
            // Unfolding a DrawingBrush occurs in brush space and involves the following steps:
            //
            // 1) Get this primitive's clipped geometry in brush space and use as tiling bounds.
            // 2) Generate tiling information from brush and tiling bounds.
            // 2) Create canvas primitive that mirror's this primitive's properties. It'll be the
            //    parent of children tile primitives.
            // 3) For each tile, clone brush's primitive tree, transform tile primitive to tile bounds,
            //    and add as child of canvas.
            //
            Matrix brushTransform = drawingBrush.Transform == null ? Matrix.Identity : drawingBrush.Transform.Value;
            Matrix brushToWorldTransform = brushTransform * Transform;

            Primitive brushPrimitive = _brush.GetDrawingPrimitive();

            if (brushPrimitive == null) // nothing to draw
            {
                return null;
            }
                
            //
            // Get primitive geometry in brush space.
            //
            Geometry worldGeometry = GetClippedShapeGeometry();
            Geometry brushGeometry;

            if (worldGeometry == null)
            {
                // nothing visible
                return null;
            }
            else
            {
                Matrix worldToBrushTransform = brushToWorldTransform;
                worldToBrushTransform.Invert();

                brushGeometry = Utility.TransformGeometry(worldGeometry, worldToBrushTransform);
            }

            Rect brushGeometryBounds = brushGeometry.Bounds;

            //
            // Get information for tiling brush content within brushGeometryBounds.
            //
            Rect startTileBounds;
            int startScaleX, startScaleY, scaleFlipX, scaleFlipY, rowCount, columnCount;

            GetTilingInformation(
                drawingBrush,
                brushGeometryBounds,
                out startTileBounds,
                out startScaleX,
                out startScaleY,
                out scaleFlipX,
                out scaleFlipY,
                out rowCount,
                out columnCount
                );

            //
            // Compare cost of rasterizing entire primitive to the cost of GDI rasterizing the
            // unfolded primitives. If GDI is more expensive, then abort and rasterize everything.
            //
            // Note that transparency and overlaps may cause unfolding to be even more expensive,
            // but this is a good enough metric.
            //
            double primitiveRasterizeCost = Configuration.RasterizationCost(
                worldGeometry.Bounds.Width,
                worldGeometry.Bounds.Height
                );

            Matrix viewboxToViewportTransform = Utility.CreateViewboxToViewportTransform(
                drawingBrush,
                drawingBrush.Viewbox,   // absolute coordinates
                startTileBounds         // absolute coordinates
                );

            double gdiRasterizeCost = rowCount * columnCount * brushPrimitive.GetDrawingCost(viewboxToViewportTransform);

            if (OpacityMask != null)
            {
                gdiRasterizeCost *= Utility.TransparencyCostFactor;
            }

            if (primitiveRasterizeCost < gdiRasterizeCost)
            {
                return this;
            }

            //
            // Create canvas primitive that'll serve as parent to tile primitives.
            //
            CanvasPrimitive canvas = new CanvasPrimitive();

            canvas.Opacity = Opacity * _brush.Opacity;
            canvas.OpacityMask = BrushProxy.BlendBrush(OpacityMask, _brush.OpacityMask);

            canvas.Clip = worldGeometry;

            //
            // Compute per-tile clipping if drawing content exceeds viewbox bounds.
            //
            bool tileClip = false;

            if (!drawingBrush.Viewbox.Contains(brushPrimitive.GetRectBounds(true)))
            {
                tileClip = true;
            }

            //
            // Generate children tile primitives.
            //
            int tileScaleX = startScaleX;
            int tileScaleY = startScaleY;

            for (int y = 0; y < rowCount; y++)
            {
                tileScaleX = startScaleX;

                for (int x = 0; x < columnCount; x++)
                {
                    // Compute tile bounds.
                    Rect tileBounds = startTileBounds;
                    tileBounds.Offset(x * startTileBounds.Width, y * startTileBounds.Height);

                    // Transform tile primitive to brush space, taking into account stretching, flipping.
                    viewboxToViewportTransform = Utility.CreateViewboxToViewportTransform(
                        drawingBrush,
                        drawingBrush.Viewbox,   // absolute
                        tileBounds              // absolute
                        );

                    Matrix tileTransform = brushPrimitive.Transform;
                    tileTransform.Append(viewboxToViewportTransform);

                    if (tileScaleX == -1 || tileScaleY == -1)
                    {
                        // apply tile flipping
                        double centerX = tileBounds.X + tileBounds.Width / 2.0;
                        double centerY = tileBounds.Y + tileBounds.Height / 2.0;

                        tileTransform.ScaleAt(tileScaleX, tileScaleY, centerX, centerY);
                    }

                    // Transform tile primitive to world space.
                    tileTransform.Append(brushToWorldTransform);

                    // Add as child of unfolded canvas.
                    Primitive tilePrimitive = brushPrimitive.DeepClone();
                    tilePrimitive.Transform = tileTransform;
                    tilePrimitive.ApplyTransform();

                    if (tileClip)
                    {
                        //
                        // Need to clip tile primitive to viewbox. Since this is pre-tree-flattening,
                        // Primitive.Clip is in primitive space. We transform world-space tileBounds to
                        // primitive space via Primitive.Transform (which may differ from tileTransform
                        // at this point due to ApplyTransform call).
                        //
                        if (tilePrimitive.Transform.IsIdentity)
                        {
                            tilePrimitive.Clip = new RectangleGeometry(tileBounds);
                        }
                        else
                        {
                            Matrix inverseTileTransform = tilePrimitive.Transform;
                            inverseTileTransform.Invert();

                            tilePrimitive.Clip = new RectangleGeometry(
                                tileBounds,
                                0,
                                0,
                                new MatrixTransform(inverseTileTransform)
                                );
                        }
                    }

                    canvas.Children.Add(tilePrimitive);

                    // next column
                    tileScaleX *= scaleFlipX;
                }

                // next row
                tileScaleY *= scaleFlipY;
            }

            return canvas;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Geometry fill brush.
        /// </summary>
        /// <remarks>
        /// Must be transformed by Transform to get world-space brush.
        /// </remarks>
        public BrushProxy Brush
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

        public PenProxy Pen
        {
            get
            {
                return _pen;
            }
            set
            {
                _pen = value;
            }
        }

        public Geometry Geometry
        {
            get
            {
                return _geometry;
            }
            set
            {
                _geometry = value;
            }
        }

        public virtual Geometry WidenGeometry
        {
            get
            {
                if (Pen != null)
                {
                    if (_widenGeometry == null)
                    {
                        Pen p = Pen.GetPen(true);

                        double scale = Utility.GetScale(Transform);

                        _widenGeometry = _geometry.GetWidenedPathGeometry(p, MyFlatteningTolerance / scale, ToleranceType.Absolute);
                        _widenGeometry.Transform = System.Windows.Media.Transform.Identity;
                    }

                    return _widenGeometry;
                }

                return Geometry;
            }
        }

        #endregion

        #region Protected Properties

        protected Geometry WidenGeometryCore
        {
            get
            {
                return _widenGeometry;
            }
            set
            {
                _widenGeometry = value;
            }
        }

        #endregion

        #region Private Methods

        private void AbsorbOpacity()
        {
            if (!Utility.IsOpaque(Opacity) || (OpacityMask != null))
            {
                if (Brush != null)
                {
                    Brush = Brush.Clone();

                    Brush = Brush.PushOpacity(Opacity, OpacityMask);
                }

                if (Pen != null)
                {
                    Pen.PushOpacity(Opacity, OpacityMask);
                }

                Opacity = 1;
                OpacityMask = null;
            }
        }

        #endregion

        #region Primitive Members

        public override void OnRender(DrawingContext dc)
        {
            if ((Geometry != null) && (Pen != null) || (Brush != null))
            {
                //
                // Fix bug 1308518: Gaps in DrawingBrush opacity mask
                //
                // The gaps are due to anti-aliasing, and we fix by snapping clipped primitive bounds
                // to pixels. The decision to clip the geometry is simply because DrawingBrush unfolding
                // uses clipping to determine which tile is rendered. The geometry drawn for each tile
                // is identical. Therefore, the geometry needs to be clipped prior to calculating pixel-
                // snapping.
                //
                bool empty = false;
                Geometry saveGeometry = Geometry;
                Geometry saveClip = Clip;

                if (PixelSnapBounds)
                {
                    // intersect clip with geometry
                    Geometry = Utility.Intersect(Geometry, Clip, Matrix.Identity, out empty);
                    Clip = null;
                }

                if (!empty)
                {
                    int level = PushAll(dc);

                    Pen p = null;

                    if (Pen != null)
                    {
                        p = Pen.GetPen(false);
                    }

                    if (Brush == null)
                    {
                        dc.DrawGeometry(null, p, Geometry);
                    }
                    else
                    {
                        Brush.DrawGeometry(dc, p, Geometry); // BrushProxy.GetRealBrush can't handle OpacityMask
                    }

                    PopAll(dc, level);
                }

                // restore
                Geometry = saveGeometry;
                Clip = saveClip;
            }
        }

        public override Geometry GetShapeGeometry()
        {
            Geometry g = Utility.TransformGeometry(WidenGeometry, Transform);

            return g;
        }

        public override void Exclude(Geometry g)
        {
            if (g != null)
            {
                Widen();

                ApplyTransform();

                Geometry = Utility.Exclude(Geometry, Utility.InverseTransformGeometry(g, Transform), Transform);
            }
        }

        public override BrushProxy BlendBrush(BrushProxy brushA)
        {
            BrushProxy b = Brush;

            if (b == null)
            {
                b = Pen.StrokeBrush;
            }

            // Transform is not pushed in GlyphPrimitive. 
            // Need to apply transform to Brush when returning to outside
            if (b != null)
            {
                b = b.ApplyTransformCopy(Transform);
            }

            return brushA.BlendBrush(b);
        }

        public override void BlendOverImage(ImageProxy image, Matrix trans)
        {
            BrushProxy b = Brush;

            if (b == null)
            {
                b = Pen.StrokeBrush;
            }

            // Transform is not pushed in GlyphPrimitive. 
            // Need to apply transform to Brush when returning to outside
            if (b != null)
            {
                b = b.ApplyTransformCopy(Transform);
            }

            image.BlendUnderBrush(false, b, trans);
        }

        public override Primitive BlendOpacityMaskWithColor(BrushProxy color)
        {
            GeometryPrimitive g = Clone() as GeometryPrimitive;

            //
            // Fix bug 1308518: OpacityMask from DrawingBrush ignored
            //
            // The original code blended the DrawingBrush on the bottom, and color on top.
            // We need to reverse the order, since the DrawingBrush is the mask applied to
            // the color.
            //
            if (g.Brush != null)
            {
                g.Brush = g.Brush.Clone();
                g.Brush.OpacityOnly = true;

                g.Brush = color.BlendBrush(g.Brush);
            }

            if (g.Pen != null)
            {
                g.Pen = g.Pen.Clone();
                g.Pen.StrokeBrush.OpacityOnly = true;

                g.Pen.StrokeBrush = color.BlendBrush(g.Pen.StrokeBrush);
            }

            return g;
        }

        public override bool IsOpaque
        {
            get
            {
                if ((Brush == null) && (Pen == null))
                {
                    return false;
                }

                if (!Utility.IsOpaque(Opacity))
                {
                    return false;
                }

                if (Brush == null)
                {
                    return Pen.IsOpaque();
                }

                if (Pen == null)
                {
                    return Brush.IsOpaque();
                }

                return Pen.IsOpaque() && Brush.IsOpaque();
            }
        }

        public override bool IsTransparent
        {
            get
            {
                if (Utility.IsTransparent(Opacity))
                {
                    return true;
                }

                if ((Brush == null) && (Pen == null))
                {
                    return true;
                }

                if (Brush == null)
                {
                    return Pen.IsTransparent();
                }

                if (Pen == null)
                {
                    return Brush.IsTransparent();
                }

                return Pen.IsTransparent() && Brush.IsTransparent();
            }
        }

        /// <summary>
        /// Push transform to geometry
        /// </summary>
        public override void ApplyTransform()
        {
            Debug.Assert((Brush != null) || (Pen != null), "empty primitive");

            if (!Transform.IsIdentity)
            {
                if (Pen != null)
                {
                    double scale;

                    // If transformation is unfirm scaling, just change pen thickness
                    if (Utility.HasUniformScale(Transform, out scale))
                    {
                        Pen.Scale(scale);
                        Pen.StrokeBrush.ApplyTransform(Transform);
                    }
                    else
                    {
                        Widen(); // We need to widen it because Pen does not have transformation
                    }
                }

                // Clip     = Utility.TransformGeometry(Clip, Transform);
                Geometry = Utility.TransformGeometry(Geometry, Transform);

                if (Brush != null)
                {
                    Brush.ApplyTransform(Transform);
                }

                Transform = Matrix.Identity; // Reset transform
                _widenGeometry = null;  // Reset cached widen geometry if any
            }
        }

        /// <summary>
        /// Remove gaps whened filled by a TileBrush with no tiling and None/Uniform stretch
        /// </summary>
        public override bool Optimize()
        {
            AbsorbOpacity();

            // Widen stroken with TileBrush, so that gap can be removed
            if ((Pen != null) && (Pen.StrokeBrush.Brush is TileBrush))
            {
                Widen();
            }
            // Fix for  1688277. The code below will operate on Geometry, which starts as empty for 
            // GlyphPrimitive until GlyphPrimitive.WidenGeometry is called. So skip for GlyphPrimitive
            else if (this is GlyphPrimitive)
            {
                return true;
            }
                            
            // Remove gaps whened filled by a TileBrush with no tiling and None/Uniform stretch
            if (Brush != null)
            {
                TileBrush tb = Brush.Brush as TileBrush;

                if (tb != null)
                {
                    // get geometry bounds in world space. can't touch Geometry directly since
                    // we might be a GlyphPrimitive, which has Geometry == null
                    Rect geometryBounds = GetRectBounds(true);

                    if (!Brush.IsTiled(geometryBounds))
                    {
                        Brush.CloneRealBrush();

                        tb = Brush.Brush as TileBrush;

                        // content top-left not necessarily at (0, 0) for DrawingBrush
                        Rect content = Utility.GetTileContentBounds(tb);

                        Rect viewbox = tb.Viewbox;      // absolute viewbox
                        Debug.Assert(tb.ViewboxUnits == BrushMappingMode.Absolute, "Absolute Viewbox expected");

                        //
                        // Calculate transform from absolute viewbox to geometry space.
                        // We then use transformed content bounds as viewport to remove gaps.
                        // Content bounds are used over viewbox since viewbox determines placement
                        // of content, but does not clip the content.
                        //
                        Matrix viewboxTransform = Utility.CreateViewboxToViewportTransform(tb);

                        // clip to geometry
                        bool empty;
                        Clip = Utility.Intersect(Clip, Geometry, Matrix.Identity, out empty);

                        // set viewport to be transformed viewbox
                        if (!empty)
                        {
                            Rect geometryViewbox = content;
                            geometryViewbox.Transform(viewboxTransform);

                            if (!tb.Viewport.Contains(geometryViewbox))
                            {
                                // New viewport larger than original viewport, clip to original viewport.
                                // This can occur if content is larger than viewport and stretch is none.
                                // Fix bug 1395406: Clip is in world space, also need to apply Primitive.Transform.
                                RectangleGeometry viewportGeometry = new RectangleGeometry(tb.Viewport);
                                viewportGeometry.Transform = Utility.MultiplyTransform(tb.Transform, new MatrixTransform(Transform));

                                Clip = Utility.Intersect(Clip, viewportGeometry, Matrix.Identity, out empty);
                            }

                            // Include entire content in viewbox. The viewbox is essentially pushed to
                            // the viewport.
                            tb.Viewbox = content;
                            tb.ViewboxUnits = BrushMappingMode.Absolute;
                            tb.Viewport = geometryViewbox;
                        }

                        //
                        // Fix bug 1376514: MGC: Black fill introduced printing ImageBrush with rotation
                        //
                        // Clip to brush content if it doesn't cover viewport in case we're rasterized,
                        // otherwise unfilled areas of rasterization bitmap will show through.
                        //
                        bool clipContent = false;

                        if (!empty && tb.Transform != null && !Utility.IsScaleTranslate(tb.Transform.Value))
                        {
                            // Rotation results in content not completely filling rasterization bitmap.
                            clipContent = true;
                        }

                        if (!empty && !Brush.IsViewportCoverBounds(geometryBounds))
                        {
                            // Viewport no longer covers geometry. During rasterization only the viewport
                            // will be filled, leaving the rest black. Thus, we need to clip to viewport.
                            clipContent = true;
                        }

                        if (!empty && clipContent)
                        {
                            // Intersect with entire content, then transform to world space.
                            // Fix bug 1395406: Clip is in world space, also need to apply Primitive.Transform.
                            content.Transform(viewboxTransform);

                            RectangleGeometry contentGeometry = new RectangleGeometry(content);
                            contentGeometry.Transform = Utility.MultiplyTransform(tb.Transform, new MatrixTransform(Transform));

                            Clip = Utility.Intersect(Clip, contentGeometry, Matrix.Identity, out empty);
                        }

                        if (empty)
                        {
                            Geometry = null;
                        }
                        else
                        {
                            Geometry = new RectangleGeometry(tb.Viewport);
                            Geometry.Transform = tb.Transform;
                        }
                    }
                }
            }

            // Optimize: Absorb clip into geometry to avoid converting two Geometry objects to GDI paths
            // in GDIExporter. In the future we may want to absorb clip only if intersection is simple.
            if (Clip != null && Geometry != null && Pen == null)
            {
                // By this point clip should be in world space. Transform back to geometry space before
                // performing intersection.
                Geometry geometryClip = Clip;

                if (!Transform.IsIdentity)
                {
                    Matrix inverseTransform = Transform;
                    inverseTransform.Invert();

                    geometryClip = Utility.TransformGeometry(geometryClip, inverseTransform);
                }

                bool empty;
                Geometry = Utility.Intersect(Geometry, geometryClip, Matrix.Identity, out empty);

                if (empty)
                {
                    Geometry = null;
                }

                Clip = null;
            }

            return Geometry != null;
        }

        // Override since opacity may be pushed into BrushProxy. Need to strip opacity from BrushProxy.
        public override double GetOpacity()
        {
            double opacity = Opacity;

            if (Brush != null)
            {
                // Push only inherited opacity; strip BrushProxy.Brush.Opacity from BrushProxy.Opacity.
                // Get real brush first in case it rebuilds the brush and modifies opacity.
                Brush realBrush = Brush.GetRealBrush();

                opacity *= Brush.Opacity;
                
                if (realBrush != null)
                {
                    double realOpacity = Utility.NormalizeOpacity(realBrush.Opacity);

                    if (realOpacity != 0)
                    {
                        opacity /= realOpacity;
                    }
                }
            }

            if (Pen != null)
            {
                // Like above, remove BrushProxy.Brush.Opacity since geometry rendering will
                // apply it.
                Brush realBrush = Pen.StrokeBrush.GetRealBrush();

                opacity *= Pen.StrokeBrush.Opacity;

                if (realBrush != null)
                {
                    double realOpacity = Utility.NormalizeOpacity(realBrush.Opacity);

                    if (realOpacity != 0)
                    {
                        opacity /= realOpacity;
                    }
                }
            }

            return opacity;
        }

        public override void PushOpacity(double opacity, BrushProxy opacityMask)
        {
            if (Utility.IsOpaque(opacity) && (opacityMask == null))
            {
                return;
            }

            OpacityMask = BrushProxy.BlendBrush(OpacityMask, opacityMask);

            ExtractOpacity();

            Opacity *= opacity;

            AbsorbOpacity();
        }

        protected override void CloneMembers()
        {
            base.CloneMembers();

            if (_brush != null)
            {
                _brush = _brush.Clone();
            }

            if (_pen != null)
            {
                _pen = _pen.Clone();
            }
        }

        protected override Rect GetBoundsCore()
        {
            Rect result;

            if (Transform.IsIdentity)
            {
                if (Pen != null)
                {
                    result = Geometry.GetRenderBounds(Pen.GetPen(true));
                }
                else
                {
                    result = Geometry.Bounds;
                }
            }
            else
            {
                Geometry g = Utility.TransformGeometry(WidenGeometry, Transform);

                if (g == null)
                {
                    result = Rect.Empty;
                }
                else
                {
                    result = g.Bounds;
                }
            }

            return result;
        }

        protected override double GetBaseDrawingCost(Matrix worldTransform)
        {
            Rect bounds = GetRectBounds(true);
            bounds.Transform(worldTransform);

            double cost = 0;

            if (_brush != null)
            {
                cost += _brush.GetDrawingCost(bounds.Size);
            }

            if (_pen != null && _pen.StrokeBrush != null)
            {
                cost += _pen.StrokeBrush.GetDrawingCost(bounds.Size);
            }

            return cost;
        }

        #endregion
    } // end of class GeometryPrimitive

    internal class GlyphPrimitive : GeometryPrimitive
    {
        #region Private Fields

        private GlyphRun _glyphRun;
        private Geometry _bounds;   // glyph bounds in world space (transformation has been applied)

        #endregion

        #region Public Properties

        public GlyphRun GlyphRun
        {
            get
            {
                return _glyphRun;
            }
            set
            {
                _glyphRun = value;
            }
        }

        #endregion

        #region GeometryPrimitive Members

        public override Geometry WidenGeometry
        {
            get
            {
                if (WidenGeometryCore == null)
                {
                    WidenGeometryCore = GlyphRun.BuildGeometry();
                }

                return WidenGeometryCore;
            }
        }

        #endregion

        #region Primitive Members

        public override void OnRender(DrawingContext dc)
        {
            if ((GlyphRun != null) && (Pen != null) || (Brush != null))
            {
                int level;

                level = PushAll(dc);

                dc.DrawGlyphRun(Brush.GetRealBrush(), GlyphRun);

                PopAll(dc, level);
            }
        }

        public override Geometry GetShapeGeometry()
        {
            Geometry g = Utility.TransformGeometry(WidenGeometry, Transform);

            return g; //  new RectangleGeometry(g.Bounds);
        }

        public override void Exclude(Geometry g)
        {
            if ((GlyphRun != null) && (g != null))
            {
                if (_bounds == null)
                {
                    // bounds are in world space
                    _bounds = new RectangleGeometry(GetRectBounds(true));
                }
                
                if (Utility.Covers(g, _bounds))
                {
                    GlyphRun = null;
                }
                else
                {
                    if (Clip == null)
                    {
                        Clip = _bounds;
                    }

                    Clip = Utility.Exclude(Clip, g, Matrix.Identity);

                    if (Clip == null)
                    {
                        GlyphRun = null;
                    }
                }
            }
        }

        public override void ApplyTransform()
        {
        }

        public override bool Optimize()
        {
            base.Optimize();

            return GlyphRun != null;
        }

        protected override Rect GetBoundsCore()
        {
            Rect bounds = Rect.Empty;

            if (GlyphRun != null)
            {
                bounds = GlyphRun.ComputeInkBoundingBox();

                if (!bounds.IsEmpty)
                {
                    bounds = new Rect(bounds.X + GlyphRun.BaselineOrigin.X,
                                      bounds.Y + GlyphRun.BaselineOrigin.Y,
                                      bounds.Width,
                                      bounds.Height);

                    bounds = Utility.TransformRect(bounds, Transform);
                }
            }

            return bounds;
        }

        #endregion
    } // end of class GlyphPrimitive

    internal class ImagePrimitive : Primitive
    {
        #region Private Fields

        private ImageProxy _image;

        private Rect _destRect;

        #endregion

        #region Public Properties

        public ImageProxy Image
        {
            get
            {
                return _image;
            }
            set
            {
                _image = value;
            }
        }

        public Rect DstRect
        {
            get
            {
                return _destRect;
            }
            set
            {
                _destRect = value;
            }
        }

        #endregion

        #region Private Methods

        private void AbsorbOpacity()
        {
            Image.PushOpacity(Opacity, OpacityMask, DstRect, Transform);

            Opacity = 1;
            OpacityMask = null;
        }

        #endregion

        #region Primitive Members

        public override void OnRender(DrawingContext dc)
        {
            if (Image != null)
            {
                int level;

                level = PushAll(dc);

                dc.DrawImage(Image.GetImage(), DstRect);

                PopAll(dc, level);
            }
        }

        public override Geometry GetShapeGeometry()
        {
            return Utility.TransformGeometry(new RectangleGeometry(DstRect), Transform);
        }

        public override void Exclude(Geometry g)
        {
            if (Image != null)
            {
                if (Clip == null)
                {
                    Clip = Utility.TransformGeometry(new RectangleGeometry(DstRect), Transform);
                }

                Clip = Utility.Exclude(Clip, g, Matrix.Identity);

                if (Clip == null) // nothing is visible
                {
                    Image = null; // nothing to draw
                }
            }
        }

        public override BrushProxy BlendBrush(BrushProxy brush)
        {
            Debug.Assert(false, "Image over Brush?");
            
            return brush;
        }

        public override void BlendOverImage(ImageProxy image, Matrix trans)
        {
            if (IsOpaque)
            {
                return;
            }

            ImageBrush brush = new ImageBrush();

            brush.CanBeInheritanceContext = false;              // Opt-out of inheritance
            brush.ImageSource             = Image.GetImage();
            brush.ViewportUnits           = BrushMappingMode.Absolute;
            brush.Viewport                = DstRect;
            brush.Transform               = new MatrixTransform(Transform);

            BrushProxy b = BrushProxy.CreateBrush(brush, DstRect);

            image.BlendUnderBrush(false, b, trans);
        }

        public override Primitive BlendOpacityMaskWithColor(BrushProxy color)
        {
            // blend color into image opacity mask
            ImagePrimitive primitive = (ImagePrimitive)DeepClone();

            Rect imageBounds = new Rect(0, 0, primitive.Image.PixelWidth, primitive.Image.PixelHeight);

            primitive.Image.PushOpacity(1.0, color, imageBounds, primitive.Transform);

            return primitive;
        }

        public override bool IsOpaque
        {
            get
            {
                if (Image == null)
                {
                    return false;
                }

                return Image.IsOpaque();
            }
        }

        public override bool IsTransparent
        {
            get
            {
                if (Image == null)
                {
                    return true;
                }

                if (Utility.IsTransparent(Opacity))
                {
                    return true;
                }

                return false;
            }
        }

        public override bool Optimize()
        {
            AbsorbOpacity();

            if ((Image != null) && (Clip != null))
            {
                Geometry dest = Utility.TransformGeometry(new RectangleGeometry(DstRect), Transform);

                if (Utility.Covers(Clip, dest)) // If dest is inside clipping region, ignore clipping
                {
                    Clip = null;
                }
            }

            return Image != null;
        }

        public override void PushOpacity(double opacity, BrushProxy opacityMask)
        {
            OpacityMask = BrushProxy.BlendBrush(OpacityMask, opacityMask);

            ExtractOpacity();

            Opacity *= opacity;
        }

        protected override void CloneMembers()
        {
            base.CloneMembers();

            if (_image != null)
            {
                _image = _image.Clone();
            }
        }

        protected override Rect GetBoundsCore()
        {
            return Utility.TransformRect(DstRect, Transform);
        }

        protected override double GetBaseDrawingCost(Matrix worldTransform)
        {
            Rect bounds = GetRectBounds(true);
            bounds.Transform(worldTransform);

            return Configuration.RasterizationCost(bounds.Width, bounds.Height);
        }

        #endregion
    } // end of class ImagePrimitive

    /// <summary>
    /// CanvasPrimitive
    /// </summary>
    internal class CanvasPrimitive : Primitive
    {
        #region Private Fields

        private ArrayList _children;

        #endregion

        #region Constructors

        public CanvasPrimitive() : base()
        {
            _children = new ArrayList();
        }

        #endregion

        #region Public Properties

        public ArrayList Children
        {
            get
            {
                return _children;
            }
        }

        #endregion

        #region Primitive Members

        public override void OnRender(DrawingContext dc)
        {
            int level;

            level = PushAll(dc);

            foreach (Primitive p in Children)
            {
                p.OnRender(dc);
            }

            PopAll(dc, level);
        }

        public override Geometry GetShapeGeometry()
        {
            Debug.Assert(false, "GetShapeGeometry on Canvas");
            return null;
        }

        public override void Exclude(Geometry g)
        {
            Debug.Assert(false, "Exclude on Canvas");
        }

        public override BrushProxy BlendBrush(BrushProxy brush)
        {
            Debug.Assert(false, "BlendBrush on Canvas");

            return brush;
        }

        public override void BlendOverImage(ImageProxy image, Matrix trans)
        {
            Debug.Assert(false, "BlendOverImage on Canvas");
        }

        public override Primitive BlendOpacityMaskWithColor(BrushProxy color)
        {
            CanvasPrimitive c = this.Clone() as CanvasPrimitive;

            c._children = new ArrayList();

            foreach (Primitive p in Children)
            {
                c.Children.Add(p.BlendOpacityMaskWithColor(color));
            }

            return c;
        }

        public override bool IsOpaque
        {
            get
            {
                return false;
            }
        }

        public override bool IsTransparent
        {
            get
            {
                if (Utility.IsTransparent(Opacity))
                {
                    return true;
                }

                return false;
            }
        }

        protected override void CloneMembers()
        {
            base.CloneMembers();

            _children = (ArrayList)_children.Clone();

            for (int index = 0; index < _children.Count; index++)
            {
                _children[index] = ((Primitive)_children[index]).DeepClone();
            }
        }

        protected override Rect GetBoundsCore()
        {
            Rect bounds = new Rect();

            bool first = true;

            foreach (Primitive p in Children)
            {
                Rect r = p.GetRectBounds(true);

                if (first)
                {
                    bounds = r;
                    first = false;
                }
                else
                {
                    bounds.Union(r);
                }
            }

            return Utility.TransformRect(bounds, Transform);
        }

        protected override double GetBaseDrawingCost(Matrix worldTransform)
        {
            double cost = 1024;

            foreach (Primitive primitive in _children)
            {
                cost += primitive.GetDrawingCost(worldTransform);
            }

            return cost;
        }

        #endregion
    } // end of class CanvasPrimitive

    /// <summary>
    /// Primitive related information during flattening
    /// </summary>
    internal class PrimitiveInfo       // >= 44 bytes
    {
#if DEBUG
        public string id;              //  4 bytes
#endif

        public Primitive primitive;                 //  4 bytes
        public Rect      bounds;                    // 32 bytes
        public List<int> overlap;                   //  4 + N bytes, object on top of current object
        public List<int> underlay;                  //  4 + N bytes, object under current object
        public int       overlapHasTransparency;    //  4 bytes
        public Cluster   m_cluster;
    
#if UNIT_TEST
        public PrimitiveInfo(Rect b)
        {
            bounds = b;
        }
#endif

        public PrimitiveInfo(Primitive p)
        {
            primitive   = p;
            bounds      = p.GetRectBounds(true);
        }

#if DEBUG
        internal void SetID(int i)
        {    
            id  = i.ToString(CultureInfo.InvariantCulture);
        }
#endif

        /// <summary>
        /// Gets approximate primitive bounds with clipping applied.
        /// </summary>
        public Rect GetClippedBounds()
        {
            Rect bounds = this.bounds;

            if (primitive.Clip != null)
            {
                bounds = Rect.Intersect(bounds, primitive.Clip.Bounds);
            }

            return bounds;
        }

        /// <summary>
        /// Current primitive is underneath p. Return true if current primitive fully contains p in shape and color
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public bool FullyCovers(PrimitiveInfo p)
        {
            if (bounds.Contains(p.bounds))
            {
                GeometryPrimitive gp = primitive as GeometryPrimitive;

                if ((gp != null) && (gp.Brush != null) && (gp.Pen == null))
                {
                    BrushProxy bp = gp.Brush;

                    if (!(bp.Brush is TileBrush))
                    {
                        Geometry pshape = p.primitive.GetShapeGeometry();

                        if (primitive.Clip != null)
                        {
                            if (!Utility.FullyCovers(primitive.Clip, pshape))
                            {
                                return false;
                            }
                        }

                        return Utility.FullyCovers(primitive.GetShapeGeometry(), pshape);
                    }
                }
            }

            return false;
        }

    } // end of class PrimitiveInfo

} // end of namespace
