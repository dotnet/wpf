// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//  Description:    The class StreamingZipPartStream is used to create a sequence of
//                  piece streams in order to implement streaming production of packages.
//

using System;
using System.IO;
using System.IO.Packaging;                  // For ZipPackagePart, etc.
using MS.Internal.IO.Zip;                   // For ZipFileInfo.
using System.Windows;                       // for ExceptionStringTable

using MS.Internal;                          // for Invariant
using MS.Internal.WindowsBase;

using ZipPackage = MS.Internal.IO.Packaging.Extensions.ZipPackage;

namespace MS.Internal.IO.Packaging
{
    /// <summary>
	/// The class StreamingZipPartStream is used to create a sequence of
    /// piece streams in order to implement streaming production of packages.
    /// </summary>
    /// <remarks>
    /// This class is defined for the benefit of ZipPackage, ZipPackagePart and
    /// InternalRelationshipCollection.
    /// Although it is quite specialized, it would hardly make sense to nest its definition in any
    /// of these clases.
    /// </remarks>
    internal class StreamingZipPartStream : Stream
    {
        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
       /// <summary>
        /// Build a System.IO.Stream to create a multi-piece (i.e. interleaved) part.
        /// Does not require a part, but a proper part name (not a piece name), and a ZipArchive.
        /// </summary>
        internal StreamingZipPartStream(
            string          partName,
            ZipArchive      zipArchive,
            CompressionMethodEnum compressionMethod,
            DeflateOptionEnum deflateOption,
            FileMode        mode,
            FileAccess      access)
        {
            // Right now, only production is supported in streaming mode.
            if (!(   (mode == FileMode.Create || mode == FileMode.CreateNew)
                  && access == FileAccess.Write) )
            {
                throw new NotSupportedException(SR.Get(SRID.OnlyStreamingProductionIsSupported));
            }

            _partName = partName;
            _archive = zipArchive;
            _compressionMethod = compressionMethod;
            _deflateOption = deflateOption;
            _mode = mode;
            _access = access;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        /// <summary>
        /// Return the bytes requested.
        /// </summary>
        /// <param name="buffer">Destination buffer.</param>
        /// <param name="offset">
        /// The zero-based byte offset in buffer at which to begin storing the data read
        /// from the current stream.
        /// </param>
        /// <param name="count">How many bytes requested.</param>
        /// <returns>How many bytes were written into buffer.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException(SR.Get(SRID.OnlyWriteOperationsAreSupportedInStreamingCreation));
        }

        /// <summary>
        /// Seek
        /// </summary>
        /// <param name="offset">Offset in byte.</param>
        /// <param name="origin">Offset origin (start, current, or end).</param>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(SR.Get(SRID.OnlyWriteOperationsAreSupportedInStreamingCreation));
        }

        /// <summary>
        /// SetLength
        /// </summary>
        public override void SetLength(long newLength)
        {
            throw new InvalidOperationException(SR.Get(SRID.OperationViolatesWriteOnceSemantics, "SetLength"));
        }

        /// <summary>
        /// Write. Delegate to the current piece stream.
        /// Lazily create the Zip item since we do not know what name to create it
        /// under until a write or a close occurs.
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckClosed();

