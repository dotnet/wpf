// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This program uses code hyperlinks available as part of the HyperAddin Visual Studio plug-in.
// It is available from http://www.codeplex.com/hyperAddin 
// 
using System;
using System.Text;      // For StringBuilder.
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using DeferedStreamLabel = Microsoft.Test.EventTracing.FastSerialization.StreamLabel;
using Microsoft.Test.EventTracing.FastSerialization;

namespace Microsoft.Test.EventTracing
{
    /// <summary>
    /// A MemoryStreamReader is an implementation of the IStreamReader interface that works over a given byte[] array.  
    /// </summary>
    class MemoryStreamReader : IStreamReader
    {
        public MemoryStreamReader(byte[] data) : this(data, 0, data.Length) { }
        public MemoryStreamReader(byte[] data, int start, int length)
        {
            bytes = data;
            position = start;
            endPosition = length;
        }
        public byte ReadByte()
        {
            if (position >= endPosition)
                Fill(1);
            return bytes[position++];
        }
        public short ReadInt16()
        {
            if (position + sizeof(short) > endPosition)
                Fill(sizeof(short));
            int ret = bytes[position] + (bytes[position + 1] << 8);
            position += sizeof(short);
            return (short)ret;
        }
        public int ReadInt32()
        {
            if (position + sizeof(int) > endPosition)
                Fill(sizeof(int));
            int ret = bytes[position] + ((bytes[position + 1] + ((bytes[position + 2] + (bytes[position + 3] << 8)) << 8)) << 8);
            position += sizeof(int);
            return ret;
        }
        public StreamLabel ReadLabel()
        {
            return (StreamLabel)ReadInt32();
        }
        public virtual void GotoSuffixLabel()
        {
            Goto((StreamLabel)(Length - sizeof(StreamLabel)));
            Goto(ReadLabel());
        }

        public long ReadInt64()
        {
            if (position + sizeof(long) > endPosition)
                Fill(sizeof(long));
            uint low = (uint)ReadInt32();
            uint high = (uint)ReadInt32();
            return (long)((((ulong)high) << 32) + low);        // TODO find the most efficient way of doing this. 
        }
        public string ReadString()
        {
            if (sb == null)
                sb = new StringBuilder();
            sb.Length = 0;

            int len = ReadInt32();          // Expect first a character inclusiveCountRet.  -1 means null.
            if (len < 0)
            {
                Debug.Assert(len == -1);
                return null;
            }

            Debug.Assert(len < Length);
            while (len > 0)
            {
                char c = (char)ReadByte();   // TODO do real UTF8 decode. 
                sb.Append(c);
                --len;
            }
            return sb.ToString();
        }
        public virtual void Goto(StreamLabel label)
        {
            position = (int)label;
        }
        public virtual StreamLabel Current
        {
            get
            {
                return (StreamLabel)position;
            }
        }
        public virtual long Length { get { return endPosition; } }
        public virtual void Skip(int byteCount)
        {
            Goto((StreamLabel)((int)Current + byteCount));
        }
        void IDisposable.Dispose() { }
        protected virtual void Fill(int minBytes)
        {
            throw new Exception("Streamreader read past end of buffer");
        }
        protected byte[] bytes;
        protected int position;
        protected int endPosition;
        private StringBuilder sb;
    }

    /// <summary>
    /// A StreamWriter is an implementation of the IStreamWriter interface that generates a byte[] array. 
    /// </summary>
    class MemoryStreamWriter : IStreamWriter
    {
        public MemoryStreamWriter() : this(64) { }
        public MemoryStreamWriter(int size)
        {
            bytes = new byte[size];
        }

        public virtual long Length { get { return endPosition; } }
        public virtual void Clear() { endPosition = 0; }

