// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Automation.Peers;

using MS.Internal;
using MS.Internal.KnownBoxes;
using MS.Internal.Telemetry.PresentationFramework;

namespace System.Windows.Controls
{
    /// <summary>
    /// Enum to indicate whether GridSplitter resizes Columns or Rows
    /// </summary>
    public enum GridResizeDirection
    {
        /// <summary>
        /// Determines whether to resize rows or columns based on its Alignment and 
        /// width compared to height
        /// </summary>
        Auto,
 
        /// <summary>
        /// Resize columns when dragging Splitter.
        /// </summary>
        Columns,

        /// <summary>
        /// Resize rows when dragging Splitter.
        /// </summary>
        Rows,

        // NOTE: if you add or remove any values in this enum, be sure to update GridSplitter.IsValidResizeDirection()
    }


    /// <summary>
    /// Enum to indicate what Columns or Rows the GridSplitter resizes
    /// </summary>
    public enum GridResizeBehavior
    {
        /// <summary>
        /// Determine which columns or rows to resize based on its Alignment.
        /// </summary>
        BasedOnAlignment,

        /// <summary>
        /// Resize the current and next Columns or Rows.
        /// </summary>
        CurrentAndNext,

        /// <summary>
        /// Resize the previous and current Columns or Rows.
        /// </summary>
        PreviousAndCurrent,

        /// <summary>
        /// Resize the previous and next Columns or Rows.
        /// </summary>
        PreviousAndNext,

        // NOTE: if you add or remove any values in this enum, be sure to update GridSplitter.IsValidResizeBehavior()
    }



    /// <summary>
    /// GridSplitter is used to redistribute space between two adjacent columns or rows.
    /// This control, when used in conjunction with Grid, can be used to create flexible 
    /// and complex user interfaces
    /// </summary>
    [StyleTypedProperty(Property = "PreviewStyle", StyleTargetType = typeof(Control))]
    public class GridSplitter : Thumb
    {
        #region Constructors

