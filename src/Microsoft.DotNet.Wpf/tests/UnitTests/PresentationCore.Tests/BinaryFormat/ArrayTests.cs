// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace PresentationCore.Tests.BinaryFormat;

public class ArrayTests
{
    public static TheoryData<MemoryStream, int, int> ArrayInfo_ParseSuccessData => new()
     {
         { new MemoryStream(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }), 0, 0 },
         { new MemoryStream(new byte[] { 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 }), 1, 1 },
         { new MemoryStream(new byte[] { 0x02, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 }), 2, 1 },
         { new MemoryStream(new byte[] { 0xFF, 0xFF, 0xFF, 0x7F, 0xFF, 0xFF, 0xFF, 0x7F }), int.MaxValue, int.MaxValue }
     };

    public static TheoryData<MemoryStream, Type> ArrayInfo_ParseNegativeData => new()
     {
         // Not enough data
         { new MemoryStream(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }), typeof(EndOfStreamException) },
         { new MemoryStream(new byte[] { 0x00, 0x00, 0x00 }), typeof(EndOfStreamException) },
         // Negative numbers
         { new MemoryStream(new byte[] { 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF }), typeof(ArgumentOutOfRangeException) }
     };

    public static TheoryData<string?[]> StringArray_Parse_Data => new()
     {
         new string?[] { "one", "two" },
         new string?[] { "yes", "no", null },
         new string?[] { "same", "same", "same" }
     };

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