            // We now know we're creating a non-empty piece, so it's OK to give
            // it a non-terminal name.
            EnsurePieceStream(false /* not last piece */);
            _pieceStream.Write(buffer, offset, count);
        }

        /// <summary>
        /// Close the current piece stream and increment the piece number
        /// to allow on-demand creation of the next piece stream in Write
        /// or Close.
        /// </summary>
        /// <remarks>Pass through the Flush calls because there is no need to 
        /// generate a new Piece if we are writing to a single, enormouse stream.</remarks>
        public override void Flush()
        {
            CheckClosed();

            // _pieceStream will be null if there's been no write since the last flush.
            if (_pieceStream != null)
            {
                // If CanWrite is false, we know that our underlying stream was closed by ZipIO layer
                // as a part of its logic.  Therefore, we need a new Piece.
                if (_pieceStream.CanWrite)
                    _pieceStream.Flush();
            }
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Is stream readable?
        /// </summary>
        /// <remarks>
        /// Here, the assumption, as in all capability tests, is that the status of
        /// the current piece reflects the status of all pieces for the part.
        /// This is justified by the fact that (i) all piece streams are opened with the same
        /// parameters against the same archive and (ii) the current piece stream cannot get
        /// closed unless the whole part stream is closed.
        /// </remarks>
        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Is stream seekable?
        /// </summary>
        /// <remarks>
        /// Here, the assumption, as in all capability tests, is that the status of
        /// the current piece reflects the status of all pieces for the part.
        /// This is justified by the fact that (i) all piece streams are opened with the same
        /// parameters against the same archive and (ii) the current piece stream cannot get
        /// closed unless the whole part stream is closed.
        /// </remarks>
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Is stream writable?
        /// </summary>
        /// <remarks>
        /// Here, the assumption, as in all capability tests, is that the status of
        /// the current piece reflects the status of all pieces for the part.
        /// This is justified by the fact that (i) all piece streams are opened with the same
        /// parameters against the same archive and (ii) the current piece stream cannot get
        /// closed unless the whole part stream is closed.
        /// </remarks>
        public override bool CanWrite
        {
            get
            {
                return !_closed;
            }
        }

        /// <summary>
        /// Logical byte position in this stream.
        /// </summary>
        public override long Position
        {
            get
            {
                return -1;
            }
            set
            {
                throw new InvalidOperationException(SR.Get(SRID.OperationViolatesWriteOnceSemantics, "set_Position"));
            }
        }

        /// <summary>
        /// Length.
        /// </summary>
        public override long Length
        {
            get
            {
                return -1;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Dispose(bool)
        /// </summary>
        /// <param name="disposing"></param>
        /// <remarks>
        /// An instance of streams' peculiar dispose pattern, whereby
        /// the inherited abstract Stream class implements Close by calling
        /// this virtual protected function.
        /// In turn, each implementation is responsible for calling back
        /// its base's implementation.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {  
            try
            {
                if (disposing)
                {
                    if (!_closed)
                    {
                        // Flush pending changes into a piece, if any.
                        Flush();

                        // Create an empty last piece.
                        EnsurePieceStream(true /* last piece */);
                        _pieceStream.Close();                      
                    }
                }
            }
            finally
            {
                _closed = true;
                base.Dispose(disposing);
            }
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //   Private Methods
        //
        //------------------------------------------------------

        private void EnsurePieceStream(bool isLastPiece)
        {
            if (_pieceStream != null)
            {
                // Normally, the pieces are actually closed automatically for us by the
                // underlying ZipIO logic, but in the case of the last piece (when we
                // are called by our own Dispose(bool) method) we must close it explicitly.
                if (isLastPiece)
                    _pieceStream.Close();

                // We detect that the stream has been closed by inspecting the CanWrite property
                // since this is guaranteed not to throw even when the stream is disposed.
                if (!_pieceStream.CanWrite)
                {
                    // increment our piece number so we can generate the correct
                    // one below
                    checked { ++_currentPieceNumber; }

                    // release it to trigger the new piece creation below
                    _pieceStream = null;        
                }
            }

            if (_pieceStream == null)
            {
                string pieceName = PieceNameHelper.CreatePieceName(
                    _partName,
                    _currentPieceNumber,
                    isLastPiece);
                string pieceZipName = ZipPackage.GetZipItemNameFromOpcName(pieceName);

                ZipFileInfo zipFileInfo = _archive.AddFile(pieceZipName, _compressionMethod, _deflateOption);
                // We've just created the file, so the mode can only be Create, not CreateNew.
                // (At least, this is part of ZipFileInfo's belief system.)
                _pieceStream = zipFileInfo.GetStream(FileMode.Create, _access);
            }
        }

        private void CheckClosed()
        {
            if (_closed)
                throw new ObjectDisposedException(null, SR.Get(SRID.StreamObjectDisposed));
        }

        //------------------------------------------------------
        //
        //   Private Properties
        //
        //------------------------------------------------------
        // None

        #region Private Fields

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private Stream              _pieceStream;               // write-only stream on the current piece
        private string              _partName;                  // part name used to generate correct piece names
        private ZipArchive          _archive;
        private int                 _currentPieceNumber = 0;    // incremented with each piece Close() cycle
        private CompressionMethodEnum _compressionMethod;
        private DeflateOptionEnum   _deflateOption;
        private FileMode            _mode;
        private FileAccess          _access;
        private bool                _closed = false;

        #endregion Private Fields
    }
}

