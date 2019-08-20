// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using MS.Internal;
using MS.Utility;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Media.Composition;
using System.Windows.Media.Media3D;

using MS.Internal.PresentationCore;     // SR, SRID, FriendAccessAllowed

namespace System.Windows.Media.Animation
{
    internal class AnimationStorage
    {
#if DEBUG

        private static int _nextID = 1;

        private readonly int DebugID = _nextID++;

#endif

        #region Constructor

        protected AnimationStorage()
        {
            _currentTimeInvalidatedHandler = new EventHandler(OnCurrentTimeInvalidated);
            _removeRequestedHandler = new EventHandler(OnRemoveRequested);
        }

        #endregion

        #region Uncommon Fields

        private static readonly UncommonField<FrugalMap> AnimatedPropertyMapField = new UncommonField<FrugalMap>();

        #endregion

        #region Properties

        internal bool IsEmpty
        {
            get
            {
                Debug.Assert(   _animationClocks == null
                             || _animationClocks.Count > 0);

                Debug.Assert(   _propertyTriggerLayers == null
                             || _propertyTriggerLayers.Count > 0);

                return _animationClocks == null
                    && _propertyTriggerLayers == null
                    && _snapshotValue == DependencyProperty.UnsetValue;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Attaches an AnimationClock but does not add it to the collection.
        /// </summary>
        /// <remarks>
        /// It's expected that the AnimationClock will have been added to the
        /// collection before this is called.
        /// </remarks>
        /// <param name="animationClock">The AnimationClock.</param>
        /// <param name="removeRequestedHandler">
        /// The event handler to be executed when the RemoveRequested event is
        /// raised by the AnimationClock.
        /// </param>
        internal void AttachAnimationClock(
            AnimationClock animationClock,
            EventHandler removeRequestedHandler)
        {
            Debug.Assert(animationClock != null);
            Debug.Assert(_dependencyObject.Target != null);
            Debug.Assert(_currentTimeInvalidatedHandler != null);

            animationClock.CurrentTimeInvalidated += _currentTimeInvalidatedHandler;

            if (animationClock.HasControllableRoot)
            {
                animationClock.RemoveRequested += removeRequestedHandler;
            }
        }

        /// <summary>
        /// Detaches an AnimationClock but does not remove it from the collection.
        /// </summary>
        /// <remarks>
        /// It's expected that the AnimationClock will be removed from the
        /// collection immediately after calling this method.
        /// </remarks>
        /// <param name="animationClock">The AnimationClock.</param>
        /// <param name="removeRequestedHandler">
        /// The event handler associated with the RemoveRequested event on the
        /// AnimationClock.
        /// </param>
        internal void DetachAnimationClock(
            AnimationClock animationClock,
            EventHandler removeRequestedHandler)
        {
            Debug.Assert(animationClock != null);
            Debug.Assert(_currentTimeInvalidatedHandler != null);

            animationClock.CurrentTimeInvalidated -= _currentTimeInvalidatedHandler;

            if (animationClock.HasControllableRoot)
            {
                animationClock.RemoveRequested -= removeRequestedHandler;
            }
        }

        internal void Initialize(DependencyObject d, DependencyProperty dp)
        {
            Debug.Assert(_dependencyObject == null);
            Debug.Assert(_dependencyProperty == null);

            Animatable a = d as Animatable;

            if (a != null)
            {
                _dependencyObject = a.GetWeakReference();
            }
            else
            {
                // 

                _dependencyObject = new WeakReference(d);
            }

            _dependencyProperty = dp;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="layer"></param>
        internal void RemoveLayer(AnimationLayer layer)
        {
            Debug.Assert(_propertyTriggerLayers != null);
            Debug.Assert(_propertyTriggerLayers.ContainsValue(layer));

            int index = _propertyTriggerLayers.IndexOfValue(layer);

            Debug.Assert(index >= 0);

            _propertyTriggerLayers.RemoveAt(index);

            if (_propertyTriggerLayers.Count == 0)
            {
                _propertyTriggerLayers = null;
            }
        }

        /// <summary>
        /// This should be called at the end of any method that alters the
        /// storage in any way.  This method will make sure the peer dp is
        /// set correctly and notify Animatables if something changes.
        /// </summary>
        internal void WritePostscript()
        {
            DependencyObject d = (DependencyObject)_dependencyObject.Target;

            if (d == null)
            {
                return;
            }

            FrugalMap animatedPropertyMap = AnimatedPropertyMapField.GetValue(d);

            if (   animatedPropertyMap.Count == 0
                || animatedPropertyMap[_dependencyProperty.GlobalIndex] == DependencyProperty.UnsetValue)
            {
                if (!IsEmpty)
                {
                    // This is kind of tricky:
                    //
                    // Because FrugalMap is a struct instead of a class, we must
                    // be sure to add this AnimationStorage to the map before
                    // setting the FrugalMap into the UncommonField storage. If
                    // we don't and the FrugalMap is empty then an empty FrugalMap
                    // will be set into the UncommonField storage. If we were to
                    // then add our AnimationStorage to the local FrugalMap, it
                    // would only allocate its own internal storage at that point
                    // which would not apply to the FrugalMap we set into the
                    // UncommonField storage. Once a FrugalMap has allocated its
                    // internal storage, though, that storage is copied with the
                    // FrugalMap. This is what will happen when we add our
                    // AnimationStorage to the FrugalMap first as we do below:

                    animatedPropertyMap[_dependencyProperty.GlobalIndex] = this;

                    // Since FrugalMap is a struct and adding a new value to it
                    // may re-allocate the storage, we need to set this value
                    // each time we make a change to the map.

                    AnimatedPropertyMapField.SetValue(d, animatedPropertyMap);

                    if (animatedPropertyMap.Count == 1)
                    {
                        d.IAnimatable_HasAnimatedProperties = true;
                    }

                    // If this the target is an Animatable we'll need to
                    // invalidate it so that the animation resource for this
                    // newly animated property will be passed across to the UCE.
                    Animatable a = d as Animatable;

                    if (a != null)
                    {
                        a.RegisterForAsyncUpdateResource();
                    }

                    // If this AnimationStorage is a resource, add it to the
                    // channel now.
                    DUCE.IResource animationResource = this as DUCE.IResource;

                    if (animationResource != null)
                    {
                        DUCE.IResource targetResource = d as DUCE.IResource;

                        if (targetResource != null)
                        {
                            using (CompositionEngineLock.Acquire())
                            {
                                int channelCount = targetResource.GetChannelCount();

                                for (int i = 0; i < channelCount; i++)
                                {
                                    DUCE.Channel channel = targetResource.GetChannel(i);
                                    if (!targetResource.GetHandle(channel).IsNull)
                                    {
                                        animationResource.AddRefOnChannel(channel);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.Assert(animatedPropertyMap.Count > 0);
                Debug.Assert(animatedPropertyMap[_dependencyProperty.GlobalIndex] != DependencyProperty.UnsetValue);

                if (IsEmpty)
                {
                    // If this AnimationStorage is a resource, release it from
                    // the channel now.
                    DUCE.IResource animationResource = this as DUCE.IResource;

                    if (animationResource != null)
                    {
                        DUCE.IResource targetResource = d as DUCE.IResource;

                        if (targetResource != null)
                        {
                            using (CompositionEngineLock.Acquire())
                            {
                                int channelCount = targetResource.GetChannelCount();

                                for (int i = 0; i < channelCount; i++)
                                {
                                    DUCE.Channel channel = targetResource.GetChannel(i);
                                    if (!targetResource.GetHandle(channel).IsNull)
                                    {
                                        animationResource.ReleaseOnChannel(channel);
                                    }
                                }
                            }
                        }
                    }

                    // If this the target is an Animatable we'll need to
                    // invalidate it so that the animation resource for this
                    // no longer animated property will no longer be passed
                    // across to the UCE.
                    Animatable a = d as Animatable;

                    if (a != null)
                    {
                        a.RegisterForAsyncUpdateResource();
                    }

                    animatedPropertyMap[_dependencyProperty.GlobalIndex] = DependencyProperty.UnsetValue;

                    if (animatedPropertyMap.Count == 0)
                    {
                        AnimatedPropertyMapField.ClearValue(d);

                        d.IAnimatable_HasAnimatedProperties = false;
                    }
                    else
                    {
                        AnimatedPropertyMapField.SetValue(d, animatedPropertyMap);
                    }

                    // We've removed animation storage for this DP, so if we were storing the local
                    // base value here then it has to go back to its non-animated storage spot.
                    if (_baseValue != DependencyProperty.UnsetValue)
                    {
                        d.SetValue(_dependencyProperty, _baseValue);
                    }
                }
            }

            // recompute animated value
            d.InvalidateProperty(_dependencyProperty);
        }

        internal void EvaluateAnimatedValue(
            PropertyMetadata    metadata,
            ref EffectiveValueEntry entry)
        {
            DependencyObject d = (DependencyObject)_dependencyObject.Target;

            if (d == null)
            {
                return;
            }

            object value = entry.GetFlattenedEntry(RequestFlags.FullyResolved).Value;
            if (entry.IsDeferredReference)
            {
                DeferredReference dr = (DeferredReference)value;
                value = dr.GetValue(entry.BaseValueSourceInternal);

                // Set the baseValue back into the entry
                entry.SetAnimationBaseValue(value);
            }

            object animatedValue = GetCurrentPropertyValue(this, d, _dependencyProperty, metadata, value);

            if (!_dependencyProperty.IsValidValueInternal(animatedValue))
            {
                // If the animation(s) applied to the property have calculated an
                // invalid value for the property then raise an exception.
                throw new InvalidOperationException(
                    SR.Get(
                        SRID.Animation_CalculatedValueIsInvalidForProperty,
                        _dependencyProperty.Name,
                        null));
            }
            
            entry.SetAnimatedValue(animatedValue, value);
        }

        #endregion

        #region Private

        private void OnCurrentTimeInvalidated(object sender, EventArgs args)
        {
            object target = _dependencyObject.Target;

            if (target == null)
            {
                // If the target has been garbage collected, remove this handler
                // from the AnimationClock so that this collection can be
                // released also.
                DetachAnimationClock((AnimationClock)sender, _removeRequestedHandler);
            }
            else
            {
                // recompute animated value
                try
                {
                    DependencyObject targetDO = ((DependencyObject)target);

                    // fetch the existing entry
                    EffectiveValueEntry oldEntry = targetDO.GetValueEntry(
                            targetDO.LookupEntry(_dependencyProperty.GlobalIndex),
                            _dependencyProperty,
                            null,
                            RequestFlags.RawEntry);

                    EffectiveValueEntry newEntry;
                    object value;

                    // create a copy of that entry, removing animated & coerced values

                    if (!oldEntry.HasModifiers)
                    {
                        // no modifiers; just use it, removing deferred references
                        newEntry = oldEntry;
                        value = newEntry.Value;
                        if (newEntry.IsDeferredReference)
                        {
                            value = ((DeferredReference) value).GetValue(newEntry.BaseValueSourceInternal);
                            newEntry.Value = value;
                        }
                    }
                    else
                    {
                        // else entry has modifiers; preserve expression but throw away
                        // coerced & animated values, since we'll be recomputing an animated value
                        newEntry = new EffectiveValueEntry();
                        newEntry.BaseValueSourceInternal = oldEntry.BaseValueSourceInternal;
                        newEntry.PropertyIndex = oldEntry.PropertyIndex;
                        newEntry.HasExpressionMarker = oldEntry.HasExpressionMarker;

                        value = oldEntry.ModifiedValue.BaseValue;
                        if (oldEntry.IsDeferredReference)
                        {
                            DeferredReference dr = value as DeferredReference;
                            if (dr != null)
                            {
                                value = dr.GetValue(newEntry.BaseValueSourceInternal);
                            }
                        }

                        newEntry.Value = value;

                        if (oldEntry.IsExpression)
                        {
                            value = oldEntry.ModifiedValue.ExpressionValue;
                            if (oldEntry.IsDeferredReference)
                            {
                                DeferredReference dr = value as DeferredReference;
                                if (dr != null)
                                {
                                    value = dr.GetValue(newEntry.BaseValueSourceInternal);
                                }
                            }
                            newEntry.SetExpressionValue(value, newEntry.Value);
                        }
                    }

                    // compute the new value for the property

                    PropertyMetadata metadata = _dependencyProperty.GetMetadata(targetDO.DependencyObjectType);
                    object animatedValue = AnimationStorage.GetCurrentPropertyValue(this, targetDO, _dependencyProperty, metadata, value);

                    if (_dependencyProperty.IsValidValueInternal(animatedValue))
                    {
                        // update the new entry to contain the new animated value
                        newEntry.SetAnimatedValue(animatedValue, value);

                        // call UpdateEffectiveValue to put the new entry in targetDO's effective values table
                        targetDO.UpdateEffectiveValue(
                                targetDO.LookupEntry(_dependencyProperty.GlobalIndex),
                                _dependencyProperty,
                                metadata,
                                oldEntry,
                                ref newEntry,
                                false /* coerceWithDeferredReference */,
                                false /* coerceWithCurrentValue */,
                                OperationType.Unknown);

                        if (_hadValidationError)
                        {
                            if (TraceAnimation.IsEnabled)
                            {
                                TraceAnimation.TraceActivityItem(
                                    TraceAnimation.AnimateStorageValidationNoLongerFailing,
                                    this,
                                    animatedValue,
                                    target,
                                    _dependencyProperty);

                                _hadValidationError = false;
                            }
                        }
                    }
                    else if(!_hadValidationError)
                    {
                        if (TraceAnimation.IsEnabled)
                        {
                            TraceAnimation.TraceActivityItem(
                                TraceAnimation.AnimateStorageValidationFailed,
                                this,
                                animatedValue,
                                target,
                                _dependencyProperty);
                        }

                        _hadValidationError = true;
                    }
                }
                catch (Exception e)
                {
                    // Catch all exceptions thrown during the InvalidateProperty callstack
                    // and wrap them in an AnimationException

                    throw new AnimationException(
                        (AnimationClock)sender,
                        _dependencyProperty,
                        (IAnimatable)target,
                        SR.Get(
                            SRID.Animation_Exception,
                            _dependencyProperty.Name,
                            target.GetType().FullName,
                            ((AnimationClock)sender).Timeline.GetType().FullName),
                        e);
                }
            }
        }

        private void OnRemoveRequested(object sender, EventArgs args)
        {
            Debug.Assert(   _animationClocks != null
                         && _animationClocks.Count > 0,
                "An AnimationClock no longer associated with a property should not have a RemoveRequested event handler.");

            AnimationClock animationClock = (AnimationClock)sender;

            int index = _animationClocks.IndexOf(animationClock);

            Debug.Assert(index > -1,
                "An AnimationClock no longer associated with a property should not have a RemoveRequested event handler.");

            _animationClocks.RemoveAt(index);

            if (   _hasStickySnapshotValue
                && index == 0)
            {
                // The first clock is always the one that would have unstuck
                // the snapshot value. Since it's been removed, instick the
                // snapshot value now.

                _hasStickySnapshotValue = false;
                animationClock.CurrentStateInvalidated -= new EventHandler(OnCurrentStateInvalidated);
            }

            if (_animationClocks.Count == 0)
            {
                _animationClocks = null;
                _snapshotValue = DependencyProperty.UnsetValue;
            }

            DetachAnimationClock(animationClock, _removeRequestedHandler);

            WritePostscript();
        }

        private void OnCurrentStateInvalidated(object sender, EventArgs args)
        {
            Debug.Assert(_hasStickySnapshotValue,
                "_hasStickySnapshotValue should be set to true if OnCurrentStateInvalidated has been called.");

            _hasStickySnapshotValue = false;

            ((AnimationClock)sender).CurrentStateInvalidated -= new EventHandler(OnCurrentStateInvalidated);
        }

        private void ClearAnimations()
        {
            if (_animationClocks != null)
            {
                Debug.Assert(_animationClocks.Count > 0);

                for (int i = 0; i < _animationClocks.Count; i++)
                {
                    DetachAnimationClock(_animationClocks[i], _removeRequestedHandler);
                }

                _animationClocks = null;
            }
        }

#endregion

        #region Static Helper Methods

        internal static void ApplyAnimationClock(
            DependencyObject d,
            DependencyProperty dp,
            AnimationClock animationClock,
            HandoffBehavior handoffBehavior)
        {
            if (animationClock == null)
            {
                BeginAnimation(d, dp, null, handoffBehavior);
            }
            else
            {
                // Optimize to avoid allocation of potentially unneeded array.
                ApplyAnimationClocks(d, dp, new AnimationClock[] { animationClock }, handoffBehavior);
            }
        }

        [FriendAccessAllowed] // Built into Core, also used by Framework.
        internal static void ApplyAnimationClocks(
            DependencyObject d,
            DependencyProperty dp,
            IList<AnimationClock> animationClocks,
            HandoffBehavior handoffBehavior)
        {
            Debug.Assert(animationClocks != null,
                "The animationClocks parameter should not be passed in as null.");
            Debug.Assert(animationClocks.Count > 0,
                "The animationClocks parameter should contain at least one clock.");
            Debug.Assert(!animationClocks.Contains(null),
                "The animationClocks parameter should not contain a null entry.");
            Debug.Assert(HandoffBehaviorEnum.IsDefined(handoffBehavior),
                "Public API caller of this internal method is responsible for validating that the HandoffBehavior value is valid." );

            AnimationStorage storage = GetStorage(d, dp);

            // handoffBehavior is SnapshotAndReplace or the situation is such
            // that it is the equivalent because we have nothing to compose
            // with.
            if (   handoffBehavior == HandoffBehavior.SnapshotAndReplace
                || storage == null
                || storage._animationClocks == null)
            {
                if (storage != null)
                {
                    EventHandler handler = new EventHandler(storage.OnCurrentStateInvalidated);

                    // If we have a sticky snapshot value, the clock that would have
                    // unstuck it is being replaced, so we need to remove our event
                    // handler from that clock.
                    if (storage._hasStickySnapshotValue)
                    {
                        storage._animationClocks[0].CurrentStateInvalidated -= handler;
                    }
                    // Calculate a snapshot value if we don't already have one
                    // since the last tick.
                    else
                    {
                        storage._snapshotValue = d.GetValue(dp);
                    }

                    // If we have a new clock in a stopped state, then the snapshot
                    // value will be sticky.
                    if (animationClocks[0].CurrentState == ClockState.Stopped)
                    {
                        storage._hasStickySnapshotValue = true;
                        animationClocks[0].CurrentStateInvalidated += new EventHandler(storage.OnCurrentStateInvalidated);
                    }
                    // Otherwise it won't be sticky.
                    else
                    {
                        storage._hasStickySnapshotValue = false;
                    }

                    storage.ClearAnimations();
                }
                else
                {
                    storage = CreateStorage(d, dp);
                }

                // Add and attach new animation.
                storage._animationClocks = new FrugalObjectList<AnimationClock>(animationClocks.Count);

                for (int i = 0; i < animationClocks.Count; i++)
                {
                    Debug.Assert(animationClocks[i] != null);

                    storage._animationClocks.Add(animationClocks[i]);
                    storage.AttachAnimationClock(animationClocks[i], storage._removeRequestedHandler);
                }
            }
            else
            {
                Debug.Assert(handoffBehavior == HandoffBehavior.Compose);
                Debug.Assert(storage != null);
                Debug.Assert(storage._animationClocks != null);

                FrugalObjectList<AnimationClock> newClockCollection = new FrugalObjectList<AnimationClock>(storage._animationClocks.Count + animationClocks.Count);

                for (int i = 0; i < storage._animationClocks.Count; i++)
                {
                    newClockCollection.Add(storage._animationClocks[i]);
                }

                storage._animationClocks = newClockCollection;

                for (int i = 0; i < animationClocks.Count; i++)
                {
                    newClockCollection.Add(animationClocks[i]);
                    storage.AttachAnimationClock(animationClocks[i], storage._removeRequestedHandler);
                }
            }

            storage.WritePostscript();
        }

        /// <summary>
        /// Applies animation clocks to a layer
        /// </summary>
        /// <param name="d"></param>
        /// <param name="dp"></param>
        /// <param name="animationClocks"></param>
        /// <param name="handoffBehavior"></param>
        /// <param name="propertyTriggerLayerIndex"></param>
        [FriendAccessAllowed]
        internal static void ApplyAnimationClocksToLayer(
            DependencyObject d,
            DependencyProperty dp,
            IList<AnimationClock> animationClocks,
            HandoffBehavior handoffBehavior,
            Int64 propertyTriggerLayerIndex)
        {
            if( propertyTriggerLayerIndex == 1 )
            {
                // Layer 1 is a special layer, where it gets treated as if there
                //  was no layer specification at all.
                ApplyAnimationClocks( d, dp, animationClocks, handoffBehavior );
                return;
            }

            Debug.Assert(animationClocks != null);
            Debug.Assert(!animationClocks.Contains(null));

            Debug.Assert(HandoffBehaviorEnum.IsDefined(handoffBehavior),
                "Public API caller of this internal method is responsible for validating that the HandoffBehavior value is valid.");

            AnimationStorage storage = GetStorage(d, dp);

            if (storage == null)
            {
                storage = CreateStorage(d, dp);
            }

            SortedList<Int64, AnimationLayer> propertyTriggerLayers = storage._propertyTriggerLayers;

            if (propertyTriggerLayers == null)
            {
                propertyTriggerLayers = new SortedList<Int64, AnimationLayer>(1);
                storage._propertyTriggerLayers = propertyTriggerLayers;
            }

            AnimationLayer layer;

            if (propertyTriggerLayers.ContainsKey(propertyTriggerLayerIndex))
            {
                layer = propertyTriggerLayers[propertyTriggerLayerIndex];
            }
            else
            {
                layer = new AnimationLayer(storage);

                propertyTriggerLayers[propertyTriggerLayerIndex] = layer;
            }

            object defaultDestinationValue = DependencyProperty.UnsetValue;

            if (handoffBehavior == HandoffBehavior.SnapshotAndReplace)
            {
                // consider walking through previous layers as well.
                defaultDestinationValue = ((IAnimatable)d).GetAnimationBaseValue(dp);

                int count = propertyTriggerLayers.Count;

                if (count > 1)
                {
                    IList<Int64> keys = propertyTriggerLayers.Keys;

                    for (int i = 0; i < count && keys[i] < propertyTriggerLayerIndex; i++)
                    {
                        AnimationLayer currentLayer;

                        propertyTriggerLayers.TryGetValue(keys[i], out currentLayer);

                        defaultDestinationValue = currentLayer.GetCurrentValue(defaultDestinationValue);
                    }
                }
            }

            layer.ApplyAnimationClocks(
                animationClocks,
                handoffBehavior,
                defaultDestinationValue);

            storage.WritePostscript();
        }

        internal static void BeginAnimation(
            DependencyObject d,
            DependencyProperty dp,
            AnimationTimeline animation,
            HandoffBehavior handoffBehavior)
        {
            // Caller should be validating animation.
            Debug.Assert(animation == null || IsAnimationValid(dp, animation));
            Debug.Assert(IsPropertyAnimatable(d, dp));

            Debug.Assert(HandoffBehaviorEnum.IsDefined(handoffBehavior),
                "Public API caller of this internal method is responsible for validating that the HandoffBehavior value is valid." );

            AnimationStorage storage = GetStorage(d, dp);

            if (animation == null)
            {
                if (   storage == null
                    || handoffBehavior == HandoffBehavior.Compose)
                {
                    // Composing with a null animation is a no-op.
                    return;
                }
                else
                {
                    // When the incoming animation is passed in as null and
                    // handoffBehavior == HandoffBehavior.SnapshotAndReplace it means
                    // that we should stop any and all animation behavior for
                    // this property.
                    if (storage._hasStickySnapshotValue)
                    {
                        storage._hasStickySnapshotValue = false;
                        storage._animationClocks[0].CurrentStateInvalidated -= new EventHandler(storage.OnCurrentStateInvalidated);
                    }

                    storage._snapshotValue = DependencyProperty.UnsetValue;
                    storage.ClearAnimations();
                }
            }
            else if (animation.BeginTime.HasValue)
            {
                // We have an animation
                AnimationClock animationClock;
                animationClock = animation.CreateClock();  // note that CreateClock also calls InternalBeginIn

                ApplyAnimationClocks(d, dp, new AnimationClock[] { animationClock }, handoffBehavior);

                // ApplyAnimationClocks has fixed up the storage and called
                // WritePostscript already so we can just return.
                return;
            }
            else if (storage == null)
            {
                //  The user gave us an animation with a BeginTime of null which
                // means snapshot the current value and throw away all animations
                // for SnapshotAndReplace and means nothing for Compose.
                //  But since we don't have any animations the current we don't
                // have to do anything for either of these cases.

                return;
            }
            else if (handoffBehavior == HandoffBehavior.SnapshotAndReplace)
            {
                // This block handles the case where the user has called
                // BeginAnimation with an animation that has a null begin time.
                // We handle this by taking a snapshot value and throwing
                // out the animation, which is the same as keeping it because
                // we know it will never start.
                //
                // If the handoffBehavior is Compose, we ignore the user's call
                // because it wouldn't mean anything unless they were planning
                // on changing the BeginTime of their animation later, which we
                // don't support.

                // If _hasStickySnapshotValue is set, unset it and remove our
                // event handler from the clock. The current snapshot value
                // will still be used.
                if (storage._hasStickySnapshotValue)
                {
                    Debug.Assert(storage._animationClocks != null && storage._animationClocks.Count > 0,
                        "If _hasStickySnapshotValue is set we should have at least one animation clock stored in the AnimationStorage.");

                    storage._hasStickySnapshotValue = false;
                    storage._animationClocks[0].CurrentStateInvalidated -= new EventHandler(storage.OnCurrentStateInvalidated);
                }
                // Otherwise take a new snapshot value.
                else
                {
                    storage._snapshotValue = d.GetValue(dp);
                }

                storage.ClearAnimations();
            }

            // If storage were null we would have returned already.
            storage.WritePostscript();
        }

        internal static AnimationStorage EnsureStorage(
            DependencyObject d,
            DependencyProperty dp)
        {
            FrugalMap animatedPropertyMap = AnimatedPropertyMapField.GetValue(d);
            object currentStorage = animatedPropertyMap[dp.GlobalIndex];

            if (currentStorage == DependencyProperty.UnsetValue)
            {
                return CreateStorage(d, dp);
            }
            else
            {
                return (AnimationStorage)currentStorage;
            }
        }

        /// <summary>
        /// GetCurrentPropertyValue
        /// </summary>
        /// <returns></returns>
        internal static object GetCurrentPropertyValue(
            AnimationStorage storage,
            DependencyObject d,
            DependencyProperty dp,
            PropertyMetadata metadata,
            object baseValue)
        {
            Debug.Assert(storage != null,
                "The 'storage' parameter cannot be passed into the GetCurrentPropertyValue method as null.");

            // If changes have been made to the snapshot value since the last tick
            // that value represents the current value of the property until the
            // next tick when the flag will be cleared. We will only take one
            // snapshot value between ticks.
            //
            // Since CurrentTimeInvaliated is raised before CurrentStateInvalidated
            // we need to check the state of the first clock as well to avoid
            // potential first frame issues. In this case _hasStickySnapshotValue
            // will be updated to false shortly.
            if (   storage._hasStickySnapshotValue
                && storage._animationClocks[0].CurrentState == ClockState.Stopped)
            {
                return storage._snapshotValue;
            }

            if (   storage._animationClocks == null
                && storage._propertyTriggerLayers == null)
            {
                Debug.Assert(storage._snapshotValue != DependencyProperty.UnsetValue);

                return storage._snapshotValue;
            }

            object currentPropertyValue = baseValue;

            if (currentPropertyValue == DependencyProperty.UnsetValue)
            {
                currentPropertyValue = metadata.GetDefaultValue(d, dp);
            }

            Debug.Assert(currentPropertyValue != DependencyProperty.UnsetValue);

            //
            // Process property trigger animations.
            //

            if (storage._propertyTriggerLayers != null)
            {
                int count = storage._propertyTriggerLayers.Count;

                Debug.Assert(count > 0);

                IList<AnimationLayer> layers = storage._propertyTriggerLayers.Values;

                for (int i = 0; i < count; i++)
                {
                    currentPropertyValue = layers[i].GetCurrentValue(currentPropertyValue);
                }
            }

            //
            // Process local animations
            //

            if (storage._animationClocks != null)
            {
                FrugalObjectList<AnimationClock> clocks = storage._animationClocks;
                int clocksCount = clocks.Count;
                bool hasActiveOrFillingClock = false;

                // default destination value will be the current property value
                // calculated by the previous layer.
                object defaultDestinationValue = currentPropertyValue;
                object currentLayerValue = currentPropertyValue;

                // if we have a snapshot value, then that will be the new
                // initial current property value.
                if (storage._snapshotValue != DependencyProperty.UnsetValue)
                {
                    currentLayerValue = storage._snapshotValue;
                }

                Debug.Assert(clocksCount > 0);
                Debug.Assert(defaultDestinationValue != DependencyProperty.UnsetValue);

                for (int i = 0; i < clocksCount; i++)
                {
                    if (clocks[i].CurrentState != ClockState.Stopped)
                    {
                        hasActiveOrFillingClock = true;

                        currentLayerValue = clocks[i].GetCurrentValue(currentLayerValue, defaultDestinationValue);

                        // An animation may not return DependencyProperty.UnsetValue as its
                        // current value.
                        if (currentLayerValue == DependencyProperty.UnsetValue)
                        {
                            throw new InvalidOperationException(SR.Get(
                                SRID.Animation_ReturnedUnsetValueInstance,
                                clocks[i].Timeline.GetType().FullName,
                                dp.Name,
                                d.GetType().FullName));
                        }
                    }
                }

                // The currentLayerValue only applies when there is at least one
                // active or filling clock.
                if (hasActiveOrFillingClock)
                {
                    currentPropertyValue = currentLayerValue;
                }
            }

            // We have a calculated currentPropertyValue, so return it if the type matches.
            if (DependencyProperty.IsValidType(currentPropertyValue, dp.PropertyType))
            {
                return currentPropertyValue;
            }
            else
            {
                // If the animation(s) applied to the property have calculated an
                // invalid value for the property then raise an exception.
                throw new InvalidOperationException(
                    SR.Get(
                        SRID.Animation_CalculatedValueIsInvalidForProperty,
                        dp.Name,
                        (currentPropertyValue == null ? "null" : currentPropertyValue.ToString())));
            }
        }

        [FriendAccessAllowed] // Built into Core, also used by Framework.
        internal static bool IsPropertyAnimatable(
            DependencyObject d,
            DependencyProperty dp)
        {
            if (dp.PropertyType != typeof(Visual3DCollection) && dp.ReadOnly)
            {
                return false;
            }

            UIPropertyMetadata uiMetadata = dp.GetMetadata(d.DependencyObjectType) as UIPropertyMetadata;

            if (   uiMetadata != null
                && uiMetadata.IsAnimationProhibited)
            {
                return false;
            }

            return true;
        }

        internal static bool IsAnimationValid(
            DependencyProperty dp,
            AnimationTimeline animation)
        {
            return dp.PropertyType.IsAssignableFrom(animation.TargetPropertyType)
                || (animation.TargetPropertyType == typeof(Object));
        }

        [FriendAccessAllowed] // Built into Core, also used by Framework.
        internal static bool IsAnimationClockValid(
            DependencyProperty dp,
            AnimationClock animation)
        {
            return IsAnimationValid(dp, (AnimationTimeline)animation.Timeline);
        }

        /// <summary>
        /// Returns the list of animated DependencyProperties for a
        /// given DependencyObject.
        /// </summary>
        /// <param name="d">
        /// The DependencyObject.
        /// </param>
        /// <returns>
        /// The list of animated DependencyProperties if any are animated;
        /// otherwise null.
        /// </returns>
        internal static FrugalMap GetAnimatedPropertiesMap(DependencyObject d)
        {
            return AnimatedPropertyMapField.GetValue(d);
        }

        /// <summary>
        /// Returns the AnimationStorage associated with a given DependencyProperty
        /// on a give DependencyObject.
        /// </summary>
        /// <param name="d">The DependencyObject.</param>
        /// <param name="dp">The DependencyProperty.</param>
        /// <returns>
        /// The AnimationStorage associated with the DependencyProperty if it
        /// has any; otherwise null.
        /// </returns>
        internal static AnimationStorage GetStorage(DependencyObject d, DependencyProperty dp)
        {
            Debug.Assert(   AnimatedPropertyMapField.GetValue(d)[dp.GlobalIndex] == DependencyProperty.UnsetValue
                         || AnimatedPropertyMapField.GetValue(d)[dp.GlobalIndex] is AnimationStorage);

            return AnimatedPropertyMapField.GetValue(d)[dp.GlobalIndex] as AnimationStorage;
        }

        private static AnimationStorage CreateStorage(
             DependencyObject d,
             DependencyProperty dp)
        {
            AnimationStorage newStorage;

            if (dp.GetMetadata(d.DependencyObjectType) is IndependentlyAnimatedPropertyMetadata)
            {
                newStorage = CreateIndependentAnimationStorageForType(dp.PropertyType);
            }
            else
            {
                newStorage = new AnimationStorage();
            }

            newStorage.Initialize(d, dp);

            return newStorage;
        }

        private static IndependentAnimationStorage CreateIndependentAnimationStorageForType(Type type)
        {
            if (type == typeof(Double))
            {
                return new DoubleIndependentAnimationStorage();
            }
            else if (type == typeof(Color))
            {
                return new ColorIndependentAnimationStorage();
            }
            else if (type == typeof(Matrix))
            {
                return new MatrixIndependentAnimationStorage();
            }
            else if (type == typeof(Point3D))
            {
                return new Point3DIndependentAnimationStorage();
            }
            else if (type == typeof(Point))
            {
                return new PointIndependentAnimationStorage();
            }
            else if (type == typeof(Quaternion))
            {
                return new QuaternionIndependentAnimationStorage();
            }
            else if (type == typeof(Rect))
            {
                return new RectIndependentAnimationStorage();
            }
            else if (type == typeof(Size))
            {
                return new SizeIndependentAnimationStorage();
            }
            else
            {
                Debug.Assert(type == typeof(Vector3D), "Application is trying to create independent animation storage for an unsupported type.");

                return new Vector3DIndependentAnimationStorage();
            }
        }

        #endregion

        #region Data

        protected WeakReference _dependencyObject;
        protected DependencyProperty _dependencyProperty;
        protected FrugalObjectList<AnimationClock> _animationClocks;
        private SortedList<Int64, AnimationLayer> _propertyTriggerLayers;

        private EventHandler _currentTimeInvalidatedHandler;
        private EventHandler _removeRequestedHandler;
        private object _snapshotValue = DependencyProperty.UnsetValue;
        private bool _hasStickySnapshotValue;
        private bool _hadValidationError;

        // This can be used by Animatables to store a local value if
        // they provide a WriteLocalValueOverride but need to spill
        // the local value if they are animated because they also use
        // the local value storage as a current value cache.
        internal object _baseValue = DependencyProperty.UnsetValue;

        #endregion
    }
}
