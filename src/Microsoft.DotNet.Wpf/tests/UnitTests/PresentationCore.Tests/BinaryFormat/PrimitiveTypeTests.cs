// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Globalization;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;

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

    internal class TestRecord : global::System.Windows.Record
    {
        public static void WritePrimitiveValue(BinaryWriter writer, PrimitiveType type, object value)
            => WritePrimitiveType(writer, type, value);

        public static object ReadPrimitiveValue(BinaryReader reader, PrimitiveType type)
            => type switch
            {
                PrimitiveType.Boolean => reader.ReadBoolean(),
                PrimitiveType.Byte => reader.ReadByte(),
                PrimitiveType.SByte => reader.ReadSByte(),
                PrimitiveType.Char => reader.ReadChar(),
                PrimitiveType.Int16 => reader.ReadInt16(),
                PrimitiveType.UInt16 => reader.ReadUInt16(),
                PrimitiveType.Int32 => reader.ReadInt32(),
                PrimitiveType.UInt32 => reader.ReadUInt32(),
                PrimitiveType.Int64 => reader.ReadInt64(),
                PrimitiveType.UInt64 => reader.ReadUInt64(),
                PrimitiveType.Single => reader.ReadSingle(),
                PrimitiveType.Double => reader.ReadDouble(),
                PrimitiveType.Decimal => decimal.Parse(reader.ReadString(), CultureInfo.InvariantCulture),
                PrimitiveType.DateTime => ReadDateTime(reader),
                PrimitiveType.TimeSpan => new TimeSpan(reader.ReadInt64()),
                // String is handled with a record, never on it's own
                _ => throw new SerializationException($"Failure trying to read primitve '{type}'"),
            };

        /// <summary>
        ///  Reads a binary formatted <see cref="DateTime"/> from the given <paramref name="reader"/>.
        /// </summary>
        /// <exception cref="SerializationException">The data was invalid.</exception>
        private static unsafe DateTime ReadDateTime(BinaryReader reader)
            => CreateDateTimeFromData(reader.ReadInt64());

        /// <summary>
        ///  Creates a <see cref="DateTime"/> object from raw data with validation.
        /// </summary>
        /// <exception cref="SerializationException"><paramref name="data"/> was invalid.</exception>
        private static DateTime CreateDateTimeFromData(long data)
        {
            // Copied from System.Runtime.Serialization.Formatters.Binary.BinaryParser

            // Use DateTime's public constructor to validate the input, but we
            // can't return that result as it strips off the kind. To address
            // that, store the value directly into a DateTime via an unsafe cast.
            // See BinaryFormatterWriter.WriteDateTime for details.

            try
            {
                const long TicksMask = 0x3FFFFFFFFFFFFFFF;
                _ = new DateTime(data & TicksMask);
            }
            catch (ArgumentException ex)
            {
                // Bad data
                throw new SerializationException(ex.Message, ex);
            }

            return Unsafe.As<long, DateTime>(ref data);
        }

        public override void Write(BinaryWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