        public void Write(byte value)
        {
            if (endPosition >= bytes.Length)
                MakeSpace();
            bytes[endPosition++] = value;
        }
        public void Write(short value)
        {
            if (endPosition + sizeof(short) > bytes.Length)
                MakeSpace();
            int intValue = value;
            bytes[endPosition++] = (byte)intValue; intValue = intValue >> 8;
            bytes[endPosition++] = (byte)intValue; intValue = intValue >> 8;
        }
        public void Write(int value)
        {
            if (endPosition + sizeof(int) > bytes.Length)
                MakeSpace();
            bytes[endPosition++] = (byte)value; value = value >> 8;
            bytes[endPosition++] = (byte)value; value = value >> 8;
            bytes[endPosition++] = (byte)value; value = value >> 8;
            bytes[endPosition++] = (byte)value; value = value >> 8;
        }
        public void Write(long value)
        {
            if (endPosition + sizeof(long) > bytes.Length)
                MakeSpace();
            bytes[endPosition++] = (byte)value; value = value >> 8;
            bytes[endPosition++] = (byte)value; value = value >> 8;
            bytes[endPosition++] = (byte)value; value = value >> 8;
            bytes[endPosition++] = (byte)value; value = value >> 8;
            bytes[endPosition++] = (byte)value; value = value >> 8;
            bytes[endPosition++] = (byte)value; value = value >> 8;
            bytes[endPosition++] = (byte)value; value = value >> 8;
            bytes[endPosition++] = (byte)value; value = value >> 8;
        }
        public void Write(StreamLabel value)
        {
            Write((int)value);
        }
        public void Write(string value)
        {
            if (value == null)
            {
                Write(-1);          // negative charCount means null. 
            }
            else
            {
                Write(value.Length);
                for (int i = 0; i < value.Length; i++)
                {
                    // TODO do actual UTF8
                    Write((byte)value[i]);
                }
            }
        }
        public virtual StreamLabel GetLabel()
        {
            return (StreamLabel)Length;
        }
        public void WriteSuffixLabel(StreamLabel value)
        {
            // This is guarenteed to be uncompressed, but since we are not compressing anything, we can
            // simply write the value.  
            Write(value);
        }

        public void WriteToStream(Stream outputStream)
        {
            // TODO really big streams will overflow;
            outputStream.Write(bytes, 0, (int)Length);
        }
        // Note that the returned MemoryStreamReader is not valid if more writes are done.  
        public MemoryStreamReader GetReader() { return new MemoryStreamReader(bytes); }
        public void Dispose() { }

        #region private
        protected virtual void MakeSpace()
        {
            byte[] newBytes = new byte[bytes.Length * 3 / 2];
            Array.Copy(bytes, newBytes, bytes.Length);
            bytes = newBytes;
        }
        protected byte[] bytes;
        protected int endPosition;
        #endregion
    }



    /// <summary>
    /// A IOStreamStreamWriter hooks a MemoryStreamWriter up to an output System.IO.Stream
    /// </summary>
    class IOStreamStreamWriter : MemoryStreamWriter, IDisposable
    {
        public IOStreamStreamWriter(string fileName) : this(new FileStream(fileName, FileMode.Create)) { }
        public IOStreamStreamWriter(Stream outputStream) : this(outputStream, defaultBufferSize + sizeof(long)) { }
        public IOStreamStreamWriter(Stream outputStream, int bufferSize)
            : base(bufferSize)
        {
            this.outputStream = outputStream;
        }

        public void Flush()
        {
            outputStream.Write(bytes, 0, endPosition);
            endPosition = 0;
            outputStream.Flush();
        }
        public void Close()
        {
            Flush();
            outputStream.Close();
        }
        public override long Length
        {
            get
            {
                return base.Length + outputStream.Length;
            }
        }
        public override StreamLabel GetLabel()
        {
            long len = Length;
            if (len != (uint)len)
                throw new NotSupportedException("Streams larger than 4Gig");
            return (StreamLabel)len;
        }
        public override void Clear()
        {
            outputStream.SetLength(0);
        }

        #region private
        protected override void MakeSpace()
        {
            Debug.Assert(endPosition > bytes.Length - sizeof(long));
            outputStream.Write(bytes, 0, endPosition);
            endPosition = 0;
        }
        void IDisposable.Dispose()
        {
            Close();
        }

        const int defaultBufferSize = 1024 * 8 - sizeof(long);
        Stream outputStream;
        #endregion
    }

