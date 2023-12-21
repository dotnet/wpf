// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using Xunit;
using FluentAssertions;
using System.Threading;
using System.Windows;
using PresentationCore.Tests.TestUtilities;
using PresentationCore.Tests.FluentAssertions;

namespace PresentationCore.Tests.BinaryFormat;

public class ArrayTests
{
    [Theory]
    [MemberData(nameof(ArrayInfo_ParseSuccessData))]
    public void ArrayInfo_Parse_Success(MemoryStream stream, int expectedId, int expectedLength)
    {
        using BinaryReader reader = new(stream);
        ArrayInfo info = ArrayInfo.Parse(reader, out Count length);
        info.ObjectId.Should().Be(expectedId);
        info.Length.Should().Be(expectedLength);
        length.Should().Be(expectedLength);
    }

    public static TheoryData<Stream, int, int> ArrayInfo_ParseSuccessData => new()
     {
         { new MemoryStream(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }), 0, 0 },
         { new MemoryStream(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 }), 1, 1 },
         { new MemoryStream(new byte[] { 0x02, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 }), 2, 1 },
         { new MemoryStream(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F, 0xFF, 0xFF, 0xFF, 0x7F }), int.MaxValue, int.MaxValue }
     };

    [Theory]
    [MemberData(nameof(ArrayInfo_ParseNegativeData))]
    public void ArrayInfo_Parse_Negative(MemoryStream stream, Type expectedException)
    {
        using BinaryReader reader = new(stream);
        Assert.Throws(expectedException, () => ArrayInfo.Parse(reader, out Count length));
    }

    public static TheoryData<Stream, Type> ArrayInfo_ParseNegativeData => new()
     {
         // Not enough data
         { new MemoryStream(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }), typeof(EndOfStreamException) },
         { new MemoryStream(new byte[] { 0x00, 0x00, 0x00 }), typeof(EndOfStreamException) },
         // Negative numbers
         { new MemoryStream(new byte[] { 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF }), typeof(ArgumentOutOfRangeException) }
     };

    [Theory]
    [MemberData(nameof(StringArray_Parse_Data))]
    public void StringArray_Parse(string?[] strings)
    {
        BinaryFormattedObject format = strings.SerializeAndParse();
        format.RecordCount.Should().BeGreaterThanOrEqualTo(3);
        ArraySingleString array = (ArraySingleString)format[1];
        format.GetStringValues(array, strings.Length).Should().BeEquivalentTo(strings);
    }

    public static TheoryData<string?[]> StringArray_Parse_Data => new()
     {
         new string?[] { "one", "two" },
         new string?[] { "yes", "no", null },
         new string?[] { "same", "same", "same" }
     };

    [Theory]
    [MemberData(nameof(PrimitiveArray_Parse_Data))]
    public void PrimitiveArray_Parse(Array array)
    {
        BinaryFormattedObject format = array.SerializeAndParse();
        format.RecordCount.Should().BeGreaterThanOrEqualTo(3);
        ArraySinglePrimitive arrayRecord = (ArraySinglePrimitive)format[1];
        ((IEnumerable)arrayRecord.ArrayObjects).Should().BeEquivalentTo((IEnumerable)array);
    }

    public static TheoryData<Array> PrimitiveArray_Parse_Data => new()
     {
         new int[] { 1, 2, 3 },
         new int[] { 1, 2, 1 },
         new float[] { 1.0f, float.NaN, float.PositiveInfinity },
         new DateTime[] { DateTime.MaxValue }
     };

    public static IEnumerable<object[]> Array_TestData => StringArray_Parse_Data.Concat(PrimitiveArray_Parse_Data);

    public static TheoryData<Array> Array_UnsupportedTestData => new()
     {
         new System.Drawing.Point[] { new() },
         new object[] { new() },
     };
}
