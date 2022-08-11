// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Internal.WindowsBase;
using Microsoft.Win32;
using System;
using System.Collections.Specialized;   // NameValueCollection
using System.Configuration;             // ConfigurationManager
using System.Runtime.Versioning;
using System.Security;


namespace System.Windows
{
    public static class BaseCompatibilityPreferences
    {
        #region constructor

        static BaseCompatibilityPreferences()
        {
            // user can use config file to set preferences
            NameValueCollection appSettings = null;
            try
            {
                appSettings = ConfigurationManager.AppSettings;
            }
            catch (ConfigurationErrorsException)
            {
            }

            if (appSettings != null)
            {
                SetHandleDispatcherRequestProcessingFailureFromAppSettings(appSettings);
            }

            SetMatchPackageSignatureMethodToPackagePartDigestMethod(appSettings);
        }

        #endregion constructor

        #region ReuseDispatcherSynchronizationContextInstance
        /// <summary>
        ///     WPF 4.0 had a performance optimization where it would
        ///     frequently reuse the same instance of the
        ///     DispatcherSynchronizationContext when preparing the
        ///     ExecutionContext for invoking a DispatcherOperation.  This
        ///     had observable impacts on behavior.
        ///
        ///     1) Some task-parallel implementations check the reference
        ///         equality of the SynchronizationContext to determine if the
        ///         completion can be inlined - a significant performance win.
        ///
        ///     2) But, the ExecutionContext would flow the
        ///         SynchronizationContext which could result in the same
        ///         instance of the DispatcherSynchronizationContext being the
        ///         current SynchronizationContext on two different threads.
        ///         The continuations would then be inlined, resulting in code
        ///         running on the wrong thread.
        ///
        ///     In 4.5 we changed this behavior to use a new instance of the
        ///     DispatcherSynchronizationContext for every operation, and
        ///     whenever SynchronizationContext.CreateCopy is called - such
        ///     as when the ExecutionContext is being flowed to another thread.
        ///     This has its own observable impacts:
        ///
        ///     1) Some task-parallel implementations check the reference
        ///         equality of the SynchronizationContext to determine if the
        ///         completion can be inlined - since the instances are
        ///         different, this causes them to resort to the slower
        ///         path for potentially cross-thread completions.
        ///
        ///     2) Some task-parallel implementations implement potentially
        ///         cross-thread completions by callling
        ///         SynchronizationContext.Post and Wait() and an event to be
        ///         signaled.  If this was not a true cross-thread completion,
        ///         but rather just two seperate instances of
        ///         DispatcherSynchronizationContext for the same thread, this
        ///         would result in a deadlock.
        /// </summary>
        public static bool ReuseDispatcherSynchronizationContextInstance
        {
            get { return _reuseDispatcherSynchronizationContextInstance; }
            set
            {
                lock (_lockObject)
                {
                    if (_isSealed)
                    {
                        throw new InvalidOperationException(SR.Format(SR.CompatibilityPreferencesSealed, "ReuseDispatcherSynchronizationContextInstance", "BaseCompatibilityPreferences"));
                    }

                    _reuseDispatcherSynchronizationContextInstance = value;
                }
            }
        }

        internal static bool GetReuseDispatcherSynchronizationContextInstance()
        {
            Seal();

            return ReuseDispatcherSynchronizationContextInstance;
        }

#if NETFX && !NETCOREAPP
        private static bool _reuseDispatcherSynchronizationContextInstance = BinaryCompatibility.TargetsAtLeast_Desktop_V4_5 ? false : true;
#elif NETCOREAPP
        private static bool _reuseDispatcherSynchronizationContextInstance = false;
#else
        private static bool _reuseDispatcherSynchronizationContextInstance = false;
#endif

        #endregion ReuseDispatcherSynchronizationContextInstance

        #region FlowDispatcherSynchronizationContextPriority
        /// <summary>
        ///     WPF <= 4.0 a DispatcherSynchronizationContext always used
        ///     DispatcherPriority.Normal to satisfy
        ///     SynchronizationContext.Post and SynchronizationContext.Send
        ///     calls.
        ///
        ///     With the inclusion of async Task-oriented programming in
        ///     .Net 4.5, we now record the priority of the DispatcherOperation
        ///     in the DispatcherSynchronizationContext and use that to satisfy
        ///     SynchronizationContext.Post and SynchronizationContext.Send
        ///     calls.  This enables async operations to "resume" after an
        ///     await statement at the same priority they are currently running
        ///     at.
        ///
        ///     This is, of course, an observable change in behavior.
        /// </summary>
        public static bool FlowDispatcherSynchronizationContextPriority
        {
            get { return _flowDispatcherSynchronizationContextPriority; }
            set
            {
                lock (_lockObject)
                {
                    if (_isSealed)
                    {
                        throw new InvalidOperationException(SR.Format(SR.CompatibilityPreferencesSealed, "FlowDispatcherSynchronizationContextPriority", "BaseCompatibilityPreferences"));
                    }

                    _flowDispatcherSynchronizationContextPriority = value;
                }
            }
        }

