// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Implements the FixedTextContainer
//

#pragma warning disable 1634, 1691 // To enable presharp warning disables (#pragma suppress) below.

namespace System.Windows.Documents
{
    using MS.Internal;
    using MS.Internal.Documents;
    using MS.Utility;
    using System.Windows;                // DependencyID etc.
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Markup;
    using System.Windows.Shapes;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Windows.Threading;              // Dispatcher

    //=====================================================================
    /// <summary>
    /// FixedTextContainer is an implementaiton of TextContainer for Fixed Documents
    /// </summary>
    internal sealed class FixedTextContainer : ITextContainer
    {
        //--------------------------------------------------------------------
        //
        // Connstructors
        //
        //---------------------------------------------------------------------

        #region Constructors
        /// <summary>
        ///    FixedTextContainer constructor that sets up the default backing store
        /// </summary>
        /// <param name="parent">
        ///    The object that will be parent of the top level content elements
        /// </param>
        internal FixedTextContainer(DependencyObject parent)
        {
            Debug.Assert(parent != null && parent is FixedDocument);
            _parent = parent;
            _CreateEmptyContainer();
        }
        #endregion Constructors

        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------
        #region Public Methods

        //
        // This is readonly Text OM. All modification methods returns false
        //

        void ITextContainer.BeginChange()
        {
        }

        /// <summary>
        /// <see cref="System.Windows.Documents.ITextContainer.BeginChangeNoUndo"/>
        /// </summary>
        void ITextContainer.BeginChangeNoUndo()
        {
            // We don't support undo, so follow the BeginChange codepath.
            ((ITextContainer)this).BeginChange();
        }

        /// <summary>
        /// </summary>
        void ITextContainer.EndChange()
        {
            ((ITextContainer)this).EndChange(false /* skipEvents */);
        }

        /// <summary>
        /// </summary>
        void ITextContainer.EndChange(bool skipEvents)
        {
        }

        // Allocate a new ITextPointer at the specified offset.
        // Equivalent to this.Start.CreatePointer(offset), but does not
        // necessarily allocate this.Start.
        ITextPointer ITextContainer.CreatePointerAtOffset(int offset, LogicalDirection direction)
        {
            return ((ITextContainer)this).Start.CreatePointer(offset, direction);
        }

        // Not Implemented.
        ITextPointer ITextContainer.CreatePointerAtCharOffset(int charOffset, LogicalDirection direction)
        {
            throw new NotImplementedException();
        }

        ITextPointer ITextContainer.CreateDynamicTextPointer(StaticTextPointer position, LogicalDirection direction)
        {
            return ((ITextPointer)position.Handle0).CreatePointer(direction);
        }

        StaticTextPointer ITextContainer.CreateStaticPointerAtOffset(int offset)
        {
            return new StaticTextPointer(this, ((ITextContainer)this).CreatePointerAtOffset(offset, LogicalDirection.Forward));
        }

        TextPointerContext ITextContainer.GetPointerContext(StaticTextPointer pointer, LogicalDirection direction)
        {
            return ((ITextPointer)pointer.Handle0).GetPointerContext(direction);
        }

        int ITextContainer.GetOffsetToPosition(StaticTextPointer position1, StaticTextPointer position2)
        {
            return ((ITextPointer)position1.Handle0).GetOffsetToPosition((ITextPointer)position2.Handle0);
        }

        int ITextContainer.GetTextInRun(StaticTextPointer position, LogicalDirection direction, char[] textBuffer, int startIndex, int count)
        {
            return ((ITextPointer)position.Handle0).GetTextInRun(direction, textBuffer, startIndex, count);
        }

        object ITextContainer.GetAdjacentElement(StaticTextPointer position, LogicalDirection direction)
        {
            return ((ITextPointer)position.Handle0).GetAdjacentElement(direction);
        }

        DependencyObject ITextContainer.GetParent(StaticTextPointer position)
        {
            return null;
        }

        StaticTextPointer ITextContainer.CreatePointer(StaticTextPointer position, int offset)
        {
            return new StaticTextPointer(this, ((ITextPointer)position.Handle0).CreatePointer(offset));
        }

        StaticTextPointer ITextContainer.GetNextContextPosition(StaticTextPointer position, LogicalDirection direction)
        {
            return new StaticTextPointer(this, ((ITextPointer)position.Handle0).GetNextContextPosition(direction));
        }

