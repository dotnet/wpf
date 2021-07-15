// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !DONOTREFPRINTINGASMMETA
/*
    Abstract:
        This file contains the implementation of the PrintDialog class
        and its supporting enums.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Printing;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Xps;
using MS.Internal.Printing;
using System.Windows.Xps.Serialization;
using System.Windows.Documents;
using System.Windows.Documents.Serialization;  // WritingCompletedEventArgs
using MS.Internal.PresentationFramework;

namespace System.Windows.Controls
{
    /// <summary>
    /// This class is used to show a print which will configure a
    /// user PrintTicket and Printer based on user input.  This
    /// PrintTicket and Printer is then used to perform a print job.
    /// </summary>
    public class PrintDialog
    {
        #region Constructors

        /// <summary>
        /// Instantiates an instance of the Print Dialog.
        /// </summary>
        public
        PrintDialog(
            )
        {

            _printQueue = null;
            _printTicket = null;

            _isPrintableAreaWidthUpdated  = false;
            _isPrintableAreaHeightUpdated = false;

            _pageRangeSelection = PageRangeSelection.AllPages;
            _minPage = 1;
            _maxPage = 9999;
            _userPageRangeEnabled = false;
        }

        #endregion Constructors

        #region Public properties

        /// <summary>
        /// Gets or Sets the PageRangeSelection option for the print dialog.
        /// </summary>
        public PageRangeSelection PageRangeSelection
        {
            get
            {
                return _pageRangeSelection;
            }
            set
            {
                _pageRangeSelection = value;
            }
        }

        /// <summary>
        /// Gets or sets a PageRange objects used when the PageRangeSelection
        /// option is set to UserPages.
        /// </summary>
        public PageRange PageRange
        {
            get
            {
                return _pageRange;
            }
            set
            {
                if ((value.PageTo <= 0) || (value.PageFrom <= 0))
                {
                    throw new System.ArgumentException(SR.Get(SRID.PrintDialogInvalidPageRange), "PageRange");
                }

                _pageRange = value;

                if (_pageRange.PageFrom > _pageRange.PageTo)
                {
                    int temp = _pageRange.PageFrom;
                    _pageRange.PageFrom = _pageRange.PageTo;
                    _pageRange.PageTo = temp;
                }
            }
        }

        /// <summary>
        /// Gets or a sets a flag to enable/disable the user page range support on
        /// the print dialog.
        /// </summary>
        public bool UserPageRangeEnabled
        {
            get
            {
                return _userPageRangeEnabled;
            }
            set
            {
                _userPageRangeEnabled = value;
            }
        }

        /// <summary>
        /// Gets or a sets a flag to enable/disable the selected pages support on
        /// the print dialog.
        /// </summary>
        public bool SelectedPagesEnabled
        {
            get
            {
                return _selectedPagesEnabled;
            }
            set
            {
                _selectedPagesEnabled = value;
            }
        }

        /// <summary>
        /// Gets or a sets a flag to enable/disable the current page selection support on
        /// the print dialog.
        /// </summary>
        public bool CurrentPageEnabled
        {
            get
            {
                return _currentPageEnabled;
            }
            set
            {
                _currentPageEnabled = value;
            }
        }

        // the following two properties return non CLS-compliant type UInt32 (bug 1788246)
        #pragma warning disable 3003

        /// <summary>
        /// Gets or sets the minimum page number allowed in the page ranges.
        /// </summary>
        public UInt32 MinPage
        {
            get
            {
                return _minPage;
            }
            set
            {
                if (_minPage <= 0)
                {
                    throw new System.ArgumentException(SR.Get(SRID.PrintDialogZeroNotAllowed, "MinPage"));
                }

                _minPage = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum page number allowed in the page ranges.
        /// </summary>
        public UInt32 MaxPage
        {
            get
            {
                return _maxPage;
            }
            set
            {
                if (_maxPage <= 0)
                {
                    throw new System.ArgumentException(SR.Get(SRID.PrintDialogZeroNotAllowed, "MaxPage"));
                }

                _maxPage = value;
            }
        }

        #pragma warning restore 3003

        /// <summary>
        /// Gets or sets the printer selection.
        /// </summary>
        public PrintQueue PrintQueue
        {
            get
            {

                if (_printQueue == null)
                {
                    _printQueue = AcquireDefaultPrintQueue();
                }

                return _printQueue;
            }
            set
            {

                _printQueue = value;
            }
        }

        /// <summary>
        /// Get or sets the current PrintTicket object.
        /// </summary>
        public PrintTicket PrintTicket
        {
            get
            {

                if (_printTicket == null)
                {
                    _printTicket = AcquireDefaultPrintTicket(this.PrintQueue);
                }

                return _printTicket;
            }
            set
            {

                _printTicket = value;
            }
        }

        /// <summary>
        /// Get the Width of the area on paper to which the application can print
        /// </summary>
        public
        double
        PrintableAreaWidth
        {
            get
            {
                if( ((_isPrintableAreaWidthUpdated == false) && (_isPrintableAreaHeightUpdated == false)) ||
                    ((_isPrintableAreaWidthUpdated == true)  && (_isPrintableAreaHeightUpdated == false)))
                {
                    _isPrintableAreaWidthUpdated  = true;
                    _isPrintableAreaHeightUpdated = false;

                    UpdatePrintableAreaSize();
                }

                return _printableAreaWidth;
            }
        }

        /// <summary>
        /// Get the Height of the area on paper to which the application can print
        /// </summary>
        public
        double
        PrintableAreaHeight
        {
            get
            {
                if( ((_isPrintableAreaWidthUpdated == false) && (_isPrintableAreaHeightUpdated == false)) ||
                    ((_isPrintableAreaWidthUpdated == false)  && (_isPrintableAreaHeightUpdated == true)))
                {
                    _isPrintableAreaWidthUpdated  = false;
                    _isPrintableAreaHeightUpdated = true;

                    UpdatePrintableAreaSize();
                }
                return _printableAreaHeight;
            }
        }


        #endregion Public properties

        #region Public methods

        /// <summary>
        /// Pops the dialog up to the user in a modal form.
        /// </summary>
        public
        Nullable<bool>
        ShowDialog()
        {

            Win32PrintDialog dlg = new Win32PrintDialog();

            //
            // Setup the old values if any exist.
            //
            dlg.PrintTicket = _printTicket;
            dlg.PrintQueue = _printQueue;
            dlg.MinPage = Math.Max(1, Math.Min(_minPage, _maxPage));
            dlg.MaxPage = Math.Max(dlg.MinPage, Math.Max(_minPage, _maxPage));
            dlg.PageRangeEnabled = _userPageRangeEnabled;
            dlg.SelectedPagesEnabled = _selectedPagesEnabled;
            dlg.CurrentPageEnabled = _currentPageEnabled;

            dlg.PageRange = new PageRange(
                Math.Max((int)dlg.MinPage, _pageRange.PageFrom),
                Math.Min((int)dlg.MaxPage, _pageRange.PageTo));
            dlg.PageRangeSelection = _pageRangeSelection;

            //
            // Invoke the Win32 dialog
            //
            UInt32 dialogResult = dlg.ShowDialog();

            if ((dialogResult == MS.Internal.Printing.NativeMethods.PD_RESULT_APPLY) ||
                (dialogResult == MS.Internal.Printing.NativeMethods.PD_RESULT_PRINT))
            {
                _printTicket = dlg.PrintTicket;
                _printQueue = dlg.PrintQueue;
                _pageRange = dlg.PageRange;
                _pageRangeSelection = dlg.PageRangeSelection;
            }

            return (dialogResult == MS.Internal.Printing.NativeMethods.PD_RESULT_PRINT);
        }

        /// <summary>
        /// Prints a Visual to the currently selected Print Queue.
        /// </summary>
        /// <param name="visual">
        /// The visual to be printed.
        /// </param>
        /// <param name="description">
        /// Description of the job to be printed. This shows in the Printer UI
        /// </param>
        public
        void
        PrintVisual(
            Visual visual,
            String description
            )
        {
            if (visual == null)
            {
                throw new ArgumentNullException("visual");
            }

            XpsDocumentWriter writer = CreateWriter(description);

            writer.Write(visual, _printTicket);

            _printableAreaWidth             = 0;
            _printableAreaHeight            = 0;
            _isPrintableAreaWidthUpdated    = false;
            _isPrintableAreaHeightUpdated   = false;
        }


        /// <summary>
        /// Prints an DocumentPaginator based document to the currently selected Print Queue.
        /// </summary>
        /// <param name="documentPaginator">
        /// The DocumentPaginator to be printed.
        /// </param>
        /// <param name="description">
        /// Description of the job to be printed. This shows in the Printer UI
        /// </param>
        public
        void
        PrintDocument(
            DocumentPaginator  documentPaginator,
            String             description
            )
        {
            if (documentPaginator == null)
            {
                throw new ArgumentNullException("documentPaginator");
            }

            XpsDocumentWriter writer = CreateWriter(description);

            writer.Write(documentPaginator, _printTicket);

            _printableAreaWidth = 0;
            _printableAreaHeight = 0;
            _isPrintableAreaWidthUpdated = false;
            _isPrintableAreaHeightUpdated = false;
        }

        #endregion Public methods

        #region Private methods

        private
        PrintQueue
        AcquireDefaultPrintQueue()
        {
            PrintQueue printQueue = null;

            try
            {
                LocalPrintServer server = new LocalPrintServer();
                printQueue = server.DefaultPrintQueue;
            }
            catch (PrintSystemException)
            {
                //
                // It is entirely possible for there to be no "default" printer.  In this case,
                // the printing system throws an exception.  We do not want this to propagate
                // up.  Instead, returning null is fine.
                //
                printQueue = null;
            }

            return printQueue;
        }

        private
        PrintTicket
        AcquireDefaultPrintTicket(
            PrintQueue printQueue
            )
        {
            PrintTicket printTicket = null;

            try
            {
                if (printQueue != null)
                {
                    printTicket = printQueue.UserPrintTicket;
                    if (printTicket == null)
                    {
                        printTicket = printQueue.DefaultPrintTicket;
                    }
                }
            }
            catch (PrintSystemException)
            {
                //
                // The printing subsystem can throw an exception in certain cases when
                // the print ticket is unavailable.  If it does we will handle this
                // below.  There is no real need to bubble this up to the application.
                //
                printTicket = null;
            }

            //
            // If the printing subsystem could not give us a print ticket either due to
            // a failure or because a user/system default was not available, then just
            // create a blank/empty one.
            //
            if (printTicket == null)
            {
                printTicket = new PrintTicket();
            }

            return printTicket;
        }

        private
        void
        UpdatePrintableAreaSize(
            )
        {
            PrintQueue  printQueue  = null;
            PrintTicket printTicket = null;

            PickCorrectPrintingEnvironment(ref printQueue, ref printTicket);

            PrintCapabilities printCap = null;
            if (printQueue != null)
            {
                printCap = printQueue.GetPrintCapabilities(printTicket);
            }

            // PrintCapabilities OrientedPageMediaWidth/Height are Nullable
            if ((printCap != null) &&
                (printCap.OrientedPageMediaWidth != null) &&
                (printCap.OrientedPageMediaHeight != null))
            {
                _printableAreaWidth  = (double)printCap.OrientedPageMediaWidth;
                _printableAreaHeight = (double)printCap.OrientedPageMediaHeight;
            }
            else
            {
                // Initialize page size to portrait Letter size.
                // This is our fallback if PrintTicket doesn't specify the page size.
                _printableAreaWidth = 816;
                _printableAreaHeight = 1056;

                // PrintTicket's PageMediaSize could be null and PageMediaSize Width/Height are Nullable

                if ((printTicket.PageMediaSize != null) &&
                    (printTicket.PageMediaSize.Width != null) &&
                    (printTicket.PageMediaSize.Height != null))
                {
                    _printableAreaWidth  = (double)printTicket.PageMediaSize.Width;
                    _printableAreaHeight = (double)printTicket.PageMediaSize.Height;
                }

                // If we are using PrintTicket's PageMediaSize dimensions to populate the widht/height values,
                // we need to adjust them based on current orientation. PrintTicket's PageOrientation is Nullable.
                if (printTicket.PageOrientation != null)
                {
                    PageOrientation orientation = (PageOrientation)printTicket.PageOrientation;

                    // need to swap width/height in landscape orientation
                    if ((orientation == PageOrientation.Landscape) ||
                        (orientation == PageOrientation.ReverseLandscape))
                    {
                        double t = _printableAreaWidth;
                        _printableAreaWidth  = _printableAreaHeight;
                        _printableAreaHeight = t;
                    }
                }
            }
        }

        private
        XpsDocumentWriter
        CreateWriter(
            String  description
            )
        {
            PrintQueue        printQueue  = null;
            PrintTicket       printTicket = null;
            XpsDocumentWriter writer      = null;

            PickCorrectPrintingEnvironment(ref printQueue, ref printTicket);

            if(printQueue != null)
            {
                printQueue.CurrentJobSettings.Description = description;
            }

            writer = PrintQueue.CreateXpsDocumentWriter(printQueue);

            PrintDlgPrintTicketEventHandler eventHandler = new PrintDlgPrintTicketEventHandler(printTicket);

            writer.WritingPrintTicketRequired +=
            new WritingPrintTicketRequiredEventHandler(eventHandler.SetPrintTicket);

            return writer;
        }

        private
        void
        PickCorrectPrintingEnvironment(
            ref PrintQueue  printQueue,
            ref PrintTicket printTicket
            )
        {
            //
            // If the default print queue and print ticket have not already
            // been selected then update them now since we need them.
            //
            // NOTE:  If this code gets called then we know the dialog has never
            //        been invoked but the above demand was satisfied.  In this
            //        case we want to just pickup the user defaults.
            //
            if (_printQueue == null)
            {
                _printQueue = AcquireDefaultPrintQueue();
            }
            if (_printTicket == null)
            {
                _printTicket = AcquireDefaultPrintTicket(_printQueue);
            }

            //
            // We should have valid print queue and print ticket objects to
            // return now.  As a note, a null PrintQueue is valid for this
            // since the dialog will automatically pick up the user default
            // printer for us.
            //
            printQueue = _printQueue;
            printTicket = _printTicket;
        }

        #endregion Private methods

        #region Private data

        private
        PrintTicket                 _printTicket;

        private
        PrintQueue                  _printQueue;

        private
        PageRangeSelection          _pageRangeSelection;

        private
        PageRange                   _pageRange;

        private
        bool                        _userPageRangeEnabled;

        private
        bool                        _selectedPagesEnabled;

        private
        bool                        _currentPageEnabled;        

        private
        UInt32                      _minPage;

        private
        UInt32                      _maxPage;

        private
        double                      _printableAreaWidth;

        private
        double                      _printableAreaHeight;

        private
        bool                        _isPrintableAreaWidthUpdated;

        private
        bool                        _isPrintableAreaHeightUpdated;

        #endregion Private data

        #region Internal classes

        internal class PrintDlgPrintTicketEventHandler
        {
            #region Constructor

            public
            PrintDlgPrintTicketEventHandler(
                PrintTicket printTicket
                )
            {
                _printTicket = printTicket;
            }

            #endregion Constructor

            #region Public Methods

            public
            void
            SetPrintTicket(
                Object sender,
                WritingPrintTicketRequiredEventArgs args
                )
            {
                if (args.CurrentPrintTicketLevel == PrintTicketLevel.FixedDocumentSequencePrintTicket)
                {
                    args.CurrentPrintTicket = _printTicket;
                }
            }

            #endregion Public Methods

            #region Private Data

            private
            PrintTicket _printTicket;

            #endregion Private Data
        };

        #endregion Internal classes
    }
}
#endif