    /// <summary>
    /// A IOStreamStreamReader hooks a MemoryStreamReader up to an input System.IO.Stream.  
    /// </summary>
    class IOStreamStreamReader : MemoryStreamReader, IDisposable
    {
        public IOStreamStreamReader(string fileName) : this(new FileStream(fileName, FileMode.Open)) { }
        public IOStreamStreamReader(Stream inputStream) : this(inputStream, defaultBufferSize) { }
        public IOStreamStreamReader(Stream inputStream, int bufferSize)
            : base(new byte[bufferSize + align], 0, 0)
        {
            Debug.Assert(bufferSize % align == 0);
            this.inputStream = inputStream;
        }
        public override StreamLabel Current
        {
            get
            {
                return (StreamLabel)(positionInStream + position);
            }
        }
        public override void Goto(StreamLabel label)
        {
            uint offset = (uint)label - positionInStream;
            if (offset > (uint)endPosition)
            {
                positionInStream = (uint)label;
                position = endPosition = 0;
            }
            else
                position = (int)offset;
        }

        public override long Length { get { return inputStream.Length; } }
        public void Close()
        {
            inputStream.Close();
        }

        #region private
        protected const int align = 8;        // Needs to be a power of 2
        protected const int defaultBufferSize = 0x4000;  // 16K 

        /// <summary>
        /// Fill the buffer, making sure at least 'minimum' byte are available to read.  Throw an exception
        /// if there are not that many bytes.  
        /// </summary>
        /// <param name="minimum"></param>
        protected override void Fill(int minimum)
        {
            if (endPosition != position)
            {
                int slideAmount = position & ~(align - 1);             // round down to stay aligned.  
                for (int i = slideAmount; i < endPosition; i++)        // Slide everything down.  
                    bytes[i - slideAmount] = bytes[i];
                endPosition -= slideAmount;
                position -= slideAmount;
                positionInStream += (uint)slideAmount;
            }
            else
            {
                positionInStream += (uint) position;
                endPosition = 0;
                position = 0;
                // if you are within one read of the end of file, go backward to read the whole block.  
                uint lastBlock = (uint)(((int)inputStream.Length - bytes.Length + align) & ~(align - 1));
                if (positionInStream >= lastBlock)
                    position = (int)(positionInStream - lastBlock);
                else
                    position = (int)positionInStream & (align - 1);
                positionInStream -= (uint)position;
            }

            Debug.Assert(positionInStream % align == 0);
            lock (inputStream)
            {
                inputStream.Seek(positionInStream + endPosition, SeekOrigin.Begin);
                for (; ; )
                {
                    int count = inputStream.Read(bytes, endPosition, bytes.Length - endPosition);
                    if (count == 0)
                        break;

                    endPosition += count;
                    if (endPosition == bytes.Length)
                        break;
                }
            }
            if (endPosition - position < minimum)
                throw new Exception("Read past end of stream.");
        }
        void IDisposable.Dispose()
        {
            Close();
        }

        protected Stream inputStream;
        protected uint positionInStream;
        #endregion
    }
    unsafe sealed class PinnedStreamReader : IOStreamStreamReader
    {
        public PinnedStreamReader(string fileName) : this(new FileStream(fileName, FileMode.Open, FileAccess.Read)) { }
        public PinnedStreamReader(Stream inputStream) : this(inputStream, defaultBufferSize) { }
        public PinnedStreamReader(Stream inputStream, int bufferSize)
            : base(inputStream, bufferSize)
        {
            // Pin the array
            pinningHandle = System.Runtime.InteropServices.GCHandle.Alloc(bytes, System.Runtime.InteropServices.GCHandleType.Pinned);
            fixed (byte* bytesAsPtr = &bytes[0])
                bufferStart = bytesAsPtr;
        }

        public PinnedStreamReader Clone()
        {
            PinnedStreamReader ret = new PinnedStreamReader(inputStream, bytes.Length - align);
            return ret;
        }

        public unsafe TraceEventNativeMethods.EVENT_RECORD* GetPointer(StreamLabel Position, int length)
        {
            Goto(Position);
            return GetPointer(length);
        }
        public unsafe TraceEventNativeMethods.EVENT_RECORD* GetPointer(int length)
        {
            if (position + length > endPosition)
                Fill(length);
#if DEBUG
            fixed (byte* bytesAsPtr = &bytes[0])
                Debug.Assert(bytesAsPtr == bufferStart, "Error, buffer not pinnned");
            Debug.Assert(position < bytes.Length);
#endif
            return (TraceEventNativeMethods.EVENT_RECORD*)(&bufferStart[position]);
        }

        #region private
        private System.Runtime.InteropServices.GCHandle pinningHandle;
        byte* bufferStart;
        #endregion
    }
}
