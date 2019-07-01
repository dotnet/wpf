// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Microsoft.Test.Graphics.Factories
{
    /// <summary>
    /// Make Hit Test Filter Behavior Callbacks
    /// </summary>
    public class HitTestFilterFactory
    {
        /// <summary>
        /// This summary has not been prepared yet. NOSUMMARY - pantal07
        /// </summary>
        public static HitTestFilterCallback MakeFilter(string filter)
        {
            switch (filter)
            {
                case "SkipNone":
                    return SkipNone;

                case "SkipChildren":
                    return SkipChildren;

                case "SkipSelf":
                    return SkipSelf;

                case "SkipSelfAndChildren":
                    return SkipSelfAndChildren;

                case "Stop":
                    return Stop;

                case "AsMarked":
                    return AsMarked;
            }
            throw new ArgumentException("Specified hit test filter (" + filter + ") cannot be created");
        }

        private static HitTestFilterCallback SkipNone
        {
            get
            {
                return new HitTestFilterCallback(SkipNoneCallback);
            }
        }

        private static HitTestFilterCallback SkipChildren
        {
            get
            {
                return new HitTestFilterCallback(SkipChildrenCallback);
            }
        }

        private static HitTestFilterCallback SkipSelf
        {
            get
            {
                return new HitTestFilterCallback(SkipSelfCallback);
            }
        }

        private static HitTestFilterCallback SkipSelfAndChildren
        {
            get
            {
                return new HitTestFilterCallback(SkipSelfAndChildrenCallback);
            }
        }

        private static HitTestFilterCallback Stop
        {
            get
            {
                return new HitTestFilterCallback(StopCallback);
            }
        }

        private static HitTestFilterCallback AsMarked
        {
            get
            {
                return new HitTestFilterCallback(AsMarkedCallback);
            }
        }

        private static HitTestFilterBehavior SkipNoneCallback(DependencyObject target)
        {
            target.SetValue(Const.LookedAtProperty, true);

            // Clear the skip property so we don't confuse the hit testing verification
            target.SetValue(Const.SkipProperty, string.Empty);

            return HitTestFilterBehavior.Continue;
        }

        private static HitTestFilterBehavior SkipChildrenCallback(DependencyObject target)
        {
            target.SetValue(Const.LookedAtProperty, true);

            string skip = (string)target.GetValue(Const.SkipProperty);
            if (skip == "SkipChildren")
            {
                return HitTestFilterBehavior.ContinueSkipChildren;
            }

            // Clear the skip property so we don't confuse the hit testing verification
            target.SetValue(Const.SkipProperty, string.Empty);

            return HitTestFilterBehavior.Continue;
        }

        private static HitTestFilterBehavior SkipSelfCallback(DependencyObject target)
        {
            target.SetValue(Const.LookedAtProperty, true);

            string skip = (string)target.GetValue(Const.SkipProperty);
            if (skip == "SkipSelf")
            {
                target.SetValue(Const.LookedAtProperty, false);
                return HitTestFilterBehavior.ContinueSkipSelf;
            }

            // Clear the skip property so we don't confuse the hit testing verification
            target.SetValue(Const.SkipProperty, string.Empty);

            return HitTestFilterBehavior.Continue;
        }

        private static HitTestFilterBehavior SkipSelfAndChildrenCallback(DependencyObject target)
        {
            target.SetValue(Const.LookedAtProperty, true);

            string skip = (string)target.GetValue(Const.SkipProperty);
            if (skip == "SkipSelfAndChildren")
            {
                target.SetValue(Const.LookedAtProperty, false);
                return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
            }

            // Clear the skip property so we don't confuse the hit testing verification
            target.SetValue(Const.SkipProperty, string.Empty);

            return HitTestFilterBehavior.Continue;
        }

        private static HitTestFilterBehavior StopCallback(DependencyObject target)
        {
            target.SetValue(Const.LookedAtProperty, true);

            string skip = (string)target.GetValue(Const.SkipProperty);
            if (skip == "Stop")
            {
                target.SetValue(Const.LookedAtProperty, false);
                return HitTestFilterBehavior.Stop;
            }

            // Clear the skip property so we don't confuse the hit testing verification
            target.SetValue(Const.SkipProperty, string.Empty);

            return HitTestFilterBehavior.Continue;
        }

        private static HitTestFilterBehavior AsMarkedCallback(DependencyObject target)
        {
            target.SetValue(Const.LookedAtProperty, true);

            string skip = (string)target.GetValue(Const.SkipProperty);
            switch (skip)
            {
                case "SkipChildren":
                    return HitTestFilterBehavior.ContinueSkipChildren;

                case "SkipSelf":
                    target.SetValue(Const.LookedAtProperty, false);
                    return HitTestFilterBehavior.ContinueSkipSelf;

                case "SkipSelfAndChildren":
                    target.SetValue(Const.LookedAtProperty, false);
                    return HitTestFilterBehavior.ContinueSkipSelfAndChildren;

                case "Stop":
                    target.SetValue(Const.LookedAtProperty, false);
                    return HitTestFilterBehavior.Stop;
            }

            // Clear the skip property so we don't confuse the hit testing verification
            target.SetValue(Const.SkipProperty, string.Empty);

            return HitTestFilterBehavior.Continue;
        }
    }
}
