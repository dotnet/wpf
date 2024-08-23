// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Collections;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

#nullable enable
namespace System.Formats.Nrbf
{
    internal static class SerializationRecordExtensions
    {
        private delegate bool TryGetDelegate(SerializationRecord record, [NotNullWhen(true)] out object? value);

        private static bool TryGet(TryGetDelegate get, SerializationRecord record, [NotNullWhen(true)] out object? value)
        {
            try
            {
                return get(record, out value);
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
        public static bool TryGetPointF(this SerializationRecord record, [NotNullWhen(true)] out object? value)
        {
            return TryGet(Get, record, out value);

            static bool Get(SerializationRecord record, [NotNullWhen(true)] out object? value)
            {
                value = default;

                if (record is not ClassRecord classInfo
                    || !classInfo.TypeNameMatches(typeof(PointF))
                    || !classInfo.HasMember("x")
                    || !classInfo.HasMember("y"))
                {
                    return false;
                }

                value = new PointF(classInfo.GetSingle("x"), classInfo.GetSingle("y"));

                return true;
            }
        }

        /// <summary>
        ///  Tries to get this object as a <see cref="RectangleF"/>.
        /// </summary>
        public static bool TryGetRectangleF(this SerializationRecord record, [NotNullWhen(true)] out object? value)
        {
            return TryGet(Get, record, out value);

            static bool Get(SerializationRecord record, [NotNullWhen(true)] out object? value)
            {
                value = default;

                if (record is not ClassRecord classInfo
                    || !classInfo.TypeNameMatches(typeof(RectangleF))
                    || !classInfo.HasMember("x")
                    || !classInfo.HasMember("y")
                    || !classInfo.HasMember("width")
                    || !classInfo.HasMember("height"))
                {
                    return false;
                }

                value = new RectangleF(
                    classInfo.GetSingle("x"),
                    classInfo.GetSingle("y"),
                    classInfo.GetSingle("width"),
                    classInfo.GetSingle("height"));

                return true;
            }
        }

        /// <summary>
        ///  Trys to get this object as a primitive type or string.
        /// </summary>
        /// <returns><see langword="true"/> if this represented a primitive type or string.</returns>
        public static bool TryGetPrimitiveType(this SerializationRecord record, [NotNullWhen(true)] out object? value)
        {
            return TryGet(Get, record, out value);

            static bool Get(SerializationRecord record, [NotNullWhen(true)] out object? value)
            {
                if (record.RecordType is SerializationRecordType.BinaryObjectString)
                {
                    value = ((PrimitiveTypeRecord<string>)record).Value;
                    return true;
                }
                else if (record.RecordType is SerializationRecordType.MemberPrimitiveTyped)
                {
                    value = ((PrimitiveTypeRecord)record).Value;
                    return true;
                }

                value = null;
                return false;
            }
        }

        /// <summary>
        ///  Trys to get this object as a <see cref="List{T}"/> of <see cref="PrimitiveType"/>.
        /// </summary>
        public static bool TryGetPrimitiveList(this SerializationRecord record, [NotNullWhen(true)] out object? list)
        {
            return TryGet(Get, record, out list);

            static bool Get(SerializationRecord record, [NotNullWhen(true)] out object? list)
            {
                list = null;

                if (record is not ClassRecord classInfo
                    || !classInfo.HasMember("_items")
                    || !classInfo.HasMember("_size")
                    || classInfo.GetRawValue("_size") is not int size
                    || !classInfo.TypeName.IsConstructedGenericType
                    || classInfo.TypeName.GetGenericTypeDefinition().Name != typeof(List<>).Name
                    || classInfo.TypeName.GetGenericArguments().Length != 1
                    || classInfo.GetRawValue("_items") is not ArrayRecord arrayRecord
                    || !IsPrimitiveArrayRecord(arrayRecord))
                {
                    return false;
                }

                // BinaryFormatter serializes the entire backing array, so we need to trim it down to the size of the list.
                list = arrayRecord switch
                {
                    SZArrayRecord<string> ar => ar.GetArray().CreateTrimmedList(size),
                    SZArrayRecord<bool> ar => ar.GetArray().CreateTrimmedList(size),
                    SZArrayRecord<byte> ar => ar.GetArray().CreateTrimmedList(size),
                    SZArrayRecord<sbyte> ar => ar.GetArray().CreateTrimmedList(size),
                    SZArrayRecord<char> ar => ar.GetArray().CreateTrimmedList(size),
                    SZArrayRecord<short> ar => ar.GetArray().CreateTrimmedList(size),
                    SZArrayRecord<ushort> ar => ar.GetArray().CreateTrimmedList(size),
                    SZArrayRecord<int> ar => ar.GetArray().CreateTrimmedList(size),
                    SZArrayRecord<uint> ar => ar.GetArray().CreateTrimmedList(size),
                    SZArrayRecord<long> ar => ar.GetArray().CreateTrimmedList(size),
                    SZArrayRecord<ulong> ar => ar.GetArray().CreateTrimmedList(size),
                    SZArrayRecord<float> ar => ar.GetArray().CreateTrimmedList(size),
                    SZArrayRecord<double> ar => ar.GetArray().CreateTrimmedList(size),
                    SZArrayRecord<decimal> ar => ar.GetArray().CreateTrimmedList(size),
                    SZArrayRecord<TimeSpan> ar => ar.GetArray().CreateTrimmedList(size),
                    SZArrayRecord<DateTime> ar => ar.GetArray().CreateTrimmedList(size),
                    _ => throw new InvalidOperationException()
                };

                return true;
            }
            }

        /// <summary>
        ///  Tries to get this object as a <see cref="ArrayList"/> of <see cref="PrimitiveType"/> values.
        /// </summary>
        public static bool TryGetPrimitiveArrayList(this SerializationRecord record, [NotNullWhen(true)] out object? value)
        {
            return TryGet(Get, record, out value);

            static bool Get(SerializationRecord record, [NotNullWhen(true)] out object? value)
            {
                value = null;

                if (record is not ClassRecord classInfo
                    || !classInfo.TypeNameMatches(typeof(ArrayList))
                    || !classInfo.HasMember("_items")
                    || !classInfo.HasMember("_size")
                    || classInfo.GetRawValue("_size") is not int size
                    || classInfo.GetRawValue("_items") is not SZArrayRecord<object> arrayRecord
                    || size > arrayRecord.Length)
                {
                    return false;
                }

                ArrayList arrayList = new(size);
                object?[] array = arrayRecord.GetArray();
                for (int i = 0; i < size; i++)
                {
                    if (array[i] is SerializationRecord)
                    {
                        return false;
                    }

                    arrayList.Add(array[i]);
                }

                value = arrayList;
                return true;
            }
        }

        /// <summary>
        ///  Tries to get this object as an <see cref="Array"/> of primitive types.
        /// </summary>
        public static bool TryGetPrimitiveArray(this SerializationRecord record, [NotNullWhen(true)] out object? value)
        {
            return TryGet(Get, record, out value);

            static bool Get(SerializationRecord record, [NotNullWhen(true)] out object? value)
            {
                if (!IsPrimitiveArrayRecord(record))
                {
                    value = null;
                    return false;
                }

                value = record switch
                {
                    SZArrayRecord<string> ar => ar.GetArray(),
                    SZArrayRecord<bool> ar => ar.GetArray(),
                    SZArrayRecord<byte> ar => ar.GetArray(),
                    SZArrayRecord<sbyte> ar => ar.GetArray(),
                    SZArrayRecord<char> ar => ar.GetArray(),
                    SZArrayRecord<short> ar => ar.GetArray(),
                    SZArrayRecord<ushort> ar => ar.GetArray(),
                    SZArrayRecord<int> ar => ar.GetArray(),
                    SZArrayRecord<uint> ar => ar.GetArray(),
                    SZArrayRecord<long> ar => ar.GetArray(),
                    SZArrayRecord<ulong> ar => ar.GetArray(),
                    SZArrayRecord<float> ar => ar.GetArray(),
                    SZArrayRecord<double> ar => ar.GetArray(),
                    SZArrayRecord<decimal> ar => ar.GetArray(),
                    SZArrayRecord<TimeSpan> ar => ar.GetArray(),
                    SZArrayRecord<DateTime> ar => ar.GetArray(),
                    _ => throw new InvalidOperationException()
                };

                return value is not null;
            }
        }

        /// <summary>
        ///  Trys to get this object as a binary recordted <see cref="Hashtable"/> of <see cref="PrimitiveType"/> keys and values.
        /// </summary>
        public static bool TryGetPrimitiveHashtable(this SerializationRecord record, [NotNullWhen(true)] out object? hashtable)
        {
            return TryGet(Get, record, out hashtable);

            static bool Get(SerializationRecord record, [NotNullWhen(true)] out object? hashtable)
            {
                hashtable = null;

                if (record.RecordType != SerializationRecordType.SystemClassWithMembersAndTypes
                    || record is not ClassRecord classInfo
                    || !classInfo.TypeNameMatches(typeof(Hashtable))
                    || !classInfo.HasMember("Keys")
                    || !classInfo.HasMember("Values")
                    // Note that hashtables with custom comparers and/or hash code providers will have non null Comparer
                    || classInfo.GetSerializationRecord("Comparer") is not null
                    || classInfo.GetSerializationRecord("Keys") is not SZArrayRecord<object?> keysRecord
                    || classInfo.GetSerializationRecord("Values") is not SZArrayRecord<object?> valuesRecord
                    || keysRecord.Length != valuesRecord.Length)
                {
                    return false;
                }

                Hashtable temp = new(keysRecord.Length);
                object?[] keys = keysRecord.GetArray();
                object?[] values = valuesRecord.GetArray();
                for (int i = 0; i < keys.Length; i++)
                {
                    object? key = keys[i];
                    object? value = values[i];

                    if (key is null or SerializationRecord || value is SerializationRecord)
                    {
                        return false;
                    }

                    temp[key] = value;
                }

                hashtable = temp;
                return true;
            }
        }

        /// <summary>
        ///  Trys to get this object as a binary recordted <see cref="NotSupportedException"/>.
        /// </summary>
        public static bool TryGetNotSupportedException(
            this SerializationRecord record,
            out object? exception)
        {
            return TryGet(Get, record, out exception);

            static bool Get(SerializationRecord record, [NotNullWhen(true)] out object? exception)
            {
                exception = null;

                if (record is not ClassRecord classInfo
                    || !classInfo.TypeNameMatches(typeof(NotSupportedException)))
                {
                    return false;
                }

                exception = new NotSupportedException(classInfo.GetString("Message"));
                return true;
            }
        }

        /// <summary>
        ///  Try to get a supported .NET type object (not WinForms).
        /// </summary>
        public static bool TryGetFrameworkObject(
            this SerializationRecord record,
            [NotNullWhen(true)] out object? value)
            => record.TryGetPrimitiveType(out value)
                || record.TryGetPrimitiveList(out value)
                || record.TryGetPrimitiveArray(out value)
                || record.TryGetPrimitiveArrayList(out value)
                || record.TryGetPrimitiveHashtable(out value)
                || record.TryGetRectangleF(out value)
                || record.TryGetPointF(out value)
                || record.TryGetNotSupportedException(out value);

        private static bool IsPrimitiveArrayRecord(SerializationRecord serializationRecord)
            => serializationRecord.RecordType is SerializationRecordType.ArraySingleString or SerializationRecordType.ArraySinglePrimitive;

        /// <summary>
        ///  Creates a list trimmed to the given count.
        /// </summary>
        /// <remarks>
        ///  <para>
        ///   This is an optimized implementation that avoids iterating over the entire list when possible.
        ///  </para>
        /// </remarks>
        internal static List<T> CreateTrimmedList<T>(this IReadOnlyList<T> readOnlyList, int count)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(readOnlyList.Count, count, nameof(count));

            // List<T> will use ICollection<T>.CopyTo if it's available, which is faster than iterating over the list.
            // If we just have an array this can be done easily with ArraySegment<T>.
            if (readOnlyList is T[] array)
            {
                return new List<T>(new ArraySegment<T>(array, 0, count));
            }

            // Fall back to just setting the count (by removing).
            List<T> list = new(readOnlyList);
            list.RemoveRange(count, list.Count - count);
            return list;
        }
    }
}
