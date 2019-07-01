// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing;
using System.Diagnostics;

namespace Microsoft.Test.EventTracing
{

    // Utilities for TraceEventParsers
    /// <summary>
    /// A HistoryDictionary is designed to look up 'handles' (pointer sized quantities), that might get reused
    /// over time (eg Process IDs, thread IDs).  Thus it takes a handle AND A TIME, and finds the value
    /// associated with that handles at that time.   
    /// </summary>
    public class HistoryDictionary<T>
    {
        // TODO need some methodical testing.  We don't actually get to see colisions of ids very often.  
        public HistoryDictionary(int initialSize)
        {
            entries = new Dictionary<long, HistoryValue>(initialSize);
        }
        [CLSCompliant(false)]
        public void Add(Address id, long startTime100ns, T value)
        {
            HistoryValue entry;
            if (!entries.TryGetValue((long)id, out entry))
                entries.Add((long)id, new HistoryValue(0, id, value));
            else
            {
                for (; ; )
                {
                    if (entry.next == null)
                    {
                        entry.next = new HistoryValue(startTime100ns, id, value);
                        break;
                    }

                    // We sort the entries from smallest to largest time. 
                    if (startTime100ns < entry.startTime100ns)
                    {
                        Debug.Assert(false);    // Note that we don't expect this to happen, we always add entries in time order.  

                        // This entry belongs in front of this entry.  
                        // Insert it before the current entry by moving the current entry after it.  
                        HistoryValue newEntry = new HistoryValue(entry);
                        entry.startTime100ns = startTime100ns;
                        entry.value = value;
                        entry.next = newEntry;
                        Debug.Assert(entry.startTime100ns <= entry.next.startTime100ns);
                        break;
                    }
                    entry = entry.next;
                }
            }
            count++;
        }
        [CLSCompliant(false)]
        public bool TryGetValue(Address id, long time100ns, out T value)
        {
            HistoryValue entry;
            if (entries.TryGetValue((long)id, out entry))
            {
                // The entries are shorted smallest to largest.  
                // We want the last entry that is smaller (or equal) to the target time) 
                HistoryValue last = null;
                for (; ; )
                {
                    if (time100ns < entry.startTime100ns)
                        break;
                    last = entry;
                    entry = entry.next;
                    if (entry == null)
                        break;
                }
                if (last != null)
                {
                    value = last.value;
                    return true;
                }
                Debug.Assert(false);
            }
            value = default(T);
            return false;
        }
        public IEnumerable<HistoryValue> Entries
        {
            get
            {
#if DEBUG
            int ctr = 0;
#endif
                foreach (HistoryValue entry in entries.Values)
                {
                    HistoryValue list = entry;
                    while (list != null)
                    {
#if DEBUG
                    ctr++;
#endif
                        yield return list;
                        list = list.next;
                    }
                }
#if DEBUG
            Debug.Assert(ctr == count);
#endif
            }
        }
        public int Count { get { return count; } }

        public class HistoryValue
        {
            public Address Key { get { return key; } }
            public long StartTime100ns { get { return startTime100ns; } }
            public T Value { get { return value; } }
            #region private
            internal HistoryValue(HistoryValue entry)
            {
                this.key = entry.key;
                this.startTime100ns = entry.startTime100ns;
                this.value = entry.value;
                this.next = entry.next;
            }
            internal HistoryValue(long startTime100ns, Address key, T value)
            {
                this.key = key;
                this.startTime100ns = startTime100ns;
                this.value = value;
            }

            internal Address key;
            internal long startTime100ns;
            internal T value;
            internal HistoryValue next;
            #endregion
        }
        #region private
        Dictionary<long, HistoryValue> entries;
        int count;
        #endregion
    }
}