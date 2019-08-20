// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//

//
//
// Description: Core font cache logic.
//
//---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using MS.Internal;
using MS.Utility;
using MS.Win32;
using MS.Internal.PresentationCore;

using Microsoft.Internal;

// Since we disable PreSharp warnings in this file, we first need to disable warnings about unknown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

namespace MS.Internal.FontCache
{
    /// <summary>
    /// FontCacheFullException is raised when the cache limit is reached
    /// </summary>
    [FriendAccessAllowed]
    internal class FontCacheFullException : ApplicationException
    {
        internal FontCacheFullException()
        {}
    }

    /// <summary>
    /// An abstract entity stored in the cache
    /// The layout is following:
    /// 4 byte offset of next element
    /// 4 byte element type
    /// Everything else is element-specific
    /// </summary>
    [FriendAccessAllowed]
    internal interface IFontCacheElement
    {
        /// <summary>
        /// Matches this element with the one in the cache
        /// </summary>
        /// <param name="p">Pointer to the element in the cache to compare with.</param>
        /// <returns></returns>
        bool Match(CheckedPointer p);

        /// <summary>
        /// GetData is called when the element is found in cache
        /// </summary>
        void GetData(CheckedPointer p, ElementCacher cacher);

        /// <summary>
        /// AddToCache is called to add the element to cache
        /// </summary>
        void AddToCache(CheckedPointer p, ElementCacher cacher);

        /// <summary>
        /// Returns the number of bytes that ElementCacher should allocate for the element
        /// </summary>
        int Size
        {
            get;
        }

        /// <summary>
        /// Integer value unique for the element
        /// </summary>
        int Type
        {
            get;
        }

        /// <summary>
        /// Returns whether the font cache element is application specific and should not be looked for in the shared cache.
        /// </summary>
        bool IsAppSpecific
        {
            get;
        }

        /// <summary>
        /// Elements must override default GetHashCode(), because different instances of otherwise identical elements
        /// should map to the same hash code.
        /// Important note - please avoid using standard CLR GetHashCode() calls in your GetHashCode() implementations,
        /// as they may return different results depending on the target platform (32 bit vs. 64 bit).
        /// Use HashFn helpers instead.
        /// </summary>
        int GetHashCode();

        /// <summary>
        /// Writes the element key into a memory block to be sent to the font cache service.
        /// </summary>
        void StoreKey(CheckedPointer d, out int realSize);

        /// <summary>
        /// Initializes the element using the element key from an input memory block.
        /// </summary>
        void RetrieveKey(CheckedPointer s);
    };

    /// <summary>
    /// ElementCacher - manages cache lookup logic
    /// Layout of the cache file
    /// - fixed size hash table, 4 bytes per element
    /// - user data pool, active length is variable, maximum length is determined by the font cache file size
    /// </summary>
    [FriendAccessAllowed]
    internal unsafe class ElementCacher
    {
        private const int numberOfBuckets = 512;

        // offset should be at least 4 byte aligned
        private const int offMarker = 0;
        private const int offMaxSize = 4;
        private const int offCurSize = 8;
        private const int offVersion = 12;
        internal const int offHashTable = 64;//used by HashTable class

        private const int MinCacheSize = offHashTable + numberOfBuckets * 4;

        [StructLayout(LayoutKind.Explicit, Size = 12)]
        private struct CacheHeader
        {
            [FieldOffset(offMarker)]
            internal int Marker;

            [FieldOffset(offMaxSize)]
            internal int MaxSize;

            [FieldOffset(offCurSize)]
            internal int CurSize;
        }

        // when changing cache or element layout, increment the value stored in this string
        private const string _cacheVersionString = @"76";

        private object _cacheLock = new Object();

        private FileMapping _mfile;

        private bool        _shared;

        private HashTable   _hashTable;

