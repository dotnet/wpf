// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.ComponentModel;

namespace System.Windows.Baml2006
{
    /// <summary>
    /// Reference counted stream 
    /// Allows multiple callers independently coordinate "current position"
    /// for reads and writes on a shared stream.
    /// Methods on this class are not thread safe.
    /// </summary>
    internal class SharedStream : Stream
    {
        private Stream _baseStream;
        private long _offset;
        private long _length;
        private long _position;
        private RefCount _refCount;

        public SharedStream(Stream baseStream)
        {
            if (baseStream == null)
            {
                throw new ArgumentNullException(nameof(baseStream));
            }

            Initialize(baseStream, 0, baseStream.Length);
        }

        /// <summary>
        /// Constructor that limits the bytes that can be written to or read form
        /// </summary>
        /// <param name="baseStream"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public SharedStream(Stream baseStream, long offset, long length)
        {
            if (baseStream == null)
            {
                throw new ArgumentNullException(nameof(baseStream));
            }

            Initialize(baseStream, offset, length);
        }

        private void Initialize(Stream baseStream, long offset, long length)
        {
            if (baseStream.CanSeek == false)
            {
                throw new ArgumentException("can\u2019t seek on baseStream");
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            SharedStream subStream = baseStream as SharedStream;
            if (subStream != null)
            {
                _baseStream = subStream.BaseStream;
                _offset = offset + subStream._offset;
                _length = length;
                _refCount = subStream._refCount;
                _refCount.Value++;
            }
            else
            {
                _baseStream = baseStream;
                _offset = offset;
                _length = length;
                _refCount = new RefCount();
                _refCount.Value++;
            }
        }

        /// <summary>
        /// Number of clients sharing the base stream
        /// </summary>
        public virtual int SharedCount
        {
            get { return _refCount.Value; }
        }

        public override bool CanRead
        {
            get
            {
                CheckDisposed();
                return _baseStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
#if true
                return false;
#else
                CheckDisposed();
                return _baseStream.CanWrite;
#endif
            }
        }

        public override void Flush()
        {
            CheckDisposed();
            _baseStream.Flush();
        }

        public override long Length
        {
            get
            {
                return _length;
            }
        }

        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {
                if (value < 0 || value >= _length)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, string.Empty);
                }

                _position = value;
            }
        }

        public virtual bool IsDisposed { get { return _baseStream == null; } }

        public override int ReadByte()
        {
            CheckDisposed();

            long end = Math.Min(_position + 1, _length);

            int b;
            if (Sync())
            {
                b = _baseStream.ReadByte();
                _position = _baseStream.Position - _offset;
            }
            else
            {
                b = -1;
            }
            return b;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0 || offset >= buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if ((offset + count) > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            CheckDisposed();

            long end = Math.Min(_position + count, _length);

            int bytesRead = 0;

            if (Sync())
            {
                bytesRead = _baseStream.Read(buffer, offset, (int)(end - _position));
                _position = _baseStream.Position - _offset;
            }

            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
#if true
            throw new NotSupportedException();
#else
            if(buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if(offset < 0 || offset >= buffer.Length) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if((offset + count) > buffer.Length) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            CheckDisposed(); 
                
            long end = Math.Min(_position + count, _length);

            if (Sync())
            {
                _baseStream.Write(buffer, offset, (int)(end - _position));
                _position = _baseStream.Position - _offset;
            }
#endif
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = offset;
                    break;

                case SeekOrigin.Current:
                    newPosition = _position + offset;
                    break;

                case SeekOrigin.End:
                    newPosition = _length + offset;
                    break;

                default:
                    throw new InvalidEnumArgumentException(nameof(origin), (int)origin, typeof(SeekOrigin));
            }

            if (newPosition < 0 || newPosition >= _length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, string.Empty);
            }

            CheckDisposed();

            _position = newPosition;

            return _position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_baseStream != null)
                {
                    _refCount.Value--;
                    if (_refCount.Value < 1)
                    {
                        _baseStream.Close();
                    }
                    _refCount = null;
                    _baseStream = null;
                }
            }

            base.Dispose(disposing);
        }

        public Stream BaseStream
        {
            get { return _baseStream; }
        }

        private void CheckDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("BaseStream");
            }
        }

        private bool Sync()
        {
            System.Diagnostics.Debug.Assert(_baseStream != null, "Stream has already been disposed");

            if (_position >= 0 && _position < _length)
            {
                if (_position + _offset != _baseStream.Position)
                    _baseStream.Seek(_offset + _position, SeekOrigin.Begin);
                return true;
            }

            return false;
        }

        class RefCount
        {
            public int Value;
        }
    }
}
