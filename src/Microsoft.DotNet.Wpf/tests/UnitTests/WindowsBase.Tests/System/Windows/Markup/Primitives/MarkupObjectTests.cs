// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Linq;

namespace System.Windows.Markup.Primitives.Tests;

public class MarkupObjectTests
{
    [Fact]
    public void Properties_GetObject_ReturnsExpected()
    {
        var instance = new object();
        MarkupObject obj = MarkupWriter.GetMarkupObjectFor(instance);
        Assert.NotNull(obj);
        Assert.NotEmpty(obj.Attributes);
        Assert.Same(obj.Attributes, obj.Attributes);
        Assert.Same(instance, obj.Instance);
        Assert.Equal(typeof(object), obj.ObjectType);
        Assert.Empty(obj.Properties);
        Assert.NotSame(obj.Properties, obj.Properties);
    }

    [Fact]
    public void Properties_GetConvertableToString_ReturnsExpected()
    {
        var instance = Rect.Empty;
        MarkupObject obj = MarkupWriter.GetMarkupObjectFor(instance);
        Assert.NotNull(obj);
        Assert.NotEmpty(obj.Attributes);
        Assert.Same(obj.Attributes, obj.Attributes);
        Assert.Equal(instance, obj.Instance);
        Assert.Equal(typeof(Rect), obj.ObjectType);
        Assert.NotEmpty(obj.Properties);
        Assert.NotSame(obj.Properties, obj.Properties);

        List<MarkupProperty> properties = obj.Properties.ToList();
        Assert.Single(properties);
        Assert.Empty(properties[0].Attributes);
        Assert.Same(properties[0].Attributes, properties[0].Attributes);
        Assert.Null(properties[0].DependencyProperty);
        Assert.False(properties[0].IsAttached);
        Assert.False(properties[0].IsComposite);
        Assert.False(properties[0].IsConstructorArgument);
        Assert.False(properties[0].IsContent);
        Assert.False(properties[0].IsKey);
        Assert.True(properties[0].IsValueAsString);
        Assert.Equal("StringValue", properties[0].Name);
        Assert.Null(properties[0].PropertyDescriptor);
        Assert.Equal(typeof(string), properties[0].PropertyType);
        Assert.Equal("Empty", properties[0].StringValue);
        Assert.Empty(properties[0].TypeReferences);
        Assert.Same(properties[0].TypeReferences, properties[0].TypeReferences);
        Assert.Null(properties[0].Items);
        Assert.Equal("Empty", properties[0].Value);
    }

    [Fact]
    public void Properties_GetArray_ReturnsExpected()
    {
        var instance = new int[] { 1, 2, 3 };
        MarkupObject obj = MarkupWriter.GetMarkupObjectFor(instance);
        Assert.NotNull(obj);
        Assert.NotEmpty(obj.Attributes);
        Assert.Same(obj.Attributes, obj.Attributes);
        Assert.Equal(instance, obj.Instance);
        Assert.Equal(typeof(int[]), obj.ObjectType);
        Assert.NotEmpty(obj.Properties);
        Assert.NotSame(obj.Properties, obj.Properties);

        List<MarkupProperty> properties = obj.Properties.ToList();
        Assert.Single(properties);
        Assert.Empty(properties[0].Attributes);
        Assert.Same(properties[0].Attributes, properties[0].Attributes);
        Assert.Null(properties[0].DependencyProperty);
        Assert.False(properties[0].IsAttached);
        Assert.True(properties[0].IsComposite);
        Assert.False(properties[0].IsConstructorArgument);
        Assert.True(properties[0].IsContent);
        Assert.False(properties[0].IsKey);
        Assert.False(properties[0].IsValueAsString);
        Assert.Equal("Items", properties[0].Name);
        Assert.Null(properties[0].PropertyDescriptor);
        Assert.Equal(typeof(IEnumerable), properties[0].PropertyType);
        Assert.Equal("", properties[0].StringValue);
        Assert.Empty(properties[0].TypeReferences);
        Assert.Same(properties[0].TypeReferences, properties[0].TypeReferences);
        Assert.Equal(instance.Length, properties[0].Items.Count());
        Assert.NotSame(properties[0].Items, properties[0].Items);
        Assert.Equal(instance, Assert.IsType<ArrayExtension>(properties[0].Value).Items);
    }

