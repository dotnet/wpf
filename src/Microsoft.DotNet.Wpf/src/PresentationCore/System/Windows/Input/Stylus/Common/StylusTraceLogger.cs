// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Diagnostics.Contracts;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using MS.Internal;
using MS.Internal.Telemetry;
using MS.Internal.Telemetry.PresentationCore;

namespace System.Windows.Input.Tracing
{
    /// <summary>
    /// Trace logger for the Stylus/Touch stack
    /// </summary>
    internal static class StylusTraceLogger
    {
        #region Enumerations

        /// <summary>
        /// Flags to determine used features in the touch stack.
        /// </summary>
        [Flags]
        internal enum FeatureFlags
        {
            /// <summary>
            /// Default value of no features used
            /// </summary>
            None = 0x00000000,

            /// <summary>
            /// Determines if a class derived from TouchDevice is used by the developer
            /// </summary>
            CustomTouchDeviceUsed = 0x00000001,

            /// <summary>
            /// Determines if any stylus plugin has been used.
            /// </summary>
            StylusPluginsUsed = 0x00000002,

            /// <summary>
            /// Determines if a pen flick is processed and used as a scroll up command
            /// </summary>
            FlickScrollingUsed = 0x00000004,

            /// <summary>
            /// Determines if the WM_POINTER stack is enabled
            /// </summary>
            PointerStackEnabled = 0x10000000,

            /// <summary>
            /// Determines if the WISP based stack is enabled
            /// </summary>
            WispStackEnabled = 0x20000000,
        }

        #endregion

        #region Data Collection Classes

        /// <summary>
        /// A collection of relevant stylus usage statistics and flags
        /// </summary>
        [EventData]
        internal class StylusStatistics
        {
            public FeatureFlags FeaturesUsed { get; set; } = FeatureFlags.None;
        }

        /// <summary>
        /// Tracks known re-entrancy hits in the stylus stack.
        /// </summary>
        [EventData]
        internal class ReentrancyEvent
        {
            public string FunctionName { get; set; } = string.Empty;
        }

        /// <summary>
        /// Class to log Size instances from tablet
        /// </summary>
        [EventData]
        internal class StylusSize
        {
            public StylusSize(Size size)
            {
                Width = size.Width;
                Height = size.Height;
            }

            public double Width { get; set; } = double.NaN;
            public double Height { get; set; } = double.NaN;
        }

        /// <summary>
        /// The information for a particular tablet to log on connect/disconnect
        /// </summary>
        [EventData]
        internal class StylusDeviceInfo
        {
            public StylusDeviceInfo(int id, string name, string pnpId, TabletHardwareCapabilities capabilities,
                Size tabletSize, Size screenSize, TabletDeviceType deviceType, int maxContacts)
            {
                Id = id;
                Name = name;
                PlugAndPlayId = pnpId;
                Capabilities = capabilities.ToString("F");
                TabletSize = new StylusTraceLogger.StylusSize(tabletSize);
                ScreenSize = new StylusTraceLogger.StylusSize(screenSize);
                DeviceType = deviceType.ToString("F");
                MaxContacts = maxContacts;
            }

            public int Id { get; set; }
            public string Name { get; set; }
            public string PlugAndPlayId { get; set; }
            public string Capabilities { get; set; }
            public StylusSize TabletSize { get; set; }
            public StylusSize ScreenSize { get; set; }
            public string DeviceType { get; set; }
            public int MaxContacts { get; set; }
        }

        /// <summary>
        /// The WISP device id of the disconnected device for matching against the
        /// connect.
        /// </summary>
        [EventData]
        internal class StylusDisconnectInfo
        {
            public int Id { get; set; } = -1;
        }

        /// <summary>
        /// For reporting errors in the WISP stack
        /// </summary>
        [EventData]
        internal class StylusErrorEventData
        {
            public string Error { get; set; } = null;
        }

        #endregion

        #region Constants

        /// <summary>
        /// Event name for logging datagrid usage details
        /// </summary>
        private static readonly string StartupEventTag = "StylusStartup";

        /// <summary>
        /// Event name for logging datagrid usage details
        /// </summary>
        private static readonly string ShutdownEventTag = "StylusShutdown";

        /// <summary>
        /// Event name for logging datagrid usage details
        /// </summary>
        private static readonly string StatisticsTag = "StylusStatistics";

