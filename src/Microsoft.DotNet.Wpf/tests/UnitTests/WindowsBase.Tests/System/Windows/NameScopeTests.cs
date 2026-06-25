// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Linq;
using System.Windows.Markup;

namespace System.Windows.Tests;

public class NameScopeTests
{
#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.
#pragma warning disable xUnit2017 // Do not use Contains() to check if a value exists in a collection

    [Fact]
    public void Ctor_Default()
    {
        var nameScope = new NameScope();
        Assert.Equal(0, nameScope.Count);
        Assert.False(nameScope.IsReadOnly);
        Assert.Null(nameScope.Keys);
        Assert.Null(nameScope.Values);
    }

    [Fact]
    public void GetNameScope_InvokeDefault_ReturnsExpected()
    {
        var dependencyObject = new DependencyObject();
        Assert.Null(NameScope.GetNameScope(dependencyObject));
    }

    [Fact]
    public void GetNameScope_NullDependencyObject_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("dependencyObject", () => NameScope.GetNameScope(null));
    }

    [Fact]
    public void SetNameScope_Invoke_GetNameScopeReturnsExpected()
    {
        var dependencyObject = new DependencyObject();
        var nameScope1 = new CustomNameScope();
        var nameScope2 = new CustomNameScope();

        // Set.
        NameScope.SetNameScope(dependencyObject, nameScope1);
        Assert.Same(nameScope1, NameScope.GetNameScope(dependencyObject));
        
        // Set same.
        NameScope.SetNameScope(dependencyObject, nameScope1);
        Assert.Same(nameScope1, NameScope.GetNameScope(dependencyObject));
        
        // Set different.
        NameScope.SetNameScope(dependencyObject, nameScope2);
        Assert.Same(nameScope2, NameScope.GetNameScope(dependencyObject));
    }

    [Fact]
    public void SetNameScope_NullDependencyObject_ThrowsArgumentNullException()
    {
        var nameScope = new CustomNameScope();
        Assert.Throws<ArgumentNullException>("dependencyObject", () => NameScope.SetNameScope(null, nameScope));
    }

    [Fact]
    public void Item_Get_ReturnsExpected()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope.RegisterName("name", scopedElement);
        Assert.Same(scopedElement, nameScope["name"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("0name")]
    [InlineData("na\0me")]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("_")]
    [InlineData("na0me")]
    [InlineData("_name")]
    [InlineData("NaMe")]
    public void Item_GetNoSuchNameEmpty_ReturnsNull(string name)
    {
        var nameScope = new NameScope();
        Assert.Null(nameScope[name]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("0name")]
    [InlineData("na\0me")]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("_")]
    [InlineData("na0me")]
    [InlineData("_name")]
    [InlineData("NaMe")]
    public void Item_GetNoSuchNameNotEmpty_ReturnsNull(string name)
    {
        var nameScope = new NameScope();
        nameScope.RegisterName("name1", new object());

        Assert.Null(nameScope[name]);
    }

    [Fact]
    public void Item_GetNullKey_ThrowsArgumentNullException()
    {
        var nameScope = new NameScope();
        Assert.Throws<ArgumentNullException>("key", () => nameScope[null]);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("_")]
    [InlineData("na0me")]
    [InlineData("_name")]
    [InlineData("NaMe")]
    public void Item_SetInvoke_Success(string name)
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope[name] = scopedElement;
        Assert.Equal(1, nameScope.Count);
        Assert.Same(scopedElement, nameScope.FindName(name));
        Assert.True(nameScope.ContainsKey(name));
        Assert.True(nameScope.Contains(new KeyValuePair<string, object>(name, scopedElement)));
        Assert.Equal(1, nameScope.Keys.Count);
        Assert.NotSame(nameScope.Keys, nameScope.Keys);
        Assert.Equal(name, nameScope.Keys.First());
        Assert.Equal(1, nameScope.Values.Count);
        Assert.NotSame(nameScope.Values, nameScope.Values);
        Assert.Equal(scopedElement, nameScope.Values.First());

        // Set same.
        nameScope[name] = scopedElement;
        Assert.Equal(1, nameScope.Count);
        Assert.Same(scopedElement, nameScope.FindName(name));
        Assert.True(nameScope.ContainsKey(name));
        Assert.True(nameScope.Contains(new KeyValuePair<string, object>(name, scopedElement)));
        Assert.Equal(1, nameScope.Keys.Count);
        Assert.NotSame(nameScope.Keys, nameScope.Keys);
        Assert.Equal(name, nameScope.Keys.First());
        Assert.Equal(1, nameScope.Values.Count);
        Assert.NotSame(nameScope.Values, nameScope.Values);
        Assert.Equal(scopedElement, nameScope.Values.First());
    }

    [Fact]
    public void Item_SetInvokeMultiple_Success()
    {
        var nameScope = new NameScope();
        var scopedElement1 = new object();
        var scopedElement2 = new object();

        nameScope["name1"] = scopedElement1;
        Assert.Same(scopedElement1, nameScope.FindName("name1"));
        Assert.Equal(1, nameScope.Count);

        nameScope["NAME1"] = scopedElement2;
        Assert.Same(scopedElement1, nameScope.FindName("name1"));
        Assert.Same(scopedElement2, nameScope.FindName("NAME1"));
        Assert.Equal(2, nameScope.Count);

        nameScope["name2"] = scopedElement2;
        Assert.Same(scopedElement1, nameScope.FindName("name1"));
        Assert.Same(scopedElement2, nameScope.FindName("NAME1"));
        Assert.Same(scopedElement2, nameScope.FindName("name2"));
        Assert.Equal(3, nameScope.Count);
    }

    [Fact]
    public void Item_SetNullName_ThrowsArgumentNullException()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        Assert.Throws<ArgumentNullException>("key", () => nameScope.Add(null, scopedElement));
    }

    [Fact]
    public void Item_SetEmptyName_ThrowsArgumentException()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        Assert.Throws<ArgumentException>(() => nameScope.Add(string.Empty, scopedElement));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0name")]
    [InlineData("na\0me")]
    public void Item_SetInvalidName_ThrowsArgumentException(string name)
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        Assert.Throws<ArgumentException>(() => nameScope[name] = scopedElement);
    }

    [Fact]
    public void Item_SetNullValue_ThrowsArgumentNullException()
    {
        var nameScope = new NameScope();
        Assert.Throws<ArgumentNullException>("value", () => nameScope["name"] = null);
    }

    [Fact]
    public void Item_SetDuplicate_ThrowsArgumentException()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope["name"] = scopedElement;
        Assert.Throws<ArgumentException>(() => nameScope["name"] = new object());
    }

    [Fact]
    public void NameScopeProperty_Get_ReturnsExpected()
    {
        DependencyProperty property = NameScope.NameScopeProperty;
        Assert.NotNull(property.DefaultMetadata);
        Assert.Same(property.DefaultMetadata, property.DefaultMetadata);
        Assert.Null(property.DefaultMetadata.CoerceValueCallback);
        Assert.Null(property.DefaultMetadata.DefaultValue);
        Assert.Null(property.DefaultMetadata.PropertyChangedCallback);
        Assert.True(property.GlobalIndex >= 0);
        Assert.Equal("NameScope", property.Name);
        Assert.Equal(typeof(NameScope), property.OwnerType);
        Assert.Equal(typeof(INameScope), property.PropertyType);
        Assert.False(property.ReadOnly);
        Assert.Null(property.ValidateValueCallback);
        Assert.Same(property, NameScope.NameScopeProperty);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("_")]
    [InlineData("na0me")]
    [InlineData("_name")]
    [InlineData("NaMe")]
    public void Add_InvokeStringObject_Success(string name)
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope.Add(name, scopedElement);
        Assert.Equal(1, nameScope.Count);
        Assert.Same(scopedElement, nameScope.FindName(name));
        Assert.True(nameScope.ContainsKey(name));
        Assert.True(nameScope.Contains(new KeyValuePair<string, object>(name, scopedElement)));
        Assert.Equal(1, nameScope.Keys.Count);
        Assert.NotSame(nameScope.Keys, nameScope.Keys);
        Assert.Equal(name, nameScope.Keys.First());
        Assert.Equal(1, nameScope.Values.Count);
        Assert.NotSame(nameScope.Values, nameScope.Values);
        Assert.Equal(scopedElement, nameScope.Values.First());

        // Set same.
        nameScope.Add(name, scopedElement);
        Assert.Equal(1, nameScope.Count);
        Assert.Same(scopedElement, nameScope.FindName(name));
        Assert.True(nameScope.ContainsKey(name));
        Assert.True(nameScope.Contains(new KeyValuePair<string, object>(name, scopedElement)));
        Assert.Equal(1, nameScope.Keys.Count);
        Assert.NotSame(nameScope.Keys, nameScope.Keys);
        Assert.Equal(name, nameScope.Keys.First());
        Assert.Equal(1, nameScope.Values.Count);
        Assert.NotSame(nameScope.Values, nameScope.Values);
        Assert.Equal(scopedElement, nameScope.Values.First());
    }

    [Fact]
    public void Add_InvokeStringObjectMultiple_Success()
    {
        var nameScope = new NameScope();
        var scopedElement1 = new object();
        var scopedElement2 = new object();

        nameScope.Add("name1", scopedElement1);
        Assert.Same(scopedElement1, nameScope.FindName("name1"));
        Assert.Equal(1, nameScope.Count);

        nameScope.Add("NAME1", scopedElement2);
        Assert.Same(scopedElement1, nameScope.FindName("name1"));
        Assert.Same(scopedElement2, nameScope.FindName("NAME1"));
        Assert.Equal(2, nameScope.Count);

        nameScope.Add("name2", scopedElement2);
        Assert.Same(scopedElement1, nameScope.FindName("name1"));
        Assert.Same(scopedElement2, nameScope.FindName("NAME1"));
        Assert.Same(scopedElement2, nameScope.FindName("name2"));
        Assert.Equal(3, nameScope.Count);
    }

    [Fact]
    public void Add_NullKey_ThrowsArgumentNullException()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        Assert.Throws<ArgumentNullException>(() => nameScope.Add(null, scopedElement));
    }

    [Fact]
    public void Add_EmptyName_ThrowsArgumentException()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        Assert.Throws<ArgumentException>(() => nameScope.Add(string.Empty, scopedElement));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0name")]
    [InlineData("na\0me")]
    public void Add_InvalidName_ThrowsArgumentException(string name)
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        Assert.Throws<ArgumentException>(() => nameScope.Add(name, scopedElement));
    }

    [Fact]
    public void Add_NullScopedElement_ThrowsArgumentNullException()
    {
        var nameScope = new NameScope();
        Assert.Throws<ArgumentNullException>("scopedElement", () => nameScope.Add("name", null!));
    }

    [Theory]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("_")]
    [InlineData("na0me")]
    [InlineData("_name")]
    [InlineData("NaMe")]
    public void Add_InvokeKVPStringObject_Success(string name)
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope.Add(new KeyValuePair<string, object>(name, scopedElement));
        Assert.Equal(1, nameScope.Count);
        Assert.Same(scopedElement, nameScope.FindName(name));
        Assert.True(nameScope.ContainsKey(name));
        Assert.True(nameScope.Contains(new KeyValuePair<string, object>(name, scopedElement)));
        Assert.Equal(1, nameScope.Keys.Count);
        Assert.NotSame(nameScope.Keys, nameScope.Keys);
        Assert.Equal(name, nameScope.Keys.First());
        Assert.Equal(1, nameScope.Values.Count);
        Assert.NotSame(nameScope.Values, nameScope.Values);
        Assert.Equal(scopedElement, nameScope.Values.First());

        // Set same.
        nameScope.Add(new KeyValuePair<string, object>(name, scopedElement));
        Assert.Equal(1, nameScope.Count);
        Assert.Same(scopedElement, nameScope.FindName(name));
        Assert.True(nameScope.ContainsKey(name));
        Assert.True(nameScope.Contains(new KeyValuePair<string, object>(name, scopedElement)));
        Assert.Equal(1, nameScope.Keys.Count);
        Assert.NotSame(nameScope.Keys, nameScope.Keys);
        Assert.Equal(name, nameScope.Keys.First());
        Assert.Equal(1, nameScope.Values.Count);
        Assert.NotSame(nameScope.Values, nameScope.Values);
        Assert.Equal(scopedElement, nameScope.Values.First());
    }

    [Fact]
    public void Add_InvokeKVPStringObjectMultiple_Success()
    {
        var nameScope = new NameScope();
        var scopedElement1 = new object();
        var scopedElement2 = new object();

        nameScope.Add(new KeyValuePair<string, object>("name1", scopedElement1));
        Assert.Same(scopedElement1, nameScope.FindName("name1"));
        Assert.Equal(1, nameScope.Count);

        nameScope.Add(new KeyValuePair<string, object>("NAME1", scopedElement2));
        Assert.Same(scopedElement1, nameScope.FindName("name1"));
        Assert.Same(scopedElement2, nameScope.FindName("NAME1"));
        Assert.Equal(2, nameScope.Count);

        nameScope.Add(new KeyValuePair<string, object>("name2", scopedElement2));
        Assert.Same(scopedElement1, nameScope.FindName("name1"));
        Assert.Same(scopedElement2, nameScope.FindName("NAME1"));
        Assert.Same(scopedElement2, nameScope.FindName("name2"));
        Assert.Equal(3, nameScope.Count);
    }

    [Fact]
    public void Add_NullItemKey_ThrowsArgumentException()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        Assert.Throws<ArgumentException>("item", () => nameScope.Add(new KeyValuePair<string, object>(null!, scopedElement)));
    }

    [Fact]
    public void Add_EmptyItemKey_ThrowsArgumentException()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        Assert.Throws<ArgumentException>(() => nameScope.Add(new KeyValuePair<string, object>(string.Empty, scopedElement)));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0name")]
    [InlineData("na\0me")]
    public void Add_InvalidItemKey_ThrowsArgumentException(string name)
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        Assert.Throws<ArgumentException>(() => nameScope.Add(new KeyValuePair<string, object>(name, scopedElement)));
    }

    [Fact]
    public void Add_NullItemValue_ThrowsArgumentException()
    {
        var nameScope = new NameScope();
        Assert.Throws<ArgumentException>("item", () => nameScope.Add(new KeyValuePair<string, object>("name", null!)));
    }

    [Fact]
    public void Add_Duplicate_ThrowsArgumentException()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope.Add("name", scopedElement);
        Assert.Throws<ArgumentException>(() => nameScope.Add("name", new object()));
        Assert.Throws<ArgumentException>(() => nameScope.Add(new KeyValuePair<string, object>("name", new object())));
    }

    [Fact]
    public void Clear_Invoke_Success()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope.Add("name", scopedElement);
        Assert.Equal(1, nameScope.Count);

        nameScope.Clear();
        Assert.Equal(0, nameScope.Count);
        Assert.Null(nameScope.FindName("name"));
        Assert.False(nameScope.ContainsKey("name"));
        Assert.False(nameScope.Contains(new KeyValuePair<string, object>("name", scopedElement)));
        Assert.Null(nameScope.Keys);
        Assert.Null(nameScope.Values);

        // Clear again.
        nameScope.Clear();
        Assert.Equal(0, nameScope.Count);
        Assert.Null(nameScope.FindName("name"));
        Assert.False(nameScope.ContainsKey("name"));
        Assert.False(nameScope.Contains(new KeyValuePair<string, object>("name", scopedElement)));
        Assert.Null(nameScope.Keys);
        Assert.Null(nameScope.Values);
    }

    [Fact]
    public void Clear_InvokeEmpty_Success()
    {
        var nameScope = new NameScope();

        // Clear.
        nameScope.Clear();
        Assert.Equal(0, nameScope.Count);
        Assert.Null(nameScope.FindName("name"));
        Assert.False(nameScope.ContainsKey("name"));
        Assert.False(nameScope.Contains(new KeyValuePair<string, object>("name", new object())));
        Assert.Null(nameScope.Keys);
        Assert.Null(nameScope.Values);

        // Clear again.
        nameScope.Clear();
        Assert.Equal(0, nameScope.Count);
        Assert.Null(nameScope.FindName("name"));
        Assert.False(nameScope.ContainsKey("name"));
        Assert.False(nameScope.Contains(new KeyValuePair<string, object>("name", new object())));
        Assert.Null(nameScope.Keys);
        Assert.Null(nameScope.Values);
    }

    [Fact]
    public void CopyTo_Invoke_Success()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope.RegisterName("name", scopedElement);

        var array = new KeyValuePair<string, object>[3];

        // Copy to start.
        nameScope.CopyTo(array, 0);
        Assert.Equal(new KeyValuePair<string, object>("name", scopedElement), array[0]);

        // Copy to middle.
        nameScope.CopyTo(array, 1);
        Assert.Equal(new KeyValuePair<string, object>("name", scopedElement), array[1]);

        // Copy to end.
        nameScope.CopyTo(array, 2);
        Assert.Equal(new KeyValuePair<string, object>("name", scopedElement), array[1]);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void CopyTo_InvokeEmpty_Nop(int arrayIndex)
    {
        var nameScope = new NameScope();

        // Not null.
        var array = new KeyValuePair<string, object>[1];
        nameScope.CopyTo(array, arrayIndex);
        Assert.Equal(default, array[0]);

        // Null.
        nameScope.CopyTo(null, arrayIndex);
    }

    [Fact]
    public void CopyTo_NullArray_ThrowsNullReferenceException()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope.RegisterName("name", scopedElement);

        Assert.Throws<NullReferenceException>(() => nameScope.CopyTo(null, 0));
    }

    [Fact]
    public void CopyTo_ArrayTooShort_ThrowsIndexOutOfRangeException()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope.RegisterName("name", scopedElement);

        var array = Array.Empty<KeyValuePair<string, object>>();
        Assert.Throws<IndexOutOfRangeException>(() => nameScope.CopyTo(array, 0));
    }

    [Fact]
    public void CopyTo_InvalidIndex_ThrowsIndexOutOfRangeException()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope.RegisterName("name", scopedElement);

        var array = new KeyValuePair<string, object>[1];
        Assert.Throws<IndexOutOfRangeException>(() => nameScope.CopyTo(array, -1));
        Assert.Throws<IndexOutOfRangeException>(() => nameScope.CopyTo(array, 1));
    }

    [Fact]
    public void Contains_Invoke_ReturnsExpected()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope.RegisterName("name", scopedElement);

        Assert.True(nameScope.Contains(new KeyValuePair<string, object>("name", scopedElement)));
        Assert.True(nameScope.Contains(new KeyValuePair<string, object>("name", 1)));
        Assert.True(nameScope.Contains(new KeyValuePair<string, object>("name", new object())));
        Assert.False(nameScope.Contains(new KeyValuePair<string, object>("NAME", scopedElement)));
        Assert.False(nameScope.Contains(new KeyValuePair<string, object>("nAmE", scopedElement)));
        Assert.False(nameScope.Contains(new KeyValuePair<string, object>("name2", scopedElement)));
        Assert.False(nameScope.Contains(new KeyValuePair<string, object>("", scopedElement)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("0name")]
    [InlineData("na\0me")]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("_")]
    [InlineData("na0me")]
    [InlineData("_name")]
    [InlineData("NaMe")]
    public void Contains_InvokeNoSuchNameEmpty_ReturnsFalse(string name)
    {
        var nameScope = new NameScope();
        Assert.False(nameScope.Contains(new KeyValuePair<string, object>(name, new object())));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("0name")]
    [InlineData("na\0me")]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("_")]
    [InlineData("na0me")]
    [InlineData("_name")]
    [InlineData("NaMe")]
    public void Contains_InvokeNoSuchNameNotEmpty_ReturnsFalse(string name)
    {
        var nameScope = new NameScope();
        nameScope.RegisterName("name1", new object());

        Assert.False(nameScope.Contains(new KeyValuePair<string, object>(name, new object())));
    }

    [Fact]
    public void Contains_NullKey_ThrowsArgumentException()
    {
        var nameScope = new NameScope();
        Assert.Throws<ArgumentException>("item", () => nameScope.Contains(new KeyValuePair<string, object>(null!, new object())));
        Assert.Throws<ArgumentException>("item", () => nameScope.Contains(default));
    }

    [Fact]
    public void ContainsKey_Invoke_ReturnsExpected()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope.RegisterName("name", scopedElement);

        Assert.True(nameScope.ContainsKey("name"));
        Assert.True(nameScope.ContainsKey("name"));
        Assert.False(nameScope.ContainsKey("NAME"));
        Assert.False(nameScope.ContainsKey("nAmE"));
        Assert.False(nameScope.ContainsKey("name2"));
        Assert.False(nameScope.ContainsKey(""));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("0name")]
    [InlineData("na\0me")]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("_")]
    [InlineData("na0me")]
    [InlineData("_name")]
    [InlineData("NaMe")]
    public void ContainsKey_InvokeNoSuchNameEmpty_ReturnsFalse(string name)
    {
        var nameScope = new NameScope();
        Assert.False(nameScope.ContainsKey(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("0name")]
    [InlineData("na\0me")]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("_")]
    [InlineData("na0me")]
    [InlineData("_name")]
    [InlineData("NaMe")]
    public void ContainsKey_InvokeNoSuchNameNotEmpty_ReturnsFalse(string name)
    {
        var nameScope = new NameScope();
        nameScope.RegisterName("name1", new object());

        Assert.False(nameScope.ContainsKey(name));
    }

    [Fact]
    public void ContainsKey_NullKey_ThrowsArgumentNullException()
    {
        var nameScope = new NameScope();
        Assert.Throws<ArgumentNullException>("key", () => nameScope.ContainsKey(null!));
    }

    [Fact]
    public void FindName_Invoke_ReturnsExpected()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope.RegisterName("name", scopedElement);
        Assert.Same(scopedElement, nameScope.FindName("name"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("0name")]
    [InlineData("na\0me")]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("_")]
    [InlineData("na0me")]
    [InlineData("_name")]
    [InlineData("NaMe")]
    public void FindName_InvokeNoSuchNameEmpty_ReturnsNull(string? name)
    {
        var nameScope = new NameScope();
        Assert.Null(nameScope.FindName(name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("0name")]
    [InlineData("na\0me")]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("_")]
    [InlineData("na0me")]
    [InlineData("_name")]
    [InlineData("NaMe")]
    public void FindName_InvokeNoSuchNameNotEmpty_ReturnsNull(string? name)
    {
        var nameScope = new NameScope();
        nameScope.RegisterName("name1", new object());

        Assert.Null(nameScope.FindName(name));
    }

    [Fact]
    public void GetEnumerator_InvokeIEnumerableKVPNotEmpty_ReturnsExpected()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope.RegisterName("name", scopedElement);

        IEnumerable<KeyValuePair<string, object>> collection = nameScope;

        using IEnumerator<KeyValuePair<string, object>> enumerator = collection.GetEnumerator();
        for (int i = 0; i < 2; i++)
        {
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            // Move.
            Assert.True(enumerator.MoveNext());
            Assert.Equal("name", enumerator.Current.Key);
            Assert.Same(scopedElement, enumerator.Current.Value);

            // Move end.
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            
            // Move again.
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            // Reset.
            enumerator.Reset();
        }
    }

    [Fact]
    public void GetEnumerator_InvokeIEnumerableKVPEmpty_ReturnsExpected()
    {
        var nameScope = new NameScope();
        IEnumerable<KeyValuePair<string, object>> collection = nameScope;

        using IEnumerator<KeyValuePair<string, object>> enumerator = collection.GetEnumerator();
        for (int i = 0; i < 2; i++)
        {
            Assert.Equal(default, enumerator.Current);

            // Move end.
            Assert.False(enumerator.MoveNext());
            Assert.Equal(default, enumerator.Current);
            
            // Move again.
            Assert.False(enumerator.MoveNext());
            Assert.Equal(default, enumerator.Current);

            // Reset.
            enumerator.Reset();
        }
    }

    [Fact]
    public void GetEnumerator_InvokeIEnumerableNotEmpty_ReturnsExpected()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope.RegisterName("name", scopedElement);

        IEnumerable collection = nameScope;

        IEnumerator enumerator = collection.GetEnumerator();
        for (int i = 0; i < 2; i++)
        {
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            // Move.
            Assert.True(enumerator.MoveNext());
            Assert.Equal("name", ((KeyValuePair<string, object>)enumerator.Current).Key);
            Assert.Same(scopedElement, ((KeyValuePair<string, object>)enumerator.Current).Value);

            // Move end.
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            
            // Move again.
            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            // Reset.
            enumerator.Reset();
        }
    }

    [Fact]
    public void GetEnumerator_InvokeIEnumerableEmpty_ReturnsExpected()
    {
        var nameScope = new NameScope();
        IEnumerable collection = nameScope;

        IEnumerator enumerator = collection.GetEnumerator();
        for (int i = 0; i < 2; i++)
        {
            Assert.Equal(default(KeyValuePair<string, object>), enumerator.Current);

            // Move end.
            Assert.False(enumerator.MoveNext());
            Assert.Equal(default(KeyValuePair<string, object>), enumerator.Current);
            
            // Move again.
            Assert.False(enumerator.MoveNext());
            Assert.Equal(default(KeyValuePair<string, object>), enumerator.Current);

            // Reset.
            enumerator.Reset();
        }
    }

    [Theory]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("_")]
    [InlineData("na0me")]
    [InlineData("_name")]
    [InlineData("NaMe")]
    public void RegisterName_Invoke_Success(string name)
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope.RegisterName(name, scopedElement);
        Assert.Equal(1, nameScope.Count);
        Assert.Same(scopedElement, nameScope.FindName(name));
        Assert.True(nameScope.ContainsKey(name));
        Assert.True(nameScope.Contains(new KeyValuePair<string, object>(name, scopedElement)));
        Assert.Equal(1, nameScope.Keys.Count);
        Assert.NotSame(nameScope.Keys, nameScope.Keys);
        Assert.Equal(name, nameScope.Keys.First());
        Assert.Equal(1, nameScope.Values.Count);
        Assert.NotSame(nameScope.Values, nameScope.Values);
        Assert.Equal(scopedElement, nameScope.Values.First());

        // Set same.
        nameScope.RegisterName(name, scopedElement);
        Assert.Equal(1, nameScope.Count);
        Assert.Same(scopedElement, nameScope.FindName(name));
        Assert.True(nameScope.ContainsKey(name));
        Assert.True(nameScope.Contains(new KeyValuePair<string, object>(name, scopedElement)));
        Assert.Equal(1, nameScope.Keys.Count);
        Assert.NotSame(nameScope.Keys, nameScope.Keys);
        Assert.Equal(name, nameScope.Keys.First());
        Assert.Equal(1, nameScope.Values.Count);
        Assert.NotSame(nameScope.Values, nameScope.Values);
        Assert.Equal(scopedElement, nameScope.Values.First());
    }

    [Fact]
    public void RegisterName_InvokeMultiple_Success()
    {
        var nameScope = new NameScope();
        var scopedElement1 = new object();
        var scopedElement2 = new object();

        nameScope.RegisterName("name1", scopedElement1);
        Assert.Same(scopedElement1, nameScope.FindName("name1"));
        Assert.Equal(1, nameScope.Count);

        nameScope.RegisterName("NAME1", scopedElement2);
        Assert.Same(scopedElement1, nameScope.FindName("name1"));
        Assert.Same(scopedElement2, nameScope.FindName("NAME1"));
        Assert.Equal(2, nameScope.Count);

        nameScope.RegisterName("name2", scopedElement2);
        Assert.Same(scopedElement1, nameScope.FindName("name1"));
        Assert.Same(scopedElement2, nameScope.FindName("NAME1"));
        Assert.Same(scopedElement2, nameScope.FindName("name2"));
        Assert.Equal(3, nameScope.Count);
    }

    [Fact]
    public void RegisterName_NullName_ThrowsArgumentNullException()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        Assert.Throws<ArgumentNullException>("name", () => nameScope.RegisterName(null, scopedElement));
    }

    [Fact]
    public void RegisterName_EmptyName_ThrowsArgumentException()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        Assert.Throws<ArgumentException>(() => nameScope.RegisterName(string.Empty, scopedElement));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0name")]
    [InlineData("na\0me")]
    public void RegisterName_InvalidName_ThrowsArgumentException(string name)
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        Assert.Throws<ArgumentException>(() => nameScope.RegisterName(name, scopedElement));
    }

    [Fact]
    public void RegisterName_NullScopedElement_ThrowsArgumentNullException()
    {
        var nameScope = new NameScope();
        Assert.Throws<ArgumentNullException>("scopedElement", () => nameScope.RegisterName("name", null!));
    }

    [Fact]
    public void RegisterName_Duplicate_ThrowsArgumentException()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope.RegisterName("name", scopedElement);
        Assert.Throws<ArgumentException>(() => nameScope.RegisterName("name", new object()));
    }

    [Fact]
    public void Remove_InvokeString_Success()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope.RegisterName("name", scopedElement);
        nameScope.Remove("name");
        Assert.Null(nameScope.FindName("name"));
        Assert.Equal(0, nameScope.Count);

        // Remove again.
        Assert.False(nameScope.Remove("name"));
        Assert.Null(nameScope.FindName("name"));
        Assert.Equal(0, nameScope.Count);
    }

    [Fact]
    public void Remove_NullKey_ThrowsArgumentNullException()
    {
        var nameScope = new NameScope();
        Assert.Throws<ArgumentNullException>("key", () => nameScope.Remove(null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("0name")]
    [InlineData("na\0me")]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("_")]
    [InlineData("na0me")]
    [InlineData("_name")]
    [InlineData("NaMe")]
    public void Remove_NoSuchKeyEmpty_ReturnsFalse(string name)
    {
        var nameScope = new NameScope();
        Assert.False(nameScope.Remove(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("0name")]
    [InlineData("na\0me")]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("_")]
    [InlineData("na0me")]
    [InlineData("_name")]
    [InlineData("NaMe")]
    public void Remove_NoSuchKeyNotEmpty_ReturnsFalse(string name)
    {
        var nameScope = new NameScope();
        nameScope.RegisterName("name1", new object());

        Assert.False(nameScope.Remove(name));
        Assert.Equal(1, nameScope.Count);
    }

    [Fact]
    public void Remove_InvokeKVPStringObject_Success()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope.RegisterName("name", scopedElement);
        nameScope.Remove(new KeyValuePair<string, object>("name", scopedElement));
        Assert.Null(nameScope.FindName("name"));
        Assert.Equal(0, nameScope.Count);

        // Remove again.
        Assert.False(nameScope.Remove(new KeyValuePair<string, object>("name", scopedElement)));
        Assert.Null(nameScope.FindName("name"));
        Assert.Equal(0, nameScope.Count);
    }

    [Fact]
    public void Remove_NullItemKey_ThrowsArgumentException()
    {
        var nameScope = new NameScope();
        Assert.Throws<ArgumentException>("item", () => nameScope.Remove(new KeyValuePair<string, object>(null!, new object())));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("0name")]
    [InlineData("na\0me")]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("_")]
    [InlineData("na0me")]
    [InlineData("_name")]
    [InlineData("NaMe")]
    public void Remove_NoSuchItemKeyEmpty_ReturnsFalse(string name)
    {
        var nameScope = new NameScope();
        Assert.False(nameScope.Remove(new KeyValuePair<string, object>(name, new object())));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("0name")]
    [InlineData("na\0me")]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("_")]
    [InlineData("na0me")]
    [InlineData("_name")]
    [InlineData("NaMe")]
    public void Remove_NoSuchItemKeyNotEmpty_ReturnsFalse(string name)
    {
        var nameScope = new NameScope();
        nameScope.RegisterName("name1", new object());

        Assert.False(nameScope.Remove(new KeyValuePair<string, object>(name, new object())));
        Assert.Equal(1, nameScope.Count);
    }

    [Fact]
    public void Remove_NoSuchItemValue_ReturnsFalse()
    {
        var nameScope = new NameScope();
        nameScope.RegisterName("name", new object());

        Assert.False(nameScope.Remove(new KeyValuePair<string, object>("name", new object())));
        Assert.False(nameScope.Remove(new KeyValuePair<string, object>("name", null!)));
        Assert.Equal(1, nameScope.Count);
    }

    [Fact]
    public void TryGetValue_Invoke_ReturnsExpected()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope.RegisterName("name", scopedElement);
        
        Assert.True(nameScope.TryGetValue("name", out object value));
        Assert.Same(scopedElement, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("0name")]
    [InlineData("na\0me")]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("_")]
    [InlineData("na0me")]
    [InlineData("_name")]
    [InlineData("NaMe")]
    public void TryGetValue_InvokeNoSuchNameEmpty_ReturnsNull(string key)
    {
        var nameScope = new NameScope();
        Assert.False(nameScope.TryGetValue(key, out object value));
        Assert.Null(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("0name")]
    [InlineData("na\0me")]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("_")]
    [InlineData("na0me")]
    [InlineData("_name")]
    [InlineData("NaMe")]
    public void TryGetValue_InvokeNoSuchNameNotEmpty_ReturnsNull(string key)
    {
        var nameScope = new NameScope();
        nameScope.RegisterName("name1", new object());

        Assert.False(nameScope.TryGetValue(key, out object value));
        Assert.Null(value);
    }

    [Fact]
    public void TryGetValue_NullKey_ThrowsArgumentNullException()
    {
        var nameScope = new NameScope();
        Assert.Throws<ArgumentNullException>("key", () => nameScope.TryGetValue(null, out object value));
    }

    [Fact]
    public void UnregisterName_Invoke_Success()
    {
        var nameScope = new NameScope();
        var scopedElement = new object();
        nameScope.RegisterName("name", scopedElement);
        nameScope.UnregisterName("name");
        Assert.Null(nameScope.FindName("name"));
        Assert.Equal(0, nameScope.Count);

        // Unregister again.
        Assert.Throws<ArgumentException>(() => nameScope.UnregisterName("name"));
        Assert.Null(nameScope.FindName("name"));
        Assert.Equal(0, nameScope.Count);
    }

    [Fact]
    public void UnregisterName_NullName_ThrowsArgumentNullException()
    {
        var nameScope = new NameScope();
        Assert.Throws<ArgumentNullException>("name", () => nameScope.UnregisterName(null));
    }

    [Fact]
    public void UnregisterName_EmptyName_ThrowsArgumentException()
    {
        var nameScope = new NameScope();
        Assert.Throws<ArgumentException>(() => nameScope.UnregisterName(string.Empty));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0name")]
    [InlineData("na\0me")]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("_")]
    [InlineData("na0me")]
    [InlineData("_name")]
    [InlineData("NaMe")]
    public void UnregisterName_NoSuchNameEmpty_ThrowsArgumentException(string name)
    {
        var nameScope = new NameScope();
        Assert.Throws<ArgumentException>(() => nameScope.UnregisterName(name));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0name")]
    [InlineData("na\0me")]
    [InlineData("name")]
    [InlineData("NAME")]
    [InlineData("_")]
    [InlineData("na0me")]
    [InlineData("_name")]
    [InlineData("NaMe")]
    public void UnregisterName_NoSuchNameNotEmpty_ThrowsArgumentException(string name)
    {
        var nameScope = new NameScope();
        nameScope.RegisterName("name1", new object());

        Assert.Throws<ArgumentException>(() => nameScope.UnregisterName(name));
        Assert.Equal(1, nameScope.Count);
    }

    private class CustomNameScope : INameScope
    {
        public object FindName(string name) => throw new NotImplementedException();

        public void RegisterName(string name, object scopedElement) => throw new NotImplementedException();

        public void UnregisterName(string name) => throw new NotImplementedException();
    }
}
