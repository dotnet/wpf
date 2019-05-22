// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: See <summary> for RetryHelper class below.
//

using System;
using System.Collections.Generic;
using System.Reflection;
using MS.Internal;

namespace System.Windows.Documents.MsSpellCheckLib
{
    /// <summary>
    /// Helper that abstracts away the work of retrying failed method calls. 
    /// The general outline of the logic of retrying is as follows: 
    /// 
    ///     1. Try executing method (the delegate passed in by the caller)
    ///     2. If Exceptions thrown by Step 1, Then 
    ///         a. Call RetryPreamble method. 
    ///             This will likely reset or reinitialize the object whose delegate was being executed in Step 1
    ///             This will also return a new instance to the delegate in question. 
    ///         b. If more retries are left, Then GOTO Step 1, Else indicate failure and STOP
    ///        End If 
    /// </summary>
    internal static class RetryHelper
    {
        #region Delegates 

        internal delegate bool RetryPreamble();
        internal delegate bool RetryActionPreamble(out Action action);
        internal delegate bool RetryFunctionPreamble<TResult>(out Func<TResult> func);

        #endregion // Delegates

        /// <summary>
        /// See common summary for RetryHelper
        /// </summary>
        /// <param name="action">The Action delegate to be executed until success</param>
        /// <param name="preamble">
        /// The delegate to be executed in-between retries. This should be called with <paramref name="action"/> supplied as the out parameter
        /// </param>
        /// <param name="ignoredExceptions">List of exception types against which resilience is desired</param>
        /// <param name="retries">Number of times to try executing the given method before giving up. Default is 3</param>
        /// <param name="throwOnFailure">Indicates whether the method should throw RetriesExhaustedException, or not</param>
        /// <returns>
        /// True upon successful execution of the method, otherwise False. If <paramref name="throwOnFailure"/> is True, then the method will never return False
        /// </returns>
        internal static bool TryCallAction(
            Action action, 
            RetryActionPreamble preamble, 
            List<Type> ignoredExceptions,
            int retries = 3, 
            bool throwOnFailure = false)
        {
            ValidateExceptionTypeList(ignoredExceptions);

            int retryCount = retries;
            bool success = false;

            bool retryPreambleSucceeded = true;
            do
            {
                try
                {
                    action?.Invoke();
                    success = true;
                    break;
                }
                catch (Exception e) when (MatchException(e, ignoredExceptions))
                {
                    // do nothing here
                    // the exception filter does it all
                }

                retryCount--;
                if (retryCount > 0)
                {
                    retryPreambleSucceeded = preamble(out action);
                }
            }
            while ((retryCount > 0) && retryPreambleSucceeded);

            if (!success && throwOnFailure)
            {
                throw new RetriesExhaustedException();
            }

            return success;
        }

        /// <summary>
        /// See common summary for RetryHelper
        /// </summary>
        /// <param name="action">The Action delegate to be executed until success</param>
        /// <param name="preamble">
        /// The delegate to be executed in-between retries.
        /// </param>
        /// <param name="ignoredExceptions">List of exception types against which resilience is desired</param>
        /// <param name="retries">Number of times to try executing the given method before giving up. Default is 3</param>
        /// <param name="throwOnFailure">Indicates whether the method should throw RetriesExhaustedException, or not</param>
        /// <returns>
        /// True upon successful execution of the method, otherwise False. If <paramref name="throwOnFailure"/> is True, then the method will never return False
        /// </returns>
        internal static bool TryCallAction(
            Action action,
            RetryPreamble preamble,
            List<Type> ignoredExceptions,
            int retries = 3,
            bool throwOnFailure = false)
        {
            ValidateExceptionTypeList(ignoredExceptions);

            int retryCount = retries;
            bool success = false;

            bool retryPreambleSucceeded = true;
            do
            {
                try
                {
                    action?.Invoke();
                    success = true;
                    break;
                }
                catch (Exception e) when (MatchException(e, ignoredExceptions))
                {
                    // do nothing here
                    // the exception filter does it all
                }

                retryCount--;
                if (retryCount > 0)
                {
                    retryPreambleSucceeded = preamble();
                }
            }
            while ((retryCount > 0) && retryPreambleSucceeded);

            if (!success && throwOnFailure)
            {
                throw new RetriesExhaustedException();
            }

            return success;
        }


