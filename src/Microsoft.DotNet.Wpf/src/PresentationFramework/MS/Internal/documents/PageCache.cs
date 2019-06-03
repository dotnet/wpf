// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: PageCache caches information about individual pages in a document.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Documents;

namespace MS.Internal.Documents
{
    /// <summary>
    /// PageCache acts as both a page-dimension cache and a proxy for an DocumentPaginator document.
    /// It doles out pages to DocumentGrid and keeps the cache in sync with the DocumentPaginator.
    /// </summary>
    /// <speclink>http://d2/DRX/default.aspx</speclink>
    internal class PageCache
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Default constructor for a PageCache object.
        /// Creates our internal cache, represented as a Generic List.
        /// </summary>
        public PageCache()
        {
            //Create the internal representation of our Cache with a default size.
            //This cache is a dynamic List which will expand to accommodate larger documents.
            _cache = new List<PageCacheEntry>(_defaultCacheSize);

            //Create the PageDestroyedWatcher which will keep track of what DocumentPages
            //have been destroyed.
            _pageDestroyedWatcher = new PageDestroyedWatcher();
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// The DocumentPaginator Content tree we're interested in caching information about.
        /// </summary>
        /// <value></value>
        public DynamicDocumentPaginator Content
        {
            set
            {
                //If the content is actually changing we update our paginator here.
                if (_documentPaginator != value)
                {
                    //Reset our flags and default page size.
                    _dynamicPageSizes = false;
                    _defaultPageSize = _initialDefaultPageSize;
                    _isDefaultSizeKnown = false;
                    _isPaginationCompleted = false;

                    //If the old DocumentPaginator is non-null, we need to
                    //remove the old event handlers before we assign the new one.
                    if (_documentPaginator != null)
                    {
                        _documentPaginator.PagesChanged -= new PagesChangedEventHandler(OnPagesChanged);
                        _documentPaginator.GetPageCompleted -= new GetPageCompletedEventHandler(OnGetPageCompleted);
                        _documentPaginator.PaginationProgress -= new PaginationProgressEventHandler(OnPaginationProgress);
                        _documentPaginator.PaginationCompleted -= new EventHandler(OnPaginationCompleted);

                        //Reset the Background Pagination flag to its original state.
                        _documentPaginator.IsBackgroundPaginationEnabled = _originalIsBackgroundPaginationEnabled;
                    }

                    //Now assign the new paginator.
                    _documentPaginator = value;

                    //Clear our cache.
                    ClearCache();

                    //Attach our event handlers and set relevant properties if the new content is non-null.
                    if (_documentPaginator != null)
                    {
                        _documentPaginator.PagesChanged += new PagesChangedEventHandler(OnPagesChanged);
                        _documentPaginator.GetPageCompleted += new GetPageCompletedEventHandler(OnGetPageCompleted);
                        _documentPaginator.PaginationProgress += new PaginationProgressEventHandler(OnPaginationProgress);
                        _documentPaginator.PaginationCompleted += new EventHandler(OnPaginationCompleted);

                        //Set the paginator's PageSize so the new content will reflow to fit in the requested space.                                                
                        _documentPaginator.PageSize = _defaultPageSize;

                        //We save off the original value so we can restore it when new content is assigned.
                        _originalIsBackgroundPaginationEnabled = _documentPaginator.IsBackgroundPaginationEnabled;

                        //Enable background pagination, and set the paginator's PageSize so the new content will
                        //reflow to fit in the requested space.                                                
                        _documentPaginator.IsBackgroundPaginationEnabled = true;

                        //Determine content flow direction, if the content has one specified.
                        //We look for the FrameworkElement.FlowDirection property.
                        //If it doesn't have one then we assume the content is Left-To-Right.
                        //(Note: FlowDirection is a value type and thus cannot be null;
                        //DependencyObject.GetValue will never return null for this even if the 
                        //DocumentPaginator does not have this property set -- it will just 
                        //return the default value for FlowDirectionProperty.)
                        if (_documentPaginator.Source is DependencyObject)
                        {
                            FlowDirection flowDirection = (FlowDirection)((DependencyObject)_documentPaginator.Source).GetValue(FrameworkElement.FlowDirectionProperty);
                            if (flowDirection == FlowDirection.LeftToRight)
                            {
                                _isContentRightToLeft = false;
                            }
                            else
                            {
                                _isContentRightToLeft = true;
                            }
                        }
                    }

                    //If the content is already paginated (as is the case for certain Fixed content)
                    //we'll call OnPaginationProgress here to let the Cache (and any listeners) know that 
                    //some or all the pages are available.
                    if (_documentPaginator != null)
                    {
                        if (_documentPaginator.PageCount > 0)
                        {
                            OnPaginationProgress(_documentPaginator,
                                                 new PaginationProgressEventArgs(0, _documentPaginator.PageCount));
                        }

                        if (_documentPaginator.IsPageCountValid)
                        {
                            OnPaginationCompleted(_documentPaginator, EventArgs.Empty);
                        }
                    }
                }
            }
            get
            {
                //Just return our current paginator.
                return _documentPaginator;
            }
        }

        /// <summary>
        /// The number of pages in the cache.
        /// </summary>
        /// <value></value>
        public int PageCount
        {
            get
            {
                return _cache.Count;
            }
        }

        /// <summary>
        /// Based on current knowledge, reports whether the document consists
        /// entirely of pages with the same dimensions or whether page sizes vary
        /// (i.e. are 'Dynamic')
        /// </summary>
        /// <value></value>
        public bool DynamicPageSizes
        {
            get
            {
                return _dynamicPageSizes;
            }
        }

        /// <summary>
        /// Indicates whether the content has an RTL flowdirection or not.
        /// </summary>
        /// <value></value>
        public bool IsContentRightToLeft
        {
            get
            {
                return _isContentRightToLeft;
            }
        }

        public bool IsPaginationCompleted
        {
            get
            {
                return _isPaginationCompleted;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        #region Public Events

        /// <summary>
        /// Fired when one ore more pages in the document have been paginated.
        /// </summary>
        public event PaginationProgressEventHandler PaginationProgress;

        /// <summary>
        /// Fired when the document is finished paginating.
        /// </summary>
        public event EventHandler PaginationCompleted;

        /// <summary>
        /// Fired when one or more pages in the document have changed.
        /// </summary>
        public event PagesChangedEventHandler PagesChanged;

        /// <summary>
        /// Fired when a requested page has been retrieved.
        /// </summary>
        public event GetPageCompletedEventHandler GetPageCompleted;

        /// <summary>
        /// Fired when one or more entries in the PageCache have been updated for any reason
        /// (Due to pagination, a new default page size, page retrieval, etc...)
        /// </summary>
        public event PageCacheChangedEventHandler PageCacheChanged;

        #endregion Public Events

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------   

        #region Public Methods       

        /// <summary>
        /// Retrieves the cached size of a page in the document, even if the page is
        /// marked as "dirty."  Pages are only un-dirtied when GetPage() is called 
        /// (that is, as pages are actually retrieved from the IDocumentFormatter).
        /// </summary>
        /// <param name="pageNumber">The pagenumber to return the dimensions of.</param>
        /// <returns>The dimensions of the requested page, or (0,0) for nonexistent pages.</returns>
        public Size GetPageSize(int pageNumber)
        {
            if (pageNumber >= 0 && pageNumber < _cache.Count)
            {
                Size pageSize = _cache[pageNumber].PageSize;
                Invariant.Assert(pageSize != Size.Empty, "PageCache entry's PageSize is Empty.");
                return pageSize;
            }
            else
            {
                return new Size(0, 0);
            }
        }

        // /// <summary>
        // /// --- Commenting out this method as it is currently unused. ---
        // /// Retrieves a page from the DocumentPaginator Asynchronously.
        // /// Caller will receive a GetPageCompleted event when the page is actually available,
        // /// and the Cache will be updated to reflect the true page dimensions at that time.
        // /// </summary>
        // /// <param name="pageNumber">The page to retrieve from the DocumentPaginator</param>
        // /// <returns>Nothing.</returns>
        // public void GetPage(int pageNumber)
        // {
        //     if (_documentPaginator != null)
        //     {
        //         _documentPaginator.GetPageAsync(pageNumber, (object)pageNumber);
        //     }
        // }

        /// <summary>
        /// Retrieves the "Dirty" bit for the associated page.
        /// </summary>
        /// <param name="pageNumber">The page to retrive the Dirty bit for</param>
        /// <returns>The dirty bit</returns>
        public bool IsPageDirty(int pageNumber)
        {
            if (pageNumber >= 0 && pageNumber < _cache.Count)
            {
                return _cache[pageNumber].Dirty;
            }
            else
            {
                return true;
            }
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods        

        /// <summary>
        /// Handler for the OnPaginationProgress event fired by the DocumentPaginator.         
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnPaginationProgress(object sender, PaginationProgressEventArgs args)
        {
            //Since handling the PaginationProgress event entails a bit of work, we'll
            //have our dispatcher call the PaginationProgress event at Normal priority.
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal,
                   new DispatcherOperationCallback(PaginationProgressDelegate), args);
        }

        /// <summary>
        /// Asynchronously handles the PaginationProgress event.
        /// This means that one or more pages have been added to the document, so we
        /// add any new pages to the cache, mark them as dirty, and fire off our PaginationProgress
        /// event.
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private object PaginationProgressDelegate(object parameter)
        {
            PaginationProgressEventArgs args = parameter as PaginationProgressEventArgs;

            if (args == null)
            {
                throw new InvalidOperationException("parameter");
            }

            //Validate incoming parameters.
            ValidatePaginationArgs(args.Start, args.Count);

            if (_isPaginationCompleted)
            {
                if (args.Start == 0)
                {
                    //Since we've started repaginating from the beginning of the document
                    //after pagination was completed, we can't assume we know
                    //the default page size anymore.
                    _isDefaultSizeKnown = false;
                    _dynamicPageSizes = false;
                }

                //Reset our IsPaginationCompleted flag since we just got a pagination event.
                _isPaginationCompleted = false;
            }

            //Check for integer overflow.
            if (args.Start + args.Count < 0)
            {
                throw new ArgumentOutOfRangeException("args");
            }

            //Create our list of changes.  We allocate space for 2 changes here
            //as we can have as many as two changes resulting from a Pagination event.
            List<PageCacheChange> changes = new List<PageCacheChange>(2);
            PageCacheChange change;

            //If we have pages to add or modify, do so now.
            if (args.Count > 0)
            {
                //If pagination has added new pages onto the end of the document, we
                //add new entries to our cache.
                if (args.Start >= _cache.Count)
                {
                    //Completely new pages, so we add new cache entries
                    change = AddRange(args.Start, args.Count);
                    if (change != null)
                    {
                        changes.Add(change);
                    }
                }
                else
                {
                    //Pagination has updated some currently existing pages, so we'll
                    //update our entries.
                    if (args.Start + args.Count < _cache.Count)
                    {
                        //All pre-existing pages, so we just dirty the current cache entries.
                        change = DirtyRange(args.Start, args.Count);
                        if (change != null)
                        {
                            changes.Add(change);
                        }
                    }
                    else
                    {
                        //Some pre-existing pages, some new.
                        change = DirtyRange(args.Start, _cache.Count - args.Start);
                        if (change != null)
                        {
                            changes.Add(change);
                        }

                        change = AddRange(_cache.Count, args.Count - (_cache.Count - args.Start) + 1);
                        if (change != null)
                        {
                            changes.Add(change);
                        }
                    }
                }
            }

            //If the document's PageCount is now less than the size of our cache due to repagination
            //we remove the extra entries.
            int pageCount = _documentPaginator != null ? _documentPaginator.PageCount : 0;

            if (pageCount < _cache.Count)
            {
                change = new PageCacheChange(pageCount, _cache.Count - pageCount, PageCacheChangeType.Remove);
                changes.Add(change);

                //Remove the pages from the cache.
                _cache.RemoveRange(pageCount, _cache.Count - pageCount);
            }

            //Fire off our PageCacheChanged event.
            FirePageCacheChangedEvent(changes);

            //Fire the PaginationProgress event.
            if (PaginationProgress != null)
            {
                PaginationProgress(this, args);
            }

            return null;
        }

        /// <summary>
        /// Handler for the OnPaginationCompleted event.  Merely fires off our own event.
        /// </summary>
        /// <param name="sender">The sender of this event</param>
        /// <param name="args">The arguments associated with this event</param>
        private void OnPaginationCompleted(object sender, EventArgs args)
        {
            //Since handling the PaginationCompleted event entails a bit of work, we'll
            //have our dispatcher call the PaginationCompleted event at Normal priority.
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal,
                   new DispatcherOperationCallback(PaginationCompletedDelegate), args);
        }

        /// <summary>
        /// Asynchronously handles the PaginationCompleted event.
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private object PaginationCompletedDelegate(object parameter)
        {
            EventArgs args = parameter as EventArgs;

            if (args == null)
            {
                throw new ArgumentOutOfRangeException("parameter");
            }

            //set our IsPaginationCompleted flag since we're done paginating.
            _isPaginationCompleted = true;

            //Fire the PaginationProgress event.
            if (PaginationCompleted != null)
            {
                PaginationCompleted(this, args);
            }

            return null;
        }

        /// <summary>
        /// Handler for the OnPagesChanged event fired by the DocumentPaginator.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnPagesChanged(object sender, PagesChangedEventArgs args)
        {
            //Since handling the PagesChanged event entails a bit of work, we'll
            //have our dispatcher call the PagesChangedDelegate at Normal priority.
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal,
                   new DispatcherOperationCallback(PagesChangedDelegate), args);
        }

        /// <summary>
        /// Asynchronously handles the PagesChanged event.
        /// This means that one or more pages have been invalidated so we
        /// dirty their cache entries and fire off our PagesChanged event.
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private object PagesChangedDelegate(object parameter)
        {
            PagesChangedEventArgs args = parameter as PagesChangedEventArgs;

            if (args == null)
            {
                throw new ArgumentOutOfRangeException("parameter");
            }

            //Validate incoming parameters
            ValidatePaginationArgs(args.Start, args.Count);

            //Start values outside the range of current pages are invalid.
            //if (args.Start >= _cache.Count)
            //{
            //    throw new ArgumentOutOfRangeException("args");
            //}

            //If the last page specified in the change is out of the range of currently-known
            //pages we make the assumption that the IDP means to invalidate all pages and so
            //we clip the count into range.
            //We also take into account integer overflow... if the sum of Start+Count is less than
            //zero then we've overflowed.
            int adjustedCount = args.Count;
            if (args.Start + args.Count >= _cache.Count ||
                args.Start + args.Count < 0)
            {
                adjustedCount = _cache.Count - args.Start;
            }

            //Create our list of changes.  We can have at most one.
            List<PageCacheChange> changes = new List<PageCacheChange>(1);

            //Now make the change if there is one to make.
            if (adjustedCount > 0)
            {
                PageCacheChange change = DirtyRange(args.Start, adjustedCount);
                if (change != null)
                {
                    changes.Add(change);
                }

                //Fire off our PageCacheChanged event.
                FirePageCacheChangedEvent(changes);
            }

            //Fire the PagesChanged event.
            if (PagesChanged != null)
            {
                PagesChanged(this, args);
            }

            return null;
        }

        /// <summary>
        /// Handler for the GetPageCompleted event fired by the DocumentPaginator.        
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnGetPageCompleted(object sender, GetPageCompletedEventArgs args)
        {
            if (!args.Cancelled && args.Error == null && args.DocumentPage != DocumentPage.Missing)
            {
                //Add the page to the Watcher so we can determine if the page has been
                //destroyed in our Delegate.
                _pageDestroyedWatcher.AddPage(args.DocumentPage);

                //Since handling the GetPageCompleted event entails a bit of work, we'll
                //have our dispatcher call the GetPageCompletedDelegate at Normal priority.
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal,
                       new DispatcherOperationCallback(GetPageCompletedDelegate), args);
            }
        }

