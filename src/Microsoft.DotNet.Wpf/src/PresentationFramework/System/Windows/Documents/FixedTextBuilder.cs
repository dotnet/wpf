// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      FixedTextBuilder contains heuristics to map fixed document elements
//      into stream of flow text
//

namespace System.Windows.Documents
{
    using MS.Internal.Documents;
    using System.Windows.Controls;      // UIElementCollection
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Markup;
    using System.Windows.Shapes;       // Glyphs
    using System.Windows.Automation;
    using System.Windows.Documents.DocumentStructures;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.IO;
    using System.Xml;
    using Path=System.Windows.Shapes.Path;

    using MS.Utility;

    //=====================================================================
    /// <summary>
    /// FixedTextBuilder contains heuristics to map fixed document elements
    /// into stream of flow text.
    /// </summary>
    internal sealed class FixedTextBuilder
    {
        //--------------------------------------------------------------------
        //
        // Consts
        //
        //---------------------------------------------------------------------
        #region Const
        internal const char FLOWORDER_SEPARATOR = ' ';
        #endregion Const



        //--------------------------------------------------------------------
        //
        // Statics
        //
        //---------------------------------------------------------------------
        #region Statics

        // A list of CultureInfo that are always adjacent
        internal static CultureInfo[] AdjacentLanguage =
        {
            new CultureInfo("zh-HANS"),     // Chinese Simplified
            new CultureInfo("zh-HANT"),     // Chinese Traditional
            new CultureInfo("zh-HK"),       // Chinese Hong Kong SAR
            new CultureInfo("zh-MO"),       // Chinese Macao SAR
            new CultureInfo("zh-CN"),       // Chinese China
            new CultureInfo("zh-SG"),       // Chinese Singapore
            new CultureInfo("zh-TW"),       // Chinese `Taiwan
            new CultureInfo("ja-JP"),       // Japanese
            new CultureInfo("ko-KR"),       // Korean
            new CultureInfo("th-TH")        // Thai
        };


        internal static bool AlwaysAdjacent(CultureInfo ci)
        {
            foreach (CultureInfo cultureInfo in AdjacentLanguage)
            {
                if (ci.Equals(cultureInfo))
                {
                    return true;
                }
            }
            return false;
        }



        // obtained via Unicode 3.0 book: C0 Controls and Basic Latin.
        //    0x002D/0x00AD and their related characters.
        //
        // NOTE it is okay to not getting accurate list as long as
        // we deal with common cases since this is used in heuristic
        // algorithm!
        internal static char[] HyphenSet =
        {
            '\x002D',     // Hyphen-Minus
            '\x2010',     // Hyphen
            '\x2011',     // Non-breaking Hyphen
            '\x2012',     // Figure-dash
            '\x2013',     // En-dash
            '\x2212',     // Minus Sign
            '\x00AD'      // Soft-Hyphen
        };

        internal static bool IsHyphen(char target)
        {
            foreach (char hyphen in HyphenSet)
            {
                if (hyphen == target)
                {
                    return true;
                }
            }
            return false;
        }

        // Space that was used in the heuristic algorithm.
        internal static bool IsSpace(char target)
        {
            // Only the regular space character is considered
            // space.
            return (target == '\x0020');
        }
        #endregion Statics


        //--------------------------------------------------------------------
        //
        // Connstructors
        //
        //---------------------------------------------------------------------

        #region Constructors
        internal FixedTextBuilder(FixedTextContainer container)
        {
            _container = container;
            _Init();
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
        // Internal Methods
        //
        //---------------------------------------------------------------------

        #region Internal Methods

        //--------------------------------------------------------------------
        // Virtualization
        //---------------------------------------------------------------------

        // Add virtual FlowNode to represent the page
        internal void AddVirtualPage()
        {
#if DEBUG
            DocumentsTrace.FixedTextOM.Builder.Trace(string.Format("AppendVirtualPage {0}", _pageStructures.Count));
#endif
            FixedPageStructure pageStructure = new FixedPageStructure(_pageStructures.Count);
#if DEBUG
            pageStructure.FixedTextBuilder = this;
#endif
            _pageStructures.Add(pageStructure);

            // Insert the virtural FlowNode into the Flow Order.
            _fixedFlowMap.FlowOrderInsertBefore(_fixedFlowMap.FlowEndEdge, pageStructure.FlowStart);
        }


        //--------------------------------------------------------------------
        // Devirtualizing flow structure
        //---------------------------------------------------------------------

        // Making sure a page is devirtualized
        // All function that takes FixedNode as parameter and asks for FlowPosition
        // related content should call this functions first!!!
        internal bool EnsureTextOMForPage(int pageIndex)
        {
            Debug.Assert(!_IsBoundaryPage(pageIndex));
            FixedPageStructure pageStructure = _pageStructures[pageIndex];
            if (!pageStructure.Loaded)
            {
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXEnsureOMBegin);
                try
                {
                    FixedPage page = _container.FixedDocument.SyncGetPage(pageIndex, false);

                    if (page == null)
                        return false;

                    Size pageSize = _container.FixedDocument.ComputePageSize(page);

                    page.Measure(pageSize);
                    page.Arrange(new Rect(new Point(0, 0), pageSize));

                    Debug.Assert(page != null);
                    Debug.Assert(page.IsInitialized);

                    bool constructSOM = true;
                    StoryFragments sf = page.GetPageStructure();
                    if (sf != null)
                    {
                        constructSOM = false;
                        FixedDSBuilder fb = new FixedDSBuilder(page, sf);
                        pageStructure.FixedDSBuilder = fb;
                    }

                    if (constructSOM)
                    {
                        FixedSOMPageConstructor pageConstructor = new FixedSOMPageConstructor(page, pageIndex);
                        pageStructure.PageConstructor = pageConstructor;
                        pageStructure.FixedSOMPage = pageConstructor.FixedSOMPage;
                    }

                    DocumentsTrace.FixedTextOM.Builder.Trace(string.Format("_EnsureTextOMForPage Loading..."));
                    _CreateFixedMappingAndElementForPage(pageStructure, page, constructSOM);

#if DEBUG
                    //
                    // Note: this debug verification is expensive. I have moved this code from FixedFlowMap.cs
                    // to reduce the rate is is called. Now it is called after each page is constructed and might
                    // still have effect on long documents and scrolling in page per stream case.
                    //
                    //FixedFlowMap.DebugVerifyMapping(true);
#endif
                }
                finally
                {
                    EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXEnsureOMEnd);
                }

                return true;
            }

#if DEBUG
            FixedPage fixedPage = _container.FixedDocument.SyncGetPage(pageIndex, false);
            fixedPage.FixedPageStructure = pageStructure;
#endif
            return false;
        }

        //--------------------------------------------------------------------
        // Fixed Document
        //---------------------------------------------------------------------

        // This could return null if FixedNode is an artificial boundary node
        internal FixedPage GetFixedPage(FixedNode node)
        {
            FixedDocument doc = _container.FixedDocument;
            return doc.SyncGetPageWithCheck(node.Page);
        }

