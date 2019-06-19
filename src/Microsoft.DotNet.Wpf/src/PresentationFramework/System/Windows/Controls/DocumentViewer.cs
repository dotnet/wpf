// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Control for displaying paginated content.
//

using MS.Internal;                                      // For Invariant.Assert
using MS.Internal.Commands;
using MS.Internal.Documents;
using MS.Internal.Telemetry.PresentationFramework;
using MS.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;                            // For DesignerSerializationVisibility
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;                        // For HyperLink navigation event.
using System.Windows.Markup;
using MS.Internal.Automation;                           // For TextAdaptor.
using System.Security;


namespace System.Windows.Controls
{
    /// <summary>
    /// DocumentViewer is a control that allows developers to create a custom reading
    /// experience in their applications for digital documents.
    /// </summary>
    /// <seealso cref="IDocumentPaginatorSource" />
    /// <speclink>http://d2/DRX/default.aspx</speclink>
    [TemplatePart(Name = "PART_FindToolBarHost", Type = typeof(ContentControl))]
    [TemplatePart(Name = "PART_ContentHost", Type = typeof(ScrollViewer))]
    public class DocumentViewer : DocumentViewerBase
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        /// <summary>
        /// Initializes class-wide settings.
        /// </summary>
        static DocumentViewer()
        {
            // Create our CommandBindings
            CreateCommandBindings();

            // Register property metadata
            RegisterMetadata();

            ControlsTraceLogger.AddControl(TelemetryControls.DocumentViewer);
        }

        /// <summary>
        /// Instantiates a new instance of a DocumentViewer
        /// </summary>
        public DocumentViewer() : base()
        {
            //Perf Tracing - DocumentViewer Construction
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXInstantiated);

            SetUp();
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        #region Command convenience methods

        /// <summary>
        /// Tells DocumentViewer to display a "thumbnail view" of pages.  This is analogous
        /// to the ViewThumbnailsCommand.
        /// </summary>
        public void ViewThumbnails()
        {
            OnViewThumbnailsCommand();
        }

        /// <summary>
        /// Tells DocumentViewer to fit a single page to the width of the current viewport.
        /// This is analogous to the FitToWidthCommand.
        /// </summary>
        public void FitToWidth()
        {
            OnFitToWidthCommand();
        }

        /// <summary>
        /// Tells DocumentViewer to fit a single page to the height of the current viewport.
        /// This is analogous to the FitToHeightCommand.
        /// </summary>
        public void FitToHeight()
        {
            OnFitToHeightCommand();
        }

        /// <summary>
        /// Tells DocumentViewer to fit the current MaxPagesAcross count to the current
        /// viewport.  This is analogous to the FitToMaxPagesAcrossCommand.
        /// </summary>
        public void FitToMaxPagesAcross()
        {
            OnFitToMaxPagesAcrossCommand();
        }

        /// <summary>
        /// Tells DocumentViewer to fit the specified number of pages across to the current viewport
        /// and sets MaxPagesAcross to the passed in value.  This is analogous to the
        /// FitMaxPagesAcrossCommand.
        /// </summary>
        /// <param name="pagesAcross"></param>
        public void FitToMaxPagesAcross(int pagesAcross)
        {
            if (ValidateMaxPagesAcross(pagesAcross))
            {
                if (_documentScrollInfo != null)
                {
                    _documentScrollInfo.FitColumns(pagesAcross);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException("pagesAcross");
            }
        }

        /// <summary>
        /// Tells DocumentViewer to invoke the Find Dialog.  This is analogous to the
        /// FindCommand.
        /// </summary>
        public void Find()
        {
            OnFindCommand();
        }

        /// <summary>
        /// Tells DocumentViewer to scroll up by one viewport.  This is analogous to the
        /// ScrollPageUpCommand.
        /// </summary>
        public void ScrollPageUp()
        {
            OnScrollPageUpCommand();
        }

        /// <summary>
        /// Tells DocumentViewer to scroll down by one viewport.  This is analogous to the
        /// ScrollPageDownCommand.
        /// </summary>
        public void ScrollPageDown()
        {
            OnScrollPageDownCommand();
        }

        /// <summary>
        /// Tells DocumentViewer to scroll left by one viewport.  This is analogous to the
        /// ScrollPageLeftCommand.
        /// </summary>
        public void ScrollPageLeft()
        {
            OnScrollPageLeftCommand();
        }

        /// <summary>
        /// Tells DocumentViewer to scroll right by one viewport.  This is analogous to the
        /// ScrollPageRightCommand.
        /// </summary>
        public void ScrollPageRight()
        {
            OnScrollPageRightCommand();
        }

        /// <summary>
        /// Tells DocumentViewer to scroll up by one line (16px).  This is analogous to the
        /// MoveUpCommand.
        /// </summary>
        public void MoveUp()
        {
            OnMoveUpCommand();
        }

        /// <summary>
        /// Tells DocumentViewer to scroll down by one line (16px).  This is analogous to the
        /// MoveDownCommand.
        /// </summary>
        public void MoveDown()
        {
            OnMoveDownCommand();
        }

        /// <summary>
        /// Tells DocumentViewer to scroll left by one line (16px).  This is analogous to the
        /// MoveLeftCommand.
        /// </summary>
        public void MoveLeft()
        {
            OnMoveLeftCommand();
        }

        /// <summary>
        /// Tells DocumentViewer to scroll right by one line (16px).  This is analogous to the
        /// MoveRightCommand.
        /// </summary>
        public void MoveRight()
        {
            OnMoveRightCommand();
        }

        /// <summary>
        /// Tells DocumentViewer to increase Zoom by a predefined, nonlinear value.  This is
        /// analogous to the IncreaseZoomCommand.
        /// </summary>
        public void IncreaseZoom()
        {
            OnIncreaseZoomCommand();
        }

        /// <summary>
        /// Tells DocumentViewer to decrease Zoom by a predefined, nonlinear value.  This is
        /// analogous to the DecreaseZoomCommand.
        /// </summary>
        public void DecreaseZoom()
        {
            OnDecreaseZoomCommand();
        }

        #endregion Command convenience methods

        /// <summary>
        /// Called when the Template's tree has been generated
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
        /// <exception cref="NotSupportedException">There must be a ScrollViewer in
        /// DocumentViewer's visual tree which has Name PART_ContentHost.</exception>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Walk the visual tree, locating those elements marked with the special
            // properties such as "I'm the Content area!"
            FindContentHost();

            // Create the Find toolbar and add it to our Visual Tree when appropriate.
            InstantiateFindToolBar();

            //Perf Tracing - DocumentViewer Visuals Created
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXStyleCreated);

            // If unset, we will set the ContextMenu property to null -- this will prevent the TextEditor
            // from overriding our ContextMenu with its own.  The TextEditor checks the UIScope (DocumentViewer)
            // not the RenderScope (DocumentGrid) for previously-set Context Menus, and if it finds one (or if
            // it finds that ContextMenu has explicitly been set to null, it will not override it.
            // Since DocumentViewer doesn't have a ContextMenu -- the ContextMenu is set on DocumentGrid which the
            // TextEditor won't find, we set ContextMenu to null here.
            // Yes, this seems, on the surface, to be a bit redundant -- but there is a difference between an
            // "unset null" and an "explicitly set" null to the Property engine that the TextEditor looks for.
            // We check that the ContextMenu is null because we don't want to override any user-specified ContextMenus.
            if (this.ContextMenu == null)
            {
                this.ContextMenu = null;
            }
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        #region Commands

        /// <summary>
        /// This command will invoke ViewThumbnails method, causing as many pages
        /// as feasible to be displayed in the current viewport.
        /// </summary>
        public static RoutedUICommand ViewThumbnailsCommand
        {
             get
             {
                 return _viewThumbnailsCommand;
             }
        }

        /// <summary>
        /// This command will invoke the FitToWidth method,
        /// setting the layout to 1 page across, and zooming so that one page is displayed
        /// at the width of the current viewport.
        /// </summary>
        public static RoutedUICommand FitToWidthCommand
        {
            get
            {
                return _fitToWidthCommand;
            }
        }

        /// <summary>
        /// This command will invoke the FitToWidth method,
        /// setting the layout to 1 page across, and zooming so that one page is displayed
        /// at the height of the current viewport.
        /// </summary>
        public static RoutedUICommand FitToHeightCommand
        {
            get
            {
                return _fitToHeightCommand;
            }
        }

        /// <summary>
        /// This command will invoke the FitToMaxPagesAcross method,
        /// effectively setting MaxPagesAcross
        /// to the value of the parameter and fitting those pages into view.
        /// </summary>
        public static RoutedUICommand FitToMaxPagesAcrossCommand
        {
            get
            {
                return _fitToMaxPagesAcrossCommand;
            }
        }

        #endregion Commands

        #region Dependency Properties

        #region HorizontalOffset
        /// <summary>
        /// Reflects the current Horizontal position in the document in pixel units given
        /// the current page layout.
        /// </summary>
        public static readonly DependencyProperty HorizontalOffsetProperty =
                DependencyProperty.Register(
                        "HorizontalOffset",
                        typeof(double),
                        typeof(DocumentViewer),
                        new FrameworkPropertyMetadata(
                                _horizontalOffsetDefault, //default value
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, //MetaData flags
                                new PropertyChangedCallback(OnHorizontalOffsetChanged)), //changed callback
                        new ValidateValueCallback(ValidateOffset)); //validate callback

        /// <summary>
        /// Reflects the current Horizontal position in the document in pixel units given
        /// the current page layout.
        /// </summary>
        public double HorizontalOffset
        {
            get { return (double) GetValue(HorizontalOffsetProperty); }
            set
            {
                SetValue(HorizontalOffsetProperty, value);
            }
        }
        #endregion HorizontalOffset

        #region VerticalOffset
        /// <summary>
        /// Reflects the current Vertical position in the document in pixel units given
        /// the current page layout.
        /// </summary>
        public static readonly DependencyProperty VerticalOffsetProperty =
                DependencyProperty.Register(
                        "VerticalOffset",
                        typeof(double),
                        typeof(DocumentViewer),
                        new FrameworkPropertyMetadata(
                                _verticalOffsetDefault, //default value
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, //MetaData flags
                                new PropertyChangedCallback(OnVerticalOffsetChanged)), //changed callback
                        new ValidateValueCallback(ValidateOffset)); //validate callback

