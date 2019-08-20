// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Markup;    // for XmlLanguage
using System.Windows.Media;

using MS.Internal;
using MS.Internal.PresentationCore;
using MS.Utility;
using MS.Internal.FontCache;

// Since we disable PreSharp warnings in this file, we first need to disable warnings about unknown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691


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

    internal class TrueTypeFontDriver
    {
        #region Font constants, structures and enumerations

        private struct DirectoryEntry
        {
            internal TrueTypeTags   tag;
            internal CheckedPointer pointer;
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

        #region Byte, Short, Long etc. accesss to CheckedPointers

        /// <summary>
        /// The follwoing APIs extract OpenType variable types from OpenType font
        /// files. OpenType variables are stored big-endian, and the type are named
        /// as follows:
        ///     Byte   -  signed     8 bit
        ///     UShort -  unsigned   16 bit
        ///     Short  -  signed     16 bit
        ///     ULong  -  unsigned   32 bit
        ///     Long   -  signed     32 bit
        /// </summary>
        private static ushort ReadOpenTypeUShort(CheckedPointer pointer)
        {
            unsafe
            {
                byte * readBuffer = (byte *)pointer.Probe(0, 2);
                ushort result = (ushort)((readBuffer[0] << 8) + readBuffer[1]);
                return result;
            }
        }

        private static int ReadOpenTypeLong(CheckedPointer pointer)
        {
            unsafe
            {
                byte * readBuffer = (byte *)pointer.Probe(0, 4);
                int result = (int)((((((readBuffer[0] << 8) + readBuffer[1]) << 8) + readBuffer[2]) << 8) + readBuffer[3]);
                return result;
            }
        }

        #endregion Byte, Short, Long etc. accesss to CheckedPointers

        #region Constructor and general helpers

        internal TrueTypeFontDriver(UnmanagedMemoryStream unmanagedMemoryStream, Uri sourceUri)
        {
            _sourceUri = sourceUri;
            _unmanagedMemoryStream = unmanagedMemoryStream;
            _fileStream = new CheckedPointer(unmanagedMemoryStream);

            try
            {
                CheckedPointer seekPosition = _fileStream;

                TrueTypeTags typeTag = (TrueTypeTags)ReadOpenTypeLong(seekPosition);
                seekPosition += 4;

                if (typeTag == TrueTypeTags.TTC_TTCF)
                {
                    // this is a TTC file, we need to decode the ttc header
                    _technology = FontTechnology.TrueTypeCollection;
                    seekPosition += 4; // skip version
                    _numFaces = ReadOpenTypeLong(seekPosition);
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
                // convert exceptions from CheckedPointer to FileFormatException
                throw new FileFormatException(SourceUri, e);
            }
        }

        internal void SetFace(int faceIndex)
        {
            if (_technology == FontTechnology.TrueTypeCollection)
            {
                if (faceIndex < 0 || faceIndex >= _numFaces)
                    throw new ArgumentOutOfRangeException("faceIndex");
            }
            else
            {
                if (faceIndex != 0)
                    throw new ArgumentOutOfRangeException("faceIndex", SR.Get(SRID.FaceIndexValidOnlyForTTC));
            }

            try
            {
                CheckedPointer seekPosition = _fileStream + 4;

                if (_technology == FontTechnology.TrueTypeCollection)
                {
                    // this is a TTC file, we need to decode the ttc header

                    // skip version, num faces, OffsetTable array
                    seekPosition += (4 + 4 + 4 * faceIndex);

                    _directoryOffset = ReadOpenTypeLong(seekPosition);

                    seekPosition = _fileStream + (_directoryOffset + 4);
                    // 4 means that we skip the version number
                }

                _faceIndex = faceIndex;

                int numTables = ReadOpenTypeUShort(seekPosition);
                seekPosition += 2;

                // quick check for malformed fonts, see if numTables is too large
                // file size should be >= sizeof(offset table) + numTables * (sizeof(directory entry) + minimum table size (4))
                long minimumFileSize = (4 + 2 + 2 + 2 + 2) + numTables * (4 + 4 + 4 + 4 + 4);
                if (_fileStream.Size < minimumFileSize)
                {
                    throw new FileFormatException(SourceUri);
                }

                _tableDirectory = new DirectoryEntry[numTables];

                // skip searchRange, entrySelector and rangeShift
                seekPosition += 6;

                // I can't use foreach here because C# disallows modifying the current value
                for (int i = 0; i < _tableDirectory.Length; ++i)
                {
                    _tableDirectory[i].tag = (TrueTypeTags)ReadOpenTypeLong(seekPosition);
                    seekPosition += 8; // skip checksum
                    int offset = ReadOpenTypeLong(seekPosition);
                    seekPosition += 4;
                    int length = ReadOpenTypeLong(seekPosition);
                    seekPosition += 4;

                    _tableDirectory[i].pointer = _fileStream.CheckedProbe(offset, length);
                }
            }
            catch (ArgumentOutOfRangeException e)
            {
                // convert exceptions from CheckedPointer to FileFormatException
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
            int fileSize = _fileStream.Size;
            unsafe
            {
                void* fontData = _fileStream.Probe(0, fileSize);

                // Since we currently don't have a way to subset CFF fonts, just return a copy of the font.
                if (_technology == FontTechnology.PostscriptOpenType)
                {
                    byte[] fontCopy = new byte[fileSize];
                    Marshal.Copy((IntPtr)fontData, fontCopy, 0, fileSize);
                    return fontCopy;
                }

                ushort[] glyphArray;
                if (glyphs == null || glyphs.Count == 0)
                    glyphArray = null;
                else
                {
                    glyphArray = new ushort[glyphs.Count];
                    glyphs.CopyTo(glyphArray, 0);
                }

                return TrueTypeSubsetter.ComputeSubset(fontData, fileSize, SourceUri, _directoryOffset, glyphArray);
            }
        }

            
        #endregion Public methods and properties   
        
        #region Fields

        // file-specific state
        private CheckedPointer          _fileStream;
        private UnmanagedMemoryStream   _unmanagedMemoryStream;
        private Uri                     _sourceUri;
        private int                     _numFaces;
        private FontTechnology          _technology;

        // face-specific state
        private int _faceIndex;
        private int _directoryOffset; // table directory offset for TTC, 0 for TTF
        private DirectoryEntry[] _tableDirectory;

        #endregion
    }
}

