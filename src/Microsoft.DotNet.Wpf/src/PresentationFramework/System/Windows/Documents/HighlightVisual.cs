// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Implements the HighlightVisual element for rendering highlight for fixed 
//      document
//

namespace System.Windows.Documents
{
    using MS.Internal;                  // DoubleUtil
    using MS.Internal.Documents;
    using MS.Utility;                   // ExceptionStringTable
    using System.ComponentModel;
    using System.Windows.Threading;             // Dispatcher
    using System.Windows;               // DependencyID etc.
    using System.Windows.Media;         // Visual
    using System.Windows.Shapes;        // Glyphs
    using System;
    using System.Collections;
    using System.IO;
    using System.Diagnostics;
        

    internal sealed class HighlightVisual : Adorner
    {
        //--------------------------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------------------------

        #region Constructors

        /// <summary>
        ///     HighlightVisual construction
        /// </summary>
        /// <param name="panel">FixedDocument where selection is taking place</param>
        /// <param name="page">FixedPage that is being highlighted</param>
        internal HighlightVisual(FixedDocument panel, FixedPage page) : base(page)
        {
            _panel = panel;
            _page = page;
        }
        #endregion Constructors
        
        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------
        //--------------------------------------------------------------------
        //
        // Public Properties
        //
        //---------------------------------------------------------------------
        //--------------------------------------------------------------------
        //
        // Public Events
        //
        //---------------------------------------------------------------------
        //--------------------------------------------------------------------
        //
        // Protected Methods
        //
        //---------------------------------------------------------------------

        #region Protected Methods
        protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
        {
            return null;
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return null;
        }

        /// <summary>
        /// Draw highlight
        /// </summary>
        override protected void OnRender(DrawingContext dc)
        {
#if DEBUG
            DocumentsTrace.FixedTextOM.Highlight.Trace(string.Format("HightlightVisual Rendering"));
#endif
            if (_panel.Highlights.ContainsKey(_page))
            {
                ArrayList highlights = _panel.Highlights[_page];

                Size pageSize = _panel.ComputePageSize(_page);
                Rect clipRect = new Rect(new Point(0, 0), pageSize);
                dc.PushClip(new RectangleGeometry(clipRect));
                if (highlights != null)
                {
                    _UpdateHighlightBackground(dc, highlights);
                    _UpdateHighlightForeground(dc, highlights);
                }
                dc.Pop(); //clip
            }

            if (_rubberbandSelector != null &&
                _rubberbandSelector.Page == _page)
            {
                Rect r = _rubberbandSelector.SelectionRect;
                if (!r.IsEmpty)
                {
                    dc.DrawRectangle(SelectionHighlightInfo.ObjectMaskBrush, null, r);
                }
            }
        }
        #endregion Protected Methods

        //--------------------------------------------------------------------
        //
        // Internal Methods
        //
        //---------------------------------------------------------------------

        #region Internal Methods

        internal void InvalidateHighlights()
        {
            AdornerLayer al = AdornerLayer.GetAdornerLayer(_page);

            if (al == null)
            {
                return;
            }
		al.Update(_page);
        }

        internal void UpdateRubberbandSelection(RubberbandSelector selector)
        {
            _rubberbandSelector = selector;
            InvalidateHighlights();
        }

        /// <summary>
        /// Finds the HighlightVisual for this page.
        /// </summary>
        internal static HighlightVisual GetHighlightVisual(FixedPage page)
        {
            AdornerLayer al = AdornerLayer.GetAdornerLayer(page);
            HighlightVisual hv;

            if (al == null)
            {
                return null;
            }

            Adorner[] adorners = al.GetAdorners(page);

            if (adorners != null)
            {
                foreach (Adorner ad in adorners)
                {
                    hv = ad as HighlightVisual;
                    if (hv != null)
                    {
                        return hv;
                    }
                }
            }

            return null;
        }
        #endregion Internal Methods

        #region private Methods

