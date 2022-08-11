// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      DocumentSequenceTextPointer is an implementation of ITextPointer/ITextPointer
//      for FixedDocumentSequence. It is the base class for DocumentSequenceTextPointer and
//      DocumentSequenceTextPointer.
//

#pragma warning disable 1634, 1691 // To enable presharp warning disables (#pragma suppress) below.

namespace System.Windows.Documents
{
    using MS.Internal.Documents;
    using MS.Utility;
    using MS.Internal;
    using System.Windows;
    using System;
    using System.Diagnostics;


    /// <summary>
    /// DocumentSequenceTextPointer is an implementation of ITextPointer for FixedDocumentSequence
    /// </summary>
    internal sealed class DocumentSequenceTextPointer : ContentPosition, ITextPointer
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        #region Constructors
        // Ctor always set mutable flag to false
        internal DocumentSequenceTextPointer(ChildDocumentBlock childBlock, ITextPointer childPosition)
        {
            Debug.Assert(childBlock != null);
            Debug.Assert(childPosition != null);
            _childBlock = childBlock;
            _childTp = childPosition;
        }
        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        #region TextPointer Methods

        /// <summary>
        /// <see cref="ITextPointer.SetLogicalDirection"/>
        /// </summary>
        void ITextPointer.SetLogicalDirection(LogicalDirection direction)
        {
            Debug.Assert(!_isFrozen, "Can't reposition a frozen pointer!");
            _childTp.SetLogicalDirection(direction);
        }

        /// <summary>
        /// <see cref="ITextPointer.CompareTo(ITextPointer)"/>
        /// </summary>
        int ITextPointer.CompareTo(ITextPointer position)
        {
            return DocumentSequenceTextPointer.CompareTo(this, position);
        }

        /// <summary>
        /// <see cref="ITextPointer.CompareTo(StaticTextPointer)"/>
        /// </summary>
        int ITextPointer.CompareTo(StaticTextPointer position)
        {
            return ((ITextPointer)this).CompareTo((ITextPointer)position.Handle0);
        }

        /// <summary>
        /// <see cref="ITextPointer.GetOffsetToPosition"/>
        /// </summary>
        int ITextPointer.GetOffsetToPosition(ITextPointer position)
        {
            return DocumentSequenceTextPointer.GetOffsetToPosition(this, position);
        }

        /// <summary>
        /// <see cref="ITextPointer.GetPointerContext"/>
        /// </summary>
        TextPointerContext ITextPointer.GetPointerContext(LogicalDirection direction)
        {
            return DocumentSequenceTextPointer.GetPointerContext(this, direction);
        }

        /// <summary>
        /// <see cref="ITextPointer.GetTextRunLength"/>
        /// </summary>
        /// <remarks>Return 0 if non-text run</remarks>
        int ITextPointer.GetTextRunLength(LogicalDirection direction)
        {
            return DocumentSequenceTextPointer.GetTextRunLength(this, direction);
        }

        // <see cref="ITextPointer.GetTextInRun(LogicalDirection)"/>
        string ITextPointer.GetTextInRun(LogicalDirection direction)
        {
            return TextPointerBase.GetTextInRun(this, direction);
        }

        /// <summary>
        /// <see cref="ITextPointer.GetTextInRun(LogicalDirection,char[],int,int)"/>
        /// </summary>
        /// <remarks>Only reutrn uninterrupted runs of text</remarks>
        int ITextPointer.GetTextInRun(LogicalDirection direction, char[] textBuffer, int startIndex, int count)
        {
            return DocumentSequenceTextPointer.GetTextInRun(this, direction, textBuffer, startIndex, count);
        }

        /// <summary>
        /// <see cref="ITextPointer.GetAdjacentElement"/>
        /// </summary>
        /// <remarks>Return null if the embedded object does not exist</remarks>
        object ITextPointer.GetAdjacentElement(LogicalDirection direction)
        {
            return DocumentSequenceTextPointer.GetAdjacentElement(this, direction);
        }

        /// <summary>
        /// <see cref="ITextPointer.GetElementType"/>
        /// </summary>
        /// <remarks>Return null if no TextElement in the direction</remarks>
        Type ITextPointer.GetElementType(LogicalDirection direction)
        {
            return DocumentSequenceTextPointer.GetElementType(this, direction);
        }

