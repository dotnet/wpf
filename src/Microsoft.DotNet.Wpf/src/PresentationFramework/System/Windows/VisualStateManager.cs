// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MS.Internal;  // ObservableCollectionDefaultValueFactory
using System.Security;

namespace System.Windows
{
    /// <summary>
    ///     Manages visual states and their transitions on a control.
    /// </summary>
    public class VisualStateManager : DependencyObject
    {
        /// <summary>
        ///     Transitions a control's state.
        /// </summary>
        /// <param name="control">The control who's state is changing.</param>
        /// <param name="stateGroupsRoot">The element to get the VSG & customer VSM from.</param>
        /// <param name="stateName">The new state that the control is in.</param>
        /// <param name="useTransitions">Whether to use transition animations.</param>
        /// <returns>true if the state changed successfully, false otherwise.</returns>
        private static bool GoToStateCommon(FrameworkElement control, FrameworkElement stateGroupsRoot, string stateName, bool useTransitions)
        {
            if (stateName == null)
            {
                throw new ArgumentNullException("stateName");
            }
            
            if (stateGroupsRoot == null)
            {
                return false; // Ignore state changes if a stateGroupsRoot doesn't exist yet
            }

            IList<VisualStateGroup> groups = VisualStateManager.GetVisualStateGroupsInternal(stateGroupsRoot);
            if (groups == null)
            {
                return false;
            }

            VisualState state;
            VisualStateGroup group;
            VisualStateManager.TryGetState(groups, stateName, out group, out state);

            // Look for a custom VSM, and call it if it was found, regardless of whether the state was found or not.
            // This is because we don't know what the custom VSM will want to do. But for our default implementation,
            // we know that if we haven't found the state, we don't actually want to do anything.
            VisualStateManager customVsm = GetCustomVisualStateManager(stateGroupsRoot);
            if (customVsm != null)
            {
                return customVsm.GoToStateCore(control, stateGroupsRoot, stateName, group, state, useTransitions);
            }
            else if (state != null)
            {
                return GoToStateInternal(control, stateGroupsRoot, group, state, useTransitions);
            }

            return false;
        }

        /// <summary>
        ///     Transitions a control's state.
        /// </summary>
        /// <param name="control">The control who's state is changing.</param>
        /// <param name="stateName">The new state that the control is in.</param>
        /// <param name="useTransitions">Whether to use transition animations.</param>
        /// <returns>true if the state changed successfully, false otherwise.</returns>
        public static bool GoToState(FrameworkElement control, string stateName, bool useTransitions)
        {
            if (control == null)
            {
                throw new ArgumentNullException("control");
            }

            FrameworkElement stateGroupsRoot = control.StateGroupsRoot;

            return GoToStateCommon(control, stateGroupsRoot, stateName, useTransitions);
        }

        /// <summary>
        ///     Transitions a control's state.
        /// </summary>
        /// <param name="control">The control who's state is changing.</param>
        /// <param name="stateName">The new state that the control is in.</param>
        /// <param name="useTransitions">Whether to use transition animations.</param>
        /// <returns>true if the state changed successfully, false otherwise.</returns>
        public static bool GoToElementState(FrameworkElement stateGroupsRoot, string stateName, bool useTransitions)
        {
            if (stateGroupsRoot == null)
            {
                throw new ArgumentNullException("stateGroupsRoot");
            }

            return GoToStateCommon(null, stateGroupsRoot, stateName, useTransitions);
        }

        /// <summary>
        ///     Allows subclasses to override the GoToState logic.
        /// </summary>
        protected virtual bool GoToStateCore(FrameworkElement control, FrameworkElement stateGroupsRoot, string stateName, VisualStateGroup group, VisualState state, bool useTransitions)
        {
            return GoToStateInternal(control, stateGroupsRoot, group, state, useTransitions);
        }

        #region CustomVisualStateManager

        public static readonly DependencyProperty CustomVisualStateManagerProperty = 
            DependencyProperty.RegisterAttached(
            "CustomVisualStateManager",
            typeof(VisualStateManager),
            typeof(VisualStateManager),
            null);

        public static VisualStateManager GetCustomVisualStateManager(FrameworkElement obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return obj.GetValue(VisualStateManager.CustomVisualStateManagerProperty) as VisualStateManager;
        }

