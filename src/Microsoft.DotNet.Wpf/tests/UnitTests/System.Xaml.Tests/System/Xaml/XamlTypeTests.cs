// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows.Markup;
using System.Xaml.Schema;
using System.Xaml.Tests.Common;
using System.Xml.Serialization;
using Xunit;

namespace System.Xaml.Tests
{
public class XamlTypeTests
{
    public static IEnumerable<object?[]> Ctor_String_String_XamlTypeNames_XamlSchemaContext_TestData()
    {
        yield return new object?[] { "", "", null, new XamlSchemaContext() };
        yield return new object?[] { "unknownTypeNamespace", "unknownTypeName", Array.Empty<XamlType>(), new XamlSchemaContext() };
        yield return new object?[] { "unknownTypeNamespace", "unknownTypeName", new XamlType[] { new XamlType(typeof(int), new XamlSchemaContext()) }, new XamlSchemaContext() };
    }

    [Theory]
    [MemberData(nameof(Ctor_String_String_XamlTypeNames_XamlSchemaContext_TestData))]
    public void Ctor_String_String_XamlTypeNames_XamlSchemaContext(string unknownTypeNamespace, string unknownTypeName, IList<XamlType> typeArguments, XamlSchemaContext schemaContext)
    {
        var type = new XamlType(unknownTypeNamespace, unknownTypeName, typeArguments, schemaContext);
        Assert.Equal(new string[] { unknownTypeNamespace }, type.GetXamlNamespaces());
        Assert.Equal(unknownTypeName, type.Name);
        Assert.Equal(typeArguments != null && typeArguments.Count > 0 ? typeArguments : null, type.TypeArguments);
        Assert.Equal(schemaContext, type.SchemaContext);
        Assert.True(type.IsUnknown);
        Assert.Equal(XamlTypeInvoker.UnknownInvoker, type.Invoker);
    }

    [Fact]
    public void Ctor_NullUnknownTypeNamespace_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("unknownTypeNamespace", () => new XamlType(null, "unknownTypeName", Array.Empty<XamlType>(), new XamlSchemaContext()));
    }

