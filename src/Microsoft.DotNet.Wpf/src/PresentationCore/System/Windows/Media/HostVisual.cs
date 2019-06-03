// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//     Host visual.
//

namespace System.Windows.Media
{
    using System;
    using System.Windows.Threading;
    using System.Windows.Media;
    using System.Windows.Media.Composition;
    using System.Diagnostics;
    using System.Collections;
    using System.Collections.Generic;
    using MS.Internal;
    using System.Resources;
    using System.Runtime.InteropServices;
    using MS.Win32;
    using System.Threading;

    using SR=MS.Internal.PresentationCore.SR;
    using SRID=MS.Internal.PresentationCore.SRID;

    /// <summary>
    /// Host visual.
    /// </summary>
    public class HostVisual : ContainerVisual
    {
        //----------------------------------------------------------------------
        //
        //  Constructors
        //
        //----------------------------------------------------------------------

        #region Constructors

        /// <summary>
        ///
        /// </summary>
        public HostVisual()
        {
}

        #endregion Constructors

        //----------------------------------------------------------------------
        //
        //  Protected Methods
        //
        //----------------------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// HitTestCore
        /// </summary>
        protected override HitTestResult HitTestCore(
            PointHitTestParameters hitTestParameters)
        {
            //
            // HostVisual never reports itself as being hit. To change this
            // behavior clients should derive from HostVisual and override
            // HitTestCore methods.
            //
            return null;
        }

        /// <summary>
        /// HitTestCore
        /// </summary>
        protected override GeometryHitTestResult HitTestCore(
            GeometryHitTestParameters hitTestParameters)
        {
            //
            // HostVisual never reports itself as being hit. To change this
            // behavior clients should derive from HostVisual and override
            // HitTestCore methods.
            //
            return null;
        }

        #endregion Protected Methods

        //----------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //----------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        ///
        /// </summary>
        internal override Rect GetContentBounds()
        {
            return Rect.Empty;
        }

        /// <summary>
        ///
        /// </summary>
        internal override void RenderContent(RenderContext ctx, bool isOnChannel)
        {
            //
            // Make sure that the visual target is properly hosted.
            //

            EnsureHostedVisualConnected(ctx.Channel);
        }

        /// <summary>
        ///
        /// </summary>
        internal override void FreeContent(DUCE.Channel channel)
        {
            //
            // Disconnect hosted visual from this channel.
            //

            using (CompositionEngineLock.Acquire())
            {
                DisconnectHostedVisual(
                    channel,
                    /* removeChannelFromCollection */ true);
            }

            base.FreeContent(channel);
        }

