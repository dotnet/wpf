// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace MS.Internal;

/// <summary>
/// This is a ThreadSafe <see cref="List{T}"/> that uses Copy On Write to support consistency.
/// - When the "List" property is requested a readonly reference to the
/// list is returned and a reference to the readonly list is cached.
/// - If the "List" is requested again, the same cached reference is returned.
/// - When the list is modified, if a readonly reference is present in the
/// cache then the list is copied before it is modified and the readonly list is
/// released from the cache.
/// </summary>
internal abstract class CopyOnWriteList<T> where T : class
{
    private ReadOnlyCollection<T>? _readonlyWrapper;
    private List<T> _liveList;

    private readonly object _syncRoot;

    /// <summary>
    /// Creates a new WeakReferenceList with the default capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity of the list.</param>
    public CopyOnWriteList(int capacity)
    {
        _syncRoot = new object();
        _liveList = new List<T>(capacity);
    }

    /// <summary>
    /// Creates a new CopyOnWriteList with the specified sync root.
    /// </summary>
    /// <param name="syncRoot">The synchronization object used to manage access to the list.</param>
    /// <remarks>In case <paramref name="syncRoot"/> is <see langword="null"/>, a private instance will be created.</remarks>
    public CopyOnWriteList(object? syncRoot)
    {
        syncRoot ??= new object();

        _syncRoot = syncRoot;
        _liveList = new List<T>();
    }

    /// <summary>
    /// Return a readonly wrapper of the list.
    /// A non-null _readonlyWrapper is a "Copy on Write" flag.
    /// Methods that change the list (eg. Add() and Remove()) are responsible for:
    ///  1) Checking _readonlyWrapper and copying the list before modifying it.
    ///  2) Clearing _readonlyWrapper.
    /// </summary>
    /// <remarks>Note: This is NOT a copy.</remarks>
    public ReadOnlyCollection<T> List
    {
        get
        {
            ReadOnlyCollection<T> tempList;

            lock (_syncRoot)
            {
                _readonlyWrapper ??= _liveList.AsReadOnly();

                tempList = _readonlyWrapper;
            }

            return tempList;
        }
    }

    /// <summary>
    /// Add an object to the List.
    /// Returns true if successfully added.
    /// Returns false if object is already on the list.
    /// </summary>
    protected bool Add(T obj)
    {
        Debug.Assert(obj is not null, "CopyOnWriteList.Add() should not be passed null.");

        lock (_syncRoot)
        {
            int index = Find(obj);

            return index < 0 && Internal_Add(obj);
        }
    }

    /// <summary>
    /// Remove an object from the List.
    /// Returns true if successfully removed.
    /// Returns false if object was not on the list.
    /// </summary>
    protected bool Remove(T obj)
    {
        Debug.Assert(obj is not null, "CopyOnWriteList.Remove() should not be passed null.");
        lock (_syncRoot)
        {
            int index = Find(obj);

            // If the object is not on the list then we are done.
            return index >= 0 && RemoveAt(index);
        }
    }

    /// <summary>
    /// This allows derived classes to take the lock.  This is mostly used
    /// to extend Add() and Remove() etc.
    /// </summary>
    protected object SyncRoot
    {
        get => _syncRoot;
    }

    /// <summary>
    /// This is protected and the caller can get into real serious trouble
    /// using this.  Because this points at the real current list without
    /// any copy on write protection.  So the caller must really know what
    /// they are doing.
    /// </summary>
    protected List<T> LiveList
    {
        get => _liveList;
    }

    /// <summary>
    /// Add an object to the List.
    /// Without any error checks.
    /// For use by derived classes that implement their own error checks.
    /// </summary>
    protected bool Internal_Add(T obj)
    {
        DoCopyOnWriteCheck();

        _liveList.Add(obj);

        return true;
    }

    /// <summary>
    /// Insert an object into the List at the given index.
    /// Without any error checks.
    /// For use by derived classes that implement there own error checks.
    /// </summary>
    protected bool Internal_Insert(int index, T obj)
    {
        DoCopyOnWriteCheck();

        _liveList.Insert(index, obj);

        return true;
    }

    /// <summary>
    /// Find an object on the List.
    /// </summary>
    private int Find(T obj)
    {
        // syncRoot Lock MUST be held by the caller.
        for (int i = 0; i < _liveList.Count; i++)
        {
            if (obj == _liveList[i])
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Remove the object at a given index from the List.
    /// Returns true if successfully removed.
    /// Returns false if index is outside the range of the list.
    /// </summary>
    /// <remarks>This is protected because it operates on the <see cref="LiveList"/>.</remarks>
    protected bool RemoveAt(int index)
    {
        // syncRoot Lock MUST be held by the caller.
        if ((uint)index >= (uint)_liveList.Count)
            return false;

        DoCopyOnWriteCheck();

        _liveList.RemoveAt(index);

        return true;
    }

    /// <remarks>
    /// This must be called under <see cref="SyncRoot"/> lock held.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void DoCopyOnWriteCheck()
    {
        // If we have exposed (given out) a readonly reference to this version of the list,
        // then clone a new internal copy and cut the old version free.
        if (_readonlyWrapper is not null)
        {
            _liveList = new List<T>(_liveList);
            _readonlyWrapper = null;
        }
    }
}
