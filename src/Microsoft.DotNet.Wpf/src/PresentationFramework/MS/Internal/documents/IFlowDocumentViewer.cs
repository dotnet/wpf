// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;                       // Object, IServiceProvider
using System.Windows;               // Rect, Point
using System.Windows.Documents;     // ITextSelection
using System.Windows.Controls;      // FlowDocumentScrollViewer
using System.Windows.Controls.Primitives;   // IScrollInfo
using System.Windows.Input;         // UICommand
using System.Windows.Threading;     // Dispatcher
using MS.Internal.KnownBoxes;       // BooleanBoxes

namespace MS.Internal.Documents
{
    /// <summary>
    /// An IFlowDocumentViewer is used for FlowDocumentReader in order to simplify its 
    /// view switching logic.
    /// </summary>
    internal interface IFlowDocumentViewer
    {
        /// <summary>
        /// Navigate to the previous page.
        /// </summary>
        void PreviousPage();

        /// <summary>
        /// Navigate to the next page.
        /// </summary>
        void NextPage();

        /// <summary>
        /// Navigate to the first page.
        /// </summary>
        void FirstPage();

        /// <summary>
        /// Navigate to the last page.
        /// </summary>
        void LastPage();

        /// <summary>
        /// Print the document.
        /// </summary>
        void Print();

        /// <summary>
        /// Cancel printing of the document.
        /// </summary>
        void CancelPrint();

        /// <summary>
        /// Show find result.
        /// </summary>
        void ShowFindResult(ITextRange findResult);

        /// <summary>
        /// Whether can navigate to specific page number.
        /// </summary>
        /// <param name="pageNumber">Requested page number.</param>
        /// <returns>Whether navigation is possible or not.</returns>
        bool CanGoToPage(int pageNumber);

        /// <summary>
        /// Navigate to requested page number.
        /// </summary>
        /// <param name="pageNumber">Requested page number.</param>
        void GoToPage(int pageNumber);

        /// <summary>
        /// Associate the viewer with new document.
        /// </summary>
        /// <param name="document">New document.</param>
        void SetDocument(FlowDocument document);

        /// <summary>
        /// The content position of the document showed in the control.
        /// </summary>
        ContentPosition ContentPosition { get; set; }

        /// <summary>
        /// The Selection of TextEditor
        /// </summary>
        ITextSelection TextSelection { get; set; }

        /// <summary>
        /// Whether can navigate to the previous page.
        /// </summary>
        bool CanGoToPreviousPage { get; }

        /// <summary>
        /// Whether can navigate to the next page.
        /// </summary>
        bool CanGoToNextPage { get; }

        /// <summary>
        /// The one-based page number of the page being displayed. 
        /// If there is no content, this value will be 0.
        /// </summary>
        int PageNumber { get; }

        /// <summary>
        /// The number of pages currently available for viewing.
        /// </summary>
        int PageCount { get; }

        /// <summary>
        /// Fired when the current page number has been changed.
        /// </summary>
        event EventHandler PageNumberChanged;

        /// <summary>
        /// Fired when the page count has been changed.
        /// </summary>
        event EventHandler PageCountChanged;

        /// <summary>
        /// Fired when the print has been started.
        /// </summary>
        event EventHandler PrintStarted;

        /// <summary>
        /// Fired when the print has been completed.
        /// </summary>
        event EventHandler PrintCompleted;
    }