        internal static bool GetFlowDispatcherSynchronizationContextPriority()
        {
            Seal();

            return FlowDispatcherSynchronizationContextPriority;
        }

#if NETFX && !NETCOREAPP
        private static bool _flowDispatcherSynchronizationContextPriority = BinaryCompatibility.TargetsAtLeast_Desktop_V4_5 ? true : false;
#elif NETCOREAPP
        private static bool _flowDispatcherSynchronizationContextPriority = true;
#else
        private static bool _flowDispatcherSynchronizationContextPriority = true;
#endif

        #endregion FlowDispatcherSynchronizationContextPriority

        #region InlineDispatcherSynchronizationContextSend
        /// <summary>
        ///     WPF <= 4.0 a DispatcherSynchronizationContext always used
        ///     DispatcherPriority.Normal to satisfy
        ///     SynchronizationContext.Post and SynchronizationContext.Send
        ///     calls.  This can result in unexpected re-entrancy when calling
        ///     SynchronizationContext.Send on the same thread, since it still
        ///     posts through the Dispatcher queue.
        ///
        ///     In WPF 4.5 we are changing the behavior such that calling
        ///     SynchronizationContext.Send on the same thread, will not post
        ///     through the Dispatcher queue, but rather invoke the delegate
        ///     more directly.  The cross-thread behavior does not change.
        ///
        ///     This is, of course, an observable change in behavior.
        /// </summary>
        public static bool InlineDispatcherSynchronizationContextSend
        {
            get { return _inlineDispatcherSynchronizationContextSend; }
            set
            {
                lock (_lockObject)
                {
                    if (_isSealed)
                    {
                        throw new InvalidOperationException(SR.Format(SR.CompatibilityPreferencesSealed, "InlineDispatcherSynchronizationContextSend", "BaseCompatibilityPreferences"));
                    }

                    _inlineDispatcherSynchronizationContextSend = value;
                }
            }
        }

        internal static bool GetInlineDispatcherSynchronizationContextSend()
        {
            Seal();

            return InlineDispatcherSynchronizationContextSend;
        }

#if NETFX && !NETCOREAPP
        private static bool _inlineDispatcherSynchronizationContextSend = BinaryCompatibility.TargetsAtLeast_Desktop_V4_5 ? true : false;
#elif NETCOREAPP
        private static bool _inlineDispatcherSynchronizationContextSend = true;
#else
        private static bool _inlineDispatcherSynchronizationContextSend = true;
#endif
        #endregion InlineDispatcherSynchronizationContextSend    

        #region MatchPackageSignatureMethodToPackagePartDigestMethod

        private static bool _matchPackageSignatureMethodToPackagePartDigestMethod = true;

        /// <summary>
        ///
        /// Instructs WPF to not attempt to set an equivalent XML package signature method for a given digest hash algorithm.
        /// This affects the signing and digest methods used in the packaging APIs.  With this set to false, signature methods
        /// will always be SHA1.
        /// </summary>
        internal static bool MatchPackageSignatureMethodToPackagePartDigestMethod
        {
            get { return _matchPackageSignatureMethodToPackagePartDigestMethod; }
            set
            {
                _matchPackageSignatureMethodToPackagePartDigestMethod = value;
            }
        }

        static void SetMatchPackageSignatureMethodToPackagePartDigestMethod(NameValueCollection appSettings)
        {
            if (appSettings == null || !SetMatchPackageSignatureMethodToPackagePartDigestMethodFromAppSettings(appSettings))
            {
                SetMatchPackageSignatureMethodToPackagePartDigestMethodFromRegistry();
            }
        }

        static bool SetMatchPackageSignatureMethodToPackagePartDigestMethodFromAppSettings(NameValueCollection appSettings)
        {
            // user can use config file to opt out of signature method fixes
            string s = appSettings[nameof(MatchPackageSignatureMethodToPackagePartDigestMethod)];
            bool value;

            if (Boolean.TryParse(s, out value))
            {
                MatchPackageSignatureMethodToPackagePartDigestMethod = value;
                return true;
            }

            return false;
        }

        #region Registry

