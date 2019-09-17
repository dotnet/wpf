// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal;
using MS.Internal.KnownBoxes;
using MS.Internal.Telemetry.PresentationFramework;
using MS.Utility;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace System.Windows.Controls.Primitives
{
    /// <summary>
    ///     ToolBarPanel is used to arrange children within the ToolBar. It is used in the ToolBar style as items host.
    /// </summary>
    // There is no need for this panel to be a StackPanel.
    public class ToolBarPanel : StackPanel
    {
        static ToolBarPanel()
        {
            ControlsTraceLogger.AddControl(TelemetryControls.ToolBarPanel);
        }

        /// <summary>
        ///     Instantiates a new instance of this class.
        /// </summary>
        public ToolBarPanel() : base()
        {
        }

        #region Layout

        internal double MinLength
        {
            get;
            private set;
        }

        internal double MaxLength
        {
            get;
            private set;
        }

        private bool MeasureGeneratedItems(bool asNeededPass, Size constraint, bool horizontal, double maxExtent, ref Size panelDesiredSize, out double overflowExtent)
        {
            ToolBarOverflowPanel overflowPanel = ToolBarOverflowPanel;
            bool sendToOverflow = false; // Becomes true when the first AsNeeded does not fit
            bool hasOverflowItems = false;
            bool overflowNeedsInvalidation = false;
            overflowExtent = 0.0;
            UIElementCollection children = InternalChildren;
            int childrenCount = children.Count;
            int childrenIndex = 0;

            for (int i = 0; i < _generatedItemsCollection.Count; i++)
            {
                UIElement child = _generatedItemsCollection[i];
                OverflowMode overflowMode = ToolBar.GetOverflowMode(child);
                bool asNeededMode = overflowMode == OverflowMode.AsNeeded;

                // MeasureGeneratedItems is called twice to do a complete measure.
                // The first pass measures Always and Never items -- items that
                // are guaranteed to be or not to be in the overflow menu.
                // The second pass measures AsNeeded items and determines whether
                // there is enough room for them in the main bar or if they should
                // be placed in the overflow menu.
                // Check here whether the overflow mode matches a mode we should be
                // examining in this pass.
                if (asNeededMode == asNeededPass)
                {
                    DependencyObject visualParent = VisualTreeHelper.GetParent(child);

                    // In non-Always overflow modes, measure for main bar placement.
                    if ((overflowMode != OverflowMode.Always) && !sendToOverflow)
                    {
                        // Children may change their size depending on whether they are in the overflow
                        // menu or not. Ensure that when we measure, we are using the main bar size.
                        // If the item goes to overflow, this property will be updated later in this loop
                        // when it is removed from the visual tree.
                        ToolBar.SetIsOverflowItem(child, BooleanBoxes.FalseBox);
                        child.Measure(constraint);
                        Size childDesiredSize = child.DesiredSize;

                        // If the child is an AsNeeded, check if it fits. If it doesn't then
                        // this child and all subsequent AsNeeded children should be sent
                        // to the overflow menu.
                        if (asNeededMode)
                        {
                            double newExtent;
                            if (horizontal)
                            {
                                newExtent = childDesiredSize.Width + panelDesiredSize.Width;
                            }
                            else
                            {
                                newExtent = childDesiredSize.Height + panelDesiredSize.Height;
                            }
                            if (DoubleUtil.GreaterThan(newExtent, maxExtent))
                            {
                                // It doesn't fit, send to overflow
                                sendToOverflow = true;
                            }
                        }

                        // The child has been validated as belonging in the main bar.
                        // Update the panel desired size dimensions, and ensure the child
                        // is in the main bar's visual tree.
                        if (!sendToOverflow)
                        {
                            if (horizontal)
                            {
                                panelDesiredSize.Width += childDesiredSize.Width;
                                panelDesiredSize.Height = Math.Max(panelDesiredSize.Height, childDesiredSize.Height);
                            }
                            else
                            {
                                panelDesiredSize.Width = Math.Max(panelDesiredSize.Width, childDesiredSize.Width);
                                panelDesiredSize.Height += childDesiredSize.Height;
                            }

                            if (visualParent != this)
                            {
                                if ((visualParent == overflowPanel) && (overflowPanel != null))
                                {
                                    overflowPanel.Children.Remove(child);
                                }

                                if (childrenIndex < childrenCount)
                                {
                                    children.InsertInternal(childrenIndex, child);
                                }
                                else
                                {
                                    children.AddInternal(child);
                                }
                                childrenCount++;
                            }

                            Debug.Assert(children[childrenIndex] == child, "InternalChildren is out of sync with _generatedItemsCollection.");
                            childrenIndex++;
                        }
                    }

                    // The child should go to the overflow menu
                    if ((overflowMode == OverflowMode.Always) || sendToOverflow)
                    {
                        hasOverflowItems = true;

                        // If a child is in the overflow menu, we don't want to keep measuring.
                        // However, we need to calculate the MaxLength as well as set the desired height
                        // correctly. Thus, we will use the DesiredSize of the child. There is a problem
                        // that can occur if the child changes size while in the overflow menu and
                        // was recently displayed. It will be measure clean, yet its DesiredSize
                        // will not be accurate for the MaxLength calculation.
                        if (child.MeasureDirty)
                        {
                            // Set this temporarily in case the size is different while in the overflow area
                            ToolBar.SetIsOverflowItem(child, BooleanBoxes.FalseBox);
                            child.Measure(constraint);
                        }

                        // Even when in the overflow, we need two pieces of information:
                        // 1. We need to continue to track the maximum size of the non-logical direction
                        //    (i.e. height in horizontal bars). This way, ToolBars with everything in
                        //    the overflow will still have some height.
                        // 2. We want to track how much of the space we saved by placing the child in
                        //    the overflow menu. This is used to calculate MinLength and MaxLength.
                        Size childDesiredSize = child.DesiredSize;
                        if (horizontal)
                        {
                            overflowExtent += childDesiredSize.Width;
                            panelDesiredSize.Height = Math.Max(panelDesiredSize.Height, childDesiredSize.Height);
                        }
                        else
                        {
                            overflowExtent += childDesiredSize.Height;
                            panelDesiredSize.Width = Math.Max(panelDesiredSize.Width, childDesiredSize.Width);
                        }

                        // Set the flag to indicate that the child is in the overflow menu
                        ToolBar.SetIsOverflowItem(child, BooleanBoxes.TrueBox);

                        // If the child is in this panel's visual tree, remove it.
                        if (visualParent == this)
                        {
                            Debug.Assert(children[childrenIndex] == child, "InternalChildren is out of sync with _generatedItemsCollection.");
                            children.RemoveNoVerify(child);
                            childrenCount--;
                            overflowNeedsInvalidation = true;
                        }
                        // If the child isnt connected to the visual tree, notify the overflow panel to pick it up.
                        else if (visualParent == null)
                        {
                            overflowNeedsInvalidation = true;
                        }
                    }
                }
                else
                {
                    // We are not measure this child in this pass. Update the index into the
                    // visual children collection.
                    if ((childrenIndex < childrenCount) && (children[childrenIndex] == child))
                    {
                        childrenIndex++;
                    }
                }
            }

            // A child was added to the overflow panel, but since we don't add it
            // to the overflow panel's visual collection until that panel's measure
            // pass, we need to mark it as measure dirty.
            if (overflowNeedsInvalidation && (overflowPanel != null))
            {
                overflowPanel.InvalidateMeasure();
            }

            return hasOverflowItems;
        }

        /// <summary>
        /// Measure the content and store the desired size of the content
        /// </summary>
        /// <param name="constraint"></param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size constraint)
        {
            Size stackDesiredSize = new Size();

            if (IsItemsHost)
            {
                Size layoutSlotSize = constraint;
                double maxExtent;
                double overflowExtent;
                bool horizontal = (Orientation == Orientation.Horizontal);

                if (horizontal)
                {
                    layoutSlotSize.Width = Double.PositiveInfinity;
                    maxExtent = constraint.Width;
                }
                else
                {
                    layoutSlotSize.Height = Double.PositiveInfinity;
                    maxExtent = constraint.Height;
                }

                // This first call will measure all of the non-AsNeeded elements (i.e. we know
                // whether they're going into the overflow or not.
                // overflowExtent will be the size of the Always elements, which is not actually
                // needed for subsequent calculations.
                bool hasAlwaysOverflowItems = MeasureGeneratedItems(/* asNeeded = */ false, layoutSlotSize, horizontal, maxExtent, ref stackDesiredSize, out overflowExtent);

                // At this point, the desired size is the minimum size of the ToolBar.
                MinLength = horizontal ? stackDesiredSize.Width : stackDesiredSize.Height;

                // This second call will measure all of the AsNeeded elements and place
                // them in the appropriate location.
                bool hasAsNeededOverflowItems = MeasureGeneratedItems(/* asNeeded = */ true, layoutSlotSize, horizontal, maxExtent, ref stackDesiredSize, out overflowExtent);

                // At this point, the desired size is complete. The desired size plus overflowExtent
                // is the maximum size of the ToolBar.
                MaxLength = (horizontal ? stackDesiredSize.Width : stackDesiredSize.Height) + overflowExtent;

                ToolBar toolbar = ToolBar;
                if (toolbar != null)
                {
                    toolbar.SetValue(ToolBar.HasOverflowItemsPropertyKey, hasAlwaysOverflowItems || hasAsNeededOverflowItems);
                }
            }
            else
            {
                stackDesiredSize = base.MeasureOverride(constraint);
            }

            return stackDesiredSize;
        }

        /// <summary>
        /// Content arrangement.
        /// </summary>
        /// <param name="arrangeSize">Arrange size</param>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            UIElementCollection children = InternalChildren;
            bool fHorizontal = (Orientation == Orientation.Horizontal);
            Rect rcChild = new Rect(arrangeSize);
            double previousChildSize = 0.0d;

            //
            // Arrange and Position Children.
            //
            for (int i = 0, count = children.Count; i < count; ++i)
            {
                UIElement child = (UIElement)children[i];

                if (fHorizontal)
                {
                    rcChild.X += previousChildSize;
                    previousChildSize = child.DesiredSize.Width;
                    rcChild.Width = previousChildSize;
                    rcChild.Height = Math.Max(arrangeSize.Height, child.DesiredSize.Height);
                }
                else
                {
                    rcChild.Y += previousChildSize;
                    previousChildSize = child.DesiredSize.Height;
                    rcChild.Height = previousChildSize;
                    rcChild.Width = Math.Max(arrangeSize.Width, child.DesiredSize.Width);
                }

                child.Arrange(rcChild);
            }

            return arrangeSize;
        }

        /// <summary>
        /// ToolBarPanel sets bindings a on its Orientation property to its TemplatedParent if the
        /// property is not already set.
        /// </summary>
        internal override void OnPreApplyTemplate()
        {
            base.OnPreApplyTemplate();

            if (TemplatedParent is ToolBar && !HasNonDefaultValue(OrientationProperty))
            {
                Binding binding = new Binding();
                binding.RelativeSource = RelativeSource.TemplatedParent;
                binding.Path = new PropertyPath(ToolBar.OrientationProperty);
                SetBinding(OrientationProperty, binding);
            }
        }

        #endregion

        #region Item Generation

        internal override void GenerateChildren()
        {
            base.GenerateChildren();

            // This could re-enter InternalChildren, but after base.GenerateChildren, the collection
            // should be fully instantiated and ready to go.
            UIElementCollection children = InternalChildren;
            if (_generatedItemsCollection == null)
            {
                _generatedItemsCollection = new List<UIElement>(children.Count);
            }
            else
            {
                _generatedItemsCollection.Clear();
            }

            ToolBarOverflowPanel overflowPanel = ToolBarOverflowPanel;
            if (overflowPanel != null)
            {
                overflowPanel.Children.Clear();
                overflowPanel.InvalidateMeasure();
            }

            int childrenCount = children.Count;
            for (int i = 0; i < childrenCount; i++)
            {
                UIElement child = children[i];

                // Reset the overflow decision. This will be re-evaluated on the next measure
                // by ToolBarPanel.MeasureOverride.
                ToolBar.SetIsOverflowItem(child, BooleanBoxes.FalseBox);

                _generatedItemsCollection.Add(child);
            }
        }

        // This method returns a bool to indicate if or not the panel layout is affected by this collection change
        internal override bool OnItemsChangedInternal(object sender, ItemsChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddChildren(args.Position, args.ItemCount);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveChildren(args.Position, args.ItemUICount);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    ReplaceChildren(args.Position, args.ItemCount, args.ItemUICount);
                    break;
                case NotifyCollectionChangedAction.Move:
                    MoveChildren(args.OldPosition, args.Position, args.ItemUICount);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    base.OnItemsChangedInternal(sender, args);
                    break;
            }

            return true;
        }

        private void AddChildren(GeneratorPosition pos, int itemCount)
        {
            IItemContainerGenerator generator = (IItemContainerGenerator)Generator;
            using (generator.StartAt(pos, GeneratorDirection.Forward))
            {
                for (int i = 0; i < itemCount; i++)
                {
                    UIElement e = generator.GenerateNext() as UIElement;
                    if (e != null)
                    {
                        _generatedItemsCollection.Insert(pos.Index + 1 + i, e);
                        generator.PrepareItemContainer(e);
                    }
                    else
                    {
                        ItemContainerGenerator icg = Generator as ItemContainerGenerator;
                        if (icg != null)
                        {
                            icg.Verify();
                        }
                    }
                }
            }
        }

        private void RemoveChild(UIElement child)
        {
            DependencyObject visualParent = VisualTreeHelper.GetParent(child);

            if (visualParent == this)
            {
                InternalChildren.RemoveInternal(child);
            }
            else
            {
                ToolBarOverflowPanel overflowPanel = ToolBarOverflowPanel;
                if ((visualParent == overflowPanel) && (overflowPanel != null))
                {
                    overflowPanel.Children.Remove(child);
                }
            }
        }

        private void RemoveChildren(GeneratorPosition pos, int containerCount)
        {
            for (int i = 0; i < containerCount; i++)
            {
                RemoveChild(_generatedItemsCollection[pos.Index + i]);
            }

            _generatedItemsCollection.RemoveRange(pos.Index, containerCount);
        }

        private void ReplaceChildren(GeneratorPosition pos, int itemCount, int containerCount)
        {
            Debug.Assert(itemCount == containerCount, "ToolBarPanel expects Replace to affect only realized containers");

            IItemContainerGenerator generator = (IItemContainerGenerator)Generator;
            using (generator.StartAt(pos, GeneratorDirection.Forward, true))
            {
                for (int i = 0; i < itemCount; i++)
                {
                    bool isNewlyRealized;
                    UIElement e = generator.GenerateNext(out isNewlyRealized) as UIElement;

                    Debug.Assert(e != null && !isNewlyRealized, "ToolBarPanel expects Replace to affect only realized containers");
                    if (e != null && !isNewlyRealized)
                    {
                        RemoveChild(_generatedItemsCollection[pos.Index + i]);
                        _generatedItemsCollection[pos.Index + i] = e;
                        generator.PrepareItemContainer(e);
                    }
                    else
                    {
                        ItemContainerGenerator icg = Generator as ItemContainerGenerator;
                        if (icg != null)
                        {
                            icg.Verify();
                        }
                    }
                }
            }
        }

        private void MoveChildren(GeneratorPosition fromPos, GeneratorPosition toPos, int containerCount)
        {
            if (fromPos == toPos)
                return;

            IItemContainerGenerator generator = (IItemContainerGenerator)Generator;
            int toIndex = generator.IndexFromGeneratorPosition(toPos);

            UIElement[] elements = new UIElement[containerCount];

            for (int i = 0; i < containerCount; i++)
            {
                UIElement child = _generatedItemsCollection[fromPos.Index + i];
                RemoveChild(child);
                elements[i] = child;
            }

            _generatedItemsCollection.RemoveRange(fromPos.Index, containerCount);

            for (int i = 0; i < containerCount; i++)
            {
                _generatedItemsCollection.Insert(toIndex + i, elements[i]);
            }
        }

        #endregion

        #region Helpers

        private ToolBar ToolBar
        {
            get { return TemplatedParent as ToolBar; }
        }

        private ToolBarOverflowPanel ToolBarOverflowPanel
        {
            get
            {
                ToolBar tb = ToolBar;
                return tb == null ? null : tb.ToolBarOverflowPanel;
            }
        }

        // Accessed by ToolBarOverflowPanel
        internal List<UIElement> GeneratedItemsCollection
        {
            get { return _generatedItemsCollection; }
        }

        #endregion

        #region Data

        private List<UIElement> _generatedItemsCollection;

        #endregion
    }
}

