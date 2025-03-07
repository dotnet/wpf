// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Interop.Tests;

public class ComponentDispatcherTests
{
    // TODO:
    // - Invoke tests (needs RemoteExecutor)

    [Fact]
    public void CurrentKeyboardMessage_Get_ReturnsExpected()
    {
        MSG msg = ComponentDispatcher.CurrentKeyboardMessage;
        Assert.Equal(msg, ComponentDispatcher.CurrentKeyboardMessage);
    }

    [Fact]
    public void IsThreadModal_Get_ReturnsExpected()
    {
        bool isThreadModal = ComponentDispatcher.IsThreadModal;
        Assert.Equal(isThreadModal, ComponentDispatcher.IsThreadModal);
    }

    [Fact]
    public void EnterThreadModal_AddRemove_Success()
    {
        int callCount = 0;
        EventHandler handler = (s, e) => callCount++;
        ComponentDispatcher.EnterThreadModal += handler;
        Assert.Equal(0, callCount);

        ComponentDispatcher.EnterThreadModal -= handler;
        Assert.Equal(0, callCount);

        // Remove non existent.
        ComponentDispatcher.EnterThreadModal -= handler;
        Assert.Equal(0, callCount);

        // Add null.
        ComponentDispatcher.EnterThreadModal += null;
        Assert.Equal(0, callCount);

        // Remove null.
        ComponentDispatcher.EnterThreadModal -= null;
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void LeaveThreadModal_AddRemove_Success()
    {
        int callCount = 0;
        EventHandler handler = (s, e) => callCount++;
        ComponentDispatcher.LeaveThreadModal += handler;
        Assert.Equal(0, callCount);

        ComponentDispatcher.LeaveThreadModal -= handler;
        Assert.Equal(0, callCount);

        // Remove non existent.
        ComponentDispatcher.LeaveThreadModal -= handler;
        Assert.Equal(0, callCount);

        // Add null.
        ComponentDispatcher.LeaveThreadModal += null;
        Assert.Equal(0, callCount);

        // Remove null.
        ComponentDispatcher.LeaveThreadModal -= null;
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void ThreadFilterMessage_AddRemove_Success()
    {
        int callCount = 0;
        ThreadMessageEventHandler handler = (ref MSG m, ref bool h) => callCount++;
        ComponentDispatcher.ThreadFilterMessage += handler;
        Assert.Equal(0, callCount);

        ComponentDispatcher.ThreadFilterMessage -= handler;
        Assert.Equal(0, callCount);

        // Remove non existent.
        ComponentDispatcher.ThreadFilterMessage -= handler;
        Assert.Equal(0, callCount);

        // Add null.
        ComponentDispatcher.ThreadFilterMessage += null;
        Assert.Equal(0, callCount);

        // Remove null.
        ComponentDispatcher.ThreadFilterMessage -= null;
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void ThreadPreprocessMessage_AddRemove_Success()
    {
        int callCount = 0;
        ThreadMessageEventHandler handler = (ref MSG m, ref bool h) => callCount++;
        ComponentDispatcher.ThreadPreprocessMessage += handler;
        Assert.Equal(0, callCount);

        ComponentDispatcher.ThreadPreprocessMessage -= handler;
        Assert.Equal(0, callCount);

        // Remove non existent.
        ComponentDispatcher.ThreadPreprocessMessage -= handler;
        Assert.Equal(0, callCount);

        // Add null.
        ComponentDispatcher.ThreadPreprocessMessage += null;
        Assert.Equal(0, callCount);

        // Remove null.
        ComponentDispatcher.ThreadPreprocessMessage -= null;
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void ThreadIdle_AddRemove_Success()
    {
        int callCount = 0;
        EventHandler handler = (s, e) => callCount++;
        ComponentDispatcher.ThreadIdle += handler;
        Assert.Equal(0, callCount);

        ComponentDispatcher.ThreadIdle -= handler;
        Assert.Equal(0, callCount);

        // Remove non existent.
        ComponentDispatcher.ThreadIdle -= handler;
        Assert.Equal(0, callCount);

        // Add null.
        ComponentDispatcher.ThreadIdle += null;
        Assert.Equal(0, callCount);

        // Remove null.
        ComponentDispatcher.ThreadIdle -= null;
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void RaiseIdle_Invoke_Success()
    {
        // Raise.
        ComponentDispatcher.RaiseIdle();

        // Raise again.
        ComponentDispatcher.RaiseIdle();
    }

    [Fact]
    public void RaiseIdle_InvokeWithHandler_Success()
    {
        int callCount = 0;
        EventHandler handler = (s, e) =>
        {
            Assert.Null(s);
            Assert.Same(EventArgs.Empty, e);
            callCount++;
        };
        ComponentDispatcher.ThreadIdle += handler;

        // Raise.
        ComponentDispatcher.RaiseIdle();
        Assert.Equal(1, callCount);

        // Raise again.
        ComponentDispatcher.RaiseIdle();
        Assert.Equal(2, callCount);

        // Remove handler.
        ComponentDispatcher.ThreadIdle -= handler;
        ComponentDispatcher.RaiseIdle();
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void RaiseThreadMessage_Invoke_Success()
    {
        var msg = new MSG();

        // Raise.
        ComponentDispatcher.RaiseThreadMessage(ref msg);

        // Raise again.
        ComponentDispatcher.RaiseThreadMessage(ref msg);
    }

    [Fact]
    public void PushModal_Invoke_Success()
    {
        bool original = ComponentDispatcher.IsThreadModal;

        // Push.
        ComponentDispatcher.PushModal();
        Assert.True(ComponentDispatcher.IsThreadModal);
        
        // Pop.
        ComponentDispatcher.PopModal();
        Assert.Equal(original, ComponentDispatcher.IsThreadModal);
    }

    [Fact]
    public void PushModal_InvokeMultipleTimes_Success()
    {
        bool original = ComponentDispatcher.IsThreadModal;

        // Push.
        ComponentDispatcher.PushModal();
        Assert.True(ComponentDispatcher.IsThreadModal);

        // Push again.
        ComponentDispatcher.PushModal();
        Assert.True(ComponentDispatcher.IsThreadModal);
        
        // Pop.
        ComponentDispatcher.PopModal();
        Assert.Equal(original, ComponentDispatcher.IsThreadModal);

        // Pop again.
        ComponentDispatcher.PopModal();
        Assert.Equal(original, ComponentDispatcher.IsThreadModal);
    }

    [Fact]
    public void PopModal_Invoke_Success()
    {
        bool original = ComponentDispatcher.IsThreadModal;

        // Push.
        ComponentDispatcher.PushModal();
        Assert.True(ComponentDispatcher.IsThreadModal);

        // Pop.
        ComponentDispatcher.PopModal();
        Assert.Equal(original, ComponentDispatcher.IsThreadModal);
        
        // Pop again.
        ComponentDispatcher.PopModal();
        Assert.Equal(original, ComponentDispatcher.IsThreadModal);
    }

    [Fact]
    public void PopModal_InvokeNoPush_Success()
    {
        // Pop.
        ComponentDispatcher.PopModal();
        Assert.False(ComponentDispatcher.IsThreadModal);
        
        // Pop again.
        ComponentDispatcher.PopModal();
        Assert.False(ComponentDispatcher.IsThreadModal);

        // Push.
        ComponentDispatcher.PushModal();
        Assert.True(ComponentDispatcher.IsThreadModal);

        // Push again.
        ComponentDispatcher.PushModal();
        Assert.True(ComponentDispatcher.IsThreadModal);
    }

    [Fact]
    public void PopModal_InvokeMultipleTimes_Success()
    {
        // Push.
        ComponentDispatcher.PushModal();

        // Pop.
        ComponentDispatcher.PopModal();
        
        // Pop again.
        ComponentDispatcher.PopModal();
    }
}
