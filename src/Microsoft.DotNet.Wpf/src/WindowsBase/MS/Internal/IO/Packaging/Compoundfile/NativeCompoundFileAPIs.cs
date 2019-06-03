// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  The COM and P/Invoke interop code necessary for the managed compound
//  file layer to call the existing APIs in OLE32.DLL.
//
//  Note that not everything is properly ported, for example the SNB type
//  used in several IStorage methods is just ignored.

//

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;

using MS.Internal.Interop;
using MS.Internal.WindowsBase;  // for SecurityHelper

using CultureInfo = System.Globalization.CultureInfo;

namespace MS.Internal.IO.Packaging.CompoundFile
{
    // <SecurityNote>
    //     Critical:  This class serves as a wrapper on top of several unmanaged CompoundFile
    //      interfaces and API calls. These interfaces and APIs has suppress unamanged
    //      code attribute set.
    //     It is up to this class to ensure that the only calls that can go through must
    //     be either done in Full Trust or with CompoundFileIOPermission.
    //     This class exposes several internal APIs and interfaces built on top of classes that
    //     demand the CompoundFileIOPermission and then call through the matching member of
    //      the unsafe APIs and interfaces.
    //     SecurityTreatAsSafe:  Demands CompoundFileIOPermission before it makes any calls to
    //      unmanaged APIs
    // </SecurityNote>
    [SecurityCritical(SecurityCriticalScope.Everything), SecurityTreatAsSafe]
    internal static class SafeNativeCompoundFileMethods
    {
        /// <summary>
        /// Utility function to update a grfMode value based on FileAccess.
        /// 6/12/2002: Fixes bug #4938, 4960, 5096, 4858
        /// </summary>
        /// <param name="access">FileAccess we're translating</param>
        /// <param name="grfMode">Mode flag parameter to modify</param>
        // <SecurityNote>
        //     SecurityTreatAsSafe:  Makes NO call to security suppressed unmanaged code
        // </SecurityNote>
        internal static void UpdateModeFlagFromFileAccess( FileAccess access, ref int grfMode )
        {
            // Supporting write-only scenarios container-wide gets tricky and it 
            //  is rarely used.  Don't support it for now because of poor 
            //  cost/benefit ratio.
            if( FileAccess.Write == access )
                throw new NotSupportedException(
                    SR.Get(SRID.WriteOnlyUnsupported));
            
            // Generate STGM from FileAccess
            // STGM_READ is 0x00, so it's "by default"
            if( (  FileAccess.ReadWrite                == (access &  FileAccess.ReadWrite) )  ||
                ( (FileAccess.Read | FileAccess.Write) == (access & (FileAccess.Read | FileAccess.Write))) )
            {
                grfMode |= SafeNativeCompoundFileConstants.STGM_READWRITE;
            }
            else if( FileAccess.Write == (access & FileAccess.Write) )
            {
                grfMode |= SafeNativeCompoundFileConstants.STGM_WRITE;
            }
            else if( FileAccess.Read != (access & FileAccess.Read))
            {
                throw new ArgumentException(
                    SR.Get(SRID.FileAccessInvalid));
            }
        }

        internal static int SafeStgCreateDocfileOnStream(
            Stream s,
            int grfMode,
            out IStorage ppstgOpen
            )
        {
            SecurityHelper.DemandCompoundFileIOPermission();

            Invariant.Assert(s != null, "s cannot be null");

            UnsafeNativeCompoundFileMethods.UnsafeNativeIStorage storage;
            UnsafeNativeCompoundFileMethods.UnsafeLockBytesOnStream lockByteStream = new UnsafeNativeCompoundFileMethods.UnsafeLockBytesOnStream(s);
            int result;

            result = UnsafeNativeCompoundFileMethods.StgCreateDocfileOnILockBytes(
                (UnsafeNativeCompoundFileMethods.UnsafeNativeILockBytes) lockByteStream,
                grfMode,
                0, // Must be zero
                out storage);

            if (result == SafeNativeCompoundFileConstants.S_OK)
                ppstgOpen = new SafeIStorageImplementation(storage, lockByteStream);
            else
            {
                ppstgOpen = null;
                lockByteStream.Dispose();
            }

            return result;
        }
        
