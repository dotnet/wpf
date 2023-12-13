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
//      Provides support for the player state manager. This provides a separate
//      thread which starts up the Player OCX and also provides services for
//
//  $ENDTAG
//
//------------------------------------------------------------------------------

/*static*/ inline
bool
CWmpStateEngine::
IsStatePartOfSet(
    __in                        ActionState::Enum           playerState,
    __in_ecount(cPlayerState)   const ActionState::Enum     *aPlayerState,
    __in                        int                         cPlayerState
    )
{
    bool    inSet = false;

    for(int i = 0; i < cPlayerState; i++)
    {
        if (playerState == aPlayerState[i])
        {
            inSet = true;

            break;
        }
    }

    return inSet;
}

/*static*/ inline
bool
CWmpStateEngine::
EvrStateToIsEvrClockRunning(
    __in      RenderState::Enum                    renderState
    )
{
    if (renderState == RenderState::Stopped)
    {
        return false;
    }
    else if (renderState == RenderState::Paused)
    {
        return false;
    }
    else if (renderState == RenderState::Started)
    {
        return true;
    }
    else
    {
        RIP("Invalid state");
        return false;
    }
}

/*static*/ inline
LONGLONG
CWmpStateEngine::
SecondsToTicks(
    __in                        double                      seconds
    )
{
    return LONGLONG((seconds * gc_ticksPerSecond) + 0.5);
}

inline
HRESULT
CWmpStateEngine::
AddItem(
    __in    CStateThreadItem    *pItem
    )
{
    //
    // m_pStateThread isn't released in Shutdown (it's only released in the
    // destructor), so it's guaranteed to still be here.
    //
    RRETURN(m_pStateThread->AddItem(pItem));
}



