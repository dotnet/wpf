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
using MS.Internal.Ink;
using System.Collections.Generic;
using System.Diagnostics;


using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Ink.InkSerializedFormat
{
    /// <summary>
    /// LZCodec
    /// </summary>
    internal class LZCodec
    {
        /// <summary>
        /// LZCodec
        /// </summary>
        internal LZCodec()
        { 
        }

        /// <summary>
        /// Uncompress
        /// </summary>
        /// <param name="input"></param>
        /// <param name="inputIndex"></param>
        /// <returns></returns>
        internal byte[] Uncompress(byte[] input, int inputIndex)
        {
            //first things first
            Debug.Assert(input != null);
            Debug.Assert(input.Length > 1);
            Debug.Assert(inputIndex < input.Length);
            Debug.Assert(inputIndex >= 0);

            List<byte> output = new List<byte>();
            BitStreamWriter writer = new BitStreamWriter(output);
            BitStreamReader reader = new BitStreamReader(input, inputIndex);

            //decode
            int index = 0, countBytes = 0, start = 0;
            byte byte1 = 0, byte2 = 0;

            _maxMatchLength = FirstMaxMatchLength;

            // initialize the ring buffer
            for (index = 0; index < RingBufferLength - _maxMatchLength; index++)
            {
                _ringBuffer[index] = 0;
            }

            //initialize decoding globals
            _flags = 0;
            _currentRingBufferPosition = RingBufferLength - _maxMatchLength;
            while (!reader.EndOfStream)
            {
                byte1 = reader.ReadByte(Native.BitsPerByte);

                // High order byte counts the number of bits used in the low order
                // byte.
                if (((_flags >>= 1) & 0x100) == 0)
                {
                    // Set bit mask describing the next 8 bytes.
                    _flags = (((int)byte1) | 0xff00);

                    byte1 = reader.ReadByte(Native.BitsPerByte);
                }

                if ((_flags & 1) != 0)
                {
                    // Just store the literal byte in the buffer.
                    writer.Write(byte1, Native.BitsPerByte);

                    _ringBuffer[_currentRingBufferPosition++] = byte1;
                    _currentRingBufferPosition &= RingBufferLength - 1;
                }
                else
                {
                    // Extract the offset and count to copy from the ring buffer.
                    byte2 = reader.ReadByte(Native.BitsPerByte);

                    countBytes = (int)byte2;
                    start = (countBytes & 0xf0) << 4 | (int)byte1;
                    countBytes = (countBytes & 0x0f) + MaxLiteralLength;

                    for (index = 0; index <= countBytes; index++)
                    {
                        byte1 = _ringBuffer[(start + index) & (RingBufferLength - 1)];
                        writer.Write(byte1, Native.BitsPerByte);

                        _ringBuffer[_currentRingBufferPosition++] = byte1;
                        _currentRingBufferPosition &= RingBufferLength - 1;
                    }
                }
            }

            return output.ToArray();
        }


        /// <summary>
        /// Privates
        /// </summary>
        private byte[] _ringBuffer = new byte[RingBufferLength];
        private int _maxMatchLength = 0;
        private int _flags = 0;
        private int _currentRingBufferPosition = 0;

        /// <summary>
        /// Statics / constants
        /// </summary>
        private static readonly int FirstMaxMatchLength = 0x10;
        private static readonly int RingBufferLength = 4069;
        private static readonly int MaxLiteralLength = 2;
}
}