        //The allocated size of the cache, as determined by the most recent call to GetCacheMemoryRemaining().
        //Since the cache size can never decrease, we know that at least this many bytes have been allocated
        //for the cache, even if this value is out of date.  Saving this value allows us to omit redundant
        //calls to VirtualQuery().
        //
        //Note: _cacheAllocSize should be used instead of _mfile.Length because _cacheAllocSize
        //updates itself inside the getter of Mapping if the cache grows; _mfile.Length does not
        //get updated.
        private volatile int _cacheAllocSize = 0;

        //Returns a pointer the cache header, without bounds-checking
        private unsafe CacheHeader* UnsafeGetCacheHeader()
        {
            return (CacheHeader*)(_mfile.PositionPointer);
        }

        //Returns CheckedPointer representing the cache version string.  This is bounded
        //by the start of the cache table.
        private unsafe CheckedPointer GetVersionPointer()
        {
            //Bounds-checking the buffer is not necessary here since the version is part of the header
            return new CheckedPointer(_mfile.PositionPointer + offVersion, offHashTable - offVersion);
        }

        //Returns a pointer to the cache header, assuming the cache has already been initialized
        private unsafe CacheHeader* GetCacheHeader()
        {
            //Since the header is always in memory, Mapping.Probe() is equivlant
            //to just calling the unsafe version
            return UnsafeGetCacheHeader();
        }

        //Initializes the cache header
        private void InitCacheHeader(int curSize, int maxSize)
        {
            Util.AssertAligned8(curSize);
            Util.AssertAligned8(maxSize);
            Invariant.Assert(curSize >= MinCacheSize);
            Invariant.Assert(maxSize >= curSize);

            //First, commit the necessary memory
            _mfile.Commit(MinCacheSize);

            //The unsafe version is ok here since we just committed the memory.  It is also
            //mandatory because the safe version would throw ArgumentOutOfRangeException
            //as the size has not been initialized yet
            CacheHeader* header = UnsafeGetCacheHeader();
            header->Marker = 0;
            header->CurSize = curSize;
            header->MaxSize = maxSize;
        }


        internal ElementCacher(FileMapping mfile, bool create, bool shared)
        {
            _mfile = mfile;
            _shared = shared;

            if (create)
            {
                //First, initialize the cache header; then, create the hash table.
                InitCacheHeader(MinCacheSize, (int)_mfile.Capacity);

                SetNew();
                SetVersion();

                _hashTable = new HashTable(this, numberOfBuckets);
                _hashTable.Init();

                // flush the cache so that clients have a consistent view of it
                Thread.MemoryBarrier();
            }
            else
            {
                //Cache data is already initialized and committed to memory,
                //so all we have to do is just initialize the hash table
                _hashTable = new HashTable(this, numberOfBuckets);
            }
        }

        ~ElementCacher()
        {
            ((IDisposable)_mfile).Dispose();
        }

        /// <summary>
        /// Specifies whether ElementCacher corresponds to an instance of shared cache.
        /// Elements such as FamilyCollection perform error handling differently if they operate on the shared cache.
        /// </summary>
        public bool IsShared
        {
            get
            {
                return _shared;
            }
        }

        private void SetVersion()
        {
            Util.StringCopyToCheckedPointer(GetVersionPointer(), _cacheVersionString);
        }

        internal bool VersionUpToDate()
        {
            return Util.StringEqual(GetVersionPointer(), _cacheVersionString);
        }

        internal int MaxCacheSize
        {
            get
            {
                return GetCacheHeader()->MaxSize;
            }
            set
            {
                GetCacheHeader()->MaxSize = value;
            }
        }


        internal int CurrentSize
        {
            get
            {
                return GetCacheHeader()->CurSize;
            }
        }

        internal void SetNew()
        {
            GetCacheHeader()->Marker = 0;
        }

        internal void MarkObsolete()
        {
            GetCacheHeader()->Marker = 1;
        }


        internal bool IsObsolete()
        {
            return GetCacheHeader()->Marker != 0;
        }