        static GridSplitter()
        {
            EventManager.RegisterClassHandler(typeof(GridSplitter), Thumb.DragStartedEvent, new DragStartedEventHandler(GridSplitter.OnDragStarted));
            EventManager.RegisterClassHandler(typeof(GridSplitter), Thumb.DragDeltaEvent, new DragDeltaEventHandler(GridSplitter.OnDragDelta));
            EventManager.RegisterClassHandler(typeof(GridSplitter), Thumb.DragCompletedEvent, new DragCompletedEventHandler(GridSplitter.OnDragCompleted));

            DefaultStyleKeyProperty.OverrideMetadata(typeof(GridSplitter), new FrameworkPropertyMetadata(typeof(GridSplitter)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(GridSplitter));

            FocusableProperty.OverrideMetadata(typeof(GridSplitter), new FrameworkPropertyMetadata(MS.Internal.KnownBoxes.BooleanBoxes.TrueBox));
            FrameworkElement.HorizontalAlignmentProperty.OverrideMetadata(typeof(GridSplitter), new FrameworkPropertyMetadata(HorizontalAlignment.Right));
            
            // Cursor depends on ResizeDirection, ActualWidth, and ActualHeight 
            CursorProperty.OverrideMetadata(typeof(GridSplitter), new FrameworkPropertyMetadata(null, new CoerceValueCallback(CoerceCursor)));

            ControlsTraceLogger.AddControl(TelemetryControls.GridSplitter);
        }

        /// <summary>
        /// Instantiates a new instance of a GridSplitter.
        /// </summary>
        public GridSplitter()
        {
        }

        #endregion

        #region Properties

        private static void UpdateCursor(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            o.CoerceValue(CursorProperty);
        }

        private static object CoerceCursor(DependencyObject o, object value)
        {
            GridSplitter splitter = (GridSplitter)o;

            bool hasModifiers;
            BaseValueSourceInternal vs = splitter.GetValueSource(CursorProperty, null, out hasModifiers);
            if (value == null && vs == BaseValueSourceInternal.Default)
            {
                switch (splitter.GetEffectiveResizeDirection())
                {
                    case GridResizeDirection.Columns:
                        return Cursors.SizeWE;
                    case GridResizeDirection.Rows:
                        return Cursors.SizeNS;
                }
            }
            return value;
        }

        /// <summary>
        ///     The DependencyProperty for the ResizeDirection property.
        ///     Default Value:      GridResizeDirection.Auto
        /// </summary>
        public static readonly DependencyProperty ResizeDirectionProperty
            = DependencyProperty.Register("ResizeDirection",
                                            typeof(GridResizeDirection),
                                            typeof(GridSplitter),
                                            new FrameworkPropertyMetadata(GridResizeDirection.Auto,
                                                                          new PropertyChangedCallback(UpdateCursor)),
                                            new ValidateValueCallback(IsValidResizeDirection));

        /// <summary>
        /// Indicates whether the Splitter resizes the Columns, Rows, or Both.
        /// </summary>
        public GridResizeDirection ResizeDirection
        {
            get { return (GridResizeDirection)GetValue(ResizeDirectionProperty); }

            set { SetValue(ResizeDirectionProperty, value); }
        }

        private static bool IsValidResizeDirection(object o)
        {
            GridResizeDirection resizeDirection = (GridResizeDirection)o;
            return resizeDirection == GridResizeDirection.Auto ||
                   resizeDirection == GridResizeDirection.Columns ||
                   resizeDirection == GridResizeDirection.Rows;
        }

        /// <summary>
        ///     The DependencyProperty for the ResizeBehavior property.
        ///     Default Value:      GridResizeBehavior.BasedOnAlignment
        /// </summary>
        public static readonly DependencyProperty ResizeBehaviorProperty
            = DependencyProperty.Register("ResizeBehavior",
                                            typeof(GridResizeBehavior),
                                            typeof(GridSplitter),
                                            new FrameworkPropertyMetadata(GridResizeBehavior.BasedOnAlignment),
                                            new ValidateValueCallback(IsValidResizeBehavior));

        /// <summary>
        /// Indicates which Columns or Rows the Splitter resizes.
        /// </summary>
        public GridResizeBehavior ResizeBehavior
        {
            get { return (GridResizeBehavior)GetValue(ResizeBehaviorProperty); }

            set { SetValue(ResizeBehaviorProperty, value); }
        }

        private static bool IsValidResizeBehavior(object o)
        {
            GridResizeBehavior resizeBehavior = (GridResizeBehavior)o;
            return resizeBehavior == GridResizeBehavior.BasedOnAlignment ||
                   resizeBehavior == GridResizeBehavior.CurrentAndNext ||
                   resizeBehavior == GridResizeBehavior.PreviousAndCurrent ||
                   resizeBehavior == GridResizeBehavior.PreviousAndNext;
        }


        /// <summary>
        ///     The DependencyProperty for the ShowsPreview property.
        ///     Default Value:      false
        /// </summary>
        public static readonly DependencyProperty ShowsPreviewProperty
            = DependencyProperty.Register("ShowsPreview",
                                          typeof(bool),
                                          typeof(GridSplitter),
                                          new FrameworkPropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// Indicates whether to Preview the column resizing without updating layout.
        /// </summary>
        public bool ShowsPreview
        {
            get { return (bool)GetValue(ShowsPreviewProperty); }
            set { SetValue(ShowsPreviewProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>
        ///     The DependencyProperty for the PreviewStyle property.
        ///     Default Value:      null
        /// </summary>
        public static readonly DependencyProperty PreviewStyleProperty =
                    DependencyProperty.Register(
                                "PreviewStyle",
                                typeof(Style),
                                typeof(GridSplitter),
                                new FrameworkPropertyMetadata((Style)null));

        /// <summary>
        ///     The Style used to render the Preview.
        /// </summary>
        public Style PreviewStyle
        {
            get { return (Style)GetValue(PreviewStyleProperty); }
            set { SetValue(PreviewStyleProperty, value); }
        }


        /// <summary>
        ///     The DependencyProperty for the KeyboardIncrement property.
        ///     Default Value:      10.0
        /// </summary>
        public static readonly DependencyProperty KeyboardIncrementProperty =
                    DependencyProperty.Register(
                                "KeyboardIncrement",
                                typeof(double),
                                typeof(GridSplitter),
                                new FrameworkPropertyMetadata(10.0),
                                new ValidateValueCallback(IsValidDelta));

        /// <summary>
        ///     The Distance to move the splitter when pressing the Keyboard arrow keys
        /// </summary>
        public double KeyboardIncrement
        {
            get { return (double)GetValue(KeyboardIncrementProperty); }
            set { SetValue(KeyboardIncrementProperty, value); }
        }


        private static bool IsValidDelta(object o)
        {
            double delta = (double)o;
            return delta > 0.0 && !Double.IsPositiveInfinity(delta);
        }


        /// <summary>
        ///     The DependencyProperty for the DragIncrement property.
        ///     Default Value:      1.0
        /// </summary>
        public static readonly DependencyProperty DragIncrementProperty =
                    DependencyProperty.Register(
                                "DragIncrement",
                                typeof(double),
                                typeof(GridSplitter),
                                new FrameworkPropertyMetadata(1.0),
                                new ValidateValueCallback(IsValidDelta));

        /// <summary>
        ///     Restricts splitter to move a multiple of the specified units.
        /// </summary>
        public double DragIncrement
        {
            get { return (double)GetValue(DragIncrementProperty); }
            set { SetValue(DragIncrementProperty, value); }
        }

        #endregion


        #region Method Overrides

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new GridSplitterAutomationPeer(this);
        }

        // Converts BasedOnAlignment direction to Rows, Columns, or Both depending on its width/height
        private GridResizeDirection GetEffectiveResizeDirection()
        {
            GridResizeDirection direction = ResizeDirection;

            if (direction == GridResizeDirection.Auto)
            {
                // When HorizontalAlignment is Left, Right or Center, resize Columns
                if (HorizontalAlignment != HorizontalAlignment.Stretch)
                {
                   direction = GridResizeDirection.Columns;
                }
                else if (VerticalAlignment != VerticalAlignment.Stretch)
                {
                   direction = GridResizeDirection.Rows;
                }
                else if (ActualWidth <= ActualHeight)// Fall back to Width vs Height
                {
                   direction = GridResizeDirection.Columns;
                }
                else
                {
                   direction = GridResizeDirection.Rows;
                }
            }
            return direction;
        }

        // Convert BasedOnAlignment to Next/Prev/Both depending on alignment and Direction
        private GridResizeBehavior GetEffectiveResizeBehavior(GridResizeDirection direction)
        {
            GridResizeBehavior resizeBehavior = ResizeBehavior;

            if (resizeBehavior == GridResizeBehavior.BasedOnAlignment)
            {
                if (direction == GridResizeDirection.Columns)
                {
                   switch (HorizontalAlignment)
                   {
                       case HorizontalAlignment.Left:
                           resizeBehavior = GridResizeBehavior.PreviousAndCurrent;
                           break;
                       case HorizontalAlignment.Right:
                           resizeBehavior = GridResizeBehavior.CurrentAndNext;
                           break;
                       default:
                           resizeBehavior = GridResizeBehavior.PreviousAndNext;
                           break;
                   }
                }
                else
                {
                   switch (VerticalAlignment)
                   {
                       case VerticalAlignment.Top:
                           resizeBehavior = GridResizeBehavior.PreviousAndCurrent;
                           break;
                       case VerticalAlignment.Bottom:
                           resizeBehavior = GridResizeBehavior.CurrentAndNext;
                           break;
                       default:
                           resizeBehavior = GridResizeBehavior.PreviousAndNext;
                           break;
                   }
                }
            }
            return resizeBehavior;
        }

        /// <summary>
        /// Override for <seealso cref="UIElement.OnRenderSizeChanged"/>
        /// </summary>
        protected internal override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            CoerceValue(CursorProperty);
        }

        #endregion

        #region PreviewAdorner

        // This adorner draws the preview for the GridSplitter
        // It also positions the adorner
        // Note:- This class is sealed because it calls OnVisualChildrenChanged virtual in the 
        //              constructor and it does not override it, but derived classes could.        
        private sealed class PreviewAdorner : Adorner
        {
            public PreviewAdorner(GridSplitter gridSplitter, Style previewStyle)
                : base(gridSplitter)
            {
                // Create a preview control to overlay on top of the GridSplitter
                Control previewControl = new Control();
                previewControl.Style = previewStyle;
                previewControl.IsEnabled = false;

                // Add a decorator to perform translations
                Translation = new TranslateTransform();                
                _decorator = new Decorator();
                _decorator.Child = previewControl;
                _decorator.RenderTransform = Translation;
 
                this.AddVisualChild(_decorator);  
            }

            /// <summary>
            ///   Derived class must implement to support Visual children. The method must return
            ///    the child at the specified index. Index must be between 0 and GetVisualChildrenCount-1.
            ///
            ///    By default a Visual does not have any children.
            ///
            ///  Remark: 
            ///       During this virtual call it is not valid to modify the Visual tree. 
            /// </summary>
            protected override Visual GetVisualChild(int index)
            {
                // it is initialized in the constructor
                Debug.Assert(_decorator != null);
                if(index != 0)
                {
                    throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
                }

                return _decorator;
            }
            
            /// <summary>
            ///  Derived classes override this property to enable the Visual code to enumerate 
            ///  the Visual children. Derived classes need to return the number of children
            ///  from this method.
            ///
            ///    By default a Visual does not have any children.
            ///
            ///  Remark: During this virtual method the Visual tree must not be modified.
            /// </summary>        
            protected override int VisualChildrenCount
            {        
                get 
                {
                    // it is initialized in the constructor
                    Debug.Assert(_decorator != null);
                    return 1; 
                }
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                _decorator.Arrange(new Rect(new Point(), finalSize));
                return finalSize;
            }

            // The Preview's Offset in the X direction from the GridSplitter
            public double OffsetX
            {
                get { return Translation.X; }
                set { Translation.X = value; }
            }

            // The Preview's Offset in the Y direction from the GridSplitter
            public double OffsetY
            {
                get { return Translation.Y; }
                set { Translation.Y = value; }
            }

            private TranslateTransform Translation;
            private Decorator _decorator;
        }

        // Removes the Preview Adorner
        private void RemovePreviewAdorner()
        {
            // Remove the preview grid from the adorner
            if (_resizeData.Adorner != null)
            {
                AdornerLayer layer = VisualTreeHelper.GetParent(_resizeData.Adorner) as AdornerLayer;
                layer.Remove(_resizeData.Adorner);
            }
        }

        #endregion

        #region Splitter Setup

        // Initialize the data needed for resizing
        private void InitializeData(bool ShowsPreview)
        {
            Grid grid = Parent as Grid;

            // If not in a grid or can't resize, do nothing
            if (grid != null)
            {
                // Setup data used for resizing
                _resizeData = new ResizeData();
                _resizeData.Grid = grid;
                _resizeData.ShowsPreview = ShowsPreview;
                _resizeData.ResizeDirection = GetEffectiveResizeDirection();
                _resizeData.ResizeBehavior = GetEffectiveResizeBehavior(_resizeData.ResizeDirection);
                _resizeData.SplitterLength = Math.Min(ActualWidth, ActualHeight);

                // Store the rows and columns to resize on drag events
                if (!SetupDefinitionsToResize())
                {
                    // Unable to resize, clear data
                    _resizeData = null;
                    return;
                }

                // Setup the preview in the adorner if ShowsPreview is true
                SetupPreview();
            }
        }

        // Returns true if GridSplitter can resize rows/columns
        private bool SetupDefinitionsToResize()
        {
            int splitterIndex, index1, index2;

            int gridSpan = (int)GetValue(_resizeData.ResizeDirection == GridResizeDirection.Columns ? Grid.ColumnSpanProperty : Grid.RowSpanProperty);

            if (gridSpan == 1)
            {
                splitterIndex = (int)GetValue(_resizeData.ResizeDirection == GridResizeDirection.Columns ? Grid.ColumnProperty : Grid.RowProperty);
                
                // Select the columns based on Behavior
                switch (_resizeData.ResizeBehavior)
                {
                    case GridResizeBehavior.PreviousAndCurrent:
                        // get current and previous
                        index1 = splitterIndex - 1;
                        index2 = splitterIndex;
                        break;
                    case GridResizeBehavior.CurrentAndNext:
                        // get current and next
                        index1 = splitterIndex;
                        index2 = splitterIndex + 1;
                        break;
                    default: // GridResizeBehavior.PreviousAndNext
                        // get previous and next
                        index1 = splitterIndex - 1;
                        index2 = splitterIndex + 1;
                        break;
                }

                // Get # of rows/columns in the resize direction
                int count = (_resizeData.ResizeDirection == GridResizeDirection.Columns) ? _resizeData.Grid.ColumnDefinitions.Count : _resizeData.Grid.RowDefinitions.Count;

                if (index1 >= 0 && index2 < count)
                {
                    _resizeData.SplitterIndex = splitterIndex;

                    _resizeData.Definition1Index = index1;
                    _resizeData.Definition1 = GetGridDefinition(_resizeData.Grid, index1, _resizeData.ResizeDirection);
                    _resizeData.OriginalDefinition1Length = _resizeData.Definition1.UserSizeValueCache;  //save Size if user cancels
                    _resizeData.OriginalDefinition1ActualLength = GetActualLength(_resizeData.Definition1);

                    _resizeData.Definition2Index = index2;
                    _resizeData.Definition2 = GetGridDefinition(_resizeData.Grid, index2, _resizeData.ResizeDirection);
                    _resizeData.OriginalDefinition2Length = _resizeData.Definition2.UserSizeValueCache;  //save Size if user cancels
                    _resizeData.OriginalDefinition2ActualLength = GetActualLength(_resizeData.Definition2);

                    // Determine how to resize the columns 
                    bool isStar1 = IsStar(_resizeData.Definition1);
                    bool isStar2 = IsStar(_resizeData.Definition2);
                    if (isStar1 && isStar2)
                    {
                        // If they are both stars, resize both
                        _resizeData.SplitBehavior = SplitBehavior.Split;  
                    }
                    else
                    {   
                        // One column is fixed width, resize the first one that is fixed
                        _resizeData.SplitBehavior = !isStar1 ? SplitBehavior.Resize1 : SplitBehavior.Resize2;
                    }

                    return true;
                }
            }
            return false;
        }

        // Create the Preview adorner and add it to the adorner layer
        private void SetupPreview()
        {
            if (_resizeData.ShowsPreview)
            {
                // Get the adorner layer and add an adorner to it
                AdornerLayer adornerlayer = AdornerLayer.GetAdornerLayer(_resizeData.Grid);

                // Can't display preview
                if (adornerlayer == null)
                {
                    return;
                }

                _resizeData.Adorner = new PreviewAdorner(this, PreviewStyle);
                adornerlayer.Add(_resizeData.Adorner);

                // Get constraints on preview's translation
                GetDeltaConstraints(out _resizeData.MinChange, out _resizeData.MaxChange);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        ///     An event announcing that the splitter is no longer focused
        /// </summary>
        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);

            if (_resizeData != null)
            {
                CancelResize();
            }
        }

        private static void OnDragStarted(object sender, DragStartedEventArgs e)
        {
            GridSplitter splitter = sender as GridSplitter;
            splitter.OnDragStarted(e);
        }

        // Thumb Mouse Down
        private void OnDragStarted(DragStartedEventArgs e)
        {
            Debug.Assert(_resizeData == null, "_resizeData is not null, DragCompleted was not called");

            InitializeData(ShowsPreview);
        }

        private static void OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            GridSplitter splitter = sender as GridSplitter;
            splitter.OnDragDelta(e);
        }

        // Thumb dragged
        private void OnDragDelta(DragDeltaEventArgs e)
        {
            if (_resizeData != null)
            {
                double horizontalChange = e.HorizontalChange;
                double verticalChange = e.VerticalChange;

                // Round change to nearest multiple of DragIncrement
                double dragIncrement = DragIncrement;
                horizontalChange = Math.Round(horizontalChange / dragIncrement) * dragIncrement;
                verticalChange = Math.Round(verticalChange / dragIncrement) * dragIncrement;

                if (_resizeData.ShowsPreview)
                {
                    //Set the Translation of the Adorner to the distance from the thumb
                    if (_resizeData.ResizeDirection == GridResizeDirection.Columns)
                    {
                        _resizeData.Adorner.OffsetX = Math.Min(Math.Max(horizontalChange, _resizeData.MinChange), _resizeData.MaxChange);
                    }
                    else
                    {
                        _resizeData.Adorner.OffsetY = Math.Min(Math.Max(verticalChange, _resizeData.MinChange), _resizeData.MaxChange);
                    }
                }
                else
                {
                    // Directly update the grid
                    MoveSplitter(horizontalChange, verticalChange);
                }
            }
        }

        private static void OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            GridSplitter splitter = sender as GridSplitter;
            splitter.OnDragCompleted(e);
        }

        // Thumb dragging finished
        private void OnDragCompleted(DragCompletedEventArgs e)
        {
            if (_resizeData != null)
            {
                if (_resizeData.ShowsPreview)
                {
                    // Update the grid
                    MoveSplitter(_resizeData.Adorner.OffsetX, _resizeData.Adorner.OffsetY);
                    RemovePreviewAdorner();
                }

                _resizeData = null;
            }
        }

        /// <summary>
        ///     This is the method that responds to the KeyDown event.
        /// </summary>
        /// <param name="e">Event Arguments</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            Key key = e.Key;
            switch (key)
            {
                case Key.Escape:
                    if (_resizeData != null)
                    {
                        CancelResize();
                        e.Handled = true;
                    }
                    break;

                case Key.Left:
                    e.Handled = KeyboardMoveSplitter(-KeyboardIncrement, 0);
                    break;
                case Key.Right:
                    e.Handled = KeyboardMoveSplitter(KeyboardIncrement, 0);
                    break;
                case Key.Up:
                    e.Handled = KeyboardMoveSplitter(0, -KeyboardIncrement);
                    break;
                case Key.Down:
                    e.Handled = KeyboardMoveSplitter(0, KeyboardIncrement);
                    break;
            }
        }