    [Fact]
    public void Ctor_NullUnknownTypeName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("unknownTypeName", () => new XamlType("unknownTypeNamespace", null, Array.Empty<XamlType>(), new XamlSchemaContext()));
    }

    public static IEnumerable<object?[]> Ctor_String_XamlTypeNames_XamlSchemaContext_TestData()
    {
        yield return new object?[] { "", null, new XamlSchemaContext() };
    }

    [Theory]
    [MemberData(nameof(Ctor_String_XamlTypeNames_XamlSchemaContext_TestData))]
    public void Ctor_String_XamlTypeNames_XamlSchemaContext(string typeName, IList<XamlType?>? typeArguments, XamlSchemaContext schemaContext)
    {
        var type = new SubXamlType(typeName, typeArguments, schemaContext);
        Assert.Equal(new string[] { "" }, type.GetXamlNamespaces());
        Assert.Equal(typeName, type.Name);
        Assert.Equal(typeArguments != null && typeArguments.Count > 0 ? typeArguments : null, type.TypeArguments);
        Assert.Equal(schemaContext, type.SchemaContext);
        Assert.True(type.IsUnknown);
        Assert.Equal(XamlTypeInvoker.UnknownInvoker, type.Invoker);
    }

    [Fact]
    public void Ctor_NullTypeName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("typeName", () => new SubXamlType(null!, Array.Empty<XamlType>(), new XamlSchemaContext()));
    }

    public static IEnumerable<object?[]> Ctor_Type_XamlSchemaContext_TestData()
    {
        yield return new object?[] { typeof(int), new XamlSchemaContext(), "Int32", null };
        yield return new object?[] { typeof(NestedClass), new XamlSchemaContext(), "XamlTypeTests+NestedClass", null };
        yield return new object?[] { typeof(List<int>), new XamlSchemaContext(), "List", new XamlType[] { new XamlType(typeof(int), new XamlSchemaContext()) } };
        yield return new object?[] { typeof(List<int>[][]), new XamlSchemaContext(), "List[][]", new XamlType[] { new XamlType(typeof(int), new XamlSchemaContext()) } };
    }

    [Theory]
    [MemberData(nameof(Ctor_Type_XamlSchemaContext_TestData))]
    public void Ctor_Type_XamlSchemaContext(Type underlyingType, XamlSchemaContext schemaContext, string expectedName, XamlType[] expectedTypeArguments)
    {
        var type = new XamlType(underlyingType, schemaContext);
        Assert.Equal(expectedName, type.Name);
        Assert.Equal(expectedTypeArguments, type.TypeArguments);
        Assert.Equal(schemaContext, type.SchemaContext);
        Assert.False(type.IsUnknown);
        Assert.NotEqual(XamlTypeInvoker.UnknownInvoker, type.Invoker);
    }

    public static IEnumerable<object?[]> Ctor_Type_XamlSchemaContext_XamlTypeInvoker_TestData()
    {
        yield return new object?[] { typeof(int), new XamlSchemaContext(), null, "Int32", null };
        yield return new object?[] { typeof(NestedClass), new XamlSchemaContext(), new XamlTypeInvoker(new XamlType(typeof(int), new XamlSchemaContext())), "XamlTypeTests+NestedClass", null };
        yield return new object?[] { typeof(List<int>), new XamlSchemaContext(), XamlTypeInvoker.UnknownInvoker, "List", new XamlType[] { new XamlType(typeof(int), new XamlSchemaContext()) } };
        yield return new object?[] { typeof(List<int>[][]), new XamlSchemaContext(), null, "List[][]", new XamlType[] { new XamlType(typeof(int), new XamlSchemaContext()) } };
    }

    [Theory]
    [MemberData(nameof(Ctor_Type_XamlSchemaContext_XamlTypeInvoker_TestData))]
    public void Ctor_Type_XamlSchemaContext_XamlTypeInvoker(Type underlyingType, XamlSchemaContext schemaContext, XamlTypeInvoker invoker, string expectedName, XamlType[] expectedTypeArguments)
    {
        var type = new XamlType(underlyingType, schemaContext, invoker);
        Assert.Equal(expectedName, type.Name);
        Assert.Equal(expectedTypeArguments, type.TypeArguments);
        Assert.Equal(schemaContext, type.SchemaContext);
        Assert.False(type.IsUnknown);
        if (invoker == null)
        {
            Assert.NotNull(type.Invoker);
            Assert.NotEqual(XamlTypeInvoker.UnknownInvoker, type.Invoker);
        }
        else
        {
            Assert.Equal(invoker, type.Invoker);
        }
    }

    [Fact]
    public void Ctor_NullUnderlyingType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("underlyingType", () => new XamlType(null, new XamlSchemaContext()));
        Assert.Throws<ArgumentNullException>("underlyingType", () => new XamlType(null, new XamlSchemaContext(), XamlTypeInvoker.UnknownInvoker));
    }

    [Fact]
    public void Ctor_NullValueInTypeArguments_ThrowsArgumentException()
    {
        // TODO: paramName.
        Assert.Throws<ArgumentException>(() => new XamlType("unknownTypeNamespace", "unknownTypeName", new XamlType?[] { null }, new XamlSchemaContext()));
        Assert.Throws<ArgumentException>(() => new SubXamlType("typeName", new XamlType?[] { null }, new XamlSchemaContext()));
    }

    [Fact]
    public void Ctor_NullSchemaContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("schemaContext", () => new XamlType("unknownTypeNamespace", "unknownTypeName", Array.Empty<XamlType>(), null));
        Assert.Throws<ArgumentNullException>("schemaContext", () => new XamlType(typeof(int), null));
        Assert.Throws<ArgumentNullException>("schemaContext", () => new XamlType(typeof(int), null, XamlTypeInvoker.UnknownInvoker));
        Assert.Throws<ArgumentNullException>("schemaContext", () => new SubXamlType("typeName", Array.Empty<XamlType>(), null));
    }

    [Theory]
    [InlineData("name", true)]
    [InlineData("_aA1e\u0300\u0903", true)]
    [InlineData("", false)]
    [InlineData(" name  ", false)]
    [InlineData("1name", false)]
    [InlineData("n#me", false)]
    [InlineData("n.me", false)]
    public void IsNameValid_Invoke_ReturnsExpected(string name, bool expected)
    {
        var type = new XamlType("namespace", name, null, new XamlSchemaContext());
        Assert.Equal(expected, type.IsNameValid);
        Assert.Equal(type.IsNameValid, type.IsNameValid);
    }

    [Fact]
    public void PreferredXamlNamespace_Unknown_ReturnsExpected()
    {
        var type = new XamlType("namespace", "name", null, new XamlSchemaContext());
        Assert.Equal("namespace", type.PreferredXamlNamespace);
    }

    [Fact]
    public void PreferredXamlNamespace_UnderlyingType_ReturnsExpected()
    {
        var type = new XamlType(typeof(int), new XamlSchemaContext());
        Assert.Equal("http://schemas.microsoft.com/winfx/2006/xaml", type.PreferredXamlNamespace);
    }

    [Fact]
    public void PreferredXamlNamespace_GetXamlNamespacesReturnsNonEmpty_ReturnsFirstElement()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext()) { GetXamlNamespacesResult = new string[] { "namespace1", "namespace2" } };
        Assert.Equal("namespace1", type.PreferredXamlNamespace);
    }

    [Fact]
    public void PreferredXamlNamespace_GetXamlNamespacesReturnsEmpty_ReturnsNull()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext()) { GetXamlNamespacesResult = Array.Empty<string>() };
        Assert.Null(type.PreferredXamlNamespace);
    }

    [Fact]
    public void PreferredXamlNamespace_GetXamlNamespacesReturnsNull_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext()) { GetXamlNamespacesResult = null };
        Assert.Throws<NullReferenceException>(() => type.PreferredXamlNamespace);
    }

    public static IEnumerable<object?[]> GetXamlNamespaces_TestData()
    {
        const string Default = "http://schemas.microsoft.com/winfx/2006/xaml";
        
        static string Name(Assembly assembly) => new AssemblyName(assembly.FullName!).Name!;

        // Unknown.
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), new string[] { "namespace" } };
        yield return new object?[] { new SubXamlType("", "name", null, new XamlSchemaContext()), new string[] { "" } };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), new string[] { "" } };
        yield return new object?[] { new NoUnderlyingOrBaseType(), new string[] { "" } };

        // Clr Type.
        yield return new object?[]
        {
            new XamlType(typeof(int), new XamlSchemaContext()),
            new string[] { Default, $"clr-namespace:System;assembly={Name(typeof(int).Assembly)}" }
        };
        yield return new object?[]
        {
            new XamlType(typeof(int), new XamlSchemaContext(new XamlSchemaContextSettings
            {
                FullyQualifyAssemblyNamesInClrNamespaces = true
            })),
            new string[] { Default, $"clr-namespace:System;assembly={typeof(int).Assembly.FullName}" }
        };

        // Custom type.
        yield return new object?[]
        {
            new XamlType(typeof(GlobalNamespaceClass), new XamlSchemaContext()),
            new string[] { $"clr-namespace:;assembly={Name(typeof(GlobalNamespaceClass).Assembly)}" }
        };
        yield return new object?[]
        {
            new XamlType(typeof(GlobalNamespaceClass), new XamlSchemaContext(new XamlSchemaContextSettings
            {
                FullyQualifyAssemblyNamesInClrNamespaces = true
            })),
            new string[] { $"clr-namespace:;assembly={typeof(GlobalNamespaceClass).Assembly.FullName}" }
        };
        yield return new object?[]
        {
            new XamlType(typeof(XamlTypeTests), new XamlSchemaContext()),
            new string[] { $"clr-namespace:System.Xaml.Tests;assembly={Name(typeof(GlobalNamespaceClass).Assembly)}" }
        };
        yield return new object?[]
        {
            new XamlType(typeof(XamlTypeTests), new XamlSchemaContext(new XamlSchemaContextSettings
            {
                FullyQualifyAssemblyNamesInClrNamespaces = true
            })),
            new string[] { $"clr-namespace:System.Xaml.Tests;assembly={typeof(GlobalNamespaceClass).Assembly.FullName}" }
        };

        // Reflection Only Assembly.
        yield return new object?[]
        {
            new XamlType(new ReflectionOnlyType(typeof(XamlTypeTests)), new XamlSchemaContext()),
            new string[] { $"clr-namespace:System.Xaml.Tests;assembly={Name(typeof(XamlTypeTests).Assembly)}" }
        };
        yield return new object?[]
        {
            new XamlType(new CustomType(typeof(XamlTypeTests))
            {
                AssemblyResult = new CustomAssembly(typeof(XamlTypeTests).Assembly)
                {
                    ReflectionOnlyResult = true,
                    GetCustomAttributesDataResult = new CustomAttributeData[]
                    {
                        new CustomCustomAttributeData
                        {
                            ConstructorResult = typeof(XmlnsDefinitionAttribute).GetConstructors()[0],
                            ConstructorArgumentsResult = new CustomAttributeTypedArgument[]
                            {
                                new CustomAttributeTypedArgument(typeof(string), "xmlNamespace"),
                                new CustomAttributeTypedArgument(typeof(string), "System.Xaml.Tests")
                            }
                        }
                    }
                }
            }, new XamlSchemaContext()),
            new string[] { "xmlNamespace", $"clr-namespace:System.Xaml.Tests;assembly={Name(typeof(XamlTypeTests).Assembly)}" }
        };

        // Dynamic Assembly.
        AssemblyBuilder dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.Run);
        ModuleBuilder dynamicModule = dynamicAssembly.DefineDynamicModule("Name");
        TypeBuilder dynamicType = dynamicModule.DefineType("Type");
        Type createdDynamicType = dynamicType.CreateType();
        var dynamicCacheContext = new XamlSchemaContext();
        yield return new object?[]
        {
            new XamlType(createdDynamicType, dynamicCacheContext),
            new string[] { $"clr-namespace:;assembly=Name" }
        };
        yield return new object?[]
        {
            new XamlType(createdDynamicType, dynamicCacheContext),
            new string[] { $"clr-namespace:;assembly=Name" }
        };
    }

    [Theory]
    [MemberData(nameof(GetXamlNamespaces_TestData))]
    public void GetXamlNamespaces_Type_ReturnsExpected(XamlType type, string[] expected)
    {
        Assert.Equal(expected, type.GetXamlNamespaces());
    }

    public static IEnumerable<object[]> GetXamlNamespaces_InvalidXmlnsDefinitionAttribute_TestData()
    {
        yield return new object[]
        {
            new CustomAttributeTypedArgument[]
            {
                new CustomAttributeTypedArgument(typeof(string), null),
                new CustomAttributeTypedArgument(typeof(string), "clrNamespace")
            }
        };
        yield return new object[]
        {
            new CustomAttributeTypedArgument[]
            {
                new CustomAttributeTypedArgument(typeof(string), ""),
                new CustomAttributeTypedArgument(typeof(string), "clrNamespace")
            }
        };
        yield return new object[]
        {
            new CustomAttributeTypedArgument[]
            {
                new CustomAttributeTypedArgument(typeof(string), "xmlNamespace"),
                new CustomAttributeTypedArgument(typeof(string), null)
            }
        };
    }

    [Theory]
    [MemberData(nameof(GetXamlNamespaces_InvalidXmlnsDefinitionAttribute_TestData))]
    public void GetXamlNamespaces_InvalidXmlnsDefinitionAttribute_ThrowsXamlSchemaException(CustomAttributeTypedArgument[] arguments)
    {
        var type = new CustomXamlType(new CustomType(typeof(int))
        {
            AssemblyResult = new CustomAssembly(typeof(int).Assembly)
            {
                ReflectionOnlyResult = true,
                GetCustomAttributesDataResult = new CustomAttributeData[]
                {
                    new CustomCustomAttributeData
                    {
                        ConstructorResult = typeof(XmlnsDefinitionAttribute).GetConstructors()[0],
                        ConstructorArgumentsResult = arguments
                    }
                }
            }
        }, new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => type.GetXamlNamespaces());
    }

    [Fact]
    public void GetXamlNamespaces_NullAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(new CustomType(typeof(int))
        {
            AssemblyResult = new CustomAssembly(typeof(int).Assembly)
            {
                GetCustomAttributesMap = new Dictionary<Type, object?[]?>
                {
                    { typeof(XmlnsDefinitionAttribute), null }
                }
            }
        }, new XamlSchemaContext());
        Assert.Throws<NullReferenceException>(() => type.GetXamlNamespaces());
    }

    [Fact]
    public void GetXamlNamespaces_InvalidAttributeResultType_ThrowsInvalidCastException()
    {
        var type = new CustomXamlType(new CustomType(typeof(int))
        {
            AssemblyResult = new CustomAssembly(typeof(int).Assembly)
            {
                GetCustomAttributesMap = new Dictionary<Type, object?[]?>
                {
                    { typeof(XmlnsDefinitionAttribute), new object[] { new XmlnsDefinitionAttribute("xmlNamespace", "clrNamespace") } }
                }
            }
        }, new XamlSchemaContext());
        Assert.Throws<InvalidCastException>(() => type.GetXamlNamespaces());
    }

    [Fact]
    public void GetXamlNamespaces_NullItemInAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(new CustomType(typeof(int))
        {
            AssemblyResult = new CustomAssembly(typeof(int).Assembly)
            {
                GetCustomAttributesMap = new Dictionary<Type, object?[]?>
                {
                    { typeof(XmlnsDefinitionAttribute), new Attribute?[] { null } }
                }
            }
        }, new XamlSchemaContext());
        Assert.Throws<NullReferenceException>(() => type.GetXamlNamespaces());
    }

    [Fact]
    public void GetXamlNamespaces_InvalidTypeItemInAttributeResult_ThrowsInvalidCastException()
    {
        var type = new CustomXamlType(new CustomType(typeof(int))
        {
            AssemblyResult = new CustomAssembly(typeof(int).Assembly)
            {
                GetCustomAttributesMap = new Dictionary<Type, object?[]?>
                {
                    { typeof(XmlnsDefinitionAttribute), new Attribute[] { new AttributeUsageAttribute(AttributeTargets.All) } }
                }
            }
        }, new XamlSchemaContext());
        Assert.Throws<InvalidCastException>(() => type.GetXamlNamespaces());
    }

    public static IEnumerable<object?[]> LookupAliasedProperty_TestData()
    {
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), XamlLanguage.Key, null };
        yield return new object?[] { new SubXamlType(typeof(int), new XamlSchemaContext()), XamlLanguage.Key, null };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), XamlLanguage.Key, null };
        yield return new object?[] { new NoUnderlyingOrBaseType(), XamlLanguage.Key, null };

        // Key.
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new DictionaryKeyPropertyAttribute("name") }
                }
            },
            XamlLanguage.Key,
            new XamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false)
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithAliasedAttributes), new XamlSchemaContext()),
            XamlLanguage.Key,
            new XamlMember("keyName", new XamlType(typeof(ClassWithAliasedAttributes), new XamlSchemaContext()), false)
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(InheritedClassWithAliasedAttributes), new XamlSchemaContext()),
            XamlLanguage.Key,
            new XamlMember("keyName", new XamlType(typeof(InheritedClassWithAliasedAttributes), new XamlSchemaContext()), false)
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithNullAttributes), new XamlSchemaContext()),
            XamlLanguage.Key,
            null
        };

        // Name.
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new RuntimeNamePropertyAttribute("name") }
                }
            },
            XamlLanguage.Name,
            new XamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false)
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithAliasedAttributes), new XamlSchemaContext()),
            XamlLanguage.Name,
            new XamlMember("runtimeName", new XamlType(typeof(ClassWithAliasedAttributes), new XamlSchemaContext()), false)
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(InheritedClassWithAliasedAttributes), new XamlSchemaContext()),
            XamlLanguage.Name,
            new XamlMember("runtimeName", new XamlType(typeof(InheritedClassWithAliasedAttributes), new XamlSchemaContext()), false)
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithNullAttributes), new XamlSchemaContext()),
            XamlLanguage.Name,
            null
        };

        // Uid.
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new UidPropertyAttribute("name") }
                }
            },
            XamlLanguage.Uid,
            new XamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false)
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithAliasedAttributes), new XamlSchemaContext()),
            XamlLanguage.Uid,
            new XamlMember("uidName", new XamlType(typeof(ClassWithAliasedAttributes), new XamlSchemaContext()), false)
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(InheritedClassWithAliasedAttributes), new XamlSchemaContext()),
            XamlLanguage.Uid,
            new XamlMember("uidName", new XamlType(typeof(InheritedClassWithAliasedAttributes), new XamlSchemaContext()), false)
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithNullAttributes), new XamlSchemaContext()),
            XamlLanguage.Uid,
            null
        };

        // Lang.
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new XmlLangPropertyAttribute("name") }
                }
            },
            XamlLanguage.Lang,
            new XamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false)
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithAliasedAttributes), new XamlSchemaContext()),
            XamlLanguage.Lang,
            new XamlMember("langName", new XamlType(typeof(ClassWithAliasedAttributes), new XamlSchemaContext()), false)
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(InheritedClassWithAliasedAttributes), new XamlSchemaContext()),
            XamlLanguage.Lang,
            new XamlMember("langName", new XamlType(typeof(InheritedClassWithAliasedAttributes), new XamlSchemaContext()), false)
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithNullAttributes), new XamlSchemaContext()),
            XamlLanguage.Lang,
            null
        };

        // Other.
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => Array.Empty<object>()
                }
            },
            XamlLanguage.Lang,
            null
        };
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new XmlLangPropertyAttribute("name") }
                }
            },
            new XamlDirective("namespace", "name"),
            null
        };
        yield return new object?[]
        {
            new SubXamlType(new ReflectionOnlyCustomAttributeDataType(typeof(int)), new XamlSchemaContext()),
            XamlLanguage.Lang,
            null
        };
        yield return new object?[]
        {
            new SubXamlType(new ThrowsCustomAttributeFormatExceptionDelegator(typeof(int)), new XamlSchemaContext()),
            XamlLanguage.Lang,
            null
        };
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupBaseTypeResult = new XamlType("namespace", "name", null, new XamlSchemaContext())
            },
            XamlLanguage.Lang,
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupAliasedProperty_TestData))]
    public void LookupAliasedProperty_Type_ReturnsExpected(SubXamlType type, XamlDirective directive, XamlMember expected)
    {
        Assert.Equal(expected, type.LookupAliasedPropertyEntry(directive));
        XamlMember actual = type.GetAliasedProperty(directive);
        Assert.Equal(expected, actual);
        Assert.Same(actual, type.GetAliasedProperty(directive));
    }

    [Fact]
    public void LookupAliasedProperty_NullDirective_ThrowsArgumentNullException()
    {
        var type = new SubXamlType(typeof(int), new XamlSchemaContext());
        Assert.Throws<ArgumentNullException>("key", () => type.LookupAliasedPropertyEntry(null!));
        Assert.Throws<ArgumentNullException>("key", () => type.GetAliasedProperty(null));
    }

    public static IEnumerable<object[]> LookupAliasedProperty_Invalid_TestData()
    {
        yield return new object[] { XamlLanguage.Key };
        yield return new object[] { XamlLanguage.Name };
        yield return new object[] { XamlLanguage.Uid };
        yield return new object[] { XamlLanguage.Lang };
    }

    [Theory]
    [MemberData(nameof(LookupAliasedProperty_Invalid_TestData))]
    public void LookupAliasedProperty_NullAttributeResult_ThrowsNullReferenceException(XamlDirective directive)
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => null!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupAliasedPropertyEntry(directive));
        Assert.Throws<NullReferenceException>(() => type.GetAliasedProperty(directive));
    }

    [Theory]
    [MemberData(nameof(LookupAliasedProperty_Invalid_TestData))]
    public void LookupAliasedProperty_NullItemInAttributeResult_ThrowsNullReferenceException(XamlDirective directive)
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object?[] { null }!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupAliasedPropertyEntry(directive));
        Assert.Throws<NullReferenceException>(() => type.GetAliasedProperty(directive));
    }

    [Theory]
    [MemberData(nameof(LookupAliasedProperty_Invalid_TestData))]
    public void LookupAliasedProperty_InvalidTypeItemInAttributeResult_ThrowsInvalidCastException(XamlDirective directive)
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object[] { new object() }
            }
        };
        Assert.Throws<InvalidCastException>(() => type.LookupAliasedPropertyEntry(directive));
        Assert.Throws<InvalidCastException>(() => type.GetAliasedProperty(directive));
    }

    public static IEnumerable<object[]> LookupAliasedProperty_InvalidAttribute_TestData()
    {
        yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1), new CustomAttributeTypedArgument(typeof(int), 2) } };
        yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1) } };
    }

    [Theory]
    [MemberData(nameof(LookupAliasedProperty_InvalidAttribute_TestData))]
    public void LookupAliasedProperty_InvalidAttribute_ThrowsXamlSchemaException(CustomAttributeTypedArgument[] arguments)
    {
        var type = new SubXamlType(new CustomType(typeof(int))
        {
            GetCustomAttributesDataResult = new CustomAttributeData[]
            {
                new CustomCustomAttributeData
                {
                    ConstructorResult = typeof(XmlLangPropertyAttribute).GetConstructors()[0],
                    ConstructorArgumentsResult = arguments
                }
            }
        }, new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => type.LookupAliasedPropertyEntry(XamlLanguage.Lang));
        Assert.Throws<XamlSchemaException>(() => type.GetAliasedProperty(XamlLanguage.Lang));
    }

    private class InheritedClassWithAliasedAttributes : ClassWithAliasedAttributes
    {
    }

    [RuntimeNamePropertyAttribute("runtimeName")]
    [DictionaryKeyPropertyAttribute("keyName")]
    [UidPropertyAttribute("uidName")]
    [XmlLangPropertyAttribute("langName")]
    private class ClassWithAliasedAttributes
    {
    }

    [RuntimeNamePropertyAttribute(null!)]
    [DictionaryKeyPropertyAttribute(null!)]
    [UidPropertyAttribute(null!)]
    [XmlLangPropertyAttribute(null!)]
    private class ClassWithNullAttributes
    {
    }

    public static IEnumerable<object?[]> LookupAllAttachableMembers_TestData()
    {
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), Array.Empty<XamlMember>() };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), Array.Empty<XamlMember>() };
        yield return new object?[] { new SubXamlType(typeof(EmptyClass), new XamlSchemaContext()), Array.Empty<XamlMember>() };
        yield return new object?[] { new SubXamlType(typeof(object), new XamlSchemaContext()), Array.Empty<XamlMember>() };
        yield return new object?[] { new NoUnderlyingOrBaseType(), null };
    
        yield return new object?[]
        {
            new SubXamlType(typeof(AttachableMembersDataClass), new XamlSchemaContext()),
            new XamlMember[]
            {
                new XamlMember("", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.AddHandler)), new XamlSchemaContext()),
                new XamlMember("DifferentFirstParameter", null, typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetDifferentFirstParameter)), new XamlSchemaContext()),
                new XamlMember("DifferentFirstParameter", null, typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetDifferentFirstParameter)), new XamlSchemaContext()),
                new XamlMember("DifferentSecondParameter", null, typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetDifferentSecondParameter)), new XamlSchemaContext()),
                new XamlMember("DifferentSecondParameter", null, typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetDifferentSecondParameter)), new XamlSchemaContext()),
                new XamlMember("Event", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.AddEventHandler)), new XamlSchemaContext()),
                new XamlMember("GetOnlyDictionaryProperty", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetOnlyDictionaryProperty)), null, new XamlSchemaContext()),
                new XamlMember("GetOnlyIntProperty", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetOnlyIntProperty)), null, new XamlSchemaContext()),
                new XamlMember("GetOnlyListProperty", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetOnlyListProperty)), null, new XamlSchemaContext()),
                new XamlMember("GetOnlyXDataProperty", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetOnlyXDataProperty)), null, new XamlSchemaContext()),
                new XamlMember("GetSetProperty", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetSetProperty)), typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetGetSetProperty)), new XamlSchemaContext()),
                new XamlMember("GetSetProperty", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetSetProperty)), typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetGetSetProperty)), new XamlSchemaContext()),
                new XamlMember("InternalProperty", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetInternalProperty), BindingFlags.Static | BindingFlags.NonPublic), typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetInternalProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("InternalProperty", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetInternalProperty), BindingFlags.Static | BindingFlags.NonPublic), typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetInternalProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("ProtectedProperty", typeof(AttachableMembersDataClass).GetMethod("GetProtectedProperty", BindingFlags.Static | BindingFlags.NonPublic), typeof(AttachableMembersDataClass).GetMethod("SetProtectedProperty", BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("ProtectedProperty", typeof(AttachableMembersDataClass).GetMethod("GetProtectedProperty", BindingFlags.Static | BindingFlags.NonPublic), typeof(AttachableMembersDataClass).GetMethod("SetProtectedProperty", BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("SetOnlyProperty", null, typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetSetOnlyProperty)), new XamlSchemaContext()),
            },
            new XamlMember[]
            {
                new XamlMember("", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.AddHandler)), new XamlSchemaContext()),
                new XamlMember("DifferentFirstParameter", null, typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetDifferentFirstParameter)), new XamlSchemaContext()),
                new XamlMember("DifferentSecondParameter", null, typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetDifferentSecondParameter)), new XamlSchemaContext()),
                new XamlMember("Event", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.AddEventHandler)), new XamlSchemaContext()),
                new XamlMember("GetOnlyDictionaryProperty", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetOnlyDictionaryProperty)), null, new XamlSchemaContext()),
                new XamlMember("GetOnlyIntProperty", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetOnlyIntProperty)), null, new XamlSchemaContext()),
                new XamlMember("GetOnlyListProperty", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetOnlyListProperty)), null, new XamlSchemaContext()),
                new XamlMember("GetOnlyXDataProperty", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetOnlyXDataProperty)), null, new XamlSchemaContext()),
                new XamlMember("GetSetProperty", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetSetProperty)), typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetGetSetProperty)), new XamlSchemaContext()),
                new XamlMember("InternalProperty", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetInternalProperty), BindingFlags.Static | BindingFlags.NonPublic), typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetInternalProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("ProtectedProperty", typeof(AttachableMembersDataClass).GetMethod("GetProtectedProperty", BindingFlags.Static | BindingFlags.NonPublic), typeof(AttachableMembersDataClass).GetMethod("SetProtectedProperty", BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("SetOnlyProperty", null, typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetSetOnlyProperty)), new XamlSchemaContext())
            }
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(PrivateAttachableMembersDataClass), new XamlSchemaContext()),
            new XamlMember[]
            {
                new XamlMember("", typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.AddHandler)), new XamlSchemaContext()),
                new XamlMember("DifferentFirstParameter", null, typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetDifferentFirstParameter)), new XamlSchemaContext()),
                new XamlMember("DifferentFirstParameter", null, typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetDifferentFirstParameter)), new XamlSchemaContext()),
                new XamlMember("DifferentSecondParameter", null, typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetDifferentSecondParameter)), new XamlSchemaContext()),
                new XamlMember("DifferentSecondParameter", null, typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetDifferentSecondParameter)), new XamlSchemaContext()),
                new XamlMember("Event", typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.AddEventHandler)), new XamlSchemaContext()),
                new XamlMember("GetOnlyDictionaryProperty", typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetOnlyDictionaryProperty)), null, new XamlSchemaContext()),
                new XamlMember("GetOnlyIntProperty", typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetOnlyIntProperty)), null, new XamlSchemaContext()),
                new XamlMember("GetOnlyListProperty", typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetOnlyListProperty)), null, new XamlSchemaContext()),
                new XamlMember("GetOnlyXDataProperty", typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetOnlyXDataProperty)), null, new XamlSchemaContext()),
                new XamlMember("GetSetProperty", typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetSetProperty)), typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetGetSetProperty)), new XamlSchemaContext()),
                new XamlMember("GetSetProperty", typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetSetProperty)), typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetGetSetProperty)), new XamlSchemaContext()),
                new XamlMember("InternalProperty", typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetInternalProperty), BindingFlags.Static | BindingFlags.NonPublic), typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetInternalProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("InternalProperty", typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetInternalProperty), BindingFlags.Static | BindingFlags.NonPublic), typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetInternalProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("ProtectedProperty", typeof(PrivateAttachableMembersDataClass).GetMethod("GetProtectedProperty", BindingFlags.Static | BindingFlags.NonPublic), typeof(PrivateAttachableMembersDataClass).GetMethod("SetProtectedProperty", BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("ProtectedProperty", typeof(PrivateAttachableMembersDataClass).GetMethod("GetProtectedProperty", BindingFlags.Static | BindingFlags.NonPublic), typeof(PrivateAttachableMembersDataClass).GetMethod("SetProtectedProperty", BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("SetOnlyProperty", null, typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetSetOnlyProperty)), new XamlSchemaContext()),
            },
            new XamlMember[]
            {
                new XamlMember("", typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.AddHandler)), new XamlSchemaContext()),
                new XamlMember("DifferentFirstParameter", null, typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetDifferentFirstParameter)), new XamlSchemaContext()),
                new XamlMember("DifferentSecondParameter", null, typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetDifferentSecondParameter)), new XamlSchemaContext()),
                new XamlMember("Event", typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.AddEventHandler)), new XamlSchemaContext()),
                new XamlMember("GetOnlyDictionaryProperty", typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetOnlyDictionaryProperty)), null, new XamlSchemaContext()),
                new XamlMember("GetOnlyIntProperty", typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetOnlyIntProperty)), null, new XamlSchemaContext()),
                new XamlMember("GetOnlyListProperty", typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetOnlyListProperty)), null, new XamlSchemaContext()),
                new XamlMember("GetOnlyXDataProperty", typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetOnlyXDataProperty)), null, new XamlSchemaContext()),
                new XamlMember("GetSetProperty", typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetSetProperty)), typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetGetSetProperty)), new XamlSchemaContext()),
                new XamlMember("InternalProperty", typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetInternalProperty), BindingFlags.Static | BindingFlags.NonPublic), typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetInternalProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("ProtectedProperty", typeof(PrivateAttachableMembersDataClass).GetMethod("GetProtectedProperty", BindingFlags.Static | BindingFlags.NonPublic), typeof(PrivateAttachableMembersDataClass).GetMethod("SetProtectedProperty", BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("SetOnlyProperty", null, typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetSetOnlyProperty)), new XamlSchemaContext())
            }
        };

        yield return new object?[]
        {
            new SubXamlType(typeof(DuplicateDataClass), new XamlSchemaContext()),
            new XamlMember[]
            {
                new XamlMember("Event", typeof(DuplicateDataClass).GetMethod(nameof(DuplicateDataClass.AddEventHandler), new Type[] { typeof(object), typeof(EventHandler) }), new XamlSchemaContext()),
                new XamlMember("Getter", typeof(DuplicateDataClass).GetMethod(nameof(DuplicateDataClass.GetGetter), new Type[] { typeof(string) }), null, new XamlSchemaContext()),
                new XamlMember("InternalThenInternal", typeof(DuplicateDataClass).GetMethod(nameof(DuplicateDataClass.GetInternalThenInternal), BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(string) }, null), null, new XamlSchemaContext()),
                new XamlMember("InternalThenInternalEvent", typeof(DuplicateDataClass).GetMethod(nameof(DuplicateDataClass.AddInternalThenInternalEventHandler), BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(object), typeof(EventHandler) }, null), new XamlSchemaContext()),
                new XamlMember("InternalThenPublic", typeof(DuplicateDataClass).GetMethod(nameof(DuplicateDataClass.GetInternalThenPublic), new Type[] { typeof(int) }), null, new XamlSchemaContext()),
                new XamlMember("InternalThenPublicEvent", typeof(DuplicateDataClass).GetMethod(nameof(DuplicateDataClass.AddInternalThenPublicEventHandler), new Type[] { typeof(object), typeof(OtherHandler) }), new XamlSchemaContext()),
                new XamlMember("PublicThenInternal", typeof(DuplicateDataClass).GetMethod(nameof(DuplicateDataClass.GetPublicThenInternal), new Type[] { typeof(string) }), null, new XamlSchemaContext()),
                new XamlMember("PublicThenInternalEvent", typeof(DuplicateDataClass).GetMethod(nameof(DuplicateDataClass.AddPublicThenInternalEventHandler), new Type[] { typeof(object), typeof(EventHandler) }), new XamlSchemaContext()),
                new XamlMember("Setter", null, typeof(DuplicateDataClass).GetMethod(nameof(DuplicateDataClass.SetSetter), new Type[] { typeof(string), typeof(int) }), new XamlSchemaContext()),
            }
        };

        yield return new object?[]
        {
            new SubXamlType(typeof(PrivateDuplicateDataClass), new XamlSchemaContext()),
            new XamlMember[]
            {
                new XamlMember("Event", typeof(PrivateDuplicateDataClass).GetMethod(nameof(DuplicateDataClass.AddEventHandler), new Type[] { typeof(object), typeof(EventHandler) }), new XamlSchemaContext()),
                new XamlMember("Getter", typeof(PrivateDuplicateDataClass).GetMethod(nameof(DuplicateDataClass.GetGetter), new Type[] { typeof(string) }), null, new XamlSchemaContext()),
                new XamlMember("InternalThenInternal", typeof(PrivateDuplicateDataClass).GetMethod(nameof(DuplicateDataClass.GetInternalThenInternal), BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(string) }, null), null, new XamlSchemaContext()),
                new XamlMember("InternalThenInternalEvent", typeof(PrivateDuplicateDataClass).GetMethod(nameof(DuplicateDataClass.AddInternalThenInternalEventHandler), BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(object), typeof(EventHandler) }, null), new XamlSchemaContext()),
                new XamlMember("InternalThenPublic", typeof(PrivateDuplicateDataClass).GetMethod(nameof(DuplicateDataClass.GetInternalThenPublic), BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(string) }, null), null, new XamlSchemaContext()),
                new XamlMember("InternalThenPublicEvent", typeof(PrivateDuplicateDataClass).GetMethod(nameof(DuplicateDataClass.AddInternalThenPublicEventHandler), BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(object), typeof(EventHandler) }, null), new XamlSchemaContext()),
                new XamlMember("PublicThenInternal", typeof(PrivateDuplicateDataClass).GetMethod(nameof(DuplicateDataClass.GetPublicThenInternal), new Type[] { typeof(string) }), null, new XamlSchemaContext()),
                new XamlMember("PublicThenInternalEvent", typeof(PrivateDuplicateDataClass).GetMethod(nameof(DuplicateDataClass.AddPublicThenInternalEventHandler), new Type[] { typeof(object), typeof(EventHandler) }), new XamlSchemaContext()),
                new XamlMember("Setter", null, typeof(PrivateDuplicateDataClass).GetMethod(nameof(DuplicateDataClass.SetSetter), new Type[] { typeof(string), typeof(int) }), new XamlSchemaContext()),
            }
        };

        yield return new object?[]
        {
            new SubXamlType(typeof(NonMatchingInternalSetterClass), new XamlSchemaContext()),
            new XamlMember[]
            {
                new XamlMember("DictionaryProperty", typeof(NonMatchingInternalSetterClass).GetMethod(nameof(NonMatchingInternalSetterClass.GetDictionaryProperty)), null, new XamlSchemaContext()),
                new XamlMember("DictionaryProperty", typeof(NonMatchingInternalSetterClass).GetMethod(nameof(NonMatchingInternalSetterClass.GetDictionaryProperty)), null, new XamlSchemaContext()),
                new XamlMember("ListProperty", typeof(NonMatchingInternalSetterClass).GetMethod(nameof(NonMatchingInternalSetterClass.GetListProperty)), null, new XamlSchemaContext()),
                new XamlMember("ListProperty", typeof(NonMatchingInternalSetterClass).GetMethod(nameof(NonMatchingInternalSetterClass.GetListProperty)), null, new XamlSchemaContext()),
                new XamlMember("XDataProperty", typeof(NonMatchingInternalSetterClass).GetMethod(nameof(NonMatchingInternalSetterClass.GetXDataProperty)), null, new XamlSchemaContext()),
                new XamlMember("XDataProperty", typeof(NonMatchingInternalSetterClass).GetMethod(nameof(NonMatchingInternalSetterClass.GetXDataProperty)), null, new XamlSchemaContext())
            },
            new XamlMember[]
            {
                new XamlMember("DictionaryProperty", typeof(NonMatchingInternalSetterClass).GetMethod(nameof(NonMatchingInternalSetterClass.GetDictionaryProperty)), null, new XamlSchemaContext()),
                new XamlMember("ListProperty", typeof(NonMatchingInternalSetterClass).GetMethod(nameof(NonMatchingInternalSetterClass.GetListProperty)), null, new XamlSchemaContext()),
                new XamlMember("XDataProperty", typeof(NonMatchingInternalSetterClass).GetMethod(nameof(NonMatchingInternalSetterClass.GetXDataProperty)), null, new XamlSchemaContext())
            }
        };

        yield return new object?[]
        {
            new SubXamlType(typeof(PrivateNonMatchingInternalSetterClass), new XamlSchemaContext()),
            new XamlMember[]
            {
                new XamlMember("DictionaryProperty", null, typeof(PrivateNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetDictionaryProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("DictionaryProperty", null, typeof(PrivateNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetDictionaryProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("IntProperty", null, typeof(PrivateNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetIntProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("IntProperty", null, typeof(PrivateNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetIntProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("ListProperty", null, typeof(PrivateNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetListProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("ListProperty", null, typeof(PrivateNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetListProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("XDataProperty", null, typeof(PrivateNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetXDataProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("XDataProperty", null, typeof(PrivateNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetXDataProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext())
            },
            new XamlMember[]
            {
                new XamlMember("DictionaryProperty", null, typeof(PrivateNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetDictionaryProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("IntProperty", null, typeof(PrivateNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetIntProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("ListProperty", null, typeof(PrivateNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetListProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("XDataProperty", null, typeof(PrivateNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetXDataProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext())
            }
        };

        yield return new object?[]
        {
            new SubXamlType(typeof(InternalGetterNonMatchingInternalSetterClass), new XamlSchemaContext()),
            new XamlMember[]
            {
                new XamlMember("DictionaryProperty", null, typeof(InternalGetterNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetDictionaryProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("DictionaryProperty", null, typeof(InternalGetterNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetDictionaryProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("IntProperty", null, typeof(InternalGetterNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetIntProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("IntProperty", null, typeof(InternalGetterNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetIntProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("ListProperty", null, typeof(InternalGetterNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetListProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("ListProperty", null, typeof(InternalGetterNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetListProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("XDataProperty", null, typeof(InternalGetterNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetXDataProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("XDataProperty", null, typeof(InternalGetterNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetXDataProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext())
            },
            new XamlMember[]
            {
                new XamlMember("DictionaryProperty", null, typeof(InternalGetterNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetDictionaryProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("IntProperty", null, typeof(InternalGetterNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetIntProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("ListProperty", null, typeof(InternalGetterNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetListProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember("XDataProperty", null, typeof(InternalGetterNonMatchingInternalSetterClass).GetMethod(nameof(PrivateNonMatchingInternalSetterClass.SetXDataProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext())
            }
        };

        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupAllAttachableMembersResult = null
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupAllAttachableMembers_TestData))]
    public void LookupAllAttachableMembers_Invoke_ReturnsExpected(SubXamlType type, XamlMember[]? expectedLookup, XamlMember[]? expectedGet = null)
    {
        AssertEqualXamlMembers(expectedGet ?? expectedLookup ?? Array.Empty<XamlMember>(), type.GetAllAttachableMembers());
        AssertEqualXamlMembers(expectedLookup, type.LookupAllAttachableMembersEntry());
    }

#if !DEBUG // This triggers a Debug.Assert.
    [Fact]
    public void LookupAllAttachableMembers_MembersNotCalled_ThrowsNullReferenceException()
    {
        var type = new SubXamlType(typeof(string), new XamlSchemaContext());
        Assert.Throws<NullReferenceException>(() => type.LookupAllAttachableMembersEntry());
    }
#endif
    
    private static MethodInfo GetUnderlyingGetter(XamlMember member)
    {
        MethodInfo underlyingGetter = member.GetType().GetMethod("LookupUnderlyingGetter", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (MethodInfo)underlyingGetter.Invoke(member, Array.Empty<object>())!;
    }
    
    private static MethodInfo GetUnderlyingSetter(XamlMember member)
    {
        MethodInfo underlyingSetter = member.GetType().GetMethod("LookupUnderlyingSetter", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (MethodInfo)underlyingSetter.Invoke(member, Array.Empty<object>())!;
    }

#pragma warning disable IDE0060, IDE0051, CA1052 // Remove unused parameter, Remove unused member, Static holder types should be Static or NotInheritable
    public class AttachableMembersDataClass
    {
        public static int GetGetSetProperty(string value) => 0;
        public static void SetGetSetProperty(string value1, int value2) { }

        public static int GetDifferentFirstParameter(string value) => 0;
        public static void SetDifferentFirstParameter(int value1, int value2) { }

        public static int GetDifferentSecondParameter(string value) => 0;
        public static void SetDifferentSecondParameter(string value1, string value2) { }

        protected static int GetProtectedProperty(string value) => 0;
        protected static void SetProtectedProperty(string value1, int value2) { }

        internal static int GetInternalProperty(string value) => 0;
        internal static void SetInternalProperty(string value1, int value2) { }

        private static int GetPrivateProperty(string value) => 0;
        private static void SetPrivateProperty(string value1, int value2) { }

        public static int GetGetOnlyIntProperty(int value) => 0;
        public static List<int>? GetGetOnlyListProperty(int value) => null;
        public static Dictionary<int, string>? GetGetOnlyDictionaryProperty(int value) => null;
        public static IXmlSerializable? GetGetOnlyXDataProperty(int value) => null;

        public static void SetSetOnlyProperty(int value1, int value2) { }

        public static void GetVoid(string value) { }
        public static int GetParameterless() => 0;
        public static int GetTooManyParameters(string value1, int value2) => 0;

        public static void SetParameterless() { }
        public static void SetTooManyParameters() { }

        public static void AddEventHandler(object value, EventHandler handler) { }
        private static void AddPrivateEventHandler(object value, EventHandler handler) { }
        public static void AddHandler(object value, EventHandler handler) { }
        public static void AddParameterlessHandler() { }
        public static void AddTooManyParametersHandler(object value1, EventHandler handler, int value2) { }
        public static void AddInvalidSecondParameterHandler(object value1, object value2) { }

        public static void AddDoesNotEndCorrectly() { }
        public static void DoesNotStartWithGetSetAdd() { }
    }

    private class PrivateAttachableMembersDataClass
    {
        public static int GetGetSetProperty(string value) => 0;
        public static void SetGetSetProperty(string value1, int value2) { }

        public static int GetDifferentFirstParameter(string value) => 0;
        public static void SetDifferentFirstParameter(int value1, int value2) { }

        public static int GetDifferentSecondParameter(string value) => 0;
        public static void SetDifferentSecondParameter(string value1, string value2) { }

        protected static int GetProtectedProperty(string value) => 0;
        protected static void SetProtectedProperty(string value1, int value2) { }

        internal static int GetInternalProperty(string value) => 0;
        internal static void SetInternalProperty(string value1, int value2) { }

        private static int GetPrivateProperty(string value) => 0;
        private static void SetPrivateProperty(string value1, int value2) { }

        public static int GetGetOnlyIntProperty(int value) => 0;
        public static List<int>? GetGetOnlyListProperty(int value) => null;
        public static Dictionary<int, string>? GetGetOnlyDictionaryProperty(int value) => null;
        public static IXmlSerializable? GetGetOnlyXDataProperty(int value) => null;

        public static void SetSetOnlyProperty(int value1, int value2) { }

        public static void GetVoid(string value) { }
        public static int GetParameterless() => 0;
        public static int GetTooManyParameters(string value1, int value2) => 0;

        public static void SetParameterless() { }
        public static void SetTooManyParameters() { }

        public static void AddEventHandler(object value, EventHandler handler) { }
        private static void AddPrivateEventHandler(object value, EventHandler handler) { }
        public static void AddHandler(object value, EventHandler handler) { }
        public static void AddParameterlessHandler() { }
        public static void AddTooManyParametersHandler(object value1, EventHandler handler, int value2) { }
        public static void AddInvalidSecondParameterHandler(object value1, object value2) { }

        public static void AddDoesNotEndCorrectly() { }
        public static void DoesNotStartWithGetSetAdd() { }
    }

    public class DuplicateDataClass
    {
        public static int GetGetter(string value) => 0;
        public static int GetGetter(int value) => 0;

        public static int SetSetter(string value1, int value2) => 0;
        public static int SetSetter(int value1, int value2) => 0;

        public static int GetPublicThenInternal(string value) => 0;
        internal static int GetPublicThenInternal(int value) => 0;

        internal static int GetInternalThenPublic(string value) => 0;
        public static int GetInternalThenPublic(int value) => 0;

        internal static int GetInternalThenInternal(string value) => 0;
        internal static int GetInternalThenInternal(int value) => 0;

        public static void AddEventHandler(object value, EventHandler handler) { }
        public static void AddEventHandler(object value, OtherHandler handler) { }

        public static int AddPublicThenInternalEventHandler(object value, EventHandler handler) => 0;
        internal static int AddPublicThenInternalEventHandler(object value, OtherHandler handler) => 0;

        internal static int AddInternalThenPublicEventHandler(object value, EventHandler handler) => 0;
        public static int AddInternalThenPublicEventHandler(object value, OtherHandler handler) => 0;

        internal static int AddInternalThenInternalEventHandler(object value, EventHandler handler) => 0;
        internal static int AddInternalThenInternalEventHandler(object value, OtherHandler handler) => 0;
    }

    private class PrivateDuplicateDataClass
    {
        public static int GetGetter(string value) => 0;
        public static int GetGetter(int value) => 0;

        public static int SetSetter(string value1, int value2) => 0;
        public static int SetSetter(int value1, int value2) => 0;

        public static int GetPublicThenInternal(string value) => 0;
        internal static int GetPublicThenInternal(int value) => 0;

        internal static int GetInternalThenPublic(string value) => 0;
        public static int GetInternalThenPublic(int value) => 0;

        internal static int GetInternalThenInternal(string value) => 0;
        internal static int GetInternalThenInternal(int value) => 0;

        public static void AddEventHandler(object value, EventHandler handler) { }
        public static void AddEventHandler(object value, OtherHandler handler) { }

        public static int AddPublicThenInternalEventHandler(object value, EventHandler handler) => 0;
        internal static int AddPublicThenInternalEventHandler(object value, OtherHandler handler) => 0;

        internal static int AddInternalThenPublicEventHandler(object value, EventHandler handler) => 0;
        public static int AddInternalThenPublicEventHandler(object value, OtherHandler handler) => 0;

        internal static int AddInternalThenInternalEventHandler(object value, EventHandler handler) => 0;
        internal static int AddInternalThenInternalEventHandler(object value, OtherHandler handler) => 0;
    }

    public class NonMatchingInternalSetterClass
    {
        public static int GetIntProperty(string value1) => 0;
        internal static int SetIntProperty(string value1, string value2) => 0;

        public static List<int>? GetListProperty(string value1) => null;
        internal static int SetListProperty(string value1, string value2) => 0;

        public static Dictionary<int, string>? GetDictionaryProperty(string value1) => null;
        internal static int SetDictionaryProperty(string value1, string value2) => 0;

        public static IXmlSerializable? GetXDataProperty(string value1) => null;
        internal static int SetXDataProperty(string value1, string value2) => 0;
    }

    public class InternalGetterNonMatchingInternalSetterClass
    {
        internal static int GetIntProperty(string value1) => 0;
        internal static int SetIntProperty(string value1, string value2) => 0;

        internal static List<int>? GetListProperty(string value1) => null;
        internal static int SetListProperty(string value1, string value2) => 0;

        internal static Dictionary<int, string>? GetDictionaryProperty(string value1) => null;
        internal static int SetDictionaryProperty(string value1, string value2) => 0;

        internal static IXmlSerializable? GetXDataProperty(string value1) => null;
        internal static int SetXDataProperty(string value1, string value2) => 0;
    }

    private class PrivateNonMatchingInternalSetterClass
    {
        public static int GetIntProperty(string value1) => 0;
        internal static int SetIntProperty(string value1, string value2) => 0;

        public static List<int>? GetListProperty(string value1) => null;
        internal static int SetListProperty(string value1, string value2) => 0;

        public static Dictionary<int, string>? GetDictionaryProperty(string value1) => null;
        internal static int SetDictionaryProperty(string value1, string value2) => 0;

        public static IXmlSerializable? GetXDataProperty(string value1) => null;
        internal static int SetXDataProperty(string value1, string value2) => 0;
    }

    private class EmptyClass
    {
    }
#pragma warning restore IDE0060, IDE0051, CA1052 // Remove unused parameter, Remove unused member, Static holder types should be Static or NotInheritable

    public static IEnumerable<object?[]> LookupAllMembers_TestData()
    {
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), Array.Empty<XamlMember>() };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), Array.Empty<XamlMember>() };
        yield return new object?[] { new SubXamlType(typeof(int), new XamlSchemaContext()), Array.Empty<XamlMember>() };
        yield return new object?[] { new SubXamlType(typeof(object), new XamlSchemaContext()), Array.Empty<XamlMember>() };
        yield return new object?[] { new NoUnderlyingOrBaseType(), null };

        yield return new object?[]
        {
            new SubXamlType(typeof(MembersDataClass), new XamlSchemaContext()),
            new XamlMember[]
            {
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.Property)), new XamlSchemaContext()),
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.SetOnlyProperty)), new XamlSchemaContext()),
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.PrivateGetProperty)), new XamlSchemaContext()),
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.PrivateSetProperty)), new XamlSchemaContext()),
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.GetOnlyListProperty)), new XamlSchemaContext()),
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.GetOnlyDictionaryProperty)), new XamlSchemaContext()),
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.GetXDataProperty)), new XamlSchemaContext()),
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.InternalProperty), BindingFlags.Instance | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember(typeof(MembersDataClass).GetProperty("ProtectedProperty", BindingFlags.Instance | BindingFlags.NonPublic), new XamlSchemaContext()),
                new XamlMember(typeof(MembersDataClass).GetEvent(nameof(MembersDataClass.Event)), new XamlSchemaContext())
            }
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(MoreDerivedShadowedDataClass), new XamlSchemaContext()),
            new XamlMember[]
            {
                new XamlMember(typeof(MoreDerivedShadowedDataClass).GetProperty(nameof(MoreDerivedShadowedDataClass.Property), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), new XamlSchemaContext()),
                new XamlMember(typeof(MoreDerivedShadowedDataClass).GetProperty(nameof(MoreDerivedShadowedDataClass.Property), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), new XamlSchemaContext()),
                new XamlMember(typeof(MoreDerivedShadowedDataClass).GetEvent(nameof(MoreDerivedShadowedDataClass.Event), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), new XamlSchemaContext())
            },
            new XamlMember[]
            {
                new XamlMember(typeof(MoreDerivedShadowedDataClass).GetProperty(nameof(MoreDerivedShadowedDataClass.Property), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), new XamlSchemaContext()),
                new XamlMember(typeof(MoreDerivedShadowedDataClass).GetEvent(nameof(MoreDerivedShadowedDataClass.Event), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), new XamlSchemaContext())
            },
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(PrivateMembersDataClass), new XamlSchemaContext()),
            Array.Empty<XamlMember>()
        };

        yield return new object?[]
        {
            new SubXamlType(new CustomType(typeof(MembersDataClass))
            {
                GetPropertiesResult = new PropertyInfo[]
                {
                    typeof(ShadowedBaseClass).GetProperty(nameof(ShadowedBaseClass.Property), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!,
                    typeof(MoreDerivedShadowedDataClass).GetProperty(nameof(MoreDerivedShadowedDataClass.Property), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!
                },
                GetEventsResult = new EventInfo[]
                {
                    typeof(ShadowedBaseClass).GetEvent(nameof(ShadowedBaseClass.Event), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!,
                    typeof(MoreDerivedShadowedDataClass).GetEvent(nameof(MoreDerivedShadowedDataClass.Event), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!
                }
            }, new XamlSchemaContext()),
            new XamlMember[]
            {
                new XamlMember(typeof(MoreDerivedShadowedDataClass).GetProperty(nameof(MoreDerivedShadowedDataClass.Property), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), new XamlSchemaContext()),
                new XamlMember(typeof(MoreDerivedShadowedDataClass).GetProperty(nameof(MoreDerivedShadowedDataClass.Property), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), new XamlSchemaContext()),
                new XamlMember(typeof(MoreDerivedShadowedDataClass).GetEvent(nameof(MoreDerivedShadowedDataClass.Event), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), new XamlSchemaContext()),
                new XamlMember(typeof(MoreDerivedShadowedDataClass).GetEvent(nameof(MoreDerivedShadowedDataClass.Event), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), new XamlSchemaContext())
            },
            new XamlMember[]
            {
                new XamlMember(typeof(MoreDerivedShadowedDataClass).GetProperty(nameof(MoreDerivedShadowedDataClass.Property), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), new XamlSchemaContext()),
                new XamlMember(typeof(MoreDerivedShadowedDataClass).GetEvent(nameof(MoreDerivedShadowedDataClass.Event), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), new XamlSchemaContext())
            }
        };

        yield return new object?[]
        {
            new CustomXamlType(typeof(MembersDataClass), new XamlSchemaContext())
            {
                LookupAllMembersResult = null
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupAllMembers_TestData))]
    public void LookupAllMembers_Invoke_ReturnsExpected(SubXamlType type, XamlMember[]? expectedLookup, XamlMember[]? expectedGet = null)
    {
        IEnumerable<XamlMember> expectedGetAllMembers = expectedGet ?? expectedLookup ?? Array.Empty<XamlMember>();
        Assert.Equal(expectedGetAllMembers.OrderBy(m => m.Name), type.GetAllMembers().OrderBy(m => m.Name));
        Assert.Equal(expectedLookup?.OrderBy(m => m.Name), type.LookupAllMembersEntry()?.OrderBy(m => m.Name));
    }

#if !DEBUG // This triggers a Debug.Assert.
    [Fact]
    public void LookupAllMembers_MembersNotCalled_ThrowsNullReferenceException()
    {
        var type = new SubXamlType(typeof(string), new XamlSchemaContext());
        Assert.Throws<NullReferenceException>(() => type.LookupAllMembersEntry());
    }
#endif

#pragma warning disable IDE0060, IDE0051 // Remove unused parameter, Remove unused member
    public class MembersDataClass
    {
        public int Property { get; set; }
        public int GetOnlyProperty { get; }
        public List<int>? GetOnlyListProperty { get; }
        public Dictionary<int, string>? GetOnlyDictionaryProperty { get; }
        public IXmlSerializable? GetXDataProperty { get; }
        public int SetOnlyProperty { set { } }
        public int PrivateGetProperty { private get; set; }
        public int PrivateSetProperty { get; private set; }

        public int this[int i]
        {
            get => 0;
            set { }
        }

        internal int InternalProperty { get; set; }
        protected int ProtectedProperty { get; set; }
        private int PrivateProperty { get; set; }
        public static int StaticProperty { get; set; }

        public event EventHandler Event
        {
            add { }
            remove { }
        }
        private event EventHandler PrivateEvent
        {
            add { }
            remove { }
        }

        public static event EventHandler StaticEvent
        {
            add { }
            remove { }
        }
    }
    
    public class ShadowedBaseClass
    {
        public Array? Property { get; set; }
        public int AccessChangedProperty { get; set; }

        public event EventHandler Event
        {
            add { }
            remove { }
        }
        public event EventHandler AccessChangedEvent
        {
            add { }
            remove { }
        }
    }

    public class MoreDerivedShadowedDataClass : ShadowedBaseClass
    {
        public new int[]? Property { get; set; }
        private new int AccessChangedProperty { get; set; }

        public new event OtherHandler Event
        {
            add { }
            remove { }
        }
        private new event EventHandler AccessChangedEvent
        {
            add { }
            remove { }
        }
    }

    public class EvenMoreDerivedShadowedBaseClass : MoreDerivedShadowedDataClass
    {
        public new string[]? Property { get; set; }
    }

    public class PrivateMembersDataClass
    {
        private int PrivateProperty { get; set; }
        private EventHandler? PrivateEvent { get; set; }
    }

    public delegate void OtherHandler();
#pragma warning restore IDE0060, IDE0051 // Remove unused parameter, Remove unused member

    public static IEnumerable<object?[]> LookupAllowedContentTypes_TestData()
    {
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), new XamlType?[] { null } };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), new XamlType?[] { null } };
        yield return new object?[] { new SubXamlType(typeof(int), new XamlSchemaContext()), new XamlType?[] { null } };
        yield return new object?[] { new SubXamlType(typeof(object), new XamlSchemaContext()), new XamlType?[] { null } };
        yield return new object?[] { new SubXamlType(typeof(List<int>), new XamlSchemaContext()), new XamlType[] { new XamlType(typeof(int), new XamlSchemaContext()) } };
        yield return new object?[] { new NoUnderlyingOrBaseType(), new XamlType?[] { null } };

        // Has provider.
        yield return new object?[]
        {
            new CustomXamlType(typeof(List<string>), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new ContentWrapperAttribute(typeof(ClassWithUnknownContentPropertyAttribute)) }
                }
            },
            new XamlType[] { new XamlType(typeof(string), new XamlSchemaContext()) }
        };
        yield return new object?[]
        {
            new CustomXamlType(typeof(List<string>), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => Array.Empty<object>()
                }
            },
            new XamlType[] { new XamlType(typeof(string), new XamlSchemaContext()) }
        };
        yield return new object?[]
        {
            new CustomXamlType("namespace", "name", null, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new ContentWrapperAttribute(typeof(int)) }
                }
            },
            new XamlType?[] { null }
        };

        // Has attribute.
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithContentPropertyContentWrapperAttribute), new XamlSchemaContext()),
            new XamlType[] { new XamlType(typeof(string), new XamlSchemaContext()), new XamlType(typeof(int), new XamlSchemaContext()) }
        };

        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithContentWrapperAttribute), new XamlSchemaContext()),
            new XamlType[] { new XamlType(typeof(string), new XamlSchemaContext()) }
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(InheritedClassWithContentWrapperAttribute), new XamlSchemaContext()),
            new XamlType[] { new XamlType(typeof(string), new XamlSchemaContext()) }
        };
        yield return new object?[]
        {
            new SubXamlType(new ReflectionOnlyCustomAttributeDataType(typeof(ClassWithContentWrapperAttribute)), new XamlSchemaContext()),
            new XamlType[] { new XamlType(typeof(string), new XamlSchemaContext()) }
        };
        yield return new object?[]
        {
            new SubXamlType(new ThrowsCustomAttributeFormatExceptionDelegator(typeof(List<string>)), new XamlSchemaContext()),
            new XamlType[] { new XamlType(typeof(string), new XamlSchemaContext()) }
        };

        yield return new object?[]
        {
            new CustomXamlType(typeof(ClassWithContentWrapperAttribute), new XamlSchemaContext())
            {
                LookupAllowedContentTypesResult = null
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupAllowedContentTypes_TestData))]
    public void LookupAllowedContentTypes_Invoke_ReturnsExpected(SubXamlType type, IList<XamlType> expected)
    {
        Assert.Equal(expected, type.LookupAllowedContentTypesEntry());
        Assert.Equal((type.IsCollection || type.IsDictionary) ? expected ?? Array.Empty<XamlType>() : null, type.AllowedContentTypes);
    }

    [Fact]
    public void LookupAllowedContentTypes_NullTypeInAttribute_ThrowsArgumentNullException()
    {
        var type = new SubXamlType(typeof(ClassWithNullContentWrapperAttribute), new XamlSchemaContext());
        Assert.Throws<ArgumentNullException>("type", () => type.LookupAllowedContentTypesEntry());
        Assert.Throws<ArgumentNullException>("type", () => type.AllowedContentTypes);
    }

    [Fact]
    public void LookupAllowedContentTypes_NullAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(List<int>), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => null!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupAllowedContentTypesEntry());
        Assert.Throws<NullReferenceException>(() => type.AllowedContentTypes);
    }

    [Fact]
    public void LookupAllowedContentTypes_NullItemInAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(List<int>), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object?[] { null }!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupAllowedContentTypesEntry());
        Assert.Throws<NullReferenceException>(() => type.AllowedContentTypes);
    }

    [Fact]
    public void LookupAllowedContentTypes_InvalidTypeItemInAttributeResult_ThrowsInvalidCastException()
    {
        var type = new CustomXamlType(typeof(List<int>), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object[] { new object() }
            }
        };
        Assert.Throws<InvalidCastException>(() => type.LookupAllowedContentTypesEntry());
        Assert.Throws<InvalidCastException>(() => type.AllowedContentTypes);
    }

    [ContentWrapper(typeof(int))]
    [ContentWrapper(typeof(ClassWithUnknownContentPropertyAttribute))]
    [ContentWrapper(typeof(ClassWithKnownContentPropertyAttribute))]
    [ContentWrapper(typeof(ClassWithKnownContentPropertyAttribute))]
    [ContentWrapper(typeof(InheritedClassWithContentPropertyAttribute))]
    private class ClassWithContentPropertyContentWrapperAttribute : Collection<string>
    {
    }

    public static IEnumerable<object[]> LookupAllowedContentTypes_InvalidAttribute_TestData()
    {
        yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1), new CustomAttributeTypedArgument(typeof(int), 2) } };
        yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1) } };
    }

    [Theory]
    [MemberData(nameof(LookupAllowedContentTypes_InvalidAttribute_TestData))]
    public void LookupAllowedContentTypes_InvalidAttribute_ThrowsXamlSchemaException(CustomAttributeTypedArgument[] arguments)
    {
        var type = new SubXamlType(new CustomType(typeof(List<int>))
        {
            GetCustomAttributesDataResult = new CustomAttributeData[]
            {
                new CustomCustomAttributeData
                {
                    ConstructorResult = typeof(ContentWrapperAttribute).GetConstructors()[0],
                    ConstructorArgumentsResult = arguments
                }
            }
        }, new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => type.LookupAllowedContentTypesEntry());
        Assert.Throws<XamlSchemaException>(() => type.AllowedContentTypes);
    }

    public static IEnumerable<object?[]> LookupAttachableMember_TestData()
    {
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), "name", null };
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), "", null };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), "name", null };
        yield return new object?[] { new SubXamlType(typeof(int), new XamlSchemaContext()), "name", null };
        yield return new object?[] { new SubXamlType(typeof(object), new XamlSchemaContext()), "name", null };
        yield return new object?[] { new NoUnderlyingOrBaseType(), "name", null };

        // Property.
        yield return new object?[]
        {
            new SubXamlType(typeof(AttachableMembersDataClass), new XamlSchemaContext()),
            "GetSetProperty",
            new XamlMember("GetSetProperty", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetSetProperty)), typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetGetSetProperty)), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(AttachableMembersDataClass), new XamlSchemaContext()),
            "ProtectedProperty",
            new XamlMember("ProtectedProperty", typeof(AttachableMembersDataClass).GetMethod("GetProtectedProperty", BindingFlags.Static | BindingFlags.NonPublic), typeof(AttachableMembersDataClass).GetMethod("SetProtectedProperty", BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(AttachableMembersDataClass), new XamlSchemaContext()),
            "InternalProperty",
            new XamlMember("InternalProperty", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetInternalProperty), BindingFlags.Static | BindingFlags.NonPublic), typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetInternalProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(AttachableMembersDataClass), new XamlSchemaContext()),
            "PrivateProperty",
            null
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(AttachableMembersDataClass), new XamlSchemaContext()),
            "GetOnlyIntProperty",
            null
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(AttachableMembersDataClass), new XamlSchemaContext()),
            "GetOnlyListProperty",
            new XamlMember("GetOnlyListProperty", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetOnlyListProperty)), null, new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(AttachableMembersDataClass), new XamlSchemaContext()),
            "GetOnlyDictionaryProperty",
            new XamlMember("GetOnlyDictionaryProperty", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetOnlyDictionaryProperty)), null, new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(AttachableMembersDataClass), new XamlSchemaContext()),
            "GetOnlyXDataProperty",
            new XamlMember("GetOnlyXDataProperty", typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.GetGetOnlyXDataProperty)), null, new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(AttachableMembersDataClass), new XamlSchemaContext()),
            "DifferentFirstParameter",
            new XamlMember("DifferentFirstParameter", null, typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetDifferentFirstParameter)), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(AttachableMembersDataClass), new XamlSchemaContext()),
            "DifferentSecondParameter",
            new XamlMember("DifferentSecondParameter", null, typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.SetDifferentSecondParameter)), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(AttachableMembersDataClass), new XamlSchemaContext()),
            "Void",
            null
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(AttachableMembersDataClass), new XamlSchemaContext()),
            "Parameterless",
            null
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(AttachableMembersDataClass), new XamlSchemaContext()),
            "TooManyParameters",
            null
        };

        // Property in public class.
        yield return new object?[]
        {
            new SubXamlType(typeof(PrivateAttachableMembersDataClass), new XamlSchemaContext()),
            "GetSetProperty",
            new XamlMember("GetSetProperty", typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(PrivateAttachableMembersDataClass.GetGetSetProperty)), typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(PrivateAttachableMembersDataClass.SetGetSetProperty)), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(PrivateAttachableMembersDataClass), new XamlSchemaContext()),
            "ProtectedProperty",
            new XamlMember("ProtectedProperty", typeof(PrivateAttachableMembersDataClass).GetMethod("GetProtectedProperty", BindingFlags.Static | BindingFlags.NonPublic), typeof(PrivateAttachableMembersDataClass).GetMethod("SetProtectedProperty", BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(PrivateAttachableMembersDataClass), new XamlSchemaContext()),
            "InternalProperty",
            new XamlMember("InternalProperty", typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(PrivateAttachableMembersDataClass.GetInternalProperty), BindingFlags.Static | BindingFlags.NonPublic), typeof(PrivateAttachableMembersDataClass).GetMethod(nameof(PrivateAttachableMembersDataClass.SetInternalProperty), BindingFlags.Static | BindingFlags.NonPublic), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(PrivateAttachableMembersDataClass), new XamlSchemaContext()),
            "PrivateProperty",
            null
        };

        // Event.
        yield return new object?[]
        {
            new SubXamlType(typeof(AttachableMembersDataClass), new XamlSchemaContext()),
            "Event",
            new XamlMember("Event", null, typeof(AttachableMembersDataClass).GetMethod(nameof(AttachableMembersDataClass.AddEventHandler)), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(AttachableMembersDataClass), new XamlSchemaContext()),
            "PrivateEvent",
            null
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(AttachableMembersDataClass), new XamlSchemaContext()),
            "Parameterless",
            null
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(AttachableMembersDataClass), new XamlSchemaContext()),
            "TooManyParameters",
            null
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(AttachableMembersDataClass), new XamlSchemaContext()),
            "InvalidSecondParameter",
            null
        };

        yield return new object?[]
        {
            new CustomXamlType(typeof(AttachableMembersDataClass), new XamlSchemaContext())
            {
                LookupAttachableMemberResult = null
            },
            "name",
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupAttachableMember_TestData))]
    public void LookupAttachableMember_Invoke_ReturnsExpected(SubXamlType type, string name, XamlMember expectedLookup, XamlMember? expectedGet = null)
    {
        AssertEqualXamlMember(expectedLookup, type.LookupAttachableMemberEntry(name));
        AssertEqualXamlMember(expectedGet ?? expectedLookup, type.GetAttachableMember(name));
    }

    [Fact]
    public void LookupAttachableMember_NullName_ThrowsArgumentNullException()
    {
        var type = new SubXamlType(typeof(int), new XamlSchemaContext());
        Assert.Null(type.LookupAttachableMemberEntry(null!));
        Assert.Throws<ArgumentNullException>("key", () => type.GetAttachableMember(null));
    }

    [Fact]
    public void LookupAttachableMember_NullNameUnknownNoBaseType_ThrowsArgumentNullExceptionOnGetter()
    {
        var type = new NoUnderlyingOrBaseType();
        Assert.Null(type.LookupAttachableMemberEntry(null!));
        Assert.Throws<ArgumentNullException>("key", () => type.GetAttachableMember(null));
    }

    public static IEnumerable<object?[]> LookupBaseType_TestData()
    {
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), XamlLanguage.Object };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), XamlLanguage.Object };
        yield return new object?[] { new NoUnderlyingOrBaseType(), null };

        yield return new object?[] { new SubXamlType(typeof(int), new XamlSchemaContext()), new XamlType(typeof(ValueType), new XamlSchemaContext()) };
        yield return new object?[] { new SubXamlType(typeof(object), new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(IConvertible), new XamlSchemaContext()), null };

        yield return new object?[]
        {
            new SubXamlType(typeof(CustomXamlSchemaContext), new CustomXamlSchemaContext
            {
                GetXamlTypeAction = type =>
                {
                    Assert.Equal(typeof(XamlSchemaContext), type);
                    return XamlLanguage.Single;
                }
            }),
            XamlLanguage.Single
        };
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupBaseTypeResult = null
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupBaseType_TestData))]
    public void LookupBaseType_Invoke_ReturnsExpected(SubXamlType type, XamlType expected)
    {
        Assert.Equal(expected, type.LookupBaseTypeEntry());
        Assert.Equal(expected, type.BaseType);
    }

    public static IEnumerable<object?[]> LookupCollectionKind_TestData()
    {
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), XamlCollectionKind.None };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), XamlCollectionKind.None };
        yield return new object?[] { new NoUnderlyingOrBaseType(), XamlCollectionKind.None };

        yield return new object?[] { new SubXamlType(typeof(int), new XamlSchemaContext()), XamlCollectionKind.None };
        yield return new object?[] { new SubXamlType(typeof(int[]), new XamlSchemaContext()), XamlCollectionKind.Array };
        yield return new object?[] { new SubXamlType(typeof(IEnumerable), new XamlSchemaContext()), XamlCollectionKind.None };
        yield return new object?[] { new SubXamlType(typeof(IEnumerable<int>), new XamlSchemaContext()), XamlCollectionKind.None };
        yield return new object?[] { new SubXamlType(typeof(ICollection), new XamlSchemaContext()), XamlCollectionKind.None };
        yield return new object?[] { new SubXamlType(typeof(ICollection<int>), new XamlSchemaContext()), XamlCollectionKind.Collection };
        yield return new object?[] { new SubXamlType(typeof(IList), new XamlSchemaContext()), XamlCollectionKind.Collection };
        yield return new object?[] { new SubXamlType(typeof(IList<int>), new XamlSchemaContext()), XamlCollectionKind.Collection };
        yield return new object?[] { new SubXamlType(typeof(IDictionary), new XamlSchemaContext()), XamlCollectionKind.Dictionary };
        yield return new object?[] { new SubXamlType(typeof(IDictionary<int, int>), new XamlSchemaContext()), XamlCollectionKind.Dictionary };
        yield return new object?[] { new SubXamlType(typeof(Collection<int>), new XamlSchemaContext()), XamlCollectionKind.Collection };
        yield return new object?[] { new SubXamlType(typeof(List<int>), new XamlSchemaContext()), XamlCollectionKind.Collection };
        yield return new object?[] { new SubXamlType(typeof(GetEnumeratorClass), new XamlSchemaContext()), XamlCollectionKind.Collection };
        yield return new object?[] { new SubXamlType(typeof(InvalidReturnGetEnumeratorClass), new XamlSchemaContext()), XamlCollectionKind.None };
        yield return new object?[] { new SubXamlType(typeof(InvalidParametersGetEnumeratorClass), new XamlSchemaContext()), XamlCollectionKind.None };
        yield return new object?[] { new SubXamlType(typeof(MultiICollectionImplementer), new XamlSchemaContext()), XamlCollectionKind.Collection };
        yield return new object?[] { new SubXamlType(typeof(ICollectionImplementer), new XamlSchemaContext()), XamlCollectionKind.Collection };
        yield return new object?[] { new SubXamlType(typeof(InternalICollectionImplementer), new XamlSchemaContext()), XamlCollectionKind.Collection };
        yield return new object?[] { new SubXamlType(typeof(MoreThanOneICollectionImplementer), new XamlSchemaContext()), XamlCollectionKind.Collection };
        yield return new object?[] { new SubXamlType(typeof(PrivateICollectionImplementer), new XamlSchemaContext()), XamlCollectionKind.None };
        yield return new object?[] { new SubXamlType(typeof(ProtectedICollectionImplementer), new XamlSchemaContext()), XamlCollectionKind.None };
        yield return new object?[] { new SubXamlType(typeof(Dictionary<int, int>), new XamlSchemaContext()), XamlCollectionKind.Dictionary };
        yield return new object?[] { new SubXamlType(typeof(IDictionaryImplementer), new XamlSchemaContext()), XamlCollectionKind.Dictionary };
        yield return new object?[] { new SubXamlType(typeof(InternalIDictionaryImplementer), new XamlSchemaContext()), XamlCollectionKind.Dictionary };
        yield return new object?[] { new SubXamlType(typeof(MoreThanOneIDictionaryImplementer), new XamlSchemaContext()), XamlCollectionKind.Dictionary };
        yield return new object?[] { new SubXamlType(typeof(PrivateIDictionaryImplementer), new XamlSchemaContext()), XamlCollectionKind.None };
        yield return new object?[] { new SubXamlType(typeof(ProtectedIDictionaryImplementer), new XamlSchemaContext()), XamlCollectionKind.None };

