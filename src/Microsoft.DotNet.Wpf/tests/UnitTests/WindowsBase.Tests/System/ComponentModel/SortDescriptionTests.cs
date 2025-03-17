// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Tests;

public class SortDescriptionTests
{
    [Fact]
    public void Ctor_Default()
    {
        var description = new SortDescription();
        Assert.Null(description.PropertyName);
        Assert.Equal(ListSortDirection.Ascending, description.Direction);
        Assert.False(description.IsSealed);
    }

    [Theory]
    [InlineData(null, ListSortDirection.Ascending)]
    [InlineData("", ListSortDirection.Ascending)]
    [InlineData("Name", ListSortDirection.Ascending)]
    [InlineData("Name", ListSortDirection.Descending)]
    public void Ctor_String_ListSortDirection(string? propertyName, ListSortDirection direction)
    {
        var description = new SortDescription(propertyName, direction);
        Assert.Equal(direction, description.Direction);
        Assert.Equal(propertyName, description.PropertyName);
        Assert.False(description.IsSealed);
    }

    [Theory]
    [InlineData(ListSortDirection.Ascending - 1)]
    [InlineData(ListSortDirection.Descending + 1)]
    public void Ctor_InvalidDirection_ThrowsInvalidEnumArgumentException(ListSortDirection direction)
    {
        Assert.Throws<InvalidEnumArgumentException>("direction", () => new SortDescription("Name", direction));
    }

    [Theory]
    [InlineData(ListSortDirection.Ascending)]
    [InlineData(ListSortDirection.Descending)]
    public void Direction_Set_GetReturnsExpected(ListSortDirection value)
    {
        var description = new SortDescription
        {
            Direction = value
        };
        Assert.Equal(value, description.Direction);

        // Set same.
        description.Direction = value;
        Assert.Equal(value, description.Direction);
    }

    [Theory]
    [InlineData(ListSortDirection.Ascending - 1)]
    [InlineData(ListSortDirection.Descending + 1)]
    public void Direction_SetInvalid_ThrowsInvalidEnumArgumentException(ListSortDirection value)
    {
        var description = new SortDescription();
        Assert.Throws<InvalidEnumArgumentException>("value", () => description.Direction = value);
    }

    [Theory]
    [InlineData(ListSortDirection.Ascending - 1)]
    [InlineData(ListSortDirection.Ascending)]
    [InlineData(ListSortDirection.Descending)]
    [InlineData(ListSortDirection.Descending + 1)]
    public void Direction_SetSealed_ThrowsInvalidOperationException(ListSortDirection value)
    {
        var collection = new SortDescriptionCollection();
        collection.Add(new SortDescription());

        SortDescription description = collection[0];
        Assert.Throws<InvalidOperationException>(() => description.Direction = value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Name")]
    public void PropertyName_Set_GetReturnsExpected(string? value)
    {
        var description = new SortDescription
        {
            PropertyName = value
        };
        Assert.Equal(value, description.PropertyName);

        // Set same.
        description.PropertyName = value;
        Assert.Equal(value, description.PropertyName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Name")]
    public void PropertyName_SetSealed_ThrowsInvalidOperationException(string? value)
    {
        var collection = new SortDescriptionCollection();
        collection.Add(new SortDescription());

        SortDescription description = collection[0];
        Assert.Throws<InvalidOperationException>(() => description.PropertyName = value);
    }

    public static IEnumerable<object?[]> Equals_TestData()
    {
        yield return new object?[] { new SortDescription("Name", ListSortDirection.Ascending), new SortDescription("Name", ListSortDirection.Ascending), true };
        yield return new object?[] { new SortDescription("Name", ListSortDirection.Ascending), new SortDescription(null, ListSortDirection.Ascending), false };
        yield return new object?[] { new SortDescription("Name", ListSortDirection.Ascending), new SortDescription("", ListSortDirection.Ascending), false };
        yield return new object?[] { new SortDescription("Name", ListSortDirection.Ascending), new SortDescription("Name2", ListSortDirection.Ascending), false };
        yield return new object?[] { new SortDescription("Name", ListSortDirection.Ascending), new SortDescription("Name", ListSortDirection.Descending), false };
        yield return new object?[] { new SortDescription(null, ListSortDirection.Descending), new SortDescription(null, ListSortDirection.Descending), true };
        yield return new object?[] { new SortDescription(null, ListSortDirection.Descending), new SortDescription("", ListSortDirection.Descending), false };
        yield return new object?[] { new SortDescription(null, ListSortDirection.Descending), new SortDescription("Name2", ListSortDirection.Descending), false };
        yield return new object?[] { new SortDescription(null, ListSortDirection.Descending), new SortDescription(null, ListSortDirection.Ascending), false };
        yield return new object?[] { new SortDescription("Name", ListSortDirection.Ascending), new object(), false };
        yield return new object?[] { new SortDescription("Name", ListSortDirection.Ascending), null, false };
    }

    [Theory]
    [MemberData(nameof(Equals_TestData))]
    public void Equals_Object_ReturnsExpected(SortDescription description, object other, bool expected)
    {
        Assert.Equal(expected, description.Equals(other));
        if (other is SortDescription otherDescription)
        {
            Assert.Equal(expected, description.GetHashCode().Equals(otherDescription.GetHashCode()));
            Assert.Equal(expected, description == otherDescription);
            Assert.Equal(!expected, description != otherDescription);
        }
    }
}