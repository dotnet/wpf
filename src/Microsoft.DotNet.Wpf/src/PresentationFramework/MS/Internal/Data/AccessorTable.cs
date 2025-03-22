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

using System.Windows.Threading;             // Dispatcher

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

        private readonly object _accessor;   // DP, PD, or PI
        private readonly Type _propertyType; // type of the property
        private readonly object[] _args;     // args for indexed property
        private int _generation;             // used for discarding aged entries
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

                if (_table.TryGetValue(new AccessorTableKey(sourceValueType, type, name), out AccessorInfo info))
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
#if DEBUG
            int originalCount = _table.Count;
#endif

            // Remove entries that are sufficiently old
            foreach (KeyValuePair<AccessorTableKey, AccessorInfo> entry in _table)
            {
                int age = _generation - entry.Value.Generation;
                if (age >= AgeLimit)
                {
                    _table.Remove(entry.Key);
                }
            }

#if DEBUG
            if (_traceSize)
            {
                Console.WriteLine($"After generation {_generation}, removed {originalCount - _table.Count} of {originalCount} entries from AccessorTable, new count is {_table.Count}");
            }
#endif

            ++_generation;

            _cleanupRequested = false;
            return null;
        }

        // print data about how the cache behaved
        [Conditional("DEBUG")]
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
            Console.WriteLine("  Age   Hits   Pct   Cumulative");
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

        private readonly Dictionary<AccessorTableKey, AccessorInfo> _table = new Dictionary<AccessorTableKey, AccessorInfo>();
        private int _generation;
        private bool _cleanupRequested;
        private bool _traceSize;
#if DEBUG
        private int[]       _ages = new int[10];
        private int         _hits, _misses;
#endif

        private readonly struct AccessorTableKey : IEquatable<AccessorTableKey>
        {
            public AccessorTableKey(SourceValueType sourceValueType, Type type, string name)
            {
                Invariant.Assert(type != null);

                _sourceValueType = sourceValueType;
                _type = type;
                _name = name;
            }

            public override bool Equals(object o) => o is AccessorTableKey other && Equals(other);

            public bool Equals(AccessorTableKey other) =>
                _sourceValueType == other._sourceValueType
                && _type == other._type
                && _name == other._name;

            public static bool operator ==(AccessorTableKey k1, AccessorTableKey k2) => k1.Equals(k2);

            public static bool operator !=(AccessorTableKey k1, AccessorTableKey k2) => !k1.Equals(k2);

            public override int GetHashCode() => unchecked(_type.GetHashCode() + _name.GetHashCode());

            private readonly SourceValueType _sourceValueType;
            private readonly Type _type;
            private readonly string _name;
        }
    }
}
