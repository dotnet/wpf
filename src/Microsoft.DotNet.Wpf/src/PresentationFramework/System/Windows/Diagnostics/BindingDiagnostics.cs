// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//  Binding diagnostics API
//

using System.Collections.Generic;

namespace System.Windows.Diagnostics
{
    /// <summary>
    /// Provides a notification infrastructure for listening to binding failure events.
    /// </summary>
    /// <remarks>
    /// This type supports the .NET Framework infrastructure and is not intended to be used directly
    /// from application code.
    /// </remarks>
    public static class BindingDiagnostics
    {
        internal static bool IsEnabled { get; private set; }

        private static event EventHandler<BindingFailedEventArgs> bindingFailed;
        private static List<BindingFailedEventArgs> pendingEvents;
        private static object pendingEventsLock;
        private const int maxPendingEvents = 2000;

        static BindingDiagnostics()
        {
            BindingDiagnostics.IsEnabled = VisualDiagnostics.IsEnabled && VisualDiagnostics.IsEnvironmentVariableSet(null, XamlSourceInfoHelper.XamlSourceInfoEnvironmentVariable);

            if (BindingDiagnostics.IsEnabled)
            {
                // Listeners may miss the initial set of binding failures, so cache events until the first listener attaches.
                // Normally there will only be one listener added soon after the process starts,
                // and it will want to know about any binding failures that already happened.

                BindingDiagnostics.pendingEvents = new List<BindingFailedEventArgs>();
                BindingDiagnostics.pendingEventsLock = new object();
            }
        }

        /// <summary>
        /// Handlers of this event should return control to WPF quickly, and not cache BindingFailedEventArgs for future use.
        /// </summary>
        public static event EventHandler<BindingFailedEventArgs> BindingFailed
        {
            add
            {
                if (BindingDiagnostics.IsEnabled)
                {
                    BindingDiagnostics.bindingFailed += value;
                    BindingDiagnostics.FlushPendingBindingFailedEvents();
                }
            }

            remove
            {
                BindingDiagnostics.bindingFailed -= value;
            }
        }

        /// <summary>
        /// Flushes all cached binding failure events and stops any further events from being cached.
        /// </summary>
        private static void FlushPendingBindingFailedEvents()
        {
            if (BindingDiagnostics.pendingEvents != null)
            {
                BindingFailedEventArgs[] pendingEventsCopy = null;

                lock (BindingDiagnostics.pendingEventsLock)
                {
                    pendingEventsCopy = BindingDiagnostics.pendingEvents?.ToArray();

                    // Don't allow any more event caching
                    BindingDiagnostics.pendingEvents = null;
                }

                if (pendingEventsCopy != null)
                {
                    foreach (BindingFailedEventArgs args in pendingEventsCopy)
                    {
                        BindingDiagnostics.bindingFailed?.Invoke(null, args);
                    }
                }
            }
        }

        /// <summary>
        /// Either triggers the BindingFailed event or caches the event for when the first listener attaches.
        /// </summary>
        internal static void NotifyBindingFailed(BindingFailedEventArgs args)
        {
            if (!BindingDiagnostics.IsEnabled)
            {
                return;
            }

            if (BindingDiagnostics.pendingEvents != null)
            {
                lock (BindingDiagnostics.pendingEventsLock)
                {
                    if (BindingDiagnostics.pendingEvents != null)
                    {
                        // Limit the pending event count so that memory doesn't grow unbounded if no event handler is ever added
                        if (BindingDiagnostics.pendingEvents.Count < BindingDiagnostics.maxPendingEvents)
                        {
                            BindingDiagnostics.pendingEvents.Add(args);
                        }

                        return;
                    }
                }
            }

            BindingDiagnostics.bindingFailed?.Invoke(null, args);
        }
    }
}
