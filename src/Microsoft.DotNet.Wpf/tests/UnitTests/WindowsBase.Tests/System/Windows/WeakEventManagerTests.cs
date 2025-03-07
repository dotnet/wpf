// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Windows.Threading;

namespace System.Windows.Tests;

public class WeakEventManagerTests : WeakEventManager
{
    // TODO:
    // Read/Write lock using RemoteExecutor

    [Fact]
    public void Ctor_Default()
    {
        var manager = new SubWeakEventManager();
        Assert.NotNull(manager.Dispatcher);
        Assert.Same(manager.Dispatcher, manager.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, manager.Dispatcher);
        IDisposable readLock = manager.ReadLock;
        try
        {
            Assert.NotNull(readLock);
        }
        finally
        {
            readLock.Dispose();
        }

        IDisposable writeLock = manager.WriteLock;
        try
        {
            Assert.NotNull(writeLock);
        }
        finally
        {
            writeLock.Dispose();
        }
    }

    [Fact]
    public void Item_Get_ReturnsExpected()
    {
        var manager = new SubWeakEventManager
        {
            StartListeningAction = (source) => { }
        };

        var source = new object();
        var listener = new CustomWeakEventListener();
        manager.ProtectedAddListener(source, listener);

        // Get.
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener, list[0]);
    }

    public static IEnumerable<object?[]> Item_GetNoSuchSource_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { new object() };
        yield return new object?[] { 1 };
    }

    [Theory]
    [MemberData(nameof(Item_GetNoSuchSource_TestData))]
    public void Item_GetNoSuchSourceNotEmpty_ReturnsExpected(object source)
    {
        var manager = new SubWeakEventManager
        {
            StartListeningAction = (source) => { }
        };

        manager.ProtectedAddListener(new object(), new CustomWeakEventListener());
        Assert.Null(manager[source]);
    }

    [Theory]
    [MemberData(nameof(Item_GetNoSuchSource_TestData))]
    public void Item_GetNoSuchSourceEmpty_ReturnsExpected(object source)
    {
        var manager = new SubWeakEventManager();
        Assert.Null(manager[source]);
    }

    public static IEnumerable<object?[]> Item_Set_TestData()
    {
        yield return new object?[] { new object(), null };
        yield return new object?[] { new object(), new object() };
        yield return new object?[] { new object(), 1 };
        yield return new object?[] { 1, null };
        yield return new object?[] { 1, new object() };
        yield return new object?[] { 1, 1 };
    }

    [Theory]
    [MemberData(nameof(Item_Set_TestData))]
    public void Item_Set_GetReturnsExpected(object source, object value)
    {
        var manager = new SubWeakEventManager();

        // Set.
        manager[source] = value;
        Assert.Equal(value, manager[source]);

        // Set same.
        manager[source] = value;
        Assert.Equal(value, manager[source]);
    }

    public static IEnumerable<object?[]> Item_SetNullSource_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { new object() };
        yield return new object?[] { 1 };
    }

    [Theory]
    [MemberData(nameof(Item_SetNullSource_TestData))]
    public void Item_SetNullSource_GetReturnsNull(object value)
    {
        var manager = new SubWeakEventManager();
        object source = null!;

        // Set.
        manager[source] = value;
        Assert.Null(manager[source]);

        // Set same.
        manager[source] = value;
        Assert.Null(manager[source]);
    }

    public static IEnumerable<object?[]> DeliverEvent_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { new EventArgs() };
        yield return new object?[] { EventArgs.Empty };
    }

    [Theory]
    [MemberData(nameof(DeliverEvent_TestData))]
    public void DeliverEvent_InvokeWithListeners_Success(EventArgs args)
    {
        var manager = new SubWeakEventManager
        {
            StartListeningAction = (source) => { }
        };
        var source = new object();
        var events = new List<string>();
        var listener1 = new CustomWeakEventListener
        {
            ReceiveWeakEventAction = (t, s, e) =>
        {
            Assert.Same(typeof(SubWeakEventManager), t);
            Assert.Same(source, s);
            Assert.Same(args, e);
            events.Add("listener1");
            return true;
        }
        };
        var listener2 = new CustomWeakEventListener
        {
            ReceiveWeakEventAction = (t, s, e) =>
        {
            Assert.Same(typeof(SubWeakEventManager), t);
            Assert.Same(source, s);
            Assert.Same(args, e);
            events.Add("listener2");
            return true;
        }
        };
        manager.ProtectedAddListener(source, listener1);
        manager.ProtectedAddListener(source, listener2);

        manager.DeliverEvent(source, args);
        Assert.Equal(new string[] { "listener1", "listener2" }, events);
    }

    [Theory]
    [MemberData(nameof(DeliverEvent_TestData))]
    public void DeliverEvent_InvokeWithHandlers_Success(EventArgs args)
    {
        var manager = new SubWeakEventManager
        {
            StartListeningAction = (source) => { }
        };
        var source = new object();
        var events = new List<string>();
        var listener1 = new CustomWeakEventListener
        {
            HandlerAction = (s, e) =>
        {
            Assert.Same(source, s);
            Assert.Same(args, e);
            events.Add("handler1");
        }
        };
        var listener2 = new CustomWeakEventListener
        {
            HandlerAction = (s, e) =>
        {
            Assert.Same(source, s);
            Assert.Same(args, e);
            events.Add("handler2");
        }
        };
        Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), listener1, nameof(CustomWeakEventListener.Handler));
        Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), listener2, nameof(CustomWeakEventListener.Handler));

        manager.ProtectedAddHandler(source, handler1);
        manager.ProtectedAddHandler(source, handler2);

        manager.DeliverEvent(source, args);
        Assert.Equal(new string[] { "handler1", "handler2" }, events);
    }

    [Theory]
    [MemberData(nameof(DeliverEvent_TestData))]
    public void DeliverEvent_InvokeWithListenersAndHandlers_Success(EventArgs args)
    {
        var manager = new SubWeakEventManager
        {
            StartListeningAction = (source) => { }
        };
        var source = new object();
        var events = new List<string>();
        var listener1 = new CustomWeakEventListener
        {
            HandlerAction = (s, e) =>
        {
            Assert.Same(source, s);
            Assert.Same(args, e);
            events.Add("handler1");
        }
        };
        var listener2 = new CustomWeakEventListener
        {
            ReceiveWeakEventAction = (t, s, e) =>
        {
            Assert.Same(typeof(SubWeakEventManager), t);
            Assert.Same(source, s);
            Assert.Same(args, e);
            events.Add("listener1");
            return true;
        }
        };
        var listener3 = new CustomWeakEventListener
        {
            HandlerAction = (s, e) =>
        {
            Assert.Same(source, s);
            Assert.Same(args, e);
            events.Add("handler2");
        }
        };
        var listener4 = new CustomWeakEventListener
        {
            ReceiveWeakEventAction = (t, s, e) =>
        {
            Assert.Same(typeof(SubWeakEventManager), t);
            Assert.Same(source, s);
            Assert.Same(args, e);
            events.Add("listener2");
            return true;
        }
        };
        Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), listener1, nameof(CustomWeakEventListener.Handler));
        Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), listener3, nameof(CustomWeakEventListener.Handler));
        manager.ProtectedAddHandler(source, handler1);
        manager.ProtectedAddListener(source, listener2);
        manager.ProtectedAddHandler(source, handler2);
        manager.ProtectedAddListener(source, listener4);

        manager.DeliverEvent(source, args);
        Assert.Equal(new string[] { "handler1", "listener1", "handler2", "listener2" }, events);
    }

    [Theory]
    [MemberData(nameof(DeliverEvent_TestData))]
    public void DeliverEvent_InvokeMultipleSources_Success(EventArgs args)
    {
        var manager = new SubWeakEventManager
        {
            StartListeningAction = (source) => { }
        };
        var source1 = new object();
        var source2 = new object();
        var events = new List<string>();
        var listener1 = new CustomWeakEventListener
        {
            ReceiveWeakEventAction = (t, s, e) =>
        {
            Assert.Same(typeof(SubWeakEventManager), t);
            Assert.Same(source1, s);
            Assert.Same(args, e);
            events.Add("listener1");
            return true;
        }
        };
        var listener2 = new CustomWeakEventListener
        {
            ReceiveWeakEventAction = (t, s, e) =>
        {
            Assert.Same(typeof(SubWeakEventManager), t);
            Assert.Same(source2, s);
            Assert.Same(args, e);
            events.Add("listener2");
            return true;
        }
        };
        manager.ProtectedAddListener(source1, listener1);
        manager.ProtectedAddListener(source2, listener2);

        manager.DeliverEvent(source2, args);
        Assert.Equal(new string[] { "listener2" }, events);
        
        manager.DeliverEvent(source1, args);
        Assert.Equal(new string[] { "listener2", "listener1" }, events);
    }

    [Theory]
    [MemberData(nameof(DeliverEvent_TestData))]
    public void DeliverEvent_InvokeWithInvalidHandlerAction_ThrowsInvalidCastException(EventArgs args)
    {
        var manager = new SubWeakEventManager
        {
            StartListeningAction = (source) => { }
        };
        var source = new object();
        Delegate handler = () => {};
        manager.ProtectedAddHandler(source, handler);

        Assert.Throws<InvalidCastException>(() => manager.DeliverEvent(source, args));
    }

    [Theory]
    [MemberData(nameof(DeliverEvent_TestData))]
    public void DeliverEvent_InvokeWithInvalidHandlerFunc_ThrowsInvalidCastException(EventArgs args)
    {
        var manager = new SubWeakEventManager
        {
            StartListeningAction = (source) => { }
        };
        var source = new object();
        static bool EventHandler(object sender, EventArgs args) => true;
        manager.ProtectedAddHandler(source, EventHandler);

        Assert.Throws<InvalidCastException>(() => manager.DeliverEvent(source, args));
    }

    [Theory]
    [MemberData(nameof(DeliverEvent_TestData))]
    public void DeliverEvent_InvokeWithInvalidHandlerGenericEventArgs_ThrowsInvalidCastException(EventArgs args)
    {
        var manager = new SubWeakEventManager
        {
            StartListeningAction = (source) => { }
        };
        var source = new object();
        var target = new CustomWeakEventListener();
        Delegate handler = Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), target, nameof(CustomWeakEventListener.Handler));
        manager.ProtectedAddHandler(source, handler);

        Assert.Throws<InvalidCastException>(() => manager.DeliverEvent(source, args));
    }

    [Theory]
    [MemberData(nameof(DeliverEvent_TestData))]
    public void DeliverEvent_InvokeNoSuchSource_Success(EventArgs args)
    {
        var manager = new SubWeakEventManager();
        manager.DeliverEvent(new object(), args);
        manager.DeliverEvent(null!, args);
    }

    [Theory]
    [MemberData(nameof(DeliverEvent_TestData))]
    public void DeliverEvent_InvokeNotEmptyNoSuchSource_Success(EventArgs args)
    {
        var manager = new SubWeakEventManager
        {
            StartListeningAction = (source) => { }
        };
        var source1 = new object();
        var events = new List<string>();
        var listener = new CustomWeakEventListener
        {
            ReceiveWeakEventAction = (t, s, e) =>
        {
            events.Add("listener1");
            return true;
        }
        };
        manager.ProtectedAddListener(source1, listener);

        var source2 = new object();
        manager.DeliverEvent(source2, args);
        Assert.Empty(events);
        
        manager.DeliverEvent(null!, args);
        Assert.Empty(events);
    }

    [Theory]
    [MemberData(nameof(DeliverEvent_TestData))]
    public void DeliverEvent_InvokeEmpty_Success(EventArgs args)
    {
        var manager = new SubWeakEventManager();
        manager.DeliverEvent(new object(), args);
        manager.DeliverEvent(null!, args);
    }

    [Theory]
    [MemberData(nameof(DeliverEvent_TestData))]
    public void DeliverEvent_SourceNotListenerList_ThrowsInvalidCastException(EventArgs args)
    {
        var manager = new SubWeakEventManager();
        var source = new object();
        manager[source] = new object();

        Assert.Throws<InvalidCastException>(() => manager.DeliverEvent(source, args));
    }

    public static IEnumerable<object?[]> DeliverEventToList_TestData()
    {
        yield return new object?[] { null, null };
        yield return new object?[] { new object(), new EventArgs() };
        yield return new object?[] { new object(), EventArgs.Empty };
    }

    [Theory]
    [MemberData(nameof(DeliverEventToList_TestData))]
    public void DeliverEventToList_InvokeWithListeners_Success(object sender, EventArgs args)
    {
        var manager = new SubWeakEventManager();

        var list = new ListenerList();
        var events = new List<string>();
        var listener1 = new CustomWeakEventListener
        {
            ReceiveWeakEventAction = (t, s, e) =>
        {
            Assert.Equal(typeof(SubWeakEventManager), t);
            Assert.Same(sender, s);
            Assert.Same(args, e);
            events.Add("listener1");
            return true;
        }
        };
        var listener2 = new CustomWeakEventListener
        {
            ReceiveWeakEventAction = (t, s, e) =>
        {
            Assert.Equal(typeof(SubWeakEventManager), t);
            Assert.Same(sender, s);
            Assert.Same(args, e);
            events.Add("listener2");
            return true;
        }
        };
        list.Add(listener1);
        list.Add(listener2);

        manager.DeliverEventToList(sender, args, list);
        Assert.Equal(new string[] { "listener1", "listener2" }, events);
    }

    [Theory]
    [MemberData(nameof(DeliverEventToList_TestData))]
    public void DeliverEventToList_InvokeWithHandlers_Success(object sender, EventArgs args)
    {
        var manager = new SubWeakEventManager();

        var list = new ListenerList();
        var events = new List<string>();
        var listener1 = new CustomWeakEventListener
        {
            HandlerAction = (s, e) =>
        {
            Assert.Same(sender, s);
            Assert.Same(args, e);
            events.Add("handler1");
        }
        };
        var listener2 = new CustomWeakEventListener
        {
            HandlerAction = (s, e) =>
        {
            Assert.Same(sender, s);
            Assert.Same(args, e);
            events.Add("handler2");
        }
        };
        Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), listener1, nameof(CustomWeakEventListener.Handler));
        Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), listener2, nameof(CustomWeakEventListener.Handler));
        list.AddHandler(handler1);
        list.AddHandler(handler2);

        manager.DeliverEventToList(sender, args, list);
        Assert.Equal(new string[] { "handler1", "handler2" }, events);
    }

    [Theory]
    [MemberData(nameof(DeliverEventToList_TestData))]
    public void DeliverEventToList_InvokeWithListenersAndHandlers_Success(object sender, EventArgs args)
    {
        var manager = new SubWeakEventManager();

        var list = new ListenerList();
        var events = new List<string>();
        var listener1 = new CustomWeakEventListener
        {
            HandlerAction = (s, e) =>
        {
            Assert.Same(sender, s);
            Assert.Same(args, e);
            events.Add("handler1");
        }
        };
        var listener2 = new CustomWeakEventListener
        {
            ReceiveWeakEventAction = (t, s, e) =>
        {
            Assert.Equal(typeof(SubWeakEventManager), t);
            Assert.Same(sender, s);
            Assert.Same(args, e);
            events.Add("listener1");
            return true;
        }
        };
        var listener3 = new CustomWeakEventListener
        {
            HandlerAction = (s, e) =>
        {
            Assert.Same(sender, s);
            Assert.Same(args, e);
            events.Add("handler2");
        }
        };
        var listener4 = new CustomWeakEventListener
        {
            ReceiveWeakEventAction = (t, s, e) =>
        {
            Assert.Equal(typeof(SubWeakEventManager), t);
            Assert.Same(sender, s);
            Assert.Same(args, e);
            events.Add("listener2");
            return true;
        }
        };
        Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), listener1, nameof(CustomWeakEventListener.Handler));
        Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), listener3, nameof(CustomWeakEventListener.Handler));
        list.AddHandler(handler1);
        list.Add(listener2);
        list.AddHandler(handler2);
        list.Add(listener4);

        manager.DeliverEventToList(sender, args, list);
        Assert.Equal(new string[] { "handler1", "listener1", "handler2", "listener2" }, events);
    }

    // TODO: this causes a crash.
#if false
    [Theory]
    [MemberData(nameof(DeliverEventToList_TestData))]
    public void DeliverEventToList_InvokeWithListenersNotHandled_Success(object sender, EventArgs args)
    {
        var manager = new SubWeakEventManager();

        var list = new ListenerList();
        var events = new List<string>();
        var listener1 = new CustomWeakEventListener();
        listener1.ReceiveWeakEventAction = (t, s, e) =>
        {
            Assert.Equal(typeof(SubWeakEventManager), t);
            Assert.Same(sender, s);
            Assert.Same(args, e);
            events.Add("listener1");
            return false;
        };
        var listener2 = new CustomWeakEventListener();
        listener2.ReceiveWeakEventAction = (t, s, e) =>
        {
            Assert.Equal(typeof(SubWeakEventManager), t);
            Assert.Same(sender, s);
            Assert.Same(args, e);
            events.Add("listener2");
            return true;
        };
        list.Add(listener1);
        list.Add(listener2);

        manager.DeliverEventToList(sender, args, list);
        Assert.Equal(new string[] { "listener1", "listener2" }, events);
    }
