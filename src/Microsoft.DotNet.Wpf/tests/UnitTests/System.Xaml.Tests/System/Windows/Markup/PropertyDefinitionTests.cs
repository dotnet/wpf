// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Windows.Markup.Tests;

public class PropertyDefinitionTests
{
    [Fact]
    public void Ctor_Default()
    {
        var definition = new PropertyDefinition();
        Assert.Empty(definition.Attributes);
        Assert.Null(definition.Name);
        Assert.Null(definition.Type);
        Assert.Null(definition.Modifier);
    }

    [Fact]
    public void Attributes_Get_ReturnsSameInstance()
    {
        var definition = new PropertyDefinition();
        Assert.Same(definition.Attributes, definition.Attributes);
    }
}
