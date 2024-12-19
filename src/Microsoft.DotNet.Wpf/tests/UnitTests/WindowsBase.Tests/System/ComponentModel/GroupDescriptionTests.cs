// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace System.ComponentModel.Tests;

public class GroupDescriptionTests
{
    [Fact]
    public void Ctor_Default()
    {
        var description = new SubGroupDescription();
        Assert.Null(description.CustomSort);
        Assert.Empty(description.GroupNames);
        Assert.Same(description.GroupNames, description.GroupNames);
        Assert.Empty(description.SortDescriptions);
        Assert.Same(description.SortDescriptions, description.SortDescriptions);
    }

    public static IEnumerable<object?[]> CustomSort_Set_TestData()
    {
        yield return new object?[] { null };
        yield return new object?[] { StringComparer.CurrentCulture };
    }

    [Theory]
    [MemberData(nameof(CustomSort_Set_TestData))]
    public void CustomSort_Set_GetReturnsExpected(IComparer? value)
    {
        var description = new SubGroupDescription
        {
            CustomSort = value
        };
        Assert.Same(value, description.CustomSort);
        Assert.Empty(description.SortDescriptions);

        // Set same.
        description.CustomSort = value;
        Assert.Same(value, description.CustomSort);
        Assert.Empty(description.SortDescriptions);
    }

    [Theory]
    [MemberData(nameof(CustomSort_Set_TestData))]
    public void CustomSort_SetNonNullOldValue_GetReturnsExpected(IComparer? value)
    {
        var description = new SubGroupDescription
        {
            CustomSort = StringComparer.Ordinal
        };

        description.CustomSort = value;
        Assert.Same(value, description.CustomSort);
        Assert.Empty(description.SortDescriptions);

        // Set same.
        description.CustomSort = value;
        Assert.Same(value, description.CustomSort);
        Assert.Empty(description.SortDescriptions);
    }

    [Theory]
    [MemberData(nameof(CustomSort_Set_TestData))]
    public void CustomSort_SetWithSortDescriptions_GetReturnsExpected(IComparer? value)
    {
        var description = new SubGroupDescription();
        description.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

        // Set.
        description.CustomSort = value;
        Assert.Same(value, description.CustomSort);
        Assert.Empty(description.SortDescriptions);

        // Set same.
        description.CustomSort = value;
        Assert.Same(value, description.CustomSort);
        Assert.Empty(description.SortDescriptions);
    }

    [Theory]
    [MemberData(nameof(CustomSort_Set_TestData))]
    public void CustomSort_SetWithEmptySortDescriptions_GetReturnsExpected(IComparer? value)
    {
        var description = new SubGroupDescription();
        Assert.Empty(description.SortDescriptions);

        // Set.
        description.CustomSort = value;
        Assert.Same(value, description.CustomSort);
        Assert.Empty(description.SortDescriptions);

        // Set same.
        description.CustomSort = value;
        Assert.Same(value, description.CustomSort);
        Assert.Empty(description.SortDescriptions);
    }