/*
        yield return new object?[]
        {
            new CustomXamlType(typeof(ICollectionImplementer))
        }
*/
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCollectionKindResult = (XamlCollectionKind)(XamlCollectionKind.Array + 1)
            },
            (XamlCollectionKind)(XamlCollectionKind.Array + 1)
        };
    }

    [Theory]
    [MemberData(nameof(LookupCollectionKind_TestData))]
    public void LookupCollectionKind_Invoke_ReturnsExpected(SubXamlType type, XamlCollectionKind expected)
    {
        Assert.Equal(expected, type.LookupCollectionKindEntry());
        Assert.Equal(expected == XamlCollectionKind.Array, type.IsArray);
        Assert.Equal(expected == XamlCollectionKind.Collection, type.IsCollection);
        Assert.Equal(expected == XamlCollectionKind.Dictionary, type.IsDictionary);
    }

#pragma warning disable IDE0060, IDE0051 // Remove unused parameter, Remove unused member
    private class GetEnumeratorClass
    {
        public IEnumerator GetEnumerator() => throw new NotImplementedException();
        public void Add(object value) => throw new NotImplementedException();
    }

    private class InvalidReturnGetEnumeratorClass
    {
        public int GetEnumerator() => throw new NotImplementedException();
        public void Add(object value) => throw new NotImplementedException();
    }

    private class InvalidParametersGetEnumeratorClass
    {
        public IEnumerator GetEnumerator(int value) => throw new NotImplementedException();
        public void Add(object value) => throw new NotImplementedException();
    }

    private class MultiICollectionImplementer : ICollection<int>, ICollection<object>
    {
        public int Count => throw new NotImplementedException();

        public bool IsReadOnly => throw new NotImplementedException();

        public void Add(int item) => throw new NotImplementedException();
        public void Add(object item) => throw new NotImplementedException();

        public void Clear() => throw new NotImplementedException();

        public bool Contains(int item) => throw new NotImplementedException();
        public bool Contains(object item) => throw new NotImplementedException();

        public void CopyTo(int[] array, int arrayIndex) => throw new NotImplementedException();
        public void CopyTo(object[] array, int arrayIndex) => throw new NotImplementedException();

        IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw new NotImplementedException();
        IEnumerator<object> IEnumerable<object>.GetEnumerator() => throw new NotImplementedException();

        public bool Remove(int item) => throw new NotImplementedException();
        public bool Remove(object item) => throw new NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }

    private class ICollectionImplementer : IEnumerable
    {
        public void Add(object value) => throw new NotImplementedException();
        public IEnumerator GetEnumerator() => throw new NotImplementedException();
    }

    private class MoreThanOneICollectionImplementer : ICollectionImplementer
    {
        public void Add(int value) => throw new NotImplementedException();
    }

    private class InternalICollectionImplementer : IEnumerable
    {
        internal void Add(object value) => throw new NotImplementedException();
        public IEnumerator GetEnumerator() => throw new NotImplementedException();
    }

    private class PrivateICollectionImplementer : IEnumerable
    {
        private void Add(object value) => throw new NotImplementedException();
        public IEnumerator GetEnumerator() => throw new NotImplementedException();
    }

    private class ProtectedICollectionImplementer : IEnumerable
    {
        protected void Add(object value) => throw new NotImplementedException();
        public IEnumerator GetEnumerator() => throw new NotImplementedException();
    }

    private class IDictionaryImplementer : IEnumerable
    {
        public void Add(object key, object value) => throw new NotImplementedException();
        public IEnumerator GetEnumerator() => throw new NotImplementedException();
    }

    private class MoreThanOneIDictionaryImplementer : IDictionaryImplementer
    {
        public void Add(int key, int value) => throw new NotImplementedException();
        public void Add() => throw new NotImplementedException();
    }

    private class InternalIDictionaryImplementer : IEnumerable
    {
        internal void Add(object key, object value) => throw new NotImplementedException();
        public IEnumerator GetEnumerator() => throw new NotImplementedException();
    }

    private class PrivateIDictionaryImplementer : IEnumerable
    {
        private void Add(object key, object value) => throw new NotImplementedException();
        public IEnumerator GetEnumerator() => throw new NotImplementedException();
    }

    private class ProtectedIDictionaryImplementer : IEnumerable
    {
        protected void Add(object key, object value) => throw new NotImplementedException();
        public IEnumerator GetEnumerator() => throw new NotImplementedException();
    }
