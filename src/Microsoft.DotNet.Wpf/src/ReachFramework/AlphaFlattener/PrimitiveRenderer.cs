// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Diagnostics;
using System.Collections;              // for ArrayList
using System.Windows;                  // for Rect                        WindowsBase.dll
using System.Windows.Media;            // for Geometry, Brush, ImageData. PresentationCore.dll
using System.Windows.Media.Imaging;    // for BitmapSource
using System.Collections.Generic;

namespace Microsoft.Internal.AlphaFlattener
{
    /// <summary>
    /// Render a primitive to IProxyDrawingContext
    /// </summary>
    internal class PrimitiveRenderer
    {
        #region Public Methods

        // External API
        public void RenderImage(ImageProxy image, Rect dest, Geometry clip, Matrix trans, string desp)
        {
            if (image == null)
            {
                return;
            }

            Geometry bounds;
            bool clipToBounds;

            if (clip == null)
            {
                // no clipping needed, draw everything
                bounds = Utility.TransformGeometry(new RectangleGeometry(dest), trans);
                clipToBounds = false;
            }
            else
            {
                // clip to provided geometry. it's already in world space
                bounds = clip;
                clipToBounds = true;
            }

            RenderImage(image, dest, bounds, clipToBounds, 0, trans, desp);
        }

        // External API
        public void DrawGeometry(Geometry cur, string desp, GeometryPrimitive gp)
        {
            if (cur == null)
            {
                return;
            }

            int start = 0;

            PrimitiveInfo topPI;
            Geometry topBounds;
            Geometry inter;

            if (_pen != null)
            {
                Debug.Assert(_brush == null, "no brush");

                if ((_overlapping != null) && FindIntersection(gp.WidenGeometry, ref start, out topPI, out topBounds, out inter))
                {
                    cur    = gp.WidenGeometry;
                    _brush = _pen.StrokeBrush;
                    _pen   = null;

                    // Draw the stroking as filling widened path
#if DEBUG
                    FillGeometry(topPI, cur, desp + "_widen", null, null, start, inter, topBounds);
#else
                    FillGeometry(topPI, cur, null, null, null, start, inter, topBounds);
#endif
                }
                else
                {
                    // Render to dc if nothing on top
                    _dc.Comment(desp);
                    _dc.DrawGeometry(_brush, _pen, cur, _clip, Matrix.Identity, ProxyDrawingFlags.None);
                }
            }
            else
            {
                if (FindIntersection(cur, ref start, out topPI, out topBounds, out inter))
                {
                    FillGeometry(topPI, cur, desp, null, null, start, inter, topBounds);
                }
                else
                {
                    // Render to dc if nothing on top
                    _dc.Comment(desp);
                    _dc.DrawGeometry(_brush, _pen, cur, _clip, Matrix.Identity, ProxyDrawingFlags.None);
                }
            }
        }

        // External API
        public bool DrawGlyphs(GlyphRun glyphrun, Rect bounds, Matrix trans, string desp)
        {
            if (glyphrun == null)
            {
                return true;
            }

            int start = 0;

            PrimitiveInfo topPI;
            Geometry topBounds;
            Geometry inter;

            // If glyph has intersection with something on the top, change to geometry fill
            // Use bounding rectangle to test for overlapping first, avoinding expensive BuildGeometry call
            if ((_overlapping != null) && FindIntersection(new RectangleGeometry(bounds), ref start, out topPI, out topBounds, out inter))
            {
                start = 0;

                Geometry cur = glyphrun.BuildGeometry();

                cur = Utility.TransformGeometry(cur, trans);

                if (FindIntersection(cur, ref start, out topPI, out topBounds, out inter))
                {
                    // FillGeometry expects brush in world space. Apply trans to brush.
                    if (_brush != null)
                    {
                        _brush = _brush.ApplyTransformCopy(trans);
                    }

                    FillGeometry(topPI, cur, desp, null, null, start, inter, topBounds);

                    return true;
                }
            }

            _dc.Comment(desp);
            
            return _dc.DrawGlyphs(glyphrun, _clip, trans, _brush);
        }

        #endregion

        #region Public Properties

        public Geometry Clip
        {
            set { _clip = value; }
        }

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
        
        public List<int> Overlapping
        {
            set { _overlapping = value; }
        }

        public List<PrimitiveInfo> Commands
        {
            set { _commands = value; }
        }

        public IProxyDrawingContext DC
        {
            set { _dc = value; }
        }

        public bool Disjoint
        {
            set { _disjoint = value; }
        }

        #endregion

        #region Private Static Methods

