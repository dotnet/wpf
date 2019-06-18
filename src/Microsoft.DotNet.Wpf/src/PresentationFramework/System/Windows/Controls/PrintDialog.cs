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
using System.Security.Permissions;
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
        /// <SecurityNote>
        ///     Critical:    - setting critical data (_printQueue, _printTicket, _dialogInvoked)
        ///     PublicOk:    - We are setting these to "known" values of null.  There
        ///                    is no data exposure here.
        /// </SecurityNote>
        public
        PrintDialog(
            )
        {
            _dialogInvoked = false;

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
        /// <SecurityNote>
        ///     Critical:    - The getter is critical since it accesses critical data in web application mode.
        ///                  - The getter is calling critical code (AcquireDefaultPrintQueue).
        ///                  - The setter is critical since it is returning critical data.
        ///     PublicOk:    - A demand is made for default printing before returning the print queue.  This
        ///                    would be the same permission required to get the print queue in the first place.
        ///                  - We also demand before setting the print queue object.  If they can satisfy the
        ///                    the demand then they are safe to print anyways.
        /// </SecurityNote>
        public PrintQueue PrintQueue
        {
            get
            {
                SecurityHelper.DemandPrintDialogPermissions();

                if (_printQueue == null)
                {
                    _printQueue = AcquireDefaultPrintQueue();
                }

                return _printQueue;
            }
            set
            {
                SecurityHelper.DemandPrintDialogPermissions();

                _printQueue = value;
            }
        }

        /// <summary>
        /// Get or sets the current PrintTicket object.
        /// </summary>
        /// <SecurityNote>
        ///     Critical:    - The getter is critical since it accesses critical data.
        ///                  - The getter is calling critical code (AcquireDefaultPrintTicket).
        ///                  - The setter is critical since it is returning critical data.
        ///     PublicOk:    - A demand is made for default printing before returning the print ticket.  This
        ///                    would be the same permission required to get the print ticket in the first place.
        ///                  - We also demand before setting the print ticket object.  If they can satisfy the
        ///                    the demand then they are safe to print anyways.
        /// </SecurityNote>
        public PrintTicket PrintTicket
        {
            get
            {
                SecurityHelper.DemandPrintDialogPermissions();

                if (_printTicket == null)
                {
                    _printTicket = AcquireDefaultPrintTicket(this.PrintQueue);
                }

                return _printTicket;
            }
            set
            {
                SecurityHelper.DemandPrintDialogPermissions();

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
        /// <SecurityNote>
        ///     Critical:    - Accesses and sets critical data.
        ///     PublicOk:    - Data is internal to this dialog and can only be retrieved
        ///                    by other critical code.  No information leaves this method.
        /// </SecurityNote>
        public
        Nullable<bool>
        ShowDialog()
        {
            //
            // Reset this flag as we have not displayed the dialog yet.
            //
            _dialogInvoked = false;

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
                _dialogInvoked = true;
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
        /// <SecurityNote>
        ///     Critical:    - Sets a critical data property.
        ///     PublicOk:    - The critical data is a flag that needs to be reset for each print
        ///                    job to enforce the dialog invocation on every print operation.  Without
        ///                    this method resetting this flag we would not be honoring the security
        ///                    goal of displaying the dialog once per print job.
        /// </SecurityNote>
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
            _dialogInvoked                  = false;
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
        /// <SecurityNote>
        ///     Critical:    - Sets a critical data property.
        ///     PublicOk:    - The critical data is a flag that needs to be reset for each print
        ///                    job to enforce the dialog invocation on every print operation.  Without
        ///                    this method resetting this flag we would not be honoring the security
        ///                    goal of displaying the dialog once per print job.
        /// </SecurityNote>
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
            _dialogInvoked = false;
        }

        #endregion Public methods

        #region Private methods

        /// <SecurityNote>
        ///     Critical:    - Asserts to obtain the default print queue from the local server.
        /// </SecurityNote>
        private
        PrintQueue
        AcquireDefaultPrintQueue()
        {
            PrintQueue printQueue = null;

            MS.Internal.SystemDrawingHelper.NewDefaultPrintingPermission().Assert(); //BlessedAssert
            try
            {
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
            }
            finally
            {
                CodeAccessPermission.RevertAssert();
            }

            return printQueue;
        }

        /// <SecurityNote>
        ///     Critical:    - Asserts to obtain the PrintTicket from the specified PrintQueue
        ///                    object or create a blank PrintTicket object (i.e. PrintTicket::ctor).
        /// </SecurityNote>
        private
        PrintTicket
        AcquireDefaultPrintTicket(
            PrintQueue printQueue
            )
        {
            PrintTicket printTicket = null;

            MS.Internal.SystemDrawingHelper.NewDefaultPrintingPermission().Assert(); //BlessedAssert
            try
            {
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
            }
            finally
            {
                CodeAccessPermission.RevertAssert();
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

        /// <SecurityNote>
        ///     Critical:    - Invokes a critical method (PickCorrectPrintingEnvironment).
        ///     TreatAsSafe: - Critical data returned from above method is internal and does
        ///                    not leave the scope of this method.  It is only used to calculate
        ///                    non-critical values.
        /// </SecurityNote>
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

        /// <SecurityNote>
        ///     Critical:    - Asserts for PrintingPermissionLevel.DefaultPrinting
        ///                    to be able to use the printQueue to create the
        ///                    XpsDocumentWriter.
        ///     TreatAsSafe: - The assert is only done after ensuring that the user
        ///                    has conciously made a decision to print by successfully
        ///                    dismissing the Print Dialog.  This logic of a dialog
        ///                    being required is only needed for partial trust applications.
        ///                    The logic of checking this criteria is contained within the
        ///                    PickCorrectPrintingEnvironment method.
        ///                  - The XpsDocumentWriter instance returned from this method
        ///                    is not unsafe since the application is either full trust
        ///                    or the user chose to print.  It is okay for the application
        ///                    to use the XpsDocumentWriter to print at this point.
        /// </SecurityNote>
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

            MS.Internal.SystemDrawingHelper.NewDefaultPrintingPermission().Assert(); //BlessedAssert
            try
            {
                if(printQueue != null)
                {
                    printQueue.CurrentJobSettings.Description = description;
                }

                writer = PrintQueue.CreateXpsDocumentWriter(printQueue);

                PrintDlgPrintTicketEventHandler eventHandler = new PrintDlgPrintTicketEventHandler(printTicket);

                writer.WritingPrintTicketRequired +=
                new WritingPrintTicketRequiredEventHandler(eventHandler.SetPrintTicket);
            }
            finally
            {
                CodeAccessPermission.RevertAssert();
            }

            return writer;
        }

        /// <SecurityNote>
        ///     Critical:    - Accesses critical data and returns it to the caller.
        ///                  - Calls critical code (AcquireDefaultPrintQueue/AcquireDefaultPrintTicket)
        ///                  - Detects whether a caller is allowed to acquire this data
        ///                    based on a demand.  This demand is only performed if the
        ///                    dialog was not invoked already.  It is fine to return the
        ///                    data if the dialog was invoked, however, the data is still
        ///                    critical.
        ///
        /// NOTE:  This method validates that a dialog was invoked prior to returning the
        ///        PrintQueue and PrintTicket for the case of web applications.  If the
        ///        dialog was not invoked then an exception is thrown.
        /// </SecurityNote>
        private
        void
        PickCorrectPrintingEnvironment(
            ref PrintQueue  printQueue,
            ref PrintTicket printTicket
            )
        {
            if (_dialogInvoked == false)
            {
                //
                // If the dialog has not been invoked then the user needs printing permissions.
                // If the demand succeeds then they can print.  If the demand fails, then we
                // tell them that the print dialog must be displayed first by throwing a dialog
                // exception.
                //
                try
                {
                    SecurityHelper.DemandPrintDialogPermissions();
                }
                catch (SecurityException)
                {
                    throw new PrintDialogException(SR.Get(SRID.PartialTrustPrintDialogMustBeInvoked));
                }
            }

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

        /// <SecurityNote>
        /// The PrintTicket is critical and not obtainable from a partial
        /// trust application unless they can satisfy a printing permission
        /// demand.
        /// </SecurityNote>
        private
        PrintTicket                 _printTicket;

        /// <SecurityNote>
        /// The PrintQueue is critical and not obtainable from a partial
        /// trust application unless they can satisfy a printing permission
        /// demand.
        /// </SecurityNote>
        private
        PrintQueue                  _printQueue;

        /// <SecurityNote>
        /// This variable is used to determine whether a user actually invoked
        /// and dismissed the dialog prior to printing.  In a partial trust app,
        /// we can safely perform the necessary asserts to print as long as the
        /// user said printing was okay.
        /// </SecurityNote>
        private
        bool                        _dialogInvoked;

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

            /// <SecurityNote>
            ///     Critical    -   PrintTicket argument is critical because it is defined in the none APTCA assembly ReachFramework.dll
            ///     TreatAsSafe -   PrintTicket type is safe
            /// </SecurityNote>
            public
            PrintDlgPrintTicketEventHandler(
                PrintTicket printTicket
                )
            {
                _printTicket = printTicket;
            }

            #endregion Constructor

            #region Public Methods

            /// <SecurityNote>
            ///     Critical    -   Makes use of PrintTicket type which is critical because it is defined in the none APTCA assembly ReachFramework.dll
            ///                 -   Makes use of PrintTicketLevel type which is critical because it is defined in the none APTCA assembly ReachFramework.dll
            ///     TreatAsSafe -   PrintTicket type is safe
            /// </SecurityNote>
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

            /// <SecurityNote>
            ///     Critical    -   Field for PrintTicket type which is critical because it is defined in the none APTCA assembly ReachFramework.dll
            /// </SecurityNote>
            private
            PrintTicket _printTicket;

            #endregion Private Data
        };

        #endregion Internal classes
    }
}
#endif
