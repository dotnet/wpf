// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//#define OLD_ISF

using MS.Utility;
using System;
using System.IO;
using System.Security;
using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using MS.Internal.Ink.InkSerializedFormat;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Ink.InkSerializedFormat
{
    /// <summary>
    /// ExtendedProperty converter for ISF and serialization purposes
    /// </summary>
    internal static class ExtendedPropertySerializer
    {
            // If the ExtendedProperty identifier matches one of the original ISF/Tablet-internal
            //      Guids that did not include embedded type information (e.g. used the OS-internal
            //      property storage API), then it is always stored as byte array and does not
            //      include type information
        private static bool UsesEmbeddedTypeInformation(Guid propGuid)
        {
            for (int i = 0; i < KnownIdCache.OriginalISFIdTable.Length; i++)
            {
                if (propGuid.Equals(KnownIdCache.OriginalISFIdTable[i]))
                {
                    return false;
                }
            }

            for (int i = 0; i < KnownIdCache.TabletInternalIdTable.Length; i++)
            {
                if (propGuid.Equals(KnownIdCache.TabletInternalIdTable[i]))
                {
                    return false;
                }
            }

            return true;
        }

        internal static void EncodeToStream(ExtendedProperty attribute, Stream stream)
        {
            VarEnum interopTypeInfo;
            object data = attribute.Value;
            // we need to find better way to discover which properties should be persistable
            if (attribute.Id == KnownIds.DrawingFlags)
            {
                interopTypeInfo = VarEnum.VT_I4;
            }
            else if (attribute.Id == KnownIds.StylusTip)
            {
                interopTypeInfo = VarEnum.VT_I4;
            }
            else
            {
                // Find the type of the object if it's embedded in the stream
                if (UsesEmbeddedTypeInformation(attribute.Id))
                {
                    interopTypeInfo = SerializationHelper.ConvertToVarEnum(attribute.Value.GetType(), true);
                }
                else // Otherwise treat this as byte array
                {
                    interopTypeInfo = (VarEnum.VT_ARRAY | VarEnum.VT_UI1);
                }
            }
            EncodeAttribute(attribute.Id, data, interopTypeInfo, stream);
        }

        /// <summary>
        /// This function returns the Data bytes that accurately describes the object
        /// </summary>
        /// <returns></returns>
        internal static void EncodeAttribute(Guid guid, object value, VarEnum type, Stream stream)
        {
            // Presharp gives a warning when local IDisposable variables are not closed
            // in this case, we can't call Dispose since it will also close the underlying stream
            // which still needs to be written to
#pragma warning disable 1634, 1691
#pragma warning disable 6518
            BinaryWriter bw = new BinaryWriter(stream);

            // if this guid used the legacy internal attribute persistence APIs,
            //      then it doesn't include embedded type information (it's always a byte array)
            if (UsesEmbeddedTypeInformation(guid))
            {
                // StylusTip is being serialized as a ushort, is this ok? 
                ushort datatype = (ushort)type;
                bw.Write(datatype);
            }
            // We know the type of the object. We must serialize it accordingly.
            switch(type)
            {
                case (VarEnum.VT_ARRAY | VarEnum.VT_I1)://8208
                {
                    char[] data = (char[])value;
                    bw.Write(data);
                    break;
                }
                case (VarEnum.VT_ARRAY | VarEnum.VT_UI1)://8209
                {
                    byte[] data = (byte[])value;
                    bw.Write(data);
                    break;
                }
                case (VarEnum.VT_ARRAY | VarEnum.VT_I2)://8194
                {
                    short [] data = (short[])value;
                    for( int i = 0; i < data.Length; i++ )
                        bw.Write(data[i]);
                    break;
                }
                case (VarEnum.VT_ARRAY | VarEnum.VT_UI2)://8210
                {
                    ushort [] data = (ushort[])value;
                    for( int i = 0; i < data.Length; i++ )
                        bw.Write(data[i]);
                    break;
                }
                case (VarEnum.VT_ARRAY | VarEnum.VT_I4)://8195
                {
                    int [] data = (int[])value;
                    for( int i = 0; i < data.Length; i++ )
                        bw.Write(data[i]);
                    break;
                }
                case (VarEnum.VT_ARRAY | VarEnum.VT_UI4)://8211
                {
                    uint [] data = (uint[])value;
                    for( int i = 0; i < data.Length; i++ )
                        bw.Write(data[i]);
                    break;
                }
                case (VarEnum.VT_ARRAY | VarEnum.VT_I8)://8212
                {
                    long [] data = (long[])value;
                    for( int i = 0; i < data.Length; i++ )
                        bw.Write(data[i]);
                    break;
                }
                case (VarEnum.VT_ARRAY | VarEnum.VT_UI8)://8213
                {
                    ulong [] data = (ulong[])value;
                    for( int i = 0; i < data.Length; i++ )
                        bw.Write(data[i]);
                    break;
                }
                case (VarEnum.VT_ARRAY | VarEnum.VT_R4 )://8196
                {
                    float [] data = (float[])value;
                    for( int i = 0; i < data.Length; i++ )
                        bw.Write(data[i]);
                    break;
                }
                case (VarEnum.VT_ARRAY | VarEnum.VT_R8)://8197
                {
                    double [] data = (double[])value;
                    for( int i = 0; i < data.Length; i++ )
                        bw.Write(data[i]);
                    break;
                }
                case (VarEnum.VT_ARRAY | VarEnum.VT_DATE)://8199
                {
                    DateTime [] data = (DateTime[])value;
                    for( int i = 0; i < data.Length; i++ )
                        bw.Write(data[i].ToOADate());
                    break;
                }
                case (VarEnum.VT_ARRAY | VarEnum.VT_BOOL )://8203
                {
                    bool [] data = (bool[])value;
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (data[i])
                        {
                            //true is two consecutive all bits on bytes
                            bw.Write((byte)0xFF);
                            bw.Write((byte)0xFF);
                        }
                        else
                        {
                            //false is two consecutive all bits off
                            bw.Write((byte)0);
                            bw.Write((byte)0);
                        }
                    }
                    break;
                }
                case (VarEnum.VT_ARRAY | VarEnum.VT_DECIMAL)://8206
                {
                    decimal [] data = (decimal[])value;
                    for( int i = 0; i < data.Length; i++ )
                        bw.Write(data[i]);
                    break;
                }
                case (VarEnum.VT_I1)://16
                {
                    char data = (char)value;
                    bw.Write(data);
                    break;
                }
                case (VarEnum.VT_UI1)://17
                {
                    byte data = (byte)value;
                    bw.Write(data);
                    break;
                }
                case (VarEnum.VT_I2)://2
                {
                    short data = (short)value;
                    bw.Write(data);
                    break;
                }
                case (VarEnum.VT_UI2)://18
                {
                    ushort data = (ushort)value;
                    bw.Write(data);
                    break;
                }
                case (VarEnum.VT_I4)://3
                {
                    int data = (int)value;
                    bw.Write(data);
                    break;
                }
                case (VarEnum.VT_UI4)://19
                {
                    uint data = (uint)value;
                    bw.Write(data);
                    break;
                }
                case (VarEnum.VT_I8)://20
                {
                    long data = (long)value;
                    bw.Write(data);
                    break;
                }
                case (VarEnum.VT_UI8)://21
                {
                    ulong data = (ulong)value;
                    bw.Write(data);
                    break;
                }
                case (VarEnum.VT_R4 )://4
                {
                    float data = (float)value;
                    bw.Write(data);
                    break;
                }
                case (VarEnum.VT_R8)://5
                {
                    double data = (double)value;
                    bw.Write(data);
                    break;
                }
                case (VarEnum.VT_DATE)://7
                {
                    DateTime data = (DateTime)value;
                    bw.Write(data.ToOADate());
                    break;
                }
                case (VarEnum.VT_BOOL )://11
                {
                    bool data = (bool)value;
                    if (data)
                    {
                        //true is two consecutive all bits on bytes
                        bw.Write((byte)0xFF);
                        bw.Write((byte)0xFF);
                    }
                    else
                    {
                        //false is two consecutive all bits off bytes
                        bw.Write((byte)0);
                        bw.Write((byte)0);
                    }
                    break;
                }
                case (VarEnum.VT_DECIMAL)://14
                {
                    decimal data = (decimal)value;
                    bw.Write(data);
                    break;
                }
                case (VarEnum.VT_BSTR)://8
                {
                    string data = (string)value;
                    bw.Write( System.Text.Encoding.Unicode.GetBytes( data ) );
                    break;
                }
                default:
                {
                    throw new InvalidOperationException(SR.Get(SRID.InvalidEpInIsf));
                }
            }
#pragma warning restore 6518
#pragma warning restore 1634, 1691
        }
#if OLD_ISF
        /// <summary>
        /// Encodes a custom attribute to the ISF stream
        /// </summary>
#else
        /// <summary>
        /// Encodes a custom attribute to the ISF stream
        /// </summary>
#endif
        internal static uint EncodeAsISF(Guid id, byte[] data, Stream strm, GuidList guidList, byte compressionAlgorithm, bool fTag)
        {
            uint cbWrite = 0;
            uint cbSize = GuidList.GetDataSizeIfKnownGuid(id);
            Debug.Assert(strm != null);

            if (fTag)
            {
                uint uTag = (uint)guidList.FindTag(id, true);

                cbWrite += SerializationHelper.Encode(strm, uTag);
            }

            // If cbSize is 0, it is either a custom property or a known property with 0
            // size. In either case, we need to write the size of the individual object
            if (0 == cbSize)
            {
                // Now we need to write the actual data for the property
                cbSize = (uint)data.Length;

                byte[] compresseddata = Compressor.CompressPropertyData(data, compressionAlgorithm);

#if OLD_ISF
                byte nAlgo = compressionAlgorithm;
                uint cbOut = 0;

                Compressor.CompressPropertyData(data, ref nAlgo, ref cbOut, null);

                // Allocate a buffer big enough to hold the compressed data
                byte[] compresseddata2 = new byte[cbOut];

                // NativeCompressor the data
                Compressor.CompressPropertyData(data, ref nAlgo, ref cbOut, compresseddata2);

                if (compresseddata.Length != compresseddata2.Length)
                {
                    throw new InvalidOperationException("MAGIC EXCEPTION: Property bytes length when compressed didn't match with new compression");
                }
                for (int i = 0; i < compresseddata.Length; i++)
                {
                    if (compresseddata[i] != compresseddata2[i])
                    {
                        throw new InvalidOperationException("MAGIC EXCEPTION: Property data didn't match with new property compression at index " + i.ToString());
                    }
                }
#endif

                // write the encoded compressed size minus the algo byte
                cbWrite += SerializationHelper.Encode(strm, (uint)(compresseddata.Length - 1));

                // Write the raw data
                strm.Write(compresseddata, 0, (int)compresseddata.Length);
                cbWrite += (uint)compresseddata.Length;
            }
            else
            {
                //
                // note that we used to write the nocompression byte, but that
                // was incorrect.  We must not write it because loaders do not 
                // expect it for known guids
                //
                // write the algo byte
                //strm.WriteByte(Compressor.NoCompression);
                //cbWrite++;
        
                // write the raw data without compression
                strm.Write(data, 0, (int)data.Length);
                cbWrite += (uint)data.Length;
            }

            return cbWrite;
        }
#if OLD_ISF
        /// <summary>
        /// Loads a single ExtendedProperty from the stream and add that to the list. Tag may be passed as in
        /// the case of Stroke ExtendedPropertyCollection where tag is stored in the stroke descriptor or 0 when tag
        /// is embeded in the stream
        /// </summary>
        /// <param name="stream">Memory buffer to load from</param>
        /// <param name="cbSize">Maximum length of buffer to read</param>
        /// <param name="guidList">Guid cache to read from</param>
        /// <param name="tag">Guid tag to lookup</param>
        /// <param name="guid">Guid of property</param>
        /// <param name="data">Data of property</param>
        /// <returns>Length of buffer read</returns>
#else
        /// <summary>
        /// Loads a single ExtendedProperty from the stream and add that to the list. Tag may be passed as in
        /// the case of Stroke ExtendedPropertyCollection where tag is stored in the stroke descriptor or 0 when tag
        /// is embeded in the stream
        /// </summary>
        /// <param name="stream">Memory buffer to load from</param>
        /// <param name="cbSize">Maximum length of buffer to read</param>
        /// <param name="guidList">Guid cache to read from</param>
        /// <param name="tag">Guid tag to lookup</param>
        /// <param name="guid">Guid of property</param>
        /// <param name="data">Data of property</param>
        /// <returns>Length of buffer read</returns>
#endif
        internal static uint DecodeAsISF(Stream stream, uint cbSize, GuidList guidList, KnownTagCache.KnownTagIndex tag, ref Guid guid, out object data)
        {
            uint cb, cbRead = 0;
            uint cbTotal = cbSize;

            if (0 == cbSize)
            {
                throw new InvalidOperationException(SR.Get(SRID.EmptyDataToLoad));
            }

            if (0 == tag) // no tag is passed, it must be embedded in the data
            {
                uint uiTag;
                cb = SerializationHelper.Decode(stream, out uiTag);
                tag = (KnownTagCache.KnownTagIndex)uiTag;
                if (cb > cbTotal)
                    throw new ArgumentException(SR.Get(SRID.InvalidSizeSpecified), "cbSize");

                cbTotal -= cb;
                cbRead += cb;
                System.Diagnostics.Debug.Assert(guid == Guid.Empty);
                guid = guidList.FindGuid(tag);
            }

            if (guid == Guid.Empty)
            {
                throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("Custom Attribute tag embedded in ISF stream does not match guid table"), "tag");
            }

            // Try and find the size
            uint size = GuidList.GetDataSizeIfKnownGuid(guid);

            if (size > cbTotal)
                throw new ArgumentException(SR.Get(SRID.InvalidSizeSpecified), "cbSize");

            // if the size is 0
            if (0 == size)
            {
                // Size must be embedded in the stream. Find out the compressed data size
                cb = SerializationHelper.Decode(stream, out size);

                uint cbInsize = size + 1;

                cbRead += cb;
                cbTotal -= cb;
                if (cbInsize > cbTotal)
                    throw new ArgumentException();

                byte[] bytes = new byte[cbInsize];

                uint bytesRead = (uint) stream.Read(bytes, 0, (int)cbInsize);
                if (cbInsize != bytesRead)
                {
                    throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("Read different size from stream then expected"), "cbSize");
                }

                cbRead += cbInsize;
                cbTotal -= cbInsize;

		        //Find out the Decompressed buffer size
                using (MemoryStream decompressedStream = new MemoryStream(Compressor.DecompressPropertyData(bytes)))
                {
                    // Add the property
                    data = ExtendedPropertySerializer.DecodeAttribute(guid, decompressedStream);
                }
            }
            else
            {
                // For known size data, we just read the data directly from the stream
                byte[] bytes = new byte[size];

                uint bytesRead = (uint) stream.Read(bytes, 0, (int)size);
                if (size != bytesRead)
                {
                    throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("Read different size from stream then expected"), "cbSize");
                }

                using (MemoryStream subStream = new MemoryStream(bytes))
                {
                    data = ExtendedPropertySerializer.DecodeAttribute(guid, subStream);
                }

                cbTotal -= size;
                cbRead +=  size;
            }

            return cbRead;
        }

        /// <summary>
        /// Decodes a byte array (stored in the memory stream) into an object
        /// If the GUID is one of the internal versions, then the type is assumed
        /// to be byte array.
        /// If however, the guid is of unknown origin or not v1 internal, then the type
        ///     information is assumed to be stored in the first 2 bytes of the stream.
        /// </summary>
        /// <param name="guid">Guid of property - to detect origin</param>
        /// <param name="stream">Buffer of data</param>
        /// <returns>object stored in data buffer</returns>
        internal static object DecodeAttribute(Guid guid, Stream stream)
        {
            VarEnum type;
            return DecodeAttribute(guid, stream, out type);
        }

        /// <summary>
        /// Decodes a byte array (stored in the memory stream) into an object
        /// If the GUID is one of the internal versions, then the type is assumed
        /// to be byte array.
        /// If however, the guid is of unknown origin or not v1 internal, then the type
        ///     information is assumed to be stored in the first 2 bytes of the stream.
        /// </summary>
        /// <param name="guid">Guid of property - to detect origin</param>
        /// <param name="memStream">Buffer of data</param>
        /// <param name="type">the type info stored in the stream</param>
        /// <returns>object stored in data buffer</returns>
        /// <remarks>The buffer stream passed in to the method will be closed after reading</remarks>
        internal static object DecodeAttribute(Guid guid, Stream memStream, out VarEnum type)
        {
            // First determine the object type
            using (BinaryReader br = new BinaryReader(memStream))
            {
                //
                // if usesEmbeddedTypeInfo is true, we do not 
                // read the variant type from the ISF stream.  Instead, 
                // we assume it to be a byte[]
                //
                bool usesEmbeddedTypeInfo = UsesEmbeddedTypeInformation(guid);

                // if the Id has embedded type information then retrieve it from the stream
                if (usesEmbeddedTypeInfo)
                {
                    // We must read the data type from the stream
                    type = (VarEnum)br.ReadUInt16();
                }
                else
                {
                    // The data is stored as byte array
                    type = (VarEnum.VT_ARRAY | VarEnum.VT_UI1);
                }
                switch (type)
                {
                    case (VarEnum.VT_ARRAY | VarEnum.VT_I1):
                        return br.ReadChars((int)(memStream.Length - 2));
                    case (VarEnum.VT_ARRAY | VarEnum.VT_UI1):
                        {
                            //
                            // note: for (VarEnum.VT_ARRAY | VarEnum.VT_UI1),
                            // we might be reading data that didn't have the 
                            // type embedded in the ISF stream, in which case
                            // we must not assume we've already read two bytes
                            //
                            int previouslyReadBytes = 2;
                            if (!usesEmbeddedTypeInfo)
                            {
                                previouslyReadBytes = 0;
                            }
                            return br.ReadBytes((int)(memStream.Length - previouslyReadBytes));
                        }
                    case (VarEnum.VT_ARRAY | VarEnum.VT_I2):
                        {
                            int count = (int)(memStream.Length - 2) / 2;    // 2 is the size of one element
                            short[] val = new short[count];
                            for (int i = 0; i < count; i++)
                                val[i] = br.ReadInt16();
                            return val;
                        }
                    case (VarEnum.VT_ARRAY | VarEnum.VT_UI2):
                        {
                            int count = (int)(memStream.Length - 2) / 2;    // 2 is the size of one element
                            ushort[] val = new ushort[count];
                            for (int i = 0; i < count; i++)
                                val[i] = br.ReadUInt16();
                            return val;
                        }
                    case (VarEnum.VT_ARRAY | VarEnum.VT_I4):
                        {
                            int count = (int)(memStream.Length - 2) / 4;    // 2 is the size of one element
                            int[] val = new int[count];
                            for (int i = 0; i < count; i++)
                                val[i] = br.ReadInt32();
                            return val;
                        }
                    case (VarEnum.VT_ARRAY | VarEnum.VT_UI4):
                        {
                            int count = (int)(memStream.Length - 2) / 4;    // size of one element
                            uint[] val = new uint[count];
                            for (int i = 0; i < count; i++)
                                val[i] = br.ReadUInt32();
                            return val;
                        }
                    case (VarEnum.VT_ARRAY | VarEnum.VT_I8):
                        {
                            int count = (int)(memStream.Length - 2) / Native.BitsPerByte;    // size of one element
                            long[] val = new long[count];
                            for (int i = 0; i < count; i++)
                                val[i] = br.ReadInt64();
                            return val;
                        }
                    case (VarEnum.VT_ARRAY | VarEnum.VT_UI8):
                        {
                            int count = (int)(memStream.Length - 2) / Native.BitsPerByte;    // size of one element
                            ulong[] val = new ulong[count];
                            for (int i = 0; i < count; i++)
                                val[i] = br.ReadUInt64();
                            return val;
                        }
                    case (VarEnum.VT_ARRAY | VarEnum.VT_R4):
                        {
                            int count = (int)(memStream.Length - 2) / 4;    // size of one element
                            float[] val = new float[count];
                            for (int i = 0; i < count; i++)
                                val[i] = br.ReadSingle();
                            return val;
                        }
                    case (VarEnum.VT_ARRAY | VarEnum.VT_R8):
                        {
                            int count = (int)(memStream.Length - 2) / Native.BitsPerByte;    // size of one element
                            double[] val = new double[count];
                            for (int i = 0; i < count; i++)
                                val[i] = br.ReadDouble();
                            return val;
                        }
                    case (VarEnum.VT_ARRAY | VarEnum.VT_DATE):
                        {
                            int count = (int)(memStream.Length - 2) / Native.BitsPerByte;    // size of one element
                            DateTime[] val = new DateTime[count];
                            for (int i = 0; i < count; i++)
                            {
                                val[i] = DateTime.FromOADate(br.ReadDouble());
                            }
                            return val;
                        }
                    case (VarEnum.VT_ARRAY | VarEnum.VT_BOOL):
                        {
                            int count = (int)(memStream.Length - 2);    // size of one element
                            bool[] val = new bool[count];
                            for (int i = 0; i < count; i++)
                                val[i] = br.ReadBoolean();
                            return val;
                        }
                    case (VarEnum.VT_ARRAY | VarEnum.VT_DECIMAL):
                        {
                            int count = (int)((memStream.Length - 2) / Native.SizeOfDecimal);    // size of one element
                            decimal[] val = new decimal[count];
                            for (int i = 0; i < count; i++)
                                val[i] = br.ReadDecimal();
                            return val;
                        }
                    case (VarEnum.VT_I1):
                        return br.ReadChar();
                    case (VarEnum.VT_UI1):
                        return br.ReadByte();
                    case (VarEnum.VT_I2):
                        return br.ReadInt16();
                    case (VarEnum.VT_UI2):
                        return br.ReadUInt16();
                    case (VarEnum.VT_I4):
                        return br.ReadInt32();
                    case (VarEnum.VT_UI4):
                        return br.ReadUInt32();
                    case (VarEnum.VT_I8):
                        return br.ReadInt64();
                    case (VarEnum.VT_UI8):
                        return br.ReadUInt64();
                    case (VarEnum.VT_R4):
                        return br.ReadSingle();
                    case (VarEnum.VT_R8):
                        return br.ReadDouble();
                    case (VarEnum.VT_DATE):
                        return DateTime.FromOADate(br.ReadDouble());
                    case (VarEnum.VT_BOOL):
                        return br.ReadBoolean();
                    case (VarEnum.VT_DECIMAL):
                        return br.ReadDecimal();
                    case (VarEnum.VT_BSTR):
                        {
                            byte[] bytestring = br.ReadBytes((int)memStream.Length);
                            return System.Text.Encoding.Unicode.GetString(bytestring);
                        }
                    default:
                        {
                            throw new InvalidOperationException(SR.Get(SRID.InvalidEpInIsf));
                        }
                }
            }
        }