    [Fact]
    public void Properties_GetList_ReturnsExpected()
    {
        var instance = new List<int> { 1, 2, 3 };
        MarkupObject obj = MarkupWriter.GetMarkupObjectFor(instance);
        Assert.NotNull(obj);
        Assert.NotEmpty(obj.Attributes);
        Assert.Same(obj.Attributes, obj.Attributes);
        Assert.Equal(instance, obj.Instance);
        Assert.Equal(typeof(List<int>), obj.ObjectType);
        Assert.NotEmpty(obj.Properties);
        Assert.NotSame(obj.Properties, obj.Properties);

        List<MarkupProperty> properties = obj.Properties.ToList();
        Assert.Equal(2, properties.Count);
        Assert.NotEmpty(properties[0].Attributes);
        Assert.Same(properties[0].Attributes, properties[0].Attributes);
        Assert.Null(properties[0].DependencyProperty);
        Assert.False(properties[0].IsAttached);
        Assert.False(properties[0].IsComposite);
        Assert.False(properties[0].IsConstructorArgument);
        Assert.False(properties[0].IsContent);
        Assert.False(properties[0].IsKey);
        Assert.False(properties[0].IsValueAsString);
        Assert.Equal("Capacity", properties[0].Name);
        Assert.NotNull(properties[0].PropertyDescriptor);
        Assert.Equal(typeof(int), properties[0].PropertyType);
        Assert.NotEmpty(properties[0].StringValue);
        Assert.Empty(properties[0].TypeReferences);
        Assert.Same(properties[0].TypeReferences, properties[0].TypeReferences);
        Assert.NotSame(obj, Assert.Single(properties[0].Items));
        Assert.NotSame(properties[0].Items, properties[0].Items);
        Assert.NotEqual(0, properties[0].Value);

        Assert.Empty(properties[1].Attributes);
        Assert.Same(properties[1].Attributes, properties[1].Attributes);
        Assert.Null(properties[1].DependencyProperty);
        Assert.False(properties[1].IsAttached);
        Assert.True(properties[1].IsComposite);
        Assert.False(properties[1].IsConstructorArgument);
        Assert.True(properties[1].IsContent);
        Assert.False(properties[1].IsKey);
        Assert.False(properties[1].IsValueAsString);
        Assert.Equal("Items", properties[1].Name);
        Assert.Null(properties[1].PropertyDescriptor);
        Assert.Equal(typeof(IEnumerable), properties[1].PropertyType);
        Assert.Empty(properties[1].StringValue);
        Assert.Empty(properties[1].TypeReferences);
        Assert.Same(properties[1].TypeReferences, properties[1].TypeReferences);
        Assert.Equal(instance.Count, properties[1].Items.Count());
        Assert.NotSame(properties[1].Items, properties[1].Items);
        Assert.Same(instance, properties[1].Value);
    }