    /// <summary>
    /// This class is an internal subclass of the FlowDocumentScrollViewer that provides 
    /// IFlowDocumentViewer interface used by FlowDocumentReader.
    /// </summary>
    internal class ReaderScrollViewer : FlowDocumentScrollViewer, IFlowDocumentViewer
    {
        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Called when print has been completed.
        /// </summary>
        protected override void OnPrintCompleted()
        {
            base.OnPrintCompleted();

            if (_printCompleted != null)
            {
                _printCompleted(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Handler for the Print command.
        /// </summary>
        protected override void OnPrintCommand()
        {
            base.OnPrintCommand();

            if (_printStarted != null && IsPrinting)
            {
                _printStarted(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Notification that a specified property has been changed.
        /// </summary>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // PageCount and PageNumber are dependent on Document property value.
            // Hence need to raise changed events when Document property is changing.
            if (e.Property == DocumentProperty)
            {
                if (_pageNumberChanged != null)
                {
                    _pageNumberChanged(this, EventArgs.Empty);
                }
                if (_pageCountChanged != null)
                {
                    _pageCountChanged(this, EventArgs.Empty);
                }
            }
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Returns whether TextSelection is valid for document - As this is flow document, we require the text containers to be equal
        /// to allow it as a valid selection.
        /// </summary>
        private bool IsValidTextSelectionForDocument(ITextSelection textSelection, FlowDocument flowDocument)
        {
            if (textSelection.Start != null && textSelection.Start.TextContainer == flowDocument.StructuralCache.TextContainer)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Set TextSelection.
        /// </summary>
        private object SetTextSelection(object arg)
        {
            ITextSelection newTextSelection = arg as ITextSelection;
            if (newTextSelection != null && Document != null && IsValidTextSelectionForDocument(newTextSelection, Document))
            {
                ITextSelection textSelection = Document.StructuralCache.TextContainer.TextSelection;
                if (textSelection != null)
                {
                    textSelection.SetCaretToPosition(newTextSelection.AnchorPosition, newTextSelection.MovingPosition.LogicalDirection, true, true);
                    textSelection.ExtendToPosition(newTextSelection.MovingPosition);
                }
            }
            return null;
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private EventHandler _pageNumberChanged;
        private EventHandler _pageCountChanged;
        private EventHandler _printCompleted;
        private EventHandler _printStarted;

        #endregion Private Fields

        //-------------------------------------------------------------------
        //
        //  IFlowDocumentViewer Members
        //
        //-------------------------------------------------------------------

        #region IFlowDocumentViewer Members

        /// <summary>
        /// <see cref="IFlowDocumentViewer.PreviousPage"/>
        /// </summary>
        void IFlowDocumentViewer.PreviousPage()
        {
            if (ScrollViewer != null)
            {
                ScrollViewer.PageUp();
            }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.NextPage"/>
        /// </summary>
        void IFlowDocumentViewer.NextPage()
        {
            if (ScrollViewer != null)
            {
                ScrollViewer.PageDown();
            }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.FirstPage"/>
        /// </summary>
        void IFlowDocumentViewer.FirstPage()
        {
            if (ScrollViewer != null)
            {
                ScrollViewer.ScrollToHome();
            }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.LastPage"/>
        /// </summary>
        void IFlowDocumentViewer.LastPage()
        {
            if (ScrollViewer != null)
            {
                ScrollViewer.ScrollToEnd();
            }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.Print"/>
        /// </summary>
        void IFlowDocumentViewer.Print()
        {
            OnPrintCommand();
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.CancelPrint"/>
        /// </summary>
        void IFlowDocumentViewer.CancelPrint()
        {
            OnCancelPrintCommand();
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.ShowFindResult"/>
        /// </summary>
        void IFlowDocumentViewer.ShowFindResult(ITextRange findResult)
        {
            // If we found something, TextEditor will bring the selection into view.
            // It is possible, because RenderScope is inside ScrollViewer.
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.CanGoToPage"/>
        /// </summary>
        bool IFlowDocumentViewer.CanGoToPage(int pageNumber)
        {
            // FlowDocumentScrollViewer is always a 1 page document, so its page number will always be 1
            return (pageNumber == 1);
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.GoToPage"/>
        /// </summary>
        void IFlowDocumentViewer.GoToPage(int pageNumber)
        {
            if (pageNumber == 1 && ScrollViewer != null)
            {
                ScrollViewer.ScrollToHome();
            }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.SetDocument"/>
        /// </summary>
        void IFlowDocumentViewer.SetDocument(FlowDocument document)
        {
            Document = document;
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.ContentPosition"/>
        /// </summary>
        ContentPosition IFlowDocumentViewer.ContentPosition
        {
            get
            {
                return this.ContentPosition;
            }
            set
            {
                if (value != null && Document != null)
                {
                    // This need be called because the UI may not be ready when Contentposition is set.
                    Dispatcher.BeginInvoke(DispatcherPriority.Input, new DispatcherOperationCallback(BringContentPositionIntoView), value);
                }
            }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.TextSelection"/>
        /// </summary>
        ITextSelection IFlowDocumentViewer.TextSelection
        {
            get
            {
                return this.Selection;
            }
            set
            {
                if (value != null && Document != null)
                {
                    // This need be called because the UI may not be ready when Contentposition is set.
                    Dispatcher.BeginInvoke(DispatcherPriority.Input, new DispatcherOperationCallback(SetTextSelection), value);
                }
            }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.CanGoToPreviousPage"/>
        /// </summary>
        bool IFlowDocumentViewer.CanGoToPreviousPage
        {
            get { return false; }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.CanGoToNextPage"/>
        /// </summary>
        bool IFlowDocumentViewer.CanGoToNextPage
        {
            get { return false; }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.PageNumber"/>
        /// </summary>
        int IFlowDocumentViewer.PageNumber
        {
            get { return (Document != null) ? 1 : 0; }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.PageCount"/>
        /// </summary>
        int IFlowDocumentViewer.PageCount
        {
            get { return (Document != null) ? 1 : 0; }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.PageNumberChanged"/>
        /// </summary>
        event EventHandler IFlowDocumentViewer.PageNumberChanged
        {
            add { _pageNumberChanged += value; }
            remove { _pageNumberChanged -= value; }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.PageCountChanged"/>
        /// </summary>
        event EventHandler IFlowDocumentViewer.PageCountChanged
        {
            add { _pageCountChanged += value; }
            remove { _pageCountChanged -= value; }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.PrintStarted"/>
        /// </summary>
        event EventHandler IFlowDocumentViewer.PrintStarted
        {
            add { _printStarted += value; }
            remove { _printStarted -= value; }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.PrintCompleted"/>
        /// </summary>
        event EventHandler IFlowDocumentViewer.PrintCompleted
        {
            add { _printCompleted += value; }
            remove { _printCompleted -= value; }
        }

        #endregion IFlowDocumentViewer Members
    }

    /// <summary>
    /// This class is an internal subclass of the FlowDocumentPageViewer that provides 
    /// IFlowDocumentViewer interface used by FlowDocumentReader.
    /// </summary>
    internal class ReaderPageViewer : FlowDocumentPageViewer, IFlowDocumentViewer
    {
        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Called when print has been completed.
        /// </summary>
        protected override void OnPrintCompleted()
        {
            base.OnPrintCompleted();

            if (_printCompleted != null)
            {
                _printCompleted(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Handler for the Print command.
        /// </summary>
        protected override void OnPrintCommand()
        {
            base.OnPrintCommand();

            if (_printStarted != null && IsPrinting)
            {
                _printStarted(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Notification that a specified property has been changed.
        /// </summary>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // Reader control depends on the actual value of PageCount, PageNumber
            // CanGoToPreviousPage and CanGoToNextPage. So when values of those 
            // properties are changing, schedule async update of the viewer.
            if (e.Property == PageCountProperty || e.Property == MasterPageNumberProperty ||
                e.Property == CanGoToPreviousPageProperty || e.Property == CanGoToNextPageProperty)
            {
                // If there is already pending update request, do nothng. Otherwise schedule a dispatcher task.
                if (!_raisePageNumberChanged && !_raisePageCountChanged)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Input, new DispatcherOperationCallback(RaisePropertyChangedAsync), null);
                }
                // Treat CanGoToPage properties as PageNumber. Becasuse PageNumber change affects 
                // those properties, the reader control will update them.
                if (e.Property == PageCountProperty)
                {
                    _raisePageCountChanged = true;
                    CoerceValue(CanGoToNextPageProperty);
                }
                else if (e.Property == MasterPageNumberProperty)
                {
                    _raisePageNumberChanged = true;
                    CoerceValue(CanGoToNextPageProperty);
                }
                else
                {
                    _raisePageNumberChanged = true;
                }
            }
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Set TextSelection.
        /// </summary>
        private object SetTextSelection(object arg)
        {
            ITextSelection newTextSelection = arg as ITextSelection;
            FlowDocument flowDocument = Document as FlowDocument;
            if (newTextSelection != null && flowDocument != null &&
                newTextSelection.AnchorPosition != null &&
                newTextSelection.AnchorPosition.TextContainer == flowDocument.StructuralCache.TextContainer)
            {
                ITextSelection textSelection = flowDocument.StructuralCache.TextContainer.TextSelection;
                if (textSelection != null)
                {
                    textSelection.SetCaretToPosition(newTextSelection.AnchorPosition, newTextSelection.MovingPosition.LogicalDirection, true, true);
                    textSelection.ExtendToPosition(newTextSelection.MovingPosition);
                }
            }
            return null;
        }

        /// <summary>
        /// Asynchronously notify about property changes.
        /// </summary>
        private object RaisePropertyChangedAsync(object arg)
        {
            // PageCount and PageNumber are dependent on associated DP values.
            // Hence need to raise changed events when those DPs are changing.
            if (_raisePageCountChanged)
            {
                if (_pageCountChanged != null)
                {
                    _pageCountChanged(this, EventArgs.Empty);
                }
                _raisePageCountChanged = false;
            }
            if (_raisePageNumberChanged)
            {
                if (_pageNumberChanged != null)
                {
                    _pageNumberChanged(this, EventArgs.Empty);
                }
                _raisePageNumberChanged = false;
            }
            return null;
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        private EventHandler _pageNumberChanged;
        private EventHandler _pageCountChanged;
        private EventHandler _printCompleted;
        private EventHandler _printStarted;
        private bool _raisePageNumberChanged;
        private bool _raisePageCountChanged;

        #endregion Private Fields

        //-------------------------------------------------------------------
        //
        //  IFlowDocumentViewer Members
        //
        //-------------------------------------------------------------------

        #region IFlowDocumentViewer Members

        /// <summary>
        /// <see cref="IFlowDocumentViewer.PreviousPage"/>
        /// </summary>
        void IFlowDocumentViewer.PreviousPage()
        {
            OnPreviousPageCommand();
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.NextPage"/>
        /// </summary>
        void IFlowDocumentViewer.NextPage()
        {
            OnNextPageCommand();
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.FirstPage"/>
        /// </summary>
        void IFlowDocumentViewer.FirstPage()
        {
            OnFirstPageCommand();
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.LastPage"/>
        /// </summary>
        void IFlowDocumentViewer.LastPage()
        {
            OnLastPageCommand();
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.Print"/>
        /// </summary>
        void IFlowDocumentViewer.Print()
        {
            OnPrintCommand();
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.CancelPrint"/>
        /// </summary>
        void IFlowDocumentViewer.CancelPrint()
        {
            OnCancelPrintCommand();
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.ShowFindResult"/>
        /// </summary>
        void IFlowDocumentViewer.ShowFindResult(ITextRange findResult)
        {
            if (findResult.Start is ContentPosition)
            {
                BringContentPositionIntoView((ContentPosition)findResult.Start);
            }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.CanGoToPage"/>
        /// </summary>
        bool IFlowDocumentViewer.CanGoToPage(int pageNumber)
        {
            return CanGoToPage(pageNumber);
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.GoToPage"/>
        /// </summary>
        void IFlowDocumentViewer.GoToPage(int pageNumber)
        {
            OnGoToPageCommand(pageNumber);
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.SetDocument"/>
        /// </summary>
        void IFlowDocumentViewer.SetDocument(FlowDocument document)
        {
            Document = document;
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.ContentPosition"/>
        /// </summary>
        ContentPosition IFlowDocumentViewer.ContentPosition
        {
            get { return ContentPosition; }
            set
            {
                if (value != null && Document != null)
                {
                    // This need be called because the UI may not be ready when Contentposition is set.
                    Dispatcher.BeginInvoke(DispatcherPriority.Input, new DispatcherOperationCallback(BringContentPositionIntoView), value);
                }
            }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.TextSelection"/>
        /// </summary>
        ITextSelection IFlowDocumentViewer.TextSelection
        {
            get
            {
                return this.Selection;
            }
            set
            {
                if (value != null && Document != null)
                {
                    // This need be called because the UI may not be ready when Contentposition is set.
                    Dispatcher.BeginInvoke(DispatcherPriority.Input, new DispatcherOperationCallback(SetTextSelection), value);
                }
            }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.CanGoToPreviousPage"/>
        /// </summary>
        bool IFlowDocumentViewer.CanGoToPreviousPage
        {
            get { return CanGoToPreviousPage; }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.CanGoToNextPage"/>
        /// </summary>
        bool IFlowDocumentViewer.CanGoToNextPage
        {
            get { return CanGoToNextPage; }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.PageNumber"/>
        /// </summary>
        int IFlowDocumentViewer.PageNumber
        {
            get { return MasterPageNumber; }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.PageCount"/>
        /// </summary>
        int IFlowDocumentViewer.PageCount
        {
            get { return PageCount; }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.PageNumberChanged"/>
        /// </summary>
        event EventHandler IFlowDocumentViewer.PageNumberChanged
        {
            add { _pageNumberChanged += value; }
            remove { _pageNumberChanged -= value; }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.PageCountChanged"/>
        /// </summary>
        event EventHandler IFlowDocumentViewer.PageCountChanged
        {
            add { _pageCountChanged += value; }
            remove { _pageCountChanged -= value; }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.PrintStarted"/>
        /// </summary>
        event EventHandler IFlowDocumentViewer.PrintStarted
        {
            add { _printStarted += value; }
            remove { _printStarted -= value; }
        }

        /// <summary>
        /// <see cref="IFlowDocumentViewer.PrintCompleted"/>
        /// </summary>
        event EventHandler IFlowDocumentViewer.PrintCompleted
        {
            add { _printCompleted += value; }
            remove { _printCompleted -= value; }
        }

        #endregion IFlowDocumentViewer Members
    }

    /// <summary>
    /// This class is an internal subclass of the FlowDocumentPageViewer that provides 
    /// IFlowDocumentViewer interface used by FlowDocumentReader.
    /// In addition to that it also provides support for TwoPage view.
    /// </summary>
    internal class ReaderTwoPageViewer : ReaderPageViewer
    {
        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Handler for the PreviousPage command.
        /// </summary>
        protected override void OnPreviousPageCommand()
        {
            GoToPage(Math.Max(1, MasterPageNumber - 2));
        }

        /// <summary>
        /// Handler for the NextPage command.
        /// </summary>
        protected override void OnNextPageCommand()
        {
            GoToPage(Math.Min(PageCount, MasterPageNumber + 2));
        }

        /// <summary>
        /// Handler for the LastPage command.
        /// </summary>
        protected override void OnLastPageCommand()
        {
            // Always navigate to odd PageNumber
            GoToPage(PageCount);
        }

        /// <summary>
        /// Handler for the GoToPage command.
        /// </summary>
        /// <param name="pageNumber"></param>
        protected override void OnGoToPageCommand(int pageNumber)
        {
            // Go to only odd page numbers. 
            base.OnGoToPageCommand((((pageNumber - 1) / 2) * 2) + 1);
        }

        /// <summary>
        /// Notification that a specified property has been changed.
        /// </summary>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == MasterPageNumberProperty)
            {
                // Only odd page numbers are allowed. 
                int pageNumber = (int)e.NewValue;
                pageNumber = (((pageNumber - 1) / 2) * 2) + 1;
                if (pageNumber != (int)e.NewValue)
                {
                    GoToPage(pageNumber);
                }
            }
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        static ReaderTwoPageViewer()
        {
            CanGoToNextPagePropertyKey.OverrideMetadata(typeof(ReaderTwoPageViewer), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox, null, new CoerceValueCallback(CoerceCanGoToNextPage)));
        }

        /// <summary>
        /// Coerce the value for CanGoToNextPage property.
        /// </summary>
        private static object CoerceCanGoToNextPage(DependencyObject d, object value)
        {
            Invariant.Assert(d != null && d is ReaderTwoPageViewer);

            ReaderTwoPageViewer viewer = (ReaderTwoPageViewer)d;
            return (viewer.MasterPageNumber < viewer.PageCount - 1);
        }

        #endregion Private Methods
    }
}
