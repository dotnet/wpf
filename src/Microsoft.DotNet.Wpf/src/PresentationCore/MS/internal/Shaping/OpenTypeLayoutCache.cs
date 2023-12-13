// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: The OpenTypeLayoutCache class is used by OpenType layout services
//              for performance optimizations. It is responsible for creating cache 
//              structures as well as runtime support during time of actual layout calls.
// 
//

using System;
using System.Security;
using System.IO;
using System.Diagnostics;
using System.Collections;

using MS.Internal;
using MS.Internal.FontCache;

namespace MS.Internal.Shaping
{
    // Cache is stroing for each glyph list of lookups where this glyph participate as
    // primary (first in rule sequence), and so can be substituted or positioned. 
    // At runtime, main loop will attempt to apply lookups only to places where there
    // is a potential for substitution. 
    // 
    // Cache is of following structure. It contains a list of all glyphs that are affected 
    // and each glyph has a list of lookups associated with it. Each list is terminated by 0xffff 
    // value for glyph index.
    //
    // Glyph1 -> Lookup1.1, Lookup1.2, ..., 0xffff
    // ...
    // GlyphN -> LookupN.1, LookupN.2, ... 0xfffff
    //
    // Binary structure consists of a header, list of glyphs and offsets and area where actual 
    // lookup lists reside. Offsets to lookup lists are relative to the start of the table:
    // 
    // USHORT totalCacheSize           // total size of table cache
    // USHORT 0xFFFF                   // glyphs not present assumed pointing to this empty list
    // USHORT lookupCount              // number of lookups fit into cache
    // USHORT glyphCount               // number of glyph records in the cache
    // (                               // array of glyph records, each has 
    //     USHORT glyphId,             //     glyph id and
    //     USHORT lookupListOffset     //     offset to lookup list, from the cache start
    // ) [glyphCount]                  //
    // USHORT[] lookupLists            // Here is the area where lookup lists reside. Each list 
    //                                 // is in ascending order, terminataed by 0xffff. Several glyphs
    //                                 // may point to the same list, for saving space.
    //
    // Cache buildig code in CreateTableCache does simple size optimiation, comparing two
    // consecutive glyphs if they have the same list of glyphs and point both to the same 
    // physical list. If all lookups can not fit into cache, cache will remember how many 
    // actually fit and simply assume that the rest of lookups applicable to all glyphs.
    //
    // During runtime, OTLS supports array of pointers to cache lookup lists, parallel to 
    // array of glyphs. When processing lookup 'i', all pointers a being moved furhter through 
    // the list to point to lookup that is >= i. Simple loop through pointers (FindNextLookup)
    // allows to find next lookup index to be processed. Then in FindNextGlyphInLookup, going 
    // through pointers allows to find next glyph that should be tried for this lookup.
    //
    // Since every list is terminated by 0xffff, loop in FindNextLookup:
    //          while(*cachePointers[i] < firstLookupIndex) cachePointers[i]++;
    // will never overrun the cache, and so does not require special check or additional data
    // to indicate end of the list.
    //
    /// <summary>
    /// Implements OpenType layout services cache logic at both caching and using time
    /// </summary>
    internal static class OpenTypeLayoutCache
    {
        public static void InitCache(
                                IOpenTypeFont   font,
                                OpenTypeTags    tableTag,
                                GlyphInfoList   glyphInfo,
                                OpenTypeLayoutWorkspace workspace
                            )
        {
            Debug.Assert(tableTag == OpenTypeTags.GSUB || tableTag == OpenTypeTags.GPOS);
            
            byte[] cacheArray = font.GetTableCache(tableTag);

            unsafe
            {
                if (cacheArray == null)
                {
                    workspace.TableCacheData = null;
                }
                else
                {
                    workspace.TableCacheData = cacheArray;

                    workspace.AllocateCachePointers(glyphInfo.Length);
                    RenewPointers(glyphInfo, workspace, 0, glyphInfo.Length);
                }
            }
        }
        
        public static void OnGlyphsChanged(
                                            OpenTypeLayoutWorkspace workspace,
                                            GlyphInfoList           glyphInfo,
                                            int                     oldLength,
                                            int                     firstGlyphChanged,
                                            int                     afterLastGlyphChanged
                                          )
        {
            unsafe
            {
                if (workspace.TableCacheData == null)
                {
                    return;
                }
            }
            
            workspace.UpdateCachePointers(oldLength, glyphInfo.Length, firstGlyphChanged, afterLastGlyphChanged);
            RenewPointers(glyphInfo, workspace, firstGlyphChanged, afterLastGlyphChanged);
        }

