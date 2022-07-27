// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//------------------------------------------------------------------------------
//

//
//  Description:
//      Contains CVkConfigDatabase
//

MtExtern(CVkConfigDatabase);

//------------------------------------------------------------------------------
//
//  Class: CVkConfigDatabase
//
//  Description:
//      Accesses the config to determine if we can run hw accelerated on
//      the current driver
//
//      Config means Register in Windows
//
//      Note that all methods and data here are static so that we can be
//      smart enough to only access the config once to query this 
//      information.
//
//------------------------------------------------------------------------------

class CVkConfigDatabase  
{
public:
    DECLARE_METERHEAP_ALLOC(ProcessHeap, Mt(CVkConfigDatabase));

    //
    // CVkConfigDatabase methods
    //

    static HRESULT InitializeFromConfig(
    __in_ecount(1) vk::Instance *pInst
        );

    static HRESULT IsGpuEnabled(
        UINT uGpu, 
        __out_ecount(1) bool *pfEnabled
        );

    static bool ShouldSkipDriverCheck();

    static HRESULT DisableGpu(
        UINT uGpu
        );

    static HRESULT HandleGpuUnexpectedError(
        UINT uGpu
        );

    static void Cleanup();
    
private:
    static void EnableAllGpus(
        bool fEnabled
        );

    static HRESULT InitializeDriversFromConfig(
    __inout_ecount(1) vk::Instance *pInst
        );

private:
    static bool m_fInitialized;
    static UINT m_cNumGpus;

    // Error count is number of errors associated with this adapter.  If
    // it is greater than or equal to c_uMaxErrorCount the adapter is
    // disabled.
    static UINT *m_prguErrorCount;
    static bool m_fSkipDriverCheck;
};




