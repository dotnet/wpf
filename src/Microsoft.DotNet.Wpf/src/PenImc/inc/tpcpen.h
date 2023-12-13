// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 7.00.0498 */
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

#ifndef __tpcpen_h__
#define __tpcpen_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __ITabletEventSink_FWD_DEFINED__
#define __ITabletEventSink_FWD_DEFINED__
typedef interface ITabletEventSink ITabletEventSink;
#endif 	/* __ITabletEventSink_FWD_DEFINED__ */


#ifndef __AsyncITabletEventSink_FWD_DEFINED__
#define __AsyncITabletEventSink_FWD_DEFINED__
typedef interface AsyncITabletEventSink AsyncITabletEventSink;
#endif 	/* __AsyncITabletEventSink_FWD_DEFINED__ */


#ifndef __ITabletManager_FWD_DEFINED__
#define __ITabletManager_FWD_DEFINED__
typedef interface ITabletManager ITabletManager;
#endif 	/* __ITabletManager_FWD_DEFINED__ */


#ifndef __ITablet_FWD_DEFINED__
#define __ITablet_FWD_DEFINED__
typedef interface ITablet ITablet;
#endif 	/* __ITablet_FWD_DEFINED__ */


#ifndef __ITablet2_FWD_DEFINED__
#define __ITablet2_FWD_DEFINED__
typedef interface ITablet2 ITablet2;
#endif 	/* __ITablet2_FWD_DEFINED__ */


#ifndef __ITabletSettings_FWD_DEFINED__
#define __ITabletSettings_FWD_DEFINED__
typedef interface ITabletSettings ITabletSettings;
#endif 	/* __ITabletSettings_FWD_DEFINED__ */


#ifndef __ITabletContext_FWD_DEFINED__
#define __ITabletContext_FWD_DEFINED__
typedef interface ITabletContext ITabletContext;
#endif 	/* __ITabletContext_FWD_DEFINED__ */


#ifndef __ITabletCursor_FWD_DEFINED__
#define __ITabletCursor_FWD_DEFINED__
typedef interface ITabletCursor ITabletCursor;
#endif 	/* __ITabletCursor_FWD_DEFINED__ */


#ifndef __ITabletCursorButton_FWD_DEFINED__
#define __ITabletCursorButton_FWD_DEFINED__
typedef interface ITabletCursorButton ITabletCursorButton;
#endif 	/* __ITabletCursorButton_FWD_DEFINED__ */


#ifndef __ITabletEventSink_FWD_DEFINED__
#define __ITabletEventSink_FWD_DEFINED__
typedef interface ITabletEventSink ITabletEventSink;
#endif 	/* __ITabletEventSink_FWD_DEFINED__ */


#ifndef __ITabletManager_FWD_DEFINED__
#define __ITabletManager_FWD_DEFINED__
typedef interface ITabletManager ITabletManager;
#endif 	/* __ITabletManager_FWD_DEFINED__ */


#ifndef __ITablet_FWD_DEFINED__
#define __ITablet_FWD_DEFINED__
typedef interface ITablet ITablet;
#endif 	/* __ITablet_FWD_DEFINED__ */


#ifndef __ITabletContext_FWD_DEFINED__
#define __ITabletContext_FWD_DEFINED__
typedef interface ITabletContext ITabletContext;
#endif 	/* __ITabletContext_FWD_DEFINED__ */


#ifndef __ITabletCursor_FWD_DEFINED__
#define __ITabletCursor_FWD_DEFINED__
typedef interface ITabletCursor ITabletCursor;
#endif 	/* __ITabletCursor_FWD_DEFINED__ */


#ifndef __ITabletCursorButton_FWD_DEFINED__
#define __ITabletCursorButton_FWD_DEFINED__
typedef interface ITabletCursorButton ITabletCursorButton;
#endif 	/* __ITabletCursorButton_FWD_DEFINED__ */


#ifndef __TabletManager_FWD_DEFINED__
#define __TabletManager_FWD_DEFINED__

#ifdef __cplusplus
typedef class TabletManager TabletManager;
#else
typedef struct TabletManager TabletManager;
#endif /* __cplusplus */

#endif 	/* __TabletManager_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"
#include "tpcshrd.h"
#include "pentypes.h"

