// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.IO;
using System.Windows.Ink;
using MS.Internal.IO.Packaging;

namespace MS.Internal.Ink.InkSerializedFormat
{
    /// <summary>
    /// Summary description for GuidTagList.
    /// </summary>
    internal class GuidList
    {
        private readonly System.Collections.Generic.List<Guid> _customGuids = new System.Collections.Generic.List<Guid>();


        public GuidList()
        {
        }


        /// <summary>
        /// Adds a guid to the list of Custom Guids if it is not a known guid and is already not
        /// in the list of Custom Guids
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public bool Add(Guid guid)
        {
            // If the guid is not found in the known guids list nor in the custom guid list,
            // add that to the custom guid list
            if (0 == FindTag(guid, true))
            {
                _customGuids.Add(guid);
                return true;
            }
            else
                return false;
        }


        /// <summary>
        /// Finds the tag for a Guid based on the list of Known Guids
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static KnownTagCache.KnownTagIndex FindKnownTag(Guid guid)
        {
            // Find out if the guid is in the known guid table
            for (byte iIndex = 0; iIndex < KnownIdCache.OriginalISFIdTable.Length; ++iIndex)
                if (guid == KnownIdCache.OriginalISFIdTable[iIndex])
                    return KnownIdCache.KnownGuidBaseIndex + iIndex;

            // Couldnt find in the known list
            return 0;
        }


        /// <summary>
        /// Finds the tag for a guid based on the list of Custom Guids
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        KnownTagCache.KnownTagIndex FindCustomTag(Guid guid)
        {
            int i;

            for (i = 0; i < _customGuids.Count; i++)
            {
                if (guid.Equals(_customGuids[i]))
                    return (KnownTagCache.KnownTagIndex)(KnownIdCache.CustomGuidBaseIndex + i);
            }

            return KnownTagCache.KnownTagIndex.Unknown;
        }


        /// <summary>
        /// Finds a tag corresponding to a guids
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="bFindInKnownListFirst"></param>
        /// <returns></returns>
        public KnownTagCache.KnownTagIndex FindTag(Guid guid, bool bFindInKnownListFirst)
        {
            KnownTagCache.KnownTagIndex tag = KnownTagCache.KnownTagIndex.Unknown;

            if (bFindInKnownListFirst)
            {
                tag = FindKnownTag(guid);
                if (KnownTagCache.KnownTagIndex.Unknown == tag)
                    tag = FindCustomTag(guid);
            }
            else
            {
                tag = FindCustomTag(guid);
                if (KnownTagCache.KnownTagIndex.Unknown == tag)
                    tag = FindKnownTag(guid);
            }

            return tag;
        }


        /// <summary>
        /// Finds a known guid based on a Tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        static Guid FindKnownGuid(KnownTagCache.KnownTagIndex tag)
        {
            if (tag < KnownIdCache.KnownGuidBaseIndex)
            {
                throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("Tag is outside of the known guid tag range"));
            }

            // Get the index in the OriginalISFIdTable array first
            uint nIndex = (uint)(tag - KnownIdCache.KnownGuidBaseIndex);

            // If invalid, return Guid.Empty
            if (KnownIdCache.OriginalISFIdTable.Length <= nIndex)
                return Guid.Empty;

            // Otherwise, return the guid
            return KnownIdCache.OriginalISFIdTable[nIndex];
        }


        /// <summary>
        /// Finds a Custom Guid based on a Tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        Guid FindCustomGuid(KnownTagCache.KnownTagIndex tag)
        {
            if ((int)tag < (int)KnownIdCache.CustomGuidBaseIndex)
            {
                throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("Tag is outside of the known guid tag range"));
            }

            // Get the index in the OriginalISFIdTable array first
            int nIndex = (int)(tag - KnownIdCache.CustomGuidBaseIndex);

            // If invalid, return Guid.Empty
            if ((0 > nIndex) || (_customGuids.Count <= nIndex))
                return Guid.Empty;

            // Otherwise, return the guid
            return (Guid)_customGuids[(int)nIndex];
        }


        /// <summary>
        /// Finds a guid based on Tag
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public Guid FindGuid(KnownTagCache.KnownTagIndex tag)
        {
            if (tag < (KnownTagCache.KnownTagIndex)KnownIdCache.CustomGuidBaseIndex)
            {
                Guid guid = FindKnownGuid(tag);

                if (Guid.Empty != guid)
                    return guid;

                return FindCustomGuid(tag);
            }
            else
            {
                Guid guid = FindCustomGuid(tag);

                if (Guid.Empty != guid)
                    return guid;

                return FindKnownGuid(tag);
            }
        }


        /// <summary>
        /// Returns the expected size of data if it is a known guid
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public static uint GetDataSizeIfKnownGuid(Guid guid)
        {
            for (uint i = 0; i < KnownIdCache.OriginalISFIdTable.Length; ++i)
            {
                if (guid == KnownIdCache.OriginalISFIdTable[i])
                {
                    return KnownIdCache.OriginalISFIdPersistenceSize[i];
                }
            }

            return 0;
        }

        /// <summary>
        /// Serializes the GuidList in the memory stream and returns the size
        /// </summary>
        /// <param name="stream">If null, calculates the size only</param>
        /// <returns></returns>
        public uint Save(Stream stream)
        {
                // calculate the number of custom guids to persist
                //   custom guids are those which are not reserved in ISF via 'tags'
            uint ul = (uint)(_customGuids.Count * Native.SizeOfGuid);

                // if there are no custom guids, then the guid list can be persisted
                //      without any cost ('tags' are freely storeable)
            if (ul == 0)
            {
                return 0;
            }
            
                // if only the size was requested, return it
            if (null == stream)
            {
                return (uint)(ul + SerializationHelper.VarSize(ul) + SerializationHelper.VarSize((uint)KnownTagCache.KnownTagIndex.GuidTable));
            }

                // encode the guid table tag in the output stream
            uint cbWrote = SerializationHelper.Encode(stream, (uint)KnownTagCache.KnownTagIndex.GuidTable);

                // encode the size of the guid table
            cbWrote += SerializationHelper.Encode(stream, ul);

                // encode each guid in the table
            for (int i = 0; i < _customGuids.Count; i++)
            {
                Guid guid = (Guid)_customGuids[i];

                stream.Write(guid.ToByteArray(), 0, (int)Native.SizeOfGuid);
            }

            cbWrote += ul;
            return cbWrote;
        }


        /// <summary>
        /// Deserializes the GuidList from the memory stream
        /// </summary>
        /// <param name="strm"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public uint Load(Stream strm, uint size)
        {
            uint cbsize = 0;

            _customGuids.Clear();

            uint count = size / Native.SizeOfGuid;
            byte[] guids = new byte[Native.SizeOfGuid];

            for (uint i = 0; i < count; i++)
            {
                // Stream.Read could read less number of bytes than the request. We call ReliableRead that
                // reads the bytes in a loop until all requested bytes are received or reach the end of the stream.
                uint bytesRead = StrokeCollectionSerializer.ReliableRead(strm, guids, Native.SizeOfGuid);

                cbsize += bytesRead;
                if ( bytesRead == Native.SizeOfGuid )
                {
                    _customGuids.Add(new Guid(guids));
                }
                else
                {
                    // If Stream.Read cannot return the expected number of bytes, we should break here.
                    // The caller - StrokeCollectionSerializer.DecodeRawISF will check our return value. 
                    // An exception might be thrown if reading is failed.
                    break;
                }
            }

            return cbsize;
        }
    }
}
