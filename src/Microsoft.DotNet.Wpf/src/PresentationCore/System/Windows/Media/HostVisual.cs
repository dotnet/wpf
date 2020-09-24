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
                // if there's a pending disconnect, do it now preemptively;
                // otherwise do the disconnect the normal way.
                // This ensures we do the disconnect before calling base,
                // as required.
                if (!DoPendingDisconnect(channel))
                {
                    DisconnectHostedVisual(
                        channel,
                        /* removeChannelFromCollection */ true);
                }
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
                && !_connectedChannels.ContainsKey(channel))
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

                        // remember what channel we connected to, and which thread
                        // did the connection, so that we can disconnect on the
                        // same thread.  Earlier comments imply this is the HostVisual's
                        // dispatcher thread, which we assert here.  Even if it's not,
                        // the code downstream should work, or at least not crash
                        // (even if channelDispatcher is set to null).
                        Dispatcher channelDispatcher = Dispatcher.FromThread(Thread.CurrentThread);
                        Debug.Assert(channelDispatcher == this.Dispatcher, "HostVisual connecting on a second thread");
                        _connectedChannels.Add(channel, channelDispatcher);

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
            IDictionaryEnumerator ide = _connectedChannels.GetEnumerator() as IDictionaryEnumerator;
            while (ide.MoveNext())
            {
                DisconnectHostedVisual(
                    (DUCE.Channel)ide.Key,
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
            Dispatcher channelDispatcher;
            if (_target != null && _connectedChannels.TryGetValue(channel, out channelDispatcher))
            {
                // Adding commands to a channel is not thread-safe,
                // we must do the actual work on the same dispatcher thread
                // where the connection happened.
                if (channelDispatcher != null && channelDispatcher.CheckAccess())
                {
                    Disconnect(channel,
                               channelDispatcher,
                               _proxy.GetHandle(channel),
                               _target._contentRoot.GetHandle(channel),
                               _target._contentRoot);
                }
                else
                {
                    // marshal to the right thread
                    if (channelDispatcher != null)
                    {
                        DispatcherOperation op = channelDispatcher.BeginInvoke(
                            DispatcherPriority.Normal,
                            new DispatcherOperationCallback(DoDisconnectHostedVisual),
                            channel);

                        _disconnectData = new DisconnectData(
                                                     op: op,
                                                     channel: channel,
                                                     dispatcher: channelDispatcher,
                                                     hostVisual: this,
                                                     hostHandle: _proxy.GetHandle(channel),
                                                     targetHandle: _target._contentRoot.GetHandle(channel),
                                                     contentRoot: _target._contentRoot,
                                                     next: _disconnectData);
                    }
                }

                if (removeChannelFromCollection)
                {
                    _connectedChannels.Remove(channel);
                }
            }
        }

        /// <summary>
        /// Callback to disconnect on the right thread
        /// </summary>
        private object DoDisconnectHostedVisual(object arg)
        {
            using (CompositionEngineLock.Acquire())
            {
                DoPendingDisconnect((DUCE.Channel)arg);
            }

            return null;
        }

        /// <summary>
        /// Perform a pending disconnect for the given channel.
        /// This method should be called under the CompositionEngineLock,
        /// on the thread that owns the channel.  It can be called either
        /// from the dispatcher callback DoDisconnectHostedVisual or
        /// from FreeContent, whichever happens to occur first.
        /// </summary>
        /// <returns>
        /// True if a matching request was found and processed.  False if not.
        /// </returns>
        private bool DoPendingDisconnect(DUCE.Channel channel)
        {
            DisconnectData disconnectData = _disconnectData;
            DisconnectData previous = null;

            // search the list for an entry matching the given channel
            while (disconnectData != null && (disconnectData.HostVisual != this || disconnectData.Channel != channel))
            {
                previous = disconnectData;
                disconnectData = disconnectData.Next;
            }

            // if no match found, do nothing
            if (disconnectData == null)
            {
                return false;
            }

            // remove the matching entry from the list
            if (previous == null)
            {
                _disconnectData = disconnectData.Next;
            }
            else
            {
                previous.Next = disconnectData.Next;
            }

            // cancel the dispatcher callback, (if we're already in it,
            // this call is a no-op)
            disconnectData.DispatcherOperation.Abort();

            // do the actual disconnect
            Disconnect(disconnectData.Channel,
                       disconnectData.ChannelDispatcher,
                       disconnectData.HostHandle,
                       disconnectData.TargetHandle,
                       disconnectData.ContentRoot);

            return true;
        }

        /// <summary>
        /// Do the actual work to disconnect the VisualTarget.
        /// This is called (on the channel's thread) either from
        /// DisconnectHostedVisual or from DoPendingDisconnect,
        /// depending on which thread the request arrived on.
        /// </summary>
        private void Disconnect(DUCE.Channel channel,
                                Dispatcher channelDispatcher,
                                DUCE.ResourceHandle hostHandle,
                                DUCE.ResourceHandle targetHandle,
                                DUCE.MultiChannelResource contentRoot)
        {
            channelDispatcher.VerifyAccess();

            DUCE.CompositionNode.RemoveChild(
                hostHandle,
                targetHandle,
                channel
                );

            //
            // Release the targets handle. If we had duplicated the handle,
            // then this removes the duplicated handle, otherwise just decrease
            // the ref count for VisualTarget.
            //

            contentRoot.ReleaseOnChannel(channel);

            SetFlags(channel, false, VisualProxyFlags.IsContentNodeConnected);
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
        private Dictionary<DUCE.Channel, Dispatcher> _connectedChannels = new Dictionary<DUCE.Channel, Dispatcher>();

        /// <summary>
        /// Data needed to disconnect the visual target.
        /// </summary>
        /// <remarks>
        /// This field is free-threaded and should be accessed from under a lock.
        /// It's the head of a singly-linked list of pending disconnect requests,
        /// each identified by the channel and HostVisual.  In practice, the list
        /// is either empty or has only one entry.
        /// </remarks>
        private static DisconnectData _disconnectData;

        private class DisconnectData
        {
            public DispatcherOperation DispatcherOperation { get; private set; }
            public DUCE.Channel Channel { get; private set; }
            public Dispatcher ChannelDispatcher { get; private set; }
            public HostVisual HostVisual { get; private set; }
            public DUCE.ResourceHandle HostHandle { get; private set; }
            public DUCE.ResourceHandle TargetHandle { get; private set; }
            public DUCE.MultiChannelResource ContentRoot { get; private set; }
            public DisconnectData Next { get; set; }

            public DisconnectData(DispatcherOperation op,
                                  DUCE.Channel channel,
                                  Dispatcher dispatcher,
                                  HostVisual hostVisual,
                                  DUCE.ResourceHandle hostHandle,
                                  DUCE.ResourceHandle targetHandle,
                                  DUCE.MultiChannelResource contentRoot,
                                  DisconnectData next)
            {
                DispatcherOperation = op;
                Channel = channel;
                ChannelDispatcher = dispatcher;
                HostVisual = hostVisual;
                HostHandle = hostHandle;
                TargetHandle = targetHandle;
                ContentRoot = contentRoot;
                Next = next;
            }
        }

        #endregion Private Fields
    }
}