        /// <summary>
        /// Event name for a stylus error
        /// </summary>
        private static readonly string ErrorTag = "StylusError";

        /// <summary>
        /// Event name for a stylus connection
        /// </summary>
        private static readonly string DeviceConnectTag = "StylusConnect";

        /// <summary>
        /// Event name for a stylus disconnection
        /// </summary>
        private static readonly string DeviceDisconnectTag = "StylusDisconnect";

        /// <summary>
        /// Event name for a stylus disconnection
        /// </summary>
        private static readonly string ReentrancyTag = "StylusReentrancy";

        /// <summary>
        /// Event name for the retry limits on re-entrancy into the touch stack being reached
        /// </summary>
        private static readonly string ReentrancyRetryLimitTag = "StylusReentrancyRetryLimitReached";

        #endregion

        #region Data Collection Functions

        /// <summary>
        /// Logs Stylus/Touch stack startup
        /// </summary>
        internal static void LogStartup()
        {
            Log(StartupEventTag);
        }

        /// <summary>
        /// Log various statistics about the stack
        /// </summary>
        /// <param name="stylusData">The statistics to log</param>
        internal static void LogStatistics(StylusStatistics stylusData)
        {
            Requires<ArgumentNullException>(stylusData != null);

            Log(StatisticsTag, stylusData);
        }

        /// <summary>
        /// Log that the retry limit for touch stack re-entrancy has been reached.
        /// </summary>
        internal static void LogReentrancyRetryLimitReached()
        {
            Log(ReentrancyRetryLimitTag);
        }

        /// <summary>
        /// Logs an error in the stack
        /// </summary>
        /// <param name="error"></param>
        internal static void LogError(string error)
        {
            Requires<ArgumentNullException>(error != null);

            Log(ErrorTag, new StylusErrorEventData() { Error = error });
        }

        /// <summary>
        /// Logs device information on connect
        /// </summary>
        /// <param name="deviceInfo"></param>
        internal static void LogDeviceConnect(StylusDeviceInfo deviceInfo)
        {
            Requires<ArgumentNullException>(deviceInfo != null);

            Log(DeviceConnectTag, deviceInfo);
        }

        /// <summary>
        /// Logs device id on disconnect
        /// </summary>
        /// <param name="deviceId"></param>
        internal static void LogDeviceDisconnect(int deviceId)
        {
            Log(DeviceDisconnectTag, new StylusDisconnectInfo() { Id = deviceId });
        }

        /// <summary>
        /// Logs detected re-entrancy in the stack
        /// </summary>
        /// <param name="message"></param>
        /// <param name="functionName"></param>
        internal static void LogReentrancy([CallerMemberName] string functionName = "")
        {
            Log(ReentrancyTag, new ReentrancyEvent() { FunctionName = functionName });
        }

        /// <summary>
        /// Logs Stylus/Touch stack shutdown
        /// </summary>
        internal static void LogShutdown()
        {
            Log(ShutdownEventTag);
        }

        #endregion

        #region Utility

        /// <summary>
        /// Throws exception when condition is not met.  We can't use the contracts version of this
        /// since ccrewrite does not work on C++\CLI or netmodules and PresentationCore is a hybrid 
        /// assembly.
        /// </summary>
        /// <typeparam name="T">The type of exception to throw</typeparam>
        /// <param name="condition">The condition to check</param>
        private static void Requires<T>(bool condition)
            where T : Exception, new()
        {
            if (!condition) throw new T();
        }

        /// <summary>
        /// Logs a tag with no associated data
        /// </summary>
        /// <param name="tag">The event tag to log</param>
        private static void Log(string tag)
        {
            EventSource logger = TraceLoggingProvider.GetProvider();
            logger?.Write(tag, TelemetryEventSource.MeasuresOptions());
        }

        /// <summary>
        /// Logs a tag with associated event data
        /// </summary>
        /// <typeparam name="T">The type of the event data</typeparam>
        /// <param name="tag">The event tag to log</param>
        /// <param name="data">The event data to log (default null)</param>
        private static void Log<T>(string tag, T data = null)
            where T : class
        {
            EventSource logger = TraceLoggingProvider.GetProvider();
            logger?.Write(tag, TelemetryEventSource.MeasuresOptions(), data);
        }

        #endregion
    }
}
