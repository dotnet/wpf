// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//              Object returned by the NextContentUnit method of XamlFilter.
//              An IndexingContentUnit contains a chunk and its contents.
//


using System;
using MS.Internal.Interop;  // for CHUNK_BREAKTYPE

namespace MS.Internal.IO.Packaging
{
    ///<summary>A descriptor for a chunk, as returned by XamlFilter.NextContentUnit.</summary>
    internal class IndexingContentUnit : ManagedChunk
    {
        ///<summary>Build a contents chunk, passing the contents string and specifying whether it comes from a Glyphs element.</summary>
        ///<param name="contents">The value of the chunk's contents property.</param>
        ///<param name="chunkID">An arbitrary Uint32 to identify each chunk returned by IFilter.GetChunk.</param>
        ///<param name="breakType">The opening break for the chunk.</param>
        ///<param name="attribute">A description of the property represented by the chunk.</param>
        ///<param name="lcid">The locale ID for the chunk.</param>
        internal IndexingContentUnit(
            string contents,
            uint chunkID,
            CHUNK_BREAKTYPE breakType, 
            ManagedFullPropSpec attribute, 
            uint lcid)
            : base(chunkID, breakType, attribute, lcid, CHUNKSTATE.CHUNK_TEXT)
        {
            _contents = contents;
        }

        /// <summary>
        /// A utility to be used when one wants to reuse
        /// one object to hold different values in succession.
        /// </summary>
        internal void InitIndexingContentUnit(
            string contents,
            uint chunkID,
            CHUNK_BREAKTYPE breakType, 
            ManagedFullPropSpec attribute, 
            uint lcid)
        {
            _contents = contents;
            ID = chunkID;
            BreakType = breakType;
            Attribute = attribute;
            Locale = lcid;
        }

        ///<summary>The chunk's contents.</summary>
        internal string Text 
        { 
            get
            {
                return _contents;
            }
        } 

        private string          _contents;
    }
}