        // This could return null if FixedNode is an artificial boundary node
        internal Glyphs GetGlyphsElement(FixedNode node)
        {
            FixedPage page = GetFixedPage(node);
            if (page != null)
            {
                return page.GetGlyphsElement(node);
            }
            return null;
        }


        //--------------------------------------------------------------------
        // Hit Testing / Keyboard Navigation
        //---------------------------------------------------------------------

        // Given a fixed node, find its next/previous line
        // count:
        //      in   # of lines desired
        //      out  # of lines remaining
        // Return null if no next line
        internal FixedNode[] GetNextLine(FixedNode currentNode, bool forward, ref int count)
        {
            if (_IsBoundaryPage(currentNode.Page))
            {
                return null;
            }

            EnsureTextOMForPage(currentNode.Page);
            FixedPageStructure ps = _pageStructures[currentNode.Page];

            if (_IsStartVisual(currentNode[1]))
            {
                FixedNode[] firstLine = ps.FirstLine;
                if (firstLine == null)
                {
                    return null;
                }
                currentNode = firstLine[0];
                count--;
            }
            else if (_IsEndVisual(currentNode[1]))
            {
                FixedNode[] lastLine = ps.LastLine;
                if (lastLine == null)
                {
                    return null;
                }
                currentNode = lastLine[0];
                count--;
            }

            FixedSOMTextRun run = _fixedFlowMap.MappingGetFixedSOMElement(currentNode, 0) as FixedSOMTextRun;

            if (run == null)
            {
                return null;
            }

            int lineIndex = run.LineIndex;

            return ps.GetNextLine(lineIndex, forward, ref count);
        }


        // Given a fixed node
        // Return null is cannot get line
        internal FixedNode[] GetLine(int pageIndex, Point pt)
        {
            EnsureTextOMForPage(pageIndex);
            return _pageStructures[pageIndex].FindSnapToLine(pt);
        }

        // Find first line on page
        internal FixedNode[] GetFirstLine(int pageIndex)
        {
            EnsureTextOMForPage(pageIndex);
            return _pageStructures[pageIndex].FirstLine;
        }

        //-----------------------------------------------
        // Fixed nodes ==>  FlowPosition
        //-----------------------------------------------

        // Find out flow offset given fixed position
        internal FlowPosition CreateFlowPosition(FixedPosition fixedPosition)
        {
            // make sure page has been loaded
            EnsureTextOMForPage(fixedPosition.Page);

            FixedSOMElement element = _fixedFlowMap.MappingGetFixedSOMElement(fixedPosition.Node, fixedPosition.Offset);
            if (element != null)
            {
                FlowNode flow = element.FlowNode;
                int fixedOffset = fixedPosition.Offset;
                FixedSOMTextRun run = element as FixedSOMTextRun;
                if (run != null && run.IsReversed)
                {
                    fixedOffset  = run.EndIndex - run.StartIndex - fixedOffset;
                }
                int offset = element.OffsetInFlowNode + fixedOffset - element.StartIndex;
                return new FlowPosition(_container, flow, offset);
            }
            return null;
        }


        internal FlowPosition GetPageStartFlowPosition(int pageIndex)
        {
            EnsureTextOMForPage(pageIndex);

            FlowNode fn = _pageStructures[pageIndex].FlowStart;
            // Remove this assert once we start supporting element across page bounary
            Debug.Assert(fn.Type == FlowNodeType.Start);
            return new FlowPosition(_container, fn, 0);
        }


        internal FlowPosition GetPageEndFlowPosition(int pageIndex)
        {
            EnsureTextOMForPage(pageIndex);

            // Remove this assert once we start supporting element across page bounary
            FlowNode fn = _pageStructures[pageIndex].FlowEnd;
            return new FlowPosition(_container, fn, 1);
        }

        //-----------------------------------------------
        // FlowPosition ==>  Fixed Nodes
        //-----------------------------------------------

        // This function can handle flow nodes that are mapped to fixed nodes (Object or Run type) and start and end nodes
        internal bool GetFixedPosition(FlowPosition position, LogicalDirection textdir, out FixedPosition fixedp)
        {
            // Currently broken on empty page and on start/end flow nodes!
            // Init out parameter
            fixedp = new FixedPosition(FixedFlowMap.FixedStartEdge, 0);

            int flowOffset;
            FlowNode flow;

            position.GetFlowNode(textdir, out flow, out flowOffset);
            FixedSOMElement[] fixes = flow.FixedSOMElements;
            if (fixes == null)
            {
                return false;
            }

            int loIndex = 0, hiIndex = fixes.Length - 1;
            while (hiIndex > loIndex)
            {
                int index = (hiIndex + loIndex + 1) >> 1; // middle, round up -- guarantee hiIndex >= index > loIndex
                if (fixes[index].OffsetInFlowNode > flowOffset)
                {
                    hiIndex = index - 1;
                }
                else
                {
                    loIndex = index;
                }
            }

            FixedSOMElement element = fixes[loIndex];
            FixedSOMTextRun run;

            if (loIndex > 0 &&
                textdir == LogicalDirection.Backward &&
                element.OffsetInFlowNode == flowOffset)
            {
                //check if we should really be in the prior element
                FixedSOMElement last = fixes[loIndex - 1];
                int offsetInLast = flowOffset - last.OffsetInFlowNode + last.StartIndex;

                if (offsetInLast == last.EndIndex)
                {
                    run = last as FixedSOMTextRun;
                    if (run != null && run.IsReversed)
                    {
                        offsetInLast = run.EndIndex - run.StartIndex - offsetInLast;
                    }
                    fixedp = new FixedPosition(last.FixedNode, offsetInLast);
                    return true;
                }
            }

            run = element as FixedSOMTextRun;
            int fixedOffset  = flowOffset - element.OffsetInFlowNode + element.StartIndex;
            if (run != null && run.IsReversed)
            {
                fixedOffset = run.EndIndex - run.StartIndex - fixedOffset;
            }
            fixedp = new FixedPosition(element.FixedNode, fixedOffset);
            return true;
        }


