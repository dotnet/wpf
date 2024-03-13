// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows
{
    internal static class TypeInfo
    {
        public const string BooleanType = "System.Boolean";
        public const string CharType = "System.Char";
        public const string StringType = "System.String";
        public const string SByteType = "System.SByte";
        public const string ByteType = "System.Byte";
        public const string Int16Type = "System.Int16";
        public const string UInt16Type = "System.UInt16";
        public const string Int32Type = "System.Int32";
        public const string UInt32Type = "System.UInt32";
        public const string Int64Type = "System.Int64";
        public const string DecimalType = "System.Decimal";
        public const string UInt64Type = "System.UInt64";
        public const string SingleType = "System.Single";
        public const string DoubleType = "System.Double";
        public const string TimeSpanType = "System.TimeSpan";
        public const string DateTimeType = "System.DateTime";
        public const string IntPtrType = "System.IntPtr";
        public const string UIntPtrType = "System.UIntPtr";

        public const string HashtableType = "System.Collections.Hashtable";
        public const string IDictionaryType = "System.Collections.IDictionary";
        public const string ExceptionType = "System.Exception";
        public const string NotSupportedExceptionType = "System.NotSupportedException";

        public const string MscorlibAssemblyName
            = "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        public const string SystemDrawingAssemblyName
            = "System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";

        /// <summary>
        ///  Returns the <see cref="PrimitiveType"/> for the given <paramref name="typeName"/>.
        /// </summary>
        internal static PrimitiveType GetPrimitiveType(ReadOnlySpan<char> typeName) => typeName switch
        {
            BooleanType => PrimitiveType.Boolean,
            CharType => PrimitiveType.Char,
            SByteType => PrimitiveType.SByte,
            ByteType => PrimitiveType.Byte,
            Int16Type => PrimitiveType.Int16,
            UInt16Type => PrimitiveType.UInt16,
            Int32Type => PrimitiveType.Int32,
            UInt32Type => PrimitiveType.UInt32,
            Int64Type => PrimitiveType.Int64,
            UInt64Type => PrimitiveType.UInt64,
            SingleType => PrimitiveType.Single,
            DoubleType => PrimitiveType.Double,
            DecimalType => PrimitiveType.Decimal,
            DateTimeType => PrimitiveType.DateTime,
            StringType => PrimitiveType.String,
            TimeSpanType => PrimitiveType.TimeSpan,
            _ => default,
        };

        /// <summary>
        ///  Returns the <see cref="PrimitiveType"/> for the given <paramref name="type"/>.
        /// </summary>
        /// <returns><see cref="PrimitiveType"/> or <see langword="default"/> if not a <see cref="PrimitiveType"/>.</returns>
        internal static PrimitiveType GetPrimitiveType(Type type) => Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => PrimitiveType.Boolean,
            TypeCode.Char => PrimitiveType.Char,
            TypeCode.SByte => PrimitiveType.SByte,
            TypeCode.Byte => PrimitiveType.Byte,
            TypeCode.Int16 => PrimitiveType.Int16,
            TypeCode.UInt16 => PrimitiveType.UInt16,
            TypeCode.Int32 => PrimitiveType.Int32,
            TypeCode.UInt32 => PrimitiveType.UInt32,
            TypeCode.Int64 => PrimitiveType.Int64,
            TypeCode.UInt64 => PrimitiveType.UInt64,
            TypeCode.Single => PrimitiveType.Single,
            TypeCode.Double => PrimitiveType.Double,
            TypeCode.Decimal => PrimitiveType.Decimal,
            TypeCode.DateTime => PrimitiveType.DateTime,
            TypeCode.String => PrimitiveType.String,
            // TypeCode.Empty => 0,
            // TypeCode.Object => 0,
            // TypeCode.DBNull => 0,
            _ => type == typeof(TimeSpan) ? PrimitiveType.TimeSpan : default,
        };
    }
}