#endif

    [Theory]
    [MemberData(nameof(DeliverEventToList_TestData))]
    public void DeliverEventToList_InvokeWithInvalidHandlerAction_ThrowsInvalidCastException(object sender, EventArgs args)
    {
        var manager = new SubWeakEventManager();

        var list = new ListenerList();
        Delegate handler = () => {};
        list.AddHandler(handler);

        Assert.Throws<InvalidCastException>(() => manager.DeliverEventToList(sender, args, list));
    }

    [Theory]
    [MemberData(nameof(DeliverEventToList_TestData))]
    public void DeliverEventToList_InvokeWithInvalidHandlerFunc_ThrowsInvalidCastException(object sender, EventArgs args)
    {
        var manager = new SubWeakEventManager();

        var list = new ListenerList();
        static bool EventHandler(object sender, EventArgs args) => true;
        list.AddHandler(EventHandler);

        Assert.Throws<InvalidCastException>(() => manager.DeliverEventToList(sender, args, list));
    }

    [Theory]
    [MemberData(nameof(DeliverEventToList_TestData))]
    public void DeliverEventToList_InvokeWithInvalidHandlerGenericEventArgs_ThrowsInvalidCastException(object sender, EventArgs args)
    {
        var manager = new SubWeakEventManager();

        var list = new ListenerList();
        var target = new CustomWeakEventListener();
        Delegate handler = Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), target, nameof(CustomWeakEventListener.Handler));
        list.AddHandler(handler);

        Assert.Throws<InvalidCastException>(() => manager.DeliverEventToList(sender, args, list));
    }

    [Theory]
    [MemberData(nameof(DeliverEventToList_TestData))]
    public void DeliverEventToList_InvokeEmpty_Success(object sender, EventArgs args)
    {
        var manager = new SubWeakEventManager();

        var list = new ListenerList();
        manager.DeliverEventToList(sender, args, list);
    }

    public static IEnumerable<object?[]> DeliverEventToList_CustomDeliverEvent_TestData()
    {
        yield return new object?[] { null, null, true };
        yield return new object?[] { null, null, false };
        yield return new object?[] { new object(), new EventArgs(), true };
        yield return new object?[] { new object(), new EventArgs(), false };
        yield return new object?[] { new object(), EventArgs.Empty, true };
        yield return new object?[] { new object(), EventArgs.Empty, false };
    }

    [Theory]
    [MemberData(nameof(DeliverEventToList_CustomDeliverEvent_TestData))]
    public void DeliverEventToList_InvokeCustomDeliverEvent_CallsDeliverEvent(object sender, EventArgs args, bool result)
    {
        var manager = new SubWeakEventManager();
        
        var list = new CustomListenerList();
        var events = new List<string>();
        var listener = new CustomWeakEventListener
        {
            ReceiveWeakEventAction = (t, s, e) =>
        {
            events.Add("listener1");
            return true;
        }
        };
        list.Add(listener);
        int deliverEventCallCount = 0;
        list.DeliverEventAction = (s, a, t) =>
        {
            Assert.Same(sender, s);
            Assert.Same(args, a);
            Assert.Equal(typeof(SubWeakEventManager), t);
            deliverEventCallCount++;
            return result;
        };

        // Call.
        manager.DeliverEventToList(sender, args, list);
        Assert.Equal(1, deliverEventCallCount);
        Assert.Empty(events);

        // Call again.
        manager.DeliverEventToList(sender, args, list);
        Assert.Equal(2, deliverEventCallCount);
        Assert.Empty(events);
    }

    [Theory]
    [MemberData(nameof(DeliverEventToList_CustomDeliverEvent_TestData))]
    public void DeliverEventToList_SourceNotListenerList_Success(object sender, EventArgs args, bool result)
    {
        var manager = new SubWeakEventManager();
        manager[sender] = new object();

        var list = new CustomListenerList();
        var events = new List<string>();
        var listener = new CustomWeakEventListener
        {
            ReceiveWeakEventAction = (t, s, e) =>
        {
            events.Add("listener1");
            return true;
        }
        };
        list.Add(listener);
        int deliverEventCallCount = 0;
        list.DeliverEventAction = (s, a, t) =>
        {
            Assert.Same(sender, s);
            Assert.Same(args, a);
            Assert.Equal(typeof(SubWeakEventManager), t);
            deliverEventCallCount++;
            return result;
        };

        // Call.
        manager.DeliverEventToList(sender, args, list);
        Assert.Equal(1, deliverEventCallCount);
        Assert.Empty(events);

        // Call again.
        manager.DeliverEventToList(sender, args, list);
        Assert.Equal(2, deliverEventCallCount);
        Assert.Empty(events);
    }

    [Fact]
    public void GetCurrentManager_InvokeNoSuchManagerType_ReturnsNull()
    {
        Assert.Null(SubWeakEventManager.GetCurrentManager(typeof(SentinelType1)));
    }

    private class SentinelType1 { }

    [Fact]
    public void GetCurrentManager_NullSource_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("key", () => SubWeakEventManager.GetCurrentManager(null!));
    }

    [Fact]
    public void NewListenerList_Invoke_ReturnsExpected()
    {
        var manager = new SubWeakEventManager();
        SubWeakEventManager.ListenerList list = manager.NewListenerList();
        Assert.NotNull(list);
        Assert.Equal(0, list.Count);
        Assert.True(list.IsEmpty);

        // Invoke again.
        Assert.NotSame(list, manager.NewListenerList());
    }

    [Fact]
    public void ProtectedAddHandler_Invoke_Success()
    {
        var manager = new SubWeakEventManager();
        object? expectedSource = null;
        int startListeningCallCount = 0;
        manager.StartListeningAction += (actualSource) =>
        {
            Assert.Same(expectedSource, actualSource);
            startListeningCallCount++;
        };

        var target1 = new CustomWeakEventListener();
        Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
        Delegate handler2 = (EventHandler)delegate { };
        Delegate handler3 = StaticEventHandler;
        Delegate handler4 = (EventHandler)delegate { };

        // Add new source.
        var source1 = new object();
        expectedSource = source1;
        manager.ProtectedAddHandler(source1, handler1);
        Assert.Equal(1, startListeningCallCount);
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target1, list[0]);

        // Add another listener to first source.
        manager.ProtectedAddHandler(source1, handler2);
        Assert.Equal(1, startListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target1, list[0]);
        Assert.Throws<InvalidCastException>(() => list[1]);

        // Add another source.
        var source2 = new object();
        expectedSource = source2;
        manager.ProtectedAddHandler(source2, handler3);
        Assert.Equal(2, startListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target1, list[0]);
        Assert.Throws<InvalidCastException>(() => list[1]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source2]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Throws<InvalidCastException>(() => list[0]);

        // Add static source.
        expectedSource = null;
        manager.ProtectedAddHandler(null!, handler4);
        Assert.Equal(3, startListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target1, list[0]);
        Assert.Throws<InvalidCastException>(() => list[1]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source2]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Throws<InvalidCastException>(() => list[0]);
        Assert.Null(manager[null!]);
    }

    [Fact]
    public void ProtectedAddHandler_InvokeSameTargetMultipleTimes_Success()
    {
        var manager = new SubWeakEventManager();
        
        object? expectedSource = null;
        int startListeningCallCount = 0;
        manager.StartListeningAction += (actualSource) =>
        {
            Assert.Same(expectedSource, actualSource);
            startListeningCallCount++;
        };

        var source = new object();
        var target1 = new CustomWeakEventListener();
        var target2 = new CustomWeakEventListener();
        Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
        Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));
        Delegate handler3 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.SecondHandler));
        Delegate handler4 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
        Delegate handler5 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.SecondHandler));
        Delegate handler6 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.SecondHandler));
        Delegate handler7 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));

        // Add first.
        expectedSource = source;
        manager.ProtectedAddHandler(source, handler1);
        Assert.Equal(1, startListeningCallCount);
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target1, list[0]);

        // Add second.
        manager.ProtectedAddHandler(source, handler2);
        Assert.Equal(1, startListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target1, list[0]);
        Assert.Same(target2, list[1]);

        // Add third.
        manager.ProtectedAddHandler(source, handler3);
        Assert.Equal(1, startListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(3, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target1, list[0]);
        Assert.Same(target2, list[1]);
        Assert.Same(target1, list[2]);

        // Add fourth.
        manager.ProtectedAddHandler(source, handler4);
        Assert.Equal(1, startListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(4, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target1, list[0]);
        Assert.Same(target2, list[1]);
        Assert.Same(target1, list[2]);
        Assert.Same(target1, list[3]);

        // Add fifth.
        manager.ProtectedAddHandler(source, handler5);
        Assert.Equal(1, startListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(5, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target1, list[0]);
        Assert.Same(target2, list[1]);
        Assert.Same(target1, list[2]);
        Assert.Same(target1, list[3]);
        Assert.Same(target1, list[4]);

        // Add sixth.
        manager.ProtectedAddHandler(source, handler6);
        Assert.Equal(1, startListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(6, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target1, list[0]);
        Assert.Same(target2, list[1]);
        Assert.Same(target1, list[2]);
        Assert.Same(target1, list[3]);
        Assert.Same(target1, list[4]);
        Assert.Same(target2, list[5]);

        // Add seventh.
        manager.ProtectedAddHandler(source, handler6);
        Assert.Equal(1, startListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(7, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target1, list[0]);
        Assert.Same(target2, list[1]);
        Assert.Same(target1, list[2]);
        Assert.Same(target1, list[3]);
        Assert.Same(target1, list[4]);
        Assert.Same(target2, list[5]);
        Assert.Same(target2, list[6]);
    }

    [Fact]
    public void ProtectedAddHandler_InvokeStaticSource_Success()
    {
        var manager = new SubWeakEventManager();

        FieldInfo actualSourceField = typeof(WeakEventManager).GetField("StaticSource", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(actualSourceField);
        object actualSource = actualSourceField.GetValue(null)!;
        Assert.Equal("{StaticSource}", actualSource.ToString());

        int startListeningCallCount = 0;
        manager.StartListeningAction += (source) =>
        {
            Assert.Null(source);
            startListeningCallCount++;
        };

        Delegate handler1 = (EventHandler)delegate { };
        Delegate handler2 = (EventHandler)delegate { };

        // Add first listener.
        manager.ProtectedAddHandler(null!, handler1);
        Assert.Equal(1, startListeningCallCount);
        Assert.Null(manager[null!]);
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[actualSource]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Throws<InvalidCastException>(() => list[0]);

        // Add second listener.
        manager.ProtectedAddHandler(null!, handler2);
        Assert.Equal(1, startListeningCallCount);
        Assert.Null(manager[null!]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[actualSource]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Throws<InvalidCastException>(() => list[0]);
        Assert.Throws<InvalidCastException>(() => list[1]);
    }

    [Fact]
    public void ProtectedAddHandler_InvokeWithListeners_Success()
    {
        var manager = new SubWeakEventManager();
        object? expectedSource = null;
        int startListeningCallCount = 0;
        manager.StartListeningAction += (actualSource) =>
        {
            Assert.Same(expectedSource, actualSource);
            startListeningCallCount++;
        };

        var target = new CustomWeakEventListener();
        Delegate handler = Delegate.CreateDelegate(typeof(EventHandler), target, nameof(CustomWeakEventListener.Handler));
        var listener = new CustomWeakEventListener();

        // Add listener.
        var source = new object();
        expectedSource = source;
        manager.ProtectedAddListener(source, listener);
        Assert.Equal(1, startListeningCallCount);
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener, list[0]);

        // Add handler.
        manager.ProtectedAddHandler(source, handler);
        Assert.Equal(1, startListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener, list[0]);
        Assert.Same(target, list[1]);
    }

    [Fact]
    public void ProtectedAddHandler_InvokeMultipleTimesSameSource_Success()
    {
        var manager = new SubWeakEventManager();
        object? expectedSource = null;
        int startListeningCallCount = 0;
        manager.StartListeningAction += (actualSource) =>
        {
            Assert.Same(expectedSource, actualSource);
            startListeningCallCount++;
        };

        var target = new CustomWeakEventListener();
        Delegate handler = Delegate.CreateDelegate(typeof(EventHandler), target, nameof(CustomWeakEventListener.Handler));

        // Add first.
        var source = new object();
        expectedSource = source;
        manager.ProtectedAddHandler(source, handler);
        Assert.Equal(1, startListeningCallCount);
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target, list[0]);

        // Add second.
        manager.ProtectedAddHandler(source, handler);
        Assert.Equal(1, startListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target, list[0]);
        Assert.Same(target, list[1]);
    }

    [Fact]
    public void ProtectedAddHandler_InvokeMultipleTimesDifferentSource_Success()
    {
        var manager = new SubWeakEventManager();
        object? expectedSource = null;
        int startListeningCallCount = 0;
        manager.StartListeningAction += (actualSource) =>
        {
            Assert.Same(expectedSource, actualSource);
            startListeningCallCount++;
        };

        var target = new CustomWeakEventListener();
        Delegate handler = Delegate.CreateDelegate(typeof(EventHandler), target, nameof(CustomWeakEventListener.Handler));

        // Add first source.
        var source1 = new object();
        expectedSource = source1;
        manager.ProtectedAddHandler(source1, handler);
        Assert.Equal(1, startListeningCallCount);
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target, list[0]);

        // Add second source.
        var source2 = new object();
        expectedSource = source2;
        manager.ProtectedAddHandler(source2, handler);
        Assert.Equal(2, startListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target, list[0]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source2]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target, list[0]);
    }

    [Fact]
    public void ProtectedAddHandler_InvokeMultipleSameSourceNotInUse_DoesNotCloneListenerList()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};

        var source = new object();
        var target1 = new CustomWeakEventListener();
        Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
        var target2 = new CustomWeakEventListener();
        Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));

        // Add first listener.
        manager.ProtectedAddHandler(source, handler1);
        SubWeakEventManager.ListenerList list1 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list1.Count);
        Assert.False(list1.IsEmpty);
        Assert.Same(target1, list1[0]);

        // Add second listener.
        manager.ProtectedAddHandler(source, handler2);
        SubWeakEventManager.ListenerList list2 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Same(list2, list1);
        Assert.Equal(2, list2.Count);
        Assert.False(list2.IsEmpty);
        Assert.Same(target1, list2[0]);
        Assert.Same(target2, list2[1]);
    }

    [Fact]
    public void ProtectedAddHandler_InvokeMultipleSameSourceInUse_ClonesListenerList()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};

        var source = new object();
        var target1 = new CustomWeakEventListener();
        Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
        var target2 = new CustomWeakEventListener();
        Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));

        // Add first listener.
        manager.ProtectedAddHandler(source, handler1);
        SubWeakEventManager.ListenerList list1 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list1.Count);
        Assert.False(list1.IsEmpty);
        Assert.Same(target1, list1[0]);

        // Begin use.
        list1.BeginUse();

        // Add second listener.
        manager.ProtectedAddHandler(source, handler2);
        SubWeakEventManager.ListenerList list2 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.NotSame(list2, list1);
        Assert.Equal(1, list1.Count);
        Assert.False(list1.IsEmpty);
        Assert.Same(target1, list1[0]);
        Assert.Equal(2, list2.Count);
        Assert.False(list2.IsEmpty);
        Assert.Same(target1, list2[0]);
        Assert.Same(target2, list2[1]);
    }

    [Fact]
    public void ProtectedAddHandler_InvokeMultipleSameSourceNoLongerInUse_DoesNotCloneListenerList()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};

        var source = new object();
        var target1 = new CustomWeakEventListener();
        Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
        var target2 = new CustomWeakEventListener();
        Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));

        // Add first listener.
        manager.ProtectedAddHandler(source, handler1);
        SubWeakEventManager.ListenerList list1 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list1.Count);
        Assert.False(list1.IsEmpty);
        Assert.Same(target1, list1[0]);

        // Begin use.
        list1.BeginUse();

        // End use.
        list1.EndUse();

        // Add second listener.
        manager.ProtectedAddHandler(source, handler2);
        SubWeakEventManager.ListenerList list2 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Same(list2, list1);
        Assert.Equal(2, list2.Count);
        Assert.False(list2.IsEmpty);
        Assert.Same(target1, list2[0]);
        Assert.Same(target2, list2[1]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(7)]
    public void ProtectedAddHandler_InvokeDifferentSizes_Success(int count)
    {
        // Test internal FrugalList implementation.
        var handlers = new List<Delegate>();
        for (int i = 0; i < count; i++)
        {
            handlers.Add(Delegate.CreateDelegate(typeof(EventHandler), new CustomWeakEventListener(), nameof(CustomWeakEventListener.Handler)));
        }

        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};

        // Add.
        var source = new object();
        for (int i = 0; i < count; i++)
        {
            manager.ProtectedAddHandler(source, handlers[i]);
            SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
            Assert.Equal(i + 1, list.Count);
            Assert.False(list.IsEmpty);
            for (int j = 0; j <= i; j++)
            {
                Assert.Same(handlers[j].Target, list[j]);
            }
        }
    }

    [Fact]
    public void ProtectedAddHandler_NullHandler_ThrowsArgumentNullException()
    {
        var manager = new SubWeakEventManager();
        var source = new object();
        Assert.Throws<ArgumentNullException>("handler", () => manager.ProtectedAddHandler(source, null!));
    }

    [Fact]
    public void ProtectedAddHandler_SourceNotListenerList_ThrowsInvalidCastException()
    {
        var manager = new SubWeakEventManager();
        var source = new object();
        manager[source] = new object();

        var target = new CustomWeakEventListener();
        var handler = Delegate.CreateDelegate(typeof(EventHandler), target, nameof(CustomWeakEventListener.Handler));        
        Assert.Throws<InvalidCastException>(() => manager.ProtectedAddHandler(source, handler));
    }

    [Fact]
    public void ProtectedAddListener_Invoke_Success()
    {
        var manager = new SubWeakEventManager();
        object? expectedSource = null;
        int startListeningCallCount = 0;
        manager.StartListeningAction += (actualSource) =>
        {
            Assert.Same(expectedSource, actualSource);
            startListeningCallCount++;
        };

        var listener1 = new CustomWeakEventListener();
        var listener2 = new CustomWeakEventListener();
        var listener3 = new CustomWeakEventListener();
        var listener4 = new CustomWeakEventListener();

        // Add new source.
        var source1 = new object();
        expectedSource = source1;
        manager.ProtectedAddListener(source1, listener1);
        Assert.Equal(1, startListeningCallCount);
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener1, list[0]);

        // Add another listener to first source.
        manager.ProtectedAddListener(source1, listener2);
        Assert.Equal(1, startListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener1, list[0]);
        Assert.Same(listener2, list[1]);

        // Add another source.
        var source2 = new object();
        expectedSource = source2;
        manager.ProtectedAddListener(source2, listener3);
        Assert.Equal(2, startListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener1, list[0]);
        Assert.Same(listener2, list[1]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source2]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener3, list[0]);

        // Add static source.
        expectedSource = null;
        manager.ProtectedAddListener(null!, listener4);
        Assert.Equal(3, startListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener1, list[0]);
        Assert.Same(listener2, list[1]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source2]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener3, list[0]);
        Assert.Null(manager[null!]);
        
        manager.ProtectedRemoveListener(null!, listener4);
    }

    [Fact]
    public void ProtectedAddListener_InvokeStaticSource_Success()
    {
        var manager = new SubWeakEventManager();

        FieldInfo actualSourceField = typeof(WeakEventManager).GetField("StaticSource", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(actualSourceField);
        object actualSource = actualSourceField.GetValue(null)!;
        Assert.Equal("{StaticSource}", actualSource.ToString());

        int startListeningCallCount = 0;
        manager.StartListeningAction += (source) =>
        {
            Assert.Null(source);
            startListeningCallCount++;
        };

        var listener1 = new CustomWeakEventListener();
        var listener2 = new CustomWeakEventListener();

        // Add first listener.
        manager.ProtectedAddListener(null!, listener1);
        Assert.Equal(1, startListeningCallCount);
        Assert.Null(manager[null!]);
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[actualSource]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener1, list[0]);

        // Add second listener.
        manager.ProtectedAddListener(null!, listener2);
        Assert.Equal(1, startListeningCallCount);
        Assert.Null(manager[null!]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[actualSource]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener1, list[0]);
        Assert.Same(listener2, list[1]);

        manager.ProtectedRemoveListener(null!, listener1);
        manager.ProtectedRemoveListener(null!, listener2);
    }

    [Fact]
    public void ProtectedAddListener_InvokeWithHandlers_Success()
    {
        var manager = new SubWeakEventManager();
        object? expectedSource = null;
        int startListeningCallCount = 0;
        manager.StartListeningAction += (actualSource) =>
        {
            Assert.Same(expectedSource, actualSource);
            startListeningCallCount++;
        };

        Delegate handler = () => { };
        var listener = new CustomWeakEventListener();

        // Add handler.
        var source = new object();
        expectedSource = source;
        manager.ProtectedAddHandler(source, handler);
        Assert.Equal(1, startListeningCallCount);
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Throws<InvalidCastException>(() => list[0]);

        // Add listener.
        manager.ProtectedAddListener(source, listener);
        Assert.Equal(1, startListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Throws<InvalidCastException>(() => list[0]);
        Assert.Same(listener, list[1]);
    }

    [Fact]
    public void ProtectedAddListener_InvokeMultipleTimesSameSource_Success()
    {
        var manager = new SubWeakEventManager();
        object? expectedSource = null;
        int startListeningCallCount = 0;
        manager.StartListeningAction += (actualSource) =>
        {
            Assert.Same(expectedSource, actualSource);
            startListeningCallCount++;
        };

        var listener = new CustomWeakEventListener();

        // Add first.
        var source = new object();
        expectedSource = source;
        manager.ProtectedAddListener(source, listener);
        Assert.Equal(1, startListeningCallCount);
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener, list[0]);

        // Add second.
        manager.ProtectedAddListener(source, listener);
        Assert.Equal(1, startListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener, list[0]);
        Assert.Same(listener, list[1]);
    }

    [Fact]
    public void ProtectedAddListener_InvokeMultipleTimesDifferentSource_Success()
    {
        var manager = new SubWeakEventManager();
        object? expectedSource = null;
        int startListeningCallCount = 0;
        manager.StartListeningAction += (actualSource) =>
        {
            Assert.Same(expectedSource, actualSource);
            startListeningCallCount++;
        };

        var listener = new CustomWeakEventListener();

        // Add first source.
        var source1 = new object();
        expectedSource = source1;
        manager.ProtectedAddListener(source1, listener);
        Assert.Equal(1, startListeningCallCount);
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener, list[0]);

        // Add second source.
        var source2 = new object();
        expectedSource = source2;
        manager.ProtectedAddListener(source2, listener);
        Assert.Equal(2, startListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener, list[0]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source2]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener, list[0]);
    }

    [Fact]
    public void ProtectedAddListener_InvokeMultipleSameSourceNotInUse_DoesNotCloneListenerList()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};

        var source = new object();
        var listener1 = new CustomWeakEventListener();
        var listener2 = new CustomWeakEventListener();

        // Add first listener.
        manager.ProtectedAddListener(source, listener1);
        SubWeakEventManager.ListenerList list1 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list1.Count);
        Assert.False(list1.IsEmpty);
        Assert.Same(listener1, list1[0]);

        // Add second listener.
        manager.ProtectedAddListener(source, listener2);
        SubWeakEventManager.ListenerList list2 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Same(list2, list1);
        Assert.Equal(2, list2.Count);
        Assert.False(list2.IsEmpty);
        Assert.Same(listener1, list2[0]);
        Assert.Same(listener2, list2[1]);
    }

    [Fact]
    public void ProtectedAddListener_InvokeMultipleSameSourceInUse_ClonesListenerList()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};

        var source = new object();
        var listener1 = new CustomWeakEventListener();
        var listener2 = new CustomWeakEventListener();

        // Add first listener.
        manager.ProtectedAddListener(source, listener1);
        SubWeakEventManager.ListenerList list1 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list1.Count);
        Assert.False(list1.IsEmpty);
        Assert.Same(listener1, list1[0]);

        // Begin use.
        list1.BeginUse();

        // Add second listener.
        manager.ProtectedAddListener(source, listener2);
        SubWeakEventManager.ListenerList list2 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.NotSame(list2, list1);
        Assert.Equal(1, list1.Count);
        Assert.False(list1.IsEmpty);
        Assert.Same(listener1, list1[0]);
        Assert.Equal(2, list2.Count);
        Assert.False(list2.IsEmpty);
        Assert.Same(listener1, list2[0]);
        Assert.Same(listener2, list2[1]);
    }

    [Fact]
    public void ProtectedAddListener_InvokeMultipleSameSourceNoLongerInUse_DoesNotCloneListenerList()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};

        var source = new object();
        var listener1 = new CustomWeakEventListener();
        var listener2 = new CustomWeakEventListener();

        // Add first listener.
        manager.ProtectedAddListener(source, listener1);
        SubWeakEventManager.ListenerList list1 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list1.Count);
        Assert.False(list1.IsEmpty);
        Assert.Same(listener1, list1[0]);

        // Begin use.
        list1.BeginUse();

        // End use.
        list1.EndUse();

        // Add second listener.
        manager.ProtectedAddListener(source, listener2);
        SubWeakEventManager.ListenerList list2 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Same(list2, list1);
        Assert.Equal(2, list2.Count);
        Assert.False(list2.IsEmpty);
        Assert.Same(listener1, list2[0]);
        Assert.Same(listener2, list2[1]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(7)]
    public void ProtectedAddListener_InvokeDifferentSizes_Success(int count)
    {
        // Test internal FrugalList implementation.
        var listeners = new List<CustomWeakEventListener>();
        for (int i = 0; i < count; i++)
        {
            listeners.Add(new CustomWeakEventListener());
        }

        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};

        // Add.
        var source = new object();
        for (int i = 0; i < count; i++)
        {
            manager.ProtectedAddListener(source, listeners[i]);
            SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
            Assert.Equal(i + 1, list.Count);
            Assert.False(list.IsEmpty);
            for (int j = 0; j <= i; j++)
            {
                Assert.Same(listeners[j], list[j]);
            }
        }
    }

    [Fact]
    public void ProtectedAddListener_NullListener_ThrowsArgumentNullException()
    {
        var manager = new SubWeakEventManager();
        var source = new object();
        Assert.Throws<ArgumentNullException>("listener", () => manager.ProtectedAddListener(source, null!));
    }

    [Fact]
    public void ProtectedAddListener_SourceNotListenerList_ThrowsInvalidCastException()
    {
        var manager = new SubWeakEventManager();
        var source = new object();
        manager[source] = new object();

        var listener = new CustomWeakEventListener();
        Assert.Throws<InvalidCastException>(() => manager.ProtectedAddListener(source, listener));
    }

    [Fact]
    public void ProtectedRemoveHandler_Invoke_Success()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};
        object? expectedSource = null;
        int stopListeningCallCount = 0;
        manager.StopListeningAction += (source) =>
        {
            Assert.Same(expectedSource, source);
            stopListeningCallCount++;
        };

        var source1 = new object();
        var source2 = new object();
        var target1 = new CustomWeakEventListener();
        Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
        Delegate handler2 = (EventHandler)delegate { };
        Delegate handler3 = StaticEventHandler;
        Delegate handler4 = (EventHandler)delegate { };
        manager.ProtectedAddHandler(source1, handler1);
        manager.ProtectedAddHandler(source1, handler2);
        manager.ProtectedAddHandler(source2, handler3);
        manager.ProtectedAddHandler(source2, handler4);
        
        // Remove handler2.
        manager.ProtectedRemoveHandler(source1, handler2);
        Assert.Equal(0, stopListeningCallCount);
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target1, list[0]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source2]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Throws<InvalidCastException>(() => list[0]);
        Assert.Throws<InvalidCastException>(() => list[1]);
        
        // Remove handler2 again.
        manager.ProtectedRemoveHandler(source1, handler2);
        Assert.Equal(0, stopListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target1, list[0]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source2]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Throws<InvalidCastException>(() => list[0]);
        Assert.Throws<InvalidCastException>(() => list[1]);

        // Remove handler1.
        expectedSource = source1;
        manager.ProtectedRemoveHandler(source1, handler1);
        Assert.Equal(1, stopListeningCallCount);
        Assert.Null(manager[source1]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source2]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Throws<InvalidCastException>(() => list[0]);
        Assert.Throws<InvalidCastException>(() => list[1]);

        // Remove handler3.
        expectedSource = source2;
        manager.ProtectedRemoveHandler(source2, handler3);
        Assert.Equal(1, stopListeningCallCount);
        Assert.Null(manager[source1]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source2]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Throws<InvalidCastException>(() => list[0]);

        // Remove handler4.
        expectedSource = source2;
        manager.ProtectedRemoveHandler(source2, handler4);
        Assert.Equal(2, stopListeningCallCount);
        Assert.Null(manager[source1]);
        Assert.Null(manager[source2]);
    }

    [Fact]
    public void ProtectedRemoveHandler_InvokeStaticSource_Success()
    {
        var manager = new SubWeakEventManager();

        FieldInfo actualSourceField = typeof(WeakEventManager).GetField("StaticSource", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(actualSourceField);
        object actualSource = actualSourceField.GetValue(null)!;
        Assert.Equal("{StaticSource}", actualSource.ToString());

        manager.StartListeningAction += (source) => {};
        int stopListeningCallCount = 0;
        manager.StopListeningAction += (source) =>
        {
            Assert.Null(source);
            stopListeningCallCount++;
        };

        Delegate handler1 = (EventHandler)delegate { };
        Delegate handler2 = (EventHandler)delegate { };
        manager.ProtectedAddHandler(null!, handler1);
        manager.ProtectedAddHandler(null!, handler2);

        // Add first handler.
        manager.ProtectedRemoveHandler(null!, handler1);
        Assert.Equal(0, stopListeningCallCount);
        Assert.Null(manager[null!]);
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[actualSource]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Throws<InvalidCastException>(() => list[0]);

        // Add second handler.
        manager.ProtectedRemoveHandler(null!, handler2);
        Assert.Equal(1, stopListeningCallCount);
        Assert.Null(manager[null!]);
        Assert.Null(manager[actualSource]);
    }

    [Fact]
    public void ProtectedRemoveHandler_InvokeSameTargetMultipleTimesSuccess()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};
        object? expectedSource = null;
        int stopListeningCallCount = 0;
        manager.StopListeningAction += (source) =>
        {
            Assert.Same(expectedSource, source);
            stopListeningCallCount++;
        };

        var source = new object();
        var target1 = new CustomWeakEventListener();
        var target2 = new CustomWeakEventListener();
        Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
        Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));
        Delegate handler3 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.SecondHandler));
        Delegate handler4 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
        Delegate handler5 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.SecondHandler));
        Delegate handler6 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.SecondHandler));
        Delegate handler7 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));

        manager.ProtectedAddHandler(source, handler1);
        manager.ProtectedAddHandler(source, handler2);
        manager.ProtectedAddHandler(source, handler3);
        manager.ProtectedAddHandler(source, handler4);
        manager.ProtectedAddHandler(source, handler5);
        manager.ProtectedAddHandler(source, handler6);
        manager.ProtectedAddHandler(source, handler7);

        // Remove handler1.
        manager.ProtectedRemoveHandler(source, handler1);
        Assert.Equal(0, stopListeningCallCount);
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(6, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Equal(target1, list[0]);
        Assert.Equal(target2, list[1]);
        Assert.Equal(target1, list[2]);
        Assert.Equal(target1, list[3]);
        Assert.Equal(target2, list[4]);
        Assert.Equal(target2, list[5]);

        // Remove handler2.
        manager.ProtectedRemoveHandler(source, handler2);
        Assert.Equal(0, stopListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(5, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Equal(target1, list[0]);
        Assert.Equal(target2, list[1]);
        Assert.Equal(target1, list[2]);
        Assert.Equal(target1, list[3]);
        Assert.Equal(target2, list[4]);

        // Remove handler3.
        manager.ProtectedRemoveHandler(source, handler3);
        Assert.Equal(0, stopListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(4, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Equal(target1, list[0]);
        Assert.Equal(target2, list[1]);
        Assert.Equal(target1, list[2]);
        Assert.Equal(target2, list[3]);

        // Remove handler4.
        manager.ProtectedRemoveHandler(source, handler4);
        Assert.Equal(0, stopListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(3, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Equal(target2, list[0]);
        Assert.Equal(target1, list[1]);
        Assert.Equal(target2, list[2]);
        
        // Remove handler5.
        manager.ProtectedRemoveHandler(source, handler5);
        Assert.Equal(0, stopListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Equal(target2, list[0]);
        Assert.Equal(target2, list[1]);

        // Remove handler6.
        manager.ProtectedRemoveHandler(source, handler6);
        Assert.Equal(0, stopListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Equal(target2, list[0]);

        // Remove handler7.
        expectedSource = source;
        manager.ProtectedRemoveHandler(source, handler7);
        Assert.Equal(1, stopListeningCallCount);
        Assert.Null(manager[source]);
    }

    [Fact]
    public void ProtectedRemoveHandler_InvokeWithListeners_Success()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (actualSource) => {};
        manager.StopListeningAction += (source) => {};

        var source = new object();
        var listener = new CustomWeakEventListener();
        Delegate handler = (EventHandler)delegate { };
        manager.ProtectedAddListener(source, listener);
        manager.ProtectedAddHandler(source, handler);
        
        // Remove handler.
        manager.ProtectedRemoveHandler(source, handler);
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener, list[0]);
    }

    [Fact]
    public void ProtectedRemoveHandler_InvokeMultipleSameSourceNotInUse_DoesNotCloneListenerList()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};
        manager.StopListeningAction += (source) => {};

        var source = new object();
        var target1 = new CustomWeakEventListener();
        Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
        var target2 = new CustomWeakEventListener();
        Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));
        var target3 = new CustomWeakEventListener();
        Delegate handler3 = Delegate.CreateDelegate(typeof(EventHandler), target3, nameof(CustomWeakEventListener.Handler));
        manager.ProtectedAddHandler(source, handler1);
        manager.ProtectedAddHandler(source, handler2);
        manager.ProtectedAddHandler(source, handler3);

        // Remove first handler.
        SubWeakEventManager.ListenerList list1 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        manager.ProtectedRemoveHandler(source, handler1);
        SubWeakEventManager.ListenerList list2 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Same(list2, list1);
        Assert.Equal(2, list2.Count);
        Assert.False(list2.IsEmpty);
        Assert.Same(target2, list2[0]);
        Assert.Same(target3, list2[1]);
        
        // Remove second handler.
        manager.ProtectedRemoveHandler(source, handler2);
        SubWeakEventManager.ListenerList list3 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Same(list3, list1);
        Assert.Equal(1, list1.Count);
        Assert.False(list3.IsEmpty);
        Assert.Same(target3, list3[0]);

        // Remove third handler.
        manager.ProtectedRemoveHandler(source, handler3);
        Assert.Null(manager[source]);
    }

    [Fact]
    public void ProtectedRemoveHandler_InvokeMultipleSameSourceInUse_ClonesListenerList()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};
        manager.StopListeningAction += (source) => {};

        var source = new object();
        var target1 = new CustomWeakEventListener();
        Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
        var target2 = new CustomWeakEventListener();
        Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));
        var target3 = new CustomWeakEventListener();
        Delegate handler3 = Delegate.CreateDelegate(typeof(EventHandler), target3, nameof(CustomWeakEventListener.Handler));
        manager.ProtectedAddHandler(source, handler1);
        manager.ProtectedAddHandler(source, handler2);
        manager.ProtectedAddHandler(source, handler3);

        // Remove first handler.
        SubWeakEventManager.ListenerList list1 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        manager.ProtectedRemoveHandler(source, handler1);
        SubWeakEventManager.ListenerList list2 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Same(list2, list1);
        Assert.Equal(2, list2.Count);
        Assert.False(list2.IsEmpty);
        Assert.Same(target2, list2[0]);
        Assert.Same(target3, list2[1]);

        // Begin use.
        list1.BeginUse();

        // Remove second handler.
        manager.ProtectedRemoveHandler(source, handler2);
        SubWeakEventManager.ListenerList list3 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.NotSame(list3, list1);
        Assert.Equal(2, list1.Count);
        Assert.False(list1.IsEmpty);
        Assert.Same(target2, list1[0]);
        Assert.Same(target3, list1[1]);
        Assert.Equal(1, list3.Count);
        Assert.False(list3.IsEmpty);
        Assert.Same(target3, list3[0]);

        // Remove third handler.
        manager.ProtectedRemoveHandler(source, handler3);
        Assert.Null(manager[source]);
        Assert.NotSame(list3, list1);
        Assert.Equal(2, list1.Count);
        Assert.False(list1.IsEmpty);
        Assert.Same(target2, list1[0]);
        Assert.Same(target3, list1[1]);
        Assert.Equal(0, list3.Count);
        Assert.True(list3.IsEmpty);
    }

    [Fact]
    public void ProtectedRemoveHandler_InvokeMultipleSameSourceNoLongerInUse_DoesNotCloneListenerList()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};
        manager.StopListeningAction += (source) => {};

        var source = new object();
        var target1 = new CustomWeakEventListener();
        Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
        var target2 = new CustomWeakEventListener();
        Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));
        var target3 = new CustomWeakEventListener();
        Delegate handler3 = Delegate.CreateDelegate(typeof(EventHandler), target3, nameof(CustomWeakEventListener.Handler));
        manager.ProtectedAddHandler(source, handler1);
        manager.ProtectedAddHandler(source, handler2);
        manager.ProtectedAddHandler(source, handler3);

        // Remove first handler.
        SubWeakEventManager.ListenerList list1 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        manager.ProtectedRemoveHandler(source, handler1);
        SubWeakEventManager.ListenerList list2 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Same(list2, list1);
        Assert.Equal(2, list1.Count);
        Assert.False(list1.IsEmpty);
        Assert.Same(target2, list1[0]);
        Assert.Same(target3, list1[1]);

        // Begin use.
        list1.BeginUse();

        // End use.
        list1.EndUse();

        // Remove second handler.
        manager.ProtectedRemoveHandler(source, handler2);
        SubWeakEventManager.ListenerList list3 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Same(list3, list1);
        Assert.Equal(1, list3.Count);
        Assert.False(list3.IsEmpty);
        Assert.Same(target3, list3[0]);

        // Remove third handler.
        manager.ProtectedRemoveHandler(source, handler3);
        Assert.Null(manager[source]);
        Assert.Same(list3, list1);
        Assert.Equal(0, list3.Count);
        Assert.True(list3.IsEmpty);
    }

    [Fact]
    public void ProtectedRemoveHandler_InvokeNoSuchSourceEmpty_Succcess()
    {
        var manager = new SubWeakEventManager();

        // Remove.
        manager.ProtectedRemoveHandler(new object(), (EventHandler)delegate { });

        // Remove again.
        manager.ProtectedRemoveHandler(new object(), (EventHandler)delegate { });
    }

    [Fact]
    public void ProtectedRemoveHandler_InvokeNoSuchSourceNotEmpty_Succcess()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};
        var source = new object();
        var target = new CustomWeakEventListener();
        Delegate handler = Delegate.CreateDelegate(typeof(EventHandler), target, nameof(CustomWeakEventListener.Handler));
        manager.ProtectedAddHandler(source, handler);

        // Remove.
        manager.ProtectedRemoveHandler(new object(), (EventHandler)delegate { });
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target, list[0]);

        // Remove again.
        manager.ProtectedRemoveHandler(new object(), (EventHandler)delegate { });
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target, list[0]);
    }

    [Fact]
    public void ProtectedRemoveHandler_InvokeNoSuchHandler_Success()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};
        var source = new object();
        var target = new CustomWeakEventListener();
        Delegate handler = Delegate.CreateDelegate(typeof(EventHandler), target, nameof(CustomWeakEventListener.Handler));
        manager.ProtectedAddHandler(source, handler);

        // Remove.
        manager.ProtectedRemoveHandler(source, (EventHandler)delegate { });
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target, list[0]);

        // Remove again.
        manager.ProtectedRemoveHandler(source, (EventHandler)delegate { });
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target, list[0]);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(3, 0)]
    [InlineData(3, 1)]
    [InlineData(3, 2)]
    [InlineData(4, 0)]
    [InlineData(4, 1)]
    [InlineData(4, 2)]
    [InlineData(4, 3)]
    [InlineData(6, 0)]
    [InlineData(6, 1)]
    [InlineData(6, 2)]
    [InlineData(6, 3)]
    [InlineData(6, 4)]
    [InlineData(6, 5)]
    [InlineData(7, 0)]
    [InlineData(7, 1)]
    [InlineData(7, 2)]
    [InlineData(7, 3)]
    [InlineData(7, 4)]
    [InlineData(7, 5)]
    public void ProtectedRemoveHandler_InvokeDifferentSizes_Success(int count, int removeIndex)
    {
        // Test internal FrugalList implementation.
        var handlers = new List<Delegate>();
        for (int i = 0; i < count; i++)
        {
            handlers.Add(Delegate.CreateDelegate(typeof(EventHandler), new CustomWeakEventListener(), nameof(CustomWeakEventListener.Handler)));
        }

        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};
        manager.StopListeningAction += (source) => {};

        var source = new object();
        for (int i = 0; i < handlers.Count; i++)
        {
            manager.ProtectedAddHandler(source, handlers[i]);
        }

        // Remove.
        manager.ProtectedRemoveHandler(source, handlers[removeIndex]);

        var expectedHandlers = new List<Delegate>(handlers);
        expectedHandlers.RemoveAt(removeIndex);
        if (expectedHandlers.Count != 0)
        {
            SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
            Assert.Equal(expectedHandlers.Count, list.Count);
            Assert.False(list.IsEmpty);
            for (int i = 0; i < expectedHandlers.Count; i++)
            {
                Assert.Same(expectedHandlers[i].Target, list[i]);
            }
        }
        else
        {
            Assert.Null(manager[source]);
        }

        // Remove all.
        for (int i = 0; i < expectedHandlers.Count; i++)
        {
            manager.ProtectedRemoveHandler(source, expectedHandlers[i]);
        }

        Assert.Null(manager[source]);
    }

    [Fact]
    public void ProtectedRemoveHandler_NullHandler_ThrowsArgumentNullException()
    {
        var manager = new SubWeakEventManager();
        var source = new object();
        Assert.Throws<ArgumentNullException>("handler", () => manager.ProtectedRemoveHandler(source, null!));
    }

    [Fact]
    public void ProtectedRemoveHandler_SourceNotListenerList_ThrowsInvalidCastException()
    {
        var manager = new SubWeakEventManager();
        var source = new object();
        manager[source] = new object();

        var target = new CustomWeakEventListener();
        var handler = Delegate.CreateDelegate(typeof(EventHandler), target, nameof(CustomWeakEventListener.Handler));        
        Assert.Throws<InvalidCastException>(() => manager.ProtectedRemoveHandler(source, handler));
    }

    [Fact]
    public void ProtectedRemoveListener_Invoke_Success()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};
        object? expectedSource = null;
        int stopListeningCallCount = 0;
        manager.StopListeningAction += (source) =>
        {
            Assert.Same(expectedSource, source);
            stopListeningCallCount++;
        };

        var source1 = new object();
        var source2 = new object();
        var listener1 = new CustomWeakEventListener();
        var listener2 = new CustomWeakEventListener();
        var listener3 = new CustomWeakEventListener();
        var listener4 = new CustomWeakEventListener();
        manager.ProtectedAddListener(source1, listener1);
        manager.ProtectedAddListener(source1, listener2);
        manager.ProtectedAddListener(source2, listener3);
        manager.ProtectedAddListener(source2, listener4);
        
        // Remove listener2.
        manager.ProtectedRemoveListener(source1, listener2);
        Assert.Equal(0, stopListeningCallCount);
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener1, list[0]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source2]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener3, list[0]);
        Assert.Same(listener4, list[1]);
        
        // Remove listener2 again.
        manager.ProtectedRemoveListener(source1, listener2);
        Assert.Equal(0, stopListeningCallCount);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener1, list[0]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source2]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener3, list[0]);
        Assert.Same(listener4, list[1]);

        // Remove listener1.
        expectedSource = source1;
        manager.ProtectedRemoveListener(source1, listener1);
        Assert.Equal(1, stopListeningCallCount);
        Assert.Null(manager[source1]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source2]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener3, list[0]);
        Assert.Same(listener4, list[1]);

        // Remove listener3.
        expectedSource = source2;
        manager.ProtectedRemoveListener(source2, listener3);
        Assert.Equal(1, stopListeningCallCount);
        Assert.Null(manager[source1]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source2]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener4, list[0]);

        // Remove listener4.
        expectedSource = source2;
        manager.ProtectedRemoveListener(source2, listener4);
        Assert.Equal(2, stopListeningCallCount);
        Assert.Null(manager[source1]);
        Assert.Null(manager[source2]);
    }

    [Fact]
    public void ProtectedRemoveListener_InvokeStaticSource_Success()
    {
        var manager = new SubWeakEventManager();

        FieldInfo actualSourceField = typeof(WeakEventManager).GetField("StaticSource", BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.NotNull(actualSourceField);
        object actualSource = actualSourceField.GetValue(null)!;
        Assert.Equal("{StaticSource}", actualSource.ToString());

        manager.StartListeningAction += (source) => {};
        int stopListeningCallCount = 0;
        manager.StopListeningAction += (source) =>
        {
            Assert.Null(source);
            stopListeningCallCount++;
        };

        var listener1 = new CustomWeakEventListener();
        var listener2 = new CustomWeakEventListener();
        manager.ProtectedAddListener(null!, listener1);
        manager.ProtectedAddListener(null!, listener2);

        // Add first listener.
        manager.ProtectedRemoveListener(null!, listener1);
        Assert.Equal(0, stopListeningCallCount);
        Assert.Null(manager[null!]);
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[actualSource]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener2, list[0]);

        // Add second listener.
        manager.ProtectedRemoveListener(null!, listener2);
        Assert.Equal(1, stopListeningCallCount);
        Assert.Null(manager[null!]);
        Assert.Null(manager[actualSource]);
    }

    [Fact]
    public void ProtectedRemoveListener_InvokeWithHandlers_Success()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (actualSource) => {};
        manager.StopListeningAction += (source) => {};

        var source = new object();
        Delegate handler = () => { };
        var listener = new CustomWeakEventListener();
        manager.ProtectedAddHandler(source, handler);
        manager.ProtectedAddListener(source, listener);
        
        // Remove listener.
        manager.ProtectedRemoveListener(source, listener);
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Throws<InvalidCastException>(() => list[0]);
    }

    [Fact]
    public void ProtectedRemoveListener_InvokeMultipleSameSourceNotInUse_DoesNotCloneListenerList()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};
        manager.StopListeningAction += (source) => {};

        var source = new object();
        var listener1 = new CustomWeakEventListener();
        var listener2 = new CustomWeakEventListener();
        var listener3 = new CustomWeakEventListener();
        manager.ProtectedAddListener(source, listener1);
        manager.ProtectedAddListener(source, listener2);
        manager.ProtectedAddListener(source, listener3);

        // Remove first listener.
        SubWeakEventManager.ListenerList list1 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        manager.ProtectedRemoveListener(source, listener1);
        SubWeakEventManager.ListenerList list2 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Same(list2, list1);
        Assert.Equal(2, list2.Count);
        Assert.False(list2.IsEmpty);
        Assert.Same(listener2, list2[0]);
        Assert.Same(listener3, list2[1]);
        
        // Remove second listener.
        manager.ProtectedRemoveListener(source, listener2);
        SubWeakEventManager.ListenerList list3 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Same(list3, list1);
        Assert.Equal(1, list1.Count);
        Assert.False(list3.IsEmpty);
        Assert.Same(listener3, list3[0]);

        // Remove third listener.
        manager.ProtectedRemoveListener(source, listener3);
        Assert.Null(manager[source]);
    }

    [Fact]
    public void ProtectedRemoveListener_InvokeMultipleSameSourceInUse_ClonesListenerList()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};
        manager.StopListeningAction += (source) => {};

        var source = new object();
        var listener1 = new CustomWeakEventListener();
        var listener2 = new CustomWeakEventListener();
        var listener3 = new CustomWeakEventListener();
        manager.ProtectedAddListener(source, listener1);
        manager.ProtectedAddListener(source, listener2);
        manager.ProtectedAddListener(source, listener3);

        // Remove first listener.
        SubWeakEventManager.ListenerList list1 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        manager.ProtectedRemoveListener(source, listener1);
        SubWeakEventManager.ListenerList list2 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Same(list2, list1);
        Assert.Equal(2, list1.Count);
        Assert.False(list1.IsEmpty);
        Assert.Same(listener2, list1[0]);
        Assert.Same(listener3, list1[1]);

        // Begin use.
        list1.BeginUse();

        // Remove second listener.
        manager.ProtectedRemoveListener(source, listener2);
        SubWeakEventManager.ListenerList list3 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.NotSame(list3, list1);
        Assert.Equal(2, list1.Count);
        Assert.False(list1.IsEmpty);
        Assert.Same(listener2, list1[0]);
        Assert.Same(listener3, list1[1]);
        Assert.Equal(1, list3.Count);
        Assert.False(list3.IsEmpty);
        Assert.Same(listener3, list3[0]);

        // Remove third listener.
        manager.ProtectedRemoveListener(source, listener3);
        Assert.Null(manager[source]);
        Assert.NotSame(list3, list1);
        Assert.Equal(2, list1.Count);
        Assert.False(list1.IsEmpty);
        Assert.Same(listener2, list1[0]);
        Assert.Same(listener3, list1[1]);
        Assert.Equal(0, list3.Count);
        Assert.True(list3.IsEmpty);
    }

    [Fact]
    public void ProtectedRemoveListener_InvokeMultipleSameSourceNoLongerInUse_DoesNotCloneListenerList()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};
        manager.StopListeningAction += (source) => {};

        var source = new object();
        var listener1 = new CustomWeakEventListener();
        var listener2 = new CustomWeakEventListener();
        var listener3 = new CustomWeakEventListener();
        manager.ProtectedAddListener(source, listener1);
        manager.ProtectedAddListener(source, listener2);
        manager.ProtectedAddListener(source, listener3);

        // Remove first listener.
        SubWeakEventManager.ListenerList list1 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        manager.ProtectedRemoveListener(source, listener1);
        SubWeakEventManager.ListenerList list2 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Same(list2, list1);
        Assert.Equal(2, list1.Count);
        Assert.False(list1.IsEmpty);
        Assert.Same(listener2, list1[0]);
        Assert.Same(listener3, list1[1]);

        // Begin use.
        list1.BeginUse();

        // End use.
        list1.EndUse();

        // Remove second listener.
        manager.ProtectedRemoveListener(source, listener2);
        SubWeakEventManager.ListenerList list3 = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Same(list3, list1);
        Assert.Equal(1, list3.Count);
        Assert.False(list3.IsEmpty);
        Assert.Same(listener3, list3[0]);

        // Remove third listener.
        manager.ProtectedRemoveListener(source, listener3);
        Assert.Null(manager[source]);
        Assert.Same(list3, list1);
        Assert.Equal(0, list3.Count);
        Assert.True(list3.IsEmpty);
    }

    [Fact]
    public void ProtectedRemoveListener_InvokeNoSuchSourceEmpty_Succcess()
    {
        var manager = new SubWeakEventManager();

        // Remove.
        manager.ProtectedRemoveListener(new object(), new CustomWeakEventListener());

        // Remove again.
        manager.ProtectedRemoveListener(new object(), new CustomWeakEventListener());
    }

    [Fact]
    public void ProtectedRemoveListener_InvokeNoSuchSourceNotEmpty_Succcess()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};
        var source = new object();
        var listener = new CustomWeakEventListener();
        manager.ProtectedAddListener(source, listener);

        // Remove.
        manager.ProtectedRemoveListener(new object(), new CustomWeakEventListener());
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener, list[0]);

        // Remove again.
        manager.ProtectedRemoveListener(new object(), new CustomWeakEventListener());
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener, list[0]);
    }

    [Fact]
    public void ProtectedRemoveListener_InvokeNoSuchListener_Success()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};
        var source = new object();
        var listener = new CustomWeakEventListener();
        manager.ProtectedAddListener(source, listener);

        // Remove.
        manager.ProtectedRemoveListener(source, new CustomWeakEventListener());
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener, list[0]);

        // Remove again.
        manager.ProtectedRemoveListener(source, new CustomWeakEventListener());
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener, list[0]);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(3, 0)]
    [InlineData(3, 1)]
    [InlineData(3, 2)]
    [InlineData(4, 0)]
    [InlineData(4, 1)]
    [InlineData(4, 2)]
    [InlineData(4, 3)]
    [InlineData(6, 0)]
    [InlineData(6, 1)]
    [InlineData(6, 2)]
    [InlineData(6, 3)]
    [InlineData(6, 4)]
    [InlineData(6, 5)]
    [InlineData(7, 0)]
    [InlineData(7, 1)]
    [InlineData(7, 2)]
    [InlineData(7, 3)]
    [InlineData(7, 4)]
    [InlineData(7, 5)]
    public void ProtectedRemoveListener_InvokeDifferentSizes_Success(int count, int removeIndex)
    {
        // Test internal FrugalList implementation.
        var listeners = new List<CustomWeakEventListener>();
        for (int i = 0; i < count; i++)
        {
            listeners.Add(new CustomWeakEventListener());
        }

        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};
        manager.StopListeningAction += (source) => {};

        var source = new object();
        for (int i = 0; i < listeners.Count; i++)
        {
            manager.ProtectedAddListener(source, listeners[i]);
        }
        
        // Remove.
        manager.ProtectedRemoveListener(source, listeners[removeIndex]);
        
        var expectedListeners = new List<CustomWeakEventListener>(listeners);
        expectedListeners.RemoveAt(removeIndex);
        if (expectedListeners.Count != 0)
        {
            SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source]);
            Assert.Equal(expectedListeners.Count, list.Count);
            Assert.False(list.IsEmpty);
            for (int i = 0; i < expectedListeners.Count; i++)
            {
                Assert.Equal(expectedListeners[i], list[i]);
            }
        }
        else
        {
            Assert.Null(manager[source]);
        }

        // Remove all.
        for (int i = 0; i < expectedListeners.Count; i++)
        {
            manager.ProtectedRemoveListener(source, expectedListeners[i]);
        }

        Assert.Null(manager[source]);
    }

    [Fact]
    public void ProtectedRemoveListener_NullListener_ThrowsArgumentNullException()
    {
        var manager = new SubWeakEventManager();
        var source = new object();
        Assert.Throws<ArgumentNullException>("listener", () => manager.ProtectedRemoveListener(source, null!));
    }

    [Fact]
    public void ProtectedRemoveListener_SourceNotListenerList_ThrowsInvalidCastException()
    {
        var manager = new SubWeakEventManager();
        var source = new object();
        manager[source] = new object();

        var listener = new CustomWeakEventListener();
        Assert.Throws<InvalidCastException>(() => manager.ProtectedRemoveListener(source, listener));
    }

    public static IEnumerable<object?[]> Purge_Data_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { new object() };
        yield return new object?[] { new ListenerList() };
    }

    [Theory]
    [MemberData(nameof(Purge_Data_TestData))]
    public void Purge_InvokeNotEmptyPurgeAllWithSource_Success(object data)
    {
        var manager = new SubWeakEventManager();
        object? expectedSource = null;
        int stopListeningCallCount = 0;
        manager.StopListeningAction = (source) =>
        {
            Assert.Same(expectedSource, source);
            stopListeningCallCount++;
        };
        var source = new object();
        var value = new object();
        manager[source] = value;
        
        // Purge.
        expectedSource = source;
        Assert.False(manager.Purge(source, data, true));
        Assert.Equal(1, stopListeningCallCount);
        Assert.Same(value, manager[source]);
        
        // Purge again.
        expectedSource = source;
        Assert.False(manager.Purge(source, data, true));
        Assert.Equal(2, stopListeningCallCount);
        Assert.Same(value, manager[source]);
    }

    [Fact]
    public void Purge_InvokeNotEmptyNoPurgeAllNotEmptyListenerListData_ReturnsTrue()
    {
        var manager = new SubWeakEventManager();
        var source = new object();
        var value = new object();
        manager[source] = value;

        var list = new ListenerList();
        var listener = new CustomWeakEventListener();
        var target = new CustomWeakEventListener();
        Delegate handler = Delegate.CreateDelegate(typeof(EventHandler), target, nameof(CustomWeakEventListener.Handler));
        list.Add(listener);
        list.AddHandler(handler);

        // Purge.
        Assert.False(manager.Purge(source, list, false));
        Assert.False(list.IsEmpty);
        Assert.Equal(2, list.Count);
        Assert.Same(listener, list[0]);
        Assert.Same(target, list[1]);
        Assert.Same(value, manager[source]);
    }

    [Fact]
    public void Purge_InvokeNotEmptyNoPurgeAllEmptyListenerListData_ReturnsTrue()
    {
        var manager = new SubWeakEventManager();
        object? expectedSource = null;
        int stopListeningCallCount = 0;
        manager.StopListeningAction = (source) =>
        {
            Assert.Same(expectedSource, source);
            stopListeningCallCount++;
        };
        var source = new object();
        var value = new object();
        manager[source] = value;

        var list = new ListenerList();

        // Purge.
        expectedSource = source;
        Assert.True(manager.Purge(source, list, false));
        Assert.Equal(1, stopListeningCallCount);
        Assert.True(list.IsEmpty);
        Assert.Equal(0, list.Count);
        Assert.Null(manager[source]);
        
        // Purge again.
        expectedSource = source;
        Assert.True(manager.Purge(source, list, false));
        Assert.Equal(2, stopListeningCallCount);
        Assert.True(list.IsEmpty);
        Assert.Equal(0, list.Count);
        Assert.Null(manager[source]);
    }

    [Fact]
    public void Purge_InvokeNotEmptyNoPurgeAllNullData_ThrowsNullReferenceException()
    {
        var manager = new SubWeakEventManager();
        var source = new object();
        var value = new object();
        manager[source] = value;

        // Purge.
        Assert.Throws<NullReferenceException>(() => manager.Purge(source, null!, false));
        Assert.Same(value, manager[source]);
    }

    [Fact]
    public void Purge_InvokeNotEmptyNoPurgeAllNotListenerListData_ThrowsInvalidCastException()
    {
        var manager = new SubWeakEventManager();
        var source = new object();
        var value = new object();
        manager[source] = value;

        // Purge.
        Assert.Throws<InvalidCastException>(() => manager.Purge(source, new object(), false));
        Assert.Same(value, manager[source]);
    }

    [Theory]
    [MemberData(nameof(Purge_Data_TestData))]
    public void Purge_InvokeNotEmptyPurgeAllWithNoSource_Success(object data)
    {
        var manager = new SubWeakEventManager();
        var source = new object();
        var value = new object();
        manager[source] = value;
        
        // Purge.
        Assert.False(manager.Purge(null!, data, true));
        Assert.Same(value, manager[source]);
        
        // Purge again.
        Assert.False(manager.Purge(null!, data, true));
        Assert.Same(value, manager[source]);
    }

    [Theory]
    [MemberData(nameof(Purge_Data_TestData))]
    public void Purge_InvokeNotEmptyNoPurgeAllWithNoSource_Success(object data)
    {
        var manager = new SubWeakEventManager();
        var source = new object();
        var value = new object();
        manager[source] = value;
        
        // Purge.
        Assert.False(manager.Purge(null!, data, true));
        Assert.Same(value, manager[source]);
        
        // Purge again.
        Assert.False(manager.Purge(null!, data, true));
        Assert.Same(value, manager[source]);
    }

    [Theory]
    [MemberData(nameof(Purge_Data_TestData))]
    public void Purge_InvokeEmptyPurgeAllWithSource_Success(object data)
    {
        var manager = new SubWeakEventManager();
        object? expectedSource = null;
        int stopListeningCallCount = 0;
        manager.StopListeningAction = (source) =>
        {
            Assert.Same(expectedSource, source);
            stopListeningCallCount++;
        };
        var source = new object();
        
        // Purge.
        expectedSource = source;
        Assert.False(manager.Purge(source, data, true));
        Assert.Equal(1, stopListeningCallCount);
        Assert.Null(manager[source]);
        
        // Purge again.
        expectedSource = source;
        Assert.False(manager.Purge(source, data, true));
        Assert.Equal(2, stopListeningCallCount);
        Assert.Null(manager[source]);
    }

    [Fact]
    public void Purge_InvokeEmptyNoPurgeAllNotEmptyListenerListData_ReturnsTrue()
    {
        var manager = new SubWeakEventManager();
        var source = new object();

        var list = new ListenerList();
        var listener = new CustomWeakEventListener();
        var target = new CustomWeakEventListener();
        Delegate handler = Delegate.CreateDelegate(typeof(EventHandler), target, nameof(CustomWeakEventListener.Handler));
        list.Add(listener);
        list.AddHandler(handler);

        // Purge.
        Assert.False(manager.Purge(source, list, false));
        Assert.False(list.IsEmpty);
        Assert.Equal(2, list.Count);
        Assert.Same(listener, list[0]);
        Assert.Same(target, list[1]);
        Assert.Null(manager[source]);
    }

    [Fact]
    public void Purge_InvokeEmptyNoPurgeAllEmptyListenerListData_ReturnsTrue()
    {
        var manager = new SubWeakEventManager();
        object? expectedSource = null;
        int stopListeningCallCount = 0;
        manager.StopListeningAction = (source) =>
        {
            Assert.Same(expectedSource, source);
            stopListeningCallCount++;
        };
        var source = new object();

        var list = new ListenerList();

        // Purge.
        expectedSource = source;
        Assert.True(manager.Purge(source, list, false));
        Assert.Equal(1, stopListeningCallCount);
        Assert.True(list.IsEmpty);
        Assert.Equal(0, list.Count);
        Assert.Null(manager[source]);
        
        // Purge again.
        expectedSource = source;
        Assert.True(manager.Purge(source, list, false));
        Assert.Equal(2, stopListeningCallCount);
        Assert.True(list.IsEmpty);
        Assert.Equal(0, list.Count);
        Assert.Null(manager[source]);
    }

    [Fact]
    public void Purge_InvokeEmptyNoPurgeAllNullData_ThrowsNullReferenceException()
    {
        var manager = new SubWeakEventManager();
        var source = new object();

        // Purge.
        Assert.Throws<NullReferenceException>(() => manager.Purge(source, null!, false));
        Assert.Null(manager[source]);
    }

    [Fact]
    public void Purge_InvokeEmptyNoPurgeAllNotListenerListData_ThrowsInvalidCastException()
    {
        var manager = new SubWeakEventManager();
        var source = new object();

        // Purge.
        Assert.Throws<InvalidCastException>(() => manager.Purge(source, new object(), false));
        Assert.Null(manager[source]);
    }

    [Theory]
    [MemberData(nameof(Purge_Data_TestData))]
    public void Purge_InvokeEmptyPurgeAllWithNoSource_Success(object data)
    {
        var manager = new SubWeakEventManager();
        
        // Purge.
        Assert.False(manager.Purge(null!, data, true));
        
        // Purge again.
        Assert.False(manager.Purge(null!, data, true));
    }

    [Theory]
    [MemberData(nameof(Purge_Data_TestData))]
    public void Purge_InvokeEmptyNoPurgeAllWithNoSource_Success(object data)
    {
        var manager = new SubWeakEventManager();
        
        // Purge.
        Assert.False(manager.Purge(null!, data, true));
        
        // Purge again.
        Assert.False(manager.Purge(null!, data, true));
    }

    public static IEnumerable<object?[]> Remove_TestData()
    {
        yield return new object?[] { new object() };
        yield return new object?[] { 1 };
    }

    [Theory]
    [MemberData(nameof(Remove_TestData))]
    public void Remove_Invoke_Success(object source)
    {
        var manager = new SubWeakEventManager();

        object listener = new object();
        manager[source] = listener;
        Assert.Same(listener, manager[source]);

        // Remove.
        manager.Remove(source);
        Assert.Null(manager[source]);

        // Remove again.
        manager.Remove(source);
        Assert.Null(manager[source]);
    }

    public static IEnumerable<object?[]> Remove_NoSuchSource_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { new object() };
        yield return new object?[] { 1 };
    }

    [Theory]
    [MemberData(nameof(Remove_NoSuchSource_TestData))]
    public void Remove_InvokeNoSuchSource_Success(object source)
    {
        var manager = new SubWeakEventManager();

        // Remove.
        manager.Remove(source);

        // Remove again.
        manager.Remove(source);
    }

    [Fact]
    public void SetCurrentManager_Invoke_GetReturnsExpected()
    {
        var newManager = new SubWeakEventManager();

        // Set.
        SubWeakEventManager.SetCurrentManager(typeof(SentinelType2), newManager);
        Assert.Same(newManager, SubWeakEventManager.GetCurrentManager(typeof(SentinelType2)));

        // Set same.
        SubWeakEventManager.SetCurrentManager(typeof(SentinelType2), newManager);
        Assert.Same(newManager, SubWeakEventManager.GetCurrentManager(typeof(SentinelType2)));

        // Set null.
        SubWeakEventManager.SetCurrentManager(typeof(SentinelType2), null);
        Assert.Null(SubWeakEventManager.GetCurrentManager(typeof(SentinelType2)));
    }

    private class SentinelType2 { }

    [Fact]
    public void SetCurrentManager_NullSource_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("key", () => SubWeakEventManager.SetCurrentManager(null!, new SubWeakEventManager()));
    }
    
    [Fact]
    public void ScheduleCleanup_InvokeWithListeners_Success()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};
        
        var source1 = new object();
        var source2 = new object();
        var listener1 = new CustomWeakEventListener();
        var listener2 = new CustomWeakEventListener();
        var listener3 = new CustomWeakEventListener();
        manager.ProtectedAddListener(source1, listener1);
        manager.ProtectedAddListener(source1, listener2);
        manager.ProtectedAddListener(source2, listener3);

        // Schedule.
        manager.ScheduleCleanup();
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener1, list[0]);
        Assert.Same(listener2, list[1]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source2]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener3, list[0]);

        // Schedule again.
        manager.ScheduleCleanup();
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener1, list[0]);
        Assert.Same(listener2, list[1]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source2]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener3, list[0]);
    }
    
    [Fact]
    public void ScheduleCleanup_InvokeWithHandlers_Success()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};
        
        var source1 = new object();
        var source2 = new object();
        var target1 = new CustomWeakEventListener();
        Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
        Delegate handler2 = (EventHandler)delegate { };
        Delegate handler3 = StaticEventHandler;
        manager.ProtectedAddHandler(source1, handler1);
        manager.ProtectedAddHandler(source1, handler2);
        manager.ProtectedAddHandler(source2, handler3);

        // Schedule.
        manager.ScheduleCleanup();
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target1, list[0]);
        Assert.Throws<InvalidCastException>(() => list[1]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source2]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Throws<InvalidCastException>(() => list[0]);

        // Schedule again.
        manager.ScheduleCleanup();
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(target1, list[0]);
        Assert.Throws<InvalidCastException>(() => list[1]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source2]);
        Assert.Equal(1, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Throws<InvalidCastException>(() => list[0]);
    }
    
    [Fact]
    public void ScheduleCleanup_InvokeWithListenersAndHandlers_Success()
    {
        var manager = new SubWeakEventManager();
        manager.StartListeningAction += (source) => {};
        
        var source1 = new object();
        var source2 = new object();
        var listener1 = new CustomWeakEventListener();
        var listener2 = new CustomWeakEventListener();
        var listener3 = new CustomWeakEventListener();
        var target1 = new CustomWeakEventListener();
        Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
        Delegate handler2 = (EventHandler)delegate { };
        Delegate handler3 = StaticEventHandler;
        manager.ProtectedAddListener(source1, listener1);
        manager.ProtectedAddHandler(source1, handler1);
        manager.ProtectedAddListener(source1, listener2);
        manager.ProtectedAddHandler(source1, handler2);
        manager.ProtectedAddListener(source2, listener3);
        manager.ProtectedAddHandler(source2, handler3);

        // Schedule.
        manager.ScheduleCleanup();
        SubWeakEventManager.ListenerList list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(4, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener1, list[0]);
        Assert.Same(target1, list[1]);
        Assert.Same(listener2, list[2]);
        Assert.Throws<InvalidCastException>(() => list[3]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source2]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener3, list[0]);
        Assert.Throws<InvalidCastException>(() => list[1]);

        // Schedule again.
        manager.ScheduleCleanup();
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source1]);
        Assert.Equal(4, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener1, list[0]);
        Assert.Same(target1, list[1]);
        Assert.Same(listener2, list[2]);
        Assert.Throws<InvalidCastException>(() => list[3]);
        list = Assert.IsType<SubWeakEventManager.ListenerList>(manager[source2]);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsEmpty);
        Assert.Same(listener3, list[0]);
        Assert.Throws<InvalidCastException>(() => list[1]);
    }
    
    [Fact]
    public void ScheduleCleanup_InvokeEmpty_Success()
    {
        var manager = new SubWeakEventManager();

        // Schedule
        manager.ScheduleCleanup();

        // Schedule again.
        manager.ScheduleCleanup();
    }

    public class ListenerListTests : WeakEventManager
    {
        [Fact]
        public void Ctor_Default()
        {
            var list = new SubListenerList();
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public void Ctor_Int(int capacity)
        {
            var list = new SubListenerList(capacity);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        [Fact]
        public void Empty_Get_Success()
        {
            ListenerList list = ListenerList.Empty;
            Assert.NotNull(list);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
            Assert.Same(list, ListenerList.Empty);
        }

        [Fact]
        public void PrepareForWriting_InvokeEmptyInUse_ClonesList()
        {
            var originalList = new ListenerList();
            ListenerList list = originalList;

            // Begin use.
            list.BeginUse();

            // Invoke.
            Assert.True(ListenerList.PrepareForWriting(ref list));
            Assert.NotSame(originalList, list);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);

            // Invoke again.
            Assert.False(ListenerList.PrepareForWriting(ref list));
            Assert.NotSame(originalList, list);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        [Fact]
        public void PrepareForWriting_InvokeEmptyNotInUse_DoesNotCloneList()
        {
            var originalList = new ListenerList();

            // Invoke.
            ListenerList list = originalList;
            Assert.False(ListenerList.PrepareForWriting(ref list));
            Assert.Same(originalList, list);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);

            // Invoke again.
            ListenerList newList = list;
            Assert.False(ListenerList.PrepareForWriting(ref list));
            Assert.Same(originalList, list);
            Assert.Same(newList, list);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        [Fact]
        public void PrepareForWriting_InvokeEmptyNoLongerInUse_DoesNotCloneList()
        {
            var originalList = new ListenerList();
            ListenerList list = originalList;

            // Begin use.
            list.BeginUse();

            // End use.
            list.EndUse();

            // Invoke.
            Assert.False(ListenerList.PrepareForWriting(ref list));
            Assert.Same(originalList, list);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);

            // Invoke again.
            Assert.False(ListenerList.PrepareForWriting(ref list));
            Assert.Same(originalList, list);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        [Fact]
        public void PrepareForWriting_InvokeNotEmptyNotInUse_DoesNotCloneList()
        {
            var originalList = new ListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();

            originalList.Add(listener1);
            originalList.Add(listener2);

            // Invoke.
            ListenerList list = originalList;
            Assert.False(ListenerList.PrepareForWriting(ref list));
            Assert.Same(originalList, list);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener2, list[1]);

            // Invoke again.
            ListenerList newList = list;
            Assert.False(ListenerList.PrepareForWriting(ref list));
            Assert.Same(originalList, list);
            Assert.Same(newList, list);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener2, list[1]);
        }

        [Fact]
        public void PrepareForWriting_InvokeNotEmptyInUse_ClonesList()
        {
            var originalList = new ListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();

            originalList.Add(listener1);
            originalList.Add(listener2);

            // Begin use.
            originalList.BeginUse();

            // Invoke.
            ListenerList list = originalList;
            Assert.True(ListenerList.PrepareForWriting(ref list));
            Assert.NotSame(originalList, list);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener2, list[1]);

            // Invoke again.
            ListenerList newList = list;
            Assert.False(ListenerList.PrepareForWriting(ref list));
            Assert.Same(newList, list);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener2, list[1]);
        }

        [Fact]
        public void PrepareForWriting_InvokeNotEmptyNoLongerInUse_DoesNotCloneList()
        {
            var originalList = new ListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();

            originalList.Add(listener1);
            originalList.Add(listener2);

            // Begin use.
            originalList.BeginUse();

            // End use.
            originalList.EndUse();

            // Invoke.
            ListenerList list = originalList;
            Assert.False(ListenerList.PrepareForWriting(ref list));
            Assert.Same(originalList, list);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener2, list[1]);

            // Invoke again.
            Assert.False(ListenerList.PrepareForWriting(ref list));
            Assert.Same(originalList, list);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener2, list[1]);
        }

        public static IEnumerable<object?[]> PrepareForWriting_CustomClone_TestData()
        {
            yield return new object?[] { null };
            yield return new object?[] { new SubListenerList() };
        }

        [Theory]
        [MemberData(nameof(PrepareForWriting_CustomClone_TestData))]
        public void PrepareForWriting_CustomCloneInUse_Success(object result)
        {
            var originalList = new CustomListenerList();
            int cloneCallCount = 0;
            originalList.CloneAction = () =>
            {
                cloneCallCount++;
                return (ListenerList)result;
            };

            ListenerList list = originalList;
            list.BeginUse();

            // Invoke.
            Assert.True(ListenerList.PrepareForWriting(ref list));
            Assert.NotSame(originalList, list);
            Assert.Same(result, list);
            Assert.Equal(1, cloneCallCount);
        }

        [Fact]
        public void PrepareForWriting_NullList_ThrowsArgumentNullException()
        {
            ListenerList? list = null;
            // TODO: this should throw ANE.
            //Assert.Throws<ArgumentNullException>("list", () => ListenerList.PrepareForWriting(ref list));
            Assert.Throws<NullReferenceException>(() => ListenerList.PrepareForWriting(ref list));
        }

        [Fact]
        public void Item_GetListener_ReturnsExpected()
        {
            var list = new SubListenerList();
            var listener1 = new CustomWeakEventListener();
            list.Add(listener1);
            Assert.Same(listener1, list[0]);
        }

        [Fact]
        public void Item_GetHandlerIWeakEventListenerTarget_ReturnsExpected()
        {
            var list = new SubListenerList();
            var target = new CustomWeakEventListener();
            Delegate handler = Delegate.CreateDelegate(typeof(EventHandler), target, nameof(CustomWeakEventListener.Handler));
            Assert.NotNull(handler.Target);
            Assert.IsAssignableFrom<IWeakEventListener>(handler.Target);

            list.AddHandler(handler);
            Assert.Same(target, list[0]);
        }

        [Fact]
        public void Item_GetHandlerStatic_ThrowsInvalidCastException()
        {
            var list = new SubListenerList();
            static void Handler(object sender, EventArgs e) { }
            Delegate handler = Handler;
            Assert.Null(handler.Target);

            list.AddHandler(handler);
            Assert.Throws<InvalidCastException>(() => list[0]);
        }

        [Fact]
        public void Item_GetHandlerNotIWeakEventListenerTarget_ThrowsInvalidCastException()
        {
            var list = new SubListenerList();
            Delegate handler = (EventHandler)delegate { };
            Assert.NotNull(handler.Target);
            Assert.False(handler.Target is IWeakEventListener);

            list.AddHandler(handler);
            Assert.Throws<InvalidCastException>(() => list[0]);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public void Item_GetWithCapacity_ReturnsExpected(int capacity)
        {
            var list = new SubListenerList(capacity);
            var listener1 = new CustomWeakEventListener();
            list.Add(listener1);
            Assert.Same(listener1, list[0]);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void Item_GetInvalidIndexEmpty_ThrowsArgumentOutOfRangeException(int index)
        {
            var list = new SubListenerList();
            Assert.Throws<ArgumentOutOfRangeException>("index", () => list[index]);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public void Item_GetInvalidIndexEmptyWithCapacity_ReturnsExpected(int capacity)
        {
            var list = new SubListenerList(capacity);
            Assert.Throws<ArgumentOutOfRangeException>("index", () => list[-1]);
            Assert.Throws<ArgumentOutOfRangeException>("index", () => list[0]);
            Assert.Throws<ArgumentOutOfRangeException>("index", () => list[1]);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        [InlineData(2)]
        public void Item_GetInvalidIndexNotEmpty_ThrowsArgumentOutOfRangeException(int index)
        {
            var list = new SubListenerList();
            list.Add(new CustomWeakEventListener());
            Assert.Throws<ArgumentOutOfRangeException>("index", () => list[index]);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public void Item_GetInvalidIndexNotEmptyWithCapacity_ThrowsArgumentOutOfRangeException(int capacity)
        {
            var list = new SubListenerList(capacity);
            list.Add(new CustomWeakEventListener());
            Assert.Throws<ArgumentOutOfRangeException>("index", () => list[-1]);
            Assert.Throws<ArgumentOutOfRangeException>("index", () => list[1]);
            Assert.Throws<ArgumentOutOfRangeException>("index", () => list[2]);
        }

        [Fact]
        public void Add_Invoke_Success()
        {
            var list = new SubListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();
            var listener3 = new CustomWeakEventListener();
            var listener4 = new CustomWeakEventListener();
            var listener5 = new CustomWeakEventListener();
            var listener6 = new CustomWeakEventListener();

            // Add once.
            list.Add(listener1);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);

            // Add again.
            list.Add(listener1);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener1, list[1]);

            // Add third.
            list.Add(null);
            Assert.Equal(3, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener1, list[1]);
            Assert.Throws<InvalidCastException>(() => list[2]);

            // Add fourth.
            list.Add(listener2);
            Assert.Equal(4, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener1, list[1]);
            Assert.Throws<InvalidCastException>(() => list[2]);
            Assert.Same(listener2, list[3]);

            // Add fifth.
            list.Add(listener3);
            Assert.Equal(5, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener1, list[1]);
            Assert.Throws<InvalidCastException>(() => list[2]);
            Assert.Same(listener2, list[3]);
            Assert.Same(listener3, list[4]);

            // Add sixth.
            list.Add(listener4);
            Assert.Equal(6, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener1, list[1]);
            Assert.Throws<InvalidCastException>(() => list[2]);
            Assert.Same(listener2, list[3]);
            Assert.Same(listener3, list[4]);
            Assert.Same(listener4, list[5]);

            // Add seventh.
            list.Add(listener5);
            Assert.Equal(7, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener1, list[1]);
            Assert.Throws<InvalidCastException>(() => list[2]);
            Assert.Same(listener2, list[3]);
            Assert.Same(listener3, list[4]);
            Assert.Same(listener4, list[5]);
            Assert.Same(listener5, list[6]);

            // Add eighth.
            list.Add(listener6);
            Assert.Equal(8, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener1, list[1]);
            Assert.Throws<InvalidCastException>(() => list[2]);
            Assert.Same(listener2, list[3]);
            Assert.Same(listener3, list[4]);
            Assert.Same(listener4, list[5]);
            Assert.Same(listener5, list[6]);
            Assert.Same(listener6, list[7]);
        }

        // TODO: this causes a crash.
#if false
        [Fact]
        public void Add_InvokeInUse_Success()
        {
            var list = new SubListenerList();
            var listener = new SubListener();

            // Begin use.
            Assert.False(list.BeginUse());

            // Add.
            list.Add(listener);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener, list[0]);
        }
#endif

        [Fact]
        public void Add_InvokeNoLongerInUse_Success()
        {
            var list = new SubListenerList();
            var listener = new CustomWeakEventListener();

            // Begin use.
            Assert.False(list.BeginUse());

            // End use.
            list.EndUse();

            // Add.
            list.Add(listener);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener, list[0]);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(6)]
        [InlineData(7)]
        public void Add_InvokeDifferentSizes_Success(int count)
        {
            // Test internal FrugalList implementation.
            var listeners = new List<CustomWeakEventListener>();
            for (int i = 0; i < count; i++)
            {
                listeners.Add(new CustomWeakEventListener());
            }

            // Add.
            var list = new SubListenerList();
            for (int i = 0; i < count; i++)
            {
                list.Add(listeners[i]);
                Assert.Equal(i + 1, list.Count);
                Assert.False(list.IsEmpty);
                for (int j = 0; j <= i; j++)
                {
                    Assert.Same(listeners[j], list[j]);
                }
            }
        }

        [Fact]
        public void AddHandler_Invoke_Success()
        {
            var list = new SubListenerList();
            var target1 = new CustomWeakEventListener();
            var target2 = new CustomWeakEventListener();
            var target3 = new CustomWeakEventListener();
            Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
            Delegate handler2 = WeakEventManagerTests.StaticEventHandler;
            Delegate handler3 = (EventHandler)delegate { };
            Delegate handler4 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));
            Delegate handler5 = Delegate.CreateDelegate(typeof(EventHandler), target3, nameof(CustomWeakEventListener.Handler));

            // Add once.
            list.AddHandler(handler1);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target1, list[0]);

            // Add again.
            list.AddHandler(handler1);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target1, list[0]);
            Assert.Same(target1, list[1]);

            // Add third.
            list.AddHandler(handler2);
            Assert.Equal(3, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target1, list[0]);
            Assert.Same(target1, list[1]);
            Assert.Throws<InvalidCastException>(() => list[2]);

            // Add again.
            list.AddHandler(handler2);
            Assert.Equal(4, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target1, list[0]);
            Assert.Same(target1, list[1]);
            Assert.Throws<InvalidCastException>(() => list[2]);
            Assert.Throws<InvalidCastException>(() => list[3]);

            // Add fifth.
            list.AddHandler(handler3);
            Assert.Equal(5, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target1, list[0]);
            Assert.Same(target1, list[1]);
            Assert.Throws<InvalidCastException>(() => list[2]);
            Assert.Throws<InvalidCastException>(() => list[3]);
            Assert.Throws<InvalidCastException>(() => list[4]);

            // Add again.
            list.AddHandler(handler3);
            Assert.Equal(6, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target1, list[0]);
            Assert.Same(target1, list[1]);
            Assert.Throws<InvalidCastException>(() => list[2]);
            Assert.Throws<InvalidCastException>(() => list[3]);
            Assert.Throws<InvalidCastException>(() => list[4]);
            Assert.Throws<InvalidCastException>(() => list[5]);

            // Add seventh.
            list.AddHandler(handler4);
            Assert.Equal(7, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target1, list[0]);
            Assert.Same(target1, list[1]);
            Assert.Throws<InvalidCastException>(() => list[2]);
            Assert.Throws<InvalidCastException>(() => list[3]);
            Assert.Throws<InvalidCastException>(() => list[4]);
            Assert.Throws<InvalidCastException>(() => list[5]);
            Assert.Same(target2, list[6]);

            // Add eighth.
            list.AddHandler(handler5);
            Assert.Equal(8, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target1, list[0]);
            Assert.Same(target1, list[1]);
            Assert.Throws<InvalidCastException>(() => list[2]);
            Assert.Throws<InvalidCastException>(() => list[3]);
            Assert.Throws<InvalidCastException>(() => list[4]);
            Assert.Throws<InvalidCastException>(() => list[5]);
            Assert.Same(target2, list[6]);
            Assert.Same(target3, list[7]);
        }

        [Fact]
        public void AddHandler_InvokeSameTargetMultipleTimes_Success()
        {
            var list = new SubListenerList();

            var target1 = new CustomWeakEventListener();
            var target2 = new CustomWeakEventListener();
            Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
            Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));
            Delegate handler3 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.SecondHandler));
            Delegate handler4 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
            Delegate handler5 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.SecondHandler));
            Delegate handler6 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.SecondHandler));
            Delegate handler7 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));

            // Add first.
            list.AddHandler(handler1);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target1, list[0]);

            // Add second.
            list.AddHandler(handler2);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target1, list[0]);
            Assert.Same(target2, list[1]);

            // Add third.
            list.AddHandler(handler3);
            Assert.Equal(3, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target1, list[0]);
            Assert.Same(target2, list[1]);
            Assert.Same(target1, list[2]);

            // Add fourth.
            list.AddHandler(handler4);
            Assert.Equal(4, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target1, list[0]);
            Assert.Same(target2, list[1]);
            Assert.Same(target1, list[2]);
            Assert.Same(target1, list[3]);

            // Add fifth.
            list.AddHandler(handler5);
            Assert.Equal(5, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target1, list[0]);
            Assert.Same(target2, list[1]);
            Assert.Same(target1, list[2]);
            Assert.Same(target1, list[3]);
            Assert.Same(target1, list[4]);

            // Add sixth.
            list.AddHandler(handler6);
            Assert.Equal(6, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target1, list[0]);
            Assert.Same(target2, list[1]);
            Assert.Same(target1, list[2]);
            Assert.Same(target1, list[3]);
            Assert.Same(target1, list[4]);
            Assert.Same(target2, list[5]);

            // Add seventh.
            list.AddHandler(handler7);
            Assert.Equal(7, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target1, list[0]);
            Assert.Same(target2, list[1]);
            Assert.Same(target1, list[2]);
            Assert.Same(target1, list[3]);
            Assert.Same(target1, list[4]);
            Assert.Same(target2, list[5]);
            Assert.Same(target2, list[6]);
        }

        [Fact]
        public void AddHandler_InvokeMultipleTimesIWeakEventListenerTarget_Success()
        {
            var list = new SubListenerList();
            var target = new CustomWeakEventListener();
            Delegate handler = Delegate.CreateDelegate(typeof(EventHandler), target, nameof(CustomWeakEventListener.Handler));

            // Add once.
            list.AddHandler(handler);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target, list[0]);

            // Add twice.
            list.AddHandler(handler);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target, list[0]);
            Assert.Same(target, list[1]);

            // Add three times.
            list.AddHandler(handler);
            Assert.Equal(3, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target, list[0]);
            Assert.Same(target, list[1]);
            Assert.Same(target, list[2]);
        }

        [Fact]
        public void AddHandler_InvokeMultipleTimesStaticTarget_Success()
        {
            var list = new SubListenerList();
            Delegate handler = WeakEventManagerTests.StaticEventHandler;

            // Add once.
            list.AddHandler(handler);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);

            // Add twice.
            list.AddHandler(handler);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);
            Assert.Throws<InvalidCastException>(() => list[1]);

            // Add three times.
            list.AddHandler(handler);
            Assert.Equal(3, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);
            Assert.Throws<InvalidCastException>(() => list[1]);
            Assert.Throws<InvalidCastException>(() => list[2]);
        }

        [Fact]
        public void AddHandler_InvokeMultipleTimesNotIWeakEventListenerTarget_Success()
        {
            var list = new SubListenerList();
            Delegate handler = (EventHandler)delegate { };

            // Add once.
            list.AddHandler(handler);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);

            // Add twice.
            list.AddHandler(handler);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);
            Assert.Throws<InvalidCastException>(() => list[1]);

            // Add three times.
            list.AddHandler(handler);
            Assert.Equal(3, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);
            Assert.Throws<InvalidCastException>(() => list[1]);
            Assert.Throws<InvalidCastException>(() => list[2]);
        }

        // TODO: this causes a crash.
