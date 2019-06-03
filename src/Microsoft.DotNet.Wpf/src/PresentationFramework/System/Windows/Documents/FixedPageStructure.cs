// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      FixedPageStructure represents deduced information (such as boundary, 
//      geometry,layout, semantic, etc.) after a fixed page is analyzed. 
//

namespace System.Windows.Documents
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using System.Windows.Controls;
    using System.Windows.Markup;
    using System.Windows.Documents.DocumentStructures;
    using MS.Internal.Documents;
    using CultureInfo = System.Globalization.CultureInfo;

    //=====================================================================
    /// <summary>
    /// FixedPageStructure represents deduced information (such as boundary, 
    /// geometry, layout, semantic, etc.) after a fixed page is analyzed. 
    /// </summary>
    internal sealed class FixedPageStructure
    {
        //--------------------------------------------------------------------
        //
        // Constructors
        //
        //---------------------------------------------------------------------

        #region Constructors
        internal FixedPageStructure(int pageIndex)
        {
            Debug.Assert(pageIndex >= 0);
            _pageIndex = pageIndex;

            // Initialize to virtual 
            _flowStart = new FlowNode(FixedFlowMap.FlowOrderVirtualScopeId, FlowNodeType.Virtual, pageIndex);
            _flowEnd   = _flowStart;

            // 
            _fixedStart = FixedNode.Create(pageIndex, 1, FixedFlowMap.FixedOrderStartVisual, -1, null);
            _fixedEnd   = FixedNode.Create(pageIndex, 1, FixedFlowMap.FixedOrderEndVisual, -1, null);
        }
        #endregion Constructors
        
        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------
          
        #region Public Methods

#if DEBUG
        /// <summary>
        /// Create a string representation of this object
        /// </summary>
        /// <returns>string - A string representation of this object</returns>
        public override string ToString()
        {
            return String.Format("Pg{0}- ", _pageIndex);
        }
#endif
        #endregion Public Methods

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
        // Internal Methods
        //
        //---------------------------------------------------------------------

        #region Internal Methods

        //-------------------------------------
        // Line Detection
        //-------------------------------------

        internal void SetupLineResults(FixedLineResult[] lineResults)
        {
            _lineResults = lineResults;
#if DEBUG
            DocumentsTrace.FixedTextOM.Builder.Trace(string.Format("----LineResults Begin Dump-----\r\n"));
            foreach(FixedLineResult lineResult in _lineResults)
            {
                Debug.Assert(lineResult != null);
                DocumentsTrace.FixedTextOM.Builder.Trace(string.Format("{0}\r\n", lineResult.ToString()));
            }
            DocumentsTrace.FixedTextOM.Builder.Trace(string.Format("----LineResults End Dump-----\r\n"));            
#endif
        }

        // count: desired as input, remaining as output
        // if input count == 0, return current line range. 
        // Return true if it can get some line range
        internal FixedNode[] GetNextLine(int line, bool forward, ref int count)
        {
            Debug.Assert(_lineResults != null);

            if (forward)
            {
                while (line < _lineResults.Length - 1 && count > 0)
                {
                    line++;
                    count--;
                }
            }
            else
            {
                while (line > 0 && count > 0)
                {
                    line--;
                    count--;
                }
            }

            if (count <= 0)
            {
                line = Math.Max(0, Math.Min(line, _lineResults.Length - 1));
                return _lineResults[line].Nodes;
            }

            return null;
        }


        //
        /// <summary>
        /// If the point is in one of the lines, return that line.
        /// Otherwise, return the line with smallest (modified) manhattan distance.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        internal FixedNode[] FindSnapToLine(Point pt)
        {
            Debug.Assert(_lineResults != null);
            FixedLineResult closestLine = null;
            FixedLineResult closestManhattan = null;
            double minVerDistance = double.MaxValue;
            double minHorDistance = double.MaxValue;
            double minManhattan = double.MaxValue;
            foreach (FixedLineResult lineResult in _lineResults)
            {
                double absVerDistance = Math.Max(0, (pt.Y > lineResult.LayoutBox.Y) ? (pt.Y - lineResult.LayoutBox.Bottom) : (lineResult.LayoutBox.Y - pt.Y));
                double absHorDistance = Math.Max(0, (pt.X > lineResult.LayoutBox.X) ? (pt.X - lineResult.LayoutBox.Right) : (lineResult.LayoutBox.X - pt.X));
                if (absVerDistance == 0 && absHorDistance == 0)
                {
                    return lineResult.Nodes;
                }

                //Update the closest line information. We need this if we can't find a close line below the point
                if (absVerDistance < minVerDistance || (absVerDistance == minVerDistance && absHorDistance < minHorDistance))
                {
                    minVerDistance = absVerDistance;
                    minHorDistance = absHorDistance;
                    closestLine = lineResult;
                }
                //Update closest manhattan.  We decide which metric to choose later.
                double manhattan = 5*absVerDistance + absHorDistance;
                //Consider removing second condition, or perhaps come up with an exponential weighting for vertical.
                if (manhattan < minManhattan && absVerDistance < lineResult.LayoutBox.Height)
                {
                    minManhattan = manhattan;
                    closestManhattan = lineResult;
                }
            }
            //We couldn't find the next line below. Return the closest line in this case

            if (closestLine != null)
            {
                if (closestManhattan != null && (closestManhattan.LayoutBox.Left > closestLine.LayoutBox.Right || closestLine.LayoutBox.Left > closestManhattan.LayoutBox.Right))
                {
                    // they don't overlap, so go with closer one
                    return closestManhattan.Nodes;
                }

                // no manhattan, or they overlap/are in same column

                return closestLine.Nodes;
            }

            return null;
        }

        //-------------------------------------
        // Flow Order
        //-------------------------------------
        internal void SetFlowBoundary(FlowNode flowStart, FlowNode flowEnd)
        {
            Debug.Assert(flowStart != null && flowStart.Type != FlowNodeType.Virtual);
            Debug.Assert(flowEnd   != null && flowEnd.Type != FlowNodeType.Virtual);
            _flowStart = flowStart;
            _flowEnd = flowEnd;
        }


#if DEBUG
        private void DrawRectOutline(DrawingContext dc, Pen pen, Rect rect)
        {
            Debug.Assert(!rect.IsEmpty);
            dc.DrawLine(pen, rect.TopLeft,      rect.TopRight);
            dc.DrawLine(pen, rect.TopRight,     rect.BottomRight);
            dc.DrawLine(pen, rect.BottomRight,  rect.BottomLeft);
            dc.DrawLine(pen, rect.BottomLeft,   rect.TopLeft);
        }

        internal void RenderLayoutBox(DrawingContext dc)
        {
            Pen pen = new Pen(Brushes.Blue, 1);
            Rect rect;
            for (int line = 0; line < _lineResults.Length; line++)
            {
                rect = _lineResults[line].LayoutBox;
                if (!rect.IsEmpty)
                {
                    DrawRectOutline(dc, pen, rect);
                }
            }
        }


        private Point CreateFromLastTextPoint(Point p)
        {
            // those is always in the left margin area
            Point newp = new Point(1, p.Y + 10);
            return newp;
        }

        internal void RenderFixedNode(DrawingContext dc)
        {
            //
            //Iterate through fix node to draw red dotted line
            //
            CultureInfo EnglishCulture = System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS;

            int lineCount = _lineResults.Length;
            if (lineCount == 0)
                return;
            
            FixedNode   fixedStartPage = _lineResults[0].Start;
            FixedNode   fixedEndPage = _lineResults[lineCount-1].End;
                
            FixedNode[] fixedNodes = _fixedTextBuilder.FixedFlowMap.FixedOrderGetRangeNodes(fixedStartPage, fixedEndPage);
            FixedPage fp = _fixedTextBuilder.FixedTextContainer.FixedDocument.GetFixedPage(PageIndex);
            FormattedText ft;
            Point prevTextPoint = new Point(0, 0);
            DpiScale dpi = fp.GetDpi();
            foreach (FixedNode currentFixedNode in fixedNodes)
            {
                if (currentFixedNode.Page == FixedFlowMap.FixedOrderStartPage)
                {
                    prevTextPoint.X = prevTextPoint.Y = 0;
                    ft = new FormattedText("FixedOrderStartPage", 
                                            EnglishCulture,
                                            FlowDirection.LeftToRight,
                                            new Typeface("Courier New"), 
                                            8,
                                            Brushes.DarkViolet,
                                            dpi.PixelsPerDip);
                    dc.DrawText(ft, prevTextPoint);
                    continue;
                }
                if (currentFixedNode.Page == FixedFlowMap.FixedOrderEndPage)
                {
                    prevTextPoint.X = fp.Width - 100;
                    prevTextPoint.Y = fp.Height - 10;
                    ft = new FormattedText("FixedOrderEndPage", 
                                            EnglishCulture,
                                            FlowDirection.LeftToRight,
                                            new Typeface("Courier New"), 
                                            8,
                                            Brushes.DarkViolet,
                                            dpi.PixelsPerDip);
                    dc.DrawText(ft, prevTextPoint);
                    continue;
                }
                if (currentFixedNode[1] == FixedFlowMap.FixedOrderStartVisual ||
                    currentFixedNode[1] == FixedFlowMap.FixedOrderEndVisual)
                {
                    prevTextPoint.X = 2;
                    prevTextPoint.Y = prevTextPoint.Y + 10;
                    String outputString = currentFixedNode[1] == FixedFlowMap.FixedOrderStartVisual ?
                                "FixedOrderStartVisual" : "FixedOrderEndVisual";
                    ft = new FormattedText(outputString,
                                            EnglishCulture,
                                            FlowDirection.LeftToRight,
                                            new Typeface("Courier New"),
                                            8,
                                            Brushes.DarkViolet,
                                            dpi.PixelsPerDip);
                    dc.DrawText(ft, prevTextPoint);
                    continue;
                }

                DependencyObject dependencyObject = fp.GetElement(currentFixedNode);
                
                Image image = dependencyObject as Image;
                if (image != null)
                {
                    GeneralTransform transform = image.TransformToAncestor(fp);
                    // You can't use GetContentBounds inside OnRender
                    Rect boundingRect = new Rect(0, 0, image.Width, image.Height);
                    Rect imageRect = transform.TransformBounds(boundingRect);

                    if (!imageRect.IsEmpty)
                    {
                        // Image might overlap, inflate the box.
                        imageRect.Inflate(1, 1);

                        Pen pen = new Pen(Brushes.DarkMagenta, 1.5);
                        DrawRectOutline(dc, pen, imageRect);

                        prevTextPoint.X = imageRect.Right;
                        prevTextPoint.Y = imageRect.Bottom - 10;
                    }
                    else
                    {
                        prevTextPoint.X = 2;
                        prevTextPoint.Y = prevTextPoint.Y + 10;
                    }
                    ft = new FormattedText(currentFixedNode.ToString(),
                                            EnglishCulture,
                                            FlowDirection.LeftToRight,
                                            new Typeface("Courier New"),
                                            8,
                                            Brushes.DarkViolet,
                                            dpi.PixelsPerDip);
                    dc.DrawText(ft, prevTextPoint);
                    continue;
                }
                Path path = dependencyObject as Path;
                if (path != null)
                {
                    GeneralTransform transform = path.TransformToAncestor(fp);
                    // You can't use GetContentBounds inside OnRender
                    Rect boundingRect = path.Data.Bounds;
                    Rect imageRect = transform.TransformBounds(boundingRect);

                    if (!imageRect.IsEmpty)
                    {
                        // Image might overlap, inflate the box.
                        imageRect.Inflate(1, 1);

                        Pen pen = new Pen(Brushes.DarkMagenta, 1.5);
                        DrawRectOutline(dc, pen, imageRect);

                        prevTextPoint.X = imageRect.Right;
                        prevTextPoint.Y = imageRect.Bottom - 10;
                    }
                    else
                    {
                        prevTextPoint.X = 2;
                        prevTextPoint.Y = prevTextPoint.Y + 10;
                    }
                    ft = new FormattedText(currentFixedNode.ToString(),
                                            EnglishCulture,
                                            FlowDirection.LeftToRight,
                                            new Typeface("Courier New"),
                                            8,
                                            Brushes.DarkViolet,
                                            dpi.PixelsPerDip);
                    dc.DrawText(ft, prevTextPoint);
                    continue;
                }
                Glyphs glyphs = dependencyObject as Glyphs;
                if (glyphs != null)
                {
                    GlyphRun run = glyphs.ToGlyphRun();
                    if (run != null)
                    {
                        Rect glyphBox = run.ComputeAlignmentBox();
                        glyphBox.Offset(glyphs.OriginX, glyphs.OriginY);
                        GeneralTransform transform = glyphs.TransformToAncestor(fp);
                        //
                        // Draw it using the dotted red line
                        //
                        Pen pen = new Pen(Brushes.Red, 0.5);
                        Transform t = transform.AffineTransform;
                        if (t != null)
                        {
                            dc.PushTransform(t);
                        }
                        else
                        {
                            dc.PushTransform(Transform.Identity);
                        }
                        DrawRectOutline(dc, pen, glyphBox);

                        prevTextPoint.X = glyphBox.Right;
                        prevTextPoint.Y = glyphBox.Bottom;
                        transform.TryTransform(prevTextPoint, out prevTextPoint);
                        dc.Pop(); // transform
                    }
                    else
                    {
                        prevTextPoint.X = 2;
                        prevTextPoint.Y = prevTextPoint.Y + 10;

                    }

                    ft = new FormattedText(currentFixedNode.ToString(),
                                            EnglishCulture,
                                            FlowDirection.LeftToRight,
                                            new Typeface("Courier New"),
                                            8,
                                            Brushes.DarkViolet,
                                            dpi.PixelsPerDip);
                    dc.DrawText(ft, prevTextPoint);
                    continue;
                }

                //
                // For anything else, there is this code to draw ...
                //
                prevTextPoint.X = 2;
                prevTextPoint.Y = prevTextPoint.Y + 10;
                ft = new FormattedText(currentFixedNode.ToString(),
                                        EnglishCulture,
                                        FlowDirection.LeftToRight,
                                        new Typeface("Courier New"),
                                        8,
                                        Brushes.DarkViolet,
                                        dpi.PixelsPerDip);
                dc.DrawText(ft, prevTextPoint);
            }
        }


        internal void RenderFlowNode(DrawingContext dc)
        {
            FormattedText ft;
            FixedNode fixedNode;
            FixedSOMElement[] somElements;
            String ouptputString;
            FixedElement fixedElement;
            Random random = new Random();

            CultureInfo EnglishCulture = System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS;

            FixedPage fp = _fixedTextBuilder.FixedTextContainer.FixedDocument.GetFixedPage(PageIndex);
            //
            //Iterate through flow node to draw Transparent Rect and draw its index
            //
            Point prevTextPoint=new Point(0, 0);
            DpiScale dpi = fp.GetDpi();
            for (int i = FlowStart.Fp; i <= FlowEnd.Fp; i++)
            {
                FlowNode fn = _fixedTextBuilder.FixedFlowMap[i];
                switch (fn.Type)
                {
                    case FlowNodeType.Boundary :
                    case FlowNodeType.Virtual  :
                        // this two cases won't happen.
                        Debug.Assert(false);
                        break;

                    case FlowNodeType.Start   :
                    case FlowNodeType.End     :
                        {
                        fixedElement = fn.Cookie as FixedElement;
                        String typeString = fixedElement.Type.ToString();
                        int indexofDot = typeString.LastIndexOf('.');
                        ouptputString = String.Format("{0}-{1}",
                                fn.ToString(),
                                typeString.Substring(indexofDot+1));

                        ft = new FormattedText(ouptputString,  
                                                EnglishCulture,
                                                FlowDirection.LeftToRight,
                                                new Typeface("Courier New"), 
                                                8,
                                                Brushes.DarkGreen,
                                                dpi.PixelsPerDip);
                        // Ideally, for FlowNodeType.Start, this should find next FlowNode with physical location, 
                        // and draw it around the physical location. 
                        prevTextPoint = CreateFromLastTextPoint(prevTextPoint);

                        dc.DrawText(ft, prevTextPoint);
                        break;
                        }

                    case FlowNodeType.Noop:
                        ft = new FormattedText(fn.ToString(),
                                                EnglishCulture,
                                                FlowDirection.LeftToRight,
                                                new Typeface("Courier New"),
                                                8,
                                                Brushes.DarkGreen,
                                                dpi.PixelsPerDip);
                        prevTextPoint = CreateFromLastTextPoint(prevTextPoint);
                        dc.DrawText(ft, prevTextPoint);
                        break;

                    case FlowNodeType.Run     :
                        //
                        // Paint the region. The rect is the union of child glyphs.
                        //
                        
                        Glyphs  glyphs;
                        Rect    flowRunBox = Rect.Empty;
                        Rect glyphBox;
                        somElements = _fixedTextBuilder.FixedFlowMap.FlowNodes[fn.Fp].FixedSOMElements;

                        foreach (FixedSOMElement currentSomeElement in somElements)
                        {
                            FixedNode currentFixedNode = currentSomeElement.FixedNode;
                            int startIndex = currentSomeElement.StartIndex;
                            int endIndex = currentSomeElement.EndIndex;

                            // same as (_IsBoundaryFixedNode(currentFixedNode))
                            if (currentFixedNode.Page == FixedFlowMap.FixedOrderStartPage   ||
                                currentFixedNode.Page == FixedFlowMap.FixedOrderEndPage     ||
                                currentFixedNode[1] == FixedFlowMap.FixedOrderStartVisual   ||
                                currentFixedNode[1] == FixedFlowMap.FixedOrderEndVisual)
                            {
                                continue;
                            }

                            glyphs = fp.GetGlyphsElement(currentFixedNode);
                            Debug.Assert(glyphs!= null);

                            glyphBox = FixedTextView._GetGlyphRunDesignRect(glyphs, startIndex, endIndex);
                            if (!glyphBox.IsEmpty)
                            {
                                GeneralTransform g = glyphs.TransformToAncestor(fp);
                                
                                glyphBox = g.TransformBounds(glyphBox);
                                
                            }

                            flowRunBox.Union(glyphBox);
                        }

                        if (flowRunBox.IsEmpty)
                        {
                            Debug.Assert(false);
                        }
                        prevTextPoint.X = flowRunBox.Right;
                        prevTextPoint.Y = flowRunBox.Bottom - random.Next(15);

                        // Draw something the upper left corner of region.
                        ft = new FormattedText(fn.ToString() + "-" + Convert.ToString((int)(fn.Cookie)) +
                                                "-" + Convert.ToString(somElements.Length),
                                                EnglishCulture,
                                                FlowDirection.LeftToRight,
                                                new Typeface("Courier New"),
                                                8,
                                                Brushes.DarkBlue,
                                                dpi.PixelsPerDip);
                        dc.DrawText(ft, prevTextPoint);
 
                        Pen pen = new Pen(Brushes.Blue, 2);
                        flowRunBox.Inflate(random.Next(3), random.Next(3));
                        DrawRectOutline(dc, pen, flowRunBox);
                        break;

                    case FlowNodeType.Object:
                        //
                        // Find the mapping fixed node
                        //
                        somElements = _fixedTextBuilder.FixedFlowMap.FlowNodes[fn.Fp].FixedSOMElements;

                        foreach (FixedSOMElement currentSomeElement in somElements)
                        {
                            fixedNode = currentSomeElement.FixedNode;

                            DependencyObject dependencyObject = fp.GetElement(fixedNode);

                            Image image = dependencyObject as Image;
                            Path path = dependencyObject as Path;

                            if (image != null || path != null)
                            {
                                Rect  imageRect, boundingRect = Rect.Empty;
                                //
                                // Get Image bounding box.
                                //
                                GeneralTransform transform = ((Visual)dependencyObject).TransformToAncestor(fp);
                                // You can't use GetContentBounds inside OnRender
                                if (image != null)
                                {
                                    boundingRect = new Rect(0, 0, image.Width, image.Height);
                                }
                                else
                                {
                                    boundingRect = path.Data.Bounds;
                                }

                                if (!boundingRect.IsEmpty)
                                {
                                    imageRect = transform.TransformBounds(boundingRect);

                                    // Image might overlap, inflate the box.
                                    imageRect.Inflate(3, 3);
                                    dc.DrawRectangle(Brushes.CadetBlue, null, imageRect);

                                    prevTextPoint.X = imageRect.Right;
                                    prevTextPoint.Y = imageRect.Top;
                                }

                            }
                            else
                            {
                                //
                                // If the object is the Image type(that is not likey).
                                // Use the last Point to infer a comment area!
                                //
                                Debug.Assert(false);
                            }

                            fixedElement = fn.Cookie as FixedElement;
                            ft = new FormattedText(fn.ToString(),
                                                    EnglishCulture,
                                                    FlowDirection.LeftToRight,
                                                    new Typeface("Courier New"),
                                                    8,
                                                    Brushes.DarkGreen,
                                                    dpi.PixelsPerDip);
                            dc.DrawText(ft, prevTextPoint);
                        }

                        break;

                    default:
                        Debug.Assert(false);
                        break;
                }
            }
        }

        internal void RenderLines(DrawingContext dc)
        {
            for (int i=0; i<_lineResults.Length; i++)
            {
                FixedLineResult lineResult = _lineResults[i];
                
                Pen pen = new Pen(Brushes.Red, 1);
                Rect layoutBox = lineResult.LayoutBox;
                dc.DrawRectangle(null, pen , layoutBox);
                
                CultureInfo EnglishCulture = System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS;
                FixedPage fp = _fixedTextBuilder.FixedTextContainer.FixedDocument.GetFixedPage(PageIndex);
                FormattedText ft = new FormattedText(i.ToString(), 
                                            EnglishCulture,
                                            FlowDirection.LeftToRight,
                                            new Typeface("Arial"), 
                                            10,
                                            Brushes.White,
                                            fp.GetDpi().PixelsPerDip);
                Point labelLocation = new Point(layoutBox.Left-25, (layoutBox.Bottom + layoutBox.Top)/2 - 10);
                Geometry geom = ft.BuildHighlightGeometry(labelLocation);
                Pen backgroundPen = new Pen(Brushes.Black,1);
                dc.DrawGeometry(Brushes.Black, backgroundPen, geom);
                dc.DrawText(ft, labelLocation);
            }
        }

#endif


        public void ConstructFixedSOMPage(List<FixedNode> fixedNodes)
        {
            Debug.Assert(_fixedSOMPageConstructor != null);
            _fixedSOMPageConstructor.ConstructPageStructure(fixedNodes);
        }

        #endregion Internal Methods

        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------

        #region Internal Properties

        internal FixedNode[] LastLine
        {
            get
            {
                if (_lineResults.Length > 0)
                {
                    return _lineResults[_lineResults.Length - 1].Nodes;
                }
                return null;
            }
        }

        internal FixedNode[] FirstLine
        {
            get
            {
                if (_lineResults.Length > 0)
                {
                    return _lineResults[0].Nodes;
                }
                return null;
            }
        }


        //-------------------------------------
        // Page
        //-------------------------------------
        internal int PageIndex
        {
            get
            {
                return _pageIndex;
            }
        }

        //-------------------------------------
        // Virtualization
        //-------------------------------------
        internal bool Loaded
        {
            get
            {
                return (_flowStart != null && _flowStart.Type != FlowNodeType.Virtual);
            }
        }

        //-------------------------------------
        // Flow Order
        //-------------------------------------
        internal FlowNode FlowStart
        {
            get
            {
                return _flowStart;
            }
        }

        internal FlowNode FlowEnd
        {
            get
            {
                return _flowEnd;
            }
        }

        //-------------------------------------
        // Fixed Order
        //-------------------------------------
        internal FixedSOMPage FixedSOMPage
        {
            get
            {
                return _fixedSOMPage;
            }
            set
            {
                _fixedSOMPage = value;
            }
        }

        internal FixedDSBuilder FixedDSBuilder 
        {
            get
            {
                return _fixedDSBuilder;
            }
            set
            {
                _fixedDSBuilder = value;
            }
        }

        internal FixedSOMPageConstructor PageConstructor
        {
            get
            {
                return _fixedSOMPageConstructor;
            }
            set 
            {
                _fixedSOMPageConstructor = value;
            }
        }

#if DEBUG
        internal FixedTextBuilder FixedTextBuilder
        {
            get
            {
                return _fixedTextBuilder;
            }
            
            set 
            { 
                _fixedTextBuilder = value;
            }
        }

        internal FlowNode[] FlowNodes
        {
            get
            {
                if (_flowNodes == null)
                {
                    List<FlowNode> nodes = new List<FlowNode>();
                    //Find the start of flow nodes
                    int flowCount = this.FixedTextBuilder.FixedFlowMap.FlowCount;
                    FlowNode flowNode = null;
                    int startIdx = 0;

                    if (flowCount > 0)
                    {
                        do 
                        {
                            flowNode = this.FixedTextBuilder.FixedFlowMap.FlowNodes[startIdx++];
                            if (this.FlowStart == flowNode)
                            {
                                break;
                            }
                        }
                        while (startIdx < flowCount);
                    }
                    if (startIdx < flowCount)
                    {
                        do
                        {
                            flowNode = this.FixedTextBuilder.FixedFlowMap.FlowNodes[startIdx++];
                            nodes.Add(flowNode);
                        }while (startIdx < flowCount && this.FlowEnd != flowNode);
                    }
                    _flowNodes = nodes.ToArray();
                    
                }
                return _flowNodes;
            }
        }

        internal List<FixedNode> FixedNodes
        {
            get
            {
                return _fixedNodes;
            }
            set
            {
                _fixedNodes = value;
            }
        }
#endif
        #endregion Internal Properties

 
        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private Fields
        private readonly int _pageIndex;
        
        // Flow Order Boundary
        private FlowNode    _flowStart;
        private FlowNode    _flowEnd;

        // Fixed Order Boundary
        private FixedNode   _fixedStart;
        private FixedNode   _fixedEnd;

        private FixedSOMPageConstructor _fixedSOMPageConstructor;
        private FixedSOMPage _fixedSOMPage;

        private FixedDSBuilder _fixedDSBuilder;

        // Baseline sorted line results
        private FixedLineResult[] _lineResults;

        //Determines whether a point is close enough to a line when determining snap to line
        
#if DEBUG
        private FixedTextBuilder _fixedTextBuilder;
        private FlowNode[] _flowNodes; //Flow nodes for this page
        private List<FixedNode> _fixedNodes;
#endif        
        #endregion Private Fields
    }
}
