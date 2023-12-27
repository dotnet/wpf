// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Drawing;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Windows;
using FluentAssertions;
using PresentationCore.Tests.TestUtilities;
using PresentationCore.Tests.FluentAssertions;

namespace PresentationCore.Tests.BinaryFormat;

public class ListTests
{
    [Fact]
    public void BinaryFormattedObject_ParseEmptyArrayList()
    {
        BinaryFormattedObject format = new ArrayList().SerializeAndParse();
        SystemClassWithMembersAndTypes systemClass = (SystemClassWithMembersAndTypes)format[1];

        systemClass.Name.Should().Be(typeof(ArrayList).FullName);
        systemClass.MemberNames.Should().BeEquivalentTo(new string[] { "_items", "_size", "_version" });
        systemClass.MemberTypeInfo[0].Should().Be((BinaryType.ObjectArray, null));

        format[2].Should().BeOfType<ArraySingleObject>();
    }

    [Theory]
    [MemberData(nameof(ArrayList_Primitive_Data))]
    public void BinaryFormattedObject_ParsePrimitivesArrayList(object value)
    {
        BinaryFormattedObject format = new ArrayList()
        {
            value
        }.SerializeAndParse();

        SystemClassWithMembersAndTypes systemClass = (SystemClassWithMembersAndTypes)format[1];

        systemClass.Name.Should().Be(typeof(ArrayList).FullName);
        systemClass.MemberNames.Should().BeEquivalentTo(new string[] { "_items", "_size", "_version" });
        systemClass.MemberTypeInfo[0].Should().Be((BinaryType.ObjectArray, null));

        ArraySingleObject array = (ArraySingleObject)format[2];
        MemberPrimitiveTyped primitve = (MemberPrimitiveTyped)array[0];
        primitve.Value.Should().Be(value);
    }

    [Fact]
    public void BinaryFormattedObject_ParseStringArrayList()
    {
        BinaryFormattedObject format = new ArrayList()
        {
            "JarJar"
        }.SerializeAndParse();

        SystemClassWithMembersAndTypes systemClass = (SystemClassWithMembersAndTypes)format[1];

        systemClass.Name.Should().Be(typeof(ArrayList).FullName);
        systemClass.MemberNames.Should().BeEquivalentTo(new string[] { "_items", "_size", "_version" });
        systemClass.MemberTypeInfo[0].Should().Be((BinaryType.ObjectArray, null));

        ArraySingleObject array = (ArraySingleObject)format[2];
        BinaryObjectString binaryString = (BinaryObjectString)array[0];
        binaryString.Value.Should().Be("JarJar");
    }

    public static TheoryData<object> ArrayList_Primitive_Data => new()
    {
        int.MaxValue,
        uint.MaxValue,
        long.MaxValue,
        ulong.MaxValue,
        short.MaxValue,
        ushort.MaxValue,
        byte.MaxValue,
        sbyte.MaxValue,
        true,
        float.MaxValue,
        double.MaxValue,
        char.MaxValue,
        TimeSpan.MaxValue,
        DateTime.MaxValue,
        decimal.MaxValue,
    };

    public static TheoryData<ArrayList> ArrayLists_TestData => new()
    {
        new ArrayList(),
        new ArrayList()
        {
            int.MaxValue,
            uint.MaxValue,
            long.MaxValue,
            ulong.MaxValue,
            short.MaxValue,
            ushort.MaxValue,
            byte.MaxValue,
            sbyte.MaxValue,
            true,
            float.MaxValue,
            double.MaxValue,
            char.MaxValue,
            TimeSpan.MaxValue,
            DateTime.MaxValue,
            decimal.MaxValue,
            "You betcha"
        },
        new ArrayList() { "Same", "old", "same", "old" }
    };

    public static TheoryData<ArrayList> ArrayLists_UnsupportedTestData => new()
    {
        new ArrayList()
        {
            new object(),
        },
        new ArrayList()
        {
            int.MaxValue,
            new System.Drawing.Point()
        }
    };