        /// <summary>
        /// See common summary for RetryHelper
        /// </summary>
        /// <typeparam name="TResult">The type of return value of <paramref name="func"/></typeparam>
        /// <param name="func">The Func<out TResult> delegate to be executed until success</param>
        /// <param name="result">The return value from the execution of <paramref name="func"/></param>
        /// <param name="preamble">
        /// The delegate to be executed in-between retries. This should be called with <paramref name="func"/> supplied as the out parameter.
        /// </param>
        /// <param name="ignoredExceptions">List if exception types against which resilience is desired</param>
        /// <param name="retries">Number of times to try executing the given method before giving up. Default is 3</param>
        /// <param name="throwOnFailure">Indicates whether the method should throw RetriesExhaustedException, or not</param>
        /// <returns>
        /// True upon successful execution of the method, otherwise False. If <paramref name="throwOnFailure"/> is True, then the method will never return False
        /// </returns>
        internal static bool TryExecuteFunction<TResult>(
            Func<TResult> func, 
            out TResult result, 
            RetryFunctionPreamble<TResult> preamble, 
            List<Type> ignoredExceptions, 
            int retries = 3, 
            bool throwOnFailure = false)
        {
            ValidateExceptionTypeList(ignoredExceptions);

            result = default(TResult);
            
            int retryCount = retries;
            bool success = false;


            bool retryPreambleSucceeded = true;
            do
            {
                try
                {
                    if (func != null)
                    {
                        result = func.Invoke();
                    }
                    
                    success = true;
                    break;
                }
                catch (Exception e) when (MatchException(e, ignoredExceptions))
                {
                }

                retryCount--;
                if (retryCount > 0)
                {
                    retryPreambleSucceeded = preamble(out func); 
                }
            }
            while ((retryCount > 0) && retryPreambleSucceeded);

            if (!success && throwOnFailure)
            {
                throw new RetriesExhaustedException();
            }

            return success;
        }

        /// <summary>
        /// See common summary for RetryHelper
        /// </summary>
        /// <typeparam name="TResult">The type of return value of <paramref name="func"/></typeparam>
        /// <param name="func">The Func<out TResult> delegate to be executed until success</param>
        /// <param name="result">The return value from the execution of <paramref name="func"/></param>
        /// <param name="preamble"> The delegate to be executed in-between retries. </param>
        /// <param name="ignoredExceptions">List if exception types against which resilience is desired</param>
        /// <param name="retries">Number of times to try executing the given method before giving up. Default is 3</param>
        /// <param name="throwOnFailure">Indicates whether the method should throw RetriesExhaustedException, or not</param>
        /// <returns>
        /// True upon successful execution of the method, otherwise False. If <paramref name="throwOnFailure"/> is True, then the method will never return False
        /// </returns>
        internal static bool TryExecuteFunction<TResult>(
            Func<TResult> func,
            out TResult result,
            RetryPreamble preamble,
            List<Type> ignoredExceptions,
            int retries = 3,
            bool throwOnFailure = false)
        {
            ValidateExceptionTypeList(ignoredExceptions);

            result = default(TResult);

            int retryCount = retries;
            bool success = false;


            bool retryPreambleSucceeded = true;
            do
            {
                try
                {
                    if (func != null)
                    {
                        result = func.Invoke();
                    }

                    success = true;
                    break;
                }
                catch (Exception e) when (MatchException(e, ignoredExceptions))
                {
                }

                retryCount--;
                if (retryCount > 0)
                {
                    retryPreambleSucceeded = preamble();
                }
            }
            while ((retryCount > 0) && retryPreambleSucceeded);

            if (!success && throwOnFailure)
            {
                throw new RetriesExhaustedException();
            }

            return success;
        }

        #region Private Helpers

        /// <summary>
        /// Verifies whether <paramref name="exception"/> matches one of the <paramref name="exceptions"/>.
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="exceptions"></param>
        /// <returns></returns>
        private static bool MatchException(Exception exception, List<Type> exceptions)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            if (exceptions == null) throw new ArgumentNullException(nameof(exceptions));

            Type exceptionType = exception.GetType();

            Type match = exceptions.Find((e) => e.IsAssignableFrom(exceptionType));
            return (match != null);
        }

        /// <summary>
        /// Asserts that the list of Types given as input are all Exception types. 
        /// </summary>
        /// <param name="exceptions"></param>
        private static void ValidateExceptionTypeList(List<Type> exceptions)
        {
            if (exceptions == null) throw new ArgumentNullException(nameof(exceptions));

            Invariant.Assert(exceptions.TrueForAll((t) => typeof(Exception).IsAssignableFrom(t)));
        }

        #endregion // Private Helpers
    }

    /// <summary>
    /// Exception type thrown by RetryHelper.TryExecuteFunction or RetryHelper.TryCallFunction<T>
    /// </summary>
    internal class RetriesExhaustedException : Exception
    {
        internal RetriesExhaustedException() : base() { }
        internal RetriesExhaustedException(string message) : base(message) { }
        internal RetriesExhaustedException(string message, Exception innerException) : base(message, innerException) { }
    }
}