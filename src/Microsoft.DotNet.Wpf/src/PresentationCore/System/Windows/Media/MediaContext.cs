// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//
//
// Description:
//      The MediaContext class controls the media layer.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Composition;
using System.Security;
using System.Windows.Media.Effects;

using MS.Internal;
using MS.Internal.PresentationCore;
using MS.Utility;
using MS.Win32;

using Microsoft.Win32.SafeHandles;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    /// <summary>
    /// The MediaContext class controls the media layer.
    /// </summary>
    /// <remarks>
    /// Use <see cref="MediaSystem.Startup"/> to start up the media system and <see cref="MediaSystem.Shutdown"/> to
    /// shut down the media system.
    /// <seealso cref="CompositionTarget"/>
    /// </remarks>
    internal partial class MediaContext : DispatcherObject, IDisposable, IClock
    {
        /// <summary>
        /// Initializes the MediaContext's clock service.
        /// </summary>
        static MediaContext()
        {
            long qpcCurrentTime;

            SafeNativeMethods.QueryPerformanceFrequency(out _perfCounterFreq);

            if (IsClockSupported)
            {
                SafeNativeMethods.QueryPerformanceCounter(out qpcCurrentTime);
            }
            else
            {
                qpcCurrentTime = 0;
            }

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordGraphics, EventTrace.Event.WClientQPCFrequency, _perfCounterFreq, qpcCurrentTime);
        }

        /// <summary>
        /// Returns true if the MediaContext can return current time values,
        /// false otherwise.
        /// </summary>
        internal static bool IsClockSupported
        {
            get
            {
                return _perfCounterFreq != 0;
            }
        }

        /// <summary>
        /// Converts a time value expressed in "counts" (as returned by a call
        /// to QueryPerformanceCounter) to "ticks".  A Tick is the smallest
        /// time unit expressable by a TimeSpan and is equal to 100ns
        /// </summary>
        /// <param name="counts"></param>
        /// <returns></returns>
        private static long CountsToTicks(long counts)
        {
            // The following expression retains precision while avoiding overflow:
            return (long)(TimeSpan.TicksPerSecond * (counts / _perfCounterFreq) + (TimeSpan.TicksPerSecond * (counts % _perfCounterFreq)) / _perfCounterFreq);
        }

        /// <summary>
        /// Converts a time value expressed in "ticks" to an estimate of a count
        /// (as returned by a call to QueryPerformanceCounter)
        /// </summary>
        /// <param name="ticks"></param>
        /// <returns></returns>
        private static long TicksToCounts(long ticks)
        {
            return (long)(_perfCounterFreq * (ticks / TimeSpan.TicksPerSecond) + (_perfCounterFreq * (ticks % TimeSpan.TicksPerSecond)) / TimeSpan.TicksPerSecond);
        }

        /// <summary>
        /// Finds out whether a number is prime or not
        /// </summary>
        /// <remarks>
        /// Fails on 2 by saying that it is not prime but we won't call the
        /// method with 2 as input
        /// </remarks>
        private static bool IsPrime(int number)
        {
            // If the number is even then it's not prime.
            // This is WRONG for 2 but we don't get called with 2.
            if ((number & 1) == 0)
                return false;

            int sqrt = (int) Math.Sqrt(number);

            for (int i = 3; i <= sqrt; i += 2)
            {
                if (number % i == 0)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Find the next prime number greater than number
        /// </summary>
        private static int FindNextPrime(int number)
        {
            while (!IsPrime(++number))
            {
                // Nothing to do
            }

            return number;
        }

        #region Compat support for rendering in a Non-interactive Window Station

        /// <summary>
        /// General case: 
        ///     True if our window station is interactive (WinSta0), otherwise false. 
        ///     In addition to this, two compatibility switches are provided to opt-in 
        ///     or opt-out of this behavior
        ///     
        /// Compatibility switches
        ///     i. <see cref=" MS.Internal.CoreAppContextSwitches.ShouldRenderEvenWhenNoDisplayDevicesAreAvailable"/> 
        ///     ii. <see cref="MS.Internal.CoreAppContextSwitches.ShouldNotRenderInNonInteractiveWindowStation"/>
        /// 
        /// How this will work:
        ///     Desktop/Interactive Window Stations:
        ///         Rendering will be throttled back/stopped when no display devices are available. For e.g., when a TS 
        ///         session is in WTSDisconnected state, the OS may not provide any display devices in response to our enumeration.
        ///         If an application would like to continue rendering in the absence of display devices (accepting that 
        ///         it can lead to a CPU spike), it can set <see cref=" MS.Internal.CoreAppContextSwitches.ShouldRenderEvenWhenNoDisplayDevicesAreAvailable"/> 
        ///         to true.
        ///     Service/Non-interactive Window Stations
        ///         Rendering will continue by default, irrespective of the presence of display devices.Unless the WPF
        ///         API's being used are shortlived (like rendering to a bitmap), it can lead to a CPU spike. 
        ///         If an application running inside a service would like to receive the 'default' WPF behavior, 
        ///         i.e., no rendering in the absence of display devices, then it should set
        ///         <see cref="MS.Internal.CoreAppContextSwitches.ShouldNotRenderInNonInteractiveWindowStation"/> to true
        ///     In pseudocode, 
        ///         IsNonInteractiveWindowStation = !Environment.UserInteractive
        ///         IF DisplayDevicesNotFound() THEN
        ///             IF IsNonInteractiveWindowStation THEN 
        ///                 // We are inside a SCM service
        ///                 // Default = True, AppContext switch can override it to False
        ///                 ShouldRender = !CoreAppContextSwitches.ShouldNotRenderInNonInteractiveWindowStation
        ///             ELSE 
        ///                 // Desktop/interactive mode, including WTSDisconnected scenarios
        ///                 // Default = False, AppContext switch can override it to True
        ///                 ShouldRender = CoreAppContextSwitches.ShouldRenderEvenWhenNoDisplayDevicesAreAvailable
        ///             END IF
        ///         END IF
        ///     
        /// </summary>
        /// <remarks>
        /// i. <see cref=">Environment.UserInteractive"/> calls into Window Station related
        /// Win32 API's to identify whether the current Window Station has WSF_VISIBLE 
        /// flag set. 
        /// 
        /// ii. Field is internal to allow <see cref="HwndTarget"/> to consume its value
        /// 
        /// iii. This field is named to reflect the general use-case, namely to force rendering 
        /// when inside a SCM service. 
        /// </remarks>
        internal static bool ShouldRenderEvenWhenNoDisplayDevicesAreAvailable { get; } =
            !Environment.UserInteractive ? // IF DisplayDevicesNotAvailable && IsNonInteractiveWindowStation/IsService...  
                !CoreAppContextSwitches.ShouldNotRenderInNonInteractiveWindowStation :      // THEN render by default, allow ShouldNotRender AppContext override 
                CoreAppContextSwitches.ShouldRenderEvenWhenNoDisplayDevicesAreAvailable;   // ELSE do not render by default, allow ShouldRender AppContext override


        #endregion

        /// <summary>
        /// The MediaContext lives in the Dispatcher and is the MediaSystem's class that keeps
        /// per Dispatcher state.
        /// </summary>
        internal MediaContext(Dispatcher dispatcher)
        {
            // We create exactly one MediaContext per thread. This is the one
            // for this thread
            Debug.Assert(dispatcher.Reserved0 == null);

            // Initialize frame time information
            if (IsClockSupported)
            {
                SafeNativeMethods.QueryPerformanceCounter(out _lastPresentationTime);
                _estimatedNextPresentationTime = TimeSpan.FromTicks(CountsToTicks(_lastPresentationTime));
            }

            // Generate a unique id for our context so that we can pass this along to
            // CreateHWNDRenderTarget
            _contextGuid = Guid.NewGuid();

            // Create a dictionary in which we manage the CompositionTargets.
            _registeredICompositionTargets = new HashSet<ICompositionTarget>();

            _renderModeMessage = new DispatcherOperationCallback(InvalidateRenderMode);

            // Create a notification window to listen for broadcast window messages
            _notificationWindow = new MediaContextNotificationWindow(this);

            // Connect to the MediaSystem.
            if (MediaSystem.Startup(this))
            {
                _isConnected = MediaSystem.ConnectChannels(this);
            }

            // Subscribe to the OnDestroyContext event so that we can cleanup our state.
            _destroyHandler = new EventHandler(this.OnDestroyContext);
            Dispatcher.ShutdownFinished += _destroyHandler;

            _renderMessage = new DispatcherOperationCallback(RenderMessageHandler);
            _animRenderMessage = new DispatcherOperationCallback(AnimatedRenderMessageHandler);
            _inputMarkerMessage = new DispatcherOperationCallback(InputMarkerMessageHandler);

            // We hold off connecting ourselves to the dispatcher until we are sure that
            // initialization will complete successfully.  In rare cases, function calls
            // earlier in this constructor throw exceptions, resulting in the MediaContext
            // being left in an uninitialized state; however, the Dispatcher could call methods
            // on the MediaContext, resulting in unpredictable behaviour.

            //
            // NOTE: We must attach to the Dispatcher before creating a TimeManager,
            // otherwise we will create a circular function loop where TimeManager attempts
            // to create a Clock, which attempts to locate a MediaContext, which attempts to
            // create a TimeManager, resulting in a stack overflow.



            dispatcher.Reserved0 = this;

            _timeManager = new TimeManager();
            _timeManager.Start();
            _timeManager.NeedTickSooner += new EventHandler(OnNeedTickSooner);

            _promoteRenderOpToInput = new DispatcherTimer(DispatcherPriority.Render);
            _promoteRenderOpToInput.Tick += new EventHandler(PromoteRenderOpToInput);
            _promoteRenderOpToRender = new DispatcherTimer(DispatcherPriority.Render);
            _promoteRenderOpToRender.Tick += new EventHandler(PromoteRenderOpToRender);
            _estimatedNextVSyncTimer = new DispatcherTimer(DispatcherPriority.Render);
            _estimatedNextVSyncTimer.Tick += new EventHandler(EstimatedNextVSyncTimeExpired);

            _commitPendingAfterRender = false;
        }

        /// <summary>
        /// Called by a message processor to notify us that our asynchronous
        /// channel has outstanding messages that need to be pumped.
        /// </summary>
        internal void NotifySyncChannelMessage(DUCE.Channel channel)
        {
            // empty the channel messages.
            DUCE.MilMessage.Message message;
            while (channel.PeekNextMessage(out message))
            {
                switch (message.Type)
                {
                    case DUCE.MilMessage.Type.Caps:
                    case DUCE.MilMessage.Type.SyncModeStatus:
                    case DUCE.MilMessage.Type.Presented:
                        break;
                    case DUCE.MilMessage.Type.PartitionIsZombie:
                        // we remove the sync channels so that if the app handles the exception
                        // it will get a new partition on the next sync render request.
                        _channelManager.RemoveSyncChannels();
                        NotifyPartitionIsZombie(message.HRESULTFailure.HRESULTFailureCode);
                        break;

                    default:
                        HandleInvalidPacketNotification();
                        break;
                }
            }
        }

        /// <summary>
        /// Called by a message processor to notify us that our asynchronous
        /// channel has outstanding messages that need to be pumped.
        /// </summary>
        internal void NotifyChannelMessage()
        {
            // Since a notification message may sit in the queue while we
            // disconnect, we need to check that we actually have a channel
            // when we receive this notification. If not, there's no harm;
            // just skip the operation
            if (Channel != null)
            {
                DUCE.MilMessage.Message message;
                while (Channel.PeekNextMessage(out message))
                {
                    switch (message.Type)
                    {
                        case DUCE.MilMessage.Type.Caps:
                            NotifySetCaps(message.Caps.Caps);
                            break;

                        case DUCE.MilMessage.Type.SyncModeStatus:
                            NotifySyncModeStatus(message.SyncModeStatus.Enabled);
                            break;

                        case DUCE.MilMessage.Type.Presented:
                            NotifyPresented(
                                message.Presented.PresentationResults,
                                message.Presented.PresentationTime,
                                message.Presented.RefreshRate
                                );
                            break;

                        case DUCE.MilMessage.Type.PartitionIsZombie:
                            NotifyPartitionIsZombie(message.HRESULTFailure.HRESULTFailureCode);
                            break;

                        case DUCE.MilMessage.Type.BadPixelShader:
                            NotifyBadPixelShader();
                            break;

                        default:
                            HandleInvalidPacketNotification();
                            break;
                    }
                }
            }
        }

        // MediaSystem is per-AppDomain and so it uses this to ensure that InvalidateRenderMode() 
        // is called by the right thread.
        internal void PostInvalidateRenderMode()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, _renderModeMessage, null);
        }

        /// <summary>
        /// Tells all of the HwndTargets to InvalidateRenderMode()
        /// </summary>
        private object InvalidateRenderMode(object dontCare)
        {
            Debug.Assert(CheckAccess());
            
            foreach (ICompositionTarget target in _registeredICompositionTargets)
            {
                HwndTarget hwndTarget = target as HwndTarget;

                if (hwndTarget != null)
                {
                    hwndTarget.InvalidateRenderMode();
                }
            }

            return null;
        }

        /// <summary>
        /// NotifySetCaps - this method is called to update the graphics caps
        /// If the new render tier is different from the previous render tier,
        /// this method will notify all TierChanged listeners.
        /// </summary>
        /// <param name="tier"> int - the new render tier </param>
        private void NotifySetCaps(MilGraphicsAccelerationCaps caps)
        {
            PixelShaderVersion = caps.PixelShaderVersion;
            MaxPixelShader30InstructionSlots = caps.MaxPixelShader30InstructionSlots;
            HasSSE2Support = Convert.ToBoolean(caps.HasSSE2Support);
            MaxTextureSize = new Size(caps.MaxTextureWidth, caps.MaxTextureHeight);

            int tier = caps.TierValue;
            if (_tier != tier)
            {
                _tier = tier;

                if (TierChanged != null)
                {
                    TierChanged(null, null);
                }
            }
        }

        /// <summary>
        /// Internal event which is raised when a bad pixel shader is detected on this MediaContext.
        /// </summary>
        internal event EventHandler InvalidPixelShaderEncountered;

        /// <summary>
        /// NotifyBadPixelShader - this method is called when the render
        /// thread has detected a bad pixel shader.  The render thread continues
        /// to raise this until the problem's been corrected.  This method
        /// invokes the listeners on the static event in PixelShader.
        /// </summary>
        private void NotifyBadPixelShader()
        {
            if (InvalidPixelShaderEncountered != null)
            {
                InvalidPixelShaderEncountered(null, null);
            }
            else
            {
                // It's never correct to not have an event handler hooked up in
                // the case when an invalid shader is encountered.  Raise an
                // exception directing the app to hook up an event handler.
                throw new InvalidOperationException(SR.Get(SRID.MediaContext_NoBadShaderHandler));
            }
        }

        /// <summary>
        /// The partition this media context is connected to went into
        /// zombie state. This means either an unhandled batch processing,
        /// rendering or presentation error and will require us to reconnect.
        /// </summary>
        private void NotifyPartitionIsZombie(int failureCode)
        {
            //
            // We only get back these kinds of notification:-
            // For all OOM cases, we get E_OUTOFMEMORY.
            // For all OOVM cases, we get D3DERR_OUTOFVIDEOMEMORY and
            // for all other errors we get WGXERR_UCE_RENDERTHREADFAILURE.
            //

            switch (failureCode)
            {
            case HRESULT.E_OUTOFMEMORY:
                throw new System.OutOfMemoryException();
            case HRESULT.D3DERR_OUTOFVIDEOMEMORY:
                throw new System.OutOfMemoryException(SR.Get(SRID.MediaContext_OutOfVideoMemory));
            default:
                throw new System.InvalidOperationException(SR.Get(SRID.MediaContext_RenderThreadError));
            }
        }

        /// <summary>
        /// The back channel processed a malformed packet and so gives
        /// the notification of invalid packet.
        /// </summary>
        private void HandleInvalidPacketNotification()
        {
            //
            // -
            // For now we ignore the packet and continue processing
            // other packets. In future, we could also close this channel.
            //
        }

        /// <summary>
        /// Tier Property - returns the current render tier for this MediaContext.
        /// </summary>
        internal int Tier
        {
            get
            {
                return _tier;
            }
        }

        /// <summary>
        /// PixelShaderVersion Property - returns the current PixelShader
        /// (major<<16|minor) version
        /// </summary>
        internal UInt32 PixelShaderVersion
        {
            get;
            private set;
        }

        /// <summary>
        /// MaxPixelShader30InstructionSlots Property - returns the max number of instruction
        /// slots for PS 3.0
        /// </summary>
        internal UInt32 MaxPixelShader30InstructionSlots
        {
            get;
            private set;
        }

        /// <summary>
        /// HasSSE2Support Property - returns true if the processor supports SSE2 instructions
        /// </summary>
        internal Boolean HasSSE2Support
        {
            get;
            private set;
        }

        /// <summary>
        /// MaxTextureSize Property - returns the max texture width and height creatable by the
        /// underlying hardware.  The API returns the minimum values across all available hardware devices.
        /// </summary>
        internal Size MaxTextureSize
        {
            get;
            private set;
        }
        
        /// <summary>
        /// Internal event which is raised when the Tier changes on this MediaContext.
        /// </summary>
        internal event EventHandler TierChanged;

        /// <summary>
        /// Asks the composition engine to retrieve the current hardware tier.
        /// This tier will be sent back via NotifyChannelMessage.
        /// </summary>
        private void RequestTier(DUCE.Channel channel)
        {
            unsafe
            {
                DUCE.MILCMD_CHANNEL_REQUESTTIER data;

                //
                // Ask for the hardware tier information for the primary display
                //

                data.Type = MILCMD.MilCmdChannelRequestTier;
                data.ReturnCommonMinimum = 0; // false

                channel.SendCommand(
                    (byte*)&data,
                    sizeof(DUCE.MILCMD_CHANNEL_REQUESTTIER)
                    );
            }
        }

        /// <summary>
        /// Schedule the next rendering operation based on our presentation
        /// mode and the next time we will have active animations.
        /// </summary>
        /// <param name="minimumDelay">
        /// Specifies the minimum time before making the next rendering operation
        /// active
        /// </param>
        private void ScheduleNextRenderOp(TimeSpan minimumDelay)
        {
            //
            // If _needToCommitChannel is true, then we are in a waiting state and we've
            // already rendered a new frame. We don't want to render again until we've
            // hit the next VSync at which point we'll commit the rendered frame.
            // Note that we will still render again if someone explicitely forces a
            // render (PostRender). When we do hit the next VSync and commit the channel
            // we will schedule the next render operation.
            //
            if (!_isDisconnecting && !_needToCommitChannel)
            {
                //
                // This is the time at which the next animation will be active
                // in ms. (a negative value represents no animations are active)
                //
                TimeSpan nextTickNeeded = TimeSpan.Zero;

                //
                // If we have one or more active Rendering events it's the same
                // as having an active animation so we know that we'll need to
                // render another frame.
                //
                if (Rendering == null)
                {
                    nextTickNeeded = _timeManager.GetNextTickNeeded();
                }

                //
                // If we have a tick in the future, make sure that it's not before
                // the minimum delay requested.
                //
                if (nextTickNeeded >= TimeSpan.Zero)
                {
                    nextTickNeeded = TimeSpan.FromTicks(Math.Max(nextTickNeeded.Ticks, minimumDelay.Ticks));
                    EnterInterlockedPresentation();
                }
                else
                {
                    LeaveInterlockedPresentation();
                }

                // We need to tick in the distant future, schedule a far way message
                if (nextTickNeeded > TimeSpan.FromSeconds(1))
                {
                    if (_currentRenderOp == null)
                    {
                        _currentRenderOp = Dispatcher.BeginInvoke(DispatcherPriority.Inactive, _animRenderMessage, null);

                        _promoteRenderOpToRender.Interval = nextTickNeeded;
                        _promoteRenderOpToRender.Start();
                    }
                }
                // We need to tick soon (< 1 second)
                else if (nextTickNeeded > TimeSpan.Zero)
                {
                    // Only create a new render op if we don't have one
                    // scheduled
                    if (_currentRenderOp == null)
                    {
                        _currentRenderOp = Dispatcher.BeginInvoke(DispatcherPriority.Inactive, _animRenderMessage, null);

                        _promoteRenderOpToInput.Interval = nextTickNeeded;
                        _promoteRenderOpToInput.Start();

                        _promoteRenderOpToRender.Interval = TimeSpan.FromSeconds(1);
                        _promoteRenderOpToRender.Start();
                    }
                }
                else if (nextTickNeeded == TimeSpan.Zero)
                {
                    Debug.Assert(InterlockIsEnabled,
                        "If we are not in Interlocked Mode, we should always have a delay");

                    DispatcherPriority priority = DispatcherPriority.Render;

                    //
                    // We normally want to schedule rendering at Render priority, however if something at
                    // render priority takes more than a frame it will block input from ever being processed.
                    // To prevent this we create an operation at input priority which lets us know how long
                    // input has been blocked for.  If it's blocked for too long we should schedule our render
                    // at Input priority so that input can flush before we start another render.
                    //
                    if (_inputMarkerOp == null)
                    {
                        _inputMarkerOp = Dispatcher.BeginInvoke(DispatcherPriority.Input, _inputMarkerMessage, null);
                        _lastInputMarkerTime = CurrentTicks;
                    }
                    else if (CurrentTicks - _lastInputMarkerTime > MaxTicksWithoutInput)
                    {
                        priority = DispatcherPriority.Input;
                    }

                    // Schedule an operation to happen immediately.
                    if (_currentRenderOp == null)
                    {
                        _currentRenderOp = Dispatcher.BeginInvoke(priority, _animRenderMessage, null);
                    }
                    else
                    {
                        _currentRenderOp.Priority = priority;
                    }

                    _promoteRenderOpToInput.Stop();
                    _promoteRenderOpToRender.Stop();
                }

                //
                // Trace the scheduling of the render
                //
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordGraphics, EventTrace.Event.WClientScheduleRender, nextTickNeeded.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Commits the channel, but only after the next vblank has occured.
        /// </summary>
        private void CommitChannelAfterNextVSync()
        {
            if (_animationRenderRate != 0)
            {
                //
                // estimate the next vblank interval and set our timer interval to wake up at this time.
                // we add 1ms to the estimated time because are estimated time make not be perfectly accurate
                // and its more important that wake up after the vblank than right on it.
                //

                long currentTicks = CurrentTicks;
                long earliestWakeupTicks = currentTicks + TicksUntilNextVsync(currentTicks) + TimeSpan.TicksPerMillisecond;
                _estimatedNextVSyncTimer.Interval = TimeSpan.FromTicks(earliestWakeupTicks - currentTicks);
                _estimatedNextVSyncTimer.Tag = earliestWakeupTicks;
            }
            else
            {
                // It's possible our first notification from the UCE didn't give us the
                // refresh rate.  We can't estimate when vsync will be - try again in about a vblank
                _estimatedNextVSyncTimer.Interval = TimeSpan.FromMilliseconds(17);
            }

            _estimatedNextVSyncTimer.Start();

            //
            // We are waiting for the next VBlank to occur
            //
            _interlockState = InterlockState.WaitingForNextFrame;
            _lastPresentationResults = MIL_PRESENTATION_RESULTS.MIL_PRESENTATION_NOPRESENT;
        }

        /// <summary>
        /// Processes the Presented composition engine notification.
        /// </summary>
        /// <param name="presentationResults">
        /// The results of the last presentation.
        /// </param>
        /// <param name="presentationTime">
        /// The timestamp of the last presentation.
        /// </param>
        /// <param name="displayRefreshRate">
        /// The current display refresh rate.
        /// </param>
        private void NotifyPresented(
            MIL_PRESENTATION_RESULTS presentationResults,
            long presentationTime,
            int displayRefreshRate
            )
        {
            if (InterlockIsEnabled)
            {
                Debug.Assert(_interlockState == InterlockState.WaitingForResponse,
                    "We should not be getting a notification unless we asked for one");

                //
                // The composition engine has presented, so we are ready to start
                // another frame, if necessary. Also remember the presentation
                // time to use as the basis for estimating the time for the next
                // frame.
                //

                //
                // presentationDelay represents the time we want to wait until
                // we activate a new render operation.
                //
                TimeSpan presentationDelay = TimeSpan.Zero;
                _lastPresentationResults = presentationResults;

                //
                // The UCE has processed our frame. So we are not waiting on it
                // anymore. Set our state to idle.
                //
                _interlockState = InterlockState.Idle;

                switch (presentationResults)
                {
                    case MIL_PRESENTATION_RESULTS.MIL_PRESENTATION_VSYNC:
                        {
                            // Adjust the refresh rate to prevent constant tearing
                            // on the screen. We've chosen NextPrime(RefreshRate+5)
                            // as a function that seems to look good at all
                            // popular refresh rates.
                            // Only update the adjusted refresh rate when the
                            // display refresh rate changes. If changing this
                            // make sure to look at the performance of the lookup
                            // of the adjusted refresh rate.
                            if (displayRefreshRate != _displayRefreshRate)
                            {
                                _displayRefreshRate = displayRefreshRate;
                                _adjustedRefreshRate = FindNextPrime(displayRefreshRate + 5);
                            }

                            // VSync means that the UCE has presented at the time we
                            // requested, but it can still tear, so if the user has
                            // requested a high framerate through DFR, override the
                            // monitor refresh rate with this DFR.
                            _animationRenderRate = Math.Max(_adjustedRefreshRate, _timeManager.GetMaxDesiredFrameRate());
                            _lastPresentationTime = presentationTime;
                        }
                        break;

                    case MIL_PRESENTATION_RESULTS.MIL_PRESENTATION_VSYNC_UNSUPPORTED:
                        {
                            //
                            // If we don't support VSync then wait a small delay so that we don't
                            // just overrun the UCE
                            //
                            presentationDelay = _timeDelay;
                        }
                        break;

                    case MIL_PRESENTATION_RESULTS.MIL_PRESENTATION_NOPRESENT:
                        {
                            //
                            // We didn't present because the scene didn't change.
                            // Since the UCE returned early, the vblank at which
                            // we were hoping to present the last frame has not
                            // occurred yet.  We will set a timer to trigger at
                            // the time at which we think that vblank will occur
                            // (with a fudge factor of 1 ms to ensure we don't
                            // wake up before)
                            //
                            // Until the timer expires we will not send any updates
                            // to the UCE because we don't know if we'll be able to
                            // hit the vblank. We will update the Visual tree and
                            // queue the UCE changes, but we will not commit yet.
                            //

                            Debug.Assert(!InterlockIsWaiting,
                                "We should not be waiting at this point");

                            CommitChannelAfterNextVSync();
                        }
                        break;

                        // This return code represents that we've presented with
                        // the DWM, so there is no tearing, we don't need to
                        // override the refresh rate in this case.
                    case MIL_PRESENTATION_RESULTS.MIL_PRESENTATION_DWM:
                        {
                            // In the DWM case these values are actually correct, so we update them here
                            _animationRenderRate = displayRefreshRate;
                            _lastPresentationTime = presentationTime;
                        }
                        break;
                }

                // Cap our Animation RenderRate to 1000 fps.
                _animationRenderRate = Math.Min(_animationRenderRate, 1000);

                //
                // Trace the notification from the UCE
                //
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WClientUceNotifyPresent, _lastPresentationTime, (Int64)presentationResults);

                if (presentationResults == MIL_PRESENTATION_RESULTS.MIL_PRESENTATION_NOPRESENT)
                {
                    // dont commit or schedule because we've created a timer to do this.
                    // the only reason we're waiting until here to do this is because we want the ETW event to fire
                    return;
                }
                else
                {
                    // if we get any result other than a NOPRESENT then we should stop the _estimatedNextVSyncTimer timer
                    // so we dont end up commiting twice in 1 vblank interval
                    _estimatedNextVSyncTimer.Stop();
                }

                //
                // We want to schedule the next render operation before commiting the
                // channel so that we can send the command right away.

                //
                // If we already had a render occur then we've ticked the
                // time manager and layout has been updated send this frame
                // to the UCE.
                //

                if (!InterlockIsWaiting && _needToCommitChannel)
                {
                    //
                    // if we've already commit during this vblank interval, dont do it again
                    // until the following vblank
                    //

                    if (HasCommittedThisVBlankInterval)
                    {
                        CommitChannelAfterNextVSync();
                        return;
                    }

                    CommitChannel();

                    Debug.Assert(InterlockIsWaiting,
                        "We had something to commit, we should be waiting for that"+
                        "notification to come back");
               }

                ScheduleNextRenderOp(presentationDelay);
            }
        }


        /// <summary>
        /// Return true if the current time is within the same vblank interval as our last commit time
        /// </summary>
        private bool HasCommittedThisVBlankInterval
        {
            get
            {
                if (_animationRenderRate == 0)
                    return false;

                // is our last commit within 1 refresh period of our current time?
                // Need to investigate if throttling will break this
                if (CurrentTicks - _lastCommitTime < RefreshPeriod)
                {
                    // if the last commit is later than the last presentation, then we
                    // have committed a frame and haven't been notified of it yet.
                    // in this case, we dont want to commit the channel again
                    if (_lastCommitTime > CountsToTicks(_lastPresentationTime))
                        return true;
                }

                return false;
            }
        }


        /// <summary>
        /// Returns the current time in Ticks (100 ns intervals).
        /// </summary>
        internal static long CurrentTicks
        {
            get
            {
                long counts;
                SafeNativeMethods.QueryPerformanceCounter(out counts);
                return CountsToTicks(counts);
            }
        }

        //
        //private long CurrentTimeInMs
        //{
        //    get
        //    {
        //        return CurrentTicks / TimeSpan.TicksPerMillisecond;
        //    }
        //}
        //

        /// <summary>
        /// Returns the time in Ticks (100 ns intervals) between vsyncs.
        /// It's up to the caller to ensure that _animationRenderRate is valid.
        /// </summary>
        private long RefreshPeriod
        {
            get
            {
                return TimeSpan.TicksPerSecond / _animationRenderRate;
            }
        }


        /// <summary>
        /// Computes the number of ticks since the last UCE present
        /// </summary>
        /// <param name="currentTime"></param>
        /// <returns></returns>
        private long TicksSinceLastPresent(long currentTime)
        {
            return currentTime - CountsToTicks(_lastPresentationTime);
        }


        /// <summary>
        /// Estimates the time in Ticks (100 ns intervals) since the
        /// last vsync.  It starts with the last presentation time
        /// and extrapolates based on the display refresh rate.
        /// </summary>
        private long TicksSinceLastVsync(long currentTime)
        {
            return TicksSinceLastPresent(currentTime) % RefreshPeriod;
        }


        /// <summary>
        /// Estimates the number of ticks until the next vsync
        /// </summary>
        /// <param name="currentTime"></param>
        /// <returns></returns>
        private long TicksUntilNextVsync(long currentTime)
        {
            return RefreshPeriod - TicksSinceLastVsync(currentTime);
        }

        /// <summary>
        /// Processes the SyncMode composition engine notification.
        /// </summary>
        /// <param name="enabledResult">
        /// The HRESULT of enabling sync mode.
        /// </param>
        private void NotifySyncModeStatus(int enabledResult)
        {
            //
            // Only process the notification if we asked to start interlocked
            // presentation mode
            //
            if (_interlockState == InterlockState.RequestedStart)
            {
                if (enabledResult >= 0)
                {
                    //
                    // We succeded in entering the interlocked mode in the
                    // composition engine.
                    //

                    _interlockState = InterlockState.Idle;

                    if (Channel != null)
                    {
                        // SyncFlush will Commit()
                        if (CommittingBatch != null)
                        {
                            CommittingBatch(Channel, new EventArgs());
                        }
                        
                        Channel.SyncFlush();
                    }
                }
                else
                {
                    _interlockState = InterlockState.Disabled;
                }
            }
        }

        /// <summary>
        /// Estimates the timestamp of the next frame to be presented
        /// </summary>
        /// <remarks>
        ///
        /// How the current time is computed
        /// ================================
        ///
        /// The MediaContext keeps track of when frames are presented to the
        /// display. The "current time" for the MediaContext is actually the
        /// time at which we estimate the next frame to be presented. This
        /// allows the system to produce a frame that is correct at the time
        /// it is seen by the user, leading to smooth animations. The main
        /// assumption at this time is that a frame can be compiled, composed
        /// and presented before the next vertical refresh, whenever that may
        /// be. A future refinement of this algorithm will take historical
        /// profiling data into account to compute a more accurate target
        /// presentation frame.
        ///
        ///
        /// The next frame time is estimated by taking the current system
        /// time and rounding that up to the next refresh time, based on the
        /// last refresh time and the current display refresh rate.
        ///
        /// This is the situation when we are ready to submit a new frame:
        ///
        ///   refresh:   -|  F  |-
        /// --+----[+]----+-----+-----+--*-(+)----+-----+  t
        ///         L                    C  N
        ///         last frame        now   next frame
        ///
        ///
        /// In order to pipeline the UI thread and the composition thread we
        /// will try to start rendering the next frame while the composition
        /// is presenting the last frame that we've sent it. This allows us to
        /// make use of 2 CPUs more efficiently (UI thread can renders 1 frame
        /// while the composition thread is presenting another). So when
        /// calculating the current time, if we are waiting on a frame already
        /// then we will produce the frame for the VSync after. That means if we
        /// are in the wait state then the next presentation time is not the
        /// next refresh time, but at least *two* refresh times in the future,
        /// because the next refresh is when the *last* frame will be presented.
        ///
        /// This is the situation when we are in a wait state:
        ///
        ///   refresh:   -|  F  |-
        /// --+----[+]----+-----+-----+--*--+----(+)----+  t
        ///         L                    C        N
        ///         last frame        now         next frame
        ///
        ///
        /// The computation of the next frame time "N", then, involves four
        /// variables:
        ///
        ///     Variable                            Units
        ///     --------------------------------------------------------------
        ///     L   The last presentation time      Ticks
        ///     C   The current actual time         Ticks
        ///     R   The display refresh rate        frames per second
        ///     W   The wait state                  boolean -- true if waiting
        ///
        /// The computation is fairly straightforward.  We take the time since
        /// we've last presented (C - L) and mod it with the refresh rate (R) to get
        /// the time since the last vsync.  We can then get the time until the next
        /// vsync by subtracting the refresh rate (R) from that value.
        /// TicksSinceLastVsync() and TicksUntilNextVsync() implement this.
        ///
        /// If we're not waiting we can render at the next vsync.  If we are waiting,
        /// we'll wait until the vsync after. This computation can get slightly
        /// hairy if we're exactly on a frame boundary; the comments inside
        /// IClock.CurrentTime explain this a bit more.
        ///
        /// Each time the composition engine notifies us that a frame
        /// was presented it also tells us the timestamp for that frame. At
        /// that time we also leave the wait state. If we successfully present
        /// on every refresh then two consecutive computations should give the
        /// same result.  However,  the values of L we get from the composition
        /// engine are subject to fluctuations (due to thread scheduling issues),
        /// so we may in fact compute different time values, either earlier or later.
        /// To avoid thrashing the Tick and Layout processes we keep the previous
        /// estimate if the new estimate is within half a frame of it. If the
        /// new estimate is later than that then it means the previous estimate
        /// was too inaccurate, potentially because we actually took longer
        /// than a single refresh to compile, compose and present the last
        /// frame. In that case we abandon the previous estimated value.
        ///
        /// </remarks>
        TimeSpan IClock.CurrentTime
        {
            get
            {
                Debug.Assert(IsClockSupported, "MediaContext.CurrentTime called when QueryPerformaceCounter is not supported");

                long counts;
                SafeNativeMethods.QueryPerformanceCounter(out counts);

                long countsTicks = CountsToTicks(counts);

                if (_interlockState != InterlockState.Disabled)
                {
                    //
                    // On the first frame we haven't yet received information about
                    // the display refresh rate from the compositor. In that case
                    // we can't snap to frame times, so we simply use the current
                    // time.
                    //

                    if (   _animationRenderRate != 0
                        && _lastPresentationResults != MIL_PRESENTATION_RESULTS.MIL_PRESENTATION_VSYNC_UNSUPPORTED)
                    {
                        //
                        // Figure out where we are in the vsync period. The entire computation
                        // is done in TimeSpan Ticks (100ns intervals)
                        //

                        long nextVsyncTicks;        // Absolute time in ticks at which the next vsync will occur
                        long vsyncAdvance;          // We expect to present this many vsyncs in the future
                        long nextPresentationTicks; // Absolute time in ticks at which we expect to present


                        // Future: eventually we should actually keep track of how long it takes
                        // the UCE to present.  For now we assume it's one vsync
                        _averagePresentationInterval = RefreshPeriod;


                        //
                        // Compute how many frames in the future we expect to present to.
                        //

                        //
                        // We have to be very careful about the computation when we're on a frame boundary.
                        //
                        // If the time since last vsync is very small it means that we, by computation, think a vsync
                        // just happened.  In reality, it may have just happened, is happening, or will happen very soon
                        // Since the UCE is on a different thread, it's possible in all three cases that the UCE is about
                        // to notify us of the vsync.
                        //
                        // This can get us into a situation where, though the computed time since last vsync is small,
                        // the timeSinceLastPresent is one frame ago. Either the UCE is currently presenting and
                        // timeSinceLastPresent is stale (i.e. we just haven't been notified yet), or the UCE will be
                        // missing this particular vsync and will return from present on a subsequent vsync.
                        //
                        // This is a problem when we're in a wait state (we've previously committed a frame and are
                        // waiting for the UCE to return from presenting it). If the UCE is about to return, we
                        // can tick the current frame to the next vsync.  If the UCE is not about to return, we
                        // must tick the current frame in the future.
                        //
                        // The best way to disambiguate this is to look up the last time a frame was committed.
                        // This is how long the UCE has been working on it.  By comparing that with how long the UCE
                        // takes on average to present a frame we'll be able to determine if it is finishing it now.

                        vsyncAdvance = 0;

                        if (InterlockIsWaiting)
                        {
                            // If we're waiting for the UCE to finish presenting a frame then
                            // in most cases it'll come back at the next vsync and we'll set our
                            // render time to the one after.
                            // We once (v1 shipping code) attempted to limit vsyncAdvance to 0
                            // based on heuristics involving where in the frame we are.  This was
                            // removed after it was discovered that the logic was flawed and it made
                            // analyzing timing graphs more difficult.
                            vsyncAdvance = 1;
                        }

                        nextVsyncTicks = countsTicks + TicksUntilNextVsync(countsTicks);
                        nextPresentationTicks = (nextVsyncTicks + (vsyncAdvance * RefreshPeriod));

                        //
                        // If we had previously estimated the next presentation time
                        // and that estimate still seems reasonable then use the
                        // previous estimate rather than the newly computed value.
                        // This is a good performance win because it means we will
                        // tick animations and thus run layout to the same value as
                        // last time, which saves a lot of computation. For this
                        // purpose, we will consider the previous estimate "reasonable"
                        // if it falls within 1/2 frame of the new value.
                        //

                        if ((nextPresentationTicks - _estimatedNextPresentationTime.Ticks) * _animationRenderRate > TimeSpan.FromMilliseconds(500).Ticks)
                        {
                            // Establish a new estimate
                            _estimatedNextPresentationTime = TimeSpan.FromTicks(nextPresentationTicks);
                        }
                    }
                    else
                    {
                        _estimatedNextPresentationTime = TimeSpan.FromTicks(countsTicks);
                    }
                }
                else
                {
                    _estimatedNextPresentationTime = TimeSpan.FromTicks(countsTicks);
                }

                return _estimatedNextPresentationTime;
            }
        }

        /// <summary>
        /// Starts up the media system and creates needed channels used by the media context
        /// </summary>
        internal void CreateChannels()
        {
            _channelManager.CreateChannels();

            // Notify renderer how it should behave when no valid displays are available, 
            // or when this process is running in a non-interactive Window Station, or when 
            // an application has opted into behavior that requests WPF to continue rendering
            // even when no valid displays are detected.
            // 
            // Do this immediately after creating channels. 
            DUCE.NotifyPolicyChangeForNonInteractiveMode(
                    ShouldRenderEvenWhenNoDisplayDevicesAreAvailable,
                    Channel);

            HookNotifications();

            // Create an ETW Event Resource for performance tracing
            // It might be good enough to put this in the current batch without
            // submitting it.
            _uceEtwEvent.CreateOrAddRefOnChannel(this, Channel, DUCE.ResourceType.TYPE_ETWEVENTRESOURCE);

            // Send a request for an updated render tier value
            RequestTier(Channel);

            Channel.CloseBatch();
            Channel.Commit();

            // We now call CompleteRender, which calls SyncFlush, to ensure that all commands have
            // been processed, including the tier request.
            CompleteRender();

            // Since all of the commands have now been processed, we can go ahead and manually call
            // NotifyChannelMessage to pick up any back channel messages which have been sent.
            NotifyChannelMessage();
        }

        /// <summary>
        /// Starts releases channels and shuts down the media system. When the last visual manager has
        /// disconnected the transport will shut down.
        /// </summary>
        private void RemoveChannels()
        {
            // This test is needed because this method is called by Dispose which is called
            // on shutdown which can happen in a disconnected state. We can replace this
            // test with an assert by moving
            // the management of transport connectedness state to the media system.
            if (Channel != null)
            {
                _uceEtwEvent.ReleaseOnChannel(Channel);

                //
                // With no channels left open, we cannot be in an interlocked
                // presentation mode because we don't have a connection to the
                // composition engine.
                //

                LeaveInterlockedPresentation();
            }

            _channelManager.RemoveChannels();
        }

        /// <summary>
        /// Start interlocked presentation mode and resquest
        /// </summary>
        private void EnterInterlockedPresentation()
        {
            if (!InterlockIsEnabled)
            {
                if (MediaSystem.AnimationSmoothing
                    && Channel.MarshalType == ChannelMarshalType.ChannelMarshalTypeCrossThread
                    && IsClockSupported)
                {
                    unsafe
                    {
                        //
                        // Ask the UCE to get into VSync mode
                        //

                        DUCE.MILCMD_PARTITION_SETVBLANKSYNCMODE data;
                        data.Type = MILCMD.MilCmdPartitionSetVBlankSyncMode;
                        data.Enable = 1; /* true */

                        Channel.SendCommand(
                            (byte*)&data,
                            sizeof(DUCE.MILCMD_PARTITION_SETVBLANKSYNCMODE),
                            true);

                        _interlockState = InterlockState.RequestedStart;
                    }
                }
            }
        }


        /// <summary>
        /// Leaves interlocked presentation mode and cleans up state so we can
        /// continue to present in non-interlocked mode.
        /// </summary>
        private void LeaveInterlockedPresentation()
        {
            bool interlockDisabled = (_interlockState == InterlockState.Disabled);

            if (_interlockState == InterlockState.WaitingForResponse)
            {
                // Process messages until we get a response for the outstanding frame.
                // This is necessary because we are unsure whether the UCE has already
                // posted a notification in our message queue
                CompleteRender();
            }

            // If we are waiting for the next frame stop the timer since we are
            // leaving interlocked presentation mode

            _estimatedNextVSyncTimer.Stop();

            //
            // If we are not disabled then request to stop the mode.
            // If we had already asked to start but haven't gotten the response
            // still request to stop
            //
            if (!interlockDisabled)
            {
                _interlockState = InterlockState.Disabled;

                unsafe
                {
                    //
                    // Tell the UCE that we are not in VSync mode anymore
                    //

                    DUCE.MILCMD_PARTITION_SETVBLANKSYNCMODE data;
                    data.Type = MILCMD.MilCmdPartitionSetVBlankSyncMode;
                    data.Enable = 0; /* false */

                    Channel.SendCommand(
                        (byte*)&data,
                        sizeof(DUCE.MILCMD_PARTITION_SETVBLANKSYNCMODE),
                        true);

                    // We need to send this notification now otherwise, we don't
                    // know when we'll send the next packet. This will clear the
                    // channel and render the last frame (if necessary).
                    _needToCommitChannel = true;
                    CommitChannel();
                }
}

            Debug.Assert(_interlockState == InterlockState.Disabled,
                "LeaveInterlockedPresentationMode should set the InterlockedState to Disabled");
        }

        /// <summary>
        /// Hooks the async channel so we get notifications.
        /// </summary>
        private void HookNotifications()
        {
            Debug.Assert(Channel != null);

            //
            // This associates this channel with the given notification
            // window so that we can receive a window message whenever
            // there is a new message posted.
            //
            _notificationWindow.SetAsChannelNotificationWindow();

            //
            // This actually populates the channel into the composition
            // engine so that it can receive notifications.
            //
            RegisterForNotifications(Channel);
        }

        /// <summary>
        /// Gets the MediaContext from the context passed in as argument.
        /// </summary>
        internal static MediaContext From(Dispatcher dispatcher)
        {
            Debug.Assert(dispatcher != null, "Dispatcher required");
            MediaContext cm = (MediaContext)dispatcher.Reserved0;
            if (cm == null)
            {
                cm = new MediaContext(dispatcher);
                Debug.Assert(dispatcher.Reserved0 == cm);
            }

            return cm;
        }

        /// <summary>
        /// Gets the MediaContext from the current UI context.
        /// </summary>
        internal static MediaContext CurrentMediaContext
        {
            get
            {
                return From(Dispatcher.CurrentDispatcher);
            }
        }


        /// <summary>
        /// Called by the Dispatcher to let us know that we are going away.
        /// </summary>
        void OnDestroyContext(object sender, EventArgs e)
        {
            Debug.Assert(CheckAccess());
            Dispose();
        }

        /// <summary>
        /// Disposes the MediaContext.
        /// </summary>
        public virtual void Dispose()
        {
            Debug.Assert(CheckAccess());

            if (!_isDisposed)
            {
                // If we crash in this destructor because of an OOM exception we
                // are not cleaning up everything correctly. Moving forward we need to see if we can handle this
                // in a better way.

                // Dispose all still registered ICompositionTargets ----------------
                // Note that disposing the CompositionTargets should be the first thing we do here.

                // First make a copy of the dictionarys contents, because ICompositionTarget.Dispose modifies this collection.
                ICompositionTarget[] registeredVTs = new ICompositionTarget[_registeredICompositionTargets.Count];
                _registeredICompositionTargets.CopyTo(registeredVTs, 0);

                // Iterate through the ICompositionTargets and dispose them. Be careful, ICompositionTarget.Dispose
                // removes the ICompositionTargets from the Dictionary. This is why we don't iterate the Dictionary directly.
                foreach (ICompositionTarget iv in registeredVTs)
                {
                    iv.Dispose();
                }
                _registeredICompositionTargets = null;

                // Dispose the notification window
                _notificationWindow.Dispose();

                // Unhook the context destroy event handler -------------------
                Dispatcher.ShutdownFinished -= _destroyHandler;
                _destroyHandler = null;

                // Dispose the time manager ----------------------------------
                Debug.Assert(_timeManager != null);
                _timeManager.NeedTickSooner -= new EventHandler(OnNeedTickSooner);
                _timeManager.Stop();

                // From now on we are disposed -------------------------------
                _isDisposed = true;

                RemoveChannels();

                // if we set the Dispatcher.Reserved0 field to null, we end
                // creating another media context on the shutdown pass when the
                // HwndSrc class sets its visual root to null. In a disconnected
                // state this attempts to re open the transport.

                // Disconnect from MediaSystem -------------------------------
                MediaSystem.Shutdown(this);
                _timeManager = null;

                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Registers a new ICompositionTarget with the MediaSystem.
        /// </summary>
        /// <param name="dispatcher">Dispatcher with which the ICompositionTarget should be registered.</param>
        /// <param name="iv">The ICompositionTarget to register with the MediaSystem.</param>
        internal static void RegisterICompositionTarget(Dispatcher dispatcher, ICompositionTarget iv)
        {
            Debug.Assert(dispatcher != null);
            Debug.Assert(iv != null);

            MediaContext current = From(dispatcher);
            current.RegisterICompositionTargetInternal(iv);
        }

        /// <summary>
        /// Registers the ICompositionTarget from this MediaContext.
        /// </summary>
        /// <param name="iv"></param>
        private void RegisterICompositionTargetInternal(ICompositionTarget iv)
        {
            Debug.Assert(!_isDisposed);
            Debug.Assert(iv != null);

            // If channel is not available, we are in a disconnected state.
            // When connect handler is invoked for this media context, all
            // registered targets will be visited and AddRefChannel will be
            // called for them, so here we just skip the operation.
            if (Channel != null)
            {
                // if _currentRenderingChannel is nonempty, we're registering this ICompositionTarget
                // from within a render walk and it is thus a visualbrush, we need to add it to the
                // channel which we are currently rendering. If _currentRenderingChannel
                // is null, we just get the target channels for this ICompositionTarget and add
                // there.
                DUCE.ChannelSet channelSet = (_currentRenderingChannel == null) ? GetChannels() : _currentRenderingChannel.Value;
                iv.AddRefOnChannel(channelSet.Channel, channelSet.OutOfBandChannel);
            }

            _registeredICompositionTargets.Add(iv);
        }

        /// <summary>
        /// Unregisters the ICompositionTarget from the Dispatcher.
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <param name="iv"></param>
        internal static void UnregisterICompositionTarget(Dispatcher dispatcher, ICompositionTarget iv)
        {
            Debug.Assert(dispatcher != null);
            Debug.Assert(iv != null);

            MediaContext.From(dispatcher).UnregisterICompositionTargetInternal(iv);
        }

        /// <summary>
        /// Removes the ICompositionTarget from this MediaContext.
        /// </summary>
        /// <param name="iv">ICompositionTarget to unregister.</param>
        private void UnregisterICompositionTargetInternal(ICompositionTarget iv)
        {
            Debug.Assert(iv != null);

            // this test is needed because we always unregister the target when the ReleaseUCEResources
            // is called on the target and Dispose is called from both the media context and the
            // hwnd source, so when shutting down in a disconnected state we end up calling here
            // after a Dispose.
            if (_isDisposed)
            {
                return;
            }

            // If channel is not available, we are in a disconnected state, which means
            // that all resources have been released and we can just skip the operation.
            if (Channel != null)
            {
                // if _currentRenderingChannel is nonempty, we're unregistering this ICompositionTarget
                // from within a render walk and it is thus a visualbrush, we need to remove it from the
                // channel which we are currently rendering. If _currentRenderingChannel
                // is null, we just get the target channels for this ICompositionTarget and release
                // there.
                DUCE.ChannelSet channelSet = (_currentRenderingChannel == null) ? GetChannels() : _currentRenderingChannel.Value;
                iv.ReleaseOnChannel(channelSet.Channel, channelSet.OutOfBandChannel);
            }

            _registeredICompositionTargets.Remove(iv);
        }

        private class InvokeOnRenderCallback
        {
            private DispatcherOperationCallback _callback;
            private object _arg;

            public InvokeOnRenderCallback(
                DispatcherOperationCallback callback,
                object arg)
            {
                _callback = callback;
                _arg = arg;
            }

            public void DoWork()
            {
                _callback(_arg);
            }
        }

        internal void BeginInvokeOnRender(
            DispatcherOperationCallback callback,
            object arg)
        {
            Debug.Assert(callback != null);

            // While technically it could be OK for the arg to be null, for now
            // I know that arg represents the this reference for the layout
            // process and should never be null.
            Debug.Assert(arg != null);

            if (_invokeOnRenderCallbacks == null)
            {
                _invokeOnRenderCallbacks = new FrugalObjectList<InvokeOnRenderCallback>();
            }

            _invokeOnRenderCallbacks.Add(new InvokeOnRenderCallback(callback, arg));

            if (!_isRendering)
            {
                PostRender();
            }
        }

        /// <summary>
        /// Add a pending loaded or unloaded callback
        /// </summary>
        [FriendAccessAllowed] // Built into Core, also used by Framework.
        internal LoadedOrUnloadedOperation AddLoadedOrUnloadedCallback(
            DispatcherOperationCallback callback,
            DependencyObject target)
        {
            LoadedOrUnloadedOperation op = new LoadedOrUnloadedOperation(callback, target);

            if (_loadedOrUnloadedPendingOperations == null)
            {
                _loadedOrUnloadedPendingOperations = new FrugalObjectList<LoadedOrUnloadedOperation>(1);
            }

            _loadedOrUnloadedPendingOperations.Add(op);

            return op;
        }

        /// <summary>
        /// Remove a pending loaded or unloaded callback
        /// </summary>
        [FriendAccessAllowed] // Built into Core, also used by Framework.
        internal void RemoveLoadedOrUnloadedCallback(LoadedOrUnloadedOperation op)
        {
            Debug.Assert(op != null);

            // cancel the operation - this prevents it from running even if it has
            // already been copied into the local array in FireLoadedPendingCallbacks
            op.Cancel();

            if (_loadedOrUnloadedPendingOperations != null)
            {
                for (int i=0; i<_loadedOrUnloadedPendingOperations.Count; i++)
                {
                    LoadedOrUnloadedOperation operation = _loadedOrUnloadedPendingOperations[i];
                    if (operation == op)
                    {
                        _loadedOrUnloadedPendingOperations.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// If there is already a render operation in the Dispatcher queue, this
        /// method will bump it up to render priority.  If not, it will add a
        /// render operation at render priority.
        /// </summary>
        /// <remarks>
        /// This method should only be called when a render is necessary "right
        /// now."  Events such as a change to the visual tree would result in
        /// this method being called.
        /// </remarks>
        internal void PostRender()
        {
            // this is now needed because we no longer set Dispatcher.Reserved0 to null
            // in the Dispose method. See comment in the Dispose method.
            if (_isDisposed)
            {
                return;
            }
            Debug.Assert(CheckAccess());


            if (!_isRendering)
            {
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WClientPostRender);

                if (_currentRenderOp != null)
                {
                    // If we already have a render operation in the queue, we should
                    // change its priority to render priority so it happens sooner.
                    _currentRenderOp.Priority = DispatcherPriority.Render;
                }
                else
                {
                    // If we don't have a render operation in the queue, add one at
                    // render priority.
                    _currentRenderOp = Dispatcher.BeginInvoke(DispatcherPriority.Render, _renderMessage, null);
                }

                // We don't need to keep our promotion timers around.
                _promoteRenderOpToInput.Stop();
                _promoteRenderOpToRender.Stop();
            }
        }

        /// <summary>
        /// This method is invoked from the HwndTarget when the window is resize.
        /// It will cancel pending render queue items and then run the dispatch for the
        /// render queue item by hand.
        /// </summary>
        internal void Resize(ICompositionTarget resizedCompositionTarget)
        {
            // Cancel pending render queue items so that we don't dispatch them later
            // causing a double render during Resize. (Note that RenderMessage will schedule a
            // new RenderQueueItem).
            if (_currentRenderOp != null)
            {
                _currentRenderOp.Abort();
                _currentRenderOp = null;
            }

            // We don't need to keep our promotion timers around.
            _promoteRenderOpToInput.Stop();
            _promoteRenderOpToRender.Stop();

            // Now render manually directly from the resize handler.
            // Alternatively we could pump the message queue here with a filter that only allows
            // RenderQueueItems to get dispatched.
            RenderMessageHandler(resizedCompositionTarget);
        }

        /// <summary>
        /// This is the standard RenderMessageHandler callback, posted via PostRender()
        /// and Resize().  This wraps RenderMessageHandlerCore and emits an ETW events
        /// to trace its execution.
        /// </summary>
        private object RenderMessageHandler(
              object resizedCompositionTarget /* can be null if we are not resizing*/
            )
        {
            if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info))
            {
                EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientRenderHandlerBegin, EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, PerfService.GetPerfElementID(this));
            }

            RenderMessageHandlerCore(resizedCompositionTarget);

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WClientRenderHandlerEnd);

            return null;
        }


        /// <summary>
        /// This is the RenderMessageHandler callback posted by RenderMessageHandlerCore
        /// when animations are active
        /// This wraps RenderMessageHandlerCore and emits an ETW event to signify that
        /// the Render Message being handled is processing an animation.
        /// </summary>

        private object AnimatedRenderMessageHandler(
            object resizedCompositionTarget /* can be null if we are not resizing*/
            )
        {
            if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info))
            {
                EventTrace.EventProvider.TraceEvent(EventTrace.Event.WClientAnimRenderHandlerBegin, EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info, PerfService.GetPerfElementID(this));
            }

            RenderMessageHandlerCore(resizedCompositionTarget);

            EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WClientAnimRenderHandlerEnd);

            return null;
}

        /// <summary>
        /// This handles the _inputMarkerOp message.  We're using
        /// _inputMarkerOp to determine if input priority dispatcher ops
        /// have been processes.
        /// </summary>
        private object InputMarkerMessageHandler(object arg)
        {
            //set the marker to null so we know that input priority has been processed
            _inputMarkerOp = null;
            return null;
        }

        //static int queueItemID;

        /// <summary>
        /// The ol' RenderQueueItem.
        /// </summary>
        private void RenderMessageHandlerCore(
            object resizedCompositionTarget /* can be null if we are not resizing*/
            )
        {
            // if the media system is disconnected bail.
            if (Channel == null)
            {
                return;
            }

            Debug.Assert(CheckAccess());
            Debug.Assert(
                (resizedCompositionTarget == null) ||
                (resizedCompositionTarget is ICompositionTarget));

            _isRendering = true;

            // We don't need our promotion timers anymore.
            _promoteRenderOpToInput.Stop();
            _promoteRenderOpToRender.Stop();

            bool gotException = true;

            try
            {
                int tickLoopCount = 0;


                do
                {
                    tickLoopCount++;
                    if (tickLoopCount > 153)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.MediaContext_InfiniteTickLoop));
                    }

                    _timeManager.Tick();

                    // Although the timing tree is now clean, during layout
                    // more animations may be added, in which case we must tick
                    // again to prevent "first frame" problems. If we do tick
                    // again we want to do so at the same time as before, until
                    // we are done. To that end, lock the tick time to the
                    // first tick. Note that Lock/Unlock aren't counted, so we
                    // can call Lock inside the loop and still safely call
                    // Unlock just once after we are done.
                    _timeManager.LockTickTime();

                    // call all render callbacks
                    FireInvokeOnRenderCallbacks();

                    // signal that the frame has been updated and we are ready to render.
                    // only fire on the first iteration
                    if (Rendering != null && tickLoopCount==1)
                    {
                        // The RenderingEventArgs class stores the next estimated presentation time.
                        // Since the TimeManager has just ticked, LastTickTime is exactly this time.
                        // (TimeManager gets its tick time from MediaContext's IClock implementation).
                        // In the case where we can't query QPC or aren't doing interlocked presents,
                        // this will be equal to the current time, which is a good enough approximation.
                        Rendering(this.Dispatcher, new RenderingEventArgs(_timeManager.LastTickTime));

                        // call all render callbacks again in case the Rendering event affects layout
                        // this will enable layout effecting changes to get triggered this frame
                        FireInvokeOnRenderCallbacks();
                    }
                }
                while (_timeManager.IsDirty);

                _timeManager.UnlockTickTime();

                // Invalidate the input devices on the InputManager
                InputManager.UnsecureCurrent.InvalidateInputDevices();

                //
                // before we call Render we want to save the in Interlock state so we know
                // if we need to schedule another render or not.  This is because we want to render
                // while the UCE is working on rendering the previous frame. To do this we would like to get into
                // a NotifyPresent, CommitChannel, Render pattern.  If the interlock state is not
                // "Waiting" at this point, then we must be in a Render, CommitChannel pattern
                // and we will need to do something in order to get us back in parallel operation
                //

                bool interlockWasNotWaiting = !InterlockIsWaiting;

                //
                // This is the big Render!
                //
                // We've now updated timing and layout and the updated scene will be sent
                // to the UCE.
                //

                Render((ICompositionTarget)resizedCompositionTarget);

                //
                // We've processed the currentRenderOp so clear it
                //

                if (_currentRenderOp != null)
                {
                    _currentRenderOp.Abort();
                    _currentRenderOp = null;
                }

                if (!InterlockIsEnabled)
                {
                    //
                    // Schedule our next rendering operation. We want to introduce
                    // a minimum delay, so that we don't overrun the composition
                    // thread
                    //

                    ScheduleNextRenderOp(_timeDelay);
                }
                else if (interlockWasNotWaiting)
                {
                    //
                    // schedule another render because we were in the Render, CommitChannel
                    // pattern, and this will get us back in the CommitChannel, Render pattern so
                    // we will have the channel full when notification comes back from the UCE.
                    //

                    ScheduleNextRenderOp(TimeSpan.Zero);
                }

                gotException = false;
            }
            finally
            {
                // Reset current operation so it can be re-queued by layout
                // This is needed when exception happens in the midst of layout/TemplateExpansion
                // and it unwinds from the stack. If we don't clean this field here, the subsequent
                // PostRender won't queue new render operation and the window gets stuck. 
                if (gotException
                    && _currentRenderOp != null)
                {
                    _currentRenderOp.Abort();
                    _currentRenderOp = null;
                }

                _isRendering = false;
            }
        }

        private int InvokeOnRenderCallbacksCount
        {
            get
            {
                return _invokeOnRenderCallbacks != null ? _invokeOnRenderCallbacks.Count : 0;
            }
        }


        /// <summary>
        /// Calls all _invokeOnRenderCallbacks until no more are added
        /// </summary>
        private void FireInvokeOnRenderCallbacks()
        {
            int callbackLoopCount = 0;
            int count = InvokeOnRenderCallbacksCount;

            // This outer loop is to re-run layout in case the app causes a layout to get enqueued in response
            // to a Loaded event. In this case we would like to re-run layout before we allow render.
            do
            {
                while (count > 0)
                {
                    callbackLoopCount++;
                    if (callbackLoopCount > 153)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.MediaContext_InfiniteLayoutLoop));
                    }

                    FrugalObjectList<InvokeOnRenderCallback> callbacks = _invokeOnRenderCallbacks;
                    _invokeOnRenderCallbacks = null;

                    for (int i = 0; i < count; i++)
                    {
                        callbacks[i].DoWork();
                    }

                    count = InvokeOnRenderCallbacksCount;
                }

                // Fire all the pending Loaded events before Render happens
                // but after the layout storm has subsided
                FireLoadedPendingCallbacks();

                count = InvokeOnRenderCallbacksCount;
            }
            while (count > 0);
        }

        /// <summary>
        /// Fire all the pending Loaded callbacks before Render happens
        /// </summary>
        private void FireLoadedPendingCallbacks()
        {
            // Fire all the pending Loaded events before Render happens but after layout
            if (_loadedOrUnloadedPendingOperations != null)
            {
                var count = _loadedOrUnloadedPendingOperations.Count;
                if (count == 0)
                {
                    return;
                }

                // Create a copy of the _loadedOrUnloadedPendingOperations
                var copyOfPendingCallbacks = _loadedOrUnloadedPendingOperations;

                // Clear up the _loadedOrUnloadedPendingOperations in case the broadcast of Loaded causes
                // more of the pending operations to get posted.
                _loadedOrUnloadedPendingOperations = null;

                // Iterate and fire all the pending loaded operations
                for (int i=0; i<count; i++)
                {
                    copyOfPendingCallbacks[i].DoWork();
                }
            }
        }

        /// <summary>
        /// Render all registered ICompositionTargets.
        /// </summary>
        /// <remarks>
        /// * We have to render all visual targets on the same context at once. The reason for this is that
        ///   we batch per Dispatcher (we use the context to get to the batch all over the place).
        /// * On a WM_SIZE we also need to render because USER32 is sitting in a tight loop sending us messages
        ///   continously. Hence we need to render all visual trees attached to visual targets otherwise we
        ///   would submit a batch that has only part of the changes for some visual trees. This would cause
        ///   structural tearing.
        /// </remarks>
        private void Render(ICompositionTarget resizedCompositionTarget)
        {
            // resizedCompositionTarget is the HwndTarget that is currently being resized.

            //
            // Disable reentrancy during the Render pass.  This is because much work is done
            // during Render and we cannot survive reentrancy in these code paths.
            // Disabling processing will prevent the lock() statement from pumping messages,
            // so we don�t run the risk of having to process an unrelated message in the middle
            // of this code. Message pumping will resume sometime after we return.
            //
            // Note: The possible downside of DisableProcessing is
            //      1) Cross-Apartment COM calls may deadlock.
            //      2) We restrict what people can do in callbacks ie, they can�t display a message box.
            //
            using (Dispatcher.DisableProcessing())
            {
                Debug.Assert(CheckAccess());

                Debug.Assert(!_isDisposed);
                Debug.Assert(_registeredICompositionTargets != null);

                // ETW event tracing
                bool etwTracingEnabled = false;
                uint renderID = (uint)Interlocked.Increment(ref _contextRenderID);
                if (EventTrace.IsEnabled(EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info))
                {
                    etwTracingEnabled = true;

                    DUCE.ETWEvent.RaiseEvent(
                        _uceEtwEvent.Handle,
                        renderID,
                        Channel);

                    EventTrace.EventProvider.TraceEvent(
                        EventTrace.Event.WClientMediaRenderBegin,
                        EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info,
                        renderID,
                        TicksToCounts(_estimatedNextPresentationTime.Ticks)
                        );
                }

                // ----------------------------------------------------------------
                // 1) Render each registered ICompositionTarget to finish up the batch.

                foreach (ICompositionTarget registeredTarget in _registeredICompositionTargets)
                {
                    DUCE.ChannelSet channelSet;
                    channelSet.Channel = _channelManager.Channel;
                    channelSet.OutOfBandChannel = _channelManager.OutOfBandChannel;
                    _currentRenderingChannel = channelSet;
                    registeredTarget.Render((registeredTarget == resizedCompositionTarget), channelSet.Channel);
                    _currentRenderingChannel = null;
                }


                // ----------------------------------------------------------------
                // 2) Update any resources that need to be updated for this render.

                RaiseResourcesUpdated();


                //
                // 3) Commit the channel.
                //
                // if we are not already waiting for a present then commit the
                // channel at this time. If we are waiting for a present then we
                // will wait until we have presented before committing this channel
                //

                if (Channel != null)
                {
                    Channel.CloseBatch();
                }

                _needToCommitChannel = true;
                _commitPendingAfterRender = true;
                if (!InterlockIsWaiting)
                {
                    //if we've already commit during this vblank interval, dont do it again
                    // because it will cause the DWM to stall
                    if (HasCommittedThisVBlankInterval)
                    {
                        CommitChannelAfterNextVSync();
                    }
                    else
                    {
                        CommitChannel();
                    }
                }

                // ----------------------------------------------------------------
                // 4) Raise RenderComplete event.

                if (etwTracingEnabled)
                {
                    EventTrace.EventProvider.TraceEvent(
                        EventTrace.Event.WClientMediaRenderEnd,
                        EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Level.Info);


                    // trace the UI Response event
                    EventTrace.EventProvider.TraceEvent(
                        EventTrace.Event.WClientUIResponse,
                        EventTrace.Keyword.KeywordGraphics, EventTrace.Level.Info,
                        GetHashCode(),
                        renderID);
                }
            }
        }


        /// <summary>
        /// Commit the current channel to the composition thread.
        /// </summary>
        /// <remarks>
        /// This allows us to separate updating the visual tree from sending
        /// data to the UCE. When in InterlockedPresentation mode, we'll always
        /// have layout properly updated but we'll only commit 1 render per
        /// frame to the composition.
        /// </remarks>
        private void CommitChannel()
        {
            // if we get render messages posted while we are disconnected we don't have a channel.
            if (Channel != null)
            {
                Debug.Assert(_needToCommitChannel, "CommitChannel called with nothing on the channel");

                if (InterlockIsEnabled)
                {
                    Debug.Assert(!InterlockIsWaiting,
                        "We can't be committing the channel while waiting for a notification");

                    long currentTicks = CurrentTicks;
                    long presentationTime = _estimatedNextPresentationTime.Ticks;

                    //
                    // it is possible that presentationTime is in the past, if we request this time
                    // we will get an immediate NotifyPresent instead of getting one after the next
                    // vblank.  To prevent this we ensure the presentaitonTime is no earlier than the
                    // next VBlank time.
                    //
                    if (_animationRenderRate > 0)
                    {
                        long nextVBlank = currentTicks + TicksUntilNextVsync(currentTicks);
                        if (nextVBlank > presentationTime)
                        {
                            presentationTime = nextVBlank;
                        }
                    }

                    RequestPresentedNotification(Channel, TicksToCounts(presentationTime));

                    //
                    // If we are in interlocked presentation mode then we enter
                    // a wait state once we commit the channel.
                    //
                    _interlockState = InterlockState.WaitingForResponse;
                    _lastCommitTime = currentTicks;
                }

                if (CommittingBatch != null)
                {
                    CommittingBatch(Channel, new EventArgs());
                }

                Channel.Commit();

                if (_commitPendingAfterRender)
                {
                    //
                    // Raise Render Complete event since a Render happened and
                    // the commit for that render happened.
                    //
                    if (_renderCompleteHandlers != null)
                    {
                        _renderCompleteHandlers(this, null);
                    }

                    _commitPendingAfterRender = false;
                }

                //
                // The channel has just been commited. There's nothing more in it.
                //

                // The payload data for this event is the render ID of the frame we're committing.
                EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordGraphics | EventTrace.Keyword.KeywordPerf, EventTrace.Event.WClientUICommitChannel, _contextRenderID);
}

            _needToCommitChannel = false;
        }

        /// <summary>
        /// Asks the composition engine to notify us once the frame we are
        /// submitted has been presented to the screen.
        /// </summary>
        private void RequestPresentedNotification(DUCE.Channel channel, long estimatedFrameTime)
        {
            Debug.Assert(InterlockIsEnabled,
                "Cannot request presentation notification unless interlock mode is enabled");

            unsafe
            {
                DUCE.MILCMD_PARTITION_NOTIFYPRESENT data;
                data.Type = MILCMD.MilCmdPartitionNotifyPresent;
                data.FrameTime = (ulong) estimatedFrameTime;

                channel.SendCommand(
                    (byte*)&data,
                    sizeof(DUCE.MILCMD_PARTITION_NOTIFYPRESENT),
                    true);
            }
        }

        /// <summary>
        /// CompleteRender
        /// </summary>
        /// <remarks>
        /// Wait for the rendering loop to finish any pending instructions.
        /// </remarks>
        internal void CompleteRender()
        {
            // for now just bail if we are not connected.
            if (Channel != null)
            {
                //
                // In intelocked mode in order to make sure that frames are
                // fully updated to the screen we need to do the folloing:
                // 1. If we are waiting for a response from the UCE, then wait
                //    until we get that response.
                // 2. If we had pending operations then flush the channel and
                //    wait for the notification that this new frame has been
                //    processed by the composition.
                //
                if (InterlockIsEnabled)
                {
                    if (_interlockState == InterlockState.WaitingForResponse)
                    {
                        do
                        {
                            // WaitForNextMessage will Commit()
                            if (CommittingBatch != null)
                            {
                                CommittingBatch(Channel, new EventArgs());
                            }
                            
                            Channel.WaitForNextMessage();
                            NotifyChannelMessage();
                        } while (_interlockState == InterlockState.WaitingForResponse);
                    }

                    //
                    // We might have started a timer to wait for the next frame.
                    // stop it now and go back to idle state
                    //
                    _estimatedNextVSyncTimer.Stop();
                    _interlockState = InterlockState.Idle;

                    if (_needToCommitChannel)
                    {
                        CommitChannel();

                        if (_interlockState == InterlockState.WaitingForResponse)
                        {
                            do
                            {
                                // WaitForNextMessage will Commit(), but CommitChannel()
                                // was just called already and it handles CommittingBatch
                                Channel.WaitForNextMessage();
                                NotifyChannelMessage();
                            } while (_interlockState == InterlockState.WaitingForResponse);

                            //
                            // We might have started a timer to wait for the next frame.
                            // stop it now and go back to idle state
                            //
                            _estimatedNextVSyncTimer.Stop();
                           _interlockState = InterlockState.Idle;
                        }
                    }
                }
                else
                {
                    // SyncFlush() will Commit()
                    if (CommittingBatch != null)
                    {
                        CommittingBatch(Channel, new EventArgs());
                    }
                    
                    //
                    // Issue a sync flush, which will only return after
                    // the last frame is presented
                    //

                    Channel.SyncFlush();
                }
            }
        }

        /// <summary>
        /// This function is registered with the MediaContext's TimeManager. It is called whenever
        /// a clock managed by the TimeManager goes active, but only if there hasn't been already an
        /// active clock. For now we start the animation thread in there.
        /// </summary>
        private void OnNeedTickSooner(object sender, EventArgs e)
        {
            PostRender();
        }

        /// <summary>
        /// Checks if the current context can request the specified permissions.
        /// </summary>
        internal void VerifyWriteAccess()
        {
            if (!WriteAccessEnabled)
            {
                throw new InvalidOperationException(SR.Get(SRID.MediaContext_APINotAllowed));
            }
        }

        /// <summary>
        /// Returns false if the MediaContext is currently read-only
        /// </summary>
        internal bool WriteAccessEnabled
        {
            get { return _readOnlyAccessCounter <= 0; }
        }

        /// <summary>
        /// Methods to lock down the Visual tree for write access.
        /// </summary>
        internal void PushReadOnlyAccess()
        {
            _readOnlyAccessCounter++;
        }

        internal void PopReadOnlyAccess()
        {
            _readOnlyAccessCounter--;
        }

        /// <summary>
        /// Each MediaContext is associated with a TimeManager. The TimeManager is shared by all ICompositionTargets.
        /// </summary>
        private TimeManager _timeManager;

        internal TimeManager TimeManager
        {
            get
            {
                return _timeManager;
            }
        }


        /// <summary>
        /// RenderComplete event is fired when Render method commits the channel.
        /// This is used for ink transition. Currently this event is internal and will
        /// be accessed using reflection until proper object model is defined.
        /// </summary>
        internal event EventHandler RenderComplete
        {
            add
            {
                //
                // If the Render happened (i.e. flag is true) and corresponding Commit
                // has not happened, then set the flag to false, since the next
                // Commit will not have the changes after Render and before +RenderComplete.
                // In other words, consider this event sequence:-
                //  1. Render
                //  2. Some resource property changes (eg. visual.Clip = null)
                //  3. +RenderComplete
                //  4. Commit
                // Then the 4th Commit will not have 2's changes(as it requires a
                // new Render pass) and so raising the event is wrong.
                //
                // Note: If the event is added every time in the middle of Render
                // and Commit, then RenderComplete will starve.
                //
                if (_commitPendingAfterRender)
                {
                    _commitPendingAfterRender = false;
                }
                _renderCompleteHandlers += value;
            }
            remove
            {
                _renderCompleteHandlers -= value;
            }
        }

        /// <summary>
        /// ResourcesUpdatedHandler - This event handler prototype defines the callback
        /// for our async update callback in the ResourcesUpdated Event.
        /// The method which implements this prototype is also often called in situations where
        /// the resource is known to be "on channel" - in those cases, "true" is passed for the second
        /// parameter (allowing the implementation to skip the check).
        /// </summary>
        internal delegate void ResourcesUpdatedHandler(DUCE.Channel channel, bool skipOnChannelCheck);

        internal event ResourcesUpdatedHandler ResourcesUpdated
        {
            add
            {
                _resourcesUpdatedHandlers += value;
            }
            remove
            {
                _resourcesUpdatedHandlers -= value;
            }
        }

        private void RaiseResourcesUpdated()
        {
            if (_resourcesUpdatedHandlers != null)
            {
                DUCE.ChannelSet channelSet = GetChannels();
                _resourcesUpdatedHandlers(channelSet.Channel, false /* do not skip the "on channel" check */);
                _resourcesUpdatedHandlers = null;
            }
        }

        /// <summary>
        /// Create a fresh or fetch one from the pool synchronous channel.
        /// </summary>
        internal DUCE.Channel AllocateSyncChannel()
        {
            return _channelManager.AllocateSyncChannel();
        }

        /// <summary>
        /// Returns a sync channel back to the pool.
        /// </summary>
        internal void ReleaseSyncChannel(DUCE.Channel channel)
        {
            _channelManager.ReleaseSyncChannel(channel);
        }


        /// <summary>
        /// Returns the asynchronous channel for this media context.
        /// </summary>
        /// <remarks>
        /// This property is deprecated and scheduled to be removed as per task #26681.
        /// Please do not create additional dependencies on it.
        /// </remarks>
        internal DUCE.Channel Channel
        {
            get
            {
                return _channelManager.Channel;
            }
        }

        /// <summary>
        /// Returns the asynchronous out-of-band channel for this media context.
        /// </summary>
        internal DUCE.Channel OutOfBandChannel
        {
            get
            {
                return _channelManager.OutOfBandChannel;
            }
        }


        internal bool IsConnected
        {
            get
            {
                return _isConnected;
            }
        }

        /// <summary>
        /// Returns the BoundsDrawingContextWalker for this media context.
        /// To handle reentrance we want to make sure that
        /// no one else on the same thread gets the same context.
        /// </summary>
        internal BoundsDrawingContextWalker AcquireBoundsDrawingContextWalker()
        {
            if (_cachedBoundsDrawingContextWalker == null)
            {
                return new BoundsDrawingContextWalker();
            }

            BoundsDrawingContextWalker ctx = _cachedBoundsDrawingContextWalker;
            _cachedBoundsDrawingContextWalker = null;
            ctx.ClearState();

            return ctx;
        }

        /// <summary>
        /// Set the BoundsDrawingContextWalker for next use
        /// To handle reentrance we want to make sure that
        /// no one else on the same thread gets the same context.
        /// </summary>
        internal void ReleaseBoundsDrawingContextWalker(BoundsDrawingContextWalker ctx)
        {
            _cachedBoundsDrawingContextWalker = ctx;
        }

        private void PromoteRenderOpToInput(object sender, EventArgs e)
        {
            if(_currentRenderOp != null)
            {
                _currentRenderOp.Priority = DispatcherPriority.Input;
            }

            ((DispatcherTimer)sender).Stop();
        }

        private void PromoteRenderOpToRender(object sender, EventArgs e)
        {
            if(_currentRenderOp != null)
            {
                _currentRenderOp.Priority = DispatcherPriority.Render;
            }

            ((DispatcherTimer)sender).Stop();
        }

        /// <summary>
        /// We setup a timer when the UCE doesn't present a frame because the
        /// scene hasn't changed. When this timer triggers, we have passed the
        /// estimated time at which the predicted VSync should have occured.
        /// We can now commit our accumulated changes to the channel. This
        /// timer should only be active if we got a NoPresent notification from
        /// the composition thread.
        /// </summary>

        private void EstimatedNextVSyncTimeExpired(object sender, EventArgs e)
        {
            Debug.Assert(_interlockState == InterlockState.WaitingForNextFrame
                         && _lastPresentationResults == MIL_PRESENTATION_RESULTS.MIL_PRESENTATION_NOPRESENT,
                "CommitRenderChannel timer should only be trigger while waiting for the frame to expire");

            //
            // if we wake up before our earliest wakup time, we run the risk of commiting twice in one
            // vblank interval.  This is mitigated by detecting the early wakup and creating a new timer
            // for the remaining time.  This could cause us to skip a vblank if our new timer wakes up too
            // late, but this is seens as a better alternative to commiting twice in one vblank interval.
            // A better solution would be to have some form of high resolution timer.
            //
            long currentTicks = CurrentTicks;
            DispatcherTimer timer = ((DispatcherTimer)sender);
            long earliestWakeupTicks = 0;
            if(timer.Tag != null)
                earliestWakeupTicks = (long)timer.Tag;
            if (earliestWakeupTicks > currentTicks)
            {
                timer.Stop();
                timer.Interval = TimeSpan.FromTicks(earliestWakeupTicks - currentTicks);
                timer.Start();
                return;
            }

            _interlockState = InterlockState.Idle;

            if (_needToCommitChannel)
            {
                CommitChannel();

                //schedule the next render so we're back on the np-commit-render pattern
                ScheduleNextRenderOp(TimeSpan.Zero);
            }

            timer.Stop();
        }

        //+---------------------------------------------------------------------
        //
        //  Private Methods
        //
        //----------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Tells the composition engine that we want to receive asynchronous
        /// notifications on this channel.
        /// </summary>
        private void RegisterForNotifications(DUCE.Channel channel)
        {
            DUCE.MILCMD_PARTITION_REGISTERFORNOTIFICATIONS registerCmd;

            registerCmd.Type = MILCMD.MilCmdPartitionRegisterForNotifications;
            registerCmd.Enable = 1;  // Enable notifications.

            unsafe
            {
                channel.SendCommand(
                    (byte*)&registerCmd,
                    sizeof(DUCE.MILCMD_PARTITION_REGISTERFORNOTIFICATIONS)
                    );
            }
        }

        #endregion Private Methods


        //+---------------------------------------------------------------------
        //
        //  Private Fields
        //
        //----------------------------------------------------------------------

        #region Private Fields
        /// <summary>
        /// Returns the current channel set for this MediaContext.
        /// </summary>
        internal DUCE.ChannelSet GetChannels()
        {
            DUCE.ChannelSet channelSet;
            channelSet.Channel = _channelManager.Channel;
            channelSet.OutOfBandChannel = _channelManager.OutOfBandChannel;
            return channelSet;
        }

        /// <summary>
        /// The disposed flag indicates if the object got disposed.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Event handler that is called when the context is destroyed.
        /// </summary>
        private EventHandler _destroyHandler;

        /// <summary>
        /// This event is raised when the MediaContext.Render has
        /// committed the channel.
        /// </summary>
        private event EventHandler _renderCompleteHandlers;

        /// <summary>
        /// This event is raised when the MediaContext is ready for all of the
        /// invalid resources to update their values.
        /// </summary>
        private event ResourcesUpdatedHandler _resourcesUpdatedHandlers;

        /// <summary>
        /// Use a guid to uniquely identify the current context.  Note that
        /// we can't use static data to generate a unique id since static
        /// data is not shared across app domains and this id must be
        /// truly unique.
        /// </summary>
        private Guid _contextGuid;

        /// <summary>
        /// Message delegate.
        /// </summary>
        private DispatcherOperation _currentRenderOp;
        private DispatcherOperation _inputMarkerOp;
        private DispatcherOperationCallback _renderMessage;
        private DispatcherOperationCallback _animRenderMessage;
        private DispatcherOperationCallback _inputMarkerMessage;
        private DispatcherOperationCallback _renderModeMessage;
        private DispatcherTimer _promoteRenderOpToInput;
        private DispatcherTimer _promoteRenderOpToRender;

        /// <summary>
        /// This timer is used to keep track of when we should commit our
        /// accumulated renders to the composition thread when the composition
        /// thread hasn't presented our frame. If we didn't wait for this time
        /// we might present a frame one vsync too early.
        /// </summary>
        private DispatcherTimer _estimatedNextVSyncTimer;

        /// <summary>
        /// The channel manager is a security wrapper for channel operations.
        /// </summary>
        private ChannelManager _channelManager;

        /// <summary>
        /// ETW Event Resource handle for performance tracing
        /// </summary>
        private DUCE.Resource _uceEtwEvent = new DUCE.Resource();

        /// <summary>
        /// Indicates that we are in the middle of processing a render message.
        /// </summary>
        private bool _isRendering;

        /// <summary>
        /// Indicates we are in the process of disconnecting.
        /// This flag is set so that we know not to schedule an animation
        /// render after the render pass we use to unmarshall resources.
        /// </summary>
        private bool _isDisconnecting = false;

        /// <summary>
        /// Indicates we are in a disconnected state. This flag is used by the
        /// composition targets to determine if they need to DeleteCobsInSubgraph
        /// (i.e. unmarshall the visual tree)
        /// </summary>
        private bool _isConnected = false;

        private FrugalObjectList<InvokeOnRenderCallback> _invokeOnRenderCallbacks;

        /// <summary>
        /// Set of ICompositionTargets that are currently registered with the MediaSystem;
        /// </summary>
        private HashSet<ICompositionTarget> _registeredICompositionTargets;

        /// <summary>
        /// This are the the permissions the Context has to access Visual APIs.
        /// </summary>
        private int _readOnlyAccessCounter;

        private BoundsDrawingContextWalker _cachedBoundsDrawingContextWalker = new BoundsDrawingContextWalker();

        //
        // The ID associated with a render dispatch.  This is used to track
        // renders, and is used by the realization cache to determine
        // uniqueness of realizations, and across the UI and UCE threads
        // in ETW trace events.
        //
        private static int _contextRenderID = 0;

        // The render tier associated with this MediaContext. This is updated
        // when channels are created.
        private int _tier;

        /// <summary>
        /// Rendering event.  Registers a delegate to be notified after animation and layout but before rendering
        /// Its EventArgs parameter can be cast to RenderingEventArgs to get the last presentation time.
        /// </summary>
        internal event EventHandler Rendering;

        /// <summary>
        /// CommittingBatch event.  Registers a delegate to be notified when a batch is
        /// about to be committed to MIL.
        /// </summary>
        internal event EventHandler CommittingBatch;

        // List of pending loaded event dispatcher operations
        private FrugalObjectList<LoadedOrUnloadedOperation> _loadedOrUnloadedPendingOperations;

        // Time to wait for unthrottled renders
        private TimeSpan _timeDelay = TimeSpan.FromMilliseconds(10);

        // A flag to determine if RenderComplete event is raised. We only
        // raise the event if Render + Commit happens.
        //
        // Note: If the event is added every time in the middle of Render
        // and Commit, then RenderComplete will starve.
        private bool _commitPendingAfterRender;

        // The top-level hidden notification window that is used to receive
        // and forward broadcast messages
        private MediaContextNotificationWindow _notificationWindow;

        private DUCE.ChannelSet? _currentRenderingChannel = null;

        #endregion Private Fields


        //+---------------------------------------------------------------------
        //
        //  Animation Smoothing
        //
        //----------------------------------------------------------------------

        #region Animation Smoothing

        /// <summary>
        /// This enum indicates that we are in an interlocked presentation mode.
        /// We render a frame. send it to be presented and the composition comes
        /// to tell us that the frame was presented. Otherwise we just keep giving
        /// frames to the composition thread and assume that they get presented
        /// as requested.
        /// </summary>

        private enum InterlockState
        {
            /// <summary>
            /// Interlock presentation mode is disabled. We send a frame to the
            /// composition thread and simply schedule our next one when we know
            /// that we have something to render
            /// </summary>
            Disabled             = 0,

            /// <summary>
            /// Interlock presentation mode has requested a roundtrip message to
            /// before enabling the mode. This state indicates that we've
            /// requested the UCE enter the interlocked presentation mode but we
            /// haven't received the response yet.
            /// </summary>
            RequestedStart,

            /// <summary>
            /// We are in interlocked presentation mode and are not waiting for
            /// anything. If we get a render request we will process it
            /// immediately
            /// </summary>
            Idle,

            /// <summary>
            /// We are in interlocked presentation mode and have sumitted a
            /// frame to be presented. We are waiting for the notification from
            /// the UCE thread.
            /// </summary>
            WaitingForResponse,

            /// <summary>
            /// We are in interlocked presentation mode but the last frame we
            /// submitted to be presented wasn't presented. We are waiting until
            /// the next VSync before submitting another frame for presentation.
            /// </summary>
            WaitingForNextFrame
        };

        /// <summary>
        /// The current state of the interlocked presentation mode
        /// </summary>
        private InterlockState _interlockState;

        /// <summary>
        /// This indicates that we are waiting for something (either the next
        /// frame time to occur or for a response from the UCE).
        /// </summary>
        private bool InterlockIsWaiting
        {
            get
            {
                return (_interlockState == InterlockState.WaitingForNextFrame ||
                        _interlockState == InterlockState.WaitingForResponse);
            }
        }

        /// <summary>
        /// We are currently in an interlocked presentation mode and the UCE
        /// has acknolwedged that it is also in that mode.
        /// </summary>
        private bool InterlockIsEnabled
        {
            get
            {
                return (   _interlockState != InterlockState.Disabled
                        && _interlockState != InterlockState.RequestedStart);
            }
        }

        /// <summary>
        /// This is used in interlocked presentation mode. This flag indicates
        /// that we've put something on the channel but that we haven't commited
        /// it yet. This occurs when we know that the composition thread is already
        /// processing a batch for a frame. We only want to give the UCE 1 frame
        /// per VSync so that we try to make it present the frame at the time
        /// that we have estimated. At this point we have rendered 1 frame in
        /// advance and will wait until we reach the next frame boundary before
        /// committing the channel to have the information sent to the
        /// composition thread on the right frame.
        /// </summary>
        private bool _needToCommitChannel;

        /// <summary>
        /// Last time the composition presented a frame
        /// Units: counts
        /// </summary>
        private long _lastPresentationTime;

        /// <summary>
        /// Last time the UI thread committed a frame
        /// Units: Ticks
        /// </summary>
        private long _lastCommitTime;

        /// <summary>
        /// Last time the the input marker was added to the queue
        /// Units: Ticks
        /// </summary>
        private long _lastInputMarkerTime;

        /// <summary>
        /// Average time it takes the UCE to present a frame
        /// Units: Ticks
        /// </summary>
        private long _averagePresentationInterval;

        /// <summary>
        /// Estimation of the next time we want a frame to appear on screen. We
        /// will set the TimeManager's time to this to have the animations look
        /// smooth
        /// </summary>
        private TimeSpan _estimatedNextPresentationTime;

        /// <summary>
        /// The refresh rate of the monitor that we are displaying to
        /// </summary>
        private int _displayRefreshRate;

        /// <summary>
        /// The rate at which we try to display content if no special throttling
        /// mechanism is used
        /// </summary>
        private int _adjustedRefreshRate;

        /// <summary>
        /// The rate at which we are rendering animations. This can be the
        /// refresh rate of the monitor that we are presenting on or can be
        /// overridden with a DesiredFrameRate on the Timeline. This is
        /// used to estimate the time of the next frame that we want to present.
        /// </summary>
        private int _animationRenderRate;

        /// <summary>
        /// The results of the last present call.
        /// </summary>
        private MIL_PRESENTATION_RESULTS _lastPresentationResults = MIL_PRESENTATION_RESULTS.MIL_PRESENTATION_VSYNC_UNSUPPORTED;

        static private long _perfCounterFreq;

        private const long MaxTicksWithoutInput = TimeSpan.TicksPerSecond / 2;

        #endregion Animation Smoothing
    }
 }