        private void _UpdateHighlightBackground(DrawingContext dc, ArrayList highlights)
        {
            Debug.Assert(highlights != null);

            PathGeometry highlightGeometry = null;
            Brush highlightBrush = null;

            Rect combinedRect = Rect.Empty;

            foreach (FixedHighlight fh in highlights)
            {
                Brush bg = null;

                if (fh.HighlightType == FixedHighlightType.None)
                {
#if NEVER
                    // use this code if you want to see unrecognized highlights
                    bg = Brushes.Yellow;
#else
                    continue;
#endif
                }

                Rect backgroundRect = fh.ComputeDesignRect();

                if (backgroundRect == Rect.Empty)
                {
                    continue;
                }

                GeneralTransform transform = fh.Element.TransformToAncestor(_page);
                // This is a workaround. We should really look into changing 
                Transform t = transform.AffineTransform;
                if (t == null)
                {
                    t = Transform.Identity;
                }

                // Inflate by 1 pixel on each side to have a better selection visual for continuous lines
                // backgroundRect.Inflate(1,1);

                Glyphs g = fh.Glyphs;

                if (fh.HighlightType == FixedHighlightType.TextSelection)
                {
                    bg = (g == null) ? SelectionHighlightInfo.ObjectMaskBrush : SelectionHighlightInfo.BackgroundBrush;
                } 
                else if (fh.HighlightType == FixedHighlightType.AnnotationHighlight)
                {
                    bg = fh.BackgroundBrush;
                }

                
                
                // can add cases for new types of highlights
                if (fh.Element.Clip != null)
                {
                    Rect clipRect = fh.Element.Clip.Bounds;
                    backgroundRect.Intersect(clipRect);
                    //thisGeometry = Geometry.Combine(thisGeometry, fh.Element.Clip, GeometryCombineMode.Intersect, t);
                }
                
                Geometry thisGeometry = new RectangleGeometry(backgroundRect);
                thisGeometry.Transform = t;

                backgroundRect = transform.TransformBounds(backgroundRect);


                // used to cut down on calls to Geometry.Combine for complex geometries
                // involving multiple non-intersecting paths

                Debug.Assert(bg != null);
                if (bg != highlightBrush || backgroundRect.Top > combinedRect.Bottom + .1 || backgroundRect.Bottom + .1 < combinedRect.Top
                    || backgroundRect.Left > combinedRect.Right + .1 || backgroundRect.Right + .1 < combinedRect.Left)
                {
                    if (highlightBrush != null)
                    {
                        Debug.Assert(highlightGeometry != null);
                        highlightGeometry.FillRule = FillRule.Nonzero;
                        dc.DrawGeometry(highlightBrush, null, highlightGeometry);
                    }
                    highlightBrush = bg;
                    highlightGeometry = new PathGeometry();
                    highlightGeometry.AddGeometry(thisGeometry);
                    combinedRect = backgroundRect;
                }
                else
                {
                    highlightGeometry.AddGeometry(thisGeometry);
                    combinedRect.Union(backgroundRect);
                }
            }

            if (highlightBrush != null)
            {
                Debug.Assert(highlightGeometry != null);
                highlightGeometry.FillRule = FillRule.Nonzero;
                dc.DrawGeometry(highlightBrush, null, highlightGeometry);
            }
        }

        private void _UpdateHighlightForeground(DrawingContext dc, ArrayList highlights)
        {
            foreach (FixedHighlight fh in highlights)
            {
                Brush fg = null;

                if (fh.HighlightType == FixedHighlightType.None)
                {
#if NEVER
                    // use this code if you want to see unrecognized highlights
                    bg = Brushes.Yellow;
#else
                    continue;
#endif
                }

                Glyphs g = fh.Glyphs;
                if (g == null)
                {
                    continue;
                }


                Rect clipRect = fh.ComputeDesignRect();

                if (clipRect == Rect.Empty)
                {
                    continue;
                }

                GeneralTransform transform = fh.Element.TransformToAncestor(_page);

                Transform t = transform.AffineTransform;
                if (t != null)
                {
                    dc.PushTransform(t);
                }
                else
                {
                    dc.PushTransform(Transform.Identity);
                }

                dc.PushClip(new RectangleGeometry(clipRect));

                if (fh.HighlightType == FixedHighlightType.TextSelection)
                {
                    fg = SelectionHighlightInfo.ForegroundBrush;
                } 
                else if (fh.HighlightType == FixedHighlightType.AnnotationHighlight)
                {
                    fg = fh.ForegroundBrush;
                }
                // can add cases for new types of highlights

                GlyphRun gr = g.ToGlyphRun();

                if (fg == null)
                {
                    fg = g.Fill;
                }

                dc.PushGuidelineY1(gr.BaselineOrigin.Y);
                dc.PushClip(g.Clip);
                dc.DrawGlyphRun(fg, gr);
                dc.Pop(); // Glyphs clip
                dc.Pop(); // Guideline
                dc.Pop(); // clip
                dc.Pop(); // transform
            }
        }

        #endregion


        //--------------------------------------------------------------------
        //
        // private Properties
        //
        //---------------------------------------------------------------------
        //--------------------------------------------------------------------
        //
        // Private Methods
        //
        //---------------------------------------------------------------------
        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------
        private FixedDocument _panel;
        private RubberbandSelector _rubberbandSelector;
        private FixedPage _page;
    }
}
