// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
//  Description: Wrapper for System.Threading.ExecutionContext that allows 
//               custom management of information relevant to a logical thread
//               of execution.
//
//               Starting .NET 4.6, ExecutionContext tracks
//               Thread.CurrentCulture and Thread.CurrentUICulture, 
//               which would be restored to their respective previous values 
//               after a call to ExecutionContext.Run. 
//               This behavior is undesirable within the Dispatcher - various dispatcher 
//               operations can run user code that can in turn set Thread.CurrentCulture or 
//               Thread.CurrentUICulture, and we do not want those values to be overwritten 
//               with their respective previous values. 
//
//               This wrapper forwards all calls to ExecutionContext, and manages the 
//               values of Thread.CurrentCulture and Thread.CurrentUICulture carefully
//               during Run and Dispose. 


using System;
using System.Globalization;
using System.Security;
using System.Threading;

namespace MS.Internal
{
    /// <summary>
    /// An encapsulation of ExecutionContext that preserves thread culture infos 
    /// during DispatcherOperations
    /// </summary>
    /// <remarks>
    ///     On applications targeting 4.6 and later, the flow of execution durign a DispatcherOperation
    ///     would go like this:
    /// 
    ///         DispatcherOperation ctor
    ///             EC.Capture                  // EC saves culture info $1 
    ///        (other code runs)                // Modifies culture info to $2 
    ///         DispatcherOperation is scheduled
    ///             EC.Run(callback)            // callback will run under $1 (not $2)
    ///                 callback()              // callback modifies culture info to $3
    ///             EC.Run terminates           // EC reverts culture info to $1 (we lose $3) 
    /// 
    ///     With the use of CulturePreservingExecutionContext, the flow is modified as follows:
    /// 
    ///         DispatcherOperation ctor
    ///             CPEC.Capture                // EC saves culture info $1
    ///         (other code runs)               // Modifies culture info to $2
    ///         DispatcherOperation is scheduled
    ///             CPEC.Run(callback)          // CPEC saves culture info $2 by 
    ///                                         // calling CultureAndContextManager.Initialize
    ///                 Calls EC.Run(CallbackWrapper)
    ///                     CallbackWrapper()   // EC will run this under $1 
    ///                         CallbackWrapper will restore culture info $2
    ///                         callback()      // callback is run under $2, it modifies culture info to $3
    ///                         CallbackWrapper saves $3 for later use
    ///                 EC.Run terminates       // EC reverts culture info to $1
    ///             CPEC.Run restores $3 which was saved by CallbackWrapper
    ///         DispatcherOperation completes - current culture info is set to $3 
    ///
    ///     This flow is similar to the default behavior on .NET 4.5.2 and earlier. 
    /// </remarks>
    internal class CulturePreservingExecutionContext: IDisposable
    {
        #region ExecutionContext Forwarders

        /// <summary>
        ///     Captures the execution context from the current thread.
        /// </summary>
        /// <returns>
        ///     An <see cref="CulturePreservingExecutionContext"/> object representing 
        ///     the <see cref="ExecutionContext"/> for the current thread.
        /// </returns>
        /// <remarks>
        ///     If ExecutionContext.SuppressFlow had been previously called, 
        ///     then this method would return null; 
        /// </remarks>
        public static CulturePreservingExecutionContext Capture()
        {
            // ExecutionContext.SuppressFlow had been called - we expect
            // ExecutionContext.Capture() to return null, so match that 
            // behavior and return null. 
            if (ExecutionContext.IsFlowSuppressed())
            {
                return null; 
            }

            var culturePreservingContext = new CulturePreservingExecutionContext();

            if (culturePreservingContext._context != null)
            {
                return culturePreservingContext;
            }
            else
            {
                // If ExecutionContext.Capture() returns null for any other 
                // reason besides IsFlowSuppressed, then match that behavior
                // and return null 
                culturePreservingContext.Dispose();
                return null; 
            }
        }

        /// <summary>
        /// Runs a method in a specified execution context on the current thread by 
        /// delegating the call to <see cref="CallbackWrapper(object)"/>, which will save
        /// relevant CultureInfo values before returning. 
        /// </summary>
        /// <param name="executionContext">
        ///     The <see cref="ExecutionContext"/> to set, represeted by 
        ///     the <see cref="CulturePreservingExecutionContext"/> instance.
        /// </param>
        /// <param name="callback">
        ///     A <see cref="ContextCallback"/> delegate that represents the 
        ///     method to be run in the provided execution context.
        /// </param>
        /// <param name="state">
        ///     The object to pass to the callback method.
        /// </param>
        /// <remarks>
        /// BaseAppContextSwitches.DoNotUseCulturePreservingDispatcherOperations indicates whether 
        /// CulturePreservingExecutionContext should do extra work to preserve culture infos, or not. 
        /// 
        /// Generally set to true when target framework version is less than or equals 4.5.2, and false
        /// on 4.6 and above. 
        /// 
        /// On 4.5.2 and earlier frameworks, ExecutionContext does not include culture infos 
        /// in its state, nor does it restore them after ExecutionContext.Run. Thus WPF 
        /// does not have to do extra work to propagate culture infos modified within a 
        /// call to ExecutionContext.Run (typically, this happens within a DispatcherOperation). In this
        /// case, we can simply defer all the work to ExecutionContext.Run directly. 
        /// 
        /// On 4.6 and above, the design is to do some extra work to preserve culture infos.
        /// 
        /// This switch can be overridden by the application by calling 
        /// AppContext.SetSwitch("Switch.MS.Internal.DoNotUseCulturePreservingDispatcherOperations", true|false)
        /// or by setting the switch in app.config in the runtime section like this:
        /// <code 
        ///   <runtime>
        ///     <AppContextSwitchOverrides value="Switch.MS.Internal.DoNotUseCulturePreservingDispatcherOperations=true|false"/> 
        ///   </runtime>
        /// />
        /// </remarks>
        public static void Run(CulturePreservingExecutionContext executionContext, ContextCallback callback, object state)
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            if (callback == null) return; // Bail out early if callback is null

