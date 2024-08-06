// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Buffers.Binary;
using System.Collections.Generic;

using MS.Internal.PresentationCore;

namespace MS.Internal.FontFace
{
    /// <summary>
    /// Font technology.
    /// </summary>
    internal enum FontTechnology
    {
        // this enum need to be kept in order of preference that we want to use with duplicate font face,
        // highest value will win in case of duplicate
        PostscriptOpenType,
        TrueType,
        TrueTypeCollection
    }

    internal sealed unsafe class TrueTypeFontDriver
    {
        #region Font constants, structures and enumerations

        /// <summary>
        /// Describes a directory entry within TrueType Font.
        /// </summary>
        private readonly unsafe struct FontDirectoryEntry
        {
            public readonly TrueTypeTags  Tag;
            public readonly byte*         Data;
            public readonly int           Length;

            public FontDirectoryEntry(TrueTypeTags tag, byte* data, int length)
            {
                Tag = tag;
                Data = data;
                Length = length;
            }
        }

        private enum TrueTypeTags : int
        {
            CharToIndexMap      = 0x636d6170,        /* 'cmap' */
            ControlValue        = 0x63767420,        /* 'cvt ' */
            BitmapData          = 0x45424454,        /* 'EBDT' */
            BitmapLocation      = 0x45424c43,        /* 'EBLC' */
            BitmapScale         = 0x45425343,        /* 'EBSC' */
            Editor0             = 0x65647430,        /* 'edt0' */
            Editor1             = 0x65647431,        /* 'edt1' */
            Encryption          = 0x63727970,        /* 'cryp' */
            FontHeader          = 0x68656164,        /* 'head' */
            FontProgram         = 0x6670676d,        /* 'fpgm' */
            GridfitAndScanProc  = 0x67617370,        /* 'gasp' */
            GlyphDirectory      = 0x67646972,        /* 'gdir' */
            GlyphData           = 0x676c7966,        /* 'glyf' */
            HoriDeviceMetrics   = 0x68646d78,        /* 'hdmx' */
            HoriHeader          = 0x68686561,        /* 'hhea' */
            HorizontalMetrics   = 0x686d7478,        /* 'hmtx' */
            IndexToLoc          = 0x6c6f6361,        /* 'loca' */
            Kerning             = 0x6b65726e,        /* 'kern' */
            LinearThreshold     = 0x4c545348,        /* 'LTSH' */
            MaxProfile          = 0x6d617870,        /* 'maxp' */
            NamingTable         = 0x6e616d65,        /* 'name' */
            OS_2                = 0x4f532f32,        /* 'OS/2' */
            Postscript          = 0x706f7374,        /* 'post' */
            PreProgram          = 0x70726570,        /* 'prep' */
            VertDeviceMetrics   = 0x56444d58,        /* 'VDMX' */
            VertHeader          = 0x76686561,        /* 'vhea' */
            VerticalMetrics     = 0x766d7478,        /* 'vmtx' */
            PCLT                = 0x50434C54,        /* 'PCLT' */
            TTO_GSUB            = 0x47535542,        /* 'GSUB' */
            TTO_GPOS            = 0x47504F53,        /* 'GPOS' */
            TTO_GDEF            = 0x47444546,        /* 'GDEF' */
            TTO_BASE            = 0x42415345,        /* 'BASE' */
            TTO_JSTF            = 0x4A535446,        /* 'JSTF' */
            OTTO                = 0x4f54544f,        // Adobe OpenType 'OTTO'
            TTC_TTCF            = 0x74746366         // 'ttcf'
        }

        #endregion

        #region Byte, Short, Long etc. types in OpenType fonts

        // The following APIs extract OpenType variable types from OpenType font
        // files. OpenType variables are stored big-endian, and the type are named
        // as follows:
        //     Byte   -  signed     8 bit
        //     UShort -  unsigned   16 bit
        //     Short  -  signed     16 bit
        //     ULong  -  unsigned   32 bit
        //     Long   -  signed     32 bit

        #endregion Byte, Short, Long etc. types in OpenType fonts

        #region Constructor and general helpers

        /// <summary>
        /// Initializes instance from the underlying buffer held by <paramref name="unmanagedMemoryStream"/>.
        /// The buffer is expected to be pinned (non-movable).
        /// </summary>
        /// <param name="unmanagedMemoryStream">TrueType font buffer/file mapping.</param>
        /// <param name="sourceUri"></param>
        /// <exception cref="FileFormatException">Thrown when the buffer is not a TrueType font.</exception>
        internal TrueTypeFontDriver(UnmanagedMemoryStream unmanagedMemoryStream, Uri sourceUri)
        {
            // Check whether we have valid length
            long streamLength = unmanagedMemoryStream.Length;
            ArgumentOutOfRangeException.ThrowIfNegative(streamLength);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(streamLength, int.MaxValue);

            // Assign parameters
            _sourceUri = sourceUri;
            _unmanagedMemoryStream = unmanagedMemoryStream;

            try
            {
                ReadOnlySpan<byte> fileStream = new(unmanagedMemoryStream.PositionPointer, (int)streamLength);

                TrueTypeTags typeTag = (TrueTypeTags)BinaryPrimitives.ReadInt32BigEndian(fileStream);
                fileStream = fileStream.Slice(4);

                if (typeTag == TrueTypeTags.TTC_TTCF)
                {
                    // this is a TTC file, we need to decode the ttc header
                    _technology = FontTechnology.TrueTypeCollection;
                    fileStream = fileStream.Slice(4); // skip version
                    _numFaces = BinaryPrimitives.ReadInt32BigEndian(fileStream);
                }
                else if (typeTag == TrueTypeTags.OTTO)
                {
                    _technology = FontTechnology.PostscriptOpenType;
                    _numFaces = 1;
                }
                else
                {
                    _technology = FontTechnology.TrueType;
                    _numFaces = 1;
                }
            }
            catch (ArgumentOutOfRangeException e)
            {
                // convert exceptions from ReadOnlySpan<T> to FileFormatException
                throw new FileFormatException(SourceUri, e);
            }
        }