        /// <summary>
        /// Gets number of lookups that fit into table cache
        /// </summary>
        /// <param name="workspace">In: Storage for buffers we need</param>
        /// <returns>Number of lookups in cache</returns>
        private static unsafe ushort GetCacheLookupCount(OpenTypeLayoutWorkspace workspace)
        {
            // If there is no chache, just exit
            if (workspace.TableCacheData == null)
            {
                return 0;
            }
            fixed (byte* pCacheByte = &workspace.TableCacheData[0])
            {
                ushort* pCache = (ushort*)pCacheByte;

                return pCache[2];
            }
        }
        
        /// <summary>
        /// Find next glyph in lookup. Depending on search direction, 
        /// it will update either firstGlyph or afterLastGlyph
        /// </summary>
        /// <param name="workspace">In: Storage for buffers we need</param>
        /// <param name="glyphInfo">In: Glyph run</param>
        /// <param name="firstLookupIndex">In: Minimal lookup index to search for.</param>
        /// <param name="lookupIndex">Out: Lookup index found</param>
        /// <param name="firstGlyph">Out: First applicable glyph for this lookup</param>
        /// <returns>True if any lookup found, false otherwise</returns>
        public static unsafe void FindNextLookup(
                                    OpenTypeLayoutWorkspace workspace,
                                    GlyphInfoList glyphInfo,
                                    ushort     firstLookupIndex,
                                    out ushort lookupIndex,
                                    out int    firstGlyph
                                  )
        {
            if (firstLookupIndex >= GetCacheLookupCount(workspace))
            {
                // For lookups that did not fit into cache, just say we should always try it
                lookupIndex = firstLookupIndex;
                firstGlyph  = 0;
                return;
            }
        
            ushort[] cachePointers = workspace.CachePointers;
            int glyphCount = glyphInfo.Length;
            
            lookupIndex = 0xffff;
            firstGlyph   = 0;
            
            for(int i = 0; i < glyphCount; i++)
            {
                // Sync up inside the list up to the minimal lookup requested
                // No additional boundary checks are necessary, because every list terminates with 0xffff
                while(cachePointers[i] < firstLookupIndex) cachePointers[i]++;
                //Now we know that our index is higher or equal than firstLookup index
                
                if (cachePointers[i] < lookupIndex)
                {
                    // We now have new minimum
                    lookupIndex = cachePointers[i];
                    firstGlyph  = i;
                }
            }

            if (lookupIndex == 0xffff)
            {
                // We can't just say we are done, there may be lookups that did not fit into cache
                lookupIndex = GetCacheLookupCount(workspace);
                firstGlyph  = 0;
            }
        }

        /// <summary>
        /// Find next glyph in lookup. Depending on search direction, 
        /// it will update either firstGlyph or afterLastGlyph
        /// </summary>
        /// <param name="workspace">Storage for buffers we need</param>
        /// <param name="lookupIndex">Current lookup in processing</param>
        /// <param name="isLookupReversal">Do we go forward or backwards</param>
        /// <param name="firstGlyph">first glyph of search range</param>
        /// <param name="afterLastGlyph">position after last glyph</param>
        /// <returns>True if any glyph found, false otherwise</returns>
        public static unsafe bool FindNextGlyphInLookup(
                                    OpenTypeLayoutWorkspace workspace,
                                    ushort          lookupIndex,
                                    bool            isLookupReversal,
                                    ref int         firstGlyph,
                                    ref int         afterLastGlyph
                                )
        {
            if (lookupIndex >= GetCacheLookupCount(workspace))
            {
                return true;
            }

            ushort[] cachePointers = workspace.CachePointers;

            if (!isLookupReversal)
            {
                for (int i = firstGlyph; i < afterLastGlyph; i++)
                {
                    if (cachePointers[i] == lookupIndex)
                    {
                        firstGlyph = i;
                        return true;
                    }
                }

                return false;
            }            
            else
            {
                for(int i = afterLastGlyph - 1; i >= firstGlyph; i--)
                {
                    if (cachePointers[i] == lookupIndex)
                    {
                        afterLastGlyph = i + 1;
                        return true;
                    }
                }

                return false;
            } 
        }

