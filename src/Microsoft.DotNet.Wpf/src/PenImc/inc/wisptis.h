// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 7.00.0498 */
/* Compiler settings for wisptis.idl:
    Oicf, W1, Zp8, env=Win32 (32b run)
    protocol : dce , ms_ext, c_ext, robust
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
//@@MIDL_FILE_HEADING(  )

#pragma warning( disable: 4049 )  /* more than 64k source lines */


/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 500
#endif

/* verify that the <rpcsal.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCSAL_H_VERSION__
#define __REQUIRED_RPCSAL_H_VERSION__ 100
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __RPCNDR_H_VERSION__
#error this stub requires an updated version of <rpcndr.h>
#endif // __RPCNDR_H_VERSION__


#ifndef __wisptis_h__
#define __wisptis_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __TabletManagerS_FWD_DEFINED__
#define __TabletManagerS_FWD_DEFINED__

#ifdef __cplusplus
typedef class TabletManagerS TabletManagerS;
#else
typedef struct TabletManagerS TabletManagerS;
#endif /* __cplusplus */

#endif 	/* __TabletManagerS_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"
#include "tpcpen.h"

#ifdef __cplusplus
extern "C"{
#endif 



#ifndef __WISPTISLib_LIBRARY_DEFINED__
#define __WISPTISLib_LIBRARY_DEFINED__

/* library WISPTISLib */
/* [helpstring][version][uuid] */ 








EXTERN_C const IID LIBID_WISPTISLib;

EXTERN_C const CLSID CLSID_TabletManagerS;

#ifdef __cplusplus

class DECLSPEC_UUID("A5B020FD-E04B-4e67-B65A-E7DEED25B2CF")
TabletManagerS;
#endif
#endif /* __WISPTISLib_LIBRARY_DEFINED__ */

/* interface __MIDL_itf_wisptis_0000_0000 */
/* [local] */ 

#define SZ_REGKEY_PROFILE TEXT("Software\\Microsoft\\Wisp\\Pen\\Profile")


extern RPC_IF_HANDLE __MIDL_itf_wisptis_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_wisptis_0000_0000_v0_0_s_ifspec;

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif



