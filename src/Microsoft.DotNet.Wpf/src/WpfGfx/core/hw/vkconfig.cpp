// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//------------------------------------------------------------------------------
//

//
//  Description:
//      CVkConfigDatabase implementation.  
//

#include "precomp.hpp"

MtDefine(CVkConfigDatabase, MILRender, "CVkConfigDatabase");

//
// Statics
//

bool CVkConfigDatabase::m_fInitialized = false;
UINT CVkConfigDatabase::m_cNumGpus = 0;
UINT *CVkConfigDatabase::m_prguErrorCount = NULL;
bool CVkConfigDatabase::m_fSkipDriverCheck = false;

// Maximum number of internal errors on the D3DDevice before we disable it.
// Set m_prguErrorCount[gpu] to this to disable.
const UINT c_uMaxErrorCount = 5;

//+------------------------------------------------------------------------
//
//  Function:  CVkConfigDatabase::IsGpuEnabled
//
//  Synopsis:  1. Ensure we initialized our status
//             2. Look up gpu in our list
//
//-------------------------------------------------------------------------
HRESULT 
CVkConfigDatabase::IsGpuEnabled(
    UINT uGpu, 
    __out_ecount(1) bool *pfEnabled
    )
{
    HRESULT hr = S_OK;

    Assert(pfEnabled);

    *pfEnabled = false;

    //
    // Make sure that we have initialized our list from the config
    //

    Assert(m_fInitialized);

    //
    // Ensure parameters are valid
    //

    if (uGpu >= m_cNumGpus)
    {
        IFC(E_INVALIDARG);
    }

    //
    // Return status
    //

    *pfEnabled = m_prguErrorCount[uGpu] < c_uMaxErrorCount;

Cleanup:
    RRETURN(hr);
}

//+------------------------------------------------------------------------
//
//  Function:  CVkConfigDatabase::DisableGpu
//
//  Synopsis:  Mark a given gpu as unusable
//
//-------------------------------------------------------------------------
HRESULT 
CVkConfigDatabase::DisableGpu(
    UINT uGpu
    )
{
    HRESULT hr = S_OK;

    //
    // Make sure that we have initialized our list from the config
    //

    Assert(m_fInitialized);

    //
    // Ensure parameters are valid
    //

    if (uGpu >= m_cNumGpus)
    {
        IFC(E_INVALIDARG);
    }

    //
    // Set status
    //

    m_prguErrorCount[uGpu] = c_uMaxErrorCount;

Cleanup:
    RRETURN(hr);
}

//+------------------------------------------------------------------------
//
//  Function:  CVkConfigDatabase::HandleGpuUnexpectedError
//
//  Synopsis:  Handle an unexpected error from an gpu, possibly
//             disabling the gpu.
//
//-------------------------------------------------------------------------
HRESULT 
CVkConfigDatabase::HandleGpuUnexpectedError(
    UINT uGpu
    )
{
    HRESULT hr = S_OK;

    //
    // Make sure that we have initialized our list from the config
    //

    Assert(m_fInitialized);

    //
    // Ensure parameters are valid
    //

    if (uGpu >= m_cNumGpus)
    {
        IFC(E_INVALIDARG);
    }

    //
    // Increment errors
    //

    if (m_prguErrorCount[uGpu] < c_uMaxErrorCount)
    {
        ++m_prguErrorCount[uGpu];
        if (m_prguErrorCount[uGpu] >= c_uMaxErrorCount)
        {
            TraceTag((tagError, "MIL-HW(gpu=%d): Too many d3d internal errors-- switching to software rendering.", uGpu));
        }
    }

Cleanup:
    RRETURN(hr);
}

//+------------------------------------------------------------------------
//
//  Function:  CVkConfigDatabase::ShouldSkipDriverCheck
//
//  Synopsis:  Should we skip driver/vendor checks?  This flag enables 
//             IHV's to investigate issues after we've disabled their 
//             card.
//
//-------------------------------------------------------------------------
bool
CVkConfigDatabase::ShouldSkipDriverCheck()
{
    return m_fSkipDriverCheck;
}

