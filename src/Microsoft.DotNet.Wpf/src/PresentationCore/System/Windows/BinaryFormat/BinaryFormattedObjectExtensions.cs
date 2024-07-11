// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Collections;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#nullable enable
namespace System.Windows
{
    internal static class BinaryFormattedObjectExtensions
    {
        /// <summary>
        ///  Type names for <see cref="SystemClassWithMembersAndTypes"/> that are raw primitives.
        /// </summary>
        private static bool IsPrimitiveTypeClassName(ReadOnlySpan<char> typeName)
            => TypeInfo.GetPrimitiveType(typeName) switch
            {
                PrimitiveType.Boolean => true,
                PrimitiveType.Byte => true,
                PrimitiveType.Char => true,
                PrimitiveType.Double => true,
                PrimitiveType.Int32 => true,
                PrimitiveType.Int64 => true,
                PrimitiveType.SByte => true,
                PrimitiveType.Single => true,
                PrimitiveType.Int16 => true,
                PrimitiveType.UInt16 => true,
                PrimitiveType.UInt32 => true,
                PrimitiveType.UInt64 => true,
                _ => false,
            };

        private delegate bool TryGetDelegate(BinaryFormattedObject format, [NotNullWhen(true)] out object? value);

        private static bool TryGet(TryGetDelegate get, BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
        {
            try
            {
                return get(format, out value);
            }
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidCastException)
            {
                // This should only really happen with corrupted data.
                Debug.Fail(ex.Message);
                value = default;
                return false;
            }
        }

