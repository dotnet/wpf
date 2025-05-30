﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description:
//  Implementation of the FormatVersion class, which describes the versioning
//  of an individual "format feature" within a compound file.
//

using System;
using System.IO;

#if PBTCOMPILER
using MS.Utility;     // For SR.cs
#else
using System.Windows;
using MS.Internal.WindowsBase;
#endif

namespace MS.Internal.IO.Packaging.CompoundFile
{
    ///<summary>Class for manipulating version object</summary>
    internal class FormatVersion
    {
        //------------------------------------------------------
        //
        //  Public Constructors
        //
        //------------------------------------------------------

        #region Constructors

#if !PBTCOMPILER
        /// <summary>
        /// Constructor for FormatVersion
        /// </summary>
        private FormatVersion()
        {
        }
#endif

        /// <summary>
        /// Constructor for FormatVersion with given featureId and version
        /// </summary>
        /// <param name="featureId">feature identifier</param>
        /// <param name="version">version</param>
        /// <remarks>reader, updater, and writer versions are set to version</remarks>
        public FormatVersion(string featureId, VersionPair version)
            : this(featureId, version, version, version)
        {
        }

        /// <summary>
        /// Constructor for FormatVersion with given featureId and reader, updater,
        /// and writer version
        /// </summary>
        /// <param name="featureId">feature identifier</param>
        /// <param name="writerVersion">Writer Version</param>
        /// <param name="readerVersion">Reader Version</param>
        /// <param name="updaterVersion">Updater Version</param>
        public FormatVersion(String featureId,
                                VersionPair writerVersion,
                                VersionPair readerVersion,
                                VersionPair updaterVersion)
        {
            if (featureId == null)
                throw new ArgumentNullException(nameof(featureId));
            if (writerVersion == null)
                throw new ArgumentNullException(nameof(writerVersion));
            if (readerVersion == null)
                throw new ArgumentNullException(nameof(readerVersion));
            if (updaterVersion == null)
                throw new ArgumentNullException(nameof(updaterVersion));

            if (featureId.Length == 0)
            {
                throw new ArgumentException(SR.ZeroLengthFeatureID);
            }

            _featureIdentifier = featureId;
            _reader = readerVersion;
            _updater = updaterVersion;
            _writer = writerVersion;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

#if !PBTCOMPILER
        /// <summary>
        /// reader version
        /// </summary>
        public VersionPair ReaderVersion
        {
            get
            {
                return _reader;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _reader = value;
            }
        }

        /// <summary>
        /// writer version
        /// </summary>
        public VersionPair WriterVersion
        {
            get
            {
                return _writer;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _writer = value;
            }
        }

        /// <summary>
        /// updater version
        /// </summary>
        public VersionPair UpdaterVersion
        {
            get
            {
                return _updater;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _updater = value;
            }
        }

        /// <summary>
        /// feature identifier
        /// </summary>
        public String FeatureIdentifier
        {
            get
            {
                return _featureIdentifier;
            }
        }
#endif

        #endregion Public Properties

        #region Operators

#if false
        /// <summary>
        /// == comparison operator
        /// </summary>
        /// <param name="v1">version to be compared</param>
        /// <param name="v2">version to be compared</param>
        public static bool operator ==(FormatVersion v1, FormatVersion v2)
        {
            // We have to cast v1 and v2 to Object
            //  to ensure that the == operator on Ojbect class is used not the == operator on FormatVersion
            if ((Object) v1 == null || (Object) v2 == null)
            {
                return ((Object) v1 == null && (Object) v2 == null);
            }

            // Do comparison only if both v1 and v2 are not null
            return v1.Equals(v2);
        }

        /// <summary>
        /// != comparison operator
        /// </summary>
        /// <param name="v1">version to be compared</param>
        /// <param name="v2">version to be compared</param>
        public static bool operator !=(FormatVersion v1, FormatVersion v2)
        {
            return !(v1 == v2);
        }

        /// <summary>
        /// Eaual comparison operator
        /// </summary>
        /// <param name="obj">Object to compare</param>
        /// <returns>ture if the object is equal to this instance</returns>
        public override bool Equals(Object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(FormatVersion))
            {
                return false;
            }

            FormatVersion v = (FormatVersion) obj;

            if (!string.Equals(_featureIdentifier, v.FeatureIdentifier, StringComparison.OrdinalIgnoreCase)
                || _reader != v.ReaderVersion
                || _writer != v.WriterVersion
                || _updater != v.UpdaterVersion)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Hash code
        /// </summary>
        public override int GetHashCode()
        {

            int hash = _reader.Major & HashMask;

            hash <<= 5;
            hash |= (_reader.Minor & HashMask);
            hash <<= 5;
            hash |= (_updater.Major & HashMask);
            hash <<= 5;
            hash |= (_updater.Minor & HashMask);
            hash <<= 5;
            hash |= (_writer.Major & HashMask);
            hash <<= 5;
            hash |= (_writer.Minor & HashMask);

            return hash;
        }
#endif

        #endregion Operators

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------        

        #region Public Methods

#if !PBTCOMPILER
        /// <summary>
        /// Constructor for FormatVersion with information read from the given stream
        /// </summary>
        /// <param name="stream">Stream where version information is read from</param>
        /// <remarks>Before this function call the current position of stream should be
        /// pointing to the begining of the version structure. After this call the current
        /// poisition will be pointing immediately after the version structure</remarks>
        public static FormatVersion LoadFromStream(Stream stream)
        {
            int bytesRead;

            return LoadFromStream(stream, out bytesRead);
        }
#endif

        /// <summary>
        /// Persist format version to the given stream
        /// </summary>
        /// <param name="stream">the stream to be written</param>
        /// <remarks>
        /// This operation will change the stream pointer
        /// stream can be null and will still return the number of bytes to be written
        /// </remarks>
        public int SaveToStream(Stream stream)
        {
            checked
            {
                // Suppress 56518 Local IDisposable object not disposed: 
                // Reason: The stream is not owned by the BlockManager, therefore we can 
                // close the BinaryWriter as it will Close the stream underneath.        
                int len = 0;
                BinaryWriter binarywriter = null;

                if (stream != null)
                {
                    binarywriter = new BinaryWriter(stream, System.Text.Encoding.Unicode);
                }

                // ************
                //  feature ID
                // ************

                len += ContainerUtilities.WriteByteLengthPrefixedDWordPaddedUnicodeString(binarywriter, _featureIdentifier);

                // ****************
                //  Reader Version
                // ****************

                if (stream != null)
                {
                    binarywriter.Write(_reader.Major);   // Major number
                    binarywriter.Write(_reader.Minor);   // Minor number
                }

                len += ContainerUtilities.Int16Size;
                len += ContainerUtilities.Int16Size;

                // *****************
                //  Updater Version
                // *****************

                if (stream != null)
                {
                    binarywriter.Write(_updater.Major);   // Major number
                    binarywriter.Write(_updater.Minor);   // Minor number
                }

                len += ContainerUtilities.Int16Size;
                len += ContainerUtilities.Int16Size;

                // ****************
                //  Writer Version
                // ****************

                if (stream != null)
                {
                    binarywriter.Write(_writer.Major);   // Major number
                    binarywriter.Write(_writer.Minor);   // Minor number
                }

                len += ContainerUtilities.Int16Size;
                len += ContainerUtilities.Int16Size;

                return len;
            }
        }

#if !PBTCOMPILER
        /// <summary>
        /// Check if a component with the given version can read this format version safely
        /// </summary>
        /// <param name="version">version of a component</param>
        /// <returns>true if this format version can be read safely by the component
        /// with the given version, otherwise false</returns>
        /// <remarks>
        /// The given version is checked against ReaderVersion
        /// </remarks>
        public bool IsReadableBy(VersionPair version)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            return (_reader <= version);
        }

