// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//#define OLD_ISF

using MS.Utility;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Ink;
using MS.Internal.Ink.InkSerializedFormat;


using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

namespace MS.Internal.Ink.InkSerializedFormat
{
    internal class Compressor 
#if OLD_ISF
        : IDisposable
#endif
    {
#if OLD_ISF
        /// <summary>
        /// Non-static members
        /// </summary>
        private MS.Win32.Penimc.CompressorSafeHandle _compressorHandle;

        /// <summary>
        /// Compressor constructor.  This is called by our ISF decompression
        /// after reading the ISF header that indicates which type of compression
        /// was performed on the ISF when it was being compressed.
        /// </summary>
        /// <param name="data">a byte[] specifying the compressor used to compress the ISF being decompressed</param>
        /// <param name="size">expected initially to be the length of data, it IsfLoadCompressor sets it to the 
        ///                    length of the header that is read.  They should always match, but in cases where they 
        ///                    don't, we immediately fail</param>
        internal Compressor(byte[] data, ref uint size)
        {
            if (data == null || data.Length != size)
            {
                //we don't raise any information that could be used to attack our ISF code
                //a simple 'ISF Operation Failed' is sufficient since the user can't do 
                //anything to fix bogus ISF
                throw new InvalidOperationException(StrokeCollectionSerializer.ISFDebugMessage(SR.Get(SRID.InitializingCompressorFailed)));
            }
            
            _compressorHandle = MS.Win32.Penimc.UnsafeNativeMethods.IsfLoadCompressor(data, ref size);
            if (_compressorHandle.IsInvalid)
            {
                //we don't raise any information that could be used to attack our ISF code
                //a simple 'ISF Operation Failed' is sufficient since the user can't do 
                //anything to fix bogus ISF
                throw new InvalidOperationException(StrokeCollectionSerializer.ISFDebugMessage(SR.Get(SRID.InitializingCompressorFailed)));
            }
        }
        /// <summary>
        /// DecompressPacketData - take a byte[] or a subset of a byte[] and decompresses it into 
        ///     an int[] of packet data (for example, x's in a Stroke)
        /// </summary>
        /// <param name="compressor">
        ///     The compressor used to decompress this byte[] of packet data (for example, x's in a Stroke)
        ///     Can be null
        /// </param>
        /// <param name="compressedInput">The byte[] to decompress</param>
        /// <param name="size">In: the max size of the subset of compressedInput to read, out: size read</param>
        /// <param name="decompressedPackets">The int[] to write the packet data to</param>
#else
        /// <summary>
        /// DecompressPacketData - take a byte[] or a subset of a byte[] and decompresses it into 
        ///     an int[] of packet data (for example, x's in a Stroke)
        /// </summary>
        /// <param name="compressedInput">The byte[] to decompress</param>
        /// <param name="size">In: the max size of the subset of compressedInput to read, out: size read</param>
        /// <param name="decompressedPackets">The int[] to write the packet data to</param>
#endif
        internal static void DecompressPacketData(
#if OLD_ISF
            Compressor compressor,
#endif
            byte[] compressedInput,
            ref uint size,
            int[] decompressedPackets)
        {
#if OLD_ISF
            //
            // lock to prevent multi-threaded vulnerabilities 
            //
            lock (_compressSync)
            {
#endif
                if (compressedInput == null ||
                    size > compressedInput.Length ||
                    decompressedPackets == null)
                {
                    //we don't raise any information that could be used to attack our ISF code
                    //a simple 'ISF Operation Failed' is sufficient since the user can't do 
                    //anything to fix bogus ISF
                    throw new InvalidOperationException(StrokeCollectionSerializer.ISFDebugMessage(SR.Get(SRID.DecompressPacketDataFailed)));
                }
#if OLD_ISF
                uint size2 = size;
#endif

                size = AlgoModule.DecompressPacketData(compressedInput, decompressedPackets);

#if OLD_ISF
                MS.Win32.Penimc.CompressorSafeHandle safeCompressorHandle = (compressor == null) ?
                                                    MS.Win32.Penimc.CompressorSafeHandle.Null :
                                                    compressor._compressorHandle;

                int[] decompressedPackets2 = new int[decompressedPackets.Length];
                byte algo = AlgoModule.NoCompression;
                int hr = MS.Win32.Penimc.UnsafeNativeMethods.IsfDecompressPacketData(safeCompressorHandle,
                                                                                            compressedInput,
                                                                                            ref size2,
                                                                                            (uint)decompressedPackets2.Length,
                                                                                            decompressedPackets2,
                                                                                            ref algo);
                if (0 != hr)
                {
                    //we don't raise any information that could be used to attack our ISF code
                    //a simple 'ISF Operation Failed' is sufficient since the user can't do 
                    //anything to fix bogus ISF
                    throw new InvalidOperationException(StrokeCollectionSerializer.ISFDebugMessage("IsfDecompressPacketData returned: " + hr.ToString(CultureInfo.InvariantCulture)));
                }

                if (size != size2)
                {
                    throw new InvalidOperationException("MAGIC EXCEPTION: Packet data bytes read didn't match with new uncompression");
                }
                for (int i = 0; i < decompressedPackets.Length; i++)
                {
                    if (decompressedPackets[i] != decompressedPackets2[i])
                    {
                        throw new InvalidOperationException("MAGIC EXCEPTION: Packet data didn't match with new uncompression at index " + i.ToString());
                    }
                }
            }
#endif
        }

#if OLD_ISF
        /// <summary>
        /// DecompressPropertyData - decompresses a byte[] representing property data (such as DrawingAttributes.Color)
        /// </summary>
        /// <param name="input">The byte[] to decompress</param>
#else
        /// <summary>
        /// DecompressPropertyData - decompresses a byte[] representing property data (such as DrawingAttributes.Color)
        /// </summary>
        /// <param name="input">The byte[] to decompress</param>
#endif
        internal static byte[] DecompressPropertyData(byte[] input)
        {
#if OLD_ISF
            //
            // lock to prevent multi-threaded vulnerabilities 
            //
            lock (_compressSync)
            {
#endif
                if (input == null)
                {
                    //we don't raise any information that could be used to attack our ISF code
                    //a simple 'ISF Operation Failed' is sufficient since the user can't do 
                    //anything to fix bogus ISF
                    throw new InvalidOperationException(StrokeCollectionSerializer.ISFDebugMessage(SR.Get(SRID.DecompressPropertyFailed)));
                }

                byte[] data = AlgoModule.DecompressPropertyData(input);
#if OLD_ISF
                uint size = 0;
                byte algo = 0;
                int hr = MS.Win32.Penimc.UnsafeNativeMethods.IsfDecompressPropertyData(input, (uint)input.Length, ref size, null, ref algo);
                if (0 == hr)
                {
                    byte[] data2 = new byte[size];
                    hr = MS.Win32.Penimc.UnsafeNativeMethods.IsfDecompressPropertyData(input, (uint)input.Length, ref size, data2, ref algo);
                    if (0 == hr)
                    {

                        if (data.Length != data2.Length)
                        {
                            throw new InvalidOperationException("MAGIC EXCEPTION: Property bytes length when decompressed didn't match with new uncompression");
                        }
                        for (int i = 0; i < data.Length; i++)
                        {
                            if (data[i] != data2[i])
                            {
                                throw new InvalidOperationException("MAGIC EXCEPTION: Property data didn't match with new property uncompression at index " + i.ToString());
                            }
                        }
                        return data;
                    }
                }
                //we don't raise any information that could be used to attack our ISF code
                //a simple 'ISF Operation Failed' is sufficient since the user can't do 
                //anything to fix bogus ISF
                throw new InvalidOperationException(StrokeCollectionSerializer.ISFDebugMessage("IsfDecompressPropertyData returned: " + hr.ToString(CultureInfo.InvariantCulture)));
#else
                return data;
#endif

#if OLD_ISF
            }
#endif
        }

#if OLD_ISF
        /// <summary>
        /// CompressPropertyData - compresses property data
        /// </summary>
        /// <param name="data">The property data to compress</param>
        /// <param name="algorithm">In: the desired algorithm to use.  Out: the algorithm used</param>
        /// <param name="outputSize">In: if output is not null, the size of output.  Out: the size required if output is null</param>
        /// <param name="output">The byte[] to writ the compressed data to</param>
        internal static void CompressPropertyData(byte[] data, ref byte algorithm, ref uint outputSize, byte[] output)
        {
            //
            // lock to prevent multi-threaded vulnerabilities 
            //
            lock (_compressSync)
            {
                //
                // it is valid to pass is null for output to check to see what the 
                // required buffer size is.  We want to guard against when output is not null
                // and outputSize doesn't match, as this is passed directly to unmanaged code
                // and could result in bytes being written past the end of output buffer
                //
                if (output != null && outputSize != output.Length)
                {
                    //we don't raise any information that could be used to attack our ISF code
                    //a simple 'ISF Operation Failed' is sufficient since the user can't do 
                    //anything to fix bogus ISF
                    throw new InvalidOperationException(SR.Get(SRID.IsfOperationFailed));
                }
                int hr = MS.Win32.Penimc.UnsafeNativeMethods.IsfCompressPropertyData(data, (uint)data.Length, ref algorithm, ref outputSize, output);
                if (0 != hr)
                {
                    //we don't raise any information that could be used to attack our ISF code
                    //a simple 'ISF Operation Failed' is sufficient since the user can't do 
                    //anything to fix bogus ISF
                    throw new InvalidOperationException(StrokeCollectionSerializer.ISFDebugMessage("IsfCompressPropertyData returned: " + hr.ToString(CultureInfo.InvariantCulture)));
                }
            }
        }
#endif

        /// <summary>
        /// CompressPropertyData
        /// </summary>
        /// <param name="data"></param>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        internal static byte[] CompressPropertyData(byte[] data, byte algorithm)
        {
            if (data == null)
            {
                //we don't raise any information that could be used to attack our ISF code
                //a simple 'ISF Operation Failed' is sufficient since the user can't do 
                //anything to fix bogus ISF
                throw new ArgumentNullException("data");
            }

            return AlgoModule.CompressPropertyData(data, algorithm);
        }

#if OLD_ISF        
        /// <summary>
        /// CompressPropertyData - compresses property data using the compression defined by 'compressor'
        /// </summary>
        /// <param name="compressor">The compressor to use.  This can be null for default compression</param>
        /// <param name="input">The int[] of packet data to compress</param>
        /// <param name="algorithm">In: the desired algorithm to use.  Out: the algorithm used</param>
        /// <returns>the compressed data in a byte[]</returns>
#else
        /// <summary>
        /// CompressPropertyData - compresses property data using the compression defined by 'compressor'
        /// </summary>
        /// <param name="input">The int[] of packet data to compress</param>
        /// <param name="algorithm">In: the desired algorithm to use.  Out: the algorithm used</param>
        /// <returns>the compressed data in a byte[]</returns>
#endif
        internal static byte[] CompressPacketData(
#if OLD_ISF
                Compressor compressor, 
#endif
                int[] input, 
                ref byte algorithm)
        {
#if OLD_ISF
            //
            // lock to prevent multi-threaded vulnerabilities 
            //
            lock (_compressSync)
            {
#endif
                if (input == null)
                {
                    //we don't raise any information that could be used to attack our ISF code
                    //a simple 'ISF Operation Failed' is sufficient since the user can't do 
                    //anything to fix bogus ISF
                    throw new InvalidOperationException(SR.Get(SRID.IsfOperationFailed));
                }

                byte[] data = AlgoModule.CompressPacketData(input, algorithm);
#if OLD_ISF
                uint cbOutSize = 0;
                MS.Win32.Penimc.CompressorSafeHandle safeCompressorHandle = (compressor == null) ?
                    MS.Win32.Penimc.CompressorSafeHandle.Null : compressor._compressorHandle;
                int hr = MS.Win32.Penimc.UnsafeNativeMethods.IsfCompressPacketData(safeCompressorHandle, input, (uint)input.Length, ref algorithm, ref cbOutSize, null);
                if (0 == hr)
                {
                    byte[] data2 = new byte[cbOutSize];
                    hr = MS.Win32.Penimc.UnsafeNativeMethods.IsfCompressPacketData(safeCompressorHandle, input, (uint)input.Length, ref algorithm, ref cbOutSize, data2);
                    if (0 == hr)
                    {
                        //see if data matches
                        if (data2.Length != data.Length)
                        {
                            throw new InvalidOperationException("MAGIC EXCEPTION: Packet data length didn't match with new compression");
                        }
                        for (int i = 0; i < data2.Length; i++)
                        {
                            if (data2[i] != data[i])
                            {
                                throw new InvalidOperationException("MAGIC EXCEPTION: Packet data didn't match with new compression at index " + i.ToString());
                            }
                        }

                        return data;
                    }
                }
                throw new InvalidOperationException(StrokeCollectionSerializer.ISFDebugMessage("IsfCompressPacketData returned:" + hr.ToString(CultureInfo.InvariantCulture)));

            }
#else
                return data;
#endif
        }

        /// <summary>
        /// Thread static algo module
        /// </summary>
        [ThreadStatic]
        private static AlgoModule _algoModule;

        /// <summary>
        /// Private AlgoModule, one per thread, lazy init'd
        /// </summary>
        private static AlgoModule AlgoModule
        {
            get
            {
                if (_algoModule == null)
                {
                    _algoModule = new AlgoModule();
                }
                return _algoModule;
            }
        }

#if OLD_ISF
        private static object _compressSync = new object();
        private bool _disposed = false;

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _compressorHandle.Dispose();
            _disposed = true;
            _compressorHandle = null;
        }
#endif
    }
}