        internal static int SafeStgOpenStorageOnStream(
            Stream s,
            int grfMode,
            out IStorage ppstgOpen
            )
        {
            SecurityHelper.DemandCompoundFileIOPermission();

            Invariant.Assert(s != null, "s cannot be null");

            UnsafeNativeCompoundFileMethods.UnsafeNativeIStorage storage;
            UnsafeNativeCompoundFileMethods.UnsafeLockBytesOnStream lockByteStream = new UnsafeNativeCompoundFileMethods.UnsafeLockBytesOnStream(s);
            int result;

            result = UnsafeNativeCompoundFileMethods.StgOpenStorageOnILockBytes(
                (UnsafeNativeCompoundFileMethods.UnsafeNativeILockBytes) lockByteStream,
                null,
                grfMode,
                new IntPtr(0), // Pointer to SNB struct, not marshalled, must be null.
                0,
                out storage);

            if (result == SafeNativeCompoundFileConstants.S_OK)
                ppstgOpen = new SafeIStorageImplementation(storage);
            else
            {
                ppstgOpen = null;
                lockByteStream.Dispose();
            }

            return result;
}

        internal static int SafeStgCreateStorageEx(
            string pwcsName,     //Pointer to path of compound file to create
            int grfMode,       // Specifies the access mode for opening the storage object
            int stgfmt,        // Specifies the storage file format, 5 is DocFile
            int grfAttrs,      // Reserved; must be zero
            IntPtr pStgOptions,// Pointer to STGOPTIONS, not marshalled, must use NULL.
            IntPtr reserved2,  // Reserved; must be null
            ref Guid riid,     // Specifies the GUID of the interface pointer
            out IStorage ppObjectOpen       //Pointer to an interface pointer
            )
        {
            SecurityHelper.DemandCompoundFileIOPermission();

            UnsafeNativeCompoundFileMethods.UnsafeNativeIStorage storage;
            int result;

            result = UnsafeNativeCompoundFileMethods.StgCreateStorageEx(
                pwcsName,
                grfMode,
                stgfmt,
                grfAttrs,
                pStgOptions,
                reserved2,
                ref riid,
                out storage);

            if (result == SafeNativeCompoundFileConstants.S_OK)
                ppObjectOpen = new SafeIStorageImplementation(storage);
            else
                ppObjectOpen = null;

            return result;
        }

        internal static int SafeStgOpenStorageEx(
            string pwcsName,     //Pointer to path of compound file to create
            int grfMode,       // Specifies the access mode for opening the storage object
            int stgfmt,        // Specifies the storage file format, 5 is DocFile
            int grfAttrs,      // Reserved; must be zero
            IntPtr pStgOptions,// Pointer to STGOPTIONS, not marshalled, must use NULL.
            IntPtr reserved2,  // Reserved; must be null
            ref Guid riid,     // Specifies the GUID of the interface pointer
            out IStorage ppObjectOpen       //Pointer to an interface pointer
            )    
        {
            SecurityHelper.DemandCompoundFileIOPermission();

            UnsafeNativeCompoundFileMethods.UnsafeNativeIStorage storage;
            int result;

            result = UnsafeNativeCompoundFileMethods.StgOpenStorageEx(
                pwcsName,
                grfMode,
                stgfmt,
                grfAttrs,
                pStgOptions,
                reserved2,
                ref riid,
                out storage);

            if (result == SafeNativeCompoundFileConstants.S_OK)
                ppObjectOpen = new SafeIStorageImplementation(storage);
            else
                ppObjectOpen = null;

            return result;
}

        // <SecurityNote>
        //     Critical:  This method calls security suppressed unmanaged CompoundFile code.
        //     ComoundFileIOPermission needs to be demanded to ensure that the only calls
        //     that can go through must be either done in Full Trust or with under assertion of
        //     CompoundFileIOPermission.
        //     SecurityTreatAsSafe:  Demands CompoundFileIOPermission before it makes any calls to
        //      unmanaged APIs
        // </SecurityNote>
        internal static int SafePropVariantClear(ref PROPVARIANT pvar)
        {
            SecurityHelper.DemandCompoundFileIOPermission();

            return UnsafeNativeCompoundFileMethods.PropVariantClear(ref pvar);
        }

        private class SafeIStorageImplementation : IStorage, IPropertySetStorage, IDisposable
        {
            internal SafeIStorageImplementation(UnsafeNativeCompoundFileMethods.UnsafeNativeIStorage storage)
                    : this(storage, null)
            {
            }