        private static Matrix ReverseMap(Matrix trans, Rect dest, double width, double height)
        {
            // Render the intersection using blended image
            Matrix mat = Matrix.Identity;

            // Transformation from source rect to destination
            mat.Scale(dest.Width / width, dest.Height / height);
            mat.Translate(dest.Left, dest.Top);

            // Transformation from source rect to canvas coordinate space
            mat.Append(trans);

            // from canvas to source rect
            mat.Invert();

            return mat;
        }

#if DEBUG
        private static string Oper(string t1, char op, string t2)
        {
            char[] opers = new char[2];

            opers[0] = '-';
            opers[1] = '*';

            if ((op != '-') && t1.IndexOfAny(opers) >= 0)
            {
                t1 = "(" + t1 + ")";
            }

            if (t2.IndexOfAny(opers) >= 0)
            {
                t2 = "(" + t2 + ")";
            }

            return t1 + op + t2;
        }
#endif

        #endregion

        #region Private Methods

        private void RenderImage(
            ImageProxy image, 
            Rect       dest, 
            Geometry   bounds, 
            bool       clipToBounds, 
            int        start, 
            Matrix     trans,
            string     desp
            )
        {
            PrimitiveInfo topPI;
            Geometry topBounds;
            Geometry inter;

            if (FindIntersection(bounds, ref start, out topPI, out topBounds, out inter))
            {
                Primitive p = topPI.primitive;
                Geometry diff = Utility.Exclude(bounds, topBounds, trans);

                // DrawImage may modify image
                ImageProxy imageBlend = new ImageProxy(image.GetImage());

                if (diff != null)
                {
                    // Render cur - top
#if DEBUG
                    RenderImage(image, dest, diff, true, start + 1, trans, Oper(desp, '-', topPI.id));
#else
                    RenderImage(image, dest, diff, true, start + 1, trans, null);
#endif
                }

                if (!p.IsTransparent)
                {
#if DEBUG
                    topPI.id = Oper(topPI.id, '-', Oper(desp, '.', "bounds"));
#endif

                    // Render the intersection using blended image
                    p.BlendOverImage(imageBlend, ReverseMap(trans, dest, imageBlend.PixelWidth, imageBlend.PixelHeight));

#if DEBUG
                    RenderImage(imageBlend, dest, inter, true, start + 1, trans, Oper(desp, '*', topPI.id));
#else
                    RenderImage(imageBlend, dest, inter, true, start + 1, trans, null);
#endif
                }

                p.Exclude(bounds);
            }
            else
            {
                Geometry clip = _clip;

                bool empty = false;

                if (clipToBounds)
                {
                    clip = Utility.Intersect(clip, bounds, Matrix.Identity, out empty);
                }

                if (!empty)
                {
                    _dc.Comment(desp);
                    _dc.DrawImage(image, dest, clip, trans);
                }
            }
        }


        // Find the next Primitive having intersection with cur in overlapping list
        private bool FindIntersection(Geometry cur, ref int start, out PrimitiveInfo topPI, out Geometry topBounds, out Geometry inter)
        {
            topPI = null;
            topBounds = null;
            inter = null;

            if (_overlapping == null)
            {
                return false;
            }

            // If not in a subtree which needs composition, igore overlapping list if all are opaque
            if (!_disjoint)
            {
                bool allopaque = true;

                for (int s = start; s < _overlapping.Count; s++)
                {
                    PrimitiveInfo pi = _commands[_overlapping[s]] as PrimitiveInfo;

                    if ((pi != null) && !pi.primitive.IsTransparent && !pi.primitive.IsOpaque)
                    {
                        allopaque = false;
                        break;
                    }
                }

                if (allopaque)
                {
                    return false;
                }
            }

            // Search for all possible intersections
            while (start < _overlapping.Count)
            {
                topPI = _commands[_overlapping[start]] as PrimitiveInfo;

                if (!topPI.primitive.IsTransparent) // Skip primitives with nothing to draw
                {
                    topBounds = topPI.primitive.GetClippedShapeGeometry();

                    if (topBounds != null)
                    {
                        bool empty;

                        inter = Utility.Intersect(cur, topBounds, Matrix.Identity, out empty);

                        if (inter != null)
                        {
                            return true;
                        }
                    }
                }

                start++;
            }

            return false;
        }


        // Recursive
        // _brush must be in world space
        private void FillGeometry(Geometry cur, string desp, Geometry curAlt, string despAlt, int start)
        {
            PrimitiveInfo pi;
            Geometry top;
            Geometry inter;

            if (FindIntersection(cur, ref start, out pi, out top, out inter))
            {
                FillGeometry(pi, cur, desp, curAlt, despAlt, start, inter, top);
            }
            else
            {
                if (curAlt != null)
                {
                    cur  = curAlt;
                    desp = despAlt;
                }

                // Render to dc if nothing on top
                _dc.Comment(desp);
                _dc.DrawGeometry(_brush, _pen, cur, _clip, Matrix.Identity, ProxyDrawingFlags.None);
            }
        }