        private static unsafe void RenewPointers(
                                            GlyphInfoList glyphInfo, 
                                            OpenTypeLayoutWorkspace workspace, 
                                            int firstGlyph, 
                                            int afterLastGlyph
                                         )
        {
            fixed (byte* pCache = &workspace.TableCacheData[0])
            {
                // If there is no chache, just exit
                if (pCache == null)
                {
                    return;
                }

                ushort[] cachePointers = workspace.CachePointers;

                for (int i = firstGlyph; i < afterLastGlyph; i++)
                {
                    ushort glyph = glyphInfo.Glyphs[i];

                    // If glyph is not there, we will point to the constant 0xFFFF in the cache
                    int listOffset = 2;

                    //Find glyph entry in the cache
                    int glyphCount = *((ushort*)pCache + 3);
                    ushort* pGlyphs = (ushort*)pCache + 4;
                    int low = 0, high = glyphCount;

                    while (low < high)
                    {
                        int mid = (low + high) >> 1;
                        ushort midGlyph = pGlyphs[mid * 2];

                        if (glyph < midGlyph)
                        {
                            high = mid;
                            continue;
                        }
                        if (glyph > midGlyph)
                        {
                            low = mid + 1;
                            continue;
                        }

                        // Found it!
                        listOffset = pGlyphs[mid * 2 + 1];
                        break;
                    }

                    // Whether we found glyph in the cache or not,
                    // Pointer will be set to the list, but it may be empty.
                    cachePointers[i] = *((ushort*)(pCache + listOffset));
                }
            }
        }
        
#region Cache filling

        internal static void CreateCache(IOpenTypeFont font, int maxCacheSize)
        {
            if (maxCacheSize > ushort.MaxValue)
            {
                // Data structures do not support cache sizes more than 64K.
                maxCacheSize = ushort.MaxValue;
            }

            int tableCacheSize;
            int totalSize = 0;

            CreateTableCache(font, OpenTypeTags.GSUB, maxCacheSize - totalSize, out tableCacheSize);
            totalSize += tableCacheSize;
            Debug.Assert(totalSize <= maxCacheSize);

            CreateTableCache(font, OpenTypeTags.GPOS, maxCacheSize - totalSize, out tableCacheSize);
            totalSize += tableCacheSize;
            Debug.Assert(totalSize <= maxCacheSize);
        }
        
        private static void CreateTableCache(IOpenTypeFont font, OpenTypeTags tableTag, int maxCacheSize, out int tableCacheSize)
        {
            // Initialize all computed values
            tableCacheSize = 0;
            int cacheSize = 0;
            int recordCount = 0;
            int glyphCount = 0;
            int lastLookupAdded = -1;
            GlyphLookupRecord[] records = null;

            try
            {
                ComputeTableCache(
                    font,
                    tableTag,
                    maxCacheSize,
                    ref cacheSize,
                    ref records,
                    ref recordCount,
                    ref glyphCount,
                    ref lastLookupAdded
                    );
            }
            catch (FileFormatException)
            {
                cacheSize = 0;
            }

            if (cacheSize > 0)
            {
                tableCacheSize = FillTableCache(
                    font,
                    tableTag,
                    cacheSize,
                    records,
                    recordCount,
                    glyphCount,
                    lastLookupAdded
                    );
            }
        }


