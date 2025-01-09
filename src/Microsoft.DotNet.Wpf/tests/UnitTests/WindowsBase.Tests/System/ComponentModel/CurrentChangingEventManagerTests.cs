// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;

namespace System.ComponentModel.Tests;

public class CurrentChangingEventManagerTests
{
    public static IEnumerable<object?[]> AddHandler_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { new CurrentChangingEventArgs(false) };
    }

    [Theory]
    [MemberData(nameof(AddHandler_TestData))]
    public void AddHandler_InvokeWithHandler_CallsCurrentChanging(CurrentChangingEventArgs e)
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
        EventHandler<CurrentChangingEventArgs> handler1 = (EventHandler<CurrentChangingEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<CurrentChangingEventArgs>), listener1, nameof(CustomWeakEventListener.Handler));
        int callCount2 = 0;
        var listener2 = new CustomWeakEventListener();
        listener2.HandlerAction += (actualSender, actualE) =>
        {
            Assert.Same(source, actualSender);
            Assert.Same(e, actualE);
            callCount2++;
        };
        EventHandler<CurrentChangingEventArgs> handler2 = (EventHandler<CurrentChangingEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<CurrentChangingEventArgs>), listener2, nameof(CustomWeakEventListener.Handler));

        // Add.
        CurrentChangingEventManager.AddHandler(source, handler1);

        // Call.
        source.OnCurrentChanging(source, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Call invalid source.
        source.OnCurrentChanging(listener1, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        
        source.OnCurrentChanging(new object(), e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        
        source.OnCurrentChanging(null!, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Add again.
        CurrentChangingEventManager.AddHandler(source, handler1);
        source.OnCurrentChanging(source, e);
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);

        // Add another.
        CurrentChangingEventManager.AddHandler(source, handler2);
        source.OnCurrentChanging(source, e);
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);

        // Remove second.
        CurrentChangingEventManager.RemoveHandler(source, handler2);
        source.OnCurrentChanging(source, e);
        Assert.Equal(7, callCount1);
        Assert.Equal(1, callCount2);

        // Remove first.
        CurrentChangingEventManager.RemoveHandler(source, handler1);
        source.OnCurrentChanging(source, e);
        Assert.Equal(8, callCount1);
        Assert.Equal(1, callCount2);

        // Remove again.
        CurrentChangingEventManager.RemoveHandler(source, handler1);
        source.OnCurrentChanging(source, e);
        Assert.Equal(8, callCount1);
        Assert.Equal(1, callCount2);
    }

    [Fact]
    public void AddHandler_InvokeMultipleTimes_Success()
    {
        var source1 = new CustomCollectionView();
        var source2 = new CustomCollectionView();
        var target1 = new CustomWeakEventListener();
        int callCount1 = 0;
        target1.HandlerAction += (s, e) => callCount1++;
        EventHandler<CurrentChangingEventArgs> handler1 = (EventHandler<CurrentChangingEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<CurrentChangingEventArgs>), target1, nameof(CustomWeakEventListener.Handler));
        var target2 = new CustomWeakEventListener();
        int callCount2 = 0;
        target2.HandlerAction += (s, e) => callCount2++;
        EventHandler<CurrentChangingEventArgs> handler2 = (EventHandler<CurrentChangingEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<CurrentChangingEventArgs>), target2, nameof(CustomWeakEventListener.Handler));
        var target3 = new CustomWeakEventListener();
        int callCount3 = 0;
        target3.HandlerAction += (s, e) => callCount3++;
        EventHandler<CurrentChangingEventArgs> handler3 = (EventHandler<CurrentChangingEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<CurrentChangingEventArgs>), target3, nameof(CustomWeakEventListener.Handler));

        // Add.
        CurrentChangingEventManager.AddHandler(source1, handler1);
        source1.OnCurrentChanging(source1, new CurrentChangingEventArgs(false));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCurrentChanging(source2, new CurrentChangingEventArgs(false));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);

        // Add again.
        CurrentChangingEventManager.AddHandler(source1, handler1);
        source1.OnCurrentChanging(source1, new CurrentChangingEventArgs(false));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCurrentChanging(source2, new CurrentChangingEventArgs(false));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);

        // Add another handler.
        CurrentChangingEventManager.AddHandler(source1, handler2);
        source1.OnCurrentChanging(source1, new CurrentChangingEventArgs(false));
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCurrentChanging(source2, new CurrentChangingEventArgs(false));
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);
        Assert.Equal(0, callCount3);

        // Add another source.
        CurrentChangingEventManager.AddHandler(source2, handler3);
        source1.OnCurrentChanging(source1, new CurrentChangingEventArgs(false));
        Assert.Equal(7, callCount1);
        Assert.Equal(2, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCurrentChanging(source2, new CurrentChangingEventArgs(false));
        Assert.Equal(7, callCount1);
        Assert.Equal(2, callCount2);
        Assert.Equal(1, callCount3);
    }

    // [Fact]
    // public void AddHandler_InvokeNoSource_Success()
    // {
    //     var target1 = new CustomWeakEventListener();
    //     EventHandler<CurrentChangingEventArgs> handler1 = (EventHandler<CurrentChangingEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<CurrentChangingEventArgs>), target1, nameof(CustomWeakEventListener.Handler));
    //     var target2 = new CustomWeakEventListener();
    //     EventHandler<CurrentChangingEventArgs> handler2 = (EventHandler<CurrentChangingEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<CurrentChangingEventArgs>), target2, nameof(CustomWeakEventListener.Handler));

    //     // Add.
    //     Assert.Throws<NullReferenceException>(() => CurrentChangingEventManager.AddHandler(null, handler1));

    //     // Add again.
    //     CurrentChangingEventManager.AddHandler(null, handler1);

    //     // Add another.
    //     CurrentChangingEventManager.AddHandler(null, handler2);
    // }

    [Fact]
    public void AddHandler_NullHandler_ThrowsArgumentNullException()
    {
        var source = new CustomCollectionView();
        Assert.Throws<ArgumentNullException>("handler", () => CurrentChangingEventManager.AddHandler(null, null));
        Assert.Throws<ArgumentNullException>("handler", () => CurrentChangingEventManager.AddHandler(source, null));
    }

    public static IEnumerable<object?[]> AddListener_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { new CurrentChangingEventArgs(false) };
    }

    [Theory]
    [MemberData(nameof(AddListener_TestData))]
    public void AddListener_InvokeWithHandler_CallsCurrentChanging(CurrentChangingEventArgs e)
    {
        var source = new CustomCollectionView();
        int callCount1 = 0;
        var listener1 = new CustomWeakEventListener();
        listener1.ReceiveWeakEventAction += (actualManagerType, actualSender, actualE) =>
        {
            Assert.Equal(typeof(CurrentChangingEventManager), actualManagerType);
            Assert.Same(source, actualSender);
            Assert.Same(e, actualE);
            callCount1++;
            return true;
        };
        int callCount2 = 0;
        var listener2 = new CustomWeakEventListener();
        listener2.ReceiveWeakEventAction += (actualManagerType, actualSender, actualE) =>
        {
            Assert.Equal(typeof(CurrentChangingEventManager), actualManagerType);
            Assert.Same(source, actualSender);
            Assert.Same(e, actualE);
            callCount2++;
            return true;
        };

        // Add.
        CurrentChangingEventManager.AddListener(source, listener1);

        // Call.
        source.OnCurrentChanging(source, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Call invalid source.
        source.OnCurrentChanging(listener1, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        
        source.OnCurrentChanging(new object(), e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        
        source.OnCurrentChanging(null!, e);
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Add again.
        CurrentChangingEventManager.AddListener(source, listener1);
        source.OnCurrentChanging(source, e);
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);

        // Add another.
        CurrentChangingEventManager.AddListener(source, listener2);
        source.OnCurrentChanging(source, e);
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);

        // Remove second.
        CurrentChangingEventManager.RemoveListener(source, listener2);
        source.OnCurrentChanging(source, e);
        Assert.Equal(7, callCount1);
        Assert.Equal(1, callCount2);

        // Remove first.
        CurrentChangingEventManager.RemoveListener(source, listener1);
        source.OnCurrentChanging(source, e);
        Assert.Equal(8, callCount1);
        Assert.Equal(1, callCount2);

        // Remove again.
        CurrentChangingEventManager.RemoveListener(source, listener1);
        source.OnCurrentChanging(source, e);
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
        CurrentChangingEventManager.AddListener(source1, listener1);
        source1.OnCurrentChanging(source1, new CurrentChangingEventArgs(false));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCurrentChanging(source2, new CurrentChangingEventArgs(false));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);

        // Add again.
        CurrentChangingEventManager.AddListener(source1, listener1);
        source1.OnCurrentChanging(source1, new CurrentChangingEventArgs(false));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCurrentChanging(source2, new CurrentChangingEventArgs(false));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);

        // Add another listener.
        CurrentChangingEventManager.AddListener(source1, listener2);
        source1.OnCurrentChanging(source1, new CurrentChangingEventArgs(false));
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCurrentChanging(source2, new CurrentChangingEventArgs(false));
        Assert.Equal(5, callCount1);
        Assert.Equal(1, callCount2);
        Assert.Equal(0, callCount3);
        
        // Add another source.
        CurrentChangingEventManager.AddListener(source2, listener3);
        source1.OnCurrentChanging(source1, new CurrentChangingEventArgs(false));
        Assert.Equal(7, callCount1);
        Assert.Equal(2, callCount2);
        Assert.Equal(0, callCount3);
        source2.OnCurrentChanging(source2, new CurrentChangingEventArgs(false));
        Assert.Equal(7, callCount1);
        Assert.Equal(2, callCount2);
        Assert.Equal(1, callCount3);
    }

    [Fact]
    public void AddListener_NullSource_ThrowsArgumentNullException()
    {
        var listener = new CustomWeakEventListener();
        Assert.Throws<ArgumentNullException>("source", () => CurrentChangingEventManager.AddListener(null, listener));
    }

    [Fact]
    public void AddListener_NullListener_ThrowsArgumentNullException()
    {
        var source = new CustomCollectionView();
        Assert.Throws<ArgumentNullException>("listener", () => CurrentChangingEventManager.AddListener(source, null));
    }

    [Fact]
    public void RemoveHandler_Invoke_Success()
    {
        var source = new CustomCollectionView();
        var target = new CustomWeakEventListener();
        int callCount = 0;
        target.HandlerAction += (s, e) => callCount++;
        EventHandler<CurrentChangingEventArgs> handler = (EventHandler<CurrentChangingEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<CurrentChangingEventArgs>), target, nameof(CustomWeakEventListener.Handler));
        CurrentChangingEventManager.AddHandler(source, handler);

        // Remove.
        CurrentChangingEventManager.RemoveHandler(source, handler);
        source.OnCurrentChanging(source, new CurrentChangingEventArgs(false));
        Assert.Equal(0, callCount);

        // Remove again.
        CurrentChangingEventManager.RemoveHandler(source, handler);
        source.OnCurrentChanging(source, new CurrentChangingEventArgs(false));
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void RemoveHandler_InvokeNoSource_Success()
    {
        var target = new CustomWeakEventListener();
        EventHandler<CurrentChangingEventArgs> handler = (EventHandler<CurrentChangingEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<CurrentChangingEventArgs>), target, nameof(CustomWeakEventListener.Handler));
        Assert.Throws<NullReferenceException>(() => CurrentChangingEventManager.AddHandler(null, handler));

        // Remove.
        Assert.Throws<NullReferenceException>(() => CurrentChangingEventManager.RemoveHandler(null, handler));

        // Remove again.
        CurrentChangingEventManager.RemoveHandler(null, handler);
    }

    [Fact]
    public void RemoveHandler_InvokeNoSuchSource_Nop()
    {
        var source1 = new CustomCollectionView();
        var source2 = new CustomCollectionView();
        var target = new CustomWeakEventListener();
        int callCount = 0;
        target.HandlerAction += (s, e) => callCount++;
        EventHandler<CurrentChangingEventArgs> handler = (EventHandler<CurrentChangingEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<CurrentChangingEventArgs>), target, nameof(CustomWeakEventListener.Handler));
        CurrentChangingEventManager.AddHandler(source1, handler);

        // Remove.
        CurrentChangingEventManager.RemoveHandler(source2, handler);
        source1.OnCurrentChanging(source1, new CurrentChangingEventArgs(false));
        Assert.Equal(1, callCount);
        source2.OnCurrentChanging(source2, new CurrentChangingEventArgs(false));
        Assert.Equal(1, callCount);

        // Remove again.
        CurrentChangingEventManager.RemoveHandler(source2, handler);
        source1.OnCurrentChanging(source1, new CurrentChangingEventArgs(false));
        Assert.Equal(2, callCount);
        source2.OnCurrentChanging(source2, new CurrentChangingEventArgs(false));
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void RemoveHandler_InvokeNoSuchHandler_Nop()
    {
        var source = new CustomCollectionView();
        var target1 = new CustomWeakEventListener();
        int callCount1 = 0;
        target1.HandlerAction += (s, e) => callCount1++;
        EventHandler<CurrentChangingEventArgs> handler1 = (EventHandler<CurrentChangingEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<CurrentChangingEventArgs>), target1, nameof(CustomWeakEventListener.Handler));
        var target2 = new CustomWeakEventListener();
        int callCount2 = 0;
        target2.HandlerAction += (s, e) => callCount2++;
        EventHandler<CurrentChangingEventArgs> handler2 = (EventHandler<CurrentChangingEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<CurrentChangingEventArgs>), target2, nameof(CustomWeakEventListener.Handler));
        CurrentChangingEventManager.AddHandler(source, handler1);

        // Remove.
        CurrentChangingEventManager.RemoveHandler(source, handler2);
        source.OnCurrentChanging(source, new CurrentChangingEventArgs(false));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Remove again.
        CurrentChangingEventManager.RemoveHandler(source, handler2);
        source.OnCurrentChanging(source, new CurrentChangingEventArgs(false));
        Assert.Equal(2, callCount1);
        Assert.Equal(0, callCount2);
    }

    [Fact]
    public void RemoveHandler_NullHandler_ThrowsArgumentNullException()
    {
        var source = new CustomCollectionView();
        Assert.Throws<ArgumentNullException>("handler", () => CurrentChangingEventManager.RemoveHandler(null, null));
        Assert.Throws<ArgumentNullException>("handler", () => CurrentChangingEventManager.RemoveHandler(source, null));
    }

    [Fact]
    public void RemoveListener_Invoke_Success()
    {
        var source = new CustomCollectionView();
        var listener = new CustomWeakEventListener();
        int callCount = 0;
        listener.ReceiveWeakEventAction += (t, s, e) =>
        {
            callCount++;
            return true;
        };
        CurrentChangingEventManager.AddListener(source, listener);

        // Remove.
        CurrentChangingEventManager.RemoveListener(source, listener);
        source.OnCurrentChanging(source, new CurrentChangingEventArgs(false));
        Assert.Equal(0, callCount);

        // Remove again.
        CurrentChangingEventManager.RemoveListener(source, listener);
        source.OnCurrentChanging(source, new CurrentChangingEventArgs(false));
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void RemoveListener_InvokeNoSuchSource_Nop()
    {
        var source1 = new CustomCollectionView();
        var source2 = new CustomCollectionView();
        var listener = new CustomWeakEventListener();
        int callCount = 0;
        listener.ReceiveWeakEventAction += (t, s, e) =>
        {
            callCount++;
            return true;
        };
        CurrentChangingEventManager.AddListener(source1, listener);

        // Remove.
        CurrentChangingEventManager.RemoveListener(source2, listener);
        source1.OnCurrentChanging(source1, new CurrentChangingEventArgs(false));
        Assert.Equal(1, callCount);
        source2.OnCurrentChanging(source2, new CurrentChangingEventArgs(false));
        Assert.Equal(1, callCount);

        // Remove again.
        CurrentChangingEventManager.RemoveListener(source2, listener);
        source1.OnCurrentChanging(source1, new CurrentChangingEventArgs(false));
        Assert.Equal(2, callCount);
        source2.OnCurrentChanging(source2, new CurrentChangingEventArgs(false));
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void RemoveListener_InvokeNoSuchListener_Nop()
    {
        var source = new CustomCollectionView();
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
        CurrentChangingEventManager.AddListener(source, listener1);

        // Remove.
        CurrentChangingEventManager.RemoveListener(source, listener2);
        source.OnCurrentChanging(source, new CurrentChangingEventArgs(false));
        Assert.Equal(1, callCount1);

        // Remove again.
        CurrentChangingEventManager.RemoveListener(source, listener2);
        source.OnCurrentChanging(source, new CurrentChangingEventArgs(false));
        Assert.Equal(2, callCount1);
    }

    [Fact]
    public void RemoveListener_NullListener_ThrowsArgumentNullException()
    {
        var source = new CustomCollectionView();
        Assert.Throws<ArgumentNullException>("listener", () => CurrentChangingEventManager.RemoveListener(source, null));
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

        public void OnCurrentChanging(object sender, CurrentChangingEventArgs e) => CurrentChanging?.Invoke(sender, e);
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

        public Action<object, CurrentChangingEventArgs>? HandlerAction { get; set; }

        public void Handler(object sender, CurrentChangingEventArgs e)
        {
            HandlerAction?.Invoke(sender, e);
        }
    }
}
