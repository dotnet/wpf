// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Diagnostics;
using System.Windows;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class AnimationTimeline : Timeline
    {
        /// <summary>
        /// 
        /// </summary>
        protected AnimationTimeline()
            : base()
        {
        }

        #region Dependency Properties

        private static void AnimationTimeline_PropertyChangedFunction(DependencyObject d, 
                                                                      DependencyPropertyChangedEventArgs e)
        {
            ((AnimationTimeline)d).PropertyChanged(e.Property);
        }

        /// <summary>
        ///
        /// </summary>
        public static readonly DependencyProperty IsAdditiveProperty =
            DependencyProperty.Register(
                "IsAdditive",               // Property Name
                typeof(bool),               // Property Type
                typeof(AnimationTimeline),  // Owner Class
                new PropertyMetadata(false,
                                     new PropertyChangedCallback(AnimationTimeline_PropertyChangedFunction)));

        /// <summary>
        ///
        /// </summary>
        public static readonly DependencyProperty IsCumulativeProperty =
            DependencyProperty.Register(
                "IsCumulative",             // Property Name
                typeof(bool),               // Property Type
                typeof(AnimationTimeline),  // Owner Class
                new PropertyMetadata(false,
                                     new PropertyChangedCallback(AnimationTimeline_PropertyChangedFunction)));

        #endregion

        #region Freezable

        /// <summary>
        /// Creates a copy of this AnimationTimeline.
        /// </summary>
        /// <returns>The copy.</returns>
        public new AnimationTimeline Clone()
        {
            return (AnimationTimeline)base.Clone();
        }

        #endregion

        #region Timeline

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected internal override Clock AllocateClock()
        {
            return new AnimationClock(this);
        }

        /// <summary>
        /// Creates a new AnimationClock using this AnimationTimeline.
        /// </summary>
        /// <returns>A new AnimationClock.</returns>
        new public AnimationClock CreateClock()
        {
            return (AnimationClock)base.CreateClock();
        }

        #endregion

        /// <summary>
        /// Calculates the value this animation believes should be the current value for the property.
        /// </summary>
        /// <param name="defaultOriginValue">
        /// This value is the suggested origin value provided to the animation
        /// to be used if the animation does not have its own concept of a
        /// start value. If this animation is not the first in a composition
        /// chain this value will be the value returned by the previous 
        /// animation in the chain with an animationClock that is not Stopped.
        /// </param>
        /// <param name="defaultDestinationValue">
        /// This value is the suggested destination value provided to the animation
        /// to be used if the animation does not have its own concept of an
        /// end value. If this animation is not the first in a composition
        /// chain this value will be the value returned by the previous 
        /// animation in the chain with an animationClock that is not Stopped.
        /// </param>
        /// <param name="animationClock">
        /// This is the animationClock which can generate the CurrentTime or
        /// CurrentProgress value to be used by the animation to generate its
        /// output value.
        /// </param>
        /// <returns>
        /// The value this animation believes should be the current value for the property.
        /// </returns>
        public virtual object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            ReadPreamble();

            return defaultDestinationValue;
        }

        /// <summary>
        /// Provide a custom natural Duration when the Duration property is set to Automatic.
        /// </summary>
        /// <param name="clock">
        /// The Clock whose natural duration is desired.
        /// </param>
        /// <returns>
        /// A Duration quantity representing the natural duration.  Default is 1 second for animations.
        /// </returns>
        protected override Duration GetNaturalDurationCore(Clock clock)
        {
            return new TimeSpan(0, 0, 1);
        }

        /// <summary>
        /// Returns the type of the animation.
        /// </summary>
        /// <value></value>
        public abstract Type TargetPropertyType { get; }

        /// <summary>
        /// This property is implemented by the animation to return true if the
        /// animation uses the defaultDestinationValue parameter to the 
        /// GetCurrentValue method as its destination value. Specifically, if
        /// Progress is equal to 1.0, will this animation return the 
        /// default destination value as its current value.
        /// </summary>
        public virtual bool IsDestinationDefault
        {
            get
            {
                ReadPreamble();

                return false;
            }
        }
    }
}


