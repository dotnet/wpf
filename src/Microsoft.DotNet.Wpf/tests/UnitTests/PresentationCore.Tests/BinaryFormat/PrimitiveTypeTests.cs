// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Collections.Generic;
using System.Windows;
using FluentAssertions;
using System.IO;
using PresentationCore.Tests.TestUtilities;

namespace PresentationCore.Tests.BinaryFormat;

public class PrimitiveTypeTests
{
    [Theory]
    [MemberData(nameof(RoundTrip_Data))]
    public void WriteReadPrimitiveValue_RoundTrip(byte type, object value)
    {
        MemoryStream stream = new();
        using (BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true))
        {
            TestRecord.WritePrimitiveValue(writer, (PrimitiveType)type, value);
        }

        stream.Position = 0;

        using BinaryReader reader = new(stream);
        object result = TestRecord.ReadPrimitiveValue(reader, (PrimitiveType)type);
        result.Should().Be(value);
    }

    public static TheoryData<byte, object> RoundTrip_Data => new()
    {
        { (byte)PrimitiveType.Int64, 0L },
        { (byte)PrimitiveType.Int64, -1L },
        { (byte)PrimitiveType.Int64, 1L },
        { (byte)PrimitiveType.Int64, long.MaxValue },
        { (byte)PrimitiveType.Int64, long.MinValue },
        { (byte)PrimitiveType.UInt64, ulong.MaxValue },
        { (byte)PrimitiveType.UInt64, ulong.MinValue },
        { (byte)PrimitiveType.Int32, 0 },
        { (byte)PrimitiveType.Int32, -1 },
        { (byte)PrimitiveType.Int32, 1 },
        { (byte)PrimitiveType.Int32, int.MaxValue },
        { (byte)PrimitiveType.Int32, int.MinValue },
        { (byte)PrimitiveType.UInt32, uint.MaxValue },
        { (byte)PrimitiveType.UInt32, uint.MinValue },
        { (byte)PrimitiveType.Int16, (short)0 },
        { (byte)PrimitiveType.Int16, (short)-1 },
        { (byte)PrimitiveType.Int16, (short)1 },
        { (byte)PrimitiveType.Int16, short.MaxValue },
        { (byte)PrimitiveType.Int16, short.MinValue },
        { (byte)PrimitiveType.UInt16, ushort.MaxValue },
        { (byte)PrimitiveType.UInt16, ushort.MinValue },
        { (byte)PrimitiveType.SByte, (sbyte)0 },
        { (byte)PrimitiveType.SByte, (sbyte)-1 },
        { (byte)PrimitiveType.SByte, (sbyte)1 },
        { (byte)PrimitiveType.SByte, sbyte.MaxValue },
        { (byte)PrimitiveType.SByte, sbyte.MinValue },
        { (byte)PrimitiveType.Byte, byte.MinValue },
        { (byte)PrimitiveType.Byte, byte.MaxValue },
        { (byte)PrimitiveType.Boolean, true },
        { (byte)PrimitiveType.Boolean, false },
        { (byte)PrimitiveType.Single, 0.0f },
        { (byte)PrimitiveType.Single, -1.0f },
        { (byte)PrimitiveType.Single, 1.0f },
        { (byte)PrimitiveType.Single, float.MaxValue },
        { (byte)PrimitiveType.Single, float.MinValue },
        { (byte)PrimitiveType.Single, float.NegativeZero },
        { (byte)PrimitiveType.Single, float.NaN },
        { (byte)PrimitiveType.Single, float.NegativeInfinity },
        { (byte)PrimitiveType.Double, 0.0d },
        { (byte)PrimitiveType.Double, -1.0d },
        { (byte)PrimitiveType.Double, 1.0d },
        { (byte)PrimitiveType.Double, double.MaxValue },
        { (byte)PrimitiveType.Double, double.MinValue },
        { (byte)PrimitiveType.Double, double.NegativeZero },
        { (byte)PrimitiveType.Double, double.NaN },
        { (byte)PrimitiveType.Double, double.NegativeInfinity },
        { (byte)PrimitiveType.TimeSpan, TimeSpan.MinValue },
        { (byte)PrimitiveType.TimeSpan, TimeSpan.MaxValue },
        { (byte)PrimitiveType.DateTime, DateTime.MinValue },
        { (byte)PrimitiveType.DateTime, DateTime.MaxValue },
    };

    [Theory]
    [MemberData(nameof(Primitive_Data))]
    public void PrimitiveTypeMemberName(object value)
    {
        BinaryFormattedObject format = value.SerializeAndParse();
        SystemClassWithMembersAndTypes systemClass = (SystemClassWithMembersAndTypes)format[1];
        systemClass.MemberNames[0].Should().Be("m_value");
        systemClass.MemberValues.Count.Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(Primitive_Data))]
    [MemberData(nameof(Primitive_ExtendedData))]
    public void BinaryFormatWriter_WritePrimitive(object value)
    {
        MemoryStream stream = new();
        BinaryFormatWriter.WritePrimitive(stream, value);
        stream.Position = 0;

        using BinaryFormatterScope formatterScope = new(enable: true);
#pragma warning disable SYSLIB0011 // Type or member is obsolete
        BinaryFormatter formatter = new();
#pragma warning restore SYSLIB0011 // Type or member is obsolete
        object deserialized = formatter.Deserialize(stream);
        deserialized.Should().Be(value);
    }

    [Theory]
    [MemberData(nameof(Primitive_Data))]
    [MemberData(nameof(Primitive_ExtendedData))]
    public void BinaryFormattedObject_ReadPrimitive(object value)
    {
        BinaryFormattedObject formattedObject = value.SerializeAndParse();
        formattedObject.TryGetPrimitiveType(out object? deserialized).Should().BeTrue();
        deserialized.Should().Be(value);
    }

    public static TheoryData<object> Primitive_Data => new()
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
        char.MaxValue
    };

    public static TheoryData<object> Primitive_ExtendedData => new()
    {
        TimeSpan.MaxValue,
        DateTime.MaxValue,
        decimal.MaxValue,
        (nint)1918,
        (nuint)2020,
        "Roundabout"
    };

    internal class TestRecord : System.Windows.Record
    {
        public static void WritePrimitiveValue(BinaryWriter writer, PrimitiveType type, object value)
            => WritePrimitiveType(writer, type, value);

        public static object ReadPrimitiveValue(BinaryReader reader, PrimitiveType type)
            => ReadPrimitiveType(reader, type);

        public override void Write(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}