        /// <summary>
        /// <see cref="ITextPointer.HasEqualScope"/>
        /// </summary>
        bool ITextPointer.HasEqualScope(ITextPointer position)
        {
            return DocumentSequenceTextPointer.HasEqualScope(this, position);
        }


        /// <summary>
        /// <see cref="ITextPointer.GetValue"/>
        /// </summary>
        /// <remarks>return property values even if there is no scoping element</remarks>
        object ITextPointer.GetValue(DependencyProperty property)
        {
            return DocumentSequenceTextPointer.GetValue(this, property);
        }

        /// <summary>
        /// <see cref="ITextPointer.ReadLocalValue"/>
        /// </summary>
        /// <remarks>Throws InvalidOperationException if there is no scoping element</remarks>
        object ITextPointer.ReadLocalValue(DependencyProperty property)
        {
            return DocumentSequenceTextPointer.ReadLocalValue(this, property);
        }

        /// <summary>
        /// <see cref="ITextPointer.GetLocalValueEnumerator"/>
        /// </summary>
        /// <remarks>Returns an empty enumerator if there is no scoping element</remarks>
        LocalValueEnumerator ITextPointer.GetLocalValueEnumerator()
        {
            return DocumentSequenceTextPointer.GetLocalValueEnumerator(this);
        }

        /// <summary>
        /// <see cref="ITextPointer.CreatePointer()"/>
        /// </summary>
        ITextPointer ITextPointer.CreatePointer()
        {
            return DocumentSequenceTextPointer.CreatePointer(this);
        }

        // Unoptimized CreateStaticPointer implementation.
        // Creates a simple wrapper for an ITextPointer instance.
        /// <summary>
        /// <see cref="ITextPointer.CreateStaticPointer"/>
        /// </summary>
        StaticTextPointer ITextPointer.CreateStaticPointer()
        {
            return new StaticTextPointer(((ITextPointer)this).TextContainer, ((ITextPointer)this).CreatePointer());
        }

        /// <summary>
        /// <see cref="ITextPointer.CreatePointer(int)"/>
        /// </summary>
        ITextPointer ITextPointer.CreatePointer(int distance)
        {
            return DocumentSequenceTextPointer.CreatePointer(this, distance);
        }

        /// <summary>
        /// <see cref="ITextPointer.CreatePointer(LogicalDirection)"/>
        /// </summary>
        ITextPointer ITextPointer.CreatePointer(LogicalDirection gravity)
        {
            return DocumentSequenceTextPointer.CreatePointer(this, gravity);
        }

        /// <summary>
        /// <see cref="ITextPointer.CreatePointer(int,LogicalDirection)"/>
        /// </summary>
        ITextPointer ITextPointer.CreatePointer(int distance, LogicalDirection gravity)
        {
            return DocumentSequenceTextPointer.CreatePointer(this, distance, gravity);
        }

        // <see cref="ITextPointer.Freeze"/>
        void ITextPointer.Freeze()
        {
            _isFrozen = true;
        }

        /// <summary>
        /// <see cref="ITextPointer.GetFrozenPointer"/>
        /// </summary>
        ITextPointer ITextPointer.GetFrozenPointer(LogicalDirection logicalDirection)
        {
            return TextPointerBase.GetFrozenPointer(this, logicalDirection);
        }

        /// <summary>
        /// Inserts text at a specified position.
        /// </summary>
        /// <param name="textData">
        /// Text to insert.
        /// </param>
        void ITextPointer.InsertTextInRun(string textData)
        {
            throw new InvalidOperationException(SR.Get(SRID.DocumentReadOnly));
        }

        /// <summary>
        /// Removes content covered by a pair of positions.
        /// </summary>
        /// <param name="limit">
        /// Position following the last symbol to delete.  endPosition must be
        /// scoped by the same text element as startPosition.
        /// </param>
        void ITextPointer.DeleteContentToPosition(ITextPointer limit)
        {
            throw new InvalidOperationException(SR.Get(SRID.DocumentReadOnly));
        }

        // Candidate for replacing MoveToNextContextPosition for immutable TextPointer model
        ITextPointer ITextPointer.GetNextContextPosition(LogicalDirection direction)
        {
            ITextPointer pointer = ((ITextPointer)this).CreatePointer();
            if (pointer.MoveToNextContextPosition(direction))
            {
                pointer.Freeze();
            }
            else
            {
                pointer = null;
            }
            return pointer;
        }

