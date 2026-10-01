// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Automation.Peers;
using System.Windows.Automation;
using System.Windows.Threading;

namespace MS.Internal.Automation;

/// <summary>
/// Utility class for working with <see cref="AutomationPeer"/>.
/// </summary>
internal static class ElementUtil
{
    /// <summary>
    /// Provides a helper to invoke work on the UI thread, re-throwing all exceptions on the thread that invoked this execution.
    /// </summary>
    internal static object Invoke(AutomationPeer peer, DispatcherOperationCallback work, object arg)
    {
        // Null dispatcher likely means the visual is in bad shape
        Dispatcher dispatcher = peer.Dispatcher ?? throw new ElementNotAvailableException();

        Exception remoteException = null;
        bool completed = false;

        object retVal = dispatcher.Invoke(
            DispatcherPriority.Send,
            TimeSpan.FromMinutes(3),
            (DispatcherOperationCallback)delegate (object unused)
            {
                try
                {
                    return work(arg);
                }
                catch (Exception e)
                {
                    remoteException = e;
                    return null;
                }
                catch        //for non-CLS Compliant exceptions
                {
                    remoteException = null;
                    return null;
                }
                finally
                {
                    completed = true;
                }
            },
            null);

        if (completed)
        {
            if (remoteException is not null)
            {
                throw remoteException;
            }
        }
        else
        {
            bool dispatcherInShutdown = dispatcher.HasShutdownStarted;

            if (dispatcherInShutdown)
            {
                throw new InvalidOperationException(SR.AutomationDispatcherShutdown);
            }
            else
            {
                throw new TimeoutException(SR.AutomationTimeout);
            }
        }

        return retVal;
    }
}
