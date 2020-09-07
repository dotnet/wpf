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
    /// This class is used as part of a QuaternionKeyFrameCollection in
    /// conjunction with a KeyFrameQuaternionAnimation to animate a
    /// Quaternion property value along a set of key frames.
    ///
    /// This QuaternionKeyFrame interpolates the between the Quaternion Value of
    /// the previous key frame and its own Value linearly to produce its output value.
    /// </summary>
    public partial class LinearQuaternionKeyFrame : QuaternionKeyFrame
    {
        /// <summary>
        /// UseShortestPath Property
        /// </summary>
        public static readonly DependencyProperty UseShortestPathProperty =
            DependencyProperty.Register(
                    "UseShortestPath",
                    typeof(bool),
                    typeof(LinearQuaternionKeyFrame),
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
