// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//      Implements the Avalon Journal Object
//
//      The WCP application journal enables users to retrace their steps backward 
//      and forward in a linear navigation sequence. Whether a navigation application 
//      is hosted in the browser or in a standalone NavigationWindow, each navigation 
//      is persisted in the journal, and can be revisited in a linear sequence by 
//      using the Forward and Back buttons. An application can have multiple 
//      NavigationWindows. Each NavigationWindow has its own Journal.
//
//      The Windows Client Platform will also provide some value adds over the 
//      current journaling behavior. Developers will be able to add their own journal entries,
//      and to remove entries from the journal (within their own application).
//
//
// 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Windows.Threading;
using System.Security;

using MS.Internal;
using MS.Internal.AppModel;
using MS.Utility;
using System.ComponentModel;

// Since we disable PreSharp warnings in this file, we first need to disable warnings about unknown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

namespace System.Windows.Navigation
{
    /// <summary>
    /// Journal object is provided for each NavigationWindow for linear
    /// navigations in history. Developers can also add or remove entries
    /// from the journal.
    /// </summary>
    /// <speclink>http://avalon/app/Journalling/Journaling.doc</speclink>
    [Serializable]
    internal sealed class Journal : ISerializable
    {
        /// <summary>
        /// Construct a new Journal instance.
        /// </summary>
        internal Journal()
        {
            _Initialize();
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("_journalEntryList", _journalEntryList);
            info.AddValue("_currentEntryIndex", _currentEntryIndex);
            info.AddValue("_journalEntryId", _journalEntryId);
        }

        /// <summary>
        /// Ctor for ISerializable implementation
        /// </summary>
        private Journal(SerializationInfo info, StreamingContext context)
        {
            _Initialize();
            _journalEntryList = (List<JournalEntry>)info.GetValue("_journalEntryList", typeof(List<JournalEntry>));
            _currentEntryIndex = info.GetInt32("_currentEntryIndex");
            _uncommittedCurrentIndex = _currentEntryIndex;
            _journalEntryId = info.GetInt32("_journalEntryId");
        }

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        #region Internal Properties

        #region Operator Overloads
        /// <summary>
        /// Gets the journal entry at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the journal entry to get or set.</param>
        /// <returns>The journal entry at the specified index.</returns>
        // CONSIDER: Do we want to make this public or limit access to only entries which are navigable?
        // If we want to expose an index, then we should also implement ICollection so they can get 
        // the count and Copy the Journal list as well (safe to do so since they have a separate copy
        // that is pretty much read-only list of entries like the main one we hold onto, any changes in 
        // the copy won't be reflected here anyway). For now my vote is no, since they may end up 
        // iterating over a stale copy of the JournalEntry list whereas the Enumerator will always ensure
        // they are looking at the current list
        internal JournalEntry this[int index]
        {
            get 
            { 
                return _journalEntryList[index]; 
            }
        }
        #endregion Operator Overloads

        // Total number of entries in the journal including non-navigable entries
        internal int TotalCount
        {
            get 
            { 
                return _journalEntryList.Count; 
            }
        }

        /// <summary>
        /// Current index - could be in the middle of the list when in history
        /// navigation. Else will be at the end of the list, a new entry will be 
        /// added at this index for normal navigations
        /// </summary>
        internal int CurrentIndex
        {
            get 
            { 
                return _currentEntryIndex; 
            }
        }

        /// <summary>
        /// Get the current journal entry.
        /// </summary>
        internal JournalEntry CurrentEntry
        {
            get 
            {
                if (_currentEntryIndex >= 0 && _currentEntryIndex < TotalCount)
                {
                    return _journalEntryList[_currentEntryIndex];
                }
                else
                {
                    return null;
                }
            }
        }

        internal bool HasUncommittedNavigation
        {
            get { return _uncommittedCurrentIndex != _currentEntryIndex; }
        }

        /// <summary>
        /// The getter for the BackStack
        /// </summary>
        /// <value>Gets the BackStack</value>
        internal JournalEntryStack BackStack
        {
            get
            {
                return _backStack;
            }
        }