        /// <summary>
        /// Asynchronously handles the GetPageCompleted event.
        /// This means that a requested page is available, so we
        /// update the page's cache entry, mark it as clean and fire off our
        /// GetPageCompleted event.
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        private object GetPageCompletedDelegate(object parameter)
        {
            GetPageCompletedEventArgs args = parameter as GetPageCompletedEventArgs;

            if (args == null)
            {
                throw new ArgumentOutOfRangeException("parameter");
            }

            //Check to see if the page has been destroyed, and remove it from the Watcher.
            bool pageDestroyed = _pageDestroyedWatcher.IsDestroyed(args.DocumentPage);
            _pageDestroyedWatcher.RemovePage(args.DocumentPage);

            //The page was destroyed, return early.
            if (pageDestroyed)
            {
                return null;
            }

            //We only update the entry if the GetPageAsync call was not canceled,
            //points to a valid page (i.e. is not DocumentPage.Missing)
            //and did not result in an Error condition.
            if (!args.Cancelled && args.Error == null && args.DocumentPage != DocumentPage.Missing)
            {
                if (args.DocumentPage.Size == Size.Empty)
                {
                    throw new ArgumentOutOfRangeException("args");
                }

                //Update the cache.
                PageCacheEntry newEntry;
                newEntry.PageSize = args.DocumentPage.Size;
                newEntry.Dirty = false;

                //Create our list of changes.  We can have at most two.
                List<PageCacheChange> changes = new List<PageCacheChange>(2);
                PageCacheChange change;

                //If we add pages such that there's going to be a gap
                //in the cache (for example if we get PaginationProgress events
                //for pages 1-3 and then 7-10), we need to fill in the gap with entries.
                if (args.PageNumber > _cache.Count - 1)
                {
                    //Add the new page (this will cause any pages we
                    //skipped over to be filled in)
                    change = AddRange(args.PageNumber, 1);
                    if (change != null)
                    {
                        changes.Add(change);
                    }

                    //Update the just-retrieved-page to reflect the actual page size.
                    change = UpdateEntry(args.PageNumber, newEntry);
                    if (change != null)
                    {
                        changes.Add(change);
                    }
                }
                else
                {
                    //Just update the retrieved page's cache entry.
                    change = UpdateEntry(args.PageNumber, newEntry);
                    if (change != null)
                    {
                        changes.Add(change);
                    }
                }

                //If this page is a different size than the last-retrieved page, then we have a
                //dynamic document.
                if (_isDefaultSizeKnown && newEntry.PageSize != _lastPageSize)
                {
                    _dynamicPageSizes = true;
                }

                _lastPageSize = newEntry.PageSize;

                //If this is the first page in the document that we've actually retrieved from
                //the DocumentPaginator, we'll use this page's size as the default size and 
                //update any dirty pages in the cache with this default size.
                //(Technically there should be no non-dirty pages in the document at this point, but
                //if we want to change our heuristic (for example, use the "most common page size")
                //to define the default then we don't want to clobber known pages.)
                if (!_isDefaultSizeKnown)
                {
                    _defaultPageSize = newEntry.PageSize;
                    _isDefaultSizeKnown = true;

                    SetDefaultPageSize(true);
                }

                //Fire off our PageCacheChanged event.
                FirePageCacheChangedEvent(changes);
            }

            //Fire the GetPageCompleted event.
            if (GetPageCompleted != null)
            {
                GetPageCompleted(this, args);
            }

            return null;
        }

