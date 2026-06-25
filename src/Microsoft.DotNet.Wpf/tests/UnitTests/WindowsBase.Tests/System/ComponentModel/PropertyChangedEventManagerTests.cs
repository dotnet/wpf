// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows;

namespace System.ComponentModel.Tests;

public class PropertyChangedEventManagerTests
{
    [Fact]
    public void AddHandler_InvokeWithHandler_CallsErrorsChanged()
    {
        var source = new CustomNotifyPropertyChanged();
        int callCount1 = 0;
        var listener1 = new CustomWeakEventListener();
        listener1.HandlerAction += (actualSender, actualE) =>
        {
            Assert.Same(source, actualSender);
            callCount1++;
        };
        EventHandler<PropertyChangedEventArgs> handler1 = (EventHandler<PropertyChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<PropertyChangedEventArgs>), listener1, nameof(CustomWeakEventListener.Handler));
        int callCount2 = 0;
        var listener2 = new CustomWeakEventListener();
        listener2.HandlerAction += (actualSender, actualE) =>
        {
            Assert.Same(source, actualSender);
            callCount2++;
        };
        EventHandler<PropertyChangedEventArgs> handler2 = (EventHandler<PropertyChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<PropertyChangedEventArgs>), listener2, nameof(CustomWeakEventListener.Handler));

        // Add.
        PropertyChangedEventManager.AddHandler(source, handler1, "propertyName");

        // Call.
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Call null property name.
        source.OnPropertyChanged(source, new PropertyChangedEventArgs(null));
        Assert.Equal(2, callCount1);
        Assert.Equal(0, callCount2);

        // Call empty property name.
        source.OnPropertyChanged(source, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);

        // Call invalid source.
        source.OnPropertyChanged(listener1, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(3, callCount1);    
        Assert.Equal(0, callCount2);
        
        source.OnPropertyChanged(new object(), new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        
        source.OnPropertyChanged(null!, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);

        // Add again.
        PropertyChangedEventManager.AddHandler(source, handler1, "propertyName");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(5, callCount1);
        Assert.Equal(0, callCount2);

        // Add another.
        PropertyChangedEventManager.AddHandler(source, handler2, "propertyName");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(7, callCount1);
        Assert.Equal(1, callCount2);

        // Remove second.
        PropertyChangedEventManager.RemoveHandler(source, handler2, "propertyName");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(9, callCount1);
        Assert.Equal(1, callCount2);

        // Remove first.
        PropertyChangedEventManager.RemoveHandler(source, handler1, "propertyName");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(10, callCount1);
        Assert.Equal(1, callCount2);

        // Remove again.
        PropertyChangedEventManager.RemoveHandler(source, handler1, "propertyName");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(10, callCount1);
        Assert.Equal(1, callCount2);
    }

    [Theory]
    [InlineData("propertyName")]
    [InlineData("PROPERTYNAME")]
    public void AddHandler_InvokeMultipleTimes_Success(string propertyName)
    {
        var source1 = new CustomNotifyPropertyChanged();
        var source2 = new CustomNotifyPropertyChanged();
        var target1 = new CustomWeakEventListener();
        int callCount1 = 0;
        target1.HandlerAction += (s, e) => callCount1++;
        EventHandler<PropertyChangedEventArgs> handler1 = (EventHandler<PropertyChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<PropertyChangedEventArgs>), target1, nameof(CustomWeakEventListener.Handler));
        var target2 = new CustomWeakEventListener();
        int callCount2 = 0;
        target2.HandlerAction += (s, e) => callCount2++;
        EventHandler<PropertyChangedEventArgs> handler2 = (EventHandler<PropertyChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<PropertyChangedEventArgs>), target2, nameof(CustomWeakEventListener.Handler));
        var target3 = new CustomWeakEventListener();
        int callCount3 = 0;
        target3.HandlerAction += (s, e) => callCount3++;
        EventHandler<PropertyChangedEventArgs> handler3 = (EventHandler<PropertyChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<PropertyChangedEventArgs>), target3, nameof(CustomWeakEventListener.Handler));
        var target4 = new CustomWeakEventListener();
        int callCount4 = 0;
        target4.HandlerAction += (s, e) => callCount4++;
        EventHandler<PropertyChangedEventArgs> handler4 = (EventHandler<PropertyChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<PropertyChangedEventArgs>), target4, nameof(CustomWeakEventListener.Handler));
        var target5 = new CustomWeakEventListener();
        int callCount5 = 0;
        target5.HandlerAction += (s, e) => callCount5++;
        EventHandler<PropertyChangedEventArgs> handler5 = (EventHandler<PropertyChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<PropertyChangedEventArgs>), target5, nameof(CustomWeakEventListener.Handler));

        // Add.
        PropertyChangedEventManager.AddHandler(source1, handler1, "propertyName");
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs("propertyName2"));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(2, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(null));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(null));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);

        // Add again.
        PropertyChangedEventManager.AddHandler(source1, handler1, "propertyName");
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(5, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs("propertyName2"));
        Assert.Equal(5, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(7, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(null));
        Assert.Equal(9, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(9, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(9, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(null));
        Assert.Equal(9, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);

        // Add another handler.
        PropertyChangedEventManager.AddHandler(source1, handler2, "propertyName");
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(11, callCount1);
        Assert.Equal(1, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs("propertyName2"));
        Assert.Equal(11, callCount1);
        Assert.Equal(1, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(13, callCount1);
        Assert.Equal(2, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(null));
        Assert.Equal(15, callCount1);
        Assert.Equal(3, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(15, callCount1);
        Assert.Equal(3, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(15, callCount1);
        Assert.Equal(3, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(null));
        Assert.Equal(15, callCount1);
        Assert.Equal(3, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);

        // Add another property name.
        PropertyChangedEventManager.AddHandler(source1, handler3, "propertyName2");
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(17, callCount1);
        Assert.Equal(4, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs("propertyName2"));
        Assert.Equal(17, callCount1);
        Assert.Equal(4, callCount2);
        Assert.Equal(1, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(19, callCount1);
        Assert.Equal(5, callCount2);
        Assert.Equal(2, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(null));
        Assert.Equal(21, callCount1);
        Assert.Equal(6, callCount2);
        Assert.Equal(3, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(21, callCount1);
        Assert.Equal(6, callCount2);
        Assert.Equal(3, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(21, callCount1);
        Assert.Equal(6, callCount2);
        Assert.Equal(3, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(null));
        Assert.Equal(21, callCount1);
        Assert.Equal(6, callCount2);
        Assert.Equal(3, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);

        // Add another source.
        PropertyChangedEventManager.AddHandler(source2, handler4, "propertyName");
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(23, callCount1);
        Assert.Equal(7, callCount2);
        Assert.Equal(3, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs("propertyName2"));
        Assert.Equal(23, callCount1);
        Assert.Equal(7, callCount2);
        Assert.Equal(4, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(25, callCount1);
        Assert.Equal(8, callCount2);
        Assert.Equal(5, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(null));
        Assert.Equal(27, callCount1);
        Assert.Equal(9, callCount2);
        Assert.Equal(6, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(27, callCount1);
        Assert.Equal(9, callCount2);
        Assert.Equal(6, callCount3);
        Assert.Equal(1, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(27, callCount1);
        Assert.Equal(9, callCount2);
        Assert.Equal(6, callCount3);
        Assert.Equal(2, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(null));
        Assert.Equal(27, callCount1);
        Assert.Equal(9, callCount2);
        Assert.Equal(6, callCount3);
        Assert.Equal(3, callCount4);
        Assert.Equal(0, callCount5);

        // Add another source (empty).
        PropertyChangedEventManager.AddHandler(source2, handler5, "");
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(29, callCount1);
        Assert.Equal(10, callCount2);
        Assert.Equal(6, callCount3);
        Assert.Equal(3, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs("propertyName2"));
        Assert.Equal(29, callCount1);
        Assert.Equal(10, callCount2);
        Assert.Equal(7, callCount3);
        Assert.Equal(3, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(31, callCount1);
        Assert.Equal(11, callCount2);
        Assert.Equal(8, callCount3);
        Assert.Equal(3, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(null));
        Assert.Equal(33, callCount1);
        Assert.Equal(12, callCount2);
        Assert.Equal(9, callCount3);
        Assert.Equal(3, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(33, callCount1);
        Assert.Equal(12, callCount2);
        Assert.Equal(9, callCount3);
        Assert.Equal(4, callCount4);
        Assert.Equal(1, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(33, callCount1);
        Assert.Equal(12, callCount2);
        Assert.Equal(9, callCount3);
        Assert.Equal(5, callCount4);
        Assert.Equal(2, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(null));
        Assert.Equal(33, callCount1);
        Assert.Equal(12, callCount2);
        Assert.Equal(9, callCount3);
        Assert.Equal(6, callCount4);
        Assert.Equal(3, callCount5);
    }

    [Fact]
    public void AddHandler_NullSource_ThrowsArgumentNullException()
    {
        var target = new CustomWeakEventListener();
        EventHandler<PropertyChangedEventArgs> handler = (EventHandler<PropertyChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<PropertyChangedEventArgs>), target, nameof(CustomWeakEventListener.Handler));
        // TODO: this should be NullReferenceException.
        Assert.Throws<NullReferenceException>(() => PropertyChangedEventManager.AddHandler(null, handler, "propertyName"));
    
        // Add again.
        Assert.Throws<NullReferenceException>(() => PropertyChangedEventManager.AddHandler(null, handler, "propertyName"));
    }

    [Fact]
    public void AddHandler_NullHandler_ThrowsArgumentNullException()
    {
        var source = new CustomNotifyPropertyChanged();
        Assert.Throws<ArgumentNullException>("handler", () => PropertyChangedEventManager.AddHandler(source, null, "propertyName"));
    }

    [Fact]
    public void AddHandler_NullPropertyName_ThrowsArgumentNullException()
    {
        var source = new CustomNotifyPropertyChanged();
        var target = new CustomWeakEventListener();
        EventHandler<PropertyChangedEventArgs> handler = (EventHandler<PropertyChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<PropertyChangedEventArgs>), target, nameof(CustomWeakEventListener.Handler));
        // TODO: incorrect paramName.
        Assert.Throws<ArgumentNullException>("key", () => PropertyChangedEventManager.AddHandler(source, handler, null));
    }
    [Fact]
    public void AddListener_InvokeWithHandler_CallsErrorsChanged()
    {
        var source = new CustomNotifyPropertyChanged();
        int callCount1 = 0;
        var listener1 = new CustomWeakEventListener();
        listener1.ReceiveWeakEventAction += (actualManagerType, actualSender, actualE) =>
        {
            Assert.Equal(typeof(PropertyChangedEventManager), actualManagerType);
            Assert.Same(source, actualSender);
            callCount1++;
            return true;
        };
        int callCount2 = 0;
        var listener2 = new CustomWeakEventListener();
        listener2.ReceiveWeakEventAction += (actualManagerType, actualSender, actualE) =>
        {
            Assert.Equal(typeof(PropertyChangedEventManager), actualManagerType);
            Assert.Same(source, actualSender);
            callCount2++;
            return true;
        };

        // Add.
        PropertyChangedEventManager.AddListener(source, listener1, "propertyName");

        // Call.
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Call null property name.
        source.OnPropertyChanged(source, new PropertyChangedEventArgs(null));
        Assert.Equal(2, callCount1);
        Assert.Equal(0, callCount2);

        // Call empty property name.
        source.OnPropertyChanged(source, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);

        // Call invalid source.
        source.OnPropertyChanged(listener1, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(3, callCount1);    
        Assert.Equal(0, callCount2);
        
        source.OnPropertyChanged(new object(), new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        
        source.OnPropertyChanged(null!, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);

        // Add again.
        PropertyChangedEventManager.AddListener(source, listener1, "propertyName");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(5, callCount1);
        Assert.Equal(0, callCount2);

        // Add another.
        PropertyChangedEventManager.AddListener(source, listener2, "propertyName");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(7, callCount1);
        Assert.Equal(1, callCount2);

        // Remove second.
        PropertyChangedEventManager.RemoveListener(source, listener2, "propertyName");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(9, callCount1);
        Assert.Equal(1, callCount2);

        // Remove first.
        PropertyChangedEventManager.RemoveListener(source, listener1, "propertyName");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(10, callCount1);
        Assert.Equal(1, callCount2);

        // Remove again.
        PropertyChangedEventManager.RemoveListener(source, listener1, "propertyName");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(10, callCount1);
        Assert.Equal(1, callCount2);
    }

    [Theory]
    [InlineData("propertyName")]
    [InlineData("PROPERTYNAME")]
    public void AddListener_InvokeMultipleTimes_Success(string propertyName)
    {
        var source1 = new CustomNotifyPropertyChanged();
        var source2 = new CustomNotifyPropertyChanged();
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
        var listener4 = new CustomWeakEventListener();
        int callCount4 = 0;
        listener4.ReceiveWeakEventAction += (t, s, e) =>
        {
            callCount4++;
            return true;
        };
        var listener5 = new CustomWeakEventListener();
        int callCount5 = 0;
        listener5.ReceiveWeakEventAction += (t, s, e) =>
        {
            callCount5++;
            return true;
        };

        // Add.
        PropertyChangedEventManager.AddListener(source1, listener1, "propertyName");
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs("propertyName2"));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(2, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(null));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(null));
        Assert.Equal(3, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);

        // Add again.
        PropertyChangedEventManager.AddListener(source1, listener1, "propertyName");
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(5, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs("propertyName2"));
        Assert.Equal(5, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(7, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(null));
        Assert.Equal(9, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(9, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(9, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(null));
        Assert.Equal(9, callCount1);
        Assert.Equal(0, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);

        // Add another listener.
        PropertyChangedEventManager.AddListener(source1, listener2, "propertyName");
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(11, callCount1);
        Assert.Equal(1, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs("propertyName2"));
        Assert.Equal(11, callCount1);
        Assert.Equal(1, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(13, callCount1);
        Assert.Equal(2, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(null));
        Assert.Equal(15, callCount1);
        Assert.Equal(3, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(15, callCount1);
        Assert.Equal(3, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(15, callCount1);
        Assert.Equal(3, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(null));
        Assert.Equal(15, callCount1);
        Assert.Equal(3, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);

        // Add another property name.
        PropertyChangedEventManager.AddListener(source1, listener3, "propertyName2");
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(17, callCount1);
        Assert.Equal(4, callCount2);
        Assert.Equal(0, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs("propertyName2"));
        Assert.Equal(17, callCount1);
        Assert.Equal(4, callCount2);
        Assert.Equal(1, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(19, callCount1);
        Assert.Equal(5, callCount2);
        Assert.Equal(2, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(null));
        Assert.Equal(21, callCount1);
        Assert.Equal(6, callCount2);
        Assert.Equal(3, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(21, callCount1);
        Assert.Equal(6, callCount2);
        Assert.Equal(3, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(21, callCount1);
        Assert.Equal(6, callCount2);
        Assert.Equal(3, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(null));
        Assert.Equal(21, callCount1);
        Assert.Equal(6, callCount2);
        Assert.Equal(3, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);

        // Add another source.
        PropertyChangedEventManager.AddListener(source2, listener4, "propertyName");
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(23, callCount1);
        Assert.Equal(7, callCount2);
        Assert.Equal(3, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs("propertyName2"));
        Assert.Equal(23, callCount1);
        Assert.Equal(7, callCount2);
        Assert.Equal(4, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(25, callCount1);
        Assert.Equal(8, callCount2);
        Assert.Equal(5, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(null));
        Assert.Equal(27, callCount1);
        Assert.Equal(9, callCount2);
        Assert.Equal(6, callCount3);
        Assert.Equal(0, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(27, callCount1);
        Assert.Equal(9, callCount2);
        Assert.Equal(6, callCount3);
        Assert.Equal(1, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(27, callCount1);
        Assert.Equal(9, callCount2);
        Assert.Equal(6, callCount3);
        Assert.Equal(2, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(null));
        Assert.Equal(27, callCount1);
        Assert.Equal(9, callCount2);
        Assert.Equal(6, callCount3);
        Assert.Equal(3, callCount4);
        Assert.Equal(0, callCount5);

        // Add another source (empty).
        PropertyChangedEventManager.AddListener(source2, listener5, "");
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(29, callCount1);
        Assert.Equal(10, callCount2);
        Assert.Equal(6, callCount3);
        Assert.Equal(3, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs("propertyName2"));
        Assert.Equal(29, callCount1);
        Assert.Equal(10, callCount2);
        Assert.Equal(7, callCount3);
        Assert.Equal(3, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(31, callCount1);
        Assert.Equal(11, callCount2);
        Assert.Equal(8, callCount3);
        Assert.Equal(3, callCount4);
        Assert.Equal(0, callCount5);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs(null));
        Assert.Equal(33, callCount1);
        Assert.Equal(12, callCount2);
        Assert.Equal(9, callCount3);
        Assert.Equal(3, callCount4);
        Assert.Equal(0, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(propertyName));
        Assert.Equal(33, callCount1);
        Assert.Equal(12, callCount2);
        Assert.Equal(9, callCount3);
        Assert.Equal(4, callCount4);
        Assert.Equal(1, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(string.Empty));
        Assert.Equal(33, callCount1);
        Assert.Equal(12, callCount2);
        Assert.Equal(9, callCount3);
        Assert.Equal(5, callCount4);
        Assert.Equal(2, callCount5);
        source2.OnPropertyChanged(source2, new PropertyChangedEventArgs(null));
        Assert.Equal(33, callCount1);
        Assert.Equal(12, callCount2);
        Assert.Equal(9, callCount3);
        Assert.Equal(6, callCount4);
        Assert.Equal(3, callCount5);
    }

    [Fact]
    public void AddListener_NullSource_ThrowsArgumentNullException()
    {
        var listener = new CustomWeakEventListener();
        Assert.Throws<ArgumentNullException>("source", () => PropertyChangedEventManager.AddListener(null, listener, "propertyName"));
    
        // Add again.
        Assert.Throws<ArgumentNullException>("source", () => PropertyChangedEventManager.AddListener(null, listener, "propertyName"));
    }

    [Fact]
    public void AddListener_NullListener_ThrowsArgumentNullException()
    {
        var source = new CustomNotifyPropertyChanged();
        Assert.Throws<ArgumentNullException>("listener", () => PropertyChangedEventManager.AddListener(source, null, "propertyName"));
    }

#if !DEBUG // This triggers a Debug.Assert.
    [Fact]
    public void AddListener_NullPropertyName_ThrowsArgumentNullException()
    {
        var source = new CustomNotifyPropertyChanged();
        var listener = new CustomWeakEventListener();
        // TODO: incorrect paramName.
        Assert.Throws<ArgumentNullException>("key", () => PropertyChangedEventManager.AddListener(source, listener, null));
    }
#endif

    [Theory]
    [InlineData("propertyName")]
    [InlineData("PROPERTYNAME")]
    public void RemoveHandler_Invoke_Success(string propertyName)
    {
        var source = new CustomNotifyPropertyChanged();
        var target = new CustomWeakEventListener();
        int callCount = 0;
        target.HandlerAction += (s, e) => callCount++;
        EventHandler<PropertyChangedEventArgs> handler = (EventHandler<PropertyChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<PropertyChangedEventArgs>), target, nameof(CustomWeakEventListener.Handler));
        PropertyChangedEventManager.AddHandler(source, handler, "propertyName");

        // Remove.
        PropertyChangedEventManager.RemoveHandler(source, handler, propertyName);
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(0, callCount);

        // Remove again.
        PropertyChangedEventManager.RemoveHandler(source, handler, propertyName);
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void RemoveHandler_InvokeMultipleNames_Success()
    {
        var source = new CustomNotifyPropertyChanged();
        var target1 = new CustomWeakEventListener();
        int callCount1 = 0;
        target1.HandlerAction += (s, e) => callCount1++;
        EventHandler<PropertyChangedEventArgs> handler1 = (EventHandler<PropertyChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<PropertyChangedEventArgs>), target1, nameof(CustomWeakEventListener.Handler));
        var target2 = new CustomWeakEventListener();
        int callCount2 = 0;
        target2.HandlerAction += (s, e) => callCount2++;
        EventHandler<PropertyChangedEventArgs> handler2 = (EventHandler<PropertyChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<PropertyChangedEventArgs>), target2, nameof(CustomWeakEventListener.Handler));
        var target3 = new CustomWeakEventListener();
        int callCount3 = 0;
        target3.HandlerAction += (s, e) => callCount3++;
        EventHandler<PropertyChangedEventArgs> handler3 = (EventHandler<PropertyChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<PropertyChangedEventArgs>), target3, nameof(CustomWeakEventListener.Handler));
        PropertyChangedEventManager.AddHandler(source, handler1, "propertyName1");
        PropertyChangedEventManager.AddHandler(source, handler2, "propertyName2");
        PropertyChangedEventManager.AddHandler(source, handler3, "");

        // Remove.
        PropertyChangedEventManager.RemoveHandler(source, handler1, "propertyName1");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs(null));
        Assert.Equal(0, callCount1);
        Assert.Equal(1, callCount2);
        Assert.Equal(1, callCount3);

        // Remove again.
        PropertyChangedEventManager.RemoveHandler(source, handler1, "propertyName1");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs(null));
        Assert.Equal(0, callCount1);
        Assert.Equal(2, callCount2);
        Assert.Equal(2, callCount3);

        // Remove another.
        PropertyChangedEventManager.RemoveHandler(source, handler3, "");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs(null));
        Assert.Equal(0, callCount1);
        Assert.Equal(3, callCount2);
        Assert.Equal(2, callCount3);

        // Remove last.
        PropertyChangedEventManager.RemoveHandler(source, handler2, "propertyName2");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs(null));
        Assert.Equal(0, callCount1);
        Assert.Equal(3, callCount2);
        Assert.Equal(2, callCount3);
    }

    [Fact]
    public void RemoveHandler_InvokeNoSource_Success()
    {
        var target = new CustomWeakEventListener();
        EventHandler<PropertyChangedEventArgs> handler = (EventHandler<PropertyChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<PropertyChangedEventArgs>), target, nameof(CustomWeakEventListener.Handler));

        // Remove.
        PropertyChangedEventManager.RemoveHandler(null, handler, "propertyName");

        // Remove again.
        PropertyChangedEventManager.RemoveHandler(null, handler, "propertyName");
    }

    [Theory]
    [InlineData("propertyName")]
    [InlineData("")]
    [InlineData(null)]
    public void RemoveHandler_InvokeNoSuchSource_Nop(string? propertyName)
    {
        var source1 = new CustomNotifyPropertyChanged();
        var source2 = new CustomNotifyPropertyChanged();
        var target = new CustomWeakEventListener();
        int callCount = 0;
        target.HandlerAction += (s, e) => callCount++;
        EventHandler<PropertyChangedEventArgs> handler = (EventHandler<PropertyChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<PropertyChangedEventArgs>), target, nameof(CustomWeakEventListener.Handler));
        PropertyChangedEventManager.AddHandler(source1, handler, "propertyName");

        // Remove.
        PropertyChangedEventManager.RemoveHandler(source2, handler, propertyName);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs("propertyName"));

        // Remove again.
        PropertyChangedEventManager.RemoveHandler(source2, handler, propertyName);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs("propertyName"));
    }

    [Fact]
    public void RemoveHandler_InvokeNoSuchHandler_Nop()
    {
        var source = new CustomNotifyPropertyChanged();
        var target1 = new CustomWeakEventListener();
        int callCount1 = 0;
        target1.HandlerAction += (s, e) => callCount1++;
        EventHandler<PropertyChangedEventArgs> handler1 = (EventHandler<PropertyChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<PropertyChangedEventArgs>), target1, nameof(CustomWeakEventListener.Handler));
        var target2 = new CustomWeakEventListener();
        int callCount2 = 0;
        target2.HandlerAction += (s, e) => callCount2++;
        EventHandler<PropertyChangedEventArgs> handler2 = (EventHandler<PropertyChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<PropertyChangedEventArgs>), target2, nameof(CustomWeakEventListener.Handler));
        PropertyChangedEventManager.AddHandler(source, handler1, "propertyName");

        // Remove.
        PropertyChangedEventManager.RemoveHandler(source, handler2, "propertyName");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Remove again.
        PropertyChangedEventManager.RemoveHandler(source, handler2, "propertyName");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(2, callCount1);
    }

    [Theory]
    [InlineData("propertyName2")]
    [InlineData("")]
    public void RemoveHandler_InvokeNoSuchPropertyName_Nop(string propertyName)
    {
        var source = new CustomNotifyPropertyChanged();
        var target = new CustomWeakEventListener();
        int callCount = 0;
        target.HandlerAction += (s, e) => callCount++;
        EventHandler<PropertyChangedEventArgs> handler = (EventHandler<PropertyChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<PropertyChangedEventArgs>), target, nameof(CustomWeakEventListener.Handler));
        PropertyChangedEventManager.AddHandler(source, handler, "propertyName");

        // Remove.
        PropertyChangedEventManager.RemoveHandler(source, handler, propertyName);
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(1, callCount);

        // Remove again.
        PropertyChangedEventManager.RemoveHandler(source, handler, propertyName);
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void RemoveHandler_NullPropertyName_ThrowsArgumentNullException()
    {
        var source = new CustomNotifyPropertyChanged();
        var target = new CustomWeakEventListener();
        int callCount = 0;
        target.HandlerAction += (s, e) => callCount++;
        EventHandler<PropertyChangedEventArgs> handler = (EventHandler<PropertyChangedEventArgs>)Delegate.CreateDelegate(typeof(EventHandler<PropertyChangedEventArgs>), target, nameof(CustomWeakEventListener.Handler));
        PropertyChangedEventManager.AddHandler(source, handler, "propertyName");

        // Remove.
        // TODO: incorrect paramName.
        Assert.Throws<ArgumentNullException>("key", () => PropertyChangedEventManager.RemoveHandler(source, handler, null));
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(1, callCount);

        // Remove again.
        // TODO: incorrect paramName.
        Assert.Throws<ArgumentNullException>("key", () => PropertyChangedEventManager.RemoveHandler(source, handler, null));
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void RemoveHandler_NullHandler_ThrowsArgumentNullException()
    {
        var source = new CustomNotifyPropertyChanged();
        Assert.Throws<ArgumentNullException>("handler", () => PropertyChangedEventManager.RemoveHandler(source, null, "propertyName"));
    }

    [Theory]
    [InlineData("propertyName")]
    [InlineData("PROPERTYNAME")]
    public void RemoveListener_Invoke_Success(string propertyName)
    {
        var source = new CustomNotifyPropertyChanged();
        var listener = new CustomWeakEventListener();
        int callCount = 0;
        listener.ReceiveWeakEventAction += (t, s, e) =>
        {
            callCount++;
            return true;
        };
        PropertyChangedEventManager.AddListener(source, listener, "propertyName");

        // Remove.
        PropertyChangedEventManager.RemoveListener(source, listener, propertyName);
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(0, callCount);

        // Remove again.
        PropertyChangedEventManager.RemoveListener(source, listener, propertyName);
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void RemoveListener_InvokeMultipleNames_Success()
    {
        var source = new CustomNotifyPropertyChanged();
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
        PropertyChangedEventManager.AddListener(source, listener1, "propertyName1");
        PropertyChangedEventManager.AddListener(source, listener2, "propertyName2");
        PropertyChangedEventManager.AddListener(source, listener3, "");

        // Remove.
        PropertyChangedEventManager.RemoveListener(source, listener1, "propertyName1");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs(null));
        Assert.Equal(0, callCount1);
        Assert.Equal(1, callCount2);
        Assert.Equal(1, callCount3);

        // Remove again.
        PropertyChangedEventManager.RemoveListener(source, listener1, "propertyName1");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs(null));
        Assert.Equal(0, callCount1);
        Assert.Equal(2, callCount2);
        Assert.Equal(2, callCount3);

        // Remove another.
        PropertyChangedEventManager.RemoveListener(source, listener3, "");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs(null));
        Assert.Equal(0, callCount1);
        Assert.Equal(3, callCount2);
        Assert.Equal(2, callCount3);

        // Remove last.
        PropertyChangedEventManager.RemoveListener(source, listener2, "propertyName2");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs(null));
        Assert.Equal(0, callCount1);
        Assert.Equal(3, callCount2);
        Assert.Equal(2, callCount3);
    }

#if !DEBUG // This triggers a Debug.Assert.
    [Fact]
    public void RemoveListener_InvokeNoSource_Success()
    {
        var listener = new CustomWeakEventListener();

        // Remove.
        PropertyChangedEventManager.RemoveListener(null, listener, "propertyName");

        // Remove again.
        PropertyChangedEventManager.RemoveListener(null, listener, "propertyName");
    }

    [Theory]
    [InlineData("propertyName")]
    [InlineData("")]
    [InlineData(null)]
    public void RemoveListener_InvokeNoSuchSource_Nop(string? propertyName)
    {
        var source1 = new CustomNotifyPropertyChanged();
        var source2 = new CustomNotifyPropertyChanged();
        var listener = new CustomWeakEventListener();
        int callCount = 0;
        listener.ReceiveWeakEventAction += (t, s, e) =>
        {
            callCount++;
            return true;
        };
        PropertyChangedEventManager.AddListener(source1, listener, "propertyName");

        // Remove.
        PropertyChangedEventManager.RemoveListener(source2, listener, propertyName);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs("propertyName"));

        // Remove again.
        PropertyChangedEventManager.RemoveListener(source2, listener, propertyName);
        source1.OnPropertyChanged(source1, new PropertyChangedEventArgs("propertyName"));
    }
#endif

    [Fact]
    public void RemoveListener_InvokeNoSuchListener_Nop()
    {
        var source = new CustomNotifyPropertyChanged();
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
        PropertyChangedEventManager.AddListener(source, listener1, "propertyName");

        // Remove.
        PropertyChangedEventManager.RemoveListener(source, listener2, "propertyName");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(1, callCount1);
        Assert.Equal(0, callCount2);

        // Remove again.
        PropertyChangedEventManager.RemoveListener(source, listener2, "propertyName");
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(2, callCount1);
    }

    [Theory]
    [InlineData("propertyName2")]
    [InlineData("")]
    public void RemoveListener_InvokeNoSuchPropertyName_Nop(string propertyName)
    {
        var source = new CustomNotifyPropertyChanged();
        var listener = new CustomWeakEventListener();
        int callCount = 0;
        listener.ReceiveWeakEventAction += (t, s, e) =>
        {
            callCount++;
            return true;
        };
        PropertyChangedEventManager.AddListener(source, listener, "propertyName");

        // Remove.
        PropertyChangedEventManager.RemoveListener(source, listener, propertyName);
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(1, callCount);

        // Remove again.
        PropertyChangedEventManager.RemoveListener(source, listener, propertyName);
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(2, callCount);
    }

#if !DEBUG // This triggers a Debug.Assert.
    [Fact]
    public void RemoveListener_NullPropertyName_ThrowsArgumentNullException()
    {
        var source = new CustomNotifyPropertyChanged();
        var listener = new CustomWeakEventListener();
        int callCount = 0;
        listener.ReceiveWeakEventAction += (t, s, e) =>
        {
            callCount++;
            return true;
        };
        PropertyChangedEventManager.AddListener(source, listener, "propertyName");

        // Remove.
        // TODO: incorrect paramName.
        Assert.Throws<ArgumentNullException>("key", () => PropertyChangedEventManager.RemoveListener(source, listener, null));
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(1, callCount);

        // Remove again.
        // TODO: incorrect paramName.
        Assert.Throws<ArgumentNullException>("key", () => PropertyChangedEventManager.RemoveListener(source, listener, null));
        source.OnPropertyChanged(source, new PropertyChangedEventArgs("propertyName"));
        Assert.Equal(2, callCount);
    }
#endif

    [Fact]
    public void RemoveListener_NullListener_ThrowsArgumentNullException()
    {
        var source = new CustomNotifyPropertyChanged();
        Assert.Throws<ArgumentNullException>("listener", () => PropertyChangedEventManager.RemoveListener(source, null, "propertyName"));
    }

    private class CustomNotifyPropertyChanged : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged(object sender, PropertyChangedEventArgs e) => PropertyChanged?.Invoke(sender, e);
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

        public Action<object, PropertyChangedEventArgs>? HandlerAction { get; set; }

        public void Handler(object sender, PropertyChangedEventArgs e)
        {
            HandlerAction?.Invoke(sender, e);
        }
    }
}
