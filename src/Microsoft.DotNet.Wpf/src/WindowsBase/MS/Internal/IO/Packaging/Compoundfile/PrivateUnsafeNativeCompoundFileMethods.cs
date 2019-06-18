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
//  WARNING: This class should ONLY be access by SafeNativeCompoundFileMethods class
//      although this class is marked as "internal". This is only done because
//      TAS cannot be set on the individual member level if the entire class is
//      marked as SecurityCritical. This class should be treated as if it is a nested
//      private class of SafeNativeCompoundfileMethods.
//
//
//

//

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

using MS.Internal.Interop;    // For PROPSPEC and PROPVARIANT.
using System.Security;
using MS.Internal.WindowsBase;

using CultureInfo = System.Globalization.CultureInfo;

namespace MS.Internal.IO.Packaging.CompoundFile
{
    internal static class UnsafeNativeCompoundFileMethods
    {
        /////////////////////////////////////////////////////
        // Security Suppressed APIs
        /////////////////////////////////////////////////////

        [DllImport("ole32.dll")]
        internal static extern int StgCreateDocfileOnILockBytes(
            UnsafeNativeILockBytes plkbyt,
            int grfMode,
            int reserved, // Must be zero
            out UnsafeNativeIStorage ppstgOpen
            );
        
        [DllImport("ole32.dll")]
        internal static extern int StgOpenStorageOnILockBytes(
            UnsafeNativeILockBytes plkbyt,
            UnsafeNativeIStorage pStgPriority, // Most often NULL
            int grfMode,
            IntPtr snbExclude, // Pointer to SNB struct, not marshalled, must be null.
            int reserved, // Must be zero
            out UnsafeNativeIStorage ppstgOpen
            );

        [DllImport("ole32.dll")]
        internal static extern int StgCreateStorageEx(
            [In, MarshalAs( UnmanagedType.LPWStr )] string pwcsName,     //Pointer to path of compound file to create
            int grfMode,       // Specifies the access mode for opening the storage object
            int stgfmt,        // Specifies the storage file format, 5 is DocFile
            int grfAttrs,      // Reserved; must be zero
            IntPtr pStgOptions,// Pointer to STGOPTIONS, not marshalled, must use NULL.
            IntPtr reserved2,  // Reserved; must be null
            ref Guid riid,     // Specifies the GUID of the interface pointer
            out UnsafeNativeIStorage ppObjectOpen       //Pointer to an interface pointer
            );

        [DllImport("ole32.dll")]
        internal static extern int StgOpenStorageEx(
            [In, MarshalAs( UnmanagedType.LPWStr )] string pwcsName,     //Pointer to path of compound file to create
            int grfMode,       // Specifies the access mode for opening the storage object
            int stgfmt,        // Specifies the storage file format, 5 is DocFile
            int grfAttrs,      // Reserved; must be zero
            IntPtr pStgOptions,// Pointer to STGOPTIONS, not marshalled, must use NULL.
            IntPtr reserved2,  // Reserved; must be null
            ref Guid riid,     // Specifies the GUID of the interface pointer
            out UnsafeNativeIStorage ppObjectOpen       //Pointer to an interface pointer
            );    

        [DllImport("ole32.dll")]
        internal static extern int PropVariantClear(ref PROPVARIANT pvar);

        internal class UnsafeLockBytesOnStream : UnsafeNativeILockBytes, IDisposable
        {
            internal UnsafeLockBytesOnStream( Stream underlyingStream )
            {
                if( !underlyingStream.CanSeek )
                {
                    throw new NotSupportedException(
                        SR.Get(SRID.ILockBytesStreamMustSeek));
                }

                _baseStream = underlyingStream;
            }
            
