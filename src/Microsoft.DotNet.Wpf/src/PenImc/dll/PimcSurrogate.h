// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// PimcSurrogate.h : Declaration of the CPimcSurrogate

#pragma once
#include "resource.h"       // main symbols
#include "PenImc.h"

/////////////////////////////////////////////////////////////////////////////
// CPimcSurrogate
class ATL_NO_VTABLE CPimcSurrogate : 
	public CComObjectRootEx<CComSingleThreadModel>,
	public CComCoClass<CPimcSurrogate, &CLSID_PimcSurrogate3>,
    public IPimcSurrogate3
{
public:

    /////////////////////////////////////////////////////////////////////////

    CPimcSurrogate();

    HRESULT FinalConstruct();
    void    FinalRelease();

    //
    // IPimcSurrogate3
    //
    STDMETHOD(GetWisptisITabletManager)(__deref_out IUnknown** ppTabletManagerUnknown);

    // wiring
    DECLARE_REGISTRY_RESOURCEID(IDR_PIMCSURROGATE)

    BEGIN_COM_MAP(CPimcSurrogate)
    	COM_INTERFACE_ENTRY(IPimcSurrogate3)
    END_COM_MAP()

    DECLARE_PROTECT_FINAL_CONSTRUCT()
};

/////////////////////////////////////////////////////////////////////////////

OBJECT_ENTRY_AUTO(__uuidof(PimcSurrogate3), CPimcSurrogate)

