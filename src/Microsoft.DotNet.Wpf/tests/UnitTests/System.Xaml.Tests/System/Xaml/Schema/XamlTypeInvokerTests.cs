// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xaml.Tests.Common;
using System.Windows.Markup;
using Xunit;

namespace System.Xaml.Schema.Tests;

public class XamlTypeInvokerTests
{
    [Fact]
    public void Ctor_Default()
    {
        var invoker = new SubXamlTypeInvoker();
        Assert.Null(invoker.SetMarkupExtensionHandler);
        Assert.Null(invoker.SetTypeConverterHandler);
    }

    [Fact]
    public void Ctor_Type()
    {
        var type = new XamlType(typeof(ClassWithAttributes), new XamlSchemaContext());
        var invoker = new XamlTypeInvoker(type);
        Assert.Equal(Delegate.CreateDelegate(typeof(EventHandler<XamlSetMarkupExtensionEventArgs>), typeof(ClassWithAttributes), nameof(ClassWithAttributes.MarkupExtensionMethod)), invoker.SetMarkupExtensionHandler); 
        Assert.Equal(Delegate.CreateDelegate(typeof(EventHandler<XamlSetTypeConverterEventArgs>), typeof(ClassWithAttributes), nameof(ClassWithAttributes.TypeConverterMethod)), invoker.SetTypeConverterHandler); 
    }