            public void Dispose()
            {              
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Dispose(bool)
            /// </summary>
            /// <param name="disposing"></param>
            protected virtual void Dispose(bool disposing)
            {
                if (disposing && (_baseStream != null))
                {
                    // We only set the _baseStream to null without closing it,
                    // because _baseStream is a reference of an outside stream,
                    // and was set when this class was constructed. We didn't open 
                    // the stream and should leave the original owner of the stream
                    // to close it.
                    _baseStream = null;
                }
            }

            private void CheckDisposed()
            {
                if (_baseStream==null)
                {
                    throw new ObjectDisposedException(null, SR.Get(SRID.StreamObjectDisposed));
                }
            }

            void UnsafeNativeILockBytes.ReadAt (
                UInt64 offset,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] Byte[] pv,
                int cb,
                out int pcbRead)
            {
                CheckDisposed();
                checked { _baseStream.Seek( (long)offset, SeekOrigin.Begin ); }
                pcbRead = _baseStream.Read( pv, 0, cb );
            }

            void UnsafeNativeILockBytes.WriteAt(
                UInt64 offset,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] Byte[] pv,
                int cb,
                out int pcbWritten)
            {
                CheckDisposed();
                checked { _baseStream.Seek( (long)offset, SeekOrigin.Begin ); }
                _baseStream.Write( pv, 0, cb );

                // System.IO.Stream.Write does not return the number of bytes
                //  written.  Presumably this means an exception will be thrown
                //  if fewer than cb bytes are written.
                pcbWritten = cb;
            }


            void UnsafeNativeILockBytes.Flush()
            {
                CheckDisposed();
                _baseStream.Flush();
            }


            void UnsafeNativeILockBytes.SetSize( UInt64 cb )
            {
                CheckDisposed();
                checked { _baseStream.SetLength((long)cb); }
            }

            void UnsafeNativeILockBytes.LockRegion(
                UInt64 libOffset,
                UInt64 cb,
                int dwLockType )
            {
                throw new NotSupportedException();
            }


            void UnsafeNativeILockBytes.UnlockRegion(
                UInt64 libOffset,
                UInt64 cb,
                int dwLockType )
            {
                throw new NotSupportedException();
            }


            void UnsafeNativeILockBytes.Stat(
                out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg,
                int grfStatFlag )
            {
                CheckDisposed();

                if ((grfStatFlag & ~(SafeNativeCompoundFileConstants.STATFLAG_NONAME | 
                                     SafeNativeCompoundFileConstants.STATFLAG_NOOPEN  )) != 0)
                {
                    // validate grfStatFlag's value
                    throw new ArgumentException(SR.Get(SRID.InvalidArgumentValue, "grfStatFlag", grfStatFlag.ToString(CultureInfo.InvariantCulture)));
                }

                System.Runtime.InteropServices.ComTypes.STATSTG returnValue = new System.Runtime.InteropServices.ComTypes.STATSTG();
                
                returnValue.grfLocksSupported = 0 ; // No lock supported

                returnValue.cbSize = _baseStream.Length;
                returnValue.type = SafeNativeCompoundFileConstants.STGTY_LOCKBYTES;

                pstatstg = returnValue;
            }

            private Stream _baseStream;
        }

        /////////////////////////////////////////////////////
        // Security Suppressed Private Interfaces
        /////////////////////////////////////////////////////
        