            // Compat switch is set, defer directly to EC.Run
            if (BaseAppContextSwitches.DoNotUseCulturePreservingDispatcherOperations)
            {
                ExecutionContext.Run(executionContext._context, callback, state);
                return;
            }

            // Save culture information - we will need this to 
            // restore just before the callback is actually invoked from 
            // CallbackWrapper.
            executionContext._cultureAndContext = CultureAndContextManager.Initialize(callback, state);

            try
            {
                ExecutionContext.Run(
                    executionContext._context,
                    CulturePreservingExecutionContext.CallbackWrapperDelegate,
                    executionContext._cultureAndContext);
            }
            finally
            {
                // Restore culture information - it might have been 
                // modified during the callback execution.
                executionContext._cultureAndContext.WriteCultureInfosToCurrentThread();
            }
}

        #endregion

        #region Private Methods

        /// <summary>
        ///     Executes the callback supplied to the <see cref="Run(CulturePreservingExecutionContext, ContextCallback, object)"/> method
        ///     and saves <see cref="Thread.CurrentUICulture"/> and <see cref="Thread.CurrentCulture"/> values immediately 
        ///     afterwards.
        /// </summary>
        /// <param name="obj">
        ///     Contains a Tuple{ContextCallback, object} which represents the actual callback supplied by the caller of 
        ///     <see cref="Run(CulturePreservingExecutionContext, ContextCallback, object)"/>, and the corresponding state 
        ///     that is intended to be passed to the callback. 
        /// </param>
        private static void CallbackWrapper(object obj)
        {
            var cultureAndContext = obj as CultureAndContextManager;

            ContextCallback callback = cultureAndContext.Callback;
            object state = cultureAndContext.State;

            // Restore cultre information previously saved from the call site, 
            // call into the callback, and recapture culture information which 
            // might have been updated by the callback. 
            // 
            // The callback is guaranteed to be non-null by Run, so an explicit
            // check is not needed here. 

            cultureAndContext.WriteCultureInfosToCurrentThread();
            callback.Invoke(state);
            cultureAndContext.ReadCultureInfosFromCurrentThread();
        }

        #endregion

        #region Constructors

        static CulturePreservingExecutionContext()
        {
            CallbackWrapperDelegate = new ContextCallback(CulturePreservingExecutionContext.CallbackWrapper);
        }


        private CulturePreservingExecutionContext()
        {
            _context = ExecutionContext.Capture();
        }

        #endregion

        #region IDisposable Support

        /// <summary>
        ///     Disposes the encapsulated <see cref="ExecutionContext"/> instance.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _context?.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        ///     Releases all resources used by the current instance of the <see cref="CulturePreservingExecutionContext"/>
        ///     class, which will indirectly release the resources held by the encapsulated <see cref="ExecutionContext"/>
        ///     instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        private bool _disposed = false;

        #endregion

        #region Private Fields

        private ExecutionContext _context;
        private CultureAndContextManager _cultureAndContext;

        // static delegate to prevent repeated implicit allocations during Run
        private static ContextCallback CallbackWrapperDelegate;

        #endregion

        #region Private Types

        /// <summary>
        /// Encapsulates culture, callback and state information. 
        /// Abstracts the work of capture culture information from
        ///   the current thread, and restoring it back.
        /// </summary>
        private class CultureAndContextManager
        {
            #region Constructor 

            private CultureAndContextManager(ContextCallback callback, object state)
            {
                Callback = callback;
                State = state;
                ReadCultureInfosFromCurrentThread();
            }

            #endregion

            /// <summary>
            /// Factory - Captures cuture information from current thread, and 
            /// saves callback and state information for future use by the caller. 
            /// </summary>
            /// <param name="callback"></param>
            /// <param name="state"></param>
            /// <returns></returns>
            public static CultureAndContextManager Initialize(ContextCallback callback, object state)
            {
                return new CultureAndContextManager(callback, state);
            }


            public void ReadCultureInfosFromCurrentThread()
            {
                _culture = Thread.CurrentThread.CurrentCulture;
                _uICulture = Thread.CurrentThread.CurrentUICulture;
            }

            public void WriteCultureInfosToCurrentThread()
            {
                Thread.CurrentThread.CurrentCulture = _culture;
                Thread.CurrentThread.CurrentUICulture = _uICulture;
            }

            public ContextCallback Callback
            {
                get; private set;
            }

            public object State
            {
                get; private set;
            }

            private CultureInfo _culture;
            private CultureInfo _uICulture;
        }

        #endregion
    }
}
