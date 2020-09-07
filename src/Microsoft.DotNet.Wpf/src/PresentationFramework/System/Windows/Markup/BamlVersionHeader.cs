// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Globalization;
using System.Diagnostics;

using MS.Internal.IO.Packaging.CompoundFile;

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{
    internal class BamlVersionHeader
    {
        // The current BAML record version.  This is incremented whenever
        // the BAML format changes
        // Baml Format Breaking Changes should change this.

        internal static readonly VersionPair BamlWriterVersion;

        static BamlVersionHeader()
        {
            // Initialize the Version number this way so that it can be
            // seen in the Lutz Reflector.
            BamlWriterVersion = new VersionPair(0, 96);
        }

        public BamlVersionHeader()
        {
            _bamlVersion = new FormatVersion("MSBAML", BamlWriterVersion);
        }

        public FormatVersion BamlVersion
        {
            get { return _bamlVersion; }
#if !PBTCOMPILER
            set { _bamlVersion = value; }
#endif
        }


        // This is used by Async loading to measure if the whole record is present
        static public int BinarySerializationSize
        {
            get
            {
                // Unicode "MSBAML" = 12
                // + 4 bytes length header = 12 + 4 = 16
                // + 3*(16bit MinorVer + 16bit MajorVer) = 16+(3*(2+2))= 28
                // For product stability the size of this data structure
                // shouldn't change anyway.
                return 28;
            }
        }


#if !PBTCOMPILER
        internal void  LoadVersion(BinaryReader bamlBinaryReader)
        {
#if DEBUG
            long posStart = bamlBinaryReader.BaseStream.Position;
#endif

            BamlVersion = FormatVersion.LoadFromStream(bamlBinaryReader.BaseStream);

#if DEBUG
            long posEnd = bamlBinaryReader.BaseStream.Position;
            Debug.Assert((posEnd-posStart) == BamlVersionHeader.BinarySerializationSize,
                             "Incorrect Baml Version Header Size");
#endif

            // We're assuming that only major versions are significant for compatibility,
            // so if we have a major version in the file that is higher than that in
            // the code, we can't read it.
            if (BamlVersion.ReaderVersion != BamlWriterVersion)
            {
                throw new InvalidOperationException(SR.Get(SRID.ParserBamlVersion,
                                         (BamlVersion.ReaderVersion.Major.ToString(CultureInfo.CurrentCulture) + "." +
                                          BamlVersion.ReaderVersion.Minor.ToString(CultureInfo.CurrentCulture)),
                                         (BamlWriterVersion.Major.ToString(CultureInfo.CurrentCulture) + "." +
                                          BamlWriterVersion.Minor.ToString(CultureInfo.CurrentCulture))));
            }
        }
#endif


        internal void WriteVersion(BinaryWriter bamlBinaryWriter)
        {
#if DEBUG
            long posStart = bamlBinaryWriter.BaseStream.Position;
#endif
            BamlVersion.SaveToStream(bamlBinaryWriter.BaseStream);

#if DEBUG
            long posEnd = bamlBinaryWriter.BaseStream.Position;
            if(-1 == posStart)
            {
                long length = bamlBinaryWriter.BaseStream.Length;
                Debug.Assert(length == BamlVersionHeader.BinarySerializationSize,
                                 "Incorrect Baml Version Header Size");
            }
            else
            {
                Debug.Assert((posEnd-posStart) == BamlVersionHeader.BinarySerializationSize,
                                 "Incorrect Baml Version Header Size");
            }
#endif
        }

        FormatVersion _bamlVersion;
    }
}