    [Fact]
    public void CustomSort_SetWithHandler_CallsPropertyChanged()
    {
        var description = new SubGroupDescription();
        int callCount = 0;
        PropertyChangedEventHandler handler = (sender, e) =>
        {
            Assert.Same(description, sender);
            Assert.Equal("CustomSort", e.PropertyName);
            callCount++;
        }; ;
        ((INotifyPropertyChanged)description).PropertyChanged += handler;

        // Set.
        description.CustomSort = StringComparer.CurrentCulture;
        Assert.Equal(StringComparer.CurrentCulture, description.CustomSort);
        Assert.Equal(1, callCount);

        // Set same.
        description.CustomSort = StringComparer.CurrentCulture;
        Assert.Equal(StringComparer.CurrentCulture, description.CustomSort);
        Assert.Equal(2, callCount);

        // Remove handler.
        ((INotifyPropertyChanged)description).PropertyChanged -= handler;
        description.CustomSort = null;
        Assert.Null(description.CustomSort);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void CustomSort_SetWithSortDescriptionsWithHandler_CallsPropertyChanged()
    {
        var description = new SubGroupDescription();
        description.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

        int callCount = 0;
        var properties = new List<string>();
        PropertyChangedEventHandler handler = (sender, e) =>
        {
            Assert.Same(description, sender);
            properties.Add(e.PropertyName!);
            callCount++;
        }; ;
        ((INotifyPropertyChanged)description).PropertyChanged += handler;

        // Set.
        description.CustomSort = StringComparer.CurrentCulture;
        Assert.Equal(StringComparer.CurrentCulture, description.CustomSort);
        Assert.Equal(2, callCount);
        Assert.Equal(new[] { "SortDescriptions", "CustomSort" }, properties);

        // Set same.
        description.CustomSort = StringComparer.CurrentCulture;
        Assert.Equal(StringComparer.CurrentCulture, description.CustomSort);
        Assert.Equal(3, callCount);
        Assert.Equal(new[] { "SortDescriptions", "CustomSort", "CustomSort" }, properties);

        // Remove handler.
        ((INotifyPropertyChanged)description).PropertyChanged -= handler;
        description.CustomSort = null;
        Assert.Null(description.CustomSort);
        Assert.Equal(3, callCount);
        Assert.Equal(new[] { "SortDescriptions", "CustomSort", "CustomSort" }, properties);
    }

    [Fact]
    public void CustomSort_SetWithEmptySortDescriptionsWithHandler_CallsPropertyChanged()
    {
        var description = new SubGroupDescription();
        Assert.Empty(description.SortDescriptions);

        int callCount = 0;
        var properties = new List<string>();
        PropertyChangedEventHandler handler = (sender, e) =>
        {
            Assert.Same(description, sender);
            properties.Add(e.PropertyName!);
            callCount++;
        }; ;
        ((INotifyPropertyChanged)description).PropertyChanged += handler;

        // Set.
        description.CustomSort = StringComparer.CurrentCulture;
        Assert.Equal(StringComparer.CurrentCulture, description.CustomSort);
        Assert.Equal(2, callCount);
        Assert.Equal(new[] { "SortDescriptions", "CustomSort" }, properties);

        // Set same.
        description.CustomSort = StringComparer.CurrentCulture;
        Assert.Equal(StringComparer.CurrentCulture, description.CustomSort);
        Assert.Equal(3, callCount);
        Assert.Equal(new[] { "SortDescriptions", "CustomSort", "CustomSort" }, properties);

        // Remove handler.
        ((INotifyPropertyChanged)description).PropertyChanged -= handler;
        description.CustomSort = null;
        Assert.Null(description.CustomSort);
        Assert.Equal(3, callCount);
        Assert.Equal(new[] { "SortDescriptions", "CustomSort", "CustomSort" }, properties);
    }

    [Fact]
    public void CustomSort_GetFirstTime_DoesNotCallPropertyChanged()
    {
        var description = new SubGroupDescription();
        int callCount = 0;
        ((INotifyPropertyChanged)description).PropertyChanged += (sender, e) => callCount++;

        // Get.
        Assert.Null(description.CustomSort);
        Assert.Equal(0, callCount);

        // Get again.
        Assert.Null(description.CustomSort);
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void GroupNames_GetFirstTime_DoesNotCallPropertyChanged()
    {
        var description = new SubGroupDescription();
        int callCount = 0;
        ((INotifyPropertyChanged)description).PropertyChanged += (sender, e) => callCount++;

        // Get.
        ObservableCollection<object> collection = description.GroupNames;
        Assert.Empty(collection);
        Assert.Same(collection, description.GroupNames);
        Assert.Equal(0, callCount);

        // Get again.
        Assert.Empty(description.GroupNames);
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void GroupNames_Change_CallsPropertyChanged()
    {
        var description = new SubGroupDescription();
        int callCount = 0;
        PropertyChangedEventHandler handler = (sender, e) =>
        {
            Assert.Same(description, sender);
            Assert.Equal("GroupNames", e.PropertyName);
            callCount++;
        };
        ((INotifyPropertyChanged)description).PropertyChanged += handler;

        // Clear.
        description.GroupNames.Clear();
        Assert.Empty(description.GroupNames);
        Assert.Equal(1, callCount);

        // Clear again.
        description.GroupNames.Clear();
        Assert.Empty(description.GroupNames);
        Assert.Equal(2, callCount);

        // Remove handler.
        ((INotifyPropertyChanged)description).PropertyChanged -= handler;
        description.GroupNames.Clear();
        Assert.Empty(description.GroupNames);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void GroupNames_ShouldSerialize_ReturnsExpected()
    {
        var description = new SubGroupDescription();
        PropertyDescriptor property = TypeDescriptor.GetProperties(typeof(GroupDescription))[nameof(GroupDescription.GroupNames)]!;
        Assert.False(property.ShouldSerializeValue(description));

        Assert.Empty(description.GroupNames);
        Assert.False(property.ShouldSerializeValue(description));

        description.GroupNames.Add("Name");
        Assert.True(property.ShouldSerializeValue(description));

        description.GroupNames.Clear();
        Assert.False(property.ShouldSerializeValue(description));
    }

    [Fact]
    public void SortDescriptions_GetFirstTime_CallsPropertyChanged()
    {
        var description = new SubGroupDescription();
        int callCount = 0;
        ((INotifyPropertyChanged)description).PropertyChanged += (sender, e) =>
        {
            Assert.Same(description, sender);
            Assert.Equal("SortDescriptions", e.PropertyName);
            callCount++;
        };

        // Get.
        SortDescriptionCollection collection = description.SortDescriptions;
        Assert.Empty(collection);
        Assert.Same(collection, description.SortDescriptions);
        Assert.Equal(1, callCount);

        // Get again.
        Assert.Empty(description.SortDescriptions);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void SortDescriptions_GetFirstTimeWithCustomSort_CallsPropertyChanged()
    {
        var description = new SubGroupDescription
        {
            CustomSort = StringComparer.CurrentCulture
        };
        Assert.Equal(StringComparer.CurrentCulture, description.CustomSort);

        int callCount = 0;
        ((INotifyPropertyChanged)description).PropertyChanged += (sender, e) =>
        {
            Assert.Same(description, sender);
            Assert.Equal("SortDescriptions", e.PropertyName);
            callCount++;
        };

        // Get.
        SortDescriptionCollection collection = description.SortDescriptions;
        Assert.Empty(collection);
        Assert.Same(collection, description.SortDescriptions);
        Assert.Equal(1, callCount);

        // Get again.
        Assert.Empty(description.SortDescriptions);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void SortDescriptions_GetRemovedHandler_DoesNotCallPropertyChanged()
    {
        var description = new SubGroupDescription();
        int callCount = 0;
        PropertyChangedEventHandler handler = (sender, e) => callCount++;
        ((INotifyPropertyChanged)description).PropertyChanged += handler;
        ((INotifyPropertyChanged)description).PropertyChanged -= handler;

        // Get.
        SortDescriptionCollection collection = description.SortDescriptions;
        Assert.Empty(collection);
        Assert.Same(collection, description.SortDescriptions);
        Assert.Equal(0, callCount);

        // Get again.
        Assert.Empty(description.SortDescriptions);
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void SortDescriptions_ChangeCount_ClearsCustomSort()
    {
        var description = new SubGroupDescription();
        var description1 = new SortDescription("Name1", ListSortDirection.Ascending);
        _ = new SortDescription("Name2", ListSortDirection.Ascending);

        description.CustomSort = StringComparer.CurrentCulture;
        Assert.Equal(StringComparer.CurrentCulture, description.CustomSort);

        // Add.
        description.SortDescriptions.Add(description1);
        Assert.Equal(new[] { description1 }, description.SortDescriptions.Cast<SortDescription>());
        Assert.Null(description.CustomSort);
    }

    [Fact]
    public void SortDescriptions_ChangeNoCount_DoesNotClearCustomSort()
    {
        var description = new SubGroupDescription();
        _ = new SortDescription("Name1", ListSortDirection.Ascending);
        _ = new SortDescription("Name2", ListSortDirection.Ascending);

        description.CustomSort = StringComparer.CurrentCulture;
        Assert.Equal(StringComparer.CurrentCulture, description.CustomSort);

        // Clear.
        description.SortDescriptions.Clear();
        Assert.Empty(description.SortDescriptions);
        Assert.Equal(StringComparer.CurrentCulture, description.CustomSort);
    }

    [Fact]
    public void SortDescriptions_Change_CallsPropertyChanged()
    {
        var description = new SubGroupDescription();
        var description1 = new SortDescription("Name1", ListSortDirection.Ascending);
        var description2 = new SortDescription("Name2", ListSortDirection.Ascending);
        Assert.Empty(description.SortDescriptions);

        int callCount = 0;
        PropertyChangedEventHandler handler = (sender, e) =>
        {
            Assert.Same(description, sender);
            Assert.Equal("SortDescriptions", e.PropertyName);
            callCount++;
        };
        ((INotifyPropertyChanged)description).PropertyChanged += handler;

        // Add.
        description.SortDescriptions.Add(description1);
        Assert.Equal(new[] { description1 }, description.SortDescriptions.Cast<SortDescription>());
        Assert.Null(description.CustomSort);
        Assert.Equal(1, callCount);

        // Clear.
        description.SortDescriptions.Clear();
        Assert.Empty(description.SortDescriptions);
        Assert.Null(description.CustomSort);
        Assert.Equal(2, callCount);

        // Remove handler.
        ((INotifyPropertyChanged)description).PropertyChanged -= handler;
        description.SortDescriptions.Add(description2);
        Assert.Equal(new[] { description2 }, description.SortDescriptions.Cast<SortDescription>());
        Assert.Null(description.CustomSort);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public void SortDescriptions_ChangeWithCustomSort_CallsPropertyChanged()
    {
        var description = new SubGroupDescription();
        var description1 = new SortDescription("Name1", ListSortDirection.Ascending);
        var description2 = new SortDescription("Name2", ListSortDirection.Ascending);
        description.CustomSort = StringComparer.CurrentCulture;
        Assert.Empty(description.SortDescriptions);

        int callCount = 0;
        var properties = new List<string>();
        PropertyChangedEventHandler handler = (sender, e) =>
        {
            Assert.Same(description, sender);
            properties.Add(e.PropertyName!);
            callCount++;
        };
        ((INotifyPropertyChanged)description).PropertyChanged += handler;

        // Add.
        description.SortDescriptions.Add(description1);
        Assert.Equal(new[] { description1 }, description.SortDescriptions.Cast<SortDescription>());
        Assert.Null(description.CustomSort);
        Assert.Equal(2, callCount);
        Assert.Equal(new[] { "CustomSort", "SortDescriptions" }, properties);

        // Clear.
        description.SortDescriptions.Clear();
        Assert.Empty(description.SortDescriptions);
        Assert.Null(description.CustomSort);
        Assert.Equal(3, callCount);
        Assert.Equal(new[] { "CustomSort", "SortDescriptions", "SortDescriptions" }, properties);

        // Remove handler.
        ((INotifyPropertyChanged)description).PropertyChanged -= handler;
        description.SortDescriptions.Add(description2);
        Assert.Equal(new[] { description2 }, description.SortDescriptions.Cast<SortDescription>());
        Assert.Null(description.CustomSort);
        Assert.Equal(3, callCount);
        Assert.Equal(new[] { "CustomSort", "SortDescriptions", "SortDescriptions" }, properties);
    }

    [Fact]
    public void SortDescriptions_ShouldSerialize_ReturnsExpected()
    {
        var description = new SubGroupDescription();
        PropertyDescriptor property = TypeDescriptor.GetProperties(typeof(GroupDescription))[nameof(GroupDescription.SortDescriptions)]!;
        Assert.False(property.ShouldSerializeValue(description));

        Assert.Empty(description.SortDescriptions);
        Assert.False(property.ShouldSerializeValue(description));

        description.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        Assert.True(property.ShouldSerializeValue(description));

        description.SortDescriptions.Clear();
        Assert.False(property.ShouldSerializeValue(description));
    }

    [Fact]
    public void PropertyChanged_INotifyPropertyChangedAddRemove_Success()
    {
        INotifyPropertyChanged description = new SubGroupDescription();

        int callCount = 0;
        PropertyChangedEventHandler handler = (s, e) => callCount++;
        ((INotifyPropertyChanged)description).PropertyChanged += handler;
        Assert.Equal(0, callCount);

        ((INotifyPropertyChanged)description).PropertyChanged -= handler;
        Assert.Equal(0, callCount);

        // Remove non existent.
        ((INotifyPropertyChanged)description).PropertyChanged -= handler;
        Assert.Equal(0, callCount);

        // Add null.
        ((INotifyPropertyChanged)description).PropertyChanged += null;
        Assert.Equal(0, callCount);

        // Remove null.
        ((INotifyPropertyChanged)description).PropertyChanged -= null;
        Assert.Equal(0, callCount);
    }

    public static IEnumerable<object?[]> NamesMatch_TestData()
    {
        yield return new object?[] { null, null, true };

        var obj = new object();
        yield return new object?[] { obj, obj, true };
        yield return new object?[] { obj, new object(), false };
        yield return new object?[] { obj, null, false };
        yield return new object?[] { null, null, true };
        yield return new object?[] { null, new object(), false };
    }

    [Theory]
    [MemberData(nameof(NamesMatch_TestData))]
    public void NamesMatch_Invoke_ReturnsExpected(object? groupName1, object? groupName2, bool expected)
    {
        var description = new SubGroupDescription();
        Assert.Equal(expected, description.NamesMatch(groupName1, groupName2));
    }

    public static IEnumerable<object?[]> OnPropertyChanged_TestData()
    {
        yield return new object?[] { new PropertyChangedEventArgs("Name") };
        yield return new object?[] { null };
    }

    [Theory]
    [MemberData(nameof(OnPropertyChanged_TestData))]
    public void OnPropertyChanged_Invoke_CallsPropertyChangedEvent(PropertyChangedEventArgs eventArgs)
    {
        var description = new SubGroupDescription();
        int callCount = 0;
        PropertyChangedEventHandler handler = (sender, e) =>
        {
            Assert.Same(description, sender);
            Assert.Same(eventArgs, e);
            callCount++;
        };

        // Call with handler.
        ((INotifyPropertyChanged)description).PropertyChanged += handler;
        description.OnPropertyChanged(eventArgs);
        Assert.Equal(1, callCount);

        // Remove handler.
        ((INotifyPropertyChanged)description).PropertyChanged -= handler;
        description.OnPropertyChanged(eventArgs);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void ShouldSerializeGroupNames_Invoke_ReturnsExpected()
    {
        var description = new SubGroupDescription();
        _ = TypeDescriptor.GetProperties(typeof(GroupDescription))[nameof(GroupDescription.GroupNames)]!;
        Assert.False(description.ShouldSerializeGroupNames());

        Assert.Empty(description.GroupNames);
        Assert.False(description.ShouldSerializeGroupNames());

        description.GroupNames.Add("Name");
        Assert.True(description.ShouldSerializeGroupNames());

        description.GroupNames.Clear();
        Assert.False(description.ShouldSerializeGroupNames());
    }

    [Fact]
    public void ShouldSerializeGroupNames_Invoke_DoesNotCallPropertyChanged()
    {
        var description = new SubGroupDescription();
        int callCount = 0;
        ((INotifyPropertyChanged)description).PropertyChanged += (sender, e) => callCount++;

        Assert.False(description.ShouldSerializeGroupNames());
        Assert.Equal(0, callCount);
    }

    [Fact]
    public void ShouldSerializeSortDescriptions_Invoke_ReturnsExpected()
    {
        var description = new SubGroupDescription();
        Assert.False(description.ShouldSerializeSortDescriptions());

        Assert.Empty(description.SortDescriptions);
        Assert.False(description.ShouldSerializeSortDescriptions());

        description.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        Assert.True(description.ShouldSerializeSortDescriptions());

        description.SortDescriptions.Clear();
        Assert.False(description.ShouldSerializeSortDescriptions());
    }

    [Fact]
    public void ShouldSerializeSortDescriptions_Invoke_DoesNotCallPropertyChanged()
    {
        var description = new SubGroupDescription();
        int callCount = 0;
        ((INotifyPropertyChanged)description).PropertyChanged += (sender, e) => callCount++;

        Assert.False(description.ShouldSerializeSortDescriptions());
        Assert.Equal(0, callCount);
    }

    private class SubGroupDescription : GroupDescription
    {
        public SubGroupDescription() : base()
        {
        }

        public override object GroupNameFromItem(object item, int level, CultureInfo culture)
            => throw new NotImplementedException();

        public new void OnPropertyChanged(PropertyChangedEventArgs e) => base.OnPropertyChanged(e);
    }
}