        // Find out fixed node given flow positions
        internal bool GetFixedNodesForFlowRange(FlowPosition pStart,
                                                            FlowPosition pEnd,
                                                            out FixedSOMElement[] somElements,
                                                            out int firstElementStart,
                                                            out int lastElementEnd)
        {
            Debug.Assert(pStart.CompareTo(pEnd) < 0);
            somElements = null;
            firstElementStart = 0;
            lastElementEnd = 0;

            FlowNode[] flowNodes;
            int charStart = 0;
            int charEnd = -1;

            int offsetStart;
            int offsetEnd;

            pStart.GetFlowNodes(pEnd, out flowNodes, out offsetStart, out offsetEnd);
            if (flowNodes.Length <= 0)
            {
                return false;
            }

            ArrayList ar = new ArrayList();
            FlowNode flowStart  = flowNodes[0];
            FlowNode flowEnd    = flowNodes[flowNodes.Length - 1];

            foreach (FlowNode flowScan in flowNodes)
            {
                int skipBefore = 0;
                int stopAt     = Int32.MaxValue;
                if (flowScan == flowStart)
                {
                    skipBefore = offsetStart;
                }

                if (flowScan == flowEnd)
                {
                    stopAt = offsetEnd;
                }

                if (flowScan.Type == FlowNodeType.Object)
                {
                    FixedSOMElement[] fixes = flowScan.FixedSOMElements;
                    Debug.Assert(fixes.Length == 1);
                    ar.Add(fixes[0]);
                }

                if (flowScan.Type == FlowNodeType.Run)
                {
                    FixedSOMElement[] fixes = flowScan.FixedSOMElements;
                    foreach (FixedSOMElement element in fixes)
                    {
                        int startIndex = element.OffsetInFlowNode;
                        if (startIndex >= stopAt)
                        {
                            break;
                        }

                        int endIndex = startIndex + element.EndIndex - element.StartIndex;
                        if (endIndex <= skipBefore)
                        {
                            continue;
                        }

                        ar.Add(element);

                        if (skipBefore >= startIndex && flowScan == flowStart)
                        {
                            charStart = element.StartIndex + skipBefore - startIndex;
                        }

                        if (stopAt <= endIndex && flowScan == flowEnd)
                        {
                            charEnd = element.StartIndex + stopAt - startIndex;
                            break;
                        }
                        else if (stopAt == endIndex + 1)
                        {
                            charEnd = element.EndIndex; //in case this ends at beginning of next node
                        }
                    }
                }//endofifrun
            }//endofforeach FlowNode

            // This array could be empty! (Maybe...)  Broken if it is.
            somElements = (FixedSOMElement[])ar.ToArray(typeof(FixedSOMElement));
            if (somElements.Length == 0)
            {
                return false;
            }

            if (flowStart.Type == FlowNodeType.Object)
            {   // Image
                firstElementStart = offsetStart;
            }
            else
            {
                firstElementStart = charStart;
            }
            if (flowEnd.Type == FlowNodeType.Object)
            {
                lastElementEnd = offsetEnd;
            }
            else
            {
                lastElementEnd = charEnd;
            }

            return true;
        }

        //-----------------------------------------------
        // FlowPosition ==>  Flow Content
        //-----------------------------------------------

        // Helper function to retrieve text from FixedNodes that are mapped
        // to a flow run represented by a FlowPosition.
        internal string GetFlowText(FlowNode flowNode)
        {
            Debug.Assert(flowNode.Type == FlowNodeType.Run);

            StringBuilder sb = new StringBuilder();
            FixedSOMElement[] fixes = flowNode.FixedSOMElements;
            Debug.Assert(flowNode.Type == FlowNodeType.Run);
            Debug.Assert(fixes != null);

            foreach (FixedSOMTextRun element in fixes)
            {
                sb.Append(element.Text);
            }
            return sb.ToString();
        }

        internal static bool MostlyRTL(string s)
        {
            int RTL = 0;
            int LTR = 0;
            foreach (char c in s)
            {
                if (_IsRTL(c))
                {
                    RTL++;
                }
                else if (c != ' ')
                {
                    LTR++;
                }
            }

            return (RTL > 0) && (LTR == 0 || (RTL / LTR >= 2));
        }

        //Decides whether two lines specified by line heights and a vertical distance between them is considered on the same line
        //verticalDistance needs to be passed as top of line2 - top of line 1
        internal static bool IsSameLine(double verticalDistance, double fontSize1, double fontSize2)
        {
            // According to spec for rubber band selection, to adjacent Glyphs are on same line if they have 50% overlap
            double smallSize = (fontSize1 < fontSize2) ? fontSize1 : fontSize2;
            double overlap = (verticalDistance > 0) ? (fontSize1 - verticalDistance) : (fontSize2 + verticalDistance);
            return overlap / smallSize > .5;
        }

        //------------------------------------------------------------------------------
        // Design Note (ZhenbinX - 2004/11/15) - Heuristic to test if two FixedSOMTextRun adjacent
        // in geometry for the purpose of adding additional separator into non-adjacent runs.
        // This algorithm bias toward NOT adding separator. The reason is artifically breaking
        // a word is more annoying than missing a space between words.
        //
        // The following steps are applied in order:
        // 1. If the two TextRun belong to two different languages, they are not
        //    adjacent. (A separator is needed)
        // 2. If the TextRun belongs to a language that does not use space as separator,
        //    they are always adjacent. (Need additional input about Bidi and complext
        //    script here. Currently NOT adjacent).
        // 3. If the two TextRuns belong to two different lines, they are NOT adjacent
        //    unless the end of the previous GlyphRun is HYPHEN.
        // 3. For TextRuns on the same line:
        //    a. If the last character of the previous run is SPACE, they are adjacent.
        //    b. If they are in different Bidi embedding level, they are NOT adjacent.
        //    c. If the distance between their InkBoundingBox is greater than 1/4 of
        //       the BoundingBox of the preceeding GlyphRun, they are NOT adjacent.
        //       (This can be tweaked to use step function in the future!).
        // All others are adjacent Glyphs!
        //
        //------------------------------------------------------------------------------
        internal static bool IsNonContiguous(CultureInfo ciPrev,
                                                    CultureInfo ciCurrent,
                                                    bool isSidewaysPrev,
                                                    bool isSidewaysCurrent,
                                                    string strPrev,
                                                    string strCurrent,
                                                    GlyphComparison comparison)

        {
            if (ciPrev != ciCurrent)
            {
                return true;
            }
            if (AlwaysAdjacent(ciPrev))
            {
                return false;
            }


            if (isSidewaysPrev != isSidewaysCurrent)
            {
                return true;  // not adjacent
            }

            if (strPrev.Length == 0 || strCurrent.Length == 0)
            {
                return false;  // adjacent
            }

            if (!isSidewaysPrev)
            {   // Horizontal Lines
                int textLength = strPrev.Length;
                char lastChar = strPrev[textLength - 1];

                if (IsSpace(lastChar))
                {
                    return false;       // adjacent
                }

                if (comparison == GlyphComparison.DifferentLine ||
                    comparison == GlyphComparison.Unknown)
                {
                    // Different Line
                    if (!IsHyphen(lastChar))
                    {
                        return true;   // non adjacent
                    }
                }
                else
                {
                    return comparison != GlyphComparison.Adjacent;
                }
            }
            return false;   // Default is adjacent
        }


        #endregion Internal Methods


        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------
        #region Internal Properties
        internal FixedFlowMap FixedFlowMap
        {
            get
            {
                return _fixedFlowMap;
            }
        }
#if DEBUG
        internal FixedTextContainer FixedTextContainer
        {
            get
            {
                return _container;
            }
        }
#endif

        #endregion Internal Propeties

        //--------------------------------------------------------------------
        //
        // Private Methods
        //
        //---------------------------------------------------------------------

        #region Private Methods

