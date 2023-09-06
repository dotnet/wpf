// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Xaml;
using Xunit;

namespace System.Windows.Markup.Tests;

public class XamlSetTypeConverterEventArgsTests
{
    public static IEnumerable<object?[]> Ctor_XamlMember_TypeConverter_Object_ITypeDescriptorContext_CultureInfo_TestData()
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        yield return new object?[] { null, null, null, null, null };
        yield return new object?[] { new XamlMember("name", type, false), new Int32Converter(), new object(), new CustomTypeDescriptorContext(), CultureInfo.InvariantCulture };
    }

    [Theory]
    [MemberData(nameof(Ctor_XamlMember_TypeConverter_Object_ITypeDescriptorContext_CultureInfo_TestData))]
    public void Ctor_XamlMember_TypeConverter_Object_ITypeDescriptorContext_CultureInfo(XamlMember member, TypeConverter typeConverter, object value, ITypeDescriptorContext serviceProvider, CultureInfo cultureInfo)
    {
        var e = new XamlSetTypeConverterEventArgs(member, typeConverter, value, serviceProvider, cultureInfo);
        Assert.Equal(member, e.Member);
        Assert.Equal(typeConverter, e.TypeConverter);
        Assert.Equal(value, e.Value);
        Assert.Equal(serviceProvider, e.ServiceProvider);
        Assert.Equal(cultureInfo, e.CultureInfo);
        Assert.False(e.Handled);
    }

    [Fact]
    public void CallBase_Invoke_Nop()
    {
        var e = new XamlSetTypeConverterEventArgs(null, null, null, null, null);
        e.CallBase();
        e.CallBase();
    }

    private class CustomTypeDescriptorContext : ITypeDescriptorContext
    {
        public IContainer Container => throw new NotImplementedException();

        public object Instance => throw new NotImplementedException();

        public PropertyDescriptor PropertyDescriptor => throw new NotImplementedException();

        public object GetService(Type serviceType) => throw new NotImplementedException();

        public void OnComponentChanged() => throw new NotImplementedException();

        public bool OnComponentChanging() => throw new NotImplementedException();
    }
}