//+------------------------------------------------------------------------
//
//  Function:  CVkConfigDatabase::EnableAllGpus
//
//  Synopsis:  Either enable or disable all gpus
//
//-------------------------------------------------------------------------
void 
CVkConfigDatabase::EnableAllGpus(bool fEnabled)
{
    for (UINT i = 0; i < m_cNumGpus; i++)
    {
        m_prguErrorCount[i] = fEnabled ? 0 : c_uMaxErrorCount;
    }
}

//+------------------------------------------------------------------------
//
//  Function:  CVkConfigDatabase::InitializeFromConfig
//
//  Synopsis:  Initialize our database from the driver list
//
//-------------------------------------------------------------------------

MtDefine(CVkConfigData, MILRender, "CVkConfigData enable array")

HRESULT
CVkConfigDatabase::InitializeFromConfig(
    __in_ecount(1) vk::Instance* pInst
)
{
    HRESULT hr = S_OK;

    Assert(!m_fInitialized);

    IFC(InitializeDriversFromConfig(pInst));

Cleanup:
    m_fInitialized = SUCCEEDED(hr);

    RRETURN(hr);
}



//+------------------------------------------------------------------------
//
//  Function:  CVkConfigDatabase::InitializeDriversFromConfig
//
//  Synopsis:  Initialize Drivers based on config key settings
//
//-------------------------------------------------------------------------
HRESULT
CVkConfigDatabase::InitializeDriversFromConfig(
    __inout_ecount(1) vk::Instance* pInst
    )
{
    HRESULT hr = S_OK;
    HKEY hRegAvalonGraphics = NULL;
    DWORD dwType;
    DWORD dwDisableHWAcceleration;
    DWORD dwDataSize;

    Assert(pInst);
    //
    // Get number of GPUs
    //
    auto result = pInst->enumeratePhysicalDevices(&m_cNumGpus, NULL);
    IFCV(result);

    //
    // Allocate gpu enable array
    //

    {
        UINT cAllocSize = 0;
        IFC(MultiplyUINT(m_cNumGpus, sizeof(*m_prguErrorCount), OUT cAllocSize));

        m_prguErrorCount = WPFAllocType(UINT *,
            ProcessHeap,
            Mt(CVkConfigDatabase),
            cAllocSize
            );
        IFCOOM(m_prguErrorCount);
    }

#ifdef _WIN32
    //
    // Check for global Avalon register hooks
    //

    hr = GetAvalonRegistrySettingsKey(&hRegAvalonGraphics);
    if (FAILED(hr))
    {
        // If we can't open the root key, assume everything is enabled
        // and ignore the error
        EnableAllGpus(true /* fEnabled */);
        hr = S_OK;
        goto Cleanup;
    }

    //
    // Check if HW acceleration is disabled
    //

    dwDataSize = 4;
    if (RegQueryValueEx(
        hRegAvalonGraphics,
        _T("DisableHWAcceleration"),
        NULL,
        &dwType,
        reinterpret_cast<LPBYTE>(&dwDisableHWAcceleration),
        &dwDataSize
        ) == ERROR_SUCCESS)
    {
        if (dwType != REG_DWORD || dwDisableHWAcceleration)
        {
            EnableAllGpus(false /* fEnabled */);
            goto Cleanup;
        }
    }
#else
    #error Not impl.
#endif

    EnableAllGpus(true /* fEnabled */);

Cleanup:
    if (FAILED(hr))
    {
        WPFFree(ProcessHeap, m_prguErrorCount);
        m_prguErrorCount = NULL;
    }

#ifdef _WIN32
    if (hRegAvalonGraphics != NULL)
    {
        RegCloseKey(hRegAvalonGraphics);
    }
#else
    #error Not impl.
#endif

    RRETURN(hr);
}

//+------------------------------------------------------------------------
//
//  Function:  CVkConfigDatabase::Cleanup
//
//  Synopsis:  Reset to uninitialized state
//
//-------------------------------------------------------------------------
void 
CVkConfigDatabase::Cleanup(
    )
{
    WPFFree(ProcessHeap, m_prguErrorCount);

    m_fInitialized = false;
    m_cNumGpus = 0;
    m_prguErrorCount = NULL;
}

