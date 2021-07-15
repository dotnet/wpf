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
    /// This ThicknessKeyFrame interpolates between the Thickness Value of
    /// the previous key frame and its own Value to produce its output value.
    /// </summary>
    public partial class SplineThicknessKeyFrame : ThicknessKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new SplineThicknessKeyFrame.
        /// </summary>
        public SplineThicknessKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new SplineThicknessKeyFrame.
        /// </summary>
        public SplineThicknessKeyFrame(Thickness value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new SplineThicknessKeyFrame.
        /// </summary>
        public SplineThicknessKeyFrame(Thickness value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new SplineThicknessKeyFrame.
        /// </summary>
        public SplineThicknessKeyFrame(Thickness value, KeyTime keyTime, KeySpline keySpline)
            : this()
        {
            if (keySpline == null)
            {
                throw new ArgumentNullException("keySpline");
            }

            Value = value;
            KeyTime = keyTime;
            KeySpline = keySpline;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new  SplineThicknessKeyFrame();
        }

        #endregion

        #region ThicknessKeyFrame

        /// <summary>
        /// Implemented to linearly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Thickness InterpolateValueCore(Thickness baseValue, double keyFrameProgress)
        {
            if (keyFrameProgress == 0.0)
            {
                return baseValue;
            }
            else if (keyFrameProgress == 1.0)
            {
                return Value;
            }
            else
            {
                double splineProgress = KeySpline.GetSplineProgress(keyFrameProgress);

                return AnimatedTypeHelpers.InterpolateThickness(baseValue, Value, splineProgress);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// KeySpline Property
        /// </summary>
        public static readonly DependencyProperty KeySplineProperty =
            DependencyProperty.Register(
                "KeySpline",
                typeof(KeySpline),
                typeof(SplineThicknessKeyFrame),
                new PropertyMetadata(new KeySpline()));

        /// <summary>
        /// The KeySpline defines the way that progress will be altered for this
        /// key frame.
        /// </summary>
        public KeySpline KeySpline
        {
            get
            {
                return (KeySpline)GetValue(KeySplineProperty);
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                SetValue(KeySplineProperty, value);
            }
        }

        #endregion
    }
}