        /// <summary>
        /// Contains the setting for whether SignatureMethods should be set to match the strength of the selected
        /// HashAlgorithm.
        /// </summary>
        private static void SetMatchPackageSignatureMethodToPackagePartDigestMethodFromRegistry()
        {
            try
            {
                // Query registry for system override
                using (var regKey = Registry.CurrentUser.OpenSubKey(WpfPackagingSubKeyPath, RegistryKeyPermissionCheck.ReadSubTree))
                {
                    if (regKey != null
                        && regKey.GetValueKind(MatchPackageSignatureMethodToPackagePartDigestMethodValue) == RegistryValueKind.DWord)
                    {
                        var regVal = regKey.GetValue(MatchPackageSignatureMethodToPackagePartDigestMethodValue);

                        if (regVal != null)
                        {
                            MatchPackageSignatureMethodToPackagePartDigestMethod = ((int)regVal == 1);
                        }
                    }
                }
            }
            catch
            {
                // Do nothing here otherwise.
            }
        }

        /// <summary>
        /// String to use for assert of registry permissions
        /// </summary>
        private const string WpfPackagingKey = @"HKEY_CURRENT_USER\" + WpfPackagingSubKeyPath;

        /// <summary>
        /// The key location for the registry switch to configure the Packaging API
        /// </summary>
        private const string WpfPackagingSubKeyPath = @"Software\Microsoft\Avalon.Packaging\";

        /// <summary>
        /// The value of the switch for system wide signature method lookup
        /// </summary>
        private const string MatchPackageSignatureMethodToPackagePartDigestMethodValue = "MatchPackageSignatureMethodToPackagePartDigestMethod";

        #endregion


        #endregion

        #region HandleDispatcherRequestProcessingFailure

        /// <summary>
        ///     A Dispatcher can become unresponsive when it is unable to
        ///     set a timer or post a message to itself.  This failure is usually
        ///     the fault of the application, for posting messages faster than
        ///     the Dispatcher can handle them, or for starving the Dispatcher's
        ///     message pump (or both).
        ///
        ///     As an aid in diagnosing the root cause of this non-responsiveness,
        ///     an app can control how Dispatcher reacts to these failures by setting
        ///     the HandleDispatcherRequestProcessingFailure property.
        /// </summary>
        public static HandleDispatcherRequestProcessingFailureOptions HandleDispatcherRequestProcessingFailure
        {
            // no need for lock or seal - this value can be changed at any time, and
            // the "last writer wins" behavior is fine if multiple threads are involved.
            get { return _handleDispatcherRequestProcessingFailure; }
            set { _handleDispatcherRequestProcessingFailure = value; }
        }

        /// <summary>
        /// The HandleDispatcherRequestProcessingFailureOptions enumeration describes
        /// how Dispatcher reacts to failures encountered while requesting processing.
        /// Dispatcher tries to set a timer or post a message to itself, either
        /// of which can fail if the underlying OS resource is exhausted.
        /// </summary>
        public enum HandleDispatcherRequestProcessingFailureOptions
        {
            /// <summary>
            ///     Continue after the failure.
            ///     The Dispatcher may become unresponsive.
            ///     This is the default, and the behavior of WPF prior to version 4.7.1.
            /// </summary>
            Continue = 0,

            /// <summary>
            ///     Throw an exception.
            ///     This brings the problem to the attention of the app author immediately.
            /// </summary>
            Throw = 1,

            /// <summary>
            ///     Reset the Dispatcher's state to try another request the next
            ///     time one is needed.
            ///     While this can sometimes "repair" non-responsiveness, it cannot honor
            ///     the usual timing of processing, which can be crucial.
            ///     Using this option can lead to unexpected behavior.
            /// </summary>
            Reset = 2,
        }

        static void SetHandleDispatcherRequestProcessingFailureFromAppSettings(NameValueCollection appSettings)
        {
            // user can use config file to set the property
            string s = appSettings["HandleDispatcherRequestProcessingFailure"];
            HandleDispatcherRequestProcessingFailureOptions value;
            if (Enum.TryParse(s, true, out value))
            {
                HandleDispatcherRequestProcessingFailure = value;
            }
        }

        private static HandleDispatcherRequestProcessingFailureOptions
                        _handleDispatcherRequestProcessingFailure;

        #endregion HandleDispatcherRequestProcessingFailure

        private static void Seal()
        {
            if (!_isSealed)
            {
                lock (_lockObject)
                {
                    _isSealed = true;
                }
            }
        }

        private static bool _isSealed;
        private static readonly object _lockObject = new object();
    }
}
