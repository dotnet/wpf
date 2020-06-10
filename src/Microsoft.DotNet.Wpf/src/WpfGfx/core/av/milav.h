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
//      Header for CMILAV, which facilitates calling unmanaged code from managed
//      code.
//
//  $ENDTAG
//
//------------------------------------------------------------------------------

#pragma once

class MediaInstance;
class CMediaEventProxy;
class CEventProxy;

class CMILAV
{
public:

    static HRESULT CreateMedia(
        __in        CEventProxy *pEventProxy,
        __in        bool        canOpenAnyMedia,
        __deref_out IMILMedia   **ppMedia
        );

private:
    static HRESULT ChoosePlayer(
        __in        MediaInstance       *pMediaInstance,
        __in        bool                canOpenAnyMedia,
        __deref_out IMILMedia           **ppMedia
        );
};

class NoDllRefCount
{
public:

    static inline
    void
    AddRef(
        void
        );

    static inline
    void
    Release(
        void
        );
};

template<class Base, class DllCount>
class RealComObject : public Base
{
public:

    RealComObject(
        void
        );

    template<typename P1>
    RealComObject(
        __in    P1      p1
        );

    template<typename P1, typename P2>
    RealComObject(
        __in    P1      p1,
        __in    P2      p2
        );


    template<typename P1, typename P2, typename P3>
    RealComObject(
        __in    P1      p1,
        __in    P2      p2,
        __in    P3      p3
        );

    template<typename P1, typename P2, typename P3, typename P4>
    RealComObject(
        __in    P1      p1,
        __in    P2      p2,
        __in    P3      p3,
        __in    P4      p4
        );

    template<typename P1, typename P2, typename P3, typename P4, typename P5>
    RealComObject(
        __in    P1      p1,
        __in    P2      p2,
        __in    P3      p3,
        __in    P4      p4,
        __in    P5      p5
        );

    template<typename P1, typename P2, typename P3, typename P4, typename P5, typename P6>
    RealComObject(
        __in    P1      p1,
        __in    P2      p2,
        __in    P3      p3,
        __in    P4      p4,
        __in    P5      p5,
        __in    P6      p6
        );

    ~RealComObject(
        void
        );

    STDMETHOD(QueryInterface)(
        __in        REFIID      riid,
        __deref_out void        **ppv
        );

    STDMETHOD_(ULONG, AddRef)(
        void
        );

    STDMETHOD_(ULONG, Release)(
        void
        );

private:

    //
    // Cannot copy or assign a RealComObject
    //
    RealComObject(
        __in    const RealComObject &
        );

    RealComObject &
    operator=(
        __in    const RealComObject &
        );

    inline
    void
    Construct(
        void
        );

    LONG        m_cRef;
};

#include "..\av\milav.inl"