        int ITextContainer.CompareTo(StaticTextPointer position1, StaticTextPointer position2)
        {
            return ((ITextPointer)position1.Handle0).CompareTo((ITextPointer)position2.Handle0);
        }

        int ITextContainer.CompareTo(StaticTextPointer position1, ITextPointer position2)
        {
            return ((ITextPointer)position1.Handle0).CompareTo(position2);
        }

        object ITextContainer.GetValue(StaticTextPointer position, DependencyProperty formattingProperty)
        {
            return ((ITextPointer)position.Handle0).GetValue(formattingProperty);
        }

        #endregion Public Methods

        //--------------------------------------------------------------------
        //
        // Public Properties
        //
        //---------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Specifies whether or not the content of this TextContainer may be
        /// modified.
        /// </summary>
        /// <value>
        /// True if content may be modified, false otherwise.
        /// </value>
        /// <remarks>
        /// This TextContainer implementation always returns true.
        /// </remarks>
        bool ITextContainer.IsReadOnly
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// <see cref="TextContainer.Start"/>
        /// </summary>
        ITextPointer ITextContainer.Start
        {
            get
            {
                Debug.Assert(_start != null);
                return _start;
            }
        }


        /// <summary>
        /// <see cref="TextContainer.End"/>
        /// </summary>
        ITextPointer ITextContainer.End
        {
            get
            {
                Debug.Assert(_end != null);
                return _end;
            }
        }

        /// <summary>
        /// Autoincremented counter of content changes in this TextContainer
        /// </summary>
        uint ITextContainer.Generation
        {
            get
            {
                // For read-only content, return some constant value.
                return 0;
            }
        }

        /// <summary>
        /// Collection of highlights applied to TextContainer content.
        /// </summary>
        Highlights ITextContainer.Highlights
        {
            get
            {
                return this.Highlights;
            }
        }

        /// <summary>
        /// <see cref="TextContainer.Parent"/>
        /// </summary>
        DependencyObject ITextContainer.Parent
        {
            get { return _parent; }
        }

        // TextEditor owns setting and clearing this property inside its
        // ctor/OnDetach methods.
        ITextSelection ITextContainer.TextSelection
        {
            get { return this.TextSelection;}
            set { _textSelection = value; }
        }

        // Optional undo manager, always null for this ITextContainer.
        UndoManager ITextContainer.UndoManager { get { return null; } }

        // <see cref="System.Windows.Documents.ITextContainer/>
        ITextView ITextContainer.TextView
        {
            get
            {
                return _textview;
            }

            set
            {
                _textview = value;
            }
        }

        // Count of symbols in this tree, equivalent to this.Start.GetOffsetToPosition(this.End),
        // but doesn't necessarily allocate anything.
        int ITextContainer.SymbolCount
        {
            get
            {
                return ((ITextContainer)this).Start.GetOffsetToPosition(((ITextContainer)this).End);
            }
        }

        // Not implemented.
        int ITextContainer.IMECharCount
        {
            get
            {
                #pragma warning suppress 56503
                throw new NotImplementedException();
            }
        }

        #endregion Public Properties

        //--------------------------------------------------------------------
        //
        // Public Events
        //
        //---------------------------------------------------------------------

        #region Public Events

        public event EventHandler Changing { add { } remove { } }

        public event TextContainerChangeEventHandler Change { add { } remove { } }

        public event TextContainerChangedEventHandler Changed { add { } remove { } }

        #endregion Public Events

        //--------------------------------------------------------------------
        //
        // Internal Methods
        //
        //---------------------------------------------------------------------

        #region Internal Methods

        //--------------------------------------------------------------------
        // Utility Method
        //---------------------------------------------------------------------
        internal FixedTextPointer VerifyPosition(ITextPointer position)
        {
            if (position == null)
            {
                throw new ArgumentNullException("position");
            }

            if (position.TextContainer != this)
            {
                throw new ArgumentException(SR.Get(SRID.NotInAssociatedContainer, "position"));
            }

            FixedTextPointer ftp = position as FixedTextPointer;

            if (ftp == null)
            {
                throw new ArgumentException(SR.Get(SRID.BadFixedTextPosition, "position"));
            }

            return ftp;
        }