            internal SafeIStorageImplementation(UnsafeNativeCompoundFileMethods.UnsafeNativeIStorage storage,
                                                        UnsafeNativeCompoundFileMethods.UnsafeLockBytesOnStream lockBytesStream)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                if (storage == null)
                {
                    throw new ArgumentNullException("storage");
                }

                _unsafeStorage = storage;
                _unsafePropertySetStorage = (UnsafeNativeCompoundFileMethods.UnsafeNativeIPropertySetStorage) _unsafeStorage;
                _unsafeLockByteStream = lockBytesStream;
            }

            public void Dispose()
            {              
                SecurityHelper.DemandCompoundFileIOPermission();

                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Dispose(bool)
            /// </summary>
            /// <param name="disposing"></param>
            protected virtual void Dispose(bool disposing)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                try
                {
                    if (disposing && (_unsafeStorage != null))
                    {
                        // We only need to release IStorage only not IPropertySetStorage
                        //  since it shares once instance of RCW
                        MS.Win32.UnsafeNativeMethods.SafeReleaseComObject((object) _unsafeStorage);

                        // If the storage was originally opened on lockbyte implementation
                        //  we need to dispose it as well
                        if (_unsafeLockByteStream != null)
                        {
                            _unsafeLockByteStream.Dispose();
                        }
                    }
                }
                finally
                {
                    _unsafeStorage = null;
                    _unsafePropertySetStorage = null;
                    _unsafeLockByteStream = null;
                }
            }

            //
            // IStorage Implementation
            //

            int IStorage.CreateStream(
                string pwcsName,
                int grfMode, 
                int reserved1, 
                int reserved2,
                out IStream ppstm )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                UnsafeNativeCompoundFileMethods.UnsafeNativeIStream stream;
                int result;

                result = _unsafeStorage.CreateStream( 
                    pwcsName,
                    grfMode, 
                    reserved1, 
                    reserved2,
                    out stream);

                if (result == SafeNativeCompoundFileConstants.S_OK)
                {
                    ppstm = new SafeIStreamImplementation(stream);
                }
                else
                {
                    ppstm = null;
                }

                return result;
            }

            int IStorage.OpenStream(
                string pwcsName,
                int reserved1,
                int grfMode,
                int reserved2,
                out IStream ppstm )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                UnsafeNativeCompoundFileMethods.UnsafeNativeIStream stream;
                int result;

                result = _unsafeStorage.OpenStream(
                    pwcsName,
                    reserved1,
                    grfMode,
                    reserved2,
                    out stream);

                if (result == SafeNativeCompoundFileConstants.S_OK)
                {
                    ppstm = new SafeIStreamImplementation(stream);
                }
                else
                {
                    ppstm = null;
                }

