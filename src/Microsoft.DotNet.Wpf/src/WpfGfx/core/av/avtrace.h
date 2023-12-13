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
//
//  $ENDTAG
//
//------------------------------------------------------------------------------

#pragma once

//
// Macros
//

#define EXPECT_SUCCESSINL(hr)
#define EXPECT_SUCCESSINLID(id,hr)

//
// Trace levels that we use. These are numbered as they are
// to correspond as closely as possible to TRACE_LEVEL*,
// defined in evntrace.h
//
#define AVTRACE_LEVEL_ERROR             2
#define AVTRACE_LEVEL_WARNING           3
#define AVTRACE_LEVEL_INFO              4
#define AVTRACE_LEVEL_VERBOSE           5
#define AVTRACE_LEVEL_FUNCTION_TRACING  6

#define AVAMEDIA_TRACE_WPP_CONTROL_GUID (9D21D267,6211,4214,B7B0,E4D7733096AE)

#define WPP_CONTROL_GUIDS                               \
    WPP_DEFINE_CONTROL_GUID(                            \
        AvamediaTraceProvider,                          \
        AVAMEDIA_TRACE_WPP_CONTROL_GUID,                \
        WPP_DEFINE_BIT(AVCOMP_DEFAULT)     /* 0x0001 */ \
        WPP_DEFINE_BIT(AVCOMP_MILAV)       /* 0x0002 */ \
        WPP_DEFINE_BIT(AVCOMP_PLAYER)      /* 0x0004 */ \
        WPP_DEFINE_BIT(AVCOMP_PRESENTER)   /* 0x0008 */ \
        WPP_DEFINE_BIT(AVCOMP_CLOCKWRAPPER)/* 0x0010 */ \
        WPP_DEFINE_BIT(AVCOMP_DECODE)      /* 0x0020 */ \
        WPP_DEFINE_BIT(AVCOMP_DXVAMANWRAP) /* 0x0040 */ \
        WPP_DEFINE_BIT(AVCOMP_EVENTS)      /* 0x0080 */ \
        WPP_DEFINE_BIT(AVCOMP_STATEENGINE) /* 0x0100 */ \
        WPP_DEFINE_BIT(AVCOMP_SAMPLEQUEUE) /* 0x0200 */ \
        WPP_DEFINE_BIT(AVCOMP_BUFFER)      /* 0x0400 */ \
    )                                                   \

#define WPP_LEVEL_SUBCOMP_ENABLED(level, subcomp)   (WPP_LEVEL_ENABLED(subcomp) && WPP_CONTROL(WPP_BIT_ ## subcomp).Level >= level)
#define WPP_LEVEL_SUBCOMP_LOGGER(level, subcomp)    WPP_LEVEL_LOGGER(subcomp)

#ifndef DBG
//
// For TRACEF tracing. This is debug only because it uses the constructor, destructor paradigm
//
#define WPP_PHR_ENABLED(PHR)                        (WPP_CONTROL(WPP_BIT_AVCOMP_DEFAULT).Level >= AVTRACE_LEVEL_FUNCTION_TRACING)
#define WPP_PHR_LOGGER(PHR)                         WPP_LEVEL_LOGGER(AVCOMP_DEFAULT)

//
// For TRACEFID tracing. This is debug only because it uses the constructor, destructor paradigm
//
#define WPP_ID_PHR_ENABLED(ID, PHR)                 (WPP_CONTROL(WPP_BIT_AVCOMP_DEFAULT).Level >= AVTRACE_LEVEL_FUNCTION_TRACING)
#define WPP_ID_PHR_LOGGER(ID, PHR)                  WPP_LEVEL_LOGGER(AVCOMP_DEFAULT)
#endif // !DBG

//
// For EXPECT_SUCCESS tracing
//
#define WPP_LEVEL_HR_ENABLED(LEVEL, HR)             (WPP_CONTROL(WPP_BIT_AVCOMP_DEFAULT).Level >= LEVEL)
#define WPP_LEVEL_HR_LOGGER(LEVEL, HR)              WPP_LEVEL_LOGGER(AVCOMP_DEFAULT)
#define WPP_LEVEL_HR_PRE(LEVEL, HR)                 {if (FAILED(HR)) {
#define WPP_LEVEL_HR_POST(LEVEL, HR)                ;}}

//
// For EXPECT_SUCCESSID tracing
//
#define WPP_LEVEL_ID_HR_ENABLED(LEVEL, ID, HR)      (WPP_CONTROL(WPP_BIT_AVCOMP_DEFAULT).Level >= LEVEL)
#define WPP_LEVEL_ID_HR_LOGGER(LEVEL, ID, HR)       WPP_LEVEL_LOGGER(AVCOMP_DEFAULT)
#define WPP_LEVEL_ID_HR_PRE(LEVEL, ID, HR)          {if (FAILED(HR)) {
#define WPP_LEVEL_ID_HR_POST(LEVEL, ID, HR)         ;}}

//
// begin_wpp config
//
// CUSTOM_TYPE(wmpplay, ItemEnum(WMPPlayState) );
// CUSTOM_TYPE(wmpopen, ItemEnum(WMPOpenState) );
// CUSTOM_TYPE(ActionState, ItemEnum(ActionState::Enum) );
//
// FUNC LogAVDataX(LEVEL,SUBCOMP,MSG,...);
//
// FUNC LogAVDataM(LEVEL,SUBCOMP,MSG,...);
// USESUFFIX(LogAVDataM, "    [%u, %p]", m_uiID, this);
//
// FUNC EXPECT_SUCCESS{LEVEL=AVTRACE_LEVEL_ERROR}(HR);
// USESUFFIX(EXPECT_SUCCESS, "%!FUNC! returned unexpected %!HRESULT!    [%u, %p]", HR, m_uiID, this);
//
// FUNC EXPECT_SUCCESSID{LEVEL=AVTRACE_LEVEL_ERROR}(ID, HR);
// USESUFFIX(EXPECT_SUCCESSID, "%!FUNC! returned unexpected %!HRESULT!    [%u,]", HR, ID);
//
// end_wpp
//

#ifdef DBG
#define TRACEF(x) AutoTrace __trace(m_uiID, this, __FUNCTION__, x);
#define TRACEFID(id,x) AutoTrace __trace(id, NULL, __FUNCTION__, x);

class AutoTrace
{
private:
    UINT          m_uiID;
    void          *m_pThisPointer;
    const char    *m_functionName;
    HRESULT       *m_phr;

public:
    AutoTrace(
        __in                UINT       uiID,
        __in_opt            void       *pThisPointer,
        __in	            const char *functionName,
        __in_ecount_opt(1)  HRESULT    *phr
        );

    ~AutoTrace(
        void
        );
};

#endif // DBG

