// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Tests;

public class CurrentChangingEventArgsTests
{
    [Fact]
    public void Ctor_Default()
    {
        var args = new CurrentChangingEventArgs();
        Assert.True(args.IsCancelable);
        Assert.False(args.Cancel);
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Ctor_Bool(bool isCancelable)
    {
        var args = new CurrentChangingEventArgs(isCancelable);
        Assert.Equal(isCancelable, args.IsCancelable);
        Assert.False(args.Cancel);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Cancel_Set_GetReturnsExpected(bool value)
    {
        var args = new CurrentChangingEventArgs
        {
            // Set.
            Cancel = value
        };
        Assert.Equal(value, args.Cancel);

        // Set same.
        args.Cancel = value;
        Assert.Equal(value, args.Cancel);

        // Set different.
        args.Cancel = !value;
        Assert.Equal(!value, args.Cancel);
    }

    [Fact]
    public void Cancel_SetFalseNotCancellable_Success()
    {
        var args = new CurrentChangingEventArgs(false)
        {
            // Set.
            Cancel = false
        };
        Assert.False(args.Cancel);
    }

    [Fact]
    public void Cancel_SetTrueNotCancellable_ThrowsInvalidOperationException()
    {
        var args = new CurrentChangingEventArgs(false);
        Assert.Throws<InvalidOperationException>(() => args.Cancel = true);
    }
}