        // Cancels the Resize when the user hits Escape
        private void CancelResize()
        {
            // Restore original column/row lengths
            Grid grid = Parent as Grid;

            if (_resizeData.ShowsPreview)
            {
                RemovePreviewAdorner();
            }
            else // Reset the columns'/rows' lengths to the saved values 
            {
                SetDefinitionLength(_resizeData.Definition1, _resizeData.OriginalDefinition1Length);
                SetDefinitionLength(_resizeData.Definition2, _resizeData.OriginalDefinition2Length);
            }

            _resizeData = null;
        }

        #endregion

        #region Helper Methods

        #region Row/Column Abstractions 
        // These methods are to help abstract dealing with rows and columns.  
        // DefinitionBase already has internal helpers for getting Width/Height, MinWidth/MinHeight, and MaxWidth/MaxHeight

        // Returns true if the row/column has a Star length
        private static bool IsStar(DefinitionBase definition)
        {
            return definition.UserSizeValueCache.IsStar;
        }

        // Gets Column or Row definition at index from grid based on resize direction
        private static DefinitionBase GetGridDefinition(Grid grid, int index, GridResizeDirection direction)
        {
            return direction == GridResizeDirection.Columns ? (DefinitionBase)grid.ColumnDefinitions[index] : (DefinitionBase)grid.RowDefinitions[index];
        }

