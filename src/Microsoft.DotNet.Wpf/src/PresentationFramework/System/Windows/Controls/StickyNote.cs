// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// In order to disable Presharp warning 6507 - Prefer 'string.IsNullOrEmpty(value)' over checks for null and/or emptiness,
// we have to disable warnings 1634 and 1691 to make the compiler happy first.
#pragma warning disable 1634, 1691

//
// Description: Implementation of StickyNoteControl control.
//
//              See spec at StickyNoteControlSpec.mht
//

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;                           // Assert
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Win32;              // SystemEvents
using MS.Internal;
using MS.Internal.Annotations.Component;
using MS.Internal.Controls.StickyNote;
using MS.Internal.Commands;
using MS.Internal.KnownBoxes;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Annotations;
using System.Windows.Automation;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Text;
using System.Text.RegularExpressions;
using MS.Utility;


namespace System.Windows.Controls
{
    /// <summary>
    /// The type of data handled by a StickyNoteControl.
    /// </summary>
    public enum StickyNoteType
    {
        /// <summary>
        /// Text StickyNote
        /// </summary>
        Text,
        /// <summary>
        /// Ink StickyNote
        /// </summary>
        Ink,
    }


    /// <summary>
    /// StickyNoteControl control intends to be an UI control for user to make annotation.
    /// </summary>
    [TemplatePart(Name = SNBConstants.c_CloseButtonId, Type = typeof(Button))]
    [TemplatePart(Name = SNBConstants.c_TitleThumbId, Type = typeof(Thumb))]
    [TemplatePart(Name = SNBConstants.c_BottomRightResizeThumbId, Type = typeof(Thumb))]
    [TemplatePart(Name = SNBConstants.c_ContentControlId, Type = typeof(ContentControl))]
    [TemplatePart(Name = SNBConstants.c_IconButtonId, Type = typeof(Button))]
    [TemplatePart(Name = SNBConstants.c_CopyMenuId, Type = typeof(MenuItem))]
    [TemplatePart(Name = SNBConstants.c_PasteMenuId, Type = typeof(MenuItem))]
    [TemplatePart(Name = SNBConstants.c_InkMenuId, Type = typeof(MenuItem))]
    [TemplatePart(Name = SNBConstants.c_SelectMenuId, Type = typeof(MenuItem))]
    [TemplatePart(Name = SNBConstants.c_EraseMenuId, Type = typeof(MenuItem))]
    public sealed partial class StickyNoteControl : Control,
        IAnnotationComponent
    {
        //-------------------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// The static constructor
        /// </summary>
        static StickyNoteControl()
        {
            Type owner = typeof(StickyNoteControl);

            // Register event handlers
            // 
            // We want to bring a note to front when the input device is pressed down on it at the first time.
            // StickyNote invokes PresentationContext.BringToFront method to achieve it.
            // Eventually the Annotation AdornerLayer will re-arrange index of its children to change their "Z-Order".
            // So, the BringToFront could temporarily remove StickyNote from the visual tree.
            // But the hosted InkCanvas might capture Stylus for collecting packets. If the host note is removed from the tree, the capture
            // will be released automatically which will mess up the InkCanvas' states.
            // So we have to do BringToFront before InkCanvas does capture. Adding a handler of PreviewStylusDownEvent fixes the problem.
            // Unfortunately we still need the existing handler of PreviewMouseDownEvent since there is no stylus event for text StickyNote.

            // FUTURE-2005/05/27-WAYNEZEN,
            // Regardsing to Flick, We should remove the listener for PreviewStylusDownEvent if we turn InkCanvas EditingMode to None
            // when StickyNote losts the focus. The line needs to be revisited.
            EventManager.RegisterClassHandler(owner, Stylus.PreviewStylusDownEvent, new StylusDownEventHandler(_OnPreviewDeviceDown<StylusDownEventArgs>));
            EventManager.RegisterClassHandler(owner, Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(_OnPreviewDeviceDown<MouseButtonEventArgs>));
            EventManager.RegisterClassHandler(owner, Mouse.MouseDownEvent, new MouseButtonEventHandler(_OnDeviceDown<MouseButtonEventArgs>));
            EventManager.RegisterClassHandler(owner, ContextMenuService.ContextMenuOpeningEvent, new ContextMenuEventHandler(_OnContextMenuOpening));

            CommandHelpers.RegisterCommandHandler(typeof(StickyNoteControl), StickyNoteControl.DeleteNoteCommand,
                new ExecutedRoutedEventHandler(_OnCommandExecuted), new CanExecuteRoutedEventHandler(_OnQueryCommandEnabled));
            CommandHelpers.RegisterCommandHandler(typeof(StickyNoteControl), StickyNoteControl.InkCommand,
                new ExecutedRoutedEventHandler(_OnCommandExecuted), new CanExecuteRoutedEventHandler(_OnQueryCommandEnabled));

            //
            // set the main style CRK to the default theme style key
            //
            DefaultStyleKeyProperty.OverrideMetadata(owner, new FrameworkPropertyMetadata(
                new ComponentResourceKey(typeof(PresentationUIStyleResources), "StickyNoteControlStyleKey")));

            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(owner, new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));
            Control.IsTabStopProperty.OverrideMetadata(owner, new FrameworkPropertyMetadata(false));

            // Override the changed callback of the Foreground Property.
            ForegroundProperty.OverrideMetadata(
                    owner,
                    new FrameworkPropertyMetadata(new PropertyChangedCallback(_UpdateInkDrawingAttributes)));

            FontFamilyProperty.OverrideMetadata(owner, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnFontPropertyChanged)));
            FontSizeProperty.OverrideMetadata(owner, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnFontPropertyChanged)));
            FontStretchProperty.OverrideMetadata(owner, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnFontPropertyChanged)));
            FontStyleProperty.OverrideMetadata(owner, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnFontPropertyChanged)));
            FontWeightProperty.OverrideMetadata(owner, new FrameworkPropertyMetadata(new PropertyChangedCallback(OnFontPropertyChanged)));
        }

        /// <summary>
        /// This is an instance constructor for StickyNoteControl class.  Should not be used
        /// </summary>
        private StickyNoteControl() : this(StickyNoteType.Text)
        {
        }

        /// <summary>
        /// Creates an instance of StickyNoteControl that handles the specified type.
        /// </summary>
        /// <param name="type">the type of data to be handled by the StickyNoteControl</param>
        internal StickyNoteControl(StickyNoteType type) : base()
        {
            _stickyNoteType = type;
            SetValue(StickyNoteTypePropertyKey, type);
            InitStickyNoteControl();
        }

        #endregion // Constructors

        //-------------------------------------------------------------------------------
        //
        // Public Methods
        //
        //-------------------------------------------------------------------------------

        #region Public Methods


        /// <summary>
        /// Override OnApplyTemplate method. This method will ensure whether the created visual is one which
        /// StickyNoteControl expects. If so, the internal controls will be cached and Annotation will be applied
        /// to the UI status.
        /// </summary>
        /// <returns>Whether Visuals were added to the tree</returns>
        public override void OnApplyTemplate()
        {
            // No need for calling VerifyAccess since we call the method on the base here.
            base.OnApplyTemplate();

            // Ensure the type
            if (this.IsExpanded)
            {
                EnsureStickyNoteType();
            }

            // Setup the inner controls with the Annotation's data
            UpdateSNCWithAnnotation(SNCAnnotation.AllValues);

            // If the visual tree just gets populated from the new style, we have to add our event handles to the individual
            // elements.
            if (!this.IsExpanded)
            {
                Button button = GetIconButton();
                if (button != null)
                {
                    button.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnButtonClick));
                }
            }
            else
            {
                Button closeButton = GetCloseButton();
                if (closeButton != null)
                {
                    closeButton.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnButtonClick));
                }

                Thumb titleThumb = GetTitleThumb();
                if (titleThumb != null)
                {
                    titleThumb.AddHandler(Thumb.DragDeltaEvent, new DragDeltaEventHandler(OnDragDelta));
                    titleThumb.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(OnDragCompleted));
                }

                Thumb resizeThumb = GetResizeThumb();
                if (resizeThumb != null)
                {
                    resizeThumb.AddHandler(Thumb.DragDeltaEvent, new DragDeltaEventHandler(OnDragDelta));
                    resizeThumb.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(OnDragCompleted));
                }

                // Set up the bindings to the menuitems.
                SetupMenu();
            }
        }

        #endregion // Public Methods

        //-------------------------------------------------------------------------------
        //
        // Public Properties
        //
        //-------------------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// The property key of Author
        /// </summary>
        internal static readonly DependencyPropertyKey AuthorPropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "Author",
                        typeof(string),
                        typeof(StickyNoteControl),
                        new FrameworkPropertyMetadata(String.Empty));

        /// <summary>
        /// Author Dependency Property
        /// </summary>
        public static readonly DependencyProperty AuthorProperty = AuthorPropertyKey.DependencyProperty;

        /// <summary>
        /// Returns the author of the annotation the StickyNoteControl is editing.
        /// </summary>
        public String Author
        {
            get
            {
                return (String)GetValue(StickyNoteControl.AuthorProperty);
            }
        }

        /// <summary>
        /// Gets/Sets the expanded or minimized state of the StickyNoteControl.
        /// If Expanded=false, the bubble is in a minimized state.
        /// </summary>
        public static readonly DependencyProperty IsExpandedProperty =
                DependencyProperty.Register(
                        "IsExpanded",
                        typeof(bool),
                        typeof(StickyNoteControl),
                        new FrameworkPropertyMetadata(
                                BooleanBoxes.TrueBox,
                                new PropertyChangedCallback(_OnIsExpandedChanged)));

        /// <summary>
        /// Gets/Sets the expanded or minimized state of the StickyNoteControl.
        /// </summary>
        public bool IsExpanded
        {
            get { return (bool) GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        /// <summary>
        /// true - StickyNoteControl is active (i.e. has the focus or selection includes part of its anchor)
        ///The value of this property is set by the MarkedHighlightComponent which controls the SN state
        /// </summary>
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.RegisterAttached("IsActive",
                typeof(bool),
                typeof(StickyNoteControl),
                new FrameworkPropertyMetadata(BooleanBoxes.FalseBox,
                    FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// The state of StickyNoteControl - true : active, false:inactive
        /// </summary>
        public bool IsActive
        {
            get
            {
                return (bool)GetValue(StickyNoteControl.IsActiveProperty);
            }
        }

        /// <summary>
        /// If true, mouse is over the SticyNoteControl's anchor (which could be used to change its appearance
        /// in its style).
        /// </summary>
        internal static readonly DependencyPropertyKey IsMouseOverAnchorPropertyKey =
            DependencyProperty.RegisterReadOnly("IsMouseOverAnchor",
                typeof(bool),
                typeof(StickyNoteControl),
                    new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// If true, mouse is over the SticyNoteControl's anchor (which could be used to change its appearance
        /// in its style).
        /// </summary>
        public static readonly DependencyProperty IsMouseOverAnchorProperty = IsMouseOverAnchorPropertyKey.DependencyProperty;

        /// <summary>
        /// True if the mouse is over the StickyNote anchor, false otherwise
        /// </summary>
        public bool IsMouseOverAnchor
        {
            get
            {
               return  (bool) GetValue(StickyNoteControl.IsMouseOverAnchorProperty);
            }
        }

        /// <summary>
        ///     The DependencyProperty for the CaptionFontFamily property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      System Dialog Font
        /// </summary>
        public static readonly DependencyProperty CaptionFontFamilyProperty =
                DependencyProperty.Register(
                        "CaptionFontFamily",
                        typeof(FontFamily),
                        typeof(StickyNoteControl),
                        new FrameworkPropertyMetadata(
                                SystemFonts.MessageFontFamily,
                                FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        ///     The caption font family
        /// </summary>
        public FontFamily CaptionFontFamily
        {
            get { return (FontFamily) GetValue(CaptionFontFamilyProperty); }
            set { SetValue(CaptionFontFamilyProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the CaptionFontSize property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      System Dialog Font Size
        /// </summary>
        public static readonly DependencyProperty CaptionFontSizeProperty =
                DependencyProperty.Register(
                        "CaptionFontSize",
                        typeof(double),
                        typeof(StickyNoteControl),
                        new FrameworkPropertyMetadata(
                                SystemFonts.MessageFontSize,
                                FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        ///     The size of the caption font.
        /// </summary>
        public double CaptionFontSize
        {
            get { return (double) GetValue(CaptionFontSizeProperty); }
            set { SetValue(CaptionFontSizeProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the CaptionFontStretch property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      FontStretches.Normal
        /// </summary>
        public static readonly DependencyProperty CaptionFontStretchProperty =
                DependencyProperty.Register(
                        "CaptionFontStretch",
                        typeof(FontStretch),
                        typeof(StickyNoteControl),
                        new FrameworkPropertyMetadata(
                                FontStretches.Normal,
                                FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        ///     The stretch of the caption font.
        /// </summary>
        public FontStretch CaptionFontStretch
        {
            get { return (FontStretch) GetValue(CaptionFontStretchProperty); }
            set { SetValue(CaptionFontStretchProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the CaptionFontStyle property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      System Dialog Font Style
        /// </summary>
        public static readonly DependencyProperty CaptionFontStyleProperty =
                DependencyProperty.Register(
                        "CaptionFontStyle",
                        typeof(FontStyle), typeof(StickyNoteControl),
                        new FrameworkPropertyMetadata(
                                SystemFonts.MessageFontStyle,
                                FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        ///     The style of the caption font.
        /// </summary>
        public FontStyle CaptionFontStyle
        {
            get { return (FontStyle) GetValue(CaptionFontStyleProperty); }
            set { SetValue(CaptionFontStyleProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the CaptionFontWeight property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      System Dialog Font Weight
        /// </summary>
        public static readonly DependencyProperty CaptionFontWeightProperty =
                DependencyProperty.Register(
                        "CaptionFontWeight",
                        typeof(FontWeight),
                        typeof(StickyNoteControl),
                        new FrameworkPropertyMetadata(
                                SystemFonts.MessageFontWeight,
                                FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        ///     The weight or thickness of the caption font.
        /// </summary>
        public FontWeight CaptionFontWeight
        {
            get { return (FontWeight) GetValue(CaptionFontWeightProperty); }
            set { SetValue(CaptionFontWeightProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the PenWidth property.
        ///     Flags:              Can be used in style rules
        ///     Default Value:      The default width of System.Windows.Ink.DrawingAttributes.
        /// </summary>
        public static readonly DependencyProperty PenWidthProperty =
                DependencyProperty.Register(
                        "PenWidth",
                        typeof(double),
                        typeof(StickyNoteControl),
                        new FrameworkPropertyMetadata(
                                (new DrawingAttributes()).Width,
                                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                                new PropertyChangedCallback(_UpdateInkDrawingAttributes)));

        /// <summary>
        ///     The width of the pen for the ink StickyNoteControl.
        /// </summary>
        public double PenWidth
        {
            get { return (double) GetValue(PenWidthProperty); }
            set { SetValue(PenWidthProperty, value); }
        }

        /// <summary>
        /// StickyNoteType Property Key
        /// </summary>
        private static readonly DependencyPropertyKey StickyNoteTypePropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "StickyNoteType",
                        typeof(StickyNoteType),
                        typeof(StickyNoteControl),
                        new FrameworkPropertyMetadata(StickyNoteType.Text));

        /// <summary>
        /// StickyNoteType Dependency Property
        /// </summary>
        public static readonly DependencyProperty StickyNoteTypeProperty = StickyNoteTypePropertyKey.DependencyProperty;

        /// <summary>
        /// Gets StickyNoteType Property
        /// </summary>
        public StickyNoteType StickyNoteType
        {
            get{ return (StickyNoteType) GetValue(StickyNoteTypeProperty); }
        }

        /// <summary>
        /// Returns the Annotation this StickyNote is representing.
        /// </summary>
        public IAnchorInfo AnchorInfo
        {
            get
            {
                if (_attachedAnnotation != null)
                    return _attachedAnnotation;
                return null;
            }
        }

        #endregion  Public Properties

        //-------------------------------------------------------------------------------
        //
        // Public Commands
        //
        //-------------------------------------------------------------------------------

        #region Public Commands

        /// <summary>
        /// Delete a note
        /// </summary>
        public static readonly RoutedCommand DeleteNoteCommand = new RoutedCommand("DeleteNote", typeof(StickyNoteControl));

        /// <summary>
        /// Ink Mode
        /// </summary>
        public static readonly RoutedCommand InkCommand = new RoutedCommand("Ink", typeof(StickyNoteControl));

        #endregion Public Commands


        //-------------------------------------------------------------------------------
        //
        // Protected Methods
        //
        //-------------------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Called whenever the template changes.
        /// </summary>
        /// <param name="oldTemplate">Old template</param>
        /// <param name="newTemplate">New template</param>
        protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
            // No need for calling VerifyAccess since we call the method on the base here.
            base.OnTemplateChanged(oldTemplate, newTemplate);

            // If StickyNote's control template has been changed, we should invalidate current cached controls.
            ClearCachedControls();
        }

        /// <summary>
        /// Called whenever focus enters or leaves the StickyNoteControl.  This includes when focus
        /// is set/removed from any element within the StickyNoteControl.
        /// </summary>
        /// <param name="args">arguments describing the property change</param>
        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs args)
        {
            base.OnIsKeyboardFocusWithinChanged(args);

            // If we lost focus due to a context menu on some element within us,
            // we don't consider that a loss of focus.  We simply exit early.
            ContextMenu menu = Keyboard.FocusedElement as ContextMenu;
            if (menu != null)
            {
                if (menu.PlacementTarget != null && menu.PlacementTarget.IsDescendantOf(this))
                {
                        return;
                }
            }

            // Must update our anchor that we are now focused or not focused.
            _anchor.Focused = IsKeyboardFocusWithin;
        }

        /// <summary>
        /// An event announcing that the keyboard is focused on this bubble.
        /// </summary>
        /// <param name="args">>FocusChangedEvent Argument</param>
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs args)
        {
            // No need for calling VerifyAccess since we call the method on the base here.
            base.OnGotKeyboardFocus(args);

            // Dwayne's Input BC(#73066) changed the behavior of mouse button event.
            // So we could get GotKeyboardFocus event without the visual being set up.
            // Now, we verify the visual is ready by invoking ApplyTemplate.
            ApplyTemplate();

            // We are interested in the expanded note.
            if ( IsExpanded == true )
            {
                Invariant.Assert(Content != null);

                BringToFront();
                // If focus was set on us, we should set the focus on our inner control
                if ( args.NewFocus == this )
                {
                    UIElement innerControl = this.Content.InnerControl as UIElement;
                    Invariant.Assert(innerControl != null, "InnerControl is null or not a UIElement.");

                    // Don't mess with focus if its already on our inner control
                    if ( innerControl.IsKeyboardFocused == false )
                    {
                        // We should set the focus to the inner control after it is added the visual tree.
                        innerControl.Focus();
                    }
                }
            }
        }

        #endregion // Protected Methods

        //-------------------------------------------------------------------------------
        //
        // Internal Methods
        //
        //-------------------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Creates our inner control and adds it to the tree.  If the inner control
        /// already exists we verify its of the right type for our current data.
        /// This is called when the template is applied or when the annotation for this
        /// control is set.
        /// </summary>
        private void EnsureStickyNoteType()
        {
            UIElement contentContainer = GetContentContainer();

            // Check whether we need to recreate a content control based on the new type.
            if (_contentControl != null)
            {
                // Check if the type has been changed
                if (_contentControl.Type != _stickyNoteType)
                {
                    // Recreate the content control when the type has been changed.
                    DisconnectContent();
                    _contentControl = StickyNoteContentControlFactory.CreateContentControl(_stickyNoteType, contentContainer);
                    ConnectContent();
                }
            }
            else
            {
                _contentControl = StickyNoteContentControlFactory.CreateContentControl(_stickyNoteType, contentContainer);
                ConnectContent();
            }
        }

        /// <summary>
        /// When our inner control is changing, we disconnect from the existing one
        /// by unregistering for events and removing the control from the visual tree.
        /// </summary>
        private void DisconnectContent()
        {
            Invariant.Assert(Content != null, "Content is null.");

            // Unregister for all events and clear bindings
            StopListenToContentControlEvent();
            UnbindContentControlProperties();

            _contentControl = null;
        }

        /// <summary>
        /// When a new content control is created we register on different portions of
        /// the visual tree for certain events.
        /// </summary>
        private void ConnectContent()
        {
            Invariant.Assert(Content != null);

            // Set the default inking mode and attributes
            InkCanvas innerInkCanvas = Content.InnerControl as InkCanvas;
            if (innerInkCanvas != null)
            {
                // Create the event handlers we'll use for ink notes
                InitializeEventHandlers();

                // We set the value on the StickyNoteControl which eventually gets
                // set on the InkCanvas.  The property on the InkCanvas isn't a DP
                // we can't create a one-way binding as would be preferred.
                this.SetValue(InkEditingModeProperty, InkCanvasEditingMode.Ink);

                UpdateInkDrawingAttributes();
            }

            // Register for events and setup bindings
            StartListenToContentControlEvent();
            BindContentControlProperties();
        }

        /// <summary>
        /// Returns the Content element for this StickyNoteControl.  Should never
        /// be null when IsExpanded = true.
        /// </summary>
        internal StickyNoteContentControl Content
        {
            get
            {
                return _contentControl;
            }
        }

        /// <summary>
        /// Returns the button used to close the StickyNoteControl (it actually sets IsExpanded to false).
        /// </summary>
        private Button GetCloseButton()
        {
            return GetTemplateChild(SNBConstants.c_CloseButtonId) as Button;
        }

        /// <summary>
        /// Returns the button when the StickyNoteControl has IsExpanded=false.
        /// </summary>
        private Button GetIconButton()
        {
                return GetTemplateChild(SNBConstants.c_IconButtonId) as Button;
        }

        /// <summary>
        /// Returns the thumb that controls the dragging of the StickyNote as a whole.
        /// </summary>
        private Thumb GetTitleThumb()
        {
            return GetTemplateChild(SNBConstants.c_TitleThumbId) as Thumb;
        }

        /// <summary>
        /// Return the ContentControl viewer in the StickyNoteControl.
        /// </summary>
        private UIElement GetContentContainer()
        {
            return GetTemplateChild(SNBConstants.c_ContentControlId) as UIElement;
        }

        /// <summary>
        /// Return the resize thumb in the StickyNoteControl.
        /// </summary>
        private Thumb GetResizeThumb()
        {
            return GetTemplateChild(SNBConstants.c_BottomRightResizeThumbId) as Thumb;
        }


        #endregion Internal Methods

        //-------------------------------------------------------------------------------
        //
        // Internal Properties
        //
        //-------------------------------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// InkEditingMode Property Key
        /// </summary>
        private static readonly DependencyProperty InkEditingModeProperty =
                        DependencyProperty.Register(
                                "InkEditingMode",
                                typeof(InkCanvasEditingMode),
                                typeof(StickyNoteControl),
                                new FrameworkPropertyMetadata(
                                    InkCanvasEditingMode.None));

        /// <summary>
        /// Gets/Sets the dirty state of the control.
        /// </summary>
        private bool IsDirty
        {
            get
            {
                return _dirty;
            }
            set
            {
                _dirty = value;
            }
        }

        #endregion // Internal Properties

        //-------------------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Called when a StickyNoteControl's IsExpanded property changes.  Simply
        /// pass it on to the StickyNoteControl instance.
        /// </summary>
        private static void _OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StickyNoteControl snc = (StickyNoteControl)d;

            snc.OnIsExpandedChanged();
        }

        private static void OnFontPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            StickyNoteControl stickyNoteControl = (StickyNoteControl)d;
            if (stickyNoteControl.Content != null && stickyNoteControl.Content.Type != StickyNoteType.Ink)
            {
                FrameworkElement innerControl = stickyNoteControl.Content.InnerControl;
                if (innerControl != null)
                    innerControl.SetValue(e.Property, e.NewValue);
            }
        }

        /// <summary>
        /// The changed callback attached to the Foreground and PenWidth DPs
        /// </summary>
        private static void _UpdateInkDrawingAttributes(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Update the DrawingAttributes
            StickyNoteControl stickyNoteControl = (StickyNoteControl)d;
            stickyNoteControl.UpdateInkDrawingAttributes();

            if (e.Property == ForegroundProperty && stickyNoteControl.Content != null && stickyNoteControl.Content.Type != StickyNoteType.Ink)
            {
                FrameworkElement innerControl = stickyNoteControl.Content.InnerControl;
                if (innerControl != null)
                    innerControl.SetValue(ForegroundProperty, e.NewValue);
            }
        }


        // A class handler for TextBox.TextChanged events
        //      obj       -   the event sender
        //      args    -   Event argument
        private void OnTextChanged(object obj, TextChangedEventArgs args)
        {
            // We must update the annotation asynchronously because we can't Since Textbox doesn't allow any layout measurement during its content is changing, we have to do
            // the update asynchronously.  We also prevent updating the annotation when the content in the
            // textbox was changed due to a change in the annotation itself.
            if (!InternalLocker.IsLocked(LockHelper.LockFlag.DataChanged))
            {
                //fire trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.AnnotationTextChangedBegin);
                try
                {
                    AsyncUpdateAnnotation(XmlToken.Text);
                    //Dispatcher.BeginInvoke(DispatcherPriority.Normal, new DispatcherOperationCallback(AsyncUpdateAnnotation), XmlToken.Text);
                    IsDirty = true;
                }
                finally
                {
                    //fire trace event
                    EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.AnnotationTextChangedEnd);
                }
            }
        }

        private static void _OnDeviceDown<TEventArgs>(object sender, TEventArgs args)
            where TEventArgs : InputEventArgs
        {
            // Block all mouse downs from leaving this control
            args.Handled = true;
        }

        private static void _OnContextMenuOpening(object sender, ContextMenuEventArgs args)
        {
            // Block all ContextMenuOpenings from leaving this control
            if (!(args.TargetElement is ScrollBar))
            {
                args.Handled = true;
            }
        }

        // A generic class handler for both Stylus.PreviewStylusDown and Mouse.PreviewMouseDown events
        //      dc       -   the event sender
        //      args    -   Event argument
        private static void _OnPreviewDeviceDown<TEventArgs>(object sender, TEventArgs args)
            where TEventArgs : InputEventArgs
        {
            StickyNoteControl snc = sender as StickyNoteControl;

            IInputElement captured = null;
            StylusDevice stylusDevice = null;
            MouseDevice mouseDevice = null;

            // Check whether Stylus or Mouse has been captured.
            stylusDevice = args.Device as StylusDevice;
            if ( stylusDevice != null )
            {
                captured = stylusDevice.Captured;
            }
            else
            {
                mouseDevice = args.Device as MouseDevice;

                if (mouseDevice != null)
                {
                    captured = mouseDevice.Captured;
                }
            }

            // ContextMenu may capture the inputdevice in front.
            // If the device is captured by an element other than StickyNote, we should not try to bring note to front.
            if ( snc != null && ( captured == snc || captured == null ) )
            {
                snc.OnPreviewDeviceDown(sender, args);
            }
        }

        /// <summary>
        /// When the Strokes are replaced on an InkCanvas we must unregister on the previous set
        /// of strokes and register on the new set of strokes.
        /// </summary>
        private void OnInkCanvasStrokesReplacedEventHandler(object sender, InkCanvasStrokesReplacedEventArgs e)
        {
            StopListenToStrokesEvent(e.PreviousStrokes);
            StartListenToStrokesEvent(e.NewStrokes);
        }

        /// <summary>
        /// Raised when the user moves the selection.  We prevent the selection from going into negative
        /// territory because ScrollViewer does not scroll content there and the ink gets lost.
        ///
        /// For move, we just clamp X or Y to 0.
        /// </summary>
        private void OnInkCanvasSelectionMovingEventHandler(object sender, InkCanvasSelectionEditingEventArgs e)
        {
            Rect newRectangle = e.NewRectangle;
            if (newRectangle.X < 0 || newRectangle.Y < 0)
            {
                newRectangle.X = newRectangle.X < 0d ? 0d : newRectangle.X;
                newRectangle.Y = newRectangle.Y < 0d ? 0d : newRectangle.Y;
                e.NewRectangle = newRectangle;
            }
        }

        /// <summary>
        /// Raised when the user resizes the selection.  We prevent the selection from going into negative
        /// territory because ScrollViewer does not scroll content there and the ink gets lost.
        ///
        /// For Resize, we recompute the new rect after clamping x or y (or both) to 0,0
        /// </summary>
        private void OnInkCanvasSelectionResizingEventHandler(object sender, InkCanvasSelectionEditingEventArgs e)
        {
            Rect newRectangle = e.NewRectangle;
            if (newRectangle.X < 0 || newRectangle.Y < 0)
            {
                if (newRectangle.X < 0)
                {
                    //newRect.X is negative, simply add it to width to subtract it
                    newRectangle.Width = newRectangle.Width + newRectangle.X;
                    newRectangle.X = 0d;
                }
                if (newRectangle.Y < 0)
                {
                    //newRect.Y is negative, simply add it to height to subtract it
                    newRectangle.Height = newRectangle.Height + newRectangle.Y;
                    newRectangle.Y = 0d;
                }
                e.NewRectangle = newRectangle;
            }
        }

        // A handler for the events of ink stroke being changed.
        private void OnInkStrokesChanged(object sender, StrokeCollectionChangedEventArgs args)
        {
            // We have two options for tracking ink's dirty flag.
            // 1) use the UndoStateChanged event to detect when any data changes in the Ink object model
            //      Advantages:
            //          Handles ALL data changes in Ink object model
            //          One event handler
            //      Disadvantages:
            //          Very perf intensive since undo serialization is triggered
            // 2) Handle the StrokesChanged or DrawingAttributesChanged events to detect when specific kinds of data change in the Ink object model.
            //      Advantages:
            //          Efficient since no serialization occurs.
            //          Very targeted handling of types of data changes (e.g. points/transforms, drawing attributes)
            //      Disadvantages:
            //          Does not include changes to ExtendedProperties/ExtendedProperties and potentially 3rd party events. - not a true 100% perfect dirty flag

            // Since we only care about transforms/points and drawing attributes for now, #2 is a better choice.

            StopListenToStrokeEvent(args.Removed);
            StartListenToStrokeEvent(args.Added);

            if (args.Removed.Count > 0 || args.Added.Count > 0)
            {
                Invariant.Assert(Content != null && Content.InnerControl is InkCanvas);
                FrameworkElement parent = VisualTreeHelper.GetParent(Content.InnerControl) as FrameworkElement;

                if (parent != null)
                {
                    // Invalidate ContentArea's measure so that scrollbar could be updated correctly.
                    parent.InvalidateMeasure();
                }
            }

            //fire trace event
            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.AnnotationInkChangedBegin);
            try
            {
                // Update the Ink in the annotation.
                UpdateAnnotationWithSNC(XmlToken.Ink);
                IsDirty = true;
            }
            finally
            {
                //fire trace event
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordAnnotation, EventTrace.Event.AnnotationInkChangedEnd);
            }
        }

        /// <summary>
        /// Initializes a StickyNoteControl's private members.
        /// </summary>
        private void InitStickyNoteControl()
        {
            //set the anchor as DataContext
            XmlQualifiedName type = _stickyNoteType == StickyNoteType.Text ?
                                                                  TextSchemaName : InkSchemaName;
            _anchor = new MarkedHighlightComponent(type, this);

            IsDirty = false;

            //listen to Loaded event to set Focus if needed
            Loaded += new RoutedEventHandler(OnLoadedEventHandler);
        }

        /// <summary>
        /// Create the listeners we will use to register on our InkCanvas when it gets created.
        /// Only called once this StickyNote is first used for ink content.
        /// </summary>
        private void InitializeEventHandlers()
        {
            _propertyDataChangedHandler = new StrokeChangedHandler<PropertyDataChangedEventArgs>(this);
            _strokeDrawingAttributesReplacedHandler = new StrokeChangedHandler<DrawingAttributesReplacedEventArgs>(this);
            _strokePacketDataChangedHandler = new StrokeChangedHandler<EventArgs>(this);
        }


        /// <summary>
        /// Listens for click events from the close button and the icon button.
        /// Both cause the IsExpanded property to be negated.
        /// </summary>
        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            bool currentExpanded = IsExpanded;
            SetCurrentValueInternal(IsExpandedProperty, BooleanBoxes.Box(!currentExpanded));
        }

        /// <summary>
        /// Simple method that deletes the current annotation from the annotation store.
        /// This is called in response to the DeleteNote command.
        /// </summary>
        private void DeleteStickyNote()
        {
            Invariant.Assert(_attachedAnnotation != null, "AttachedAnnotation is null.");
            Invariant.Assert(_attachedAnnotation.Store != null, "AttachedAnnotation's Store is null.");

            _attachedAnnotation.Store.DeleteAnnotation(_attachedAnnotation.Annotation.Id);
        }

        /// <summary>
        /// Handles drag completed events for both thumbs in the Sticky Note Control.
        /// When the drag is completed, we write out the new values that might have changed.
        /// </summary>
        private void OnDragCompleted(object sender, DragCompletedEventArgs args)
        {
            Thumb source = args.Source as Thumb;

            // Update the cached offsets of StickyNote
            if (source == GetTitleThumb())
            {
                // Update the Top and Left in the annotation.
                UpdateAnnotationWithSNC(XmlToken.XOffset | XmlToken.YOffset | XmlToken.Left | XmlToken.Top);
            }
            else if (source == GetResizeThumb())
            {
                // Update the Width and Height and Top and Left in the annotation.
                UpdateAnnotationWithSNC(XmlToken.XOffset | XmlToken.YOffset | XmlToken.Width | XmlToken.Height | XmlToken.Left | XmlToken.Top);
            }
        }

        /// <summary>
        /// Called when either thumb is dragged.  We then process the drag in specific ways depending
        /// on which thumb was dragged.
        /// </summary>
        private void OnDragDelta(object sender, DragDeltaEventArgs args)
        {
            Invariant.Assert(IsExpanded == true, "Dragging occurred when the StickyNoteControl was not expanded.");

            Thumb source = args.Source as Thumb;
            double horizontalChange = args.HorizontalChange;

            // Because we are self-mirroring, we have flipped the layout within the note
            // but our environment isn't flipped.  The Thumb (within the note) is providing
            // mirrored values but we will use them to position ourselves within the unmirrored
            // environment, so we flip the values again before using them.
            if (_selfMirroring)
            {
                horizontalChange = -horizontalChange;
            }

            if (source == GetTitleThumb())
            {
                OnTitleDragDelta(horizontalChange, args.VerticalChange);
            }
            else if (source == GetResizeThumb())
            {
                OnResizeDragDelta(args.HorizontalChange, args.VerticalChange);
            }

            // Update the cached offsets of StickyNote
            UpdateOffsets();
        }

        /// <summary>
        /// Handles dragging the title thumb.  This updates the StickyNoteControl's
        /// position.
        /// </summary>
        private void OnTitleDragDelta(double horizontalChange, double verticalChange)
        {
            Invariant.Assert(IsExpanded != false);

            Rect rectNote = StickyNoteBounds;
            Rect rectPage = PageBounds;

            // These are the minimum widths that must be visible when a note is partially off
            // the left or right side of a page.  The difference is due to the Close button
            // being on one side and wanting some of the title bar to be visible on that side.
            double leftBoundary = 45;
            double rightBoundary = 20;

            // Because we are self-mirroring, we need to flip the minimum widths
            if (_selfMirroring)
            {
                double temp = rightBoundary;
                rightBoundary = leftBoundary;
                leftBoundary = temp;
            }

            // Figure out the new position while enforcing a portion of the note being always on the page.
            Point minBoundary = new Point(-(rectNote.X + rectNote.Width - leftBoundary), - rectNote.Y);
            Point maxBoundary = new Point(rectPage.Width - rectNote.X - rightBoundary, rectPage.Height - rectNote.Y - 20);

            horizontalChange = Math.Min(Math.Max(minBoundary.X, horizontalChange), maxBoundary.X);
            verticalChange = Math.Min(Math.Max(minBoundary.Y, verticalChange), maxBoundary.Y);

            TranslateTransform currentTransform = PositionTransform;

            // Include any temporary delta we are currently using to avoid any visible jumping to the user
            PositionTransform = new TranslateTransform(currentTransform.X + horizontalChange + _deltaX, currentTransform.Y + verticalChange + _deltaY);
            _deltaX = _deltaY = 0;

            IsDirty = true;
        }


        /// <summary>
        /// Handles dragging the bottom/right resize thumb.  Updates the StickyNoteControl's size.
        /// </summary>
        private void OnResizeDragDelta(double horizontalChange, double verticalChange)
        {
            Invariant.Assert(IsExpanded != false);

            Rect rectNote = StickyNoteBounds;

            double wNew = rectNote.Width + horizontalChange;
            double hNew = rectNote.Height + verticalChange;

            // This method doesn't apply during self-mirroring because
            // we are actually moving the note during which time the note's
            // location is bounded by the page borders (plus a cushion).
            if (!_selfMirroring)
            {
                // If the new size would put the right side of the SN off the page
                // we don't allow that resize anymore.
                if (rectNote.X + wNew < 45)
                    wNew = rectNote.Width;
            }

            double minWidth = MinWidth;
            double minHeight = MinHeight;

            if (wNew < minWidth)
            {
                wNew = minWidth;
                // This is only used in self-mirroring - if we clamp the size change,
                // we must also clamp the move (see below)
                horizontalChange = wNew - this.Width;
            }

            if (hNew < minHeight)
            {
                hNew = minHeight;
            }

            SetCurrentValueInternal(WidthProperty, wNew);
            SetCurrentValueInternal(HeightProperty, hNew);

            // Because we are self-mirroring, as the note changes size we have to
            // move it to make it appear its changing size from the left (where the
            // resize handle is located during mirroring) instead of the right which
            // is what is actually happening.
            if (_selfMirroring)
            {
                OnTitleDragDelta(-horizontalChange, 0);
            }
            else
            {
                // This appears to be a no-op but OnTitleDragDelta also takes
                // care of applying permanently any temporary offsets
                OnTitleDragDelta(0, 0);
            }

            IsDirty = true;
        }

        /// <summary>
        /// Any click in the StickyNoteControl brings it to the top in z-order and
        /// requests the focus.
        /// Additionally we must `swallow the event here if the click happend on
        /// our InkCanvas because the RTI engine isn't setup yet and the user will
        /// be creating ink without seeing it.
        /// </summary>
        private void OnPreviewDeviceDown(object dc, InputEventArgs args)
        {
            if (IsExpanded)
            {
                bool eatEvent = false;

                if (!IsKeyboardFocusWithin && this.StickyNoteType == StickyNoteType.Ink)
                {
                    // Only event we want to `swallow here is a click on the InkCanvas
                    // when the StickyNote isn't focused because RTI isn't set up yet
                    Visual source = args.OriginalSource as Visual;
                    if (source != null)
                    {
                        Invariant.Assert(Content.InnerControl != null, "InnerControl is null.");
                        eatEvent = source.IsDescendantOf(this.Content.InnerControl);
                    }
                }

                // Will have no effect if already in front
                BringToFront();

                if (!IsActive || !IsKeyboardFocusWithin)
                {
                    Focus();
                }

                if (eatEvent == true)
                {
                    args.Handled = true;
                }
            }
        }

        /// <summary>
        /// Set the focus on SN if needed
        /// </summary>
        /// <param name="sender">sender - not used</param>
        /// <param name="e">arguments - not used</param>
        private void OnLoadedEventHandler(object sender, RoutedEventArgs e)
        {
            if (IsExpanded)
            {
                // Setup the inner controls with the Annotation's data - we must have correct
                // values for SN sizes before Focus->BringIntoView is invoked
                UpdateSNCWithAnnotation(SNCAnnotation.Sizes);

                if (_sncAnnotation.IsNewAnnotation)
                {
                    // 
                    // After the annotations has been added, we should set focus on the element.
                    Focus();
                }
            }

            //unregister
            Loaded -= new RoutedEventHandler(OnLoadedEventHandler);
        }


        /// <summary>
        /// Unregister from any events on the current visual tree.  Also disconnect the current
        /// content control.
        /// </summary>
        private void ClearCachedControls()
        {
            if (Content != null)
            {
                // Disconnect the content control which will be re-connected to the new ContentControl
                // in ApplyTemplate when the new visual tree is populated from the new control template.
                DisconnectContent();
            }

            Button closeButton = GetCloseButton();
            if (closeButton != null)
            {
                closeButton.RemoveHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnButtonClick));
            }

            Button iconButton = GetIconButton();
            if (iconButton != null)
            {
                iconButton.RemoveHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnButtonClick));
            }

            Thumb titleThumb = GetTitleThumb();
            if (titleThumb != null)
            {
                titleThumb.RemoveHandler(Thumb.DragDeltaEvent, new DragDeltaEventHandler(OnDragDelta));
                titleThumb.RemoveHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(OnDragCompleted));
            }

            Thumb resizeThumb = GetResizeThumb();
            if (resizeThumb != null)
            {
                resizeThumb.RemoveHandler(Thumb.DragDeltaEvent, new DragDeltaEventHandler(OnDragDelta));
                resizeThumb.RemoveHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(OnDragCompleted));
            }
        }

        /// <summary>
        /// Called when this StickyNoteControl's IsExpanded property changes.
        /// </summary>
        private void OnIsExpandedChanged()
        {
            InvalidateTransform();

            // Update the Iconized in the annotation.
            UpdateAnnotationWithSNC(XmlToken.IsExpanded);

            IsDirty = true;

            if (IsExpanded)
            {
                BringToFront();
                // We request the focus from a dispatcher callback because we may
                // have been called from a
                this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(AsyncTakeFocus), null);
            }
            else
            {
                GiveUpFocus();
                SendToBack();
            }
        }

        /// <summary>
        /// Requests focus to this control.  Used asynchronously from event handlers
        /// to prevent other event handlers from stealing focus right back.
        /// </summary>
        private object AsyncTakeFocus(object notUsed)
        {
            this.Focus();

            return null;
        }

        /// <summary>
        /// If focus us within this control, sets the focus on the first element in the attached
        /// annotation's parent's ancestor chain that accepts it.  If no element accepts it,
        /// then focus is set to null.
        /// </summary>
        private void GiveUpFocus()
        {
            // Only send focus away when we actually have it.
            if (IsKeyboardFocusWithin)
            {
                // Start with the attached annotation's parent,
                // walk up the tree looking for the first element
                // to take focus.
                bool transferred = false;
                DependencyObject parent = _attachedAnnotation.Parent;
                IInputElement newFocus = null;

                while (parent != null && !transferred)
                {
                    newFocus = parent as IInputElement;
                    if (newFocus != null)
                    {
                        transferred = newFocus.Focus();
                    }

                    // Go up the parent chain if focus wasn't taken
                    if (!transferred)
                    {
                        parent = FrameworkElement.GetFrameworkParent(parent);
                    }
                }

                // If no element was found, just give up focus
                if (!transferred)
                {
                    Keyboard.Focus(null);
                }
            }
        }

        /// <summary>
        /// Request this control be sent to the front of the z-order stack in its
        /// presentation context.  Call to PresentationContext should have no affect
        /// if its already at the front.
        /// </summary>
        private void BringToFront()
        {
            PresentationContext pc = ((IAnnotationComponent)this).PresentationContext;
            if ( pc != null )
            {
                pc.BringToFront(this);
            }
        }

        /// <summary>
        /// Request this control be sent to the back of the z-order stack in its
        /// presentation context.  Call to PresentationContext should have no affect
        /// if its already at the back.
        /// </summary>
        private void SendToBack()
        {
            PresentationContext pc = ((IAnnotationComponent)this).PresentationContext;
            if (pc != null)
            {
                pc.SendToBack(this);
            }
        }

        /// <summary>
        /// Invalidate the Transform for this control in its presentation context.
        /// </summary>
        private void InvalidateTransform()
        {
            PresentationContext pc = ((IAnnotationComponent)this).PresentationContext;
            if ( pc != null )
            {
                pc.InvalidateTransform(this);
            }
        }

        /// <summary>
        /// Updates the attached annotation with the data currently in this control.
        /// Called asynchronously from event handler because TextBox can't handle
        /// changing its content from an event handler.  // Do we need this to be async?
        /// </summary>
        private object AsyncUpdateAnnotation(object arg)
        {
            UpdateAnnotationWithSNC((XmlToken)arg);
            return null;
        }

        /// <summary>
        /// This binds a new content control's properties to those set on
        /// the StickyNoteControl.  Should be called each time a new content
        /// control is created.
        /// </summary>
        private void BindContentControlProperties()
        {
            Invariant.Assert(Content != null);

            // ISSUE-2005/03/23/WAYNEZEN,
            // Somehow, the bound font family can't be loaded again by Parser once the attribute persists in XAML.
            // Since InkCanvas doesn't care about FontFamily, we just walk the issue around by not binding the property.
            if (Content.Type != StickyNoteType.Ink)
            {
                FrameworkElement innerControl = Content.InnerControl;
                innerControl.SetValue(FontFamilyProperty, GetValue(FontFamilyProperty));
                innerControl.SetValue(FontSizeProperty, GetValue(FontSizeProperty));
                innerControl.SetValue(FontStretchProperty, GetValue(FontStretchProperty));
                innerControl.SetValue(FontStyleProperty, GetValue(FontStyleProperty));
                innerControl.SetValue(FontWeightProperty, GetValue(FontWeightProperty));
                innerControl.SetValue(ForegroundProperty, GetValue(ForegroundProperty));
            }
            else
            {
                // Create a TwoWay MultiBinding for InkCanvas.EditingMode.
                // The internal InkCanvas' EditingMode will be determined by
                // both StickyNoteControl.InkEditingMode and StickyNoteControl.IsKeyboardFocusWithin
                // If StickyNoteControl.IsKeyboardFocusWithin is false, the InkCanvas.EditingMode should be none.
                // Otherwise InkCanvas.EditingMode is same as the StickyNoteControl.InkEditingMode.
                MultiBinding inkCanvasEditingMode = new MultiBinding();
                inkCanvasEditingMode.Mode = BindingMode.TwoWay;
                inkCanvasEditingMode.Converter = new InkEditingModeIsKeyboardFocusWithin2EditingMode();

                Binding stickyNoteInkEditingMode = new Binding();
                stickyNoteInkEditingMode.Mode = BindingMode.TwoWay;
                stickyNoteInkEditingMode.Path = new PropertyPath(StickyNoteControl.InkEditingModeProperty);
                stickyNoteInkEditingMode.Source = this;

                inkCanvasEditingMode.Bindings.Add(stickyNoteInkEditingMode);

                Binding stickyNoteIsKeyboardFocusWithin = new Binding();
                stickyNoteIsKeyboardFocusWithin.Path = new PropertyPath(UIElement.IsKeyboardFocusWithinProperty);
                stickyNoteIsKeyboardFocusWithin.Source = this;

                inkCanvasEditingMode.Bindings.Add(stickyNoteIsKeyboardFocusWithin);

                Content.InnerControl.SetBinding(InkCanvas.EditingModeProperty, inkCanvasEditingMode);
            }
        }

        /// <summary>
        /// Removes the bindings we previously created between the content control
        /// and the StickyNoteControl.
        /// </summary>
        private void UnbindContentControlProperties()
        {
            Invariant.Assert(Content != null);

            FrameworkElement innerControl = (FrameworkElement)Content.InnerControl;
            if (Content.Type != StickyNoteType.Ink)
            {
                innerControl.ClearValue(FontFamilyProperty);
                innerControl.ClearValue(FontSizeProperty);
                innerControl.ClearValue(FontStretchProperty);
                innerControl.ClearValue(FontStyleProperty);
                innerControl.ClearValue(FontWeightProperty);
                innerControl.ClearValue(ForegroundProperty);
            }
            else
            {
                BindingOperations.ClearBinding(innerControl, InkCanvas.EditingModeProperty);
            }
        }

        /// <summary>
        /// Registers for change events from the content control.  This lets us
        /// update the annotation when something in the controls change.  Should
        /// be called when a new content control is created.
        /// </summary>
        private void StartListenToContentControlEvent()
        {
            Invariant.Assert(Content != null);

            if (Content.Type == StickyNoteType.Ink)
            {
                InkCanvas inkCanvas = Content.InnerControl as InkCanvas;
                Invariant.Assert(inkCanvas != null, "InnerControl is not an InkCanvas for note of type Ink.");
                inkCanvas.StrokesReplaced += new InkCanvasStrokesReplacedEventHandler(OnInkCanvasStrokesReplacedEventHandler);
                inkCanvas.SelectionMoving += new InkCanvasSelectionEditingEventHandler(OnInkCanvasSelectionMovingEventHandler);
                inkCanvas.SelectionResizing += new InkCanvasSelectionEditingEventHandler(OnInkCanvasSelectionResizingEventHandler);
                StartListenToStrokesEvent(inkCanvas.Strokes);
            }
            else
            {
                TextBoxBase textBoxBase = Content.InnerControl as TextBoxBase;
                Invariant.Assert(textBoxBase != null, "InnerControl is not a TextBoxBase for note of type Text.");
                textBoxBase.TextChanged += new TextChangedEventHandler(OnTextChanged);
            }
        }

        /// <summary>
        /// Unregisters for any events from the content control.  Should be called
        /// when a content control is being changed.
        /// </summary>
        private void StopListenToContentControlEvent()
        {
            Invariant.Assert(Content != null);

            if (Content.Type == StickyNoteType.Ink)
            {
                InkCanvas inkCanvas = Content.InnerControl as InkCanvas;
                Invariant.Assert(inkCanvas != null, "InnerControl is not an InkCanvas for note of type Ink.");
                inkCanvas.StrokesReplaced -= new InkCanvasStrokesReplacedEventHandler(OnInkCanvasStrokesReplacedEventHandler);
                inkCanvas.SelectionMoving -= new InkCanvasSelectionEditingEventHandler(OnInkCanvasSelectionMovingEventHandler);
                inkCanvas.SelectionResizing -= new InkCanvasSelectionEditingEventHandler(OnInkCanvasSelectionResizingEventHandler);
                StopListenToStrokesEvent(inkCanvas.Strokes);
            }
            else
            {
                TextBoxBase textBoxBase = Content.InnerControl as TextBoxBase;
                Invariant.Assert(textBoxBase != null, "InnerControl is not a TextBoxBase for note of type Text.");
                textBoxBase.TextChanged -= new TextChangedEventHandler(OnTextChanged);
            }
        }

        /// <summary>
        /// Register for events on the StrokeCollection from the InkCanvas.
        /// </summary>
        private void StartListenToStrokesEvent(StrokeCollection strokes)
        {
            strokes.StrokesChanged += new StrokeCollectionChangedEventHandler(OnInkStrokesChanged);
            strokes.PropertyDataChanged += new PropertyDataChangedEventHandler(_propertyDataChangedHandler.OnStrokeChanged);
            StartListenToStrokeEvent(strokes);
        }

        /// <summary>
        /// Unregister for events on the StrokeCollection from the InkCanvas.
        /// </summary>
        private void StopListenToStrokesEvent(StrokeCollection strokes)
        {
            strokes.StrokesChanged -= new StrokeCollectionChangedEventHandler(OnInkStrokesChanged);
            strokes.PropertyDataChanged -= new PropertyDataChangedEventHandler(_propertyDataChangedHandler.OnStrokeChanged);
            StopListenToStrokeEvent(strokes);
        }

        /// <summary>
        /// Register on each stroke in the InkCanvas.  If any of them change we need to know about it.
        /// </summary>
        private void StartListenToStrokeEvent(IEnumerable<Stroke> strokes)
        {
            foreach (Stroke s in strokes)
            {
                s.DrawingAttributes.AttributeChanged += new PropertyDataChangedEventHandler(
                                                            _propertyDataChangedHandler.OnStrokeChanged);
                s.DrawingAttributesReplaced += new DrawingAttributesReplacedEventHandler(
                                                            _strokeDrawingAttributesReplacedHandler.OnStrokeChanged);
                s.StylusPointsReplaced +=               new StylusPointsReplacedEventHandler(
                                                            _strokePacketDataChangedHandler.OnStrokeChanged);
                s.StylusPoints.Changed +=               new EventHandler(
                                                            _strokePacketDataChangedHandler.OnStrokeChanged);
                s.PropertyDataChanged +=                new PropertyDataChangedEventHandler(
                                                            _propertyDataChangedHandler.OnStrokeChanged);
            }
        }

        /// <summary>
        /// Unregister on each stroke in the InkCanvas.  We previously registered on each stroke and
        /// now need to disconnect from them.
        /// </summary>
        private void StopListenToStrokeEvent(IEnumerable<Stroke> strokes)
        {
            foreach (Stroke s in strokes)
            {
                s.DrawingAttributes.AttributeChanged -= new PropertyDataChangedEventHandler(
                                                            _propertyDataChangedHandler.OnStrokeChanged);
                s.DrawingAttributesReplaced -= new DrawingAttributesReplacedEventHandler(
                                                            _strokeDrawingAttributesReplacedHandler.OnStrokeChanged);
                s.StylusPointsReplaced  -=              new StylusPointsReplacedEventHandler(
                                                            _strokePacketDataChangedHandler.OnStrokeChanged);
                s.StylusPoints.Changed -=               new EventHandler(
                                                            _strokePacketDataChangedHandler.OnStrokeChanged);
                s.PropertyDataChanged -=                new PropertyDataChangedEventHandler(
                                                            _propertyDataChangedHandler.OnStrokeChanged);
            }
        }

        /// <summary>
        /// Set bindings on the menuitems which StickyNote knows about
        /// </summary>
        private void SetupMenu()
        {
            MenuItem inkMenuItem = GetInkMenuItem();
            if (inkMenuItem != null)
            {
                // Bind the EditingMode to item's IsChecked DP.
                Binding checkedBind = new Binding("InkEditingMode");
                checkedBind.Mode = BindingMode.OneWay;
                checkedBind.RelativeSource = RelativeSource.TemplatedParent;
                checkedBind.Converter = new InkEditingModeConverter();
                checkedBind.ConverterParameter = InkCanvasEditingMode.Ink;
                inkMenuItem.SetBinding(MenuItem.IsCheckedProperty, checkedBind);
            }

            MenuItem selectMenuItem = GetSelectMenuItem();
            if (selectMenuItem != null)
            {
                // Bind the EditingMode to item's IsChecked DP.
                Binding checkedBind = new Binding("InkEditingMode");
                checkedBind.Mode = BindingMode.OneWay;
                checkedBind.RelativeSource = RelativeSource.TemplatedParent;
                checkedBind.Converter = new InkEditingModeConverter();
                checkedBind.ConverterParameter = InkCanvasEditingMode.Select;
                selectMenuItem.SetBinding(MenuItem.IsCheckedProperty, checkedBind);
            }

            MenuItem eraseMenuItem = GetEraseMenuItem();
            if (eraseMenuItem != null)
            {
                // Bind the EditingMode to item's IsChecked DP.
                Binding checkedBind = new Binding("InkEditingMode");
                checkedBind.Mode = BindingMode.OneWay;
                checkedBind.RelativeSource = RelativeSource.TemplatedParent;
                checkedBind.Converter = new InkEditingModeConverter();
                checkedBind.ConverterParameter = InkCanvasEditingMode.EraseByStroke;
                eraseMenuItem.SetBinding(MenuItem.IsCheckedProperty, checkedBind);
            }

            // Set the target for the Copy/Paste commands to our inner control
            MenuItem copyMenuItem = GetCopyMenuItem();
            if (copyMenuItem != null)
            {
                copyMenuItem.CommandTarget = Content.InnerControl;
            }

            MenuItem pasteMenuItem = GetPasteMenuItem();
            if (pasteMenuItem != null)
            {
                pasteMenuItem.CommandTarget = Content.InnerControl;
            }
        }


        /// <summary>
        /// CommandExecuted Handler - processes the commands for SNC.
        ///     DeleteNoteCommand - deletes the annotation from the store (causing the SNC to disappear)
        ///     InkCommand - changes the mode of the ink canvas
        ///     Copy/Paste - we pass on to our inner control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void _OnCommandExecuted(object sender, ExecutedRoutedEventArgs args)
        {
            RoutedCommand command = (RoutedCommand)(args.Command);
            StickyNoteControl snc = sender as StickyNoteControl;

            Invariant.Assert(snc != null, "Unexpected Commands");
            Invariant.Assert(command == StickyNoteControl.DeleteNoteCommand
                || command == StickyNoteControl.InkCommand, "Unknown Commands");

            if ( command == StickyNoteControl.DeleteNoteCommand )
            {
                // DeleteNote Command
                snc.DeleteStickyNote();
            }
            else if (command == StickyNoteControl.InkCommand)
            {
                StickyNoteContentControl content = snc.Content;

                if (content == null || content.Type != StickyNoteType.Ink)
                {
                    throw new InvalidOperationException(SR.Get(SRID.CannotProcessInkCommand));
                }

                // Set the StickyNoteControl's ink editing mode to the command's parameter
                InkCanvasEditingMode mode = (InkCanvasEditingMode)args.Parameter;
                snc.SetValue(InkEditingModeProperty, mode);
            }
        }

        /// <summary>
        /// QueryCommandEnabled Handler - determines if a command should be enabled or not.
        ///   DeleteNoteCommand - if the SNC has an attached annotation
        ///   InkCommand - if the SNC is of type InkStickyNote
        ///   AllOthers - if focus is on our inner control, we let the inner control decide,
        ///               otherwise we return false
        /// </summary>
        private static void _OnQueryCommandEnabled(object sender, CanExecuteRoutedEventArgs args)
        {
            RoutedCommand command = (RoutedCommand)( args.Command );
            StickyNoteControl snc = sender as StickyNoteControl;

            Invariant.Assert(snc != null, "Unexpected Commands");
            Invariant.Assert(command == StickyNoteControl.DeleteNoteCommand
                || command == StickyNoteControl.InkCommand, "Unknown Commands");

            if ( command == StickyNoteControl.DeleteNoteCommand )
            {
                // Enable/Disable DeleteNote Command based on the Attached Annotation.
                args.CanExecute = snc._attachedAnnotation != null;
            }
            else if (command == StickyNoteControl.InkCommand)
            {
                StickyNoteContentControl content = snc.Content;

                // Enabled/Disable InkCommand based on the StickyNote type
                args.CanExecute = (content != null && content.Type == StickyNoteType.Ink);
            }
            else
            {
                Invariant.Assert(false, "Unknown command.");
            }
        }


        /// <summary>
        /// Update DrawingAttributes on InkCanvas
        /// </summary>
        private void UpdateInkDrawingAttributes()
        {
            if ( Content == null || Content.Type != StickyNoteType.Ink )
            {
                // Return now if there is no InkCanvas.
                return;
            }

            DrawingAttributes da = new DrawingAttributes();

            SolidColorBrush foreground = Foreground as SolidColorBrush;

            // Make sure the foreground is type of SolidColorBrush.
            if ( foreground == null )
            {
                throw new ArgumentException(SR.Get(SRID.InvalidInkForeground));
            }

            da.StylusTip = StylusTip.Ellipse;
            da.Width = PenWidth;
            da.Height = PenWidth;
            da.Color = foreground.Color;

            // Update the DA on InkCanvas.
            ( (InkCanvas)( Content.InnerControl ) ).DefaultDrawingAttributes = da;
        }

        #endregion // Private Methods

        //-------------------------------------------------------------------------------
        //
        // Private Properties
        //
        //-------------------------------------------------------------------------------

        #region Private Properties

        /// <summary>
        /// Returns Ink MenuItem
        /// </summary>
        private MenuItem GetInkMenuItem()
        {
            return GetTemplateChild(SNBConstants.c_InkMenuId) as MenuItem;
        }

        /// <summary>
        /// Returns Select MenuItem
        /// </summary>
        private MenuItem GetSelectMenuItem()
        {
            return GetTemplateChild(SNBConstants.c_SelectMenuId) as MenuItem;
        }

        /// <summary>
        /// Returns Erase MenuItem
        /// </summary>
        private MenuItem GetEraseMenuItem()
        {
            return GetTemplateChild(SNBConstants.c_EraseMenuId) as MenuItem;
        }

        /// <summary>
        /// Returns Copy MenuItem
        /// </summary>
        private MenuItem GetCopyMenuItem()
        {
            return GetTemplateChild(SNBConstants.c_CopyMenuId) as MenuItem;
        }

        /// <summary>
        /// Returns Paste MenuItem
        /// </summary>
        private MenuItem GetPasteMenuItem()
        {
            return GetTemplateChild(SNBConstants.c_PasteMenuId) as MenuItem;
        }

        /// <summary>
        /// Returns Separator for clipboard MenuItems
        /// </summary>
        private Separator GetClipboardSeparator()
        {
            return GetTemplateChild(SNBConstants.c_ClipboardSeparatorId) as Separator;
        }

        // This is the getter of _lockHelper which is a helper object for locking/unlocking a specified flag automatically.
        private LockHelper InternalLocker
        {
            get
            {
                // Check if we have create a helper. If not, we go ahead create one.
                if (_lockHelper == null)
                {
                    _lockHelper = new LockHelper();
                }

                return _lockHelper;
            }
        }

        #endregion // Private Properties

        //-------------------------------------------------------------------------------
        //
        // Private Fields
        //
        //-------------------------------------------------------------------------------

        #region Private Fields

        private LockHelper _lockHelper;

        private MarkedHighlightComponent _anchor; //holds inactive, active, focused state

        private bool _dirty = false;

        // Cache for the dependency properties
        private StickyNoteType _stickyNoteType = StickyNoteType.Text;

        private StickyNoteContentControl _contentControl;

        private StrokeChangedHandler<PropertyDataChangedEventArgs> _propertyDataChangedHandler;
        private StrokeChangedHandler<DrawingAttributesReplacedEventArgs> _strokeDrawingAttributesReplacedHandler;
        private StrokeChangedHandler<EventArgs> _strokePacketDataChangedHandler;

        #endregion // Private Fields



        //-------------------------------------------------------------------------------
        //
        // Private classes
        //
        //-------------------------------------------------------------------------------

        #region Private classes

        // This is a binding converter which alters the IsChecked DP based on ink StickyNote's EditingMode of its InkCanvas.
        private class InkEditingModeConverter : IValueConverter
        {
            public object Convert(object o, Type type, object parameter, CultureInfo culture)
            {
                InkCanvasEditingMode expectedMode = (InkCanvasEditingMode)parameter;
                InkCanvasEditingMode currentMode = (InkCanvasEditingMode)o;

                // If the current EditingMode is the mode which menuitem is expecting, return true for IsChecked.
                if ( currentMode == expectedMode )
                {
                    return true;
                }
                else
                {
                    return DependencyProperty.UnsetValue;
                }
            }

            public object ConvertBack(object o, Type type, object parameter, CultureInfo culture)
            {
                return null;
            }
        }

        // This is a binding converter which alters the InkCanvas.EditingMode DP
        // based on StickyNoteControl.InkEditingMode and StickyNoteControl.IsKeyboardFocusWithin.
        private class InkEditingModeIsKeyboardFocusWithin2EditingMode : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                InkCanvasEditingMode sncInkEditingMode = (InkCanvasEditingMode)values[0];

                bool sncIsKeyboardFocusWithin = (bool)values[1];

                // If there is no focus on the StickyNote, we should return InkCanvasEditingMode.None to disable the RTI.
                // Otherwise return the  value of the StickyNoteControl.InkEditingMode property.
                if ( sncIsKeyboardFocusWithin )
                {
                    return sncInkEditingMode;
                }
                else
                {
                    return InkCanvasEditingMode.None;
                }
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                return new object[] { value, Binding.DoNothing };
            }
        }

        // A helper class which suppresses severial handlers to a single methods by using generic
        private class StrokeChangedHandler<TEventArgs>
        {
            public StrokeChangedHandler(StickyNoteControl snc)
            {
                Invariant.Assert(snc != null);
                _snc = snc;
            }

            public void OnStrokeChanged(object sender, TEventArgs t)
            {
                // Dirty the ink data
                _snc.UpdateAnnotationWithSNC(XmlToken.Ink);
                _snc._dirty = true;
            }

            private StickyNoteControl _snc;
        }

        #endregion Private classes
    }
}




