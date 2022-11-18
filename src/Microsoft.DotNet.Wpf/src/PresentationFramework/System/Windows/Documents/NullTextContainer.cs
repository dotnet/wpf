// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      NullTextContainer is an immutable empty TextContainer that contains 
//      single collapsed Start/End position. This is primarily used internally 
//      so parameter check is mostly replaced with debug assert.  
//

namespace System.Windows.Documents
{
    using System;
    using System.Diagnostics;
    using System.Windows.Threading;
    using System.Windows;                // DependencyID etc.
    using MS.Internal.Documents;
    using MS.Internal;

    //=====================================================================
    /// <summary>
    /// NullTextContainer is an immutable empty TextContainer that contains 
    /// single collapsed Start/End position. This is primarily used internally 
    /// so parameter check is mostly replaced with debug assert.  
    /// </summary>
    internal sealed class NullTextContainer : ITextContainer
    {
        //--------------------------------------------------------------------
        //
        // Connstructors
        //
        //---------------------------------------------------------------------

        #region Constructors

        internal NullTextContainer()
        {
            _start  = new NullTextPointer(this, LogicalDirection.Backward);
            _end    = new NullTextPointer(this, LogicalDirection.Forward);
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
        /// <see cref="ITextContainer.IsReadOnly" />
        /// </summary>
        bool ITextContainer.IsReadOnly
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// <see cref="ITextContainer.Start"/>
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
        /// <see cref="ITextContainer.End"/>
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
                //agurcan: The following line makes it hard to use debugger on FDS code so I'm commenting it out
                //Debug.Assert(false, "Unexpected Highlights access on NullTextContainer!");
                return null;
            }
        }

        /// <summary>
        /// <see cref="ITextContainer.Parent"/>
        /// </summary>
        DependencyObject ITextContainer.Parent
        {
            get { return null; }
        }

        // Optional text selection, always null for this ITextContainer.
        ITextSelection ITextContainer.TextSelection
        { 
            get { return null; }
            set { Invariant.Assert(false, "NullTextContainer is never associated with a TextEditor/TextSelection!"); }
        }

        // Optional undo manager, always null for this ITextContainer.
        UndoManager ITextContainer.UndoManager { get { return null; } }

        /// <summary>
        /// <see cref="ITextContainer"/>
        /// </summary>
        ITextView ITextContainer.TextView
        {
            get
            {
                return null;
            }

            set
            {
                Debug.Assert(false, "Unexpected call to NullTextContainer.set_TextView!");
            }
        }

        // Count of symbols in this tree, equivalent to this.Start.GetOffsetToPosition(this.End),
        // but doesn't necessarily allocate anything.
        int ITextContainer.SymbolCount
        {
            get
            {
                return 0;
            }
        }

        // Not implemented.
        int ITextContainer.IMECharCount
        {
            get
            {
                Invariant.Assert(false); // Should never be called.
                return 0;
            }
        }

        #endregion Public Properties
 
        //--------------------------------------------------------------------
        //
        // Public Events
        //
        //---------------------------------------------------------------------
        
        #region Public Events

        // 9/15/2004: these don't need to be "public",
        // they're part of the ITextContainer interface.  How do
        // we qualify them with the interface name?  ITextContainer.Changed
        // doesn't compile.
        public event EventHandler Changing { add { } remove { } }

        public event TextContainerChangeEventHandler Change { add { } remove { } }

        public event TextContainerChangedEventHandler Changed { add { } remove { } }

        #endregion Public Events


        //--------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private Fields
        private NullTextPointer    _start;
        private NullTextPointer    _end;

        #endregion Private Fields
    }
}
