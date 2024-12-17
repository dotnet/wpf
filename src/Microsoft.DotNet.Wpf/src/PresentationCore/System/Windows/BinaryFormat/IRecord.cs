// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace System.Windows
{
    /// <summary>
    ///  Non-generic record base interface.
    /// </summary>
    internal interface IRecord : IBinaryWriteable
    {
        static virtual RecordType RecordType { get; }
    }
}

