// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon.Primitives
#else
namespace Microsoft.Windows.Controls.Ribbon.Primitives
#endif
{
    #region Using declarations

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Threading;
    using MS.Internal;
    using Microsoft.Windows.Controls;

    #endregion

    /// <summary>
    ///   The panel that contains the RibbonGroups of a RibbonTab.
    /// </summary>
    public class RibbonGroupsPanel : StackPanel, ISupportStarLayout
    {
        #region Fields

        private double _cachedRemainingSpace; // A cached copy of the remaining space from the previous layout pass.
        WeakHashSet<IProvideStarLayoutInfo> _registeredStarLayoutProviders = new WeakHashSet<IProvideStarLayoutInfo>();
        double _nextGroupIncreaseWidth = double.NaN;
        int _cachedChildCount = 0;
        WeakDictionary<RibbonGroup, double> _changedWidthGroups = new WeakDictionary<RibbonGroup, double>();
        bool _processGroupWidthChangeQueued = false;

        #endregion

        #region Constructors

        /// <summary>
        ///   Initializes static members of the RibbonGroupsContainer class.
        /// </summary>
        static RibbonGroupsPanel()
        {
            OrientationProperty.OverrideMetadata(typeof(RibbonGroupsPanel), new FrameworkPropertyMetadata(Orientation.Horizontal, null, new CoerceValueCallback(CoerceOrientation)));
        }

        #endregion

        #region ISupportStarLayout Members

        public void RegisterStarLayoutProvider(IProvideStarLayoutInfoBase starLayoutInfoProvider)
        {
            if (starLayoutInfoProvider == null)
            {
                throw new ArgumentNullException("starLayoutInfoProvider");
            }
            IProvideStarLayoutInfo provider = starLayoutInfoProvider as IProvideStarLayoutInfo;
            if (provider == null)
            {
                throw new ArgumentException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.RibbonGroupsPanel_InvalidRegistrationParameter), "starLayoutInfoProvider");
            }
            if (!_registeredStarLayoutProviders.Contains(provider))
            {
                _registeredStarLayoutProviders.Add(provider);
                InvalidateMeasure();
            }
        }

        public void UnregisterStarLayoutProvider(IProvideStarLayoutInfoBase starLayoutInfoProvider)
        {
            if (starLayoutInfoProvider == null)
            {
                throw new ArgumentNullException("starLayoutInfoProvider");
            }
            IProvideStarLayoutInfo provider = starLayoutInfoProvider as IProvideStarLayoutInfo;
            if (provider == null)
            {
                throw new ArgumentException(Microsoft.Windows.Controls.SR.Get(Microsoft.Windows.Controls.SRID.RibbonGroupsPanel_InvalidRegistrationParameter), "starLayoutInfoProvider");
            }
            if (_registeredStarLayoutProviders.Contains(provider))
            {
                _registeredStarLayoutProviders.Remove(provider);
                InvalidateMeasure();
            }
        }

        #endregion

        #region Internal Methods

        internal void InvalidateCachedMeasure()
        {
            _cachedRemainingSpace = 0;
            InvalidateMeasure();
        }

        internal void OnChildGroupRenderSizeChanged(RibbonGroup group, double originalWidth)
        {
            if (!_changedWidthGroups.ContainsKey(group))
            {
                _changedWidthGroups[group] = originalWidth;
            }
            if (!_processGroupWidthChangeQueued)
            {
                _processGroupWidthChangeQueued = true;
                Dispatcher.BeginInvoke(
                    (Action)delegate()
                    {
                        if (_changedWidthGroups.Count > 0)
                        {
                            foreach (RibbonGroup invalidGroup in _changedWidthGroups.Keys)
                            {
                                double originalRenderWidth = _changedWidthGroups[invalidGroup];
                                if (!DoubleUtil.AreClose(originalRenderWidth, invalidGroup.ActualWidth))
                                {
                                    // Reset the next increase group's cached data
                                    // if there was a real change in any groups width.
                                    ResetNextIncreaseGroupCache();
                                    InvalidateMeasure();
                                    break;
                                }
                            }
                        }
                        _changedWidthGroups.Clear();
                        _processGroupWidthChangeQueued = false;
                    },
                    DispatcherPriority.Input,
                    null);
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        ///   Measures all of the RibbonGroups, and asks them to resize themselves appropriately
        ///   to fit within the available room in the GroupsContainer.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements.</param>
        /// <returns>
        ///   The size that the groups container determines it needs during layout, based
        ///   on its calculations of child element sizes.
        /// </returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            RibbonTab ribbonTab = ParentRibbonTab;

            double remainingSpace = 0;
            Size desiredSize = BasicMeasure(availableSize, out remainingSpace);
            UIElementCollection children = InternalChildren;
            if (children.Count != _cachedChildCount)
            {
                ResetNextIncreaseGroupCache();
                _cachedChildCount = children.Count;
            }

            if (DoubleUtil.GreaterThan(remainingSpace, 0))
            {
                // nudging the positive remaining space by a pixel
                // protects against aggregation of double errors and
                // provides a better appreance.
                remainingSpace = Math.Max(0, remainingSpace - 1);
            }

            if ((DoubleUtil.GreaterThanOrClose(_cachedRemainingSpace, 0) && (DoubleUtil.LessThan(remainingSpace, 0) || DoubleUtil.GreaterThan(remainingSpace, _cachedRemainingSpace))) ||
                (DoubleUtil.LessThan(_cachedRemainingSpace, 0) && DoubleUtil.GreaterThan(remainingSpace, 0)))
            {
                if (ribbonTab != null)
                {
                    double? lastPreIncreaseRemainingSpace = null;
                    while (DoubleUtil.GreaterThan(remainingSpace, 0))
                    {
                        // When there is remaining space try to give it
                        // to stars, which qualify to increase before 
                        // next group resize.
                        desiredSize = StarMeasure(availableSize,
                            desiredSize,
                            ribbonTab,
                            ref remainingSpace);

                        // When there is more remaining space, increase
                        // the size of the next group.
                        if (DoubleUtil.GreaterThan(remainingSpace, 0))
                        {
                            if (double.IsNaN(_nextGroupIncreaseWidth) ||
                                DoubleUtil.GreaterThanOrClose(remainingSpace, _nextGroupIncreaseWidth))
                            {
                                if (ribbonTab.IncreaseNextGroupSize())
                                {
                                    ResetNextIncreaseGroupCache();
                                    lastPreIncreaseRemainingSpace = remainingSpace;
                                    desiredSize = BasicMeasure(availableSize, out remainingSpace);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    double preDecreaseGroupRemainingSpace = remainingSpace;
                    while (DoubleUtil.LessThan(remainingSpace, 0))
                    {
                        // When the remaining space is negative decrease the
                        // next groups size.
                        if (ribbonTab.DecreaseNextGroupSize())
                        {
                            desiredSize = BasicMeasure(availableSize, out remainingSpace);
                            if (lastPreIncreaseRemainingSpace != null)
                            {
                                // Though in most of the cases we expect that the desired size of the
                                // entire subtree is up to date synchronously, there are some cases where desired 
                                // size of subtree gets updated asynchronously(eg. where bindings 
                                // impacting desired size resolve at a later time). To gaurd against infinite
                                // loops in such cases, if this decrease operation is to compensate any unnecessary
                                // increase operation performed above, then use the remaining space
                                // computed before such corresponding unnecessary increase operation, instead
                                // of the value computed post decrease which may be inaccurate due to some
                                // pending async updates. On the other hand if the group remains in
                                // same stage and when such async update happens at a later point of time,
                                // this measure would be executed again and hence fixing things.
                                remainingSpace = lastPreIncreaseRemainingSpace.Value;

                                // The remaining space needed to successfully perform the
                                // preceeding group increase without needing to do a compensating
                                // group decrease is cached to be used to optimize out the 
                                // unsuccessful group size increases on further increase
                                // in remaining space.
                                _nextGroupIncreaseWidth = lastPreIncreaseRemainingSpace.Value - preDecreaseGroupRemainingSpace;
                                lastPreIncreaseRemainingSpace = null;
                            }
                            else
                            {
                                ResetNextIncreaseGroupCache();
                            }
                            if (DoubleUtil.GreaterThan(remainingSpace, 0))
                            {
                                // Now if there is remaining space, give it to the
                                // qualified stars.
                                desiredSize = StarMeasure(availableSize,
                                    desiredSize,
                                    ribbonTab,
                                    ref remainingSpace);
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            else if (DoubleUtil.GreaterThan(remainingSpace, 0))
            {
                desiredSize = StarMeasure(availableSize,
                                desiredSize,
                                ribbonTab,
                                ref remainingSpace);
            }

            _cachedRemainingSpace = remainingSpace;

            //// Scroll if not enough space
            return desiredSize;
        }

        #endregion

        #region Properties

        public bool IsStarLayoutPass
        {
            get { return (bool)GetValue(IsStarLayoutPassProperty); }
            private set { SetValue(IsStarLayoutPassPropertyKey, value); }
        }

        // Using a DependencyProperty as the backing store for IsStarLayoutPass.  This enables animation, styling, binding, etc...
        private static readonly DependencyPropertyKey IsStarLayoutPassPropertyKey =
            DependencyProperty.RegisterReadOnly("IsStarLayoutPass", typeof(bool), typeof(RibbonGroupsPanel), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsStarLayoutPassProperty = IsStarLayoutPassPropertyKey.DependencyProperty;

        private RibbonTab ParentRibbonTab
        {
            get
            {
                FrameworkElement itemsPresenter = TemplatedParent as FrameworkElement;
                if (itemsPresenter != null)
                {
                    return itemsPresenter.TemplatedParent as RibbonTab;
                }

                return null;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///   Calculates the total width of all of the RibbonGroups in the RibbonTab.
        /// </summary>
        /// <returns>Returns the total width of all of the RibbonGroups in the RibbonTab.</returns>
        private double GetChildrenTotalWidth()
        {
            double result = 0;
            foreach (UIElement child in this.Children)
            {
                result += child.DesiredSize.Width;
            }

            return result;
        }

        /// <summary>
        ///     Calls OnInitializeLayout of star layout contract on
        ///     star data providers
        /// </summary>
        private void InitializeLayoutOnStars()
        {
            foreach (IProvideStarLayoutInfo starProvider in _registeredStarLayoutProviders)
            {
                starProvider.OnInitializeLayout();
            }
        }

        /// <summary>
        ///     A basic measure which doesnt deal with stars
        ///     (to be precise if there are any stars they get
        ///     the minimum possible space).
        /// </summary>
        private Size BasicMeasure(Size constraint, out double remainingSpace)
        {
            InitializeLayoutOnStars();
            Size desiredSize = base.MeasureOverride(constraint);
            remainingSpace = constraint.Width - GetChildrenTotalWidth();
            return desiredSize;
        }

        /// <summary>
        ///     A special pass for all the stars,
        ///     where remaining space is allocated appropriately
        /// </summary>
        private Size StarMeasure(Size constraint,
            Size originalDesiredSize,
            RibbonTab ribbonTab,
            ref double remainingSpace)
        {
            Size desiredSize = originalDesiredSize;
            RibbonGroup nextRibbonGroup = ribbonTab.GetNextIncreaseSizeGroup();
            double newRemainingSpace = AllocateStarValues(nextRibbonGroup, remainingSpace);

            if (!DoubleUtil.AreClose(remainingSpace, newRemainingSpace))
            {
                IsStarLayoutPass = true;
                InitializeLayoutOnStars();
                desiredSize = base.MeasureOverride(constraint);
                IsStarLayoutPass = false;
                remainingSpace = newRemainingSpace;
            }
            return desiredSize;
        }

        /// <summary>
        ///     Allocation algorithm for star values.
        /// </summary>
        private double AllocateStarValues(RibbonGroup ribbonGroup, double remainingSpace)
        {
            List<StarLayoutInfo> starInfoList = new List<StarLayoutInfo>(2);
            List<IProvideStarLayoutInfo> starLayoutInfoProviders = new List<IProvideStarLayoutInfo>(2);

            // creates a list of appropriate candidates for star allocation
            foreach (IProvideStarLayoutInfo starLayoutInfoProvider in _registeredStarLayoutProviders)
            {
                bool considerForAllocation = ((ribbonGroup == null && starLayoutInfoProvider.TargetElement is RibbonGroup) ||
                                              ribbonGroup == starLayoutInfoProvider.TargetElement);
                bool added = false;
                IEnumerable<StarLayoutInfo> starLayoutCombinations = starLayoutInfoProvider.StarLayoutCombinations;
                if (starLayoutCombinations != null)
                {
                    foreach (StarLayoutInfo starLayoutInfo in starLayoutCombinations)
                    {
                        if (starLayoutInfo != null && DoubleUtil.GreaterThan(starLayoutInfo.RequestedStarWeight, 0))
                        {
                            starLayoutInfo.AllocatedStarWidth = starLayoutInfo.RequestedStarMinWidth;
                            if (considerForAllocation)
                            {
                                added = true;
                                starInfoList.Add(starLayoutInfo);
                            }
                        }
                    }
                }
                if (added)
                {
                    starLayoutInfoProviders.Add(starLayoutInfoProvider);
                }
            }

            if (DoubleUtil.GreaterThan(remainingSpace, 0))
            {
                // Tries to equalize the perstarspace of star element
                // constrained by their min/max constraints and available space.
                starInfoList.Sort(StarLayoutInfo.PerStarValueComparer);
                int rightMostEqualizerIndex = -1;
                EqualizeStarValues(starInfoList, ref remainingSpace, out rightMostEqualizerIndex);

                // Distributes the remaining space after step 1 equally among all the
                // qualified member, such that they are constrained by their min/max constraints
                // maintaining (but not necessarily attaining) the goal of making perstarvalue of
                // all the elements as equal as possible.
                if (rightMostEqualizerIndex >= 0 && DoubleUtil.GreaterThan(remainingSpace, 0))
                {
                    starInfoList.Sort(0,
                        rightMostEqualizerIndex + 1,
                        StarLayoutInfo.PotentialPerStarValueComparer);
                    DistributeRemainingSpace(starInfoList, (rightMostEqualizerIndex + 1), ref remainingSpace);
                }
            }

            foreach (IProvideStarLayoutInfo starLayoutInfoProvider in starLayoutInfoProviders)
            {
                starLayoutInfoProvider.OnStarSizeAllocationCompleted();
            }

            return remainingSpace;
        }

        /// <summary>
        ///     Tries to equalize the perstarspace of star element
        ///     constrained by their min/max constraints and available space.
        /// </summary>
        private static void EqualizeStarValues(List<StarLayoutInfo> starInfoList,
            ref double remainingSpace,
            out int rightMostEqualizerIndex)
        {
            Debug.Assert(DoubleUtil.GreaterThan(remainingSpace, 0));
            rightMostEqualizerIndex = -1;
            int elementCount = starInfoList.Count;
            if (elementCount > 0)
            {
                if (DoubleUtil.LessThanOrClose(EqualizeLeftOf(starInfoList, elementCount - 1, true), remainingSpace))
                {
                    remainingSpace -= EqualizeLeftOf(starInfoList, elementCount - 1, false);
                    rightMostEqualizerIndex = (elementCount - 1);
                }
                else
                {
                    int startIndex = 0;
                    int endIndex = elementCount - 1;
                    while (true)
                    {
                        int currentIndex = (startIndex + endIndex) / 2;
                        if (currentIndex == rightMostEqualizerIndex)
                        {
                            break;
                        }
                        if (DoubleUtil.LessThanOrClose(EqualizeLeftOf(starInfoList, currentIndex, true), remainingSpace))
                        {
                            startIndex = rightMostEqualizerIndex = currentIndex;
                        }
                        else
                        {
                            endIndex = currentIndex;
                        }
                    }

                    remainingSpace -= EqualizeLeftOf(starInfoList, rightMostEqualizerIndex, false);
                }
            }
        }

        private static double EqualizeLeftOf(List<StarLayoutInfo> starInfoList, int index, bool isChecking)
        {
            Debug.Assert(index >= 0 && index < starInfoList.Count);
            double spaceNeeded = 0;
            StarLayoutInfo baseStarInfo = starInfoList[index];
            double basePerStar = baseStarInfo.AllocatedStarWidth / baseStarInfo.RequestedStarWeight;
            for (int i = 0; i < index; i++)
            {
                StarLayoutInfo starInfo = starInfoList[i];
                double targetValue = Math.Min(basePerStar * starInfo.RequestedStarWeight, starInfo.RequestedStarMaxWidth);
                spaceNeeded += (targetValue - starInfo.AllocatedStarWidth);
                if (!isChecking)
                {
                    starInfo.AllocatedStarWidth = targetValue;
                }
            }
            return spaceNeeded;
        }

        /// <summary>
        ///     Distributes the remaining space after step 1 equally among all the
        ///     qualified member, such that they are constrained by their min/max constraints
        ///     maintaining (but not necessarily attaining) the goal of making perstarvalue of
        ///     all the elements as equal as possible.
        /// </summary>
        private static void DistributeRemainingSpace(List<StarLayoutInfo> starInfoList,
            int distributionCount,
            ref double remainingSpace)
        {
            Debug.Assert(distributionCount > 0 && distributionCount <= starInfoList.Count);
            Debug.Assert(DoubleUtil.GreaterThan(remainingSpace, 0));

            double remainingStarWeight = 0;
            for (int i = 0; i < distributionCount; i++)
            {
                remainingStarWeight += starInfoList[i].RequestedStarWeight;
            }

            double impactPerStar = 0;
            for (int i = 0; i < distributionCount; i++)
            {
                StarLayoutInfo starInfo = starInfoList[i];
                if (DoubleUtil.GreaterThan(remainingSpace, 0))
                {
                    double currentContribution = starInfo.RequestedStarMaxWidth - starInfo.AllocatedStarWidth;
                    currentContribution -= (impactPerStar * starInfo.RequestedStarWeight);
                    currentContribution /= starInfo.RequestedStarWeight;
                    if (DoubleUtil.GreaterThan(currentContribution * remainingStarWeight, remainingSpace))
                    {
                        currentContribution = remainingSpace / remainingStarWeight;
                    }
                    impactPerStar += currentContribution;
                    remainingSpace -= (currentContribution * remainingStarWeight);
                }
                starInfo.AllocatedStarWidth += (impactPerStar * starInfo.RequestedStarWeight);
                remainingStarWeight -= starInfo.RequestedStarWeight;
            }
        }

        private static object CoerceOrientation(DependencyObject d, object baseValue)
        {
            return Orientation.Horizontal;
        }

        private void ResetNextIncreaseGroupCache()
        {
            _nextGroupIncreaseWidth = double.NaN;
        }

        #endregion
    }
}
