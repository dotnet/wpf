// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;

namespace System.Windows.Data.Tests;

public class DataSourceProviderTests
{
    [Fact]
    public void Ctor_Default()
    {
        var provider = new SubDataSourceProvider();
        Assert.Null(provider.Data);
        Assert.NotNull(provider.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, provider.Dispatcher);
        Assert.Null(provider.Error);
        Assert.True(provider.IsInitialLoadEnabled);
        Assert.False(provider.IsRefreshDeferred);
    }

    [Fact]
    public void DataChanged_AddRemove_Success()
    {
        var provider = new SubDataSourceProvider();

        int callCount = 0;
        EventHandler handler = (s, e) => callCount++;
        provider.DataChanged += handler;
        Assert.Equal(0, callCount);

        provider.DataChanged -= handler;
        Assert.Equal(0, callCount);

        // Remove non existent.
        provider.DataChanged -= handler;
        Assert.Equal(0, callCount);

        // Add null.
        provider.DataChanged += null;
        Assert.Equal(0, callCount);

        // Remove null.
        provider.DataChanged -= null;
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void PropertyChanged_INotifyPropertyChangedAddRemove_Success()
    {
        var provider = new SubDataSourceProvider();

        int callCount = 0;
        PropertyChangedEventHandler handler = (s, e) => callCount++;
        ((INotifyPropertyChanged)provider).PropertyChanged += handler;
        Assert.Equal(0, callCount);

        ((INotifyPropertyChanged)provider).PropertyChanged -= handler;
        Assert.Equal(0, callCount);

        // Remove non existent.
        ((INotifyPropertyChanged)provider).PropertyChanged -= handler;
        Assert.Equal(0, callCount);

        // Add null.
        ((INotifyPropertyChanged)provider).PropertyChanged += null;
        Assert.Equal(0, callCount);

        // Remove null.
        ((INotifyPropertyChanged)provider).PropertyChanged -= null;
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void PropertyChanged_AddRemove_Success()
    {
        var provider = new SubDataSourceProvider();

        int callCount = 0;
        PropertyChangedEventHandler handler = (s, e) => callCount++;
        provider.PropertyChanged += handler;
        Assert.Equal(0, callCount);

        provider.PropertyChanged -= handler;
        Assert.Equal(0, callCount);

        // Remove non existent.
        provider.PropertyChanged -= handler;
        Assert.Equal(0, callCount);

        // Add null.
        provider.PropertyChanged += null;
        Assert.Equal(0, callCount);

        // Remove null.
        provider.PropertyChanged -= null;
        Assert.Equal(0, callCount);
    }

    public static IEnumerable<object?[]> Dispatcher_Set_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { Dispatcher.CurrentDispatcher };
    }
    
    [Theory]
    [MemberData(nameof(Dispatcher_Set_TestData))]
    public void Dispatcher_Set_GetReturnsExpected(Dispatcher value)
    {
        var provider = new SubDataSourceProvider
        {
            // Set.
            Dispatcher = value
        };
        Assert.Same(value, provider.Dispatcher);
        
        // Set same.
        provider.Dispatcher = value;
        Assert.Same(value, provider.Dispatcher);
    }

    [Fact]
    public void Dispatcher_SetWithHandler_DoesNotCallPropertyChanged()
    {
        var provider = new SubDataSourceProvider();
        int callCount = 0;
        PropertyChangedEventHandler handler = (sender, e) => callCount++;
        ((INotifyPropertyChanged)provider).PropertyChanged += handler;

        // Set.
        provider.Dispatcher = null;
        Assert.Null(provider.Dispatcher);
        Assert.Equal(0, callCount);

        // Set same.
        provider.Dispatcher = null;
        Assert.Null(provider.Dispatcher);
        Assert.Equal(0, callCount);

        // Set different.
        provider.Dispatcher = Dispatcher.CurrentDispatcher;
        Assert.Same(Dispatcher.CurrentDispatcher, provider.Dispatcher);
        Assert.Equal(0, callCount);

        // Remove handler.
        ((INotifyPropertyChanged)provider).PropertyChanged -= handler;
        provider.Dispatcher = null;
        Assert.Null(provider.Dispatcher);
        Assert.Equal(0, callCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsInitialLoadEnabled_Set_GetReturnsExpected(bool value)
    {
        var provider = new SubDataSourceProvider
        {
            // Set.
            IsInitialLoadEnabled = value
        };
        Assert.Equal(value, provider.IsInitialLoadEnabled);

        // Set same.
        provider.IsInitialLoadEnabled = value;
        Assert.Equal(value, provider.IsInitialLoadEnabled);

        // Set different.
        provider.IsInitialLoadEnabled = !value;
        Assert.Equal(!value, provider.IsInitialLoadEnabled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsInitialLoadEnabled_SetAlreadyLoaded_GetReturnsExpected(bool value)
    {
        var provider = new SubDataSourceProvider();
        provider.InitialLoad();

        // Set.
        provider.IsInitialLoadEnabled = value;
        Assert.Equal(value, provider.IsInitialLoadEnabled);

        // Set same.
        provider.IsInitialLoadEnabled = value;
        Assert.Equal(value, provider.IsInitialLoadEnabled);

        // Set different.
        provider.IsInitialLoadEnabled = !value;
        Assert.Equal(!value, provider.IsInitialLoadEnabled);
    }

    [Fact]
    public void IsInitialLoadEnabled_SetWithHandler_CallsPropertyChanged()
    {
        var provider = new SubDataSourceProvider();
        int callCount = 0;
        PropertyChangedEventHandler handler = (sender, e) =>
        {
            Assert.Same(provider, sender);
            Assert.Equal("IsInitialLoadEnabled", e.PropertyName);
            callCount++;
        };
        ((INotifyPropertyChanged)provider).PropertyChanged += handler;

        // Set.
        provider.IsInitialLoadEnabled = false;
        Assert.False(provider.IsInitialLoadEnabled);
        Assert.Equal(1, callCount);

        // Set same.
        provider.IsInitialLoadEnabled = false;
        Assert.False(provider.IsInitialLoadEnabled);
        Assert.Equal(2, callCount);

        // Set different.
        provider.IsInitialLoadEnabled = true;
        Assert.True(provider.IsInitialLoadEnabled);
        Assert.Equal(3, callCount);

        // Remove handler.
        ((INotifyPropertyChanged)provider).PropertyChanged -= handler;
        provider.IsInitialLoadEnabled = false;
        Assert.False(provider.IsInitialLoadEnabled);
        Assert.Equal(3, callCount);
    }

    [Fact]
    public void BeginInit_Invoke_EndCallsBeginQuery()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        // Begin.
        provider.BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);
        
        // End.
        provider.EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void BeginInit_InvokeInitialLoadDisabled_EndCallsBeginQuery()
    {
        var provider = new CustomBeginQueryDataSourceProvider
        {
            IsInitialLoadEnabled = false
        };
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        // Begin.
        provider.BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);
        
        // End.
        provider.EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void BeginInit_InvokeInitialLoaded_EndCallsBeginQuery()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        provider.InitialLoad();
        Assert.Equal(1, callCount);

        // Begin.
        provider.BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
        
        // End.
        provider.EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void BeginInit_InvokeRefreshed_EndCallsBeginQuery()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        provider.Refresh();
        Assert.Equal(1, callCount);

        // Begin.
        provider.BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
        
        // End.
        provider.EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void BeginInit_InvokeRefreshDefer_EndCallsDoesNotCallBeginQuery()
    {
        var provider = new CustomBeginQueryDataSourceProvider
        {
            IsInitialLoadEnabled = false
        };
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        IDisposable defer = provider.DeferRefresh();

        // Begin.
        provider.BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);
        
        // End.
        provider.EndInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // End defer.
        defer.Dispose();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void BeginInit_InvokeMultipleTimes_Success()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        // Begin.
        provider.BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // Invoke again.
        provider.BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);
        
        // End.
        provider.EndInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // End agian.
        provider.EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void BeginInit_InvokeMultipleTimesEnded_Success()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        // End.
        provider.EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // Begin.
        provider.BeginInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // Invoke again.
        provider.BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // End agian.
        provider.EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void ISupportInitializeBeginInit_Invoke_EndCallsBeginQuery()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        // Begin.
        ((ISupportInitialize)provider).BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);
        
        // End.
        ((ISupportInitialize)provider).EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void ISupportInitializeBeginInit_InvokeInitialLoadDisabled_EndCallsBeginQuery()
    {
        var provider = new CustomBeginQueryDataSourceProvider
        {
            IsInitialLoadEnabled = false
        };
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        // Begin.
        ((ISupportInitialize)provider).BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);
        
        // End.
        ((ISupportInitialize)provider).EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void ISupportInitializeBeginInit_InvokeInitialLoaded_EndCallsBeginQuery()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        provider.InitialLoad();
        Assert.Equal(1, callCount);

        // Begin.
        ((ISupportInitialize)provider).BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
        
        // End.
        ((ISupportInitialize)provider).EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void ISupportInitializeBeginInit_InvokeRefreshed_EndCallsBeginQuery()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        provider.Refresh();
        Assert.Equal(1, callCount);

        // Begin.
        ((ISupportInitialize)provider).BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
        
        // End.
        ((ISupportInitialize)provider).EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void ISupportInitializeBeginInit_InvokeRefreshDefer_EndCallsDoesNotCallBeginQuery()
    {
        var provider = new CustomBeginQueryDataSourceProvider
        {
            IsInitialLoadEnabled = false
        };
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        IDisposable defer = provider.DeferRefresh();

        // Begin.
        ((ISupportInitialize)provider).BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);
        
        // End.
        ((ISupportInitialize)provider).EndInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // End defer.
        defer.Dispose();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void ISupportInitializeBeginInit_InvokeMultipleTimes_Success()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        // Begin.
        ((ISupportInitialize)provider).BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // Invoke again.
        ((ISupportInitialize)provider).BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);
        
        // End.
        ((ISupportInitialize)provider).EndInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // End agian.
        ((ISupportInitialize)provider).EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void ISupportInitializeBeginInit_InvokeMultipleTimesEnded_Success()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        // End.
        ((ISupportInitialize)provider).EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // Begin.
        ((ISupportInitialize)provider).BeginInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // Invoke again.
        ((ISupportInitialize)provider).BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // End agian.
        ((ISupportInitialize)provider).EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void ISupportInitializeBeginInit_Invoke_CallsBeginInit()
    {
        var provider = new CustomInitDataSourceProvider();
        int callCount = 0;
        provider.BeginInitAction += () => callCount++;

        // Invoke.
        ((ISupportInitialize)provider).BeginInit();
        Assert.Equal(1, callCount);

        // Invoke again.
        ((ISupportInitialize)provider).BeginInit();
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void BeginQuery_Invoke_Nop()
    {
        var provider = new SubDataSourceProvider();

        // Invoke.
        provider.BeginQuery();
        Assert.False(provider.IsRefreshDeferred);

        // Invoke again.
        provider.BeginQuery();
        Assert.False(provider.IsRefreshDeferred);
    }

    [Fact]
    public void DeferRefresh_Invoke_Success()
    {
        var provider = new SubDataSourceProvider();

        IDisposable result1 = provider.DeferRefresh();
        Assert.NotNull(result1);
        Assert.True(provider.IsRefreshDeferred);

        // Clear.
        result1.Dispose();
        Assert.False(provider.IsRefreshDeferred);
        
        // Clear again.
        result1.Dispose();
        Assert.False(provider.IsRefreshDeferred);
    }

    [Fact]
    public void DeferRefresh_InvokeBeganInitialize_Success()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        IDisposable result1 = provider.DeferRefresh();
        Assert.NotNull(result1);
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // Begin.
        provider.BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // Clear.
        result1.Dispose();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);
        
        // Clear again.
        result1.Dispose();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // End init.
        provider.EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void DeferRefresh_InvokeEndedInitialize_Success()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        IDisposable result1 = provider.DeferRefresh();
        Assert.NotNull(result1);
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // Begin.
        provider.BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // End.
        provider.EndInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // Clear.
        result1.Dispose();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
        
        // Clear again.
        result1.Dispose();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void DeferRefresh_InvokeMultipleTimes_Success()
    {
        var provider = new SubDataSourceProvider();

        // Invoke.
        IDisposable result1 = provider.DeferRefresh();
        Assert.NotNull(result1);
        Assert.True(provider.IsRefreshDeferred);

        // Invoke again.
        IDisposable result2 = provider.DeferRefresh();
        Assert.NotNull(result2);
        Assert.NotSame(result1, result2);
        Assert.True(provider.IsRefreshDeferred);

        // Clear first.
        result1.Dispose();
        Assert.True(provider.IsRefreshDeferred);

        // Clear first again.
        result1.Dispose();
        Assert.True(provider.IsRefreshDeferred);

        // Clear second.
        result2.Dispose();
        Assert.False(provider.IsRefreshDeferred);
    }

    [Fact]
    public void EndInit_Invoke_Success()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        // Begin.
        provider.BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // End.
        provider.EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void EndInit_InvokeMultipleTimes_Success()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        // Begin.
        provider.BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // End.
        provider.EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);

