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
//      Provide implementation of the activation object. This object provides
//      the EVR presenter instance to the MF Filter graph.
//
//  $ENDTAG
//
//------------------------------------------------------------------------------
#pragma once

MtExtern(MFActivate);

class MFLockWrapper : public CCriticalSection
{
public:

    MFLockWrapper(
        void
        );

    ~MFLockWrapper(
        void
        );

    HRESULT
    Init(
        void
        ) const;

    void
    Lock(
        void
        );

    void
    Unlock(
        void
        );

private:

    //
    // Cannot copy or assign an MFLockWrapper
    //
    MFLockWrapper(
        __in const MFLockWrapper &
        );

    MFLockWrapper &
    operator=(
        __in const MFLockWrapper &
        );

    HRESULT     m_hr;
};

class MFActivate;

typedef RealComObject<MFActivate, NoDllRefCount> MFActivateObj;

class MFActivate : public CMFAttributesImpl<IMFActivate, MFLockWrapper>
{
public:

    DECLARE_METERHEAP_CLEAR(ProcessHeap, Mt(MFActivate));

    static
    HRESULT
    Create(
        __in        UINT                uiID,
        __in        CWmpStateEngine     *pWmpStateEngine,
        __deref_out MFActivateObj       **ppMFActivateObj
        );

    STDMETHOD(DetachObject)(
        );

    STDMETHOD(ShutdownObject)(
        );

    STDMETHOD(ActivateObject)(
        __in        REFIID  riid,
        __deref_out void    **ppv
        );

protected:

    void *
    GetInterface(
        __in    REFIID      riid
        );

    MFActivate(
        __in    UINT                uiID,
        __in    CWmpStateEngine     *pWmpStateEngine
        );

    ~MFActivate(
        void
        );

private:

    //
    // Cannot copy or assign an MFActivate object.
    //
    MFActivate(
        __in const MFActivate &
        );

    MFActivate &
    operator=(
        __in const MFActivate &
        );

    HRESULT
    Init(
        void
        );

    UINT                m_uiID;
    CWmpStateEngine     *m_pWmpStateEngine;
    EvrPresenterObj     *m_pEvrPresenter;
};