        /// <summary>
        ///
        /// </summary>
        internal void BeginHosting(VisualTarget target)
        {
            //
            // This method is executed on the visual target thread.
            //

            Debug.Assert(target != null);
            Debug.Assert(target.Dispatcher.Thread == Thread.CurrentThread);

            using (CompositionEngineLock.Acquire())
            {
                //
                // Check if another target is already hosted by this
                // visual and throw exception if this is the case.
                //
                if (_target != null)
                {
                    throw new InvalidOperationException(
                        SR.Get(SRID.VisualTarget_AnotherTargetAlreadyConnected)
                        );
                }

                _target = target;

                //
                // If HostVisual and VisualTarget on same thread, then call Invalidate
                // directly. Otherwise post invalidate message to the host visual thread
                // indicating that content update is required.
                //
                if (this.CheckAccess())
                {
                    Invalidate();
                }
                else
                {
                    Dispatcher.BeginInvoke(
                        DispatcherPriority.Normal,
                        (DispatcherOperationCallback)delegate(object args)
                        {
                            Invalidate();
                            return null;
                        },
                        null
                        );
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        internal void EndHosting()
        {
            //
            // This method is executed on the visual target thread.
            //

            using (CompositionEngineLock.Acquire())
            {
                Debug.Assert(_target != null);
                Debug.Assert(_target.Dispatcher.Thread == Thread.CurrentThread);

                DisconnectHostedVisualOnAllChannels();

                _target = null;
            }
        }

        /// <summary>
        /// Should be called from the VisualTarget thread
        /// when it is safe to access the composition node
        /// and out of band channel from the VisualTarget thread
        /// to allow for the handle duplication/channel commit
        /// </summary>
        internal object DoHandleDuplication(object channel)
        {
            DUCE.ResourceHandle targetsHandle = DUCE.ResourceHandle.Null;

            using (CompositionEngineLock.Acquire())
            {
                targetsHandle = _target._contentRoot.DuplicateHandle(_target.OutOfBandChannel, (DUCE.Channel)channel);

                Debug.Assert(!targetsHandle.IsNull);

                _target.OutOfBandChannel.CloseBatch();
                _target.OutOfBandChannel.Commit();
            }
            
            return targetsHandle;
        }

        #endregion Internal Methods


        //----------------------------------------------------------------------
        //
        //  Private Methods
        //
        //----------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Connects the hosted visual on a channel if necessary.
        /// </summary>
        private void EnsureHostedVisualConnected(DUCE.Channel channel)
        {
            //
            // Conditions for connecting VisualTarget to Host Visual:-
            // 1. The channel on which we are rendering should not be synchronous. This
            //    scenario is not supported currently.
            // 2. VisualTarget should not be null.
            // 3. They should not be already connected.
            //
            if (!(channel.IsSynchronous)
                && _target != null
                && !_connectedChannels.Contains(channel))
            {
                Debug.Assert(IsOnChannel(channel));

                DUCE.ResourceHandle targetsHandle = DUCE.ResourceHandle.Null;

                bool doDuplication = true;

                //
                // If HostVisual and VisualTarget are on same thread, then we just addref
                // VisualTarget. Otherwise, if on different threads, then we duplicate
                // VisualTarget onto Hostvisual's channel.
                //
                if (_target.CheckAccess())
                {
                    Debug.Assert(_target._contentRoot.IsOnChannel(channel));
                    Debug.Assert(_target.OutOfBandChannel == MediaContext.CurrentMediaContext.OutOfBandChannel);
                    bool created = _target._contentRoot.CreateOrAddRefOnChannel(this, channel, VisualTarget.s_contentRootType);
                    Debug.Assert(!created);
                    targetsHandle = _target._contentRoot.GetHandle(channel);
                }
                else
                {
                    //
                    // Duplicate the target's handle onto our channel.
                    //
                    // We must wait synchronously for the _targets Dispatcher to call
                    // back and do handle duplication. We can't do handle duplication
                    // on this thread because access to the _target CompositionNode
                    // is not synchronized. If we duplicated here, we could potentially
                    // corrupt the _target OutOfBandChannel or the CompositionNode
                    // MultiChannelResource. We have to wait synchronously because
                    // we need the resulting duplicated handle to hook up as a child
                    // to this HostVisual.
                    //

                    object returnValue = _target.Dispatcher.Invoke(
                        DispatcherPriority.Normal,
                        TimeSpan.FromMilliseconds(1000),
                        new DispatcherOperationCallback(DoHandleDuplication),
                        channel
                        );

                    //
                    // Duplication and flush is complete, we can resume processing
                    // Only if the Invoke succeeded will we have a handle returned.
                    //
                    if (returnValue != null)
                    {
                        targetsHandle = (DUCE.ResourceHandle)returnValue;
                    }
                    else
                    {
                        // The Invoke didn't complete
                        doDuplication = false;
                    }
                }

                if (doDuplication)
                {
                    if (!targetsHandle.IsNull)
                    {
                        using (CompositionEngineLock.Acquire())
                        {
                            DUCE.CompositionNode.InsertChildAt(
                                _proxy.GetHandle(channel),
                                targetsHandle,
                                0,
                                channel);
                        }

                        _connectedChannels.Add(channel);

                        //
                        // Indicate that that content composition root has been
                        // connected, this needs to be taken into account to
                        // properly manage children of this visual.
                        //

                        SetFlags(channel, true, VisualProxyFlags.IsContentNodeConnected);
                    }
                }
                else
                {
                    //
                    // We didn't get a handle, because _target belongs to a
                    // different thread, and the Invoke operation failed. We can't do
                    // anything except try again in the next render pass. We can't
                    // call Invalidate during the render pass because it pushes up
                    // flags that are being modified within the render pass, so get
                    // the local Dispatcher to do it for us later.
                    //
                    Dispatcher.BeginInvoke(
                        DispatcherPriority.Normal,
                        (DispatcherOperationCallback)delegate(object args)
                        {
                            Invalidate();
                            return null;
                        },
                        null
                        );
                }
            }
        }


        /// <summary>
        /// Disconnects the hosted visual on all channels we have
        /// connected it to.
        /// </summary>
        private void DisconnectHostedVisualOnAllChannels()
        {
            foreach (DUCE.Channel channel in _connectedChannels)
            {
                DisconnectHostedVisual(
                    channel,
                    /* removeChannelFromCollection */ false);
            }

            _connectedChannels.Clear();
        }


        /// <summary>
        /// Disconnects the hosted visual on a channel.
        /// </summary>
        private void DisconnectHostedVisual(
            DUCE.Channel channel,
            bool removeChannelFromCollection)
        {
            if (_target != null && _connectedChannels.Contains(channel))
            {
                DUCE.CompositionNode.RemoveChild(
                    _proxy.GetHandle(channel),
                    _target._contentRoot.GetHandle(channel),
                    channel
                    );

                //
                // Release the targets handle. If we had duplicated the handle,
                // then this removes the duplicated handle, otherwise just decrease
                // the ref count for VisualTarget.
                //

                _target._contentRoot.ReleaseOnChannel(channel);

                SetFlags(channel, false, VisualProxyFlags.IsContentNodeConnected);

                if (removeChannelFromCollection)
                {
                    _connectedChannels.Remove(channel);
                }
            }
        }


        /// <summary>
        /// Invalidate this visual.
        /// </summary>
        private void Invalidate()
        {
            SetFlagsOnAllChannels(true, VisualProxyFlags.IsContentDirty);

            PropagateChangedFlags();
        }

        #endregion Private Methods


        //----------------------------------------------------------------------
        //
        //  Private Fields
        //
        //----------------------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// The hosted visual target.
        /// </summary>
        /// <remarks>
        /// This field is free-threaded and should be accessed from under a lock.
        /// </remarks>
        private VisualTarget _target;

        /// <summary>
        /// The channels we have marshalled the visual target composition root.
        /// </summary>
        /// <remarks>
        /// This field is free-threaded and should be accessed from under a lock.
        /// </remarks>
        private List<DUCE.Channel> _connectedChannels = new List<DUCE.Channel>();

        #endregion Private Fields
    }
}

