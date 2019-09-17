// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description: TypefaceMetricsCache
//
//

using System;
using System.Collections;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

using MS.Internal.FontFace;
using MS.Internal.Shaping;

namespace MS.Internal.FontCache
{
    /// <summary>
    /// TypefaceMetricsCache caches managed objects related to a Font's realization. It caches the 3 kinds of 
    /// key-value pairs currently: 
    /// o Friendly name        - canonical name
    /// o FontFamilyIdentifier - First IFontFamily
    /// o Typeface             - CachedTypeface
    ///
    /// The cache lives in managed space to save working set by allowing multiple instances of FontFamily 
    /// and Typeface to share the same IFontFamily and ITypefaceMetrics object. 
    /// For example: in MSNBAML, there are 342 typeface objects and they are canonicalized to only 5 
    /// ITypefaceMetrics.
    ///
    /// When cache is full, a new instance of the hashtable will be created and the old one will be discarded.
    /// Hence, it is important that the cached object do not keep a pointer to the hashtable to ensure obsolete cached 
    /// values are properly GC'ed.
    /// </summary>
    internal static class TypefaceMetricsCache
    {
        /// <summary>
        /// Readonly lookup from the cache.
        /// </summary>
        internal static object ReadonlyLookup(object key)
        {
            return _hashTable[key];
        }

        /// <summary>
        /// The method adds values into the cache. It uses lock to synchronize access.
        /// </summary>
        internal static void Add(object key, object value)
        {
            // Hashtable allows for one writer and multiple reader at the same time. So we don't have
            // read-write confict. In heavy threading environment, the worst is adding 
            // the same value more than once. 
            lock(_lock)
            {
                if (_hashTable.Count >= MaxCacheCapacity)
                {
                    // when cache is full, we just renew the cache.
                    _hashTable = new Hashtable(MaxCacheCapacity);
                }
                
                _hashTable[key] = value;
            }
        }
   
        private static Hashtable _hashTable = new Hashtable(MaxCacheCapacity);
        private static object _lock         = new object();        
        private const int MaxCacheCapacity  = 64;   // Maximum cache capacity
    }        
}