#if OLD_ISF
        /// <summary>
        /// Saves all elements in this list in the stream passed with the tags being generated based on the GuidList
        /// by the caller and using compressionAlgorithm as the preferred algorith identifier. For ExtendedPropertyCollection associated
        /// with Ink, drawing attributes and Point properties, we need to write the tag while saving them and hence
        /// fTag param is true. For strokes, the Tag is stored in the stroke descriptor and hence we don't store the
        /// tag
        /// </summary>
        /// <param name="attributes">Custom attributes to encode</param>
        /// <param name="stream">If stream is null, then size is calculated only.</param>
        /// <param name="guidList"></param>
        /// <param name="compressionAlgorithm"></param>
        /// <param name="fTag"></param>
        /// <returns></returns>
#else
        /// <summary>
        /// Saves all elements in this list in the stream passed with the tags being generated based on the GuidList
        /// by the caller and using compressionAlgorithm as the preferred algorith identifier. For ExtendedPropertyCollection associated
        /// with Ink, drawing attributes and Point properties, we need to write the tag while saving them and hence
        /// fTag param is true. For strokes, the Tag is stored in the stroke descriptor and hence we don't store the
        /// tag
        /// </summary>
        /// <param name="attributes">Custom attributes to encode</param>
        /// <param name="stream">If stream is null, then size is calculated only.</param>
        /// <param name="guidList"></param>
        /// <param name="compressionAlgorithm"></param>
        /// <param name="fTag"></param>