#if false
        [Fact]
        public void AddHandler_InvokeInUse_Success()
        {
            var list = new SubListenerList();
            Delegate handler = (EventHandler)delegate { };

            // Begin use.
            Assert.False(list.BeginUse());

            // Add.
            list.AddHandler(handler);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);
        }
#endif

        [Fact]
        public void AddHandler_InvokeNoLongerInUse_Success()
        {
            var list = new SubListenerList();
            Delegate handler = (EventHandler)delegate { };

            // Begin use.
            Assert.False(list.BeginUse());

            // End use.
            list.EndUse();

            // Add.
            list.AddHandler(handler);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(6)]
        [InlineData(7)]
        public void AddHandler_InvokeDifferentSizes_Success(int count)
        {
            // Test internal FrugalList implementation.
            var handlers = new List<Delegate>();
            for (int i = 0; i < count; i++)
            {
                handlers.Add(Delegate.CreateDelegate(typeof(EventHandler), new CustomWeakEventListener(), nameof(CustomWeakEventListener.Handler)));
            }

            // Add.
            var list = new SubListenerList();
            for (int i = 0; i < count; i++)
            {
                list.AddHandler(handlers[i]);
                Assert.Equal(i + 1, list.Count);
                Assert.False(list.IsEmpty);
                for (int j = 0; j <= i; j++)
                {
                    Assert.Same(handlers[j].Target, list[j]);
                }
            }
        }

        [Fact]
        public void AddHandler_NullHandler_ThrowsArgumentNullException()
        {
            var list = new SubListenerList();
            // TODO: this should throw ANE.
            //Assert.Throws<ArgumentNullException>("handler", () => list.AddHandler(null));
            Assert.Throws<NullReferenceException>(() => list.AddHandler(null));
        }

        [Fact]
        public void BeginUse_Invoke_Success()
        {
            var list = new SubListenerList();

            // Call once.
            Assert.False(list.BeginUse());

            // Call again.
            Assert.True(list.BeginUse());
        }

        [Fact]
        public void Clone_InvokeEmptyToEmpty_Success()
        {
            var list = new SubListenerList();

            ListenerList newList = Assert.IsType<ListenerList>(list.Clone());
            Assert.NotSame(list, newList);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
            Assert.Equal(0, newList.Count);
            Assert.True(newList.IsEmpty);
        }

        [Fact]
        public void Clone_NotEmptyWithListenersToEmpty_Success()
        {
            var list = new SubListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();
            list.Add(listener1);
            list.Add(listener2);

            ListenerList newList = Assert.IsType<ListenerList>(list.Clone());
            Assert.NotSame(list, newList);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener2, list[1]);
            Assert.Equal(2, newList.Count);
            Assert.False(newList.IsEmpty);
            Assert.Same(listener1, newList[0]);
            Assert.Same(listener2, newList[1]);
        }

        [Fact]
        public void Clone_NotEmptyWithHandlersToEmpty_Success()
        {
            var list = new SubListenerList();
            var target1 = new CustomWeakEventListener();
            Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
            var target2 = new CustomWeakEventListener();
            Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));
            Delegate handler3 = (EventHandler)delegate { };
            Delegate handler4 = WeakEventManagerTests.StaticEventHandler;

            list.AddHandler(handler1);
            list.AddHandler(handler2);
            list.AddHandler(handler3);
            list.AddHandler(handler4);

            ListenerList newList = Assert.IsType<ListenerList>(list.Clone());
            Assert.NotSame(list, newList);
            Assert.Equal(4, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target1, list[0]);
            Assert.Same(target2, list[1]);
            Assert.Throws<InvalidCastException>(() => list[2]);
            Assert.Throws<InvalidCastException>(() => list[3]);
            Assert.Equal(4, newList.Count);
            Assert.False(newList.IsEmpty);
            Assert.Same(target1, newList[0]);
            Assert.Same(target2, newList[1]);
            Assert.Throws<InvalidCastException>(() => newList[2]);
            Assert.Throws<InvalidCastException>(() => newList[3]);
        }

        [Fact]
        public void Clone_NotEmptyWithListenersAndHandlersToEmpty_Success()
        {
            var list = new SubListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();
            var target1 = new CustomWeakEventListener();
            Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
            var target2 = new CustomWeakEventListener();
            Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));
            Delegate handler3 = (EventHandler)delegate { };
            Delegate handler4 = WeakEventManagerTests.StaticEventHandler;

            list.Add(listener1);
            list.AddHandler(handler1);
            list.Add(listener2);
            list.AddHandler(handler2);
            list.AddHandler(handler3);
            list.AddHandler(handler4);

            ListenerList newList = Assert.IsType<ListenerList>(list.Clone());
            Assert.NotSame(list, newList);
            Assert.Equal(6, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(target1, list[1]);
            Assert.Same(listener2, list[2]);
            Assert.Same(target2, list[3]);
            Assert.Throws<InvalidCastException>(() => list[4]);
            Assert.Throws<InvalidCastException>(() => list[5]);
            Assert.Equal(6, newList.Count);
            Assert.False(newList.IsEmpty);
            Assert.Same(listener1, newList[0]);
            Assert.Same(target1, newList[1]);
            Assert.Same(listener2, newList[2]);
            Assert.Same(target2, newList[3]);
            Assert.Throws<InvalidCastException>(() => newList[4]);
            Assert.Throws<InvalidCastException>(() => newList[5]);
        }

        [Fact]
        public void CopyTo_InvokeEmptyToEmpty_Success()
        {
            var list = new SubListenerList();
            var newList = new ListenerList();

            list.CopyTo(newList);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
            Assert.Equal(0, newList.Count);
            Assert.True(newList.IsEmpty);
        }

        [Fact]
        public void CopyTo_InvokeEmptyToNotEmptyWithListeners_Success()
        {
            var list = new SubListenerList();
            var newList = new ListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();

            newList.Add(listener1);
            newList.Add(listener2);

            list.CopyTo(newList);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
            Assert.Equal(2, newList.Count);
            Assert.False(newList.IsEmpty);
            Assert.Equal(listener1, newList[0]);
            Assert.Equal(listener2, newList[1]);
        }

        [Fact]
        public void CopyTo_InvokeEmptyToNotEmptyWithHandlers_Success()
        {
            var list = new SubListenerList();
            var newList = new ListenerList();
            var target1 = new CustomWeakEventListener();
            Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
            var target2 = new CustomWeakEventListener();
            Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));
            Delegate handler3 = (EventHandler)delegate { };
            Delegate handler4 = WeakEventManagerTests.StaticEventHandler;

            newList.AddHandler(handler1);
            newList.AddHandler(handler2);
            newList.AddHandler(handler3);
            newList.AddHandler(handler4);

            list.CopyTo(newList);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
            Assert.Equal(4, newList.Count);
            Assert.False(newList.IsEmpty);
            Assert.Equal(target1, newList[0]);
            Assert.Equal(target2, newList[1]);
            Assert.Throws<InvalidCastException>(() => newList[2]);
            Assert.Throws<InvalidCastException>(() => newList[3]);
        }

        [Fact]
        public void CopyTo_InvokeEmptyToNotEmptyWithListenersAndHandlers_Success()
        {
            var list = new SubListenerList();
            var newList = new ListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();
            var target1 = new CustomWeakEventListener();
            Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
            var target2 = new CustomWeakEventListener();
            Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));
            Delegate handler3 = (EventHandler)delegate { };
            Delegate handler4 = WeakEventManagerTests.StaticEventHandler;

            newList.Add(listener1);
            newList.AddHandler(handler1);
            newList.Add(listener2);
            newList.AddHandler(handler2);
            newList.AddHandler(handler3);
            newList.AddHandler(handler4);

            list.CopyTo(newList);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
            Assert.Equal(6, newList.Count);
            Assert.False(newList.IsEmpty);
            Assert.Same(listener1, newList[0]);
            Assert.Equal(target1, newList[1]);
            Assert.Same(listener2, newList[2]);
            Assert.Equal(target2, newList[3]);
            Assert.Throws<InvalidCastException>(() => newList[4]);
            Assert.Throws<InvalidCastException>(() => newList[5]);
        }

        [Fact]
        public void CopyTo_InvokeNotEmptyWithListenersToEmpty_Success()
        {
            var list = new SubListenerList();
            var newList = new ListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();
            list.Add(listener1);
            list.Add(listener2);

            list.CopyTo(newList);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener2, list[1]);
            Assert.Equal(2, newList.Count);
            Assert.False(newList.IsEmpty);
            Assert.Same(listener1, newList[0]);
            Assert.Same(listener2, newList[1]);
        }

        [Fact]
        public void CopyTo_InvokeNotEmptyWithHandlersToEmpty_Success()
        {
            var list = new SubListenerList();
            var newList = new ListenerList();
            var target1 = new CustomWeakEventListener();
            Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
            var target2 = new CustomWeakEventListener();
            Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));
            Delegate handler3 = (EventHandler)delegate { };
            Delegate handler4 = WeakEventManagerTests.StaticEventHandler;

            list.AddHandler(handler1);
            list.AddHandler(handler2);
            list.AddHandler(handler3);
            list.AddHandler(handler4);

            list.CopyTo(newList);
            Assert.Equal(4, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target1, list[0]);
            Assert.Same(target2, list[1]);
            Assert.Throws<InvalidCastException>(() => list[2]);
            Assert.Throws<InvalidCastException>(() => list[3]);
            Assert.Equal(4, newList.Count);
            Assert.False(newList.IsEmpty);
            Assert.Same(target1, newList[0]);
            Assert.Same(target2, newList[1]);
            Assert.Throws<InvalidCastException>(() => newList[2]);
            Assert.Throws<InvalidCastException>(() => newList[3]);
        }

        [Fact]
        public void CopyTo_InvokeNotEmptyWithListenersAndHandlersToEmpty_Success()
        {
            var list = new SubListenerList();
            var newList = new ListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();
            var target1 = new CustomWeakEventListener();
            Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
            var target2 = new CustomWeakEventListener();
            Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));
            Delegate handler3 = (EventHandler)delegate { };
            Delegate handler4 = WeakEventManagerTests.StaticEventHandler;

            list.Add(listener1);
            list.AddHandler(handler1);
            list.Add(listener2);
            list.AddHandler(handler2);
            list.AddHandler(handler3);
            list.AddHandler(handler4);

            list.CopyTo(newList);
            Assert.Equal(6, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(target1, list[1]);
            Assert.Same(listener2, list[2]);
            Assert.Same(target2, list[3]);
            Assert.Throws<InvalidCastException>(() => list[4]);
            Assert.Throws<InvalidCastException>(() => list[5]);
            Assert.Equal(6, newList.Count);
            Assert.False(newList.IsEmpty);
            Assert.Same(listener1, newList[0]);
            Assert.Same(target1, newList[1]);
            Assert.Same(listener2, newList[2]);
            Assert.Same(target2, newList[3]);
            Assert.Throws<InvalidCastException>(() => newList[4]);
            Assert.Throws<InvalidCastException>(() => newList[5]);
        }

        [Fact]
        public void CopyTo_InvokeNotEmptyToNotEmpty_Success()
        {
            var list = new SubListenerList();
            var newList = new ListenerList();
            var listener1 = new CustomWeakEventListener();
            var target1 = new CustomWeakEventListener();
            Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
            var listener2 = new CustomWeakEventListener();
            var target2 = new CustomWeakEventListener();
            Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));

            list.Add(listener1);
            list.AddHandler(handler1);
            newList.Add(listener2);
            newList.AddHandler(handler2);

            list.CopyTo(newList);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(target1, list[1]);
            Assert.Equal(4, newList.Count);
            Assert.False(newList.IsEmpty);
            Assert.Same(listener2, newList[0]);
            Assert.Same(target2, newList[1]);
            Assert.Same(listener1, newList[2]);
            Assert.Same(target1, newList[3]);
        }

        [Fact]
        public void CopyTo_InvokeSameNewListEmpty_Success()
        {
            var list = new SubListenerList();
            list.CopyTo(list);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        [Fact]
        public void CopyTo_InvokeSameNewListNotEmpty_Success()
        {
            var list = new SubListenerList();
            var listener1 = new CustomWeakEventListener();
            list.Add(listener1);

            list.CopyTo(list);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener1, list[1]);
        }

        [Fact]
        public void CopyTo_NullNewListEmpty_ThrowsArgumentNullException()
        {
            var list = new SubListenerList();
            // TODO: this should throw ANE.
            //Assert.Throws<ArgumentNullException>("newList", () => list.CopyTo(null!));
            list.CopyTo(null!);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        [Fact]
        public void CopyTo_NullNewListNotEmpty_ThrowsArgumentNullException()
        {
            var list = new SubListenerList();
            var listener1 = new CustomWeakEventListener();
            list.Add(listener1);

            // TODO: this should throw ANE.
            //Assert.Throws<ArgumentNullException>("newList", () => list.CopyTo(null!));
            Assert.Throws<NullReferenceException>(() => list.CopyTo(null!));
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
        }

        public static IEnumerable<object?[]> DeliverEvent_TestData()
        {
            yield return new object?[] { null, null, null };
            yield return new object?[] { new object(), new EventArgs(), typeof(int) };
            yield return new object?[] { new object(), EventArgs.Empty, typeof(int) };
        }

        [Theory]
        [MemberData(nameof(DeliverEvent_TestData))]
        public void DeliverEvent_InvokeWithListeners_Success(object sender, EventArgs args, Type managerType)
        {
            var list = new SubListenerList();
            var events = new List<string>();
            var listener1 = new CustomWeakEventListener
            {
                ReceiveWeakEventAction = (t, s, e) =>
                {
                    Assert.Same(managerType, t);
                    Assert.Same(sender, s);
                    Assert.Same(args, e);
                    events.Add("listener1");
                    return true;
                }
            };
            var listener2 = new CustomWeakEventListener
            {
                ReceiveWeakEventAction = (t, s, e) =>
                {
                    Assert.Same(managerType, t);
                    Assert.Same(sender, s);
                    Assert.Same(args, e);
                    events.Add("listener2");
                    return true;
                }
            };
            list.Add(listener1);
            list.Add(listener2);

            Assert.False(list.DeliverEvent(sender, args, managerType));
            Assert.Equal(new string[] { "listener1", "listener2" }, events);
        }

        [Theory]
        [MemberData(nameof(DeliverEvent_TestData))]
        public void DeliverEvent_InvokeWithHandlers_Success(object sender, EventArgs args, Type managerType)
        {
            var list = new SubListenerList();
            var events = new List<string>();
            var listener1 = new CustomWeakEventListener
            {
                HandlerAction = (s, e) =>
                {
                    Assert.Same(sender, s);
                    Assert.Same(args, e);
                    events.Add("handler1");
                }
            };
            var listener2 = new CustomWeakEventListener
            {
                HandlerAction = (s, e) =>
            {
                Assert.Same(sender, s);
                Assert.Same(args, e);
                events.Add("handler2");
            }
            };
            Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), listener1, nameof(CustomWeakEventListener.Handler));
            Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), listener2, nameof(CustomWeakEventListener.Handler));
            list.AddHandler(handler1);
            list.AddHandler(handler2);

            Assert.False(list.DeliverEvent(sender, args, managerType));
            Assert.Equal(new string[] { "handler1", "handler2" }, events);
        }

        [Theory]
        [MemberData(nameof(DeliverEvent_TestData))]
        public void DeliverEvent_InvokeWithListenersAndHandlers_Success(object sender, EventArgs args, Type managerType)
        {
            var list = new SubListenerList();
            var events = new List<string>();
            var listener1 = new CustomWeakEventListener
            {
                HandlerAction = (s, e) =>
            {
                Assert.Same(sender, s);
                Assert.Same(args, e);
                events.Add("handler1");
            }
            };
            var listener2 = new CustomWeakEventListener
            {
                ReceiveWeakEventAction = (t, s, e) =>
            {
                Assert.Same(managerType, t);
                Assert.Same(sender, s);
                Assert.Same(args, e);
                events.Add("listener1");
                return true;
            }
            };
            var listener3 = new CustomWeakEventListener
            {
                HandlerAction = (s, e) =>
            {
                Assert.Same(sender, s);
                Assert.Same(args, e);
                events.Add("handler2");
            }
            };
            var listener4 = new CustomWeakEventListener
            {
                ReceiveWeakEventAction = (t, s, e) =>
            {
                Assert.Same(managerType, t);
                Assert.Same(sender, s);
                Assert.Same(args, e);
                events.Add("listener2");
                return true;
            }
            };
            Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), listener1, nameof(CustomWeakEventListener.Handler));
            Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), listener3, nameof(CustomWeakEventListener.Handler));
            list.AddHandler(handler1);
            list.Add(listener2);
            list.AddHandler(handler2);
            list.Add(listener4);

            Assert.False(list.DeliverEvent(sender, args, managerType));
            Assert.Equal(new string[] { "handler1", "listener1", "handler2", "listener2" }, events);
        }

        // TODO: this causes a crash.