    [Fact]
    public void Properties_GetDictionary_ReturnsExpected()
    {
        var instance = new Dictionary<int, string>
        {
            { 1, "value1" },
            { 2, "value2" },
            { 3, "value3" }
        };
        MarkupObject obj = MarkupWriter.GetMarkupObjectFor(instance);
        Assert.NotNull(obj);
        Assert.NotEmpty(obj.Attributes);
        Assert.Same(obj.Attributes, obj.Attributes);
        Assert.Equal(instance, obj.Instance);
        Assert.Equal(typeof(Dictionary<int, string>), obj.ObjectType);
        Assert.NotEmpty(obj.Properties);
        Assert.NotSame(obj.Properties, obj.Properties);

        List<MarkupProperty> properties = obj.Properties.ToList();
        Assert.Single(properties);
        Assert.Empty(properties[0].Attributes);
        Assert.Same(properties[0].Attributes, properties[0].Attributes);
        Assert.Null(properties[0].DependencyProperty);
        Assert.False(properties[0].IsAttached);
        Assert.True(properties[0].IsComposite);
        Assert.False(properties[0].IsConstructorArgument);
        Assert.True(properties[0].IsContent);
        Assert.False(properties[0].IsKey);
        Assert.False(properties[0].IsValueAsString);
        Assert.Equal("Entries", properties[0].Name);
        Assert.Null(properties[0].PropertyDescriptor);
        Assert.Equal(typeof(IDictionary), properties[0].PropertyType);
        Assert.Equal("", properties[0].StringValue);
        Assert.Empty(properties[0].TypeReferences);
        Assert.Same(properties[0].TypeReferences, properties[0].TypeReferences);
        Assert.Equal(instance.Count, properties[0].Items.Count());
        Assert.NotSame(properties[0].Items, properties[0].Items);
        Assert.Same(instance, properties[0].Value);
    }

    [Fact]
    public void Properties_GetEnumerable_ReturnsExpected()
    {
        var list = new List<int> { 1, 2, 3 };
        var instance = new IEnumerableWrapper(list);
        MarkupObject obj = MarkupWriter.GetMarkupObjectFor(instance);
        Assert.NotNull(obj);
        Assert.NotEmpty(obj.Attributes);
        Assert.Same(obj.Attributes, obj.Attributes);
        Assert.Equal(instance, obj.Instance);
        Assert.Equal(typeof(IEnumerableWrapper), obj.ObjectType);
        Assert.NotEmpty(obj.Properties);
        Assert.NotSame(obj.Properties, obj.Properties);

        List<MarkupProperty> properties = obj.Properties.ToList();
        Assert.Single(properties);
        Assert.Empty(properties[0].Attributes);
        Assert.Same(properties[0].Attributes, properties[0].Attributes);
        Assert.Null(properties[0].DependencyProperty);
        Assert.False(properties[0].IsAttached);
        Assert.True(properties[0].IsComposite);
        Assert.False(properties[0].IsConstructorArgument);
        Assert.True(properties[0].IsContent);
        Assert.False(properties[0].IsKey);
        Assert.False(properties[0].IsValueAsString);
        Assert.Equal("Items", properties[0].Name);
        Assert.Null(properties[0].PropertyDescriptor);
        Assert.Equal(typeof(IEnumerable), properties[0].PropertyType);
        Assert.Equal("", properties[0].StringValue);
        Assert.Empty(properties[0].TypeReferences);
        Assert.Same(properties[0].TypeReferences, properties[0].TypeReferences);
        Assert.Equal(list.Count, properties[0].Items.Count());
        Assert.NotSame(properties[0].Items, properties[0].Items);
        Assert.Same(instance, properties[0].Value);
    }

