// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Backing store for the PasswordBox control.
//

using System;
using System.Windows.Threading;
using System.Collections;
using System.Security;
using System.Diagnostics;
using System.Windows.Documents;
using MS.Internal;
using MS.Internal.Documents;

namespace System.Windows.Controls
{
    // Backing store for the PasswordBox control.
    //
    // This TextContainer implementation is unusual in that
    //
    //  - It never returns actual content through any of the abstract methods.
    //    If you need the actual content, you must cast to PasswordTextContainer
    //    and use the internal Password property.
    //
    //  - Performance doesn't scale with large documents.  The expectation is
    //    that the document will always be small (< 100 chars).
    //
    internal sealed class PasswordTextContainer : ITextContainer
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Creates a new PasswordTextContainer instance.
        internal PasswordTextContainer(PasswordBox passwordBox)
        {
            _passwordBox = passwordBox;
            _password = new SecureString();
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Inserts text at a specified position.
        /// </summary>
        /// <param name="position">
        /// Position at which to insert the new text.
        /// </param>
        /// <param name="textData">
        /// Text to insert.
        /// </param>
        /// <remarks>
        /// Use the CanInsertText method to determine if text may be inserted
        /// at position.
        ///
        /// All positions at this location are repositioned
        /// before or after the inserted text according to their gravity.
        /// </remarks>
        internal void InsertText(ITextPointer position, string textData)
        {
            int offset;
            int i;

            BeginChange();
            try
            {
                offset = ((PasswordTextPointer)position).Offset;

                // Strangely, there is no SecureString.InsertAt(offset, string), so
                // we must use a loop here.
                for (i = 0; i < textData.Length; i++)
                {
                    _password.InsertAt(offset + i, textData[i]);
                }

                OnPasswordChange(offset, textData.Length);
            }
            finally
            {
                EndChange();
            }
        }

        /// <summary>
        /// Removes content covered by a pair of positions.
        /// </summary>
        /// <param name="startPosition">
        /// Position preceeding the first symbol to delete.  startPosition must be
        /// scoped by the same text element as endPosition.
        /// </param>
        /// <param name="endPosition">
        /// Position following the last symbol to delete.  endPosition must be
        /// scoped by the same text element as startPosition.
        /// </param>
        /// <remarks>
        /// Use CanDeleteContent to determine if content may be removed.
        /// </remarks>
        internal void DeleteContent(ITextPointer startPosition, ITextPointer endPosition)
        {
            int startOffset;
            int endOffset;
            int i;

            BeginChange();
            try
            {
                startOffset = ((PasswordTextPointer)startPosition).Offset;
                endOffset = ((PasswordTextPointer)endPosition).Offset;

                // Strangely, there is no SecureString.RemoveAt(offset, count), so
                // we must use a loop here.
                for (i = 0; i < endOffset - startOffset; i++)
                {
                    _password.RemoveAt(startOffset);
                }

                OnPasswordChange(startOffset, startOffset - endOffset);
            }
            finally
            {
                EndChange();
            }
        }

        /// <summary>
        /// </summary>
        internal void BeginChange()
        {
            _changeBlockLevel++;

            // We'll raise the Changing event when/if we get an actual
            // change added, inside AddChangeSegment.
        }

        /// <summary>
        /// </summary>
        internal void EndChange()
        {
            EndChange(false /* skipEvents */);
        }

        /// <summary>
        /// </summary>
        internal void EndChange(bool skipEvents)
        {
            TextContainerChangedEventArgs changes;

            // probably should throw
            Invariant.Assert(_changeBlockLevel > 0, "Unmatched EndChange call!");

            _changeBlockLevel--;

            if (_changeBlockLevel == 0 &&
                _changes != null)
            {
                changes = _changes;
                _changes = null;

                // Contact any listeners.
                if (this.Changed != null && !skipEvents)
                {
                    Changed(this, changes);
                }
            }
        }

        /// <summary>
        /// </summary>
        void ITextContainer.BeginChange()
        {
            BeginChange();
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
            EndChange(false /* skipEvents */);
        }

        /// <summary>
        /// </summary>
        void ITextContainer.EndChange(bool skipEvents)
        {
            EndChange(skipEvents);
        }

        // Allocate a new ITextPointer at the specified offset.
        // Equivalent to this.Start.CreatePointer(offset), but does not
        // necessarily allocate this.Start.
        ITextPointer ITextContainer.CreatePointerAtOffset(int offset, LogicalDirection direction)
        {
            return new PasswordTextPointer(this, direction, offset);
        }

        // Allocate a new ITextPointer at a specificed offset in unicode chars within the document.
        ITextPointer ITextContainer.CreatePointerAtCharOffset(int charOffset, LogicalDirection direction)
        {
            return ((ITextContainer)this).CreatePointerAtOffset(charOffset, direction);
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

        // Adds a PasswordTextPointer to the list of live positions.
        // Positions in the list are updated as the document content changes.
        internal void AddPosition(PasswordTextPointer position)
        {
            int index;

            RemoveUnreferencedPositions();

            if (_positionList == null)
            {
                _positionList = new ArrayList();
            }

            index = FindIndex(position.Offset, position.LogicalDirection);

            _positionList.Insert(index, new WeakReference(position));

            DebugAssertPositionList();
        }

        // Removes a PasswordTextPointer from the list of live positions.
        // Positions in the list are updated as the document content changes.
        internal void RemovePosition(PasswordTextPointer searchPosition)
        {
            int index;
            PasswordTextPointer position;

            Invariant.Assert(_positionList != null);

            for (index = 0; index < _positionList.Count; index++)
            {
                position = GetPointerAtIndex(index);

                if (position == searchPosition)
                {
                    _positionList.RemoveAt(index);
                    // Tag index with a sentinel so we can assert below that we
                    // did find our position...
                    index = -1;
                    break;
                }
            }
            Invariant.Assert(index == -1, "Couldn't find position to remove!");
        }

        #endregion Internal Methods        

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Specifies whether or not the content of this PasswordTextContainer may be
        /// modified.
        /// </summary>
        /// <value>
        /// True if content may be modified, false otherwise.
        /// </value>
        /// <remarks>
        /// Methods that modify the PasswordTextContainer, such as InsertText or
        /// DeleteContent, will throw InvalidOperationExceptions if this
        /// property returns true.
        /// </remarks>
        bool ITextContainer.IsReadOnly
        { 
            get
            { 
                return false;
            }
        }

        /// <summary>
        /// A position preceding the first symbol of this PasswordTextContainer.
        /// </summary>
        /// <remarks>
        /// The returned ITextPointer has LogicalDirection.Backward gravity.
        /// </remarks>
        ITextPointer ITextContainer.Start
        {
            get
            {
                return this.Start;
            }
        }

        /// <summary>
        /// A position following the last symbol of this PasswordTextContainer.
        /// </summary>
        /// <remarks>
        /// The returned ITextPointer has LogicalDirection.Forward gravity.
        /// </remarks>
        ITextPointer ITextContainer.End
        {
            get
            {
                return this.End;
            }
        }

        /// <summary>
        /// Autoincremented counter of content changes in this TextContainer
        /// </summary>
        uint ITextContainer.Generation
        {
            get
            {
                //  This property is used only during textrange normalization. 
                // For completeness, we should implement this counter since PasswordBox content is editable. 
                // However in V1 we do not expose ranges in PasswordBox publicly.
                // Bug 1424449 tracks this issue.
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
                if (_highlights == null)
                {
                    _highlights = new Highlights(this);
                }

                return _highlights;
            }
        }

        /// <summary>
        /// The object containing this PasswordTextContainer, from which property
        /// values are inherited.
        /// </summary>
        /// <remarks>
        /// May be null.
        /// </remarks>
        DependencyObject ITextContainer.Parent
        {
            get
            {
                return _passwordBox;
            }
        }

        
        // We need to store the text selection as we will use it later on to access
        // the UiScope during splitting text for selection purposes.
        ITextSelection ITextContainer.TextSelection
        { 
            get
            {
                return _textSelection;
            }

            set
            {
                _textSelection = value;
            }
        }

        // Optional undo manager, always null for this ITextContainer.
        UndoManager ITextContainer.UndoManager
        {
            get
            {
                return null;
            }
        }

        // <see cref="System.Windows.Documents.ITextContainer/>
        ITextView ITextContainer.TextView
        {
            get
            {
                return this.TextView;
            }

            set
            {
                this.TextView = value;
            }
        }

        // TextView associated with this container.
        internal ITextView TextView
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
                return this.SymbolCount;
            }
        }

