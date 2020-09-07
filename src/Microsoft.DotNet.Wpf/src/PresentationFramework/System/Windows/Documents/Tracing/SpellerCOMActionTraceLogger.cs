// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A trace logging manager for collecting debug information. 
//              See doc comments below for more details. 
//
//              The underlying logging infrastructure does the right thing with
//              respect to user opt-in for CEIP. 
//

using MS.Internal;
using MS.Internal.Telemetry;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace System.Windows.Documents.Tracing
{
    /// <summary>
    /// When a WPF process that has registered custom spellcheck dictionaries 
    /// is killed (for e.g., when an application being debugged under Visual Studio 
    /// is stopped using the debugger), the process does not have adequate opportunity
    /// to unregister all the custom dictionaries. 
    /// 
    /// On Windows 8.1+, this results in stale registrations that can cause perf
    /// problems for the underlying OS COM server that provides the ISpellChecker 
    /// facilities. 
    /// 
    /// The fix for this on the ISpellChecker side is expensive, and we intend to use this 
    /// logger to measure the extent of this problem in the ecosystem, and in turn 
    /// determine whether a fix in ISpellChecker is warranted.
    /// 
    /// Once we start getting this telemetry, we can track the deviation of 
    /// (Sum(Registered) - Sum(Unregistered)) from zero as a measure of the size of 
    /// the problem. 
    /// 
    /// We will also receive COM call times (average and instantaneous ones) which can be used 
    /// to demonstrate the degree of the problem. These times are represented in 100ns intervals. 
    /// For e.g., if the received value is 123456, it should be interpreted 
    /// as 123456 * 100 ns = 12.3456 ms. 
    /// </summary>
    /// <remarks>
    /// When this data is no longer needed, this class and all references to it 
    /// can be safely removed.
    /// </remarks>
    internal class SpellerCOMActionTraceLogger : IDisposable
    {
        /// <summary>
        /// The name of the event that will be logged by this class
        /// </summary>
        private static readonly string SpellerCOMLatencyMeasurement = nameof(SpellerCOMLatencyMeasurement);

        /// <summary>
        /// Various actions that we track in this logger
        /// </summary>
        public enum Actions : int
        {
            /// <summary>
            /// SpellChecker Creation
            /// </summary>
            SpellCheckerCreation,
            /// <summary>
            /// Dictionary Registration
            /// </summary>
            RegisterUserDictionary,
            /// <summary>
            /// Dictionary Unregistration
            /// </summary>
            UnregisterUserDictionary,
            /// <summary>
            /// Spellcheck of text
            /// </summary>
            ComprehensiveCheck
        }

        /// <summary>
        /// Time limits beyond which we will log a trace as an indicator of perf problems. 
        /// These are magic constants derived by empirical trial-and-error. 
        /// If a call takes less than the time indicated here, that may not mean that 
        /// the perf is ideal - it just means that the responsivness is not excessively 
        /// compromised.
        /// 
        /// If a specific call takes more than the time indicated here, we will log it. 
        /// If the running average of a particular call exceeds twice the value in this table, 
        /// then we will log those calls as well.
        /// </summary>
        private static readonly Dictionary<Actions, long> _timeLimits100Ns = new Dictionary<Actions, long>
        {
            {Actions.SpellCheckerCreation,      250  * 10000 }, // 250  ms
            {Actions.ComprehensiveCheck,        50   * 10000 }, // 50   ms
            {Actions.RegisterUserDictionary,    1000 * 10000 }, // 1000 ms
            {Actions.UnregisterUserDictionary,  1000 * 10000 }  // 1000 ms
        };

        /// <summary>
        /// Metadata that is associated uniquely with each instance of <see cref="WinRTSpellerInterop"/>.
        /// </summary>
        private class InstanceInfo
        {
            public Guid Id { get; set; }
            public Dictionary<Actions, long> CumulativeCallTime100Ns { get; set; }
            public Dictionary<Actions, long> NumCallsMeasured { get; set; }
        }

        /// <summary>
        /// A cache of persistent information represented by <see cref="InstanceInfo"/>
        /// uniquely associated with each <see cref="WinRTSpellerInterop"/> instance.
        /// </summary>
        private static WeakDictionary<WinRTSpellerInterop, InstanceInfo> _instanceInfos
            = new WeakDictionary<WinRTSpellerInterop, InstanceInfo>();

        /// <summary>
        /// A lock object to serialize updates to <see cref="_instanceInfos"/>
        /// and the <see cref="InstanceInfo"/> instances contained therein.
        /// </summary>
        private static object _lockObject = new object();

        /// <summary>
        /// The current COM action being tracked
        /// </summary>
        private Actions _action;

        /// <summary>
        /// DateTime.Now.Ticks when the <see cref="_action"/> started
        /// </summary>
        private long _beginTicks;

        /// <summary>
        /// Tracking metadata associated with the instnace of <see cref="WinRTSpellerInterop"/>
        /// that created this instance. 
        /// </summary>
        private InstanceInfo _instanceInfo;

        /// <summary>
        /// Creates a new instance of <see cref="SpellerCOMActionTraceLogger"/> and instantiates
        /// and associates tracking metadata (represented by <see cref="InstanceInfo"/>. 
        /// 
        /// This constructer should usually be called within a using block
        /// </summary>
        /// <param name="caller"/>
        /// <param name="action"/>
        public SpellerCOMActionTraceLogger(WinRTSpellerInterop caller, Actions action)
        {
            _action = action;

            InstanceInfo instanceInfo = null;

            lock (_lockObject)
            {
                if (!_instanceInfos.TryGetValue(caller, out instanceInfo))
                {
                    instanceInfo = new InstanceInfo
                    {
                        Id = Guid.NewGuid(),
                        CumulativeCallTime100Ns = new Dictionary<Actions, long>(),
                        NumCallsMeasured = new Dictionary<Actions, long>()
                    };

                    foreach (Actions a in Enum.GetValues(typeof(Actions)))
                    {
                        instanceInfo.CumulativeCallTime100Ns.Add(a, 0);
                        instanceInfo.NumCallsMeasured.Add(a, 0);
                    }

                    _instanceInfos.Add(caller, instanceInfo);
                }
            }

            _instanceInfo = instanceInfo;
            _beginTicks = DateTime.Now.Ticks;
        }

        /// <summary>
        /// Does the actual work of logging information
        /// </summary>
        /// <remarks>
        /// We do not want a failure in this method to affect callers - so we 
        /// catch and suppress all exceptions
        /// 
        /// Skipping overflow checks in this method because long.MaxValue*100 ns ~= 29k years
        /// </remarks>
        /// <param name="endTicks"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void UpdateRunningAverageAndLogDebugInfo(long endTicks)
        {
            try
            {
                long ticks = (endTicks - _beginTicks);

                lock (_lockObject)
                {
                    _instanceInfo.NumCallsMeasured[_action]++;
                    _instanceInfo.CumulativeCallTime100Ns[_action] += ticks;
                }

                long runningAverage =
                    (long)Math.Floor(1.0d * _instanceInfo.CumulativeCallTime100Ns[_action] / _instanceInfo.NumCallsMeasured[_action]);


                // Always log dictionary register/unregister events
                // Log SpellChecker creation and ComprehensiveCheck if 
                // runningAverage > _timeout100Ns
                bool alwaysLog =
                    (_action == Actions.RegisterUserDictionary) ||
                    (_action == Actions.UnregisterUserDictionary);

                if (alwaysLog || (ticks > _timeLimits100Ns[_action]) || (runningAverage > 2 * _timeLimits100Ns[_action]))
                {
                    var logger = MS.Internal.Telemetry.PresentationFramework.TraceLoggingProvider.GetProvider();
                    var eventSourceOptions = MS.Internal.Telemetry.PresentationFramework.TelemetryEventSource.MeasuresOptions();

                    // Convert 100 ns units to ms
                    // 1 * 100 ns = (1 * 100 / 1000000) ms = (1/10000) ms
                    var traceData = new SpellerCOMTimingData
                    {
                        TextBoxBaseIdentifier = _instanceInfo.Id.ToString(),
                        SpellerCOMAction = _action.ToString(),
                        CallTimeForCOMCallMs = (long)Math.Floor(ticks * 1.0d / 10000),
                        RunningAverageCallTimeForCOMCallsMs = (long)Math.Floor(runningAverage * 1.0d / 10000)
                    };

                    logger?.Write<SpellerCOMTimingData>(SpellerCOMLatencyMeasurement, eventSourceOptions, traceData);
                }
            }
            catch
            {
                // Catch all exceptions - do not propagate any to the caller
            }
        }

        #region IDisposable Support

        private bool _disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    UpdateRunningAverageAndLogDebugInfo(endTicks: DateTime.Now.Ticks);
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        /// <summary>
        /// Structure encoding data needed to track dictionary COM actions
        /// </summary>
        [EventData]
        private struct SpellerCOMTimingData
        {
            /// <summary>
            /// An identifier that will allow us to group 
            /// the data by a given TextBoxBase instance
            /// </summary>
            /// <remarks>
            /// We maintain a 1:1 correspondence between a WinRTSpellerInterop 
            /// instance and a <see cref="InstanceInfo"/> instance by means 
            /// of the static <see cref="_instanceInfos"/> WeakDictionary.
            /// 
            /// Usually (though not always), TextBoxBase instances that have
            /// SpellCheck.IsEnabled = true are associated with a single
            /// WinRTSpellerInterop instance. At the time of logging telemetry, 
            /// we will use the <see cref="InstanceInfo.Id"/> Guid field as an 
            /// identifier for a TextBoxBase instance and use its stringized
            /// representation to initialize this property. 
            /// 
            /// During analysis of logs collected from this logger in the backend, 
            /// data can be grouped by this value to get an overview of perf characteristics 
            /// encountered by individual TextBoxBase instances.
            /// </remarks>
            public string TextBoxBaseIdentifier { get; set; }

            /// <summary>
            /// The speller COM action. This value 
            /// can be "RegisterUserDictionary", "UnregisterUserDictionary", or "ComprehensiveCheck"
            /// </summary>
            public string SpellerCOMAction { get; set; }

            /// <summary>
            /// The call time (into COM) for this dictionary action
            /// </summary>
            public long CallTimeForCOMCallMs { get; set; }

            /// <summary>
            /// The current running average call time for all COM calls being tracked in 100ns units
            /// </summary>
            public long RunningAverageCallTimeForCOMCallsMs { get; set; }
        }
    }
}