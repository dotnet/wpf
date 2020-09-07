// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




/* this ALWAYS GENERATED file contains the definitions for the interfaces */


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

#ifndef COM_NO_WINDOWS_H
#include "windows.h"
#include "ole2.h"
#endif /*COM_NO_WINDOWS_H*/

#ifndef __wisptics_h__
#define __wisptics_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __ITabletManagerP_FWD_DEFINED__
#define __ITabletManagerP_FWD_DEFINED__
typedef interface ITabletManagerP ITabletManagerP;
#endif 	/* __ITabletManagerP_FWD_DEFINED__ */


#ifndef __ITabletManagerDrt_FWD_DEFINED__
#define __ITabletManagerDrt_FWD_DEFINED__
typedef interface ITabletManagerDrt ITabletManagerDrt;
#endif 	/* __ITabletManagerDrt_FWD_DEFINED__ */


#ifndef __ITabletP_FWD_DEFINED__
#define __ITabletP_FWD_DEFINED__
typedef interface ITabletP ITabletP;
#endif 	/* __ITabletP_FWD_DEFINED__ */


#ifndef __ITabletP2_FWD_DEFINED__
#define __ITabletP2_FWD_DEFINED__
typedef interface ITabletP2 ITabletP2;
#endif 	/* __ITabletP2_FWD_DEFINED__ */


#ifndef __ITabletContextP_FWD_DEFINED__
#define __ITabletContextP_FWD_DEFINED__
typedef interface ITabletContextP ITabletContextP;
#endif 	/* __ITabletContextP_FWD_DEFINED__ */


#ifndef __ITabletCursorP_FWD_DEFINED__
#define __ITabletCursorP_FWD_DEFINED__
typedef interface ITabletCursorP ITabletCursorP;
#endif 	/* __ITabletCursorP_FWD_DEFINED__ */


#ifndef __ITabletCursorButtonP_FWD_DEFINED__
#define __ITabletCursorButtonP_FWD_DEFINED__
typedef interface ITabletCursorButtonP ITabletCursorButtonP;
#endif 	/* __ITabletCursorButtonP_FWD_DEFINED__ */


#ifndef __ITabletEventSinkP_FWD_DEFINED__
#define __ITabletEventSinkP_FWD_DEFINED__
typedef interface ITabletEventSinkP ITabletEventSinkP;
#endif 	/* __ITabletEventSinkP_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"
#include "tpcpen.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_wisptics_0000_0000 */
/* [local] */ 

#pragma once








extern RPC_IF_HANDLE __MIDL_itf_wisptics_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_wisptics_0000_0000_v0_0_s_ifspec;

#ifndef __ITabletManagerP_INTERFACE_DEFINED__
#define __ITabletManagerP_INTERFACE_DEFINED__

/* interface ITabletManagerP */
/* [unique][helpstring][uuid][object] */ 

typedef ITabletManagerP *PTABLETMANAGERP;


EXTERN_C const IID IID_ITabletManagerP;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("663C73A5-8715-4499-B809-43689A93086B")
    ITabletManagerP : public ITabletManager
    {
    public:
    };
    