        public static void SetCustomVisualStateManager(FrameworkElement obj, VisualStateManager value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            obj.SetValue(VisualStateManager.CustomVisualStateManagerProperty, value);
        }

        #endregion

        #region VisualStateGroups

        private static readonly DependencyPropertyKey VisualStateGroupsPropertyKey = 
            DependencyProperty.RegisterAttachedReadOnly(
            "VisualStateGroups",
            typeof(IList),
            typeof(VisualStateManager),
            new FrameworkPropertyMetadata(new ObservableCollectionDefaultValueFactory<VisualStateGroup>()));

        /// <summary>
        ///     Read only VisualStateGroups property
        /// </summary>
        public static readonly DependencyProperty VisualStateGroupsProperty =
                VisualStateGroupsPropertyKey.DependencyProperty;

        internal static Collection<VisualStateGroup> GetVisualStateGroupsInternal(FrameworkElement obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            // We don't want to get the default value because it will create/return an empty colleciton.
            bool hasModifiers;
            BaseValueSourceInternal source = obj.GetValueSource(VisualStateGroupsProperty, null, out hasModifiers);
            if (source != BaseValueSourceInternal.Default)
            {
                return obj.GetValue(VisualStateManager.VisualStateGroupsProperty) as Collection<VisualStateGroup>;
            }
            
            return null;
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public static IList GetVisualStateGroups(FrameworkElement obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return obj.GetValue(VisualStateManager.VisualStateGroupsProperty) as IList;
        }

        #endregion

        #region State Change

        internal static bool TryGetState(IList<VisualStateGroup> groups, string stateName, out VisualStateGroup group, out VisualState state)
        {
            for (int groupIndex = 0; groupIndex < groups.Count; ++groupIndex)
            {
                VisualStateGroup g = groups[groupIndex];
                VisualState s = g.GetState(stateName);
                if (s != null)
                {
                    group = g;
                    state = s;
                    return true;
                }
            }

            group = null;
            state = null;
            return false;
        }

        private static bool GoToStateInternal(FrameworkElement control, FrameworkElement stateGroupsRoot, VisualStateGroup group, VisualState state, bool useTransitions)
        {
            if (stateGroupsRoot == null)
            {
                throw new ArgumentNullException("stateGroupsRoot");
            }

            if (state == null)
            {
                throw new ArgumentNullException("state");
            }

            if (group == null)
            {
                throw new InvalidOperationException();
            }

            VisualState lastState = group.CurrentState;
            if (lastState == state)
            {
                return true;
            }

            // Get the transition Storyboard. Even if there are no transitions specified, there might
            // be properties that we're rolling back to their default values.
            VisualTransition transition = useTransitions ? VisualStateManager.GetTransition(stateGroupsRoot, group, lastState, state) : null;

            // Generate dynamicTransition Storyboard
            Storyboard dynamicTransition = GenerateDynamicTransitionAnimations(stateGroupsRoot, group, state, transition);

            // If the transition is null, then we want to instantly snap. The dynamicTransition will
            // consist of everything that is being moved back to the default state.
            // If the transition.Duration and explicit storyboard duration is zero, then we want both the dynamic 
            // and state Storyboards to happen in the same tick, so we start them at the same time.
            if (transition == null || (transition.GeneratedDuration == DurationZero &&
                                            (transition.Storyboard == null || transition.Storyboard.Duration == DurationZero)))
            {
                // Start new state Storyboard and stop any previously running Storyboards
                if (transition != null && transition.Storyboard != null)
                {
                    group.StartNewThenStopOld(stateGroupsRoot, transition.Storyboard, state.Storyboard);
                }
                else
                {
                    group.StartNewThenStopOld(stateGroupsRoot, state.Storyboard);
                }

                // Fire both CurrentStateChanging and CurrentStateChanged events
                group.RaiseCurrentStateChanging(stateGroupsRoot, lastState, state, control);
                group.RaiseCurrentStateChanged(stateGroupsRoot, lastState, state, control);
            }
            else
            {
                // In this case, we have an interstitial storyboard of duration > 0 and/or
                // explicit storyboard of duration >0 , so we need 
                // to run them first, and then we'll run the state storyboard.
                // we have to wait for both storyboards to complete before
                // starting the steady state animations.
                transition.DynamicStoryboardCompleted = false;

                // Hook up generated Storyboard's Completed event handler
                dynamicTransition.Completed += delegate(object sender, EventArgs e)
                {
                    if (transition.Storyboard == null || transition.ExplicitStoryboardCompleted)
                    {
                        if (ShouldRunStateStoryboard(control, stateGroupsRoot, state, group))
                        {
                            group.StartNewThenStopOld(stateGroupsRoot, state.Storyboard);
                        }

                        group.RaiseCurrentStateChanged(stateGroupsRoot, lastState, state, control);
                    }

                    transition.DynamicStoryboardCompleted = true;
                };

                if (transition.Storyboard != null && transition.ExplicitStoryboardCompleted == true)
                {
                    EventHandler transitionCompleted = null;
                    transitionCompleted = new EventHandler(delegate(object sender, EventArgs e)
                    {
                        if (transition.DynamicStoryboardCompleted)
                        {
                            if (ShouldRunStateStoryboard(control, stateGroupsRoot, state, group))
                            {
                                group.StartNewThenStopOld(stateGroupsRoot, state.Storyboard);
                            }

                            group.RaiseCurrentStateChanged(stateGroupsRoot, lastState, state, control);
                        }

                        transition.Storyboard.Completed -= transitionCompleted;
                        transition.ExplicitStoryboardCompleted = true;
                    });

                    // hook up explicit storyboard's Completed event handler
                    transition.ExplicitStoryboardCompleted = false;
                    transition.Storyboard.Completed += transitionCompleted;
                }

                // Start transition and dynamicTransition Storyboards
                // Stop any previously running Storyboards
                group.StartNewThenStopOld(stateGroupsRoot, transition.Storyboard, dynamicTransition);

                group.RaiseCurrentStateChanging(stateGroupsRoot, lastState, state, control);
            }

            group.CurrentState = state;

            return true;
        }

        /// <summary>
        ///   If the stateGroupsRoot or control is removed from the tree, then the new
        ///   storyboards will not be able to resolve target names. Thus,
        ///   if the stateGroupsRoot or control is not in the tree, don't start the new
        ///   storyboards. Also if the group has already changed state, then
        ///   don't start the new storyboards.
        /// </summary> 
        private static bool ShouldRunStateStoryboard(FrameworkElement control, FrameworkElement stateGroupsRoot, VisualState state, VisualStateGroup group)
        {
            bool controlInTree = true;
            bool stateGroupsRootInTree = true;

            // We cannot simply check control.IsLoaded because the control may not be in the visual tree
            // even though IsLoaded is true.  Instead we will check that it can find a PresentationSource
            // which would tell us it's in the visual tree.
            if (control != null)
            {
                // If it's visible then it's in the visual tree, so we don't even have to look for a 
                // PresentationSource
                if (!control.IsVisible)
                {
                    controlInTree = (PresentationSource.CriticalFromVisual(control) != null);
                }
            }

            if (stateGroupsRoot != null)
            {
                if (!stateGroupsRoot.IsVisible)
                {
                    stateGroupsRootInTree = (PresentationSource.CriticalFromVisual(stateGroupsRoot) != null);
                }
            }

            return (controlInTree && stateGroupsRootInTree && (state == group.CurrentState));
        }

        protected void RaiseCurrentStateChanging(VisualStateGroup stateGroup, VisualState oldState, VisualState newState, FrameworkElement control, FrameworkElement stateGroupsRoot)
        {
            if (stateGroup == null)
            {
                throw new ArgumentNullException("stateGroup");
            }

            if (newState == null)
            {
                throw new ArgumentNullException("newState");
            }

            if (stateGroupsRoot == null)
            {
                return; // Ignore if a ControlTemplate hasn't been applied
            }

            stateGroup.RaiseCurrentStateChanging(stateGroupsRoot, oldState, newState, control);
        }

        protected void RaiseCurrentStateChanged(VisualStateGroup stateGroup, VisualState oldState, VisualState newState, FrameworkElement control, FrameworkElement stateGroupsRoot)
        {
            if (stateGroup == null)
            {
                throw new ArgumentNullException("stateGroup");
            }

            if (newState == null)
            {
                throw new ArgumentNullException("newState");
            }

            if (stateGroupsRoot == null)
            {
                return; // Ignore if a ControlTemplate hasn't been applied
            }

            stateGroup.RaiseCurrentStateChanged(stateGroupsRoot, oldState, newState, control);
        }

        #endregion

        #region Transitions

        private static Storyboard GenerateDynamicTransitionAnimations(FrameworkElement root, VisualStateGroup group, VisualState newState, VisualTransition transition)
        {
            IEasingFunction easingFunction = null;
            Storyboard dynamic = new Storyboard();
            
            if (transition != null)
            {
                if (transition.GeneratedDuration != null)
                {
                    dynamic.Duration = transition.GeneratedDuration;
                }

                easingFunction = transition.GeneratedEasingFunction;
            }
            else
            {
                dynamic.Duration = new Duration(TimeSpan.Zero);
            }

            Dictionary<TimelineDataToken, Timeline> currentAnimations = FlattenTimelines(group.CurrentStoryboards);
            Dictionary<TimelineDataToken, Timeline> transitionAnimations = FlattenTimelines(transition != null ? transition.Storyboard : null);
            Dictionary<TimelineDataToken, Timeline> newStateAnimations = FlattenTimelines(newState.Storyboard);

            // Remove any animations that the transition already animates.
            // There is no need to create an interstitial animation if one already exists.
            foreach (KeyValuePair<TimelineDataToken, Timeline> pair in transitionAnimations)
            {
                currentAnimations.Remove(pair.Key);
                newStateAnimations.Remove(pair.Key);
            }

            // Generate the "to" animations
            foreach (KeyValuePair<TimelineDataToken, Timeline> pair in newStateAnimations)
            {
                // The new "To" Animation -- the root is passed as a reference point for name
                // lookup.  
                Timeline toAnimation = GenerateToAnimation(root, pair.Value, easingFunction, true);

                // If the animation is of a type that we can't generate transition animations
                // for, GenerateToAnimation will return null, and we should just keep going.
                if (toAnimation != null)
                {
                    toAnimation.Duration = dynamic.Duration;
                    dynamic.Children.Add(toAnimation);
                }

                // Remove this from the list of current state animations we have to consider next
                currentAnimations.Remove(pair.Key);
            }

            // Generate the "from" animations
            foreach (KeyValuePair<TimelineDataToken, Timeline> pair in currentAnimations)
            {
                Timeline fromAnimation = GenerateFromAnimation(root, pair.Value, easingFunction);
                if (fromAnimation != null)
                {
                    fromAnimation.Duration = dynamic.Duration;
                    dynamic.Children.Add(fromAnimation);
                }
            }

            return dynamic;
        }

        private static Timeline GenerateFromAnimation(FrameworkElement root, Timeline timeline, IEasingFunction easingFunction)
        {
            Timeline result = null;
            
            if (timeline is ColorAnimation || timeline is ColorAnimationUsingKeyFrames)
            {
                result = new ColorAnimation() { EasingFunction = easingFunction };
            }
            else if (timeline is DoubleAnimation || timeline is DoubleAnimationUsingKeyFrames)
            {
                result = new DoubleAnimation() { EasingFunction = easingFunction };
            }
            else if (timeline is PointAnimation || timeline is PointAnimationUsingKeyFrames)
            {
                result = new PointAnimation() { EasingFunction = easingFunction };
            }

            if (result != null)
            {
                CopyStoryboardTargetProperties(root, timeline, result);
            }

            // All other animation types are ignored. We will not build transitions for them,
            // but they will end up being executed.
            return result;
        }

        private static Timeline GenerateToAnimation(FrameworkElement root, Timeline timeline, IEasingFunction easingFunction, bool isEntering)
        {
            Timeline result = null;

            Color? targetColor = GetTargetColor(timeline, isEntering);
            if (targetColor.HasValue)
            {
                ColorAnimation ca = new ColorAnimation() { To = targetColor, EasingFunction = easingFunction };
                result = ca;
            }

            if (result == null)
            {
                double? targetDouble = GetTargetDouble(timeline, isEntering);
                if (targetDouble.HasValue)
                {
                    DoubleAnimation da = new DoubleAnimation() { To = targetDouble, EasingFunction = easingFunction };
                    result = da;
                }
            }

            if (result == null)
            {
                Point? targetPoint = GetTargetPoint(timeline, isEntering);
                if (targetPoint.HasValue)
                {
                    PointAnimation pa = new PointAnimation() { To = targetPoint, EasingFunction = easingFunction };
                    result = pa;
                }
            }

            if (result != null)
            {
                CopyStoryboardTargetProperties(root, timeline, result);
            }

            return result;
        }

        private static void CopyStoryboardTargetProperties(FrameworkElement root, Timeline source, Timeline destination)
        {
            if (source != null || destination != null)
            {
                // Target takes priority over TargetName
                string targetName = Storyboard.GetTargetName(source);
                DependencyObject target = Storyboard.GetTarget(source);
                PropertyPath path = Storyboard.GetTargetProperty(source);
                
                if (target == null && !string.IsNullOrEmpty(targetName))
                {
                    target = root.FindName(targetName) as DependencyObject;
                }

                if (targetName != null)
                {
                    Storyboard.SetTargetName(destination, targetName);
                }

                if (target != null)
                {
                    Storyboard.SetTarget(destination, target);
                }

                if (path != null)
                {
                    Storyboard.SetTargetProperty(destination, path);
                }
            }
        }

        /// <summary>
        /// Get the most appropriate transition between two states.
        /// </summary>
        /// <param name="element">Element being transitioned.</param>
        /// <param name="group">Group being transitioned.</param>
        /// <param name="from">VisualState being transitioned from.</param>
        /// <param name="to">VisualState being transitioned to.</param>
        /// <returns>
        /// The most appropriate transition between the desired states.
        /// </returns>
        internal static VisualTransition GetTransition(FrameworkElement element, VisualStateGroup group, VisualState from, VisualState to)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            if (group == null)
            {
                throw new ArgumentNullException("group");
            }

            if (to == null)
            {
                throw new ArgumentNullException("to");
            }

            VisualTransition best = null;
            VisualTransition defaultTransition = null;
            int bestScore = -1;

            IList<VisualTransition> transitions = (IList<VisualTransition>)group.Transitions;
            if (transitions != null)
            {
                foreach (VisualTransition transition in transitions)
                {
                    if (defaultTransition == null && transition.IsDefault)
                    {
                        defaultTransition = transition;
                        continue;
                    }

                    int score = -1;

                    VisualState transitionFromState = group.GetState(transition.From);
                    VisualState transitionToState = group.GetState(transition.To);

                    if (from == transitionFromState)
                    {
                        score += 1;
                    }
                    else if (transitionFromState != null)
                    {
                        continue;
                    }

                    if (to == transitionToState)
                    {
                        score += 2;
                    }
                    else if (transitionToState != null)
                    {
                        continue;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = transition;
                    }
                }
            }

            return best ?? defaultTransition;
        }

