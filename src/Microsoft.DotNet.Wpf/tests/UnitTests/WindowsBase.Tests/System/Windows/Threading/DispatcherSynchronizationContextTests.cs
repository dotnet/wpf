// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Windows.Threading.Tests;

public class DispatcherSynchronizationContextTests
{
    [Fact]
    public void Ctor_Default()
    {
        new DispatcherSynchronizationContext();
    }

    [Fact]
    public void Ctor_Dispatcher()
    {
        new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher);
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
    public void Ctor_Dispatcher_DispatcherPriority(DispatcherPriority priority)
    {
        new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher, priority);
    }

    [Fact]
    public void Ctor_NullDispatcher_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("dispatcher", () => new DispatcherSynchronizationContext(null));
        Assert.Throws<ArgumentNullException>("dispatcher", () => new DispatcherSynchronizationContext(null, DispatcherPriority.Normal));
    }

    [Theory]
    [InlineData(DispatcherPriority.Invalid)]
    [InlineData(DispatcherPriority.Invalid - 1)]
    [InlineData(DispatcherPriority.Send + 1)]
    public void Ctor_InvalidPriority_ThrowsInvalidEnumArgumentException(DispatcherPriority priority)
    {
        Assert.Throws<InvalidEnumArgumentException>("priority", () => new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher, priority));
    }

    [Fact]
    public void CreateCopy_Default_ReturnsSelf()
    {
        var context = new DispatcherSynchronizationContext();
        DispatcherSynchronizationContext copy = Assert.IsType<DispatcherSynchronizationContext>(context.CreateCopy());
        Assert.NotNull(copy);
        Assert.NotSame(context, copy);
    }

    [Theory]
    [InlineData(DispatcherPriority.ApplicationIdle)]
    public void CreateCopy_Custom_ReturnsSelf(DispatcherPriority priority)
    {
        var context = new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher, priority);
        DispatcherSynchronizationContext copy = Assert.IsType<DispatcherSynchronizationContext>(context.CreateCopy());
        Assert.NotNull(copy);
        Assert.NotSame(context, copy);
    }
}
