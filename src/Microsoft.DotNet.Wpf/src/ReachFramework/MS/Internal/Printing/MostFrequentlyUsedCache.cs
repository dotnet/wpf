// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



namespace MS.Internal.Printing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    
    internal class MostFrequentlyUsedCache<K, V>
    {
        public MostFrequentlyUsedCache(int maxEntries)
        {
            if(maxEntries <= 0)
            {
                throw new ArgumentOutOfRangeException("maxEntries", maxEntries, string.Empty);
            }
            
            this._dictionary = new Dictionary<K, Entry>(maxEntries);
            this._maxEntries = maxEntries;
        }
        
        public void CacheValue(K key, V value)
        {
            Entry entry;
            if(!this._dictionary.TryGetValue(key, out entry))
            {
                entry = new Entry(value);
                // Trim the dictionary to make sure it does not exceed
                // maxEntries entries
                while(this._dictionary.Count >= this._maxEntries)
                {
                    RemoveLeastFrequentlyUsedEntry();
                }
                
                Debug.Assert(this._dictionary.Count <= this._maxEntries);
                
                this._dictionary.Add(key, entry);
            }
            else
            {
                entry.Value = value;
                entry.UseCount = 0;
            }
        }
        
        public bool TryGetValue(K key, out V value)
        {
            Entry entry;
            if(this._dictionary.TryGetValue(key, out entry))
            {
                value = entry.Value;
                entry.UseCount++;
                return true;
            }
            
            value = default(V);
            return false;
        }
        
        private void RemoveLeastFrequentlyUsedEntry()
        {
            int minUseCount = int.MaxValue;
            K minUseCountKey = default(K);
            bool keyFound = false;
            
            foreach(KeyValuePair<K, Entry> pair in this._dictionary)
            {
                if(pair.Value.UseCount < minUseCount)
                {
                    minUseCount = pair.Value.UseCount;
                    minUseCountKey = pair.Key;
                    keyFound = true;
                }
            }
            
            if(keyFound)
            {
                this._dictionary.Remove(minUseCountKey);
            }
        }

        class Entry {
            public Entry(V value)
            {
                Value = value;
            }
            
            public V Value;
            public int UseCount;
        }
        
        
        private readonly IDictionary<K, Entry> _dictionary;
        private readonly int _maxEntries;
    }
}