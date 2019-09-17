// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
* Purpose:  Contains the Reader/Writer stream implementation for
*           Doing async parsing on a separate thread.
*
\***************************************************************************/
using System;
using System.Xml;
using System.IO;
using System.Windows;
using System.Collections;
using System.ComponentModel;

using System.Diagnostics;
using System.Reflection;
using System.Threading;

using MS.Utility;


#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{
    /// <summary>
    /// Main class for setting up Reader and Writer streams as well as
    /// keeping common data and any synchronization work.
    ///
    /// Writer is allowed write and seek back to any position in the file
    /// until it calls UpdateReaderLength(position). The position passed in is
    /// an absolute position in the file. After making this call the Writer is
    /// only allowed to seek and write past this position.
    ///
    /// The Reader only sees the part of the file that the Writer says is ready
    /// to read. Initially the length of the Reader stream is zero. When the Writer
    /// calls UpdateReaderLength(position)the length of the Reader stream is set
    /// to that position. The Reader calls ReaderDoneWithFileUpToPosition(position) to indicate
    /// it no longer needs the file up to and including that position. After this call
    /// it is an error for the Reader to try read bits before the position.
    /// </summary>
    internal class ReadWriteStreamManager
    {
        #region Constructors

#if PBTCOMPILER
        // The compiler only needs the class definition, and none of the implementation.
        private ReadWriteStreamManager()
#else
        internal ReadWriteStreamManager()
#endif
        {
            ReaderFirstBufferPosition = 0;
            WriterFirstBufferPosition = 0;
            ReaderBufferArrayList = new ArrayList();
            WriterBufferArrayList = new ArrayList();

            _writerStream = new WriterStream(this);
            _readerStream = new ReaderStream(this);

            _bufferLock = new ReaderWriterLock();
        }

        #endregion Constructors

        #region WriterCallbacks

        /// <summary>
        /// Writes the counts of bytes into the stream.
        /// </summary>
        /// <param name="buffer">input buffer</param>
        /// <param name="offset">starting offset into the buffer</param>
        /// <param name="count">number of bytes to write</param>
        internal void Write(byte[] buffer, int offset, int count)
        {
            int bufferOffset;
            int bufferIndex;
#if DEBUG
            Debug.Assert(!WriteComplete,"Write called after close");
#endif
            Debug.Assert(null != buffer,"Null buffer past to the Writer");

            // if nothing to write then just return.
            if (0 == count)
            {
                return;
            }

            byte[] writeBuffer = GetBufferFromFilePosition(
                                    WritePosition,false /*writer*/,
                                    out bufferOffset, out bufferIndex);

            Debug.Assert(null != writeBuffer,"Null writeBuffer returned");

            // see how many bits fit into this buffer.
            int availableBytesInBuffer =  BufferSize - bufferOffset;
            int leftOverBytes = 0;
            int bufferWriteCount = 0;

            // check if ther is enough room in the write buffer to meet the
            // request or if there will be leftOverBytes.
            if (count > availableBytesInBuffer)
            {
                bufferWriteCount = availableBytesInBuffer;
                leftOverBytes = count - availableBytesInBuffer;
            }
            else
            {
                leftOverBytes = 0;
                bufferWriteCount = count;
            }

            Debug.Assert(0 < bufferWriteCount,"Not writing any bytes to the buffer");

            // now loop through writing out all or the number of bits that can fit in the buffer.
            for (int loopCount = 0; loopCount < bufferWriteCount; loopCount++)
            {
                Debug.Assert(bufferOffset < BufferSize,"Trying to Read past bufer");

                writeBuffer[bufferOffset++] = buffer[offset++];
            }

            // update the writePosition
            WritePosition += bufferWriteCount;

            // check if need to update length of the file that the writer sees.
            if (WritePosition > WriteLength)
            {
                WriteLength = WritePosition;
            }

            // if we have any leftOver Bytes call Write Again.
            if (leftOverBytes > 0)
            {
                Write(buffer,offset,leftOverBytes);
            }
        }

        /// <summary>
        /// Adjust the Writer's Seek Pointer.
        /// Writer is not allowed to Seek before where the ReaderLength or
        /// Seek past the writeLength.
        /// </summary>
        /// <param name="offset">seek offset</param>
        /// <param name="loc">specifies how to interpret the seeek offset</param>
        /// <returns></returns>
        internal long WriterSeek(long offset, SeekOrigin loc)
        {
            switch(loc)
            {
                case SeekOrigin.Begin:
                    WritePosition = (int) offset;
                    break;
                case SeekOrigin.Current:
                    WritePosition = (int) (WritePosition + offset);
                    break;
                case SeekOrigin.End:
                    throw new NotSupportedException(SR.Get(SRID.ParserWriterNoSeekEnd));
                default:
                    throw new ArgumentException(SR.Get(SRID.ParserWriterUnknownOrigin));
            }

            if( (!( WritePosition <= WriteLength ))
                ||
                (!( WritePosition >= ReadLength  )) )
            {
                throw new ArgumentOutOfRangeException( "offset" );
            }

            return WritePosition;
        }


        /// <summary>
        /// Called by the Writer to indicate its okay for the reader to see
        /// the file up to and including the position. Once the Writer calls
        /// this it cannot go back and change the content.
        /// </summary>
        /// <param name="position">Absolute position in the stream</param>
        internal void UpdateReaderLength(long position)
        {
            if(!(ReadLength <= position))
            {
                throw new ArgumentOutOfRangeException( "position" );
            }
#if DEBUG
            Debug.Assert(!WriteComplete,"UpdateReaderLength called after close");
#endif

            ReadLength = position;

            if(!(ReadLength <= WriteLength))
            {
                throw new ArgumentOutOfRangeException( "position" );
            }

            // safe for them to check and remove unused buffers.
            CheckIfCanRemoveFromArrayList(position,WriterBufferArrayList,
                                                ref _writerFirstBufferPosition);
        }

        /// <summary>
        /// Closes the Writer Stream
        /// </summary>
        internal void WriterClose()
        {
#if DEBUG
            _writeComplete = true;
#endif
        }

        #endregion WriterCallbacks


        #region ReaderCallbacks

        /// <summary>
        /// Reads the specified number of bytes into the buffer
        /// </summary>
        /// <param name="buffer">buffer to add the bytes</param>
        /// <param name="offset">zero base starting offset</param>
        /// <param name="count">number of bytes to read</param>
        /// <returns></returns>
        internal int Read(byte[] buffer, int offset, int count)
        {
            if(!(count  + ReadPosition <= ReadLength))
            {
                throw new ArgumentOutOfRangeException( "count" );
            }
            int bufferOffset;
            int bufferIndex;

            byte[] readBuffer = GetBufferFromFilePosition(
                ReadPosition,true /*reader*/,
                out bufferOffset, out bufferIndex);


            Debug.Assert(bufferOffset < BufferSize,"Calculated bufferOffset is greater than buffer");

            // see how many bytes we can read from this buffer.
            int availableBytesInBuffer =  BufferSize - bufferOffset;
            int leftOverBytes = 0;
            int bufferReadCount = 0;

            // check if ther is enough room in the write buffer to meet the
            // request or if there will be leftOverBytes.
            if (count > availableBytesInBuffer)
            {
                bufferReadCount = availableBytesInBuffer;
                leftOverBytes = count - availableBytesInBuffer;
            }
            else
            {
                leftOverBytes = 0;
                bufferReadCount = count;
            }

            Debug.Assert(0 < bufferReadCount,"Not reading any bytes to the buffer");

            for (int loopCount = 0; loopCount < bufferReadCount; loopCount++)
            {
                // make sure not going over the buffer.
                Debug.Assert(bufferOffset < BufferSize,"Trying ot read past buffer");
                buffer[offset++] = readBuffer[bufferOffset++];
            }

            // update the read position
            ReadPosition += bufferReadCount;


            if (leftOverBytes > 0)
            {
                Read(buffer,offset,(int) leftOverBytes);
            }

            return count;
        }


        /// <summary>
        /// Called to Read a Byte from the file. for now we allocate a byte
        /// and call the standard Read method.
        /// </summary>
        /// <returns></returns>
        internal int ReadByte()
        {
            byte[] buffer = new byte[1];

            // uses Read to validate if reading past the end of the file.
            Read(buffer,0,1);
            return (int) buffer[0];
        }

        /// <summary>
        /// Adjusts the Reader Seek position.
        /// </summary>
        /// <param name="offset">offset for the seek</param>
        /// <param name="loc">defines relative offset </param>
        /// <returns></returns>
        internal long ReaderSeek(long offset, SeekOrigin loc)
        {
            switch(loc)
            {
                case SeekOrigin.Begin:
                    ReadPosition = (int) offset;
                    break;
                case SeekOrigin.Current:
                    ReadPosition = (int) (ReadPosition + offset);
                    break;
                case SeekOrigin.End:
                    throw new NotSupportedException(SR.Get(SRID.ParserWriterNoSeekEnd));
                default:
                    throw new ArgumentException(SR.Get(SRID.ParserWriterUnknownOrigin));
            }

            // validate if at a good readPosition.
            if((!(ReadPosition >= ReaderFirstBufferPosition))
                ||
               (!(ReadPosition < ReadLength)))
            {
                throw new ArgumentOutOfRangeException( "offset" );
            }

            return ReadPosition;
        }

        /// <summary>
        /// called by Reader to tell us it is done with everything
        /// up to  the given position. Once making this call the
        /// Reader can no long reader at and before the position.
        /// </summary>
        /// <param name="position"></param>
        internal void ReaderDoneWithFileUpToPosition(long position)
        {
            // call CheckIfCanRemove to update the readers BufferPositions
            // and do any cleanup.
            CheckIfCanRemoveFromArrayList(position,ReaderBufferArrayList,
                                ref _readerFirstBufferPosition);
        }


        #endregion ReaderCallbacks


        #region PrivateMethods

        /// <summary>
        /// Given a position in the Stream returns the buffer and
        /// start offset position in the buffer
         /// </summary>
        byte[] GetBufferFromFilePosition(long position,bool reader,
            out int bufferOffset,out int bufferIndex)
        {
            byte[] buffer = null;

            // get bufferArray and firstBuffer position based
            // on if being called by the reader or the writer.
            ArrayList bufferArray; // arraylist of buffers
            long firstBufferPosition; // absolute file position of first buffer in the arrayList


            // Ensure that while buffer and buffer position are stable while calculating
            // buffer offsets and retrieving the buffer.  The tokenizer thread can call
            // CheckIfCanRemoveFromArrayList, which uses this same lock when modifying the
            // writer buffer.
            _bufferLock.AcquireWriterLock(-1);

            if (reader)
            {
                bufferArray = ReaderBufferArrayList;
                firstBufferPosition = ReaderFirstBufferPosition;
            }
            else
            {
                bufferArray = WriterBufferArrayList;
                firstBufferPosition =  WriterFirstBufferPosition;
            }

            // calc get the bufferIndex
            bufferIndex = (int) ((position - firstBufferPosition)/BufferSize);

            // calc the byte offset in the buffer for the position
            bufferOffset =
                (int) ((position - firstBufferPosition) - (bufferIndex*BufferSize));

            Debug.Assert(bufferOffset < BufferSize,"Calculated bufferOffset is greater than buffer");

            // check if we need to allocate a new buffer.
            if (bufferArray.Count <= bufferIndex)
            {
                Debug.Assert(bufferArray.Count == bufferIndex,"Need to allocate more than one buffer");
                Debug.Assert(false == reader,"Allocating a buffer on Read");

                buffer = new byte[BufferSize];

                // add to both the reader and writer ArrayLists.
                ReaderBufferArrayList.Add(buffer);
                WriterBufferArrayList.Add(buffer);
            }
            else
            {
                // just use the buffer that is there.
                buffer =  bufferArray[bufferIndex] as byte[];
            }
            _bufferLock.ReleaseWriterLock();

            return buffer;
        }

        /// <summary>
        /// helper function called by to check is any memory buffers
        /// can be safely removed.
        /// </summary>
        /// <param name="position">Absolute File Position</param>
        /// <param name="arrayList">ArrayList containing the Memory</param>
        /// <param name="firstBufferPosition">If any arrays are cleaned up returns the
        /// updated position that the first array in the buffer starts at</param>
         void CheckIfCanRemoveFromArrayList(long position,ArrayList arrayList,ref long firstBufferPosition)
        {
            // see if there are any buffers we can get rid of.
            int bufferIndex = (int) ((position - firstBufferPosition)/BufferSize);

            if (bufferIndex > 0)
            {
                // we can safely remove all previous buffers from the ArrayList.
                int numBuffersToRemove = bufferIndex;

                // Ensure that while modifying the buffer position and buffer list that
                // another thread can't get partially updated information while
                // calling GetBufferFromFilePosition().
                _bufferLock.AcquireWriterLock(-1);

                // update buffer position offset for number of buffers to be
                // removed.
                firstBufferPosition += numBuffersToRemove*BufferSize;

                arrayList.RemoveRange(0,bufferIndex);

                _bufferLock.ReleaseWriterLock();
            }
        }

        #endregion PrivateMethods


        #region Properties

        /// <summary>
        /// WriterStream instance
        /// </summary>
        internal WriterStream WriterStream
        {
            get { return _writerStream; }
        }

        /// <summary>
        /// ReaderStream instance
        /// </summary>
        internal ReaderStream ReaderStream
        {
            get { return _readerStream; }
        }

        /// <summary>
        /// Current position inthe Reader stream
        /// </summary>
        internal long ReadPosition
        {
            get { return _readPosition; }
            set { _readPosition = value; }
        }


        /// <summary>
        /// Length of the Reader stream
        /// </summary>
        internal long ReadLength
        {
            get { return _readLength; }
            set { _readLength = value; }
        }

        /// <summary>
        /// Current position in the writer stream
        /// </summary>
        internal long WritePosition
        {
            get { return _writePosition ; }
            set { _writePosition = value; }
        }

        /// <summary>
        /// current length of the writer stream
        /// </summary>
        internal long WriteLength
        {
            get { return _writeLength ; }
            set { _writeLength = value; }
        }


        /// <summary>
        /// Constant Buffer size to be used for all allocated buffers
        /// </summary>
        int BufferSize
        {
            get { return _bufferSize; }
        }


        /// <summary>
        /// File Position that the first buffer in the Readers array of buffer starts at
        /// </summary>
        long ReaderFirstBufferPosition
        {
            get { return _readerFirstBufferPosition ; }
            set { _readerFirstBufferPosition = value; }
        }

        /// <summary>
        /// File Position that the first buffer in the Writers array of buffer starts at
        /// </summary>
        long WriterFirstBufferPosition
        {
            get { return _writerFirstBufferPosition ; }
            set { _writerFirstBufferPosition = value; }
        }

        /// <summary>
        /// ArrayList containing all the buffers used by the Reader
        /// </summary>
        ArrayList ReaderBufferArrayList
        {
            get { return _readerBufferArrayList ; }
            set { _readerBufferArrayList = value; }
        }

        /// <summary>
        /// ArrayList of all the buffers used by the Writer.
        /// </summary>
        ArrayList WriterBufferArrayList
        {
            get { return _writerBufferArrayList ; }
            set { _writerBufferArrayList = value; }
        }

#if DEBUG
        /// <summary>
        /// Set when all bytes have been written
        /// </summary>
        internal bool WriteComplete
        {
            get { return _writeComplete; }
        }
#endif
        #endregion Properties


        #region Data

        long _readPosition;
        long _readLength;
        long _writePosition;
        long _writeLength;

        ReaderWriterLock _bufferLock;

        WriterStream _writerStream;
        ReaderStream _readerStream;
        long _readerFirstBufferPosition;
        long _writerFirstBufferPosition;
        ArrayList _readerBufferArrayList;
        ArrayList _writerBufferArrayList;
#if DEBUG
        bool _writeComplete;
#endif

        // size of each allocated buffer.
        private const int _bufferSize = 4096;

        #endregion Data
    }

    /// <summary>
    /// Writer Stream class.
    /// This is the Stream implementation the Writer sees.
    /// </summary>
    internal class WriterStream : Stream
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="streamManager">StreamManager that the writer stream should use</param>
        internal WriterStream(ReadWriteStreamManager streamManager)
        {
            _streamManager = streamManager;
        }

        #endregion Constructor

        #region overrides

        /// <summary>
        /// Override of Stream.CanRead
        /// </summary>
        public override bool CanRead
        {
            get { return false; }
        }

        /// <summary>
        /// Override of Stream.CanSeek
        /// </summary>
        public override bool CanSeek
        {
            get { return true; }
        }

        /// <summary>
        /// Override of Stream.CanWrite
        /// </summary>
        public override bool CanWrite
        {
            get { return true; }
        }

        /// <summary>
        /// Override of Stream.Close
        /// </summary>
        public override void Close()
        {
             StreamManager.WriterClose();
        }

        /// <summary>
        /// Override of Stream.Flush
        /// </summary>
        public override void Flush()
        {
            return; // nothing to Flush
        }

        /// <summary>
        /// Override of Stream.Length
        /// </summary>
        public override long Length
        {
            get
            {
                return StreamManager.WriteLength;
            }
        }

        /// <summary>
        /// Override of Stream.Position
        /// </summary>
        public override long Position
        {
            get
            {
                return -1;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Override of Stream.Read
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Override of Stream.ReadByte
        /// </summary>
        public override int ReadByte()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Override of Stream.Seek
        /// </summary>
        public override long Seek(long offset, SeekOrigin loc)
        {
            return StreamManager.WriterSeek(offset,loc);
        }

        /// <summary>
        /// Override of Stream.SetLength
        /// </summary>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }


        /// <summary>
        /// Override of Stream.Write
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            StreamManager.Write(buffer,offset,count);
        }

        #endregion overrides


        #region Methods

        /// <summary>
        /// Called by the writer to say its okay to let the reader see what
        /// it has written up to the position.
        /// </summary>
        /// <param name="position">Absolute position inthe file</param>
        internal void UpdateReaderLength(long position)
        {
            StreamManager.UpdateReaderLength(position);
        }


        #endregion Methods

        #region Properties

        /// <summary>
        /// StreamManager for the writer stream
        /// </summary>
        ReadWriteStreamManager StreamManager
        {
            get { return _streamManager; }
        }


        #endregion Properties

        #region Data

        ReadWriteStreamManager _streamManager;

        #endregion Data
    }


    ///  <summary>
    ///  Reader Stream class
    /// This is the Stream implementation the Writer sees.
    /// </summary>
    internal class ReaderStream : Stream
    {
        #region Constructor

        internal ReaderStream(ReadWriteStreamManager streamManager)
        {
            _streamManager = streamManager;
        }

        #endregion Constructor

        #region overrides

        /// <summary>
        /// Override of Stream.CanRead
        /// </summary>
        public override bool CanRead
        {
            get { return true; }
        }

        /// <summary>
        /// Override of Stream.CanSeek
        /// </summary>
        public override bool CanSeek
        {
            get { return true; }
        }

        /// <summary>
        /// Override of Stream.CanWrite
        /// </summary>
        public override bool CanWrite
        {
            get { return false; }
        }

        /// <summary>
        /// Override of Stream.Close
        /// </summary>
        public override void Close()
        {
            Debug.Assert(false,"Close called on ReaderStream");
        }

        /// <summary>
        /// Override of Stream.Flush
        /// </summary>
        public override void Flush()
        {
            Debug.Assert(false,"Flush called on ReaderStream");
        }

        /// <summary>
        /// Override of Stream.Length
        /// </summary>
        public override long Length
        {
            get
            {
                return StreamManager.ReadLength;
            }
        }

        /// <summary>
        /// Override of Stream.Position
        /// </summary>
        public override long Position
        {
            get
            {
                return StreamManager.ReadPosition;
            }
            set
            {
                StreamManager.ReaderSeek(value,SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Override of Stream.Read
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return StreamManager.Read(buffer,offset,count);
        }

        /// <summary>
        /// Override of Stream.ReadByte
        /// </summary>
        public override int ReadByte()
        {
            return StreamManager.ReadByte();
        }


        /// <summary>
        /// Override of Stream.Seek
        /// </summary>
        public override long Seek(long offset, SeekOrigin loc)
        {
            return StreamManager.ReaderSeek(offset,loc);
        }

        /// <summary>
        /// Override of Stream.SetLength
        /// </summary>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }


        /// <summary>
        /// Override of Stream.Write
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
             throw new NotSupportedException();
        }

        #endregion Overrides

        #region Methods

        /// <summary>
        /// Called by the reader to indicate all bytes up to and
        /// including the position are no longer needed
        /// After making this call the reader cannot go back and
        /// read this data
        /// </summary>
        /// <param name="position"></param>
        internal void ReaderDoneWithFileUpToPosition(long position)
        {
            StreamManager.ReaderDoneWithFileUpToPosition(position);
        }

#if DEBUG
        internal bool IsWriteComplete
        {
             get { return StreamManager.WriteComplete; }
        }
#endif
        #endregion Methods

        #region Properties

        /// <summary>
        /// StreamManager that this class should use
        /// </summary>
        ReadWriteStreamManager StreamManager
        {
            get { return _streamManager; }
        }

        #endregion Properties

        #region Data

        ReadWriteStreamManager _streamManager;

        #endregion Data
    }
}
