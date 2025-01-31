// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace System.Windows.Tests;

public class WeakEventManagerTTests
{
    public static IEnumerable<object?[]> AddHandler_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { new EventArgs() };
        yield return new object?[] { EventArgs.Empty };
    }

    [Theory]
    [MemberData(nameof(AddHandler_TestData))]
    public void AddHandler_Invoke_Success(EventArgs e)
    {
        var source1 = new ClassWithEvents();
        var source2 = new ClassWithEvents();
        int callCount1 = 0;
        int callCount2 = 0;
        EventHandler<EventArgs> handler1 = (sender, actualE) =>
        {
            Assert.Same(source1, sender);
            Assert.Same(e, actualE);
            callCount1++;
        };
        EventHandler<EventArgs> handler2 = (sender, actualE) =>
        {
            Assert.Same(source1, sender);
            Assert.Same(e, actualE);
            callCount2++;
        };

        // Add.
        WeakEventManager<ClassWithEvents, EventArgs>.AddHandler(source1, nameof(ClassWithEvents.CustomEvent), handler1);

        // Call.
        source1.OnCustomEvent(source1, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Call invalid source1.
        source1.OnCustomEvent(source2, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        
        source1.OnCustomEvent(new object(), e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        
        source1.OnCustomEvent(null!, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Add again.
        WeakEventManager<ClassWithEvents, EventArgs>.AddHandler(source1, nameof(ClassWithEvents.CustomEvent), handler1);
        source1.OnCustomEvent(source1, e);
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);

        // Add another.
        WeakEventManager<ClassWithEvents, EventArgs>.AddHandler(source1, nameof(ClassWithEvents.CustomEvent), handler2);
        source1.OnCustomEvent(source1, e);
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);

        // Remove second.
        WeakEventManager<ClassWithEvents, EventArgs>.RemoveHandler(source1, nameof(ClassWithEvents.CustomEvent), handler2);
        source1.OnCustomEvent(source1, e);
        Assert.Equal(7, callCount1);
        Assert.Equal(1, callCount2);

        // Remove first.
        WeakEventManager<ClassWithEvents, EventArgs>.RemoveHandler(source1, nameof(ClassWithEvents.CustomEvent), handler1);
        source1.OnCustomEvent(source1, e);
        Assert.Equal(8, callCount1);
        Assert.Equal(1, callCount2);

        // Remove again.
        WeakEventManager<ClassWithEvents, EventArgs>.RemoveHandler(source1, nameof(ClassWithEvents.CustomEvent), handler1);
        source1.OnCustomEvent(source1, e);
        Assert.Equal(8, callCount1);
        Assert.Equal(1, callCount2);
    }

    [Theory]
    [MemberData(nameof(AddHandler_TestData))]
    public void AddHandler_InvokeNoSource_Success(EventArgs e)
    {
        var source = new ClassWithEvents();
        int callCount1 = 0;
        int callCount2 = 0;
        EventHandler<EventArgs> handler1 = (sender, actualE) =>
        {
            Assert.Null(sender);
            //Assert.Same(e, actualE);
            callCount1++;
        };
        EventHandler<EventArgs> handler2 = (sender, actualE) =>
        {
            Assert.Null(sender);
            //Assert.Same(e, actualE);
            callCount2++;
        };

        // Add.
        WeakEventManager<ClassWithEvents, EventArgs>.AddHandler(null!, nameof(ClassWithEvents.StaticEvent1), handler1);

        // Call.
        ClassWithEvents.OnStaticEvent1(null, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Call invalid source.
        ClassWithEvents.OnStaticEvent1(source, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        
        ClassWithEvents.OnStaticEvent1(new object(), e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Add again.
        WeakEventManager<ClassWithEvents, EventArgs>.AddHandler(null!, nameof(ClassWithEvents.StaticEvent1), handler1);
        ClassWithEvents.OnStaticEvent1(null, e);
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);

        // Add another.
        WeakEventManager<ClassWithEvents, EventArgs>.AddHandler(null!, nameof(ClassWithEvents.StaticEvent1), handler2);
        ClassWithEvents.OnStaticEvent1(null, e);
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);

        // Remove second.
        WeakEventManager<ClassWithEvents, EventArgs>.RemoveHandler(null!, nameof(ClassWithEvents.StaticEvent1), handler2);
        ClassWithEvents.OnStaticEvent1(null, e);
        Assert.Equal(7, callCount1);
        Assert.Equal(1, callCount2);

        // Remove first.
        WeakEventManager<ClassWithEvents, EventArgs>.RemoveHandler(null!, nameof(ClassWithEvents.StaticEvent1), handler1);
        ClassWithEvents.OnStaticEvent1(null, e);
        Assert.Equal(8, callCount1);
        Assert.Equal(1, callCount2);

        // Remove again.
        WeakEventManager<ClassWithEvents, EventArgs>.RemoveHandler(null!, nameof(ClassWithEvents.StaticEvent1), handler1);
        ClassWithEvents.OnStaticEvent1(null, e);
        Assert.Equal(8, callCount1);
        Assert.Equal(1, callCount2);
    }

    [Fact]
    public void AddHandler_InvokeMultipleTimes_Success()
    {
        var source = new ClassWithEvents();
        EventHandler<EventArgs> handler1 = (sender, e) => { };
        EventHandler<EventArgs> handler2 = (sender, actualE) => { };

        // Add.
        WeakEventManager<ClassWithEvents, EventArgs>.AddHandler(source, nameof(ClassWithEvents.CustomEvent), handler1);

        // Add again.
        WeakEventManager<ClassWithEvents, EventArgs>.AddHandler(source, nameof(ClassWithEvents.CustomEvent), handler1);

        // Add another.
        WeakEventManager<ClassWithEvents, EventArgs>.AddHandler(source, nameof(ClassWithEvents.CustomEvent), handler2);
    }

    // [Fact]
    // public void AddHandler_NullSourceNonStaticEvent_ThrowsTargetException()
    // {
    //     // Add.
    //     Assert.Throws<TargetException>(() => WeakEventManager<ClassWithEvents, EventArgs>.AddHandler(null!, nameof(ClassWithEvents.CustomEvent), (sender, e) => { }));

    //     // Add again.
    //     WeakEventManager<ClassWithEvents, EventArgs>.AddHandler(null!, nameof(ClassWithEvents.CustomEvent), (sender, e) => { });
    // }

    [Fact]
    public void AddHandler_NullEventName_ThrowsArgumentNullException()
    {
        // TODO: should throw ArgumentNullException
        //Assert.Throws<ArgumentNullException>("eventName", () => WeakEventManager<object, EventArgs>.AddHandler(new object(), null, (sender, e) => { }));
        Assert.Throws<NullReferenceException>(() => WeakEventManager<object, EventArgs>.AddHandler(new object(), null, (sender, e) => { }));

        // Call again to test caching.
        Assert.Throws<NullReferenceException>(() => WeakEventManager<object, EventArgs>.AddHandler(new object(), null, (sender, e) => { }));
    }

    public static IEnumerable<object?[]> AddHandler_NoSuchEvent_TestData()
    {
        yield return new object?[] { null, "" };
        yield return new object?[] { null, " " };
        yield return new object?[] { null, "eventName" };
        yield return new object?[] { new object(), "" };
        yield return new object?[] { new object(), " " };
        yield return new object?[] { new object(), "eventName" };
        yield return new object?[] { new ClassWithEvents(), nameof(ClassWithEvents.CustomEvent) };
        yield return new object?[] { new ClassWithEvents(), nameof(ClassWithEvents.StaticEvent1) };
    }
    
    [Theory]
    [MemberData(nameof(AddHandler_NoSuchEvent_TestData))]
    public void AddHandler_NoSuchEvent_ThrowsArgumentException(object source, string eventName)
    {
        var handler = new EventHandler<EventArgs>((sender, e) => { });
        // TODO: this should have a paramName.
        Assert.Throws<ArgumentException>(() => WeakEventManager<object, EventArgs>.AddHandler(source, eventName, handler));

        // Call again to test caching.
        Assert.Throws<ArgumentException>(() => WeakEventManager<object, EventArgs>.AddHandler(source, eventName, handler));
    }

    [Fact]
    public void AddHandler_NullHandler_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("handler", () => WeakEventManager<object, EventArgs>.AddHandler(new object(), "EventName", null));
    }

    [Fact]
    public void RemoveHandler_Invoke_Success()
    {
        var source = new ClassWithEvents();
        int callCount = 0;
        EventHandler<EventArgs> handler = (sender, e) => callCount++;

        WeakEventManager<ClassWithEvents, EventArgs>.AddHandler(source, nameof(ClassWithEvents.CustomEvent), handler);

        // Remove.
        WeakEventManager<ClassWithEvents, EventArgs>.RemoveHandler(source, nameof(ClassWithEvents.CustomEvent), handler);
        Assert.Equal(0, callCount);

        // Call event.
        source.OnCustomEvent(source, EventArgs.Empty);
        Assert.Equal(0, callCount);

        // Remove again.
        WeakEventManager<ClassWithEvents, EventArgs>.RemoveHandler(source, nameof(ClassWithEvents.CustomEvent), handler);
        Assert.Equal(0, callCount);

        // Call event.
        source.OnCustomEvent(source, EventArgs.Empty);
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void RemoveHandler_InvokeNoSource_Success()
    {
        int callCount = 0;
        EventHandler<EventArgs> handler = (sender, e) => callCount++;

        WeakEventManager<ClassWithEvents, EventArgs>.AddHandler(null!, nameof(ClassWithEvents.StaticEvent2), handler);

        // Remove.
        WeakEventManager<ClassWithEvents, EventArgs>.RemoveHandler(null!, nameof(ClassWithEvents.StaticEvent2), handler);
        Assert.Equal(0, callCount);

        // Call event.
        ClassWithEvents.OnStaticEvent2(null!, EventArgs.Empty);
        Assert.Equal(0, callCount);

        // Remove again.
        WeakEventManager<ClassWithEvents, EventArgs>.RemoveHandler(null!, nameof(ClassWithEvents.StaticEvent2), handler);
        Assert.Equal(0, callCount);

        // Call event.
        ClassWithEvents.OnStaticEvent2(null!, EventArgs.Empty);
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void RemoveHandler_NoSuchSource_Success()
    {
        var source1 = new ClassWithEvents();
        var source2 = new ClassWithEvents();
        int callCount = 0;
        EventHandler<EventArgs> handler = (sender, e) => callCount++;

        WeakEventManager<ClassWithEvents, EventArgs>.AddHandler(source1, nameof(ClassWithEvents.CustomEvent), handler);

        // Remove.
        WeakEventManager<ClassWithEvents, EventArgs>.RemoveHandler(source2, nameof(ClassWithEvents.CustomEvent), handler);
        Assert.Equal(0, callCount);

        // Call event.
        source1.OnCustomEvent(source1, EventArgs.Empty);
        Assert.Equal(1, callCount);

        // Remove again.
        WeakEventManager<ClassWithEvents, EventArgs>.RemoveHandler(source2, nameof(ClassWithEvents.CustomEvent), handler);
        Assert.Equal(1, callCount);

        // Call event.
        source1.OnCustomEvent(source1, EventArgs.Empty);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void RemoveHandler_NoSuchHandler_Success()
    {
        var source = new ClassWithEvents();
        int callCount1 = 0;
        EventHandler<EventArgs> handler1 = (sender, e) => callCount1++;
        int callCount2 = 0;
        EventHandler<EventArgs> handler2 = (sender, e) => callCount2++;

        WeakEventManager<ClassWithEvents, EventArgs>.AddHandler(source, nameof(ClassWithEvents.CustomEvent), handler1);

        // Remove.
        WeakEventManager<ClassWithEvents, EventArgs>.RemoveHandler(source, nameof(ClassWithEvents.CustomEvent), handler2);
        Assert.Equal(0, callCount1);
        Assert.Equal(0, callCount2);

        // Call event.
        source.OnCustomEvent(source, EventArgs.Empty);        
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Remove again.
        WeakEventManager<ClassWithEvents, EventArgs>.RemoveHandler(source, nameof(ClassWithEvents.CustomEvent), handler2);

        // Call event.
        source.OnCustomEvent(source, EventArgs.Empty);        
        Assert.Equal(2, callCount1);
        Assert.Equal(0, callCount2);
    }

    [Fact]
    public void RemoveHandler_NullSourceNonStaticEvent_()
    {
        EventHandler<EventArgs> handler = (sender, e) => { };
        WeakEventManager<ClassWithEvents, EventArgs>.RemoveHandler(null!, nameof(ClassWithEvents.CustomEvent), handler);
    }

    [Fact]
    public void RemoveHandler_NullHandler_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("handler", () => WeakEventManager<object, EventArgs>.RemoveHandler(new object(), "EventName", null));
    }

    [Fact]
    public void RemoveHandler_NullEventName_ThrowsArgumentNullException()
    {
        // TODO: this should throw ArgumentNullException
        //Assert.Throws<ArgumentNullException>("eventName", () => WeakEventManager<object, EventArgs>.RemoveHandler(new object(), null, (sender, e) => { }));
        Assert.Throws<NullReferenceException>(() => WeakEventManager<object, EventArgs>.RemoveHandler(new object(), null, (sender, e) => { }));
    }

    public static IEnumerable<object?[]> RemoveHandler_NoSuchEvent_TestData()
    {
        yield return new object?[] { null, "" };
        yield return new object?[] { null, " " };
        yield return new object?[] { null, "eventName" };
        yield return new object?[] { new object(), "" };
        yield return new object?[] { new object(), " " };
        yield return new object?[] { new object(), "eventName" };
        yield return new object?[] { new ClassWithEvents(), nameof(ClassWithEvents.CustomEvent) };
        yield return new object?[] { new ClassWithEvents(), nameof(ClassWithEvents.StaticEvent1) };
    }

    [Theory]
    [MemberData(nameof(RemoveHandler_NoSuchEvent_TestData))]
    public void RemoveHandler_NoSuchEvent_ThrowsArgumentException(object source, string eventName)
    {
        var handler = new EventHandler<EventArgs>((sender, e) => { });
        // TODO: this should have a paramName.
        Assert.Throws<ArgumentException>(() => WeakEventManager<object, EventArgs>.RemoveHandler(source, eventName, handler));

        // Call again to test caching.
        Assert.Throws<ArgumentException>(() => WeakEventManager<object, EventArgs>.RemoveHandler(source, eventName, handler));
    }

    private class ClassWithEvents
    {
        private EventHandler? _event;

        public event EventHandler CustomEvent
        {
            add => _event += value;
            remove => _event -= value;
        }

        public void OnCustomEvent(object sender, EventArgs e) => _event?.Invoke(sender, e);

        private static EventHandler? s_staticEvent1;

        public static event EventHandler StaticEvent1
        {
            add => s_staticEvent1 += value;
            remove => s_staticEvent1 -= value;
        }

        public static void OnStaticEvent1(object? sender, EventArgs e) => s_staticEvent1?.Invoke(sender, e);

        private static EventHandler? s_staticEvent2;

        public static event EventHandler StaticEvent2
        {
            add => s_staticEvent2 += value;
            remove => s_staticEvent2 -= value;
        }

        public static void OnStaticEvent2(object? sender, EventArgs e) => s_staticEvent2?.Invoke(sender, e);
    }
}