    [Fact]
    public void Ctor_NullType_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("type", () => new XamlTypeInvoker(null));
    }

    [Fact]
    public void Unknown_Get_ReturnsExpected()
    {
        XamlTypeInvoker invoker = XamlTypeInvoker.UnknownInvoker;
        Assert.Same(invoker, XamlTypeInvoker.UnknownInvoker);
        Assert.Null(invoker.SetMarkupExtensionHandler);
        Assert.Null(invoker.SetTypeConverterHandler);
    }

    [Fact]
    public void AddToCollection_HasAddMethod_ThrowsNotSupportedException()
    {
        var invoker = new XamlTypeInvoker(new XamlType(typeof(ListAddGetEnumeratorClass), new XamlSchemaContext()));
        var instance = new ListAddGetEnumeratorClass();
        invoker.AddToCollection(instance, "a");
        invoker.AddToCollection(instance, null);
        Assert.Equal(new string?[] { "a", null }, instance.List);
    }

    [Fact]
    public void AddToCollection_IListInstance_AddsToList()
    {
        XamlTypeInvoker invoker = XamlTypeInvoker.UnknownInvoker;
        var instance = new List<string>();
        invoker.AddToCollection(instance, "a");
        invoker.AddToCollection(instance, null);
        Assert.Equal(new string?[] { "a", null }, instance);
    }

    [Fact]
    public void AddToCollection_NullInstance_ThrowsArgumentNullException()
    {
        XamlTypeInvoker invoker = XamlTypeInvoker.UnknownInvoker;
        Assert.Throws<ArgumentNullException>("instance", () => invoker.AddToCollection(null, 1));
    }

    public static IEnumerable<object[]> UnknownInvoker_TestData()
    {
        yield return new object[] { XamlTypeInvoker.UnknownInvoker };
        yield return new object[] { new XamlTypeInvoker(new XamlType("namespace", "name", null, new XamlSchemaContext())) };
    }

    [Theory]
    [MemberData(nameof(UnknownInvoker_TestData))]
    public void AddToCollection_UnknownInvoker_ThrowsNotSupportedException(XamlTypeInvoker invoker)
    {
        Assert.Throws<NotSupportedException>(() => invoker.AddToCollection(new object(), 1));
    }

    [Fact]
    public void AddToCollection_TypeNotCollection_ThrowsNotSupportedException()
    {
        var invoker = new XamlTypeInvoker(new XamlType(typeof(object), new XamlSchemaContext()));
        Assert.Throws<NotSupportedException>(() => invoker.AddToCollection(new object(), 1));
    }

    [Fact]
    public void AddToCollection_NoSuchAddMethod_ThrowsXamlSchemaException()
    {
        var invoker = new XamlTypeInvoker(new XamlType(typeof(List<int>), new XamlSchemaContext()));
        Assert.Throws<XamlSchemaException>(() => invoker.AddToCollection(new GetEnumeratorClass(), "a"));
    }

    [Fact]
    public void AddToDictionary_HasAddMethod_ThrowsNotSupportedException()
    {
        var invoker = new XamlTypeInvoker(new XamlType(typeof(DictionaryAddGetEnumeratorClass), new XamlSchemaContext()));
        var instance = new DictionaryAddGetEnumeratorClass();
        invoker.AddToDictionary(instance, 1, "a");
        invoker.AddToDictionary(instance, 2, null);
        Assert.Equal("a", instance.Dictionary[1]);
        Assert.Null(instance.Dictionary[2]);
    }

    [Fact]
    public void AddToDictionary_IDictionaryInstance_AddsToList()
    {
        XamlTypeInvoker invoker = XamlTypeInvoker.UnknownInvoker;
        var instance = new Dictionary<int, string>();
        invoker.AddToDictionary(instance, 1, "a");
        invoker.AddToDictionary(instance, 2, null);
        Assert.Equal("a", instance[1]);
        Assert.Null(instance[2]);
    }

    [Fact]
    public void AddToDictionary_NullInstance_ThrowsArgumentNullException()
    {
        XamlTypeInvoker invoker = XamlTypeInvoker.UnknownInvoker;
        Assert.Throws<ArgumentNullException>("instance", () => invoker.AddToDictionary(null, 1, 2));
    }

    [Theory]
    [MemberData(nameof(UnknownInvoker_TestData))]
    public void AddToDictionary_UnknownInvoker_ThrowsNotSupportedException(XamlTypeInvoker invoker)
    {
        Assert.Throws<NotSupportedException>(() => invoker.AddToDictionary(new object(), 1, 2));
    }

    [Fact]
    public void AddToDictionary_TypeNotDictionary_ThrowsNotSupportedException()
    {
        var invoker = new XamlTypeInvoker(new XamlType(typeof(object), new XamlSchemaContext()));
        Assert.Throws<NotSupportedException>(() => invoker.AddToDictionary(new object(), 1, 2));
    }

    [Fact]
    public void AddToDictionary_NoSuchAddMethod_ThrowsXamlSchemaException()
    {
        var invoker = new XamlTypeInvoker(new XamlType(typeof(Dictionary<object, int>), new XamlSchemaContext()));
        Assert.Throws<XamlSchemaException>(() => invoker.AddToDictionary(new GetEnumeratorClass(), new object(), "a"));
    }

    [Fact]
    public void CreateInstance_ReferenceTypeWithNoParameters_ReturnsExpected()
    {
        var invoker = new XamlTypeInvoker(new XamlType(typeof(PublicClass), new XamlSchemaContext()));
        Assert.Equal(1, Assert.IsType<PublicClass>(invoker.CreateInstance(null)).Value);
        Assert.Equal(1, Assert.IsType<PublicClass>(invoker.CreateInstance(Array.Empty<object>())).Value);
    }

    [Fact]
    public void CreateInstance_ReferenceTypeWithParameters_ReturnsExpected()
    {
        var invoker = new XamlTypeInvoker(new XamlType(typeof(PublicClass), new XamlSchemaContext()));
        Assert.Equal(2, Assert.IsType<PublicClass>(invoker.CreateInstance(new object[] { 2 })).Value);
    }

    [Fact]
    public void CreateInstance_PrivateReferenceType_ReturnsExpected()
    {
        var invoker = new XamlTypeInvoker(new XamlType(typeof(PrivateClass), new XamlSchemaContext()));
        Assert.Equal(1, Assert.IsType<PrivateClass>(invoker.CreateInstance(null)).Value);
        Assert.Equal(1, Assert.IsType<PrivateClass>(invoker.CreateInstance(Array.Empty<object>())).Value);
    }

    [Fact]
    public void CreateInstance_ValueTypeWithNoParameters_ReturnsExpected()
    {
        var invoker = new XamlTypeInvoker(new XamlType(typeof(int), new XamlSchemaContext()));
        Assert.Equal(0, invoker.CreateInstance(null));
        Assert.Equal(0, invoker.CreateInstance(Array.Empty<object>()));
    }

    [Theory]
    [MemberData(nameof(UnknownInvoker_TestData))]
    public void CreateInstance_UnknownInvoker_ThrowsNotSupportedException(XamlTypeInvoker invoker)
    {
        Assert.Throws<NotSupportedException>(() => invoker.CreateInstance(null));
        Assert.Throws<NotSupportedException>(() => invoker.CreateInstance(Array.Empty<object>()));
    }

    [Fact]
    public void CreateInstance_NoDefaultConstructor_ThrowsMissingMethodException()
    {
        var invoker = new XamlTypeInvoker(new XamlType(typeof(NoDefaultConstructorClass), new XamlSchemaContext()));
        Assert.Throws<MissingMethodException>(() => invoker.CreateInstance(null));
        Assert.Throws<MissingMethodException>(() => invoker.CreateInstance(Array.Empty<object>()));
    }

    public class PublicClass
    {
        public int Value { get; set; }

        public PublicClass() => Value = 1;
        public PublicClass(int i) => Value = i;
    }

    private class PrivateClass
    {
        public int Value { get; set; }

        public PrivateClass() => Value = 1;
    }

    public class NoDefaultConstructorClass
    {
        public NoDefaultConstructorClass(int i) { }
    }

    public static IEnumerable<object?[]> GetAddMethod_TestData()
    {
        yield return new object?[]
        {
            XamlTypeInvoker.UnknownInvoker,
            new XamlType(typeof(int), new XamlSchemaContext()),
            null
        };
        yield return new object?[]
        {
            new XamlTypeInvoker(new XamlType("namespace", "name", null, new XamlSchemaContext())),
            new XamlType(typeof(int), new XamlSchemaContext()),
            null
        };
        yield return new object?[]
        {
            new XamlTypeInvoker(new XamlType(typeof(int), new XamlSchemaContext())),
            new XamlType(typeof(int), new XamlSchemaContext()),
            null
        };

        // Collection.
        yield return new object?[]
        {
            new XamlTypeInvoker(new XamlType(typeof(List<int>), new XamlSchemaContext())),
            new XamlType(typeof(int), new XamlSchemaContext()),
            typeof(ICollection<int>).GetMethod(nameof(ICollection<int>.Add))
        };
        yield return new object?[]
        {
            new XamlTypeInvoker(new XamlType(typeof(List<Array>), new XamlSchemaContext())),
            new XamlType(typeof(int[]), new XamlSchemaContext()),
            typeof(ICollection<Array>).GetMethod(nameof(ICollection<Array>.Add))
        };
        yield return new object?[]
        {
            new XamlTypeInvoker(new XamlType(typeof(List<Array>), new XamlSchemaContext())),
            new XamlType(typeof(object), new XamlSchemaContext()),
            null
        };
        yield return new object?[]
        {
            new XamlTypeInvoker(new XamlType(typeof(List<int>), new XamlSchemaContext())),
            new XamlType(typeof(string), new XamlSchemaContext()),
            null
        };
        yield return new object?[]
        {
            new XamlTypeInvoker(new XamlType(typeof(ListAddGetEnumeratorClass), new XamlSchemaContext())),
            new XamlType(typeof(object), new XamlSchemaContext()),
            typeof(ListAddGetEnumeratorClass).GetMethod(nameof(DictionaryAddGetEnumeratorClass.Add))
        };
        yield return new object?[]
        {
            new XamlTypeInvoker(new XamlType(typeof(Dictionary<int, string>), new XamlSchemaContext())),
            new XamlType(typeof(int[]), new XamlSchemaContext()),
            null
        };
        yield return new object?[]
        {
            new XamlTypeInvoker(new XamlType(typeof(Dictionary<int, string>), new XamlSchemaContext())),
            new XamlType(typeof(KeyValuePair<int, string>), new XamlSchemaContext()),
            null
        };
        yield return new object?[]
        {
            new XamlTypeInvoker(new XamlType(typeof(DictionaryAddGetEnumeratorClass), new XamlSchemaContext())),
            new XamlType(typeof(object), new XamlSchemaContext()),
            typeof(DictionaryAddGetEnumeratorClass).GetMethod(nameof(DictionaryAddGetEnumeratorClass.Add))
        };

        // Custom add method.
        foreach (Type type in new Type[] { typeof(ClassWithAllowedContentTypes), typeof(PrivateClassWithAllowedContentTypes) })
        {
            yield return new object?[]
            {
                new XamlTypeInvoker(new XamlType(type, new XamlSchemaContext())),
                new XamlType(typeof(int), new XamlSchemaContext()),
                typeof(ICollection<int>).GetMethod(nameof(ICollection<int>.Add))
            };
            yield return new object?[]
            {
                new XamlTypeInvoker(new XamlType(type, new XamlSchemaContext())),
                new XamlType(typeof(string), new XamlSchemaContext()),
                type.GetMethod(nameof(ClassWithAllowedContentTypes.Add), new Type[] { typeof(string) })
            };
            yield return new object?[]
            {
                new XamlTypeInvoker(new CustomXamlType(type, new XamlSchemaContext())
                {
                    LookupAllowedContentTypesResult = new XamlType[]
                    {
                        new XamlType(typeof(string), new XamlSchemaContext()),
                        new XamlType(typeof(Array), new XamlSchemaContext()),
                        new XamlType(typeof(short), new XamlSchemaContext())
                    }
                }),
                new XamlType(typeof(string), new XamlSchemaContext()),
                type.GetMethod(nameof(ClassWithAllowedContentTypes.Add), new Type[] { typeof(string) })
            };
            yield return new object?[]
            {
                new XamlTypeInvoker(new XamlType(type, new XamlSchemaContext())),
                new XamlType(typeof(Array), new XamlSchemaContext()),   
                type.GetMethod(nameof(ClassWithAllowedContentTypes.Add), new Type[] { typeof(Array) })
            };
            yield return new object?[]
            {
                new XamlTypeInvoker(new XamlType(type, new XamlSchemaContext())),
                new XamlType(typeof(int[]), new XamlSchemaContext()),   
                type.GetMethod(nameof(ClassWithAllowedContentTypes.Add), new Type[] { typeof(Array) })
            };
            yield return new object?[]
            {
                new XamlTypeInvoker(new CustomXamlType(type, new XamlSchemaContext())
                {
                    LookupAllowedContentTypesResult = new XamlType[]
                    {
                        new XamlType(typeof(string), new XamlSchemaContext()),
                        new XamlType(typeof(Array), new XamlSchemaContext()),
                        new XamlType(typeof(double), new XamlSchemaContext())
                    }
                }),
                new XamlType(typeof(int[]), new XamlSchemaContext()),
                type.GetMethod(nameof(ClassWithAllowedContentTypes.Add), new Type[] { typeof(Array) })
            };
            yield return new object?[]
            {
                new XamlTypeInvoker(new CustomXamlType(type, new XamlSchemaContext())
                {
                    LookupAllowedContentTypesResult = new XamlType[]
                    {
                        new XamlType(typeof(string), new XamlSchemaContext()),
                        new XamlType(typeof(Array), new XamlSchemaContext()),
                        new XamlType(typeof(double), new XamlSchemaContext())
                    }
                }),
                new XamlType(typeof(double), new XamlSchemaContext()),
                null
            };
            yield return new object?[]
            {
                new XamlTypeInvoker(new CustomXamlType(type, new XamlSchemaContext())
                {
                    LookupAllowedContentTypesResult = new XamlType[]
                    {
                        new XamlType(typeof(string), new XamlSchemaContext()),
                        new XamlType(typeof(Array), new XamlSchemaContext()),
                        new XamlType(typeof(double), new XamlSchemaContext())
                    }
                }),
                new XamlType(typeof(object), new XamlSchemaContext()),
                null
            };
        }
    }

    [Theory]
    [MemberData(nameof(GetAddMethod_TestData))]
    public void GetAddMethod_Invoke_ReturnsExpected(XamlTypeInvoker invoker, XamlType contentType, MethodInfo expected)
    {
        Assert.Equal(expected, invoker.GetAddMethod(contentType));
        Assert.Equal(expected, invoker.GetAddMethod(contentType));
    }

    [Fact]
    public void GetAddMethod_NullContentType_ThrowsArgumentNullException()
    {
        XamlTypeInvoker invoker = XamlTypeInvoker.UnknownInvoker;
        Assert.Throws<ArgumentNullException>("contentType", () => invoker.GetAddMethod(null));
    }