                return result;
            }

            int IStorage.CreateStorage(
                string pwcsName,
                int grfMode,
                int reserved1,
                int reserved2,
                out IStorage ppstg )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                UnsafeNativeCompoundFileMethods.UnsafeNativeIStorage storage;
                int result;

                result = _unsafeStorage.CreateStorage(
                    pwcsName,
                    grfMode,
                    reserved1,
                    reserved2,
                    out storage);

                if (result == SafeNativeCompoundFileConstants.S_OK)
                {
                    ppstg = new SafeIStorageImplementation(storage);
                }
                else
                {
                    ppstg = null;
                }

                return result;
            }

            int IStorage.OpenStorage(
                string pwcsName,
                IStorage pstgPriority,
                int grfMode,
                IntPtr snbExclude,  // Not properly translated, but must be NULL anyway
                int reserved,
                out IStorage ppstg )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                UnsafeNativeCompoundFileMethods.UnsafeNativeIStorage storage;
                int result;

                result = _unsafeStorage.OpenStorage(
                    pwcsName,
                    pstgPriority == null ? null : ((SafeIStorageImplementation) pstgPriority)._unsafeStorage,
                    grfMode,
                    snbExclude,
                    reserved,
                    out storage);

                if (result == SafeNativeCompoundFileConstants.S_OK)
                {
                    ppstg = new SafeIStorageImplementation(storage);
                }
                else
                {
                    ppstg = null;
                }

                return result;
            }

            void IStorage.CopyTo(
                int ciidExclude,
                Guid[] rgiidExclude,
                IntPtr snbExclude,  // Not properly translated, use NULL to avoid `blow-up
                IStorage ppstg )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                Invariant.Assert(ppstg != null, "ppstg cannot be null");

                _unsafeStorage.CopyTo(
                    ciidExclude,
                    rgiidExclude,
                    snbExclude,
                    ((SafeIStorageImplementation) ppstg)._unsafeStorage);
            }

            void IStorage.MoveElementTo(
                string pwcsName,
                IStorage pstgDest,
                string pwcsNewName,
                int grfFlags )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                Invariant.Assert(pstgDest != null, "pstgDest cannot be null");

                _unsafeStorage.MoveElementTo(
                    pwcsName,
                    ((SafeIStorageImplementation) pstgDest)._unsafeStorage,
                    pwcsNewName,
                    grfFlags);
            }

            void IStorage.Commit(
                int grfCommitFlags )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafeStorage.Commit(
                    grfCommitFlags);
            }

            void IStorage.Revert()
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafeStorage.Revert();
            }

            void IStorage.EnumElements(
                int reserved1,
                IntPtr reserved2,
                int reserved3,
                out IEnumSTATSTG ppEnum )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                UnsafeNativeCompoundFileMethods.UnsafeNativeIEnumSTATSTG enumSTATSTG;

                _unsafeStorage.EnumElements(
                    reserved1,
                    reserved2,
                    reserved3,
                    out enumSTATSTG);

                if (enumSTATSTG != null)
                    ppEnum = new SafeIEnumSTATSTGImplementation(enumSTATSTG);
                else
                    ppEnum = null;
            }

            void IStorage.DestroyElement(
                string pwcsName )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafeStorage.DestroyElement(
                    pwcsName);
            }

            void IStorage.RenameElement(
                string pwcsOldName,
                string pwcsNewName )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafeStorage.RenameElement(
                    pwcsOldName,
                    pwcsNewName);
            }

            void IStorage.SetElementTimes(
                string pwcsName,
                System.Runtime.InteropServices.ComTypes.FILETIME pctime,
                System.Runtime.InteropServices.ComTypes.FILETIME patime,
                System.Runtime.InteropServices.ComTypes.FILETIME pmtime )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafeStorage.SetElementTimes(
                    pwcsName,
                    pctime,
                    patime,
                    pmtime);
            }

            void IStorage.SetClass(
                ref Guid clsid ) // Hopefully "ref" is how I tell it to use a pointer 
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafeStorage.SetClass(
                    ref clsid );
            }

            void IStorage.SetStateBits(
                int grfStateBits,
                int grfMask )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafeStorage.SetStateBits(
                    grfStateBits,
                    grfMask);
            }

            void IStorage.Stat(
                out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg,
                int grfStatFlag )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafeStorage.Stat(
                    out pstatstg,
                    grfStatFlag);
            }

            void IPropertySetStorage.Create(
                    ref Guid rfmtid,
                    ref Guid pclsid,
                    UInt32 grfFlags,
                    UInt32 grfMode,
                    out IPropertyStorage ppprstg
                    )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                UnsafeNativeCompoundFileMethods.UnsafeNativeIPropertyStorage propertyStorage;

                _unsafePropertySetStorage.Create(
                    ref rfmtid,
                    ref pclsid,
                    grfFlags,
                    grfMode,
                    out propertyStorage
                    );

                if (propertyStorage != null)
                    ppprstg = new SafeIPropertyStorageImplementation(propertyStorage);
                else
                    ppprstg = null;
            }

            int IPropertySetStorage.Open(
                    ref Guid rfmtid,
                    UInt32 grfMode,
                    out IPropertyStorage ppprstg
                    )
            {
                SecurityHelper.DemandCompoundFileIOPermission();
                
                UnsafeNativeCompoundFileMethods.UnsafeNativeIPropertyStorage propertyStorage;

                int hr = _unsafePropertySetStorage.Open(
                    ref rfmtid,
                    grfMode,
                    out propertyStorage
                    );

                if (propertyStorage != null)
                    ppprstg = new SafeIPropertyStorageImplementation(propertyStorage);
                else
                    ppprstg = null;

                return hr;
            }

            void IPropertySetStorage.Delete(
                    ref Guid rfmtid
                    )
            {
                SecurityHelper.DemandCompoundFileIOPermission();
                
                _unsafePropertySetStorage.Delete(
                    ref rfmtid
                    );
            }

            void IPropertySetStorage.Enum(
                    out IEnumSTATPROPSETSTG ppenum
                    )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                UnsafeNativeCompoundFileMethods.UnsafeNativeIEnumSTATPROPSETSTG enumSTATPROPSETSTG;
                
                _unsafePropertySetStorage.Enum(
                    out enumSTATPROPSETSTG
                    );

                if (enumSTATPROPSETSTG != null)
                    ppenum = new SafeIEnumSTATPROPSETSTGImplementation(enumSTATPROPSETSTG);
                else
                    ppenum = null;
            }

            private UnsafeNativeCompoundFileMethods.UnsafeNativeIPropertySetStorage _unsafePropertySetStorage;
            private UnsafeNativeCompoundFileMethods.UnsafeNativeIStorage _unsafeStorage;
            private UnsafeNativeCompoundFileMethods.UnsafeLockBytesOnStream _unsafeLockByteStream;
        }

        private class SafeIStreamImplementation : IStream, IDisposable
        {
            internal SafeIStreamImplementation(UnsafeNativeCompoundFileMethods.UnsafeNativeIStream stream)
            {
                _unsafeStream = stream;
            }

            public void Dispose()
            {              
                SecurityHelper.DemandCompoundFileIOPermission();

                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                try
                {
                    if (disposing && (_unsafeStream != null))
                    {
                        MS.Win32.UnsafeNativeMethods.SafeReleaseComObject((object) _unsafeStream);
                    }
                }
                finally
                {
                    _unsafeStream = null;
                }
            }

            //
            // IStream Implementation
            //
            void IStream.Read(Byte[] pv, int cb, out int pcbRead)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                if (cb < 0)
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidArgumentValue, "cb", cb.ToString(CultureInfo.InvariantCulture)));
                }

                _unsafeStream.Read(pv, cb, out pcbRead);
            }

            void IStream.Write(Byte[] pv, int cb, out int pcbWritten)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                if (cb < 0)
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidArgumentValue, "cb", cb.ToString(CultureInfo.InvariantCulture)));
                }

                _unsafeStream.Write(pv, cb, out pcbWritten);
            }


            // IStream portion
            void IStream.Seek(long dlibMove, int dwOrigin, out long plibNewPosition)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                if (dwOrigin < 0)
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidArgumentValue, "dwOrigin", dwOrigin.ToString(CultureInfo.InvariantCulture)));
                }

                if (dlibMove < 0 && dwOrigin == SafeNativeCompoundFileConstants.STREAM_SEEK_SET)
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidArgumentValue, "dlibMove", dlibMove.ToString(CultureInfo.InvariantCulture)));
                }

                _unsafeStream.Seek(dlibMove, dwOrigin, out plibNewPosition);
            }

            void IStream.SetSize(long libNewSize)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                if (libNewSize < 0)
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidArgumentValue, "libNewSize", libNewSize.ToString(CultureInfo.InvariantCulture)));
                }

                _unsafeStream.SetSize(libNewSize);
            }

            void IStream.CopyTo(IStream pstm, long cb, out long pcbRead, out long pcbWritten)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                Invariant.Assert(pstm != null, "pstm cannot be null");

                if (cb < 0)
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidArgumentValue, "cb", cb.ToString(CultureInfo.InvariantCulture)));
                }

                _unsafeStream.CopyTo(((SafeIStreamImplementation) pstm)._unsafeStream, cb, out pcbRead, out pcbWritten);
            }

            void IStream.Commit(int grfCommitFlags)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafeStream.Commit(grfCommitFlags);
            }

            void IStream.Revert()
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafeStream.Revert();
            }

            void IStream.LockRegion(long libOffset, long cb, int dwLockType)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                if (libOffset < 0)
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidArgumentValue, "libOffset", libOffset.ToString(CultureInfo.InvariantCulture)));
                }
                if (cb < 0)
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidArgumentValue, "cb", cb.ToString(CultureInfo.InvariantCulture)));
                }

                _unsafeStream.LockRegion(libOffset, cb, dwLockType);
            }

            void IStream.UnlockRegion(long libOffset, long cb, int dwLockType)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                if (libOffset < 0)
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidArgumentValue, "libOffset", libOffset.ToString(CultureInfo.InvariantCulture)));
                }
                if (cb < 0)
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidArgumentValue, "cb", cb.ToString(CultureInfo.InvariantCulture)));
                }

                _unsafeStream.UnlockRegion(libOffset, cb, dwLockType);
            }

            void IStream.Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg, int grfStatFlag)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafeStream.Stat(out pstatstg, grfStatFlag);
            }

            void IStream.Clone(out IStream ppstm)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                UnsafeNativeCompoundFileMethods.UnsafeNativeIStream stream;

                _unsafeStream.Clone(out stream);

                if (stream != null)
                {
                    ppstm = new SafeIStreamImplementation(stream);
                }
                else
                {
                    ppstm = null;
                }
            }

            private UnsafeNativeCompoundFileMethods.UnsafeNativeIStream _unsafeStream;
        }

        private class SafeIEnumSTATPROPSETSTGImplementation : IEnumSTATPROPSETSTG, IDisposable
        {
            internal SafeIEnumSTATPROPSETSTGImplementation(UnsafeNativeCompoundFileMethods.UnsafeNativeIEnumSTATPROPSETSTG enumSTATPROPSETSTG)
            {
                _unsafeEnumSTATPROPSETSTG = enumSTATPROPSETSTG;
            }

            public void Dispose()
            {              
                SecurityHelper.DemandCompoundFileIOPermission();

                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Dispose(bool)
            /// </summary>
            /// <param name="disposing"></param>
            protected virtual void Dispose(bool disposing)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                try
                {
                    if (disposing && (_unsafeEnumSTATPROPSETSTG != null))
                    {
                        MS.Win32.UnsafeNativeMethods.SafeReleaseComObject((object) _unsafeEnumSTATPROPSETSTG);
                    }
                }
                finally
                {
                    _unsafeEnumSTATPROPSETSTG = null;
                }
            }

            //
            // The caller must allocate an array of celt STATPROPSETSTG structures
            // to receive the results.
            //
            // This method is PreserveSig because it can return a non-0 success
            // code; S_FALSE => fewer than celt elements were returned.
            //
            int
            IEnumSTATPROPSETSTG.Next(
                UInt32 celt,
                STATPROPSETSTG rgelt,
                out UInt32 pceltFetched
                )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                return _unsafeEnumSTATPROPSETSTG.Next(
                    celt,
                    rgelt,
                    out pceltFetched
                    );
            }

            void IEnumSTATPROPSETSTG.Skip(UInt32 celt)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafeEnumSTATPROPSETSTG.Skip(celt);
            }

            void IEnumSTATPROPSETSTG.Reset()
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafeEnumSTATPROPSETSTG.Reset();
            }

            void IEnumSTATPROPSETSTG.Clone(out IEnumSTATPROPSETSTG ppenum)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                UnsafeNativeCompoundFileMethods.UnsafeNativeIEnumSTATPROPSETSTG enumSTATPROPSETSTG;

                _unsafeEnumSTATPROPSETSTG.Clone(out enumSTATPROPSETSTG);

                if (enumSTATPROPSETSTG != null)
                    ppenum = new SafeIEnumSTATPROPSETSTGImplementation(enumSTATPROPSETSTG);
                else
                    ppenum = null;
            }

            private UnsafeNativeCompoundFileMethods.UnsafeNativeIEnumSTATPROPSETSTG _unsafeEnumSTATPROPSETSTG;
        }

        private class SafeIPropertyStorageImplementation : IPropertyStorage, IDisposable
        {
            internal SafeIPropertyStorageImplementation(UnsafeNativeCompoundFileMethods.UnsafeNativeIPropertyStorage propertyStorage)
            {
                _unsafePropertyStorage = propertyStorage;
            }

            public void Dispose()
            {              
                SecurityHelper.DemandCompoundFileIOPermission();

                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Dispose(bool)
            /// </summary>
            /// <param name="disposing"></param>
            protected virtual void Dispose(bool disposing)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                try
                {
                    if (disposing && (_unsafePropertyStorage != null))
                    {
                        MS.Win32.UnsafeNativeMethods.SafeReleaseComObject((object) _unsafePropertyStorage);
                    }
                }
                finally
                {
                    _unsafePropertyStorage = null;
                }
            }

            //
            // We preserve the HRESULT on this method because we need to distinguish
            // between S_OK (we got the properties we asked for) and S_FALSE (none of
            // the properties exist).
            //
            int IPropertyStorage.ReadMultiple(
                UInt32 cpspec,
                PROPSPEC[] rgpspec,
                PROPVARIANT[] rgpropvar
                )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                return _unsafePropertyStorage.ReadMultiple(
                    cpspec,
                    rgpspec,
                    rgpropvar
                    );
            }

            void IPropertyStorage.WriteMultiple(
                UInt32 cpspec,
                PROPSPEC[] rgpspec,
                PROPVARIANT[] rgpropvar,
                uint propidNameFirst
                )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafePropertyStorage.WriteMultiple(
                    cpspec,
                    rgpspec,
                    rgpropvar,
                    propidNameFirst
                    );
            }

            void IPropertyStorage.DeleteMultiple(
                UInt32 cpspec,
                PROPSPEC[] rgpspec
                )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafePropertyStorage.DeleteMultiple(
                    cpspec,
                    rgpspec
                    );
            }

            void IPropertyStorage.ReadPropertyNames(
                UInt32 cpropid,
                UInt32[] rgpropid,
                string[] rglpwstrName
                )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafePropertyStorage.ReadPropertyNames(
                    cpropid,
                    rgpropid,
                    rglpwstrName
                    );
            }

            void IPropertyStorage.WritePropertyNames(
                UInt32 cpropid,
                UInt32[] rgpropid,
                string[] rglpwstrName
                )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafePropertyStorage.WritePropertyNames(
                    cpropid,
                    rgpropid,
                    rglpwstrName
                    );
            }

            void IPropertyStorage.DeletePropertyNames(
                UInt32 cpropid,
                UInt32[] rgpropid
                )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafePropertyStorage.DeletePropertyNames(
                    cpropid,
                    rgpropid
                    );
            }

            void IPropertyStorage.Commit(
                UInt32 grfCommitFlags
                )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafePropertyStorage.Commit(
                    grfCommitFlags
                    );
            }

            void IPropertyStorage.Revert()
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafePropertyStorage.Revert();
            }

            void IPropertyStorage.Enum(
                out IEnumSTATPROPSTG ppenum
                )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

