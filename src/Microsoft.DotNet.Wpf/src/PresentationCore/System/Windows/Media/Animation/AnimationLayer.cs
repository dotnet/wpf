// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Collections.Generic;
using System.Diagnostics;

namespace System.Windows.Media.Animation
{
    internal class AnimationLayer
    {
        private object _snapshotValue = DependencyProperty.UnsetValue;
        private IList<AnimationClock> _animationClocks;
        private AnimationStorage _ownerStorage;
        private EventHandler _removeRequestedHandler;
        private bool _hasStickySnapshotValue;

        internal AnimationLayer(AnimationStorage ownerStorage)
        {
            Debug.Assert(ownerStorage != null);

            _ownerStorage = ownerStorage;
            _removeRequestedHandler = new EventHandler(OnRemoveRequested);
        }

        internal void ApplyAnimationClocks(
            IList<AnimationClock> newAnimationClocks,
            HandoffBehavior handoffBehavior,
            object defaultDestinationValue)
        {
            Debug.Assert(
                newAnimationClocks == null
                || (newAnimationClocks.Count > 0
                    && !newAnimationClocks.Contains(null)));

            if (handoffBehavior == HandoffBehavior.SnapshotAndReplace)
            {
                Debug.Assert(defaultDestinationValue != DependencyProperty.UnsetValue,
                    "We need a valid default destination value when peforming a snapshot and replace.");

                EventHandler handler = new EventHandler(OnCurrentStateInvalidated);

                // If we have a sticky snapshot value, the clock that would have
                // unstuck it is being replaced, so we need to remove our event
                // handler from that clock.
                if (_hasStickySnapshotValue)
                {
                    _animationClocks[0].CurrentStateInvalidated -= handler;

                    DetachAnimationClocks();
                }
                // Otherwise if we have at least one clock take a new snapshot
                // value.
                else if (_animationClocks != null)
                {
                    _snapshotValue = GetCurrentValue(defaultDestinationValue);

                    DetachAnimationClocks();
                }
                // Otherwise we can use the defaultDestinationValue as the
                // new snapshot value.
                else
                {
                    _snapshotValue = defaultDestinationValue;
                }

                // If we have a new clock in a stopped state, then the snapshot 
                // value will be sticky.
                if (newAnimationClocks != null
                    && newAnimationClocks[0].CurrentState == ClockState.Stopped)
                {
                    _hasStickySnapshotValue = true;
                    newAnimationClocks[0].CurrentStateInvalidated += handler;
                }
                // Otherwise it won't be sticky.
                else
                {
                    _hasStickySnapshotValue = false;
                }

                SetAnimationClocks(newAnimationClocks);
            }
            else
            {
                Debug.Assert(handoffBehavior == HandoffBehavior.Compose,
                    "Unhandled handoffBehavior value.");
                Debug.Assert(defaultDestinationValue == DependencyProperty.UnsetValue,
                    "We shouldn't take the time to calculate a default destination value when it isn't needed.");

                if (newAnimationClocks == null)
                {
                    return;
                }
                else if (_animationClocks == null)
                {
                    SetAnimationClocks(newAnimationClocks);
                }
                else
                {
                    AppendAnimationClocks(newAnimationClocks);
                }
            }
        }

        private void DetachAnimationClocks()
        {
            Debug.Assert(_animationClocks != null);

            int count = _animationClocks.Count;

            for (int i = 0; i < count; i++)
            {
                _ownerStorage.DetachAnimationClock(_animationClocks[i], _removeRequestedHandler);
            }

            _animationClocks = null;
        }

        private void SetAnimationClocks(
            IList<AnimationClock> animationClocks)
        {
            Debug.Assert(animationClocks != null);
            Debug.Assert(animationClocks.Count > 0);
            Debug.Assert(!animationClocks.Contains(null));
            Debug.Assert(_animationClocks == null);

            _animationClocks = animationClocks;

            int count = animationClocks.Count;

            for (int i = 0; i < count; i++)
            {
                _ownerStorage.AttachAnimationClock(animationClocks[i], _removeRequestedHandler);
            }
        }

        private void OnCurrentStateInvalidated(object sender, EventArgs args)
        {
            Debug.Assert(_hasStickySnapshotValue, 
                "_hasStickySnapshotValue should be set to true if OnCurrentStateInvalidated has been called.");

            _hasStickySnapshotValue = false;

            ((AnimationClock)sender).CurrentStateInvalidated -= new EventHandler(OnCurrentStateInvalidated);
        }

