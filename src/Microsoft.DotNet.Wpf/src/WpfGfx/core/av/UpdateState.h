// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#pragma once

MtExtern(UpdateState);

//
// UpdateState is a CStateThreadItem whose Run method updates CWmpStateEngine.
// WmpPlayer sets the desired state and calls CWmpStateEngine::AddItem so
// that UpdateState::Run is called from the apartment thread.
//
class UpdateState : public CStateThreadItem,
                    public CMILCOMBase
{
public:
    DECLARE_METERHEAP_CLEAR(ProcessHeap, Mt(UpdateState));

    DECLARE_COM_BASE;

    static
    HRESULT
    Create(
        __in    MediaInstance       *pMediaInstance,
        __in    CWmpStateEngine     *pCWmpStateEngine,
        __in    UpdateState         **ppUpdateState
        );

    void
    OpenHelper(
        __in LPCWSTR pwszURL
        );

    void
    SetRateHelper(
        __in double dRate
        );

    void
    SetTargetActionState(
        __in    ActionState::Enum   targetActionState
        );

    void
    SetTargetVolume(
        __in    long                targetVolume
        );

    void
    SetTargetBalance(
        __in    long                targetBalance
        );

    void
    SetTargetSeekTo(
        __in    double              targetSeekTo
        );

    void
    SetTargetIsScrubbingEnabled(
        __in    bool                isScrubbingEnabled
        );

    void
    UpdateTransients(
        void
        );

    void
    Close(
        void
        );

    HRESULT
    UpdateTransientsSync(
        __in    DWORD       timeOutInMilliseconds,
        __out   bool        *pDidTimeOut
        );

protected:
    //
    // CMILCOMBase
    //
    STDMETHOD(HrFindInterface)(
        __in_ecount(1) REFIID riid,
        __deref_out void **ppvObject
        );

    //
    // CStateThreadItem
    //
    __override
    void
    Run(
        void
        );

private:
    UpdateState(
        __in    MediaInstance   *pMediaInstance,
        __in    CWmpStateEngine *pCWmpStateEngine
        );

    ~UpdateState(
        void
        );

    HRESULT
    Init(
        void
        );

    CCriticalSection            m_lock;
    UINT                        m_uiID;
    MediaInstance               *m_pMediaInstance;
    CWmpStateEngine             *m_pCWmpStateEngine;
    HANDLE                      m_waitEvent;

    Optional<ActionState::Enum> m_targetActionState;
    Optional<bool>              m_targetOcx;
    Optional<double>            m_targetRate;
    Optional<PWSTR>             m_targetUrl;
    Optional<long>              m_targetVolume;
    Optional<long>              m_targetBalance;
    Optional<double>            m_targetSeekTo;
    Optional<bool>              m_targetIsScrubbingEnabled;

    bool                        m_didUrlChange;
    bool                        m_doUpdateTransients;
    bool                        m_doClose;
    LONGLONG                    m_lastRequest;
    LONGLONG                    m_lastUpdate;
};

