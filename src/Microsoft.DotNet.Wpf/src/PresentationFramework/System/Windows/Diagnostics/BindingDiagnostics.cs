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

        private static event EventHandler<BindingFailedEventArgs> s_bindingFailed;
        private static List<BindingFailedEventArgs> s_pendingEvents;
        private static readonly object s_pendingEventsLock;
        private const int MaxPendingEvents = 2000;

        static BindingDiagnostics()
        {
            IsEnabled = VisualDiagnostics.IsEnabled && VisualDiagnostics.IsEnvironmentVariableSet(null, XamlSourceInfoHelper.XamlSourceInfoEnvironmentVariable);

            if (IsEnabled)
            {
                // Listeners may miss the initial set of binding failures, so cache events until the first listener attaches.
                // Normally there will only be one listener added soon after the process starts,
                // and it will want to know about any binding failures that already happened.

                s_pendingEvents = new List<BindingFailedEventArgs>();
                s_pendingEventsLock = new object();
            }
        }

        /// <summary>
        /// Handlers of this event should return control to WPF quickly, and not cache BindingFailedEventArgs for future use.
        /// </summary>
        public static event EventHandler<BindingFailedEventArgs> BindingFailed
        {
            add
            {
                if (IsEnabled)
                {
                    s_bindingFailed += value;
                    FlushPendingBindingFailedEvents();
                }
            }

            remove
            {
                s_bindingFailed -= value;
            }
        }

        /// <summary>
        /// Flushes all cached binding failure events and stops any further events from being cached.
        /// </summary>
        private static void FlushPendingBindingFailedEvents()
        {
            if (s_pendingEvents != null)
            {
                List<BindingFailedEventArgs> pendingEvents = null;

                lock (s_pendingEventsLock)
                {
                    pendingEvents = s_pendingEvents;

                    // Don't allow any more event caching
                    s_pendingEvents = null;
                }

                if (pendingEvents != null)
                {
                    foreach (BindingFailedEventArgs args in pendingEvents)
                    {
                        s_bindingFailed?.Invoke(null, args);
                    }
                }
            }
        }

        /// <summary>
        /// Either triggers the BindingFailed event or caches the event for when the first listener attaches.
        /// </summary>
        internal static void NotifyBindingFailed(BindingFailedEventArgs args)
        {
            if (!IsEnabled)
            {
                return;
            }

            if (s_pendingEvents != null)
            {
                lock (s_pendingEventsLock)
                {
                    if (s_pendingEvents != null)
                    {
                        // Limit the pending event count so that memory doesn't grow unbounded if no event handler is ever added
                        if (s_pendingEvents.Count < MaxPendingEvents)
                        {
                            s_pendingEvents.Add(args);
                        }

                        return;
                    }
                }
            }

            s_bindingFailed?.Invoke(null, args);
        }
    }
}