        private static void ComputeTableCache(
            IOpenTypeFont           font, 
            OpenTypeTags            tableTag, 
            int                     maxCacheSize,
            ref int                 cacheSize,
            ref GlyphLookupRecord[] records,
            ref int                 recordCount,
            ref int                 glyphCount,
            ref int                 lastLookupAdded
            )
        {
            FontTable table = font.GetFontTable(tableTag);

            if (!table.IsPresent)
            {
                return;
            }
            
            FeatureList featureList;
            LookupList  lookupList;

            Debug.Assert(tableTag == OpenTypeTags.GSUB || tableTag == OpenTypeTags.GPOS);

            switch (tableTag)
            {
                case OpenTypeTags.GSUB:
                {
                    GSUBHeader header = new GSUBHeader();
                    featureList = header.GetFeatureList(table);
                    lookupList  = header.GetLookupList(table);
                    break;                    
                }    
                case OpenTypeTags.GPOS:
                {
                    GPOSHeader header = new GPOSHeader();
                    featureList = header.GetFeatureList(table);
                    lookupList = header.GetLookupList(table);
                    break;                    
                }
                default:
                {
                    Debug.Assert(false);
                    featureList = new FeatureList(0);
                    lookupList  = new LookupList(0);
                    break;
                }
            }
            
            // Estimate number of records that can fit into cache using ratio of approximately 
            // 4 bytes of cache per actual record. Most of fonts will fit into this value, except 
            // some tiny caches and big EA font that can have ratio of around 5 (theoretical maximum is 8).
            //
            // If actual ratio for particluar font will be larger than 4, we will remove records 
            // from the end to fit into cache.
            //
            // If ratio is less than 4 we actually can fit more lookups, but for the speed and because most fonts
            // will fit into cache completely anyway we do not do anything about this here.
            int maxRecordCount = maxCacheSize / 4;

            // For now, we will just allocate array of maximum size.
            // Given heuristics above, it wont be greater than max cache size.
            // Consider dynamic reallocation here.
            records = new GlyphLookupRecord[maxRecordCount];
            
            //
            // Now iterate through lookups and subtables, filling in lookup-glyph pairs list
            //
            int lookupCount     = lookupList.LookupCount(table);
            int recordCountAfterLastLookup = 0;

            //
            // Not all lookups can be invoked from feature directly,
            // they are actions from contextual lookups.
            // We are not interested in those, because they will
            // never work from high level, so do not bother adding them to the cache.
            //
            // Filling array of lookup usage bits, to skip those not mapped to any lookup
            //
            BitArray lookupUsage = new BitArray(lookupCount);
            
            for (ushort feature = 0; feature < featureList.FeatureCount(table); feature++)
            {
                FeatureTable featureTable = featureList.FeatureTable(table, feature);

                for (ushort lookup = 0; lookup < featureTable.LookupCount(table); lookup++)
                {
                    ushort lookupIndex = featureTable.LookupIndex(table, lookup);

                    if (lookupIndex >= lookupCount)
                    {
                        // This must be an invalid font. Just igonoring this lookup here.
                        continue;
                    }
                    
                    lookupUsage[lookupIndex] = true;
                }
            }
            // Done with lookup usage bits
            
            for(ushort lookupIndex = 0; lookupIndex < lookupCount; lookupIndex++)
            {
                if (!lookupUsage[lookupIndex])
                {
                    continue;
                }
                
                int firstLookupRecord   = recordCount;
                int maxLookupGlyph      = -1;
                bool cacheIsFull        = false;

                LookupTable lookup   = lookupList.Lookup(table, lookupIndex);
                ushort lookupType    = lookup.LookupType();
                ushort subtableCount = lookup.SubTableCount();
                
                for(ushort subtableIndex = 0; subtableIndex < subtableCount; subtableIndex++)
                {
                    int subtableOffset = lookup.SubtableOffset(table, subtableIndex);
                    
                    CoverageTable coverage = GetSubtablePrincipalCoverage(table, tableTag, lookupType, subtableOffset);
                    
                    if (coverage.IsInvalid) continue;
                    
                    cacheIsFull = !AppendCoverageGlyphRecords(table, lookupIndex, coverage, records, ref recordCount, ref maxLookupGlyph);
                    
                    if (cacheIsFull) break;
                }
                
                if (cacheIsFull) break;
                
                lastLookupAdded = lookupIndex;
                recordCountAfterLastLookup = recordCount;
            }
            
            // We may hit storage overflow in the middle of lookup. Throw this partial lookup away
            recordCount = recordCountAfterLastLookup;
            
            if (lastLookupAdded == -1)
            {
                // We did not succeed adding even single lookup.
                return;
            }
            
            // We now have glyph records for (may be not all) lookups in the table.
            // Cache structures should be sorted by glyph, then by lookup index.
            Array.Sort(records, 0, recordCount);
            
            cacheSize  = -1;
            glyphCount = -1;

            // It may happen, that records do not fit into cache, even using our heuristics. 
            // We will remove lookups one by one from the end until it fits.
            while (recordCount > 0)
            {
                CalculateCacheSize(records, recordCount, out cacheSize, out glyphCount);
            
                if (cacheSize <= maxCacheSize)
                {
                    // Fine, we now fit into max cache size
                    break;
                }
                else
                {
                    // Find last lookup index
                    int lastLookup = -1;
                    for(int i = 0; i < recordCount; i++)
                    {
                        int lookup = records[i].Lookup;

                        if (lastLookup < lookup) 
                        {
                            lastLookup = lookup;
                        }
                    }

                    Debug.Assert(lastLookup >= 0); // There are lookups, so there was an index

                    // Remove it
                    int currentRecord = 0;
                    for(int i = 0; i < recordCount; i++)
                    {
                        if (records[i].Lookup == lastLookup) continue;

                        if (currentRecord == i) continue;

                        records[currentRecord] = records[i];
                        currentRecord++;
                    }

                    recordCount = currentRecord;

                    // Do not forget update lastLookupAdded variable
                    lastLookupAdded = lastLookup - 1;
                }
            }

            if (recordCount == 0)
            {
                // We can't fit even single lookup into the cache
                return;
            }

            Debug.Assert(cacheSize  > 0); // We've calcucalted them at least ones, and 
            Debug.Assert(glyphCount > 0); // if there is no records, we already should've exited
        }


