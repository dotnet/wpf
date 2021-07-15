// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      DocumentSequenceTextContainer is a TextContainer that aggregates
//      0 or more TextContainer and expose them as single TextContainer.
//

#pragma warning disable 1634, 1691 // To enable presharp warning disables (#pragma suppress) below.

namespace System.Windows.Documents
{
    using MS.Internal;
    using MS.Internal.Documents;
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;                // DependencyID etc.
    using System.Text;
    using System.Windows.Threading;              // Dispatcher

    //=====================================================================
    /// <summary>
    /// DocumentSequenceTextContainer is a TextContainer that aggregates
    /// 0 or more TextContainer and expose them as single TextContainer.
    /// </summary>
    internal sealed class DocumentSequenceTextContainer : ITextContainer
    {
        //--------------------------------------------------------------------
        //
        // Connstructors
        //
        //---------------------------------------------------------------------

        #region Constructors

        internal DocumentSequenceTextContainer(DependencyObject parent)
        {
            Debug.Assert(parent != null);
            Debug.Assert(parent is FixedDocumentSequence);

            _parent = (FixedDocumentSequence)parent;
            _Initialize();
        }

        #endregion Constructors

        //--------------------------------------------------------------------
        //
        // Public Methods
        //
        //---------------------------------------------------------------------
        #region Public Methods

        /// <summary>
        /// </summary>
        void ITextContainer.BeginChange()
        {
            _changeBlockLevel++;

            // We'll raise the Changing event when/if we get an actual
            // change added, inside AddChangeSegment.
        }

        /// <summary>
        /// <see cref="System.Windows.Documents.ITextContainer.BeginChangeNoUndo"/>
        /// </summary>
        void ITextContainer.BeginChangeNoUndo()
        {
            // We don't support undo, so follow the BeginChange codepath.
            ((ITextContainer)this).BeginChange();
        }

        void ITextContainer.EndChange()
        {
            ((ITextContainer)this).EndChange(false /* skipEvents */);
        }

