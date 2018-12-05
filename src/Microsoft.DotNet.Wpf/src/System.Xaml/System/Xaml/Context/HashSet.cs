// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace System.Xaml.Context
{
    internal class HashSet<T> : Dictionary<T, object>
    {
        public HashSet()
            : base()
        {
        }

        public HashSet(IDictionary<T, object> other)
            : base(other)
        {
        }

        public HashSet(IEqualityComparer<T> comparer)
            : base(comparer)
        {
        }

        public void Add(T item)
        {
            Add(item, null);
        }
    }
}
