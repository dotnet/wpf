// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  Implementation of a helper class that provides a fully functional Stream on a restricted functionality
//  Compression stream (System.IO.Compression.DeflateStream).
//
//
//

using System;
using System.IO;
using System.IO.Compression;                // for DeflateStream
using System.Diagnostics;

using System.IO.Packaging;
using System.Windows;

namespace MS.Internal.IO.Packaging
{
    //------------------------------------------------------
    //
    //  Internal Members
    //
    //------------------------------------------------------
    /// <summary>
    /// Emulates a fully functional stream using restricted functionality DeflateStream
    /// </summary>
    internal class DeflateEmulationTransform : IDeflateTransform
    {
        /// <summary>
        /// Extract from DeflateStream to temp stream
        /// </summary>
        /// <remarks>Caller is responsible for correctly positioning source and sink stream pointers before calling.</remarks>
        public void Decompress(Stream source, Stream sink)
        {
            // for non-empty stream create deflate stream that can 
            // actually decompress
            using (DeflateStream deflateStream = new DeflateStream(
                source,                    // source of compressed data
                CompressionMode.Decompress,     // compress or decompress
                true))                          // leave base stream open when the deflate stream is closed
            {
                int bytesRead = 0;
                do
                {
                    bytesRead = deflateStream.Read(Buffer, 0, Buffer.Length);
                    if (bytesRead > 0)
                        sink.Write(Buffer, 0, bytesRead);
} while (bytesRead > 0);
            }
        }

        /// <summary>
        /// Compress from the temp stream into the base stream
        /// </summary>
        /// <remarks>Caller is responsible for correctly positioning source and sink stream pointers before calling.</remarks>
        public void Compress(Stream source, Stream sink)
        {
            // create deflate stream that can actually compress or decompress
            using (DeflateStream deflateStream = new DeflateStream(
                sink,                       // destination for compressed data
                CompressionMode.Compress,   // compress or decompress
                true))                      // leave base stream open when the deflate stream is closed
            {
                // persist to deflated stream from working stream
                int bytesRead = 0;
                do
                {
                    bytesRead = source.Read(Buffer, 0, Buffer.Length);
                    if (bytesRead > 0)
                        deflateStream.Write(Buffer, 0, bytesRead);
                } while (bytesRead > 0);
            }

            // truncate if necessary and possible
            if (sink.CanSeek)
                sink.SetLength(sink.Position);
        }

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------
        private byte[] Buffer
        {
            get
            {
                if (_buffer == null)
                    _buffer = new byte[0x1000];     // 4k 

                return _buffer;
            }
        }

        //------------------------------------------------------
        //
        //  Private Members
        //
        //------------------------------------------------------
        private byte[] _buffer;     // alloc and re-use to reduce memory fragmentation
                                    // this is safe because we are not thread-safe
    }
}
