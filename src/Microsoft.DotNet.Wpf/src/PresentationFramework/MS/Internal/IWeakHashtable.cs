// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Collections;

namespace MS.Internal
{
    internal interface IWeakHashtable
    {
        // Hashtable members
        object this[object key] { get; }
        ICollection Keys { get; }
        int Count { get; }
        bool ContainsKey(object key);
        void Remove(object key);
        void Clear();

        // Additional members needed for WeakHashtable usage
        void SetWeak(object key, object value);
        object UnwrapKey(object key);
    }
}
