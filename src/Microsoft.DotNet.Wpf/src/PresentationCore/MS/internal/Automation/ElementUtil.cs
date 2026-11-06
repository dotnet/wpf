// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Windows.Automation.Peers;
using System.Windows.Automation;
using System.Windows.Threading;

#nullable enable

namespace MS.Internal.Automation;

/// <summary>
/// Utility class for working with <see cref="AutomationPeer"/>.
/// </summary>
internal static partial class ElementUtil
{
    /// <summary>
    /// Provides a helper to invoke work on the UI thread, re-throwing all exceptions on the thread that invoked this execution.
    /// </summary>
    internal static void Invoke<TArg>(AutomationPeer peer, Action<TArg> work, TArg arg)
    {
        // Null dispatcher likely means the visual is in bad shape
        Dispatcher dispatcher = peer.Dispatcher ?? throw new ElementNotAvailableException();

        ActionInfo retVal = dispatcher.Invoke(ExceptionWrapper, DispatcherPriority.Send, TimeSpan.FromMinutes(3), work, arg);

        static ActionInfo ExceptionWrapper(Action<TArg> func, TArg arg)
        {
            try
            {
                func(arg);
                return ActionInfo.Completed;
            }
            catch (Exception e)
            {
                return ActionInfo.FromException(e);
            }
        }

        // Either throws an exception if the operation did not complete successfully, or does nothing.
        HandleActionInfo(dispatcher, retVal);
    }

    /// <summary>
    /// Provides a helper to invoke work on the UI thread, re-throwing all exceptions on the thread that invoked this execution.
    /// </summary>
    internal static void Invoke<TArg1, TArg2>(AutomationPeer peer, Action<TArg1, TArg2> work, TArg1 arg1, TArg2 arg2)
    {
        // Null dispatcher likely means the visual is in bad shape
        Dispatcher dispatcher = peer.Dispatcher ?? throw new ElementNotAvailableException();

        ActionInfo retVal = dispatcher.Invoke(ExceptionWrapper, DispatcherPriority.Send, TimeSpan.FromMinutes(3), work, arg1, arg2);

        static ActionInfo ExceptionWrapper(Action<TArg1, TArg2> func, TArg1 arg1, TArg2 arg2)
        {
            try
            {
                func(arg1, arg2);
                return ActionInfo.Completed;
            }
            catch (Exception e)
            {
                return ActionInfo.FromException(e);
            }
        }

        // Either throws an exception if the operation did not complete successfully, or does nothing.
        HandleActionInfo(dispatcher, retVal);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void HandleActionInfo(Dispatcher dispatcher, ActionInfo retVal)
    {
        if (!retVal.HasCompleted)
        {
            if (dispatcher.HasShutdownStarted)
            {
                ThrowInvalidOperationException();
            }
            else
            {
                ThrowTimeoutException();
            }
        }

        if (retVal.StoredException is not null)
            UnwrapException(retVal.StoredException);
    }

    /// <summary>
    /// Provides a helper to invoke work on the UI thread, re-throwing all exceptions on the thread that invoked this execution.
    /// </summary>
    internal static TReturn Invoke<TArg, TReturn>(AutomationPeer peer, Func<TArg, TReturn> work, TArg arg)
    {
        // Null dispatcher likely means the visual is in bad shape
        Dispatcher dispatcher = peer.Dispatcher ?? throw new ElementNotAvailableException();

        ReturnInfo<TReturn> retVal = dispatcher.Invoke(ExceptionWrapper, DispatcherPriority.Send, TimeSpan.FromMinutes(3), work, arg);

        static ReturnInfo<TReturn> ExceptionWrapper(Func<TArg, TReturn> func, TArg arg)
        {
            try
            {
                return ReturnInfo<TReturn>.FromResult(func(arg));
            }
            catch (Exception e)
            {
                return ReturnInfo<TReturn>.FromException(e);
            }
        }

        // Either returns the result or throws an exception if the operation did not complete successfully
        return HandleReturnValue(dispatcher, in retVal);
    }

    /// <summary>
    /// Provides a helper to invoke work on the UI thread, re-throwing all exceptions on the thread that invoked this execution.
    /// </summary>
    internal static TReturn Invoke<TArg1, TArg2, TReturn>(AutomationPeer peer, Func<TArg1, TArg2, TReturn> work, TArg1 arg1, TArg2 arg2)
    {
        // Null dispatcher likely means the visual is in bad shape
        Dispatcher dispatcher = peer.Dispatcher ?? throw new ElementNotAvailableException();

        ReturnInfo<TReturn> retVal = dispatcher.Invoke(ExceptionWrapper, DispatcherPriority.Send, TimeSpan.FromMinutes(3), work, arg1, arg2);

        static ReturnInfo<TReturn> ExceptionWrapper(Func<TArg1, TArg2, TReturn> func, TArg1 arg1, TArg2 arg2)
        {
            try
            {
                return ReturnInfo<TReturn>.FromResult(func(arg1, arg2));
            }
            catch (Exception e)
            {
                return ReturnInfo<TReturn>.FromException(e);
            }
        }

        // Either returns the result or throws an exception if the operation did not complete successfully
        return HandleReturnValue(dispatcher, in retVal);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TReturn HandleReturnValue<TReturn>(Dispatcher dispatcher, ref readonly ReturnInfo<TReturn> retVal)
    {
        if (!retVal.HasCompleted)
        {
            if (dispatcher.HasShutdownStarted)
            {
                ThrowInvalidOperationException();
            }
            else
            {
                ThrowTimeoutException();
            }
        }

        if (retVal.StoredException is not null)
            UnwrapException(retVal.StoredException);

        return retVal.Value;
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> indicating that the associated dispatcher has been shut down.
    /// </summary>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalidOperationException() => throw new InvalidOperationException(SR.AutomationDispatcherShutdown);

    /// <summary>
    /// Throws a <see cref="TimeoutException"/> indicating that the automation operation has timed out.
    /// </summary>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowTimeoutException() => throw new TimeoutException(SR.AutomationTimeout);

    /// <summary>
    /// Unwraps the exception and throws it on the current thread.
    /// </summary>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UnwrapException(Exception exception) => throw exception;

}
