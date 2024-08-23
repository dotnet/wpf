// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Globalization;
using System.IO;
using System.Collections.Generic;

namespace System.Windows
{
    /// <summary>
    ///  Base record class.
    /// </summary>
    internal abstract class Record : IRecord
    {
        /// <summary>
        ///  Writes <paramref name="value"/> as <paramref name="primitiveType"/> to the given <paramref name="writer"/>.
        /// </summary>
        private protected static unsafe void WritePrimitiveType(BinaryWriter writer, PrimitiveType primitiveType, object value)
        {
            switch (primitiveType)
            {
                case PrimitiveType.Boolean:
                    writer.Write((bool)value);
                    break;
                case PrimitiveType.Byte:
                    writer.Write((byte)value);
                    break;
                case PrimitiveType.Char:
                    writer.Write((char)value);
                    break;
                case PrimitiveType.Decimal:
                    writer.Write(((decimal)value).ToString(CultureInfo.InvariantCulture));
                    break;
                case PrimitiveType.Double:
                    writer.Write((double)value);
                    break;
                case PrimitiveType.Int16:
                    writer.Write((short)value);
                    break;
                case PrimitiveType.Int32:
                    writer.Write((int)value);
                    break;
                case PrimitiveType.Int64:
                    writer.Write((long)value);
                    break;
                case PrimitiveType.SByte:
                    writer.Write((sbyte)value);
                    break;
                case PrimitiveType.Single:
                    writer.Write((float)value);
                    break;
                case PrimitiveType.TimeSpan:
                    writer.Write(((TimeSpan)value).Ticks);
                    break;
                case PrimitiveType.DateTime:
                    writer.Write((DateTime)value);
                    break;
                case PrimitiveType.UInt16:
                    writer.Write((ushort)value);
                    break;
                case PrimitiveType.UInt32:
                    writer.Write((uint)value);
                    break;
                case PrimitiveType.UInt64:
                    writer.Write((ulong)value);
                    break;
                // String is handled with a record, never on it's own
                case PrimitiveType.Null:
                case PrimitiveType.String:
                default:
                    throw new ArgumentException("Invalid primitive type.", nameof(primitiveType));
            }
        }

        /// <summary>
        ///  Writes <paramref name="values"/> as <paramref name="primitiveType"/> to the given <paramref name="writer"/>.
        /// </summary>
        private protected static void WritePrimitiveTypes(BinaryWriter writer, PrimitiveType primitiveType, IReadOnlyList<object> values)
        {
            for (int i = 0; i < values.Count; i++)
            {
                WritePrimitiveType(writer, primitiveType, values[i]);
            }
        }

        /// <summary>
        ///  Writes records, coalescing null records into single entries.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///  <paramref name="objects"/> contained an object that isn't a record.
        /// </exception>
        private protected static void WriteRecords(BinaryWriter writer, IReadOnlyList<object> objects)
        {
            int nullCount = 0;

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] is not IRecord record)
                {
                    throw new ArgumentException("Invalid record.", nameof(objects));
                }

                // Aggregate consecutive null records.
                if (record is NullRecord)
                {
                    nullCount++;
                    continue;
                }

                if (nullCount > 0)
                {
                    NullRecord.Write(writer, nullCount);
                    nullCount = 0;
                }

                record.Write(writer);
            }

            if (nullCount > 0)
            {
                NullRecord.Write(writer, nullCount);
            }
        }

        /// <summary>
        ///  Writes <paramref name="memberValues"/> as specified by the <paramref name="memberTypeInfo"/>
        /// </summary>
        private protected static void WriteValuesFromMemberTypeInfo(
            BinaryWriter writer,
            MemberTypeInfo memberTypeInfo,
            IReadOnlyList<object> memberValues)
        {
            for (int i = 0; i < memberTypeInfo.Count; i++)
            {
                (BinaryType type, object? info) = memberTypeInfo[i];
                switch (type)
                {
                    case BinaryType.Primitive:
                        WritePrimitiveType(writer, (PrimitiveType)info!, memberValues[i]);
                        break;
                    case BinaryType.String:
                    case BinaryType.Object:
                    case BinaryType.StringArray:
                    case BinaryType.PrimitiveArray:
                    case BinaryType.Class:
                    case BinaryType.SystemClass:
                    case BinaryType.ObjectArray:
                        ((IRecord)memberValues[i]).Write(writer);
                        break;
                    default:
                        throw new ArgumentException("Invalid binary type.", nameof(memberTypeInfo));
                }
            }
        }

        public abstract void Write(BinaryWriter writer);
    }
}

