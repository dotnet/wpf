// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows
{
    /// <summary>
    ///  Primitive type.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/4e77849f-89e3-49db-8fb9-e77ee4bc7214">
    ///    [MS-NRBF] 2.1.2.3
    ///   </see>
    ///  </para>
    /// </remarks>
    internal enum PrimitiveType : byte
    {
        Boolean = 1,
        Byte,
        Char,
        Decimal = 5,
        Double,
        Int16,
        Int32,
        Int64,
        SByte,
        Single,
        TimeSpan,
        DateTime,
        UInt16,
        UInt32,
        UInt64,
        Null,
        String
    }
}