        private void OnRemoveRequested(object sender, EventArgs args)
        {
            Debug.Assert(_animationClocks != null
                         && _animationClocks.Count > 0,
                "An AnimationClock no longer associated with a property should not have a RemoveRequested event handler.");

            AnimationClock animationClock = (AnimationClock)sender;

            int index = _animationClocks.IndexOf(animationClock);

            Debug.Assert(index >= 0,
                "An AnimationClock no longer associated with a property should not have a RemoveRequested event handler.");

            if (_hasStickySnapshotValue
                && index == 0)
            {
                _animationClocks[0].CurrentStateInvalidated -= new EventHandler(OnCurrentStateInvalidated);
                _hasStickySnapshotValue = false;
            }

            _animationClocks.RemoveAt(index);

            _ownerStorage.DetachAnimationClock(animationClock, _removeRequestedHandler);

            AnimationStorage tmpOwnerStorage = _ownerStorage;

            if (_animationClocks.Count == 0)
            {
                _animationClocks = null;
                _snapshotValue = DependencyProperty.UnsetValue;
                _ownerStorage.RemoveLayer(this);
                _ownerStorage = null;
            }

            // _ownerStorage may be null here.

            tmpOwnerStorage.WritePostscript();
        }

        private void AppendAnimationClocks(
            IList<AnimationClock> newAnimationClocks)
        {
            Debug.Assert(newAnimationClocks != null);
            Debug.Assert(newAnimationClocks.Count > 0);
            Debug.Assert(!newAnimationClocks.Contains(null));
            // _animationClocks may be null or non-null here.

            int newClocksCount = newAnimationClocks.Count;
            List<AnimationClock> animationClockList = _animationClocks as List<AnimationClock>;

            // If _animationClocks is not a List<AnimationClock> then make it one.
            if (animationClockList == null)
            {
                int oldClocksCount = (_animationClocks == null) ? 0 : _animationClocks.Count;

                animationClockList = new List<AnimationClock>(oldClocksCount + newClocksCount);

                for (int i = 0; i < oldClocksCount; i++)
                {
                    animationClockList.Add(_animationClocks[i]);
                }

                _animationClocks = animationClockList;
            }

            for (int i = 0; i < newClocksCount; i++)
            {
                AnimationClock clock = newAnimationClocks[i];

                animationClockList.Add(clock);
                _ownerStorage.AttachAnimationClock(clock, _removeRequestedHandler);
            }
        }

        internal object GetCurrentValue(
            object defaultDestinationValue)
        {
            Debug.Assert(defaultDestinationValue != DependencyProperty.UnsetValue);

            // We have a sticky snapshot value if changes have been made to the
            // animations in this layer since the last tick. This flag will be
            // unset when the next tick starts.
            //
            // Since CurrentTimeInvaliated is raised before CurrentStateInvalidated
            // we need to check the state of the first clock as well to avoid
            // potential first frame issues. In this case _hasStickySnapshotValue
            // will be updated to false shortly.
            if (   _hasStickySnapshotValue
                && _animationClocks[0].CurrentState == ClockState.Stopped)
            {
                return _snapshotValue;
            }

            // This layer just contains a snapshot value or a fill value after
            // all the clocks have been completed.
            if (_animationClocks == null)
            {
                Debug.Assert(_snapshotValue != DependencyProperty.UnsetValue);

                // In this case, we're using _snapshotValue to store the value,
                // but the value here does not represent the snapshotValue. It
                // represents the fill value of an animation clock that has been
                // removed for performance reason because we know it will never
                // be restarted.

                return _snapshotValue;
            }

            object currentLayerValue = _snapshotValue;
            bool hasUnstoppedClocks = false;

            if (currentLayerValue == DependencyProperty.UnsetValue)
            {
                currentLayerValue = defaultDestinationValue;
            }

            int count = _animationClocks.Count;

            Debug.Assert(count > 0);

            for (int i = 0; i < count; i++)
            {
                AnimationClock clock = _animationClocks[i];

                if (clock.CurrentState != ClockState.Stopped)
                {
                    hasUnstoppedClocks = true;

                    currentLayerValue = clock.GetCurrentValue(
                        currentLayerValue,
                        defaultDestinationValue);
                }
            }

            if (hasUnstoppedClocks)
            {
                return currentLayerValue;
            }
            else
            {
                return defaultDestinationValue;
            }
        }
    }
}
