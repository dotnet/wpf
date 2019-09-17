// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//
//

using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Automation.Provider;
using System.Windows.Media.Composition;
using System.Runtime.InteropServices;
using System.Security;
using MS.Internal;
using MS.Win32;
using MS.Utility;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    /// <summary>
    ///
    /// </summary>
    public abstract class CompositionTarget : DispatcherObject, IDisposable, ICompositionTarget
    {
        //
        // Data types for communicating state information between
        // CompositionTarget and its host.
        //

        internal enum HostStateFlags : uint
        {
            None            = 0,
            WorldTransform  = 1,
            ClipBounds      = 2
        };

        //----------------------------------------------------------------------
        //
        //  Constructors
        //
        //----------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// CompositionTarget
        /// </summary>
        internal CompositionTarget()
        {
#if TRACE_MVR
            markVisibleCountTotal = 0;
#endif
        }

        /// <summary>
        /// This method is used to create all uce resources either on Startup or session connect
        /// </summary>
        internal virtual void CreateUCEResources(DUCE.Channel channel, DUCE.Channel outOfBandChannel)
        {
            Debug.Assert(channel != null);
            Debug.Assert(!_contentRoot.IsOnChannel(channel));

            Debug.Assert(outOfBandChannel != null);
            Debug.Assert(!_contentRoot.IsOnChannel(outOfBandChannel));

            //
            // Create root visual on the current channel and send
            // this command out of band to ensure that composition node is
            // created by the time this visual target is available for hosting
            // and to avoid life-time issues when we are working with this node
            // from the different channels.
            //

            bool resourceCreated = _contentRoot.CreateOrAddRefOnChannel(this, outOfBandChannel, s_contentRootType);
            Debug.Assert(resourceCreated);
            _contentRoot.DuplicateHandle(outOfBandChannel, channel);
            outOfBandChannel.CloseBatch();
            outOfBandChannel.Commit();
}

        /// <summary>
        /// This method is used to release all uce resources either on Shutdown or session disconnect
        /// </summary>
        internal virtual void ReleaseUCEResources(DUCE.Channel channel, DUCE.Channel outOfBandChannel)
        {
            if (_rootVisual.Value != null)
            {
                ((DUCE.IResource)(_rootVisual.Value)).ReleaseOnChannel(channel);
            }

            //
            // Release the root visual.
            //
            if (_contentRoot.IsOnChannel(channel))
            {
                _contentRoot.ReleaseOnChannel(channel);
            }

            if (_contentRoot.IsOnChannel(outOfBandChannel))
            {
                _contentRoot.ReleaseOnChannel(outOfBandChannel);
            }
        }

        #endregion Constructors

        //----------------------------------------------------------------------
        //
        //  Public Methods
        //
        //----------------------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Disposes CompositionTarget.
        /// </summary>
        public virtual void Dispose()
        {
            //
            // Here we cannot use VerifyAPI methods because they check
            // for the disposed state.
            //
            VerifyAccess();

            if (!_isDisposed)
            {
                //
                // Disconnect the root visual so that all of the child
                // animations and resources are cleaned up.
                //

                _isDisposed = true;

                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Returns true if the CompositionTarget is disposed; otherwise returns false.
        /// </summary>
        internal bool IsDisposed { get { return _isDisposed; } }

        #endregion Public Methods

        //----------------------------------------------------------------------
        //
        //  Public Properties
        //
        //----------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Gets and sets the root Visual of the CompositionTarget.
        /// </summary>
        /// <value></value>
        /// <remarks>
        ///     Callers must have UIPermission(UIPermissionWindow.AllWindows) to call this API.
        /// </remarks>
        public virtual Visual RootVisual
        {
            get
            {
                VerifyAPIReadOnly();
                return (_rootVisual.Value);
            }

            set
            {
                VerifyAPIReadWrite();
                if (_rootVisual.Value != value)
                {
                    SetRootVisual(value);

                    MediaContext.From(Dispatcher).PostRender();
                }
            }
        }

        /// <summary>
        /// Returns matrix that can be used to transform coordinates from this
        /// target to the rendering destination device.
        /// </summary>
        public abstract Matrix TransformToDevice { get; }

        /// <summary>
        /// Returns matrix that can be used to transform coordinates from
        /// the rendering destination device to this target.
        /// </summary>
        public abstract Matrix TransformFromDevice { get; }

        #endregion Public Properties

        //----------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //----------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        ///
        /// </summary>
        internal object StateChangedCallback(object arg)
        {
            object[] argArray = arg as object[];

            HostStateFlags stateFlags = (HostStateFlags)argArray[0];

            //
            // Check if world transform of the host has changed and
            // update cached value accordingly.
            //

            if ((stateFlags & HostStateFlags.WorldTransform) != 0)
            {
                _worldTransform = (Matrix)argArray[1];
            }

            //
            // Check if clip bounds have changed, update cached value.
            //

            if ((stateFlags & HostStateFlags.ClipBounds) != 0)
            {
                _worldClipBounds = (Rect)argArray[2];
            }

            //
            // Set corresponding flags on the root visual and schedule
            // render if one has not already been scheduled.
            //

            if (_rootVisual.Value != null)
            {
                //
                // When replacing the root visual, we need to re-realize all
                // content in the new tree
                //
                Visual.PropagateFlags(
                    _rootVisual.Value,
                    VisualFlags.IsSubtreeDirtyForPrecompute,
                    VisualProxyFlags.IsSubtreeDirtyForRender
                    );
            }

            return null;
        }

        void ICompositionTarget.AddRefOnChannel(DUCE.Channel channel, DUCE.Channel outOfBandChannel)
        {
            // create all uce resources.
            CreateUCEResources(channel, outOfBandChannel);
        }

        void ICompositionTarget.ReleaseOnChannel(DUCE.Channel channel, DUCE.Channel outOfBandChannel)
        {
            // release all the uce resources.
            ReleaseUCEResources(channel, outOfBandChannel);
        }

        /// <summary>
        /// Render method renders the visual tree.
        /// </summary>
        void ICompositionTarget.Render(bool inResize, DUCE.Channel channel)
        {
#if DEBUG_CLR_MEM
            bool clrTracingEnabled = false;

            if (CLRProfilerControl.ProcessIsUnderCLRProfiler &&
               (CLRProfilerControl.CLRLoggingLevel >= CLRProfilerControl.CLRLogState.Performance))
            {
                clrTracingEnabled = true;
                ++_renderCLRPass;
                CLRProfilerControl.CLRLogWriteLine("Begin_FullRender_{0}", _renderCLRPass);
            }
#endif // DEBUG_CLR_MEM

            //
            // Now we render the scene
            //

#if MEDIA_PERFORMANCE_COUNTERS
            _frameRateTimer.Begin();
#endif

            if (_rootVisual.Value != null)
            {
                bool etwTracingEnabled = false;

                if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordGeneral | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info))
                {
                    etwTracingEnabled = true;
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientPrecomputeSceneBegin, EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, PerfService.GetPerfElementID(this));
                }

#if MEDIA_PERFORMANCE_COUNTERS
                _precomputeRateTimer.Begin();
#endif
                // precompute is channel agnostic
                _rootVisual.Value.Precompute();

#if MEDIA_PERFORMANCE_COUNTERS
                _precomputeRateTimer.End();
#endif

                if (etwTracingEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientPrecomputeSceneEnd, EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info);
                }

#if DEBUG
                MediaTrace.RenderPass.Trace("Full Update");
#endif

                if (etwTracingEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(
                            EventTrace.Event.WClientCompileSceneBegin, EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, PerfService.GetPerfElementID(this));
                }

#if MEDIA_PERFORMANCE_COUNTERS
                _renderRateTimer.Begin();
#endif

                Compile(channel);

#if MEDIA_PERFORMANCE_COUNTERS
                _renderRateTimer.End();
#endif

                if (etwTracingEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(
                            EventTrace.Event.WClientCompileSceneEnd, EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info);
                }
            }

