// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "precomp.hpp"
#include "WmpEventHandler.tmh"

MtDefine(CWmpEventHandler, Mem, "CWmpEventHandler");
MtDefine(AVEvent, Mem, "AVEvent");

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::CWmpEventHandler
//
//  Synopsis: constructor
//
//  Returns: A new instance of CWmpEventHandler
//
//------------------------------------------------------------------------------
CWmpEventHandler::CWmpEventHandler(
    __in    MediaInstance       *pMediaInstance,
    __in    CWmpStateEngine     *pStateEngine
    ) : m_uiID(pMediaInstance->GetID()),
        m_pMediaInstance(NULL),
        m_pStateEngine(NULL),
        m_fBuffering(false)
{
    TRACEF(NULL);

    AddRef();
    CD3DLoader::GetLoadRef();

    SetInterface(m_pMediaInstance, pMediaInstance);
    SetInterface(m_pStateEngine, pStateEngine);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::~CWmpEventHandler
//
//  Synopsis: destructor
//
//------------------------------------------------------------------------------
CWmpEventHandler::~CWmpEventHandler()
{
    TRACEF(NULL);

    ReleaseInterface(m_pMediaInstance);
    ReleaseInterface(m_pStateEngine);
    CD3DLoader::ReleaseLoadRef();
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::Create
//
//  Synopsis: public interface for creating a new instance of CWmpEventHandler
//
//------------------------------------------------------------------------------
/*static*/
HRESULT
CWmpEventHandler::
Create(
    __in            MediaInstance       *pMediaInstance,
    __in            CWmpStateEngine     *pStateEngine,
    __deref_out     CWmpEventHandler** ppEventHandler
    )
{
    HRESULT hr = S_OK;
    CHECKPTRARG(ppEventHandler);
    *ppEventHandler = new CWmpEventHandler(pMediaInstance, pStateEngine);
    IFCOOM(*ppEventHandler);

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::HrFindInterface, CMILCOMBase
//
//  Synopsis: Get a pointer to another interface implemented by
//      CWmpEventHandler
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpEventHandler::HrFindInterface(
    __in_ecount(1) REFIID riid,
    __deref_out void **ppvObject
    )
{
    HRESULT hr = S_OK;

    if (!ppvObject)
    {
        IFCN(E_INVALIDARG);
    }

    if (riid == __uuidof(IWMPEvents))
    {
        *ppvObject = static_cast<IWMPEvents*>(this);
    }
    else if (riid == __uuidof(_WMPOCXEvents))
    {
        *ppvObject = static_cast<_WMPOCXEvents*>(this);
    }
    else
    {
        LogAVDataM(
            AVTRACE_LEVEL_ERROR,
            AVCOMP_EVENTS,
            "Unexpected interface request: %!IID!",
            &riid);

        IFCN(E_NOINTERFACE);
    }

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::Invoke, IDispatch
//
//  Synopsis: Invoke a member function of CWmpEventHandler
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpEventHandler::Invoke(
    __in DISPID  dispIdMember,
    __in REFIID  riid,
    __in LCID  lcid,
    __in WORD  wFlags,
    __in_ecount(1)  DISPPARAMS FAR*  pDispParams,
    __in VARIANT FAR*  pVarResult,
    __in EXCEPINFO FAR*  pExcepInfo,
    __out_ecount(1) unsigned int FAR*  puArgErr
    )
{
    HRESULT hr = S_OK;

    CHECKPTRARG(pDispParams);
    if (pDispParams->cNamedArgs != 0)
    {
        IFC(DISP_E_NONAMEDARGS);
    }


    switch (dispIdMember)
    {
        case DISPID_WMPCOREEVENT_OPENSTATECHANGE:
            OpenStateChange(pDispParams->rgvarg[0].lVal /* NewState */ );
            break;

        case DISPID_WMPCOREEVENT_PLAYSTATECHANGE:
            PlayStateChange(pDispParams->rgvarg[0].lVal /* NewState */);
            break;

        case DISPID_WMPCOREEVENT_AUDIOLANGUAGECHANGE:
            AudioLanguageChange(pDispParams->rgvarg[0].lVal /* LangID */);
            break;

        case DISPID_WMPCOREEVENT_STATUSCHANGE:
            StatusChange();
            break;

        case DISPID_WMPCOREEVENT_SCRIPTCOMMAND:
            ScriptCommand(pDispParams->rgvarg[1].bstrVal /* scType */, pDispParams->rgvarg[0].bstrVal /* Param */ );
            break;

        case DISPID_WMPCOREEVENT_NEWSTREAM:
            NewStream();
            break;

        case DISPID_WMPCOREEVENT_DISCONNECT:
            Disconnect(pDispParams->rgvarg[0].lVal /* Result */ );
            break;

        case DISPID_WMPCOREEVENT_BUFFERING:
            Buffering(pDispParams->rgvarg[0].boolVal /* Start */);
            break;

        case DISPID_WMPCOREEVENT_ERROR:
            Error();
            break;

        case DISPID_WMPCOREEVENT_WARNING:
            Warning(pDispParams->rgvarg[1].lVal /* WarningType */, pDispParams->rgvarg[0].lVal /* Param */, pDispParams->rgvarg[2].bstrVal /* Description */);
            break;

        case DISPID_WMPCOREEVENT_ENDOFSTREAM:
            EndOfStream(pDispParams->rgvarg[0].lVal /* Result */ );
            break;

        case DISPID_WMPCOREEVENT_POSITIONCHANGE:
            PositionChange(pDispParams->rgvarg[1].dblVal /* oldPosition */, pDispParams->rgvarg[0].dblVal /* newPosition */);
            break;

        case DISPID_WMPCOREEVENT_MARKERHIT:
            MarkerHit(pDispParams->rgvarg[0].lVal /* MarkerNum */);
            break;

        case DISPID_WMPCOREEVENT_DURATIONUNITCHANGE:
            DurationUnitChange(pDispParams->rgvarg[0].lVal /* NewDurationUnit */);
            break;

        case DISPID_WMPCOREEVENT_CDROMMEDIACHANGE:
            CdromMediaChange(pDispParams->rgvarg[0].lVal /* CdromNum */);
            break;

        case DISPID_WMPCOREEVENT_PLAYLISTCHANGE:
            PlaylistChange(pDispParams->rgvarg[1].pdispVal /* Playlist */, (WMPPlaylistChangeEventType) pDispParams->rgvarg[0].lVal /* change */);
            break;

        case DISPID_WMPCOREEVENT_CURRENTPLAYLISTCHANGE:
            CurrentPlaylistChange((WMPPlaylistChangeEventType) pDispParams->rgvarg[0].lVal /* change */);
            break;

        case DISPID_WMPCOREEVENT_CURRENTPLAYLISTITEMAVAILABLE:
            CurrentPlaylistItemAvailable(pDispParams->rgvarg[0].bstrVal /*  bstrItemName */);
            break;

        case DISPID_WMPCOREEVENT_MEDIACHANGE:
            MediaChange(pDispParams->rgvarg[0].pdispVal /* Item */);
            break;

        case DISPID_WMPCOREEVENT_CURRENTMEDIAITEMAVAILABLE:
            CurrentMediaItemAvailable(pDispParams->rgvarg[0].bstrVal /* bstrItemName */);
            break;

        case DISPID_WMPCOREEVENT_CURRENTITEMCHANGE:
            CurrentItemChange(pDispParams->rgvarg[0].pdispVal /* pdispMedia */);
            break;

        case DISPID_WMPCOREEVENT_MEDIACOLLECTIONCHANGE:
            MediaCollectionChange();
            break;

        case DISPID_WMPCOREEVENT_MEDIACOLLECTIONATTRIBUTESTRINGADDED:
            MediaCollectionAttributeStringAdded(pDispParams->rgvarg[1].bstrVal /* bstrAttribName */, pDispParams->rgvarg[0].bstrVal /* bstrAttribVal */ );
            break;

        case DISPID_WMPCOREEVENT_MEDIACOLLECTIONATTRIBUTESTRINGREMOVED:
            MediaCollectionAttributeStringRemoved(pDispParams->rgvarg[1].bstrVal /* bstrAttribName */, pDispParams->rgvarg[0].bstrVal /* bstrAttribVal */ );
            break;

        case DISPID_WMPCOREEVENT_MEDIACOLLECTIONATTRIBUTESTRINGCHANGED:
            MediaCollectionAttributeStringChanged(pDispParams->rgvarg[2].bstrVal /* bstrAttribName */, pDispParams->rgvarg[1].bstrVal /* bstrOldAttribVal */, pDispParams->rgvarg[0].bstrVal /* bstrNewAttribVal */);
            break;

        case DISPID_WMPCOREEVENT_PLAYLISTCOLLECTIONCHANGE:
            PlaylistCollectionChange();
            break;

        case DISPID_WMPCOREEVENT_PLAYLISTCOLLECTIONPLAYLISTADDED:
            PlaylistCollectionPlaylistAdded(pDispParams->rgvarg[0].bstrVal /* bstrPlaylistName */ );
            break;

        case DISPID_WMPCOREEVENT_PLAYLISTCOLLECTIONPLAYLISTREMOVED:
            PlaylistCollectionPlaylistRemoved(pDispParams->rgvarg[0].bstrVal /* bstrPlaylistName */ );
            break;

        case DISPID_WMPCOREEVENT_PLAYLISTCOLLECTIONPLAYLISTSETASDELETED:
            PlaylistCollectionPlaylistSetAsDeleted(pDispParams->rgvarg[1].bstrVal /* bstrPlaylistName */, pDispParams->rgvarg[0].boolVal /* varfIsDeleted */);
            break;

        case DISPID_WMPCOREEVENT_MODECHANGE:
            ModeChange(pDispParams->rgvarg[1].bstrVal /* ModeName */, pDispParams->rgvarg[0].boolVal /* NewValue */);
            break;

        case DISPID_WMPCOREEVENT_MEDIAERROR:
            MediaError(pDispParams->rgvarg[0].pdispVal /* pMediaObject */);
            break;

        case DISPID_WMPCOREEVENT_OPENPLAYLISTSWITCH:
            OpenPlaylistSwitch(pDispParams->rgvarg[0].pdispVal /* pItem */);
            break;

        case DISPID_WMPCOREEVENT_DOMAINCHANGE:
            DomainChange(pDispParams->rgvarg[0].bstrVal /* strDomain */);
            break;

        case DISPID_WMPOCXEVENT_SWITCHEDTOPLAYERAPPLICATION:
            SwitchedToPlayerApplication();
            break;

        case DISPID_WMPOCXEVENT_SWITCHEDTOCONTROL:
            SwitchedToControl();
            break;

        case DISPID_WMPOCXEVENT_PLAYERDOCKEDSTATECHANGE:
            PlayerDockedStateChange();
            break;

        case DISPID_WMPOCXEVENT_PLAYERRECONNECT:
            PlayerReconnect();
            break;

        case DISPID_WMPOCXEVENT_CLICK:
            Click(pDispParams->rgvarg[3].iVal /* nButton */, pDispParams->rgvarg[2].iVal /* nShiftState */,  pDispParams->rgvarg[1].lVal /* fX */,  pDispParams->rgvarg[0].lVal /* fY */);
            break;

        case DISPID_WMPOCXEVENT_DOUBLECLICK:
            DoubleClick(pDispParams->rgvarg[3].iVal /* nButton */, pDispParams->rgvarg[2].iVal /* nShiftState */,  pDispParams->rgvarg[1].lVal /* fX */,  pDispParams->rgvarg[0].lVal /* fY */);
            break;

        case DISPID_WMPOCXEVENT_KEYDOWN:
            KeyDown(pDispParams->rgvarg[1].iVal /* nKeyCode */, pDispParams->rgvarg[0].iVal /* nShiftState */);
            break;

        case DISPID_WMPOCXEVENT_KEYPRESS:
            KeyPress(pDispParams->rgvarg[0].iVal /* nKeyAscii */);
            break;

        case DISPID_WMPOCXEVENT_KEYUP:
            KeyUp(pDispParams->rgvarg[1].iVal /* nKeyCode */, pDispParams->rgvarg[0].iVal /* nShiftState */);
            break;

        case DISPID_WMPOCXEVENT_MOUSEDOWN:
            MouseDown(pDispParams->rgvarg[3].iVal /* nButton */, pDispParams->rgvarg[2].iVal /* nShiftState */,  pDispParams->rgvarg[1].lVal /* fX */,  pDispParams->rgvarg[0].lVal /* fY */);
            break;

        case DISPID_WMPOCXEVENT_MOUSEMOVE:
            MouseMove(pDispParams->rgvarg[3].iVal /* nButton */, pDispParams->rgvarg[2].iVal /* nShiftState */,  pDispParams->rgvarg[1].lVal /* fX */,  pDispParams->rgvarg[0].lVal /* fY */);
            break;

        case DISPID_WMPOCXEVENT_MOUSEUP:
            MouseUp(pDispParams->rgvarg[3].iVal /* nButton */, pDispParams->rgvarg[2].iVal /* nShiftState */,  pDispParams->rgvarg[1].lVal /* fX */,  pDispParams->rgvarg[0].lVal /* fY */);
            break;

        default:
            IFC(DISP_E_MEMBERNOTFOUND);
    }

Cleanup:
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::OpenStateChange, IWMPEvents
//
//  Synopsis: Sent when the control changes OpenState
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::OpenStateChange(long newState)
{
    TRACEF(NULL);
    WMPOpenState state = static_cast<WMPOpenState>(newState);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_EVENTS,
        "Received OpenStateChange(%!wmpopen!)",
        state);

    //
    // Signal the state engine only if we are still connection. This is to handle
    // running down outstanding state-arc during close.
    //
    if (m_pStateEngine)
    {
        m_pStateEngine->PlayerReachedOpenState(state);

        switch (state)
        {
            case wmposMediaOpen:
                RaiseEvent(AVMediaOpened);
                break;
            default:
                ; // do nothing
        }
    }
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::PlayStateChange, IWMPEvents
//
//  Synopsis: Sent when the control changes PlayState
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::PlayStateChange(long newState)
{
    TRACEF(NULL);
    WMPPlayState state = static_cast<WMPPlayState>(newState);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_EVENTS,
        "Received PlayStateChange(%!wmpplay!)",
        state);

    if (m_pStateEngine)
    {
        m_pStateEngine->PlayerReachedActionState(state);

        if (state != wmppsBuffering && m_fBuffering)
        {
            m_fBuffering = false;
            RaiseEvent(AVMediaBufferingEnded);
        }
        else if (state == wmppsBuffering && !m_fBuffering)
        {
            m_fBuffering = true;
            RaiseEvent(AVMediaBufferingStarted);
        }
    }
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::AudioLanguageChange, IWMPEvents
//
//  Synopsis: Sent when the audio language changes
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::AudioLanguageChange(long LangID)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::StatusChange, IWMPEvents
//
//  Synopsis: Sent when the status string changes
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::StatusChange()
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::ScriptCommand, IWMPEvents
//
//  Synopsis: Sent when a synchronized command or URL is received
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::ScriptCommand(
    BSTR scType,
        // The type of scripting event being sent
    BSTR Param
        // The parameter information associated with the type
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    hr = m_pMediaInstance->GetMediaEventProxy().RaiseEvent(
                AVMediaScriptCommand,
                scType,
                Param);

}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::NewStream, IWMPEvents
//
//  Synopsis: Sent when a new stream is encountered (obsolete)
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::NewStream()
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::Disconnect, IWMPEvents
//
//  Synopsis: Sent when the control is disconnected from the server (obsolete)
//
//------------------------------------------------------------------------------
void
CWmpEventHandler:: Disconnect(long Result )
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::Buffering, IWMPEvents
//
//  Synopsis: Sent when the control begins or ends buffering
//
//------------------------------------------------------------------------------
void
CWmpEventHandler:: Buffering(VARIANT_BOOL Start)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::Error, IWMPEvents
//
//  Synopsis: Sent when the control has an error condition
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::Error()
{
    TRACEF(NULL);

    //
    // Calls to Error will always be accompanied by calls to MediaError,
    // so it's safe to ignore this call.
    //

    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::Warning, IWMPEvents
//
//  Synopsis: Sent when the control has an warning condition (obsolete)
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::Warning(long WarningType, long Param, BSTR Description)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::EndOfStream, IWMPEvents
//
//  Synopsis: Sent when the media has reached end of stream
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::EndOfStream(long Result)
{
    TRACEF(NULL);

    //
    // Only raise this if we haven't been closed.
    //
    if (m_pStateEngine)
    {
        RaiseEvent(AVMediaEnded);
    }

    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::PositionChange, IWMPEvents
//
//  Synopsis: Indicates that the current position of the movie has changed
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::PositionChange(double oldPosition,double newPosition)
{
    TRACEF(NULL);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_EVENTS,
        "Received PositionChange(%f, %f)",
        oldPosition,
        newPosition);

    if (m_pStateEngine)
    {
        m_pStateEngine->PlayerReachedPosition(newPosition);
    }

    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::MarkerHit, IWMPEvents
//
//  Synopsis: Sent when a marker is reached
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::MarkerHit(long MarkerNum )
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::DurationUnitChange, IWMPEvents
//
//  Synopsis: Indicates that the unit used to express duration and position has
//      changed
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::DurationUnitChange(long NewDurationUnit)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::CdromMediaChange, IWMPEvents
//
//  Synopsis: Indicates that the CD ROM media has changed
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::CdromMediaChange(long CdromNum)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::PlaylistChange, IWMPEvents
//
//  Synopsis: Sent when a playlist changes
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::PlaylistChange(__in_ecount(1) IDispatch * Playlist,WMPPlaylistChangeEventType change)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::CurrentPlaylistChange, IWMPEvents
//
//  Synopsis: Sent when the current playlist changes
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::CurrentPlaylistChange(WMPPlaylistChangeEventType change )
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::CurrentPlaylistItemAvailable, IWMPEvents
//
//  Synopsis: Sent when a current playlist item becomes available
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::CurrentPlaylistItemAvailable(BSTR bstrItemName)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::MediaChange, IWMPEvents
//
//  Synopsis: Sent when a media object changes
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::MediaChange(__in_ecount(1) IDispatch * Item)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::CurrentMediaItemAvailable, IWMPEvents
//
//  Synopsis: Sent when a current media item becomes available
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::CurrentMediaItemAvailable(BSTR bstrItemName)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::CurrentItemChange, IWMPEvents
//
//  Synopsis: Sent when the item selection on the current playlist changes
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::CurrentItemChange(__in_ecount(1) IDispatch *pdispMedia)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::MediaCollectionChange, IWMPEvents
//
//  Synopsis: Sent when the media collection needs to be requeried
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::MediaCollectionChange()
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::MediaCollectionAttributeStringAdded, IWMPEvents
//
//  Synopsis: Sent when an attribute string is added in the media collection
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::MediaCollectionAttributeStringAdded(
    BSTR bstrAttribName,
    BSTR bstrAttribVal
    )
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::MediaCollectionAttributeStringRemoved,
//      IWMPEvents
//
//  Synopsis: Sent when an attribute string is removed from the media collection
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::MediaCollectionAttributeStringRemoved(
    BSTR bstrAttribName,
    BSTR bstrAttribVal
    )
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::MediaCollectionAttributeStringChanged,
//      IWMPEvents
//
//  Synopsis: Sent when an attribute string is changed in the media collection
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::MediaCollectionAttributeStringChanged(
    BSTR bstrAttribName,
    BSTR bstrOldAttribVal,
    BSTR bstrNewAttribVal
    )
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::PlaylistCollectionChange, IWMPEvents
//
//  Synopsis: Sent when playlist collection needs to be requeried
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::PlaylistCollectionChange()
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::PlaylistCollectionPlaylistAdded, IWMPEvents
//
//  Synopsis: Sent when a playlist is added to the playlist collection
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::PlaylistCollectionPlaylistAdded(BSTR bstrPlaylistName)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::PlaylistCollectionPlaylistRemoved, IWMPEvents
//
//  Synopsis: Sent when a playlist is removed from the playlist collection
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::PlaylistCollectionPlaylistRemoved(BSTR bstrPlaylistName)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::PlaylistCollectionPlaylistSetAsDeleted,
//      IWMPEvents
//
//  Synopsis: Sent when a playlist has been set or reset as deleted
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::PlaylistCollectionPlaylistSetAsDeleted(
    BSTR bstrPlaylistName,
    VARIANT_BOOL varfIsDeleted
    )
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::ModeChange, IWMPEvents
//
//  Synopsis: Playlist playback mode has changed
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::ModeChange(BSTR ModeName, VARIANT_BOOL NewValue)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::MediaError, IWMPEvents
//
//  Synopsis: Sent when the media object has an error condition
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::MediaError(__in_ecount(1) IDispatch * pMediaObject)
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    IWMPMedia2      *pIMedia2        = NULL;
    IWMPErrorItem   *pIErrorItem     = NULL;
    HRESULT         failureHR        = S_OK;

    IFC(
        pMediaObject->QueryInterface(
                            __uuidof(IWMPMedia2),
                            reinterpret_cast<void **>(&pIMedia2)));

    IFC(pIMedia2->get_error(&pIErrorItem));

    //
    //
    // We seem to be seeing cases where errors are propagated incorrectly in the
    // latest builds. Revisit whether we should otherwise expect NULL in this case.
    //
    if (pIErrorItem)
    {
        IFC(pIErrorItem->get_errorCode(&failureHR));

        IFC(RaiseEvent(AVMediaFailed, failureHR));
    }

Cleanup:

    ReleaseInterface(pIMedia2);
    ReleaseInterface(pIErrorItem);

    IGNORE_HR(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::OpenPlaylistSwitch, IWMPEvents
//
//  Synopsis: Current playlist switch with no open state change
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::OpenPlaylistSwitch(__in_ecount(1) IDispatch *pItem)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::DomainChange, IWMPEvents
//
//  Synopsis: Sent when the current DVD domain changes
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::DomainChange(BSTR strDomain)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::SwitchedToPlayerApplication, IWMPEvents
//
//  Synopsis: Sent when display switches to player application
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::SwitchedToPlayerApplication()
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::SwitchedToControl, IWMPEvents
//
//  Synopsis: Sent when display switches to control
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::SwitchedToControl()
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::PlayerDockedStateChange, IWMPEvents
//
//  Synopsis: Sent when the player docks or undocks
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::PlayerDockedStateChange()
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::PlayerReconnect, IWMPEvents
//
//  Synopsis: Sent when the OCX reconnects to the player
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::PlayerReconnect()
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::Click, IWMPEvents
//
//  Synopsis: Occurs when a user clicks the mouse
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::Click(short nButton, short nShiftState, long fX, long fY)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::DoubleClick, IWMPEvents
//
//  Synopsis: Occurs when a user double-clicks the mouse
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::DoubleClick(short nButton, short nShiftState, long fX, long fY)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::KeyDown, IWMPEvents
//
//  Synopsis: Occurs when a key is pressed
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::KeyDown(short nKeyCode, short nShiftState)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::KeyPress, IWMPEvents
//
//  Synopsis: Occurs when a key is pressed and released
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::KeyPress(short nKeyAscii)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::KeyUp, IWMPEvents
//
//  Synopsis: Occurs when a key is released
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::KeyUp(short nKeyCode, short nShiftState)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::MouseDown, IWMPEvents
//
//  Synopsis: Occurs when a mouse button is pressed
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::MouseDown(
    short nButton,
    short nShiftState,
    long fX,
    long fY
    )
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::MouseMove, IWMPEvents
//
//  Synopsis: Occurs when a mouse pointer is moved
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::MouseMove(
    short nButton,
    short nShiftState,
    long fX,
    long fY
    )
{
    TRACEF(NULL);
    return;
}


//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::DisconnectStateEngine
//
//  Synopsis: Called when we are closing media.
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::DisconnectStateEngine(
    void
    )
{
    ReleaseInterface(m_pStateEngine);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::MouseUp, IWMPEvents
//
//  Synopsis: Occurs when a mouse button is released
//
//------------------------------------------------------------------------------
void
CWmpEventHandler::MouseUp(short nButton, short nShiftState, long fX, long fY)
{
    TRACEF(NULL);
    return;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpEventHandler::RaiseEvent
//
//  Synopsis: Raise an event in managed code by sending it through the proxy
//
//------------------------------------------------------------------------------
HRESULT
CWmpEventHandler::RaiseEvent(
    __in    AVEvent avEventType,
    __in    HRESULT failureHr
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    IFC(m_pMediaInstance->GetMediaEventProxy().RaiseEvent(avEventType, failureHr));

Cleanup:

    RRETURN(hr);
}


