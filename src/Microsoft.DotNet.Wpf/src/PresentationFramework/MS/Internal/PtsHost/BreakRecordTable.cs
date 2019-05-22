// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: BreakRecordTable manages cached informaion bout pages and 
//              break records of FlowDocument contnet.
//


using System;                           // WeakReference, ...
using System.Collections.Generic;       // List<T>
using System.Collections.ObjectModel;   // ReadOnlyCollection<T>
using System.Windows.Documents;         // FlowDocument, TextPointer
using MS.Internal.Documents;            // TextDocumentView

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// BreakRecordTable manages cached informaion bout pages and break 
    /// records of FlowDocument contnet.
    /// </summary>
    internal sealed class BreakRecordTable
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">Ownder of the BreakRecordTable.</param>
        internal BreakRecordTable(FlowDocumentPaginator owner)
        {
            _owner = owner;
            _breakRecords = new List<BreakRecordTableEntry>();
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Retrieves input BreakRecord for given PageNumber.
        /// </summary>
        /// <param name="pageNumber">
        /// Page index indicating which input BreakRecord should be retrieved.</param>
        /// <returns>Input BreakRecord for given PageNumber.</returns>
        internal PageBreakRecord GetPageBreakRecord(int pageNumber)
        {
            PageBreakRecord breakRecord = null;

            Invariant.Assert(pageNumber >= 0 && pageNumber <= _breakRecords.Count, "Invalid PageNumber.");

            // Input BreakRecord for the first page is always NULL.
            // For the rest of pages, go to the entry preceding requested index and 
            // return the output BreakRecord.
            if (pageNumber > 0)
            {
                Invariant.Assert(_breakRecords[pageNumber - 1] != null, "Invalid BreakRecordTable entry.");
                breakRecord = _breakRecords[pageNumber - 1].BreakRecord;
                Invariant.Assert(breakRecord != null, "BreakRecord can be null only for the first page.");
            }
            return breakRecord;
        }

        /// <summary>
        /// Retrieves cached DocumentPage for given PageNumber.
        /// </summary>
        /// <param name="pageNumber">
        /// Page index indicating which cached DocumentPage should be retrieved.
        /// </param>
        /// <returns>Cached DocumentPage for given PageNumber.</returns>
        internal FlowDocumentPage GetCachedDocumentPage(int pageNumber)
        {
            WeakReference pageRef;
            FlowDocumentPage documentPage = null;

            if (pageNumber < _breakRecords.Count)
            {
                Invariant.Assert(_breakRecords[pageNumber] != null, "Invalid BreakRecordTable entry.");
                pageRef = _breakRecords[pageNumber].DocumentPage;
                if (pageRef != null)
                {
                    documentPage = pageRef.Target as FlowDocumentPage;
                    if (documentPage != null && documentPage.IsDisposed)
                    {
                        documentPage = null;
                    }
                }
            }
            return documentPage;
        }

        /// <summary>
        /// Retrieves PageNumber for specified ContentPosition.
        /// </summary>
        /// <param name="contentPosition">
        /// Represents content position for which PageNumber is requested.</param>
        /// <param name="pageNumber">Starting index for search process.</param>
        /// <returns>
        /// Returns true, if successfull. 'pageNumber' is updated with actual
        ///     page number that contains specified ContentPosition.
        /// Returns false, if BreakRecordTable is missing information about
        /// page that contains specified ContentPosition. 'pageNumber'
        /// is updated with the last investigated page number.
        /// </returns>
        internal bool GetPageNumberForContentPosition(TextPointer contentPosition, ref int pageNumber)
        {
            bool foundPageNumber = false;
            ReadOnlyCollection<TextSegment> textSegments;

            Invariant.Assert(pageNumber >= 0 && pageNumber <= _breakRecords.Count, "Invalid PageNumber.");

            // Iterate through entries in the BreakRecordTable (starting from specified index) 
            // and look for page that contains specified ContentPosition.
            // NOTE: For each cached page collection of TextSegments is stored to 
            // optimize this search.
            while (pageNumber < _breakRecords.Count)
            {
                Invariant.Assert(_breakRecords[pageNumber] != null, "Invalid BreakRecordTable entry.");
                textSegments = _breakRecords[pageNumber].TextSegments;
                if (textSegments != null)
                {
                    if (TextDocumentView.Contains(contentPosition, textSegments))
                    {
                        foundPageNumber = true;
                        break;
                    }
                }
                else
                {
                    // There is no information about this page.
                    break;
                }
                ++pageNumber;
            }
            return foundPageNumber;
        }

        /// <summary>
        /// Layout of entire content has been affected.
        /// </summary>
        internal void OnInvalidateLayout()
        {
            if (_breakRecords.Count > 0)
            {
                // Destroy all affected BreakRecords.
                InvalidateBreakRecords(0, _breakRecords.Count);

                // Initiate the next async operation.
                _owner.InitiateNextAsyncOperation();

                // Raise PagesChanged event. Start with the first page and set 
                // count to Int.Max/2, because somebody might want to display a page
                // that wasn't available before, but will be right now.
                _owner.OnPagesChanged(0, int.MaxValue/2);
            }
        }

        /// <summary>
        /// Layout for specified range has been affected.
        /// </summary>
        /// <param name="start">Start of the affected content range.</param>
        /// <param name="end">End of the affected content range.</param>
        internal void OnInvalidateLayout(ITextPointer start, ITextPointer end)
        {
            int pageStart, pageCount;

            if (_breakRecords.Count > 0)
            {
                // Get range of affected pages and dispose them
                GetAffectedPages(start, end, out pageStart, out pageCount);

                // Currently there is no possibility to do partial invalidation
                // of BreakRecordTable, so always extend pageCount to the end
                // of BreakRecordTable.
                pageCount = _breakRecords.Count - pageStart;
                if (pageCount > 0)
                {
                    // Destroy all affected BreakRecords.
                    InvalidateBreakRecords(pageStart, pageCount);

                    // Initiate the next async operation.
                    _owner.InitiateNextAsyncOperation();

                    // Raise PagesChanged event. Start with the first affected page and set 
                    // count to Int.Max/2, because somebody might want to display a page
                    // that wasn't available before, but will be right now.
                    _owner.OnPagesChanged(pageStart, int.MaxValue/2);
                }
            }
        }

        /// <summary>
        /// Rendering of entire content has been affected.
        /// </summary>
        internal void OnInvalidateRender()
        {
            if (_breakRecords.Count > 0)
            {
                // Dispose all existing pages.
                DisposePages(0, _breakRecords.Count);

                // Raise PagesChanged event. Start with the first page and set 
                // count to this.Count (number of pages have not been changed).
                _owner.OnPagesChanged(0, _breakRecords.Count);
            }
        }

        /// <summary>
        /// Rendering for specified range has been affected.
        /// </summary>
        /// <param name="start">Start of the affected content range.</param>
        /// <param name="end">End of the affected content range.</param>
        internal void OnInvalidateRender(ITextPointer start, ITextPointer end)
        {
            int pageStart, pageCount;

            if (_breakRecords.Count > 0)
            {
                // Get range of affected pages and dispose them.
                GetAffectedPages(start, end, out pageStart, out pageCount);
                if (pageCount > 0)
                {
                    // Dispose all affected pages.
                    DisposePages(pageStart, pageCount);

                    // Raise PagesChanged event. 
                    _owner.OnPagesChanged(pageStart, pageCount);
                }
            }
        }

        /// <summary>
        /// Updates entry of BreakRecordTable with new data.
        /// </summary>
        /// <param name="pageNumber">Index of the entry to update.</param>
        /// <param name="page">DocumentPage object that has been just created.</param>
        /// <param name="brOut">Output BreakRecord for created page.</param>
        /// <param name="dependentMax">Last content position that can affect the output break record.</param>
        internal void UpdateEntry(int pageNumber, FlowDocumentPage page, PageBreakRecord brOut, TextPointer dependentMax)
        {
            ITextView textView;
            BreakRecordTableEntry entry;
            bool isClean;

            Invariant.Assert(pageNumber >= 0 && pageNumber <= _breakRecords.Count, "The previous BreakRecord does not exist.");
            Invariant.Assert(page != null && page != DocumentPage.Missing, "Cannot update BRT with an invalid document page.");
            
            // Get TextView for DocumentPage. This TextView is used to access list of
            // content ranges. Those serve as optimalization in finding affeceted pages.
            textView = (ITextView)((IServiceProvider)page).GetService(typeof(ITextView));
            Invariant.Assert(textView != null, "Cannot access ITextView for FlowDocumentPage.");

            // Get current state of BreakRecordTable
            isClean = this.IsClean;

            // Add new entry into BreakRecordTable
            entry = new BreakRecordTableEntry();
            entry.BreakRecord = brOut;
            entry.DocumentPage = new WeakReference(page);
            entry.TextSegments = textView.TextSegments;
            entry.DependentMax = dependentMax;
            if (pageNumber == _breakRecords.Count)
            {
                _breakRecords.Add(entry);

                // Raise PaginationProgress event only if we did not have valid
                // entry for specified page number.
                _owner.OnPaginationProgress(pageNumber, 1);
            }
            else
            {
                // If old Page and/or BreakRecord are not changing, do not dispose them.
                if (_breakRecords[pageNumber].BreakRecord != null && 
                    _breakRecords[pageNumber].BreakRecord != entry.BreakRecord)
                {
                    _breakRecords[pageNumber].BreakRecord.Dispose();
                }
                if (_breakRecords[pageNumber].DocumentPage != null && 
                    _breakRecords[pageNumber].DocumentPage.Target != null &&
                    _breakRecords[pageNumber].DocumentPage.Target != entry.DocumentPage.Target)
                {
                    ((FlowDocumentPage)_breakRecords[pageNumber].DocumentPage.Target).Dispose();
                }
                _breakRecords[pageNumber] = entry;
            }

            // Raise PaginationCompleted event only if the BreakRecordTable just
            // become clean.
            if (!isClean && this.IsClean)
            {
                _owner.OnPaginationCompleted();
            }
        }

        /// <summary>
        /// Determines whenever input BreakRecord for given page number exists.
        /// </summary>
        /// <param name="pageNumber">Page index.</param>
        /// <returns>true, if BreakRecord for given page number exists.</returns>
        internal bool HasPageBreakRecord(int pageNumber)
        {
            Invariant.Assert(pageNumber >= 0, "Page number cannot be negative.");

            // For the first page, the input break record is always NULL.
            // For the rest of pages, it exists if the preceding entry has 
            // non-NULL output BreakRecord.
            if (pageNumber == 0)
                return true;
            if (pageNumber > _breakRecords.Count)
                return false;
            Invariant.Assert(_breakRecords[pageNumber - 1] != null, "Invalid BreakRecordTable entry.");
            return (_breakRecords[pageNumber - 1].BreakRecord != null);
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //-------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Current count reflecting number of known pages.
        /// </summary>
        internal int Count
        {
            get { return _breakRecords.Count; }
        }

        /// <summary>
        /// Whether BreakRecordTable is clean.
        /// </summary>
        internal bool IsClean
        {
            get
            {
                if (_breakRecords.Count == 0)
                    return false;
                Invariant.Assert(_breakRecords[_breakRecords.Count - 1] != null, "Invalid BreakRecordTable entry.");
                return (_breakRecords[_breakRecords.Count - 1].BreakRecord == null);
            }
        }

        #endregion Internal Properties

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Dispose all alive pages for specified range.
        /// </summary>
        /// <param name="start">Index of the first page to dispose.</param>
        /// <param name="count">Number of pages to dispose.</param>
        private void DisposePages(int start, int count)
        {
            WeakReference pageRef;
            int index = start + count - 1;  // Start from the end of BreakRecordTable

            Invariant.Assert(start >= 0 && start < _breakRecords.Count, "Invalid starting index for BreakRecordTable invalidation.");
            Invariant.Assert(start + count <= _breakRecords.Count, "Partial invalidation of BreakRecordTable is not allowed.");

            while (index >= start)
            {
                Invariant.Assert(_breakRecords[index] != null, "Invalid BreakRecordTable entry.");
                pageRef = _breakRecords[index].DocumentPage;
                if (pageRef != null && pageRef.Target != null)
                {
                    ((FlowDocumentPage)pageRef.Target).Dispose();
                }
                _breakRecords[index].DocumentPage = null;
                index--;
            }
        }

        /// <summary>
        /// Destroy BreakRecordsTable entries for specified range.
        /// </summary>
        /// <param name="start">Index of the first entry to destroy.</param>
        /// <param name="count">Nmber of entries to destroy.</param>
        private void InvalidateBreakRecords(int start, int count)
        {
            WeakReference pageRef;
            int index = start + count - 1;  // Start from the end of BreakRecordTable

            Invariant.Assert(start >= 0 && start < _breakRecords.Count, "Invalid starting index for BreakRecordTable invalidation.");
            Invariant.Assert(start + count == _breakRecords.Count, "Partial invalidation of BreakRecordTable is not allowed.");

            while (index >= start)
            {
                Invariant.Assert(_breakRecords[index] != null, "Invalid BreakRecordTable entry.");
                // Dispose Page and BreakRecord before removing the entry.
                pageRef = _breakRecords[index].DocumentPage;
                if (pageRef != null && pageRef.Target != null)
                {
                    ((FlowDocumentPage)pageRef.Target).Dispose();
                }
                if (_breakRecords[index].BreakRecord != null)
                {
                    _breakRecords[index].BreakRecord.Dispose();
                }
                // Remov the entry.
                _breakRecords.RemoveAt(index);
                index--;
            }
        }

        /// <summary>
        /// Retrieves indices of affected pages by specified content range.
        /// </summary>
        /// <param name="start">Content change start position.</param>
        /// <param name="end">Content change end position.</param>
        /// <param name="pageStart">The first affected page.</param>
        /// <param name="pageCount">Number of affected pages.</param>
        private void GetAffectedPages(ITextPointer start, ITextPointer end, out int pageStart, out int pageCount)
        {
            bool affects;
            ReadOnlyCollection<TextSegment> textSegments;
            TextPointer dependentMax;

            // Find the first affected page.
            pageStart = 0;
            while (pageStart < _breakRecords.Count)
            {
                Invariant.Assert(_breakRecords[pageStart] != null, "Invalid BreakRecordTable entry.");

                // If the start position is before last position affecting the output break record,
                // this page is affected.
                dependentMax = _breakRecords[pageStart].DependentMax;
                if (dependentMax != null)
                {
                    if (start.CompareTo(dependentMax) <= 0)
                        break;
                }

                textSegments = _breakRecords[pageStart].TextSegments;
                if (textSegments != null)
                {
                    affects = false;
                    foreach (TextSegment textSegment in textSegments)
                    {
                        if (start.CompareTo(textSegment.End) <= 0)
                        {
                            affects = true;
                            break;
                        }
                    }
                    if (affects)
                        break;
                }
                else
                {
                    // There is no information about this page, so assume that it is 
                    // affected.
                    break;
                }
                ++pageStart;
            }
            // Find the last affected page
            // For now assume that all following pages are affected.
            pageCount = _breakRecords.Count - pageStart;
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// Owner of the BreakRecordTable.
        /// </summary>
        private FlowDocumentPaginator _owner;

        /// <summary>
        /// Array of entries in the BreakRecordTable.
        /// </summary>
        private List<BreakRecordTableEntry> _breakRecords;

        /// <summary>
        /// BreakRecordTableEntry
        /// </summary>
        private class BreakRecordTableEntry
        {
            public PageBreakRecord BreakRecord;
            public ReadOnlyCollection<TextSegment> TextSegments;
            public WeakReference DocumentPage;
            public TextPointer DependentMax;
        }

        #endregion Private Fields
    }
}
