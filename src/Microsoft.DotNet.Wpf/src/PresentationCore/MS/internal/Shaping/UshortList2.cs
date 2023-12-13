// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Internal growable list with sublisting

using System.Windows;
using System;
using System.Collections;
using System.Diagnostics;
using MS.Internal;
using System.Security;

namespace MS.Internal.Shaping
{
    /// <summary>
    /// Growable ushort array.
    /// Only current sublist is visible for client. Sublists should be processed
    /// in sequence from start to end.
    /// </summary>
    internal class UshortList
    {
        internal UshortList(int capacity, int leap)
        {
            Invariant.Assert(capacity >= 0 && leap >= 0, "Invalid parameter");
            _storage = new UshortArray(capacity, leap);
        }

        internal UshortList(ushort[] array)
        {
            Invariant.Assert(array != null, "Invalid parameter");
            _storage = new UshortArray(array);
        }

        internal UshortList(CheckedUShortPointer unsafeArray, int arrayLength)
        {
            _storage = new UnsafeUshortArray(unsafeArray, arrayLength);
            _length = arrayLength;
        }

        public ushort this[int index]
        {
            get
            {
                Invariant.Assert(index >= 0  &&  index < _length, "Index out of range");
                return _storage[_index + index];
            }
            set
            {
                Invariant.Assert(index >= 0  &&  index < _length, "Index out of range");
                _storage[_index + index] = value;
            }
        }

        /// <summary>
        /// Length of current sublist
        /// </summary>
        public int Length
        {
            get { return _length; }
            set { _length = value; }
        }

        /// <summary>
        /// Offset inside whole storage
        /// </summary>
        public int Offset
        {
            get { return _index; }
        }

        /// <summary>
        /// Reset processing sequence to the start of the storage
        /// </summary>
        public void SetRange(int index, int length)
        {
            Invariant.Assert(length >= 0 && (index + length) <= _storage.Length, "List out of storage");
            _index  = index;
            _length = length;
        }

        /// <summary>
        /// Insert elements to the current run. Elements are not intialized.
        /// </summary>
        /// <param name="index">Position</param>
        /// <param name="count">Number of elements</param>
        public void Insert(int index, int count)
        {
            Invariant.Assert(index <= _length && index >= 0, "Index out of range");
            Invariant.Assert(count > 0, "Invalid argument");

            _storage.Insert(_index + index, count, _index + _length);
            _length += count;
        }


        /// <summary>
        /// Remove elements from the current run.
        /// </summary>
        /// <param name="index">Position</param>
        /// <param name="count">Number of elements</param>
        public void Remove(int index, int count)
        {
            Invariant.Assert(index < _length && index >= 0, "Index out of range");
            Invariant.Assert(count > 0 && (index + count) <= _length, "Invalid argument");

            _storage.Remove(_index + index, count, _index + _length);
            _length -= count;
        }

        public ushort[] ToArray()
        {
            return _storage.ToArray();
        }

        public ushort[] GetCopy()
        {
            return _storage.GetSubsetCopy(_index,_length);
        }

        private UshortBuffer    _storage;
        private int             _index;
        private int             _length;
    }


    /// <summary>
    /// Abstract ushort buffer
    /// </summary>
    internal abstract class UshortBuffer
    {
        protected int _leap;

        public abstract ushort this[int index] { get; set; }
        public abstract int Length { get; }

        public virtual ushort[] ToArray()
        {
            Debug.Assert(false, "Not supported");
            return null;
        }

        public virtual ushort[] GetSubsetCopy(int index, int count)
        {
            Debug.Assert(false, "Not supported");
            return null;
        }

        public virtual void Insert(int index, int count, int length)
        {
            Debug.Assert(false, "Not supported");
        }

        public virtual void Remove(int index, int count, int length)
        {
            Debug.Assert(false, "Not supported");
        }
    }


    /// <summary>
    /// Ushort buffer implemented as managed ushort array
    /// </summary>
    internal class UshortArray : UshortBuffer
    {
        private ushort[]    _array;

        internal UshortArray(ushort[] array)
        {
            _array = array;
        }

        internal UshortArray(int capacity, int leap)
        {
            _array = new ushort[capacity];
            _leap = leap;
        }

        public override ushort this[int index]
        {
            get { return _array[index];  }
            set { _array[index] = value; }
        }

        public override int Length
        {
            get { return _array.Length; }
        }

        public override ushort[] ToArray()
        {
            return _array;
        }

        public override ushort[] GetSubsetCopy(int index, int count)
        {
            ushort[] subsetArray = new ushort[count];
            
            //Move elements
            Buffer.BlockCopy(
                _array,
                index * sizeof(ushort),
                subsetArray,
                0,
                ((index + count) <= _array.Length ? count : _array.Length) * sizeof(ushort) 
                );

            return subsetArray;
        }
        
        public override void Insert(int index, int count, int length)
        {
            int newLength = length + count;

            if (newLength > _array.Length)
            {
                Invariant.Assert(_leap > 0, "Growing an ungrowable list!");

                //increase storage by integral number of _leaps.
                int extra = newLength - _array.Length;
                int newArraySize = _array.Length + ((extra - 1) / _leap + 1) * _leap;

                // get a new buffer
                ushort[] newArray = new ushort[newArraySize];

                //Move elements
                Buffer.BlockCopy(_array, 0, newArray, 0, index * sizeof(ushort));

                if (index < length)
                {
                    Buffer.BlockCopy(
                        _array,
                        index * sizeof(ushort),
                        newArray,
                        (index + count) * sizeof(ushort),
                        (length - index) * sizeof(ushort)
                        );
                }

                _array = newArray;
            }
            else
            {
                if (index < length)
                {
                    Buffer.BlockCopy(
                        _array,
                        index * sizeof(ushort),
                        _array,
                        (index + count) * sizeof(ushort),
                        (length - index) * sizeof(ushort)
                        );
                }
            }
        }

        public override void Remove(int index, int count, int length)
        {
            Buffer.BlockCopy(
                _array,
                (index + count) * sizeof(ushort),
                _array,
                (index) * sizeof(ushort),
                (length - index - count) * sizeof(ushort)
                );
        }
    }


    /// <summary>
    /// Ushort buffer implemented as unmanaged ushort array
    /// </summary>
    internal unsafe class UnsafeUshortArray : UshortBuffer
    {
        private ushort*     _array;

        private SecurityCriticalDataForSet<int>         _arrayLength;


        internal UnsafeUshortArray(CheckedUShortPointer array, int arrayLength)
        {            
            _array = array.Probe(0, arrayLength);
            _arrayLength.Value = arrayLength;
        }


        public override ushort this[int index]
        {
            get
            {
                Invariant.Assert(index >= 0 && index < _arrayLength.Value);
                return _array[index];
            }
            set
            {
                Invariant.Assert(index >= 0 && index < _arrayLength.Value);
                _array[index] = value;
            }
        }

        public override int Length
        {
            get { return _arrayLength.Value; }
        }
    }
}

