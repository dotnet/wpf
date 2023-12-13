// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



class PresenterWrapper
{
public:
    PresenterWrapper(
        __in    UINT    id
        );

    ~PresenterWrapper(
        void
        );

    HRESULT
    Init(
        void
        );

    //
    // Sample scheduling
    //

    void
    BeginScrub(
        void
        );

    void
    EndScrub(
        void
        );

    void
    BeginFakePause(
        void
        );

    void
    EndFakePause(
        void
        );

    void
    BeginStopToPauseFreeze(
        void
        );

    void
    EndStopToPauseFreeze(
        bool doFlush
        );

    //
    // Surface Renderer
    //
    HRESULT
    GetSurfaceRenderer(
        __deref_out_opt IAVSurfaceRenderer  **ppISurfaceRenderer
        );

    //
    // Dimensions
    //
    DWORD
    DisplayWidth(
        void
        );

    DWORD
    DisplayHeight(
        void
        );

    void
    SetPresenter(
        __in    EvrPresenterObj     *pEvrPresenter
        );

private:
    //
    // immutable
    //
    UINT                m_uiID;

    CCriticalSection    m_lock;
    bool                m_isScrubbing;
    bool                m_isFakePause;
    bool                m_isStopToPauseFreeze;
    EvrPresenterObj     *m_pEvrPresenter;
};