        /// <summary>
        ///  Tries to get this object as a <see cref="PointF"/>.
        /// </summary>
        public static bool TryGetPointF(this BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
        {
            return TryGet(Get, format, out value);

            static bool Get(BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
            {
                value = default;

                if (format.RecordCount < 4
                    || format[1] is not BinaryLibrary binaryLibrary
                    || binaryLibrary.LibraryName != TypeInfo.SystemDrawingAssemblyName
                    || format[2] is not ClassWithMembersAndTypes classInfo
                    || classInfo.Name != typeof(PointF).FullName
                    || classInfo.MemberValues.Count != 2)
                {
                    return false;
                }

                value = new PointF((float)classInfo["x"], (float)classInfo["y"]);

                return true;
            }
        }

        /// <summary>
        ///  Tries to get this object as a <see cref="RectangleF"/>.
        /// </summary>
        public static bool TryGetRectangleF(this BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
        {
            return TryGet(Get, format, out value);

            static bool Get(BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
            {
                value = default;

                if (format.RecordCount < 4
                    || format[1] is not BinaryLibrary binaryLibrary
                    || binaryLibrary.LibraryName != TypeInfo.SystemDrawingAssemblyName
                    || format[2] is not ClassWithMembersAndTypes classInfo
                    || classInfo.Name != typeof(RectangleF).FullName
                    || classInfo.MemberValues.Count != 4)
                {
                    return false;
                }

                value = new RectangleF(
                (float)classInfo["x"],
                (float)classInfo["y"],
                (float)classInfo["width"],
                (float)classInfo["height"]);

                return true;
            }
        }

        /// <summary>
        ///  Trys to get this object as a primitive type or string.
        /// </summary>
        /// <returns><see langword="true"/> if this represented a primitive type or string.</returns>
        public static bool TryGetPrimitiveType(this BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
        {
            return TryGet(Get, format, out value);

            static bool Get(BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
            {
                value = default;
                if (format.RecordCount < 3)
                {
                    return false;
                }

                if (format[1] is BinaryObjectString binaryString)
                {
                    value = binaryString.Value;
                    return true;
                }

                if (format[1] is not SystemClassWithMembersAndTypes systemClass)
                {
                    return false;
                }

                if (IsPrimitiveTypeClassName(systemClass.Name) && systemClass.MemberTypeInfo[0].Type == BinaryType.Primitive)
                {
                    value = systemClass.MemberValues[0];
                    return true;
                }

                if (systemClass.Name == typeof(TimeSpan).FullName)
                {
                    value = new TimeSpan((long)systemClass.MemberValues[0]);
                    return true;
                }

                switch (systemClass.Name)
                {
                    case TypeInfo.TimeSpanType:
                        value = new TimeSpan((long)systemClass.MemberValues[0]);
                        return true;
                    case TypeInfo.DateTimeType:
                        ulong ulongValue = (ulong)systemClass["dateData"];
                        value = Unsafe.As<ulong, DateTime>(ref ulongValue);
                        return true;
                    case TypeInfo.DecimalType:
                        Span<int> bits = stackalloc int[4]
                        {
                            (int)systemClass["lo"],
                            (int)systemClass["mid"],
                            (int)systemClass["hi"],
                            (int)systemClass["flags"]
                        };

                        value = new decimal(bits);
                        return true;
                    case TypeInfo.IntPtrType:
                        // Rehydrating still throws even though casting doesn't any more
                        value = checked((nint)(long)systemClass.MemberValues[0]);
                        return true;
                    case TypeInfo.UIntPtrType:
                        value = checked((nuint)(ulong)systemClass.MemberValues[0]);
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        ///  Trys to get this object as a <see cref="List{T}"/> of <see cref="PrimitiveType"/>.
        /// </summary>
        public static bool TryGetPrimitiveList(this BinaryFormattedObject format, [NotNullWhen(true)] out object? list)
        {
            return TryGet(Get, format, out list);

            static bool Get(BinaryFormattedObject format, [NotNullWhen(true)] out object? list)
            {
                list = null;

                const string ListTypeName = "System.Collections.Generic.List`1[[";

                if (format.RecordCount != 4
                    || format[1] is not SystemClassWithMembersAndTypes classInfo
                    || !classInfo.Name.StartsWith(ListTypeName, StringComparison.Ordinal)
                    || format[2] is not ArrayRecord array)
                {
                    return false;
                }

                int commaIndex = classInfo.Name.IndexOf(',');
                if (commaIndex == -1)
                {
                    return false;
                }

                ReadOnlySpan<char> typeName = classInfo.Name.AsSpan()[ListTypeName.Length..commaIndex];
                PrimitiveType primitiveType = TypeInfo.GetPrimitiveType(typeName);

                int size;
                try
                {
                    // Lists serialize the entire backing array.
                    if ((size = (int)classInfo["_size"]) > array.Length)
                    {
                        return false;
                    }
                }
                catch (KeyNotFoundException)
                {
                    return false;
                }

                switch (primitiveType)
                {
                    case default(PrimitiveType):
                        return false;
                    case PrimitiveType.String:
                        if (array is ArraySingleString stringArray)
                        {
                            List<string> stringList = new(size);
                            stringList.AddRange((IEnumerable<string>)format.GetStringValues(stringArray, size));
                            list = stringList;
                            return true;
                        }

                        return false;
                }

                if (array is not ArraySinglePrimitive primitiveArray || primitiveArray.PrimitiveType != primitiveType)
                {
                    return false;
                }

                IList primitiveList = primitiveType switch
                {
                    PrimitiveType.Boolean => new List<bool>(size),
                    PrimitiveType.Byte => new List<byte>(size),
                    PrimitiveType.Char => new List<char>(size),
                    PrimitiveType.Decimal => new List<decimal>(size),
                    PrimitiveType.Double => new List<double>(size),
                    PrimitiveType.Int16 => new List<short>(size),
                    PrimitiveType.Int32 => new List<int>(size),
                    PrimitiveType.Int64 => new List<long>(size),
                    PrimitiveType.SByte => new List<sbyte>(size),
                    PrimitiveType.Single => new List<float>(size),
                    PrimitiveType.TimeSpan => new List<TimeSpan>(size),
                    PrimitiveType.DateTime => new List<DateTime>(size),
                    PrimitiveType.UInt16 => new List<ushort>(size),
                    PrimitiveType.UInt32 => new List<uint>(size),
                    PrimitiveType.UInt64 => new List<ulong>(size),
                    _ => throw new InvalidOperationException()
                };

                foreach (object item in array.Take(size))
                {
                    primitiveList.Add(item);
                }

                list = primitiveList;
                return true;
            }
        }

        /// <summary>
        ///  Tries to get this object as a <see cref="ArrayList"/> of <see cref="PrimitiveType"/> values.
        /// </summary>
        public static bool TryGetPrimitiveArrayList(this BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
        {
            return TryGet(Get, format, out value);

            static bool Get(BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
            {
                value = null;

                if (format.RecordCount != 4
                    || format[1] is not SystemClassWithMembersAndTypes classInfo
                    || classInfo.Name != typeof(ArrayList).FullName
                    || format[2] is not ArraySingleObject array)
                {
                    return false;
                }

                int size;
                try
                {
                    // Lists serialize the entire backing array.
                    if ((size = (int)classInfo["_size"]) > array.Length)
                    {
                        return false;
                    }
                }
                catch (KeyNotFoundException)
                {
                    return false;
                }

                ArrayList arrayList = new(size);
                for (int i = 0; i < size; i++)
                {
                    if (!format.TryGetPrimitiveRecordValueOrNull((IRecord)array[i], out object? item))
                    {
                        return false;
                    }

                    arrayList.Add(item);
                }

                value = arrayList;
                return true;
            }
        }

        /// <summary>
        ///  Tries to get this object as an <see cref="Array"/> of primitive types.
        /// </summary>
        public static bool TryGetPrimitiveArray(this BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
        {
            return TryGet(Get, format, out value);

            static bool Get(BinaryFormattedObject format, [NotNullWhen(true)] out object? value)
            {
                value = null;
                if (format.RecordCount != 3)
                {
                    return false;
                }

                if (format[1] is ArraySingleString stringArray)
                {
                    value = format.GetStringValues(stringArray, stringArray.Length).ToArray();
                    return true;
                }

                if (format[1] is not ArraySinglePrimitive primitiveArray)
                {
                    return false;
                }

                value = primitiveArray.PrimitiveType switch
                {
                    PrimitiveType.Boolean => primitiveArray.ArrayObjects.Cast<bool>().ToArray(),
                    PrimitiveType.Byte => primitiveArray.ArrayObjects.Cast<byte>().ToArray(),
                    PrimitiveType.Char => primitiveArray.ArrayObjects.Cast<char>().ToArray(),
                    PrimitiveType.Decimal => primitiveArray.ArrayObjects.Cast<decimal>().ToArray(),
                    PrimitiveType.Double => primitiveArray.ArrayObjects.Cast<double>().ToArray(),
                    PrimitiveType.Int16 => primitiveArray.ArrayObjects.Cast<short>().ToArray(),
                    PrimitiveType.Int32 => primitiveArray.ArrayObjects.Cast<int>().ToArray(),
                    PrimitiveType.Int64 => primitiveArray.ArrayObjects.Cast<long>().ToArray(),
                    PrimitiveType.SByte => primitiveArray.ArrayObjects.Cast<sbyte>().ToArray(),
                    PrimitiveType.Single => primitiveArray.ArrayObjects.Cast<float>().ToArray(),
                    PrimitiveType.TimeSpan => primitiveArray.ArrayObjects.Cast<TimeSpan>().ToArray(),
                    PrimitiveType.DateTime => primitiveArray.ArrayObjects.Cast<DateTime>().ToArray(),
                    PrimitiveType.UInt16 => primitiveArray.ArrayObjects.Cast<ushort>().ToArray(),
                    PrimitiveType.UInt32 => primitiveArray.ArrayObjects.Cast<uint>().ToArray(),
                    PrimitiveType.UInt64 => primitiveArray.ArrayObjects.Cast<ulong>().ToArray(),
                    _ => null
                };

                return value is not null;
            }
        }

        /// <summary>
        ///  Trys to get this object as a binary formatted <see cref="Hashtable"/> of <see cref="PrimitiveType"/> keys and values.
        /// </summary>
        public static bool TryGetPrimitiveHashtable(this BinaryFormattedObject format, [NotNullWhen(true)] out Hashtable? hashtable)
        {
            bool success = format.TryGetPrimitiveHashtable(out object? value);
            hashtable = (Hashtable?)value;
            return success;
        }

        /// <summary>
        ///  Trys to get this object as a binary formatted <see cref="Hashtable"/> of <see cref="PrimitiveType"/> keys and values.
        /// </summary>
        public static bool TryGetPrimitiveHashtable(this BinaryFormattedObject format, [NotNullWhen(true)] out object? hashtable)
        {
            return TryGet(Get, format, out hashtable);

            static bool Get(BinaryFormattedObject format, [NotNullWhen(true)] out object? hashtable)
            {
                hashtable = null;

                // Note that hashtables with custom comparers and/or hash code providers will have that information before
                // the value pair arrays.
                if (format.RecordCount != 5
                    || format[1] is not SystemClassWithMembersAndTypes classInfo
                    || classInfo.Name != TypeInfo.HashtableType
                    || format[2] is not ArraySingleObject keys
                    || format[3] is not ArraySingleObject values
                    || keys.Length != values.Length)
                {
                    return false;
                }

                Hashtable temp = new(keys.Length);
                for (int i = 0; i < keys.Length; i++)
                {
                    if (!format.TryGetPrimitiveRecordValue((IRecord)keys[i], out object? key)
                        || !format.TryGetPrimitiveRecordValueOrNull((IRecord)values[i], out object? value))
                    {
                        return false;
                    }
                    if(key!=null)
                    {
                        temp[key] = value;
                    }
                    
                }

                hashtable = temp;
                return true;
            }
        }

        /// <summary>
        ///  Tries to get the value for the given <paramref name="record"/> if it represents a <see cref="PrimitiveType"/>
        ///  that isn't <see cref="PrimitiveType.Null"/>.
        /// </summary>
        public static bool TryGetPrimitiveRecordValue(
            this BinaryFormattedObject format,
            IRecord record,
            [NotNullWhen(true)] out object? value)
        {
            format.TryGetPrimitiveRecordValueOrNull(record, out value);
            return value is not null;
        }

        /// <summary>
        ///  Tries to get the value for the given <paramref name="record"/> if it represents a <see cref="PrimitiveType"/>.
        /// </summary>
        public static bool TryGetPrimitiveRecordValueOrNull(
            this BinaryFormattedObject format,
            IRecord record,
            out object? value)
        {
            value = null;
            if (record is ObjectNull)
            {
                return true;
            }

            value = format.Dereference(record) switch
            {
                BinaryObjectString valueString => valueString.Value,
                MemberPrimitiveTyped primitive => primitive.Value,
                _ => null,
            };

            return value is not null;
        }

        /// <summary>
        ///  Trys to get this object as a binary formatted <see cref="NotSupportedException"/>.
        /// </summary>
        public static bool TryGetNotSupportedException(
            this BinaryFormattedObject format,
            out object? exception)
        {
            return TryGet(Get, format, out exception);

            static bool Get(BinaryFormattedObject format, [NotNullWhen(true)] out object? exception)
            {
                exception = null;

                if (format.RecordCount < 3
                    || format[1] is not SystemClassWithMembersAndTypes classInfo
                    || classInfo.Name != TypeInfo.NotSupportedExceptionType)
                {
                    return false;
                }

                exception = new NotSupportedException(classInfo["Message"].ToString());
                return true;
            }
        }

        /// <summary>
        ///  Try to get a supported .NET type object (not WinForms).
        /// </summary>
        public static bool TryGetFrameworkObject(
            this BinaryFormattedObject format,
            [NotNullWhen(true)] out object? value)
            => format.TryGetPrimitiveType(out value)
                || format.TryGetPrimitiveList(out value)
                || format.TryGetPrimitiveArray(out value)
                || format.TryGetPrimitiveArrayList(out value)
                || format.TryGetPrimitiveHashtable(out value)
                || format.TryGetRectangleF(out value)
                || format.TryGetPointF(out value)
                || format.TryGetNotSupportedException(out value);

        /// <summary>
        ///  Dereferences <see cref="MemberReference"/> records.
        /// </summary>
        public static IRecord Dereference(this BinaryFormattedObject format, IRecord record) => record switch
        {
            MemberReference reference => format[reference.IdRef],
            _ => record
        };

        /// <summary>
        ///  Gets <paramref name="count"/> number of strings from a <see cref="ArraySingleString"/>.
        /// </summary>
        public static IEnumerable<string?> GetStringValues(this BinaryFormattedObject format, ArraySingleString array, int count)
            => array.ArrayObjects.Take(count).Select(record =>
                format.Dereference((IRecord)record) switch
                {
                    BinaryObjectString stringRecord => stringRecord.Value,
                    _ => null
                });
    }
    
}