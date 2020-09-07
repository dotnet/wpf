// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#pragma once

template<typename T, typename P>
class Scope
{
  public:
    Scope() :
        m_t(P::DefaultValue())
    {
    }
    
    Scope(const T& t) :
        m_t(t)
    {
    }

    ~Scope()
    {
        P::Close(m_t);
    }

  public:
    T& get()
    {
        return m_t;
    }

  public:
    operator T()
    {
        return m_t;
    }

    bool operator==(const T& t) const
    {
        return m_t == t;
    }

    bool operator!=(const T& t) const
    {
        return m_t != t;
    }

  private:
    T m_t;

  private: //not implemented
    Scope(const Scope& o) {}
    Scope& operator=(const Scope& o) {}
};

