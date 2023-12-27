// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Windows
{
    /// <summary>
    ///  Writer that writes specific types in binary format without using the BinaryFormatter.
    /// </summary>
    internal static class BinaryFormatWriter
    {
        private static string[]? s_hashtableMemberNames;
        private static string[] HashtableMemberNames => s_hashtableMemberNames ??= new[]
        {
            "LoadFactor", "Version", "Comparer", "HashCodeProvider", "HashSize", "Keys", "Values"
        };

        private static string[]? s_notSupportedExceptionMemberNames;
        private static string[] NotSupportedExceptionMemberNames => s_notSupportedExceptionMemberNames ??= new[]
        {
            "ClassName", "Message", "Data", "InnerException", "HelpURL", "StackTraceString", "RemoteStackTraceString",
            "RemoteStackIndex", "ExceptionMethod", "HResult", "Source", "WatsonBuckets"
        };

        private static string[]? s_listMemberNames;
        private static string[] ListMemberNames => s_listMemberNames ??= new[] { "_items", "_size", "_version" };

        private static string[]? s_decimalMemberNames;
        private static string[] DecimalMemberNames => s_decimalMemberNames ??= new[] { "flags", "hi", "lo", "mid" };

        private static readonly string[] s_dateTimeMemberNames = new[] { "ticks", "dateData" };
        private static readonly string[] s_primitiveMemberName = new[] { "m_value" };

        private static string[]? s_pointMemberNames;
        private static string[] PointMemberNames => s_pointMemberNames ??= new[] { "x", "y" };

        private static string[]? s_rectangleMemberNames;
        private static string[] RectangleMemberNames => s_rectangleMemberNames ??= new[] { "x", "y", "width", "height" };

        /// <summary>
        ///  Writes a <see langword="string"/> in binary format.
        /// </summary>
        public static void WriteString(Stream stream, string value)
        {
            using var writer = new BinaryFormatWriterScope(stream);
            new BinaryObjectString(1, value).Write(writer);
        }

        /// <summary>
        ///  Writes a <see langword="decimal"/> in binary format.
        /// </summary>
        public static void WriteDecimal(Stream stream, decimal value)
        {
            Span<int> ints = stackalloc int[4];
            decimal.TryGetBits(value, ints, out _);

            using var writer = new BinaryFormatWriterScope(stream);

            new SystemClassWithMembersAndTypes(
                new ClassInfo(1, typeof(decimal).FullName!, DecimalMemberNames),
                new MemberTypeInfo(
                    (BinaryType.Primitive, PrimitiveType.Int32),
                    (BinaryType.Primitive, PrimitiveType.Int32),
                    (BinaryType.Primitive, PrimitiveType.Int32),
                    (BinaryType.Primitive, PrimitiveType.Int32)),
                ints[3],
                ints[2],
                ints[0],
                ints[1]).Write(writer);
        }

        /// <summary>
        ///  Writes a <see cref="DateTime"/> in binary format.
        /// </summary>
        public static void WriteDateTime(Stream stream, DateTime value)
        {
            using var writer = new BinaryFormatWriterScope(stream);

            // We could use ISerializable here to get the data, but it is pretty
            // heavy weight, and the internals of DateTime should never change.

            new SystemClassWithMembersAndTypes(
                new ClassInfo(1, typeof(DateTime).FullName!, s_dateTimeMemberNames),
                new MemberTypeInfo(
                    (BinaryType.Primitive, PrimitiveType.Int64),
                    (BinaryType.Primitive, PrimitiveType.UInt64)),
                value.Ticks,
                Unsafe.As<DateTime, ulong>(ref value)).Write(writer);
        }

        /// <summary>
        ///  Writes a <see cref="TimeSpan"/> in binary format.
        /// </summary>
        public static void WriteTimeSpan(Stream stream, TimeSpan value)
        {
            using var writer = new BinaryFormatWriterScope(stream);
            new SystemClassWithMembersAndTypes(
                new ClassInfo(1, typeof(TimeSpan).FullName!, new string[] { "_ticks" }),
                new MemberTypeInfo((BinaryType.Primitive, PrimitiveType.Int64)),
                value.Ticks).Write(writer);
        }

        /// <summary>
        ///  Writes a nint in binary format.
        /// </summary>
        public static void WriteNativeInt(Stream stream, nint value)
        {
            using var writer = new BinaryFormatWriterScope(stream);
            new SystemClassWithMembersAndTypes(
                new ClassInfo(1, typeof(nint).FullName!, new string[] { "value" }),
                new MemberTypeInfo((BinaryType.Primitive, PrimitiveType.Int64)),
                (long)value).Write(writer);
        }

        /// <summary>
        ///  Writes a nuint in binary format.
        /// </summary>
        public static void WriteNativeUInt(Stream stream, nuint value)
        {
            using var writer = new BinaryFormatWriterScope(stream);
            new SystemClassWithMembersAndTypes(
                new ClassInfo(1, typeof(nuint).FullName!, new string[] { "value" }),
                new MemberTypeInfo((BinaryType.Primitive, PrimitiveType.UInt64)),
                (ulong)value).Write(writer);
        }

        /// <summary>
        ///  Writes a <see cref="PointF"/> in binary format.
        /// </summary>
        public static void WritePointF(Stream stream, PointF value)
        {
            using BinaryFormatWriterScope writer = new(stream);
            new BinaryLibrary(2, TypeInfo.SystemDrawingAssemblyName).Write(writer);
            new ClassWithMembersAndTypes(
                new ClassInfo(1, typeof(PointF).FullName!, PointMemberNames),
                libraryId: 2,
                new MemberTypeInfo(
                    (BinaryType.Primitive, PrimitiveType.Single),
                    (BinaryType.Primitive, PrimitiveType.Single)),
                value.X,
                value.Y).Write(writer);
        }

        /// <summary>
        ///  Writes a <see cref="RectangleF"/> in binary format.
        /// </summary>
        public static void WriteRectangleF(Stream stream, RectangleF value)
        {
            using BinaryFormatWriterScope writer = new(stream);
            new BinaryLibrary(2, TypeInfo.SystemDrawingAssemblyName).Write(writer);
            new ClassWithMembersAndTypes(
                new ClassInfo(1, typeof(RectangleF).FullName!, RectangleMemberNames),
                libraryId: 2,
                new MemberTypeInfo(
                    (BinaryType.Primitive, PrimitiveType.Single),
                    (BinaryType.Primitive, PrimitiveType.Single),
                    (BinaryType.Primitive, PrimitiveType.Single),
                    (BinaryType.Primitive, PrimitiveType.Single)),
                value.X,
                value.Y,
                value.Width,
                value.Height).Write(writer);
        }

        /// <summary>
        ///  Attempts to write a <see cref="PrimitiveType"/> value in binary format.
        /// </summary>
        /// <returns><see langword="true"/> if successful.</returns>
        public static bool TryWritePrimitive(Stream stream, object primitive)
            => TryWrite(WritePrimitive, stream, primitive);

        /// <summary>
        ///  Writes a .NET primitive value in binary format.
        /// </summary>
        /// <exception cref="ArgumentException">
        ///  <paramref name="primitive"/> is not a a primitive value.
        /// </exception>
        public static void WritePrimitive(Stream stream, object primitive)
        {
            Type type = primitive.GetType();
            PrimitiveType primitiveType = TypeInfo.GetPrimitiveType(type);

            if (primitiveType == default)
            {
                // These two are considered primitive by .NET but not the binary format spec
                switch (primitive)
                {
                    case nint nativeInt:
                        WriteNativeInt(stream, nativeInt);
                        return;
                    case nuint nativeUint:
                        WriteNativeUInt(stream, nativeUint);
                        return;
                }

                throw new ArgumentException($"{nameof(primitive)} is not primitive.");
            }

            // These are handled differently from the rest of the primitive types when serialized on their own.
            switch (primitiveType)
            {
                case PrimitiveType.String:
                    WriteString(stream, (string)primitive);
                    return;
                case PrimitiveType.Decimal:
                    WriteDecimal(stream, (decimal)primitive);
                    return;
                case PrimitiveType.DateTime:
                    WriteDateTime(stream, (DateTime)primitive);
                    return;
                case PrimitiveType.TimeSpan:
                    WriteTimeSpan(stream, (TimeSpan)primitive);
                    return;
            }

            using var writer = new BinaryFormatWriterScope(stream);
            new SystemClassWithMembersAndTypes(
                new ClassInfo(1, type.FullName!, s_primitiveMemberName),
                new MemberTypeInfo((BinaryType.Primitive, primitiveType)),
                primitive).Write(writer);
        }

        /// <summary>
        ///  Writes a <see cref="List{String}"/> in binary format.
        /// </summary>
        public static void WriteStringList(Stream stream, List<string> list)
        {
            using BinaryFormatWriterScope writer = new(stream);

            new SystemClassWithMembersAndTypes(
                new ClassInfo(
                    1,
                    $"System.Collections.Generic.List`1[[{TypeInfo.StringType}, {TypeInfo.MscorlibAssemblyName}]]",
                    ListMemberNames),
                new MemberTypeInfo(
                    (BinaryType.StringArray, null),
                    (BinaryType.Primitive, PrimitiveType.Int32),
                    (BinaryType.Primitive, PrimitiveType.Int32)),
                new MemberReference(2),
                list.Count,
                // _version doesn't matter
                0).Write(writer);

            StringRecordsCollection strings = new(currentId: 3);

            new ArraySingleString(
                new ArrayInfo(2, list.Count),
                new ListConverter<string>(list, strings.GetStringRecord)).Write(writer);
        }

        /// <summary>
        ///  Writes a primitive list in binary format.
        /// </summary>
        public static void WritePrimitiveList<T>(Stream stream, List<T> list)
            where T : unmanaged
        {
            PrimitiveType primitiveType = TypeInfo.GetPrimitiveType(typeof(T));
            if (primitiveType == default)
            {
                throw new NotSupportedException($"{nameof(T)} is not primitive.");
            }

            using var writer = new BinaryFormatWriterScope(stream);

            new SystemClassWithMembersAndTypes(
                new ClassInfo(
                    1,
                    $"System.Collections.Generic.List`1[[{typeof(T).FullName}, {TypeInfo.MscorlibAssemblyName}]]",
                    ListMemberNames),
                new MemberTypeInfo(
                    (BinaryType.PrimitiveArray, primitiveType),
                    (BinaryType.Primitive, PrimitiveType.Int32),
                    (BinaryType.Primitive, PrimitiveType.Int32)),
                new MemberReference(2),
                list.Count,
                // _version doesn't matter
                0).Write(writer);

            new ArraySinglePrimitive(
                new ArrayInfo(2, list.Count),
                primitiveType,
                new ListConverter<T>(list, (T value) => value!)).Write(writer);
        }

        /// <summary>
        ///  Writes the given <paramref name="list"/> in binary format if supported.
        /// </summary>
        public static bool TryWritePrimitiveList(Stream stream, IList list)
        {
            Type type = list.GetType();
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(List<>))
            {
                return false;
            }

            return TryWrite(Write, stream, list);

            static bool Write(Stream stream, IList list)
            {
                switch (list)
                {
                    case List<string> typedList:
                        WriteStringList(stream, typedList);
                        return true;
                    case List<int> typedList:
                        WritePrimitiveList(stream, typedList);
                        return true;
                    case List<byte> typedList:
                        WritePrimitiveList(stream, typedList);
                        return true;
                    case List<sbyte> typedList:
                        WritePrimitiveList(stream, typedList);
                        return true;
                    case List<short> typedList:
                        WritePrimitiveList(stream, typedList);
                        return true;
                    case List<ushort> typedList:
                        WritePrimitiveList(stream, typedList);
                        return true;
                    case List<uint> typedList:
                        WritePrimitiveList(stream, typedList);
                        return true;
                    case List<long> typedList:
                        WritePrimitiveList(stream, typedList);
                        return true;
                    case List<ulong> typedList:
                        WritePrimitiveList(stream, typedList);
                        return true;
                    case List<float> typedList:
                        WritePrimitiveList(stream, typedList);
                        return true;
                    case List<double> typedList:
                        WritePrimitiveList(stream, typedList);
                        return true;
                    case List<decimal> typedList:
                        WritePrimitiveList(stream, typedList);
                        return true;
                    case List<DateTime> typedList:
                        WritePrimitiveList(stream, typedList);
                        return true;
                    case List<TimeSpan> typedList:
                        WritePrimitiveList(stream, typedList);
                        return true;
                    case List<char> typedList:
                        WritePrimitiveList(stream, typedList);
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        ///  Writes the given <paramref name="list"/> in binary format if supported.
        /// </summary>
        public static bool TryWriteArrayList(Stream stream, ArrayList list)
        {
            return TryWrite(Write, stream, list);

            static bool Write(Stream stream, ArrayList list)
            {
                using BinaryFormatWriterScope writer = new(stream);
                new SystemClassWithMembersAndTypes(
                    new ClassInfo(1, typeof(ArrayList).FullName!, ListMemberNames),
                    new MemberTypeInfo(
                        (BinaryType.ObjectArray, null),
                        (BinaryType.Primitive, PrimitiveType.Int32),
                        (BinaryType.Primitive, PrimitiveType.Int32)),
                    new MemberReference(2),
                    list.Count,
                    // _version doesn't matter
                    0).Write(writer);

                new ArraySingleObject(
                    new ArrayInfo(2, list.Count),
                    ListConverter.GetPrimitiveConverter(list, new StringRecordsCollection(currentId: 3))).Write(writer);

                return true;
            }
        }

        /// <summary>
        ///  Writes the given <paramref name="array"/> in binary format if supported.
        /// </summary>
        public static bool TryWriteArray(Stream stream, Array array)
        {
            return TryWrite(Write, stream, array);

            static bool Write(Stream stream, Array array)
            {
                PrimitiveType primitiveType = TypeInfo.GetPrimitiveType(array.GetType().GetElementType()!);

                if (primitiveType == default)
                {
                    return false;
                }

                using BinaryFormatWriterScope writer = new(stream);
                if (primitiveType == PrimitiveType.String)
                {
                    StringRecordsCollection strings = new(currentId: 2);
                    new ArraySingleString(
                        new ArrayInfo(1, array.Length),
                        ListConverter.GetPrimitiveConverter(array, strings)).Write(writer);
                    return true;
                }

                new ArraySinglePrimitive(
                    new ArrayInfo(1, array.Length),
                    primitiveType,
                    new ListConverter<object?>(array, o => o!)).Write(writer);

                return true;
            }
        }

        /// <summary>
        ///  Tries to write the given <paramref name="hashtable"/> if supported.
        /// </summary>
        public static bool TryWriteHashtable(Stream stream, Hashtable hashtable)
            => TryWrite(WritePrimitiveHashtable, stream, hashtable);

        /// <summary>
        ///  Writes a <see cref="Hashtable"/> of primitive to primitive values to the given stream in binary format.
        /// </summary>
        /// <remarks>
        ///  <para>
        ///   Primitive types are anything in the <see cref="PrimitiveType"/> enum.
        ///  </para>
        /// </remarks>
        /// <exception cref="ArgumentException">
        ///  <paramref name="hashtable"/> contained non-primitive values or a custom comparer or hash code provider.
        /// </exception>
        public static void WritePrimitiveHashtable(Stream stream, Hashtable hashtable)
        {
            // Get the ISerializable data from the hashtable. This way we don't have to worry about
            // getting the LoadFactor, Version, etc. wrong.
    #pragma warning disable SYSLIB0050 // Type or member is obsolete
            SerializationInfo info = new(typeof(Hashtable), FormatterConverterStub.Instance);
    #pragma warning restore SYSLIB0050
    #pragma warning disable SYSLIB0051 // Type or member is obsolete
            hashtable.GetObjectData(info, default);
    #pragma warning restore SYSLIB0051

            if (info.GetValue<object?>("Comparer") is not null
                || info.GetValue<object?>("HashCodeProvider") is not null)
            {
                throw new ArgumentException("Hashtable has custom Comparer or HashCodeProvider.", nameof(hashtable));
            }

            // Build up the key and value data
            object[] keys = info.GetValue<object[]>("Keys")!;
            object?[] values = info.GetValue<object?[]>("Values")!;

            using var writer = new BinaryFormatWriterScope(stream);

            new SystemClassWithMembersAndTypes(
                new ClassInfo(1, TypeInfo.HashtableType, HashtableMemberNames),
                new MemberTypeInfo(
                    (BinaryType.Primitive, PrimitiveType.Single),
                    (BinaryType.Primitive, PrimitiveType.Int32),
                    (BinaryType.SystemClass, "System.Collections.IComparer"),
                    (BinaryType.SystemClass, "System.Collections.IHashCodeProvider"),
                    (BinaryType.Primitive, PrimitiveType.Int32),
                    (BinaryType.ObjectArray, null),
                    (BinaryType.ObjectArray, null)),
                info.GetValue<float>("LoadFactor"),
                info.GetValue<int>("Version"),
                // No need to persist the comparer and hashcode provider
                ObjectNull.Instance,
                ObjectNull.Instance,
                info.GetValue<int>("HashSize"),
                // MemberReference to Arrays here
                new MemberReference(2),
                new MemberReference(3)).Write(writer);

            // 1, 2 and 3 are used for the class id and array ids.
            StringRecordsCollection strings = new(currentId: 4);

            new ArraySingleObject(
                new ArrayInfo(2, keys.Length),
                ListConverter.GetPrimitiveConverter(keys, strings)).Write(writer);
            new ArraySingleObject(
                new ArrayInfo(3, values.Length),
                ListConverter.GetPrimitiveConverter(values, strings)).Write(writer);
        }

        /// <summary>
        ///  Writes a <see cref="NotSupportedException"/> in binary format.
        /// </summary>
        public static void WriteNotSupportedException(Stream stream, NotSupportedException exception)
        {
            using var writer = new BinaryFormatWriterScope(stream);

            // We only serialize the message to avoid binary serialization risks.
            new SystemClassWithMembersAndTypes(
                new ClassInfo(1, TypeInfo.NotSupportedExceptionType, NotSupportedExceptionMemberNames),
                new MemberTypeInfo(
                    (BinaryType.String, null),
                    (BinaryType.String, null),
                    (BinaryType.SystemClass, TypeInfo.IDictionaryType),
                    (BinaryType.SystemClass, TypeInfo.ExceptionType),
                    (BinaryType.String, null),
                    (BinaryType.String, null),
                    (BinaryType.String, null),
                    (BinaryType.Primitive, PrimitiveType.Int32),
                    (BinaryType.String, null),
                    (BinaryType.Primitive, PrimitiveType.Int32),
                    (BinaryType.String, null),
                    (BinaryType.PrimitiveArray, PrimitiveType.Byte)),
                new BinaryObjectString(2, TypeInfo.NotSupportedExceptionType),
                new BinaryObjectString(3, exception.Message),
                ObjectNull.Instance,
                ObjectNull.Instance,
                ObjectNull.Instance,
                ObjectNull.Instance,
                ObjectNull.Instance,
                0,
                ObjectNull.Instance,
                exception.HResult,
                ObjectNull.Instance,
                ObjectNull.Instance).Write(writer);
        }

        /// <summary>
        ///  Writes the given <paramref name="value"/> if supported.
        /// </summary>
        public static bool TryWriteFrameworkObject(Stream stream, object value)
        {
            return TryWrite(Write, stream, value);

            static bool Write(Stream stream, object value)
            {
                Type type = value.GetType();
                if (type.IsPrimitive)
                {
                    WritePrimitive(stream, value);
                    return true;
                }

                switch (value)
                {
                    case string stringValue:
                        WriteString(stream, stringValue);
                        return true;
                    case Array arrayValue:
                        return TryWriteArray(stream, arrayValue);
                    case decimal decimalValue:
                        WriteDecimal(stream, decimalValue);
                        return true;
                    case DateTime dateTime:
                        WriteDateTime(stream, dateTime);
                        return true;
                    case TimeSpan timeSpan:
                        WriteTimeSpan(stream, timeSpan);
                        return true;
                    case PointF point:
                        WritePointF(stream, point);
                        return true;
                    case RectangleF rectangle:
                        WriteRectangleF(stream, rectangle);
                        return true;
                    case Hashtable hashtable:
                        return TryWriteHashtable(stream, hashtable);
                    case ArrayList arrayList:
                        return TryWriteArrayList(stream, arrayList);
                    case NotSupportedException exception:
                        WriteNotSupportedException(stream, exception);
                        return true;
                }

                return type.IsGenericType
                    && type.GetGenericTypeDefinition() == typeof(List<>)
                    && TryWritePrimitiveList(stream, (IList)value);
            }
        }

        /// <summary>
        ///  Simple wrapper to ensure the <paramref name="stream"/> is reset to it's original position if the
        ///  <paramref name="action"/> throws.
        /// </summary>
        public static bool TryWrite<T>(Action<Stream, T> action, Stream stream, T value)
            => TryWrite(
                (s, o) => { action(s, o); return true; },
                stream,
                value);

        /// <summary>
        ///  Simple wrapper to ensure the <paramref name="stream"/> is reset to it's original position if the
        ///  <paramref name="func"/> throws or returns <see langword="false"/>.
        /// </summary>
        public static bool TryWrite<T>(Func<Stream, T, bool> func, Stream stream, T value)
        {
            long position = stream.Position;
            bool success;

            try
            {
                success = func(stream, value);
            }
            catch (Exception ex) when (!ex.IsCriticalException())
            {
                Debug.WriteLine($"Failed to binary format: {ex.Message}");
                Debug.Assert(ex is ArgumentException, "We should only be getting ArgumentExceptions on write.");
                success = false;
            }

            if (!success)
            {
                stream.Position = position;
            }

            return success;
        }
    }
}
