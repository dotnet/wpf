// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            if (o is WeakReferenceKey<T> key)
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
