// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Specialized;
using System.Linq;

namespace System.ComponentModel.Tests;

public class SortDescriptionCollectionTests
{
    [Fact]
    public void Ctor_Default()
    {
        var collection = new SortDescriptionCollection();
        Assert.Empty(collection);
        Assert.False(((IList)collection).IsFixedSize);
        Assert.False(((IList)collection).IsReadOnly);
        Assert.False(((IList)collection).IsSynchronized);
    }

    [Fact]
    public void CollectionChanged_INotifyCollectionChangedAddRemove_Success()
    {
        INotifyCollectionChanged collection = new SortDescriptionCollection();

        int callCount = 0;
        NotifyCollectionChangedEventHandler handler = (s, e) => callCount++;
        ((INotifyCollectionChanged)collection).CollectionChanged += handler;
        Assert.Equal(0, callCount);

        ((INotifyCollectionChanged)collection).CollectionChanged -= handler;
        Assert.Equal(0, callCount);

        // Remove non existent.
        ((INotifyCollectionChanged)collection).CollectionChanged -= handler;
        Assert.Equal(0, callCount);

        // Add null.
        ((INotifyCollectionChanged)collection).CollectionChanged += null;
        Assert.Equal(0, callCount);

        // Remove null.
        ((INotifyCollectionChanged)collection).CollectionChanged -= null;
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void Item_Set_GetReturnsExpected()
    {
        var collection = new SortDescriptionCollection();
        var description1 = new SortDescription("Name1", ListSortDirection.Ascending);
        var description2 = new SortDescription("Name2", ListSortDirection.Descending);

        collection.Add(description1);

        // Set.
        collection[0] = description2;
        Assert.Single(collection);
        Assert.Equal(description2, collection[0]);
        Assert.True(collection[0].IsSealed);
    }

    [Fact]
    public void Item_SetWithHandler_GetReturnsExpected()
    {
        var collection = new SortDescriptionCollection();
        var description1 = new SortDescription("Name1", ListSortDirection.Ascending);
        var description2 = new SortDescription("Name2", ListSortDirection.Descending);
        var description3 = new SortDescription("Name3", ListSortDirection.Descending);

        collection.Add(description1);

        var events = new List<NotifyCollectionChangedEventArgs>(); 
        int callCount = 0;
        NotifyCollectionChangedEventHandler handler = (s, e) =>
        {
            Assert.Same(collection, s);
            events.Add(e);
            callCount++;
        };
        ((INotifyCollectionChanged)collection).CollectionChanged += handler;

        // Set.
        collection[0] = description2;
        Assert.Single(collection);
        Assert.Equal(description2, collection[0]);
        Assert.True(collection[0].IsSealed);
        Assert.Equal(2, callCount);
        Assert.Equal(NotifyCollectionChangedAction.Remove, events[0].Action);
        Assert.Null(events[0].NewItems);
        Assert.Equal(new[] { description1 }, events[0].OldItems!.Cast<SortDescription>());
        Assert.Equal(-1, events[0].NewStartingIndex);
        Assert.Equal(0, events[0].OldStartingIndex);
        Assert.Equal(NotifyCollectionChangedAction.Add, events[1].Action);
        Assert.Equal(new[] { description2 }, events[1].NewItems!.Cast<SortDescription>());
        Assert.Equal(0, events[1].NewStartingIndex);
        Assert.Equal(-1, events[1].OldStartingIndex);

        // Remove handler.
        ((INotifyCollectionChanged)collection).CollectionChanged -= handler;
        collection[0] = description3;
        Assert.Single(collection);
        Assert.Equal(description3, collection[0]);
        Assert.True(collection[0].IsSealed);
        Assert.Equal(2, callCount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void Item_GetInvalidIndex_ThrowsArgumentOutOfRangeException(int index)
    {
        var collection = new SortDescriptionCollection();
        Assert.Throws<ArgumentOutOfRangeException>("index", () => collection[index]);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void Item_SetInvalidIndex_ThrowsArgumentOutOfRangeException(int index)
    {
        var collection = new SortDescriptionCollection();
        var description = new SortDescription("Name", ListSortDirection.Ascending);
        Assert.Throws<ArgumentOutOfRangeException>("index", () => collection[index] = description);
    }

    [Fact]
    public void Add_Invoke_Success()
    {
        var collection = new SortDescriptionCollection();
        var description1 = new SortDescription("Name1", ListSortDirection.Ascending);
        var description2 = new SortDescription("Name2", ListSortDirection.Descending);

        // Add.
        collection.Add(description1);
        Assert.Single(collection);
        Assert.Equal(description1, collection[0]);
        Assert.True(collection[0].IsSealed);

        // Add again.
        collection.Add(description2);
        Assert.Equal(2, collection.Count);
        Assert.Equal(description1, collection[0]);
        Assert.True(collection[0].IsSealed);
        Assert.Equal(description2, collection[1]);
        Assert.True(collection[1].IsSealed);
    }

    [Fact]
    public void Add_InvokeWithHandler_CallsCollectionChanged()
    {
        var collection = new SortDescriptionCollection();
        var description1 = new SortDescription("Name1", ListSortDirection.Ascending);
        var description2 = new SortDescription("Name2", ListSortDirection.Descending);
        var description3 = new SortDescription("Name3", ListSortDirection.Descending);

        var events = new List<NotifyCollectionChangedEventArgs>(); 
        int callCount = 0;
        NotifyCollectionChangedEventHandler handler = (s, e) =>
        {
            Assert.Same(collection, s);
            events.Add(e);
            callCount++;
        };
        ((INotifyCollectionChanged)collection).CollectionChanged += handler;

        // Add.
        collection.Add(description1);
        Assert.Single(collection);
        Assert.Equal(description1, collection[0]);
        Assert.True(collection[0].IsSealed);
        Assert.Equal(1, callCount);
        Assert.Equal(NotifyCollectionChangedAction.Add, events[0].Action);
        Assert.Equal(new[] { description1 }, events[0].NewItems!.Cast<SortDescription>());
        Assert.Null(events[0].OldItems);
        Assert.Equal(0, events[0].NewStartingIndex);
        Assert.Equal(-1, events[0].OldStartingIndex);

        // Add again.
        collection.Add(description2);
        Assert.Equal(2, collection.Count);
        Assert.Equal(description1, collection[0]);
        Assert.True(collection[0].IsSealed);
        Assert.Equal(description2, collection[1]);
        Assert.True(collection[1].IsSealed);
        Assert.Equal(2, callCount);
        Assert.Equal(NotifyCollectionChangedAction.Add, events[1].Action);
        Assert.Equal(new[] { description1 }, events[0].NewItems!.Cast<SortDescription>());
        Assert.Null(events[1].OldItems);
        Assert.Equal(1, events[1].NewStartingIndex);
        Assert.Equal(-1, events[1].OldStartingIndex);

        // Remove handler.
        ((INotifyCollectionChanged)collection).CollectionChanged -= handler;
        collection.Add(description3);
        Assert.Equal(3, collection.Count);
        Assert.Equal(description1, collection[0]);
        Assert.True(collection[0].IsSealed);
        Assert.Equal(description2, collection[1]);
        Assert.True(collection[1].IsSealed);
        Assert.Equal(description3, collection[2]);
        Assert.True(collection[2].IsSealed);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void Insert_Invoke_Success()
    {
        var collection = new SortDescriptionCollection();
        var description1 = new SortDescription("Name1", ListSortDirection.Ascending);
        var description2 = new SortDescription("Name2", ListSortDirection.Descending);

        // Add.
        collection.Insert(0, description1);
        Assert.Single(collection);
        Assert.Equal(description1, collection[0]);
        Assert.True(collection[0].IsSealed);

        // Add again.
        collection.Insert(0, description2);
        Assert.Equal(2, collection.Count);
        Assert.Equal(description2, collection[0]);
        Assert.True(collection[0].IsSealed);
        Assert.Equal(description1, collection[1]);
        Assert.True(collection[1].IsSealed);
    }

    [Fact]
    public void Insert_InvokeWithHandler_CallsCollectionChanged()
    {
        var collection = new SortDescriptionCollection();
        var description1 = new SortDescription("Name1", ListSortDirection.Ascending);
        var description2 = new SortDescription("Name2", ListSortDirection.Descending);
        var description3 = new SortDescription("Name3", ListSortDirection.Descending);

        var events = new List<NotifyCollectionChangedEventArgs>(); 
        int callCount = 0;
        NotifyCollectionChangedEventHandler handler = (s, e) =>
        {
            Assert.Same(collection, s);
            events.Add(e);
            callCount++;
        };
        ((INotifyCollectionChanged)collection).CollectionChanged += handler;

        // Add.
        collection.Insert(0, description1);
        Assert.Single(collection);
        Assert.Equal(description1, collection[0]);
        Assert.True(collection[0].IsSealed);
        Assert.Equal(1, callCount);
        Assert.Equal(NotifyCollectionChangedAction.Add, events[0].Action);
        Assert.Equal(new[] { description1 }, events[0].NewItems!.Cast<SortDescription>());
        Assert.Null(events[0].OldItems);
        Assert.Equal(0, events[0].NewStartingIndex);
        Assert.Equal(-1, events[0].OldStartingIndex);

        // Add again.
        collection.Insert(0, description2);
        Assert.Equal(2, collection.Count);
        Assert.Equal(description2, collection[0]);
        Assert.True(collection[0].IsSealed);
        Assert.Equal(description1, collection[1]);
        Assert.True(collection[1].IsSealed);
        Assert.Equal(2, callCount);
        Assert.Equal(NotifyCollectionChangedAction.Add, events[1].Action);
        Assert.Equal(new[] { description2 }, events[1].NewItems!.Cast<SortDescription>());
        Assert.Null(events[1].OldItems);
        Assert.Equal(0, events[1].NewStartingIndex);
        Assert.Equal(-1, events[1].OldStartingIndex);

        // Remove handler.
        ((INotifyCollectionChanged)collection).CollectionChanged -= handler;
        collection.Insert(2, description3);
        Assert.Equal(3, collection.Count);
        Assert.Equal(description2, collection[0]);
        Assert.True(collection[0].IsSealed);
        Assert.Equal(description1, collection[1]);
        Assert.True(collection[1].IsSealed);
        Assert.Equal(description3, collection[2]);
        Assert.True(collection[2].IsSealed);
        Assert.Equal(2, callCount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void Insert_InvalidIndex_ThrowsArgumentOutOfRangeException(int index)
    {
        var collection = new SortDescriptionCollection();
        var description = new SortDescription("Name", ListSortDirection.Ascending);
        Assert.Throws<ArgumentOutOfRangeException>("index", () => collection.Insert(index, description));
    }

    [Fact]
    public void InsertItem_Invoke_Success()
    {
        var collection = new SubSortDescriptionCollection();
        var description1 = new SortDescription("Name1", ListSortDirection.Ascending);
        var description2 = new SortDescription("Name2", ListSortDirection.Descending);

        // Add.
        collection.InsertItem(0, description1);
        Assert.Single(collection);
        Assert.Equal(description1, collection[0]);
        Assert.True(collection[0].IsSealed);

        // Add again.
        collection.InsertItem(0, description2);
        Assert.Equal(2, collection.Count);
        Assert.Equal(description2, collection[0]);
        Assert.True(collection[0].IsSealed);
        Assert.Equal(description1, collection[1]);
        Assert.True(collection[1].IsSealed);
    }

    [Fact]
    public void InsertItem_InvokeWithHandler_CallsCollectionChanged()
    {
        var collection = new SubSortDescriptionCollection();
        var description1 = new SortDescription("Name1", ListSortDirection.Ascending);
        var description2 = new SortDescription("Name2", ListSortDirection.Descending);
        var description3 = new SortDescription("Name3", ListSortDirection.Descending);

        var events = new List<NotifyCollectionChangedEventArgs>(); 
        int callCount = 0;
        NotifyCollectionChangedEventHandler handler = (s, e) =>
        {
            Assert.Same(collection, s);
            events.Add(e);
            callCount++;
        };
        ((INotifyCollectionChanged)collection).CollectionChanged += handler;

        // Add.
        collection.InsertItem(0, description1);
        Assert.Single(collection);
        Assert.Equal(description1, collection[0]);
        Assert.True(collection[0].IsSealed);
        Assert.Equal(1, callCount);
        Assert.Equal(NotifyCollectionChangedAction.Add, events[0].Action);
        Assert.Equal(new[] { description1 }, events[0].NewItems!.Cast<SortDescription>());
        Assert.Null(events[0].OldItems);
        Assert.Equal(0, events[0].NewStartingIndex);
        Assert.Equal(-1, events[0].OldStartingIndex);

        // Add again.
        collection.InsertItem(0, description2);
        Assert.Equal(2, collection.Count);
        Assert.Equal(description2, collection[0]);
        Assert.True(collection[0].IsSealed);
        Assert.Equal(description1, collection[1]);
        Assert.True(collection[1].IsSealed);
        Assert.Equal(2, callCount);
        Assert.Equal(NotifyCollectionChangedAction.Add, events[1].Action);
        Assert.Equal(new[] { description2 }, events[1].NewItems!.Cast<SortDescription>());
        Assert.Null(events[1].OldItems);
        Assert.Equal(0, events[1].NewStartingIndex);
        Assert.Equal(-1, events[1].OldStartingIndex);

        // Remove handler.
        ((INotifyCollectionChanged)collection).CollectionChanged -= handler;
        collection.InsertItem(2, description3);
        Assert.Equal(3, collection.Count);
        Assert.Equal(description2, collection[0]);
        Assert.True(collection[0].IsSealed);
        Assert.Equal(description1, collection[1]);
        Assert.True(collection[1].IsSealed);
        Assert.Equal(description3, collection[2]);
        Assert.True(collection[2].IsSealed);
        Assert.Equal(2, callCount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void InsertItem_InvalidIndex_ThrowsArgumentOutOfRangeException(int index)
    {
        var collection = new SubSortDescriptionCollection();
        var description = new SortDescription("Name", ListSortDirection.Ascending);
        Assert.Throws<ArgumentOutOfRangeException>("index", () => collection.InsertItem(index, description));
    }

    [Fact]
    public void Clear_Invoke_Success()
    {
        var collection = new SortDescriptionCollection();

        // Clear empty.
        collection.Clear();
        Assert.Empty(collection);

        collection.Add(new SortDescription("Name", ListSortDirection.Ascending));

        // Clear.
        collection.Clear();
        Assert.Empty(collection);

        // Clear again.
        collection.Clear();
    }

    [Fact]
    public void Clear_InvokeWithHandler_CallsCollectionChanged()
    {
        var collection = new SortDescriptionCollection();
        var events = new List<NotifyCollectionChangedEventArgs>(); 
        int callCount = 0;
        NotifyCollectionChangedEventHandler handler = (s, e) =>
        {
            Assert.Same(collection, s);
            events.Add(e);
            callCount++;
        };
        ((INotifyCollectionChanged)collection).CollectionChanged += handler;

        // Clear empty.
        collection.Clear();
        Assert.Empty(collection);
        Assert.Equal(1, callCount);
        Assert.Equal(NotifyCollectionChangedAction.Reset, events[0].Action);
        Assert.Null(events[0].NewItems);
        Assert.Null(events[0].OldItems);
        Assert.Equal(-1, events[0].NewStartingIndex);
        Assert.Equal(-1, events[0].OldStartingIndex);

        collection.Add(new SortDescription("Name", ListSortDirection.Ascending));
        Assert.Equal(2, callCount);

        // Clear.
        collection.Clear();
        Assert.Empty(collection);
        Assert.Equal(3, callCount);
        Assert.Equal(NotifyCollectionChangedAction.Reset, events[2].Action);
        Assert.Null(events[2].NewItems);
        Assert.Null(events[2].OldItems);
        Assert.Equal(-1, events[2].NewStartingIndex);
        Assert.Equal(-1, events[2].OldStartingIndex);

        // Clear again.
        collection.Clear();
        Assert.Empty(collection);
        Assert.Equal(4, callCount);
        Assert.Equal(NotifyCollectionChangedAction.Reset, events[3].Action);
        Assert.Null(events[3].NewItems);
        Assert.Null(events[3].OldItems);
        Assert.Equal(-1, events[3].NewStartingIndex);
        Assert.Equal(-1, events[3].OldStartingIndex);

        // Remove handler.
        ((INotifyCollectionChanged)collection).CollectionChanged -= handler;
        collection.Clear();
        Assert.Equal(4, callCount);
    }

    [Fact]
    public void ClearItems_Invoke_Success()
    {
        var collection = new SubSortDescriptionCollection();

        // Clear empty.
        collection.ClearItems();
        Assert.Empty(collection);

        collection.Add(new SortDescription("Name", ListSortDirection.Ascending));

        // Clear.
        collection.ClearItems();
        Assert.Empty(collection);

        // Clear again.
        collection.ClearItems();
    }

    [Fact]
    public void ClearItems_InvokeWithHandler_CallsCollectionChanged()
    {
        var collection = new SubSortDescriptionCollection();
        var events = new List<NotifyCollectionChangedEventArgs>(); 
        int callCount = 0;
        NotifyCollectionChangedEventHandler handler = (s, e) =>
        {
            Assert.Same(collection, s);
            events.Add(e);
            callCount++;
        };
        ((INotifyCollectionChanged)collection).CollectionChanged += handler;

        // Clear empty.
        collection.ClearItems();
        Assert.Empty(collection);
        Assert.Equal(1, callCount);
        Assert.Equal(NotifyCollectionChangedAction.Reset, events[0].Action);
        Assert.Null(events[0].NewItems);
        Assert.Null(events[0].OldItems);
        Assert.Equal(-1, events[0].NewStartingIndex);
        Assert.Equal(-1, events[0].OldStartingIndex);

        collection.Add(new SortDescription("Name", ListSortDirection.Ascending));
        Assert.Equal(2, callCount);

        // Clear.
        collection.ClearItems();
        Assert.Empty(collection);
        Assert.Equal(3, callCount);
        Assert.Equal(NotifyCollectionChangedAction.Reset, events[2].Action);
        Assert.Null(events[2].NewItems);
        Assert.Null(events[2].OldItems);
        Assert.Equal(-1, events[2].NewStartingIndex);
        Assert.Equal(-1, events[2].OldStartingIndex);

        // Clear again.
        collection.ClearItems();
        Assert.Empty(collection);
        Assert.Equal(4, callCount);
        Assert.Equal(NotifyCollectionChangedAction.Reset, events[3].Action);
        Assert.Null(events[3].NewItems);
        Assert.Null(events[3].OldItems);
        Assert.Equal(-1, events[3].NewStartingIndex);
        Assert.Equal(-1, events[3].OldStartingIndex);

        // Remove handler.
        ((INotifyCollectionChanged)collection).CollectionChanged -= handler;
        collection.ClearItems();
        Assert.Equal(4, callCount);
    }

    [Fact]
    public void RemoveAt_Invoke_Success()
    {
        var collection = new SubSortDescriptionCollection();
        var description1 = new SortDescription("Name1", ListSortDirection.Ascending);
        var description2 = new SortDescription("Name2", ListSortDirection.Descending);

        collection.Add(description1);
        collection.Add(description2);

        // Remove last.
        collection.RemoveAt(1);
        Assert.Single(collection);
        Assert.Equal(description1, collection[0]);
        Assert.True(collection[0].IsSealed);

        // Remove first.
        collection.RemoveAt(0);
        Assert.Empty(collection);
    }

    [Fact]
    public void RemoveAt_InvokeWithHandler_CallsCollectionChanged()
    {
        var collection = new SubSortDescriptionCollection();
        var description1 = new SortDescription("Name1", ListSortDirection.Ascending);
        var description2 = new SortDescription("Name2", ListSortDirection.Descending);
        var description3 = new SortDescription("Name3", ListSortDirection.Descending);

        collection.Add(description1);
        collection.Add(description2);
        collection.Add(description3);

        var events = new List<NotifyCollectionChangedEventArgs>(); 
        int callCount = 0;
        NotifyCollectionChangedEventHandler handler = (s, e) =>
        {
            Assert.Same(collection, s);
            events.Add(e);
            callCount++;
        };
        ((INotifyCollectionChanged)collection).CollectionChanged += handler;

        // Remove middle.
        collection.RemoveAt(1);
        Assert.Equal(2, collection.Count);
        Assert.Equal(description1, collection[0]);
        Assert.True(collection[0].IsSealed);
        Assert.Equal(description3, collection[1]);
        Assert.True(collection[1].IsSealed);
        Assert.Equal(1, callCount);
        Assert.Equal(NotifyCollectionChangedAction.Remove, events[0].Action);
        Assert.Null(events[0].NewItems);
        Assert.Equal(new[] { description2 }, events[0].OldItems!.Cast<SortDescription>());
        Assert.Equal(-1, events[0].NewStartingIndex);
        Assert.Equal(1, events[0].OldStartingIndex);

        // Remove last.
        collection.RemoveAt(1);
        Assert.Single(collection);
        Assert.Equal(description1, collection[0]);
        Assert.True(collection[0].IsSealed);
        Assert.Equal(2, callCount);
        Assert.Equal(NotifyCollectionChangedAction.Remove, events[1].Action);
        Assert.Null(events[1].NewItems);
        Assert.Equal(new[] { description3 }, events[1].OldItems!.Cast<SortDescription>());
        Assert.Equal(-1, events[1].NewStartingIndex);
        Assert.Equal(1, events[1].OldStartingIndex);

        // Remove handler.
        ((INotifyCollectionChanged)collection).CollectionChanged -= handler;
        collection.RemoveAt(0);
        Assert.Empty(collection);
        Assert.Equal(2, callCount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void RemoveAt_InvalidIndex_ThrowsArgumentOutOfRangeException(int index)
    {
        var collection = new SubSortDescriptionCollection();
        Assert.Throws<ArgumentOutOfRangeException>("index", () => collection.RemoveAt(index));
    }

    [Fact]
    public void RemoveItem_Invoke_Success()
    {
        var collection = new SubSortDescriptionCollection();
        var description1 = new SortDescription("Name1", ListSortDirection.Ascending);
        var description2 = new SortDescription("Name2", ListSortDirection.Descending);

        collection.Add(description1);
        collection.Add(description2);

        // Remove last.
        collection.RemoveItem(1);
        Assert.Single(collection);
        Assert.Equal(description1, collection[0]);
        Assert.True(collection[0].IsSealed);

        // Remove first.
        collection.RemoveItem(0);
        Assert.Empty(collection);
    }

    [Fact]
    public void RemoveItem_InvokeWithHandler_CallsCollectionChanged()
    {
        var collection = new SubSortDescriptionCollection();
        var description1 = new SortDescription("Name1", ListSortDirection.Ascending);
        var description2 = new SortDescription("Name2", ListSortDirection.Descending);
        var description3 = new SortDescription("Name3", ListSortDirection.Descending);

        collection.Add(description1);
        collection.Add(description2);
        collection.Add(description3);

        var events = new List<NotifyCollectionChangedEventArgs>(); 
        int callCount = 0;
        NotifyCollectionChangedEventHandler handler = (s, e) =>
        {
            Assert.Same(collection, s);
            events.Add(e);
            callCount++;
        };
        ((INotifyCollectionChanged)collection).CollectionChanged += handler;

        // Remove middle.
        collection.RemoveItem(1);
        Assert.Equal(2, collection.Count);
        Assert.Equal(description1, collection[0]);
        Assert.True(collection[0].IsSealed);
        Assert.Equal(description3, collection[1]);
        Assert.True(collection[1].IsSealed);
        Assert.Equal(1, callCount);
        Assert.Equal(NotifyCollectionChangedAction.Remove, events[0].Action);
        Assert.Null(events[0].NewItems);
        Assert.Equal(new[] { description2 }, events[0].OldItems!.Cast<SortDescription>());
        Assert.Equal(-1, events[0].NewStartingIndex);
        Assert.Equal(1, events[0].OldStartingIndex);

        // Remove last.
        collection.RemoveItem(1);
        Assert.Single(collection);
        Assert.Equal(description1, collection[0]);
        Assert.True(collection[0].IsSealed);
        Assert.Equal(2, callCount);
        Assert.Equal(NotifyCollectionChangedAction.Remove, events[1].Action);
        Assert.Null(events[1].NewItems);
        Assert.Equal(new[] { description3 }, events[1].OldItems!.Cast<SortDescription>());
        Assert.Equal(-1, events[1].NewStartingIndex);
        Assert.Equal(1, events[1].OldStartingIndex);

        // Remove handler.
        ((INotifyCollectionChanged)collection).CollectionChanged -= handler;
        collection.RemoveItem(0);
        Assert.Empty(collection);
        Assert.Equal(2, callCount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void RemoveItem_InvalidIndex_ThrowsArgumentOutOfRangeException(int index)
    {
        var collection = new SubSortDescriptionCollection();
        Assert.Throws<ArgumentOutOfRangeException>("index", () => collection.RemoveItem(index));
    }


    [Fact]
    public void SetItem_Invoke_GetReturnsExpected()
    {
        var collection = new SubSortDescriptionCollection();
        var description1 = new SortDescription("Name1", ListSortDirection.Ascending);
        var description2 = new SortDescription("Name2", ListSortDirection.Descending);

        collection.Add(description1);

        // Set.
        collection.SetItem(0, description2);
        Assert.Single(collection);
        Assert.Equal(description2, collection[0]);
        Assert.True(collection[0].IsSealed);
    }

    [Fact]
    public void SetItem_WithHandler_GetReturnsExpected()
    {
        var collection = new SubSortDescriptionCollection();
        var description1 = new SortDescription("Name1", ListSortDirection.Ascending);
        var description2 = new SortDescription("Name2", ListSortDirection.Descending);
        var description3 = new SortDescription("Name3", ListSortDirection.Descending);

        collection.Add(description1);

        var events = new List<NotifyCollectionChangedEventArgs>(); 
        int callCount = 0;
        NotifyCollectionChangedEventHandler handler = (s, e) =>
        {
            Assert.Same(collection, s);
            events.Add(e);
            callCount++;
        };
        ((INotifyCollectionChanged)collection).CollectionChanged += handler;

        // Set.
        collection.SetItem(0, description2);
        Assert.Single(collection);
        Assert.Equal(description2, collection[0]);
        Assert.True(collection[0].IsSealed);
        Assert.Equal(2, callCount);
        Assert.Equal(NotifyCollectionChangedAction.Remove, events[0].Action);
        Assert.Null(events[0].NewItems);
        Assert.Equal(new[] { description1 }, events[0].OldItems!.Cast<SortDescription>());
        Assert.Equal(-1, events[0].NewStartingIndex);
        Assert.Equal(0, events[0].OldStartingIndex);
        Assert.Equal(NotifyCollectionChangedAction.Add, events[1].Action);
        Assert.Equal(new[] { description2 }, events[1].NewItems!.Cast<SortDescription>());
        Assert.Equal(0, events[1].NewStartingIndex);
        Assert.Equal(-1, events[1].OldStartingIndex);

        // Remove handler.
        ((INotifyCollectionChanged)collection).CollectionChanged -= handler;
        collection.SetItem(0, description3);
        Assert.Single(collection);
        Assert.Equal(description3, collection[0]);
        Assert.True(collection[0].IsSealed);
        Assert.Equal(2, callCount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void SetItem_InvalidIndex_ThrowsArgumentOutOfRangeException(int index)
    {
        var collection = new SubSortDescriptionCollection();
        var description = new SortDescription("Name", ListSortDirection.Ascending);
        Assert.Throws<ArgumentOutOfRangeException>("index", () => collection.SetItem(index, description));
    }

    [Fact]
    public void Empty_Get_ReturnsExpected()
    {
        SortDescriptionCollection collection = SortDescriptionCollection.Empty;
        Assert.Empty(collection);
        Assert.Same(SortDescriptionCollection.Empty, collection);
        Assert.True(((IList)collection).IsFixedSize);
        Assert.True(((IList)collection).IsReadOnly);
        Assert.False(((IList)collection).IsSynchronized);
    }

    [Fact]
    public void Empty_Modify_ThrowsNotSupportedException()
    {
        SortDescriptionCollection collection = SortDescriptionCollection.Empty;
        Assert.Throws<NotSupportedException>(() => collection.Add(new SortDescription("Name", ListSortDirection.Ascending)));
        Assert.Throws<NotSupportedException>(() => collection.Insert(0, new SortDescription("Name", ListSortDirection.Ascending)));
        Assert.Throws<NotSupportedException>(() => collection.Clear());
    }

    private class SubSortDescriptionCollection : SortDescriptionCollection
    {
        public new void ClearItems() => base.ClearItems();

        public new void InsertItem(int index, SortDescription item) => base.InsertItem(index, item);

        public new void RemoveItem(int index) => base.RemoveItem(index);

        public new void SetItem(int index, SortDescription item) => base.SetItem(index, item);
    }
}