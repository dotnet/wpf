// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Attached to custom journal entries created for changes in the DocumentApplication's
//   UI state. 
//

using System;
using System.Runtime.Serialization;
using System.Security;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using System.Windows.Controls;

namespace MS.Internal.Documents.Application
{
    /// <remarks>
    /// DocumentApplicationJournalEntry is not a real journal entry, just the CustomContentState
    /// attached to one. It wraps a DocumentApplicationState object, which is the actual view state.
    /// The split is needed because PresentationUI cannot access internal Framework classes and methods.
    /// </remarks>
    [Serializable]
    internal sealed class DocumentApplicationJournalEntry : System.Windows.Navigation.CustomContentState
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        /// <summary>
        /// Constructs a DocumentApplicationJournalEntry
        /// </summary>
        /// <param name="state">State of the DocumentApplication to journal</param>
        /// <param name="name">Name of the journal entry to display in the UI.
        /// If this is null it will default to the URI source.</param>
        public DocumentApplicationJournalEntry(object state, string name)
        {
            Invariant.Assert(state is DocumentApplicationState,
                "state should be of type DocumentApplicationState");

            // Store parameters locally.
            _state = state;
            _displayName = name;
        }

        public DocumentApplicationJournalEntry(object state) :
            this(state, null)
        {
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Used to reset the UI to state of this entry
        /// </summary>
        /// <param name="navigationService">NavigationService currently running</param>
        /// <param name="mode">Navigation direction</param>
        public override void Replay(NavigationService navigationService, NavigationMode mode)
        {
            ContentControl navigator = (ContentControl)navigationService.INavigatorHost;
            // Find a reference to the DocumentViewer hosted in the NavigationWindow
            // On initial history navigation in the browser, the window's layout may not have been 
            // done yet. ApplyTemplate() causes the viewer to be created.
            navigator.ApplyTemplate();
            DocumentApplicationDocumentViewer docViewer = navigator.Template.FindName(
                "PUIDocumentApplicationDocumentViewer", navigator)
                as DocumentApplicationDocumentViewer;
            Debug.Assert(docViewer != null, "PUIDocumentApplicationDocumentViewer not found.");
            if (docViewer != null)
            {
                // Set the new state on the DocumentViewer
                if (_state is DocumentApplicationState)
                {
                    docViewer.StoredDocumentApplicationState = (DocumentApplicationState)_state;
                }

                // Check that a Document exists.
                if (navigationService.Content != null)
                {
                    IDocumentPaginatorSource document = navigationService.Content as IDocumentPaginatorSource;

                    // If the document has already been paginated (could happen in the
                    // case of a fragment navigation), then set the DocumentViewer to the
                    // new state that was set.
                    if ((document != null) && (document.DocumentPaginator.IsPageCountValid))
                    {
                        docViewer.SetUIToStoredState();
                    }
                }
            }
        }

        public override string JournalEntryName
        {
            get { return _displayName; }
        }

        //------------------------------------------------------
        //
        //  Private Fields.
        //
        //------------------------------------------------------

        // The DocumentApplicationState has been weakly-typed to avoid PresentationFramework
        // having a type dependency on PresentationUI.  The perf impact of the weak
        // typed variables in this case was determined to be much less than forcing the load
        // of a new assembly when Assembly.GetTypes was called on PresentationFramework.
        private object _state; // DocumentApplicationState

        private string _displayName;
    }
}