        // Retrieves the ActualWidth or ActualHeight of the definition depending on its type Column or Row
        private double GetActualLength(DefinitionBase definition)
        {
            ColumnDefinition column = definition as ColumnDefinition;

            return column == null ? ((RowDefinition)definition).ActualHeight : column.ActualWidth;
        }

        // Gets Column or Row definition at index from grid based on resize direction
        private static void SetDefinitionLength(DefinitionBase definition, GridLength length)
        {
            definition.SetValue(definition is ColumnDefinition ? ColumnDefinition.WidthProperty : RowDefinition.HeightProperty, length);
        }

        #endregion

        // Get the minimum and maximum Delta can be given definition constraints (MinWidth/MaxWidth)
        private void GetDeltaConstraints(out double minDelta, out double maxDelta)
        {
            double definition1Len = GetActualLength(_resizeData.Definition1);
            double definition1Min = _resizeData.Definition1.UserMinSizeValueCache;
            double definition1Max = _resizeData.Definition1.UserMaxSizeValueCache;

            double definition2Len = GetActualLength(_resizeData.Definition2);
            double definition2Min = _resizeData.Definition2.UserMinSizeValueCache;
            double definition2Max = _resizeData.Definition2.UserMaxSizeValueCache;

            //Set MinWidths to be greater than width of splitter
            if (_resizeData.SplitterIndex == _resizeData.Definition1Index)
            {
                definition1Min = Math.Max(definition1Min, _resizeData.SplitterLength);
            }
            else if (_resizeData.SplitterIndex == _resizeData.Definition2Index)
            {
                definition2Min = Math.Max(definition2Min, _resizeData.SplitterLength);
            }

            if (_resizeData.SplitBehavior == SplitBehavior.Split)
            {
                // Determine the minimum and maximum the columns can be resized
                minDelta = -Math.Min(definition1Len - definition1Min, definition2Max - definition2Len);
                maxDelta = Math.Min(definition1Max - definition1Len, definition2Len - definition2Min);
            }
            else if (_resizeData.SplitBehavior == SplitBehavior.Resize1)
            {
                minDelta = definition1Min - definition1Len;
                maxDelta = definition1Max - definition1Len;
            }
            else
            {
                minDelta = definition2Len - definition2Max;
                maxDelta = definition2Len - definition2Min;
            }
        }

