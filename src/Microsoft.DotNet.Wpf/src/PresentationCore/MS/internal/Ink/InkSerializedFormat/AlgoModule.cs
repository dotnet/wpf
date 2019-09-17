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
    internal class AlgoModule
    {
        /// <summary>
        /// Ctor
        /// </summary>
        internal AlgoModule()
        {
        }

        /// <summary>
        /// Based on the given input, finds the best compression to use on it.
        /// </summary>
        /// <param name="input">assumed to be point data (x,x,x,x,x,x,x)</param>
        /// <returns></returns>
        internal byte GetBestDefHuff(int[] input)
        {
            if (input.Length < 3)
            {
                return NoCompression;
            }
            DeltaDelta xfDelDel = new DeltaDelta();
            int xfData = 0;
            int exData = 0;

            // Perform delta delta 2 times to set up the internal state of
            // delta delta transform
            xfDelDel.Transform(input[0], ref xfData, ref exData);
            xfDelDel.Transform(input[1], ref xfData, ref exData);
            double sumSq = 0.0;

            // Compute the variance of the delta delta
            uint n = 2;
            for(; n < input.Length; n++)
            {
                xfDelDel.Transform(input[n], ref xfData, ref exData);
                if (0 == exData)
                {
                    sumSq += ((double)xfData * (double)xfData);
                }
            }
            sumSq *= (0.205625 / (n - 1.0));

            int i = DefaultFirstSquareRoot.Length - 2;
            for(; i > 1; i--)
            {
                if(sumSq > DefaultFirstSquareRoot[i])
                {
                    break;
                }
            }

            byte retVal = (byte)(IndexedHuffman | (byte)(i + 1));
            return retVal;
        }

        /// <summary>
        /// Compresses int[] packet data, returns it as a byte[]
        /// </summary>
        /// <param name="input">assumed to be point data (x,x,x,x,x,x,x)</param>
        /// <param name="compression">magic byte specifying the compression to use</param>
        /// <returns></returns>
        internal byte[] CompressPacketData(int[] input, byte compression)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            List<byte> compressedData = new List<byte>();
            //leave room at the beginning of 
            //compressedData for the compression header byte
            //which we will add at the end
            compressedData.Add((byte)0);

            if (DefaultCompression == (DefaultCompression & compression))
            {
                compression = GetBestDefHuff(input);
            }
            if (IndexedHuffman == (DefaultCompression & compression))
            {
                DataXform dtxf = this.HuffModule.FindDtXf(compression);
                HuffCodec huffCodec = this.HuffModule.FindCodec(compression);
                huffCodec.Compress(dtxf, input, compressedData);
                if (((compressedData.Count - 1/*for the algo byte we just made room for*/) >> 2) > input.Length)
                {
                    //recompress with no compression (gorilla)
                    compression = NoCompression;
                    //reset
                    compressedData.Clear();
                    compressedData.Add((byte)0);
                }
            }
            if (NoCompression == (DefaultCompression & compression))
            {
                bool testDelDel = ((compression & 0x20) != 0);
                compression =
                    this.GorillaCodec.FindPacketAlgoByte(input, testDelDel);
                
                DeltaDelta dtxf = null;
                if ((compression & 0x20) != 0)
                {
                    dtxf = this.DeltaDelta;
                }

                int inputIndex = 0;
                if (null != dtxf)
                {
                    //multibyteencode the first two values
                    int xfData = 0;
                    int xfExtra = 0;
                    
                    dtxf.ResetState();
                    dtxf.Transform(input[0], ref xfData, ref xfExtra);
                    this.MultiByteCodec.SignEncode(xfData, compressedData);

                    dtxf.Transform(input[1], ref xfData, ref xfExtra);
                    this.MultiByteCodec.SignEncode(xfData, compressedData);

                    //advance to the third member, we've already read the first two
                    inputIndex = 2;
                }

                //Gorllia time
                int bitCount = (compression & 0x1F);
                this.GorillaCodec.Compress( bitCount,           //the max count of bits required for each int
                                            input,              //the input array to compress
                                            inputIndex,         //the index to start compressing at
                                            dtxf,               //data transform to use when compressing, can be null
                                            compressedData);    //a ref to the compressed data that will be written to
            }

            // compression / algo data always goes in index 0
            compressedData[0] = compression;
            return compressedData.ToArray();
        }

        /// <summary>
        /// DecompressPacketData - given a compressed byte[], uncompress it to the outputBuffer
        /// </summary>
        /// <param name="input">compressed byte from the ISF stream</param>
        /// <param name="outputBuffer">prealloc'd buffer to write to</param>
        /// <returns></returns>
        internal uint DecompressPacketData(byte[] input, int[] outputBuffer)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (input.Length < 2)
            {
                throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("Input buffer passed was shorter than expected"));
            }
            if (outputBuffer == null)
            {
                throw new ArgumentNullException("outputBuffer");
            }
            if (outputBuffer.Length == 0)
            {
                throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("output buffer length was zero"));
            }

            byte compression = input[0];
            uint totalBytesRead = 1; //we just read one
            int inputIndex = 1;
            switch (compression & 0xC0)
            {
                case 0x80://IndexedHuffman
                    {
                        DataXform dtxf = this.HuffModule.FindDtXf(compression);
                        HuffCodec huffCodec = this.HuffModule.FindCodec(compression);
                        totalBytesRead += huffCodec.Uncompress(dtxf, input, inputIndex, outputBuffer);
                        return totalBytesRead;
                    }
                case 0x00: //NoCompression
                    {
                        int outputBufferIndex = 0;
                        DeltaDelta dtxf = null;
                        if ((compression & 0x20) != 0)
                        {
                            dtxf = this.DeltaDelta;
                        }

                        int bitCount = 0;
                        if ((compression & 0x1F) == 0)
                        {
                            bitCount = Native.BitsPerInt;//32
                        }
                        else
                        {
                            bitCount = (compression & 0x1F);
                        }

                        if (null != dtxf)
                        {
                            //must have at least two more bytes besides the
                            //initial algo byte
                            if (input.Length < 3)
                            {
                                throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("Input buffer was too short (must be at least 3 bytes)"));
                            }

                            //multibyteencode the first two values
                            int xfData = 0;
                            int xfExtra = 0;

                            dtxf.ResetState();

                            uint bytesRead = 
                                this.MultiByteCodec.SignDecode(input, inputIndex, ref xfData);
                            //advance our index
                            inputIndex += (int)bytesRead;
                            totalBytesRead += bytesRead;
                            int result = dtxf.InverseTransform(xfData, xfExtra);
                            Debug.Assert(outputBufferIndex < outputBuffer.Length);
                            outputBuffer[outputBufferIndex++] = result;

                            bytesRead =
                                this.MultiByteCodec.SignDecode(input, inputIndex, ref xfData);
                            //advance our index
                            inputIndex += (int)bytesRead;
                            totalBytesRead += bytesRead;
                            result = dtxf.InverseTransform(xfData, xfExtra);
                            Debug.Assert(outputBufferIndex < outputBuffer.Length);
                            outputBuffer[outputBufferIndex++] = result;
                        }

                        totalBytesRead +=
                            this.GorillaCodec.Uncompress(  bitCount,    //the max count of bits required for each int
                                                           input,       //the input array to uncompress
                                                           inputIndex,  //the index to start uncompressing at
                                                           dtxf,        //data transform to use when compressing, can be null
                                                           outputBuffer,//a ref to the output buffer to write to
                                                           outputBufferIndex); //the index of the output buffer to write to

                        return totalBytesRead;
                    }
                default:
                    {
                        throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("Invalid decompression algo byte"));
                    }
}
        }

        /// <summary>
        /// Compresses property data which is already in the form of a byte[]
        /// into a compressed byte[]
        /// </summary>
        /// <param name="input">byte[] data ready to be compressed</param>
        /// <param name="compression">the compression to use</param>
        /// <returns></returns>
        internal byte[] CompressPropertyData(byte[] input, byte compression)
        {
            List<byte> compressedData = new List<byte>(input.Length + 1); //reasonable default based on profiling.

            //leave room at the beginning of 
            //compressedData for the compression header byte
            compressedData.Add((byte)0);

            if (DefaultCompression == (DefaultCompression & compression))
            {
                compression = this.GorillaCodec.FindPropAlgoByte(input);
            }

            //validate that we never lzencode
            if (LempelZiv == (compression & LempelZiv))
            {
                throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("Invalid compression specified or computed by FindPropAlgoByte"));
            }

            //determine what the optimal way to compress the data is.  Should we treat
            //the byte[] as a series of Int's, Short's or Byte's?
            int countPerItem = 0, bitCount = 0, padCount = 0;
            this.GorillaCodec.GetPropertyBitCount(compression, ref countPerItem, ref bitCount, ref padCount);

            Debug.Assert(countPerItem == 4 || countPerItem == 2 || countPerItem == 1);

            GorillaEncodingType type = GorillaEncodingType.Byte;
            int unitCount = input.Length;
            if (countPerItem == 4)
            {
                type = GorillaEncodingType.Int;
                unitCount >>= 2;
            }
            else if (countPerItem == 2)
            {
                type = GorillaEncodingType.Short;
                unitCount >>= 1;
            }
            

            BitStreamReader reader = new BitStreamReader(input);

            //encode, gorilla style
            this.GorillaCodec.Compress(bitCount,            //the max count of bits required for each int
                                        reader,             //the reader, which can read int, byte, short
                                        type,               //informs how the reader reads
                                        unitCount,          //just how many items do we need to compress?
                                        compressedData);    //a ref to the compressed data that will be written to

            compressedData[0] = compression;
            return compressedData.ToArray();
        }

        /// <summary>
        /// Decompresses property data (from a compressed byte[] to an uncompressed byte[])
        /// </summary>
        /// <param name="input">The byte[] to decompress</param>
        /// <returns></returns>
        internal byte[] DecompressPropertyData(byte[] input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (input.Length < 2)
            {
                throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("input.Length must be at least 2"));
            }

            byte compression = input[0];
            int inputIndex = 1;

            if (LempelZiv == (compression & LempelZiv))
            {
                if (0 != (compression & (~LempelZiv)))
                {
                    throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("bogus isf, we don't decompress property data with lz"));
                }
                return this.LZCodec.Uncompress(input, inputIndex);
            }
            else
            {
                //gorilla

                //determine what the way to uncompress the data.  Should we treat
                //the byte[] as a series of Int's, Short's or Byte's?
                int countPerItem = 0, bitCount = 0, padCount = 0;
                this.GorillaCodec.GetPropertyBitCount(compression, ref countPerItem, ref bitCount, ref padCount);
                Debug.Assert(countPerItem == 4 || countPerItem == 2 || countPerItem == 1);

                GorillaEncodingType type = GorillaEncodingType.Byte;
                if (countPerItem == 4)
                {
                    type = GorillaEncodingType.Int;
                }
                else if (countPerItem == 2)
                {
                    type = GorillaEncodingType.Short;
                }

                //determine how many units (of int, short or byte) that there are to decompress
                int unitsToDecode = ((input.Length - inputIndex << 3) / bitCount) - padCount;
                BitStreamReader reader = new BitStreamReader(input, inputIndex);
                return this.GorillaCodec.Uncompress(bitCount, reader, type, unitsToDecode);
            }
}

        /// <summary>
        /// Private lazy init'd member
        /// </summary>
        private HuffModule HuffModule
        {
            get
            {
                if (_huffModule == null)
                {
                    _huffModule = new HuffModule();
                }
                return _huffModule;
            }
        }

        /// <summary>
        /// Private lazy init'd member
        /// </summary>
        private MultiByteCodec MultiByteCodec
        {
            get
            {
                if (_multiByteCodec == null)
                {
                    _multiByteCodec = new MultiByteCodec();
                }
                return _multiByteCodec;
            }
        }

        /// <summary>
        /// Private lazy init'd member
        /// </summary>
        private DeltaDelta DeltaDelta
        {
            get
            {
                if (_deltaDelta == null)
                {
                    _deltaDelta = new DeltaDelta();
                }
                return _deltaDelta;
            }
        }

        /// <summary>
        /// Private lazy init'd member
        /// </summary>
        private GorillaCodec GorillaCodec
        {
            get
            {
                if (_gorillaCodec == null)
                {
                    _gorillaCodec = new GorillaCodec();
                }
                return _gorillaCodec;
            }
        }

        /// <summary>
        /// Private lazy init'd member
        /// </summary>
        private LZCodec LZCodec
        {
            get
            {
                if (_lzCodec == null)
                {
                    _lzCodec = new LZCodec();
                }
                return _lzCodec;
            }
        }

        /// <summary>
        /// Privates, lazy initialized, do not reference directly
        /// </summary>
        private HuffModule          _huffModule;
        private MultiByteCodec      _multiByteCodec;
        private DeltaDelta          _deltaDelta;
        private GorillaCodec        _gorillaCodec;
        private LZCodec             _lzCodec;

        /// <summary>
        /// Static members defined in Penimc code
        /// </summary>
        internal static readonly byte NoCompression = 0x00;
        internal static readonly byte DefaultCompression = 0xC0;
        internal static readonly byte IndexedHuffman = 0x80;
        internal static readonly byte LempelZiv = 0x80;
        internal static readonly byte DefaultBAACount = 8;
        internal static readonly byte MaxBAACount = 10;


        private static readonly double[] DefaultFirstSquareRoot = { 1, 1, 1, 4, 9, 16, 36, 49};
    }
}
