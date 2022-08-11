// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Used as a custom journal entry for changes in the DocumentApplication's UI state.

using MS.Internal.PresentationUI;   // For FriendAccessAllowed

using System;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace MS.Internal.Documents.Application
{
    /// <summary>
    /// SignatureStatusEventArgs, object used when firing SigStatus change.
    /// </summary>
    [FriendAccessAllowed]
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