#if DEBUG_CLR_MEM
            if (clrTracingEnabled &&
                CLRProfilerControl.CLRLoggingLevel >= CLRProfilerControl.CLRLogState.Performance)
            {
                CLRProfilerControl.CLRLogWriteLine("End_FullRender_{0}", _renderCLRPass);
            }
#endif // DEBUG_CLR_MEM



#if MEDIA_PERFORMANCE_COUNTERS
            _frameRateTimer.End();
            System.Console.WriteLine("RENDERING PERFORMANCE DATA");
            System.Console.WriteLine("Frame rendering time:  " + _frameRateTimer.TimeOfLastPeriod + "ms");
            System.Console.WriteLine("Frame precompute time: " + _precomputeRateTimer.TimeOfLastPeriod + "ms");
            System.Console.WriteLine("Frame render time:     " + _renderRateTimer.TimeOfLastPeriod + "ms");
#endif
        }

        #endregion Internal Methods

        //----------------------------------------------------------------------
        //
        //  Internal Properties
        //
        //----------------------------------------------------------------------

        #region Internal Properties

        internal DUCE.MultiChannelResource _contentRoot = new DUCE.MultiChannelResource();
        internal const DUCE.ResourceType s_contentRootType = DUCE.ResourceType.TYPE_VISUAL;

        /// <summary>
        ///
        /// </summary>
        internal Matrix WorldTransform
        {
            get
            {
                return _worldTransform;
            }
        }

        internal Rect WorldClipBounds
        {
            get
            {
                return _worldClipBounds;
            }
        }
        #endregion Internal Properties

        //----------------------------------------------------------------------
        //
        //  Private Methods
        //
        //----------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// The compile method transforms the Visual Scene Graph into the Composition Scene Graph.
        /// </summary>
        private void Compile(DUCE.Channel channel)
        {
            MediaContext mctx = MediaContext.From(Dispatcher);

            Invariant.Assert(_rootVisual.Value!=null);

            // 1) Check if we have a cached render context.
            // 2) Initialize the render context.
            // 3) Call to render the scene graph (transforming it into the composition scene graph).
            // 4) Deinitalize the render context and cache it if possible.

            // ------------------------------------------------------------------------------------
            // 1) Get cached render context if possible.

            // For performance reasons the render context is cached between frames. Here we check if
            // we have a cached one. If we don't we just create a new one. If we do have one, we use
            // the render context. Note that we null out the _cachedRenderContext field. This means
            // that in failure cases we will always recreate the render context.

            RenderContext rc = null;

            Invariant.Assert(channel != null);
            if (_cachedRenderContext != null)
            {
                rc = _cachedRenderContext;
                _cachedRenderContext = null;
            }
            else
            {
                rc = new RenderContext();
            }

            // ------------------------------------------------------------------------------------
            // 2) Prepare the render context.

            rc.Initialize(channel, _contentRoot.GetHandle(channel));

            // ------------------------------------------------------------------------------------
            // 3) Compile the scene.

            if (mctx.IsConnected)
            {
               _rootVisual.Value.Render(rc, 0);
            }

            // ------------------------------------------------------------------------------------
            // 4) Cache the render context.

            Debug.Assert(_cachedRenderContext == null);
            _cachedRenderContext = rc;
        }

        /// <summary>
        /// Internal method to set the root visual.
        /// </summary>
        /// <param name="visual">Root visual, can be null, but can not be a child of another
        /// Visual.</param>
        private void SetRootVisual(Visual visual)
        {
            // We need to make this function robust by leaving the
            // _rootVisual in a consistent state.

            if (visual != null &&
                (visual._parent != null
                 || visual.IsRootElement))
            {
                // If a Visual has already a parent it can not be the root in a CompositionTarget because
                // otherwise we would have two CompositionTargets party on the same Visual tree.
                // If want to allow this we need to explicitly add support for this.
                throw new System.ArgumentException(SR.Get(SRID.CompositionTarget_RootVisual_HasParent));
            }

            DUCE.ChannelSet channelSet = MediaContext.From(Dispatcher).GetChannels();
            DUCE.Channel channel = channelSet.Channel;
            if (_rootVisual.Value != null && _contentRoot.IsOnChannel(channel))
            {
                ClearRootNode(channel);

                ((DUCE.IResource)_rootVisual.Value).ReleaseOnChannel(channel);

                _rootVisual.Value.IsRootElement = false;
            }

            _rootVisual.Value = visual;

            if (_rootVisual.Value != null)
            {
                _rootVisual.Value.IsRootElement = true;

                _rootVisual.Value.SetFlagsOnAllChannels(
                    true,
                    VisualProxyFlags.IsSubtreeDirtyForRender);
            }
        }

        /// <summary>
        /// Removes all children from the current root node.
        /// </summary>
        private void ClearRootNode(DUCE.Channel channel)
        {
            //
            // Currently we enqueue this command on the channel immediately
            // because if we put it in the delayed release queue, then
            // the _contentRoot might have been disposed by the time we
            // process the queue.
            //
            // Note: Currently we might flicker when replacing the root of the
            // compositionTarget.

            DUCE.CompositionNode.RemoveAllChildren(
                _contentRoot.GetHandle(channel),
                channel);
        }

        /// <summary>
        /// Verifies that the CompositionTarget can be accessed.
        /// </summary>
        internal void VerifyAPIReadOnly()
        {
            VerifyAccess();
            if (_isDisposed)
            {
                throw new System.ObjectDisposedException("CompositionTarget");
            }
        }

        /// <summary>
        /// Verifies that the CompositionTarget can be accessed.
        /// </summary>
        internal void VerifyAPIReadWrite()
        {
            VerifyAccess();
            if (_isDisposed)
            {
                throw new System.ObjectDisposedException("CompositionTarget");
            }

            MediaContext.From(Dispatcher).VerifyWriteAccess();
        }

        #endregion

        //----------------------------------------------------------------------
        //
        //  Private Fields
        //
        //----------------------------------------------------------------------

        #region Private Fields

        private bool _isDisposed;
        private SecurityCriticalDataForSet<Visual> _rootVisual;
        private RenderContext _cachedRenderContext;
        private Matrix _worldTransform = Matrix.Identity;

        //
        // ISSUE-ABaioura-10/19/2004: For now we assume a very large client
        // rect, because currently clip infromation cannot be robustly
        // communicated from the host. When this is fixed, we can start off
        // an empty rect; clip bounds will be updated based on the host clip.
        //
        private Rect _worldClipBounds = new Rect(
                     Double.MinValue / 2.0,
                     Double.MinValue / 2.0,
                     Double.MaxValue,
                     Double.MaxValue);
#if DEBUG_CLR_MEM
        //
        // Used for CLRProfiler comments.
        //
        private static int _renderCLRPass = 0;
#endif // DEBUG_CLR_MEM


#if MEDIA_PERFORMANCE_COUNTERS
        private HFTimer _frameRateTimer;
        private HFTimer _precomputeRateTimer;
        private HFTimer _renderRateTimer;
#endif

        #endregion Private Fields


        //----------------------------------------------------------------------
        //
        //  Static Events
        //
        //----------------------------------------------------------------------
        #region Static Events
        /// <summary>
        /// Rendering event.  Registers a delegate to be notified after animation and layout but before rendering
        /// </summary>
        public static event EventHandler Rendering
        {
            add
            {
                MediaContext mc = MediaContext.From(Dispatcher.CurrentDispatcher);
                mc.Rendering += value;

                // We need to get a new rendering operation queued.
                mc.PostRender();
            }
            remove
            {
                MediaContext mc = MediaContext.From(Dispatcher.CurrentDispatcher);
                mc.Rendering -= value;
            }
        }

        #endregion
    }
}

