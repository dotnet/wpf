// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//  The COM and P/Invoke interop code necessary for the managed compound
//  file layer to call the existing APIs in OLE32.DLL.
//
//  The CF interfaces that can be used by the managed CF APis
//

using System;

using MS.Internal.Interop;

// For using PreserveSigAttribute Class.
using System.Runtime.InteropServices;

    
namespace MS.Internal.IO.Packaging.CompoundFile
{
    // Partial interface definition for existing IStorage
    internal interface IStorage
    {
        int CreateStream( 
            string pwcsName,
            int grfMode, 
            int reserved1, 
            int reserved2,
            out IStream ppstm );

        int OpenStream(
            string pwcsName,
            int reserved1,
            int grfMode,
            int reserved2,
            out IStream ppstm );

        int CreateStorage(
            string pwcsName,
            int grfMode,
            int reserved1,
            int reserved2,
            out IStorage ppstg );

        int OpenStorage(
            string pwcsName,
            IStorage pstgPriority,
            int grfMode,
            IntPtr snbExclude,// Not properly translated, must be NULL anyway
            int reserved,
            out IStorage ppstg );

        void CopyTo(
            int ciidExclude,
            Guid[] rgiidExclude,
            IntPtr snbExclude,// Not properly translated, use NULL to avoid blow-up
            IStorage ppstg );

        void MoveElementTo(
            string pwcsName,
            IStorage pstgDest,
            string pwcsNewName,
            int grfFlags );

        void Commit(
            int grfCommitFlags );

        void Revert();

        void EnumElements(
            int reserved1,
            IntPtr reserved2,
            int reserved3,
            out IEnumSTATSTG ppEnum );

        void DestroyElement(
            string pwcsName );

        void RenameElement(
            string pwcsOldName,
            string pwcsNewName );

        void SetElementTimes(
            string pwcsName,
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

    internal interface IStream
    {
        // ISequentialStream portion
        void Read(Byte[] pv, int cb, out int pcbRead);
        void Write(Byte[] pv, int cb, out int pcbWritten);

        // IStream portion
        void Seek(long dlibMove, int dwOrigin, out long plibNewPosition);
        void SetSize(long libNewSize);
        void CopyTo(IStream pstm, long cb, out long pcbRead, out long pcbWritten);
        void Commit(int grfCommitFlags);
        void Revert();
        void LockRegion(long libOffset, long cb, int dwLockType);
        void UnlockRegion(long libOffset, long cb, int dwLockType);
        void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg, int grfStatFlag);
        void Clone(out IStream ppstm);
    }

    internal interface IPropertySetStorage
    {
        void Create(
                ref Guid rfmtid,
                ref Guid pclsid,
                UInt32 grfFlags,
                UInt32 grfMode,
                out IPropertyStorage ppprstg
                );

        [PreserveSig]
        int Open(
                ref Guid rfmtid,
                UInt32 grfMode,
                out IPropertyStorage ppprstg
                );

        void Delete(
                ref Guid rfmtid
                );

        void Enum(
                out IEnumSTATPROPSETSTG ppenum
                );
    }

    internal interface IEnumSTATPROPSETSTG
    {
        //
        // The caller must allocate an array of celt STATPROPSETSTG structures
        // to receive the results.
        //
        // This method is PreserveSig because it can return a non-0 success
        // code; S_FALSE => fewer than celt elements were returned.
        //
        int
        Next(
            UInt32 celt,
            STATPROPSETSTG rgelt,
            out UInt32 pceltFetched
            );

        void Skip(UInt32 celt);

        void Reset();

        void Clone(out IEnumSTATPROPSETSTG ppenum);
    }

    internal interface IPropertyStorage
    {
        //
        // We preserve the HRESULT on this method because we need to distinguish
        // between S_OK (we got the properties we asked for) and S_FALSE (none of
        // the properties exist).
        //
        int ReadMultiple(
            UInt32 cpspec,
            PROPSPEC[] rgpspec,
            PROPVARIANT[] rgpropvar
            );

        void WriteMultiple(
            UInt32 cpspec,
            PROPSPEC[] rgpspec,
            PROPVARIANT[] rgpropvar,
            uint propidNameFirst
            );

        void DeleteMultiple(
            UInt32 cpspec,
            PROPSPEC[] rgpspec
            );

        void ReadPropertyNames(
            UInt32 cpropid,
            UInt32[] rgpropid,
            string[] rglpwstrName
            );

        void WritePropertyNames(
            UInt32 cpropid,
            UInt32[] rgpropid,
            string[] rglpwstrName
            );

        void DeletePropertyNames(
            UInt32 cpropid,
            UInt32[] rgpropid
            );

        void Commit(
            UInt32 grfCommitFlags
            );

        void Revert();

        void Enum(
            out IEnumSTATPROPSTG ppenum
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

    internal interface IEnumSTATPROPSTG
    {
        //
        // The caller must allocate an array of celt STATPROPSTG structures
        // to receive the results.
        //
        // This method is PreserveSig because it can return a non-0 success
        // code; S_FALSE => fewer than celt elements were returned.
        //
        int
        Next(
                UInt32 celt,
                STATPROPSTG rgelt,
                out UInt32 pceltFetched
                );

        void Skip(UInt32 celt);

        void Reset();

        void Clone(out IEnumSTATPROPSTG ppenum);
    }


    internal interface IEnumSTATSTG
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
            out IEnumSTATSTG ppenum);
    }
}

