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
    /// This ThicknessKeyFrame interpolates the between the Thickness Value of
    /// the previous key frame and its own Value Linearly with an EasingFunction to produce its output value.
    /// </summary>
    public partial class EasingThicknessKeyFrame : ThicknessKeyFrame
    {
        #region Constructors

        /// <summary>
        /// Creates a new EasingThicknessKeyFrame.
        /// </summary>
        public EasingThicknessKeyFrame()
            : base()
        {
        }

        /// <summary>
        /// Creates a new EasingThicknessKeyFrame.
        /// </summary>
        public EasingThicknessKeyFrame(Thickness value)
            : this()
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new EasingThicknessKeyFrame.
        /// </summary>
        public EasingThicknessKeyFrame(Thickness value, KeyTime keyTime)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
        }

        /// <summary>
        /// Creates a new EasingThicknessKeyFrame.
        /// </summary>
        public EasingThicknessKeyFrame(Thickness value, KeyTime keyTime, IEasingFunction easingFunction)
            : this()
        {
            Value = value;
            KeyTime = keyTime;
            EasingFunction = easingFunction;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new EasingThicknessKeyFrame();
        }

        #endregion

        #region ThicknessKeyFrame

        /// <summary>
        /// Implemented to Easingly interpolate between the baseValue and the
        /// Value of this KeyFrame using the keyFrameProgress.
        /// </summary>
        protected override Thickness InterpolateValueCore(Thickness baseValue, double keyFrameProgress)
        {
            IEasingFunction easingFunction = EasingFunction;
            if (easingFunction != null)
            {
                keyFrameProgress = easingFunction.Ease(keyFrameProgress);
            }

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
                return AnimatedTypeHelpers.InterpolateThickness(baseValue, Value, keyFrameProgress);
            }
        }

        #endregion

        #region Public Properties

            /// <summary>
            /// EasingFunctionProperty
            /// </summary>                                 
            public static readonly DependencyProperty EasingFunctionProperty =
                DependencyProperty.Register(
                    "EasingFunction",
                    typeof(IEasingFunction),
                    typeof(EasingThicknessKeyFrame));

            /// <summary>
            /// EasingFunction
            /// </summary>
            public IEasingFunction EasingFunction                
            {
                get
                {
                    return (IEasingFunction)GetValue(EasingFunctionProperty);
                }
                set
                {
                    SetValueInternal(EasingFunctionProperty, value);
                }
            }

        #endregion
    }
}