        //Sets the length of definition1 and definition2 
        private void SetLengths(double definition1Pixels, double definition2Pixels)
        {
            // For the case where both definition1 and 2 are stars, update all star values to match their current pixel values
            if (_resizeData.SplitBehavior == SplitBehavior.Split)
            {
                IEnumerable definitions = _resizeData.ResizeDirection == GridResizeDirection.Columns ? (IEnumerable)_resizeData.Grid.ColumnDefinitions : (IEnumerable)_resizeData.Grid.RowDefinitions;

                int i = 0;
                foreach (DefinitionBase definition in definitions)
                {
                    // For each definition, if it is a star, set is value to ActualLength in stars
                    // This makes 1 star == 1 pixel in length
                    if (i == _resizeData.Definition1Index)
                    {
                        SetDefinitionLength(definition, new GridLength(definition1Pixels, GridUnitType.Star));
                    }
                    else if (i == _resizeData.Definition2Index)
                    {
                        SetDefinitionLength(definition, new GridLength(definition2Pixels, GridUnitType.Star ));
                    }
                    else if (IsStar(definition))
                    {
                        SetDefinitionLength(definition, new GridLength(GetActualLength(definition), GridUnitType.Star));
                    }
               
                    i++;
                }
            }
            else if (_resizeData.SplitBehavior == SplitBehavior.Resize1)
            {
                SetDefinitionLength(_resizeData.Definition1, new GridLength(definition1Pixels));
            }
            else
            {
                SetDefinitionLength(_resizeData.Definition2, new GridLength(definition2Pixels));
            }
        }

