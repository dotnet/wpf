// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
    Description:
        This class is responsible for algorithmically reconstructing a semantic object model (SOM)
        for each page on the document
--*/

namespace System.Windows.Documents
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Windows.Shapes;
    using System.Windows.Controls;
    using System.Diagnostics;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Globalization;

    #region GeometryAnalyzer

    /// <summary>
    /// Walk a StreamGeometry to find line shapes for table recognition, without expensive conversion to PathGeometry
    ///    For filling,  check bounding box of each figure, abort on curves
    ///    For stroking, check straight lines, ignore curves
    ///
    /// Calling sequence from PathGeometry.ParsePathGeometryData
    ///     SetFigureCount
    ///         BeginFigure
    ///         SetSegmentCount
    ///         LineTo | BezierTo | ...
    /// </summary>
    internal sealed class GeometryWalker : CapacityStreamGeometryContext
    {
        private FixedSOMPageConstructor _pageConstructor;

        private Matrix    _transform;                   // Transformation from page root
        private bool      _stroke;                      // Path has stroke brush
        private bool      _fill;                        // Path has fill brush

        private Point     _startPoint;                  // Start point for current figure
        private Point     _lastPoint;                   // Current end point for current figure
        private bool      _isClosed;                    // Is current figure closed?
        private bool      _isFilled;                    // Is current figure filled?

        private double    _xMin, _xMax, _yMin, _yMax;   // Bounding box for current figure, only needed when (_fill && _isFilled)

        private bool      _needClose;                   // Need to check for closing current/last figure

        public GeometryWalker(FixedSOMPageConstructor pageConstructor)
        {
            _pageConstructor = pageConstructor;
        }

        public void FindLines(StreamGeometry geometry, bool stroke, bool fill, Matrix trans)
        {
            Debug.Assert(stroke || fill, "should not be a nop");

            _transform = trans;
            _fill      = fill;
            _stroke    = stroke;

            PathGeometry.ParsePathGeometryData(geometry.GetPathGeometryData(), this);

            CheckCloseFigure();
        }

        private void CheckCloseFigure()
        {
            if (_needClose)
            {
                if (_stroke && _isClosed)
                {
                    _pageConstructor._AddLine(_startPoint, _lastPoint, _transform);
                }

                if (_fill && _isFilled)
                {
                    _pageConstructor._ProcessFilledRect(_transform, new Rect(_xMin, _yMin, _xMax - _xMin, _yMax - _yMin));
                }

                _needClose = false;
            }
        }

        private void GatherBounds(Point p)
        {
            if (p.X < _xMin)
            {
                _xMin = p.X;
            }
            else if (p.X > _xMax)
            {
                _xMax = p.X;
            }

            if (p.Y < _yMin)
            {
                _yMin = p.Y;
            }
            else if (p.Y > _yMax)
            {
                _yMax = p.Y;
            }
        }


        // CapacityStreamGeometryContext Members
        public override void BeginFigure(Point startPoint, bool isFilled, bool isClosed)
        {
            CheckCloseFigure();

            _startPoint = startPoint;
            _lastPoint  = startPoint;
            _isClosed   = isClosed;
            _isFilled   = isFilled;

            if (_isFilled && _fill)
            {
                _xMin = _xMax = startPoint.X;
                _yMin = _yMax = startPoint.Y;
            }
        }

        public override void LineTo(Point point, bool isStroked, bool isSmoothJoin)
        {
            if (isStroked && _stroke)
            {
                _pageConstructor._AddLine(_lastPoint, point, _transform);
            }

            if (_isFilled && _fill)
            {
                GatherBounds(point);
            }

            _lastPoint = point;
        }

        public override void QuadraticBezierTo(Point point1, Point point2, bool isStroked, bool isSmoothJoin)
        {
            _lastPoint = point2;
            _fill      = false;
        }

        public override void BezierTo(Point point1, Point point2, Point point3, bool isStroked, bool isSmoothJoin)
        {
            _lastPoint = point3;
            _fill      = false;
        }

        public override void PolyLineTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
        {
            if (isStroked && _stroke)
            {
                for (int i = 0; i <points.Count; i ++)
                {
                    _pageConstructor._AddLine(_lastPoint, points[i], _transform);
                    _lastPoint = points[i];
                }
            }
            else
            {
                _lastPoint = points[points.Count - 1];
            }

            if (_isFilled && _fill)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    GatherBounds(points[i]);
                }
            }
        }

        public override void PolyQuadraticBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
        {
            _lastPoint = points[points.Count - 1];
            _fill      = false;
        }

        public override void PolyBezierTo(IList<Point> points, bool isStroked, bool isSmoothJoin)
        {
            _lastPoint = points[points.Count - 1];
            _fill      = false;
        }

        public override void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked, bool isSmoothJoin)
        {
            _lastPoint = point;
            _fill      = false;
        }

        internal override void SetClosedState(bool closed)
        {
            Debug.Assert(false, "It should not be called");
        }

        internal override void SetFigureCount(int figureCount)
        {
        }

        internal override void SetSegmentCount(int segmentCount)
        {
            if (segmentCount != 0)
            {
                _needClose = true;
            }
        }
    }

    #endregion

    internal sealed class FixedSOMPageConstructor
    {
        //--------------------------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------------------------

        #region Constructors
        public FixedSOMPageConstructor(FixedPage fixedPage, int pageIndex)
        {
            Debug.Assert(fixedPage != null);
            _fixedPage = fixedPage;
            _pageIndex = pageIndex;
            _fixedSOMPage  = new FixedSOMPage();
            _fixedSOMPage.CultureInfo = _fixedPage.Language.GetCompatibleCulture();
            _fixedNodes = new List<FixedNode>();
            _lines = new FixedSOMLineCollection();
        }
        #endregion Constructors

        #region Public methods

        public FixedSOMPage ConstructPageStructure(List<FixedNode> fixedNodes)
        {
            Debug.Assert(_fixedPage != null);

            foreach (FixedNode node in fixedNodes)
            {
                DependencyObject obj = _fixedPage.GetElement(node);
                Debug.Assert(obj != null);

                if (obj is Glyphs)
                {
                    _ProcessGlyphsElement(obj as Glyphs, node);
                }
                else if (obj is Image ||
                         obj is Path && ((obj as Path).Fill is ImageBrush))
                {
                    _ProcessImage(obj, node);
                }
            }



            //Inner sorting of all page elements

            foreach (FixedSOMSemanticBox box in _fixedSOMPage.SemanticBoxes)
            {
                FixedSOMContainer container = box as FixedSOMContainer;
                container.SemanticBoxes.Sort();
            }


            _DetectTables();
            _CombinePass();
            _CreateGroups(_fixedSOMPage);

            _fixedSOMPage.SemanticBoxes.Sort();

            return _fixedSOMPage;
        }


        public void ProcessPath(Path path, Matrix transform)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            Geometry geom   = path.Data;
            bool     fill   = path.Fill != null;
            bool     stroke = path.Stroke != null;

            if ((geom == null) || (! fill && ! stroke))
            {
                return;
            }

            Transform transPath = path.RenderTransform;

            if (transPath != null)
            {
                transform *= transPath.Value;
            }

            // When filling, we may be able to determine from bounding box only
            if (fill && _ProcessFilledRect(transform, geom.Bounds))
            {
                fill = false;

                if (! stroke)
                {
                    return;
                }
            }

            StreamGeometry sgeo = geom as StreamGeometry;

            // Avoiding convert to PathGeometry if it's StreamGeometry, which can be walked

            if (sgeo != null)
            {
                if (_geometryWalker == null)
                {
                    _geometryWalker = new GeometryWalker(this);
                }

                _geometryWalker.FindLines(sgeo, stroke, fill, transform);
            }
            else
            {
                PathGeometry pathGeom = PathGeometry.CreateFromGeometry(geom);

                if (pathGeom != null)
                {
                    if (fill)
                    {
                        _ProcessSolidPath(transform, pathGeom);
                    }

                    if (stroke)
                    {
                        _ProcessOutlinePath(transform, pathGeom);
                    }
                }
            }
        }

        #endregion Public methods

        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------

        #region Public Properties

        public FixedSOMPage FixedSOMPage
        {
            get
            {
                return _fixedSOMPage;
            }
        }
        #endregion Public Properties


        //--------------------------------------------------------------------
        //
        // Private Methods
        //
        //---------------------------------------------------------------------


        #region Private methods

        private void _ProcessImage(DependencyObject obj, FixedNode fixedNode)
        {
            Debug.Assert(obj is Image || obj is Path);
            FixedSOMImage somImage = null;

            while (true)
            {
                Image image = obj as Image;
                if (image != null)
                {
                    somImage = FixedSOMImage.Create(_fixedPage, image, fixedNode);
                    break;
                }
                Path path = obj as Path;
                if (path != null)
                {
                    somImage = FixedSOMImage.Create(_fixedPage, path, fixedNode);
                    break;
                }
            }
            //Create a wrapper FixedBlock:

            FixedSOMFixedBlock fixedBlock = new FixedSOMFixedBlock(_fixedSOMPage);
            fixedBlock.AddImage(somImage);
            _fixedSOMPage.AddFixedBlock(fixedBlock);
            _currentFixedBlock = fixedBlock;
        }

        //Processes the Glyphs element, create one or more text runs out of it, add to containing text line and text box
        private void _ProcessGlyphsElement(Glyphs glyphs, FixedNode node)
        {
            Debug.Assert(glyphs != null);
            string s = glyphs.UnicodeString;
            if (s.Length == 0 ||
                glyphs.FontRenderingEmSize <= 0)
            {
                return;
            }

           //Multiple table cells separated by wide spaces should be identified
            GlyphRun glyphRun = glyphs.ToGlyphRun();

            if (glyphRun == null)
            {
                //Could not create a GlyphRun out of this Glyphs element
                //Some key properties might be missing/invalid
                return;
            }

            Rect alignmentBox = glyphRun.ComputeAlignmentBox();
            alignmentBox.Offset(glyphs.OriginX, glyphs.OriginY);
            GlyphTypeface typeFace = glyphRun.GlyphTypeface;
            GeneralTransform trans = glyphs.TransformToAncestor(_fixedPage);

            int charIndex= -1;
            double advWidth = 0;
            double cumulativeAdvWidth = 0;
            int lastAdvWidthIndex = 0;
            int textRunStartIndex = 0;
            double lastX = alignmentBox.Left;
            int glyphIndex = charIndex;
            do
            {
                charIndex = s.IndexOf(" ", charIndex+1, s.Length - charIndex -1, StringComparison.Ordinal);
                if (charIndex >=0 )
                {
                    if (glyphRun.ClusterMap != null && glyphRun.ClusterMap.Count > 0)
                    {
                        glyphIndex = glyphRun.ClusterMap[charIndex];
                    }
                    else
                    {
                        glyphIndex = charIndex;
                    }
                    //Advance width of the space character in the font
                    double advFont = typeFace.AdvanceWidths[glyphRun.GlyphIndices[glyphIndex]] * glyphRun.FontRenderingEmSize;
                    double advSpecified = glyphRun.AdvanceWidths[glyphIndex];

                    if ((advSpecified / advFont) > 2)
                    {
                        //Are these seperated by a vertical line?
                        advWidth = 0;
                        for (int i=lastAdvWidthIndex; i<glyphIndex; i++)
                        {
                            advWidth += glyphRun.AdvanceWidths[i];
                        }
                        cumulativeAdvWidth += advWidth;
                        lastAdvWidthIndex = glyphIndex + 1;


                        if (_lines.IsVerticallySeparated(glyphRun.BaselineOrigin.X + cumulativeAdvWidth,
                                                     alignmentBox.Top,
                                                     glyphRun.BaselineOrigin.X + cumulativeAdvWidth + advSpecified,
                                                     alignmentBox.Bottom))
                        {
                            //Create a new FixedTextRun
                            Rect boundingRect = new Rect(lastX, alignmentBox.Top, advWidth + advFont, alignmentBox.Height);

                            int endIndex = charIndex;

                            if ((charIndex == 0 || s[charIndex-1] == ' ') && (charIndex != s.Length - 1))
                            {
                                endIndex = charIndex + 1;
                            }

                            _CreateTextRun(boundingRect,
                                           trans,
                                           glyphs,
                                           node,
                                           textRunStartIndex,
                                           endIndex);

                            lastX = lastX + advWidth + advSpecified;
                            textRunStartIndex = charIndex+1;
                        }
                        cumulativeAdvWidth += advSpecified;
                    }
                }
            } while (charIndex >= 0 && charIndex < s.Length-1);

            if (textRunStartIndex < s.Length)
            {
                //Last text run
                //For non-partitioned elements this will be the whole Glyphs element
                Rect boundingRect = new Rect(lastX, alignmentBox.Top, alignmentBox.Right-lastX, alignmentBox.Height);

                _CreateTextRun( boundingRect,
                                trans,
                                glyphs,
                                node,
                                textRunStartIndex,
                                s.Length);
            }
        }


        //Creates a FixedSOMTextRun, and updates containing structures
        private void _CreateTextRun(Rect boundingRect, GeneralTransform trans, Glyphs glyphs, FixedNode node, int startIndex, int endIndex)
        {
            if (startIndex < endIndex)
            {
                FixedSOMTextRun textRun = FixedSOMTextRun.Create(boundingRect,
                                                                 trans,
                                                                 glyphs,
                                                                 node,
                                                                 startIndex,
                                                                 endIndex,
                                                                 true);


                FixedSOMFixedBlock fixedBlock = _GetContainingFixedBlock(textRun);

                if (fixedBlock == null)
                {
                    fixedBlock= new FixedSOMFixedBlock(_fixedSOMPage);
                    fixedBlock.AddTextRun(textRun);
                    _fixedSOMPage.AddFixedBlock(fixedBlock);
                 }
                else
                {
                    fixedBlock.AddTextRun(textRun);
                }
                _currentFixedBlock = fixedBlock;
            }
        }


        //Find and return a FixedBlock that would contain this TextRun
        private FixedSOMFixedBlock _GetContainingFixedBlock(FixedSOMTextRun textRun)
        {
            FixedSOMFixedBlock fixedBlock = null;

            if (_currentFixedBlock == null)
            {
                return null;
            }

            if (_currentFixedBlock != null && _IsCombinable(_currentFixedBlock, textRun))
            {
                fixedBlock = _currentFixedBlock;
            }
            else
            {
                //If this is aligned with the previous block, simply create a new block
                Rect textRunRect = textRun.BoundingRect;
                Rect fixedBlockRect = _currentFixedBlock.BoundingRect;
                if (Math.Abs(textRunRect.Left - fixedBlockRect.Left) <= textRun.DefaultCharWidth ||
                    Math.Abs(textRunRect.Right - fixedBlockRect.Right) <= textRun.DefaultCharWidth)
                {
                    return null;
                }
                foreach (FixedSOMSemanticBox box in _fixedSOMPage.SemanticBoxes)
                {
                    if ((box is FixedSOMFixedBlock) && _IsCombinable(box as FixedSOMFixedBlock, textRun))
                    {
                        fixedBlock = box as FixedSOMFixedBlock;
                    }
                }
            }
            return fixedBlock;
        }


        private bool _IsCombinable(FixedSOMFixedBlock fixedBlock, FixedSOMTextRun textRun)
        {
            Debug.Assert (fixedBlock.SemanticBoxes.Count > 0);
            if (fixedBlock.SemanticBoxes.Count == 0)
            {
                return false;
            }

            //Currently we do not support inline images
            if (fixedBlock.IsFloatingImage)
            {
                return false;
            }

            Rect textRunRect = textRun.BoundingRect;
            Rect fixedBlockRect = fixedBlock.BoundingRect;

            FixedSOMTextRun compareLine = null;
            FixedSOMTextRun lastLine = fixedBlock.SemanticBoxes[fixedBlock.SemanticBoxes.Count - 1] as FixedSOMTextRun;

            if (lastLine != null && textRunRect.Bottom <= lastLine.BoundingRect.Top)
            {
                //This run is above the last run of the fixed block. Can't be the same paragraph
                return false;
            }

            bool fixedBlockBelow = false;
            bool textRunBelow = false;
            //Allow 20% overlap
            double verticalOverlap = textRunRect.Height * 0.2;
            if (textRunRect.Bottom - verticalOverlap < fixedBlockRect.Top)
            {
                fixedBlockBelow = true;
                compareLine = fixedBlock.SemanticBoxes[0] as FixedSOMTextRun;
            }
            else if (textRunRect.Top + verticalOverlap > fixedBlockRect.Bottom)
            {
                textRunBelow = true;
                compareLine = fixedBlock.SemanticBoxes[fixedBlock.SemanticBoxes.Count-1] as FixedSOMTextRun;
            }

            if ( (fixedBlock.IsWhiteSpace || textRun.IsWhiteSpace) &&
                 (fixedBlock != _currentFixedBlock || compareLine != null || !_IsSpatiallyCombinable(fixedBlockRect, textRunRect, textRun.DefaultCharWidth * 3, 0))
                 )
            {
                //When combining with white spaces, they need to be consecutive in markup and need to be on the same line.
                return false;
            }
            if (fixedBlock.Matrix.M11 != textRun.Matrix.M11 ||
                fixedBlock.Matrix.M12 != textRun.Matrix.M12 ||
                fixedBlock.Matrix.M21 != textRun.Matrix.M21 ||
                fixedBlock.Matrix.M22 != textRun.Matrix.M22)
            {
                //We don't allow combining TextRuns with different scale/rotation properties
                return false;
            }

            Debug.Assert(textRunRect.Height != 0 && fixedBlock.LineHeight != 0);

            //Rect textRunRect = textRun.BoundingRect;

            if (compareLine != null) //Most probably different lines
            {
                double ratio = fixedBlock.LineHeight / textRunRect.Height;
                if (ratio<1.0)
                {
                    ratio = 1.0 / ratio;
                }
                //Allow 10% height difference
                if ((ratio > 1.1) &&
                    !(FixedTextBuilder.IsSameLine(compareLine.BoundingRect.Top - textRunRect.Top, textRunRect.Height, compareLine.BoundingRect.Height)))
                {
                    return false;
                }
            }

            double width = textRun.DefaultCharWidth;
            if (width < 1.0)
            {
                width = 1.0;
            }

            double dHorInflate = 0;
            double heightRatio = fixedBlock.LineHeight / textRunRect.Height;
            if (heightRatio < 1.0)
            {
                heightRatio = 1.0 / heightRatio;
            }

            //If consecutive in markup and seem to be on the same line, almost discard horizontal distance
            if (fixedBlock == _currentFixedBlock &&
                compareLine == null &&
                heightRatio < 1.5
                )
            {
                dHorInflate = 200;
            }
            else
            {
                dHorInflate = width*1.5;
            }

            if (!_IsSpatiallyCombinable(fixedBlockRect, textRunRect, dHorInflate, textRunRect.Height*0.7))
            {
                return false;
            }

            //If these two have originated from the same Glyphs element, this means we intentionally separated them (separated by vertical lines).
            //Don't combine in this case.
            FixedSOMElement element = fixedBlock.SemanticBoxes[fixedBlock.SemanticBoxes.Count - 1] as FixedSOMElement;

            if (element!=null && element.FixedNode.CompareTo(textRun.FixedNode) == 0)
            {
                return false;
            }

            //Are these seperated by a line? Check only if they are not considered overlapping
            if (fixedBlockBelow || textRunBelow)
            {
                double bottom = 0.0;
                double top = 0.0;
                double margin = textRunRect.Height * 0.2;

                if (textRunBelow)
                {
                    top = fixedBlockRect.Bottom - margin;
                    bottom = textRunRect.Top + margin;
                }
                else
                {
                    top = textRunRect.Bottom - margin ;
                    bottom = fixedBlockRect.Top + margin;
                }
                double left = (fixedBlockRect.Left > textRunRect.Left) ? fixedBlockRect.Left : textRunRect.Left;
                double right = (fixedBlockRect.Right < textRunRect.Right) ? fixedBlockRect.Right: textRunRect.Right;
                return (!_lines.IsHorizontallySeparated(left, top, right, bottom));
            }
            else
            {
                //These two overlap vertically. Let's check whether there is a vertical separator in between
                double left = (fixedBlockRect.Right < textRunRect.Right) ? fixedBlockRect.Right: textRunRect.Right;
                double right =(fixedBlockRect.Left > textRunRect.Left) ? fixedBlockRect.Left: textRunRect.Left;
                if (left < right)
                {
                    return (!_lines.IsVerticallySeparated(left, textRunRect.Top, right, textRunRect.Bottom));
                }
                else
                {
                    // they also overlap horizontally, so they should be combined
                    return true;
                }
            }
        }

        private bool _IsSpatiallyCombinable(FixedSOMSemanticBox box1, FixedSOMSemanticBox box2, double inflateH, double inflateV)
        {
            return _IsSpatiallyCombinable(box1.BoundingRect, box2.BoundingRect, inflateH, inflateV);
        }

        private bool _IsSpatiallyCombinable(Rect rect1, Rect rect2, double inflateH, double inflateV)
        {
            //Do these rects intersect? If so, we can combine
            if (rect1.IntersectsWith(rect2))
            {
                return true;
            }

            //Try inflating
            rect1.Inflate(inflateH, inflateV);
            if (rect1.IntersectsWith(rect2))
            {
                return true;
            }
            return false;
        }


       private void _DetectTables()
       {
           double minLineSeparation = FixedSOMLineRanges.MinLineSeparation;

           List<FixedSOMLineRanges> horizontal = _lines.HorizontalLines;
           List<FixedSOMLineRanges> vertical = _lines.VerticalLines;

           if (horizontal.Count < 2 || vertical.Count < 2)
               return;

           List<FixedSOMTableRow> tableRows = new List<FixedSOMTableRow>();
           FixedSOMTableRow currentRow = null;

            //iterate through
            for (int h = 0; h < horizontal.Count; h++)
            {
                int v = 0;
                int h2 = -1;
                int hSeg2 = -1;
                int hLastCellBottom = -1;
                int vLastCellRight = -1;

                double dropLine = horizontal[h].Line + minLineSeparation;
                //loop through each line segment on this Y value
                for (int hSeg = 0; hSeg < horizontal[h].Count; hSeg++)
                {
                    // X range for this segment -- allow some margin for error
                    double hStart = horizontal[h].Start[hSeg] - minLineSeparation;
                    double hEnd = horizontal[h].End[hSeg] + minLineSeparation;

                    // no cell has been started
                    int vCellStart = -1;
                    while (v < vertical.Count && vertical[v].Line < hStart)
                    {
                        v++;
                    }
                    for (; v < vertical.Count && vertical[v].Line < hEnd; v++)
                    {
                        int vSeg = vertical[v].GetLineAt(dropLine);
                        if (vSeg != -1)
                        {
                            double vBottom = vertical[v].End[vSeg];
                            if (vCellStart != -1 && horizontal[h2].Line < vBottom + minLineSeparation &&
                                horizontal[h2].End[hSeg2] + minLineSeparation > vertical[v].Line)
                            {
                                // should also check if any other lines cut through rectangle?
                                double top = horizontal[h].Line;
                                double bottom = horizontal[h2].Line;
                                double left = vertical[vCellStart].Line;
                                double right = vertical[v].Line;
                                // Create Table Cell
                                FixedSOMTableCell cell = new FixedSOMTableCell(left, top, right, bottom);
                                //_fixedSOMPage.Add(cell); // for now just doing cells

                                // Check if in same row
                                if (vCellStart == vLastCellRight && h2 == hLastCellBottom)
                                {
                                    // same row!
                                    // Assert(currentRow != null);
                                }
                                else
                                {
                                    currentRow = new FixedSOMTableRow();
                                    tableRows.Add(currentRow);
                                }
                                currentRow.AddCell(cell);

                                vLastCellRight = v;
                                hLastCellBottom = h2;
                            }
                            vCellStart = -1; // any previously started cell is not valid

                            // look for cell bottom
                            for (h2 = h + 1; h2 < horizontal.Count && horizontal[h2].Line < vBottom + minLineSeparation; h2++)
                            {
                                hSeg2 = horizontal[h2].GetLineAt(vertical[v].Line + minLineSeparation);
                                if (hSeg2 != -1)
                                {
                                    // start of new cell! (maybe...)
                                    vCellStart = v;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            _FillTables(tableRows);
        }


        public void _AddLine(Point startP, Point endP, Matrix transform)
        {
            startP = transform.Transform(startP);
            endP   = transform.Transform(endP);

            if (startP.X == endP.X)
            {
                _lines.AddVertical(startP, endP);
            }
            else if (startP.Y == endP.Y)
            {
                _lines.AddHorizontal(startP, endP);
            }
        }


        private void _CombinePass()
        {
            if (_fixedSOMPage.SemanticBoxes.Count < 2)
            {
                //Nothing to do
                return;
            }

            int prevBoxCount;
            do
            {
                prevBoxCount = _fixedSOMPage.SemanticBoxes.Count;

                List<FixedSOMSemanticBox> boxes = _fixedSOMPage.SemanticBoxes;
                for (int i = 0; i < boxes.Count; i++)
                {
                    FixedSOMTable table1 = boxes[i] as FixedSOMTable;
                    if (table1 != null)
                    {
                        //Check for nested tables
                        for (int j = i + 1; j < boxes.Count; j++)
                        {
                            FixedSOMTable table2 = boxes[j] as FixedSOMTable;
                            if (table2 != null &&
                                table1.AddContainer(table2))
                            {
                                boxes.Remove(table2);
                            }
                        }
                        continue;
                    }


                    FixedSOMFixedBlock box1 = boxes[i] as FixedSOMFixedBlock;
                    if (box1 == null || box1.IsFloatingImage)
                    {
                        continue;
                    }

                    for (int j = i + 1; j < boxes.Count; j++)
                    {
                        FixedSOMFixedBlock box2 = boxes[j] as FixedSOMFixedBlock;
                        if (box2 != null && !box2.IsFloatingImage &&
                            box2.Matrix.Equals(box1.Matrix) &&
                            (_IsSpatiallyCombinable(box1, box2, 0, 0)))
                        {
                            {
                                box1.CombineWith(box2);
                                boxes.Remove(box2);
                            }
                        }
                    }
                }
            } while (_fixedSOMPage.SemanticBoxes.Count > 1 && _fixedSOMPage.SemanticBoxes.Count != prevBoxCount);
        }

        // Check if a Geometry bound has a line shape
        internal bool _ProcessFilledRect(Matrix transform, Rect bounds)
        {
            const double maxLineWidth = 10;
            const double minLineRatio = 5;

            if (bounds.Height > bounds.Width && bounds.Width < maxLineWidth && bounds.Height > bounds.Width * minLineRatio)
            {
                double center = bounds.Left + .5 * bounds.Width;
                _AddLine(new Point(center, bounds.Top), new Point(center, bounds.Bottom), transform);

                return true;
            }
            else if (bounds.Height < maxLineWidth && bounds.Width > bounds.Height * minLineRatio)
            {
                double center = bounds.Top + .5 * bounds.Height;
                _AddLine(new Point(bounds.Left, center), new Point(bounds.Right, center), transform);

                return true;
            }

            return false;
        }

        // Check if each PathFigure within a PathGeometry has a line shape
        private void _ProcessSolidPath(Matrix transform, PathGeometry pathGeom)
        {
            PathFigureCollection pathFigures = pathGeom.Figures;

            // Single figure should already covered by bounding box check
            if ((pathFigures != null) && (pathFigures.Count > 1))
            {
                foreach (PathFigure pathFigure in pathFigures)
                {
                    PathGeometry pg = new PathGeometry();
                    pg.Figures.Add(pathFigure);

                    _ProcessFilledRect(transform, pg.Bounds);
                }
            }
        }

        // Find all straight lines within a PathGeometry
        private void _ProcessOutlinePath(Matrix transform, PathGeometry pathGeom)
        {
            PathFigureCollection pathFigures = pathGeom.Figures;

            foreach (PathFigure pathFigure in pathFigures)
            {
                PathSegmentCollection pathSegments = pathFigure.Segments;
                Point startPoint = pathFigure.StartPoint;
                Point lastPoint = startPoint;

                foreach (PathSegment pathSegment in pathSegments)
                {
                    if (pathSegment is ArcSegment)
                    {
                        lastPoint = (pathSegment as ArcSegment).Point;
                    }
                    else if (pathSegment is BezierSegment)
                    {
                        lastPoint = (pathSegment as BezierSegment).Point3;
                    }
                    else if (pathSegment is LineSegment)
                    {
                        Point endPoint = (pathSegment as LineSegment).Point;
                        _AddLine(lastPoint, endPoint, transform);
                        lastPoint = endPoint;
                    }
                    else if (pathSegment is PolyBezierSegment)
                    {
                        PointCollection points = (pathSegment as PolyBezierSegment).Points;
                        lastPoint = points[points.Count - 1];
                    }
                    else if (pathSegment is PolyLineSegment)
                    {
                        PointCollection points = (pathSegment as PolyLineSegment).Points;
                        foreach (Point point in points)
                        {
                            _AddLine(lastPoint, point, transform);
                            lastPoint = point;
                        }
                    }
                    else if (pathSegment is PolyQuadraticBezierSegment)
                    {
                        PointCollection points = (pathSegment as PolyQuadraticBezierSegment).Points;
                        lastPoint = points[points.Count - 1];
                    }
                    else if (pathSegment is QuadraticBezierSegment)
                    {
                        lastPoint = (pathSegment as QuadraticBezierSegment).Point2;
                    }
                    else
                    {
                        Debug.Assert(false);
                    }
                }

                if (pathFigure.IsClosed)
                {
                     _AddLine(lastPoint, startPoint, transform);
                }
            }
        }


        private void _FillTables(List<FixedSOMTableRow> tableRows)
        {
            List<FixedSOMTable> tables = new List<FixedSOMTable>();
            foreach (FixedSOMTableRow row in tableRows)
            {
                FixedSOMTable table = null;
                double fudge = 0.01;
                foreach (FixedSOMTable t in tables)
                {
                    if (Math.Abs(t.BoundingRect.Left - row.BoundingRect.Left) < fudge &&
                        Math.Abs(t.BoundingRect.Right - row.BoundingRect.Right) < fudge &&
                        Math.Abs(t.BoundingRect.Bottom - row.BoundingRect.Top) < fudge)
                    {
                        table = t;
                        break;
                    }
                }

                if (table == null)
                {
                    table = new FixedSOMTable(_fixedSOMPage);
                    tables.Add(table);
                }
                table.AddRow(row);
            }

            //Check for nested tables first
            for (int i=0; i<tables.Count-1; i++)
            {
                for (int j=i+1; j<tables.Count; j++)
                {
                    if (tables[i].BoundingRect.Contains(tables[j].BoundingRect) &&
                        tables[i].AddContainer(tables[j]))
                    {
                        tables.RemoveAt(j--);
                    }
                    else if (tables[j].BoundingRect.Contains(tables[i].BoundingRect) &&
                             tables[j].AddContainer(tables[i]))
                    {
                        tables.RemoveAt(i--);
                        if (i < 0)
                        {
                            break;
                        }
                    }
                }
            }

            foreach (FixedSOMTable table in tables)
            {
                if (table.IsSingleCelled)
                {
                    continue;
                }

                bool containsAnything = false;

                for (int i = 0; i < _fixedSOMPage.SemanticBoxes.Count;)
                {
                    //Only FixedBlocks are added to tables at this stage
                    if (_fixedSOMPage.SemanticBoxes[i] is FixedSOMFixedBlock &&
                        table.AddContainer(_fixedSOMPage.SemanticBoxes[i] as FixedSOMContainer))
                    {
                        _fixedSOMPage.SemanticBoxes.RemoveAt(i);
                        containsAnything = true;
                    }
                    else
                    {
                        i++;
                    }
                }

                if (containsAnything)
                {
                    table.DeleteEmptyRows();
                    table.DeleteEmptyColumns();
                    //Remove any internal empty tables
                    //Do grouping and sorting inside cells
                    foreach (FixedSOMTableRow row in table.SemanticBoxes)
                    {
                        foreach (FixedSOMTableCell cell in row.SemanticBoxes)
                        {
                            for (int i=0; i<cell.SemanticBoxes.Count;)
                            {
                                FixedSOMTable innerTable = cell.SemanticBoxes[i] as FixedSOMTable;
                                if (innerTable != null && innerTable.IsEmpty)
                                {
                                    cell.SemanticBoxes.Remove(innerTable);
                                }
                                else
                                {
                                    i++;
                                }
                            }
                            _CreateGroups(cell);
                            cell.SemanticBoxes.Sort();
                        }
                    }
                    _fixedSOMPage.AddTable(table);
                }
            }
        }


        //Creates a set of groups inside this container based on heuristics.
        //This will ensure that elements consecutive in markup that also seem to be
        //spatially consecutive don't get separated
        private void _CreateGroups(FixedSOMContainer container)
        {
            if (container.SemanticBoxes.Count > 0)
            {
                List<FixedSOMSemanticBox> groups = new List<FixedSOMSemanticBox>();

                FixedSOMGroup currentGroup = new FixedSOMGroup(_fixedSOMPage);
                FixedSOMPageElement currentPageElement = container.SemanticBoxes[0] as FixedSOMPageElement;
                Debug.Assert(currentPageElement != null);

                FixedSOMPageElement nextPageElement = null;
                currentGroup.AddContainer(currentPageElement);

                groups.Add(currentGroup);

                for (int i=1; i<container.SemanticBoxes.Count; i++)
                {
                    nextPageElement = container.SemanticBoxes[i] as FixedSOMPageElement;
                    Debug.Assert(nextPageElement != null);

                    if (!( _IsSpatiallyCombinable(currentPageElement, nextPageElement, 0, 30) &&
                         nextPageElement.BoundingRect.Top >= currentPageElement.BoundingRect.Top))
                    {
                        currentGroup = new FixedSOMGroup(_fixedSOMPage);
                        groups.Add(currentGroup);
                    }
                    currentGroup.AddContainer(nextPageElement);
                    currentPageElement = nextPageElement;
                }
                container.SemanticBoxes = groups;
            }
        }



        #endregion Private methods




        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------


        #region Private Fields

        private FixedSOMFixedBlock _currentFixedBlock;
        private int _pageIndex;
        private FixedPage _fixedPage;
        private FixedSOMPage _fixedSOMPage;
        private List<FixedNode> _fixedNodes;
        private FixedSOMLineCollection _lines;
        private GeometryWalker _geometryWalker;
        #endregion Private Fields
    }
}