        //--------------------------------------------------------------------
        // Initilization
        //---------------------------------------------------------------------
        private void _Init()
        {
            // Initialize Id
            _nextScopeId  = FixedFlowMap.FlowOrderScopeIdStart;

            // Create empty mapping
            _fixedFlowMap = new FixedFlowMap();

            //
            _pageStructures  = new List<FixedPageStructure>();
        }

        //--------------------------------------------------------------------
        // Heuristics
        //
        // Design Note (ZhenbinX):
        //      Page boundary is artificially represented by a pair
        //      of FixedNodes. Start of Page uses int.MinValue as
        //      its glyphs index, while End of Page uses int.MaxValue
        //      as its glyphs index.  All users of FixedNode needs
        //      to be aware of such articifical index and special case
        //      so that the "fake" glyphs indice are not used to access
        //      children.  One should always use FixedPage.GetGlyphs
        //      instead of using FixedPage's logical children collection
        //      directly, and should always check for possible null return.
        //
        //      The page boundaries, however, are mapped to a FlowNodeType.Run
        //      instead of any other FlowNodeType.  This is to be
        //      consistent with the general principal that non-run type
        //      FlowNode has no corresponding FixedNode.
        //
        //      Each FlowNode, if it is of type FlowNodeType.Run, contains
        //      a cookie that is the run length.
        //---------------------------------------------------------------------

        // Helper function for efficient creation of FixedNode used during pre-order walking.
        // current index is @ childIndex
        // depending on nesting level, the prefix can be hold at
        //      1. no prefix (level1)
        //      2. level1Index
        //      3. pathPrefix.
        //
        FixedNode _NewFixedNode(int pageIndex, int nestingLevel, int level1Index, int[] pathPrefix, int childIndex)
        {
            if (nestingLevel == 1)
            {
                return FixedNode.Create(pageIndex, nestingLevel, childIndex, -1, null);
            }
            else if (nestingLevel == 2)
            {
                return FixedNode.Create(pageIndex, nestingLevel, level1Index, childIndex, null);
            }
            else
            {
                // For deeply nested path, we need to append current childIndex to a pathPrefix
                // to form the entire childPath.
                Debug.Assert(pathPrefix != null);
                int[] newPath = new int[pathPrefix.Length + 1];
                pathPrefix.CopyTo(newPath, 0);
                newPath[newPath.Length - 1] = childIndex;
                return FixedNode.Create(pageIndex, nestingLevel, -1, -1, newPath);
            }
        }


        // Helper function to get Paths with ImageBrushes
        private bool _IsImage(object o)
        {
            System.Windows.Shapes.Path p = o as System.Windows.Shapes.Path;
            if (p != null)
            {
                return p.Fill is ImageBrush && p.Data != null;
            }
            return o is Image;
        }


        private bool _IsNonContiguous(FixedSOMTextRun prevRun, FixedSOMTextRun currentRun, GlyphComparison comparison)
        {
            Debug.Assert(prevRun != null);
            Debug.Assert(currentRun != null);

            if (prevRun.FixedNode == currentRun.FixedNode)
            {
                return currentRun.StartIndex != prevRun.EndIndex;
            }

            return IsNonContiguous(prevRun.CultureInfo,
                                    currentRun.CultureInfo,
                                    prevRun.IsSideways,
                                    currentRun.IsSideways,
                                    prevRun.Text,
                                    currentRun.Text,
                                    comparison);
        }

        private GlyphComparison _CompareGlyphs(Glyphs glyph1, Glyphs glyph2)
        {
            GlyphComparison comparison = GlyphComparison.DifferentLine;
            if (glyph1 == glyph2)
            {
                comparison = GlyphComparison.SameLine;
            }
            else if (glyph1 != null && glyph2 != null)
            {
                GlyphRun run1 = glyph1.ToGlyphRun();
                GlyphRun run2 = glyph2.ToGlyphRun();
                if (run1 != null && run2 != null)
                {
                    Rect box1 = run1.ComputeAlignmentBox();
                    box1.Offset(glyph1.OriginX, glyph1.OriginY);
                    Rect box2 = run2.ComputeAlignmentBox();
                    box2.Offset(glyph2.OriginX, glyph2.OriginY);

                    bool LTR1 = ((glyph1.BidiLevel & 1) == 0);
                    bool LTR2 = ((glyph2.BidiLevel & 1) == 0);

                    GeneralTransform transform = glyph2.TransformToVisual(glyph1);
                    Point prevPt = LTR1 ? box1.TopRight : box1.TopLeft;
                    Point currentPt = LTR2 ? box2.TopLeft : box2.TopRight;
                    if (transform != null)
                    {
                        transform.TryTransform(currentPt, out currentPt);
                    }

                    if (IsSameLine(currentPt.Y - prevPt.Y, box1.Height, box2.Height))
                    {
                        comparison = GlyphComparison.SameLine;
                        if (LTR1 == LTR2)
                        {
                            double xDistance = Math.Abs(currentPt.X - prevPt.X);
                            double maxHeight = Math.Max(box1.Height, box2.Height);

                            if (xDistance / maxHeight < .05)
                            {
                                comparison = GlyphComparison.Adjacent;
                            }
                        }
                    }
                }
            }
            return comparison;
        }

        // Apply heuristics
        // Create FlowNode structure
        // Create Fixed to Flow mapping
        // Create FixedElement
        private void _CreateFixedMappingAndElementForPage(FixedPageStructure pageStructure, FixedPage page, bool constructSOM)
        {
            List<FixedNode> fixedNodes = new List<FixedNode>();
            _GetFixedNodes(pageStructure,
                           page.Children,
                           1,
                           -1,
                           null,
                           constructSOM,
                           fixedNodes,
                           Matrix.Identity);

            FlowModelBuilder flowBuilder = new FlowModelBuilder(this, pageStructure, page);
            flowBuilder.FindHyperlinkPaths(page);

            if (constructSOM)
            {
                pageStructure.FixedSOMPage.MarkupOrder = fixedNodes;
                pageStructure.ConstructFixedSOMPage(fixedNodes);


                _CreateFlowNodes(pageStructure.FixedSOMPage, flowBuilder);
                pageStructure.PageConstructor = null;
            }
            else
            {
                pageStructure.FixedDSBuilder.ConstructFlowNodes(flowBuilder, fixedNodes);
            }

            flowBuilder.FinishMapping();

#if DEBUG
            pageStructure.FixedNodes = fixedNodes;
            //flowBuilder.DumpToFile("FlowDump_Page_" + pageStructure.PageIndex.ToString() + ".xml");
#endif
        }

