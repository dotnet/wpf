// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Diagnostics;

namespace System.Windows.Input
{
    /// <summary>
    ///     Handles detection of TwoFingerTap and Rollover static gestures.
    /// </summary>
    internal class MultiTouchSystemGestureLogic
    {
        internal MultiTouchSystemGestureLogic()
        {
            _currentState = State.Idle;
            Reset();
        }

        /// <summary>
        ///     Analyzes stylus input and determines if a SystemGesture should be generated.
        /// </summary>
        /// <param name="stylusInputReport">The latest stylus input report.</param>
        /// <returns>The SystemGesture to generate, null otherwise.</returns>
        internal SystemGesture? GenerateStaticGesture(RawStylusInputReport stylusInputReport)
        {
            switch (stylusInputReport.Actions)
            {
                case RawStylusActions.Down:
                    OnTouchDown(stylusInputReport);
                    return null;

                case RawStylusActions.Up:
                    return OnTouchUp(stylusInputReport);


                case RawStylusActions.SystemGesture:
                    OnSystemGesture((RawStylusSystemGestureInputReport)stylusInputReport);
                    return null;

                    /*
                case RawStylusActions.Move:
                case RawStylusActions.Activate:
                case RawStylusActions.Deactivate:
                case RawStylusActions.OutOfRange:
                    return null;
                     */

                default:
                    return null;
            }
        }

        private void OnTouchDown(RawStylusInputReport stylusInputReport)
        {
            switch (_currentState)
            {
                case State.Idle:
                    // The first finger came down
                    Reset(); // Clear old settings
                    _firstStylusDeviceId = stylusInputReport.StylusDeviceId;
                    _currentState = State.OneFingerDown;
                    _firstDownTime = Environment.TickCount;
                    break;

                case State.OneFingerDown:
                    // The second finger came down
                    Debug.Assert(_firstStylusDeviceId != null && _firstStylusDeviceId != stylusInputReport.StylusDeviceId);
                    Debug.Assert(_secondStylusDeviceId == null);
                    _secondStylusDeviceId = stylusInputReport.StylusDeviceId;
                    _currentState = State.TwoFingersDown;
                    break;

                    // Ignoring fingers beyond two
            }
        }

        private SystemGesture? OnTouchUp(RawStylusInputReport stylusInputReport)
        {
            switch (_currentState)
            {
                case State.TwoFingersDown:
                    if (IsTrackedStylusId(stylusInputReport.StylusDeviceId))
                    {
                        // One of the two fingers released
                        _firstUpTime = Environment.TickCount;
                        _currentState = State.OneFingerInStaticGesture;
                    }
                    break;

                case State.OneFingerDown:
                    if (IsTrackedStylusId(stylusInputReport.StylusDeviceId))
                    {
                        // The single finger that was down was released
                        _currentState = State.Idle;
                    }
                    break;

                case State.OneFingerInStaticGesture:
                    // The last of two fingers released
                    _currentState = State.Idle;

                    // Check the times and see if a TwoFingerTap or Rollover should be generated.
                    // TwoFingerTap is more constrained than Rollover, so check it first.
                    if (IsTwoFingerTap())
                    {
                        return SystemGesture.TwoFingerTap;
                    }
#if ROLLOVER_IMPLEMENTED
                    else if (IsRollover())
                    {
                        return SystemGesture.Rollover;
                    }
#endif
                    break;

                case State.TwoFingersInWisptisGesture:
                    if (IsTrackedStylusId(stylusInputReport.StylusDeviceId))
                    {
                        // One of the two fingers released.
                        _currentState = State.OneFingerInWisptisGesture;
                    }
                    break;

                case State.OneFingerInWisptisGesture:
                    if (IsTrackedStylusId(stylusInputReport.StylusDeviceId))
                    {
                        // The last finger released
                        _currentState = State.Idle;
                    }
                    break;
}

            return null;
        }

        private void OnSystemGesture(RawStylusSystemGestureInputReport stylusInputReport)
        {
            switch (_currentState)
            {
                case State.TwoFingersDown:
                    switch (stylusInputReport.SystemGesture)
                    {
                        case SystemGesture.Drag:
                        case SystemGesture.RightDrag:
                        case SystemGesture.Flick:
                            // One of the two fingers made a Wisptis detected gesture
                            // that prevents TwoFingerTap or Rollover.
                            _currentState = State.TwoFingersInWisptisGesture;
                            break;
                    }
                    break;

                case State.OneFingerInStaticGesture:
                case State.OneFingerDown:
                    switch (stylusInputReport.SystemGesture)
                    {
                        case SystemGesture.Drag:
                        case SystemGesture.RightDrag:
                        case SystemGesture.Flick:
                            // A finger made a Wisptis detected gesture
                            // that prevents TwoFingerTap or Rollover.
                            _currentState = State.OneFingerInWisptisGesture;
                            break;
                    }
                    break;
            }
        }

        private void Reset()
        {
            _firstStylusDeviceId = null;
            _secondStylusDeviceId = null;
            _firstDownTime = 0;
            _firstUpTime = 0;
        }

        private bool IsTrackedStylusId(int id)
        {
            return (id == _firstStylusDeviceId) || (id == _secondStylusDeviceId);
        }

        private bool IsTwoFingerTap()
        {
            int now = Environment.TickCount;
            int sinceFirstDown = now - _firstDownTime;
            int sinceFirstUp = now - _firstUpTime;
            return (sinceFirstUp < TwoFingerTapTime) && (sinceFirstDown < RolloverTime);
        }

#if ROLLOVER_IMPLEMENTED
        private bool IsRollover()
        {
            int sinceFirstDown = Environment.TickCount - _firstDownTime;
            return sinceFirstDown < RolloverTime;
        }
#endif

        private State _currentState;
        private int? _firstStylusDeviceId;
        private int? _secondStylusDeviceId;
        private int _firstDownTime;
        private int _firstUpTime;

        // These numbers come from some usability tests and have yet
        // to become system parameters. If Windows exposes these as parameters,
        // then query the system for these values.
        private const int TwoFingerTapTime = 150;
        private const int RolloverTime = 1158;

        private enum State
        {
            Idle,
            OneFingerDown,
            TwoFingersDown,
            OneFingerInStaticGesture,
            TwoFingersInWisptisGesture,
            OneFingerInWisptisGesture,
        }
    }
}