        private static int FillTableCache(
            IOpenTypeFont       font, 
            OpenTypeTags        tableTag, 
            int                 cacheSize,
            GlyphLookupRecord[] records,
            int                 recordCount,
            int                 glyphCount,
            int                 lastLookupAdded
            )
        {
            // Fill the cache.

            // We are using basically the same code to fill the cache 
            // that had been used to calculate the size. So pList pointer
            // moving through cache memory should not overrun allocated space.
            // Asserts are set to chek that at every place where we write to cache
            // and at the end where we check that we filled exactly the same amount.
            
            unsafe 
            {
                byte[] cache = font.AllocateTableCache(tableTag, cacheSize);
                if (cache == null)
                {
                    // We failed to allocate cache of requested size, 
                    // exit without created cache.
                    return 0;
                }

                fixed (byte* pCacheByte = &cache[0])
                {
                    ushort* pCache = (ushort*) pCacheByte;

                    pCache[0] = (ushort)cacheSize;              // Cache size
                    pCache[1] = 0xFFFF;                         // 0xFFFF constants
                    pCache[2] = (ushort)(lastLookupAdded + 1);  // Number of lookups that fit into the cache
                    pCache[3] = (ushort)glyphCount;             // Glyph count

                    ushort* pGlyphs = pCache + 4;
                    ushort* pList = pGlyphs + glyphCount * 2;
                    ushort* pPrevList = null;

                    int prevListIndex = -1, prevListLength = 0;
                    int curListIndex = 0, curListLength = 1;
                    ushort curGlyph = records[0].Glyph;

                    for (int i = 1; i < recordCount; i++)
                    {
                        if (records[i].Glyph != curGlyph)
                        {
                            // We've found another list. Compare it with previous
                            if (prevListLength != curListLength || // Fast check to avoid full comparison
                                !CompareGlyphRecordLists(records,
                                                         recordCount,
                                                         prevListIndex,
                                                         curListIndex)
                               )
                            {
                                // New list. Remember position in pPrevList and write list down
                                pPrevList = pList;

                                for (int j = curListIndex; j < i; j++)
                                {
                                    Debug.Assert((pList - pCache) * sizeof(ushort) < cacheSize);
                                    *pList = records[j].Lookup;
                                    pList++;
                                }

                                Debug.Assert((pList - pCache) * sizeof(ushort) < cacheSize);
                                *pList = 0xFFFF;
                                pList++;
                            }
                            // Now pPrevList points at the first element of the correct list.

                            *pGlyphs = curGlyph;    // Write down glyph id
                            pGlyphs++;
                            *pGlyphs = (ushort)((pPrevList - pCache) * sizeof(ushort)); // Write down list offset
                            pGlyphs++;

                            prevListIndex = curListIndex;
                            prevListLength = curListLength;

                            curGlyph = records[i].Glyph;
                            curListIndex = i;
                            curListLength = 1;
                        }
                    }

                    // And we need to check the last list we missed in the loop
                    if (prevListLength != curListLength || // Fast check to avoid full comparison
                        !CompareGlyphRecordLists(records,
                                                 recordCount,
                                                 prevListIndex,
                                                 curListIndex)
                       )
                    {
                        // New list. Remember position in pPrevList and write list down
                        pPrevList = pList;

                        for (int j = curListIndex; j < recordCount; j++)
                        {
                            Debug.Assert((pList - pCache) * sizeof(ushort) < cacheSize);
                            *pList = records[j].Lookup;
                            pList++;
                        }

                        Debug.Assert((pList - pCache) * sizeof(ushort) < cacheSize);
                        *pList = 0xFFFF;
                        pList++;
                    }
                    // Now pPrevList points at the first element of the correct list.

                    *pGlyphs = curGlyph;    // Write down glyph id
                    pGlyphs++;
                    *pGlyphs = (ushort)((pPrevList - pCache) * sizeof(ushort)); // Write down list offset
                    pGlyphs++;

                    // We are done with the cache
                    Debug.Assert((pList - pCache) * sizeof(ushort) == cacheSize);         // We exactly filled up the cache
                    Debug.Assert((pGlyphs - pCache) * sizeof(ushort) == (4 + glyphCount * 2) * sizeof(ushort)); // Glyphs ended where lists start.
                }
            }

            return cacheSize;
        }

