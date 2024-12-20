// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xunit;

namespace System.Xaml.Schema.Tests;

public class XamlTypeNameTests
{
    [Fact]
    public void Ctor_Default()
    {
        var typeName = new XamlTypeName();
        Assert.Null(typeName.Namespace);
        Assert.Null(typeName.Name);
        Assert.Empty(typeName.TypeArguments);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("xamlNamespace", "name")]
    public void Ctor_String_String(string? xamlNamespace, string? name)
    {
        var typeName = new XamlTypeName(xamlNamespace, name);
        Assert.Equal(xamlNamespace, typeName.Namespace);
        Assert.Equal(name, typeName.Name);
        Assert.Empty(typeName.TypeArguments);
    }

    public static IEnumerable<object?[]> Ctor_String_String_XamlTypeNames_TestData()
    {
        yield return new object?[] { null, null, null };
        yield return new object?[] { "", "", Array.Empty<XamlTypeName>() };
        yield return new object?[] { "xamlNamespace", "name", new XamlTypeName?[] { null, new XamlTypeName() } };
    }

    [Theory]
    [MemberData(nameof(Ctor_String_String_XamlTypeNames_TestData))]
    public void Ctor_String_String_XamlTypeNames(string xamlNamespace, string name, IEnumerable<XamlTypeName> typeArguments)
    {
        var typeName = new XamlTypeName(xamlNamespace, name, typeArguments);
        Assert.Equal(xamlNamespace, typeName.Namespace);
        Assert.Equal(name, typeName.Name);
        Assert.Equal(typeArguments ?? Array.Empty<XamlTypeName>(), typeName.TypeArguments);
    }

    [Fact]
    public void Ctor_NonGenericXamlType_Success()
    {
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", null, new XamlSchemaContext());
        var typeName = new XamlTypeName(type);
        Assert.Equal("unknownTypeNamespace", typeName.Namespace);
        Assert.Equal("unknownTypeName", typeName.Name);
        Assert.Empty(typeName.TypeArguments);
    }

    [Fact]
    public void Ctor_GenericXamlType_Success()
    {
        var typeArgument = new XamlType("typeNamespace", "typeName", null, new XamlSchemaContext());
        var type = new XamlType("unknownTypeNamespace", "unknownTypeName", new XamlType[] { typeArgument }, new XamlSchemaContext());
        var typeName = new XamlTypeName(type);
        Assert.Equal("unknownTypeNamespace", typeName.Namespace);
        Assert.Equal("unknownTypeName", typeName.Name);
        Assert.Equal("typeNamespace", Assert.Single(typeName.TypeArguments).Namespace);
        Assert.Equal("typeName", Assert.Single(typeName.TypeArguments).Name);
        Assert.Empty(Assert.Single(typeName.TypeArguments).TypeArguments);
    }