        // End again. 
        provider.EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);

        // Begin.
        provider.BeginInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);

        // Begin again.
        provider.BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void EndInit_InvokeNotBegan_Success()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        // End.
        provider.EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // Begin.
        provider.BeginInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // End.
        provider.EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void EndInit_InvokeInitialLoaded_Success()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        // Begin.
        provider.BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // Load.
        provider.InitialLoad();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);

        // End.
        provider.EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void EndInit_InvokeRefreshDeferred_Success()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        // Begin.
        provider.BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // Defer.
        IDisposable defer = provider.DeferRefresh();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // End.
        provider.EndInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // End again.
        provider.EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void ISupportInitializeEndInit_Invoke_Success()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        // Begin.
        ((ISupportInitialize)provider).BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // End.
        ((ISupportInitialize)provider).EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void ISupportInitializeEndInit_InvokeMultipleTimes_Success()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        // Begin.
        ((ISupportInitialize)provider).BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // End.
        ((ISupportInitialize)provider).EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);

        // End again. 
        ((ISupportInitialize)provider).EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);

        // Begin.
        ((ISupportInitialize)provider).BeginInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);

        // Begin again.
        ((ISupportInitialize)provider).BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void ISupportInitializeEndInit_InvokeNotBegan_Success()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        // End.
        ((ISupportInitialize)provider).EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // Begin.
        ((ISupportInitialize)provider).BeginInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // End.
        ((ISupportInitialize)provider).EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void ISupportInitializeEndInit_InvokeInitialLoaded_Success()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        // Begin.
        ((ISupportInitialize)provider).BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // Load.
        provider.InitialLoad();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);

        // End.
        ((ISupportInitialize)provider).EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void ISupportInitializeEndInit_InvokeRefreshDeferred_Success()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction += () => callCount++;

        // Begin.
        ((ISupportInitialize)provider).BeginInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // Defer.
        IDisposable defer = provider.DeferRefresh();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // End.
        ((ISupportInitialize)provider).EndInit();
        Assert.True(provider.IsRefreshDeferred);
        Assert.Equal(0, callCount);

        // End again.
        ((ISupportInitialize)provider).EndInit();
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void ISupportInitializeEndInit_Invoke_CallsEndInit()
    {
        var provider = new CustomInitDataSourceProvider();
        int callCount = 0;
        provider.EndInitAction += () => callCount++;

        // Invoke.
        ((ISupportInitialize)provider).EndInit();
        Assert.Equal(1, callCount);

        // Invoke again.
        ((ISupportInitialize)provider).EndInit();
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void InitialLoad_Invoke_CallsBeginQuery()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction = () => callCount++;
        
        // Invoke.
        provider.InitialLoad();
        Assert.Equal(1, callCount);
        Assert.False(provider.IsRefreshDeferred);

        // Invoke again.
        provider.InitialLoad();
        Assert.Equal(1, callCount);
        Assert.False(provider.IsRefreshDeferred);
    }

    [Fact]
    public void InitialLoad_InvokeInitialLoadNotEnabled_DoesNotCallBeginQuery()
    {
        var provider = new CustomBeginQueryDataSourceProvider
        {
            IsInitialLoadEnabled = false
        };
        int callCount = 0;
        provider.BeginQueryAction = () => callCount++;
        
        // Invoke.
        provider.InitialLoad();
        Assert.Equal(0, callCount);
        Assert.True(provider.IsRefreshDeferred);

        // Invoke again.
        provider.InitialLoad();
        Assert.Equal(0, callCount);
        Assert.True(provider.IsRefreshDeferred);
    }

    [Fact]
    public void InitialLoad_InvokeBeganInitialize_CallsBeginQuery()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction = () => callCount++;

        provider.BeginInit();
        Assert.Equal(0, callCount);
        Assert.True(provider.IsRefreshDeferred);
        
        // Invoke.
        provider.InitialLoad();
        Assert.Equal(1, callCount);
        Assert.True(provider.IsRefreshDeferred);

        // Invoke again.
        provider.InitialLoad();
        Assert.Equal(1, callCount);
        Assert.True(provider.IsRefreshDeferred);
    }

    [Fact]
    public void InitialLoad_InvokeEndedInitialize_DoesNotCallBeginQuery()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction = () => callCount++;

        provider.BeginInit();
        Assert.Equal(0, callCount);
        Assert.True(provider.IsRefreshDeferred);
        
        provider.EndInit();
        Assert.Equal(1, callCount);
        Assert.False(provider.IsRefreshDeferred);
        
        // Invoke.
        provider.InitialLoad();
        Assert.Equal(1, callCount);
        Assert.False(provider.IsRefreshDeferred);

        // Invoke again.
        provider.InitialLoad();
        Assert.Equal(1, callCount);
        Assert.False(provider.IsRefreshDeferred);
    }

    [Fact]
    public void InitialLoad_InvokeRefreshed_DoesNotCallBeginQuery()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction = () => callCount++;

        provider.Refresh();
        Assert.Equal(1, callCount);
        Assert.False(provider.IsRefreshDeferred);
        
        // Invoke.
        provider.InitialLoad();
        Assert.Equal(1, callCount);
        Assert.False(provider.IsRefreshDeferred);

        // Invoke again.
        provider.InitialLoad();
        Assert.Equal(1, callCount);
        Assert.False(provider.IsRefreshDeferred);
    }

    [Fact]
    public void Refresh_Invoke_CallsBeginQuery()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction = () => callCount++;
        
        // Refresh.
        provider.Refresh();
        Assert.Equal(1, callCount);
        Assert.False(provider.IsRefreshDeferred);

        // Refresh again.
        provider.Refresh();
        Assert.Equal(2, callCount);
        Assert.False(provider.IsRefreshDeferred);
    }

    [Fact]
    public void Refresh_InvokeInitialLoaded_CallsBeginQuery()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction = () => callCount++;
        
        provider.InitialLoad();
        Assert.Equal(1, callCount);
        Assert.False(provider.IsRefreshDeferred);
        
        // Refresh.
        provider.Refresh();
        Assert.Equal(2, callCount);
        Assert.False(provider.IsRefreshDeferred);

        // Refresh again.
        provider.Refresh();
        Assert.Equal(3, callCount);
        Assert.False(provider.IsRefreshDeferred);
    }

    [Fact]
    public void Refresh_InvokeRefreshDeferred_CallsBeginQuery()
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction = () => callCount++;

        provider.DeferRefresh();
        Assert.Equal(0, callCount);
        Assert.True(provider.IsRefreshDeferred);

        // Refresh.
        provider.Refresh();
        Assert.Equal(1, callCount);
        Assert.True(provider.IsRefreshDeferred);

        // Refresh again.
        provider.Refresh();
        Assert.Equal(2, callCount);
        Assert.True(provider.IsRefreshDeferred);
    }

    public static IEnumerable<object?[]> OnPropertyChanged_TestData()
    {
        yield return new object?[] { new PropertyChangedEventArgs("Name") };
        yield return new object?[] { null };
    }

    [Theory]
    [MemberData(nameof(OnPropertyChanged_TestData))]
    public void OnPropertyChanged_Invoke_CallsPropertyChangedEvent(PropertyChangedEventArgs eventArgs)
    {
        var provider = new SubDataSourceProvider();
        int callCount = 0;
        PropertyChangedEventHandler handler = (sender, e) =>
        {
            Assert.Same(provider, sender);
            Assert.Same(eventArgs, e);
            callCount++;
        };

        // Call with handler.
        ((INotifyPropertyChanged)provider).PropertyChanged += handler;
        provider.OnPropertyChanged(eventArgs);
        Assert.Equal(1, callCount);

        // Remove handler.
        ((INotifyPropertyChanged)provider).PropertyChanged -= handler;
        provider.OnPropertyChanged(eventArgs);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void OnQueryFinished_InvokeSimple_Success()
    {
        var provider = new SubDataSourceProvider();
        var newData = new object();
        
        // Invoke.
        provider.OnQueryFinished(newData);
        Assert.Same(newData, provider.Data);
        Assert.NotNull(provider.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, provider.Dispatcher);
        Assert.Null(provider.Error);
        Assert.True(provider.IsInitialLoadEnabled);
        Assert.False(provider.IsRefreshDeferred);
        
        // Invoke again.
        provider.OnQueryFinished(newData);
        Assert.Same(newData, provider.Data);
        Assert.NotNull(provider.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, provider.Dispatcher);
        Assert.Null(provider.Error);
        Assert.True(provider.IsInitialLoadEnabled);
        Assert.False(provider.IsRefreshDeferred);
        
        // Invoke null.
        provider.OnQueryFinished(null!);
        Assert.Null(provider.Data);
        Assert.NotNull(provider.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, provider.Dispatcher);
        Assert.Null(provider.Error);
        Assert.True(provider.IsInitialLoadEnabled);
        Assert.False(provider.IsRefreshDeferred);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(null)]
    public void OnQueryFinished_InvokeSimpleInitialLoaded_DoesNotResetInitialLoad(object? newData)
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction = () => callCount++;

        provider.InitialLoad();
        Assert.Equal(1, callCount);

        // Invoke.
        provider.OnQueryFinished(newData!);
        Assert.Equal(newData, provider.Data);
        Assert.NotNull(provider.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, provider.Dispatcher);
        Assert.Null(provider.Error);
        Assert.True(provider.IsInitialLoadEnabled);
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
        
        // Load again.
        provider.InitialLoad();
        Assert.Equal(1, callCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(null)]
    public void OnQueryFinished_InvokeSimpleWithError_Success(object? newData)
    {
        var provider = new SubDataSourceProvider();
#pragma warning disable CA2201 // Do not raise reserved exception types
        provider.OnQueryFinished(new object(), new Exception(), e => e, new object());
#pragma warning restore CA2201 // Do not raise reserved exception types

        // Invoke.
        provider.OnQueryFinished(newData!);
        Assert.Equal(newData, provider.Data);
        Assert.NotNull(provider.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, provider.Dispatcher);
        Assert.Null(provider.Error);
        Assert.True(provider.IsInitialLoadEnabled);
        Assert.False(provider.IsRefreshDeferred);
    }

    [Fact]
    public void OnQueryFinished_InvokeSimpleWithPropertyChangedHandler_CallsPropertyChanged()
    {
        var provider = new SubDataSourceProvider();
        int callCount = 0;
        object? newData = null;
        PropertyChangedEventHandler handler = (sender, e) =>
        {
            Assert.Same(provider, sender);
            Assert.Equal("Data", e.PropertyName);
            Assert.Same(newData, provider.Data);
            callCount++;
        };
        ((INotifyPropertyChanged)provider).PropertyChanged += handler;

        // Call.
        newData = new object();
        provider.OnQueryFinished(newData);
        Assert.Equal(1, callCount);

        // Call again.
        provider.OnQueryFinished(newData);
        Assert.Equal(2, callCount);

        // Call different.
        newData = null;
        provider.OnQueryFinished(null!);
        Assert.Equal(3, callCount);

        // Remove handler.
        ((INotifyPropertyChanged)provider).PropertyChanged -= handler;
        newData = new object();
        provider.OnQueryFinished(newData);
        Assert.Equal(3, callCount);
    }

    [Fact]
    public void OnQueryFinished_InvokeSimpleWithDataChangedHandler_CallsDataChanged()
    {
        var provider = new SubDataSourceProvider();
        int callCount = 0;
        object? newData = null;
        EventHandler handler = (sender, e) =>
        {
            Assert.Same(provider, sender);
            Assert.Same(EventArgs.Empty, e);
            Assert.Same(newData, provider.Data);
            callCount++;
        };
        provider.DataChanged += handler;

        // Call.
        newData = new object();
        provider.OnQueryFinished(newData);
        Assert.Equal(1, callCount);

        // Call again.
        provider.OnQueryFinished(newData);
        Assert.Equal(2, callCount);

        // Call different.
        newData = null;
        provider.OnQueryFinished(newData!);
        Assert.Equal(3, callCount);

        // Remove handler.
        provider.DataChanged -= handler;
        newData = new object();
        provider.OnQueryFinished(newData);
        Assert.Equal(3, callCount);
    }

    [Fact]
    public void OnQueryFinished_InvokeSimpleWithPropertyChangedAndDataChangedHandler_CallsPropertyChangedAndDataChanged()
    {
        var provider = new SubDataSourceProvider();
        int propertyChangedCallCount = 0;
        int dataChangedCallCount = 0;
        object? newData = null;
        PropertyChangedEventHandler propertyChangedHandler = (sender, e) =>
        {
            Assert.Equal(dataChangedCallCount, propertyChangedCallCount);
            Assert.Same(provider, sender);
            Assert.Equal("Data", e.PropertyName);
            propertyChangedCallCount++;
        };
        ((INotifyPropertyChanged)provider).PropertyChanged += propertyChangedHandler;
        EventHandler dataChangedHandler = (sender, e) =>
        {
            Assert.True(propertyChangedCallCount > dataChangedCallCount);
            Assert.Same(provider, sender);
            Assert.Same(EventArgs.Empty, e);
            dataChangedCallCount++;
        };
        provider.DataChanged += dataChangedHandler;

        // Call.
        newData = new object();
        provider.OnQueryFinished(newData);
        Assert.Equal(1, propertyChangedCallCount);
        Assert.Equal(1, dataChangedCallCount);

        // Call again.
        provider.OnQueryFinished(newData);
        Assert.Equal(2, propertyChangedCallCount);
        Assert.Equal(2, dataChangedCallCount);

        // Call different.
        newData = null;
        provider.OnQueryFinished(newData!);
        Assert.Equal(3, propertyChangedCallCount);
        Assert.Equal(3, dataChangedCallCount);

        // Remove handler.
        ((INotifyPropertyChanged)provider).PropertyChanged -= propertyChangedHandler;
        provider.DataChanged -= dataChangedHandler;
        newData = new object();
        provider.OnQueryFinished(newData);
        Assert.Equal(3, propertyChangedCallCount);
        Assert.Equal(3, dataChangedCallCount);
    }

    [Fact]
    public void OnQueryFinished_InvokeSimpleOnDifferentThread_Success()
    {
        var provider = new SubDataSourceProvider();

        bool? success = false;
        var thread = new Thread(() =>
        {
            try
            { 
                provider.OnQueryFinished(new object());
                success = true;
            }
            catch
            {
                success = false;
            }
        });
        thread.Start();
        thread.Join();
        Assert.True(success);
    }
    
    [Theory]
    [InlineData(1)]
    [InlineData(null)]
    public void OnQueryFinished_InvokeSimple_CallsComplex(object? newData)
    {
        var provider = new CustomOnQueryFinishedDataSourceProvider();

        int callCount = 0;
        provider.OnQueryFinishedAction = (data, error, completionWork, callbackArguments) =>
        {
            Assert.Equal(newData, data);
            Assert.Null(error);
            Assert.Null(completionWork);
            Assert.Null(callbackArguments);
            callCount++;
        };

        // Call.
        provider.OnQueryFinished(newData!);
        Assert.Equal(1, callCount);

        // Call again.
        provider.OnQueryFinished(newData!);
        Assert.Equal(2, callCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(null)]
    public void OnQueryFinished_InvokeComplex_Success(object? newData)
    {
        var provider = new SubDataSourceProvider();

        // Invoke.
        provider.OnQueryFinished(newData!, null!, null!, null!);
        Assert.Equal(newData, provider.Data);
        Assert.NotNull(provider.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, provider.Dispatcher);
        Assert.Null(provider.Error);
        Assert.True(provider.IsInitialLoadEnabled);
        Assert.False(provider.IsRefreshDeferred);
        
        // Invoke again.
        provider.OnQueryFinished(newData!, null!, null!, null!);
        Assert.Equal(newData, provider.Data);
        Assert.NotNull(provider.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, provider.Dispatcher);
        Assert.Null(provider.Error);
        Assert.True(provider.IsInitialLoadEnabled);
        Assert.False(provider.IsRefreshDeferred);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(null)]
    public void OnQueryFinished_InvokeComplexWithError_Success(object? newData)
    {
        var provider = new SubDataSourceProvider();
#pragma warning disable CA2201 // Do not raise reserved exception types
        var error = new Exception();
#pragma warning restore CA2201 // Do not raise reserved exception types

        // Invoke.
        provider.OnQueryFinished(newData!, error, null!, null!);
        Assert.Null(provider.Data);
        Assert.NotNull(provider.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, provider.Dispatcher);
        Assert.Same(error, provider.Error);
        Assert.True(provider.IsInitialLoadEnabled);
        Assert.False(provider.IsRefreshDeferred);
        
        // Invoke again.
        provider.OnQueryFinished(newData!, error, null!, null!);
        Assert.Null(provider.Data);
        Assert.NotNull(provider.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, provider.Dispatcher);
        Assert.Same(error, provider.Error);
        Assert.True(provider.IsInitialLoadEnabled);
        Assert.False(provider.IsRefreshDeferred);
        
        // Clear error.
        provider.OnQueryFinished(newData!, null!, null!, null!);
        Assert.Equal(newData, provider.Data);
        Assert.NotNull(provider.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, provider.Dispatcher);
        Assert.Null(provider.Error);
        Assert.True(provider.IsInitialLoadEnabled);
        Assert.False(provider.IsRefreshDeferred);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(1, null)]
    [InlineData(null, 2)]
    [InlineData(null, null)]
    public void OnQueryFinished_InvokeComplexWithCallback_CallsCallback(object? newData, object? callbackArg)
    {
        var provider = new SubDataSourceProvider();
        int callbackCallCount = 0;
        DispatcherOperationCallback callback = arg =>
        {
            Assert.Same(callbackArg, arg);
            Assert.Equal(newData, provider.Data);
            callbackCallCount++;
            return null!;
        };

        // Invoke.
        provider.OnQueryFinished(newData!, null!, callback, callbackArg!);
        Assert.Equal(1, callbackCallCount);
        Assert.Equal(newData, provider.Data);
        Assert.NotNull(provider.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, provider.Dispatcher);
        Assert.Null(provider.Error);
        Assert.True(provider.IsInitialLoadEnabled);
        Assert.False(provider.IsRefreshDeferred);
        
        // Invoke again.
        provider.OnQueryFinished(newData!, null!, callback, callbackArg!);
        Assert.Equal(2, callbackCallCount);
        Assert.Equal(newData, provider.Data);
        Assert.NotNull(provider.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, provider.Dispatcher);
        Assert.Null(provider.Error);
        Assert.True(provider.IsInitialLoadEnabled);
        Assert.False(provider.IsRefreshDeferred);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(null)]
    public void OnQueryFinished_InvokeComplexInitialLoadedWithError_ResetsInitialLoad(object? newData)
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction = () => callCount++;
#pragma warning disable CA2201 // Do not raise reserved exception types
        var error = new Exception();
#pragma warning restore CA2201 // Do not raise reserved exception types

        provider.InitialLoad();
        Assert.Equal(1, callCount);

        // Invoke.
        provider.OnQueryFinished(newData!, error, null!, null!);
        Assert.Null(provider.Data);
        Assert.NotNull(provider.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, provider.Dispatcher);
        Assert.Same(error, provider.Error);
        Assert.True(provider.IsInitialLoadEnabled);
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
        
        // Load again.
        provider.InitialLoad();
        Assert.Equal(2, callCount);

        // Invoke again.
        provider.OnQueryFinished(newData!, error, null!, null!);
        Assert.Null(provider.Data);
        Assert.NotNull(provider.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, provider.Dispatcher);
        Assert.Same(error, provider.Error);
        Assert.True(provider.IsInitialLoadEnabled);
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(2, callCount);
        
        // Load again.
        provider.InitialLoad();
        Assert.Equal(3, callCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(null)]
    public void OnQueryFinished_InvokeComplexInitialLoadedWithoutError_DoesNotResetInitialLoad(object? newData)
    {
        var provider = new CustomBeginQueryDataSourceProvider();
        int callCount = 0;
        provider.BeginQueryAction = () => callCount++;

        provider.InitialLoad();
        Assert.Equal(1, callCount);

        // Invoke.
        provider.OnQueryFinished(newData!, null!, null!, null!);
        Assert.Equal(newData, provider.Data);
        Assert.NotNull(provider.Dispatcher);
        Assert.Same(Dispatcher.CurrentDispatcher, provider.Dispatcher);
        Assert.Null(provider.Error);
        Assert.True(provider.IsInitialLoadEnabled);
        Assert.False(provider.IsRefreshDeferred);
        Assert.Equal(1, callCount);
        
        // Load again.
        provider.InitialLoad();
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void OnQueryFinished_InvokeComplexWithPropertyChangedHandler_CallsPropertyChanged()
    {
        var provider = new SubDataSourceProvider();
        int callCount = 0;
        int callbackCallCount = 0;
        object? newData = null;
        var callbackArguments = new object();
        PropertyChangedEventHandler handler = (sender, e) =>
        {
            Assert.True(callbackCallCount > callCount);
            Assert.Same(provider, sender);
            Assert.Equal("Data", e.PropertyName);
            Assert.Same(newData, provider.Data);
            callCount++;
        };
        ((INotifyPropertyChanged)provider).PropertyChanged += handler;
        DispatcherOperationCallback callback = (arg) =>
        {
            Assert.Equal(callbackCallCount, callCount);
            Assert.Same(callbackArguments, arg);
            Assert.Same(newData, provider.Data);
            callbackCallCount++;
            return null;
        };

        // Call.
        newData = new object();
        provider.OnQueryFinished(newData, null!, callback, callbackArguments);
        Assert.Equal(1, callCount);
        Assert.Equal(1, callbackCallCount);

        // Call again.
        provider.OnQueryFinished(newData, null!, callback, callbackArguments);
        Assert.Equal(2, callCount);
        Assert.Equal(2, callbackCallCount);

        // Call different.
        newData = null;
        provider.OnQueryFinished(null!, null!, callback, callbackArguments);
        Assert.Equal(3, callCount);
        Assert.Equal(3, callbackCallCount);

        // Remove handler.
        ((INotifyPropertyChanged)provider).PropertyChanged -= handler;
        newData = new object();
        provider.OnQueryFinished(newData, null!, callback, callbackArguments);
        Assert.Equal(3, callCount);
        Assert.Equal(4, callbackCallCount);
    }

    [Fact]
    public void OnQueryFinished_InvokeComplexWithErrorWithPropertyChangedHandler_CallsPropertyChanged()
    {
        var provider = new SubDataSourceProvider();
        int callCount = 0;
        object? newData = null;
        var callbackArguments = new object();
        var propertyNames = new List<string>();
        PropertyChangedEventHandler handler = (sender, e) =>
        {
            Assert.Same(provider, sender);
            propertyNames.Add(e.PropertyName!);
            callCount++;
        };
        ((INotifyPropertyChanged)provider).PropertyChanged += handler;

#pragma warning disable CA2201 // Do not raise reserved exception types
        var error1 = new Exception();
#pragma warning restore CA2201 // Do not raise reserved exception types
#pragma warning disable CA2201 // Do not raise reserved exception types
        var error2 = new Exception();
#pragma warning restore CA2201 // Do not raise reserved exception types

        // Call.
        newData = new object();
        provider.OnQueryFinished(newData, error1, null!, null!);
        Assert.Equal(2, callCount);
        Assert.Equal(new[] { "Data", "Error" }, propertyNames);
        Assert.Same(error1, provider.Error);

        // Call again.
        provider.OnQueryFinished(newData, error1, null!, null!);
        Assert.Equal(3, callCount);
        Assert.Equal(new[] { "Data", "Error", "Data" }, propertyNames);
        Assert.Same(error1, provider.Error);

        // Call different.
        newData = null;
        provider.OnQueryFinished(null!, error2, null!, null!);
        Assert.Equal(5, callCount);
        Assert.Equal(new[] { "Data", "Error", "Data", "Data", "Error" }, propertyNames);
        Assert.Same(error2, provider.Error);

        // Call different.
        provider.OnQueryFinished(null!, null!, null!, null!);
        Assert.Equal(7, callCount);
        Assert.Equal(new[] { "Data", "Error", "Data", "Data", "Error", "Data", "Error" }, propertyNames);
        Assert.Null(provider.Error);

        // Remove handler.
        ((INotifyPropertyChanged)provider).PropertyChanged -= handler;
        newData = new object();
        provider.OnQueryFinished(newData, error1, null!, null!);
        Assert.Equal(7, callCount);
        Assert.Equal(new[] { "Data", "Error", "Data", "Data", "Error", "Data", "Error" }, propertyNames);
        Assert.Same(error1, provider.Error);
    }

    [Fact]
    public void OnQueryFinished_InvokeComplexWithDataChangedHandler_CallsDataChanged()
    {
        var provider = new SubDataSourceProvider();
        int callCount = 0;
        int callbackCallCount = 0;
        var callbackArguments = new object();
        object? newData = null;
        EventHandler handler = (sender, e) =>
        {
            Assert.Same(provider, sender);
            Assert.Same(EventArgs.Empty, e);
            Assert.Same(newData, provider.Data);
            callCount++;
        };
        provider.DataChanged += handler;
        DispatcherOperationCallback callback = (arg) =>
        {
            Assert.Equal(callbackCallCount, callCount);
            Assert.Same(callbackArguments, arg);
            Assert.Same(newData, provider.Data);
            callbackCallCount++;
            return null;
        };

        // Call.
        newData = new object();
        provider.OnQueryFinished(newData, null!, callback, callbackArguments);
        Assert.Equal(1, callCount);
        Assert.Equal(1, callbackCallCount);

        // Call again.
        provider.OnQueryFinished(newData, null!, callback, callbackArguments);
        Assert.Equal(2, callCount);
        Assert.Equal(2, callbackCallCount);

        // Call different.
        newData = null;
        provider.OnQueryFinished(newData!, null!, callback, callbackArguments);
        Assert.Equal(3, callCount);
        Assert.Equal(3, callbackCallCount);

        // Remove handler.
        provider.DataChanged -= handler;
        newData = new object();
        provider.OnQueryFinished(newData, null!, callback, callbackArguments);
        Assert.Equal(3, callCount);
        Assert.Equal(4, callbackCallCount);
    }

    [Fact]
    public void OnQueryFinished_InvokeComplexWithPropertyChangedAndDataChangedHandler_CallsPropertyChangedAndDataChanged()
    {
        var provider = new SubDataSourceProvider();
        int propertyChangedCallCount = 0;
        int dataChangedCallCount = 0;
        int callbackCallCount = 0;
        var callbackArguments = new object();
        object? newData = null;
        PropertyChangedEventHandler propertyChangedHandler = (sender, e) =>
        {
            Assert.True(callbackCallCount > propertyChangedCallCount);
            Assert.Equal(dataChangedCallCount, propertyChangedCallCount);
            Assert.Same(provider, sender);
            Assert.Equal("Data", e.PropertyName);
            propertyChangedCallCount++;
        };
        ((INotifyPropertyChanged)provider).PropertyChanged += propertyChangedHandler;
        EventHandler dataChangedHandler = (sender, e) =>
        {
            Assert.True(propertyChangedCallCount > dataChangedCallCount);
            Assert.True(callbackCallCount > dataChangedCallCount);
            Assert.Same(provider, sender);
            Assert.Same(EventArgs.Empty, e);
            dataChangedCallCount++;
        };
        provider.DataChanged += dataChangedHandler;
        DispatcherOperationCallback callback = (arg) =>
        {
            Assert.Equal(callbackCallCount, propertyChangedCallCount);
            Assert.Equal(callbackCallCount, dataChangedCallCount);
            Assert.Same(callbackArguments, arg);
            Assert.Same(newData, provider.Data);
            callbackCallCount++;
            return null;
        };

        // Call.
        newData = new object();
        provider.OnQueryFinished(newData, null!, callback, callbackArguments);
        Assert.Equal(1, propertyChangedCallCount);
        Assert.Equal(1, dataChangedCallCount);
        Assert.Equal(1, callbackCallCount);

        // Call again.
        provider.OnQueryFinished(newData, null!, callback, callbackArguments);
        Assert.Equal(2, propertyChangedCallCount);
        Assert.Equal(2, dataChangedCallCount);
        Assert.Equal(2, callbackCallCount);

        // Call different.
        newData = null;
        provider.OnQueryFinished(newData!, null!, callback, callbackArguments);
        Assert.Equal(3, propertyChangedCallCount);
        Assert.Equal(3, dataChangedCallCount);
        Assert.Equal(3, callbackCallCount);

        // Remove handler.
        ((INotifyPropertyChanged)provider).PropertyChanged -= propertyChangedHandler;
        provider.DataChanged -= dataChangedHandler;
        newData = new object();
        provider.OnQueryFinished(newData, null!, callback, callbackArguments);
        Assert.Equal(3, propertyChangedCallCount);
        Assert.Equal(3, dataChangedCallCount);
        Assert.Equal(4, callbackCallCount);
    }

    [Fact]
    public void OnQueryFinished_InvokeComplexOnDifferentThread_Success()
    {
        var provider = new SubDataSourceProvider();

        bool? success = false;
        var thread = new Thread(() =>
        {
            try
            { 
#pragma warning disable CA2201 // Do not raise reserved exception types
                provider.OnQueryFinished(new object(), new Exception(), e => e, new object());
#pragma warning restore CA2201 // Do not raise reserved exception types
                success = true;
            }
            catch
            {
                success = false;
            }
        });
        thread.Start();
        thread.Join();
        Assert.True(success);
    }

    // TODO: this causes a crash.
#if false
    [Fact]
    public void OnQueryFinished_NullDispatcher_ThrowsNullReferenceException()
    {
        var provider = new SubDataSourceProvider();
        provider.Dispatcher = null;
        Assert.Throws<NullReferenceException>(() => provider.OnQueryFinished(new object()));
        Assert.Throws<NullReferenceException>(() => provider.OnQueryFinished(new object(), new Exception(), e => e, new object()));
    }
#endif

    private class CustomBeginQueryDataSourceProvider : DataSourceProvider
    {
        public new Dispatcher? Dispatcher
        {
            get => base.Dispatcher;
            set => base.Dispatcher = value;
        }

        public new bool IsRefreshDeferred => base.IsRefreshDeferred;

        public new void BeginInit() => base.BeginInit();

        public Action? BeginQueryAction { get; set; }

        protected override void BeginQuery()
        {
            if (BeginQueryAction is null)
            {
                throw new NotImplementedException();
            }

            BeginQueryAction();
        }

        public new void EndInit() => base.EndInit();

        public new void OnQueryFinished(object newData) => base.OnQueryFinished(newData);

        public new void OnQueryFinished(object newData, Exception error, DispatcherOperationCallback completionWork, object callbackArguments) => base.OnQueryFinished(newData, error, completionWork, callbackArguments);
    }

    private class CustomInitDataSourceProvider : DataSourceProvider
    {
        public Action? BeginInitAction { get; set; }

        protected override void BeginInit()
        {
            if (BeginInitAction is null)
            {
                throw new NotImplementedException();
            }

            BeginInitAction();
        }

        public Action? EndInitAction { get; set; }

        protected override void EndInit()
        {
            if (EndInitAction is null)
            {
                throw new NotImplementedException();
            }

            EndInitAction();
        }
    }

    private class CustomOnQueryFinishedDataSourceProvider : DataSourceProvider
    {
        public new void OnQueryFinished(object newData) => base.OnQueryFinished(newData);

        public Action<object, Exception, DispatcherOperationCallback, object>? OnQueryFinishedAction { get; set; }

        protected override void OnQueryFinished(object newData, Exception error, DispatcherOperationCallback completionWork, object callbackArguments)
        {
            if (OnQueryFinishedAction is null)
            {
                throw new NotImplementedException();
            }

            OnQueryFinishedAction(newData, error, completionWork, callbackArguments);
        }
    }

    private class SubDataSourceProvider : DataSourceProvider
    {
        public SubDataSourceProvider() : base()
        {
        }

        public new event PropertyChangedEventHandler PropertyChanged
        {
            add => base.PropertyChanged += value;
            remove => base.PropertyChanged -= value;
        }

        public new Dispatcher? Dispatcher
        {
            get => base.Dispatcher;
            set => base.Dispatcher = value;
        }

        public new bool IsRefreshDeferred => base.IsRefreshDeferred;

        public new void BeginInit() => base.BeginInit();

        public new void BeginQuery() => base.BeginQuery();

        public new void EndInit() => base.EndInit();

        public new void OnPropertyChanged(PropertyChangedEventArgs e) => base.OnPropertyChanged(e);

        public new void OnQueryFinished(object newData) => base.OnQueryFinished(newData);

        public new void OnQueryFinished(object newData, Exception error, DispatcherOperationCallback completionWork, object callbackArguments) => base.OnQueryFinished(newData, error, completionWork, callbackArguments);
    }
}
