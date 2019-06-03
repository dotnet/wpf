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
using System.Collections.Generic;
using System.Windows.Ink;
using MS.Internal.Ink.InkSerializedFormat;
using System.Diagnostics;


using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Ink.InkSerializedFormat
{
    /// <summary>
    /// HuffCodec
    /// </summary>
    internal class HuffCodec
    {
        /// <summary>
        /// HuffCodec
        /// </summary>
        /// <param name="defaultIndex"></param>
        internal HuffCodec(uint defaultIndex)
        {
            HuffBits bits = new HuffBits();
            bits.InitBits(defaultIndex);
            InitHuffTable(bits);
        }

        /// <summary>
        /// InitHuffTable
        /// </summary>
        /// <param name="huffBits"></param>
        private void InitHuffTable(HuffBits huffBits)
        {
            _huffBits = huffBits;
            uint bitSize = _huffBits.GetSize();
            int lowerBound = 1;
            _mins[0] = 0;
            for (uint n = 1; n < bitSize; n++)
            {
                _mins[n] = (uint)lowerBound;
                lowerBound += (1 << (_huffBits.GetBitsAtIndex(n) - 1));
            }
        }

        /// <summary>
        /// Compress
        /// </summary>
        /// <param name="dataXf">can be null</param>
        /// <param name="input">input array to compress</param>
        /// <param name="compressedData"></param>
        internal void Compress(DataXform dataXf, int[] input, List<byte> compressedData)
        {
            //
            // use the writer to write to the list<byte>
            //
            BitStreamWriter writer = new BitStreamWriter(compressedData);
            if (null != dataXf)
            {
                dataXf.ResetState();
                int xfData = 0;
                int xfExtra = 0;
                for (uint i = 0; i < input.Length; i++)
                {
                    dataXf.Transform(input[i], ref xfData, ref xfExtra);
                    Encode(xfData, xfExtra, writer);
                }
            }
            else
            {
                for (uint i = 0; i < input.Length; i++)
                {
                    Encode(input[i], 0, writer);
                }
            }
        }

        /// <summary>
        /// Uncompress
        /// </summary>
        /// <param name="dtxf"></param>
        /// <param name="input"></param>
        /// <param name="startIndex"></param>
        /// <param name="outputBuffer"></param>
        internal uint Uncompress(DataXform dtxf, byte[] input, int startIndex, int[] outputBuffer)
        {
            Debug.Assert(input != null);
            Debug.Assert(input.Length >= 2);
            Debug.Assert(startIndex == 1);
            Debug.Assert(outputBuffer != null);
            Debug.Assert(outputBuffer.Length != 0);

            BitStreamReader reader = new BitStreamReader(input, startIndex);
            int xfExtra = 0, xfData = 0;
            int outputBufferIndex = 0;
            if (null != dtxf)
            {
                dtxf.ResetState();
                while (!reader.EndOfStream)
                {
                    Decode(ref xfData, ref xfExtra, reader);
                    int uncompressed = dtxf.InverseTransform(xfData, xfExtra);
                    Debug.Assert(outputBufferIndex < outputBuffer.Length);
                    outputBuffer[outputBufferIndex++] = uncompressed;
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
                    Decode(ref xfData, ref xfExtra, reader);
                    Debug.Assert(outputBufferIndex < outputBuffer.Length);
                    outputBuffer[outputBufferIndex++] = xfData;
                    if (outputBufferIndex == outputBuffer.Length)
                    {
                        //only write as much as the outputbuffer can hold
                        //this is assumed by calling code
                        break;
                    }
                }
            }
            return (uint)((reader.CurrentIndex + 1) - startIndex); //we include startIndex in the read count
        }

        /// <summary>
        /// Encode
        /// </summary>
        /// <param name="data"></param>
        /// <param name="extra"></param>
        /// <param name="writer"></param>
        /// <returns>number of bits encoded, 0 for failure</returns>
        internal byte Encode(int data, int extra, BitStreamWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            if (data == 0)
            {
                writer.Write((byte)0, 1); //more efficent
                return (byte)1;
            }
            
            // First, encode extra if non-ZERO
            uint bitSize = _huffBits.GetSize();
            if (0 != extra)
            {
                // Prefix lenght is 1 more than table size
                byte extraPrefixLength = (byte)(bitSize + 1);
                int extraPrefix = ((1 << extraPrefixLength) - 2);

                writer.Write((uint)extraPrefix, (int)extraPrefixLength);

                // Encode the extra data first
                byte extraCodeLength = Encode(extra, 0, writer);
                // Encode the actual data next
                byte dataCodeLength = Encode(data, 0, writer);
                // Return the total code lenght
                return (byte)((int)extraPrefixLength + (int)extraCodeLength + (int)dataCodeLength);
            }
            // Find the absolute value of the data
            // IMPORTANT : It is extremely important that nData is uint, and NOT int
            // If it is int, the LONG_MIN will be encoded erroneaouly
            uint nData = (uint)MathHelper.AbsNoThrow(data);
            // Find the prefix lenght
            byte nPrefLen = 1;
            for (; (nPrefLen < bitSize) && (nData >= _mins[nPrefLen]); ++nPrefLen) ;
            // Get the data length
            uint nDataLen = _huffBits.GetBitsAtIndex((uint)nPrefLen - 1);

            // Find the prefix
            int nPrefix = ((1 << nPrefLen) - 2);
            // Append the prefix to the bit stream
            writer.Write((uint)nPrefix, (int)nPrefLen);
            // Find the data offset by lower bound
            // and append sign bit at LSB
            Debug.Assert(nDataLen > 0 && nDataLen - 1 <= Int32.MaxValue);
            int dataLenMinusOne = (int)(nDataLen - 1); //can't left shift by a uint, we need to thunk to an int
            nData = ((((nData - _mins[nPrefLen - 1]) & (uint)((1 << dataLenMinusOne) - 1)) << 1) | (uint)((data < 0) ? 1 : 0));
            // Append data into the bit streamdataLenMinusOne
            Debug.Assert(nDataLen <= Int32.MaxValue);
            writer.Write(nData, (int)nDataLen);

            return (byte)((uint)nPrefLen + nDataLen);
        }

        /// <summary>
        /// Decode
        /// </summary>
        /// <param name="data"></param>
        /// <param name="extra"></param>
        /// <param name="reader"></param>
        /// <returns>number of bits decoded, 0 for error</returns>
        internal void Decode(ref int data, ref int extra, BitStreamReader reader)
        {
            // Find the prefix length
            byte prefIndex = 0;
            while (reader.ReadBit())
            {
                prefIndex++;
            }
            // First indicate there is no extra data
            extra = 0;

            // More efficient for 0
            if (0 == prefIndex)
            {
                data = 0;
                return;
            }
            else if (prefIndex < _huffBits.GetSize())
            {
                // Find the data lenght
                uint nDataLen = _huffBits.GetBitsAtIndex(prefIndex);
                // Extract the offset data by lower dound with sign bit at LSB
                long nData = reader.ReadUInt64((int)(byte)nDataLen);
                // Find the sign bit
                bool bNeg = ((nData & 0x01) != 0);
                // Construct the data
                nData = (nData >> 1) + _mins[prefIndex];
                // Adjust the sign bit
                data = bNeg ? -((int)nData) : (int)nData;
                // return the bit count read from stream
                return;
            }
            else if (prefIndex == _huffBits.GetSize())
            {
                // This is the special prefix for extra data.
                // Decode the prefix first
                int extra2 = 0;
                int extra2Ignored = 0;
                Decode(ref extra2, ref extra2Ignored, reader);
                extra = extra2;
                
                // Following is the actual data
                int data2 = 0;
                Decode(ref data2, ref extra2Ignored, reader);
                data = data2;
                return;
            }
            throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("invalid huffman encoded data"));
        }

        /// <summary>
        /// Privates
        /// </summary>
        private HuffBits    _huffBits;
        private uint[]      _mins = new uint[MaxBAASize];

        /// <summary>
        /// Private statics
        /// </summary>
        private static readonly byte MaxBAASize = 10;

        /// <summary>
        /// Private helper class
        /// </summary>
        private class HuffBits
        {
            /// <summary>
            /// HuffBits
            /// </summary>
            internal HuffBits()
            {
                _size = 2;
                _bits[0] = 0;
                _bits[1] = 32;
                _matchIndex = 0;
                _prefixCount = 1;
                //_findMatch = true;

            }

            /// <summary>
            /// InitBits
            /// </summary>
            /// <param name="defaultIndex"></param>
            /// <returns></returns>
            internal bool InitBits(uint defaultIndex)
            {
                if (defaultIndex < DefaultBAACount && DefaultBAASize[defaultIndex] <= MaxBAASize)
                {
                    _size = DefaultBAASize[defaultIndex];
                    _matchIndex = defaultIndex;
                    _prefixCount = _size;
                    //_findMatch = true;
                    _bits = DefaultBAAData[defaultIndex];
                    return true;
                }
                return false;
            }

            /// <summary>
            /// GetSize
            /// </summary>
            internal uint GetSize()
            {
                return _size;
            }

            /// <summary>
            /// GetBitsAtIndex
            /// </summary>
            internal byte GetBitsAtIndex(uint index)
            {
                return _bits[(int)index];
            }

            /// <summary>
            /// Privates
            /// </summary>
            private byte[] _bits = new byte[MaxBAASize];
            private uint _size;
            private uint _matchIndex;
            private uint _prefixCount;
            //private bool _findMatch;

            /// <summary>
            /// Private statics
            /// </summary>
            private static readonly byte MaxBAASize = 10;
            private static readonly byte DefaultBAACount = 8;
            private static readonly byte[][] DefaultBAAData = new byte[][]
            {
                new byte[]{0, 1,  2,  4,  6,  8, 12, 16, 24, 32},
                new byte[]{0, 1,  1,  2,  4,  8, 12, 16, 24, 32},
                new byte[]{0, 1,  1,  1,  2,  4,  8, 14, 22, 32},
                new byte[]{0, 2,  2,  3,  5,  8, 12, 16, 24, 32},
                new byte[]{0, 3,  4,  5,  8, 12, 16, 24, 32,  0},
                new byte[]{0, 4,  6,  8, 12, 16, 24, 32,  0,  0},
                new byte[]{0, 6,  8, 12, 16, 24, 32,  0,  0,  0},
                new byte[]{0, 7,  8, 12, 16, 24, 32,  0,  0,  0},
            };
            private static readonly byte[] DefaultBAASize = new byte[] { 10, 10, 10, 10, 9, 8, 7, 7 };
        }
    }
}
