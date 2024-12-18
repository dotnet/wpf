// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Windows.Tests;

public class ExpressionTests
{
    [Fact]
    public void TypeConverter_Get_ReturnsExpected()
    {
        Assert.IsType<ExpressionConverter>(TypeDescriptor.GetConverter(typeof(Expression)));
    }
}