#if Using_SafeIPropertyStorageImplementation_Enum                

                UnsafeNativeCompoundFileMethods.UnsafeNativeIEnumSTATPROPSTG unsafeEnumSTATPROPSTG;

                _unsafePropertyStorage.Enum(
                    out unsafeEnumSTATPROPSTG
                    );

                if (unsafeEnumSTATPROPSTG != null)
                    ppenum = new SafeIEnumSTATPROPSTGImplementation(unsafeEnumSTATPROPSTG);
                else
#endif                    
                    ppenum = null;
            }

            void IPropertyStorage.SetTimes(
                ref System.Runtime.InteropServices.ComTypes.FILETIME pctime,
                ref System.Runtime.InteropServices.ComTypes.FILETIME patime,
                ref System.Runtime.InteropServices.ComTypes.FILETIME pmtime
                )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafePropertyStorage.SetTimes(
                    ref pctime,
                    ref patime,
                    ref pmtime
                    );
            }

            void IPropertyStorage.SetClass(
                ref Guid clsid
                )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafePropertyStorage.SetClass(
                    ref clsid
                    );
            }

            void IPropertyStorage.Stat(
                out STATPROPSETSTG pstatpsstg
                )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafePropertyStorage.Stat(
                    out pstatpsstg
                    );
            }

            private UnsafeNativeCompoundFileMethods.UnsafeNativeIPropertyStorage _unsafePropertyStorage;
        }

