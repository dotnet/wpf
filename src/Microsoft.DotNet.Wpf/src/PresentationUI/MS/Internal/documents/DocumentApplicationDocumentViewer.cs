// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: The main DocumentViewer subclass that drives the MongooseUI


// Used to support the warnings disabled below
#pragma warning disable 1634, 1691

using MS.Internal.Documents.Application;
using MS.Internal.IO.Packaging;             // For PreloadedPackages
using MS.Internal.PresentationUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;                // For IValueConverter
using System.Globalization;                 // For localization of string conversion
using System.IO;
using System.IO.Packaging;                  // For Packages
using System.Printing;                      // For PrintQueue
using System.Security;
using System.Windows;
using System.Windows.Controls;              // For Page Ranges
using System.Windows.Controls.Primitives;   // For ToggleButton
using System.Windows.Data;                  // For data binding
using System.Windows.Documents;             // For PresentationUIStyleResources
using System.Windows.Documents.Serialization;             // For WritingCompletedEventArgs
using System.Windows.Input;                 // For focus / input based events
using System.Windows.Interop;               // For WindowInteropHelper
using System.Windows.Navigation;            // For NavigationWindow
using System.Windows.Markup;                // For MarkupExtension
using System.Windows.Threading;             // For DispatcherPriority
using System.Windows.TrustUI;               // For string resources
using System.Windows.Xps;                   // XpsDocumentWriter
using System.Windows.Media;                 // Visual Stuff

namespace MS.Internal.Documents
{
    [FriendAccessAllowed]
    internal sealed class DocumentApplicationDocumentViewer : DocumentViewer
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
        #region Constructors

        /// <summary>
        /// Static constructor used to ensure commands are setup.
        /// </summary>
        static DocumentApplicationDocumentViewer()
        {
            CreateCommandBindings();
        }

        /// <summary>
        /// Constructor for DocumentApplicationDocumentViewer
        /// </summary>
        public DocumentApplicationDocumentViewer() : base()
        {
            System.Diagnostics.Debug.Assert(
                _singletonInstance == null,
                "DocumentApplicationDocumentViewer constructor called twice.");

            if (_singletonInstance == null)
            {
                _singletonInstance = this;

                // Setup the CommandEnforcer before any of the UI is prepared.
                CreateEnforcer();
            }
        }        

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        #region Public Properties
        /// <summary>
        /// This command will give focus to the first control in our main toolbar.
        /// </summary>
        public static RoutedUICommand FocusToolBar
        {
            get
            {
                return _focusToolBarCommand;
            }
        }

        /// <summary>
        /// This command will issue the Sign Command to the DocumentSignatureManager
        /// </summary>
        public static RoutedUICommand Sign
        {
            get
            {
                return _signCommand;
            }
        }

        /// <summary>
        /// This command will issue the RequestSigners Command to the DocumentSignatureManager
        /// </summary>
        public static RoutedUICommand RequestSigners
        {
            get
            {
                return _requestSignersCommand;
            }
        }

        /// <summary>
        /// This command will issue the ShowSignatureSummary Command to the DocumentSignatureManager
        /// </summary>
        public static RoutedUICommand ShowSignatureSummary
        {
            get
            {
                return _showSignatureSummaryCommand;
            }
        }

        /// <summary>
        /// This command will invoke the RM Permissions dialog.
        /// </summary>
        public static RoutedUICommand ShowRMPermissions
        {
            get
            {
                return _showRMPermissionsCommand;
            }
        }

        /// <summary>
        /// This command will invoke the RM Credentials manager.
        /// </summary>
        public static RoutedUICommand ShowRMCredentialManager
        {
            get
            {
                return _showRMCredentialManagerCommand;
            }
        }

        /// <summary>
        /// This command will invoke the RM publishing ui.
        /// </summary>
        public static RoutedUICommand ShowRMPublishingUI
        {
            get
            {
                return _showRMPublishingUICommand;
            }
        }

