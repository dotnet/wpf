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
//      Provides for simple utility functions. The general rule is that none of
//      the functions in this file can have dependencies on other functions in
//      the file. If this rule is broken, seperate out the functions into their
//      own file.
//
//  $ENDTAG
//
//------------------------------------------------------------------------------
inline
HRESULT 
IsSupportedWmpReturn(
    __in    HRESULT     hr
    )
{
    // convert this "success" code to a failure
    if (hr == NS_S_WMPCORE_COMMAND_NOT_AVAILABLE)
    {
        return WGXERR_AV_UNEXPECTEDWMPFAILURE;
    }
    else
    {
        return hr;
    }
}

inline
HRESULT
SysAllocStringCheck(
    __in    PCWSTR          pszString,
    __out   BSTR            *pbstrString
    )
{
    HRESULT     hr = S_OK;

    *pbstrString = SysAllocString(pszString);

    if (!*pbstrString && pszString)
    {
        hr = E_OUTOFMEMORY;
    }

    return hr;
}

template<class T>
void
SmartRelease(
    T **ppInstance
    )
{
    if (*ppInstance)
    {
        (*ppInstance)->Release();
        *ppInstance = NULL;
    }
}

inline
D3DFORMAT
FormatFromMediaType(
    __in        IMFVideoMediaType   *pIVideoMediaType
    )
{
    return static_cast<D3DFORMAT>(pIVideoMediaType->GetVideoFormat()->guidFormat.Data1);
}