#pragma warning restore IDE0060, IDE0051 // Remove unused parameter, Remove unused member

    public static IEnumerable<object[]> LookupConstructionRequiresArguments_TestData()
    {
        yield return new object[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType("name", null, new XamlSchemaContext()), false };
        yield return new object[] { new NoUnderlyingOrBaseType(), false };

        yield return new object[] { new SubXamlType(typeof(int), new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(ClassWithDefaultConstructor), new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(AbstractClass), new XamlSchemaContext()), true };
        yield return new object[] { new SubXamlType(typeof(IConvertible), new XamlSchemaContext()), true };
        yield return new object[] { new SubXamlType(typeof(NestedClass), new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(ClassWithInternalDefaultConstructor), new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(ClassWithProtectedDefaultConstructor), new XamlSchemaContext()), true };
        yield return new object[] { new SubXamlType(typeof(ClassWithPrivateDefaultConstructor), new XamlSchemaContext()), true };
        yield return new object[] { new SubXamlType(typeof(ClassWithCustomConstructor), new XamlSchemaContext()), true };
        yield return new object[] { new SubXamlType(typeof(StaticClass), new XamlSchemaContext()), true };

        yield return new object[]
        {
            new CustomXamlType(typeof(ClassWithCustomConstructor), new XamlSchemaContext())
            {
                LookupConstructionRequiresArgumentsResult = false
            },
            false
        };
    }

    [Theory]
    [MemberData(nameof(LookupConstructionRequiresArguments_TestData))]
    public void LookupConstructionRequiresArguments_Invoke_ReturnsExpected(SubXamlType type, bool expected)
    {
        Assert.Equal(expected, type.LookupConstructionRequiresArgumentsEntry());
        Assert.Equal(expected, type.ConstructionRequiresArguments);
    }

    public static IEnumerable<object?[]> LookupContentProperty_TestData()
    {
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(int), new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(object), new XamlSchemaContext()), null };
        yield return new object?[] { new NoUnderlyingOrBaseType(), null };

        // Has provider.
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new ContentPropertyAttribute("name") }
                }
            },
            new XamlMember("name", new XamlType(typeof(int), new XamlSchemaContext()), false)
        };
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => Array.Empty<object>()
                }
            },
            null
        };
        yield return new object?[]
        {
            new CustomXamlType("namespace", "name", null, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new ContentPropertyAttribute("name") }
                }
            },
            null
        };

        // Has attribute.
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithKnownContentPropertyAttribute), new XamlSchemaContext()),
            new XamlMember(typeof(ClassWithKnownContentPropertyAttribute).GetProperty(nameof(ClassWithKnownContentPropertyAttribute.Name)), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithUnknownContentPropertyAttribute), new XamlSchemaContext()),
            new XamlMember("name", new XamlType(typeof(ClassWithUnknownContentPropertyAttribute), new XamlSchemaContext()), false)
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(InheritedClassWithContentPropertyAttribute), new XamlSchemaContext()),
            new XamlMember(typeof(ClassWithKnownContentPropertyAttribute).GetProperty(nameof(ClassWithKnownContentPropertyAttribute.Name)), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithNullContentPropertyAttribute), new XamlSchemaContext()),
            null
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithDefaultContentPropertyAttribute), new XamlSchemaContext()),
            null
        };
        yield return new object?[]
        {
            new SubXamlType(new ReflectionOnlyCustomAttributeDataType(typeof(ClassWithUnknownContentPropertyAttribute)), new XamlSchemaContext()),
            new XamlMember("name", new XamlType(new ReflectionOnlyCustomAttributeDataType(typeof(ClassWithUnknownContentPropertyAttribute)), new XamlSchemaContext()), false)
        };
        yield return new object?[]
        {
            new SubXamlType(new ThrowsCustomAttributeFormatExceptionDelegator(typeof(int)), new XamlSchemaContext()),
            null
        };
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupBaseTypeResult = new XamlType("namespace", "name", null, new XamlSchemaContext())
            },
            null
        };

        yield return new object?[]
        {
            new CustomXamlType(typeof(ClassWithUnknownContentPropertyAttribute), new XamlSchemaContext())
            {
                LookupContentPropertyResult = null
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupContentProperty_TestData))]
    public void LookupContentProperty_Invoke_ReturnsExpected(SubXamlType type, XamlMember expected)
    {
        Assert.Equal(expected, type.LookupContentPropertyEntry());
        Assert.Equal(expected, type.ContentProperty);
    }

    [Fact]
    public void LookupContentProperty_NullAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => null!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupContentPropertyEntry());
        Assert.Throws<NullReferenceException>(() => type.ContentProperty);
    }

    [Fact]
    public void LookupContentProperty_NullItemInAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object?[] { null }!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupContentPropertyEntry());
        Assert.Throws<NullReferenceException>(() => type.ContentProperty);
    }

    [Fact]
    public void LookupContentProperty_InvalidTypeItemInAttributeResult_ThrowsInvalidCastException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object[] { new object() }
            }
        };
        Assert.Throws<InvalidCastException>(() => type.LookupContentPropertyEntry());
        Assert.Throws<InvalidCastException>(() => type.ContentProperty);
    }

    public static IEnumerable<object[]> LookupContentProperty_InvalidAttribute_TestData()
    {
        yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1), new CustomAttributeTypedArgument(typeof(int), 2) } };
        yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1) } };
    }

    [Theory]
    [MemberData(nameof(LookupContentProperty_InvalidAttribute_TestData))]
    public void LookupContentProperty_InvalidAttribute_ThrowsXamlSchemaException(CustomAttributeTypedArgument[] arguments)
    {
        var type = new SubXamlType(new CustomType(typeof(int))
        {
            GetCustomAttributesDataResult = new CustomAttributeData[]
            {
                new CustomCustomAttributeData
                {
                    ConstructorResult = typeof(ContentPropertyAttribute).GetConstructors()[0],
                    ConstructorArgumentsResult = arguments
                }
            }
        }, new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => type.LookupContentPropertyEntry());
        Assert.Throws<XamlSchemaException>(() => type.ContentProperty);
    }

    [ContentProperty("name")]
    private class ClassWithUnknownContentPropertyAttribute
    {
    }

    [ContentProperty("Name")]
    private class ClassWithKnownContentPropertyAttribute
    {
        public int Name { get; set; }
    }

    private class InheritedClassWithContentPropertyAttribute : ClassWithKnownContentPropertyAttribute
    {
    }

    [ContentProperty(null)]
    private class ClassWithNullContentPropertyAttribute
    {
    }

    [ContentProperty]
    private class ClassWithDefaultContentPropertyAttribute
    {
    }

    public static IEnumerable<object?[]> LookupContentWrappers_TestData()
    {
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(int), new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(object), new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(List<int>), new XamlSchemaContext()), null };
        yield return new object?[] { new NoUnderlyingOrBaseType(), null };

        // Has provider.
        yield return new object?[]
        {
            new CustomXamlType(typeof(List<string>), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new ContentWrapperAttribute(typeof(int)) }
                }
            },
            new XamlType[] { new XamlType(typeof(int), new XamlSchemaContext()) }
        };
        yield return new object?[]
        {
            new CustomXamlType(typeof(List<string>), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => Array.Empty<object>()
                }
            },
            null
        };
        yield return new object?[]
        {
            new CustomXamlType("namespace", "name", null, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new ContentWrapperAttribute(typeof(int)) }
                }
            },
            null
        };

        // Has attribute.
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithContentWrapperAttribute), new XamlSchemaContext()),
            new XamlType[] { new XamlType(typeof(int), new XamlSchemaContext()) }
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(InheritedClassWithContentWrapperAttribute), new XamlSchemaContext()),
            new XamlType[] { new XamlType(typeof(string), new XamlSchemaContext()), new XamlType(typeof(int), new XamlSchemaContext()) }
        };
        yield return new object?[]
        {
            new SubXamlType(new ReflectionOnlyCustomAttributeDataType(typeof(ClassWithContentWrapperAttribute)), new XamlSchemaContext()),
            new XamlType[] { new XamlType(typeof(int), new XamlSchemaContext()) }
        };
        yield return new object?[]
        {
            new SubXamlType(new ThrowsCustomAttributeFormatExceptionDelegator(typeof(List<string>)), new XamlSchemaContext()),
            null
        };

        yield return new object?[]
        {
            new CustomXamlType(typeof(ClassWithContentWrapperAttribute), new XamlSchemaContext())
            {
                LookupContentWrappersResult = null
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupContentWrappers_TestData))]
    public void LookupContentWrappers_Invoke_ReturnsExpected(SubXamlType type, IList<XamlType> expected)
    {
        Assert.Equal(expected, type.LookupContentWrappersEntry());
        Assert.Equal(type.IsCollection ? expected ?? Array.Empty<XamlType>() : expected, type.ContentWrappers);
    }

    [Fact]
    public void LookupContentWrappers_NullTypeInAttribute_ThrowsArgumentNullException()
    {
        var type = new SubXamlType(typeof(ClassWithNullContentWrapperAttribute), new XamlSchemaContext());
        Assert.Throws<ArgumentNullException>("type", () => type.LookupContentWrappersEntry());
        Assert.Throws<ArgumentNullException>("type", () => type.ContentWrappers);
    }

    [Fact]
    public void LookupContentWrappers_NullAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(List<int>), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => null!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupContentWrappersEntry());
        Assert.Throws<NullReferenceException>(() => type.ContentWrappers);
    }

    [Fact]
    public void LookupContentWrappers_NullItemInAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(List<int>), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object?[] { null }!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupContentWrappersEntry());
        Assert.Throws<NullReferenceException>(() => type.ContentWrappers);
    }

    [Fact]
    public void LookupContentWrappers_InvalidTypeItemInAttributeResult_ThrowsInvalidCastException()
    {
        var type = new CustomXamlType(typeof(List<int>), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object[] { new object() }
            }
        };
        Assert.Throws<InvalidCastException>(() => type.LookupContentWrappersEntry());
        Assert.Throws<InvalidCastException>(() => type.ContentWrappers);
    }

    public static IEnumerable<object[]> LookupContentWrappers_InvalidAttribute_TestData()
    {
        yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1), new CustomAttributeTypedArgument(typeof(int), 2) } };
        yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1) } };
    }

    [Theory]
    [MemberData(nameof(LookupContentWrappers_InvalidAttribute_TestData))]
    public void LookupContentWrappers_InvalidAttribute_ThrowsXamlSchemaException(CustomAttributeTypedArgument[] arguments)
    {
        var type = new SubXamlType(new CustomType(typeof(List<int>))
        {
            GetCustomAttributesDataResult = new CustomAttributeData[]
            {
                new CustomCustomAttributeData
                {
                    ConstructorResult = typeof(ContentWrapperAttribute).GetConstructors()[0],
                    ConstructorArgumentsResult = arguments
                }
            }
        }, new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => type.LookupContentWrappersEntry());
        Assert.Throws<XamlSchemaException>(() => type.ContentWrappers);
    }

    [ContentWrapper(typeof(int))]
    private class ClassWithContentWrapperAttribute : Collection<string>
    {
    }

    [ContentWrapper(typeof(string))]
    private class InheritedClassWithContentWrapperAttribute : ClassWithContentWrapperAttribute
    {
    }

    [ContentWrapper(null!)]
    private class ClassWithNullContentWrapperAttribute : Collection<string>
    {
    }

    [Fact]
    public void LookupCustomAttributeProvider_Unknown_ReturnsNull()
    {
        var type = new SubXamlType(typeof(int), new XamlSchemaContext());
        Assert.Null(type.LookupCustomAttributeProviderEntry());
    }

    [Fact]
    public void LookupCustomAttributeProvider_UnderlyingType_ReturnsNull()
    {
        var type = new SubXamlType("namespace", "name", null, new XamlSchemaContext());
        Assert.Null(type.LookupCustomAttributeProviderEntry());
    }

    public static IEnumerable<object?[]> LookupDeferringLoader_TestData()
    {
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(int), new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(object), new XamlSchemaContext()), null };
        yield return new object?[] { new NoUnderlyingOrBaseType(), null };

        // Has provider.
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new XamlDeferLoadAttribute(typeof(int), typeof(string)) }
                }
            },
            new XamlValueConverter<XamlDeferringLoader>(typeof(int), null)
        };
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => Array.Empty<object>()
                }
            },
            null
        };
        yield return new object?[]
        {
            new CustomXamlType("namespace", "name", null, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new XamlDeferLoadAttribute(typeof(int), typeof(string)) }
                }
            },
            null
        };

        // Has attribute.
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithTypeXamlDeferLoadAttribute), new XamlSchemaContext()),
            new XamlValueConverter<XamlDeferringLoader>(typeof(int), null)
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(InheritedClassWithTypeXamlDeferLoadAttribute), new XamlSchemaContext()),
            new XamlValueConverter<XamlDeferringLoader>(typeof(int), null)
        };
        yield return new object?[]
        {
            new SubXamlType(new ReflectionOnlyCustomAttributeDataType(typeof(ClassWithStringXamlDeferLoadAttribute)), new XamlSchemaContext()),
            new XamlValueConverter<XamlDeferringLoader>(typeof(int), null)
        };
        yield return new object?[]
        {
            new SubXamlType(new ThrowsCustomAttributeFormatExceptionDelegator(typeof(int)), new XamlSchemaContext()),
            null
        };

        yield return new object?[]
        {
            new CustomXamlType(typeof(ClassWithTypeXamlDeferLoadAttribute), new XamlSchemaContext())
            {
                LookupDeferringLoaderResult = null
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupDeferringLoader_TestData))]
    public void LookupDeferringLoader_Invoke_ReturnsExpected(SubXamlType type, XamlValueConverter<XamlDeferringLoader> expected)
    {
        Assert.Equal(expected, type.LookupDeferringLoaderEntry());
        Assert.Equal(expected, type.DeferringLoader);
    }

    [Fact]
    public void LookupDeferringLoader_NullAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => null!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupDeferringLoaderEntry());
        Assert.Throws<NullReferenceException>(() => type.DeferringLoader);
    }

    [Fact]
    public void LookupDeferringLoader_NullItemInAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object?[] { null }!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupDeferringLoaderEntry());
        Assert.Throws<NullReferenceException>(() => type.DeferringLoader);
    }

    [Fact]
    public void LookupDeferringLoader_InvalidTypeItemInAttributeResult_ThrowsInvalidCastException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object[] { new object() }
            }
        };
        Assert.Throws<InvalidCastException>(() => type.LookupDeferringLoaderEntry());
        Assert.Throws<InvalidCastException>(() => type.DeferringLoader);
    }

    public static IEnumerable<object[]> LookupDeferringLoader_InvalidAttribute_TestData()
    {
        yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1), new CustomAttributeTypedArgument(typeof(int), 2) } };
        yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1) } };
    }

    [Theory]
    [MemberData(nameof(LookupDeferringLoader_InvalidAttribute_TestData))]
    public void LookupDeferringLoader_InvalidAttribute_ThrowsXamlSchemaException(CustomAttributeTypedArgument[] arguments)
    {
        var type = new SubXamlType(new CustomType(typeof(List<int>))
        {
            GetCustomAttributesDataResult = new CustomAttributeData[]
            {
                new CustomCustomAttributeData
                {
                    ConstructorResult = typeof(XamlDeferLoadAttribute).GetConstructors()[0],
                    ConstructorArgumentsResult = arguments
                }
            }
        }, new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => type.LookupDeferringLoaderEntry());
        Assert.Throws<XamlSchemaException>(() => type.DeferringLoader);
    }


    [Theory]
    [InlineData(typeof(ClassWithNullLoaderTypeXamlDeferLoadAttribute))]
    [InlineData(typeof(ClassWithNullContentTypeXamlDeferLoadAttribute))]
    [InlineData(typeof(ClassWithNoSuchLoaderTypeNameXamlDeferLoadAttribute))]
    [InlineData(typeof(ClassWithNoSuchContentTypeNameXamlDeferLoadAttribute))]
    public void LookupDeferringLoader_InvalidParametersType_ThrowsXamlSchemaException(Type underlyingType)
    {
        var type = new SubXamlType(underlyingType, new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => type.LookupDeferringLoaderEntry());
        Assert.Throws<XamlSchemaException>(() => type.DeferringLoader);
    }

    [Theory]
    [InlineData(typeof(ClassWithNullLoaderTypeNameXamlDeferLoadAttribute))]
    [InlineData(typeof(ClassWithNullContentTypeNameXamlDeferLoadAttribute))]
    public void LookupDeferringLoader_NullParametersTypeNames_ThrowsXamlSchemaException(Type underlyingType)
    {
        var type = new SubXamlType(underlyingType, new XamlSchemaContext());
        Assert.Throws<ArgumentNullException>("typeName", () => type.LookupDeferringLoaderEntry());
        Assert.Throws<ArgumentNullException>("typeName", () => type.DeferringLoader);
    }

    [XamlDeferLoad(typeof(int), typeof(string))]
    public class ClassWithTypeXamlDeferLoadAttribute
    {
    }

    [XamlDeferLoad("System.Int32", "System.String")]
    public class ClassWithStringXamlDeferLoadAttribute
    {
    }

    [XamlDeferLoad(null!, typeof(string))]
    public class ClassWithNullLoaderTypeXamlDeferLoadAttribute
    {
    }

    [XamlDeferLoad(null!, typeof(string))]
    public class ClassWithNullContentTypeXamlDeferLoadAttribute
    {
    }

    [XamlDeferLoad(null!, "System.String")]
    public class ClassWithNullLoaderTypeNameXamlDeferLoadAttribute
    {
    }

    [XamlDeferLoad("System.Int32", null!)]
    public class ClassWithNullContentTypeNameXamlDeferLoadAttribute
    {
    }

    [XamlDeferLoad("NoSuchType", "System.String")]
    public class ClassWithNoSuchLoaderTypeNameXamlDeferLoadAttribute
    {
    }

    [XamlDeferLoad("System.Int32", "NoSuchType")]
    public class ClassWithNoSuchContentTypeNameXamlDeferLoadAttribute
    {
    }

    public class InheritedClassWithTypeXamlDeferLoadAttribute : ClassWithTypeXamlDeferLoadAttribute
    {
    }

    [Fact]
    public void LookupInvoker_Unknown_ReturnsExpected()
    {
        var type = new SubXamlType("namespace", "name", null, new XamlSchemaContext());
        Assert.Null(type.LookupInvokerEntry());
        Assert.Equal(XamlTypeInvoker.UnknownInvoker, type.Invoker);
    }

    [Fact]
    public void LookupInvoker_UnderlyingType_ReturnsExpected()
    {
        var type = new SubXamlType(typeof(int), new XamlSchemaContext());
        Assert.NotNull(type.LookupInvokerEntry());
        Assert.NotNull(type.Invoker);
    }

    [Fact]
    public void LookupInvoker_NullResult_ReturnsExpected()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupInvokerResult = null
        };
        Assert.Null(type.LookupInvokerEntry());
        Assert.Equal(XamlTypeInvoker.UnknownInvoker, type.Invoker);
    }

    public static IEnumerable<object[]> LookupIsAmbient_TestData()
    {
        yield return new object[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType("name", null, new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(int), new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(object), new XamlSchemaContext()), false };
        yield return new object[] { new NoUnderlyingOrBaseType(), false };

        // Has provider.
        yield return new object[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    IsDefinedAction = (attributeType, inherit) => true
                }
            },
            true
        };
        yield return new object[]
        {
            new CustomXamlType("namespace", "name", null, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    IsDefinedAction = (attributeType, inherit) => true
                }
            },
            false
        };

        // Has attribute.
        yield return new object[]
        {
            new SubXamlType(typeof(ClassWithAmbientAttribute), new XamlSchemaContext()),
            true
        };
        yield return new object[]
        {
            new SubXamlType(typeof(InheritedClassWithAmbientAttribute), new XamlSchemaContext()),
            true
        };
        yield return new object[]
        {
            new SubXamlType(new ReflectionOnlyCustomAttributeDataType(typeof(ClassWithAmbientAttribute)), new XamlSchemaContext()),
            true
        };
        yield return new object[]
        {
            new SubXamlType(new ThrowsCustomAttributeFormatExceptionDelegator(typeof(int)), new XamlSchemaContext()),
            false
        };

        yield return new object[]
        {
            new CustomXamlType(typeof(ClassWithAmbientAttribute), new XamlSchemaContext())
            {
                LookupIsAmbientResult = false
            },
            false
        };
    }

    [Theory]
    [MemberData(nameof(LookupIsAmbient_TestData))]
    public void LookupIsAmbient_Invoke_ReturnsExpected(SubXamlType type, bool expected)
    {
        Assert.Equal(expected, type.LookupIsAmbientEntry());
        Assert.Equal(expected, type.IsAmbient);
    }

    [Ambient]
    private class ClassWithAmbientAttribute
    {
    }

    private class InheritedClassWithAmbientAttribute : ClassWithAmbientAttribute
    {
    }

    public static IEnumerable<object[]> LookupIsConstructible_TestData()
    {
        yield return new object[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), true };
        yield return new object[] { new SubXamlType("name", null, new XamlSchemaContext()), true };
        yield return new object[] { new NoUnderlyingOrBaseType(), true };

        yield return new object[] { new SubXamlType(typeof(int), new XamlSchemaContext()), true };
        yield return new object[] { new SubXamlType(typeof(AbstractClass), new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(IConvertible), new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(NestedClass), new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(List<>), new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(List<>).GetTypeInfo().GenericTypeParameters[0], new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(List<int>), new XamlSchemaContext()), true };
        yield return new object[] { new SubXamlType(typeof(ClassWithDefaultConstructor), new XamlSchemaContext()), true };
        yield return new object[] { new SubXamlType(typeof(ClassWithInternalDefaultConstructor), new XamlSchemaContext()), true };
        yield return new object[] { new SubXamlType(typeof(ClassWithProtectedDefaultConstructor), new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(ClassWithPrivateDefaultConstructor), new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(ClassWithCustomConstructor), new XamlSchemaContext()), true };
        yield return new object[] { new SubXamlType(typeof(StaticClass), new XamlSchemaContext()), false };

        yield return new object[]
        {
            new CustomXamlType(typeof(ClassWithCustomConstructor), new XamlSchemaContext())
            {
                LookupIsConstructibleResult = false
            },
            false
        };
    }

    [Theory]
    [MemberData(nameof(LookupIsConstructible_TestData))]
    public void LookupIsConstructible_Invoke_ReturnsExpected(SubXamlType type, bool expected)
    {
        Assert.Equal(expected, type.LookupIsConstructibleEntry());
        Assert.Equal(expected, type.IsConstructible);
    }

    public static IEnumerable<object[]> LookupIsMarkupExtension_TestData()
    {
        yield return new object[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType("name", null, new XamlSchemaContext()), false };
        yield return new object[] { new NoUnderlyingOrBaseType(), false };

        yield return new object[] { new SubXamlType(typeof(MarkupExtension), new XamlSchemaContext()), true };
        yield return new object[] { new SubXamlType(typeof(ArrayExtension), new XamlSchemaContext()), true };
        yield return new object[] { new SubXamlType(typeof(int), new XamlSchemaContext()), false };

        yield return new object[]
        {
            new CustomXamlType(typeof(MarkupExtension), new XamlSchemaContext())
            {
                LookupIsMarkupExtensionResult = false
            },
            false
        };
    }

    [Theory]
    [MemberData(nameof(LookupIsMarkupExtension_TestData))]
    public void LookupIsMarkupExtension_Invoke_ReturnsExpected(SubXamlType type, bool expected)
    {
        Assert.Equal(expected, type.LookupIsMarkupExtensionEntry());
        Assert.Equal(expected, type.IsMarkupExtension);
    }

    public static IEnumerable<object?[]> LookupIsNameScope_TestData()
    {
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), false };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), false };
        yield return new object?[] { new NoUnderlyingOrBaseType(), false };

        yield return new object?[] { new SubXamlType(typeof(INameScope), new XamlSchemaContext()), true };
        yield return new object?[] { new SubXamlType(typeof(INameScopeDictionary), new XamlSchemaContext()), true };
        yield return new object?[] { new SubXamlType(typeof(CustomNameScope), new XamlSchemaContext()), true };
        yield return new object?[] { new SubXamlType(typeof(int), new XamlSchemaContext()), false };

        yield return new object?[]
        {
            new CustomXamlType(typeof(INameScope), new XamlSchemaContext())
            {
                LookupIsNameScopeResult = false
            },
            false
        };
    }

    [Theory]
    [MemberData(nameof(LookupIsNameScope_TestData))]
    public void LookupIsNameScope_Invoke_ReturnsExpected(SubXamlType type, bool expected)
    {
        Assert.Equal(expected, type.LookupIsNameScopeEntry());
        Assert.Equal(expected, type.IsNameScope);
    }

