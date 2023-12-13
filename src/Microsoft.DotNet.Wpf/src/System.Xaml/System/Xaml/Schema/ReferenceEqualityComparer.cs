// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Xaml.Schema
{
    internal class ReferenceEqualityTuple<T1, T2> : Tuple<T1, T2>
    {
        public ReferenceEqualityTuple(T1 item1, T2 item2)
            : base(item1, item2)
        {
        }

        public override bool Equals(object obj)
        {
            return ((IStructuralEquatable)this).Equals(obj, ReferenceEqualityComparer.Instance);
        }

        public override int GetHashCode()
        {
            return ((IStructuralEquatable)this).GetHashCode(ReferenceEqualityComparer.Instance);
        }
    }

    internal class ReferenceEqualityTuple<T1, T2, T3> : Tuple<T1, T2, T3>
    {
        public ReferenceEqualityTuple(T1 item1, T2 item2, T3 item3)
            : base(item1, item2, item3)
        {
        }

        public override bool Equals(object obj)
        {
            return ((IStructuralEquatable)this).Equals(obj, ReferenceEqualityComparer.Instance);
        }

        public override int GetHashCode()
        {
            return ((IStructuralEquatable)this).GetHashCode(ReferenceEqualityComparer.Instance);
        }
    }
}
