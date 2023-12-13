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
// [pfx_parse] - workaround for PREfix parse problems
//
#if ((defined(_PREFIX_)) || (defined(_PREFAST_)))  && (_MSC_VER < 1400)

template<class T>
inline
Optional<T>::
Optional() : m_isValid(false)
{
}

#else //!_PREFIX_

template<class T>
inline
Optional<T>::
Optional() : m_isValid(false), m_value()
{
}

#endif //!_PREFIX_



template<class T>
inline
Optional<T>::
Optional(
    T value
    ) : m_isValid(true), m_value(value)
{
}



template<class T>
inline
Optional<T> &
Optional<T>::
operator=(
    __in  T value
    )
{
    m_value = value;
    m_isValid = true;

    return *this;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      Optional<T>::DoesMatch
//
//  Synopsis:
//      Check if an Optional<T> matches a T. They match if this
//      Optional<T> is invalid (indicating don't care) or if the underlying
//      string matches the T.
//
//------------------------------------------------------------------------------
template <class T>
bool
Optional<T>::
DoesMatch(
    T value
    )
    const
{
    return (!m_isValid) || (m_value == value);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      Optional<T>::ApplyAsMask
//
//  Synopsis:
//      Use this Optional<T> as a mask. We return m_value if we're valid,
//      otherwise we return value.
//
//------------------------------------------------------------------------------
template <class T>
T
Optional<T>::
ApplyAsMask(
    T value
    )
    const
{
    if (m_isValid)
    {
        return m_value;
    }
    else
    {
        return value;
    }
}

//+-----------------------------------------------------------------------------
//
//  Function:
//      operator==(Optional<T>, Optional<T>)
//
//  Synopsis:
//      Compare two Optionals
//
//------------------------------------------------------------------------------
template <class T>
bool operator==(
    __in    Optional<T>     &o1,
    __in    Optional<T>     &o2
    )
{
    return (   (!o1.m_isValid && !o2.m_isValid)
            || (o1.m_isValid && o2.m_isValid && o1.m_value == o2.m_value));
}

//+-----------------------------------------------------------------------------
//
//  Function:
//      operator!=(Optional<T>, Optional<T>)
//
//  Synopsis:
//      Compare two Optionals
//
//------------------------------------------------------------------------------
template <class T>
bool operator!=(
    __in    Optional<T>     &o1,
    __in    Optional<T>     &o2
    )
{
    return !(o1 == o2);
}