#if Using_SafeIPropertyStorageImplementation_Enum                
        private class SafeIEnumSTATPROPSTGImplementation : IEnumSTATPROPSTG
        {
            internal SafeIEnumSTATPROPSTGImplementation(UnsafeNativeCompoundFileMethods.UnsafeNativeIEnumSTATPROPSTG enumSTATPROPSTG)
            {
                _unsafeEnumSTATPROPSTG= enumSTATPROPSTG;
            }

            public void Dispose()
            {              
                SecurityHelper.DemandCompoundFileIOPermission();

                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Dispose(bool)
            /// </summary>
            /// <param name="disposing"></param>
            protected virtual void Dispose(bool disposing)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                try
                {
                    if (disposing && (_unsafeEnumSTATPROPSTG != null))
                    {
                        MS.Win32.UnsafeNativeMethods.SafeReleaseComObject((object) _unsafeEnumSTATPROPSTG);
                    }
                }
                finally
                {
                    _unsafeEnumSTATPROPSTG = null;
                }
            }

            //
            // The caller must allocate an array of celt STATPROPSTG structures
            // to receive the results.
            //
            // This method is PreserveSig because it can return a non-0 success
            // code; S_FALSE => fewer than celt elements were returned.
            //
            int
            IEnumSTATPROPSTG.Next(
                UInt32 celt,
                STATPROPSTG rgelt,
                out UInt32 pceltFetched
                )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                return _unsafeEnumSTATPROPSTG.Next(
                    celt,
                    rgelt,
                    out pceltFetched
                    );
            }

            void IEnumSTATPROPSTG.Skip(UInt32 celt)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafeEnumSTATPROPSTG.Skip(celt);
            }

            void IEnumSTATPROPSTG.Reset()
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafeEnumSTATPROPSTG.Reset();
            }

            void IEnumSTATPROPSTG.Clone(out IEnumSTATPROPSTG ppenum)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                UnsafeNativeCompoundFileMethods.UnsafeNativeIEnumSTATPROPSTG enumSTATPROPSTG;

                _unsafeEnumSTATPROPSTG.Clone(out enumSTATPROPSTG);

                if (enumSTATPROPSTG != null)
                    ppenum = new SafeIEnumSTATPROPSTGImplementation(enumSTATPROPSTG);
                else
                    ppenum = null;
            }

            private UnsafeNativeCompoundFileMethods.UnsafeNativeIEnumSTATPROPSTG _unsafeEnumSTATPROPSTG;
        }