#if false
        [Theory]
        [MemberData(nameof(DeliverEvent_TestData))]
        public void DeliverEvent_InvokeWithListenersNotHandled_Success(object sender, EventArgs args, Type managerType)
        {
            var list = new SubListenerList();
            var events = new List<string>();
            var listener1 = new CustomWeakEventListener();
            listener1.ReceiveWeakEventAction = (t, s, e) =>
            {
                Assert.Same(managerType, t);
                Assert.Same(sender, s);
                Assert.Same(args, e);
                events.Add("listener1");
                return false;
            };
            var listener2 = new CustomWeakEventListener();
            listener2.ReceiveWeakEventAction = (t, s, e) =>
            {
                Assert.Same(managerType, t);
                Assert.Same(sender, s);
                Assert.Same(args, e);
                events.Add("listener2");
                return true;
            };
            list.Add(listener1);
            list.Add(listener2);

            Assert.False(list.DeliverEvent(sender, args, managerType));
            Assert.Equal(new string[] { "listener1", "listener2" }, events);
        }
#endif

        [Theory]
        [MemberData(nameof(DeliverEvent_TestData))]
        public void DeliverEvent_InvokeWithInvalidHandlerAction_ThrowsInvalidCastException(object sender, EventArgs args, Type managerType)
        {
            var list = new SubListenerList();
            Delegate handler = () => {};
            list.AddHandler(handler);

            Assert.Throws<InvalidCastException>(() => list.DeliverEvent(sender, args, managerType));
        }

        [Theory]
        [MemberData(nameof(DeliverEvent_TestData))]
        public void DeliverEvent_InvokeWithInvalidHandlerFunc_ThrowsInvalidCastException(object sender, EventArgs args, Type managerType)
        {
            var list = new SubListenerList();
            static bool EventHandler(object sender, EventArgs args) => true;
            list.AddHandler(EventHandler);

            Assert.Throws<InvalidCastException>(() => list.DeliverEvent(sender, args, managerType));
        }

        [Theory]
        [MemberData(nameof(DeliverEvent_TestData))]
        public void DeliverEvent_InvokeWithInvalidHandlerGenericEventHandler_ThrowsInvalidCastException(object sender, EventArgs args, Type managerType)
        {
            var list = new SubListenerList();
            var target = new CustomWeakEventListener();
            Delegate handler = Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), target, nameof(CustomWeakEventListener.Handler));
            list.AddHandler(handler);

            Assert.Throws<InvalidCastException>(() => list.DeliverEvent(sender, args, managerType));
        }

        [Theory]
        [MemberData(nameof(DeliverEvent_TestData))]
        public void DeliverEvent_InvokeEmpty_ReturnsFalse(object sender, EventArgs args, Type managerType)
        {
            var list = new SubListenerList();
            Assert.False(list.DeliverEvent(sender, args, managerType));
        }

        [Fact]
        public void EndUse_InvokeBegan_Success()
        {
            var list = new SubListenerList();

            // Begin.
            Assert.False(list.BeginUse());

            // End.
            list.EndUse();

            // Begin.
            Assert.False(list.BeginUse());

            // Begin.
            Assert.True(list.BeginUse());
        }

        [Fact]
        public void EndUse_InvokeNotBegan_Success()
        {
            var list = new SubListenerList();

            // Call once.
            list.EndUse();

            // Call again.
            list.EndUse();

            // Begin.
            Assert.True(list.BeginUse());

            // Begin.
            Assert.True(list.BeginUse());

            // Begin.
            Assert.False(list.BeginUse());

            // Begin.
            Assert.True(list.BeginUse());
        }

        [Fact]
        public void Purge_InvokeEmpty_Success()
        {
            var list = new SubListenerList();

            Assert.False(list.Purge());
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        // TODO: this causes a crash.
#if false
        [Fact]
        public void Purge_InvokeEmptyInUse_Success()
        {
            var list = new SubListenerList();
            Assert.False(list.BeginUse());

            Assert.False(list.Purge());
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }
#endif

        [Fact]
        public void Purge_InvokeNotEmptyWithListeners_Success()
        {
            var list = new SubListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();
            list.Add(listener1);
            list.Add(listener2);

            Assert.False(list.Purge());
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener2, list[1]);
        }
        
        [Fact]
        public void Purge_InvokeNotEmptyWithHandlers_Success()
        {
            var list = new SubListenerList();
            var target1 = new CustomWeakEventListener();
            Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
            Delegate handler2 = (EventHandler)delegate { };
            Delegate handler3 = WeakEventManagerTests.StaticEventHandler;
            list.AddHandler(handler1);
            list.AddHandler(handler2);
            list.AddHandler(handler3);

            Assert.False(list.Purge());
            Assert.Equal(3, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Equal(target1, list[0]);
            Assert.Same(target1, list[0]);
            Assert.Throws<InvalidCastException>(() => list[1]);
            Assert.Throws<InvalidCastException>(() => list[2]);
        }

        [Fact]
        public void Purge_InvokeNotEmptyWithListenersAndHandlers_Success()
        {
            var list = new SubListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();
            var target1 = new CustomWeakEventListener();
            Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
            Delegate handler2 = (EventHandler)delegate { };
            Delegate handler3 = WeakEventManagerTests.StaticEventHandler;
            list.Add(listener1);
            list.AddHandler(handler1);
            list.Add(listener2);
            list.AddHandler(handler2);
            list.AddHandler(handler3);

            Assert.False(list.Purge());
            Assert.Equal(5, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(target1, list[1]);
            Assert.Same(listener2, list[2]);
            Assert.Throws<InvalidCastException>(() => list[3]);
            Assert.Throws<InvalidCastException>(() => list[4]);
        }

        // TODO: this causes a crash.
#if false
        [Fact]
        public void Purge_InvokeNotEmptyInUse_Success()
        {
            var list = new SubListenerList();
            var listener1 = new SubListener();
            list.Add(listener1);
            Assert.False(list.BeginUse());

            Assert.False(list.Purge());
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
        }
#endif


        [Fact]
        public void Remove_InvokeEmptyList_Success()
        {
            var list = new SubListenerList();
            var listener = new CustomWeakEventListener();

            list.Remove(listener);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        [Fact]
        public void Remove_InvokeOneItemList_Success()
        {
            var list = new SubListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();

            list.Add(listener1);

            // Remove none.
            list.Remove(listener2);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);

            // Remove last.
            list.Remove(listener1);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);

            // Remove again.
            list.Remove(listener1);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        [Fact]
        public void Remove_InvokeOneItemListWithNull_Success()
        {
            var list = new SubListenerList();
            var listener1 = new CustomWeakEventListener();

            list.Add(null);

            // Remove none.
            list.Remove(listener1);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);

            // Remove last.
            list.Remove(null);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);

            // Remove again.
            list.Remove(null);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);
        }

        [Fact]
        public void Remove_InvokeThreeItemList_Success()
        {
            var list = new SubListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();
            var listener3 = new CustomWeakEventListener();
            var listener4 = new CustomWeakEventListener();

            list.Add(listener1);
            list.Add(listener2);
            list.Add(listener3);

            // Remove none.
            list.Remove(listener4);
            Assert.Equal(3, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener2, list[1]);
            Assert.Same(listener3, list[2]);

            // Remove last.
            list.Remove(listener3);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener2, list[1]);

            // Remove first.
            list.Remove(listener1);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener2, list[0]);

            // Remove last.
            list.Remove(listener2);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);

            // Remove again.
            list.Remove(listener2);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        [Fact]
        public void Remove_InvokeThreeItemListWithNull_Success()
        {
            var list = new SubListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();
            var listener3 = new CustomWeakEventListener();

            list.Add(listener1);
            list.Add(null);
            list.Add(listener2);

            // Remove none.
            list.Remove(listener3);
            Assert.Equal(3, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Throws<InvalidCastException>(() => list[1]);
            Assert.Same(listener2, list[2]);

            // Remove last.
            list.Remove(listener2);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Throws<InvalidCastException>(() => list[1]);

            // Remove first.
            list.Remove(listener1);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);

            // Remove null.
            list.Remove(null);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);

            // Remove again.
            list.Remove(null);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);
        }

        [Fact]
        public void Remove_InvokeFourItemList_Success()
        {
            var list = new SubListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();
            var listener3 = new CustomWeakEventListener();
            var listener4 = new CustomWeakEventListener();
            var listener5 = new CustomWeakEventListener();

            list.Add(listener1);
            list.Add(listener2);
            list.Add(listener3);
            list.Add(listener4);

            // Remove none.
            list.Remove(listener5);
            Assert.Equal(4, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener2, list[1]);
            Assert.Same(listener3, list[2]);
            Assert.Same(listener4, list[3]);

            // Remove last.
            list.Remove(listener4);
            Assert.Equal(3, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener2, list[1]);
            Assert.Same(listener3, list[2]);

            // Remove middle.
            list.Remove(listener2);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener3, list[1]);

            // Remove first.
            list.Remove(listener1);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener3, list[0]);

            // Remove last.
            list.Remove(listener3);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);

            // Remove again.
            list.Remove(listener3);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        [Fact]
        public void Remove_InvokeFourItemListWithNull_Success()
        {
            var list = new SubListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();
            var listener3 = new CustomWeakEventListener();
            var listener4 = new CustomWeakEventListener();

            list.Add(listener1);
            list.Add(null);
            list.Add(listener2);
            list.Add(listener3);

            // Remove none.
            list.Remove(listener4);
            Assert.Equal(4, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Throws<InvalidCastException>(() => list[1]);
            Assert.Same(listener2, list[2]);

            // Remove last.
            list.Remove(listener3);
            Assert.Equal(3, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Throws<InvalidCastException>(() => list[1]);
            Assert.Same(listener2, list[2]);

            // Remove first.
            list.Remove(listener1);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);
            Assert.Same(listener2, list[1]);

            // Remove null.
            list.Remove(null);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);
            Assert.Same(listener2, list[1]);

            // Remove last.
            list.Remove(listener2);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);

            // Remove again.
            list.Remove(listener2);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);
        }

        [Fact]
        public void Remove_InvokeSixItemList_Success()
        {
            var list = new SubListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();
            var listener3 = new CustomWeakEventListener();
            var listener4 = new CustomWeakEventListener();
            var listener5 = new CustomWeakEventListener();
            var listener6 = new CustomWeakEventListener();
            var listener7 = new CustomWeakEventListener();

            list.Add(listener1);
            list.Add(listener2);
            list.Add(listener3);
            list.Add(listener4);
            list.Add(listener5);
            list.Add(listener6);

            // Remove none.
            list.Remove(listener7);
            Assert.Equal(6, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener2, list[1]);
            Assert.Same(listener3, list[2]);
            Assert.Same(listener4, list[3]);
            Assert.Same(listener5, list[4]);
            Assert.Same(listener6, list[5]);

            // Remove last.
            list.Remove(listener6);
            Assert.Equal(5, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener2, list[1]);
            Assert.Same(listener3, list[2]);
            Assert.Same(listener4, list[3]);
            Assert.Same(listener5, list[4]);

            // Remove middle.
            list.Remove(listener2);
            Assert.Equal(4, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener3, list[1]);
            Assert.Same(listener4, list[2]);
            Assert.Same(listener5, list[3]);

            // Remove first.
            list.Remove(listener1);
            Assert.Equal(3, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener3, list[0]);
            Assert.Same(listener4, list[1]);
            Assert.Same(listener5, list[2]);

            // Remove all.
            list.Remove(listener3);
            list.Remove(listener4);
            list.Remove(listener5);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);

            // Remove again.
            list.Remove(listener5);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        [Fact]
        public void Remove_InvokeSixItemListWithNull_Success()
        {
            var list = new SubListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();
            var listener3 = new CustomWeakEventListener();
            var listener4 = new CustomWeakEventListener();
            var listener5 = new CustomWeakEventListener();
            var listener6 = new CustomWeakEventListener();

            list.Add(listener1);
            list.Add(null);
            list.Add(listener2);
            list.Add(listener3);
            list.Add(listener4);
            list.Add(listener5);

            // Remove none.
            list.Remove(listener6);
            Assert.Equal(6, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Throws<InvalidCastException>(() => list[1]);
            Assert.Same(listener2, list[2]);
            Assert.Same(listener3, list[3]);
            Assert.Same(listener4, list[4]);
            Assert.Same(listener5, list[5]);

            // Remove last.
            list.Remove(listener5);
            Assert.Equal(5, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Throws<InvalidCastException>(() => list[1]);
            Assert.Same(listener2, list[2]);
            Assert.Same(listener3, list[3]);
            Assert.Same(listener4, list[4]);

            // Remove middle.
            list.Remove(listener2);
            Assert.Equal(4, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Throws<InvalidCastException>(() => list[1]);
            Assert.Same(listener3, list[2]);
            Assert.Same(listener4, list[3]);

            // Remove first.
            list.Remove(listener1);
            Assert.Equal(3, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);
            Assert.Same(listener3, list[1]);
            Assert.Same(listener4, list[2]);

            // Remove null.
            list.Remove(null);
            Assert.Equal(3, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);
            Assert.Same(listener3, list[1]);
            Assert.Same(listener4, list[2]);

            // Remove all.
            list.Remove(listener3);
            list.Remove(listener4);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);

            // Remove again.
            list.Remove(listener4);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);
        }

        [Fact]
        public void Remove_InvokeSevenItemList_Success()
        {
            var list = new SubListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();
            var listener3 = new CustomWeakEventListener();
            var listener4 = new CustomWeakEventListener();
            var listener5 = new CustomWeakEventListener();
            var listener6 = new CustomWeakEventListener();
            var listener7 = new CustomWeakEventListener();
            var listener8 = new CustomWeakEventListener();

            list.Add(listener1);
            list.Add(listener2);
            list.Add(listener3);
            list.Add(listener4);
            list.Add(listener5);
            list.Add(listener6);
            list.Add(listener7);

            // Remove none.
            list.Remove(listener8);
            Assert.Equal(7, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener2, list[1]);
            Assert.Same(listener3, list[2]);
            Assert.Same(listener4, list[3]);
            Assert.Same(listener5, list[4]);
            Assert.Same(listener6, list[5]);
            Assert.Same(listener7, list[6]);

            // Remove last.
            list.Remove(listener7);
            Assert.Equal(6, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener2, list[1]);
            Assert.Same(listener3, list[2]);
            Assert.Same(listener4, list[3]);
            Assert.Same(listener5, list[4]);
            Assert.Same(listener6, list[5]);

            // Remove middle.
            list.Remove(listener2);
            Assert.Equal(5, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener3, list[1]);
            Assert.Same(listener4, list[2]);
            Assert.Same(listener5, list[3]);
            Assert.Same(listener6, list[4]);

            // Remove first.
            list.Remove(listener1);
            Assert.Equal(4, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener3, list[0]);
            Assert.Same(listener4, list[1]);
            Assert.Same(listener5, list[2]);
            Assert.Same(listener6, list[3]);

            // Remove all.
            list.Remove(listener3);
            list.Remove(listener4);
            list.Remove(listener5);
            list.Remove(listener6);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);

            // Remove again.
            list.Remove(listener6);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        [Fact]
        public void Remove_InvokeSevenItemListWithNull_Success()
        {
            var list = new SubListenerList();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();
            var listener3 = new CustomWeakEventListener();
            var listener4 = new CustomWeakEventListener();
            var listener5 = new CustomWeakEventListener();
            var listener6 = new CustomWeakEventListener();
            var listener7 = new CustomWeakEventListener();

            list.Add(listener1);
            list.Add(null);
            list.Add(listener2);
            list.Add(listener3);
            list.Add(listener4);
            list.Add(listener5);
            list.Add(listener6);

            // Remove none.
            list.Remove(listener7);
            Assert.Equal(7, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Throws<InvalidCastException>(() => list[1]);
            Assert.Same(listener2, list[2]);
            Assert.Same(listener3, list[3]);
            Assert.Same(listener4, list[4]);
            Assert.Same(listener5, list[5]);
            Assert.Same(listener6, list[6]);

            // Remove last.
            list.Remove(listener6);
            Assert.Equal(6, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Throws<InvalidCastException>(() => list[1]);
            Assert.Same(listener2, list[2]);
            Assert.Same(listener3, list[3]);
            Assert.Same(listener4, list[4]);
            Assert.Same(listener5, list[5]);

            // Remove middle.
            list.Remove(listener2);
            Assert.Equal(5, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Throws<InvalidCastException>(() => list[1]);
            Assert.Same(listener3, list[2]);
            Assert.Same(listener4, list[3]);
            Assert.Same(listener5, list[4]);

            // Remove first.
            list.Remove(listener1);
            Assert.Equal(4, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);
            Assert.Same(listener3, list[1]);
            Assert.Same(listener4, list[2]);
            Assert.Same(listener5, list[3]);

            // Remove null.
            list.Remove(null);
            Assert.Equal(4, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);
            Assert.Same(listener3, list[1]);
            Assert.Same(listener4, list[2]);
            Assert.Same(listener5, list[3]);

            // Remove all.
            list.Remove(listener3);
            list.Remove(listener4);
            list.Remove(listener5);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);

            // Remove again.
            list.Remove(listener5);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Throws<InvalidCastException>(() => list[0]);
        }

        // TODO: this causes a crash.
#if false
        [Fact]
        public void Remove_InvokeInUse_Success()
        {
            var list = new SubListenerList();
            var listener = new SubListener();
            list.Add(listener);

            // Begin use.
            Assert.False(list.BeginUse());

            // Add.
            list.Remove(listener);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }
#endif

        [Fact]
        public void Remove_InvokeNoLongerInUse_Success()
        {
            var list = new SubListenerList();
            var listener = new CustomWeakEventListener();
            list.Add(listener);

            // Begin use.
            Assert.False(list.BeginUse());

            // End use.
            list.EndUse();

            // Add.
            list.Remove(listener);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }
        
        [Fact]
        public void Remove_InvokeNoSuchListenerEmpty_Success()
        {
            var list = new SubListenerList();

            // Remove.
            list.Remove(new CustomWeakEventListener());
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
            
            // Remove again.
            list.Remove(new CustomWeakEventListener());
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);

            // Remove null.
            list.Remove(null);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }
        
        [Fact]
        public void Remove_InvokeNoSuchListenerNotEmpty_Success()
        {
            var list = new SubListenerList();
            var listener = new CustomWeakEventListener();
            list.Add(listener);

            // Remove.
            list.Remove(new CustomWeakEventListener());
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener, list[0]);

            // Remove again.
            list.Remove(new CustomWeakEventListener());
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener, list[0]);

            // Remove null.
            list.Remove(null);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener, list[0]);
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(3, 0)]
        [InlineData(3, 1)]
        [InlineData(3, 2)]
        [InlineData(4, 0)]
        [InlineData(4, 1)]
        [InlineData(4, 2)]
        [InlineData(4, 3)]
        [InlineData(6, 0)]
        [InlineData(6, 1)]
        [InlineData(6, 2)]
        [InlineData(6, 3)]
        [InlineData(6, 4)]
        [InlineData(6, 5)]
        [InlineData(7, 0)]
        [InlineData(7, 1)]
        [InlineData(7, 2)]
        [InlineData(7, 3)]
        [InlineData(7, 4)]
        [InlineData(7, 5)]
        public void Remove_InvokeDifferentSizes_Success(int count, int removeIndex)
        {
            // Test internal FrugalList implementation.
            var listeners = new List<CustomWeakEventListener>();
            for (int i = 0; i < count; i++)
            {
                listeners.Add(new CustomWeakEventListener());
            }

            var list = new SubListenerList();
            for (int i = 0; i < listeners.Count; i++)
            {
                list.Add(listeners[i]);
            }

            // Remove.
            list.Remove(listeners[removeIndex]);

            var expectedListeners = new List<CustomWeakEventListener>(listeners);
            expectedListeners.RemoveAt(removeIndex);
            Assert.Equal(expectedListeners.Count, list.Count);
            Assert.Equal(expectedListeners.Count == 0, list.IsEmpty);
            for (int i = 0; i < expectedListeners.Count; i++)
            {
                Assert.Equal(expectedListeners[i], list[i]);
            }

            // Remove all.
            for (int i = 0; i < expectedListeners.Count; i++)
            {
                list.Remove(expectedListeners[i]);
            }

            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        [Fact]
        public void RemoveHandler_Invoke_Success()
        {
            var list = new SubListenerList();
            var target1 = new CustomWeakEventListener();
            Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
            var target2 = new CustomWeakEventListener();
            Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));
            var target3 = new CustomWeakEventListener();
            Delegate handler3 = Delegate.CreateDelegate(typeof(EventHandler), target3, nameof(CustomWeakEventListener.SecondHandler));

            list.AddHandler(handler1);
            list.AddHandler(handler2);
            list.AddHandler(handler3);

            // Remove handler2.
            list.RemoveHandler(handler2);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Equal(target1, list[0]);
            Assert.Equal(target3, list[1]);

            // Remove again.
            list.RemoveHandler(handler2);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.False(list.IsEmpty);
            Assert.Equal(target1, list[0]);
            Assert.Equal(target3, list[1]);

            // Remove handler3.
            list.RemoveHandler(handler3);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Equal(target1, list[0]);

            // Remove handler1.
            list.RemoveHandler(handler1);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        [Fact]
        public void Remove_InvokeSameTargetMultipleTimesSuccess()
        {
            var list = new SubListenerList();

            var target1 = new CustomWeakEventListener();
            var target2 = new CustomWeakEventListener();
            Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
            Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));
            Delegate handler3 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.SecondHandler));
            Delegate handler4 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
            Delegate handler5 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.SecondHandler));
            Delegate handler6 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.SecondHandler));
            Delegate handler7 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));

            list.AddHandler(handler1);
            list.AddHandler(handler2);
            list.AddHandler(handler3);
            list.AddHandler(handler4);
            list.AddHandler(handler5);
            list.AddHandler(handler6);
            list.AddHandler(handler7);

            // Remove handler1.
            list.RemoveHandler(handler1);
            Assert.Equal(6, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Equal(target1, list[0]);
            Assert.Equal(target2, list[1]);
            Assert.Equal(target1, list[2]);
            Assert.Equal(target1, list[3]);
            Assert.Equal(target2, list[4]);
            Assert.Equal(target2, list[5]);

            // Remove handler2.
            list.RemoveHandler(handler2);
            Assert.Equal(5, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Equal(target1, list[0]);
            Assert.Equal(target2, list[1]);
            Assert.Equal(target1, list[2]);
            Assert.Equal(target1, list[3]);
            Assert.Equal(target2, list[4]);

            // Remove handler3.
            list.RemoveHandler(handler3);
            Assert.Equal(4, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Equal(target1, list[0]);
            Assert.Equal(target2, list[1]);
            Assert.Equal(target1, list[2]);
            Assert.Equal(target2, list[3]);

            // Remove handler4.
            list.RemoveHandler(handler4);
            Assert.Equal(3, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Equal(target2, list[0]);
            Assert.Equal(target1, list[1]);
            Assert.Equal(target2, list[2]);
            
            // Remove handler5.
            list.RemoveHandler(handler5);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Equal(target2, list[0]);
            Assert.Equal(target2, list[1]);

            // Remove handler6.
            list.RemoveHandler(handler6);
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Equal(target2, list[0]);

            // Remove handler7.
            list.RemoveHandler(handler7);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        // TODO: this causes a crash.
#if false
        [Fact]
        public void RemoveHandler_InvokeInUse_Success()
        {
            var list = new SubListenerList();
            var target = new CustomWeakEventListener();
            Delegate handler = Delegate.CreateDelegate(typeof(EventHandler), target, nameof(CustomWeakEventListener.Handler));
            list.AddHandler(handler);

            // Begin use.
            Assert.False(list.BeginUse());

            // Add.
            list.RemoveHandler(handler);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }
#endif

        [Fact]
        public void RemoveHandler_InvokeNoLongerInUse_Success()
        {
            var list = new SubListenerList();
            var target = new CustomWeakEventListener();
            Delegate handler = Delegate.CreateDelegate(typeof(EventHandler), target, nameof(CustomWeakEventListener.Handler));
            list.AddHandler(handler);

            // Begin use.
            Assert.False(list.BeginUse());

            // End use.
            list.EndUse();

            // Add.
            list.RemoveHandler(handler);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        [Fact]
        public void RemoveHandler_InvokeNoSuchHandlerEmpty_Success()
        {
            var list = new SubListenerList();
            var target = new CustomWeakEventListener();
            Delegate handler = Delegate.CreateDelegate(typeof(EventHandler), target, nameof(CustomWeakEventListener.Handler));

            // Remove.
            list.RemoveHandler(handler);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);

            // Remove again.
            list.RemoveHandler(handler);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }
        
        [Fact]
        public void RemoveHandler_InvokeNoSuchHandlerNotEmpty_Success()
        {
            var list = new SubListenerList();
            var target = new CustomWeakEventListener();
            Delegate handler = Delegate.CreateDelegate(typeof(EventHandler), target, nameof(CustomWeakEventListener.Handler));
            list.AddHandler(handler);

            // Remove.
            list.RemoveHandler(Delegate.CreateDelegate(typeof(EventHandler), target, nameof(CustomWeakEventListener.SecondHandler)));
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Equal(target, list[0]);

            // Remove again.
            list.RemoveHandler(Delegate.CreateDelegate(typeof(EventHandler), target, nameof(CustomWeakEventListener.SecondHandler)));
            Assert.Equal(1, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Equal(target, list[0]);
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(3, 0)]
        [InlineData(3, 1)]
        [InlineData(3, 2)]
        [InlineData(4, 0)]
        [InlineData(4, 1)]
        [InlineData(4, 2)]
        [InlineData(4, 3)]
        [InlineData(6, 0)]
        [InlineData(6, 1)]
        [InlineData(6, 2)]
        [InlineData(6, 3)]
        [InlineData(6, 4)]
        [InlineData(6, 5)]
        [InlineData(7, 0)]
        [InlineData(7, 1)]
        [InlineData(7, 2)]
        [InlineData(7, 3)]
        [InlineData(7, 4)]
        [InlineData(7, 5)]
        public void RemoveHandler_InvokeDifferentSizes_Success(int count, int removeIndex)
        {
            // Test internal FrugalList implementation.
            var handlers = new List<Delegate>();
            for (int i = 0; i < count; i++)
            {
                handlers.Add(Delegate.CreateDelegate(typeof(EventHandler), new CustomWeakEventListener(), nameof(CustomWeakEventListener.Handler)));
            }

            var list = new SubListenerList();
            for (int i = 0; i < count; i++)
            {
                list.AddHandler(handlers[i]);
            }

            // Remove.
            list.RemoveHandler(handlers[removeIndex]);

            var expectedHandlers = new List<Delegate>(handlers);
            expectedHandlers.RemoveAt(removeIndex);
            Assert.Equal(expectedHandlers.Count, list.Count);
            Assert.Equal(expectedHandlers.Count == 0, list.IsEmpty);
            for (int i = 0; i < expectedHandlers.Count; i++)
            {
                Assert.Same(expectedHandlers[i].Target, list[i]);
            }

            // Remove all.
            for (int i = 0; i < expectedHandlers.Count; i++)
            {
                list.RemoveHandler(expectedHandlers[i]);
            }

            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        [Fact]
        public void RemoveHandler_NullHandler_ThrowsArgumentNullException()
        {
            var list = new SubListenerList();
            // TODO: this should throw ANE.
            //Assert.Throws<ArgumentNullException>("handler", () => list.RemoveHandler(null));
            Assert.Throws<NullReferenceException>(() => list.RemoveHandler(null));
        }

        private class SubListenerList : ListenerList
        {
            public SubListenerList() : base()
            {
            }

            public SubListenerList(int capacity) : base(capacity)
            {
            }

            public new void CopyTo(ListenerList newList) => base.CopyTo(newList);
        }

        protected override void StartListening(object source) => throw new NotImplementedException();

        protected override void StopListening(object source) => throw new NotImplementedException();
    }

    public class ListenerListTEventArgsTests : WeakEventManager
    {
        [Fact]
        public void Ctor_Default()
        {
            var list = new SubListenerList<EventArgs>();
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public void Ctor_Int(int capacity)
        {
            var list = new SubListenerList<EventArgs>(capacity);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
        }

        [Fact]
        public void Clone_InvokeEmptyToEmpty_Success()
        {
            var list = new SubListenerList<EventArgs>();

            ListenerList newList = Assert.IsType<ListenerList<EventArgs>>(list.Clone());
            Assert.NotSame(list, newList);
            Assert.Equal(0, list.Count);
            Assert.True(list.IsEmpty);
            Assert.Equal(0, newList.Count);
            Assert.True(newList.IsEmpty);
        }

        [Fact]
        public void Clone_NotEmptyWithListenersToEmpty_Success()
        {
            var list = new SubListenerList<EventArgs>();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();
            list.Add(listener1);
            list.Add(listener2);

            ListenerList newList = Assert.IsType<ListenerList<EventArgs>>(list.Clone());
            Assert.NotSame(list, newList);
            Assert.Equal(2, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(listener2, list[1]);
            Assert.Equal(2, newList.Count);
            Assert.False(newList.IsEmpty);
            Assert.Same(listener1, newList[0]);
            Assert.Same(listener2, newList[1]);
        }

        [Fact]
        public void Clone_NotEmptyWithHandlersToEmpty_Success()
        {
            var list = new SubListenerList<EventArgs>();
            var target1 = new CustomWeakEventListener();
            Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
            var target2 = new CustomWeakEventListener();
            Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));
            Delegate handler3 = (EventHandler)delegate { };
            Delegate handler4 = WeakEventManagerTests.StaticEventHandler;

            list.AddHandler(handler1);
            list.AddHandler(handler2);
            list.AddHandler(handler3);
            list.AddHandler(handler4);

            ListenerList newList = Assert.IsType<ListenerList<EventArgs>>(list.Clone());
            Assert.NotSame(list, newList);
            Assert.Equal(4, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(target1, list[0]);
            Assert.Same(target2, list[1]);
            Assert.Throws<InvalidCastException>(() => list[2]);
            Assert.Throws<InvalidCastException>(() => list[3]);
            Assert.Equal(4, newList.Count);
            Assert.False(newList.IsEmpty);
            Assert.Same(target1, newList[0]);
            Assert.Same(target2, newList[1]);
            Assert.Throws<InvalidCastException>(() => newList[2]);
            Assert.Throws<InvalidCastException>(() => newList[3]);
        }

        [Fact]
        public void Clone_NotEmptyWithListenersAndHandlersToEmpty_Success()
        {
            var list = new SubListenerList<EventArgs>();
            var listener1 = new CustomWeakEventListener();
            var listener2 = new CustomWeakEventListener();
            var target1 = new CustomWeakEventListener();
            Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler), target1, nameof(CustomWeakEventListener.Handler));
            var target2 = new CustomWeakEventListener();
            Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler), target2, nameof(CustomWeakEventListener.Handler));
            Delegate handler3 = (EventHandler)delegate { };
            Delegate handler4 = WeakEventManagerTests.StaticEventHandler;

            list.Add(listener1);
            list.AddHandler(handler1);
            list.Add(listener2);
            list.AddHandler(handler2);
            list.AddHandler(handler3);
            list.AddHandler(handler4);

            ListenerList newList = Assert.IsType<ListenerList<EventArgs>>(list.Clone());
            Assert.NotSame(list, newList);
            Assert.Equal(6, list.Count);
            Assert.False(list.IsEmpty);
            Assert.Same(listener1, list[0]);
            Assert.Same(target1, list[1]);
            Assert.Same(listener2, list[2]);
            Assert.Same(target2, list[3]);
            Assert.Throws<InvalidCastException>(() => list[4]);
            Assert.Throws<InvalidCastException>(() => list[5]);
            Assert.Equal(6, newList.Count);
            Assert.False(newList.IsEmpty);
            Assert.Same(listener1, newList[0]);
            Assert.Same(target1, newList[1]);
            Assert.Same(listener2, newList[2]);
            Assert.Same(target2, newList[3]);
            Assert.Throws<InvalidCastException>(() => newList[4]);
            Assert.Throws<InvalidCastException>(() => newList[5]);
        }

        public static IEnumerable<object?[]> DeliverEvent_TestData()
        {
            yield return new object?[] { null, null, null };
            yield return new object?[] { new object(), new EventArgs(), typeof(int) };
            yield return new object?[] { new object(), EventArgs.Empty, typeof(int) };
        }

        [Theory]
        [MemberData(nameof(DeliverEvent_TestData))]
        public void DeliverEvent_InvokeWithListeners_Success(object sender, EventArgs args, Type managerType)
        {
            var list = new SubListenerList<EventArgs>();
            var events = new List<string>();
            var listener1 = new CustomWeakEventListener
            {
                ReceiveWeakEventAction = (t, s, e) =>
            {
                Assert.Same(managerType, t);
                Assert.Same(sender, s);
                Assert.Same(args, e);
                events.Add("listener1");
                return true;
            }
            };
            var listener2 = new CustomWeakEventListener
            {
                ReceiveWeakEventAction = (t, s, e) =>
            {
                Assert.Same(managerType, t);
                Assert.Same(sender, s);
                Assert.Same(args, e);
                events.Add("listener2");
                return true;
            }
            };
            list.Add(listener1);
            list.Add(listener2);

            Assert.False(list.DeliverEvent(sender, args, managerType));
            Assert.Equal(new string[] { "listener1", "listener2" }, events);
        }

        [Theory]
        [MemberData(nameof(DeliverEvent_TestData))]
        public void DeliverEvent_InvokeWithHandlers_Success(object sender, EventArgs args, Type managerType)
        {
            var list = new SubListenerList<EventArgs>();
            var events = new List<string>();
            var listener1 = new CustomWeakEventListener
            {
                HandlerAction = (s, e) =>
            {
                Assert.Same(sender, s);
                Assert.Same(args, e);
                events.Add("handler1");
            }
            };
            var listener2 = new CustomWeakEventListener
            {
                HandlerAction = (s, e) =>
            {
                Assert.Same(sender, s);
                Assert.Same(args, e);
                events.Add("handler2");
            }
            };
            var listener3 = new CustomWeakEventListener
            {
                HandlerAction = (s, e) =>
            {
                Assert.Same(sender, s);
                Assert.Same(args, e);
                events.Add("handler3");
            }
            };
            Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), listener1, nameof(CustomWeakEventListener.Handler));
            Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), listener2, nameof(CustomWeakEventListener.Handler));
            Delegate handler3 = Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), listener3, nameof(CustomWeakEventListener.Handler));
            list.AddHandler(handler1);
            list.AddHandler(handler2);
            list.AddHandler(handler3);

            Assert.False(list.DeliverEvent(sender, args, managerType));
            Assert.Equal(new string[] { "handler1", "handler2", "handler3" }, events);
        }

        [Theory]
        [MemberData(nameof(DeliverEvent_TestData))]
        public void DeliverEvent_InvokeWithListenersAndHandlers_Success(object sender, EventArgs args, Type managerType)
        {
            var list = new SubListenerList<EventArgs>();
            var events = new List<string>();
            var listener1 = new CustomWeakEventListener
            {
                HandlerAction = (s, e) =>
            {
                Assert.Same(sender, s);
                Assert.Same(args, e);
                events.Add("handler1");
            }
            };
            var listener2 = new CustomWeakEventListener
            {
                ReceiveWeakEventAction = (t, s, e) =>
            {
                Assert.Same(managerType, t);
                Assert.Same(sender, s);
                Assert.Same(args, e);
                events.Add("listener1");
                return true;
            }
            };
            var listener3 = new CustomWeakEventListener
            {
                HandlerAction = (s, e) =>
            {
                Assert.Same(sender, s);
                Assert.Same(args, e);
                events.Add("handler2");
            }
            };
            var listener4 = new CustomWeakEventListener
            {
                ReceiveWeakEventAction = (t, s, e) =>
            {
                Assert.Same(managerType, t);
                Assert.Same(sender, s);
                Assert.Same(args, e);
                events.Add("listener2");
                return true;
            }
            };
            var listener5 = new CustomWeakEventListener
            {
                HandlerAction = (s, e) =>
            {
                Assert.Same(sender, s);
                Assert.Same(args, e);
                events.Add("handler3");
            }
            };
            Delegate handler1 = Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), listener1, nameof(CustomWeakEventListener.Handler));
            Delegate handler2 = Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), listener3, nameof(CustomWeakEventListener.Handler));
            Delegate handler3 = Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), listener5, nameof(CustomWeakEventListener.Handler));
            list.AddHandler(handler1);
            list.Add(listener2);
            list.AddHandler(handler2);
            list.Add(listener4);
            list.AddHandler(handler3);

            Assert.False(list.DeliverEvent(sender, args, managerType));
            Assert.Equal(new string[] { "handler1", "listener1", "handler2", "listener2", "handler3" }, events);
        }

        // TODO: this causes a crash.
