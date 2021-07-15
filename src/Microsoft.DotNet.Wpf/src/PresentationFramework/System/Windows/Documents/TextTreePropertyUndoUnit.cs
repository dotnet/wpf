// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Undo unit for TextContainer.SetValue, etc. calls.
//

using System;
using MS.Internal;

namespace System.Windows.Documents
{
    // Undo unit for TextContainer.SetValue, etc. calls.
    internal class TextTreePropertyUndoUnit : TextTreeUndoUnit
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Create a new undo unit instance.
        // symbolOffset is where property values will be set.
        internal TextTreePropertyUndoUnit(TextContainer tree, int symbolOffset, PropertyRecord propertyRecord) : base(tree, symbolOffset)
        {
            _propertyRecord = propertyRecord;
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
            TextPointer position;

            VerifyTreeContentHashCode();

            position = new TextPointer(this.TextContainer, this.SymbolOffset, LogicalDirection.Forward);

            Invariant.Assert(position.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart, "TextTree undo unit out of sync with TextTree.");

            if (_propertyRecord.Value != DependencyProperty.UnsetValue)
            {
                this.TextContainer.SetValue(position, _propertyRecord.Property, _propertyRecord.Value);
            }
            else
            {
                position.Parent.ClearValue(_propertyRecord.Property);
            }
        }

        #endregion Public Methods        

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Property/value pair to restore.
        private readonly PropertyRecord _propertyRecord;

        #endregion Private Fields
    }
}