        internal void InitFromCacheImage(CheckedPointer cacheImage)
        {
            if (cacheImage.Size < MinCacheSize ||
                (cacheImage.Size % 8) != 0 ||
                cacheImage.Size > MaxCacheSize)
            {
                // Malformed input cache, just return. Assert in debug builds to catch possible bugs.
                Debug.Assert(false);
                return;
            }

            int oldCurrentSize;
            unsafe
            {
                // Validate the current size field in the existing cache image.
                oldCurrentSize = *(int*)cacheImage.Probe(offCurSize, sizeof(int));
            }

            if (Util.Align8(oldCurrentSize) != oldCurrentSize || oldCurrentSize != cacheImage.Size)
            {
                // Malformed input cache, just return. Assert in debug builds to catch possible bugs.
                Debug.Assert(false);
                return;
            }

            _mfile.Commit(cacheImage.Size);
            GetCacheHeader()->CurSize = cacheImage.Size;

            cacheImage.CopyTo(Mapping);

            // This can happen only if the current cache size field was not set properly in the cache image,
            // and we should have caught this situation earlier in this function.
            Invariant.Assert(CurrentSize == cacheImage.Size);

            SetNew();
        }

        internal void InitFromPreviousCache(ElementCacher oldCache, int newCacheSize)
        {
            // We need to lock the old cache to make sure it's CurrentSize is not updated in the middle of the copy.
            lock (oldCache.Lock)
            {
                InitFromCacheImage(oldCache.Mapping);

                // Reset max cache size to the new size, because InitFromCacheImage overwrote it.
                MaxCacheSize = newCacheSize;
            }
        }

        ///<summary>
        /// Note: we use _mapping + offset directly instead of this indexer
        /// in cases when CurrentSize property is not set yet
        ///</summary>
        internal unsafe byte* this[int offset]
        {
            get
            {
                Invariant.Assert(offset != Util.nullOffset);
                Invariant.Assert(0 <= offset && offset <= CurrentSize); // Note: offset==CurrentSize for zero length arrays

                // Eventually we plan to switch everything to CheckedPointer,
                // and this unsafe probe will go away. For now we rely on caller to
                // specify correct buffer sizes when manipulating values returned from this[int].
                return (byte*)(Mapping.Probe(offset, 0));
            }
        }

        internal int this[byte* pointer]
        {
            get
            {
                long longRes = Mapping.OffsetOf(pointer);
                int intRes = (int)longRes;
                Invariant.Assert(longRes == intRes);
                return intRes;
            }
        }
        internal CheckedPointer GetCheckedPointer(int offset)
        {
            Invariant.Assert(offset != Util.nullOffset && offset <= CurrentSize);
            return Mapping + offset;
        }

        internal object Lock
        {
            get
            {
                return _cacheLock;
            }
        }

        /// <summary>
        /// Allocates 8 byte aligned data from _mfile.
        /// Throws FontCacheFullException if we are out of space
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        internal int Alloc(int size)
        {
            Invariant.Assert(size >= 0);

            // If the requested size is zero, we can return any offset within the file.
            if (size == 0)
                return 0;

            lock(_cacheLock)
            {
                int oldSize = CurrentSize;
                Util.AssertAligned8(oldSize);
                int newSize = Util.Align8(oldSize + size);
                Invariant.Assert(newSize > oldSize);
                if (newSize > MaxCacheSize)
                    throw new FontCacheFullException();


                //commit the memory for the new data
                _mfile.Commit(newSize);

                //Now, update the size entry in the cache header
                GetCacheHeader()->CurSize = newSize;

                Thread.MemoryBarrier();
                return oldSize;
            }
        }

        /// <summary>
        /// IFontCacheElement lookup
        /// </summary>
        /// <param name="e"></param>
        /// Adds the element if it doesn't exist in the cache.
        /// Can be called only if caller is on the same side as cache construction (e.g. client-client).
        /// Thread safe.
        /// Throws FontCacheFullException in case of cache overflow
        /// <returns>Returns whether the element already existed in the cache.</returns>
        internal bool LookupAndAdd(IFontCacheElement e)
        {
            return _hashTable.Lookup(e, true);
        }

