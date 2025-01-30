// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: Used as a custom journal entry for changes in the DocumentApplication's UI state.

using System;

namespace MS.Internal.Documents.Application
{
    /// <summary>
    /// SignatureStatusEventArgs, object used when firing SigStatus change.
    /// </summary>
    internal class DocumentApplicationJournalEntryEventArgs : EventArgs
    {

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        /// <summary>
        /// The constructor
        /// </summary>
        public DocumentApplicationJournalEntryEventArgs(DocumentApplicationState state)
        {
            _state = state;
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        /// <summary>
        /// State value stored at this JournalEntry
        /// </summary>
        public DocumentApplicationState State
        {
            get { return _state; }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        private DocumentApplicationState _state;
    }
}
