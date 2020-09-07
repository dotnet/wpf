// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

using MS.Internal;

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon.Primitives
#else
namespace Microsoft.Windows.Controls.Ribbon.Primitives
#endif
{
    /// <summary>
    ///   Used in the RibbonQuickAccessToolBar template as the items host.  Not meant to be used separately from RibbonQuickAccessToolBar.
    /// </summary>
    public class RibbonQuickAccessToolBarPanel : VirtualizingPanel
    {
        #region OnItemsChanged

        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
        {
            base.OnItemsChanged(sender, args);

            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (Generator != null)
                    {
                        using (Generator.StartAt(args.Position, GeneratorDirection.Forward, true))
                        {
                            UIElement childToAdd = Generator.GenerateNext() as UIElement;
                            Generator.PrepareItemContainer(childToAdd);
                            int indexToInsertAt = Generator.IndexFromGeneratorPosition(args.Position);
                            GeneratedChildren.Insert(indexToInsertAt, childToAdd);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveItem(args.Position.Index);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    RemoveInternalChildRange(0, InternalChildren.Count);
                    if (QAT != null &&
                        QAT.OverflowPanel != null)
                    {
                        QAT.OverflowPanel.Children.Clear();
                    }

                    RepopulateGeneratedChildren();
                    break;
                case NotifyCollectionChangedAction.Move:
                    UIElement childToMove = GeneratedChildren[args.OldPosition.Index];
                    RemoveItem(args.OldPosition.Index);
                    GeneratedChildren.Insert(args.Position.Index, childToMove);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
            }
        }

        private void RemoveItem(int index)
        {
            if (index < InternalChildren.Count)
            {
                RemoveInternalChildRange(index, 1);
            }
            else
            {
                int indexInOverflowPanel = index - InternalChildren.Count;
                if (QAT != null &&
                    QAT.OverflowPanel != null)
                {
                    QAT.OverflowPanel.Children.RemoveAt(indexInOverflowPanel);
                }
            }

            GeneratedChildren.RemoveAt(index);
        }

        #endregion OnItemsChanged

        #region Generator and GeneratedChildren

#if !RIBBON_IN_FRAMEWORK
        private IItemContainerGenerator _generator;

        private IItemContainerGenerator Generator
        {
            get
            {
                if (_generator == null &&
                    QAT != null)
                {
                    IItemContainerGenerator parentGenerator = QAT.ItemContainerGenerator;
                    _generator = parentGenerator.GetItemContainerGeneratorForPanel(this);
                }

                return _generator;
            }
        }
#endif

        private List<UIElement> _generatedChildren;

        internal List<UIElement> GeneratedChildren
        {
            get
            {
                if (_generatedChildren == null)
                {
                    RepopulateGeneratedChildren();
                }

                return _generatedChildren;
            }
        }

        private void RepopulateGeneratedChildren()
        {
            if (_generatedChildren == null)
            {
                _generatedChildren = new List<UIElement>();
            }
            else
            {
                _generatedChildren.Clear();
            }

            if (Generator != null)
            {
                GeneratorPosition startPos = Generator.GeneratorPositionFromIndex(0);

                UIElement child;
                using (Generator.StartAt(startPos, GeneratorDirection.Forward, true))
                {
                    while ((child = Generator.GenerateNext() as UIElement) != null)
                    {
                        Generator.PrepareItemContainer(child);
                        _generatedChildren.Add(child);
                    }
                }
            }
        }

        #endregion Generator and GeneratedChildren

        #region Measure & Arrange

        // Basic algorithm:
        // For each child in GeneratedChildren...
        //   If we are not sending items to overflow...
        //     If the child is already in InternalChildren at the current index, do nothing.  Otherwise...
        //       If the child is in overlow panel at position 0, remove it and set IsOverflowItem = false.
        //       Insert the child into InternalChildren at the current index.
        //       Measure the child...
        //         If the child fits, increment the panel's desired width by the child's width.
        //         Otherwise...
        //           Set sendToOverflow flag to start sending items to overflow.
        //           Remove child and any other items beyond it from InternalChildren.
        //   If we are sending items to overflow...
        //     If child is not already in the overflow panel at overflowIndex, insert it.
        //     Increment overflowIndex.
        // Set HasOverflowItems appropriately & invalidate the overflow panel's measure if necessary.
        protected override Size MeasureOverride(Size availableSize)
        {
            Size panelDesiredSize = new Size();
            double maxExtent = availableSize.Width;
            bool sendToOverflow = false;
            UIElementCollection children = InternalChildren;
            List<UIElement> generatedItems = GeneratedChildren;
            int overflowIndex = 0;
            RibbonQuickAccessToolBarOverflowPanel overflowPanel = QAT == null ? null : QAT.OverflowPanel;

            for (int i = 0; i < generatedItems.Count; i++)
            {
                UIElement generatedChild = generatedItems[i];
                Debug.Assert(generatedChild != null);

                if (!sendToOverflow)
                {
                    if (i >= children.Count ||
                        !object.ReferenceEquals(children[i], generatedChild))
                    {
                        if (overflowPanel != null &&
                            overflowPanel.Children.Count > 0 &&
                            object.ReferenceEquals(overflowPanel.Children[0], generatedChild))
                        {
                            overflowPanel.Children.RemoveAt(0);
                            RibbonQuickAccessToolBar.SetIsOverflowItem(generatedChild, false);
                        }

                        base.InsertInternalChild(i, generatedChild);                        
                    }
                    
                    Size infinity = new Size(Double.PositiveInfinity, availableSize.Height);
                    generatedChild.Measure(infinity);
                    Size childDesiredSize = generatedChild.DesiredSize;

                    double newExtent = panelDesiredSize.Width + childDesiredSize.Width;

                    if (DoubleUtil.GreaterThan(newExtent, maxExtent))
                    {
                        // It doesn't fit, send to overflow
                        sendToOverflow = true;
                        base.RemoveInternalChildRange(i, children.Count - i);
                    }
                    else
                    {
                        panelDesiredSize.Width = newExtent;
                        panelDesiredSize.Height = Math.Max(panelDesiredSize.Height, childDesiredSize.Height);
                    }
                }

                if (sendToOverflow)
                {
                    Debug.Assert(overflowPanel.Children.Count >= overflowIndex, "overflowIndex should never get above overflowPanel.Children.Count");

                    if (overflowPanel != null &&
                        overflowPanel.Children.Count == overflowIndex ||
                        !object.ReferenceEquals(overflowPanel.Children[overflowIndex], generatedChild))
                    {
                        overflowPanel.Children.Insert(overflowIndex, generatedChild);
                    }

                    RibbonQuickAccessToolBar.SetIsOverflowItem(generatedChild, true);
                    overflowIndex++;
                }
            }

            // If necessary, set HasOverflowItems and invalidate the overflow panel's measure.
            if (QAT != null)
            {
                if (QAT.HasOverflowItems != sendToOverflow)
                {
                    QAT.HasOverflowItems = sendToOverflow;

                    // HasOverflowItems has changed, but the Grid that holds OverflowButtonHost does not automatically remeasure since it is already in its measure pass.
                    // Therefore, we must dispatch a call to remeasure that Grid after this current pass has completed.
                    Dispatcher.BeginInvoke((Action)delegate()
                        {
                            UIElement parent = VisualTreeHelper.GetParent(this) as UIElement;
                            if (parent != null)
                            {
                                parent.InvalidateMeasure();
                            }
                        },
                        DispatcherPriority.Normal,
                        null);
                }

                if (sendToOverflow &&
                    QAT.OverflowPanel != null)
                {
                    QAT.OverflowPanel.InvalidateMeasure();
                }
            }

#if DEBUG
            ValidateMeasure();
#endif

            return panelDesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            UIElementCollection children = InternalChildren;
            Rect rcChild = new Rect(finalSize);
            double previousChildSize = 0.0d;

            for (int i = 0, count = children.Count; i < count; ++i)
            {
                UIElement child = (UIElement)children[i];

                rcChild.X += previousChildSize;
                previousChildSize = child.DesiredSize.Width;
                rcChild.Width = previousChildSize;
                rcChild.Height = Math.Max(finalSize.Height, child.DesiredSize.Height);

                child.Arrange(rcChild);
            }

            return finalSize;
        }

        #endregion Measure & Arrange

        #region Helpers

        private RibbonQuickAccessToolBar QAT
        {
            get { return TemplatedParent as RibbonQuickAccessToolBar; }
        }

#if DEBUG
        private void ValidateMeasure()
        {
            // validation code

            if (QAT.HasOverflowItems)
            {
                Debug.Assert(GeneratedChildren.Count > InternalChildren.Count);
                int numItemsExpectedInOverflowPanel = GeneratedChildren.Count - InternalChildren.Count;
                Debug.Assert(QAT.OverflowPanel.Children.Count == numItemsExpectedInOverflowPanel);
            }

            // MainPanel
            for (int j = 0; j < InternalChildren.Count; j++)
            {
                Debug.Assert(object.ReferenceEquals(InternalChildren[j], GeneratedChildren[j]));
                UIElement currentChild = InternalChildren[j];
                Debug.Assert(currentChild != null);
                Debug.Assert(RibbonQuickAccessToolBar.GetIsOverflowItem(currentChild) == false);
                Debug.Assert(this.Children.Contains(currentChild) == true);
                Debug.Assert(QAT.OverflowPanel.Children.Contains(currentChild) == false);
                Debug.Assert(currentChild.IsVisible == this.IsVisible);
                Debug.Assert(currentChild.DesiredSize.Width > 0.0);
            }

            // OverflowPanel
            for (int k = InternalChildren.Count; k < GeneratedChildren.Count; k++)
            {
                int overflowPanelIndex = k - InternalChildren.Count;
                Debug.Assert(object.ReferenceEquals(QAT.OverflowPanel.Children[overflowPanelIndex], GeneratedChildren[k]));
                UIElement currentChild = GeneratedChildren[k];
                Debug.Assert(currentChild != null);
                Debug.Assert(RibbonQuickAccessToolBar.GetIsOverflowItem(currentChild) == true);
                Debug.Assert(this.Children.Contains(currentChild) == false);
                Debug.Assert(QAT.OverflowPanel.Children.Contains(currentChild) == true);
            }
        }
#endif

        #endregion Helpers

    }
}