#pragma warning disable CS8644
    private class CustomNameScopeDictionary : Dictionary<string, object>, INameScopeDictionary
    {
        public void RegisterName(string name, object scopedElement)
        {
        }

        public void UnregisterName(string name)
        {
        }

        public object FindName(string name) => null!;
    }
#pragma warning restore CS8644

    private class CustomNameScope : INameScope
    {
        public void RegisterName(string name, object scopedElement)
        {
        }

        public void UnregisterName(string name)
        {
        }

        public object FindName(string name) => null!;
    }

    public static IEnumerable<object[]> LookupIsNullable_TestData()
    {
        yield return new object[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), true };
        yield return new object[] { new SubXamlType("name", null, new XamlSchemaContext()), true };
        yield return new object[] { new NoUnderlyingOrBaseType(), true };

        yield return new object[] { new SubXamlType(typeof(int?), new XamlSchemaContext()), true };
        yield return new object[] { new SubXamlType(typeof(System.Nullable<>), new XamlSchemaContext()), true };
        yield return new object[] { new SubXamlType(typeof(Nullable<int>), new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(Generic<int>), new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(KeyValuePair<int, string>), new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(int), new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(string), new XamlSchemaContext()), true };
        yield return new object[] { new SubXamlType(typeof(List<int>), new XamlSchemaContext()), true };
        yield return new object[]
        {
            new SubXamlType(new ReflectionOnlyType(typeof(Nullable<int>))
            {
                AssemblyDelegator = new CustomAssembly(typeof(System.Nullable<>).Assembly)
                {
                    ReflectionOnlyResult = true
                }
            },
            new XamlSchemaContext()), false
        };

        yield return new object[]
        {
            new CustomXamlType(typeof(int?), new XamlSchemaContext())
            {
                LookupIsNullableResult = false
            },
            false
        };
    }

    [Theory]
    [MemberData(nameof(LookupIsNullable_TestData))]
    public void LookupIsNullable_Invoke_ReturnsExpected(SubXamlType type, bool expected)
    {
        Assert.Equal(expected, type.LookupIsNullableEntry());
        Assert.Equal(expected, type.IsNullable);
    }

    private struct Generic<T> { }
    private struct Nullable<T> { }

    public static IEnumerable<object[]> LookupIsPublic_TestData()
    {
        yield return new object[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), true };
        yield return new object[] { new SubXamlType("name", null, new XamlSchemaContext()), true };
        yield return new object[] { new NoUnderlyingOrBaseType(), true };

        yield return new object[] { new SubXamlType(typeof(int), new XamlSchemaContext()), true };
        yield return new object[] { new SubXamlType(typeof(Generic<>), new XamlSchemaContext()), false };

        yield return new object[]
        {
            new CustomXamlType(typeof(INameScope), new XamlSchemaContext())
            {
                LookupIsPublicResult = false
            },
            false
        };
    }

    [Theory]
    [MemberData(nameof(LookupIsPublic_TestData))]
    public void LookupIsPublic_Invoke_ReturnsExpected(SubXamlType type, bool expected)
    {
        Assert.Equal(expected, type.LookupIsPublicEntry());
        Assert.Equal(expected, type.IsPublic);
    }

    public static IEnumerable<object[]> LookupIsUnknown_TestData()
    {
        yield return new object[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), true, true };
        yield return new object[] { new SubXamlType("name", null, new XamlSchemaContext()), true, true };
        yield return new object[] { new NoUnderlyingOrBaseType(), false, false };

        yield return new object[] { new SubXamlType(typeof(int), new XamlSchemaContext()), false, false };

        yield return new object[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupIsUnknownResult = true
            },
            true, false
        };
    }

    [Theory]
    [MemberData(nameof(LookupIsUnknown_TestData))]
    public void LookupIsUnknown_Invoke_ReturnsExpected(SubXamlType type, bool expectedLookup, bool expectedIs)
    {
        Assert.Equal(expectedLookup, type.LookupIsUnknownEntry());
        Assert.Equal(expectedIs, type.IsUnknown);
    }

    public static IEnumerable<object[]> LookupIsWhitespaceSignificantCollection_TestData()
    {
        yield return new object[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), false, true };
        yield return new object[] { new SubXamlType("name", null, new XamlSchemaContext()), false, true };
        yield return new object[] { new SubXamlType(typeof(int), new XamlSchemaContext()), false, false };
        yield return new object[] { new SubXamlType(typeof(object), new XamlSchemaContext()), false, false };
        yield return new object[] { new NoUnderlyingOrBaseType(), false, false };

        // Has provider.
        yield return new object[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    IsDefinedAction = (attributeType, inherit) => true
                }
            },
            true, true
        };
        yield return new object[]
        {
            new CustomXamlType("namespace", "name", null, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    IsDefinedAction = (attributeType, inherit) => true
                }
            },
            false, true
        };

        // Has attribute.
        yield return new object[]
        {
            new SubXamlType(typeof(ClassWithWhitespaceSignificantCollectionAttribute), new XamlSchemaContext()),
            true, true
        };
        yield return new object[]
        {
            new SubXamlType(typeof(InheritedClassWithWhitespaceSignificantCollectionAttribute), new XamlSchemaContext()),
            true, true
        };
        yield return new object[]
        {
            new SubXamlType(new ReflectionOnlyCustomAttributeDataType(typeof(ClassWithWhitespaceSignificantCollectionAttribute)), new XamlSchemaContext()),
            true, true
        };
        yield return new object[]
        {
            new SubXamlType(new ThrowsCustomAttributeFormatExceptionDelegator(typeof(int)), new XamlSchemaContext()),
            false, false
        };

        yield return new object[]
        {
            new CustomXamlType(typeof(ClassWithWhitespaceSignificantCollectionAttribute), new XamlSchemaContext())
            {
                LookupIsWhitespaceSignificantCollectionResult = false
            },
            false, false
        };
    }

    [Theory]
    [MemberData(nameof(LookupIsWhitespaceSignificantCollection_TestData))]
    public void LookupIsWhitespaceSignificantCollection_Invoke_ReturnsExpected(SubXamlType type, bool expectedLookup, bool expectedIs)
    {
        Assert.Equal(expectedLookup, type.LookupIsWhitespaceSignificantCollectionEntry());
        Assert.Equal(expectedIs, type.IsWhitespaceSignificantCollection);
    }

    [WhitespaceSignificantCollection]
    private class ClassWithWhitespaceSignificantCollectionAttribute
    {
    }

    private class InheritedClassWithWhitespaceSignificantCollectionAttribute : ClassWithWhitespaceSignificantCollectionAttribute
    {
    }

    public static IEnumerable<object[]> LookupIsXData_TestData()
    {
        yield return new object[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType("name", null, new XamlSchemaContext()), false };
        yield return new object[] { new NoUnderlyingOrBaseType(), false };

        yield return new object[] { new SubXamlType(typeof(IXmlSerializable), new XamlSchemaContext()), true };
        yield return new object[] { new SubXamlType(typeof(int), new XamlSchemaContext()), false };

        yield return new object[]
        {
            new CustomXamlType(typeof(IXmlSerializable), new XamlSchemaContext())
            {
                LookupIsXDataResult = false
            },
            false
        };
    }

    [Theory]
    [MemberData(nameof(LookupIsXData_TestData))]
    public void LookupIsXData_Invoke_ReturnsExpected(SubXamlType type, bool expected)
    {
        Assert.Equal(expected, type.LookupIsXDataEntry());
        Assert.Equal(expected, type.IsXData);
    }

    public static IEnumerable<object?[]> LookupItemType_TestData()
    {
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(int), new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(object), new XamlSchemaContext()), null };
        yield return new object?[] { new NoUnderlyingOrBaseType(), null };

        yield return new object?[] { new SubXamlType(typeof(int[]), new XamlSchemaContext()), new XamlType(typeof(int), new XamlSchemaContext()) };
        yield return new object?[] { new SubXamlType(typeof(IList), new XamlSchemaContext()), new XamlType(typeof(object), new XamlSchemaContext()) };
        yield return new object?[] { new SubXamlType(typeof(List<int>), new XamlSchemaContext()), new XamlType(typeof(int), new XamlSchemaContext()) };
        yield return new object?[] { new SubXamlType(typeof(AmbiguousIList), new XamlSchemaContext()), new XamlType(typeof(object), new XamlSchemaContext()) };
        yield return new object?[] { new SubXamlType(typeof(AmbiguousICollection), new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(MultiICollectionImplementer), new XamlSchemaContext()), new XamlType(typeof(object), new XamlSchemaContext()) };
        yield return new object?[] { new SubXamlType(typeof(GetEnumeratorClass), new XamlSchemaContext()), new XamlType(typeof(object), new XamlSchemaContext()) };
        yield return new object?[] { new SubXamlType(typeof(IDictionary), new XamlSchemaContext()), new XamlType(typeof(object), new XamlSchemaContext()) };
        yield return new object?[] { new SubXamlType(typeof(Dictionary<int, string>), new XamlSchemaContext()), new XamlType(typeof(string), new XamlSchemaContext()) };
        yield return new object?[] { new SubXamlType(typeof(AmbiguousIDictionary), new XamlSchemaContext()), new XamlType(typeof(object), new XamlSchemaContext()) };

        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupBaseTypeResult = new XamlType("namespace", "name", null, new XamlSchemaContext())
            },
            null
        };
        yield return new object?[]
        {
            new CustomXamlType(typeof(List<int>), new XamlSchemaContext())
            {
                LookupItemTypeResult = null
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupItemType_TestData))]
    public void LookupItemType_Invoke_ReturnsExpected(SubXamlType type, XamlType expected)
    {
        Assert.Equal(expected, type.LookupItemTypeEntry());
        Assert.Equal((type.IsArray || type.IsCollection || type.IsDictionary) ? expected ?? XamlLanguage.Object : null, type.ItemType);
    }

    [Theory]
    [InlineData(typeof(MultipleICollection))]
    [InlineData(typeof(MultipleIDictionary))]
    public void LookupItemType_MultipleAddMethods_ThrowsXamlSchemaException(Type underlyingType)
    {
        var type = new SubXamlType(underlyingType, new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => type.LookupItemTypeEntry());
        Assert.Throws<XamlSchemaException>(() => type.ItemType);
    }

    public static IEnumerable<object?[]> LookupKeyType_TestData()
    {
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(int), new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(object), new XamlSchemaContext()), null };
        yield return new object?[] { new NoUnderlyingOrBaseType(), null };

        yield return new object?[] { new SubXamlType(typeof(int[]), new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(IList), new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(List<int>), new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(AmbiguousIList), new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(AmbiguousICollection), new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(MultiICollectionImplementer), new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(GetEnumeratorClass), new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(IDictionary), new XamlSchemaContext()), new XamlType(typeof(object), new XamlSchemaContext()) };
        yield return new object?[] { new SubXamlType(typeof(Dictionary<int, string>), new XamlSchemaContext()), new XamlType(typeof(int), new XamlSchemaContext()) };
        yield return new object?[] { new SubXamlType(typeof(AmbiguousIDictionary), new XamlSchemaContext()), new XamlType(typeof(object), new XamlSchemaContext()) };

        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupBaseTypeResult = new XamlType("namespace", "name", null, new XamlSchemaContext())
            },
            null
        };
        yield return new object?[]
        {
            new CustomXamlType(typeof(Dictionary<int, string>), new XamlSchemaContext())
            {
                LookupKeyTypeResult = null
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupKeyType_TestData))]
    public void LookupKeyType_Invoke_ReturnsExpected(SubXamlType type, XamlType expected)
    {
        Assert.Equal(expected, type.LookupKeyTypeEntry());
        Assert.Equal(type.IsDictionary ? expected ?? XamlLanguage.Object : null, type.KeyType);
    }

    [Theory]
    [InlineData(typeof(MultipleICollection))]
    [InlineData(typeof(MultipleIDictionary))]
    public void LookupKeyType_MultipleAddMethods_ThrowsXamlSchemaException(Type underlyingType)
    {
        var type = new SubXamlType(underlyingType, new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => type.LookupKeyTypeEntry());
        if (type.IsDictionary)
        {
            Assert.Throws<XamlSchemaException>(() => type.KeyType);
        }
        else
        {
            Assert.Null(type.KeyType);
        }
    }

