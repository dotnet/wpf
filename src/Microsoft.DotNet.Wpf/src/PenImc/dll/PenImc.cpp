// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// PenImc.cpp : Implementation of DLL Exports.

#include "stdafx.h"
#include "resource.h"
#include "PenImc.h"
#include "PimcManager.h"

#include <initguid.h>
#include "dlldatax.h"
#include "penimc_i.c"
#include <wisptis_i.c>
#include <wisptics_i.c>
#include <tpcpen_i.c>
//#include <tpccom_i.c>


#if 1 // from tablib.lib WIP (toddt) this is to be fixed after the new build changes settle down.
void SafeCloseHandle(__inout HANDLE * pHandle)
{
    ASSERT (pHandle);

    if (*pHandle)
    {
        CloseHandle(*pHandle);
        *pHandle = NULL;
    }
}
#endif

/////////////////////////////////////////////////////////////////////////////

class CPenImcModule : public CAtlDllModuleT< CPenImcModule >
{
public :
	DECLARE_LIBID(LIBID_PenImcLib4v3)
	DECLARE_REGISTRY_APPID_RESOURCEID(IDR_PENIMC, "{E31B1A40-9FE5-46D8-98F0-9B0F75F0320C}")
};

CPenImcModule _AtlModule;

/////////////////////////////////////////////////////////////////////////////

#if WANT_SINGLETON
class CPimcManagerFactory : public IClassFactory
{
public:
    STDMETHOD_(ULONG, AddRef) () { return 1; }
    STDMETHOD_(ULONG, Release)() { return 1; };
    STDMETHOD(QueryInterface)(REFIID riid, void** ppv);
    STDMETHOD(CreateInstance)(LPUNKNOWN pUnkOuter, REFIID riid, LPVOID * ppvObject);
    STDMETHOD(LockServer)(BOOL fLock);
};

CPimcManagerFactory _PimcManagerFactory;
#endif // WANT_SINGLETON

/////////////////////////////////////////////////////////////////////////////

extern "C" BOOL WINAPI DllMain(__in HINSTANCE hInstance, DWORD dwReason, LPVOID lpReserved)
{
    if (dwReason == DLL_PROCESS_ATTACH)
    {
        g_hMutexHook = CreateMutex(NULL, /* initial ownership */FALSE, NULL);
        //Assert g_hMutexHook != null?
    }
    else if (dwReason == DLL_PROCESS_DETACH)
    {
        //ASSERT(g_cHookLock == 0);
        SafeCloseHandle(&g_hMutexHook);
    }

#ifdef _MERGE_PROXYSTUB
    if (!PrxDllMain(hInstance, dwReason, lpReserved))
        return FALSE;
#endif
    hInstance;
    return _AtlModule.DllMain(dwReason, lpReserved); 
}

/////////////////////////////////////////////////////////////////////////////

__control_entrypoint(DllExport)
STDAPI DllCanUnloadNow(void)
{
#ifdef _MERGE_PROXYSTUB
    HRESULT hr = PrxDllCanUnloadNow();
    if (hr != S_OK)
        return hr;
#endif
    return _AtlModule.DllCanUnloadNow();
}


/////////////////////////////////////////////////////////////////////////////

_Check_return_
STDAPI DllGetClassObject(_In_ REFCLSID rclsid, _In_ REFIID riid, _Outptr_ LPVOID* ppv)
{
#ifdef _MERGE_PROXYSTUB
    HRESULT hr = PrxDllGetClassObject(rclsid, riid, ppv);
    if (hr != CLASS_E_CLASSNOTAVAILABLE)
        return hr;
#endif

    // NOTE:
    // In order to support multiple app domains we don't want to return just one
    // object for both those app domains or we cause problems with the RPC interface
    // we have to wisptis.  We need to actually create two different CPimcManager
    // objects in order to run properly.  This required the removal of the singleton 
    // support below.  Since the avalon stylus code uses a static class to manage
    // the CPimcManager object we will only get one instance per app domain which is
    // what we want.
    
#if WANT_SINGLETON
    // This is how we implement PimcManager as a singleton. We could've used
    // DECLARE_CLASSFACTORY_SINGLETON, but in that case PimcManager gets released
    // way too late because _AtlModule is holding refs on it until destructor.
    // In that case PimcManager thread gets killed rather than experiencing a normal
    // shutdown.
    if (IsEqualGUID(rclsid, CLSID_PimcManager))
    {
        return _PimcManagerFactory.QueryInterface(riid, ppv);
    }
#endif

    return _AtlModule.DllGetClassObject(rclsid, riid, ppv);
}


/////////////////////////////////////////////////////////////////////////////

__control_entrypoint(DllExport)
STDAPI DllRegisterServer(void)
{
    // registers object, typelib and all interfaces in typelib
    HRESULT hr = _AtlModule.DllRegisterServer();
#ifdef _MERGE_PROXYSTUB
    if (FAILED(hr))
        return hr;
    hr = PrxDllRegisterServer();
#endif
    return hr;
}


/////////////////////////////////////////////////////////////////////////////

__control_entrypoint(DllExport)
STDAPI DllUnregisterServer(void)
{
    HRESULT hr = _AtlModule.DllUnregisterServer();
#ifdef _MERGE_PROXYSTUB
    if (FAILED(hr))
        return hr;
    hr = PrxDllRegisterServer();
    if (FAILED(hr))
        return hr;
    hr = PrxDllUnregisterServer();
#endif
    return hr;
}


#if WANT_SINGLETON

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcManagerFactory::QueryInterface(REFIID riid, __typefix(IClassFactory **) __deref_out void** ppv)
{
    DHR;
    if (IsEqualGUID(riid, IID_IUnknown) ||
        IsEqualGUID(riid, IID_IClassFactory))
    {
        *ppv = (IClassFactory*)this;
        AddRef();
        CHR(S_OK);
    }
    else
    {
        CHR(E_NOINTERFACE);
    }
CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcManagerFactory::CreateInstance(LPUNKNOWN pUnkOuter, REFIID riid, __deref_out LPVOID * ppvObject)
{
    DHR;
    if (Mgr() == NULL)
    {
        CComObject<CPimcManager> * pMgr;
        CHR(CComObject<CPimcManager>::CreateInstance(&pMgr));
    }
    ASSERT(Mgr() != NULL);
    CHR(Mgr()->QueryInterface(riid, (void**)ppvObject));
CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcManagerFactory::LockServer(BOOL fLock)
{
    if (fLock)
        _AtlModule.Lock();
    else
        _AtlModule.Unlock();
    return S_OK;
}
#endif // WANT_SINGLETON