    [Fact]
    public void Properties_GetNormalProperty_ReturnsExpected()
    {
        var instance = new CustomClass
        {
            Property = 1
        };
        MarkupObject obj = MarkupWriter.GetMarkupObjectFor(instance);
        Assert.NotNull(obj);
        Assert.Empty(obj.Attributes);
        Assert.Same(obj.Attributes, obj.Attributes);
        Assert.Same(instance, obj.Instance);
        Assert.Equal(typeof(CustomClass), obj.ObjectType);
        Assert.NotEmpty(obj.Properties);
        Assert.NotSame(obj.Properties, obj.Properties);

        List<MarkupProperty> properties = obj.Properties.ToList();
        Assert.Single(properties);
        Assert.NotEmpty(properties[0].Attributes);
        Assert.Same(properties[0].Attributes, properties[0].Attributes);
        Assert.Null(properties[0].DependencyProperty);
        Assert.False(properties[0].IsAttached);
        Assert.False(properties[0].IsComposite);
        Assert.False(properties[0].IsConstructorArgument);
        Assert.False(properties[0].IsContent);
        Assert.False(properties[0].IsKey);
        Assert.False(properties[0].IsValueAsString);
        Assert.Equal(nameof(CustomClass.Property), properties[0].Name);
        Assert.NotNull(properties[0].PropertyDescriptor);
        Assert.Equal(typeof(int), properties[0].PropertyType);
        Assert.Equal("1", properties[0].StringValue);
        Assert.Empty(properties[0].TypeReferences);
        Assert.Same(properties[0].TypeReferences, properties[0].TypeReferences);
        Assert.NotSame(obj, Assert.Single(properties[0].Items));
        Assert.NotSame(properties[0].Items, properties[0].Items);
        Assert.Equal(1, properties[0].Value);
    }

    [Fact]
    public void Properties_GetArrayProperty_ReturnsExpected()
    {
        var array = new int[] { 1, 2, 3 };
        var instance = new ArrayClass
        {
            Property = array
        };
        MarkupObject obj = MarkupWriter.GetMarkupObjectFor(instance);
        Assert.NotNull(obj);
        Assert.NotEmpty(obj.Attributes);
        Assert.Same(obj.Attributes, obj.Attributes);
        Assert.Same(instance, obj.Instance);
        Assert.Equal(typeof(ArrayClass), obj.ObjectType);
        Assert.NotEmpty(obj.Properties);
        Assert.NotSame(obj.Properties, obj.Properties);

        List<MarkupProperty> properties = obj.Properties.ToList();
        Assert.Single(properties);
        Assert.NotEmpty(properties[0].Attributes);
        Assert.Same(properties[0].Attributes, properties[0].Attributes);
        Assert.Null(properties[0].DependencyProperty);
        Assert.False(properties[0].IsAttached);
        Assert.True(properties[0].IsComposite);
        Assert.False(properties[0].IsConstructorArgument);
        Assert.False(properties[0].IsContent);
        Assert.False(properties[0].IsKey);
        Assert.False(properties[0].IsValueAsString);
        Assert.Equal(nameof(ArrayClass.Property), properties[0].Name);
        Assert.NotNull(properties[0].PropertyDescriptor);
        Assert.Equal(typeof(int[]), properties[0].PropertyType);
        Assert.Empty(properties[0].StringValue);
        Assert.Empty(properties[0].TypeReferences);
        Assert.Same(properties[0].TypeReferences, properties[0].TypeReferences);
        Assert.NotSame(obj, Assert.Single(properties[0].Items));
        Assert.NotSame(properties[0].Items, properties[0].Items);
        Assert.Equal(array, Assert.IsType<ArrayExtension>(properties[0].Value).Items);
    }

    [Fact]
    public void Properties_GetListProperty_ReturnsExpected()
    {
        var list = new List<int> { 1, 2, 3 };
        var instance = new ListClass
        {
            Property = list
        };
        MarkupObject obj = MarkupWriter.GetMarkupObjectFor(instance);
        Assert.NotNull(obj);
        Assert.NotEmpty(obj.Attributes);
        Assert.Same(obj.Attributes, obj.Attributes);
        Assert.Same(instance, obj.Instance);
        Assert.Equal(typeof(ListClass), obj.ObjectType);
        Assert.NotEmpty(obj.Properties);
        Assert.NotSame(obj.Properties, obj.Properties);

        List<MarkupProperty> properties = obj.Properties.ToList();
        Assert.Single(properties);
        Assert.NotEmpty(properties[0].Attributes);
        Assert.Same(properties[0].Attributes, properties[0].Attributes);
        Assert.Null(properties[0].DependencyProperty);
        Assert.False(properties[0].IsAttached);
        Assert.True(properties[0].IsComposite);
        Assert.False(properties[0].IsConstructorArgument);
        Assert.False(properties[0].IsContent);
        Assert.False(properties[0].IsKey);
        Assert.False(properties[0].IsValueAsString);
        Assert.Equal(nameof(ListClass.Property), properties[0].Name);
        Assert.NotNull(properties[0].PropertyDescriptor);
        Assert.Equal(typeof(IList), properties[0].PropertyType);
        Assert.Empty(properties[0].StringValue);
        Assert.Empty(properties[0].TypeReferences);
        Assert.Same(properties[0].TypeReferences, properties[0].TypeReferences);
        Assert.NotSame(obj, Assert.Single(properties[0].Items));
        Assert.NotSame(properties[0].Items, properties[0].Items);
        Assert.Equal(list, properties[0].Value);
    }

