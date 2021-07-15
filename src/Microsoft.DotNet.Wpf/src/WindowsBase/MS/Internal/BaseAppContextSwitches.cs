// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Runtime.CompilerServices;

namespace MS.Internal
{
    // There are cases where we have multiple assemblies that are going to import this file and
    // if they are going to also have InternalsVisibleTo between them, there will be a compiler warning
    // that the type is found both in the source and in a referenced assembly. The compiler will prefer
    // the version of the type defined in the source
    //
    // In order to disable the warning for this type we are disabling this warning for this entire file.
    #pragma warning disable 436

    /// <summary>
    /// Appcompat switches used by WindowsBase. See comments at the start of each switch.
    /// Also see AppContextDefaultValues which initializes default values for each of
    /// these switches.
    /// </summary>
    internal static class BaseAppContextSwitches
    {
        /// <summary>
        /// Starting .NET 4.6, ExecutionContext tracks Thread.CurrentCulture and Thread.CurrentUICulture, which would be restored
        /// to their respective previous values after a call to ExecutionContext.Run. This behavior is undesirable within the
        /// Dispatcher - various dispatcher operations can run user code that can in turn set Thread.CurrentCulture or
        /// Thread.CurrentUICulture, and we do not want those values to be overwritten with their respective previous values.
        /// To work around the new ExecutionContext behavior, we introduce CulturePreservingExecutionContext for use within
        /// Dispatcher and DispatcherOperation. WPF in .NET 4.6 & 4.6.1 shipped with buggy behavior - each DispatcherOperation
        /// ends with all modificaitons to culture infos being reverted.Though unlikely, if some applications targeting 4.6 or
        /// above might have taken a dependence on this bug, we provide this compatiblity switch that can be enabled by the application.
        /// </summary>
        #region UseCulturePreservingDispatcherOperations

        internal const string SwitchDoNotUseCulturePreservingDispatcherOperations = "Switch.MS.Internal.DoNotUseCulturePreservingDispatcherOperations";
        private static int _doNotUseCulturePreservingDispatcherOperations;

        public static bool DoNotUseCulturePreservingDispatcherOperations
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(SwitchDoNotUseCulturePreservingDispatcherOperations, ref _doNotUseCulturePreservingDispatcherOperations);
            }
        }

        #endregion

        /// <summary>
        /// PacakageDigitalSignatureManager.DefaultHashAlgorithm is now SHA256.  Setting this flag will make it SHA1 as it is in legacy scenarios prior to .NET 4.7.1.
        /// </summary>
        #region UseSha1AsDefaultHashAlgorithmForDigitalSignatures

        internal const string SwitchUseSha1AsDefaultHashAlgorithmForDigitalSignatures = "Switch.MS.Internal.UseSha1AsDefaultHashAlgorithmForDigitalSignatures";
        private static int _useSha1AsDefaultHashAlgorithmForDigitalSignatures;

        public static bool UseSha1AsDefaultHashAlgorithmForDigitalSignatures
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(SwitchUseSha1AsDefaultHashAlgorithmForDigitalSignatures, ref _useSha1AsDefaultHashAlgorithmForDigitalSignatures);
            }
        }

        #endregion

        /// <summary>
        /// Allowing developers to turn off the Invoke as there are compat issues with timing during shutdown for some applications.
        /// Applications that require this switch are most likely mismanaging their Dispatchers.  They should ensure that any Dispatchers created on a
        /// worker thread are shut down prior to shutting down the AppDomain or process.  While this switch may help to alleviate specific symptoms of this
        /// it does not handle all possible side effects of this application bug.
        /// </summary>
        #region DoNotInvokeInWeakEventTableShutdownListener

        internal const string SwitchDoNotInvokeInWeakEventTableShutdownListener = "Switch.MS.Internal.DoNotInvokeInWeakEventTableShutdownListener";
        private static int _doNotInvokeInWeakEventTableShutdownListener;

        public static bool DoNotInvokeInWeakEventTableShutdownListener
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(SwitchDoNotInvokeInWeakEventTableShutdownListener, ref _doNotInvokeInWeakEventTableShutdownListener);
            }
        }

        #endregion

        /// <summary>
        /// Enable/disable various perf and memory improvements related to WeakEvents
        /// and the cleanup of WeakReference-dependent data structures
        /// </summary>
        #region EnableWeakEventMemoryImprovements

        internal const string SwitchEnableWeakEventMemoryImprovements = "Switch.MS.Internal.EnableWeakEventMemoryImprovements";
        private static int _enableWeakEventMemoryImprovements;

        public static bool EnableWeakEventMemoryImprovements
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(SwitchEnableWeakEventMemoryImprovements, ref _enableWeakEventMemoryImprovements);
            }
        }

        #endregion

        /// <summary>
        /// Enable/disable heuristic for scheduling cleanup of
        /// WeakReference-dependent data structures
        /// </summary>
        #region EnableCleanupSchedulingImprovements

        internal const string SwitchEnableCleanupSchedulingImprovements = "Switch.MS.Internal.EnableCleanupSchedulingImprovements";
        private static int _enableCleanupSchedulingImprovements;

        public static bool EnableCleanupSchedulingImprovements
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(SwitchEnableCleanupSchedulingImprovements, ref _enableCleanupSchedulingImprovements);
            }
        }

        #endregion
    }

#pragma warning restore 436
}