        /// <summary>
        /// </summary>
        void ITextContainer.EndChange(bool skipEvents)
        {
            TextContainerChangedEventArgs changes;

            Invariant.Assert(_changeBlockLevel > 0, "Unmatched EndChange call!");

            _changeBlockLevel--;

            if (_changeBlockLevel == 0 &&
                _changes != null)
            {
                changes = _changes;
                _changes = null;

                if (this.Changed != null && !skipEvents)
                {
                    Changed(this, changes);
                }
            }
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

#if DEBUG
        /// <summary>
        /// Debug only ToString override.
        /// </summary>
        public override string ToString()
        {
            int blocks = 0;
            ChildDocumentBlock cdb = this._doclistHead;
            while (cdb != null)
            {
                blocks++;
                cdb = cdb.NextBlock;
            }
            return  "DSTC Id=" + DebugId + " Blocks= " + blocks;
        }
#endif // DEBUG
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
            get { return this.TextSelection; }
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

        public event EventHandler Changing;

        public event TextContainerChangeEventHandler Change;

        public event TextContainerChangedEventHandler Changed;

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

        // Verify parameter. Throw Exception if necessary.
        internal DocumentSequenceTextPointer VerifyPosition(ITextPointer position)
        {
            if (position == null)
            {
                throw new ArgumentNullException("position");
            }

            if (position.TextContainer != this)
            {
                throw new ArgumentException(SR.Get(SRID.NotInAssociatedContainer, "position"));
            }

            DocumentSequenceTextPointer tp = position as DocumentSequenceTextPointer;
            if (tp == null)
            {
                throw new ArgumentException(SR.Get(SRID.BadFixedTextPosition, "position"));
            }

            return tp;
        }


        // Given an ITextPointer in a child TextContainer, create a position in parent's
        // address space to represent it.
        internal DocumentSequenceTextPointer MapChildPositionToParent(ITextPointer tp)
        {
            ChildDocumentBlock cdb = this._doclistHead;
            while (cdb != null)
            {
                if (cdb.ChildContainer == tp.TextContainer)
                {
                    return new DocumentSequenceTextPointer(cdb, tp);
                }
                cdb = cdb.NextBlock;
            }
            return null;
        }


        // Find a ChildBlock in the list that corresponds to the given DocumentReference
        // Return null if cannot find
        internal ChildDocumentBlock FindChildBlock(DocumentReference docRef)
        {
            Debug.Assert(docRef != null);
            ChildDocumentBlock cdb = _doclistHead.NextBlock;
            while (cdb != null)
            {
                if (cdb.DocRef == docRef)
                {
                    return cdb;
                }
                cdb = cdb.NextBlock;
            }
            return null;
        }


        // Return distance between two child TextContainer
        // 0 means the same container
        // n means block1 is n steps before block2 in the link list
        // -n  means block1 is n steps after block2 in the link list
        internal int GetChildBlockDistance(ChildDocumentBlock block1, ChildDocumentBlock block2)
        {
            // Note: we can improve perf of this function by using generation
            // mark + caching index, if this function turns out to be costly.
            // However, I would expect it to not happen very often.
            if ((object)block1 == (object)block2)
            {
                return 0;
            }

            // Assuming block1 is before block2
            int count = 0;
            ChildDocumentBlock cdb = block1;
            while (cdb != null)
            {
                if (cdb == block2)
                {
                    return count;
                }
                count++;
                cdb = cdb.NextBlock;
            }

            // Now block2 has to be before block1
            count = 0;
            cdb = block1;
            while (cdb != null)
            {
                if (cdb == block2)
                {
                    return count;
                }
                count--;
                cdb = cdb.PreviousBlock;
            }

            Debug.Assert(false, "should never be here");
            return 0;
        }
        #endregion Internal Methods

        //--------------------------------------------------------------------
        //
        // Internal Properties
        //
        //---------------------------------------------------------------------

        #region Internal Properties
        /// <summary>
        /// Collection of highlights applied to TextContainer content.
        /// </summary>
        internal Highlights Highlights
        {
            get
            {
                if (_highlights == null)
                {
                    _highlights = new DocumentSequenceHighlights(this);
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


#if DEBUG
        internal uint DebugId
        {
            get
            {
                return _debugId;
            }
        }
#endif
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
        private void _Initialize()
        {
            Debug.Assert(_parent != null);

            // Create Start Block/Container/Position
            _doclistHead = new ChildDocumentBlock(this, new NullTextContainer());

            // Create End Block/Container/Position
            _doclistTail = new ChildDocumentBlock(this, new NullTextContainer());

            // Link Start and End container together
            _doclistHead.InsertNextBlock(_doclistTail);

            // Now initialize the child doc block list
            ChildDocumentBlock currentBlock = _doclistHead;
            foreach (DocumentReference docRef in _parent.References)
            {
                currentBlock.InsertNextBlock(new ChildDocumentBlock(this, docRef));
                currentBlock = currentBlock.NextBlock;
            }

            //if we have at least one document, start and end pointers should be set to valid child blocks not to the placeholders
            if (_parent.References.Count != 0)
            {
                _start = new DocumentSequenceTextPointer(_doclistHead.NextBlock,  _doclistHead.NextBlock.ChildContainer.Start);
                _end = new DocumentSequenceTextPointer(_doclistTail.PreviousBlock, _doclistTail.PreviousBlock.ChildContainer.End);
            }
            else
            {
                _start = new DocumentSequenceTextPointer(_doclistHead,  _doclistHead.ChildContainer.Start);
                _end = new DocumentSequenceTextPointer(_doclistTail, _doclistTail.ChildContainer.End);
            }

            // Listen to collection changes
            _parent.References.CollectionChanged += new NotifyCollectionChangedEventHandler(_OnContentChanged);

            // Listen to Highlights changes so that it can notify sub-TextContainer
            this.Highlights.Changed += new HighlightChangedEventHandler(_OnHighlightChanged);
        }

        private void AddChange(ITextPointer startPosition, int symbolCount, PrecursorTextChangeType precursorTextChange)
        {
            Invariant.Assert(!_isReadOnly, "Illegal to modify DocumentSequenceTextContainer inside Change event scope!");

            ITextContainer textContainer = (ITextContainer)this;
            textContainer.BeginChange();
            try
            {
                // Contact any listeners.
                if (this.Changing != null)
                {
                    Changing(this, EventArgs.Empty);
                }

                // Fire the ChangingEvent now if we haven't already.
                if (_changes == null)
                {
                    _changes = new TextContainerChangedEventArgs();
                }

                _changes.AddChange(precursorTextChange, DocumentSequenceTextPointer.GetOffsetToPosition(_start, startPosition), symbolCount, false /* collectTextChanges */);

                if (this.Change != null)
                {
                    Invariant.Assert(precursorTextChange == PrecursorTextChangeType.ContentAdded || precursorTextChange == PrecursorTextChangeType.ContentRemoved);
                    TextChangeType textChange = (precursorTextChange == PrecursorTextChangeType.ContentAdded) ?
                        TextChangeType.ContentAdded : TextChangeType.ContentRemoved;

                    _isReadOnly = true;
                    try
                    {
                        // Pass in a -1 for charCount parameter.  DocumentSequenceTextContainer
                        // doesn't support this feature because it is only consumed by IMEs
                        // which never run on read-only documents.
                        Change(this, new TextContainerChangeEventArgs(startPosition, symbolCount, -1, textChange));
                    }
                    finally
                    {
                        _isReadOnly = false;
                    }
                }
            }
            finally
            {
                textContainer.EndChange();
            }
        }

        //--------------------------------------------------------------------
        // ContentChange
        //---------------------------------------------------------------------
        private void _OnContentChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
#if DEBUG
            this._generation++;
#endif

            if (args.Action == NotifyCollectionChangedAction.Add)
            {
                if (args.NewItems.Count != 1)
                {
                    throw new NotSupportedException(SR.Get(SRID.RangeActionsNotSupported));
                }
                else
                {
                        object item = args.NewItems[0];
                        int startingIndex = args.NewStartingIndex;

                        if (startingIndex != _parent.References.Count - 1)
                        {
                            throw new NotSupportedException(SR.Get(SRID.UnexpectedCollectionChangeAction, args.Action));
                        }

                        ChildDocumentBlock newBlock = new ChildDocumentBlock(this, (DocumentReference)item);

                        ChildDocumentBlock insertAfter = _doclistTail.PreviousBlock;
                        insertAfter.InsertNextBlock(newBlock);
                        DocumentSequenceTextPointer changeStart =
                            new DocumentSequenceTextPointer(insertAfter, insertAfter.End);

                        //Update end pointer
                        _end = new DocumentSequenceTextPointer(newBlock, newBlock.ChildContainer.End);

                        if (newBlock.NextBlock == _doclistTail && newBlock.PreviousBlock == _doclistHead)
                        {
                            //Update start pointer for the first block
                            _start = new DocumentSequenceTextPointer(newBlock,  newBlock.ChildContainer.Start);
                        }
                        

                        // Record Change Notifications
                        ITextContainer container = newBlock.ChildContainer;
                        int symbolCount = 1; // takes too long to calculate for large documents, and no one will use this info

                        // this does not affect state, only fires event handlers
                        AddChange(changeStart, symbolCount, PrecursorTextChangeType.ContentAdded);
                  }
            }
            else
            {
                throw new NotSupportedException(SR.Get(SRID.UnexpectedCollectionChangeAction, args.Action));
            }
        }

        //--------------------------------------------------------------------
        // Highlight compositing
        //---------------------------------------------------------------------

        private void _OnHighlightChanged(object sender, HighlightChangedEventArgs args)
        {
            Debug.Assert(sender != null);
            Debug.Assert(args != null);
            Debug.Assert(args.Ranges != null);
#if DEBUG
            {
                Highlights highlights = this.Highlights;
                StaticTextPointer highlightTransitionPosition;
                StaticTextPointer highlightRangeStart;
                object selected;

                DocumentsTrace.FixedDocumentSequence.Highlights.Trace("===BeginNewHighlightRange===");
                highlightTransitionPosition = ((ITextContainer)this).CreateStaticPointerAtOffset(0);
                while (true)
                {
                    // Move to the next highlight start.
                    if (!highlights.IsContentHighlighted(highlightTransitionPosition, LogicalDirection.Forward))
                    {
                        highlightTransitionPosition = highlights.GetNextHighlightChangePosition(highlightTransitionPosition, LogicalDirection.Forward);

                        // No more highlights?
                        if (highlightTransitionPosition.IsNull)
                            break;
                    }

                    // highlightTransitionPosition is at the start of a new highlight run.
                    selected = highlights.GetHighlightValue(highlightTransitionPosition, LogicalDirection.Forward, typeof(TextSelection));

                    // Save the start position and find the end.
                    highlightRangeStart = highlightTransitionPosition;
                    highlightTransitionPosition = highlights.GetNextHighlightChangePosition(highlightTransitionPosition, LogicalDirection.Forward);
                    Invariant.Assert(!highlightTransitionPosition.IsNull, "Highlight start not followed by highlight end!");

                    // Store the highlight.
                    if (selected != DependencyProperty.UnsetValue)
                    {
                        DocumentsTrace.FixedDocumentSequence.Highlights.Trace(string.Format("HightlightRange {0}-{1}", highlightRangeStart.ToString(), highlightTransitionPosition.ToString()));
                        if (highlightRangeStart.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.Text)
                        {
                            DocumentsTrace.FixedDocumentSequence.Highlights.Trace("<HighlightNotOnText>");
                        }
                        else
                        {
                            char[] sb = new char[256];
                            TextPointerBase.GetTextWithLimit(highlightRangeStart.CreateDynamicTextPointer(LogicalDirection.Forward), LogicalDirection.Forward, sb, 0, 256, highlightTransitionPosition.CreateDynamicTextPointer(LogicalDirection.Forward));
                            DocumentsTrace.FixedDocumentSequence.TextOM.Trace(string.Format("HightlightContent [{0}]", new String(sb)));
                        }
                    }
                }
                DocumentsTrace.FixedDocumentSequence.TextOM.Trace("===EndNewHighlightRange===");
            }
#endif
            Debug.Assert(args.Ranges.Count > 0 && ((TextSegment)args.Ranges[0]).Start.CompareTo(((TextSegment)args.Ranges[0]).End) < 0);


            // For each change range we received, we need to figure out
            // affected child TextContainer, and notify it with appropriate
            // ranges that are in the child's address space.
            //
            // We only fire one highlight change notification for any child
            // TextContainer even if there is multiple change ranges fall
            // into the same child TextContainer.
            //
            // We scan the ranges and the child TextContainer in the same loop,
            // moving forward two scanning pointers and at boundary of each
            // TextContainer, we fire a change notification.
            //
            int idxScan = 0;
            DocumentSequenceTextPointer  tsScan  = null;
            ChildDocumentBlock cdbScan =  null;
            List<TextSegment>  rangeArray = new List<TextSegment>(4);
            while (idxScan < args.Ranges.Count)
            {
                TextSegment ts = (TextSegment)args.Ranges[idxScan];
                DocumentSequenceTextPointer tsEnd   = (DocumentSequenceTextPointer)ts.End;
                ITextPointer tpChildStart, tpChildEnd;
                ChildDocumentBlock lastBlock;

                // If tsScan == null, we were done with previous range,
                // so we are going to set tsScan to begining of this range.
                // Otherwise the previous range was split so we will simply
                // start from what was left over from previous loop.
                if (tsScan == null)
                {
                    tsScan = (DocumentSequenceTextPointer)ts.Start;
                }
                lastBlock = cdbScan;
                cdbScan = tsScan.ChildBlock;

                if (lastBlock != null && cdbScan != lastBlock && !(lastBlock.ChildContainer is NullTextContainer) && rangeArray.Count != 0)
                {
                    // This range is in a different block, so take care of old blocks first
                    lastBlock.ChildHighlightLayer.RaiseHighlightChangedEvent(new ReadOnlyCollection<TextSegment>(rangeArray));
                    rangeArray.Clear();
                }

                tpChildStart = tsScan.ChildPointer;

                if (tsEnd.ChildBlock != cdbScan)
                {
                    // If this range crosses blocks, we are done with current block
                    tpChildEnd = tsScan.ChildPointer.TextContainer.End;
                    if (tpChildStart.CompareTo(tpChildEnd) != 0)
                    {
                        rangeArray.Add(new TextSegment(tpChildStart, tpChildEnd));
                    }
                    // Notify child container
                    if (!(cdbScan.ChildContainer is NullTextContainer) && rangeArray.Count != 0)
                    {
                        cdbScan.ChildHighlightLayer.RaiseHighlightChangedEvent(new ReadOnlyCollection<TextSegment>(rangeArray));
                    }

                    // Move on to next block;
                    cdbScan = cdbScan.NextBlock;
                    tsScan  = new DocumentSequenceTextPointer(cdbScan, cdbScan.ChildContainer.Start);
                    rangeArray.Clear();
                }
                else
                {
                    // Otherwise we need to go on to see if there is more ranges
                    // fall withing the same block. Simply add this change range
                    tpChildEnd = tsEnd.ChildPointer;
                    if (tpChildStart.CompareTo(tpChildEnd) != 0)
                    {
                        rangeArray.Add(new TextSegment(tpChildStart, tpChildEnd));
                    }

                    // Move on to next range
                    idxScan++;
                    tsScan = null;
                }
            }

            // Fire change notification for the last child block.
            if (rangeArray.Count > 0 && (!(cdbScan == null || cdbScan.ChildContainer is NullTextContainer)))
            {
                cdbScan.ChildHighlightLayer.RaiseHighlightChangedEvent(new ReadOnlyCollection<TextSegment>(rangeArray));
            }
        }
        #endregion Private Methods

        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private Fields
        private readonly FixedDocumentSequence   _parent;    // The content aggregator, supplied in ctor
        private DocumentSequenceTextPointer _start;    // Start of the aggregated TextContainer
        private DocumentSequenceTextPointer _end;      // End of the aggregated TextContainer
        private ChildDocumentBlock  _doclistHead;       // Head of the doubly linked list of child TextContainer entries
        private ChildDocumentBlock  _doclistTail;       // Tail of the doubly linked list of child TextContainer entries
        private ITextSelection _textSelection;

        // Collection of highlights applied to TextContainer content.
        private Highlights _highlights;

        // BeginChange ref count.  When non-zero, we are inside a change block.
        private int _changeBlockLevel;

        // Array of pending changes in the current change block.
        // Null outside of a change block.
        private TextContainerChangedEventArgs _changes;

        // TextView associated with this TextContainer.
        private ITextView _textview;

        // Set true during Change event callback.
        // When true, modifying the TextContainer is disallowed.
        private bool _isReadOnly;

#if DEBUG
        // The container generation, increamented whenever container content changes.
        private uint  _generation;
        private uint _debugId = (DocumentSequenceTextContainer._debugIdCounter++);
        private static uint _debugIdCounter = 0;
#endif
        #endregion Private Fields


        //--------------------------------------------------------------------
        //
        // Private Classes
        //
        //---------------------------------------------------------------------

        #region Private Classes

        /// <summary>
        /// DocumentSequence specific Highlights subclass accepts text pointers from
        /// either the DocSequence or any of its child FixedDocuments.  This allows
        /// the highlights to be set on the DocSequence but the FixedDocuments continue
        /// to handle the rendering (FixedDocs look for their parent's Highlights if
        /// the parent is a DocSequence).
        /// </summary>
        private sealed class DocumentSequenceHighlights : Highlights
        {
            internal DocumentSequenceHighlights(DocumentSequenceTextContainer textContainer)
                : base(textContainer)
            {
            }

            /// <summary>
            /// Returns the value of a property stored on scoping highlight, if any.
            /// </summary>
            /// <param name="textPosition">
            /// Position to query.
            /// </param>
            /// <param name="direction">
            /// Direction of content to query.
            /// </param>
            /// <param name="highlightLayerOwnerType">
            /// Type of the matching highlight layer owner.
            /// </param>
            /// <returns>
            /// The highlight value if set on any scoping highlight.  If no property
            /// value is set, returns DependencyProperty.UnsetValue.
            /// </returns>
            internal override object GetHighlightValue(StaticTextPointer textPosition, LogicalDirection direction, Type highlightLayerOwnerType)
            {
                StaticTextPointer parentPosition;

                if (EnsureParentPosition(textPosition, direction, out parentPosition))
                {
                    return base.GetHighlightValue(parentPosition, direction, highlightLayerOwnerType); 
                }

                return DependencyProperty.UnsetValue;
            }

            /// <summary>
            /// Returns true iff the indicated content has scoping highlights.
            /// </summary>
            /// <param name="textPosition">
            /// Position to query.
            /// </param>
            /// <param name="direction">
            /// Direction of content to query.
            /// </param>
            internal override bool IsContentHighlighted(StaticTextPointer textPosition, LogicalDirection direction)
            {
                StaticTextPointer parentPosition; 
                
                if (EnsureParentPosition(textPosition, direction, out parentPosition))
                {
                    return base.IsContentHighlighted(parentPosition, direction);
                }

                return false;
            }

            /// <summary>
            /// Returns the position of the next highlight start or end in an
            /// indicated direction, or null if there is no such position.
            /// </summary>
            /// <param name="textPosition">
            /// Position to query.
            /// </param>
            /// <param name="direction">
            /// Direction of content to query.
            /// </param>
            internal override StaticTextPointer GetNextHighlightChangePosition(StaticTextPointer textPosition, LogicalDirection direction)
            {
                StaticTextPointer parentPosition;
                StaticTextPointer returnPointer = StaticTextPointer.Null;

                if (EnsureParentPosition(textPosition, direction, out parentPosition))
                {
                    returnPointer = base.GetNextHighlightChangePosition(parentPosition, direction);

                    // If we were passed a child position, we need to convert the result back to a child position
                    if (textPosition.TextContainer.Highlights != this)
                    {
                        returnPointer = GetStaticPositionInChildContainer(returnPointer, direction, textPosition);
                    }
                }

                return returnPointer;
            }

            /// <summary>
            /// Returns the closest neighboring TextPointer in an indicated
            /// direction where a property value calculated from an embedded
            /// object, scoping text element, or scoping highlight could
            /// change.
            /// </summary>
            /// <param name="textPosition">
            /// Position to query.
            /// </param>
            /// <param name="direction">
            /// Direction of content to query.
            /// </param>
            /// <returns>
            /// If the following symbol is TextPointerContext.EmbeddedElement,
            /// TextPointerContext.ElementBegin, or TextPointerContext.ElementEnd, returns
            /// a TextPointer exactly one symbol distant.
            ///
            /// If the following symbol is TextPointerContext.Text, the distance
            /// of the returned TextPointer is the minimum of the value returned
            /// by textPosition.GetTextLength and the distance to any highlight
            /// start or end edge.
            ///
            /// If the following symbol is TextPointerContext.None, returns null.
            /// </returns>
            internal override StaticTextPointer GetNextPropertyChangePosition(StaticTextPointer textPosition, LogicalDirection direction)
            {
                StaticTextPointer parentPosition;
                StaticTextPointer returnPointer = StaticTextPointer.Null;

                if (EnsureParentPosition(textPosition, direction, out parentPosition))
                {
                    returnPointer = base.GetNextPropertyChangePosition(parentPosition, direction);                     

                    // If we were passed a child position, we need to convert the result back to a child position
                    if (textPosition.TextContainer.Highlights != this)
                    {
                        returnPointer = GetStaticPositionInChildContainer(returnPointer, direction, textPosition);
                    }
                }

                return returnPointer; 
            }

            /// <summary>
            /// Sets parentPosition to be a valid TextPointer in the parent document.  This could either
            /// be the textPosition passed in (if its already on the parent document) or a conversion
            /// of the textPosition passed in.
            /// </summary>
            /// <returns>whether or not parentPosition is valid and should be used</returns>
            private bool EnsureParentPosition(StaticTextPointer textPosition, LogicalDirection direction, out StaticTextPointer parentPosition)
            {
                // Simple case - textPosition is already in the parent TextContainer
                parentPosition = textPosition;

                // If textPosition is on a child TextContainer, we convert it
                if (textPosition.TextContainer.Highlights != this)
                {
                    // This case can't be converted so return false, out parameter should not be used
                    if (textPosition.GetPointerContext(direction) == TextPointerContext.None)
                        return false;

                    // Turn the textPosition (which should be in the scope of a FixedDocument) 
                    // into a position in the scope of the DocumentSequence.
                    ITextPointer dynamicTextPointer = textPosition.CreateDynamicTextPointer(LogicalDirection.Forward);
                    ITextPointer parentTextPointer = ((DocumentSequenceTextContainer)this.TextContainer).MapChildPositionToParent(dynamicTextPointer);
                    Debug.Assert(parentTextPointer != null);
                    parentPosition = parentTextPointer.CreateStaticPointer();
                }

                // Returning true - either we started with a parent position or we converted to one
                return true;
            }

            /// <summary>
            /// Conversion from a StaticTextPointer on a DocumentSequence into a StaticTextPointer
            /// on a specified FixedDocument.  If the conversion results in a pointer on a different
            /// FixedDocument then we return one end of the FixedDocument (based on direction).
            /// </summary>
            /// <param name="textPosition">position in a DocumentSequence to convert</param>
            /// <param name="direction">direction of the desired conversion</param>
            /// <param name="originalPosition">original pointer from FixedDocument</param>
            private StaticTextPointer GetStaticPositionInChildContainer(StaticTextPointer textPosition, LogicalDirection direction, StaticTextPointer originalPosition)
            {
                StaticTextPointer parentTextPointer = StaticTextPointer.Null;

                if (!textPosition.IsNull)
                {
                    DocumentSequenceTextPointer parentChangePosition = textPosition.CreateDynamicTextPointer(LogicalDirection.Forward) as DocumentSequenceTextPointer;
                    Debug.Assert(parentChangePosition != null);
                    
                    // If the DocSequence position translates into a position in a different FixedDocument than
                    // the original request, we return an end of the original FixedDocument (which end depends on direction)
                    ITextPointer childTp = parentChangePosition.ChildPointer;
                    if (childTp.TextContainer != originalPosition.TextContainer)
                    {
                        // If the position we started searching from is highlighted, cut the highlight
                        // at the end of the text container.  Otherwise return null (the highlight must
                        // start in the next document).
                        if (IsContentHighlighted(originalPosition, direction))
                        {
                            childTp = direction == LogicalDirection.Forward ?
                                                   originalPosition.TextContainer.End
                                                 : originalPosition.TextContainer.Start;
                            parentTextPointer = childTp.CreateStaticPointer();
                        }
                        else
                        {
                            parentTextPointer = StaticTextPointer.Null;
                        }
                    }
                    else
                    {
                        parentTextPointer = childTp.CreateStaticPointer();
                    }
                }

                return parentTextPointer;
            }
        }

        #endregion Private Classes
    }
}