#endif 

        private class SafeIEnumSTATSTGImplementation : IEnumSTATSTG, IDisposable
        {
            internal SafeIEnumSTATSTGImplementation(UnsafeNativeCompoundFileMethods.UnsafeNativeIEnumSTATSTG enumSTATSTG)
            {
                _unsafeEnumSTATSTG = enumSTATSTG;
            }

            public void Dispose()
            {              
                SecurityHelper.DemandCompoundFileIOPermission();

                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Dispose(bool)
            /// </summary>
            /// <param name="disposing"></param>
            protected virtual void Dispose(bool disposing)
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                try
                {
                    if (disposing && (_unsafeEnumSTATSTG != null))
                    {
                        MS.Win32.UnsafeNativeMethods.SafeReleaseComObject((object) _unsafeEnumSTATSTG);
                    }
                }
                finally
                {
                    _unsafeEnumSTATSTG = null;
                }
            }

            void IEnumSTATSTG.Next(
                UInt32 celt,
                out System.Runtime.InteropServices.ComTypes.STATSTG rgelt, // This should really be array, but we're OK if we stick with one item at a time.
                    // Because marshalling an array of structs that have pointers to strings are troublesome.
                out UInt32 pceltFetched )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                if (celt != 1)
                {
                    throw new ArgumentException(SR.Get(SRID.InvalidArgumentValue, "celt", celt.ToString(CultureInfo.InvariantCulture)));
                }
                
                _unsafeEnumSTATSTG.Next(
                    celt,
                    out rgelt,
                    out pceltFetched );
            }

            void IEnumSTATSTG.Skip(
                UInt32 celt )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafeEnumSTATSTG.Skip(
                    celt );
            }

            void IEnumSTATSTG.Reset()
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                _unsafeEnumSTATSTG.Reset();
            }

            void IEnumSTATSTG.Clone(
                out IEnumSTATSTG ppenum )
            {
                SecurityHelper.DemandCompoundFileIOPermission();

                UnsafeNativeCompoundFileMethods.UnsafeNativeIEnumSTATSTG enumSTATSTG;

                _unsafeEnumSTATSTG.Clone(
                    out enumSTATSTG );

                if (enumSTATSTG != null)
                    ppenum = new SafeIEnumSTATSTGImplementation(enumSTATSTG);
                else
                    ppenum = null;
            }

            private UnsafeNativeCompoundFileMethods.UnsafeNativeIEnumSTATSTG _unsafeEnumSTATSTG;
        }
}
}