        private static void CalculateCacheSize(GlyphLookupRecord[] records,
                                                         int                 recordCount,
                                                         out int             cacheSize,
                                                         out int             glyphCount
                                                        )
        {
            // Calc cache size
            glyphCount = 1;
            int listCount = 0;
            int entryCount = 0;
            
            int prevListIndex = -1, prevListLength = 0;
            int curListIndex  =  0, curListLength  = 1;
            ushort curGlyph = records[0].Glyph;
            
            for(int i = 1; i < recordCount; i++)
            {
                if (records[i].Glyph != curGlyph)
                {
                    ++glyphCount;
                    
                    // We've found another list. Compare it with previous
                    if (prevListLength != curListLength || // Fast check to avoid full comparison
                        !CompareGlyphRecordLists(records,
                                                 recordCount,
                                                 prevListIndex,
                                                 curListIndex)
                       )
                    {
                        listCount++;
                        entryCount += curListLength;
                    }
                        
                    prevListIndex  = curListIndex;
                    prevListLength = curListLength;

                    curGlyph = records[i].Glyph;
                    curListIndex = i;
                    curListLength = 1;
                }
                else
                {
                    ++curListLength;
                }
            }
            
            // And we need to check the last list we missed in the loop
            if (prevListLength != curListLength || // Fast check to avoid full comparison
                !CompareGlyphRecordLists(records,
                                         recordCount,
                                         prevListIndex,
                                         curListIndex)
               )
            {
                listCount++;
                entryCount += curListLength;
            }
            
            cacheSize = sizeof(ushort) *
                              ( 1 +                 // TotalCacheSize
                                1 +                 // Constant 0xFFFF, so we can point to it from glyphs that are not there
                                1 +                 // Number of lookups that fit into the cache
                                1 +                 // glyph count
                                glyphCount * 2 +    // {glyphId; listOffset} per glyph
                                entryCount +        // Each entry has lookup index
                                listCount           // Plus, terminator entry for each list
                              );
        }
        
        private static bool CompareGlyphRecordLists(
                                                     GlyphLookupRecord[] records,
                                                     int                 recordCount,
                                                     int                 glyphListIndex1,
                                                     int                 glyphListIndex2
                                                   )
        {
            ushort listGlyph1 = records[glyphListIndex1].Glyph;
            ushort listGlyph2 = records[glyphListIndex2].Glyph;
            
            while (true)
            {
                ushort glyph1,  glyph2;
                ushort lookup1, lookup2;
                
                if (glyphListIndex1 != recordCount)
                {
                    glyph1  = records[glyphListIndex1].Glyph;
                    lookup1 = records[glyphListIndex1].Lookup;
                }
                else
                {
                    // Just emulate something that will be never in the real input
                    glyph1 = 0xffff; 
                    lookup1 = 0xffff;
                }
                
                if (glyphListIndex2 != recordCount)
                {
                    glyph2  = records[glyphListIndex2].Glyph;
                    lookup2 = records[glyphListIndex2].Lookup;
                }
                else
                {
                    // Just emulate something that will be never in the real input.
                    glyph2 = 0xffff; 
                    lookup2 = 0xffff;
                }
                
                if (glyph1 != listGlyph1 && glyph2 != listGlyph2)
                {
                    // Both lists are ended at the same time.
                    return true;
                }
                
                if (glyph1 != listGlyph1 || glyph2 != listGlyph2)
                {
                    // One list is ended, another does not.
                    return false;
                }
                
                if (lookup1 != lookup2)
                {
                    // We have different lookups on the lists.
                    return false;
                }
                
                //Lists match so far, move further
                ++glyphListIndex1;
                ++glyphListIndex2;
            }
        }

