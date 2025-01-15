// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;

namespace System.ComponentModel.Tests;

public class CurrentChangedEventManagerTests
{
    public static IEnumerable<object?[]> AddHandler_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { new EventArgs() };
        yield return new object?[] { EventArgs.Empty };
    }

    [Theory]
    [MemberData(nameof(AddHandler_TestData))]
    public void AddHandler_InvokeWithHandler_CallsCurrentChanged(EventArgs e)
    {
        var source = new CustomCollectionView();
        int callCount1 = 0;
        var listener1 = new CustomWeakEventListener();
        listener1.HandlerAction += (actualSender, actualE) =>
        {
            Assert.Same(source, actualSender);
            Assert.Same(e, actualE);
            callCount1++;
        };
        EventHandler<EventArgs> handler1 = (EventHandler<EventArgs>)Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), listener1, nameof(CustomWeakEventListener.Handler));
        int callCount2 = 0;
        var listener2 = new CustomWeakEventListener();
        listener2.HandlerAction += (actualSender, actualE) =>
        {
            Assert.Same(source, actualSender);
            Assert.Same(e, actualE);
            callCount2++;
        };
        EventHandler<EventArgs> handler2 = (EventHandler<EventArgs>)Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), listener2, nameof(CustomWeakEventListener.Handler));

        // Add.
        CurrentChangedEventManager.AddHandler(source, handler1);

        // Call.
        source.OnCurrentChanged(source, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Call invalid source.
        source.OnCurrentChanged(listener1, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        
        source.OnCurrentChanged(new object(), e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        
        source.OnCurrentChanged(null!, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Add again.
        CurrentChangedEventManager.AddHandler(source, handler1);
        source.OnCurrentChanged(source, e);
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);

        // Add another.
        CurrentChangedEventManager.AddHandler(source, handler2);
        source.OnCurrentChanged(source, e);
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);

        // Remove second.
        CurrentChangedEventManager.RemoveHandler(source, handler2);
        source.OnCurrentChanged(source, e);
        Assert.Equal(7, callCount1);
        Assert.Equal(1, callCount2);

        // Remove first.
        CurrentChangedEventManager.RemoveHandler(source, handler1);
        source.OnCurrentChanged(source, e);
        Assert.Equal(8, callCount1);
        Assert.Equal(1, callCount2);

        // Remove again.
        CurrentChangedEventManager.RemoveHandler(source, handler1);
        source.OnCurrentChanged(source, e);
        Assert.Equal(8, callCount1);
        Assert.Equal(1, callCount2);
    }

    [Fact]
    public void AddHandler_Invoke_Success()
    {
        var source1 = new CustomCollectionView();
        var source2 = new CustomCollectionView();
        var target1 = new CustomWeakEventListener();
        int callCount1 = 0;
        target1.HandlerAction += (sender, e) => callCount1++;
        EventHandler<EventArgs> handler1 = (EventHandler<EventArgs>)Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), target1, nameof(CustomWeakEventListener.Handler));
        var target2 = new CustomWeakEventListener();
        int callCount2 = 0;
        target2.HandlerAction += (sender, e) => callCount2++;
        EventHandler<EventArgs> handler2 = (EventHandler<EventArgs>)Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), target2, nameof(CustomWeakEventListener.Handler));
        int callCount3 = 0;
        var target3 = new CustomWeakEventListener();
        target3.HandlerAction += (sender, e) => callCount3++;
        EventHandler<EventArgs> handler3 = (EventHandler<EventArgs>)Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), target3, nameof(CustomWeakEventListener.Handler));

        // Add.
        CurrentChangedEventManager.AddHandler(source1, handler1);
        source1.OnCurrentChanged(source1, EventArgs.Empty);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCurrentChanged(source2, EventArgs.Empty);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);

        // Add again.
        CurrentChangedEventManager.AddHandler(source1, handler1);
        source1.OnCurrentChanged(source1, EventArgs.Empty);
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCurrentChanged(source2, EventArgs.Empty);
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);

        // Add another handler.
        CurrentChangedEventManager.AddHandler(source1, handler2);
        source1.OnCurrentChanged(source1, EventArgs.Empty);
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCurrentChanged(source2, EventArgs.Empty);
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);
        Assert.Equal(0, callCount3);

        // Add another source.
        CurrentChangedEventManager.AddHandler(source2, handler3);
        source1.OnCurrentChanged(source1, EventArgs.Empty);
        Assert.Equal(7, callCount1);
        Assert.Equal(2, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCurrentChanged(source2, EventArgs.Empty);
        Assert.Equal(7, callCount1);
        Assert.Equal(2, callCount2);
        Assert.Equal(1, callCount3);
    }

    // [Fact]
    // public void AddHandler_InvokeNoSource_Success()
    // {
    //     var target1 = new CustomWeakEventListener();
    //     EventHandler<EventArgs> handler1 = (EventHandler<EventArgs>)Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), target1, nameof(CustomWeakEventListener.Handler));
    //     var target2 = new CustomWeakEventListener();
    //     EventHandler<EventArgs> handler2 = (EventHandler<EventArgs>)Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), target2, nameof(CustomWeakEventListener.Handler));

    //     // Add.
    //     // There is a race condition where an NRE is thrown if the source is null and
    //     // no previous listeners or handlers were ever added.
    //     try
    //     {
    //         CurrentChangedEventManager.AddHandler(null, handler1);
    //     }
    //     catch (NullReferenceException)
    //     {
    //     }

    //     // Add again.
    //     CurrentChangedEventManager.AddHandler(null, handler1);

    //     // Add another.
    //     CurrentChangedEventManager.AddHandler(null, handler2);
    // }

    [Fact]
    public void AddHandler_NullHandler_ThrowsArgumentNullException()
    {
        var source = new CustomCollectionView();
        Assert.Throws<ArgumentNullException>("handler", () => CurrentChangedEventManager.AddHandler(null, null));
        Assert.Throws<ArgumentNullException>("handler", () => CurrentChangedEventManager.AddHandler(source, null));
    }

    public static IEnumerable<object?[]> AddListener_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset) };
    }

    [Theory]
    [MemberData(nameof(AddListener_TestData))]
    public void AddListener_InvokeWithHandler_CallsCurrentChanged(NotifyCollectionChangedEventArgs e)
    {
        var source = new CustomCollectionView();
        int callCount1 = 0;
        var listener1 = new CustomWeakEventListener();
        listener1.ReceiveWeakEventAction += (actualManagerType, actualSender, actualE) =>
        {
            Assert.Equal(typeof(CurrentChangedEventManager), actualManagerType);
            Assert.Same(source, actualSender);
            Assert.Same(e, actualE);
            callCount1++;
            return true;
        };
        int callCount2 = 0;
        var listener2 = new CustomWeakEventListener();
        listener2.ReceiveWeakEventAction += (actualManagerType, actualSender, actualE) =>
        {
            Assert.Equal(typeof(CurrentChangedEventManager), actualManagerType);
            Assert.Same(source, actualSender);
            Assert.Same(e, actualE);
            callCount2++;
            return true;
        };

        // Add.
        CurrentChangedEventManager.AddListener(source, listener1);

        // Call.
        source.OnCurrentChanged(source, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Call invalid source.
        source.OnCurrentChanged(listener1, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        
        source.OnCurrentChanged(new object(), e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        
        source.OnCurrentChanged(null!, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Add again.
        CurrentChangedEventManager.AddListener(source, listener1);
        source.OnCurrentChanged(source, e);
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);

        // Add another.
        CurrentChangedEventManager.AddListener(source, listener2);
        source.OnCurrentChanged(source, e);
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);

        // Remove second.
        CurrentChangedEventManager.RemoveListener(source, listener2);
        source.OnCurrentChanged(source, e);
        Assert.Equal(7, callCount1);
        Assert.Equal(1, callCount2);

        // Remove first.
        CurrentChangedEventManager.RemoveListener(source, listener1);
        source.OnCurrentChanged(source, e);
        Assert.Equal(8, callCount1);
        Assert.Equal(1, callCount2);

        // Remove again.
        CurrentChangedEventManager.RemoveListener(source, listener1);
        source.OnCurrentChanged(source, e);
        Assert.Equal(8, callCount1);
        Assert.Equal(1, callCount2);
    }

    [Fact]
    public void AddListener_InvokeMultipleTimes_Success()
    {
        var source1 = new CustomCollectionView();
        var source2 = new CustomCollectionView();
        var listener1 = new CustomWeakEventListener();
        int callCount1 = 0;
        listener1.ReceiveWeakEventAction += (managerType, sender, e) =>
        {
            callCount1++;
            return true;
        };
        var listener2 = new CustomWeakEventListener();
        int callCount2 = 0;
        listener2.ReceiveWeakEventAction += (managerType, sender, e) =>
        {
            callCount2++;
            return true;
        };
        var listener3 = new CustomWeakEventListener();
        int callCount3 = 0;
        listener3.ReceiveWeakEventAction += (managerType, sender, e) =>
        {
            callCount3++;
            return true;
        };

        // Add.
        CurrentChangedEventManager.AddListener(source1, listener1);
        source1.OnCurrentChanged(source1, EventArgs.Empty);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCurrentChanged(source2, EventArgs.Empty);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);

        // Add again.
        CurrentChangedEventManager.AddListener(source1, listener1);
        source1.OnCurrentChanged(source1, EventArgs.Empty);
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCurrentChanged(source2, EventArgs.Empty);
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);

        // Add another listener.
        CurrentChangedEventManager.AddListener(source1, listener2);
        source1.OnCurrentChanged(source1, EventArgs.Empty);
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCurrentChanged(source2, EventArgs.Empty);
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);
        Assert.Equal(0, callCount3);

        // Add another source.
        CurrentChangedEventManager.AddListener(source2, listener3);
        source1.OnCurrentChanged(source1, EventArgs.Empty);
        Assert.Equal(7, callCount1);
        Assert.Equal(2, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCurrentChanged(source2, EventArgs.Empty);
        Assert.Equal(7, callCount1);
        Assert.Equal(2, callCount2);
        Assert.Equal(1, callCount3);
    }

    [Fact]
    public void AddListener_NullSource_ThrowsArgumentNullException()
    {
        var listener = new CustomWeakEventListener();
        Assert.Throws<ArgumentNullException>("source", () => CurrentChangedEventManager.AddListener(null, listener));
    }

    [Fact]
    public void AddListener_NullListener_ThrowsArgumentNullException()
    {
        var source = new CustomCollectionView();
        Assert.Throws<ArgumentNullException>("listener", () => CurrentChangedEventManager.AddListener(source, null));
    }

    [Fact]
    public void RemoveHandler_Invoke_Success()
    {
        var source = new CustomCollectionView();
        var target = new CustomWeakEventListener();
        int callCount = 0;
        target.HandlerAction += (sender, e) => callCount++;
        EventHandler<EventArgs> handler = (EventHandler<EventArgs>)Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), target, nameof(CustomWeakEventListener.Handler));
        CurrentChangedEventManager.AddHandler(source, handler);

        // Remove.
        CurrentChangedEventManager.RemoveHandler(source, handler);
        source.OnCurrentChanged(source, EventArgs.Empty);
        Assert.Equal(0, callCount);

        // Remove again.
        CurrentChangedEventManager.RemoveHandler(source, handler);
        source.OnCurrentChanged(source, EventArgs.Empty);
        Assert.Equal(0, callCount);
    }

    // [Fact]
    // public void RemoveHandler_InvokeNoSource_Success()
    // {
    //     var target = new CustomWeakEventListener();
    //     EventHandler<EventArgs> handler = (EventHandler<EventArgs>)Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), target, nameof(CustomWeakEventListener.Handler));
    //     try
    //     {
    //         CurrentChangedEventManager.AddHandler(null, handler);

    //         // Remove.
    //         CurrentChangedEventManager.RemoveHandler(null, handler);

    //         // Remove again.
    //         CurrentChangedEventManager.RemoveHandler(null, handler);
    //     }
    //     catch (NullReferenceException)
    //     {
    //         // There is a race condition where an NRE is thrown if the source is null and
    //         // no previous listeners or handlers were ever added.
    //     }
    // }

    [Fact]
    public void RemoveHandler_InvokeNoSuchSource_Nop()
    {
        var source1 = new CustomCollectionView();
        var source2 = new CustomCollectionView();
        var target = new CustomWeakEventListener();
        int callCount = 0;
        target.HandlerAction += (sender, e) => callCount++;
        EventHandler<EventArgs> handler = (EventHandler<EventArgs>)Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), target, nameof(CustomWeakEventListener.Handler));
        CurrentChangedEventManager.AddHandler(source1, handler);

        // Remove.
        CurrentChangedEventManager.RemoveHandler(source2, handler);
        source1.OnCurrentChanged(source1, EventArgs.Empty);
        Assert.Equal(1, callCount);
        source2.OnCurrentChanged(source2, EventArgs.Empty);
        Assert.Equal(1, callCount);

        // Remove again.
        CurrentChangedEventManager.RemoveHandler(source2, handler);
        source1.OnCurrentChanged(source1, EventArgs.Empty);
        Assert.Equal(2, callCount);
        source2.OnCurrentChanged(source2, EventArgs.Empty);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void RemoveHandler_InvokeNoSuchHandler_Nop()
    {
        var source = new CustomCollectionView();
        var target1 = new CustomWeakEventListener();
        int callCount1 = 0;
        target1.HandlerAction += (sender, e) => callCount1++;
        EventHandler<EventArgs> handler1 = (EventHandler<EventArgs>)Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), target1, nameof(CustomWeakEventListener.Handler));
        var target2 = new CustomWeakEventListener();
        int callCount2 = 0;
        target2.HandlerAction += (sender, e) => callCount2++;
        EventHandler<EventArgs> handler2 = (EventHandler<EventArgs>)Delegate.CreateDelegate(typeof(EventHandler<EventArgs>), target2, nameof(CustomWeakEventListener.Handler));
        CurrentChangedEventManager.AddHandler(source, handler1);

        // Remove.
        CurrentChangedEventManager.RemoveHandler(source, handler2);
        source.OnCurrentChanged(source, EventArgs.Empty);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Remove again.
        CurrentChangedEventManager.RemoveHandler(source, handler2);
        source.OnCurrentChanged(source, EventArgs.Empty);
        Assert.Equal(2, callCount1);
        Assert.Equal(0, callCount2);
    }

    [Fact]
    public void RemoveHandler_NullHandler_ThrowsArgumentNullException()
    {
        var source = new CustomCollectionView();
        Assert.Throws<ArgumentNullException>("handler", () => CurrentChangedEventManager.RemoveHandler(null, null));
        Assert.Throws<ArgumentNullException>("handler", () => CurrentChangedEventManager.RemoveHandler(source, null));
    }

    [Fact]
    public void RemoveListener_Invoke_Success()
    {
        var source = new CustomCollectionView();
        var listener = new CustomWeakEventListener();
        int callCount = 0;
        listener.ReceiveWeakEventAction += (managerType, sender, e) =>
        {
            callCount++;
            return true;
        };
        CurrentChangedEventManager.AddListener(source, listener);

        // Remove.
        CurrentChangedEventManager.RemoveListener(source, listener);
        source.OnCurrentChanged(source, EventArgs.Empty);
        Assert.Equal(0, callCount);

        // Remove again.
        CurrentChangedEventManager.RemoveListener(source, listener);
        source.OnCurrentChanged(source, EventArgs.Empty);
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void RemoveListener_NoSuchSource_Success()
    {
        var source1 = new CustomCollectionView();
        var source2 = new CustomCollectionView();
        var listener = new CustomWeakEventListener();
        int callCount = 0;
        listener.ReceiveWeakEventAction += (managerType, sender, e) =>
        {
            callCount++;
            return true;
        };
        CurrentChangedEventManager.AddListener(source1, listener);

        // Remove.
        CurrentChangedEventManager.RemoveListener(source2, listener);
        source1.OnCurrentChanged(source1, EventArgs.Empty);
        Assert.Equal(1, callCount);
        source2.OnCurrentChanged(source2, EventArgs.Empty);
        Assert.Equal(1, callCount);

        // Remove again.
        CurrentChangedEventManager.RemoveListener(source2, listener);
        source1.OnCurrentChanged(source1, EventArgs.Empty);
        Assert.Equal(2, callCount);
        source2.OnCurrentChanged(source2, EventArgs.Empty);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void RemoveListener_InvokeNoSuchListener_Nop()
    {
        var source = new CustomCollectionView();
        var listener1 = new CustomWeakEventListener();
        int callCount1 = 0;
        listener1.ReceiveWeakEventAction += (managerType, sender, e) =>
        {
            callCount1++;
            return true;
        };
        var listener2 = new CustomWeakEventListener();
        int callCount2 = 0;
        listener2.ReceiveWeakEventAction += (managerType, sender, e) =>
        {
            callCount2++;
            return true;
        };
        CurrentChangedEventManager.AddListener(source, listener1);

        // Remove.
        CurrentChangedEventManager.RemoveListener(source, listener2);
        source.OnCurrentChanged(source, EventArgs.Empty);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Remove again.
        CurrentChangedEventManager.RemoveListener(source, listener2);
        source.OnCurrentChanged(source, EventArgs.Empty);
        Assert.Equal(2, callCount1);
    }

    [Fact]
    public void RemoveListener_NullListener_ThrowsArgumentNullException()
    {
        var source = new CustomCollectionView();
        Assert.Throws<ArgumentNullException>("listener", () => CurrentChangedEventManager.RemoveListener(source, null));
    }

    private class CustomCollectionView : ICollectionView
    {
        public bool CanFilter => throw new NotImplementedException();

        public bool CanGroup => throw new NotImplementedException();

        public bool CanSort => throw new NotImplementedException();

        public CultureInfo Culture
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public object CurrentItem => throw new NotImplementedException();

        public int CurrentPosition => throw new NotImplementedException();

        public Predicate<object> Filter
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public ObservableCollection<GroupDescription> GroupDescriptions => throw new NotImplementedException();

        public ReadOnlyObservableCollection<object> Groups => throw new NotImplementedException();

        public bool IsCurrentAfterLast => throw new NotImplementedException();

        public bool IsCurrentBeforeFirst => throw new NotImplementedException();

        public bool IsEmpty => throw new NotImplementedException();

        public SortDescriptionCollection SortDescriptions => throw new NotImplementedException();

        public IEnumerable SourceCollection => throw new NotImplementedException();

        #pragma warning disable CS0067
        public event EventHandler? CurrentChanged;
        public event CurrentChangingEventHandler? CurrentChanging;
        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        #pragma warning restore CS0067

        public bool Contains(object item) => throw new NotImplementedException();

        public IDisposable DeferRefresh() => throw new NotImplementedException();

        public IEnumerator GetEnumerator() => throw new NotImplementedException();

        public bool MoveCurrentTo(object item) => throw new NotImplementedException();

        public bool MoveCurrentToFirst() => throw new NotImplementedException();

        public bool MoveCurrentToLast() => throw new NotImplementedException();

        public bool MoveCurrentToNext() => throw new NotImplementedException();

        public bool MoveCurrentToPosition(int position) => throw new NotImplementedException();

        public bool MoveCurrentToPrevious() => throw new NotImplementedException();

        public void Refresh() => throw new NotImplementedException();

        public void OnCurrentChanged(object sender, EventArgs e) => CurrentChanged?.Invoke(sender, e);
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

        public Action<object, EventArgs>? HandlerAction { get; set; }

        public void Handler(object sender, EventArgs e)
        {
            HandlerAction?.Invoke(sender, e);
        }
    }
}
