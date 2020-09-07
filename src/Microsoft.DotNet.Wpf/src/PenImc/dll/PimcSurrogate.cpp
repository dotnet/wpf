// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// PimcSurrogate.cpp : Implementation of CPimcSurrogate

#include "stdafx.h"
#include "Penimc.h"
#include "PimcSurrogate.h"
#include <strsafe.h>
#include <windows.h>

/////////////////////////////////////////////////////////////////////////////
// CPimcSurrogate

/////////////////////////////////////////////////////////////////////////////

CPimcSurrogate::CPimcSurrogate()
{
}

/////////////////////////////////////////////////////////////////////////////

HRESULT CPimcSurrogate::FinalConstruct()
{
    DHR;
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

void CPimcSurrogate::FinalRelease() 
{
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcSurrogate::GetWisptisITabletManager(__deref_out IUnknown** ppTabletManagerUnknown)
{
    return ::CoCreateInstance(CLSID_TabletManagerS, NULL, CLSCTX_INPROC_SERVER | CLSCTX_LOCAL_SERVER, IID_IUnknown, (LPVOID*)ppTabletManagerUnknown);
}


