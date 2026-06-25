// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Windows.Markup;

namespace System.Windows.Tests;

public class DesignerSerializationOptionsAttributeTests
{
    [Theory]
    [InlineData(DesignerSerializationOptions.SerializeAsAttribute)]
    public void Ctor_DesignerSerializationOptions(DesignerSerializationOptions options)
    {
        var attribute = new DesignerSerializationOptionsAttribute(options);
        Assert.Equal(options, attribute.DesignerSerializationOptions);
    }

    [Theory]
    [InlineData((DesignerSerializationOptions)0)]
    [InlineData(DesignerSerializationOptions.SerializeAsAttribute + 1)]
    public void Ctor_InvalidOptions_ThrowsInvalidEnumArgumentException(DesignerSerializationOptions options)
    {
        // TODO: add paramName.
        Assert.Throws<InvalidEnumArgumentException>(() => new DesignerSerializationOptionsAttribute(options));
    }
}
