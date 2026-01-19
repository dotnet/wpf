// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Printing.IndexedProperties;

public class PrintStringPropertyTests
{
    [Fact]
    public void Constructor_Name()
    {
        using PrintStringProperty property = new("TestProperty");
        property.Value.Should().BeNull();
        property.Name.Should().Be("TestProperty");
        bool disposed = property.TestAccessor.Dynamic.IsDisposed;
        disposed.Should().BeFalse();
        bool initialized = property.TestAccessor.Dynamic.IsInitialized;
        initialized.Should().BeFalse();
    }

    [Fact]
    public void Constructor_Name_Value()
    {
        using PrintStringProperty property = new("TestProperty", "TestValue");
        property.Value.Should().Be("TestValue");
        property.Name.Should().Be("TestProperty");
        bool disposed = property.TestAccessor.Dynamic.IsDisposed;
        disposed.Should().BeFalse();
        bool initialized = property.TestAccessor.Dynamic.IsInitialized;
        initialized.Should().BeFalse();
    }

    [Fact]
    public void Constructor_Name_Value_Changed()
    {
        List<string?> changes = [];

        using PrintStringProperty property = new(
            "TestProperty",
            "TestValue",
            (PrintSystemDelegates.StringValueChanged)((string? value) => changes.Add(value)));

        property.Value.Should().Be("TestValue");
        property.Name.Should().Be("TestProperty");
        bool initialized = property.TestAccessor.Dynamic.IsInitialized;
        initialized.Should().BeFalse();

        changes.Should().BeEquivalentTo(["TestValue"]);

        property.Value = new object();
        changes.Should().BeEquivalentTo(["TestValue"]);

        property.Value = "SecondValue";
        changes.Should().BeEquivalentTo(["TestValue", "SecondValue"]);

        property.Value = null;
        changes.Should().BeEquivalentTo(["TestValue", "SecondValue", null]);
    }

    [Fact]
    public void Dispose()
    {
        PrintStringProperty property = new("TestProperty", "TestValue");
        property.Dispose();

        // Name and Value are set to null
        property.Value.Should().BeNull();
        property.Name.Should().BeNull();
        bool disposed = property.TestAccessor.Dynamic.IsDisposed;
        disposed.Should().BeTrue();
        bool initialized = property.TestAccessor.Dynamic.IsInitialized;
        initialized.Should().BeFalse();
    }

    [Fact]
    public void Value_Set()
    {
        using PrintStringProperty property = new("TestProperty");

        // Set to a string
        property.Value = "TestValue";
        property.Value.Should().Be("TestValue");
        property.Name.Should().Be("TestProperty");
        bool initialized = property.TestAccessor.Dynamic.IsInitialized;
        initialized.Should().BeFalse();

        // Set to non-string does nothing
        property.Value = new object();
        property.Value.Should().Be("TestValue");
        initialized = property.TestAccessor.Dynamic.IsInitialized;
        initialized.Should().BeFalse();

        // Set to null
        property.Value = null;
        property.Value.Should().BeNull();
        initialized = property.TestAccessor.Dynamic.IsInitialized;
        initialized.Should().BeFalse();
    }

    [Fact]
    public void Create_Name()
    {
        using var property = PrintStringProperty.Create("TestProperty");

        property.Value.Should().BeNull();
        property.Name.Should().Be("TestProperty");
        bool disposed = property.TestAccessor.Dynamic.IsDisposed;
        disposed.Should().BeFalse();
        bool initialized = property.TestAccessor.Dynamic.IsInitialized;
        initialized.Should().BeFalse();
    }
}