        private void _GetFixedNodes(
                                    FixedPageStructure pageStructure,
                                    IEnumerable oneLevel,
                                    int nestingLevel,       // the level of nesting
                                    int level1Index,        // if nesting level == 2, this is leve1 1 index
                                    int[] pathPrefix,   // if nesting level > 2, this is used to represent prefix, otherwise it is null
                                    bool constructLines,
                                    List<FixedNode> fixedNodes, // start empty, nodes will be added to this list
                                    Matrix transform
                                    )
        {
            int pageIndex = pageStructure.PageIndex;

            DocumentsTrace.FixedTextOM.Builder.Trace(string.Format("_FlowOrderAnalysis P{0}-L[{0}]", pageIndex, nestingLevel));

            int currentScopeId = _NewScopeId();

            // Create per run fixed nodes
            IFrameworkInputElement namedNode;

            int childIndex = 0;
            IEnumerator elements = oneLevel.GetEnumerator();
            while (elements.MoveNext())
            {
                if (!constructLines)
                {
                    namedNode = elements.Current as IFrameworkInputElement;
                    if (namedNode != null && namedNode.Name != null && namedNode.Name.Length != 0)
                    {
                        pageStructure.FixedDSBuilder.BuildNameHashTable(namedNode.Name,
                                    elements.Current as UIElement,
                                    fixedNodes.Count);
                    }
                }

                if (_IsImage(elements.Current) ||
                    (elements.Current is Glyphs && (elements.Current as Glyphs).MeasurementGlyphRun != null))
                {
                    fixedNodes.Add(
                        _NewFixedNode(pageIndex, nestingLevel, level1Index, pathPrefix, childIndex)
                                            );
                    // GlyphRuns are non contiguous due to Image
                }
                else if (constructLines && elements.Current is Path)
                {
                    pageStructure.PageConstructor.ProcessPath(elements.Current as Path, transform);
                }
                else if (elements.Current is Canvas)
                {
                    Transform localTransform = Transform.Identity;

                    // Drill down to next level
                    IEnumerable children;

                    Canvas canvas = elements.Current as Canvas;

                    children       = canvas.Children;
                    localTransform = canvas.RenderTransform;

                    if (localTransform == null)
                    {
                        localTransform = Transform.Identity;
                    }

                    if (children != null)
                    {
                        int[] newPathPrefix = null;
                        if (nestingLevel >= 2)
                        {
                            // anything more than two level deep, we need to use path array
                            if (nestingLevel == 2)
                            {
                                Debug.Assert(pathPrefix == null);
                                Debug.Assert(level1Index >= 0);
                                newPathPrefix = new int[2];
                                newPathPrefix[0] = level1Index;
                            }
                            else
                            {
                                newPathPrefix = new int[pathPrefix.Length + 1];
                                pathPrefix.CopyTo(newPathPrefix, 0);
                            }
                            // Append the childIndex to newPathPrefix.
                            Debug.Assert(newPathPrefix != null);
                            newPathPrefix[newPathPrefix.Length - 1] = childIndex;
                        }
#if DEBUG
                        else
                        {
                            Debug.Assert(pathPrefix == null && newPathPrefix == null);
                        }
#endif
                        _GetFixedNodes(
                                pageStructure,
                                children,
                                (nestingLevel + 1), // go to next nesting level
                                (nestingLevel == 1 ? childIndex : -1), // use this index if we are going from 1 to 2
                                newPathPrefix,         // otherwise use this path prefix
                                constructLines,
                                fixedNodes,
                                transform * localTransform.Value
                                );
                    }//endofElementIsCanvas
                }

                childIndex++;
            }
        }

        private void _CreateFlowNodes(FixedSOMPage somPage, FlowModelBuilder flowBuilder)
        {
            flowBuilder.AddStartNode(FixedElement.ElementType.Section);
            somPage.SetRTFProperties(flowBuilder.FixedElement);

            foreach (FixedSOMContainer container in somPage.SemanticBoxes)
            {
                _CreateFlowNodes(container, flowBuilder);
            }

            //Add the remaining hyperlinks at the end of the page
            flowBuilder.AddLeftoverHyperlinks();

            flowBuilder.AddEndNode();
        }




        //We will need to have a special case for an empty page
        private void _CreateFlowNodes(FixedSOMContainer node, FlowModelBuilder flowBuilder)
        {
            FixedElement.ElementType[] elementsForNode = node.ElementTypes;
            foreach (FixedElement.ElementType type in elementsForNode)
            {
                flowBuilder.AddStartNode(type);
                node.SetRTFProperties(flowBuilder.FixedElement);
            }


            List<FixedSOMSemanticBox> children = node.SemanticBoxes;

            foreach (FixedSOMSemanticBox box in children)
            {
                if (box is FixedSOMElement)
                {
                    flowBuilder.AddElement((FixedSOMElement)box);
                }
                else if (box is FixedSOMContainer)
                {
                    _CreateFlowNodes((FixedSOMContainer)box, flowBuilder);
                }
                else
                {
                    Debug.Assert(false);
                }
            }

            foreach (FixedElement.ElementType type in elementsForNode)
            {
                flowBuilder.AddEndNode();
            }
        }



        //--------------------------------------------------------------------
        // Fixed Document
        //---------------------------------------------------------------------

        private bool _IsStartVisual(int visualIndex)
        {
            return visualIndex == FixedFlowMap.FixedOrderStartVisual;
        }

        private bool _IsEndVisual(int visualIndex)
        {
            return visualIndex == FixedFlowMap.FixedOrderEndVisual;
        }

        private bool _IsBoundaryPage(int pageIndex)
        {
            return (   pageIndex == FixedFlowMap.FixedOrderStartPage
                    || pageIndex == FixedFlowMap.FixedOrderEndPage
                    );
        }

        // Advance the scope ID -- called when a new flow scope is needed.
        // the scope ID is used to identify the flow scope in flow order.
        //
        // For instance:
        //  <Start Id=1> <TextRun Id=1> <Image Id=2> <TextRun Id=1> <TextRun Id=1> <End Id=1>
        // All Id =1 consists of one scope while Id=2 opens a new scope (in this case a nested scope).
        //
        private int _NewScopeId()
        {
            return _nextScopeId++;
        }


        private static bool _IsRTL(char c)
        {
            /*return (c > 0x590 && c < 0x780) ||
                (c >= 0xFB1D && c <= 0xFDFD) ||
                (c >= 0xFE70 && c <= 0xFEFC);*/
            return (c >= 0x5D0 && c <= 0x60B) ||
                (c == 0x60D) ||
                (c >= 0x61B && c <= 0x64A) ||
                (c >= 0x66D && c <= 0x6D5 && c != 0x670) ||
                (c == 0x6DD) || (c == 0x6E5) || (c == 0x6E6) ||
                (c == 0x6EE) || (c == 0x6EF) ||
                (c >= 0x6FA && c <= 0x70D) ||
                (c == 0x710) || (c >= 0x712 && c <= 0x72F) ||
                (c >= 0x74D && c <= 0x7A5) || (c == 0x7B1) ||
                (c == 0xFB1D) || (c >= 0xFB1F && c <= 0xFD3D && c != 0xFB29) ||
                (c >= 0xFD50 && c <= 0xFDFC) || (c >= 0xFE70 && c <= 0xFEFC);
        }

        #endregion Private Methods

        //--------------------------------------------------------------------
        //
        // Private Properties
        //
        //---------------------------------------------------------------------

