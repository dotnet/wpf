// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
* This class provides a public interface to access TraceSources for
* enabling/disabling/filtering of trace messages by area.
*
* The other portion of this partial class is generated using
* genTraceSource.pl and AvTraceMessages.txt
*
*
\***************************************************************************/

using System.Collections.Generic;
using System.Security;
using MS.Internal;
using System.Windows;

namespace System.Diagnostics
{
    /// <summary>
    /// PresentationTraceLevel - Enum which describes how much detail to trace about a particular object.
    /// </summary>
    public enum PresentationTraceLevel
    {
        /// <summary>
        /// Trace no additional information.
        /// </summary>
        None,

        /// <summary>
        /// Trace some additional information.
        /// </summary>
        Low,

        /// <summary>
        /// Trace a medium amount of additional information.
        /// </summary>
        Medium,

        /// <summary>
        /// Trace all available additional information.
        /// </summary>
        High,
    }

    /// <summary>
    /// Helper class for retrieving TraceSources
    /// </summary>

    public static partial class PresentationTraceSources
    {
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// DependencyProperty for TraceLevel property.
        /// </summary>
        public static readonly DependencyProperty TraceLevelProperty =
                DependencyProperty.RegisterAttached(
                        "TraceLevel",
                        typeof(PresentationTraceLevel),
                        typeof(PresentationTraceSources));

        /// <summary>
        /// Reads the attached property TraceLevel from the given element.
        /// </summary>
        public static PresentationTraceLevel GetTraceLevel(object element)
        {
            return TraceLevelStore.GetTraceLevel(element);
        }

        /// <summary>
        /// Writes the attached property TraceLevel to the given element.
        /// </summary>
        public static void SetTraceLevel(object element, PresentationTraceLevel traceLevel)
        {
            TraceLevelStore.SetTraceLevel(element, traceLevel);
        }

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Refresh TraceSources (re-read config file), creating if necessary.
        /// </summary>
        // Note: Better would be to separate enable from the Refresh method.
        public static void Refresh()
        {
            // Let AvTrace know that an explicit Refresh has been called.
            AvTrace.OnRefresh();

            // Re-read the .config files
            System.Diagnostics.Trace.Refresh();

            // Initialize any traces classes if needed
            if (TraceRefresh != null)
            {
                TraceRefresh();
            }
        }

        internal static event TraceRefreshEventHandler TraceRefresh;

        private static TraceSource CreateTraceSource(string sourceName)
        {
            // Create the trace source.  Whether or not it will actually
            // trace anything is a decision of the trace source, e.g. it
            // depends on the app.config file settings.

            TraceSource source = new TraceSource(sourceName);

            // If we're attached to the debugger, ensure that at least
            // warnings/errors are getting traced.

            if (source.Switch.Level == SourceLevels.Off
                &&
                AvTrace.IsDebuggerAttached())
            {
                source.Switch.Level = SourceLevels.Warning;
            }

            // returning source after reverting the assert to avoid
            // using exposed elements under the assert
            return source;
        }
}

    internal delegate void TraceRefreshEventHandler();
}

