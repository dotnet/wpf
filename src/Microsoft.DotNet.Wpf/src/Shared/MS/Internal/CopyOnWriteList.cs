// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;

#if WINDOWS_BASE
    using MS.Internal.WindowsBase;
#elif PRESENTATION_CORE
    using MS.Internal.PresentationCore;
#elif PRESENTATIONFRAMEWORK
    using MS.Internal.PresentationFramework;
#elif DRT
    using MS.Internal.Drt;
#else
#error Attempt to use FriendAccessAllowedAttribute from an unknown assembly.
using MS.Internal.YourAssemblyName;
#endif

namespace MS.Internal
{
    /// <summary>
    ///   This is a ThreadSafe ArrayList that uses Copy On Write to support consistency.
    ///   - When the "List" property is requested a readonly reference to the
    ///   list is returned and a reference to the readonly list is cached.
    ///   - If the "List" is requested again, the same cached reference is returned.
    ///   - When the list is modified, if a readonly reference is present in the
    ///   cache then the list is copied before it is modified and the readonly list is
    ///   released from the cache.
    /// </summary>
    [FriendAccessAllowed]
    internal class CopyOnWriteList
    {
        public CopyOnWriteList() : this(null)
        {
        }

        public CopyOnWriteList(object syncRoot)
        {
            if(syncRoot == null)
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
                ArrayList tempList;

                lock(_syncRoot)
                {
                    if(null == _readonlyWrapper)
                        _readonlyWrapper = ArrayList.ReadOnly(_LiveList);
                    tempList = _readonlyWrapper;
                }
                return tempList;
            }
        }

        /// <summary>
        ///   Add an object to the List.
        ///   Returns true if successfully added.
        ///   Returns false if object is already on the list.
        /// </summary>
        public virtual bool Add(object obj)
        {
            Debug.Assert(null!=obj, "CopyOnWriteList.Add() should not be passed null.");
            lock(_syncRoot)
            {
                int index = Find(obj);

                if(index >= 0)
                    return false;

                return Internal_Add(obj);
            }
        }

        /// <summary>
        ///   Remove an object from the List.
        ///   Returns true if successfully removed.
        ///   Returns false if object was not on the list.
        /// </summary>
        public virtual bool Remove(object obj)
        {
            Debug.Assert(null!=obj, "CopyOnWriteList.Remove() should not be passed null.");
            lock(_syncRoot)
            {
                int index = Find(obj);

                // If the object is not on the list then
                // we are done.  (return false)
                if(index < 0)
                    return false;

                return RemoveAt(index);
            }
        }

        /// <summary>
        ///   This allows derived classes to take the lock.  This is mostly used
        ///   to extend Add() and Remove() etc.
        /// </summary>
        protected object SyncRoot
        {
            get{ return _syncRoot; }
        }

        /// <summary>
        ///  This is protected and the caller can get into real serious trouble
        ///  using this.  Because this points at the real current list without
        ///  any copy on write protection.  So the caller must really know what
        ///  they are doing.
        /// </summary>
        protected ArrayList LiveList
        {
            get{ return _LiveList; }
        }

        /// <summary>
        ///   Add an object to the List.
        ///   Without any error checks.
        ///   For use by derived classes that implement there own error checks.
        /// </summary>
        protected bool Internal_Add(object obj)
        {
            DoCopyOnWriteCheck();
            _LiveList.Add(obj);
            return true;
        }

        /// <summary>
        ///   Insert an object into the List at the given index.
        ///   Without any error checks.
        ///   For use by derived classes that implement there own error checks.
        /// </summary>
        protected bool Internal_Insert(int index, object obj)
        {
            DoCopyOnWriteCheck();
            _LiveList.Insert(index, obj);
            return true;
        }

        /// <summary>
        ///   Find an object on the List.
        /// </summary>
        private int Find(object obj)
        {
            // syncRoot Lock MUST be held by the caller.
            for(int i = 0; i < _LiveList.Count; i++)
            {
                if(obj == _LiveList[i])
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        ///   Remove the object at a given index from the List.
        ///   Returns true if successfully removed.
        ///   Returns false if index is outside the range of the list.
        ///
        ///  This is protected because it operates on the LiveList
        /// </summary>
        protected bool RemoveAt(int index)
        {
            // syncRoot Lock MUST be held by the caller.
            if(index <0 || index >= _LiveList.Count )
                return false;

            DoCopyOnWriteCheck();
            _LiveList.RemoveAt(index);
            return true;
        }

        protected void DoCopyOnWriteCheck()
        {
            // syncRoot Lock MUST be held by the caller.
            // If we have exposed (given out) a readonly reference to this
            // version of the list, then clone a new internal copy and cut
            // the old version free.
            if(null != _readonlyWrapper)
            {
                _LiveList = (ArrayList)_LiveList.Clone();
                _readonlyWrapper = null;
            }
        }

        private object _syncRoot;
        private ArrayList _LiveList = new ArrayList();
        private ArrayList _readonlyWrapper;
    }
}