        private static CoverageTable GetSubtablePrincipalCoverage(
                                                    FontTable    table, 
                                                    OpenTypeTags tableTag, 
                                                    ushort       lookupType, 
                                                    int          subtableOffset
                                                 )
        {
            Debug.Assert(tableTag == OpenTypeTags.GSUB || tableTag == OpenTypeTags.GPOS);
            
            CoverageTable coverage = CoverageTable.InvalidCoverage;
            
            switch (tableTag)
            {
                case OpenTypeTags.GSUB:
                    if (lookupType == 7)
                    {
                        ExtensionLookupTable extension = 
                                new ExtensionLookupTable(subtableOffset);

                        lookupType = extension.LookupType(table);
                        subtableOffset = extension.LookupSubtableOffset(table);
                    }
                    
                    switch (lookupType)
                    {
                        case 1: //SingleSubst
                            SingleSubstitutionSubtable singleSubst = 
                                new SingleSubstitutionSubtable(subtableOffset);

                            return singleSubst.GetPrimaryCoverage(table);
                        
                        case 2: //MultipleSubst 
                            MultipleSubstitutionSubtable multipleSub = 
                                new MultipleSubstitutionSubtable(subtableOffset);
                            return multipleSub.GetPrimaryCoverage(table);
                                                        
                        case 3: //AlternateSubst
                            AlternateSubstitutionSubtable alternateSub =
                                new AlternateSubstitutionSubtable(subtableOffset);
                            return alternateSub.GetPrimaryCoverage(table);

                        case 4: //Ligature subst
                            LigatureSubstitutionSubtable ligaSub = 
                                new LigatureSubstitutionSubtable(subtableOffset);
                            return ligaSub.GetPrimaryCoverage(table);
                                                    
                        case 5: //ContextualSubst
                            ContextSubtable contextSub = 
                                new ContextSubtable(subtableOffset);
                            return contextSub.GetPrimaryCoverage(table);
                            
                        case 6: //ChainingSubst
                            ChainingSubtable chainingSub = 
                                                new ChainingSubtable(subtableOffset);
                            return chainingSub.GetPrimaryCoverage(table);
                            
                        case 7: //Extension lookup
                            // Ext.Lookup processed earlier. It can't contain another ext.lookups in it
                            break;
                            
                        case 8: //ReverseCahiningSubst
                            ReverseChainingSubtable reverseChainingSub = 
                                new ReverseChainingSubtable(subtableOffset);
                            return reverseChainingSub.GetPrimaryCoverage(table);
                    }
                    
                    break;

                case OpenTypeTags.GPOS:
                    if (lookupType == 9)
                    {
                        ExtensionLookupTable extension = 
                                new ExtensionLookupTable(subtableOffset);

                        lookupType = extension.LookupType(table);
                        subtableOffset = extension.LookupSubtableOffset(table);
}
                    
                    switch (lookupType)
                    {
                        case 1: //SinglePos
                            SinglePositioningSubtable singlePos = 
                                new SinglePositioningSubtable(subtableOffset);
                            return singlePos.GetPrimaryCoverage(table);
                                
                        case 2: //PairPos
                            PairPositioningSubtable pairPos = 
                                new PairPositioningSubtable(subtableOffset);
                            return pairPos.GetPrimaryCoverage(table);

                        case 3: // CursivePos
                            CursivePositioningSubtable cursivePos = 
                                new CursivePositioningSubtable(subtableOffset);
                            return cursivePos.GetPrimaryCoverage(table);

                        case 4: //MarkToBasePos
                            MarkToBasePositioningSubtable markToBasePos = 
                                new MarkToBasePositioningSubtable(subtableOffset);
                            return markToBasePos.GetPrimaryCoverage(table);
                            
                        case 5: //MarkToLigaturePos
                            // Under construction
                            MarkToLigaturePositioningSubtable markToLigaPos =
                                new MarkToLigaturePositioningSubtable(subtableOffset);
                            return markToLigaPos.GetPrimaryCoverage(table);

                        case 6: //MarkToMarkPos
                            MarkToMarkPositioningSubtable markToMarkPos = 
                                new MarkToMarkPositioningSubtable(subtableOffset);
                            return markToMarkPos.GetPrimaryCoverage(table);

                        case 7: // Contextual
                            ContextSubtable contextPos = 
                                new ContextSubtable(subtableOffset);
                            return contextPos.GetPrimaryCoverage(table);
                                
                        case 8: // Chaining
                            ChainingSubtable chainingPos = 
                                new ChainingSubtable(subtableOffset);
                            return chainingPos.GetPrimaryCoverage(table);
                        
                        case 9: //Extension lookup
                            // Ext.Lookup processed earlier. It can't contain another ext.lookups in it
                            break;
                    }                
                    
                    break;
            }
            
            return CoverageTable.InvalidCoverage;
        }
        