        #region Private Properties
        #endregion Private Properties

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private Fields
        private readonly FixedTextContainer _container;     // page container
        private List<FixedPageStructure>  _pageStructures; // contain all FixedPageStructure for each page.
        private int _nextScopeId;                           // Next FlowNode ScopeId
        private FixedFlowMap _fixedFlowMap;
        private static bool[] _cTable = new bool[] { true, //0x5BF   //Combining marks table
                false, true, true, false, // 0x5C0 - 3
                true, true, false, true, //0x5C4
                true, true, //0x6D6 - 7
                true, true, true, true, //0x6D8 - B
                true, false, false, true, //C - F
                true, true, true, true, //0x6E0 - 3
                true, false, false, true, //0x6E4 - 7
                true, false, true, true, //0x6E8 - B
                true, true //C, D
            };
        #endregion Private Fields

        //--------------------------------------------------------------------
        //
        // Private Class
        //
        //--------------------------------------------------------------------

        #region Private Class
        internal sealed class FlowModelBuilder
        {
            private sealed class LogicalHyperlink
            {
                public LogicalHyperlink(Uri uri, Geometry geom, UIElement uiElement)
                {
                    _uiElement = uiElement;
                    _uri = uri;
                    _geometry = geom;
                    _boundingRect = geom.Bounds; //Get bounding rect
                    _used = false;
                }

                public Uri Uri
                {
                    get
                    {
                        return _uri;
                    }
                }

                public Geometry Geometry
                {
                    get
                    {
                        return _geometry;
                    }
                }

                public Rect BoundingRect
                {
                    get
                    {
                        return _boundingRect;
                    }
                }

                public UIElement UIElement
                {
                    get
                    {
                        return _uiElement;
                    }
                }

                public bool Used
                {
                    get
                    {
                        return _used;
                    }
                    set
                    {
                        _used = value;
                    }
                }


                private UIElement _uiElement;
                private Uri _uri;
                private Geometry _geometry;
                private Rect _boundingRect;
                private bool _used;
            }

            private sealed class LogicalHyperlinkContainer : IEnumerable<LogicalHyperlink>
            {
                public LogicalHyperlinkContainer()
                {
                    _hyperlinks = new List<LogicalHyperlink>();
                }

                IEnumerator<LogicalHyperlink> IEnumerable<LogicalHyperlink>.GetEnumerator()
                {
                    return _hyperlinks.GetEnumerator();
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return _hyperlinks.GetEnumerator();
                }

                public void AddLogicalHyperlink(Uri uri, Geometry geometry, UIElement uiElement)
                {
                    LogicalHyperlink hyperlink = new LogicalHyperlink(uri, geometry, uiElement);
                    _hyperlinks.Add(hyperlink);
                }

                //Returns a Uri that is associated with a SOMElement if there is one
                //Also returns the actual UIElement as an output variable if a Path with NavigateUri
                //is used to make this SOMElement look like an actual hyperlink
                public Uri GetUri(FixedSOMElement element, FixedPage p, out UIElement shadowElement)
                {
                    shadowElement = null;
                    UIElement e = p.GetElement(element.FixedNode) as UIElement;
                    if (e == null)
                    {
                        Debug.Assert(false);
                        return null;
                    }
                    LogicalHyperlink logicalHyperlink = null;
                    Uri relUri = FixedPage.GetNavigateUri(e);
                    if (relUri == null && _hyperlinks.Count > 0)
                    {
                        Transform t = e.TransformToAncestor(p) as Transform;
                        Geometry g;
                        if (e is Glyphs)
                        {
                            GlyphRun run = ((Glyphs)e).ToGlyphRun();
                            Rect designRect = run.ComputeAlignmentBox();
                            designRect.Offset(run.BaselineOrigin.X, run.BaselineOrigin.Y);

                            g = new RectangleGeometry(designRect);
                        }
                        else if (e is Path)
                        {
                            g = ((Path)e).Data;
                        }
                        else
                        {
                            Debug.Assert(e is Image);
                            Image im = (Image)e;
                            g = new RectangleGeometry(new Rect(0, 0, im.Width, im.Height));
                        }
                        logicalHyperlink = _GetHyperlinkFromGeometry(g, t);
                        if (logicalHyperlink != null)
                        {
                            relUri = logicalHyperlink.Uri;
                            shadowElement = logicalHyperlink.UIElement;
                        }
                    }
                    if (relUri == null)
                    {
                        return null;
                    }

                    return FixedPage.GetLinkUri(p, relUri);
                }


                //Marks the hyperlink associated with a specific UIElement as "used"
                public void MarkAsUsed(UIElement uiElement)
                {
                    for (int i=0; i<_hyperlinks.Count; i++)
                    {
                        LogicalHyperlink hyperlink = _hyperlinks[i];
                        if (hyperlink.UIElement == uiElement)
                        {
                            hyperlink.Used = true;
                            break;
                        }
                    }
                }

                private LogicalHyperlink _GetHyperlinkFromGeometry(Geometry geom, Transform t)
                {
                    Geometry g = geom;

                    if (t != null && !t.Value.IsIdentity)
                    {
                        g = PathGeometry.CreateFromGeometry(geom);
                        g.Transform = t;
                    }

                    double minArea = g.GetArea() * .99;
                    Rect r = g.Bounds;

                    for (int i = 0; i < _hyperlinks.Count; i++)
                    {
                        // do fast rect overlap before doing more expensive geometric comparison
                        if (r.IntersectsWith(_hyperlinks[i].BoundingRect))
                        {
                            Geometry combined = Geometry.Combine(g, _hyperlinks[i].Geometry, GeometryCombineMode.Intersect, Transform.Identity);
                            if (combined.GetArea() > minArea)
                            {
                                return _hyperlinks[i];
                            }
                        }
                    }

                    return null;
                }

                private List<LogicalHyperlink> _hyperlinks;
            }

            public FlowModelBuilder(FixedTextBuilder builder, FixedPageStructure pageStructure, FixedPage page)
            {
                _builder = builder;
                _container = builder._container;
                _pageIndex = pageStructure.PageIndex;
                _textRuns = new List<FixedSOMTextRun>();
                _flowNodes = new List<FlowNode>();
                _fixedNodes = new List<FixedNode>();
                _nodesInLine = new List<FixedNode>();
                _lineResults = new List<FixedLineResult>();
                _endNodes = new Stack();
                _fixedElements = new Stack();
                _mapping = builder._fixedFlowMap;
                _pageStructure = pageStructure;
                _currentFixedElement = _container.ContainerElement;
                _lineLayoutBox = Rect.Empty;
                _logicalHyperlinkContainer = new LogicalHyperlinkContainer();
                _fixedPage = page;
#if DEBUG
                _dumpDoc = new XmlDocument();
                _currentDumpNode = _dumpDoc.CreateElement("FlowModel");
                _dumpDoc.AppendChild(_currentDumpNode);
#endif

            }