        /// <summary>
        /// The current DocumentApplicationState stored on the DocumentViewer.  Does not necessarily
        /// mean the currently visible state.
        /// </summary>
        public DocumentApplicationState StoredDocumentApplicationState
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
            }
        }

        /// <summary>
        /// The current RightManagementPolicy that is being enforced on the UI.
        /// Used to safely deliver the RightsManagementPolicy to the CommandEnforcer.
        /// </summary>
        public RightsManagementPolicy RightsManagementPolicy
        {
            get
            {
                return _rightsManagementPolicy.Value;
            }
        }

        /// <summary>
        /// Exposes XPSViewer's RootBrowserWindow as an IWin32Window for use in parenting Winforms 
        /// dialogs.
        /// </summary>
        public System.Windows.Forms.IWin32Window RootBrowserWindow
        {
            get
            {
                if (_rootBrowserWindow == null)
                {
                    WindowInteropHelper helper =
                        new WindowInteropHelper(
                            System.Windows.Application.Current.MainWindow);

                    IntPtr handle = helper.Handle;
                    _rootBrowserWindow = new WrapperIWin32Window(handle);
                }

                return _rootBrowserWindow;
            }
        }

        /// <summary>
        /// Exposes the singleton instance of DocumentApplicationDocumentViewer.
        /// </summary>
        public static DocumentApplicationDocumentViewer Instance
        {
            get
            {
                return _singletonInstance;
            }
        }      

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
        #region Public Methods       

        /// <summary>
        /// Used to initialize the UI controls.
        /// If multiple initializations are needed during navigation, etc, this method should be used.
        /// </summary>
        /// <param name="docSigManager">A reference to the DocumentSignatureManager</param>
        public void InitializeUI(DocumentSignatureManager docSigManager, DocumentRightsManagementManager rmManager)
        {
            if (docSigManager == null)
            {
                throw new ArgumentNullException("docSigManager");
            }

            if (rmManager == null)
            {
                throw new ArgumentNullException("rmManager");
            }

            // Set DocumentSignatureManager reference.
            _docSigManager = docSigManager;

            // Setup DigSigInfoBar event handler
            _docSigManager.SignatureStatusChange += new DocumentSignatureManager.SignatureStatusChangeHandler(_digSigInfoBar.OnStatusChange);

            //We disallow all RM-protected actions until the RM Manager tells us otherwise.
            _rightsManagementPolicy.Value = RightsManagementPolicy.AllowNothing;
            CommandEnforcer.Enforce();

            _rmManager = rmManager;

            _rmManager.RMStatusChange += new DocumentRightsManagementManager.RMStatusChangeHandler(_rmInfoBar.OnStatusChange);
            _rmManager.RMStatusChange += new DocumentRightsManagementManager.RMStatusChangeHandler(OnRMStatusChanged);
            _rmManager.RMPolicyChange += new DocumentRightsManagementManager.RMPolicyChangeHandler(OnRMPolicyChanged);

            Invariant.Assert(
                RequiredControlsExist(),
                "DocumentApplicationDocumentViewer must have a valid style.");

            // Set default focus on DocumentViewer
            Focus();
        }

        /// <summary>
        /// Called when the Template's tree has been generated.
        /// </summary>
        /// <remarks>
        /// This method is commonly used to check the status of the visual
        /// tree prior to rendering, so that elements of the tree can be
        /// customized before they are shown.
        /// If a style is changed that affects the visual tree, the
        /// ApplyTemplate method will expand the new visual tree and return
        /// true. Otherwise, it will return false.
        /// When overriding this method, be sure to call the ApplyTemplate
        /// method of the base class.
        /// </remarks>
        /// <returns>
        /// True if the Visual Tree has been created
        /// </returns>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Setup the UI controls event handlers, if needed
            if (RequiredControlsExist() && (!_isUISetup))
            {
                SetupUIControls();
                SetupUITabIndices();
                _isUISetup = true;
            }
        }

        /// <summary>
        /// Handler for the Print command.
        /// </summary>
        protected override void OnPrintCommand()
        {
            if ((RightsManagementPolicy & RightsManagementPolicy.AllowPrint) ==
                RightsManagementPolicy.AllowPrint)
            {
                OnPrintCommandPageRangeOverride();
            }
            else
            {
                throw new InvalidOperationException(
                    SR.Get(SRID.RightsManagementExceptionNoRightsForOperation));
            }
        }
        
        /// <summary>
        /// Handler for the CancelPrint command.
        /// </summary>
        protected override void OnCancelPrintCommand()
        {
#if !DONOTREFPRINTINGASMMETA
            if (_documentWriter != null)
            {
                _documentWriter.CancelAsync();
            }
#endif // DONOTREFPRINTINGASMMETA
        }

        /// <summary>
        /// Called when WritingCompleted event raised by a DocumentWriter (during printing).
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void HandlePrintCompleted(object sender, WritingCompletedEventArgs e)
        {
#if !DONOTREFPRINTINGASMMETA
            if (_documentWriter != null)
            {
                _documentWriter.WritingCompleted -= new WritingCompletedEventHandler(HandlePrintCompleted);
                _documentWriter = null;

                // Since _documentWriter value is used to determine CanExecute state, we must invalidate that state.
                CommandManager.InvalidateRequerySuggested();
            }
#endif // DONOTREFPRINTINGASMMETA
        }

        /// <summary>
        /// Called when WritingCancelled event raised by a DocumentWriter (during printing).
        /// </summary>
        /// <param name="sender">Sender of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void HandlePrintCancelled(object sender, WritingCancelledEventArgs e)
        {
#if !DONOTREFPRINTINGASMMETA
            if (_documentWriter != null)
            {
                _documentWriter.WritingCancelled -= new WritingCancelledEventHandler(HandlePrintCancelled);
                _documentWriter = null;

                // Since _documentWriter value is used to determine CanExecute state, we must invalidate that state.
                CommandManager.InvalidateRequerySuggested();
            }
#endif // DONOTREFPRINTINGASMMETA
        }
        /// <summary>
        /// This is a copy of DocumentViewerBase OnPrintCommand modified to 
        /// handle page ranges
        /// </summary>
        private  void OnPrintCommandPageRangeOverride()
        {
#if !DONOTREFPRINTINGASMMETA
            XpsDocumentWriter docWriter;
            PrintDocumentImageableArea ia = null;
            PageRangeSelection		pageRangeSelection = PageRangeSelection.AllPages;
            PageRange			    pageRange = new PageRange(0);

            // Only one printing job is allowed.
            if (_documentWriter != null)
            {
                return;
            }

            if (Document != null)
            {
                // Show print dialog.
                docWriter = PrintQueue.CreateXpsDocumentWriter(
                    Path.GetFileNameWithoutExtension(DocumentProperties.Current.Filename),
                    ref ia,
                    ref pageRangeSelection,
                    ref pageRange);
                if (docWriter != null && ia != null)
                {
                    // Register for WritingCompleted event.
                    _documentWriter = docWriter;
                    _documentWriter.WritingCompleted += new WritingCompletedEventHandler(HandlePrintCompleted);
                    _documentWriter.WritingCancelled += new WritingCancelledEventHandler(HandlePrintCancelled);

                    // Since _documentWriter value is used to determine CanExecute state, we must invalidate that state.
                    CommandManager.InvalidateRequerySuggested();

                    DocumentPaginator paginator = Document.DocumentPaginator;
                    int pageCount = paginator.PageCount;

                    // Write to the PrintQueue
                    if (pageRangeSelection == PageRangeSelection.UserPages)
                    {
                        if (pageRange.PageFrom < 1)
                        {
                            pageRange.PageFrom = 1;
                        }
                        else if (pageRange.PageFrom > pageCount)
                        {
                            pageRange.PageFrom = 1;
                            pageRange.PageTo = pageCount;
                        }

                        if (pageRange.PageTo < pageRange.PageFrom)
                        {
                            pageRange.PageTo = pageRange.PageFrom;
                        }
                        else if (pageRange.PageTo > pageCount)
                        {
                            pageRange.PageTo = pageCount;
                        }
                    }
                    else
                    {
                        pageRange.PageFrom = 1;
                        pageRange.PageTo = pageCount;
                    }

                    WritePageSelection(docWriter, paginator, pageRange, ia);
                }
            }
#endif // DONOTREFPRINTINGASMMETA
        }

        /// <summary>
        /// This method finds the URI to pages from the document 
        /// specified in the page range.  Deserializes a copy of the pages from
        /// a package stored in the PreloadedPackages and generates a new
        /// doucment with these pages.  This is serialized to the provided
        /// document writer.
        /// </summary>
        private
        void
        WritePageSelection( 
            XpsDocumentWriter docWriter,
            DocumentPaginator docPaginator, 
            System.Windows.Controls.PageRange pageRange,
            PrintDocumentImageableArea ia
            )
        {
            //
            // Create a destination FixedDocument
            //
            FixedDocument dstDoc = new FixedDocument();
            FixedDocumentSequence fds = docPaginator.Source as FixedDocumentSequence;

            if (fds != null)
            {
                //
                // If the source is a FixedDocumentSequence (Should be in Mongoose!), do not use a DocumentPaginator
                // because that unnecessarily loads pages. Instead, enumerate through the references only
                //
                FixedDocument fd = null;

                int skip = pageRange.PageFrom - 1;
                int pageInDoc = 0;
                int docIndex = 0;

                while (skip> 0)
                {
                    if (fd == null)
                    {
                        fd = fds.References[docIndex++].GetDocument(false);
                        pageInDoc = 0;
                        if (fd == null)
                        {
                            break;
                        }
                    }

                    int k = skip;

                    if (k >= fd.Pages.Count)
                    {
                        k = fd.Pages.Count;
                        fd = null;
                    }

                    skip -= k;
                    pageInDoc += k;
                }

                int pages = pageRange.PageTo - pageRange.PageFrom + 1;
                while ( pages > 0 )
                {
                    if (fd == null)
                    {
                        fd = fds.References[docIndex++].GetDocument(false);
                        pageInDoc = 0;
                        if (fd == null)
                        {
                            break;
                        }
                    }

                    if (pageInDoc >= fd.Pages.Count)
                    {
                        fd = null;
                    }
                    else
                    {
                        PageContent pageContent = new PageContent();
                        pageContent.BeginInit();
                        ((IUriContext)pageContent).BaseUri = ((IUriContext)fd.Pages[pageInDoc]).BaseUri;
                        pageContent.Source = fd.Pages[pageInDoc++].Source;
                        pageContent.EndInit();
                        (dstDoc as IAddChild).AddChild(pageContent);

                        pages--;
                    }
                }
            }
            else
            {
                for (int i = pageRange.PageFrom; i <= pageRange.PageTo; i++)
                {
                    DocumentPage page = docPaginator.GetPage(i - 1);
                    IUriContext fixedPage = page.Visual as IUriContext;
                    PageContent pageContent = new PageContent();
                    pageContent.BeginInit();
                    pageContent.Source = fixedPage.BaseUri;
                    pageContent.EndInit();
                    (dstDoc as IAddChild).AddChild(pageContent);
                }
            }

            DocumentPaginator scalingDocument = new ScalingDocument(dstDoc, ia);

            docWriter.WriteAsync(scalingDocument);
        }

        /// <summary>
        /// Called whenever a new document is assigned.
        /// </summary>
        protected override void OnDocumentChanged()
        {
            base.OnDocumentChanged();

            // Whenever the document changes (possibly from navigation) hook up a handler
            // to reset the DocumentViewer to the stored state (usually this will mean the
            // default state)
            DynamicDocumentPaginator paginator = Document.DocumentPaginator as DynamicDocumentPaginator;
            if (paginator != null)
            {
                paginator.PaginationCompleted += new EventHandler(OnPaginationCompleted);

                //If the doc is already paginated, update now.
                if (paginator.IsPageCountValid)
                {
                    OnPaginationCompleted(Document, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Called when ContextMenuOpening is raised on this element.
        /// We use this opportunity to make sure the ScrollViewer's ContextMenu is invoked.  
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnContextMenuOpening(ContextMenuEventArgs e)
        {
            // Raise the ContextMenu command on the ScrollViewer if the ContextMenu
            // was invoked via the Menu key.
            // A negative offset for e.CursorLeft means the user invoked
            // the menu with a hotkey (shift-F10).            
            if (_scrollViewer != null && DoubleUtil.LessThan(e.CursorLeft, 0))
            {                            
                this.ContextMenu = ScrollViewer.ContextMenu;
            }

            base.OnContextMenuOpening(e);
        }

        /// <summary>
        /// Called when ContextMenuClosing is raised on this element.
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnContextMenuClosing(ContextMenuEventArgs e)
        {
            //Reset our ContextMenu back to null.
            this.ContextMenu = null;
            base.OnContextMenuOpening(e);
        }

        /// <summary>
        /// This is the method that responds to the KeyDown event.  It will reclaim the
        /// focus when the user presses escape if one of our buttons is focused.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Focus();
            }

            base.OnKeyDown(e);
        }  

        /// <summary>
        /// Returns the current UI state
        /// </summary>
        /// <returns>The current UI state</returns>
        public DocumentApplicationState GetCurrentState()
        {
            // Store the current state, so that on refresh the data is still available.
            _state = new DocumentApplicationState(Zoom, HorizontalOffset, VerticalOffset, MaxPagesAcross);
            return _state;
        }

        /// <summary>
        /// This will reset the current UI to match that of the StoredDocumentApplicationState
        /// </summary>
        public void SetUIToStoredState()
        {
            // Must be positive
            if (_state.MaxPagesAcross > 0)
            {
                MaxPagesAcross = _state.MaxPagesAcross;
            }

            // Must be positive
            if (_state.Zoom > 0)
            {
                Zoom = _state.Zoom;
            }

            // Invoke a delegate at Background priority so that
            // the Vertical and Horizontal offsets are restored
            // after DocumentViewer has finished laying out the document.
            // (This is not necessary for MaxPagesAcross and Zoom since
            // DocumentViewer will not reset those during layout.)
            Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (DispatcherOperationCallback)delegate(object arg)
                {
                    // Must be non-negative
                    if (_state.VerticalOffset >= 0)
                    {
                        VerticalOffset = _state.VerticalOffset;
                    }

                    // Must be non-negative
                    if (_state.HorizontalOffset >= 0)
                    {
                        HorizontalOffset = _state.HorizontalOffset;
                    }

                    return null;
                }, null);
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Private Events
        //
        //------------------------------------------------------
        /// <summary>
        /// Called when a new custom JournalEntry should be made
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public delegate void JournalEntryHandler(object sender, DocumentApplicationJournalEntryEventArgs args);

        /// <summary>
        /// Fired whenever the current UI should have a custom journal entry added to the
        /// NavWindow's back stack
        /// </summary>
        public event JournalEntryHandler AddJournalEntry;

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //  All of these CLR properties (UI-controls) are guaranteed to exist (non-null) otherwise
        //  the UI will throw upon startup
        //
        //------------------------------------------------------

        #region Private Properties
        /// <summary>
        /// The ToolBar that contains all of the main controls.
        /// </summary>
        private Grid ToolBar
        {
            get
            {
                // find control if the reference is not set.
                if (_toolBar == null)
                {
                  _toolBar = GetTemplateChild(_toolBarName) as Grid;
                }
                return _toolBar;
            }
        }

        /// <summary>
        /// ZoomComboBox used for zoom value selection, and text zoom input
        /// </summary>
        private ZoomComboBox ZoomComboBox
        {
            get
            {
                // find control if the reference is not set.
                if (_zoomComboBox == null)
                {
                    // Find ContentControl to host the ZoomComboBox.
                    ContentControl host = GetTemplateChild("ZoomComboBoxHost") as ContentControl;
                    if (host != null)
                    {
                        // Construct a new ZoomComboBox, and insert it into the host.
                        _zoomComboBox = new ZoomComboBox();

                        // Setup two binds on the Width and Height to ensure these are controlled
                        // by the ContentControl parent.
                        Binding bind = new Binding("Width");
                        bind.Mode = BindingMode.OneWay;
                        bind.Source = host;
                        _zoomComboBox.SetBinding(ContentControl.WidthProperty, bind);
                        bind = new Binding("Height");
                        bind.Mode = BindingMode.OneWay;
                        bind.Source = host;
                        _zoomComboBox.SetBinding(ContentControl.HeightProperty, bind);

                        // Insert the ZoomComboBox into it's host (and thus the ToolBar).
                        host.Content = _zoomComboBox;
                    }
                }
                return _zoomComboBox;
            }
        }

        /// <summary>
        /// Used to move up a page
        /// </summary>
        private Button PageUpButton
        {
            get
            {
                // find control if the reference is not set.
                if (_pageUpButton == null)
                {
                    _pageUpButton = GetTemplateChild(_pageUpButtonName) as Button;
                }
                return _pageUpButton;
            }
        }

        /// <summary>
        /// Used to display, and input page selection
        /// </summary>
        private PageTextBox PageTextBox
        {
            get
            {
                // find control if the reference is not set.
                if (_pageTextBox == null)
                {
                    // Find ContentControl to host the PageTextBox.
                    ContentControl host = GetTemplateChild("PageTextBoxHost") as ContentControl;
                    if (host != null)
                    {
                        // Construct a new PageTextBox, and insert it into the host.
                        _pageTextBox = new PageTextBox();

                        // Setup two binds on the Width and Height to ensure these are controlled
                        // by the ContentControl parent.
                        Binding bind = new Binding("Width");
                        bind.Mode = BindingMode.OneWay;
                        bind.Source = host;
                        _pageTextBox.SetBinding(ContentControl.WidthProperty, bind);
                        bind = new Binding("Height");
                        bind.Mode = BindingMode.OneWay;
                        bind.Source = host;
                        _pageTextBox.SetBinding(ContentControl.HeightProperty, bind);

                        // Insert the PageTextBox into it's host (and thus the ToolBar).
                        host.Content = _pageTextBox;
                    }
                }
                return _pageTextBox;
            }
        }

        /// <summary>
        /// Used to move down a page
        /// </summary>
        private Button PageDownButton
        {
            get
            {
                // find control if the reference is not set.
                if (_pageDownButton == null)
                {
                    _pageDownButton = GetTemplateChild(_pageDownButtonName) as Button;
                }
                return _pageDownButton;
            }
        }
         
        /// <summary>
        /// Used to view document at 100%
        /// </summary>
        private Button ActualSizeButton
        {
            get
            {
                // find control if the reference is not set.
                if (_actualSizeButton == null)
                {
                    _actualSizeButton = GetTemplateChild(_actualSizeButtonName) as Button;
                }
                return _actualSizeButton;
            }
        }
        
        /// <summary>
        /// Used to view document at page width
        /// </summary>
        private Button PageWidthButton
        {
            get
            {
                // find control if the reference is not set.
                if (_pageWidthButton == null)
                {
                    _pageWidthButton = GetTemplateChild(_pageWidthButtonName) as Button;
                }
                return _pageWidthButton;
            }
        }
          
        /// <summary>
        /// Used to view document at 1 whole page at a time
        /// </summary>
        private Button WholePageButton
        {
            get
            {
                // find control if the reference is not set.
                if (_wholePageButton == null)
                {
                    _wholePageButton = GetTemplateChild(_wholePageButtonName) as Button;
                }
                return _wholePageButton;
            }
        }
        
          
        /// <summary>
        /// Used to view document at 2 pages at a time
        /// </summary>
        private Button TwoPageButton
        {
            get
            {
                // find control if the reference is not set.
                if (_twoPageButton == null)
                {
                    _twoPageButton = GetTemplateChild(_twoPageButtonName) as Button;
                }
                return _twoPageButton;
            }
        }
        
        /// <summary>
        /// Used to view document in a 'tiled' or 'thumbnail' view
        /// </summary>
        private Button ThumbnailButton
        {
            get
            {
                // find control if the reference is not set.
                if (_thumbnailButton == null)
                {
                    _thumbnailButton = GetTemplateChild(_thumbnailButtonName) as Button;
                }
                return _thumbnailButton;
            }
        }

        /// <summary>
        /// Used to spawn RightsManagement dialogs
        /// </summary>
        private Button RMButton
        {
            get
            {
                // find control if the reference is not set.
                if (_rmButton == null)
                {
                    _rmButton = GetTemplateChild(_rmButtonName) as Button;
                }
                return _rmButton;
            }
        }
        
        /// <summary>
        /// Used to spawn the Save As dialog
        /// </summary>
        private Button SaveAsButton
        {
            get
            {
                // find control if the reference is not set.
                if (_saveAsButton == null)
                {
                    _saveAsButton = GetTemplateChild(_saveAsButtonName) as Button;
                }
                return _saveAsButton;
            }
        }
        
        /// <summary>
        /// Menu to hold the DigSig options
        /// </summary>
        private MenuItem DigitalSignaturesMenuItem
        {
            get
            {
                // find control if the reference is not set.
                if (_digitalSignaturesMenuItem == null)
                {
                    _digitalSignaturesMenuItem = GetTemplateChild(_digitalSignaturesMenuItemName) as MenuItem;
                }
                return _digitalSignaturesMenuItem;
            }
        }

        /// <summary>
        /// Used to display DigSig and Permission information
        /// </summary>
        private FrameworkElement InfoBar
        {
            get
            {
                // find control if the reference is not set.
                if (_infoBar == null)
                {
                    _infoBar = GetTemplateChild(_infoBarName) as FrameworkElement;
                }
                return _infoBar;
            }
        }

        /// <summary>
        /// Used to close the InfoBar
        /// </summary>
        private Button InfoBarCloseButton
        {
            get
            {
                // find control if the reference is not set.
                if (_infoBarCloseButton == null)
                {
                    // Setup InfoBar Close Button
                    _infoBarCloseButton = GetTemplateChild(_infoBarCloseButtonName)
                            as Button;
                    if (_infoBarCloseButton != null)
                    {
                        _infoBarCloseButton.Click += OnInfoBarCloseClicked;
                    }
                }
                return _infoBarCloseButton;
            }
        }

        /// <summary>
        /// Used to display DigSig information and to launch the 
        /// summary dialog
        /// </summary>
        private Button InfoBarDigSigButton
        {
            get
            {
                // find control if the reference is not set.
                if (_infoBarDigSigButton == null)
                {
                    // Setup Digital Signature Status Changes
                    _infoBarDigSigButton = GetTemplateChild(_infoBarSignaturesButtonName) as Button;
                    if (_infoBarDigSigButton != null)
                    {
                        _infoBarDigSigButton.Command = DocumentApplicationDocumentViewer.ShowSignatureSummary;
                        _infoBarDigSigButton.ApplyTemplate();
                        _digSigInfoBar = new StatusInfoItem(StatusInfoItemType.DigSig, _infoBarDigSigButton, DigitalSignaturesMenuItem);
                        _digSigInfoBar.InfoBarVisibilityChanged += new EventHandler(OnInfoBarVisibilityChanged);
                    }
                }
                return _infoBarDigSigButton;
            }
        }

        /// <summary>
        /// Used to display RM information and to launch the 
        /// a dialog with more information
        /// </summary>
        private Button InfoBarRMButton
        {
            get
            {
                // find control if the reference is not set.
                if (_infoBarRMButton == null)
                {
                    // Setup Rights Management Status Changes
                    _infoBarRMButton = GetTemplateChild(_infoBarRMButtonName) as Button;
                    if (_infoBarRMButton != null)
                    {
                        _infoBarRMButton.Command = DocumentApplicationDocumentViewer.ShowRMPermissions;
                        _infoBarRMButton.ApplyTemplate();
                        _rmInfoBar = new StatusInfoItem(StatusInfoItemType.RM, _infoBarRMButton, RMButton);
                        _rmInfoBar.InfoBarVisibilityChanged += new EventHandler(OnInfoBarVisibilityChanged);
                    }
                }
                return _infoBarRMButton;
            }
        }

        /// <summary>
        /// Gets a reference to the FindToolBar and sets up default properties.
        /// </summary>
        private FindToolBar FindToolBar
        {
            get
            {
                // Locate the FindToolBar to set styling properties (to match main ToolBar).
                if (_findToolBar == null)
                {
                    // Find ContentControl to host the FindToolBar.

                    ContentControl host = GetTemplateChild("PART_FindToolBarHost") as ContentControl;
                    if ((host != null) && ((_findToolBar = host.Content as FindToolBar) != null))
                    {
                        ResourceKey toolBarStyleKey = new ComponentResourceKey(
                            typeof(PresentationUIStyleResources), _toolBarStyleKeyName);
                        _findToolBar.Style = _findToolBar.FindResource(toolBarStyleKey) as Style;
                        _findToolBar.Background = ToolBar.Background;
                    }
                }
                return _findToolBar;
            }
        }

        /// <summary>
        /// Gets a reference to the ScrollViewer used to scroll the document content.
        /// </summary>
        private ScrollViewer ScrollViewer
        {
            get
            {
                if (_scrollViewer == null)
                {
                    _scrollViewer = GetTemplateChild(_contentHostName) as ScrollViewer;
                }
                return _scrollViewer;
            }
        }

        /// <summary>
        /// The current command enforcer.
        /// </summary>
        private CommandEnforcer CommandEnforcer
        {
            get
            {
                return _commandEnforcer.Value;
            }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods
        /// <summary>
        /// Used to setup the properties and behaviors of controls
        /// </summary>
        private void SetupUIControls()
        {
            // Attach handler to RMButton
            RMButton.Command = DocumentApplicationDocumentViewer.ShowRMPermissions;

            // Setup the DigitalSignaturesMenu
            // Add the Sign MenuItem
            MenuItem menuItem = new MenuItem();
            menuItem.Name = _digSigSignMenuItemName;
            menuItem.Command = DocumentApplicationDocumentViewer.Sign;
            DigitalSignaturesMenuItem.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Name = _digSigRequestSignersMenuItemName;
            menuItem.Command = DocumentApplicationDocumentViewer.RequestSigners;
            DigitalSignaturesMenuItem.Items.Add(menuItem);

            menuItem = new MenuItem();
            menuItem.Name = _digSigShowSignatureSummaryMenuItemName;
            menuItem.Command = DocumentApplicationDocumentViewer.ShowSignatureSummary;
            DigitalSignaturesMenuItem.Items.Add(menuItem);

            // Properly handle Esc from menus
            DigitalSignaturesMenuItem.PreviewKeyDown += MenuPreviewKeyDown;
            FindToolBar.OptionsMenuItem.PreviewKeyDown += MenuPreviewKeyDown;

            // Setup ZoomComboBox
            // Bind Text to ZoomPercentage
            Binding bind = new Binding("Zoom");
            bind.Mode = BindingMode.OneWay;
            bind.Source = this;
            ZoomComboBox.SetBinding(ZoomComboBox.ZoomProperty, bind);
            // Attach ZoomComboBox event handlers
            ZoomComboBox.LostKeyboardFocus += new KeyboardFocusChangedEventHandler(OnZoomComboBoxLostFocus);
            ZoomComboBox.SelectionChanged += new SelectionChangedEventHandler(OnZoomComboBoxSelectionChanged);
            ZoomComboBox.ZoomValueEdited += new EventHandler(OnZoomComboBoxValueEdited);
            ZoomComboBox.ZoomValueEditCancelled += new EventHandler(OnZoomComboBoxEditCancelled);
            // Set default ZoomComboBox properties
            ZoomComboBox.Name = _zoomComboBoxName;
            // Fill the ZoomComboBox items
            PopulateZoomComboBoxItems();

            bind = new Binding("MasterPageNumber");
            bind.Mode = BindingMode.OneWay;
            bind.Source = this;
            PageTextBox.SetBinding(PageTextBox.PageNumberProperty, bind);
            // Attach PageTextBox event handlers
            PageTextBox.LostKeyboardFocus += new KeyboardFocusChangedEventHandler(OnPageTextBoxLostFocus);
            PageTextBox.PageNumberEdited += new EventHandler(OnPageTextBoxValueEdited);
            PageTextBox.PageNumberEditCancelled += new EventHandler(OnPageTextBoxEditCancelled);
            PageTextBox.Name = _pageTextBoxName;
        }

        /// <summary>
        /// Sets the TabIndex explicitly for all controls in the
        /// UI where we would want the focus to stop.
        /// </summary>
        private void SetupUITabIndices()
        {
            int tabIndex = 1;

            // Initial tab stop
            this.TabIndex                           = tabIndex++;

            // Top Toolbar
            SaveAsButton.TabIndex                   = tabIndex++;
            RMButton.TabIndex                       = tabIndex++;
            DigitalSignaturesMenuItem.TabIndex      = tabIndex++;

            // Find Toolbar
            FindToolBar.FindTextBox.TabIndex        = tabIndex++;
            FindToolBar.FindPreviousButton.TabIndex = tabIndex++;
            FindToolBar.FindNextButton.TabIndex     = tabIndex++;
            FindToolBar.OptionsMenuItem.TabIndex    = tabIndex++;

            // InfoBar
            InfoBarRMButton.TabIndex                = tabIndex++;
            InfoBarDigSigButton.TabIndex            = tabIndex++;
            InfoBarCloseButton.TabIndex             = tabIndex++;

            // Bottom Toolbar region
            PageTextBox.TabIndex                    = tabIndex++;
            PageUpButton.TabIndex                   = tabIndex++;
            PageDownButton.TabIndex                 = tabIndex++;
            ActualSizeButton.TabIndex               = tabIndex++;
            PageWidthButton.TabIndex                = tabIndex++;
            WholePageButton.TabIndex                = tabIndex++;
            TwoPageButton.TabIndex                  = tabIndex++;
            ThumbnailButton.TabIndex                = tabIndex++;
            ZoomComboBox.TabIndex                   = tabIndex++;
        }

        /// <summary>
        /// Verify that all UI controls exist.  Since the ZoomComboBox uses properties of ZoomInButton
        /// to find the correct location to inject the control into the ToolBar, the ZoomInButton must
        /// be validated prior to the ZoomComboBox.
        /// </summary>
        /// <returns></returns>
        private bool RequiredControlsExist()
        {
            return ((ToolBar != null) &&
                 (SaveAsButton != null) &&
                 (RMButton != null) &&
                 (DigitalSignaturesMenuItem != null) &&
                 (FindToolBar != null) &&
                 (InfoBar != null) &&
                 (PageTextBox != null) &&
                 (PageUpButton != null) &&
                 (PageDownButton != null) &&
                 (ActualSizeButton != null) &&
                 (PageWidthButton != null) &&
                 (WholePageButton != null) &&
                 (TwoPageButton != null) &&
                 (ThumbnailButton != null) &&
                 (ZoomComboBox != null) &&
                 (ScrollViewer != null)
                 );
        }

        /// <summary>
        /// Sets the visibility of the element passed in.
        /// </summary>
        /// <param name="uie">The element whose visibility will be changed</param>
        private void ChangeControlVisibility(UIElement uie)
        {
            if (uie != null)
            {
                ChangeControlVisibility(uie, uie.Visibility == Visibility.Collapsed);
            }
        }

        /// <summary>
        /// Sets the visibility of the element passed in.
        /// </summary>
        /// <param name="uie">The element whose visibility will be changed</param>
        /// <param name="visibility">True if uie should be set visible, false for collapsed</param>
        private void ChangeControlVisibility(UIElement uie, bool visibility)
        {
            if (uie != null)
            {
                if (visibility)
                {
                    uie.Visibility = Visibility.Visible;
                }
                else
                {
                    uie.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Called when the user presses a key in the digital signatures menu or find toolbar
        /// If the user presses Esc this replaces the normal handling to focus on the document.
        /// This will only be done if the event was fired directly to the menu header and the
        /// menu is not currently open.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Argument to the event</param>
        private void MenuPreviewKeyDown(object sender, KeyEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null && menuItem.Role == MenuItemRole.TopLevelHeader &&
                !menuItem.IsSubmenuOpen && e.Key == Key.Escape)
            {
                Focus();
                e.Handled = true;
            }

        }

        /// <summary>
        /// Called whenever the visibility of either the DigSig, or RM InfoBar buttons changes.
        /// If both are collapsed, this will hide the entire InfoBar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnInfoBarVisibilityChanged(object sender, EventArgs e)
        {
            if ((_digSigInfoBar.Visibility == Visibility.Collapsed) &&
                (_rmInfoBar.Visibility == Visibility.Collapsed))
            {
                ChangeControlVisibility(InfoBar, false);
            }
            else
            {
                ChangeControlVisibility(InfoBar, true);
            }
        }

        /// <summary>
        /// When the 'Close-Arrow' button is pressed, this will cause the InfoBar to Collapse
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnInfoBarCloseClicked(object sender, EventArgs e)
        {
            _digSigInfoBar.Visibility = Visibility.Collapsed;
            _rmInfoBar.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Called when any ApplicationCommand has been fired.  Will perform the
        /// appropriate action depending on the control that was clicked.
        /// </summary>
        /// <param name="sender">The document which hosts the command</param>
        /// <param name="e">Event arguments</param>
        private static void OnApplicationCommandExecute(object sender, ExecutedRoutedEventArgs e)
        {
            // Ensure parameters are valid, and DocumentManager is set.
            DocumentManager docManager = DocumentManager.CreateDefault();

            Invariant.Assert(
                docManager != null,
                "Required DocumentManager instance is not available.");

            // Check if arguments are valid, fail silently otherwise.
            if ((e != null) && (e.Command != null))
            {
                // Check which command was executed, and act appropriately
                if (e.Command.Equals(ApplicationCommands.Save))
                {
                    Trace.SafeWrite(Trace.File, "Save ApplicationCommand fired.");
                    if (docManager.CanSave)
                    {
                        // Save document.
                        docManager.Save(null);
                    }
                    else
                    {
                        docManager.SaveAs(null);
                    }
                }
                else if (e.Command.Equals(ApplicationCommands.SaveAs))
                {
                    Trace.SafeWrite(Trace.File, "SaveAs ApplicationCommand fired.");
                    // SaveAs is always available, so execute when requested.
                    docManager.SaveAs(null);
                }
                else if (e.Command.Equals(ApplicationCommands.Properties))
                {
                    Trace.SafeWrite(Trace.Presentation, "Properties ApplicationCommand fired.");
                    // Launch dialog here.
                    DocumentProperties.Current.ShowDialog();
                }
                else if (e.Command.Equals(DocumentApplicationDocumentViewer.FocusToolBar))
                {
                    Trace.SafeWrite(Trace.Presentation, "FocusToolBar command fired.");

                    // Ensure that the sender is a DocumentApplicationDocumentViewer
                    DocumentApplicationDocumentViewer docViewer = sender as DocumentApplicationDocumentViewer;
                    Invariant.Assert(docViewer != null, "Sender must be a valid DocumentApplicationDocumentViewer");

                    // Handle command so that it doesn't bubble up to DocumentViewer
                    e.Handled = true;

                    // Focus the save as button since it is the first control.
                    docViewer.SaveAsButton.Focus();
                }
            }
        }

        /// <summary>
        /// Central handler for QueryEnabled events fired by Commands directed at DocumentViewer.       
        /// </summary>
        /// <param name="target">The target of this Command, expected to be DocumentViewer</param>
        /// <param name="args">The event arguments for this event.</param>
        private static void OnApplicationCommandQuery(object target, CanExecuteRoutedEventArgs e)
        {
            
            // Check if arguments are valid, fail silently otherwise.
            if ((e != null) && (e.Command != null))
            {

                // Check which command was executed, and act appropriately
                if (e.Command.Equals(ApplicationCommands.Save))
                {
                    e.CanExecute = false;

                    // Check DocumentManager to see if we can save package
                    DocumentManager documentManager = DocumentManager.CreateDefault();
                    if (documentManager != null)
                    {
                        e.CanExecute = documentManager.CanSave && documentManager.IsModified;
                    }
                    Trace.SafeWrite(
                        Trace.File,
                        "Save ApplicationCommand queried result is {0}.",
                        e.CanExecute);
                }
                else if (e.Command.Equals(ApplicationCommands.SaveAs))
                {
                    e.CanExecute = true;
                    Trace.SafeWrite(
                        Trace.File,
                        "SaveAs ApplicationCommand queried result is {0}.",
                        e.CanExecute);
                }
                else if (e.Command.Equals(ApplicationCommands.Properties))
                {
                    e.CanExecute = true;
                    Trace.SafeWrite(
                        Trace.Presentation,
                        "Properties ApplicationCommand queried result is {0}.",
                        e.CanExecute);
                }
                else if (e.Command.Equals(DocumentApplicationDocumentViewer.FocusToolBar))
                {
                    e.CanExecute = true;
                    Trace.SafeWrite(
                        Trace.Presentation,
                        "FocusToolBar ApplicationCommand queried result is {0}.",
                        e.CanExecute);
                }
            }
        }

        /// <summary>
        /// Called when any NavigationCommand has been fired.  Will perform the
        /// appropriate action depending on the control that was clicked.
        /// </summary>
        /// <param name="sender">The document which hosts the command</param>
        /// <param name="e">Event arguments</param>
        private static void OnNavigationCommandExecute(object sender, ExecutedRoutedEventArgs e)
        {
            DocumentApplicationDocumentViewer dv = sender as DocumentApplicationDocumentViewer;

            if ((e != null) && (dv != null) && (e.Command != null))
            {
                // If FirstPage or LastPage were executed, add a journal entry
                if (e.Command.Equals(NavigationCommands.FirstPage))
                {
                    dv.FireJournalEntryEvent();
                    dv.OnFirstPageCommand();
                }
                else if (e.Command.Equals(NavigationCommands.LastPage))
                {
                    dv.FireJournalEntryEvent();
                    dv.OnLastPageCommand();
                }
            }
        }

        /// <summary>
        /// Central handler for QueryEnabled events fired by NavigationCommands directed
        /// at DocumentViewer.
        /// </summary>
        /// <param name="sender">The document which hosts the command</param>
        /// <param name="e">Event arguments</param>
        private static void OnNavigationCommandQuery(object sender, CanExecuteRoutedEventArgs e)
        {
            DocumentApplicationDocumentViewer dv = sender as DocumentApplicationDocumentViewer;

            if ((e != null) && (dv != null) && (e.Command != null))
            {
                // Check if the commands are allowed
                if (e.Command.Equals(NavigationCommands.FirstPage))
                {
                    e.CanExecute = dv.CanGoToPreviousPage;
                }
                else if (e.Command.Equals(NavigationCommands.LastPage))
                {
                    e.CanExecute = dv.CanGoToNextPage;
                }
            }
        }

        /// <summary>
        /// Called when any DigSig command has been fired.  Will perform the
        /// appropriate action depending on the control that was clicked.
        /// </summary>
        /// <param name="sender">The document which hosts the command</param>
        /// <param name="e">Event arguments</param>
        private static void OnDigSigExecute(object sender, ExecutedRoutedEventArgs e)
        {
            DocumentApplicationDocumentViewer dv = sender as DocumentApplicationDocumentViewer;

            // Ensure parameters are valid, and DocumentSignatureManager is set.
            // This method is setup to fail silently if called inappropriately.
            if ((e != null) && (dv != null) && (dv._docSigManager != null))
            {
                // Check which command was executed, and act appropriately
                if (e.Command.Equals(DocumentApplicationDocumentViewer.Sign))
                {
                    dv._docSigManager.ShowSigningDialog();
                }
                else if (e.Command.Equals(DocumentApplicationDocumentViewer.RequestSigners))
                {
                    dv._docSigManager.ShowSignatureRequestSummaryDialog();
                }
                else if (e.Command.Equals(DocumentApplicationDocumentViewer.ShowSignatureSummary))
                {
                    dv._docSigManager.ShowSignatureSummaryDialog();
                }
            }
        }

        /// <summary>
        /// Called when any DigSig command is queried for an IsEnabled value.
        /// </summary>
        /// <param name="sender">The document which hosts the command</param>
        /// <param name="e">Event arguments</param>
        private static void OnDigSigQuery(object sender, CanExecuteRoutedEventArgs e)
        {
            DocumentApplicationDocumentViewer dv = sender as DocumentApplicationDocumentViewer;

            // Ensure parameters are valid, and DocumentSignatureManager is set.
            // This method is setup to fail silently if called inappropriately.
            if ((e != null) && (dv != null) && (dv._docSigManager != null))
            {
                // Check which command was executed, and act appropriately:
                // 1) The Sign command can always be executed.
                // 2) The Request Signers command can only be executed when the document
                //    is not signed
                // 3) The Show Signatures Summary command can only be executed when the
                //    document is either signed or has signature requests
                // 4) No other commands should be executed

                if (e.Command.Equals(DocumentApplicationDocumentViewer.Sign))
                {
                    e.CanExecute = true;
                }
                else if (e.Command.Equals(DocumentApplicationDocumentViewer.RequestSigners))
                {
                    e.CanExecute = !dv._docSigManager.IsSigned;
                }
                else if (e.Command.Equals(DocumentApplicationDocumentViewer.ShowSignatureSummary))
                {
                    e.CanExecute = dv._docSigManager.IsSigned || dv._docSigManager.HasRequests;
                }
                else
                {
                    e.CanExecute = false;
                }
            }
        }

        /// <summary>
        /// Will fire an AddJournalEntry event with the current state, when needed.
        /// </summary>
        private void FireJournalEntryEvent()
        {
            AddJournalEntry(this, new DocumentApplicationJournalEntryEventArgs(GetCurrentState()));
        }

        /// <summary>
        /// Called if a document has finished paginating, to ensure the UI state is accurate.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnPaginationCompleted(object sender, EventArgs args)
        {
            // this will change the current state to match the StoredState.
            SetUIToStoredState();
        }

        #region Rights Management

        /// <summary>
        /// Called when any RM command has been fired.  Will perform the
        /// appropriate action depending on the control that was clicked.
        /// </summary>
        /// <param name="sender">The document which hosts the command</param>
        /// <param name="e">Event arguments</param>
        private static void OnRMExecute(object sender, ExecutedRoutedEventArgs e)
        {
            DocumentApplicationDocumentViewer dv = sender as DocumentApplicationDocumentViewer;

            // Ensure parameters are valid, and DocumentRightsManagementManager is set.
            // This method is setup to fail silently if called inappropriately.
            if ((e != null) && (dv != null) && (dv._rmManager != null))
            {
                // Verify that the RM Client is installed.
                if (dv._rmManager.IsRMInstalled)
                {
                    // Check which command was executed, and act appropriately
                    if (e.Command.Equals(DocumentApplicationDocumentViewer.ShowRMPermissions))
                    {
                        dv._rmManager.ShowPermissions();
                    }
                    else if (e.Command.Equals(DocumentApplicationDocumentViewer.ShowRMCredentialManager))
                    {
                        dv._rmManager.ShowCredentialManagementUI();
                    }
                    else if (e.Command.Equals(DocumentApplicationDocumentViewer.ShowRMPublishingUI))
                    {
                        dv._rmManager.ShowPublishing();
                    }
                }
                else
                {
                    dv._rmManager.PromptToInstallRM();
                }
            }
        }

        /// <summary>
        /// Called when any RM command is queried for an IsEnabled value.
        /// </summary>
        /// <param name="sender">The document which hosts the command</param>
        /// <param name="e">Event arguments</param>
        private static void OnRMQuery(object sender, CanExecuteRoutedEventArgs e)
        {
            DocumentApplicationDocumentViewer dv = sender as DocumentApplicationDocumentViewer;
            // Ensure parameters are valid.
            // This method is setup to fail silently if called inappropriately.
            if ((e != null) && (dv != null))
            {
                // Check which command was executed, and act appropriately

                bool isProtected = (dv._rightsManagementStatus == RightsManagementStatus.Protected);

                if (e.Command.Equals(DocumentApplicationDocumentViewer.ShowRMCredentialManager))
                {
                    e.CanExecute = true;
                }
                else if (e.Command.Equals(DocumentApplicationDocumentViewer.ShowRMPermissions))
                {
                    // Enable the ShowRMPermissions dialog anytime the document is already protected.
                    // This is okay even if you're the owner because the RightsManagementManager
                    // makes the actual decision over which dialog is shown.
                    e.CanExecute = isProtected;
                }
                else if (e.Command.Equals(DocumentApplicationDocumentViewer.ShowRMPublishingUI))
                {
                    // Enable the ShowRMPublishing dialog whenever the dialog is not protected.
                    e.CanExecute = !isProtected;
                }
                else
                {
                    e.CanExecute = false;
                }
            }
        }

        /// <summary>
        /// Handles the RMPolicyChanged event fired by the RMManager.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnRMPolicyChanged(object sender, DocumentRightsManagementManager.RightsManagementPolicyEventArgs args)
        {
            if (args != null)
            {
                //Invoke the CommandEnforcer to enable/disable commands as appropriate.
                _rightsManagementPolicy.Value = args.RMPolicy;
                CommandEnforcer.Enforce();
            }
            else
            {
                throw new ArgumentNullException("args");
            }
        }

        /// <summary>
        /// Used to be notified of possible RM status changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnRMStatusChanged(object sender, DocumentRightsManagementManager.RightsManagementStatusEventArgs args)
        {
            if (args != null)
            {
                _rightsManagementStatus = args.RMStatus;
                if (_rightsManagementStatus == RightsManagementStatus.Protected)
                {
                    RMButton.Command = DocumentApplicationDocumentViewer.ShowRMPermissions;
                }
                else
                {
                    RMButton.Command = DocumentApplicationDocumentViewer.ShowRMPublishingUI;
                }
            }
        }

        #endregion Rights Management

        #region ZoomComboBox
        /// <summary>
        /// Called when ZoomComboBox loses focus.  Will reset the Text value.
        /// </summary>
        /// <param name="sender">The ZoomComboBox to change.</param>
        /// <param name="e">Event args</param>
        private void OnZoomComboBoxLostFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender == ZoomComboBox)
            {
                // Set the default zoom value, as we've lost focus
                SetZoomComboBoxValue();
                // Ensure that when the focus is lost no text is selected.
                ZoomComboBox.TextBox.Select(0, 0);
            }
        }

        /// <summary>
        /// Used to apply selection when selected from list.
        /// </summary>
        /// <param name="sender">The ZoomComboBox to change.</param>
        /// <param name="e">Event args</param>
        private void OnZoomComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == ZoomComboBox)
            {
                // We don't want to respond to selections generated by the user going through
                // the dropdown list with the keyboard.
                if (ZoomComboBox.ProcessSelections)
                {
                    ComboBoxItem cbItem = ZoomComboBox.SelectedItem as ComboBoxItem;
                    // Check if a selection is made (when an item is selected from the list the
                    // SelectedItem will be non-null)                
                    if (cbItem != null &&
                        cbItem.Content is ZoomComboBoxItem)
                    {
                        ZoomComboBoxItem item = (ZoomComboBoxItem)cbItem.Content;

                        // Check type of SelectedItem
                        switch (item.Type)
                        {
                            // Zoom selected
                            case ZoomComboBoxItemType.Zoom:
                                Zoom = item.Parameter;
                                break;
                            // PageWidth selected
                            case ZoomComboBoxItemType.PageWidth:
                                FitToWidth();
                                break;
                            // WholePage or TwoPages selected
                            case ZoomComboBoxItemType.AdjacentPages:
                                FitToMaxPagesAcross((int)item.Parameter);
                                break;
                            // Thumbnails selected
                            case ZoomComboBoxItemType.Thumbnails:
                                ViewThumbnails();
                                break;

                        }
                    }

                    // Reset selection
                    ZoomComboBox.SelectedIndex = -1;
                    SetZoomComboBoxValue();
                }
                e.Handled = true;
            }
        }

        /// <summary>
        /// Called when a zoom value has been entered in the textbox.
        /// </summary>
        /// <param name="sender">The ZoomComboBox to change.</param>
        /// <param name="e">Event args</param>
        private void OnZoomComboBoxValueEdited(object sender, EventArgs e)
        {
            double result;
            // Parse the new value.
            if (StringToZoomValue(ZoomComboBox.Text, out result))
            {
                // The value is valid, set on DocumentViewer
                Zoom = result;
            }
            // Set the text shown in the ZoomComboBox to match the current Zoom value.
            // This is necessary when an invalid value is entered or the user has
            // typed a valid value that doesn't change the Zoom.
            SetZoomComboBoxValue();
        }

        /// <summary>
        /// Called when a zoom value edit has been cancelled.
        /// </summary>
        /// <param name="sender">The ZoomComboBox to change.</param>
        /// <param name="e">Event args</param>
        private void OnZoomComboBoxEditCancelled(object sender, EventArgs e)
        {
            // Editing has been cancelled, reset the text
            SetZoomComboBoxValue();
        }

        /// <summary>
        /// Used to reset the ZoomComboBox text to the current DocumentViewer zoom value.
        /// </summary>
        private void SetZoomComboBoxValue()
        {
            // Set the current Zoom value on the ZoomComboBox
            ZoomComboBox.SetZoom(Zoom);
        }

        /// <summary>
        /// Fill the ZoomComboBox with items.
        /// </summary>
        private void PopulateZoomComboBoxItems()
        {
            AddZoomComboBoxItem(
                new ZoomComboBoxItem(
                    ZoomComboBoxItemType.Zoom,
                    SR.Get(SRID.ZoomComboBoxItem400),
                    400.0),
                "Zoom400");
            AddZoomComboBoxItem(
                new ZoomComboBoxItem(
                    ZoomComboBoxItemType.Zoom,
                    SR.Get(SRID.ZoomComboBoxItem250),
                    250.0),
                "Zoom250");
            AddZoomComboBoxItem(
                new ZoomComboBoxItem(
                    ZoomComboBoxItemType.Zoom,
                    SR.Get(SRID.ZoomComboBoxItem150),
                    150.0),
                "Zoom150");
            AddZoomComboBoxItem(
                new ZoomComboBoxItem(
                    ZoomComboBoxItemType.Zoom,
                    SR.Get(SRID.ZoomComboBoxItem100),
                    100.0),
                "Zoom100");
            AddZoomComboBoxItem(
                new ZoomComboBoxItem(
                    ZoomComboBoxItemType.Zoom,
                    SR.Get(SRID.ZoomComboBoxItem75),
                    75.0),
                "Zoom75");
            AddZoomComboBoxItem(
                new ZoomComboBoxItem(
                    ZoomComboBoxItemType.Zoom,
                    SR.Get(SRID.ZoomComboBoxItem50),
                    50.0),
                "Zoom50");
            AddZoomComboBoxItem(
                new ZoomComboBoxItem(
                    ZoomComboBoxItemType.Zoom,
                    SR.Get(SRID.ZoomComboBoxItem25),
                    25.0),
                "Zoom25");
        }

        /// <summary>
        /// Wraps a ZoomComboBoxItem in a ComboBoxItem, sets the name (AutomationId) on the
        /// ComboBoxItem, and places it into the ZoomComboBox.
        /// </summary>
        /// <param name="zoomItem">New Item to add.</param>
        /// <param name="name">Desired AutomationId</param>
        private void AddZoomComboBoxItem(ZoomComboBoxItem zoomItem, String name)
        {
            // Create new ComboBox Item
            ComboBoxItem newItem = new ComboBoxItem();

            // Assign Content and Name
            newItem.Content = zoomItem;
            newItem.Name = String.IsNullOrEmpty(name) ? String.Empty : name;

            // Right align the content to match the TextBox portion of the ComboBox.
            newItem.HorizontalAlignment = HorizontalAlignment.Right;

            // Add item to ZoomComboBox
            ZoomComboBox.Items.Add(newItem);
        }

        private static bool StringToZoomValue(string zoomString, out double zoomValue)
        {
            bool isValidArg = false;
            zoomValue = 0.0;

            CultureInfo culture = CultureInfo.CurrentCulture;

            // If this fails return false;
            try
            {
                // Remove whitespace on either end of the string.
                if ((culture != null) && !String.IsNullOrEmpty(zoomString))
                {
                    zoomString = zoomString.Trim();

                    // If this is not a neutral culture attempt to remove the percent symbol.
                    if ((!culture.IsNeutralCulture) && (zoomString.Length > 0))
                    {
                        // This will strip the percent sign (if it exists) depending on the culture information.
                        switch (culture.NumberFormat.PercentPositivePattern)
                        {
                            case 0: // n %
                            case 1: // n%
                                // Remove the last character if it is a percent sign
                                if (zoomString.Length - 1 == zoomString.LastIndexOf(
                                                                culture.NumberFormat.PercentSymbol,
                                                                StringComparison.CurrentCultureIgnoreCase))
                                {
                                    zoomString = zoomString.Substring(0, zoomString.Length - 1);
                                }
                                break;
                            case 2: // %n
                            case 3: // % n
                                // Remove the first character if it is a percent sign.
                                if (0 == zoomString.IndexOf(
                                            culture.NumberFormat.PercentSymbol,
                                            StringComparison.CurrentCultureIgnoreCase))
                                {
                                    zoomString = zoomString.Substring(1);
                                }
                                break;
                        }
                    }

                    // If this conversion throws then the string wasn't a valid zoom value.
                    zoomValue = System.Convert.ToDouble(zoomString, culture);
                    isValidArg = true;
                }
            }
            // Allow empty catch statements.
#pragma warning disable 56502

            // Catch only the expected parse exceptions
            catch (ArgumentOutOfRangeException) { }
            catch (ArgumentNullException) { }
            catch (FormatException) { }
            catch (OverflowException) { }

            // Disallow empty catch statements.
#pragma warning restore 56502

            return isValidArg;
        }
        #endregion ZoomComboBox

        #region PageTextBox
        /// <summary>
        /// Called when PageTextBox loses focus.  Will reset the Text value.
        /// </summary>
        /// <param name="sender">The PageTextBox to change.</param>
        /// <param name="e">Event args</param>
        private void OnPageTextBoxLostFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender == PageTextBox)
            {
                // Set the default page value, as we've lost focus
                SetPageTextBoxValue();
                // Ensure that when the focus is lost no text is selected.
                PageTextBox.Select(0, 0);
            }
        }

        /// <summary>
        /// Called when a page value has been entered in the textbox.
        /// </summary>
        /// <param name="sender">The PageTextBox to change.</param>
        /// <param name="e">Event args</param>
        private void OnPageTextBoxValueEdited(object sender, EventArgs e)
        {
            int pageNumber = ParsePageNumber(PageTextBox.Text);
            // Check that page number is within range
            if ((pageNumber > 0) && (pageNumber <= PageCount))
            {
                FireJournalEntryEvent();
                NavigationCommands.GoToPage.Execute(pageNumber, this);
            }
            // Set the textbox to reflect the current page and select the number.
            // Reselecting the number allows the user to rapidly go between pages by
            // typing one number after another.  If the user entered an invalid
            // number this will simply restore the previous value.  Otherwise,
            // it will set the value to the first page on screen, which may not
            // be exactly the same as the value entered if there are multiple
            // pages on screen.
            SetPageTextBoxValue();
        }

        /// <summary>
        /// Called when a page value edit has been cancelled.
        /// </summary>
        /// <param name="sender">The PageTextBox to change.</param>
        /// <param name="e">Event args</param>
        private void OnPageTextBoxEditCancelled(object sender, EventArgs e)
        {
            // Editing has been cancelled, reset the text
            SetPageTextBoxValue();
        }

        /// <summary>
        /// Used to reset the PageTextBox text to the current DocumentViewer zoom value.
        /// </summary>
        private void SetPageTextBoxValue()
        {
            // Set the current Page value on the PageTextBox
            PageTextBox.SetPageNumber(MasterPageNumber);
        }

        /// <summary>
        /// Parses a string for a page number.
        /// </summary>
        /// <param name="pageNumberString">A string representing an int pagenumber</param>
        /// <returns>An int represented by the string, or -1 on a parse error</returns>
        public int ParsePageNumber(string pageNumberString)
        {
            CultureInfo culture = CultureInfo.CurrentCulture;

            // If this fails return _invalidPageNumber;
            try
            {
                // Remove whitespace on either end of the string.
                if ((culture != null) && !String.IsNullOrEmpty(pageNumberString))
                {
                    pageNumberString = pageNumberString.Trim();

                    // If this conversion throws then the string wasn't a valid page number value.
                    return int.Parse(pageNumberString, culture);
                }
            }
// Allow empty catch statements.
#pragma warning disable 56502

            // Catch only the expected parse exceptions
            catch (ArgumentNullException) { }
            catch (FormatException) { }
            catch (OverflowException) { }

// Disallow empty catch statements.
#pragma warning restore 56502

            return _invalidPageNumber;
        }
        #endregion PageTextBox

        #region Commands
        /// <summary>
        /// Set up our command bindings
        /// </summary>
        private static void CreateCommandBindings()
        {
            // Setup the DigSig command bindings.
            ExecutedRoutedEventHandler executeHandler = new ExecutedRoutedEventHandler(OnDigSigExecute);
            CanExecuteRoutedEventHandler queryEnabledHandler = new CanExecuteRoutedEventHandler(OnDigSigQuery);

            //
            // Command: Sign
            //          Tells DocumentApplicationDocumentViewer to fire the Sign DigSig operation
            _signCommand = CreateAndBindCommand("Sign",
                SR.Get(SRID.DocumentApplicationDocumentViewerSignCommand), null,
                executeHandler, queryEnabledHandler);

            //
            // Command: RequestSigners
            //          Tells DocumentApplicationDocumentViewer to fire the RequestSigners DigSig operation
            _requestSignersCommand = CreateAndBindCommand("RequestSigners",
                SR.Get(SRID.DocumentApplicationDocumentViewerRequestSignersCommand), null,
                executeHandler, queryEnabledHandler);

            //
            // Command: ShowSignatureSummary
            //          Tells DocumentApplicationDocumentViewer to fire the ShowSignatureSummary DigSig operation
            _showSignatureSummaryCommand = CreateAndBindCommand("ShowSignatureSummary",
                SR.Get(SRID.DocumentApplicationDocumentViewerShowSignatureSummaryCommand), null,
                executeHandler, queryEnabledHandler);

            // Setup the RM command bindings.
            executeHandler = new ExecutedRoutedEventHandler(OnRMExecute);
            queryEnabledHandler = new CanExecuteRoutedEventHandler(OnRMQuery);

            //
            // Command: ShowRMPermissionsSummary
            //          Tells DocumentApplicationDocumentViewer to fire the ShowRMPermissions operation
            _showRMPermissionsCommand = CreateAndBindCommand("ShowRMPermissions",
                SR.Get(SRID.DocumentApplicationDocumentViewerShowRMPermissionsCommand), null,
                executeHandler, queryEnabledHandler);

            //
            // Command: ShowRMCredentialManager
            //          Tells DocumentApplicationDocumentViewer to fire the ShowRMCredentialManager operation
            _showRMCredentialManagerCommand = CreateAndBindCommand("ShowRMCredentialManager",
                SR.Get(SRID.DocumentApplicationDocumentViewerShowRMCredentialManagerCommand), null,
                executeHandler, queryEnabledHandler);

            //
            // Command: ShowRMPublishingUI
            //          Tells DocumentApplicationDocumentViewer to fire the ShowRMPublishingUI operation
            _showRMPublishingUICommand = CreateAndBindCommand("ShowRMPublishingUI",
                SR.Get(SRID.DocumentApplicationDocumentViewerShowRMPublishingUICommand), null,
                executeHandler, queryEnabledHandler);

            // Setup the Save bindings.
            executeHandler = new ExecutedRoutedEventHandler(OnApplicationCommandExecute);
            queryEnabledHandler = new CanExecuteRoutedEventHandler(OnApplicationCommandQuery);

            //
            // Command: ApplicationCommands.Save
            //          Tells DocumentApplicationDocumentViewer to fire the ApplicationCommands.Save operation
            BindCommand(ApplicationCommands.Save, executeHandler, queryEnabledHandler);

            //
            // Command: ApplicationCommands.SaveAs
            //          Tells DocumentApplicationDocumentViewer to fire the ApplicationCommands.SaveAs operation
            BindCommand(ApplicationCommands.SaveAs, executeHandler, queryEnabledHandler);

            //
            // Command: ApplicationCommands.Properties
            //          Tells DocumentApplicationDocumentViewer to fire the ApplicationCommands.Properties operation
            BindCommand(ApplicationCommands.Properties, executeHandler, queryEnabledHandler);

            //
            // Command: FocusToolBar
            //          Tells DocumentApplicationDocumentViewer to fire the FocusToolBar operation
            // Bind to the Ctrl-F6 gesture.
            InputGestureCollection input = new InputGestureCollection();
            input.Add(new KeyGesture(Key.F6, ModifierKeys.Control));
            _focusToolBarCommand = CreateAndBindCommand("FocusToolBar",
                SR.Get(SRID.DocumentApplicationDocumentViewerFocusToolBarCommand), input,
                executeHandler, queryEnabledHandler);

            //
            // Command: ViewThumbnails
            //          Tells DocumentViewer to fire the ViewThumbnails operation
            // Bind to the Ctrl+5 gesture.
            DocumentViewer.ViewThumbnailsCommand.InputGestures.Add(new KeyGesture(Key.D5, ModifierKeys.Control));

            // Setup the NavigationCommand bindings.
            executeHandler = new ExecutedRoutedEventHandler(OnNavigationCommandExecute);
            queryEnabledHandler = new CanExecuteRoutedEventHandler(OnNavigationCommandQuery);

            BindCommand(NavigationCommands.FirstPage, executeHandler, queryEnabledHandler);
            BindCommand(NavigationCommands.LastPage, executeHandler, queryEnabledHandler);
        }

        /// <summary>
        /// Will instantiate a command, and do the necessary registrations with the DocumentViewer
        /// </summary>
        /// <param name="name">Command name</param>
        /// <param name="header">UIHeader text for the command</param>
        /// <param name="gestures">Input gestures to bind</param>
        /// <param name="executeHandler">Delegate for Execute</param>
        /// <param name="queryEnabledHandler">Delegate for QueryEnabled</param>
        /// <returns>The new registered command.</returns>
        private static RoutedUICommand CreateAndBindCommand(string name, string header, InputGestureCollection gestures,
            ExecutedRoutedEventHandler executeHandler, CanExecuteRoutedEventHandler queryEnabledHandler)
        {
            // Declare the command
            RoutedUICommand command = new RoutedUICommand(header, name, typeof(DocumentApplicationDocumentViewer), gestures);

            BindCommand(command, executeHandler, queryEnabledHandler);

            return command;
        }

        /// <summary>
        /// Will register a command handler with the DocumentViewer
        /// </summary>
        /// <param name="command">Command to bind</param>
        /// <param name="executeHandler">Delegate for Execute</param>
        /// <param name="queryEnabledHandler">Delegate for QueryEnabled</param>
        private static void BindCommand(RoutedUICommand command, ExecutedRoutedEventHandler executeHandler,
            CanExecuteRoutedEventHandler queryEnabledHandler)
        {
            if (command != null)
            {
                // Register DocumentApplicationDocumentViewer has a handler for the command.
                CommandManager.RegisterClassCommandBinding(typeof(DocumentApplicationDocumentViewer),
                    new CommandBinding(command, executeHandler, queryEnabledHandler));

                // Bind each input gesture on the DocumentApplicationDocumentViewer
                if (command.InputGestures != null)
                {
                    foreach (InputGesture inputGesture in command.InputGestures)
                    {
                        CommandManager.RegisterClassInputBinding(typeof(DocumentApplicationDocumentViewer),
                            new InputBinding(command, inputGesture));
                    }
                }
            }
        }

        /// <summary>
        /// Creates the CommandEnforcer and adds the necessary PolicyBindings to it.
        /// </summary>
        private void CreateEnforcer()
        {
            CommandEnforcer enforcer = new CommandEnforcer(this);

            //Printing enforcements
            enforcer.AddBinding(new PolicyBinding(ApplicationCommands.Print, RightsManagementPolicy.AllowPrint));
            enforcer.AddBinding(new PolicyBinding(ApplicationCommands.PrintPreview, RightsManagementPolicy.AllowPrint));

            //Copy enforcements
            enforcer.AddBinding(new PolicyBinding(ApplicationCommands.Copy, RightsManagementPolicy.AllowCopy));
            enforcer.AddBinding(new PolicyBinding(ApplicationCommands.Cut, RightsManagementPolicy.AllowCopy));
            enforcer.AddBinding(new PolicyBinding(ApplicationCommands.Save, RightsManagementPolicy.AllowCopy));
            enforcer.AddBinding(new PolicyBinding(ApplicationCommands.SaveAs, RightsManagementPolicy.AllowCopy));

            //Signing enforcements
            enforcer.AddBinding(new PolicyBinding(DocumentApplicationDocumentViewer.Sign, RightsManagementPolicy.AllowSign));
            enforcer.AddBinding(new PolicyBinding(DocumentApplicationDocumentViewer.RequestSigners, RightsManagementPolicy.AllowSign));

            _commandEnforcer.Value = enforcer;
        }

        #endregion Commands

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //  These must be checked for null prior to use.  If available, use the CLR properties
        //  above before these.
        //
        //------------------------------------------------------
        #region Private Fields
        //  Used to override Document ViewerBase OnPrintCommand
