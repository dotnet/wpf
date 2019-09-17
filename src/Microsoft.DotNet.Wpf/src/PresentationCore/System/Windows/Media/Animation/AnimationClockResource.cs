// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: This file contains the implementation of AnimationClockResource.
//              An AnimationClockResource is used to tie together an AnimationClock
//              and a base value as a DUCE resource.  This base class provides
//              Changed Events.
//
//

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// AnimationClockResource class.
    /// AnimationClockResource classes refer to an AnimationClock and a base
    /// value.  They implement DUCE.IResource, and thus can be used to produce
    /// a render-side resource which represents the current value of this
    /// AnimationClock.
    /// They subscribe to the Changed event on the AnimationClock and ensure
    /// that the resource's current value is up to date.
    /// </summary>
    internal abstract class AnimationClockResource: DUCE.IResource
    {
        /// <summary>
        /// Protected constructor for AnimationClockResource.
        /// The derived class must provide a created duceResource.
        /// </summary>
        /// <param name="animationClock"> The AnimationClock for this resource.  Can be null. </param>
        protected AnimationClockResource(AnimationClock animationClock)
        {
            _animationClock = animationClock;

            if (_animationClock != null)
            {
                _animationClock.CurrentTimeInvalidated += new EventHandler(OnChanged);
            }
        }

        #region Public Properties

        /// <summary>
        /// AnimationClock - accessor for the AnimationClock.
        /// </summary>
        public AnimationClock AnimationClock
        {
            get
            {
                return _animationClock;
            }
        }

        #endregion Public Properties

        /// <summary>
        /// OnChanged - this is fired if any dependents change.
        /// In this case, that means the Clock, which means we can (and do) assert that the Clock isn't null.
        /// </summary>
        /// <param name="sender"> object - the origin of the change. </param>
        /// <param name="args"> EventArgs - ignored. </param>
        protected void OnChanged(object sender, EventArgs args)
        {
            Debug.Assert(sender as System.Windows.Threading.DispatcherObject != null);
            Debug.Assert(((System.Windows.Threading.DispatcherObject)sender).Dispatcher != null);
            Debug.Assert(_animationClock != null);

            System.Windows.Threading.Dispatcher dispatcher = ((System.Windows.Threading.DispatcherObject)sender).Dispatcher;

            MediaContext mediaContext = MediaContext.From(dispatcher);

            DUCE.Channel channel = mediaContext.Channel;

            // Only register for an update if this resource is currently on channel and
            // isn't already registered.
            if (!IsResourceInvalid && _duceResource.IsOnAnyChannel)
            {
                // Add this handler to this event means that the handler will be
                // called on the next UIThread render for this Dispatcher.
                mediaContext.ResourcesUpdated += new MediaContext.ResourcesUpdatedHandler(UpdateResourceFromMediaContext);
                IsResourceInvalid = true;
            }
        }

        /// <summary>
        ///     Propagagtes handler to the _animationClock.
        /// </summary>
        /// <param name="handler">
        ///   EventHandler - the EventHandle to associate with the mutable dependents.
        /// </param>
        /// <param name="adding"> bool - if true, we're adding the new handler, if false we're removing it. </param>
        internal virtual void PropagateChangedHandlersCore(EventHandler handler, bool adding)
        {
            // Nothing to do if the clock is null.
            if (_animationClock != null)
            {
                if (adding)
                {
                    _animationClock.CurrentTimeInvalidated += handler;
                }
                else
                {
                    _animationClock.CurrentTimeInvalidated -= handler;
                }
            }
        }

        #region DUCE

        /// <summary>
        /// UpdateResourceFromMediaContext - this is called by the MediaContext
        /// to validate the render-thread resource.
        /// Sender is the MediaContext.
        /// </summary>
        private void UpdateResourceFromMediaContext(DUCE.Channel channel, bool skipOnChannelCheck)
        {
            // If we're told we can skip the channel check, then we must be on channel
            Debug.Assert(!skipOnChannelCheck || _duceResource.IsOnChannel(channel));

            // Check to see if we're on the channel and if we're invalid.
            // Only perform the update if this resource is currently on channel.
            if (IsResourceInvalid && (skipOnChannelCheck || _duceResource.IsOnChannel(channel)))
            {
                UpdateResource(_duceResource.GetHandle(channel),
                               channel);

                // This resource is now valid.
                IsResourceInvalid = false;
            }
        }

        /// <summary>
        /// UpdateResource - This method is called to update the render-thread
        /// resource on a given channel.
        /// </summary>
        /// <param name="handle"> The DUCE.ResourceHandle for this resource on this channel. </param>
        /// <param name="channel"> The channel on which to update the render-thread resource. </param>
        protected abstract void UpdateResource(DUCE.ResourceHandle handle,
                                               DUCE.Channel channel);

        DUCE.ResourceHandle DUCE.IResource.AddRefOnChannel(DUCE.Channel channel)
        {
            using (CompositionEngineLock.Acquire())
            {
                if (_duceResource.CreateOrAddRefOnChannel(this, channel, ResourceType))
                {
                    UpdateResource(_duceResource.GetHandle(channel),
                                   channel);
                }

                return _duceResource.GetHandle(channel);
            }
        }

        void DUCE.IResource.ReleaseOnChannel(DUCE.Channel channel)
        {
            using (CompositionEngineLock.Acquire())
            {
                Debug.Assert(_duceResource.IsOnChannel(channel));

                _duceResource.ReleaseOnChannel(channel);
            }
        }

        DUCE.ResourceHandle DUCE.IResource.GetHandle(DUCE.Channel channel)
        {
            DUCE.ResourceHandle handle;

            // Reconsider the need for this lock when removing the MultiChannelResource.
            using (CompositionEngineLock.Acquire())
            {
                // This method is a short cut and must only be called while the ref count
                // of this resource on this channel is non-zero.  Thus we assert that this
                // resource is already on this channel.
                Debug.Assert(_duceResource.IsOnChannel(channel));

                handle = _duceResource.GetHandle(channel);
            }

            return handle;
        }

        int DUCE.IResource.GetChannelCount()
        {
            return _duceResource.GetChannelCount();
        }

        DUCE.Channel DUCE.IResource.GetChannel(int index)
        {
            return _duceResource.GetChannel(index);
        }

        /// <summary>
        /// This is only implemented by Visual and Visual3D.
        /// </summary>
        void DUCE.IResource.RemoveChildFromParent(DUCE.IResource parent, DUCE.Channel channel)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This is only implemented by Visual and Visual3D.
        /// </summary>
        DUCE.ResourceHandle DUCE.IResource.Get3DHandle(DUCE.Channel channel)
        {
            throw new NotImplementedException();
        }

        #endregion DUCE

        #region Protected Method

        protected bool IsResourceInvalid
        {
            get
            {
                return _isResourceInvalid;
            }
            set
            {
                _isResourceInvalid = value;
            }
        }

        //
        // Method which returns the DUCE type of this class.
        // The base class needs this type when calling CreateOrAddRefOnChannel.
        // By providing this via a virtual, we avoid a per-instance storage cost.
        //
        protected abstract DUCE.ResourceType ResourceType { get; }

        #endregion Protected Method

        // DUCE resource
        // It is provided via the constructor.
        private DUCE.MultiChannelResource _duceResource = new DUCE.MultiChannelResource();

        // This bool keeps track of whether or not this resource is valid
        // on its channel.
        private bool _isResourceInvalid;

        // This AnimationClock is the animation associated with this resource.
        // It is provided via the constructor.
        protected AnimationClock _animationClock;
    }
}
