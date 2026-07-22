// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Threading;

namespace MS.Internal.Automation;

public sealed class ElementUtilTests
{
    [WpfFact]
    public void Invoke_TArg_Success()
    {
        UIElementAutomationPeer peer = new UIElementAutomationPeer(new UIElement());

        ElementUtil.Invoke(peer, static (arg) => Assert.Equal("InvocationTest", arg), "InvocationTest");
    }

    [WpfFact(Skip = "This test is ignored because the default timeout is 3 minutes.")]
    public void Invoke_TArg_CrossThreadInvocation_TimesOut()
    {
        UIElementAutomationPeer peer = new UIElementAutomationPeer(new UIElement());
        TimeoutException? expectedEx = null;

        Thread thread = new Thread(() =>
        {
            // Wait for 5 seconds, set the timeout on ElementUtil sooner than that
            expectedEx = Assert.Throws<TimeoutException>(() => ElementUtil.Invoke(peer, static (delay) =>
            {
                while (true)
                {
                    Thread.Sleep(delay);
                }
            }, 333));
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        // Pump the Dispatcher queue and block until it times out
        thread.Join();

        Assert.NotNull(expectedEx);
        Assert.IsType<TimeoutException>(expectedEx);
    }

    [WpfFact]
    public async Task Invoke_TArg_CrossThreadInvocation_PropagatesException()
    {
        UIElementAutomationPeer peer = new UIElementAutomationPeer(new UIElement());
        InvalidOperationException? expectedEx = null;

        Thread thread = new Thread(() =>
        {
            // Even when invoking on a different thread, the exception should propagate back to the calling thread
            expectedEx = Assert.Throws<InvalidOperationException>(() => ElementUtil.Invoke(peer, static (text) => throw new InvalidOperationException(text), "Throw me"));
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        // We cannot Join here as that would block the queued operation on the Dispatcher thread
        await peer.Dispatcher.InvokeAsync(async () =>
        {
            while (expectedEx is null)
            {
                // Wait until the exception is thrown in the other thread
                await Task.Delay(100).ConfigureAwait(false);
            }

        }, DispatcherPriority.Background);

        // Ensure the thread has completed execution
        thread.Join();

        Assert.NotNull(expectedEx);
        Assert.IsType<InvalidOperationException>(expectedEx);
    }

    [WpfFact]
    public void Invoke_TArg1_TArg2_Success()
    {
        UIElementAutomationPeer peer = new UIElementAutomationPeer(new UIElement());

        static void ActionWithTwoArgs(string arg1, string arg2)
        {
            Assert.Equal("InvocationTest", arg1);
            Assert.Equal("AdditionalArgument", arg2);
        }

        ElementUtil.Invoke(peer, ActionWithTwoArgs, "InvocationTest", "AdditionalArgument");
    }

    [WpfFact]
    public void Invoke_TReturn_TArg_Success()
    {
        UIElementAutomationPeer peer = new UIElementAutomationPeer(new UIElement());

        Assert.Equal(0xFF_FF, ElementUtil.Invoke(peer, static (arg) => 0xFF | arg, 0xFF_FF));
    }

    [WpfFact]
    public void Invoke_TReturn_TArg1_TArg2_Success()
    {
        UIElementAutomationPeer peer = new UIElementAutomationPeer(new UIElement());

        Assert.Equal(66 + 420, ElementUtil.Invoke(peer, static (arg1, arg2) => arg1 + arg2, 66, 420));
    }
}