        /// <summary>
        /// IFontCacheElement read-only lookup
        /// </summary>
        /// <param name="e"></param>
        /// Thread safe.
        /// <returns>Returns whether the element was found in the cache.</returns>
        internal bool ReadOnlyLookup(IFontCacheElement e)
        {
            return _hashTable.Lookup(e, false);
        }

        internal CheckedPointer Mapping
        {
            get
            {
                unsafe
                {
                    //The size of the CheckedPointer buffer will be equal to the size of the cache,
                    //as contained in the mapping itself.

                    //To prevent corrupt cache data from causing buffer overflows, we check the cache size
                    //against the maximum cache size and number of bytes actually allocated for the cache.
                    //An Invariant.Assert() will be triggered if the cache size extends past the cache buffer.
                    //If the cache size field is larger than the actual cache size, but smaller than the cache buffer
                    //size, we are ok because this excess space is not being used for anything else and if it exposes
                    //corrupt internal offsets, we will eventually have a CheckedPointer ArgumentOutOfRangeException, rather
                    //than buffer overflow.

                    //Since FileMapping objects are guarenteed to be at least one page long, overflowing
                    //the buffer while accessing the font cache header information is not possible.

                    byte* mapping = _mfile.PositionPointer;
                    int currentCacheSize = *((int*)(mapping + offCurSize));
                    int maxCacheSize = *((int*)(mapping + offMaxSize));
                    Invariant.Assert(currentCacheSize <= maxCacheSize);

                    //Generate a local copy in case another thread changes _cacheAllocSize in this section
                    int cacheAllocSize = _cacheAllocSize;

                    if (currentCacheSize > cacheAllocSize)
                    {
                        //Verify that the cache has grown to accomodate the additional space.
                        int bytesRemaining = GetCacheMemoryRemaining(mapping + cacheAllocSize);
                        cacheAllocSize += bytesRemaining;
                        Invariant.Assert(currentCacheSize <= cacheAllocSize);

                        //If _cacheAllocSize increased during our call to GetCacheMemoryRemaining by another thread,
                        //use the newer value.  If another thread increases the value of _cacheAllocSize while we
                        //are inside Math.Max, _cacheAllocSize will be set to a value smaller than the actual allocation
                        //size.  This just means there is a slight chance that we might end up doing a redundant call
                        //to VirtualQuery() later, but the correctness of the program is not affected.
                        _cacheAllocSize = Math.Max(cacheAllocSize, _cacheAllocSize);
                    }

                    //Internal cache size is ok, so create the CheckedPointer for our mapping
                    return new CheckedPointer(mapping, currentCacheSize);

                }
            }
        }

        //Returns the number of bytes available for the current cache
        //starting from the given pointer, which may lie anywhere in the middle
        //of the cache.  ptr need not be aligned on a page boundary.
        private unsafe int GetCacheMemoryRemaining(byte* ptr)
        {
            VirtualQueryClass.MemoryBasicInformation mbi;

            VirtualQueryClass.VirtualQuery(ptr, out mbi);

            //These assertions will fail if ptr is not part of the font cache
            Invariant.Assert(mbi.AllocationBase == _mfile.PositionPointer);
            Invariant.Assert((mbi.AllocationProtect == VirtualQueryClass.PageReadOnly) ||
                             (mbi.AllocationProtect == VirtualQueryClass.PageReadWrite));
            Invariant.Assert(mbi.State == VirtualQueryClass.MemCommit);
            Invariant.Assert(mbi.Type == VirtualQueryClass.MemMapped);

            //Now, compute the memory remaining
            int offsetIntoPage = (int)(ptr - (byte*)mbi.BaseAddress);
            return ((int)mbi.RegionSize) - offsetIntoPage;
        }

    }

