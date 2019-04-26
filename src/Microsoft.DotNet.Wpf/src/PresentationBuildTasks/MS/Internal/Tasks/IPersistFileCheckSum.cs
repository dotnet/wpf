// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
// Description:
//      Managed declaration for IPersistFileCheckSum used for
//      VS project system for MarkupCompile tasks.
//
//  ***********************IMPORTANT**************************
//
//      The managed side declaration of this interface should match with 
//      the native side declaration which lives in VS.NET project tree.
//
//---------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Security;


namespace MS.Internal
{
    //MIDL_INTERFACE("35355DA7-3EEA-452e-89F3-68344278F806")
    // IPersistFileCheckSum : public IUnknown
    // {
    // public:
    //    virtual HRESULT STDMETHODCALLTYPE CalculateCheckSum( 
    //        /* [in] */ __RPC__in REFGUID guidCheckSumAlgorithm,
    //        /* [in] */ DWORD cbBufferSize,
    //        /* [size_is][out] */ __RPC__out_ecount_full(cbBufferSize) BYTE *pbHash,
    //        /* [out] */ __RPC__out DWORD *pcbActualSize) = 0;
    // };

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("35355DA7-3EEA-452e-89F3-68344278F806")]
    internal interface IPersistFileCheckSum
    {
        void CalculateCheckSum( [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidCheckSumAlgorithm,
                                [In, MarshalAs(UnmanagedType.U4)]       int cbBufferSize,
                                [Out, MarshalAs(UnmanagedType.LPArray,
                                                     SizeParamIndex=1)] byte[] Hash,
                                [Out, MarshalAs(UnmanagedType.U4)]      out int ActualSize);
    }
}