        #endregion

        #region GetTarget Methods

        // These methods are used when generating a transition animation between states.
        // The timeline is the "to" state, and we need to find the To value for the
        // animation we're generating.
        private static Color? GetTargetColor(Timeline timeline, bool isEntering)
        {
            ColorAnimation ca = timeline as ColorAnimation;
            if (ca != null)
            {
                return ca.From.HasValue ? ca.From : ca.To;
            }

            ColorAnimationUsingKeyFrames cak = timeline as ColorAnimationUsingKeyFrames;
            if (cak != null)
            {
                if (cak.KeyFrames.Count == 0)
                {
                    return null;
                }

                ColorKeyFrame keyFrame = cak.KeyFrames[isEntering ? 0 : cak.KeyFrames.Count - 1];
                return keyFrame.Value;
            }

            return null;
        }

        private static double? GetTargetDouble(Timeline timeline, bool isEntering)
        {
            DoubleAnimation da = timeline as DoubleAnimation;
            if (da != null)
            {
                return da.From.HasValue ? da.From : da.To;
            }

            DoubleAnimationUsingKeyFrames dak = timeline as DoubleAnimationUsingKeyFrames;
            if (dak != null)
            {
                if (dak.KeyFrames.Count == 0)
                {
                    return null;
                }

                DoubleKeyFrame keyFrame = dak.KeyFrames[isEntering ? 0 : dak.KeyFrames.Count - 1];
                return keyFrame.Value;
            }

            return null;
        }

        private static Point? GetTargetPoint(Timeline timeline, bool isEntering)
        {
            PointAnimation pa = timeline as PointAnimation;
            if (pa != null)
            {
                return pa.From.HasValue ? pa.From : pa.To;
            }

            PointAnimationUsingKeyFrames pak = timeline as PointAnimationUsingKeyFrames;
            if (pak != null)
            {
                if (pak.KeyFrames.Count == 0)
                {
                    return null;
                }

                PointKeyFrame keyFrame = pak.KeyFrames[isEntering ? 0 : pak.KeyFrames.Count - 1];
                return keyFrame.Value;
            }

            return null;
        }

        #endregion

        #region FlattenTimelines

        // These methods exist to put extract all animations from a Storyboard, and store them in 
        // a Dictionary keyed on what element:property is being animated. Storyboards can contain
        // Storyboards, hence the "Flatten".
        private static Dictionary<TimelineDataToken, Timeline> FlattenTimelines(Storyboard storyboard)
        {
            Dictionary<TimelineDataToken, Timeline> result = new Dictionary<TimelineDataToken, Timeline>();

            FlattenTimelines(storyboard, result);

            return result;
        }

        private static Dictionary<TimelineDataToken, Timeline> FlattenTimelines(Collection<Storyboard> storyboards)
        {
            Dictionary<TimelineDataToken, Timeline> result = new Dictionary<TimelineDataToken, Timeline>();

            for (int index = 0; index < storyboards.Count; ++index)
            {
                FlattenTimelines(storyboards[index], result);
            }

            return result;
        }

