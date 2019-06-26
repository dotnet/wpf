// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Win32.Pointer;
using MS.Internal;
using MS.Internal.Interop;
using MS.Internal.PresentationCore;                        // SecurityHelper
using MS.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input.StylusPointer
{
    /// <summary>
    /// Provides access and data from the Windows Interaction Context engine.
    /// 
    /// This gives WPF access to gestures and other features based off WM_POINTER data.
    /// </summary>
    internal class PointerInteractionEngine : IDisposable
    {
        #region Constants

        /// <summary>
        /// The delay before firing a hover in processor ticks.
        /// This threshold is taken from WISP activation delay code.
        /// </summary>
        private const int HoverActivationThresholdTicks = 275;

        /// <summary>
        /// The drag threshold in inches.
        /// This threshold is taken from WISP drag detection code.
        /// </summary>
        private const double DragThresholdInches = 0.106299;

        /// <summary>
        /// Configuration parameters for the interaction context.  We use it for tap, hold, right (secondary) tap,
        /// and drag detection via manipulation.  Manipulation can also feed flick detection if used.
        /// </summary>
        private static List<UnsafeNativeMethods.INTERACTION_CONTEXT_CONFIGURATION> DefaultConfiguration =
            new List<UnsafeNativeMethods.INTERACTION_CONTEXT_CONFIGURATION>()
            {
                new UnsafeNativeMethods.INTERACTION_CONTEXT_CONFIGURATION()
                {
                    enable = UnsafeNativeMethods.INTERACTION_CONFIGURATION_FLAGS.INTERACTION_CONFIGURATION_FLAG_TAP,
                    interactionId = UnsafeNativeMethods.INTERACTION_ID.INTERACTION_ID_TAP
                },
                new UnsafeNativeMethods.INTERACTION_CONTEXT_CONFIGURATION()
                {
                    enable = UnsafeNativeMethods.INTERACTION_CONFIGURATION_FLAGS.INTERACTION_CONFIGURATION_FLAG_HOLD,
                    interactionId = UnsafeNativeMethods.INTERACTION_ID.INTERACTION_ID_HOLD
                },
                new UnsafeNativeMethods.INTERACTION_CONTEXT_CONFIGURATION()
                {
                    enable = UnsafeNativeMethods.INTERACTION_CONFIGURATION_FLAGS.INTERACTION_CONFIGURATION_FLAG_SECONDARY_TAP,
                    interactionId = UnsafeNativeMethods.INTERACTION_ID.INTERACTION_ID_SECONDARY_TAP
                },
                new UnsafeNativeMethods.INTERACTION_CONTEXT_CONFIGURATION()
                {
                    enable = UnsafeNativeMethods.INTERACTION_CONFIGURATION_FLAGS.INTERACTION_CONFIGURATION_FLAG_MANIPULATION
                    | UnsafeNativeMethods.INTERACTION_CONFIGURATION_FLAGS.INTERACTION_CONFIGURATION_FLAG_MANIPULATION_TRANSLATION_INERTIA
                    | UnsafeNativeMethods.INTERACTION_CONFIGURATION_FLAGS.INTERACTION_CONFIGURATION_FLAG_MANIPULATION_TRANSLATION_X
                    | UnsafeNativeMethods.INTERACTION_CONFIGURATION_FLAGS.INTERACTION_CONFIGURATION_FLAG_MANIPULATION_TRANSLATION_Y,
                    interactionId = UnsafeNativeMethods.INTERACTION_ID.INTERACTION_ID_MANIPULATION
                },
            };

        #endregion

        #region Enumerations

        /// <summary>
        /// Determines the current hover tracking state
        /// </summary>
        private enum HoverState
        {
            AwaitingHover,
            TimingHover,
            HoverCancelled,
            InHover,
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Holds the reference to the interaction context
        /// </summary>
        private SecurityCriticalDataForSet<IntPtr> _interactionContext = new SecurityCriticalDataForSet<IntPtr>(IntPtr.Zero);

        /// <summary>
        /// The stylus device that owns this interaction engine.
        /// </summary>
        private PointerStylusDevice _stylusDevice = null;

        /// <summary>
        /// A callback for interaction events
        /// </summary>
        private UnsafeNativeMethods.INTERACTION_CONTEXT_OUTPUT_CALLBACK _callbackDelegate;

        #region Drag/Flick/Hold/Hover Tracking

        /// <summary>
        /// Has a drag been fired in the latest pointer message series
        /// </summary>
        private bool _firedDrag = false;

        /// <summary>
        /// Has a hold been fired in the latest pointer message series
        /// </summary>
        private bool _firedHold = false;

        /// <summary>
        /// Has a flick been fired in the latest pointer message series
        /// </summary>
        private bool _firedFlick = false;

        /// <summary>
        /// The current state of hover tracking
        /// </summary>
        private HoverState _hoverState;

        /// <summary>
        /// When hover started, in processor ticks
        /// </summary>
        private uint _hoverStartTicks = 0;

        /// <summary>
        /// The engine used to detect and fire flicks
        /// </summary>
        private PointerFlickEngine _flickEngine = null;

        #endregion

        #endregion

        #region Events

        /// <summary>
        /// An event to forward interactions back to the stack as touch gestures
        /// </summary>
        internal event EventHandler<RawStylusSystemGestureInputReport> InteractionDetected;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the interaction engine
        /// </summary>
        /// <param name="stylusDevice"></param>
        /// <param name="configuration"></param>
        internal PointerInteractionEngine(PointerStylusDevice stylusDevice, List<UnsafeNativeMethods.INTERACTION_CONTEXT_CONFIGURATION> configuration = null)
        {
            _stylusDevice = stylusDevice;

            // Only create a flick engine for Pen devices
            if (_stylusDevice.TabletDevice.Type == TabletDeviceType.Stylus)
            {
                // Currently disabled pending decision about flick support in Windows 10 RS3
                //_flickEngine = new PointerFlickEngine(_stylusDevice);
            }

            // Create our interaction context for gesture recognition
            IntPtr interactionContext = IntPtr.Zero;
            UnsafeNativeMethods.CreateInteractionContext(out interactionContext);
            _interactionContext = new SecurityCriticalDataForSet<IntPtr>(interactionContext);

            if (configuration == null)
            {
                configuration = DefaultConfiguration;
            }

            if (_interactionContext.Value != IntPtr.Zero)
            {
                // We do not want to filter specific pointers
                UnsafeNativeMethods.SetPropertyInteractionContext(_interactionContext.Value,
                    UnsafeNativeMethods.INTERACTION_CONTEXT_PROPERTY.INTERACTION_CONTEXT_PROPERTY_FILTER_POINTERS,
                    Convert.ToUInt32(false));

                // Use screen measurements here as this makes certain math easier for us
                UnsafeNativeMethods.SetPropertyInteractionContext(_interactionContext.Value,
                   UnsafeNativeMethods.INTERACTION_CONTEXT_PROPERTY.INTERACTION_CONTEXT_PROPERTY_MEASUREMENT_UNITS,
                   (UInt32)UnsafeNativeMethods.InteractionMeasurementUnits.Screen);

                // Configure the context
                UnsafeNativeMethods.SetInteractionConfigurationInteractionContext(_interactionContext.Value, (uint)configuration.Count, configuration.ToArray());

                // Store the delegate so it can be accessed over time
                _callbackDelegate = Callback;

                // Register for interaction notifications
                UnsafeNativeMethods.RegisterOutputCallbackInteractionContext(_interactionContext.Value, _callbackDelegate);
            }
        }

        #endregion

        #region IDisposable Support

        private bool _disposed = false;

        /// <summary>
        /// Destroy native resources
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                // We must destroy the interaction context when done
                if (_interactionContext.Value != IntPtr.Zero)
                {
                    UnsafeNativeMethods.DestroyInteractionContext(_interactionContext.Value);
                    _interactionContext.Value = IntPtr.Zero;
                }

                _disposed = true;
            }
        }

        ~PointerInteractionEngine()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Message Processing

        /// <summary>
        /// Update the interaction context with the latest pointer input
        /// </summary>
        /// <param name="rsir">The raw stylus input</param>
        internal void Update(RawStylusInputReport rsir)
        {
            try
            {
                // Queue up the latest message for processing
                UnsafeNativeMethods.BufferPointerPacketsInteractionContext(_interactionContext.Value, 1, new UnsafeNativeMethods.POINTER_INFO[] { _stylusDevice.CurrentPointerInfo });

                // Hover processing should occur directly from message receipt.
                // Do this prior to the IC engine processing so HoverEnter/Leave has priority.
                DetectHover();

                // This should be removed if flicks become unsupported
                DetectFlick(rsir);

                // Fire processing of the queued messages
                UnsafeNativeMethods.ProcessBufferedPacketsInteractionContext(_interactionContext.Value);
            }
            catch
            {
}
        }

        #endregion

        #region Callback Function

        /// <summary>
        /// Processes raw interaction output into gesture messages for the stack callback
        /// </summary>
        /// <param name="clientData">Unused, the interaction context pointer</param>
        /// <param name="output">The interaction output</param>
        private void Callback(IntPtr clientData, ref UnsafeNativeMethods.INTERACTION_CONTEXT_OUTPUT output)
        {
            SystemGesture gesture = SystemGesture.None;

            // Create the appropriate gesture based on interaction output
            switch (output.interactionId)
            {
                case UnsafeNativeMethods.INTERACTION_ID.INTERACTION_ID_TAP:
                    {
                        gesture = SystemGesture.Tap;
                    }
                    break;
                case UnsafeNativeMethods.INTERACTION_ID.INTERACTION_ID_SECONDARY_TAP:
                    {
                        gesture = SystemGesture.RightTap;
                    }
                    break;
                case UnsafeNativeMethods.INTERACTION_ID.INTERACTION_ID_HOLD:
                    {
                        _firedHold = true;

                        if (output.interactionFlags.HasFlag(UnsafeNativeMethods.INTERACTION_FLAGS.INTERACTION_FLAG_BEGIN))
                        {
                            gesture = SystemGesture.HoldEnter;
                        }
                        else
                        {
                            gesture = SystemGesture.HoldLeave;
                        }
                    }
                    break;
                case UnsafeNativeMethods.INTERACTION_ID.INTERACTION_ID_MANIPULATION:
                    {
                        gesture = DetectDragOrFlick(output);
                    }
                    break;
            }

            if (gesture != SystemGesture.None)
            {
                InteractionDetected?.Invoke(this,
                    new RawStylusSystemGestureInputReport(
                        InputMode.Foreground,
                        Environment.TickCount,
                        _stylusDevice.CriticalActiveSource,
                        (Func<StylusPointDescription>)null,
                        -1,
                        -1,
                        gesture,
                        Convert.ToInt32(output.x),
                        Convert.ToInt32(output.y),
                        0));
            }
        }

        #endregion

        #region Interaction Detection Functions       

        /// <summary>
        /// Updates and forwards flick engine results as a gesture
        /// </summary>
        /// <remarks>
        /// Remove this if flicks no longer supported
        /// </remarks>
        private void DetectFlick(RawStylusInputReport rsir)
        {
            // Make sure the flick engine has the latest data if applicable.
            _flickEngine?.Update(rsir);

            // If we have received an up then we should check if
            // we can still be a flick.  If this is true, then fire a flick.
            if (rsir.Actions == RawStylusActions.Up
                && (_flickEngine?.Result?.CanBeFlick ?? false))
            {
                // 

                InteractionDetected?.Invoke(this,
                    new RawStylusSystemGestureInputReport(
                        InputMode.Foreground,
                        Environment.TickCount,
                        _stylusDevice.CriticalActiveSource,
                        (Func<StylusPointDescription>)null,
                        -1,
                        -1,
                        SystemGesture.Flick,
                        Convert.ToInt32(_flickEngine.Result.TabletStart.X),
                        Convert.ToInt32(_flickEngine.Result.TabletStart.Y),
                        0));

                _firedFlick = true;
            }
        }

        /// <summary>
        /// Detects a hover and forwards it as a system gesture.
        /// </summary>
        private void DetectHover()
        {
            // Hover only applies to Pen
            if (_stylusDevice.TabletDevice.Type == TabletDeviceType.Stylus)
            {
                SystemGesture gesture = SystemGesture.None;

                if (_stylusDevice.IsNew)
                {
                    // Any new stylus should automatically await hover
                    _hoverState = HoverState.AwaitingHover;
                }

                switch (_hoverState)
                {
                    case HoverState.AwaitingHover:
                        {
                            if (_stylusDevice.InAir)
                            {
                                // If we see an InAir while awaiting, start timing
                                _hoverStartTicks = _stylusDevice.TimeStamp;
                                _hoverState = HoverState.TimingHover;
                            }
                        }
                        break;
                    case HoverState.TimingHover:
                        {
                            if (_stylusDevice.InAir)
                            {
                                if (_stylusDevice.TimeStamp < _hoverStartTicks)
                                {
                                    // We looped over in ticks, just retry using the new ticks as the start.
                                    // At worst this doubles the hover time, but it is rare and simplifies logic.
                                    _hoverStartTicks = _stylusDevice.TimeStamp;
                                }
                                else if (_stylusDevice.TimeStamp - _hoverStartTicks > HoverActivationThresholdTicks)
                                {
                                    // We should now activate a hover, so send HoverEnter and switch state
                                    gesture = SystemGesture.HoverEnter;
                                    _hoverState = HoverState.InHover;
                                }
                            }
                            else if (_stylusDevice.IsDown)
                            {
                                // The device is no longer in air so cancel
                                _hoverState = HoverState.HoverCancelled;
                            }
                        }
                        break;
                    case HoverState.HoverCancelled:
                        {
                            if (_stylusDevice.InAir)
                            {
                                // If we are back in air post cancellation, we might need to trigger another hover
                                // so restart the state machine
                                _hoverState = HoverState.AwaitingHover;
                            }
                        }
                        break;
                    case HoverState.InHover:
                        {
                            if (_stylusDevice.IsDown || !_stylusDevice.InRange)
                            {
                                // We are cancelling a hover so send HoverLeave and switch state.
                                gesture = SystemGesture.HoverLeave;
                                _hoverState = HoverState.HoverCancelled;
                            }
                        }
                        break;
                }

                if (gesture != SystemGesture.None)
                {
                    InteractionDetected?.Invoke(this,
                        new RawStylusSystemGestureInputReport(
                            InputMode.Foreground,
                            Environment.TickCount,
                            _stylusDevice.CriticalActiveSource,
                            (Func<StylusPointDescription>)null,
                            -1,
                            -1,
                            gesture,
                            Convert.ToInt32(_stylusDevice.RawStylusPoint.X),
                            Convert.ToInt32(_stylusDevice.RawStylusPoint.Y),
                            0));
                }
            }
        }

        /// <summary>
        /// If flicks are removed, clean up this code
        /// Detect a flick or a drag and send the appropriate gesture.  Flicks always
        /// take precedence over drags as a flick is basically a very fast drag/release.
        /// </summary>
        private SystemGesture DetectDragOrFlick(UnsafeNativeMethods.INTERACTION_CONTEXT_OUTPUT output)
        {
            SystemGesture gesture = SystemGesture.None;

            if (output.interactionFlags.HasFlag(UnsafeNativeMethods.INTERACTION_FLAGS.INTERACTION_FLAG_END))
            {
                // At the end of an interaction, any drag/flick/hold state is no longer needed
                _firedDrag = false;
                _firedHold = false;
                _firedFlick = false;
            }
            else
            {
                // If we have not already fired a drag/flick and we cannot be a flick
                if (!_firedDrag && !_firedFlick
                    && (!_flickEngine?.Result?.CanBeFlick ?? true))
                {
                    // Convert screen pixels to inches using current DPI
                    DpiScale dpi = VisualTreeHelper.GetDpi(_stylusDevice.CriticalActiveSource.RootVisual);

                    double xChangeInches = output.arguments.manipulation.cumulative.translationX / dpi.PixelsPerInchX;
                    double yChangeInches = output.arguments.manipulation.cumulative.translationY / dpi.PixelsPerInchY;

                    // If the cumulative change is greater than our threshold 
                    // (taken from WISP, converted to inches) then fire a drag.
                    if (xChangeInches > DragThresholdInches || yChangeInches > DragThresholdInches)
                    {
                        // If we have a current hold being tracked, convert to right drag
                        gesture = (_firedHold) ? SystemGesture.RightDrag : SystemGesture.Drag;

                        // This pointer has seen a drag
                        _firedDrag = true;
                    }
                }
            }

            return gesture;
        }

        #endregion
    }
}
