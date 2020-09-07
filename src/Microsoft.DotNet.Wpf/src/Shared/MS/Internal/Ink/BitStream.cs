// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Utility;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Diagnostics;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Ink
{
    /// <summary>
    /// A stream-style reader for retrieving packed bits from a byte array
    /// </summary>
    /// <remarks>This bits should packed into the leftmost position in each byte.
    /// For compatibility purposes with the v1 ISF encoder and decoder, the order of the
    /// packing must not be changed. This code is a from-scratch rewrite of the BitStream
    /// natice C++ class in the v1 Ink code, but still maintaining the same packing
    /// behavior.</remarks>
    internal class BitStreamReader
    {
        /// <summary>
        /// Create a new BitStreamReader to unpack the bits in a buffer of bytes
        /// </summary>
        /// <param name="buffer">Buffer of bytes</param>
        internal BitStreamReader(byte[] buffer)
        {
            Debug.Assert(buffer != null);

            _byteArray = buffer;
            _bufferLengthInBits = (uint)buffer.Length * (uint)Native.BitsPerByte;
        }

        /// <summary>
        /// Create a new BitStreamReader to unpack the bits in a buffer of bytes
        /// </summary>
        /// <param name="buffer">Buffer of bytes</param>
        /// <param name="startIndex">The index to start reading at</param>
        internal BitStreamReader(byte[] buffer, int startIndex)
        {
            Debug.Assert(buffer != null);

            if (startIndex < 0 || startIndex >= buffer.Length)
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }
            _byteArray = buffer;
            _byteArrayIndex = startIndex;
            _bufferLengthInBits = (uint)(buffer.Length - startIndex) * (uint)Native.BitsPerByte;
        }

        /// <summary>
        /// Create a new BitStreamReader to unpack the bits in a buffer of bytes
        /// and enforce a maximum buffer read length
        /// </summary>
        /// <param name="buffer">Buffer of bytes</param>
        /// <param name="bufferLengthInBits">Maximum number of bytes to read from the buffer</param>
        internal BitStreamReader(byte[] buffer, uint bufferLengthInBits)
            : this(buffer)
        {
            if (bufferLengthInBits > (buffer.Length * Native.BitsPerByte))
            {
                throw new ArgumentOutOfRangeException("bufferLengthInBits", SR.Get(SRID.InvalidBufferLength));
            }

            _bufferLengthInBits = bufferLengthInBits;
        }

        /// <summary>
        /// Read a specified number of bits from the stream into a long
        /// </summary>
        internal long ReadUInt64(int countOfBits)
        {
            // we only support 1-64 bits currently, not multiple bytes, and not 0 bits
            if (countOfBits > Native.BitsPerLong || countOfBits <= 0)
            {
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, SR.Get(SRID.CountOfBitsOutOfRange));
            }
            long retVal = 0;
            while (countOfBits > 0)
            {
                int countToRead = (int)Native.BitsPerByte;
                if (countOfBits < 8)
                {
                    countToRead = countOfBits;
                }
                //make room
                retVal <<= countToRead;
                byte b = ReadByte(countToRead);
                retVal |= (long)b;
                countOfBits -= countToRead;
            }
            return retVal;
        }

        /// <summary>
        /// Read a single UInt16 from the byte[]
        /// </summary>
        /// <param name="countOfBits"></param>
        /// <returns></returns>
        internal ushort ReadUInt16(int countOfBits)
        {
            // we only support 1-16 bits currently, not multiple bytes, and not 0 bits
            if (countOfBits > Native.BitsPerShort || countOfBits <= 0)
            {
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, SR.Get(SRID.CountOfBitsOutOfRange));
            }

            ushort retVal = 0;
            while (countOfBits > 0)
            {
                int countToRead = (int)Native.BitsPerByte;
                if (countOfBits < 8)
                {
                    countToRead = countOfBits;
                }
                //make room
                retVal <<= countToRead;
                byte b = ReadByte(countToRead);
                retVal |= (ushort)b;
                countOfBits -= countToRead;
            }
            return retVal;
        }

        /// <summary>
        /// Read a specified number of bits from the stream in reverse byte order
        /// </summary>
        internal uint ReadUInt16Reverse(int countOfBits)
        {
            // we only support 1-8 bits currently, not multiple bytes, and not 0 bits
            if (countOfBits > Native.BitsPerShort|| countOfBits <= 0)
            {
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, SR.Get(SRID.CountOfBitsOutOfRange));
            }

            ushort retVal = 0;
            int fullBytesRead = 0;
            while (countOfBits > 0)
            {
                int countToRead = (int)Native.BitsPerByte;
                if (countOfBits < 8)
                {
                    countToRead = countOfBits;
                }
                //make room
                ushort b = (ushort)ReadByte(countToRead);
                b <<= (fullBytesRead * Native.BitsPerByte);
                retVal |= b;
                fullBytesRead++;
                countOfBits -= countToRead;
            }
            return retVal;
        }

        /// <summary>
        /// Read a specified number of bits from the stream into a single byte
        /// </summary>
        internal uint ReadUInt32(int countOfBits)
        {
            // we only support 1-8 bits currently, not multiple bytes, and not 0 bits
            if (countOfBits > Native.BitsPerInt || countOfBits <= 0)
            {
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, SR.Get(SRID.CountOfBitsOutOfRange));
            }

            uint retVal = 0;
            while (countOfBits > 0)
            {
                int countToRead = (int)Native.BitsPerByte;
                if (countOfBits < 8)
                {
                    countToRead = countOfBits;
                }
                //make room
                retVal <<= countToRead;
                byte b = ReadByte(countToRead);
                retVal |= (uint)b;
                countOfBits -= countToRead;
            }
            return retVal;
        }

        /// <summary>
        /// Read a specified number of bits from the stream in reverse byte order
        /// </summary>
        internal uint ReadUInt32Reverse(int countOfBits)
        {
            // we only support 1-8 bits currently, not multiple bytes, and not 0 bits
            if (countOfBits > Native.BitsPerInt || countOfBits <= 0)
            {
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, SR.Get(SRID.CountOfBitsOutOfRange));
            }

            uint retVal = 0;
            int fullBytesRead = 0;
            while (countOfBits > 0)
            {
                int countToRead = (int)Native.BitsPerByte;
                if (countOfBits < 8)
                {
                    countToRead = countOfBits;
                }
                //make room
                uint b = (uint)ReadByte(countToRead);
                b <<= (fullBytesRead * Native.BitsPerByte);
                retVal |= b;
                fullBytesRead++;
                countOfBits -= countToRead;
            }
            return retVal;
        }

        /// <summary>
        /// Reads a single bit from the buffer
        /// </summary>
        /// <returns></returns>
        internal bool ReadBit()
        {
            byte b = ReadByte(1);
            return ((b & 1) == 1);
        }

        /// <summary>
        /// Read a specified number of bits from the stream into a single byte
        /// </summary>
        /// <param name="countOfBits">The number of bits to unpack</param>
        /// <returns>A single byte that contains up to 8 packed bits</returns>
        /// <remarks>For example, if 2 bits are read from the stream, then a full byte
        /// will be created with the least significant bits set to the 2 unpacked bits
        /// from the stream</remarks>
        internal byte ReadByte(int countOfBits)
        {
            // if the end of the stream has been reached, then throw an exception
            if (EndOfStream)
            {
                throw new System.IO.EndOfStreamException(SR.Get(SRID.EndOfStreamReached));
            }

            // we only support 1-8 bits currently, not multiple bytes, and not 0 bits
            if (countOfBits > Native.BitsPerByte || countOfBits <= 0)
            {
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, SR.Get(SRID.CountOfBitsOutOfRange));
            }

            if (countOfBits > _bufferLengthInBits)
            {
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, SR.Get(SRID.CountOfBitsGreatThanRemainingBits));
            }

            _bufferLengthInBits -= (uint)countOfBits;

            // initialize return byte to 0 before reading from the cache
            byte returnByte = 0;

            // if the partial bit cache contains more bits than requested, then read the
            //      cache only
            if (_cbitsInPartialByte >= countOfBits)
            {
                // retrieve the requested count of most significant bits from the cache
                //      and store them in the least significant positions in the return byte
                int rightShiftPartialByteBy = Native.BitsPerByte - countOfBits;
                returnByte = (byte)(_partialByte >> rightShiftPartialByteBy);

                // reposition any unused portion of the cache in the most significant part of the bit cache
                unchecked // disable overflow checking since we are intentionally throwing away
                            //  the significant bits
                {
                    _partialByte <<= countOfBits;
                }
                // update the bit count in the cache
                _cbitsInPartialByte -= countOfBits;
            }
                // otherwise, we need to retrieve more full bytes from the stream
            else
            {
                // retrieve the next full byte from the stream
                byte nextByte = _byteArray[_byteArrayIndex];
                _byteArrayIndex++;

                //right shift partial byte to get it ready to or with the partial next byte
                int rightShiftPartialByteBy = Native.BitsPerByte - countOfBits;
                returnByte = (byte)(_partialByte >> rightShiftPartialByteBy);

                // now copy the remaining chunk of the newly retrieved full byte
                int rightShiftNextByteBy = Math.Abs((countOfBits - _cbitsInPartialByte) - Native.BitsPerByte);
                returnByte |= (byte)(nextByte >> rightShiftNextByteBy);

                // update the partial bit cache with the remainder of the newly retrieved full byte
                unchecked // disable overflow checking since we are intentionally throwing away
                            //  the significant bits
                {
                    _partialByte = (byte)(nextByte << (countOfBits - _cbitsInPartialByte));
                }

                _cbitsInPartialByte = Native.BitsPerByte - (countOfBits - _cbitsInPartialByte);
            }
            return returnByte;
        }

        /// <summary>
        /// Since the return value of Read cannot distinguish between valid and invalid
        /// data (e.g. 8 bits set), the EndOfStream property detects when there is no more
        /// data to read.
        /// </summary>
        /// <value>True if stream end has been reached</value>
        internal bool EndOfStream
        {
            get
            {
                return 0 == _bufferLengthInBits;
            }
        }

        /// <summary>
        /// The current read index in the array
        /// </summary>
        internal int CurrentIndex
        {
            get
            {
                //_byteArrayIndex is always advanced to the next index
                // so we always decrement before returning
                return _byteArrayIndex - 1;
            }
        }


        // Privates
        // reference to the source byte buffer to read from
        private byte[] _byteArray = null;

        // maximum length of buffer to read in bits
        private uint _bufferLengthInBits = 0;

            // the index in the source buffer for the next byte to be read
        private int _byteArrayIndex = 0;

            // since the bits from multiple inputs can be packed into a single byte
            //  (e.g. 2 bits per input fits 4 per byte), we use this field as a cache
            //  of the remaining partial bits.
        private byte _partialByte = 0;

            // the number of bits (partial byte) left to read in the overlapped byte field
        private int _cbitsInPartialByte = 0;
    }

    /// <summary>
    /// A stream-like writer for packing bits into a byte buffer
    /// </summary>
    /// <remarks>This class is to be used with the BitStreamReader for reading
    /// and writing bytes. Note that the bytes should be read in the same order
    /// and lengths as they were written to retrieve the same values.
    /// See remarks in BitStreamReader regarding compatibility with the native C++
    /// BitStream class.</remarks>
    internal class BitStreamWriter
    {
        /// <summary>
        /// Create a new bit writer that writes to the target buffer
        /// </summary>
        /// <param name="bufferToWriteTo"></param>
        internal BitStreamWriter(List<byte> bufferToWriteTo)
        {
            if (bufferToWriteTo == null)
            {
                throw new ArgumentNullException("bufferToWriteTo");
            }
            _targetBuffer = bufferToWriteTo;
        }

        /// <summary>
        /// Writes the count of bits from the int to the left packed buffer
        /// </summary>
        /// <param name="bits"></param>
        /// <param name="countOfBits"></param>
        internal void Write(uint bits, int countOfBits)
        {
            // validate that a subset of the bits in a single byte are being written
            if (countOfBits <= 0 || countOfBits > Native.BitsPerInt)
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, SR.Get(SRID.CountOfBitsOutOfRange));


            // calculate the number of full bytes
            //   Example: 10 bits would require 1 full byte
            int fullBytes = countOfBits / Native.BitsPerByte;

            // calculate the number of bits that spill beyond the full byte boundary
            //   Example: 10 buttons would require 2 extra bits (8 fit in a full byte)
            int bitsToWrite = countOfBits % Native.BitsPerByte;

            for (; fullBytes >= 0; fullBytes--)
            {
                byte byteOfData = (byte)(bits >> (fullBytes * Native.BitsPerByte));
                //
                // write 8 or less bytes to the bitwriter
                // checking for 0 handles the case where we're writing 8, 16 or 24 bytes
                // and bitsToWrite is initialize to zero
                //
                if (bitsToWrite > 0)
                {
                    Write(byteOfData, bitsToWrite);
                }
                if (fullBytes > 0)
                {
                    bitsToWrite = Native.BitsPerByte;
                }
            }
        }

        /// <summary>
        /// Writes the count of bits from the int to the buffer in reverse order
        /// </summary>
        /// <param name="bits"></param>
        /// <param name="countOfBits"></param>
        internal void WriteReverse(uint bits, int countOfBits)
        {
            // validate that a subset of the bits in a single byte are being written
            if (countOfBits <= 0 || countOfBits > Native.BitsPerInt)
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, SR.Get(SRID.CountOfBitsOutOfRange));

            // calculate the number of full bytes
            //   Example: 10 bits would require 1 full byte
            int fullBytes = countOfBits / Native.BitsPerByte;

            // calculate the number of bits that spill beyond the full byte boundary
            //   Example: 10 buttons would require 2 extra bits (8 fit in a full byte)
            int bitsToWrite = countOfBits % Native.BitsPerByte;
            if (bitsToWrite > 0)
            {
                fullBytes++;
            }
            for (int x = 0; x < fullBytes; x++)
            {
                byte byteOfData = (byte)(bits >> (x * Native.BitsPerByte));
                Write(byteOfData, Native.BitsPerByte);
            }
        }

        /// <summary>
        /// Write a specific number of bits from byte input into the stream
        /// </summary>
        /// <param name="bits">The byte to read the bits from</param>
        /// <param name="countOfBits">The number of bits to read</param>
        internal void Write(byte bits, int countOfBits)
        {
            // validate that a subset of the bits in a single byte are being written
            if (countOfBits <= 0 || countOfBits > Native.BitsPerByte)
                throw new ArgumentOutOfRangeException("countOfBits", countOfBits, SR.Get(SRID.CountOfBitsOutOfRange));

            byte buffer;
                // if there is remaining bits in the last byte in the stream
                //      then use those first
            if (_remaining > 0)
            {
                // retrieve the last byte from the stream, update it, and then replace it
                buffer = _targetBuffer[_targetBuffer.Count - 1];
                    // if the remaining bits aren't enough then just copy the significant bits
                    //      of the input into the remainder
                if (countOfBits > _remaining)
                {
                    buffer |= (byte)((bits & (0xFF >> (Native.BitsPerByte - countOfBits))) >> (countOfBits - _remaining));
                }
                    // otherwise, copy the entire set of input bits into the remainder
                else
                {
                    buffer |= (byte)((bits & (0xFF >> (Native.BitsPerByte - countOfBits))) << (_remaining - countOfBits));
                }
                _targetBuffer[_targetBuffer.Count - 1] = buffer;
            }

            // if the remainder wasn't large enough to hold the entire input set
            if (countOfBits > _remaining)
            {
                  // then copy the uncontained portion of the input set into a temporary byte
                _remaining = Native.BitsPerByte - (countOfBits - _remaining);
                unchecked // disable overflow checking since we are intentionally throwing away
                            //  the significant bits
                {
                    buffer = (byte)(bits << _remaining);
                }
                    // and add it to the target buffer
                _targetBuffer.Add(buffer);
            }
            else
            {
                // otherwise, simply update the amount of remaining bits we have to spare
                _remaining -= countOfBits;
            }
        }


        // the buffer that the bits are written into
        private List<byte> _targetBuffer = null;

        // number of free bits remaining in the last byte added to the target buffer
        private int _remaining = 0;
    }
}