    [Fact]
    public void Properties_GetDictionaryProperty_ReturnsExpected()
    {
        var dictionary = new Dictionary<int, string>
        {
            { 1, "value1" },
            { 2, "value2" },
            { 3, "value3" }
        };
        var instance = new DictionaryClass
        {
            Property = dictionary
        };
        MarkupObject obj = MarkupWriter.GetMarkupObjectFor(instance);
        Assert.NotNull(obj);
        Assert.NotEmpty(obj.Attributes);
        Assert.Same(obj.Attributes, obj.Attributes);
        Assert.Same(instance, obj.Instance);
        Assert.Equal(typeof(DictionaryClass), obj.ObjectType);
        Assert.NotEmpty(obj.Properties);
        Assert.NotSame(obj.Properties, obj.Properties);

        List<MarkupProperty> properties = obj.Properties.ToList();
        Assert.Single(properties);
        Assert.NotEmpty(properties[0].Attributes);
        Assert.Same(properties[0].Attributes, properties[0].Attributes);
        Assert.Null(properties[0].DependencyProperty);
        Assert.False(properties[0].IsAttached);
        Assert.True(properties[0].IsComposite);
        Assert.False(properties[0].IsConstructorArgument);
        Assert.False(properties[0].IsContent);
        Assert.False(properties[0].IsKey);
        Assert.False(properties[0].IsValueAsString);
        Assert.Equal(nameof(DictionaryClass.Property), properties[0].Name);
        Assert.NotNull(properties[0].PropertyDescriptor);
        Assert.Equal(typeof(IDictionary), properties[0].PropertyType);
        Assert.Empty(properties[0].StringValue);
        Assert.Empty(properties[0].TypeReferences);
        Assert.Same(properties[0].TypeReferences, properties[0].TypeReferences);
        Assert.NotSame(obj, Assert.Single(properties[0].Items));
        Assert.NotSame(properties[0].Items, properties[0].Items);
        Assert.Equal(dictionary, properties[0].Value);
    }

    [Fact]
    public void Properties_GetMarkupExtensionDefault_ReturnsExpected()
    {
        var instance = new DefaultMarkupExtension();
        MarkupObject obj = MarkupWriter.GetMarkupObjectFor(instance);
        Assert.NotNull(obj);
        Assert.NotEmpty(obj.Attributes);
        Assert.Same(obj.Attributes, obj.Attributes);
        Assert.Same(instance, obj.Instance);
        Assert.Equal(typeof(DefaultMarkupExtension), obj.ObjectType);
        Assert.Empty(obj.Properties);
        Assert.NotSame(obj.Properties, obj.Properties);
    }

