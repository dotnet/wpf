// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: An override class representing a movable
//              position within a PasswordTextContainer.
//

namespace System.Windows.Controls
{
    using System;
    using System.Windows.Threading;
    using System.Diagnostics;
    using System.Windows.Documents;
    using MS.Internal;

    // TextNavigator implementation for the PasswordTextContainer.
    internal sealed class PasswordTextPointer : ITextPointer
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Creates a new PasswordTextPointer instance.
        internal PasswordTextPointer(PasswordTextContainer container, LogicalDirection gravity, int offset)
        {
            Debug.Assert(offset >= 0 && offset <= container.SymbolCount, "Bad PasswordTextPointer offset!");

            _container = container;
            _gravity = gravity;
            _offset = offset;

            container.AddPosition(this);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// <see cref="ITextPointer.SetLogicalDirection"/>
        /// </summary>
        void ITextPointer.SetLogicalDirection(LogicalDirection direction)
        {
            Debug.Assert(!_isFrozen, "Can't reposition a frozen pointer!");

            if (direction != _gravity)
            {
                // We need to remove the position from the container since we're
                // going to change its gravity, which changes its internal sort order.
                this.Container.RemovePosition(this);

                _gravity = direction;

                // Now start tracking the position again, at it's new sort order.
                this.Container.AddPosition(this);
            }
        }

        /// <summary>
        /// Compares this TextPointer with another TextPointer.
        /// </summary>
        /// <param name="position">
        /// The TextPointer to compare with.
        /// </param>
        /// <returns>
        /// Less than zero: this TextPointer preceeds position.
        /// Zero: this TextPointer is at the same location as position.
        /// Greater than zero: this TextPointer follows position.
        /// </returns>
        int ITextPointer.CompareTo(ITextPointer position)
        {
            int offset;
            int result;

            offset = ((PasswordTextPointer)position)._offset;

            if (_offset < offset)
            {
                result = -1;
            }
            else if (_offset > offset)
            {
                result = +1;
            }
            else
            {
                result = 0;
            }

            return result;
        }

        int ITextPointer.CompareTo(StaticTextPointer position)
        {
            return ((ITextPointer)this).CompareTo((ITextPointer)position.Handle0);
        }

        /// <summary>
        /// Returns the count of symbols between this TextPointer and another.
        /// </summary>
        /// <param name="position">
        /// TextPointer to compare.
        /// </param>
        /// <returns>
        /// The return value will be negative if
        /// position preceeds this TextPointer, zero if the two TextPositions
        /// are equally positioned, or positive if position follows this
        /// TextPointer.
        /// </returns>
        int ITextPointer.GetOffsetToPosition(ITextPointer position)
        {
            return ((PasswordTextPointer)position)._offset - _offset;
        }

        /// <summary>
        /// Returns the TextPointerContext of a symbol bordering this TextPointer
        /// in a given direction.
        /// </summary>
        /// <param name="direction">
        /// Direction to query.
        /// </param>
        /// <remarks>
        /// Returns TextPointerContext.None if the query is Backward at
        /// TextContainer.Start or Forward at TextContainer.End.
        /// </remarks>
        TextPointerContext ITextPointer.GetPointerContext(LogicalDirection direction)
        {
            TextPointerContext symbolType;

            if ((direction == LogicalDirection.Backward && _offset == 0) ||
                (direction == LogicalDirection.Forward && _offset == _container.SymbolCount))
            {
                symbolType = TextPointerContext.None;
            }
            else
            {
                symbolType = TextPointerContext.Text;
            }

            return symbolType;
        }

        /// <summary>
        /// Returns the count of characters between this TextPointer and the
        /// next non-Character symbol.
        /// </summary>
        /// <param name="direction">
        /// Direction to query.
        /// </param>
        /// <remarks>
        /// If a non-Character symbol follows immediately in the specified
        /// direction, returns zero.
        ///
        /// TextContainer implementation does not guarantee that all neighboring
        /// characters are counted, but this method must return values consistent
        /// with GetText and TextPointerBase.Move.
        /// </remarks>
        int ITextPointer.GetTextRunLength(LogicalDirection direction)
        {
            int length;

            if (direction == LogicalDirection.Forward)
            {
                length = _container.SymbolCount - _offset;
            }
            else
            {
                length = _offset;
            }

            return length;
        }

        // <see cref="System.Windows.Documents.ITextPointer.GetText"/>
        string ITextPointer.GetTextInRun(LogicalDirection direction)
        {
            return TextPointerBase.GetTextInRun(this, direction);
        }

        int ITextPointer.GetTextInRun(LogicalDirection direction, char[] textBuffer, int startIndex, int count)
        {
            int finalCount;
            int i;

            // Truncate based on document size.
            if (direction == LogicalDirection.Forward)
            {
                finalCount = Math.Min(count, _container.SymbolCount - _offset);
            }
            else
            {
                finalCount = Math.Min(count, _offset);
            }

            // Substitute a placeholder char for the real password text.
            char passwordChar = _container.PasswordChar;
            for (i = 0; i < finalCount; i++)
            {
                textBuffer[startIndex + i] = passwordChar;
            }

            return finalCount;
        }

        /// <summary>
        /// Returns an embedded object, if any, bordering this TextPointer in the
        /// specified direction.
        /// </summary>
        /// <param name="direction">
        /// Direction to query.
        /// </param>
        /// <returns>
        /// The embedded object, if any exists, otherwise null.
        /// </returns>
        object ITextPointer.GetAdjacentElement(LogicalDirection direction)
        {
            return null;
        }

        /// <summary>
        /// Returns the type of the text element whose edge is in a specified
        /// direction from position.
        /// </summary>
        /// <param name="direction">
        /// Direction to query.
        /// </param>
        /// <returns>
        /// If the symbol in the specified direction is
        /// TextPointerContext.ElementStart or TextPointerContext.ElementEnd, then this
        /// method will return the type of element whose edge preceeds this
        /// TextPointer.
        ///
        /// Otherwise, the method returns null.
        /// </returns>
        Type ITextPointer.GetElementType(LogicalDirection direction)
        {
            return null;
        }

        /// <summary>
        /// Tests if this position has the same scoping element as
        /// another one.
        /// </summary>
        /// <param name="position">
        /// Position to compare.
        /// </param>
        /// <returns>
        /// true if the scoping element for this position is the same
        /// instance as the one for a position passed as a parameter,
        /// or if the both positions have no scoping element.
        ///
        /// false otherwise.
        /// </returns>
        bool ITextPointer.HasEqualScope(ITextPointer position)
        {
            return true;
        }

        /// <summary>
        /// Returns the value of a DependencyProperty at the specified position.
        /// </summary>
        /// <param name="formattingProperty">
        /// Property to query.
        /// </param>
        /// <returns>
        /// Returns the value of the specified property at position.
        /// If the position has no scoping text element, and the TextContainer
        /// has a null Parent property, returns DependencyProperty.UnsetValue.
        /// </returns>
        /// <remarks>
        /// This method considers inherited values from the containing control.
        /// This method will return property values even from positions with
        /// no scoping element (when GetElement returns null).
        /// </remarks>
        object ITextPointer.GetValue(DependencyProperty formattingProperty)
        {
            return _container.PasswordBox.GetValue(formattingProperty);
        }

        /// <summary>
        /// Returns the local value of a DependencyProperty at the specified position.
        /// </summary>
        /// <param name="formattingProperty">
        /// Property to query.
        /// </param>
        object ITextPointer.ReadLocalValue(DependencyProperty formattingProperty)
        {
            // This method should never be called because we never have a scoping element.
            Debug.Assert(false);
            return DependencyProperty.UnsetValue;
        }

        /// <summary>
        /// Returns an enumerator for property values declared locally on this
        /// TextPointer's scoping element.  If there is no scoping element,
        /// returns an empty enumerator.
        /// </summary>
        LocalValueEnumerator ITextPointer.GetLocalValueEnumerator()
        {
            return (new DependencyObject()).GetLocalValueEnumerator();
        }

        ITextPointer ITextPointer.CreatePointer()
        {
            return new PasswordTextPointer(_container, _gravity, _offset);
        }

        // Unoptimized CreateStaticPointer implementation.
        // Creates a simple wrapper for an ITextPointer instance.
        StaticTextPointer ITextPointer.CreateStaticPointer()
        {
            return new StaticTextPointer(((ITextPointer)this).TextContainer, ((ITextPointer)this).CreatePointer());
        }

        ITextPointer ITextPointer.CreatePointer(int distance)
        {
            return new PasswordTextPointer(_container, _gravity, _offset + distance);
        }

        ITextPointer ITextPointer.CreatePointer(LogicalDirection gravity)
        {
            return new PasswordTextPointer(_container, gravity, _offset);
        }

        /// <summary>
        /// Returns an immutable TextPointer located at some distance
        /// relative to this TextPointer.
        /// </summary>
        /// <param name="distance">
        /// Distance, in symbols, of the new TextPointer relative to this one.
        /// distance may be negative, placing the new TextPositon behind this
        /// one.
        /// </param>
        /// <param name="gravity">
        /// Gravity of the new TextPointer.
        /// </param>
        /// <remarks>
        /// The returned position may be the same instance as the calling
        /// position if distance is zero and this TextPointer is immutable with
        /// equal gravity.
        /// </remarks>
        ITextPointer ITextPointer.CreatePointer(int distance, LogicalDirection gravity)
        {
            return new PasswordTextPointer(_container, gravity, _offset + distance);
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
            _container.InsertText(this, textData);
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
            _container.DeleteContent(this, limit);
        }

        /// <summary>
        /// Advances this TextNavigator over one or more symbols -- a single
        /// ElementStart/End or Object symbol, or a group of neighboring
        /// Character symbols.
        /// </summary>
        /// <param name="direction">
        /// Direction to move.
        /// </param>
        /// <returns>
        /// true if the navigator is repositioned, false if the navigator
        /// borders the start or end of the TextContainer.
        /// </returns>
        /// <remarks>
        /// If the following symbol is of type EmbeddedObject, ElementBegin,
        /// or ElementEnd, this TextNavigator is advanced by exactly one symbol.
        ///
        /// If the following symbol is of type Character, this TextNavigator is
        /// advanced by some number of Character symbols.  The exact value can
        /// determined in advance by calling TextPointer.GetTextLength at the
        /// current position.  The movement distance is not guaranteed to
        /// include all neighboring characters, but it must be consistent with
        /// TextPointer.GetTextLength and TextContainer.GetText.
        ///
        /// If there is no following symbol in the indicated direction, this
        /// TextNavigator is not repositioned and the method returns
        /// TextPointerContext.None.
        /// </remarks>
        bool ITextPointer.MoveToNextContextPosition(LogicalDirection direction)
        {
            int offset;

            Debug.Assert(!_isFrozen, "Can't reposition a frozen pointer!");

            if (direction == LogicalDirection.Backward)
            {
                if (this.Offset == 0)
                {
                    return false;
                }
                offset = 0;
            }
            else
            {
                if (this.Offset == this.Container.SymbolCount)
                {
                    return false;
                }
                offset = this.Container.SymbolCount;
            }

            this.Container.RemovePosition(this);

            this.Offset = offset;

            this.Container.AddPosition(this);

            return true;
        }

        /// <summary>
        /// Repositions this TextNavigator at a symbol offset relative to
        /// its current position.
        /// </summary>
        /// <param name="distance">
        /// The offset count, in symbols.  This value may be
        /// negative, positioning this TextNavigator behind its current
        /// position.
        /// </param>
        int ITextPointer.MoveByOffset(int distance)
        {
            int offset;

            Debug.Assert(!_isFrozen, "Can't reposition a frozen pointer!");

            offset = this.Offset + distance;

            if (offset < 0 || offset > this.Container.SymbolCount)
            {
                Debug.Assert(false, "Bad distance!");
            }

            this.Container.RemovePosition(this);

            this.Offset = offset;

            this.Container.AddPosition(this);

            return distance;
        }

        /// <summary>
        /// Repositions this TextNavigator at the location of a TextPointer.
        /// </summary>
        /// <param name="position">
        /// TextPointer to move to.
        /// </param>
        void ITextPointer.MoveToPosition(ITextPointer position)
        {
            Debug.Assert(!_isFrozen, "Can't reposition a frozen pointer!");

            this.Container.RemovePosition(this);

            this.Offset = ((PasswordTextPointer)position).Offset;

            this.Container.AddPosition(this);
        }

        /// <summary>
        /// Repositions this TextNavigator at an element edge of its scoping
        /// text element.
        /// </summary>
        /// <param name="edge">
        /// The specific edge to move to.
        /// </param>
        void ITextPointer.MoveToElementEdge(ElementEdge edge)
        {
            Debug.Assert(!_isFrozen, "Can't reposition a frozen pointer!");
            Debug.Assert(false, "No scoping element!");
        }

        // <see cref="TextPointer.MoveToLineBoundary"/>
        int ITextPointer.MoveToLineBoundary(int count)
        {
            return TextPointerBase.MoveToLineBoundary(this, _container.TextView, count);
        }

        // <see cref="TextPointer.GetCharacterRect"/>
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

        /// <see cref="ITextPointer.ValidateLayout"/>
        bool ITextPointer.ValidateLayout()
        {
            return TextPointerBase.ValidateLayout(this, _container.TextView);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // <see cref="System.Windows.Documents.ITextPointer.ParentType"/>
        Type ITextPointer.ParentType
        {
            get
            {
                return null;
            }
        }

        // The PasswordTextContainer associated with this PasswordTextPointer.
        ITextContainer ITextPointer.TextContainer
        {
            get
            {
                return _container;
            }
        }

        // <see cref="TextPointer.HasValidLayout"/>
        bool ITextPointer.HasValidLayout
        {
            get
            {
                return (_container.TextView != null && _container.TextView.IsValid && _container.TextView.Contains(this));
            }
        }

        // <see cref="ITextPointer.IsAtCaretUnitBoundary"/>
        bool ITextPointer.IsAtCaretUnitBoundary
        {
            get
            {
                // Because the PasswordTextContainer only ever contains PasswordChars
                // (as far as the renderer is concerned) all positions are caret
                // unit boundaries.
                return true;
            }
        }

        /// <summary>
        /// Returns the gravity of this TextPointer.
        /// </summary>
        /// <remarks>
        /// Gravity determines where a TextPointer moves after an insertion of
        /// content at its location.  Backward gravity implies the TextPointer
        /// sticks with its preceding symbol; forward gravity implies the TextPointer
        /// sticks with its following symbol.
        /// </remarks>
        LogicalDirection ITextPointer.LogicalDirection
        {
            get
            {
                return _gravity;
            }
        }

        bool ITextPointer.IsAtInsertionPosition
        {
            get { return TextPointerBase.IsAtInsertionPosition(this); }
        }

        // <see cref="TextPointer.IsFrozen"/>
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

        // Offset in unicode chars within the document.
        int ITextPointer.CharOffset
        {
            get
            {
                return this.Offset;
            }
        }

        // The PasswordTextContainer associated with this PasswordTextPointer.
        internal PasswordTextContainer Container
        {
            get
            {
                return _container;
            }
            // setter removed (unused internal API).  If needed, recall from history.
        }

        // This PasswordTextPointer's gravity.
        internal LogicalDirection LogicalDirection
        {
            get
            {
                return _gravity;
            }
            // setter removed (unused internal API).  If needed, recall from history.
        }

        // The offset, in symbols, of this PasswordTextPointer.
        // NB: do not modify this value directly unless you have first removed
        // the positoin from PasswordTextContainer's _positionList with
        // PasswordTextContainter.RemovePosition!  Failure to do so will break
        // the list's sort order.
        internal int Offset
        {
            get
            {
                return _offset;
            }

            set
            {
                _offset = value;
            }
        }

#if DEBUG
        // Unique debug-only identifier for this instance.
        internal int DebugId
        {
            get
            {
                return _debugId;
            }
        }
#endif // DEBUG

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // The PasswordTextContainer associated with this PasswordTextPointer.
        private PasswordTextContainer _container;

        // This position's gravity.
        private LogicalDirection _gravity;

        // The offset, in symbols, of this PasswordTextPointer.
        // NB: do not modify this value directly unless you have first removed
        // the positoin from PasswordTextContainer's _positionList with
        // PasswordTextContainter.RemovePosition!  Failure to do so will break
        // the list's sort order.
        private int _offset;

        // True if Freeze has been called, in which case
        // this TextPointer is immutable and may not be repositioned.
        private bool _isFrozen;

#if DEBUG
        private static int _debugIdCounter;

        // Unique debug-only identifier for this instance.
        private int _debugId = _debugIdCounter++;
#endif

        #endregion Private Fields
    }
}