#if !DONOTREFPRINTINGASMMETA
        private XpsDocumentWriter _documentWriter;                  // DocumentWriter used for printing.
#endif // DONOTREFPRINTINGASMMETA

        // The single instance of our DocumentApplicationDocumentViewer
        private static DocumentApplicationDocumentViewer            _singletonInstance;

        private DocumentSignatureManager                            _docSigManager;
        private DocumentRightsManagementManager                     _rmManager;
        private bool                                                _isUISetup;
        private StatusInfoItem                                      _digSigInfoBar;
        private StatusInfoItem                                      _rmInfoBar;
        private DocumentApplicationState                            _state;
        private const int                                           _invalidPageNumber = -1;
        private SecurityCriticalDataForSet<RightsManagementPolicy>  _rightsManagementPolicy;
        private RightsManagementStatus                              _rightsManagementStatus;        

        // The enforcer for RM
        private SecurityCriticalDataForSet<CommandEnforcer>         _commandEnforcer;

        // Declare commands that are located on DocumentApplicationDocumentViewer
        private static RoutedUICommand                _focusToolBarCommand;
        private static RoutedUICommand                _signCommand;
        private static RoutedUICommand                _requestSignersCommand;
        private static RoutedUICommand                _showSignatureSummaryCommand;
        private static RoutedUICommand                _showRMPermissionsCommand;
        private static RoutedUICommand                _showRMCredentialManagerCommand;
        private static RoutedUICommand                _showRMPublishingUICommand;

        // All of the items below have clr properties above.
        private Grid                            _toolBar;
        private ZoomComboBox                    _zoomComboBox;
        private Button                          _pageUpButton;
        private PageTextBox                     _pageTextBox;
        private Button                          _pageDownButton;
        private Button                          _actualSizeButton;
        private Button                          _pageWidthButton;
        private Button                          _wholePageButton;
        private Button                          _twoPageButton;
        private Button                          _thumbnailButton;
        private Button                          _saveAsButton;
        private Button                          _rmButton;
        private MenuItem                        _digitalSignaturesMenuItem;

        private FrameworkElement                _infoBar;
        private Button                          _infoBarDigSigButton;
        private Button                          _infoBarRMButton;
        private Button                          _infoBarCloseButton;

        private FindToolBar                     _findToolBar;
        private ScrollViewer                    _scrollViewer;

        // Constants for style name references.
        private const string _toolBarName                            = "PUIDocumentApplicationToolBar";
        private const string _toolBarStyleKeyName                    = "PUIDocumentApplicationToolBarStyleKey";
        private const string _zoomComboBoxName                       = "PUIDocumentApplicationZoomComboBox";
        private const string _pageUpButtonName                       = "PUIDocumentApplicationPageUpButton";
        private const string _pageTextBoxName                        = "PUIDocumentApplicationPageTextBox";
        private const string _pageDownButtonName                     = "PUIDocumentApplicationPageDownButton";
        private const string _actualSizeButtonName                   = "PUIDocumentApplicationActualSizeButton";
        private const string _pageWidthButtonName                    = "PUIDocumentApplicationPageWidthButton";
        private const string _wholePageButtonName                    = "PUIDocumentApplicationWholePageButton";
        private const string _twoPageButtonName                      = "PUIDocumentApplicationTwoPagesButton";
        private const string _thumbnailButtonName                    = "PUIDocumentApplicationThumbnailButton";
        private const string _saveAsButtonName                       = "PUIDocumentApplicationSaveAsButton";
        private const string _rmButtonName                           = "PUIDocumentApplicationRMButton";
        private const string _digitalSignaturesMenuItemName          = "PUIDocumentApplicationDigitalSignaturesMenuItem";
        private const string _infoBarName                            = "PUIDocumentApplicationInfoBar";
        private const string _forwardButtonName                      = "PUIDocumentApplicationForwardButton";
        private const string _backButtonName                         = "PUIDocumentApplicationBackButton";
        private const string _digSigSignMenuItemName                 = "PUIDocumentApplicationDigSigSignMenuItem";
        private const string _digSigRequestSignersMenuItemName       = "PUIDocumentApplicationDigSigRequestSignersMenuItem";
        private const string _digSigShowSignatureSummaryMenuItemName = "PUIDocumentApplicationDigSigShowSignatureSummaryMenuItem";
        private const string _infoBarSignaturesButtonName            = "PUIDocumentApplicationInfoBarSignaturesButton";
        private const string _infoBarRMButtonName                    = "PUIDocumentApplicationInfoBarRMButton";
        private const string _infoBarCloseButtonName                 = "PUIDocumentApplicationInfoBarCloseButton";
        private const string _contentHostName                        = "PART_ContentHost";

        /// <summary>
        /// An IWin32Window reference to the RootBrowserWindow hosting XPSViewer.
        /// </summary>
        private System.Windows.Forms.IWin32Window _rootBrowserWindow;

        #endregion Private Fields

        //------------------------------------------------------
        //
        //  Private Nested Classes
        //
        //------------------------------------------------------

        #region WrapperIWin32Window
        /// <summary>
        /// This class exists for the sole purpose of wrapping an IntPtr Window handle into
        /// an IWin32Window object that our WinForms dialogs can use.
        /// </summary>
        private class WrapperIWin32Window : System.Windows.Forms.IWin32Window
        {
            /// <summary>
            /// Constructor for WrapperIWin32Window
            /// </summary>
            /// <param name="handle"></param>
            public WrapperIWin32Window(IntPtr handle)
            {
                _handle = handle;
            }

            /// <summary>
            /// Returns the handle for this IWin32Window object
            /// </summary>
            /// <param name="handle"></param>
            IntPtr System.Windows.Forms.IWin32Window.Handle
            {
                get { return _handle; }
            }

            /// <summary>
            /// The window handle for this IWin32Window object
            /// </summary>
            private IntPtr _handle;
        }
        #endregion WrapperIWin32Window

        #region ZoomComboBoxItem
        /// <summary>
        /// Used to determine what type of item was selected from a ZoomComboBox
        /// </summary>
        private enum ZoomComboBoxItemType
        {
            Unknown = -1,
            Zoom,
            PageWidth,
            AdjacentPages,
            Thumbnails,
        }

        /// <summary>
        /// Used as an item in a ZoomComboBox
        /// </summary>
        private struct ZoomComboBoxItem
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="itemType">Type of item</param>
            /// <param name="description">String representation</param>
            /// <param name="parameter">Zoom value, or number of pages</param>
            public ZoomComboBoxItem(ZoomComboBoxItemType itemType, string description, double parameter)
            {
                _type = itemType;
                _description = description;
                _parameter = parameter;
            }

            /// <summary>
            /// Type of ZoomComboBox item this refers to
            /// </summary>
            public ZoomComboBoxItemType Type
            {
                get
                {
                    return _type;
                }
            }

            /// <summary>
            /// A double value which is used for as a zoom value or the number of adjacent
            /// pages to display, depending on the Type defined.
            /// </summary>
            public double Parameter
            {
                get
                {
                    return _parameter;
                }
            }

            /// <summary>
            /// Returns the description, to be displayed in the ComboBox
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                return _description;
            }

            // Private fields.
            private ZoomComboBoxItemType _type;
            private string _description;
            private double _parameter;
        }
        #endregion ZoomComboBoxItem

        #region ScalingDocument
        /// <summary>
        /// Paginates any document source and scales to fit paper
        /// </summary>
        private sealed class ScalingDocument : DocumentPaginator
        {
            /// <summary>
            /// Constructor for ScalingDocument
            /// </summary>
            /// <param name="document">Document to be printed</param>
            /// <param name="ia">Imagable area of page</param>
            public ScalingDocument(IDocumentPaginatorSource document, PrintDocumentImageableArea ia)
            {
                _documentPaginator = document.DocumentPaginator;
                _ia = ia;
            }

            /// <summary>
            /// Returns a scaled DocumentPage
            /// </summary>
            public override DocumentPage GetPage(int pageNumber)
            {
                DocumentPage page = null;
                FixedPage fixedPage = null;
                FixedDocument fd = _documentPaginator.Source as FixedDocument;

                if (fd != null && pageNumber < fd.Pages.Count)
                {
                    fixedPage = fd.Pages[pageNumber].GetPageRoot(false) as FixedPage;
                }

                if (fixedPage == null)
                {
                    page = _documentPaginator.GetPage(pageNumber);

                    fixedPage = page.Visual as FixedPage;
                }

                if (fixedPage!=null)
                {
                    Rect bleedBox = fixedPage.BleedBox;
                    Rect contentBox = fixedPage.ContentBox;

                    //
                    // Create a Canvas and transfer all children of the fixedPage to the Canvas.
                    // This is required, because XpsDocumentWriter.Write does not handle FixedPage returned
                    // as the Visual from DocumentPaginator.GetPage()
                    //
                    Canvas canvas = new Canvas();

                    canvas.BeginInit();

                    canvas.Language = fixedPage.Language;
                    canvas.Name = fixedPage.Name;

                    UIElement[] children = new UIElement[fixedPage.Children.Count];
                    int i = 0;
                    foreach (UIElement child in fixedPage.Children)
                    {
                        children[i++] = child;
                    }

                    fixedPage.Children.Clear();

                    foreach (UIElement child in children)
                    {
                        canvas.Children.Add(child);
                    }

                    //
                    // First check if the FixedPage fits onto the imagable area
                    //

                    Rect pageBounds = new Rect(0.0, 0.0, fixedPage.Width, fixedPage.Height);

                    TransformGroup tg = new TransformGroup();

                    tg.Children.Add(Transform.Identity);

                    //
                    // See if this is a case of mismatched orientation
                    //
                    bool documentPagePortrait = (fixedPage.Width < fixedPage.Height);
                    bool physicalPagePortrait = (_ia.MediaSizeWidth < _ia.MediaSizeHeight);
                    if (documentPagePortrait != physicalPagePortrait)
                    {
                        //
                        // Must Rotate
                        //
                        if (documentPagePortrait)
                        {
                            //
                            // Printing a Portrait document on a landscape paper. Rotate clockwise
                            // (Rotation direction is NOT arbitrary and matches printschema!)
                            // And shift origin to fit
                            //
                            tg.Children.Add(new MatrixTransform(0.0, 1.0, -1.0, 0.0, fixedPage.Height, 0.0));
                        }
                        else
                        {
                            //
                            // Printing a Landscape document on a portrait paper. Rotate counter-clockwise
                            // (Rotation direction is NOT arbitrary and matches printschema!)
                            // And shift origin to fit
                            //
                            tg.Children.Add(new MatrixTransform(0.0, -1.0, 1.0, 0.0, 0.0, fixedPage.Width));
                        }

                        pageBounds = tg.TransformBounds(pageBounds);
                    }

                    if (pageBounds.Width > _ia.MediaSizeWidth || pageBounds.Height > _ia.MediaSizeHeight )
                    {
                        //
                        // We need to scale, because page is too big
                        //
                        double factorX = _ia.MediaSizeWidth / pageBounds.Width;
                        double factorY = _ia.MediaSizeHeight / pageBounds.Height;

                        //
                        // Scale uniformly by smaller of two factors.
                        //
                        if (factorY < factorX)
                        {
                            factorX = factorY;
                        }

                        ScaleTransform scale = new ScaleTransform(factorX, factorX);
                        tg.Children.Add(scale);
                        pageBounds = scale.TransformBounds(pageBounds);
                    }

                    // 
                    // Now center the result
                    //
                    double left = (_ia.MediaSizeWidth - pageBounds.Width) / 2.0;
                    double top = (_ia.MediaSizeHeight - pageBounds.Height) / 2.0;

                    if (left > 0.0 || top > 0.0)
                    {
                        tg.Children.Add(new TranslateTransform(left - pageBounds.Left, top - pageBounds.Top));
                    }

                    //
                    // Attach resulting transform
                    //
                    canvas.RenderTransform = tg;

                    canvas.EndInit();

                    //
                    // Finally, force measure and arrange
                    //
                    Size size = new Size(_ia.MediaSizeWidth, _ia.MediaSizeHeight);
                    canvas.Measure(size);
                    canvas.Arrange(new Rect(size));
                    canvas.UpdateLayout();

                    if ( !bleedBox.IsEmpty )
                    {
                        bleedBox= tg.TransformBounds(bleedBox);
                    }
                    if (!contentBox.IsEmpty)
                    {
                        contentBox = tg.TransformBounds(contentBox);
                    }
                    page = new DocumentPage(canvas, size, bleedBox, contentBox);
                }

                return page;
            }

            public override bool IsPageCountValid
            {
                get { return _documentPaginator.IsPageCountValid; }
            }

            public override int PageCount
            {
                get { return _documentPaginator.PageCount; }
            }

            public override System.Windows.Size PageSize
            {
                get
                {
                    return _documentPaginator.PageSize;
                }
                set
                {
                    _documentPaginator.PageSize = value;
                }
            }

            public override IDocumentPaginatorSource Source
            {
                get { return _documentPaginator.Source; }
            }

            // Private fields.
            private DocumentPaginator _documentPaginator;
            private PrintDocumentImageableArea _ia;
        }

        #endregion ScalingDocument
    }
}
