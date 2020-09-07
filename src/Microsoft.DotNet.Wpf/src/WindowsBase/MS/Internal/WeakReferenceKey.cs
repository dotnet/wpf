// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  This file defines a class that holds a weak reference to an object.  It preserves the hashcode 
//  of the object and is intended to be used as a key in hashtables or dictionaries.
//


using System;
using MS.Internal;

namespace MS.Internal.Utility
{
	/// <summary>
    /// Helper class that allows using a weak reference to an item as a key in a hash table.
    /// </summary>
    internal class WeakReferenceKey<T>
    {
        public WeakReferenceKey(T item)
        {
            Invariant.Assert(item != null);

            _item = new WeakReference(item);
            _hashCode = item.GetHashCode();
        }

        public T Item
        {
            get { return (T)_item.Target; }
        }

        public override bool Equals(object o)
        {
            if (o == this)
                return true;

            WeakReferenceKey<T> key = o as WeakReferenceKey<T>;
            if (key != null)
            {
                T item = this.Item;
                
                if (item == null)
                    return false;   // a stale key matches nothing (except itself)

                return this._hashCode == key._hashCode &&
                        Object.Equals(item, key.Item);
            }

            return false; 
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        private WeakReference _item;
        private int _hashCode;
    }
}