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
using System.Formats.Nrbf;

namespace PresentationCore.Tests.BinaryFormat;

public class ListTests
{
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
        // CodeQL [SM04191] : This is testing a case around latest implementation of Binary Formatter.
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
        SerializationRecord format = list.SerializeAndParse();
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
