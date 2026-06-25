// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows;

namespace System.Collections.Specialized.Tests;

public class CollectionChangedEventManagerTests
{
    public static IEnumerable<object?[]> AddHandler_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset) };
    }

    [Theory]
    [MemberData(nameof(AddHandler_TestData))]
    public void AddHandler_InvokeWithHandler_CallsCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        var source = new CustomNotifyCollectionChanged();
        int callCount1 = 0;
        var listener1 = new CustomWeakEventListener();
        listener1.HandlerAction += (actualSender, actualE) =>
        {
            Assert.Same(source, actualSender);
            Assert.Same(e, actualE);
            callCount1++;
        };
        EventHandler<NotifyCollectionChangedEventArgs> handler1 = (EventHandler<NotifyCollectionChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<NotifyCollectionChangedEventArgs>), listener1, nameof(CustomWeakEventListener.Handler));
        int callCount2 = 0;
        var listener2 = new CustomWeakEventListener();
        listener2.HandlerAction += (actualSender, actualE) =>
        {
            Assert.Same(source, actualSender);
            Assert.Same(e, actualE);
            callCount2++;
        };
        EventHandler<NotifyCollectionChangedEventArgs> handler2 = (EventHandler<NotifyCollectionChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<NotifyCollectionChangedEventArgs>), listener2, nameof(CustomWeakEventListener.Handler));

        // Add.
        CollectionChangedEventManager.AddHandler(source, handler1);

        // Call.
        source.OnCollectionChanged(source, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Call invalid source.
        source.OnCollectionChanged(listener1, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        
        source.OnCollectionChanged(new object(), e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        
        source.OnCollectionChanged(null!, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Add again.
        CollectionChangedEventManager.AddHandler(source, handler1);
        source.OnCollectionChanged(source, e);
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);

        // Add another.
        CollectionChangedEventManager.AddHandler(source, handler2);
        source.OnCollectionChanged(source, e);
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);

        // Remove second.
        CollectionChangedEventManager.RemoveHandler(source, handler2);
        source.OnCollectionChanged(source, e);
        Assert.Equal(7, callCount1);
        Assert.Equal(1, callCount2);

        // Remove first.
        CollectionChangedEventManager.RemoveHandler(source, handler1);
        source.OnCollectionChanged(source, e);
        Assert.Equal(8, callCount1);
        Assert.Equal(1, callCount2);

        // Remove again.
        CollectionChangedEventManager.RemoveHandler(source, handler1);
        source.OnCollectionChanged(source, e);
        Assert.Equal(8, callCount1);
        Assert.Equal(1, callCount2);
    }

    [Fact]
    public void AddHandler_InvokeMultipleTimes_Success()
    {
        var source1 = new CustomNotifyCollectionChanged();
        var source2 = new CustomNotifyCollectionChanged();
        var target1 = new CustomWeakEventListener();
        int callCount1 = 0;
        target1.HandlerAction += (actualSender, actualE) => callCount1++;
        EventHandler<NotifyCollectionChangedEventArgs> handler1 = (EventHandler<NotifyCollectionChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<NotifyCollectionChangedEventArgs>), target1, nameof(CustomWeakEventListener.Handler));
        var target2 = new CustomWeakEventListener();
        int callCount2 = 0;
        target2.HandlerAction += (actualSender, actualE) => callCount2++;
        EventHandler<NotifyCollectionChangedEventArgs> handler2 = (EventHandler<NotifyCollectionChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<NotifyCollectionChangedEventArgs>), target2, nameof(CustomWeakEventListener.Handler));
        var target3 = new CustomWeakEventListener();
        int callCount3 = 0;
        target3.HandlerAction += (actualSender, actualE) => callCount3++;
        EventHandler<NotifyCollectionChangedEventArgs> handler3 = (EventHandler<NotifyCollectionChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<NotifyCollectionChangedEventArgs>), target3, nameof(CustomWeakEventListener.Handler));

        // Add.
        CollectionChangedEventManager.AddHandler(source1, handler1);
        source1.OnCollectionChanged(source1, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCollectionChanged(source2, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);

        // Add again.
        CollectionChangedEventManager.AddHandler(source1, handler1);
        source1.OnCollectionChanged(source1, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCollectionChanged(source2, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);

        // Add another handler.
        CollectionChangedEventManager.AddHandler(source1, handler2);
        source1.OnCollectionChanged(source1, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCollectionChanged(source2, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);
        Assert.Equal(0, callCount3);

        // Add another source.
        CollectionChangedEventManager.AddHandler(source2, handler3);
        source1.OnCollectionChanged(source1, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(7, callCount1);
        Assert.Equal(2, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCollectionChanged(source2, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(7, callCount1);
        Assert.Equal(2, callCount2);
        Assert.Equal(1, callCount3);
    }

    // [Fact]
    // public void AddHandler_InvokeNoSource_Success()
    // {
    //     var target1 = new CustomWeakEventListener();
    //     EventHandler<NotifyCollectionChangedEventArgs> handler1 = (EventHandler<NotifyCollectionChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<NotifyCollectionChangedEventArgs>), target1, nameof(CustomWeakEventListener.Handler));
    //     var target2 = new CustomWeakEventListener();
    //     EventHandler<NotifyCollectionChangedEventArgs> handler2 = (EventHandler<NotifyCollectionChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<NotifyCollectionChangedEventArgs>), target2, nameof(CustomWeakEventListener.Handler));

    //     // Add.
    //     Assert.Throws<NullReferenceException>(() => CollectionChangedEventManager.AddHandler(null, handler1));

    //     // Add again.
    //     CollectionChangedEventManager.AddHandler(null, handler1);

    //     // Add another.
    //     CollectionChangedEventManager.AddHandler(null, handler2);
    // }

    [Fact]
    public void AddHandler_NullHandler_ThrowsArgumentNullException()
    {
        var source = new CustomNotifyCollectionChanged();
        Assert.Throws<ArgumentNullException>("handler", () => CollectionChangedEventManager.AddHandler(null, null));
        Assert.Throws<ArgumentNullException>("handler", () => CollectionChangedEventManager.AddHandler(source, null));
    }

    public static IEnumerable<object?[]> AddListener_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset) };
    }

    [Theory]
    [MemberData(nameof(AddListener_TestData))]
    public void AddListener_InvokeWithHandler_CallsCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        var source = new CustomNotifyCollectionChanged();
        int callCount1 = 0;
        var listener1 = new CustomWeakEventListener();
        listener1.ReceiveWeakEventAction += (actualManagerType, actualSender, actualE) =>
        {
            Assert.Equal(typeof(CollectionChangedEventManager), actualManagerType);
            Assert.Same(source, actualSender);
            Assert.Same(e, actualE);
            callCount1++;
            return true;
        };
        int callCount2 = 0;
        var listener2 = new CustomWeakEventListener();
        listener2.ReceiveWeakEventAction += (actualManagerType, actualSender, actualE) =>
        {
            Assert.Equal(typeof(CollectionChangedEventManager), actualManagerType);
            Assert.Same(source, actualSender);
            Assert.Same(e, actualE);
            callCount2++;
            return true;
        };

        // Add.
        CollectionChangedEventManager.AddListener(source, listener1);

        // Call.
        source.OnCollectionChanged(source, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Call invalid source.
        source.OnCollectionChanged(listener1, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        
        source.OnCollectionChanged(new object(), e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        
        source.OnCollectionChanged(null!, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Add again.
        CollectionChangedEventManager.AddListener(source, listener1);
        source.OnCollectionChanged(source, e);
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);

        // Add another.
        CollectionChangedEventManager.AddListener(source, listener2);
        source.OnCollectionChanged(source, e);
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);

        // Remove second.
        CollectionChangedEventManager.RemoveListener(source, listener2);
        source.OnCollectionChanged(source, e);
        Assert.Equal(7, callCount1);
        Assert.Equal(1, callCount2);

        // Remove first.
        CollectionChangedEventManager.RemoveListener(source, listener1);
        source.OnCollectionChanged(source, e);
        Assert.Equal(8, callCount1);
        Assert.Equal(1, callCount2);

        // Remove again.
        CollectionChangedEventManager.RemoveListener(source, listener1);
        source.OnCollectionChanged(source, e);
        Assert.Equal(8, callCount1);
        Assert.Equal(1, callCount2);
    }

    [Fact]
    public void AddListener_InvokeMultipleTimes_Success()
    {
        var source1 = new CustomNotifyCollectionChanged();
        var source2 = new CustomNotifyCollectionChanged();
        var listener1 = new CustomWeakEventListener();
        int callCount1 = 0;
        listener1.ReceiveWeakEventAction += (t, s, e) =>
        {
            callCount1++;
            return true;
        };
        var listener2 = new CustomWeakEventListener();
        int callCount2 = 0;
        listener2.ReceiveWeakEventAction += (t, s, e) =>
        {
            callCount2++;
            return true;
        };
        var listener3 = new CustomWeakEventListener();
        int callCount3 = 0;
        listener3.ReceiveWeakEventAction += (t, s, e) =>
        {
            callCount3++;
            return true;
        };

        // Add.
        CollectionChangedEventManager.AddListener(source1, listener1);
        source1.OnCollectionChanged(source1, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCollectionChanged(source2, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);

        // Add again.
        CollectionChangedEventManager.AddListener(source1, listener1);
        source1.OnCollectionChanged(source1, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCollectionChanged(source2, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);

        // Add another listener.
        CollectionChangedEventManager.AddListener(source1, listener2);
        source1.OnCollectionChanged(source1, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCollectionChanged(source2, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);
        Assert.Equal(0, callCount3);

        // Add another source.
        CollectionChangedEventManager.AddListener(source2, listener3);
        source1.OnCollectionChanged(source1, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(7, callCount1);
        Assert.Equal(2, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCollectionChanged(source2, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(7, callCount1);
        Assert.Equal(2, callCount2);
        Assert.Equal(1, callCount3);
    }

    [Fact]
    public void AddListener_NullSource_ThrowsArgumentNullException()
    {
        var listener = new CustomWeakEventListener();
        Assert.Throws<ArgumentNullException>("source", () => CollectionChangedEventManager.AddListener(null, listener));
    }

    [Fact]
    public void AddListener_NullListener_ThrowsArgumentNullException()
    {
        var source = new CustomNotifyCollectionChanged();
        Assert.Throws<ArgumentNullException>("listener", () => CollectionChangedEventManager.AddListener(source, null));
    }

    [Fact]
    public void RemoveHandler_Invoke_Success()
    {
        var source = new CustomNotifyCollectionChanged();
        var target = new CustomWeakEventListener();
        int callCount = 0;
        target.HandlerAction += (actualSender, actualE) => callCount++;
        EventHandler<NotifyCollectionChangedEventArgs> handler = (EventHandler<NotifyCollectionChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<NotifyCollectionChangedEventArgs>), target, nameof(CustomWeakEventListener.Handler));
        CollectionChangedEventManager.AddHandler(source, handler);

        // Remove.
        CollectionChangedEventManager.RemoveHandler(source, handler);
        source.OnCollectionChanged(source, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(0, callCount);

        // Remove again.
        CollectionChangedEventManager.RemoveHandler(source, handler);
        source.OnCollectionChanged(source, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(0, callCount);
    }

    // [Fact]
    // public void RemoveHandler_InvokeNoSource_Success()
    // {
    //     var target = new CustomWeakEventListener();
    //     EventHandler<NotifyCollectionChangedEventArgs> handler = (EventHandler<NotifyCollectionChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<NotifyCollectionChangedEventArgs>), target, nameof(CustomWeakEventListener.Handler));
    //     CollectionChangedEventManager.AddHandler(null, handler);

    //     // Remove.
    //     CollectionChangedEventManager.RemoveHandler(null, handler);

    //     // Remove again.
    //     CollectionChangedEventManager.RemoveHandler(null, handler);
    // }

    [Fact]
    public void RemoveHandler_InvokeNoSuchSource_Nop()
    {
        var source1 = new CustomNotifyCollectionChanged();
        var source2 = new CustomNotifyCollectionChanged();
        var target = new CustomWeakEventListener();
        int callCount = 0;
        target.HandlerAction += (actualSender, actualE) => callCount++;
        EventHandler<NotifyCollectionChangedEventArgs> handler = (EventHandler<NotifyCollectionChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<NotifyCollectionChangedEventArgs>), target, nameof(CustomWeakEventListener.Handler));
        CollectionChangedEventManager.AddHandler(source1, handler);

        // Remove.
        CollectionChangedEventManager.RemoveHandler(source2, handler);
        source1.OnCollectionChanged(source1, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(1, callCount);
        source2.OnCollectionChanged(source2, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(1, callCount);

        // Remove again.
        CollectionChangedEventManager.RemoveHandler(source2, handler);
        source1.OnCollectionChanged(source1, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(2, callCount);
        source2.OnCollectionChanged(source2, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void RemoveHandler_InvokeNoSuchHandler_Nop()
    {
        var source = new CustomNotifyCollectionChanged();
        var target1 = new CustomWeakEventListener();
        int callCount1 = 0;
        target1.HandlerAction += (actualSender, actualE) => callCount1++;
        EventHandler<NotifyCollectionChangedEventArgs> handler1 = (EventHandler<NotifyCollectionChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<NotifyCollectionChangedEventArgs>), target1, nameof(CustomWeakEventListener.Handler));
        var target2 = new CustomWeakEventListener();
        int callCount2 = 0;
        target2.HandlerAction += (actualSender, actualE) => callCount2++;
        EventHandler<NotifyCollectionChangedEventArgs> handler2 = (EventHandler<NotifyCollectionChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<NotifyCollectionChangedEventArgs>), target2, nameof(CustomWeakEventListener.Handler));
        CollectionChangedEventManager.AddHandler(source, handler1);

        // Remove.
        CollectionChangedEventManager.RemoveHandler(source, handler2);
        source.OnCollectionChanged(source, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Remove again.
        CollectionChangedEventManager.RemoveHandler(source, handler2);
        source.OnCollectionChanged(source, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(2, callCount1);
        Assert.Equal(0, callCount2);
    }

    [Fact]
    public void RemoveHandler_NullHandler_ThrowsArgumentNullException()
    {
        var source = new CustomNotifyCollectionChanged();
        Assert.Throws<ArgumentNullException>("handler", () => CollectionChangedEventManager.RemoveHandler(null, null));
        Assert.Throws<ArgumentNullException>("handler", () => CollectionChangedEventManager.RemoveHandler(source, null));
    }

    [Fact]
    public void RemoveListener_Invoke_Success()
    {
        var source = new CustomNotifyCollectionChanged();
        var listener = new CustomWeakEventListener();
        int callCount = 0;
        listener.ReceiveWeakEventAction += (t, s, e) =>
        {
            callCount++;
            return true;
        };
        CollectionChangedEventManager.AddListener(source, listener);

        // Remove.
        CollectionChangedEventManager.RemoveListener(source, listener);
        source.OnCollectionChanged(source, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(0, callCount);

        // Remove again.
        CollectionChangedEventManager.RemoveListener(source, listener);
        source.OnCollectionChanged(source, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void RemoveListener_InvokeNoSuchSource_Nop()
    {
        var source1 = new CustomNotifyCollectionChanged();
        var source2 = new CustomNotifyCollectionChanged();
        var listener = new CustomWeakEventListener();
        int callCount = 0;
        listener.ReceiveWeakEventAction += (t, s, e) =>
        {
            callCount++;
            return true;
        };
        CollectionChangedEventManager.AddListener(source1, listener);

        // Remove.
        CollectionChangedEventManager.RemoveListener(source2, listener);
        source1.OnCollectionChanged(source1, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(1, callCount);
        source2.OnCollectionChanged(source2, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(1, callCount);

        // Remove again.
        CollectionChangedEventManager.RemoveListener(source2, listener);
        source1.OnCollectionChanged(source1, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(2, callCount);
        source2.OnCollectionChanged(source2, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void RemoveListener_InvokeNoSuchListener_Nop()
    {
        var source = new CustomNotifyCollectionChanged();
        var listener1 = new CustomWeakEventListener();
        int callCount1 = 0;
        listener1.ReceiveWeakEventAction += (t, s, e) =>
        {
            callCount1++;
            return true;
        };
        var listener2 = new CustomWeakEventListener();
        int callCount2 = 0;
        listener2.ReceiveWeakEventAction += (t, s, e) =>
        {
            callCount2++;
            return true;
        };
        CollectionChangedEventManager.AddListener(source, listener1);

        // Remove.
        CollectionChangedEventManager.RemoveListener(source, listener2);
        source.OnCollectionChanged(source, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Remove again.
        CollectionChangedEventManager.RemoveListener(source, listener2);
        source.OnCollectionChanged(source, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        Assert.Equal(2, callCount1);
        Assert.Equal(0, callCount2);
    }

    [Fact]
    public void RemoveListener_NullListener_ThrowsArgumentNullException()
    {
        var source = new CustomNotifyCollectionChanged();
        Assert.Throws<ArgumentNullException>("listener", () => CollectionChangedEventManager.RemoveListener(source, null));
    }

    private class CustomNotifyCollectionChanged : INotifyCollectionChanged
    {
        #pragma warning disable CS0067
        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        #pragma warning restore CS0067

        public void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(sender, e);
        }
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

        public Action<object, NotifyCollectionChangedEventArgs>? HandlerAction { get; set; }

        public void Handler(object sender, NotifyCollectionChangedEventArgs e)
        {
            HandlerAction?.Invoke(sender, e);
        }
    }
}
