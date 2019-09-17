// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 

using System;
using System.Windows;
using MS.Internal.KnownBoxes;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// Animates the value of a bool property using linear interpolation
    /// between two values.  The values are determined by the combination of
    /// From, To, or By values that are set on the animation.
    /// </summary>
    public partial class QuaternionAnimation : QuaternionAnimationBase
    {
        /// <summary>
        /// UseShortestPath Property
        /// </summary>
        public static readonly DependencyProperty UseShortestPathProperty =
            DependencyProperty.Register(
                    "UseShortestPath",
                    typeof(bool),
                    typeof(QuaternionAnimation),
                    new PropertyMetadata(/* defaultValue = */ BooleanBoxes.TrueBox));

        /// <summary>
        /// If true, the animation will automatically flip the sign of the destination
        /// Quaternion to ensure the shortest path is taken.
        /// </summary>
        public bool UseShortestPath
        {
            get
            {
                return (bool) GetValue(UseShortestPathProperty);
            }
            set
            {
                SetValue(UseShortestPathProperty, value);
            }
        }
    }
}
