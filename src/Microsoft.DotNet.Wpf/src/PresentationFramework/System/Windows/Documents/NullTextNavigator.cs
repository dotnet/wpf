// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      TextNavigator implementation for NullTextContainer
//      This is primarily used by internal code.
//

#pragma warning disable 1634, 1691 // To enable presharp warning disables (#pragma suppress) below.

namespace System.Windows.Documents
{
    using System;
    using System.Diagnostics;
    using System.Windows;
    using MS.Internal;

    /// <summary>
    /// NullTextPointer is an implementation of ITextPointer for NullTextContainer
    /// </summary>
    internal sealed class NullTextPointer : ITextPointer
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        #region Constructors
        // Ctor always set mutable flag to false
        internal NullTextPointer(NullTextContainer container, LogicalDirection gravity)
        {
            _container = container;
            _gravity = gravity;
        }
        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        #region ITextPointer Methods
        /// <summary>
        /// <see cref="TextPointer.CompareTo"/>
        /// </summary>
        int ITextPointer.CompareTo(ITextPointer position)
        {
            Debug.Assert(position is NullTextPointer || position is NullTextPointer);
            // There is single position in the container.
            return 0;
        }

        int ITextPointer.CompareTo(StaticTextPointer position)
        {
            // There is single position in the container.
            return 0;
        }

        /// <summary>
        /// <see cref="TextPointer.GetOffsetToPosition"/>
        /// </summary>
        int ITextPointer.GetOffsetToPosition(ITextPointer position)
        {
            Debug.Assert(position is NullTextPointer || position is NullTextPointer);
            // There is single position in the container.
            return 0;
        }

        /// <summary>
        /// <see cref="ITextPointer.GetPointerContext"/>
        /// </summary>
        TextPointerContext ITextPointer.GetPointerContext(LogicalDirection direction)
        {
            // There is no content for this container
            return TextPointerContext.None;
        }

        /// <summary>
        /// <see cref="ITextPointer.GetTextRunLength"/>
        /// </summary>
        /// <remarks>Return 0 if non-text run</remarks>
        int ITextPointer.GetTextRunLength(LogicalDirection direction)
        {
            // There is no content in this container
            return 0;
        }

