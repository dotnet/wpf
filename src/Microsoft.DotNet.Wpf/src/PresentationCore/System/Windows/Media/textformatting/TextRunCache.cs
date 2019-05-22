// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Cache of text and text properties of run
//
//  Spec:      Text Formatting API.doc
//
//


using System;
using System.Collections.Generic;

using MS.Internal.PresentationCore;
using MS.Internal.TextFormatting;

namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// TextFormatter caches runs it receives from GetTextRun callback. This cache 
    /// object is managed by text layout client. 
    /// 
    /// This method is used to improve performance in application whose fetching the 
    /// run has significant performance implication. Application using this caching 
    /// mechanism is responsible for invalidating the content in the cache when 
    /// its changed.
    /// </summary>
    public sealed class TextRunCache
    {
        /// <summary>
        /// Constructing text run cache
        /// </summary>
        public TextRunCache() {}



        /// <summary>
        /// Client to notify change in part of the cache when text or 
        /// properties of the run is being added, removed or replaced.
        /// </summary>
        /// <param name="textSourceCharacterIndex">text source character index to specify where in the source text the change starts.</param>
        /// <param name="addition">the number of text source characters to be added in the source text</param>
        /// <param name="removal">the number of text source characters to be removed in the source text</param>
        public void Change(
            int     textSourceCharacterIndex,
            int     addition,
            int     removal
            )
        {
            if(_imp != null)
            {
                _imp.Change(
                    textSourceCharacterIndex,
                    addition,
                    removal
                    );
            }
        }



        /// <summary>
        /// Client to invalidate the whole cache, in effect emptying the cache and
        /// cause the cache refill in subsequent call to Text Formatting API.
        /// </summary>
        public void Invalidate()
        {
            if(_imp != null)
            {
                _imp = null;
            }
        }


        /// <summary>
        /// Return all cached TextRun in a TextSpan list. If TextRun is not cached for a particular character range, 
        /// the TextSpan would contain null TextRun object.
        /// </summary>
#if OPTIMALBREAK_API
        public IList<TextSpan<TextRun>> GetTextRunSpans()
#else
        [FriendAccessAllowed]
        internal IList<TextSpan<TextRun>> GetTextRunSpans()
#endif               
        {
            if (_imp != null)
            {                
                return _imp.GetTextRunSpans();
            }

            // otherwise, return an empty collection
            return new TextSpan<TextRun>[0];
        }


        /// <summary>
        /// Get/set the actual cache instance
        /// </summary>
        internal TextRunCacheImp Imp
        {
            get { return _imp; }
            set { _imp = value; }
        }

        private TextRunCacheImp   _imp;
    }
}