        internal int GetPageNumber(ITextPointer  textPointer)
        {
            FixedTextPointer fixedTextPointer = textPointer as FixedTextPointer;
            int  pageNumber = int.MaxValue;

            if (fixedTextPointer != null)
            {
                if (fixedTextPointer.CompareTo(((ITextContainer)this).Start) == 0)
                {
                    pageNumber = 0;
                }
                else if (fixedTextPointer.CompareTo(((ITextContainer)this).End) == 0)
                {
                    pageNumber = this.FixedDocument.PageCount - 1;
                }
                else
                {
                    FlowNode flowNode;
                    int flowOffset;

                    fixedTextPointer.FlowPosition.GetFlowNode(fixedTextPointer.LogicalDirection, out flowNode, out flowOffset);

                    FixedElement fixedElement = flowNode.Cookie as FixedElement;

                    if (flowNode.Type == FlowNodeType.Boundary)
                    {
                        if (flowNode.Fp > 0)
                        {
                            //Document end boundary node
                            pageNumber = this.FixedDocument.PageCount - 1;
                        }
                        else
                        {
                            //Document start boundary node
                            pageNumber = 0;
                        }
                    }
                    else if (flowNode.Type == FlowNodeType.Virtual || flowNode.Type == FlowNodeType.Noop)
                    {
                        pageNumber = (int)flowNode.Cookie;
                    }
                    else if (fixedElement != null)
                    {
                        pageNumber = (int)fixedElement.PageIndex;
                    }
                    else
                    {
                        FixedPosition fixPos;
                        bool res = FixedTextBuilder.GetFixedPosition(fixedTextPointer.FlowPosition, fixedTextPointer.LogicalDirection, out fixPos);
                        Debug.Assert(res);
                        if (res)
                        {
                            pageNumber = fixPos.Page;
                        }
                    }
                }
            }

            return pageNumber;
        }

        // Get the highlights, in Glyphs granularity, that covers this range
        internal void GetMultiHighlights(FixedTextPointer start, FixedTextPointer end, Dictionary<FixedPage, ArrayList> highlights, FixedHighlightType t,
            Brush foregroundBrush, Brush backgroundBrush)
        {
            Debug.Assert(highlights != null);
            if (start.CompareTo(end) > 0)
            {
                // make sure start <= end
                FixedTextPointer temp = start;
                start = end;
                end = temp;
            }

            FixedSOMElement[] elements;
            //Start and end indices in selection for first and last FixedSOMElements respectively
            int startIndex = 0;
            int endIndex = 0;
            if (_GetFixedNodesForFlowRange(start, end, out elements, out startIndex, out endIndex))
            {
                for(int i=0; i<elements.Length; i++)
                {
                    FixedSOMElement elem = elements[i];
                    
                    FixedNode fn = elem.FixedNode;
                    // Get the FixedPage if possible
                    FixedPage page = this.FixedDocument.SyncGetPageWithCheck(fn.Page);
                    if (page == null)
                    {
                        continue;
                    }

                    DependencyObject o = page.GetElement(fn);
                    if (o == null)
                    {
                        continue;
                    }

                    int beginOffset = 0;
                    int endOffset;
                    UIElement e;
                    if (o is Image || o is Path)
                    {
                        e = (UIElement)o;
                        endOffset = 1;
                    }
                    else
                    {
                        Glyphs g = o as Glyphs;
                        if (g == null)
                        {
                            continue;
                        }
                        e = (UIElement)o;
                        beginOffset = elem.StartIndex;
                        endOffset = elem.EndIndex;
                    }

                    if (i == 0)
                    {
                        beginOffset = startIndex;
                    }

                    if (i == elements.Length - 1)
                    {
                        endOffset = endIndex;
                    }

                    ArrayList lfs;
                    if (highlights.ContainsKey(page))
                    {
                        lfs = highlights[page];
                    }
                    else
                    {
                        lfs = new ArrayList();
                        highlights.Add(page, lfs);
                    }

                    FixedSOMTextRun textRun = elem as FixedSOMTextRun;
                    if (textRun != null && textRun.IsReversed)
                    {
                        int oldBeginOffset = beginOffset;
                        beginOffset = elem.EndIndex - endOffset;
                        endOffset = elem.EndIndex - oldBeginOffset;
                    }

                    FixedHighlight fh = new FixedHighlight(e, beginOffset, endOffset, t, foregroundBrush, backgroundBrush);
                    lfs.Add(fh);
                }
            }
        }
        #endregion Internal Methods

        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------

        #region Internal Properties

