// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

 using System.Collections.Generic;

namespace System.Windows
{
    /// <summary>
    ///  Map of records that ensures that IDs are only entered once.
    /// </summary>
    internal class RecordMap
    {
        private readonly Dictionary<int, IRecord> _records = new();

        public IRecord this[Id id]
        {
            get => _records[id];
            set => _records.Add(id, value);
        }
    }

}
