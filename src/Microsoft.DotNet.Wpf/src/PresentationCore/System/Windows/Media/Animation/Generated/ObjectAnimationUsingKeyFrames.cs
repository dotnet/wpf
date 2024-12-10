// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file was generated, please do not edit it directly.
// Please see MilCodeGen.html for more information.

using System.Collections;
using System.ComponentModel;
using System.Windows.Markup;
using MS.Internal.PresentationCore;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// This class is used to animate a Object property value along a set
    /// of key frames.
    /// </summary>
    [ContentProperty("KeyFrames")]
    public class ObjectAnimationUsingKeyFrames : ObjectAnimationBase, IKeyFrameAnimation, IAddChild
    {
        #region Data

        private ObjectKeyFrameCollection _keyFrames;
        private ResolvedKeyFrameEntry[] _sortedResolvedKeyFrames;
        private bool _areKeyTimesValid;

        #endregion

        #region Constructors

        /// <Summary>
        /// Creates a new KeyFrameObjectAnimation.
        /// </Summary>
        public ObjectAnimationUsingKeyFrames()
            : base()
        {
            _areKeyTimesValid = true;
        }

        #endregion

        #region Freezable

        /// <summary>
        /// Creates a copy of this KeyFrameObjectAnimation.
        /// </summary>
        /// <returns>The copy</returns>
        public new ObjectAnimationUsingKeyFrames Clone()
        {
            return (ObjectAnimationUsingKeyFrames)base.Clone();
        }


        /// <summary>
        /// Returns a version of this class with all its base property values
        /// set to the current animated values and removes the animations.
        /// </summary>
        /// <returns>
        /// Since this class isn't animated, this method will always just return
        /// this instance of the class.
        /// </returns>
        public new ObjectAnimationUsingKeyFrames CloneCurrentValue()
        {
            return (ObjectAnimationUsingKeyFrames)base.CloneCurrentValue();
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.FreezeCore">Freezable.FreezeCore</see>.
        /// </summary>
        protected override bool FreezeCore(bool isChecking)
        {
            bool canFreeze = base.FreezeCore(isChecking);

            canFreeze &= Freezable.Freeze(_keyFrames, isChecking);

            if (canFreeze & !_areKeyTimesValid)
            {
                ResolveKeyTimes();
            }

            return canFreeze;
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.OnChanged">Freezable.OnChanged</see>.
        /// </summary>
        protected override void OnChanged()
        {
            _areKeyTimesValid = false;

            base.OnChanged();
        }


        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new ObjectAnimationUsingKeyFrames();
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCore(System.Windows.Freezable)">Freezable.CloneCore</see>.
        /// </summary>
        protected override void CloneCore(Freezable sourceFreezable)
        {
            ObjectAnimationUsingKeyFrames sourceAnimation = (ObjectAnimationUsingKeyFrames) sourceFreezable;
            base.CloneCore(sourceFreezable);

            CopyCommon(sourceAnimation, /* isCurrentValueClone = */ false);
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CloneCurrentValueCore(Freezable)">Freezable.CloneCurrentValueCore</see>.
        /// </summary>
        protected override void CloneCurrentValueCore(Freezable sourceFreezable)
        {
            ObjectAnimationUsingKeyFrames sourceAnimation = (ObjectAnimationUsingKeyFrames) sourceFreezable;
            base.CloneCurrentValueCore(sourceFreezable);

            CopyCommon(sourceAnimation, /* isCurrentValueClone = */ true);
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetAsFrozenCore(Freezable)">Freezable.GetAsFrozenCore</see>.
        /// </summary>
        protected override void GetAsFrozenCore(Freezable source)
        {
            ObjectAnimationUsingKeyFrames sourceAnimation = (ObjectAnimationUsingKeyFrames) source;
            base.GetAsFrozenCore(source);

            CopyCommon(sourceAnimation, /* isCurrentValueClone = */ false);
        }

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.GetCurrentValueAsFrozenCore(Freezable)">Freezable.GetCurrentValueAsFrozenCore</see>.
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable source)
        {
            ObjectAnimationUsingKeyFrames sourceAnimation = (ObjectAnimationUsingKeyFrames) source;
            base.GetCurrentValueAsFrozenCore(source);

            CopyCommon(sourceAnimation, /* isCurrentValueClone = */ true);
        }

        /// <summary>
        /// Helper used by the four Freezable clone methods to copy the resolved key times and 
        /// key frames. The Get*AsFrozenCore methods are implemented the same as the Clone*Core
        /// methods; Get*AsFrozen at the top level will recursively Freeze so it's not done here.
        /// </summary>
        /// <param name="sourceAnimation"></param>
        /// <param name="isCurrentValueClone"></param>
        private void CopyCommon(ObjectAnimationUsingKeyFrames sourceAnimation, bool isCurrentValueClone)
        {    
            _areKeyTimesValid = sourceAnimation._areKeyTimesValid;

            if (   _areKeyTimesValid 
                && sourceAnimation._sortedResolvedKeyFrames != null)
            {
                // _sortedResolvedKeyFrames is an array of ResolvedKeyFrameEntry so the notion of CurrentValueClone doesn't apply
                _sortedResolvedKeyFrames = (ResolvedKeyFrameEntry[])sourceAnimation._sortedResolvedKeyFrames.Clone(); 
            }

            if (sourceAnimation._keyFrames != null)
            {
                if (isCurrentValueClone)
                {
                    _keyFrames = (ObjectKeyFrameCollection)sourceAnimation._keyFrames.CloneCurrentValue();
                }
                else
                {
                    _keyFrames = (ObjectKeyFrameCollection)sourceAnimation._keyFrames.Clone();
                }

                OnFreezablePropertyChanged(null, _keyFrames);
            }
        }

        #endregion  // Freezable

        #region IAddChild interface

        /// <summary>
        /// Adds a child object to this KeyFrameAnimation.
        /// </summary>
        /// <param name="child">
        /// The child object to add.
        /// </param>
        /// <remarks>
        /// A KeyFrameAnimation only accepts a KeyFrame of the proper type as
        /// a child.
        /// </remarks>
        void IAddChild.AddChild(object child)
        {
            WritePreamble();

            ArgumentNullException.ThrowIfNull(child);

            AddChild(child);

            WritePostscript();
        }

        /// <summary>
        /// Implemented to allow KeyFrames to be direct children
        /// of KeyFrameAnimations in markup.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected virtual void AddChild(object child)
        {
            ObjectKeyFrame keyFrame = child as ObjectKeyFrame;

            if (keyFrame != null)
            {
                KeyFrames.Add(keyFrame);
            }
            else
            {        
                throw new ArgumentException(SR.Animation_ChildMustBeKeyFrame, "child");
            }
        }

        /// <summary>
        /// Adds a text string as a child of this KeyFrameAnimation.
        /// </summary>
        /// <param name="childText">
        /// The text to add.
        /// </param>
        /// <remarks>
        /// A KeyFrameAnimation does not accept text as a child, so this method will
        /// raise an InvalididOperationException unless a derived class has
        /// overridden the behavior to add text.
        /// </remarks>
        /// <exception cref="ArgumentNullException">The childText parameter is
        /// null.</exception>
        void IAddChild.AddText(string childText)
        {
            ArgumentNullException.ThrowIfNull(childText);

            AddText(childText);
        }

        /// <summary>
        /// This method performs the core functionality of the AddText()
        /// method on the IAddChild interface.  For a KeyFrameAnimation this means
        /// throwing and InvalidOperationException because it doesn't
        /// support adding text.
        /// </summary>
        /// <remarks>
        /// This method is the only core implementation.  It does not call
        /// WritePreamble() or WritePostscript().  It also doesn't throw an
        /// ArgumentNullException if the childText parameter is null.  These tasks
        /// are performed by the interface implementation.  Therefore, it's OK
        /// for a derived class to override this method and call the base
        /// class implementation only if they determine that it's the right
        /// course of action.  The derived class can rely on KeyFrameAnimation's
        /// implementation of IAddChild.AddChild or implement their own
        /// following the Freezable pattern since that would be a public
        /// method.
        /// </remarks>
        /// <param name="childText">A string representing the child text that
        /// should be added.  If this is a KeyFrameAnimation an exception will be
        /// thrown.</param>
        /// <exception cref="InvalidOperationException">Timelines have no way
        /// of adding text.</exception>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected virtual void AddText(string childText)
        {
            throw new InvalidOperationException(SR.Animation_NoTextChildren);
        }

        #endregion

        #region ObjectAnimationBase

        /// <summary>
        /// Calculates the value this animation believes should be the current value for the property.
        /// </summary>
        /// <param name="defaultOriginValue">
        /// This value is the suggested origin value provided to the animation
        /// to be used if the animation does not have its own concept of a
        /// start value. If this animation is the first in a composition chain
        /// this value will be the snapshot value if one is available or the
        /// base property value if it is not; otherise this value will be the 
        /// value returned by the previous animation in the chain with an 
        /// animationClock that is not Stopped.
        /// </param>
        /// <param name="defaultDestinationValue">
        /// This value is the suggested destination value provided to the animation
        /// to be used if the animation does not have its own concept of an
        /// end value. This value will be the base value if the animation is
        /// in the first composition layer of animations on a property; 
        /// otherwise this value will be the output value from the previous 
        /// composition layer of animations for the property.
        /// </param>
        /// <param name="animationClock">
        /// This is the animationClock which can generate the CurrentTime or
        /// CurrentProgress value to be used by the animation to generate its
        /// output value.
        /// </param>
        /// <returns>
        /// The value this animation believes should be the current value for the property.
        /// </returns>
        protected sealed override Object GetCurrentValueCore(
            Object defaultOriginValue,
            Object defaultDestinationValue, 
            AnimationClock animationClock)
        {
            Debug.Assert(animationClock.CurrentState != ClockState.Stopped);

            if (_keyFrames == null)
            {
                return defaultDestinationValue;
            }

            // We resolved our KeyTimes when we froze, but also got notified
            // of the frozen state and therefore invalidated ourselves.
            if (!_areKeyTimesValid)
            {
                ResolveKeyTimes();
            }

            if (_sortedResolvedKeyFrames == null)
            {
                return defaultDestinationValue;
            }

            TimeSpan    currentTime         = animationClock.CurrentTime.Value;
            Int32       keyFrameCount       = _sortedResolvedKeyFrames.Length;
            Int32       maxKeyFrameIndex    = keyFrameCount - 1;

            Object currentIterationValue;

            Debug.Assert(maxKeyFrameIndex >= 0, "maxKeyFrameIndex is less than zero which means we don't actually have any key frames.");

            Int32 currentResolvedKeyFrameIndex = 0;

            // Skip all the key frames with key times lower than the current time.
            // currentResolvedKeyFrameIndex will be greater than maxKeyFrameIndex 
            // if we are past the last key frame.
            while (   currentResolvedKeyFrameIndex < keyFrameCount
                   && currentTime  > _sortedResolvedKeyFrames[currentResolvedKeyFrameIndex]._resolvedKeyTime)
            {
                currentResolvedKeyFrameIndex++;
            }

            // If there are multiple key frames at the same key time, be sure to go to the last one.
            while (   currentResolvedKeyFrameIndex < maxKeyFrameIndex
                   && currentTime == _sortedResolvedKeyFrames[currentResolvedKeyFrameIndex + 1]._resolvedKeyTime)
            {
                currentResolvedKeyFrameIndex++;
            }

            if (currentResolvedKeyFrameIndex == keyFrameCount)
            {
                // Past the last key frame.
                currentIterationValue = GetResolvedKeyFrameValue(maxKeyFrameIndex);
            }
            else if (currentTime == _sortedResolvedKeyFrames[currentResolvedKeyFrameIndex]._resolvedKeyTime)
            {
                // Exactly on a key frame.
                currentIterationValue = GetResolvedKeyFrameValue(currentResolvedKeyFrameIndex);
            }
            else
            {
                // Between two key frames.
                Double currentSegmentProgress = 0.0;
                Object fromValue;

                if (currentResolvedKeyFrameIndex == 0)
                {
                    // The current key frame is the first key frame so we have
                    // some special rules for determining the fromValue and an
                    // optimized method of calculating the currentSegmentProgress.                                        

                    fromValue = defaultOriginValue;

                    // Current segment time divided by the segment duration.
                    // Note: the reason this works is that we know that we're in
                    // the first segment, so we can assume:
                    //
                    // currentTime.TotalMilliseconds                                  = current segment time
                    // _sortedResolvedKeyFrames[0]._resolvedKeyTime.TotalMilliseconds = current segment duration

                    currentSegmentProgress = currentTime.TotalMilliseconds 
                                             / _sortedResolvedKeyFrames[0]._resolvedKeyTime.TotalMilliseconds;
                }
                else
                {
                    Int32    previousResolvedKeyFrameIndex = currentResolvedKeyFrameIndex - 1;
                    TimeSpan previousResolvedKeyTime = _sortedResolvedKeyFrames[previousResolvedKeyFrameIndex]._resolvedKeyTime;

                    fromValue = GetResolvedKeyFrameValue(previousResolvedKeyFrameIndex);

                    TimeSpan segmentCurrentTime = currentTime - previousResolvedKeyTime;
                    TimeSpan segmentDuration    = _sortedResolvedKeyFrames[currentResolvedKeyFrameIndex]._resolvedKeyTime - previousResolvedKeyTime;

                    currentSegmentProgress = segmentCurrentTime.TotalMilliseconds 
                                            / segmentDuration.TotalMilliseconds;
                }

                currentIterationValue = GetResolvedKeyFrame(currentResolvedKeyFrameIndex).InterpolateValue(fromValue, currentSegmentProgress);
            }



            return currentIterationValue;
        }

        /// <summary>
        /// Provide a custom natural Duration when the Duration property is set to Automatic.
        /// </summary>
        /// <param name="clock">
        /// The Clock whose natural duration is desired.
        /// </param>
        /// <returns>
        /// If the last KeyFrame of this animation is a KeyTime, then this will
        /// be used as the NaturalDuration; otherwise it will be one second.
        /// </returns>
        protected override sealed Duration GetNaturalDurationCore(Clock clock)
        {
            return new Duration(LargestTimeSpanKeyTime);
        }

        #endregion

        #region IKeyFrameAnimation

        /// <summary>
        /// Returns the ObjectKeyFrameCollection used by this KeyFrameObjectAnimation.
        /// </summary>
        IList IKeyFrameAnimation.KeyFrames
        {
            get
            {
                return KeyFrames;
            }
            set
            {
                KeyFrames = (ObjectKeyFrameCollection)value;
            }
        }

        /// <summary>
        /// Returns the ObjectKeyFrameCollection used by this KeyFrameObjectAnimation.
        /// </summary>
        public ObjectKeyFrameCollection KeyFrames
        {
            get
            {
                ReadPreamble();

                // The reason we don't just set _keyFrames to the empty collection
                // in the first place is that null tells us that the user has not
                // asked for the collection yet. The first time they ask for the
                // collection and we're unfrozen, policy dictates that we give
                // them a new unfrozen collection. All subsequent times they will
                // get whatever collection is present, whether frozen or unfrozen.

                if (_keyFrames == null)
                {
                    if (this.IsFrozen)
                    {
                        _keyFrames = ObjectKeyFrameCollection.Empty;
                    }
                    else
                    {
                        WritePreamble();

                        _keyFrames = new ObjectKeyFrameCollection();

                        OnFreezablePropertyChanged(null, _keyFrames);

                        WritePostscript();
                    }
                }

                return _keyFrames;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);

                WritePreamble();

                if (value != _keyFrames)
                {
                    OnFreezablePropertyChanged(_keyFrames, value);
                    _keyFrames = value;

                    WritePostscript();
                }
            }
        }

        /// <summary>
        /// Returns true if we should serialize the KeyFrames, property for this Animation.
        /// </summary>
        /// <returns>True if we should serialize the KeyFrames property for this Animation; otherwise false.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeKeyFrames()
        {
            ReadPreamble();

            return _keyFrames != null 
                && _keyFrames.Count > 0;
        }

        #endregion



        #region Private Methods

        private struct KeyTimeBlock
        {
            public int BeginIndex;
            public int EndIndex;
        }

        private Object GetResolvedKeyFrameValue(Int32 resolvedKeyFrameIndex)
        {
            Debug.Assert(_areKeyTimesValid, "The key frames must be resolved and sorted before calling GetResolvedKeyFrameValue");

            return GetResolvedKeyFrame(resolvedKeyFrameIndex).Value;
        }

        private ObjectKeyFrame GetResolvedKeyFrame(Int32 resolvedKeyFrameIndex)
        {
            Debug.Assert(_areKeyTimesValid, "The key frames must be resolved and sorted before calling GetResolvedKeyFrame");

            return _keyFrames[_sortedResolvedKeyFrames[resolvedKeyFrameIndex]._originalKeyFrameIndex];
        }

        /// <summary>
        /// Returns the largest time span specified key time from all of the key frames.
        /// If there are not time span key times a time span of one second is returned
        /// to match the default natural duration of the From/To/By animations.
        /// </summary>
        private TimeSpan LargestTimeSpanKeyTime
        {
            get
            {
                bool hasTimeSpanKeyTime = false;
                TimeSpan largestTimeSpanKeyTime = TimeSpan.Zero;

                if (_keyFrames != null)
                {
                    Int32 keyFrameCount = _keyFrames.Count;

                    for (int index = 0; index < keyFrameCount; index++)
                    {
                        KeyTime keyTime = _keyFrames[index].KeyTime;

                        if (keyTime.Type == KeyTimeType.TimeSpan)
                        {
                            hasTimeSpanKeyTime = true;

                            if (keyTime.TimeSpan > largestTimeSpanKeyTime)
                            {
                                largestTimeSpanKeyTime = keyTime.TimeSpan;
                            }
                        }
                    }
                }

                if (hasTimeSpanKeyTime)
                {
                    return largestTimeSpanKeyTime;
                }
                else
                {
                    return TimeSpan.FromSeconds(1.0);
                }
            }
        }

        private void ResolveKeyTimes()
        {
            Debug.Assert(!_areKeyTimesValid, "KeyFrameObjectAnimaton.ResolveKeyTimes() shouldn't be called if the key times are already valid.");

            int keyFrameCount = 0;

            if (_keyFrames != null)
            {
                keyFrameCount = _keyFrames.Count;
            }

            if (keyFrameCount == 0)
            {
                _sortedResolvedKeyFrames = null;
                _areKeyTimesValid = true;
                return;
            }

            _sortedResolvedKeyFrames = new ResolvedKeyFrameEntry[keyFrameCount];

            int index = 0;

            // Initialize the _originalKeyFrameIndex.
            for ( ; index < keyFrameCount; index++)
            {
                _sortedResolvedKeyFrames[index]._originalKeyFrameIndex = index;
            }

            // calculationDuration represents the time span we will use to resolve
            // percent key times. This is defined as the value in the following
            // precedence order:
            //   1. The animation's duration, but only if it is a time span, not auto or forever.
            //   2. The largest time span specified key time of all the key frames.
            //   3. 1 second, to match the From/To/By animations.

            TimeSpan calculationDuration = TimeSpan.Zero;

            Duration duration = Duration;

            if (duration.HasTimeSpan)
            {
                calculationDuration = duration.TimeSpan;
            }
            else
            {
                calculationDuration = LargestTimeSpanKeyTime;
            }

            int maxKeyFrameIndex = keyFrameCount - 1;
            List<KeyTimeBlock> unspecifiedBlocks = new List<KeyTimeBlock>();
            bool hasPacedKeyTimes = false;

            //
            // Pass 1: Resolve Percent and Time key times.
            //

            index = 0;
            while (index < keyFrameCount)
            {
                KeyTime keyTime = _keyFrames[index].KeyTime;

                switch (keyTime.Type)
                {
                    case KeyTimeType.Percent:

                        _sortedResolvedKeyFrames[index]._resolvedKeyTime = TimeSpan.FromMilliseconds(
                            keyTime.Percent * calculationDuration.TotalMilliseconds);
                        index++;
                        break;

                    case KeyTimeType.TimeSpan:

                        _sortedResolvedKeyFrames[index]._resolvedKeyTime = keyTime.TimeSpan;

                        index++;
                        break;

                    case KeyTimeType.Paced:
                    case KeyTimeType.Uniform:

                        if (index == maxKeyFrameIndex)
                        {
                            // If the last key frame doesn't have a specific time
                            // associated with it its resolved key time will be
                            // set to the calculationDuration, which is the
                            // defined in the comments above where it is set. 
                            // Reason: We only want extra time at the end of the
                            // key frames if the user specifically states that
                            // the last key frame ends before the animation ends.

                            _sortedResolvedKeyFrames[index]._resolvedKeyTime = calculationDuration;
                            index++;
                        }
                        else if (   index == 0
                                 && keyTime.Type == KeyTimeType.Paced)
                        {
                            // Note: It's important that this block come after
                            // the previous if block because of rule precendence.

                            // If the first key frame in a multi-frame key frame
                            // collection is paced, we set its resolved key time
                            // to 0.0 for performance reasons.  If we didn't, the
                            // resolved key time list would be dependent on the
                            // base value which can change every animation frame
                            // in many cases.

                            _sortedResolvedKeyFrames[index]._resolvedKeyTime = TimeSpan.Zero;
                            index++;
                        }
                        else
                        {
                            if (keyTime.Type == KeyTimeType.Paced)
                            {
                                hasPacedKeyTimes = true;
                            }

                            KeyTimeBlock block = new KeyTimeBlock();
                            block.BeginIndex = index;

                            // NOTE: We don't want to go all the way up to the
                            // last frame because if it is Uniform or Paced its
                            // resolved key time will be set to the calculation 
                            // duration using the logic above.
                            //
                            // This is why the logic is:
                            //    ((++index) < maxKeyFrameIndex)
                            // instead of:
                            //    ((++index) < keyFrameCount)

                            while ((++index) < maxKeyFrameIndex)
                            {
                                KeyTimeType type = _keyFrames[index].KeyTime.Type;

                                if (   type == KeyTimeType.Percent
                                    || type == KeyTimeType.TimeSpan)
                                {
                                    break;
                                }   
                                else if (type == KeyTimeType.Paced)
                                {
                                    hasPacedKeyTimes = true;
                                }                                
                            }

                            Debug.Assert(index < keyFrameCount, 
                                "The end index for a block of unspecified key frames is out of bounds.");

                            block.EndIndex = index;
                            unspecifiedBlocks.Add(block);
                        }

                        break;
                }
            }

            //
            // Pass 2: Resolve Uniform key times.
            //

            for (int j = 0; j < unspecifiedBlocks.Count; j++)
            {
                KeyTimeBlock block = unspecifiedBlocks[j];

                TimeSpan blockBeginTime = TimeSpan.Zero;

                if (block.BeginIndex > 0)
                {
                    blockBeginTime = _sortedResolvedKeyFrames[block.BeginIndex - 1]._resolvedKeyTime;
                }

                // The number of segments is equal to the number of key
                // frames we're working on plus 1.  Think about the case
                // where we're working on a single key frame.  There's a
                // segment before it and a segment after it.
                //
                //  Time known         Uniform           Time known
                //  ^                  ^                 ^
                //  |                  |                 |
                //  |   (segment 1)    |   (segment 2)   |

                Int64 segmentCount = (block.EndIndex - block.BeginIndex) + 1;
                TimeSpan uniformTimeStep = TimeSpan.FromTicks((_sortedResolvedKeyFrames[block.EndIndex]._resolvedKeyTime - blockBeginTime).Ticks / segmentCount);

                index = block.BeginIndex;
                TimeSpan resolvedTime = blockBeginTime + uniformTimeStep;

                while (index < block.EndIndex)
                {
                    _sortedResolvedKeyFrames[index]._resolvedKeyTime = resolvedTime;

                    resolvedTime += uniformTimeStep;
                    index++;
                }
            }

            //
            // Pass 3: Resolve Paced key times.
            //

            if (hasPacedKeyTimes)
            {
                ResolvePacedKeyTimes();
            }

            //
            // Sort resolved key frame entries.
            //

            Array.Sort(_sortedResolvedKeyFrames);

            _areKeyTimesValid = true;
            return;
        }

        /// <summary>
        /// This should only be called from ResolveKeyTimes and only at the
        /// appropriate time.
        /// </summary>
        private void ResolvePacedKeyTimes()
        {
            Debug.Assert(_keyFrames != null && _keyFrames.Count > 2,
                "Caller must guard against calling this method when there are insufficient keyframes.");

            // If the first key frame is paced its key time has already
            // been resolved, so we start at index 1.

            int index = 1;
            int maxKeyFrameIndex = _sortedResolvedKeyFrames.Length - 1;

            do
            {
                if (_keyFrames[index].KeyTime.Type == KeyTimeType.Paced)
                {
                    //
                    // We've found a paced key frame so this is the
                    // beginning of a paced block.
                    //

                    // The first paced key frame in this block.
                    int firstPacedBlockKeyFrameIndex = index;

                    // List of segment lengths for this paced block.
                    List<Double> segmentLengths = new List<Double>();

                    // The resolved key time for the key frame before this
                    // block which we'll use as our starting point.
                    TimeSpan prePacedBlockKeyTime = _sortedResolvedKeyFrames[index - 1]._resolvedKeyTime;

                    // The total of the segment lengths of the paced key
                    // frames in this block.
                    Double totalLength = 0.0;

                    // The key value of the previous key frame which will be
                    // used to determine the segment length of this key frame.
                    Object prevKeyValue = _keyFrames[index - 1].Value;

                    do
                    {
                        Object currentKeyValue = _keyFrames[index].Value;

                        // Determine the segment length for this key frame and
                        // add to the total length.
                        totalLength += AnimatedTypeHelpers.GetSegmentLengthObject(prevKeyValue, currentKeyValue);

                        // Temporarily store the distance into the total length 
                        // that this key frame represents in the resolved
                        // key times array to be converted to a resolved key
                        // time outside of this loop.
                        segmentLengths.Add(totalLength);

                        // Prepare for the next iteration.
                        prevKeyValue = currentKeyValue;
                        index++;
                    }
                    while (   index < maxKeyFrameIndex
                            && _keyFrames[index].KeyTime.Type == KeyTimeType.Paced);

                    // index is currently set to the index of the key frame
                    // after the last paced key frame.  This will always
                    // be a valid index because we limit ourselves with 
                    // maxKeyFrameIndex.

                    // We need to add the distance between the last paced key
                    // frame and the next key frame to get the total distance
                    // inside the key frame block.
                    totalLength += AnimatedTypeHelpers.GetSegmentLengthObject(prevKeyValue, _keyFrames[index].Value);

                    // Calculate the time available in the resolved key time space.
                    TimeSpan pacedBlockDuration = _sortedResolvedKeyFrames[index]._resolvedKeyTime - prePacedBlockKeyTime;

                    // Convert lengths in segmentLengths list to resolved
                    // key times for the paced key frames in this block.
                    for (int i = 0, currentKeyFrameIndex = firstPacedBlockKeyFrameIndex; i < segmentLengths.Count; i++, currentKeyFrameIndex++)
                    {
                        // The resolved key time for each key frame is:
                        // 
                        // The key time of the key frame before this paced block
                        // + ((the percentage of the way through the total length)
                        //    * the resolved key time space available for the block)
                        _sortedResolvedKeyFrames[currentKeyFrameIndex]._resolvedKeyTime = prePacedBlockKeyTime + TimeSpan.FromMilliseconds(
                            (segmentLengths[i] / totalLength) * pacedBlockDuration.TotalMilliseconds);
                    }
                }
                else
                {
                    index++;
                }
            } 
            while (index < maxKeyFrameIndex);
        }

        #endregion
    }
}