        /// <summary>
        /// Checks that the start and count parameters passed by a
        /// Pagination event are valid.  
        /// </summary>
        /// <param name="start"></param>
        /// <param name="count"></param>
        private void ValidatePaginationArgs(int start, int count)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException("start");
            }

            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }
        }

        /// <summary>
        /// Updates entries in the cache to have the current default page size.
        /// </summary>
        private void SetDefaultPageSize(bool dirtyOnly)
        {
            //Create our list of changes.  We can potentially have as many
            //changes as there are pages in the document.
            List<PageCacheChange> changes = new List<PageCacheChange>(PageCount);

            Invariant.Assert(_defaultPageSize != Size.Empty, "Default Page Size is Empty.");

            for (int i = 0; i < _cache.Count; i++)
            {
                if (_cache[i].Dirty || !dirtyOnly)
                {
                    PageCacheEntry newEntry;
                    newEntry.PageSize = _defaultPageSize;
                    newEntry.Dirty = true;

                    PageCacheChange change = UpdateEntry(i, newEntry);
                    if (change != null)
                    {
                        changes.Add(change);
                    }
                }
            }

            //Fire off our PageCacheChanged event.
            FirePageCacheChangedEvent(changes);
        }

        /// <summary>
        /// Fires the PageCacheChanged event with the specified changelist.
        /// </summary>
        /// <param name="changes">The changes to pass along with the event.</param>
        private void FirePageCacheChangedEvent(List<PageCacheChange> changes)
        {
            Debug.Assert(changes != null, "Attempt to fire PageCacheChangedEvent with null change set.");

            //Fire off our PageCacheChangedEvent if we have any changes
            if (PageCacheChanged != null && changes != null && changes.Count > 0)
            {
                PageCacheChangedEventArgs args = new PageCacheChangedEventArgs(changes);
                PageCacheChanged(this, args);
            }
        }

        /// <summary>
        /// Adds a range of dirty cache entries to the cache.
        /// </summary>
        /// <param name="start">The starting index for the entries.</param>
        /// <param name="count">The number of entries to add.</param>
        private PageCacheChange AddRange(int start, int count)
        {
            //Make sure we're in range.
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException("start");
            }

            if (count < 1)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            Invariant.Assert(_defaultPageSize != Size.Empty, "Default Page Size is Empty.");

            //If we add pages such that there's going to be a gap
            //in the cache (for example if we get PaginationProgress events
            //for pages 1-3 and then 7-10), we need to fill in the gap with entries.
            if (start >= _cache.Count)
            {
                count += (start - _cache.Count);
                start = _cache.Count;
            }

            //Add the new entries.  
            //Each entry is marked as dirty and is assumed to have the default page size.
            for (int i = start; i < start + count; i++)
            {
                PageCacheEntry newEntry;
                newEntry.PageSize = _defaultPageSize;
                newEntry.Dirty = true;
                _cache.Add(newEntry);
            }

            return new PageCacheChange(start, count, PageCacheChangeType.Add);
        }

        /// <summary>
        /// Updates the cache entry at the specified index with a new entry.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="newEntry"></param>
        private PageCacheChange UpdateEntry(int index, PageCacheEntry newEntry)
        {
            //Make sure we're in range.
            if (index >= _cache.Count || index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            Invariant.Assert(newEntry.PageSize != Size.Empty, "Updated entry newEntry has Empty PageSize.");

            //Check to see if the entry has changed.
            if (newEntry.PageSize != _cache[index].PageSize ||
                newEntry.Dirty != _cache[index].Dirty)
            {
                //Update the cache entry
                _cache[index] = newEntry;

                return new PageCacheChange(index, 1, PageCacheChangeType.Update);
            }

            return null;
        }

        /// <summary>
        /// Dirties the cache entries for a range of pages that have been
        /// modified.  Adds new entries where necessary.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="count"></param>
        private PageCacheChange DirtyRange(int start, int count)
        {
            //Make sure we're in range.
            if (start >= _cache.Count)
            {
                throw new ArgumentOutOfRangeException("start");
            }

            if (start + count > _cache.Count || count < 1)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            Invariant.Assert(_defaultPageSize != Size.Empty, "Default Page Size is Empty.");

            for (int i = start; i < start + count; i++)
            {
                //Dirty the pages in the range of invalidated pages.
                //This entails setting the dirty bit and
                //setting the page size to the default.
                //We'll add new entries if necessary.
                PageCacheEntry newEntry;
                newEntry.Dirty = true;
                newEntry.PageSize = _defaultPageSize;
                _cache[i] = newEntry;
            }

            return new PageCacheChange(start, count, PageCacheChangeType.Update);
        }

        /// <summary>
        /// Clears out the current cache and lets listeners know of the change.
        /// </summary>
        private void ClearCache()
        {
            if (_cache.Count > 0)
            {
                //Build our list of changes
                List<PageCacheChange> changes = new List<PageCacheChange>(1);
                //This change indicates that all of the pages were removed from the cache.
                PageCacheChange change = new PageCacheChange(0, _cache.Count, PageCacheChangeType.Remove);
                changes.Add(change);

                //Clear the cache
                _cache.Clear();

                //Fire the event.
                FirePageCacheChangedEvent(changes);
            }
        }



        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        //The List<T> that contains our cache.
        private List<PageCacheEntry> _cache;

        //The PageDestroyedWatcher that keeps track of the Destroyed-state of DocumentPages.
        private PageDestroyedWatcher _pageDestroyedWatcher;

        //Our document
        private DynamicDocumentPaginator _documentPaginator;

        //The original state of IDP.IsBackgroundPaginationEnabled
        private bool _originalIsBackgroundPaginationEnabled;

        //Whether the page sizes are uniform or not
        private bool _dynamicPageSizes;

        //Whether our content has a FlowDirection of RTL or not.
        private bool _isContentRightToLeft;

        //Whether pagination has finished or is still in progress.
        private bool _isPaginationCompleted;

        //Flags related to the "default" size
        private bool _isDefaultSizeKnown;
        private Size _defaultPageSize;

        //The last page size retrieved from the DocumentPaginator.
        private Size _lastPageSize;

        //The _default_ default page size.  We don't want the default to be (0,0) because:
        //a) (0,0) is nearly never a valid page size.
        //b) it causes page layout to end up with all pages initially visible which causes a huge perf hit.
        //Our initial default page size is 8.5"x11", which is 816x1056 pixels (at 1/96")
        private readonly Size _initialDefaultPageSize = new Size(816, 1056);

        //The default size of our List<T> cache.
        private readonly int _defaultCacheSize = 64;

        #endregion Private Fields
    }


    #region PageDestroyedWatcher

    /// <summary>
    /// PageDestroyedWatcher is used to keep track of whether one or more
    /// DocumentPages have been Destroyed.  This is necessary because
    /// DocumentPage does not expose a property that indicates whether it has
    /// been destroyed, only a PageDestroyed event.  PageDestroyedWatcher
    /// listens for this event and keeps track of which DocumentPages have been
    /// destroyed.
    /// </summary>
    internal class PageDestroyedWatcher
    {
        /// <summary>
        /// Instantiates a new PageDestroyedWatcher
        /// </summary>
        public PageDestroyedWatcher()
        {
            _table = new Hashtable(16);
        }

        /// <summary>
        /// Adds a new DocumentPage to the Watcher
        /// </summary>
        /// <param name="page">The page to add</param>
        public void AddPage(DocumentPage page)
        {
            if (!_table.Contains(page))
            {
                _table.Add(page, false);
                page.PageDestroyed += new EventHandler(OnPageDestroyed);
            }
            else
            {
                _table[page] = false;
            }
        }

        /// <summary>
        /// Removes an existing page from the Watcher
        /// </summary>
        /// <param name="page"></param>
        public void RemovePage(DocumentPage page)
        {
            if (_table.Contains(page))
            {
                _table.Remove(page);
                page.PageDestroyed -= new EventHandler(OnPageDestroyed);
            }
        }

        /// <summary>
        /// Indicates whether the specified DocumentPage has been destroyed.
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public bool IsDestroyed(DocumentPage page)
        {
            if (_table.Contains(page))
            {
                return (bool)_table[page];
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Handles the OnPageDestroyed event and updates the Destroyed state of the pages
        /// associated with this Watcher.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPageDestroyed(object sender, EventArgs e)
        {
            DocumentPage page = sender as DocumentPage;
            Invariant.Assert(page != null, "Invalid type in PageDestroyedWatcher");

            if (_table.Contains(page))
            {
                _table[page] = true;
            }
        }

        //Hashtable to associate Destroyed states with DocumentPages.
        private Hashtable _table;
    }
    #endregion PageDestroyedWatcher


    #region PageCacheEntry
    /// <summary>
    /// An entry in our PageCache
    /// </summary>
    internal struct PageCacheEntry
    {
        /// <summary>
        /// The size of the given page
        /// </summary>
        public Size PageSize;

        /// <summary>
        /// Whether the above PageSize is up to date.
        /// </summary>
        public bool Dirty;
    }

    #endregion PageCacheEntry


    #region PageCacheChangedEvent

    /// <summary>
    /// PageCacheChanged event handler.
    /// </summary>
    internal delegate void PageCacheChangedEventHandler(object sender, PageCacheChangedEventArgs e);

    /// <summary>
    /// Event arguments for the PageCacheChanged event.
    /// </summary>
    internal class PageCacheChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="changes">The changes corresponding to this event</param>
        public PageCacheChangedEventArgs(List<PageCacheChange> changes)
        {
            _changes = changes;
        }

        /// <summary>
        /// The list of changes associated with this event.
        /// </summary>
        public List<PageCacheChange> Changes
        {
            get
            {
                return _changes;
            }
        }

        private readonly List<PageCacheChange> _changes;
    }


    /// <summary>
    /// Represents a single change to the PageCache
    /// </summary>
    internal class PageCacheChange
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="start">The first page changed.</param>
        /// <param name="count">The number of pages changed.</param>
        /// <param name="type">The type of changed incurred.</param>
        public PageCacheChange(int start, int count, PageCacheChangeType type)
        {
            _start = start;
            _count = count;
            _type = type;
        }

        /// <summary>
        /// Zero-based page number for this first page that has changed.
        /// </summary>
        public int Start
        {
            get
            {
                return _start;
            }
        }

        /// <summary>
        /// Number of continuous pages changed.
        /// </summary>
        public int Count
        {
            get
            {
                return _count;
            }
        }

        /// <summary>
        /// The type of change that occurred.
        /// </summary>
        public PageCacheChangeType Type
        {
            get
            {
                return _type;
            }
        }

        private readonly int _start;
        private readonly int _count;
        private readonly PageCacheChangeType _type;
    }

    internal enum PageCacheChangeType
    {
        /// <summary>
        /// Pages were added to the cache.
        /// </summary>
        Add = 0,

        /// <summary>
        /// Pages were removed from the cache.
        /// </summary>
        Remove,

        /// <summary>
        /// Pages in the cache were updated.
        /// </summary>
        Update
    }

    #endregion PageCacheChangedEvent
}
