// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




/* this ALWAYS GENERATED file contains the IIDs and CLSIDs */

/* link this file in with the server and any clients */


 /* File created by MIDL compiler version 7.00.0499 */
/* Compiler settings for tpcpen.idl:
    Oicf, W1, Zp8, env=Win32 (32b run)
    protocol : dce , ms_ext, c_ext, robust
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
//@@MIDL_FILE_HEADING(  )

#pragma warning( disable: 4049 )  /* more than 64k source lines */


#ifdef __cplusplus
extern "C"{
#endif 


#include <rpc.h>
#include <rpcndr.h>

#ifdef _MIDL_USE_GUIDDEF_

#ifndef INITGUID
#define INITGUID
#include <guiddef.h>
#undef INITGUID
#else
#include <guiddef.h>
#endif

#define MIDL_DEFINE_GUID(type,name,l,w1,w2,b1,b2,b3,b4,b5,b6,b7,b8) \
        DEFINE_GUID(name,l,w1,w2,b1,b2,b3,b4,b5,b6,b7,b8)

#else // !_MIDL_USE_GUIDDEF_

#ifndef __IID_DEFINED__
#define __IID_DEFINED__

typedef struct _IID
{
    unsigned long x;
    unsigned short s1;
    unsigned short s2;
    unsigned char  c[8];
} IID;

#endif // __IID_DEFINED__

#ifndef CLSID_DEFINED
#define CLSID_DEFINED
typedef IID CLSID;
#endif // CLSID_DEFINED

#define MIDL_DEFINE_GUID(type,name,l,w1,w2,b1,b2,b3,b4,b5,b6,b7,b8) \
        const type name = {l,w1,w2,{b1,b2,b3,b4,b5,b6,b7,b8}}

#endif !_MIDL_USE_GUIDDEF_

MIDL_DEFINE_GUID(IID, IID_ITabletEventSink,0x788459C8,0x26C8,0x4666,0xBF,0x57,0x04,0xAD,0x3A,0x0A,0x5E,0xB5);


MIDL_DEFINE_GUID(IID, IID_AsyncITabletEventSink,0xCDF7D7D6,0x2E5D,0x47c7,0x90,0xFC,0xC6,0x38,0xC7,0xFA,0x3F,0xC4);


MIDL_DEFINE_GUID(IID, IID_ITabletManager,0x764DE8AA,0x1867,0x47C1,0x8F,0x6A,0x12,0x24,0x45,0xAB,0xD8,0x9A);


MIDL_DEFINE_GUID(IID, IID_ITablet,0x1CB2EFC3,0xABC7,0x4172,0x8F,0xCB,0x3B,0xC9,0xCB,0x93,0xE2,0x9F);


MIDL_DEFINE_GUID(IID, IID_ITablet2,0xC247F616,0xBBEB,0x406A,0xAE,0xD3,0xF7,0x5E,0x65,0x65,0x99,0xAE);


MIDL_DEFINE_GUID(IID, IID_ITabletSettings,0x120ae7c9,0x36f7,0x4be6,0x93,0xda,0xe5,0xf2,0x66,0x84,0x7b,0x01);


MIDL_DEFINE_GUID(IID, IID_ITabletContext,0x45AAAF04,0x9D6F,0x41AE,0x8E,0xD1,0xEC,0xD6,0xD4,0xB2,0xF1,0x7F);


MIDL_DEFINE_GUID(IID, IID_ITabletCursor,0xEF9953C6,0xB472,0x4B02,0x9D,0x22,0xD0,0xE2,0x47,0xAD,0xE0,0xE8);


MIDL_DEFINE_GUID(IID, IID_ITabletCursorButton,0x997A992E,0x8B6C,0x4945,0xBC,0x17,0xA1,0xEE,0x56,0x3B,0x3A,0xB7);


MIDL_DEFINE_GUID(IID, LIBID_TABLETLib,0xC3F76406,0x6CA5,0x4BCD,0x85,0xE4,0x0E,0x7F,0x9E,0x05,0xD5,0x08);


MIDL_DEFINE_GUID(CLSID, CLSID_TabletManager,0x786CDB70,0x1628,0x44A0,0x85,0x3C,0x5D,0x34,0x0A,0x49,0x91,0x37);

#undef MIDL_DEFINE_GUID

#ifdef __cplusplus
}
#endif




