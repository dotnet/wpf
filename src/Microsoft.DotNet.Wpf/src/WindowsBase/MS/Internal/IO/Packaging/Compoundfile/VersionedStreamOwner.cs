// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   This class provides file versioning support for streams provided by 
//   IDataTransform implementations and any client code that needs
//   to store a FormatVersion at the beginning of a stream.
//
//
//
//

using System;
using System.IO;                                // for Stream
using System.Windows;                           // ExceptionStringTable
using System.Globalization;                     // for CultureInfo
using System.Diagnostics;                       // for Debug.Assert
using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Packaging.CompoundFile
{
    /// <summary>
    /// Specialized stream that owns the FormatVersion
    /// </summary>
    /// <remarks>Dispose() functionality is handled by our subclass VersionedStream</remarks>
    internal class VersionedStreamOwner : VersionedStream
    {
        #region Stream Methods
        /// <summary>
        /// Return the bytes requested from the container
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            // throw if version missing
            ReadAttempt(true);
            return BaseStream.Read(buffer, offset, count);
        }

        /// <summary>
        /// Write
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAttempt();
            BaseStream.Write(buffer, offset, count);
        }

        /// <summary>
        /// ReadByte
        /// </summary>
        public override int ReadByte()
        {
            ReadAttempt(true);
            return BaseStream.ReadByte();
        }

        /// <summary>
        /// WriteByte
        /// </summary>
        public override void WriteByte(byte b)
        {
            WriteAttempt();
            BaseStream.WriteByte(b);
        }

        /// <summary>
        /// Seek
        /// </summary>
        /// <param name="offset">offset</param>
        /// <param name="origin">origin</param>
        /// <returns>zero</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            ReadAttempt();

            long temp = -1;
            switch (origin)
            {
                // seek beyond the FormatVersion
                case SeekOrigin.Begin:
                    temp = offset;
                    break;

                case SeekOrigin.Current:
                    checked { temp = Position + offset; }
                    break;

                case SeekOrigin.End:
                    checked { temp = Length + offset; }
                    break;
            }

            if (temp < 0)
                throw new ArgumentException(SR.SeekNegative);

            checked { BaseStream.Position = temp + _dataOffset; }
            return temp;
        }

        /// <summary>
        /// SetLength
        /// </summary>
        public override void SetLength(long newLength)
        {
            if (newLength < 0)
                throw new ArgumentOutOfRangeException("newLength");

            WriteAttempt();
            checked { BaseStream.SetLength(newLength + _dataOffset); }
        }

        /// <summary>
        /// Flush
        /// </summary>
        public override void Flush()
        {
            CheckDisposed();
            BaseStream.Flush();
        }
        #endregion Stream Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
        #region Stream Properties
        /// <summary>
        /// Current logical position within the stream
        /// </summary>
        public override long Position
        {
            get
            {
                ReadAttempt();  // ensure _dataOffset is valid
                return checked(BaseStream.Position - _dataOffset);
            }
            set
            {
                // share Seek logic and validation
                Seek(value, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Length
        /// </summary>
        public override long Length
        {
            get
            {
                ReadAttempt();  // ensure _dataOffset is valid

                long temp = checked(BaseStream.Length - _dataOffset);
                Invariant.Assert(temp >= 0);                    // catch any math errors
                return temp;
            }
        }

        /// <summary>
        /// Is stream readable?
        /// </summary>
        /// <remarks>returns false when called on disposed stream</remarks>
        public override bool CanRead
        {
            get
            {
                return (BaseStream != null) && BaseStream.CanRead && IsReadable;
            }
        }

        /// <summary>
        /// Is stream seekable - should be handled by our owner
        /// </summary>
        /// <remarks>returns false when called on disposed stream</remarks>
        public override bool CanSeek
        {
            get
            {
                return (BaseStream != null) && BaseStream.CanSeek && IsReadable;
            }
        }

        /// <summary>
        /// Is stream writeable?
        /// </summary>
        /// <remarks>returns false when called on disposed stream</remarks>
        public override bool CanWrite
        {
            get
            {
                return (BaseStream != null) && BaseStream.CanWrite && IsUpdatable;
            }
        }
        #endregion


        /// <summary>
        /// Constructor to use for the "versioned stream" - the one that actually houses the 
        /// persisted FormatVersion.
        /// </summary>
        /// <param name="baseStream"></param>
        /// <param name="codeVersion"></param>
        internal VersionedStreamOwner(Stream baseStream, FormatVersion codeVersion)
            : base(baseStream)
        {
            _codeVersion = codeVersion;
        }

        internal bool IsUpdatable
        {
            get
            {
                CheckDisposed();

                // first try to read the format version
                EnsureParsed();

                // We can write if either:
                // 1. FileVersion doesn't exist - this is normal for a new, empty stream
                // 2. It does exist and the FormatVersion indicates that we can update it
                return (_fileVersion == null) ||
                    _fileVersion.IsUpdatableBy(_codeVersion.UpdaterVersion);
            }
        }

        internal bool IsReadable
        {
            get
            {
                CheckDisposed();

                // first try to read the format version
                EnsureParsed();

                // We can read if either:
                // 1. FileVersion doesn't exist - normal for a new, empty stream
                // 1. FileVersion exists and indicates that we can read it
                return (_fileVersion == null) ||
                    _fileVersion.IsReadableBy(_codeVersion.ReaderVersion);
            }
        }

        /// <summary>
        /// Callback for when a stream is written to
        /// </summary>
        /// <remarks>Can modify the FormatVersion stream pointer.</remarks>
        internal void WriteAttempt()
        {
            CheckDisposed();    // central location

            if (!_writeOccurred)
            {
                // first try to read the format version
                EnsureParsed();

                if (_fileVersion == null)
                {
                    // stream is empty so write our version
                    PersistVersion(_codeVersion);
                }
                else
                {
                    // file version found - ensure we are able to update it
                    if (!_fileVersion.IsUpdatableBy(_codeVersion.UpdaterVersion))
                    {
                        throw new FileFormatException(
                                        SR.Format(
                                            SR.UpdaterVersionError,
                                            _fileVersion.UpdaterVersion,
                                            _codeVersion
                                            )
                                        );
                    }

                    // if our version is different than previous
                    // updater then "update" the updater
                    if (_codeVersion.UpdaterVersion != _fileVersion.UpdaterVersion)
                    {
                        _fileVersion.UpdaterVersion = _codeVersion.UpdaterVersion;
                        PersistVersion(_fileVersion);
                    }
                }

                _writeOccurred = true;
            }
        }

        internal void ReadAttempt()
        {
            ReadAttempt(false);
        }

        /// <summary>
        /// Callback for when a Stream is read from
        /// </summary>
        /// <remarks>Can modify the FormatVersion stream pointer.</remarks>
        /// <param name="throwIfEmpty">caller requires an existing FormatVersion</param>
        internal void ReadAttempt(bool throwIfEmpty)
        {
            CheckDisposed();    // central location

            // only do this once
            if (!_readOccurred)
            {
                // read
                EnsureParsed();

                // first usage?
                if (throwIfEmpty || BaseStream.Length > 0)
                {
                    if (_fileVersion == null)
                        throw new FileFormatException(SR.VersionStreamMissing);

                    // compare versions
                    // verify we can read this version
                    if (!_fileVersion.IsReadableBy(_codeVersion.ReaderVersion))
                    {
                        throw new FileFormatException(
                                        SR.Format(
                                            SR.ReaderVersionError,
                                            _fileVersion.ReaderVersion,
                                            _codeVersion
                                            )
                                        );
                    }
                }
                _readOccurred = true;
            }
        }

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        /// <summary>
        /// Ensure that the version is persisted
        /// </summary>
        /// <remarks>Leaves stream at position just after the FormatVersion.
        /// Destructive.  This is called automatically from WriteAttempt but callers
        /// can call directly if they have changed the stream contents to a format
        /// that is no longer compatible with the persisted FormatVersion.  If
        /// this is not called directly, and a FormatVersion was found in the file
        /// then only the Updater field is modified.
        /// </remarks>
        private void PersistVersion(FormatVersion version)
        {
            if (!BaseStream.CanWrite)
                throw new NotSupportedException(SR.WriteNotSupported);

            // normalize and save
            long tempPos = checked(BaseStream.Position - _dataOffset);
            BaseStream.Seek(0, SeekOrigin.Begin);

            // update _dataOffset
            long offset = version.SaveToStream(BaseStream);
            _fileVersion = version;     // we know what it is - no need to deserialize

            // existing value - ensure we didn't change sizes as this could lead to
            // data corruption
            if ((_dataOffset != 0) && (offset != _dataOffset))
                throw new FileFormatException(SR.VersionUpdateFailure);

            // at this point we know the offset
            _dataOffset = offset;

            // restore and shift
            checked { BaseStream.Position = tempPos + _dataOffset; }
        }

        /// <summary>
        /// Load and compare feature identifier
        /// </summary>
        /// <remarks>There is no need for this method to maintain any previous Seek pointer.
        /// This method only modifies the stream position when called for the first time with a non-empty
        /// stream.  It is always called from Seek() and set_Position, which subsequently modify the stream
        /// pointer as appropriate after the call.</remarks>
        private void EnsureParsed()
        {
            // empty stream cannot have a version in it
            if ((_fileVersion == null) && (BaseStream.Length > 0))
            {
                Debug.Assert(_dataOffset == 0);

                // if no version was found and we cannot read from it, then the format is invalid
                if (!BaseStream.CanRead)
                    throw new NotSupportedException(SR.ReadNotSupported);

                //
                // The physical stream begins with a header that identifies the transform to
                // which the stream belongs. The "logical" stream object handed to us by the
                // compound file begins -after- this stream header, so when we seek to the
                // "beginning" of this stream, we are actually seeking to the location after
                // the stream header, where the instance data starts.
                //
                BaseStream.Seek(0, SeekOrigin.Begin);

                //
                // The instance data starts with format version information for this transform.
                //
                _fileVersion = FormatVersion.LoadFromStream(BaseStream);

                //
                // Ensure that the feature name is as expected.
                //
                // NOTE: We preserve case, but do case-insensitive comparison.
                if (String.CompareOrdinal(
                                _fileVersion.FeatureIdentifier.ToUpper(CultureInfo.InvariantCulture),
                                _codeVersion.FeatureIdentifier.ToUpper(CultureInfo.InvariantCulture)) != 0)
                {
                    throw new FileFormatException(
                                    SR.Format(
                                        SR.InvalidTransformFeatureName,
                                        _fileVersion.FeatureIdentifier,
                                        _codeVersion.FeatureIdentifier
                                        )
                                    );
                }

                _dataOffset = BaseStream.Position;
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
        private bool            _writeOccurred;     // did one of our streams get written to?
        private bool            _readOccurred;      // did one of our streams get read from?
        private FormatVersion   _codeVersion;       // code version
        private FormatVersion   _fileVersion;       // current file version (null if not read or created yet)
        private long            _dataOffset = 0;    // where FormatVersion ends and data begins
    }
}