        /// <summary>
        /// Check if a component with the given version can update this format version safely
        /// </summary>
        /// <param name="version">version of a component</param>
        /// <returns>true if this format version can be updated safely by the component
        /// with the given version, otherwise false</returns>
        /// <remarks>
        /// The given version is checked against UpdaterVersion
        /// </remarks>
        public bool IsUpdatableBy(VersionPair version)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            return (_updater <= version);
        }
#endif

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------
        // None
        //------------------------------------------------------
        //
        //  Internal Constructors
        //
        //------------------------------------------------------
        // None       
        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        // None       
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
#if !PBTCOMPILER
        /// <summary>
        /// Constructor for FormatVersion with information read from the given BinaryReader
        /// </summary>
        /// <param name="reader">BinaryReader where version information is read from</param>
        /// <param name="bytesRead">number of bytes read including padding</param>
        /// <returns>FormatVersion object</returns>
        /// <remarks>
        /// This operation will change the stream pointer. This function is preferred over the 
        /// LoadFromStream as it doesn't leave around Undisposed BinaryReader, which 
        /// LoadFromStream will
        /// </remarks>
        private static FormatVersion LoadFromBinaryReader(BinaryReader reader, out Int32 bytesRead)
        {
            checked
            {
                if (reader == null)
                {
                    throw new ArgumentNullException(nameof(reader));
                }

                FormatVersion ver = new FormatVersion();

                bytesRead = 0;            // Initialize the number of bytes read

                // **************
                //  feature ID
                // **************

                Int32 strBytes;

                ver._featureIdentifier = ContainerUtilities.ReadByteLengthPrefixedDWordPaddedUnicodeString(reader, out strBytes);
                bytesRead += strBytes;

                Int16 major;
                Int16 minor;

                // ****************
                //  Reader Version
                // ****************

                major = reader.ReadInt16();   // Major number
                bytesRead += ContainerUtilities.Int16Size;
                minor = reader.ReadInt16();   // Minor number
                bytesRead += ContainerUtilities.Int16Size;

                ver.ReaderVersion = new VersionPair(major, minor);

                // *****************
                //  Updater Version
                // *****************

                major = reader.ReadInt16();   // Major number
                bytesRead += ContainerUtilities.Int16Size;
                minor = reader.ReadInt16();   // Minor number
                bytesRead += ContainerUtilities.Int16Size;

                ver.UpdaterVersion = new VersionPair(major, minor);

                // ****************
                //  Writer Version
                // ****************

                major = reader.ReadInt16();   // Major number
                bytesRead += ContainerUtilities.Int16Size;
                minor = reader.ReadInt16();   // Minor number
                bytesRead += ContainerUtilities.Int16Size;

                ver.WriterVersion = new VersionPair(major, minor);

                return ver;
            }
        }

