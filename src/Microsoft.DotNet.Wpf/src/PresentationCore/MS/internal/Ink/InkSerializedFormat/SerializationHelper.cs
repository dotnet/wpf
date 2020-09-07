// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Utility;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Ink.InkSerializedFormat
{
    /// <summary>
    /// Summary description for HelperMethods.
    /// </summary>
    internal static class SerializationHelper
    {
        /// <summary>
        /// returns the no of byte it requires to mutlibyte encode a uint value
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static uint VarSize(uint Value)
        {
            if (Value < 0x80)
                return 1;
            else if (Value < 0x4000)
                return 2;
            else if (Value < 0x200000)
                return 3;
            else if (Value < 0x10000000)
                return 4;
            else
                return 5;
        }


        /// <summary>
        /// MultiByte Encodes an uint Value into the stream
        /// </summary>
        /// <param name="strm"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static uint Encode(Stream strm, uint Value)
        {
            ulong ib = 0;

            for (; ; )
            {
                if (Value < 128)
                {
                    strm.WriteByte((byte)Value);
                    return (uint)(ib + 1);
                }

                strm.WriteByte((byte)(0x0080 | (Value & 0x7f)));
                Value >>= 7;
                ib++;
            }
        }


        /// <summary>
        /// Multibyte encodes a unsinged long value in the stream
        /// </summary>
        /// <param name="strm"></param>
        /// <param name="ulValue"></param>
        /// <returns></returns>
        public static uint EncodeLarge(Stream strm, ulong ulValue)
        {
            uint ib = 0;

            for (; ; )
            {
                if (ulValue < 128)
                {
                    strm.WriteByte((byte)ulValue);
                    return ib + 1;
                }

                strm.WriteByte((byte)(0x0080 | (ulValue & 0x7f)));
                ulValue >>= 7;
                ib++;
            }
        }


        /// <summary>
        /// Multibyte encodes a signed integer value into a stream. Use 1's complement to 
        /// store signed values.  This means both 00 and 01 are actually 0, but we don't 
        /// need to encode all negative numbers as 64 bit values.
        /// </summary>
        /// <param name="strm"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        // Use 1's complement to store signed values.  This means both 00 and 01 are
        // actually 0, but we don't need to encode all negative numbers as 64 bit values.
        public static uint SignEncode(Stream strm, int Value)
        {
            ulong ull = 0;

            // special case LONG_MIN
            if (-2147483648 == Value)
            {
                ull = 0x0000000100000001;
            }
            else
            {
                ull = (ulong)Math.Abs(Value);

                // multiply by 2
                ull <<= 1;

                // For -ve nos, add 1
                if (Value < 0)
                    ull |= 1;
            }

            return EncodeLarge(strm, ull);
        }

        /// <summary>
        /// Decodes a multi byte encoded unsigned integer from the stream
        /// </summary>
        /// <param name="strm"></param>
        /// <param name="dw"></param>
        /// <returns></returns>
        public static uint Decode(Stream strm, out uint dw)
        {
            int shift = 0;
            byte b = 0;
            uint cb = 0;

            dw = 0;
            do
            {
                b = (byte)strm.ReadByte();
                cb++;
                dw += (uint)((int)(b & 0x7f) << shift);
                shift += 7;
            } while (((b & 0x80) > 0) && (shift < 29));

            return cb;
        }


        /// <summary>
        /// Decodes a multibyte encoded unsigned long from the stream
        /// </summary>
        /// <param name="strm"></param>
        /// <param name="ull"></param>
        /// <returns></returns>
        public static uint DecodeLarge(Stream strm, out ulong ull)
        {
            long ull1;
            int shift = 0;
            byte b = 0, a = 0;
            uint cb = 0;

            ull = 0;
            do
            {
                b = (byte)strm.ReadByte();
                cb++;
                a = (byte)(b & 0x7f);
                ull1 = a;
                ull |= (ulong)(ull1 << shift);
                shift += 7;
            } while (((b & 0x80) > 0) && (shift < 57));

            return cb;
        }


        /// <summary>
        /// Decodes a multibyte encoded signed integer from the stream
        /// </summary>
        /// <param name="strm"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        public static uint SignDecode(Stream strm, out int i)
        {
            i = 0;

            ulong ull = 0;
            uint cb = DecodeLarge(strm, out ull);

            if (cb > 0)
            {
                bool fneg = false;

                if ((ull & 0x0001) > 0)
                    fneg = true;

                ull = ull >> 1;

                long l = (long)ull;

                i = (int)(fneg ? -l : l);
            }

            return cb;
        }

        /// <summary>
        /// Converts the CLR type information into a COM-compatible type enumeration
        /// </summary>
        /// <param name="type">The CLR type information of the object to convert</param>
        /// <param name="throwOnError">Throw an exception if unknown type is used</param>
        /// <returns>The COM-compatible type enumeration</returns>
        /// <remarks>Only supports the types of data that are supported in ISF ExtendedProperties</remarks>
        public static VarEnum ConvertToVarEnum(Type type, bool throwOnError)
        {
            if (typeof(char) == type)
            {
                return VarEnum.VT_I1;
            }
            else if (typeof(char[]) == type)
            {
                return (VarEnum.VT_ARRAY | VarEnum.VT_I1);
            }
            else if (typeof(byte) == type)
            {
                return VarEnum.VT_UI1;
            }
            else if (typeof(byte[]) == type)
            {
                return (VarEnum.VT_ARRAY | VarEnum.VT_UI1);
            }
            else if (typeof(Int16) == type)
            {
                return VarEnum.VT_I2;
            }
            else if (typeof(Int16[]) == type)
            {
                return (VarEnum.VT_ARRAY | VarEnum.VT_I2);
            }
            else if (typeof(UInt16) == type)
            {
                return VarEnum.VT_UI2;
            }
            else if (typeof(UInt16[]) == type)
            {
                return (VarEnum.VT_ARRAY | VarEnum.VT_UI2);
            }
            else if (typeof(Int32) == type)
            {
                return VarEnum.VT_I4;
            }
            else if (typeof(Int32[]) == type)
            {
                return (VarEnum.VT_ARRAY | VarEnum.VT_I4);
            }
            else if (typeof(UInt32) == type)
            {
                return VarEnum.VT_UI4;
            }
            else if (typeof(UInt32[]) == type)
            {
                return (VarEnum.VT_ARRAY | VarEnum.VT_UI4);
            }
            else if (typeof(Int64) == type)
            {
                return VarEnum.VT_I8;
            }
            else if (typeof(Int64[]) == type)
            {
                return (VarEnum.VT_ARRAY | VarEnum.VT_I8);
            }
            else if (typeof(UInt64) == type)
            {
                return VarEnum.VT_UI8;
            }
            else if (typeof(UInt64[]) == type)
            {
                return (VarEnum.VT_ARRAY | VarEnum.VT_UI8);
            }
            else if (typeof(Single) == type)
            {
                return VarEnum.VT_R4;
            }
            else if (typeof(Single[]) == type)
            {
                return (VarEnum.VT_ARRAY | VarEnum.VT_R4);
            }
            else if (typeof(Double) == type)
            {
                return VarEnum.VT_R8;
            }
            else if (typeof(Double[]) == type)
            {
                return (VarEnum.VT_ARRAY | VarEnum.VT_R8);
            }
            else if (typeof(DateTime) == type)
            {
                return VarEnum.VT_DATE;
            }
            else if (typeof(DateTime[]) == type)
            {
                return (VarEnum.VT_ARRAY | VarEnum.VT_DATE);
            }
            else if (typeof(Boolean) == type)
            {
                return VarEnum.VT_BOOL;
            }
            else if (typeof(Boolean[]) == type)
            {
                return (VarEnum.VT_ARRAY | VarEnum.VT_BOOL);
            }
            else if (typeof(String) == type)
            {
                return VarEnum.VT_BSTR;
            }
            else if (typeof(Decimal) == type)
            {
                return VarEnum.VT_DECIMAL;
            }
            else if (typeof(Decimal[]) == type)
            {
                return (VarEnum.VT_ARRAY | VarEnum.VT_DECIMAL);
            }
            else
            {
                if (throwOnError)
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidDataTypeForExtendedProperty));
                }
                else
                {
                    return VarEnum.VT_UNKNOWN;
                }
            }
        }
    }
}
