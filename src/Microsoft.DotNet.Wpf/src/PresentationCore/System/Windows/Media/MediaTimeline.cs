// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Diagnostics;
using System.ComponentModel;
using MS.Internal;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    #region MediaTimeline


    /// <summary>
    /// MediaTimeline functions as a template for creating MediaClocks. Whenever
    /// you create a MediaClock it inherits all of the properties and events of
    /// the MediaTimeline. Whenever you change a property or register for an
    /// event on a MediaTimeline, all of those changes get propagated down to
    /// every MediaClock created off of that MediaTimeline (and any future
    /// MediaClocks created from it too).
    /// </summary>
    public partial class MediaTimeline : Timeline, IUriContext
    {
        internal const uint LastTimelineFlag = 0x1;
        #region Constructor

        /// <summary>
        /// Creates a new MediaTimeline.
        /// </summary>
        /// <param name="source">Source of the media.</param>
        public MediaTimeline(Uri source) : this()
        {
            Source = source;
        }

        /// <summary>
        /// Creates a new MediaTimeline.
        /// </summary>
        /// <param name="context">Context used to resolve relative URIs</param>
        /// <param name="source">Source of the media.</param>
        internal MediaTimeline(ITypeDescriptorContext context, Uri source) : this()
        {
            _context = context;
            Source = source;
        }

        /// <summary>
        /// Creates a new MediaTimeline.
        /// </summary>
        public MediaTimeline()
        {
        }

        /// <summary>
        /// Creates a new MediaTimeline.
        /// </summary>
        /// <param name="beginTime">The value for the BeginTime property</param>
        public MediaTimeline(Nullable<TimeSpan> beginTime) : this()
        {
            BeginTime = beginTime;
        }

        /// <summary>
        /// Creates a new MediaTimeline.
        /// </summary>
        /// <param name="beginTime">The value for the BeginTime property</param>
        /// <param name="duration">The value for the Duration property</param>
        public MediaTimeline(Nullable<TimeSpan> beginTime, Duration duration)
            : this()
        {
            BeginTime = beginTime;
            Duration = duration;
        }

        /// <summary>
        /// Creates a new MediaTimeline.
        /// </summary>
        /// <param name="beginTime">The value for the BeginTime property</param>
        /// <param name="duration">The value for the Duration property</param>
        /// <param name="repeatBehavior">The value for the RepeatBehavior property</param>
        public MediaTimeline(Nullable<TimeSpan> beginTime, Duration duration, RepeatBehavior repeatBehavior)
            : this()
        {
            BeginTime = beginTime;
            Duration = duration;
            RepeatBehavior = repeatBehavior;
        }

        #endregion

        #region IUriContext implementation
        /// <summary>
        /// Base Uri to use when resolving relative Uri's
        /// </summary>
        Uri IUriContext.BaseUri
        {
            get
            {
                return _baseUri;
            }
            set
            {
                _baseUri = value;
            }
        }
        #endregion


        #region Timeline

        /// <summary>
        /// Called by the Clock to create a type-specific clock for this
        /// timeline.
        /// </summary>
        /// <returns>
        /// A clock for this timeline.
        /// </returns>
        /// <remarks>
        /// If a derived class overrides this method, it should only create
        /// and return an object of a class inheriting from Clock.
        /// </remarks>
        protected internal override Clock AllocateClock()
        {
            if (Source == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.Media_UriNotSpecified));
            }

            MediaClock mediaClock = new MediaClock(this);

            return mediaClock;
        }

        /// <summary>
        /// Called by the base Freezable class to make this object
        /// frozen.
        /// </summary>
        protected override bool FreezeCore(bool isChecking)
        {
            bool canFreeze = base.FreezeCore(isChecking);
            if (!canFreeze)
            {
                return false;
            }

            // First, if checking, make sure that we don't have any expressions
            // on our properties.  (base.FreezeCore takes care of animations)
            if (isChecking)
            {
                canFreeze &= !HasExpression(LookupEntry(SourceProperty.GlobalIndex), SourceProperty);
            }

            return canFreeze;
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCore(System.Windows.Freezable)">Freezable.CloneCore</see>.
        /// </summary>
        protected override void CloneCore(Freezable sourceFreezable)
        {
            MediaTimeline sourceTimeline = (MediaTimeline) sourceFreezable;
            base.CloneCore(sourceFreezable);

            CopyCommon(sourceTimeline);
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCurrentValueCore(Freezable)">Freezable.CloneCurrentValueCore</see>.
        /// </summary>
        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            MediaTimeline sourceTimeline = (MediaTimeline) sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);

            CopyCommon(sourceTimeline);
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetAsFrozenCore(Freezable)">Freezable.GetAsFrozenCore</see>.
        /// </summary>
        protected override void GetAsFrozenCore(Freezable source)
        {
            MediaTimeline sourceTimeline = (MediaTimeline) source;
            base.GetAsFrozenCore(source);

            CopyCommon(sourceTimeline);
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetCurrentValueAsFrozenCore(Freezable)">Freezable.GetCurrentValueAsFrozenCore</see>.
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable source)
        {
            MediaTimeline sourceTimeline = (MediaTimeline) source;
            base.GetCurrentValueAsFrozenCore(source);

            CopyCommon(sourceTimeline);
        }

        private void CopyCommon(MediaTimeline sourceTimeline)
        {
            _context = sourceTimeline._context;
            _baseUri = sourceTimeline._baseUri;
        }

        /// <summary>
        /// Creates a new MediaClock using this MediaTimeline.
        /// </summary>
        /// <returns>A new MediaClock.</returns>
        public new MediaClock CreateClock()
        {
            return (MediaClock)base.CreateClock();
        }

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
            MediaClock mc = (MediaClock)clock;
            if (mc.Player == null)
            {
                return Duration.Automatic;
            }
            else
            {
                return mc.Player.NaturalDuration;
            }
        }

        #endregion

        #region ToString

        /// <summary>
        /// Persist MediaTimeline in a string
        /// </summary>
        public override string ToString()
        {
            if (null == Source)
                throw new InvalidOperationException(SR.Get(SRID.Media_UriNotSpecified));

            return Source.ToString();
        }

        #endregion

        internal ITypeDescriptorContext _context = null;

        private Uri _baseUri = null;
    }

    #endregion
};
