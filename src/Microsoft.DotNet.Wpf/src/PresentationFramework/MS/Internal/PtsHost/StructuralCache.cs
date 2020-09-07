// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
//
// Description: PTS structural cache related data. 
//


using MS.Internal.Text;
using MS.Internal.PtsHost.UnsafeNativeMethods;
using MS.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Documents;

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// PTS structural cache related data
    /// </summary>
    internal sealed class StructuralCache
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Structural Cache contructor.
        /// </summary>
        /// <param name="owner">Owner of the conent</param>
        /// <param name="textContainer">TextContainer representing content</param>
        internal StructuralCache(FlowDocument owner, TextContainer textContainer)
        {
            Invariant.Assert(owner != null);
            Invariant.Assert(textContainer != null);
            Invariant.Assert(textContainer.Parent != null);

            _owner = owner;
            _textContainer = textContainer;
            _backgroundFormatInfo = new BackgroundFormatInfo(this);
        }
        
        /// <summary>
        /// Finalizer
        /// </summary>
        ~StructuralCache()
        {
            // Notify the PtsCache about the fact that PtsContext needs to be destroyed.
            // NOTE: It is safe to access PtsContext, because the PtsContext
            //       does not have a finalizer.
            if (_ptsContext != null)
            {
                PtsCache.ReleaseContext(_ptsContext);
            }
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods 

        /// <summary>
        /// Sets current page context to be used in the cache's queries
        /// </summary>
        /// <param name="currentPage">Document page to become current in the context</param>
        /// <returns>Reference to object compatible with IDisposable to re-initialize page context</returns>
        internal IDisposable SetDocumentFormatContext(FlowDocumentPage currentPage)
        {
            if (!CheckFlags(Flags.FormattedOnce))
            {
                SetFlags(true, Flags.FormattedOnce);
                _owner.InitializeForFirstFormatting();
            }
            return (new DocumentFormatContext(this, currentPage) as IDisposable);
        }

        /// <summary>
        /// Sets current page context to be used in the cache's queries
        /// </summary>
        /// <param name="currentPage">Document page to become current in the context</param>
        /// <returns>Reference to object compatible with IDisposable to re-initialize page context</returns>
        internal IDisposable SetDocumentArrangeContext(FlowDocumentPage currentPage)
        {
            return (new DocumentArrangeContext(this, currentPage) as IDisposable);
        }

        /// <summary>
        /// Sets current page context to be used in the cache's queries
        /// </summary>
        /// <param name="currentPage">Document page to become current in the context</param>
        /// <returns>Reference to object compatible with IDisposable to re-initialize page context</returns>
        internal IDisposable SetDocumentVisualValidationContext(FlowDocumentPage currentPage)
        {
            return (new DocumentVisualValidationContext(this, currentPage) as IDisposable);
        }

        /// <summary>
        /// Detects if illegal tree change operation has been performed, but hidden by external
        /// code through try-catch statement.
        /// </summary>
        internal void DetectInvalidOperation()
        {
            if (_illegalTreeChangeDetected)
            {
                throw new InvalidOperationException(SR.Get(SRID.IllegalTreeChangeDetectedPostAction));
            }
        }

        /// <summary>
        /// Notes the fact that world has changed while in measure / arrange.
        /// </summary>
        internal void OnInvalidOperationDetected()
        {
            if (_currentPage != null)
            {
                _illegalTreeChangeDetected = true;
            }
        }

        /// <summary>
        /// Invalidate format caches accumulated in the NameTable.
        /// </summary>
        internal void InvalidateFormatCache(bool destroyStructure)
        {
            if (_section != null)
            {
                _section.InvalidateFormatCache();
                _destroyStructure = _destroyStructure || destroyStructure;
                // Formatting caches are acquired during page formatting (full or incremental).
                // But since there is no DTR available, need to force reformatting 
                // to reacquire formatting caches.
                _forceReformat = true;
            }
        }

        /// <summary>
        /// Add new DirtyTextRange.
        /// </summary>
        /// <param name="dtr">New DTR being added.</param>
        internal void AddDirtyTextRange(DirtyTextRange dtr)
        {
            if (_dtrs == null) 
            { 
                _dtrs = new DtrList(); 
            }
            _dtrs.Merge(dtr);
        }

        /// <summary>
        /// Retrieve list of dtrs from range.
        /// DTR StartIndex for each dtr returned is scaled to be relative to dcpNew, no other translation required.
        /// </summary>
        /// <param name="dcpNew">Distance from the beginning of textContainer after all tree changes</param>
        /// <param name="cchOld">Number of characters in the range, but before any  tree changes</param>
        /// <returns>List of DRTs for specified range.</returns>
        internal DtrList DtrsFromRange(int dcpNew, int cchOld)
        {
            return (_dtrs != null) ? _dtrs.DtrsFromRange(dcpNew, cchOld) : null;
        }

        /// <summary>
        /// Clear update info.
        /// </summary>
        /// <param name="destroyStructureCache">Destroy structure cache.</param>
        internal void ClearUpdateInfo(bool destroyStructureCache)
        {
            _dtrs = null;
            _forceReformat = false;
            _destroyStructure = false;

           /*
            * Have to make sure the ptsContext is not disposed 
            * as the resulting call to ReleaseHandle will throw an exception.
            *
            * This is possible when the dispatcher.Shutdown event fires before
            * the Visibility changed event fires.
            **/
            if (_section != null && !_ptsContext.Disposed)
            {
                if (destroyStructureCache)
                {
                    _section.DestroyStructure();
                }
                _section.ClearUpdateInfo();
            }
        }

        /// <summary>
        /// This method is called after user input.
        /// Temporarily drop our background layout time slice to cut down on
        /// latency.
        /// </summary>
        internal void ThrottleBackgroundFormatting()
        {
            _backgroundFormatInfo.ThrottleBackgroundFormatting();
        }

        /// <summary>
        /// Whether PtsContext has been set
        /// </summary>
        internal bool HasPtsContext()
        {
            return _ptsContext != null;
        }

        #endregion Internal Methods 

        // ------------------------------------------------------------------
        //
        //  Internal Properties
        //
        // ------------------------------------------------------------------

        #region Internal Properties 

        /// <summary>
        /// The DependencyObject supplying property values for this cache.
        /// </summary>
        /// <remarks>
        /// Typically PropertyOwner == FormattingOwner.  However, when content
        /// is hosted by TextBox or RichTextBox, we want to read property values
        /// from the control, not FlowDocument.
        /// </remarks>
        internal DependencyObject PropertyOwner
        {
            get
            {
                return _textContainer.Parent;
            }
        }

        /// <summary>
        /// The DependencyObject whose structure is represented by this cache.
        /// </summary>
        internal FlowDocument FormattingOwner 
        { 
            get 
            {
                return _owner; 
            } 
        }

        /// <summary>
        /// PTS section object.
        /// </summary>
        internal Section Section
        {
            get 
            { 
                EnsurePtsContext();
                return _section;
            }
        }

        /// <summary>
        /// Hyphenator
        /// </summary>
        internal NaturalLanguageHyphenator Hyphenator
        { 
            get 
            { 
                EnsureHyphenator();
                return _hyphenator; 
            } 
        }

        /// <summary>
        /// Context used to communicate with PTS component.
        /// </summary>
        internal PtsContext PtsContext 
        { 
            get 
            { 
                EnsurePtsContext();
                return _ptsContext;
            } 
        }

        /// <summary>
        /// Context used to communicate with PTS component.
        /// </summary>
        internal DocumentFormatContext CurrentFormatContext { get { return _documentFormatContext; } }

        /// <summary>
        /// Context used to communicate with PTS component.
        /// </summary>
        internal DocumentArrangeContext CurrentArrangeContext { get { return _documentArrangeContext; } }

        /// <summary>
        /// TextFormatter host.
        /// </summary>
        internal TextFormatterHost TextFormatterHost 
        { 
            get 
            { 
                EnsurePtsContext();
                return _textFormatterHost;
            } 
        }

        /// <summary>
        /// TextContainer exposing access to the content.
        /// </summary>
        internal TextContainer TextContainer
        {
            get
            {
                return _textContainer;
            }
        }

        /// <summary>
        /// TextContainer exposing access to the content.
        /// </summary>
        internal FlowDirection PageFlowDirection
        {
            get
            {
                return _pageFlowDirection;
            }
            set
            {
                _pageFlowDirection = value;
            }
        }

        /// <summary>
        /// Force content reformatting?
        /// </summary>
        internal bool ForceReformat
        {
            get 
            { 
                return _forceReformat; 
            }
            set 
            { 
                _forceReformat = value; 
            }
        }

        /// <summary>
        /// destroy name table on reformatting?
        /// </summary>
        internal bool DestroyStructure
        {
            get 
            {
                return _destroyStructure; 
            }
        }

        /// <summary>
        /// DTRs list.
        /// </summary>
        internal DtrList DtrList 
        { 
            get 
            { 
                return _dtrs; 
            } 
        }

        /// <summary>
        /// Whether deferred visual creation is supported for the given context
        /// </summary>
        internal bool IsDeferredVisualCreationSupported
        {
            get { return _currentPage != null && !_currentPage.FinitePage; }
        }

        /// <summary>
        /// Background formatting information
        /// </summary>
        internal BackgroundFormatInfo BackgroundFormatInfo 
        { 
            get
            {
                return _backgroundFormatInfo;
            } 
        }

        /// <summary>
        /// Is Optimal paragraph enabled for this session
        /// </summary>
        internal bool IsOptimalParagraphEnabled
        {
            get
            {
                if (PtsContext.IsOptimalParagraphEnabled)
                {
                    return (bool)this.PropertyOwner.GetValue(FlowDocument.IsOptimalParagraphEnabledProperty);
                }
                return false;
            }
        }

        /// <summary>
        /// Whether formatting is currently in progress.
        /// </summary>
        internal bool IsFormattingInProgress
        {
            get
            {
                return CheckFlags(Flags.FormattingInProgress);
            }
            set
            {
                SetFlags(value, Flags.FormattingInProgress);
            }
        }

        /// <summary>
        /// Whether content change is currently in progress.
        /// </summary>
        internal bool IsContentChangeInProgress
        {
            get
            {
                return CheckFlags(Flags.ContentChangeInProgress);
            }
            set
            {
                SetFlags(value, Flags.ContentChangeInProgress);
            }
        }

        /// <summary>
        /// Whether the first formatting was done.
        /// </summary>
        internal bool IsFormattedOnce
        {
            get
            {
                return CheckFlags(Flags.FormattedOnce);
            }
            set
            {
                SetFlags(value, Flags.FormattedOnce);
            }
        }

        #endregion Internal Properties 

        // ------------------------------------------------------------------
        //
        //  Private Methods
        //
        // ------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Ensures the hyphenator exists.
        /// </summary>
        private void EnsureHyphenator()
        {
            if (_hyphenator == null)
            {
                _hyphenator = new NaturalLanguageHyphenator();
            }
        }

        /// <summary>
        /// Ensures the PtsContext exists.
        /// </summary>
        private void EnsurePtsContext()
        {
            if (_ptsContext == null)
            {
                TextFormattingMode textFormattingMode = TextOptions.GetTextFormattingMode(this.PropertyOwner);
                _ptsContext = new PtsContext(true, textFormattingMode);
                _textFormatterHost = new TextFormatterHost(_ptsContext.TextFormatter, textFormattingMode, _owner.PixelsPerDip);

                _section = new MS.Internal.PtsHost.Section(this);
            }
        }

        /// <summary>
        /// SetFlags is used to set or unset one or multiple flags.
        /// </summary>
        private void SetFlags(bool value, Flags flags)
        {
            _flags = value ? (_flags | flags) : (_flags & (~flags));
        }

        /// <summary>
        /// CheckFlags returns true if all of passed flags in the bitmask are set.
        /// </summary>
        private bool CheckFlags(Flags flags)
        {
            return ((_flags & flags) == flags);
        }

        #endregion Private Methods

        // ------------------------------------------------------------------
        //
        //  Private Fields
        //
        // ------------------------------------------------------------------

        #region Private Fields 

        /// <summary>
        /// Owner of the content.
        /// </summary>
        private readonly FlowDocument _owner;

        /// <summary>
        /// Context used to communicate with PTS.
        /// </summary>
        private PtsContext _ptsContext;

        /// <summary>
        /// Root of the NameTable for PTS. Encapsulates all content of TextFlow and Cell.
        /// There is always one section.
        /// </summary>
        private Section _section;

        /// <summary>
        /// TextContainer exposing access to the content.
        /// </summary>
        private TextContainer _textContainer;

        /// <summary>
        /// TextFormatter host.
        /// </summary>
        private TextFormatterHost _textFormatterHost;

        /// <summary>
        /// Currently formatted page. Valid only during measure pass.
        /// </summary>
        private FlowDocumentPage _currentPage;

        /// <summary>
        /// Document Format Context - All information necessary during document formatting. Null outside of DocumentFormat.
        /// </summary>
        private DocumentFormatContext _documentFormatContext;

        /// <summary>
        /// Document Arrange Context - All information necessary during document arrange. Null outside of Arrange.
        /// </summary>
        private DocumentArrangeContext _documentArrangeContext;

        /// <summary>
        /// List of dirty text ranges.
        /// </summary>
        private DtrList _dtrs = null;

        /// <summary>
        /// Set to <c>true</c> if tree and / or properties were changed during document page context life time
        /// </summary>
        private bool _illegalTreeChangeDetected;

        /// <summary>
        /// Set to <c>true</c> if full formatting needs to be done (incremental upate is not possible).
        /// </summary>
        private bool _forceReformat;

        /// <summary>
        /// Set to <c>true</c> if name table should be destroyed before reformatting.
        /// </summary>
        private bool _destroyStructure;

        /// <summary>
        /// Info class with background format information
        /// </summary>
        private BackgroundFormatInfo _backgroundFormatInfo;

        /// <summary>
        /// Flow direction for page.
        /// </summary>
        private FlowDirection _pageFlowDirection;

        /// <summary>
        /// Hyphenator
        /// </summary>
        private NaturalLanguageHyphenator _hyphenator;

        /// <summary>
        /// Flags reflecting various aspects of FlowDocumentPaginator's state.
        /// </summary>
        private Flags _flags;
        [System.Flags]
        private enum Flags
        {
            FormattedOnce = 0x001,              // Element has been formatted at least once.
            ContentChangeInProgress = 0x002,    // Content change is in progress.
            FormattingInProgress = 0x008,       // Formatting operation in progress.
        }

        #endregion Private Fields 

        // ------------------------------------------------------------------
        //
        //  Private Structures and Classes
        //
        // ------------------------------------------------------------------

        #region Private Structures and Classes 

        /// <summary>
        /// Base helper class for setting / resetting structural cache state
        /// </summary>
        internal abstract class DocumentOperationContext
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="owner">Associated structural cache instance</param>
            /// <param name="page">Document page to set</param>
            internal DocumentOperationContext(StructuralCache owner, FlowDocumentPage page)
            {
                Invariant.Assert(owner != null, "Invalid owner object.");
                Invariant.Assert(page != null, "Invalid page object.");
                Invariant.Assert(owner._currentPage == null, "Page formatting reentrancy detected. Trying to create second _DocumentPageContext for the same StructuralCache.");

                _owner = owner;
                _owner._currentPage = page;
                _owner._illegalTreeChangeDetected = false;
                owner.PtsContext.Enter();
            }

            /// <summary>
            /// <see cref="IDisposable.Dispose"/>
            /// </summary>
            protected void Dispose()
            {
                Invariant.Assert(_owner._currentPage != null, "DocumentPageContext is already disposed.");

                try
                {
                    _owner.PtsContext.Leave();
                }
                finally
                {
                    _owner._currentPage = null;
                }
            }

            /// <summary>
            /// Size of document 
            /// </summary>
            internal Size DocumentPageSize { get { return _owner._currentPage.Size; } }

            /// <summary>
            /// Thickness of document margin
            /// </summary>
            internal Thickness DocumentPageMargin { get { return _owner._currentPage.Margin; } }

            /// <summary>
            /// Owner of the _DocumentPageContext.
            /// </summary>
            protected readonly StructuralCache _owner;
        }

        /// <summary>
        /// Document format context - holds any information needed during the formatting of a document.
        /// </summary>
        internal class DocumentFormatContext : DocumentOperationContext, IDisposable
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="owner">Associated structural cache instance</param>
            /// <param name="page">Document page to set</param>
            internal DocumentFormatContext(StructuralCache owner, FlowDocumentPage page) : base(owner, page)
            {
                _owner._documentFormatContext = this;
            }

            /// <summary>
            /// <see cref="IDisposable.Dispose"/>
            /// </summary>
            void IDisposable.Dispose()
            {
                _owner._documentFormatContext = null;

                base.Dispose();
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// OnFormatLine - Adds to current line format count.
            /// </summary>
            internal void OnFormatLine()
            {
                _owner._currentPage.OnFormatLine();
            }

            /// <summary>
            /// PushNewPageData - Pushes new page data to the top of the stack.
            /// </summary>
            /// <param name="pageSize">Size of page</param>
            /// <param name="pageMargin">Margin of page</param>
            /// <param name="incrementalUpdate">Are we in Incremental Update</param>
            /// <param name="finitePage">Is the current page a Finite Page</param>
            internal void PushNewPageData(Size pageSize, Thickness pageMargin, bool incrementalUpdate, bool finitePage)
            {
                _documentFormatInfoStack.Push(_currentFormatInfo);

                _currentFormatInfo.PageSize = pageSize;
                _currentFormatInfo.PageMargin = pageMargin;
                _currentFormatInfo.IncrementalUpdate = incrementalUpdate;
                _currentFormatInfo.FinitePage = finitePage;
            }

            /// <summary>
            /// PopPageData - Pops page data from top of stack.
            /// </summary>
            internal void PopPageData()
            {
                _currentFormatInfo = _documentFormatInfoStack.Pop();
            }

            /// <summary>
            /// Height of page
            /// </summary>
            internal double PageHeight { get { return _currentFormatInfo.PageSize.Height; } }

            /// <summary>
            /// Width of page
            /// </summary>
            internal double PageWidth { get { return _currentFormatInfo.PageSize.Width; } }

            /// <summary>
            /// PageSize as a size
            /// </summary>
            internal Size PageSize { get { return _currentFormatInfo.PageSize; } }

            /// <summary>
            /// Margin in page.
            /// </summary>
            internal Thickness PageMargin { get { return _currentFormatInfo.PageMargin; } }

            /// <summary>
            /// Incremental update mode
            /// </summary>
            internal bool IncrementalUpdate { get { return _currentFormatInfo.IncrementalUpdate; } }

            /// <summary>
            /// Is Finite or Bottomless Page
            /// </summary>
            internal bool FinitePage { get { return _currentFormatInfo.FinitePage; } }

            /// <summary>
            /// Rectangle of current page in TextDpi
            /// </summary>
            internal PTS.FSRECT PageRect { get { return new PTS.FSRECT(new Rect(0, 0, PageWidth, PageHeight)); } }

            /// <summary>
            /// Rectangle of margin in TextDpi
            /// </summary>
            internal PTS.FSRECT PageMarginRect { get { return new PTS.FSRECT(new Rect(PageMargin.Left, PageMargin.Top, 
                                                                                      PageSize.Width - PageMargin.Left - PageMargin.Right, 
                                                                                      PageSize.Height - PageMargin.Top - PageMargin.Bottom)); } }
            /// <summary>
            /// DependentMax used for invalidation calculations
            /// </summary>
            internal TextPointer DependentMax { set { _owner._currentPage.DependentMax = value; } }


            private struct DocumentFormatInfo
            {
                internal Size PageSize;
                internal Thickness PageMargin;
                internal bool IncrementalUpdate;
                internal bool FinitePage;
            };

            private DocumentFormatInfo _currentFormatInfo;
            private Stack<DocumentFormatInfo> _documentFormatInfoStack = new Stack<DocumentFormatInfo>();
        }

        /// <summary>
        /// Document arrange context - holds any information needed during the arrange of a document.
        /// </summary>
        internal class DocumentArrangeContext : DocumentOperationContext, IDisposable
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="owner">Associated structural cache instance</param>
            /// <param name="page">Document page to set</param>
            internal DocumentArrangeContext(StructuralCache owner, FlowDocumentPage page) : base(owner, page)
            {
                _owner._documentArrangeContext = this;
            }

            /// <summary>
            /// PushNewPageData - Pushes new page data to the top of the stack.
            /// </summary>
            /// <param name="pageContext">Page context we were formatted in</param>
            /// <param name="columnRect">Rect of current column</param>
            /// <param name="finitePage">Is the current page a Finite Page</param>
            internal void PushNewPageData(PageContext pageContext, PTS.FSRECT columnRect, bool finitePage)
            {
                _documentArrangeInfoStack.Push(_currentArrangeInfo);

                _currentArrangeInfo.PageContext = pageContext;
                _currentArrangeInfo.ColumnRect = columnRect;
                _currentArrangeInfo.FinitePage = finitePage;
            }

            /// <summary>
            /// PopPageData - Pops page data from top of stack.
            /// </summary>
            internal void PopPageData()
            {
                _currentArrangeInfo = _documentArrangeInfoStack.Pop();
            }

            /// <summary>
            /// <see cref="IDisposable.Dispose"/>
            /// </summary>
            void IDisposable.Dispose()
            {
                GC.SuppressFinalize(this);
                _owner._documentArrangeContext = null;

                base.Dispose();
            }


            /// <summary>
            /// Page Context (used to register/unregister floating elements)
            /// </summary>
            internal PageContext PageContext { get { return _currentArrangeInfo.PageContext; } }

            /// <summary>
            /// Rectangle of current column
            /// </summary>
            internal PTS.FSRECT  ColumnRect  { get { return _currentArrangeInfo.ColumnRect; } }

            /// <summary>
            /// Is current page Finite
            /// </summary>
            internal bool        FinitePage  { get { return _currentArrangeInfo.FinitePage; } }


            private struct DocumentArrangeInfo
            {
                internal PageContext PageContext;
                internal PTS.FSRECT ColumnRect;
                internal bool FinitePage;
            };

            private DocumentArrangeInfo _currentArrangeInfo;
            private Stack<DocumentArrangeInfo> _documentArrangeInfoStack = new Stack<DocumentArrangeInfo>();
}

        /// <summary>
        /// Document visual validation context - holds any information needed during the visual validation of a document.
        /// </summary>
        internal class DocumentVisualValidationContext : DocumentOperationContext, IDisposable
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="owner">Associated structural cache instance</param>
            /// <param name="page">Document page to set</param>
            internal DocumentVisualValidationContext(StructuralCache owner, FlowDocumentPage page) : base(owner, page) { }


            /// <summary>
            /// <see cref="IDisposable.Dispose"/>
            /// </summary>
            void IDisposable.Dispose() 
            {
                GC.SuppressFinalize(this);
                base.Dispose(); 
            }
        }

        #endregion Private Structures and Classes 
    }
}
