// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description: Undo unit for TextContainer.UpdateIMELeftEdgeDeferred calls.
//

using MS.Internal;

namespace System.Windows.Documents
{
    // Undo unit for TextContainer.UpdateIMELeftEdgeDeferred calls.
    internal class TextTreeIMEDeferralUndoUnit : TextTreeUndoUnit
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Creates a new undo unit instance.
        // symbolOffset should be just before the start edge of the TextElement.
        internal TextTreeIMEDeferralUndoUnit(TextContainer tree, int symbolOffset, bool isDeferred) : base(tree, symbolOffset)
        {
            _isDeferred = isDeferred;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        // Called by the undo manager.  Restores tree state to its condition
        // when the unit was created.  Assumes the tree state matches conditions
        // just after the unit was created.
        public override void DoCore()
        {
            TextPointer start;

            VerifyTreeContentHashCode();

            start = new TextPointer(this.TextContainer, this.SymbolOffset, LogicalDirection.Forward);

            Invariant.Assert(start.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart, "TextTree undo unit out of sync with TextTree.");

            // If the composition has ended, this reveals the edge.
            this.TextContainer.UpdateIMELeftEdgeDeferred(start.GetAdjacentElementFromOuterPosition(LogicalDirection.Forward), _isDeferred);
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // The left edge was deferred before the change.
        private readonly bool _isDeferred;

        #endregion Private Fields
    }
}
