// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Markup;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// This class represents base class for Timelines that have functionality
    /// related to having Children.
    /// </summary>
    [ContentProperty("Children")]
    public abstract partial class TimelineGroup : Timeline, IAddChild
    {
        #region Constructors

        /// <summary>
        /// Creates a TimelineGroup with default properties.
        /// </summary>
        protected TimelineGroup()
            : base()
        {
        }

        /// <summary>
        /// Creates a TimelineGroup with the specified BeginTime.
        /// </summary>
        /// <param name="beginTime">
        /// The scheduled BeginTime for this TimelineGroup.
        /// </param>
        protected TimelineGroup(Nullable<TimeSpan> beginTime)
            : base(beginTime)
        {
        }

        /// <summary>
        /// Creates a TimelineGroup with the specified begin time and duration.
        /// </summary>
        /// <param name="beginTime">
        /// The scheduled BeginTime for this TimelineGroup.
        /// </param>
        /// <param name="duration">
        /// The simple Duration of this TimelineGroup.
        /// </param>
        protected TimelineGroup(Nullable<TimeSpan> beginTime, Duration duration)
            : base(beginTime, duration)
        {
        }

        /// <summary>
        /// Creates a TimelineGroup with the specified BeginTime, Duration and RepeatBehavior.
        /// </summary>
        /// <param name="beginTime">
        /// The scheduled BeginTime for this TimelineGroup.
        /// </param>
        /// <param name="duration">
        /// The simple Duration of this TimelineGroup.
        /// </param>
        /// <param name="repeatBehavior">
        /// The RepeatBehavior for this TimelineGroup.
        /// </param>
        protected TimelineGroup(Nullable<TimeSpan> beginTime, Duration duration, RepeatBehavior repeatBehavior)
            : base(beginTime, duration, repeatBehavior)
        {
        }

        #endregion

        #region Timeline

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected internal override Clock AllocateClock()
        {
            return new ClockGroup(this);
        }

        /// <summary>
        /// Creates a new ClockGroup using this TimelineGroup.
        /// </summary>
        /// <returns>A new ClockGroup.</returns>
        new public ClockGroup CreateClock()
        {
            return (ClockGroup)base.CreateClock();
        }

        #endregion
        
        #region IAddChild interface

        /// <summary>
        /// Adds a child object to this TimelineGroup.
        /// </summary>
        /// <param name="child">
        /// The child object to add.
        /// </param>
        /// <remarks>
        /// A Timeline only accepts another Timeline (or derived class) as
        /// a child.
        /// </remarks>
        void IAddChild.AddChild(object child)
        {
            WritePreamble();

            if (child == null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            AddChild(child);

            WritePostscript();
        }

        /// <summary>
        /// This method performs the core functionality of the AddChild()
        /// method on the IAddChild interface.  For a Timeline this means
        /// determining adding the child parameter to the Children collection
        /// if it's a Timeline.
        /// </summary>
        /// <remarks>
        /// This method is the only core implementation.  It does not call
        /// WritePreamble() or WritePostscript().  It also doesn't throw an
        /// ArgumentNullException if the child parameter is null.  These tasks
        /// are performed by the interface implementation.  Therefore, it's OK
        /// for a derived class to override this method and call the base
        /// class implementation only if they determine that it's the right
        /// course of action.  The derived class can rely on Timeline's
        /// implementation of IAddChild.AddChild or implement their own
        /// following the Freezable pattern since that would be a public
        /// method.
        /// </remarks>
        /// <param name="child">An object representing the child that
        /// should be added.  If this is a Timeline it will be added to the
        /// Children collection; otherwise an exception will be thrown.</param>
        /// <exception cref="ArgumentException">The child parameter is not a
        /// Timeline.</exception>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected virtual void AddChild(object child)
        {
            Timeline timelineChild = child as Timeline;

            if (timelineChild == null)
            {
                throw new ArgumentException(SR.Get(SRID.Timing_ChildMustBeTimeline), nameof(child));
            }
            else
            {
                Children.Add(timelineChild);
            }
        }

        /// <summary>
        /// Adds a text string as a child of this Timeline.
        /// </summary>
        /// <param name="childText">
        /// The text to add.
        /// </param>
        /// <remarks>
        /// A Timeline does not accept text as a child, so this method will
        /// raise an InvalididOperationException unless a derived class has
        /// overridden the behavior to add text.
        /// </remarks>
        /// <exception cref="ArgumentNullException">The childText parameter is
        /// null.</exception>
        void IAddChild.AddText(string childText)
        {
            WritePreamble();

            if (childText == null)
            {
                throw new ArgumentNullException(nameof(childText));
            }

            AddText(childText);

            WritePostscript();
        }

        /// <summary>
        /// This method performs the core functionality of the AddText()
        /// method on the IAddChild interface.  For a Timeline this means
        /// throwing and InvalidOperationException because it doesn't
        /// support adding text.
        /// </summary>
        /// <remarks>
        /// This method is the only core implementation.  It does not call
        /// WritePreamble() or WritePostscript().  It also doesn't throw an
        /// ArgumentNullException if the childText parameter is null.  These tasks
        /// are performed by the interface implementation.  Therefore, it's OK
        /// for a derived class to override this method and call the base
        /// class implementation only if they determine that it's the right
        /// course of action.  The derived class can rely on Timeline's
        /// implementation of IAddChild.AddChild or implement their own
        /// following the Freezable pattern since that would be a public
        /// method.
        /// </remarks>
        /// <param name="childText">A string representing the child text that
        /// should be added.  If this is a Timeline an exception will be
        /// thrown.</param>
        /// <exception cref="InvalidOperationException">Timelines have no way
        /// of adding text.</exception>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected virtual void AddText(string childText)
        {
            throw new InvalidOperationException(SR.Get(SRID.Timing_NoTextChildren));
        }

        #endregion // IAddChild interface
    }
}