        private static void FlattenTimelines(Storyboard storyboard, Dictionary<TimelineDataToken, Timeline> result)
        {
            if (storyboard == null)
            {
                return;
            }

            for (int index = 0; index < storyboard.Children.Count; ++index)
            {
                Timeline child = storyboard.Children[index];
                Storyboard childStoryboard = child as Storyboard;
                if (childStoryboard != null)
                {
                    FlattenTimelines(childStoryboard, result);
                }
                else
                {
                    result[new TimelineDataToken(child)] = child;
                }
            }
        }

        // specifies a token to uniquely identify a Timeline object
        private struct TimelineDataToken : IEquatable<TimelineDataToken>
        {
            public TimelineDataToken(Timeline timeline)
            {
                _target = Storyboard.GetTarget(timeline);
                _targetName = Storyboard.GetTargetName(timeline);
                _targetProperty = Storyboard.GetTargetProperty(timeline);
            }

            public bool Equals(TimelineDataToken other)
            {
                bool targetsEqual = false;
                if (_targetName != null)
                {
                    targetsEqual = other._targetName == _targetName;
                }
                else if (_target != null)
                {
                    targetsEqual = other._target == _target;
                }
                else
                {
                    targetsEqual = (other._target == null && other._targetName == null);
                }

                if (targetsEqual && 
                    (other._targetProperty.Path == _targetProperty.Path) &&
                    (other._targetProperty.PathParameters.Count == _targetProperty.PathParameters.Count))
                {
                    bool paramsEqual = true;

                    for (int i = 0, count = _targetProperty.PathParameters.Count; i < count; i++)
                    {
                        if (other._targetProperty.PathParameters[i] != _targetProperty.PathParameters[i])
                        {
                            paramsEqual = false;
                            break;
                        }
                    }

                    return paramsEqual;
                }

                return false;
            }

            public override int GetHashCode()
            {
                //
                // The below code has some limitations.  We don't handle canonicalizing property paths, so
                // having two paths that target the same object/property can easily get different hash codes.
                //
                // For example the Opacity can be specified either from a string "Opacity" or via the string "(0)"
                // and a parameter Visual.OpacityPropety.  These wont match as far as VSM is concerned.
                //
                int targetHash = _target != null ? _target.GetHashCode() : 0;
                int targetNameHash = _targetName != null ? _targetName.GetHashCode() : 0;
                int targetPropertyHash = (_targetProperty != null && _targetProperty.Path != null) ? _targetProperty.Path.GetHashCode() : 0;

                return ((_targetName != null) ? targetNameHash : targetHash) ^ targetPropertyHash;
            }

            private DependencyObject _target;
            private string _targetName;
            private PropertyPath _targetProperty;
        }
        
        #endregion

        #region Data

        private static readonly Duration DurationZero = new Duration(TimeSpan.Zero);

        #endregion
    }
}
