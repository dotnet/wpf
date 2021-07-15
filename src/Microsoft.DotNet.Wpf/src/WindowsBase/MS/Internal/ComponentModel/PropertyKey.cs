// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace MS.Internal.ComponentModel 
{
    using System;
    using System.Collections.Generic;
    using System.Windows;

    // This structure is used as a key in a dictionary of property key -> property descriptor
    // The key is unique based on the type the property is attached to, and the property
    // itself.
    internal struct PropertyKey : IEquatable<PropertyKey>
    {
        internal PropertyKey(Type attachedType, DependencyProperty prop) 
        {
            DependencyProperty = prop;
            AttachedType = attachedType;
            _hashCode = AttachedType.GetHashCode() ^ DependencyProperty.GetHashCode();
        }

        public override int GetHashCode() 
        {
            return _hashCode;
        }

        public override bool Equals(object obj) 
        {
            return Equals((PropertyKey)obj);
        }

        public bool Equals(PropertyKey key) 
        {
            return (key.AttachedType == AttachedType && key.DependencyProperty == DependencyProperty);
        }

        public static bool operator ==(PropertyKey key1, PropertyKey key2) 
        {
            return (key1.AttachedType == key2.AttachedType && key1.DependencyProperty == key2.DependencyProperty);
        }

        public static bool operator !=(PropertyKey key1, PropertyKey key2) 
        {
            return (key1.AttachedType != key2.AttachedType || key1.DependencyProperty != key2.DependencyProperty);
        }

        internal DependencyProperty DependencyProperty;
        internal Type AttachedType;
        private int _hashCode;
    }
}