#pragma warning disable IDE0060, IDE0051, CA1052 // Remove unused parameter, Remove unused member, Static holder types should be Static or NotInheritable
    [ContentWrapper(typeof(ClassWithStringContentPropertyAttribute))]
    [ContentWrapper(typeof(ClassWithArrayContentPropertyAttribute))]
    [ContentWrapper(typeof(ClassWithShortContentPropertyAttribute))]
    public class ClassWithAllowedContentTypes : List<int>
    {
        public void Add(string i) { }
        public void Add(Array i) { }
        private void Add(double i) { }
    }

    [ContentWrapper(typeof(ClassWithStringContentPropertyAttribute))]
    [ContentWrapper(typeof(ClassWithArrayContentPropertyAttribute))]
    [ContentWrapper(typeof(ClassWithShortContentPropertyAttribute))]
    private class PrivateClassWithAllowedContentTypes : List<int>
    {
        public void Add(string i) { }
        public void Add(Array i) { }
        private void Add(double i) { }
    }

    [ContentProperty(nameof(Name))]
    public class ClassWithStringContentPropertyAttribute
    {
        public string? Name { get; set; }
    }

    [ContentProperty(nameof(Name))]
    public class ClassWithArrayContentPropertyAttribute
    {
        public Array? Name { get; set; }
    }

    [ContentProperty(nameof(ClassWithShortContentPropertyAttribute.Name))]
    public class ClassWithShortContentPropertyAttribute
    {
        public short Name { get; set; }
    }

    [XamlSetMarkupExtension(nameof(ClassWithAttributes.MarkupExtensionMethod))]
    [XamlSetTypeConverter(nameof(ClassWithAttributes.TypeConverterMethod))]
    private class ClassWithAttributes
    {
        public static void MarkupExtensionMethod(object sender, XamlSetMarkupExtensionEventArgs e)
        {
        }
        public static void TypeConverterMethod(object sender, XamlSetTypeConverterEventArgs e)
        {
        }
    }