        // Recursive
        // _brush must be in world space
        private void FillGeometry(
            PrimitiveInfo topPI, 
            Geometry cur, 
            string desp, 
            Geometry curAlt, 
            string despAlt, 
            int start, 
            Geometry inter, 
            Geometry topBounds
            )
        {
            Primitive p = topPI.primitive;
            Geometry diff = Utility.Exclude(cur, topBounds, Matrix.Identity);

            if (diff != null)
            {
                // Render cur [- topBounds] using original brush

                if (_disjoint)
                {
#if DEBUG
                    FillGeometry(diff, Oper(desp, '-', topPI.id), null, null, start + 1);
#else
                    FillGeometry(diff, null, null, null, start + 1);
#endif
                }
                else
                {
                    // Only diff = cur - topBounds need to be rendered. But it may generate more
                    // complicated path and gaps between objects

                    if (curAlt != null)
                    {
#if DEBUG
                        FillGeometry(diff, Oper(desp, '-', topPI.id), curAlt, despAlt, start + 1);
#else
                        FillGeometry(diff, null, curAlt, despAlt, start + 1);
#endif
                    }
                    else
                    {
#if DEBUG
                        FillGeometry(diff, Oper(desp, '-', topPI.id), cur, desp, start + 1);
#else
                        FillGeometry(diff, null, cur, desp, start + 1);
#endif
                    }
                }
            }

            //if (_disjoint || ! p.IsOpaque)
            {
                if (topPI.primitive is ImagePrimitive)
                {
                    // If primitve on the top is ImagePrimitive, change it to DrawImage with blended image.
                    // An alternative will be generating an image brush

                    ImagePrimitive ip = topPI.primitive as ImagePrimitive;

                    bool empty;

                    double imageWidth = ip.Image.Image.Width;
                    double imageHeight = ip.Image.Image.Height;

                    // Get clip in world space.
                    Geometry clip = Utility.Intersect(inter, Utility.TransformGeometry(new RectangleGeometry(ip.DstRect), ip.Transform), ip.Transform, out empty);

                    if (!empty)
                    {
                        // Get clip bounds in image space.
                        Geometry clipImageSpace = Utility.TransformGeometry(clip, ReverseMap(ip.Transform, ip.DstRect, imageWidth, imageHeight));
                        Rect drawBounds = clipImageSpace.Bounds;

                        // Clip image data to the intersection. Resulting draw bounds are in image space.
                        BitmapSource clippedImage = ip.Image.GetClippedImage(drawBounds, out drawBounds);
                        if (clippedImage != null)
                        {
                            // Transform draw bounds back to world space.
                            drawBounds.Scale(ip.DstRect.Width / imageWidth, ip.DstRect.Height / imageHeight);
                            drawBounds.Offset(ip.DstRect.Left, ip.DstRect.Top);

                            ImageProxy image = new ImageProxy(clippedImage);

                            // Blend image with other brush, then render composited image.
                            image.BlendOverBrush(false, _brush, ReverseMap(ip.Transform, drawBounds, image.PixelWidth, image.PixelHeight));

#if DEBUG
                            RenderImage(image, drawBounds, clip, true, start + 1, ip.Transform, Oper(desp, '*', topPI.id));
#else
                            RenderImage(image, drawBounds, clip, true, start + 1, ip.Transform, null);
#endif
                        }
                    }

                }
                else
                {
                    // -- If top primitive opaque, skip the intersection
                    // -- If current primitive is completely covered by an opaque object, skip the intersection
                    if (p.IsOpaque) // && Utility.Covers(topBounds, cur))
                    {
                        cur = null;
                    }
                    else
                    {
                        // Render the intersection using blended brush
                        BrushProxy oldbrush = _brush;

                        _brush = p.BlendBrush(_brush);

#if DEBUG
                        FillGeometry(inter, Oper(desp, '*', topPI.id), null, null, start + 1);
#else
                        FillGeometry(inter, null, null, null, start + 1);
#endif

                        _brush = oldbrush;
                    }
                }

                if (cur != null)
                {
                    bool empty;

                    Geometry geo = Utility.Intersect(cur, _clip, Matrix.Identity, out empty);

                    if (geo != null)
                    {
                        topPI.primitive.Exclude(geo); // exclude cur & _clip

#if DEBUG
                        topPI.id = Oper(topPI.id, '-', Oper(desp, '*', Oper(desp, '.', "c")));
#endif
                    }
                }
            }
        }

        #endregion

        #region Private Fields

        private Geometry             _clip;
        private BrushProxy           _brush;    // primitive brush, possibly in world space
        private PenProxy             _pen;
        private List<int>            _overlapping;
        private List<PrimitiveInfo>  _commands;
        private IProxyDrawingContext _dc;
        private bool                 _disjoint;

        #endregion
    } // end of class PrimitiveRenderer

} // end of namespace