        /// <summary>
        /// Append lookup coverage table to the list.
        /// </summary>
        /// <param name="table">Font table</param>
        /// <param name="lookupIndex">Lookup index</param>
        /// <param name="coverage">Lookup principal coverage</param>
        /// <param name="records">Record array</param>
        /// <param name="recordCount">Real number of records in record array</param>
        /// <param name="maxLookupGlyph">Highest glyph index that we saw in this lookup</param>
        /// <returns>Returns false if we are out of list space</returns>
        private static bool AppendCoverageGlyphRecords(
                                                    FontTable           table,
                                                    ushort              lookupIndex,
                                                    CoverageTable       coverage,
                                                    GlyphLookupRecord[] records, 
                                                    ref int             recordCount,
                                                    ref int             maxLookupGlyph
                                                )
        {
            switch (coverage.Format(table))
            {
                case 1:
                    ushort glyphCount = coverage.Format1GlyphCount(table);
                
                    for(ushort i = 0; i < glyphCount; i++)
                    {
                        ushort glyph = coverage.Format1Glyph(table, i);
                        
                        if (!AppendGlyphRecord(glyph, lookupIndex, records, ref recordCount, ref maxLookupGlyph))
                        {
                            // We've failed to add another record.
                            return false;
                        }
                    }
                    
                    break;
                    
                case 2:
                
                    ushort rangeCount = coverage.Format2RangeCount(table);
                    
                    for(ushort i = 0; i < rangeCount; i++)
                    {
                        ushort firstGlyph = coverage.Format2RangeStartGlyph(table, i);
                        ushort lastGlyph  = coverage.Format2RangeEndGlyph(table, i);
                        
                        for(int glyph = firstGlyph; glyph <= lastGlyph; glyph++)
                        {
                            if (!AppendGlyphRecord((ushort)glyph, lookupIndex, records, ref recordCount, ref maxLookupGlyph))
                            {
                                // We've failed to add another record.
                                return false;
                            }
                        }
                    }
                
                    break;
            }
            
            return true;
        }
        
        /// <summary>
        /// Append record to the list, but first check if we have duplicate.
        /// </summary>
        /// <param name="glyph">Glyph</param>
        /// <param name="lookupIndex">Lookup index</param>
        /// <param name="records">Record array</param>
        /// <param name="recordCount">Real number of records in record array</param>
        /// <param name="maxLookupGlyph">Highest glyph index that we saw in this lookup</param>
        /// <returns>Returns false if we are out of list space</returns>
        private static bool AppendGlyphRecord(
                                                ushort              glyph,
                                                ushort              lookupIndex,
                                                GlyphLookupRecord[] records, 
                                                ref int             recordCount,
                                                ref int             maxLookupGlyph
                                            )
        {
            if (glyph == maxLookupGlyph)
            {
                // It is exactly max, which means we already've seen it before.
                return true; 
            }

            if (glyph > maxLookupGlyph)
            {
                // This should be very common - coverage tables are ordered by glyph index.
                maxLookupGlyph = glyph;
            }
            else
            {
                // We will go through records to check for duplicate.
                Debug.Assert(recordCount > 0); // Otherwise, we would go into (glyph > maxGlyphLookup);
                for(int i = recordCount - 1; i >= 0; i--)
                {
                    if (records[i].Lookup != lookupIndex) 
                    {
                        // We've iterated through all lookup records
                        // (and haven't found duplicate)
                        break;
                    }

                    if (records[i].Glyph == glyph)
                    {
                        // We found duplicate, no need to do anything. 
                        return true;
                    }
                }
            }
            
            // Now, we need to add new record
            
            if (recordCount == records.Length)
            {
                // There is no space for new record.
                return false;
            }

            records[recordCount] = new GlyphLookupRecord(glyph, lookupIndex);
            recordCount++;
            
            return true;
        }
                                                    
        private class GlyphLookupRecord : IComparable<GlyphLookupRecord>
        {
            private ushort _glyph;
            private ushort _lookup;

            public GlyphLookupRecord(ushort glyph, ushort lookup)
            {
                _glyph = glyph;
                _lookup = lookup;
            }
            
            public ushort Glyph
            {
                get { return _glyph; }
            }
            
            public ushort Lookup
            {
                get { return _lookup; }
            }
                        
            // Records will be sorted by glyph, then by lookup index
            public int CompareTo(GlyphLookupRecord value)
            {
                if (_glyph < value._glyph) return -1;
                if (_glyph > value._glyph) return  1;

                if (_lookup < value._lookup) return -1;
                if (_lookup > value._lookup) return 1;
                
                return 0;
            }

            public bool Equals(GlyphLookupRecord value)
            {
                return _glyph  == value._glyph && 
                       _lookup == value._lookup;
            }
            
            public static bool operator ==(GlyphLookupRecord value1, GlyphLookupRecord value2)
            {
                return value1.Equals(value2);
            }
            public static bool operator !=(GlyphLookupRecord value1, GlyphLookupRecord value2)
            {
                return !value1.Equals(value2);
            }
            public override bool Equals(object value)
            {
                return Equals((GlyphLookupRecord)value);
            }
            public override int GetHashCode()
            {
                return _glyph << 16 + _lookup;
            }
        }

#endregion Cache filling
    }
}
