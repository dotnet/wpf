// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   The object for wrapping a data stream and its associated context
//   information dictionary.
//
//
//
//
//

using System;
using System.Collections;
using System.IO;

namespace MS.Internal.IO.Packaging.CompoundFile
{
    internal class StreamWithDictionary : Stream, IDictionary
    {
        Stream baseStream;
        IDictionary baseDictionary;
        private bool _disposed;         // keep track of if we are disposed

        internal StreamWithDictionary( Stream wrappedStream, IDictionary wrappedDictionary )
        {
            baseStream = wrappedStream;
            baseDictionary = wrappedDictionary;
        }

        /*************************************************************************/
        // Stream members
        public override bool CanRead { get{ return !_disposed && baseStream.CanRead; }}
        public override bool CanSeek { get { return !_disposed && baseStream.CanSeek; } }
        public override bool CanWrite { get { return !_disposed && baseStream.CanWrite; } }
        public override long Length  
        {
            get
            {
                CheckDisposed();
                return baseStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                CheckDisposed();
                return baseStream.Position;
            } 
            set
            {
                CheckDisposed();
                baseStream.Position = value;
            }
        }

        public override void Flush() 
        {
            CheckDisposed();
            baseStream.Flush(); 
        }

        public override long Seek( long offset, SeekOrigin origin )
        {
            CheckDisposed();
            return baseStream.Seek(offset, origin);
        }

        public override void SetLength( long newLength ) 
        {
            CheckDisposed();
            baseStream.SetLength(newLength);
        }

        public override int Read( byte[] buffer, int offset, int count )
        {
            CheckDisposed();
            return baseStream.Read(buffer, offset, count);
        }

        public override void Write( byte[] buffer, int offset, int count )
        {
            CheckDisposed();
            baseStream.Write(buffer, offset, count);
        }

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Dispose(bool)
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && !_disposed)
                {
                    _disposed = true;
                    baseStream.Close();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /*************************************************************************/
        // IDictionary members
        bool IDictionary.Contains( object key )
        {
            CheckDisposed();
            return baseDictionary.Contains(key);
        }

        void IDictionary.Add( object key, object val )
        {
            CheckDisposed();
            baseDictionary.Add(key, val);
        }

        void IDictionary.Clear()
        {
            CheckDisposed();
            baseDictionary.Clear();
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            CheckDisposed();
            // IDictionary.GetEnumerator vs. IEnumerable.GetEnumerator?
            return ((IDictionary)baseDictionary).GetEnumerator();
        }

        void IDictionary.Remove( object key )
        {
            CheckDisposed();
            baseDictionary.Remove(key);
        }

        object IDictionary.this[ object index ]
        {
            get
            {
                CheckDisposed();
                return baseDictionary[index];
            }
            set
            {
                CheckDisposed();
                baseDictionary[index] = value;
            }
        }

        ICollection IDictionary.Keys
        {
            get
            {
                CheckDisposed();
                return baseDictionary.Keys;
            }
        }

        ICollection IDictionary.Values
        {
            get
            {
                CheckDisposed();
                return baseDictionary.Values;
            }
        }

        bool IDictionary.IsReadOnly
        {
            get
            {
                CheckDisposed();
                return baseDictionary.IsReadOnly;
            }
        }

        bool IDictionary.IsFixedSize
        {
            get
            {
                CheckDisposed();
                return baseDictionary.IsFixedSize;
            }
        }

        /*************************************************************************/
        // ICollection methods

        void ICollection.CopyTo( Array array, int index )
        {
            CheckDisposed();
            ((ICollection)baseDictionary).CopyTo(array, index);
        }

        int ICollection.Count
        {
            get
            {
                CheckDisposed();
                return ((ICollection)baseDictionary).Count;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                CheckDisposed();
                return ((ICollection)baseDictionary).SyncRoot;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                CheckDisposed();
                return ((ICollection)baseDictionary).IsSynchronized;
            }
        }

        /*************************************************************************/
        // IEnumerable method

        IEnumerator IEnumerable.GetEnumerator()
        {
            CheckDisposed();
            return ((IEnumerable)baseDictionary).GetEnumerator();
        }

        /// <summary>
        /// Disposed - were we disposed? Offer this to DataSpaceManager so it can do smart flushing
        /// </summary>
        /// <returns></returns>
        internal bool Disposed
        {
            get
            {
                return _disposed;
            }
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("Stream");
        }
    }
}
