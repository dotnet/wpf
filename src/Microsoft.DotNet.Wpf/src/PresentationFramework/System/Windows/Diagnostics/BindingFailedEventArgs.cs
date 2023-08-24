// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//      Contains EventArg types raised to communicate BindingFailed events.

using System.Diagnostics;
using System.Windows.Data;

namespace System.Windows.Diagnostics
{
    /// <summary>
    /// Provides data for <see cref="BindingDiagnostics.BindingFailed"/> 
    /// </summary>
    public class BindingFailedEventArgs : EventArgs
    {
        /// <summary>
        /// For filtering failures (warnings, errors, etc).
        /// </summary>
        public TraceEventType EventType { get; }

        /// <summary>
        /// Failure code.
        /// </summary>
        public int Code { get; }

        /// <summary>
        /// This is the full message that is also written to debug output.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Can be null for some failure codes that don't have a BindingExpressionBase context.
        /// This reference should be used while handling the BindingFailed event and not cached for future use, it
        /// could be holding onto a lot of objects that could be garbage collected.
        /// </summary>
        public BindingExpressionBase Binding { get; }

        /// <summary>
        /// Extra parameters that are unique to certain failure codes, such as an Exception instance.
        /// </summary>
        public object[] Parameters { get; }

        internal BindingFailedEventArgs(TraceEventType eventType, int code, string message, BindingExpressionBase binding, params object[] parameters)
        {
            this.EventType = eventType;
            this.Code = code;
            this.Message = message ?? string.Empty;
            this.Binding = binding;
            this.Parameters = parameters ?? Array.Empty<object>();
        }
    }
}
