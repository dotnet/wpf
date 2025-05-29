// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Windows.Threading
{
    /// <summary>
    /// Class for Filtering and Catching Exceptions
    /// </summary>
    internal static class ExceptionWrapper
    {
        internal static event CatchHandler Catch;
        internal static event FilterHandler Filter;

        /// <summary>
        /// Exception Catch Handler Delegate
        ///  Returns true if the exception is "handled"
        ///  Returns false if the caller should rethow the exception.
        /// </summary>
        internal delegate bool CatchHandler(object source, Exception e);

        /// <summary>
        /// Exception Catch Handler
        ///  Returns true if the exception is "handled"
        ///  Returns false if the caller should rethow the exception.
        /// </summary>
        internal delegate bool FilterHandler(object source, Exception e);

        // Helper for exception filtering:
        internal static object TryCatchWhen(object source, Delegate callback, object args, int numArgs, Delegate catchHandler)
        {
            object result = null;

            try
            {
                result = InternalRealCall(callback, args, numArgs);
            }
            catch (Exception e) when (FilterException(source, e))
            {
                if (!CatchException(source, e, catchHandler))
                {
                    throw;
                }
            }

            return result;
        }

        private static object InternalRealCall(Delegate callback, object args, int numArgs)
        {
            object result = null;

            Debug.Assert(numArgs == 0 || // old API, no args
                         numArgs == 1 || // old API, 1 arg, the args param is it
                         numArgs == -1); // new API, any number of args, the args param is an array of them

            // Support the fast-path for certain 0-param and 1-param delegates, even
            // of an arbitrary "params object[]" is passed.
            int numArgsEx = numArgs;
            object singleArg = args;
            if(numArgs == -1)
            {
                object[] argsArr = (object[])args;
                if (argsArr == null || argsArr.Length == 0)
                {
                    numArgsEx = 0;
                }
                else if(argsArr.Length == 1)
                {
                    numArgsEx = 1;
                    singleArg = argsArr[0];
                }
            }

            // Special-case delegates that we know about to avoid the
            // expensive DynamicInvoke call.
            if(numArgsEx == 0)
            {
                if (callback is Action action)
                {
                    action();
                }
                else
                {
                    if (callback is Dispatcher.ShutdownCallback shutdownCallback)
                    {
                        shutdownCallback();
                    }
                    else
                    {
                        // The delegate could return anything.
                        result = callback.DynamicInvoke();
                    }
                }
            }
            else if(numArgsEx == 1)
            {
                if (callback is DispatcherOperationCallback dispatcherOperationCallback)
                {
                    result = dispatcherOperationCallback(singleArg);
                }
                else
                {
                    if (callback is SendOrPostCallback sendOrPostCallback)
                    {
                        sendOrPostCallback(singleArg);
                    }
                    else
                    {
                        if (numArgs == -1)
                        {
                            // Explicitly pass an object[] to DynamicInvoke so that
                            // it will not try to wrap the arg in another object[].
                            result = callback.DynamicInvoke((object[])args);
                        }
                        else
                        {
                            // By pass the args parameter as a single object,
                            // DynamicInvoke will wrap it in an object[] due to the
                            // params keyword.
                            result = callback.DynamicInvoke(args);
                        }
                    }
                }
            }
            else
            {
                // Explicitly pass an object[] to DynamicInvoke so that
                // it will not try to wrap the arg in another object[].
                result = callback.DynamicInvoke((object[])args);
            }

            return result;
        }

        private static bool FilterException(object source, Exception e)
        {
            // If we have a Catch handler we should catch the exception
            // unless the Filter handler says we shouldn't.
            return Filter?.Invoke(source, e) ?? Catch is not null;
        }

        // This returns false when caller should rethrow the exception.
        // true means Exception is "handled" and things just continue on.
        private static bool CatchException(object source, Exception e, Delegate catchHandler)
        {
            if (catchHandler is not null)
            {
                if (catchHandler is DispatcherOperationCallback catchCallback)
                {
                    catchCallback(null);
                }
                else
                {
                    catchHandler.DynamicInvoke(null);
                }
            }

            return Catch?.Invoke(source, e) ?? false;
        }
    }
}


