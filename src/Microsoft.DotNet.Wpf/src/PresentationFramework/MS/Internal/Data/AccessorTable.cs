// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Mapping of (SourceValueType, type, name) to (info, propertyType, args)
//

/***************************************************************************\
    Data binding uses reflection to obtain accessors for source properties,
    where an "accessor" can be a DependencyProperty, a PropertyInfo, or a
    PropertyDescriptor, depending on the nature of the source item and the
    property.  We cache the result of this discovery process in the
    AccessorTable;  table lookup is cheaper than doing reflection again.
\***************************************************************************/

using System;
using System.Collections;
using System.ComponentModel;                // IBindingList
using System.Reflection;                    // TypeDescriptor
using System.Windows;                       // SR
using System.Windows.Threading;             // Dispatcher
using MS.Internal;                          // Invariant.Assert

namespace MS.Internal.Data
{
    internal sealed class AccessorInfo
    {
        internal AccessorInfo(object accessor, Type propertyType, object[] args)
        {
            _accessor = accessor;
            _propertyType = propertyType;
            _args = args;
        }

        internal object Accessor { get { return _accessor; } }
        internal Type PropertyType { get { return _propertyType; } }
        internal object[] Args { get { return _args; } }

        internal int Generation { get { return _generation; } set { _generation = value; } }

        object _accessor;          // DP, PD, or PI
        Type _propertyType;      // type of the property
        object[] _args;              // args for indexed property
        int _generation;        // used for discarding aged entries
    }


    internal sealed class AccessorTable
    {
        internal AccessorTable()
        {
        }

        // map (SourceValueType, type, name) to (accessor, propertyType, args)
        internal AccessorInfo this[SourceValueType sourceValueType, Type type, string name]
        {
            get
            {
                if (type == null || name == null)
                    return null;

                AccessorInfo info = (AccessorInfo)_table[new AccessorTableKey(sourceValueType, type, name)];

                if (info != null)
                {
#if DEBUG
                    // record the age of cache hits
                    int age = _generation - info.Generation;

                    if (age >= _ages.Length)
                    {
                        int[] newAges = new int[2*age];
                        _ages.CopyTo(newAges, 0);
                        _ages = newAges;
                    }

                    ++ _ages[age];
                    ++ _hits;
#endif
                    info.Generation = _generation;
                }
#if DEBUG
                else
                {
                    ++ _misses;
                }
#endif
                return info;
            }
            set
            {
                if (type != null && name != null)
                {
                    value.Generation = _generation;
                    _table[new AccessorTableKey(sourceValueType, type, name)] = value;

                    if (!_cleanupRequested)
                        RequestCleanup();
                }
            }
        }

        // request a cleanup pass
        private void RequestCleanup()
        {
            _cleanupRequested = true;
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new DispatcherOperationCallback(CleanupOperation), null);
        }

        // run a cleanup pass
        private object CleanupOperation(object arg)
        {
            // find entries that are sufficiently old
            object[] keysToRemove = new object[_table.Count];
            int n = 0;
            IDictionaryEnumerator ide = _table.GetEnumerator();
            while (ide.MoveNext())
            {
                AccessorInfo info = (AccessorInfo)ide.Value;
                int age = _generation - info.Generation;
                if (age >= AgeLimit)
                {
                    keysToRemove[n++] = ide.Key;
                }
            }

#if DEBUG
            if (_traceSize)
            {
                Console.WriteLine("After generation {0}, removing {1} of {2} entries from AccessorTable, new count is {3}",
                    _generation, n, _table.Count, _table.Count - n);
            }
#endif

            // remove those entries
            for (int i = 0; i < n; ++i)
            {
                _table.Remove(keysToRemove[i]);
            }

            ++_generation;

            _cleanupRequested = false;
            return null;
        }

        // print data about how the cache behaved
        internal void PrintStats()
        {
#if DEBUG
            if (_generation == 0 || _hits == 0)
            {
                Console.WriteLine("No stats available for AccessorTable.");
                return;
            }

            Console.WriteLine("AccessorTable had {0} hits, {1} misses ({2,2}%) in {3} generations.",
                        _hits, _misses, (100*_hits)/(_hits+_misses), _generation);
            Console.WriteLine("  Age   Hits   Pct   Cum");
            int cumulativeHits = 0;
            for (int i=0; i<_ages.Length; ++i)
            {
                if (_ages[i] > 0)
                {
                    cumulativeHits += _ages[i];
                    Console.WriteLine("{0,5} {1,6} {2,5} {3,5}",
                                    i, _ages[i], 100*_ages[i]/_hits, 100*cumulativeHits/_hits);
                }
            }
#endif
        }

        internal bool TraceSize
        {
            get { return _traceSize; }
            set { _traceSize = value; }
        }

        private const int AgeLimit = 10;      // entries older than this get removed.

        private Hashtable _table = new Hashtable();
        private int _generation;
        private bool _cleanupRequested;
        bool _traceSize;
#if DEBUG
        private int[]       _ages = new int[10];
        private int         _hits, _misses;
#endif

        private struct AccessorTableKey
        {
            public AccessorTableKey(SourceValueType sourceValueType, Type type, string name)
            {
                Invariant.Assert(type != null && type != null);

                _sourceValueType = sourceValueType;
                _type = type;
                _name = name;
            }

            public override bool Equals(object o)
            {
                if (o is AccessorTableKey)
                    return this == (AccessorTableKey)o;
                else
                    return false;
            }

            public static bool operator ==(AccessorTableKey k1, AccessorTableKey k2)
            {
                return k1._sourceValueType == k2._sourceValueType
                    && k1._type == k2._type
                    && k1._name == k2._name;
            }

            public static bool operator !=(AccessorTableKey k1, AccessorTableKey k2)
            {
                return !(k1 == k2);
            }

            public override int GetHashCode()
            {
                return unchecked(_type.GetHashCode() + _name.GetHashCode());
            }

            SourceValueType _sourceValueType;
            Type _type;
            string _name;
        }
    }
}

