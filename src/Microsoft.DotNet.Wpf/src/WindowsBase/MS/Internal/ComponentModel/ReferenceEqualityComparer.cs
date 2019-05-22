// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace MS.Internal.ComponentModel 
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    // This is a hashcode comparer we use to compare object references
    // rather than object equality.
    // 
    internal class ReferenceEqualityComparer : IEqualityComparer<object> 
    {
        bool IEqualityComparer<object>.Equals(object o1, object o2)
        {
            return object.ReferenceEquals(o1, o2);
        }

        public int GetHashCode(object o)
        {
            return o.GetHashCode();
        }
    }
}

