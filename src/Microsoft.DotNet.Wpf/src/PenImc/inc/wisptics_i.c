// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




/* this ALWAYS GENERATED file contains the IIDs and CLSIDs */

/* link this file in with the server and any clients */


 /* File created by MIDL compiler version 7.00.0498 */
/* Compiler settings for wisptics.idl:
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

MIDL_DEFINE_GUID(IID, IID_ITabletManagerP,0x663C73A5,0x8715,0x4499,0xB8,0x09,0x43,0x68,0x9A,0x93,0x08,0x6B);


MIDL_DEFINE_GUID(IID, IID_ITabletManagerDrt,0xA56AB812,0x2AC7,0x443d,0xA8,0x7A,0xF1,0xEE,0x1C,0xD5,0xA0,0xE6);


MIDL_DEFINE_GUID(IID, IID_ITabletP,0xE65752FA,0x600B,0x43bd,0x8B,0xFE,0x6A,0x68,0x6F,0xA3,0xA2,0x01);


MIDL_DEFINE_GUID(IID, IID_ITabletP2,0xde5d1ed5,0x41d4,0x475d,0xbd,0xd8,0xea,0x74,0x96,0x77,0xb3,0xa1);


MIDL_DEFINE_GUID(IID, IID_ITabletContextP,0x22F74D0A,0x694F,0x4f47,0xA5,0xCE,0xAE,0x08,0xA6,0x40,0x9A,0xC8);


MIDL_DEFINE_GUID(IID, IID_ITabletCursorP,0x35DE0002,0x232C,0x4629,0xA9,0x15,0x7E,0x60,0x0E,0x80,0xCD,0x88);


MIDL_DEFINE_GUID(IID, IID_ITabletCursorButtonP,0xFCA502B0,0x5409,0x434d,0x8C,0x35,0xA9,0x6C,0x76,0xCC,0xA9,0x9C);


MIDL_DEFINE_GUID(IID, IID_ITabletEventSinkP,0x287A9E67,0x8D1D,0x4a65,0x8D,0xB4,0x51,0x91,0x53,0x95,0xD0,0x19);

#undef MIDL_DEFINE_GUID

#ifdef __cplusplus
}
#endif




