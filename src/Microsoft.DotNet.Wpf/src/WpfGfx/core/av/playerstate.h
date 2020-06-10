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

//
// Describes the four WMP action states that we recognize.
//
namespace ActionState
{
    enum Enum
    {
        Stop,
        Pause,
        Play,
        Buffer
    };

}

//
// Useful template for allowing a value to be present or invalid. Some types
// come with an invalid value (e.g. NULL for pointers) but using this template
// we can create invalid values for any type
//
template <class T>
struct Optional
{
    bool                m_isValid;
    T                   m_value;

    inline
    Optional();

    inline
    Optional(
        T value
        );

    inline
    Optional &
    operator=(
        __in  T value
        );

    bool DoesMatch(T value) const;

    T ApplyAsMask(T value) const;
};

//
// Because there are failure points when allocating strings, and strings
// need to be cleaned up, and string comparison is special,
// OptionalBstr has to be different. I didn't use specialization because
// almost everything would have to be specialized, and there would be
// subtle bugs if I missed anything (e.g. pointer comaprison would usually
// work, but not always)
//
struct OptionalString
{
    bool                m_isValid;
    PWSTR               m_value;

    OptionalString();

    ~OptionalString();

    bool
    DoesMatch(
        PCWSTR  value
        ) const;

    HRESULT
    ApplyAsMask(
        __in        PCWSTR      uri,
        __deref_out PWSTR       *pRet
        ) const;
};

template <class T>
bool operator==(
    __in    Optional<T>     &o1,
    __in    Optional<T>     &o2
    );

template <class T>
bool operator!=(
    __in    Optional<T>     &o1,
    __in    Optional<T>     &o2
    );

//
// The state of media encompasses more than just whether it is playing, paused,
// or stopped; it also includes the current url, whether the WMP OCX has been
// created or not, if we're currently seeking, etc. This struct describes the
// entire state of the media at a point in time
//
struct PlayerState
{
    bool                m_isOcxCreated;
    PWSTR                m_url;
    ActionState::Enum   m_actionState;
    long                m_volume;
    long                m_balance;
    double              m_rate;
    Optional<double>    m_seekTo;

    PlayerState();
    ~PlayerState();

    void
    Clear(
        void
        );

    HRESULT
    Copy(
        __out PlayerState *
        ) const;

    void
    DumpPlayerState(
        __in UINT       uiID,
        __in char       *description
        ) const;

private:
    //
    // Copy and assigning are not allowed because they have failure points. Use
    // Copy
    //
    PlayerState(
        __in PlayerState &
        );

    PlayerState &
    operator=(
        __in PlayerState &
        );

    static const long msc_defaultWmpVolume = 100;
};

bool operator==(
    __in    PlayerState     &ps1,
    __in    PlayerState     &ps2
    );

bool operator!=(
    __in    PlayerState     &ps1,
    __in    PlayerState     &ps2
    );

bool
AreStringsEqual(
    __in_opt    const wchar_t     *s1,
    __in_opt    const wchar_t     *s2
    );

#include "playerstate.inl"