#endif
        internal static uint EncodeAsISF(ExtendedPropertyCollection attributes, Stream stream, GuidList guidList, byte compressionAlgorithm, bool fTag)
        {
            uint cbWrite = 0;

            for (int i = 0; i < attributes.Count; i++)
            {
                ExtendedProperty prop = attributes[i];

                using (MemoryStream localStream = new MemoryStream(10)) //reasonable default
                {
                    ExtendedPropertySerializer.EncodeToStream(prop, localStream);

                    byte[] data = localStream.ToArray(); 

                    cbWrite += ExtendedPropertySerializer.EncodeAsISF(prop.Id, data, stream, guidList, compressionAlgorithm, fTag);
                }
            }

            return cbWrite;
        }

        /// <summary>
        /// Retrieve the guids for the custom attributes that are not known by
        /// the v1 ISF decoder
        /// </summary>
        /// <param name="attributes"></param>
        /// <param name="count">count of guids returned (can be less than return.Length</param>
        /// <returns></returns>
        internal static Guid[] GetUnknownGuids(ExtendedPropertyCollection attributes, out int count)
        {
            Guid[] guids = new Guid[attributes.Count];
            count = 0;
            for (int x = 0; x < attributes.Count; x++)
            {
                ExtendedProperty attribute = attributes[x];
                if (0 == GuidList.FindKnownTag(attribute.Id))
                {
                    guids[count++] = attribute.Id;
                }
            }
            return guids;
        }

        #region Key/Value pair validation helpers
        /// <summary>
        /// Validates the data to be associated with a ExtendedProperty id
        /// </summary>
        /// <param name="id">ExtendedProperty identifier</param>
        /// <param name="value">data</param>
        /// <remarks>Ignores Ids that are not known (e.g. ExtendedProperties)</remarks>
        internal static void Validate(Guid id, object value)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(SR.Get(SRID.InvalidGuid));
            }

            if (id == KnownIds.Color)
            {
                if (!(value is System.Windows.Media.Color))
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidValueType, typeof(System.Windows.Media.Color)), "value");
                }
            }
                // int attributes
            else if (id == KnownIds.CurveFittingError)
            {
                if (!(value.GetType() == typeof(int)))
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidValueType, typeof(int)), "value");
                }
            }
            else if (id == KnownIds.DrawingFlags)
            {
                // ignore validation of flags
                if (value.GetType() != typeof(DrawingFlags))
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidValueType, typeof(DrawingFlags)), "value");
                }
            }
            else if (id == KnownIds.StylusTip)
            {
                Type valueType = value.GetType();
                bool fStylusTipType = ( valueType == typeof(StylusTip) );
                bool fIntType = ( valueType == typeof(int) );

                if ( !fStylusTipType && !fIntType )
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidValueType1, typeof(StylusTip), typeof(int)), "value");
                }
                else if ( !StylusTipHelper.IsDefined((StylusTip)value) )
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidValueOfType, value, typeof(StylusTip)), "value");
                }
            }
            else if (id == KnownIds.StylusTipTransform)
            {
                //
                // StylusTipTransform gets serialized as a String, but at runtime is a Matrix
                //
                Type t = value.GetType();
                if ( t != typeof(String) && t != typeof(Matrix) )
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidValueType1, typeof(String), typeof(Matrix)), "value");
                }
                else if ( t == typeof(Matrix) )
                {
                    Matrix matrix = (Matrix)value;
                    if ( !matrix.HasInverse )
                    {
                        throw new ArgumentException(SR.Get(SRID.MatrixNotInvertible), "value");
                    }
                    if ( MatrixHelper.ContainsNaN(matrix))
                    {
                        throw new ArgumentException(SR.Get(SRID.InvalidMatrixContainsNaN), "value");
                    }
                    if ( MatrixHelper.ContainsInfinity(matrix))
                    {
                        throw new ArgumentException(SR.Get(SRID.InvalidMatrixContainsInfinity), "value");
                    }
}
            }
            else if (id == KnownIds.IsHighlighter)
            {
                if ( value.GetType() != typeof(bool))
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidValueType, typeof(bool)), "value");
                }
            }
            else if ( id == KnownIds.StylusHeight || id == KnownIds.StylusWidth )
            {
                if ( value.GetType() != typeof(double) )
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidValueType, typeof(double)), "value");
                }

                double dVal = (double)value;

                if (id == KnownIds.StylusHeight)
                {
                    if ( Double.IsNaN(dVal) || dVal < DrawingAttributes.MinHeight || dVal > DrawingAttributes.MaxHeight)
                    {
                        throw new ArgumentOutOfRangeException("value", SR.Get(SRID.InvalidDrawingAttributesHeight));
                    }
                }
                else
                {
                    if (Double.IsNaN(dVal) ||  dVal < DrawingAttributes.MinWidth || dVal > DrawingAttributes.MaxWidth)
                    {
                        throw new ArgumentOutOfRangeException("value", SR.Get(SRID.InvalidDrawingAttributesWidth));
                    }
                }
            }
            else if ( id == KnownIds.Transparency )
            {
                if ( value.GetType() != typeof(byte) )
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidValueType, typeof(byte)), "value");
                }

                double dVal = (double)value;
            }
            else
            {
                if ( !UsesEmbeddedTypeInformation(id) )
                {
                    // if this guid used the legacy internal attribute persistence APIs,
                    //      then it doesn't include embedded type information (it's always a byte array)
                    if ( value.GetType() != typeof(byte[]) )
                    {
                        throw new ArgumentException(SR.Get(SRID.InvalidValueType, typeof(byte[])), "value");
                    }
                }
                else
                {
                    // if there is any unsupported type, this call will throw.
                    VarEnum varEnum = SerializationHelper.ConvertToVarEnum(value.GetType(), true);

                    switch (varEnum)
                    {
                        case (VarEnum.VT_ARRAY | VarEnum.VT_I1)://8208
                        case (VarEnum.VT_I1)://16
                        case (VarEnum.VT_ARRAY | VarEnum.VT_DATE)://8199
                        case (VarEnum.VT_DATE)://7
                        {
                            //we have a char or char[], datetime or datetime[], 
                            //we need to write them to a Stream using a BinaryWriter
                            //to see if an exception is thrown so that the exception 
                            //happens now, and not at serialization time...
                            using (MemoryStream stream = new MemoryStream(32))//reasonable default
                            {
                                using (BinaryWriter writer = new BinaryWriter(stream))
                                {
                                    try
                                    {
                                        switch (varEnum)
                                        {
                                            case (VarEnum.VT_ARRAY | VarEnum.VT_I1)://8208
                                            {
                                                writer.Write((char[])value);
                                                break;
                                            }
                                            case (VarEnum.VT_I1)://16
                                            {
                                                writer.Write((char)value);
                                                break;
                                            }
                                            case (VarEnum.VT_ARRAY | VarEnum.VT_DATE)://8199
                                            {
                                                DateTime[] data = (DateTime[])value;
                                                for (int i = 0; i < data.Length; i++)
                                                    writer.Write(data[i].ToOADate());
                                                break;
                                            }
                                            case (VarEnum.VT_DATE)://7
                                            {
                                                DateTime data = (DateTime)value;
                                                writer.Write(data.ToOADate());
                                                break;
                                            }
                                            default:
                                            {
                                                Debug.Assert(false, "Missing case statement!");
                                                break;
                                            }
                                        }
                                    }
                                    catch (ArgumentException ex)
                                    {
                                        //catches bad char & char[]
                                        throw new ArgumentException(SR.Get(SRID.InvalidDataInISF), ex);
                                    }
                                    catch (OverflowException ex)
                                    {
                                        //catches bad DateTime
                                        throw new ArgumentException(SR.Get(SRID.InvalidDataInISF), ex);
                                    }
                                }
                            }
                            break;
                        }
                        //do nothing in the default case...
                    }
                }
                return;
            }
        }
        #endregion // Key/Value pair validation helpers

    }
}
