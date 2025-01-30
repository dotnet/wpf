// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Windows;

namespace System.ComponentModel.Tests;

public class ErrorsChangedEventManagerTests
{
    public static IEnumerable<object?[]> AddHandler_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { new DataErrorsChangedEventArgs(null) };
        yield return new object?[] { new DataErrorsChangedEventArgs("") };
        yield return new object?[] { new DataErrorsChangedEventArgs("propertyName") };
    }

    [Theory]
    [MemberData(nameof(AddHandler_TestData))]
    public void AddHandler_InvokeWithHandler_CallsErrorsChanged(DataErrorsChangedEventArgs e)
    {
        var source = new CustomNotifyDataErrorInfo();
        int callCount1 = 0;
        var listener1 = new CustomWeakEventListener();
        listener1.HandlerAction += (actualSender, actualE) =>
        {
            Assert.Same(source, actualSender);
            Assert.Same(e, actualE);
            callCount1++;
        };
        EventHandler<DataErrorsChangedEventArgs> handler1 = (EventHandler<DataErrorsChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<DataErrorsChangedEventArgs>), listener1, nameof(CustomWeakEventListener.Handler));
        int callCount2 = 0;
        var listener2 = new CustomWeakEventListener();
        listener2.HandlerAction += (actualSender, actualE) =>
        {
            Assert.Same(source, actualSender);
            Assert.Same(e, actualE);
            callCount2++;
        };
        EventHandler<DataErrorsChangedEventArgs> handler2 = (EventHandler<DataErrorsChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<DataErrorsChangedEventArgs>), listener2, nameof(CustomWeakEventListener.Handler));

        // Add.
        ErrorsChangedEventManager.AddHandler(source, handler1);

        // Call.
        source.OnErrorsChanged(source, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Call invalid source.
        source.OnErrorsChanged(listener1, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        
        source.OnErrorsChanged(new object(), e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        
        source.OnErrorsChanged(null!, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Add again.
        ErrorsChangedEventManager.AddHandler(source, handler1);
        source.OnErrorsChanged(source, e);
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);

        // Add another.
        ErrorsChangedEventManager.AddHandler(source, handler2);
        source.OnErrorsChanged(source, e);
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);

        // Remove second.
        ErrorsChangedEventManager.RemoveHandler(source, handler2);
        source.OnErrorsChanged(source, e);
        Assert.Equal(7, callCount1);
        Assert.Equal(1, callCount2);

        // Remove first.
        ErrorsChangedEventManager.RemoveHandler(source, handler1);
        source.OnErrorsChanged(source, e);
        Assert.Equal(8, callCount1);
        Assert.Equal(1, callCount2);

        // Remove again.
        ErrorsChangedEventManager.RemoveHandler(source, handler1);
        source.OnErrorsChanged(source, e);
        Assert.Equal(8, callCount1);
        Assert.Equal(1, callCount2);
    }

    [Fact]
    public void AddHandler_InvokeMultipleTimes_Success()
    {
        var source1 = new CustomNotifyDataErrorInfo();
        var source2 = new CustomNotifyDataErrorInfo();
        var target1 = new CustomWeakEventListener();
        int callCount1 = 0;
        target1.HandlerAction += (sender, e) => callCount1++;
        EventHandler<DataErrorsChangedEventArgs> handler1 = (EventHandler<DataErrorsChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<DataErrorsChangedEventArgs>), target1, nameof(CustomWeakEventListener.Handler));
        var target2 = new CustomWeakEventListener();
        int callCount2 = 0;
        target2.HandlerAction += (sender, e) => callCount2++;
        EventHandler<DataErrorsChangedEventArgs> handler2 = (EventHandler<DataErrorsChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<DataErrorsChangedEventArgs>), target2, nameof(CustomWeakEventListener.Handler));
        var target3 = new CustomWeakEventListener();
        int callCount3 = 0;
        target3.HandlerAction += (sender, e) => callCount3++;
        EventHandler<DataErrorsChangedEventArgs> handler3 = (EventHandler<DataErrorsChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<DataErrorsChangedEventArgs>), target3, nameof(CustomWeakEventListener.Handler));

        // Add.
        ErrorsChangedEventManager.AddHandler(source1, handler1);
        source1.OnErrorsChanged(source1, new DataErrorsChangedEventArgs(null));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnErrorsChanged(source2, new DataErrorsChangedEventArgs(null));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);

        // Add again.
        ErrorsChangedEventManager.AddHandler(source1, handler1);
        source1.OnErrorsChanged(source1, new DataErrorsChangedEventArgs(null));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnErrorsChanged(source2, new DataErrorsChangedEventArgs(null));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);

        // Add another handler.
        ErrorsChangedEventManager.AddHandler(source1, handler2);
        source1.OnErrorsChanged(source1, new DataErrorsChangedEventArgs(null));
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnErrorsChanged(source2, new DataErrorsChangedEventArgs(null));
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);
        Assert.Equal(0, callCount3);

        // Add another source.
        ErrorsChangedEventManager.AddHandler(source2, handler3);
        source1.OnErrorsChanged(source1, new DataErrorsChangedEventArgs(null));
        Assert.Equal(7, callCount1);
        Assert.Equal(2, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnErrorsChanged(source2, new DataErrorsChangedEventArgs(null));
        Assert.Equal(7, callCount1);
        Assert.Equal(2, callCount2);
        Assert.Equal(1, callCount3);
    }

    [Fact]
    public void AddHandler_NullSource_ThrowsArgumentNullException()
    {
        var target = new CustomWeakEventListener();
        EventHandler<DataErrorsChangedEventArgs> handler = (EventHandler<DataErrorsChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<DataErrorsChangedEventArgs>), target, nameof(CustomWeakEventListener.Handler));
        Assert.Throws<ArgumentNullException>("source", () => ErrorsChangedEventManager.AddHandler(null, handler));
    }

    [Fact]
    public void AddHandler_NullHandler_ThrowsArgumentNullException()
    {
        var source = new CustomNotifyDataErrorInfo();
        Assert.Throws<ArgumentNullException>("handler", () => ErrorsChangedEventManager.AddHandler(source, null));
    }

    [Fact]
    public void RemoveHandler_Invoke_Success()
    {
        var source = new CustomNotifyDataErrorInfo();
        var target = new CustomWeakEventListener();
        int callCount = 0;
        target.HandlerAction += (sender, e) => callCount++;
        EventHandler<DataErrorsChangedEventArgs> handler = (EventHandler<DataErrorsChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<DataErrorsChangedEventArgs>), target, nameof(CustomWeakEventListener.Handler));
        ErrorsChangedEventManager.AddHandler(source, handler);

        // Remove.
        ErrorsChangedEventManager.RemoveHandler(source, handler);
        source.OnErrorsChanged(source, new DataErrorsChangedEventArgs(null));
        Assert.Equal(0, callCount);

        // Remove again.
        ErrorsChangedEventManager.RemoveHandler(source, handler);
        source.OnErrorsChanged(source, new DataErrorsChangedEventArgs(null));
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void RemoveHandler_InvokeNoSuchSource_Nop()
    {
        var source1 = new CustomNotifyDataErrorInfo();
        var source2 = new CustomNotifyDataErrorInfo();
        var target = new CustomWeakEventListener();
        int callCount = 0;
        target.HandlerAction += (sender, e) => callCount++;
        EventHandler<DataErrorsChangedEventArgs> handler = (EventHandler<DataErrorsChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<DataErrorsChangedEventArgs>), target, nameof(CustomWeakEventListener.Handler));
        ErrorsChangedEventManager.AddHandler(source1, handler);

        // Remove.
        ErrorsChangedEventManager.RemoveHandler(source2, handler);
        source1.OnErrorsChanged(source1, new DataErrorsChangedEventArgs(null));
        Assert.Equal(1, callCount);
        source2.OnErrorsChanged(source1, new DataErrorsChangedEventArgs(null));
        Assert.Equal(1, callCount);

        // Remove again.
        ErrorsChangedEventManager.RemoveHandler(source2, handler);
        source1.OnErrorsChanged(source1, new DataErrorsChangedEventArgs(null));
        Assert.Equal(2, callCount);
        source2.OnErrorsChanged(source1, new DataErrorsChangedEventArgs(null));
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void RemoveHandler_InvokeNoSuchHandler_Nop()
    {
        var source = new CustomNotifyDataErrorInfo();
        var target1 = new CustomWeakEventListener();
        int callCount = 0;
        target1.HandlerAction += (sender, e) => callCount++;
        EventHandler<DataErrorsChangedEventArgs> handler1 = (EventHandler<DataErrorsChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<DataErrorsChangedEventArgs>), target1, nameof(CustomWeakEventListener.Handler));
        var target2 = new CustomWeakEventListener();
        EventHandler<DataErrorsChangedEventArgs> handler2 = (EventHandler<DataErrorsChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<DataErrorsChangedEventArgs>), target2, nameof(CustomWeakEventListener.Handler));
        ErrorsChangedEventManager.AddHandler(source, handler1);

        // Remove.
        ErrorsChangedEventManager.RemoveHandler(source, handler2);
        source.OnErrorsChanged(source, new DataErrorsChangedEventArgs(null));
        Assert.Equal(1, callCount);

        // Remove again.
        ErrorsChangedEventManager.RemoveHandler(source, handler2);
        source.OnErrorsChanged(source, new DataErrorsChangedEventArgs(null));
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void RemoveHandler_NullSource_ThrowsArgumentNullException()
    {
        var target = new CustomWeakEventListener();
        EventHandler<DataErrorsChangedEventArgs> handler = (EventHandler<DataErrorsChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<DataErrorsChangedEventArgs>), target, nameof(CustomWeakEventListener.Handler));
        Assert.Throws<ArgumentNullException>("source", () => ErrorsChangedEventManager.RemoveHandler(null, handler));
    }

    [Fact]
    public void RemoveHandler_NullHandler_ThrowsArgumentNullException()
    {
        var source = new CustomNotifyDataErrorInfo();
        Assert.Throws<ArgumentNullException>("handler", () => ErrorsChangedEventManager.RemoveHandler(source, null));
    }

    private class CustomNotifyDataErrorInfo : INotifyDataErrorInfo
    {
        public bool HasErrors => throw new NotImplementedException();

        #pragma warning disable CS0067
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        #pragma warning restore CS0067

        public IEnumerable GetErrors(string? propertyName) => throw new NotImplementedException();

        public void OnErrorsChanged(object sender, DataErrorsChangedEventArgs e) => ErrorsChanged?.Invoke(sender, e);
    }

    private class CustomWeakEventListener : IWeakEventListener
    {
        public Func<Type, object, EventArgs, bool>? ReceiveWeakEventAction { get; set; }

        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (ReceiveWeakEventAction is null)
            {
                return true;
            }

            return ReceiveWeakEventAction(managerType, sender, e);
        }

        public Action<object, DataErrorsChangedEventArgs>? HandlerAction { get; set; }

        public void Handler(object sender, DataErrorsChangedEventArgs e)
        {
            HandlerAction?.Invoke(sender, e);
        }
    }
}
