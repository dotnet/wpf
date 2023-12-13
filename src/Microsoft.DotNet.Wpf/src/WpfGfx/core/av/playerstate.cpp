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
//      Provides extra enums, templates, and classes for use with
//      CWmpStateEngine
//
//  $ENDTAG
//
//------------------------------------------------------------------------------

#include "precomp.hpp"
#include "playerstate.tmh"


//
// Optional BSTR methods
//



//+-----------------------------------------------------------------------------
//
//  Member:
//      OptionalString::OptionalString
//
//  Synopsis:
//      Create a new OptionalString. It will be invalid to start off
//
//------------------------------------------------------------------------------
OptionalString::
OptionalString(
    void
    ) : m_isValid(false)
      , m_value(NULL)
{}

//+-----------------------------------------------------------------------------
//
//  Member:
//      OptionalString::~OptionalString
//
//  Synopsis:
//      Destructor. It will free the associated string (regardless of whether or
//      not the value is valid)
//
//------------------------------------------------------------------------------
OptionalString::
~OptionalString()
{
    delete[] m_value; // okay to pass in NULL here
    m_value = NULL;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      OptionalString::DoesMatch
//
//  Synopsis:
//      Check if a BSTR matches this OptionalString. They match if this
//      OptionalString is invalid (indicating don't care) or if the underlying
//      string matches the BSTR.
//
//------------------------------------------------------------------------------
bool
OptionalString::
DoesMatch(
    PCWSTR  value
    )
    const
{
    return (!m_isValid) || AreStringsEqual(m_value, value);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      OptionalString::ApplyAsMask
//
//  Synopsis:
//      Use this OptionalString as a mask. We return m_value if we're valid,
//      otherwise we return ori. Each time we allocate a new string so that we
//      don't have to worry about reference counting issues.
//
//------------------------------------------------------------------------------
HRESULT
OptionalString::
ApplyAsMask(
    __in  PCWSTR            uri,
    __deref_out PWSTR       *pRet
    ) const
{
    HRESULT hr = S_OK;

    if (m_isValid)
    {
        IFC(CopyHeapString(m_value, pRet));
    }
    else
    {
        IFC(CopyHeapString(uri, pRet));
    }

Cleanup:

    RRETURN(hr);
}

//
// PlayerState methods
//


//+-----------------------------------------------------------------------------
//
//  Member:
//      PlayerState::PlayerState
//
//  Synopsis:
//      Create a new player state object. Values are set to sensible defaults
//
//------------------------------------------------------------------------------
PlayerState::
PlayerState(
    void
    ) : m_url(NULL)
{
    Clear();
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      PlayerState::~PlayerState
//
//  Synopsis:
//      PlayerState destructor
//
//------------------------------------------------------------------------------
PlayerState::
~PlayerState(
    void
    )
{
    delete[] m_url;
    m_url = NULL;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      PlayerState::Clear
//
//  Synopsis:
//      Clears the player state back to its initial state.
//
//------------------------------------------------------------------------------
void
PlayerState::
Clear(
    void
    )
{
    m_isOcxCreated = false;

    delete[] m_url;
    m_url = NULL;

    m_actionState = ActionState::Stop;
    m_volume = msc_defaultWmpVolume;
    m_balance = 0;
    m_rate = 1;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      PlayerState::Copy
//
//  Synopsis:
//      Copy this PlayerState. We need to allocate a new string so there is a
//      failure point.
//
//------------------------------------------------------------------------------
HRESULT
PlayerState::
Copy(
    __out     PlayerState     *dst
    )
    const
{
    HRESULT hr = S_OK;

    dst->m_isOcxCreated = m_isOcxCreated;
    dst->m_actionState = m_actionState;
    dst->m_volume = m_volume;
    dst->m_balance = m_balance;
    dst->m_rate = m_rate;
    dst->m_seekTo = m_seekTo;

    IFC(CopyHeapString(m_url, &(dst->m_url)));

Cleanup:
    RRETURN(hr);
}


void
PlayerState::
DumpPlayerState(
    __in UINT       uiID,
    __in char       *description
    ) const
{
    LogAVDataX(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "%s PlayerState: (OC: %d, AS: %!ActionState!, VOL: %d, BAL: %d, RATE: %f, SI: %d, SV: %f" #
        " [%u, %p]",
        description,
        m_isOcxCreated,
        m_actionState,
        m_volume,
        m_balance,
        m_rate,
        m_seekTo.m_isValid,
        m_seekTo.m_value,
        uiID,
        this);
}

//+-----------------------------------------------------------------------------
//
//  operator==(PlayerState, PlayerState)
//
//  Synopsis:
//      Compare two PlayerStates
//
//------------------------------------------------------------------------------
bool operator==(
    __in            PlayerState     &ps1,
    __in            PlayerState     &ps2
    )
{
    return (   (ps1.m_isOcxCreated == ps2.m_isOcxCreated)
            && (AreStringsEqual(ps1.m_url, ps2.m_url))
            && (ps1.m_actionState == ps2.m_actionState)
            && (ps1.m_volume == ps2.m_volume)
            && (ps1.m_balance == ps2.m_balance)
            && (ps1.m_rate == ps2.m_rate)
            && (ps1.m_seekTo == ps2.m_seekTo));
}

//+-----------------------------------------------------------------------------
//
//  operator!=(PlayerState, PlayerState)
//
//  Synopsis:
//      Compare two PlayerStates for inequality
//
//------------------------------------------------------------------------------
bool operator!=(
    __in    PlayerState     &ps1,
    __in    PlayerState     &ps2
    )
{
    return !(ps1 == ps2);
}

//+-----------------------------------------------------------------------------
//
//  AreStringsEqual(wchar_t *, wchar_t *)
//
//  Synopsis:
//      Compare two strings for equality - also handles null strings
//
//------------------------------------------------------------------------------
bool
AreStringsEqual(
    __in_opt    const wchar_t     *s1,
    __in_opt    const wchar_t     *s2
    )
{
    if (s1 && s2)
    {
        return wcscmp(s1, s2) == 0;
    }
    else if (!s1 && !s2)
    {
        return true;
    }
    else
    {
        return false;
    }
}