#pragma warning restore IDE0060, IDE0051, CA1052 // Remove unused parameter, Remove unused member, Static holder types should be Static or NotInheritable

    public static IEnumerable<object?[]> GetEnumeratorMethod_TestData()
    {
        yield return new object?[]
        {
            XamlTypeInvoker.UnknownInvoker,
            null
        };
        yield return new object?[]
        {
            new XamlTypeInvoker(new XamlType("namespace", "name", null, new XamlSchemaContext())),
            null
        };
        yield return new object?[]
        {
            new XamlTypeInvoker(new XamlType(typeof(int), new XamlSchemaContext())),
            null
        };

        // Collection.
        yield return new object?[]
        {
            new XamlTypeInvoker(new XamlType(typeof(ICollection<int>), new XamlSchemaContext())),
            typeof(IEnumerable).GetMethod(nameof(IEnumerable.GetEnumerator))
        };
        yield return new object?[]
        {
            new XamlTypeInvoker(new XamlType(typeof(List<int>), new XamlSchemaContext())),
            typeof(IEnumerable).GetMethod(nameof(IEnumerable.GetEnumerator))
        };
        yield return new object?[]
        {
            new XamlTypeInvoker(new XamlType(typeof(GetEnumeratorClass), new XamlSchemaContext())),
            typeof(GetEnumeratorClass).GetMethod(nameof(GetEnumeratorClass.GetEnumerator))
        };
        yield return new object?[]
        {
            new XamlTypeInvoker(new XamlType(typeof(BadReturnGetEnumeratorClass), new XamlSchemaContext())),
            null
        };
        yield return new object?[]
        {
            new XamlTypeInvoker(new XamlType(typeof(TooManyParametersGetEnumeratorClass), new XamlSchemaContext())),
            null
        };
    }

    [Theory]
    [MemberData(nameof(GetEnumeratorMethod_TestData))]
    public void GetEnumeratorMethod_Invoke_ReturnsExpected(XamlTypeInvoker invoker, MethodInfo expected)
    {
        Assert.Equal(expected, invoker.GetEnumeratorMethod());
        Assert.Equal(expected, invoker.GetEnumeratorMethod());
    }

    [Fact]
    public void GetItems_HasGetEnumeratorMethod_ReturnsExpected()
    {
        var invoker = new XamlTypeInvoker(new XamlType(typeof(GetEnumeratorClass), new XamlSchemaContext()));
        var instance = new GetEnumeratorClass();
        IEnumerator items = invoker.GetItems(instance);
        Assert.True(items.MoveNext());
        Assert.Equal(1, items.Current);
    }

    [Fact]
    public void GetItems_IEnumerableInstance_ReturnsExpected()
    {
        XamlTypeInvoker invoker = XamlTypeInvoker.UnknownInvoker;
        var instance = new List<int> { 1 };
        IEnumerator items = invoker.GetItems(instance);
        Assert.True(items.MoveNext());
        Assert.Equal(1, items.Current);
    }

    [Fact]
    public void GetItems_NullInstance_ThrowsArgumentNullException()
    {
        XamlTypeInvoker invoker = XamlTypeInvoker.UnknownInvoker;
        Assert.Throws<ArgumentNullException>("instance", () => invoker.GetItems(null));
    }

    [Theory]
    [MemberData(nameof(UnknownInvoker_TestData))]
    public void GetItems_UnknownInvoker_ThrowsNotSupportedException(XamlTypeInvoker invoker)
    {
        Assert.Throws<NotSupportedException>(() => invoker.GetItems(new object()));
    }

    [Fact]
    public void GetItems_TypeNotCollectionOrDictionary_ThrowsNotSupportedException()
    {
        var invoker = new XamlTypeInvoker(new XamlType(typeof(object), new XamlSchemaContext()));
        Assert.Throws<NotSupportedException>(() => invoker.GetItems(new object()));
    }

    private class GetEnumeratorClass
    {
        public IEnumerator GetEnumerator() => new List<int> { 1 }.GetEnumerator();
        public void Add(object value) => throw new NotImplementedException();
    }

    private class ListAddGetEnumeratorClass
    {
        public List<object> List { get; } = new List<object>();

        public IEnumerator GetEnumerator() => new List<int> { 1 }.GetEnumerator();
        public void Add(object value) => List.Add(value);
    }

    private class DictionaryAddGetEnumeratorClass
    {
        public Dictionary<object, object> Dictionary { get; } = new Dictionary<object, object>();

        public IEnumerator GetEnumerator() => new List<int> { 1 }.GetEnumerator();
        public void Add(object key, object value) => Dictionary.Add(key, value);
    }

    private class BadReturnGetEnumeratorClass
    {
        public void GetEnumerator() => throw new NotImplementedException();
        public void Add(object value) => throw new NotImplementedException();
    }

    private class TooManyParametersGetEnumeratorClass
    {
        public IEnumerator GetEnumerator(object i) => throw new NotImplementedException();
        public void Add(object value) => throw new NotImplementedException();
    }

    private class SubXamlTypeInvoker : XamlTypeInvoker
    {
        public SubXamlTypeInvoker() : base() { }
    }
}