        // Return the Fixed Document
        internal FixedDocument FixedDocument
        {
            get
            {
                if (_fixedPanel == null)
                {
                    if (_parent is FixedDocument)
                    {
                        _fixedPanel = (FixedDocument)_parent;
                    }
                }

                return _fixedPanel;
            }
        }

        internal FixedTextBuilder FixedTextBuilder
        {
            get
            {
                Debug.Assert(_fixedTextBuilder != null);
                return _fixedTextBuilder;
            }
        }

        internal FixedElement ContainerElement
        {
            get
            {
                Debug.Assert(_containerElement != null);
                return _containerElement;
            }
        }

        /// <summary>
        /// Collection of highlights applied to TextContainer content.
        /// </summary>
        internal Highlights Highlights
        {
            get
            {
                if (_highlights == null)
                {
                    _highlights = new Highlights(this);
                }

                return _highlights;
            }
        }

        // TextSelection associated with this container.
        internal ITextSelection TextSelection
        {
            get
            {
                return _textSelection;
            }
        }

        #endregion Internal Properties

        //--------------------------------------------------------------------
        //
        // Private Methods
        //
        //---------------------------------------------------------------------

        #region Private Methods

        //--------------------------------------------------------------------
        // Initilization
        //---------------------------------------------------------------------
        private void _CreateEmptyContainer()
        {
            // new text builder with map
            _fixedTextBuilder = new FixedTextBuilder(this);

            // create initial TextPointer and container element
            _start = new  FixedTextPointer(false, LogicalDirection.Backward, new FlowPosition(this, this.FixedTextBuilder.FixedFlowMap.FlowStartEdge, 1));
            _end = new FixedTextPointer(false, LogicalDirection.Forward, new FlowPosition(this, this.FixedTextBuilder.FixedFlowMap.FlowEndEdge, 0));

            _containerElement = new FixedElement(FixedElement.ElementType.Container, _start, _end, int.MaxValue);
            _start.FlowPosition.AttachElement(_containerElement);
            _end.FlowPosition.AttachElement(_containerElement);
        }

        internal void OnNewFlowElement(FixedElement parentElement, FixedElement.ElementType elementType, FlowPosition pStart, FlowPosition pEnd, Object source, int pageIndex)
        {
            FixedTextPointer eStart = new FixedTextPointer(false, LogicalDirection.Backward, pStart);
            FixedTextPointer eEnd = new FixedTextPointer(false, LogicalDirection.Forward, pEnd);
            FixedElement e = new FixedElement(elementType, eStart, eEnd, pageIndex);
            if (source != null)
            {
                e.Object = source;
            }
            // hook up logical tree
            parentElement.Append(e);

            // attach element to flownode for faster lookup later.
            pStart.AttachElement(e);
            pEnd.AttachElement(e);
        }

        //--------------------------------------------------------------------
        // TextContainer Element
        //---------------------------------------------------------------------

        // given a TextPointer range, find out all fixed position included in this range and
        // offset into the begin and end fixed element
        private bool _GetFixedNodesForFlowRange(ITextPointer start, ITextPointer end, out FixedSOMElement[] elements, out int startIndex, out int endIndex)
        {
            Debug.Assert(start.CompareTo(end) <= 0);
            elements  = null;
            startIndex = 0;
            endIndex = 0;

            if (start.CompareTo(end) == 0)
            {
                return false;
            }

            FixedTextPointer pStart    = (FixedTextPointer)start;
            FixedTextPointer pEnd      = (FixedTextPointer)end;

            return this.FixedTextBuilder.GetFixedNodesForFlowRange(pStart.FlowPosition, pEnd.FlowPosition, out elements, out startIndex, out endIndex);
        } //endofGetFixedNodes
        #endregion Private Methods

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private Fields
        /// <summary>
        /// Cache for document content provider
        /// </summary>
        private FixedDocument          _fixedPanel;    // the fixed document

        /// <summary>
        /// Fixed To Flow Implemenation
        /// </summary>
        private FixedTextBuilder    _fixedTextBuilder; // heuristics to build text stream from fixed document

        /// <summary>
        ///  Text OM
        /// </summary>
        private DependencyObject  _parent;
        private FixedElement _containerElement;
        private FixedTextPointer _start;
        private FixedTextPointer _end;

        // Collection of highlights applied to TextContainer content.
        private Highlights _highlights;
        private ITextSelection _textSelection;
        // TextView associated with this TextContainer.
        private ITextView _textview;

        #endregion Private Fields
    }
}
