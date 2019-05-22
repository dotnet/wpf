// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//   Implementation of the CompoundFileReference base class.
//

// Notes:
//  Persistence of specific classes is mostly hard-coded in this base class because
//  the persistence must follow a shared binary implementation with Office.  It is
//  also intentionally not polymorphic because we don't allow arbitrary subclasses
//  to participate.

using System;
using System.Collections.Specialized;       // for StringCollection class
using System.IO;
using System.Diagnostics;                   // for Debug.Assert
using System.Windows;                       // for SR error message lookup

using MS.Internal.WindowsBase;

namespace MS.Internal.IO.Packaging.CompoundFile
{
    /// <summary>
    /// Logical reference to a portion of a container
    /// </summary>
    /// <remarks>
    /// Use this class to represent a logical reference to a portion of a container such as a stream
    /// Note that a CompoundFileReference is not natively tied to any specific container.  This lack of context allows
    /// the developer freedom to create the reference in the absence of the container, or to have the reference
    /// refer to any one of multiple containers having a similar format.
    /// </remarks>
    internal abstract class CompoundFileReference: IComparable
    {
        #region Enums
        /// <summary>
        /// Reference component types
        /// </summary>
        /// <remarks>
        /// These are only used for serialization
        /// </remarks>
        private enum RefComponentType : int
        { 
            /// <summary>
            /// Stream component
            /// </summary>
            Stream = 0,
            /// <summary>
            /// Storage component
            /// </summary>
            Storage = 1,
        };
        #endregion

        #region Abstracts

        /// <summary>
        /// Full name of the stream or storage this reference refers to (see StreamInfo and StorageInfo)
        /// </summary>
        abstract public string FullName {get;}

        #endregion

        #region IComparable
        /// <summary>
        /// This is not implemented - it exists as a reminder to authors of subclasses that they must implement this interface
        /// </summary>
        /// <param name="ob">ignored</param>
        int IComparable.CompareTo(object ob)
        {
            // this must be implemented by our inheritors
            Debug.Assert(false, "subclasses must override this method");
            return 0;
        }
        #endregion

        #region Operators
        /// <summary>Compare for equality</summary>
        /// <param name="o">the CompoundFileReference to compare to</param>
        public override bool Equals(object o)
        {
            // this must be implemented by our inheritors
            Debug.Assert(false, "subclasses must override this method");
            return false;
        }

        /// <summary>Returns an integer suitable for including this object in a hash table</summary>
        public override int GetHashCode()
        {
            // this must be implemented by our inheritors
            Debug.Assert(false, "subclasses must override this method");
            return 0;
        }
        #endregion

        #region Persistence

        /// <summary>Save to a stream</summary>
        /// <param name="reference">reference to save</param>
        /// <param name="writer">The BinaryWriter to persist this object to.  
        /// This method will alter the stream pointer of the underlying stream as a side effect.
        /// Passing null simply calculates how many bytes would be written.</param>
        /// <returns>number of bytes written including any padding</returns>
        static internal int Save(CompoundFileReference reference, BinaryWriter writer)
        {
            int bytes = 0;

            // NOTE: Our RefComponentType must be written by our caller
            bool calcOnly = (writer == null);

            // what are we dealing with here?
            CompoundFileStreamReference streamReference = reference as CompoundFileStreamReference;
            if ((streamReference == null) && (!(reference is CompoundFileStorageReference)))
                throw new ArgumentException(SR.Get(SRID.UnknownReferenceSerialize), "reference");

            // first parse the path into strings
            string[] segments = ContainerUtilities.ConvertBackSlashPathToStringArrayPath(reference.FullName);
            int entries = segments.Length;

            // write the count
            if (!calcOnly)
                writer.Write( entries );

            bytes += ContainerUtilities.Int32Size;

            // write the segments - if we are dealing with a stream entry, don't write the last "segment"
            // because it is in fact a stream name
            for (int i = 0; i < segments.Length - (streamReference == null ? 0 : 1); i++)
            {
                if (!calcOnly)
                {
                    writer.Write( (Int32)RefComponentType.Storage );
                }
                bytes += ContainerUtilities.Int32Size;
                bytes += ContainerUtilities.WriteByteLengthPrefixedDWordPaddedUnicodeString(writer, segments[i]);
            }

            if (streamReference != null)
            {
                // we are responsible for the prefix
                if (!calcOnly)
                {
                    writer.Write( (Int32)RefComponentType.Stream );
                }
                bytes += ContainerUtilities.Int32Size;

                // write the stream name
                bytes += ContainerUtilities.WriteByteLengthPrefixedDWordPaddedUnicodeString(writer, segments[segments.Length - 1]);
            }

            return bytes;
        }

        /// <summary>
        /// Deserialize from the given stream
        /// </summary>
        /// <param name="reader">the BinaryReader to deserialize from with the seek pointer at the beginning of the container reference</param>
        /// <param name="bytesRead">bytes consumed from the stream</param>
        /// <remarks>
        /// Side effect of change the stream pointer
        /// </remarks>
        /// <exception cref="FileFormatException">Throws a FileFormatException if any formatting errors are encountered</exception>
        internal static CompoundFileReference Load(BinaryReader reader, out int bytesRead)
        {
            ContainerUtilities.CheckAgainstNull( reader, "reader" );

            bytesRead = 0;  // running count of how much we've read - sanity check

            // create the TypeMap
            // reconstitute ourselves from the given BinaryReader
            
            // in this version, the next Int32 is the number of entries
            Int32 entryCount = reader.ReadInt32();
            bytesRead += ContainerUtilities.Int32Size;
            // EntryCount of zero indicates the root storage.
            if (entryCount < 0)
                throw new FileFormatException(
                    SR.Get(SRID.CFRCorrupt));

            // need a temp collection because we don't know what we're dealing with until a non-storage component
            // type is encountered
            StringCollection storageList = null;
            String streamName = null;

            // loop through the entries - accumulating strings until we know what kind of object
            // we ultimately need
            int byteLength;     // reusable
            while (entryCount > 0)
            {
                // first Int32 tells us what kind of component this entry represents
                RefComponentType refType = (RefComponentType)reader.ReadInt32();
                bytesRead += ContainerUtilities.Int32Size;

                switch (refType)
                {
                    case RefComponentType.Storage:
                    {
                        if (streamName != null)
                            throw new FileFormatException(
                                SR.Get(SRID.CFRCorruptStgFollowStm));

                        if (storageList == null)
                            storageList = new StringCollection();

                        String str = ContainerUtilities.ReadByteLengthPrefixedDWordPaddedUnicodeString(reader, out byteLength);
                        bytesRead += byteLength;
                        storageList.Add(str);
} break;
                    case RefComponentType.Stream:
                    {
                        if (streamName != null)
                            throw new FileFormatException(
                                SR.Get(SRID.CFRCorruptMultiStream));

                        streamName = ContainerUtilities.ReadByteLengthPrefixedDWordPaddedUnicodeString(reader, out byteLength);
                        bytesRead += byteLength;
                    } break;

                    // we don't handle these types yet
                    default:
                        throw new FileFormatException(
                            SR.Get(SRID.UnknownReferenceComponentType));
                }

                --entryCount;
            }

            CompoundFileReference newRef = null;

            // stream or storage?
            if (streamName == null)
            {
                newRef = new CompoundFileStorageReference(
                    ContainerUtilities.ConvertStringArrayPathToBackSlashPath(storageList));
            }
            else
                newRef = new CompoundFileStreamReference(
                    ContainerUtilities.ConvertStringArrayPathToBackSlashPath(storageList, streamName));

            return newRef;
        }
        #endregion
    }
}
