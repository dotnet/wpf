// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace MS.Internal.ComponentModel 
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    // This is a hashcode comparer we use to key property descriptors.  We
    // want property descriptors keyed off of reference equality.
    // 
    internal class PropertyDescriptorComparer : IEqualityComparer<PropertyDescriptor> 
    {
        public bool Equals(PropertyDescriptor p1, PropertyDescriptor p2)
        {
            return object.ReferenceEquals(p1, p2);
        }

        public int GetHashCode(PropertyDescriptor p)
        {
            return p.GetHashCode();
        }
    }
}

