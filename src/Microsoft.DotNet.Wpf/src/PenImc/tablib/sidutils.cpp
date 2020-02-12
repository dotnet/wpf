// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#include "sidutils.h"
#include "sddl.h"
#include <tabassert.h>

HRESULT
GetUserSid(__inout LPTSTR* sid)
{
    ASSERT(sid != NULL);

    if(sid == NULL)
        return E_POINTER;

    HRESULT hr = E_FAIL;

    HANDLE hToken = NULL;
    BOOL bResult = OpenProcessToken(
        GetCurrentProcess(),
        TOKEN_QUERY,
        &hToken);

    if(bResult)
    {
        DWORD dwLength = 0;
        bResult = GetTokenInformation(
            hToken,
            TokenUser,
            NULL,
            0,
            &dwLength);

        if(!bResult && GetLastError() == ERROR_INSUFFICIENT_BUFFER)
        {
            TOKEN_USER* ptu = (TOKEN_USER*)new char[dwLength];

            if(ptu != NULL)
            {
                bResult = GetTokenInformation(
                    hToken,
                    TokenUser,
                    ptu,
                    dwLength,
                    &dwLength);

                if(bResult)
                {
                    bResult = ConvertSidToStringSid(ptu->User.Sid, sid);
                
                    if(bResult)
                        hr = S_OK;
                }
                
                delete [] (char*)ptu;
            }
        }

        CloseHandle(hToken);
    }

    return hr;
}


HRESULT
GetMandatoryLabel(__inout LPTSTR* sid)
{
    ASSERT(sid != NULL);

    if(sid == NULL)
        return E_POINTER;

    HRESULT hr = E_FAIL;

    HANDLE hToken = NULL;
    BOOL bResult = OpenProcessToken(
        GetCurrentProcess(),
        TOKEN_QUERY,
        &hToken);

    if(bResult)
    {
        DWORD dwLength = 0;
        bResult = GetTokenInformation(
            hToken,
            TokenIntegrityLevel,
            NULL,
            0,
            &dwLength);

        if(!bResult && GetLastError() == ERROR_INSUFFICIENT_BUFFER)
        {
            TOKEN_MANDATORY_LABEL* ptml = (TOKEN_MANDATORY_LABEL*)new char[dwLength];

            if(ptml != NULL)
            {
                bResult = GetTokenInformation(
                    hToken,
                    TokenIntegrityLevel,
                    ptml,
                    dwLength,
                    &dwLength);

                if(bResult)
                {
                    bResult = ConvertSidToStringSid(ptml->Label.Sid, sid);
                
                    if(bResult)
                        hr = S_OK;
                }
                
                delete [] (char*)ptml;
            }
        }

        CloseHandle(hToken);
    }

    return hr;
}

HRESULT
GetLogonSessionSid(__inout LPTSTR* sid)
{
    ASSERT(sid != NULL);

    if(sid == NULL)
        return E_POINTER;

    HRESULT hr = E_FAIL;
    
    HANDLE hToken = NULL;
    BOOL bResult = OpenProcessToken(
        GetCurrentProcess(),
        TOKEN_QUERY,
        &hToken);

    if(bResult)
    {
        hr = GetLogonSessionSid(hToken, sid);
        CloseHandle(hToken);
    }
    
    return hr;
}

HRESULT
GetLogonSessionSid(HANDLE hToken, __inout LPTSTR* sid)
{
    ASSERT(sid != NULL);

    if(sid == NULL)
        return E_POINTER;

    HRESULT hr = E_FAIL;

    DWORD dwLength = 0;
    BOOL bResult = GetTokenInformation(
        hToken,
        TokenLogonSid,
        NULL,
        0,
        &dwLength);

    if(!bResult && GetLastError() == ERROR_INSUFFICIENT_BUFFER)
    {
        TOKEN_GROUPS* ptg = (TOKEN_GROUPS*)new char[dwLength];

        if(ptg != NULL)
        {
            bResult = GetTokenInformation(
                hToken,
                TokenLogonSid,
                ptg,
                dwLength,
                &dwLength);

            if(bResult && ptg->GroupCount == 1)
            {
                PSID pSid = ptg->Groups[0].Sid;

                bResult = ConvertSidToStringSid(pSid, sid);

                if(bResult)
                    hr = S_OK;
            }

            delete [] (char*)ptg;
        }
    }

    return hr;
}

