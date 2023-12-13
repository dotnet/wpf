// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//+--------------------------------------------------------------------------
//

//
//----------------------------------------------------------------------------

#include "precomp.hpp"

void WINAPI
RenderOptions_ForceSoftwareRenderingModeForProcess(BOOL fForce)
{
    RenderOptions::ForceSoftwareRenderingForProcess(fForce);
}

BOOL WINAPI
RenderOptions_IsSoftwareRenderingForcedForProcess()
{
    return RenderOptions::IsSoftwareRenderingForcedForProcess();
}

void WINAPI
RenderOptions_EnableHardwareAccelerationInRdp(BOOL fEnable)
{
    RenderOptions::EnableHardwareAccelerationInRdp(fEnable);
}

// m_cs must be entered before accessing m_fForceSoftware because multiple
// managed threads plus the render thread could try to access it
static CCriticalSection m_cs;
static bool m_fForceSoftware;
static bool m_fHwAccelerationInRdpEnabled;

//+---------------------------------------------------------------------------------
//
//  RenderOptions::Init
//
//  Synopsis:   Initializes the RenderOptions. This is not thread safe and should only be called once
//                  at DLL load.
//
//----------------------------------------------------------------------------------

HRESULT 
RenderOptions::Init()
{
    m_fForceSoftware = false;
    m_fHwAccelerationInRdpEnabled = false;
    RRETURN(m_cs.Init());
}

//+---------------------------------------------------------------------------------
//
//  RenderOptions::DeInit
//
//  Synopsis:   Deinits the RenderOptions. This is not thread safe and should only be called once
//                  at DLL unload
//
//----------------------------------------------------------------------------------

void
RenderOptions::DeInit()
{
    m_cs.DeInit();
}

//+---------------------------------------------------------------------------------
//
//  RenderOptions::ForceSoftwareRenderingForProcess
//
//  Synopsis:   Sets whether or not we should force all render targets to software for the process
//
//                  Note: this uses BOOL not bool because fForce is marshalled from managed code
//
//----------------------------------------------------------------------------------

void 
RenderOptions::ForceSoftwareRenderingForProcess(BOOL fForce)
{
    CGuard<CCriticalSection> guard(m_cs);
    m_fForceSoftware = !!fForce;
}

//+---------------------------------------------------------------------------------
//
//  RenderOptions::IsSoftwareRenderingForcedForProcess
//
//  Synopsis:   Sets whether or not we should force all render targets to software for the process
//
//                  Note: this returns BOOL not bool to facilitate managed marshalling
//
//----------------------------------------------------------------------------------

BOOL 
RenderOptions::IsSoftwareRenderingForcedForProcess()
{
    CGuard<CCriticalSection> guard(m_cs);
    
    return m_fForceSoftware;
}

//+---------------------------------------------------------------------------------
//
//  RenderOptions::EnableGraphicHWAccelerationInRdp
//
//  Synopsis:   Sets whether or not Hardware Acceleration should be enabled for RDP.
//
//----------------------------------------------------------------------------------
void
RenderOptions::EnableHardwareAccelerationInRdp(BOOL fEnable)
{
    CGuard<CCriticalSection> guard(m_cs);
    m_fHwAccelerationInRdpEnabled = !!fEnable;
}

//+---------------------------------------------------------------------------------
//
//  RenderOptions::IsHardwareAccelerationInRdpEnabled
//
//  Synopsis:   return whether or not Hardware Acceleration for RDP is enabled.
//
//----------------------------------------------------------------------------------
BOOL
RenderOptions::IsHardwareAccelerationInRdpEnabled()
{
    return m_fHwAccelerationInRdpEnabled;
}

