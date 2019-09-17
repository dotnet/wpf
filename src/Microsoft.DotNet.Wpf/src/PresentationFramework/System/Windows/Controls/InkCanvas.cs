// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//#define DEBUG_LASSO_FEEDBACK // DO NOT LEAVE ENABLED IN CHECKED IN CODE
//
// Description:
//      Defines an inkable canvas that represents the primary api for
//      editing ink
//

using MS.Utility;
using MS.Internal;
using MS.Internal.Commands;
using MS.Internal.Controls;
using MS.Internal.Ink;
using MS.Internal.KnownBoxes;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Collections.Generic;
using System.Security;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Controls;
using System.Windows.Markup; // IAddChild, ContentPropertyAttribute
using System.Windows.Threading;
using System.Windows.Automation.Peers;

using BuildInfo = MS.Internal.PresentationFramework.BuildInfo;
using SecurityHelper = MS.Internal.SecurityHelper;

namespace System.Windows.Controls
{
    /// <summary>
    /// InkCanvas is used to allow inking on a canvas
    /// </summary>
    [ContentProperty("Children")]
    public class InkCanvas : FrameworkElement, IAddChild
    {
        #region Constructors / Initialization

        /// <summary>
        /// The static constructor
        /// </summary>
        static InkCanvas()
        {
            Type ownerType = typeof(InkCanvas);

            // 
            // We should add the following listener as the class handler which will be guarantied to receive the
            // notification before the handler on the instances. So we won't be trapped in the bad state due to the
            // event routing.
            // Listen to stylus events which will be redirected to the current StylusEditingBehavior

            //Down
            EventManager.RegisterClassHandler(ownerType, Stylus.StylusDownEvent,
                new StylusDownEventHandler(_OnDeviceDown<StylusDownEventArgs>));
            EventManager.RegisterClassHandler(ownerType, Mouse.MouseDownEvent,
                new MouseButtonEventHandler(_OnDeviceDown<MouseButtonEventArgs>));

            //Up
            EventManager.RegisterClassHandler(ownerType, Stylus.StylusUpEvent,
                new StylusEventHandler(_OnDeviceUp<StylusEventArgs>));
            EventManager.RegisterClassHandler(ownerType, Mouse.MouseUpEvent,
                new MouseButtonEventHandler(_OnDeviceUp<MouseButtonEventArgs>));



            EventManager.RegisterClassHandler(ownerType, Mouse.QueryCursorEvent,
                new QueryCursorEventHandler(_OnQueryCursor), true);

            // Set up the commanding handlers
            _RegisterClipboardHandlers();
            CommandHelpers.RegisterCommandHandler(ownerType, ApplicationCommands.Delete,
                new ExecutedRoutedEventHandler(_OnCommandExecuted), new CanExecuteRoutedEventHandler(_OnQueryCommandEnabled));

            CommandHelpers.RegisterCommandHandler(ownerType, ApplicationCommands.SelectAll,
                Key.A, ModifierKeys.Control, new ExecutedRoutedEventHandler(_OnCommandExecuted), new CanExecuteRoutedEventHandler(_OnQueryCommandEnabled));

            CommandHelpers.RegisterCommandHandler(ownerType, InkCanvas.DeselectCommand,
                new ExecutedRoutedEventHandler(_OnCommandExecuted), new CanExecuteRoutedEventHandler(_OnQueryCommandEnabled),
                KeyGesture.CreateFromResourceStrings(InkCanvasDeselectKey, SRID.InkCanvasDeselectKeyDisplayString));

            //
            //set our clipping
            //
            ClipToBoundsProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(BooleanBoxes.TrueBox));

            //
            //enable input focus
            //
            FocusableProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(BooleanBoxes.TrueBox));

            // The default InkCanvas style
            Style defaultStyle = new Style(ownerType);
            // The background - Window Color
            defaultStyle.Setters.Add(new Setter(InkCanvas.BackgroundProperty,
                            new DynamicResourceExtension(SystemColors.WindowBrushKey)));
            // Default InkCanvas to having flicks disabled by default.
            defaultStyle.Setters.Add(new Setter(Stylus.IsFlicksEnabledProperty, false));
            // Default InkCanvas to having tap feedback disabled by default.
            defaultStyle.Setters.Add(new Setter(Stylus.IsTapFeedbackEnabledProperty, false));
            // Default InkCanvas to having touch feedback disabled by default.
            defaultStyle.Setters.Add(new Setter(Stylus.IsTouchFeedbackEnabledProperty, false));

            // Set MinWidth to 350d if Width is set to Auto
            Trigger trigger = new Trigger();
            trigger.Property = WidthProperty;
            trigger.Value = double.NaN;
            Setter setter = new Setter();
            setter.Property = MinWidthProperty;
            setter.Value = 350d;
            trigger.Setters.Add(setter);
            defaultStyle.Triggers.Add(trigger);

            // Set MinHeight to 250d if Height is set to Auto
            trigger = new Trigger();
            trigger.Property = HeightProperty;
            trigger.Value = double.NaN;
            setter = new Setter();
            setter.Property = MinHeightProperty;
            setter.Value = 250d;
            trigger.Setters.Add(setter);
            defaultStyle.Triggers.Add(trigger);

            // Seal the default style
            defaultStyle.Seal();

            StyleProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(defaultStyle));
            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(typeof(InkCanvas)));

            FocusVisualStyleProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata((object)null /* default value */));
        }

        /// <summary>
        /// Public constructor.
        /// </summary>
        public InkCanvas() : base()
        {
            Initialize();
        }

        /// <summary>
        /// Private initialization method used by the constructors
        /// </summary>
        private void Initialize()
        {
            //
            // instance the DynamicRenderer and add it to the StylusPlugIns
            //
            _dynamicRenderer = new DynamicRenderer();
            _dynamicRenderer.Enabled = false;
            this.StylusPlugIns.Add(_dynamicRenderer);

            //
            // create and initialize an editing coordinator
            //
            _editingCoordinator = new EditingCoordinator(this);
            _editingCoordinator.UpdateActiveEditingState();


            // connect the attributes event handler after setting the stylus shape to avoid unnecessary
            //      calls into the RTI service
            DefaultDrawingAttributes.AttributeChanged += new PropertyDataChangedEventHandler(DefaultDrawingAttributes_Changed);

            //
            //
            // We must initialize this here (after adding DynamicRenderer to Sytlus).
            //
            this.InitializeInkObject();

            _rtiHighContrastCallback = new RTIHighContrastCallback(this);

            // Register rti high contrast callback. Then check whether we are under the high contrast already.
            HighContrastHelper.RegisterHighContrastCallback(_rtiHighContrastCallback);
            if ( SystemParameters.HighContrast )
            {
                _rtiHighContrastCallback.TurnHighContrastOn(SystemColors.WindowTextColor);
            }
        }

        /// <summary>
        /// Private helper used to change the Ink objects.  Used in the constructor
        /// and the Ink property.
        ///
        /// NOTE -- Caller is responsible for clearing any selection!  (We can't clear it
        ///         here because the Constructor calls this method and it would end up calling
        ///         looking like it could call a virtual method and FxCop doesn't like that!)
        ///
        /// </summary>
        private void InitializeInkObject()
        {
            // Update the RealTimeInking PlugIn for the Renderer changes.
            UpdateDynamicRenderer();

            // Initialize DefaultPacketDescription
            _defaultStylusPointDescription = new StylusPointDescription();
        }
        #endregion Constructors  / Initialization

        #region Protected Overrides

        /// <summary>
        /// MeasureOverride
        /// </summary>
        /// <param name="availableSize"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            // No need to invoke VerifyAccess since _localAdornerDecorator.Measure should check it.
            if ( _localAdornerDecorator == null )
            {
                ApplyTemplate();
            }

            _localAdornerDecorator.Measure(availableSize);

            return  _localAdornerDecorator.DesiredSize;
        }

        /// <summary>
        /// ArrangeOverride
        /// </summary>
        /// <param name="arrangeSize"></param>
        /// <returns></returns>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            // No need to invoke VerifyAccess since _localAdornerDecorator.Arrange should check it.

            if ( _localAdornerDecorator == null )
            {
                ApplyTemplate();
            }

            _localAdornerDecorator.Arrange(new Rect(arrangeSize));

            return arrangeSize;
        }


        /// <summary>
        /// HitTestCore implements precise hit testing against render contents
        /// </summary>
        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParams)
        {
            VerifyAccess();

            Rect r = new Rect(new Point(), RenderSize);
            if (r.Contains(hitTestParams.HitPoint))
            {
                return new PointHitTestResult(this, hitTestParams.HitPoint);
            }

            return null;
        }

        /// <summary>
        /// OnPropertyChanged
        /// </summary>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.IsAValueChange || e.IsASubPropertyChange)
            {
                if (e.Property == UIElement.RenderTransformProperty ||
                    e.Property == FrameworkElement.LayoutTransformProperty)
                {
                    EditingCoordinator.InvalidateTransform();

                    Transform transform = e.NewValue as Transform;
                    if (transform != null && !transform.HasAnimatedProperties)
                    {
                        TransformGroup transformGroup = transform as TransformGroup;
                        if ( transformGroup != null )
                        {
                            //walk down the tree looking for animated transforms
                            Stack<Transform> transforms = new Stack<Transform>();
                            transforms.Push(transform);
                            while ( transforms.Count > 0 )
                            {
                                transform = transforms.Pop();
                                if ( transform.HasAnimatedProperties )
                                {
                                    return;
                                }
                                transformGroup = transform as TransformGroup;
                                if ( transformGroup != null )
                                {
                                    for ( int i = 0; i < transformGroup.Children.Count; i++ )
                                    {
                                        transforms.Push(transformGroup.Children[i]);
                                    }
                                }
                            }
                        }

                        //
                        // only invalidate when there is not an animation on the xf,
                        // or we could wind up creating thousands of new cursors.  That's bad.
                        //
                        _editingCoordinator.InvalidateBehaviorCursor(_editingCoordinator.InkCollectionBehavior);
                        EditingCoordinator.UpdatePointEraserCursor();
                    }
                }
                if (e.Property == FrameworkElement.FlowDirectionProperty)
                {
                    //flow direction only affects the inking cursor.
                    _editingCoordinator.InvalidateBehaviorCursor(_editingCoordinator.InkCollectionBehavior);
                }
            }
        }

        /// <summary>
        /// Called when the Template's tree is about to be generated
        /// </summary>
        internal override void OnPreApplyTemplate()
        {
            // No need for calling VerifyAccess since we call the method on the base here.

            base.OnPreApplyTemplate();

            // Build our visual tree here.
            // <InkCanvas>
            //     <AdornerDecorator>
            //         <InkPresenter>
            //             <InnerCanvas />
            //             <ContainerVisual />              <!-- Ink Renderer static visual -->
            //             <HostVisual />                   <!-- Ink Dynamic Renderer -->
            //         </InkPresenter>
            //         <AdornerLayer>                       <!-- Create by AdornerDecorator automatically -->
            //             <InkCanvasSelectionAdorner />
            //             <InkCanvasFeedbackAdorner />     <!-- Dynamically hooked up when moving/sizing the selection -->
            //         </AdornerLayer>
            //     </AdornerDecorator>
            //  </InkCanvas>

            if ( _localAdornerDecorator == null )
            {
                //
                _localAdornerDecorator = new AdornerDecorator();
                InkPresenter inkPresenter = InkPresenter;

                // Build the visual tree top-down
                AddVisualChild(_localAdornerDecorator);
                _localAdornerDecorator.Child = inkPresenter;
                inkPresenter.Child = InnerCanvas;

                // Add the SelectionAdorner after Canvas is added.
                _localAdornerDecorator.AdornerLayer.Add(SelectionAdorner);
            }
        }

        /// <summary>
        /// Returns the Visual children count.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get { return (_localAdornerDecorator == null) ? 0 : 1; }
        }

        /// <summary>
        /// Returns the child at the specified index.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (    (_localAdornerDecorator == null)
                ||  (index != 0))
            {
                throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
            }

            return _localAdornerDecorator;
        }

        /// <summary>
        /// UIAutomation support
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new InkCanvasAutomationPeer(this);
        }

        #endregion Protected Overrides

        #region Public Properties

        /// <summary>
        ///     The DependencyProperty for the Background property.
        /// </summary>
        public static readonly DependencyProperty BackgroundProperty =
                Panel.BackgroundProperty.AddOwner(
                        typeof(InkCanvas),
                        new FrameworkPropertyMetadata(
                                null,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        ///     An object that describes the background.
        /// </summary>
        [Bindable(true), Category("Appearance")]
        public Brush Background
        {
            get { return (Brush) GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// Top DependencyProperty
        /// </summary>
        public static readonly DependencyProperty TopProperty =
                DependencyProperty.RegisterAttached("Top", typeof(double), typeof(InkCanvas),
                    new FrameworkPropertyMetadata(Double.NaN, new PropertyChangedCallback(OnPositioningChanged)),
                    new ValidateValueCallback(System.Windows.Shapes.Shape.IsDoubleFiniteOrNaN));

        /// <summary>
        /// Reads the attached property Top from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the Top attached property.</param>
        /// <returns>The property's value.</returns>
        /// <seealso cref="InkCanvas.TopProperty" />
        [TypeConverter("System.Windows.LengthConverter, PresentationFramework, Version=" +BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" +BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
        [AttachedPropertyBrowsableForChildren()]
        public static double GetTop(UIElement element)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            return (double)element.GetValue(TopProperty);
        }

        /// <summary>
        /// Writes the attached property Top to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the Top attached property.</param>
        /// <param name="length">The length to set</param>
        /// <seealso cref="InkCanvas.TopProperty" />
        public static void SetTop(UIElement element, double length)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            element.SetValue(TopProperty, length);
        }

        /// <summary>
        /// The Bottom DependencyProperty
        /// </summary>
        public static readonly DependencyProperty BottomProperty =
                DependencyProperty.RegisterAttached("Bottom", typeof(double), typeof(InkCanvas),
                    new FrameworkPropertyMetadata(Double.NaN, new PropertyChangedCallback(OnPositioningChanged)),
                    new ValidateValueCallback(System.Windows.Shapes.Shape.IsDoubleFiniteOrNaN));

        /// <summary>
        /// Reads the attached property Bottom from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the Bottom attached property.</param>
        /// <returns>The property's Length value.</returns>
        /// <seealso cref="InkCanvas.BottomProperty" />
        [TypeConverter("System.Windows.LengthConverter, PresentationFramework, Version=" +BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" +BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
        [AttachedPropertyBrowsableForChildren()]
        public static double GetBottom(UIElement element)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            return (double)element.GetValue(BottomProperty);
        }

        /// <summary>
        /// Writes the attached property Bottom to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the Bottom attached property.</param>
        /// <param name="length">The Length to set</param>
        /// <seealso cref="InkCanvas.BottomProperty" />
        public static void SetBottom(UIElement element, double length)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            element.SetValue(BottomProperty, length);
        }

        /// <summary>
        /// The Left DependencyProperty
        /// </summary>
        public static readonly DependencyProperty LeftProperty =
                DependencyProperty.RegisterAttached("Left", typeof(double), typeof(InkCanvas),
                    new FrameworkPropertyMetadata(Double.NaN, new PropertyChangedCallback(OnPositioningChanged)),
                    new ValidateValueCallback(System.Windows.Shapes.Shape.IsDoubleFiniteOrNaN));

        /// <summary>
        /// Reads the attached property Left from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the Left attached property.</param>
        /// <returns>The property's value.</returns>
        /// <seealso cref="InkCanvas.LeftProperty" />
        [TypeConverter("System.Windows.LengthConverter, PresentationFramework, Version=" +BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" +BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
        [AttachedPropertyBrowsableForChildren()]
        public static double GetLeft(UIElement element)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            return (double)element.GetValue(LeftProperty);
        }

        /// <summary>
        /// Writes the attached property Left to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the Left attached property.</param>
        /// <param name="length">The length to set</param>
        /// <seealso cref="InkCanvas.LeftProperty" />
        public static void SetLeft(UIElement element, double length)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            element.SetValue(LeftProperty, length);
        }

        /// <summary>
        /// The Right DependencyProperty
        /// </summary>
        public static readonly DependencyProperty RightProperty =
                DependencyProperty.RegisterAttached("Right", typeof(double), typeof(InkCanvas),
                    new FrameworkPropertyMetadata(Double.NaN, new PropertyChangedCallback(OnPositioningChanged)),
                    new ValidateValueCallback(System.Windows.Shapes.Shape.IsDoubleFiniteOrNaN));

        /// <summary>
        /// Reads the attached property Right from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the Right attached property.</param>
        /// <returns>The property's Length value.</returns>
        /// <seealso cref="InkCanvas.RightProperty" />
        [TypeConverter("System.Windows.LengthConverter, PresentationFramework, Version=" +BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" +BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
        [AttachedPropertyBrowsableForChildren()]
        public static double GetRight(UIElement element)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            return (double)element.GetValue(RightProperty);
        }

        /// <summary>
        /// Writes the attached property Right to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the Right attached property.</param>
        /// <param name="length">The Length to set</param>
        /// <seealso cref="InkCanvas.RightProperty" />
        public static void SetRight(UIElement element, double length)
        {
            if (element == null) { throw new ArgumentNullException("element"); }
            element.SetValue(RightProperty, length);
        }

        /// <summary>
        /// OnPositioningChanged
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnPositioningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = d as UIElement;
            if ( uie != null )
            {
                // Make sure the UIElement is a child of InkCanvasInnerCanvas.
                InkCanvasInnerCanvas p = VisualTreeHelper.GetParent(uie) as InkCanvasInnerCanvas;
                if ( p != null )
                {
                    if ( e.Property == InkCanvas.LeftProperty
                        || e.Property == InkCanvas.TopProperty )
                    {
                        // Invalidate measure for Left and/or Top.
                        p.InvalidateMeasure();
                    }
                    else
                    {
                        Debug.Assert(e.Property == InkCanvas.RightProperty || e.Property == InkCanvas.BottomProperty,
                            string.Format(System.Globalization.CultureInfo.InvariantCulture, "Unknown dependency property detected - {0}.", e.Property));

                        // Invalidate arrange for Right and/or Bottom.
                        p.InvalidateArrange();
                    }
                }
            }
        }

        /// <summary>
        ///     The DependencyProperty for the Strokes property.
        /// </summary>
        public static readonly DependencyProperty StrokesProperty =
                InkPresenter.StrokesProperty.AddOwner(
                        typeof(InkCanvas),
                        new FrameworkPropertyMetadata(
                                new StrokeCollectionDefaultValueFactory(),
                                new PropertyChangedCallback(OnStrokesChanged)));

        /// <summary>
        /// Gets/Sets the Strokes property.
        /// </summary>
        public StrokeCollection Strokes
        {
            get { return (StrokeCollection)GetValue(StrokesProperty); }
            set { SetValue(StrokesProperty, value); }
        }

        private static void OnStrokesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            InkCanvas inkCanvas = (InkCanvas)d;
            StrokeCollection oldValue = (StrokeCollection)e.OldValue;
            StrokeCollection newValue = (StrokeCollection)e.NewValue;

            //
            // only change the prop if it's a different object.  We don't
            // want to be doing this for no reason
            //
            if ( !object.ReferenceEquals(oldValue, newValue) )
            {
                // Clear the selected strokes without raising event.
                inkCanvas.CoreChangeSelection(new StrokeCollection(), inkCanvas.InkCanvasSelection.SelectedElements, false);

                inkCanvas.InitializeInkObject();

                InkCanvasStrokesReplacedEventArgs args =
                    new InkCanvasStrokesReplacedEventArgs(newValue, oldValue); //new, previous

                //raise the StrokesChanged event through our protected virtual
                inkCanvas.OnStrokesReplaced(args);
            }
        }

        /// <summary>
        /// Returns the SelectionAdorner
        /// </summary>
        internal InkCanvasSelectionAdorner SelectionAdorner
        {
            get
            {
                // We have to create our visual at this point.
                if ( _selectionAdorner == null )
                {
                    // Create the selection Adorner.
                    _selectionAdorner = new InkCanvasSelectionAdorner(InnerCanvas);

                    // Bind the InkCanvas.ActiveEditingModeProperty
                    // to SelectionAdorner.VisibilityProperty.
                    Binding activeEditingModeBinding = new Binding();
                    activeEditingModeBinding.Path = new PropertyPath(InkCanvas.ActiveEditingModeProperty);
                    activeEditingModeBinding.Mode = BindingMode.OneWay;
                    activeEditingModeBinding.Source = this;
                    activeEditingModeBinding.Converter = new ActiveEditingMode2VisibilityConverter();
                    _selectionAdorner.SetBinding(UIElement.VisibilityProperty, activeEditingModeBinding);
                }

                return _selectionAdorner;
            }
        }

        /// <summary>
        /// Returns the FeedbackAdorner
        /// </summary>
        internal InkCanvasFeedbackAdorner FeedbackAdorner
        {
            get
            {
                VerifyAccess();

                if ( _feedbackAdorner == null )
                {
                    _feedbackAdorner = new InkCanvasFeedbackAdorner(this);
                }

                return _feedbackAdorner;
            }
        }

        /// <summary>
        /// Read/Write access to the EraserShape property.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsGestureRecognizerAvailable
        {
            get
            {
                //this property will verify access
                return this.GestureRecognizer.IsRecognizerAvailable;
            }
        }


        /// <summary>
        /// Emulate Panel's Children property.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public UIElementCollection Children
        {
            get
            {
                // No need to invoke VerifyAccess since the call is forwarded.

                return InnerCanvas.Children;
            }
        }

        /// <summary>
        /// The DependencyProperty for the DefaultDrawingAttributes property.
        /// </summary>
        public static readonly DependencyProperty DefaultDrawingAttributesProperty =
                DependencyProperty.Register(
                        "DefaultDrawingAttributes",
                        typeof(DrawingAttributes),
                        typeof(InkCanvas),
                        new FrameworkPropertyMetadata(
                                new DrawingAttributesDefaultValueFactory(),
                                new PropertyChangedCallback(OnDefaultDrawingAttributesChanged)),
                        (ValidateValueCallback)delegate(object value)
                            { return value != null; });

        /// <summary>
        /// Gets/Sets the DefaultDrawingAttributes property.
        /// </summary>
        public DrawingAttributes DefaultDrawingAttributes
        {
            get { return (DrawingAttributes)GetValue(DefaultDrawingAttributesProperty); }
            set { SetValue(DefaultDrawingAttributesProperty, value); }
        }

        private static void OnDefaultDrawingAttributesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            InkCanvas inkCanvas = (InkCanvas)d;

            DrawingAttributes oldValue = (DrawingAttributes)e.OldValue;
            DrawingAttributes newValue = (DrawingAttributes)e.NewValue;

            // This can throw, so call it first
            inkCanvas.UpdateDynamicRenderer(newValue);

            // We only fire Changed event when there is an instance change.
            if ( !object.ReferenceEquals(oldValue, newValue) )
            {
                //we didn't throw, change our backing value
                oldValue.AttributeChanged -= new PropertyDataChangedEventHandler(inkCanvas.DefaultDrawingAttributes_Changed);
                DrawingAttributesReplacedEventArgs args =
                    new DrawingAttributesReplacedEventArgs(newValue, oldValue);

                newValue.AttributeChanged += new PropertyDataChangedEventHandler(inkCanvas.DefaultDrawingAttributes_Changed);
                inkCanvas.RaiseDefaultDrawingAttributeReplaced(args);
            }
        }

        /// <summary>
        /// Read/Write access to the EraserShape property.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public StylusShape EraserShape
        {
            get
            {
                VerifyAccess();
                if (_eraserShape == null)
                {
                    _eraserShape = new RectangleStylusShape(8f, 8f);
                }
                return _eraserShape;
            }
            set
            {
                VerifyAccess();
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                else
                {
                    // Invoke getter since this property is lazily created.
                    StylusShape oldShape = EraserShape;

                    _eraserShape = value;


                    if ( oldShape.Width != _eraserShape.Width || oldShape.Height != _eraserShape.Height
                        || oldShape.Rotation != _eraserShape.Rotation || oldShape.GetType() != _eraserShape.GetType())
                    {
                        EditingCoordinator.UpdatePointEraserCursor();
                    }
                }
            }
        }


        /// <summary>
        /// ActiveEditingMode
        /// </summary>
        internal static readonly DependencyPropertyKey ActiveEditingModePropertyKey =
                DependencyProperty.RegisterReadOnly(
                        "ActiveEditingMode",
                        typeof(InkCanvasEditingMode),
                        typeof(InkCanvas),
                        new FrameworkPropertyMetadata(InkCanvasEditingMode.Ink));

        /// <summary>
        /// ActiveEditingModeProperty Dependency Property
        /// </summary>
        public static readonly DependencyProperty ActiveEditingModeProperty = ActiveEditingModePropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the ActiveEditingMode
        /// </summary>
        public InkCanvasEditingMode ActiveEditingMode
        {
            get { return (InkCanvasEditingMode)GetValue(ActiveEditingModeProperty); }
        }

        /// <summary>
        /// The DependencyProperty for the EditingMode property.
        /// </summary>
        public static readonly DependencyProperty EditingModeProperty =
                DependencyProperty.Register(
                        "EditingMode",
                        typeof(InkCanvasEditingMode),
                        typeof(InkCanvas),
                        new FrameworkPropertyMetadata(
                                InkCanvasEditingMode.Ink,
                                new PropertyChangedCallback(OnEditingModeChanged)),
                        new ValidateValueCallback(ValidateEditingMode));


        /// <summary>
        /// Gets/Sets EditingMode
        /// </summary>
        public InkCanvasEditingMode EditingMode
        {
            get { return (InkCanvasEditingMode)GetValue(EditingModeProperty); }
            set { SetValue(EditingModeProperty, value); }
        }


        private static void OnEditingModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ( (InkCanvas)d ).RaiseEditingModeChanged(
                                new RoutedEventArgs(InkCanvas.EditingModeChangedEvent, d));
        }

        /// <summary>
        /// The DependencyProperty for the EditingModeInverted property.
        /// </summary>
        public static readonly DependencyProperty EditingModeInvertedProperty =
                DependencyProperty.Register(
                        "EditingModeInverted",
                        typeof(InkCanvasEditingMode),
                        typeof(InkCanvas),
                        new FrameworkPropertyMetadata(
                                InkCanvasEditingMode.EraseByStroke,
                                new PropertyChangedCallback(OnEditingModeInvertedChanged)),
                        new ValidateValueCallback(ValidateEditingMode));

        /// <summary>
        /// Gets/Sets EditingMode
        /// </summary>
        public InkCanvasEditingMode EditingModeInverted
        {
            get { return (InkCanvasEditingMode)GetValue(EditingModeInvertedProperty); }
            set { SetValue(EditingModeInvertedProperty, value); }
        }

        private static void OnEditingModeInvertedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ( (InkCanvas)d ).RaiseEditingModeInvertedChanged(
                new RoutedEventArgs(InkCanvas.EditingModeInvertedChangedEvent, d));
        }

        private static bool ValidateEditingMode(object value)
        {
            return EditingModeHelper.IsDefined((InkCanvasEditingMode)value);
        }

        /// <summary>
        /// This flag indicates whether the developer is using a custom mouse cursor.
        ///
        /// If this flag is true, we will never change the current cursor on them. Not
        /// on edit mode change.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool UseCustomCursor
        {
            get
            {
                VerifyAccess();
                return _useCustomCursor;
            }
            set
            {
                VerifyAccess();

                if ( _useCustomCursor != value )
                {
                    _useCustomCursor = value;
                    UpdateCursor();
                }
            }
        }

        /// <summary>
        /// Gets or set if moving of selection is enabled
        /// </summary>
        /// <value>bool</value>
        public bool MoveEnabled
        {
            get
            {
                VerifyAccess();
                return _editingCoordinator.MoveEnabled;
            }
            set
            {
                VerifyAccess();
                bool oldValue = _editingCoordinator.MoveEnabled;

                if (oldValue != value)
                {
                    _editingCoordinator.MoveEnabled = value;
                }
            }
        }

        /// <summary>
        /// Gets or set if resizing selection is enabled
        /// </summary>
        /// <value>bool</value>
        public bool ResizeEnabled
        {
            get
            {
                VerifyAccess();
                return _editingCoordinator.ResizeEnabled;
            }
            set
            {
                VerifyAccess();
                bool oldValue = _editingCoordinator.ResizeEnabled;

                if (oldValue != value)
                {
                    _editingCoordinator.ResizeEnabled = value;
                }
            }
        }


        /// <summary>
        /// Read/Write access to the DefaultPacketDescription property.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public StylusPointDescription DefaultStylusPointDescription
        {
            get
            {
                VerifyAccess();

                return _defaultStylusPointDescription;
            }
            set
            {
                VerifyAccess();

                //
                // no nulls allowed
                //
                if ( value == null )
                {
                    throw new ArgumentNullException("value");
                }

                _defaultStylusPointDescription = value;
            }
        }

        /// <summary>
        /// Read/Write the enabled ClipboardFormats
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IEnumerable<InkCanvasClipboardFormat> PreferredPasteFormats
        {
            get
            {
                VerifyAccess();

                return ClipboardProcessor.PreferredFormats;
            }
            set
            {
                VerifyAccess();

                // Cannot be null
                if ( value == null )
                {
                    // Null is not allowed as the argument value
                    throw new ArgumentNullException("value");
                }

                ClipboardProcessor.PreferredFormats = value;
            }
        }

        #endregion Public Properties

        #region Public Events

        /// <summary>
        ///     The StrokeErased Routed Event
        /// </summary>
        public static readonly RoutedEvent StrokeCollectedEvent =
            EventManager.RegisterRoutedEvent("StrokeCollected", RoutingStrategy.Bubble, typeof(InkCanvasStrokeCollectedEventHandler), typeof(InkCanvas));

        /// <summary>
        ///     Add / Remove StrokeCollected handler
        /// </summary>
        [Category("Behavior")]
        public event InkCanvasStrokeCollectedEventHandler StrokeCollected
        {
            add
            {
                AddHandler(InkCanvas.StrokeCollectedEvent, value);
            }

            remove
            {
                RemoveHandler(InkCanvas.StrokeCollectedEvent, value);
            }
        }

        /// <summary>
        /// Protected virtual version for developers deriving from InkCanvas.
        /// This method is what actually throws the event.
        /// </summary>
        /// <param name="e">InkCanvasStrokeCollectedEventArgs to raise the event with</param>
        protected virtual void OnStrokeCollected(InkCanvasStrokeCollectedEventArgs e)
        {
            // No need to invoke VerifyAccess since this method is thread free.

            if ( e == null )
            {
                throw new ArgumentNullException("e");
            }

            RaiseEvent(e);
        }

        /// <summary>
        /// Allows the InkCollectionBehavior to raise the StrokeCollected event via the protected virtual
        /// </summary>
        /// <param name="e">InkCanvasStrokeCollectedEventArgs to raise the event with</param>
        /// <param name="userInitiated">true only if 100% of the stylusPoints that makes up the stroke
        /// came from eventargs with the UserInitiated flag set to true</param>
        internal void RaiseGestureOrStrokeCollected(InkCanvasStrokeCollectedEventArgs e, bool userInitiated)
        {
            Debug.Assert(e != null, "EventArg can not be null");
            bool addStrokeToInkCanvas = true; // Initialize our flag.

            // The follow code raises Gesture event
            // The out-side code could throw exception in the their handlers. We use try/finally block to protect our status.
            try
            {
                //
                // perform gesture reco before raising this event
                // if we're in the right mode
                //
                //IMPORTANT: only call gesture recognition if userInitiated.  See SecurityNote.
                if (userInitiated)
                {
                    if ((this.ActiveEditingMode == InkCanvasEditingMode.InkAndGesture ||
                          this.ActiveEditingMode == InkCanvasEditingMode.GestureOnly) &&
                          this.GestureRecognizer.IsRecognizerAvailable)
                    {
                        StrokeCollection strokes = new StrokeCollection();
                        strokes.Add(e.Stroke);

                        //
                        // GestureRecognizer.Recognize demands unmanaged code, we assert it here
                        // as this codepath is only called in response to user input
                        //
                        ReadOnlyCollection<GestureRecognitionResult> results =
                            this.GestureRecognizer.CriticalRecognize(strokes);

                        if (results.Count > 0)
                        {
                            InkCanvasGestureEventArgs args =
                                new InkCanvasGestureEventArgs(strokes, results);

                            if (results[0].ApplicationGesture == ApplicationGesture.NoGesture)
                            {
                                //
                                // we set Cancel=true if we didn't detect a gesture
                                //
                                args.Cancel = true;
                            }
                            else
                            {
                                args.Cancel = false;
                            }

                            this.OnGesture(args);

                            //
                            // now that we've raised the Gesture event and the developer
                            // has had a chance to change args.Cancel, see what their intent is.
                            //
                            if (args.Cancel == false)
                            {
                                //bail out and don't add
                                //the stroke to InkCanvas.Strokes
                                addStrokeToInkCanvas = false; // Reset the flag.
                                return;
                            }
                        }
                    }
                }

                // Reset the flag.
                addStrokeToInkCanvas = false;

                //
                // only raise StrokeCollected if we're in InkCanvasEditingMode.Ink or InkCanvasEditingMode.InkAndGesture
                //
                if ( this.ActiveEditingMode == InkCanvasEditingMode.Ink ||
                    this.ActiveEditingMode == InkCanvasEditingMode.InkAndGesture )
                {
                    //add the stroke to the StrokeCollection and raise this event
                    this.Strokes.Add(e.Stroke);
                    this.OnStrokeCollected(e);
                }
            }
            finally
            {
                // If the gesture events are failed, we should still add Stroke to the InkCanvas so that the data won't be lost.
                if ( addStrokeToInkCanvas )
                {
                    this.Strokes.Add(e.Stroke);
                }
            }
        }

        /// <summary>
        ///     The Gesture Routed Event
        /// </summary>
        public static readonly RoutedEvent GestureEvent =
            EventManager.RegisterRoutedEvent("Gesture", RoutingStrategy.Bubble, typeof(InkCanvasGestureEventHandler), typeof(InkCanvas));

        /// <summary>
        ///     Add / Remove Gesture handler
        /// </summary>
        [Category("Behavior")]
        public event InkCanvasGestureEventHandler Gesture
        {
            add
            {
                AddHandler(InkCanvas.GestureEvent, value);
            }

            remove
            {
                RemoveHandler(InkCanvas.GestureEvent, value);
            }
        }

        /// <summary>
        /// Protected virtual version for developers deriving from InkCanvas.
        /// This method is what actually throws the event.
        /// </summary>
        /// <param name="e">InkCanvasGestureEventArgs to raise the event with</param>
        protected virtual void OnGesture(InkCanvasGestureEventArgs e)
        {
            // No need to invoke VerifyAccess since this method is thread free.

            if ( e == null )
            {
                throw new ArgumentNullException("e");
            }

            RaiseEvent(e);
        }

        /// <summary>
        /// Raised when the InkCanvas.Strokes StrokeCollection has been replaced with another one
        /// </summary>
        public event InkCanvasStrokesReplacedEventHandler StrokesReplaced;

        /// <summary>
        /// Protected virtual version for developers deriving from InkCanvas.
        /// This method is what actually throws the event.
        /// </summary>
        /// <param name="e">InkCanvasStrokesChangedEventArgs to raise the event with</param>
        protected virtual void OnStrokesReplaced(InkCanvasStrokesReplacedEventArgs e)
        {
            // No need to invoke VerifyAccess since this method is thread free.

            if ( e == null )
            {
                throw new ArgumentNullException("e");
            }
            if (null != this.StrokesReplaced)
            {
                StrokesReplaced(this, e);
            }
        }

        /// <summary>
        /// Raised when the InkCanvas.DefaultDrawingAttributes has been replaced with another one
        /// </summary>
        public event DrawingAttributesReplacedEventHandler DefaultDrawingAttributesReplaced;

        /// <summary>
        /// Protected virtual version for developers deriving from InkCanvas.
        /// This method is what actually throws the event.
        /// </summary>
        /// <param name="e">DrawingAttributesReplacedEventArgs to raise the event with</param>
        protected virtual void OnDefaultDrawingAttributesReplaced(DrawingAttributesReplacedEventArgs e)
        {
            // No need to invoke VerifyAccess since this method is thread free.

            if (e == null)
            {
                throw new ArgumentNullException("e");
            }
            if (null != this.DefaultDrawingAttributesReplaced)
            {
                DefaultDrawingAttributesReplaced(this, e);
            }
        }

        /// <summary>
        /// Private helper for raising DDAReplaced.  Invalidates the inking cursor
        /// </summary>
        /// <param name="e"></param>
        private void RaiseDefaultDrawingAttributeReplaced(DrawingAttributesReplacedEventArgs e)
        {
            this.OnDefaultDrawingAttributesReplaced(e);

            // Invalidate the inking cursor
            _editingCoordinator.InvalidateBehaviorCursor(_editingCoordinator.InkCollectionBehavior);
        }

        /// <summary>
        ///     Event corresponds to ActiveEditingModeChanged
        /// </summary>
        public static readonly RoutedEvent ActiveEditingModeChangedEvent =
            EventManager.RegisterRoutedEvent("ActiveEditingModeChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(InkCanvas));

        /// <summary>
        ///     Add / Remove ActiveEditingModeChanged handler
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler ActiveEditingModeChanged
        {
            add
            {
                AddHandler(InkCanvas.ActiveEditingModeChangedEvent, value);
            }
            remove
            {
                RemoveHandler(InkCanvas.ActiveEditingModeChangedEvent, value);
            }
        }

        /// <summary>
        /// Protected virtual version for developers deriving from InkCanvas.
        /// This method is what actually throws the event.
        /// </summary>
        /// <param name="e">EventArgs to raise the event with</param>
        protected virtual void OnActiveEditingModeChanged(RoutedEventArgs e)
        {
            // No need to invoke VerifyAccess since this method is thread free.

            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            RaiseEvent(e);
        }

        /// <summary>
        /// Private helper that raises ActiveEditingModeChanged
        /// </summary>
        /// <param name="e">EventArgs to raise the event with</param>
        internal void RaiseActiveEditingModeChanged(RoutedEventArgs e)
        {
            Debug.Assert(e != null, "EventArg can not be null");

            InkCanvasEditingMode mode = this.ActiveEditingMode;
            if (mode != _editingCoordinator.ActiveEditingMode)
            {
                //change our DP, then raise the event via our protected override
                SetValue(ActiveEditingModePropertyKey, _editingCoordinator.ActiveEditingMode);

                this.OnActiveEditingModeChanged(e);
            }
        }



        /// <summary>
        ///     Event corresponds to EditingModeChanged
        /// </summary>
        public static readonly RoutedEvent EditingModeChangedEvent =
            EventManager.RegisterRoutedEvent("EditingModeChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(InkCanvas));

        /// <summary>
        ///     Add / Remove EditingModeChanged handler
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler EditingModeChanged
        {
            add
            {
                AddHandler(InkCanvas.EditingModeChangedEvent, value);
            }

            remove
            {
                RemoveHandler(InkCanvas.EditingModeChangedEvent, value);
            }
        }

        /// <summary>
        /// Protected virtual version for developers deriving from InkCanvas.
        /// This method is what actually throws the event.
        /// </summary>
        /// <param name="e">EventArgs to raise the event with</param>
        protected virtual void OnEditingModeChanged(RoutedEventArgs e)
        {
            // No need to invoke VerifyAccess since this method is thread free.

            if ( e == null )
            {
                throw new ArgumentNullException("e");
            }

            RaiseEvent(e);
        }
        /// <summary>
        /// Private helper that raises EditingModeChanged but first
        /// talks to the InkEditor about it
        /// </summary>
        /// <param name="e">EventArgs to raise the event with</param>
        private void RaiseEditingModeChanged(RoutedEventArgs e)
        {
            Debug.Assert(e != null, "EventArg can not be null");

            _editingCoordinator.UpdateEditingState(false /* EditingMode */);

            this.OnEditingModeChanged(e);
        }

        //note: there is no need for an internal RaiseEditingModeInvertedChanging
        //since this isn't a dynamic property and therefore can not be set
        //outside of this class

        /// <summary>
        ///     Event corresponds to EditingModeInvertedChanged
        /// </summary>
        public static readonly RoutedEvent EditingModeInvertedChangedEvent =
            EventManager.RegisterRoutedEvent("EditingModeInvertedChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(InkCanvas));

        /// <summary>
        ///     Add / Remove EditingModeChanged handler
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler EditingModeInvertedChanged
        {
            add
            {
                AddHandler(InkCanvas.EditingModeInvertedChangedEvent, value);
            }

            remove
            {
                RemoveHandler(InkCanvas.EditingModeInvertedChangedEvent, value);
            }
        }

        /// <summary>
        /// Protected virtual version for developers deriving from InkCanvas.
        /// This method is what actually throws the event.
        /// </summary>
        /// <param name="e">EventArgs to raise the event with</param>
        protected virtual void OnEditingModeInvertedChanged(RoutedEventArgs e)
        {
            // No need to invoke VerifyAccess since this method is thread free.

            if ( e == null )
            {
                throw new ArgumentNullException("e");
            }

            RaiseEvent(e);
        }
        /// <summary>
        /// Private helper that raises EditingModeInvertedChanged but first
        /// talks to the InkEditor about it
        /// </summary>
        /// <param name="e">EventArgs to raise the event with</param>
        private void RaiseEditingModeInvertedChanged(RoutedEventArgs e)
        {
            Debug.Assert(e != null, "EventArg can not be null");

            _editingCoordinator.UpdateEditingState(true /* EditingModeInverted */);

            this.OnEditingModeInvertedChanged(e);
        }

        /// <summary>
        /// Occurs when the user has moved the selection, after they lift their stylus to commit the change.
        /// This event allows the developer to cancel the move.
        /// </summary>
        public event  InkCanvasSelectionEditingEventHandler SelectionMoving;
        /// <summary>
        /// Protected virtual version for developers deriving from InkCanvas.
        /// This method is what actually throws the event.
        /// </summary>
        /// <param name="e"> InkCanvasSelectionEditingEventArgs to raise the event with</param>
        protected virtual void OnSelectionMoving( InkCanvasSelectionEditingEventArgs e)
        {
            // No need to invoke VerifyAccess since this method is thread free.

            if ( e == null )
            {
                throw new ArgumentNullException("e");
            }
            if (null != SelectionMoving)
            {
                SelectionMoving(this, e);
            }
        }

        /// <summary>
        /// Allows the EditingBehaviors to raise the SelectionMoving event via the protected virtual
        /// </summary>
        /// <param name="e"> InkCanvasSelectionEditingEventArgs to raise the event with</param>
        internal void RaiseSelectionMoving( InkCanvasSelectionEditingEventArgs e)
        {
            Debug.Assert(e != null, "EventArg can not be null");
            this.OnSelectionMoving(e);
        }

        /// <summary>
        /// Occurs when the user has moved the selection, after they lift their stylus to commit the change.
        /// This event allows the developer to cancel the move.
        /// </summary>
        public event EventHandler SelectionMoved;
        /// <summary>
        /// Protected virtual version for developers deriving from InkCanvas.
        /// This method is what actually throws the event.
        /// </summary>
        /// <param name="e">EventArgs to raise the event with</param>
        protected virtual void OnSelectionMoved(EventArgs e)
        {
            // No need to invoke VerifyAccess since this method is thread free.

            if ( e == null )
            {
                throw new ArgumentNullException("e");
            }
            if (null != SelectionMoved)
            {
                SelectionMoved(this, e);
            }
        }

        /// <summary>
        /// Allows the EditingBehaviors to raise the SelectionMoved event via the protected virtual
        /// </summary>
        /// <param name="e">EventArgs to raise the event with</param>
        internal void RaiseSelectionMoved(EventArgs e)
        {
            Debug.Assert(e != null, "EventArg can not be null");
            
            this.OnSelectionMoved(e);
            // Update the cursor of SelectionEditor behavior.
            EditingCoordinator.SelectionEditor.OnInkCanvasSelectionChanged();
        }

        /// <summary>
        /// Occurs when the user has erased Strokes using the erase behavior
        ///
        /// This event allows the developer to cancel the erase -- therefore, the Stroke should not disappear until
        /// this event has finished.
        /// </summary>
        public event InkCanvasStrokeErasingEventHandler StrokeErasing;
        /// <summary>
        /// Protected virtual version for developers deriving from InkCanvas.
        /// This method is what actually throws the event.
        /// </summary>
        /// <param name="e">InkCanvasStrokeErasingEventArgs to raise the event with</param>
        protected virtual void OnStrokeErasing(InkCanvasStrokeErasingEventArgs e)
        {
            // No need to invoke VerifyAccess since this method is thread free.

            if ( e == null )
            {
                throw new ArgumentNullException("e");
            }
            if (null != StrokeErasing)
            {
                StrokeErasing(this, e);
            }
        }

        /// <summary>
        /// Allows the EditingBehaviors to raise the InkErasing event via the protected virtual
        /// </summary>
        /// <param name="e">InkCanvasStrokeErasingEventArgs to raise the event with</param>
        internal void RaiseStrokeErasing(InkCanvasStrokeErasingEventArgs e)
        {
            Debug.Assert(e != null, "EventArg can not be null");
            this.OnStrokeErasing(e);
        }

        /// <summary>
        ///     The StrokeErased Routed Event
        /// </summary>
        public static readonly RoutedEvent StrokeErasedEvent =
            EventManager.RegisterRoutedEvent("StrokeErased", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(InkCanvas));

        /// <summary>
        ///     Add / Remove EditingModeChanged handler
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler StrokeErased
        {
            add
            {
                AddHandler(InkCanvas.StrokeErasedEvent, value);
            }

            remove
            {
                RemoveHandler(InkCanvas.StrokeErasedEvent, value);
            }
        }

        /// <summary>
        /// Protected virtual version for developers deriving from InkCanvas.
        /// This method is what actually throws the event.
        /// </summary>
        /// <param name="e">EventArgs to raise the event with</param>
        protected virtual void OnStrokeErased(RoutedEventArgs e)
        {
            // No need to invoke VerifyAccess since this method is thread free.

            if ( e == null )
            {
                throw new ArgumentNullException("e");
            }
            RaiseEvent(e);
        }

        /// <summary>
        /// Allows the EditingBehaviors to raise the InkErasing event via the protected virtual
        /// </summary>
        internal void RaiseInkErased()
        {
            this.OnStrokeErased(
                new RoutedEventArgs(InkCanvas.StrokeErasedEvent, this));
        }

        /// <summary>
        /// Occurs when the user has resized the selection, after they lift their stylus to commit the change.
        /// This event allows the developer to cancel the resize.
        /// </summary>
        public event  InkCanvasSelectionEditingEventHandler SelectionResizing;
        /// <summary>
        /// Protected virtual version for developers deriving from InkCanvas.
        /// This method is what actually throws the event.
        /// </summary>
        /// <param name="e"> InkCanvasSelectionEditingEventArgs to raise the event with</param>
        protected virtual void OnSelectionResizing( InkCanvasSelectionEditingEventArgs e)
        {
            // No need to invoke VerifyAccess since this method is thread free.

            if ( e == null )
            {
                throw new ArgumentNullException("e");
            }
            if (null != SelectionResizing)
            {
                SelectionResizing(this, e);
            }
        }

        /// <summary>
        /// Allows the EditingBehaviors to raise the SelectionResizing event via the protected virtual
        /// </summary>
        /// <param name="e"> InkCanvasSelectionEditingEventArgs to raise the event with</param>
        internal void RaiseSelectionResizing( InkCanvasSelectionEditingEventArgs e)
        {
            Debug.Assert(e != null, "EventArg can not be null");
            this.OnSelectionResizing(e);
        }

        /// <summary>
        /// Occurs when the selection has been resized via UI interaction.
        /// </summary>
        public event EventHandler SelectionResized;
        /// <summary>
        /// Protected virtual version for developers deriving from InkCanvas.
        /// This method is what actually throws the event.
        /// </summary>
        /// <param name="e">EventArgs to raise the event with</param>
        protected virtual void OnSelectionResized(EventArgs e)
        {
            // No need to invoke VerifyAccess since this method is thread free.

            if ( e == null )
            {
                throw new ArgumentNullException("e");
            }
            if (null != SelectionResized)
            {
                SelectionResized(this, e);
            }
        }

        /// <summary>
        /// Allows the EditingBehaviors to raise the SelectionResized event via the protected virtual
        /// </summary>
        /// <param name="e">EventArgs to raise the event with</param>
        internal void RaiseSelectionResized(EventArgs e)
        {
            Debug.Assert(e != null, "EventArg can not be null");

            this.OnSelectionResized(e);
            // Update the cursor of SelectionEditor behavior.
            EditingCoordinator.SelectionEditor.OnInkCanvasSelectionChanged();
        }

        /// <summary>
        /// Occurs when the selection has been changed, either using the lasso or programmatically.
        /// This event allows the developer to cancel the change.
        /// </summary>
        public event InkCanvasSelectionChangingEventHandler SelectionChanging;
        /// <summary>
        /// Protected virtual version for developers deriving from InkCanvas.
        /// This method is what actually throws the event.
        /// </summary>
        /// <param name="e">InkCanvasSelectionChangingEventArgs to raise the event with</param>
        protected virtual void OnSelectionChanging(InkCanvasSelectionChangingEventArgs e)
        {
            // No need to invoke VerifyAccess since this method is thread free.

            if ( e == null )
            {
                throw new ArgumentNullException("e");
            }
            if (null != SelectionChanging)
            {
                SelectionChanging(this, e);
            }
        }

        /// <summary>
        /// Allows the EditingBehaviors to raise the SelectionChanging event via the protected virtual
        /// </summary>
        /// <param name="e">InkCanvasSelectionChangingEventArgs to raise the event with</param>
        private void RaiseSelectionChanging(InkCanvasSelectionChangingEventArgs e)
        {
            Debug.Assert(e != null, "EventArg can not be null");
            this.OnSelectionChanging(e);
        }

        /// <summary>
        /// Occurs when the selection has been changed
        /// </summary>
        public event EventHandler SelectionChanged;
        /// <summary>
        /// Protected virtual version for developers deriving from InkCanvas.
        /// This method is what actually throws the event.
        /// </summary>
        /// <param name="e">EventArgs to raise the event with</param>
        protected virtual void OnSelectionChanged(EventArgs e)
        {
            // No need to invoke VerifyAccess since this method is thread free.

            if ( e == null )
            {
                throw new ArgumentNullException("e");
            }
            if (null != SelectionChanged)
            {
                SelectionChanged(this, e);
            }
        }

        /// <summary>
        /// Allows the EditingBehaviors to raise the SelectionChanged event via the protected virtual
        /// </summary>
        /// <param name="e">EventArgs to raise the event with</param>
        internal void RaiseSelectionChanged(EventArgs e)
        {
            Debug.Assert(e != null, "EventArg can not be null");

            this.OnSelectionChanged(e);
            // Update the cursor of SelectionEditor behavior.
            EditingCoordinator.SelectionEditor.OnInkCanvasSelectionChanged();
        }

        /// <summary>
        /// The InkCanvas uses an inner Canvas to host children.  When the inner Canvas's children
        /// are changed, we need to call the protected virtual OnVisualChildrenChanged on the InkCanvas
        /// so that subclasses can be notified
        /// </summary>
        internal void RaiseOnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            this.OnVisualChildrenChanged(visualAdded, visualRemoved);
        }

        #endregion Public Events

        #region Public Methods

        /// <summary>
        /// Returns the enabled gestures.  This method throws an exception if GestureRecognizerAvailable
        /// is false
        /// </summary>
        /// <returns></returns>
        public ReadOnlyCollection<ApplicationGesture> GetEnabledGestures()
        {
            // No need to invoke VerifyAccess since it's checked in GestureRecognizer.GetEnabledGestures.

            //gestureRecognizer throws appropriately if there is no gesture recognizer available
            return new ReadOnlyCollection<ApplicationGesture>(this.GestureRecognizer.GetEnabledGestures());
        }

        /// <summary>
        /// Sets the enabled gestures.  This method throws an exception if GestureRecognizerAvailable
        /// is false
        /// </summary>
        /// <returns></returns>
        public void SetEnabledGestures(IEnumerable<ApplicationGesture> applicationGestures)
        {
            // No need to invoke VerifyAccess since it's checked in GestureRecognizer.GetEnabledGestures.

            //gestureRecognizer throws appropriately if there is no gesture recognizer available
            this.GestureRecognizer.SetEnabledGestures(applicationGestures);
        }

        /// <summary>
        /// Get the selection bounds.
        /// </summary>
        /// <returns></returns>
        public Rect GetSelectionBounds()
        {
            VerifyAccess();

            return InkCanvasSelection.SelectionBounds;
        }

        /// <summary>
        /// provides access to the currently selected elements which are children of this InkCanvas
        /// </summary>
        public ReadOnlyCollection<UIElement> GetSelectedElements()
        {
            VerifyAccess();
            return InkCanvasSelection.SelectedElements;
        }

        /// <summary>
        /// provides read access to the currently selected strokes
        /// </summary>
        public StrokeCollection GetSelectedStrokes()
        {
            VerifyAccess();

            StrokeCollection sc = new StrokeCollection();
            sc.Add(InkCanvasSelection.SelectedStrokes);
            return sc;
        }

        /// <summary>
        /// Overload which calls the more complex version, passing null for selectedElements
        /// </summary>
        /// <param name="selectedStrokes">The strokes to select</param>
        public void Select(StrokeCollection selectedStrokes)
        {
            // No need to invoke VerifyAccess since this call is forwarded.
            Select(selectedStrokes, null);
        }

        /// <summary>
        /// Overload which calls the more complex version, passing null for selectedStrokes
        /// </summary>
        /// <param name="selectedElements">The elements to select</param>
        public void Select(IEnumerable<UIElement> selectedElements)
        {
            // No need to invoke VerifyAccess since this call is forwarded.
            Select(null, selectedElements);
        }

        /// <summary>
        /// Overload which calls the more complex version, passing null for selectedStrokes
        /// </summary>
        /// <param name="selectedStrokes">The strokes to select</param>
        /// <param name="selectedElements">The elements to select</param>
        public void Select(StrokeCollection selectedStrokes, IEnumerable<UIElement> selectedElements)
        {
            VerifyAccess();

            // 
            // Try to switch to Select mode first. If we fail to change the mode, then just simply no-op.
            if ( EnsureActiveEditingMode(InkCanvasEditingMode.Select) )
            {
                //
                // validate
                //
                UIElement[] validElements = ValidateSelectedElements(selectedElements);
                StrokeCollection validStrokes = ValidateSelectedStrokes(selectedStrokes);

                //
                // this will raise the 'SelectionChanging' event ONLY if the selection
                // is actually different
                //
                ChangeInkCanvasSelection(validStrokes, validElements);
            }
        }

        /// <summary>
        /// Hit test on the selection
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public InkCanvasSelectionHitResult HitTestSelection(Point point)
        {
            VerifyAccess();

            // Ensure the visual tree.
            if ( _localAdornerDecorator == null )
            {
                ApplyTemplate();
            }

            return InkCanvasSelection.HitTestSelection(point);
        }

        /// <summary>
        /// Copy the current selection in the InkCanvas to the clipboard
        /// </summary>
        public void CopySelection()
        {
            VerifyAccess();
            PrivateCopySelection();
        }

        /// <summary>
        /// Copy the current selection in the InkCanvas to the clipboard and then delete it
        /// </summary>
        public void CutSelection()
        {
            VerifyAccess();

            // Copy first
            InkCanvasClipboardDataFormats copiedDataFormats = PrivateCopySelection();

            // Don't even bother if we don't have a selection.
            if ( copiedDataFormats != InkCanvasClipboardDataFormats.None )
            {
                // Then delete the current selection. Note the XAML format won't be avaliable under Partial
                // Trust. So, the selected element shouldn't be copied or removed.
                DeleteCurrentSelection(
                    /* We want to delete the selected Strokes if there is ISF and/or XAML data being copied */
                    (copiedDataFormats &
                        (InkCanvasClipboardDataFormats.ISF | InkCanvasClipboardDataFormats.XAML)) != 0,
                    /* We only want to delete the selected elements if there is XAML data being copied */
                    (copiedDataFormats & InkCanvasClipboardDataFormats.XAML) != 0);
            }
        }

        /// <summary>
        /// Paste the contents of the clipboard into the InkCanvas
        /// </summary>
        public void Paste()
        {
            // No need to call VerifyAccess since this call is forwarded.

            // We always paste the data to the default location which is (0,0).
            Paste(new Point(c_pasteDefaultLocation, c_pasteDefaultLocation));
        }

        /// <summary>
        /// Paste the contents of the clipboard to the specified location in the InkCanvas
        /// </summary>
        public void Paste(Point point)
        {
            VerifyAccess();

            if (DoubleUtil.IsNaN(point.X) ||
                DoubleUtil.IsNaN(point.Y) ||
                Double.IsInfinity(point.X)||
                Double.IsInfinity(point.Y) )
            {
                    throw new ArgumentException(SR.Get(SRID.InvalidPoint), "point");
            }


            //
            // only do this if the user is not editing (input active)
            // or we will violate a dispatcher lock
            //
            if (!_editingCoordinator.UserIsEditing)
            {
                IDataObject dataObj = null;
                try
                {
                    dataObj = Clipboard.GetDataObject();
                }
                catch (ExternalException)
                {
                    //harden against ExternalException
                    return;
                }
                if (dataObj != null)
                {
                    PasteFromDataObject(dataObj, point);
                }
            }
        }

        /// <summary>
        /// Return true if clipboard contents can be pasted into the InkCanvas.
        /// </summary>
        public bool CanPaste()
        {
            VerifyAccess();

            bool ret = false;
            //
            // can't paste if the user is editing (input active)
            // or we will violate a dispatcher lock
            //
            if (_editingCoordinator.UserIsEditing)
            {
                return false;
            }

            ret = PrivateCanPaste();

            return ret;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  IAddChild Interface
        //
        //------------------------------------------------------

        #region IAddChild Interface

        ///<summary>
        /// Called to Add the object as a Child.
        ///</summary>
        ///<param name="value">
        /// Object to add as a child
        ///</param>
        void IAddChild.AddChild(Object value)
        {
            //             VerifyAccess();

            if ( value == null )
            {
                throw new ArgumentNullException("value");
            }

            ( (IAddChild)InnerCanvas ).AddChild(value);
        }

        ///<summary>
        /// Called when text appears under the tag in markup.
        ///</summary>
        ///<param name="textData">
        /// Text to Add to the Canvas
        ///</param>
        void IAddChild.AddText(string textData)
        {
            //             VerifyAccess();

            ( (IAddChild)InnerCanvas ).AddText(textData);
        }

        #endregion IAddChild Interface

        //------------------------------------------------------
        //
        //  Protected Properties
        //
        //------------------------------------------------------

        #region Protected Properties

        /// <summary>
        /// Returns enumerator to logical children.
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                //        VerifyAccess( );

                // Return the private logical children of the InnerCanvas
                return ( (InkCanvasInnerCanvas)InnerCanvas).PrivateLogicalChildren;
            }
        }

        /// <summary>
        /// Protected DynamicRenderer property.
        /// </summary>
        protected DynamicRenderer DynamicRenderer
        {
            get
            {
                VerifyAccess();
                return InternalDynamicRenderer;
            }
            set
            {
                VerifyAccess();
                if (!object.ReferenceEquals(value, _dynamicRenderer))
                {
                    int previousIndex = -1;
                    //remove the existing plugin
                    if (_dynamicRenderer != null)
                    {
                        //remove the plugin from the collection
                        previousIndex = this.StylusPlugIns.IndexOf(_dynamicRenderer);
                        if (-1 != previousIndex)
                        {
                            this.StylusPlugIns.RemoveAt(previousIndex);
                        }

                        //remove the plugin's visual from the InkPresenter
                        if (this.InkPresenter.ContainsAttachedVisual(_dynamicRenderer.RootVisual))
                        {
                            this.InkPresenter.DetachVisuals(_dynamicRenderer.RootVisual);
                        }
                    }

                    _dynamicRenderer = value;

                    if (_dynamicRenderer != null) //null is acceptable
                    {
                        //remove the plugin from the collection
                        if (!this.StylusPlugIns.Contains(_dynamicRenderer))
                        {
                            if (-1 != previousIndex)
                            {
                                //insert the new DR in the same location as the old one
                                this.StylusPlugIns.Insert(previousIndex, _dynamicRenderer);
                            }
                            else
                            {
                                this.StylusPlugIns.Add(_dynamicRenderer);
                            }
                        }

                        //refer to the same DrawingAttributes as the InkCanvas
                        _dynamicRenderer.DrawingAttributes = this.DefaultDrawingAttributes;

                        //attach the DynamicRenderer if it is not already
                        if (!(this.InkPresenter.ContainsAttachedVisual(_dynamicRenderer.RootVisual)) &&
                            _dynamicRenderer.Enabled &&
                            _dynamicRenderer.RootVisual != null)
                        {
                            this.InkPresenter.AttachVisuals(_dynamicRenderer.RootVisual, this.DefaultDrawingAttributes);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Protected read only access to the InkPresenter this InkCanvas uses
        /// </summary>
        protected InkPresenter InkPresenter
        {
            get
            {
                VerifyAccess();
                if ( _inkPresenter == null )
                {
                    _inkPresenter = new InkPresenter();

                    // Bind the InkPresenter.Strokes to InkCanvas.Strokes
                    Binding strokes = new Binding();
                    strokes.Path = new PropertyPath(InkCanvas.StrokesProperty);
                    strokes.Mode = BindingMode.OneWay;
                    strokes.Source = this;
                    _inkPresenter.SetBinding(InkPresenter.StrokesProperty, strokes);
                }
                return _inkPresenter;
            }
        }

        #endregion Protected Properties

        #region Internal Properties / Methods


        /// <summary>
        /// Deselect the current selection
        /// </summary>
        internal static readonly RoutedCommand DeselectCommand = new RoutedCommand("Deselect", typeof(InkCanvas));

        /// <summary>
        /// UserInitiatedCanPaste
        /// </summary>
        /// <returns></returns>
        private bool UserInitiatedCanPaste()
        {
            return PrivateCanPaste();
        }

        /// <summary>
        /// PrivateCanPaste
        /// </summary>
        /// <returns></returns>
        private bool PrivateCanPaste()
        {
            bool canPaste = false;
            IDataObject dataObj = null;
            try
            {
                dataObj = Clipboard.GetDataObject();
            }
            catch (ExternalException)
            {
                //harden against ExternalException
                return false;
            }
            if ( dataObj != null )
            {
                canPaste = ClipboardProcessor.CheckDataFormats(dataObj);
            }

            return canPaste;
        }

        /// <summary>
        /// This method pastes data from an IDataObject object
        /// </summary>
        internal void PasteFromDataObject(IDataObject dataObj, Point point)
        {
            // Reset the current selection
            ClearSelection(false);

            // Assume that there is nothing to be selected.
            StrokeCollection newStrokes = new StrokeCollection();
            List<UIElement> newElements = new List<UIElement>();

            // Paste the data from the data object.
            if ( !ClipboardProcessor.PasteData(dataObj, ref newStrokes, ref newElements) )
            {
                // Paste was failed.
                return;
            }
            else if ( newStrokes.Count == 0 && newElements.Count == 0 )
            {
                // Nothing has been received from the clipboard.
                return;
            }

            // We add elements here. Then we have to wait for the layout update.
            UIElementCollection children = Children;
            foreach ( UIElement element in newElements )
            {
                children.Add(element);
            }

            if ( newStrokes != null )
            {
                Strokes.Add(newStrokes);
            }

            try
            {
                // We should fire SelectionChanged event if the current editing mode is Select.
                CoreChangeSelection(newStrokes, newElements.ToArray(), EditingMode == InkCanvasEditingMode.Select);
            }
            finally
            {
                // Now move the selection to the desired location.
                Rect bounds = GetSelectionBounds( );
                InkCanvasSelection.CommitChanges(Rect.Offset(bounds, -bounds.Left + point.X, -bounds.Top + point.Y), false);

                if (EditingMode != InkCanvasEditingMode.Select)
                {
                    // Clear the selection without the event if the editing mode is not Select.
                    ClearSelection(false);
                }
            }
        }

        /// <summary>
        /// Copies the InkCanvas contents to a DataObject and returns it to the caller.
        ///  Can return NULL for DataObject.
        /// </summary>
        private InkCanvasClipboardDataFormats CopyToDataObject()
        {
             DataObject dataObj;
            dataObj = new DataObject();
            InkCanvasClipboardDataFormats copiedDataFormats = InkCanvasClipboardDataFormats.None;

            // Try to copy the data from the InkCanvas to the clipboard.
            copiedDataFormats = ClipboardProcessor.CopySelectedData(dataObj);

            if ( copiedDataFormats != InkCanvasClipboardDataFormats.None )
            {
                // Put our data object into the clipboard.
                Clipboard.SetDataObject(dataObj, true);
            }

            return copiedDataFormats;
        }

        /// <summary>
        /// Read-only property of the associated EditingCoordinator.
        /// </summary>
        internal EditingCoordinator EditingCoordinator
        {
            get
            {
                return _editingCoordinator;
            }
        }

        /// <summary>
        /// Internal access to the protected DynamicRenderer.  Can be null.
        /// </summary>
        internal DynamicRenderer InternalDynamicRenderer
        {
            get
            {
                return _dynamicRenderer;
            }
        }

        /// <summary>
        /// Return the inner Canvas.
        /// </summary>
        internal InkCanvasInnerCanvas InnerCanvas
        {
            get
            {
                // We have to create our visual at this point.
                if (_innerCanvas == null)
                {
                    // Create our InnerCanvas to change the logical parent of Canvas' children.
                    _innerCanvas = new InkCanvasInnerCanvas(this);

                    // Bind the inner Canvas' Background to InkCanvas' Background
                    Binding background = new Binding();
                    background.Path = new PropertyPath(InkCanvas.BackgroundProperty);
                    background.Mode = BindingMode.OneWay;
                    background.Source = this;
                    _innerCanvas.SetBinding(Panel.BackgroundProperty, background);
                }

                return _innerCanvas;
            }
        }

        /// <summary>
        /// Internal access to the current selection
        /// </summary>
        internal InkCanvasSelection InkCanvasSelection
        {
            get
            {
                if ( _selection == null )
                {
                    _selection = new InkCanvasSelection(this);
                }

                return _selection;
            }
        }


        /// <summary>
        /// Internal helper called by the LassoSelectionBehavior
        /// </summary>
        internal void BeginDynamicSelection(Visual visual)
        {
            EditingCoordinator.DebugCheckActiveBehavior(EditingCoordinator.LassoSelectionBehavior);

            _dynamicallySelectedStrokes = new StrokeCollection();

            InkPresenter.AttachVisuals(visual, new DrawingAttributes());
        }

        /// <summary>
        /// Internal helper called by LassoSelectionBehavior to update the display
        /// of dynamically added strokes
        /// </summary>
        internal void UpdateDynamicSelection(   StrokeCollection strokesToDynamicallySelect,
                                                StrokeCollection strokesToDynamicallyUnselect)
        {
            EditingCoordinator.DebugCheckActiveBehavior(EditingCoordinator.LassoSelectionBehavior);

            //
            // update our internal stroke collections used by dynamic selection
            //
            if (strokesToDynamicallySelect != null)
            {
                foreach (Stroke s in strokesToDynamicallySelect)
                {
                    _dynamicallySelectedStrokes.Add(s);
                    s.IsSelected = true;
                }
            }

            if (strokesToDynamicallyUnselect != null)
            {
                foreach (Stroke s in strokesToDynamicallyUnselect)
                {
                    System.Diagnostics.Debug.Assert(_dynamicallySelectedStrokes.Contains(s));
                    _dynamicallySelectedStrokes.Remove(s);
                    s.IsSelected = false;
                }
            }
        }

        /// <summary>
        /// Internal helper used by LassoSelectionBehavior
        /// </summary>
        internal StrokeCollection EndDynamicSelection(Visual visual)
        {
            EditingCoordinator.DebugCheckActiveBehavior(EditingCoordinator.LassoSelectionBehavior);

            InkPresenter.DetachVisuals(visual);

            StrokeCollection selectedStrokes = _dynamicallySelectedStrokes;
            _dynamicallySelectedStrokes = null;

            return selectedStrokes;
        }

        /// <summary>
        /// Clears the selection in the ink canvas
        /// and returns a bool indicating if the selection was actually cleared
        /// (developers can cancel selectionchanging)
        ///
        /// If the InkCanvas has no selection, selectionchanging is not raised
        /// and this method returns true
        ///
        /// used by InkEditor during editing operations
        /// </summary>
        /// <returns>true if selection was cleared even after raising selectionchanging</returns>
        internal bool ClearSelectionRaiseSelectionChanging()
        {
            if ( !InkCanvasSelection.HasSelection )
            {
                return true;
            }

            //
            // attempt to clear selection
            //
            ChangeInkCanvasSelection(new StrokeCollection(), new UIElement[]{});

            return !InkCanvasSelection.HasSelection;
        }

        /// <summary>
        /// ClearSelection
        ///     Called by:
        ///         PasteFromDataObject
        ///         EditingCoordinator.UpdateEditingState
        /// </summary>
        internal void ClearSelection(bool raiseSelectionChangedEvent)
        {
            if ( InkCanvasSelection.HasSelection )
            {
                // Reset the current selection
                CoreChangeSelection(new StrokeCollection(), new UIElement[] { }, raiseSelectionChangedEvent);
            }
        }


        /// <summary>
        /// Helper that creates selection for an InkCanvas.  Used by the SelectedStrokes and
        /// SelectedElements properties
        /// </summary>
        internal void ChangeInkCanvasSelection(StrokeCollection strokes, UIElement[] elements)
        {
            //validate in debug only for this internal static
            Debug.Assert(strokes != null
                        && elements != null,
                        "Invalid arguments in ChangeInkCanvasSelection");

            bool strokesAreDifferent;
            bool elementsAreDifferent;
            InkCanvasSelection.SelectionIsDifferentThanCurrent(strokes, out strokesAreDifferent, elements, out elementsAreDifferent);
            if ( strokesAreDifferent || elementsAreDifferent )
            {
                InkCanvasSelectionChangingEventArgs args = new InkCanvasSelectionChangingEventArgs(strokes, elements);

                StrokeCollection validStrokes = strokes;
                UIElement[] validElements = elements;

                this.RaiseSelectionChanging(args);

                //now that the event has been raised and all of the delegates
                //have had their way with it, process the result
                if ( !args.Cancel )
                {
                    //
                    // rock and roll, time to validate our arguments
                    // note: these event args are visible outside the apis,
                    // so we need to validate them again
                    //

                    // PERF-2006/05/02-WAYNEZEN,
                    // Check our internal flag. If the SelectedStrokes has been changed, we shouldn't do any extra work here.
                    if ( args.StrokesChanged )
                    {
                        validStrokes = ValidateSelectedStrokes(args.GetSelectedStrokes());
                        int countOldSelectedStrokes = strokes.Count;
                        for ( int i = 0; i < countOldSelectedStrokes; i++ )
                        {
                            // PERF-2006/05/02-WAYNEZEN,
                            // We only have to reset IsSelected for the elements no longer exists in the new collection.
                            if ( !validStrokes.Contains(strokes[i]) )
                            {
                                // 
                                // Make sure we reset the IsSelected property which could have been 
                                // set to true in the dynamic selection.
                                strokes[i].IsSelected = false;
                            }
                        }
                    }


                    // PERF-2006/05/02-WAYNEZEN,
                    // Check our internal flag. If the SelectedElements has been changed, we shouldn't do any extra work here.
                    if ( args.ElementsChanged )
                    {
                        validElements = ValidateSelectedElements(args.GetSelectedElements());
                    }

                    CoreChangeSelection(validStrokes, validElements, true);
                }
                else
                {
                    StrokeCollection currentSelectedStrokes = InkCanvasSelection.SelectedStrokes;
                    int countOldSelectedStrokes = strokes.Count;
                    for ( int i = 0; i < countOldSelectedStrokes; i++ )
                    {
                        // Make sure we reset the IsSelected property which could have been 
                        // set to true in the dynamic selection but not being selected previously.
                        if ( !currentSelectedStrokes.Contains(strokes[i]) )
                        {
                            strokes[i].IsSelected = false;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Helper method used by ChangeInkCanvasSelection and directly by ClearSelectionWithoutSelectionChanging
        /// </summary>
        /// <param name="validStrokes">validStrokes</param>
        /// <param name="validElements">validElements</param>
        /// <param name="raiseSelectionChanged">raiseSelectionChanged</param>
        private void CoreChangeSelection(StrokeCollection validStrokes, IList<UIElement> validElements, bool raiseSelectionChanged)
        {
            InkCanvasSelection.Select(validStrokes, validElements, raiseSelectionChanged);
        }

#if DEBUG_LASSO_FEEDBACK
        internal ContainerVisual RendererRootContainer
        {
            get {return _inkContainerVisual;}
        }
#endif


        /// <summary>
        /// Private helper method used to retrieve a StrokeCollection which does AND operation on two input collections.
        /// </summary>
        /// <param name="subset">The possible subset</param>
        /// <param name="superset">The container set</param>
        /// <returns>True if subset is a subset of superset, false otherwise</returns>
        internal static StrokeCollection GetValidStrokes(StrokeCollection subset, StrokeCollection superset)
        {
            StrokeCollection validStrokes = new StrokeCollection();

            int subsetCount = subset.Count;

            // special case an empty subset as a guaranteed subset
            if ( subsetCount == 0 )
            {
                return validStrokes;
            }

            for ( int i = 0; i < subsetCount; i++ )
            {
                Stroke stroke = subset[i];
                if ( superset.Contains(stroke) )
                {
                    validStrokes.Add(stroke);
                }
            }

            return validStrokes;
        }

        /// <summary>
        /// Register the commanding handlers for the clipboard operations
        /// </summary>
        private static void _RegisterClipboardHandlers()
        {
            Type ownerType = typeof(InkCanvas);

            CommandHelpers.RegisterCommandHandler(ownerType, ApplicationCommands.Cut,
                new ExecutedRoutedEventHandler(_OnCommandExecuted), new CanExecuteRoutedEventHandler(_OnQueryCommandEnabled),
                KeyGesture.CreateFromResourceStrings(KeyShiftDelete, SRID.KeyShiftDeleteDisplayString));
            CommandHelpers.RegisterCommandHandler(ownerType, ApplicationCommands.Copy,
                new ExecutedRoutedEventHandler(_OnCommandExecuted), new CanExecuteRoutedEventHandler(_OnQueryCommandEnabled),
                KeyGesture.CreateFromResourceStrings(KeyCtrlInsert, SRID.KeyCtrlInsertDisplayString));

            // Use temp variables to reduce code under elevation
            ExecutedRoutedEventHandler pasteExecuteEventHandler = new ExecutedRoutedEventHandler(_OnCommandExecuted);
            CanExecuteRoutedEventHandler pasteQueryEnabledEventHandler = new CanExecuteRoutedEventHandler(_OnQueryCommandEnabled);
            InputGesture pasteInputGesture = KeyGesture.CreateFromResourceStrings(KeyShiftInsert, SR.Get(SRID.KeyShiftInsertDisplayString));

            CommandHelpers.RegisterCommandHandler(ownerType, ApplicationCommands.Paste,
                pasteExecuteEventHandler, pasteQueryEnabledEventHandler, pasteInputGesture);
        }

        /// <summary>
        /// Private helper used to ensure that any stroke collection
        /// passed to the InkCanvas is valid.  Throws exceptions if the argument is invalid
        /// </summary>
        private StrokeCollection ValidateSelectedStrokes(StrokeCollection strokes)
        {
            //
            //  null is a valid input
            //
            if (strokes == null)
            {
                return new StrokeCollection();
            }
            else
            {
                return GetValidStrokes(strokes, this.Strokes);
            }
        }

        /// <summary>
        /// Private helper used to ensure that a UIElement argument passed in
        /// is valid.
        /// </summary>
        private UIElement[] ValidateSelectedElements(IEnumerable<UIElement> selectedElements)
        {
            if (selectedElements == null)
            {
                return new UIElement[]{};
            }

            List<UIElement> elements = new List<UIElement>();
            foreach (UIElement element in selectedElements)
            {
                // 
                // Don't add the duplicated element.
                if ( !elements.Contains(element) )
                {
                    //
                    // check the common case first, e
                    //
                    if ( InkCanvasIsAncestorOf(element) )
                    {
                        elements.Add(element);
                    }
                }
            }

            return elements.ToArray();
        }

        /// <summary>
        /// Helper method used by DesignActivation to see if an element
        /// has this InkCanvas as an Ancestor
        /// </summary>
        private bool InkCanvasIsAncestorOf(UIElement element)
        {
            if (this != element && this.IsAncestorOf(element))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Handler called whenever changes are made to properties of the DefaultDrawingAttributes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <remarks>For example, when a developer changes the Color property on the DefaultDrawingAttributes,
        /// this event will fire - allowing the InkCanvas a chance to notify the BasicRTI Service.
        /// Also - we do not currently call through the DefaultDrawingAttributes setter since
        /// parameter validation in the setter may detect if the reference isn't changing, and ignore
        /// the call. Also - there is no need for extra parameter validation.</remarks>
        private void DefaultDrawingAttributes_Changed(object sender, PropertyDataChangedEventArgs args)
        {
            // note that sender should be the same as _defaultDrawingAttributes
            // If a developer writes code to change the DefaultDrawingAttributes inside of the event
            //      handler before the InkCanvas receives the notification (multi-cast delegate scenario) -
            //      The DefaultDrawingAttributes should still be updated, and in that case we would
            //      update the RTI DAC twice. Typically, however, this will just refresh the
            //      attributes in the RTI thread.
            System.Diagnostics.Debug.Assert(object.ReferenceEquals(sender, DefaultDrawingAttributes));

            InvalidateSubProperty(DefaultDrawingAttributesProperty);

            // Be sure to update the RealTimeInking PlugIn with the drawing attribute changes.
            UpdateDynamicRenderer();

            // Invalidate the inking cursor
            _editingCoordinator.InvalidateBehaviorCursor(_editingCoordinator.InkCollectionBehavior);
        }

        /// <summary>
        /// Helper method used to set up the DynamicRenderer.
        /// </summary>
        internal void UpdateDynamicRenderer()
        {
            UpdateDynamicRenderer(DefaultDrawingAttributes);
        }
        /// <summary>
        /// Helper method used to set up the DynamicRenderer.
        /// </summary>
        private void UpdateDynamicRenderer(DrawingAttributes newDrawingAttributes)
        {
            ApplyTemplate();

            if (this.DynamicRenderer != null)
            {
                this.DynamicRenderer.DrawingAttributes = newDrawingAttributes;

                if (!this.InkPresenter.AttachedVisualIsPositionedCorrectly(this.DynamicRenderer.RootVisual, newDrawingAttributes))
                {
                    if (this.InkPresenter.ContainsAttachedVisual(this.DynamicRenderer.RootVisual))
                    {
                        this.InkPresenter.DetachVisuals(this.DynamicRenderer.RootVisual);
                    }

                    // Only hook up if we are enabled.  As we change editing modes this routine will be called
                    // to clean up things.
                    if (this.DynamicRenderer.Enabled && this.DynamicRenderer.RootVisual != null)
                    {
                        this.InkPresenter.AttachVisuals(this.DynamicRenderer.RootVisual, newDrawingAttributes);
                    }
                }
            }
        }

        private bool EnsureActiveEditingMode(InkCanvasEditingMode newEditingMode)
        {
            bool ret = true;

            if ( ActiveEditingMode != newEditingMode )
            {
                if ( EditingCoordinator.IsStylusInverted )
                {
                    EditingModeInverted = newEditingMode;
                }
                else
                {
                    EditingMode = newEditingMode;
                }

                // Verify whether user has cancelled the change in EditingModeChanging event.
                ret = ( ActiveEditingMode == newEditingMode );
            }

            return ret;
        }

        // The ClipboardProcessor instance which deals with the operations relevant to the clipboard.
        private ClipboardProcessor ClipboardProcessor
        {
            get
            {
                if ( _clipboardProcessor == null )
                {
                    _clipboardProcessor = new ClipboardProcessor(this);
                }

                return _clipboardProcessor;
            }
        }

        //lazy instance the gesture recognizer
        private GestureRecognizer GestureRecognizer
        {
            get
            {
                if (_gestureRecognizer == null)
                {
                    _gestureRecognizer = new GestureRecognizer();
                }
                return _gestureRecognizer;
            }
        }

        /// <summary>
        /// Delete the current selection
        /// </summary>
        private void DeleteCurrentSelection(bool removeSelectedStrokes, bool removeSelectedElements)
        {
            Debug.Assert(removeSelectedStrokes || removeSelectedElements, "At least either Strokes or Elements should be removed!");

            // Now delete the current selection.
            StrokeCollection strokes = GetSelectedStrokes();
            IList<UIElement> elements = GetSelectedElements();

            // Clear the selection first.
            CoreChangeSelection(
                removeSelectedStrokes ? new StrokeCollection() : strokes,
                removeSelectedElements ? new List<UIElement>() : elements,
                true);

            // Remove the ink.
            if ( removeSelectedStrokes && strokes != null && strokes.Count != 0 )
            {
                Strokes.Remove(strokes);
            }

            // Remove the elements.
            if ( removeSelectedElements )
            {
                UIElementCollection children = Children;
                foreach ( UIElement element in elements )
                {
                    children.Remove(element);
                }
            }
        }

        /// <summary>
        /// A class handler of the Commands
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void _OnCommandExecuted(object sender, ExecutedRoutedEventArgs args)
        {
            ICommand command = args.Command;
            InkCanvas inkCanvas = sender as InkCanvas;

            Debug.Assert(inkCanvas != null);

            if ( inkCanvas.IsEnabled && !inkCanvas.EditingCoordinator.UserIsEditing )
            {
                if ( command == ApplicationCommands.Delete )
                {
                    inkCanvas.DeleteCurrentSelection(true, true);
                }
                else if ( command == ApplicationCommands.Cut )
                {
                    inkCanvas.CutSelection();
                }
                else if ( command == ApplicationCommands.Copy )
                {
                    inkCanvas.CopySelection();
                }
                else if ( command == ApplicationCommands.SelectAll )
                {
                    if ( inkCanvas.ActiveEditingMode == InkCanvasEditingMode.Select )
                    {
                        IEnumerable<UIElement> children = null;
                        UIElementCollection uiElementCollection = inkCanvas.Children;
                        if ( uiElementCollection.Count > 0 )
                        {
                            //UIElementCollection doesn't implement IEnumerable<UIElement>
                            //for some reason
                            UIElement[] uiElementArray = new UIElement[uiElementCollection.Count];
                            for ( int i = 0; i < uiElementCollection.Count; i++ )
                            {
                                uiElementArray[i] = uiElementCollection[i];
                            }
                            children = uiElementArray;
                        }
                        inkCanvas.Select(inkCanvas.Strokes, children);
                    }
                }
                else if ( command == ApplicationCommands.Paste )
                {
                    try
                    {
                        inkCanvas.Paste();
                    }
                    // Eat it and do nothing if one of the following exceptions is caught.
                    catch ( System.Runtime.InteropServices.COMException )
                    {
                        // The window may be destroyed which could cause the opening failed..
                    }
                    catch ( XamlParseException )
                    {
                        // The Xaml parser fails
                    }
                    catch ( ArgumentException )
                    {
                        // The ISF decoder fails
                    }
                }
                else if ( command == InkCanvas.DeselectCommand )
                {
                    inkCanvas.ClearSelectionRaiseSelectionChanging();
                }
            }
        }

        /// <summary>
        /// A class handler for querying the enabled status of the commands.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void _OnQueryCommandEnabled(object sender, CanExecuteRoutedEventArgs args)
        {
            RoutedCommand command = (RoutedCommand)(args.Command);
            InkCanvas inkCanvas = sender as InkCanvas;

            Debug.Assert(inkCanvas != null);

            if ( inkCanvas.IsEnabled
                // 
                // If user is editing, we should disable all commands.
                && !inkCanvas.EditingCoordinator.UserIsEditing )
            {
                if ( command == ApplicationCommands.Delete
                    || command == ApplicationCommands.Cut
                    || command == ApplicationCommands.Copy
                    || command == InkCanvas.DeselectCommand )
                {
                    args.CanExecute = inkCanvas.InkCanvasSelection.HasSelection;
                }
                else if ( command == ApplicationCommands.Paste )
                {
                    try
                    {
                        args.CanExecute = args.UserInitiated
                                            ? inkCanvas.UserInitiatedCanPaste() /* Call UserInitiatedCanPaste when the query is initiated by user */
                                            : inkCanvas.CanPaste() /* Call the public CanPaste if not */;
                    }
                    catch ( System.Runtime.InteropServices.COMException )
                    {
                        // The window may be destroyed which could cause the opening failed..
                        // Eat the exception and do nothing.
                        args.CanExecute = false;
                    }
                }
                else if ( command == ApplicationCommands.SelectAll )
                {
                    //anything to select?
                    args.CanExecute = ( inkCanvas.ActiveEditingMode == InkCanvasEditingMode.Select 
                                            && (inkCanvas.Strokes.Count > 0 || inkCanvas.Children.Count > 0));
                }
            }
            else
            {
                // 
                // Return false for CanExecute if InkCanvas is disabled.
                args.CanExecute = false;
            }

            // 
            // Mark Handled as true so that the clipboard commands stops routing to InkCanvas' ancestors.
            if ( command == ApplicationCommands.Cut || command == ApplicationCommands.Copy
                || command == ApplicationCommands.Paste )
            {
                args.Handled = true;
            }
        }

        private InkCanvasClipboardDataFormats PrivateCopySelection()
        {
            InkCanvasClipboardDataFormats copiedDataFormats = InkCanvasClipboardDataFormats.None;

            // Don't even bother if we don't have a selection or UserIsEditing has been set.
            if ( InkCanvasSelection.HasSelection && !_editingCoordinator.UserIsEditing)
            {
                copiedDataFormats = CopyToDataObject();
            }

            return copiedDataFormats;
        }


        /// <summary>
        /// _OnDeviceDown
        /// </summary>
        /// <typeparam name="TEventArgs"></typeparam>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void _OnDeviceDown<TEventArgs>(object sender, TEventArgs e)
            where TEventArgs : InputEventArgs
        {
            ( (InkCanvas)sender ).EditingCoordinator.OnInkCanvasDeviceDown(sender, e);
        }

        /// <summary>
        /// _OnDeviceUp
        /// </summary>
        /// <typeparam name="TEventArgs"></typeparam>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void _OnDeviceUp<TEventArgs>(object sender, TEventArgs e)
            where TEventArgs : InputEventArgs
        {
            ((InkCanvas)sender).EditingCoordinator.OnInkCanvasDeviceUp(sender, e);
        }

        /// <summary>
        /// _OnQueryCursor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void _OnQueryCursor(object sender, QueryCursorEventArgs e)
        {
            InkCanvas inkCanvas = (InkCanvas)sender;

            if ( inkCanvas.UseCustomCursor )
            {
                // If UseCustomCursor is set, we bail out. Let the base class (FrameworkElement) to do the rest.
                return;
            }

            // We should behave like our base - honor ForceCursor property.
            if ( !e.Handled || inkCanvas.ForceCursor )
            {
                Cursor cursor = inkCanvas.EditingCoordinator.GetActiveBehaviorCursor();
                
                // If cursor is null, we don't handle the event and leave it as whatever the default is.
                if ( cursor != null )
                {
                    e.Cursor = cursor;
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Update the current cursor if mouse is over InkCanvas. Called by
        ///     EditingCoordinator.InvalidateBehaviorCursor
        ///     EditingCoordinator.UpdateEditingState
        ///     InkCanvas.set_UseCustomCursor
        /// </summary>
        internal void UpdateCursor()
        {
            if ( IsMouseOver )
            {
                Mouse.UpdateCursor();
            }
        }

        #endregion Private Properties / Methods

        //------------------------------------------------------
        //
        //  Private Classes
        //
        //------------------------------------------------------

        #region Private Classes


        /// <summary>
        /// A helper class for RTI high contrast support
        /// </summary>
        private class RTIHighContrastCallback : HighContrastCallback
        {
            //------------------------------------------------------
            //
            //  Cnostructors
            //
            //------------------------------------------------------

            #region Constructors

            internal RTIHighContrastCallback(InkCanvas inkCanvas)
            {
                _thisInkCanvas = inkCanvas;
            }

            private RTIHighContrastCallback() { }

            #endregion Constructors

            //------------------------------------------------------
            //
            //  Internal Methods
            //
            //------------------------------------------------------

            #region Internal Methods

            /// <summary>
            /// TurnHighContrastOn
            /// </summary>
            /// <param name="highContrastColor"></param>
            internal override void TurnHighContrastOn(Color highContrastColor)
            {
                // The static strokes already have been taken care of by InkPresenter.
                // We only update the RTI renderer here.
                DrawingAttributes highContrastDa = _thisInkCanvas.DefaultDrawingAttributes.Clone();
                highContrastDa.Color = highContrastColor;
                _thisInkCanvas.UpdateDynamicRenderer(highContrastDa);
            }

            /// <summary>
            /// TurnHighContrastOff
            /// </summary>
            internal override void TurnHighContrastOff()
            {
                // The static strokes already have been taken care of by InkPresenter.
                // We only update the RTI renderer here.
                _thisInkCanvas.UpdateDynamicRenderer(_thisInkCanvas.DefaultDrawingAttributes);
            }

            #endregion Internal Methods

            //------------------------------------------------------
            //
            //  Internal Properties
            //
            //------------------------------------------------------

            #region Internal Properties

            /// <summary>
            /// Returns the dispatcher if the object is associated to a UIContext.
            /// </summary>
            internal override Dispatcher Dispatcher
            {
                get
                {
                    return _thisInkCanvas.Dispatcher;
                }
            }

            #endregion Internal Properties

            //------------------------------------------------------
            //
            //  Private Fields
            //
            //------------------------------------------------------

            #region Private Fields

            private InkCanvas _thisInkCanvas;

            #endregion Private Fields
        }

        /// <summary>
        /// This is a binding converter which translates the InkCanvas.ActiveEditingMode to UIElement.Visibility.
        /// </summary>
        private class ActiveEditingMode2VisibilityConverter : IValueConverter
        {
            public object Convert(object o, Type type, object parameter, System.Globalization.CultureInfo culture)
            {
                InkCanvasEditingMode activeMode = (InkCanvasEditingMode)o;

                // If the current EditingMode is the mode which menuitem is expecting, return true for IsChecked.
                if ( activeMode != InkCanvasEditingMode.None )
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }

            public object ConvertBack(object o, Type type, object parameter, System.Globalization.CultureInfo culture)
            {
                // Non-reversed convertion
                return null;
            }
        }

        #endregion Private Classes

        #region Private Members

        /// <summary>
        /// The element that represents the selected ink strokes, if any exist.  Will frequently be null.
        /// </summary>
        private InkCanvasSelection          _selection = null;
        private InkCanvasSelectionAdorner   _selectionAdorner = null;
        private InkCanvasFeedbackAdorner    _feedbackAdorner = null;

        /// <summary>
        /// The internal Canvas used to hold elements
        /// </summary>
        private InkCanvasInnerCanvas        _innerCanvas = null;

        /// <summary>
        /// The internal private AdornerDecorator
        /// </summary>
        private AdornerDecorator            _localAdornerDecorator = null;

        /// <summary>
        /// Runtime Selection StrokeCollection
        /// </summary>
        private StrokeCollection            _dynamicallySelectedStrokes;

        /// <summary>
        /// Our editing logic
        /// </summary>
        private EditingCoordinator          _editingCoordinator;

        /// <summary>
        /// Defines the default StylusPointDescription
        /// </summary>
        private StylusPointDescription     _defaultStylusPointDescription;


        /// <summary>
        /// Defines the shape of the eraser tip
        /// </summary>
        private StylusShape                 _eraserShape;

        /// <summary>
        /// Determines if EditingBehaviors should use their own cursor or a custom one specified.
        /// </summary>
        private bool                        _useCustomCursor = false;


        //
        // Rendering support.
        //
        private InkPresenter                _inkPresenter;

        //
        // The RealTimeInking PlugIn that handles our off UIContext rendering.
        //
        private DynamicRenderer             _dynamicRenderer;

        //
        // Clipboard Helper
        //
        private ClipboardProcessor          _clipboardProcessor;

        //
        // Gesture support
        //
        private GestureRecognizer           _gestureRecognizer;

        //
        // HighContrast support
        //
        private RTIHighContrastCallback     _rtiHighContrastCallback;

        private const double                    c_pasteDefaultLocation = 0.0;

        private const string InkCanvasDeselectKey   = "Esc";
        private const string KeyCtrlInsert = "Ctrl+Insert";
        private const string KeyShiftInsert = "Shift+Insert";
        private const string KeyShiftDelete = "Shift+Delete";

        #endregion Private Members
    }
}


