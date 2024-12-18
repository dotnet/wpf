// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Media.Test;

public class DisableDpiAwarenessAttributeTests
{
    [Fact]
    public void Ctor_Default()
    {
        var attribute = new DisableDpiAwarenessAttribute();
        Assert.Equal(typeof(DisableDpiAwarenessAttribute), attribute.TypeId);
    }
}