// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Collections;

namespace Microsoft.Test.Win32
{
    /// <summary>
    ///   This is a Cached ThreadSafe ArrayList of WeakReferences.
    ///   - When the "List" property is requested a readonly reference to the
    ///   list is returned and a reference to the readonly list is cached.
    ///   - If the "List" is requested again, the same cached reference is returned.
    ///   - When the list is modified, if a readonly reference is present in the
    ///   cache then the list is copied before it is modified and the readonly list is
    ///   released from the cache.
    /// </summary>
    internal class WeakReferenceList : IEnumerable
    {
        public WeakReferenceList()
            : this(null)
        {
        }

        public WeakReferenceList(object syncRoot)
        {
            if (syncRoot == null)
            {
                syncRoot = new Object();
            }
            _syncRoot = syncRoot;
        }

        /// <summary>
        ///   Return a readonly wrapper of the list.  Note: this is NOT a copy.
        ///   A non-null _readonlyWrapper  is a "Copy on Write" flag.
        ///   Methods that change the list (eg. Add() and Remove()) are
        ///   responsible for:
        ///    1) Checking _readonlyWrapper and copying the list before modifing it.
        ///    2) Clearing _readonlyWrapper.
        /// </summary>
        public ArrayList List
        {
            get
            {
                ArrayList arrayListTemp = null;
                lock (_syncRoot)
                {
                    if (null == _readonlyWrapper)
                    {
                        _readonlyWrapper = ArrayList.ReadOnly(_LiveList);
                    }
                    arrayListTemp = _readonlyWrapper;
                }
                return arrayListTemp;
            }
        }

        public WeakReferenceListEnumerator GetEnumerator()
        {
            return new WeakReferenceListEnumerator(List);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///   Add a weak reference to the List.
        ///   Returns true if successfully added.
        ///   Returns false if object is already on the list.
        /// </summary>
        public bool Add(object obj)
        {
            if (null == obj)
                throw new ArgumentNullException("e");

            lock (_syncRoot)
            {
                CleanUpDeadReferences();    // have to do this somewhere
                int index = Find(obj);

                // If the object is already on the list then
                // we are done.  (return false)
                if (index >= 0)
                    return false;

                // If we have exposed (given out) a readonly reference to this
                // version of the list, then clone a new internal copy and strike the
                // old version.
                if (null != _readonlyWrapper)
                {
                    _LiveList = (ArrayList)_LiveList.Clone();
                    _readonlyWrapper = null;
                }
                _LiveList.Add(new WeakReference(obj));
                return true;
            }
        }

        /// <summary>
        ///   Remove a weak reference to the List.
        ///   Returns true if successfully added.
        ///   Returns false if object is already on the list.
        /// </summary>
        public bool Remove(object obj)
        {
            if (null == obj)
                throw new ArgumentNullException("e");

            lock (_syncRoot)
            {
                CleanUpDeadReferences();    // have to do this somewhere
                int index = Find(obj);

                // If the object is not on the list then
                // we are done.  (return false)
                if (index < 0)
                    return false;

                // If we have exposed (given out) a readonly reference to this
                // version of the list, then clone a new internal copy and strike the
                // old version.
                if (null != _readonlyWrapper)
                {
                    _LiveList = (ArrayList)_LiveList.Clone();
                    _readonlyWrapper = null;
                }
                _LiveList.RemoveAt(index);
                return true;
            }
        }

        private int Find(object obj)
        {
            WeakReference weakRef;

            // syncRoot Lock should be held by the caller.

            for (int i = 0; i < _LiveList.Count; i++)
            {
                weakRef = (WeakReference)_LiveList[i];

                if (weakRef.IsAlive)
                {
                    if ((object)obj == weakRef.Target)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        private void CleanUpDeadReferences()
        {
            WeakReference weakRef;

            // syncRoot Lock should be held by the caller.

            // Search the LiveList looking for any dead references.
            // Remove any dead references we find.
            for (int i = 0; i < _LiveList.Count; i++)
            {
                weakRef = (WeakReference)_LiveList[i];

                if (!weakRef.IsAlive)
                {
                    // Removing dead references is just like any other
                    // List changing operation.
                    if (null != _readonlyWrapper)
                    {
                        _LiveList = (ArrayList)_LiveList.Clone();
                        _readonlyWrapper = null;
                    }
                    _LiveList.RemoveAt(i);

                    // The ArrayList will copy-up to fill the Removed element
                    // so back up and do the same index again.
                    i -= 1;
                }
            }
        }

        private object _syncRoot;
        private ArrayList _LiveList = new ArrayList();
        private ArrayList _readonlyWrapper;
    }
}
