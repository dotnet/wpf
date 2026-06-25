// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Windows.Threading.Tests;

public class DispatcherOperationTests
{
    [Fact]
    public void Aborted_AddRemove_Success()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        DispatcherOperation operation = dispatcher.BeginInvoke(() => {});

        int callCount = 0;
        EventHandler handler = (s, e) => callCount++;
        operation.Aborted += handler;
        Assert.Equal(0, callCount);

        operation.Aborted -= handler;
        Assert.Equal(0, callCount);

        // Remove non existent.
        operation.Aborted -= handler;
        Assert.Equal(0, callCount);

        // Add null.
        operation.Aborted += null;
        Assert.Equal(0, callCount);

        // Remove null.
        operation.Aborted -= null;
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void Completed_AddRemove_Success()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        DispatcherOperation operation = dispatcher.BeginInvoke(() => {});

        int callCount = 0;
        EventHandler handler = (s, e) => callCount++;
        operation.Completed += handler;
        Assert.Equal(0, callCount);

        operation.Completed -= handler;
        Assert.Equal(0, callCount);

        // Remove non existent.
        operation.Completed -= handler;
        Assert.Equal(0, callCount);

        // Add null.
        operation.Completed += null;
        Assert.Equal(0, callCount);

        // Remove null.
        operation.Completed -= null;
        Assert.Equal(0, callCount);
    }

    [Theory]
    [InlineData(DispatcherPriority.ApplicationIdle)]
    [InlineData(DispatcherPriority.Background)]
    [InlineData(DispatcherPriority.ContextIdle)]
    [InlineData(DispatcherPriority.DataBind)]
    [InlineData(DispatcherPriority.Inactive)]
    [InlineData(DispatcherPriority.Input)]
    [InlineData(DispatcherPriority.Loaded)]
    [InlineData(DispatcherPriority.Normal)]
    [InlineData(DispatcherPriority.Render)]
    [InlineData(DispatcherPriority.Send)]
    [InlineData(DispatcherPriority.SystemIdle)]
    public void Priority_Set_GetReturnsExpected(DispatcherPriority value)
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        DispatcherOperation operation = dispatcher.BeginInvoke(() => { });
        
        // Set.
        operation.Priority = value;
        Assert.Equal(value, operation.Priority);

        // Set same.
        operation.Priority = value;
        Assert.Equal(value, operation.Priority);
    }

    [Theory]
    [InlineData(DispatcherPriority.Invalid)]
    [InlineData(DispatcherPriority.Invalid - 1)]
    [InlineData(DispatcherPriority.Send + 1)]
    public void Priority_SetInvalid_ThrowsInvalidEnumArgumentException(DispatcherPriority value)
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        DispatcherOperation operation = dispatcher.BeginInvoke(() => { });

        Assert.Throws<InvalidEnumArgumentException>("value", () => operation.Priority = value);
    }

    [Fact]
    public void Abort_Invoke_Success()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        int callCount = 0;
        DispatcherOperation operation = null!;
        Delegate method = new Action(() => callCount++);
        operation = dispatcher.BeginInvoke(method);

        // Abort.
        Assert.True(operation.Abort());
        Assert.Equal(0, callCount);
        Assert.Equal(DispatcherOperationStatus.Aborted, operation.Status);
        Assert.Null(operation.Result);

        // Abort again.
        Assert.False(operation.Abort());
        Assert.Equal(0, callCount);
        Assert.Equal(DispatcherOperationStatus.Aborted, operation.Status);
        Assert.Null(operation.Result);
    }

    [Fact]
    public void Abort_InvokeCompleted_Success()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        int callCount = 0;
        DispatcherOperation operation = null!;
        Delegate method = new Action(() => callCount++);
        operation = dispatcher.BeginInvoke(method);

        // Wait.
        Assert.Equal(DispatcherOperationStatus.Completed, operation.Wait());
        Assert.Equal(1, callCount);
        Assert.Equal(DispatcherOperationStatus.Completed, operation.Status);
        Assert.Null(operation.Result);

        // Abort.
        Assert.False(operation.Abort());
        Assert.Equal(1, callCount);
        Assert.Equal(DispatcherOperationStatus.Completed, operation.Status);
        Assert.Null(operation.Result);

        // Abort again.
        Assert.False(operation.Abort());
        Assert.Equal(1, callCount);
        Assert.Equal(DispatcherOperationStatus.Completed, operation.Status);
        Assert.Null(operation.Result);
    }

    [Fact]
    public void Abort_InvokeWithHandler_CallsAborted()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        int callCount = 0;
        DispatcherOperation operation = null!;
        Delegate method = new Action(() => callCount++);
        operation = dispatcher.BeginInvoke(method);

        int abortedCallCount = 0;
        EventHandler aborted = (sender, e) =>
        {
            Assert.Same(operation, sender);
            Assert.Equal(EventArgs.Empty, e);
            abortedCallCount++;
        };
        operation.Aborted += aborted;

        // Abort.
        Assert.True(operation.Abort());
        Assert.Equal(0, callCount);
        Assert.Equal(1, abortedCallCount);
        Assert.Equal(DispatcherOperationStatus.Aborted, operation.Status);
        Assert.Null(operation.Result);

        // Abort again.
        Assert.False(operation.Abort());
        Assert.Equal(0, callCount);
        Assert.Equal(1, abortedCallCount);
        Assert.Equal(DispatcherOperationStatus.Aborted, operation.Status);
        Assert.Null(operation.Result);
    }

    [Fact]
    public void Abort_InvokeWithRemovedHandler_DoesNotCallAborted()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        int callCount = 0;
        DispatcherOperation operation = null!;
        Delegate method = new Action(() => callCount++);
        operation = dispatcher.BeginInvoke(method);

        int abortedCallCount = 0;
        EventHandler aborted = (sender, e) => abortedCallCount++;
        operation.Aborted += aborted;
        operation.Aborted -= aborted;

        // Abort.
        Assert.True(operation.Abort());
        Assert.Equal(0, callCount);
        Assert.Equal(0, abortedCallCount);
        Assert.Equal(DispatcherOperationStatus.Aborted, operation.Status);
        Assert.Null(operation.Result);

        // Abort again.
        Assert.False(operation.Abort());
        Assert.Equal(0, callCount);
        Assert.Equal(0, abortedCallCount);
        Assert.Equal(DispatcherOperationStatus.Aborted, operation.Status);
        Assert.Null(operation.Result);
    }

    [Fact]
    public void Wait_InvokeNoResult_Success()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        int callCount = 0;
        DispatcherOperation operation = null!;
        Delegate method = new Action(() =>
        {
            Assert.Equal(DispatcherOperationStatus.Executing, operation.Status);
            callCount++;
        }); 
        operation = dispatcher.BeginInvoke(method);
        
        // Wait.
        Assert.Equal(DispatcherOperationStatus.Completed, operation.Wait());
        Assert.Equal(1, callCount);
        Assert.Equal(DispatcherOperationStatus.Completed, operation.Status);
        Assert.Null(operation.Result);

        // Wait again.
        Assert.Equal(DispatcherOperationStatus.Completed, operation.Wait());
        Assert.Equal(1, callCount);
        Assert.Equal(DispatcherOperationStatus.Completed, operation.Status);
        Assert.Null(operation.Result);
    }
    
    [Fact]
    public void Wait_InvokeResult_Success()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        int callCount = 0;
        DispatcherOperation operation = null!;
        var result = new object();
        Delegate method = new Func<object>(() =>
        {
            Assert.Equal(DispatcherOperationStatus.Executing, operation.Status);
            callCount++;
            return result;
        }); 
        operation = dispatcher.BeginInvoke(method);

        // Wait.
        Assert.Equal(DispatcherOperationStatus.Completed, operation.Wait());
        Assert.Equal(1, callCount);
        Assert.Equal(DispatcherOperationStatus.Completed, operation.Status);
        Assert.Same(result, operation.Result);
        
        // Wait again.
        Assert.Equal(DispatcherOperationStatus.Completed, operation.Wait());
        Assert.Equal(1, callCount);
        Assert.Equal(DispatcherOperationStatus.Completed, operation.Status);
        Assert.Same(result, operation.Result);
    }

    [Fact]
    public void Wait_InvokeWithHandler_CallsCompleted()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        int callCount = 0;
        DispatcherOperation operation = null!;
        Delegate method = new Action(() =>
        {
            Assert.Equal(DispatcherOperationStatus.Executing, operation.Status);
            callCount++;
        }); 
        operation = dispatcher.BeginInvoke(method);

        int completedCallCount = 0;
        EventHandler handler = (sender, e) =>
        {
            Assert.Same(operation, sender);
            Assert.Equal(EventArgs.Empty, e);
            completedCallCount++;
        };
        operation.Completed += handler;
        
        // Wait.
        Assert.Equal(DispatcherOperationStatus.Completed, operation.Wait());
        Assert.Equal(1, callCount);
        Assert.Equal(1, completedCallCount);
        Assert.Equal(DispatcherOperationStatus.Completed, operation.Status);
        Assert.Null(operation.Result);

        // Wait again.
        Assert.Equal(DispatcherOperationStatus.Completed, operation.Wait());
        Assert.Equal(1, callCount);
        Assert.Equal(1, completedCallCount);
        Assert.Equal(DispatcherOperationStatus.Completed, operation.Status);
        Assert.Null(operation.Result);
    }

    [Fact]
    public void Wait_InvokeWithRemovedHandler_DoesNotCallCompleted()
    {
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        int callCount = 0;
        DispatcherOperation operation = null!;
        Delegate method = new Action(() =>
        {
            Assert.Equal(DispatcherOperationStatus.Executing, operation.Status);
            callCount++;
        }); 
        operation = dispatcher.BeginInvoke(method);

        int completedCallCount = 0;
        EventHandler handler = (sender, e) => completedCallCount++;
        operation.Completed += handler;
        operation.Completed -= handler;
        
        // Wait.
        Assert.Equal(DispatcherOperationStatus.Completed, operation.Wait());
        Assert.Equal(1, callCount);
        Assert.Equal(0, completedCallCount);
        Assert.Equal(DispatcherOperationStatus.Completed, operation.Status);
        Assert.Null(operation.Result);

        // Wait again.
        Assert.Equal(DispatcherOperationStatus.Completed, operation.Wait());
        Assert.Equal(1, callCount);
        Assert.Equal(0, completedCallCount);
        Assert.Equal(DispatcherOperationStatus.Completed, operation.Status);
        Assert.Null(operation.Result);
    }
}