    [Fact]
    public void BinaryFormattedObject_ParseEmptyIntList()
    {
        BinaryFormattedObject format = new List<int>().SerializeAndParse();
        SystemClassWithMembersAndTypes classInfo = (SystemClassWithMembersAndTypes)format[1];

        // Note that T types are serialized as the mscorlib type.
        classInfo.Name.Should().Be(
            "System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]");

        classInfo.ClassInfo.MemberNames.Should().BeEquivalentTo(new string[]
        {
            "_items",
            // This is something that wouldn't be needed if List<T> implemented ISerializable. If we format
            // we can save any extra unused array spots.
            "_size",
            // It is a bit silly that _version gets serialized, it's only use is as a check to see if
            // the collection is modified while it is being enumerated.
            "_version"
        });
        classInfo.MemberTypeInfo[0].Should().Be((BinaryType.PrimitiveArray, PrimitiveType.Int32));
        classInfo.MemberTypeInfo[1].Should().Be((BinaryType.Primitive, PrimitiveType.Int32));
        classInfo.MemberTypeInfo[2].Should().Be((BinaryType.Primitive, PrimitiveType.Int32));
        classInfo["_items"].Should().BeOfType<MemberReference>();
        classInfo["_size"].Should().Be(0);
        classInfo["_version"].Should().Be(0);

        ArraySinglePrimitive array = (ArraySinglePrimitive)format[2];
        array.Length.Should().Be(0);
    }

    [Fact]
    public void BinaryFormattedObject_ParseEmptyStringList()
    {
        BinaryFormattedObject format = new List<string>().SerializeAndParse();
        SystemClassWithMembersAndTypes classInfo = (SystemClassWithMembersAndTypes)format[1];
        classInfo.ClassInfo.Name.Should().StartWith("System.Collections.Generic.List`1[[System.String,");
        classInfo.MemberTypeInfo[0].Should().Be((BinaryType.StringArray, null));
        classInfo["_items"].Should().BeOfType<MemberReference>();

        ArraySingleString array = (ArraySingleString)format[2];
        array.Length.Should().Be(0);
    }

    [Theory]
    [MemberData(nameof(PrimitiveLists_TestData))]
    public void BinaryFormatWriter_TryWritePrimitiveList(IList list)
    {
        using MemoryStream stream = new();
        BinaryFormatWriter.TryWritePrimitiveList(stream, list).Should().BeTrue();
        stream.Position = 0;

        using var formatterScope = new BinaryFormatterScope(enable: true);
#pragma warning disable SYSLIB0011 // Type or member is obsolete
        BinaryFormatter formatter = new();
        IList deserialized = (IList)formatter.Deserialize(stream);
#pragma warning restore SYSLIB0011

        deserialized.Should().BeEquivalentTo(list);
    }

    [Theory]
    [MemberData(nameof(Lists_UnsupportedTestData))]
    public void BinaryFormatWriter_TryWritePrimitiveList_Unsupported(IList list)
    {
        using MemoryStream stream = new();
        BinaryFormatWriter.TryWritePrimitiveList(stream, list).Should().BeFalse();
        stream.Position.Should().Be(0);
    }

    [Theory]
    [MemberData(nameof(PrimitiveLists_TestData))]
    public void BinaryFormattedObjectExtensions_TryGetPrimitiveList(IList list)
    {
        BinaryFormattedObject format = list.SerializeAndParse();
        format.TryGetPrimitiveList(out object? deserialized).Should().BeTrue();
        deserialized.Should().BeEquivalentTo(list);
    }

    public static TheoryData<IList> PrimitiveLists_TestData => new()
    {
        new List<int>(),
        new List<float>() { 3.14f },
        new List<float>() { float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.NegativeZero },
        new List<int>() { 1, 3, 4, 5, 6, 7 },
        new List<byte>() { 0xDE, 0xAD, 0xBE, 0xEF },
        new List<char>() { 'a', 'b',  'c', 'd', 'e', 'f', 'g', 'h' },
        new List<char>() { 'a', '\0',  'c' },
        new List<string>() { "Believe", "it", "or", "not" },
        new List<decimal>() { 42m },
        new List<DateTime>() { new(2000, 1, 1) },
        new List<TimeSpan>() { new(0, 0, 50) }
    };

    public static TheoryData<IList> Lists_UnsupportedTestData => new()
    {
        new List<object>(),
        new List<nint>(),
        new List<(int, int)>()
    };
}