// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Windows.Media.Animation
{
    /// <summary>
    /// 
    /// </summary>
    public class AnimationClock : Clock
    {
        /// <summary>
        /// Creates a new empty AnimationClock to be used in a Clock
        /// tree.
        /// </summary>
        /// <param name="animation">The Animation used to define the new
        /// AnimationClock.</param>
        protected internal AnimationClock(AnimationTimeline animation)
            : base(animation)
        {
        }

        /// <summary>
        /// Gets the Animation object that holds the description controlling the
        /// behavior of this clock.
        /// </summary>
        /// <value>
        /// The Animation object that holds the description controlling the
        /// behavior of this clock.
        /// </value>
        public new AnimationTimeline Timeline
        {
            get
            {
                return (AnimationTimeline)base.Timeline;
            }
        }

        /// <summary>
        /// Returns the current value of this AnimationClock.
        /// </summary>
        /// <param name="defaultOriginValue"></param>
        /// <param name="defaultDestinationValue">The unanimated property value or the current 
        /// value of the previous AnimationClock in a list.</param>
        /// <returns>The current value of this AnimationClock.</returns>
        public object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue)
        {
            return ((AnimationTimeline)base.Timeline).GetCurrentValue(defaultOriginValue, defaultDestinationValue, this);
        }



        /// <summary>
        /// Returns true if this timeline needs continuous frames.
        /// This is a hint that we should keep updating our time during the active period.
        /// </summary>
        /// <returns></returns>
        internal override bool NeedsTicksWhenActive
        {
            get
            {
                return true;
            }
        }
    }
}