        internal void SetFace(int faceIndex)
        {
            if (_technology == FontTechnology.TrueTypeCollection)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(faceIndex);
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(faceIndex, _numFaces);
            }
            else
            {
                if (faceIndex != 0)
                    throw new ArgumentOutOfRangeException(nameof(faceIndex), SR.FaceIndexValidOnlyForTTC);
            }

            try
            {
                // 4 means that we skip the type tag
                byte* ptrFontFile = _unmanagedMemoryStream.PositionPointer;
                int lenFontFile = (int)_unmanagedMemoryStream.Length;

                ReadOnlySpan<byte> fileStream = new(ptrFontFile, lenFontFile);
                fileStream = fileStream.Slice(4);

                if (_technology == FontTechnology.TrueTypeCollection)
                {   // this is a TTC file, we need to decode the ttc header

                    // skip version, num faces, OffsetTable array
                    _directoryOffset = BinaryPrimitives.ReadInt32BigEndian(fileStream.Slice(4 + 4 + 4 * faceIndex));

                    // 4 means that we skip the version number
                    fileStream = fileStream.Slice(_directoryOffset);
                }

                _faceIndex = faceIndex;

                int numTables = BinaryPrimitives.ReadUInt16BigEndian(fileStream);
                fileStream = fileStream.Slice(2);

                // quick check for malformed fonts, see if numTables is too large
                // file size should be >= sizeof(offset table) + numTables * (sizeof(directory entry) + minimum table size (4))
                long minimumFileSize = (4 + 2 + 2 + 2 + 2) + numTables * (4 + 4 + 4 + 4 + 4);
                if (lenFontFile < minimumFileSize)
                {
                    throw new FileFormatException(SourceUri);
                }

                // skip searchRange, entrySelector and rangeShift
                fileStream = fileStream.Slice(6);

                ReadOnlySpan<byte> fontFile = new(ptrFontFile, lenFontFile);
                _tableDirectory = new FontDirectoryEntry[numTables];
                for (int i = 0; i < _tableDirectory.Length; i++)
                {
                    TrueTypeTags tag = (TrueTypeTags)BinaryPrimitives.ReadInt32BigEndian(fileStream);
                    fileStream = fileStream.Slice(8); // skip checksum
                    int offset = BinaryPrimitives.ReadInt32BigEndian(fileStream);
                    fileStream = fileStream.Slice(4);
                    int length = BinaryPrimitives.ReadInt32BigEndian(fileStream);
                    fileStream = fileStream.Slice(4);

                    //fontFile.Slice(offset, length).Length checks validity of the offset/length
                    _tableDirectory[i] = new(tag, ptrFontFile + offset, fontFile.Slice(offset, length).Length);
                }
            }
            catch (ArgumentOutOfRangeException e)
            {
                // convert exceptions from ReadOnlySpan<T> to FileFormatException
                throw new FileFormatException(SourceUri, e);
            }
        }

        #endregion

        #region Public methods and properties

        internal int NumFaces
        {
            get
            {
                return _numFaces;
            }
        }

        private Uri SourceUri
        {
            get
            {
                return _sourceUri;
            }
        }

        /// <summary>
        /// Create font subset that includes glyphs in the input collection.
        /// </summary>
        internal byte[] ComputeFontSubset(ICollection<ushort> glyphs)
        {
            // Since we currently don't have a way to subset CFF fonts, just return a copy of the font.
            if (_technology == FontTechnology.PostscriptOpenType)
            {
                return new ReadOnlySpan<byte>(_unmanagedMemoryStream.PositionPointer, (int)_unmanagedMemoryStream.Length).ToArray();
            }

            ushort[] glyphArray = null;
            if (glyphs?.Count > 0)
            {
                glyphArray = new ushort[glyphs.Count];
                glyphs.CopyTo(glyphArray, 0);
            }

            return TrueTypeSubsetter.ComputeSubset(_unmanagedMemoryStream.PositionPointer, (int)_unmanagedMemoryStream.Length, SourceUri, _directoryOffset, glyphArray);
        }


        #endregion Public methods and properties   

        #region Fields

        // file-specific state
        private readonly UnmanagedMemoryStream   _unmanagedMemoryStream;
        private readonly Uri                     _sourceUri;
        private readonly int                     _numFaces;
        private readonly FontTechnology          _technology;

        // face-specific state
        private int _faceIndex;
        private int _directoryOffset; // table directory offset for TTC, 0 for TTF
        private FontDirectoryEntry[] _tableDirectory;

        #endregion
    }
}

