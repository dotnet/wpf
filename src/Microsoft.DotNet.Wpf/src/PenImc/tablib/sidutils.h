// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#pragma once

#include "windows.h"

// sid must be freed via LocalFree
HRESULT GetUserSid(__inout LPTSTR* sid);
HRESULT GetMandatoryLabel(__inout LPTSTR* sid);
HRESULT GetLogonSessionSid(__inout LPTSTR* sid);
HRESULT GetLogonSessionSid(HANDLE hToken, __inout LPTSTR* sid);