    [Fact]
    public void Properties_GetMarkupExtensionWithTypeConverter_ReturnsExpected()
    {
        var instance = new MarkupExtensionWithTypeConverter();
        MarkupObject obj = MarkupWriter.GetMarkupObjectFor(instance);
        Assert.NotNull(obj);
        Assert.NotEmpty(obj.Attributes);
        Assert.Same(obj.Attributes, obj.Attributes);
        Assert.Same(instance, obj.Instance);
        Assert.Equal(typeof(MarkupExtensionWithTypeConverter), obj.ObjectType);
        Assert.NotEmpty(obj.Properties);
        Assert.NotSame(obj.Properties, obj.Properties);

        List<MarkupProperty> properties = obj.Properties.ToList();
        Assert.Equal(2, properties.Count);
        Assert.Empty(properties[0].Attributes);
        Assert.Same(properties[0].Attributes, properties[0].Attributes);
        Assert.Null(properties[0].DependencyProperty);
        Assert.False(properties[0].IsAttached);
        Assert.False(properties[0].IsComposite);
        Assert.True(properties[0].IsConstructorArgument);
        Assert.False(properties[0].IsContent);
        Assert.False(properties[0].IsKey);
        Assert.False(properties[0].IsValueAsString);
        Assert.Equal("Argument", properties[0].Name);
        Assert.Null(properties[0].PropertyDescriptor);
        Assert.Equal(typeof(int), properties[0].PropertyType);
        Assert.Equal("1", properties[0].StringValue);
        Assert.Empty(properties[0].TypeReferences);
        Assert.Same(properties[0].TypeReferences, properties[0].TypeReferences);
        Assert.NotSame(obj, Assert.Single(properties[0].Items));
        Assert.NotSame(properties[0].Items, properties[0].Items);
        Assert.Equal(1, properties[0].Value);

        Assert.Empty(properties[1].Attributes);
        Assert.Same(properties[1].Attributes, properties[1].Attributes);
        Assert.Null(properties[1].DependencyProperty);
        Assert.False(properties[1].IsAttached);
        Assert.False(properties[1].IsComposite);
        Assert.True(properties[1].IsConstructorArgument);
        Assert.False(properties[1].IsContent);
        Assert.False(properties[1].IsKey);
        Assert.False(properties[1].IsValueAsString);
        Assert.Equal("Argument", properties[1].Name);
        Assert.Null(properties[1].PropertyDescriptor);
        Assert.Equal(typeof(int), properties[1].PropertyType);
        Assert.Equal("2", properties[1].StringValue);
        Assert.Empty(properties[1].TypeReferences);
        Assert.Same(properties[1].TypeReferences, properties[1].TypeReferences);
        Assert.NotSame(obj, Assert.Single(properties[1].Items));
        Assert.NotSame(properties[1].Items, properties[1].Items);
        Assert.Equal(2, properties[1].Value);
    }

    private class DefaultMarkupExtension : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider) => throw new NotImplementedException();
    }

    private class CustomTypeConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                var ctor = typeof(MarkupExtensionWithTypeConverter).GetConstructor(new[] { typeof(int), typeof(int) });
                Assert.NotNull(ctor);
                return new InstanceDescriptor(ctor, new object[] { 1, 2 });
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

#pragma warning disable IDE0060 // Remove unused parameter
    [TypeConverter(typeof(CustomTypeConverter))]
    private class MarkupExtensionWithTypeConverter : MarkupExtension
    {
        public MarkupExtensionWithTypeConverter()
        {
        }
        
        public MarkupExtensionWithTypeConverter(int x, int y)
        {
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => throw new NotImplementedException();
    }
#pragma warning restore IDE0060 // Remove unused parameter

    private class CustomClass
    {
        public int Property { get; set; }
    }

    private class ArrayClass
    {
        public int[]? Property { get; set; }
    }

    private class ListClass
    {
        public IList? Property { get; set; }
    }

    private class DictionaryClass
    {
        public IDictionary? Property { get; set; }
    }

    private class IEnumerableWrapper : IEnumerable
    {
        private readonly IEnumerable _enumerable;

        public IEnumerableWrapper(IEnumerable enumerable)
        {
            _enumerable = enumerable;
        }

        public IEnumerator GetEnumerator() => _enumerable.GetEnumerator();
    }
}
