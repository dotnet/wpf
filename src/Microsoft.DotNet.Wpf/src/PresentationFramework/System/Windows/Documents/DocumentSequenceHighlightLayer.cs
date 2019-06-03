// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: DocumentSequence's HighlightLayer for TextSelection.
//

namespace System.Windows.Documents
{
    using MS.Internal.Documents;
    using System;
    using System.Diagnostics;
    using System.Collections;
    using System.Collections.Generic;

    // A special HighlightLayer that exists only to notify a FixedDocument
    // of changes to its highlights when the highlights are stored on a
    // DocumentSequenceTextContainer.
    // This layer is set on a FixedDocumentTextContainer that's part of a
    // DocumentSequence.  When the DocumentSequence's highlight layer changes
    // it determines which sub-documents need to be notified and uses the
    // instance of this class for each of those documents to notify it.
    // The FixedDoc then uses the DocumentSequence's Highlights directly to
    // get the highlight information.
    // Note: Some of the methods below are used in constructing the event args
    // but others are not. This class is not intended to be used directly as an 
    // actual highlight layer.
    internal class DocumentSequenceHighlightLayer : HighlightLayer
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        internal DocumentSequenceHighlightLayer(DocumentSequenceTextContainer docSeqContainer)
        {
            _docSeqContainer = docSeqContainer;
        }
        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Method is not implemented.  Should not need to be called for constructing the event args.
        internal override object GetHighlightValue(StaticTextPointer staticTextPointer, LogicalDirection direction)
        {
            Debug.Assert(false, "This method is not implemented and not expected to be called.");
            return null;
        }

        // Returns whether or not this text pointer has a highlight on it.  Determines this by checking
        // the highlights of the DocumentSequence.
        internal override bool IsContentHighlighted(StaticTextPointer staticTextPointer, LogicalDirection direction)
        {
            return this._docSeqContainer.Highlights.IsContentHighlighted(staticTextPointer, direction);
        }

        // Returns the next change position starting from the passed in StaticTextPointer.  Determines this by checking
        // the highlights of the DocumentSequence.
        internal override StaticTextPointer GetNextChangePosition(StaticTextPointer staticTextPointer, LogicalDirection direction)
        {
            return this._docSeqContainer.Highlights.GetNextHighlightChangePosition(staticTextPointer, direction);
        }

        // Called by the DocumentSequenceTextContainer to communicate changes to its highlight layer
        // to the FixedDocumentTextContainer which contains this layer.
        internal void RaiseHighlightChangedEvent(IList ranges)
        {
            DocumentsTrace.FixedDocumentSequence.Highlights.Trace(string.Format("DSHL.RaiseHighlightChangedEvent ranges={0}", ranges.Count));
            Debug.Assert(ranges.Count > 0);
            if (this.Changed != null)
            {
                DocumentSequenceHighlightChangedEventArgs args;
                args = new DocumentSequenceHighlightChangedEventArgs(ranges);
                this.Changed(this, args);
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Type identifying this layer for Highlights.GetHighlightValue calls.
        /// </summary>
        internal override Type OwnerType
        {
            get
            {
                return typeof(TextSelection);
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        #region Internal Events

        // Event raised when a highlight range is inserted, removed, moved, or
        // has a local property value change.
        internal override event HighlightChangedEventHandler Changed;

        #endregion Internal Events

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields
        private readonly DocumentSequenceTextContainer _docSeqContainer;
        #endregion Private Fields


        //------------------------------------------------------
        //
        //  Private Classes
        //
        //------------------------------------------------------

        #region Private Classes
        // Argument for the Changed event, encapsulates a highlight change.
        private class DocumentSequenceHighlightChangedEventArgs : HighlightChangedEventArgs
        {
            // Constructor.
            internal DocumentSequenceHighlightChangedEventArgs(IList ranges)
            {
                _ranges = ranges;
            }

            // Collection of changed content ranges.
            internal override IList Ranges
            {
                get
                {
                    return _ranges;
                }
            }

            // Type identifying the owner of the changed layer.
            internal override Type OwnerType
            {
                get
                {
                    return typeof(TextSelection);
                }
            }

            // Collection of changed content ranges.
            private readonly IList _ranges;
        }
        #endregion Private Classes
    }
}
