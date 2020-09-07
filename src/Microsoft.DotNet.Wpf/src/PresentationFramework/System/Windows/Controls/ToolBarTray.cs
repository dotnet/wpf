// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal;
using MS.Internal.Controls;
using MS.Internal.Documents;
using MS.Internal.KnownBoxes;
using MS.Internal.Telemetry.PresentationFramework;
using MS.Utility;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Markup; // IAddChild, ContentPropertyAttribute

namespace System.Windows.Controls
{
    /// <summary>
    ///     ToolBarTray is the layout container which handles layout of ToolBars relative to one another.
    /// It is responsible for handling placement, sizing and drag-and-drop to rearrange and
    /// resize behaviors associated with toolbars.
    /// It is also responsible for managing the rows (or "bands") in which toolbars appear.
    /// A ToolBar tray can be horizontal or vertical and can be used anywhere within an Avalon application,
    /// but it is expected that (like a Menu) it will often be docked to the top or side of the application window.     
    /// </summary>
    [ContentProperty("ToolBars")]
    public class ToolBarTray : FrameworkElement, IAddChild
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        static ToolBarTray()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToolBarTray), new FrameworkPropertyMetadata(typeof(ToolBarTray)));
            _dType = DependencyObjectType.FromSystemTypeInternal(typeof(ToolBarTray));


            EventManager.RegisterClassHandler(typeof(ToolBarTray), Thumb.DragDeltaEvent, new DragDeltaEventHandler(OnThumbDragDelta));
            KeyboardNavigation.ControlTabNavigationProperty.OverrideMetadata(typeof(ToolBarTray), new FrameworkPropertyMetadata(KeyboardNavigationMode.Cycle));

            ControlsTraceLogger.AddControl(TelemetryControls.ToolBarTray);
        }

        /// <summary>
        ///     Default ToolBarTray constructor
        /// </summary>
        public ToolBarTray() : base()
        {
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Properties
        //
        //-------------------------------------------------------------------

        #region Properties

        /// <summary>
        /// DependencyProperty for <see cref="Background" /> property.
        /// </summary>
        public static readonly DependencyProperty BackgroundProperty =
                Panel.BackgroundProperty.AddOwner(typeof(ToolBarTray),
                        new FrameworkPropertyMetadata(
                                (Brush)null,
                                FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The Background property defines the brush used to fill the area within the ToolBarTray.
        /// </summary>
        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        ///     Orientation property specify the flow direction. The default is horizontal.
        /// </summary>
        public static readonly DependencyProperty OrientationProperty =
                    DependencyProperty.Register(
                                "Orientation",
                                typeof(Orientation),
                                typeof(ToolBarTray),
                                new FrameworkPropertyMetadata(
                                            Orientation.Horizontal,
                                            FrameworkPropertyMetadataOptions.AffectsParentMeasure,
                                            new PropertyChangedCallback(OnOrientationPropertyChanged)),
                                new ValidateValueCallback(ScrollBar.IsValidOrientation));

        // Then ToolBarTray Orientation is changing we need to invalidate its ToolBars Orientation
        private static void OnOrientationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Collection<ToolBar> toolbarCollection = ((ToolBarTray)d).ToolBars;
            for (int i = 0; i < toolbarCollection.Count; i++)
            {
                toolbarCollection[i].CoerceValue(ToolBar.OrientationProperty);
            }
        }


        /// <summary>
        /// Specifies the orientation (horizontal or vertical)
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation) GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        ///     The DependencyProperty for the IsLocked property.
        /// </summary>
        public static readonly DependencyProperty IsLockedProperty =
                    DependencyProperty.RegisterAttached(
                               "IsLocked",
                                typeof(bool),
                                typeof(ToolBarTray),
                                new FrameworkPropertyMetadata(
                                            BooleanBoxes.FalseBox, 
                                            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// IsLocked property determine the ability to move the toolbars within the ToolBarTray
        /// If true then thumb from ToolBar style becomes hidden.
        /// This property inherits so ToolBar can use it in its Style triggers
        /// Default is false.
        /// </summary>
        public bool IsLocked
        {
            get { return (bool) GetValue(IsLockedProperty); }
            set { SetValue(IsLockedProperty, value); }
        }

        /// <summary>
        /// Writes the attached property IsLocked to the given element.
        /// </summary>
        /// <param name="element">The element to which to write the attached property.</param>
        /// <param name="value">The property value to set</param>
        public static void SetIsLocked(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            element.SetValue(IsLockedProperty, value);
        }

        /// <summary>
        /// Reads the attached property IsLocked from the given element.
        /// </summary>
        /// <param name="element">The element from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        public static bool GetIsLocked(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
            return (bool)element.GetValue(IsLockedProperty);
        }

        /// <summary>
        /// Returns a collection of ToolBar children for user to add/remove children manually
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public Collection<ToolBar> ToolBars
        {
            get
            {
                if (_toolBarsCollection == null)
                    _toolBarsCollection = new ToolBarCollection(this);

                return _toolBarsCollection;
            }
        }

        private class ToolBarCollection : Collection<ToolBar>
        {
            public ToolBarCollection(ToolBarTray parent)
            {
                _parent = parent;
            }

            protected override void InsertItem(int index, ToolBar toolBar)
            {
                base.InsertItem(index, toolBar);

                _parent.AddLogicalChild(toolBar);
                _parent.AddVisualChild(toolBar);
                _parent.InvalidateMeasure();
            }

            protected override void SetItem(int index, ToolBar toolBar)
            {
                ToolBar currentToolBar = Items[index];
                if (toolBar != currentToolBar)
                {
                    base.SetItem(index, toolBar);

                    // remove old item visual and logical links
                    _parent.RemoveVisualChild(currentToolBar);
                    _parent.RemoveLogicalChild(currentToolBar);

                    // add new item visual and logical links
                    _parent.AddLogicalChild(toolBar);
                    _parent.AddVisualChild(toolBar);
                    _parent.InvalidateMeasure();
                }
            }

            protected override void RemoveItem(int index)
            {
                ToolBar currentToolBar = this[index];
                base.RemoveItem(index);

                // remove old item visual and logical links
                _parent.RemoveVisualChild(currentToolBar);
                _parent.RemoveLogicalChild(currentToolBar);
                _parent.InvalidateMeasure();
            }

            protected override void ClearItems()
            {
                int count = Count;
                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        ToolBar currentToolBar = this[i];
                        _parent.RemoveVisualChild(currentToolBar);
                        _parent.RemoveLogicalChild(currentToolBar);
                    }
                    _parent.InvalidateMeasure();
                }

                base.ClearItems();
            }


            // Ref to a visual/logical ToolBarTray parent
            private readonly ToolBarTray _parent;
        }

        #endregion Properties


        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods
        ///<summary>
        /// This method is called to Add the object as a child of the ToolBarTray.  This method is used primarily
        /// by the parser.
        ///</summary>
        ///<param name="value">
        /// The object to add as a child; it must be a UIElement.
        ///</param>
        /// <ExternalAPI/>
        void IAddChild.AddChild(Object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            ToolBar toolBar = value as ToolBar;
            if (toolBar == null)
            {
                throw new ArgumentException(SR.Get(SRID.UnexpectedParameterType, value.GetType(), typeof(ToolBar)), "value");
            }

            ToolBars.Add(toolBar);
        }

        ///<summary>
        /// This method is called by the parser when text appears under the tag in markup.
        /// As default Panels do not support text, calling this method has no effect.
        ///</summary>
        ///<param name="text">
        /// Text to add as a child.
        ///</param>
        void IAddChild.AddText(string text)
        {
            XamlSerializerUtil.ThrowIfNonWhiteSpaceInAddText(text, this);
        }


        /// <summary>
        /// Returns enumerator to logical children.
        /// </summary>
        protected internal override IEnumerator LogicalChildren
        {
            get
            {
                if (this.VisualChildrenCount == 0)
                {
                    return EmptyEnumerator.Instance;
                }

                return this.ToolBars.GetEnumerator();
            }
        }

        #endregion Public Methods


        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Override from UIElement
        /// </summary>
        protected override void OnRender(DrawingContext dc)
        {
            // Draw background in rectangle inside border.
            Brush background = this.Background;
            if (background != null)
            {
                dc.DrawRectangle(background,
                                 null,
                                 new Rect(0, 0, RenderSize.Width, RenderSize.Height));
            }
        }

        /// <summary>
        /// Updates DesiredSize of the ToolBarTray. Called by parent UIElement.
        /// This is the first pass of layout.
        /// MeasureOverride distributes all ToolBars in bands depend on Band and BandIndex properties.
        /// All ToolBars with the same Band are places in one band. After that they are sorted by BandIndex.
        /// </summary>
        /// <param name="constraint">Constraint size is an "upper limit" that ToolBarTray should not exceed.</param>
        /// <returns>ToolBarTray' desired size.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            GenerateBands();

            Size toolBarTrayDesiredSize = new Size();
            int bandIndex;
            int toolBarIndex;
            bool fHorizontal = (Orientation == Orientation.Horizontal);
            Size childConstraint = new Size(Double.PositiveInfinity, Double.PositiveInfinity);

            for (bandIndex = 0; bandIndex < _bands.Count; bandIndex++)
            {
                // Calculate the available size before we measure the children.
                // remainingLength is the constraint minus sum of all minimum sizes
                double remainingLength = fHorizontal ? constraint.Width : constraint.Height;
                List<ToolBar> band = _bands[bandIndex].Band;
                double bandThickness = 0d;
                double bandLength = 0d;
                for (toolBarIndex = 0; toolBarIndex < band.Count; toolBarIndex++)
                {
                    ToolBar toolBar = band[toolBarIndex];
                    remainingLength -= toolBar.MinLength;
                    if (DoubleUtil.LessThan(remainingLength, 0))
                    {
                        remainingLength = 0;
                        break;
                    }
                }

                // Measure all children passing the remainingLength as a constraint
                for (toolBarIndex = 0; toolBarIndex < band.Count; toolBarIndex++)
                {
                    ToolBar toolBar = band[toolBarIndex];
                    remainingLength += toolBar.MinLength;
                    if (fHorizontal)
                        childConstraint.Width = remainingLength;
                    else
                        childConstraint.Height = remainingLength;
                    toolBar.Measure(childConstraint);
                    bandThickness = Math.Max(bandThickness, fHorizontal ? toolBar.DesiredSize.Height : toolBar.DesiredSize.Width);
                    bandLength += fHorizontal ? toolBar.DesiredSize.Width : toolBar.DesiredSize.Height;
                    remainingLength -= fHorizontal ? toolBar.DesiredSize.Width : toolBar.DesiredSize.Height;
                    if (DoubleUtil.LessThan(remainingLength, 0))
                    {
                        remainingLength = 0;
                    }
                }

                // Store band thickness in the BandInfo property
                _bands[bandIndex].Thickness = bandThickness;

                if (fHorizontal)
                {
                    toolBarTrayDesiredSize.Height += bandThickness;
                    toolBarTrayDesiredSize.Width = Math.Max(toolBarTrayDesiredSize.Width, bandLength);
                }
                else
                {
                    toolBarTrayDesiredSize.Width += bandThickness;
                    toolBarTrayDesiredSize.Height = Math.Max(toolBarTrayDesiredSize.Height, bandLength);
                }
            }

            return toolBarTrayDesiredSize;
        }

        /// <summary>
        /// ToolBarTray arranges its ToolBar children.
        /// </summary>
        /// <param name="arrangeSize">Size that ToolBarTray will assume to position children.</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            int bandIndex;
            int toolBarIndex;
            bool fHorizontal = (Orientation == Orientation.Horizontal);
            Rect rcChild = new Rect();

            for (bandIndex = 0; bandIndex < _bands.Count; bandIndex++)
            {
                List<ToolBar> band = _bands[bandIndex].Band;
                
                double bandThickness = _bands[bandIndex].Thickness;

                if (fHorizontal)
                    rcChild.X = 0;
                else
                    rcChild.Y = 0;

                for (toolBarIndex = 0; toolBarIndex < band.Count; toolBarIndex++)
                {
                    ToolBar toolBar = band[toolBarIndex];
                    Size toolBarArrangeSize = new Size(fHorizontal ? toolBar.DesiredSize.Width : bandThickness, fHorizontal ? bandThickness : toolBar.DesiredSize.Height );
                    rcChild.Size = toolBarArrangeSize;
                    toolBar.Arrange(rcChild);
                    if (fHorizontal)
                        rcChild.X += toolBarArrangeSize.Width;
                    else
                        rcChild.Y += toolBarArrangeSize.Height;
                }

                if (fHorizontal)
                    rcChild.Y += bandThickness;
                else
                    rcChild.X += bandThickness;
            }

            return arrangeSize;
        }

        /// <summary>
        /// Gets the Visual children count.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get
            {
                if (_toolBarsCollection == null)
                {
                    return 0;
                }
                else
                {
                    return _toolBarsCollection.Count;
                }
            }
        }

        /// <summary>
        /// Gets the Visual child at the specified index.
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (_toolBarsCollection == null)
            {
                throw new ArgumentOutOfRangeException("index", index, SR.Get(SRID.Visual_ArgumentOutOfRange));
            }
            return _toolBarsCollection[index];
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        // Event handler to listen to thumb events.
        private static void OnThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            ToolBarTray toolBarTray = (ToolBarTray)sender;

            // Don't move toolbars if IsLocked == true
            if (toolBarTray.IsLocked)
                return;

            toolBarTray.ProcessThumbDragDelta(e);
        }

        private void ProcessThumbDragDelta(DragDeltaEventArgs e)
        {
            // Process thumb event only if Thumb styled parent is a ToolBar under the TollBarTray
            Thumb thumb = e.OriginalSource as Thumb;
            if (thumb != null)
            {
                ToolBar toolBar = thumb.TemplatedParent as ToolBar;
                if (toolBar != null && toolBar.Parent == this)
                {
                    // _bandsDirty would be true at this time only when a Measure gets
                    // skipped between two mouse moves. Ideally that should not happen
                    // but VS has proved that it can. Hence making the code more robust.
                    // Uncomment the line below if the measure skip issue ever gets fixed.
                    // Debug.Assert(!_bandsDirty, "Bands should not be dirty at this point");
                    if (_bandsDirty)
                    {
                        GenerateBands();
                    }

                    bool fHorizontal = (Orientation == Orientation.Horizontal);
                    int currentBand = toolBar.Band;
                    Point pointRelativeToToolBarTray = Mouse.PrimaryDevice.GetPosition((IInputElement)this);
                    Point pointRelativeToToolBar = TransformPointToToolBar(toolBar, pointRelativeToToolBarTray);
                    int hittestBand = GetBandFromOffset(fHorizontal ? pointRelativeToToolBarTray.Y : pointRelativeToToolBarTray.X);
                    double newPosition;
                    double thumbChange = fHorizontal ? e.HorizontalChange : e.VerticalChange;
                    double toolBarPosition;
                    if (fHorizontal)
                    {
                        toolBarPosition = pointRelativeToToolBarTray.X - pointRelativeToToolBar.X;
                    }
                    else
                    {
                        toolBarPosition = pointRelativeToToolBarTray.Y - pointRelativeToToolBar.Y;
                    }
                    newPosition = toolBarPosition + thumbChange; // New toolBar position

                    // Move within the band
                    if (hittestBand == currentBand)
                    {
                        List<ToolBar> band = _bands[currentBand].Band;
                        int toolBarIndex = toolBar.BandIndex;

                        // Move ToolBar within the band
                        if (DoubleUtil.LessThan(thumbChange, 0)) // Move left/up
                        {
                            double toolBarsTotalMinimum = ToolBarsTotalMinimum(band, 0, toolBarIndex - 1);
                            // Check if minimized toolbars will fit in the range
                            if (DoubleUtil.LessThanOrClose(toolBarsTotalMinimum, newPosition))
                            {
                                ShrinkToolBars(band, 0, toolBarIndex - 1, -thumbChange);
                            }
                            else if (toolBarIndex > 0) // Swap toolbars
                            {
                                ToolBar prevToolBar = band[toolBarIndex - 1];
                                Point pointRelativeToPreviousToolBar = TransformPointToToolBar(prevToolBar, pointRelativeToToolBarTray);
                                // if pointer in on the left side of previous toolbar
                                if (DoubleUtil.LessThan((fHorizontal ? pointRelativeToPreviousToolBar.X : pointRelativeToPreviousToolBar.Y), 0))
                                {
                                    prevToolBar.BandIndex = toolBarIndex;
                                    band[toolBarIndex] = prevToolBar;

                                    toolBar.BandIndex = toolBarIndex - 1;
                                    band[toolBarIndex-1] = toolBar;

                                    if (toolBarIndex + 1 == band.Count) // If toolBar was the last item in the band
                                    {
                                        prevToolBar.ClearValue(fHorizontal ? WidthProperty : HeightProperty);
                                    }
                                }
                                else
                                { // Move to the left/up and shring the other toolbars
                                    if (fHorizontal)
                                    {
                                        if (DoubleUtil.LessThan(toolBarsTotalMinimum, pointRelativeToToolBarTray.X - pointRelativeToToolBar.X))
                                        {
                                            ShrinkToolBars(band, 0, toolBarIndex - 1, pointRelativeToToolBarTray.X - pointRelativeToToolBar.X - toolBarsTotalMinimum);
                                        }
                                    }
                                    else
                                    {
                                        if (DoubleUtil.LessThan(toolBarsTotalMinimum, pointRelativeToToolBarTray.Y - pointRelativeToToolBar.Y))
                                        {
                                            ShrinkToolBars(band, 0, toolBarIndex - 1, pointRelativeToToolBarTray.Y - pointRelativeToToolBar.Y - toolBarsTotalMinimum);
                                        }
                                    }
                                }
                            }
                        }
                        else // Move right/down
                        {
                            double toolBarsTotalMaximum = ToolBarsTotalMaximum(band, 0, toolBarIndex - 1);

                            if (DoubleUtil.GreaterThan(toolBarsTotalMaximum, newPosition))
                            {
                                ExpandToolBars(band, 0, toolBarIndex - 1, thumbChange);
                            }
                            else 
                            {
                                if (toolBarIndex < band.Count - 1) // Swap toolbars
                                {
                                    ToolBar nextToolBar = band[toolBarIndex + 1];
                                    Point pointRelativeToNextToolBar = TransformPointToToolBar(nextToolBar, pointRelativeToToolBarTray);
                                    // if pointer in on the right side of next toolbar
                                    if (DoubleUtil.GreaterThanOrClose((fHorizontal ? pointRelativeToNextToolBar.X : pointRelativeToNextToolBar.Y), 0))
                                    {
                                        nextToolBar.BandIndex = toolBarIndex;
                                        band[toolBarIndex] = nextToolBar;

                                        toolBar.BandIndex = toolBarIndex + 1;
                                        band[toolBarIndex + 1] = toolBar;
                                        if (toolBarIndex + 2 == band.Count) // If toolBar becomes the last item in the band
                                        {
                                            toolBar.ClearValue(fHorizontal ? WidthProperty : HeightProperty);
                                        }
                                    }
                                    else
                                    {
                                        ExpandToolBars(band, 0, toolBarIndex - 1, thumbChange);
                                    }
                                }
                                else
                                {
                                    ExpandToolBars(band, 0, toolBarIndex - 1, thumbChange);
                                }
                            }
                        }
                    }
                    else // Move ToolBar to another band
                    {
                        _bandsDirty = true;
                        toolBar.Band = hittestBand;
                        toolBar.ClearValue(fHorizontal ? WidthProperty : HeightProperty);

                        // move to another existing band
                        if (hittestBand >= 0 && hittestBand < _bands.Count)
                        {
                            MoveToolBar(toolBar, hittestBand, newPosition);
                        }

                        List<ToolBar> oldBand = _bands[currentBand].Band;
                        // currentBand should restore sizes to Auto
                        for (int i = 0; i < oldBand.Count; i++)
                        {
                            ToolBar currentToolBar = oldBand[i];
                            currentToolBar.ClearValue(fHorizontal ? WidthProperty : HeightProperty);
                        }
                    }

                    e.Handled = true;
                }
            }
        }

        private Point TransformPointToToolBar(ToolBar toolBar, Point point)
        {
            Point p = point;
            GeneralTransform transform = this.TransformToDescendant(toolBar);
            if (transform != null)
            {
                transform.TryTransform(point, out p);
            }
            return p;
        }

        private void ShrinkToolBars(List<ToolBar> band, int startIndex, int endIndex, double shrinkAmount)
        {
            if (Orientation == Orientation.Horizontal)
            {
                for (int i = endIndex; i >= startIndex; i--)
                {
                    ToolBar toolBar = band[i];
                    if (DoubleUtil.GreaterThanOrClose(toolBar.RenderSize.Width - shrinkAmount, toolBar.MinLength))
                    {
                        toolBar.Width = toolBar.RenderSize.Width - shrinkAmount;
                        break;
                    }
                    else
                    {
                        toolBar.Width = toolBar.MinLength;
                        shrinkAmount -= toolBar.RenderSize.Width - toolBar.MinLength;
                    }
                }
            }
            else
            {
                for (int i = endIndex; i >= startIndex; i--)
                {
                    ToolBar toolBar = band[i];
                    if (DoubleUtil.GreaterThanOrClose(toolBar.RenderSize.Height - shrinkAmount, toolBar.MinLength))
                    {
                        toolBar.Height = toolBar.RenderSize.Height - shrinkAmount;
                        break;
                    }
                    else
                    {
                        toolBar.Height = toolBar.MinLength;
                        shrinkAmount -= toolBar.RenderSize.Height - toolBar.MinLength;
                    }
                }
            }
        }

        private double ToolBarsTotalMinimum(List<ToolBar> band, int startIndex, int endIndex)
        {
            double totalMinLenght = 0d;
            for (int i = startIndex; i <= endIndex; i++)
            {
                totalMinLenght += band[i].MinLength;
            }
            return totalMinLenght;
        }

        private void ExpandToolBars(List<ToolBar> band, int startIndex, int endIndex, double expandAmount)
        {
            if (Orientation == Orientation.Horizontal)
            {
                for (int i = endIndex; i >= startIndex; i--)
                {
                    ToolBar toolBar = band[i];
                    if (DoubleUtil.LessThanOrClose(toolBar.RenderSize.Width + expandAmount, toolBar.MaxLength))
                    {
                        toolBar.Width = toolBar.RenderSize.Width + expandAmount;
                        break;
                    }
                    else
                    {
                        toolBar.Width = toolBar.MaxLength;
                        expandAmount -= toolBar.MaxLength - toolBar.RenderSize.Width;
                    }
                }
            }
            else
            {
                for (int i = endIndex; i >= startIndex; i--)
                {
                    ToolBar toolBar = band[i];
                    if (DoubleUtil.LessThanOrClose(toolBar.RenderSize.Height + expandAmount, toolBar.MaxLength))
                    {
                        toolBar.Height = toolBar.RenderSize.Height + expandAmount;
                        break;
                    }
                    else
                    {
                        toolBar.Height = toolBar.MaxLength;
                        expandAmount -= toolBar.MaxLength - toolBar.RenderSize.Height;
                    }
                }
            }
        }

        private double ToolBarsTotalMaximum(List<ToolBar> band, int startIndex, int endIndex)
        {
            double totalMaxLength = 0d;
            for (int i = startIndex; i <= endIndex; i++)
            {
                totalMaxLength += band[i].MaxLength;
            }
            return totalMaxLength;
        }

        private void MoveToolBar(ToolBar toolBar, int newBandNumber, double position)
        {
            int i;
            bool fHorizontal = Orientation == Orientation.Horizontal;

            List<ToolBar> newBand = _bands[newBandNumber].Band;
            // calculate the new BandIndex where toolBar should insert
            // calculate Width (layout) of the items before the toolBar
            if (DoubleUtil.LessThanOrClose(position, 0))
            {
                toolBar.BandIndex = -1; // This will position toolBar at the first place
            }
            else
            {
                double toolBarOffset = 0d;
                int newToolBarIndex = -1;
                for (i = 0; i < newBand.Count; i++)
                {
                    ToolBar currentToolBar = newBand[i];
                    if (newToolBarIndex == -1)
                    {
                        toolBarOffset += fHorizontal ? currentToolBar.RenderSize.Width : currentToolBar.RenderSize.Height; // points at the end of currentToolBar
                        if (DoubleUtil.GreaterThan(toolBarOffset, position))
                        {
                            newToolBarIndex = i + 1;
                            toolBar.BandIndex = newToolBarIndex;
                            // Update the currentToolBar width
                            if (fHorizontal)
                                currentToolBar.Width = Math.Max(currentToolBar.MinLength, currentToolBar.RenderSize.Width - toolBarOffset + position);
                            else
                                currentToolBar.Height = Math.Max(currentToolBar.MinLength, currentToolBar.RenderSize.Height - toolBarOffset + position);
                        }
                    }
                    else // After we insert the toolBar we need to increase the indexes
                    {
                        currentToolBar.BandIndex = i + 1;
                    }
                }
                if (newToolBarIndex == -1)
                {
                    toolBar.BandIndex = i;
                }
            }
        }

        private int GetBandFromOffset(double toolBarOffset)
        {
            if (DoubleUtil.LessThan(toolBarOffset, 0))
                return -1;

            double bandOffset = 0d;
            for (int i = 0; i < _bands.Count; i++)
            {
                bandOffset += _bands[i].Thickness;
                if (DoubleUtil.GreaterThan(bandOffset, toolBarOffset))
                    return i;
            }

            return _bands.Count;
        }

        #region Generate and Normalize bands

        // Generate all bands and normalize Band and BandIndex properties
        /// All ToolBars with the same Band are places in one band. After that they are sorted by BandIndex.
        private void GenerateBands()
        {
            if (!IsBandsDirty())
                return;

            Collection<ToolBar> toolbarCollection = ToolBars;

            _bands.Clear();
            for (int i = 0; i < toolbarCollection.Count; i++)
            {
                InsertBand(toolbarCollection[i], i);
            }

            // Normalize bands (make Band and BandIndex property 0,1,2,...)
            for (int bandIndex = 0; bandIndex < _bands.Count; bandIndex++)
            {
                List<ToolBar> band = _bands[bandIndex].Band;
                for (int toolBarIndex = 0; toolBarIndex < band.Count; toolBarIndex++)
                {
                    ToolBar toolBar = band[toolBarIndex];
                    // This will cause measure/arrange if some property changes
                    toolBar.Band = bandIndex;
                    toolBar.BandIndex = toolBarIndex;
                }
            }
            _bandsDirty = false;
        }

        // Verify is all toolbars are normalized (sorted in _bands by Band and BandIndex properties)
        private bool IsBandsDirty()
        {
            if (_bandsDirty)
                return true;

            int totalNumber = 0;
            Collection<ToolBar> toolbarCollection = ToolBars;
            for (int bandIndex = 0; bandIndex < _bands.Count; bandIndex++)
            {
                List<ToolBar> band = _bands[bandIndex].Band;
                for (int toolBarIndex = 0; toolBarIndex < band.Count; toolBarIndex++)
                {
                    ToolBar toolBar = band[toolBarIndex];
                    if (toolBar.Band != bandIndex || toolBar.BandIndex != toolBarIndex || !toolbarCollection.Contains(toolBar))
                        return true;
                }
                totalNumber += band.Count;
            }

            return totalNumber != toolbarCollection.Count;
        }

        // if toolBar.Band does not exist in bands collection when we create a new band
        private void InsertBand(ToolBar toolBar, int toolBarIndex)
        {
            int bandNumber = toolBar.Band;
            for (int i = 0; i < _bands.Count; i++)
            {
                int currentBandNumber = ((_bands[i].Band)[0]).Band;
                if (bandNumber == currentBandNumber)
                    return;
                if (bandNumber < currentBandNumber)
                {
                    // Band number does not exist - Insert
                    _bands.Insert(i, CreateBand(toolBarIndex));
                    return;
                }
            }

            // Band number does not exist - Add band at trhe end
            _bands.Add(CreateBand(toolBarIndex));
        }

        // Create new band and add all toolbars with the same Band and toolbar with index startIndex
        private BandInfo CreateBand(int startIndex)
        {
            Collection<ToolBar> toolbarCollection = ToolBars;
            BandInfo bandInfo = new BandInfo();
            ToolBar toolBar = toolbarCollection[startIndex];
            bandInfo.Band.Add(toolBar);
            int bandNumber = toolBar.Band;
            for (int i = startIndex + 1; i < toolbarCollection.Count; i++)
            {
                toolBar = toolbarCollection[i];
                if (bandNumber == toolBar.Band)
                    InsertToolBar(toolBar, bandInfo.Band);
            }
            return bandInfo;
        }

        // Insert toolbar into band list so band remains sorted
        private void InsertToolBar(ToolBar toolBar, List<ToolBar> band)
        {
            for (int i = 0; i < band.Count; i++)
            {
                if (toolBar.BandIndex < band[i].BandIndex)
                {
                    band.Insert(i, toolBar);
                    return;
                }
            }
            band.Add(toolBar);
        }

        #endregion Generate and Normalize bands

        #endregion

        //-------------------------------------------------------------------
        //
        //  Private classes
        //
        //-------------------------------------------------------------------

        #region Private classes

        private class BandInfo
        {
            public BandInfo() { }

            public List<ToolBar> Band
            {
                get { return _band; }
            }

            public double Thickness
            {
                get { return _thickness; }
                set { _thickness = value; }
            }

            private List<ToolBar> _band = new List<ToolBar>();
            private double _thickness;
        }

        #endregion

        #region Private members

        // ToolBarTray generates list of bands depend on ToolBar.Band property.
        // Each band is a list of toolbars sorted by ToolBar.BandIndex property.
        private List<BandInfo> _bands = new List<BandInfo>(0);
        private bool _bandsDirty = true;
        private ToolBarCollection _toolBarsCollection = null;

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
