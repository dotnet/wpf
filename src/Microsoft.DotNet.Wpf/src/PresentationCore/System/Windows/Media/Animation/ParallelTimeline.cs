// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// This class represents a group of Timelines where the children
    /// become active according to the value of their Begin property rather
    /// than their specific order in the Children collection. Children
    /// are also able to overlap and run in parallel with each other.
    /// </summary>
    public partial class ParallelTimeline : TimelineGroup
    {
        #region Constructors

        /// <summary>
        /// Creates a ParallelTimeline with default properties.
        /// </summary>
        public ParallelTimeline()
            : base()
        {
        }

        /// <summary>
        /// Creates a ParallelTimeline with the specified BeginTime.
        /// </summary>
        /// <param name="beginTime">
        /// The scheduled BeginTime for this ParallelTimeline.
        /// </param>
        public ParallelTimeline(TimeSpan? beginTime)
            : base(beginTime)
        {
        }

        /// <summary>
        /// Creates a ParallelTimeline with the specified begin time and duration.
        /// </summary>
        /// <param name="beginTime">
        /// The scheduled BeginTime for this ParallelTimeline.
        /// </param>
        /// <param name="duration">
        /// The simple Duration of this ParallelTimeline.
        /// </param>
        public ParallelTimeline(TimeSpan? beginTime, Duration duration)
            : base(beginTime, duration)
        {
        }

        /// <summary>
        /// Creates a ParallelTimeline with the specified BeginTime, Duration and RepeatBehavior.
        /// </summary>
        /// <param name="beginTime">
        /// The scheduled BeginTime for this ParallelTimeline.
        /// </param>
        /// <param name="duration">
        /// The simple Duration of this ParallelTimeline.
        /// </param>
        /// <param name="repeatBehavior">
        /// The RepeatBehavior for this ParallelTimeline.
        /// </param>
        public ParallelTimeline(TimeSpan? beginTime, Duration duration, RepeatBehavior repeatBehavior)
            : base(beginTime, duration, repeatBehavior)
        {
        }

        #endregion

        #region Methods
        
        /// <summary>
        /// Return the duration from a specific clock
        /// </summary>
        /// <param name="clock">
        /// The Clock whose natural duration is desired.
        /// </param>
        /// <returns>
        /// A Duration quantity representing the natural duration.
        /// </returns>
        protected override Duration GetNaturalDurationCore(Clock clock)
        {
            Duration simpleDuration = TimeSpan.Zero;

            ClockGroup clockGroup = clock as ClockGroup;

            if (clockGroup != null)
            {
                List<Clock> children = clockGroup.InternalChildren;

                // The container ends when all of its children have ended at least
                // one of their active periods.
                if (children != null)
                {
                    bool hasChildWithUnresolvedDuration = false;

                    for (int childIndex = 0; childIndex < children.Count; childIndex++)
                    {
                        Duration childEndOfActivePeriod = children[childIndex].EndOfActivePeriod;

                        if (childEndOfActivePeriod == Duration.Forever)
                        {
                            // If we have even one child with a duration of forever
                            // our resolved duration will also be forever. It doesn't
                            // matter if other children have unresolved durations.
                            return Duration.Forever;
                        }
                        else if (childEndOfActivePeriod == Duration.Automatic)
                        {
                            hasChildWithUnresolvedDuration = true;
                        }
                        else if (childEndOfActivePeriod > simpleDuration)
                        {
                            simpleDuration = childEndOfActivePeriod;
                        }
                    }

                    // We've iterated through all our children. We know that at this
                    // point none of them have a duration of Forever or we would have
                    // returned already. If any of them still have unresolved 
                    // durations then our duration is also still unresolved and we
                    // will return automatic. Otherwise, we'll fall out of the 'if'
                    // block and return the simpleDuration as our final resolved 
                    // duration.
                    if (hasChildWithUnresolvedDuration)
                    {
                        return Duration.Automatic;
                    }
                }
            }

            return simpleDuration;
        }

        #endregion



        #region SlipBehavior Property

        /// <summary>
        /// SlipBehavior Property
        /// </summary>
        public static readonly DependencyProperty SlipBehaviorProperty =
            DependencyProperty.Register(
                "SlipBehavior",
                typeof(SlipBehavior),
                typeof(ParallelTimeline),
                new PropertyMetadata(
                    SlipBehavior.Grow,
                    new PropertyChangedCallback(ParallelTimeline_PropertyChangedFunction)),
                new ValidateValueCallback(ValidateSlipBehavior));


        private static bool ValidateSlipBehavior(object value)
        {
            return TimeEnumHelper.IsValidSlipBehavior((SlipBehavior)value);
        }

        /// <summary>
        /// Returns the SlipBehavior for this ClockGroup
        /// </summary>
        [DefaultValue(SlipBehavior.Grow)]
        public SlipBehavior SlipBehavior
        {
            get
            {
                return (SlipBehavior)GetValue(SlipBehaviorProperty);
            }
            set
            {
                SetValue(SlipBehaviorProperty, value);
            }
        }

        internal static void ParallelTimeline_PropertyChangedFunction(DependencyObject d,
                                                                      DependencyPropertyChangedEventArgs e)
        {
            ((ParallelTimeline)d).PropertyChanged(e.Property);
        }

        #endregion // SlipBehavior Property
    }
}