        [Guid("0000000a-0000-0000-C000-000000000046")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        [ComImport]
        internal interface UnsafeNativeILockBytes
        {
            void ReadAt (
                UInt64 offset,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] Byte[] pv,
                int cb,
                out int pcbRead);
            void WriteAt(
                UInt64 offset,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] Byte[] pv,
                int cb,
                out int pcbWritten);
            void Flush();
            void SetSize( UInt64 cb );
            void LockRegion(
                UInt64 libOffset,
                UInt64 cb,
                int dwLockType );
            void UnlockRegion(
                UInt64 libOffset,
                UInt64 cb,
                int dwLockType );
            void Stat(
                out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg,
                int grfStatFlag );
        }

        // Partial interface definition for existing IStorage
        [Guid("0000000b-0000-0000-C000-000000000046")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        [ComImport]
        internal interface UnsafeNativeIStorage
        {
            [PreserveSig]
            int CreateStream( 
                [In, MarshalAs( UnmanagedType.LPWStr )] string pwcsName,
                int grfMode, 
                int reserved1, 
                int reserved2,
                out UnsafeNativeIStream ppstm );
            [PreserveSig]
            int OpenStream(
                [In, MarshalAs( UnmanagedType.LPWStr )] string pwcsName,
                int reserved1,
                int grfMode,
                int reserved2,
                out UnsafeNativeIStream ppstm );
            [PreserveSig]
            int CreateStorage(
                [In, MarshalAs( UnmanagedType.LPWStr )] string pwcsName,
                int grfMode,
                int reserved1,
                int reserved2,
                out UnsafeNativeIStorage ppstg );
            [PreserveSig]
            int OpenStorage(
                [In, MarshalAs( UnmanagedType.LPWStr )] string pwcsName,
                UnsafeNativeIStorage pstgPriority,
                int grfMode,
                IntPtr snbExclude,// Not properly translated, must be NULL anyway
                int reserved,
                out UnsafeNativeIStorage ppstg );
            void CopyTo(
                int ciidExclude,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)] Guid[] rgiidExclude,
                IntPtr snbExclude,// Not properly translated, use NULL to avoid `blow-up
                UnsafeNativeIStorage ppstg );
            void MoveElementTo(
                [In, MarshalAs( UnmanagedType.LPWStr )] string pwcsName,
                UnsafeNativeIStorage pstgDest,
                [In, MarshalAs( UnmanagedType.LPWStr )] string pwcsNewName,
                int grfFlags );
            void Commit(
                int grfCommitFlags );
            void Revert();
            void EnumElements(
                int reserved1,
                IntPtr reserved2,
                int reserved3,
                out UnsafeNativeIEnumSTATSTG ppEnum );
            void DestroyElement(
                [In, MarshalAs( UnmanagedType.LPWStr )] string pwcsName );
            void RenameElement(
                [In, MarshalAs( UnmanagedType.LPWStr )] string pwcsOldName,
                [In, MarshalAs( UnmanagedType.LPWStr )] string pwcsNewName );
            void SetElementTimes(
                [In, MarshalAs( UnmanagedType.LPWStr )] string pwcsName,
                System.Runtime.InteropServices.ComTypes.FILETIME pctime,
                System.Runtime.InteropServices.ComTypes.FILETIME patime,
                System.Runtime.InteropServices.ComTypes.FILETIME pmtime );
            void SetClass(
                ref Guid clsid ); // Hopefully "ref" is how I tell it to use a pointer 
            void SetStateBits(
                int grfStateBits,
                int grfMask );
            void Stat(
                out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg,
                int grfStatFlag );
        }

        [Guid("0000000c-0000-0000-C000-000000000046")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        [ComImport]
        internal interface UnsafeNativeIStream
        {
            // ISequentialStream portion
            void Read([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] Byte[] pv, int cb, out int pcbRead);
            void Write([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Byte[] pv, int cb, out int pcbWritten);

            // IStream portion
            void Seek(long dlibMove, int dwOrigin, out long plibNewPosition);
            void SetSize(long libNewSize);
            void CopyTo(UnsafeNativeIStream pstm, long cb, out long pcbRead, out long pcbWritten);
            void Commit(int grfCommitFlags);
            void Revert();
            void LockRegion(long libOffset, long cb, int dwLockType);
            void UnlockRegion(long libOffset, long cb, int dwLockType);
            void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg, int grfStatFlag);
            void Clone(out UnsafeNativeIStream ppstm);
        }

        [ComImport]
        [Guid("0000013A-0000-0000-C000-000000000046")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface UnsafeNativeIPropertySetStorage
        {
            void Create(
                    ref Guid rfmtid,
                    ref Guid pclsid,
                    UInt32 grfFlags,
                    UInt32 grfMode,
                    out UnsafeNativeIPropertyStorage ppprstg
                    );

            [PreserveSig]
            int Open(
                    ref Guid rfmtid,
                    UInt32 grfMode,
                    out UnsafeNativeIPropertyStorage ppprstg
                    );

            void Delete(
                    ref Guid rfmtid
                    );

            void Enum(
                    out UnsafeNativeIEnumSTATPROPSETSTG ppenum
                    );
        }

        [ComImport]
        [Guid("0000013B-0000-0000-C000-000000000046")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface UnsafeNativeIEnumSTATPROPSETSTG
        {
            //
            // The caller must allocate an array of celt STATPROPSETSTG structures
            // to receive the results.
            //
            // This method is PreserveSig because it can return a non-0 success
            // code; S_FALSE => fewer than celt elements were returned.
            //
            [PreserveSig]
            int
            Next(
                UInt32 celt,
                [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)]
                STATPROPSETSTG rgelt,
                out UInt32 pceltFetched
                );

            void Skip(UInt32 celt);

            void Reset();

            void Clone(out UnsafeNativeIEnumSTATPROPSETSTG ppenum);
        }

        [ComImport]
        [Guid("00000138-0000-0000-C000-000000000046")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface UnsafeNativeIPropertyStorage
        {
            //
            // We preserve the HRESULT on this method because we need to distinguish
            // between S_OK (we got the properties we asked for) and S_FALSE (none of
            // the properties exist).
            //
            [PreserveSig]
            int
            ReadMultiple(
                UInt32 cpspec,
                [In,  MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)]
                PROPSPEC[] rgpspec,
                [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)]
                PROPVARIANT[] rgpropvar
                );

            void WriteMultiple(
                UInt32 cpspec,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)]
                PROPSPEC[] rgpspec,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)]
                PROPVARIANT[] rgpropvar,
                uint propidNameFirst
                );

            void DeleteMultiple(
                UInt32 cpspec,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)]
                PROPSPEC[] rgpspec
                );

            void ReadPropertyNames(
                UInt32 cpropid,
                [In,  MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)]
                UInt32[] rgpropid,
                [Out, MarshalAs(UnmanagedType.LPArray,
                                ArraySubType=UnmanagedType.LPWStr,
                                SizeParamIndex=0)]
                string[] rglpwstrName
                );

            void WritePropertyNames(
                UInt32 cpropid,
                [In,  MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)]
                UInt32[] rgpropid,
                [In,  MarshalAs(UnmanagedType.LPArray,
                                ArraySubType=UnmanagedType.LPWStr,
                                SizeParamIndex=0)]
                string[] rglpwstrName
                );

            void DeletePropertyNames(
                UInt32 cpropid,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)]
                UInt32[] rgpropid
                );

            void Commit(
                UInt32 grfCommitFlags
                );

            void Revert();

            void Enum(
                out UnsafeNativeIEnumSTATPROPSTG ppenum
                );

            void SetTimes(
                ref System.Runtime.InteropServices.ComTypes.FILETIME pctime,
                ref System.Runtime.InteropServices.ComTypes.FILETIME patime,
                ref System.Runtime.InteropServices.ComTypes.FILETIME pmtime
                );

            void SetClass(
                ref Guid clsid
                );

            void Stat(
                out STATPROPSETSTG pstatpsstg
                );
        }

        [ComImport]
        [Guid("00000139-0000-0000-C000-000000000046")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface UnsafeNativeIEnumSTATPROPSTG
            {
                //
                // The caller must allocate an array of celt STATPROPSTG structures
                // to receive the results.
                //
                // This method is PreserveSig because it can return a non-0 success
                // code; S_FALSE => fewer than celt elements were returned.
                //
            [PreserveSig]
                int
            Next(
                    UInt32 celt,
                    [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)]
                    STATPROPSTG rgelt,
                    out UInt32 pceltFetched
                    );

            void Skip(UInt32 celt);

            void Reset();

            void Clone(out UnsafeNativeIEnumSTATPROPSTG ppenum);
        }


        [Guid("0000000d-0000-0000-C000-000000000046")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        [ComImport]
        internal interface UnsafeNativeIEnumSTATSTG
        {
            void Next(
                UInt32 celt,
                out System.Runtime.InteropServices.ComTypes.STATSTG rgelt, // This should really be array, but we're OK if we stick with one item at a time.
                    // Because marshalling an array of structs that have pointers to strings are troublesome.
                out UInt32 pceltFetched );
            void Skip(
                UInt32 celt );
            void Reset();
            void Clone(
                out UnsafeNativeIEnumSTATSTG ppenum );
        }
    }
}
