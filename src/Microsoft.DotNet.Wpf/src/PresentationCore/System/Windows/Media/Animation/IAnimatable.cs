// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Windows.Media.Animation
{
    /// <summary>
    /// This interface is supported by DependencyObjects whose properties are
    /// animatable.
    /// </summary>
    public interface IAnimatable
    {
        /// <summary>
        /// Applies an AnimationClock to a DepencencyProperty which will
        /// replace the current animations on the property using the snapshot
        /// and replace HandoffBehavior.
        /// </summary>
        /// <param name="dp">
        /// The DependencyProperty to animate.
        /// </param>
        /// <param name="clock">
        /// The AnimationClock that will animate the property. If this is null
        /// then all animations will be removed from the property.
        /// </param>
        void ApplyAnimationClock(DependencyProperty dp, AnimationClock clock);

        /// <summary>
        /// Applies an AnimationClock to a DependencyProperty. The effect of
        /// the new AnimationClock on any current animations will be determined by
        /// the value of the handoffBehavior parameter.
        /// </summary>
        /// <param name="dp">
        /// The DependencyProperty to animate.
        /// </param>
        /// <param name="clock">
        /// The AnimationClock that will animate the property. If parameter is null
        /// then animations will be removed from the property if handoffBehavior is
        /// SnapshotAndReplace; otherwise the method call will have no result.
        /// </param>
        /// <param name="handoffBehavior">
        /// Determines how the new AnimationClock will transition from or
        /// affect any current animations on the property.
        /// </param>
        void ApplyAnimationClock(DependencyProperty dp, AnimationClock clock, HandoffBehavior handoffBehavior);

        /// <summary>
        /// Starts an animation for a DependencyProperty. The animation will
        /// begin when the next frame is rendered.
        /// </summary>
        /// <param name="dp">The DependencyProperty to animate.</param>
        /// <param name="animation">The animation to used to animate the property.</param>
        void BeginAnimation(DependencyProperty dp, AnimationTimeline animation);

        /// <summary>
        /// Starts an animation for a DependencyProperty. The animation will
        /// begin when the next frame is rendered.
        /// </summary>
        /// <param name="dp">The DependencyProperty to animate.</param>
        /// <param name="animation">The animation to used to animate the property.</param>
        /// <param name="handoffBehavior">
        /// Specifies how the new animation should interact with any current
        /// animations already affecting the property value.
        /// </param>
        void BeginAnimation(DependencyProperty dp, AnimationTimeline animation, HandoffBehavior handoffBehavior);

        /// <summary>
        /// Returns true if any properties on this DependencyObject have a
        /// persistent animation or if the object has one or more clocks associated
        /// with any of its properties.
        /// </summary>
        bool HasAnimatedProperties { get; }

        /// <summary>
        /// Returns the value of a DependencyProperty as if it had no animations
        /// modifying its value.
        /// </summary>
        /// <param name="dp">
        /// The DependencyProperty for which the animation base value is being requested.
        /// </param>
        /// <returns>
        /// The value of a DependencyProperty as if it had no animations
        /// modifying its value.
        /// </returns>
        Object GetAnimationBaseValue(DependencyProperty dp);
    }
}
