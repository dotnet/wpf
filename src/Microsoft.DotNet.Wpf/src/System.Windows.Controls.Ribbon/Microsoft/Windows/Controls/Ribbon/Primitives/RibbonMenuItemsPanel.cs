// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon.Primitives
#else
namespace Microsoft.Windows.Controls.Ribbon.Primitives
#endif
{
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Collections.Generic;
    using System;
    using MS.Internal;
    using Microsoft.Windows.Controls;

    public class RibbonMenuItemsPanel : VirtualizingStackPanel, ISupportStarLayout
    {
        #region Constructor

        static RibbonMenuItemsPanel()
        {
            OrientationProperty.OverrideMetadata(typeof(RibbonMenuItemsPanel), new FrameworkPropertyMetadata(Orientation.Vertical));
        }

        #endregion

        #region ISupportStarLayout Members

        public void RegisterStarLayoutProvider(IProvideStarLayoutInfoBase starLayoutInfoProvider)
        {
            if (starLayoutInfoProvider == null)
            {
                throw new ArgumentNullException("starLayoutInfoProvider");
            }
            if (!_registeredStarLayoutProviders.Contains(starLayoutInfoProvider))
            {
                _registeredStarLayoutProviders.Add(starLayoutInfoProvider);
                InvalidateMeasure();
            }
        }

        public void UnregisterStarLayoutProvider(IProvideStarLayoutInfoBase starLayoutInfoProvider)
        {
            if (starLayoutInfoProvider == null)
            {
                throw new ArgumentNullException("starLayoutInfoProvider");
            }
            if (_registeredStarLayoutProviders.Contains(starLayoutInfoProvider))
            {
                _registeredStarLayoutProviders.Remove(starLayoutInfoProvider);
                InvalidateMeasure();
            }
        }

        public bool IsStarLayoutPass
        {
            get { return _isStarLayout; }
        }

        #endregion
        
        #region Protected Methods

        protected override void OnIsItemsHostChanged(bool oldIsItemsHost, bool newIsItemsHost)
        {
            base.OnIsItemsHostChanged(oldIsItemsHost, newIsItemsHost);

            ItemsControl itemsControl = ParentItemsControl;
            RibbonMenuButton menuButtonParent = itemsControl as RibbonMenuButton;
            RibbonMenuItem menuItemParent = itemsControl as RibbonMenuItem;

            // ParentItemsControl should be either RibbonMenuButton or RibbonMenuItem
            if (menuButtonParent != null || menuItemParent != null)
            {
                if (newIsItemsHost)
                {
                    IItemContainerGenerator generator = itemsControl.ItemContainerGenerator as IItemContainerGenerator;
                    if (generator != null && generator.GetItemContainerGeneratorForPanel(this) == generator)
                    {
                        if (menuButtonParent != null)
                        {
                            menuButtonParent.InternalItemsHost = this;
                        }
                        else if (menuItemParent != null)
                        {
                            menuItemParent.InternalItemsHost = this;
                        }
                    }
                }
                else
                {
                    if (menuButtonParent != null && menuButtonParent.InternalItemsHost == this)
                    {
                        menuButtonParent.InternalItemsHost = null;
                    }
                    else if (menuItemParent != null && menuItemParent.InternalItemsHost == this)
                    {
                        menuItemParent.InternalItemsHost = null;
                    }
                }
            }
        }


        protected override Size MeasureOverride(Size availableSize)
        {
            InitializeLayoutOnStars();
            Size baseDesiredSize = base.MeasureOverride(availableSize);

            // If there are no starLayoutProviders then return desiredSize calculated by VSP.
            if (_registeredStarLayoutProviders.Count == 0)
            {
                return baseDesiredSize;
            }

            Size desiredSize = new Size();
            double maxChildWidth = 0.0, totalChildHeight = 0.0;

            // First pass: All children are Auto sized by the previous Measure
            // Gallery understands AutoLayout pass 
            // and its DesiredSize is just the width required to display its MinColumnCount
            // and MinHeight required to display atleast 1 row
            foreach (UIElement child in InternalChildren)
            {
                if (child.DesiredSize.Width > maxChildWidth)
                {
                    maxChildWidth = child.DesiredSize.Width;
                }
                totalChildHeight += child.DesiredSize.Height;
            }
            
            desiredSize.Width = maxChildWidth;
            desiredSize.Height = totalChildHeight;
            
            // Cache the MinSize computed in the Auto-pass. 
            // this value is consumed by resizing logic to prevent resizing less than the MinSize.
            if (double.IsInfinity(availableSize.Width))
            {
                _cachedAutoSize = new Size(maxChildWidth, totalChildHeight);
            }
            else
            {
                // If not infinity then stretch to all the available width 
                maxChildWidth = Math.Max(availableSize.Width, maxChildWidth);
            }

            // 2nd pass: StarLayout pass
            // Iterate through registered StarLayoutProviders (aka galleries) 
            // and remeasure them with maxWidth,remainingHeight calculated in 1st pass.
            _isStarLayout = true;
            double remainingHeight = availableSize.Height - totalChildHeight;
            InitializeLayoutOnStars();
            if (_registeredStarLayoutProviders.Count > 0)
            {
                // Divide remaining height equally among all galleries. Even if there 
                // is no remaining space it is important to rerun the star layout pass 
                // so that the ScrollBars for the galleries can be enabled. Otherwise 
                // that wont happen because the galleries were measured to infinity 
                // in the auto pass.

                double surplusHeight = Math.Max(remainingHeight, 0.0) / _registeredStarLayoutProviders.Count;
                List<IProvideStarLayoutInfoBase> InvalidateRegisteredLayoutProvidersList = new List<IProvideStarLayoutInfoBase>(); ;
                foreach (IProvideStarLayoutInfoBase starLayoutInfoProvider in _registeredStarLayoutProviders)
                {
                    UIElement starLayoutTarget = starLayoutInfoProvider.TargetElement;
                    if (!InternalChildren.Contains(starLayoutTarget))
                    {
                        InvalidateRegisteredLayoutProvidersList.Add(starLayoutInfoProvider);
                        starLayoutTarget = null;
                    }
                    if (starLayoutTarget != null)
                    {
                        // Remeasure with surplusHeight added
                        desiredSize.Height -= starLayoutTarget.DesiredSize.Height;
                        double availableHeight = starLayoutTarget.DesiredSize.Height + surplusHeight;
                        starLayoutTarget.Measure(new Size(maxChildWidth, availableHeight));

                        desiredSize.Width = Math.Max(starLayoutTarget.DesiredSize.Width, desiredSize.Width);
                        desiredSize.Height += starLayoutTarget.DesiredSize.Height;
                    }
                }
                // Invalidating _registeredStarLayoutProviders as some star provider Targets children has been removed from the Panel
                if (InvalidateRegisteredLayoutProvidersList.Count > 0)
                {
                    for (int i = 0; i < InvalidateRegisteredLayoutProvidersList.Count; i++)
                    {
                        if (_registeredStarLayoutProviders.Contains(InvalidateRegisteredLayoutProvidersList[i]))
                        {
                            UnregisterStarLayoutProvider(InvalidateRegisteredLayoutProvidersList[i]);
                        }
                    }
                }
            }

            _isStarLayout = false;
            return desiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_registeredStarLayoutProviders.Count == 0)
            {
                return base.ArrangeOverride(finalSize);
            }

            double totalDesiredHeight = 0.0;
            double remainingHeight = 0.0, surplusHeight = 0.0;
            UIElementCollection children = InternalChildren;

            // First pass: Sum up each child's DesiredSize and calculate the surplus Height
            for (int i = 0; i < children.Count; i++)
            {
                totalDesiredHeight += children[i].DesiredSize.Height;
            }

            // Divide remainingHeight equally among starLayoutProviders
            remainingHeight = finalSize.Height - totalDesiredHeight;
            HashSet<UIElement> starLayoutTargets = GetStarLayoutProviderTargets();
            if (DoubleUtil.GreaterThan(remainingHeight, 0.0) && starLayoutTargets.Count > 0)
            {
                surplusHeight = remainingHeight / starLayoutTargets.Count;
            }

            // Second pass. Arrange each child
            double startY = 0.0;
            for (int i = 0; i < children.Count; i++)
            {
                UIElement child = children[i];
                if (starLayoutTargets.Contains(child))
                {
                    // if the child is a StarLayoutProvider, give it the surplusHeight.
                    double availableHeight = child.DesiredSize.Height + surplusHeight;
                    child.Arrange(new Rect(0, startY, finalSize.Width, availableHeight));
                    startY += availableHeight;
                }
                else
                {
                    child.Arrange(new Rect(0.0, startY, finalSize.Width, child.DesiredSize.Height));
                    startY += child.DesiredSize.Height;
                }
            }
            return finalSize;
        }

        /// <summary>
        ///     Calls OnInitializeLayout on each of the registered StarLayoutProviders
        /// </summary>
        private void InitializeLayoutOnStars()
        {
            foreach (IProvideStarLayoutInfoBase starProvider in _registeredStarLayoutProviders)
            {
                starProvider.OnInitializeLayout();
            }
        }

        #endregion

        #region Internal Methods

        internal void BringIndexIntoViewInternal(int index)
        {
            base.BringIndexIntoView(index);
        }

        private HashSet<UIElement> GetStarLayoutProviderTargets()
        {
            HashSet<UIElement> targets = new HashSet<UIElement>();

            foreach (IProvideStarLayoutInfoBase starProvider in _registeredStarLayoutProviders)
            {
                UIElement starLayoutTarget = starProvider.TargetElement;
                if (starLayoutTarget != null)
                {
                    targets.Add(starLayoutTarget);
                }
            }

            return targets;
        }

        #endregion

#if IN_RIBBON_GALLERY
        #region InRibbonGallery

        internal void RemoveFirstGallery(InRibbonGallery irg)
        {
            for (int i = 0; i < InternalChildren.Count; i++)
            {
                if (InternalChildren[i].Equals(irg.FirstGallery))
                {
                    Debug.Assert(_firstRibbonGalleryReInsertIndex == -1);
                    RemoveInternalChildRange(i, 1);
                    _firstRibbonGalleryReInsertIndex = i;
                    break;
                }
            }
        }

        internal void ReInsertFirstGallery(InRibbonGallery irg)
        {
            RibbonGallery firstGallery = irg.FirstGallery;
            if (firstGallery != null &&
                _firstRibbonGalleryReInsertIndex >= 0)
            {
                Debug.Assert(_firstRibbonGalleryReInsertIndex <= InternalChildren.Count);

                InsertInternalChild(_firstRibbonGalleryReInsertIndex, firstGallery);
                _firstRibbonGalleryReInsertIndex = -1;
            }
        }

        // Used in the InRibbonGallery scenario to remember the index of a plucked RibbonGallery.
        private int _firstRibbonGalleryReInsertIndex = -1;

        #endregion
#endif

        /// <summary>
        ///     The parent ItemsControl
        /// </summary>
        private ItemsControl ParentItemsControl
        {
            get
            {
                return TreeHelper.FindTemplatedAncestor<ItemsControl>(this);
            }
        }

        internal Size CachedAutoSize
        {
            get
            {
                return _cachedAutoSize;
            }
        }

        private bool _isStarLayout;
        private WeakHashSet<IProvideStarLayoutInfoBase> _registeredStarLayoutProviders = new WeakHashSet<IProvideStarLayoutInfoBase>();
        private Size _cachedAutoSize = new Size();
    }
}