        /// <summary>
        /// <see cref="ITextPointer.GetTextInRun(LogicalDirection)"/>
        /// </summary>
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
            // There is no content in this container.
            return 0;
        }

        /// <summary>
        /// <see cref="ITextPointer.GetAdjacentElement"/>
        /// </summary>
        /// <remarks>Return null if the embedded object does not exist</remarks>
        object ITextPointer.GetAdjacentElement(LogicalDirection direction)
        {
            // There is no content in this container.
            return null;
        }

        /// <summary>
        /// <see cref="ITextPointer.GetElementType"/>
        /// </summary>
        /// <remarks>Return null if no TextElement in the direction</remarks>
        Type ITextPointer.GetElementType(LogicalDirection direction)
        {
            // There is no content in this container.
            return null;
        }

        /// <summary>
        /// <see cref="ITextPointer.HasEqualScope"/>
        /// </summary>
        bool ITextPointer.HasEqualScope(ITextPointer position)
        {
            return true;
        }

        /// <summary>
        /// <see cref="ITextPointer.GetValue"/>
        /// </summary>
        /// <remarks>return property values even if there is no scoping element</remarks>
        object ITextPointer.GetValue(DependencyProperty property)
        {
            return property.DefaultMetadata.DefaultValue;
        }

        /// <summary>
        /// <see cref="ITextPointer.ReadLocalValue"/>
        /// </summary>
        object ITextPointer.ReadLocalValue(DependencyProperty property)
        {
            return DependencyProperty.UnsetValue;
        }

        /// <summary>
        /// <see cref="ITextPointer.GetLocalValueEnumerator"/>
        /// </summary>
        /// <remarks>Returns an empty enumerator if there is no scoping element</remarks>
        LocalValueEnumerator ITextPointer.GetLocalValueEnumerator()
        {
            return (new DependencyObject()).GetLocalValueEnumerator();
        }

        /// <summary>
        /// <see cref="ITextPointer.CreatePointer()"/>
        /// </summary>
        ITextPointer ITextPointer.CreatePointer()
        {
            return ((ITextPointer)this).CreatePointer(0, _gravity);
        }

        // Unoptimized CreateStaticPointer implementation.
        // Creates a simple wrapper for an ITextPointer instance.
        StaticTextPointer ITextPointer.CreateStaticPointer()
        {
            return new StaticTextPointer(((ITextPointer)this).TextContainer, ((ITextPointer)this).CreatePointer());
        }

        /// <summary>
        /// <see cref="ITextPointer.CreatePointer(int)"/>
        /// </summary>
        ITextPointer ITextPointer.CreatePointer(int distance)
        {
            return ((ITextPointer)this).CreatePointer(distance, _gravity);
        }

        /// <summary>
        /// <see cref="ITextPointer.CreatePointer(LogicalDirection)"/>
        /// </summary>
        ITextPointer ITextPointer.CreatePointer(LogicalDirection gravity)
        {
            return ((ITextPointer)this).CreatePointer(0, gravity);
        }

        /// <summary>
        /// <see cref="ITextPointer.CreatePointer(int,LogicalDirection)"/>
        /// </summary>
        ITextPointer ITextPointer.CreatePointer(int distance, LogicalDirection gravity)
        {
            // There is no content in this container
            Debug.Assert(distance == 0);
            return new NullTextPointer(_container, gravity);
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

        #endregion ITextPointer Methods

        #region ITextPointer Methods

        /// <summary>
        /// <see cref="ITextPointer.SetLogicalDirection"/>
        /// </summary>
        /// <param name="direction"></param>
        void ITextPointer.SetLogicalDirection(LogicalDirection direction)
        {
            ValidationHelper.VerifyDirection(direction, "gravity");
            Debug.Assert(!_isFrozen, "Can't reposition a frozen pointer!");

            _gravity = direction;
        }

        /// <summary>
        /// <see cref="ITextPointer.MoveByOffset"/>
        /// </summary>
        bool ITextPointer.MoveToNextContextPosition(LogicalDirection direction)
        {
            Debug.Assert(!_isFrozen, "Can't reposition a frozen pointer!");

            // Nowhere to move in an empty container
            return false;
        }


        /// <summary>
        /// <see cref="ITextPointer.MoveByOffset"/>
        /// </summary>
        int ITextPointer.MoveByOffset(int distance)
        {
            Debug.Assert(!_isFrozen, "Can't reposition a frozen pointer!");

            Debug.Assert(distance == 0, "Single possible position in this empty container");

            return 0;
        }

        /// <summary>
        /// <see cref="ITextPointer.MoveToPosition"/>
        /// </summary>
        void ITextPointer.MoveToPosition(ITextPointer position)
        {
            Debug.Assert(!_isFrozen, "Can't reposition a frozen pointer!");

            // There is single possible position in this empty container.
        }

        /// <summary>
        /// <see cref="ITextPointer.MoveToElementEdge"/>
        /// </summary>
        void ITextPointer.MoveToElementEdge(ElementEdge edge)
        {
            Debug.Assert(!_isFrozen, "Can't reposition a frozen pointer!");

            Debug.Assert(false, "No scoping element!");
        }

        /// <summary>
        /// <see cref="ITextPointer.MoveToLineBoundary"/>
        /// </summary>
        int ITextPointer.MoveToLineBoundary(int count)
        {
            Debug.Assert(false, "NullTextPointer does not expect layout dependent method calls!");
            return 0;
        }

        /// <summary>
        /// <see cref="ITextPointer.GetCharacterRect"/>
        /// </summary>
        Rect ITextPointer.GetCharacterRect(LogicalDirection direction)
        {
            Debug.Assert(false, "NullTextPointer does not expect layout dependent method calls!");
            return new Rect();
        }

        /// <summary>
        /// <see cref="ITextPointer.MoveToInsertionPosition"/>
        /// </summary>
        bool ITextPointer.MoveToInsertionPosition(LogicalDirection direction)
        {
            return TextPointerBase.MoveToInsertionPosition(this, direction);
        }

        /// <summary>
        /// <see cref="ITextPointer.MoveToNextInsertionPosition"/>
        /// </summary>
        bool ITextPointer.MoveToNextInsertionPosition(LogicalDirection direction)
        {
            return TextPointerBase.MoveToNextInsertionPosition(this, direction);
        }

        /// <summary>
        /// <see cref="ITextPointer.InsertTextInRun"/>
        /// </summary>
        void ITextPointer.InsertTextInRun(string textData)
        {
            Debug.Assert(false); // must never call this
        }

        /// <summary>
        /// <see cref="ITextPointer.DeleteContentToPosition"/>
        /// </summary>
        void ITextPointer.DeleteContentToPosition(ITextPointer limit)
        {
            Debug.Assert(false); // must never call this
        }

        /// <summary>
        /// Candidate for replacing MoveToNextContextPosition for immutable TextPointer model
        /// <see cref="ITextPointer.GetNextContextPosition"/>
        /// </summary>
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

        /// <summary>
        /// Candidate for replacing MoveToInsertionPosition for immutable TextPointer model
        /// <see cref="ITextPointer.GetInsertionPosition"/>
        /// </summary>
        ITextPointer ITextPointer.GetInsertionPosition(LogicalDirection direction)
        {
            ITextPointer pointer = ((ITextPointer)this).CreatePointer();
            pointer.MoveToInsertionPosition(direction);
            pointer.Freeze();
            return pointer;
        }

        /// <summary>
        /// Returns the closest insertion position, treating all unicode code points
        /// as valid insertion positions.  A useful performance win over 
        /// GetNextInsertionPosition when only formatting scopes are important.
        /// <see cref="ITextPointer.GetFormatNormalizedPosition"/>
        /// </summary>
        ITextPointer ITextPointer.GetFormatNormalizedPosition(LogicalDirection direction)
        {
            ITextPointer pointer = ((ITextPointer)this).CreatePointer();
            TextPointerBase.MoveToFormatNormalizedPosition(pointer, direction);
            pointer.Freeze();
            return pointer;
        }

        /// <summary>
        /// Candidate for replacing MoveToNextInsertionPosition for immutable TextPointer model
        /// <see cref="ITextPointer.GetNextInsertionPosition"/>
        /// </summary>
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

        /// <summary>
        /// <see cref="ITextPointer.ValidateLayout"/>
        /// </summary>
        bool ITextPointer.ValidateLayout()
        {
            return false;
        }

        #endregion ITextPointer Methods


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region ITextPointer Properties

        /// <summary>
        /// <see cref="ITextPointer.ParentType"/>
        /// </summary>
        Type ITextPointer.ParentType
        {
            get
            {
                // There is no content in this container.
                // We want to behave consistently with FixedTextPointer so we know
                // we're at the root when the parent is a FixedDocument
                return typeof(FixedDocument);
            }
        }

        /// <summary>
        /// <see cref="ITextPointer.TextContainer"/>
        /// </summary>
        ITextContainer ITextPointer.TextContainer
        {
            get { return _container; }
        }

        /// <summary>
        /// <see cref="ITextPointer.HasValidLayout"/>
        /// </summary>
        bool ITextPointer.HasValidLayout
        {
            get
            {
                // NullTextContainer's never have a layout.
                return false;
            }
        }

        /// <summary>
        /// <see cref="ITextPointer.IsAtCaretUnitBoundary"/>
        /// </summary>
        bool ITextPointer.IsAtCaretUnitBoundary
        {
            get
            {
                Invariant.Assert(false, "NullTextPointer never has valid layout!");
                return false;
            }
        }

        /// <summary>
        /// <see cref="ITextPointer.LogicalDirection"/>
        /// </summary>
        LogicalDirection ITextPointer.LogicalDirection
        {
            get { return _gravity; }
        }

        /// <summary>
        /// <see cref="ITextPointer.IsAtInsertionPosition"/>
        /// </summary>
        bool ITextPointer.IsAtInsertionPosition
        {
            get { return TextPointerBase.IsAtInsertionPosition(this); }
        }

        /// <summary>
        /// <see cref="ITextPointer.IsFrozen"/>
        /// </summary>
        bool ITextPointer.IsFrozen
        {
            get
            {
                return _isFrozen;
            }
        }

        /// <summary>
        /// <see cref="ITextPointer.Offset"/>
        /// </summary>
        int ITextPointer.Offset
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Not implemented.
        /// <see cref="ITextPointer.CharOffset"/>
        /// </summary>
        int ITextPointer.CharOffset
        {
            get
            {
                    #pragma warning suppress 56503
                    throw new NotImplementedException();
            }
        }

        #endregion ITextPointer Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        #region Private Fields
        private LogicalDirection    _gravity;
        private NullTextContainer   _container;

        // True if Freeze has been called, in which case
        // this TextPointer is immutable and may not be repositioned.
        private bool _isFrozen;

        #endregion Private Fields
    }
}