#ifdef __cplusplus
extern "C"{
#endif 


/* interface __MIDL_itf_tpcpen_0000_0000 */
/* [local] */ 


#pragma once







extern RPC_IF_HANDLE __MIDL_itf_tpcpen_0000_0000_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_tpcpen_0000_0000_v0_0_s_ifspec;

#ifndef __ITabletEventSink_INTERFACE_DEFINED__
#define __ITabletEventSink_INTERFACE_DEFINED__

/* interface ITabletEventSink */
/* [unique][helpstring][async_uuid][uuid][object] */ 

typedef /* [unique] */  __RPC_unique_pointer ITabletEventSink *PTABLETEVENTSINK;


EXTERN_C const IID IID_ITabletEventSink;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("788459C8-26C8-4666-BF57-04AD3A0A5EB5")
    ITabletEventSink : public IUnknown
    {
    public:
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE ContextCreate( 
            /* [in] */ TABLET_CONTEXT_ID tcid) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE ContextDestroy( 
            /* [in] */ TABLET_CONTEXT_ID tcid) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE CursorNew( 
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE CursorInRange( 
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE CursorOutOfRange( 
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE CursorDown( 
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid,
            /* [in] */ ULONG nSerialNumber,
            /* [in] */ ULONG cbPkt,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkt) BYTE *pbPkt) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE CursorUp( 
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid,
            /* [in] */ ULONG nSerialNumber,
            /* [in] */ ULONG cbPkt,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkt) BYTE *pbPkt) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Packets( 
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ ULONG cPkts,
            /* [in] */ ULONG cbPkts,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkts) BYTE *pbPkts,
            /* [size_is][unique][in] */ __RPC__in_ecount_full_opt(cPkts) ULONG *pnSerialNumbers,
            /* [in] */ CURSOR_ID cid) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE SystemEvent( 
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid,
            /* [in] */ SYSTEM_EVENT event,
            /* [in] */ SYSTEM_EVENT_DATA eventdata) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ITabletEventSinkVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITabletEventSink * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITabletEventSink * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITabletEventSink * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *ContextCreate )( 
            ITabletEventSink * This,
            /* [in] */ TABLET_CONTEXT_ID tcid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *ContextDestroy )( 
            ITabletEventSink * This,
            /* [in] */ TABLET_CONTEXT_ID tcid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *CursorNew )( 
            ITabletEventSink * This,
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *CursorInRange )( 
            ITabletEventSink * This,
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *CursorOutOfRange )( 
            ITabletEventSink * This,
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *CursorDown )( 
            ITabletEventSink * This,
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid,
            /* [in] */ ULONG nSerialNumber,
            /* [in] */ ULONG cbPkt,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkt) BYTE *pbPkt);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *CursorUp )( 
            ITabletEventSink * This,
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid,
            /* [in] */ ULONG nSerialNumber,
            /* [in] */ ULONG cbPkt,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkt) BYTE *pbPkt);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Packets )( 
            ITabletEventSink * This,
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ ULONG cPkts,
            /* [in] */ ULONG cbPkts,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkts) BYTE *pbPkts,
            /* [size_is][unique][in] */ __RPC__in_ecount_full_opt(cPkts) ULONG *pnSerialNumbers,
            /* [in] */ CURSOR_ID cid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *SystemEvent )( 
            ITabletEventSink * This,
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid,
            /* [in] */ SYSTEM_EVENT event,
            /* [in] */ SYSTEM_EVENT_DATA eventdata);
        
        END_INTERFACE
    } ITabletEventSinkVtbl;

    interface ITabletEventSink
    {
        CONST_VTBL struct ITabletEventSinkVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITabletEventSink_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITabletEventSink_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITabletEventSink_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITabletEventSink_ContextCreate(This,tcid)	\
    ( (This)->lpVtbl -> ContextCreate(This,tcid) ) 

#define ITabletEventSink_ContextDestroy(This,tcid)	\
    ( (This)->lpVtbl -> ContextDestroy(This,tcid) ) 

#define ITabletEventSink_CursorNew(This,tcid,cid)	\
    ( (This)->lpVtbl -> CursorNew(This,tcid,cid) ) 

#define ITabletEventSink_CursorInRange(This,tcid,cid)	\
    ( (This)->lpVtbl -> CursorInRange(This,tcid,cid) ) 

#define ITabletEventSink_CursorOutOfRange(This,tcid,cid)	\
    ( (This)->lpVtbl -> CursorOutOfRange(This,tcid,cid) ) 

#define ITabletEventSink_CursorDown(This,tcid,cid,nSerialNumber,cbPkt,pbPkt)	\
    ( (This)->lpVtbl -> CursorDown(This,tcid,cid,nSerialNumber,cbPkt,pbPkt) ) 

#define ITabletEventSink_CursorUp(This,tcid,cid,nSerialNumber,cbPkt,pbPkt)	\
    ( (This)->lpVtbl -> CursorUp(This,tcid,cid,nSerialNumber,cbPkt,pbPkt) ) 

#define ITabletEventSink_Packets(This,tcid,cPkts,cbPkts,pbPkts,pnSerialNumbers,cid)	\
    ( (This)->lpVtbl -> Packets(This,tcid,cPkts,cbPkts,pbPkts,pnSerialNumbers,cid) ) 

#define ITabletEventSink_SystemEvent(This,tcid,cid,event,eventdata)	\
    ( (This)->lpVtbl -> SystemEvent(This,tcid,cid,event,eventdata) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITabletEventSink_INTERFACE_DEFINED__ */


#ifndef __AsyncITabletEventSink_INTERFACE_DEFINED__
#define __AsyncITabletEventSink_INTERFACE_DEFINED__

/* interface AsyncITabletEventSink */
/* [uuid][unique][helpstring][object] */ 


EXTERN_C const IID IID_AsyncITabletEventSink;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("CDF7D7D6-2E5D-47c7-90FC-C638C7FA3FC4")
    AsyncITabletEventSink : public IUnknown
    {
    public:
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Begin_ContextCreate( 
            /* [in] */ TABLET_CONTEXT_ID tcid) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Finish_ContextCreate( void) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Begin_ContextDestroy( 
            /* [in] */ TABLET_CONTEXT_ID tcid) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Finish_ContextDestroy( void) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Begin_CursorNew( 
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Finish_CursorNew( void) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Begin_CursorInRange( 
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Finish_CursorInRange( void) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Begin_CursorOutOfRange( 
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Finish_CursorOutOfRange( void) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Begin_CursorDown( 
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid,
            /* [in] */ ULONG nSerialNumber,
            /* [in] */ ULONG cbPkt,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkt) BYTE *pbPkt) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Finish_CursorDown( void) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Begin_CursorUp( 
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid,
            /* [in] */ ULONG nSerialNumber,
            /* [in] */ ULONG cbPkt,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkt) BYTE *pbPkt) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Finish_CursorUp( void) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Begin_Packets( 
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ ULONG cPkts,
            /* [in] */ ULONG cbPkts,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkts) BYTE *pbPkts,
            /* [size_is][unique][in] */ __RPC__in_ecount_full_opt(cPkts) ULONG *pnSerialNumbers,
            /* [in] */ CURSOR_ID cid) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Finish_Packets( void) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Begin_SystemEvent( 
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid,
            /* [in] */ SYSTEM_EVENT event,
            /* [in] */ SYSTEM_EVENT_DATA eventdata) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Finish_SystemEvent( void) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct AsyncITabletEventSinkVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            AsyncITabletEventSink * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            AsyncITabletEventSink * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            AsyncITabletEventSink * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Begin_ContextCreate )( 
            AsyncITabletEventSink * This,
            /* [in] */ TABLET_CONTEXT_ID tcid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Finish_ContextCreate )( 
            AsyncITabletEventSink * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Begin_ContextDestroy )( 
            AsyncITabletEventSink * This,
            /* [in] */ TABLET_CONTEXT_ID tcid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Finish_ContextDestroy )( 
            AsyncITabletEventSink * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Begin_CursorNew )( 
            AsyncITabletEventSink * This,
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Finish_CursorNew )( 
            AsyncITabletEventSink * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Begin_CursorInRange )( 
            AsyncITabletEventSink * This,
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Finish_CursorInRange )( 
            AsyncITabletEventSink * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Begin_CursorOutOfRange )( 
            AsyncITabletEventSink * This,
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Finish_CursorOutOfRange )( 
            AsyncITabletEventSink * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Begin_CursorDown )( 
            AsyncITabletEventSink * This,
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid,
            /* [in] */ ULONG nSerialNumber,
            /* [in] */ ULONG cbPkt,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkt) BYTE *pbPkt);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Finish_CursorDown )( 
            AsyncITabletEventSink * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Begin_CursorUp )( 
            AsyncITabletEventSink * This,
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid,
            /* [in] */ ULONG nSerialNumber,
            /* [in] */ ULONG cbPkt,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkt) BYTE *pbPkt);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Finish_CursorUp )( 
            AsyncITabletEventSink * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Begin_Packets )( 
            AsyncITabletEventSink * This,
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ ULONG cPkts,
            /* [in] */ ULONG cbPkts,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkts) BYTE *pbPkts,
            /* [size_is][unique][in] */ __RPC__in_ecount_full_opt(cPkts) ULONG *pnSerialNumbers,
            /* [in] */ CURSOR_ID cid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Finish_Packets )( 
            AsyncITabletEventSink * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Begin_SystemEvent )( 
            AsyncITabletEventSink * This,
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [in] */ CURSOR_ID cid,
            /* [in] */ SYSTEM_EVENT event,
            /* [in] */ SYSTEM_EVENT_DATA eventdata);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Finish_SystemEvent )( 
            AsyncITabletEventSink * This);
        
        END_INTERFACE
    } AsyncITabletEventSinkVtbl;

    interface AsyncITabletEventSink
    {
        CONST_VTBL struct AsyncITabletEventSinkVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define AsyncITabletEventSink_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define AsyncITabletEventSink_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define AsyncITabletEventSink_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define AsyncITabletEventSink_Begin_ContextCreate(This,tcid)	\
    ( (This)->lpVtbl -> Begin_ContextCreate(This,tcid) ) 

#define AsyncITabletEventSink_Finish_ContextCreate(This)	\
    ( (This)->lpVtbl -> Finish_ContextCreate(This) ) 

#define AsyncITabletEventSink_Begin_ContextDestroy(This,tcid)	\
    ( (This)->lpVtbl -> Begin_ContextDestroy(This,tcid) ) 

#define AsyncITabletEventSink_Finish_ContextDestroy(This)	\
    ( (This)->lpVtbl -> Finish_ContextDestroy(This) ) 

#define AsyncITabletEventSink_Begin_CursorNew(This,tcid,cid)	\
    ( (This)->lpVtbl -> Begin_CursorNew(This,tcid,cid) ) 

#define AsyncITabletEventSink_Finish_CursorNew(This)	\
    ( (This)->lpVtbl -> Finish_CursorNew(This) ) 

#define AsyncITabletEventSink_Begin_CursorInRange(This,tcid,cid)	\
    ( (This)->lpVtbl -> Begin_CursorInRange(This,tcid,cid) ) 

#define AsyncITabletEventSink_Finish_CursorInRange(This)	\
    ( (This)->lpVtbl -> Finish_CursorInRange(This) ) 

#define AsyncITabletEventSink_Begin_CursorOutOfRange(This,tcid,cid)	\
    ( (This)->lpVtbl -> Begin_CursorOutOfRange(This,tcid,cid) ) 

#define AsyncITabletEventSink_Finish_CursorOutOfRange(This)	\
    ( (This)->lpVtbl -> Finish_CursorOutOfRange(This) ) 

#define AsyncITabletEventSink_Begin_CursorDown(This,tcid,cid,nSerialNumber,cbPkt,pbPkt)	\
    ( (This)->lpVtbl -> Begin_CursorDown(This,tcid,cid,nSerialNumber,cbPkt,pbPkt) ) 

#define AsyncITabletEventSink_Finish_CursorDown(This)	\
    ( (This)->lpVtbl -> Finish_CursorDown(This) ) 

#define AsyncITabletEventSink_Begin_CursorUp(This,tcid,cid,nSerialNumber,cbPkt,pbPkt)	\
    ( (This)->lpVtbl -> Begin_CursorUp(This,tcid,cid,nSerialNumber,cbPkt,pbPkt) ) 

#define AsyncITabletEventSink_Finish_CursorUp(This)	\
    ( (This)->lpVtbl -> Finish_CursorUp(This) ) 

#define AsyncITabletEventSink_Begin_Packets(This,tcid,cPkts,cbPkts,pbPkts,pnSerialNumbers,cid)	\
    ( (This)->lpVtbl -> Begin_Packets(This,tcid,cPkts,cbPkts,pbPkts,pnSerialNumbers,cid) ) 

#define AsyncITabletEventSink_Finish_Packets(This)	\
    ( (This)->lpVtbl -> Finish_Packets(This) ) 

#define AsyncITabletEventSink_Begin_SystemEvent(This,tcid,cid,event,eventdata)	\
    ( (This)->lpVtbl -> Begin_SystemEvent(This,tcid,cid,event,eventdata) ) 

#define AsyncITabletEventSink_Finish_SystemEvent(This)	\
    ( (This)->lpVtbl -> Finish_SystemEvent(This) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __AsyncITabletEventSink_INTERFACE_DEFINED__ */


#ifndef __ITabletManager_INTERFACE_DEFINED__
#define __ITabletManager_INTERFACE_DEFINED__

/* interface ITabletManager */
/* [unique][helpstring][uuid][object] */ 

typedef /* [unique] */  __RPC_unique_pointer ITabletManager *PTABLETMANAGER;


EXTERN_C const IID IID_ITabletManager;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("764DE8AA-1867-47C1-8F6A-122445ABD89A")
    ITabletManager : public IUnknown
    {
    public:
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetDefaultTablet( 
            /* [out] */ __RPC__deref_out_opt ITablet **ppTablet) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetTabletCount( 
            /* [out] */ __RPC__out ULONG *pcTablets) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetTablet( 
            /* [in] */ ULONG iTablet,
            /* [out] */ __RPC__deref_out_opt ITablet **ppTablet) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetTabletContextById( 
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [out] */ __RPC__deref_out_opt ITabletContext **ppContext) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetCursorById( 
            /* [in] */ CURSOR_ID cid,
            /* [out] */ __RPC__deref_out_opt ITabletCursor **ppCursor) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ITabletManagerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITabletManager * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITabletManager * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITabletManager * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetDefaultTablet )( 
            ITabletManager * This,
            /* [out] */ __RPC__deref_out_opt ITablet **ppTablet);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetTabletCount )( 
            ITabletManager * This,
            /* [out] */ __RPC__out ULONG *pcTablets);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetTablet )( 
            ITabletManager * This,
            /* [in] */ ULONG iTablet,
            /* [out] */ __RPC__deref_out_opt ITablet **ppTablet);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetTabletContextById )( 
            ITabletManager * This,
            /* [in] */ TABLET_CONTEXT_ID tcid,
            /* [out] */ __RPC__deref_out_opt ITabletContext **ppContext);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetCursorById )( 
            ITabletManager * This,
            /* [in] */ CURSOR_ID cid,
            /* [out] */ __RPC__deref_out_opt ITabletCursor **ppCursor);
        
        END_INTERFACE
    } ITabletManagerVtbl;

    interface ITabletManager
    {
        CONST_VTBL struct ITabletManagerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITabletManager_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITabletManager_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITabletManager_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITabletManager_GetDefaultTablet(This,ppTablet)	\
    ( (This)->lpVtbl -> GetDefaultTablet(This,ppTablet) ) 

#define ITabletManager_GetTabletCount(This,pcTablets)	\
    ( (This)->lpVtbl -> GetTabletCount(This,pcTablets) ) 

#define ITabletManager_GetTablet(This,iTablet,ppTablet)	\
    ( (This)->lpVtbl -> GetTablet(This,iTablet,ppTablet) ) 

#define ITabletManager_GetTabletContextById(This,tcid,ppContext)	\
    ( (This)->lpVtbl -> GetTabletContextById(This,tcid,ppContext) ) 

#define ITabletManager_GetCursorById(This,cid,ppCursor)	\
    ( (This)->lpVtbl -> GetCursorById(This,cid,ppCursor) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITabletManager_INTERFACE_DEFINED__ */


#ifndef __ITablet_INTERFACE_DEFINED__
#define __ITablet_INTERFACE_DEFINED__

/* interface ITablet */
/* [unique][helpstring][uuid][object] */ 

typedef /* [unique] */  __RPC_unique_pointer ITablet *PTABLET;


EXTERN_C const IID IID_ITablet;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("1CB2EFC3-ABC7-4172-8FCB-3BC9CB93E29F")
    ITablet : public IUnknown
    {
    public:
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetDefaultContextSettings( 
            /* [out] */ __RPC__deref_out_opt TABLET_CONTEXT_SETTINGS **ppTCS) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE CreateContext( 
            /* [in] */ __RPC__in HWND hWnd,
            /* [unique][in] */ __RPC__in_opt RECT *prcInput,
            /* [in] */ DWORD dwOptions,
            /* [unique][in] */ __RPC__in_opt TABLET_CONTEXT_SETTINGS *pTCS,
            /* [in] */ CONTEXT_ENABLE_TYPE cet,
            /* [out] */ __RPC__deref_out_opt ITabletContext **ppCtx,
            /* [unique][out][in] */ __RPC__inout_opt TABLET_CONTEXT_ID *pTcid,
            /* [unique][out][in] */ __RPC__deref_opt_inout_opt PACKET_DESCRIPTION **ppPD,
            /* [unique][in] */ __RPC__in_opt ITabletEventSink *pSink) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetName( 
            /* [out] */ __RPC__deref_out_opt LPWSTR *ppwszName) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetMaxInputRect( 
            /* [out] */ __RPC__out RECT *prcInput) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetHardwareCaps( 
            /* [out] */ __RPC__out DWORD *pdwCaps) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetPropertyMetrics( 
            /* [in] */ __RPC__in REFGUID rguid,
            /* [out] */ __RPC__out PROPERTY_METRICS *pPM) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetPlugAndPlayId( 
            /* [out] */ __RPC__deref_out_opt LPWSTR *ppwszPPId) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetCursorCount( 
            /* [out] */ __RPC__out ULONG *pcCurs) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetCursor( 
            /* [in] */ ULONG iCur,
            /* [out] */ __RPC__deref_out_opt ITabletCursor **ppCur) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ITabletVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITablet * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITablet * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITablet * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetDefaultContextSettings )( 
            ITablet * This,
            /* [out] */ __RPC__deref_out_opt TABLET_CONTEXT_SETTINGS **ppTCS);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *CreateContext )( 
            ITablet * This,
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
            ITablet * This,
            /* [out] */ __RPC__deref_out_opt LPWSTR *ppwszName);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetMaxInputRect )( 
            ITablet * This,
            /* [out] */ __RPC__out RECT *prcInput);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetHardwareCaps )( 
            ITablet * This,
            /* [out] */ __RPC__out DWORD *pdwCaps);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetPropertyMetrics )( 
            ITablet * This,
            /* [in] */ __RPC__in REFGUID rguid,
            /* [out] */ __RPC__out PROPERTY_METRICS *pPM);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetPlugAndPlayId )( 
            ITablet * This,
            /* [out] */ __RPC__deref_out_opt LPWSTR *ppwszPPId);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetCursorCount )( 
            ITablet * This,
            /* [out] */ __RPC__out ULONG *pcCurs);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetCursor )( 
            ITablet * This,
            /* [in] */ ULONG iCur,
            /* [out] */ __RPC__deref_out_opt ITabletCursor **ppCur);
        
        END_INTERFACE
    } ITabletVtbl;

    interface ITablet
    {
        CONST_VTBL struct ITabletVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITablet_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITablet_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITablet_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITablet_GetDefaultContextSettings(This,ppTCS)	\
    ( (This)->lpVtbl -> GetDefaultContextSettings(This,ppTCS) ) 

#define ITablet_CreateContext(This,hWnd,prcInput,dwOptions,pTCS,cet,ppCtx,pTcid,ppPD,pSink)	\
    ( (This)->lpVtbl -> CreateContext(This,hWnd,prcInput,dwOptions,pTCS,cet,ppCtx,pTcid,ppPD,pSink) ) 

#define ITablet_GetName(This,ppwszName)	\
    ( (This)->lpVtbl -> GetName(This,ppwszName) ) 

#define ITablet_GetMaxInputRect(This,prcInput)	\
    ( (This)->lpVtbl -> GetMaxInputRect(This,prcInput) ) 

#define ITablet_GetHardwareCaps(This,pdwCaps)	\
    ( (This)->lpVtbl -> GetHardwareCaps(This,pdwCaps) ) 

#define ITablet_GetPropertyMetrics(This,rguid,pPM)	\
    ( (This)->lpVtbl -> GetPropertyMetrics(This,rguid,pPM) ) 

#define ITablet_GetPlugAndPlayId(This,ppwszPPId)	\
    ( (This)->lpVtbl -> GetPlugAndPlayId(This,ppwszPPId) ) 

#define ITablet_GetCursorCount(This,pcCurs)	\
    ( (This)->lpVtbl -> GetCursorCount(This,pcCurs) ) 

#define ITablet_GetCursor(This,iCur,ppCur)	\
    ( (This)->lpVtbl -> GetCursor(This,iCur,ppCur) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITablet_INTERFACE_DEFINED__ */


/* interface __MIDL_itf_tpcpen_0000_0003 */
/* [local] */ 

typedef 
enum _TABLET_DEVICE_KIND
    {	TABLET_DEVICE_MOUSE	= 0,
	TABLET_DEVICE_PEN	= ( TABLET_DEVICE_MOUSE + 1 ) ,
	TABLET_DEVICE_TOUCH	= ( TABLET_DEVICE_PEN + 1 ) 
    } 	TABLET_DEVICE_KIND;



extern RPC_IF_HANDLE __MIDL_itf_tpcpen_0000_0003_v0_0_c_ifspec;
extern RPC_IF_HANDLE __MIDL_itf_tpcpen_0000_0003_v0_0_s_ifspec;

#ifndef __ITablet2_INTERFACE_DEFINED__
#define __ITablet2_INTERFACE_DEFINED__

/* interface ITablet2 */
/* [unique][helpstring][uuid][object] */ 

typedef /* [unique] */  __RPC_unique_pointer ITablet2 *PTABLET2;


EXTERN_C const IID IID_ITablet2;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("C247F616-BBEB-406A-AED3-F75E656599AE")
    ITablet2 : public IUnknown
    {
    public:
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetDeviceKind( 
            /* [out] */ __RPC__out TABLET_DEVICE_KIND *pKind) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetMatchingScreenRect( 
            /* [out] */ __RPC__out RECT *prcInput) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ITablet2Vtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITablet2 * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITablet2 * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITablet2 * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetDeviceKind )( 
            ITablet2 * This,
            /* [out] */ __RPC__out TABLET_DEVICE_KIND *pKind);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetMatchingScreenRect )( 
            ITablet2 * This,
            /* [out] */ __RPC__out RECT *prcInput);
        
        END_INTERFACE
    } ITablet2Vtbl;

    interface ITablet2
    {
        CONST_VTBL struct ITablet2Vtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITablet2_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITablet2_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITablet2_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITablet2_GetDeviceKind(This,pKind)	\
    ( (This)->lpVtbl -> GetDeviceKind(This,pKind) ) 

#define ITablet2_GetMatchingScreenRect(This,prcInput)	\
    ( (This)->lpVtbl -> GetMatchingScreenRect(This,prcInput) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITablet2_INTERFACE_DEFINED__ */


#ifndef __ITabletSettings_INTERFACE_DEFINED__
#define __ITabletSettings_INTERFACE_DEFINED__

/* interface ITabletSettings */
/* [unique][helpstring][uuid][object] */ 

typedef ITabletSettings *PTABLETSETTINGS;


EXTERN_C const IID IID_ITabletSettings;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("120ae7c9-36f7-4be6-93da-e5f266847b01")
    ITabletSettings : public IUnknown
    {
    public:
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetProperty( 
            /* [in] */ DWORD dwProperty,
            /* [out][in] */ __RPC__inout DWORD *pcbData,
            /* [length_is][size_is][unique][out][in] */ __RPC__inout_ecount_part_opt(*pcbData, *pcbData) BYTE *pbData) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE SetProperty( 
            /* [in] */ DWORD dwProperty,
            /* [in] */ DWORD cbData,
            /* [size_is][unique][in] */ __RPC__in_ecount_full_opt(cbData) BYTE *pbData) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ITabletSettingsVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITabletSettings * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITabletSettings * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITabletSettings * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetProperty )( 
            ITabletSettings * This,
            /* [in] */ DWORD dwProperty,
            /* [out][in] */ __RPC__inout DWORD *pcbData,
            /* [length_is][size_is][unique][out][in] */ __RPC__inout_ecount_part_opt(*pcbData, *pcbData) BYTE *pbData);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *SetProperty )( 
            ITabletSettings * This,
            /* [in] */ DWORD dwProperty,
            /* [in] */ DWORD cbData,
            /* [size_is][unique][in] */ __RPC__in_ecount_full_opt(cbData) BYTE *pbData);
        
        END_INTERFACE
    } ITabletSettingsVtbl;

    interface ITabletSettings
    {
        CONST_VTBL struct ITabletSettingsVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITabletSettings_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITabletSettings_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITabletSettings_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITabletSettings_GetProperty(This,dwProperty,pcbData,pbData)	\
    ( (This)->lpVtbl -> GetProperty(This,dwProperty,pcbData,pbData) ) 

#define ITabletSettings_SetProperty(This,dwProperty,cbData,pbData)	\
    ( (This)->lpVtbl -> SetProperty(This,dwProperty,cbData,pbData) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITabletSettings_INTERFACE_DEFINED__ */


#ifndef __ITabletContext_INTERFACE_DEFINED__
#define __ITabletContext_INTERFACE_DEFINED__

/* interface ITabletContext */
/* [unique][helpstring][uuid][object] */ 

typedef ITabletContext *PTABLETCONTEXT;


EXTERN_C const IID IID_ITabletContext;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("45AAAF04-9D6F-41AE-8ED1-ECD6D4B2F17F")
    ITabletContext : public IUnknown
    {
    public:
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetId( 
            /* [out] */ __RPC__out TABLET_CONTEXT_ID *pTcid) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetWindow( 
            /* [out] */ __RPC__deref_out_opt HWND *pHwnd) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetSettings( 
            /* [out] */ __RPC__deref_out_opt TABLET_CONTEXT_SETTINGS **ppTCS) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetTablet( 
            /* [out] */ __RPC__deref_out_opt ITablet **ppTablet) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE Enable( 
            /* [in] */ CONTEXT_ENABLE_TYPE cet) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetOptions( 
            /* [out] */ __RPC__out DWORD *pdwOptions) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetPacketDescription( 
            /* [out] */ __RPC__deref_out_opt PACKET_DESCRIPTION **ppPD) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetStatus( 
            /* [out] */ __RPC__out DWORD *pdwStatus) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetInputRect( 
            /* [out] */ __RPC__out RECT *prcInput) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE SetInputRect( 
            /* [unique][in] */ __RPC__in_opt RECT *prcInput) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE SetDevInputRect( 
            /* [unique][in] */ __RPC__in_opt RECT *prcInput) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetDevInputRect( 
            /* [out] */ __RPC__out RECT *prcInput) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE SetCapture( void) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE ReleaseCapture( void) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE SetCursorCapture( 
            /* [in] */ CURSOR_ID cid) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE ReleaseCursorCapture( 
            /* [in] */ CURSOR_ID cid) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetPackets( 
            /* [in] */ ULONG nBegin,
            /* [in] */ ULONG nEnd,
            /* [out][in] */ __RPC__inout ULONG *pcPkts,
            /* [in] */ ULONG cbPkts,
            /* [size_is][out] */ __RPC__out_ecount_full(cbPkts) BYTE *pbPkts,
            /* [out] */ __RPC__out CURSOR_ID *pCid) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE PeekPackets( 
            /* [in] */ ULONG nBegin,
            /* [in] */ ULONG nEnd,
            /* [out][in] */ __RPC__inout ULONG *pcPkts,
            /* [in] */ ULONG cbPkts,
            /* [size_is][out] */ __RPC__out_ecount_full(cbPkts) BYTE *pbPkts,
            /* [out] */ __RPC__out CURSOR_ID *pCid) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE FlushPackets( 
            /* [in] */ ULONG nBegin,
            /* [in] */ ULONG nEnd) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE FlushQueue( void) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetPacketCount( 
            /* [in] */ ULONG nBegin,
            /* [in] */ ULONG nEnd,
            /* [out] */ __RPC__out ULONG *pcPkts) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetPacketQueueInfo( 
            /* [out] */ __RPC__out ULONG *pnBegin,
            /* [out] */ __RPC__out ULONG *pnEnd,
            /* [out] */ __RPC__out ULONG *pcPkts) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE ForwardPackets( 
            /* [in] */ ULONG nBegin,
            /* [in] */ ULONG nEnd) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE InjectPackets( 
            /* [in] */ ULONG cPkts,
            /* [in] */ ULONG cbPkts,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkts) BYTE *pbPkts,
            /* [size_is][in] */ __RPC__in_ecount_full(cPkts) CURSOR_ID *pCids) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE ModifyPackets( 
            /* [in] */ ULONG nBegin,
            /* [in] */ ULONG nEnd,
            /* [in] */ ULONG cbPkts,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkts) BYTE *pbPkts) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE ConvertToScreenCoordinates( 
            /* [in] */ ULONG cPkts,
            /* [in] */ ULONG cbPkts,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkts) BYTE *pbPkts,
            /* [size_is][out] */ __RPC__out_ecount_full(cPkts) POINT *pPointsInScreen) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ITabletContextVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITabletContext * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITabletContext * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITabletContext * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetId )( 
            ITabletContext * This,
            /* [out] */ __RPC__out TABLET_CONTEXT_ID *pTcid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetWindow )( 
            ITabletContext * This,
            /* [out] */ __RPC__deref_out_opt HWND *pHwnd);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetSettings )( 
            ITabletContext * This,
            /* [out] */ __RPC__deref_out_opt TABLET_CONTEXT_SETTINGS **ppTCS);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetTablet )( 
            ITabletContext * This,
            /* [out] */ __RPC__deref_out_opt ITablet **ppTablet);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *Enable )( 
            ITabletContext * This,
            /* [in] */ CONTEXT_ENABLE_TYPE cet);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetOptions )( 
            ITabletContext * This,
            /* [out] */ __RPC__out DWORD *pdwOptions);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetPacketDescription )( 
            ITabletContext * This,
            /* [out] */ __RPC__deref_out_opt PACKET_DESCRIPTION **ppPD);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetStatus )( 
            ITabletContext * This,
            /* [out] */ __RPC__out DWORD *pdwStatus);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetInputRect )( 
            ITabletContext * This,
            /* [out] */ __RPC__out RECT *prcInput);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *SetInputRect )( 
            ITabletContext * This,
            /* [unique][in] */ __RPC__in_opt RECT *prcInput);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *SetDevInputRect )( 
            ITabletContext * This,
            /* [unique][in] */ __RPC__in_opt RECT *prcInput);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetDevInputRect )( 
            ITabletContext * This,
            /* [out] */ __RPC__out RECT *prcInput);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *SetCapture )( 
            ITabletContext * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *ReleaseCapture )( 
            ITabletContext * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *SetCursorCapture )( 
            ITabletContext * This,
            /* [in] */ CURSOR_ID cid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *ReleaseCursorCapture )( 
            ITabletContext * This,
            /* [in] */ CURSOR_ID cid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetPackets )( 
            ITabletContext * This,
            /* [in] */ ULONG nBegin,
            /* [in] */ ULONG nEnd,
            /* [out][in] */ __RPC__inout ULONG *pcPkts,
            /* [in] */ ULONG cbPkts,
            /* [size_is][out] */ __RPC__out_ecount_full(cbPkts) BYTE *pbPkts,
            /* [out] */ __RPC__out CURSOR_ID *pCid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *PeekPackets )( 
            ITabletContext * This,
            /* [in] */ ULONG nBegin,
            /* [in] */ ULONG nEnd,
            /* [out][in] */ __RPC__inout ULONG *pcPkts,
            /* [in] */ ULONG cbPkts,
            /* [size_is][out] */ __RPC__out_ecount_full(cbPkts) BYTE *pbPkts,
            /* [out] */ __RPC__out CURSOR_ID *pCid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *FlushPackets )( 
            ITabletContext * This,
            /* [in] */ ULONG nBegin,
            /* [in] */ ULONG nEnd);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *FlushQueue )( 
            ITabletContext * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetPacketCount )( 
            ITabletContext * This,
            /* [in] */ ULONG nBegin,
            /* [in] */ ULONG nEnd,
            /* [out] */ __RPC__out ULONG *pcPkts);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetPacketQueueInfo )( 
            ITabletContext * This,
            /* [out] */ __RPC__out ULONG *pnBegin,
            /* [out] */ __RPC__out ULONG *pnEnd,
            /* [out] */ __RPC__out ULONG *pcPkts);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *ForwardPackets )( 
            ITabletContext * This,
            /* [in] */ ULONG nBegin,
            /* [in] */ ULONG nEnd);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *InjectPackets )( 
            ITabletContext * This,
            /* [in] */ ULONG cPkts,
            /* [in] */ ULONG cbPkts,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkts) BYTE *pbPkts,
            /* [size_is][in] */ __RPC__in_ecount_full(cPkts) CURSOR_ID *pCids);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *ModifyPackets )( 
            ITabletContext * This,
            /* [in] */ ULONG nBegin,
            /* [in] */ ULONG nEnd,
            /* [in] */ ULONG cbPkts,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkts) BYTE *pbPkts);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *ConvertToScreenCoordinates )( 
            ITabletContext * This,
            /* [in] */ ULONG cPkts,
            /* [in] */ ULONG cbPkts,
            /* [size_is][in] */ __RPC__in_ecount_full(cbPkts) BYTE *pbPkts,
            /* [size_is][out] */ __RPC__out_ecount_full(cPkts) POINT *pPointsInScreen);
        
        END_INTERFACE
    } ITabletContextVtbl;

    interface ITabletContext
    {
        CONST_VTBL struct ITabletContextVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITabletContext_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITabletContext_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITabletContext_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITabletContext_GetId(This,pTcid)	\
    ( (This)->lpVtbl -> GetId(This,pTcid) ) 

#define ITabletContext_GetWindow(This,pHwnd)	\
    ( (This)->lpVtbl -> GetWindow(This,pHwnd) ) 

#define ITabletContext_GetSettings(This,ppTCS)	\
    ( (This)->lpVtbl -> GetSettings(This,ppTCS) ) 

#define ITabletContext_GetTablet(This,ppTablet)	\
    ( (This)->lpVtbl -> GetTablet(This,ppTablet) ) 

#define ITabletContext_Enable(This,cet)	\
    ( (This)->lpVtbl -> Enable(This,cet) ) 

#define ITabletContext_GetOptions(This,pdwOptions)	\
    ( (This)->lpVtbl -> GetOptions(This,pdwOptions) ) 

#define ITabletContext_GetPacketDescription(This,ppPD)	\
    ( (This)->lpVtbl -> GetPacketDescription(This,ppPD) ) 

#define ITabletContext_GetStatus(This,pdwStatus)	\
    ( (This)->lpVtbl -> GetStatus(This,pdwStatus) ) 

#define ITabletContext_GetInputRect(This,prcInput)	\
    ( (This)->lpVtbl -> GetInputRect(This,prcInput) ) 

#define ITabletContext_SetInputRect(This,prcInput)	\
    ( (This)->lpVtbl -> SetInputRect(This,prcInput) ) 

#define ITabletContext_SetDevInputRect(This,prcInput)	\
    ( (This)->lpVtbl -> SetDevInputRect(This,prcInput) ) 

#define ITabletContext_GetDevInputRect(This,prcInput)	\
    ( (This)->lpVtbl -> GetDevInputRect(This,prcInput) ) 

#define ITabletContext_SetCapture(This)	\
    ( (This)->lpVtbl -> SetCapture(This) ) 

#define ITabletContext_ReleaseCapture(This)	\
    ( (This)->lpVtbl -> ReleaseCapture(This) ) 

#define ITabletContext_SetCursorCapture(This,cid)	\
    ( (This)->lpVtbl -> SetCursorCapture(This,cid) ) 

#define ITabletContext_ReleaseCursorCapture(This,cid)	\
    ( (This)->lpVtbl -> ReleaseCursorCapture(This,cid) ) 

#define ITabletContext_GetPackets(This,nBegin,nEnd,pcPkts,cbPkts,pbPkts,pCid)	\
    ( (This)->lpVtbl -> GetPackets(This,nBegin,nEnd,pcPkts,cbPkts,pbPkts,pCid) ) 

#define ITabletContext_PeekPackets(This,nBegin,nEnd,pcPkts,cbPkts,pbPkts,pCid)	\
    ( (This)->lpVtbl -> PeekPackets(This,nBegin,nEnd,pcPkts,cbPkts,pbPkts,pCid) ) 

#define ITabletContext_FlushPackets(This,nBegin,nEnd)	\
    ( (This)->lpVtbl -> FlushPackets(This,nBegin,nEnd) ) 

#define ITabletContext_FlushQueue(This)	\
    ( (This)->lpVtbl -> FlushQueue(This) ) 

#define ITabletContext_GetPacketCount(This,nBegin,nEnd,pcPkts)	\
    ( (This)->lpVtbl -> GetPacketCount(This,nBegin,nEnd,pcPkts) ) 

#define ITabletContext_GetPacketQueueInfo(This,pnBegin,pnEnd,pcPkts)	\
    ( (This)->lpVtbl -> GetPacketQueueInfo(This,pnBegin,pnEnd,pcPkts) ) 

#define ITabletContext_ForwardPackets(This,nBegin,nEnd)	\
    ( (This)->lpVtbl -> ForwardPackets(This,nBegin,nEnd) ) 

#define ITabletContext_InjectPackets(This,cPkts,cbPkts,pbPkts,pCids)	\
    ( (This)->lpVtbl -> InjectPackets(This,cPkts,cbPkts,pbPkts,pCids) ) 

#define ITabletContext_ModifyPackets(This,nBegin,nEnd,cbPkts,pbPkts)	\
    ( (This)->lpVtbl -> ModifyPackets(This,nBegin,nEnd,cbPkts,pbPkts) ) 

#define ITabletContext_ConvertToScreenCoordinates(This,cPkts,cbPkts,pbPkts,pPointsInScreen)	\
    ( (This)->lpVtbl -> ConvertToScreenCoordinates(This,cPkts,cbPkts,pbPkts,pPointsInScreen) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITabletContext_INTERFACE_DEFINED__ */


#ifndef __ITabletCursor_INTERFACE_DEFINED__
#define __ITabletCursor_INTERFACE_DEFINED__

/* interface ITabletCursor */
/* [unique][helpstring][uuid][object] */ 

typedef ITabletCursor *PTABLETCURSOR;


EXTERN_C const IID IID_ITabletCursor;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("EF9953C6-B472-4B02-9D22-D0E247ADE0E8")
    ITabletCursor : public IUnknown
    {
    public:
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetName( 
            /* [out] */ __RPC__deref_out_opt LPWSTR *ppwszName) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE IsInverted( void) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetId( 
            /* [out] */ __RPC__out CURSOR_ID *pCid) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetTablet( 
            /* [out] */ __RPC__deref_out_opt ITablet **ppTablet) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetButtonCount( 
            /* [out] */ __RPC__out ULONG *pcButtons) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetButton( 
            /* [in] */ ULONG iButton,
            /* [out] */ __RPC__deref_out_opt ITabletCursorButton **ppButton) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ITabletCursorVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITabletCursor * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITabletCursor * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITabletCursor * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetName )( 
            ITabletCursor * This,
            /* [out] */ __RPC__deref_out_opt LPWSTR *ppwszName);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *IsInverted )( 
            ITabletCursor * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetId )( 
            ITabletCursor * This,
            /* [out] */ __RPC__out CURSOR_ID *pCid);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetTablet )( 
            ITabletCursor * This,
            /* [out] */ __RPC__deref_out_opt ITablet **ppTablet);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetButtonCount )( 
            ITabletCursor * This,
            /* [out] */ __RPC__out ULONG *pcButtons);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetButton )( 
            ITabletCursor * This,
            /* [in] */ ULONG iButton,
            /* [out] */ __RPC__deref_out_opt ITabletCursorButton **ppButton);
        
        END_INTERFACE
    } ITabletCursorVtbl;

    interface ITabletCursor
    {
        CONST_VTBL struct ITabletCursorVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITabletCursor_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITabletCursor_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITabletCursor_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITabletCursor_GetName(This,ppwszName)	\
    ( (This)->lpVtbl -> GetName(This,ppwszName) ) 

#define ITabletCursor_IsInverted(This)	\
    ( (This)->lpVtbl -> IsInverted(This) ) 

#define ITabletCursor_GetId(This,pCid)	\
    ( (This)->lpVtbl -> GetId(This,pCid) ) 

#define ITabletCursor_GetTablet(This,ppTablet)	\
    ( (This)->lpVtbl -> GetTablet(This,ppTablet) ) 

#define ITabletCursor_GetButtonCount(This,pcButtons)	\
    ( (This)->lpVtbl -> GetButtonCount(This,pcButtons) ) 

#define ITabletCursor_GetButton(This,iButton,ppButton)	\
    ( (This)->lpVtbl -> GetButton(This,iButton,ppButton) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITabletCursor_INTERFACE_DEFINED__ */


#ifndef __ITabletCursorButton_INTERFACE_DEFINED__
#define __ITabletCursorButton_INTERFACE_DEFINED__

/* interface ITabletCursorButton */
/* [unique][helpstring][uuid][object] */ 

typedef ITabletCursorButton *PTABLETCURSORBUTTON;


EXTERN_C const IID IID_ITabletCursorButton;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("997A992E-8B6C-4945-BC17-A1EE563B3AB7")
    ITabletCursorButton : public IUnknown
    {
    public:
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetName( 
            /* [out] */ __RPC__deref_out_opt LPWSTR *ppwszName) = 0;
        
        virtual /* [helpstring] */ HRESULT STDMETHODCALLTYPE GetGuid( 
            /* [out] */ __RPC__out GUID *pguidBtn) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct ITabletCursorButtonVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            ITabletCursorButton * This,
            /* [in] */ __RPC__in REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            ITabletCursorButton * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            ITabletCursorButton * This);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetName )( 
            ITabletCursorButton * This,
            /* [out] */ __RPC__deref_out_opt LPWSTR *ppwszName);
        
        /* [helpstring] */ HRESULT ( STDMETHODCALLTYPE *GetGuid )( 
            ITabletCursorButton * This,
            /* [out] */ __RPC__out GUID *pguidBtn);
        
        END_INTERFACE
    } ITabletCursorButtonVtbl;

    interface ITabletCursorButton
    {
        CONST_VTBL struct ITabletCursorButtonVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define ITabletCursorButton_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define ITabletCursorButton_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define ITabletCursorButton_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define ITabletCursorButton_GetName(This,ppwszName)	\
    ( (This)->lpVtbl -> GetName(This,ppwszName) ) 

#define ITabletCursorButton_GetGuid(This,pguidBtn)	\
    ( (This)->lpVtbl -> GetGuid(This,pguidBtn) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __ITabletCursorButton_INTERFACE_DEFINED__ */



#ifndef __TABLETLib_LIBRARY_DEFINED__
#define __TABLETLib_LIBRARY_DEFINED__

/* library TABLETLib */
/* [helpstring][version][uuid] */ 








EXTERN_C const IID LIBID_TABLETLib;

EXTERN_C const CLSID CLSID_TabletManager;

#ifdef __cplusplus

class DECLSPEC_UUID("786CDB70-1628-44A0-853C-5D340A499137")
TabletManager;
#endif
#endif /* __TABLETLib_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

unsigned long             __RPC_USER  HWND_UserSize(     unsigned long *, unsigned long            , HWND * ); 
unsigned char * __RPC_USER  HWND_UserMarshal(  unsigned long *, unsigned char *, HWND * ); 
unsigned char * __RPC_USER  HWND_UserUnmarshal(unsigned long *, unsigned char *, HWND * ); 
void                      __RPC_USER  HWND_UserFree(     unsigned long *, HWND * ); 

unsigned long             __RPC_USER  HWND_UserSize64(     unsigned long *, unsigned long            , HWND * ); 
unsigned char * __RPC_USER  HWND_UserMarshal64(  unsigned long *, unsigned char *, HWND * ); 
unsigned char * __RPC_USER  HWND_UserUnmarshal64(unsigned long *, unsigned char *, HWND * ); 
void                      __RPC_USER  HWND_UserFree64(     unsigned long *, HWND * ); 

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif




