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
using System.Windows.Markup;
using System.Windows.Media.Animation;

namespace System.Windows
{
    /// <summary>
    ///     A group of mutually exclusive visual states.
    /// </summary>
    [ContentProperty("States")] 
    [RuntimeNameProperty("Name")]
    public class VisualStateGroup : DependencyObject 
    {
        /// <summary>
        ///     The Name of the VisualStateGroup.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        ///     VisualStates in the group.
        /// </summary>
        public IList States
        {
            get
            {
                if (_states == null)
                {
                    _states = new FreezableCollection<VisualState>();
                }

                return _states;
            }
        }

        /// <summary>
        ///     Transitions between VisualStates in the group.
        /// </summary>
        public IList Transitions
        {
            get
            {
                if (_transitions == null)
                {
                    _transitions = new FreezableCollection<VisualTransition>();
                }

                return _transitions;
            }
        }

        /// <summary>
        ///     VisualState that is currently applied.
        /// </summary>
        public VisualState CurrentState 
        { 
            get; 
            internal set; 
        }

        internal VisualState GetState(string stateName)
        {
            for (int stateIndex = 0; stateIndex < States.Count; ++stateIndex)
            {
                VisualState state = (VisualState)States[stateIndex];
                if (state.Name == stateName)
                {
                    return state;
                }
            }

            return null;
        }

        internal Collection<Storyboard> CurrentStoryboards
        {
            get
            {
                if (_currentStoryboards == null)
                {
                    _currentStoryboards = new Collection<Storyboard>();
                }

                return _currentStoryboards;
            }
        }

        internal void StartNewThenStopOld(FrameworkElement element, params Storyboard[] newStoryboards)
        {
            // Remove the old Storyboards. Remove is delayed until the next TimeManager tick, so the
            // handoff to the new storyboard is unaffected.
            for (int index = 0; index < CurrentStoryboards.Count; ++index)
            {
                if (CurrentStoryboards[index] == null)
                {
                    continue;
                }

                CurrentStoryboards[index].Remove(element);
            }
            CurrentStoryboards.Clear();

            // Start the new Storyboards
            for (int index = 0; index < newStoryboards.Length; ++index)
            {
                if (newStoryboards[index] == null)
                {
                    continue;
                }

                newStoryboards[index].Begin(element, HandoffBehavior.SnapshotAndReplace, true);
                
                // Hold on to the running Storyboards
                CurrentStoryboards.Add(newStoryboards[index]);

                // Silverlight had an issue where initially, a checked CheckBox would not show the check mark
                // until the second frame. They chose to do a Seek(0) at this point, which this line
                // is supposed to mimic. It does not seem to be equivalent, though, and WPF ends up
                // with some odd animation behavior. I haven't seen the CheckBox issue on WPF, so
                // commenting this out for now.
                // newStoryboards[index].SeekAlignedToLastTick(element, TimeSpan.Zero, TimeSeekOrigin.BeginTime);
            }

        }

        internal void RaiseCurrentStateChanging(FrameworkElement stateGroupsRoot, VisualState oldState, VisualState newState, FrameworkElement control)
        {
            if (CurrentStateChanging != null)
            {
                CurrentStateChanging(stateGroupsRoot, new VisualStateChangedEventArgs(oldState, newState, control, stateGroupsRoot));
            }
        }

        internal void RaiseCurrentStateChanged(FrameworkElement stateGroupsRoot, VisualState oldState, VisualState newState, FrameworkElement control)
        {
            if (CurrentStateChanged != null)
            {
                CurrentStateChanged(stateGroupsRoot, new VisualStateChangedEventArgs(oldState, newState, control, stateGroupsRoot));
            }
        }

        /// <summary>
        ///     Raised when transition begins
        /// </summary>
        public event EventHandler<VisualStateChangedEventArgs> CurrentStateChanged;

        /// <summary>
        ///     Raised when transition ends and new state storyboard begins.
        /// </summary>
        public event EventHandler<VisualStateChangedEventArgs> CurrentStateChanging;

        private Collection<Storyboard> _currentStoryboards;
        private FreezableCollection<VisualState> _states;
        private FreezableCollection<VisualTransition> _transitions;
    } 
}