        /// <summary>
        /// Create FormatVersion object and read version information from the given stream
        /// </summary>
        /// <param name="stream">the stream to read version information from</param>
        /// <param name="bytesRead">number of bytes read including padding</param>
        /// <returns>FormatVersion object</returns>
        /// <remarks>
        /// This operation will change the stream pointer. This function shouldn't be 
        /// used in the scenarios when LoadFromBinaryReader can do the job. 
        /// LoadFromBinaryReader will not leave around any undisposed objects, 
        /// and LoadFromStream will. 
        /// </remarks>
        internal static FormatVersion LoadFromStream(Stream stream, out Int32 bytesRead)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            // Suppress 56518 Local IDisposable object not disposed: 
            // Reason: The stream is not owned by the BlockManager, therefore we can 
            // close the BinaryWriter as it will Close the stream underneath.
            BinaryReader streamReader = new BinaryReader(stream, System.Text.Encoding.Unicode);

            return LoadFromBinaryReader(streamReader, out bytesRead);
        }
#endif

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------
        // None
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        // None
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Member Variables

        private VersionPair _reader;
        private VersionPair _updater;
        private VersionPair _writer;

        private String _featureIdentifier;

#if false
        static private readonly int HashMask = 0x1f;
#endif

        #endregion Member Variables
    }
}