    [Fact]
    public void Ctor_NullType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("xamlType", () => new XamlTypeName(null));
    }

    public static IEnumerable<object?[]> ToString_TestData()
    {
        yield return new object?[] { new XamlTypeName("", "name"), null, "{}name" };
        yield return new object?[] { new XamlTypeName("namespace", "name"), null, "{namespace}name" };
        yield return new object?[] { new XamlTypeName("namespace", "name", new XamlTypeName[] { new XamlTypeName("typeNamespace", "typeName") }), null, "{namespace}name({typeNamespace}typeName)" };
        yield return new object?[] { new XamlTypeName("namespace", "name[", new XamlTypeName[] { new XamlTypeName("typeNamespace1", "typeName1"), new XamlTypeName("typeNamespace2", "typeName2") }), null, "{namespace}name({typeNamespace1}typeName1, {typeNamespace2}typeName2)[" };
        yield return new object?[] { new XamlTypeName("namespace", "name[value]", Array.Empty<XamlTypeName>()), null, "{namespace}name[value]" };
        
        yield return new object?[]
        {
            new XamlTypeName("namespace", "name"),
            new CustomNamespacePrefixLookup
            {
                LookupPrefixAction = ns =>
                {
                    Assert.Equal("namespace", ns);
                    return "prefix";
                }
            },
            "prefix:name"
        };
        yield return new object?[]
        {
            new XamlTypeName("namespace", "name"),
            new CustomNamespacePrefixLookup
            {
                LookupPrefixAction = ns =>
                {
                    Assert.Equal("namespace", ns);
                    return "";
                }
            },
            "name"
        };
        yield return new object?[]
        {
            new XamlTypeName("namespace", "name", new XamlTypeName[] { new XamlTypeName("typeNamespace", "typeName") }),
            new CustomNamespacePrefixLookup
            {
                LookupPrefixAction = ns =>
                {
                    if (ns == "namespace")
                    {
                        return "prefix";
                    }

                    Assert.Equal("typeNamespace", ns);
                    return "prefix";
                }
            },
            "prefix:name(prefix:typeName)"
        };
    }

    [Theory]
    [MemberData(nameof(ToString_TestData))]
    public void ToString_Invoke_ReturnsExpected(XamlTypeName name, INamespacePrefixLookup prefixLookup, string expected)
    {
        if (prefixLookup == null)
        {
            Assert.Equal(expected, name.ToString());
        }

        Assert.Equal(expected, name.ToString(prefixLookup));
    }

    [Fact]
    public void ToString_NullNamespace_ThrowsInvalidOperationException()
    {
        var typeName = new XamlTypeName(null, "name");
        Assert.Throws<InvalidOperationException>(() => typeName.ToString());
        Assert.Throws<InvalidOperationException>(() => typeName.ToString(null));
        Assert.Throws<InvalidOperationException>(() => typeName.ToString(new CustomNamespacePrefixLookup()));
        Assert.Throws<InvalidOperationException>(() => XamlTypeName.ToString(new XamlTypeName[] { typeName }, new CustomNamespacePrefixLookup()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ToString_NullName_ThrowsInvalidOperationException(string? name)
    {
        var typeName = new XamlTypeName("xamlNamespace", name);
        Assert.Throws<InvalidOperationException>(() => typeName.ToString());
        Assert.Throws<InvalidOperationException>(() => typeName.ToString(null));
        Assert.Throws<InvalidOperationException>(() => typeName.ToString(new CustomNamespacePrefixLookup()));
        Assert.Throws<InvalidOperationException>(() => XamlTypeName.ToString(new XamlTypeName[] { typeName }, new CustomNamespacePrefixLookup()));
    }

    [Fact]
    public void ToString_NullValueInTypeArguments_ThrowsNullReferenceException()
    {
        var prefixLookup = new CustomNamespacePrefixLookup
        {
            LookupPrefixAction = ns => "prefix"
        };
        var typeName = new XamlTypeName("namespace", "name", new XamlTypeName?[] { null });
        Assert.Throws<NullReferenceException>(() => typeName.ToString());
        Assert.Throws<NullReferenceException>(() => typeName.ToString(null));
        Assert.Throws<NullReferenceException>(() => typeName.ToString(prefixLookup));
        Assert.Throws<NullReferenceException>(() => XamlTypeName.ToString(new XamlTypeName[] { typeName }, prefixLookup));
    }

    [Fact]
    public void ToString_NullPrefixLookup_ThrowsInvalidOperationException()
    {
        var prefixLookup = new CustomNamespacePrefixLookup
        {
            LookupPrefixAction = ns => null!
        };
        var typeName = new XamlTypeName("namespace", "name", new XamlTypeName?[] { null });
        Assert.Throws<InvalidOperationException>(() => typeName.ToString(prefixLookup));
        Assert.Throws<InvalidOperationException>(() => XamlTypeName.ToString(new XamlTypeName[] { typeName }, prefixLookup));
    }

    public static IEnumerable<object[]> ToString_List_TestData()
    {
        yield return new object[] { Array.Empty<XamlTypeName>(), "" };
        yield return new object[] { new XamlTypeName[] { new XamlTypeName("namespace1", "name") }, "prefix1:name" };
        yield return new object[] { new XamlTypeName[] { new XamlTypeName("namespace1", "name1"), new XamlTypeName("namespace2", "name2") }, "prefix1:name1, prefix2:name2" };
    }

    [Theory]
    [MemberData(nameof(ToString_List_TestData))]
    public void ToString_ListInvoke_ReturnsExpected(IList<XamlTypeName> typeNameList, string expected)
    {
        var prefixLookup = new CustomNamespacePrefixLookup
        {
            LookupPrefixAction = ns =>
            {
                if (ns == "namespace1")
                {
                    return "prefix1";
                }

                Assert.Equal("namespace2", ns);
                return "prefix2";
            }
        };
        Assert.Equal(expected, XamlTypeName.ToString(typeNameList, prefixLookup));
    }

    [Fact]
    public void ToString_NullTypeNameList_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("typeNameList", () => XamlTypeName.ToString(null, new CustomNamespacePrefixLookup()));
    }

    [Fact]
    public void ToString_NullPrefixLookup_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("prefixLookup", () => XamlTypeName.ToString(Array.Empty<XamlTypeName>(), null));
    }

    public static IEnumerable<object[]> Parse_TestData()
    {
        yield return new object[] { "name", "", "name", Array.Empty<XamlTypeName>() };
        yield return new object[] { "  name  ", "", "name", Array.Empty<XamlTypeName>() };
        yield return new object[] { "_name", "", "_name", Array.Empty<XamlTypeName>() };
        yield return new object[] { "prefix:name", "prefix", "name", Array.Empty<XamlTypeName>() };
        yield return new object[] { "  prefix  :  name  ", "prefix", "name", Array.Empty<XamlTypeName>() };
        yield return new object[] { "_aA.1e\u0300\u0903:_bB1ee\u0300\u0903", "_aA.1e\u0300\u0903", "_bB1ee\u0300\u0903", Array.Empty<XamlTypeName>() };
        yield return new object[] { "prefix:name(prefix:typeName)", "prefix", "name", new XamlTypeName[] { new XamlTypeName("namespace", "typeName" ) } };
        yield return new object[] { "prefix:name(prefix:typeName1, prefix:typeName2)", "prefix", "name", new XamlTypeName[] { new XamlTypeName("namespace", "typeName1"), new XamlTypeName("namespace", "typeName2") } };
        yield return new object[] { "prefix:name(prefix:typeName1, prefix:typeName2)[]", "prefix", "name[]", new XamlTypeName[] { new XamlTypeName("namespace", "typeName1"), new XamlTypeName("namespace", "typeName2") } };
        yield return new object[] { "prefix:name(prefix:typeName1, prefix:typeName2)[,  ,]", "prefix", "name[,  ,]", new XamlTypeName[] { new XamlTypeName("namespace", "typeName1"), new XamlTypeName("namespace", "typeName2") } };
        yield return new object[] { "name(typeName1, typeName2)[,  ,]", "", "name[,  ,]", new XamlTypeName[] { new XamlTypeName("namespace", "typeName1"), new XamlTypeName("namespace", "typeName2") } };
        yield return new object[] { "name[,  ,]", "", "name[,  ,]", Array.Empty<XamlTypeName>() };
        yield return new object[] { "name[][]", "", "name[][]", Array.Empty<XamlTypeName>() };
    }

    [Theory]
    [MemberData(nameof(Parse_TestData))]
    public void Parse_TypeName_ReturnsExpected(string typeName, string expectedPrefix, string expectedName, XamlTypeName[] expectedTypeArguments)
    {
        var namespaceResolver = new CustomXamlNamespaceResolver
        {
            GetNamespaceAction = prefix =>
            {
                Assert.Equal(expectedPrefix, prefix);
                return "namespace";
            }
        };
        XamlTypeName name = XamlTypeName.Parse(typeName, namespaceResolver);
        Assert.Equal("namespace", name.Namespace);
        Assert.Equal(expectedName, name.Name);
        AssertEqualTypeNames(expectedTypeArguments, name.TypeArguments.ToArray());
    }

    [Theory]
    [MemberData(nameof(Parse_TestData))]
    public void TryParse_TypeName_ReturnsExpected(string typeName, string expectedPrefix, string expectedName, XamlTypeName[] expectedTypeArguments)
    {
        var namespaceResolver = new CustomXamlNamespaceResolver
        {
            GetNamespaceAction = prefix =>
            {
                Assert.Equal(expectedPrefix, prefix);
                return "namespace";
            }
        };
        Assert.True(XamlTypeName.TryParse(typeName, namespaceResolver, out XamlTypeName name));
        Assert.Equal("namespace", name.Namespace);
        Assert.Equal(expectedName, name.Name);
        AssertEqualTypeNames(expectedTypeArguments, name.TypeArguments.ToArray());
    }

    [Fact]
    public void Parse_TrivialTypeNameNullGetNamespaces_ThrowsFormatException()
    {
        var namespaceResolver = new CustomXamlNamespaceResolver
        {
            GetNamespaceAction = prefix => null!
        };
        Assert.Throws<FormatException>(() => XamlTypeName.Parse("name", namespaceResolver));

        XamlTypeName? result = null;
        Assert.False(XamlTypeName.TryParse("name", namespaceResolver, out result));
        Assert.Null(result);
    }

    [Fact]
    public void Parse_TrivialTypeNameEmptyGetNamespaces_ReturnsExpected()
    {
        var namespaceResolver = new CustomXamlNamespaceResolver
        {
            GetNamespaceAction = prefix => ""
        };
        XamlTypeName name = XamlTypeName.Parse("name", namespaceResolver);
        Assert.Equal("", name.Namespace);
        Assert.Equal("name", name.Name);
        Assert.Empty(name.TypeArguments);
    }

    [Fact]
    public void Parse_NullTypeName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("typeName", () => XamlTypeName.Parse(null, new CustomXamlNamespaceResolver()));

        XamlTypeName? result = null;
        Assert.Throws<ArgumentNullException>("typeName", () => XamlTypeName.TryParse(null, new CustomXamlNamespaceResolver(), out result));
        Assert.Null(result);
    }

    [Fact]
    public void Parse_NullNamespaceResolver_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("namespaceResolver", () => XamlTypeName.Parse("typeName", null));

        XamlTypeName? result = null;
        Assert.Throws<ArgumentNullException>("namespaceResolver", () => XamlTypeName.TryParse("typeName", null, out result));
        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("_#")]
    [InlineData("prefix:")]
    [InlineData("prefix:1")]
    [InlineData("prefix:_#")]
    [InlineData(":name")]
    [InlineData("1:name")]
    [InlineData("_#:name")]
    [InlineData("prefix:name[")]
    [InlineData("prefix:name(")]
    [InlineData("prefix:name(prefix:")]
    [InlineData("prefix:name(:typeName)")]
    [InlineData("prefix:name(prefix:typeName")]
    [InlineData("prefix:name(prefix:typeName,")]
    [InlineData("prefix:name(prefix:typeName, ")]
    [InlineData("prefix:name(prefix:typeName, prefix2:typeName")]
    [InlineData("(")]
    [InlineData(")")]
    [InlineData("[")]
    [InlineData("[,  ,]")]
    [InlineData("]")]
    [InlineData(",")]
    [InlineData("(prefix:typeName")]
    [InlineData("(prefix:typeName)")]
    [InlineData("prefix:name(prefix:typeName)[")]
    [InlineData("name(")]
    [InlineData("name(prefix:typeName")]
    [InlineData("name(prefix:1)")]
    [InlineData("name(prefix:typeName1,prefix:1)")]
    [InlineData("name(1:prefix)")]
    [InlineData("name(prefix:typeName1,1:typeName2)")]
    [InlineData("name(typeName)(typeName)")]
    [InlineData("name(typeName)name")]
    [InlineData("name()")]
    [InlineData("name[")]
    [InlineData("name[ ")]
    [InlineData("name[n]")]
    [InlineData("name[](typeName)")]
    [InlineData("name()()")]
    [InlineData("name[][]()()")]
    public void Parse_InvalidTypeName_ThrowsFormatException(string typeName)
    {
        var namespaceResolver = new CustomXamlNamespaceResolver
        {
            GetNamespaceAction = prefix => "namespace"
        };
        Assert.Throws<FormatException>(() => XamlTypeName.Parse(typeName, namespaceResolver));
        Assert.False(XamlTypeName.TryParse(typeName, namespaceResolver, out XamlTypeName result));
        Assert.Null(result);
    }

    public static IEnumerable<object[]> ParseList_TestData()
    {
        yield return new object[] { "name", new XamlTypeName[] { new XamlTypeName("namespace", "name") } };
        yield return new object[] { "name1, name2", new XamlTypeName[] { new XamlTypeName("namespace", "name1"), new XamlTypeName("namespace", "name2") } };
    }

    [Theory]
    [MemberData(nameof(ParseList_TestData))]
    public void ParseList_TypeNameList_ReturnsExpected(string typeNameList, XamlTypeName[] expected)
    {
        var namespaceResolver = new CustomXamlNamespaceResolver
        {
            GetNamespaceAction = prefix => "namespace"
        };
        XamlTypeName[] typeNames = XamlTypeName.ParseList(typeNameList, namespaceResolver).ToArray();
        AssertEqualTypeNames(expected, typeNames);
    }

    [Theory]
    [MemberData(nameof(ParseList_TestData))]
    public void TryParseList_TypeNameList_ReturnsExpected(string typeNameList, XamlTypeName[] expected)
    {
        var namespaceResolver = new CustomXamlNamespaceResolver
        {
            GetNamespaceAction = prefix => "namespace"
        };
        Assert.True(XamlTypeName.TryParseList(typeNameList, namespaceResolver, out IList<XamlTypeName> typeNames));
        AssertEqualTypeNames(expected, typeNames.ToArray());
    }

    [Theory]
    [InlineData("")]
    [InlineData(",")]
    [InlineData("name,")]
    [InlineData("1")]
    [InlineData("name,1")]
    [InlineData("name,   ")]
    [InlineData("name,name2()()")]
    [InlineData("name,name2[][]()()")]
    public void ParseList_InvalidTypeNameList_ThrowsFormatException(string typeNameList)
    {
        var namespaceResolver = new CustomXamlNamespaceResolver
        {
            GetNamespaceAction = prefix => "namespace"
        };
        Assert.Throws<FormatException>(() => XamlTypeName.ParseList(typeNameList, namespaceResolver));
        Assert.False(XamlTypeName.TryParseList(typeNameList, namespaceResolver, out IList<XamlTypeName> result));
        Assert.Null(result);
    }

    [Fact]
    public void ParseList_NullTypeNameList_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("typeNameList", () => XamlTypeName.ParseList(null, new CustomXamlNamespaceResolver()));

        IList<XamlTypeName>? result = null;
        Assert.Throws<ArgumentNullException>("typeNameList", () => XamlTypeName.TryParseList(null, new CustomXamlNamespaceResolver(), out result));
        Assert.Null(result);
    }

    [Fact]
    public void ParseList_NullNamespaceResolver_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("namespaceResolver", () => XamlTypeName.ParseList("typeNameList", null));

        IList<XamlTypeName>? result = null;
        Assert.Throws<ArgumentNullException>("namespaceResolver", () => XamlTypeName.TryParseList("typeNameList", null, out result));
        Assert.Null(result);
    }

    private void AssertEqualTypeNames(XamlTypeName[] expected, XamlTypeName[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i].Namespace, actual[i].Namespace);
            Assert.Equal(expected[i].Name, actual[i].Name);
            AssertEqualTypeNames(expected[i].TypeArguments.ToArray(), actual[i].TypeArguments.ToArray());
        }
    }
    
    private class CustomTypeDescriptorContext : ITypeDescriptorContext
    {
        public IContainer Container => throw new NotImplementedException();

        public object Instance => throw new NotImplementedException();

        public PropertyDescriptor PropertyDescriptor => throw new NotImplementedException();

        public object? GetService(Type serviceType) => throw new NotImplementedException();

        public void OnComponentChanged() => throw new NotImplementedException();

        public bool OnComponentChanging() => throw new NotImplementedException();
    }

    private class CustomNamespacePrefixLookup : INamespacePrefixLookup
    {
        public Func<string, string>? LookupPrefixAction { get; set; }

        public string LookupPrefix(string ns)
        {
            if (LookupPrefixAction is null)
            {
                throw new NotImplementedException();
            }

            return LookupPrefixAction(ns);
        }
    }

    private class CustomXamlNamespaceResolver : IXamlNamespaceResolver
    {
        public Func<string, string>? GetNamespaceAction { get; set; }

        public string GetNamespace(string prefix)
        {
            if (GetNamespaceAction is null)
            {
                throw new NotImplementedException();
            }

            return GetNamespaceAction(prefix);
        }

        public IEnumerable<NamespaceDeclaration> GetNamespacePrefixes() => throw new NotImplementedException();
    }
}
