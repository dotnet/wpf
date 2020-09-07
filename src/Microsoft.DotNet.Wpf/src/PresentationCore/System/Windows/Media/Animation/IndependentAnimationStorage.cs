// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Media.Composition;

namespace System.Windows.Media.Animation
{
    /// <summary>
    ///
    /// </summary>
    internal abstract class IndependentAnimationStorage : AnimationStorage, DUCE.IResource
    {
        protected MediaContext.ResourcesUpdatedHandler       _updateResourceHandler;
        protected DUCE.MultiChannelResource     _duceResource = new DUCE.MultiChannelResource();
        private bool _isValid = true;

        #region Constructor

        protected IndependentAnimationStorage()
            : base()
        {
        }

        #endregion

        #region Protected

        protected abstract void UpdateResourceCore(DUCE.Channel channel);

        //
        // Method which returns the DUCE type of this class.
        // The base class needs this type when calling CreateOrAddRefOnChannel.
        // By providing this via a virtual, we avoid a per-instance storage cost.
        //
        protected abstract DUCE.ResourceType ResourceType { get; }

        #endregion

        #region DUCE.IResource

        /// <summary>
        /// AddRefOnChannel
        /// </summary>
        DUCE.ResourceHandle DUCE.IResource.AddRefOnChannel(DUCE.Channel channel)
        {
            // reconsider the need for this lock
            using (CompositionEngineLock.Acquire())
            {
#if DEBUG
                // We assume that a multi-channel resource can only be multi-channel
                // if it is Frozen and does not have animated properties. In this case we know
                // the target resource has at least one animated property so we expect that this
                // independently animated property resource will only be added to the channel
                // associated with the MediaContext associated with the target object's Dispatcher.

                DependencyObject d = (DependencyObject)_dependencyObject.Target;

                // I'm not sure how our target animated DependencyObject would get garbage
                // collected before we call AddRefOnChannel on one of its animated property
                // resources, but if it happens it will be a bad thing.
                Debug.Assert(d != null);

                // Any animated DependencyObject must be associated with a Dispatcher because the
                // AnimationClocks doing the animating must be associated with a Dispatcher.
                Debug.Assert(d.Dispatcher != null);

                // Make sure the target belongs to this thread
                Debug.Assert(d.CheckAccess());
#endif

                if (_duceResource.CreateOrAddRefOnChannel(this, channel, ResourceType))
                {
                    _updateResourceHandler = new MediaContext.ResourcesUpdatedHandler(UpdateResource);

                    UpdateResourceCore(channel);
                }

                return _duceResource.GetHandle(channel);
            }
        }

        /// <summary>
        /// ReleaseOnChannel
        /// </summary>
        void DUCE.IResource.ReleaseOnChannel(DUCE.Channel channel)
        {
            // reconsider the need for this lock
            using (CompositionEngineLock.Acquire())
            {
                Debug.Assert(_duceResource.IsOnChannel(channel));

                //release from this channel
                _duceResource.ReleaseOnChannel(channel);

                if (!_duceResource.IsOnAnyChannel)
                {
                    // If this was the last reference on the channel then clear up our state.
                    // Again, we assume here that if the target DependencyObject is animated that
                    // it will be associated with a Dispatcher and that this animation resource
                    // will also be associated with that Dispatcher's channel.

                    DependencyObject d = (DependencyObject)_dependencyObject.Target;

                    // DependencyObject shouldn't have been garbage collected before we've
                    // released all of its property animation resources.
                    Debug.Assert(d != null);

                    // The target DependencyObject should be associated with a Dispatcher.
                    Debug.Assert(d.Dispatcher != null);

                    // Make sure the target belongs to this thread
                    Debug.Assert(d.CheckAccess());

                    // If we're invalid, that means we've added our _updateResourceHandler to the
                    // MediaContext's ResourcesUpdated event. Since we've been entirely released
                    // from the channel we can cancel this update by removing the handler.
                    if (!_isValid)
                    {
                        MediaContext mediaContext = MediaContext.From(d.Dispatcher);
                        mediaContext.ResourcesUpdated -= _updateResourceHandler;
                        _isValid = true;
                    }

                    _updateResourceHandler = null;
                }
            }
        }

        /// <summary>
        /// Returns the DUCE.ResourceHandle associated with this resource.
        /// </summary>
        DUCE.ResourceHandle DUCE.IResource.GetHandle(DUCE.Channel channel)
        {
            DUCE.ResourceHandle handle;

            using (CompositionEngineLock.Acquire())
            {
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


        #endregion

        #region Private

        private void UpdateResource(DUCE.Channel channel, bool skipOnChannelCheck)
        {
            Debug.Assert(!skipOnChannelCheck || _duceResource.IsOnChannel(channel));

            if (skipOnChannelCheck || _duceResource.IsOnChannel(channel))
            {
                UpdateResourceCore(channel);

                _isValid = true;
            }
        }

        #endregion

        #region Internal

        /// <summary>
        /// If necessary this method will add an event handler to the MediaContext's
        /// ResourcesUpdated event which will be raised the next time we render. This prevents us
        /// from updating this animated property value resource more than once per rendered frame.
        /// </summary>
        internal void InvalidateResource()
        {
            // If _isValid is false we've already added the event handler for the next frame.
            // If _updateResourceHandler is null we haven't been added to our channel yet so
            //     there's no need to update anything.
            if (   _isValid
                && _updateResourceHandler != null)
            {
                DependencyObject d = (DependencyObject)_dependencyObject.Target;

                // If d is null it means the resource that we're animating has been garbage
                // collected and had not fully been released it from its channel. This is
                // highly unlikely and if it occurs something has gone horribly wrong.
                Debug.Assert(d != null);

                // Just make sure that this resource has been added to the channel associated
                // with the MediaContext associated with the target object's Dispatcher.
                Debug.Assert(_duceResource.IsOnAnyChannel);

                // Set this flag so that we won't add the event handler to the MediaContext's
                // ResourcesUpdated event again before the next frame is rendered.
                _isValid = false;

                MediaContext.CurrentMediaContext.ResourcesUpdated += _updateResourceHandler;
            }
        }

        #endregion

        #region Static helper methods

        internal static DUCE.ResourceHandle GetResourceHandle(DependencyObject d, DependencyProperty dp, DUCE.Channel channel)
        {
            Debug.Assert(d != null);
            Debug.Assert(dp != null);
            Debug.Assert(d is Animatable ? ((Animatable)d).HasAnimatedProperties : true);

            IndependentAnimationStorage storage = AnimationStorage.GetStorage(d, dp) as IndependentAnimationStorage;

            if (storage == null)
            {
                return DUCE.ResourceHandle.Null;
            }
            else
            {
                Debug.Assert(storage._duceResource.IsOnChannel(channel));

                return ((DUCE.IResource)storage).GetHandle(channel);
            }
        }

        #endregion
    }
}