        // Character count for the entire tree.
        int ITextContainer.IMECharCount
        {
            get
            {
                return this.SymbolCount;
            }
        }

        /// <summary>
        /// A position preceding the first symbol of this PasswordTextContainer.
        /// </summary>
        /// <remarks>
        /// The returned ITextPointer has LogicalDirection.Backward gravity.
        /// </remarks>
        internal ITextPointer Start
        {
            get
            {
                return new PasswordTextPointer(this, LogicalDirection.Backward, 0);
            }
        }

        /// <summary>
        /// A position following the last symbol of this PasswordTextContainer.
        /// </summary>
        /// <remarks>
        /// The returned ITextPointer has LogicalDirection.Forward gravity.
        /// </remarks>
        internal ITextPointer End
        {
            get
            {
                return new PasswordTextPointer(this, LogicalDirection.Forward, this.SymbolCount);
            }
        }

        // The actual content of the PasswordTextContainer.
        // GetText will not reveal this content.
        internal SecureString GetPasswordCopy()
        {
            return _password.Copy();
        }

        // Sets the content with a copy of a supplied SecureString.
        internal void SetPassword(SecureString value)
        {
            int symbolCount;

            symbolCount = this.SymbolCount;
            _password.Clear();
            OnPasswordChange(0, -symbolCount);

            _password = (value == null) ? new SecureString() : value.Copy();
            OnPasswordChange(0, this.SymbolCount);
        }

