// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Contains the DockPanel class.
//              Spec at DockPanel.xml
//

using MS.Internal;
using MS.Internal.PresentationFramework;
using MS.Internal.Telemetry.PresentationFramework;
using MS.Utility;
using System.Diagnostics;
using System.Windows.Threading;

using System.Windows.Media;

using System;

namespace System.Windows.Controls
{
    #region Dock enum type

    /// <summary>
    /// Dock - Enum which describes how to position and stretch the child of a DockPanel.
    /// </summary>
    /// <seealso cref="DockPanel" />
    public enum Dock
    {
        /// <summary>
        /// Position this child at the left of the remaining space.
        /// </summary>
        Left,

        /// <summary>
        /// Position this child at the top of the remaining space.
        /// </summary>
        Top,

        /// <summary>
        /// Position this child at the right of the remaining space.
        /// </summary>
        Right,

        /// <summary>
        /// Position this child at the bottom of the remaining space.
        /// </summary>
        Bottom,
    }

    #endregion

    /// <summary>
    /// DockPanel is used to size and position children inward from the edges of available space.
    ///
    /// A <see cref="System.Windows.Controls.Dock" /> enum (see <see cref="SetDock" /> and <see cref="GetDock" />)
    /// determines on which size a child is placed.  Children are stacked in order from these edges until
    /// there is no more space; this happens when previous children have consumed all available space, or a child
    /// with Dock set to Fill is encountered.
    /// </summary>
    public class DockPanel : Panel
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        static DockPanel()
        {
            ControlsTraceLogger.AddControl(TelemetryControls.DockPanel);
        }

        /// <summary>
        ///     Default DependencyObject constructor
        /// </summary>
        /// <remarks>
        ///     Automatic determination of current Dispatcher. Use alternative constructor
        ///     that accepts a Dispatcher for best performance.
        /// </remarks>
        public DockPanel() : base()
        {
        }

        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Methods
        //
        //-------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Reads the attached property Dock from the given element.
        /// </summary>
        /// <param name="element">UIElement from which to read the attached property.</param>
        /// <returns>The property's value.</returns>
        /// <seealso cref="DockPanel.DockProperty" />
        [AttachedPropertyBrowsableForChildren()]
        public static Dock GetDock(UIElement element)
        {
            if (element == null) { throw new ArgumentNullException("element"); }

            return (Dock) element.GetValue(DockProperty);
        }

        /// <summary>
        /// Writes the attached property Dock to the given element.
        /// </summary>
        /// <param name="element">UIElement to which to write the attached property.</param>
        /// <param name="dock">The property value to set</param>
        /// <seealso cref="DockPanel.DockProperty" />
        public static void SetDock(UIElement element, Dock dock)
        {
            if (element == null) { throw new ArgumentNullException("element"); }

            element.SetValue(DockProperty, dock);
        }