#pragma warning disable IDE0060, IDE0051 // Remove unused parameter, Remove unused member
    private class MultipleICollection : ICollection<object>, ICollection<int>
    {
        int ICollection<object>.Count => throw new NotImplementedException();

        int ICollection<int>.Count => throw new NotImplementedException();

        bool ICollection<object>.IsReadOnly => throw new NotImplementedException();

        bool ICollection<int>.IsReadOnly => throw new NotImplementedException();

        void ICollection<object>.Add(object item) => throw new NotImplementedException();

        void ICollection<int>.Add(int item) => throw new NotImplementedException();

        void ICollection<object>.Clear() => throw new NotImplementedException();

        void ICollection<int>.Clear() => throw new NotImplementedException();

        bool ICollection<object>.Contains(object item) => throw new NotImplementedException();

        bool ICollection<int>.Contains(int item) => throw new NotImplementedException();

        void ICollection<object>.CopyTo(object[] array, int arrayIndex) => throw new NotImplementedException();

        void ICollection<int>.CopyTo(int[] array, int arrayIndex) => throw new NotImplementedException();

        IEnumerator<object> IEnumerable<object>.GetEnumerator() => throw new NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

        IEnumerator<int> IEnumerable<int>.GetEnumerator() => throw new NotImplementedException();

        bool ICollection<object>.Remove(object item) => throw new NotImplementedException();

        bool ICollection<int>.Remove(int item) => throw new NotImplementedException();
    }

    private class MultipleIDictionary : IDictionary<string, int>, IDictionary<int, string>
    {
        int IDictionary<string, int>.this[string key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        string IDictionary<int, string>.this[int key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        ICollection<string> IDictionary<string, int>.Keys => throw new NotImplementedException();

        ICollection<int> IDictionary<int, string>.Keys => throw new NotImplementedException();

        ICollection<int> IDictionary<string, int>.Values => throw new NotImplementedException();

        ICollection<string> IDictionary<int, string>.Values => throw new NotImplementedException();

        int ICollection<KeyValuePair<string, int>>.Count => throw new NotImplementedException();

        int ICollection<KeyValuePair<int, string>>.Count => throw new NotImplementedException();

        bool ICollection<KeyValuePair<string, int>>.IsReadOnly => throw new NotImplementedException();

        bool ICollection<KeyValuePair<int, string>>.IsReadOnly => throw new NotImplementedException();

        void IDictionary<string, int>.Add(string key, int value) => throw new NotImplementedException();

        void ICollection<KeyValuePair<string, int>>.Add(KeyValuePair<string, int> item) => throw new NotImplementedException();

        void IDictionary<int, string>.Add(int key, string value) => throw new NotImplementedException();

        void ICollection<KeyValuePair<int, string>>.Add(KeyValuePair<int, string> item) => throw new NotImplementedException();

        void ICollection<KeyValuePair<string, int>>.Clear() => throw new NotImplementedException();

        void ICollection<KeyValuePair<int, string>>.Clear() => throw new NotImplementedException();

        bool ICollection<KeyValuePair<string, int>>.Contains(KeyValuePair<string, int> item) => throw new NotImplementedException();

        bool ICollection<KeyValuePair<int, string>>.Contains(KeyValuePair<int, string> item) => throw new NotImplementedException();

        bool IDictionary<string, int>.ContainsKey(string key) => throw new NotImplementedException();

        bool IDictionary<int, string>.ContainsKey(int key) => throw new NotImplementedException();

        void ICollection<KeyValuePair<string, int>>.CopyTo(KeyValuePair<string, int>[] array, int arrayIndex) => throw new NotImplementedException();

        void ICollection<KeyValuePair<int, string>>.CopyTo(KeyValuePair<int, string>[] array, int arrayIndex) => throw new NotImplementedException();

        IEnumerator<KeyValuePair<string, int>> IEnumerable<KeyValuePair<string, int>>.GetEnumerator() => throw new NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

        IEnumerator<KeyValuePair<int, string>> IEnumerable<KeyValuePair<int, string>>.GetEnumerator() => throw new NotImplementedException();

        bool IDictionary<string, int>.Remove(string key) => throw new NotImplementedException();

        bool ICollection<KeyValuePair<string, int>>.Remove(KeyValuePair<string, int> item) => throw new NotImplementedException();

        bool IDictionary<int, string>.Remove(int key) => throw new NotImplementedException();

        bool ICollection<KeyValuePair<int, string>>.Remove(KeyValuePair<int, string> item) => throw new NotImplementedException();

        bool IDictionary<string, int>.TryGetValue(string key, out int value) => throw new NotImplementedException();

        bool IDictionary<int, string>.TryGetValue(int key, out string value) => throw new NotImplementedException();
    }

    private class AmbiguousICollection : ICollection
    {
        public int Count => throw new NotImplementedException();

        public object SyncRoot => throw new NotImplementedException();

        public bool IsSynchronized => throw new NotImplementedException();

        public void CopyTo(Array array, int index) => throw new NotImplementedException();

        public IEnumerator GetEnumerator() => throw new NotImplementedException();
    }

    private class AmbiguousIList : AmbiguousICollection, IList
    {
        public void Add(int value)
        {
        }

        public void Add(string value)
        {
        }

        public object? this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsReadOnly => throw new NotImplementedException();

        public bool IsFixedSize => throw new NotImplementedException();

        public int Add(object? value) => throw new NotImplementedException();

        public void Clear() => throw new NotImplementedException();

        public bool Contains(object? value) => throw new NotImplementedException();

        public int IndexOf(object? value) => throw new NotImplementedException();

        public void Insert(int index, object? value) => throw new NotImplementedException();

        public void Remove(object? value) => throw new NotImplementedException();

        public void RemoveAt(int index) => throw new NotImplementedException();
    }

    private class AmbiguousIDictionary : IDictionary
    {
        public void Add(string key, int value)
        {
        }

        public void Add(int key, string value)
        {
        }

        public object? this[object key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ICollection Keys => throw new NotImplementedException();

        public ICollection Values => throw new NotImplementedException();

        public bool IsReadOnly => throw new NotImplementedException();

        public bool IsFixedSize => throw new NotImplementedException();

        public int Count => throw new NotImplementedException();

        public object SyncRoot => throw new NotImplementedException();

        public bool IsSynchronized => throw new NotImplementedException();

        public void Add(object key, object? value) => throw new NotImplementedException();

        public void Clear() => throw new NotImplementedException();

        public bool Contains(object key) => throw new NotImplementedException();

        public void CopyTo(Array array, int index) => throw new NotImplementedException();

        public IDictionaryEnumerator GetEnumerator() => throw new NotImplementedException();

        public void Remove(object key) => throw new NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }
#pragma warning restore IDE0060, IDE0051 // Remove unused parameter, Remove unused member

    public static IEnumerable<object?[]> LookupMarkupExtensionReturnType_TestData()
    {
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(int), new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(object), new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(MarkupExtension), new XamlSchemaContext()), null };
        yield return new object?[] { new NoUnderlyingOrBaseType(), null };

        // Has provider.
        yield return new object?[]
        {
            new CustomXamlType(typeof(MarkupExtension), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new MarkupExtensionReturnTypeAttribute(typeof(int)) }
                }
            },
            new XamlType(typeof(int), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new CustomXamlType(typeof(MarkupExtension), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => Array.Empty<object>()
                }
            },
            null
        };
        yield return new object?[]
        {
            new CustomXamlType("namespace", "name", null, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new MarkupExtensionReturnTypeAttribute(typeof(int)) }
                }
            },
            null
        };

        // Has attribute.
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithMarkupExtensionReturnTypeAttribute), new XamlSchemaContext()),
            new XamlType(typeof(int), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(InheritedClassWithMarkupExtensionReturnTypeAttribute), new XamlSchemaContext()),
            new XamlType(typeof(int), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlType(new ReflectionOnlyCustomAttributeDataType(typeof(ClassWithMarkupExtensionReturnTypeAttribute)), new XamlSchemaContext()),
            new XamlType(typeof(int), new XamlSchemaContext())
        };
        yield return new object?[]
        {
            new SubXamlType(new ThrowsCustomAttributeFormatExceptionDelegator(typeof(MarkupExtension)), new XamlSchemaContext()),
            null
        };

        yield return new object?[]
        {
            new CustomXamlType(typeof(ClassWithMarkupExtensionReturnTypeAttribute), new XamlSchemaContext())
            {
                LookupMarkupExtensionReturnTypeResult = null
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupMarkupExtensionReturnType_TestData))]
    public void LookupMarkupExtensionReturnType_Invoke_ReturnsExpected(SubXamlType type, XamlType expected)
    {
        Assert.Equal(expected, type.LookupMarkupExtensionReturnTypeEntry());
        Assert.Equal(type.IsMarkupExtension ? expected ?? XamlLanguage.Object : null, type.MarkupExtensionReturnType);
    }

    [Fact]
    public void LookupMarkupExtensionReturnType_NullTypeInAttribute_ThrowsXamlSchemaException()
    {
        var type = new SubXamlType(typeof(ClassWithNullMarkupExtensionReturnTypeAttribute), new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => type.LookupMarkupExtensionReturnTypeEntry());
        Assert.Throws<XamlSchemaException>(() => type.MarkupExtensionReturnType);
    }

    [Fact]
    public void LookupMarkupExtensionReturnType_NullAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(MarkupExtension), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => null!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupMarkupExtensionReturnTypeEntry());
        Assert.Throws<NullReferenceException>(() => type.MarkupExtensionReturnType);
    }

    [Fact]
    public void LookupMarkupExtensionReturnType_NullItemInAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(MarkupExtension), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object?[] { null }!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupMarkupExtensionReturnTypeEntry());
        Assert.Throws<NullReferenceException>(() => type.MarkupExtensionReturnType);
    }

    [Fact]
    public void LookupMarkupExtensionReturnType_InvalidTypeItemInAttributeResult_ThrowsInvalidCastException()
    {
        var type = new CustomXamlType(typeof(MarkupExtension), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object[] { new object() }
            }
        };
        Assert.Throws<InvalidCastException>(() => type.LookupMarkupExtensionReturnTypeEntry());
        Assert.Throws<InvalidCastException>(() => type.MarkupExtensionReturnType);
    }

    [MarkupExtensionReturnType(typeof(int))]
    private class ClassWithMarkupExtensionReturnTypeAttribute : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => null!;
    }

    private class InheritedClassWithMarkupExtensionReturnTypeAttribute : ClassWithMarkupExtensionReturnTypeAttribute
    {
    }

    [MarkupExtensionReturnType(null)]
    private class ClassWithNullMarkupExtensionReturnTypeAttribute : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => null!;
    }

    [MarkupExtensionReturnType]
    private class ClassWihDefaultMarkupExtensionReturnTypeAttribute : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => null!;
    }

#pragma warning disable 0618
    [MarkupExtensionReturnType(typeof(int), typeof(string))]
#pragma warning restore 0618
    private class ClassWihExpressionTypeMarkupExtensionReturnTypeAttribute : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => null!;
    }

    public static IEnumerable<object?[]> LookupMember_TestData()
    {
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), "name", false, null, null };
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), "", true, null, null };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), "name", false, null, null };
        yield return new object?[] { new SubXamlType(typeof(int), new XamlSchemaContext()), "name", false, null, null };
        yield return new object?[] { new SubXamlType(typeof(object), new XamlSchemaContext()), "name", false, null, null };
        yield return new object?[] { new NoUnderlyingOrBaseType(), "name", true, null, null };
        yield return new object?[] { new NoUnderlyingOrBaseType(), "name", false, null, null };

        // Property.
        foreach (bool skipReadOnlyCheck in new bool[] { true, false })
        {
            yield return new object?[]
            {
                new SubXamlType(typeof(MembersDataClass), new XamlSchemaContext()),
                "Property", skipReadOnlyCheck,
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.Property)), new XamlSchemaContext()),
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.Property)), new XamlSchemaContext())
            };
            yield return new object?[]
            {
                new SubXamlType(typeof(MembersDataClass), new XamlSchemaContext()),
                "GetOnlyListProperty", skipReadOnlyCheck,
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.GetOnlyListProperty)), new XamlSchemaContext()),
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.GetOnlyListProperty)), new XamlSchemaContext())
            };
            yield return new object?[]
            {
                new SubXamlType(typeof(MembersDataClass), new XamlSchemaContext()),
                "GetOnlyDictionaryProperty", skipReadOnlyCheck,
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.GetOnlyDictionaryProperty)), new XamlSchemaContext()),
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.GetOnlyDictionaryProperty)), new XamlSchemaContext())
            };
            yield return new object?[]
            {
                new SubXamlType(typeof(MembersDataClass), new XamlSchemaContext()),
                "GetXDataProperty", skipReadOnlyCheck,
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.GetXDataProperty)), new XamlSchemaContext()),
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.GetXDataProperty)), new XamlSchemaContext())
            };
            yield return new object?[]
            {
                new SubXamlType(typeof(MembersDataClass), new XamlSchemaContext()),
                "SetOnlyProperty", skipReadOnlyCheck,
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.SetOnlyProperty)), new XamlSchemaContext()),
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.SetOnlyProperty)), new XamlSchemaContext())
            };
            yield return new object?[]
            {
                new SubXamlType(typeof(MembersDataClass), new XamlSchemaContext()),
                "PrivateGetProperty", skipReadOnlyCheck,
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.PrivateGetProperty)), new XamlSchemaContext()),
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.PrivateGetProperty)), new XamlSchemaContext())
            };
            yield return new object?[]
            {
                new SubXamlType(typeof(MembersDataClass), new XamlSchemaContext()),
                "PrivateSetProperty", skipReadOnlyCheck,
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.PrivateSetProperty)), new XamlSchemaContext()),
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.PrivateSetProperty)), new XamlSchemaContext())
            };
            yield return new object?[]
            {
                new SubXamlType(typeof(MoreDerivedShadowedDataClass), new XamlSchemaContext()),
                "Property", skipReadOnlyCheck,
                new XamlMember(typeof(MoreDerivedShadowedDataClass).GetProperty(nameof(MoreDerivedShadowedDataClass.Property), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!, new XamlSchemaContext()),
                new XamlMember(typeof(MoreDerivedShadowedDataClass).GetProperty(nameof(MoreDerivedShadowedDataClass.Property), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!, new XamlSchemaContext())
            };
            yield return new object?[]
            {
                new SubXamlType(new CustomType(typeof(EvenMoreDerivedShadowedBaseClass))
                {
                    GetMemberResult = new MemberInfo[]
                    {
                        typeof(ShadowedBaseClass).GetProperty(nameof(ShadowedBaseClass.Property), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!,
                        typeof(MoreDerivedShadowedDataClass).GetProperty(nameof(MoreDerivedShadowedDataClass.Property), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!,
                        typeof(EvenMoreDerivedShadowedBaseClass).GetProperty(nameof(EvenMoreDerivedShadowedBaseClass.Property), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!
                    }
                }, new XamlSchemaContext()),
                "Property", skipReadOnlyCheck,
                new XamlMember(typeof(EvenMoreDerivedShadowedBaseClass).GetProperty(nameof(EvenMoreDerivedShadowedBaseClass.Property), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!, new XamlSchemaContext()),
                new XamlMember(typeof(EvenMoreDerivedShadowedBaseClass).GetProperty(nameof(EvenMoreDerivedShadowedBaseClass.Property), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!, new XamlSchemaContext())
            };
            yield return new object?[]
            {
                new SubXamlType(new CustomType(typeof(EvenMoreDerivedShadowedBaseClass))
                {
                    GetMemberResult = new MemberInfo[]
                    {
                        typeof(ShadowedBaseClass).GetProperty(nameof(ShadowedBaseClass.Property), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!,
                        typeof(EvenMoreDerivedShadowedBaseClass).GetProperty(nameof(EvenMoreDerivedShadowedBaseClass.Property), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!,
                        typeof(MoreDerivedShadowedDataClass).GetProperty(nameof(MoreDerivedShadowedDataClass.Property), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!,
                    }
                }, new XamlSchemaContext()),
                "Property", skipReadOnlyCheck,
                new XamlMember(typeof(EvenMoreDerivedShadowedBaseClass).GetProperty(nameof(EvenMoreDerivedShadowedBaseClass.Property)!, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), new XamlSchemaContext()),
                new XamlMember(typeof(EvenMoreDerivedShadowedBaseClass).GetProperty(nameof(EvenMoreDerivedShadowedBaseClass.Property)!, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly), new XamlSchemaContext())
            };
            yield return new object?[]
            {
                new SubXamlType(typeof(MembersDataClass), new XamlSchemaContext()),
                "Item", skipReadOnlyCheck,
                null,
                null
            };
            yield return new object?[]
            {
                new SubXamlType(typeof(MembersDataClass), new XamlSchemaContext()),
                "ProtectedProperty", skipReadOnlyCheck,
                new XamlMember(typeof(MembersDataClass).GetProperty("ProtectedProperty", BindingFlags.Instance | BindingFlags.NonPublic)!, new XamlSchemaContext()),
                new XamlMember(typeof(MembersDataClass).GetProperty("ProtectedProperty", BindingFlags.Instance | BindingFlags.NonPublic)!, new XamlSchemaContext())
            };
            yield return new object?[]
            {
                new SubXamlType(typeof(MembersDataClass), new XamlSchemaContext()),
                "InternalProperty", skipReadOnlyCheck,
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.InternalProperty), BindingFlags.Instance | BindingFlags.NonPublic)!, new XamlSchemaContext()),
                new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.InternalProperty), BindingFlags.Instance | BindingFlags.NonPublic)!, new XamlSchemaContext())
            };
            yield return new object?[]
            {
                new SubXamlType(typeof(MembersDataClass), new XamlSchemaContext()),
                "PrivateProperty", skipReadOnlyCheck,
                null,
                null
            };
            yield return new object?[]
            {
                new SubXamlType(typeof(MembersDataClass), new XamlSchemaContext()),
                "StaticProperty", skipReadOnlyCheck,
                null,
                null
            };
            yield return new object?[]
            {
                new SubXamlType(typeof(MembersDataClass), new XamlSchemaContext()),
                "NoSuchProperty", skipReadOnlyCheck,
                null,
                null
            };
        }
    
        yield return new object?[]
        {
            new SubXamlType(typeof(MembersDataClass), new XamlSchemaContext()),
            "GetOnlyProperty", false,
            null,
            null
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(MembersDataClass), new XamlSchemaContext()),
            "GetOnlyProperty", true,
            new XamlMember(typeof(MembersDataClass).GetProperty(nameof(MembersDataClass.GetOnlyProperty))!, new XamlSchemaContext()),
            null
        };

        // Event.
        foreach (bool skipReadOnlyCheck in new bool[] { true, false })
        {
            yield return new object?[]
            {
                new SubXamlType(typeof(MembersDataClass), new XamlSchemaContext()),
                "Event", skipReadOnlyCheck,
                new XamlMember(typeof(MembersDataClass).GetEvent(nameof(MembersDataClass.Event))!, new XamlSchemaContext()),
                new XamlMember(typeof(MembersDataClass).GetEvent(nameof(MembersDataClass.Event))!, new XamlSchemaContext())
            };
            yield return new object?[]
            {
                new SubXamlType(typeof(MembersDataClass), new XamlSchemaContext()),
                "PrivateEvent", skipReadOnlyCheck,
                null,
                null
            };
            yield return new object?[]
            {
                new SubXamlType(typeof(MembersDataClass), new XamlSchemaContext()),
                "StaticEvent", skipReadOnlyCheck,
                null,
                null
            };
        }

        yield return new object?[]
        {
            new CustomXamlType(typeof(MembersDataClass), new XamlSchemaContext())
            {
                LookupMemberResult = null
            },
            "name", false,
            null,
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupMember_TestData))]
    public void LookupMember_Invoke_ReturnsExpected(SubXamlType type, string name, bool skipReadOnlyCheck, XamlMember expectedLookup, XamlMember expectedGet)
    {
        Assert.Equal(expectedLookup, type.LookupMemberEntry(name, skipReadOnlyCheck));
        Assert.Equal(expectedGet, type.GetMember(name));
    }

    [Fact]
    public void LookupMember_NullName_ThrowsArgumentNullException()
    {
        var type = new SubXamlType(typeof(int), new XamlSchemaContext());
        Assert.Throws<ArgumentNullException>("name", () => type.LookupMemberEntry(null!, false));
        Assert.Throws<ArgumentNullException>("key", () => type.GetMember(null));
    }

    [Fact]
    public void LookupMember_NullNameUnknownNoBaseType_ThrowsArgumentNullExceptionOnGetter()
    {
        var type = new NoUnderlyingOrBaseType();
        Assert.Null(type.LookupMemberEntry(null!, false));
        Assert.Throws<ArgumentNullException>("key", () => type.GetMember(null));
    }

    public static IEnumerable<object?[]> LookupPositionalParameters_TestData()
    {
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), 1, null };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()),  1, null };
        yield return new object?[] { new SubXamlType(typeof(int), new XamlSchemaContext()),  1, null };
        yield return new object?[] { new SubXamlType(typeof(object), new XamlSchemaContext()),  -1, null };
        yield return new object?[] { new SubXamlType(typeof(MarkupExtension), new XamlSchemaContext()),  1, null };
        yield return new object?[] { new NoUnderlyingOrBaseType(),  1, null };

        yield return new object?[] { new SubXamlType(typeof(ClassWithCustomConstructor), new XamlSchemaContext()), 1, new XamlType[] { new XamlType(typeof(int), new XamlSchemaContext()) } };

        yield return new object?[] { new SubXamlType(typeof(TypeExtension), new XamlSchemaContext()), 2, null };
        yield return new object?[] { new SubXamlType(typeof(TypeExtension), new XamlSchemaContext()), 1, new XamlType[] { new XamlType(typeof(Type), new XamlSchemaContext()) } };
        yield return new object?[] { new SubXamlType(typeof(TypeExtension), new XamlSchemaContext()), 0, null };
        yield return new object?[] { new SubXamlType(typeof(TypeExtension), new XamlSchemaContext()), -1, null };

        var duplicateSupport = new XamlSchemaContextSettings { SupportMarkupExtensionsWithDuplicateArity = true };
        yield return new object?[] { new SubXamlType(typeof(ArrayExtension), new XamlSchemaContext(duplicateSupport)), 2, null };
        yield return new object?[] { new SubXamlType(typeof(ArrayExtension), new XamlSchemaContext(duplicateSupport)), 1, new XamlType[] { new XamlType(typeof(Type), new XamlSchemaContext()) } };
        yield return new object?[] { new SubXamlType(typeof(ArrayExtension), new XamlSchemaContext(duplicateSupport)), 0, Array.Empty<XamlType>() };
        yield return new object?[] { new SubXamlType(typeof(ArrayExtension), new XamlSchemaContext(duplicateSupport)), -1, null };

        yield return new object?[]
        {
            new CustomXamlType(typeof(ArrayExtension), new XamlSchemaContext())
            {
                LookupPositionalParametersResult = null
            },
            0, null
        };
    }

    [Theory]
    [MemberData(nameof(LookupPositionalParameters_TestData))]
    public void LookupPositionalParameters_Invoke_ReturnsExpected(SubXamlType type, int parameterCount, IList<XamlType> expected)
    {
        Assert.Equal(expected, type.LookupPositionalParametersEntry(parameterCount));
        Assert.Equal(expected, type.GetPositionalParameters(parameterCount));
    }

    [Theory]
    [InlineData(typeof(ArrayExtension), 0)]
    [InlineData(typeof(ArrayExtension), 1)]
    [InlineData(typeof(ArrayExtension), 2)]
    public void LookupPositionalParameters_DuplicatesNotSupported_ThrowsXamlSchemaException(Type underlyingType, int parameterCount)
    {
        var type = new SubXamlType(underlyingType, new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => type.LookupPositionalParametersEntry(parameterCount));
        Assert.Throws<XamlSchemaException>(() => type.GetPositionalParameters(parameterCount));
    }

    public static IEnumerable<object?[]> LookupSetMarkupExtensionHandler_TestData()
    {
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(int), new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(object), new XamlSchemaContext()), null };
        yield return new object?[] { new NoUnderlyingOrBaseType(), null };

        // Has provider.
        yield return new object?[]
        {
            new CustomXamlType(typeof(ClassWithXamlSetMarkupExtensionEventArgsDelegate), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new XamlSetMarkupExtensionAttribute("Method") }
                }
            },
            Delegate.CreateDelegate(typeof(EventHandler<XamlSetMarkupExtensionEventArgs>), typeof(ClassWithXamlSetMarkupExtensionEventArgsDelegate), "Method")
        };
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => Array.Empty<object>()
                }
            },
            null
        };
        yield return new object?[]
        {
            new CustomXamlType("namespace", "name", null, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new XamlSetMarkupExtensionAttribute("name") }
                }
            },
            null
        };

        // Has attribute.
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithXamlSetMarkupExtensionAttribute), new XamlSchemaContext()),
            Delegate.CreateDelegate(typeof(EventHandler<XamlSetMarkupExtensionEventArgs>), typeof(ClassWithXamlSetMarkupExtensionAttribute), "Method")
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(InheritedClassWithXamlSetMarkupExtensionAttribute), new XamlSchemaContext()),
            Delegate.CreateDelegate(typeof(EventHandler<XamlSetMarkupExtensionEventArgs>), typeof(InheritedClassWithXamlSetMarkupExtensionAttribute), "Method")
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithNullXamlSetMarkupExtensionAttribute), new XamlSchemaContext()),
            null
        };
        yield return new object?[]
        {
            new SubXamlType(new ThrowsCustomAttributeFormatExceptionDelegator(typeof(int)), new XamlSchemaContext()),
            null
        };

        yield return new object?[]
        {
            new CustomXamlType(typeof(ClassWithXamlSetMarkupExtensionAttribute), new XamlSchemaContext())
            {
                LookupSetMarkupExtensionHandlerResult = null
            },
            null
        };
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupBaseTypeResult = new XamlType("namespace", "name", null, new XamlSchemaContext())
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupSetMarkupExtensionHandler_TestData))]
    public void LookupSetMarkupExtensionHandler_Invoke_ReturnsExpected(SubXamlType type, EventHandler<XamlSetMarkupExtensionEventArgs> expected)
    {
        Assert.Equal(expected, type.LookupSetMarkupExtensionHandlerEntry());
    }

    [Fact]
    public void LookupSetMarkupExtensionHandler_NullAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => null!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupSetMarkupExtensionHandlerEntry());
    }

    [Fact]
    public void LookupSetMarkupExtensionHandler_NullItemInAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object?[] { null }!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupSetMarkupExtensionHandlerEntry());
    }

    [Fact]
    public void LookupSetMarkupExtensionHandler_InvalidTypeItemInAttributeResult_ThrowsInvalidCastException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object[] { new object() }
            }
        };
        Assert.Throws<InvalidCastException>(() => type.LookupSetMarkupExtensionHandlerEntry());
    }

    public static IEnumerable<object[]> LookupSetMarkupExtensionHandler_InvalidAttribute_TestData()
    {
        yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1), new CustomAttributeTypedArgument(typeof(int), 2) } };
        yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1) } };
    }

    [Theory]
    [MemberData(nameof(LookupSetMarkupExtensionHandler_InvalidAttribute_TestData))]
    public void LookupSetMarkupExtensionHandler_InvalidAttribute_ThrowsXamlSchemaException(CustomAttributeTypedArgument[] arguments)
    {
        var type = new SubXamlType(new CustomType(typeof(int))
        {
            GetCustomAttributesDataResult = new CustomAttributeData[]
            {
                new CustomCustomAttributeData
                {
                    ConstructorResult = typeof(XamlSetMarkupExtensionAttribute).GetConstructors()[0],
                    ConstructorArgumentsResult = arguments
                }
            }
        }, new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => type.LookupSetMarkupExtensionHandlerEntry());
    }

    [Fact]
    public void LookupSetMarkupExtensionHandler_NoSuchName_ThrowsArgumentException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object[] { new XamlSetMarkupExtensionAttribute("name") }
            }
        };
        Assert.Throws<ArgumentException>(() => type.LookupSetMarkupExtensionHandlerEntry());
    }

    [Fact]
    public void LookupSetMarkupExtensionHandler_NotRuntimeType_ThrowsArgumentException()
    {
        var type = new SubXamlType(new ReflectionOnlyCustomAttributeDataType(typeof(ClassWithXamlSetMarkupExtensionAttribute)), new XamlSchemaContext());
        Assert.Throws<ArgumentException>("target", () => type.LookupSetMarkupExtensionHandlerEntry());
    }

#pragma warning disable IDE0060, IDE0051, CA1052 // Remove unused parameter, Remove unused private members, Static holder types should be Static or NotInheritable
    private class ClassWithXamlSetMarkupExtensionEventArgsDelegate
    {
        public static void Method(object sender, XamlSetMarkupExtensionEventArgs e)
        {
        }
    }

    [XamlSetMarkupExtension("Method")]
    private class ClassWithXamlSetMarkupExtensionAttribute
    {
        public static void Method(object sender, XamlSetMarkupExtensionEventArgs e)
        {
        }
    }

    private class InheritedClassWithXamlSetMarkupExtensionAttribute : ClassWithXamlSetMarkupExtensionAttribute
    {
    }

    [XamlSetMarkupExtension(null!)]
    private class ClassWithNullXamlSetMarkupExtensionAttribute
    {
    }