#else 	/* C style interface */

    typedef struct ITabletManagerPVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITabletManagerP * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITabletManagerP * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITabletManagerP * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetDefaultTablet )( 
            ITabletManagerP * This,
            /* [out] */ __RPC__deref_out_opt ITablet **ppTablet);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetTabletCount )( 
            ITabletManagerP * This,
            /* [out] */ __RPC__out ULONG *pcTablets);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetTablet )( 
            ITabletManagerP * This,
            /* [in] */ ULONG iTablet,
            /* [out] */ __RPC__deref_out_opt ITablet **ppTablet);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetTabletContextById )( 
            ITabletManagerP * This,
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [out] */ __RPC__deref_out_opt ITabletContext **ppContext);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetCursorById )( 
            ITabletManagerP * This,
            /* [in] */ CURSOR_ID cid,
            /* [out] */ __RPC__deref_out_opt ITabletCursor **ppCursor);
        
        END_INTERFACE
    } ITabletManagerPVtbl;

    interface ITabletManagerP
    {
        CONST_VTBL struct ITabletManagerPVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITabletManagerP_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITabletManagerP_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITabletManagerP_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITabletManagerP_GetDefaultTablet(This,ppTablet)	\
    ( (This)->lpVtbl -> GetDefaultTablet(This,ppTablet) ) 

#define ITabletManagerP_GetTabletCount(This,pcTablets)	\
    ( (This)->lpVtbl -> GetTabletCount(This,pcTablets) ) 

#define ITabletManagerP_GetTablet(This,iTablet,ppTablet)	\
    ( (This)->lpVtbl -> GetTablet(This,iTablet,ppTablet) ) 

#define ITabletManagerP_GetTabletContextById(This,tcid,ppContext)	\
    ( (This)->lpVtbl -> GetTabletContextById(This,tcid,ppContext) ) 

#define ITabletManagerP_GetCursorById(This,cid,ppCursor)	\
    ( (This)->lpVtbl -> GetCursorById(This,cid,ppCursor) ) 


#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITabletManagerP_INTERFACE_DEFINED__ */


#ifndef __ITabletManagerDrt_INTERFACE_DEFINED__
#define __ITabletManagerDrt_INTERFACE_DEFINED__

/* interface ITabletManagerDrt */
/* [unique][helpstring][uuid][object] */ 


EXTERN_C const IID IID_ITabletManagerDrt;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("A56AB812-2AC7-443d-A87A-F1EE1CD5A0E6")
    ITabletManagerDrt : public IUnknown
    {
    public:
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE IsTabletPresent( 
            __RPC__in BSTR bstrTablet,
            /* [out] */ __RPC__out BOOL *pfPresent) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE SimulatePacket( 
            __RPC__in BSTR bstrTablet,
            LONG x,
            LONG y,
            BOOL fCursorDown) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE EnablePacketsTransfer( 
            BOOL fEnable) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE SimulateCursorInRange( 
            DWORD cursorKey) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE SimulateCursorOutOfRange( 
            DWORD cursorKey) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetTabletRectangle( 
            __RPC__in BSTR bstrTablet,
            /* [out] */ __RPC__out RECT *prc) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE FindTablet( 
            __RPC__in BSTR bstrTablet,
            /* [out] */ __RPC__out ULONG *piTablet) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE SimulatePacketWithButton( 
            __RPC__in BSTR bstrTablet,
            LONG x,
            LONG y,
            BOOL fCursorDown,
            BOOL fBarrelButton) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE SimulateCursorInRangeForTablet( 
            __RPC__in BSTR bstrTablet,
            DWORD cursorKey) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE SimulateCursorOutOfRangeForTablet( 
            __RPC__in BSTR bstrTablet,
            DWORD cursorKey) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE EnsureTablet( 
            __RPC__in BSTR bstrTablet) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ITabletManagerDrtVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITabletManagerDrt * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITabletManagerDrt * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITabletManagerDrt * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *IsTabletPresent )( 
            ITabletManagerDrt * This,
            __RPC__in BSTR bstrTablet,
            /* [out] */ __RPC__out BOOL *pfPresent);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *SimulatePacket )( 
            ITabletManagerDrt * This,
            __RPC__in BSTR bstrTablet,
            LONG x,
            LONG y,
            BOOL fCursorDown);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *EnablePacketsTransfer )( 
            ITabletManagerDrt * This,
            BOOL fEnable);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *SimulateCursorInRange )( 
            ITabletManagerDrt * This,
            DWORD cursorKey);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *SimulateCursorOutOfRange )( 
            ITabletManagerDrt * This,
            DWORD cursorKey);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetTabletRectangle )( 
            ITabletManagerDrt * This,
            __RPC__in BSTR bstrTablet,
            /* [out] */ __RPC__out RECT *prc);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *FindTablet )( 
            ITabletManagerDrt * This,
            __RPC__in BSTR bstrTablet,
            /* [out] */ __RPC__out ULONG *piTablet);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *SimulatePacketWithButton )( 
            ITabletManagerDrt * This,
            __RPC__in BSTR bstrTablet,
            LONG x,
            LONG y,
            BOOL fCursorDown,
            BOOL fBarrelButton);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *SimulateCursorInRangeForTablet )( 
            ITabletManagerDrt * This,
            __RPC__in BSTR bstrTablet,
            DWORD cursorKey);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *SimulateCursorOutOfRangeForTablet )( 
            ITabletManagerDrt * This,
            __RPC__in BSTR bstrTablet,
            DWORD cursorKey);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *EnsureTablet )( 
            ITabletManagerDrt * This,
            __RPC__in BSTR bstrTablet);
        
        END_INTERFACE
    } ITabletManagerDrtVtbl;

    interface ITabletManagerDrt
    {
        CONST_VTBL struct ITabletManagerDrtVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITabletManagerDrt_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITabletManagerDrt_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITabletManagerDrt_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITabletManagerDrt_IsTabletPresent(This,bstrTablet,pfPresent)	\
    ( (This)->lpVtbl -> IsTabletPresent(This,bstrTablet,pfPresent) ) 

#define ITabletManagerDrt_SimulatePacket(This,bstrTablet,x,y,fCursorDown)	\
    ( (This)->lpVtbl -> SimulatePacket(This,bstrTablet,x,y,fCursorDown) ) 

#define ITabletManagerDrt_EnablePacketsTransfer(This,fEnable)	\
    ( (This)->lpVtbl -> EnablePacketsTransfer(This,fEnable) ) 

#define ITabletManagerDrt_SimulateCursorInRange(This,cursorKey)	\
    ( (This)->lpVtbl -> SimulateCursorInRange(This,cursorKey) ) 

#define ITabletManagerDrt_SimulateCursorOutOfRange(This,cursorKey)	\
    ( (This)->lpVtbl -> SimulateCursorOutOfRange(This,cursorKey) ) 

#define ITabletManagerDrt_GetTabletRectangle(This,bstrTablet,prc)	\
    ( (This)->lpVtbl -> GetTabletRectangle(This,bstrTablet,prc) ) 

#define ITabletManagerDrt_FindTablet(This,bstrTablet,piTablet)	\
    ( (This)->lpVtbl -> FindTablet(This,bstrTablet,piTablet) ) 

#define ITabletManagerDrt_SimulatePacketWithButton(This,bstrTablet,x,y,fCursorDown,fBarrelButton)	\
    ( (This)->lpVtbl -> SimulatePacketWithButton(This,bstrTablet,x,y,fCursorDown,fBarrelButton) ) 

#define ITabletManagerDrt_SimulateCursorInRangeForTablet(This,bstrTablet,cursorKey)	\
    ( (This)->lpVtbl -> SimulateCursorInRangeForTablet(This,bstrTablet,cursorKey) ) 

#define ITabletManagerDrt_SimulateCursorOutOfRangeForTablet(This,bstrTablet,cursorKey)	\
    ( (This)->lpVtbl -> SimulateCursorOutOfRangeForTablet(This,bstrTablet,cursorKey) ) 

#define ITabletManagerDrt_EnsureTablet(This,bstrTablet)	\
    ( (This)->lpVtbl -> EnsureTablet(This,bstrTablet) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITabletManagerDrt_INTERFACE_DEFINED__ */


#ifndef __ITabletP_INTERFACE_DEFINED__
#define __ITabletP_INTERFACE_DEFINED__

/* interface ITabletP */
/* [unique][helpstring][uuid][object] */ 

typedef ITabletP *PTABLETP;


EXTERN_C const IID IID_ITabletP;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("E65752FA-600B-43bd-8BFE-6A686FA3A201")
    ITabletP : public ITablet
    {
    public:
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetId( 
            /* [out] */ __RPC__out DWORD *pId) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ITabletPVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITabletP * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITabletP * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITabletP * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetDefaultContextSettings )( 
            ITabletP * This,
            /* [out] */ __RPC__deref_out_opt TABLET_CONTEXT_SETTINGS **ppTCS);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *CreateContext )( 
            ITabletP * This,
            /* [in] */ __RPC__in HWND hWnd,
            /* [unique][in] */ __RPC__in_opt RECT *prcInput,
            /* [in] */ DWORD dwOptions,
            /* [unique][in] */ __RPC__in_opt TABLET_CONTEXT_SETTINGS *pTCS,
            /* [in] */ CONTEXT_ENABLE_TYPE cet,
            /* [out] */ __RPC__deref_out_opt ITabletContext **ppCtx,
            /* [unique][out][in] */ __RPC__inout_opt TABLET_CONTEXT_ID *pTcid,
            /* [unique][out][in] */ __RPC__deref_opt_inout_opt PACKET_DESCRIPTION **ppPD,
            /* [unique][in] */ __RPC__in_opt ITabletEventSink *pSink);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetName )( 
            ITabletP * This,
            /* [out] */ __RPC__deref_out_opt LPWSTR *ppwszName);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetMaxInputRect )( 
            ITabletP * This,
            /* [out] */ __RPC__out RECT *prcInput);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetHardwareCaps )( 
            ITabletP * This,
            /* [out] */ __RPC__out DWORD *pdwCaps);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetPropertyMetrics )( 
            ITabletP * This,
            /* [in] */ __RPC__in REFGUID rguid,
            /* [out] */ __RPC__out PROPERTY_METRICS *pPM);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetPlugAndPlayId )( 
            ITabletP * This,
            /* [out] */ __RPC__deref_out_opt LPWSTR *ppwszPPId);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetCursorCount )( 
            ITabletP * This,
            /* [out] */ __RPC__out ULONG *pcCurs);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetCursor )( 
            ITabletP * This,
            /* [in] */ ULONG iCur,
            /* [out] */ __RPC__deref_out_opt ITabletCursor **ppCur);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetId )( 
            ITabletP * This,
            /* [out] */ __RPC__out DWORD *pId);
        
        END_INTERFACE
    } ITabletPVtbl;

    interface ITabletP
    {
        CONST_VTBL struct ITabletPVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITabletP_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITabletP_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITabletP_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITabletP_GetDefaultContextSettings(This,ppTCS)	\
    ( (This)->lpVtbl -> GetDefaultContextSettings(This,ppTCS) ) 

#define ITabletP_CreateContext(This,hWnd,prcInput,dwOptions,pTCS,cet,ppCtx,pTcid,ppPD,pSink)	\
    ( (This)->lpVtbl -> CreateContext(This,hWnd,prcInput,dwOptions,pTCS,cet,ppCtx,pTcid,ppPD,pSink) ) 

#define ITabletP_GetName(This,ppwszName)	\
    ( (This)->lpVtbl -> GetName(This,ppwszName) ) 

#define ITabletP_GetMaxInputRect(This,prcInput)	\
    ( (This)->lpVtbl -> GetMaxInputRect(This,prcInput) ) 

#define ITabletP_GetHardwareCaps(This,pdwCaps)	\
    ( (This)->lpVtbl -> GetHardwareCaps(This,pdwCaps) ) 

#define ITabletP_GetPropertyMetrics(This,rguid,pPM)	\
    ( (This)->lpVtbl -> GetPropertyMetrics(This,rguid,pPM) ) 

#define ITabletP_GetPlugAndPlayId(This,ppwszPPId)	\
    ( (This)->lpVtbl -> GetPlugAndPlayId(This,ppwszPPId) ) 

#define ITabletP_GetCursorCount(This,pcCurs)	\
    ( (This)->lpVtbl -> GetCursorCount(This,pcCurs) ) 

#define ITabletP_GetCursor(This,iCur,ppCur)	\
    ( (This)->lpVtbl -> GetCursor(This,iCur,ppCur) ) 


#define ITabletP_GetId(This,pId)	\
    ( (This)->lpVtbl -> GetId(This,pId) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITabletP_INTERFACE_DEFINED__ */


#ifndef __ITabletP2_INTERFACE_DEFINED__
#define __ITabletP2_INTERFACE_DEFINED__

/* interface ITabletP2 */
/* [unique][helpstring][uuid][object] */ 

typedef ITabletP2 *PTABLETP2;


EXTERN_C const IID IID_ITabletP2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("de5d1ed5-41d4-475d-bdd8-ea749677b3a1")
    ITabletP2 : public ITablet2
    {
    public:
    };
    
#else 	/* C style interface */

    typedef struct ITabletP2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITabletP2 * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITabletP2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITabletP2 * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetDeviceKind )( 
            ITabletP2 * This,
            /* [out] */ __RPC__out TABLET_DEVICE_KIND *pKind);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetMatchingScreenRect )( 
            ITabletP2 * This,
            /* [out] */ __RPC__out RECT *prcInput);
        
        END_INTERFACE
    } ITabletP2Vtbl;

    interface ITabletP2
    {
        CONST_VTBL struct ITabletP2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITabletP2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITabletP2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITabletP2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITabletP2_GetDeviceKind(This,pKind)	\
    ( (This)->lpVtbl -> GetDeviceKind(This,pKind) ) 

#define ITabletP2_GetMatchingScreenRect(This,prcInput)	\
    ( (This)->lpVtbl -> GetMatchingScreenRect(This,prcInput) ) 


#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITabletP2_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_wisptics_0000_0004 */
/* [local] */ 

typedef 
enum _CONTEXT_TYPE
    {	WINTAB	= 1,
	HID	= ( WINTAB + 1 ) ,
	MOUSE	= ( HID + 1 ) 
    } 	CONTEXT_TYPE;



extern RPC_IF_HANDLE __MIDL_itf_wisptics_0000_0004_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_wisptics_0000_0004_v0_0_s_ifspec;

#ifndef __ITabletContextP_INTERFACE_DEFINED__
#define __ITabletContextP_INTERFACE_DEFINED__

/* interface ITabletContextP */
/* [unique][helpstring][uuid][object] */ 

typedef ITabletContextP *PTABLETCONTEXTP;


EXTERN_C const IID IID_ITabletContextP;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("22F74D0A-694F-4f47-A5CE-AE08A6409AC8")
    ITabletContextP : public ITabletContext
    {
    public:
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Overlap( 
            /* [in] */ BOOL bTop,
            /* [out] */ __RPC__out DWORD *pdwtcid) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetType( 
            /* [out] */ __RPC__out CONTEXT_TYPE *pct) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE TrackInputRect( 
            /* [out] */ __RPC__out RECT *prcInput) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE IsTopMostHook( void) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetEventSink( 
            /* [out] */ __RPC__deref_out_opt ITabletEventSink **ppSink) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE UseSharedMemoryCommunications( 
            /* [in] */ DWORD pid,
            /* [out] */ __RPC__out DWORD *phEventMoreData,
            /* [out] */ __RPC__out DWORD *phEventClientReady,
            /* [out] */ __RPC__out DWORD *phMutexAccess,
            /* [out] */ __RPC__out DWORD *phFileMapping) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE UseNamedSharedMemoryCommunications( 
            /* [in] */ DWORD pid,
            /* [string][in] */ __RPC__in LPCTSTR szSid,
            /* [string][in] */ __RPC__in LPCTSTR sdIlSid,
            /* [out] */ __RPC__out DWORD *pdwEventMoreDataId,
            /* [out] */ __RPC__out DWORD *pdwEventClientReadyId,
            /* [out] */ __RPC__out DWORD *pdwMutexAccessId,
            /* [out] */ __RPC__out DWORD *pdwFileMappingId) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ITabletContextPVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITabletContextP * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITabletContextP * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITabletContextP * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetId )( 
            ITabletContextP * This,
            /* [out] */ __RPC__out TABLET_CONTEXT_ID *pTcid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetWindow )( 
            ITabletContextP * This,
            /* [out] */ __RPC__deref_out_opt HWND *pHwnd);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetSettings )( 
            ITabletContextP * This,
            /* [out] */ __RPC__deref_out_opt TABLET_CONTEXT_SETTINGS **ppTCS);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetTablet )( 
            ITabletContextP * This,
            /* [out] */ __RPC__deref_out_opt ITablet **ppTablet);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Enable )( 
            ITabletContextP * This,
            /* [in] */ CONTEXT_ENABLE_TYPE cet);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetOptions )( 
            ITabletContextP * This,
            /* [out] */ __RPC__out DWORD *pdwOptions);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetPacketDescription )( 
            ITabletContextP * This,
            /* [out] */ __RPC__deref_out_opt PACKET_DESCRIPTION **ppPD);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetStatus )( 
            ITabletContextP * This,
            /* [out] */ __RPC__out DWORD *pdwStatus);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetInputRect )( 
            ITabletContextP * This,
            /* [out] */ __RPC__out RECT *prcInput);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *SetInputRect )( 
            ITabletContextP * This,
            /* [unique][in] */ __RPC__in_opt RECT *prcInput);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *SetDevInputRect )( 
            ITabletContextP * This,
            /* [unique][in] */ __RPC__in_opt RECT *prcInput);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetDevInputRect )( 
            ITabletContextP * This,
            /* [out] */ __RPC__out RECT *prcInput);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *SetCapture )( 
            ITabletContextP * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *ReleaseCapture )( 
            ITabletContextP * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *SetCursorCapture )( 
            ITabletContextP * This,
            /* [in] */ CURSOR_ID cid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *ReleaseCursorCapture )( 
            ITabletContextP * This,
            /* [in] */ CURSOR_ID cid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetPackets )( 
            ITabletContextP * This,
            /* [in] */ ULONG nBegin,
            /* [in] */ ULONG nEnd,
            /* [out][in] */ __RPC__inout ULONG *pcPkts,
            /* [in] */ ULONG cbPkts,
            /* [size_is][out] */ __RPC__out_ecount_full(cbPkts) BYTE *pbPkts,
            /* [out] */ __RPC__out CURSOR_ID *pCid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *PeekPackets )( 
            ITabletContextP * This,
            /* [in] */ ULONG nBegin,
            /* [in] */ ULONG nEnd,
            /* [out][in] */ __RPC__inout ULONG *pcPkts,
            /* [in] */ ULONG cbPkts,
            /* [size_is][out] */ __RPC__out_ecount_full(cbPkts) BYTE *pbPkts,
            /* [out] */ __RPC__out CURSOR_ID *pCid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *FlushPackets )( 
            ITabletContextP * This,
            /* [in] */ ULONG nBegin,
            /* [in] */ ULONG nEnd);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *FlushQueue )( 
            ITabletContextP * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetPacketCount )( 
            ITabletContextP * This,
            /* [in] */ ULONG nBegin,
            /* [in] */ ULONG nEnd,
            /* [out] */ __RPC__out ULONG *pcPkts);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetPacketQueueInfo )( 
            ITabletContextP * This,
            /* [out] */ __RPC__out ULONG *pnBegin,
            /* [out] */ __RPC__out ULONG *pnEnd,
            /* [out] */ __RPC__out ULONG *pcPkts);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *ForwardPackets )( 
            ITabletContextP * This,
            /* [in] */ ULONG nBegin,
            /* [in] */ ULONG nEnd);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *InjectPackets )( 
            ITabletContextP * This,
            /* [in] */ ULONG cPkts,
            /* [in] */ ULONG cbPkts,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkts) BYTE *pbPkts,
            /* [size_is][in] */ __RPC__in_ecount_full(cPkts) CURSOR_ID *pCids);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *ModifyPackets )( 
            ITabletContextP * This,
            /* [in] */ ULONG nBegin,
            /* [in] */ ULONG nEnd,
            /* [in] */ ULONG cbPkts,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkts) BYTE *pbPkts);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *ConvertToScreenCoordinates )( 
            ITabletContextP * This,
            /* [in] */ ULONG cPkts,
            /* [in] */ ULONG cbPkts,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkts) BYTE *pbPkts,
            /* [size_is][out] */ __RPC__out_ecount_full(cPkts) POINT *pPointsInScreen);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Overlap )( 
            ITabletContextP * This,
            /* [in] */ BOOL bTop,
            /* [out] */ __RPC__out DWORD *pdwtcid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetType )( 
            ITabletContextP * This,
            /* [out] */ __RPC__out CONTEXT_TYPE *pct);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *TrackInputRect )( 
            ITabletContextP * This,
            /* [out] */ __RPC__out RECT *prcInput);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *IsTopMostHook )( 
            ITabletContextP * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetEventSink )( 
            ITabletContextP * This,
            /* [out] */ __RPC__deref_out_opt ITabletEventSink **ppSink);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *UseSharedMemoryCommunications )( 
            ITabletContextP * This,
            /* [in] */ DWORD pid,
            /* [out] */ __RPC__out DWORD *phEventMoreData,
            /* [out] */ __RPC__out DWORD *phEventClientReady,
            /* [out] */ __RPC__out DWORD *phMutexAccess,
            /* [out] */ __RPC__out DWORD *phFileMapping);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *UseNamedSharedMemoryCommunications )( 
            ITabletContextP * This,
            /* [in] */ DWORD pid,
            /* [string][in] */ __RPC__in LPCTSTR szSid,
            /* [string][in] */ __RPC__in LPCTSTR sdIlSid,
            /* [out] */ __RPC__out DWORD *pdwEventMoreDataId,
            /* [out] */ __RPC__out DWORD *pdwEventClientReadyId,
            /* [out] */ __RPC__out DWORD *pdwMutexAccessId,
            /* [out] */ __RPC__out DWORD *pdwFileMappingId);
        
        END_INTERFACE
    } ITabletContextPVtbl;

    interface ITabletContextP
    {
        CONST_VTBL struct ITabletContextPVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITabletContextP_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITabletContextP_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITabletContextP_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITabletContextP_GetId(This,pTcid)	\
    ( (This)->lpVtbl -> GetId(This,pTcid) ) 

#define ITabletContextP_GetWindow(This,pHwnd)	\
    ( (This)->lpVtbl -> GetWindow(This,pHwnd) ) 

#define ITabletContextP_GetSettings(This,ppTCS)	\
    ( (This)->lpVtbl -> GetSettings(This,ppTCS) ) 

#define ITabletContextP_GetTablet(This,ppTablet)	\
    ( (This)->lpVtbl -> GetTablet(This,ppTablet) ) 

#define ITabletContextP_Enable(This,cet)	\
    ( (This)->lpVtbl -> Enable(This,cet) ) 

#define ITabletContextP_GetOptions(This,pdwOptions)	\
    ( (This)->lpVtbl -> GetOptions(This,pdwOptions) ) 

#define ITabletContextP_GetPacketDescription(This,ppPD)	\
    ( (This)->lpVtbl -> GetPacketDescription(This,ppPD) ) 

#define ITabletContextP_GetStatus(This,pdwStatus)	\
    ( (This)->lpVtbl -> GetStatus(This,pdwStatus) ) 

#define ITabletContextP_GetInputRect(This,prcInput)	\
    ( (This)->lpVtbl -> GetInputRect(This,prcInput) ) 

#define ITabletContextP_SetInputRect(This,prcInput)	\
    ( (This)->lpVtbl -> SetInputRect(This,prcInput) ) 

#define ITabletContextP_SetDevInputRect(This,prcInput)	\
    ( (This)->lpVtbl -> SetDevInputRect(This,prcInput) ) 

#define ITabletContextP_GetDevInputRect(This,prcInput)	\
    ( (This)->lpVtbl -> GetDevInputRect(This,prcInput) ) 

#define ITabletContextP_SetCapture(This)	\
    ( (This)->lpVtbl -> SetCapture(This) ) 

#define ITabletContextP_ReleaseCapture(This)	\
    ( (This)->lpVtbl -> ReleaseCapture(This) ) 

#define ITabletContextP_SetCursorCapture(This,cid)	\
    ( (This)->lpVtbl -> SetCursorCapture(This,cid) ) 

#define ITabletContextP_ReleaseCursorCapture(This,cid)	\
    ( (This)->lpVtbl -> ReleaseCursorCapture(This,cid) ) 

#define ITabletContextP_GetPackets(This,nBegin,nEnd,pcPkts,cbPkts,pbPkts,pCid)	\
    ( (This)->lpVtbl -> GetPackets(This,nBegin,nEnd,pcPkts,cbPkts,pbPkts,pCid) ) 

#define ITabletContextP_PeekPackets(This,nBegin,nEnd,pcPkts,cbPkts,pbPkts,pCid)	\
    ( (This)->lpVtbl -> PeekPackets(This,nBegin,nEnd,pcPkts,cbPkts,pbPkts,pCid) ) 

#define ITabletContextP_FlushPackets(This,nBegin,nEnd)	\
    ( (This)->lpVtbl -> FlushPackets(This,nBegin,nEnd) ) 

#define ITabletContextP_FlushQueue(This)	\
    ( (This)->lpVtbl -> FlushQueue(This) ) 

#define ITabletContextP_GetPacketCount(This,nBegin,nEnd,pcPkts)	\
    ( (This)->lpVtbl -> GetPacketCount(This,nBegin,nEnd,pcPkts) ) 

#define ITabletContextP_GetPacketQueueInfo(This,pnBegin,pnEnd,pcPkts)	\
    ( (This)->lpVtbl -> GetPacketQueueInfo(This,pnBegin,pnEnd,pcPkts) ) 

#define ITabletContextP_ForwardPackets(This,nBegin,nEnd)	\
    ( (This)->lpVtbl -> ForwardPackets(This,nBegin,nEnd) ) 

#define ITabletContextP_InjectPackets(This,cPkts,cbPkts,pbPkts,pCids)	\
    ( (This)->lpVtbl -> InjectPackets(This,cPkts,cbPkts,pbPkts,pCids) ) 

#define ITabletContextP_ModifyPackets(This,nBegin,nEnd,cbPkts,pbPkts)	\
    ( (This)->lpVtbl -> ModifyPackets(This,nBegin,nEnd,cbPkts,pbPkts) ) 

#define ITabletContextP_ConvertToScreenCoordinates(This,cPkts,cbPkts,pbPkts,pPointsInScreen)	\
    ( (This)->lpVtbl -> ConvertToScreenCoordinates(This,cPkts,cbPkts,pbPkts,pPointsInScreen) ) 


#define ITabletContextP_Overlap(This,bTop,pdwtcid)	\
    ( (This)->lpVtbl -> Overlap(This,bTop,pdwtcid) ) 

#define ITabletContextP_GetType(This,pct)	\
    ( (This)->lpVtbl -> GetType(This,pct) ) 

#define ITabletContextP_TrackInputRect(This,prcInput)	\
    ( (This)->lpVtbl -> TrackInputRect(This,prcInput) ) 

#define ITabletContextP_IsTopMostHook(This)	\
    ( (This)->lpVtbl -> IsTopMostHook(This) ) 

#define ITabletContextP_GetEventSink(This,ppSink)	\
    ( (This)->lpVtbl -> GetEventSink(This,ppSink) ) 

#define ITabletContextP_UseSharedMemoryCommunications(This,pid,phEventMoreData,phEventClientReady,phMutexAccess,phFileMapping)	\
    ( (This)->lpVtbl -> UseSharedMemoryCommunications(This,pid,phEventMoreData,phEventClientReady,phMutexAccess,phFileMapping) ) 

#define ITabletContextP_UseNamedSharedMemoryCommunications(This,pid,szSid,sdIlSid,pdwEventMoreDataId,pdwEventClientReadyId,pdwMutexAccessId,pdwFileMappingId)	\
    ( (This)->lpVtbl -> UseNamedSharedMemoryCommunications(This,pid,szSid,sdIlSid,pdwEventMoreDataId,pdwEventClientReadyId,pdwMutexAccessId,pdwFileMappingId) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITabletContextP_INTERFACE_DEFINED__ */


#ifndef __ITabletCursorP_INTERFACE_DEFINED__
#define __ITabletCursorP_INTERFACE_DEFINED__

/* interface ITabletCursorP */
/* [unique][helpstring][uuid][object] */ 

typedef ITabletCursorP *PTABLETCURSORP;


EXTERN_C const IID IID_ITabletCursorP;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("35DE0002-232C-4629-A915-7E600E80CD88")
    ITabletCursorP : public ITabletCursor
    {
    public:
    };
    
#else 	/* C style interface */

    typedef struct ITabletCursorPVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITabletCursorP * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITabletCursorP * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITabletCursorP * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetName )( 
            ITabletCursorP * This,
            /* [out] */ __RPC__deref_out_opt LPWSTR *ppwszName);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *IsInverted )( 
            ITabletCursorP * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetId )( 
            ITabletCursorP * This,
            /* [out] */ __RPC__out CURSOR_ID *pCid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetTablet )( 
            ITabletCursorP * This,
            /* [out] */ __RPC__deref_out_opt ITablet **ppTablet);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetButtonCount )( 
            ITabletCursorP * This,
            /* [out] */ __RPC__out ULONG *pcButtons);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetButton )( 
            ITabletCursorP * This,
            /* [in] */ ULONG iButton,
            /* [out] */ __RPC__deref_out_opt ITabletCursorButton **ppButton);
        
        END_INTERFACE
    } ITabletCursorPVtbl;

    interface ITabletCursorP
    {
        CONST_VTBL struct ITabletCursorPVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITabletCursorP_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITabletCursorP_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITabletCursorP_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITabletCursorP_GetName(This,ppwszName)	\
    ( (This)->lpVtbl -> GetName(This,ppwszName) ) 

#define ITabletCursorP_IsInverted(This)	\
    ( (This)->lpVtbl -> IsInverted(This) ) 

#define ITabletCursorP_GetId(This,pCid)	\
    ( (This)->lpVtbl -> GetId(This,pCid) ) 

#define ITabletCursorP_GetTablet(This,ppTablet)	\
    ( (This)->lpVtbl -> GetTablet(This,ppTablet) ) 

#define ITabletCursorP_GetButtonCount(This,pcButtons)	\
    ( (This)->lpVtbl -> GetButtonCount(This,pcButtons) ) 

#define ITabletCursorP_GetButton(This,iButton,ppButton)	\
    ( (This)->lpVtbl -> GetButton(This,iButton,ppButton) ) 


#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITabletCursorP_INTERFACE_DEFINED__ */


#ifndef __ITabletCursorButtonP_INTERFACE_DEFINED__
#define __ITabletCursorButtonP_INTERFACE_DEFINED__

/* interface ITabletCursorButtonP */
/* [unique][helpstring][uuid][object] */ 

typedef ITabletCursorButtonP *PTABLETCURSORBUTTONP;


EXTERN_C const IID IID_ITabletCursorButtonP;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("FCA502B0-5409-434d-8C35-A96C76CCA99C")
    ITabletCursorButtonP : public ITabletCursorButton
    {
    public:
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetId( 
            /* [out] */ __RPC__out DWORD *pId) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ITabletCursorButtonPVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITabletCursorButtonP * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITabletCursorButtonP * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITabletCursorButtonP * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetName )( 
            ITabletCursorButtonP * This,
            /* [out] */ __RPC__deref_out_opt LPWSTR *ppwszName);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetGuid )( 
            ITabletCursorButtonP * This,
            /* [out] */ __RPC__out GUID *pguidBtn);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetId )( 
            ITabletCursorButtonP * This,
            /* [out] */ __RPC__out DWORD *pId);
        
        END_INTERFACE
    } ITabletCursorButtonPVtbl;

    interface ITabletCursorButtonP
    {
        CONST_VTBL struct ITabletCursorButtonPVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITabletCursorButtonP_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITabletCursorButtonP_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITabletCursorButtonP_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITabletCursorButtonP_GetName(This,ppwszName)	\
    ( (This)->lpVtbl -> GetName(This,ppwszName) ) 

#define ITabletCursorButtonP_GetGuid(This,pguidBtn)	\
    ( (This)->lpVtbl -> GetGuid(This,pguidBtn) ) 


#define ITabletCursorButtonP_GetId(This,pId)	\
    ( (This)->lpVtbl -> GetId(This,pId) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITabletCursorButtonP_INTERFACE_DEFINED__ */


#ifndef __ITabletEventSinkP_INTERFACE_DEFINED__
#define __ITabletEventSinkP_INTERFACE_DEFINED__

/* interface ITabletEventSinkP */
/* [unique][helpstring][uuid][object] */ 

typedef ITabletEventSinkP *PTABLETEVENTSINKP;


EXTERN_C const IID IID_ITabletEventSinkP;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("287A9E67-8D1D-4a65-8DB4-51915395D019")
    ITabletEventSinkP : public IUnknown
    {
    public:
    };
    
#else 	/* C style interface */

    typedef struct ITabletEventSinkPVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITabletEventSinkP * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITabletEventSinkP * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITabletEventSinkP * This);
        
        END_INTERFACE
    } ITabletEventSinkPVtbl;

    interface ITabletEventSinkP
    {
        CONST_VTBL struct ITabletEventSinkPVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITabletEventSinkP_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITabletEventSinkP_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITabletEventSinkP_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITabletEventSinkP_INTERFACE_DEFINED__ */


/* Additional Prototypes for ALL interfaces */

unsigned long             __RPC_USER  BSTR_UserSize(     unsigned long *, unsigned long            , BSTR * ); 
unsigned char * __RPC_USER  BSTR_UserMarshal(  unsigned long *, unsigned char *, BSTR * ); 
unsigned char * __RPC_USER  BSTR_UserUnmarshal(unsigned long *, unsigned char *, BSTR * ); 
void                      __RPC_USER  BSTR_UserFree(     unsigned long *, BSTR * ); 

unsigned long             __RPC_USER  BSTR_UserSize64(     unsigned long *, unsigned long            , BSTR * ); 
unsigned char * __RPC_USER  BSTR_UserMarshal64(  unsigned long *, unsigned char *, BSTR * ); 
unsigned char * __RPC_USER  BSTR_UserUnmarshal64(unsigned long *, unsigned char *, BSTR * ); 
void                      __RPC_USER  BSTR_UserFree64(     unsigned long *, BSTR * ); 

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif



