// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Linq;

namespace System.Windows
{
    /// <summary>
    ///  Non-generic record base interface.
    /// </summary>
    internal interface IRecord : IBinaryWriteable
    {
        static virtual RecordType RecordType { get; }
    }

    /// <summary>
    ///  Typed record interface.
    /// </summary>
    internal interface IRecord<T> : IRecord, IBinaryFormatParseable<T> where T : class, IRecord
    {
    }
}