#pragma warning restore IDE0060, IDE0051, CA1052 // Remove unused parameter, Remove unused private members, Static holder types should be Static or NotInheritable

    public static IEnumerable<object?[]> LookupSetTypeConverterHandler_TestData()
    {
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(int), new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(object), new XamlSchemaContext()), null };
        yield return new object?[] { new NoUnderlyingOrBaseType(), null };

        // Has provider.
        yield return new object?[]
        {
            new CustomXamlType(typeof(ClassWithXamlSetTypeConverterEventArgsDelegate), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new XamlSetTypeConverterAttribute("Method") }
                }
            },
            Delegate.CreateDelegate(typeof(EventHandler<XamlSetTypeConverterEventArgs>), typeof(ClassWithXamlSetTypeConverterEventArgsDelegate), "Method")
        };
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => Array.Empty<object>()
                }
            },
            null
        };
        yield return new object?[]
        {
            new CustomXamlType("namespace", "name", null, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new XamlSetTypeConverterAttribute("name") }
                }
            },
            null
        };

        // Has attribute.
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithXamlSetTypeConverterAttribute), new XamlSchemaContext()),
            Delegate.CreateDelegate(typeof(EventHandler<XamlSetTypeConverterEventArgs>), typeof(ClassWithXamlSetTypeConverterAttribute), "Method")
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(InheritedClassWithXamlSetTypeConverterAttribute), new XamlSchemaContext()),
            Delegate.CreateDelegate(typeof(EventHandler<XamlSetTypeConverterEventArgs>), typeof(InheritedClassWithXamlSetTypeConverterAttribute), "Method")
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithNullXamlSetTypeConverterAttribute), new XamlSchemaContext()),
            null
        };
        yield return new object?[]
        {
            new SubXamlType(new ThrowsCustomAttributeFormatExceptionDelegator(typeof(int)), new XamlSchemaContext()),
            null
        };

        yield return new object?[]
        {
            new CustomXamlType(typeof(ClassWithXamlSetTypeConverterAttribute), new XamlSchemaContext())
            {
                LookupSetTypeConverterHandlerResult = null
            },
            null
        };
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupBaseTypeResult = new XamlType("namespace", "name", null, new XamlSchemaContext())
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupSetTypeConverterHandler_TestData))]
    public void LookupSetTypeConverterHandler_Invoke_ReturnsExpected(SubXamlType type, EventHandler<XamlSetTypeConverterEventArgs> expected)
    {
        Assert.Equal(expected, type.LookupSetTypeConverterHandlerEntry());
    }

    [Fact]
    public void LookupSetTypeConverterHandlerEntry_NullAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => null!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupSetTypeConverterHandlerEntry());
    }

    [Fact]
    public void LookupSetTypeConverterHandler_NullItemInAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object?[] { null }!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupSetTypeConverterHandlerEntry());
    }

    [Fact]
    public void LookupSetTypeConverterHandler_InvalidTypeItemInAttributeResult_ThrowsInvalidCastException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object[] { new object() }
            }
        };
        Assert.Throws<InvalidCastException>(() => type.LookupSetTypeConverterHandlerEntry());
    }

    public static IEnumerable<object[]> LookupSetTypeConverterHandler_InvalidAttribute_TestData()
    {
        yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1), new CustomAttributeTypedArgument(typeof(int), 2) } };
        yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1) } };
    }

    [Theory]
    [MemberData(nameof(LookupSetTypeConverterHandler_InvalidAttribute_TestData))]
    public void LookupSetTypeConverterHandler_InvalidAttribute_ThrowsXamlSchemaException(CustomAttributeTypedArgument[] arguments)
    {
        var type = new SubXamlType(new CustomType(typeof(int))
        {
            GetCustomAttributesDataResult = new CustomAttributeData[]
            {
                new CustomCustomAttributeData
                {
                    ConstructorResult = typeof(XamlSetTypeConverterAttribute).GetConstructors()[0],
                    ConstructorArgumentsResult = arguments
                }
            }
        }, new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => type.LookupSetTypeConverterHandlerEntry());
    }

    [Fact]
    public void LookupSetTypeConverterHandler_NoSuchName_ThrowsArgumentException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object[] { new XamlSetTypeConverterAttribute("name") }
            }
        };
        Assert.Throws<ArgumentException>(() => type.LookupSetTypeConverterHandlerEntry());
    }

    [Fact]
    public void LookupSetTypeConverterHandler_NotRuntimeType_ThrowsArgumentException()
    {
        var type = new SubXamlType(new ReflectionOnlyCustomAttributeDataType(typeof(ClassWithXamlSetTypeConverterAttribute)), new XamlSchemaContext());
        Assert.Throws<ArgumentException>("target", () => type.LookupSetTypeConverterHandlerEntry());
    }

#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
    private class ClassWithXamlSetTypeConverterEventArgsDelegate
    {
        public static void Method(object sender, XamlSetTypeConverterEventArgs e)
        {
        }
    }

    [XamlSetTypeConverter("Method")]
    private class ClassWithXamlSetTypeConverterAttribute
    {
        public static void Method(object sender, XamlSetTypeConverterEventArgs e)
        {
        }
    }

    private class InheritedClassWithXamlSetTypeConverterAttribute : ClassWithXamlSetTypeConverterAttribute
    {
    }

    [XamlSetTypeConverter(null!)]
    private class ClassWithNullXamlSetTypeConverterAttribute
    {
    }
