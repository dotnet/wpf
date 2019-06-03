// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using MS.Internal;

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using MS.Internal.PresentationFramework;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// This class is used as part of a ThicknessKeyFrameCollection in
    /// conjunction with a KeyFrameThicknessAnimation to animate a
    /// Thickness property value along a set of key frames.
    ///
    /// This ThicknessKeyFrame changes from the Thickness Value of
    /// the previous key frame to its own Value without interpolation.  The
    /// change occurs at the KeyTime.
    /// </summary>
    public class DiscreteThicknessKeyFrame : ThicknessKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new DiscreteThicknessKeyFrame.
        /// </summary>
        public DiscreteThicknessKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new DiscreteThicknessKeyFrame.
        /// </summary>
        public DiscreteThicknessKeyFrame(Thickness value)
            : base(value)
        {
        }

        /// <summary>
        /// Creates a new DiscreteThicknessKeyFrame.
        /// </summary>
        public DiscreteThicknessKeyFrame(Thickness value, KeyTime keyTime)
            : base(value, keyTime)
        {
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new DiscreteThicknessKeyFrame();
        }

        // We don't need to override CloneCore because it won't do anything

        #endregion

        #region ThicknessKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Thickness InterpolateValueCore(Thickness baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress < 1.0)
            {
                return baseValue;
            }
            else
            {
                return Value;
            }
        }

        #endregion
    }
}