            public void FindHyperlinkPaths(FrameworkElement elem)
            {
                //we are only interested in hyperlinks created by putting Path on top of a Gyphs element

                Debug.Assert(elem is FixedPage || elem is Canvas);
                IEnumerable children = LogicalTreeHelper.GetChildren(elem);

                foreach (UIElement child in children)
                {
                    Canvas canvas = child as Canvas;
                    if (canvas != null)
                    {
                        FindHyperlinkPaths(canvas);
                    }

                    if (!(child is Path) || ((Path)child).Fill is ImageBrush)
                    {
                        // ignore these, these are content
                        continue;
                    }

                    Uri navUri = FixedPage.GetNavigateUri(child);

                    if (navUri != null && ((Path)child).Data != null)
                    {
                        Transform trans = child.TransformToAncestor(_fixedPage) as Transform;

                        Geometry geom = ((Path)child).Data;
                        if (trans != null && !trans.Value.IsIdentity)
                        {
                            geom = PathGeometry.CreateFromGeometry(geom);
                            geom.Transform = trans;
                        }
                        _logicalHyperlinkContainer.AddLogicalHyperlink(navUri, geom, child);
                    }
                }
            }

            public void AddLeftoverHyperlinks()
            {
                foreach (LogicalHyperlink hyperlink in _logicalHyperlinkContainer)
                {
                    if (!hyperlink.Used)
                    {
                        _AddStartNode(FixedElement.ElementType.Paragraph);
                        _AddStartNode(FixedElement.ElementType.Hyperlink);
                        _currentFixedElement.SetValue(Hyperlink.NavigateUriProperty, hyperlink.Uri);
                        _currentFixedElement.SetValue(FixedElement.HelpTextProperty, (String) (hyperlink.UIElement.GetValue(AutomationProperties.HelpTextProperty)));
                        _currentFixedElement.SetValue(FixedElement.NameProperty, (String) (hyperlink.UIElement.GetValue(AutomationProperties.NameProperty)));
                        _AddEndNode();
                        _AddEndNode();
                    }
                }
            }
            //Use this for start tags
            public void AddStartNode(FixedElement.ElementType type)
            {
                _FinishTextRun(true);
                _FinishHyperlink();
                _AddStartNode(type);
            }

            public void AddEndNode()
            {
                _FinishTextRun(false);
                _FinishHyperlink();
                _AddEndNode();
            }
            //Use this for images and text runs -- generates necessary flow nodes
            public void AddElement(FixedSOMElement element)
            {
                FixedPage page = _builder.GetFixedPage(element.FixedNode);
                UIElement shadowHyperlink;
                Uri navUri = _logicalHyperlinkContainer.GetUri(element, page, out shadowHyperlink);
                if (element is FixedSOMTextRun)
                {
                    // Will add code to get font info for rich copy here
                    FixedSOMTextRun run = element as FixedSOMTextRun;
                    bool createNewRun = (_currentRun == null) || (!run.HasSameRichProperties(_currentRun))
                        || navUri != _currentNavUri || (navUri != null && navUri.ToString() != _currentNavUri.ToString());

                    if (createNewRun)
                    {
                        if (_currentRun != null)
                        {
                            //Close existing inline tag
                            FixedSOMFixedBlock parent = run.FixedBlock;

                            FixedSOMTextRun lastRun = _textRuns[_textRuns.Count - 1];

                            Glyphs currentRunGlyph = _builder.GetGlyphsElement(lastRun.FixedNode);
                            Glyphs glyphs = _builder.GetGlyphsElement(run.FixedNode);

                            GlyphComparison comparison = _builder._CompareGlyphs(currentRunGlyph, glyphs);

                            bool addSpace = false;
                            if (_builder._IsNonContiguous(lastRun, run, comparison))
                            {
                                addSpace = true;
                            }

                            _FinishTextRun(addSpace);
                        }

                        _SetHyperlink(navUri, run.FixedNode, shadowHyperlink);
                        //Open new Run tag and set RTF props

                        _AddStartNode(FixedElement.ElementType.Run);
                        run.SetRTFProperties(_currentFixedElement);
                        _currentRun = run;
                    }

                    _textRuns.Add((FixedSOMTextRun)element);
                    if (_fixedNodes.Count == 0 || _fixedNodes[_fixedNodes.Count - 1] != element.FixedNode)
                    {
                        _fixedNodes.Add(element.FixedNode);
                    }
                }
                else if (element is FixedSOMImage)
                {
                    FixedSOMImage image = (FixedSOMImage)element;
                    _FinishTextRun(true);
                    _SetHyperlink(navUri, image.FixedNode, shadowHyperlink);

                    _AddStartNode(FixedElement.ElementType.InlineUIContainer);

                    FlowNode flowImageNode = new FlowNode(_NewScopeId(), FlowNodeType.Object, null);
                    // Create a new FixedElement to represent this new node.
                    _container.OnNewFlowElement(_currentFixedElement,
                                    FixedElement.ElementType.Object,
                                    new FlowPosition(_container, flowImageNode, 0),
                                    new FlowPosition(_container, flowImageNode, 1),
                                    image.Source,
                                    _pageIndex
                                    );


                    _flowNodes.Add(flowImageNode);

                    // Do fixed/flow mapping for image
                    element.FlowNode = flowImageNode;
                    flowImageNode.FixedSOMElements = new FixedSOMElement[] { element };
                    _mapping.AddFixedElement(element);
                    // Do we need this?
                    _fixedNodes.Add(element.FixedNode);

                    //copy automation properties
                    FixedElement fElement = (FixedElement)flowImageNode.Cookie;
                    fElement.SetValue(FixedElement.NameProperty, image.Name);
                    fElement.SetValue(FixedElement.HelpTextProperty, image.HelpText);

                    _AddEndNode();
                }
                else
                {
                    Debug.Assert(false);
                }
            }

            public void FinishMapping()
            {
                _FinishLine();
                _mapping.MappingReplace(_pageStructure.FlowStart, _flowNodes);
                _pageStructure.SetFlowBoundary(_flowNodes[0], _flowNodes[_flowNodes.Count-1]);
                _pageStructure.SetupLineResults(_lineResults.ToArray());
            }

#if DEBUG
            public void DumpToFile(string file)
            {
                //_dumpDoc.Save(file);
                StringWriter writer = new StringWriter();
                _dumpDoc.Save(writer);
            }

            private void DumpFlowNode(FlowNode node)
            {
                FixedElement element = node.Cookie as FixedElement;
                if (element != null)
                {
                    XmlElement newDumpNode = _dumpDoc.CreateElement(element.Type.Name);
                    newDumpNode.SetAttribute("id", node.ScopeId.ToString());
                    _currentDumpNode.AppendChild(newDumpNode);
                    _currentDumpNode = newDumpNode;
                }
                else
                {
                    //Run
                    int nodeLength = (int) node.Cookie;
                    _currentDumpNode.SetAttribute("Cookie", nodeLength.ToString());
                    FixedSOMElement[] somElems = node.FixedSOMElements;
                    if (somElems!=null)
                    {
                        StringBuilder strBuilder = new StringBuilder();
                        foreach (FixedSOMElement somElem in somElems)
                        {
                            FixedSOMTextRun run = somElem as FixedSOMTextRun;
                            if (run != null)
                            {
                                strBuilder.Append(run.Text);
                            }
                        }
                        _currentDumpNode.InnerText = strBuilder.ToString();
                    }
                }

            }
#endif

