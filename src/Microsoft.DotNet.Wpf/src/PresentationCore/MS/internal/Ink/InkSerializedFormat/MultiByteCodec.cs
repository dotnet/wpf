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
using System.Collections.Generic;
using MS.Internal.Ink.InkSerializedFormat;
using System.Diagnostics;

using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Ink.InkSerializedFormat
{
    /// <summary>
    /// MultiByteCodec
    /// </summary>
    internal class MultiByteCodec
    {
        /// <summary>
        /// MultiByteCodec
        /// </summary>
        internal MultiByteCodec()
        {
        }

        /// <summary>
        /// Encode
        /// </summary>
        /// <param name="data"></param>
        /// <param name="output"></param>
        internal void Encode(uint data, List<byte> output)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            while (data > 0x7f)
            {
                byte byteToAdd = (byte)(0x80 | (byte)data & 0x7f);
                output.Add(byteToAdd);
                data >>= 7;
            }
            byte finalByteToAdd = (byte)(data & 0x7f);
            output.Add(finalByteToAdd);
        }

        /// <summary>
        /// SignEncode
        /// </summary>
        /// <param name="data"></param>
        /// <param name="output"></param>
        internal void SignEncode(int data, List<byte> output)
        {
            uint xfData = 0;
            if (data < 0)
            {
                xfData = (uint)( (-data << 1) | 0x01 );
            }
            else
            {
                xfData = (uint)data << 1;
            }
            Encode(xfData, output);
        }

        /// <summary>
        /// Decode
        /// </summary>
        /// <param name="input"></param>
        /// <param name="inputIndex"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        internal uint Decode(byte[] input, int inputIndex, ref uint data)
        {
            Debug.Assert(input != null);
            Debug.Assert(inputIndex < input.Length);

            // We care about first 5 bytes
            uint cb = (input.Length - inputIndex > 5) ? 5 : (uint)(input.Length - inputIndex);
            uint index = 0;
            data = 0;
            while ((index < cb) && (input[index] > 0x7f))
            {
                int leftShift = (int)(index * 7);
                data |= (uint)((input[index] & 0x7f) << leftShift);
                ++index;
            }
            if (index < cb)
            {
                int leftShift = (int)(index * 7);
                data |= (uint)((input[index] & 0x7f) << leftShift);
            }
            else
            {
                throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("invalid input in MultiByteCodec.Decode"));
            }
            return (index + 1);
        }


        /// <summary>
        /// SignDecode
        /// </summary>
        /// <param name="input"></param>
        /// <param name="inputIndex"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        internal uint SignDecode(byte[] input, int inputIndex, ref int data)
        {
            Debug.Assert(input != null); //already validated at the AlgoModule level
            if (inputIndex >= input.Length)
            {
                throw new ArgumentOutOfRangeException("inputIndex");
            }
            uint xfData = 0;
            uint cb = Decode(input, inputIndex, ref xfData);
            data = (0 != (0x01 & xfData)) ? -(int)(xfData >> 1) : (int)(xfData >> 1);
            return cb;
        }
}
}
