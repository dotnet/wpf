// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xaml;
using Xunit;

namespace System.Windows.Markup.Tests;

public class XamlSetMarkupExtensionEventArgsTests
{
    public static IEnumerable<object?[]> Ctor_XamlMember_MarkupExtension_IServiceProvider_TestData()
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        yield return new object?[] { null, null, null };
        yield return new object?[] { new XamlMember("name", type, false), new ArrayExtension(), new CustomServiceProvider() };
    }

    [Theory]
    [MemberData(nameof(Ctor_XamlMember_MarkupExtension_IServiceProvider_TestData))]
    public void Ctor_XamlMember_MarkupExtension_IServiceProvider(XamlMember member, MarkupExtension value, IServiceProvider serviceProvider)
    {
        var e = new XamlSetMarkupExtensionEventArgs(member, value, serviceProvider);
        Assert.Equal(member, e.Member);
        Assert.Equal(value, e.Value);
        Assert.Equal(value, e.MarkupExtension);
        Assert.Equal(serviceProvider, e.ServiceProvider);
        Assert.False(e.Handled);
    }

    [Fact]
    public void CallBase_Invoke_Nop()
    {
        var e = new XamlSetMarkupExtensionEventArgs(null, null, null);
        e.CallBase();
        e.CallBase();
    }

    private class CustomServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType) => throw new NotImplementedException();
    }
}
