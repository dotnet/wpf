// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Windows.Markup.Tests;

public class UsableDuringInitializationAttributeTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Ctor_Bool(bool usable)
    {
        var attribute = new UsableDuringInitializationAttribute(usable);
        Assert.Equal(usable, attribute.Usable);
    }
}
