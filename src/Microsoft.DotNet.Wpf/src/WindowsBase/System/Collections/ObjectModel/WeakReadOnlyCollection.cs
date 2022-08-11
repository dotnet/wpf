// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



// This is basically copied from ReadOnlyCollection<T>, with just enough modification
// to support storage of items as WeakReferences.  Use of internal BCL features
// is commented out.

// Most of the O(N) operations - Contains, IndexOf, etc. -
// are implemented by simply exploding the base list into a List<T>.  This isn't
// particularly efficient, but we don't expect to use these methods very often.

// The important scenario is one where we need WR not because we expect the targets
// to die (we don't), but because we need to avoid creating cycles containing the
// WeakReadOnlyCollection.  Thus we don't check for WR.Target==null.

namespace System.Collections.ObjectModel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using MS.Internal.WindowsBase;      // [FriendAccessAllowed]

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(false)]
    //[DebuggerTypeProxy(typeof(Mscorlib_CollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    [FriendAccessAllowed]
    internal class WeakReadOnlyCollection<T>: IList<T>, IList
    {
        //IList<T> list;
        IList<WeakReference> list;
        [NonSerialized]
        private Object _syncRoot;

        public WeakReadOnlyCollection(IList<WeakReference> list) {  // assumption: the WRs in list refer to T's
            if (list == null) {
                throw new ArgumentNullException(nameof(list));
            }
            this.list = list;
        }

        public int Count {
            get { return list.Count; }
        }

        public T this[int index] {
            //get { return list[index]; }
            get { return (T)list[index].Target; }
        }

        public bool Contains(T value) {
            //return list.Contains(value);
            return CreateDereferencedList().Contains(value);
        }

        public void CopyTo(T[] array, int index) {
            //list.CopyTo(array, index);
            CreateDereferencedList().CopyTo(array, index);
        }

        public IEnumerator<T> GetEnumerator() {
            //return list.GetEnumerator();
            return new WeakEnumerator(list.GetEnumerator());
        }

        public int IndexOf(T value) {
            //return list.IndexOf(value);
            return CreateDereferencedList().IndexOf(value);
        }

        /*
        protected IList<T> Items {
            get {
                return list;
            }
        }
        */

        bool ICollection<T>.IsReadOnly {
            get { return true; }
        }

        T IList<T>.this[int index] {
            //get { return list[index]; }
            get { return (T)list[index].Target; }
            set {
                //ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
                throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            }
        }

        void ICollection<T>.Add(T value) {
            //ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        void ICollection<T>.Clear() {
            //ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        void IList<T>.Insert(int index, T value) {
            //ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        bool ICollection<T>.Remove(T value) {
            //ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            //return false;
        }

        void IList<T>.RemoveAt(int index) {
            //ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            //return ((IEnumerable)list).GetEnumerator();
            return new WeakEnumerator(((IEnumerable)list).GetEnumerator());
        }

        bool ICollection.IsSynchronized {
            get { return false; }
        }

        object ICollection.SyncRoot {
            get {
                if( _syncRoot == null) {
                    ICollection c = list as ICollection;
                    if( c != null) {
                        _syncRoot = c.SyncRoot;
                    }
                    else {
                        System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
                    }
                }
                return _syncRoot;
            }
        }

        void ICollection.CopyTo(Array array, int index) {
            if (array == null) {
                //ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != 1) {
                //ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported);
            }

            if( array.GetLowerBound(0) != 0 ) {
                //ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NonZeroLowerBound);
                throw new ArgumentException(SR.Arg_NonZeroLowerBound);
            }

            if (index < 0) {
                //ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.arrayIndex, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (array.Length - index < Count) {
                //ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);
            }

            IList<T> dlist = CreateDereferencedList();
            T[] items = array as T[];
            if (items != null) {
                dlist.CopyTo(items, index);
            }
            else {
                //
                // Catch the obvious case assignment will fail.
                // We can found all possible problems by doing the check though.
                // For example, if the element type of the Array is derived from T,
                // we can't figure out if we can successfully copy the element beforehand.
                //
                Type targetType = array.GetType().GetElementType();
                Type sourceType = typeof(T);
                if(!(targetType.IsAssignableFrom(sourceType) || sourceType.IsAssignableFrom(targetType))) {
                    //ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
                    throw new ArgumentException(SR.Argument_InvalidArrayType);
                }

                //
                // We can't cast array of value type to object[], so we don't support
                // widening of primitive types here.
                //
                object[] objects = array as object[];
                if( objects == null) {
                    //ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
                    throw new ArgumentException(SR.Argument_InvalidArrayType);
                }

                int count = dlist.Count;
                try {
                    for (int i = 0; i < count; i++) {
                        objects[index++] = dlist[i];
                    }
                }
                catch(ArrayTypeMismatchException) {
                    //ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
                    throw new ArgumentException(SR.Argument_InvalidArrayType);
                }
            }
        }

        bool IList.IsFixedSize {
            get { return true; }
        }

        bool IList.IsReadOnly {
            get { return true; }
        }

        object IList.this[int index] {
            //get { return list[index]; }
            get { return (T)list[index].Target; }
            set {
                //ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
                throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            }
        }

        int IList.Add(object value) {
            //ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
            //return -1;
        }

        void IList.Clear() {
            //ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        private static bool IsCompatibleObject(object value) {
            // Non-null values are fine.  Only accept nulls if T is a class or Nullable<U>.
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>.
            return ((value is T) || (value == null && default(T) == null));
        }

        bool IList.Contains(object value) {
            if(IsCompatibleObject(value)) {
                return Contains((T) value);
            }
            return false;
        }

        int IList.IndexOf(object value) {
            if(IsCompatibleObject(value)) {
                return IndexOf((T)value);
            }
            return -1;
        }

        void IList.Insert(int index, object value) {
            //ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        void IList.Remove(object value) {
            //ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        void IList.RemoveAt(int index) {
            //ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
            throw new NotSupportedException(SR.NotSupported_ReadOnlyCollection);
        }

        IList<T> CreateDereferencedList()
        {
            int n = list.Count;
            List<T> newList = new List<T>(n);
            for (int i=0; i<n; ++i)
            {
                newList.Add((T)list[i].Target);
            }
            return newList;
        }

        private class WeakEnumerator : IEnumerator<T>, IEnumerator
        {
            public WeakEnumerator(IEnumerator ie) {
                this.ie = ie;
            }

            public void Dispose() {
            }

            public bool MoveNext() {
                return ie.MoveNext();
            }

            public T Current {
                get {
                    WeakReference wr = ie.Current as WeakReference;
                    if (wr != null)
                        return (T)wr.Target;
                    else
                        return default(T);
                }
            }

            object IEnumerator.Current {
                get { return Current; }
            }

            void IEnumerator.Reset() {
                ie.Reset();
            }

            private IEnumerator ie;
        }
    }
}