        /// <summary>
        /// The getter for the ForwardStack
        /// </summary>
        /// <value>Gets the ForwardStack</value>
        internal JournalEntryStack ForwardStack
        {
            get
            {
                return _forwardStack;
            }
        }

        /// <summary>
        /// Check if there are journal entries for going back.
        /// </summary>
        internal bool CanGoBack
        {
            get
            {
                return GetGoBackEntry() != null;
            }
        }

        /// <summary>
        /// Check if there are journal entries for going forward.
        /// </summary>
        internal bool CanGoForward
        {
            get
            {
                int index;
                GetGoForwardEntryIndex(out index);
                return index != -1;
            }
        }

        /// <summary>
        /// Returns a journal version used to invalidate old enumerators after journal data changes
        /// </summary>
        /// <value>Current journal version</value>
        internal int Version
        {
            get
            {
                return _version;
            }
        }

        internal JournalEntryFilter Filter
        {
            get { return _filter; }
            set
            {
                _filter = value;
                BackStack.Filter = _filter;
                ForwardStack.Filter = _filter;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        #region Internal Events
        /// <summary>
        /// Raised when the contents of the BackStack or ForwardStack changes.
        /// Note that this doesn't always mean CanGoBack/CanGoForward has changed.
        /// </summary>
        internal event EventHandler BackForwardStateChange
        {
            add { _backForwardStateChange += value; }
            remove { _backForwardStateChange -= value; }
        }

        [NonSerialized()]
        EventHandler _backForwardStateChange;
        #endregion

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //-----------------------------------------------------
       
        #region Internal Methods

        /// <summary>
        /// Remove the top JournalEntry from back entry
        /// </summary>
        // Not a true "remove" 
        internal JournalEntry RemoveBackEntry()
        {
            Debug.Assert(ValidateIndexes());
            int index = _currentEntryIndex; // start from current but do not change it
            do
            {
                if (--index < 0)
                {
                    return null;
                }
            } while (IsNavigable(_journalEntryList[index]) == false);
            JournalEntry removedEntry = RemoveEntryInternal(index);
            Debug.Assert(ValidateIndexes());
            UpdateView();
            return removedEntry;
        }

        /// <summary>
        /// Ensures current data about the current page is stored in the journal.
        /// This either updates an existing entry or adds a new one.
        /// </summary>
        /// <param name="journalEntry"></param>
        internal void UpdateCurrentEntry(JournalEntry journalEntry)
        {
            if (journalEntry == null)
            {
                throw new ArgumentNullException("journalEntry");
            }
            Debug.Assert(journalEntry.ContentId != 0);
            Debug.Assert(!(journalEntry.IsAlive() && journalEntry.JEGroupState.JournalDataStreams != null),
                "Keep-alive content state should not be serialized.");

            if (_currentEntryIndex > -1 && _currentEntryIndex < TotalCount)
            {
                // update existing entry using the old entry's index.
                // Note: the new entry can be for a different NavigationService.
                JournalEntry oldEntry = _journalEntryList[_currentEntryIndex];
                journalEntry.Id = oldEntry.Id;
                _journalEntryList[_currentEntryIndex] = journalEntry;
            }
            else
            {
                // add new entry to the front
                journalEntry.Id = ++_journalEntryId;
                _journalEntryList.Add(journalEntry);
            }
            _version++;

            // If the next navigation is not #fragment or CustomContentState, this entry should be
            // remembered as the "exit" entry for the group, so when navigating back to the same
            // page, it will be shown. (It is not necessarily the last one in the group.)
            // Journal filtering will hide all other entries while at another page (different
            // NavigationService.Content object).
            journalEntry.JEGroupState.GroupExitEntry = journalEntry;
        }

        internal void RecordNewNavigation()
        {
            Invariant.Assert(ValidateIndexes());
            Debug.Assert(_uncommittedCurrentIndex == _currentEntryIndex,
                "This method should be called only in steady state.");

            // moves _currentEntryIndex forward
            // clear forward entries if necessary

            _currentEntryIndex++;
            _uncommittedCurrentIndex = _currentEntryIndex;

            if (!ClearForwardStack())
            {
                // If ClearForwardStack() didn't change the journal, UpdateView() needs to be
                // called here to enable the Back button.
                UpdateView();
            }
        }

        internal bool ClearForwardStack()
        {
            Debug.Assert(ValidateIndexes());

            if (_currentEntryIndex >= TotalCount)
                return false; // nothing to do

            if(_uncommittedCurrentIndex > _currentEntryIndex)
                throw new InvalidOperationException(SR.Get(SRID.InvalidOperation_CannotClearFwdStack));

            _journalEntryList.RemoveRange(_currentEntryIndex, _journalEntryList.Count - _currentEntryIndex);
            UpdateView();
            return true;
        }

        internal void CommitJournalNavigation(JournalEntry navigated)
        {
            NavigateTo(navigated);
        }

        internal void AbortJournalNavigation()
        {
            _uncommittedCurrentIndex = _currentEntryIndex;
            UpdateView();
        }

        /// <summary>
        /// Get the previous journal entry without changing any indexes.
        /// </summary>
        /// <returns>Null if we cannot go back, otherwise the journal entry on the top of the back stack</returns>
        internal JournalEntry BeginBackNavigation()
        {
            Invariant.Assert(ValidateIndexes());

            int index;
            JournalEntry journalEntry = GetGoBackEntry(out index);
            if (journalEntry == null)
                throw new InvalidOperationException(SR.Get(SRID.NoBackEntry));
            _uncommittedCurrentIndex = index;
            UpdateView();
            if (_uncommittedCurrentIndex == _currentEntryIndex)
                return null; // See BeginForwardNavigation() for explanation of this special case.
            return journalEntry;
        }

        internal JournalEntry BeginForwardNavigation()
        {
            Invariant.Assert(ValidateIndexes());

            int fwdEntryIndex;

            GetGoForwardEntryIndex(out fwdEntryIndex);
            if (fwdEntryIndex == -1)
                throw new InvalidOperationException(SR.Get(SRID.NoForwardEntry));

            _uncommittedCurrentIndex = fwdEntryIndex;
            UpdateView();

            if (fwdEntryIndex == _currentEntryIndex)
            {
                // this is a special case where the user BeginBackNavigation() was called but not allowed to finish
                // before BeginForwardNavigation() was called.  
                // Note that _uncommittedCurrentIndex may be less than _currentEntryIndex-1 at this
                // point. That's because there might be non-navigable entries between the two indexes...
                // Returning null indicates to the caller that it should stop any current navigation
                // and remain at the current page. If reloading of the current page were allowed,
                // its controls' state would be lost.
                return null;
            }

            return _journalEntryList[fwdEntryIndex];
        }

        /// <summary>
        /// For jump navigation this determines if it is a backwards or forwards navigation
        /// </summary>
        internal NavigationMode GetNavigationMode(JournalEntry entry)
        {
            int index = _journalEntryList.IndexOf(entry);

            if (index <= _currentEntryIndex)
            {
                // If index = _currentEntryIndex it means the application is being navigated back to
                // in the browser.  The browser has just loaded the journal and is restoring the 
                // current page.  This would also work if we chose "forward" but it must be one of the
                // two so that NavigationService will complete the navigation with CommitJournalNavigation()
                return NavigationMode.Back;
            }
            else
            {
                return NavigationMode.Forward;
            }
        }

        internal void NavigateTo(JournalEntry target)
        {
            Debug.Assert(IsNavigable(target), "target must be navigable");
            Debug.Assert(ValidateIndexes());

            int index = _journalEntryList.IndexOf(target);

            // When navigating back to a page which contains a previously navigated frame a 
            // saved journal entry is replayed to restore the frame’s location, in many cases 
            // this entry is not in the journal.
            if (index > -1)
            {
                _currentEntryIndex = index;
                _uncommittedCurrentIndex = _currentEntryIndex;
                UpdateView();
            }
        }

        internal int FindIndexForEntryWithId(int id)
        {
            // Search the list
            for (int i = 0; i < TotalCount; i++)
            {
                if (this[i].Id == id)
                {
                    return i;
                }
            }
            
            // Didn't find it
            return -1;
        }

        // This is only called from ApplicationProxyInternal.GetSaveHistoryBytes when
        // we are persisting the entire journal; we only do that when we're quitting.
        // [new] Also when navigating a Frame that has its own journal.
        //  What happens to a bunch of PageFunctions, some of which are KeepAlive
        // and some of which are not? We'll get "holes" in the "call stack" when we go
        // back.
        internal void PruneKeepAliveEntries()
        {
            for (int i = TotalCount - 1; i >= 0; --i)
            {
                JournalEntry je = _journalEntryList[i];
                if (je.IsAlive())
                {
                    RemoveEntryInternal(i); 
                }
                else
                {
                    Debug.Assert(je.GetType().IsSerializable);
                    // There can be keep-alive JEs creates for child frames.
                    DataStreams jds = je.JEGroupState.JournalDataStreams;
                    if (jds != null)
                    {
                        jds.PrepareForSerialization();
                    }

                    if (je.RootViewerState != null)
                    {
                        je.RootViewerState.PrepareForSerialization();
                    }
                }
            }
        }

        /// <remarks> The caller is responsible for calling UpdateView(). </remarks>
        internal JournalEntry RemoveEntryInternal(int index)
        {
            Debug.Assert(index < TotalCount && index >= 0, "Invalid index passed to RemoveEntryInternal");
            Debug.Assert(_uncommittedCurrentIndex == _currentEntryIndex, 
                "This method should be called only in steady state.");

            JournalEntry theEntry = _journalEntryList[index];
            Debug.Assert(theEntry != null, "Journal list state is messed up");

            // Increase version always, see note above the data member declaration
            _version++;

            _journalEntryList.RemoveAt(index);
            if (_currentEntryIndex > index)
            {
                _currentEntryIndex--;
            }
            if (_uncommittedCurrentIndex > index)
            {
                _uncommittedCurrentIndex--;
            }

            return theEntry;
        }

        internal void RemoveEntries(Guid navSvcId)
        {
            for (int i = TotalCount - 1; i >= 0; i--)
            {
                // The entry at _currentEntryIndex is just a placeholder. It should not be deleted.
                // Otherwise, the following entry (first one in the "forward stack") will get overwritten 
                // if a Back navigation occurs next.
                if (i != _currentEntryIndex)
                {
                    JournalEntry entry = _journalEntryList[i];
                    if (entry.NavigationServiceId == navSvcId)
                    {
                        RemoveEntryInternal(i);
                    }
                }
            }

            UpdateView();
        }

        //[IsKeepAlive() moved to NavigationService.IsContentKeepAlive()]

        internal void UpdateView()
        {
            BackStack.OnCollectionChanged();
            ForwardStack.OnCollectionChanged();
            if (_backForwardStateChange != null)
            {
                _backForwardStateChange(this, EventArgs.Empty);
            }
        }

        /// <summary> Returns the entry the GoBack command would navigate to; null/-1 if can't go back. </summary>
        internal JournalEntry GetGoBackEntry(out int index)
        {
            for (index = _uncommittedCurrentIndex - 1; index >= 0; index--)
            {
                JournalEntry je = _journalEntryList[index];
                if (IsNavigable(je))
                {
                    return je;
                }
            }
            return null; // and index=-1
        }
        internal JournalEntry GetGoBackEntry()
        {
            int unused;
            return GetGoBackEntry(out unused);
        }

        /// <summary> 
        /// Returns the index of the entry the GoForward command would navigate to; -1 if can't
        /// go forward.
        /// </summary>
        /// <remarks> 
        /// This funtion is not symmetric to GetGoBackEntry() becaue of the special case when
        /// _currentEntryIndex=TotalCount and _uncommittedCurrentIndex=TotalCount-1. Then there is
        /// no JournalEntry object to return (but fwd navigation is allowed--to the current page).
        /// </remarks>
        internal void GetGoForwardEntryIndex(out int index)
        {
            Debug.Assert(ValidateIndexes());

            // Special case: _uncommittedCurrentIndex=_currentEntryIndex=TotalCount-1.
            // Then we can't go fwd. But if _currentEntryIndex=TotalCount, we can. 
            // See also the special case in BeginForwardNavigation().
            index = _uncommittedCurrentIndex;
            do {
                index++;
                if (index == _currentEntryIndex)
                {
                    return;
                }
                if (index >= TotalCount)
                {
                    index = -1;
                    return;
                }
            } while (!IsNavigable(_journalEntryList[index]));
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary> Checks that the internal indices are not out of range. If an index is equal
        /// to TotalCount, it is valid, but there is no JournalEntry created yet (for the current page).
        /// </summary>
        private bool ValidateIndexes()
        {
            return _currentEntryIndex >= 0 && _currentEntryIndex <= TotalCount
                && _uncommittedCurrentIndex >= 0 && _uncommittedCurrentIndex <= TotalCount;
        }

        private void _Initialize()
        {
            _backStack = new JournalEntryBackStack(this);
            _forwardStack = new JournalEntryForwardStack(this);
        }

        internal bool IsNavigable(JournalEntry entry)
        {
            if (entry == null)
                return false;
            // Fallback to entry.IsNavigable if the Filter hasn't been specified
            return (Filter != null) ? Filter(entry) : entry.IsNavigable();
        }
        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private JournalEntryFilter  _filter;

        JournalEntryBackStack       _backStack;
        JournalEntryForwardStack    _forwardStack;

        // This is where we get the id we assign to all JournalEntries.
        // It will be incremented each time.
        // This is stored in WINDOWDATA structure of the browser's travellog. Trident uses it to 
        // identify frame windows and decide if a frame journal entry is invocable or not. We use it 
        // to identify the JournalEntry which has the NavigationService Guid to identify navigable frame
        // entries in the current context. When the travelentry is invoked we use this id to find
        // the JournalEntry to navigate it to. Since we don't explicitly remove the entry from the browser's
        // travellog when it is removed from the internal Avalon journal, we need to keep this id 
        // unique so we can respond correctly to the CanInvokeEntry calls from the browser. As such 
        // this id needs to be serialized so we can continue to assign unique numbers to each journal entry
        // if we navigate away and back to the avalon app in the journal
        //
        // ISSUE: Multiple browser applications activated in the same browser window (incl. multiple 
        // instances of the same app) need to also use unique ids. Otherwise they can get mixed up.
        // This can also happen when opening a new window from the current one (Ctrl+N). Then the 
        // TravelLog is copied. 
        //   Unfortunately, IE does not distinguish the entries of multiple instances of the same 
        // DocObject when making calls on ITravelLogClient. It gives us only a DWORD for the id 
        // ('dwWindowID'). Attempts to ensure a globally unique instance id across all PresentationHost
        // instances proved impractical due to restricted access rights. The solution here is to use
        // the system tick count as an initial value and keep incrementing it. This should be good 
        // enough in all normal usage scenarios.
        private int _journalEntryId = MS.Win32.SafeNativeMethods.GetTickCount();

        private List<JournalEntry> _journalEntryList = new List<JournalEntry>();
        private int _currentEntryIndex = 0;

        // This index is used to support the case where the back/forward is called multiple times
        // without letting the first navigation finish loading.  For example if the page is at 'C'
        // and the back stack contains 'b','a' and Back() is called twice the user should end up at 
        // 'a'. Navigation to 'b' starts but is canceled before it finishes when navigation to 'a' begins.
        private int _uncommittedCurrentIndex = 0;

        // Incremented everytime a journal entry is added/removed/updated. The enumerator
        // operation will then be invalidated since the list it was enumerating over has now
        // changed. This is the standard implementation used by the .Net ArrayList enumerator too.
        // We could optimize for the case for when the changes happen at an index greater than
        // the enumerator index (enumerator would do this check against a lastDirtyIndex that the
        // journal would maintain). But this will be bad if we decided to implement ICollection later
        // since we would then export the Count which would need to be invalidated as well.
        // Also if the enumerator user maintains some kind of count of entries he is interested in
        // then the index/count would be invalidated.
        private int _version;

        #endregion
    }

    internal delegate bool JournalEntryFilter(JournalEntry entry);
}
