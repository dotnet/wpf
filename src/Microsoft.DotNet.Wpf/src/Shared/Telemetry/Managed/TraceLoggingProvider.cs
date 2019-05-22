// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;
using MS.Internal.Telemetry;

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
    /// Registers provider for TraceLogging
    /// </summary>
    internal static class TraceLoggingProvider
    {
        /// <summary>
        /// Registers the provider and returns the instance
        /// </summary>
        /// <returns>EventSource logger if successful, null otherwise</returns>
        internal static EventSource GetProvider()
        {
            if (_logger == null)
            {
                lock (_lockObject)
                {
                    if (_logger == null)
                    {
                        try
                        {
                            _logger = new TelemetryEventSource(ProviderName);
                        }
                        catch(ArgumentException)
                        {
                            // do nothing as we expect _logger to be null in case exception
                        }
                    }
                }
            }
            return _logger;
        }

        private static EventSource _logger;
        private static object _lockObject = new object();

#if WINDOWS_BASE
        /// <summary>
        /// Windows Base provider name
        /// </summary>
        private static readonly string ProviderName = "Microsoft.DOTNET.WPF.WindowsBase";
#elif PRESENTATION_CORE
        /// <summary>
        /// Presentation Core provider name
        /// </summary>
        private static readonly string ProviderName = "Microsoft.DOTNET.WPF.PresentationCore";
#elif PRESENTATIONFRAMEWORK
        /// <summary>
        /// Presentation Framework provider name
        /// </summary>
        private static readonly string ProviderName = "Microsoft.DOTNET.WPF.PresentationFramework";
#endif
    }
}