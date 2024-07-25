// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace PresentationCore.Tests.BinaryFormat;

public class ArrayTests
{
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