        // Move the splitter by the given Delta's in the horizontal and vertical directions
        private void MoveSplitter(double horizontalChange, double verticalChange)
        {
            Debug.Assert(_resizeData != null, "_resizeData should not be null when calling MoveSplitter");

            double delta;
            DpiScale dpi = GetDpi();

            // Calculate the offset to adjust the splitter.  If layout rounding is enabled, we
            // need to round to an integer physical pixel value to avoid round-ups of children that
            // expand the bounds of the Grid.  In practice this only happens in high dpi because
            // horizontal/vertical offsets here are never fractional (they correspond to mouse movement
            // across logical pixels).  Rounding error only creeps in when converting to a physical
            // display with something other than the logical 96 dpi.
            if (_resizeData.ResizeDirection == GridResizeDirection.Columns)
            {
                delta = horizontalChange;
                if (this.UseLayoutRounding)
                {
                    delta = UIElement.RoundLayoutValue(delta, dpi.DpiScaleX);
                }
            }
            else
            {
                delta = verticalChange;
                if (this.UseLayoutRounding)
                {
                    delta = UIElement.RoundLayoutValue(delta, dpi.DpiScaleY);
                }
            }

            DefinitionBase definition1 = _resizeData.Definition1;
            DefinitionBase definition2 = _resizeData.Definition2;
            if (definition1 != null && definition2 != null)
            {
                double actualLength1 = GetActualLength(definition1);
                double actualLength2 = GetActualLength(definition2);

                // When splitting, Check to see if the total pixels spanned by the definitions 
                // is the same asbefore starting resize. If not cancel the drag
                if (_resizeData.SplitBehavior == SplitBehavior.Split && 
                    !LayoutDoubleUtil.AreClose(actualLength1 + actualLength2, _resizeData.OriginalDefinition1ActualLength + _resizeData.OriginalDefinition2ActualLength))
                {
                    CancelResize();
                    return;
                }

                double min, max;
                GetDeltaConstraints(out min, out max);

                // Flip when the splitter's flow direction isn't the same as the grid's
                if (FlowDirection != _resizeData.Grid.FlowDirection)
                    delta = -delta;

                // Constrain Delta to Min/MaxWidth of columns
                delta = Math.Min(Math.Max(delta, min), max);

                // With floating point operations there may be loss of precision to some degree. Eg. Adding a very 
                // small value to a very large one might result in the small value being ignored. In the following 
                // steps there are two floating point operations viz. actualLength1+delta and actualLength2-delta. 
                // It is possible that the addition resulted in loss of precision and the delta value was ignored, whereas 
                // the subtraction actual absorbed the delta value. This now means that 
                // (definition1LengthNew + definition2LengthNewis) 2 factors of precision away from 
                // (actualLength1 + actualLength2). This can cause a problem in the subsequent drag iteration where 
                // this will be interpreted as the cancellation of the resize operation. To avoid this imprecision we use 
                // make definition2LengthNew be a function of definition1LengthNew so that the precision or the loss 
                // thereof can be counterbalanced. See DevDiv bug#140228 for a manifestation of this problem.
                
                double definition1LengthNew = actualLength1 + delta;
                //double definition2LengthNew = actualLength2 - delta;
                double definition2LengthNew = actualLength1 + actualLength2 - definition1LengthNew;

                SetLengths(definition1LengthNew, definition2LengthNew);
            }
        }

