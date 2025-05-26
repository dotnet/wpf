// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;

namespace MS.Internal;

/// <summary>
/// This is a Cached ThreadSafe ArrayList of WeakReferences.
/// - When the "List" property is requested a readonly reference to the
/// list is returned and a reference to the readonly list is cached.
/// - If the "List" is requested again, the same cached reference is returned.
/// - When the list is modified, if a readonly reference is present in the
/// cache then the list is copied before it is modified and the readonly list is
/// released from the cache.
/// </summary>
internal sealed class WeakReferenceList<T> : CopyOnWriteList<WeakReference<T>>, IEnumerable<T> where T : class
{
    public int Count
    {
        get
        {
            lock (SyncRoot)
            {
                return LiveList.Count;
            }
        }
    }

    public WeakReferenceList(int capacity) : base(capacity)
    {
    }

    public WeakReferenceList(object? syncRoot) : base(syncRoot)
    {
    }

    public WeakReferenceListEnumerator<T> GetEnumerator()
    {
        return new WeakReferenceListEnumerator<T>(List);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Contains(T item)
    {
        Debug.Assert(item is not null, "WeakReferenceList.Contains() should not be passed null.");

        lock (SyncRoot)
        {
            int index = FindWeakReference(item);

            // If the object is already on the list then
            // return true
            if (index >= 0)
                return true;

            return false;
        }
    }

    /// <summary>
    /// Add a weak reference to the List.
    /// Returns true if successfully added.
    /// Returns false if object is already on the list.
    /// </summary>
    public bool Add(T obj)
    {
        Debug.Assert(obj is not null, "WeakReferenceList.Add() should not be passed null.");

        return Add(obj, skipFind: false);
    }

    //Will insert a new WeakReference into the list.
    //The object bein inserted MUST be unique as there is no check for it.
    public bool Add(T obj, bool skipFind)
    {
        Debug.Assert(obj is not null, "WeakReferenceList.Add() should not be passed null.");

        lock (SyncRoot)
        {
            if (skipFind)
            {
                // before growing the list, purge it of dead entries.
                // The expense of purging amortizes to O(1) per entry, because
                // the list doubles its capacity when it grows.
                if (LiveList.Count == LiveList.Capacity)
                {
                    Purge();
                }
            }
            else if (FindWeakReference(obj) >= 0)
            {
                return false;
            }

            return Internal_Add(new WeakReference<T>(obj));
        }
    }

    /// <summary>
    /// Remove a weak reference to the List.
    /// Returns true if successfully removed.
    /// Returns false if object is not in the list.
    /// </summary>
    public bool Remove(T obj)
    {
        Debug.Assert(obj is not null, "WeakReferenceList.Remove() should not be passed null.");

        lock (SyncRoot)
        {
            int index = FindWeakReference(obj);

            // If the object is not on the list then
            // we are done.  (return false)
            if (index < 0)
                return false;

            return RemoveAt(index);
        }
    }

    /// <summary>
    /// Insert a weak reference into the List.
    /// Returns true if successfully inserted.
    /// Returns false if object is already on the list.
    /// </summary>
    public bool Insert(int index, T obj)
    {
        Debug.Assert(obj is not null, "WeakReferenceList.Add() should not be passed null.");

        lock (SyncRoot)
        {
            int existingIndex = FindWeakReference(obj);

            // If the object is already on the list then
            // we are done.  (return false)
            if (existingIndex >= 0)
                return false;

            return Internal_Insert(index, new WeakReference<T>(obj));
        }
    }

    /// <summary>
    /// Find an object on the List.
    /// Also cleans up dead weak references.
    /// </summary>
    private int FindWeakReference(T obj)
    {
        // syncRoot Lock MUST be held by the caller.
        // Search the LiveList looking for the object, also remove any
        // dead references we find.
        //
        // We use the "LiveList" to avoid snapping a Clone every time we
        // Change something.
        // To do this correctly you need to understand how the base class
        // virtualizes the Copy On Write.
        bool foundDeadReferences = true;   // so that the while loop runs the first time
        int foundItem = -1;

        while (foundDeadReferences)
        {
            foundDeadReferences = false;
            List<WeakReference<T>> list = LiveList;

            for (int i = 0; i < list.Count; i++)
            {
                WeakReference<T> weakRef = list[i];
                if (weakRef.TryGetTarget(out T? target))
                {
                    if (obj == target)
                    {
                        foundItem = i;
                        break;
                    }
                }
                else
                {
                    foundDeadReferences = true;
                }
            }

            if (foundDeadReferences)
            {
                // if there were dead references, take this opportunity
                // to clean up _all_ the dead references.  After doing this,
                // the foundItem index is no longer valid, so we just
                // compute it again.
                // Most of the time we expect no dead references, so the while
                // loop runs once and the for loop walks the list once.
                // Occasionally there will be dead references - the while loop
                // runs twice and the for loop walks the list twice.  Purge
                // also walks the list once, for a total of three times.
                Purge();
            }
        }

        return foundItem;
    }

    // purge the list of dead references
    // caller is expected to lock the SyncRoot
    private void Purge()
    {
        List<WeakReference<T>> list = LiveList;
        int destIndex;
        int n = list.Count;

        // skip over valid entries at the beginning of the list
        for (destIndex = 0; destIndex < n; ++destIndex)
        {
            WeakReference<T> wr = list[destIndex];
            if (!wr.TryGetTarget(out _))
                break;
        }

        // there may be nothing to purge
        if (destIndex >= n)
            return;

        // but if there is, check for copy-on-write
        DoCopyOnWriteCheck();
        list = LiveList;

        // move remaining valid entries toward the beginning, into one
        // contiguous block
        for (int i = destIndex + 1; i < n; ++i)
        {
            WeakReference<T> wr = list[i];
            if (wr.TryGetTarget(out _))
            {
                list[destIndex++] = list[i];
            }
        }

        // remove the remaining entries and shrink the list
        if (destIndex < n)
        {
            list.RemoveRange(destIndex, n - destIndex);

            // shrink the list if it would be less than half full otherwise.
            // This is more liberal than List<T>.TrimExcess(), because we're
            // probably in the situation where additions to the list are common.
            int newCapacity = destIndex << 1;
            if (newCapacity < list.Capacity)
            {
                list.Capacity = newCapacity;
            }
        }
    }
}

