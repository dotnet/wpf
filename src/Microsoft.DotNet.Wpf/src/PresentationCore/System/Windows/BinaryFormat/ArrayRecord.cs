// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace System.Windows
{
    /// <summary>
    ///  Base class for array records.
    /// </summary>
    /// <devdoc>
    ///  <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/f57d41e5-d3c0-4340-add8-fa4449a68d1c">
    ///  [MS-NRBF] 2.4</see> describes how item records must follow the array record and how multiple null records
    ///  can be coalesced into an <see cref="NullRecord.ObjectNullMultiple"/> or <see cref="NullRecord.ObjectNullMultiple256"/>
    ///  record.
    /// </devdoc>
    internal abstract class ArrayRecord : Record, IEnumerable<object>
    {
        public ArrayInfo ArrayInfo { get; }

        /// <summary>
        ///  The array items.
        /// </summary>
        /// <remarks>
        ///  <para>
        ///   Multi-null records are always expanded to individual <see cref="ObjectNull"/> entries when reading.
        ///  </para>
        /// </remarks>
        public IReadOnlyList<object> ArrayObjects { get; }

        /// <summary>
        ///  Identifier for the array.
        /// </summary>
        public Id ObjectId => ArrayInfo.ObjectId;

        /// <summary>
        ///  Length of the array.
        /// </summary>
        public Count Length => ArrayInfo.Length;

        /// <summary>
        ///  Returns the item at the given index.
        /// </summary>
        public object this[int index] => ArrayObjects[index];

        public ArrayRecord(ArrayInfo arrayInfo, IReadOnlyList<object> arrayObjects)
        {
            if (arrayInfo.Length != arrayObjects.Count)
            {
                throw new ArgumentException($"{nameof(arrayInfo)} doesn't match count of {nameof(arrayObjects)}");
            }

            ArrayInfo = arrayInfo;
            ArrayObjects = arrayObjects;
        }

        IEnumerator<object> IEnumerable<object>.GetEnumerator() => ArrayObjects.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ArrayObjects.GetEnumerator();
    }
}
