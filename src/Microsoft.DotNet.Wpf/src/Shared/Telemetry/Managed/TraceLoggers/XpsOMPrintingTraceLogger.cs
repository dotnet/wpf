// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;
using MS.Internal;
using MS.Internal.Telemetry;
using MS.Internal.Telemetry.PresentationCore;

namespace MS.Internal.Telemetry.PresentationCore
{
    /// <summary>
    /// Trace logger for Xps OM Printing disable regkey
    /// </summary>
    internal static class XpsOMPrintingTraceLogger
    {
        [EventData]
        internal class XpsOMStatus
        {
            public bool Enabled { get; set; } = true;
        }
        /// <summary>
        /// Logs Xps OM Printing being enabled/disabled
        /// </summary>
        internal static void LogXpsOMStatus(bool enabled)
        {
            EventSource logger = TraceLoggingProvider.GetProvider();
            logger?.Write(XpsOMEnabled, TelemetryEventSource.MeasuresOptions(), new XpsOMStatus() { Enabled = enabled });
        }

        /// <summary>
        /// Event name for logging Xps OM Printing enabled/disabled
        /// </summary>
        private static readonly string XpsOMEnabled = "XpsOMEnabled";
    }
}