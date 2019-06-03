// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Utility;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Ink;
using MS.Internal.Ink.InkSerializedFormat;
using System.Collections.Generic;
using System.Diagnostics;


using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Ink.InkSerializedFormat
{
    /// <summary>
    /// Represents a simple encoding scheme that removes non-significant bits
    /// </summary>
    internal class GorillaCodec
    {
        /// <summary>
        /// GorillaCodec
        /// </summary>
        public GorillaCodec()
        {
        }

        /// <summary>
        /// Static ctor
        /// </summary>
        static GorillaCodec()
        {
            //magic numbers
            _gorIndexMap = new GorillaAlgoByte[] {
                                // cBits   cPads  Index
                new GorillaAlgoByte(8,     0), // 0
                // for 1
                new GorillaAlgoByte(1,     0), // 1
                new GorillaAlgoByte(1,     1), // 2
                new GorillaAlgoByte(1,     2), // 3
                new GorillaAlgoByte(1,     3), // 4
                new GorillaAlgoByte(1,     4), // 5
                new GorillaAlgoByte(1,     5), // 6
                new GorillaAlgoByte(1,     6), // 7
                new GorillaAlgoByte(1,     7), // 8
                // for 2
                new GorillaAlgoByte(2,     0), // 9
                new GorillaAlgoByte(2,     1), // 10
                new GorillaAlgoByte(2,     2), // 11
                new GorillaAlgoByte(2,     3), // 12
                // for 3
                new GorillaAlgoByte(3,     0), // 13
                new GorillaAlgoByte(3,     1), // 14
                new GorillaAlgoByte(3,     2), // 15
                // for 4
                new GorillaAlgoByte(4,     0), // 16
                new GorillaAlgoByte(4,     1), // 17
                // for 5
                new GorillaAlgoByte(5,     0), // 18
                new GorillaAlgoByte(5,     1), // 19
                // for 6
                new GorillaAlgoByte(6,     0), // 20
                new GorillaAlgoByte(6,     1), // 21
                // for 7
                new GorillaAlgoByte(7,     0), // 22
                new GorillaAlgoByte(7,     1)}; // 23

            _gorIndexOffset = new byte[]{
                     0, // for 0, never used
                     1, // [ 1,  8] for 1
                     9, // [ 9, 12] for 2
                    13, // [13, 15] for 3
                    16, // [16, 17] for 4
                    18, // [18, 19] for 5
                    20, // [20, 21] for 6
                    22};// [22, 23] for 7
}

        /// <summary>
        /// FindPacketAlgoByte
        /// </summary>
        /// <param name="input">input stream to find the best compression for</param>
        /// <param name="testDelDel"></param>
        internal byte FindPacketAlgoByte(int[] input, bool testDelDel)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            // Check for the input item count
            if (0 == input.Length)
            {
                return 0;
            }
            // If the input count is less than 3, we cannot do del del
            testDelDel = testDelDel && (input.Length < 3);

            int minVal, maxVal;
            int minDelDel, maxDelDel;
            uint startIndex = 1;
            int xfData = 0, xfExtra = 0;
            DeltaDelta delDel = new DeltaDelta();

            // Initialize all the max-min's to initial value
            minVal = maxVal = minDelDel = maxDelDel = input[0];

            // Skip first two elements for del-del
            if (testDelDel)
            {
                delDel.Transform(input[0], ref xfData, ref xfExtra);
                delDel.Transform(input[1], ref xfData, ref xfExtra);
                // if we need extra bits, we cannot do del-del
                if (0 != xfExtra)
                {
                    testDelDel = false;
                }
            }
            // Initialize DelDelMax/Min if we can do del-del
            if (testDelDel)
            {
                delDel.Transform(input[2], ref xfData, ref xfExtra);
                // Again, if nExtra is non-zero, we cannot do del-del
                if (0 != xfExtra)
                {
                    testDelDel = false;
                }
                else
                {
                    minDelDel = maxDelDel = xfData;
                    // Update raw max/min for two elements
                    UpdateMinMax(input[1], ref maxVal, ref minVal);
                    UpdateMinMax(input[2], ref maxVal, ref minVal);
                    // Following loop starts from 3
                    startIndex = 3;
                }
            }

            for (uint dataIndex = startIndex; dataIndex < input.Length; dataIndex++)
            {
                // Update the raw min-max first
                UpdateMinMax(input[dataIndex], ref maxVal, ref minVal);
                if (testDelDel)
                {
                    // If we can do del-del, first do the transformation
                    delDel.Transform(input[dataIndex], ref xfData, ref xfExtra);
                    // again, cannot do del-del if xfExtra is non-zero
                    // otherwise, update the del-del min/max
                    if (0 != xfExtra)
                    {
                        testDelDel = false;
                    }
                    else
                    {
                        UpdateMinMax(xfData, ref maxDelDel, ref minDelDel);
                    }
                }
            }
            // Find the absolute max for del-del
            uint ulAbsMaxDelDel = (uint)Math.Max(MathHelper.AbsNoThrow(minDelDel), MathHelper.AbsNoThrow(maxDelDel));
            // Find the Math.Abs max for raw
            uint ulAbsMax = (uint)Math.Max(MathHelper.AbsNoThrow(minVal), MathHelper.AbsNoThrow(maxVal));
            // If we could do del-del and Math.Abs max of del-del is at least twice smaller than 
            // original, we do del-del, otherwise, we bitpack raw data
            if (testDelDel && ((ulAbsMaxDelDel >> 1) < ulAbsMax))
            {
                ulAbsMax = ulAbsMaxDelDel;
            }
            else
            {
                testDelDel = false;
            }
            // Absolute bits
            int bitCount = 0;
            while ((0 != (ulAbsMax >> bitCount)) && (31 > bitCount))
            {
                bitCount++;
            }
            // Sign bit
            bitCount++;

            // Return the algo data
            return (byte)((byte)(bitCount & 0x1F) | (testDelDel ? (byte)0x20 : (byte)0));
        }

        /// <summary>
        /// FindPropAlgoByte - find the best way to compress the input array
        /// </summary>
        /// <param name="input"></param>
        internal byte FindPropAlgoByte(byte[] input)
        {
            // Empty buffer case
            if(0 == input.Length)
            {
                return 0;
            }
            // We test for int's only if the data size is multiple of 4
            int countOfInts = ((0 == (input.Length & 0x03)) ? input.Length >> 2 : 0);
            BitStreamReader intReader = null;
            if (countOfInts > 0)
            {
                intReader = new BitStreamReader(input);
            }

            // We test for shorts's if datasize is multiple of 2
            int countOfShorts = ((0 == (input.Length & 0x01)) ? input.Length >> 1 : 0);
            BitStreamReader shortReader = null;
            if (countOfShorts > 0)
            {
                shortReader = new BitStreamReader(input);
            }

            // Min Max variables for different data type
            int maxInt = 0, minInt = 0;

            // Unsigned min vals
            ushort maxShort = 0;

            // byte min/max
            byte maxByte = input[0];

            // Find min/max of all data
            // This loop covers:
            //   All of int data, if there is any
            //   First half of Word data, if there is int data, there MUST be word data
            //   First quarter of byte data.
            uint n = 0;
            for(n = 0; n < countOfInts; ++n)
            {
                Debug.Assert(intReader != null);
                Debug.Assert(shortReader != null);

                maxByte = Math.Max(input[n], maxByte);
                maxShort = Math.Max((ushort)shortReader.ReadUInt16Reverse(Native.BitsPerShort), maxShort);
                UpdateMinMax((int)intReader.ReadUInt32Reverse(Native.BitsPerInt), ref maxInt, ref minInt);
            }
            // This loop covers:
            //   Second half of short data, if there were int data,
            //   or all of short data, if there were no int data
            //   Upto half of byte data
            for(; n < countOfShorts; ++n)
            {
                Debug.Assert(shortReader != null);
                maxByte = Math.Max(input[n], maxByte);
                maxShort = Math.Max((ushort)shortReader.ReadUInt16Reverse(Native.BitsPerShort), maxShort);
            }
            // This loop covers last half of byte data if word data existed
            // or, all of bytes data
            for (; n < input.Length; ++n)
            {
                maxByte = Math.Max(input[n], maxByte);
            }


            // Which one gives the best result?
            int bitCount = 1;
            // Find the Math.Abs max for byte
            uint ulAbsMax = (uint)maxByte;
            // Find the number of bits required to encode that number
            while ((0 != (ulAbsMax >> bitCount)) && (bitCount < (uint)Native.BitsPerByte))
            {
                bitCount++;
            }
            // Also compute the padding required
            int padCount = ((((~(bitCount * input.Length)) & 0x07) + 1) & 0x07) / bitCount;
            // Compare the result with word partition
            if (countOfShorts > 0)
            {
                int shortBitCount = 1;
                // Find the Math.Abs max for word
                ulAbsMax = (uint)maxShort;
                // Find the number of bits required to encode that number
                while ((0 != (ulAbsMax >> shortBitCount)) && (shortBitCount < (uint)Native.BitsPerShort))
                {
                    shortBitCount++;
                }
                // Determine which scheme requires lesser number of bytes
                if (shortBitCount < (bitCount << 1))
                {
                    bitCount = shortBitCount;
                    padCount = ((((~(bitCount * countOfShorts)) & 0x07) + 1) & 0x07) / bitCount;
                }
                else
                {
                    countOfShorts = 0;
                }
            }
            // Compare the best with int
            if(countOfInts > 0)
            {
                int intBitCount = 0;
                // Find the Math.Abs max for int
                ulAbsMax = (uint)Math.Max(MathHelper.AbsNoThrow(minInt), MathHelper.AbsNoThrow(maxInt));
                // Find the number of bits required to encode that number
                while ((0 != (ulAbsMax >> intBitCount)) && (31 > intBitCount))
                {
                    intBitCount++;
                }
                // Adjust for the sign bit
                intBitCount++;
                // Determine which one is better
                if (intBitCount < ((0 < countOfShorts) ? (bitCount << 1) : (bitCount << 2)))
                {
                    bitCount = intBitCount;
                    padCount = ((((~(bitCount * countOfInts)) & 0x07) + 1) & 0x07) / bitCount;
                    // Set the countOfShorts to 0 to indicate int wins over word
                    countOfShorts = 0;
                }
                else
                {
                    countOfInts = 0;
                }
            }
            // AlgoByte starts with 000, 001 and 01 for byte, word and int correspondingly
            byte algorithmByte = (byte)((0 < countOfInts) ? 0x40 : ((0 < countOfShorts) ? 0x20 : 0x00));
            // If byte, and bitCount is 8, we revert use 0 as algo byte
            if ((8 == bitCount) && (0 == (countOfInts + countOfShorts)))
            {
                algorithmByte = 0;
            }
            // If bitCount is more than 7, we add 16 to make the index
            else if (bitCount > 7)
            {
                algorithmByte |= (byte)(16 + bitCount);
            }
            // Otherwise, we find the index from the table
            else
            {
                algorithmByte |= (byte)(_gorIndexOffset[bitCount] + padCount);
            }
            return algorithmByte;
        }

        /// <summary>
        /// GetPropertyBitCount
        /// </summary>
        /// <param name="algorithmByte"></param>
        /// <param name="countPerItem"></param>
        /// <param name="bitCount"></param>
        /// <param name="padCount"></param>
        internal void GetPropertyBitCount(byte algorithmByte, ref int countPerItem, ref int bitCount, ref int padCount)
        {
            int index = 0;
            if (0 != (algorithmByte & 0x40))
            {
                countPerItem = 4;
                index = (int)algorithmByte & 0x3F;
            }
            else
            {
                countPerItem = (0 != (algorithmByte & 0x20)) ? 2 : 1;
                index = (int)algorithmByte & 0x1F;
            }
            bitCount = index - 16;
            padCount = 0;
            if (index < _gorIndexMap.Length && index >= 0)
            {
                bitCount = (int)_gorIndexMap[index].BitCount;
                padCount = (int)_gorIndexMap[index].PadCount;
            }
        }

        /// <summary>
        /// Compress - compress the input[] into compressedData
        /// </summary>
        /// <param name="bitCount">The count of bits needed for all elements</param>
        /// <param name="input">input buffer</param>
        /// <param name="startInputIndex">offset into the input buffer</param>
        /// <param name="dtxf">data transform.  can be null</param>
        /// <param name="compressedData">The list of bytes to write the compressed input to</param>
        internal void Compress(int bitCount, int[] input, int startInputIndex, DeltaDelta dtxf, List<byte> compressedData)
        {
            if (null == input || null == compressedData)
            {
                throw new ArgumentNullException(StrokeCollectionSerializer.ISFDebugMessage("input or compressed data was null in Compress"));
            }
            if (bitCount < 0)
            {
                throw new ArgumentOutOfRangeException("bitCount");
            }

            if (bitCount == 0)
            {
                //adjust if the bitcount is 0
                //(this makes bitCount 32)
                bitCount = (int)(Native.SizeOfInt << 3);
            }

            //have the writer adapt to the List<byte> passed in and write to it
            BitStreamWriter writer = new BitStreamWriter(compressedData);
            if (null != dtxf)
            {
                int xfData = 0;
                int xfExtra = 0;
                for (int i = startInputIndex; i < input.Length; i++)
                {
                    dtxf.Transform(input[i], ref xfData, ref xfExtra);
                    if (xfExtra != 0)
                    {
                        throw new InvalidOperationException(StrokeCollectionSerializer.ISFDebugMessage("Transform returned unexpected results"));
                    }
                    writer.Write((uint)xfData, bitCount);
                }
            }
            else
            {
                for (int i = startInputIndex; i < input.Length; i++)
                {
                    writer.Write((uint)input[i], bitCount);
                }
            }
        }

        /// <summary>
        /// Compress - compresses the byte[] being read by the BitStreamReader into compressed data
        /// </summary>
        /// <param name="bitCount">the number of bits to use for each element</param>
        /// <param name="reader">a reader over the byte[] to compress</param>
        /// <param name="encodingType">int, short or byte?</param>
        /// <param name="unitsToEncode">number of logical units to encoded</param>
        /// <param name="compressedData">output write buffer</param>
        internal void Compress(int bitCount, BitStreamReader reader, GorillaEncodingType encodingType, int unitsToEncode, List<byte> compressedData)
        {
            if (null == reader || null == compressedData)
            {
                throw new ArgumentNullException(StrokeCollectionSerializer.ISFDebugMessage("reader or compressedData was null in compress"));
            }
            if (bitCount < 0)
            {
                throw new ArgumentOutOfRangeException("bitCount");
            }
            if (unitsToEncode < 0)
            {
                throw new ArgumentOutOfRangeException("unitsToEncode");
            }

            if (bitCount == 0)
            {
                //adjust if the bitcount is 0
                //(this makes bitCount 32)
                switch (encodingType)
                {
                    case GorillaEncodingType.Int:
                        {
                            bitCount = Native.BitsPerInt;
                            break;
                        }
                    case GorillaEncodingType.Short:
                        {
                            bitCount = Native.BitsPerShort;
                            break;
                        }
                    case GorillaEncodingType.Byte:
                        {
                            bitCount = Native.BitsPerByte;
                            break;
                        }
                    default:
                        {
                            throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("bogus GorillaEncodingType passed to compress"));
                        }
                }
            }

            //have the writer adapt to the List<byte> passed in and write to it
            BitStreamWriter writer = new BitStreamWriter(compressedData);
            while (!reader.EndOfStream && unitsToEncode > 0)
            {
                int data = GetDataFromReader(reader, encodingType);
                writer.Write((uint)data, bitCount);
                unitsToEncode--;
            }
        }

        /// <summary>
        /// Private helper used to read an int, short or byte (in reverse order) from the reader
        /// and return an int
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private int GetDataFromReader(BitStreamReader reader, GorillaEncodingType type)
        {
            switch (type)
            {
                case GorillaEncodingType.Int:
                    {
                        return (int)reader.ReadUInt32Reverse(Native.BitsPerInt);
                    }
                case GorillaEncodingType.Short:
                    {
                        return (int)reader.ReadUInt16Reverse(Native.BitsPerShort);
                    }
                case GorillaEncodingType.Byte:
                    {
                        return (int)reader.ReadByte(Native.BitsPerByte);
                    }
                default:
                    {
                        throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("bogus GorillaEncodingType passed to GetDataFromReader"));
                    }
            }
        }


        /// <summary>
        /// Uncompress - uncompress a byte[] into an int[] of point data (x,x,x,x,x)
        /// </summary>
        /// <param name="bitCount">The number of bits each element uses in input</param>
        /// <param name="input">compressed data</param>
        /// <param name="inputIndex">index to begin decoding at</param>
        /// <param name="dtxf">data xf, can be null</param>
        /// <param name="outputBuffer">output buffer that is prealloc'd to write to</param>
        /// <param name="outputBufferIndex">the index of the output buffer to write to</param>
        internal uint Uncompress(int bitCount, byte[] input, int inputIndex, DeltaDelta dtxf, int[] outputBuffer, int outputBufferIndex)
        {
            if (null == input)
            {
                throw new ArgumentNullException("input");
            }
            if (inputIndex >= input.Length)
            {
                throw new ArgumentOutOfRangeException("inputIndex");
            }
            if (null == outputBuffer)
            {
                throw new ArgumentNullException("outputBuffer");
            }
            if (outputBufferIndex >= outputBuffer.Length)
            {
                throw new ArgumentOutOfRangeException("outputBufferIndex");
            }

            if (bitCount < 0)
            {
                throw new ArgumentOutOfRangeException("bitCount");
            }

            // Adjust bit count if 0 passed in
            if (bitCount == 0)
            {
                //adjust if the bitcount is 0
                //(this makes bitCount 32)
                bitCount = (int)(Native.SizeOfInt << 3);
            }

            // Test whether the items are signed. For unsigned number, we don't need mask
            // If we are trying to compress signed long values with bit count = 5
            // The mask will be 1111 1111 1111 0000. The way it is done is, if the 5th
            // bit is 1, the number is negative numbe, othrwise it's positive. Testing
            // will be non-zero, ONLY if the 5th bit is 1, in which case we OR with the mask
            // otherwise we leave the number as it is.
            uint bitMask = (unchecked((uint)~0) << (bitCount - 1));
            uint bitData = 0;
            BitStreamReader reader = new BitStreamReader(input, inputIndex);

            if(dtxf != null)
            {
                while (!reader.EndOfStream)
                {
                    bitData = reader.ReadUInt32(bitCount);
                    // Construct the item
                    bitData = ((bitData & bitMask) != 0) ? bitMask | bitData : bitData;
                    int result = dtxf.InverseTransform((int)bitData, 0);
                    Debug.Assert(outputBufferIndex < outputBuffer.Length);
                    outputBuffer[outputBufferIndex++] = result;
                    if (outputBufferIndex == outputBuffer.Length)
                    {
                        //only write as much as the outputbuffer can hold
                        //this is assumed by calling code
                        break;
                    }
                }
            }
            else
            {
                while (!reader.EndOfStream)
                {
                    bitData = reader.ReadUInt32(bitCount);
                    // Construct the item
                    bitData = ((bitData & bitMask) != 0) ? bitMask | bitData : bitData;
                    Debug.Assert(outputBufferIndex < outputBuffer.Length);
                    outputBuffer[outputBufferIndex++] = (int)bitData;
                    if (outputBufferIndex == outputBuffer.Length)
                    {
                        //only write as much as the outputbuffer can hold
                        //this is assumed by calling code
                        break;
                    }
                }
            }

            // Calculate how many bytes were read from input buffer
            return (uint)((outputBuffer.Length * bitCount + 7) >> 3);
        }


        /// <summary>
        /// Uncompress - uncompress the byte[] in the reader to a byte[] to return
        /// </summary>
        /// <param name="bitCount">number of bits each element is compressed to</param>
        /// <param name="reader">a reader over the compressed byte[]</param>
        /// <param name="encodingType">int, short or byte?</param>
        /// <param name="unitsToDecode">number of logical units to decode</param>
        /// <returns>Uncompressed byte[]</returns>
        internal byte[] Uncompress(int bitCount, BitStreamReader reader, GorillaEncodingType encodingType, int unitsToDecode)
        {
            if (null == reader)
            {
                throw new ArgumentNullException("reader");
            }
            if (bitCount < 0)
            {
                throw new ArgumentOutOfRangeException("bitCount");
            }
            if (unitsToDecode < 0)
            {
                throw new ArgumentOutOfRangeException("unitsToDecode");
            }

            int bitsToWrite = 0;

            // Test whether the items are signed. For unsigned number, we don't need mask
            // If we are trying to compress signed long values with bit count = 5
            // The mask will be 1111 1111 1111 0000. The way it is done is, if the 5th
            // bit is 1, the number is negative numbe, othrwise it's positive. Testing
            // will be non-zero, ONLY if the 5th bit is 1, in which case we OR with the mask
            // otherwise we leave the number as it is.
            uint bitMask = 0;
            //adjust if the bitcount is 0
            //(this makes bitCount 32)
            switch (encodingType)
            {
                case GorillaEncodingType.Int:
                    {
                        if (bitCount == 0)
                        {
                            bitCount = Native.BitsPerInt;
                        }
                        bitsToWrite = Native.BitsPerInt;
                        //we decode int's as unsigned, so we need to create a mask
                        bitMask = (unchecked((uint)~0) << (bitCount - 1));
                        break;
                    }
                case GorillaEncodingType.Short:
                    {
                        if (bitCount == 0)
                        {
                            bitCount = Native.BitsPerShort;
                        }
                        bitsToWrite = Native.BitsPerShort;
                        //shorts are decoded as unsigned values, no mask required
                        bitMask = 0;
                        break;
                    }
                case GorillaEncodingType.Byte:
                    {
                        if (bitCount == 0)
                        {
                            bitCount = Native.BitsPerByte;
                        }
                        bitsToWrite = Native.BitsPerByte;
                        //bytes are decoded as unsigned values, no mask required
                        bitMask = 0;
                        break;
                    }
                default:
                    {
                        throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("bogus GorillaEncodingType passed to Uncompress"));
                    }
            }
            List<byte> output = new List<byte>((bitsToWrite / 8) * unitsToDecode);
            BitStreamWriter writer = new BitStreamWriter(output);
            uint bitData = 0;

            while (!reader.EndOfStream && unitsToDecode > 0)
            {
                //we're going to cast to an uint anyway, just read as one
                bitData = reader.ReadUInt32(bitCount);
                // Construct the item
                bitData = ((bitData & bitMask) != 0) ? bitMask | bitData : bitData;
                writer.WriteReverse(bitData, bitsToWrite);
                unitsToDecode--;
            }

            return output.ToArray();
        }

        /// <summary>
        /// UpdateMinMax
        /// </summary>
        /// <param name="n">number to evaluate</param>
        /// <param name="max">a ref to the max, which will be updated if n is more</param>
        /// <param name="min">a ref to the min, which will be updated if n is less</param>
        private static void UpdateMinMax(int n, ref int max, ref int min)
        {
            if (n > max)
            {
                max = n;
            }
            else if (n < min)
            {
                min = n;
            }
        }
       
        /// <summary>
        /// Private statics
        /// </summary>
        private static GorillaAlgoByte[]    _gorIndexMap;
        private static byte[]               _gorIndexOffset;
}

    /// <summary>
    /// Helper struct
    /// </summary>
    internal struct GorillaAlgoByte
    {
        public GorillaAlgoByte(uint bitCount, uint padCount)
        {
            BitCount = bitCount;
            PadCount = padCount;
        }
        public uint BitCount;
        public uint PadCount;
    }

    /// <summary>
    /// Simple helper enum to control Gorilla encoding in Compress
    /// </summary>
    internal enum GorillaEncodingType
    {
        Byte = 0,
        Short,
        Int,
    }
}