            private void _AddStartNode(FixedElement.ElementType type)
            {
                FlowNode startNode = new FlowNode(_NewScopeId(), FlowNodeType.Start, _pageIndex);
                FlowNode endNode = new FlowNode(_NewScopeId(), FlowNodeType.End, _pageIndex);
                // add fixed element
                _container.OnNewFlowElement(_currentFixedElement, //_container.ContainerElement,
                type,
                new FlowPosition(_container, (FlowNode)startNode, 1),
                new FlowPosition(_container, (FlowNode)endNode, 0),
                null,
                _pageIndex
                );
                // Create fixed element
                _fixedElements.Push(_currentFixedElement);
                _currentFixedElement = (FixedElement)startNode.Cookie;
                _flowNodes.Add(startNode);
                _endNodes.Push(endNode);
#if DEBUG
                DumpFlowNode(startNode);
#endif


            }

            private void _AddEndNode()
            {
                _flowNodes.Add((FlowNode)_endNodes.Pop());
                _currentFixedElement = (FixedElement)_fixedElements.Pop();
#if DEBUG
                _currentDumpNode = (XmlElement) _currentDumpNode.ParentNode;
#endif
            }

            private void _FinishTextRun(bool addSpace)
            {
                if (_textRuns.Count > 0)
                {
                    int textRunLength = 0;
                    FixedSOMTextRun run = null;
                    for (int i=0; i<_textRuns.Count; i++)
                    {
                        run = _textRuns[i];
                        Glyphs glyphs = _builder.GetGlyphsElement(run.FixedNode);
                        GlyphComparison comparison = _builder._CompareGlyphs(_lastGlyphs, glyphs);

                        if (comparison == GlyphComparison.DifferentLine)
                        {
                            _FinishLine();
                        }

                        _lastGlyphs = glyphs;

                        _lineLayoutBox.Union(run.BoundingRect);
                        run.LineIndex = _lineResults.Count;
                        if (_nodesInLine.Count == 0 || _nodesInLine[_nodesInLine.Count - 1] != run.FixedNode)
                        {
                            _nodesInLine.Add(run.FixedNode);
                        }

                        textRunLength += run.EndIndex - run.StartIndex;
                        Debug.Assert(run.EndIndex - run.StartIndex == run.Text.Length);

                        if (i>0 && _builder._IsNonContiguous(_textRuns[i-1], run, comparison))
                        {
                            _textRuns[i-1].Text = _textRuns[i-1].Text + " ";
                            textRunLength++;
                        }
                    }
                    if (addSpace && run.Text.Length>0 && !run.Text.EndsWith(" ", StringComparison.Ordinal) && !IsHyphen(run.Text[run.Text.Length - 1]))
                    {
                        run.Text = run.Text + " ";
                        textRunLength ++;
                    }

                    if (textRunLength != 0)
                    {
                        FlowNode flowNodeRun = new FlowNode(_NewScopeId(), FlowNodeType.Run, textRunLength);
                        // Add list of text runs to flow node
                        flowNodeRun.FixedSOMElements = _textRuns.ToArray();

                        int offset = 0;

                        foreach (FixedSOMTextRun textRun in _textRuns)
                        {
                            textRun.FlowNode = flowNodeRun;
                            textRun.OffsetInFlowNode = offset;
                            _mapping.AddFixedElement(textRun);

                            offset += textRun.Text.Length;
                        }

                        //Debug.Assert(offset == textRunLength);

                        _flowNodes.Add(flowNodeRun);
#if DEBUG
                        DumpFlowNode(flowNodeRun);
#endif

                        // clear the list
                        _textRuns.Clear();
                    }
                }

                //Close the inline tag if any
                if (_currentRun != null)
                {
                    _AddEndNode();
                    _currentRun = null;
                }
            }

            private void _FinishHyperlink()
            {
                if (_currentNavUri != null)
                {
                    _AddEndNode(); // </Hyperlink>
                    _currentNavUri = null;
                }
            }

            private void _SetHyperlink(Uri navUri, FixedNode node, UIElement shadowHyperlink)
            {
                if (navUri != _currentNavUri || (navUri != null && navUri.ToString() != _currentNavUri.ToString()))
                {
                    if (_currentNavUri != null)
                    {
                        _AddEndNode(); // </Hyperlink>
                    }

                    if (navUri != null)
                    {
                        _AddStartNode(FixedElement.ElementType.Hyperlink);
                        _currentFixedElement.SetValue(Hyperlink.NavigateUriProperty, navUri);
                        UIElement uiElement = _fixedPage.GetElement(node) as UIElement;
                        Debug.Assert(uiElement != null);
                        if (uiElement != null)
                        {
                            _currentFixedElement.SetValue(FixedElement.HelpTextProperty, (String) (uiElement.GetValue(AutomationProperties.HelpTextProperty)));
                            _currentFixedElement.SetValue(FixedElement.NameProperty, (String) (uiElement.GetValue(AutomationProperties.NameProperty)));
                            if (shadowHyperlink != null)
                            {
                                _logicalHyperlinkContainer.MarkAsUsed(shadowHyperlink);
                            }
                        }
                    }

                    _currentNavUri = navUri;
                }
            }

            private void _FinishLine()
            {
                if (_nodesInLine.Count > 0)
                {
                    FixedLineResult newLineResult = new FixedLineResult(_nodesInLine.ToArray(), _lineLayoutBox);
                    Debug.Assert(newLineResult != null);

                    _lineResults.Add(newLineResult);

                    _nodesInLine.Clear();
                    _lineLayoutBox = Rect.Empty;
                }
            }

            private int _NewScopeId()
            {
                return _builder._nextScopeId++;
            }

            public FixedElement FixedElement
            {
                get
                {
                    return _currentFixedElement;
                }
            }

            private int _pageIndex;
            private FixedTextContainer _container;
            private FixedTextBuilder _builder;
            private List<FixedSOMTextRun> _textRuns;
            private List<FlowNode> _flowNodes;
            private List<FixedNode> _fixedNodes;
            private List<FixedNode> _nodesInLine;
            private List<FixedLineResult> _lineResults;
            private Rect _lineLayoutBox;
            private Stack _endNodes;
            private Stack _fixedElements;
            private FixedElement _currentFixedElement;
            private FixedFlowMap _mapping;
            private FixedPageStructure _pageStructure;
            private Glyphs _lastGlyphs;
            private FixedSOMTextRun _currentRun;
            private LogicalHyperlinkContainer _logicalHyperlinkContainer;
            private FixedPage _fixedPage;
            private Uri _currentNavUri;
#if DEBUG
            private XmlDocument _dumpDoc;
            private XmlElement _currentDumpNode;
#endif

        }


        internal enum GlyphComparison
        {
            DifferentLine,
            SameLine,
            Adjacent, //also on same line
            Unknown
        }

        #endregion
    }
}
