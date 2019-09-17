// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// This is a variant of WeakHashtable that works when the keys are value-types.
// In particular:
//  1. Do not create weak references to a value-type key.  That makes no sense -
//      the GC system doesn't manage the key itself, but only boxes holding the
//      key.  The lifetime of a particular box is irrelevant, and the "lifetime"
//      concept doesn't apply to a value-type.
//  2. Use value-semantics in equality tests.  (Object.Equals, not Object.ReferenceEquals).
//      Reference-semantics tests whether the two boxes are the same, which
//      is largely a coincidence of how the boxes arrived here.
//  3. A few small perf or style improvements over the WeakHashtable code.

namespace MS.Internal
{
    using System;
    using System.Collections;

    /// <devdoc>
    ///     This is a hashtable that stores object keys as weak references.
    ///     It monitors memory usage and will periodically scavenge the
    ///     hash table to clean out dead references.
    /// </devdoc>
    internal sealed class WeakObjectHashtable : Hashtable, IWeakHashtable
    {
        private static IEqualityComparer _comparer = new WeakKeyComparer();

        private long _lastGlobalMem;
        private int _lastHashCount;

        internal WeakObjectHashtable()
            : base(_comparer)
        {
        }

        /// <devdoc>
        ///     Override of Item that wraps a weak reference around the
        ///     key and performs a scavenge.
        /// </devdoc>
        public void SetWeak(object key, object value)
        {
            ScavengeKeys();
            WrapKey(ref key);
            this[key] = value;
        }

        private void WrapKey(ref object key)
        {
            if (key != null && !key.GetType().IsValueType)
            {
                key = new EqualityWeakReference(key);
            }
        }

        public object UnwrapKey(object key)
        {
            EqualityWeakReference keyRef = key as EqualityWeakReference;
            return (keyRef != null) ? keyRef.Target : key;
        }

        /// <devdoc>
        ///     This method checks to see if it is necessary to
        ///     scavenge keys, and if it is it performs a scan
        ///     of all keys to see which ones are no longer valid.
        ///     To determine if we need to scavenge keys we need to
        ///     try to track the current GC memory.  Our rule of
        ///     thumb is that if GC memory is decreasing and our
        ///     key count is constant we need to scavenge.  We
        ///     will need to see if this is too often for extreme
        ///     use cases like the CompactFramework (they add
        ///     custom type data for every object at design time).
        /// </devdoc>
        private void ScavengeKeys()
        {
            int hashCount = Count;

            if (hashCount == 0)
            {
                return;
            }

            if (_lastHashCount == 0)
            {
                _lastHashCount = hashCount;
                return;
            }

            long globalMem = GC.GetTotalMemory(false);

            if (_lastGlobalMem == 0)
            {
                _lastGlobalMem = globalMem;
                return;
            }

            long memDelta = globalMem - _lastGlobalMem;
            long hashDelta = hashCount - _lastHashCount;

            if (memDelta < 0 && hashDelta >= 0)
            {
                // Perform a scavenge through our keys, looking
                // for dead references.
                ArrayList cleanupList = null;
                foreach (object o in Keys)
                {
                    EqualityWeakReference wr = o as EqualityWeakReference;
                    if (wr != null && !wr.IsAlive)
                    {
                        if (cleanupList == null)
                        {
                            cleanupList = new ArrayList();
                        }

                        cleanupList.Add(wr);
                    }
                }

                if (cleanupList != null)
                {
                    foreach (object o in cleanupList)
                    {
                        Remove(o);
                    }
                }
            }

            _lastGlobalMem = globalMem;
            _lastHashCount = hashCount;
        }

        private class WeakKeyComparer : IEqualityComparer
        {
            bool IEqualityComparer.Equals(object x, object y)
            {
                if (x == null)
                {
                    return y == null;
                }

                if (y == null || x.GetHashCode() != y.GetHashCode())
                {
                    return false;
                }

                if (object.ReferenceEquals(x, y))
                {
                    return true;
                }

                EqualityWeakReference wX, wY;

                if ((wX = x as EqualityWeakReference) != null)
                {
                    x = wX.Target;
                    if (x == null)
                    {
                        // if a reference-type key has been GC'd, the weak-ref
                        // wrapper can only match itself.  We've already checked
                        // that via ReferenceEquals.
                        return false;
                    }
                }

                if ((wY = y as EqualityWeakReference) != null)
                {
                    y = wY.Target;
                    if (y == null)
                    {
                        // if a reference-type key has been GC'd, the weak-ref
                        // wrapper can only match itself.  We've already checked
                        // that via ReferenceEquals.
                        return false;
                    }
                }

                return object.Equals(x, y);
            }

            int IEqualityComparer.GetHashCode(object obj)
            {
                return obj.GetHashCode();
            }
        }

        /// <devdoc>
        ///     A wrapper of WeakReference that overrides GetHashCode and
        ///     Equals so that the weak reference returns the same equality
        ///     semantics as the object it wraps.  This will always return
        ///     the object's hash code and will return True for a Equals
        ///     comparison of the object it is wrapping.  If the object
        ///     it is wrapping has finalized, Equals always returns false.
        /// </devdoc>
        internal sealed class EqualityWeakReference
        {
            private int _hashCode;
            private WeakReference _weakRef;

            internal EqualityWeakReference(object o)
            {
                _weakRef = new WeakReference(o);
                _hashCode = o.GetHashCode();
            }

            public bool IsAlive
            {
                get { return _weakRef.IsAlive; }
            }

            public object Target
            {
                get { return _weakRef.Target; }
            }

            public override bool Equals(object o)
            {
                if (o == null)
                {
                    return false;
                }

                if (o.GetHashCode() != _hashCode)
                {
                    return false;
                }

                if (o == this || object.ReferenceEquals(o, Target))
                {
                    return true;
                }

                return false;
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }
        }
    }
}

