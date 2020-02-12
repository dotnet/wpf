// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//+-----------------------------------------------------------------------------
//

//
//  $TAG ENGR

//      $Module:    win_mil_graphics_media
//      $Keywords:
//
//  $Description:
//      Header for the CWmpEventHandler class, which handles event callbacks
//      from WMP.
//
//  $ENDTAG
//
//------------------------------------------------------------------------------
#pragma once


MtExtern(CWmpEventHandler);

class CWmpStateEngine;
class CMediaEventProxy;

class CWmpEventHandler :
    public CMILCOMBase,
    public IWMPEvents,
    public _WMPOCXEvents
{
public:
    DECLARE_METERHEAP_CLEAR(ProcessHeap, Mt(CWmpEventHandler));

    DECLARE_COM_BASE; // CMILCOMBase

    static
    HRESULT
    Create(
        __in            MediaInstance       *pMediaInstance,
        __in            CWmpStateEngine     *pStateEngine,
        __deref_out     CWmpEventHandler    **ppEventHandler
        );


    // IDispatch methods
    STDMETHOD(GetIDsOfNames)(
        __in REFIID riid,
        __in_ecount(cNames) OLECHAR FAR *FAR *rgszNames,
        __in unsigned int cNames,
        __in LCID lcid,
        __out_ecount(cNames) DISPID FAR *rgDispId )
    { return( E_NOTIMPL ); }

    STDMETHOD(GetTypeInfo)(
        __in unsigned int iTInfo,
        __in LCID lcid,
        __out_ecount(1) ITypeInfo FAR *FAR *ppTInfo )
    { return( E_NOTIMPL ); }

    STDMETHOD(GetTypeInfoCount)(
        __out_ecount(1) unsigned int FAR *pctinfo )
    {
        return( E_NOTIMPL );
    }

    STDMETHOD(Invoke)(
        __in DISPID  dispIdMember,
        __in REFIID  riid,
        __in LCID  lcid,
        __in WORD  wFlags,
        __in_ecount(1) DISPPARAMS FAR*  pDispParams,
        __in VARIANT FAR*  pVarResult,
        __in EXCEPINFO FAR*  pExcepInfo,
        __out_ecount(1) unsigned int FAR*  puArgErr );

    // IWMPEvents methods
    void STDMETHODCALLTYPE OpenStateChange( long NewState );
    void STDMETHODCALLTYPE PlayStateChange( long NewState );
    void STDMETHODCALLTYPE AudioLanguageChange( long LangID );
    void STDMETHODCALLTYPE StatusChange();
    void STDMETHODCALLTYPE ScriptCommand( BSTR scType, BSTR Param );
    void STDMETHODCALLTYPE NewStream();
    void STDMETHODCALLTYPE Disconnect( long Result );
    void STDMETHODCALLTYPE Buffering( VARIANT_BOOL Start );
    void STDMETHODCALLTYPE Error();
    void STDMETHODCALLTYPE Warning( long WarningType, long Param, BSTR Description );
    void STDMETHODCALLTYPE EndOfStream( long Result );
    void STDMETHODCALLTYPE PositionChange( double oldPosition, double newPosition);
    void STDMETHODCALLTYPE MarkerHit( long MarkerNum );
    void STDMETHODCALLTYPE DurationUnitChange( long NewDurationUnit );
    void STDMETHODCALLTYPE CdromMediaChange( long CdromNum );
    void STDMETHODCALLTYPE PlaylistChange( __in_ecount(1) IDispatch * Playlist, WMPPlaylistChangeEventType change );
    void STDMETHODCALLTYPE CurrentPlaylistChange( WMPPlaylistChangeEventType change );
    void STDMETHODCALLTYPE CurrentPlaylistItemAvailable( BSTR bstrItemName );
    void STDMETHODCALLTYPE MediaChange( __in_ecount(1) IDispatch * Item );
    void STDMETHODCALLTYPE CurrentMediaItemAvailable( BSTR bstrItemName );
    void STDMETHODCALLTYPE CurrentItemChange( __in_ecount(1) IDispatch *pdispMedia);
    void STDMETHODCALLTYPE MediaCollectionChange();
    void STDMETHODCALLTYPE MediaCollectionAttributeStringAdded( BSTR bstrAttribName,  BSTR bstrAttribVal );
    void STDMETHODCALLTYPE MediaCollectionAttributeStringRemoved( BSTR bstrAttribName,  BSTR bstrAttribVal );
    void STDMETHODCALLTYPE MediaCollectionAttributeStringChanged( BSTR bstrAttribName, BSTR bstrOldAttribVal, BSTR bstrNewAttribVal);
    void STDMETHODCALLTYPE PlaylistCollectionChange();
    void STDMETHODCALLTYPE PlaylistCollectionPlaylistAdded( BSTR bstrPlaylistName);
    void STDMETHODCALLTYPE PlaylistCollectionPlaylistRemoved( BSTR bstrPlaylistName);
    void STDMETHODCALLTYPE PlaylistCollectionPlaylistSetAsDeleted( BSTR bstrPlaylistName, VARIANT_BOOL varfIsDeleted);
    void STDMETHODCALLTYPE ModeChange( BSTR ModeName, VARIANT_BOOL NewValue);
    void STDMETHODCALLTYPE MediaError( __in_ecount(1) IDispatch * pMediaObject);
    void STDMETHODCALLTYPE OpenPlaylistSwitch( __in_ecount(1) IDispatch *pItem );
    void STDMETHODCALLTYPE DomainChange( BSTR strDomain);
    void STDMETHODCALLTYPE SwitchedToPlayerApplication();
    void STDMETHODCALLTYPE SwitchedToControl();
    void STDMETHODCALLTYPE PlayerDockedStateChange();
    void STDMETHODCALLTYPE PlayerReconnect();
    void STDMETHODCALLTYPE Click( short nButton, short nShiftState, long fX, long fY );
    void STDMETHODCALLTYPE DoubleClick( short nButton, short nShiftState, long fX, long fY );
    void STDMETHODCALLTYPE KeyDown( short nKeyCode, short nShiftState );
    void STDMETHODCALLTYPE KeyPress( short nKeyAscii );
    void STDMETHODCALLTYPE KeyUp( short nKeyCode, short nShiftState );
    void STDMETHODCALLTYPE MouseDown( short nButton, short nShiftState, long fX, long fY );
    void STDMETHODCALLTYPE MouseMove( short nButton, short nShiftState, long fX, long fY );
    void STDMETHODCALLTYPE MouseUp( short nButton, short nShiftState, long fX, long fY );

    //
    // Normal methods
    //
    void
    DisconnectStateEngine(
        void
        );


protected:
    // CMILCOMBase
    STDMETHOD(HrFindInterface)(__in_ecount(1) REFIID riid, __deref_out void **ppv);

private:
    CWmpEventHandler(
        __in        MediaInstance       *pMediaInstance,
        __in        CWmpStateEngine     *pStateEngine
        );

    virtual
    ~CWmpEventHandler(
        void
        );

    HRESULT
    RaiseEvent(
        __in        AVEvent         avEventType,
        __in        HRESULT         hr = S_OK
        );

    UINT m_uiID;
    MediaInstance       *m_pMediaInstance;
    CWmpStateEngine     *m_pStateEngine;
    bool m_fBuffering;
};