    internal static class CacheManager
    {
        // Disable Presharp warning about Dispose() not being called on the disposable FileMapping object.
        // This is by design, because lifetime of m extends this function - the pointer is passed to ElementCacher ctor.
#pragma warning disable 6518

        private static ElementCacher OpenServerCache(string serverSectionName)
        {
            // Disable Presharp warning about empty catch body.
            // This is by design, as we should continue even in case of server connection failures.
#pragma warning disable 6502
            try
            {
                // open cache
                FileMapping m = new FileMapping();
                m.OpenSection(serverSectionName);
                ElementCacher c = new ElementCacher(m, false, true);

                if (c.VersionUpToDate())
                {
                    _serverCache = c;
                    return c;
                }
            }
            // This can be thrown when for some reason we cannot connect to the server.
            catch (IOException)
            {
            }
            return null;
#pragma warning restore 6502
        }

        internal static ElementCacher GetServerCache()
        {
            // the current cache is up to date
            if (_serverCache != null && !_serverCache.IsObsolete())
                return _serverCache;

            // we know that connecting to the service will likely fail
            if (!_tryToConnect)
                return _serverCache;

            lock (_sharedCacheLock)
            {
                // repeat the checks from above within the lock
                if (_serverCache != null && !_serverCache.IsObsolete())
                    return _serverCache;

                if (!_tryToConnect)
                    return _serverCache;

                if (_fc == null)
                {
                    _fc = FontCacheConfig.Current;
                    _ipcMngr = new IPCCacheManager(_fc);
                }

                int errorCode = 0;
                string serverSectionName = null;
                if (_serverCache != null)
                {
                    // Server port is open, but the cache is obsolete. Get a new section name.
                    Debug.Assert(_serverCache.IsObsolete());
                    Debug.Assert(_ipcMngr.IsConnected);

                    Debug.WriteLine("Retrieving cache name from server");
                    //Ignore error code, just let serverSectionName be null if error.
                    serverSectionName = _ipcMngr.GetServerSectionName(_fc.SecondConnectTimeout, out errorCode);
                }
                else
                {
                    //Connect to the server and get the server cache
                    int[] timeouts = { _fc.FirstConnectTimeout, _fc.SecondConnectTimeout };
                    for (int i = 0; ; i++)
                    {
                        //If we had an old connection lying around close it and get rid of it
                        if (_ipcMngr.IsConnected)
                        {
                            _ipcMngr.CloseConnection();
                        }
                        //Get the server name.  If we're not connected, connect.
                        serverSectionName = _ipcMngr.GetServerSectionName(timeouts[i], out errorCode);

                        //If we succeeded, we can stop here
                        if (serverSectionName != null)
                            break;//success

                        if ((i + 1) >= timeouts.Length)
                            break;//all attempts exhausted - give up

                        //If we failed, it could be because the font cache service isn't running,
                        //so try to start it now.  Don't waste time with this if we fail for another reason.
                        if (errorCode == _ipcMngr.ServerNotFoundErrorCode)
                        {
                            if (_fc.RestartServiceOnError)
                            {
                                if (!ServiceOps.StartServiceWithTimeout(BuildInfo.FontCacheServiceName,_fc.ServerStartTimeout))
                                {
                                    //could not start service - give up
                                    break;
                                }
                            }
                            else
                                break;//restart service option not set
                        }
                        else
                        {
                            break;//no point in continuing
                        }
                    }
                }
                if (serverSectionName == null)
                {
                    // keep using the old cache if the new name can't be obtained (or null if there is no old cache)
                    _ipcMngr.CloseConnection();
                    _tryToConnect = false;
                    return _serverCache;
                }

                //Attempt to open the cache.  Update _serverCache if successful,
                //but keep using the old cache if there is an error.
                ElementCacher c = OpenServerCache(serverSectionName);
                if (c != null)
                    _serverCache = c;
                else
                {
                    // low memory conditions, don't attempt to connect, but keep using the old cache
                    _tryToConnect = false;
                }

                return _serverCache;
            }
        }

        internal static ElementCacher GetCurrentCache()
        {
            ElementCacher c = _currentCache;
            if (c == null || c.IsObsolete())
                c = RenewCache(c);

            Debug.Assert(c != null);
            return c;
        }

        internal static ElementCacher RenewCache(ElementCacher oldCache)
        {
            lock (_clientCacheLock)
            {
                // If another thread has already updated the cache, we don't need to renew.
                if (_currentCache != oldCache)
                    return _currentCache;

                // The size of the new cache.
                int newCacheSize;

                // Whether to copy the contents of the old cache into the new cache.
                bool copyOldCache = false;

                if (oldCache == null)
                    newCacheSize = FontCacheConstants.InitialLocalCacheSize;
                else
                {
                    newCacheSize = oldCache.MaxCacheSize * FontCacheConstants.CacheGrowthFactor;

                    if (newCacheSize > FontCacheConstants.MaximumLocalCacheSize)
                        newCacheSize = FontCacheConstants.MaximumLocalCacheSize;
                    else
                        copyOldCache = true;
                }

                // Current client cache either doesn't exist or it is obsolete,
                // create a new one.
                FileMapping m = new FileMapping();
                m.Create(null, newCacheSize);
                ElementCacher c = new ElementCacher(m, true, false);

                if (copyOldCache)
                {
                    // Copy the bits from the old cache.
                    Debug.Assert(newCacheSize > oldCache.CurrentSize);

                    c.InitFromPreviousCache(oldCache, newCacheSize);
                }

                // Flush the cache before exposing it, so that clients have a consistent view of it
                Thread.MemoryBarrier();

                _currentCache = c;

                if (oldCache != null)
                    oldCache.MarkObsolete();
                return c;
            }
        }
#pragma warning restore 6518

        internal static void SaveNativeCache(ElementCacher c, IList<ElementCacher> nativeCaches)
        {
            int count = nativeCaches.Count;
            if (count > 0 && c == nativeCaches[count - 1])
                return; // it's already stored, so don't bother
            nativeCaches.Add(c);
        }

        internal static void SendMissReport(IFontCacheElement e)
        {
            Debug.Assert(!e.IsAppSpecific);
            _ipcMngr.SendMissReport(e);
        }

        internal static void Lookup(IFontCacheElement e)
        {
            ElementCacher c;
            if (!e.IsAppSpecific)
            {
                c = GetServerCache();
                if (c != null)
                {
                    if (c.ReadOnlyLookup(e))
                        return;

                    EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordText, EventTrace.Event.WClientFontCache);
                }
                else
                {
                    EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordText, EventTrace.Event.WClientFontCache);
                }
            }

            c = GetCurrentCache();

            for (; ; )
            {
                try
                {
                    if (!c.LookupAndAdd(e) && !e.IsAppSpecific)
                    {
                        // send a miss report if we had to add a new element
                        SendMissReport(e);
                    }
                    return;
                }
                catch (FontCacheFullException)
                {
                    EventTrace.EasyTraceEvent(EventTrace.Keyword.KeywordText, EventTrace.Event.WClientFontCache);
                }

                // Cache overflow, start a new cache and attempt element construction again.
                c = RenewCache(c);
            }
        }


        //--------------------------------//
        // Private Fields                 //
        //--------------------------------//
        #region Private Fields

        private static object _sharedCacheLock = new object();

        private static FontCacheConfig _fc = null;

        private static IPCCacheManager _ipcMngr = null;

        private static volatile ElementCacher _serverCache;

        // this will be set to false if we decide that FontCacheService is in a bad state
        private static volatile bool _tryToConnect = true;

        // client cache
        private static object _clientCacheLock = new object();

        private static volatile ElementCacher _currentCache;

        #endregion Private Fields
    }
}
