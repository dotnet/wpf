// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Windows.Input.Tests;

public class TraversalRequestTests
{
    [Theory]
    [InlineData(FocusNavigationDirection.Next)]
    [InlineData(FocusNavigationDirection.Previous)]
    [InlineData(FocusNavigationDirection.First)]
    [InlineData(FocusNavigationDirection.Last)]
    [InlineData(FocusNavigationDirection.Left)]
    [InlineData(FocusNavigationDirection.Right)]
    [InlineData(FocusNavigationDirection.Up)]
    [InlineData(FocusNavigationDirection.Down)]
    public void Ctor_FocusNavigationDirection(FocusNavigationDirection focusNavigationDirection)
    {
        var request = new TraversalRequest(focusNavigationDirection);
        Assert.Equal(focusNavigationDirection, request.FocusNavigationDirection);
        Assert.False(request.Wrapped);
    }

    [Theory]
    [InlineData(FocusNavigationDirection.Next - 1)]
    [InlineData(FocusNavigationDirection.Down + 1)]
    public void Ctor_InvalidFocusNavigationDirection_ThrowsInvalidEnumArgumentException(FocusNavigationDirection focusNavigationDirection)
    {
        Assert.Throws<InvalidEnumArgumentException>("focusNavigationDirection", () => new TraversalRequest(focusNavigationDirection));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Wrapped_Set_GetReturnsExpected(bool value)
    {
        var request = new TraversalRequest(FocusNavigationDirection.Next)
        {
            // Set.
            Wrapped = value
        };
        Assert.Equal(value, request.Wrapped);

        // Set same.
        request.Wrapped = value;
        Assert.Equal(value, request.Wrapped);

        // Set different.
        request.Wrapped = !value;
        Assert.Equal(!value, request.Wrapped);
    }
}