        /// <summary>
        /// Reflects the current Vertical position in the document in pixel units given
        /// the current page layout.
        /// </summary>
        public double VerticalOffset
        {
            get { return (double) GetValue(VerticalOffsetProperty); }
            set
            {
                SetValue(VerticalOffsetProperty, value);
            }
        }
        #endregion VerticalOffset

        #region ExtentWidth
        /// <summary>
        /// Reflects the current width of the document layout.
        /// </summary>
        private static readonly DependencyPropertyKey ExtentWidthPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "ExtentWidth",
                        typeof(double),
                        typeof(DocumentViewer),
                        new FrameworkPropertyMetadata(
                                _extentWidthDefault,
                                new PropertyChangedCallback(OnExtentWidthChanged)));

        /// <summary>
        /// Reflects the current width of the document layout.
        /// </summary>
        public static readonly DependencyProperty ExtentWidthProperty =
                ExtentWidthPropertyKey.DependencyProperty;

        /// <summary>
        /// Reflects the current width of the document layout.
        /// </summary>
        public double ExtentWidth
        {
            get { return (double) GetValue(ExtentWidthProperty); }
        }
        #endregion ExtentWidth

        #region ExtentHeight
        /// <summary>
        /// Reflects the current height of the document layout.
        /// </summary>
        private static readonly DependencyPropertyKey ExtentHeightPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "ExtentHeight",
                        typeof(double),
                        typeof(DocumentViewer),
                        new FrameworkPropertyMetadata(
                                _extentHeightDefault,
                                new PropertyChangedCallback(OnExtentHeightChanged)));

        /// <summary>
        /// Reflects the current height of the document layout.
        /// </summary>
        public static readonly DependencyProperty ExtentHeightProperty =
                ExtentHeightPropertyKey.DependencyProperty;

        /// <summary>
        /// Reflects the current height of the document layout.
        /// </summary>
        public double ExtentHeight
        {
            get { return (double) GetValue(ExtentHeightProperty); }
        }
        #endregion ExtentHeight

        #region ViewportWidth
        /// <summary>
        /// Reflects the current width of the DocumentViewer's content area.
        /// </summary>
        private static readonly DependencyPropertyKey ViewportWidthPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "ViewportWidth",
                        typeof(double),
                        typeof(DocumentViewer),
                        new FrameworkPropertyMetadata(
                                _viewportWidthDefault,
                                new PropertyChangedCallback(OnViewportWidthChanged)));


        /// <summary>
        /// Reflects the current width of the DocumentViewer's content area.
        /// </summary>
        public static readonly DependencyProperty ViewportWidthProperty =
                ViewportWidthPropertyKey.DependencyProperty;

        /// <summary>
        /// Reflects the current width of the DocumentViewer's content area.
        /// </summary>
        public double ViewportWidth
        {
            get { return (double) GetValue(ViewportWidthProperty); }
        }
        #endregion ViewportWidth

        #region ViewportHeight
        /// <summary>
        /// Reflects the current height of the DocumentViewer's content area.
        /// </summary>
        private static readonly DependencyPropertyKey ViewportHeightPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "ViewportHeight",
                        typeof(double),
                        typeof(DocumentViewer),
                        new FrameworkPropertyMetadata(
                                _viewportHeightDefault,
                                new PropertyChangedCallback(OnViewportHeightChanged)));

        /// <summary>
        /// Reflects the current height of the DocumentViewer's content area.
        /// </summary>
        public static readonly DependencyProperty ViewportHeightProperty =
                ViewportHeightPropertyKey.DependencyProperty;

        /// <summary>
        /// Reflects the current height of the DocumentViewer's content area.
        /// </summary>
        public double ViewportHeight
        {
            get { return (double) GetValue(ViewportHeightProperty);  }
        }
        #endregion ViewportHeight

        #region ShowPageBorders
        /// <summary>
        /// Reflects whether a "Drop Shadow" border should be shown around the pages being displayed.
        /// </summary>
        public static readonly DependencyProperty ShowPageBordersProperty =
                DependencyProperty.Register(
                        "ShowPageBorders",
                        typeof(bool),
                        typeof(DocumentViewer),
                        new FrameworkPropertyMetadata(
                                _showPageBordersDefault, //default value
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, //MetaData flags
                                new PropertyChangedCallback(OnShowPageBordersChanged))); //changed callback

        /// <summary>
        /// Reflects whether a "Drop Shadow" border should be shown around the pages being displayed.
        /// </summary>
        public bool ShowPageBorders
        {
            get { return (bool) GetValue(ShowPageBordersProperty); }
            set { SetValue(ShowPageBordersProperty, value); }
        }
        #endregion ShowPageBorders

        #region Zoom
        /// <summary>
        /// Reflects the effective Zoom percentage based on the last layout related
        /// property set or command issued.
        /// If this was the last property set, or the Zoom command was last issued,
        /// then this will just be the last Zoom value set.
        /// When another layout related property (ie MaxPagesAcross, etc) was set
        /// or a command (ie FitToMaxPagesAcross, FitToHeight, etc) was issued, then
        /// this value will be the resulting Zoom value from that layout adjustment.
        ///
        /// Must be greater than 5.0 and less than 5000.0.  Invalid values will throw.
        /// </summary>
        public static readonly DependencyProperty ZoomProperty =
                DependencyProperty.Register(
                        "Zoom",
                        typeof(double),
                        typeof(DocumentViewer),
                        new FrameworkPropertyMetadata(
                                _zoomPercentageDefault, //default value
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, //MetaData flags
                                new PropertyChangedCallback(OnZoomChanged),  //changed callback
                                new CoerceValueCallback(CoerceZoom))); // coercion callback

        /// <summary>
        /// Reflects the effective Zoom percentage based on the last layout related
        /// property set or command issued.
        /// If this was the last property set, or the Zoom command was last issued,
        /// then this will just be the last Zoom value set.
        /// When another layout related property (ie MaxPagesAcross, etc) was set
        /// or a command (ie FitToMaxPagesAcross, FitToHeight, etc) was issued, then
        /// this value will be the resulting Zoom value from that layout adjustment.
        ///
        /// Must be greater than 5.0 and less than 5000.0.
        /// </summary>
        public double Zoom
        {
            get { return (double) GetValue(ZoomProperty); }
            set { SetValue(ZoomProperty, value); }
        }
        #endregion Zoom

        #region MaxPagesAcross
        /// <summary>
        /// Reflects the number of Columns of pages displayed, based on the last
        /// value set through this property, or a layout related command
        /// (ie FitToMaxPagesAcross, ViewThumbnails, etc..)
        /// </summary>
        public static readonly DependencyProperty MaxPagesAcrossProperty =
                DependencyProperty.Register(
                        "MaxPagesAcross",
                        typeof(int),
                        typeof(DocumentViewer),
                        new FrameworkPropertyMetadata(
                                _maxPagesAcrossDefault, //default value
                                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, //MetaData flags
                                new PropertyChangedCallback(OnMaxPagesAcrossChanged)), //changed callback
                        new ValidateValueCallback(ValidateMaxPagesAcross)); //validation callback

        /// <summary>
        /// Reflects the number of Columns of pages displayed, based on the last
        /// value set through this property, or a layout related command
        /// (ie FitToMaxPagesAcross, ViewThumbnails, etc..)
        /// Valid values are from 1 to 32, inclusive.
        /// </summary>
        public int MaxPagesAcross
        {
            get { return (int) GetValue(MaxPagesAcrossProperty); }
            set { SetValue(MaxPagesAcrossProperty, value); }
        }
        #endregion MaxPagesAcross

        #region VerticalPageSpacing
        /// <summary>
        /// Reflects the vertical gap between Pages when laid out, in pixel units.
        /// </summary>
        public static readonly DependencyProperty VerticalPageSpacingProperty =
                DependencyProperty.Register(
                        "VerticalPageSpacing",
                        typeof(double),
                        typeof(DocumentViewer),
                        new FrameworkPropertyMetadata(
                                _verticalPageSpacingDefault, //default value
                                new PropertyChangedCallback(OnVerticalPageSpacingChanged)), //changed callback
                        new ValidateValueCallback(ValidatePageSpacing)); //validation callback

        /// <summary>
        /// Reflects the vertical gap between Pages when laid out, in pixel units.
        /// </summary>
        public double VerticalPageSpacing
        {
            get { return (double) GetValue(VerticalPageSpacingProperty); }
            set { SetValue(VerticalPageSpacingProperty, value); }
        }
        #endregion VerticalPageSpacing

        #region HorizontalPageSpacing
        /// <summary>
        /// Reflects the horizontal gap between Pages when laid out, in pixel units.
        /// </summary>
        public static readonly DependencyProperty HorizontalPageSpacingProperty =
                DependencyProperty.Register(
                        "HorizontalPageSpacing",
                        typeof(double),
                        typeof(DocumentViewer),
                        new FrameworkPropertyMetadata(
                                _horizontalPageSpacingDefault, //default value
                                new PropertyChangedCallback(OnHorizontalPageSpacingChanged)), //changed callback
                        new ValidateValueCallback(ValidatePageSpacing)); //validation callback

        /// <summary>
        /// Reflects the horizontal gap between Pages when laid out, in pixel units.
        /// </summary>
        public double HorizontalPageSpacing
        {
            get { return (double) GetValue(HorizontalPageSpacingProperty); }
            set { SetValue(HorizontalPageSpacingProperty, value); }
        }
        #endregion HorizontalPageSpacing

        #region CanMoveUp
        /// <summary>
        /// Reflects whether the DocumentViewer is at the top of the current document.
        /// </summary>
        private static readonly DependencyPropertyKey CanMoveUpPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "CanMoveUp",
                        typeof(bool),
                        typeof(DocumentViewer),
                        new FrameworkPropertyMetadata(_canMoveUpDefault));

        /// <summary>
        /// Reflects whether the DocumentViewer is at the top of the current document.
        /// </summary>
        public static readonly DependencyProperty CanMoveUpProperty =
                CanMoveUpPropertyKey.DependencyProperty;

        /// <summary>
        /// Reflects whether the DocumentViewer is at the top of the current document.
        /// </summary>
        public bool CanMoveUp
        {
            get { return (bool) GetValue(CanMoveUpProperty); }
        }
        #endregion CanMoveUp

        #region CanMoveDown
        /// <summary>
        /// Reflects whether the DocumentViewer is at the bottom of the current document.
        /// </summary>
        private static readonly DependencyPropertyKey CanMoveDownPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "CanMoveDown",
                        typeof(bool),
                        typeof(DocumentViewer),
                        new FrameworkPropertyMetadata(_canMoveDownDefault));

        /// <summary>
        /// Reflects whether the DocumentViewer is at the bottom of the current document.
        /// </summary>
        public static readonly DependencyProperty CanMoveDownProperty =
                CanMoveDownPropertyKey.DependencyProperty;

        /// <summary>
        /// Reflects whether the DocumentViewer is at the bottom of the current document.
        /// </summary>
        public bool CanMoveDown
        {
            get { return (bool) GetValue(CanMoveDownProperty); }
        }
        #endregion CanMoveDown

        #region CanMoveLeft
        /// <summary>
        /// Reflects whether the DocumentViewer is at the leftmost extent of the current document.
        /// </summary>
        private static readonly DependencyPropertyKey CanMoveLeftPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "CanMoveLeft",
                        typeof(bool),
                        typeof(DocumentViewer),
                        new FrameworkPropertyMetadata(_canMoveLeftDefault));

        /// <summary>
        /// Reflects whether the DocumentViewer is at the leftmost extent of the current document.
        /// </summary>
        public static readonly DependencyProperty CanMoveLeftProperty =
                CanMoveLeftPropertyKey.DependencyProperty;

        /// <summary>
        /// Reflects whether the DocumentViewer is at the leftmost extent of the current document.
        /// </summary>
        public bool CanMoveLeft
        {
            get { return (bool) GetValue(CanMoveLeftProperty); }
        }
        #endregion CanMoveLeft

        #region CanMoveRight
        /// <summary>
        /// Reflects whether the DocumentViewer is at the rightmost extent of the current document.
        /// </summary>
        private static readonly DependencyPropertyKey CanMoveRightPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "CanMoveRight",
                        typeof(bool),
                        typeof(DocumentViewer),
                        new FrameworkPropertyMetadata(_canMoveRightDefault));

        /// <summary>
        /// Reflects whether the DocumentViewer is at the rightmost extent of the current document.
        /// </summary>
        public static readonly DependencyProperty CanMoveRightProperty =
                CanMoveRightPropertyKey.DependencyProperty;

        /// <summary>
        /// Reflects whether the DocumentViewer is at the rightmost extent of the current document.
        /// </summary>
        public bool CanMoveRight
        {
            get { return (bool) GetValue(CanMoveRightProperty); }
        }
        #endregion CanMoveRight

        #region CanIncreaseZoom
        /// <summary>
        /// Reflects whether the DocumentViewer can zoom in any further
        /// (ie not at the highest "zoom level") of the current document.
        /// </summary>
        private static readonly DependencyPropertyKey CanIncreaseZoomPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "CanIncreaseZoom",
                        typeof(bool),
                        typeof(DocumentViewer),
                        new FrameworkPropertyMetadata(_canIncreaseZoomDefault));

        /// <summary>
        /// Reflects whether the DocumentViewer can zoom in any further
        /// (ie not at the highest "zoom level") of the current document.
        /// </summary>
        public static readonly DependencyProperty CanIncreaseZoomProperty =
                CanIncreaseZoomPropertyKey.DependencyProperty;

        /// <summary>
        /// Reflects whether the DocumentViewer can zoom in any further
        /// (ie not at the highest "zoom level") of the current document.
        /// </summary>
        public bool CanIncreaseZoom
        {
            get { return (bool) GetValue(CanIncreaseZoomProperty); }
        }
        #endregion CanIncreaseZoom

        #region CanDecreaseZoom
        /// <summary>
        /// Reflects whether the DocumentViewer can zoom out any further
        /// (ie not at the lowest "zoom level") of the current document.
        /// </summary>
        private static readonly DependencyPropertyKey CanDecreaseZoomPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "CanDecreaseZoom",
                        typeof(bool),
                        typeof(DocumentViewer),
                        new FrameworkPropertyMetadata(_canDecreaseZoomDefault));

        /// <summary>
        /// Reflects whether the DocumentViewer can zoom out any further
        /// (ie not at the lowest "zoom level") of the current document.
        /// </summary>
        public static readonly DependencyProperty CanDecreaseZoomProperty =
                CanDecreaseZoomPropertyKey.DependencyProperty;

        /// <summary>
        /// Reflects whether the DocumentViewer can zoom out any further
        /// (ie not at the lowest "zoom level") of the current document.
        /// </summary>
        public bool CanDecreaseZoom
        {
            get { return (bool) GetValue(CanDecreaseZoomProperty); }
        }
        #endregion CanDecreaseZoom

        #endregion Dependency Properties

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Operators
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
        #region Protected Methods

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new DocumentViewerAutomationPeer(this);
        }

        /// <summary>
        /// Attaches DocumentGrid to our document when it changes.
        /// </summary>
        protected override void OnDocumentChanged()
        {
            // Validate the new document type
            if (!(Document is FixedDocument) && !(Document is FixedDocumentSequence)
                && !(Document == null))
            {
                throw new NotSupportedException(SR.Get(SRID.DocumentViewerOnlySupportsFixedDocumentSequence));
            }

            //Call the base so that TextEditors are attached.
            base.OnDocumentChanged();

            //Assign the content to DocumentGrid.
            AttachContent();

            // Update the toolbar with our current document state.
            if (_findToolbar != null)
            {
                _findToolbar.DocumentLoaded = (Document != null) ? true : false;
            }

            // We do not automatically go to the first page on the _first_ content
            // assignment, for two reasons:
            //  1) If this is the first assignment, then we're already there by default.
            //  2) The user may have specified vertical or horizontal offsets in markup or
            //     otherwise (<DocumentViewer VerticalOffset="1000">) and we need to honor
            //     those settings.
            if (!_firstDocumentAssignment)
            {
                // Go to the first page of new content.
                OnGoToPageCommand(1);
            }
            _firstDocumentAssignment = false;
        }

        /// <summary>
        /// Called when a BringIntoView event is bubbled up from the Document.
        /// Base implementation will move the master page to the page
        /// on which the element occurs.
        /// </summary>
        /// <param name="element">The object to make visible.</param>
        /// <param name="rect">The rectangular region in the object's coordinate space which should be made visible.</param>
        /// <param name="pageNumber"></param>
        protected override void OnBringIntoView(DependencyObject element, Rect rect, int pageNumber)
        {
            // DVBase will give us a 1-indexed page number, we convert to 0-indexed.
            int zeroIndexed = pageNumber - 1;
            if (zeroIndexed >= 0 && zeroIndexed < PageCount)
            {
                _documentScrollInfo.MakeVisible(element, rect, zeroIndexed);
            }
        }

        /// <summary>
        /// Handler for the PreviousPage command.
        /// </summary>
        protected override void OnPreviousPageCommand()
        {
            //Scroll to the previous row.
            if (_documentScrollInfo != null)
            {
                _documentScrollInfo.ScrollToPreviousRow();
            }
        }

        /// <summary>
        /// Handler for the NextPage command.
        /// </summary>
        protected override void OnNextPageCommand()
        {
            //Scroll to the previous row.
            if (_documentScrollInfo != null)
            {
                _documentScrollInfo.ScrollToNextRow();
            }
        }

        /// <summary>
        /// Handler for the FirstPage command.
        /// </summary>
        protected override void OnFirstPageCommand()
        {
            //Scroll to the top of the document.
            if (_documentScrollInfo != null)
            {
                _documentScrollInfo.MakePageVisible( 0 );
            }
        }

        /// <summary>
        /// Handler for the LastPage command.
        /// </summary>
        protected override void OnLastPageCommand()
        {
            //Scroll to the bottom of the document.
            if (_documentScrollInfo != null)
            {
                _documentScrollInfo.MakePageVisible( PageCount - 1 );
            }
        }

        /// <summary>
        /// Handler for the GoToPage command.
        /// </summary>
        protected override void OnGoToPageCommand(int pageNumber)
        {
            // Check if we can go to the specified page.
            // and navigate there.
            if (CanGoToPage(pageNumber))
            {
                //Scroll to the specified page in the document.
                if (_documentScrollInfo != null)
                {
                    // CanGoToPage should have guaranteed that this assert is always true.
                    Invariant.Assert(pageNumber > 0, "PageNumber must be positive.");
                    _documentScrollInfo.MakePageVisible(pageNumber - 1);
                }
            }
        }

        /// <summary>
        /// Handler for the ViewThumbnails Command
        /// </summary>
        protected virtual void OnViewThumbnailsCommand()
        {
            if (_documentScrollInfo != null)
            {
                _documentScrollInfo.ViewThumbnails();
            }
        }

        /// <summary>
        /// Handler for the FitToWidth Command
        /// </summary>
        protected virtual void OnFitToWidthCommand()
        {
            if (_documentScrollInfo != null)
            {
                _documentScrollInfo.FitToPageWidth();
            }
        }

        /// <summary>
        /// Handler for the FitToHeight Command
        /// </summary>
        protected virtual void OnFitToHeightCommand()
        {
            if (_documentScrollInfo != null)
            {
                _documentScrollInfo.FitToPageHeight();
            }
        }

        /// <summary>
        /// Handler for the FitToMaxPagesAcross Command
        /// </summary>
        protected virtual void OnFitToMaxPagesAcrossCommand()
        {
            if (_documentScrollInfo != null)
            {
                _documentScrollInfo.FitColumns(MaxPagesAcross);
            }
        }

        /// <summary>
        /// HAndler for the FitMaxPagesAcross Command
        /// </summary>
        /// <param name="pagesAcross"></param>
        protected virtual void OnFitToMaxPagesAcrossCommand(int pagesAcross)
        {
            if (ValidateMaxPagesAcross(pagesAcross))
            {
                if (_documentScrollInfo != null)
                {
                    _documentScrollInfo.FitColumns(pagesAcross);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException("pagesAcross");
            }
        }

        /// <summary>
        /// Handler for the Find Command
        /// </summary>
        protected virtual void OnFindCommand()
        {
            GoToFind();
        }

        /// <summary>
        /// This is the method that responds to the KeyDown event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Look for Find specific key inputs and process them.
            // If the key is processed, this event will be marked handled.
            e = ProcessFindKeys(e);

            base.OnKeyDown(e);
        }

        /// <summary>
        /// Handler for the ScrollPageUp Command
        /// </summary>
        protected virtual void OnScrollPageUpCommand()
        {
            if (_documentScrollInfo != null)
            {
                _documentScrollInfo.PageUp();
            }
        }

        /// <summary>
        /// Handler for the ScrollPageDown Command
        /// </summary>
        protected virtual void OnScrollPageDownCommand()
        {
            if (_documentScrollInfo != null)
            {
                _documentScrollInfo.PageDown();
            }
        }

        /// <summary>
        /// Handler for the ScrollPageLeft Command
        /// </summary>
        protected virtual void OnScrollPageLeftCommand()
        {
            if (_documentScrollInfo != null)
            {
                _documentScrollInfo.PageLeft();
            }
        }

        /// <summary>
        /// Handler for the ScrollPageRight Command
        /// </summary>
        protected virtual void OnScrollPageRightCommand()
        {
            if (_documentScrollInfo != null)
            {
                _documentScrollInfo.PageRight();
            }
        }

        /// <summary>
        /// Handler for the MoveUp Command
        /// </summary>
        protected virtual void OnMoveUpCommand()
        {
            if (_documentScrollInfo != null)
            {
                _documentScrollInfo.LineUp();
            }
        }

        /// <summary>
        /// Handler for the MoveDown Command
        /// </summary>
        protected virtual void OnMoveDownCommand()
        {
            if (_documentScrollInfo != null)
            {
                _documentScrollInfo.LineDown();
            }
        }

        /// <summary>
        /// Handler for the MoveLeft Command
        /// </summary>
        protected virtual void OnMoveLeftCommand()
        {
            if (_documentScrollInfo != null)
            {
                _documentScrollInfo.LineLeft();
            }
        }

        /// <summary>
        /// Handler for the MoveRight Command
        /// </summary>
        protected virtual void OnMoveRightCommand()
        {
            if (_documentScrollInfo != null)
            {
                _documentScrollInfo.LineRight();
            }
        }

        /// <summary>
        /// Handler for the IncreaseZoom Command
        /// </summary>
        protected virtual void OnIncreaseZoomCommand()
        {
            // Check if possible to zoom in
            if (CanIncreaseZoom)
            {
                // Update the zoom level index to the appropriate place.
                double oldZoom = Zoom;
                FindZoomLevelIndex();
                // As long as more zoomLevel's exist, increase zoom.
                if (_zoomLevelIndex > 0)
                {
                    _zoomLevelIndex--;
                }

                // Set the zoom percentage, with _updatingInternalZoomLevel set to true to
                //   avoid resetting the zoom level index.
                _updatingInternalZoomLevel = true;
                Zoom = DocumentViewer._zoomLevelCollection[_zoomLevelIndex];
                _updatingInternalZoomLevel = false;
            }
        }

        /// <summary>
        /// Handler for the DecreaseZoom Command
        /// </summary>
        protected virtual void OnDecreaseZoomCommand()
        {
            // Check if possible to zoom out.
            if (CanDecreaseZoom)
            {
                // Update the zoom level index to the appropriate place.
                double oldZoom = Zoom;
                FindZoomLevelIndex();
                // If the current zoom value exists in the zoomLevelCollection, and can
                //   still be zoomed out, then zoom out another level.
                if ((oldZoom == DocumentViewer._zoomLevelCollection[_zoomLevelIndex]) &&
                    (_zoomLevelIndex < DocumentViewer._zoomLevelCollection.Length - 1))
               {
                    _zoomLevelIndex++;
                }

                // Set the zoom percentage, with _updatingInternalZoomLevel set to true to
                //   avoid resetting the zoom level index.
                _updatingInternalZoomLevel = true;
                Zoom = _zoomLevelCollection[_zoomLevelIndex];
                _updatingInternalZoomLevel = false;
            }
        }

        /// <summary>
        /// Overrides the base implementation and returns the current collection of
        /// DocumentPageViews being displayed in our IDSI.
        /// </summary>
        /// <param name="changed"></param>
        /// <returns></returns>
        protected override ReadOnlyCollection<DocumentPageView> GetPageViewsCollection(out bool changed)
        {
            ReadOnlyCollection<DocumentPageView> pageViews = null;

            //Save off the current value of our PageView changed flag so we can
            //return it to indicate if the collection has actually changed.
            changed = _pageViewCollectionChanged;

            //Reset the flag so that if this is called again before InvalidatePageViews is called
            //it'll reflect the unchanged-ness of the collection.
            _pageViewCollectionChanged = false;

            if (_documentScrollInfo != null && _documentScrollInfo.PageViews != null)
            {
                //Return the current collection.
                pageViews = _documentScrollInfo.PageViews;
            }
            else
            {
                //Return an empty collection (null is not valid).
                pageViews = new ReadOnlyCollection<DocumentPageView>(new List<DocumentPageView>(0));
            }

            return pageViews;
        }

        /// <summary>
        /// Overrides the OnMouseLeftButtonDown method so that we can take focus when clicked.
        /// </summary>
        /// <param name="e">The MouseButtonEventArgs associated with this mouse event.</param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            // If no other controls in our Template have handled the event, we'll
            // take focus here.
            if (!e.Handled)
            {
                Focus();
                e.Handled = true;
            }
        }

        /// <summary>
        /// OnPreviewMouseWheel Zooms in/out on the document when the
        /// mouse wheel is scrolled and the Ctrl key is depressed.
        /// </summary>
        /// <param name="e">Event Arguments</param>
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            if (e.Handled)
            {
                return;
            }

            //Get the state of the Ctrl key -- if it's pressed, we'll Zoom.
            //Otherwise we do nothing and let others handle this event.
            if (Keyboard.IsKeyDown(Key.LeftCtrl) ||
                Keyboard.IsKeyDown(Key.RightCtrl))
            {
                e.Handled = true;

                //Zoom based on the direction of the wheel.
                if (e.Delta < 0)
                {
                    DecreaseZoom();
                }
                else
                {
                    IncreaseZoom();
                }
            }
        }

        #endregion Protected Methods

        #region Internal Methods
        /// <summary>
        /// Called when our IDocumentScrollInfo has new layout information to share with us.
        /// </summary>
        internal void InvalidateDocumentScrollInfo()
        {
            // We need to see if any IDocumentScrollInfo properties that DocumentViewer
            //   either exposes or makes use of have changed.  If so we invalidate properties
            //   or take actions here.

            // Any properties that are read only need to have the cache set and be invalidated
            // Settable DP's can be set directly (and should be, for validation)

            //Set our internal change flag.
            //DP Invalidation callbacks can check for this flag to see if a change originated
            //from our IDocumentScrollInfo object or not and take the appropriate action.
            //(Usually, if the IDSI caused the change to a DP, the invalidated callback won't need to
            //update the associated property on the IDSI.)
            _internalIDSIChange = true;

            SetValue(ExtentWidthPropertyKey, _documentScrollInfo.ExtentWidth);
            SetValue(ExtentHeightPropertyKey, _documentScrollInfo.ExtentHeight);
            SetValue(ViewportWidthPropertyKey, _documentScrollInfo.ViewportWidth);
            SetValue(ViewportHeightPropertyKey, _documentScrollInfo.ViewportHeight);

            if (HorizontalOffset != _documentScrollInfo.HorizontalOffset)
            {
                HorizontalOffset = _documentScrollInfo.HorizontalOffset;
            }

            if (VerticalOffset != _documentScrollInfo.VerticalOffset)
            {
                VerticalOffset = _documentScrollInfo.VerticalOffset;
            }

            // IDSI is 0-indexed
            SetValue(MasterPageNumberPropertyKey, _documentScrollInfo.FirstVisiblePageNumber + 1);

            // Convert IDocumentScrollInfo.Scale into 100-based percentage value for comparison.
            double scrollZoom = ScaleToZoom(_documentScrollInfo.Scale);
            if (Zoom != scrollZoom)
            {
                Zoom = scrollZoom;
            }

            if (MaxPagesAcross != _documentScrollInfo.MaxPagesAcross)
            {
                MaxPagesAcross = _documentScrollInfo.MaxPagesAcross;
            }

            //Reset our internal change flag.
            _internalIDSIChange = false;
        }

        /// <summary>
        /// Merely calls the base's InvalidatePageViews (which is protected).
        /// Used by our IDSI to keep the DPV collection in sync.
        /// </summary>
        internal void InvalidatePageViewsInternal()
        {
            //Our PageView collection has changed, set the flag.
            _pageViewCollectionChanged = true;
            InvalidatePageViews();
        }

        /// <summary>
        /// BringPointIntoView is called by the base if a selection goes outside of the bounds
        /// of the current TextView.  If this happens it is necessary to scroll our content
        /// in an attempt to make that selection point visible, thus moving the scope of the TextView
        /// and allowing selection to continue.
        /// </summary>
        /// <param name="point">The point to be brought into view.</param>
        /// <returns>Whether operation is pending or not.</returns>
        internal bool BringPointIntoView(Point point)
        {
            FrameworkElement grid = _documentScrollInfo as FrameworkElement;

            if (grid != null)
            {
                //Calculate the bounds of the DocumentGrid relative to the bounds of DocumentViewer
                Transform tr = this.TransformToDescendant(grid) as Transform;
                Rect gridRect = Rect.Transform(new Rect(grid.RenderSize),
                                    tr.Value);
                double verticalOffset = VerticalOffset;
                double horizontalOffset = HorizontalOffset;

                //Scroll the point into view Vertically.
                if (point.Y > gridRect.Y + gridRect.Height)
                {
                    verticalOffset += (point.Y - (gridRect.Y + gridRect.Height));
                }
                else if (point.Y < gridRect.Y)
                {
                    verticalOffset -= (gridRect.Y - point.Y);
                }

                //Scroll the point into view Horizontally.
                if (point.X < gridRect.X)
                {
                    horizontalOffset -= (gridRect.X - point.X);
                }
                else if (point.X > gridRect.X + gridRect.Width)
                {
                    horizontalOffset += (point.X - (gridRect.X + gridRect.Width));
                }

                VerticalOffset = Math.Max(verticalOffset, 0.0);
                HorizontalOffset = Math.Max(horizontalOffset, 0.0);
            }
            return false;
        }

        #endregion Internal Methods

        #region Internal Properties

        /// <summary>
        /// Internally exposes our TextEditor's Selection, for use
        /// by Annotations code.
        /// </summary>
        internal ITextSelection TextSelection
        {
            get
            {
                if (TextEditor != null)
                {
                    return TextEditor.Selection;
                }
                else
                {
                    return null;
                }
            }
        }


        /// <summary>
        /// Internally exposes our IDocumentScrollInfo, for use
        /// by Annotations code.
        /// </summary>
        internal IDocumentScrollInfo DocumentScrollInfo
        {
            get
            {
                return _documentScrollInfo;
            }
        }

        /// <summary>
        /// Internally exposes out ScrollViewer, for use in DocumentViewerAutomationPeer.
        /// </summary>
        internal ScrollViewer ScrollViewer
        {
            get
            {
                return _scrollViewer;
            }
        }

        #endregion InternalProperties



        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        #region Commands
        /// <summary>
        /// Set up our Command bindings
        /// </summary>
        /// <summary>
        /// Set up our RoutedUICommand bindings
        /// </summary>
        private static void CreateCommandBindings()
        {
            // Create our generic ExecutedRoutedEventHandler.
            ExecutedRoutedEventHandler executeHandler = new ExecutedRoutedEventHandler(ExecutedRoutedEventHandler);

            // Create our generic QueryEnabledStatusHandler
            CanExecuteRoutedEventHandler queryEnabledHandler = new CanExecuteRoutedEventHandler(QueryEnabledHandler);

            //
            // Command: ViewThumbnails
            //          Tells DocumentViewer to display thumbnails.
            _viewThumbnailsCommand = new RoutedUICommand(SR.Get(SRID.DocumentViewerViewThumbnailsCommandText),
                "ViewThumbnailsCommand",
                typeof(DocumentViewer),
                null);

            CommandHelpers.RegisterCommandHandler( typeof(DocumentViewer),
                _viewThumbnailsCommand,
                executeHandler,
                queryEnabledHandler);
                //no key gesture

            //
            // Command: FitToWidth
            //          Tells DocumentViewer to zoom to the document width.
            _fitToWidthCommand = new RoutedUICommand(
                SR.Get(SRID.DocumentViewerViewFitToWidthCommandText),
                "FitToWidthCommand",
                typeof(DocumentViewer),
                null);

            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewer),
                _fitToWidthCommand,
                executeHandler,
                queryEnabledHandler,
                new KeyGesture(Key.D2, ModifierKeys.Control));

            //
            // Command: FitToHeight
            //          Tells DocumentViewer to zoom to the document height.
            _fitToHeightCommand = new RoutedUICommand(
                SR.Get(SRID.DocumentViewerViewFitToHeightCommandText),
                "FitToHeightCommand",
                typeof(DocumentViewer),
                null);

            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewer),
                _fitToHeightCommand,
                executeHandler,
                queryEnabledHandler);
                //no key gesture

            //
            // Command: MaxPagesAcross
            //          Sets the MaxPagesAcross to the value provided.
            _fitToMaxPagesAcrossCommand = new RoutedUICommand(
                SR.Get(SRID.DocumentViewerViewFitToMaxPagesAcrossCommandText),
                "FitToMaxPagesAcrossCommand",
                typeof(DocumentViewer),
                null);

            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewer),
                _fitToMaxPagesAcrossCommand,
                executeHandler,
                queryEnabledHandler);
                //no key gesture

            #region Library Commands

            // Command: ApplicationCommands.Find - Ctrl+F
            //          Invokes DocumentViewer's Find dialog.
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewer),
                ApplicationCommands.Find,
                executeHandler,
                queryEnabledHandler);

            //
            // Command: ComponentCommands.ScrollPageUp - PageUp
            //          Causes DocumentViewer to scroll a Viewport up.
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewer),
                ComponentCommands.ScrollPageUp,
                executeHandler,
                queryEnabledHandler,
                Key.PageUp);

            //
            // Command: ComponentCommands.ScrollPageDown - PageDown
            //          Causes DocumentViewer to scroll a Viewport down.
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewer),
                ComponentCommands.ScrollPageDown,
                executeHandler,
                queryEnabledHandler,
                Key.PageDown);

            //
            // Command: ComponentCommands.ScrollPageLeft
            //          Causes DocumentViewer to scroll a Viewport to the left.
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewer),
                ComponentCommands.ScrollPageLeft,
                executeHandler,
                queryEnabledHandler);
                //no key gesture

            //
            // Command: ComponentCommands.ScrollPageRight
            //          Causes DocumentViewer to scroll a Viewport to the right.
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewer),
                ComponentCommands.ScrollPageRight,
                executeHandler,
                queryEnabledHandler);
                //no key gesture

            //
            // Command: ComponentCommands.MoveUp - Up
            //          Causes DocumentViewer to scroll the Viewport up by 16px.
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewer),
                ComponentCommands.MoveUp,
                executeHandler,
                queryEnabledHandler,
                Key.Up);

            //
            // Command: ComponentCommands.MoveDown - Down
            //          Causes DocumentViewer to scroll the Viewport down by 16px.
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewer),
                ComponentCommands.MoveDown,
                executeHandler,
                queryEnabledHandler,
                Key.Down);

            //
            // Command: ComponentCommands.MoveLeft - Left
            //          Causes DocumentViewer to scroll a Viewport left by 16px.
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewer),
                ComponentCommands.MoveLeft,
                executeHandler,
                queryEnabledHandler,
                Key.Left);

            //
            // Command: ComponentCommands.MoveRight - Right
            //          Causes DocumentViewer to scroll a Viewport right by 16px.
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewer),
                ComponentCommands.MoveRight,
                executeHandler,
                queryEnabledHandler,
                Key.Right);

            //
            // Command: NavigationCommands.Zoom
            //          Sets DocumentViewer's Zoom to the specified level.
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewer),
                NavigationCommands.Zoom,
                executeHandler,
                queryEnabledHandler);
                //no key gesture

            //
            // Command: NavigationCommands.IncreaseZoom
            //          Causes DocumentViewer to zoom in on the content.
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewer),
                NavigationCommands.IncreaseZoom,
                executeHandler,
                queryEnabledHandler,
                // Ctrl+Numpad '+'
                new KeyGesture(Key.Add, ModifierKeys.Control),
                // Ctrl+Numpad '+' (In case shift is held down)
                new KeyGesture(Key.Add, ModifierKeys.Shift | ModifierKeys.Control),
                // Ctrl+'+'
                new KeyGesture(Key.OemPlus, ModifierKeys.Control),
                // Ctrl+'+' (In case shift is held down)
                new KeyGesture(Key.OemPlus, ModifierKeys.Shift | ModifierKeys.Control));

            //
            // Command: NavigationCommands.DecreaseZoom
            //          Causes DocumentViewer to zoom out of the content.
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewer),
                NavigationCommands.DecreaseZoom,
                executeHandler,
                queryEnabledHandler,
                // Ctrl+Numpad '-'
                new KeyGesture(Key.Subtract, ModifierKeys.Control),
                // Ctrl+Numpad '-' (In case shift is held down)
                new KeyGesture(Key.Subtract, ModifierKeys.Shift | ModifierKeys.Control),
                // Ctrl+'-'
                new KeyGesture(Key.OemMinus, ModifierKeys.Control),
                // Ctrl+'-' (In case shift is held down)
                new KeyGesture(Key.OemMinus, ModifierKeys.Shift | ModifierKeys.Control));

            // Command: NavigationCommands.PreviousPage
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewer),
                NavigationCommands.PreviousPage,
                executeHandler,
                queryEnabledHandler,
                new KeyGesture(Key.PageUp, ModifierKeys.Control));

            // Command: NavigationCommands.NextPage
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewer),
                NavigationCommands.NextPage,
                executeHandler,
                queryEnabledHandler,
                new KeyGesture(Key.PageDown, ModifierKeys.Control));

            // Command: NavigationCommands.FirstPage
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewer),
                NavigationCommands.FirstPage,
                executeHandler,
                queryEnabledHandler,
                new KeyGesture(Key.Home, ModifierKeys.Control));

            // Command: NavigationCommands.FirstPage
            CommandHelpers.RegisterCommandHandler(typeof(DocumentViewer),
                NavigationCommands.LastPage,
                executeHandler,
                queryEnabledHandler,
                new KeyGesture(Key.End, ModifierKeys.Control));

            #endregion Library Commands

            //Register input bindings for keyboard shortcuts that require
            //Command Parameters:

            //Zoom 100%: Requires a CommandParameter of 100.0 with the Zoom Command.
            //Bound to Ctrl+1.
            InputBinding zoom100InputBinding =
                new InputBinding(NavigationCommands.Zoom,
                new KeyGesture(Key.D1, ModifierKeys.Control));
            zoom100InputBinding.CommandParameter = 100.0;

            CommandManager.RegisterClassInputBinding(typeof(DocumentViewer),
                zoom100InputBinding);

            //Whole Page: Requires a CommandParameter of 1 with the FitToMaxPagesAcross Command.
            //Bound to Ctrl+3.
            InputBinding wholePageInputBinding =
                            new InputBinding(DocumentViewer.FitToMaxPagesAcrossCommand,
                            new KeyGesture(Key.D3, ModifierKeys.Control));
            wholePageInputBinding.CommandParameter = 1;

            CommandManager.RegisterClassInputBinding(typeof(DocumentViewer),
                wholePageInputBinding);

            //Two Pages: Requires a CommandParameter of 2 with the FitToMaxPagesAcross Command.
            //Bound to Ctrl+4.
            InputBinding twoPagesInputBinding =
                            new InputBinding(DocumentViewer.FitToMaxPagesAcrossCommand,
                            new KeyGesture(Key.D4, ModifierKeys.Control));
            twoPagesInputBinding.CommandParameter = 2;

            CommandManager.RegisterClassInputBinding(typeof(DocumentViewer),
                twoPagesInputBinding);
        }

        /// <summary>
        /// Central handler for QueryEnabled events fired by Commands directed at DocumentViewer.
        /// </summary>
        /// <param name="target">The target of this Command, expected to be DocumentViewer</param>
        /// <param name="args">The event arguments for this event.</param>
        private static void QueryEnabledHandler(object target, CanExecuteRoutedEventArgs args)
        {
            DocumentViewer dv = target as DocumentViewer;
            Invariant.Assert(dv != null, "Target of QueryEnabledEvent must be DocumentViewer.");
            Invariant.Assert(args != null, "args cannot be null.");

            // If the target is not a DocumentViewer we silently return (but note that
            // we have Asserted this above.)
            if (dv == null)
            {
                return;
            }

            // Mark this event as handled so that the CanExecute handler on the base
            // doesn't override our settings.
            args.Handled = true;

            // Now we set the IsEnabled flag based on the Command that fired this event
            // and the current state of DocumentViewer.
            if (args.Command == ViewThumbnailsCommand ||
                args.Command == FitToWidthCommand ||
                args.Command == FitToHeightCommand ||
                args.Command == FitToMaxPagesAcrossCommand ||
                args.Command == NavigationCommands.Zoom)
            {
                // Fit and Zoom operations are always enabled.
                args.CanExecute = true;
            }
            else if (args.Command == ApplicationCommands.Find)
            {
                //Find can only operate if we have a TextEditor.
                args.CanExecute = dv.TextEditor != null;
            }
            else if (args.Command == ComponentCommands.ScrollPageUp ||
                args.Command == ComponentCommands.MoveUp)
            {
                // Enabling the Move/Scroll Up Commands is tied to the
                // state of the CanMoveUp property.
                args.CanExecute = dv.CanMoveUp;
            }
            else if (args.Command == ComponentCommands.ScrollPageDown ||
                args.Command == ComponentCommands.MoveDown)
            {
                // Enabling the Move/Scroll Down Commands is tied to the
                // state of the CanMoveDown property.
                args.CanExecute = dv.CanMoveDown;
            }
            else if (args.Command == ComponentCommands.ScrollPageLeft ||
                args.Command == ComponentCommands.MoveLeft)
            {
                // Enabling the Move/Scroll Left Commands is tied to the
                // state of the CanMoveLeft property.
                args.CanExecute = dv.CanMoveLeft;
            }
            else if (args.Command == ComponentCommands.ScrollPageRight ||
                args.Command == ComponentCommands.MoveRight)
            {
                // Enabling the Move/Scroll Right Commands is tied to the
                // state of the CanMoveRight property.
                args.CanExecute = dv.CanMoveRight;
            }
            else if (args.Command == NavigationCommands.IncreaseZoom)
            {
                // Zooming in is only allowed if DocumentViewer has a valid Document assigned
                // and can increase zoom.
                args.CanExecute = dv.CanIncreaseZoom;
            }
            else if (args.Command == NavigationCommands.DecreaseZoom)
            {
                // Zooming out is only allowed if DocumentViewer has a valid Document assigned
                // and can decrease zoom.
                args.CanExecute = dv.CanDecreaseZoom;
            }
            else if (args.Command == NavigationCommands.PreviousPage
                || args.Command == NavigationCommands.FirstPage)
            {
                // Enabling the PreviousPage and FirstPage Commands is tied
                // to the state of the CanGoToPreviousPage property.
                args.CanExecute = dv.CanGoToPreviousPage;
            }
            else if (args.Command == NavigationCommands.NextPage
                || args.Command == NavigationCommands.LastPage)
            {
                // Enabling the NextPage and LastPage Commands is tied
                // to the state of the CanGoToNextPage property.
                args.CanExecute = dv.CanGoToNextPage;
            }
            else if (args.Command == NavigationCommands.GoToPage)
            {
                // This command is always enabled as long as there is a document loaded.
                args.CanExecute = (dv.Document != null);
            }
            else
            {
                args.Handled = false;
                // If we get here then we missed a Command above.
                // We assert to indicate the failure.
                Invariant.Assert(false, "Command not handled in QueryEnabledHandler.");
            }
        }

        /// <summary>
        /// Central handler for all ExecuteEvents fired by Commands directed at DocumentViewer.
        /// </summary>
        /// <param name="target">The target of this Command, expected to be DocumentViewer.</param>
        /// <param name="args">The event arguments associated with this event.</param>
        private static void ExecutedRoutedEventHandler(object target, ExecutedRoutedEventArgs args)
        {
            DocumentViewer dv = target as DocumentViewer;
            Invariant.Assert(dv != null, "Target of ExecuteEvent must be DocumentViewer.");
            Invariant.Assert(args != null, "args cannot be null.");

            // If the target is not a DocumentViewer we silently return (but note that
            // we have Asserted this above.)
            if (dv == null)
            {
                return;
            }

            // Now we execute the method corresponding to the Command that fired this event;
            // each Command has its own protected virtual method that performs the operation
            // corresponding to the Command.
            if (args.Command == ViewThumbnailsCommand)
            {
                dv.OnViewThumbnailsCommand();
            }
            else if (args.Command == FitToWidthCommand)
            {
                dv.OnFitToWidthCommand();
            }
            else if (args.Command == FitToHeightCommand)
            {
                dv.OnFitToHeightCommand();
            }
            else if (args.Command == FitToMaxPagesAcrossCommand)
            {
                DoFitToMaxPagesAcross(dv, args.Parameter);
            }
            else if (args.Command == ApplicationCommands.Find)
            {
                dv.OnFindCommand();
            }
            else if (args.Command == ComponentCommands.ScrollPageUp)
            {
                dv.OnScrollPageUpCommand();
            }
            else if (args.Command == ComponentCommands.ScrollPageDown)
            {
                dv.OnScrollPageDownCommand();
            }
            else if (args.Command == ComponentCommands.ScrollPageLeft)
            {
                dv.OnScrollPageLeftCommand();
            }
            else if (args.Command == ComponentCommands.ScrollPageRight)
            {
                dv.OnScrollPageRightCommand();
            }
            else if (args.Command == ComponentCommands.MoveUp)
            {
                dv.OnMoveUpCommand();
            }
            else if (args.Command == ComponentCommands.MoveDown)
            {
                dv.OnMoveDownCommand();
            }
            else if (args.Command == ComponentCommands.MoveLeft)
            {
                dv.OnMoveLeftCommand();
            }
            else if (args.Command == ComponentCommands.MoveRight)
            {
                dv.OnMoveRightCommand();
            }
            else if (args.Command == NavigationCommands.Zoom)
            {
                DoZoom(dv, args.Parameter);
            }
            else if (args.Command == NavigationCommands.DecreaseZoom)
            {
                dv.DecreaseZoom();
            }
            else if (args.Command == NavigationCommands.IncreaseZoom)
            {
                dv.IncreaseZoom();
            }
            else if (args.Command == NavigationCommands.PreviousPage)
            {
                dv.PreviousPage();
            }
            else if (args.Command == NavigationCommands.NextPage)
            {
                dv.NextPage();
            }
            else if (args.Command == NavigationCommands.FirstPage)
            {
                dv.FirstPage();
            }
            else if (args.Command == NavigationCommands.LastPage)
            {
                dv.LastPage();
            }
            else
            {
                Invariant.Assert(false, "Command not handled in ExecutedRoutedEventHandler.");
            }
        }

        /// <summary>
        /// Helper for the FitToMaxPagesAcross Command, called from ExecutedRoutedEventHandler.
        /// Verifies that the data passed into ExecutedRoutedEventHandler is valid for a column count
        /// and sets the MaxPagesAcross property appropriately.
        /// </summary>
        /// <param name="dv">the DocumentViewer that received the command</param>
        /// <param name="data">the data associated with this command</param>
        private static void DoFitToMaxPagesAcross(DocumentViewer dv, object data)
        {
            // Check that args is valid
            if (data != null)
            {
                int columnValue = 0;
                bool isValidArg = true;

                // If data is an int, then cast
                if (data is int)
                {
                    columnValue = (int)data;
                }
                // If args.Data is a string, then parse
                else if (data is string)
                {
                    try
                    {
                        columnValue = System.Convert.ToInt32((string)data, CultureInfo.CurrentCulture);
                    }
                    // Catch only the expected parse exceptions
                    catch (ArgumentNullException)
                    {
                        isValidArg = false;
                    }
                    catch (FormatException)
                    {
                        isValidArg = false;
                    }
                    catch (OverflowException)
                    {
                        isValidArg = false;
                    }
                }

                // Argument wasn't a valid int, throw an exception.
                if (!isValidArg)
                {
                    throw new ArgumentException(SR.Get(SRID.DocumentViewerArgumentMustBeInteger), "data");
                }

                dv.OnFitToMaxPagesAcrossCommand(columnValue);
            }
            else
            {
                throw new ArgumentNullException("data");
            }
        }

        /// <summary>
        /// Helper for the Zoom Command, called from ExecutedRoutedEventHandler.
        /// Verifies that the data passed into ExecutedRoutedEventHandler is valid for a zoom factor
        /// and sets the Zoom property appropriately.
        /// </summary>
        /// <param name="dv">The DocumentViewer that recieved the command</param>
        /// <param name="data">the data associated with this command</param>
        private static void DoZoom(DocumentViewer dv, object data)
        {
            // Check that args is valid
            if (data != null)
            {
                // If a ZoomConverter doesn't exist, create one.
                if (dv._zoomPercentageConverter == null)
                {
                    dv._zoomPercentageConverter = new ZoomPercentageConverter();
                }

                // Use ZoomConverter to convert argument to zoom value.
                // We use InvariantCulture because the Command arguments are typically
                // defined in XAML or code, which is culture invariant.
                object zoomValue = dv._zoomPercentageConverter.ConvertBack(data, typeof(double),
                    null, CultureInfo.InvariantCulture);

                // Argument wasn't a valid percent, throw an exception.
                if (zoomValue == DependencyProperty.UnsetValue)
                {
                    throw new ArgumentException(SR.Get(SRID.DocumentViewerArgumentMustBePercentage), "data");
                }
                dv.Zoom = (double)zoomValue;
            }
            else
            {
                throw new ArgumentNullException("data");
            }
        }

        #endregion Commands

        /// <summary>
        /// Register our properties' metadata so that our DependencyProperties function.
        /// </summary>
        private static void RegisterMetadata()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DocumentViewer), new FrameworkPropertyMetadata(typeof(DocumentViewer)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(DocumentViewer));
        }

        /// <summary>
        /// Initializes our DocumentScrollInfo.
        /// </summary>
        private void SetUp()
        {
            // Enable selection.
            IsSelectionEnabled = true;

            // Set the TextBox.AcceptsReturn flag -- this will disable the "select everything on focus"
            // behavior of the TextEditor.
            // We reset the TextBox.AcceptsTab property to keep the TextEditor from eating Tab.
            SetValue(TextBox.AcceptsTabProperty, false);

            // Construct the DocumentGrid.
            CreateIDocumentScrollInfo();
        }

        /// <summary>
        /// CreateIDocumentScrollInfo instantiates our IDocumentScrollInfo control
        /// and sets/resets default properties.
        /// </summary>
        private void CreateIDocumentScrollInfo()
        {
            if (_documentScrollInfo == null)
            {
                // Construct IDocumentScrollInfo (DocumentGrid).
                _documentScrollInfo = new DocumentGrid();
                _documentScrollInfo.DocumentViewerOwner = this;

                //If IDocumentScrollInfo is a FrameworkElement we can give it a
                //Name for automation.
                FrameworkElement fe = _documentScrollInfo as FrameworkElement;
                if (fe != null)
                {
                    fe.Name = "DocumentGrid";
                    fe.Focusable = false;

                    //We don't allow Tabbing to the IDocumentScrollInfo --
                    //The ScrollViewer parent is what is tabbed to.
                    fe.SetValue(KeyboardNavigation.IsTabStopProperty, false);

                    TextEditorRenderScope = fe;
                }
            }

            //Assign our content to the IDSI.
            AttachContent();

            //Give the IDocumentScrollInfo object default values for important properties.
            _documentScrollInfo.VerticalPageSpacing = VerticalPageSpacing;
            _documentScrollInfo.HorizontalPageSpacing = HorizontalPageSpacing;
        }

        /// <summary>
        /// Assigns our current Document to our IDSI and gives it a reference to our TextEditor.
        /// </summary>
        private void AttachContent()
        {
            _documentScrollInfo.Content = (Document != null) ? Document.DocumentPaginator as DynamicDocumentPaginator : null;
            IsSelectionEnabled = true;
        }

        /// <summary>
        /// FindContentHost does 2 things:
        ///  - It finds "marked" elements (elements with ContentHost attached properties)
        ///    in the current Visual Tree.
        ///  - It takes these elements and populates them with the proper UI (for Content)
        /// </summary>
        /// <exception cref="NotSupportedException">There must be a ScrollViewer in
        /// DocumentViewer's visual tree which has the Name PART_ContentHost.</exception>
        private void FindContentHost()
        {
            // Find the "special" element in the tree marked as the
            //   ContentHost.  This element must exist or we throw.
            ScrollViewer contentHost = this.Template.FindName(_contentHostName, this) as ScrollViewer;

            // Make sure contentHost exists.  This wouldn't be much of a DocumentViewer if it didn't,
            //   since we need someplace to throw our IDocumentScrollInfo so we can display documents.
            // Throw an exception if it doesn't exist.
            if (contentHost == null)
            {
                throw new NotSupportedException(SR.Get(SRID.DocumentViewerStyleMustIncludeContentHost));
            }

            _scrollViewer = contentHost;
            _scrollViewer.Focusable = false;

            Invariant.Assert(_documentScrollInfo != null, "IDocumentScrollInfo cannot be null.");
            //Make the IDSI the child of the ScrollViewer.
            _scrollViewer.Content = _documentScrollInfo;
            _scrollViewer.ScrollInfo = _documentScrollInfo;

            // Set IDocumentScrollInfo's content if its content is invalid.
            if (_documentScrollInfo.Content != Document)
            {
                AttachContent();
            }
        }

        #region Find

        /// <summary>
        /// Instantiates the Find Toolbar and adds it to our Visual tree where appropriate.
        /// </summary>
        private void InstantiateFindToolBar()
        {
            //First, find the correct place to insert toolbar.

            // Location is defined by named element, FindToolbarHost.
            ContentControl findHost = this.Template.FindName(_findToolBarHostName, this) as ContentControl;

            // Only create and hook up the toolbar, if we found a place to put it.
            if (findHost != null)
            {
               if( _findToolbar == null )
               {
                    // create the new object
                    _findToolbar = new FindToolBar();

                    // add the event handlers
                    _findToolbar.FindClicked += new EventHandler(OnFindInvoked);

                    //set initial DocumentLoaded state.
                    _findToolbar.DocumentLoaded = (Document != null) ? true : false;
                }

                // Now insert the toolbar, if it isn't already parented elsewhere.
                // (It will have been disconnected from DocumentViewer on a Theme or
                // Template change.)
                if (!_findToolbar.IsAncestorOf(this))
                {
                    ((IAddChild)findHost).AddChild(_findToolbar);
                }
            }
        }

        /// <summary>
        /// Invoked when the "Find" button in the Find Toolbar is clicked.
        /// This method invokes the actual Find process.
        /// </summary>
        /// <param name="sender">The object that sent this event</param>
        /// <param name="e">The Click Events associated with this event</param>
        private void OnFindInvoked(object sender, EventArgs e)
        {
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXFindBegin);
            try
            {
                if (_findToolbar != null && TextEditor != null)
                {
                    ITextRange findResult = Find(_findToolbar);

                    // If we found something, select it.
                    if ((findResult != null) && (!findResult.IsEmpty))
                    {
                        //Give ourselves focus, this ensures that the selection
                        //will be made visible after it's made.
                        this.Focus();

                        if (_documentScrollInfo != null)
                        {
                            _documentScrollInfo.MakeSelectionVisible();
                        }

                        //Put the focus back on the Find Toolbar's TextBox to search again.
                        _findToolbar.GoToTextBox();
                    }
                    else
                    {
                        // No, we did not find anything.  Alert the user.

                        // build our message string.
                        string messageString = _findToolbar.SearchUp ?
                            SR.Get(SRID.DocumentViewerSearchUpCompleteLabel) :
                            SR.Get(SRID.DocumentViewerSearchDownCompleteLabel);

                        messageString = String.Format(
                            CultureInfo.CurrentCulture,
                            messageString,
                            _findToolbar.SearchText);

                        Window wnd = null;
                        if (Application.Current != null && Application.Current.CheckAccess())
                        {
                            wnd = Application.Current.MainWindow;
                        }

                        MS.Internal.PresentationFramework.SecurityHelper.ShowMessageBoxHelper(
                            wnd,
                            messageString,
                            SR.Get(SRID.DocumentViewerSearchCompleteTitle),
                            MessageBoxButton.OK,
                            MessageBoxImage.Asterisk);
                    }
                }
            }
            finally
            {
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXFindEnd);
            }
        }

        /// <summary>
        /// This is just a private convenience method for handling the Find command in a
        /// localized place.
        /// </summary>
        private void GoToFind()
        {
            if (_findToolbar != null)
            {
                _findToolbar.GoToTextBox();
            }
        }


        /// <summary>
        /// This is just a private convenience method to handle those keyboard
        /// shortcuts related to the Find Command.
        /// localized place.
        /// </summary>
        private KeyEventArgs ProcessFindKeys(KeyEventArgs e)
        {
            if (_findToolbar == null || Document == null)
            {
                // Short-circuit. Find isn't enabled,
                // just exit.
                return e;
            }

            // F3 -- Invoke Find
            if (e.Key == Key.F3)
            {
                e.Handled = true;

                //If the Shift key is also pressed, then search up.
                _findToolbar.SearchUp = ((e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift);

                OnFindInvoked(this, EventArgs.Empty);
            }

            return e;
        }

        #endregion Find

        /// <summary>
        /// Find the location in the zoomLevelCollection such that it is the closest
        ///  zoomLevel equal to or lower than the current zoom.
        /// </summary>
        private void FindZoomLevelIndex()
        {
            // Ensure the list of zoom levels is created.
            if (_zoomLevelCollection != null)
            {
                // If the index is not in a valid location, update it to the list start.
                if ((_zoomLevelIndex < 0) || (_zoomLevelIndex >= _zoomLevelCollection.Length))
                {
                    _zoomLevelIndex = 0;
                    _zoomLevelIndexValid = false;
                }

                // Check if the current index is in the correct location in the list.
                if (!_zoomLevelIndexValid)
                {
                    // Since the index is not in the correct location in the list
                    //  (ie the Zoom was set by another means), then
                    //   search the list of possible zooms for the correct location.
                    double currentZoom = Zoom;

                    // Currently this search is done using a linear method which is
                    // fine given the small size of the list.  If we increase the list
                    // size, then a simple binary search could increaes performance.

                    // Search the list of zoom's from highest to lowest until the
                    //   appropriate "floor" level (level equal to, or
                    //   lower than the current zoom) is found.
                    int loopIndex;
                    for (loopIndex = 0; loopIndex < _zoomLevelCollection.Length - 1; loopIndex++)
                    {
                        if (currentZoom >= _zoomLevelCollection[loopIndex])
                        {
                            // Closest equal or lower match found
                            break;
                        }
                    }
                    // Assign the current zoom level, and mark that our index is valid
                    //   (for future Increase / Decrease zoom calls).
                    _zoomLevelIndex = loopIndex;
                    _zoomLevelIndexValid = true;
                }
            }
        }

        /// <summary>
        /// Determines if the parameter represents a valid double (that is a value other
        /// than NaN, PositiveInfinity, or NegativeInfinity).
        /// </summary>
        /// <param name="value">The double value to be checked</param>
        /// <returns>True if the double value is valid, false otherwise.</returns>
        private static bool DoubleValue_Validate(object value)
        {
            // Place this helper method
            // in a location with standard utilities.

            bool ok;

            // Ensure value is double
            if (value is double)
            {
                double checkValue = (double)value;

                // Check if double is within an assumed range
                if ((double.IsNaN(checkValue)) ||
                    (double.IsInfinity(checkValue)))
                {
                    ok = false;
                }
                else
                {
                    ok = true;
                }
            }
            else
            {
                ok = false;
            }
            return ok;
        }

        /// <summary>
        /// Converts an IDocumentScrollInfo's Scale value to a Zoom
        /// </summary>
        /// <param name="scale">A valid IDocumentScrollInfo.Scale value.</param>
        /// <returns>A Zoom value</returns>
        private static double ScaleToZoom(double scale)
        {
            return scale * 100.0;
        }

        /// <summary>
        /// Converts a Zoom value to an IDocumentScrollInfo.Scale value.
        /// </summary>
        /// <param name="zoom">A valid Zoom value.</param>
        /// <returns>An IDocumentScrollInfo.Scale value.</returns>
        private static double ZoomToScale(double zoom)
        {
            return zoom / 100.0;
        }

        #region DependencyProperties

        #region HorizontalOffset
        /// <summary>
        /// Validate the value passed in as a possible offset (either vertical or horizontal)
        /// </summary>
        /// <param name="value">A double representing the requested offset</param>
        /// <returns>True if the offset is valid, false otherwise.</returns>
        private static bool ValidateOffset(object value)
        {
            return DoubleValue_Validate(value) && ((double)value >= 0.0);
        }

        /// <summary>
        /// The HorizontalOffset has changed, and needs to be updated.
        /// </summary>
        private static void OnHorizontalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DocumentViewer dv = (DocumentViewer)d;
            double newOffset = (double) e.NewValue;

            // If the HorizontalOffset has changed and it isn't due to a change that originated
            // from our IDocumentScrollInfo object, then set the new offset value on the IDocumentScrollInfo.
            if (!dv._internalIDSIChange && (dv._documentScrollInfo != null))
            {
                dv._documentScrollInfo.SetHorizontalOffset(newOffset);
            }
            dv.SetValue(CanMoveLeftPropertyKey, newOffset > 0.0);
            dv.SetValue(CanMoveRightPropertyKey, newOffset < (dv.ExtentWidth - dv.ViewportWidth));
        }
        #endregion HorizontalOffset

        #region VerticalOffset
        /// <summary>
        /// The VerticalOffset has changed, and needs to be updated.
        /// </summary>
        private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DocumentViewer dv = (DocumentViewer)d;
            double newOffset = (double) e.NewValue;

            // If the VerticalOffset has changed and it isn't due to a change that originated
            // from our IDocumentScrollInfo object, then set the new offset value on the IDocumentScrollInfo.
            if ( !dv._internalIDSIChange && (dv._documentScrollInfo != null))
            {
                dv._documentScrollInfo.SetVerticalOffset(newOffset);
            }
            dv.SetValue(CanMoveUpPropertyKey, newOffset > 0.0);
            dv.SetValue(CanMoveDownPropertyKey, newOffset < (dv.ExtentHeight - dv.ViewportHeight));
        }
        #endregion VerticalOffset

        #region ExtentWidth
        private static void OnExtentWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DocumentViewer dv = (DocumentViewer)d;
            dv.SetValue(CanMoveRightPropertyKey, dv.HorizontalOffset < ((double) e.NewValue - dv.ViewportWidth));
        }
        #endregion ExtentWidth

        #region ExtentHeight
        private static void OnExtentHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DocumentViewer dv = (DocumentViewer)d;
            dv.SetValue(CanMoveDownPropertyKey, dv.VerticalOffset < ((double) e.NewValue - dv.ViewportHeight));
        }
        #endregion ExtentHeight

        #region ViewportWidth
        private static void OnViewportWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DocumentViewer dv = (DocumentViewer)d;
            double newWidth = (double) e.NewValue;
            dv.SetValue(CanMoveRightPropertyKey, dv.HorizontalOffset < (dv.ExtentWidth - (double) e.NewValue));
        }
        #endregion ViewportWidth

        #region ViewportHeight
        private static void OnViewportHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DocumentViewer dv = (DocumentViewer)d;
            double newHeight = (double) e.NewValue;
            dv.SetValue(CanMoveDownPropertyKey, dv.VerticalOffset < (dv.ExtentHeight - newHeight));
        }
        #endregion ViewportHeight

        #region ShowPageBorders
        /// <summary>
        /// ShowPageBorders has changed, and needs to be updated.
        /// </summary>
        private static void OnShowPageBordersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DocumentViewer dv = (DocumentViewer) d;

            // If the ShowPageBorders has changed, then set the new value on the IDocumentScrollInfo.
            if (dv._documentScrollInfo != null)
            {
                dv._documentScrollInfo.ShowPageBorders = (bool) e.NewValue;
            }
        }
        #endregion ShowPageBorders

        #region Zoom
        private static object CoerceZoom(DependencyObject d, object value)
        {
            double checkValue = (double) value;
            if (checkValue < DocumentViewerConstants.MinimumZoom)
            {
                return DocumentViewerConstants.MinimumZoom;
            }

            if (checkValue > DocumentViewerConstants.MaximumZoom)
            {
                return DocumentViewerConstants.MaximumZoom;
            }

            return value;
        }

        /// <summary>
        /// The Zoom has changed, and needs to be updated.
        /// </summary>
        private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DocumentViewer dv = (DocumentViewer)d;

            // If the ZoomPercentage has changed, then update the IDocumentScrollInfo.
            if (dv._documentScrollInfo != null)
            {
                double newZoom = (double) e.NewValue;
                if (!dv._internalIDSIChange)
                {
                    //Perf Tracing - Mark Zoom Change Start
                    EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordXPS, EventTrace.Event.WClientDRXZoom, (int)newZoom);

                    // Set the new zoom scale on IDocumentScrollInfo
                    dv._documentScrollInfo.SetScale(DocumentViewer.ZoomToScale(newZoom));
                }

                dv.SetValue(CanIncreaseZoomPropertyKey, newZoom < DocumentViewerConstants.MaximumZoom);
                dv.SetValue(CanDecreaseZoomPropertyKey, newZoom > DocumentViewerConstants.MinimumZoom);

                // If the zoom was not set by Increase / DecreaseZoom commands,
                //    then invalidate the zoom level index.
                if (!dv._updatingInternalZoomLevel)
                {
                    dv._zoomLevelIndexValid = false;
                }
            }
        }
        #endregion ZoomPercentage

        #region MaxPagesAcross

        /// <summary>
        /// Validate the value passed in as a possible MaxPagesAcross
        /// </summary>
        /// <param name="value">An int MaxPagesAcross value</param>
        /// <returns>True if the value is valid, false otherwise.</returns>
        private static bool ValidateMaxPagesAcross(object value)
        {
            int checkValue = (int)value;

            return checkValue > 0 && checkValue <= DocumentViewerConstants.MaximumMaxPagesAcross;
        }

        /// <summary>
        /// The MaxPagesAcross has changed, and needs to be updated.
        /// </summary>
        private static void OnMaxPagesAcrossChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DocumentViewer dv = (DocumentViewer)d;

            //regardless of whether the value of MaxPagesAcross has actually changed.
            //Setting this property will cause content to reflow or rescale to fit and we
            //want this to happen even if the same number of columns will be visible.
            //As an example, if we're at 500% showing 1 page, setting MaxPagesAcross to 1 will
            //cause the Zoom to change to show precisely one column.
            if (!dv._internalIDSIChange)
            {
                dv._documentScrollInfo.SetColumns((int) e.NewValue);
            }
        }
        #endregion GridColumnCount

        #region VerticalPageSpacing
        /// <summary>
        /// Validate the value passed in as a possible PageSpacing, either vertical or horizontal
        /// </summary>
        /// <param name="value">A value representing the requested page spacing</param>
        /// <returns>True if the offset is valid, false otherwise.</returns>
        private static bool ValidatePageSpacing(object value)
        {
            return DoubleValue_Validate(value) && ((double)value >= 0.0);
        }

        /// <summary>
        /// The VerticalPageSpacing has changed, and needs to be updated.
        /// </summary>
        private static void OnVerticalPageSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DocumentViewer dv = (DocumentViewer)d;

            // If the VerticalPageSpacing has changed, then set the new value on IDocumentScrollInfo.
            if (dv._documentScrollInfo != null)
            {
                dv._documentScrollInfo.VerticalPageSpacing = (double) e.NewValue;
            }
        }
        #endregion VerticalPageSpacing

        #region HorizontalPageSpacing
        /// <summary>
        /// The HorizontalPageSpacing has changed, and needs to be updated.
        /// </summary>
        private static void OnHorizontalPageSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DocumentViewer dv = (DocumentViewer)d;

            // If the HorizontalPageSpacing has changed, then set the new value on IDocumentScrollInfo.
            if (dv._documentScrollInfo != null)
            {
                dv._documentScrollInfo.HorizontalPageSpacing = (double) e.NewValue;
            }
        }
        #endregion HorizontalPageSpacing

        #endregion Dependency Properties

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        #region Private Fields

        // UI Elements
        private IDocumentScrollInfo             _documentScrollInfo;
        private ScrollViewer                    _scrollViewer;
        private ZoomPercentageConverter         _zoomPercentageConverter;

        //Find ToolBar
        private FindToolBar _findToolbar;                           // The FindToolbar UI

        // Default values for DPs
        private const double                    _horizontalOffsetDefault = 0.0;
        private const double                    _verticalOffsetDefault = 0.0;
        private const double                    _extentWidthDefault = 0.0;
        private const double                    _extentHeightDefault = 0.0;
        private const double                    _viewportWidthDefault = 0.0;
        private const double                    _viewportHeightDefault = 0.0;
        private const bool                      _showPageBordersDefault = true;
        private const double                    _zoomPercentageDefault = 100.0;
        private const int                       _maxPagesAcrossDefault = 1;
        private const double                    _verticalPageSpacingDefault = 10.0;
        private const double                    _horizontalPageSpacingDefault = 10.0;
        private const bool                      _canMoveUpDefault = false;
        private const bool                      _canMoveDownDefault = false;
        private const bool                      _canMoveLeftDefault = false;
        private const bool                      _canMoveRightDefault = false;
        private const bool                      _canIncreaseZoomDefault = true;
        private const bool                      _canDecreaseZoomDefault = true;
 //       private const bool                      _isToolbarMaximizedDefault = true;

        // Our Commands, instantiated on a pay-for-play basis
        private static RoutedUICommand            _viewThumbnailsCommand;
        private static RoutedUICommand            _fitToWidthCommand;
        private static RoutedUICommand            _fitToHeightCommand;
        private static RoutedUICommand            _fitToMaxPagesAcrossCommand;

        // This list is assumed to be in decreasing order.
        private static double[] _zoomLevelCollection = {5000.0, 4000.0, 3200.0, 2400.0, 2000.0, 1600.0,
                                                       1200.0, 800.0, 400.0, 300.0, 200.0, 175.0, 150.0,
                                                       125.0, 100.0, 75.0, 66.0, 50.0, 33.0, 25.0, 10.0, 5.0};
        private int                             _zoomLevelIndex; // = 0
        private bool                            _zoomLevelIndexValid; // = false
        private bool                            _updatingInternalZoomLevel; // = false
        private bool                            _internalIDSIChange; // = false
        private bool                            _pageViewCollectionChanged; // = false
        private bool                            _firstDocumentAssignment = true;

        private const string                    _findToolBarHostName  = "PART_FindToolBarHost";
        private const string                    _contentHostName = "PART_ContentHost";

        #endregion

        #region DTypeThemeStyleKey

        // Returns the DependencyObjectType for the registered ThemeStyleKey's default
        // value. Controls will override this method to return approriate types.
        internal override DependencyObjectType DTypeThemeStyleKey
        {
            get { return _dType; }
        }

        private static DependencyObjectType _dType;

        #endregion DTypeThemeStyleKey
    }
}