        // Candidate for replacing MoveToInsertionPosition for immutable TextPointer model
        ITextPointer ITextPointer.GetInsertionPosition(LogicalDirection direction)
        {
            ITextPointer pointer = ((ITextPointer)this).CreatePointer();
            pointer.MoveToInsertionPosition(direction);
            pointer.Freeze();
            return pointer;
        }

        // Returns the closest insertion position, treating all unicode code points
        // as valid insertion positions.  A useful performance win over 
        // GetNextInsertionPosition when only formatting scopes are important.
        ITextPointer ITextPointer.GetFormatNormalizedPosition(LogicalDirection direction)
        {
            ITextPointer pointer = ((ITextPointer)this).CreatePointer();
            TextPointerBase.MoveToFormatNormalizedPosition(pointer, direction);
            pointer.Freeze();
            return pointer;
        }

        // Candidate for replacing MoveToNextInsertionPosition for immutable TextPointer model
        ITextPointer ITextPointer.GetNextInsertionPosition(LogicalDirection direction)
        {
            ITextPointer pointer = ((ITextPointer)this).CreatePointer();
            if (pointer.MoveToNextInsertionPosition(direction))
            {
                pointer.Freeze();
            }
            else
            {
                pointer = null;
            }
            return pointer;
        }

        /// <see cref="ITextPointer.ValidateLayout"/>
        bool ITextPointer.ValidateLayout()
        {
            return TextPointerBase.ValidateLayout(this, ((ITextPointer)this).TextContainer.TextView);
        }

        #endregion TextPointer Methods

        #region Public Methods
#if DEBUG
        /// <summary>
        /// Debug only ToString override.
        /// </summary>
        public override string ToString()
        {
            return DocumentSequenceTextPointer.ToString(this);
        }
#endif // DEBUG
        #endregion Public Methods



        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region TextPointer Properties

        // <see cref="ITextPointer.ParentType"/>
        Type ITextPointer.ParentType
        {
            get
            {
                return DocumentSequenceTextPointer.GetElementType(this);
            }
        }

        /// <summary>
        /// <see cref="ITextPointer.TextContainer"/>
        /// </summary>
        ITextContainer ITextPointer.TextContainer
        {
            get { return this.AggregatedContainer; }
        }

        // <see cref="ITextPointer.HadValidLayout"/>
        bool ITextPointer.HasValidLayout
        {
            get
            {
                return (((ITextPointer)this).TextContainer.TextView != null &&
                        ((ITextPointer)this).TextContainer.TextView.IsValid &&
                        ((ITextPointer)this).TextContainer.TextView.Contains(this));
            }
        }

        // <see cref="ITextPointer.IsAtCaretUnitBoundary"/>
        bool ITextPointer.IsAtCaretUnitBoundary
        {
            get
            {
                Invariant.Assert(((ITextPointer)this).HasValidLayout);
                ITextView textView = ((ITextPointer)this).TextContainer.TextView;
                bool isAtCaretUnitBoundary = textView.IsAtCaretUnitBoundary(this);
                
                if (!isAtCaretUnitBoundary && ((ITextPointer)this).LogicalDirection == LogicalDirection.Backward)
                {
                    // In MIL Text and TextView worlds, a position at trailing edge of a newline (with backward gravity)
                    // is not an allowed caret stop. 
                    // However, in TextPointer world we must allow such a position to be a valid insertion position,
                    // since it breaks textrange normalization for empty ranges.
                    // Hence, we need to check for TextView.IsAtCaretUnitBoundary in reverse direction below.

                    ITextPointer positionForwardGravity = ((ITextPointer)this).CreatePointer(LogicalDirection.Forward);
                    isAtCaretUnitBoundary = textView.IsAtCaretUnitBoundary(positionForwardGravity);
                }
                return isAtCaretUnitBoundary;
            }
        }

        /// <summary>
        /// <see cref="ITextPointer.LogicalDirection"/>
        /// </summary>
        LogicalDirection ITextPointer.LogicalDirection
        {
            get
            {
                return _childTp.LogicalDirection;
            }
        }

        bool ITextPointer.IsAtInsertionPosition
        {
            get { return TextPointerBase.IsAtInsertionPosition(this); }
        }

        // <see cref="ITextPointer.IsFrozen"/>
        bool ITextPointer.IsFrozen
        {
            get
            {
                return _isFrozen;
            }
        }

        // <see cref="ITextPointer.Offset"/>
        int ITextPointer.Offset
        {
            get
            {
                return TextPointerBase.GetOffset(this);
            }
        }

