// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using PresentationCore.Tests.TestUtilities;
using PresentationCore.Tests.FluentAssertions;

namespace PresentationCore.Tests.BinaryFormat;

public class BinaryFormatWriterTests
{
    [Theory]
    [InlineData("Hello World.")]
    [InlineData("")]
    [InlineData("\0")]
    [InlineData("Embedded\0 Null.")]
    public void BinaryFormatWriter_WriteString(string testString)
    {
        using MemoryStream stream = new();
        BinaryFormatWriter.WriteString(stream, testString);
        stream.Position = 0;

        using var formatterScope = new BinaryFormatterScope(enable: true);
#pragma warning disable SYSLIB0011 // Type or member is obsolete
        BinaryFormatter formatter = new();
#pragma warning restore
        object deserialized = formatter.Deserialize(stream);
        deserialized.Should().Be(testString);
    }

    [Theory]
    [MemberData(nameof(TryWriteObject_SupportedObjects_TestData))]
    public void BinaryFormatWriter_TryWriteObject_SupportedObjects_BinaryFormatterRead(object value)
    {
        using MemoryStream stream = new();
        BinaryFormatWriter.TryWriteFrameworkObject(stream, value).Should().BeTrue();
        stream.Position = 0;

        using BinaryFormatterScope formaterScope = new(enable: true);
#pragma warning disable SYSLIB0011 // Type or member is obsolete
        BinaryFormatter formatter = new();
#pragma warning restore SYSLIB0011 // Type or member is obsolete
        object deserialized = formatter.Deserialize(stream);

        if (value is Hashtable hashtable)
        {
            Hashtable deserializedHashtable = (Hashtable)deserialized;
            deserializedHashtable.Count.Should().Be(hashtable.Count);
            foreach (object? key in hashtable.Keys)
            {
                deserializedHashtable[key].Should().Be(hashtable[key]);
            }
        }
        else if (value is IEnumerable enumerable)
        {
            ((IEnumerable)deserialized).Should().BeEquivalentTo(enumerable);
        }
        else
        {
            deserialized.Should().Be(value);
        }
    }

    [Theory]
    [MemberData(nameof(TryWriteObject_SupportedObjects_TestData))]
    public void BinaryFormatWriter_TryWriteObject_SupportedObjects_RoundTrip(object value)
    {
        using MemoryStream stream = new();
        BinaryFormatWriter.TryWriteFrameworkObject(stream, value).Should().BeTrue();
        stream.Position = 0;

        BinaryFormattedObject format = new(stream);
        format.TryGetFrameworkObject(out object? deserialized).Should().BeTrue();

        if (value is Hashtable hashtable)
        {
            Hashtable deserializedHashtable = (Hashtable)deserialized!;
            deserializedHashtable.Count.Should().Be(hashtable.Count);
            foreach (object? key in hashtable.Keys)
            {
                deserializedHashtable[key].Should().Be(hashtable[key]);
            }
        }
        else if (value is IEnumerable enumerable)
        {
            ((IEnumerable)deserialized!).Should().BeEquivalentTo(enumerable);
        }
        else
        {
            deserialized.Should().Be(value);
        }
    }

    [Theory]
    [MemberData(nameof(TryWriteObject_UnsupportedObjects_TestData))]
    public void BinaryFormatWriter_TryWriteObject_UnsupportedObjects_RoundTrip(object value)
    {
        using MemoryStream stream = new();
        BinaryFormatWriter.TryWriteFrameworkObject(stream, value).Should().BeFalse();
        stream.Position.Should().Be(0);
    }

    public static IEnumerable<object[]?> TryWriteObject_SupportedObjects_TestData =>
        HashtableTests.Hashtables_TestData.Concat(
            ListTests.PrimitiveLists_TestData).Concat(
            ListTests.ArrayLists_TestData).Concat(
            PrimitiveTypeTests.Primitive_Data).Concat(
            SystemDrawingTests.SystemDrawing_TestData).Concat(
            ArrayTests.Array_TestData);

    public static IEnumerable<object[]?> TryWriteObject_UnsupportedObjects_TestData =>
        HashtableTests.Hashtables_UnsupportedTestData.Concat(
            ListTests.Lists_UnsupportedTestData).Concat(
            ListTests.ArrayLists_UnsupportedTestData).Concat(
            ArrayTests.Array_UnsupportedTestData);
}