#pragma warning restore CA1052 // Static holder types should be Static or NotInheritable

    public static IEnumerable<object[]> LookupTrimSurroundingWhitespace_TestData()
    {
        yield return new object[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType("name", null, new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(int), new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(object), new XamlSchemaContext()), false };
        yield return new object[] { new NoUnderlyingOrBaseType(), false };

        // Has provider.
        yield return new object[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    IsDefinedAction = (attributeType, inherit) => true
                }
            },
            true
        };
        yield return new object[]
        {
            new CustomXamlType("namespace", "name", null, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    IsDefinedAction = (attributeType, inherit) => true
                }
            },
            false
        };

        // Has attribute.
        yield return new object[]
        {
            new SubXamlType(typeof(ClassWithTrimSurroundingWhitespaceAttribute), new XamlSchemaContext()),
            true
        };
        yield return new object[]
        {
            new SubXamlType(typeof(InheritedClassWithTrimSurroundingWhitespaceAttribute), new XamlSchemaContext()),
            true
        };
        yield return new object[]
        {
            new SubXamlType(new ReflectionOnlyCustomAttributeDataType(typeof(ClassWithTrimSurroundingWhitespaceAttribute)), new XamlSchemaContext()),
            true
        };
        yield return new object[]
        {
            new SubXamlType(new ThrowsCustomAttributeFormatExceptionDelegator(typeof(int)), new XamlSchemaContext()),
            false
        };

        yield return new object[]
        {
            new CustomXamlType(typeof(ClassWithTrimSurroundingWhitespaceAttribute), new XamlSchemaContext())
            {
                LookupTrimSurroundingWhitespaceResult = false
            },
            false
        };
    }

    [Theory]
    [MemberData(nameof(LookupTrimSurroundingWhitespace_TestData))]
    public void LookupTrimSurroundingWhitespace_Invoke_ReturnsExpected(SubXamlType type, bool expected)
    {
        Assert.Equal(expected, type.LookupTrimSurroundingWhitespaceEntry());
        Assert.Equal(expected, type.TrimSurroundingWhitespace);
    }

    [TrimSurroundingWhitespace]
    private class ClassWithTrimSurroundingWhitespaceAttribute
    {
    }

    private class InheritedClassWithTrimSurroundingWhitespaceAttribute : ClassWithTrimSurroundingWhitespaceAttribute
    {
    }

    public static IEnumerable<object?[]> LookupTypeConverter_TestData()
    {
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(byte), new XamlSchemaContext()), new XamlValueConverter<TypeConverter>(typeof(ByteConverter), null) };
        yield return new object?[] { new SubXamlType(typeof(sbyte), new XamlSchemaContext()), new XamlValueConverter<TypeConverter>(typeof(SByteConverter), null) };
        yield return new object?[] { new SubXamlType(typeof(ushort), new XamlSchemaContext()), new XamlValueConverter<TypeConverter>(typeof(UInt16Converter), null) };
        yield return new object?[] { new SubXamlType(typeof(short), new XamlSchemaContext()), new XamlValueConverter<TypeConverter>(typeof(Int16Converter), null) };
        yield return new object?[] { new SubXamlType(typeof(uint), new XamlSchemaContext()), new XamlValueConverter<TypeConverter>(typeof(UInt32Converter), null) };
        yield return new object?[] { new SubXamlType(typeof(int), new XamlSchemaContext()), new XamlValueConverter<TypeConverter>(typeof(Int32Converter), null) };
        yield return new object?[] { new SubXamlType(typeof(ulong), new XamlSchemaContext()), new XamlValueConverter<TypeConverter>(typeof(UInt64Converter), null) };
        yield return new object?[] { new SubXamlType(typeof(long), new XamlSchemaContext()), new XamlValueConverter<TypeConverter>(typeof(Int64Converter), null) };
        yield return new object?[] { new SubXamlType(typeof(char), new XamlSchemaContext()), new XamlValueConverter<TypeConverter>(typeof(CharConverter), null) };
        yield return new object?[] { new SubXamlType(typeof(bool), new XamlSchemaContext()), new XamlValueConverter<TypeConverter>(typeof(BooleanConverter), null) };
        yield return new object?[] { new SubXamlType(typeof(float), new XamlSchemaContext()), new XamlValueConverter<TypeConverter>(typeof(SingleConverter), null) };
        yield return new object?[] { new SubXamlType(typeof(double), new XamlSchemaContext()), new XamlValueConverter<TypeConverter>(typeof(DoubleConverter), null) };
        yield return new object?[] { new SubXamlType(typeof(decimal), new XamlSchemaContext()), new XamlValueConverter<TypeConverter>(typeof(DecimalConverter), null) };
        yield return new object?[] { new SubXamlType(typeof(string), new XamlSchemaContext()), new XamlValueConverter<TypeConverter>(typeof(StringConverter), null) };
        yield return new object?[] { new SubXamlType(typeof(TimeSpan), new XamlSchemaContext()), new XamlValueConverter<TypeConverter>(typeof(TimeSpanConverter), null) };
        yield return new object?[] { new SubXamlType(typeof(Guid), new XamlSchemaContext()), new XamlValueConverter<TypeConverter>(typeof(GuidConverter), null) };
        yield return new object?[] { new SubXamlType(typeof(CultureInfo), new XamlSchemaContext()), new XamlValueConverter<TypeConverter>(typeof(CultureInfoConverter), null) };
        yield return new object?[] { new SubXamlType(typeof(ConsoleColor), new XamlSchemaContext()), new XamlValueConverter<TypeConverter>(typeof(EnumConverter), new XamlType(typeof(ConsoleColor), new XamlSchemaContext())) };
        yield return new object?[] { new SubXamlType(typeof(int?), new XamlSchemaContext()), new XamlValueConverter<TypeConverter>(typeof(Int32Converter), null) };
        yield return new object?[] { new SubXamlType(typeof(object), new XamlSchemaContext()), new XamlValueConverter<TypeConverter>(null, XamlLanguage.Object) };
        yield return new object?[] { new NoUnderlyingOrBaseType(), null };

        // Has provider.
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new TypeConverterAttribute(typeof(string)) }
                }
            },
            new XamlValueConverter<TypeConverter>(typeof(string), null)
        };
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => Array.Empty<object>()
                }
            },
            new XamlValueConverter<TypeConverter>(typeof(Int32Converter), null)
        };
        yield return new object?[]
        {
            new CustomXamlType("namespace", "name", null, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new TypeConverterAttribute(typeof(int)) }
                }
            },
            null
        };

        // Has attribute.
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithTypeConverterAttribute), new XamlSchemaContext()),
            new XamlValueConverter<TypeConverter>(typeof(int), null)
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(StructWithTypeConverterAttribute?), new XamlSchemaContext()),
            new XamlValueConverter<TypeConverter>(typeof(int), null)
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(InheritedClassWithTypeConverterAttribute), new XamlSchemaContext()),
            new XamlValueConverter<TypeConverter>(typeof(int), null)
        };
        yield return new object?[]
        {
            new SubXamlType(new ReflectionOnlyCustomAttributeDataType(typeof(ClassWithStringTypeConverterAttribute)), new XamlSchemaContext()),
            new XamlValueConverter<TypeConverter>(typeof(int), null)
        };
        yield return new object?[]
        {
            new SubXamlType(new ThrowsCustomAttributeFormatExceptionDelegator(typeof(int)), new XamlSchemaContext()),
            null
        };

        yield return new object?[]
        {
            new CustomXamlType(typeof(ClassWithTypeConverterAttribute), new XamlSchemaContext())
            {
                LookupTypeConverterResult = null
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupTypeConverter_TestData))]
    public void LookupTypeConverter_Invoke_ReturnsExpected(SubXamlType type, XamlValueConverter<TypeConverter> expected)
    {
        Assert.Equal(expected, type.LookupTypeConverterEntry());
        Assert.Equal(expected, type.TypeConverter);
    }

    [Fact]
    public void LookupTypeConverter_NullAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => null!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupTypeConverterEntry());
        Assert.Throws<NullReferenceException>(() => type.TypeConverter);
    }

    [Fact]
    public void LookupTypeConverter_NullItemInAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object?[] { null }!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupTypeConverterEntry());
        Assert.Throws<NullReferenceException>(() => type.TypeConverter);
    }

    [Fact]
    public void LookupTypeConverter_InvalidTypeItemInAttributeResult_ThrowsInvalidCastException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object[] { new object() }
            }
        };
        Assert.Throws<InvalidCastException>(() => type.LookupTypeConverterEntry());
        Assert.Throws<InvalidCastException>(() => type.TypeConverter);
    }

    public static IEnumerable<object[]> LookupTypeConverter_InvalidAttribute_TestData()
    {
        yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1), new CustomAttributeTypedArgument(typeof(int), 2) } };
        yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1) } };
    }

    [Theory]
    [MemberData(nameof(LookupTypeConverter_InvalidAttribute_TestData))]
    public void LookupTypeConverter_InvalidAttribute_ThrowsXamlSchemaException(CustomAttributeTypedArgument[] arguments)
    {
        var type = new SubXamlType(new CustomType(typeof(List<int>))
        {
            GetCustomAttributesDataResult = new CustomAttributeData[]
            {
                new CustomCustomAttributeData
                {
                    ConstructorResult = typeof(TypeConverterAttribute).GetConstructors()[0],
                    ConstructorArgumentsResult = arguments
                }
            }
        }, new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => type.LookupTypeConverterEntry());
        Assert.Throws<XamlSchemaException>(() => type.TypeConverter);
    }

    [Theory]
    [InlineData(typeof(ClassWithNullTypeConverterAttribute))]
    [InlineData(typeof(ClassWithDefaultTypeConverterAttribute))]
    public void LookupTypeConverter_InvalidParametersType_ThrowsXamlSchemaException(Type underlyingType)
    {
        var type = new SubXamlType(underlyingType, new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => type.LookupTypeConverterEntry());
        Assert.Throws<XamlSchemaException>(() => type.TypeConverter);
    }

    [Fact]
    public void LookupTypeConverter_NullStringType_ThrowsArgumentNullException()
    {
        var type = new SubXamlType(typeof(ClassWithNullStringTypeConverterAttribute), new XamlSchemaContext());
        Assert.Throws<ArgumentNullException>("typeName", () => type.LookupTypeConverterEntry());
        Assert.Throws<ArgumentNullException>("typeName", () => type.TypeConverter);
    }

    [TypeConverter(typeof(int))]
    public class ClassWithTypeConverterAttribute
    {
    }

    [TypeConverter(typeof(int))]
    public struct StructWithTypeConverterAttribute
    {
    }

    [TypeConverter("System.Int32")]
    public class ClassWithStringTypeConverterAttribute
    {
    }

    public class InheritedClassWithTypeConverterAttribute : ClassWithTypeConverterAttribute
    {
    }

    [TypeConverter((string)null!)]
    public class ClassWithNullStringTypeConverterAttribute
    {
    }

    [TypeConverter((Type)null!)]
    public class ClassWithNullTypeConverterAttribute
    {
    }

    [TypeConverter]
    public class ClassWithDefaultTypeConverterAttribute
    {
    }

    [Fact]
    public void LookupUnderlyingType_Unknown_ReturnsObject()
    {
        var type = new SubXamlType("namespace", "name", null, new XamlSchemaContext());
        Assert.Null(type.LookupUnderlyingTypeEntry());
        Assert.Null(type.UnderlyingType);
    }

    [Fact]
    public void LookupUnderlyingType_HasUnderlyingType_ReturnsExpected()
    {
        var type = new SubXamlType(typeof(int), new XamlSchemaContext());
        Assert.Equal(typeof(int), type.LookupUnderlyingTypeEntry());
        Assert.Equal(typeof(int), type.UnderlyingType);
    }

    [Fact]
    public void LookupUnderlyingType_NullResult_ReturnsExpected()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupUnderlyingTypeResult = null
        };
        Assert.Null(type.LookupUnderlyingTypeEntry());
        Assert.Equal(typeof(int), type.UnderlyingType);
    }

    public static IEnumerable<object[]> LookupUsableDuringInitialization_TestData()
    {
        yield return new object[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType("name", null, new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(int), new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(object), new XamlSchemaContext()), false };
        yield return new object[] { new SubXamlType(typeof(MarkupExtension), new XamlSchemaContext()), false };
        yield return new object[] { new NoUnderlyingOrBaseType(), false };

        // Has provider.
        yield return new object[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new UsableDuringInitializationAttribute(true) }
                }
            },
            true
        };
        yield return new object[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => Array.Empty<object>()
                }
            },
            false
        };
        yield return new object[]
        {
            new CustomXamlType("namespace", "name", null, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new UsableDuringInitializationAttribute(true) }
                }
            },
            false
        };

        // Has attribute.
        yield return new object[]
        {
            new SubXamlType(typeof(ClassWithUsableDuringInitializationAttribute), new XamlSchemaContext()),
            true
        };
        yield return new object[]
        {
            new SubXamlType(typeof(InheritedClassWithUsableDuringInitializationAttribute), new XamlSchemaContext()),
            true
        };
        yield return new object[]
        {
            new SubXamlType(new ReflectionOnlyCustomAttributeDataType(typeof(ClassWithUsableDuringInitializationAttribute)), new XamlSchemaContext()),
            true
        };
        yield return new object[]
        {
            new SubXamlType(new ThrowsCustomAttributeFormatExceptionDelegator(typeof(int)), new XamlSchemaContext()),
            false
        };

        yield return new object[]
        {
            new CustomXamlType(typeof(ClassWithUsableDuringInitializationAttribute), new XamlSchemaContext())
            {
                LookupUsableDuringInitializationResult = false
            },
            false
        };
    }

    [Theory]
    [MemberData(nameof(LookupUsableDuringInitialization_TestData))]
    public void LookupUsableDuringInitialization_Invoke_ReturnsExpected(SubXamlType type, bool expected)
    {
        Assert.Equal(expected, type.LookupUsableDuringInitializationEntry());
        Assert.Equal(expected, type.IsUsableDuringInitialization);
    }

    [Fact]
    public void LookupUsableDuringInitialization_NullAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => null!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupUsableDuringInitializationEntry());
        Assert.Throws<NullReferenceException>(() => type.IsUsableDuringInitialization);
    }

    [Fact]
    public void LookupUsableDuringInitialization_NullItemInAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object?[] { null }!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupUsableDuringInitializationEntry());
        Assert.Throws<NullReferenceException>(() => type.IsUsableDuringInitialization);
    }

    [Fact]
    public void LookupUsableDuringInitialization_InvalidTypeItemInAttributeResult_ThrowsInvalidCastException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object[] { new object() }
            }
        };
        Assert.Throws<InvalidCastException>(() => type.LookupUsableDuringInitializationEntry());
        Assert.Throws<InvalidCastException>(() => type.IsUsableDuringInitialization);
    }

    [Fact]
    public void LookupUsableDuringInitialization_MarkupExtension_ThrowsXamlSchemaException()
    {
        var type = new CustomXamlType(typeof(MarkupExtension), new XamlSchemaContext())
        {
            LookupUsableDuringInitializationResult = true
        };
        Assert.True(type.LookupUsableDuringInitializationEntry());
        Assert.Throws<XamlSchemaException>(() => type.IsUsableDuringInitialization);
    }

    public static IEnumerable<object[]> LookupUsableDuringInitialization_InvalidAttribute_TestData()
    {
        yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1), new CustomAttributeTypedArgument(typeof(int), 2) } };
        yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1) } };
    }

    [Theory]
    [MemberData(nameof(LookupUsableDuringInitialization_InvalidAttribute_TestData))]
    public void LookupUsableDuringInitialization_InvalidAttribute_ThrowsXamlSchemaException(CustomAttributeTypedArgument[] arguments)
    {
        var type = new SubXamlType(new CustomType(typeof(int))
        {
            GetCustomAttributesDataResult = new CustomAttributeData[]
            {
                new CustomCustomAttributeData
                {
                    ConstructorResult = typeof(UsableDuringInitializationAttribute).GetConstructors()[0],
                    ConstructorArgumentsResult = arguments
                }
            }
        }, new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => type.LookupUsableDuringInitializationEntry());
        Assert.Throws<XamlSchemaException>(() => type.IsUsableDuringInitialization);
    }


    [UsableDuringInitializationAttribute(true)]
    private class ClassWithUsableDuringInitializationAttribute
    {
    }

    private class InheritedClassWithUsableDuringInitializationAttribute : ClassWithUsableDuringInitializationAttribute
    {
    }

    public static IEnumerable<object?[]> LookupValueSerializer_TestData()
    {
        yield return new object?[] { new SubXamlType("namespace", "name", null, new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), null };

        yield return new object?[] { new SubXamlType(typeof(int?), new XamlSchemaContext()), null };
        yield return new object?[] { new SubXamlType(typeof(string), new XamlSchemaContext()), new XamlValueConverter<ValueSerializer>(ValueSerializer.GetSerializerFor(typeof(string))!.GetType(), null) };
        yield return new object?[] { new SubXamlType(typeof(object), new XamlSchemaContext()), null };
        yield return new object?[] { new NoUnderlyingOrBaseType(), null };

        // Has provider.
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new ValueSerializerAttribute(typeof(string)) }
                }
            },
            new XamlValueConverter<ValueSerializer>(typeof(string), null)
        };
        yield return new object?[]
        {
            new CustomXamlType(typeof(int), new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => Array.Empty<object>()
                }
            },
            null
        };
        yield return new object?[]
        {
            new CustomXamlType("namespace", "name", null, new XamlSchemaContext())
            {
                LookupCustomAttributeProviderResult = new CustomAttributeProvider
                {
                    GetCustomAttributesAction = (attributeType, inherit) => new object[] { new ValueSerializerAttribute(typeof(int)) }
                }
            },
            null
        };

        // Has attribute.
        yield return new object?[]
        {
            new SubXamlType(typeof(ClassWithValueSerializerAttribute), new XamlSchemaContext()),
            new XamlValueConverter<ValueSerializer>(typeof(int), null)
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(StructWithValueSerializerAttribute?), new XamlSchemaContext()),
            new XamlValueConverter<ValueSerializer>(typeof(int), null)
        };
        yield return new object?[]
        {
            new SubXamlType(typeof(InheritedClassWithValueSerializerAttribute), new XamlSchemaContext()),
            new XamlValueConverter<ValueSerializer>(typeof(int), null)
        };
        yield return new object?[]
        {
            new SubXamlType(new ReflectionOnlyCustomAttributeDataType(typeof(ClassWithStringValueSerializerAttribute)), new XamlSchemaContext()),
            new XamlValueConverter<ValueSerializer>(typeof(int), null)
        };
        yield return new object?[]
        {
            new SubXamlType(new ThrowsCustomAttributeFormatExceptionDelegator(typeof(int)), new XamlSchemaContext()),
            null
        };

        yield return new object?[]
        {
            new CustomXamlType(typeof(ClassWithValueSerializerAttribute), new XamlSchemaContext())
            {
                LookupValueSerializerResult = null
            },
            null
        };
    }

    [Theory]
    [MemberData(nameof(LookupValueSerializer_TestData))]
    public void LookupValueSerializer_Invoke_ReturnsExpected(SubXamlType type, XamlValueConverter<ValueSerializer> expected)
    {
        Assert.Equal(expected, type.LookupValueSerializerEntry());
        Assert.Equal(expected, type.ValueSerializer);
    }

    [Fact]
    public void LookupValueSerializer_NullAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => null!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupValueSerializerEntry());
        Assert.Throws<NullReferenceException>(() => type.ValueSerializer);
    }

    [Fact]
    public void LookupValueSerializer_NullItemInAttributeResult_ThrowsNullReferenceException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object?[] { null }!
            }
        };
        Assert.Throws<NullReferenceException>(() => type.LookupValueSerializerEntry());
        Assert.Throws<NullReferenceException>(() => type.ValueSerializer);
    }

    [Fact]
    public void LookupValueSerializer_InvalidTypeItemInAttributeResult_ThrowsInvalidCastException()
    {
        var type = new CustomXamlType(typeof(int), new XamlSchemaContext())
        {
            LookupCustomAttributeProviderResult = new CustomAttributeProvider
            {
                GetCustomAttributesAction = (attributeType, inherit) => new object[] { new object() }
            }
        };
        Assert.Throws<InvalidCastException>(() => type.LookupValueSerializerEntry());
        Assert.Throws<InvalidCastException>(() => type.ValueSerializer);
    }

    public static IEnumerable<object[]> LookupValueSerializer_InvalidAttribute_TestData()
    {
        yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1), new CustomAttributeTypedArgument(typeof(int), 2) } };
        yield return new object[] { new CustomAttributeTypedArgument[] { new CustomAttributeTypedArgument(typeof(int), 1) } };
    }

    [Theory]
    [MemberData(nameof(LookupValueSerializer_InvalidAttribute_TestData))]
    public void LookupValueSerializer_InvalidAttribute_ThrowsXamlSchemaException(CustomAttributeTypedArgument[] arguments)
    {
        var type = new SubXamlType(new CustomType(typeof(List<int>))
        {
            GetCustomAttributesDataResult = new CustomAttributeData[]
            {
                new CustomCustomAttributeData
                {
                    ConstructorResult = typeof(ValueSerializerAttribute).GetConstructors()[0],
                    ConstructorArgumentsResult = arguments
                }
            }
        }, new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => type.LookupValueSerializerEntry());
        Assert.Throws<XamlSchemaException>(() => type.ValueSerializer);
    }

    [Fact]
    public void LookupValueSerializer_InvalidParametersType_ThrowsXamlSchemaException()
    {
        var type = new SubXamlType(typeof(ClassWithNullValueSerializerAttribute), new XamlSchemaContext());
        Assert.Throws<XamlSchemaException>(() => type.LookupValueSerializerEntry());
        Assert.Throws<XamlSchemaException>(() => type.ValueSerializer);
    }

    [Fact]
    public void LookupValueSerializer_NullStringType_ThrowsArgumentNullException()
    {
        var type = new SubXamlType(typeof(ClassWithNullStringValueSerializerAttribute), new XamlSchemaContext());
        Assert.Throws<ArgumentNullException>("typeName", () => type.LookupValueSerializerEntry());
        Assert.Throws<ArgumentNullException>("typeName", () => type.ValueSerializer);
    }

    [ValueSerializer(typeof(int))]
    public class ClassWithValueSerializerAttribute
    {
    }

    [ValueSerializer(typeof(int))]
    public struct StructWithValueSerializerAttribute
    {
    }

    [ValueSerializer("System.Int32")]
    public class ClassWithStringValueSerializerAttribute
    {
    }

    public class InheritedClassWithValueSerializerAttribute : ClassWithValueSerializerAttribute
    {
    }

    [ValueSerializer((string)null!)]
    public class ClassWithNullStringValueSerializerAttribute
    {
    }

    [ValueSerializer((Type)null!)]
    public class ClassWithNullValueSerializerAttribute
    {
    }

    public static IEnumerable<object?[]> CanAssignTo_TestData()
    {
        // Known.
        yield return new object?[] { new XamlType(typeof(int), new XamlSchemaContext()), new XamlType(typeof(int), new XamlSchemaContext()), true };
        yield return new object?[] { new XamlType(typeof(int), new XamlSchemaContext()), new XamlType(typeof(object), new XamlSchemaContext()), true };
        yield return new object?[] { new XamlType(typeof(int), new XamlSchemaContext()), new XamlType(typeof(string), new XamlSchemaContext()), false };
        yield return new object?[] { new XamlType(typeof(SubXamlType), new XamlSchemaContext()), new XamlType(typeof(XamlType), new XamlSchemaContext()), true };
        yield return new object?[] { new XamlType(typeof(XamlType), new XamlSchemaContext()), new XamlType(typeof(SubXamlType), new XamlSchemaContext()), false };
        yield return new object?[] { new XamlType(typeof(int), new XamlSchemaContext()), new XamlType("unknownTypeNamespace", "unknownType", null, new XamlSchemaContext()), false };
        yield return new object?[] { new XamlType(typeof(int), new XamlSchemaContext()), null, false };

        // Unknown.
        yield return new object?[] { new XamlType("unknownTypeNamespace", "unknownType", null, new XamlSchemaContext()), new XamlType("unknownTypeNamespace", "unknownType", null, new XamlSchemaContext()), true };
        yield return new object?[] { new XamlType("unknownTypeNamespace", "unknownType", null, new XamlSchemaContext()), new XamlType("otherTypeNamespace", "unknownType", null, new XamlSchemaContext()), false };
        yield return new object?[] { new XamlType("unknownTypeNamespace", "unknownType", null, new XamlSchemaContext()), new XamlType(typeof(int), new XamlSchemaContext()), false };
        yield return new object?[] { new XamlType("unknownTypeNamespace", "unknownType", null, new XamlSchemaContext()), null, false };

        // ReflectionOnly.
        yield return new object?[]
        {
            new XamlType(new ReflectionOnlyType(typeof(XamlType)), new XamlSchemaContext()),
            new XamlType(typeof(XamlType), new XamlSchemaContext()),
            true
        };
        yield return new object?[]
        {
            new XamlType(new ReflectionOnlyType(typeof(XamlType)), new XamlSchemaContext()),
            new XamlType(typeof(XamlSchemaContext), new XamlSchemaContext()),
            false
        };
        yield return new object?[]
        {
            new XamlType(new ReflectionOnlyType(typeof(int)), new XamlSchemaContext()),
            new XamlType(typeof(XamlSchemaContext), new XamlSchemaContext()),
            false
        };
        yield return new object?[]
        {
            new XamlType(new ReflectionOnlyType(typeof(XamlObjectWriterSettings)), new XamlSchemaContext()),
            new XamlType(typeof(XamlWriterSettings), new XamlSchemaContext()),
            true
        };
        yield return new object?[]
        {
            new XamlType(new ReflectionOnlyType(typeof(XamlWriterSettings)), new XamlSchemaContext()),
            new XamlType(typeof(XamlObjectWriterSettings), new XamlSchemaContext()),
            false
        };

        // WindowsBase/System.Xaml.
        var differentVersionType = new ReflectionOnlyType(typeof(XamlType));
        var differentVersionAssemblyName = new AssemblyName(differentVersionType.Assembly.FullName!)
        {
            Version = new Version(2, 0)
        };
        differentVersionType.AssemblyDelegator!.FullNameResult = differentVersionAssemblyName.FullName;

        yield return new object?[]
        {
            new XamlType(differentVersionType, new XamlSchemaContext()),
            new XamlType(typeof(XamlType), new XamlSchemaContext()),
            true
        };

        var differentCultureType = new ReflectionOnlyType(typeof(XamlType));
        var differentCultureAssemblyName = new AssemblyName(differentCultureType.Assembly.FullName!)
        {
            CultureInfo = new CultureInfo("fr-FR")
        };
        differentCultureType.AssemblyDelegator!.FullNameResult = differentCultureAssemblyName.FullName;
        yield return new object?[]
        {
            new XamlType(differentCultureType, new XamlSchemaContext()),
            new XamlType(typeof(XamlType), new XamlSchemaContext()),
            false
        };

        foreach (byte[]? keyToken in new object?[] { null, Array.Empty<byte>(), new byte[] { 183, 122, 92, 86, 25, 52, 224, 138 } })
        {
            var differentPublicKeyTokenType = new ReflectionOnlyType(typeof(XamlType));
            var differentPublicKeyTokenAssemblyName = new AssemblyName(differentPublicKeyTokenType.Assembly.FullName!);
            differentPublicKeyTokenAssemblyName.SetPublicKeyToken(keyToken);
            differentPublicKeyTokenType.AssemblyDelegator!.FullNameResult = differentPublicKeyTokenAssemblyName.FullName;
            yield return new object?[]
            {
                new XamlType(differentPublicKeyTokenType, new XamlSchemaContext()),
                new XamlType(typeof(XamlType), new XamlSchemaContext()),
                false
            };
        }

        var windowsBaseType = new ReflectionOnlyType(typeof(XamlType));
        var windowsBaseAssemblyName = new AssemblyName(windowsBaseType.Assembly.FullName!)
        {
            Name = "WindowsBase"
        };
        windowsBaseAssemblyName.SetPublicKeyToken(new byte[] { 49, 191, 56, 86, 173, 54, 78, 53 });
        windowsBaseType.AssemblyDelegator!.FullNameResult = windowsBaseAssemblyName.FullName;
        yield return new object?[]
        {
            new XamlType(windowsBaseType, new XamlSchemaContext()),
            new XamlType(typeof(XamlType), new XamlSchemaContext()),
            true
        };

        var invalidWindowsBaseType = new ReflectionOnlyType(typeof(XamlType));
        var invalidWindowsBaseAssemblyName = new AssemblyName(invalidWindowsBaseType.Assembly.FullName!)
        {
            Name = "BadWindowsBase"
        };
        invalidWindowsBaseAssemblyName.SetPublicKeyToken(new byte[] { 49, 191, 56, 86, 173, 54, 78, 53 });
        invalidWindowsBaseType.AssemblyDelegator!.FullNameResult = invalidWindowsBaseAssemblyName.FullName;
        yield return new object?[]
        {
            new XamlType(invalidWindowsBaseType, new XamlSchemaContext()),
            new XamlType(typeof(XamlType), new XamlSchemaContext()),
            false
        };

        foreach (byte[]? keyToken in new object?[] { null, Array.Empty<byte>(), new byte[] { 183, 122, 92, 86, 25, 52, 224, 137 } })
        {
            var invalidKeyTokenWindowsBaseType = new ReflectionOnlyType(typeof(XamlType));
            var invalidKeyTokenWindowsBaseAssemblyName = new AssemblyName(invalidKeyTokenWindowsBaseType.Assembly.FullName!)
            {
                Name = "WindowsBase"
            };
            invalidKeyTokenWindowsBaseAssemblyName.SetPublicKeyToken(keyToken);
            invalidKeyTokenWindowsBaseType.AssemblyDelegator!.FullNameResult = invalidKeyTokenWindowsBaseAssemblyName.FullName;
            yield return new object?[]
            {
                new XamlType(invalidKeyTokenWindowsBaseType, new XamlSchemaContext()),
                new XamlType(typeof(XamlType), new XamlSchemaContext()),
                false
            };
        }

        yield return new object?[]
        {
            new XamlType(new ReflectionOnlyType(typeof(int)), new XamlSchemaContext()),
            new XamlType(typeof(XamlType), new XamlSchemaContext()),
            false
        };

        // Interfaces
        yield return new object?[]
        {
            new XamlType(new ReflectionOnlyType(typeof(CustomNameScope)), new XamlSchemaContext()),
            new XamlType(typeof(INameScope), new XamlSchemaContext()),
            true
        };
        yield return new object?[]
        {
            new XamlType(new ReflectionOnlyType(typeof(CustomNameScopeDictionary)), new XamlSchemaContext()),
            new XamlType(typeof(INameScope), new XamlSchemaContext()),
            true
        };
        yield return new object?[]
        {
            new XamlType(new ReflectionOnlyType(typeof(XamlType)), new XamlSchemaContext()),
            new XamlType(typeof(INameScope), new XamlSchemaContext()),
            false
        };
        yield return new object?[]
        {
            new XamlType(new ReflectionOnlyType(typeof(int)), new XamlSchemaContext()),
            new XamlType(typeof(INameScope), new XamlSchemaContext()),
            false
        };
        yield return new object?[]
        {
            new XamlType(new ReflectionOnlyType(typeof(INameScope)), new XamlSchemaContext()),
            new XamlType(typeof(INameScope), new XamlSchemaContext()),
            true
        };
        yield return new object?[]
        {
            new XamlType(new ReflectionOnlyType(typeof(INameScope)), new XamlSchemaContext()),
            new XamlType(typeof(INameScopeDictionary), new XamlSchemaContext()),
            false
        };
        yield return new object?[]
        {
            new XamlType(new ReflectionOnlyType(typeof(INameScopeDictionary)), new XamlSchemaContext()),
            new XamlType(typeof(INameScope), new XamlSchemaContext()),
            true
        };

        var nullInterface = new ReflectionOnlyType(typeof(INameScopeDictionary))
        {
            GetInterfacesResult = new Type?[] { null }
        };
        yield return new object?[]
        {
            new XamlType(nullInterface, new XamlSchemaContext()),
            new XamlType(typeof(INameScope), new XamlSchemaContext()),
            false
        };

        // Generic type parameter.
        Type genericTypeParameter = typeof(XamlValueConverter<>).GetTypeInfo().GenericTypeParameters[0];
        yield return new object?[]
        {
            new XamlType(new ReflectionOnlyType(typeof(int)), new XamlSchemaContext()),
            new XamlType(genericTypeParameter, new XamlSchemaContext()),
            true
        };

        var nullConstrainedGenericParameter = new ReflectionOnlyType(genericTypeParameter)
        {
            GetGenericParameterConstraintsResult = new Type?[] { null }
        };
        yield return new object?[]
        {
            new XamlType(new ReflectionOnlyType(typeof(int)), new XamlSchemaContext()),
            new XamlType(nullConstrainedGenericParameter, new XamlSchemaContext()),
            false
        };

        var constrainedGenericParameter = new ReflectionOnlyType(genericTypeParameter)
        {
            GetGenericParameterConstraintsResult = new Type[] { typeof(XamlObjectWriterSettings) }
        };
        yield return new object?[]
        {
            new XamlType(new ReflectionOnlyType(typeof(XamlObjectWriterSettings)), new XamlSchemaContext()),
            new XamlType(constrainedGenericParameter, new XamlSchemaContext()),
            true
        };

        yield return new object?[]
        {
            new XamlType(new ReflectionOnlyType(typeof(XamlWriterSettings)), new XamlSchemaContext()),
            new XamlType(constrainedGenericParameter, new XamlSchemaContext()),
            false
        };

        // Edge case - assembly qualified name not equal, then equal
        // when checking for subclass.
        yield return new object?[]
        {
            new XamlType(new ReflectionOnlyType(typeof(XamlType)), new XamlSchemaContext()),
            new XamlType(new InconsistentNameTypeDelegator(typeof(XamlType)), new XamlSchemaContext()),
            false
        };
    }

    [Theory]
    [MemberData(nameof(CanAssignTo_TestData))]
    public void CanAssignTo_Invoke_ReturnsExpected(XamlType type, XamlType other, bool expected)
    {
        Assert.Equal(expected, type.CanAssignTo(other));
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        var type = new XamlType(typeof(int), new XamlSchemaContext());
        yield return new object?[] { type, type, true };
        yield return new object?[] { type, new XamlType(typeof(int), new XamlSchemaContext()), true };
        yield return new object?[] { type, new XamlType(typeof(string), new XamlSchemaContext()), false };
        yield return new object?[] { type, new XamlType("namespace", "name", null, new XamlSchemaContext()), false };
        yield return new object?[] { type, new SubXamlType("name", null, new XamlSchemaContext()), false };

        yield return new object?[] { new XamlType("namespace", "name", null, new XamlSchemaContext()), new XamlType("namespace", "name", null, new XamlSchemaContext()), true };
        yield return new object?[] { new XamlType("namespace", "name", new XamlType[] { new XamlType(typeof(int), new XamlSchemaContext()) }, new XamlSchemaContext()), new XamlType("namespace", "name", new XamlType[] { new XamlType(typeof(int), new XamlSchemaContext()) }, new XamlSchemaContext()), true };
        yield return new object?[] { new XamlType("namespace", "name", null, new XamlSchemaContext()), new XamlType("otherNamespace", "name", null, new XamlSchemaContext()), false };
        yield return new object?[] { new XamlType("namespace", "name", null, new XamlSchemaContext()), new XamlType("namespace", "otherName", null, new XamlSchemaContext()), false };
        yield return new object?[] { new XamlType("namespace", "name", null, new XamlSchemaContext()), new XamlType("namespace", "name", new XamlType[] { new XamlType(typeof(int), new XamlSchemaContext()) }, new XamlSchemaContext()), false };
        yield return new object?[] { new XamlType("namespace", "name", new XamlType[] { new XamlType(typeof(int), new XamlSchemaContext()) }, new XamlSchemaContext()), new XamlType("namespace", "name", null, new XamlSchemaContext()), false };
        yield return new object?[] { new XamlType("namespace", "name", new XamlType[] { new XamlType(typeof(int), new XamlSchemaContext()) }, new XamlSchemaContext()), new XamlType("namespace", "name", new XamlType[] { new XamlType(typeof(string), new XamlSchemaContext()) }, new XamlSchemaContext()), false };
        yield return new object?[] { new XamlType("namespace", "name", new XamlType[] { new XamlType(typeof(int), new XamlSchemaContext()), new XamlType(typeof(string), new XamlSchemaContext()) }, new XamlSchemaContext()), new XamlType("namespace", "name", new XamlType[] { new XamlType(typeof(string), new XamlSchemaContext()) }, new XamlSchemaContext()), false };
        yield return new object?[] { new XamlType("namespace", "name", new XamlType[] { new XamlType(typeof(int), new XamlSchemaContext()) }, new XamlSchemaContext()), new XamlType("namespace", "name", new XamlType[] { new XamlType(typeof(int), new XamlSchemaContext()), new XamlType(typeof(string), new XamlSchemaContext()) }, new XamlSchemaContext()), false };
        yield return new object?[] { new XamlType("namespace", "name", null, new XamlSchemaContext()), new XamlType(typeof(int), new XamlSchemaContext()), false };
        yield return new object?[] { new XamlType("namespace", "name", null, new XamlSchemaContext()), new SubXamlType("name", null, new XamlSchemaContext()), false };

        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), new SubXamlType("name", null, new XamlSchemaContext()), true };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), new SubXamlType("otherName", null, new XamlSchemaContext()), false };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), new XamlType(typeof(int), new XamlSchemaContext()), false };
        yield return new object?[] { new SubXamlType("name", null, new XamlSchemaContext()), new XamlType("namespace", "name", null, new XamlSchemaContext()), false };

        yield return new object?[] { type, null, false };
        yield return new object?[] { type, new object(), false };
        yield return new object?[] { null, type, false };
        yield return new object?[] { null, null, true };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Invoke_ReturnsExpected(XamlType type, object obj, bool expected)
    {
        XamlType? other = obj as XamlType;
        if (type != null)
        {
            Assert.Equal(expected, type.Equals(obj));
            Assert.Equal(expected, type.Equals(other));
        }

        Assert.Equal(expected, type == other);
        Assert.Equal(!expected, type != other);
    }

    public static IEnumerable<object[]> GetHashCode_TestData()
    {
        yield return new object[] { new XamlType("name", "namespace", null, new XamlSchemaContext()) };
        yield return new object[] { new XamlType("name", "namespace", new XamlType[] { new XamlType(typeof(int), new XamlSchemaContext()) }, new XamlSchemaContext()) };
        yield return new object[] { new XamlType(typeof(int), new XamlSchemaContext()) };
        yield return new object[] { new SubXamlType("typeName", null, new XamlSchemaContext()) };
        yield return new object[] { new NoUnderlyingOrBaseType() };
    }

    [Theory]
    [MemberData(nameof(GetHashCode_TestData))]
    public void GetHashCode_Invoke_ReturnsExpected(XamlType type)
    {
        Assert.Equal(type.GetHashCode(), type.GetHashCode());
    }

    private static void AssertEqualXamlMembers(XamlMember[]? expected, IEnumerable<XamlMember> actual)
    {
        if (expected == null)
        {
            Assert.Null(actual);
            return;
        }

        XamlMember[] sortedActual = actual.OrderBy(m => m.Name).ToArray();
        Assert.Equal(expected.Length, sortedActual.Length);

        for (int i = 0; i < expected.Length; i++)
        {
            AssertEqualXamlMember(expected[i], sortedActual[i]);
        }
    }

    private static void AssertEqualXamlMember(XamlMember expected, XamlMember actual)
    {
        if (expected == null)
        {
            Assert.Null(actual);
            return;
        }

        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.UnderlyingMember, actual.UnderlyingMember);
        Assert.Equal(GetUnderlyingGetter(expected), GetUnderlyingGetter(actual));
        Assert.Equal(GetUnderlyingSetter(expected), GetUnderlyingSetter(actual));
    }

    private class NoUnderlyingOrBaseType : SubXamlType
    {
        public NoUnderlyingOrBaseType() : base("name", null, new XamlSchemaContext()) { }

        protected override bool LookupIsUnknown() => false;
        protected override XamlType LookupBaseType() => null!;
    }

    private class ReflectionOnlyType : CustomType
    {
        public ReflectionOnlyType(Type delegatingType) : base(delegatingType)
        {
            AssemblyResult = new CustomAssembly(base.Assembly)
            {
                ReflectionOnlyResult = true
            };
        }

        public CustomAssembly? AssemblyDelegator
        {
            get => AssemblyResult.HasValue ? AssemblyResult.Value as CustomAssembly : null;
            set => AssemblyResult = value!;
        }
    }

    private class ReflectionOnlyCustomAttributeDataType : CustomType
    {
        public ReflectionOnlyCustomAttributeDataType(Type delegatingType) : base(delegatingType)
        {
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            IList<CustomAttributeData> baseData = typeImpl.GetCustomAttributesData();
            return baseData.Select(c =>
            {
                return new CustomCustomAttributeData
                {
                    ConstructorResult = new CustomConstructorInfo(c.Constructor)
                    {
                        DeclaringTypeResult = new ReflectionOnlyType(c.Constructor.DeclaringType!)
                    },
                    ConstructorArgumentsResult = c.ConstructorArguments
                };
            }).ToArray();
        }
    }

    private class ThrowsCustomAttributeFormatExceptionDelegator : TypeDelegator
    {
        public ThrowsCustomAttributeFormatExceptionDelegator(Type delegatingType) : base(delegatingType)
        {
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            throw new CustomAttributeFormatException();
        }
    }

    private class NestedClass { }

    private class InconsistentNameTypeDelegator : ReflectionOnlyType
    {
        private bool FullNameCalled { get; set; }

        public InconsistentNameTypeDelegator(Type delegatingType) : base(delegatingType)
        {
        }

        public override string FullName
        {
            get
            {
                if (FullNameCalled)
                {
                    return base.FullName!;
                }

                FullNameCalled = true;
                return "";
            }
        }
    }

    private class CustomAttributeProvider : ICustomAttributeProvider
    {
        public Func<Type, bool, object[]>? GetCustomAttributesAction { get; set; }

        public object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (GetCustomAttributesAction is null)
            {
                throw new NotImplementedException();
            }

            return GetCustomAttributesAction(attributeType, inherit);
        }

        public object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();

        public Func<Type, bool, bool>? IsDefinedAction { get; set; }

        public bool IsDefined(Type attributeType, bool inherit)
        {
            if (IsDefinedAction is null)
            {
                throw new NotImplementedException();
            }

            return IsDefinedAction(attributeType, inherit);
        }
    }
    
    private class CustomXamlSchemaContext : XamlSchemaContext
    {
        public Func<Type, XamlType>? GetXamlTypeAction { get; set; }

        public override XamlType GetXamlType(Type type)
        {
            if (GetXamlTypeAction is null)
            {
                throw new NotImplementedException();
            }

            return GetXamlTypeAction(type);
        }
    }
    
    public class CustomCustomAttributeData : CustomAttributeData
    {
        public ConstructorInfo? ConstructorResult { get; set; }
        public override ConstructorInfo Constructor => ConstructorResult!;

        public IList<CustomAttributeTypedArgument>? ConstructorArgumentsResult { get; set; }
        public override IList<CustomAttributeTypedArgument> ConstructorArguments => ConstructorArgumentsResult!;
    }
}


#pragma warning disable IDE0040, IDE0060 // Add accessibility modifiers, Remove unused parameter
abstract class AbstractClass { }

static class StaticClass { }

class ClassWithDefaultConstructor { }

class ClassWithInternalDefaultConstructor
{
    internal ClassWithInternalDefaultConstructor() { }
}

class ClassWithProtectedDefaultConstructor
{
    protected ClassWithProtectedDefaultConstructor() { }
}

class ClassWithPrivateDefaultConstructor
{
    private ClassWithPrivateDefaultConstructor() { }
}

class ClassWithCustomConstructor
{
    public ClassWithCustomConstructor(int i) {}
}
}

class GlobalNamespaceClass
{
}
#pragma warning restore IDE0040, IDE0060 // Add accessibility modifiers, Remove unused parameter