        // Not implemented.
        int ITextPointer.CharOffset
        {
            get
            {
                #pragma warning suppress 56503
                throw new NotImplementedException();
            }
        }

        #endregion TextPointer Properties

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region TextNavigator Methods

        /// <summary>
        /// <see cref="ITextPointer.MoveByOffset"/>
        /// </summary>
        bool ITextPointer.MoveToNextContextPosition(LogicalDirection direction)
        {
            Debug.Assert(!_isFrozen, "Can't reposition a frozen pointer!");
            return DocumentSequenceTextPointer.iScan(this, direction);
        }


        /// <summary>
        /// <see cref="ITextPointer.MoveByOffset"/>
        /// </summary>
        int ITextPointer.MoveByOffset(int offset)
        {
            if (_isFrozen) throw new InvalidOperationException(SR.Get(SRID.TextPositionIsFrozen));
            
            if (DocumentSequenceTextPointer.iScan(this, offset))
            {
                return offset;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// <see cref="ITextPointer.MoveToPosition"/>
        /// </summary>
        void ITextPointer.MoveToPosition(ITextPointer position)
        {
            DocumentSequenceTextPointer tp = this.AggregatedContainer.VerifyPosition(position);

            Debug.Assert(!_isFrozen, "Can't reposition a frozen pointer!");

            LogicalDirection gravity = this.ChildPointer.LogicalDirection;
            this.ChildBlock = tp.ChildBlock;
            if (this.ChildPointer.TextContainer == tp.ChildPointer.TextContainer)
            {
                this.ChildPointer.MoveToPosition(tp.ChildPointer);
            }
            else
            {
                this.ChildPointer = tp.ChildPointer.CreatePointer();
                this.ChildPointer.SetLogicalDirection(gravity);
            }
        }

        /// <summary>
        /// <see cref="ITextPointer.MoveToElementEdge"/>
        /// </summary>
        void ITextPointer.MoveToElementEdge(ElementEdge edge)
        {
            Debug.Assert(!_isFrozen, "Can't reposition a frozen pointer!");

            this.ChildPointer.MoveToElementEdge(edge);
        }

        // <see cref="ITextPointer.MoveToLineBoundary"/>
        int ITextPointer.MoveToLineBoundary(int count)
        {
            return TextPointerBase.MoveToLineBoundary(this, ((ITextPointer)this).TextContainer.TextView, count, true);
        }

        // <see cref="ITextPointer.GetCharacterRect"/>
        Rect ITextPointer.GetCharacterRect(LogicalDirection direction)
        {
            return TextPointerBase.GetCharacterRect(this, direction);
        }

        bool ITextPointer.MoveToInsertionPosition(LogicalDirection direction)
        {
            return TextPointerBase.MoveToInsertionPosition(this, direction);
        }

        bool ITextPointer.MoveToNextInsertionPosition(LogicalDirection direction)
        {
            return TextPointerBase.MoveToNextInsertionPosition(this, direction);
        }

        #endregion TextNavigator Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties
        internal DocumentSequenceTextContainer AggregatedContainer
        {
            get { return _childBlock.AggregatedContainer; }
        }


        // Accessor to base class ChildBlock.
        internal ChildDocumentBlock ChildBlock
        {
            get
            {
                return _childBlock;
            }
            set
            {
                _childBlock = value;
            }
        }

        // Helper exposing base classes ChildPointer as an ITextPointer,
        // which is guaranteed safe, since we set its value in the ctor
        // for this class.
        internal ITextPointer ChildPointer
        {
            get
            {
                return _childTp;
            }
            set
            {
                _childTp = value;
                Debug.Assert(_childTp != null);
            }
        }

#if DEBUG
        // Debug-only identifier.
        private uint DebugId
        {
            get
            {
                return _debugId;
            }
        }
#endif // DEBUG

        #endregion Internal Properties

        // ======================================================
        // Static part of a class

        // <summary>
        // DocumentSequenceTextPointer is a static  class that is provided all the
        // common functions of ITextPointer/ITextNavigaor for DocumentSequence.
        // Since we don't have multiple inheritance, this is a way to share code between
        // DocumentSequenceTextPointer and DocumentSequenceTextPointer.
        // </summary>

        //------------------------------------------------------
        //
        //  Public Methods
         //
        //------------------------------------------------------
        #region TextPointer Methods
        /// <summary>
        /// <see cref="ITextPointer.CompareTo(ITextPointer)"/>
        /// </summary>
        public static int CompareTo(DocumentSequenceTextPointer thisTp, ITextPointer position)
        {
            DocumentSequenceTextPointer tp = thisTp.AggregatedContainer.VerifyPosition(position);

            // Now do compare
            return xGapAwareCompareTo(thisTp, tp);
        }


        /// <summary>
        /// <see cref="ITextPointer.GetOffsetToPosition"/>
        /// </summary>
        public static int GetOffsetToPosition(DocumentSequenceTextPointer thisTp, ITextPointer position)
        {
            DocumentSequenceTextPointer tp = thisTp.AggregatedContainer.VerifyPosition(position);

            int comp = xGapAwareCompareTo(thisTp, tp);
            if (comp == 0)
            {
                return 0;
            }
            else if (comp <= 0)
            {
                return xGapAwareGetDistance(thisTp, tp);
            }
            else
            {
                return -1 * xGapAwareGetDistance(tp, thisTp);
            }
        }

        /// <summary>
        /// <see cref="ITextPointer.GetPointerContext"/>
        /// </summary>
        public static TextPointerContext GetPointerContext(DocumentSequenceTextPointer thisTp, LogicalDirection direction)
        {
            ValidationHelper.VerifyDirection(direction, "direction");

            return xGapAwareGetSymbolType(thisTp, direction);
        }

        /// <summary>
        /// <see cref="ITextPointer.GetTextRunLength"/>
        /// </summary>
        /// <remarks>Return 0 if non-text run</remarks>
        public static int GetTextRunLength(DocumentSequenceTextPointer thisTp, LogicalDirection direction)
        {
            ValidationHelper.VerifyDirection(direction, "direction");

            return thisTp.ChildPointer.GetTextRunLength(direction);
        }

        /// <summary>
        /// <see cref="ITextPointer.GetTextInRun(LogicalDirection,char[],int,int)"/>
        /// </summary>
        /// <remarks>Only reutrn uninterrupted runs of text</remarks>
        public static int GetTextInRun(DocumentSequenceTextPointer thisTp, LogicalDirection direction, char[] textBuffer, int startIndex, int count)
        {
            ValidationHelper.VerifyDirection(direction, "direction");

            if (textBuffer == null)
            {
                throw new ArgumentNullException("textBuffer");
            }
            if (startIndex < 0)
            {
                throw new ArgumentException(SR.Get(SRID.NegativeValue, "startIndex"));
            }
            if (startIndex > textBuffer.Length)
            {
                throw new ArgumentException(SR.Get(SRID.StartIndexExceedsBufferSize, startIndex, textBuffer.Length));
            }
            if (count < 0)
            {
                throw new ArgumentException(SR.Get(SRID.NegativeValue, "count"));
            }
            if (count > textBuffer.Length - startIndex)
            {
                throw new ArgumentException(SR.Get(SRID.MaxLengthExceedsBufferSize, count, textBuffer.Length, startIndex));
            }

            return thisTp.ChildPointer.GetTextInRun(direction, textBuffer, startIndex, count);
        }

        /// <summary>
        /// <see cref="ITextPointer.GetAdjacentElement"/>
        /// </summary>
        /// <remarks>Return null if the embedded object does not exist</remarks>
        public static object GetAdjacentElement(DocumentSequenceTextPointer thisTp, LogicalDirection direction)
        {
            ValidationHelper.VerifyDirection(direction, "direction");

            return xGapAwareGetEmbeddedElement(thisTp, direction);
        }

        /// <summary>
        /// <see cref="ITextPointer.GetElementType"/>
        /// </summary>
        /// <remarks>Return null if no TextElement in the direction</remarks>
        public static Type GetElementType(DocumentSequenceTextPointer thisTp, LogicalDirection direction)
        {
            ValidationHelper.VerifyDirection(direction, "direction");

            DocumentSequenceTextPointer tp = xGetClingDSTP(thisTp, direction);

            return tp.ChildPointer.GetElementType(direction);
        }

        /// <summary>
        /// <see cref="ITextPointer.GetElementType"/>
        /// </summary>
        public static Type GetElementType(DocumentSequenceTextPointer thisTp)
        {
            return thisTp.ChildPointer.ParentType;
        }

        /// <summary>
        /// <see cref="ITextPointer.HasEqualScope"/>
        /// </summary>
        public static bool HasEqualScope(DocumentSequenceTextPointer thisTp, ITextPointer position)
        {
            DocumentSequenceTextPointer tp = thisTp.AggregatedContainer.VerifyPosition(position);

            if (thisTp.ChildPointer.TextContainer == tp.ChildPointer.TextContainer)
            {
                return thisTp.ChildPointer.HasEqualScope(tp.ChildPointer);
            }
            // The TextOM speced behavior is if both scopes are null, return true.
            return thisTp.ChildPointer.ParentType == typeof(FixedDocument) && tp.ChildPointer.ParentType == typeof(FixedDocument);
        }

        /// <summary>
        /// <see cref="ITextPointer.GetValue"/>
        /// </summary>
        /// <remarks>return property values even if there is no scoping element</remarks>
        public static object GetValue(DocumentSequenceTextPointer thisTp, DependencyProperty property)
        {
            if (property == null)
            {
                throw new ArgumentNullException("property");
            }

            return thisTp.ChildPointer.GetValue(property);
        }

        /// <summary>
        /// <see cref="ITextPointer.ReadLocalValue"/>
        /// </summary>
        /// <remarks>Throws InvalidOperationException if there is no scoping element</remarks>
        public static object ReadLocalValue(DocumentSequenceTextPointer thisTp, DependencyProperty property)
        {
            if (property == null)
            {
                throw new ArgumentNullException("property");
            }

            return thisTp.ChildPointer.ReadLocalValue(property);
        }

        /// <summary>
        /// <see cref="ITextPointer.GetLocalValueEnumerator"/>
        /// </summary>
        /// <remarks>Returns an empty enumerator if there is no scoping element</remarks>
        public static LocalValueEnumerator GetLocalValueEnumerator(DocumentSequenceTextPointer thisTp)
        {
            return thisTp.ChildPointer.GetLocalValueEnumerator();
        }

        public static ITextPointer CreatePointer(DocumentSequenceTextPointer thisTp)
        {
            return CreatePointer(thisTp, 0, thisTp.ChildPointer.LogicalDirection);
        }

        public static ITextPointer CreatePointer(DocumentSequenceTextPointer thisTp, int distance)
        {
            return CreatePointer(thisTp, distance, thisTp.ChildPointer.LogicalDirection);
        }

        public static ITextPointer CreatePointer(DocumentSequenceTextPointer thisTp, LogicalDirection gravity)
        {
            return CreatePointer(thisTp, 0, gravity);
        }

        /// <summary>
        /// <see cref="ITextPointer.CreatePointer(int,LogicalDirection)"/>
        /// </summary>
        public static ITextPointer CreatePointer(DocumentSequenceTextPointer thisTp, int distance, LogicalDirection gravity)
        {
            ValidationHelper.VerifyDirection(gravity, "gravity");

            // Special case for common case of distance == 0
            // to avoid calculating child container size, which
            // could be expansive especially in case where child
            // container requires virtualization.
            DocumentSequenceTextPointer newTp = new DocumentSequenceTextPointer(thisTp.ChildBlock, thisTp.ChildPointer.CreatePointer(gravity));
            if (distance != 0)
            {
                if (!xGapAwareScan(newTp, distance))
                {
                    throw new ArgumentException(SR.Get(SRID.BadDistance), "distance");
                }
            }
            return newTp;
        }

        #endregion TextPointer Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------


        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods


        // Internal Method, input parameter contains TP that is not synced to generation
        internal static bool iScan(DocumentSequenceTextPointer thisTp, LogicalDirection direction)
        {
            bool moved = thisTp.ChildPointer.MoveToNextContextPosition(direction);
            if (!moved)
            {
                moved = xGapAwareScan(thisTp, (direction == LogicalDirection.Forward ? 1 : -1));
            }
            return moved;
        }


        // Internal Method, input parameter contains TP that is not synced to generation
        internal static bool iScan(DocumentSequenceTextPointer thisTp, int distance)
        {
            return xGapAwareScan(thisTp, distance);
        }

#if DEBUG
        // Allocate a unique debug-only ID as identifier.
        internal static uint GetDebugId()
        {
            return _debugIdCounter++;
        }

        internal static string ToString(DocumentSequenceTextPointer thisTp)
        {
            return  (thisTp is DocumentSequenceTextPointer ? "DSTP" : "DSTN")
                    + " Id=" + thisTp.DebugId
                    + " B=" + thisTp.ChildBlock.DebugId
                    + " G=" + thisTp.ChildPointer.LogicalDirection
                    ;
        }
#endif
        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Internal Property
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods
        private static DocumentSequenceTextPointer xGetClingDSTP(DocumentSequenceTextPointer thisTp, LogicalDirection direction)
        {
            TextPointerContext context = thisTp.ChildPointer.GetPointerContext(direction);

            if (context != TextPointerContext.None)
            {
                return thisTp;
            }

            ChildDocumentBlock block = thisTp.ChildBlock;
            ITextPointer pointer = thisTp.ChildPointer;

            if (direction == LogicalDirection.Forward)
            {
                while (context == TextPointerContext.None && !block.IsTail)
                {
                    // get next block
                    block = block.NextBlock;
                    // get start
                    pointer = block.ChildContainer.Start;
                    context = pointer.GetPointerContext(direction);
                }
            }
            else
            {
                Debug.Assert(direction == LogicalDirection.Backward);
                while (context == TextPointerContext.None && !block.IsHead)
                {
                    // get next block
                    block = block.PreviousBlock;
                    // get start
                    pointer = block.ChildContainer.End;
                    context = pointer.GetPointerContext(direction);
                }
            }

            return new DocumentSequenceTextPointer(block, pointer);
        }


        //-----------------------------------------------------------------------
        // Trusted Methods -- all positions are in valid blocks
        //
        // Each ChildDocumentBlock pair is separated by a gap (DocumentBreak),
        // which is surfaced as an embedded object. Besides the Head and Tail block,
        // each block is enclosed by two gap objects, one at each end of the
        // TextContainer (Before TextContainer.Start and After TextContainer.End)
        //-----------------------------------------------------------------------


        private static TextPointerContext xGapAwareGetSymbolType(DocumentSequenceTextPointer thisTp, LogicalDirection direction)
        {
            DocumentSequenceTextPointer tp = xGetClingDSTP(thisTp, direction);
            return tp.ChildPointer.GetPointerContext(direction);
        }


        private static object xGapAwareGetEmbeddedElement(DocumentSequenceTextPointer thisTp, LogicalDirection direction)
        {
            DocumentSequenceTextPointer tp = xGetClingDSTP(thisTp, direction);
            return tp.ChildPointer.GetAdjacentElement(direction);
        }


        //  Intelligent compare routine that understands block gap
        //  Since there it is assumed that there is an invisible Gap
        //  object between adjancent two blocks, there is no position
        //  overlap.
        private static int xGapAwareCompareTo(DocumentSequenceTextPointer thisTp, DocumentSequenceTextPointer tp)
        {
            Debug.Assert(tp != null);
            if ((object)thisTp == (object)tp)
            {
                return 0;
            }

            ChildDocumentBlock thisBlock = thisTp.ChildBlock;
            ChildDocumentBlock tpBlock = tp.ChildBlock;

            int comp = thisTp.AggregatedContainer.GetChildBlockDistance(thisBlock, tpBlock);
            if (comp == 0)
            {
                Debug.Assert(thisTp.ChildBlock.ChildContainer == tp.ChildBlock.ChildContainer);
                return thisTp.ChildPointer.CompareTo(tp.ChildPointer);
            }
            else if (comp < 0)
            {
                // thisBlock is after tpBlock
                return xUnseparated(tp, thisTp) ? 0 : 1;
            }
            else
            {
                // thisBlock is before tpBlock
                return xUnseparated(thisTp, tp) ? 0 : -1;
            }
        }

        private static bool xUnseparated(DocumentSequenceTextPointer tp1, DocumentSequenceTextPointer tp2)
        {
            // tp1 is before tp2, check both are at edge of documents
            //check nothing of any length between them
            if (tp1.ChildPointer.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.None ||
                tp2.ChildPointer.GetPointerContext(LogicalDirection.Backward) != TextPointerContext.None)
            {
                return false;
            }

            ChildDocumentBlock block = tp1.ChildBlock.NextBlock;

            while (block != tp2.ChildBlock)
            {
                if (block.ChildContainer.Start.GetPointerContext(LogicalDirection.Forward) != TextPointerContext.None)
                {
                    return false;
                }

                block = block.NextBlock;
            }

            return true;
        }
        // Get the count of symbols between two TP.
        // Gap aware
        // TP1 <= TP2
        private static int xGapAwareGetDistance(DocumentSequenceTextPointer tp1, DocumentSequenceTextPointer tp2)
        {
            Debug.Assert(xGapAwareCompareTo(tp1, tp2) <= 0);
            if (tp1 == tp2)
            {
                return 0;
            }

            int count = 0;
            DocumentSequenceTextPointer tpScan = new DocumentSequenceTextPointer(tp1.ChildBlock, tp1.ChildPointer);
            while (tpScan.ChildBlock != tp2.ChildBlock)
            {
                // Skip the entire block to the end
                count += tpScan.ChildPointer.GetOffsetToPosition(tpScan.ChildPointer.TextContainer.End);

                // Move on to next block
                ChildDocumentBlock nextBlock = tpScan.ChildBlock.NextBlock;
                tpScan.ChildBlock       = nextBlock;
                tpScan.ChildPointer    = nextBlock.ChildContainer.Start;
            }
            count += tpScan.ChildPointer.GetOffsetToPosition(tp2.ChildPointer);
            return count;
        }

        // Move this TP by distance, and respect virtualization of child TextContainer
        // Return true if distance is within boundary of the aggregated container, false otherwise
        private static bool xGapAwareScan(DocumentSequenceTextPointer thisTp, int distance)
        {
            //
            // Note: To calculate distance between thisTp.ChildPointer to
            // it container Start/End position would have been devastating
            // for those who implemented vitualization.
            // Ideally we would need a new API on ITextPointer
            //      ITextPointer.IsDistanceOutOfRange
            ChildDocumentBlock cdb = thisTp.ChildBlock;
            bool isNavigator = true;
            ITextPointer childTn = thisTp.ChildPointer;
            if (childTn == null)
            {
                isNavigator = false;
                childTn = thisTp.ChildPointer.CreatePointer();
            }
            LogicalDirection scanDir = (distance > 0 ? LogicalDirection.Forward : LogicalDirection.Backward);
            distance = Math.Abs(distance);
            while (distance > 0)
            {
                TextPointerContext tst = childTn.GetPointerContext(scanDir);
                switch (tst)
                {
                    case TextPointerContext.ElementStart:
                        childTn.MoveToNextContextPosition(scanDir);
                        distance--;
                        break;

                    case TextPointerContext.ElementEnd:
                        childTn.MoveToNextContextPosition(scanDir);
                        distance--;
                        break;

                    case TextPointerContext.EmbeddedElement:
                        childTn.MoveToNextContextPosition(scanDir);
                        distance--;
                        break;

                    case TextPointerContext.Text:
                        int runLength  = childTn.GetTextRunLength(scanDir);
                        int moveLength = runLength < distance ? runLength : distance;
                        distance -= moveLength;
                        //agurcan: Fix for 1098225
                        //We need to propagate direction info to MoveByOffset
                        if (scanDir == LogicalDirection.Backward)
                        {
                            moveLength *= -1;
                        }
                        childTn.MoveByOffset(moveLength);
                        break;

                    case TextPointerContext.None:
                        if (!((cdb.IsHead && scanDir == LogicalDirection.Backward)
                              || (cdb.IsTail && scanDir == LogicalDirection.Forward)
                              )
                            )
                        {
                            cdb = (scanDir == LogicalDirection.Forward ? cdb.NextBlock : cdb.PreviousBlock);
                            childTn = (scanDir == LogicalDirection.Forward ?
                                      cdb.ChildContainer.Start.CreatePointer(childTn.LogicalDirection)
                                    : cdb.ChildContainer.End.CreatePointer(childTn.LogicalDirection)
                                  );
                        }
                        else
                        {
                            return false;
                        }
                        break;

                    default:
                        Debug.Assert(false, "invalid TextPointerContext");
                        break;
                }
            }

            // Re-position thisTp to the new location.
            thisTp.ChildBlock = cdb;
            if (isNavigator)
            {
                thisTp.ChildPointer = childTn;
            }
            else
            {
                thisTp.ChildPointer = childTn.CreatePointer();
            }
            return true;
        }

        #endregion Private Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        #region Private Fields

        private ChildDocumentBlock _childBlock;
        private ITextPointer _childTp;

        // True if Freeze has been called, in which case
        // this TextPointer is immutable and may not be repositioned.
        private bool _isFrozen;

#if DEBUG
        private uint _debugId = GetDebugId();
        private static uint _debugIdCounter = 0;
#endif
        #endregion Private Fields
    }
}