        private static void OnDockChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement uie = d as UIElement; //it may be anyting, like FlowDocument... bug 1237275
            if(uie != null)
            {
                DockPanel p = VisualTreeHelper.GetParent(uie) as DockPanel;
                if(p != null)
                {
                    p.InvalidateMeasure();
                }
            }
        }
        
        #endregion

        //-------------------------------------------------------------------
        //
        //  Public Properties + Dependency Properties's
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// This property controls whether the last child in the DockPanel should be stretched to fill any 
        /// remaining available space.
        /// </summary>
        public bool LastChildFill
        {
            get { return (bool) GetValue(LastChildFillProperty); }
            set { SetValue(LastChildFillProperty, value); }
        }


        /// <summary>
        /// DependencyProperty for <see cref="LastChildFill" /> property.
        /// </summary>
        [CommonDependencyProperty]
        public static readonly DependencyProperty LastChildFillProperty =
                DependencyProperty.Register(
                        "LastChildFill", 
                        typeof(bool), 
                        typeof(DockPanel),
                        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange));


        /// <summary>
        /// DependencyProperty for Dock property.
        /// </summary>
        /// <seealso cref="DockPanel.GetDock" />
        /// <seealso cref="DockPanel.SetDock" />
        [CommonDependencyProperty]
        public static readonly DependencyProperty DockProperty =
                DependencyProperty.RegisterAttached(
                        "Dock", 
                        typeof(Dock), 
                        typeof(DockPanel),
                        new FrameworkPropertyMetadata(
                            Dock.Left, 
                            new PropertyChangedCallback(OnDockChanged)),
                        new ValidateValueCallback(IsValidDock));

        #endregion

        //-------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //-------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Updates DesiredSize of the DockPanel.  Called by parent UIElement.  This is the first pass of layout.
        /// </summary>
        /// <remarks>
        /// Children are measured based on their sizing properties and <see cref="System.Windows.Controls.Dock" />.  
        /// Each child is allowed to consume all of the space on the side on which it is docked; Left/Right docked
        /// children are granted all vertical space for their entire width, and Top/Bottom docked children are
        /// granted all horizontal space for their entire height.
        /// </remarks>
        /// <param name="constraint">Constraint size is an "upper limit" that the return value should not exceed.</param>
        /// <returns>The Panel's desired size.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            UIElementCollection children = InternalChildren;

            double parentWidth       = 0;   // Our current required width due to children thus far.
            double parentHeight      = 0;   // Our current required height due to children thus far.
            double accumulatedWidth  = 0;   // Total width consumed by children.
            double accumulatedHeight = 0;   // Total height consumed by children.

            for (int i = 0, count = children.Count; i < count; ++i)
            {
                UIElement child = children[i];
                Size   childConstraint;             // Contains the suggested input constraint for this child.
                Size   childDesiredSize;            // Contains the return size from child measure.

                if (child == null) { continue; }

                // Child constraint is the remaining size; this is total size minus size consumed by previous children.
                childConstraint = new Size(Math.Max(0.0, constraint.Width - accumulatedWidth),
                                           Math.Max(0.0, constraint.Height - accumulatedHeight));

                // Measure child.
                child.Measure(childConstraint);
                childDesiredSize = child.DesiredSize;

                // Now, we adjust:
                // 1. Size consumed by children (accumulatedSize).  This will be used when computing subsequent
                //    children to determine how much space is remaining for them.
                // 2. Parent size implied by this child (parentSize) when added to the current children (accumulatedSize).
                //    This is different from the size above in one respect: A Dock.Left child implies a height, but does
                //    not actually consume any height for subsequent children.
                // If we accumulate size in a given dimension, the next child (or the end conditions after the child loop)
                // will deal with computing our minimum size (parentSize) due to that accumulation.
                // Therefore, we only need to compute our minimum size (parentSize) in dimensions that this child does
                //   not accumulate: Width for Top/Bottom, Height for Left/Right.
                switch (DockPanel.GetDock(child))
                {
                    case Dock.Left:
                    case Dock.Right:
                        parentHeight = Math.Max(parentHeight, accumulatedHeight + childDesiredSize.Height);
                        accumulatedWidth += childDesiredSize.Width;
                        break;

                    case Dock.Top:
                    case Dock.Bottom:
                        parentWidth = Math.Max(parentWidth, accumulatedWidth + childDesiredSize.Width);
                        accumulatedHeight += childDesiredSize.Height;
                        break;
                }
            }

            // Make sure the final accumulated size is reflected in parentSize.
            parentWidth = Math.Max(parentWidth, accumulatedWidth);
            parentHeight = Math.Max(parentHeight, accumulatedHeight);

            return (new Size(parentWidth, parentHeight));
        }

        /// <summary>
        /// DockPanel computes a position and final size for each of its children based upon their
        /// <see cref="System.Windows.Controls.Dock" /> enum and sizing properties.
        /// </summary>
        /// <param name="arrangeSize">Size that DockPanel will assume to position children.</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            UIElementCollection children = InternalChildren;
            int totalChildrenCount = children.Count;
            int nonFillChildrenCount = totalChildrenCount - (LastChildFill ? 1 : 0);

            double accumulatedLeft   = 0;
            double accumulatedTop    = 0;
            double accumulatedRight  = 0;
            double accumulatedBottom = 0;

            for (int i = 0; i < totalChildrenCount; ++i)
            {
                UIElement child = children[i];
                if (child == null) { continue; }

                Size childDesiredSize = child.DesiredSize;
                Rect rcChild = new Rect(
                    accumulatedLeft, 
                    accumulatedTop,
                    Math.Max(0.0, arrangeSize.Width - (accumulatedLeft + accumulatedRight)), 
                    Math.Max(0.0, arrangeSize.Height - (accumulatedTop + accumulatedBottom))    );

                if (i < nonFillChildrenCount)
                {
                    switch (DockPanel.GetDock(child))
                    {
                        case Dock.Left:
                            accumulatedLeft += childDesiredSize.Width;
                            rcChild.Width = childDesiredSize.Width;
                            break;

                        case Dock.Right:
                            accumulatedRight += childDesiredSize.Width;
                            rcChild.X = Math.Max(0.0, arrangeSize.Width - accumulatedRight);
                            rcChild.Width = childDesiredSize.Width;
                            break;

                        case Dock.Top:
                            accumulatedTop += childDesiredSize.Height;
                            rcChild.Height = childDesiredSize.Height;
                            break;

                        case Dock.Bottom:
                            accumulatedBottom += childDesiredSize.Height;
                            rcChild.Y = Math.Max(0.0, arrangeSize.Height - accumulatedBottom);
                            rcChild.Height = childDesiredSize.Height;
                            break;
                    }
                }

                child.Arrange(rcChild);
            }

            return (arrangeSize);
        }

        #endregion Protected Methods

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        internal static bool IsValidDock(object o)
        {
            Dock dock = (Dock)o;

            return (    dock == Dock.Left
                    ||  dock == Dock.Top
                    ||  dock == Dock.Right
                    ||  dock == Dock.Bottom);
        }
        
        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current DependencyObject
        //  2. This is a performance optimization
        //
        internal override int EffectiveValuesInitialSize
        {
            get { return 9; }
        }

        #endregion Private Methods
    }
}