        // Move the splitter using the Keyboard (Don't show preview)
        internal bool KeyboardMoveSplitter(double horizontalChange, double verticalChange)
        {
            // If moving with the mouse, ignore keyboard motion
            if (_resizeData != null)
            {
                return false;  // don't handle the event
            }

            InitializeData(false);  // don't show preview

            // Check that we are actually able to resize
            if (_resizeData == null)
            {
                return false;  // don't handle the event
            }

            // Keyboard keys are unaffected by FlowDirection.
            if (FlowDirection == FlowDirection.RightToLeft)
            {
                horizontalChange = -horizontalChange;
            }

            MoveSplitter(horizontalChange, verticalChange);

            _resizeData = null;

            return true;
        }

        #endregion

        #region Data


        // GridSplitter has special Behavior when columns are fixed
        // If the left column is fixed, splitter will only resize that column
        // Else if the right column is fixed, splitter will only resize the right column
        private enum SplitBehavior
        {
            Split, // Both columns/rows are star lengths
            Resize1, // resize 1 only
            Resize2, // resize 2 only
        }

        // Only store resize data if we are resizing
        private class ResizeData
        {
            public bool ShowsPreview;
            public PreviewAdorner Adorner;

            // The constraints to keep the Preview within valid ranges
            public double MinChange;
            public double MaxChange;
            
            // The grid to Resize
            public Grid Grid;

            // cache of Resize Direction and Behavior
            public GridResizeDirection ResizeDirection;
            public GridResizeBehavior ResizeBehavior;

            // The columns/rows to resize
            public DefinitionBase Definition1;
            public DefinitionBase Definition2;

            // Are the columns/rows star lengths
            public SplitBehavior SplitBehavior;

            // The index of the splitter
            public int SplitterIndex;
            
            // The indices of the columns/rows
            public int Definition1Index;
            public int Definition2Index;

            // The original lengths of Definition1 and Definition2 (to restore lengths if user cancels resize)
            public GridLength OriginalDefinition1Length;
            public GridLength OriginalDefinition2Length;
            public double OriginalDefinition1ActualLength;
            public double OriginalDefinition2ActualLength;

            // The minimum of Width/Height of Splitter.  Used to ensure splitter 
            //isn't hidden by resizing a row/column smaller than the splitter
            public double SplitterLength;
        }

        // Data used for resizing
        private ResizeData _resizeData;

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



