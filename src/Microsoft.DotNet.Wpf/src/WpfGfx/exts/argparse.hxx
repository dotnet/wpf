// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#pragma once

class CommandLine
{
public:
    struct ARGUMENT
    {
        bool fIsOption;
        size_t cchLength;
        PSTR string;
    };

    static HRESULT CreateFromString(OutputControl&, PCSTR args, __deref_out CommandLine** ppCommandLine);
    CommandLine();
    virtual ~CommandLine();

    UINT GetCount() const { return m_cCount; } 
    ARGUMENT& operator[](UINT i) const { return m_pArguments[i]; }

private:
    ARGUMENT* m_pArguments;
    UINT m_cCount;
};


