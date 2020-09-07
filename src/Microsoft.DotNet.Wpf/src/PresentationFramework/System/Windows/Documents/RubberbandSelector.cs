// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Windows.Documents
{
    using MS.Internal;                          // For Invariant.Assert
    using MS.Internal.Documents;
    using System.Windows;                       // DependencyID etc.
    using System.Windows.Controls;              // Canvas
    using System.Collections;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Media.TextFormatting;  // CharacterHit
    using System.Windows.Shapes;                // Glyphs
    using System.Windows.Markup;
    using System.Windows.Input;
    using System.Threading;
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Security ;
    using System.Diagnostics;


    //=====================================================================
    /// <summary>
    /// Class has a function similar to that of TextEditor.  It can be attached
    /// to a PageGrid by PageViewer to enable rubber band selection.  It should not
    /// be attached at the same time TextEditor is attached.
    /// </summary>
    internal sealed class RubberbandSelector
    {
        #region Internal Methods
        /// <summary>
        /// Clears current selection
        /// </summary>
        internal void ClearSelection()
        {
            if (HasSelection)
            {
                FixedPage p = _page;
                _page = null;
                UpdateHighlightVisual(p);
            }
            _selectionRect = Rect.Empty;
        }

        /// <summary>
        /// Attaches selector to scope to start rubberband selection mode
        /// </summary>
        /// <param name="scope">the scope, typically a DocumentGrid</param>
        internal void AttachRubberbandSelector(FrameworkElement scope)
        {
            if (scope == null)
            {
                throw new ArgumentNullException("scope");
            }

            ClearSelection();
            scope.MouseLeftButtonDown += new MouseButtonEventHandler(OnLeftMouseDown);
            scope.MouseLeftButtonUp += new MouseButtonEventHandler(OnLeftMouseUp);
            scope.MouseMove += new MouseEventHandler(OnMouseMove);
            scope.QueryCursor += new QueryCursorEventHandler(OnQueryCursor);
            scope.Cursor = null; // Cursors.Cross;

            //If the passed-in scope is DocumentGrid, we want to
            //attach our commands to its DocumentViewerOwner, since
            //DocumentGrid is not focusable by default.
            if (scope is DocumentGrid)
            {
                _uiScope = ((DocumentGrid)scope).DocumentViewerOwner;
                Invariant.Assert(_uiScope != null, "DocumentGrid's DocumentViewerOwner cannot be null.");
            }
            else
            {
                _uiScope = scope;
            }

            //Attach the RubberBandSelector's Copy command to the UIScope.
            CommandBinding binding = new CommandBinding(ApplicationCommands.Copy);
            binding.Executed += new ExecutedRoutedEventHandler(OnCopy);
            binding.CanExecute += new CanExecuteRoutedEventHandler(QueryCopy);
            _uiScope.CommandBindings.Add(binding);

            _scope = scope;
        }

        /// <summary>
        /// Removes rubberband selector from its scope -- gets out of rubberband selection mode
        /// </summary>
        internal void DetachRubberbandSelector()
        {
            ClearSelection();

            if (_scope != null)
            {
                _scope.MouseLeftButtonDown -= new MouseButtonEventHandler(OnLeftMouseDown);
                _scope.MouseLeftButtonUp -= new MouseButtonEventHandler(OnLeftMouseUp);
                _scope.MouseMove -= new MouseEventHandler(OnMouseMove);
                _scope.QueryCursor -= new QueryCursorEventHandler(OnQueryCursor);
                _scope = null;
            }

            if (_uiScope != null)
            {
                CommandBindingCollection commandBindings = _uiScope.CommandBindings;
                foreach (CommandBinding binding in commandBindings)
                {
                    if (binding.Command == ApplicationCommands.Copy)
                    {
                        binding.Executed -= new ExecutedRoutedEventHandler(OnCopy);
                        binding.CanExecute -= new CanExecuteRoutedEventHandler(QueryCopy);
                    }
                }
                _uiScope = null;
            }
        }
        #endregion Internal Methods

        #region Private Methods
        // extends current selection to point
        private void ExtendSelection(Point pt)
        {
            // clip to page
            Size pageSize = _panel.ComputePageSize(_page);

            if (pt.X < 0)
            {
                pt.X = 0;
            }
            else if (pt.X > pageSize.Width)
            {
                pt.X = pageSize.Width;
            }

            if (pt.Y < 0)
            {
                pt.Y = 0;
            }
            else if (pt.Y > pageSize.Height)
            {
                pt.Y = pageSize.Height;
            }

            //create rectangle extending from selection origin to current point
            _selectionRect = new Rect(_origin, pt);

            UpdateHighlightVisual(_page);
        }

        //redraws highlights on page
        private void UpdateHighlightVisual(FixedPage page)
        {
            if (page != null)
            {
                HighlightVisual hv = HighlightVisual.GetHighlightVisual(page);
                if (hv != null)
                {
                    hv.UpdateRubberbandSelection(this);
                }
            }
        }

        private void OnCopy(object sender, ExecutedRoutedEventArgs e)
        {
            if (HasSelection && _selectionRect.Width > 0 && _selectionRect.Height > 0)
            {
                //Copy to clipboard
                IDataObject dataObject;
                string textString = GetText();
                object bmp = null;

                bmp = SystemDrawingHelper.GetBitmapFromBitmapSource(GetImage());

                dataObject = new DataObject();
                // Order of data is irrelevant, the pasting application will determine format
                dataObject.SetData(DataFormats.Text, textString, true);
                dataObject.SetData(DataFormats.UnicodeText, textString, true);
                if (bmp != null)
                {
                    dataObject.SetData(DataFormats.Bitmap, bmp, true);
                }

                try
                {
                    Clipboard.SetDataObject(dataObject, true);
                }
                catch (System.Runtime.InteropServices.ExternalException)
                {
                    // Clipboard is failed to set the data object.
                    return;
                }
            }
        }

        //gets snapshot image
        private BitmapSource GetImage()
        {
            //get copy of page
            Visual v = GetVisual(-_selectionRect.Left, -_selectionRect.Top);

            //create image of appropriate size
            double dpi = 96; // default screen dpi, in fact no other dpi seems to work if you want something at 100% scale
            double scale = dpi / 96.0;
            RenderTargetBitmap data = new RenderTargetBitmap((int)(scale * _selectionRect.Width), (int)(scale * _selectionRect.Height),dpi,dpi, PixelFormats.Pbgra32);

            data.Render(v);
            return data;
        }

        private Visual GetVisual(double offsetX, double offsetY)
        {
            ContainerVisual root = new ContainerVisual();
            DrawingVisual visual = new DrawingVisual();

            root.Children.Add(visual);
            visual.Offset  = new Vector(offsetX, offsetY);

            DrawingContext dc = visual.RenderOpen();
            dc.DrawDrawing(_page.GetDrawing());
            dc.Close();

            UIElementCollection vc = _page.Children;
            foreach (UIElement child in vc)
            {
                CloneVisualTree(visual, child);
            }

            return root;
        }

        private void CloneVisualTree(ContainerVisual parent, Visual old)
        {
            DrawingVisual visual = new DrawingVisual();
            parent.Children.Add(visual);

            visual.Clip = VisualTreeHelper.GetClip(old);
            visual.Offset = VisualTreeHelper.GetOffset(old);
            visual.Transform = VisualTreeHelper.GetTransform(old);
            visual.Opacity = VisualTreeHelper.GetOpacity(old);
            visual.OpacityMask = VisualTreeHelper.GetOpacityMask(old);

#pragma warning disable 0618
            visual.BitmapEffectInput = VisualTreeHelper.GetBitmapEffectInput(old);
            visual.BitmapEffect = VisualTreeHelper.GetBitmapEffect(old);
#pragma warning restore 0618

            // snapping guidelines??

            DrawingContext dc = visual.RenderOpen();
            dc.DrawDrawing(old.GetDrawing());
            dc.Close();

            int count = VisualTreeHelper.GetChildrenCount(old);
            for(int i = 0; i < count; i++)
            {
                Visual child = old.InternalGetVisualChild(i);
                CloneVisualTree(visual, child);
            }
        }
        //gets text within selected area
        private string GetText()
        {
            double top = _selectionRect.Top;
            double bottom = _selectionRect.Bottom;
            double left = _selectionRect.Left;
            double right = _selectionRect.Right;

            double lastBaseline = 0;
            double baseline = 0;
            double lastHeight = 0;
            double height = 0;

            int nChildren = _page.Children.Count;
            ArrayList ranges = new ArrayList(); //text ranges in area

            FixedNode[] nodesInLine = _panel.FixedContainer.FixedTextBuilder.GetFirstLine(_pageIndex);

            while (nodesInLine != null && nodesInLine.Length > 0)
            {
                TextPositionPair textRange = null; //current text range

                foreach (FixedNode node in nodesInLine)
                {
                    Glyphs g = _page.GetGlyphsElement(node);
                    if (g != null)
                    {
                        int begin, end; //first and last index in range
                        bool includeEnd; //is the end of this glyphs included in selection?
                        if (IntersectGlyphs(g, top, left, bottom, right, out begin, out end, out includeEnd, out baseline, out height))
                        {
                            if (textRange == null || begin > 0)
                            {
                                //begin new text range
                                textRange = new TextPositionPair();
                                textRange.first = _GetTextPosition(node, begin);
                                ranges.Add(textRange);
                            }

                            textRange.second = _GetTextPosition(node, end);

                            if (!includeEnd)
                            {
                                // so future textRanges aren't concatenated with this one
                                textRange = null;
                            }
                        }
                        else
                        {
                            //this Glyphs completely outside selected region
                            textRange = null;
                        }
                        lastBaseline = baseline;
                        lastHeight = height;
                    }
                }
                int count = 1;
                nodesInLine = _panel.FixedContainer.FixedTextBuilder.GetNextLine(nodesInLine[0], true, ref count);
            }

            string text = "";
            foreach (TextPositionPair range in ranges)
            {
                Debug.Assert(range.first != null && range.second != null);
                text = text + TextRangeBase.GetTextInternal(range.first, range.second) + "\r\n"; //CRLF
            }

            return text;
        }

        private ITextPointer _GetTextPosition(FixedNode node, int charIndex)
        {
            FixedPosition fixedPosition = new FixedPosition(node, charIndex);

            // Create a FlowPosition to represent this fixed position
            FlowPosition flowHit = _panel.FixedContainer.FixedTextBuilder.CreateFlowPosition(fixedPosition);
            if (flowHit != null)
            {
                // Create a TextPointer from the flow position
                return new FixedTextPointer(false, LogicalDirection.Forward, flowHit);
            }
            return null;
        }


        //determines whether and where a rectangle intersects a Glyphs
        private bool IntersectGlyphs(Glyphs g, double top, double left, double bottom, double right, out int begin, out int end, out bool includeEnd, out double baseline, out double height)
        {
            begin = 0;
            end = 0;
            includeEnd = false;

            GlyphRun run = g.ToGlyphRun();
            Rect boundingRect = run.ComputeAlignmentBox();
            boundingRect.Offset(run.BaselineOrigin.X, run.BaselineOrigin.Y);

            //useful for same line detection
            baseline = run.BaselineOrigin.Y;
            height = boundingRect.Height;

            double centerLine = boundingRect.Y + .5 * boundingRect.Height;
            GeneralTransform t = g.TransformToAncestor(_page);

            Point pt1;
            t.TryTransform(new Point(boundingRect.Left, centerLine), out pt1);
            Point pt2;
            t.TryTransform(new Point(boundingRect.Right, centerLine), out pt2);

            double dStart, dEnd;

            bool cross = false;
            if (pt1.X < left)
            {
                if (pt2.X < left)
                {
                    return false;
                }
                cross = true;
            }
            else if (pt1.X > right)
            {
                if (pt2.X > right)
                {
                    return false;
                }
                cross = true;
            }
            else if (pt2.X < left || pt2.X > right)
            {
                cross = true;
            }

            if (cross)
            {
                double d1 = (left - pt1.X) / (pt2.X - pt1.X);
                double d2 = (right - pt1.X) / (pt2.X - pt1.X);
                if (d2 > d1)
                {
                    dStart = d1;
                    dEnd = d2;
                }
                else
                {
                    dStart = d2;
                    dEnd = d1;
                }
            }
            else
            {
                dStart = 0;
                dEnd = 1;
            }

            cross = false;
            if (pt1.Y < top)
            {
                if (pt2.Y < top)
                {
                    return false;
                }
                cross = true;
            }
            else if (pt1.Y > bottom)
            {
                if (pt2.Y > bottom)
                {
                    return false;
                }
                cross = true;
            }
            else if (pt2.Y < top || pt2.Y > bottom)
            {
                cross = true;
            }

            if (cross)
            {
                double d1 = (top - pt1.Y) / (pt2.Y - pt1.Y);
                double d2 = (bottom - pt1.Y) / (pt2.Y - pt1.Y);
                if (d2 > d1)
                {
                    if (d1 > dStart)
                    {
                        dStart = d1;
                    }
                    if (d2 < dEnd)
                    {
                        dEnd = d2;
                    }
                }
                else
                {
                    if (d2 > dStart)
                    {
                    dStart = d2;
                    }
                    if (d1 < dEnd)
                    {
                        dEnd = d1;
                    }
                }
            }

            dStart = boundingRect.Left + boundingRect.Width * dStart;
            dEnd = boundingRect.Left + boundingRect.Width * dEnd;

            bool leftToRight = ((run.BidiLevel & 1) == 0);

            begin = GlyphRunHitTest(run, dStart, leftToRight);
            end = GlyphRunHitTest(run, dEnd, leftToRight);

            if (begin > end)
            {
                int temp = begin;
                begin = end;
                end = temp;
            }

            Debug.Assert(end >= begin);

            int characterCount = (run.Characters == null) ? 0 : run.Characters.Count;
            includeEnd = (end == characterCount);

            return true;
        }

        //Returns the character offset in a GlyphRun given an X position
        private int GlyphRunHitTest(GlyphRun run, double xoffset, bool LTR)
        {
            bool isInside;
            double distance = LTR ? xoffset - run.BaselineOrigin.X : run.BaselineOrigin.X - xoffset;
            CharacterHit hit = run.GetCaretCharacterHitFromDistance(distance, out isInside);
            return hit.FirstCharacterIndex + hit.TrailingLength;
        }

        //queryenabled handler for copy command
        private void QueryCopy(object sender, CanExecuteRoutedEventArgs e)
        {
            if (HasSelection)
            {
                e.CanExecute = true;
            }
        }

        private void OnLeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            FixedDocumentPage dp = GetFixedPanelDocumentPage(e.GetPosition(_scope));
            if (dp != null)
            {
                //give focus to the UI Scope so that actions like
                //Copy will work after making a selection.
                _uiScope.Focus();

                //turn on mouse capture
                _scope.CaptureMouse();

                ClearSelection();

                //mark start position
                _panel = dp.Owner;
                _page = dp.FixedPage;
                _isSelecting = true;
                _origin = e.GetPosition(_page);
                _pageIndex = dp.PageIndex;
            }
        }

        private void OnLeftMouseUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            _scope.ReleaseMouseCapture();

            if (_isSelecting)
            {
                _isSelecting = false;
                if (_page != null)
                {
                    ExtendSelection(e.GetPosition(_page));
                }
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = true;

            if (e.LeftButton == MouseButtonState.Released)
            {
                _isSelecting = false;
            }
            else if (_isSelecting)
            {
                if (_page != null)
                {
                    ExtendSelection(e.GetPosition(_page));
                }
            }
        }

        private void OnQueryCursor(object sender, QueryCursorEventArgs e)
        {
            if (_isSelecting || GetFixedPanelDocumentPage(e.GetPosition(_scope)) != null)
            {
                e.Cursor = Cursors.Cross;
            }
            else
            {
                e.Cursor = Cursors.Arrow;
            }

            e.Handled = true;
        }

        private FixedDocumentPage GetFixedPanelDocumentPage(Point pt)
        {
            DocumentGrid mpScope = _scope as DocumentGrid;
            if (mpScope != null)
            {
                DocumentPage dp = mpScope.GetDocumentPageFromPoint(pt);
                FixedDocumentPage fdp = dp as FixedDocumentPage;
                if (fdp == null)
                {
                    FixedDocumentSequenceDocumentPage fdsdp = dp as FixedDocumentSequenceDocumentPage;
                    if (fdsdp != null)
                    {
                        fdp = fdsdp.ChildDocumentPage as FixedDocumentPage;
                    }
                }
                return fdp;
            }
            return null;
        }
        #endregion Private Methods

        #region Internal Properties
        internal FixedPage Page
        {
            get { return _page; }
        }

        internal Rect SelectionRect
        {
            get { return _selectionRect; }
        }

        internal bool HasSelection
        {
            get { return _page != null && _panel != null && !_selectionRect.IsEmpty; }
        }
        #endregion Internal Properties

        #region Private Fields
        private FixedDocument _panel;     // FixedDocument on which we are selecting
        private FixedPage _page;       // page on which we are selecting, or null
        private Rect _selectionRect;   // rectangle in page coordinates, or empty
        private bool _isSelecting;     // true if mouse is down and we are currently drawing the box
        private Point _origin;         // point where we started dragging
        private UIElement _scope;      // element to which we are attached
        private FrameworkElement _uiScope;      // parent of _scope, if _scope is a DocumentGrid.
        private int _pageIndex;        // index of _page
        #endregion Private Fields

        // a lightweight TextRange like class used in GetText.  We needed a class
        // here because we needed this to be a reference type.
        private class TextPositionPair
        {
            public ITextPointer first;
            public ITextPointer second;
        }
    }
}