#if false
        [Theory]
        [MemberData(nameof(DeliverEvent_TestData))]
        public void DeliverEvent_InvokeWithListenersNotHandled_Success(object sender, EventArgs args, Type managerType)
        {
            var list = new SubListenerList<EventArgs>();
            var events = new List<string>();
            var listener1 = new CustomWeakEventListener();
            listener1.ReceiveWeakEventAction = (t, s, e) =>
            {
                Assert.Same(managerType, t);
                Assert.Same(sender, s);
                Assert.Same(args, e);
                events.Add("listener1");
                return false;
            };
            var listener2 = new CustomWeakEventListener();
            listener2.ReceiveWeakEventAction = (t, s, e) =>
            {
                Assert.Same(managerType, t);
                Assert.Same(sender, s);
                Assert.Same(args, e);
                events.Add("listener2");
                return true;
            };
            list.Add(listener1);
            list.Add(listener2);

            Assert.False(list.DeliverEvent(sender, args, managerType));
            Assert.Equal(new string[] { "listener1", "listener2" }, events);
        }
#endif

        [Theory]
        [MemberData(nameof(DeliverEvent_TestData))]
        public void DeliverEvent_InvokeWithInvalidHandlerAction_ThrowsInvalidCastException(object sender, EventArgs args, Type managerType)
        {
            var list = new SubListenerList<EventArgs>();
            Delegate handler = () => {};
            list.AddHandler(handler);

            Assert.Throws<InvalidCastException>(() => list.DeliverEvent(sender, args, managerType));
        }

        [Theory]
        [MemberData(nameof(DeliverEvent_TestData))]
        public void DeliverEvent_InvokeWithInvalidHandlerFunc_ThrowsInvalidCastException(object sender, EventArgs args, Type managerType)
        {
            var list = new SubListenerList<EventArgs>();
            static bool EventHandler(object sender, EventArgs args) => true;
            list.AddHandler(EventHandler);

            Assert.Throws<InvalidCastException>(() => list.DeliverEvent(sender, args, managerType));
        }

        [Theory]
        [MemberData(nameof(DeliverEvent_TestData))]
        public void DeliverEvent_InvokeWithInvalidHandlerEventHandler_ThrowsInvalidCastException(object sender, EventArgs args, Type managerType)
        {
            var list = new SubListenerList<EventArgs>();
            var target = new CustomWeakEventListener();
            Delegate handler = Delegate.CreateDelegate(typeof(EventHandler), target, nameof(CustomWeakEventListener.Handler));
            list.AddHandler(handler);

            Assert.Throws<InvalidCastException>(() => list.DeliverEvent(sender, args, managerType));
        }

        [Theory]
        [MemberData(nameof(DeliverEvent_TestData))]
        public void DeliverEvent_InvokeEmpty_ReturnsFalse(object sender, EventArgs args, Type managerType)
        {
            var list = new SubListenerList<EventArgs>();
            Assert.False(list.DeliverEvent(sender, args, managerType));
        }

        private class SubListenerList<TEventArgs> : ListenerList<TEventArgs> where TEventArgs : EventArgs
        {
            public SubListenerList() : base()
            {
            }
            
            public SubListenerList(int capacity) : base(capacity)
            {
            }
        }

        protected override void StartListening(object source) => throw new NotImplementedException();

        protected override void StopListening(object source) => throw new NotImplementedException();
    }

    private static void StaticEventHandler(object sender, EventArgs e)
    {
    }

    private class CustomListenerList : ListenerList
    {
        public Func<ListenerList>? CloneAction { get; set; }

        public override ListenerList Clone() => CloneAction!.Invoke();

        public Func<object, EventArgs, Type, bool>? DeliverEventAction { get; set; }

        public override bool DeliverEvent(object sender, EventArgs e, Type managerType)
        {
            if (DeliverEventAction is null)
            {
                return base.DeliverEvent(sender, e, managerType);
            }

            return DeliverEventAction(sender, e, managerType);
        }
    }

    private class CustomWeakEventListener : IWeakEventListener
    {
        public Func<Type, object, EventArgs, bool>? ReceiveWeakEventAction { get; set; }

        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (ReceiveWeakEventAction is null)
            {
                throw new NotImplementedException();
            }

            return ReceiveWeakEventAction(managerType, sender, e);
        }

        public Action<object, EventArgs>? HandlerAction { get; set; }

        public void Handler(object sender, EventArgs e)
        {
            if (HandlerAction is null)
            {
                throw new NotImplementedException();
            }

            HandlerAction(sender, e);
        }
        
        public void SecondHandler(object sender, EventArgs e) => throw new NotImplementedException();
    }

    private class SubWeakEventManager : WeakEventManager
    {
        public SubWeakEventManager()
        {
        }

        public new IDisposable ReadLock => base.ReadLock;

        public new IDisposable WriteLock => base.WriteLock;

        public new object this[object source]
        {
            get => base[source];
            set => base[source] = value;
        }

        public new void DeliverEvent(object sender, EventArgs args) => base.DeliverEvent(sender, args);

        public new void DeliverEventToList(object sender, EventArgs args, ListenerList list) => base.DeliverEventToList(sender, args, list);

        public static new WeakEventManager GetCurrentManager(Type managerType) => WeakEventManager.GetCurrentManager(managerType);

        public new ListenerList NewListenerList() => base.NewListenerList();

        public new void ProtectedAddHandler(object source, Delegate handler) => base.ProtectedAddHandler(source, handler);

        public new void ProtectedAddListener(object source, IWeakEventListener listener) => base.ProtectedAddListener(source, listener);

        public new void ProtectedRemoveHandler(object source, Delegate handler) => base.ProtectedRemoveHandler(source, handler);

        public new void ProtectedRemoveListener(object source, IWeakEventListener listener) => base.ProtectedRemoveListener(source, listener);

        public new bool Purge(object source, object data, bool purgeAll) => base.Purge(source, data, purgeAll);

        public new void Remove(object source) => base.Remove(source);

        public static new void SetCurrentManager(Type managerType, WeakEventManager? newManager) => WeakEventManager.SetCurrentManager(managerType, newManager);

        public new void ScheduleCleanup() => base.ScheduleCleanup();

        public Action<object>? StartListeningAction { get; set; }

        protected override void StartListening(object source)
        {
            if (StartListeningAction is null)
            {
                throw new NotImplementedException();
            }

            StartListeningAction(source);
        }

        public Action<object>? StopListeningAction { get; set; }

        protected override void StopListening(object source)
        {
            if (StopListeningAction is not null)
            {
                StopListeningAction(source);
            }
        }
    }

    protected override void StartListening(object source) => throw new NotImplementedException();

    protected override void StopListening(object source) { }
}
