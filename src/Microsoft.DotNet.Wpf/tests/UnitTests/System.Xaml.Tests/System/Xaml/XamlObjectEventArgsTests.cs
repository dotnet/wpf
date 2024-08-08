// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Xaml.Tests;

public class XamlObjectEventArgsTests
{
    [Fact]
    public void Ctor_Object()
    {
        var e = new XamlObjectEventArgs(1);
        Assert.Equal(1, e.Instance);
        Assert.Null(e.SourceBamlUri);
        Assert.Equal(0, e.ElementLineNumber);
        Assert.Equal(0, e.ElementLinePosition);
    }

    [Fact]
    public void Ctor_NullInstance_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("instance", () => new XamlObjectEventArgs(null));
    }
}
