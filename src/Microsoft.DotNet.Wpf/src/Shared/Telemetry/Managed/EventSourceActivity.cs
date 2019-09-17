// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Diagnostics.Tracing;
using Contract = System.Diagnostics.Contracts.Contract;

// Use the naming convention MS.Internal.Telemetry.<assemblyname> while adding assemblies to the provider
#if WINDOWS_BASE
namespace MS.Internal.Telemetry.WindowsBase
#elif PRESENTATION_CORE
namespace MS.Internal.Telemetry.PresentationCore
#elif PRESENTATIONFRAMEWORK
namespace MS.Internal.Telemetry.PresentationFramework
#else
#error Attempt to use Telemetry provider in an unexpected assembly.
#error To use the provider in this assembly, update TraceLoggingProvider to support it first.
namespace MS.Internal.Telemetry
#endif
{
    /// <summary>
    /// <para>
    /// Provides support for EventSource activities. An activity consists
    /// of one Start event, any number of normal events, and one Stop event.
    /// All events written by EventSourceActivity (Start, normal, and Stop)
    /// are tagged with a unique activity ID (a GUID). In addition, the Start
    /// event can optionally include the ID of a parent activity,
    /// making it possible to track activity nesting.
    /// </para>
    /// <para>
    /// This class inherits from IDisposable to enable its use in using blocks,
    /// not because it owns any unmanaged resources. It is not necessarily an
    /// error to abandon an activity without calling Dispose.
    /// Calling Dispose() on an activity in the "Started" state is equivalent
    /// to calling Stop("Dispose"). Calling Dispose() on an activity in any
    /// other state is a no-op.
    /// </para>
    /// </summary>
    internal sealed class EventSourceActivity : IDisposable
    {
        private static Guid _emptyGuid;
        private readonly EventSource _eventSource;
        private EventSourceOptions _startStopOptions;
        private Guid _parentId;
        private Guid _id = Guid.NewGuid();
        private State _state;

        /// <summary>
        /// Initializes a new instance of the EventSourceActivity class that
        /// is attached to the specified event source. The new activity will
        /// not be attached to any parent activity.
        /// The activity is created in the Initialized state. Call Start() to
        /// write the activity's Start event.
        /// </summary>
        /// <param name="eventSource">
        /// The event source to which the activity events should be written.
        /// </param>
        internal EventSourceActivity(EventSource eventSource)
            :this(eventSource, new EventSourceOptions())
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the EventSourceActivity class that
        /// is attached to the specified event source. The new activity will
        /// not be attached to any parent activity.
        /// The activity is created in the Initialized state. Call Start() to
        /// write the activity's Start event.
        /// </summary>
        /// <param name="eventSource">
        /// The event source to which the activity events should be written.
        /// </param>
        /// <param name="startStopOptions">
        /// The options to use for the start and stop events of the activity.
        /// Note that the Opcode property will be ignored.
        /// </param>
        internal EventSourceActivity(EventSource eventSource, EventSourceOptions startStopOptions)
            :this(eventSource, startStopOptions, Guid.Empty)
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the EventSourceActivity class that
        /// is attached to the specified event source. The new activity will
        /// be attached to the parent activity as specified by
        /// parentActivityId.
        /// The activity is created in the Initialized state. Call Start() to
        /// write the activity's Start event.
        /// </summary>
        /// <param name="eventSource">
        /// The event source to which the activity events should be written.
        /// </param>
        /// <param name="startStopOptions">
        /// The options to use for the start and stop events of the activity.
        /// Note that the Opcode property will be ignored.
        /// </param>
        /// <param name="parentActivityId">
        /// The id of the parent activity to which this new activity
        /// should be attached.
        /// </param>
        internal EventSourceActivity(EventSource eventSource, EventSourceOptions startStopOptions, Guid parentActivityId)
        {
            Contract.Requires<ArgumentNullException>(eventSource != null, nameof(eventSource));

            _eventSource = eventSource;
            _startStopOptions = startStopOptions;
            _parentId = parentActivityId;
        }

        /// <summary>
        /// Initializes a new instance of the EventSourceActivity class that
        /// is attached to the specified parent activity.
        /// The activity is created in the Initialized state. Call Start() to
        /// write the activity's Start event.
        /// </summary>
        /// <param name="parentActivity">
        /// The parent activity. Activity events will be written
        /// to the event source attached to this activity.
        /// </param>
        internal EventSourceActivity(EventSourceActivity parentActivity)
            :this(parentActivity, new EventSourceOptions())
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the EventSourceActivity class that
        /// is attached to the specified parent activity.
        /// The activity is created in the Initialized state. Call Start() to
        /// write the activity's Start event.
        /// </summary>
        /// <param name="parentActivity">
        /// The parent activity. Activity events will be written
        /// to the event source attached to this activity.
        /// </param>
        /// <param name="startStopOptions">
        /// The options to use for the start and stop events of the activity.
        /// Note that the Opcode property will be ignored.
        /// </param>
        internal EventSourceActivity(EventSourceActivity parentActivity, EventSourceOptions startStopOptions)
        {
            Contract.Requires<ArgumentNullException>(parentActivity != null, nameof(parentActivity));

            _eventSource = parentActivity.EventSource;
            _startStopOptions = startStopOptions;
            _parentId = parentActivity.Id;
        }

        /// <summary>
        /// Gets the event source to which this activity writes events.
        /// </summary>
        internal EventSource EventSource
        {
            get { return _eventSource; }
        }

        /// <summary>
        /// Gets this activity's unique identifier.
        /// </summary>
        internal Guid Id
        {
            get { return _id; }
        }

        /// <summary>
        /// Writes a Start event with the specified name. Sets the activity
        /// to the Started state.
        /// May only be called when the activity is in the Initialized state.
        /// </summary>
        /// <param name="eventName">
        /// The name to use for the event. Must not be null.
        /// </param>
        internal void Start(string eventName)
        {
            Contract.Requires<ArgumentNullException>(eventName != null, nameof(eventName));
            
            var data = EmptyStruct.Instance;
            Start(eventName, ref data);
        }

        /// <summary>
        /// Writes a Start event with the specified name and data. Sets the
        /// activity to the Started state.
        /// May only be called when the activity is in the Initialized state.
        /// </summary>
        /// <param name="eventName">
        /// The name to use for the event. If null, the name is determined from
        /// data's type.
        /// </param>
        /// <param name="data">The data to include in the event.</param>
        internal void Start<T>(string eventName, T data)
        {
            Start(eventName, ref data);
        }

        /// <summary>
        /// Writes a Stop event with the specified name. Sets the activity
        /// to the Stopped state.
        /// May only be called when the activity is in the Started state.
        /// </summary>
        /// <param name="eventName">
        /// The name to use for the event. Must not be null.
        /// </param>
        internal void Stop(string eventName)
        {
            Contract.Requires<ArgumentNullException>(eventName != null, nameof(eventName));

            var data = EmptyStruct.Instance;
            Stop(eventName, ref data);
        }

        /// <summary>
        /// Writes a Stop event with the specified name and data. Sets the
        /// activity to the Stopped state.
        /// May only be called when the activity is in the Started state.
        /// </summary>
        /// <param name="eventName">
        /// The name to use for the event. If null, the name is determined from
        /// data's type.
        /// </param>
        /// <param name="data">The data to include in the event.</param>
        internal void Stop<T>(string eventName, T data)
        {
            Stop(eventName, ref data);
        }

        /// <summary>
        /// Writes an event associated with this activity.
        /// May only be called when the activity is in the Started state.
        /// </summary>
        /// <param name="eventName">
        /// The name to use for the event. Must not be null.
        /// </param>
        internal void Write(string eventName)
        {
            Contract.Requires<ArgumentNullException>(eventName != null, nameof(eventName));

            var options = new EventSourceOptions();
            var data = EmptyStruct.Instance;
            Write(eventName, ref options, ref data);
        }

        /// <summary>
        /// Writes an event associated with this activity.
        /// May only be called when the activity is in the Started state.
        /// </summary>
        /// <param name="eventName">
        /// The name to use for the event. Must not be null.
        /// </param>
        /// <param name="options">
        /// The options to use for the event.
        /// </param>
        internal void Write(string eventName, EventSourceOptions options)
        {
            Contract.Requires<ArgumentNullException>(eventName != null, nameof(eventName));

            var data = EmptyStruct.Instance;
            Write(eventName, ref options, ref data);
        }

        /// <summary>
        /// Writes an event associated with this activity.
        /// May only be called when the activity is in the Started state.
        /// </summary>
        /// <param name="eventName">
        /// The name to use for the event. If null, the name is determined from
        /// data's type.
        /// </param>
        /// <param name="data">The data to include in the event.</param>
        internal void Write<T>(string eventName, T data)
        {
            var options = new EventSourceOptions();
            Write(eventName, ref options, ref data);
        }

        /// <summary>
        /// Writes an event associated with this activity.
        /// May only be called when the activity is in the Started state.
        /// </summary>
        /// <param name="eventName">
        /// The name to use for the event. If null, the name is determined from
        /// data's type.
        /// </param>
        /// <param name="options">
        /// The options to use for the event.
        /// </param>
        /// <param name="data">The data to include in the event.</param>
        internal void Write<T>(string eventName, EventSourceOptions options, T data)
        {
            Write(eventName, ref options, ref data);
        }

        /// <summary>
        /// If the activity is in the Started state, calls Stop("Dispose").
        /// If the activity is in any other state, this is a no-op.
        /// Note that this class inherits from IDisposable to enable use in
        /// using blocks, not because it owns any unmanaged resources. It is
        /// not necessarily an error to abandon an activity without calling
        /// Dispose, especially if you call Stop directly.
        /// </summary>
        public void Dispose()
        {
            if (_state == State.Started)
            {
                _state = State.Stopped;
                var data = EmptyStruct.Instance;
                _eventSource.Write("Dispose", ref _startStopOptions, ref _id, ref _emptyGuid, ref data);
            }
        }

        private void Start<T>(string eventName, ref T data)
        {
            if (_state != State.Initialized)
            {
                throw new InvalidOperationException();
            }

            _state = State.Started;
            _startStopOptions.Opcode = EventOpcode.Start;
            _eventSource.Write(eventName, ref _startStopOptions, ref _id, ref _parentId, ref data);
            _startStopOptions.Opcode = EventOpcode.Stop;
        }

        private void Write<T>(string eventName, ref EventSourceOptions options, ref T data)
        {
            if (_state != State.Started)
            {
                throw new InvalidOperationException();
            }

            _eventSource.Write(eventName, ref options, ref _id, ref _emptyGuid, ref data);
        }

        private void Stop<T>(string eventName, ref T data)
        {
            if (_state != State.Started)
            {
                throw new InvalidOperationException();
            }

            _state = State.Stopped;
            _eventSource.Write(eventName, ref _startStopOptions, ref _id, ref _emptyGuid, ref data);
        }

        private enum State
        {
            Initialized,
            Started,
            Stopped
        }

        [EventData]
        private class EmptyStruct
        {
            private EmptyStruct()
            {

            }

            internal static EmptyStruct Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        _instance = new EmptyStruct();
                    }
                    return _instance;
                }
            }

            private static EmptyStruct _instance;
        }
    }
}