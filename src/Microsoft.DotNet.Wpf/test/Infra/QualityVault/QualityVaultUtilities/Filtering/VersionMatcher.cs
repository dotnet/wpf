// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace Microsoft.Test.Filtering
{
    internal class VersionMatcher
    {
        const string Separator = "=====";

        static VersionMatcher()
        {
            s_Empty = new VersionMatcher();
        }

        internal static VersionMatcher Merge(VersionMatcher m1, string directory, string file)
        {
            VersionMatcher m2 = (file == null) ? s_Empty : new VersionMatcher(Path.Combine(directory, file));
            return (m1 == null) ? m2 : m2.Merge(m1);
        }

        internal bool VersionMatches(Collection<string> versionRanges, string version)
        {
            if (versionRanges == null)
                return false;

            foreach (string range in versionRanges)
            {
                string id, low, high;
                switch (ParseRange(range, out id, out low, out high))
                {
                    case VersionRange.Single:
                        if (String.Equals(id, version, StringComparison.OrdinalIgnoreCase))
                            return true;
                        break;
                    case VersionRange.OpenHigh:
                        if (LessOrEqual(low, version))
                            return true;
                        break;
                    case VersionRange.Range:
                        if (LessOrEqual(low, version) && LessOrEqual(version, high))
                            return true;
                        break;
                    case VersionRange.OpenLow:
                        if (LessOrEqual(version, high))
                            return true;
                        break;
                }
            }

            return false;
        }

        private VersionMatcher()
        {
        }

        private VersionMatcher(string fullPath)
        {
            int idCount = 0;

            using (StreamReader sr = new StreamReader(fullPath))
            {
                string line;
                // skip to separator line
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith(Separator) && line.EndsWith(Separator))
                        break;
                }

                // load chains of IDs, one per line
                while ((line = sr.ReadLine()) != null)
                {
                    string[] a = line.Split('<');
                    int prevIndex = -1;
                    foreach (string s in a)
                    {
                        string id = s.Trim();
                        if (id.Length == 0)
                            continue;

                        if (!_dict.ContainsKey(id))
                        {
                            _dict.Add(id, new Tuple<int,List<int>>(idCount++, new List<int>()));
                        }

                        Tuple<int,List<int>> tuple = _dict[id];
                        int index = tuple.Item1;

                        // remember immediate predecessors
                        if (prevIndex >= 0)
                        {
                            tuple.Item2.Add(prevIndex);
                        }

                        prevIndex = index;
                    }
                }
            }

            // initialize the order matrix from the predecessor information
            _order = new bool[idCount, idCount];
            foreach (KeyValuePair<String,Tuple<int,List<int>>> kvp in _dict)
            {
                Tuple<int,List<int>> tuple = kvp.Value;
                int index = tuple.Item1;
                _order[index, index] = true;
                foreach (int predecessor in tuple.Item2)
                {
                    _order[predecessor, index] = true;
                }
            }

            // compute the transitive closure (Floyd-Warshall algorithm)
            for (int k=0; k<idCount; ++k)
            {
                for (int i=0; i<idCount; ++i)
                {
                    for (int j=0; j<idCount; ++j)
                    {
                        _order[i,j] = _order[i,j] || (_order[i,k] && _order[k,j]);
                    }
                }
            }
        }

        private VersionMatcher Merge(VersionMatcher that)
        {
            // merge the smaller into the larger
            if (this._dict.Count < that._dict.Count)
                return that.Merge(this);

            // trivial case - merge with empty collection
            if (that._dict.Count == 0)
                return this;

            // If we need a non-trivial merge:
            // 1. Add IDs from that._dict into this._dict
            // 2. If new IDs appeared, create a new (larger) _order and copy the old one
            // 3. Add that._order data to this._order
            // 4. Run Floyd-Warshall
            // 5. Think about checking for inconsistencies - if that defines order
            //      between IDs where this doesn't.
            return this;
        }

        private enum VersionRange { Single, OpenHigh, Range, OpenLow }

        private static VersionRange ParseRange(string range, out string id, out string low, out string high)
        {
            id = low = high = null;

            // look for '-' or '+', at most one
            string[] a = range.Split('-', '+');
            if (a.Length > 2) goto Error;

            if (a.Length == 1)
            {
                // no '-' or '+' in range, it's a single ID
                id = range.Trim();
                if (id.Length == 0) goto Error;
                return VersionRange.Single;
            }

            if (range.IndexOf('+') >= 0)
            {
                // range is "x+"
                if (a[1].Trim().Length > 0) goto Error;
                low = a[0].Trim();
                return VersionRange.OpenHigh;
            }

            // range is either "x - y" or "-y"
            low = a[0].Trim();
            high = a[1].Trim();
            if (high.Length == 0) goto Error;
            return (low.Length > 0) ? VersionRange.Range : VersionRange.OpenLow;

            Error:
            throw new ArgumentException("Invalid range: " + range, "range");
        }

        private bool LessOrEqual(string v1, string v2)
        {
            Tuple<int,List<int>> tuple;

            int index1 = _dict.TryGetValue(v1, out tuple) ? tuple.Item1 : -1;
            int index2 = _dict.TryGetValue(v2, out tuple) ? tuple.Item1 : -1;

            return (index1 >= 0 && index2 >= 0) ? _order[index1, index2]
                : String.Equals(v1, v2, StringComparison.OrdinalIgnoreCase);
        }

        Dictionary<String,Tuple<int,List<int>>> _dict   // maps ID -> (index, list of predecessors)
            = new Dictionary<String,Tuple<int,List<int>>>(StringComparer.OrdinalIgnoreCase);
        bool[,] _order;                 // _order[i,j] is true if i-th ID compares <= j-th ID
        static VersionMatcher s_Empty;
    }
}
