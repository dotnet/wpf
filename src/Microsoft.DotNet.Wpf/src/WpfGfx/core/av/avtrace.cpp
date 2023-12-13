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

#include "precomp.hpp"
#include "avtrace.tmh"

#ifdef DBG

AutoTrace::
AutoTrace(
    __in                UINT       uiID,
    __in_opt            void       *pThisPointer,
    __in                const char *functionName,
    __in_ecount_opt(1)  HRESULT    *phr
    )
    : m_uiID(uiID), m_pThisPointer(pThisPointer), m_functionName(functionName), m_phr(phr)
{
    LogAVDataX(
        AVTRACE_LEVEL_FUNCTION_TRACING,
        AVCOMP_DEFAULT,
        "->: %-60s [%u, %p]",
        m_functionName,
        m_uiID,
        m_pThisPointer);
}

AutoTrace::
~AutoTrace(
    void
    )
{
    if (m_phr && FAILED(*m_phr))
    {
        LogAVDataX(
            AVTRACE_LEVEL_FUNCTION_TRACING,
            AVCOMP_DEFAULT,
            "<-: %-36s failed %!HRESULT! [%u, %p]",
            m_functionName,
            *m_phr,
            m_uiID,
            m_pThisPointer);
    }
    else
    {
        LogAVDataX(
            AVTRACE_LEVEL_FUNCTION_TRACING,
            AVCOMP_DEFAULT,
            "<-: %-60s [%u, %p]",
            m_functionName,
            m_uiID,
            m_pThisPointer);
    }
}

#endif // DBG

