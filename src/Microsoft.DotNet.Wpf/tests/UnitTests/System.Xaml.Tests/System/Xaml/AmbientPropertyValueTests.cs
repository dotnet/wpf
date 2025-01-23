// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Xaml.Tests;

public class AmbientPropertyValueTests
{
    public static IEnumerable<object?[]> Ctor_XamlMember_Object_TestData()
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        yield return new object?[] { new XamlMember("name", type, false), 1 };
        yield return new object?[] { null, null };
    }

    [Theory]
    [MemberData(nameof(Ctor_XamlMember_Object_TestData))]
    public void Ctor_XamlMember_Object(XamlMember property, object value)
    {
        var ambientValue = new AmbientPropertyValue(property, value);
        Assert.Equal(property, ambientValue.RetrievedProperty);
        Assert.Equal(value, ambientValue.Value);
    }
}
