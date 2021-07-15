// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*++



Module Name:

    event.hxx

Abstract:

    This header file declares event callback routines and classes.


--*/


#ifndef _EVENT_HXX_
#define _EVENT_HXX_

#define INVALID_UNIQUE_STATE    0

HRESULT SetEventCallbacks(PDEBUG_CLIENT Client);
void ReleaseEventCallbacks(PDEBUG_CLIENT Client);
HRESULT EventCallbacksReady(PDEBUG_CLIENT Client);

extern BOOL gbSymbolsNotLoaded;
extern ULONG UniqueTargetState;

#endif  _EVENT_HXX_



