// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

using System.Collections;
using System.Collections.Generic;
using System.Windows;
using MS.Internal;

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon.Primitives
#else
namespace Microsoft.Windows.Controls.Ribbon.Primitives
#endif
{
    public class StarLayoutInfo : DependencyObject
    {
        #region Properties

        public double RequestedStarWeight
        {
            get { return (double)GetValue(RequestedStarWeightProperty); }
            set { SetValue(RequestedStarWeightProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RequestedStarWeight.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RequestedStarWeightProperty =
            DependencyProperty.Register("RequestedStarWeight",
                typeof(double),
                typeof(StarLayoutInfo),
                new UIPropertyMetadata(0.0),
                new ValidateValueCallback(ValidateRequestedStarWeight));

        public double RequestedStarMinWidth
        {
            get { return (double)GetValue(RequestedStarMinWidthProperty); }
            set { SetValue(RequestedStarMinWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RequestedStarMinWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RequestedStarMinWidthProperty =
            DependencyProperty.Register("RequestedStarMinWidth",
            typeof(double),
            typeof(StarLayoutInfo),
            new UIPropertyMetadata(0.0, new PropertyChangedCallback(OnRequestedStarMinWidthChanged)),
            new ValidateValueCallback(ValidateRequestedStarMinWidth));

        public double RequestedStarMaxWidth
        {
            get { return (double)GetValue(RequestedStarMaxWidthProperty); }
            set { SetValue(RequestedStarMaxWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RequestedStarMaxWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RequestedStarMaxWidthProperty =
            DependencyProperty.Register("RequestedStarMaxWidth",
                typeof(double),
                typeof(StarLayoutInfo),
                new UIPropertyMetadata(double.PositiveInfinity, null, new CoerceValueCallback(OnCoerceRequestedStarMaxWidth)),
                new ValidateValueCallback(ValidateRequestedStarMaxWidth));

        public double AllocatedStarWidth
        {
            get { return (double)GetValue(AllocatedStarWidthProperty); }
            set { SetValue(AllocatedStarWidthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AllocatedStarWidth.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AllocatedStarWidthProperty =
            DependencyProperty.Register("AllocatedStarWidth",
                typeof(double),
                typeof(StarLayoutInfo),
                new UIPropertyMetadata(0.0, null, new CoerceValueCallback(OnCoerceAllocatedStarWidth)));

        internal static IComparer<StarLayoutInfo> PerStarValueComparer
        {
            get
            {
                if (_perStarValueComparer == null)
                {
                    _perStarValueComparer = new PerStarComparerImpl();
                }
                return _perStarValueComparer;
            }
        }

        internal static IComparer<StarLayoutInfo> PotentialPerStarValueComparer
        {
            get
            {
                if (_potentialPerStarValueComparer == null)
                {
                    _potentialPerStarValueComparer = new PotentialPerStarComparerImpl();
                }
                return _potentialPerStarValueComparer;
            }
        }

        #endregion

        #region Private Methods

        private static bool ValidateRequestedStarWeight(object value)
        {
            double starWeight = (double)value;
            return (!double.IsNaN(starWeight) &&
                !double.IsInfinity(starWeight) &&
                DoubleUtil.GreaterThanOrClose(starWeight, 0.0));
        }

        private static void OnRequestedStarMinWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(RequestedStarMaxWidthProperty);
            d.CoerceValue(AllocatedStarWidthProperty);
        }

        private static bool ValidateRequestedStarMinWidth(object value)
        {
            double starMinWidth = (double)value;
            return (!double.IsNaN(starMinWidth) &&
                !double.IsInfinity(starMinWidth) &&
                DoubleUtil.GreaterThanOrClose(starMinWidth, 0.0));
        }

        private static object OnCoerceRequestedStarMaxWidth(DependencyObject d, object baseValue)
        {
            double maxWidth = (double)baseValue;
            StarLayoutInfo starLayoutInfo = (StarLayoutInfo)d;
            double minWidth = starLayoutInfo.RequestedStarMinWidth;
            if (DoubleUtil.LessThan(maxWidth, minWidth))
            {
                return minWidth;
            }
            return baseValue;
        }

        private static bool ValidateRequestedStarMaxWidth(object value)
        {
            double starMaxWidth = (double)value;
            return (!double.IsNaN(starMaxWidth) &&
                DoubleUtil.GreaterThan(starMaxWidth, 0.0));
        }

        private static object OnCoerceAllocatedStarWidth(DependencyObject d, object baseValue)
        {
            double allocatedWidth = (double)baseValue;
            StarLayoutInfo layoutInfo = (StarLayoutInfo)d;
            if (DoubleUtil.LessThan(allocatedWidth, layoutInfo.RequestedStarMinWidth))
            {
                return layoutInfo.RequestedStarMinWidth;
            }
            return baseValue;
        }

        #endregion

        #region Comparers

        private class PerStarComparerImpl : IComparer<StarLayoutInfo>
        {
            #region IComparer<StarLayoutInfo> Members

            public int Compare(StarLayoutInfo x, StarLayoutInfo y)
            {
                return Comparer.Default.Compare(x.AllocatedStarWidth / x.RequestedStarWeight,
                                                y.AllocatedStarWidth / y.RequestedStarWeight);
            }

            #endregion
        }

        private class PotentialPerStarComparerImpl : IComparer<StarLayoutInfo>
        {
            #region IComparer<StarLayoutInfo> Members

            public int Compare(StarLayoutInfo x, StarLayoutInfo y)
            {
                return Comparer.Default.Compare((x.RequestedStarMaxWidth - x.AllocatedStarWidth) / x.RequestedStarWeight,
                                                (y.RequestedStarMaxWidth - y.AllocatedStarWidth) / y.RequestedStarWeight);
            }

            #endregion
        }

        #endregion

        #region Private Data

        private static PerStarComparerImpl _perStarValueComparer;
        private static PotentialPerStarComparerImpl _potentialPerStarValueComparer;

        #endregion
    }
}