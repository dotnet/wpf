// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xaml;
using Xunit;

namespace System.Windows.Markup.Tests;

public class XamlSetValueEventArgsTests
{
    public static IEnumerable<object?[]> Ctor_XamlMember_Object_TestData()
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        yield return new object?[] { null, null };
        yield return new object?[] { new XamlMember("name", type, false), new object() };
    }

    [Theory]
    [MemberData(nameof(Ctor_XamlMember_Object_TestData))]
    public void Ctor_XamlMember_Object(XamlMember member, object value)
    {
        var e = new XamlSetValueEventArgs(member, value);
        Assert.Equal(member, e.Member);
        Assert.Equal(value, e.Value);
        Assert.False(e.Handled);
    }

    [Fact]
    public void CallBase_Invoke_Nop()
    {
        var e = new XamlSetValueEventArgs(null, null);
        e.CallBase();
        e.CallBase();
    }
}
