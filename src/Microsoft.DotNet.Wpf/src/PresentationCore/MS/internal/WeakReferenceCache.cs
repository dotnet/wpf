// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace MS.Internal;

/// <summary>
/// Provides keyed-caching for different objects stored as <see cref="WeakReference{V}"/>.
/// </summary>
/// <remarks>The cache operations are thread-safe.</remarks>
internal sealed class WeakReferenceCache<K, V>
    where K : notnull
    where V : class
{
    /// <summary>
    /// Max number of elements stored in the cache.
    /// </summary>
    private const int MaxCacheSize = 300;

    private readonly Dictionary<K, WeakReference<V>> _cacheStore = new();
    private readonly Lock _cacheLock = new();

    /// <summary>
    /// Adds an object to the cache.
    /// </summary>
    public void Add(K key, V value)
    {
        lock (_cacheLock)
        {
            // if entry is already there, exit
            if (_cacheStore.ContainsKey(key))
            {
                return;
            }

            // if the table has reached the max size, try to see if we can reduce its size
            if (_cacheStore.Count == MaxCacheSize)
            {
                foreach (KeyValuePair<K, WeakReference<V>> item in _cacheStore)
                {
                    // if the value is a WeakReference that has been GC'd, remove it
                    if (!item.Value.TryGetTarget(out _))
                    {
                        _cacheStore.Remove(item.Key);
                    }
                }
            }

            // if table is still maxed out, exit
            if (_cacheStore.Count == MaxCacheSize)
            {
                return;
            }

            // add it
            _cacheStore.Add(key, new WeakReference<V>(value));
        }
    }

    /// <summary>
    /// Removes an object from the cache.
    /// </summary>
    public void Remove(K key)
    {
        lock (_cacheLock)
        {
            // if entry is there, remove it
            _cacheStore.Remove(key);
        }
    }

    /// <summary>
    /// Attempts to retrieve an object from the cache.
    /// </summary>
    /// <returns>Returns <see langword="true"/> in case the object exists in the cache and was retrieved alive, otherwise returns <see langword="false"/>.</returns>
    /// <remarks>In case the <see cref="WeakReference{V}"/> was already collected, the entry is removed from the cache.</remarks>
    public bool TryGetValue(K key, [NotNullWhen(true)] out V? value)
    {
        lock (_cacheLock)
        {
            if (!_cacheStore.TryGetValue(key, out WeakReference<V>? weakRef))
            {
                value = null;
                return false;
            }

            return weakRef.TryGetTarget(out value) || !_cacheStore.Remove(key);
        }
    }
}