        // The character count of the content.  Because this TextContainer
        // does not accept embedded objects or text elements, the symbol count
        // is the Unicode character count.
        internal int SymbolCount
        {
            get
            {
                return _password.Length;
            }
        }

        // The character used to obfuscate the actual content when GetText is
        // called.  Defaults to '*'.
        internal char PasswordChar
        {
            get
            {
                return PasswordBox.PasswordChar;
            }
        }

        // PasswordBox associated with this content.
        internal PasswordBox PasswordBox
        {
            get
            {
                return _passwordBox;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        #region Internal Events

        event EventHandler ITextContainer.Changing
        {
            add { Changing += value; }
            remove { Changing -= value; }
        }

        event TextContainerChangeEventHandler ITextContainer.Change
        {
            add { Change += value; }
            remove { Change -= value; }
        }

        event TextContainerChangedEventHandler ITextContainer.Changed
        {
            add { Changed += value; }
            remove { Changed -= value; }
        }

        #endregion Internal Events

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private void AddChange(ITextPointer startPosition, int symbolCount, PrecursorTextChangeType precursorTextChange)
        {
            Invariant.Assert(_changeBlockLevel > 0, "All public APIs must call BeginChange!");
            Invariant.Assert(!_isReadOnly, "Illegal to modify PasswordTextContainer inside Change event scope!");

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

            _changes.AddChange(precursorTextChange, startPosition.Offset, symbolCount, false /* collectTextChanges */);

            if (this.Change != null)
            {
                Invariant.Assert(precursorTextChange == PrecursorTextChangeType.ContentAdded || precursorTextChange == PrecursorTextChangeType.ContentRemoved);
                TextChangeType textChange = (precursorTextChange == PrecursorTextChangeType.ContentAdded) ?
                    TextChangeType.ContentAdded : TextChangeType.ContentRemoved;

                _isReadOnly = true;
                try
                {
                    Change(this, new TextContainerChangeEventArgs(startPosition, symbolCount, symbolCount, textChange));
                }
                finally
                {
                    _isReadOnly = false;
                }
            }
        }

        // Called whenever _password changes value.
        // Offset/delta identify the character offset at which chars have been
        // added (delta > 0) or removed (delta < 0).
        private void OnPasswordChange(int offset, int delta)
        {
            PasswordTextPointer textPosition;
            int symbolCount;
            PrecursorTextChangeType operation;

            if (delta != 0)
            {
                UpdatePositionList(offset, delta);

                textPosition = new PasswordTextPointer(this, LogicalDirection.Forward, offset);
                
                if (delta > 0)
                {
                    symbolCount = delta;
                    operation = PrecursorTextChangeType.ContentAdded;
                }
                else
                {
                    symbolCount = -delta;
                    operation = PrecursorTextChangeType.ContentRemoved;
                }

                AddChange(textPosition, symbolCount, operation);
            }
        }

        // Scans the list of live PasswordTextPositions, updating their
        // state to match a change to the document.
        // PasswordTextPositions "float" on the content, so need to adjust
        // to stay with local content after an insert or delete.
        private void UpdatePositionList(int offset, int delta)
        {
            int index;
            int backwardGravitySlot;
            PasswordTextPointer position;

            if (_positionList == null)
            {
                return;
            }

            RemoveUnreferencedPositions();

            // We ask for the first position at offset with Forward gravity.
            // This skips over all positions at offset with Backward gravity,
            // because we don't want to update them.
            index = FindIndex(offset, LogicalDirection.Forward);

            if (delta < 0)
            {
                // A delete.
                // Positions from offset to offset + -delta collapse to offset.

                // Track the first position index we found with Forward gravity.
                // As we walk along the list of positions scoped by the delete,
                // we'll use the index as a position to swap positions with
                // Backward gravity into.  This ensures that when we're done
                // all the positions with Offset == offset are sorted such that
                // Backward gravity positions precede Forward gravity positions.
                backwardGravitySlot = -1;

                for (; index < _positionList.Count; index++)
                {
                    position = GetPointerAtIndex(index);

                    if (position != null)
                    {
                        // If we found a position past the scope of the change,
                        // we can break out of this loop -- no more special cases.
                        if (position.Offset > offset + -delta)
                            break;

                        // Collapse the position down to the start offset.
                        position.Offset = offset;

                        // If the position has backward gravity, we need to make
                        // sure it stays sorted in our list -- positions at the
                        // same offset are sorted such that backward gravity
                        // positions precede forward gravity positions.
                        if (position.LogicalDirection == LogicalDirection.Backward)
                        {
                            if (backwardGravitySlot >= 0)
                            {
                                WeakReference tempWeakReference = (WeakReference)_positionList[backwardGravitySlot];
                                _positionList[backwardGravitySlot] = _positionList[index];
                                _positionList[index] = tempWeakReference;
                                backwardGravitySlot++;
                            }
                        }
                        else if (backwardGravitySlot == -1)
                        {
                            // This is the first position with Forward gravity,
                            // remember it.
                            backwardGravitySlot = index;
                        }
                    }
                }
            }

            // Fixup all the positions to the right of the insert/delete, but
            // not covered by a delete.
            for (; index < _positionList.Count; index++)
            {
                position = GetPointerAtIndex(index);

                if (position != null)
                {
                    position.Offset += delta;
                }
            }

            DebugAssertPositionList();
        }

        // Scans the list of live PasswordTextPositions, looking for entries
        // with no references.  If any are found, they are removed from the list.
        private void RemoveUnreferencedPositions()
        {
            int index;
            PasswordTextPointer position;

            if (_positionList == null)
            {
                return;
            }

            for (index = _positionList.Count-1; index >= 0; index--)
            {
                position = GetPointerAtIndex(index);

                if (position == null)
                {
                    _positionList.RemoveAt(index);
                }
            }
        }

        // Returns the index of the first PasswordTextPointer with the
        // specified offset and gravity in the live positions list.
        //
        // If no such position exists, returns the index of the next position,
        // the index at which a new position with the specified offset/gravity
        // would be inserted.
        private int FindIndex(int offset, LogicalDirection gravity)
        {
            PasswordTextPointer position;
            int index;

            Invariant.Assert(_positionList != null);

            for (index = 0; index < _positionList.Count; index++)
            {
                position = GetPointerAtIndex(index);

                if (position != null)
                {
                    if (position.Offset == offset &&
                        (position.LogicalDirection == gravity || gravity == LogicalDirection.Backward))
                    {
                        break;
                    }

                    if (position.Offset > offset)
                        break;
                }
            }

            return index;
        }

        // Debug only -- asserts the position list is in a good state.
        private void DebugAssertPositionList()
        {
            if (Invariant.Strict)
            {
                PasswordTextPointer position;
                int index;
                int lastOffset;
                LogicalDirection lastLogicalDirection;

                lastOffset = -1;
                lastLogicalDirection = LogicalDirection.Backward;

                for (index = 0; index < _positionList.Count; index++)
                {
                    position = GetPointerAtIndex(index);

                    if (position != null)
                    {
                        Invariant.Assert(position.Offset >= 0 && position.Offset <= _password.Length);
                        Invariant.Assert(lastOffset <= position.Offset);

                        if (index > 0 &&
                            position.LogicalDirection == LogicalDirection.Backward &&
                            lastOffset == position.Offset)
                        {
                            // Positions at the same offset should be ordered such
                            // that Backward gravity positions preceed Forward gravity positions.
                            Invariant.Assert(lastLogicalDirection != LogicalDirection.Forward);
                        }

                        lastOffset = position.Offset;
                        lastLogicalDirection = position.LogicalDirection;
                    }
                }
            }
        }

        // Returns a PasswordTextPointer at a given index within _positionList,
        // or null if the WeakReference at that index is dead.
        private PasswordTextPointer GetPointerAtIndex(int index)
        {
            WeakReference weakReference;
            object strongReference;
            PasswordTextPointer position;

            Invariant.Assert(_positionList != null);

            weakReference = (WeakReference)_positionList[index];
            Invariant.Assert(weakReference != null);

            strongReference = weakReference.Target;
            if (strongReference != null && !(strongReference is PasswordTextPointer))
            {
                // Diagnostics for bug 1267261.
                Invariant.Assert(false, "Unexpected type: " + strongReference.GetType());
            }

            position = (PasswordTextPointer)strongReference;

            return position;
        }

#if DEBUG
        // Debug only -- dumps the position list to the Console.
        // Use this method from the debugger's command window.
        private void DumpPositionList()
        {
            PasswordTextPointer position;
            int index;

            Debug.WriteLine(_positionList.Count + " entries.");

            for (index = 0; index < _positionList.Count; index++)
            {
                position = GetPointerAtIndex(index);

                if (position != null)
                {
                    Debug.Write("(" + position.DebugId + ") " + position.Offset + "/" + ((position.LogicalDirection == LogicalDirection.Forward) ? "f " : "b "));
                }
                else
                {
                    Debug.Write("-/- ");
                }
            }

            Debug.WriteLine("");
        }
#endif // DEBUG

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // PasswordBox associated with this content.
        private readonly PasswordBox _passwordBox;

        // The TextContainer content.
        private SecureString _password;

        // List of live PasswordTextPositions.
        private ArrayList _positionList;

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

        // implementation of ITextContainer.Changing
        private EventHandler Changing;

        // implementation of ITextContainer.Change
        private TextContainerChangeEventHandler Change;

        // implementation of ITextContainer.Changed
        private TextContainerChangedEventHandler Changed;

        // The current ITextSelection
        private ITextSelection _textSelection;

        #endregion Private Fields
    }
}
